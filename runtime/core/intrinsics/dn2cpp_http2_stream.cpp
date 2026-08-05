// dn2cpp_http2_stream.cpp — THE native transport for System.Net.Http, backed by
// the vendored libcurl (third_party/curl/) over the vendored Mbed TLS
// (third_party/mbedtls/): http:// and https:// alike, with no dependency on
// anything the build host or the run host provides. The DnHttp managed shim binds
// this surface with [DllImport("dn2cpp_http")] — plain unmangled C symbols (the
// extern "C" declarations in dn2cpp_core.h give these definitions their C linkage
// through the #include below; keep it), taking the shapes dn2cpp's P/Invoke
// marshalling emits: UTF-8 char*, char** + count, a byte pointer + length. No
// Dn2CppString/Dn2CppArray or GC allocation is involved.
//
// The streaming call family (dn2cpp_http2_call_*) is the route EVERY request
// takes. One opaque handle per call: open() hands the call's easy handle to the
// ONE process-wide transport thread (TransportHub below), which drives every
// transfer on one curl multi handle; write()/finish_send() feed an unknown-length
// request body incrementally (h2 DATA frames, or chunked HTTP/1.1);
// wait_headers() blocks until the response header block is complete
// (HttpCompletionOption.ResponseHeadersRead semantics); read() delivers body bytes
// as they arrive; trailers become readable once read() returns EOS.
//
// A KNOWN-length request body is handed over whole at open() instead, and goes out
// through CURLOPT_POSTFIELDS: a Content-Length rather than a chunked framing, and
// rewindable, so FOLLOWLOCATION can re-send it. The incremental route can do
// neither, which is why the two request shapes stay distinct even though the
// RESPONSE side is one path for both.
//
// Design contract:
//   * The call never throws. A null/empty url, a DNS/connect failure or a timeout
//     is a failed handle with a non-null error string — the shape HttpClient sees
//     before EnsureSuccessStatusCode(). A real HTTP response, including 4xx/5xx,
//     signals headers-ready with that status. This mirrors real HttpClient: only a
//     transport-level problem is an exception; a 404 is a perfectly good
//     HttpResponseMessage.
//   * One curl_easy handle per call, all driven by the one process-wide multi
//     (TransportHub below). The multi's own caches are what calls reuse through:
//     an idle connection, a DNS entry, and — because concurrent transfers live on
//     ONE multi — an h2 connection another call is actively using, which curl
//     MULTIPLEXES new streams onto (CURLMOPT_PIPELINING). The one other
//     process-wide prerequisite, curl_global_init, runs once on the first request
//     (std::call_once — see EnsureGlobalInit below for why it is here and not in
//     dn2cpp_runtime_init).
//   * SocketsHttpHandler's two structural pool knobs cross per call and are
//     honored, not reported: EnableMultipleHttp2Connections=false (the default)
//     becomes CURLOPT_PIPEWAIT on every h2-capable request — wait for the
//     connection being established and multiplex onto it, never open a second —
//     and MaxConnectionsPerServer becomes CURLMOPT_MAX_HOST_CONNECTIONS, curl's
//     cap on CONCURRENT connections per host (transfers past it queue inside the
//     multi, which is what real .NET does past its cap too). The cap's one
//     approximation is scope: .NET pools per handler, this multi is per process,
//     so the most recent open's value governs — exact for the sequential-handler
//     shape real programs have, last-writer-wins when two live handlers disagree.
//   * TLS VERIFICATION IS ON AND STAYS ON. curl's defaults
//     (CURLOPT_SSL_VERIFYPEER=1, CURLOPT_SSL_VERIFYHOST=2) are left alone and this
//     file exposes no way to turn either off — a knob that disables verification
//     is one that eventually ships enabled, and a request that cannot be verified
//     must fail, not succeed quietly. A verification failure is a TRANSPORT
//     failure, so it arrives the same way a refused connection does: a failed call
//     with the backend's own diagnosis in the error string ("mbedTLS: The
//     certificate is not correctly signed by the trusted CA"), which the managed
//     side turns into an HttpRequestException.
//   * THE TRUST ANCHORS ARE COMPILED IN, not read from the platform. Mbed TLS has
//     no OS-trust-store integration (curl's mbedTLS backend implements no
//     native_ca_store arm), and the Mozilla bundle in third_party/cacert/ is
//     embedded as a byte array by runtime/CMakeLists.txt's DN2CPP_USE_CURL arm and
//     handed over as CURLOPT_CAINFO_BLOB. The blob form, not CURLOPT_CAINFO's
//     path, is what makes this one line of code work identically on a desktop,
//     inside an APK, inside an .ipa and inside a wasm module — none of the last
//     three has a CA file at a path open(2) reaches.
//   * ONE OVERRIDE, AND IT IS AN ENVIRONMENT VARIABLE. DN2CPP_HTTP_CAINFO=<path>
//     makes this transport trust that PEM file instead of the embedded bundle —
//     for a corporate MITM proxy's root, or for a test that must trust a
//     self-signed certificate. It replaces the anchor set; it does not weaken
//     verification. An env var is the right lever here, and the --trim-reflection
//     flag-not-env rule does not reach it: that rule exists because
//     --trim-reflection changes the C++ a successful transpile EMITS, and the
//     self-host fixpoint requires that nothing in the environment can do that.
//     This changes no transpile at all — it is read by the built program, at
//     request time, exactly like DN2CPP_GC_INCREMENTAL.
//
// ── "NO CURL" MEANS TWO DIFFERENT THINGS, AND THIS FILE HAS AN ARM FOR EACH ─────
// The TU is a three-way switch on DN2CPP_USE_CURL (set PUBLIC by
// runtime/CMakeLists.txt's DN2CPP_USE_CURL arm) and on the target. Which arm
// compiles decides what a program that touches HttpClient does, and the difference
// between the two curl-less arms is deliberate — do not collapse them:
//
//   1. DN2CPP_USE_CURL set — the real transport, everything below. This is the
//      DEFAULT (runtime/CMakeLists.txt's option()), affordable because the
//      transport initializes lazily: see EnsureGlobalInit below for why a program
//      that issues no request links none of it.
//
//   2. Curl off on a target that COULD have had it: the TU is empty, the
//      dn2cpp_http2_call_* symbols are undefined, and a program reaching the HTTP
//      surface fails loudly at LINK. That is the right answer, because somebody
//      said `-DDN2CPP_USE_CURL=OFF`: they declined the transport, and a build that
//      then silently produced a binary whose HTTP always fails would be answering
//      a question they did not ask. Same opt-out discipline as
//      dn2cpp_zlib_native.cpp.
//
//   3. Curl off because the TARGET CANNOT HAVE IT — today exactly Emscripten,
//      which forces the option off (runtime/CMakeLists.txt's Web-lane carve-out: a
//      browser has no TCP socket layer, so Emscripten's socket() reaches only a
//      WebSocket proxy). Nobody declined anything here; the platform did. So the
//      fallback arm at the bottom of this file is ALWAYS COMPILED there and
//      defines the whole dn2cpp_http2_call_* surface, every call answering the
//      failed shape with an error string that names the cause. The Web export
//      therefore links and runs, and only the HTTP-using feature degrades — into
//      an HttpRequestException the game can catch, carrying a sentence that says
//      what happened. The alternative is arm 2's link error, i.e. a whole Web
//      export that cannot be built because one screen fetches a leaderboard.
//
// The rule behind the split: a link error is a message to whoever configured the
// build, and it is only useful when that person can act on it. In arm 2 they can
// (turn the option on). In arm 3 they cannot, and the person who needs the message
// is the one running the game — so it has to be a run-time diagnostic instead. A
// benign stub with a null error would be neither, and is the one shape that must
// never appear here.
//
// Why ONE transport thread driving one multi — not curl multi driven by the
// managed callers, and not a thread per call:
//   * curl_easy_pause must run on the thread driving the transfer, so resuming a
//     paused upload from a managed writer thread would not wake the poll — the
//     writer enqueues + curl_multi_wakeup, and the transport thread itself resumes.
//   * progress (flushing queued DATA, receiving server pushes) must not require a
//     concurrently blocked managed call; a caller-driven multi stalls the transfer
//     between managed calls, which is a deadlock shape for interlocked bidi.
//   * h2 multiplexing is a property of the MULTI: curl multiplexes a new transfer
//     onto a connection only when the same multi owns both. A per-call multi (the
//     old shape) gave every concurrent request its own connection — two
//     simultaneous calls to one server would be two handshakes — and makes
//     MaxConnectionsPerServer / EnableMultipleHttp2Connections=false structurally
//     unenforceable.
// The thread runs no managed code (pure curl + byte queues), so it is not
// GC-registered. It starts on the first open and never exits (dn2cpp_never_destroyed
// posture — a transfer may still be draining while the process tears its statics
// down); free() waits only until the transport thread has DETACHED the call's easy
// handle from the multi, never for the thread.
//
// Backpressure runs both ways, and neither direction blocks the managed caller's
// scheduler. The receive queue pauses the transfer (CURL_WRITEFUNC_PAUSE) past a
// high-water mark and resumes when the reader drains below the low-water mark. The
// send queue is bounded by the same shape, but write() still never blocks: it
// ACCEPTS the chunk and answers 2, and the writer parks in await_send_drain — a
// blocking call the managed side runs on a pool worker, so the queue-full wait is a
// pending Task rather than a stalled scheduler thread. Accepting before parking is
// what keeps the queue exceeding its bound by at most one chunk while making a
// partial write impossible.
//
// A redirect of a streamed request fails loudly (curl cannot rewind a one-pass
// body), never silently re-sends. Follows-redirects stays on to match
// HttpClient's default handler (AllowAutoRedirect); gRPC never redirects.

#ifdef DN2CPP_USE_CURL

#include "dn2cpp_core.h"

#include <curl/curl.h>

#include <algorithm>
#include <condition_variable>
#include <cstdint>
#include <cstdio>
#include <cstdlib>
#include <cstring>
#include <mutex>
#include <string>
#include <thread>
#include <utility>
#include <vector>

// The Mozilla CA bundle, generated into a TU of its own by runtime/CMakeLists.txt's
// DN2CPP_USE_CURL arm (runtime/cmake/dn2cpp_embed_bytes.cmake over
// third_party/cacert/cacert.pem) and linked into the same static archive as this
// file. Declared rather than included: the generated TU is ~1 MB of hex that exists
// only in the build tree, so there is no header to include and nothing else may
// name these. C linkage, matching the generator: a gate asserts these by NAME in
// the symbol table, and only C linkage spells the name the same on every toolchain.
extern "C" const unsigned char dn2cpp_ca_bundle_pem[];
extern "C" const unsigned int dn2cpp_ca_bundle_pem_len;

namespace
{

// The two queue bounds — one knob per direction, same spelling. Environment-driven
// is sound here
// (unlike --trim-reflection): nothing this reads can perturb a transpile's emitted
// text, and a gate needs to drive the parked path in a second rather than a minute.
size_t WaterMarkFromEnv(const char* name, size_t fallback)
{
    const char* v = std::getenv(name);
    long long n = (v != nullptr && v[0] != '\0') ? std::atoll(v) : 0;
    return n < 4096 ? fallback : static_cast<size_t>(n);
}

// The send queue's bound. Default 1 MiB — orders above any gRPC message, so this
// family's original callers never reach the parked path at all.
size_t SendHighWater()
{
    static const size_t value = WaterMarkFromEnv("DN2CPP_HTTP_SEND_HIGH_WATER", 1u << 20);
    return value;
}

// A quarter of the bound, so a resumed writer gets a run of writes rather than one
// hand-off per drain.
size_t SendLowWater()
{
    return SendHighWater() / 4;
}

// The receive queue's bound: delivery parks in CURL_WRITEFUNC_PAUSE past it and
// resumes once the reader drains to the low water mark.
size_t RecvHighWater()
{
    static const size_t value = WaterMarkFromEnv("DN2CPP_HTTP_RECV_HIGH_WATER", 4u << 20);
    return value;
}

size_t RecvLowWater()
{
    return RecvHighWater() / 4;
}

bool SendStatsEnabled()
{
    static const bool on = []() {
        const char* v = std::getenv("DN2CPP_HTTP_SEND_STATS");
        return v != nullptr && v[0] != '\0' && v[0] != '0';
    }();
    return on;
}

// open()'s bodyMode; mirrored by DnHttp's Http2Native.Body* constants.
constexpr int32_t kBodyComplete = 1;
constexpr int32_t kBodyIncremental = 2;

void StripEol(std::string& line)
{
    while (!line.empty() && (line.back() == '\r' || line.back() == '\n'))
        line.pop_back();
}

bool IsHttpsUrl(const char* url)
{
    static const char prefix[] = "https://";
    for (size_t i = 0; prefix[i] != '\0'; i++)
    {
        char a = url[i];
        if (a >= 'A' && a <= 'Z')
            a = static_cast<char>(a - 'A' + 'a');
        if (a != prefix[i])
            return false;
    }
    return true;
}

struct HttpVersionPlan
{
    long curlOpt = CURL_HTTP_VERSION_1_1;
    int32_t requireExact = 0;
    int32_t requireMin = 0;
    std::string refusal;
};

const char* HttpVersionName(int32_t v)
{
    switch (v)
    {
        case 10: return "HTTP/1.0";
        case 11: return "HTTP/1.1";
        case 20: return "HTTP/2";
        case 30: return "HTTP/3";
        default: return "an unknown protocol";
    }
}

HttpVersionPlan PlanHttpVersion(int32_t httpVersion, int32_t versionPolicy, bool isHttps)
{
    HttpVersionPlan plan;
    switch (httpVersion)
    {
        case 10:
            plan.curlOpt = CURL_HTTP_VERSION_1_0;
            return plan;
        case 0:
        case 11:
            plan.curlOpt = (versionPolicy == 2 && isHttps) ? CURL_HTTP_VERSION_2TLS
                                                           : CURL_HTTP_VERSION_1_1;
            return plan;
        case 20:
            switch (versionPolicy)
            {
                case 1:
                    plan.curlOpt = isHttps ? CURL_HTTP_VERSION_2_0
                                           : CURL_HTTP_VERSION_2_PRIOR_KNOWLEDGE;
                    plan.requireExact = 20;
                    return plan;
                case 2:
                    plan.curlOpt = isHttps ? CURL_HTTP_VERSION_2_0
                                           : CURL_HTTP_VERSION_2_PRIOR_KNOWLEDGE;
                    plan.requireMin = 20;
                    return plan;
                default:
                    plan.curlOpt = isHttps ? CURL_HTTP_VERSION_2TLS
                                           : CURL_HTTP_VERSION_1_1;
                    return plan;
            }
        case 30:
            plan.refusal = "HTTP/3 was requested but is not built into this transport "
                           "(no QUIC backend is vendored).";
            return plan;
        default:
            plan.refusal = "The request named an HTTP version this transport does not "
                           "know (Version " + std::to_string(httpVersion / 10) + "." +
                           std::to_string(httpVersion % 10) + ").";
            return plan;
    }
}

bool HasHeader(const char* const* headers, int32_t headerCount, const char* name)
{
    if (headers == nullptr)
        return false;
    size_t nameLen = strlen(name);
    for (int32_t i = 0; i < headerCount; i++)
    {
        const char* h = headers[i];
        if (h == nullptr)
            continue;
        size_t j = 0;
        while (j < nameLen)
        {
            char a = h[j];
            char b = name[j];
            if (a >= 'A' && a <= 'Z')
                a = static_cast<char>(a - 'A' + 'a');
            if (b >= 'A' && b <= 'Z')
                b = static_cast<char>(b - 'A' + 'a');
            if (a != b)
                break;
            j++;
        }
        if (j == nameLen && h[j] == ':')
            return true;
    }
    return false;
}

// The DN2CPP_HTTP_CAINFO override (see the design contract at the top), read ONCE
// per process. Once, not per request, for two reasons: getenv is not safe to race
// against a setenv on another thread and these calls run on Task.Run pool workers;
// and a trust anchor set that could change between two requests of one program is a
// property nobody wants to have to reason about. The empty case answers nullptr,
// which is the caller's "use the embedded bundle" signal.
const char* CaFileOverride()
{
    // Function-local static: C++11 guarantees the initializer runs exactly once,
    // even if several workers reach here at the same instant.
    static const std::string value = []() {
        const char* v = std::getenv("DN2CPP_HTTP_CAINFO");
        return v != nullptr ? std::string(v) : std::string();
    }();
    return value.empty() ? nullptr : value.c_str();
}

// libcurl's one required process-wide setup, run on the FIRST REQUEST rather than
// at startup — and that placement is the whole reason a binary does not carry
// libcurl unless it uses it: an unconditional call from a TU every program links
// would pull this object, and with it all of libcurl, Mbed TLS and the ~180 KB
// embedded CA bundle, into programs that never issue a request (measured +578 KB
// on HelloWorld when the init lived in dn2cpp_runtime_init). A program with no
// HTTP call site never names a symbol in this TU, static-archive semantics leave
// the object unextracted, and none of it is linked — which is what lets
// DN2CPP_USE_CURL be the default without taxing hello-world.
//
// The init now runs on whichever thread issues the first request. Three things
// carry curl's "call it single-threaded" advice:
//   * std::call_once serializes OUR side completely: no second open can reach
//     curl_easy_init until the flag is set.
//   * curl_global_init is itself locked in the vendored version (lib/easy.c under
//     GLOBAL_INIT_IS_THREADSAFE, defined here — this build has atomics and POSIX
//     threads).
//   * NOTHING ELSE IN THE PROCESS CAN CALL THIS LIBCURL: the vendored copy is
//     compiled with hidden visibility (see runtime/CMakeLists.txt's mbedTLS arm),
//     so a host process carrying its own curl cannot bind to ours.
// What changes is WHEN a failing init is discovered: at the first request instead
// of at startup. The return code is therefore kept and reported.
CURLcode EnsureGlobalInit()
{
    static CURLcode rc = CURLE_FAILED_INIT;
    static std::once_flag once;
    std::call_once(once, [] { rc = curl_global_init(CURL_GLOBAL_DEFAULT); });
    return rc;
}

void TransportLoop();

// The process-wide transport: ONE multi handle, driven by ONE thread, owning every
// live transfer. One multi is not an implementation convenience — it is the h2
// multiplexing boundary (the header's "why one thread" note), and its conncache is
// what replaced the old CURLSH: an idle connection, a DNS entry, or an
// actively-multiplexing h2 connection is reused by whatever call arrives next.
// Started on the first open and never torn down (dn2cpp_never_destroyed posture).
struct TransportHub
{
    std::mutex mtx;                        // guards pending + started/multi setup
    std::vector<Dn2CppHttp2Call*> pending; // opened, not yet admitted to the multi
    CURLM* multi = nullptr;                // written once under mtx, then read-only
    bool started = false;
    // Transport-thread-only: the CURLMOPT_MAX_HOST_CONNECTIONS value last applied.
    long maxHostApplied = -1;
};

TransportHub& Hub()
{
    static TransportHub& h = dn2cpp_never_destroyed<TransportHub>();
    return h;
}

// nullptr = curl_multi_init failed (reported like any other open-time failure).
// The multi pointer is written exactly once, under the hub mutex, before the open
// that triggered it returns its handle — so every later reader (HubWakeup from a
// writer/reader thread) sees it without the lock.
CURLM* EnsureHubStarted()
{
    TransportHub& h = Hub();
    std::lock_guard<std::mutex> lk(h.mtx);
    if (!h.started)
    {
        CURLM* m = curl_multi_init();
        if (m == nullptr)
            return nullptr;
        // Explicit, not left to curl's default: multiplexing new transfers onto a
        // live h2 connection is the property the whole hub exists for.
        curl_multi_setopt(m, CURLMOPT_PIPELINING, CURLPIPE_MULTIPLEX);
        h.multi = m;
        std::thread(TransportLoop).detach();
        h.started = true;
    }
    return h.multi;
}

// Wake the transport thread's curl_multi_poll (thread-safe by curl's contract).
// Callers hold a handle some open() returned, which happens-after the hub start.
void HubWakeup()
{
    TransportHub& h = Hub();
    if (h.multi != nullptr)
        curl_multi_wakeup(h.multi);
}

// "HTTP/2 200", "HTTP/1.1 200 OK" → Major*10+Minor; 0 unknown. Parsed from the
// status line because CURLINFO_HTTP_VERSION is not warranted mid-transfer from a
// callback, and the fabricated h2 status line always names the real protocol.
int32_t ParseStatusLineVersion(const std::string& line)
{
    size_t sp = line.find(' ');
    std::string v = line.substr(5, (sp == std::string::npos ? line.size() : sp) - 5);
    if (v == "2" || v == "2.0") return 20;
    if (v == "1.1") return 11;
    if (v == "1.0") return 10;
    if (v == "3" || v == "3.0") return 30;
    return 0;
}

} // namespace

// Opaque to managed code; every borrowed pointer an accessor hands back stays valid
// until dn2cpp_http2_call_free. The managed wrapper serializes free against every
// in-flight accessor (its refcount), so nothing here defends against use-after-free.
struct Dn2CppHttp2Call
{
    // Immutable after open.
    int32_t requireExact = 0;
    int32_t requireMin = 0;
    // A known-length body, copied because CURLOPT_POSTFIELDS does not and the
    // transfer outlives the managed call that handed the bytes over.
    std::string reqBody;

    std::mutex mtx;
    std::condition_variable cv;

    // Send queue (guarded): appended by write(), drained by the read callback.
    std::string sendBuf;
    size_t sendPos = 0;
    bool sendClosed = false;
    bool sendPaused = false; // the transfer's upload is parked in CURL_READFUNC_PAUSE
    uint64_t sendParks = 0;  // writes answered "park" (DN2CPP_HTTP_SEND_STATS)
    size_t sendPeak = 0;     // high-water the backlog actually reached

    // Receive queue (guarded): appended by the write callback, drained by read().
    std::string recvBuf;
    size_t recvPos = 0;
    bool recvPaused = false; // the transfer parked delivery in CURL_WRITEFUNC_PAUSE

    // Current header block being parsed (reset per hop — 1xx interim, redirects).
    int32_t curStatus = 0;
    int32_t curVersion = 0;
    std::string curReason;
    std::vector<std::pair<std::string, std::string>> curHeaders;

    // The final response, frozen once headersReady (never mutated after).
    bool headersReady = false;
    int32_t status = 0;
    int32_t version = 0;
    std::string reason;
    std::vector<std::pair<std::string, std::string>> headers;
    std::vector<std::pair<std::string, std::string>> trailers; // filled at completion

    bool done = false;     // transfer finished (trailers/result final)
    bool failed = false;   // error is non-empty
    bool aborted = false;  // abort()/free() asked the transfer to stop
    bool detached = false; // the transport thread holds no reference any more;
                           // free() may clean the easy handle up
    std::string error;
    char errbuf[CURL_ERROR_SIZE] = {0};

    long maxHostConns = 0; // SocketsHttpHandler.MaxConnectionsPerServer; 0 = unlimited
    CURL* easy = nullptr;
    curl_slist* headerList = nullptr;
};

namespace
{

size_t H2WriteBody(char* ptr, size_t size, size_t nmemb, void* userdata)
{
    auto* c = static_cast<Dn2CppHttp2Call*>(userdata);
    size_t total = size * nmemb;
    std::lock_guard<std::mutex> lk(c->mtx);
    if (c->aborted)
        return 0; // abort the transfer (write error)
    if (c->recvBuf.size() - c->recvPos >= RecvHighWater())
    {
        // Nothing appended: curl re-delivers the same bytes after the resume.
        c->recvPaused = true;
        return CURL_WRITEFUNC_PAUSE;
    }
    c->recvBuf.append(ptr, total);
    c->cv.notify_all();
    return total;
}

// Every received header line, including status lines and blank separators.
// Parses the block by hand (name/value pairs, reason phrase, protocol from the
// status line) so headers-ready can be signaled MID-TRANSFER — the whole point of
// this family. A new status line (redirect hop, 1xx interim) resets the block; the
// blank line after a final (non-1xx, non-3xx) block freezes it and wakes
// wait_headers. A final 3xx (redirect not followed) is frozen at completion
// instead. Lines after headersReady are trailers; they are collected at completion
// through curl's headers API, so they are ignored here.
size_t H2HeaderCb(char* ptr, size_t size, size_t nmemb, void* userdata)
{
    auto* c = static_cast<Dn2CppHttp2Call*>(userdata);
    size_t total = size * nmemb;
    std::string line(ptr, total);
    StripEol(line);
    std::lock_guard<std::mutex> lk(c->mtx);
    if (c->aborted)
        return 0;
    if (c->headersReady)
        return total;

    if (line.rfind("HTTP/", 0) == 0)
    {
        c->curHeaders.clear();
        c->curReason.clear();
        c->curVersion = ParseStatusLineVersion(line);
        c->curStatus = 0;
        size_t sp1 = line.find(' ');
        if (sp1 != std::string::npos)
        {
            c->curStatus = std::atoi(line.c_str() + sp1 + 1);
            size_t sp2 = line.find(' ', sp1 + 1);
            if (sp2 != std::string::npos && sp2 + 1 < line.size())
                c->curReason = line.substr(sp2 + 1);
        }
        return total;
    }
    if (line.empty())
    {
        if (c->curStatus >= 100 && c->curStatus < 200)
        {
            c->curHeaders.clear(); // interim block; the real one follows
            return total;
        }
        if (c->curStatus >= 300 && c->curStatus < 400)
            return total; // FOLLOWLOCATION may follow; a final 3xx freezes at completion
        c->status = c->curStatus;
        c->version = c->curVersion;
        c->reason = c->curReason;
        c->headers = c->curHeaders;
        if ((c->requireExact != 0 && c->version != c->requireExact) ||
            (c->requireMin != 0 && c->version < c->requireMin))
        {
            int32_t demanded = c->requireExact != 0 ? c->requireExact : c->requireMin;
            c->failed = true;
            c->error = std::string(HttpVersionName(demanded)) +
                       (c->requireExact != 0
                            ? " was required (HttpVersionPolicy.RequestVersionExact)"
                            : " or higher was required (HttpVersionPolicy.RequestVersionOrHigher)") +
                       " but the server negotiated " + HttpVersionName(c->version) + ".";
            c->cv.notify_all();
            return 0; // abort the transfer
        }
        c->headersReady = true;
        c->cv.notify_all();
        return total;
    }
    // "Name: value" — split at the first colon, trim the value like curl's headers
    // API does, so this family and the unary one hand back identical strings.
    size_t colon = line.find(':');
    if (colon != std::string::npos)
    {
        std::string name = line.substr(0, colon);
        size_t vb = colon + 1;
        while (vb < line.size() && (line[vb] == ' ' || line[vb] == '\t'))
            vb++;
        size_t ve = line.size();
        while (ve > vb && (line[ve - 1] == ' ' || line[ve - 1] == '\t'))
            ve--;
        c->curHeaders.emplace_back(std::move(name), line.substr(vb, ve - vb));
    }
    return total;
}

size_t H2ReadBody(char* buffer, size_t size, size_t nitems, void* userdata)
{
    auto* c = static_cast<Dn2CppHttp2Call*>(userdata);
    size_t cap = size * nitems;
    std::lock_guard<std::mutex> lk(c->mtx);
    if (c->aborted)
        return CURL_READFUNC_ABORT;
    size_t avail = c->sendBuf.size() - c->sendPos;
    if (avail == 0)
    {
        if (c->sendClosed)
            return 0; // upload EOS (h2 END_STREAM / final chunk)
        c->sendPaused = true;
        return CURL_READFUNC_PAUSE;
    }
    size_t n = avail < cap ? avail : cap;
    memcpy(buffer, c->sendBuf.data() + c->sendPos, n);
    c->sendPos += n;
    if (c->sendPos == c->sendBuf.size())
    {
        c->sendBuf.clear();
        c->sendPos = 0;
    }
    if (c->sendBuf.size() - c->sendPos <= SendLowWater())
        c->cv.notify_all(); // a parked writer may hand over its next chunk
    return n;
}

// Freeze a finished (or aborted) call's result — the error string, the
// not-yet-signaled header block of a final-3xx or trailers-only-failure shape, the
// trailers — mark it detached, and wake every waiter. Transport thread only, and
// only AFTER curl_multi_remove_handle: `detached` is free()'s license to
// curl_easy_cleanup the handle, which must be off the multi by then.
void FinalizeCall(Dn2CppHttp2Call* c, bool haveResult, CURLcode result)
{
    std::lock_guard<std::mutex> lk(c->mtx);
    if (!c->failed)
    {
        if (c->aborted)
        {
            c->failed = true;
            c->error = "The call was aborted.";
        }
        else if (!haveResult || result != CURLE_OK)
        {
            c->failed = true;
            c->error = c->errbuf[0] != '\0'
                           ? c->errbuf
                           : (haveResult ? curl_easy_strerror(result)
                                         : "the transfer ended without a result");
        }
    }
    if (!c->failed && !c->headersReady)
    {
        // A completed transfer whose header block never signaled: a final 3xx.
        c->status = c->curStatus;
        c->version = c->curVersion;
        c->reason = c->curReason;
        c->headers = c->curHeaders;
        if ((c->requireExact != 0 && c->version != c->requireExact) ||
            (c->requireMin != 0 && c->version < c->requireMin))
        {
            int32_t demanded = c->requireExact != 0 ? c->requireExact : c->requireMin;
            c->failed = true;
            c->error = std::string(HttpVersionName(demanded)) +
                       " was required but the server negotiated " +
                       HttpVersionName(c->version) + ".";
        }
        else
        {
            c->headersReady = true;
        }
    }
    if (!c->failed)
    {
        // Final-request trailers; safe here — this thread was the only curl driver
        // and the transfer is over.
        curl_header* h = nullptr;
        while ((h = curl_easy_nextheader(c->easy, CURLH_TRAILER, -1, h)) != nullptr)
            c->trailers.emplace_back(h->name, h->value != nullptr ? h->value : "");
    }
    c->done = true;
    c->detached = true;
    c->cv.notify_all();
}

// Apply the admitted call's MaxConnectionsPerServer to the multi (0 = unlimited,
// which is curl's spelling too). Scope approximation documented in the header:
// last writer wins across handlers; curl_multi_setopt is sound here because this
// thread is the only multi driver.
void ApplyMaxHostConnections(TransportHub& h, CURLM* multi, Dn2CppHttp2Call* c)
{
    if (c->maxHostConns == h.maxHostApplied)
        return;
    curl_multi_setopt(multi, CURLMOPT_MAX_HOST_CONNECTIONS, c->maxHostConns);
    h.maxHostApplied = c->maxHostConns;
}

// The transport thread: admits pending calls onto the one multi, drives every
// transfer, resumes paused directions (curl_easy_pause is only sound on this
// thread), detaches aborted transfers, and freezes each result as it completes.
// Loops forever; curl_multi_poll parks it when nothing is live, and HubWakeup
// (open/write/finish_send/read/abort/free) is what un-parks it.
void TransportLoop()
{
    TransportHub& h = Hub();
    CURLM* multi = h.multi;
    std::vector<Dn2CppHttp2Call*> active;
    for (;;)
    {
        // Admit pending opens. A failed add is finalized right here — the call
        // never joins `active`, and the caller's wait_headers sees the failure.
        std::vector<Dn2CppHttp2Call*> adds;
        {
            std::lock_guard<std::mutex> lk(h.mtx);
            adds.swap(h.pending);
        }
        bool acted = !adds.empty();
        for (Dn2CppHttp2Call* c : adds)
        {
            ApplyMaxHostConnections(h, multi, c);
            CURLMcode mrc = curl_multi_add_handle(multi, c->easy);
            if (mrc != CURLM_OK)
            {
                std::lock_guard<std::mutex> lk(c->mtx);
                c->failed = true;
                c->error = std::string("curl_multi_add_handle failed: ") + curl_multi_strerror(mrc);
                c->done = true;
                c->detached = true;
                c->cv.notify_all();
                continue;
            }
            active.push_back(c);
        }

        int running = 0;
        curl_multi_perform(multi, &running);

        // Completions. CURLOPT_PRIVATE carries the call; the handle comes off the
        // multi BEFORE FinalizeCall flips `detached` (free()'s cleanup license).
        int queued = 0;
        while (CURLMsg* m = curl_multi_info_read(multi, &queued))
        {
            if (m->msg != CURLMSG_DONE)
                continue;
            Dn2CppHttp2Call* c = nullptr;
            curl_easy_getinfo(m->easy_handle, CURLINFO_PRIVATE, &c);
            curl_multi_remove_handle(multi, m->easy_handle);
            if (c != nullptr)
            {
                FinalizeCall(c, true, m->data.result);
                active.erase(std::find(active.begin(), active.end(), c));
                acted = true;
            }
        }

        // Aborts and pause/resume, per live call. The pause call runs OUTSIDE the
        // call mutex: curl_easy_pause can re-enter the callbacks (it flushes
        // buffered data), and those take the same lock.
        for (size_t i = 0; i < active.size();)
        {
            Dn2CppHttp2Call* c = active[i];
            bool doAbort = false;
            bool doResume = false;
            {
                std::lock_guard<std::mutex> lk(c->mtx);
                if (c->aborted)
                {
                    doAbort = true;
                }
                else
                {
                    if (c->sendPaused && (c->sendPos < c->sendBuf.size() || c->sendClosed))
                    {
                        c->sendPaused = false;
                        doResume = true;
                    }
                    if (c->recvPaused && c->recvBuf.size() - c->recvPos <= RecvLowWater())
                    {
                        c->recvPaused = false;
                        doResume = true;
                    }
                }
            }
            if (doAbort)
            {
                // Mid-transfer teardown: curl resets the stream (h2 RST_STREAM) or
                // closes the connection, which is what the server's RequestAborted
                // observes.
                curl_multi_remove_handle(multi, c->easy);
                FinalizeCall(c, false, CURLE_OK);
                active.erase(active.begin() + static_cast<ptrdiff_t>(i));
                acted = true;
                continue;
            }
            if (doResume)
            {
                curl_easy_pause(c->easy, CURLPAUSE_CONT);
                acted = true;
            }
            i++;
        }

        // Re-perform immediately after any admit/resume/removal: a resume's
        // re-delivery of paused bytes is not socket activity, so a poll here
        // would sit out its full timeout with work already queued.
        if (acted)
            continue;
        curl_multi_poll(multi, nullptr, 0, 1000, nullptr);
    }
}

// Marks the handle failed-before-start: never admitted to the multi (so free()
// may clean up at once — detached), done at once, every accessor answers the
// no-response shape and error() the sentence.
Dn2CppHttp2Call* FailedCall(Dn2CppHttp2Call* c, std::string error)
{
    c->failed = true;
    c->done = true;
    c->detached = true;
    c->error = std::move(error);
    return c;
}

} // namespace

Dn2CppHttp2Call* dn2cpp_http2_call_open(
    const char* method, const char* url,
    const char* const* headers, int32_t headerCount,
    int32_t bodyMode, const uint8_t* body, int32_t bodyLen,
    int32_t httpVersion, int32_t versionPolicy,
    int32_t maxIdleSeconds, int32_t maxLifetimeSeconds,
    int32_t maxConnsPerServer, int32_t multipleH2)
{
    auto* c = new Dn2CppHttp2Call();

    if (url == nullptr || url[0] == '\0')
        return FailedCall(c, "The request URI is null or empty.");

    bool isHttps = IsHttpsUrl(url);
    HttpVersionPlan plan = PlanHttpVersion(httpVersion, versionPolicy, isHttps);
    if (!plan.refusal.empty())
        return FailedCall(c, std::move(plan.refusal));
    c->requireExact = plan.requireExact;
    c->requireMin = plan.requireMin;
    c->maxHostConns = maxConnsPerServer > 0 ? maxConnsPerServer : 0;

    if (CURLcode grc = EnsureGlobalInit(); grc != CURLE_OK)
        return FailedCall(c, std::string("curl_global_init failed: ") + curl_easy_strerror(grc));

    CURLM* multi = EnsureHubStarted();
    if (multi == nullptr)
        return FailedCall(c, "curl_multi_init failed");

    c->easy = curl_easy_init();
    if (c->easy == nullptr)
        return FailedCall(c, "curl_easy_init failed");

    // The DONE-message mapping (TransportLoop reads it back with CURLINFO_PRIVATE).
    curl_easy_setopt(c->easy, CURLOPT_PRIVATE, c);

    // EnableMultipleHttp2Connections=false (the .NET default): an h2-capable
    // request WAITS for the connection already being established to this server
    // and multiplexes onto it, rather than opening a second one. Only for plans
    // that can negotiate h2 — a 1.x-pinned request in real .NET never waits on a
    // sibling's connect, it opens its own (up to the cap).
    bool h2Capable = plan.curlOpt == CURL_HTTP_VERSION_2TLS ||
                     plan.curlOpt == CURL_HTTP_VERSION_2_0 ||
                     plan.curlOpt == CURL_HTTP_VERSION_2_PRIOR_KNOWLEDGE;
    if (h2Capable && multipleH2 == 0)
        curl_easy_setopt(c->easy, CURLOPT_PIPEWAIT, 1L);

    curl_easy_setopt(c->easy, CURLOPT_URL, url);
    curl_easy_setopt(c->easy, CURLOPT_WRITEFUNCTION, H2WriteBody);
    curl_easy_setopt(c->easy, CURLOPT_WRITEDATA, c);
    curl_easy_setopt(c->easy, CURLOPT_HEADERFUNCTION, H2HeaderCb);
    curl_easy_setopt(c->easy, CURLOPT_HEADERDATA, c);
    // curl_easy_strerror only names the CATEGORY of a failure ("SSL peer certificate or
    // SSH remote key was not OK"); the error buffer is where the cause is ("SSL
    // certificate problem: self-signed certificate"). That distinction is the whole
    // difference between a diagnosable TLS failure and a mystery, so FinalizeCall
    // prefers the buffer and keeps strerror as the fallback for the codes that fill
    // in nothing.
    curl_easy_setopt(c->easy, CURLOPT_ERRORBUFFER, c->errbuf);
    // Follow redirects like HttpClient's default handler (AllowAutoRedirect), capped
    // at .NET's default MaxAutomaticRedirections rather than curl's unlimited.
    curl_easy_setopt(c->easy, CURLOPT_FOLLOWLOCATION, 1L);
    curl_easy_setopt(c->easy, CURLOPT_MAXREDIRS, 50L);
    // No signals: the SIGALRM-based fallback timeout is unsafe off the main thread.
    curl_easy_setopt(c->easy, CURLOPT_NOSIGNAL, 1L);
    curl_easy_setopt(c->easy, CURLOPT_HTTP_VERSION, plan.curlOpt);

    // SocketsHttpHandler's two pool knobs, in whole seconds, 0 = no limit. Set even at 0,
    // because 0 is how Timeout.InfiniteTimeSpan crosses and curl's unset defaults are not
    // .NET's — an explicit Infinite would otherwise get curl's 24-hour cap. lib/url.c's
    // conn_maxage reads both off the REQUESTING handle, so a per-request value is a
    // per-handler policy even though the cache is process-wide.
    curl_easy_setopt(c->easy, CURLOPT_MAXAGE_CONN, static_cast<long>(maxIdleSeconds));
    curl_easy_setopt(c->easy, CURLOPT_MAXLIFETIME_CONN, static_cast<long>(maxLifetimeSeconds));

    // Trust anchors: identical posture to the unary TU (see its design contract).
    if (const char* caFile = CaFileOverride())
    {
        curl_easy_setopt(c->easy, CURLOPT_CAINFO, caFile);
    }
    else
    {
        curl_blob caBlob;
        caBlob.data = const_cast<unsigned char*>(dn2cpp_ca_bundle_pem);
        caBlob.len = dn2cpp_ca_bundle_pem_len;
        caBlob.flags = CURL_BLOB_NOCOPY;
        curl_easy_setopt(c->easy, CURLOPT_CAINFO_BLOB, &caBlob);
    }

    // A ZERO-length complete body still frames: a request carrying a
    // zero-byte body and a request carrying no body are different HTTP messages —
    // real .NET sends `Content-Length: 0` for a non-null zero-length content, and
    // servers route on the difference. POSTFIELDSIZE 0 is what makes curl emit it.
    if (bodyMode == kBodyComplete && body != nullptr && bodyLen >= 0)
    {
        // POSTFIELDS at the COPY, so a rewind for FOLLOWLOCATION still has the bytes
        // (std::string::data() is a valid pointer even when empty).
        c->reqBody.assign(reinterpret_cast<const char*>(body), static_cast<size_t>(bodyLen));
        curl_easy_setopt(c->easy, CURLOPT_POSTFIELDSIZE_LARGE, static_cast<curl_off_t>(bodyLen));
        curl_easy_setopt(c->easy, CURLOPT_POSTFIELDS, c->reqBody.data());
    }
    else if (bodyMode == kBodyIncremental)
    {
        curl_easy_setopt(c->easy, CURLOPT_UPLOAD, 1L);
        curl_easy_setopt(c->easy, CURLOPT_READFUNCTION, H2ReadBody);
        curl_easy_setopt(c->easy, CURLOPT_READDATA, c);
        // No CURLOPT_INFILESIZE on purpose: the body length is unknown, which is
        // h2 DATA frames without content-length, or chunked over HTTP/1.1.
    }
    if (method != nullptr && method[0] != '\0')
        curl_easy_setopt(c->easy, CURLOPT_CUSTOMREQUEST, method);
    // HEAD must tell curl that no body follows: CUSTOMREQUEST changes only the
    // verb's wire spelling, so without CURLOPT_NOBODY curl waits for the
    // Content-Length bytes a HEAD response deliberately omits — on a keep-alive
    // HTTP/1.1 connection a silent stall until somebody's timeout, not a failure.
    if (method != nullptr && strcmp(method, "HEAD") == 0)
        curl_easy_setopt(c->easy, CURLOPT_NOBODY, 1L);

    curl_slist* headerList = nullptr;
    if (headers != nullptr)
    {
        for (int32_t i = 0; i < headerCount; i++)
        {
            if (headers[i] != nullptr)
                headerList = curl_slist_append(headerList, headers[i]);
        }
    }
    // Suppress what curl volunteers and real SocketsHttpHandler never sends — the
    // unary TU's rule, plus Expect (curl's 100-continue dance on 1.1 uploads).
    if (!HasHeader(headers, headerCount, "Accept"))
        headerList = curl_slist_append(headerList, "Accept:");
    if (!HasHeader(headers, headerCount, "Expect"))
        headerList = curl_slist_append(headerList, "Expect:");
    if (bodyMode != 0 && !HasHeader(headers, headerCount, "Content-Type"))
        headerList = curl_slist_append(headerList, "Content-Type:");
    if (headerList != nullptr)
        curl_easy_setopt(c->easy, CURLOPT_HTTPHEADER, headerList);
    c->headerList = headerList;

    // Hand the call to the transport thread. From here until `detached`, the easy
    // handle belongs to that thread; a failed admit surfaces through the same
    // failed/done/detached flags a transport failure does.
    {
        TransportHub& h = Hub();
        std::lock_guard<std::mutex> lk(h.mtx);
        h.pending.push_back(c);
    }
    HubWakeup();
    return c;
}

int32_t dn2cpp_http2_call_write(Dn2CppHttp2Call* c, const uint8_t* buf, int32_t len)
{
    if (c == nullptr || len < 0)
        return 0;
    std::lock_guard<std::mutex> lk(c->mtx);
    if (c->failed || c->aborted || c->done || c->sendClosed)
        return 0;
    if (len > 0 && buf != nullptr)
        c->sendBuf.append(reinterpret_cast<const char*>(buf), static_cast<size_t>(len));
    size_t backlog = c->sendBuf.size() - c->sendPos;
    if (backlog > c->sendPeak)
        c->sendPeak = backlog;
    HubWakeup();
    if (backlog >= SendHighWater())
    {
        c->sendParks++;
        return 2; // taken, but hand over nothing more until await_send_drain returns
    }
    return 1;
}

int32_t dn2cpp_http2_call_await_send_drain(Dn2CppHttp2Call* c)
{
    if (c == nullptr)
        return 0;
    std::unique_lock<std::mutex> lk(c->mtx);
    c->cv.wait(lk, [c] {
        return c->sendBuf.size() - c->sendPos <= SendLowWater() ||
               c->failed || c->done || c->aborted;
    });
    // The same three states write() already refuses for: a drained queue nobody can
    // send from is not writable, whatever its depth.
    return (c->failed || c->done || c->aborted) ? 0 : 1;
}

void dn2cpp_http2_call_finish_send(Dn2CppHttp2Call* c)
{
    if (c == nullptr)
        return;
    std::lock_guard<std::mutex> lk(c->mtx);
    c->sendClosed = true;
    HubWakeup();
}

int32_t dn2cpp_http2_call_wait_headers(Dn2CppHttp2Call* c)
{
    if (c == nullptr)
        return 0;
    std::unique_lock<std::mutex> lk(c->mtx);
    c->cv.wait(lk, [c] { return c->headersReady || c->failed || c->done || c->aborted; });
    // A failure AFTER the header block froze still hands the response over; the
    // body read reports the failure at the point the stream broke, like a real
    // handler's response stream.
    return c->headersReady ? 1 : 0;
}

int32_t dn2cpp_http2_call_status(Dn2CppHttp2Call* c)
{
    if (c == nullptr)
        return 0;
    std::lock_guard<std::mutex> lk(c->mtx);
    return c->status;
}

int32_t dn2cpp_http2_call_http_version(Dn2CppHttp2Call* c)
{
    if (c == nullptr)
        return 0;
    std::lock_guard<std::mutex> lk(c->mtx);
    return c->version;
}

const char* dn2cpp_http2_call_reason(Dn2CppHttp2Call* c)
{
    if (c == nullptr)
        return nullptr;
    std::lock_guard<std::mutex> lk(c->mtx);
    return c->reason.c_str(); // frozen once headersReady; callers read after wait_headers
}

const char* dn2cpp_http2_call_error(Dn2CppHttp2Call* c)
{
    if (c == nullptr)
        return nullptr;
    std::lock_guard<std::mutex> lk(c->mtx);
    return c->failed ? c->error.c_str() : nullptr;
}

int32_t dn2cpp_http2_call_header_count(Dn2CppHttp2Call* c)
{
    if (c == nullptr)
        return 0;
    std::lock_guard<std::mutex> lk(c->mtx);
    return static_cast<int32_t>(c->headers.size());
}

const char* dn2cpp_http2_call_header_name(Dn2CppHttp2Call* c, int32_t index)
{
    if (c == nullptr)
        return nullptr;
    std::lock_guard<std::mutex> lk(c->mtx);
    if (index < 0 || static_cast<size_t>(index) >= c->headers.size())
        return nullptr;
    return c->headers[static_cast<size_t>(index)].first.c_str();
}

const char* dn2cpp_http2_call_header_value(Dn2CppHttp2Call* c, int32_t index)
{
    if (c == nullptr)
        return nullptr;
    std::lock_guard<std::mutex> lk(c->mtx);
    if (index < 0 || static_cast<size_t>(index) >= c->headers.size())
        return nullptr;
    return c->headers[static_cast<size_t>(index)].second.c_str();
}

int32_t dn2cpp_http2_call_read(Dn2CppHttp2Call* c, uint8_t* buf, int32_t cap)
{
    if (c == nullptr || cap < 0 || (buf == nullptr && cap > 0))
        return -1;
    std::unique_lock<std::mutex> lk(c->mtx);
    c->cv.wait(lk, [c] { return c->recvPos < c->recvBuf.size() || c->done || c->failed || c->aborted; });
    size_t avail = c->recvBuf.size() - c->recvPos;
    // Zero-byte read: the availability probe SocketsHttpHandler response streams
    // support and grpc-dotnet issues before renting a buffer. Blocks (above) until
    // something exists, consumes nothing, answers 0 for data-or-EOS and -1 only
    // for a failure — the caller's NEXT sized read tells EOS and data apart.
    if (cap == 0)
    {
        if (avail > 0 || (!c->failed && !c->aborted))
            return 0;
        return -1;
    }
    if (avail > 0)
    {
        // Buffered bytes drain before a failure reports: the stream breaks where
        // the transfer did, not earlier.
        size_t n = avail < static_cast<size_t>(cap) ? avail : static_cast<size_t>(cap);
        memcpy(buf, c->recvBuf.data() + c->recvPos, n);
        c->recvPos += n;
        if (c->recvPos == c->recvBuf.size())
        {
            c->recvBuf.clear();
            c->recvPos = 0;
        }
        if (c->recvPaused && c->recvBuf.size() - c->recvPos <= RecvLowWater())
            HubWakeup(); // the transport thread resumes delivery
        return static_cast<int32_t>(n);
    }
    if (c->failed || c->aborted)
        return -1;
    return 0; // done and drained: EOS — trailers are final from here on
}

int32_t dn2cpp_http2_call_trailer_count(Dn2CppHttp2Call* c)
{
    if (c == nullptr)
        return 0;
    std::lock_guard<std::mutex> lk(c->mtx);
    return static_cast<int32_t>(c->trailers.size());
}

const char* dn2cpp_http2_call_trailer_name(Dn2CppHttp2Call* c, int32_t index)
{
    if (c == nullptr)
        return nullptr;
    std::lock_guard<std::mutex> lk(c->mtx);
    if (index < 0 || static_cast<size_t>(index) >= c->trailers.size())
        return nullptr;
    return c->trailers[static_cast<size_t>(index)].first.c_str();
}

const char* dn2cpp_http2_call_trailer_value(Dn2CppHttp2Call* c, int32_t index)
{
    if (c == nullptr)
        return nullptr;
    std::lock_guard<std::mutex> lk(c->mtx);
    if (index < 0 || static_cast<size_t>(index) >= c->trailers.size())
        return nullptr;
    return c->trailers[static_cast<size_t>(index)].second.c_str();
}

void dn2cpp_http2_call_abort(Dn2CppHttp2Call* c)
{
    if (c == nullptr)
        return;
    std::lock_guard<std::mutex> lk(c->mtx);
    c->aborted = true;
    c->cv.notify_all();
    HubWakeup();
}

void dn2cpp_http2_call_free(Dn2CppHttp2Call* c)
{
    if (c == nullptr)
        return;
    {
        std::unique_lock<std::mutex> lk(c->mtx);
        c->aborted = true;
        c->cv.notify_all();
        HubWakeup();
        // The transport thread owns the easy handle until it detaches it (a
        // completed or aborted transfer is removed from the multi and finalized);
        // cleaning the handle up under that ownership would be a use-after-free
        // inside curl. FailedCall handles were never admitted and are born
        // detached.
        c->cv.wait(lk, [c] { return c->detached; });
    }
    // Only a call that uploaded reports, so the line names the send queue it is
    // about rather than every request the program made.
    if (c->sendPeak != 0 && SendStatsEnabled())
    {
        fprintf(stderr, "dn2cpp: http2 send queue parks=%llu peak=%llu highwater=%llu\n",
                static_cast<unsigned long long>(c->sendParks),
                static_cast<unsigned long long>(c->sendPeak),
                static_cast<unsigned long long>(SendHighWater()));
        fflush(stderr);
    }
    if (c->easy != nullptr)
        curl_easy_cleanup(c->easy);
    if (c->headerList != nullptr)
        curl_slist_free_all(c->headerList);
    delete c;
}

#elif defined(__EMSCRIPTEN__)

// ── The Web lane's fallback transport (arm 3 of the header's three-way split) ────
// Compiled whenever this is an Emscripten build, which is always a curl-less one:
// runtime/CMakeLists.txt forces DN2CPP_USE_CURL off there because a browser has no
// TCP socket layer to give libcurl. This arm exists so that fact is reported
// instead of being discovered — it defines the entire dn2cpp_http2_call_* surface
// the transpiler's unconditional HttpClient route names, so the Web export LINKS,
// and every call fails the way a refused connection does: wait_headers answers 0,
// error() a non-null sentence naming the cause, read() -1 — which DnHttpBackend
// turns into an HttpRequestException. A game that guards its network calls keeps
// running; one that does not gets an exception naming the cause, at the call site,
// in the shipped build. open() hands back a real heap handle (the managed
// ownership contract is unchanged; a singleton would quietly break the first
// caller that frees twice).
//
// The one shape that must never appear here is a benign stub — a failure with
// error()==null, or headers-ready with an empty body. Either makes "this platform
// has no HTTP" indistinguishable from "the server said nothing", the same
// silent-wrong-answer failure the --trim-reflection metadata bit exists to
// prevent. The string below is the whole value of this arm.
//
// This is a carve-out, not a to-do. A real Web transport would be fetch()/XHR
// through JS, which is asynchronous, same-origin-restricted and cannot honour the
// blocking contract this family is written to; a game that needs HTTP on the Web
// should use the engine's HTTPRequest node, which speaks that dialect natively.

#include "dn2cpp_core.h"

#include <cstdint>
#include <cstddef>

struct Dn2CppHttp2Call
{
    // Static storage, not a std::string: the pointer error() hands back must
    // outlive the accessor call and stays valid until the free. Pure ASCII on
    // purpose: this sentence travels through the managed side into an exception
    // message a gate diffs byte for byte on every host
    // (build-and-run-wasm-console.sh section 5), and a stray non-ASCII byte is one
    // console code page away from being the thing that fails.
    static constexpr const char* kNoTransport =
        "dn2cpp: the Web export has no TCP socket layer, so System.Net.Http has no "
        "transport here. Emscripten's socket() reaches only a WebSocket proxy, not a "
        "server. Use the engine's HTTPRequest node for HTTP on the Web.";
};

extern "C" {

Dn2CppHttp2Call* dn2cpp_http2_call_open(
    const char* /*method*/, const char* /*url*/,
    const char* const* /*headers*/, int32_t /*headerCount*/,
    int32_t /*bodyMode*/, const uint8_t* /*body*/, int32_t /*bodyLen*/,
    int32_t /*httpVersion*/, int32_t /*versionPolicy*/,
    int32_t /*maxIdleSeconds*/, int32_t /*maxLifetimeSeconds*/,
    int32_t /*maxConnsPerServer*/, int32_t /*multipleH2*/)
{
    return new Dn2CppHttp2Call();
}

int32_t dn2cpp_http2_call_write(Dn2CppHttp2Call*, const uint8_t*, int32_t)
{
    return 0;
}

int32_t dn2cpp_http2_call_await_send_drain(Dn2CppHttp2Call*)
{
    return 0;
}

void dn2cpp_http2_call_finish_send(Dn2CppHttp2Call*)
{
}

int32_t dn2cpp_http2_call_wait_headers(Dn2CppHttp2Call*)
{
    return 0;
}

int32_t dn2cpp_http2_call_status(Dn2CppHttp2Call*)
{
    return 0;
}

int32_t dn2cpp_http2_call_http_version(Dn2CppHttp2Call*)
{
    return 0;
}

const char* dn2cpp_http2_call_reason(Dn2CppHttp2Call*)
{
    return nullptr;
}

const char* dn2cpp_http2_call_error(Dn2CppHttp2Call* c)
{
    return c != nullptr ? Dn2CppHttp2Call::kNoTransport : nullptr;
}

int32_t dn2cpp_http2_call_header_count(Dn2CppHttp2Call*)
{
    return 0;
}

const char* dn2cpp_http2_call_header_name(Dn2CppHttp2Call*, int32_t)
{
    return nullptr;
}

const char* dn2cpp_http2_call_header_value(Dn2CppHttp2Call*, int32_t)
{
    return nullptr;
}

int32_t dn2cpp_http2_call_read(Dn2CppHttp2Call*, uint8_t*, int32_t)
{
    return -1;
}

int32_t dn2cpp_http2_call_trailer_count(Dn2CppHttp2Call*)
{
    return 0;
}

const char* dn2cpp_http2_call_trailer_name(Dn2CppHttp2Call*, int32_t)
{
    return nullptr;
}

const char* dn2cpp_http2_call_trailer_value(Dn2CppHttp2Call*, int32_t)
{
    return nullptr;
}

void dn2cpp_http2_call_abort(Dn2CppHttp2Call*)
{
}

void dn2cpp_http2_call_free(Dn2CppHttp2Call* c)
{
    delete c;
}

} // extern "C"

#endif // DN2CPP_USE_CURL / __EMSCRIPTEN__

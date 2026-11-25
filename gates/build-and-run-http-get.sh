#!/usr/bin/env bash
# Live HTTP/TLS uses one HttpTransport image. Each driver runs in its own
# process against a loopback server. GET, methods/headers/status and the
# untrusted-certificate response are compared byte-for-byte with real .NET.
# The trusted case uses a throwaway CA: macOS .NET uses the system trust store,
# so the oracle is a fixed response plus the server's negotiated TLS handshake.
#
# DN2CPP_HTTP_FULL=1 loads DnHttp from beside the CLI for the live requests.
# It also checks the explicit shim in --measure and an isolated certificate-only
# image, and compiles the same HTTP program against the curl-less runtime to
# verify the missing transport symbol at link time.
source "$(dirname "$0")/_common.sh"

case "${DN2CPP_HTTP_FULL:-0}" in
    0|1) DN2CPP_HTTP_FULL=${DN2CPP_HTTP_FULL:-0} ;;
    *) echo 'DN2CPP_HTTP_FULL must be 0 or 1' >&2; exit 1 ;;
esac
DN2CPP_GATE_EXTRA_CONTEXT="${DN2CPP_GATE_EXTRA_CONTEXT:-}|http-full:$DN2CPP_HTTP_FULL"
if [ "$DN2CPP_HTTP_FULL" = 1 ]; then
    echo 'HTTP coverage: full transport, certificate and build-option matrix'
else
    echo 'HTTP coverage: shared image for live GET, methods/headers/status and trusted/untrusted TLS'
    echo 'HTTP measure, certificate-only image, default reference and curl-less link: skipped (not selected); not counted as passed'
fi

echo "== 0/16 CA-bundle freshness =="
# Ahead of every cached arm, unconditionally: this check is keyed on the CLOCK,
# not on any input the gate cache hashes, so it must run live on every
# invocation — a fully-warm cached-green run included — and its GATE-WARNING
# line (if due) is what run-all-gates.sh surfaces in the suite summary. This
# gate is the check's natural owner: sections 13-15 are the suite's only
# asserts that the embedded bundle actually verifies a TLS handshake, and a
# stale bundle degrades exactly that surface — months later, against one real
# host, on a user's machine, where no gate can see it.
cacert_staleness_warn

echo "== 1/16 Locating the real pinned CoreLib + System.Net.Http =="
corelib=$(resolve_pinned_corelib)
bcl=$(dirname "$corelib")
nethttp="$bcl/System.Net.Http.dll"
echo "corlib:   $corelib"
echo "net.http: $nethttp"
[ -f "$nethttp" ] || { echo "error: System.Net.Http.dll not found: $nethttp" >&2; exit 1; }

echo "== 2/16 Building the DnHttp shim + the samples =="
build_proj internal/DnHttp/src/DnHttp/DnHttp.csproj
dnhttp="internal/DnHttp/src/DnHttp/bin/$CONFIG/$TFM/DnHttp.dll"
[ -f "$dnhttp" ] || { echo "error: not built: $dnhttp" >&2; exit 1; }
build_proj samples/dotnet/HttpTransport/HttpTransport.csproj
app="samples/dotnet/HttpTransport/bin/$CONFIG/$TFM/HttpTransport.dll"
[ -f "$app" ] || { echo "error: not built: $app" >&2; exit 1; }

if [ "$DN2CPP_HTTP_FULL" = 1 ]; then
build_proj samples/dotnet/HttpClientCert/HttpClientCert.csproj
certapp="samples/dotnet/HttpClientCert/bin/$CONFIG/$TFM/HttpClientCert.dll"
[ -f "$certapp" ] || { echo "error: not built: $certapp" >&2; exit 1; }

echo "== 3/16 Measuring the HTTP transport with an explicit DnHttp reference (--measure) =="
out="artifacts/httptransport-measure"
rm -rf "$out"; mkdir -p "$out"
# --measure never aborts on a gap: it records each and keeps draining. A NON-ZERO exit
# here is a harness failure (e.g. an instantiation bound), not a per-body gap.
# Keep invoke_cli in this shell so its built CLI path is reused by later arms.
invoke_cli "$app" -r "$corelib" -r "$nethttp" -r "$dnhttp" --auto-ref --measure -o "$out" \
    > "$out/measure.log"
cat "$out/measure.log"
gaps="$out/s0-gaps.tsv"
[ -f "$gaps" ] || { echo "FAIL: --measure produced no gap report ($gaps)" >&2; exit 1; }

echo "== 4/16 Asserting the transport subtree was cut =="
# (a) No socket/DNS/TLS-connection/QUIC symbol may appear anywhere in the gap set (failing
#     method OR reach chain). Here-string, not a pipe, so a grep match does not SIGPIPE the
#     producer. Unrelated BCL gaps are checked separately below.
transport_re='System\.Net\.Sockets|System\.Net\.Quic|System\.Net\.NameResolution|System\.Net\.Dns|SslStream|NetworkStream|HttpConnectionPool|SocketsHttpHandler\.(SetupHandlerChain|Send[A-Za-z]*Core|Dispose[A-Za-z]+)|::ConnectAsync|::ConnectCallback|SystemNative_(Socket|Connect|GetHostEntry)|CryptoNative_Ssl'
if grep -qE "$transport_re" "$gaps"; then
    echo "FAIL: a transport-subtree symbol appears in the gap set — the intercept cut regressed" >&2
    head >&2 <<<"$(grep -E "$transport_re" "$gaps")"
    exit 1
fi
echo "OK: zero socket/DNS/TLS-connection/QUIC gaps — the transport subtree is cut for both HttpClient paths"

# (b) The three intercepted overrides must not be *failing* methods (TSV column 3): they
#     routed to the DnHttp shim body, so they compiled rather than dragging in the transport.
if awk -F'\t' '$3 ~ /SocketsHttpHandler\.(Send|SendAsync|Dispose)$/ { found=1 } END { exit !found }' "$gaps"; then
    echo "FAIL: an intercepted SocketsHttpHandler override is itself a gap — the shim route did not apply" >&2
    awk -F'\t' '$3 ~ /SocketsHttpHandler/' "$gaps" >&2
    exit 1
fi
echo "OK: SocketsHttpHandler.Send/SendAsync/Dispose routed to the DnHttp shim (not gaps)"

# (c) A floor on coverage so the assertion cannot pass vacuously: the sample must have
#     driven a real transpile (thousands of bodies), and the gap set must be the small
#     generic-BCL tail, not an early abort that happened to name nothing transport.
attempted="$(LC_ALL=C sed -n 's/.*--measure: \([0-9][0-9]*\) compile-phase bodies attempted.*/\1/p' "$out/measure.log")"
attempted="${attempted%%$'\n'*}"
# grep -c prints "0" AND exits 1 on zero matches, so `|| echo 0` would append a
# second line and turn the -gt test below into a silent shell error (exit 2 = false).
gapcount="$(grep -c . "$gaps" 2>/dev/null || true)"
gapcount="${gapcount:-0}"
echo "measured: ${attempted:-?} bodies attempted, $gapcount gaps"
if [ "${attempted:-0}" -lt 1000 ]; then
    echo "FAIL: only ${attempted:-0} bodies attempted — the transpile did not reach the HTTP surface" >&2
    exit 1
fi
if [ "$gapcount" -gt 40 ]; then
    echo "FAIL: $gapcount gaps — far more than the known generic-BCL tail; investigate before trusting the cut assertion" >&2
    exit 1
fi
echo "OK: full transpile reached ($attempted bodies), gap tail bounded ($gapcount)"

echo "== 5/16 Asserting the client-certificate subtree was cut =="
# (c) The twin of (a) for the certificate intercept. HttpReal's `new HttpClient()` runs
#     HttpClientHandler..ctor -> set_ClientCertificateOptions, which is the ONLY thing that
#     ever reached this subtree here — nothing in the sample mentions a certificate. So one
#     of these symbols in the gap set means the setter's real IL is being scanned again.
cert_re='X509|Asn1|AsnDecoder|AsnWriter|AsnReader|BigInteger|Keychain|CertificateHelper|AppleCertificatePal|StorePal|System\.Security\.Cryptography'
if grep -qE "$cert_re" "$gaps"; then
    echo "FAIL: a client-certificate-subtree symbol appears in the gap set — the cut regressed" >&2
    head >&2 <<<"$(grep -E "$cert_re" "$gaps")"
    exit 1
fi
echo "OK: zero X509/keychain/ASN.1/BigInteger gaps — the client-certificate subtree is cut"

# (c') The intercepted setter must not be a *failing* method: it routed to the guard body.
if awk -F'\t' '$3 ~ /HttpClientHandler\.set_ClientCertificateOptions$/ { found=1 } END { exit !found }' "$gaps"; then
    echo "FAIL: set_ClientCertificateOptions is itself a gap — the guard route did not apply" >&2
    awk -F'\t' '$3 ~ /HttpClientHandler/' "$gaps" >&2
    exit 1
fi
echo "OK: HttpClientHandler.set_ClientCertificateOptions routed to the guard body (not a gap)"

echo "== 6/16 Transpiling + LINKING + RUNNING HttpClientCert =="
# This root constructs a handler without sending a request. Its object symbols
# expose any certificate implementation retained by the constructor alone.
# The binary then RUNS, and its whole stdout is diffed against a FROZEN block rather than
# against `dotnet $certapp` — the program's last two lines deliberately diverge from real
# .NET (the Automatic refusal; see the comment in the sample), and a prefix-only oracle
# diff would skip exactly the lines this section exists to pin. What the frozen block
# asserts that linking alone cannot: the ClientCertificateOptions ROUND-TRIP. The
# replaced setter stores nothing and the real getter chain bottoms out in
# HttpConnectionSettings._clientCertificateOptions, whose initializer is Automatic — only
# the real setter's dropped forward ever wrote Manual there — so before the getter was
# intercepted (constant Manual, the only value the setter accepts), every read below
# answered Automatic where real .NET answers Manual, and this gate linked the sample
# without ever running it.
certout="artifacts/httpclientcert"
rm -rf "$certout"; mkdir -p "$certout"
invoke_cli "$certapp" -r "$corelib" -r "$nethttp" -r "$dnhttp" --auto-ref -o "$certout"
# The certificate-only root has its own reachability and cache entry.
cert_hit=0
if gate_cache_check "$certout" "http-get-cert|full:$DN2CPP_HTTP_FULL|$corelib" \
        "$certapp" "$dnhttp" "${certapp%.dll}.runtimeconfig.json" "${certapp%.dll}.deps.json"; then
    gate_cache_hit_msg
    cert_hit=1
fi
if [ "$cert_hit" = 0 ]; then
compile_console "$certout" httpclientcert
echo "OK: new HttpClient() / new HttpClientHandler() transpiles and links"

# The frozen run (see the section comment for why frozen, not oracle-diffed). Real .NET's
# output differs on the last two lines by design: it ACCEPTS Automatic ("Automatic =
# accepted" / "after Automatic = Automatic"), because it really can select a client
# certificate out of the system store on the next handshake.
cat > "$certout/run.expected" <<'CERTEOF'
ctor default = Manual
explicit Manual = Manual
default HttpClient built
Automatic = refused (names the remedy)
after Automatic = Manual
CERTEOF
gate_capture_ok "$certout/run" run_bounded "$certout/httpclientcert$EXE_EXT"
if ! gate_output compare "$certout/run.stdout" "$certout/run.expected" --policy crlf; then
    echo "FAIL: HttpClientCert output diverged from the frozen block — the ClientCertificateOptions round-trip or the Automatic refusal regressed" >&2
    exit 1
fi
echo "OK: ClientCertificateOptions round-trips (Manual) and Automatic is refused naming the remedy"

echo "== 7/16 object symbols: no certificate-subtree BODY is emitted or named =="
# The gap report says those bodies were not compiled; the object files' own symbol tables say
# they neither define them nor ask the linker for them (nm on Unix, dumpbin //SYMBOLS on Windows
# — dump_object_symbols_defined/undefined in _common.sh normalize both into one plain-name-per-
# line shape so the SAME pattern below matches either).
#
# BODIES, not "any X509 symbol" — the distinction is the whole point and getting it wrong makes
# this assertion false. The cut deletes method bodies; it does not (and must not) delete TYPES.
# t_/ti_ symbols for X509Certificate, X509Chain, LocalCertificateSelectionCallback and friends
# are still there, because reachable signatures name those types and a named type needs its
# shape — and delegates are kept whatever module they come from (AGENTS.md). What must be gone
# is X509Store.Open, the keychain walk, the ASN.1 decoder and BigInteger's arithmetic.
#
# Readable method symbols deliberately omit namespaces. Classify security code
# with each generated body's full-name comment so unrelated simple type names
# and parameter types cannot produce false positives.
cert_body='^// (System\.Security\.Cryptography|System\.Formats\.Asn1|System\.Numerics\.BigInteger|System\.Net\.Security\.CertificateHelper|AppleKeychainStore|Interop\.AppleCrypto)'
# A kept type's cctor first-use guard is permitted; it carries no method-body
# comment. A reached cctor body does carry one and remains visible below.
objs=$(find_generated_objects "$certout/.cmake")
[ -n "$objs" ] || { echo "FAIL: no generated object files found under $certout/.cmake" >&2; exit 1; }
defined="$certout/defined-syms.txt"
undefined="$certout/undefined-syms.txt"
dump_object_symbols_defined "$certout/.cmake" > "$defined"
dump_object_symbols_undefined "$certout/.cmake" > "$undefined"
# Non-vacuity FIRST: if the object files carry no visible symbols at all (a stripped artifact,
# changed mangling, the wrong file), every absence check below would pass while proving nothing.
# Note the final EXECUTABLE is useless for this — a release link strips it to undefined imports
# only, which is why this reads the objects.
bodies=$(grep -cE '^_?[A-Za-z_][A-Za-z0-9_]*_m[0-9]+' "$defined" || true)
[ "${bodies:-0}" -gt 50 ] || {
    echo "FAIL: only ${bodies:-0} transpiled bodies visible in the objects — the checks below would be vacuous" >&2
    exit 1; }
grep -qE '^_?HttpClientHandler_set_ClientCertificateOptions_m[0-9]+' "$defined" || {
    echo "FAIL: the intercepted setter is not defined in the objects — the route did not emit it" >&2
    exit 1; }
echo "OK: $bodies transpiled bodies visible, including the intercepted setter"
# Captured, then tested for emptiness — never `… | grep -q .`. Under pipefail a
# trailing `grep -q` exits on its FIRST match, the upstream grep dies of SIGPIPE,
# and 141 becomes the pipeline's status: the `if` reads FALSE and the FAIL branch
# is skipped. It bites once the middle grep has more than a pipe buffer of lines
# to write — i.e. exactly the wholesale "the cut regressed and the whole
# X509/ASN.1/BigInteger subtree came back" case this section exists to catch
# (measured to invert at ~200k lines). This file's own comments name the
# hazard three times and the curl-less arm below already avoids it this way.
cert_hits=$(grep -hE "$cert_body" "$certout"/generated*.cpp | grep -vE '::\.cctor$' || true)
if [ -n "$cert_hits" ]; then
    echo "FAIL: a certificate-subtree body is present in generated C++ — the cut regressed" >&2
    printf '%s\n' "$cert_hits" >&2
    exit 1
fi
# The successful native link above is the dangling-reference backstop. Namespace
# attribution comes from source comments because symbols no longer carry it.
echo "OK: zero certificate-subtree bodies generated; native link has no dangling calls"
gate_cache_commit
fi   # cert arm

fi   # full measure and certificate checks

echo "== 8/16 Building the HTTP transport image =="
realout="artifacts/httptransport-native"
rm -rf "$realout"; mkdir -p "$realout"
refs=(-r "$corelib" -r "$nethttp")
if [ "$DN2CPP_HTTP_FULL" = 1 ]; then
    # The measure pass prepared the CLI. Its installed sibling supplies the
    # transport for every live request in this profile.
    dnhttp="$(dirname "$DN2CPP_CLI_DLL")/DnHttp.dll"
    [ -f "$dnhttp" ] || { echo "FAIL: missing default reference $dnhttp" >&2; exit 1; }
    shim_source=default
else
    refs+=(-r "$dnhttp")
    shim_source=explicit
fi
invoke_cli "$app" "${refs[@]}" --auto-ref -o "$realout"
if gate_cache_check "$realout" "http-transport|full:$DN2CPP_HTTP_FULL|dnhttp:$shim_source|$corelib" \
        "$app" "$dnhttp" "${app%.dll}.runtimeconfig.json" "${app%.dll}.deps.json"; then
    gate_cache_hit_msg
    echo OK
    exit 0
fi
compile_console "$realout" httptransport
native="$realout/httptransport$EXE_EXT"
[ -x "$native" ] || { echo "FAIL: HTTP native binary was not built" >&2; exit 1; }

echo "== 9/16 Live GET: native libcurl vs real .NET SocketsHttpHandler, byte for byte =="
# A local http.server on an ephemeral free port serves one fixed file. Both the native binary
# and the CLR GET driver fetch it and print only response-derived, deterministic lines (status
# / length / body / content-type — no Date/Server header, no client IP). Real .NET runs the
# actual SocketsHttpHandler; the native binary runs the intercepted libcurl transport. The two
# outputs must be identical. HTTP-only on purpose — the scheme is the one variable this
# section does not change; https:// over this same transport is sections 13-15.
webroot="$(mktemp -d)"
printf 'Hello from dn2cpp HttpClient!\nsecond line\n' > "$webroot/hello.txt"
# Ephemeral port: bind :0, read the kernel's pick, release, hand it to http.server. A fixed
# port would flake when two gates (or a stray process) collide.
httpport="$(gate_python -c 'import socket; s=socket.socket(); s.bind(("127.0.0.1",0)); print(s.getsockname()[1]); s.close()')"
(gate_python_exec -m http.server "$httpport" --bind 127.0.0.1 --directory "$webroot" >/dev/null 2>&1) &
httppid=$!
disown "$httppid" 2>/dev/null || true  # keep bash from printing a "Terminated" line when the trap kills it
# `|| true` on every one of this gate's cleanup traps: under `set -e` a failing
# command inside an EXIT trap exits the shell with ITS status, so a scratch dir
# the killed server has not let go of yet would report a fully green run as red.
trap 'rc=$?; kill "$httppid" 2>/dev/null || true; if [ "$rc" -eq 0 ]; then rm -rf "$webroot" || true; else printf "HTTP evidence retained: %s\n" "$webroot" >&2; fi' EXIT
# Deterministic readiness wait (no fixed sleep): poll until the server accepts a request.
httpready=
for _ in $(seq 1 100); do
    if gate_python -c "import urllib.request,sys; urllib.request.urlopen('http://127.0.0.1:$httpport/hello.txt',timeout=1)" 2>/dev/null; then
        httpready=1; break
    fi
    sleep 0.1
done
[ -n "$httpready" ] || { echo "FAIL: local http.server never came up on port $httpport" >&2; exit 1; }
base="http://127.0.0.1:$httpport"
gate_capture_ok "$realout/native" run_bounded "$native" get "$base" || {
    echo "FAIL: native HttpReal exited non-zero:" >&2; cat "$realout/native.stderr" >&2; exit 1; }
gate_capture_ok "$realout/oracle" run_bounded dotnet "$app" get "$base" || {
    echo "FAIL: real .NET HttpReal exited non-zero" >&2; exit 1; }
# Non-vacuity floor: the native run really performed the GET (a 200 carrying the body), so the
# diff below is comparing real output, not two empty streams.
gate_output byte-witness "$realout/native.stdout" 'default status=200' --policy crlf || {
    echo "FAIL: native output is missing the GET result — the request did not go through" >&2
    cat "$realout/native.stdout" >&2; exit 1; }
if ! gate_output compare "$realout/native.stdout" "$realout/oracle.stdout"; then
    echo "FAIL: native libcurl GET output differs from real .NET" >&2; exit 1
fi
echo "OK: native libcurl GET is byte-identical to real .NET ($(grep -c . "$realout/native.stdout") lines)"

echo "== 10/16 object symbols: the libcurl transport is live, the .NET socket/DNS/TLS path is gone =="
# The stripped executable shows only undefined imports, so read the OBJECT files (as section 7
# does), through the same dump_object_symbols_defined/undefined pair. Two facts: (1)
# dn2cpp_http2_call_open — the curl transport's one entry point, which every request takes
# whatever its body shape — is DEFINED in the runtime and REFERENCED by the app;
# (2) NOT ONE emitted body names the .NET socket/DNS/TLS-connection subtree the transport
# intercept cuts. libcurl's OWN raw socket()/getaddrinfo are libc calls, not the .NET
# SystemNative_*/CryptoNative_* PAL, so they are irrelevant and not matched here.
realobjs=$(find_generated_objects "$realout/.cmake")
[ -n "$realobjs" ] || { echo "FAIL: no HttpReal object files found under $realout/.cmake" >&2; exit 1; }
# The curl-enabled runtime's OWN build dir is never a bare "artifacts/.cmake-runtime-curl": when
# CMAKE_CXX_COMPILER is set (the MSVC opt-in — see ensure_msvc_env/ensure_cmake_runtime), that dir
# gets a "-$(basename compiler)" suffix (e.g. "-cl") so it never collides with a clang/gcc build
# of the same axis. Re-deriving it through ensure_cmake_runtime (rather than hardcoding the
# no-suffix name) is what makes this portable; it does not rebuild — section 8 already built this
# exact runtime, and ninja no-ops on an unchanged tree.
curlruntimedir="$(dirname "$(ensure_cmake_runtime)")"
curlobjs=$(find_generated_objects "$curlruntimedir" dn2cpp_http2_stream)
[ -n "$curlobjs" ] || { echo "FAIL: the libcurl transport object (dn2cpp_http2_stream.cpp.o) was not built" >&2; exit 1; }
# (1a) defined in the transport object.
grep -qE '^_?dn2cpp_http2_call_open$' <<<"$(dump_object_symbols_defined "$curlruntimedir" dn2cpp_http2_stream)" || {
    echo "FAIL: dn2cpp_http2_call_open is not defined in the transport object — the curl transport did not compile" >&2
    exit 1; }
# (1b) referenced (undefined) by the app. dump_object_symbols_undefined reads each object file
# SEPARATELY and concatenates (never batched: nm given several object files on one command line
# has, in this exact use, silently resolved a cross-object reference away, and macOS `nm -u`
# drops the P/Invoke asm-alias symbol on top of that — the reason a naive `nm $objs | grep`
# once reported a clean-looking miss here).
real_defined="$realout/app-defined-syms.txt"
real_undefined="$realout/app-undefined-syms.txt"
dump_object_symbols_defined "$realout/.cmake" > "$real_defined"
dump_object_symbols_undefined "$realout/.cmake" > "$real_undefined"
grep -qE '^_?dn2cpp_http2_call_open$' "$real_undefined" || {
    echo "FAIL: no app object references dn2cpp_http2_call_open — SocketsHttpHandler.Send did not route to the curl shim" >&2
    exit 1; }
echo "OK: dn2cpp_http2_call_open is defined in the transport and referenced by the app"
# (2) No CUT transport-internal body is emitted. Full-name body comments classify
# the namespace now that readable method symbols omit it. SocketsHttpHandler.Send itself is
# DELIBERATELY present: it is the intercepted entry point, routed to the DnHttp shim — hence
# Send[A-Za-z]*Core (SendCore / SendAsyncCore), not a bare Send. A kept
# socket/DNS/TLS type's cctor guard has no body comment and does not trip this check.
sock_body='^// (System\.Net\.Sockets|System\.Net\.Quic|System\.Net\.NameResolution|System\.Net\.Dns|System\.Net\.Security\.SslStream|System\.Net\.Http\.HttpConnectionPool|System\.Net\.Http\.SocketsHttpHandler::(SetupHandlerChain|Send[A-Za-z]+Core|Startup))'
# Captured, not `| grep -q .` — the SIGPIPE-under-pipefail inversion described in
# section 7 applies verbatim, and this is the gate's headline claim.
sock_hits=$(grep -hE "$sock_body" "$realout"/generated*.cpp | grep -vE '::\.cctor$' || true)
if [ -n "$sock_hits" ]; then
    echo "FAIL: a .NET socket/DNS/TLS transport body is emitted or named — the transport intercept regressed" >&2
    head >&2 <<<"$sock_hits"
    exit 1
fi
if grep -qE '^_?(SystemNative_(Socket|Connect|GetHostEntry|Accept|Bind)|CryptoNative_Ssl|CryptoNative_X509)' "$real_undefined"; then
    echo "FAIL: the app imports the .NET socket/TLS PAL — the transport intercept regressed" >&2
    exit 1
fi
echo "OK: zero .NET socket/DNS/TLS bodies or PAL imports — the libcurl transport is the sole path"

echo "== 11/16 Preparing HttpBeyondGet (POST/PUT/headers/multi-status) =="
beyondout="$realout/beyond"
mkdir -p "$beyondout"

echo "== 12/16 Beyond GET: POST/PUT/DELETE, headers, multi-status — native vs real .NET =="
# One local echo/status server serves both runs. /echo answers with the request's
# method, path, FULL sorted header set and body (as hex — binary-safe and printable),
# so anything a transport adds, drops or re-folds on the wire is a visible diff line:
# that is how curl's implicit Accept/Content-Type and the multi-value fold were found.
# The other endpoints exercise response-header readback and non-2xx statuses. Nothing
# nondeterministic reaches the output: Date/Server are suppressed server-side, the
# client never prints the base URL, and the one header carrying the ephemeral port
# (Host) is masked — masked, not dropped, so a transport that stops sending Host
# still fails the diff.
echodir="$(mktemp -d)"
cat > "$echodir/echo_server.py" <<'PYEOF'
import sys
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer

class Handler(BaseHTTPRequestHandler):
    protocol_version = "HTTP/1.1"

    # Determinism: the base class's send_response would add Date and Server
    # headers, both of which change run to run and would land in the client's
    # response-header printout — suppress them at the source.
    def send_response(self, code, message=None):
        self.send_response_only(code, message)

    def log_message(self, fmt, *args):
        pass

    # Chunked is not optional here: BaseHTTPRequestHandler does not de-chunk, so a
    # Content-Length-only read would leave the chunk framing in the socket and the NEXT
    # request on that keep-alive connection would parse garbage — the failure would land
    # on an innocent later section.
    def _read_body(self):
        if self.headers.get("Transfer-Encoding", "").lower() == "chunked":
            data = b""
            while True:
                size = int(self.rfile.readline().split(b";")[0], 16)
                if size == 0:
                    break
                data += self.rfile.read(size)
                self.rfile.read(2)  # the chunk's trailing CRLF
            while self.rfile.readline().strip():  # trailer section, usually empty
                pass
            return data
        n = int(self.headers.get("Content-Length", "0") or "0")
        return self.rfile.read(n) if n > 0 else b""

    def _reply(self, code, body=b"", extra=()):
        self.send_response(code)
        for k, v in extra:
            self.send_header(k, v)
        if code != 204:  # 204 has no body by definition, so no Content-Length
            self.send_header("Content-Length", str(len(body)))
        self.end_headers()
        # HEAD: the headers above (Content-Length included) describe the body the
        # matching GET would carry, and none of it is written — the framing the
        # client's HEAD sections stand on.
        if body and code != 204 and self.command != "HEAD":
            self.wfile.write(body)

    def _echo(self):
        body = self._read_body()
        lines = ["method=%s" % self.command, "path=%s" % self.path]
        hdrs = []
        for k, v in self.headers.items():
            k = k.lower()
            if k == "host":
                v = v.rsplit(":", 1)[0] + ":PORT"
            hdrs.append("%s: %s" % (k, v))
        for h in sorted(hdrs):
            lines.append("hdr " + h)
        lines.append("body-hex=" + body.hex())
        out = ("\n".join(lines) + "\n").encode()
        self._reply(200, out, [("Content-Type", "text/plain")])

    def _route(self):
        p = self.path
        if p == "/echo":
            self._echo()
        elif p == "/status/404":
            self._reply(404, b"nothing here\n", [("Content-Type", "text/plain")])
        elif p == "/status/500":
            self._reply(500, b"boom\n", [("Content-Type", "text/plain")])
        elif p == "/created":
            self._read_body()
            self._reply(201, b"created\n",
                        [("Location", "/things/42"), ("Content-Type", "text/plain")])
        elif p == "/nocontent":
            self._read_body()
            self._reply(204)
        elif p == "/resphdr":
            self._reply(200, b"resp-header-body\n", [
                ("X-Resp-One", "alpha"),
                ("X-Resp-Two", "beta gamma"),
                ("X-Resp-Multi", "m1"),
                ("X-Resp-Multi", "m2"),
                ("Content-Type", "text/x-dn2cpp; charset=utf-8"),
            ])
        else:
            self._reply(404, b"unknown path\n", [("Content-Type", "text/plain")])

    do_GET = do_POST = do_PUT = do_DELETE = do_HEAD = _route

# Bind :0 and report the kernel's pick on stdout: no free-port race, and the
# listener exists before the port is printed, so printing is itself readiness.
srv = ThreadingHTTPServer(("127.0.0.1", 0), Handler)
print(srv.server_address[1], flush=True)
srv.serve_forever()
PYEOF
(gate_python_exec -u "$echodir/echo_server.py" > "$echodir/port.txt" 2>"$echodir/server.err") &
echopid=$!
disown "$echopid" 2>/dev/null || true
# A second `trap ... EXIT` REPLACES section 9's — fold that cleanup in rather than
# losing it (section 9's file server is still running; both die here).
trap 'rc=$?; kill "$httppid" "$echopid" 2>/dev/null || true; if [ "$rc" -eq 0 ]; then rm -rf "$webroot" "$echodir" || true; else printf "HTTP evidence retained: %s\n" "$webroot" "$echodir" >&2; fi' EXIT
echoport=
for _ in $(seq 1 300); do
    [ -s "$echodir/port.txt" ] && { echoport="$(cat "$echodir/port.txt")"; break; }
    sleep 0.1
done
if [ -z "$echoport" ]; then
    echo "FAIL: the echo server never reported a port:" >&2
    [ -f "$echodir/server.err" ] && cat "$echodir/server.err" >&2
    exit 1
fi
# Belt-and-braces readiness: one real round-trip (same discipline as section 9).
echoready=
for _ in $(seq 1 100); do
    if gate_python -c "import urllib.request; urllib.request.urlopen('http://127.0.0.1:$echoport/resphdr', timeout=1)" 2>/dev/null; then
        echoready=1; break
    fi
    sleep 0.1
done
[ -n "$echoready" ] || { echo "FAIL: the echo server never answered on port $echoport" >&2; exit 1; }
ebase="http://127.0.0.1:$echoport"
gate_capture_ok "$beyondout/native" run_bounded "$native" beyond "$ebase" || {
    echo "FAIL: native HttpBeyondGet exited non-zero:" >&2; cat "$beyondout/native.stderr" >&2; exit 1; }
gate_capture_ok "$beyondout/oracle" run_bounded dotnet "$app" beyond "$ebase" || {
    echo "FAIL: real .NET HttpBeyondGet exited non-zero" >&2; exit 1; }
# Non-vacuity floors before the diff: the 2 KiB POST body really crossed the wire,
# and the multi-valued response header really read back as two values — so the diff
# below compares real traffic, not two empty streams.
gate_output byte-witness "$beyondout/native.stdout" 'post-binary | hdr content-length: 2000' --count any --policy crlf || {
    echo "FAIL: native output is missing the POST echo — the request did not go through" >&2
    cat "$beyondout/native.stdout" >&2; exit 1; }
gate_output byte-witness "$beyondout/native.stdout" 'resphdr msg-header X-Resp-Multi=m1|m2' --count any --policy crlf || {
    echo "FAIL: native output is missing the multi-value response-header readback" >&2
    cat "$beyondout/native.stdout" >&2; exit 1; }
# The unknown-length body really went out CHUNKED and really arrived whole. Named
# separately from the diff because both halves are load-bearing and neither is obvious
# from a matching pair of outputs: this is the suite's only SYNCHRONOUS unknown-length
# send, and a transport that closed the upload's send side late would not diff — it
# would hang until the gate's watchdog.
gate_output byte-witness "$beyondout/native.stdout" 'post-chunked | hdr transfer-encoding: chunked' --count any --policy crlf || {
    echo "FAIL: the unknown-length POST did not go out with a chunked framing" >&2
    cat "$beyondout/native.stdout" >&2; exit 1; }
# awk, not `grep -E '…{600}'`: POSIX caps an interval at RE_DUP_MAX (255), so the
# bounded-repetition form is not portable at this size and fails on a correct body.
# The regex view is separate from the raw native/oracle byte comparison.
gate_output view "$beyondout/native.stdout" "$beyondout/native.records" --policy crlf
awk -F'body-hex=' '/^post-chunked \| body-hex=/ { if (length($2) == 600) ok = 1 }
                   END { exit !ok }' "$beyondout/native.records" || {
    echo "FAIL: the chunked POST body did not arrive whole (300 bytes = 600 hex chars)" >&2
    cat "$beyondout/native.stdout" >&2; exit 1; }
# HEAD/zero-length floors. The zero-length POST really carried its framing (the echoed
# content-length: 0 is the SERVER's reading of the wire), and the HEAD really
# completed with an empty body — the pre-fix transport did not misframe a HEAD,
# it STALLED on one (waiting out the advertised Content-Length on a keep-alive
# connection), so this line existing at all is the regression's tripwire.
gate_output byte-witness "$beyondout/native.stdout" 'post-zerolen | hdr content-length: 0' --count any --policy crlf || {
    echo "FAIL: the zero-length POST went out without Content-Length: 0" >&2
    cat "$beyondout/native.stdout" >&2; exit 1; }
gate_output byte-witness "$beyondout/native.stdout" 'head len=0' --count any --policy crlf || {
    echo "FAIL: the HEAD did not complete with an empty body" >&2
    cat "$beyondout/native.stdout" >&2; exit 1; }
if ! gate_output compare "$beyondout/native.stdout" "$beyondout/oracle.stdout"; then
    echo "FAIL: native libcurl beyond-GET output differs from real .NET" >&2; exit 1
fi
echo "OK: POST/PUT/DELETE bodies, request headers, response headers and non-2xx statuses are byte-identical to real .NET ($(grep -c . "$beyondout/native.stdout") lines)"

echo "== 13/16 Checking the embedded TLS trust anchors =="
# The trust anchors, read at the LINKER's level rather than from the source. The embedded
# Mozilla bundle reaches the transport as two symbols the generated dn2cpp_ca_bundle.cpp
# defines and dn2cpp_http2_stream.cpp references — and it references them from exactly one
# place, the CURLOPT_CAINFO_BLOB setopt. So "undefined in the transport object" is not
# decoration: it is the one check that goes red if that setopt is ever dropped, a regression
# sections 14-15 cannot see (with no anchors at all the untrusted run still fails, for the
# wrong reason, and the trusted run still passes on its DN2CPP_HTTP_CAINFO override).
curlruntimedir="$(dirname "$(ensure_cmake_runtime)")"
# Captured, then matched from a here-string rather than through a pipe, for section 4's
# reason: `grep -q` stops at the first hit, and a producer whose remaining output has
# nowhere to go takes a SIGPIPE that `set -o pipefail` would turn into a gate failure.
cabundle_defined="$(dump_object_symbols_defined "$curlruntimedir" dn2cpp_ca_bundle)"
transport_undefined="$(dump_object_symbols_undefined "$curlruntimedir" dn2cpp_http2_stream)"
grep -qE '^_?dn2cpp_ca_bundle_pem$' <<<"$cabundle_defined" || {
    echo "FAIL: the embedded CA bundle is not defined in the runtime — the cacert.pem embed step did not run" >&2
    exit 1; }
grep -qE '^_?dn2cpp_ca_bundle_pem$' <<<"$transport_undefined" || {
    echo "FAIL: the transport does not reference the embedded CA bundle — CURLOPT_CAINFO_BLOB is no longer passed" >&2
    exit 1; }
# And a floor on what those bytes are: a bundle that had shrunk to a placeholder would still
# link and would still fail section 15 for a plausible-looking reason.
cacount="$(grep -c 'BEGIN CERTIFICATE' third_party/cacert/cacert.pem || true)"
[ "${cacount:-0}" -ge 100 ] || {
    echo "FAIL: third_party/cacert/cacert.pem holds only ${cacount:-0} certificates — that is not the Mozilla bundle" >&2
    exit 1; }
echo "OK: the embedded Mozilla bundle ($cacount anchors) is defined and referenced by the transport"

echo "== 14/16 Live HTTPS with the server's CA trusted: a real handshake, and the GET succeeds =="
# A throwaway CA and a 127.0.0.1 leaf, generated HERE — no private key is checked in, and the
# pair lives in a mktemp dir the trap removes. Two certificates rather than one self-signed
# leaf because Mbed TLS will only accept a trust anchor with basicConstraints CA:TRUE, and a
# leaf that carried that would be a different shape from anything real.
#
# openssl is driven through CONFIG FILES, not -addext: -addext is OpenSSL 1.1.1+/LibreSSL 3.1+
# only, and the openssl on a stock macOS is LibreSSL. Both forms of the tool were checked
# against these two files (Homebrew OpenSSL 3.6 and /usr/bin/openssl LibreSSL 3.3.6).
command -v openssl >/dev/null 2>&1 || {
    echo "FAIL: openssl not on PATH — sections 14-15 mint the TLS server's throwaway certificate with it" >&2
    exit 1; }
tlsdir="$(mktemp -d)"
tlspid=""
# Certificate generation can fail before the TLS server starts. Keep its
# diagnostics and stop the two HTTP servers on that path as well.
trap 'rc=$?; kill "$httppid" "$echopid" "$tlspid" 2>/dev/null || true; if [ "$rc" -eq 0 ]; then rm -rf "$webroot" "$echodir" "$tlsdir" || true; else printf "HTTP evidence retained: %s\n" "$webroot" "$echodir" "$tlsdir" >&2; fi' EXIT

cat > "$tlsdir/ca.cnf" <<'CAEOF'
[req]
distinguished_name = dn
prompt             = no
x509_extensions    = v3_ca
[dn]
CN = dn2cpp-gate-tls-ca
[v3_ca]
basicConstraints = critical,CA:TRUE
keyUsage         = critical,keyCertSign,cRLSign
subjectKeyIdentifier = hash
authorityKeyIdentifier = keyid:always
CAEOF
cat > "$tlsdir/srv.cnf" <<'SRVEOF'
[req]
distinguished_name = dn
prompt             = no
[dn]
CN = 127.0.0.1
[v3_srv]
basicConstraints = critical,CA:FALSE
keyUsage         = critical,digitalSignature,keyEncipherment
extendedKeyUsage = serverAuth
subjectAltName   = IP:127.0.0.1,DNS:localhost
subjectKeyIdentifier = hash
authorityKeyIdentifier = keyid:always
SRVEOF
# -days 2, not 1: a run that straddles midnight UTC must not be the thing that fails.
openssl req -x509 -newkey rsa:2048 -nodes -days 2 \
    -keyout "$tlsdir/ca.key" -out "$tlsdir/ca.pem" -config "$tlsdir/ca.cnf" >"$tlsdir/openssl.log" 2>&1 &&
openssl req -newkey rsa:2048 -nodes \
    -keyout "$tlsdir/srv.key" -out "$tlsdir/srv.csr" -config "$tlsdir/srv.cnf" >>"$tlsdir/openssl.log" 2>&1 &&
# -set_serial rather than -CAcreateserial: no .srl file to leave behind, and one fewer
# behaviour that differs between OpenSSL and LibreSSL.
openssl x509 -req -in "$tlsdir/srv.csr" -CA "$tlsdir/ca.pem" -CAkey "$tlsdir/ca.key" \
    -set_serial 1 -days 2 -extfile "$tlsdir/srv.cnf" -extensions v3_srv \
    -out "$tlsdir/srv.pem" >>"$tlsdir/openssl.log" 2>&1 || {
    echo "FAIL: could not generate the throwaway TLS certificate:" >&2; cat "$tlsdir/openssl.log" >&2; exit 1; }
# Newer TLS verifiers require the issuer key identifier even on this short chain.
for cert in "$tlsdir/ca.pem" "$tlsdir/srv.pem"; do
    cert_extensions="$(openssl x509 -in "$cert" -noout -text)" || {
        echo "FAIL: could not inspect the throwaway TLS certificate: $cert" >&2; exit 1; }
    grep -q 'Subject Key Identifier' <<<"$cert_extensions" &&
        grep -q 'Authority Key Identifier' <<<"$cert_extensions" || {
        echo "FAIL: the throwaway TLS certificate lacks key identifiers: $cert" >&2; exit 1; }
done
cat "$tlsdir/srv.pem" "$tlsdir/srv.key" > "$tlsdir/srv-chain.pem"
# The CA as its two NATIVE readers need it. MSYS rewrites a path that is a whole
# argument and nothing else, so mktemp's /tmp/... reaches a `python -c` string and
# an environment variable unconverted — and both are read by a native program.
tlsca="$(native_path "$tlsdir/ca.pem")"

cat > "$tlsdir/tls_server.py" <<'PYEOF'
import os
import ssl
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer

here = os.path.dirname(os.path.abspath(__file__))
# Line-buffered append: the gate reads this file while the server is still running,
# so a line must be visible the instant it is written.
log = open(os.path.join(here, "tls.log"), "a", buffering=1)


class Handler(BaseHTTPRequestHandler):
    protocol_version = "HTTP/1.1"

    # Determinism, as in section 12's echo server: the base class's send_response
    # would add Date and Server headers, both of which change run to run.
    def send_response(self, code, message=None):
        self.send_response_only(code, message)

    def log_message(self, fmt, *args):
        pass

    def do_GET(self):
        body = b"Hello over TLS from dn2cpp!\nsecond line\n"
        # The handshake evidence, recorded from the SERVER end. self.connection is
        # the accepted SSLSocket, so version()/cipher() are the parameters actually
        # negotiated for the connection that carried this request — facts no client
        # can produce without completing a handshake.
        log.write("served path=%s tls=%s cipher=%s\n"
                  % (self.path, self.connection.version(), self.connection.cipher()[0]))
        self.send_response(200)
        self.send_header("Content-Type", "text/plain")
        self.send_header("Content-Length", str(len(body)))
        self.end_headers()
        self.wfile.write(body)


class TlsServer(ThreadingHTTPServer):
    # The listening socket is TLS-wrapped, so the handshake happens inside accept()
    # and a client that rejects our certificate surfaces HERE, not in a handler.
    # ssl.SSLError is an OSError, which socketserver treats as "connection went
    # away" and swallows — the server stays up, which is what lets one server
    # serve both the trusted and the untrusted run. Logging it first is what turns
    # a swallowed error into the gate's evidence that the client hung up on us.
    def get_request(self):
        try:
            return super().get_request()
        except ssl.SSLError as e:
            log.write("handshake-failed %s\n" % (e.reason or type(e).__name__,))
            raise


ctx = ssl.SSLContext(ssl.PROTOCOL_TLS_SERVER)
ctx.load_cert_chain(os.path.join(here, "srv-chain.pem"))
srv = TlsServer(("127.0.0.1", 0), Handler)
srv.socket = ctx.wrap_socket(srv.socket, server_side=True)
# Bind :0 and report the kernel's pick on stdout, exactly as section 12's server:
# no free-port race, and the listener exists before the port is printed.
print(srv.server_address[1], flush=True)
srv.serve_forever()
PYEOF
(gate_python_exec -u "$tlsdir/tls_server.py" > "$tlsdir/port.txt" 2>"$tlsdir/server.err") &
tlspid=$!
disown "$tlspid" 2>/dev/null || true
tlsport=
for _ in $(seq 1 300); do
    [ -s "$tlsdir/port.txt" ] && { tlsport="$(cat "$tlsdir/port.txt")"; break; }
    sleep 0.1
done
if [ -z "$tlsport" ]; then
    echo "FAIL: the TLS server never reported a port:" >&2
    [ -f "$tlsdir/server.err" ] && cat "$tlsdir/server.err" >&2
    exit 1
fi
# Readiness by one real TLS round-trip, verifying against the same CA — so a certificate the
# server cannot load, or a python without TLS, fails here with its own message rather than
# masquerading as a dn2cpp verification failure in section 15.
tlsready=
for _ in $(seq 1 100); do
    if gate_python -c "import ssl, urllib.request; ctx = ssl.create_default_context(cafile='$tlsca'); urllib.request.urlopen('https://127.0.0.1:$tlsport/hello.txt', context=ctx, timeout=1)" 2>"$tlsdir/readiness.err"; then
        tlsready=1; break
    fi
    sleep 0.1
done
[ -n "$tlsready" ] || {
    echo "FAIL: the local TLS server never completed a handshake on port $tlsport" >&2
    cat "$tlsdir/readiness.err" >&2
    cat "$tlsdir/server.err" >&2; exit 1; }
# Drop the readiness probe's own log line: from here on every line in tls.log was caused by
# the binary under test, so the counts below mean what they say.
: > "$tlsdir/tls.log"
tbase="https://127.0.0.1:$tlsport"

gate_capture_ok "$tlsdir/trusted" run_bounded env DN2CPP_HTTP_CAINFO="$tlsca" \
    "$native" tls "$tbase" || {
    echo "FAIL: native HttpsTls exited non-zero on the trusted run:" >&2
    cat "$tlsdir/trusted.stderr" >&2; exit 1; }
# The server is the gate's own and its reply is fixed, so the expected stdout is known
# exactly — a literal block, not an oracle (see the head comment on why real .NET cannot be
# one here). Every line is response-derived: status, byte count, body, media type.
cat > "$tlsdir/trusted.expected" <<'EXPEOF'
tls outcome=ok
tls status=200
tls len=40
tls body=Hello over TLS from dn2cpp!
second line

tls ctype=text/plain
tls alive=1
EXPEOF
if ! gate_output compare "$tlsdir/trusted.stdout" "$tlsdir/trusted.expected" --policy crlf; then
    echo "FAIL: the trusted-anchor HTTPS GET did not produce the expected response" >&2
    cat "$tlsdir/trusted.stderr" >&2; exit 1
fi
# The handshake itself, from the server's end. A check that only read the client's output
# would pass on a transport that never negotiated anything — which is precisely the shape a
# "does the binary contain TLS symbols?" test has, and why this reads the server instead.
served="$(grep -c '^served ' "$tlsdir/tls.log" || true)"
[ "${served:-0}" = 1 ] || {
    echo "FAIL: the TLS server served ${served:-0} requests during the trusted run, expected exactly 1" >&2
    cat "$tlsdir/tls.log" >&2; exit 1; }
# Match raw server records with exactly one explicit CRLF policy application.
http_tls_served_record() {
    gate_output match-record "$1" '^served path=/hello\.txt tls=TLSv1\.[23] cipher=[A-Z0-9_-]+$' --count 1 --policy crlf
}
http_tls_served_record "$tlsdir/tls.log" || {
    echo "FAIL: the served request did not arrive over a negotiated TLS 1.2/1.3 connection" >&2
    cat "$tlsdir/tls.log" >&2; exit 1; }
echo "OK: trusted-anchor HTTPS GET succeeded over $(sed -n 's/^served .*tls=\([^ ]*\) cipher=\(.*\)$/\1 \2/p' "$tlsdir/tls.log")"

echo "== 15/16 Untrusted certificate: a catchable exception that names the cause, and .NET agrees =="
# THE MOST VALUABLE ASSERT IN THIS GATE. The same binary, the same server, the same URL — the
# only change is that DN2CPP_HTTP_CAINFO is unset, so the anchors are the embedded Mozilla
# bundle and this CA is not in it. The request MUST fail. A transport that quietly accepted an
# unverifiable certificate would pass every other section of this gate and every other gate in
# the suite: nothing else here ever presents one.
servedbefore="$(grep -c '^served ' "$tlsdir/tls.log" || true)"
gate_capture_ok "$tlsdir/untrusted" run_bounded "$native" tls "$tbase" || {
    echo "FAIL: native HttpsTls exited non-zero on the untrusted run — a rejected certificate must be" >&2
    echo "      a catchable HttpRequestException, not a dead process:" >&2
    cat "$tlsdir/untrusted.stderr" >&2; exit 1; }
gate_output witness "$tlsdir/untrusted.stdout" 'tls outcome=rejected' --policy crlf || {
    echo "FAIL: the GET against an untrusted certificate SUCCEEDED — verification is off" >&2
    cat "$tlsdir/untrusted.stdout" >&2; exit 1; }
# "The process is still alive" is not an inference from the exit code: the sample prints this
# line after the catch block, so it exists only if the throw was catchable at all.
gate_output witness "$tlsdir/untrusted.stdout" 'tls alive=1' --policy crlf || {
    echo "FAIL: the untrusted run did not reach the line after the catch — the failure was not catchable" >&2
    cat "$tlsdir/untrusted.stdout" >&2; exit 1; }
# The diagnosis. A bare "the request failed" would satisfy everything above and leave whoever
# hits this in a shipped game with nothing to go on, so the message must name the certificate
# AND say what is wrong with it. This is dn2cpp's own wording (Mbed TLS's, relayed by curl's
# error buffer — see dn2cpp_http2_stream.cpp on why the buffer and not curl_easy_strerror), which
# is why it is asserted on stderr and kept out of the diffed stdout.
grep -qi 'certificate' "$tlsdir/untrusted.stderr" && grep -qi 'trusted CA' "$tlsdir/untrusted.stderr" || {
    echo "FAIL: the transport's error does not name the certificate and the trust anchors:" >&2
    cat "$tlsdir/untrusted.stderr" >&2; exit 1; }
# Corroboration from the other end of the wire, and the tightest form of "verification ran":
# the server recorded a FAILED handshake and served nothing. The client can finish handling
# its alert before the server's accept thread catches and logs the peer's alert, so wait for
# that independent observation instead of racing it. With CURLOPT_SSL_VERIFYPEER at 0 the
# handshake would complete and this would be a `served` line instead.
handshake_failed=
for _ in $(seq 1 50); do
    if grep -q '^handshake-failed ' "$tlsdir/tls.log"; then
        handshake_failed=1
        break
    fi
    sleep 0.1
done
[ -n "$handshake_failed" ] || {
    echo "FAIL: the TLS server recorded no failed handshake — the client did not reject the certificate" >&2
    cat "$tlsdir/tls.log" >&2; exit 1; }
servedafter="$(grep -c '^served ' "$tlsdir/tls.log" || true)"
[ "${servedafter:-0}" = "${servedbefore:-0}" ] || {
    echo "FAIL: the TLS server served a request during the untrusted run — the handshake completed" >&2
    cat "$tlsdir/tls.log" >&2; exit 1; }
# The oracle, on the one thing it can speak to (head comment): real .NET does not trust this CA
# either, so the two transports must agree line for line on the shape of the refusal — a
# catchable HttpRequestException whose flattened cause names the certificate, and a live
# process after it. The wording differs and is deliberately not on stdout.
gate_capture_ok "$tlsdir/untrusted-oracle" run_bounded dotnet "$app" tls "$tbase" || {
    echo "FAIL: real .NET HttpsTls exited non-zero" >&2; exit 1; }
if ! gate_output compare "$tlsdir/untrusted.stdout" "$tlsdir/untrusted-oracle.stdout"; then
    echo "FAIL: the native refusal differs in shape from real .NET's" >&2; exit 1
fi
echo "OK: an untrusted certificate is a catchable HttpRequestException naming the cause, in the same shape real .NET reports ($(sed -n 's/^tls-cause: //p' "$tlsdir/untrusted.stderr"))"

if [ "$DN2CPP_HTTP_FULL" = 1 ]; then
echo "== 16/16 The opt-out: -DDN2CPP_USE_CURL=OFF still builds, and fails LOUDLY at link =="
# DN2CPP_USE_CURL is ON by default, so this is the only section that builds the other side of
# the option — and the only coverage the OFF configuration has anywhere in the suite. Before
# the default flipped, OFF was what every gate built, so anything that broke it (a stray
# unconditional reference to a dn2cpp_http_* symbol, a curl-only include leaking into a shared
# header, a runtime TU that stopped compiling without the define) went red across the board.
# The flip took that away in one line; this section is what replaces it.
#
# Two facts, and the second is a CONTRACT, not an incidental:
#   (1) the runtime still CONFIGURES AND BUILDS with the option off — curl's tree is never
#       entered, no libcurl.a is produced, and dn2cpp_http2_stream.cpp compiles to an object
#       that defines nothing (its arm-2 empty TU).
#   (2) a program that reaches the HTTP surface then fails at LINK, naming the undefined
#       transport symbol. That is the documented design (see the three-way split in
#       dn2cpp_http2_stream.cpp's header): somebody said -DDN2CPP_USE_CURL=OFF, so they declined
#       the transport, and a build that instead produced a binary whose every request fails at
#       run time would be answering a question they did not ask. The Web lane is the one place
#       that answers at run time instead, and it is a different question — the platform
#       declined, not the person configuring the build.
# A regression in (2) is silent in the only way that matters: the transpile is green, the link
# is green, and the failure surfaces in the shipped program.
# (1) The curl-less runtime builds. `|| { }` explicitly: a bare assignment does not abort under
# set -e, so a failed runtime build would otherwise sail on and be reported as the link error
# this section EXPECTS — the one confusion that would make a green here meaningless.
nocurlexport="$(export DN2CPP_NO_CURL=1; ensure_cmake_runtime)" || {
    echo "FAIL: the runtime does not build with -DDN2CPP_USE_CURL=OFF — the opt-out is broken" >&2
    exit 1; }
nocurlruntimedir="$(dirname "$nocurlexport")"
# The Windows interface-name lookup is a platform import, not an HTTP feature.
# libcurl also links iphlpapi, so the default runtime can hide a missing direct
# dependency; only this opt-out axis proves the runtime exports it on its own.
if [ "$DN2CPP_OS" = windows ] && ! grep -q 'iphlpapi' "$nocurlexport"; then
    echo "FAIL: the curl-less Windows runtime does not export iphlpapi to generated P/Invoke callers" >&2
    exit 1
fi
curl_archives=$(find "$nocurlruntimedir" -name 'libcurl*.a' -o -name 'libcurl*.lib')   # captured, not `| grep -q .` — see section 7
if [ -n "$curl_archives" ]; then
    echo "FAIL: a libcurl archive exists in the curl-less runtime build — the option was not obeyed" >&2
    printf '%s\n' "$curl_archives" >&2
    exit 1
fi
# nghttp2 rides the curl arm (runtime/CMakeLists.txt builds dn2cpp_nghttp2 inside
# if(DN2CPP_USE_CURL) — it exists only to give curl its HTTP/2 engine), so the opt-out
# must shed it exactly as it sheds curl: an archive here would mean the h2 engine grew
# a life of its own outside the option that justifies it.
nghttp2_archives=$(find "$nocurlruntimedir" \( -name '*nghttp2*.a' -o -name '*nghttp2*.lib' \))
if [ -n "$nghttp2_archives" ]; then
    echo "FAIL: an nghttp2 archive exists in the curl-less runtime build — the h2 engine must vanish with curl" >&2
    printf '%s\n' "$nghttp2_archives" >&2
    exit 1
fi
# The transport object is still compiled (the TU is unconditionally globbed) and must define
# nothing. Captured then matched from a here-string, for section 4's SIGPIPE reason.
nocurl_defined_h2="$(dump_object_symbols_defined "$nocurlruntimedir" dn2cpp_http2_stream || true)"
if grep -qE '^_?dn2cpp_http2_' <<<"$nocurl_defined_h2"; then
    echo "FAIL: the curl-less runtime DEFINES a dn2cpp_http2_call_* symbol — arm 2 of the TU's" >&2
    echo "      split is meant to be empty, and a definition here is a silent transport that never works" >&2
    exit 1
fi
echo "OK: the runtime configures and builds with the option off, carrying no curl and no transport"
# DN2CPP_USE_CURL changes the runtime target's public compile definitions.
# CMake uses the same emitted sources with that target in .cmake-nocurl.
if DN2CPP_NO_CURL=1 cmake_build_app "$realout" httptransport "" > "$realout/nocurl-link.log" 2>&1; then
    echo "FAIL: HttpTransport linked against a runtime built with -DDN2CPP_USE_CURL=OFF." >&2
    echo "      The undefined dn2cpp_http_* symbols are the opt-out's whole contract: a binary" >&2
    echo "      that links here is one whose HTTP fails only once it is running." >&2
    exit 1
fi
# Loud AND attributable: the failure has to name the transport symbol, not merely be a failure.
# A link that died for some unrelated reason (a mis-set export file, a missing target) would
# satisfy the `if` above and assert nothing at all.
grep -q 'dn2cpp_http2_call_open' "$realout/nocurl-link.log" || {
    echo "FAIL: the curl-less link failed, but not on the transport symbol — so this section" >&2
    echo "      proved nothing about the opt-out. The tail of the build log:" >&2
    tail -40 "$realout/nocurl-link.log" >&2; exit 1; }
echo "OK: the curl-less link fails loudly on dn2cpp_http2_call_open — the documented opt-out behaviour"
fi   # full curl-less link

gate_cache_commit

echo OK

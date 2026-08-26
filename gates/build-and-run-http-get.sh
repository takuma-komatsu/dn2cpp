#!/usr/bin/env bash
# The System.Net.Http vertical slice: the two intercepts, the GET, and beyond it
# POST/PUT bodies, request-header emission, response-header readback and
# multi-status (sections 11-12) — then the same transport over TLS (13-15), the
# curl option's OFF side (16), and the same live GET with no `-r DnHttp` passed at
# all (17: the conditional default reference).
#
# dn2cpp replaces the body of two families of System.Net.Http method
# (CoreIntrinsics.IsBodyReplacedHttpMethod — the union both askers ask). In each the
# reachability cut (Compilation.Reach) keeps the method reachable — so a virtual slot stays a
# real pointer (CppEmitter's RenderVtable) and the abstract dispatch lands on an emitted body
# — but never scans the real IL, and the emit route supplies a synthesized body in its place:
#
#   * TRANSPORT (SocketsHttpHandler.Send/SendAsync/Dispose). System.Net.Http reaches its
#     transport through the abstract HttpMessageHandler slots, and the override that actually
#     opens sockets is SocketsHttpHandler's. Send/SendAsync forward to the libcurl-backed
#     DnHttp.DnHttpBackend and Dispose is a no-op, so SetupHandlerChain -> connection pool ->
#     socket/DNS/TLS/QUIC never enters the tree.
#   * CLIENT CERTIFICATES (HttpClientHandler.set_ClientCertificateOptions). Each of the real
#     setter's two arms takes the address of a lambda calling CertificateHelper
#     .GetEligibleClientCertificate, and reachability follows an ldftn — so BOTH entered the
#     tree whatever the caller's argument, and HttpClientHandler..ctor is a caller: merely
#     constructing an HttpClient dragged in X509Store -> the platform keychain -> ASN.1 ->
#     BigInteger. That callback is invoked only from .NET's own SslStream handshake; the
#     transport above does run TLS (sections 13-15), but it runs it in Mbed TLS, where no .NET
#     callback is reachable — so the whole subtree is dead weight either way, over half of the
#     bodies in this very sample (3,938 -> 1,816 attempted when the cut landed).
#
# WHAT THIS GATE ASSERTS. Sections 4-5 check both cuts at the TRANSPILE level with --measure —
# the feasibility harness that drains every gap instead of stopping at the first, so it sees
# the whole reachable set. HttpReal drives BOTH HttpClient construction paths (the default
# handler stack and an explicit SocketsHttpHandler); both bottom out in the intercepted
# transport override, and the first also runs the ctor -> setter path. Sections 6-7 then check
# the certificate cut at the OBJECT level, which HttpClientCert can reach because it never
# sends a request. Together:
#   (a) NOT ONE gap — failing method or any link in its reach chain — names a
#       socket/DNS/TLS-connection/QUIC symbol. Those bodies are exactly what the transport
#       intercept deletes; one here means the real transport IL is being scanned again.
#   (b) SocketsHttpHandler.Send/SendAsync/Dispose are not themselves gaps: they routed to
#       the DnHttp shim body and compiled.
#   (c) NOT ONE gap names an X509/keychain/ASN.1/BigInteger symbol, and
#       set_ClientCertificateOptions is not itself a gap: the certificate cut is applied.
#   (d) HttpClientCert transpiles and LINKS. A broken `cut ⟹ route` surfaces nowhere else: the
#       transpile would report success and the link would die on a mangled name.
#   (e) the object files' own symbol tables (nm on Unix, dumpbin //SYMBOLS on Windows — see
#       dump_object_symbols_defined/undefined in _common.sh) find no certificate-subtree BODY
#       defined and none NAMED by an emitted one — the dead weight is gone from what the linker
#       sees, not merely absent from a gap report. Read section 7's comment before touching that
#       pattern.
#
# THE FULL GET RUNS (sections 8-10). HttpReal now transpiles gap-free — the generic upper-path
# BCL the header-parser table (KnownHeaders..cctor) and HttpClient's cancellation machinery
# reach (DateTimeOffset.TryParseExact/TryFormat span+array forms, CultureInfo.get_DateTimeFormat,
# CancellationToken.Register(Action<object>,object), CancellationTokenSource.CancelAfter, and the
# generic BCL bclgap-lane closed) is mapped — and, with the vendored libcurl linked
# (DN2CPP_USE_CURL, ON by default), builds to a native binary that actually issues the GET.
# Section 8 transpiles+builds it, section 9 GETs a local http.server and diffs the output BYTE
# for byte against `dotnet HttpReal.dll` (the real SocketsHttpHandler) hitting the same server,
# and section 10 reads the objects' own symbol tables: dn2cpp_http2_call_open (the curl transport's
# one entry point) is
# defined and referenced, and NOT ONE emitted body names the .NET socket/DNS/TLS PAL the
# transport intercept cuts. That run
# also exercises the certificate guard's runtime behaviour by construction: `new HttpClient()`
# runs HttpClientHandler..ctor -> set_ClientCertificateOptions with the default option, and the
# guard body no-ops (a non-default value is the deliberate refusal — see
# MethodCompiler.EmitClientCertificateOptionsGuard), so a byte-identical GET is also proof the
# guard did not wrongly throw on the default path.
#
# BEYOND GET (sections 11-12). The same transport, exercised past the plain GET:
# POST/PUT bodies (binary and typed text), DELETE, request-header emission (custom,
# multi-value, content headers), response-header readback (the message-vs-content
# split), and non-2xx statuses (404 / 500 / 201+Location / 204). Section 11
# transpiles+builds samples/dotnet/HttpBeyondGet gap-free; section 12 runs it against
# a local echo/status server — every request is echoed back with its full sorted
# header set, so a transport-invented header (curl's implicit `Accept: */*` and
# `Content-Type: application/x-www-form-urlencoded`, both suppressed in
# dn2cpp_http2_stream.cpp) or a mis-folded multi-value header (DnHttpBackend folds to
# one ", "-joined line, as HttpConnection does) is a visible byte diff — and diffs
# the output against `dotnet HttpBeyondGet.dll` hitting the same server.
#
# HTTPS (sections 13-15). The same transport over TLS, and — this being the half where a
# regression is silent rather than loud — over BOTH outcomes. dn2cpp's TLS is the vendored
# libcurl over the vendored Mbed TLS, whose trust anchors are the Mozilla bundle
# (third_party/cacert/cacert.pem) EMBEDDED as a byte array and handed to CURLOPT_CAINFO_BLOB;
# DN2CPP_HTTP_CAINFO=<pem> replaces that anchor set at run time, and nothing anywhere
# disables verification. Section 13 transpiles+builds samples/dotnet/HttpsTls; sections 14-15
# stand up a local TLS server on a freshly generated throwaway CA (openssl, at gate run time —
# no key is checked in, and nothing leaves 127.0.0.1) and run that ONE binary against it twice:
#
#   * TRUSTED (14). DN2CPP_HTTP_CAINFO names the server's CA, so the GET must succeed. The
#     server is ours and its reply is fixed, so the whole expected stdout is known exactly and
#     is diffed against a literal block. Proof that a HANDSHAKE really happened — not merely
#     that a TLS symbol exists — comes from the SERVER side: it logs the negotiated protocol
#     and cipher of the connection that carried the request (`tls=TLSv1.3
#     cipher=TLS_AES_256_GCM_SHA384`), which no amount of un-negotiated plumbing can fabricate.
#   * UNTRUSTED (15). The same URL with no override, so the anchors are the embedded Mozilla
#     bundle, which has never heard of this CA. Three things must hold at once and each is the
#     opposite of a failure mode that ships quietly: the request must FAIL (a transport that
#     accepts an unverifiable certificate is the bug this section exists to catch — flip
#     CURLOPT_SSL_VERIFYPEER to 0 and this is what goes red), the failure must be a CATCHABLE
#     HttpRequestException that leaves the process alive, and its message must NAME the cause
#     ("mbedTLS: The certificate is not correctly signed by the trusted CA"). The server side
#     corroborates from the other end: it records a FAILED handshake and serves no request.
#
# WHAT THE ORACLE CAN AND CANNOT SAY HERE, because being vague about it is how a weak assert
# gets adopted. The UNTRUSTED run IS diffed against `dotnet HttpsTls.dll`: real .NET does not
# trust this CA either, so both transports must report the same catchable-HttpRequestException
# shape, and they do, byte for byte. The TRUSTED run is NOT diffed, and cannot be. Real .NET on
# macOS verifies through Security.framework, which has no environment lever: SSL_CERT_FILE and
# SSL_CERT_DIR are OpenSSL's and were measured to change nothing (the oracle still answers
# "errors in the certificate chain: PartialChain"), and the in-process lever —
# SocketsHttpHandler.SslOptions.CertificateChainPolicy with a CustomTrustStore — is worse than
# useless: dn2cpp's transport is libcurl and never reads SslOptions at all, so setting it would
# make the two sides diverge by construction, on top of dragging the X509/ASN.1 subtree that
# sections 5 and 7 assert is CUT back into this very gate's sample. An OpenSSL-backed host
# (Linux) would honour SSL_CERT_FILE, but a gate that compares against an oracle on one OS and
# not another asserts different things on different machines. So the trusted run is
# self-contained by decision, not by omission — and it loses nothing, because the server is the
# gate's own and its response is pinned: there is no fact about that GET an oracle could add.
#
# THE OPT-OUT (section 16). Sections 8-15 all build against the ordinary runtime, which now
# carries libcurl by default. Section 16 builds the other side of the option — the only place
# in the suite that does, since before the default flipped the whole suite built it — and
# asserts both halves of its contract: the runtime still configures and builds with
# -DDN2CPP_USE_CURL=OFF, and an HTTP-using program then fails at LINK naming
# dn2cpp_http2_call_open, rather than linking into a binary whose requests fail at run time.
#
# THE DEFAULT REFERENCE (section 17). Every section above passes `-r $dnhttp` by hand.
# Section 17 does not, and asserts the whole of section 9 anyway: the shim is injected
# because System.Net.Http is in the load set (Compilation.InjectDefaultRefs, resolving
# out of the CLI's own directory), and the resulting binary's live GET is byte-identical
# to real .NET. That is the tool-installed shape — `new HttpClient()`, no -r, a working
# native binary — and the only section where the injection is load-bearing rather than
# skipped as already-loaded.
#
# net10.0-pinned (resolve_net10_corelib): the samples build net10.0, and a newer preview
# CoreLib carries a different System.Net.Http surface.
source "$(dirname "$0")/_common.sh"

echo "== 0/17 CA-bundle freshness =="
# Ahead of every cached arm, unconditionally: this check is keyed on the CLOCK,
# not on any input the gate cache hashes, so it must run live on every
# invocation — a fully-warm cached-green run included — and its GATE-WARNING
# line (if due) is what run-all-gates.sh surfaces in the suite summary. This
# gate is the check's natural owner: sections 13-15 are the suite's only
# asserts that the embedded bundle actually verifies a TLS handshake, and a
# stale bundle degrades exactly that surface — months later, against one real
# host, on a user's machine, where no gate can see it.
cacert_staleness_warn

echo "== 1/17 Locating the real net10 CoreLib + System.Net.Http =="
corelib=$(resolve_net10_corelib)
bcl=$(dirname "$corelib")
nethttp="$bcl/System.Net.Http.dll"
echo "corlib:   $corelib"
echo "net.http: $nethttp"
[ -f "$nethttp" ] || { echo "error: System.Net.Http.dll not found: $nethttp" >&2; exit 1; }

echo "== 2/17 Building the DnHttp shim + the samples =="
build_proj internal/DnHttp/src/DnHttp/DnHttp.csproj
dnhttp="internal/DnHttp/src/DnHttp/bin/$CONFIG/$TFM/DnHttp.dll"
[ -f "$dnhttp" ] || { echo "error: not built: $dnhttp" >&2; exit 1; }
build_proj samples/dotnet/HttpReal/HttpReal.csproj
app="samples/dotnet/HttpReal/bin/$CONFIG/$TFM/HttpReal.dll"
[ -f "$app" ] || { echo "error: not built: $app" >&2; exit 1; }
build_proj samples/dotnet/HttpClientCert/HttpClientCert.csproj
certapp="samples/dotnet/HttpClientCert/bin/$CONFIG/$TFM/HttpClientCert.dll"
[ -f "$certapp" ] || { echo "error: not built: $certapp" >&2; exit 1; }
build_proj samples/dotnet/HttpBeyondGet/HttpBeyondGet.csproj
beyondapp="samples/dotnet/HttpBeyondGet/bin/$CONFIG/$TFM/HttpBeyondGet.dll"
[ -f "$beyondapp" ] || { echo "error: not built: $beyondapp" >&2; exit 1; }
build_proj samples/dotnet/HttpsTls/HttpsTls.csproj
tlsapp="samples/dotnet/HttpsTls/bin/$CONFIG/$TFM/HttpsTls.dll"
[ -f "$tlsapp" ] || { echo "error: not built: $tlsapp" >&2; exit 1; }

echo "== 3/17 Transpiling the GET (both HttpClient paths) with DnHttp injected (--measure) =="
out="artifacts/httpreal-measure"
rm -rf "$out"; mkdir -p "$out"
# --measure never aborts on a gap: it records each and keeps draining. A NON-ZERO exit
# here is a harness failure (e.g. an instantiation bound), not a per-body gap.
invoke_cli "$app" -r "$corelib" -r "$nethttp" -r "$dnhttp" --auto-ref --measure -o "$out" \
    | tee "$out/measure.log"
gaps="$out/s0-gaps.tsv"
[ -f "$gaps" ] || { echo "FAIL: --measure produced no gap report ($gaps)" >&2; exit 1; }

echo "== 4/17 Asserting the transport subtree was cut =="
# (a) No socket/DNS/TLS-connection/QUIC symbol may appear anywhere in the gap set (failing
#     method OR reach chain). Here-string, not a pipe, so a grep match does not SIGPIPE the
#     producer. The remaining gaps are all generic upper-path BCL (see the head comment),
#     which this pattern deliberately does not match.
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

echo "== 5/17 Asserting the client-certificate subtree was cut =="
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

echo "== 6/17 Transpiling + LINKING + RUNNING HttpClientCert =="
# What --measure cannot say: that the cut holds through the C++ LINKER. This is the assertion
# that matters most, because a broken `cut ⟹ route` fails exactly here and nowhere earlier —
# the transpile reports success and the link dies on a mangled name with no cause attached.
# HttpClientCert can reach a link at all only because it just constructs: the header and
# cancellation gaps that stop HttpReal are all on the send path. A plain transpile (no
# --measure), so an unmapped call is a hard error rather than a recorded gap.
#
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
# Sections 6-7 are one cached arm (this gate transpiles four programs, so each
# compile+run arm keys on its own out dir; the transpiles above always run).
cert_hit=0
if gate_cache_check "$certout" "http-get-cert|$corelib" \
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
"$certout/httpclientcert" > "$certout/run.out"
# strip_cr_win_file on the SUBJECT: the block above is an LF literal and a Windows
# console writes CRLF, so without it every line differs and none of them by content.
# Never on the oracle-diffed outputs below — there both sides are the same host's.
if ! diff -u "$certout/run.expected" <(strip_cr_win_file "$certout/run.out"); then
    echo "FAIL: HttpClientCert output diverged from the frozen block — the ClientCertificateOptions round-trip or the Automatic refusal regressed" >&2
    exit 1
fi
echo "OK: ClientCertificateOptions round-trips (Manual) and Automatic is refused naming the remedy"

echo "== 7/17 object symbols: no certificate-subtree BODY is emitted or named =="
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
# The pattern is anchored at `m_` — the start of a transpiled method's plain name (both helpers
# strip whatever mangling glues onto that prefix: Itanium's `_Z<len>` on Unix, MSVC's `?...@@`
# decoration on Windows) — for the same reason the ORIGINAL nm-only version of this check
# anchored at `_Z<len>m_`: an unanchored 'X509' matches a mangled PARAMETER type, so every thunk
# taking an X509Certificate would trip it; and since "System_" contains the substring "m_", even
# a half-anchored `m_.*X509` matches ti_System_Collections_Generic_ICollection_...X509... . Both
# were tried; both reported a cut that is in fact clean.
cert_sym='m_(System_Security_Cryptography|System_Formats_Asn1|System_Numerics_BigInteger|System_Net_Security_CertificateHelper|AppleKeychainStore|Interop_AppleCrypto)'
# A kept type's cctor FIRST-USE GUARD (CppEmitter.EmitCctorEnsures: `inline void
# <Name>__ensure()` over an `std::atomic<int8_t> <Name>__done` flag, emitted
# alongside its own reflection field accessors whenever something notes the cctor
# via NoteCctorEnsure — a field read, an rgctx CctorEnsureFn slot, ...) is not
# itself security code and must not match either check below: the wrapper is an
# atomic-flag check that dispatches through a function-pointer argument that is
# nullptr whenever, as here, the cctor body itself was never reached
# (Compilation.ReachCctor never ran for it) — the "shape kept, body cut" case
# AGENTS.md already carves out for t_/ti_. A REAL, reached cctor body is a
# SEPARATE symbol under the SAME name minus the "__ensure"/"__done" suffix
# (CppEmitter.cs: `body = compiled ? "&" + cc.CppName : "nullptr"`), which this
# exclusion does not touch, so it stays caught.
#
# The `__done` flag is DATA, not a body, and it is the one guard piece nm can see:
# a global-scope C++ VARIABLE is unmangled under the Itanium ABI, so it survives
# the helpers' `_Z<len>` strip as a plain `_m_...__done` and would otherwise trip
# a body check that only mangled FUNCTION symbols used to reach (dumpbin never
# shows it at all — MSVC decorates data, and _dump_obj_symbols_win's
# nested-paren test drops decorated non-functions in both modes, so excluding it
# here is what keeps the two arms agreeing). `ensurev?`: an out-of-line-emitted
# wrapper is a mangled function whose stripped nix spelling keeps the Itanium
# void-parameter code (`...__ensurev`), while dumpbin's readable comment gives
# the bare `...__ensure`.
cctor_guard_re='__cctor_[0-9]+__(ensurev?|done)$'
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
bodies=$(grep -cE '^m_' "$defined" || true)
[ "${bodies:-0}" -gt 50 ] || {
    echo "FAIL: only ${bodies:-0} transpiled bodies visible in the objects — the checks below would be vacuous" >&2
    exit 1; }
grep -qE '^m_System_Net_Http_HttpClientHandler_set_ClientCertificateOptions' "$defined" || {
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
cert_hits=$(grep -E "$cert_sym" "$defined" | grep -vE "$cctor_guard_re" || true)
if [ -n "$cert_hits" ]; then
    echo "FAIL: a certificate-subtree body is compiled into the objects — the cut regressed" >&2
    printf '%s\n' "$cert_hits" >&2
    exit 1
fi
# A cut edge left dangling would surface here as an undefined reference — this is
# AssertCalledBodiesEmitted's invariant, re-asked of the linker's own symbol table.
# (The guard exclusion applies here too: the defining TU's neighbours reference the
# extern `__done` flag, so it shows up as an undefined DATA symbol as well.)
cert_named=$(grep -E "$cert_sym" "$undefined" | grep -vE "$cctor_guard_re" || true)   # captured, not `| grep -q .` — see above
if [ -n "$cert_named" ]; then
    echo "FAIL: an emitted body NAMES a cut certificate body — cut ⟹ route is broken" >&2
    printf '%s\n' "$cert_named" >&2
    exit 1
fi
echo "OK: zero certificate-subtree bodies defined, zero named — the dead weight is gone"
gate_cache_commit
fi   # cert arm

# Shared by the four live-server arms below, whichever of them runs: an empty
# pid/dir is safe in the traps (`kill "" 2>/dev/null` no-ops, `rm -rf ""` too),
# and resolve_python is a pure lookup. Declared here, ahead of every arm, because
# each arm's `trap ... EXIT` REPLACES the previous one and so must name the
# earlier arms' server and scratch dir too — under `set -u` a name an arm never
# reached would otherwise abort the trap and leak the process.
py="$(resolve_python)"
webroot=""
httppid=""
echodir=""
echopid=""
tlsdir=""
tlspid=""
defwebroot=""
defpid=""

echo "== 8/17 Transpiling + building HttpReal native on the libcurl transport =="
# The full GET now transpiles with NO gaps — a plain transpile (no --measure), so an unmapped
# call is a hard error rather than a recorded gap. Built against the ordinary runtime — the
# vendored libcurl rides in it by default (DN2CPP_USE_CURL is ON), and the runtime's
# dn2cpp_http_* transport symbols are compiled only under that option — it becomes a native
# console binary that issues a real HTTP request. There is no per-section env to set for that
# any more; what the option's OFF side does is section 16's job.
realout="artifacts/httpreal-native"
rm -rf "$realout"; mkdir -p "$realout"
invoke_cli "$app" -r "$corelib" -r "$nethttp" -r "$dnhttp" --auto-ref -o "$realout"
# Sections 8-10: the second cached arm.
real_hit=0
if gate_cache_check "$realout" "http-get-real|$corelib" \
        "$app" "$dnhttp" "${app%.dll}.runtimeconfig.json" "${app%.dll}.deps.json"; then
    gate_cache_hit_msg
    real_hit=1
fi
if [ "$real_hit" = 0 ]; then
compile_console "$realout" httpreal
[ -x "$realout/httpreal" ] || { echo "FAIL: HttpReal native binary was not built" >&2; exit 1; }
echo "OK: HttpReal transpiles gap-free and links against the libcurl transport"

echo "== 9/17 Live GET: native libcurl vs real .NET SocketsHttpHandler, byte for byte =="
# A local http.server on an ephemeral free port serves one fixed file. Both the native binary
# and `dotnet HttpReal.dll` GET it and print only response-derived, deterministic lines (status
# / length / body / content-type — no Date/Server header, no client IP). Real .NET runs the
# actual SocketsHttpHandler; the native binary runs the intercepted libcurl transport. The two
# outputs must be identical. HTTP-only on purpose — the scheme is the one variable this
# section does not change; https:// over this same transport is sections 13-15.
webroot="$(mktemp -d)"
printf 'Hello from dn2cpp HttpClient!\nsecond line\n' > "$webroot/hello.txt"
# resolve_python (_common.sh): a raw `python3` is not portable on Windows — a stock install
# with no interpreter registered under that exact alias puts a non-functional app-execution-
# alias stub on PATH instead, and `command -v` cannot tell the difference. Resolved once
# (above, before the arms) and reused for every python invocation here and in section 12.
# Ephemeral port: bind :0, read the kernel's pick, release, hand it to http.server. A fixed
# port would flake when two gates (or a stray process) collide.
httpport="$($py -c 'import socket; s=socket.socket(); s.bind(("127.0.0.1",0)); print(s.getsockname()[1]); s.close()')"
$py -m http.server "$httpport" --bind 127.0.0.1 --directory "$webroot" >/dev/null 2>&1 &
httppid=$!
disown "$httppid" 2>/dev/null || true  # keep bash from printing a "Terminated" line when the trap kills it
# `|| true` on every one of this gate's cleanup traps: under `set -e` a failing
# command inside an EXIT trap exits the shell with ITS status, so a scratch dir
# the killed server has not let go of yet would report a fully green run as red.
trap 'kill "$httppid" 2>/dev/null; rm -rf "$webroot" || true' EXIT
# Deterministic readiness wait (no fixed sleep): poll until the server accepts a request.
httpready=
for _ in $(seq 1 100); do
    if $py -c "import urllib.request,sys; urllib.request.urlopen('http://127.0.0.1:$httpport/hello.txt',timeout=1)" 2>/dev/null; then
        httpready=1; break
    fi
    sleep 0.1
done
[ -n "$httpready" ] || { echo "FAIL: local http.server never came up on port $httpport" >&2; exit 1; }
base="http://127.0.0.1:$httpport"
"$realout/httpreal" "$base" > "$realout/native.out" 2> "$realout/native.err" || {
    echo "FAIL: native HttpReal exited non-zero:" >&2; cat "$realout/native.err" >&2; exit 1; }
dotnet "$app" "$base" > "$realout/dotnet.out" 2>/dev/null || {
    echo "FAIL: real .NET HttpReal exited non-zero" >&2; exit 1; }
# Non-vacuity floor: the native run really performed the GET (a 200 carrying the body), so the
# diff below is comparing real output, not two empty streams.
grep -q 'default status=200' "$realout/native.out" || {
    echo "FAIL: native output is missing the GET result — the request did not go through" >&2
    cat "$realout/native.out" >&2; exit 1; }
if ! diff -u "$realout/dotnet.out" "$realout/native.out"; then
    echo "FAIL: native libcurl GET output differs from real .NET" >&2; exit 1
fi
echo "OK: native libcurl GET is byte-identical to real .NET ($(grep -c . "$realout/native.out") lines)"

echo "== 10/17 object symbols: the libcurl transport is live, the .NET socket/DNS/TLS path is gone =="
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
# (2) No CUT transport-internal body emitted or named — checked across BOTH the defined and
# undefined dumps, since either would mean the real transport is back in the tree. Anchored at
# `m_` — the start of a transpiled method's plain name — so a socket TYPE named by a kept
# signature does not trip it (same discipline as section 7). SocketsHttpHandler.Send itself is
# DELIBERATELY present: it is the intercepted entry point, routed to the DnHttp shim — hence
# Send[A-Za-z]*Core (SendCore / SendAsyncCore), not a bare Send. cctor_guard_re (section 7)
# applies here too, same reason: a kept socket/DNS/TLS type's cctor first-use wrapper and its
# `__done` DATA flag are not themselves transport code.
sock_sym='m_(System_Net_Sockets|System_Net_Quic|System_Net_NameResolution|System_Net_Dns|System_Net_Security_SslStream|System_Net_Http_HttpConnectionPool|System_Net_Http_SocketsHttpHandler_(SetupHandlerChain|Send[A-Za-z]+Core|Startup))'
# Captured, not `| grep -q .` — the SIGPIPE-under-pipefail inversion described in
# section 7 applies verbatim, and this is the gate's headline claim.
sock_hits=$(grep -E "$sock_sym" "$real_defined" "$real_undefined" | grep -vE "$cctor_guard_re" || true)
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
gate_cache_commit
fi   # real arm

echo "== 11/17 Transpiling + building HttpBeyondGet (POST/PUT/headers/multi-status) =="
# Same -r set and transport as section 8; a plain transpile, so an unmapped call on
# the POST/PUT/header/status surface is a hard error, not a recorded gap.
beyondout="artifacts/httpbeyondget-native"
rm -rf "$beyondout"; mkdir -p "$beyondout"
invoke_cli "$beyondapp" -r "$corelib" -r "$nethttp" -r "$dnhttp" --auto-ref -o "$beyondout"
# Sections 11-12: the third cached arm.
beyond_hit=0
if gate_cache_check "$beyondout" "http-get-beyond|$corelib" \
        "$beyondapp" "$dnhttp" "${beyondapp%.dll}.runtimeconfig.json" "${beyondapp%.dll}.deps.json"; then
    gate_cache_hit_msg
    beyond_hit=1
fi
if [ "$beyond_hit" = 0 ]; then
compile_console "$beyondout" httpbeyondget
[ -x "$beyondout/httpbeyondget" ] || { echo "FAIL: HttpBeyondGet native binary was not built" >&2; exit 1; }
echo "OK: HttpBeyondGet transpiles gap-free and links against the libcurl transport"

echo "== 12/17 Beyond GET: POST/PUT/DELETE, headers, multi-status — native vs real .NET =="
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
$py -u "$echodir/echo_server.py" > "$echodir/port.txt" 2>"$echodir/server.err" &
echopid=$!
disown "$echopid" 2>/dev/null || true
# A second `trap ... EXIT` REPLACES section 9's — fold that cleanup in rather than
# losing it (section 9's file server is still running; both die here).
trap 'kill "$httppid" "$echopid" 2>/dev/null; rm -rf "$webroot" "$echodir" || true' EXIT
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
    if $py -c "import urllib.request; urllib.request.urlopen('http://127.0.0.1:$echoport/resphdr', timeout=1)" 2>/dev/null; then
        echoready=1; break
    fi
    sleep 0.1
done
[ -n "$echoready" ] || { echo "FAIL: the echo server never answered on port $echoport" >&2; exit 1; }
ebase="http://127.0.0.1:$echoport"
"$beyondout/httpbeyondget" "$ebase" > "$beyondout/native.out" 2> "$beyondout/native.err" || {
    echo "FAIL: native HttpBeyondGet exited non-zero:" >&2; cat "$beyondout/native.err" >&2; exit 1; }
dotnet "$beyondapp" "$ebase" > "$beyondout/dotnet.out" 2>/dev/null || {
    echo "FAIL: real .NET HttpBeyondGet exited non-zero" >&2; exit 1; }
# Non-vacuity floors before the diff: the 2 KiB POST body really crossed the wire,
# and the multi-valued response header really read back as two values — so the diff
# below compares real traffic, not two empty streams.
grep -qF 'post-binary | hdr content-length: 2000' "$beyondout/native.out" || {
    echo "FAIL: native output is missing the POST echo — the request did not go through" >&2
    cat "$beyondout/native.out" >&2; exit 1; }
grep -qF 'resphdr msg-header X-Resp-Multi=m1|m2' "$beyondout/native.out" || {
    echo "FAIL: native output is missing the multi-value response-header readback" >&2
    cat "$beyondout/native.out" >&2; exit 1; }
# The unknown-length body really went out CHUNKED and really arrived whole. Named
# separately from the diff because both halves are load-bearing and neither is obvious
# from a matching pair of outputs: this is the suite's only SYNCHRONOUS unknown-length
# send, and a transport that closed the upload's send side late would not diff — it
# would hang until the gate's watchdog.
grep -qF 'post-chunked | hdr transfer-encoding: chunked' "$beyondout/native.out" || {
    echo "FAIL: the unknown-length POST did not go out with a chunked framing" >&2
    cat "$beyondout/native.out" >&2; exit 1; }
# awk, not `grep -E '…{600}'`: POSIX caps an interval at RE_DUP_MAX (255), so the
# bounded-repetition form is not portable at this size and fails on a correct body.
# A length compared against a literal counts the line terminator too, so the
# subject is CR-stripped for the same reason a literal-block diff is.
awk -F'body-hex=' '/^post-chunked \| body-hex=/ { if (length($2) == 600) ok = 1 }
                   END { exit !ok }' <(strip_cr_win_file "$beyondout/native.out") || {
    echo "FAIL: the chunked POST body did not arrive whole (300 bytes = 600 hex chars)" >&2
    cat "$beyondout/native.out" >&2; exit 1; }
# HEAD/zero-length floors. The zero-length POST really carried its framing (the echoed
# content-length: 0 is the SERVER's reading of the wire), and the HEAD really
# completed with an empty body — the pre-fix transport did not misframe a HEAD,
# it STALLED on one (waiting out the advertised Content-Length on a keep-alive
# connection), so this line existing at all is the regression's tripwire.
grep -qF 'post-zerolen | hdr content-length: 0' "$beyondout/native.out" || {
    echo "FAIL: the zero-length POST went out without Content-Length: 0" >&2
    cat "$beyondout/native.out" >&2; exit 1; }
grep -qF 'head len=0' "$beyondout/native.out" || {
    echo "FAIL: the HEAD did not complete with an empty body" >&2
    cat "$beyondout/native.out" >&2; exit 1; }
if ! diff -u "$beyondout/dotnet.out" "$beyondout/native.out"; then
    echo "FAIL: native libcurl beyond-GET output differs from real .NET" >&2; exit 1
fi
echo "OK: POST/PUT/DELETE bodies, request headers, response headers and non-2xx statuses are byte-identical to real .NET ($(grep -c . "$beyondout/native.out") lines)"
gate_cache_commit
fi   # beyond arm

echo "== 13/17 Transpiling + building HttpsTls native on the libcurl/Mbed TLS transport =="
# Same -r set and transport as sections 8 and 11; a plain transpile, so an unmapped call is a
# hard error rather than a recorded gap. https:// changes nothing about the MANAGED surface —
# HttpsTls issues one ordinary HttpClient.Send — so this section's job is only to produce the
# one binary sections 14 and 15 run twice.
tlsout="artifacts/httpstls-native"
rm -rf "$tlsout"; mkdir -p "$tlsout"
invoke_cli "$tlsapp" -r "$corelib" -r "$nethttp" -r "$dnhttp" --auto-ref -o "$tlsout"
# Sections 13-15: the fourth cached arm. The throwaway CA minted in section 14 is deliberately
# NOT a cache input: it is fresh every run by design, so keying on it would defeat the cache
# entirely while proving nothing — what the key must track is the transpile output, the sample,
# the shim and the runtime tree (the shared terms gate_cache_check already folds in, which is
# where a changed dn2cpp_http2_stream.cpp or a changed cacert.pem lands).
tls_hit=0
if gate_cache_check "$tlsout" "http-get-tls|$corelib" \
        "$tlsapp" "$dnhttp" "${tlsapp%.dll}.runtimeconfig.json" "${tlsapp%.dll}.deps.json"; then
    gate_cache_hit_msg
    tls_hit=1
fi
if [ "$tls_hit" = 0 ]; then
compile_console "$tlsout" httpstls
[ -x "$tlsout/httpstls" ] || { echo "FAIL: HttpsTls native binary was not built" >&2; exit 1; }
echo "OK: HttpsTls transpiles gap-free and links against the libcurl/Mbed TLS transport"

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

echo "== 14/17 Live HTTPS with the server's CA trusted: a real handshake, and the GET succeeds =="
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
# A third `trap ... EXIT`, and like section 12's it REPLACES the previous one — so it carries
# sections 9 and 12's servers and scratch dirs too. Both may be empty here (their arms can be
# cache hits), which the shared declarations above make safe.
trap 'kill "$httppid" "$echopid" "$tlspid" 2>/dev/null; rm -rf "$webroot" "$echodir" "$tlsdir" || true' EXIT

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
$py -u "$tlsdir/tls_server.py" > "$tlsdir/port.txt" 2>"$tlsdir/server.err" &
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
    if $py -c "import ssl, urllib.request; ctx = ssl.create_default_context(cafile='$tlsca'); urllib.request.urlopen('https://127.0.0.1:$tlsport/hello.txt', context=ctx, timeout=1)" 2>/dev/null; then
        tlsready=1; break
    fi
    sleep 0.1
done
[ -n "$tlsready" ] || {
    echo "FAIL: the local TLS server never completed a handshake on port $tlsport" >&2
    cat "$tlsdir/server.err" >&2; exit 1; }
# Drop the readiness probe's own log line: from here on every line in tls.log was caused by
# the binary under test, so the counts below mean what they say.
: > "$tlsdir/tls.log"
tbase="https://127.0.0.1:$tlsport"

DN2CPP_HTTP_CAINFO="$tlsca" "$tlsout/httpstls" "$tbase" \
    > "$tlsdir/trusted.out" 2> "$tlsdir/trusted.err" || {
    echo "FAIL: native HttpsTls exited non-zero on the trusted run:" >&2
    cat "$tlsdir/trusted.err" >&2; exit 1; }
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
if ! diff -u "$tlsdir/trusted.expected" <(strip_cr_win_file "$tlsdir/trusted.out"); then
    echo "FAIL: the trusted-anchor HTTPS GET did not produce the expected response" >&2
    cat "$tlsdir/trusted.err" >&2; exit 1
fi
# The handshake itself, from the server's end. A check that only read the client's output
# would pass on a transport that never negotiated anything — which is precisely the shape a
# "does the binary contain TLS symbols?" test has, and why this reads the server instead.
served="$(grep -c '^served ' "$tlsdir/tls.log" || true)"
[ "${served:-0}" = 1 ] || {
    echo "FAIL: the TLS server served ${served:-0} requests during the trusted run, expected exactly 1" >&2
    cat "$tlsdir/tls.log" >&2; exit 1; }
# The server's log is python text mode, so CRLF on a Windows host: an
# end-anchored pattern needs the CR-stripped read (the count above does not).
grep -qE '^served path=/hello\.txt tls=TLSv1\.[23] cipher=[A-Z0-9_-]+$' \
        <<<"$(strip_cr_win_file "$tlsdir/tls.log")" || {
    echo "FAIL: the served request did not arrive over a negotiated TLS 1.2/1.3 connection" >&2
    cat "$tlsdir/tls.log" >&2; exit 1; }
echo "OK: trusted-anchor HTTPS GET succeeded over $(sed -n 's/^served .*tls=\([^ ]*\) cipher=\(.*\)$/\1 \2/p' "$tlsdir/tls.log")"

echo "== 15/17 Untrusted certificate: a catchable exception that names the cause, and .NET agrees =="
# THE MOST VALUABLE ASSERT IN THIS GATE. The same binary, the same server, the same URL — the
# only change is that DN2CPP_HTTP_CAINFO is unset, so the anchors are the embedded Mozilla
# bundle and this CA is not in it. The request MUST fail. A transport that quietly accepted an
# unverifiable certificate would pass every other section of this gate and every other gate in
# the suite: nothing else here ever presents one.
servedbefore="$(grep -c '^served ' "$tlsdir/tls.log" || true)"
"$tlsout/httpstls" "$tbase" > "$tlsdir/untrusted.out" 2> "$tlsdir/untrusted.err" || {
    echo "FAIL: native HttpsTls exited non-zero on the untrusted run — a rejected certificate must be" >&2
    echo "      a catchable HttpRequestException, not a dead process:" >&2
    cat "$tlsdir/untrusted.err" >&2; exit 1; }
# End-anchored against a Windows console's CRLF, so read the run CR-stripped —
# the file itself stays untouched for the oracle diff at the end of this section.
untrusted_lines="$(strip_cr_win_file "$tlsdir/untrusted.out")"
grep -q '^tls outcome=rejected$' <<<"$untrusted_lines" || {
    echo "FAIL: the GET against an untrusted certificate SUCCEEDED — verification is off" >&2
    cat "$tlsdir/untrusted.out" >&2; exit 1; }
# "The process is still alive" is not an inference from the exit code: the sample prints this
# line after the catch block, so it exists only if the throw was catchable at all.
grep -q '^tls alive=1$' <<<"$untrusted_lines" || {
    echo "FAIL: the untrusted run did not reach the line after the catch — the failure was not catchable" >&2
    cat "$tlsdir/untrusted.out" >&2; exit 1; }
# The diagnosis. A bare "the request failed" would satisfy everything above and leave whoever
# hits this in a shipped game with nothing to go on, so the message must name the certificate
# AND say what is wrong with it. This is dn2cpp's own wording (Mbed TLS's, relayed by curl's
# error buffer — see dn2cpp_http2_stream.cpp on why the buffer and not curl_easy_strerror), which
# is why it is asserted on stderr and kept out of the diffed stdout.
grep -qi 'certificate' "$tlsdir/untrusted.err" && grep -qi 'trusted CA' "$tlsdir/untrusted.err" || {
    echo "FAIL: the transport's error does not name the certificate and the trust anchors:" >&2
    cat "$tlsdir/untrusted.err" >&2; exit 1; }
# Corroboration from the other end of the wire, and the tightest form of "verification ran":
# the server recorded a FAILED handshake and served nothing. With CURLOPT_SSL_VERIFYPEER at 0
# the handshake would complete and this would be a `served` line instead.
grep -q '^handshake-failed ' "$tlsdir/tls.log" || {
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
dotnet "$tlsapp" "$tbase" > "$tlsdir/untrusted.dotnet.out" 2>/dev/null || {
    echo "FAIL: real .NET HttpsTls exited non-zero" >&2; exit 1; }
if ! diff -u "$tlsdir/untrusted.dotnet.out" "$tlsdir/untrusted.out"; then
    echo "FAIL: the native refusal differs in shape from real .NET's" >&2; exit 1
fi
echo "OK: an untrusted certificate is a catchable HttpRequestException naming the cause, in the same shape real .NET reports ($(sed -n 's/^tls-cause: //p' "$tlsdir/untrusted.err"))"
gate_cache_commit
fi   # tls arm

echo "== 16/17 The opt-out: -DDN2CPP_USE_CURL=OFF still builds, and fails LOUDLY at link =="
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
nocurlout="artifacts/httpreal-nocurl"
rm -rf "$nocurlout"; mkdir -p "$nocurlout"
# The SAME transpile output as section 8, copied rather than re-transpiled: what this section
# varies is the runtime, so identical C++ on both sides is the point (and a second transpile
# of the same input would be a second thing that could differ). The glob skips the dotted
# .cmake* build dirs by construction.
cp -R "$realout"/* "$nocurlout/"
# Its own out dir, hence its own cache slug — the four arms above key on theirs.
nocurl_hit=0
if gate_cache_check "$nocurlout" "http-get-nocurl|$corelib" \
        "$app" "$dnhttp" "${app%.dll}.runtimeconfig.json" "${app%.dll}.deps.json"; then
    gate_cache_hit_msg
    nocurl_hit=1
fi
if [ "$nocurl_hit" = 0 ]; then
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
# (2) …and linking an HTTP-using program against it fails, naming the symbol. The build runs in
# its own OUT/.cmake-nocurl dir (_cmake_app_builddir's DN2CPP_NO_CURL arm), so nothing here can
# clobber the working binaries above.
if ( export DN2CPP_NO_CURL=1; compile_console "$nocurlout" httprealnocurl ) \
        > "$nocurlout/link.log" 2>&1; then
    echo "FAIL: HttpReal LINKED against a runtime built with -DDN2CPP_USE_CURL=OFF." >&2
    echo "      The undefined dn2cpp_http_* symbols are the opt-out's whole contract: a binary" >&2
    echo "      that links here is one whose HTTP fails only once it is running." >&2
    exit 1
fi
# Loud AND attributable: the failure has to name the transport symbol, not merely be a failure.
# A link that died for some unrelated reason (a mis-set export file, a missing target) would
# satisfy the `if` above and assert nothing at all.
grep -q 'dn2cpp_http2_call_open' "$nocurlout/link.log" || {
    echo "FAIL: the curl-less link failed, but not on the transport symbol — so this section" >&2
    echo "      proved nothing about the opt-out. The tail of the build log:" >&2
    tail -40 "$nocurlout/link.log" >&2; exit 1; }
echo "OK: the curl-less link fails loudly on dn2cpp_http2_call_open — the documented opt-out behaviour"
gate_cache_commit
fi   # nocurl arm

echo "== 17/17 No -r DnHttp: the shim is injected by default, and the GET still runs =="
# The conditional default reference, end to end. Every section above hands the CLI
# `-r $dnhttp` by hand; this one does NOT, and that is the ONLY difference from
# section 8 — the CoreLib and System.Net.Http references are unchanged, so what is
# under test is exactly the conditional default reference and nothing else.
#
# Why it needs its own section rather than a flag on section 8: without the shim
# the transpile fails at MethodCompiler.HttpShim's "the DnHttp transport shim is
# not referenced", so this is the only place in the suite where the injection is
# load-bearing. Compilation.InjectDefaultRefs adds it because System.Net.Http is
# in the load set, resolving DnHttp.dll from the directory the CLI itself lives in
# (AppContext.BaseDirectory — the same copy `dotnet tool install dn2cpp` lays
# down). So a green here is the tool-installed shape: somebody writes
# `new HttpClient()`, passes no -r, and gets a working native binary.
#
# The assertion is the full section 9 one, not merely "it transpiled": the
# injected shim must produce a binary whose live GET is byte-identical to real
# .NET's SocketsHttpHandler. An injection that resolved the wrong assembly, or
# resolved it but left the transport unrouted, would compile and then differ.
defout="artifacts/httpreal-defaultref"
rm -rf "$defout"; mkdir -p "$defout"
invoke_cli "$app" -r "$corelib" -r "$nethttp" --auto-ref -o "$defout"
# Sections 17: the fifth cached arm, its own OUT and so its own cache slug. The
# CONTEXT names the absent flag rather than leaving it implied by the slug: this
# arm's whole subject is a reference nobody passed, and a key that cannot see the
# difference between "-r DnHttp" and "no -r DnHttp" is one that could replay
# section 8's green here.
def_hit=0
if gate_cache_check "$defout" "http-get-defaultref|$corelib|nodnhttpref" \
        "$app" "$dnhttp" "${app%.dll}.runtimeconfig.json" "${app%.dll}.deps.json"; then
    gate_cache_hit_msg
    def_hit=1
fi
if [ "$def_hit" = 0 ]; then
compile_console "$defout" httpreal
[ -x "$defout/httpreal" ] || { echo "FAIL: HttpReal native binary was not built without -r DnHttp" >&2; exit 1; }
echo "OK: HttpReal transpiles and links with NO -r DnHttp — the shim was injected"
# Its own server on its own ephemeral port: sections 8-10 may have been a cache
# hit, in which case section 9's server was never started. A fourth `trap ... EXIT`
# REPLACES the previous one, so it carries every earlier arm's pid and dir too.
defwebroot="$(mktemp -d)"
printf 'Hello from dn2cpp HttpClient!\nsecond line\n' > "$defwebroot/hello.txt"
defport="$($py -c 'import socket; s=socket.socket(); s.bind(("127.0.0.1",0)); print(s.getsockname()[1]); s.close()')"
$py -m http.server "$defport" --bind 127.0.0.1 --directory "$defwebroot" >/dev/null 2>&1 &
defpid=$!
disown "$defpid" 2>/dev/null || true
trap 'kill "$httppid" "$echopid" "$tlspid" "$defpid" 2>/dev/null; rm -rf "$webroot" "$echodir" "$tlsdir" "$defwebroot" || true' EXIT
defready=
for _ in $(seq 1 100); do
    if $py -c "import urllib.request,sys; urllib.request.urlopen('http://127.0.0.1:$defport/hello.txt',timeout=1)" 2>/dev/null; then
        defready=1; break
    fi
    sleep 0.1
done
[ -n "$defready" ] || { echo "FAIL: local http.server never came up on port $defport" >&2; exit 1; }
defbase="http://127.0.0.1:$defport"
"$defout/httpreal" "$defbase" > "$defout/native.out" 2> "$defout/native.err" || {
    echo "FAIL: the default-reference HttpReal exited non-zero:" >&2; cat "$defout/native.err" >&2; exit 1; }
dotnet "$app" "$defbase" > "$defout/dotnet.out" 2>/dev/null || {
    echo "FAIL: real .NET HttpReal exited non-zero" >&2; exit 1; }
# Non-vacuity floor, as in section 9: the run really performed the GET.
grep -q 'default status=200' "$defout/native.out" || {
    echo "FAIL: the default-reference binary printed no GET result — the request did not go through" >&2
    cat "$defout/native.out" >&2; exit 1; }
if ! diff -u "$defout/dotnet.out" "$defout/native.out"; then
    echo "FAIL: the injected-DnHttp GET differs from real .NET" >&2; exit 1
fi
echo "OK: with no -r DnHttp at all, the GET is byte-identical to real .NET ($(grep -c . "$defout/native.out") lines)"
gate_cache_commit
fi   # default-ref arm

echo OK

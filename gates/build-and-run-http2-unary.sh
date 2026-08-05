#!/usr/bin/env bash
# The h2 transport surface beneath gRPC's unary call: version negotiation (h2c prior
# knowledge over http://, ALPN h2 over https://), the negotiated-version readback onto
# HttpResponseMessage.Version, the header/trailer split (TrailingHeaders is NOT the
# header list — grpc-status lives in the trailer section), and the loud transport
# failure an EXACT version policy produces when the peer will not negotiate it. See
# internal/DnHttp/src/DnHttp/DnHttpBackend.cs's own header comment for the four
# request/response properties gRPC's unary path stands on and why each is asserted
# here — this gate is what asserts them, one level below gRPC itself.
#
# samples/dotnet/Http2TestServer is INFRASTRUCTURE, never transpiled — the same
# posture as build-and-run-http-get.sh's local http.server / TLS python servers: a
# plain Kestrel process the gate drives with `dotnet Http2TestServer.dll ...` and
# never feeds to the transpiler. samples/dotnet/Http2Unary is the transpiled SUBJECT:
# one program, mode-dispatched on argv, that a real HttpClient (the oracle) and the
# native dn2cpp binary both run against the SAME server so their stdout can be diffed
# byte for byte.
#
# WHAT THIS GATE ASSERTS.
# Two of the h2c sections are about the CONNECTION rather than the message, and each
# is asserted through the SERVER because a transport's own account of itself proves
# nothing: two requests reading one connection id (cross-call reuse through the
# shared multi's conncache — nothing else in this suite notices its loss, since
# every other assert passes on a fresh handshake), and a canceled request the
# server saw torn down rather than drained (a transport that only released the
# awaiting caller reports the same caller-side exception).
#
# Two more are about the SEND side of the one streaming route every request
# takes: the send returns at the response header block under
# ResponseHeadersRead and at the end of the body under the default option (the
# /slowbody pair), and SocketsHttpHandler's connection-pool knobs reach curl —
# PooledConnectionIdleTimeout observed as a reuse/no-reuse pair, and the
# two STRUCTURAL knobs observed at the server: four concurrent h2 requests
# multiplex ONE connection (EnableMultipleHttp2Connections=false, the default),
# and MaxConnectionsPerServer=2 holds three concurrent HTTP/1.1 requests to two
# wire connections. Both ride the /connhold concurrency peak, the server's own
# testimony that the requests really overlapped.
#
# A third belongs to the REQUEST direction and is split across both streams: the
# streaming send queue's backpressure. The subject POSTs 4 MiB through a
# length-less body — the shape that arms an incremental (send-queue) upload — and the
# server answers with the length and an FNV-1a hash of what arrived, so both sides of
# the stdout diff assert that a writer parking and resuming loses and reorders
# nothing. That the writer PARKED, and how deep the queue got, is a fact about a
# queue real .NET does not have, so it cannot be diffed: it is asserted off the
# native run's stderr, which this gate lowers the bound to 64 KiB for.
#
#   h2c (section 4, ORACLE-DIFFED). POST /echo over h2c prior knowledge — the
#   negotiated-version readback (msg.Version == 2.0) and the response headers land
#   correctly; GET /trailers — the trailers are non-empty and land on TrailingHeaders,
#   separately from the headers; POST /echo to an HTTP/1.1-ONLY listener with
#   (2.0, RequestVersionExact) — a policy the peer cannot satisfy is a LOUD, catchable
#   HttpRequestException, never a silent downgrade; the same request with
#   RequestVersionOrLower — succeeds, downgraded to 1.1. Real .NET's SYNCHRONOUS
#   HttpClient.Send refuses HTTP/2 outright (see Http2Unary/Program.cs's own comment
#   on its `Send` helper), so both sides of this diff go through SendAsync — the one
#   deviation from a plain client.Send() the sample had to make to keep the oracle
#   arm runnable at all.
#   TLS+ALPN (section 5, self-contained — see that section's own comment for why real
#   .NET cannot be an oracle here, the same reasoning as build-and-run-http-get.sh
#   section 14): GET /echo over a real ALPN-negotiated h2 connection, verified against
#   a throwaway CA minted at run time.
#
# net10.0-pinned (resolve_net10_corelib): matches every other System.Net.Http gate —
# the samples build net10.0, and a newer preview CoreLib carries a different
# System.Net.Http surface.
source "$(dirname "$0")/_common.sh"

echo "== 1/5 Locating the real net10 CoreLib + System.Net.Http, and the h2 oracle =="
corelib=$(resolve_net10_corelib)
bcl=$(dirname "$corelib")
nethttp="$bcl/System.Net.Http.dll"
[ -f "$nethttp" ] || { echo "error: System.Net.Http.dll not found: $nethttp" >&2; exit 1; }
echo "corlib:   $corelib"
echo "net.http: $nethttp"
# The oracle server is Kestrel — Microsoft.AspNetCore.App's shared framework, not a
# NuGet package — which a bare console-only SDK install does not carry. Captured
# first, then grepped from a here-string (never `dotnet ... | grep -q`, which is the
# banned pipeline-into-an-early-exiting-consumer shape gates/_common.sh documents
# beside `set -o pipefail`).
runtimes="$(dotnet --list-runtimes)"
grep -q '^Microsoft.AspNetCore.App ' <<<"$runtimes" \
    || gate_skip "ASP.NET Core shared framework absent — the h2 oracle server is Kestrel"
echo "OK: ASP.NET Core shared framework present"

echo "== 2/5 Building the DnHttp shim + the samples =="
build_proj internal/DnHttp/src/DnHttp/DnHttp.csproj
dnhttp="internal/DnHttp/src/DnHttp/bin/$CONFIG/$TFM/DnHttp.dll"
[ -f "$dnhttp" ] || { echo "error: not built: $dnhttp" >&2; exit 1; }
build_proj samples/dotnet/Http2Unary/Http2Unary.csproj
app="samples/dotnet/Http2Unary/bin/$CONFIG/$TFM/Http2Unary.dll"
[ -f "$app" ] || { echo "error: not built: $app" >&2; exit 1; }
build_proj samples/dotnet/Http2TestServer/Http2TestServer.csproj
srv="samples/dotnet/Http2TestServer/bin/$CONFIG/$TFM/Http2TestServer.dll"
[ -f "$srv" ] || { echo "error: not built: $srv" >&2; exit 1; }

echo "== 3/5 Transpiling + building Http2Unary on the h2/nghttp2 transport =="
out="artifacts/http2unary-native"
rm -rf "$out"; mkdir -p "$out"
invoke_cli "$app" -r "$corelib" -r "$nethttp" -r "$dnhttp" --auto-ref -o "$out"
# ONE cached arm, like each artifact-keyed arm in build-and-run-http-get.sh: the
# transpile above always runs (it is what produces the surface the key hashes), but a
# hit skips the expensive part below — the C++ compile+link AND every live-server
# assertion, exactly the shape sections 8-10/13-15 use there. The live server is
# fresh every run regardless; what a cache hit buys is not starting it at all when
# the transpile output, the runtime tree and the toolchain env have not moved.
hit=0
if gate_cache_check "$out" "http2-unary|$corelib" \
        "$app" "$dnhttp" "${app%.dll}.runtimeconfig.json" "${app%.dll}.deps.json"; then
    gate_cache_hit_msg
    hit=1
fi
if [ "$hit" = 0 ]; then
compile_console "$out" http2unary
[ -x "$out/http2unary" ] || { echo "FAIL: Http2Unary native binary was not built" >&2; exit 1; }
echo "OK: Http2Unary transpiles gap-free and links against the h2/nghttp2 transport"

# Shared by both live-server arms below: declared here (empty, so `set -u` and the
# EXIT trap tolerate a not-yet-started process/dir), and the second arm's trap
# REPLACES the first's, so it must fold in what the first already started —
# mirroring build-and-run-http-get.sh's trap-folding discipline (see its section 12's
# comment on why a fresh `trap ... EXIT` names every earlier arm's pid and dir too).
h2cdir=""
h2cpid=""
tlsdir=""
tlspid=""

echo "== 4/5 Live h2c: version negotiation, trailers, the exact-policy miss, the OrLower downgrade =="
h2cdir="$(mktemp -d)"
dotnet "$srv" > "$h2cdir/ready.out" 2>"$h2cdir/server.err" &
h2cpid=$!
disown "$h2cpid" 2>/dev/null || true
trap 'kill "$h2cpid" "$tlspid" 2>/dev/null; rm -rf "$h2cdir" "$tlsdir"' EXIT
ready=""
for _ in $(seq 1 100); do
    [ -s "$h2cdir/ready.out" ] && { ready="$(head -1 "$h2cdir/ready.out")"; break; }
    sleep 0.1
done
[ -n "$ready" ] || {
    echo "FAIL: the h2 oracle server never printed READY" >&2
    cat "$h2cdir/server.err" >&2; exit 1; }
read -r tag h2cport h1port <<<"$ready"
[ "$tag" = "READY" ] && [ -n "${h2cport:-}" ] && [ -n "${h1port:-}" ] || {
    echo "FAIL: could not parse the oracle's READY line (expected 2 ports): $ready" >&2; exit 1; }
h2cbase="http://127.0.0.1:$h2cport"
h1base="http://127.0.0.1:$h1port"

# The send-queue bound is lowered to 64 KiB for the run, so the sink
# section's first 64 KiB chunk already fills the queue and the writer parks by
# CONSTRUCTION rather than by winning a race against the connection — the property
# that keeps this section's asserts off the clock. The stats line the second
# variable asks for goes to STDERR, so the stdout diff below is untouched. Both are
# set here rather than in the CONTEXT string on purpose: gate_cache_check hashes
# this script, so editing either value already invalidates the key.
DN2CPP_HTTP_SEND_HIGH_WATER=65536 DN2CPP_HTTP_SEND_STATS=1 \
    "$out/http2unary" h2c "$h2cbase" "$h1base" > "$out/native.out" 2>"$out/native.err" || {
    echo "FAIL: native Http2Unary exited non-zero:" >&2; cat "$out/native.err" >&2; exit 1; }
dotnet "$app" h2c "$h2cbase" "$h1base" > "$out/dotnet.out" 2>/dev/null || {
    echo "FAIL: real .NET Http2Unary exited non-zero" >&2; exit 1; }

# Non-vacuity floors BEFORE the diff — files given directly to grep, never a pipe (see
# _common.sh's "X | grep -q P IS A BROKEN ASSERT" for why a captured-then-piped form
# is unsafe under pipefail; a plain file argument has no producer to SIGPIPE). Each
# greps for a fact only a real negotiation, a real trailer or a real policy refusal
# can produce.
grep -qF '[h2c] version=2.0' "$out/native.out" || {
    echo "FAIL: native output never reports version=2.0 — h2c prior knowledge did not negotiate" >&2
    cat "$out/native.out" >&2; exit 1; }
grep -qF 'proto=HTTP/2' "$out/native.out" || {
    echo "FAIL: native output carries no proto=HTTP/2 body line — the SERVER never saw h2" >&2
    cat "$out/native.out" >&2; exit 1; }
grep -qF '[trailers] trailer x-gate-trailer=tv1' "$out/native.out" || {
    echo "FAIL: native output is missing the h2 trailer readback" >&2
    cat "$out/native.out" >&2; exit 1; }
grep -qF '[exact-miss] HttpRequestException' "$out/native.out" || {
    echo "FAIL: native output did not report the exact-policy miss as a catchable HttpRequestException" >&2
    cat "$out/native.out" >&2; exit 1; }
grep -qF '[orlower] version=1.1' "$out/native.out" || {
    echo "FAIL: native output did not downgrade to 1.1 under RequestVersionOrLower" >&2
    cat "$out/native.out" >&2; exit 1; }
# The SERVER's own connection id, read twice: the transport's shared multi (its
# conncache) is what makes the second request land on the first's connection, and nothing else in
# this suite would notice its loss — every other assert passes on a fresh handshake.
grep -qF '[reuse] same-connection=True non-empty=True' "$out/native.out" || {
    echo "FAIL: the second request did not reuse the first's connection (cross-call reuse)" >&2
    cat "$out/native.out" >&2; exit 1; }
# The server half is the load-bearing one: a transport that only released
# the awaiting caller would still print caller=OperationCanceledException while the
# orphaned transfer drained, which is exactly the state this closed.
grep -qF '[cancel] caller=OperationCanceledException' "$out/native.out" || {
    echo "FAIL: a fired token did not release the caller as an OperationCanceledException" >&2
    cat "$out/native.out" >&2; exit 1; }
grep -qF '[cancel] server-started=True server-aborted=True server-none-completed=True' "$out/native.out" || {
    echo "FAIL: the server never saw the canceled transfer torn down (mid-flight abort)" >&2
    cat "$out/native.out" >&2; exit 1; }
# Both halves are named, because the pair is the assertion: the send returns at
# the header block under ResponseHeadersRead and only then, and it still returns at the
# END of the body under the default — a transport that answered either option the same
# way would satisfy one of these greps and not the other. This was False before every
# request took the streaming call: routing keyed on the REQUEST body's shape, so a
# bodyless GET waited out the whole response whatever the caller asked for.
grep -qF '[headersread] body-lagged-headers=True' "$out/native.out" || {
    echo "FAIL: SendAsync under ResponseHeadersRead did not return at the header block" >&2
    cat "$out/native.out" >&2; exit 1; }
grep -qF '[contentread] body-lagged-headers=False' "$out/native.out" || {
    echo "FAIL: SendAsync under the DEFAULT completion option returned before the body" >&2
    cat "$out/native.out" >&2; exit 1; }
# Again the pair is the assertion: a transport that ignored
# PooledConnectionIdleTimeout — the failure mode being an intercept that drops the
# handler instance — reuses the connection in BOTH lines.
grep -qF '[pool] default-reuse=True' "$out/native.out" || {
    echo "FAIL: a default-configured handler did not reuse a 2 s-idle connection" >&2
    cat "$out/native.out" >&2; exit 1; }
grep -qF '[pool] expiring-reuse=False' "$out/native.out" || {
    echo "FAIL: PooledConnectionIdleTimeout=1s did not stop the reuse of a 2 s-idle connection" >&2
    cat "$out/native.out" >&2; exit 1; }
# Both halves observed at the SERVER. Four concurrent h2 requests rode ONE
# connection — multiplexing, impossible while every call has its own multi — and
# peak=4 is the non-vacuity floor proving they really were in flight together.
grep -qF '[mux] within-ceiling=True all-ok=True peak=4' "$out/native.out" || {
    echo "FAIL: four concurrent h2 requests did not multiplex one connection" >&2
    cat "$out/native.out" >&2; exit 1; }
# ...and MaxConnectionsPerServer=2 held three concurrent HTTP/1.1 requests to two
# wire connections: peak=2 — not 3 — is the cap observed where it cannot be faked.
grep -qF '[maxconn] within-ceiling=True all-ok=True peak=2' "$out/native.out" || {
    echo "FAIL: MaxConnectionsPerServer=2 did not cap three concurrent 1.1 requests" >&2
    cat "$out/native.out" >&2; exit 1; }
# The half a client CAN see: 4 MiB pushed through a length-less body arrived
# whole and in order, which only the SERVER's own length and content hash can say.
# It is diffed too (both lines are in the byte-for-byte compare below); the floor is
# here so a transport that dropped the section reads as a named failure rather than
# as a shorter diff.
grep -qF '[sink] body-line len=4194304' "$out/native.out" || {
    echo "FAIL: the 4 MiB backpressured upload did not arrive whole (send queue)" >&2
    cat "$out/native.out" >&2; exit 1; }
# A server that answers completely WITHOUT draining the request body is
# a response, never an upload error — the line must be the 200 with the server's
# own read=0, not an exception name.
grep -qF '[earlyreply] status=200 body=early-reply read=0' "$out/native.out" || {
    echo "FAIL: an early reply over an undrained request body did not surface as the response" >&2
    cat "$out/native.out" >&2; exit 1; }
# A dropped, never-read ResponseHeadersRead response must be torn down
# once the GC reaches it — the SERVER's aborted counter is the only witness, since
# by construction nobody on the client side observes that stream again.
grep -qF '[drop] server-started=True server-aborted=True' "$out/native.out" || {
    echo "FAIL: a dropped response stream was never torn down — its transfer is parked until process exit" >&2
    cat "$out/native.out" >&2; exit 1; }

if ! diff -u "$out/dotnet.out" "$out/native.out"; then
    echo "FAIL: native h2 output differs from real .NET" >&2; exit 1
fi
echo "OK: h2c version negotiation, trailers, exact-miss and OrLower downgrade are byte-identical to real .NET ($(grep -c . "$out/native.out") lines)"

# The half only the native transport can testify to: the writer really parked
# on a full send queue, and the queue really stayed bounded. Reported on STDERR under
# DN2CPP_HTTP_SEND_STATS=1 (a measurement knob, like DN2CPP_GC_STATS) because a
# counter on stdout could not be diffed against a real .NET that has no such queue.
# Exactly two lines, in section order: the sink's and the early-reply section's —
# those are the only length-less uploads in this run. Every request rides the
# streaming family, but only a call that fed the send queue (an
# incremental, bodyMode-2 upload) reports, so a third line would mean a sized body
# stopped going over whole at open(). Only the SINK line's numbers are asserted:
# the early-reply upload races the server's reply by design, so its depth is not a
# stable fact.
sinkstats="$(first_line "$(grep -F 'dn2cpp: http2 send queue' "$out/native.err" || true)")"
sinkcount="$(grep -cF 'dn2cpp: http2 send queue' "$out/native.err" || true)"
[ "$sinkcount" = 2 ] || {
    echo "FAIL: expected exactly two send-queue stats lines on stderr (sink, earlyreply), found $sinkcount" >&2
    cat "$out/native.err" >&2; exit 1; }
sinkre='parks=([0-9]+) peak=([0-9]+) highwater=([0-9]+)'
[[ "$sinkstats" =~ $sinkre ]] || {
    echo "FAIL: could not parse the send-queue stats line: $sinkstats" >&2; exit 1; }
parks="${BASH_REMATCH[1]}"
peak="${BASH_REMATCH[2]}"
highwater="${BASH_REMATCH[3]}"
[ "$highwater" = 65536 ] || {
    echo "FAIL: DN2CPP_HTTP_SEND_HIGH_WATER never reached the transport (highwater=$highwater)" >&2; exit 1; }
[ "$parks" -ge 1 ] || {
    echo "FAIL: the writer never parked — 4 MiB went past a 64 KiB bound unthrottled" >&2; exit 1; }
# The bound's whole point. The ceiling is one chunk above it, because write() takes a
# chunk WHOLE and parks after: a peak near the upload size would mean the queue is
# still buffering the difference, which is the state this ticket closed.
[ "$peak" -le 131072 ] || {
    echo "FAIL: the send queue peaked at $peak bytes against a 65536-byte bound — not bounded" >&2; exit 1; }
echo "OK: a 4 MiB upload crossed a 65536-byte send queue — writer parked $parks time(s), peak $peak bytes, every byte arrived in order"

echo "== 5/5 Live TLS+ALPN: a real h2-over-TLS handshake (self-contained) =="
# NOT oracle-diffed, for the same reason build-and-run-http-get.sh section 14's
# trusted-anchor run is not (see that section's comment): real .NET on macOS verifies
# through Security.framework, which has no environment lever to point at a throwaway
# CA — SSL_CERT_FILE/SSL_CERT_DIR are OpenSSL's and change nothing there. Measured
# directly for this gate too: real .NET against this section's own throwaway CA
# reports "The SSL connection could not be established" every time, which is the
# CORRECT verdict for a CA it was never told to trust — not a bug to route around, but
# exactly why this section runs ONLY the native binary, under
# DN2CPP_HTTP_CAINFO naming the CA, against the gate's own server.
command -v openssl >/dev/null 2>&1 || {
    echo "FAIL: openssl not on PATH — this section mints the TLS server's throwaway certificate with it" >&2
    exit 1; }
tlsdir="$(mktemp -d)"
trap 'kill "$h2cpid" "$tlspid" 2>/dev/null; rm -rf "$h2cdir" "$tlsdir"' EXIT

# openssl driven through CONFIG FILES, not -addext — see build-and-run-http-get.sh
# sections 14-15 for why (-addext is OpenSSL 1.1.1+/LibreSSL 3.1+ only; both forms
# were checked against these two files there, on Homebrew OpenSSL and the macOS
# system LibreSSL). Two certificates, not one self-signed leaf: Mbed TLS only accepts
# a trust anchor carrying basicConstraints CA:TRUE, a shape a self-signed leaf is not.
cat > "$tlsdir/ca.cnf" <<'CAEOF'
[req]
distinguished_name = dn
prompt             = no
x509_extensions    = v3_ca
[dn]
CN = dn2cpp-h2-gate-ca
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
# -days 2 (not 1): a run that straddles midnight UTC must not be the thing that
# fails. -set_serial (not -CAcreateserial): no .srl file left behind.
openssl req -x509 -newkey rsa:2048 -nodes -days 2 \
    -keyout "$tlsdir/ca.key" -out "$tlsdir/ca.pem" -config "$tlsdir/ca.cnf" >"$tlsdir/openssl.log" 2>&1 &&
openssl req -newkey rsa:2048 -nodes \
    -keyout "$tlsdir/srv.key" -out "$tlsdir/srv.csr" -config "$tlsdir/srv.cnf" >>"$tlsdir/openssl.log" 2>&1 &&
openssl x509 -req -in "$tlsdir/srv.csr" -CA "$tlsdir/ca.pem" -CAkey "$tlsdir/ca.key" \
    -set_serial 1 -days 2 -extfile "$tlsdir/srv.cnf" -extensions v3_srv \
    -out "$tlsdir/srv.pem" >>"$tlsdir/openssl.log" 2>&1 || {
    echo "FAIL: could not generate the throwaway TLS certificate:" >&2; cat "$tlsdir/openssl.log" >&2; exit 1; }

dotnet "$srv" --cert "$tlsdir/srv.pem" --key "$tlsdir/srv.key" \
    > "$tlsdir/ready.out" 2>"$tlsdir/server.err" &
tlspid=$!
disown "$tlspid" 2>/dev/null || true
tlsready=""
for _ in $(seq 1 100); do
    [ -s "$tlsdir/ready.out" ] && { tlsready="$(head -1 "$tlsdir/ready.out")"; break; }
    sleep 0.1
done
[ -n "$tlsready" ] || {
    echo "FAIL: the TLS oracle server never printed READY" >&2
    cat "$tlsdir/server.err" >&2; exit 1; }
read -r tag _ _ tlsport <<<"$tlsready"
[ "$tag" = "READY" ] && [ -n "${tlsport:-}" ] || {
    echo "FAIL: could not parse the TLS oracle's READY line (expected 3 ports): $tlsready" >&2; exit 1; }
tlsbase="https://127.0.0.1:$tlsport"

DN2CPP_HTTP_CAINFO="$tlsdir/ca.pem" "$out/http2unary" tls "$tlsbase" \
    > "$tlsdir/tls.out" 2>"$tlsdir/tls.err" || {
    echo "FAIL: native Http2Unary exited non-zero on the TLS run:" >&2; cat "$tlsdir/tls.err" >&2; exit 1; }
grep -qF '[tls] version=2.0 status=200' "$tlsdir/tls.out" || {
    echo "FAIL: the TLS+ALPN GET did not negotiate h2 — no '[tls] version=2.0 status=200' line" >&2
    cat "$tlsdir/tls.out" >&2; exit 1; }
grep -qF 'proto=HTTP/2' "$tlsdir/tls.out" || {
    echo "FAIL: the TLS+ALPN GET body carries no proto=HTTP/2 line — the SERVER never saw h2" >&2
    cat "$tlsdir/tls.out" >&2; exit 1; }
grep -qF '[tls] same-connection=True' "$tlsdir/tls.out" || {
    echo "FAIL: two sequential TLS requests did not share a connection (cross-call reuse)" >&2
    cat "$tlsdir/tls.out" >&2; exit 1; }
echo "OK: TLS+ALPN GET negotiated real h2 over a handshake verified against the throwaway CA, reused for the next request"

gate_cache_commit
fi   # http2unary arm

echo OK

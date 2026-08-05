#!/usr/bin/env bash
# grpc-dotnet, client side, unary AND streaming: the REAL Grpc.Net.Client +
# Google.Protobuf NuGet assemblies transpiled by dn2cpp and run as a native binary
# against a real Grpc.AspNetCore (Kestrel) server — request message → h2c wire →
# response message, byte-diffed against the same client run by real .NET against
# the same server. Sections (1)-(4) are the unary surface (which itself now rides
# the STREAMING transport — grpc's PushUnaryContent has no content length, so the
# DnHttp routing predicate sends every gRPC call through the dn2cpp_http2_call_*
# family); (5)-(9) are server streaming (headers before drain, trailers after
# EOS), a mid-stream server failure, client streaming, an INTERLOCKED bidi echo
# (each echo awaited before the next send — a transport that buffers either
# direction deadlocks loudly here), and a mid-stream deadline (got=1: the first
# tick surfaced before the fired token aborted the stream); (10) is the
# client-local descriptor-reflection tail.
#
# The stack under test, from the top: protoc-generated message/stub code
# (samples/dotnet/GrpcProbe/Generated/, checked in — see its header for the regen
# recipe), Grpc.Net.Client's unary call machinery (GrpcCall/RetryCallBase, deadline
# CTS, trailer readback), Google.Protobuf's wire serializer, System.Net.Http's upper
# path, the DnHttp shim (whose header comment names the four request/response
# properties this gate stands on), and the h2c/nghttp2 native transport one gate
# below (build-and-run-http2-unary.sh asserts that layer in isolation).
#
# samples/dotnet/GrpcUnaryServer is INFRASTRUCTURE, never transpiled — a real-.NET
# Kestrel gRPC service the gate drives with `dotnet`; samples/dotnet/GrpcUnary is the
# transpiled SUBJECT. Both sides of the diff run the same client program, so every
# printed line of sections (1)-(9) is server-derived (the sample prints no
# client-synthesized exception text); section (10) is the one deliberately LOCAL
# tail — descriptor reflection, see below.
#
# WHAT THIS GATE ASSERTS.
#   * The measure section: the full Grpc.Net.Client closure transpiles with ZERO gaps
#     — and not one gap chain names the socket/DNS/TLS subtree the transport
#     intercept cuts (the same regex build-and-run-http-get.sh section 4 holds; the
#     Balancer socket transport is Bounded, see CoreIntrinsics.Intercepts.cs).
#   * The live section, one round trip per RPC shape: a unary success (field-for-field
#     echo through protobuf wire format), response headers + trailers surfaced through
#     ResponseHeadersAsync/GetTrailers, a server-thrown RpcException (status +
#     detail + exception trailers — i.e. grpc-status/grpc-message arrive in the
#     TRAILER section), and a 500 ms deadline against a 30 s server sleep
#     (DeadlineExceeded raised by the fired token, not by waiting the server out).
#   * The streaming sections, all three shapes against the same server:
#     server streaming with response headers readable before the stream drains and
#     trailers after EOS; a server that fails after two messages (partial body,
#     then status/detail/trailer); client streaming (aggregate reply); the
#     interlocked bidi echo, whose await-per-message ordering is the transport's
#     incrementality proof in BOTH directions; and a mid-stream deadline, where
#     exactly one message precedes the abort (a transport that buffered the
#     response would report got=0 and diff red).
#   * The descriptor-reflection section, client-local: message ToString()/
#     JsonFormatter and descriptor-driven field access (accessor GetValue/SetValue/
#     Clear, oneof case + Clear) over an app-module message AND a library one
#     (WellKnownTypes.Value, whose Clear*/Has* members only reflection reaches),
#     byte-diffed like everything else — AND the client's startup stderr must be
#     EMPTY. The wire path never consults the accessors, so a descriptor-.cctor
#     failure leaves every RPC diffing green while printing failure lines there;
#     the stderr assert is what sees that class of regression.
#
# WHAT THE NUGET NETWORK BUYS AND WHAT PINS IT: the packages restore from nuget.org
# (the gdtask precedent — no DLL is vendored in this repository), and WHAT the gate
# transpiles is pinned by content hash against gates/expected/grpc-unary-dlls.sha256;
# a re-published package, a poisoned cache or an accidental version bump fails there,
# not as a mystery gap count.
#
# net10.0-pinned (resolve_net10_corelib), like every System.Net.Http gate.
source "$(dirname "$0")/_common.sh"

echo "== 1/6 Locating the real net10 CoreLib + the gRPC oracle prerequisites =="
corelib=$(resolve_net10_corelib)
bcl=$(dirname "$corelib")
nethttp="$bcl/System.Net.Http.dll"
[ -f "$nethttp" ] || { echo "error: System.Net.Http.dll not found: $nethttp" >&2; exit 1; }
echo "corlib:   $corelib"
# Captured then grepped from a here-string — never `dotnet ... | grep -q` (the
# banned pipeline-into-an-early-exiting-consumer shape; see _common.sh).
runtimes="$(dotnet --list-runtimes)"
grep -q '^Microsoft.AspNetCore.App ' <<<"$runtimes" \
    || gate_skip "ASP.NET Core shared framework absent — the gRPC oracle server is Kestrel"
echo "OK: ASP.NET Core shared framework present"

echo "== 2/6 Building the DnHttp shim + the client + the oracle server =="
build_proj internal/DnHttp/src/DnHttp/DnHttp.csproj
dnhttp="internal/DnHttp/src/DnHttp/bin/$CONFIG/$TFM/DnHttp.dll"
[ -f "$dnhttp" ] || { echo "error: not built: $dnhttp" >&2; exit 1; }
build_proj samples/dotnet/GrpcUnary/GrpcUnary.csproj
app="samples/dotnet/GrpcUnary/bin/$CONFIG/$TFM/GrpcUnary.dll"
appbin="$(dirname "$app")"
[ -f "$app" ] || { echo "error: not built: $app" >&2; exit 1; }
build_proj samples/dotnet/GrpcUnaryServer/GrpcUnaryServer.csproj
srv="samples/dotnet/GrpcUnaryServer/bin/$CONFIG/$TFM/GrpcUnaryServer.dll"
[ -f "$srv" ] || { echo "error: not built: $srv" >&2; exit 1; }

# Content tripwire on every restored assembly the transpile consumes (the gdtask
# discipline, one file for the set): the transport is the network, WHAT is transpiled
# is pinned by hash. This loop is also what keeps the -r list below honest — a DLL
# named here must exist in the client's bin, so a package rename/regroup fails here
# first with its own sentence.
GRPC_SHA_EXPECTED=gates/expected/grpc-unary-dlls.sha256
grpc_refs=()
while read -r want name; do
    [ -n "$want" ] || continue
    dll="$appbin/$name"
    [ -f "$dll" ] || { echo "FAIL: pinned assembly missing from the client's bin: $name" >&2; exit 1; }
    got="$(shasum -a 256 "$dll" | awk '{print $1}')"
    if [ "$got" != "$want" ]; then
        echo "FAIL: $name is not the pinned build" >&2
        echo "      expected: $want  ($GRPC_SHA_EXPECTED)" >&2
        echo "      actual:   $got" >&2
        echo "      A different assembly means different IL — re-audit before re-freezing." >&2
        exit 1
    fi
    grpc_refs+=(-r "$dll")
done < "$GRPC_SHA_EXPECTED"
echo "OK: ${#grpc_refs[@]} pinned assemblies verified against $GRPC_SHA_EXPECTED"
refs=(-r "$corelib" -r "$nethttp" -r "$dnhttp" "${grpc_refs[@]}")

echo "== 3/6 Measure: the whole Grpc.Net.Client closure, zero gaps, socket subtree cut =="
mout="artifacts/grpcunary-measure"
rm -rf "$mout"; mkdir -p "$mout"
invoke_cli "$app" "${refs[@]}" --auto-ref --measure -o "$mout" | tee "$mout/measure.log"
gaps="$mout/s0-gaps.tsv"
[ -f "$gaps" ] || { echo "FAIL: --measure produced no gap report ($gaps)" >&2; exit 1; }
gapcount="$(grep -c . "$gaps" || true)"
if [ "${gapcount:-0}" -ne 0 ]; then
    echo "FAIL: the gRPC closure has $gapcount transpile gap(s) — the corpus regressed from zero:" >&2
    head -20 "$gaps" >&2
    exit 1
fi
# The same socket/DNS/TLS absence build-and-run-http-get.sh section 4 asserts, held
# on THIS corpus too: Grpc.Net.Client reaches the socket stack through its Balancer
# lane, and the Bounded cut there is what keeps it out — a row here means that cut
# regressed. Zero gaps makes this grep vacuous today; it is the tripwire that speaks
# first if a future gap set returns.
transport_re='System\.Net\.Sockets|System\.Net\.Quic|System\.Net\.NameResolution|System\.Net\.Dns|SslStream|NetworkStream|HttpConnectionPool|SocketsHttpHandler\.(SetupHandlerChain|Send[A-Za-z]*Core|Dispose[A-Za-z]+)|::ConnectAsync|::ConnectCallback|SystemNative_(Socket|Connect|GetHostEntry)|CryptoNative_Ssl'
if grep -qE "$transport_re" "$gaps"; then
    echo "FAIL: a transport-subtree symbol appears in the gRPC gap set — a cut regressed" >&2
    head >&2 <<<"$(grep -E "$transport_re" "$gaps")"
    exit 1
fi
echo "OK: the gRPC closure measures at zero gaps with the socket subtree cut"

echo "== 4/6 Transpiling + building GrpcUnary for real =="
out="artifacts/grpcunary-native"
rm -rf "$out"; mkdir -p "$out"
invoke_cli "$app" "${refs[@]}" --auto-ref -o "$out"
# One cached arm over the compile+link and the live round trip, like
# build-and-run-http2-unary.sh's: the transpile above always runs (it is the surface
# the key hashes); the pinned NuGet assemblies are cache keys alongside the app and
# the shim, so a re-frozen pin re-runs the world.
hit=0
if gate_cache_check "$out" "grpc-unary|$corelib" \
        "$app" "$dnhttp" "$appbin/Grpc.Net.Client.dll" "$appbin/Google.Protobuf.dll" \
        "${app%.dll}.runtimeconfig.json" "${app%.dll}.deps.json"; then
    gate_cache_hit_msg
    hit=1
fi
if [ "$hit" = 0 ]; then
compile_console "$out" grpcunary
[ -x "$out/grpcunary" ] || { echo "FAIL: GrpcUnary native binary was not built" >&2; exit 1; }
echo "OK: the real Grpc.Net.Client transpiles gap-free and links"

echo "== 5/6 Live unary round trips against a real Grpc.AspNetCore server =="
srvdir="$(mktemp -d)"
dotnet "$srv" > "$srvdir/ready.out" 2>"$srvdir/server.err" &
srvpid=$!
disown "$srvpid" 2>/dev/null || true
trap 'kill "$srvpid" 2>/dev/null; rm -rf "$srvdir"' EXIT
ready=""
for _ in $(seq 1 100); do
    [ -s "$srvdir/ready.out" ] && { ready="$(head -1 "$srvdir/ready.out")"; break; }
    sleep 0.1
done
[ -n "$ready" ] || {
    echo "FAIL: the gRPC oracle server never printed READY" >&2
    cat "$srvdir/server.err" >&2; exit 1; }
read -r tag port <<<"$ready"
[ "$tag" = "READY" ] && [ -n "${port:-}" ] || {
    echo "FAIL: could not parse the oracle's READY line: $ready" >&2; exit 1; }
base="http://127.0.0.1:$port"

"$out/grpcunary" "$base" > "$out/native.out" 2>"$out/native.err" || {
    echo "FAIL: native GrpcUnary exited non-zero:" >&2
    cat "$out/native.out" >&2; cat "$out/native.err" >&2; exit 1; }
dotnet "$app" "$base" > "$out/dotnet.out" 2>/dev/null || {
    echo "FAIL: real .NET GrpcUnary exited non-zero" >&2; exit 1; }

# Non-vacuity floors BEFORE the diff (files handed to grep directly — no pipes).
# Each line is a fact only a real round trip of that shape can produce: a
# wire-serialized field echo, a trailer that arrived in the trailer section, a
# server-raised status, and a deadline that FIRED (against a 30 s sleep, so a pass
# by waiting the server out is impossible inside the gate's budget).
grep -qF '[unary] doubled=42' "$out/native.out" || {
    echo "FAIL: native output is missing the unary field echo — no request crossed the wire" >&2
    cat "$out/native.out" >&2; exit 1; }
grep -qF '[meta] trailer x-gate-trailer=tv1' "$out/native.out" || {
    echo "FAIL: native output is missing the trailer readback — trailers did not reach GetTrailers()" >&2
    cat "$out/native.out" >&2; exit 1; }
grep -qF '[fail] status=InvalidArgument' "$out/native.out" || {
    echo "FAIL: native output is missing the server-raised RpcException status" >&2
    cat "$out/native.out" >&2; exit 1; }
grep -qF '[deadline] status=DeadlineExceeded' "$out/native.out" || {
    echo "FAIL: native output is missing DeadlineExceeded — the deadline token did not release the call" >&2
    cat "$out/native.out" >&2; exit 1; }
# Streaming floors. Each is a fact only that streaming shape can produce.
grep -qF '[sstream] tick 3 doubled=6' "$out/native.out" || {
    echo "FAIL: native output is missing the last server-streamed message — the stream did not drain" >&2
    cat "$out/native.out" >&2; exit 1; }
grep -qF '[sfail] got=2 status=Unavailable' "$out/native.out" || {
    echo "FAIL: native output is missing the mid-stream failure shape (2 messages then Unavailable)" >&2
    cat "$out/native.out" >&2; exit 1; }
grep -qF '[cstream] doubled=20' "$out/native.out" || {
    echo "FAIL: native output is missing the client-streaming aggregate — the request stream did not arrive whole" >&2
    cat "$out/native.out" >&2; exit 1; }
grep -qF '[bidi] ok=True echo m3 doubled=6' "$out/native.out" || {
    echo "FAIL: native output is missing the third interlocked bidi echo — a direction stopped flowing incrementally" >&2
    cat "$out/native.out" >&2; exit 1; }
grep -qF '[sdeadline] got=1 status=DeadlineExceeded' "$out/native.out" || {
    echo "FAIL: native output is missing the mid-stream deadline shape (one tick, then abort)" >&2
    cat "$out/native.out" >&2; exit 1; }
# Descriptor-reflection floors. The oneof Clear is ClearKind via
# MethodInfo.Invoke — the exact member whose missing metadata row broke the
# descriptor .cctors; the struct line is JsonFormatter riding the accessors.
grep -qF '[refl] after clear case=none' "$out/native.out" || {
    echo "FAIL: native output is missing the oneof Clear round trip — ClearKind was not invokable" >&2
    cat "$out/native.out" >&2; exit 1; }
grep -qF '[refl] struct=' "$out/native.out" || {
    echo "FAIL: native output is missing the JsonFormatter struct rendering" >&2
    cat "$out/native.out" >&2; exit 1; }
# The startup must be CLEAN. The wire path never consults the reflection
# accessors, so a descriptor-.cctor failure leaves every diff above green while
# spilling its stack traces here — stderr emptiness is the only assert that
# sees that class of regression.
if [ -s "$out/native.err" ]; then
    echo "FAIL: native GrpcUnary wrote to stderr — a startup static constructor failed:" >&2
    cat "$out/native.err" >&2; exit 1
fi

if ! diff -u "$out/dotnet.out" "$out/native.out"; then
    echo "FAIL: native gRPC unary output differs from real .NET" >&2; exit 1
fi
echo "OK: gRPC unary success, headers+trailers, error status and deadline are byte-identical to real .NET ($(grep -c . "$out/native.out") lines)"

echo "== 6/6 Committing the cache =="
gate_cache_commit
fi   # grpcunary arm

echo OK

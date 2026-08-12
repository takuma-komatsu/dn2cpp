#!/usr/bin/env bash
# WASM-axis console gate. Transpiles each program against the tree-shaken real
# CoreLib, builds it with em++ into a wasm32 module (the WASM=1 build axis in
# _common.sh routes the CMake configure through emcmake), runs it under node,
# and diffs the output exactly against real .NET (`dotnet $app`) — proving the
# C# -> IL -> C++ -> em++/wasm32 pipeline end to end. The wasm build is
# single-threaded but now runs the REAL Boehm GC: single-threaded bdwgc, its
# conservative stack scan made sound by Binaryen's SpillPointers pass at link,
# static roots registered via the dn2cpp_roots section. Emscripten's MEMFS
# backs the filesystem.
#
# Five sections, four of them diffed against real .NET. StringCore covers the
# string/formatting core, and
# NestedFinallySubset proves the -fwasm-exceptions EH pipeline — a throw
# (dn2cpp_throw -> C++ `throw Dn2CppException`) unwinding through nested
# finally regions into a typed catch, plus the non-exceptional leave/goto
# finally dispatch.
#
# Finalizers is the COLLECTION ORACLE: it cannot pass on the calloc fallback.
# Its sections loop GC.Collect() + WaitForPendingFinalizers() and never observe
# `finalized 1`, `activator finalized count=2`, or the intentional
# finalizer-thrown abort without a collector actually reclaiming the object — so
# this section going green is proof of real collection on wasm. Verified
# red-on-calloc: under DN2CPP_NO_GC=1 the calloc build prints none of those and
# reaches `UNREACHABLE: process should have aborted`, an exact-diff FAIL vs .NET.
# The diff is stdout-only by design: the Finalizers program's last section
# deliberately aborts in a finalizer, and that abort's exit code under node can
# never match dotnet's SIGABRT — only the stdout is the oracle (assert_output
# compares command-substituted stdout).
#
# WeakReferences rides along as weak-ref-machinery coverage under the real GC
# (WeakReference<T>/TryGetTarget, GC.GetTotalMemory(true), the sweep + heap
# sections), NOT as a collection oracle: its diffable output is collection-
# invariant by construction. The sample deliberately avoids per-object
# null-after-collect (its own comments explain a single referent's collection
# is not reliably observable even on real .NET), and its one aggregate check —
# GC.GetTotalMemory(true) staying under a "everything survived" threshold —
# reads bounded on the calloc fallback too (that fallback reports a small heap),
# so it passes on BOTH axes. It still earns its place: it proves the weak-ref
# code path compiles and runs green under the wasm Boehm GC.
#
# Machines without the Emscripten toolchain (or node) skip: the suite stays
# usable without emscripten, but the runner reports the gate as SKIPPED rather
# than passed (gate_skip in _common.sh).
source "$(dirname "$0")/_common.sh"

# --no-bundled: an -O-less link needs the -debug archives the frozen bundle lacks.
dn2cpp_emsdk_resolve --no-bundled
for tool in em++ emcmake node; do
    if ! command -v "$tool" >/dev/null 2>&1; then
        gate_skip "$tool not found — no Emscripten toolchain to build wasm with"
    fi
done

export WASM=1
wasm_corelib_diff_gate StringCore System.Linq
wasm_corelib_diff_gate NestedFinallySubset
wasm_corelib_diff_gate Finalizers
gate_expected_partial "FinalizerSuppressQueuedSubset's post-enqueue window never opens here: this build never collects an object first named inside a finalizer body while that frame is live, so nothing is observed queued and no setting on either side changes it — the section runs and folds the window into its booleans, and gates/build-and-run-finalizers.sh asserts that surface for real."
wasm_corelib_diff_gate WeakReferences

# ── 5th section: the Web lane's HTTP carve-out ────────────────────────────────
# The one section here that is NOT a diff against real .NET, and it cannot be:
# real .NET has a transport, so the oracle would perform the very request this
# section exists to prove impossible. A browser has no TCP socket layer
# (Emscripten's socket() reaches only a WebSocket proxy), so runtime/CMakeLists.txt
# forces DN2CPP_USE_CURL off on this axis and dn2cpp_http2_stream.cpp's Emscripten
# fallback arm defines the dn2cpp_http2_call_* surface instead — every call failing
# with a sentence naming the cause.
#
# The expectation is frozen inline because that sentence IS the contract: the C++
# arm writes it, DnHttpBackend hands it to HttpRequestException unchanged, and a
# gate that only checked "an exception was thrown" would pass just as happily on
# the benign-stub shape that arm's header forbids. Which is also why this is not
# `grep -q`: the point is that the message survives the whole path intact.
#
# The build succeeding at all is the other half of the assertion — the
# transpiler's HttpClient route names dn2cpp_http2_call_* unconditionally, so a
# regression that removed the fallback arm surfaces here as a wasm-ld failure
# rather than as a diff.
#
# net10.0-pinned like build-and-run-http-get.sh: the sample builds net10.0 and a
# newer preview CoreLib carries a different System.Net.Http surface.
echo "== 5/5 Web-lane HTTP carve-out: HttpClient links, then degrades =="
_wh_corelib=$(resolve_net10_corelib)
_wh_bcl=$(dirname "$_wh_corelib")
_wh_nethttp="$_wh_bcl/System.Net.Http.dll"
[ -f "$_wh_nethttp" ] || { echo "error: System.Net.Http.dll not found: $_wh_nethttp" >&2; exit 1; }
build_proj internal/DnHttp/src/DnHttp/DnHttp.csproj
_wh_dnhttp="internal/DnHttp/src/DnHttp/bin/$CONFIG/$TFM/DnHttp.dll"
[ -f "$_wh_dnhttp" ] || { echo "error: not built: $_wh_dnhttp" >&2; exit 1; }
build_proj samples/dotnet/WasmHttpProbe/WasmHttpProbe.csproj
_wh_app="samples/dotnet/WasmHttpProbe/bin/$CONFIG/$TFM/WasmHttpProbe.dll"
[ -f "$_wh_app" ] || { echo "error: not built: $_wh_app" >&2; exit 1; }

_wh_out=artifacts/wasmhttpprobe-wasm
rm -rf "$_wh_out"; mkdir -p "$_wh_out"
invoke_cli "$_wh_app" -r "$_wh_corelib" -r "$_wh_nethttp" -r "$_wh_dnhttp" --auto-ref -o "$_wh_out"

# The fallback's own words, verbatim (runtime/core/intrinsics/dn2cpp_http2_stream.cpp,
# Emscripten arm). Kept ASCII there precisely so this comparison is byte-stable on
# every host that can run the gate.
#
# Two probes (samples/dotnet/WasmHttpProbe/Program.cs says why the split matters):
# "default" is a DEFAULT-configured HttpClient — its 100 s timeout arms
# CancellationTokenSource.CancelAfter inside Send, which without a thread-free
# arm aborts the module with the THREAD carve-out ("thread constructor failed:
# Not supported") before the handler runs. The runtime's Emscripten CancelAfter
# arm (dn2cpp_tasks.cpp) now arms the deadline with no thread, so the first
# diagnostic a naive port sees is the HTTP carve-out below — the right cause.
# "infinite" keeps the CancelAfter-skipped-entirely shape covered.
_wh_msg='dn2cpp: the Web export has no TCP socket layer, so System.Net.Http has no transport here. Emscripten'"'"'s socket() reaches only a WebSocket proxy, not a server. Use the engine'"'"'s HTTPRequest node for HTTP on the Web.'
_wh_expected='default: caught HttpRequestException
default: message='"$_wh_msg"'
infinite: caught HttpRequestException
infinite: message='"$_wh_msg"'
still running'

if gate_cache_check "$_wh_out" "wasm-http-carveout|$_wh_corelib|$_wh_nethttp|$_wh_expected" \
        "$_wh_app" "${_wh_app%.dll}.runtimeconfig.json" "${_wh_app%.dll}.deps.json" "$_wh_dnhttp"; then
    gate_cache_hit_msg
else
    compile_console_wasm "$_wh_out" WasmHttpProbe
    _wh_actual=$(run_bounded node "./$_wh_out/WasmHttpProbe.js")
    assert_output "$_wh_actual" "$_wh_expected"
    gate_cache_commit
fi

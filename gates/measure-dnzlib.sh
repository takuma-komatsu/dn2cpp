#!/usr/bin/env bash
# DnZlib perf measurement — Stopwatch-timed DnZlib compress/decompress on the
# dn2cpp-transpiled binary, scalar emulation vs Google Highway SIMD backend.
#
# This is a measurement aid, NOT a regression gate: timings vary run-to-run, so
# it prints numbers only and never asserts pass/fail (the `measure-*` name keeps
# it out of run-all-gates.sh, which discovers gates by the `build-and-run-*`
# glob). The correctness cross-check is inside the driver itself
# (samples/dotnet/DnZlibBench/Program.cs SelfCheck — a round-trip mismatch
# aborts before any measurement).
#
# Coverage: the full compress matrix (levels 1 and 6–9, each × text/binary/
# source) plus decompression of level-6 blobs, on both backends. Numbers are
# pasted into internal/DnZlib/README.md's Benchmarks table as dn2cpp columns
# next to the existing BenchmarkDotNet-measured DnZlib/BCL rows.
source "$(dirname "$0")/_common.sh"

OUT=artifacts/dnzlibbench
BLOB_DIR="$OUT/blobs"

echo "== 1/5 Building DnZlibBench C# assembly =="
build_proj samples/dotnet/DnZlibBench/DnZlibBench.csproj
APP="samples/dotnet/DnZlibBench/bin/$CONFIG/$TFM/DnZlibBench.dll"
DNZLIB_REF="samples/dotnet/DnZlibBench/bin/$CONFIG/$TFM/DnZlib.dll"

echo "== 2/5 Producing level-6 zlib blobs via managed .NET =="
# The driver's Decompress rows measure decompression of level-6 blobs supplied
# by managed .NET, so the decompressed input is byte-identical to what the
# BenchmarkDotNet baseline measures. Managed .NET runs the same DLL in
# --produce-blobs mode to write the blobs; the transpiled binary later reads
# them via env vars.
mkdir -p "$BLOB_DIR"
export DN2CPP_DNZLIB_REPO_ROOT="$PWD/internal/DnZlib"
dotnet "$APP" --produce-blobs "$PWD/$BLOB_DIR"

echo "== 3/5 Transpiling IL -> C++ (backend-agnostic, once) =="
CORELIB="$(locate_corelib)"
# No --no-default-ref here, unlike the compression/zip/dnbrotli measure scripts:
# the default-reference injection is conditional on the corresponding BCL being
# in the load set, and this transpile references neither System.IO.Compression
# nor .Brotli (DnZlibBench drives DnZlib's own API directly, with no --auto-ref
# closure to pull them in), so no shim can be injected. Add either of those and
# the condition flips — decline them then, or these timings quietly become
# DnZlib-vs-DnZlib.
invoke_cli "$APP" -r "$CORELIB" -r "$DNZLIB_REF" -o "$OUT"

echo "== 4/5 Building both native binaries (scalar + Highway) from the same C++ =="
compile_console_dual_backend "$OUT" DnZlibBench

echo "== 5/5 Measuring wall-clock (scalar and Highway) =="
export DN2CPP_ZLIB_BLOB_TEXT="$PWD/$BLOB_DIR/text.zlib"
export DN2CPP_ZLIB_BLOB_BINARY="$PWD/$BLOB_DIR/binary.zlib"

run_binary() {
    local bin="$1" label="$2"
    echo
    echo "──────── $label ────────"
    "$bin"
}

run_binary "$OUT/DnZlibBench.scalar" "scalar emulation"
run_binary "$OUT/DnZlibBench.hwy"    "Highway SIMD"

echo
echo "Reading: rows are 'compress L<level> <input> mean_us=<μs>' and 'decompress L6"
echo "<input> mean_us=<μs>', in microseconds per invocation (matches"
echo "BenchmarkDotNet's Mean). Paste these into internal/DnZlib/README.md's"
echo "Benchmarks table next to the existing DnZlib / BCL columns."

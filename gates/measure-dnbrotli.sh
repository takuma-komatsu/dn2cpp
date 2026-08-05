#!/usr/bin/env bash
# DnBrotli perf measurement — Stopwatch-timed DnBrotli compress/decompress on the
# dn2cpp-transpiled binary, scalar emulation vs Google Highway SIMD backend.
#
# This is a measurement aid, NOT a regression gate: timings vary run-to-run, so
# it prints numbers only and never asserts pass/fail (the `measure-*` name keeps
# it out of run-all-gates.sh, which discovers gates by the `build-and-run-*`
# glob). The correctness cross-check is inside the driver itself
# (samples/dotnet/DnBrotliBench/Program.cs SelfCheck — a round-trip mismatch
# aborts before any measurement).
#
# Coverage: the full compress matrix (qualities 1 and 4/9/11, each × text/
# binary/source) plus decompression of quality-4 blobs, on both backends.
# Numbers are pasted into internal/DnBrotli/README.md's Benchmarks section as
# dn2cpp columns next to the managed quick-mode rows.
#
# Blob choice: unlike DnZlib (whose blobs come from DnZlib itself under managed
# .NET — for zlib the two encoders' streams are equally "real"), the brotli
# blobs are produced by the BCL (= native brotli) under managed .NET, so the
# decompress rows measure decoding of real-world-encoder output rather than
# DnBrotli's own stream (byte-identical to native v1.1.0 today, but that is
# implementation-defined and nothing here relies on it). The same BCL-produced
# bytes are what DnBrotli.Benchmarks' DecompressBenchmarks baseline decodes, so
# the rows stay comparable. Because the driver therefore references the real
# System.IO.Compression[.Brotli], the transpile pins net10.0 and passes those
# assemblies like the compression-dnbrotli gate does; DnBrotli's
# [NativeImplementation] swap keeps the resulting binary native-brotli-free.
source "$(dirname "$0")/_common.sh"

OUT=artifacts/dnbrotlibench
BLOB_DIR="$OUT/blobs"

echo "== 1/5 Building DnBrotliBench C# assembly =="
build_proj samples/dotnet/DnBrotliBench/DnBrotliBench.csproj
APP="samples/dotnet/DnBrotliBench/bin/$CONFIG/$TFM/DnBrotliBench.dll"
DNBROTLI_REF="samples/dotnet/DnBrotliBench/bin/$CONFIG/$TFM/DnBrotli.dll"

echo "== 2/5 Producing quality-4 brotli blobs via managed .NET (BCL = native brotli) =="
mkdir -p "$BLOB_DIR"
export DN2CPP_DNBROTLI_REPO_ROOT="$PWD/internal/DnBrotli"
dotnet "$APP" --produce-blobs "$PWD/$BLOB_DIR"

echo "== 3/5 Transpiling IL -> C++ (backend-agnostic, once) =="
CORELIB="$(resolve_net10_corelib)"
BCL="$(dirname "$CORELIB")"
# --no-default-ref DnZlib keeps this benchmark measuring what it says it
# measures: managed brotli against the NATIVE zlib. System.IO.Compression is in
# the load set, so without the flag the transpiler injects the DnZlib shim too
# (Compilation.InjectDefaultRefs) and the zlib side of every row would silently
# become a second managed codec. DnBrotli needs no flag — the explicit -r below
# wins, and the injection skips an already-loaded simple name.
invoke_cli "$APP" -r "$CORELIB" \
    -r "$BCL/System.IO.Compression.dll" \
    -r "$BCL/System.IO.Compression.Brotli.dll" \
    -r "$DNBROTLI_REF" --no-default-ref DnZlib --auto-ref -o "$OUT"

echo "== 4/5 Building both native binaries (scalar + Highway) from the same C++ =="
compile_console_dual_backend "$OUT" DnBrotliBench

echo "== 5/5 Measuring wall-clock (scalar and Highway) =="
export DN2CPP_BROTLI_BLOB_TEXT="$PWD/$BLOB_DIR/text.br"
export DN2CPP_BROTLI_BLOB_BINARY="$PWD/$BLOB_DIR/binary.br"

run_binary() {
    local bin="$1" label="$2"
    echo
    echo "──────── $label ────────"
    "$bin"
}

run_binary "$OUT/DnBrotliBench.scalar" "scalar emulation"
run_binary "$OUT/DnBrotliBench.hwy"    "Highway SIMD"

echo
echo "Reading: rows are 'compress q<quality> <input> mean_us=<μs>' and 'decompress q4"
echo "<input> mean_us=<μs>', in microseconds per invocation (matches"
echo "BenchmarkDotNet's Mean). Paste these into internal/DnBrotli/README.md's"
echo "Benchmarks section next to the managed quick-mode rows."

# DnBrotli

A complete, from-scratch **Pure C# reimplementation of brotli** (RFC 7932), ported from
**google/brotli v1.1.0** — the exact tree vendored at `third_party/brotli/` in this repository.
No P/Invoke, no native dependencies, zero package references. The sibling project of
[`internal/DnZlib/`](../DnZlib/README.md), sharing its concept and discipline:

1. **Interoperability** — compress here → decompress with native brotli /
   `System.IO.Compression`, and vice versa, across all qualities (0–11) and window sizes
   (10–24), verified in both directions against the BCL oracle.
2. **Performance** — competitive with the native brotli behind `System.IO.Compression`;
   `unsafe` pointer internals, unmanaged-memory state mirroring the C ABI, and
   architecture-generic `Vector128<T>` SIMD only, every vector path gated on
   `IsHardwareAccelerated` with an **independently correct** scalar fallback.
3. **Transpiler-friendliness** — developed inside the dn2cpp monorepo. `Dn2CppInterop/`
   implements the twelve raw brotli entry points the BCL P/Invokes (`BrotliEncoderCompress`,
   `BrotliDecoderDecompressStream`, ...) as `[NativeImplementation]` adapters over the pure-C#
   engine's `RawBrotli` face, so a dn2cpp transpile that references DnBrotli runs
   `BrotliStream`/`BrotliEncoder`/`BrotliDecoder` fully managed and drops the vendored native
   brotli out of the binary.

Byte-identical output to the reference encoder is **not** a goal in principle (brotli's
compressed encoding is implementation-defined) — determinism is: the same input produces the
same output on every host and dn2cpp backend. In practice the encoder does match native brotli
v1.1.0 byte for byte across the corpus, which `gates/build-and-run-compression-dnbrotli.sh`
asserts end to end (exact diff vs real .NET, plus `nm` showing a `System.IO.Compression` with
zero native codec symbols).

## Scope

Complete: `c/common`, the full decoder (`decode.c` state machine, bit reader, Huffman, static
dictionary + transforms), and the full encoder — qualities 0–1 (`compress_fragment[_two_pass]`),
the generic path with hashers H2–H6/H40–42, and the zopfli-style qualities 10–11
(`backward_references_hq`, binary-tree hasher H10).

Deferred as unreachable from the BCL's twelve P/Invoke symbols: large-window brotli
(lgwin > 24), and shared/compound custom dictionaries.

## Layout

```
src/DnBrotli/
  BrotliEnums.cs BrotliException.cs      public enums (values == C headers) + exception
  Common/                                constants, context, transforms, static dictionary
  Dec/                                   decoder engine (port of c/dec)
  Enc/                                   encoder engine (port of c/enc)
  Raw/                                   RawBrotli: the brotli C ABI shape in pure C#
  Streams/                               BrotliStream (BCL-shaped)
  Dn2CppInterop/                         [NativeImplementation] swap surface (12 entry points)
tests/DnBrotli.Tests/                    xUnit; oracle = System.IO.Compression (real native brotli)
bench/DnBrotli.Benchmarks/               BenchmarkDotNet suites + `-- quick` stopwatch mode
```

See [PORTING.md](PORTING.md) for the conventions every engine file follows — the port is written
for line-level diffability against the C source.

## Benchmarks

Three comparison points, over ~1 MiB corpora (`text` = a closed 12-word synthetic vocabulary,
`binary` = a deterministic pseudo-binary pattern, `source` = this repo's own concatenated `.cs`
source; see `bench/DnBrotli.Benchmarks/BenchData.cs`). Every row runs both encoders at the
**same quality and window** via the BCL's static `BrotliEncoder.TryCompress(quality, window)`
overload, so the comparison is like-for-like — and since the compressed bytes are identical, so
is the ratio, by construction. Numbers are machine-relative; re-derive rather than quote:

```sh
dotnet run -c Release --project bench/DnBrotli.Benchmarks -- quick    # stopwatch mode
dotnet run -c Release --project bench/DnBrotli.Benchmarks -- \
  --filter "DnBrotli.Benchmarks.CompressBenchmarks.*"                 # full BenchmarkDotNet
./gates/measure-dnbrotli.sh    # from the dn2cpp repo root: the transpiled builds
```

Shape of the result on Apple Silicon: under managed .NET, compression and decompression both sit
somewhat below native brotli's throughput across every quality. Transpiled through dn2cpp
(IL → C++ → clang, no CoreCLR/RyuJIT, Boehm GC) compression reaches native parity or better and
beats RyuJIT on the same C# at every quality, while decompression trails native — the decoder's
table-walk inner loop pays dn2cpp's bounds/GC-model overhead with no vector path to reclaim it.
The opt-in Google Highway backend (real NEON/SSE2 behind `Vector128<T>`) is close to a wash:
DnBrotli's hot loops are hash- and table-bound, and its few vector paths sit off the critical
path at these qualities.

Only the one-shot `Brotli.Compress`/`Decompress` API is measured against a BCL twin.
`BrotliStream`, tiny payloads and incompressible input are not in that table;
`SmallChunkDecompressBenchmarks` (64-byte drain via the resumable `BrotliDecoder`) has no BCL
equivalent and is BenchmarkDotNet-only.

Two costs found through those measurements are worth knowing because both are general:
a per-call `stackalloc` in the hot match finder blocks RyuJIT inlining and pays `localsinit`
zeroing per position (use a fixed-size buffer, as the C uses `size_t keys[BUCKET_SWEEP]`), and
policy constants expressed as properties — the C `#define`s turned policy-struct members — must
carry `AggressiveInlining` or a transpile emits them as out-of-line cross-TU calls inside the
per-byte loop, defeating constant folding.

## License

MIT (see [LICENSE](LICENSE)); ported from google/brotli v1.1.0, whose MIT notice is retained in
[THIRD-PARTY-NOTICES.txt](THIRD-PARTY-NOTICES.txt) and must accompany distributions.

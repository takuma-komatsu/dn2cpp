# DnZlib

A complete, from-scratch **Pure C#** reimplementation of [zlib](https://github.com/madler/zlib)
(RFC 1950/1951/1952) targeting `net10.0`, with an `unsafe`/pointer-based, SIMD-accelerated
internal implementation. No P/Invoke, no native dependencies — including its lowest-level API,
which mirrors zlib's C ABI shape (`z_stream`, `deflate`, `inflate`, ...) entirely in managed
code rather than binding to a real `libz`.

## Features

- **Inflate** (decompression): full state machine for raw DEFLATE, zlib and gzip, plus
  auto-detect. Unsafe fast loop with a 64-bit bit accumulator and a generic `Vector128<T>` SIMD
  LZ77 match-copy (`chunkcopy`, covering both sliding-window and in-output-overlap copies).
- **Deflate** (compression): faithful port of zlib's engine — `stored`/`fast`/`slow`/`rle`/`huff`
  strategies, levels 0–9, static & dynamic Huffman, lazy matching, preset dictionaries. Level 1
  uses a zlib-ng-style `deflate_quick` fast path; levels 4–9 use `deflate_medium` over a
  zlib-ng-style 4-byte hash, a fast-zlib `longest_match` (width-adaptive 2/4/8-byte early-out)
  and generic `Vector128<T>` `compare256`/`slide_hash` (one architecture-independent path the JIT
  lowers to NEON on arm64, SSE2 on x86). Levels 8–9 additionally consult zlib-ng's rolling-hash
  `longest_match_slow` — see Design notes.
- **Checksums**: `Adler32` (generic `Vector128<T>` SIMD — widen + multiply + horizontal-sum,
  byte-exact with the scalar `adler32_z`) and `Crc32` (scalar slice-by-16), each with `Combine`.
- **APIs**, lowest to highest:
  - `DnZlib.Raw.RawZlib` + `z_stream` — the zlib C ABI shape itself (`deflateInit2_`, `deflate`,
    `inflate`, `compress2`, `adler32`, `zlibVersion`, ...) in pure C#. Nothing here does real
    interop; the function/struct shapes are exactly what a P/Invoke binding over `libz` would
    look like, so swapping one in later would change no call site.
  - `ZStream` + `Deflate`/`Inflate` — a C#-idiomatic stream state machine over `RawZlib`.
  - `Zlib.Compress`/`Uncompress`/`TryCompress`/`TryUncompress` — one-shot helpers.
  - `DeflateStream`, `ZLibStream`, `GZipStream` (sync + async) — mirroring the BCL, but exposing
    the `WindowBits`/`MemLevel`/dictionary knobs it hides.
- **Interoperable** in both directions with native zlib and `System.IO.Compression`.

## Usage

```csharp
using DnZlib;
using DnZlib.Streams;

// One-shot
byte[] packed   = Zlib.Compress(data, level: 6, ZlibFormat.Gzip);
byte[] original = Zlib.Uncompress(packed, ZlibFormat.AutoDetect);

// Stream (drop-in for System.IO.Compression)
using var gz = new GZipStream(output, CompressionMode.Compress);
await gz.WriteAsync(data);

// Low-level, zlib-compatible
using var zs = new ZStream();
zs.DeflateInit2(9, CompressionMethod.Deflated, 15, 8, CompressionStrategy.Default);
zs.Input = data; zs.Output = buffer;
zs.Deflate(FlushMode.Finish);

// Raw C-ABI-shaped, one level lower still — what ZStream itself calls
using DnZlib.Raw;
unsafe
{
    z_stream strm = default;
    RawZlib.deflateInit2(&strm, 9, (int)CompressionMethod.Deflated, 15, 8, (int)CompressionStrategy.Default);
    // ... set strm.next_in/avail_in/next_out/avail_out, call RawZlib.deflate(&strm, flush) ...
    RawZlib.deflateEnd(&strm);
}
```

## Layout

```
src/DnZlib/            the library
  Zlib.cs                 one-shot API (Compress/Uncompress/Try*)
  ZStream.cs              public low-level stream state machine
  ZlibEnums.cs            public enums (ZlibFormat, FlushMode, CompressionStrategy, ...)
  Raw/                    z_stream + RawZlib — the zlib C ABI shape, in Pure C#
  Checksums/              Adler32 (Vector128 SIMD), Crc32 (scalar)
  Inflate/                InfTrees, InflateEngine, Simd/{ChunkCopy}
  Deflate/                Trees, DeflateEngine, Simd/{Compare256, SlideHash}
  Streams/                DeflateStream, ZLibStream, GZipStream
  Internal/               shared low-level constants/helpers
tests/DnZlib.Tests/    xUnit — vs native libz + System.IO.Compression + fuzz
bench/DnZlib.Benchmarks/  BenchmarkDotNet (+ `-- quick` stopwatch mode)
```

## Build & test

Requires the .NET 10 SDK (pinned via `global.json`).

```sh
dotnet build -c Release
dotnet test  -c Release
dotnet run   -c Release --project bench/DnZlib.Benchmarks -- quick
```

## Benchmarks

The BenchmarkDotNet suites compare DnZlib against `System.IO.Compression` (native zlib-ng) at the
**same numeric level** — via net10's `ZLibCompressionOptions`, since the `CompressionLevel` enum
only exposes three points and `Optimal` is level 6, which silently compares different levels.
Corpora are ~1 MiB (`text` = a closed 12-word synthetic vocabulary, a near-worst case for a
chain-based match finder; `binary` = a deterministic pseudo-binary pattern; `source` = this
repo's own concatenated `.cs` source). Numbers are machine-relative; re-derive rather than quote:

```sh
dotnet run -c Release --project bench/DnZlib.Benchmarks -- \
  --filter "DnZlib.Benchmarks.CompressBenchmarks.*" "DnZlib.Benchmarks.DecompressBenchmarks.*"
./gates/measure-dnzlib.sh    # from the dn2cpp repo root: the transpiled builds
```

Shape of the result, measured on Apple Silicon: decompression is consistently faster than the
BCL at essentially identical allocation; compression allocates substantially less at every
level (one `ArrayPool`-backed output buffer vs the BCL's chunked buffering) and is at or near
parity on speed, ahead at levels 8–9 for the algorithmic reason under Design notes and behind
on the pathological `text` at levels 6–7, where the cost is managed-vs-native per-step overhead
in a pure `longest_match` walk. `Crc32` and non-`Zlib` framing are not in these suites.

`gates/measure-dnzlib.sh` adds a third point: the same C# taken to native through dn2cpp
(IL → C++ → clang, no CoreCLR/RyuJIT), under either the default scalar emulation of
`Vector128<T>` or the opt-in Google Highway backend (real NEON/SSE2). Both are correct
end-to-end; the driver self-checks each round trip before measuring.

## Backing `System.IO.Compression` under dn2cpp

`DnZlib.Dn2CppInterop/` carries managed implementations of the eight `CompressionNative_*` entry
points the real CoreLib's `DeflateStream`/`GZipStream`/`ZLibStream` (and `ZipArchive`'s CRC-32)
bottom out in, marked so that dn2cpp substitutes them for its vendored native zlib whenever a
transpiled program references DnZlib — no call-site change, and the native zlib drops out of the
binary. The code is inert under a normal .NET runtime. `build-and-run-compression-dnzlib.sh`
exercises it end to end.

That gate is also what surfaced a class of bug real .NET cannot: `ChunkCopyHelper.CopyOverlapping`
routed an overlapping back-reference with `dist >= ChunkSize` through the disjoint copy path,
whose scalar fallback is a plain `memcpy` with no forward-propagation — correct only because on a
SIMD host the fixed-stride vector path runs instead. The shortcut is now gated on hardware
acceleration, so every `dist < len` takes the RLE-correct scalar loop. **Any `IsHardwareAccelerated`
fast path needs an independently correct scalar twin**: under dn2cpp's default backend the flag is
false, and under real .NET it is effectively always true, so the scalar path is otherwise untested.

## Design notes

- Correctness is validated against a native `libz` oracle and `System.IO.Compression`, in both
  directions, across all levels/strategies/formats, plus streaming, dictionaries, and fuzz
  (truncated/corrupted input never crashes or hangs).
- Compressed output is **not** byte-identical to madler/zlib (by design — the project targets
  interoperability and speed, and adopts zlib-ng-style optimizations), but is always valid DEFLATE.
- Levels 8–9's `deflate_medium` backs its match search with zlib-ng's rolling-hash
  `longest_match_slow` (`LongestMatchSlow` in `DeflateEngine.cs`). On redundant input the primary
  4-byte hash chains fill with short matches, and at these levels' large `max_chain` (1024/4096)
  walking them dominates; after finding a match, a *second*, 3-byte rolling hash (`HashCalcRoll`)
  jumps straight to a more distant chain likely to beat the current best. Three consequences:
  - The rolling hash populates the same `head`/`prev` slots it later looks up, so levels 8–9 must
    switch **every** insertion site (`InsertString`, `FillWindow`, `SetDictionary`, via the
    `RollHash` flag set in `LmInit`) to the 3-byte hash. It is computed statelessly — masked after
    each fold, so it depends only on the 3 bytes at each position and needs no running `ins_h`
    threaded through match skips.
  - The gate is `max_chain >= 1024` (levels 8–9), wider than zlib-ng's level 9 only, because a
    managed chain walk costs more per step. It is deliberately **not** extended to levels 6–7:
    measured, the smaller `max_chain` there makes the walk cheap enough that the offset search's
    own overhead slows non-redundant input for a <2% smaller output, and collapses level 7 into
    level 8.
  - Stock zlib-ng enables it at level 9 only, so at the same numeric level native's level 8 is its
    own worst case on redundant input — which is where DnZlib's largest margin comes from.
- The exhaustive `deflate_slow` stays in the code as a reference/fallback but is selected by no
  level (levels 4–9 use `deflate_medium`), so its `Filtered`/`TooFar` short-match discount is
  unreachable; `CompressionStrategy.Filtered` still round-trips correctly, just without it.
- No architecture-specific intrinsics (no `Arm.AdvSimd`, `X86.Avx2`, ...): SIMD is used only where
  a single generic `Vector128<T>` implementation covers every ISA (`compare256`, `slide_hash`,
  `chunkcopy`, `Adler32`). `Crc32` has no such generic equivalent — ARMv8's `crc32x` and x86
  PCLMULQDQ are unrelated instruction families with nothing portable between them — so it is
  scalar-only.
- Not implemented: gzip header capture/set, `inflateBack`, the `gz*` stdio helpers,
  `DeflateParams`, `Deflate`/`InflateCopy`.

## References

Ported from zlib 1.3.2 (RFC 1950/1951/1952 reference implementation by Jean-loup Gailly and Mark
Adler). Several performance techniques are cross-referenced against two further open-source
implementations, reworked here as architecture-generic `Vector128<T>` C# rather than copied
verbatim:

- [zlib-ng](https://github.com/zlib-ng/zlib-ng) 2.2.5 (zlib License) — `compare256`, `slide_hash`,
  and the `deflate_quick`/`deflate_medium` fast paths noted above (`match_tpl.h`,
  `compare256_neon.c`, `slide_hash_neon.c`, `deflate_quick.c`, `deflate_medium.c`).
- [fast_zlib](https://github.com/gildor2/fast_zlib) (BSD-3-Clause) — the width-adaptive
  `longest_match` early-out.

# Vendored brotli

This directory is a **vendored subset** of google/brotli, compiled directly
into the dn2cpp runtime (IL2CPP-style) to back `System.IO.Compression.Brotli`'s
native P/Invoke surface (`BrotliStream` / `BrotliEncoder` / `BrotliDecoder`).

Unlike the zlib surface (see `third_party/zlib/DN2CPP-VENDORED.md` and
`runtime/core/intrinsics/dn2cpp_zlib_native.cpp`), **no shim TU exists or is
needed**: `System.IO.Compression.Brotli.dll`'s
`[DllImport("libSystem.IO.Compression.Native")]` declarations name the raw
brotli API symbols directly (`BrotliDecoderCreateInstance`,
`BrotliEncoderCompressStream`, ...) — dotnet/runtime's own
`entrypoints.c` simply re-exports the brotli library's public symbols, with no
`CompressionNative_*`-style wrapper layer — so the transpiler's lowered
P/Invoke calls link straight against this vendored library's own exports. The
twelve symbols reached from managed code: `BrotliDecoderCreateInstance`,
`BrotliDecoderDecompressStream`, `BrotliDecoderDecompress`,
`BrotliDecoderDestroyInstance`, `BrotliDecoderIsFinished`,
`BrotliEncoderCreateInstance`, `BrotliEncoderSetParameter`,
`BrotliEncoderCompressStream`, `BrotliEncoderHasMoreOutput`,
`BrotliEncoderDestroyInstance`, `BrotliEncoderCompress`,
`BrotliEncoderMaxCompressedSize` (all confirmed exported by the vendored
objects with `nm` before committing to this file set).

## Version

- **brotli 1.1.0** (`c/common/version.h`: `BROTLI_VERSION_MAJOR 1` /
  `MINOR 1` / `PATCH 0`).
- Upstream: https://github.com/google/brotli — release tarball
  `https://github.com/google/brotli/archive/refs/tags/v1.1.0.tar.gz`
  (sha256 `e720a6ca29428b803f4ad165371771f5398faba397edf6778837a18599ea13ff`).
- **This is the same version real .NET links**: dotnet/runtime v10.0.1's
  `src/native/external/brotli-version.txt` records v1.1.0, plus one
  cherry-picked upstream commit (`85d88cbfc2b742e0742286ec277b73bdbf7be433`,
  "Fix C4224 warnings when building with MSVC") that dn2cpp deliberately does
  NOT apply — see "Modifications to vendored source" below.

Byte-exact parity with real .NET's compressed output is neither required nor
guaranteed (the same discipline as the zlib vendor): brotli's compressed
encoding is implementation-/version-defined, so the gates only ever assert
round-trip/decompression correctness and never print compressed bytes or
lengths.

## How it is built

Brotli is vendored in its normal multi-file layout (the upstream `c/` tree,
relative includes intact) and compiled as one static library from 31 C TUs:
`runtime/CMakeLists.txt`'s `dn2cpp_brotli` target `file(GLOB ...)`s every
`.c` under `c/common/`, `c/dec/` and `c/enc/`, with `c/include/` as the
public include dir (the sources include their own API headers as
`<brotli/decode.h>` etc.) — the exact `dn2cpp_zlib` pattern.

Confirmed compile flags (Apple clang, macOS arm64; also intended for Linux —
plain portable C, no configure step):

```sh
clang -c -std=c11 -O2 -fPIC -w -Ithird_party/brotli/c/include \
    third_party/brotli/c/<dir>/<file>.c -o <file>.o
```

- No special `-D` defines are needed. `c/common/platform.h` resolves all its
  platform/endianness/attribute probes from compiler builtins with a plain
  clang toolchain and no configure step. Confirmed via a standalone smoke
  compile of every vendored `.c` file before committing to this file set.
- **`-w`** — third-party source, suppress its warnings (matches the
  `dn2cpp_gc` / `dn2cpp_zlib` convention for vendored vs. first-party code).
- `-fPIC` comes from the project's `CMAKE_POSITION_INDEPENDENT_CODE ON`
  (needed so the objects link into both a console executable and a
  GDExtension shared library); passed explicitly here only because this flag
  list documents a standalone manual compile.

## What was kept / dropped

Kept (the complete C library — common runtime + decoder + encoder — plus its
public API headers; brotli's `.c` granularity does not permit a
per-entry-point cut the way zlib's did, and the encoder/decoder halves are
each internally entangled, so the whole `c/{common,dec,enc}` set is the
minimal buildable unit):

- `c/common/*.c` / `*.h` (13 files),
- `c/dec/*.c` / `*.h` (8 files),
- `c/enc/*.c` / `*.h` (65 files — including the `*_inc.h` template headers
  the `.c` files `#include` with parameter macros set),
- `c/include/brotli/*.h` (5 files: `decode.h`, `encode.h`, `port.h`,
  `shared_dictionary.h`, `types.h`),
- `LICENSE` (92 files total). The upstream directory structure is preserved
  because `c/dec`/`c/enc` include `c/common` headers by relative path
  (`#include "../common/platform.h"`).

Dropped: `c/tools/` (the `brotli` CLI executable), and all non-C-library
trees and build/doc files from the tarball root — `docs/`, `python/`,
`scripts/`, `tests/`, `BUILD.bazel`, `WORKSPACE.bazel`,
`compiler_config_setting.bzl`, `CMakeLists.txt`, `CHANGELOG.md`,
`CONTRIBUTING.md`, `MANIFEST.in`, `README`, `README.md`, `SECURITY.md`,
`setup.cfg`, `setup.py`.

## Modifications to vendored source

**None.** Every vendored file is byte-identical to the v1.1.0 release
tarball.

One deliberate divergence from dotnet/runtime's own brotli vendor is worth
recording: dotnet/runtime additionally cherry-picks upstream commit
`85d88cbfc2b742e0742286ec277b73bdbf7be433` ("Fix C4224 warnings when building
with MSVC" — inserting `(brotli_reg_t)`/`(size_t)` casts on eleven `1u <<
n`-style shifts). dn2cpp does not apply it: it is an MSVC *warning* fix, not
a behaviour fix — every affected shift amount is structurally bounded well
below 32 (varint tail-lengths <= 8, context-map run-length codes, distance
postfix bits <= 3, `lgwin`/ring-buffer bits <= 30), so the widened-before-
shift form computes identical values — and this project's builds compile the
vendored tree with clang/`-w`. Keeping the tree byte-identical to the release
tarball makes future updates and audits simpler. If a Windows/MSVC build arm
ever compiles this tree, revisit (the commit is already in upstream releases
after v1.1.0, so a version bump would absorb it naturally).

## License

MIT (see [`LICENSE`](LICENSE), copied verbatim from the release tarball).

## Updating to a new version

1. Download the new release tarball and copy over the same subset listed
   above (`c/common`, `c/dec`, `c/enc`, `c/include/brotli`, `LICENSE`,
   directory structure intact).
2. Re-check dotnet/runtime's `src/native/external/brotli-version.txt` at the
   .NET tag the transpiler targets — staying on the same brotli version real
   .NET links keeps behaviour comparisons trivial — and re-check whether any
   patch it applies has become behaviour-relevant.
3. Re-run a standalone smoke compile of every `.c` file (see "How it is
   built"), and re-confirm with `nm` that the twelve P/Invoke symbols above
   are still exported, then `cmake --build` a runtime/gate to confirm the
   CMake wiring still picks the tree up (the `dn2cpp_brotli` target globs
   `c/{common,dec,enc}/*.c`, so same-named file swaps need no
   `CMakeLists.txt` edit; re-run cmake to re-glob if files were added or
   removed).
4. Update the version/URL/sha256 above.

# Vendored zlib

This directory is a **vendored subset** of classic zlib, compiled directly
into the dn2cpp runtime (IL2CPP-style) to back `System.IO.Compression`'s
`DeflateStream`/`GZipStream` native P/Invoke surface
(`libSystem.IO.Compression.Native`'s `CompressionNative_*` entry points; see
`runtime/core/intrinsics/dn2cpp_zlib_native.cpp`).

## Version

- **zlib 1.3.2** (`zlib.h`: `ZLIB_VERSION "1.3.2"`).
- Upstream: https://github.com/madler/zlib — release tarball
  `https://github.com/madler/zlib/releases/download/v1.3.2/zlib-1.3.2.tar.gz`
  (sha256 `bb329a0a2cd0274d05519d61c667c062e06990d72e125ee2dfa8de64f0119d16`).
  Confirmed current (no newer tag) against upstream's tag list as of this
  vendoring.

**Classic zlib, not zlib-ng**, even though the real .NET
`libSystem.IO.Compression.Native` uses zlib-ng internally: DEFLATE
compressed-byte output is implementation-defined, so byte-exact parity with
real .NET's compressed stream is neither required nor guaranteed for this
runtime — only round-trip/decompression correctness, which the DEFLATE/gzip
format spec guarantees regardless of encoder. Classic zlib is a flat,
configure-free amalgamation-style vendor matching this project's existing
style (see `third_party/bdwgc/`); zlib-ng needs CPU-dispatch/configure
machinery that doesn't fit.

## How it is built

Unlike bdw-gc's single-TU amalgamation, zlib is vendored as its normal
multi-file layout and compiled as one static library from several TUs:
`runtime/CMakeLists.txt`'s `dn2cpp_zlib` target `file(GLOB ...)`s every
`third_party/zlib/*.c` and compiles each independently (cached/rebuilt by
CMake as usual; no shell-gate object cache like bdw-gc's needed since no
single TU here is nearly as large as `extra/gc.c`).

Confirmed compile flags (Apple clang, macOS arm64; also intended for Linux —
plain POSIX C, no configure step):

```sh
clang -c -std=c11 -O2 -fPIC -w -Ithird_party/zlib third_party/zlib/<file>.c -o <file>.o
```

- No special `-D` defines are needed. The plain, as-shipped `zconf.h` (not
  the `zconf.h.in` autoconf template) compiles standalone: its `#ifdef`
  guards resolve sensible platform defaults with a plain POSIX/clang
  toolchain and no configure step. Confirmed via a standalone smoke compile
  of every vendored `.c` file (see below) before committing to this file set.
- **`-w`** — third-party source, suppress its warnings (matches the
  `dn2cpp_gc` / `dn2cpp_runtime` convention for vendored vs. first-party
  code).
- `-fPIC` comes from the project's `CMAKE_POSITION_INDEPENDENT_CODE ON`
  (needed so the object links into both a console executable and a
  GDExtension shared library); passed explicitly here only because this
  flag list documents a standalone manual compile.

## What was kept / dropped

Kept (exactly what `pal_zlib.c`'s call graph needs — `deflateInit2`,
`deflate`, `deflateEnd`, `inflateInit2`, `inflate`, `inflateEnd`,
`inflateReset2`, `crc32`, plus libc `calloc`/`free`):
`deflate.c`/`.h`, `inflate.c`/`.h`, `inftrees.c`/`.h`, `inffast.c`/`.h`,
`inffixed.h`, `trees.c`/`.h`, `zutil.c`/`.h`, `adler32.c`, `crc32.c`/`.h`,
`zconf.h`, `zlib.h`, `LICENSE` (19 files total).

Dropped: `gzclose.c`/`gzlib.c`/`gzread.c`/`gzwrite.c`/`gzguts.h` (zlib's own
`gzFile` convenience API — never called; `DeflateStream`/`GZipStream` drive
`deflate`/`inflate` directly against in-memory buffers, not `FILE*`),
`infback.c` (the separate low-memory "inflate back" API, unused),
`compress.c`/`uncompr.c` (the one-shot `compress()`/`uncompress()`
convenience wrappers, unused — dn2cpp drives the streaming `deflate`/
`inflate` state machine directly), `zconf.h.in` (autoconf template; the
shipped plain `zconf.h` is used instead), and all non-source build/doc files
(`CMakeLists.txt`, `Makefile*`, `configure`, `BUILD.bazel`, `MODULE.bazel`,
`*.pdf`, `ChangeLog`, `FAQ`, `README*`, `INDEX`, `treebuild.xml`, `zlib.3`,
`zlib.map`, `zlib.pc*`, `zlibConfig.cmake.in`, `make_vms.com`, and the
`amiga/`, `contrib/`, `doc/`, `examples/`, `msdos/`, `os400/`, `qnx/`,
`test/`, `watcom/`, `win32/` platform/build-system subdirectories).

## Modifications to vendored source

One line-level change from the upstream release, to keep the "drop
`gzguts.h`" decision above clean:

- **`zutil.c`**: removed the unconditional `#ifndef Z_SOLO / #include
  "gzguts.h" / #endif` near the top. Nothing in `zutil.c` actually uses a
  symbol from `gzguts.h` (confirmed by reading the full file) — the include
  is vestigial for this subset. The alternative of defining `Z_SOLO` was
  rejected: `Z_SOLO` also disables `deflateInit2_`/`inflateInit2_`'s
  fallback to the default `zcalloc`/`zcfree` allocator (in this same file)
  when the caller leaves `zalloc`/`zfree` NULL, returning `Z_STREAM_ERROR`
  instead — and the native shim (mirroring real `pal_zlib.c`) relies on
  exactly that fallback (it zero-initializes its `z_stream` and never sets
  `zalloc`/`zfree` itself). Removing just the dead include keeps that
  fallback intact. See the comment left in place at the same spot in
  `zutil.c`.

No other vendored file was modified.

## License

zlib's permissive license (see [`LICENSE`](LICENSE), copied verbatim from
the release tarball); every vendored source file also retains the license
notice in its own header comment.

## Updating to a new version

1. Download the new release tarball and copy over the same 19-file subset
   listed above (re-derive it from `pal_zlib.c`'s call graph if upstream
   ever restructures which `.c` file defines what).
2. Re-apply the `zutil.c` modification above (re-check first that it is
   still necessary/sufficient — a future zlib release could change what
   `zutil.c` includes or needs).
3. Re-run a standalone smoke compile of every `.c` file (see "How it is
   built") before trusting the new `zconf.h` to still need no extra `-D`
   flags, then `cmake --build` a runtime/gate to confirm the CMake wiring
   still picks it up (the `dn2cpp_zlib` target globs `third_party/zlib/*.c`,
   so a same-named file swap needs no `CMakeLists.txt` edit).
4. Update the version/URL/sha256 above.

#!/usr/bin/env bash
# Consolidated in-memory ZipArchive gate: Create-mode round-trips (default +
# every CompressionLevel, subdirectory names, an empty entry), full metadata
# reads over a byte-fixed embedded blob (including CompressedLength/Crc32,
# which are deterministic ONLY because the blob is frozen -- self-created
# archives never print them: real .NET links zlib-ng, the native binary
# classic zlib), Update-mode delete/append/add rewrites, archive/entry
# comments incl. 65535-byte Rune-boundary truncation, and corrupted-archive
# error paths -- transpiled once against the tree-shaken real net10.0
# CoreLib + real System.IO.Compression, and diffed exactly against real .NET.
# Everything is in-memory (MemoryStream); no filesystem/ZipFile surface here.
#
# Pinned to net10.0 (resolve_net10_corelib, not locate_corelib) for the same
# reason the compression-core/JSON gates are: this host has multiple
# side-by-side shared runtimes installed and the System.IO.Compression
# native surface can shift shape across them. See net10_bcl_diff_gate /
# resolve_net10_corelib in _common.sh.
#
# THE --no-default-ref FLAGS ARE WHAT KEEP THIS GATE NATIVE, and they look
# removable because the round-trip diff passes without them. Referencing
# System.IO.Compression makes the transpiler inject the DnZlib shim by default
# (Compilation.InjectDefaultRefs), which substitutes the CompressionNative_*
# P/Invokes — including the CRC-32 this program's every entry goes through —
# with pure C#. Drop the flags and this stops being the native-zlib ZipArchive
# test with nothing going red; the managed-backend ZipCore is already covered
# by `dnzlib_diff_gate ZipCore 0` in build-and-run-compression-dnzlib.sh, which
# asserts the swap with nm rather than trusting the diff. DnBrotli is declined
# too although Zip never uses brotli: --auto-ref pulls the Brotli assembly into
# the closure, so without the flag the shim rides in for nothing.
source "$(dirname "$0")/_common.sh"

net10_bcl_diff_gate \
    --cli-arg --no-default-ref --cli-arg DnZlib \
    --cli-arg --no-default-ref --cli-arg DnBrotli \
    ZipCore System.IO.Compression

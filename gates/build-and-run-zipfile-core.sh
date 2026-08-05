#!/usr/bin/env bash
# Consolidated ZipFile/ZipFileExtensions gate: the real-filesystem Zip face --
# ZipFile.CreateFromDirectory / ExtractToDirectory round trips (nested
# subdirectories, an empty-directory entry, both includeBaseDirectory
# settings), ZipFile.Open in all three modes with the ZipFileExtensions
# file-backed helpers (CreateEntryFromFile / ExtractToFile incl. every
# overwrite shape), and the error paths (missing archive / missing source /
# existing destination / duplicate extraction, printed as exception TYPE
# names only) -- transpiled once against the tree-shaken real net10.0 CoreLib
# + real System.IO.Compression + real System.IO.Compression.ZipFile, and
# diffed exactly against real .NET. This is also the end-to-end exercise of
# the directory-PAL surface (readdir/mkdir/rmdir/utimens/chmod) through
# ZipFile rather than through Directory.* alone.
#
# Determinism: the sample works under fixed pre-cleaned subdirectories of
# Path.GetTempPath() (fresh per run and cleaned in try/finally), prints only
# sorted relative paths / booleans / counts / exception type names, and never
# prints timestamps (DOS-time restore is second-granular) or compressed sizes
# (real .NET links zlib-ng, the native binary classic zlib).
#
# Pinned to net10.0 (resolve_net10_corelib, not locate_corelib) for the same
# reason the compression-core/zip-core gates are: this host has multiple
# side-by-side shared runtimes and the System.IO.Compression native surface
# can shift shape across them.
#
# THE --no-default-ref FLAGS ARE WHAT KEEP THIS GATE NATIVE, and they look
# removable because the round-trip diff passes without them. Referencing
# System.IO.Compression makes the transpiler inject the DnZlib shim by default
# (Compilation.InjectDefaultRefs), substituting the CompressionNative_*
# P/Invokes — deflate and the CRC-32 — with pure C#. Drop the flags and the
# real-filesystem Zip face stops exercising the native codec, silently: nothing
# in this gate's output changes. DnBrotli is declined too although Zip never
# uses brotli: --auto-ref pulls the Brotli assembly into the closure, so
# without the flag that shim rides in for nothing.
source "$(dirname "$0")/_common.sh"

net10_bcl_diff_gate \
    --cli-arg --no-default-ref --cli-arg DnZlib \
    --cli-arg --no-default-ref --cli-arg DnBrotli \
    ZipFileCore System.IO.Compression System.IO.Compression.ZipFile

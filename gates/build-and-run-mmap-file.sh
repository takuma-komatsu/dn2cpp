#!/usr/bin/env bash
# File-backed System.IO.MemoryMappedFiles subset lowered to the dn2cpp_mmap_*
# helpers (POSIX mmap/munmap). The three BCL reference types lower to small by-value
# intrinsic structs (a non-moving handle, like GCHandle): MemoryMappedFile.CreateFromFile
# opens + fstats the file; CreateViewAccessor mmaps a (page-aligned) range; the view's
# Read*/Write* typed accessors + the generic Read/Write/ReadArray/WriteArray<T> forms
# load/store the mapped bytes; the SafeMemoryMappedViewHandle exposes the raw byte*
# (AcquirePointer) that the System.Reflection.Metadata PEReader path scans. The real
# bodies are the SafeHandle/UnmanagedMemoryAccessor + OS-mapping P/Invoke cascade we
# don't model; the members are intercepted at the call site AND excluded from
# reachability. Named maps / cross-process / CreateNew / non-null mapName /
# CreateViewStream / the Windows path are carve-outs (loud NotSupportedException).
#
# The sample takes a scratch directory as args[0]; we give the native build and real
# .NET SEPARATE fresh directories and diff their output exactly — the program prints
# only computed/observed values, never addresses or paths. MemoryMappedFile lives in
# System.IO.MemoryMappedFiles (not CoreLib), so that is referenced alongside CoreLib.
# arm64 macOS is little-endian; no cross-endian assertions. CoreLib only (no Linq shim).
source "$(dirname "$0")/_common.sh"

# The sample takes a scratch directory as args[0]; @SCRATCH@ hands each side
# its own fresh mktemp dir (see the wrapper feature block in _common.sh).
export DN2CPP_GATE_RUN_ARGS='@SCRATCH@'
corelib_diff_gate MmapFile System.IO.MemoryMappedFiles

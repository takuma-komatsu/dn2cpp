#!/usr/bin/env bash
# Consolidated flat-memory / raw-pointer edge gate: reinterpret reads/writes through
# raw (int*)/(short*)/(long*) casts at non-zero offsets, mixed-width overlapping
# reads of the same bytes, pointer arithmetic (ptr + i / ptr - ptr / element stride)
# over stackalloc, fixed heap arrays, and struct buffers, round-tripping a struct
# through a flat byte buffer (raw pointer cast + MemoryMarshal.Write/Read/Cast at an
# offset), and field access on a by-value struct receiver (the surface the
# stfld/ldflda struct-value paths extend). Reinterpret byte order is host-endian;
# the gate diffs the transpiled binary against real .NET on the same machine.
# Diffed exact vs real .NET.
source "$(dirname "$0")/_common.sh"

corelib_diff_gate FlatMemEdges

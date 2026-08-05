#!/usr/bin/env bash
# Consolidated span gate: Span<T> bulk ops, scanning, sort, instance methods,
# IndexOfAny, ReadOnlySpan<byte>, and MemoryMarshal.CreateSpan. Diffed exactly vs
# real .NET.
#
# Also the MemoryMarshal-over-byte-spans section (MemoryMarshalSubset, folded in
# from its own gate). GetReference / Cast / AsBytes / Read / Write / CreateSpan /
# CreateReadOnlySpan are transpiler intrinsics (the real BCL bodies use JIT
# intrinsics we don't model: IsReferenceOrContainsReferences / Unsafe.As / nuint
# math); the transpiler builds the result span's {f__reference, f__length} struct
# directly (widths via StorageOf). TryRead / TryWrite / AsRef transpile from the
# real CoreLib IL — their bodies reduce to Unsafe.* + GetReference, all of which
# the transpiler resolves. Covers narrow/widen Cast, AsBytes, a writable
# GetReference, Read/Write + TryRead/TryWrite round-trips (including the
# too-short-buffer false paths), AsRef over Span/ReadOnlySpan, and CreateSpan over
# a single ref. Reinterpretation byte order is host-endian — the gate compares the
# transpiled binary against real .NET on the same machine, so they agree. Also the
# non-generic GetArrayDataReference(Array) overload (an intrinsic — the real body
# is RawData + MethodTable pointer math): ref byte to element 0 of byte[]/int[]/
# string[] SZ arrays, identity-checked against the generic form and read/written
# through Unsafe.*Unaligned / Unsafe.As. Rank >= 2 operands and the intra-CoreLib
# (MethodDefinition) callers of that overload are ArrayCore's
# ArrayDataRefMdSubset, not this bucket's. That section
# used to assert a hard-coded expected string; folded here it is exact-diffed
# against real .NET instead, i.e. it gained a stronger oracle than it had.
#
# CoreLib only — no extra BCL reference is needed by any section.
# Former gates: span-bulk, span-scan, span-sort, span-instance, span-indexofany,
# readonlyspan-byte, create-span, memorymarshal-subset.
source "$(dirname "$0")/_common.sh"

corelib_diff_gate SpanOps

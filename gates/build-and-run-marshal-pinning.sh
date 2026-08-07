#!/usr/bin/env bash
# Consolidated Marshal / GCHandle pinning + native-alloc gate over arbitrary
# blittable types (int/long/double/IntPtr fields — no sub-word fields). Locks in the
# conservative-GC pinning integration: GCHandle.Alloc(Pinned)/AddrOfPinnedObject
# read/write through the raw pointer aliasing the managed backing store (byte[],
# int[], double[], struct[]); Alloc(obj)/Target/IsAllocated; Marshal native-heap
# struct marshalling (AllocHGlobal + StructureToPtr/PtrToStructure round-trip,
# generic and non-generic Type-based forms, mutate-through-buffer = real memcpy);
# SizeOf/OffsetOf exactly matching real .NET (including the
# [StructLayout(Size=N)] floor rows — Size is never rounded up to the alignment,
# standalone or as a member, with the unrepresentable non-multiple shape asserted
# as a loud transpile refusal by this gate's negative arm below); Marshal.Copy
# both directions; a
# conservative-GC coherence section (pin -> native write -> heap churn -> free ->
# managed read); the native-memory primitives (Read*/Write* typed accessors,
# ReAllocHGlobal, CoTaskMem allocator); and the NativeMemory bulk byte ops
# (Fill, overlap-safe Copy in both directions, Clear); and the boundary inside
# the native allocator's own error handling (NativeMemoryOverflowSubset), whose
# subject is which failures are catchable rather than any marshalling shape: a
# NativeMemory.Alloc product that does not fit is decided before the allocator is
# asked anything and throws OutOfMemoryException, where the allocator refusing
# stays an abort — the section proves it by running on past the catch.
# Sub-word-field structs
# are carved out (their
# C++ field width is int32-widened, so the marshalled sizeof diverges from .NET).
# No raw addresses are printed. Diffed exact vs real .NET.
# Former gates: native-memory-subset (NativeMemory.{Alloc,AllocZeroed,Realloc,Free}
# + Marshal.{AllocHGlobal,FreeHGlobal} + Marshal.Copy over byte[]/int[]/double[]/
# long[] with startIndex offsets on both ends), struct-marshal-subset (the generic
# SizeOf<T>/StructureToPtr<T>/PtrToStructure<T> round trip) and
# struct-marshal-offsetof-subset (OffsetOf<T>(string)/OffsetOf(Type,string) plus
# the non-generic Type/object overloads, which read instanceSize at run time),
# aligned-alloc-subset (NativeMemory.{AlignedAlloc,AlignedFree,AlignedRealloc}:
# non-multiple byteCount rounded up at two alignments, realloc-grows-and-
# preserves, the zero-byteCount valid pointer) and gchandle-pin-subset (the
# full 20-section GCHandle model — pinning, Free invalidation and its
# propagation through copies, the weak table, ToIntPtr/FromIntPtr, native-side-
# only retention, the measured exception behaviors, the Target setter,
# GetHashCode).
#
# GC-TIMING NOTE. gchandle-pin-subset's sections 13 and 17 read process-wide
# heap state (an aggregate weakness check behind GC.GetTotalMemory(true), and an
# allocation storm forcing block reuse), which makes this fold a hazard.
# It is sound because the section is driven LAST: nothing
# inherits its ~200 MB of churn, and no earlier section leaves a live set within
# an order of magnitude of its 51.2 MB threshold. Measured on the folded bucket
# (instrumented run, then reverted): the native side's post-collection heap is
# 2,854,912 bytes against that 51,200,000-byte threshold — an 18x margin, of
# which the whole bucket prefix is a few MB — and real .NET's is 527,632. Three
# consecutive DN2CPP_GATE_CACHE=0 runs are byte-identical. Keep it last.
source "$(dirname "$0")/_common.sh"

echo "== Asserting [StructLayout(Size=N)] with N not a multiple of the alignment refuses =="
# SUBJECT: the representation-layout refusal for a declared Size the
# C++ layout cannot express — real .NET MEASURES the shape (Size=6 over an int
# reads 6 from sizeof, Unsafe.SizeOf, Marshal.SizeOf and the array stride alike,
# i.e. size 6 with alignment 4 intact), and C++ structurally cannot (sizeof is
# always a multiple of alignof), so the transpile must fail loudly naming the
# type, the Size and the alignment rather than emit a struct whose sizeof
# silently rounds to 8. The representable half of the same measured rule — Size
# floors and is not rounded up, standalone and as a member — is live-diffed in
# this bucket's SizeOfOffsetOfSubset (the sized3/sized6short rows). Deliberately
# NOT cached and ahead of the bucket's cache gate (the trim-reflection typo-arm
# doctrine): a refusal leaves no output surface to key on, and a regression here
# leaves the positive transpile's bytes identical, so a warm cache would replay
# green right over it.
_neg_corelib=$(locate_corelib)
build_proj samples/dotnet/LayoutSizeBad/LayoutSizeBad.csproj
_neg_app="samples/dotnet/LayoutSizeBad/bin/$CONFIG/$TFM/LayoutSizeBad.dll"
_neg_out="artifacts/marshalpinning-layoutsize-neg"
rm -rf "$_neg_out"
_neg_rc=0
_neg_err=$(invoke_cli "$_neg_app" -r "$_neg_corelib" -o "$_neg_out" 2>&1) || _neg_rc=$?
if [ "$_neg_rc" -ne 2 ]; then
    echo "FAIL: the Size=6-over-int transpile exited $_neg_rc (want 2: the non-multiple-Size refusal)" >&2
    printf '%s\n' "$_neg_err" | tail -3 >&2
    exit 1
fi
grep -q "error: .*Sized6Int: Size=6 over fields ending at byte 4 gives a 6-byte struct, not a multiple of its 4-byte alignment" <<<"$_neg_err" \
    || { echo "FAIL: the refusal did not name the type, Size, field end, total and alignment" >&2; printf '%s\n' "$_neg_err" | tail -3 >&2; exit 1; }
echo "non-multiple-Size refusal OK: exit 2, named type, Size, field end, total and alignment"

corelib_diff_gate MarshalPinning

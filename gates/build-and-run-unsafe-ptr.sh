#!/usr/bin/env bash
# Consolidated unsafe/pointer gate: unsafe blocks, Unsafe.Read/Write, the broad
# Unsafe surface, pointer arithmetic, pointer comparison, the fixed statement, and
# C# function pointers (delegate*/calli). Diffed exactly vs real .NET.
#
# Also the `ldftn` section (LdftnExternalSubset, folded in from its own gate):
# `ldftn` whose token is NOT a plain MethodDefinition — a MethodSpecification (a
# generic method instantiated to a delegate, the System.Linq.Utilities.Combine
# Predicates shape) or a MemberReference with a TypeSpecification parent (a method
# on a closed generic type bound to a delegate, the EnumerableSorter<T>
# comparison-helper shape). Both are resolved the same way a call site resolves its
# target, then routed through the existing delegate-adapter (static) / raw-address
# (instance) path; reachability already reaches them via ResolveCallTarget. Also
# covers a generic method on an intrinsic-mapped type (Array.Empty<T>, no transpiled
# body): the emitter synthesizes a per-instantiation body from the intrinsic
# lowering, taken both as a delegate method group and as a raw delegate* address
# (calli); Array.Empty returns the per-element-type cached singleton, so reference
# identity matches .NET. That section drives ClosedStaticDelegateSubset at its own
# tail: the same ldftn + delegate newobj pair where the delegate is CLOSED over the
# static's first argument — the C# extension-method group conversion
# (`array.Contains` as a `Func<T, bool>`, Thrive's MusicCategory shape). Its target
# slot carries the bound argument instead of being ignored, so the delegate's Invoke
# takes one argument fewer than the static and the adapter needs a second shape; one
# static appears in BOTH shapes in one program. Covers reference/interface/base-
# class/array/object/null bound arguments, void and non-void returns, the 0-argument
# closed form, generic statics (including a shared canonical body), LINQ consumption,
# multicast, Target and delegate equality. The two extra BCL references below are
# that section's (System.Runtime for the extension-method shapes, System.Linq for the
# LINQ consumption) — the pointer sections need neither, and this gate is the only
# consumer of the UnsafePtr project, so widening its reference set costs nothing
# elsewhere.
#
# And the CorelibSubset section (folded in from `corelib-subset`),
# whose real subject is neither unsafe nor pointers alone — it is the ORIGINAL
# pre-bucket real-CoreLib smoke, i.e. the cross-assembly resolve + tree-shake
# pipeline itself:
#   * real corlib LEAF bodies transpiled from their IL — char.IsAsciiDigit /
#     IsAsciiLetter / IsAsciiHexDigit, the last of which reaches HexConverter's
#     64-bit bit trick and needs correct shift/conv width+sign handling to answer 6
#     rather than a UB-corrupted 7;
#   * RuntimeHelpers.GetHashCode, a bodyless InternalCall mapped to a runtime
#     intrinsic;
#   * `stackalloc` (the localloc opcode) with byte-addressed pointer arithmetic, and
#     Unsafe.SizeOf / Unsafe.Add, which are [Intrinsic] stubs whose real corlib IL
#     just throws — they must be emitted inline as the C++ pointer ops they stand
#     for rather than transpiled. That pair is why the section lives in THIS bucket;
#   * Span<int> over a stackalloc buffer (ref-field ctor, ref-returning indexer,
#     Slice, Length) and the ThrowHelper-mapped runtime trap an out-of-range index
#     raises, which must be a CATCHABLE IndexOutOfRangeException.
# It is driven last, and it gained an oracle in the fold: the retired gate was a
# `corelib_subset_gate` with NO expected string, so it only ever asserted exit 0.
#
# Former gates: unsafe-block, unsafe-readwrite, unsafe-surface, pointer-arith,
# pointer-compare, fixed-statement, function-pointer, ldftn-external-subset,
# corelib-subset.
source "$(dirname "$0")/_common.sh"

corelib_diff_gate UnsafePtr System.Runtime System.Linq

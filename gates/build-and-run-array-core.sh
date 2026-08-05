#!/usr/bin/env bash
# Consolidated array-core gate. Merges the former per-feature array element/storage
# subset gates into one multi-section program, transpiled once against the
# tree-shaken real CoreLib and diffed exactly against real .NET. Covers basic
# array ops, Contains/IndexOf, Range/index, Array.Resize, Array.Sort, raw data
# references, byte[] handling, GetSubArray (ranges), packed/struct-element arrays,
# array-as-collection (IList/ICollection) APIs, enum-element arrays, and
# System.Buffers.ArrayPool<T>.Shared rented arrays.
# Former gates: array-ops, array-contains, array-range, array-resize, array-sort,
# array-data-ref, byte-array, getsubarray, packed-array, array-collection, enumarray,
# arraypool.
#
# The ArrayPool section runs the REAL SharedArrayPool<T> IL out of CoreLib — TLS
# buckets, per-core partitions, the ConditionalWeakTable registry — which only
# transpiles because ArrayPoolEventSource is a framework provider folded to a no-op
# (so the EventSource -> manifest -> ResourceManager -> ICU cascade never becomes a
# reachability edge) and because DependentHandle, Gen2GcCallback.Register,
# Environment.TickCount64 and Thread.GetCurrentProcessorId are lowered narrowly.
# It is driven FIRST because it is the one section asserting process-wide state
# (Return-then-Rent hands back the SAME instance); the measurements behind that
# placement are in samples/dotnet/ArrayCore/ArrayPoolSubset.cs's header.
#
# ArrayRangeFaultSubset's tail is about the MD (rank>=2) LAYOUT, not about ranges:
# an MD array's header shares no field with the SZ one — the word an SZArray reads
# as its length is the MD rank — so Array.Copy/CopyTo/Clear/Resize reached through
# a System.Array-typed operand are the tree's only readers of that distinction
# It is also the only place the RankException family and Array.Resize's
# ArgumentOutOfRangeException on a negative newSize are asserted at all. Both
# lower to emitter arms, not to BCL IL, so nothing else in the suite can see them.
#
# ArrayRangeFaultSubset is a MEMORY-SAFETY assert, not an exception-name one, and
# it is the only thing in the tree that can see its subject: Array.Copy /
# Array.Clear / Array.CopyTo lower to one memmove/memset off the element
# pointers, so a bad index or length writes over neighbouring heap objects
# Its payload lines — the arrays printed after each REJECTED call —
# carry the assert; the exception names beside them are the cheap half. Do not
# prune it as a duplicate of ArrayNullFaultSubset or ArrayIndexFaultSubset: those
# cover a null operand and a single-element access, neither of which reaches the
# block move's range check.
# BufferBlockCopyFaultSubset is a second MEMORY-SAFETY assert and not a duplicate
# of the one above it: Buffer.BlockCopy counts its offsets and its length in BYTES,
# so its bound is the array's byte extent — which each of the four representations
# states differently and none of them states as a field. It is also the
# only lowering in the tree that must REFUSE an operand outright: .NET rejects a
# non-primitive element type, and a string[] or struct[] blit would move bytes
# across GC-visible fields. Its rank>=2 rows are the tree's only assertion that
# BlockCopy over an MD array is allowed at all, flat and byte-granular.
#
# BufferExtentSubset is about where the ANSWER comes from, not about Buffer.
# Its ByteLength rows read each representation's byte extent directly, which the
# refusal-only rows above can never do. Its System.Array-typed rows are the tree's
# only operands that state no C++ representation at all, so layout, element width and
# element kind must all be recovered from the runtime type-info. And its five
# provenance rows — local, argument, static field, call return, inline allocation —
# pin the element verdict to the operand's STATIC type: an array the C++ runtime
# allocated carries an imprecise handle naming no element type, so a header-side test
# answers "primitive" for a struct[] and blits over GC-visible fields.
#
# JaggedMdArraySubset is about array TYPE IDENTITY, not element access: an
# SZArray-of-MDArray's type-info names its MD element as a linkable constant (the
# static ti_md_<T>), and the runtime interner must answer that same handle for every
# `new T[,]` of the shape — its GetType()==typeof lines are the only place that
# static/interned identity agreement is asserted, and its typeof(int[,][]).Name line
# is the only reader of an MD token whose ELEMENT is itself an array.
#
# Its GetSetByte tail is the only INDEXED read and write of that extent anywhere
# in the tree. Every row above sees the extent as a total — a bound accepted or refused —
# so a base address or an element packing that is wrong inside a correct total passes all
# of them; only Get/SetByte at a named byte offset of each representation can see it. It
# is also where the refusal ORDER is asserted, because .NET bounds the index by
# ByteLength(array) itself: a struct[] answers ArgumentException at an index that is in
# range, and a refused SetByte must leave the array untouched.
# ArrayCopyCompatSubset is Array.Copy's TYPE-compatibility verdict between two
# arrays — a question no other section asks, because every pair above
# agrees on element type. It is the tree's only assertion of the CLR rules the
# runtime verdict (dn2cpp_array_copy_checked) mirrors: normalized-integral raw
# moves, the CanPrimitiveWiden per-element conversions, boxing/unboxing copies
# with their exact-type element faults and partial-write states, the per-element
# downcast arm, and ArrayTypeMismatchException itself — including that a
# zero-length incompatible pair still refuses, and that a typed catch binds the
# runtime handle. Its statically-typed tail pins the EMITTER's screen: a
# concretely-typed mixed pair must funnel into the same verdict, not memmove
# under the source's rep (which was a heap overrun — the pre-fix binary
# SIGSEGVed in this very section).
#
# ArrayDataRefMdSubset is about the ADDRESS an array hands out, not about ranks.
# MemoryMarshal.GetArrayDataReference(Array) states no rank, so the answer comes from the
# runtime type-info; an MD array's elements live in a detached block, so reading one as an
# SZArray returns a pointer into the header and every read off it succeeds with garbage.
# Its dumps are positional because a wrong base and a wrong stride are different failures.
# Its tail is the only place in the tree that reaches that overload from REAL CoreLib
# BODIES — GCHandle.AddrOfPinnedObject and Marshal.UnsafeAddrOfPinnedArrayElement call it
# on the MethodDefinition route, a different asker pair from the MemberReference one every
# other caller uses — and the only reader of RuntimeHelpers.GetElementSize.
#
# InterfaceElementArraySubset is about the EMITTER's element precision, not about
# arrays of interfaces as data: a cross-assembly array element reaches
# CppEmitter.FieldTypeInfoExpr in ResolveTypeToken's degraded External spelling, and
# before the promotion the emitted ti_arr_ stated element OBJECT, so IDisposable[]
# was object[] to every elementType reader. It is the tree's only assertion of that
# precision: GetElementType()/typeof identity on an interface (and delegate) element,
# the covariance verdicts whose discriminating direction is object[] -> IDisposable[]
# reading False, and the Array.Copy pairs the compatibility section leaves out —
# int[] -> IDisposable[] refusing (including at length zero) and the reverse-
# assignable object[] pair cast-checking per element with its partial-write state.
#
# NonArrayOperandSubset is not about arrays at all: it is the evidence that the C++
# runtime's `_dyn` Array helpers can only be entered with a real array, and therefore
# that their non-array arm may stay an abort. The castclass ahead of every such call
# raises a catchable InvalidCastException — for a string, a bare object and a boxed
# struct — so nothing a user writes reaches the abort. Its second half runs the same
# members on real arrays through the same System.Array-typed route, so a regression
# that turned the check into a blanket refusal is red too.
source "$(dirname "$0")/_common.sh"

corelib_diff_gate ArrayCore

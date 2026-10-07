#!/usr/bin/env bash
# Explicit lower bounds preserve array shape, absolute indices, payloads and store fault order.
# Non-generic sort and search use absolute indices, including negative hits and insertion points.
# Wrapped range checks preserve noops and default primitive search slice faults.
# Sort converts element access faults through the same comparer exception boundary.
# Type-only comparer references do not register an undeclared Default static field.
# Non-SZ collection searches call each element's virtual equality for null and self.
# Collection searches preserve wrapped bounds checks and Contains compares absolute indices.
# Consolidated array-core gate. Merges the former per-feature array element/storage
# subset gates into one multi-section program, transpiled once against the
# tree-shaken real CoreLib and diffed exactly against real .NET. Covers basic
# array ops, Contains/IndexOf, Range/index, Array.Resize, Array.Sort, raw data
# references, byte[] handling, GetSubArray (ranges), packed/struct-element arrays,
# array-as-collection (IList/ICollection) APIs, enum-element arrays, and
# System.Buffers.ArrayPool<T>.Shared rented arrays.
# Nested generic interface variance reaches default comparison, array/span/list
# sorting and binary search through public and explicit implementations.
# Covariant reference stores check the actual element type before mutation.
# Multidimensional stores check value type before bounds and retain the typed
# headerless slot representation used by managed byrefs.
# Runtime handle boxing retains the selected payload's ToString override.
# Nullable array display composes generic element identities and array ranks.
# Excessive-rank errors identify the attempted array type and its assembly.
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
# RunCovariant checks the inline copy path when a reference array's runtime
# element type is narrower than its static element type, including sibling
# interfaces that require per-element casts.
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
#
# ArrayPredicateSubset is Array's delegate-driven generics (Find, FindLast, FindAll,
# the FindIndex and FindLastIndex overloads, Exists, TrueForAll, ConvertAll, ForEach,
# AsReadOnly). Their call sites are intercepted with the rest of the intrinsic type
# and call the members' real CoreLib bodies, so its fault rows are the argument order
# and messages those bodies raise through ThrowHelper — including the paramName an
# argument-only sink appends. Its generic callers put a reference-type instantiation
# behind a shared body, which must fall back to per-instantiation bodies because only
# a closed instantiation of an intrinsic type's member is ever reached.
#
# ArraySurfaceSubset is the rest of Array's public non-generic surface. The 64-bit
# index and length overloads, GetLongLength and the constant ICollection/IList
# properties call their real bodies, so a huge index is .NET's
# ArgumentOutOfRangeException rather than a truncated one, and a null receiver still
# faults although a constant body folds it away. Rank and the dimension queries on
# a statically SZ or MD receiver check null and the dimension. ConstrainedCopy moves
# only pairs that need no per-element conversion, CreateInstanceFromArrayType's
# lengths forms check the type's rank, and Initialize runs a struct's explicit
# parameterless constructor, which ILDiet must keep although no IL names it.
# RunDynamicInitialize finds that constructor from a System.Array receiver or
# a method group at runtime.
#
# ArrayResizeSubset's RunSameLength tail asserts that Array.Resize to the array's
# own length keeps the instance — in a local, a field, a generic body and behind a
# covariant slot — while another length, or a null slot, gets a new array of the
# static element type.
# ArrayConstrainedCovarianceSubset pins ConstrainedCopy's runtime array type
# verdict when both operands are statically object[]: an actual string[]
# destination must reject an object[] source before writing, while compatible
# same-type and upcast pairs still copy.
#
# ArrayArgumentCheckSubset pins Sort and Reverse range checks, a null
# Comparison<T>, and unequal span lengths before their unchecked loops.
# ArrayArgumentCheckSubset.RunSearch checks IndexOf, LastIndexOf, Fill and
# BinarySearch ranges before their loops and the non-generic Array
# Sort, Reverse and Copy rank and range messages.
# Array copy, clear, resize and slices preserve fault fields and validation precedence.
# Default comparer concrete families preserve enum widths, boxed order and user comparisons.
source "$(dirname "$0")/_common.sh"

comparison_prior_extra_asserts() {
    local out="$1" native before prefix line
    native=$(run_bounded "./$out/ArrayCore")
    native=$(strip_cr_win "$native")
    before=$(dotnet "$_CG_APP" before-validation-fields)
    before=$(strip_cr_win "$before")
    prefix=$(awk '/^== Array validation fields ==$/ { exit } { print }' <<< "$native")
    assert_output "$prefix" "$before"
    for line in '== Array validation fields ==' \
        'copy:0:0:-1:-1:-1 param=sourceArray' \
        'copy:2:0:-1:-1:-1 param=destinationArray' \
        'copy:1:1:0:-1:1 type=ArgumentException' \
        'copy:1:1:0:-1:1 param=sourceArray' \
        'copy:2:2:-1:0:0 param=sourceIndex' \
        'copy:2:2:0:0:-1 param=length' \
        'clear:0:-1:-1 param=array' \
        'slice:2:0:4:int param=length' \
        'copyto:1:0:0 param=destinationArray' \
        'copyto:1:2:0 type=ArgumentException' \
        'resize int null:-1 param=newSize' \
        'after GC:0 param=sourceArray' \
        'after GC:3 param=length' \
        'int zero slice canonical=True' \
        'byte zero slice canonical=True' \
        'string zero slice canonical=True' \
        'covariant empty canonical=False' \
        'covariant zero slice fresh=True' \
        'Array validation fields end'; do
        grep -Fxq -- "$line" <<< "$native" \
            || { echo "FAIL: ArrayCore validation witness missing: $line" >&2; exit 1; }
    done
}

# Default order and equality preserve the earlier bucket and run the appended cases.
gate_extra_asserts() {
    local out="$1" native before prefix line
    comparison_prior_extra_asserts "$out"
    native=$(run_bounded "./$out/ArrayCore")
    native=$(strip_cr_win "$native")
    before=$(dotnet "$_CG_APP" before-default-comparison)
    before=$(strip_cr_win "$before")
    prefix=$(awk '/^== default comparison validation ==$/ { exit } { print }' <<< "$native")
    assert_output "$prefix" "$before"
    for line in '== default comparison validation ==' \
        '== default order without IComparable<T> ==' \
        '== comparer message UTF-16 ==' \
        'throwing name calls=1' \
        '== non-generic null search equality ==' \
        'value-forward=1:12' \
        'generic-value-forward=0:1' \
        'default comparison validation end'; do
        grep -Fxq "$line" <<< "$native" \
            || { echo "FAIL: ArrayCore comparison coverage missing: $line" >&2; exit 1; }
    done
    before=$(run_bounded dotnet "$_CG_APP" before-comparer-identity)
    prefix=$(awk '/^== default comparer type identity ==$/ { exit } { print }' <<< "$native")
    assert_output "$prefix" "$(strip_cr_win "$before")"
    before=$(run_bounded "./$out/ArrayCore$EXE_EXT" before-comparer-identity)
    assert_output "$prefix" "$(strip_cr_win "$before")"
    for line in '== default comparer type identity ==' \
        'enum-sbyte comparer Name=EnumComparer`1' 'enum-byte comparer Name=EnumComparer`1' \
        'enum-int16 comparer Name=EnumComparer`1' 'enum-uint16 comparer Name=EnumComparer`1' \
        'enum-int32 comparer Name=EnumComparer`1' 'enum-uint32 comparer Name=EnumComparer`1' \
        'enum-int64 comparer Name=EnumComparer`1' 'enum-uint64 comparer Name=EnumComparer`1' \
        'nullable-enum-signed comparer Name=NullableComparer`1' \
        'nullable-enum-unsigned comparer Name=NullableComparer`1' \
        'object comparer Name=ObjectComparer`1' \
        'user-generic comparer Name=GenericComparer`1' \
        'user-nongeneric comparer Name=ObjectComparer`1' \
        'user-uncomparable comparer Name=ObjectComparer`1' \
        'int comparer Name=GenericComparer`1' 'string comparer Name=GenericComparer`1' \
        'user-generic direct=-16/16/-9/0' 'user-generic interface=-16/16/-9/0' \
        'enum-uint64 default sort=Zero,High,Max search=1/-2' \
        'enum-uint64 explicit sort=Zero,High,Max search=1/-2' \
        'default comparer type identity end'; do
        grep -Fxq -- "$line" <<< "$native" \
            || { echo "FAIL: default comparer identity witness missing: $line" >&2; exit 1; }
    done
}

eval "$(declare -f gate_extra_asserts | sed '1s/gate_extra_asserts/nonzero_prior_extra_asserts/')"
# Keep the prior whole bucket and prove each appended lower-bound block ran once.
gate_extra_asserts() {
    local out="$1" native before prefix line
    nonzero_prior_extra_asserts "$out"
    native=$(run_bounded "./$out/ArrayCore$EXE_EXT")
    native=$(strip_cr_win "$native")
    prefix=$(awk '/^== nonzero array lower bounds ==$/ { exit } { print }' <<< "$native")
    before=$(run_bounded dotnet "$_CG_APP" before-nonzero-lower-bounds)
    assert_output "$prefix" "$(strip_cr_win "$before")"
    before=$(run_bounded "./$out/ArrayCore$EXE_EXT" before-nonzero-lower-bounds)
    assert_output "$prefix" "$(strip_cr_win "$before")"
    before=$(run_bounded dotnet "$_CG_APP" before-lower-bound-search)
    prefix=$(awk '/^== lower-bound array sort and search ==$/ { exit } { print }' <<< "$native")
    assert_output "$prefix" "$(strip_cr_win "$before")"
    before=$(run_bounded "./$out/ArrayCore$EXE_EXT" before-lower-bound-search)
    assert_output "$prefix" "$(strip_cr_win "$before")"
    before=$(run_bounded dotnet "$_CG_APP" before-wrapped-array-ranges)
    prefix=$(awk '/^== wrapped lower-bound array ranges ==$/ { exit } { print }' <<< "$native")
    assert_output "$prefix" "$(strip_cr_win "$before")"
    before=$(run_bounded "./$out/ArrayCore$EXE_EXT" before-wrapped-array-ranges)
    assert_output "$prefix" "$(strip_cr_win "$before")"
    before=$(run_bounded dotnet "$_CG_APP" before-sort-access-faults)
    prefix=$(awk '/^== lower-bound sort access faults ==$/ { exit } { print }' <<< "$native")
    assert_output "$prefix" "$(strip_cr_win "$before")"
    before=$(run_bounded "./$out/ArrayCore$EXE_EXT" before-sort-access-faults)
    assert_output "$prefix" "$(strip_cr_win "$before")"
    for line in '== lower-bound sort access faults ==' 'lower-bound sort access faults end' \
        'sort access Int32/2 sort custom=ArgumentException/<none>' \
        'sort access String/2 sort=ArgumentException/<none>' \
        'sort access Object/3 sort custom=ArgumentException/<none>' \
        'sort access Int32/2 sort=ArgumentOutOfRangeException/<none>' \
        'sort access Object/2 reverse=IndexOutOfRangeException/<none>' \
        'sort access String/3 binary=InvalidOperationException/<none>' \
        'sort start Int32/2147483643 default=ArgumentOutOfRangeException/<none>' \
        'sort start Int32/2147483643 custom=ArgumentException/<none>' \
        'sort start String/2147483643 default=ArgumentException/<none>' \
        'sort start Object/2147483643 custom=ArgumentException/<none>'; do
        [[ $(grep -Fxc -- "$line" <<< "$native") == 1 ]] \
            || { echo "FAIL: sort access witness must run once: $line" >&2; exit 1; }
    done
    for line in '== wrapped lower-bound array ranges ==' 'wrapped lower-bound array ranges end' \
        'wrapped Int32/-5/0 sort=ok' 'wrapped Int32/-5/1 reverse=ok' \
        'wrapped Int32/-5/0 binary=ArgumentOutOfRangeException/<none>' \
        'wrapped Int32/-5/0 binary custom=-2147483648' \
        'wrapped Int32/-5/2 sort=ArgumentOutOfRangeException/<none>' \
        'wrapped Int32/-5/2 sort custom=ok' 'wrapped String/-5/2 binary=-2147483648' \
        'wrapped Object/-5/3 reverse=ok' \
        'wrapped String/-5/1 binary=InvalidOperationException/<none>' \
        'wrapped Default=ArgumentOutOfRangeException/<none>' \
        'wrapped DefaultInvariant=-2147483648' 'wrapped new comparer=-2147483648' \
        'wrapped null value=-2147483648' 'wrapped wrong value type=-2147483648'; do
        [[ $(grep -Fxc -- "$line" <<< "$native") == 1 ]] \
            || { echo "FAIL: wrapped range witness must run once: $line" >&2; exit 1; }
    done
    for line in '== nonzero array lower bounds ==' 'nonzero array lower bounds end' \
        '== nonzero covariant array stores ==' 'nonzero covariant array stores end' \
        '== lower-bound array sort and search ==' 'lower-bound array sort and search end' \
        'lower indices=-2/0:-1/-1/-3/-3' 'lower binary=-2/0:-1/0/-2' \
        'lower extreme sort=9 binary=2147483647' 'lower empty min=2147483647/2147483647/2147483647'; do
        [[ $(grep -Fxc -- "$line" <<< "$native") == 1 ]] \
            || { echo "FAIL: lower-bound witness must run once: $line" >&2; exit 1; }
    done
}

assert_runtime_box_formatting() {
    local output="$1" line
    for line in '== runtime-handle boxed formatting ==' \
        'plain copied=handle:17:17' 'plain mutated=handle:23:handle:29:distinct=True' \
        'nullable copied=nullable:31:underlying=True' 'nullable empty=True' \
        'indirect copied=indirect:53:type=True' 'generic payloads=Int32:71/String:79' \
        'ordinary control=ordinary:83/ordinary:89' 'unselected type=UnselectedPayload' \
        'runtime-handle boxed formatting end'; do
        grep -Fxq -- "$line" <<< "$output" \
            || { echo "FAIL: runtime box formatting witness missing: $line" >&2; exit 1; }
    done
}

DN2CPP_GATE_EXTRA_CONTEXT="${DN2CPP_GATE_EXTRA_CONTEXT:-}|array-box-shared-generics|before-array-provenance|before-nested-interface-variance|before-covariant-stores|before-covariant-md-stores|before-runtime-box-formatting|before-nullable-array-names|before-excess-array-rank"
DN2CPP_GATE_EXTRA_CONTEXT="$DN2CPP_GATE_EXTRA_CONTEXT|comparer-identity-prefix-argv:before-comparer-identity|before-nonzero-lower-bounds|rank1-nonsz-collections|static-rank1-md-typespec|before-lower-bound-search|rank1-collection-virtual-equality|rank1-collection-extreme-bounds|before-extreme-collection|before-wrapped-array-ranges|wrapped-range-default-comparer-identity|before-sort-access-faults|sort-window-access-fault-boundary|opaque-comparer-type-only"
DN2CPP_GATE_EXTRA_INPUTS="${DN2CPP_GATE_EXTRA_INPUTS:-} samples/dotnet/ArrayCore/ArrayComparerTypeOnly.csproj samples/dotnet/ArrayCore/ArrayComparerTypeOnlyProgram.cs samples/dotnet/ArrayCore/ArrayLowerBoundsOnly.csproj samples/dotnet/ArrayCore/ArrayLowerBoundsOnlyProgram.cs gates/fixtures/array-rank1-md/Driver.csproj samples/dotnet/ArrayCore/ArrayStaticRankOneOnlyProgram.cs gates/fixtures/array-rank1-md/Generate.csproj gates/fixtures/array-rank1-md/Program.cs samples/dotnet/ArrayCore/BoxProvenanceOnly.csproj samples/dotnet/ArrayCore/BoxProvenanceProgram.cs samples/dotnet/ArrayCore/ReflectionReturnBoxOnly.csproj samples/dotnet/ArrayCore/ReflectionReturnBoxProgram.cs samples/dotnet/ArrayCore/DiamondProvenanceOnly.csproj samples/dotnet/ArrayCore/DiamondProvenanceProgram.cs samples/dotnet/ArrayCore/FieldAliasProvenanceOnly.csproj samples/dotnet/ArrayCore/FieldAliasProvenanceProgram.cs samples/dotnet/ArrayCore/FieldAliasProvenanceSubset.cs samples/dotnet/ArrayCore/ArrayElementAliasProgram.cs samples/dotnet/ArrayCore/ArrayElementAliasOnly.csproj samples/dotnet/ArrayCore/ArrayObjectElementAliasOnly.csproj samples/dotnet/ArrayCore/ArrayUnknownElementAliasOnly.csproj samples/dotnet/ArrayCore/ArrayErasedElementAliasOnly.csproj samples/dotnet/ArrayCore/ArrayReferenceSlotAliasOnly.csproj samples/dotnet/ArrayCore/ArrayReflectedVoidBoxOnly.csproj samples/dotnet/ArrayCore/ArrayReflectedVoidBoxProgram.cs samples/dotnet/ArrayCore/ArrayFutureStoreOnly.csproj samples/dotnet/ArrayCore/ArrayFutureStoreProgram.cs samples/dotnet/ArrayCore/ArrayFutureNullStoreOnly.csproj samples/dotnet/ArrayCore/ArrayObjectFutureStoreOnly.csproj samples/dotnet/ArrayCore/ArrayObjectFutureStoreProgram.cs"
corelib_diff_gate ArrayCore System.Collections

native=$(run_bounded "./$_CG_OUT/ArrayCore$EXE_EXT")
native=$(strip_cr_win "$native")
previous=$(run_bounded dotnet "$_CG_APP" before-runtime-box-formatting)
prefix=$(awk '/^== runtime-handle boxed formatting ==$/ { exit } { print }' <<< "$native")
assert_output "$prefix" "$(strip_cr_win "$previous")"
assert_runtime_box_formatting "$native"
previous=$(run_bounded dotnet "$_CG_APP" before-nullable-array-names)
prefix=$(awk '/^== nullable array type names ==$/ { exit } { print }' <<< "$native")
assert_output "$prefix" "$(strip_cr_win "$previous")"
for line in '== nullable array type names ==' \
    'int text=System.Nullable`1[System.Int32][]' \
    'char text=System.Nullable`1[System.Char][]' \
    'jagged text=System.Nullable`1[System.Int32][][]' \
    'md text=System.Nullable`1[System.Char][,]' \
    'mixed text=System.Nullable`1[System.Int32][,][]' \
    'nullable array identities=True:True:True' 'nullable dynamic names=True:True' \
    'nullable array type names end'; do
    grep -Fxq -- "$line" <<< "$native" \
        || { echo "FAIL: nullable array name witness missing: $line" >&2; exit 1; }
done
previous=$(run_bounded dotnet "$_CG_APP" before-excess-array-rank)
prefix=$(awk '/^== excessive array rank diagnostics ==$/ { exit } { print }' <<< "$native")
assert_output "$prefix" "$(strip_cr_win "$previous")"
for line in '== excessive array rank diagnostics ==' 'rank32 accepted=32:0' \
    'rank invalid length=ArgumentOutOfRangeException:lengths[0]:<null>:Non-negative number required. (Parameter '\''lengths[0]'\'')' \
    'rank null lengths=ArgumentNullException:lengths:<none>:Value cannot be null. (Parameter '\''lengths'\'')' \
    'rank bounds mismatch=ArgumentException:<null>:<none>:Number of lengths and lowerBounds must match.' \
    'excessive array rank diagnostics end'; do
    grep -Fxq -- "$line" <<< "$native" \
        || { echo "FAIL: excessive array rank witness missing: $line" >&2; exit 1; }
done
for label in 'int lengths' 'object lengths' 'char lengths' 'int bounds' 'object bounds' 'char bounds'; do
    grep -Eq "^$label=System.TypeLoadException/80131522:'.*' from assembly '.*' has too many dimensions\\.$" <<< "$native" \
        || { echo "FAIL: excessive array rank diagnostic missing: $label" >&2; exit 1; }
done
previous=$(run_bounded dotnet "$_CG_APP" before-covariant-stores)
prefix=$(awk '/^== covariant reference array stores ==$/ { exit } { print }' <<< "$native")
assert_output "$prefix" "$(strip_cr_win "$previous")"
for line in '== covariant reference array stores ==' \
    'store message=Attempted to access an element as a type incompatible with the array.' \
    'negative before type=IndexOutOfRangeException/80131508:identity=True' \
    'past end before type=IndexOutOfRangeException/80131508:identity=True' \
    'null before type=NullReferenceException/80004003:identity=True' \
    'allocated array identities=True:True:True:True' \
    'array token identities=True:True:True' 'array token casts=True:True:True' \
    'array token cast rejected=InvalidCastException/80004002:identity=True' \
    'list byte array growth=10:True:True' 'list byte array regrowth=True' \
    'list int array growth=2:True' 'list reference array growth=2:True' \
    'constructed mismatch=Attempted to access an element as a type incompatible with the array./80131503' \
    'covariant reference array stores end'; do
    grep -Fxq -- "$line" <<< "$native" \
        || { echo "FAIL: covariant array store witness missing: $line" >&2; exit 1; }
done
for line in 'string rejected' 'argument rejected' 'field rejected' 'return rejected' \
    'shared rejected' 'class rejected' 'interface narrowed rejected' 'interface rejected' \
    'generic invariant rejected' 'generic variance rejected' 'jagged rejected' \
    'intrinsic rejected' 'runtime returned rejected' 'generic list rejected' \
    'allocated array element rejected'; do
    grep -Fxq -- "$line=ArrayTypeMismatchException/80131503:identity=True" <<< "$native" \
        || { echo "FAIL: rejected store changed array contents: $line" >&2; exit 1; }
done
for line in 'string compatible' 'string null' 'shared compatible' 'class compatible' \
    'interface narrowed compatible' 'interface compatible' 'boxed interface compatible' \
    'object boxed compatible' 'object compatible' 'object null' 'generic compatible' \
    'generic variance compatible' 'jagged compatible' 'intrinsic compatible' 'generic list compatible' \
    'allocated byte array compatible' 'allocated int array compatible' \
    'allocated reference array compatible' 'allocated nested array compatible' \
    'allocated object compatible'; do
    grep -Fxq -- "$line=ok:identity=True" <<< "$native" \
        || { echo "FAIL: compatible store lost value identity: $line" >&2; exit 1; }
done
shared_store=$(LC_ALL=C awk '{ sub(/\r$/, "") }
    /^inline void .*_StoreGeneric_TisCnRef_.*\)$/ { in_body = 1 }
    in_body && /dn2cpp_stelem_ref\(/ { found = 1 }
    in_body && /^\}/ { in_body = 0 }
    END { if (found) print "shared reference store" }' "$_CG_OUT"/generated.h)
assert_output "$shared_store" 'shared reference store'
shared_array=$(LC_ALL=C awk '{ sub(/\r$/, "") }
    /^inline Dn2CppArrayRef\* .*_Allocate_TisCnRef_.*\)$/ { in_body = 1 }
    in_body && /dn2cpp_newarr_ref_t\(.*__rgctx/ { found = 1 }
    in_body && /^\}/ { in_body = 0 }
    END { if (found) print "shared array allocation" }' "$_CG_OUT"/generated.h)
assert_output "$shared_array" 'shared array allocation'
previous=$(run_bounded dotnet "$_CG_APP" before-covariant-md-stores)
prefix=$(awk '/^== covariant multidimensional reference stores ==$/ { exit } { print }' <<< "$native")
assert_output "$prefix" "$(strip_cr_win "$previous")"
for line in '== covariant multidimensional reference stores ==' \
    'md store message=Attempted to access an element as a type incompatible with the array.' \
    'md null before type=NullReferenceException/80004003:identity=True' \
    'md culture reads=en-US:True' 'md culture ref read=en-US' \
    'md culture ref write=ja-JP:True' 'md culture null=True' \
    'multidimensional reference stores end'; do
    grep -Fxq -- "$line" <<< "$native" \
        || { echo "FAIL: multidimensional reference store witness missing: $line" >&2; exit 1; }
done
for label in 'md rank2 rejected' 'md rank2 type before negative' 'md rank2 type before past end' \
    'md argument rejected' 'md field rejected' 'md shared rejected' \
    'md rank3 rejected' 'md rank3 type before bounds' 'md rank4 rejected' 'md rank4 type before bounds' \
    'md created rejected' 'md class rejected' 'md interface rejected' 'md jagged rejected'; do
    grep -Fxq -- "$label=ArrayTypeMismatchException/80131503:identity=True" <<< "$native" \
        || { echo "FAIL: multidimensional incompatible store or retention mismatch: $label" >&2; exit 1; }
done
for label in 'md rank2 compatible negative' 'md rank2 null past end' \
    'md rank3 compatible bounds' 'md rank4 compatible bounds'; do
    grep -Fxq -- "$label=IndexOutOfRangeException/80131508:identity=True" <<< "$native" \
        || { echo "FAIL: multidimensional compatible store bounds mismatch: $label" >&2; exit 1; }
done
for label in 'md rank2 compatible' 'md rank2 null' 'md shared compatible' \
    'md rank3 compatible' 'md rank3 null' 'md rank4 compatible' 'md rank4 null' \
    'md created compatible' 'md interface compatible' 'md jagged compatible' 'md object boxed compatible'; do
    grep -Fxq -- "$label=ok:identity=True" <<< "$native" \
        || { echo "FAIL: multidimensional compatible store lost identity: $label" >&2; exit 1; }
done
shared_md_store=$(LC_ALL=C awk '{ sub(/\r$/, "") }
    /^inline void .*_StoreMdGeneric_TisCnRef_.*\)$/ { in_body = 1 }
    in_body && /dn2cpp_array_check_store_ref\(/ { found = 1 }
    in_body && /^\}/ { in_body = 0 }
    END { if (found) print "shared multidimensional store" }' "$_CG_OUT"/generated.h)
assert_output "$shared_md_store" 'shared multidimensional store'
previous=$(run_bounded "./$_CG_OUT/ArrayCore$EXE_EXT" before-array-provenance)
prefix=$(awk '/^-- array shape argument checks --$/ { exit } { print }' <<< "$native")
assert_output "$prefix" "$(strip_cr_win "$previous")"
grep -Fq 'dn2cpp_array_search_equals(' "$_CG_OUT"/generated*.cpp \
    || { echo 'FAIL: non-generic Array search lacks its missing-slot guard' >&2; exit 1; }
for line in '-- array shape argument checks --' 'array shape argument checks end' \
        'initialize-dyn-boxed-equality: True/True' \
        'initialize-bound-boxed-equality: True/True' 'Array Initialize discovery end' \
        '== runtime type-handle boxes and array refusals ==' \
        'int=System.Int32:7:equal=True/True' 'short=System.Int16:-3:equal=True/True' \
        'nullable empty=null' 'reference identity=True' \
        'runtime type-handle boxes and array refusals end'; do
    grep -Fxq -- "$line" <<< "$native" \
        || { echo "FAIL: Array guard witness missing: $line" >&2; exit 1; }
done

previous=$(run_bounded dotnet "$_CG_APP" before-dynamic-array-null-equality)
previous=$(strip_cr_win "$previous")
prefix=$(awk '/^== dynamic array null equality ==$/ { exit } { print }' <<< "$native")
assert_output "$prefix" "$previous"
previous=$(run_bounded dotnet "$_CG_APP" before-array-search-provenance-additions)
previous=$(strip_cr_win "$previous")
prefix=$(awk '/^== array search reference and type provenance ==$/ { exit } { print }' <<< "$native")
assert_output "$prefix" "$previous"
previous=$(run_bounded dotnet "$_CG_APP" before-array-search-loops)
previous=$(strip_cr_win "$previous")
prefix=$(awk '/^== array search loop provenance ==$/ { exit } { print }' <<< "$native")
assert_output "$prefix" "$previous"
for line in '== array search loop provenance ==' \
        'loop-local=0:1:1' 'loop-argument=0:1:1' 'loop-type=0' \
        'loop-type-unwrapped=0:1' 'loop-unsearched-leaf=0' \
        'array search loop provenance end'; do
    grep -Fxq -- "$line" <<< "$native" \
        || { echo "FAIL: array loop witness missing: $line" >&2; exit 1; }
done

previous=$(run_bounded dotnet "$_CG_APP" before-nested-interface-variance)
previous=$(strip_cr_win "$previous")
prefix=$(awk '/^== nested generic interface variance ==$/ { exit } { print }' <<< "$native")
assert_output "$prefix" "$previous"
for line in '== nested generic interface variance ==' \
    'nested direct=-1' 'nested covariance=True' 'nested owner distinct=False' \
    'nested flat neighbor=False' 'nested escape neighbor=False' \
    'variance nested definition=NestedInterfaceVarianceSubset.Outer+IOut`1' \
    'variance flat definition=NestedInterfaceVarianceSubset.Outer_IOut`1' \
    'nested explicit body=1' 'nested explicit interface=-1' \
    'generic covariance=True' 'generic enclosing invariant=False' \
    'generic middle invariant=False' 'nested generic interface variance end'; do
    grep -Fxq -- "$line" <<< "$native" \
        || { echo "FAIL: nested interface variance witness missing: $line" >&2; exit 1; }
done
for name in 'top public' 'top explicit' 'nested public' 'nested explicit' 'deep' 'generic enclosing'; do
    for result in 'default=-1' 'held=1' 'array=1,2,3' 'search=1' 'span=1,2,3' 'list=1,2,3/1'; do
        grep -Fxq -- "$name $result" <<< "$native" \
            || { echo "FAIL: nested interface comparison mouth missing: $name $result" >&2; exit 1; }
    done
done
for line in \
    'direct=0:1' \
    'conditional-first=0:1:0' \
    'conditional-second=0:0:1' \
    'indexed-selected=0:1' \
    'type-overwrite=0:1:TypeOverwrittenDeadMatch' \
    'field-forward=0:1' \
    'field-backward=1:1' \
    'hidden-field=0:1' \
    'field-stored=0:1:1' \
    'ref-field-write=0:1' \
    'ref-static-write=0:1' \
    'array-get-type=0:1' \
    'make-array-type=0:1:True' \
    'object-get-type=0:Object' \
    'nested-array-get-type=0:1' \
    'ranked-array-type=0:1' \
    'helper-ranked-array-type=0:1' \
    'repeated-ranked-array-type=0:1' \
    'ref-stobj=0:1' \
    'ref-ldobj=0:1' \
    'copied-object-get-type=0:Object' \
    'md-get-type=0:1:2' \
    'type-handle=0:1' \
    'empty-type-signature=0:1' \
    'field-alias=0:1' \
    'field-overwrite=0:1:1' \
    'field-virtual=0:1' \
    'static-erased=0:1' \
    'static-declared=0:1' \
    'static-overwrite=0:1:1' \
    'generic-static=0:1,0:1' \
    'struct-field=0:1' \
    'box-overwrite=0:1:True' \
    'cctor-static=0:1' \
    'constructor-field=0:1' \
    'constructor-overwrite=0:1:1' \
    'cctor-overwrite=0:1:1' \
    'property=1:1' \
    'setter-only=0:1' \
    'mixed-visibility=0:1' \
    'method=0:1' \
    'generic-argument=1:1' \
    'generic-selected=0:1' \
    'interface-provider=0:1' \
    'overload=0:1' \
    'nonzero-overload=0:1' \
    'local-after-create=0:1:UnselectedLocalMatch' \
    'helper-other-call=0:1:2' \
    'helper-sink=0:1' \
    'field-helper=0:1' \
    'deep-helper=0:1' \
    'shared-generic=0:1,0:1' \
    'unallocated-provider=UnallocatedArrayTypeProvider' \
    'unused-reflected=UnusedReflectedMatch[]' \
    'framework-box-forward=0:1:False' \
    'framework-box-backward=0:1' \
    'framework-box-null=-1:0' \
    'list-box-forward=0:1:False' \
    'list-box-backward=0:1' \
    'copy-box-forward=0:1:False' \
    'copy-box-backward=0:1' \
    'copy-box-other=-1:1' \
    'plain-forward=0' \
    'plain-backward=0' \
    'runtime-owned=1' \
    'runtime-no-slot=-1' \
    'dynamic array null equality end' \
    '== array search reference and type provenance ==' \
    'array search reference and type provenance end'; do
    grep -Fxq "$line" <<< "$native" \
        || { echo "FAIL: dynamic Array equality witness missing: $line" >&2; exit 1; }
done
for name in DirectDynamicMatch ConditionalFirstMatch ConditionalSecondMatch IndexedSelectedMatch \
        TypeOverwriteSelectedMatch \
        FieldMatch HiddenMatch FieldStoredMatch FieldAliasMatch OverwriteSelectedMatch \
        RefFieldWriteMatch RefStaticWriteMatch ArrayGetTypeMatch MakeArrayTypeMatch MdGetTypeMatch \
        NestedArrayRuntimeMatch RankedTypeArrayMatch HelperRankedTypeArrayMatch \
        RepeatedRankedTypeArrayMatch \
        RefStobjMatch RefLdobjMatch \
        TypeHandleMatch EmptySignatureSelectedMatch \
        FieldVirtualMatch StructFieldMatch BoxOverwriteSelectedMatch \
        StaticErasedMatch StaticDeclaredMatch StaticOverwriteSelectedMatch \
        GenericStaticFirstMatch GenericStaticSecondMatch CctorStaticMatch ConstructorMatch \
        ConstructorOverwriteSelectedMatch CctorOverwriteSelectedMatch \
        PropertyMatch SetterOnlyMatch \
        MixedVisibilityMatch \
        MethodMatch GenericArgumentMatch \
        GenericSelectedMatch \
        InterfaceMatch OverloadSelectedMatch NonzeroOverloadMatch LocalSelectedMatch \
        HelperSelectedMatch FrameworkBoxMatch \
        SinkHelperMatch CrossHelperMatch DeepHelperMatch SharedGenericFirstMatch SharedGenericSecondMatch \
        FrameworkListBoxMatch FrameworkCopyBoxMatch; do
    if ! grep -Eq "^int32_t ${name}_Equals_m[0-9]+\\(" "$_CG_OUT"/generated*.cpp; then
        echo "FAIL: selected value type equality body was not emitted: $name" >&2
        exit 1
    fi
done
for name in UnusedReflectedMatch GenericUnselectedMatch IndexedUnselectedMatch \
        OverloadUnselectedMatch EmptySignatureUnselectedMatch \
        ZeroOverloadUnselectedMatch \
        UnselectedLocalMatch UnselectedHelperMatch \
        UnallocatedProviderMatch; do
    if grep -Eq "^(inline |static )?int32_t ${name}_Equals_m[0-9]+\\(" \
            "$_CG_OUT"/generated*.h "$_CG_OUT"/generated*.cpp; then
        echo "FAIL: unsearched value type equality body was emitted: $name" >&2
        exit 1
    fi
done
if grep -Eq '^int32_t (ObjectGetTypeBoxMatch|CopyShapeBoxMatch)_Equals_m[0-9]+\(' \
        "$_CG_OUT"/generated*.cpp; then
    echo 'FAIL: a boxed object[] element changed its array GetType provenance' >&2
    exit 1
fi

echo '== isolated framework and reflection boxes =='
box_root=artifacts/arraycore-box-provenance
dotnet build samples/dotnet/ArrayCore/BoxProvenanceOnly.csproj -c "$CONFIG" \
    --nologo -v q -o "$box_root/app"
box_app="$box_root/app/BoxProvenanceOnly.dll"
corelib=$(locate_corelib)
bcl=$(dirname "$corelib")
invoke_cli "$box_app" -r "$corelib" -r "$bcl/System.Collections.dll" \
    --shared-generics -o "$box_root/gen"
compile_console "$box_root/gen" BoxProvenanceOnly
box_native=$(run_bounded "./$box_root/gen/BoxProvenanceOnly$EXE_EXT")
box_native=$(strip_cr_win "$box_native")
box_oracle=$(run_bounded dotnet "$box_app")
assert_output "$(strip_cr_win "$box_native")" "$(strip_cr_win "$box_oracle")"
box_before_formatting=$(run_bounded dotnet "$box_app" before-runtime-box-formatting)
box_formatting_prefix=$(awk '/^== runtime-handle boxed formatting ==$/ { exit } { print }' <<< "$box_native")
assert_output "$box_formatting_prefix" "$(strip_cr_win "$box_before_formatting")"
assert_runtime_box_formatting "$box_native"
grep -Eq '^const Dn2CppTypeInfo ti_ArrayRuntimeBoxSubset_Program_UnselectedPayload = ' "$box_root/gen"/generated*.cpp \
    || { echo 'FAIL: unselected runtime box control type-info is missing' >&2; exit 1; }
if grep -Eq '^(inline |static )?Dn2CppString\* [[:alnum:]_]+_ToString_m[0-9]+\(t_ArrayRuntimeBoxSubset_Program_UnselectedPayload\*' \
    "$box_root/gen"/generated*.h "$box_root/gen"/generated*.cpp; then
    echo 'FAIL: runtime handle boxing rooted an unselected ToString override' >&2
    exit 1
fi
box_previous=$(run_bounded dotnet "$box_app" before-array-search-provenance-additions)
box_previous=$(strip_cr_win "$box_previous")
box_prefix=$(awk '/^== array search copy and type provenance ==$/ { exit } { print }' <<< "$box_native")
box_prefix=$(strip_cr_win "$box_prefix")
assert_output "$box_prefix" "$box_previous"
box_lines=$(strip_cr_win "$box_native")
for line in \
    '== framework box provenance ==' \
    '== array search copy and type provenance ==' \
    'framework-box-forward=0:1:False' \
    'framework-box-backward=0:1' \
    'framework-box-null=-1:0' \
    'list-box-forward=0:1:False' \
    'list-box-backward=0:1' \
    'copy-box-forward=0:1:False' \
    'copy-box-backward=0:1' \
    'copy-box-other=-1:1' \
    'reflected-box-forward=0:1:False' \
    'reflected-box-backward=0:1' \
    'object-field-box=0:1' \
    'reflected-set-box=0:1' \
    'object-field-overwrite=0:1:True' \
    'reflected-set-overwrite=0:1:True' \
    'unused-box=True' \
    'unused-newarr=1' \
    'unused-user-box=UnusedUserBoxMatch' \
    'field-stored=0:1:1' \
    'instance-snapshot=0:1:1' \
    'static-snapshot=0:1:1' \
    'helper-snapshot=0:1:1' \
    'reflected-snapshot=0:1:True' \
    'array-get-holder-alias=0:1:1:True' \
    'shared-donor=0:1,0:1' \
    'array-set-box=-1:1' \
    'array-clone=0:1' \
    'byref-array=0:1' \
    'array-copy-box=-1:1' \
    'array-copyto-box=-1:1' \
    'constrained-copy-rejected=ArrayTypeMismatchException:-1:True' \
    'array-copy-rejected=ArrayTypeMismatchException:0:True' \
    'array-copyto-rejected=ArrayTypeMismatchException:0:True' \
    'constrained-copy-box=0:1' \
    'array-copy-zero=-1:True' \
    'array-copy-rank=RankException:-1:True' \
    'array-copy-value-rejected=ArrayTypeMismatchException:-1:0' \
    'array-copy-created-rejected=ArrayTypeMismatchException:0:True' \
    'constrained-copy-byref-source=-1:1' \
    'byref-before-write=0:1' \
    'byref-nonvoid-write=7:0' \
    'byref-nonvoid-return=0:1' \
    'generic-ref-before=0:1' \
    'generic-ref-after=0:1' \
    'empty-type-signature=0:1' \
    'signature-pair-forward=0:1' \
    'signature-pair-reverse=0:1' \
    'hidden-method=0:1' \
    'indexed-property=0:1' \
    'generic-arity=0:1' \
    'hidden-reflected-field=-1:1:HiddenReflectedFieldDerived' \
    'helper-copy-box=-1:1' \
    'helper-copyto-box=-1:1' \
    'ref-write=0:1' \
    'ref-field-write=0:1' \
    'ref-static-write=0:1' \
    'array-get-type=0:1' \
    'make-array-type=0:1:True' \
    'object-get-type=0:Object' \
    'nested-array-get-type=0:1' \
    'ranked-array-type=0:1' \
    'helper-ranked-array-type=0:1' \
    'repeated-ranked-array-type=0:1' \
    'ref-stobj=0:1' \
    'ref-ldobj=0:1' \
    'copied-object-get-type=0:Object' \
    'md-get-type=0:1:2' \
    'type-handle=0:1' \
    'field-alias=0:1' \
    'field-overwrite=0:1:1' \
    'field-virtual=0:1' \
    'type-overwrite=0:1:TypeOverwrittenDeadMatch' \
    'type-snapshot=0:1:TypeSnapshotUnselectedMatch' \
    'signature-overwrite=0:1' \
    'signature-runtime-overwrite=0:1' \
    'static-erased=0:1' \
    'static-declared=0:1' \
    'static-overwrite=0:1:1' \
    'generic-static=0:1,0:1' \
    'struct-field=0:1' \
    'box-overwrite=0:1:True' \
    'cctor-static=0:1' \
    'constructor-field=0:1' \
    'constructor-overwrite=0:1:1' \
    'cctor-overwrite=0:1:1' \
    'framework box provenance end' \
    'array search copy and type provenance end'; do
    grep -Fxq "$line" <<< "$box_lines" \
        || { echo "FAIL: isolated boxed equality witness missing: $line" >&2; exit 1; }
done
for name in FrameworkBoxMatch FrameworkListBoxMatch FrameworkCopyBoxMatch ReflectedFieldBoxMatch \
        ObjectFieldBoxMatch ReflectedSetBoxMatch ReflectedOverwriteSelectedMatch \
        ReflectedSetOverwriteSelectedMatch \
        FieldStoredMatch InstanceSnapshotLiveMatch StaticSnapshotLiveMatch \
        HelperSnapshotLiveMatch ReflectedSnapshotLiveMatch ArrayGetHolderSelectedMatch \
        SharedGenericFirstMatch SharedGenericSecondMatch ArraySetValueBoxMatch \
        ArrayCloneMatch ByRefArrayMatch ArrayCopyBoxMatch ArrayCopyToBoxMatch \
        ConstrainedCopyBoxMatch ByRefReplacedConstrainedMatch BeforeByRefWriteMatch \
        AfterNonVoidByRefWriteMatch ByRefReturnMatch \
        GenericRefBeforeSelectedMatch GenericRefAfterSelectedMatch \
        SignaturePairSelectedMatch HiddenMethodLiveMatch IndexerSelectedMatch AritySelectedMatch \
        HiddenReflectedFieldLiveMatch \
        HelperArrayCopyMatch HelperArrayCopyToMatch RefWriteMatch \
        RefFieldWriteMatch RefStaticWriteMatch ArrayGetTypeMatch MakeArrayTypeMatch MdGetTypeMatch \
        NestedArrayRuntimeMatch RankedTypeArrayMatch HelperRankedTypeArrayMatch \
        RepeatedRankedTypeArrayMatch \
        RefStobjMatch RefLdobjMatch \
        TypeHandleMatch EmptySignatureSelectedMatch \
        FieldAliasMatch OverwriteSelectedMatch FieldVirtualMatch \
        TypeOverwriteSelectedMatch TypeSnapshotSelectedMatch SignatureSelectedMatch \
        RuntimeSignatureSelectedMatch \
        StructFieldMatch BoxOverwriteSelectedMatch \
        StaticErasedMatch StaticDeclaredMatch StaticOverwriteSelectedMatch \
        GenericStaticFirstMatch GenericStaticSecondMatch \
        CctorStaticMatch ConstructorMatch ConstructorOverwriteSelectedMatch \
        CctorOverwriteSelectedMatch; do
    if ! awk -v name="$name" '
        FNR == 1 { signature = 0 }
        { sub(/\r$/, "") }
        signature && /^\{/ { found = 1; exit }
        { signature = ($0 ~ "^(inline |static )?int32_t " name "_Equals_m[0-9]+\\([^;]*\\)$") }
        END { exit !found }
    ' "$box_root/gen/generated.h" "$box_root/gen"/generated*.cpp; then
        echo "FAIL: searched boxed value equality body was not emitted: $name" >&2
        exit 1
    fi
done
for name in UnusedFrameworkBoxMatch UnusedNewarrMatch UnusedUserBoxMatch \
        InstanceSnapshotDeadMatch StaticSnapshotDeadMatch HelperSnapshotDeadMatch \
        ReflectedSnapshotDeadMatch ArrayGetHolderUnsearchedMatch \
        RejectedConstrainedCopyMatch AfterByRefWriteMatch BeforeNonVoidByRefWriteMatch \
        ByRefReturnBeforeMatch ByRefReturnWrittenMatch RefStobjBeforeMatch \
        GenericRefBeforeUnsearchedMatch GenericRefAfterUnsearchedMatch \
        EmptySignatureUnselectedMatch \
        RejectedArrayCopyMatch RejectedArrayCopyToMatch \
        ZeroLengthArrayCopyMatch RankRejectedArrayCopyMatch \
        RejectedValueArrayCopyMatch \
        RejectedCreatedArrayCopyMatch \
        SignaturePairUnselectedMatch HiddenMethodDeadMatch IndexerUnselectedMatch \
        ArityUnselectedMatch HiddenReflectedFieldDeadMatch \
        UnselectedFieldStoredMatch OverwrittenDeadMatch \
        StaticOverwrittenDeadMatch BoxOverwrittenDeadMatch TypeOverwrittenDeadMatch \
        TypeSnapshotUnselectedMatch SignatureUnselectedMatch \
        RuntimeSignatureUnselectedMatch ReflectedOverwrittenDeadMatch \
        ReflectedSetOverwrittenDeadMatch \
        ConstructorOverwrittenDeadMatch CctorOverwrittenDeadMatch ObjectGetTypeBoxMatch \
        CopyShapeBoxMatch; do
    if grep -Eq "^(inline |static )?int32_t ${name}_Equals_m[0-9]+\\(" \
            "$box_root/gen"/generated*.h "$box_root/gen"/generated*.cpp; then
        echo "FAIL: an unsearched value rooted its equality body: $name" >&2
        exit 1
    fi
done

grep -Fxq '// DynamicArrayNullEqualitySubset.SharedArraySearcher_$CnInt32::Search' \
    "$box_root/gen/generated.h" "$box_root/gen"/generated*.cpp \
    || { echo 'FAIL: the array search donor lost its canonical body' >&2; exit 1; }
shared_calls=$(grep -Eo 'SharedArraySearcher_1_Search_m[0-9]+\(' \
    "$box_root/gen"/generated_b*.cpp | sed 's/.*://')
[ "$(wc -l <<< "$shared_calls" | tr -d ' ')" -eq 2 ] \
    && [ "$(sort -u <<< "$shared_calls" | wc -l | tr -d ' ')" -eq 1 ] \
    || { echo 'FAIL: both array searches must call one shared body' >&2; exit 1; }

echo '== isolated reflection and array return boxes =='
return_box_root=artifacts/arraycore-return-box-provenance
dotnet build samples/dotnet/ArrayCore/ReflectionReturnBoxOnly.csproj -c "$CONFIG" \
    --nologo -v q -o "$return_box_root/app"
return_box_app="$return_box_root/app/ReflectionReturnBoxOnly.dll"
invoke_cli "$return_box_app" -r "$corelib" -r "$bcl/System.Collections.dll" \
    -o "$return_box_root/gen"
compile_console "$return_box_root/gen" ReflectionReturnBoxOnly
return_box_native=$(run_bounded "./$return_box_root/gen/ReflectionReturnBoxOnly$EXE_EXT")
return_box_oracle=$(run_bounded dotnet "$return_box_app")
assert_output "$(strip_cr_win "$return_box_native")" "$(strip_cr_win "$return_box_oracle")"
return_box_lines=$(strip_cr_win "$return_box_native")
for line in \
    '== reflection and array return boxes ==' \
    'method-box=0:1:False' \
    'property-box=0:1:False' \
    'array-get-box=0:1:False' \
    'reflection and array return boxes end'; do
    grep -Fxq "$line" <<< "$return_box_lines" \
        || { echo "FAIL: reflected return equality witness missing: $line" >&2; exit 1; }
done
for name in ReflectedMethodBoxMatch ReflectedPropertyBoxMatch ArrayGetValueBoxMatch; do
    if ! grep -Eq "^int32_t ${name}_Equals_m[0-9]+\\(" "$return_box_root/gen"/generated*.cpp; then
        echo "FAIL: reflected return value equality body was not emitted: $name" >&2
        exit 1
    fi
done

echo '== bounded Type-provider call graph =='
diamond_root=artifacts/arraycore-diamond-provenance
dotnet build samples/dotnet/ArrayCore/DiamondProvenanceOnly.csproj -c "$CONFIG" \
    --nologo -v q -o "$diamond_root/app"
diamond_app="$diamond_root/app/DiamondProvenanceOnly.dll"
invoke_cli "$diamond_app" -r "$corelib" -o "$diamond_root/gen"
compile_console "$diamond_root/gen" DiamondProvenanceOnly
diamond_native=$(run_bounded "./$diamond_root/gen/DiamondProvenanceOnly$EXE_EXT")
diamond_oracle=$(run_bounded dotnet "$diamond_app")
assert_output "$(strip_cr_win "$diamond_native")" "$(strip_cr_win "$diamond_oracle")"
diamond_lines=$(strip_cr_win "$diamond_native")
for line in \
    '== array search provenance diamond ==' \
    'diamond=0:1' \
    'array search provenance diamond end'; do
    grep -Fxq "$line" <<< "$diamond_lines" \
        || { echo "FAIL: Type-provider call graph witness missing: $line" >&2; exit 1; }
done
grep -Eq '^int32_t DiamondMatch_Equals_m[0-9]+\(' "$diamond_root/gen"/generated*.cpp \
    || { echo 'FAIL: diamond-selected value equality body was not emitted' >&2; exit 1; }

echo '== array search field aliases =='
field_alias_root=artifacts/arraycore-field-alias-provenance
dotnet build samples/dotnet/ArrayCore/FieldAliasProvenanceOnly.csproj -c "$CONFIG" \
    --nologo -v q -o "$field_alias_root/app"
field_alias_app="$field_alias_root/app/FieldAliasProvenanceOnly.dll"
invoke_cli "$field_alias_app" -r "$corelib" -o "$field_alias_root/gen"
compile_console "$field_alias_root/gen" FieldAliasProvenanceOnly
field_alias_native=$(run_bounded "./$field_alias_root/gen/FieldAliasProvenanceOnly$EXE_EXT")
field_alias_oracle=$(run_bounded dotnet "$field_alias_app")
assert_output "$(strip_cr_win "$field_alias_native")" "$(strip_cr_win "$field_alias_oracle")"
field_alias_lines=$(strip_cr_win "$field_alias_native")
for line in \
    '== array search field aliases ==' \
    'opaque-holder=0:1' \
    'array-get-holder=0:1' \
    'static-holder-overwrite=0:1' \
    'array search field aliases end'; do
    grep -Fxq "$line" <<< "$field_alias_lines" \
        || { echo "FAIL: field alias equality witness missing: $line" >&2; exit 1; }
done
for name in OpaqueHolderMatch ArrayGetHolderMatch StaticHolderSelectedMatch; do
    grep -Eq "^int32_t ${name}_Equals_m[0-9]+\\(" "$field_alias_root/gen"/generated*.cpp \
        || { echo "FAIL: field alias equality body was not emitted: $name" >&2; exit 1; }
done
if grep -Eq '^(inline |static )?int32_t StaticHolderOverwrittenMatch_Equals_m[0-9]+\(' \
        "$field_alias_root/gen"/generated*.h "$field_alias_root/gen"/generated*.cpp; then
    echo 'FAIL: overwritten static-holder value rooted its equality body' >&2
    exit 1
fi

echo '== unsupported selected value equality =='
unsupported_root=artifacts/arraycore-unsupported-equality
dotnet build samples/dotnet/ArrayCore/ArrayCore.csproj -c "$CONFIG" \
    --nologo -v q -p:DefineConstants=ARRAY_UNSUPPORTED_EQUALITY_ONLY \
    -o "$unsupported_root/app"
set +e
invoke_cli "$unsupported_root/app/ArrayCore.dll" -r "$corelib" \
    -o "$unsupported_root/gen" > "$unsupported_root/refused.log" 2>&1
unsupported_status=$?
set -e
if [ "$unsupported_status" -ne 2 ] \
        || ! grep -Fq 'UnsupportedStructuralMatch' "$unsupported_root/refused.log" \
        || ! grep -Fq 'ByValArray' "$unsupported_root/refused.log" \
        || ! grep -iq 'equal' "$unsupported_root/refused.log"; then
    echo 'FAIL: selected unsupported value equality was not explicitly refused' >&2
    cat "$unsupported_root/refused.log" >&2
    exit 1
fi

# Reference elements retain the stored array identity across a helper update.
for alias_subject in ArrayElementAliasOnly ArrayObjectElementAliasOnly ArrayUnknownElementAliasOnly ArrayErasedElementAliasOnly ArrayReferenceSlotAliasOnly; do
    alias_root="artifacts/arraycore-$alias_subject"
    dotnet build "samples/dotnet/ArrayCore/$alias_subject.csproj" -c "$CONFIG" \
        --nologo -v q -o "$alias_root/app"
    alias_app="$alias_root/app/$alias_subject.dll"
    invoke_cli "$alias_app" -r "$corelib" --shared-generics -o "$alias_root/gen"
    compile_console "$alias_root/gen" "$alias_subject"
    alias_native=$(run_bounded "./$alias_root/gen/$alias_subject$EXE_EXT")
    alias_oracle=$(run_bounded dotnet "$alias_app")
    assert_output "$(strip_cr_win "$alias_native")" "$(strip_cr_win "$alias_oracle")"
    alias_lines=$(strip_cr_win "$alias_native")
    for line in 'array element alias=0:1' 'array element alias end'; do
        if ! grep -Fxq -- "$line" <<< "$alias_lines"; then
            echo "FAIL: array element alias witness missing: $alias_subject / $line" >&2
            exit 1
        fi
    done
    if [ "$alias_subject" = ArrayReferenceSlotAliasOnly ] \
            && ! grep -Fxq -- 'array reference-slot identity=True' <<< "$alias_lines"; then
        echo 'FAIL: reference-slot boxing lost the array identity' >&2
        exit 1
    fi
done

# Reflected Void stays a runtime Box argument fault when its result feeds a search.
void_box_root="artifacts/arraycore-reflected-void-box"
dotnet build samples/dotnet/ArrayCore/ArrayReflectedVoidBoxOnly.csproj -c "$CONFIG" \
    --nologo -v q -o "$void_box_root/app"
void_box_app="$void_box_root/app/ArrayReflectedVoidBoxOnly.dll"
invoke_cli "$void_box_app" -r "$corelib" --shared-generics -o "$void_box_root/gen"
compile_console "$void_box_root/gen" ArrayReflectedVoidBoxOnly
void_box_native=$(run_bounded "./$void_box_root/gen/ArrayReflectedVoidBoxOnly$EXE_EXT")
void_box_oracle=$(run_bounded dotnet "$void_box_app")
assert_output "$(strip_cr_win "$void_box_native")" "$(strip_cr_win "$void_box_oracle")"
void_box_lines=$(strip_cr_win "$void_box_native")
for line in 'reflected void box=ArgumentException' 'reflected void box end'; do
    if ! grep -Fxq -- "$line" <<< "$void_box_lines"; then
        echo "FAIL: reflected void box witness missing: $line" >&2
        exit 1
    fi
done

# A reference-array read retains its prior slot value after escape and later stores.
for prior_store_subject in ArrayFutureStoreOnly ArrayFutureNullStoreOnly ArrayObjectFutureStoreOnly; do
    prior_store_root="artifacts/arraycore-$prior_store_subject"
    dotnet build "samples/dotnet/ArrayCore/$prior_store_subject.csproj" -c "$CONFIG" \
        --nologo -v q -o "$prior_store_root/app"
    prior_store_app="$prior_store_root/app/$prior_store_subject.dll"
    invoke_cli "$prior_store_app" -r "$corelib" --shared-generics -o "$prior_store_root/gen"
    compile_console "$prior_store_root/gen" "$prior_store_subject"
    prior_store_native=$(run_bounded "./$prior_store_root/gen/$prior_store_subject$EXE_EXT")
    prior_store_oracle=$(run_bounded dotnet "$prior_store_app")
    assert_output "$(strip_cr_win "$prior_store_native")" "$(strip_cr_win "$prior_store_oracle")"
    prior_store_lines=$(strip_cr_win "$prior_store_native")
    if [ "$prior_store_subject" = ArrayObjectFutureStoreOnly ]; then
        prior_store_witnesses=('UniqueModulo[]:0:1' 'ArrayObjectFutureUnsupportedOnly end')
    elif [ "$prior_store_subject" = ArrayFutureNullStoreOnly ]; then
        prior_store_witnesses=('array prior null=ArgumentNullException:elementType' 'array prior null end')
    else
        prior_store_witnesses=('array prior store=0:1' 'array prior store end')
    fi
    for line in "${prior_store_witnesses[@]}"; do
        if ! grep -Fxq -- "$line" <<< "$prior_store_lines"; then
            echo "FAIL: prior array store witness missing: $line" >&2
            exit 1
        fi
    done
    if grep -Eq '^int32_t .*FutureUnsupported.*_Equals_m[0-9]+\(' "$prior_store_root"/gen/generated*.cpp; then
        echo 'FAIL: a later reference-array store rooted its unused equality body' >&2
        exit 1
    fi
done

for line in '== array producer and validation order ==' \
        'runtime box search=0:0' 'nullable runtime box search=0:0' \
        'uninitialized runtime box search=0:0' 'reference-slot runtime box search=0:0' \
        'reference-slot runtime box identity=True' 'reference-slot runtime box type=0:1' \
        'array producer and validation order end'; do
    if ! grep -Fxq -- "$line" <<< "$native"; then
        echo "FAIL: runtime producer witness missing: $line" >&2
        exit 1
    fi
done

# Non-SZ rank-one collection calls need the MD map without a statically MD array.
lower_bounds_root="artifacts/arraycore-lower-bounds-only"
dotnet build samples/dotnet/ArrayCore/ArrayLowerBoundsOnly.csproj -c "$CONFIG" \
    --nologo -v q -o "$lower_bounds_root/app"
lower_bounds_app="$lower_bounds_root/app/ArrayLowerBoundsOnly.dll"
invoke_cli "$lower_bounds_app" -r "$corelib" -o "$lower_bounds_root/gen"
compile_console "$lower_bounds_root/gen" ArrayLowerBoundsOnly
lower_bounds_native=$(run_bounded "./$lower_bounds_root/gen/ArrayLowerBoundsOnly$EXE_EXT")
lower_bounds_native=$(strip_cr_win "$lower_bounds_native")
lower_bounds_oracle=$(run_bounded dotnet "$lower_bounds_app")
assert_output "$(strip_cr_win "$lower_bounds_native")" "$(strip_cr_win "$lower_bounds_oracle")"
lower_bounds_prefix=$(awk '/^== rank1 collection extreme bounds ==$/ { exit } { print }' <<< "$lower_bounds_native")
lower_bounds_before=$(run_bounded dotnet "$lower_bounds_app" before-extreme-collection)
assert_output "$lower_bounds_prefix" "$(strip_cr_win "$lower_bounds_before")"
lower_bounds_before=$(run_bounded "./$lower_bounds_root/gen/ArrayLowerBoundsOnly$EXE_EXT" before-extreme-collection)
assert_output "$lower_bounds_prefix" "$(strip_cr_win "$lower_bounds_before")"
for line in '== rank1 non-SZ collection search ==' \
    'rank1-only=System.Int32[*]:False:36:17:-1:-3:True:False' \
    'copied=17/19' 'cleared=0/0' 'rank1 non-SZ collection search end' \
    '== rank1 collection virtual equality ==' 'rank1 collection virtual equality end' \
    'collection null=-2:-2/1' 'collection contains null=-2:True/1' \
    'collection self=-2:-3/1' 'collection contains self=-2:False/1' \
    'collection null=5:5/1' 'collection contains null=5:True/1' \
    'collection self=5:4/1' 'collection contains self=5:False/1' \
    '== rank1 collection extreme bounds ==' 'rank1 collection extreme bounds end' \
    "collection extreme=2147483647/1 index hit=ArgumentOutOfRangeException/startIndex/Index was out of range. Must be non-negative and less than or equal to the size of the collection. (Parameter 'startIndex')" \
    "collection extreme=2147483647/1 index miss=ArgumentOutOfRangeException/startIndex/Index was out of range. Must be non-negative and less than or equal to the size of the collection. (Parameter 'startIndex')" \
    "collection extreme=2147483647/1 contains hit=ArgumentOutOfRangeException/startIndex/Index was out of range. Must be non-negative and less than or equal to the size of the collection. (Parameter 'startIndex')" \
    "collection extreme=2147483647/1 contains miss=ArgumentOutOfRangeException/startIndex/Index was out of range. Must be non-negative and less than or equal to the size of the collection. (Parameter 'startIndex')" \
    'collection extreme=-2147483648/1 contains miss=True' \
    'collection extreme=-2147483648/0 contains hit=True' \
    'collection extreme=-2147483648/0 contains miss=True'; do
    [[ $(grep -Fxc -- "$line" <<< "$lower_bounds_native") == 1 ]] \
        || { echo "FAIL: rank1 collection witness must run once: $line" >&2; exit 1; }
done

# Static TypeSpecs and reflective creation intern the same non-SZ rank-one identity.
static_rank1_root="artifacts/arraycore-static-rank1"
dotnet build gates/fixtures/array-rank1-md/Generate.csproj -c "$CONFIG" \
    --nologo -v q -o "$static_rank1_root/generator"
run_bounded dotnet "$static_rank1_root/generator/Generate.dll" \
    "$static_rank1_root/ArrayRankOneLibrary.dll"
dotnet build gates/fixtures/array-rank1-md/Driver.csproj -c "$CONFIG" \
    -p:ArrayRankOneLibraryPath="$(pwd)/$static_rank1_root/ArrayRankOneLibrary.dll" \
    -p:ArrayRankOneRuntimePath="$(dirname "$corelib")" \
    --nologo -v q -o "$static_rank1_root/app"
static_rank1_app="$static_rank1_root/app/ArrayStaticRankOneOnly.dll"
invoke_cli "$static_rank1_app" -r "$corelib" -r "$static_rank1_root/app/ArrayRankOneLibrary.dll" \
    -o "$static_rank1_root/gen"
compile_console "$static_rank1_root/gen" ArrayStaticRankOneOnly
static_rank1_native=$(run_bounded "./$static_rank1_root/gen/ArrayStaticRankOneOnly$EXE_EXT")
static_rank1_native=$(strip_cr_win "$static_rank1_native")
static_rank1_oracle=$(run_bounded dotnet exec --runtimeconfig "$static_rank1_root/generator/Generate.runtimeconfig.json" "$static_rank1_app")
assert_output "$(strip_cr_win "$static_rank1_native")" "$(strip_cr_win "$static_rank1_oracle")"
for line in '== static rank1 MD identity ==' \
    'static=System.Int32[*]/Int32[*]/False/1' 'identity=True/True/False' \
    'static clone=True/23' 'static rank1 MD identity end'; do
    [[ $(grep -Fxc -- "$line" <<< "$static_rank1_native") == 1 ]] \
        || { echo "FAIL: static rank1 MD witness must run once: $line" >&2; exit 1; }
done

# An is-test keeps a comparer shell without making its static fields callable.
comparer_type_root="artifacts/arraycore-comparer-type-only"
dotnet build samples/dotnet/ArrayCore/ArrayComparerTypeOnly.csproj -c "$CONFIG" \
    --nologo -v q -o "$comparer_type_root/app"
comparer_type_app="$comparer_type_root/app/ArrayComparerTypeOnly.dll"
invoke_cli "$comparer_type_app" -r "$corelib" -o "$comparer_type_root/gen"
compile_console "$comparer_type_root/gen" ArrayComparerTypeOnly
comparer_type_native=$(run_bounded "./$comparer_type_root/gen/ArrayComparerTypeOnly$EXE_EXT")
comparer_type_native=$(strip_cr_win "$comparer_type_native")
comparer_type_oracle=$(run_bounded dotnet "$comparer_type_app")
assert_output "$comparer_type_native" "$(strip_cr_win "$comparer_type_oracle")"
for line in 'opaque-comparer/is-null:False' 'opaque-comparer/is-string:False' \
    'opaque comparer type checks end'; do
    [[ $(grep -Fxc -- "$line" <<< "$comparer_type_native") == 1 ]] \
        || { echo "FAIL: comparer type-only witness must run once: $line" >&2; exit 1; }
done

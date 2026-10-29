#!/usr/bin/env bash
# Consolidated dictionary/set gate: Dictionary (core/API/enumeration/extra ops/
# string keys), HashSet, custom hash keys, the generic sorted collections
# (SortedDictionary/SortedList/SortedSet), the non-generic System.Collections.SortedList,
# and FrozenDictionary (FrozenDictSubset).
# Diffed exactly vs real .NET.
# Structural data-pointer fields preserve address equality and matching hashes.
# Also covers string.Join/Concat over a concrete IEnumerable<T> that is neither an
# array nor a List<T> backing — a SortedSet<T>, a SortedDictionary Keys/Values view,
# a Queue<T> — where the intrinsic emits an interface-enumeration loop instead of
# indexing a backing store (JoinEnumerableSubset). It is here rather than in
# StringCore, which owns the rest of string.Join, because those types need
# System.Collections and StringCore's reference set is re-declared by four other
# gates (see the note in build-and-run-string-core.sh); this bucket already has it.
# Also covers, from three retired standalone gates:
#   * OrdinalStringSetDedupSubset — HashSet/Dictionary keyed by a *runtime-built*
#     string under an explicit StringComparer.Ordinal must dedup by value, the
#     divergence that kept native dn2cpp's own emit from being byte-identical;
#   * StructKeySubset — a struct that overrides GetHashCode and IEquatable<T> as a
#     key (comparer devirtualization to those overrides; the sibling
#     ValueStructKeySubset covers the struct that overrides neither);
#   * ValueTupleKeyDictSubset — a ValueTuple-keyed dictionary populated in a STATIC
#     CONSTRUCTOR, which pins the System.HashCode..cctor seed hoist. Its cctor-order
#     probe survives the fold structurally, not by driver position: EmitInitCalls
#     orders cctors by assembly-reference depth (CoreLib first, entry assembly last)
#     with System.HashCode hoisted ahead of even that, so an app-module cctor always
#     runs after the seed is set. See the note at the head of that .cs.
# Also covers EqualityComparerDefaultSubset: Default is a non-null, stable per-T concrete
# object implementing generic and non-generic IEqualityComparer, including interface calls
# and a null-conditional equality call as used by reactive properties.
# RuntimeTypeHandleKeySubset covers the opaque pointer-backed handle as a direct key and as
# a synthesized structural field, including nested KeyValuePair dictionary values.
# NullableEqualitySubset covers nullable default equality: empty values, underlying
# IEquatable<T>/object/structural dispatch, floating NaN and nullable collection scans.
# Former gates: dict, dict-api, dict-enum, dict-more, dict-string, hashset,
# hashkey, sorted-collections, join-enumerable-subset,
# ordinal-stringset-dedup-subset, struct-key-subset, valuetuple-key-dict-subset.
DN2CPP_GATE_EXTRA_INPUTS="${DN2CPP_GATE_EXTRA_INPUTS:-} gates/_gchandle-equality.sh gates/fixtures/gchandle-equality/check-generated.py gates/fixtures/ldftn-local/GCHandleEqualityGuards.cs gates/fixtures/structural-pointer/check-generated.py gates/_structural-pointer.sh gates/fixtures/structural-pointer-negative/PointerNegative.csproj gates/fixtures/structural-pointer-negative/Program.cs gates/fixtures/ldftn-local/StructuralPointerFixture.cs gates/fixtures/ldftn-local/Program.cs gates/fixtures/ldftn-local/LdftnLocalFixture.csproj"
source "$(dirname "$0")/_common.sh"
source "$(dirname "$0")/_structural-pointer.sh"
source "$(dirname "$0")/_gchandle-equality.sh"

# System.Collections.NonGeneric carries the non-generic SortedList (NonGenericSortedListSubset);
# the generic collections come from System.Collections; System.Collections.Immutable
# carries FrozenDictionary/FrozenHashTable (FrozenDictSubset — exercises HashHelpers.Primes),
# whose ToFrozenDictionary build path reaches System.Linq.Enumerable.
# Default order and equality preserve the earlier bucket and run the appended cases.
gate_extra_asserts() {
    local out="$1" native before prefix line
    python3 gates/fixtures/structural-pointer/check-generated.py "$out"
    python3 gates/fixtures/gchandle-equality/check-generated.py "$out"
    gate_capture_ok "$out/assert-native-1" run_bounded "./$out/DictCollections"
    native="$out/assert-native-1.stdout"

    gate_capture_ok "$out/assert-before-2" run_bounded dotnet "$_CG_APP" before-default-comparison
    before="$out/assert-before-2.stdout"

    prefix="$out/assert-prefix-4.stdout"

    gate_output select "$native" "$prefix" --before '== default comparison validation ==' --policy crlf
    gate_output compare "$prefix" "$before"
    gate_capture_ok "$out/assert-before-3" run_bounded dotnet "$_CG_APP" before-nullable-equality
    before="$out/assert-before-3.stdout"

    prefix="$out/assert-prefix-5.stdout"

    gate_output select "$native" "$prefix" --before '== nullable default equality ==' --policy crlf
    gate_output compare "$prefix" "$before"
    gate_capture_ok "$out/assert-before-pointer" run_bounded dotnet "$_CG_APP" before-structural-pointer
    gate_output select "$native" "$out/assert-pointer-prefix.stdout" --before 'structural-pointer-reader-same:True' --policy crlf
    gate_output compare "$out/assert-pointer-prefix.stdout" "$out/assert-before-pointer.stdout"
    gate_output witness "$native" 'structural-pointer-checks:39' --count 1 --policy crlf
    gate_capture_ok "$out/assert-before-gchandle" run_bounded dotnet "$_CG_APP" before-gchandle-equality
    gate_output select "$native" "$out/assert-gchandle-prefix.stdout" --before 'gchandle-equality-copy:True' --policy crlf
    gate_output compare "$out/assert-gchandle-prefix.stdout" "$out/assert-before-gchandle.stdout"
    gate_output witness "$native" 'gchandle-equality-checks:39' --count 1 --policy crlf
    for line in '== default comparison validation ==' \
        '== default equality mouths ==' \
        'default equality mouths end' \
        'reference IEquatable nongeneric message: Type of argument is not compatible with the generic comparer.' \
        'reference IEquatable nongeneric param: null' \
        'default comparison validation end' \
        '== nullable default equality ==' \
        'typed equatable: True False False True False 2' \
        'generic equatable calls: 2' \
        'nullable default equality end'; do
        gate_output witness "$native" "$line" --count any --policy crlf \
            || { echo "FAIL: DictCollections comparison coverage missing: $line" >&2; exit 1; }
    done
}

corelib_diff_gate DictCollections System.Collections System.Collections.NonGeneric System.Collections.Immutable System.Linq System.Reflection.Metadata

# Excluded structural shapes are metadata-only negative inputs, never executed.
build_gate_proj gates/fixtures/ldftn-local/LdftnLocalFixture.csproj
build_gate_proj gates/fixtures/structural-pointer-negative/PointerNegative.csproj
structural_pointer_negative_gate "artifacts/structural-pointer-negative-$CONFIG" "gates/fixtures/structural-pointer-negative/bin/$CONFIG/$TFM"

gchandle_equality_guards_gate "artifacts/gchandle-equality-guards-$CONFIG"

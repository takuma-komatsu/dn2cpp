#!/usr/bin/env bash
# Consolidated dictionary/set gate: Dictionary (core/API/enumeration/extra ops/
# string keys), HashSet, custom hash keys, the generic sorted collections
# (SortedDictionary/SortedList/SortedSet), the non-generic System.Collections.SortedList,
# and FrozenDictionary (FrozenDictSubset).
# Diffed exactly vs real .NET.
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
# Former gates: dict, dict-api, dict-enum, dict-more, dict-string, hashset,
# hashkey, sorted-collections, join-enumerable-subset,
# ordinal-stringset-dedup-subset, struct-key-subset, valuetuple-key-dict-subset.
source "$(dirname "$0")/_common.sh"

# System.Collections.NonGeneric carries the non-generic SortedList (NonGenericSortedListSubset);
# the generic collections come from System.Collections; System.Collections.Immutable
# carries FrozenDictionary/FrozenHashTable (FrozenDictSubset — exercises HashHelpers.Primes),
# whose ToFrozenDictionary build path reaches System.Linq.Enumerable.
corelib_diff_gate DictCollections System.Collections System.Collections.NonGeneric System.Collections.Immutable System.Linq

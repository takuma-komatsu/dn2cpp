#!/usr/bin/env bash
# The whole LinqOrdering program transpiled against the real System.Linq.dll
# (passed with -r) — the permanent gate for the real ordering pipeline
# (EnumerableSorter / OrderedEnumerable, the stable sort, and the
# CachingComparer chain behind OrderBy/OrderByDescending/ThenBy/
# ThenByDescending with default and custom IComparer<T>, plus Distinct/Except/
# etc. with a custom comparer), which the LinqCore-based real-linq gate never
# reaches. Output diffs exact vs real .NET.
#
# It is also the gate for the generic-virtual dispatcher's emit-set invariant
# ThenBy is the transpiler's only in-corpus IOrderedEnumerable<T>.
# CreateOrderedEnumerable<TKey> call, i.e. its only interface GVM, and
# LinqOrderStructKeySubset is the shape whose canonical group owner lands in the
# dispatcher's allocated case set. See gate_extra_asserts below.
# Nested tuple elements retain generic argument boundaries in iterator layouts.
source "$(dirname "$0")/_common.sh"
DN2CPP_GATE_EXTRA_CONTEXT="${DN2CPP_GATE_EXTRA_CONTEXT:-}|before-tuple-ordering"

# A dn2cpp_gvm_* dispatcher spells `&ti_<T>` out itself, so it is the one
# family of type-info names AssertNamedTypeInfosDefined cannot see — that backstop
# diffs what the compiled BODIES named, and it runs before the dispatchers are
# written. Under shared generics the dispatcher's cases are the ALLOCATED types,
# and a canonical group owner is allocated (a shared body's newobj resolves to it)
# while carrying no type-info at all, so the branch named a symbol nothing defines:
# transpile green, C++ compile red on an undeclared identifier.
#
# The compile below is the real assert — this reads the same invariant off the
# emitted text so a regression is reported as itself rather than as a clang error,
# and pins that the dispatcher exists at all (a grep that silently matches nothing
# asserts nothing). Reads only the emitted C++, so the transpile surface already in
# the cache key covers it; no DN2CPP_GATE_EXTRA_CONTEXT needed.
gate_extra_asserts() {
    local out="$1" sym syms missing=""
    syms=$(grep -ho 'if (__t == &ti_[A-Za-z0-9_]*)' "$out"/generated*.cpp \
        | sed 's/.*&//; s/)$//' | sort -u)
    if [ -z "$syms" ]; then
        echo "FAIL: no generic-virtual dispatcher case found in $out (grep pattern rot?)" >&2
        return 1
    fi
    for sym in $syms; do
        grep -q "^extern const Dn2CppTypeInfo $sym;\$" "$out/generated.h" \
            || missing="$missing $sym"
    done
    if [ -n "$missing" ]; then
        echo "FAIL: generic-virtual dispatcher branches on undeclared type-info:$missing" >&2
        return 1
    fi
    # The struct-key section's own dispatcher must still carry a real case: the
    # dispatcher drops the dead canonical branch, and dropping the real one too
    # would leave a dispatcher that only traps.
    if ! grep -q 'if (__t == &ti_Enumerable_OrderedIterator_LinqOrderStructKeySubset_Row_String)' \
            "$out"/generated*.cpp; then
        echo "FAIL: the Row/string OrderedIterator case is missing from its GVM dispatcher" >&2
        return 1
    fi
    echo "gvm dispatcher type-info names all declared: OK"

    local native before prefix
    native="$out/native.stdout"
    before="$out/before-tuple-ordering.stdout"
    gate_capture_ok "${before%.stdout}" run_bounded dotnet "$_CG_APP" before-tuple-ordering || return $?
    gate_output prefix "$native" "$before" --before '== tuple ordering generic boundaries ==' --policy crlf || return $?
    for line in '== tuple ordering generic boundaries ==' 'pair a/y' 'pair a/z' 'pair b/x' \
            'triple a/z/1' 'triple a/y/3' 'triple b/x/2' \
            'left a/y/1' 'left b/x/2' 'right a/y/1' 'right b/x/2' \
            'tuple ordering generic boundaries end'; do
        gate_output byte-witness "$native" "$line" --count any --policy crlf \
            || { echo "FAIL: tuple ordering witness missing: $line" >&2; return 1; }
    done
    echo "tuple ordering witnesses and unchanged prefix: OK"
}

corelib_diff_gate LinqOrdering \
    System.Linq System.Collections System.Runtime

#!/usr/bin/env bash
# Consolidated enum gate: [Flags] enums, enums in string interpolation,
# Enum.ToString, value->name ToString, external-assembly enums, and the real
# System.Enum..cctor (its (RuntimeType)typeof(bool) castclass must verify
# against the shared Type header — EnumCctorRuntimeTypeSubset runs the real cctor
# via RunClassConstructor and pins the underlying-type surface). Diffed exactly
# vs real .NET.
# EnumWide64Subset is not a theme but a WIDTH: every lowering that reads an enum's
# declared member table, driven with a long/ulong-underlying enum whose constants do
# not survive the int32 model — the interpolation hole, the generic Enum.*<T>
# statics, the packed E[] materialization, the boxed ToString slot, the non-generic
# Type-driven statics, Convert.ChangeType's own boxed-enum reader, and Enum.Format.
# Its members include long.MinValue and 1UL<<63, which is also the only assert that
# the emitted enummembers_ table renders a C++ literal that COMPILES.
# Its last two blocks are a WIDTH subject the heading does not say: Enum.Parse's numeric
# token is range-checked at the UNDERLYING type, not at the int32/int64 model, so they
# span all eight underlyings — including the OverflowException-vs-Argument-
# Exception split a name-path fall-through erases — and then read a boxed enum's payload
# width back off a run-time type handle (Activator / GetUninitializedObject / ToObject),
# which is what sizes the allocation.
# Generic Enum.Parse/TryParse cover all four ReadOnlySpan<char> overloads; each
# must honor the active slice bounds before consulting the enum member table.
# EnumJoinSubset: string.Join<T>, StringBuilder.AppendJoin<T> and string.Concat<T>
# over enum elements from an array and a List<T> — each element by name, at every
# underlying width — and the ArgumentNullException a null sequence raises; then
# over an IEnumerable<T> (an array, a List<T> or an iterator behind it) and other
# collections, disposing the enumerator when the join ends or an element read throws.
# Undefined unsigned values keep their sign through array and enumeration joins,
# including flags and enums with no declared members.
# A cursor-only enum retains its metadata, and AppendJoin keeps the partial
# builder when Current or Dispose throws, including Dispose replacing Current's fault.
# Former gates: enum-flags, enum-interp, enum-tostring, enum-value-tostring,
# external-enum.
source "$(dirname "$0")/_common.sh"

gate_extra_asserts() {
    local out="$1" native before prefix line
    native=$(run_bounded "./$out/EnumOps")
    native=$(strip_cr_win "$native")
    before=$(dotnet "$_CG_APP" before-enum-join)
    before=$(strip_cr_win "$before")
    prefix=$(awk '/^== enum Join ==$/ { exit } { print }' <<< "$native")
    assert_output "$prefix" "$before"
    for line in '== enum Join ==' \
        '== enum Concat ==' \
        '== enum Join over enumerables ==' \
        'throwing cursor: cursor 2 3' \
        'ushort: B,A,40000' \
        'ulong: BA18446744073709551615' \
        'uint undefined: 4000000000 4000000000' \
        'unsigned flags: 4294967295 18446744073709551615' \
        'empty enums: 4294967295 18446744073709551615' \
        '== enum cursor AppendJoin ==' \
        'cursor identity: Low,High LowHigh' \
        'current: current prefix:Low, 1' \
        'current and dispose: dispose prefix:Low, 2' \
        'dispose: dispose prefix:Low,High 3' \
        'first current: current prefix: 4' \
        'null enum receiver: NullReferenceException'; do
        grep -Fxq "$line" <<< "$native" \
            || { echo "FAIL: sequence witness missing: $line" >&2; exit 1; }
    done
}

corelib_diff_gate EnumOps

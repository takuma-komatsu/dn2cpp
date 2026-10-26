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
# Retained Enum.InternalGetCorElementType is invoked through reflection for every
# integral underlying width; the callable body must use native enum type-info.
# InternalGetUnderlyingType retains its primitive table semantics, including null
# holes and the bounds failure for a vector type.
# Former gates: enum-flags, enum-interp, enum-tostring, enum-value-tostring,
# external-enum.
source "$(dirname "$0")/_common.sh"

comparison_prior_extra_asserts() {
    local out="$1" native before prefix line
    gate_capture_ok "$out/assert-native-1" run_bounded "./$out/EnumOps"
    native="$out/assert-native-1.stdout"

    gate_capture_ok "$out/assert-before-2" run_bounded dotnet "$_CG_APP" before-enum-join
    before="$out/assert-before-2.stdout"

    prefix="$out/assert-prefix-5.stdout"

    gate_output select "$native" "$prefix" --before '== enum Join ==' --policy crlf
    gate_output compare "$prefix" "$before"
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
        gate_output witness "$native" "$line" --count any --policy crlf \
            || { echo "FAIL: sequence witness missing: $line" >&2; exit 1; }
    done
}

# Default order and equality preserve the earlier bucket and run the appended cases.
gate_extra_asserts() {
    local out="$1" native before prefix line
    comparison_prior_extra_asserts "$out"
    gate_capture_ok "$out/assert-native-3" run_bounded "./$out/EnumOps"
    native="$out/assert-native-3.stdout"

    gate_capture_ok "$out/assert-before-4" run_bounded dotnet "$_CG_APP" before-default-comparison
    before="$out/assert-before-4.stdout"

    prefix="$out/assert-prefix-6.stdout"

    gate_output select "$native" "$prefix" --before '== default comparison validation ==' --policy crlf
    gate_output compare "$prefix" "$before"
    for line in '== default comparison validation ==' \
        '== enum CompareTo ==' \
        'byte: 247 -247' \
        'sbyte: 200 -200' \
        'short: 60000 -60000' \
        'ushort: 64999 -64999' \
        'default comparison validation end'; do
        gate_output witness "$native" "$line" --count any --policy crlf \
            || { echo "FAIL: EnumOps comparison coverage missing: $line" >&2; exit 1; }
    done

    gate_capture_ok "$out/assert-before-element-type" run_bounded dotnet "$_CG_APP" before-enum-element-type || return 1
    before="$out/assert-before-element-type.stdout"
    prefix="$out/assert-prefix-element-type.stdout"
    gate_output select "$native" "$prefix" --before '== enum primitive element type ==' --policy crlf || return 1
    gate_output compare "$prefix" "$before" || return 1
    for line in '== enum primitive element type ==' \
        'i1:4:4' 'u1:5:5' 'i2:6:6' 'u2:7:7' \
        'i4:8:8' 'u4:9:9' 'i8:10:10' 'u8:11:11' \
        'type:i1:4' 'type:u8:11' 'type:bool:2' 'type:char:3' \
        'type:float:12' 'type:double:13' 'type:nint:24' 'type:nuint:25' \
        'type:void:1' 'type:decimal:17' 'type:object:18' 'type:string:18' \
        'type:vector:29' 'type:matrix:20' 'type:typedref:22' \
        'enum primitive element type end'; do
        gate_output byte-witness "$native" "$line" --count 1 --policy crlf || return 1
    done

    gate_capture_ok "$out/assert-before-underlying-type" run_bounded dotnet "$_CG_APP" before-enum-underlying-type || return 1
    before="$out/assert-before-underlying-type.stdout"
    prefix="$out/assert-prefix-underlying-type.stdout"
    gate_output select "$native" "$prefix" --before '== enum internal underlying type ==' --policy crlf || return 1
    gate_output compare "$prefix" "$before" || return 1
    for line in '== enum internal underlying type ==' \
        'underlying:i1:System.SByte' 'underlying:u1:System.Byte' \
        'underlying:i2:System.Int16' 'underlying:u2:System.UInt16' \
        'underlying:i4:System.Int32' 'underlying:u4:System.UInt32' \
        'underlying:i8:System.Int64' 'underlying:u8:System.UInt64' \
        'underlying:bool:System.Boolean' 'underlying:char:System.Char' \
        'underlying:float:System.Single' 'underlying:double:System.Double' \
        'underlying:nint:System.IntPtr' 'underlying:nuint:System.UIntPtr' \
        'underlying:void:null' 'underlying:decimal:null' 'underlying:object:null' \
        'underlying:string:null' 'underlying:matrix:null' 'underlying:typedref:null' \
        'underlying:vector:IndexOutOfRangeException' 'underlying:null:NullReferenceException' \
        'enum internal underlying type end'; do
        gate_output byte-witness "$native" "$line" --count 1 --policy crlf || return 1
    done
}

corelib_diff_gate EnumOps --link-xml samples/dotnet/EnumOps/keep-element-type.xml

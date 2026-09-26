#!/usr/bin/env bash
# Boxing a PRIMITIVE value type: what `box`/`unbox.any` do to a type the runtime
# models as an intrinsic (no emitted ti_*), and what the Object virtuals recover
# from the resulting handle. Consolidated bucket — one sample project, one .cs per
# section, driven in order by samples/dotnet/BoxingPrimitives/Program.cs. CoreLib
# only; the program is its own oracle (exact diff vs real .NET).
#
#   * BoxPrimitiveSubset — char + the small integers (Byte/SByte/Int16/UInt16/
#     UInt32): box, ToString (Object.ToString / value concat), unbox round trip.
#     Each gained a runtime Dn2CppTypeInfo + TypeInfoExpr entry; before that,
#     `box of Char is not supported`.
#   * IntPtrBox — IntPtr/UIntPtr as primitives (dn2cpp_intptr_type /
#     dn2cpp_uintptr_type, 8-byte payload): GetType().FullName, statically typed
#     and boxed ToString, interpolation holes, unbox round trip, `is`
#     discrimination, boxed Equals/GetHashCode, and the >int-range boundary.
#   * BoxedComparableSubset — NOT a boxing test at heart: the unconstrained
#     `(IComparable<T>)box.CompareTo` cast form and the box-to-interface form,
#     both devirtualized to a typed three-way compare, plus the
#     Comparer<T>.Default order real .NET routes through ObjectComparer<T>
#     (Comparer<object>, object[] Array.Sort) and the Vector128<T> case that must
#     throw on both sides because Vector128<T> is not orderable.
#   * PrimitiveCompareHashSubset — an EMIT-ONLY intercept (Int32/UInt32 are
#     already intrinsic types, so reachability already cut the BCL bodies):
#     int/uint CompareTo returning the SIGN not the difference, and the
#     int/long/ulong/IntPtr GetHashCode folds — through comparer lambdas as well
#     as direct calls, which is the shape dn2cpp's own catch-clause and flag-enum
#     sorts reach them in.
#   * BoxedBuiltinItfDispatchSubset — NOT a boxing test either: the runtime ITABLE
#     over a boxed built-in. The const primitive/Decimal/date-time type-infos
#     take no dispatch map, so dn2cpp_resolve_interface_walk's last arm hands back a
#     slot table built from the interface's own emitted method rows; the section
#     prints each type TEST beside the CALL, since the invariant is that the set
#     answering `is IConvertible` and the set that can dispatch one are one set. Its
#     IComparable half is also the only coverage of dn2cpp_object_compare's Decimal
#     and date/time ordering arms.
#   * BoxedNegativeItfSubset — the NEGATIVE half of that itable test, and not a
#     boxing test either: `is IEnumerable` / IList / IDictionary / ICollection /
#     IEnumerable<char> over boxed float/double/int/enum/DateTime/decimal, each
#     of which must answer False, against string and int[] as the True controls.
#     The positive matrix above cannot see a type test that OVER-admits, and the
#     arm one lets run reads the box payload as a type pointer.
#   * ConstrainedObjectEqualsSubset — the RECEIVER side of that hazard, over a
#     generic wrapper: `constrained. !T; callvirt object::Equals(object)` boxes
#     only the ARGUMENT, so a receiver left unboxed at the call site is its own
#     bits read as a type-info (0.003f as a Dn2CppTypeInfo* — a SIGSEGV, seen in
#     a real game's settings comparison). Every T kind that reaches the prefix:
#     the primitives, enum, an overriding struct, decimal/DateTime, string and
#     object — the last four being the arms that already boxed.
#   * PrimitiveObjectCompareSubset — both CompareTo siblings on every scalar primitive:
#     typed ordering, plus the object overload's null, exact-box and foreign-type paths.
#     Sub-word integers and Char pin their raw difference directly and via IComparable.
#   * PrimitiveEqualsObjectSubset — Equals(object) over every non-floating primitive
#     value type: same-type payload, sub-word/pointer edge cases, wrapper structs, and
#     virtual method groups over boxed byte/int/nint receivers.
#   * BoxedClrRelationSubset — the CLR interfaces a boxed primitive, decimal or date
#     type implements beyond its dispatch arm (INumber<T>, IBinaryInteger<T>,
#     IAdditionOperators, IMinMaxValue<T>, IParsable<T>/ISpanParsable<T>,
#     IUtf8SpanFormattable, ISerializable, IDeserializationCallback), asked through the
#     type test and IsAssignableFrom with negatives beside them; String's asked through
#     reflection alone; and IUtf8SpanFormattable.TryFormat called through the
#     interface into a fitting and a too-small buffer. Its extra asserts pin that
#     Int32's answers come from the relation rows the init prologue installs, and that
#     the output before the section is unchanged.
#   * ObjectVirtualDispatchSubset — base calls to the Object and ValueType virtuals
#     inside an override (`call`, not `callvirt`): Object's type name, reference
#     equality and identity hash, ValueType's type name and field-by-field equality
#     and hash, never a dispatch back into the override; and RuntimeHelpers.GetHashCode
#     agreeing with the default Object.GetHashCode. A struct boxed where no
#     formatting call follows the box still formats through its ToString override,
#     directly, through string.Join and through the box of a Nullable<T> of it, and
#     a method group over Object's ToString, Equals or GetHashCode (`ldvirtftn`)
#     runs what a callvirt runs on a class that does not override it, a boxed
#     primitive, enum or struct, a string and an array. Its extra asserts pin that
#     the output before the section is unchanged.
#   * ConstrainedObjectCompareSubset — IComparable.CompareTo(object) on every scalar
#     primitive, string, decimal and date/time type through a `constrained. !T` call,
#     a boxed receiver and the direct overload: the order, a null argument, and a box
#     of another type rejected with .NET's "Object must be of type X." message. Its
#     extra asserts pin that the output before the section is unchanged.
#
# The culture pin is the driver's first two statements, NOT an InvariantGlobalization
# property — that one pins only the oracle and drops ICU (stated at the
# csproj's own PropertyGroup, where the absence would otherwise read as an oversight).
source "$(dirname "$0")/_common.sh"

gate_extra_asserts() {
    local out="$1" managed
    for managed in Byte SByte Int16 UInt16 IntPtr UIntPtr; do
        if awk -v managed="$managed" '
                BEGIN {
                    sig = managed "_Equals_m[0-9]+\\(t_System_" managed "\\* [^,]+, Dn2CppObject\\*"
                }
                $0 == "// System." managed "::Equals" { real_body = 1; next }
                real_body {
                    if ($0 ~ sig) found = 1
                    real_body = 0
                }
                END { exit !found }
            ' "$out/generated.h" "$out"/generated*.cpp; then
            echo "FAIL: real System.${managed}.Equals(object) body remained emitted" >&2
            return 1
        fi
    done
    echo "sub-word and pointer Equals(object) real bodies are absent: OK"

    run_bounded "$out/BoxingPrimitives$EXE_EXT" > "$out/native.stdout"
    grep -Fxq '== boxed CLR relations ==' "$out/native.stdout"
    grep -Fxq 'int INumber<int>: is=True assignable=True' "$out/native.stdout"
    grep -Fxq 'int INumber<long>: is=False assignable=False' "$out/native.stdout"
    grep -Fxq 'utf8 int X4: True:4:30304646' "$out/native.stdout"
    DN2CPP_BEFORE_BOXED_CLR_RELATIONS=1 run_bounded "$out/BoxingPrimitives$EXE_EXT" \
        > "$out/before-boxed-clr-relations.stdout"
    sed '/^== boxed CLR relations ==/,$d' "$out/native.stdout" > "$out/boxed-clr-relations-prefix.stdout"
    diff -u <(strip_cr_win_file "$out/before-boxed-clr-relations.stdout") \
        <(strip_cr_win_file "$out/boxed-clr-relations-prefix.stdout")
    # Int32's row set in the init prologue's relation table names INumber<int>.
    local int_rows
    int_rows=$(grep -ho '{ &dn2cpp_int32_type, rel_itfs_[0-9]*,' "$out"/generated*.cpp \
        | grep -o 'rel_itfs_[0-9]*' || true)
    if [ -z "$int_rows" ] || ! grep -Eq "^static const Dn2CppInterfaceEntry $int_rows\[\] = .*\{ &ti_System_Numerics_INumber_Int32, nullptr \}" \
            "$out"/generated*.cpp; then
        echo "FAIL: Int32's relation rows do not name INumber<int>" >&2
        return 1
    fi
    echo "boxed CLR relations answered from the relation rows: OK"

    grep -Fxq '== object virtual dispatch ==' "$out/native.stdout"
    grep -Fxq 'class base ToString: base-calls:ObjectVirtualDispatchSubset.BaseCalls' "$out/native.stdout"
    grep -Fxq 'identity hash: True/True/True' "$out/native.stdout"
    grep -Fxq 'struct base Equals: True/False/False/False' "$out/native.stdout"
    grep -Fxq 'boxed struct ToString: stashed:4' "$out/native.stdout"
    grep -Fxq 'boxed nullable struct: wrapped:7/label:7' "$out/native.stdout"
    grep -Fxq 'method group ToString: ObjectVirtualDispatchSubset.Plain/named/5/grouped:3/ObjectVirtualDispatchSubset.Pair/text/System.Int32[]/High' "$out/native.stdout"
    grep -Fxq 'method group Equals/GetHashCode: True/False/True/False/True/11/5/True/False' "$out/native.stdout"
    DN2CPP_BEFORE_OBJECT_VIRTUALS=1 run_bounded "$out/BoxingPrimitives$EXE_EXT" \
        > "$out/before-object-virtuals.stdout"
    sed '/^== object virtual dispatch ==/,$d' "$out/native.stdout" > "$out/object-virtuals-prefix.stdout"
    diff -u <(strip_cr_win_file "$out/before-object-virtuals.stdout") \
        <(strip_cr_win_file "$out/object-virtuals-prefix.stdout")

    grep -Fxq '== constrained CompareTo(object) ==' "$out/native.stdout"
    grep -Fxq 'ccmp byte: 197 -197 1 | ArgumentException: Object must be of type Byte. | ArgumentException: Object must be of type Byte.' "$out/native.stdout"
    DN2CPP_BEFORE_CONSTRAINED_OBJECT_COMPARE=1 run_bounded "$out/BoxingPrimitives$EXE_EXT" \
        > "$out/before-constrained-object-compare.stdout"
    sed '/^== constrained CompareTo(object) ==/,$d' "$out/native.stdout" > "$out/constrained-object-compare-prefix.stdout"
    diff -u <(strip_cr_win_file "$out/before-constrained-object-compare.stdout") \
        <(strip_cr_win_file "$out/constrained-object-compare-prefix.stdout")
}

corelib_diff_gate BoxingPrimitives

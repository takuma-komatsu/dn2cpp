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
#
# The culture pin is the driver's first two statements, NOT an InvariantGlobalization
# property — that one pins only the oracle and drops ICU (stated at the
# csproj's own PropertyGroup, where the absence would otherwise read as an oversight).
source "$(dirname "$0")/_common.sh"

corelib_diff_gate BoxingPrimitives

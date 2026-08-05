using System.Reflection.Metadata;
using System.Text;
using SRME = System.Reflection.Metadata.Ecma335.MetadataTokens;

namespace Dn2Cpp;

internal sealed partial class MethodCompiler
{
    private bool TryEmitConvertIntrinsic(string declType, string name, MethodSignature<TypeDesc> sig)
    {
        if (declType == "System.Convert" && TryEmitConvertNarrowIntrinsic(name, sig))
            return true;

        switch (declType, name)
        {

            // Convert.GetTypeCode(object): the value's TypeCode. Real .NET asks the
            // value's own IConvertible.GetTypeCode(); the two answers coincide wherever
            // the code is not Object, so the helper answers from the runtime type and
            // throws on that one shape. Null is TypeCode.Empty.
            case ("System.Convert", "GetTypeCode"):
            {
                var v = Pop();
                string ct0 = CppTypes.Of(sig.ReturnType);
                Push(CppTypes.KindOf(sig.ReturnType), ct0,
                    $"({ct0})dn2cpp_convert_get_type_code({Cast(v, "Dn2CppObject*")})");
                return true;
            }
            // ---- Convert numeric conversions ----
            // ToInt32: string -> parse; double/float -> banker's round (range-checked);
            // long -> range-checked narrowing; bool -> 0/1; int -> identity.
            // Convert.ChangeType(object, Type[, IFormatProvider]) — runtime-Type-driven
            // conversion via the IConvertible matrix. The optional provider is ignored
            // (InvariantCulture, as everywhere in the transpiler). 2nd arg is the Type.
            case ("System.Convert", "ChangeType")
                when sig.ParameterTypes is [_, { } ct, ..] && ct.ToString() == "System.Type":
            {
                if (sig.ParameterTypes.Length == 3)
                    Pop(); // IFormatProvider
                var target = Pop();
                var value = Pop();
                Push(StackKind.Ref, "Dn2CppObject*",
                    $"dn2cpp_convert_change_type({Cast(value, "Dn2CppObject*")}, {Cast(target, "Dn2CppType*")})");
                return true;
            }
            // Convert.ChangeType(object, TypeCode[, IFormatProvider]) — same matrix, target
            // named by a TypeCode (an int32 enum on the stack) instead of a runtime Type.
            case ("System.Convert", "ChangeType")
                when sig.ParameterTypes is [_, { } ct2, ..] && ct2.ToString() == "System.TypeCode":
            {
                if (sig.ParameterTypes.Length == 3)
                    Pop(); // IFormatProvider
                var code = Pop();
                var value = Pop();
                Push(StackKind.Ref, "Dn2CppObject*",
                    $"dn2cpp_convert_change_type_code({Cast(value, "Dn2CppObject*")}, {Cast(code, "int32_t")})");
                return true;
            }
            // Convert.DefaultToType(IConvertible value, Type targetType, IFormatProvider)
            // — the internal fallback each primitive's IConvertible.ToType delegates to;
            // the same runtime-Type-driven IConvertible matrix as ChangeType(object, Type)
            // above. The value arrives boxed, the provider is invariant and dropped.
            case ("System.Convert", "DefaultToType")
                when sig.ParameterTypes is [_, { } dtt, _] && dtt.ToString() == "System.Type":
            {
                Pop(); // IFormatProvider (invariant)
                var target = Pop();
                var value = Pop();
                Push(StackKind.Ref, "Dn2CppObject*",
                    $"dn2cpp_convert_change_type({Cast(value, "Dn2CppObject*")}, {Cast(target, "Dn2CppType*")})");
                return true;
            }
            case ("System.Convert", "ToInt32") when sig.ParameterTypes is [{ IsString: true }]
                or [{ IsString: true }, { Kind: TypeKind.Class }]:
                if (sig.ParameterTypes.Length == 2) Pop(); // provider (invariant) — dropped
                Push(StackKind.I4, "int32_t", $"dn2cpp_int_parse({Cast(Pop(), "Dn2CppString*")})");
                return true;
            case ("System.Convert", "ToInt32")
                when sig.ParameterTypes is [{ Primitive: PrimitiveTypeCode.Double or PrimitiveTypeCode.Single }]:
                Push(StackKind.I4, "int32_t", $"dn2cpp_convert_r8_to_i32({Cast(Pop(), "double")})");
                return true;
            case ("System.Convert", "ToInt32")
                when sig.ParameterTypes is [{ Primitive: PrimitiveTypeCode.Int64 }]:
                Push(StackKind.I4, "int32_t", $"dn2cpp_convert_i64_to_i32({Cast(Pop(), "int64_t")})");
                return true;
            // An UNSIGNED source is read at its own width first, exactly as
            // TryEmitConvertNarrowIntrinsic's integer arm does: a UInt64 sits in an
            // int64_t slot and a UInt32 in an int32_t one, so narrowing through the
            // signed helper hands ulong.MaxValue to it as -1 — which is in range, so
            // it answers -1 where .NET throws OverflowException.
            case ("System.Convert", "ToInt32")
                when sig.ParameterTypes is [{ Primitive: PrimitiveTypeCode.UInt64 }]:
                Push(StackKind.I4, "int32_t",
                    $"(int32_t)dn2cpp_convert_u64_checked((uint64_t)({Pop().Expr}), 2147483647ULL)");
                return true;
            case ("System.Convert", "ToInt32")
                when sig.ParameterTypes is [{ Primitive: PrimitiveTypeCode.UInt32 }]:
                Push(StackKind.I4, "int32_t",
                    $"(int32_t)dn2cpp_convert_i64_checked((int64_t)(uint32_t)({Pop().Expr}), "
                    + "-2147483648LL, 2147483647LL)");
                return true;
            case ("System.Convert", "ToInt32")
                when sig.ParameterTypes is [{ Primitive: PrimitiveTypeCode.Boolean }]:
                Push(StackKind.I4, "int32_t", $"(({Pop().Expr}) != 0 ? 1 : 0)");
                return true;
            case ("System.Convert", "ToInt32")
                when sig.ParameterTypes is [{ Primitive: PrimitiveTypeCode.Int32
                    or PrimitiveTypeCode.SByte or PrimitiveTypeCode.Byte or PrimitiveTypeCode.Int16 or PrimitiveTypeCode.UInt16 or PrimitiveTypeCode.Char }]:
                Push(StackKind.I4, "int32_t", Cast(Pop(), "int32_t"));
                return true;

            // ToInt64: string -> parse; double/float -> banker's round (range-checked);
            // int/bool -> widen.
            case ("System.Convert", "ToInt64") when sig.ParameterTypes is [{ IsString: true }]
                or [{ IsString: true }, { Kind: TypeKind.Class }]:
                if (sig.ParameterTypes.Length == 2) Pop();
                Push(StackKind.I8, "int64_t", $"dn2cpp_long_parse({Cast(Pop(), "Dn2CppString*")})");
                return true;
            case ("System.Convert", "ToInt64")
                when sig.ParameterTypes is [{ Primitive: PrimitiveTypeCode.Double or PrimitiveTypeCode.Single }]:
                Push(StackKind.I8, "int64_t", $"dn2cpp_convert_r8_to_i64({Cast(Pop(), "double")})");
                return true;
            case ("System.Convert", "ToInt64")
                when sig.ParameterTypes is [{ Primitive: PrimitiveTypeCode.Boolean }]:
                Push(StackKind.I8, "int64_t", $"(int64_t)(({Pop().Expr}) != 0 ? 1 : 0)");
                return true;
            // UInt64 is the only integer source that can exceed Int64.MaxValue; UInt32
            // must zero-extend out of its int32_t slot (uint.MaxValue is 4294967295,
            // not -1). Both, again, at the source's own width.
            case ("System.Convert", "ToInt64")
                when sig.ParameterTypes is [{ Primitive: PrimitiveTypeCode.UInt64 }]:
                Push(StackKind.I8, "int64_t",
                    $"dn2cpp_convert_u64_checked((uint64_t)({Pop().Expr}), 9223372036854775807ULL)");
                return true;
            case ("System.Convert", "ToInt64")
                when sig.ParameterTypes is [{ Primitive: PrimitiveTypeCode.UInt32 }]:
                Push(StackKind.I8, "int64_t", $"(int64_t)(uint32_t)({Pop().Expr})");
                return true;
            case ("System.Convert", "ToInt64")
                when sig.ParameterTypes is [{ Primitive: PrimitiveTypeCode.Int32
                    or PrimitiveTypeCode.Int64
                    or PrimitiveTypeCode.SByte or PrimitiveTypeCode.Byte or PrimitiveTypeCode.Int16 or PrimitiveTypeCode.UInt16 or PrimitiveTypeCode.Char }]:
                Push(StackKind.I8, "int64_t", $"(int64_t)({Cast(Pop(), "int64_t")})");
                return true;

            // ToDouble: string -> parse; numeric -> widen to double.
            case ("System.Convert", "ToDouble") when sig.ParameterTypes is [{ IsString: true }]
                or [{ IsString: true }, { Kind: TypeKind.Class }]:
                if (sig.ParameterTypes.Length == 2) Pop();
                Push(StackKind.R8, "double", $"dn2cpp_double_parse({Cast(Pop(), "Dn2CppString*")})");
                return true;
            // The unsigned 32/64-bit sources widen from their own width — Cast()ing the
            // signed slot to double turns uint.MaxValue into -1 rather than 4294967295.
            case ("System.Convert", "ToDouble")
                when sig.ParameterTypes is [{ Primitive: PrimitiveTypeCode.UInt32 }]:
                Push(StackKind.R8, "double", $"(double)(uint32_t)({Pop().Expr})");
                return true;
            case ("System.Convert", "ToDouble")
                when sig.ParameterTypes is [{ Primitive: PrimitiveTypeCode.UInt64 }]:
                Push(StackKind.R8, "double", $"(double)(uint64_t)({Pop().Expr})");
                return true;
            case ("System.Convert", "ToDouble")
                when sig.ParameterTypes is [{ Primitive: PrimitiveTypeCode.Int32
                    or PrimitiveTypeCode.Int64
                    or PrimitiveTypeCode.Double or PrimitiveTypeCode.Single
                    or PrimitiveTypeCode.SByte or PrimitiveTypeCode.Byte or PrimitiveTypeCode.Int16 or PrimitiveTypeCode.UInt16 or PrimitiveTypeCode.Char }]:
                Push(StackKind.R8, "double", $"(double)({Cast(Pop(), "double")})");
                return true;

            // ToBoolean: string -> "true"/"false"; numeric -> != 0.
            case ("System.Convert", "ToBoolean") when sig.ParameterTypes is [{ IsString: true }]
                or [{ IsString: true }, { Kind: TypeKind.Class }]:
                if (sig.ParameterTypes.Length == 2) Pop();
                Push(StackKind.I4, "int32_t", $"dn2cpp_convert_to_bool({Cast(Pop(), "Dn2CppString*")})");
                return true;
            case ("System.Convert", "ToBoolean")
                when sig.ParameterTypes is [{ Primitive: PrimitiveTypeCode.Int32 or PrimitiveTypeCode.UInt32
                    or PrimitiveTypeCode.Int64 or PrimitiveTypeCode.UInt64
                    or PrimitiveTypeCode.Double or PrimitiveTypeCode.Single
                    or PrimitiveTypeCode.Boolean
                    or PrimitiveTypeCode.SByte or PrimitiveTypeCode.Byte
                    or PrimitiveTypeCode.Int16 or PrimitiveTypeCode.UInt16 }]:
                Push(StackKind.I4, "int32_t", $"(({Pop().Expr}) != 0 ? 1 : 0)");
                return true;

            // ToSingle: string -> parse; numeric -> narrow/widen to float (rides
            // the R8 stack slot as the F type, like Single.Parse above).
            case ("System.Convert", "ToSingle") when sig.ParameterTypes is [{ IsString: true }]
                or [{ IsString: true }, { Kind: TypeKind.Class }]:
                if (sig.ParameterTypes.Length == 2) Pop();
                Push(StackKind.R8, "double", $"(double)dn2cpp_float_parse({Cast(Pop(), "Dn2CppString*")})");
                return true;
            case ("System.Convert", "ToSingle")
                when sig.ParameterTypes is [{ Primitive: PrimitiveTypeCode.UInt32 }]:
                Push(StackKind.R8, "double", $"(double)(float)(uint32_t)({Pop().Expr})");
                return true;
            case ("System.Convert", "ToSingle")
                when sig.ParameterTypes is [{ Primitive: PrimitiveTypeCode.UInt64 }]:
                Push(StackKind.R8, "double", $"(double)(float)(uint64_t)({Pop().Expr})");
                return true;
            case ("System.Convert", "ToSingle")
                when sig.ParameterTypes is [{ Primitive: PrimitiveTypeCode.Int32
                    or PrimitiveTypeCode.Int64
                    or PrimitiveTypeCode.Double or PrimitiveTypeCode.Single
                    or PrimitiveTypeCode.SByte or PrimitiveTypeCode.Byte or PrimitiveTypeCode.Int16 or PrimitiveTypeCode.UInt16 or PrimitiveTypeCode.Char }]:
                Push(StackKind.R8, "double", $"(double)(float)({Cast(Pop(), "double")})");
                return true;

            // Convert.To*(object[, IFormatProvider]) — boxed-source IConvertible overloads:
            // a System.Object source dispatched on its runtime type, distinct from the
            // statically-typed primitive/string overloads above. The optional provider
            // (InvariantCulture) is popped + ignored.
            case ("System.Convert", "ToInt64") when sig.ParameterTypes is [{ IsObject: true }, ..]:
                if (sig.ParameterTypes.Length == 2) Pop();
                Push(StackKind.I8, "int64_t", $"dn2cpp_convert_obj_to_i64({Cast(Pop(), "Dn2CppObject*")})");
                return true;
            // ToInt32(object[, provider]) — the i64 boxed read narrowed through the same
            // range-checked helper the statically-typed long overload uses.
            case ("System.Convert", "ToInt32") when sig.ParameterTypes is [{ IsObject: true }, ..]:
                if (sig.ParameterTypes.Length == 2) Pop();
                Push(StackKind.I4, "int32_t",
                    $"dn2cpp_convert_i64_to_i32(dn2cpp_convert_obj_to_i64({Cast(Pop(), "Dn2CppObject*")}))");
                return true;
            // ToChar over an integer source — range-checked to the UTF-16 code-unit
            // range (the sub-word IConvertible.ToChar impls delegate here).
            case ("System.Convert", "ToChar")
                when sig.ParameterTypes is [{ Primitive: PrimitiveTypeCode.Int32 or PrimitiveTypeCode.UInt32
                    or PrimitiveTypeCode.SByte or PrimitiveTypeCode.Byte or PrimitiveTypeCode.Int16 or PrimitiveTypeCode.UInt16 or PrimitiveTypeCode.Char }]:
                Push(StackKind.I4, "int32_t", $"(int32_t)dn2cpp_convert_i32_to_char({Cast(Pop(), "int32_t")})");
                return true;
            // ToChar(string[, provider]) — the single-char string form String's
            // IConvertible.ToChar delegates to.
            case ("System.Convert", "ToChar") when sig.ParameterTypes is [{ IsString: true }]
                or [{ IsString: true }, { Kind: TypeKind.Class }]:
                if (sig.ParameterTypes.Length == 2) Pop();
                Push(StackKind.I4, "int32_t", $"(int32_t)dn2cpp_convert_str_to_char({Cast(Pop(), "Dn2CppString*")})");
                return true;
            case ("System.Convert", "ToBoolean") when sig.ParameterTypes is [{ IsObject: true }, ..]:
                if (sig.ParameterTypes.Length == 2) Pop();
                Push(StackKind.I4, "int32_t", $"dn2cpp_convert_obj_to_bool({Cast(Pop(), "Dn2CppObject*")})");
                return true;
            case ("System.Convert", "ToChar") when sig.ParameterTypes is [{ IsObject: true }, ..]:
                if (sig.ParameterTypes.Length == 2) Pop();
                Push(StackKind.I4, "int32_t", $"dn2cpp_convert_obj_to_char({Cast(Pop(), "Dn2CppObject*")})");
                return true;
            case ("System.Convert", "ToUInt64") when sig.ParameterTypes is [{ IsObject: true }, ..]:
                if (sig.ParameterTypes.Length == 2) Pop();
                Push(StackKind.I8, "int64_t", $"(int64_t)(uint64_t)dn2cpp_convert_obj_to_i64({Cast(Pop(), "Dn2CppObject*")})");
                return true;
            case ("System.Convert", "ToSingle") when sig.ParameterTypes is [{ IsObject: true }, ..]:
                if (sig.ParameterTypes.Length == 2) Pop();
                Push(StackKind.R8, "double", $"(double)(float)dn2cpp_convert_obj_to_f64({Cast(Pop(), "Dn2CppObject*")})");
                return true;
            case ("System.Convert", "ToDouble") when sig.ParameterTypes is [{ IsObject: true }, ..]:
                if (sig.ParameterTypes.Length == 2) Pop();
                Push(StackKind.R8, "double", $"dn2cpp_convert_obj_to_f64({Cast(Pop(), "Dn2CppObject*")})");
                return true;

            // ToDecimal: string -> NumberStyles.Number parse (null -> 0m); numeric
            // sources ride the decimal ctor widths; bool -> 0m/1m; decimal identity;
            // a boxed object dispatches on its runtime type.
            case ("System.Convert", "ToDecimal") when sig.ParameterTypes is [{ IsString: true }]
                or [{ IsString: true }, { Kind: TypeKind.Class }]:
                if (sig.ParameterTypes.Length == 2) Pop();
                Push(StackKind.Struct, "Dn2CppDecimal",
                    $"dn2cpp_convert_str_to_decimal({Cast(Pop(), "Dn2CppString*")})");
                return true;
            case ("System.Convert", "ToDecimal")
                when sig.ParameterTypes is [{ Primitive: PrimitiveTypeCode.Boolean }]:
                Push(StackKind.Struct, "Dn2CppDecimal",
                    $"dn2cpp_decimal_from_i4(({Pop().Expr}) != 0 ? 1 : 0)");
                return true;
            case ("System.Convert", "ToDecimal") when sig.ParameterTypes is [{ IsObject: true }, ..]:
                if (sig.ParameterTypes.Length == 2) Pop();
                Push(StackKind.Struct, "Dn2CppDecimal",
                    $"dn2cpp_convert_obj_to_decimal({Cast(Pop(), "Dn2CppObject*")})");
                return true;
            case ("System.Convert", "ToDecimal")
                when sig.ParameterTypes is [{ Kind: TypeKind.Primitive }]:
            {
                var v = Pop();
                Push(StackKind.Struct, "Dn2CppDecimal", DecimalFromScalar(v, sig.ParameterTypes[0]));
                return true;
            }
            case ("System.Convert", "ToDecimal")
                when sig.ParameterTypes is [var decSrc] && IsDecimal(decSrc):
            {
                var v = Pop();
                Push(StackKind.Struct, "Dn2CppDecimal", DecVal(v));
                return true;
            }

            // ToDateTime: string -> the invariant DateTime.Parse machinery
            // (null -> DateTime.MinValue); DateTime identity; a boxed object
            // passes a DateTime through or parses a string.
            case ("System.Convert", "ToDateTime") when sig.ParameterTypes is [{ IsString: true }]
                or [{ IsString: true }, { Kind: TypeKind.Class }]:
                if (sig.ParameterTypes.Length == 2) Pop();
                Push(StackKind.Struct, "Dn2CppDateTime",
                    $"dn2cpp_convert_str_to_datetime({Cast(Pop(), "Dn2CppString*")})");
                return true;
            case ("System.Convert", "ToDateTime")
                when sig.ParameterTypes is [var dtSrc] && IsDateTime(dtSrc):
            {
                var v = Pop();
                Push(StackKind.Struct, "Dn2CppDateTime", DTVal(v));
                return true;
            }
            case ("System.Convert", "ToDateTime") when sig.ParameterTypes is [{ IsObject: true }, ..]:
                if (sig.ParameterTypes.Length == 2) Pop();
                Push(StackKind.Struct, "Dn2CppDateTime",
                    $"dn2cpp_convert_obj_to_datetime({Cast(Pop(), "Dn2CppObject*")})");
                return true;

            // ToString: numeric/bool/char format like the primitive's ToString;
            // string is identity. The UNSIGNED 32/64-bit overloads need separate arms,
            // not aliases of the signed ones: routed through dn2cpp_long_to_string,
            // every ulong above long.MaxValue (and uint above int.MaxValue) prints as
            // its negative two's-complement twin. Format unsigned at the operand's own
            // storage width.
            case ("System.Convert", "ToString")
                when sig.ParameterTypes is [{ Primitive: PrimitiveTypeCode.Int32 }]:
                Push(StackKind.Ref, "Dn2CppString*", $"dn2cpp_int_to_string({Cast(Pop(), "int32_t")})");
                return true;
            case ("System.Convert", "ToString")
                when sig.ParameterTypes is [{ Primitive: PrimitiveTypeCode.UInt32 }]:
                Push(StackKind.Ref, "Dn2CppString*",
                    $"dn2cpp_format_uint({Cast(Pop(), "uint32_t")}, 4, nullptr)");
                return true;
            case ("System.Convert", "ToString")
                when sig.ParameterTypes is [{ Primitive: PrimitiveTypeCode.Int64 }]:
                Push(StackKind.Ref, "Dn2CppString*", $"dn2cpp_long_to_string({Cast(Pop(), "int64_t")})");
                return true;
            case ("System.Convert", "ToString")
                when sig.ParameterTypes is [{ Primitive: PrimitiveTypeCode.UInt64 }]:
                Push(StackKind.Ref, "Dn2CppString*",
                    $"dn2cpp_format_uint({Cast(Pop(), "uint64_t")}, 8, nullptr)");
                return true;
            case ("System.Convert", "ToString")
                when sig.ParameterTypes is [{ Primitive: PrimitiveTypeCode.Double or PrimitiveTypeCode.Single }]:
                Push(StackKind.Ref, "Dn2CppString*", $"dn2cpp_double_to_string({Cast(Pop(), "double")})");
                return true;
            case ("System.Convert", "ToString")
                when sig.ParameterTypes is [{ Primitive: PrimitiveTypeCode.Boolean }]:
                Push(StackKind.Ref, "Dn2CppString*", $"dn2cpp_bool_to_string({Cast(Pop(), "int32_t")})");
                return true;
            case ("System.Convert", "ToString")
                when sig.ParameterTypes is [{ Primitive: PrimitiveTypeCode.Char }]:
                Push(StackKind.Ref, "Dn2CppString*", $"dn2cpp_char_to_string((char16_t)({Cast(Pop(), "int32_t")}))");
                return true;
            case ("System.Convert", "ToString") when sig.ParameterTypes is [{ IsString: true }]:
                // string -> itself (identity).
                Push(StackKind.Ref, "Dn2CppString*", Cast(Pop(), "Dn2CppString*"));
                return true;
            // ToString(object[, provider]) — the boxed value's virtual ToString; null
            // yields "" (string.Empty), unlike the (string)null identity above.
            case ("System.Convert", "ToString") when sig.ParameterTypes is [{ IsObject: true }, ..]:
                if (sig.ParameterTypes.Length == 2) Pop();
                Push(StackKind.Ref, "Dn2CppString*",
                    $"dn2cpp_convert_obj_to_string({Cast(Pop(), "Dn2CppObject*")})");
                return true;
            // ToString(value, IFormatProvider) — the invariant-provider overloads beside
            // the value-only forms above. The provider is popped and ignored, reusing the
            // primitive ToString lowering: an int prints culture-invariant decimal text, a
            // float its round-trip form (dn2cpp_float_to_string, not the double path, so
            // 0.1f prints "0.1").
            case ("System.Convert", "ToString")
                when sig.ParameterTypes is [{ Primitive: PrimitiveTypeCode.Int32 }, { Kind: TypeKind.Class }]:
                Pop(); // IFormatProvider (invariant) — dropped
                Push(StackKind.Ref, "Dn2CppString*", $"dn2cpp_int_to_string({Cast(Pop(), "int32_t")})");
                return true;
            case ("System.Convert", "ToString")
                when sig.ParameterTypes is [{ Primitive: PrimitiveTypeCode.UInt32 }, { Kind: TypeKind.Class }]:
                Pop(); // IFormatProvider (invariant) — dropped
                Push(StackKind.Ref, "Dn2CppString*",
                    $"dn2cpp_format_uint({Cast(Pop(), "uint32_t")}, 4, nullptr)");
                return true;
            case ("System.Convert", "ToString")
                when sig.ParameterTypes is [{ Primitive: PrimitiveTypeCode.Int64 }, { Kind: TypeKind.Class }]:
                Pop(); // IFormatProvider (invariant) — dropped
                Push(StackKind.Ref, "Dn2CppString*", $"dn2cpp_long_to_string({Cast(Pop(), "int64_t")})");
                return true;
            case ("System.Convert", "ToString")
                when sig.ParameterTypes is [{ Primitive: PrimitiveTypeCode.UInt64 }, { Kind: TypeKind.Class }]:
                Pop(); // IFormatProvider (invariant) — dropped
                Push(StackKind.Ref, "Dn2CppString*",
                    $"dn2cpp_format_uint({Cast(Pop(), "uint64_t")}, 8, nullptr)");
                return true;
            case ("System.Convert", "ToString")
                when sig.ParameterTypes is [{ Primitive: PrimitiveTypeCode.Single }, { Kind: TypeKind.Class }]:
                Pop(); // IFormatProvider (invariant) — dropped
                Push(StackKind.Ref, "Dn2CppString*", $"dn2cpp_float_to_string({Cast(Pop(), "float")})");
                return true;
            // Radix overloads: ToString(value, toBase) for base 2/8/10/16.
            case ("System.Convert", "ToString")
                when sig.ParameterTypes is [{ Primitive: PrimitiveTypeCode.Int32 or PrimitiveTypeCode.UInt32 }, { Primitive: PrimitiveTypeCode.Int32 }]:
            {
                var toBase = Pop();
                var v = Pop();
                Push(StackKind.Ref, "Dn2CppString*",
                    $"dn2cpp_convert_to_string_base_i32({Cast(v, "int32_t")}, {toBase.Expr})");
                return true;
            }
            case ("System.Convert", "ToString")
                when sig.ParameterTypes is [{ Primitive: PrimitiveTypeCode.Int64 or PrimitiveTypeCode.UInt64 }, { Primitive: PrimitiveTypeCode.Int32 }]:
            {
                var toBase = Pop();
                var v = Pop();
                Push(StackKind.Ref, "Dn2CppString*",
                    $"dn2cpp_convert_to_string_base_i64({Cast(v, "int64_t")}, {toBase.Expr})");
                return true;
            }
            // ToInt32/ToInt64(string, fromBase) for base 2/8/10/16.
            case ("System.Convert", "ToInt32")
                when sig.ParameterTypes is [{ IsString: true }, { Primitive: PrimitiveTypeCode.Int32 }]:
            {
                var fromBase = Pop();
                Push(StackKind.I4, "int32_t",
                    $"dn2cpp_convert_from_base_i32({Cast(Pop(), "Dn2CppString*")}, {fromBase.Expr})");
                return true;
            }
            case ("System.Convert", "ToInt64")
                when sig.ParameterTypes is [{ IsString: true }, { Primitive: PrimitiveTypeCode.Int32 }]:
            {
                var fromBase = Pop();
                Push(StackKind.I8, "int64_t",
                    $"dn2cpp_convert_from_base_i64({Cast(Pop(), "Dn2CppString*")}, {fromBase.Expr})");
                return true;
            }
            // Base64 encode: ToBase64String over byte[] / (byte[], offset, length) /
            // ReadOnlySpan<byte>, each with or without Base64FormattingOptions;
            // ToBase64CharArray into a caller char[]; TryToBase64Chars (false + 0
            // written + untouched buffer when the destination is too short).
            case ("System.Convert", "ToBase64String")
                when sig.ParameterTypes is [{ Kind: TypeKind.SZArray, Element.Primitive: PrimitiveTypeCode.Byte }]:
                Push(StackKind.Ref, "Dn2CppString*",
                    $"dn2cpp_convert_to_base64({Cast(Pop(), "Dn2CppArrayN*")}, 0)");
                return true;
            case ("System.Convert", "ToBase64String")
                when sig.ParameterTypes is [{ Kind: TypeKind.SZArray, Element.Primitive: PrimitiveTypeCode.Byte }, var bOpt]
                    && IsBase64Options(bOpt):
            {
                var opts = Pop();
                Push(StackKind.Ref, "Dn2CppString*",
                    $"dn2cpp_convert_to_base64({Cast(Pop(), "Dn2CppArrayN*")}, (int32_t)({opts.Expr}))");
                return true;
            }
            case ("System.Convert", "ToBase64String")
                when sig.ParameterTypes is [{ Kind: TypeKind.SZArray, Element.Primitive: PrimitiveTypeCode.Byte },
                    { Primitive: PrimitiveTypeCode.Int32 }, { Primitive: PrimitiveTypeCode.Int32 }, ..]
                    && (sig.ParameterTypes.Length == 3 || IsBase64Options(sig.ParameterTypes[3])):
            {
                string opts = sig.ParameterTypes.Length == 4 ? $"(int32_t)({Pop().Expr})" : "0";
                var count = Pop();
                var offset = Pop();
                var arr = Pop();
                Push(StackKind.Ref, "Dn2CppString*",
                    $"dn2cpp_convert_to_base64_offset({Cast(arr, "Dn2CppArrayN*")}, " +
                    $"{Cast(offset, "int32_t")}, {Cast(count, "int32_t")}, {opts})");
                return true;
            }
            case ("System.Convert", "ToBase64String")
                when sig.ParameterTypes is [var bSpan, ..] && IsByteSpan(bSpan)
                    && (sig.ParameterTypes.Length == 1
                        || (sig.ParameterTypes.Length == 2 && IsBase64Options(sig.ParameterTypes[1]))):
            {
                string opts = sig.ParameterTypes.Length == 2 ? $"(int32_t)({Pop().Expr})" : "0";
                string sv = SpanValue(Pop(), CppTypes.Of(sig.ParameterTypes[0]));
                Push(StackKind.Ref, "Dn2CppString*",
                    $"dn2cpp_convert_to_base64_raw((const uint8_t*){sv}.f__reference, {sv}.f__length, {opts})");
                return true;
            }
            case ("System.Convert", "ToBase64CharArray")
                when sig.ParameterTypes is [{ Kind: TypeKind.SZArray, Element.Primitive: PrimitiveTypeCode.Byte },
                    { Primitive: PrimitiveTypeCode.Int32 }, { Primitive: PrimitiveTypeCode.Int32 },
                    { Kind: TypeKind.SZArray, Element.Primitive: PrimitiveTypeCode.Char },
                    { Primitive: PrimitiveTypeCode.Int32 }, ..]
                    && (sig.ParameterTypes.Length == 5 || IsBase64Options(sig.ParameterTypes[5])):
            {
                string opts = sig.ParameterTypes.Length == 6 ? $"(int32_t)({Pop().Expr})" : "0";
                var offsetOut = Pop();
                var outArr = Pop();
                var length = Pop();
                var offsetIn = Pop();
                var inArr = Pop();
                Push(StackKind.I4, "int32_t",
                    $"dn2cpp_convert_to_base64_chararray({Cast(inArr, "Dn2CppArrayN*")}, " +
                    $"{Cast(offsetIn, "int32_t")}, {Cast(length, "int32_t")}, " +
                    $"{Cast(outArr, "Dn2CppArrayN*")}, {Cast(offsetOut, "int32_t")}, {opts})");
                return true;
            }
            case ("System.Convert", "TryToBase64Chars")
                when sig.ParameterTypes is [var tb0, var tc1, { Kind: TypeKind.ByRef }, var tOpt]
                    && IsByteSpan(tb0) && IsSpanOf(tc1, PrimitiveTypeCode.Char) && IsBase64Options(tOpt):
            {
                var opts = Pop();
                var outRef = Pop();
                string sc = SpanValue(Pop(), CppTypes.Of(sig.ParameterTypes[1]));
                string sb = SpanValue(Pop(), CppTypes.Of(sig.ParameterTypes[0]));
                string ok = NewTemp("int32_t");
                Emit($"{ok} = dn2cpp_convert_try_to_base64((const uint8_t*){sb}.f__reference, {sb}.f__length, " +
                     $"(char16_t*){sc}.f__reference, {sc}.f__length, (int32_t)({opts.Expr}), {Cast(outRef, "int32_t*")});");
                Push(StackKind.I4, "int32_t", ok);
                return true;
            }
            // Base64 decode: FromBase64String / FromBase64CharArray throw a
            // FormatException on invalid input; the Try* forms return false with
            // 0 written instead (whitespace-tolerant like FromBase64String).
            case ("System.Convert", "FromBase64String")
                when sig.ParameterTypes is [{ IsString: true }]:
                Push(StackKind.Ref, "Dn2CppArrayN*",
                    $"dn2cpp_convert_from_base64({Cast(Pop(), "Dn2CppString*")}, {ByteArrayTypeInfoExpr()})");
                return true;
            case ("System.Convert", "FromBase64CharArray")
                when sig.ParameterTypes is [{ Kind: TypeKind.SZArray, Element.Primitive: PrimitiveTypeCode.Char },
                    { Primitive: PrimitiveTypeCode.Int32 }, { Primitive: PrimitiveTypeCode.Int32 }]:
            {
                var length = Pop();
                var offset = Pop();
                var arr = Pop();
                Push(StackKind.Ref, "Dn2CppArrayN*",
                    $"dn2cpp_convert_from_base64_chararray({Cast(arr, "Dn2CppArrayN*")}, " +
                    $"{Cast(offset, "int32_t")}, {Cast(length, "int32_t")}, {ByteArrayTypeInfoExpr()})");
                return true;
            }
            case ("System.Convert", "TryFromBase64Chars")
                when sig.ParameterTypes is [var fc0, var fb1, { Kind: TypeKind.ByRef }]
                    && IsSpanOf(fc0, PrimitiveTypeCode.Char) && IsByteSpan(fb1):
            {
                var outRef = Pop();
                string sb = SpanValue(Pop(), CppTypes.Of(sig.ParameterTypes[1]));
                string sc = SpanValue(Pop(), CppTypes.Of(sig.ParameterTypes[0]));
                string ok = NewTemp("int32_t");
                Emit($"{ok} = dn2cpp_convert_try_from_base64((const char16_t*){sc}.f__reference, {sc}.f__length, " +
                     $"(uint8_t*){sb}.f__reference, {sb}.f__length, {Cast(outRef, "int32_t*")});");
                Push(StackKind.I4, "int32_t", ok);
                return true;
            }
            case ("System.Convert", "TryFromBase64String")
                when sig.ParameterTypes is [{ IsString: true }, var sb1, { Kind: TypeKind.ByRef }]
                    && IsByteSpan(sb1):
            {
                var outRef = Pop();
                string sb = SpanValue(Pop(), CppTypes.Of(sig.ParameterTypes[1]));
                var str = Pop();
                string ok = NewTemp("int32_t");
                Emit($"{ok} = dn2cpp_convert_try_from_base64_str({Cast(str, "Dn2CppString*")}, " +
                     $"(uint8_t*){sb}.f__reference, {sb}.f__length, {Cast(outRef, "int32_t*")});");
                Push(StackKind.I4, "int32_t", ok);
                return true;
            }
            // Hex: ToHexString/ToHexStringLower(byte[] | byte[],int,int |
            // ReadOnlySpan<byte>) and FromHexString(string).
            case ("System.Convert", "ToHexString" or "ToHexStringLower")
                when sig.ParameterTypes is [{ Kind: TypeKind.SZArray, Element.Primitive: PrimitiveTypeCode.Byte }]:
                Push(StackKind.Ref, "Dn2CppString*",
                    $"dn2cpp_convert_to_hex({Cast(Pop(), "Dn2CppArrayN*")}, {(name == "ToHexStringLower" ? "true" : "false")})");
                return true;
            case ("System.Convert", "ToHexString" or "ToHexStringLower")
                when sig.ParameterTypes is [{ Kind: TypeKind.SZArray, Element.Primitive: PrimitiveTypeCode.Byte },
                    { Primitive: PrimitiveTypeCode.Int32 }, { Primitive: PrimitiveTypeCode.Int32 }]:
            {
                var count = Pop();
                var offset = Pop();
                var arr = Pop();
                Push(StackKind.Ref, "Dn2CppString*",
                    $"dn2cpp_convert_to_hex_offset({Cast(arr, "Dn2CppArrayN*")}, {Cast(offset, "int32_t")}, " +
                    $"{Cast(count, "int32_t")}, {(name == "ToHexStringLower" ? "true" : "false")})");
                return true;
            }
            case ("System.Convert", "ToHexString" or "ToHexStringLower")
                when sig.ParameterTypes is [{ Kind: TypeKind.Class, Class: { } hexSpan }]
                    && Comp.GenericDefFullName(hexSpan) is "System.ReadOnlySpan" or "System.Span"
                    && hexSpan.Context.TypeArgs is [{ Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Byte }]:
            {
                string sv = SpanValue(Pop(), CppTypes.Of(sig.ParameterTypes[0]));
                Push(StackKind.Ref, "Dn2CppString*",
                    $"dn2cpp_convert_to_hex_raw((const uint8_t*){sv}.f__reference, {sv}.f__length, {(name == "ToHexStringLower" ? "true" : "false")})");
                return true;
            }
            case ("System.Convert", "FromHexString")
                when sig.ParameterTypes is [{ IsString: true }]:
                Push(StackKind.Ref, "Dn2CppArrayN*",
                    $"dn2cpp_convert_from_hex({Cast(Pop(), "Dn2CppString*")}, {ByteArrayTypeInfoExpr()})");
                return true;
            // The TryDecode-style FromHexString(ReadOnlySpan<char> chars, Span<byte> bytes,
            // out int charsConsumed, out int bytesWritten) -> OperationStatus: decodes as
            // many whole hex pairs as fit the destination, reports progress through the two
            // out-refs, and returns the OperationStatus (Done=0 / DestinationTooSmall=1 /
            // NeedMoreData=2 / InvalidData=3) rather than throwing.
            case ("System.Convert", "FromHexString")
                when sig.ParameterTypes is [var hexC, var hexB, { Kind: TypeKind.ByRef }, { Kind: TypeKind.ByRef }]
                    && IsSpanOf(hexC, PrimitiveTypeCode.Char) && IsByteSpan(hexB):
            {
                var bytesWritten = Pop();  // out int bytesWritten
                var charsConsumed = Pop(); // out int charsConsumed
                string sb = SpanValue(Pop(), CppTypes.Of(sig.ParameterTypes[1])); // Span<byte>
                string sc = SpanValue(Pop(), CppTypes.Of(sig.ParameterTypes[0])); // ReadOnlySpan<char>
                string st = NewTemp("int32_t");
                Emit($"{st} = dn2cpp_convert_from_hex_span((const char16_t*){sc}.f__reference, {sc}.f__length, " +
                     $"(uint8_t*){sb}.f__reference, {sb}.f__length, " +
                     $"{Cast(charsConsumed, "int32_t*")}, {Cast(bytesWritten, "int32_t*")});");
                Push(StackKind.I4, "int32_t", st);
                return true;
            }

            default:
                return false;
        }
    }

    /// <summary>The sub-word / unsigned Convert.To* family (ToByte/ToSByte/ToInt16/
    /// ToUInt16/ToUInt32/ToUInt64) over its string / floating / decimal / bool /
    /// integer / char / boxed-object sources. Floating and decimal sources round
    /// ties-to-even before the range check (Convert.ToByte(254.5) == 254 but 255.5
    /// rounds to the even 256 and overflows); every out-of-range source raises a
    /// catchable OverflowException; a null string yields 0, unlike Parse. The
    /// ToUInt64 boxed-object form stays in the main switch (a boxed UInt64 needs
    /// the raw-bit-pattern read, not a signed range check).</summary>
    private bool TryEmitConvertNarrowIntrinsic(string name, MethodSignature<TypeDesc> sig)
    {
        (string Store, long Lo, ulong Hi, int Bits)? map = name switch
        {
            "ToByte" => ("uint8_t", 0L, 255UL, 8),
            "ToSByte" => ("int8_t", -128L, 127UL, 8),
            "ToInt16" => ("int16_t", -32768L, 32767UL, 16),
            "ToUInt16" => ("uint16_t", 0L, 65535UL, 16),
            "ToUInt32" => ("uint32_t", 0L, 4294967295UL, 32),
            "ToUInt64" => ("uint64_t", 0L, ulong.MaxValue, 64),
            _ => null,
        };
        if (map is not { } w)
            return false;
        bool u64 = name == "ToUInt64";
        var ps = sig.ParameterTypes;
        string bounds = $"{w.Lo}LL, {(long)Math.Min(w.Hi, long.MaxValue)}LL";

        // The checked expression is int64-domain (uint64 for ToUInt64); push it
        // at the target's storage width on the natural stack slot.
        void PushNarrow(string expr)
        {
            if (u64)
                Push(StackKind.I8, "int64_t", $"(int64_t)({expr})");
            else
                Push(StackKind.I4, "int32_t", $"(int32_t)({w.Store})({expr})");
        }

        // string [+ provider] — the NumberStyles engine at the target width.
        if (ps is [{ IsString: true }] or [{ IsString: true }, { Kind: TypeKind.Class }])
        {
            if (ps.Length == 2)
                Pop(); // provider (invariant) — dropped
            PushNarrow($"dn2cpp_convert_str_to_int({Cast(Pop(), "Dn2CppString*")}, {w.Bits}, {(w.Lo < 0 ? 1 : 0)})");
            return true;
        }
        // string + fromBase — the ParseNumbers radix model at the target width
        // (non-decimal bases parse the raw bit pattern: "FF" -> byte 255 /
        // sbyte -1; base 10 range-checks signed/unsigned; the helper raises
        // the catchable Argument/Format/Overflow exceptions .NET does).
        if (ps is [{ IsString: true }, { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Int32 }])
        {
            var fromBase = Pop();
            PushNarrow($"dn2cpp_convert_from_base_any({Cast(Pop(), "Dn2CppString*")}, "
                + $"{fromBase.Expr}, {w.Bits}, {(w.Lo < 0 ? 0 : 1)})");
            return true;
        }
        // double/float — banker's round then range-check.
        if (ps is [{ Primitive: PrimitiveTypeCode.Double or PrimitiveTypeCode.Single }])
        {
            PushNarrow(u64
                ? $"dn2cpp_convert_r8_to_u64({Cast(Pop(), "double")})"
                : $"dn2cpp_convert_r8_checked({Cast(Pop(), "double")}, {bounds})");
            return true;
        }
        // decimal — banker's round at 0 digits then range-check.
        if (ps is [var dec] && IsDecimal(dec))
        {
            var v = Pop();
            PushNarrow(u64
                ? $"dn2cpp_convert_dec_to_u64({DecVal(v)})"
                : $"dn2cpp_convert_dec_checked({DecVal(v)}, {bounds})");
            return true;
        }
        // bool -> 0/1.
        if (ps is [{ Primitive: PrimitiveTypeCode.Boolean }])
        {
            PushNarrow($"(({Pop().Expr}) != 0 ? 1 : 0)");
            return true;
        }
        // Integer / char sources, incl. the same-type identity. The source reads
        // at its own width/signedness first, so a uint32 0xFFFFFFFF widens to
        // 4294967295 (not -1) before the target range check.
        if (ps is [{ Kind: TypeKind.Primitive, Primitive: var prim }]
            && prim switch
            {
                PrimitiveTypeCode.SByte or PrimitiveTypeCode.Int16 or PrimitiveTypeCode.Int32 => "int32_t",
                PrimitiveTypeCode.Byte => "uint8_t",
                PrimitiveTypeCode.UInt16 or PrimitiveTypeCode.Char => "uint16_t",
                PrimitiveTypeCode.UInt32 => "uint32_t",
                PrimitiveTypeCode.Int64 => "int64_t",
                PrimitiveTypeCode.UInt64 => "uint64_t",
                _ => null,
            } is { } srcCt)
        {
            string src = $"({srcCt})({Pop().Expr})";
            if (prim == PrimitiveTypeCode.UInt64)
                PushNarrow(u64 ? $"(uint64_t){src}" : $"dn2cpp_convert_u64_checked({src}, {w.Hi}ULL)");
            else
                PushNarrow(u64
                    ? $"dn2cpp_convert_i64_to_u64((int64_t){src})"
                    : $"dn2cpp_convert_i64_checked((int64_t){src}, {bounds})");
            return true;
        }
        // object [+ provider] — boxed IConvertible source, range-checked
        // (Convert.ToUInt16((object)(-1)) overflows like .NET).
        if (!u64 && ps is [{ IsObject: true }] or [{ IsObject: true }, { Kind: TypeKind.Class }])
        {
            if (ps.Length == 2)
                Pop();
            PushNarrow($"dn2cpp_convert_i64_checked(dn2cpp_convert_obj_to_i64({Cast(Pop(), "Dn2CppObject*")}), {bounds})");
            return true;
        }
        return false;
    }

    /// <summary>System.Base64FormattingOptions (an enum; None = 0,
    /// InsertLineBreaks = 1) as a parameter type.</summary>
    private static bool IsBase64Options(TypeDesc t) =>
        (t.Kind == TypeKind.Class && t.Class?.FullName == "System.Base64FormattingOptions")
        || (t.Kind == TypeKind.External && t.ExternalName == "System.Base64FormattingOptions");

    /// <summary>A (closed) ReadOnlySpan&lt;elem&gt; or Span&lt;elem&gt; parameter.</summary>
    private bool IsSpanOf(TypeDesc t, PrimitiveTypeCode elem) =>
        t is { Kind: TypeKind.Class, Class: { } c }
        && Comp.GenericDefFullName(c) is "System.ReadOnlySpan" or "System.Span"
        && c.Context.TypeArgs is [{ Kind: TypeKind.Primitive, Primitive: var p }] && p == elem;

    private bool IsByteSpan(TypeDesc t) => IsSpanOf(t, PrimitiveTypeCode.Byte);
}

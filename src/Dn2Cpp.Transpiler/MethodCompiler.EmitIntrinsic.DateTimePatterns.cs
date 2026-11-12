using System.Reflection.Metadata;

namespace Dn2Cpp;

internal sealed partial class MethodCompiler
{
    private bool TryEmitDateTimeFormatInfoBoundary(MethodInfo callee)
    {
        if (CoreIntrinsics.IsDateTimeFormatInfoDayArray(callee))
        {
            DateTimeFormatReceiver();
            var element = callee.Signature.ReturnType.Element!;
            Comp.NoteArrayElementType(element);
            int width = callee.Name switch
            {
                "InternalGetAbbreviatedDayOfWeekNames" => 1,
                "InternalGetSuperShortDayNames" => 2,
                _ => 0
            };
            Push(StackKind.Ref, "Dn2CppArrayRef*",
                $"dn2cpp_dtfi_invariant_day_names({PreciseArrayTypeInfoExpr(element)}, {width})");
            Top = Top with { StaticType = callee.Signature.ReturnType };
            return true;
        }
        if (CoreIntrinsics.IsDateTimeFormatInfoMonthArray(callee))
        {
            string abbreviated = callee.Name == "InternalGetGenitiveMonthNames"
                ? Cast(Pop(), "int32_t")
                : callee.Name == "InternalGetAbbreviatedMonthNames" ? "1" : "0";
            DateTimeFormatReceiver();
            var element = callee.Signature.ReturnType.Element!;
            Comp.NoteArrayElementType(element);
            Push(StackKind.Ref, "Dn2CppArrayRef*",
                $"dn2cpp_dtfi_invariant_month_names({PreciseArrayTypeInfoExpr(element)}, {abbreviated} != 0)");
            Top = Top with { StaticType = callee.Signature.ReturnType };
            return true;
        }
        if (CoreIntrinsics.IsDateTimeFormatInfoUnclonedPatternGetter(callee))
        {
            DateTimeFormatReceiver();
            var element = callee.Signature.ReturnType.Element!;
            Comp.NoteArrayElementType(element);
            int kind = callee.Name switch
            {
                "get_UnclonedYearMonthPatterns" => 0,
                "get_UnclonedShortDatePatterns" => 1,
                "get_UnclonedLongDatePatterns" => 2,
                "get_UnclonedShortTimePatterns" => 3,
                _ => 4
            };
            Push(StackKind.Ref, "Dn2CppArrayRef*",
                $"dn2cpp_dtfi_invariant_patterns({PreciseArrayTypeInfoExpr(element)}, {kind})");
            Top = Top with { StaticType = callee.Signature.ReturnType };
            return true;
        }
        if (CoreIntrinsics.IsDateTimeFormatInfoEraName(callee))
        {
            EmitDateTimeFormatEraName(callee);
            return true;
        }
        if (CoreIntrinsics.IsDateTimeFormatInfoFormatFlags(callee)
            || CoreIntrinsics.IsDateTimeFormatInfoHasForceTwoDigitYears(callee))
        {
            DateTimeFormatReceiver();
            // Invariant Gregorian data has no culture-name flags or two-digit-year rule.
            var result = callee.Signature.ReturnType;
            Comp.NoteTypeIdentityClosure(result, keepSeed: false);
            Push(StackKind.I4, "int32_t", "0");
            Top = Top with { StaticType = result };
            return true;
        }
        if (CoreIntrinsics.IsDateTimeFormatInfoGeneralTimePattern(callee))
        {
            DateTimeFormatReceiver();
            string pattern = callee.Name switch
            {
                "get_DateTimeOffsetPattern" => "MM/dd/yyyy HH:mm:ss zzz",
                "get_GeneralLongTimePattern" => "MM/dd/yyyy HH:mm:ss",
                _ => "MM/dd/yyyy HH:mm"
            };
            Push(StackKind.Ref, "Dn2CppString*", Literals.GetOrAdd(pattern));
            Top = Top with { StaticType = callee.Signature.ReturnType };
            return true;
        }
        if (CoreIntrinsics.IsDateTimeFormatInfoSpan(callee))
        {
            if (callee.Context.MethodArgs is not [{ Kind: TypeKind.Primitive,
                    Primitive: PrimitiveTypeCode.Char or PrimitiveTypeCode.Byte } element])
                throw new NotSupportedException("DateTimeFormatInfo TChar accessor requires Char or Byte");
            EmitDateTimeFormatSpan(callee, element);
            return true;
        }
        if (CoreIntrinsics.IsDateTimeFormatInfoTokenize(callee))
        {
            var arguments = new StackEntry[4];
            for (int i = arguments.Length - 1; i >= 0; i--)
                arguments[i] = Pop();
            string receiver = NewTemp(DateTimeFormatCppType());
            Emit($"{receiver} = {Cast(Pop(), DateTimeFormatCppType())};");
            // The absent token engine must not inspect or initialize out/cursor storage.
            foreach (var argument in arguments)
                Emit($"(void)({argument.Expr});");
            if (_callIsVirtual)
                Emit($"dn2cpp_null_check({receiver});");
            Emit("dn2cpp_throw_platform_not_supported(\"DateTimeFormatInfo token-table parsing is not supported\");");
            Push(StackKind.I4, "int32_t", "0");
            return true;
        }
        if (CoreIntrinsics.IsDateTimeFormatInfoCalendarSetter(callee))
        {
            string calendar = Cast(Pop(), "Dn2CppObject*");
            string receiver = DateTimeFormatReceiver();
            Emit($"if ({receiver}->dn2cpp_read_only) dn2cpp_throw_invalid_operation_msg(\"Instance is read-only.\");");
            Emit($"if ({calendar} == nullptr) dn2cpp_throw_argument_null_param(\"value\");");
            Emit("dn2cpp_throw_platform_not_supported(\"DateTimeFormatInfo.Calendar is not supported\");");
            return true;
        }
        if (CoreIntrinsics.IsDateTimeFormatInfoCalendar(callee))
        {
            DateTimeFormatReceiver();
            Emit("dn2cpp_throw_platform_not_supported(\"DateTimeFormatInfo.Calendar is not supported\");");
            var result = callee.Signature.ReturnType;
            Push(StackKind.Ref, CppTypes.Of(result), "nullptr");
            Top = Top with { StaticType = result };
            return true;
        }
        return false;
    }

    private void EmitDateTimeFormatEraName(MethodInfo callee)
    {
        string era = NewTemp("int32_t");
        Emit($"{era} = {Cast(Pop(), "int32_t")};");
        DateTimeFormatReceiver();
        string message = Comp.SrResourceText(callee.Module, "ArgumentOutOfRange_InvalidEraValue")
            ?? throw new NotSupportedException("DateTimeFormatInfo era names require CoreLib's range resource");
        var argumentType = callee.Signature.ParameterTypes[0];
        Comp.NoteTypeIdentityClosure(argumentType, keepSeed: false);
        string typeInfo = TypeInfoExpr(argumentType)
            ?? throw new NotSupportedException("DateTimeFormatInfo era names require argument type metadata");
        Emit($"if ({era} != 0 && {era} != 1) {{");
        string actual = NewTemp("Dn2CppObject*");
        string actualText = NewTemp("Dn2CppString*");
        Emit($"{actual} = dn2cpp_box({typeInfo}, &{era}, sizeof({era}));");
        Emit($"{actualText} = dn2cpp_format_int({era}, 4, nullptr);");
        Emit($"DN2CPP_WEB_GC_LIVENESS({actual});");
        Emit($"dn2cpp_throw_argument_out_of_range_actual(\"{CppLiteralBody(message)}\", \"era\", {actual}, {actualText});");
        Emit("}");
        Push(StackKind.Ref, "Dn2CppString*", Literals.GetOrAdd(callee.Name == "GetAbbreviatedEraName" ? "AD" : "A.D."));
        Top = Top with { StaticType = callee.Signature.ReturnType };
    }

    private void EmitDateTimeFormatSpan(MethodInfo target, TypeDesc element)
    {
        string receiver = DateTimeFormatReceiver();
        string text = NewTemp("Dn2CppString*");
        string literal = Literals.GetOrAdd(target.Name switch
        {
            "AMDesignatorTChar" => "AM",
            "PMDesignatorTChar" => "PM",
            "TimeSeparatorTChar" => ":",
            "DecimalSeparatorTChar" => ".",
            _ => "/"
        });
        Emit(target.Name == "DateSeparatorTChar"
            ? $"{text} = {receiver}->dn2cpp_date_separator != nullptr ? {receiver}->dn2cpp_date_separator : {literal};"
            : $"{text} = {literal};");
        var resultType = target.Signature.ReturnType;
        NoteLocalValueLayout(resultType);
        string result = NewTemp(CppTypes.Of(resultType));
        if (element.Primitive == PrimitiveTypeCode.Byte)
        {
            // A GC-owned base pointer follows the existing generic-span convention.
            string count = NewTemp("int32_t");
            string bytes = NewTemp("uint8_t*");
            Emit($"{count} = dn2cpp_string_to_utf8({text}, nullptr, 0);");
            Emit($"{bytes} = (uint8_t*)dn2cpp_alloc_atomic((size_t){count} + 1);");
            Emit($"if ({count} > 0) dn2cpp_string_to_utf8({text}, (char*){bytes}, {count});");
            Emit($"{bytes}[{count}] = 0;");
            Emit($"{result}.f__reference = {bytes};");
            Emit($"{result}.f__length = {count};");
        }
        else
        {
            Emit($"{result}.f__reference = (char16_t*){text}->chars;");
            Emit($"{result}.f__length = {text}->length;");
        }
        Push(StackKind.Struct, CppTypes.Of(resultType), result);
        Top = Top with { StaticType = resultType };
    }
}

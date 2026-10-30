using System.Reflection.Metadata;

namespace Dn2Cpp;

internal sealed partial class MethodCompiler
{
    private void EmitAllDateTimePatterns(MethodInfo callee)
    {
        string format = NewTemp("uint16_t");
        Emit($"{format} = (uint16_t)({Pop().Expr});");
        string receiver = NewTemp(DateTimeFormatCppType());
        Emit($"{receiver} = {Cast(Pop(), DateTimeFormatCppType())};");
        // Intrinsic call sites bypass the ordinary managed-call null guard.
        // A synthesized addressable body has no callvirt opcode of its own.
        if (_callIsVirtual)
            Emit($"dn2cpp_null_check({receiver});");

        string[] shortDates = ["MM/dd/yyyy", "yyyy-MM-dd"];
        string[] longDates = ["dddd, dd MMMM yyyy"];
        string[] shortTimes = ["HH:mm", "hh:mm tt", "H:mm", "h:mm tt"];
        string[] longTimes = ["HH:mm:ss"];
        static string[] Combine(string[] dates, string[] times) =>
            dates.SelectMany(date => times.Select(time => date + " " + time)).ToArray();
        (string Codes, bool ReadsReceiver, string[] Patterns)[] groups =
        [
            ("d", true, shortDates), ("D", true, longDates),
            ("t", true, shortTimes), ("T", true, longTimes),
            ("yY", true, ["yyyy MMMM"]), ("mM", true, ["MMMM dd"]),
            ("f", true, Combine(longDates, shortTimes)),
            ("FU", true, Combine(longDates, longTimes)),
            ("g", true, Combine(shortDates, shortTimes)),
            ("G", true, Combine(shortDates, longTimes)),
            ("oO", false, ["yyyy'-'MM'-'dd'T'HH':'mm':'ss.fffffffK"]),
            ("rR", false, ["ddd, dd MMM yyyy HH':'mm':'ss 'GMT'"]),
            ("s", false, ["yyyy'-'MM'-'dd'T'HH':'mm':'ss"]),
            ("u", false, ["yyyy'-'MM'-'dd HH':'mm':'ss'Z'"])
        ];
        var element = callee.Signature.ReturnType.Element!;
        Comp.NoteArrayElementType(element);
        string arrayType = PreciseArrayTypeInfoExpr(element);
        string result = NewTemp("Dn2CppArrayRef*");
        Emit($"switch ({format}) {{");
        foreach (var group in groups)
        {
            foreach (char code in group.Codes)
                Emit($"case {(int)code}:");
            Emit("{");
            if (group.ReadsReceiver)
                Emit($"dn2cpp_null_check({receiver});");
            Emit($"{result} = dn2cpp_newarr_ref_t({group.Patterns.Length}, {arrayType});");
            for (int i = 0; i < group.Patterns.Length; i++)
                Emit($"dn2cpp_gc_store_ref(&{result}->data[{i}], (Dn2CppObject*){Literals.GetOrAdd(group.Patterns[i])});");
            Emit("break;");
            Emit("}");
        }
        Emit("default: {");
        string character = NewTemp("Dn2CppString*");
        string message = NewTemp("Dn2CppString*");
        Emit($"{character} = dn2cpp_char_to_string((char16_t){format});");
        Emit($"{message} = dn2cpp_string_concat3({Literals.GetOrAdd("Format specifier '")}, {character}, {Literals.GetOrAdd("' was invalid.")});");
        Emit($"dn2cpp_throw_argument_message({message}, \"format\");");
        Emit("}");
        Emit("}");
        Push(StackKind.Ref, "Dn2CppArrayRef*", result);
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
            // Preserve the existing generic span convention: a GC-owned base
            // pointer, never temporary storage or a new owner/cache subsystem.
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


    private void EmitDateOnlyDeconstruct()
    {
        string dayOut = NewTemp("int32_t*");
        string monthOut = NewTemp("int32_t*");
        string yearOut = NewTemp("int32_t*");
        Emit($"{dayOut} = {Cast(Pop(), "int32_t*")};");
        Emit($"{monthOut} = {Cast(Pop(), "int32_t*")};");
        Emit($"{yearOut} = {Cast(Pop(), "int32_t*")};");
        var receiver = Pop();
        if (receiver.Kind == StackKind.Ptr)
            Emit($"dn2cpp_null_check({receiver.Expr});");
        string snapshot = NewTemp("Dn2CppDateOnly");
        Emit($"{snapshot} = {DOnlyVal(receiver)};");
        string year = NewTemp("int32_t");
        string month = NewTemp("int32_t");
        string day = NewTemp("int32_t");
        string rollover = NewTemp("bool");
        Emit($"{year} = dn2cpp_dateonly_year({snapshot});");
        Emit($"{month} = dn2cpp_dateonly_month({snapshot});");
        Emit($"{day} = dn2cpp_dateonly_day({snapshot});");
        Emit($"{rollover} = {month} <= 2;");
        // CoreLib writes March-based intermediates before adjusting the output
        // locations themselves. Aliasing and null-output partial writes observe it.
        Emit($"dn2cpp_null_check({yearOut});");
        Emit($"*{yearOut} = {year} - ({rollover} ? 1 : 0);");
        Emit($"dn2cpp_null_check({monthOut});");
        Emit($"*{monthOut} = {month} + ({rollover} ? 12 : 0);");
        Emit($"dn2cpp_null_check({dayOut});");
        Emit($"*{dayOut} = {day};");
        Emit($"if ({rollover}) {{ ++*{yearOut}; *{monthOut} -= 12; }}");
    }

}

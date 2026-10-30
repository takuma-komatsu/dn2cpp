namespace Dn2Cpp;

internal sealed partial class MethodCompiler
{
    private string EmitDateTimeLocalConstructorValue()
    {
        var ambiguous = Pop();
        var kind = Pop();
        var ticks = Pop();
        // The installed private constructor does not read kind, but the caller
        // still evaluates that argument. Validate ticks before touching this.
        Emit($"(void)({kind.Expr});");
        string value = NewTemp("Dn2CppDateTime");
        Emit($"{value} = dn2cpp_datetime_local_from_ticks((int64_t)({ticks.Expr}), (int32_t)({ambiguous.Expr}));");
        return value;
    }

    private void EmitDateTimeParserBoundary(MethodInfo callee)
    {
        if (CoreIntrinsics.IsDateTimeLocalConstructor(callee))
        {
            string value = EmitDateTimeLocalConstructorValue();
            var receiver = Pop();
            Emit($"dn2cpp_null_check({receiver.Expr});");
            Emit($"*({Cast(receiver, "Dn2CppDateTime*")}) = {value};");
            return;
        }
        if (CoreIntrinsics.IsDateTimeTryAddTicks(callee))
        {
            var result = Pop();
            var ticks = Pop();
            var receiver = Pop();
            if (receiver.Kind == StackKind.Ptr)
                Emit($"dn2cpp_null_check({receiver.Expr});");
            string snapshot = NewTemp("Dn2CppDateTime");
            Emit($"{snapshot} = {DTVal(receiver)};");
            Push(StackKind.I4, "int32_t", $"dn2cpp_datetime_try_add_ticks({snapshot}, (int64_t)({ticks.Expr}), {Cast(result, "Dn2CppDateTime*")})");
            return;
        }
        var args = new StackEntry[8];
        for (int i = args.Length - 1; i >= 0; i--) args[i] = Pop();
        string fields = string.Join(", ", args.Take(7).Select(a => $"(int32_t)({a.Expr})"));
        Push(StackKind.I4, "int32_t", $"dn2cpp_datetime_try_create({fields}, {Cast(args[7], "Dn2CppDateTime*")})");
    }
}

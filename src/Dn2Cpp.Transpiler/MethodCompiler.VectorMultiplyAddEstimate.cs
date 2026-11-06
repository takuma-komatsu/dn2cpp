namespace Dn2Cpp;

internal sealed partial class MethodCompiler
{
    private void EmitVectorFloatingPointTernary(MethodInfo method)
    {
        if (!CoreIntrinsics.IsVectorFloatingPointTernary(method))
            throw new NotSupportedException($"Vector {method.Name} requires its exact floating-point CoreLib declaration");
        var type = method.Signature.ReturnType;
        string cpp = CppTypes.StorageOf(type);
        string element = CppTypes.StorageOf(type.Class!.Context.TypeArgs[0]);
        int width = VecWidthBytes(cpp);
        var addend = Pop();
        var right = Pop();
        var left = Pop();
        string Value(StackEntry entry) => entry.Kind == StackKind.Ptr ? $"(*({cpp}*)({entry.Expr}))" : Cast(entry, cpp);
        string factor = Value(right), finalAddend = Value(addend);
        if (method.Name == "Lerp")
        {
            // Stack producers are already spilled. Copy amount once before its two uses.
            string amount = NewTemp(cpp);
            Emit($"{amount} = {finalAddend};");
            factor = NewTemp(cpp);
            Emit($"{factor} = dn2cpp_vec_subtract<{element}, {width}>(dn2cpp_vec_create_broadcast<{element}, {width}>(({element})1), {amount});");
            finalAddend = NewTemp(cpp);
            Emit($"{finalAddend} = dn2cpp_vec_multiply<{element}, {width}>({Value(right)}, {amount});");
            // Inlining must not contract the addend multiplication into the final addition.
            string lane = NewTemp("int32_t"), rounded = NewTemp("volatile " + element);
            Emit($"for ({lane} = 0; {lane} < {width} / sizeof({element}); ++{lane}) {{");
            Emit($"    {rounded} = dn2cpp_vec_get<{element}>({finalAddend}, {lane});");
            Emit($"    dn2cpp_vec_set<{element}>({finalAddend}, {lane}, {rounded});");
            Emit("}");
        }
        Push(StackKind.Struct, cpp, $"dn2cpp_vec_multiply_add_estimate<{element}, {width}>({Value(left)}, {factor}, {finalAddend})");
        Top = Top with { StaticType = type };
    }
}

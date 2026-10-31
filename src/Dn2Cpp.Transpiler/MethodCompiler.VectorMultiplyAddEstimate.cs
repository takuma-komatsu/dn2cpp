namespace Dn2Cpp;

internal sealed partial class MethodCompiler
{
    private void EmitVectorMultiplyAddEstimate(MethodInfo method)
    {
        if (!CoreIntrinsics.IsVectorMultiplyAddEstimate(method))
            throw new NotSupportedException("Vector MultiplyAddEstimate requires its exact floating-point CoreLib declaration");
        var type = method.Signature.ReturnType;
        string cpp = CppTypes.StorageOf(type);
        string element = CppTypes.StorageOf(type.Class!.Context.TypeArgs[0]);
        int width = VecWidthBytes(cpp);
        var addend = Pop();
        var right = Pop();
        var left = Pop();
        string Value(StackEntry entry) => entry.Kind == StackKind.Ptr ? $"(*({cpp}*)({entry.Expr}))" : Cast(entry, cpp);
        Push(StackKind.Struct, cpp, $"dn2cpp_vec_multiply_add_estimate<{element}, {width}>({Value(left)}, {Value(right)}, {Value(addend)})");
        Top = Top with { StaticType = type };
    }
}

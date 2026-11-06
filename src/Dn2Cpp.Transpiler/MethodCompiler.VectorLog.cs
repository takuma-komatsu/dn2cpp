namespace Dn2Cpp;

internal sealed partial class MethodCompiler
{
    private void EmitVectorLog(MethodInfo method)
    {
        if (!CoreIntrinsics.IsVectorLog(method))
            throw new NotSupportedException($"Vector {method.Name} requires its exact floating-point CoreLib declaration");
        var type = method.Signature.ReturnType;
        string cpp = CppTypes.StorageOf(type);
        string element = CppTypes.StorageOf(type.Class!.Context.TypeArgs[0]);
        int width = VecWidthBytes(cpp);
        var operand = Pop();
        string source = NewTemp(cpp), result = NewTemp(cpp), lane = NewTemp("int32_t");
        string value = operand.Kind == StackKind.Ptr ? $"(*({cpp}*)({operand.Expr}))" : Cast(operand, cpp);
        Emit($"{source} = {value};");
        string operation = method.Name == "Log" ? "log" : "log2";
        // Preserve the scalar fallback's element precision and target libm semantics.
        Emit($"for ({lane} = 0; {lane} < {width} / sizeof({element}); ++{lane}) {{");
        Emit($"    dn2cpp_vec_set<{element}>({result}, {lane}, std::{operation}(dn2cpp_vec_get<{element}>({source}, {lane})));");
        Emit("}");
        Push(StackKind.Struct, cpp, result);
        Top = Top with { StaticType = type };
    }
}

namespace Dn2Cpp;

internal sealed partial class MethodCompiler
{
    private void EmitVectorMagnitude(MethodInfo method)
    {
        if (!CoreIntrinsics.IsVectorMagnitude(method))
            throw new NotSupportedException($"Vector {method.Name} requires its exact floating-point CoreLib declaration");
        var type = method.Signature.ReturnType;
        string cpp = CppTypes.StorageOf(type);
        string element = CppTypes.StorageOf(type.Class!.Context.TypeArgs[0]);
        int width = VecWidthBytes(cpp);
        var rightOperand = Pop();
        var operand = Pop();
        string source = NewTemp(cpp), right = NewTemp(cpp), result = NewTemp(cpp), lane = NewTemp("int32_t");
        string value = operand.Kind == StackKind.Ptr ? $"(*({cpp}*)({operand.Expr}))" : Cast(operand, cpp);
        Emit($"{source} = {value};");
        string rightValue = rightOperand.Kind == StackKind.Ptr ? $"(*({cpp}*)({rightOperand.Expr}))" : Cast(rightOperand, cpp);
        Emit($"{right} = {rightValue};");
        string operation = method.Name switch
        {
            "MaxMagnitude" => "maxmag", "MinMagnitude" => "minmag",
            "MaxMagnitudeNumber" => "maxmagnum", "MinMagnitudeNumber" => "minmagnum",
            "MaxNumber" => "maxnum", "MinNumber" => "minnum",
            _ => throw new InvalidOperationException("Unexpected vector selection operation"),
        };
        // Return the selected operand, preserving floating-point ties and NaN rules.
        Emit($"for ({lane} = 0; {lane} < {width} / sizeof({element}); ++{lane}) {{");
        Emit($"    dn2cpp_vec_set<{element}>({result}, {lane}, dn2cpp_math_{operation}<{element}>(dn2cpp_vec_get<{element}>({source}, {lane}), dn2cpp_vec_get<{element}>({right}, {lane})));");
        Emit("}");
        Push(StackKind.Struct, cpp, result);
        Top = Top with { StaticType = type };
    }
}

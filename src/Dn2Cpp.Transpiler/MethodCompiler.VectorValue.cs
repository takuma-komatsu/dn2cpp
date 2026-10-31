namespace Dn2Cpp;

internal sealed partial class MethodCompiler
{
    private void EmitVectorValue(MethodInfo callee)
    {
        if (!CoreIntrinsics.IsVectorValueMethod(callee))
            throw new NotSupportedException("Vector<T> value methods require an exact supported declaration");
        EmitVectorValue(callee.DeclaringClass, callee.Name,
            callee.Signature.ParameterTypes.Length == 1 && callee.Signature.ParameterTypes[0].IsObject);
    }

    private void EmitVectorValue(ClassInfo owner, string name, bool objectArgument)
    {
        int width = CoreIntrinsics.VectorValueWidth(owner);
        string cpp = owner.IntrinsicCppName!;
        var argument = name == "Equals" ? Pop() : default;
        var receiver = Pop();
        string value;
        if (receiver.Kind == StackKind.Ptr)
        {
            // An instance method-group delegate can supply a boxed payload, whose
            // address need not have the vector representation's native alignment.
            value = NewTemp(cpp);
            Emit($"std::memcpy(&{value}, {receiver.Expr}, sizeof({value}));");
        }
        else
            value = Cast(receiver, cpp);
        string element = CppTypes.StorageOf(owner.Context.TypeArgs[0]);
        string expression;
        if (name == "GetHashCode")
            expression = $"dn2cpp_vec_value_hash<{element}, {width}>({value})";
        else if (objectArgument)
        {
            string type = TypeInfoExpr(TypeDesc.MakeClass(owner))
                ?? throw new NotSupportedException("Vector<T> object equality requires exact type identity");
            expression = $"dn2cpp_vec_value_object_equals<{element}, {width}>({value}, {Cast(argument!, "Dn2CppObject*")}, {type})";
        }
        else
            expression = $"dn2cpp_vec_value_equals<{element}, {width}>({value}, {Cast(argument!, cpp)})";
        Push(StackKind.I4, "int32_t", expression);
    }
}

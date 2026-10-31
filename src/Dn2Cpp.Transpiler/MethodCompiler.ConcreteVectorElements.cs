namespace Dn2Cpp;

internal sealed partial class MethodCompiler
{
    private void EmitConcreteVectorElement(MethodInfo method)
    {
        if (!CoreIntrinsics.IsConcreteVectorElement(method))
            throw new NotSupportedException("Concrete vector element access requires its exact CoreLib declaration");
        var type = method.Signature.ParameterTypes[0];
        var vector = type.Class!;
        Comp.EnsureCompleted(vector);
        NoteLocalValueLayout(type);
        int count = CoreIntrinsics.ConcreteVectorElementCount(vector, method.Module);
        bool with = method.Name == "WithElement";
        var replacement = with ? Pop() : default;
        var index = Pop();
        var source = Pop();
        string cpp = CppTypes.StorageOf(type);
        var copy = new StackEntry(NewTemp(cpp), StackKind.Struct, cpp, StaticType: type);
        Emit($"{copy.Expr} = {(source.Kind == StackKind.Ptr ? $"(*({cpp}*)({source.Expr}))" : Cast(source, cpp))};");
        string lane = NewTemp("int32_t");
        Emit($"{lane} = {Cast(index, "int32_t")};");
        string scalar = NewTemp("float");
        if (with) Emit($"{scalar} = {Cast(replacement!, "float")};");
        Emit($"if ((uint32_t){lane} >= {count}u) dn2cpp_throw_argument_out_of_range_param(DN2CPP_SR_ARGUMENT_OUT_OF_RANGE, \"index\");");
        // Named fields avoid depending on padding, alignment or a lane-array alias.
        Emit($"switch ({lane}) {{");
        for (int i = 0; i < count; i++)
        {
            string name = i == 0 ? "X" : i == 1 ? "Y" : i == 2 ? "Z" : "W";
            var field = vector.Fields.Single(f => !f.IsStatic && f.Name == name);
            string access = FieldAccess(vector, field, copy);
            Emit(with ? $"case {i}: std::memcpy(&{access}, &{scalar}, sizeof(float)); break;"
                : $"case {i}: std::memcpy(&{scalar}, &{access}, sizeof(float)); break;");
        }
        Emit("}");
        if (with) PushEntry(copy);
        else Push(StackKind.R8, "float", scalar);
        Top = Top with { StaticType = method.Signature.ReturnType };
    }
}

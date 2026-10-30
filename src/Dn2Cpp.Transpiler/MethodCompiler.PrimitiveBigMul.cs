using System.Reflection.Metadata;

namespace Dn2Cpp;

internal sealed partial class MethodCompiler
{
    private void EmitPrimitiveBigMul(MethodInfo target)
    {
        if (!CoreIntrinsics.IsPrimitiveBigMul(target))
            throw new NotSupportedException("Primitive BigMul requires its exact CoreLib declaration");
        var signature = target.Signature;
        var code = signature.ParameterTypes[0].Primitive;
        if (code is PrimitiveTypeCode.Int32 or PrimitiveTypeCode.UInt32)
        {
            if (!TryMathIntrinsic("System.Math", "BigMul", signature))
                throw new InvalidOperationException("Exact primitive BigMul has no Math lowering");
            return;
        }
        bool native = code is PrimitiveTypeCode.IntPtr or PrimitiveTypeCode.UIntPtr;
        bool signed = code is PrimitiveTypeCode.Int64 or PrimitiveTypeCode.IntPtr;
        string wide = signed ? "int64_t" : "uint64_t";
        string storage = native ? (signed ? "intptr_t" : "uintptr_t") : wide;
        var output = native ? Pop() : default;
        var right = Pop();
        var left = Pop();
        string a = NewTemp(storage), b = NewTemp(storage);
        Emit($"{a} = ({storage})({left.Expr});");
        Emit($"{b} = ({storage})({right.Expr});");
        string low = NewTemp(wide), high = NewTemp(wide);
        string helper = signed ? "dn2cpp_math_bigmul_i64" : "dn2cpp_math_bigmul_u64";
        if (native)
        {
            string narrow = signed ? "int32_t" : "uint32_t";
            Emit("if (sizeof(intptr_t) == 4) {");
            Emit($"    {low} = ({wide})({narrow}){a} * ({wide})({narrow}){b};");
            Emit($"    {high} = ({wide})((uint64_t){low} >> 32);");
            Emit("} else {");
            Emit($"    {high} = {helper}(({wide}){a}, ({wide}){b}, &{low});");
            Emit("}");
            // The managed native ABI uses intptr_t even for UIntPtr. Never pass
            // its output address to a helper that writes an eight-byte value.
            Emit($"*(intptr_t*)({output!.Expr}) = (intptr_t){low};");
            Push(CppTypes.KindOf(signature.ReturnType), "intptr_t", $"(intptr_t){high}");
            return;
        }
        Emit($"{high} = {helper}({a}, {b}, &{low});");
        var type = signature.ReturnType;
        var cls = type.Class!;
        Comp.EnsureCompleted(cls);
        NoteLocalValueLayout(type);
        string cpp = CppTypes.Of(type);
        var result = new StackEntry(NewTemp(cpp), StackKind.Struct, cpp, StaticType: type);
        var lower = cls.Fields.Single(field => field.Name == "_lower" && !field.IsStatic);
        var upper = cls.Fields.Single(field => field.Name == "_upper" && !field.IsStatic);
        Emit($"{FieldAccess(cls, lower, result)} = (uint64_t){low};");
        Emit($"{FieldAccess(cls, upper, result)} = (uint64_t){high};");
        PushEntry(result);
    }
}

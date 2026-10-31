using System.Reflection.Metadata;

namespace Dn2Cpp;

internal sealed partial class MethodCompiler
{
    private void EmitUInt128Checked(MethodInfo target)
    {
        var cls = target.DeclaringClass;
        if (!CoreIntrinsics.IsUInt128Checked(target) || !CoreIntrinsics.HasUInt128CheckedFields(cls))
            throw new NotSupportedException("UInt128.CreateChecked requires its exact integer factory and UInt64 field layout");
        Comp.EnsureCompleted(cls);
        var type = TypeDesc.MakeClass(cls);
        NoteLocalValueLayout(type);
        string cpp = CppTypes.Of(type);
        var result = new StackEntry(NewTemp(cpp), StackKind.Struct, cpp, StaticType: type);
        var source = target.Context.MethodArgs[0];
        var value = Pop();
        if (source.Kind == TypeKind.Class)
            Emit($"{result.Expr} = {Cast(value, cpp)};");
        else
        {
            bool signed = source.Primitive is PrimitiveTypeCode.SByte or PrimitiveTypeCode.Int16
                or PrimitiveTypeCode.Int32 or PrimitiveTypeCode.Int64 or PrimitiveTypeCode.IntPtr;
            string wide = NewTemp(signed ? "int64_t" : "uint64_t");
            // Native integers share a signed stack representation; restore UIntPtr's
            // target-width unsigned storage before widening it to UInt64.
            string storage = source.Primitive == PrimitiveTypeCode.UIntPtr ? "uintptr_t" : CppTypes.StorageOf(source);
            Emit($"{wide} = ({(signed ? "int64_t" : "uint64_t")})(({storage})({value.Expr}));");
            if (signed) Emit($"if ({wide} < 0) dn2cpp_overflow();");
            var lower = cls.Fields.Single(field => field.Name == "_lower" && !field.IsStatic);
            var upper = cls.Fields.Single(field => field.Name == "_upper" && !field.IsStatic);
            Emit($"{FieldAccess(cls, lower, result)} = ({LayoutFieldType(cls, lower)}){wide};");
            Emit($"{FieldAccess(cls, upper, result)} = ({LayoutFieldType(cls, upper)})0;");
        }
        PushEntry(result);
    }
}

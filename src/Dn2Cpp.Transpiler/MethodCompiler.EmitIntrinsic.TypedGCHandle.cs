using System.Reflection.Metadata;

namespace Dn2Cpp;

internal sealed partial class MethodCompiler
{
    private bool IsTypedGCHandle(ClassInfo owner) =>
        owner.IntrinsicCppName == "Dn2CppGCHandle" && owner.Context.TypeArgs.Length == 1
        && _c.GenericDefFullName(owner) == "System.Runtime.InteropServices.GCHandle";

    private string TypedGCHandleCtor(ClassInfo owner, MethodSignature<TypeDesc> sig)
    {
        if (!sig.Header.IsInstance || sig.GenericParameterCount != 0
            || sig.ParameterTypes.Length != 1
            || sig.ReturnType is not { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Void })
            throw new NotSupportedException($"{owner.FullName}: unsupported typed GCHandle constructor signature");
        var value = Pop();
        if (sig.ParameterTypes[0] is { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.IntPtr })
            return TypedGCHandleFromToken(value);
        if (sig.ParameterTypes[0] != owner.Context.TypeArgs[0])
            throw new NotSupportedException($"{owner.FullName}: unsupported typed GCHandle constructor parameter");
        string target = NewTemp("Dn2CppObject*");
        Emit($"{target} = {Cast(value, "Dn2CppObject*")};");
        return EmitGCHandleAlloc(new StackEntry(target, StackKind.Ref, "Dn2CppObject*"), "2");
    }

    private string TypedGCHandleFromToken(StackEntry value)
    {
        // Unlike non-generic GCHandle.FromIntPtr, the typed wrapper admits zero.
        string token = NewTemp("intptr_t");
        Emit($"{token} = (intptr_t)({value.Expr});");
        return $"({token} == 0 ? Dn2CppGCHandle{{}} : dn2cpp_gchandle_from_intptr({token}))";
    }

    private bool TryEmitTypedGCHandleIntrinsic(ClassInfo owner, string name, MethodSignature<TypeDesc> sig)
    {
        if (!IsTypedGCHandle(owner)) return false;
        var ps = sig.ParameterTypes;
        switch (name)
        {
            case ".ctor":
            {
                string value = TypedGCHandleCtor(owner, sig);
                var self = Pop();
                Emit($"*(Dn2CppGCHandle*)({self.Expr}) = {value};");
                return true;
            }
            case "get_Target" when sig.Header.IsInstance && ps.Length == 0:
            {
                var self = Pop();
                string handle = TypedGCHandleTargetReceiver(self);
                string ct = CppTypes.Of(sig.ReturnType);
                string target = $"dn2cpp_gchandle_target({handle})";
                Push(CppTypes.KindOf(sig.ReturnType), ct,
                    Cast(new StackEntry(target, StackKind.Ref, "Dn2CppObject*"), ct));
                Top = Top with { StaticType = sig.ReturnType };
                return true;
            }
            case "set_Target" when sig.Header.IsInstance && ps.Length == 1:
            {
                var value = Pop();
                string handle = TypedGCHandleTargetReceiver(Pop());
                Emit($"dn2cpp_gchandle_set_target({handle}, {Cast(value, "Dn2CppObject*")});");
                return true;
            }
            case "get_IsAllocated" when sig.Header.IsInstance && ps.Length == 0:
            case "GetHashCode" when sig.Header.IsInstance && ps.Length == 0:
            case "ToIntPtr" when !sig.Header.IsInstance && ps.Length == 1:
                return TryEmitGCHandleIntrinsic(name, sig);
            case "FromIntPtr" when !sig.Header.IsInstance && ps is [{ Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.IntPtr }]:
                Push(StackKind.Struct, "Dn2CppGCHandle", TypedGCHandleFromToken(Pop()));
                return true;
            case "Dispose" when sig.Header.IsInstance && ps.Length == 0:
            {
                // A typed wrapper's zero state is already disposed. Live storage still
                // goes through the shared strong-cell release and clears this copy.
                var self = Pop();
                Emit($"if (dn2cpp_gchandle_is_allocated({GCHVal(self)})) "
                    + (self.Kind == StackKind.Ptr
                        ? $"dn2cpp_gchandle_free((Dn2CppGCHandle*)({self.Expr}));"
                        : $"dn2cpp_gchandle_free_value({self.Expr});"));
                return true;
            }
            case "Equals" when sig.Header.IsInstance && ps.Length == 1:
            {
                var other = Pop();
                var self = Pop();
                if (ps[0].IsObject)
                {
                    string ti = TypeInfoExpr(TypeDesc.MakeClass(owner))
                        ?? throw new NotSupportedException($"{owner.FullName}: typed GCHandle has no runtime identity");
                    string boxed = NewTemp("Dn2CppObject*");
                    Emit($"{boxed} = {Cast(other, "Dn2CppObject*")};");
                    Push(StackKind.I4, "int32_t", $"({boxed} != nullptr && {boxed}->type == {ti} "
                        + $"&& ((Dn2CppGCHandle*)({boxed} + 1))->cell == ({GCHVal(self)}).cell ? 1 : 0)");
                    return true;
                }
                if (ps[0] is { Kind: TypeKind.Class, Class: { } typed } && typed == owner)
                {
                    Push(StackKind.I4, "int32_t", $"(({GCHVal(self)}).cell == ({GCHVal(other)}).cell ? 1 : 0)");
                    return true;
                }
                break;
            }
        }
        throw new NotSupportedException($"{owner.FullName}::{name}: typed GCHandle member signature is not modeled");
    }

    private string TypedGCHandleTargetReceiver(StackEntry self)
    {
        // Typed Target dereferences its token directly: zero is a null reference,
        // not the non-generic wrapper's explicit uninitialized-handle exception.
        string handle = NewTemp("Dn2CppGCHandle");
        Emit($"{handle} = {GCHVal(self)};");
        Emit($"if (!dn2cpp_gchandle_is_allocated({handle})) dn2cpp_throw_null_reference();");
        return handle;
    }
}

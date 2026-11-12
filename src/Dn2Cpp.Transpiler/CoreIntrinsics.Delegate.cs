using System.Reflection.Metadata;

namespace Dn2Cpp;

internal static partial class CoreIntrinsics
{
    internal static void ValidateDelegateCallableBody(MethodInfo method)
    {
        if (DelegateCallableBodyContract.IsName(method.DeclaringClass.FullName, method.Name)
            && !IsDelegateCallableBody(method))
            throw new NotSupportedException("Delegate callable member requires its exact CoreLib declaration");
    }

    internal static bool IsDelegateCallableBody(MethodInfo method)
    {
        var cls = method.DeclaringClass;
        if (!DelegateCallableBodyContract.IsName(cls.FullName, method.Name)
            || method.Module.AssemblyName != "System.Private.CoreLib" || cls.Module != method.Module
            || cls.Handle.IsNil || method.Handle.IsNil || cls.IsValueType || cls.IsEnum || cls.IsInterface
            || cls.GenericArity != 0 || cls.Context.TypeArgs.Length != 0 || cls.Context.MethodArgs.Length != 0
            || method.Context.TypeArgs.Length != 0 || method.Context.MethodArgs.Length != 0
            || method.IsSynthetic || method.PInvoke is not null) return false;
        var definition = method.Module.Reader.GetMethodDefinition(method.Handle);
        return definition.GetDeclaringType() == cls.Handle && definition.RelativeVirtualAddress == method.Rva
            && definition.Attributes == method.Attributes && definition.ImplAttributes == method.ImplAttributes
            && DelegateCallableBodyContract.IsDefinition(method.Module.Reader, method.Handle, cls.FullName, method.Name)
            && IsDelegateCallableSignature(method, method.Signature);
    }

    private static bool DelegateClass(TypeDesc type, Module module, string name) =>
        type is { Kind: TypeKind.Class, IsCanonPlaceholder: false, Class: { } cls }
        && cls.Module == module && !cls.Handle.IsNil && cls.FullName == name && !cls.IsValueType && !cls.IsInterface
        && cls.GenericArity == 0 && cls.Context.TypeArgs.Length == 0 && cls.Context.MethodArgs.Length == 0
        && module.Reader.GetTypeDefinition(cls.Handle).GetDeclaringType().IsNil;

    private static bool DelegatePrimitive(TypeDesc type, PrimitiveTypeCode code) =>
        type is { Kind: TypeKind.Primitive, IsCanonPlaceholder: false } && type.Primitive == code;

    private static bool IsDelegateCallableSignature(MethodInfo method, MethodSignature<TypeDesc> signature)
    {
        if (signature.Header.RawValue != 0x20 || signature.GenericParameterCount != 0
            || signature.RequiredParameterCount != signature.ParameterTypes.Length) return false;
        var p = signature.ParameterTypes;
        var r = signature.ReturnType;
        var module = method.Module;
        switch (method.Name)
        {
            case ".ctor":
                return DelegatePrimitive(r, PrimitiveTypeCode.Void) && p.Length == 2
                    && (DelegatePrimitive(p[0], PrimitiveTypeCode.Object) || DelegateClass(p[0], module, "System.Type"))
                    && DelegatePrimitive(p[1], PrimitiveTypeCode.String);
            case "Equals":
                return DelegatePrimitive(r, PrimitiveTypeCode.Boolean) && p.Length == 1 && DelegatePrimitive(p[0], PrimitiveTypeCode.Object);
            case "GetHashCode":
                return DelegatePrimitive(r, PrimitiveTypeCode.Int32) && p.Length == 0;
            case "NewMulticastDelegate":
                return DelegateClass(r, module, "System.MulticastDelegate") && p.Length is 2 or 3
                    && p[0] is { Kind: TypeKind.SZArray, Element: { } element } && DelegatePrimitive(element, PrimitiveTypeCode.Object)
                    && DelegatePrimitive(p[1], PrimitiveTypeCode.Int32) && (p.Length == 2 || DelegatePrimitive(p[2], PrimitiveTypeCode.Boolean));
            case "CombineImpl" or "RemoveImpl":
                return DelegateClass(r, module, "System.Delegate") && p.Length == 1 && DelegateClass(p[0], module, "System.Delegate");
            case "GetTarget":
                return DelegatePrimitive(r, PrimitiveTypeCode.Object) && p.Length == 0;
            case "GetMethodImpl":
                return DelegateClass(r, module, "System.Reflection.MethodInfo") && p.Length == 0;
            case "GetInvocationList":
                return r is { Kind: TypeKind.SZArray, Element: { } entry } && DelegateClass(entry, module, "System.Delegate") && p.Length == 0;
            case "get_HasSingleTarget":
                return DelegatePrimitive(r, PrimitiveTypeCode.Boolean) && p.Length == 0;
            case "TryGetAt":
                return DelegatePrimitive(r, PrimitiveTypeCode.Object) && p.Length == 1 && DelegatePrimitive(p[0], PrimitiveTypeCode.Int32);
            default:
                if (p.Length != DelegateCallableBodyContract.VmConstructorArity(method.Name) || p.Length < 2
                    || !DelegatePrimitive(r, PrimitiveTypeCode.Void) || !DelegatePrimitive(p[0], PrimitiveTypeCode.Object)) return false;
                for (int i = 1; i < p.Length; i++) if (!DelegatePrimitive(p[i], PrimitiveTypeCode.IntPtr)) return false;
                return true;
        }
    }

    private static bool SameDelegateType(TypeDesc left, TypeDesc right)
    {
        if (left.Kind != right.Kind) return false;
        if (left.Kind == TypeKind.Primitive) return left.Primitive == right.Primitive;
        if (left.Kind == TypeKind.SZArray)
            return left.Element is { } le && right.Element is { } re && SameDelegateType(le, re);
        return left.Class is { } lc && right.Class is { } rc && lc.Module == rc.Module && lc.Handle == rc.Handle;
    }

    internal static bool IsDelegateCallableReference(MethodInfo method, MethodSignature<TypeDesc> signature, BlobReader blob)
    {
        if (!IsDelegateCallableBody(method) || !IsDelegateCallableSignature(method, signature)
            || !DelegateCallableBodyContract.Signature(method.Module.Reader, method.DeclaringClass.FullName, method.Name, blob, false, out _))
            return false;
        var actual = method.Signature;
        if (signature.ParameterTypes.Length != actual.ParameterTypes.Length || !SameDelegateType(signature.ReturnType, actual.ReturnType)) return false;
        for (int i = 0; i < actual.ParameterTypes.Length; i++)
            if (!SameDelegateType(signature.ParameterTypes[i], actual.ParameterTypes[i])) return false;
        return true;
    }
}

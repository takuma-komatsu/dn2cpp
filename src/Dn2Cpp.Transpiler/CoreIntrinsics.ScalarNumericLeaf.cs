using System.Reflection;
using System.Reflection.Metadata;

namespace Dn2Cpp;

internal static partial class CoreIntrinsics
{
    internal static bool IsScalarNumericLeafOwner(ClassInfo owner) =>
        owner.Module.AssemblyName == "System.Private.CoreLib" && !owner.Handle.IsNil
        && owner.IsValueType && !owner.IsEnum && !owner.IsInterface && !owner.IsByRefLike
        && owner.GenericArity == 0 && owner.Context.TypeArgs.Length == 0 && owner.Context.MethodArgs.Length == 0;

    internal static bool IsScalarNumericLeaf(MethodInfo method)
    {
        var owner = method.DeclaringClass;
        if (!ScalarNumericLeafContract.IsCandidate(owner.FullName, method.Name)
            || !IsScalarNumericLeafOwner(owner) || method.Module != owner.Module
            || method.Handle.IsNil || method.IsSynthetic
            || method.Context.TypeArgs.Length != 0 || method.Context.MethodArgs.Length != 0
            || method.Rva == 0 || method.PInvoke is not null
            || method.Attributes != ScalarNumericLeafContract.Attributes(owner.FullName, method.Name)
            || method.ImplAttributes != MethodImplAttributes.IL
            || !IsScalarNumericLeafSignature(owner.FullName, method.Name, method.Signature)) return false;
        var reader = method.Module.Reader;
        var definition = reader.GetMethodDefinition(method.Handle);
        return definition.GetDeclaringType() == owner.Handle && definition.RelativeVirtualAddress == method.Rva
            && ScalarNumericLeafContract.IsDefinition(reader, method.Handle, owner.FullName, method.Name)
            && IsScalarNumericLeafBody(method);
    }

    private static bool IsScalarNumericLeafBody(MethodInfo method)
    {
        if (!ScalarNumericLeafContract.RequiresSingleStorageLoad(method.DeclaringClass.FullName, method.Name)) return true;
        var module = method.Module;
        if (!ScalarNumericLeafContract.TrySingleStorageTargets(module.Reader, module.PE, method.Handle, method.Name,
                out var bitHandle, out var extractHandle)
            || !module.MethodMap.TryGetValue(bitHandle, out var bits)
            || !module.MethodMap.TryGetValue(extractHandle, out var extract)
            || extract.Module != module || extract.Handle != extractHandle || extract.DeclaringClass != method.DeclaringClass
            || !IsScalarNumericLeaf(extract)) return false;
        var definition = module.Reader.GetMethodDefinition(bitHandle);
        var signature = bits.Signature;
        return bits.Module == module && bits.DeclaringClass.Module == module && bits.Handle == bitHandle
            && bits.DeclaringClass.Handle == definition.GetDeclaringType() && bits.DeclaringClass.FullName == "System.BitConverter"
            && !bits.DeclaringClass.IsValueType && !bits.DeclaringClass.IsInterface && !bits.DeclaringClass.IsEnum
            && !bits.DeclaringClass.IsByRefLike
            && bits.DeclaringClass.GenericArity == 0 && bits.DeclaringClass.Context.TypeArgs.Length == 0
            && bits.DeclaringClass.Context.MethodArgs.Length == 0
            && bits.Context.TypeArgs.Length == 0 && bits.Context.MethodArgs.Length == 0
            && !bits.IsSynthetic && bits.PInvoke is null && bits.Name == "SingleToUInt32Bits"
            && bits.Rva == definition.RelativeVirtualAddress && bits.Attributes == definition.Attributes
            && bits.ImplAttributes == definition.ImplAttributes
            && signature.Header.RawValue == 0 && signature.GenericParameterCount == 0
            && signature.RequiredParameterCount == 1 && signature.ParameterTypes.Length == 1
            && signature.ReturnType is { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.UInt32, IsCanonPlaceholder: false }
            && signature.ParameterTypes[0] is { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Single, IsCanonPlaceholder: false };
    }

    internal static bool IsSingleStorageBitLoad(MethodInfo method) =>
        ScalarNumericLeafContract.RequiresSingleStorageLoad(method.DeclaringClass.FullName, method.Name) && IsScalarNumericLeaf(method);

    internal static bool IsScalarNumericLeafSignature(string owner, string name, MethodSignature<TypeDesc> signature)
    {
        if (!ScalarNumericLeafContract.IsCandidate(owner, name)) return false;
        bool instance = ScalarNumericLeafContract.IsInstance(name);
        int count = instance ? 0 : 1;
        return signature.Header.RawValue == (instance ? 0x20 : 0) && signature.GenericParameterCount == 0
            && signature.RequiredParameterCount == count && signature.ParameterTypes.Length == count
            && signature.ReturnType is { Kind: TypeKind.Primitive, IsCanonPlaceholder: false } result
            && result.Primitive == ScalarNumericLeafContract.Result(owner, name)
            && (instance || signature.ParameterTypes[0] is { Kind: TypeKind.Primitive, IsCanonPlaceholder: false } parameter
                && parameter.Primitive == ScalarNumericLeafContract.Parameter(owner));
    }

    internal static void ValidateScalarNumericLeaf(MethodInfo method)
    {
        if (ScalarNumericLeafContract.IsCandidate(method.DeclaringClass.FullName, method.Name)
            && !IsScalarNumericLeaf(method))
            throw new NotSupportedException("Scalar numeric leaf requires its exact CoreLib declaration");
    }
}

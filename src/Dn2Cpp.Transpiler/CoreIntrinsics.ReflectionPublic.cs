using System.Reflection;
using System.Reflection.Metadata;

namespace Dn2Cpp;

internal static partial class CoreIntrinsics
{
    internal static bool IsModuleAttributeDataSlot(MethodInfo method) =>
        method.Name == "GetCustomAttributesData" && method.DeclaringClass.FullName == "System.Reflection.Module"
        && IsReflectionManagedBody(method);

    internal static bool IsAttributeArgumentsSlot(MethodInfo method) =>
        method.Name == "get_ConstructorArguments" && method.DeclaringClass.FullName == "System.Reflection.CustomAttributeData"
        && IsReflectionManagedBody(method);

    internal static bool IsAssemblyCollectibilitySlot(MethodInfo method) =>
        method.Name == "get_IsCollectible" && method.DeclaringClass.FullName == "System.Reflection.Assembly"
        && IsReflectionManagedBody(method);

    internal static bool IsReflectionManagedBody(MethodInfo method) =>
        ReflectionManagedBodyContract.IsName(method.DeclaringClass.FullName, method.Name)
        && method.DeclaringClass.Module == method.Module
        && method.Context.TypeArgs.Length == 0 && method.Context.MethodArgs.Length == 0
        && !method.IsSynthetic && method.PInvoke is null
        && ReflectionManagedBodyContract.IsDefinition(method.Module.Reader, method.Handle,
            method.DeclaringClass.FullName, method.Name);

    internal static bool IsMemberInfoPropertyName(string? owner, string name) =>
        owner == "System.Reflection.MemberInfo" && name is
            "get_Name" or "get_DeclaringType" or "get_ReflectedType" or "get_MemberType";

    internal static bool IsMemberInfoProperty(MethodInfo method)
    {
        if (!IsMemberInfoPropertyName(method.DeclaringClass.FullName, method.Name)
            || method.Module.AssemblyName != "System.Private.CoreLib" || method.Handle.IsNil
            || method.DeclaringClass.Module != method.Module || method.IsSynthetic || method.PInvoke is not null
            || method.Context.TypeArgs.Length != 0 || method.Context.MethodArgs.Length != 0
            || method.Attributes != (MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.NewSlot
                | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.Abstract))
            return false;
        var signature = method.Signature;
        return signature.Header.RawValue == 0x20 && signature.GenericParameterCount == 0
            && signature.ParameterTypes.Length == 0 && (method.Name switch
            {
                "get_Name" => signature.ReturnType.IsString,
                "get_MemberType" => signature.ReturnType is { Kind: TypeKind.Class,
                    Class: { FullName: "System.Reflection.MemberTypes", IsEnum: true } },
                _ => signature.ReturnType is { Kind: TypeKind.Class, Class.FullName: "System.Type" },
            });
    }

    internal static bool IsTypeAttributeFlagsName(string? owner, string name) =>
        owner == "System.Type" && name == "GetAttributeFlagsImpl";

    internal static bool IsTypeAttributeFlags(MethodInfo method)
    {
        if (!IsTypeAttributeFlagsName(method.DeclaringClass.FullName, method.Name)
            || method.Module.AssemblyName != "System.Private.CoreLib" || method.Handle.IsNil
            || method.DeclaringClass.Module != method.Module || method.IsSynthetic || method.PInvoke is not null
            || method.Context.TypeArgs.Length != 0 || method.Context.MethodArgs.Length != 0
            || method.Attributes != (MethodAttributes.Family | MethodAttributes.Virtual | MethodAttributes.NewSlot
                | MethodAttributes.HideBySig | MethodAttributes.Abstract)
            || method.Rva != 0 || method.ImplAttributes != MethodImplAttributes.IL)
            return false;
        var signature = method.Signature;
        return signature.Header.RawValue == 0x20 && signature.GenericParameterCount == 0
            && signature.ParameterTypes.Length == 0
            && signature.ReturnType is { Kind: TypeKind.Class,
                Class: { FullName: "System.Reflection.TypeAttributes", IsEnum: true } result }
            && result.Module == method.Module;
    }
}

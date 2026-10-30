using System.Reflection;
using System.Reflection.Metadata;

namespace Dn2Cpp;

internal static partial class CoreIntrinsics
{
    internal static bool IsInvariantCompareInfoGetter(MethodInfo method)
    {
        var cls = method.DeclaringClass;
        if (method.Name != "get_CompareInfo" || cls.FullName is not
                ("System.Globalization.CultureInfo" or "System.Globalization.DateTimeFormatInfo")
            || method.Module.AssemblyName != "System.Private.CoreLib" || cls.Module != method.Module
            || cls.Handle.IsNil || method.Handle.IsNil || cls.IsValueType || cls.IsInterface
            || cls.GenericArity != 0 || cls.Context.TypeArgs.Length != 0
            || method.ImplAttributes != MethodImplAttributes.IL || method.Rva == 0 || method.PInvoke is not null)
            return false;
        var expected = cls.FullName == "System.Globalization.CultureInfo"
            ? MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.NewSlot
                | MethodAttributes.HideBySig | MethodAttributes.SpecialName
            : MethodAttributes.Assembly | MethodAttributes.HideBySig | MethodAttributes.SpecialName;
        var reader = method.Module.Reader;
        var owner = reader.GetTypeDefinition(cls.Handle);
        return method.Attributes == expected && owner.GetDeclaringType().IsNil
            && owner.GetGenericParameters().Count == 0
            && reader.StringComparer.Equals(owner.Namespace, "System.Globalization")
            && reader.StringComparer.Equals(owner.Name, cls.FullName == "System.Globalization.CultureInfo"
                ? "CultureInfo" : "DateTimeFormatInfo")
            && IsInvariantCompareInfoGetterSignature(reader.GetBlobReader(reader.GetMethodDefinition(method.Handle).Signature))
            && IsInvariantCompareInfoGetterDecodedSignature(method.Signature, method.Module)
            && TryGetInvariantCompareInfoFields(method.Signature.ReturnType.Class!, out _, out _);
    }

    internal static bool IsInvariantCompareInfoGetterSignature(BlobReader blob) =>
        blob.RemainingBytes >= 4 && blob.ReadByte() == 0x20 && blob.ReadByte() == 0
        && blob.ReadByte() == 0x12 && blob.TryReadCompressedInteger(out int token)
        && token > 0 && (token >> 2) != 0 && (token & 3) != 3 && blob.RemainingBytes == 0;

    internal static bool IsInvariantCompareInfoGetterDecodedSignature(MethodSignature<TypeDesc> signature, Module module)
        => signature.Header.RawValue == 0x20 && signature.GenericParameterCount == 0
            && signature.ParameterTypes.Length == 0
            && signature.ReturnType is { Kind: TypeKind.Class, Class: { } cls }
            && cls.Module == module && IsInvariantCompareInfoType(cls);

    private static bool IsInvariantCompareInfoType(ClassInfo cls)
    {
        if (cls.FullName != "System.Globalization.CompareInfo" || cls.Module.AssemblyName != "System.Private.CoreLib"
            || cls.Handle.IsNil || cls.IsValueType || cls.IsEnum || cls.IsInterface
            || cls.GenericArity != 0 || cls.Context.TypeArgs.Length != 0)
            return false;
        var reader = cls.Module.Reader;
        var definition = reader.GetTypeDefinition(cls.Handle);
        return definition.GetDeclaringType().IsNil && definition.GetGenericParameters().Count == 0
            && (definition.Attributes & (TypeAttributes.Sealed | TypeAttributes.Abstract | TypeAttributes.LayoutMask | TypeAttributes.ClassSemanticsMask)) == TypeAttributes.Sealed
            && reader.StringComparer.Equals(definition.Namespace, "System.Globalization")
            && reader.StringComparer.Equals(definition.Name, "CompareInfo");
    }

    internal static bool TryGetInvariantCompareInfoFields(ClassInfo cls, out FieldInfo name, out FieldInfo sortName)
    {
        name = null!;
        sortName = null!;
        if (!IsInvariantCompareInfoType(cls)) return false;
        foreach (var field in cls.Fields)
        {
            if (field.Name is not ("m_name" or "_sortName")) continue;
            if (field.DeclaringClass != cls || field.Handle.IsNil || field.IsStatic || field.IsLiteral
                || field.ByValArraySize >= 0)
                return false;
            bool owned = false;
            foreach (var handle in cls.Module.Reader.GetTypeDefinition(cls.Handle).GetFields())
                if (handle == field.Handle) { owned = true; break; }
            if (!owned) return false;
            var definition = cls.Module.Reader.GetFieldDefinition(field.Handle);
            if (!cls.Module.Reader.StringComparer.Equals(definition.Name, field.Name)) return false;
            var expected = field.Name == "m_name" ? FieldAttributes.Private : FieldAttributes.Private | (FieldAttributes)0x0080;
            var blob = cls.Module.Reader.GetBlobReader(definition.Signature);
            if (definition.Attributes != expected || !definition.GetMarshallingDescriptor().IsNil
                || blob.RemainingBytes != 2 || blob.ReadByte() != 0x06 || blob.ReadByte() != 0x0E)
                return false;
            if (field.TypeReady && field.Type is not { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.String })
                return false;
            if (field.Name == "m_name")
            {
                if (name is not null) return false;
                name = field;
            }
            else
            {
                if (sortName is not null) return false;
                sortName = field;
            }
        }
        return name is not null && sortName is not null;
    }

    internal static string InvariantCompareInfoFactoryName(ClassInfo cls) => "dn2cpp_invariant_compare_info_" + cls.CppName;
}

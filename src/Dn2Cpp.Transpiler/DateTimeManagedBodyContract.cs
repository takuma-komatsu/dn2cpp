using System.Reflection;
using System.Reflection.Metadata;
using SRME = System.Reflection.Metadata.Ecma335.MetadataTokens;

namespace Dn2Cpp;

// Shared with ILDiet: these arithmetic bodies use the runtime-mapped value fields.
internal static class DateTimeManagedBodyContract
{
    internal static bool IsName(string owner, string name) => owner switch
    {
        "System.DateOnly" => name is "Deconstruct" or "GetEquivalentDateTime",
        "System.DateTime" => name is "TryAddTicks" or "TryCreate" or ".ctor" or "GetDate"
            or "CreateUnchecked" or "get_InternalKind" or "get_DaysToMonth365" or "get_DaysToMonth366"
            or "DaysToYear" or "TimeToTicks" or "ThrowTicksOutOfRange",
        _ => false,
    };

    private static bool Byte(ref BlobReader blob, byte value) =>
        blob.RemainingBytes > 0 && blob.ReadByte() == value;

    private static bool Primitive(ref BlobReader blob, PrimitiveTypeCode value) => Byte(ref blob, (byte)value);

    private static bool NamedValue(ref BlobReader blob, MetadataReader reader, string name, int arity = 0)
    {
        if (!Byte(ref blob, 0x11) || !blob.TryReadCompressedInteger(out int token)
            || (token & 3) != 0 || (token >> 2) == 0
            || (token >> 2) > reader.TypeDefinitions.Count) return false;
        var type = reader.GetTypeDefinition(SRME.TypeDefinitionHandle(token >> 2));
        if (!type.GetDeclaringType().IsNil || type.GetGenericParameters().Count != arity
            || !reader.StringComparer.Equals(type.Namespace, "System") || !reader.StringComparer.Equals(type.Name, name)
            || (type.Attributes & TypeAttributes.Interface) != 0 || type.BaseType.Kind != HandleKind.TypeDefinition)
            return false;
        var baseType = reader.GetTypeDefinition((TypeDefinitionHandle)type.BaseType);
        return baseType.GetDeclaringType().IsNil && reader.StringComparer.Equals(baseType.Namespace, "System")
            && reader.StringComparer.Equals(baseType.Name, name == "DateTimeKind" ? "Enum" : "ValueType");
    }

    private static bool Int32Ref(ref BlobReader blob) =>
        Byte(ref blob, 0x10) && Primitive(ref blob, PrimitiveTypeCode.Int32);

    private static bool DateTimeRef(ref BlobReader blob, MetadataReader reader) =>
        Byte(ref blob, 0x10) && NamedValue(ref blob, reader, "DateTime");

    private static bool Header(ref BlobReader blob, bool instance, int count) =>
        Byte(ref blob, instance ? (byte)0x20 : (byte)0) && Byte(ref blob, (byte)count);

    private static bool Signature(MetadataReader reader, string owner, string name, BlobReader blob,
        out MethodAttributes attributes, out MethodImplAttributes implementation)
    {
        attributes = MethodAttributes.Assembly | MethodAttributes.HideBySig;
        implementation = MethodImplAttributes.IL;
        bool matched;
        if (owner == "System.DateOnly")
        {
            if (name == "Deconstruct")
            {
                attributes = MethodAttributes.Public | MethodAttributes.HideBySig;
                matched = Header(ref blob, true, 3) && Primitive(ref blob, PrimitiveTypeCode.Void)
                    && Int32Ref(ref blob) && Int32Ref(ref blob) && Int32Ref(ref blob);
            }
            else
                matched = Header(ref blob, true, 0) && NamedValue(ref blob, reader, "DateTime");
            return matched && blob.RemainingBytes == 0;
        }
        switch (name)
        {
            case "TryAddTicks":
                matched = Header(ref blob, true, 2) && Primitive(ref blob, PrimitiveTypeCode.Boolean)
                    && Primitive(ref blob, PrimitiveTypeCode.Int64) && DateTimeRef(ref blob, reader);
                break;
            case "TryCreate":
                attributes |= MethodAttributes.Static;
                matched = Header(ref blob, false, 8) && Primitive(ref blob, PrimitiveTypeCode.Boolean);
                for (int i = 0; i < 7 && matched; i++) matched = Primitive(ref blob, PrimitiveTypeCode.Int32);
                matched = matched && DateTimeRef(ref blob, reader);
                break;
            case ".ctor":
            {
                var local = blob;
                matched = Header(ref local, true, 3) && Primitive(ref local, PrimitiveTypeCode.Void)
                    && Primitive(ref local, PrimitiveTypeCode.Int64) && NamedValue(ref local, reader, "DateTimeKind")
                    && Primitive(ref local, PrimitiveTypeCode.Boolean) && local.RemainingBytes == 0;
                attributes |= MethodAttributes.SpecialName | MethodAttributes.RTSpecialName;
                if (matched) { blob = local; break; }
                attributes = MethodAttributes.Private | MethodAttributes.HideBySig
                    | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName;
                matched = Header(ref blob, true, 1) && Primitive(ref blob, PrimitiveTypeCode.Void)
                    && Primitive(ref blob, PrimitiveTypeCode.UInt64);
                break;
            }
            case "GetDate":
            {
                var instance = blob;
                matched = Header(ref instance, true, 3) && Primitive(ref instance, PrimitiveTypeCode.Void)
                    && Int32Ref(ref instance) && Int32Ref(ref instance) && Int32Ref(ref instance)
                    && instance.RemainingBytes == 0;
                if (matched) { blob = instance; break; }
                attributes = MethodAttributes.Private | MethodAttributes.Static | MethodAttributes.HideBySig;
                matched = Header(ref blob, false, 4) && Primitive(ref blob, PrimitiveTypeCode.Void)
                    && Primitive(ref blob, PrimitiveTypeCode.UInt64)
                    && Int32Ref(ref blob) && Int32Ref(ref blob) && Int32Ref(ref blob);
                break;
            }
            case "CreateUnchecked":
                attributes |= MethodAttributes.Static;
                matched = Header(ref blob, false, 1) && NamedValue(ref blob, reader, "DateTime")
                    && Primitive(ref blob, PrimitiveTypeCode.Int64);
                break;
            case "get_InternalKind":
                attributes = MethodAttributes.Private | MethodAttributes.HideBySig | MethodAttributes.SpecialName;
                matched = Header(ref blob, true, 0) && Primitive(ref blob, PrimitiveTypeCode.UInt64);
                break;
            case "get_DaysToMonth365" or "get_DaysToMonth366":
                attributes |= MethodAttributes.Static | MethodAttributes.SpecialName;
                matched = Header(ref blob, false, 0) && Byte(ref blob, 0x15)
                    && NamedValue(ref blob, reader, "ReadOnlySpan`1", 1) && Byte(ref blob, 1)
                    && Primitive(ref blob, PrimitiveTypeCode.UInt32);
                break;
            case "DaysToYear":
                attributes = MethodAttributes.Private | MethodAttributes.Static | MethodAttributes.HideBySig;
                implementation = MethodImplAttributes.AggressiveInlining;
                matched = Header(ref blob, false, 1) && Primitive(ref blob, PrimitiveTypeCode.UInt32)
                    && Primitive(ref blob, PrimitiveTypeCode.UInt32);
                break;
            case "TimeToTicks":
                attributes = MethodAttributes.Private | MethodAttributes.Static | MethodAttributes.HideBySig;
                implementation = MethodImplAttributes.AggressiveInlining;
                matched = Header(ref blob, false, 3) && Primitive(ref blob, PrimitiveTypeCode.UInt64)
                    && Primitive(ref blob, PrimitiveTypeCode.Int32) && Primitive(ref blob, PrimitiveTypeCode.Int32)
                    && Primitive(ref blob, PrimitiveTypeCode.Int32);
                break;
            default:
                attributes = MethodAttributes.Private | MethodAttributes.Static | MethodAttributes.HideBySig;
                matched = Header(ref blob, false, 0) && Primitive(ref blob, PrimitiveTypeCode.Void);
                break;
        }
        return matched && blob.RemainingBytes == 0;
    }

    internal static bool IsDefinition(MetadataReader reader, MethodDefinitionHandle handle, string owner, string name)
    {
        if (!IsName(owner, name) || handle.IsNil || !reader.IsAssembly
            || !reader.StringComparer.Equals(reader.GetAssemblyDefinition().Name, "System.Private.CoreLib")) return false;
        var method = reader.GetMethodDefinition(handle);
        var type = reader.GetTypeDefinition(method.GetDeclaringType());
        if (!reader.StringComparer.Equals(type.Namespace, "System")
            || !reader.StringComparer.Equals(type.Name, owner == "System.DateOnly" ? "DateOnly" : "DateTime")
            || !type.GetDeclaringType().IsNil || type.GetGenericParameters().Count != 0
            || (type.Attributes & (TypeAttributes.Interface | TypeAttributes.Abstract | TypeAttributes.Sealed)) != TypeAttributes.Sealed
            || type.BaseType.Kind != HandleKind.TypeDefinition) return false;
        var baseType = reader.GetTypeDefinition((TypeDefinitionHandle)type.BaseType);
        return baseType.GetDeclaringType().IsNil && reader.StringComparer.Equals(baseType.Namespace, "System")
            && reader.StringComparer.Equals(baseType.Name, "ValueType") && reader.StringComparer.Equals(method.Name, name)
            && method.GetGenericParameters().Count == 0 && method.RelativeVirtualAddress != 0
            && Signature(reader, owner, name, reader.GetBlobReader(method.Signature), out var attributes, out var implementation)
            && method.Attributes == attributes && method.ImplAttributes == implementation;
    }
}

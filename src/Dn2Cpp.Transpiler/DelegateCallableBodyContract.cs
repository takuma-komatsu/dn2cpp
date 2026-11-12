using System.Reflection;
using System.Reflection.Metadata;
using SRME = System.Reflection.Metadata.Ecma335.MetadataTokens;

namespace Dn2Cpp;

// ILDiet and emission cut only the CoreCLR field/stub-dependent declarations.
internal static class DelegateCallableBodyContract
{
    internal static bool IsName(string owner, string name) =>
        CoreIntrinsicTypeMaps.IsDelegateBaseType(owner) && (name is ".ctor" or "Equals" or "GetHashCode"
            || owner == "System.MulticastDelegate" && (name is "NewMulticastDelegate" or "CombineImpl" or "RemoveImpl"
                or "GetTarget" or "GetMethodImpl" or "GetInvocationList" or "get_HasSingleTarget" or "TryGetAt"
                || VmConstructorArity(name) != 0));

    internal static int VmConstructorArity(string name) => name switch
    {
        "CtorClosed" or "CtorClosedStatic" or "CtorRTClosed" => 2,
        "CtorOpened" or "CtorVirtualDispatch" or "CtorCollectibleClosedStatic" => 3,
        "CtorCollectibleOpened" or "CtorCollectibleVirtualDispatch" => 4,
        _ => 0,
    };

    private static bool Byte(ref BlobReader blob, byte value) =>
        blob.RemainingBytes > 0 && blob.ReadByte() == value;

    private static bool NamedTarget(ref BlobReader blob, MetadataReader reader, bool definition)
    {
        var original = blob;
        if (Byte(ref blob, 0x1C)) return true;
        blob = original;
        return Class(ref blob, reader, "System", "Type", definition);
    }

    private static bool Class(ref BlobReader blob, MetadataReader reader, string space, string name, bool definition)
    {
        if (!Byte(ref blob, 0x12) || !blob.TryReadCompressedInteger(out int token)
            || (token >> 2) == 0 || (token & 3) >= 2) return false;
        if (!definition) return true; // decoded reference identity is checked by the transpiler
        if ((token & 3) != 0 || (token >> 2) > reader.TypeDefinitions.Count) return false;
        var type = reader.GetTypeDefinition(SRME.TypeDefinitionHandle(token >> 2));
        return type.GetDeclaringType().IsNil && type.GetGenericParameters().Count == 0
            && (type.Attributes & (TypeAttributes.Abstract | TypeAttributes.Sealed | TypeAttributes.Interface)) == TypeAttributes.Abstract
            && reader.StringComparer.Equals(type.Namespace, space) && reader.StringComparer.Equals(type.Name, name);
    }

    internal static bool Signature(MetadataReader reader, string owner, string name, BlobReader blob,
        bool definition, out MethodAttributes attributes)
    {
        attributes = MethodAttributes.HideBySig;
        if (!IsName(owner, name) || !Byte(ref blob, 0x20) || !blob.TryReadCompressedInteger(out int count)) return false;
        bool multicast = owner == "System.MulticastDelegate";
        bool matched;
        switch (name)
        {
            case ".ctor":
                attributes |= MethodAttributes.Family | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName;
                matched = count == 2 && Byte(ref blob, 1)
                    && NamedTarget(ref blob, reader, definition) && Byte(ref blob, 0x0E);
                break;
            case "Equals":
                attributes |= MethodAttributes.Public | MethodAttributes.Virtual;
                if (multicast) attributes |= MethodAttributes.Final;
                matched = count == 1 && Byte(ref blob, 2) && Byte(ref blob, 0x1C);
                break;
            case "GetHashCode":
                attributes |= MethodAttributes.Public | MethodAttributes.Virtual;
                if (multicast) attributes |= MethodAttributes.Final;
                matched = count == 0 && Byte(ref blob, 8);
                break;
            case "NewMulticastDelegate":
                attributes |= count == 3 ? MethodAttributes.Private : MethodAttributes.Assembly;
                matched = count is 2 or 3 && Class(ref blob, reader, "System", "MulticastDelegate", definition)
                    && Byte(ref blob, 0x1D) && Byte(ref blob, 0x1C) && Byte(ref blob, 8)
                    && (count == 2 || Byte(ref blob, 2));
                break;
            case "CombineImpl" or "RemoveImpl":
                attributes |= MethodAttributes.Family | MethodAttributes.Final | MethodAttributes.Virtual;
                matched = count == 1 && Class(ref blob, reader, "System", "Delegate", definition)
                    && Class(ref blob, reader, "System", "Delegate", definition);
                break;
            case "GetTarget":
                attributes |= MethodAttributes.Assembly | MethodAttributes.Virtual | MethodAttributes.CheckAccessOnOverride;
                matched = count == 0 && Byte(ref blob, 0x1C);
                break;
            case "GetMethodImpl":
                attributes |= MethodAttributes.Family | MethodAttributes.Virtual;
                matched = count == 0 && Class(ref blob, reader, "System.Reflection", "MethodInfo", definition);
                break;
            case "GetInvocationList":
                attributes |= MethodAttributes.Public | MethodAttributes.Final | MethodAttributes.Virtual;
                matched = count == 0 && Byte(ref blob, 0x1D) && Class(ref blob, reader, "System", "Delegate", definition);
                break;
            case "get_HasSingleTarget":
                attributes |= MethodAttributes.Assembly | MethodAttributes.SpecialName;
                matched = count == 0 && Byte(ref blob, 2);
                break;
            case "TryGetAt":
                attributes |= MethodAttributes.Assembly;
                matched = count == 1 && Byte(ref blob, 0x1C) && Byte(ref blob, 8);
                break;
            default:
                attributes |= MethodAttributes.Private;
                matched = count == VmConstructorArity(name) && Byte(ref blob, 1) && Byte(ref blob, 0x1C);
                for (int i = 1; i < count && matched; i++) matched = Byte(ref blob, 0x18);
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
        if (!type.GetDeclaringType().IsNil || type.GetGenericParameters().Count != 0
            || (type.Attributes & (TypeAttributes.Abstract | TypeAttributes.Sealed | TypeAttributes.Interface)) != TypeAttributes.Abstract
            || !reader.StringComparer.Equals(type.Namespace, "System")
            || !reader.StringComparer.Equals(type.Name, owner == "System.Delegate" ? "Delegate" : "MulticastDelegate")
            || type.BaseType.Kind != HandleKind.TypeDefinition) return false;
        var parent = reader.GetTypeDefinition((TypeDefinitionHandle)type.BaseType);
        return parent.GetDeclaringType().IsNil && reader.StringComparer.Equals(parent.Namespace, "System")
            && reader.StringComparer.Equals(parent.Name, owner == "System.Delegate" ? "Object" : "Delegate")
            && reader.StringComparer.Equals(method.Name, name) && method.GetGenericParameters().Count == 0
            && method.RelativeVirtualAddress != 0 && method.ImplAttributes == MethodImplAttributes.IL
            && Signature(reader, owner, name, reader.GetBlobReader(method.Signature), true, out var attributes)
            && method.Attributes == attributes;
    }
}

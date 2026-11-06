using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using SRME = System.Reflection.Metadata.Ecma335.MetadataTokens;

namespace Dn2Cpp;

// Signature identity is shared with ILDiet; the selected methods keep their real IL.
internal static class PrimitiveParseContract
{
    internal enum Input { None, String, Chars, Bytes }
    internal static string? Owner(PrimitiveTypeCode primitive) => primitive switch
    {
        PrimitiveTypeCode.Char => "System.Char",
        PrimitiveTypeCode.IntPtr => "System.IntPtr",
        PrimitiveTypeCode.UIntPtr => "System.UIntPtr",
        _ => null,
    };
    internal static PrimitiveTypeCode Primitive(string owner) => owner switch
    {
        "System.Char" => PrimitiveTypeCode.Char,
        "System.IntPtr" => PrimitiveTypeCode.IntPtr,
        "System.UIntPtr" => PrimitiveTypeCode.UIntPtr,
        _ => PrimitiveTypeCode.Void,
    };
    internal static bool IsSlot(string name) => name is "Parse" or "TryParse";
    internal static bool IsFormatHelper(string owner, string name) => owner == "System.ThrowHelper"
        && name is "ThrowFormatException_NeedSingleChar" or "ThrowFormatInvalidString";
    internal static string CharName(bool styles, string name) =>
        (styles ? "System.Numerics.INumberBase<System.Char>." : "System.ISpanParsable<System.Char>.") + name;
    internal static bool IsCharNumber(string owner, string name) => owner == "System.Char"
        && (name == CharName(true, "Parse") || name == CharName(true, "TryParse"));

    internal static bool IsBodyCandidate(string owner, string name, MethodAttributes attributes, int parameterCount, bool stringInput) =>
        IsFormatHelper(owner, name) && parameterCount == 0 || owner == "System.Char"
        && (IsCharNumber(owner, name) && !stringInput
            || name == CharName(false, "Parse") || name == CharName(false, "TryParse")
            || IsSlot(name) && (attributes & MethodAttributes.MemberAccessMask) == MethodAttributes.Assembly);

    private static bool Named(MetadataReader reader, TypeDefinitionHandle handle, string ns, string name) =>
        BinaryIntegerEndianContract.IsNamedType(reader, handle, ns, name);

    private static bool ReadNamed(MetadataReader reader, ref BlobReader signature, byte kind, string ns, string name)
    {
        return signature.RemainingBytes >= 2 && signature.ReadByte() == kind
            && signature.TryReadCompressedInteger(out int coded) && (coded & 3) == 0
            && coded >> 2 > 0 && coded >> 2 <= reader.TypeDefinitions.Count
            && Named(reader, SRME.TypeDefinitionHandle(coded >> 2), ns, name);
    }

    private static bool ReadSelf(ref BlobReader signature, PrimitiveTypeCode primitive) => primitive == PrimitiveTypeCode.Void
        ? signature.RemainingBytes >= 2 && signature.ReadByte() == 0x13 && signature.ReadByte() == 0
        : signature.RemainingBytes >= 1 && signature.ReadByte() == (byte)primitive;

    internal static bool TrySignature(MetadataReader reader, BlobReader signature, bool isTry,
        PrimitiveTypeCode primitive, bool styles, bool provider, out Input input)
    {
        input = Input.None;
        int count = 1 + (styles ? 1 : 0) + (provider ? 1 : 0) + (isTry ? 1 : 0);
        if (signature.RemainingBytes < 3 || signature.ReadByte() != 0 || signature.ReadByte() != count
            || !(isTry ? signature.ReadByte() == 2 : ReadSelf(ref signature, primitive))
            || signature.RemainingBytes == 0) return false;
        byte first = signature.ReadByte();
        if (first == 0x0e) input = Input.String;
        else
        {
            if (first != 0x15 || !ReadNamed(reader, ref signature, 0x11, "System", "ReadOnlySpan`1")
                || signature.RemainingBytes < 2 || signature.ReadByte() != 1) return false;
            input = signature.ReadByte() switch { 3 => Input.Chars, 5 => Input.Bytes, _ => Input.None };
            if (input == Input.None) return false;
        }
        return (!styles || ReadNamed(reader, ref signature, 0x11, "System.Globalization", "NumberStyles"))
            && (!provider || ReadNamed(reader, ref signature, 0x12, "System", "IFormatProvider"))
            && (!isTry || signature.RemainingBytes >= 2 && signature.ReadByte() == 0x10
                && ReadSelf(ref signature, primitive)) && signature.RemainingBytes == 0;
    }

    // Same-name MethodImpl siblings are authenticated, not mistaken for duplicates.
    internal static int ImplementationParent(MetadataReader reader, EntityHandle parent, PrimitiveTypeCode primitive,
        out TypeDefinitionHandle contract)
    {
        contract = default;
        if (parent.Kind != HandleKind.TypeSpecification) return 0;
        var sig = reader.GetBlobReader(reader.GetTypeSpecification((TypeSpecificationHandle)parent).Signature);
        if (sig.RemainingBytes < 5 || sig.ReadByte() != 0x15 || sig.ReadByte() != 0x12
            || !sig.TryReadCompressedInteger(out int coded) || (coded & 3) != 0 || coded >> 2 <= 0
            || coded >> 2 > reader.TypeDefinitions.Count || sig.RemainingBytes != 2
            || sig.ReadByte() != 1 || sig.ReadByte() != (byte)primitive) return 0;
        contract = SRME.TypeDefinitionHandle(coded >> 2);
        if (reader.GetTypeDefinition(contract).GetGenericParameters().Count != 1) return 0;
        if (Named(reader, contract, "System.Numerics", "INumberBase`1")) return 1;
        if (Named(reader, contract, "System", "ISpanParsable`1")) return 2;
        if (Named(reader, contract, "System", "IParsable`1")) return 3;
        return Named(reader, contract, "System", "IUtf8SpanParsable`1") ? 4 : 0;
    }

    internal static bool IsTargetDefinition(MetadataReader reader, MethodDefinitionHandle handle,
        string owner, bool isTry, Input input, bool styles, bool provider, bool internalChar = false)
    {
        var primitive = Primitive(owner);
        if (handle.IsNil || primitive == PrimitiveTypeCode.Void || !reader.IsAssembly
            || !reader.StringComparer.Equals(reader.GetAssemblyDefinition().Name, "System.Private.CoreLib")) return false;
        var method = reader.GetMethodDefinition(handle);
        var type = reader.GetTypeDefinition(method.GetDeclaringType());
        bool character = primitive == PrimitiveTypeCode.Char;
        string name = isTry ? "TryParse" : "Parse";
        if (character && !internalChar) name = CharName(styles, name);
        var access = internalChar ? MethodAttributes.Assembly : character ? MethodAttributes.Private : MethodAttributes.Public;
        return Named(reader, method.GetDeclaringType(), "System", owner.Substring(7))
            && type.GetGenericParameters().Count == 0
            && (type.Attributes & (TypeAttributes.Interface | TypeAttributes.Abstract | TypeAttributes.Sealed)) == TypeAttributes.Sealed
            && type.BaseType.Kind == HandleKind.TypeDefinition && Named(reader, (TypeDefinitionHandle)type.BaseType, "System", "ValueType")
            && reader.StringComparer.Equals(method.Name, name) && method.GetGenericParameters().Count == 0
            && method.Attributes == (access | MethodAttributes.Static | MethodAttributes.HideBySig)
            && method.ImplAttributes == MethodImplAttributes.IL && method.RelativeVirtualAddress != 0
            && TrySignature(reader, reader.GetBlobReader(method.Signature), isTry, primitive, styles, provider, out var actual)
            && actual == input;
    }

    internal static bool IsBodyDefinition(MetadataReader reader, PEReader pe, MethodDefinitionHandle handle,
        string owner, string name)
    {
        if (IsFormatHelper(owner, name)) return IsFormatDefinition(reader, pe, handle, name);
        if (owner != "System.Char" || handle.IsNil) return false;
        bool inner = IsSlot(name);
        bool styles = name == CharName(true, "Parse") || name == CharName(true, "TryParse");
        bool isTry = name == "TryParse" || name == CharName(true, "TryParse") || name == CharName(false, "TryParse");
        return (inner || styles || name == CharName(false, "Parse") || name == CharName(false, "TryParse"))
            && IsTargetDefinition(reader, handle, owner, isTry, Input.Chars, styles, !inner, inner);
    }

    private static bool IsFormatDefinition(MetadataReader reader, PEReader pe, MethodDefinitionHandle handle, string name)
    {
        if (handle.IsNil || !reader.IsAssembly
            || !reader.StringComparer.Equals(reader.GetAssemblyDefinition().Name, "System.Private.CoreLib")) return false;
        var method = reader.GetMethodDefinition(handle);
        var signature = reader.GetBlobReader(method.Signature);
        if (!Named(reader, method.GetDeclaringType(), "System", "ThrowHelper")
            || reader.GetTypeDefinition(method.GetDeclaringType()).GetGenericParameters().Count != 0
            || !reader.StringComparer.Equals(method.Name, name) || method.GetGenericParameters().Count != 0
            || method.Attributes != (MethodAttributes.Assembly | MethodAttributes.Static | MethodAttributes.HideBySig)
            || method.ImplAttributes != MethodImplAttributes.IL || method.RelativeVirtualAddress == 0
            || signature.RemainingBytes != 3 || signature.ReadByte() != 0 || signature.ReadByte() != 0 || signature.ReadByte() != 1)
            return false;
        var body = pe.GetMethodBody(method.RelativeVirtualAddress);
        var il = body.GetILReader();
        if (!body.LocalSignature.IsNil || body.ExceptionRegions.Length != 0 || il.Length != 11 || il.ReadByte() != 0x28)
            return false;
        int getterToken = il.ReadInt32();
        if (il.ReadByte() != 0x73) return false;
        int ctorToken = il.ReadInt32();
        if (il.ReadByte() != 0x7a || !MethodToken(reader, getterToken) || !MethodToken(reader, ctorToken)) return false;
        var getter = reader.GetMethodDefinition(SRME.MethodDefinitionHandle(getterToken & 0xffffff));
        var gs = reader.GetBlobReader(getter.Signature);
        string key = name == "ThrowFormatInvalidString" ? "Format_InvalidString" : "Format_NeedSingleChar";
        if (!Named(reader, getter.GetDeclaringType(), "System", "SR")
            || reader.GetTypeDefinition(getter.GetDeclaringType()).GetGenericParameters().Count != 0
            || !reader.StringComparer.Equals(getter.Name, "get_" + key) || getter.GetGenericParameters().Count != 0
            || getter.Attributes != (MethodAttributes.Assembly | MethodAttributes.Static | MethodAttributes.HideBySig | MethodAttributes.SpecialName)
            || getter.ImplAttributes != MethodImplAttributes.IL || getter.RelativeVirtualAddress == 0
            || gs.RemainingBytes != 3 || gs.ReadByte() != 0 || gs.ReadByte() != 0 || gs.ReadByte() != 0x0e) return false;
        var ctor = reader.GetMethodDefinition(SRME.MethodDefinitionHandle(ctorToken & 0xffffff));
        var cs = reader.GetBlobReader(ctor.Signature);
        return Named(reader, ctor.GetDeclaringType(), "System", "FormatException")
            && reader.GetTypeDefinition(ctor.GetDeclaringType()).GetGenericParameters().Count == 0
            && reader.StringComparer.Equals(ctor.Name, ".ctor") && ctor.GetGenericParameters().Count == 0
            && ctor.Attributes == (MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName)
            && ctor.ImplAttributes == MethodImplAttributes.IL && ctor.RelativeVirtualAddress != 0
            && cs.RemainingBytes == 4 && cs.ReadByte() == 0x20 && cs.ReadByte() == 1 && cs.ReadByte() == 1 && cs.ReadByte() == 0x0e;
    }

    private static bool MethodToken(MetadataReader reader, int token) => (uint)token >> 24 == 6
        && (token & 0xffffff) > 0 && (token & 0xffffff) <= reader.MethodDefinitions.Count;
}

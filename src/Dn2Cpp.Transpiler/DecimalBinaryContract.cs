using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using SRME = System.Reflection.Metadata.Ecma335.MetadataTokens;

namespace Dn2Cpp;

// Decimal's wire words are lo/mid/hi/flags, not its native field layout.
// These four definitions retain their original reads, writes and exception order.
internal static class DecimalBinaryContract
{
    internal static int ParameterCount(string owner, string name) => (owner, name) switch
    {
        ("System.Decimal", "ToDecimal") => 1,
        ("System.Decimal", "GetBytes") => 2,
        ("System.ThrowHelper", "ThrowEndOfFileException" or "CreateEndOfFileException") => 0,
        _ => -1,
    };

    private static bool Named(MetadataReader reader, TypeDefinitionHandle type, string ns, string name) =>
        BinaryIntegerEndianContract.IsNamedType(reader, type, ns, name);

    private static bool Byte(ref BlobReader blob, byte value) => blob.RemainingBytes != 0 && blob.ReadByte() == value;

    private static bool NamedType(MetadataReader reader, ref BlobReader blob, byte kind, string ns, string name) =>
        Byte(ref blob, kind) && blob.TryReadCompressedInteger(out int coded) && (coded & 3) == 0
        && coded >> 2 > 0 && coded >> 2 <= reader.TypeDefinitions.Count
        && Named(reader, SRME.TypeDefinitionHandle(coded >> 2), ns, name);

    private static bool ByteSpan(MetadataReader reader, ref BlobReader blob, bool readOnly) =>
        Byte(ref blob, 0x15) && NamedType(reader, ref blob, 0x11, "System", readOnly ? "ReadOnlySpan`1" : "Span`1")
        && Byte(ref blob, 1) && Byte(ref blob, 5);

    private static bool Declaration(MetadataReader reader, MethodDefinitionHandle handle, string ns, string owner,
        string name, MethodAttributes attributes)
    {
        if (handle.IsNil || !reader.IsAssembly || !reader.StringComparer.Equals(reader.GetAssemblyDefinition().Name, "System.Private.CoreLib"))
            return false;
        var method = reader.GetMethodDefinition(handle);
        return Named(reader, method.GetDeclaringType(), ns, owner)
            && reader.GetTypeDefinition(method.GetDeclaringType()).GetGenericParameters().Count == 0
            && reader.StringComparer.Equals(method.Name, name) && method.GetGenericParameters().Count == 0
            && method.Attributes == attributes && method.ImplAttributes == MethodImplAttributes.IL && method.RelativeVirtualAddress != 0;
    }

    internal static bool IsConstructor(MetadataReader reader, MethodDefinitionHandle handle)
    {
        if (!Declaration(reader, handle, "System", "Decimal", ".ctor",
                MethodAttributes.Private | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName)) return false;
        var blob = reader.GetBlobReader(reader.GetMethodDefinition(handle).Signature);
        return Byte(ref blob, 0x20) && Byte(ref blob, 4) && Byte(ref blob, 1)
            && Byte(ref blob, 8) && Byte(ref blob, 8) && Byte(ref blob, 8) && Byte(ref blob, 8) && blob.RemainingBytes == 0;
    }

    internal static bool IsBodyDefinition(MetadataReader reader, PEReader pe, MethodDefinitionHandle handle, string owner, string name)
    {
        int count = ParameterCount(owner, name);
        if (count < 0 || !Declaration(reader, handle, "System", owner.Substring(7), name,
                MethodAttributes.Assembly | MethodAttributes.Static | MethodAttributes.HideBySig)) return false;
        var method = reader.GetMethodDefinition(handle);
        var blob = reader.GetBlobReader(method.Signature);
        if (!Byte(ref blob, 0) || !Byte(ref blob, (byte)count)) return false;
        if (owner == "System.Decimal")
        {
            bool read = name == "ToDecimal";
            return (read ? NamedType(reader, ref blob, 0x11, "System", "Decimal") : Byte(ref blob, 1))
                && (read || Byte(ref blob, 0x10) && NamedType(reader, ref blob, 0x11, "System", "Decimal"))
                && ByteSpan(reader, ref blob, read) && blob.RemainingBytes == 0;
        }
        bool factory = name == "CreateEndOfFileException";
        if (!(factory ? NamedType(reader, ref blob, 0x12, "System", "Exception") : Byte(ref blob, 1)) || blob.RemainingBytes != 0)
            return false;
        var body = pe.GetMethodBody(method.RelativeVirtualAddress);
        var il = body.GetILReader();
        if (!body.LocalSignature.IsNil || body.ExceptionRegions.Length != 0 || il.Length != (factory ? 11 : 6)
            || !Byte(ref il, 0x28) || !MethodToken(reader, il.ReadInt32(), out var called)) return false;
        if (!factory)
            return Byte(ref il, 0x7a) && IsBodyDefinition(reader, pe, called, "System.ThrowHelper", "CreateEndOfFileException");
        if (!Byte(ref il, 0x73) || !MethodToken(reader, il.ReadInt32(), out var ctor) || !Byte(ref il, 0x2a)
            || !Declaration(reader, called, "System", "SR", "get_IO_EOF_ReadBeyondEOF",
                MethodAttributes.Assembly | MethodAttributes.Static | MethodAttributes.HideBySig | MethodAttributes.SpecialName)
            || !Declaration(reader, ctor, "System.IO", "EndOfStreamException", ".ctor",
                MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName)) return false;
        var gs = reader.GetBlobReader(reader.GetMethodDefinition(called).Signature);
        var cs = reader.GetBlobReader(reader.GetMethodDefinition(ctor).Signature);
        return Byte(ref gs, 0) && Byte(ref gs, 0) && Byte(ref gs, 0x0e) && gs.RemainingBytes == 0
            && Byte(ref cs, 0x20) && Byte(ref cs, 1) && Byte(ref cs, 1) && Byte(ref cs, 0x0e) && cs.RemainingBytes == 0;
    }

    private static bool MethodToken(MetadataReader reader, int token, out MethodDefinitionHandle handle)
    {
        handle = default;
        if ((uint)token >> 24 != 6 || (token & 0xffffff) == 0 || (token & 0xffffff) > reader.MethodDefinitions.Count) return false;
        handle = SRME.MethodDefinitionHandle(token & 0xffffff);
        return true;
    }
}

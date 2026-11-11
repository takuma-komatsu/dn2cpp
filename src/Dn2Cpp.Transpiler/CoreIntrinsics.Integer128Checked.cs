using System.Reflection.Metadata;

namespace Dn2Cpp;

internal static partial class CoreIntrinsics
{
    internal static bool IsInteger128CheckedInterfaceDefinition(ClassInfo cls)
    {
        if (cls.Module.AssemblyName != "System.Private.CoreLib" || cls.Handle.IsNil) return false;
        var definition = cls.Module.Reader.GetTypeDefinition(cls.Handle);
        return definition.GetDeclaringType().IsNil
            && cls.Module.Reader.StringComparer.Equals(definition.Namespace, "System.Numerics")
            && cls.Module.Reader.StringComparer.Equals(definition.Name, "INumberBase`1");
    }

    private static bool Integer128CheckedByte(ref BlobReader blob, byte value) =>
        blob.RemainingBytes != 0 && blob.ReadByte() == value;

    internal static bool IsInteger128CheckedSignature(MetadataReader reader, BlobReader blob, bool interfaceForm = false)
    {
        if (!Integer128CheckedByte(ref blob, 0x10) || !Integer128CheckedByte(ref blob, 1)
            || !Integer128CheckedByte(ref blob, 1)) return false;
        if (interfaceForm)
        {
            if (!Integer128CheckedByte(ref blob, 0x13) || !Integer128CheckedByte(ref blob, 0)) return false;
        }
        else if (!Integer128CheckedByte(ref blob, 0x11) || !Integer128CheckedTypeToken(reader, ref blob)) return false;
        return Integer128CheckedByte(ref blob, 0x1e) && Integer128CheckedByte(ref blob, 0) && blob.RemainingBytes == 0;
    }

    internal static bool IsInteger128CheckedInstantiation(MetadataReader reader, BlobHandle signature)
    {
        var blob = reader.GetBlobReader(signature);
        return Integer128CheckedByte(ref blob, 0x0a) && Integer128CheckedByte(ref blob, 1)
            && Integer128CheckedArgument(reader, ref blob) && blob.RemainingBytes == 0;
    }

    internal static bool IsInteger128CheckedOwnerSignature(MetadataReader reader, BlobHandle signature)
    {
        var blob = reader.GetBlobReader(signature);
        return Integer128CheckedByte(ref blob, 0x15) && Integer128CheckedByte(ref blob, 0x12)
            && Integer128CheckedTypeToken(reader, ref blob) && Integer128CheckedByte(ref blob, 1)
            && Integer128CheckedArgument(reader, ref blob) && blob.RemainingBytes == 0;
    }

    internal static bool IsInteger128CheckedTypeSignature(MetadataReader reader, BlobHandle signature)
    {
        var blob = reader.GetBlobReader(signature);
        return Integer128CheckedArgument(reader, ref blob) && blob.RemainingBytes == 0;
    }

    // Decoding erases modifiers and pinned wrappers. Only the finite source
    // encodings and caller substitutions can authorize these checked adapters.
    private static bool Integer128CheckedArgument(MetadataReader reader, ref BlobReader blob)
    {
        if (blob.RemainingBytes == 0) return false;
        byte kind = blob.ReadByte();
        if (kind is >= 3 and <= 0x0b or 0x18 or 0x19) return true;
        if (kind is 0x13 or 0x1e) return blob.TryReadCompressedInteger(out _);
        return kind == 0x11 && Integer128CheckedTypeToken(reader, ref blob);
    }

    private static bool Integer128CheckedTypeToken(MetadataReader reader, ref BlobReader blob)
    {
        if (!blob.TryReadCompressedInteger(out int token) || (token >> 2) == 0) return false;
        return (token & 3) == 0 ? (token >> 2) <= reader.TypeDefinitions.Count
            : (token & 3) == 1 && (token >> 2) <= reader.TypeReferences.Count;
    }
}

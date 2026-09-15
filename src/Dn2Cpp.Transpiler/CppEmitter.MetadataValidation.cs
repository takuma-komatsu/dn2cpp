using System.Text;

namespace Dn2Cpp;

internal sealed partial class CppEmitter
{
    private static ulong ReadMetadataUnsigned(IReadOnlyList<byte> bytes, ref int position, int limit)
    {
        ulong result = 0;
        for (int shift = 0; shift < 70; shift += 7)
        {
            if (position >= limit)
                throw new InvalidOperationException("Truncated metadata integer.");
            byte value = bytes[position++];
            if (shift == 63 && (value & 0xfe) != 0)
                throw new InvalidOperationException("Metadata integer exceeds UInt64.");
            result |= (ulong)(value & 0x7f) << shift;
            if ((value & 0x80) == 0)
                return result;
        }
        throw new InvalidOperationException("Unterminated metadata integer.");
    }

    private static long ReadMetadataSigned(ulong value) =>
        (value & 1) == 0 ? (long)(value / 2) : -1 - (long)(value / 2);

    // Validate while the source row and its pool are still live; no decoded program
    // graph survives the streaming metadata block.
    private void ValidateMetadataRow(string rowType, MetadataValue[] source, MetadataBlock block,
        IReadOnlyList<byte> bytes, int start)
    {
        int position = start;
        ulong blockId = ReadMetadataUnsigned(bytes, ref position, bytes.Count);
        ulong length = ReadMetadataUnsigned(bytes, ref position, bytes.Count);
        if (blockId != (ulong)block.Id || length != (ulong)(bytes.Count - start) || (length & 1) != 0)
            throw new InvalidOperationException("Invalid metadata record header.");
        int end = start + (int)length;
        ulong presence = ReadMetadataUnsigned(bytes, ref position, end);
        if (source.Length < 64 && (presence >> source.Length) != 0)
            throw new InvalidOperationException("Metadata presence mask exceeds its schema.");
        var values = new ulong[source.Length];
        for (int field = 0; field < source.Length; field++)
            if ((presence & (1UL << field)) != 0)
                values[field] = ReadMetadataUnsigned(bytes, ref position, end);
        if (end - position > 1 || (position < end && bytes[position] != 0))
            throw new InvalidOperationException("Metadata record has an invalid tail.");
        int derivedField = rowType == "Dn2CppFieldInfo" ? 3 : rowType == "Dn2CppMethodInfo" ? 5 : -1;
        long derivedAttrs = 0;
        if (derivedField >= 0 && (presence & (1UL << derivedField)) == 0)
        {
            long attributes = ReadMetadataSigned(values[rowType == "Dn2CppFieldInfo" ? 8 : 12]);
            long access = attributes & 7;
            derivedAttrs = ((attributes & 0x10) != 0 ? 1 : 0) | (access == 6 ? 2 : access == 1 ? 4 : 0);
            if (rowType == "Dn2CppFieldInfo")
                derivedAttrs += ((attributes & 0x20) != 0 ? 8 : 0) | ((attributes & 0x40) != 0 ? 16 : 0);
            else
                derivedAttrs += ((attributes & 0x800) != 0 ? 0x20 : 0) | (ReadMetadataSigned(values[15]) > 0 ? 0x40 : 0);
        }
        for (int field = 0; field < source.Length; field++)
        {
            MetadataValue expected = source[field];
            ulong value = values[field];
            bool present = (presence & (1UL << field)) != 0;
            bool derived = field == derivedField && !present;
            if (expected.Present && !present && !derived)
                throw new InvalidOperationException("Metadata lost an explicit value.");
            if (expected.StringValue is { } text)
            {
                if (value == 0 || value > (ulong)block.Pointers.Count)
                    throw new InvalidOperationException("Metadata string has an invalid pool index.");
                string reference = block.Pointers[(int)value - 1];
                string prefix = expected.IsDisplay ? "md_display_" : "md_name_";
                if (!reference.StartsWith(prefix, System.StringComparison.Ordinal)
                    || !int.TryParse(reference.Substring(prefix.Length), out int id))
                    throw new InvalidOperationException("Metadata string has the wrong pool.");
                var strings = expected.IsDisplay ? _metadataDisplays : _metadataNames;
                if (id < 0 || id >= strings.Count || strings[id] != text)
                    throw new InvalidOperationException("Metadata changed a string value.");
            }
            else if (expected.Pointer is { } pointer)
            {
                if (value == 0 || value > (ulong)block.Pointers.Count
                    || block.Pointers[(int)value - 1] != MetadataPointer(pointer))
                    throw new InvalidOperationException("Metadata changed a pointer value.");
            }
            else if (expected.IsSigned)
            {
                if ((derived ? derivedAttrs : ReadMetadataSigned(value)) != expected.SignedNumber)
                    throw new InvalidOperationException("Metadata changed a signed value.");
            }
            else if (value != expected.Encoded)
                throw new InvalidOperationException("Metadata changed an unsigned or null value.");
        }
    }
    private static void ValidateMetadataDisplay(IReadOnlyList<byte> bytes, int blockId,
        IReadOnlyList<string> tokens, string expected)
    {
        int position = 1;
        if (bytes.Count == 0 || bytes[0] != 0xff
            || ReadMetadataUnsigned(bytes, ref position, bytes.Count) != (ulong)blockId)
            throw new InvalidOperationException("Invalid metadata display header.");
        ulong count = ReadMetadataUnsigned(bytes, ref position, bytes.Count);
        var decoded = new StringBuilder();
        for (ulong i = 0; i < count; i++)
        {
            ulong id = ReadMetadataUnsigned(bytes, ref position, bytes.Count);
            if (id >= (ulong)tokens.Count)
                throw new InvalidOperationException("Metadata display has an invalid token.");
            decoded.Append(tokens[(int)id]);
        }
        if (position != bytes.Count || decoded.ToString() != expected)
            throw new InvalidOperationException("Metadata changed a display string.");
    }

    private static void ValidateMetadataNames(IReadOnlyList<string> names, long[] offsets, IReadOnlyList<int> roots, long size)
    {
        var bytes = new List<byte>();
        foreach (int root in roots)
        {
            bytes.AddRange(Encoding.UTF8.GetBytes(names[root]));
            bytes.Add(0);
        }
        if (bytes.Count != size)
            throw new InvalidOperationException("Metadata string pool has the wrong size.");
        for (int i = 0; i < names.Count; i++)
        {
            byte[] expected = Encoding.UTF8.GetBytes(names[i]);
            long offset = offsets[i];
            if (offset < 0 || offset + expected.Length >= bytes.Count || bytes[(int)offset + expected.Length] != 0)
                throw new InvalidOperationException("Metadata name has an invalid offset.");
            for (int j = 0; j < expected.Length; j++)
                if (bytes[(int)offset + j] != expected[j])
                    throw new InvalidOperationException("Metadata changed a UTF-8 name.");
        }
    }

}

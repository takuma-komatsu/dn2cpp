using System.Text;

namespace Dn2Cpp;

internal sealed partial class CppEmitter
{
    private readonly struct MetadataValue
    {
        internal readonly string? Pointer;
        internal readonly ulong Encoded;
        internal readonly bool Present;
        internal readonly string? StringValue;
        internal readonly bool IsDisplay;
        internal readonly bool IsSigned;
        internal readonly long SignedNumber;

        private MetadataValue(string? pointer, ulong encoded, bool present = false, string? text = null, bool display = false, bool isSigned = false, long signedNumber = 0)
        {
            Pointer = pointer;
            Encoded = encoded;
            Present = present;
            StringValue = text;
            IsDisplay = display;
            IsSigned = isSigned;
            SignedNumber = signedNumber;
        }

        internal static MetadataValue Ref(string? expression) =>
            new(expression is null or "nullptr" ? null : expression, 0);
        internal static MetadataValue Signed(long value) => new(null, unchecked((ulong)(value << 1) ^ (ulong)(value >> 63)), isSigned: true, signedNumber: value);
        internal static MetadataValue ExplicitSigned(long value) => new(null, unchecked((ulong)(value << 1) ^ (ulong)(value >> 63)), true, isSigned: true, signedNumber: value);
        internal static MetadataValue Unsigned(ulong value) => new(null, value);
        internal static MetadataValue Text(string? text) => new(null, 0, text: text);
        internal static MetadataValue Display(string? text) => new(null, 0, text: text, display: true);
    }

    private sealed class MetadataRow
    {
        internal readonly MetadataValue[] Values;

        internal MetadataRow(MetadataValue[] values) => Values = values;
    }

    private static string MetadataRowsKey(IReadOnlyList<MetadataRow> rows)
    {
        var key = new StringBuilder();
        foreach (MetadataRow record in rows)
        {
            MetadataValue[] row = record.Values;
            key.Append(row.Length).Append(':');
            foreach (MetadataValue value in row)
            {
                string pointer = value.Pointer ?? value.StringValue ?? "";
                key.Append(pointer.Length).Append(':').Append(pointer).Append(':').Append(value.Encoded).Append(value.Present ? '!' : ';').Append(value.StringValue is not null ? (value.IsDisplay ? 'd' : 's') : 'p');
            }
        }
        return key.ToString();
    }

    private static int MetadataMemberAttrs(int ilAttrs) =>
        ((ilAttrs & 0x10) != 0 ? 1 : 0) | ((ilAttrs & 7) == 6 ? 2 : (ilAttrs & 7) == 1 ? 4 : 0);

    private static int MetadataFieldAttrs(int ilAttrs) => MetadataMemberAttrs(ilAttrs)
        | ((ilAttrs & 0x20) != 0 ? 8 : 0) | ((ilAttrs & 0x40) != 0 ? 16 : 0);

    private sealed class MetadataBlock
    {
        internal readonly int Id;
        internal readonly Dictionary<string, ulong> PointerIndices = new(System.StringComparer.Ordinal);
        internal readonly List<string> Pointers = new();

        internal MetadataBlock(int id) => Id = id;
    }

    private MetadataBlock? _metadataRootBlock;
    private MetadataBlock? _metadataActiveBlock;
    private int _metadataBlockCount;
    private readonly Dictionary<string, string[]> _metadataRecordAddresses = new(System.StringComparer.Ordinal);

    private MetadataBlock RootMetadataBlock => _metadataRootBlock ??= new MetadataBlock(_metadataBlockCount++);

    private void BeginMetadataBlock()
    {
        _ = RootMetadataBlock;
        _metadataActiveBlock = new MetadataBlock(_metadataBlockCount++);
    }

    private void EndMetadataBlock(StringBuilder sb)
    {
        if (_metadataActiveBlock is not { } block)
            throw new InvalidOperationException("No active metadata block.");
        EmitMetadataPointers(sb, block);
        _metadataActiveBlock = null;
    }

    private void EmitMetadataPointers(StringBuilder sb, MetadataBlock block)
    {
        string symbol = "md_ptr_" + block.Id;
        _metadataHeader.AppendLine($"extern const void* const {symbol}[];");
        var values = new List<string>();
        foreach (string pointer in block.Pointers)
            values.Add("(const void*)(" + pointer + ")");
        if (values.Count == 0)
            values.Add("nullptr");
        sb.AppendLine($"extern const void* const {symbol}[] = {{ {string.Join(", ", values)} }};");
    }

    private void FinishMetadataEncoding(StringBuilder sb)
    {
        FinishMetadataStrings(sb);
        EmitMetadataPointers(sb, RootMetadataBlock);
        sb.AppendLine("const Dn2CppMetadataBlock dn2cpp_metadata_blocks[] = {");
        for (int i = 0; i < _metadataBlockCount; i++)
            sb.AppendLine(i == _metadataDisplayBlock
                ? "    { nullptr, md_display_tokens },"
                : $"    {{ md_ptr_{i}, nullptr }},");
        sb.AppendLine("};");
    }

    private string MetadataPointer(string expression)
    {
        if (_metadataRecordAddresses.TryGetValue(expression, out var rows))
            return rows[0];
        return expression;
    }

    private static void WriteMetadataUnsigned(List<byte> output, ulong value)
    {
        do
        {
            byte part = (byte)(value & 0x7f);
            value >>= 7;
            output.Add((byte)(part | (value != 0 ? 0x80 : 0)));
        } while (value != 0);
    }

    private static int MetadataUnsignedLength(ulong value)
    {
        int length = 1;
        while (value >= 0x80)
        {
            value >>= 7;
            length++;
        }
        return length;
    }

    private void EmitMetadataTable(StringBuilder sb, string rowType, string symbol,
        IReadOnlyList<MetadataRow> rows, bool external = false)
    {
        if (rows.Count == 0)
            throw new InvalidOperationException("An empty metadata table has no address.");
        MetadataBlock block = _metadataActiveBlock ?? RootMetadataBlock;
        var bytes = new List<byte>();
        var addresses = new string[rows.Count];
        string recordSymbol = "md_record_" + symbol;
        for (int rowIndex = 0; rowIndex < rows.Count; rowIndex++)
        {
            MetadataValue[] row = rows[rowIndex].Values;
            if (row.Length > 64)
                throw new InvalidOperationException("Metadata schema exceeds its presence mask.");
            addresses[rowIndex] = recordSymbol + " + " + bytes.Count + " + 1";
            ulong presence = 0;
            var payload = new List<byte>();
            for (int field = 0; field < row.Length; field++)
            {
                MetadataValue value = row[field];
                if (rowType == "Dn2CppFieldInfo" && field == 3)
                {
                    int ilAttrs = (int)((long)(row[8].Encoded >> 1) ^ -((long)row[8].Encoded & 1));
                    if (value.Encoded == MetadataValue.Signed(MetadataFieldAttrs(ilAttrs)).Encoded)
                        continue;
                }
                if (rowType == "Dn2CppMethodInfo" && field == 5)
                {
                    int ilAttrs = (int)((long)(row[12].Encoded >> 1) ^ -((long)row[12].Encoded & 1));
                    int derived = MetadataMemberAttrs(ilAttrs) | ((ilAttrs & 0x800) != 0 ? 0x20 : 0)
                        | (row[15].Encoded != 0 ? 0x40 : 0);
                    if (value.Encoded == MetadataValue.Signed(derived).Encoded)
                        continue;
                }
                ulong encoded = value.Encoded;
                string? pointer = value.StringValue is { } text ? (value.IsDisplay ? InternMetadataDisplay(text) : InternMetadataName(text)) : value.Pointer;
                if (pointer is { } reference)
                {
                    reference = MetadataPointer(reference);
                    if (!block.PointerIndices.TryGetValue(reference, out encoded))
                    {
                        encoded = (ulong)block.Pointers.Count + 1;
                        block.Pointers.Add(reference);
                        block.PointerIndices[reference] = encoded;
                    }
                }
                if (encoded == 0 && !value.Present)
                    continue;
                presence |= 1UL << field;
                WriteMetadataUnsigned(payload, encoded);
            }
            int bodyLength = MetadataUnsignedLength((ulong)block.Id) + MetadataUnsignedLength(presence) + payload.Count;
            int totalLength = bodyLength + 1;
            while (true)
            {
                int next = bodyLength + MetadataUnsignedLength((ulong)totalLength);
                next += next & 1;
                if (next == totalLength)
                    break;
                totalLength = next;
            }
            int start = bytes.Count;
            WriteMetadataUnsigned(bytes, (ulong)block.Id);
            WriteMetadataUnsigned(bytes, (ulong)totalLength);
            WriteMetadataUnsigned(bytes, presence);
            bytes.AddRange(payload);
            while (bytes.Count - start < totalLength)
                bytes.Add(0);
            ValidateMetadataRow(rowType, row, block, bytes, start);
        }
        _metadataRecordAddresses[symbol] = addresses;
        if (external)
            _metadataHeader.AppendLine($"extern const uint8_t {recordSymbol}[{bytes.Count}];");
        sb.Append($"alignas(2) {(external ? "extern" : "static")} const uint8_t {recordSymbol}[] = {{ ");
        for (int i = 0; i < bytes.Count; i++)
        {
            if (i != 0) sb.Append(", ");
            sb.Append(bytes[i]);
        }
        sb.AppendLine(" };");
        (external ? _metadataHeader : sb).AppendLine($"[[maybe_unused]] static constexpr auto {symbol} = Dn2CppMetadataTable<{rowType}>::from_static({recordSymbol});");
    }

    private string MetadataRowAddress(string symbol, int row) => _metadataRecordAddresses[symbol][row];
}

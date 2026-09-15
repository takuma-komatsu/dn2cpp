using System.Text;

namespace Dn2Cpp;

internal sealed partial class CppEmitter
{
    private readonly Dictionary<string, int> _metadataNameIds = new(System.StringComparer.Ordinal);
    private readonly List<string> _metadataNames = new();
    private readonly Dictionary<string, int> _metadataDisplayIds = new(System.StringComparer.Ordinal);
    private readonly List<string> _metadataDisplays = new();
    private int _metadataDisplayBlock = -1;

    private string InternMetadataName(string text)
    {
        if (!_metadataNameIds.TryGetValue(text, out int id))
        {
            id = _metadataNames.Count;
            _metadataNames.Add(text);
            _metadataNameIds[text] = id;
        }
        return "md_name_" + id;
    }

    private string InternMetadataDisplay(string text)
    {
        if (!_metadataDisplayIds.TryGetValue(text, out int id))
        {
            id = _metadataDisplays.Count;
            _metadataDisplays.Add(text);
            _metadataDisplayIds[text] = id;
        }
        return "md_display_" + id;
    }

    private static (long[] Offsets, List<int> Roots, long Bytes) MetadataNameLayout(IReadOnlyList<string> names)
    {
        var reversed = new string[names.Count];
        for (int i = 0; i < names.Count; i++)
        {
            char[] chars = names[i].ToCharArray();
            Array.Reverse(chars);
            reversed[i] = new string(chars);
        }
        var order = Enumerable.Range(0, names.Count).OrderBy(i => reversed[i], System.StringComparer.Ordinal).ToArray();
        var offsets = new long[names.Count];
        var roots = new List<int>();
        long bytes = 0;
        int previous = -1;
        for (int i = order.Length - 1; i >= 0; i--)
        {
            int index = order[i];
            if (previous >= 0 && names[previous].EndsWith(names[index], System.StringComparison.Ordinal))
                offsets[index] = offsets[previous] + Encoding.UTF8.GetByteCount(names[previous]) - Encoding.UTF8.GetByteCount(names[index]);
            else
            {
                offsets[index] = bytes;
                bytes += Encoding.UTF8.GetByteCount(names[index]) + 1L;
                roots.Add(index);
            }
            previous = index;
        }
        return (offsets, roots, bytes);
    }

    private static List<string> MetadataDisplayTokens(string display)
    {
        var tokens = new List<string>();
        int start = 0;
        for (int i = 0; i < display.Length; i++)
        {
            char ch = display[i];
            if (ch is not (' ' or ',' or '(' or ')' or '[' or ']' or '&' or '*'))
                continue;
            if (i > start)
                tokens.Add(display.Substring(start, i - start));
            tokens.Add(display.Substring(i, 1));
            start = i + 1;
        }
        if (start < display.Length)
            tokens.Add(display.Substring(start));
        return tokens;
    }

    private sealed class MetadataDisplayRow
    {
        internal readonly int[] Tokens;

        internal MetadataDisplayRow(int[] tokens) => Tokens = tokens;
    }

    private void FinishMetadataStrings(StringBuilder sb)
    {
        var tokenIds = new Dictionary<string, int>(System.StringComparer.Ordinal);
        var tokens = new List<string>();
        var tokenRows = new List<MetadataDisplayRow>();
        var compressed = new bool[_metadataDisplays.Count];
        int dictionaryBlock = _metadataBlockCount;
        for (int i = 0; i < _metadataDisplays.Count; i++)
        {
            var words = MetadataDisplayTokens(_metadataDisplays[i]);
            var row = new int[words.Count];
            int cost = 1 + MetadataUnsignedLength((ulong)dictionaryBlock) + MetadataUnsignedLength((ulong)row.Length);
            for (int j = 0; j < words.Count; j++)
            {
                if (!tokenIds.TryGetValue(words[j], out int id))
                {
                    id = tokens.Count;
                    tokenIds[words[j]] = id;
                    tokens.Add(words[j]);
                }
                row[j] = id;
                cost += MetadataUnsignedLength((ulong)id);
            }
            tokenRows.Add(new MetadataDisplayRow(row));
            compressed[i] = cost < Encoding.UTF8.GetByteCount(_metadataDisplays[i]) + 1;
        }
        // The dictionary is charged in full, including tokens from rejected rows.
        // Charge pointers plus relocation entries as well as UTF-8 suffix storage.
        var rawNames = _metadataNames.Concat(_metadataDisplays).Distinct(System.StringComparer.Ordinal).ToArray();
        var packedNames = _metadataNames.Concat(tokens)
            .Concat(_metadataDisplays.Where((_, index) => !compressed[index]))
            .Distinct(System.StringComparer.Ordinal).ToArray();
        long packedBytes = MetadataNameLayout(packedNames).Bytes + tokens.Count * 32L + 48;
        for (int i = 0; i < compressed.Length; i++)
            if (compressed[i])
                packedBytes += 1 + MetadataUnsignedLength((ulong)dictionaryBlock)
                    + MetadataUnsignedLength((ulong)tokenRows[i].Tokens.Length)
                    + tokenRows[i].Tokens.Sum(id => MetadataUnsignedLength((ulong)id));
        bool useDictionary = compressed.Any(value => value) && packedBytes < MetadataNameLayout(rawNames).Bytes;
        if (useDictionary)
        {
            _metadataDisplayBlock = _metadataBlockCount++;
            var pointers = new List<string>();
            foreach (string token in tokens)
                pointers.Add(InternMetadataName(token));
            sb.AppendLine($"static const char* const md_display_tokens[] = {{ {string.Join(", ", pointers)} }};");
        }
        for (int i = 0; i < _metadataDisplays.Count; i++)
        {
            if (useDictionary && compressed[i])
            {
                var bytes = new List<byte> { 0xff };
                WriteMetadataUnsigned(bytes, (ulong)_metadataDisplayBlock);
                WriteMetadataUnsigned(bytes, (ulong)tokenRows[i].Tokens.Length);
                foreach (int id in tokenRows[i].Tokens)
                    WriteMetadataUnsigned(bytes, (ulong)id);
                ValidateMetadataDisplay(bytes, _metadataDisplayBlock, tokens, _metadataDisplays[i]);
                _metadataHeader.AppendLine($"extern const char md_display_{i}[{bytes.Count}];");
                sb.Append($"extern const char md_display_{i}[] = {{ ");
                for (int j = 0; j < bytes.Count; j++)
                {
                    if (j != 0) sb.Append(", ");
                    sb.Append("(char)").Append(bytes[j]);
                }
                sb.AppendLine(" };");
            }
            else
                _metadataHeader.AppendLine($"[[maybe_unused]] static constexpr const char* md_display_{i} = {InternMetadataName(_metadataDisplays[i])};");
        }
        var layout = MetadataNameLayout(_metadataNames);
        ValidateMetadataNames(_metadataNames, layout.Offsets, layout.Roots, layout.Bytes);
        var aliases = new StringBuilder();
        aliases.AppendLine($"extern const char md_name_pool[{Math.Max(layout.Bytes, 1)}];");
        for (int i = 0; i < _metadataNames.Count; i++)
            aliases.AppendLine($"[[maybe_unused]] static constexpr const char* md_name_{i} = md_name_pool + {layout.Offsets[i]};");
        _metadataHeader.Insert(_metadataStringHeaderOffset, aliases.ToString());
        sb.AppendLine("extern const char md_name_pool[] =");
        if (layout.Roots.Count == 0)
            sb.AppendLine("    \"\"");
        for (int i = 0; i < layout.Roots.Count; i++)
            sb.AppendLine("    \"" + CLiteral(_metadataNames[layout.Roots[i]])
                + (i + 1 < layout.Roots.Count ? "\\0" : "") + "\"");
        sb.AppendLine(";");
    }

    private int _metadataStringHeaderOffset;
}

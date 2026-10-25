using System.Globalization;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Text;

namespace Dn2Cpp;

internal sealed class TypeNameSnapshot
{
    internal const string CommentPrefix = "<!--dn2cpp-type-names-v1:";
    private const string Header = "dn2cpp-type-names-v1";

    internal sealed class Source
    {
        internal required string Assembly;
        internal readonly List<(string Name, uint Hash)> Entries = new();
        internal readonly List<(string Name, string Target)> Forwarders = new();
    }

    private readonly Dictionary<string, Source> _sources = new(StringComparer.OrdinalIgnoreCase);

    internal bool TryGet(string assembly, out Source source) => _sources.TryGetValue(assembly, out source!);

    internal void AddMissing(string assembly, string path)
    {
        if (!_sources.ContainsKey(assembly))
            _sources.Add(assembly, ReadSource(assembly, path));
    }

    internal static TypeNameSnapshot Capture(IReadOnlyList<(string Assembly, string Path)> loaded)
    {
        var snapshot = new TypeNameSnapshot();
        var candidates = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (assembly, path) in loaded)
        {
            var source = ReadSource(assembly, path);
            snapshot._sources.Add(assembly, source);
            foreach (var entry in source.Entries)
                candidates.Add(entry.Name);
        }

        string? corePath = loaded.FirstOrDefault(item =>
            string.Equals(item.Assembly, "System.Private.CoreLib", StringComparison.OrdinalIgnoreCase)).Path;
        string? directory = corePath is null ? null : Path.GetDirectoryName(corePath);
        var pending = new Queue<string>(loaded.Select(item => item.Assembly)
            .Concat(new[] { "mscorlib", "netstandard", "System.Runtime" })
            .Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(name => name, StringComparer.Ordinal));
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        while (pending.Count != 0)
        {
            string assembly = pending.Dequeue();
            if (!visited.Add(assembly))
                continue;
            bool alreadyLoaded = snapshot._sources.TryGetValue(assembly, out var source);
            if (!alreadyLoaded)
            {
                string? path = directory is null ? null : Path.Combine(directory, assembly + ".dll");
                if (path is null || !File.Exists(path))
                    continue;
                source = ReadSource(assembly, path);
            }
            bool relevant = false;
            foreach (var row in source!.Forwarders)
            {
                if (!candidates.Contains(row.Name))
                    continue;
                relevant = true;
                pending.Enqueue(row.Target);
            }
            if (!alreadyLoaded && relevant)
                snapshot._sources.Add(assembly, source);
        }
        return snapshot;
    }

    internal string Comment()
    {
        var text = new StringBuilder(Header).Append('\n');
        foreach (var source in _sources.Values.OrderBy(item => item.Assembly, StringComparer.Ordinal))
        {
            text.Append("A\t").Append(Encode(source.Assembly)).Append('\n');
            foreach (var row in source.Entries)
                text.Append("T\t").Append(Encode(row.Name)).Append('\t')
                    .Append(row.Hash.ToString("x8", CultureInfo.InvariantCulture)).Append('\n');
            foreach (var row in source.Forwarders)
                text.Append("F\t").Append(Encode(row.Name)).Append('\t')
                    .Append(Encode(row.Target)).Append('\n');
            text.Append("E\n");
        }
        return CommentPrefix + Convert.ToBase64String(Encoding.UTF8.GetBytes(text.ToString())) + "-->\n";
    }

    internal static TypeNameSnapshot? FromPreservation(string path)
    {
        if (!File.Exists(path))
            return null;
        string preservation = File.ReadAllText(path);
        int start = preservation.IndexOf(CommentPrefix, StringComparison.Ordinal);
        if (start < 0)
            return null;
        start += CommentPrefix.Length;
        int end = preservation.IndexOf("-->", start, StringComparison.Ordinal);
        if (end < 0)
            throw new InvalidOperationException("Unterminated ILDiet type-name metadata: " + path);
        string encoded = preservation.Substring(start, end - start);
        string decoded = Encoding.UTF8.GetString(Convert.FromBase64String(encoded));
        string[] lines = decoded.Split('\n');
        if (lines.Length == 0 || lines[0] != Header)
            throw new InvalidOperationException("Invalid ILDiet type-name metadata: " + path);
        var snapshot = new TypeNameSnapshot();
        Source? current = null;
        for (int i = 1; i < lines.Length; i++)
        {
            if (lines[i].Length == 0)
                continue;
            string[] fields = lines[i].Split('\t');
            if (fields[0] == "A" && fields.Length == 2 && current is null)
            {
                current = new Source { Assembly = Decode(fields[1]) };
                snapshot._sources.Add(current.Assembly, current);
            }
            else if (fields[0] == "T" && fields.Length == 3 && current is not null)
                current.Entries.Add((Decode(fields[1]), uint.Parse(fields[2], NumberStyles.HexNumber,
                    CultureInfo.InvariantCulture)));
            else if (fields[0] == "F" && fields.Length == 3 && current is not null)
                current.Forwarders.Add((Decode(fields[1]), Decode(fields[2])));
            else if (fields[0] == "E" && fields.Length == 1 && current is not null)
                current = null;
            else
                throw new InvalidOperationException("Invalid ILDiet type-name row: " + path);
        }
        if (current is not null)
            throw new InvalidOperationException("Incomplete ILDiet type-name metadata: " + path);
        return snapshot;
    }

    private static string Encode(string value) => Convert.ToBase64String(Encoding.UTF8.GetBytes(value));
    private static string Decode(string value) => Encoding.UTF8.GetString(Convert.FromBase64String(value));

    private static uint NameHash(string ns, string name, uint enclosingHash)
    {
        uint hash = 5381;
        foreach (byte value in Encoding.UTF8.GetBytes(ns))
            hash = unchecked(hash * 33u ^ value);
        foreach (byte value in Encoding.UTF8.GetBytes(name))
            hash = unchecked(hash * 33u ^ value);
        if (enclosingHash != 0)
            hash = unchecked(hash * 33u ^ enclosingHash);
        return hash;
    }

    private static Source ReadSource(string assembly, string path)
    {
        using var stream = File.OpenRead(path);
        using var pe = new PEReader(stream);
        var reader = pe.GetMetadataReader();
        var source = new Source { Assembly = assembly };
        var definitions = new Dictionary<TypeDefinitionHandle, (string Name, uint Hash)>();
        (string Name, uint Hash) Definition(TypeDefinitionHandle handle)
        {
            if (definitions.TryGetValue(handle, out var known))
                return known;
            var row = reader.GetTypeDefinition(handle);
            string simple = reader.GetString(row.Name);
            string ns = reader.GetString(row.Namespace);
            var parent = row.GetDeclaringType();
            var value = parent.IsNil
                ? (ns.Length == 0 ? simple : ns + "." + simple, NameHash(ns, simple, 0))
                : (Definition(parent).Name + "+" + simple,
                    NameHash(ns, simple, Definition(parent).Hash));
            definitions.Add(handle, value);
            return value;
        }
        var exports = new Dictionary<ExportedTypeHandle, (string Name, uint Hash)>();
        (string Name, uint Hash) Exported(ExportedTypeHandle handle)
        {
            if (exports.TryGetValue(handle, out var known))
                return known;
            var row = reader.GetExportedType(handle);
            string simple = reader.GetString(row.Name);
            string ns = reader.GetString(row.Namespace);
            var parent = row.Implementation;
            var value = parent.Kind == HandleKind.ExportedType
                ? (Exported((ExportedTypeHandle)parent).Name + "+" + simple,
                    NameHash(ns, simple, Exported((ExportedTypeHandle)parent).Hash))
                : (ns.Length == 0 ? simple : ns + "." + simple, NameHash(ns, simple, 0));
            exports.Add(handle, value);
            return value;
        }
        string? Target(ExportedTypeHandle handle)
        {
            var implementation = reader.GetExportedType(handle).Implementation;
            if (implementation.Kind == HandleKind.ExportedType)
                return Target((ExportedTypeHandle)implementation);
            return implementation.Kind == HandleKind.AssemblyReference
                ? reader.GetString(reader.GetAssemblyReference((AssemblyReferenceHandle)implementation).Name)
                : null;
        }
        foreach (var handle in reader.TypeDefinitions)
            if (reader.GetString(reader.GetTypeDefinition(handle).Name) != "<Module>")
                source.Entries.Add(Definition(handle));
        foreach (var handle in reader.ExportedTypes)
            if (!reader.GetExportedType(handle).Implementation.IsNil)
            {
                source.Entries.Add(Exported(handle));
                if (Target(handle) is { } target)
                    source.Forwarders.Add((Exported(handle).Name, target));
            }
        return source;
    }
}

using System.Globalization;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Text;

namespace Dn2Cpp;

internal sealed class TypeNameSnapshot
{
    internal const string CommentPrefix = "<!--dn2cpp-type-names-v2:";
    private const string LegacyCommentPrefix = "<!--dn2cpp-type-names-v1:";
    private const string Header = "dn2cpp-type-names-v2";
    internal const int DefaultPublicCtor = 0x1, DefaultNonPublicCtor = 0x2,
        DeclaredPublicCctor = 0x10, DeclaredNonPublicCctor = 0x20,
        PubCctorBeforePubCtor = 0x40,
        BodilessPublicCtor = 0x80, BodilessNonPublicCtor = 0x100,
        MultiplePublicDefaultCtor = 0x200, MultipleNonPublicDefaultCtor = 0x400,
        MultiplePublicCctor = 0x800, MultipleNonPublicCctor = 0x1000,
        PubCctorBeforeNonPubCtor = 0x2000, NonPubCctorBeforePubCtor = 0x4000,
        NonPubCctorBeforeNonPubCtor = 0x8000,
        ModifiedPublicZeroArgCtor = 0x10000, ModifiedNonPublicZeroArgCtor = 0x20000,
        FirstPubCtorVarArg = 0x40000, FirstNonPubCtorVarArg = 0x80000,
        NonPubCtorBeforePubCtor = 0x100000, InvalidCctor = 0x200000,
        // Snapshot-only marker (never emitted into runtime DN2CPP_TA_*): ILDiet
        // removed the pre-strip loader-selected bare void () instance .ctor.
        StrippedSelectedDefaultCtor = 0x400000;

    internal sealed class Source
    {
        internal required string Assembly;
        internal readonly List<(string Name, uint Hash)> Entries = new();
        internal readonly List<(string Name, string Target)> Forwarders = new();
        internal readonly Dictionary<string, int> Initializers = new(StringComparer.Ordinal);
    }

    private readonly Dictionary<string, Source> _sources = new(StringComparer.OrdinalIgnoreCase);

    internal bool TryGet(string assembly, out Source source) => _sources.TryGetValue(assembly, out source!);

    internal void MarkStrippedSelectedDefaultCtor(string assembly, string rawTypeDefKey)
    {
        if (!_sources.TryGetValue(assembly, out var source))
            return;
        source.Initializers.TryGetValue(rawTypeDefKey, out int bits);
        source.Initializers[rawTypeDefKey] = bits | StrippedSelectedDefaultCtor;
    }

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
            foreach (var row in source.Initializers.OrderBy(item => item.Key, StringComparer.Ordinal))
                text.Append("I\t").Append(Encode(row.Key)).Append('\t')
                    .Append(row.Value.ToString("x", CultureInfo.InvariantCulture)).Append('\n');
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
        {
            if (preservation.IndexOf(LegacyCommentPrefix, StringComparison.Ordinal) >= 0)
                throw new InvalidOperationException(
                    "Unsupported ILDiet type-name metadata version (v1); regenerate preservation metadata: " + path);
            return null;
        }
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
            else if (fields[0] == "I" && fields.Length == 3 && current is not null)
                current.Initializers[Decode(fields[1])] = int.Parse(fields[2], NumberStyles.HexNumber,
                    CultureInfo.InvariantCulture);
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

    internal static string RawTypeDefKey(MetadataReader reader, TypeDefinitionHandle handle)
    {
        var row = reader.GetTypeDefinition(handle);
        string ns = reader.GetString(row.Namespace);
        string simple = reader.GetString(row.Name);
        string part = ns.Length.ToString(CultureInfo.InvariantCulture) + ":" + ns + ":"
            + simple.Length.ToString(CultureInfo.InvariantCulture) + ":" + simple;
        var parent = row.GetDeclaringType();
        return parent.IsNil ? part : RawTypeDefKey(reader, parent) + "/" + part;
    }

    // ECMA-335 bare instance void () signature: HASTHIS|DEFAULT (0x20), 0 params (0x00), ELEMENT_TYPE_VOID (0x01).
    internal static bool IsBareDefaultCtorSignature(BlobReader signature) =>
        signature.Length == 3
        && signature.ReadByte() == 0x20
        && signature.ReadByte() == 0x00
        && signature.ReadByte() == 0x01;

    // ECMA-335 bare static void () signature: DEFAULT (0x00), 0 params (0x00), ELEMENT_TYPE_VOID (0x01).
    internal static bool IsBareStaticCctorSignature(BlobReader signature) =>
        signature.Length == 3
        && signature.ReadByte() == 0x00
        && signature.ReadByte() == 0x00
        && signature.ReadByte() == 0x01;

    // CoreCLR's loader selects the last bare void () instance .ctor in MethodDef order.
    internal static MethodDefinitionHandle LastBareDefaultCtorHandle(MetadataReader reader, TypeDefinitionHandle handle)
    {
        MethodDefinitionHandle last = default;
        foreach (var methodHandle in reader.GetTypeDefinition(handle).GetMethods())
        {
            var md = reader.GetMethodDefinition(methodHandle);
            if ((md.Attributes & MethodAttributes.Static) != 0
                || (md.Attributes & MethodAttributes.RTSpecialName) == 0
                || !reader.StringComparer.Equals(md.Name, ".ctor"))
                continue;
            if (IsBareDefaultCtorSignature(reader.GetBlobReader(md.Signature)))
                last = methodHandle;
        }
        return last;
    }

    // Raw declaration order, RVA state and multiplicity of 0-arg RTSpecialName
    // .ctor/.cctor rows; independent of body retention.
    internal static int ReadActivationDeclarations(MetadataReader reader, TypeDefinitionHandle handle)
    {
        int bits = 0;
        int publicCtorCount = 0;
        int nonPublicCtorCount = 0;
        int publicCctorCount = 0;
        int nonPublicCctorCount = 0;
        int firstPubCtor = int.MaxValue;
        int firstNonPubCtor = int.MaxValue;
        int firstPubCctor = int.MaxValue;
        int firstNonPubCctor = int.MaxValue;
        bool hasLastBare = false;
        bool lastBarePublic = false;
        bool lastBareBodiless = false;
        int ordinal = 0;
        foreach (var methodHandle in reader.GetTypeDefinition(handle).GetMethods())
        {
            var md = reader.GetMethodDefinition(methodHandle);
            if ((md.Attributes & MethodAttributes.RTSpecialName) == 0)
                continue;
            bool isStatic = (md.Attributes & MethodAttributes.Static) != 0;
            bool isCctor = isStatic && reader.StringComparer.Equals(md.Name, ".cctor");
            bool isCtor = !isStatic && reader.StringComparer.Equals(md.Name, ".ctor");
            if (!isCctor && !isCtor)
                continue;
            bool isPublic = (md.Attributes & MethodAttributes.MemberAccessMask) == MethodAttributes.Public;
            if (isCctor)
            {
                if (!IsBareStaticCctorSignature(reader.GetBlobReader(md.Signature)))
                {
                    bits |= InvalidCctor;
                    continue;
                }
                int pos = ordinal++;
                if (isPublic)
                {
                    if (firstPubCctor == int.MaxValue)
                        firstPubCctor = pos;
                    bits |= DeclaredPublicCctor;
                    if (++publicCctorCount >= 2)
                        bits |= MultiplePublicCctor;
                }
                else
                {
                    if (firstNonPubCctor == int.MaxValue)
                        firstNonPubCctor = pos;
                    bits |= DeclaredNonPublicCctor;
                    if (++nonPublicCctorCount >= 2)
                        bits |= MultipleNonPublicCctor;
                }
            }
            else
            {
                var blob = reader.GetBlobReader(md.Signature);
                var header = blob.ReadSignatureHeader();
                if (header.IsGeneric || blob.ReadCompressedInteger() != 0)
                    continue;
                int pos = ordinal++;
                bool isVarArg = header.CallingConvention == SignatureCallingConvention.VarArgs;
                bool isBare = IsBareDefaultCtorSignature(reader.GetBlobReader(md.Signature));
                bool bodiless = md.RelativeVirtualAddress == 0;
                if (isPublic)
                {
                    if (firstPubCtor == int.MaxValue)
                    {
                        firstPubCtor = pos;
                        if (isVarArg)
                            bits |= FirstPubCtorVarArg;
                    }
                    if (++publicCtorCount >= 2)
                        bits |= MultiplePublicDefaultCtor;
                }
                else
                {
                    if (firstNonPubCtor == int.MaxValue)
                    {
                        firstNonPubCtor = pos;
                        if (isVarArg)
                            bits |= FirstNonPubCtorVarArg;
                    }
                    if (++nonPublicCtorCount >= 2)
                        bits |= MultipleNonPublicDefaultCtor;
                }
                if (isBare)
                {
                    if (hasLastBare)
                        bits |= lastBarePublic ? ModifiedPublicZeroArgCtor : ModifiedNonPublicZeroArgCtor;
                    hasLastBare = true;
                    lastBarePublic = isPublic;
                    lastBareBodiless = bodiless;
                }
                else
                    bits |= isPublic ? ModifiedPublicZeroArgCtor : ModifiedNonPublicZeroArgCtor;
            }
        }
        if (hasLastBare)
            bits |= lastBarePublic
                ? (lastBareBodiless ? BodilessPublicCtor : DefaultPublicCtor)
                : (lastBareBodiless ? BodilessNonPublicCtor : DefaultNonPublicCtor);
        if (firstNonPubCtor < firstPubCtor && firstPubCtor < int.MaxValue)
            bits |= NonPubCtorBeforePubCtor;
        if (firstPubCctor < firstPubCtor && firstPubCtor < int.MaxValue)
            bits |= PubCctorBeforePubCtor;
        if (firstPubCctor < firstNonPubCtor && firstNonPubCtor < int.MaxValue)
            bits |= PubCctorBeforeNonPubCtor;
        if (firstNonPubCctor < firstPubCtor && firstPubCtor < int.MaxValue)
            bits |= NonPubCctorBeforePubCtor;
        if (firstNonPubCctor < firstNonPubCtor && firstNonPubCtor < int.MaxValue)
            bits |= NonPubCctorBeforeNonPubCtor;
        return bits;
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
            {
                var def = Definition(handle);
                source.Entries.Add(def);
                int bits = ReadActivationDeclarations(reader, handle);
                if (bits != 0)
                    source.Initializers[RawTypeDefKey(reader, handle)] = bits;
            }
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

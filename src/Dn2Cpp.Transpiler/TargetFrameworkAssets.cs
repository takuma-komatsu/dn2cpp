using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace Dn2Cpp;

// Target, not host, input classification. A namespace, simple assembly name,
// or the fact that ILDiet copied a file does not establish pack membership.
// Unknown or relocated packs without an inventory stay conservative.
internal static class TargetFrameworkAssets
{
    private const ulong CoreLibKey = 0x7cec85d7bea7798eUL;
    private const string CoreLib = "System.Private.CoreLib.dll";
    private const string PackageInventory = "dn2cpp-bcl-v1.tsv";
    // An installed pack is recognized for this framework version alone; its
    // catalogue schema and signing identities are the ones the readers below know.
    private const string FrameworkVersion = "11.0";
    private static StringComparison PathComparison => Path.DirectorySeparatorChar == '\\'
        ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

    internal static HashSet<int> Classify(IReadOnlyList<string> paths)
    {
        var result = new HashSet<int>();
        try
        {
            string? corePath = null;
            foreach (string path in paths)
            {
                string full = Path.GetFullPath(path);
                if (!Path.GetFileName(full).Equals(CoreLib, PathComparison)) continue;
                if (corePath is not null) return result;
                corePath = full;
            }
            if (corePath is null) return result;
            string root = Path.GetDirectoryName(corePath)!;
            string inventory = Path.Combine(root, PackageInventory);
            if (File.Exists(inventory) || Directory.Exists(inventory))
                ClassifyPackaged(paths, corePath, root, inventory, result);
            else
                ClassifyInstalled(paths, corePath, root, result);
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or BadImageFormatException or FormatException)
        {
            // A missing, unfamiliar, or unreadable catalogue is not authority to strip.
            result.Clear();
        }
        return result;
    }

    private static void ClassifyInstalled(IReadOnlyList<string> paths, string corePath, string root, HashSet<int> result)
    {
        string? parent = Path.GetDirectoryName(root);
        if (!string.Equals(Path.GetFileName(parent), "Microsoft.NETCore.App", PathComparison)
            || !string.Equals(Path.GetFileName(Path.GetDirectoryName(parent)), "shared", PathComparison)) return;
        string release = Path.GetFileName(root);
        const string stable = FrameworkVersion + ".";
        if (release.Length <= stable.Length || !release.StartsWith(stable, StringComparison.Ordinal)
            || release.Length > stable.Length + 1 && release[stable.Length] == '0') return;
        for (int i = stable.Length; i < release.Length; i++)
            if (release[i] is < '0' or > '9') return;
        string sidecar = Path.Combine(root, "Microsoft.NETCore.App.deps.json");
        if (!SizedCatalogue(sidecar)) return;
        var manifest = new ManifestReader(File.ReadAllText(sidecar)).Read();
        const string prefix = ".NETCoreApp,Version=v" + FrameworkVersion + "/";
        string? target = manifest.Object("runtimeTarget")?.String("name");
        if (target is null || !target.StartsWith(prefix, StringComparison.Ordinal)) return;
        string rid = target[prefix.Length..];
        if (rid.Length == 0) return;
        foreach (char c in rid)
            if (c is not (>= '0' and <= '9' or >= 'A' and <= 'Z' or >= 'a' and <= 'z' or '-' or '.'))
                return;
        string runtimePack = "Microsoft.NETCore.App.Runtime." + rid + "/" + release;
        var library = manifest.Object("libraries")?.Object(runtimePack);
        var assets = manifest.Object("targets")?.Object(target)?.Object(runtimePack)?.Object("runtime");
        if (library?.String("type") != "package" || library.Boolean("serviceable") != true
            || assets?.Object(CoreLib) is not { } coreAsset
            || !Matches(corePath, CoreLib, coreAsset)) return;
        foreach (var entry in assets.Fields)
        {
            if (!IsAssetName(entry.Key) || entry.Value is not ManifestObject asset)
                continue;
            int index = InputIndex(paths, root, entry.Key);
            if (index >= 0 && Matches(paths[index], entry.Key, asset))
                result.Add(index);
        }
    }

    private static void ClassifyPackaged(IReadOnlyList<string> paths, string corePath, string root, string inventory,
        HashSet<int> result)
    {
        if (!SizedCatalogue(inventory)) return;
        string[] lines = Encoding.UTF8.GetString(File.ReadAllBytes(inventory)).Split('\n');
        if (lines.Length < 3 || lines[0] != "dn2cpp-bcl-v1" || lines[^1].Length != 0) return;
        var entries = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        string previous = "";
        string? coreHash = null;
        for (int i = 1; i < lines.Length - 1; i++)
        {
            string row = lines[i];
            if (row.Length < 70 || row[64] != '\t') return;
            string name = row[65..];
            if (!IsAssetName(name) || string.CompareOrdinal(previous, name) >= 0) return;
            for (int j = 0; j < 64; j++)
                if (row[j] is not (>= '0' and <= '9' or >= 'a' and <= 'f')) return;
            string hash = row[..64];
            if (!entries.TryAdd(name, hash)) return;
            if (name == CoreLib) coreHash = hash;
            previous = name;
        }
        if (coreHash is null || !Matches(corePath, CoreLib, sha256: coreHash)) return;
        foreach (var entry in entries)
        {
            int index = InputIndex(paths, root, entry.Key);
            if (index >= 0 && Matches(paths[index], entry.Key, sha256: entry.Value))
                result.Add(index);
        }
    }

    private static bool SizedCatalogue(string path) => File.Exists(path) && new FileInfo(path).Length <= 1024 * 1024;

    private static bool IsAssetName(string name)
    {
        static bool Alnum(char c) => c is >= '0' and <= '9' or >= 'A' and <= 'Z' or >= 'a' and <= 'z';
        if (name.Length < 5 || !Alnum(name[0]) || !name.EndsWith(".dll", StringComparison.Ordinal)) return false;
        foreach (char c in name)
            if (!Alnum(c) && c is not ('_' or '.' or '-')) return false;
        return true;
    }

    private static int InputIndex(IReadOnlyList<string> paths, string directory, string name)
    {
        string expected = Path.Combine(directory, name);
        for (int i = 0; i < paths.Count; i++)
            if (Path.GetFullPath(paths[i]).Equals(expected, PathComparison))
                return i;
        return -1;
    }

    private static bool Matches(string path, string file, ManifestObject? asset = null, string? sha256 = null)
    {
        byte[] bytes = File.ReadAllBytes(path);
        if (sha256 is not null && Convert.ToHexStringLower(SHA256.HashData(bytes)) != sha256) return false;
        using var pe = new PEReader(ImmutableCollectionsMarshal.AsImmutableArray(bytes));
        if (!pe.HasMetadata || pe.PEHeaders.CorHeader is not { } header
            || (header.Flags & CorFlags.StrongNameSigned) == 0) return false;
        var reader = pe.GetMetadataReader();
        if (!reader.IsAssembly) return false;
        var definition = reader.GetAssemblyDefinition();
        if (!reader.StringComparer.Equals(definition.Name, file[..^4]) || !definition.Culture.IsNil
            || asset is not null && definition.Version.ToString() != asset.String("assemblyVersion")
            || (definition.Flags & AssemblyFlags.PublicKey) == 0) return false;
        // These are the signing identities used by the FrameworkVersion runtime pack.
        // A token is an assembly identity check; not signature authentication.
        ulong token = PublicKeyToken(reader, definition);
        if (file == CoreLib ? token != CoreLibKey
                : token is not (0xb03f5f7f11d50a3aUL or 0xcc7b13ffcd2ddd51UL
                    or 0xb77a5c561934e089UL or 0x31bf3856ad364e35UL
                    or 0xadb9793829ddae60UL)) return false;
        return asset is null
            || FileVersion(reader, definition) is { } version && version == asset.String("fileVersion");
    }

    internal static ulong PublicKeyToken(MetadataReader reader, AssemblyDefinition definition)
    {
        byte[] key = reader.GetBlobBytes(definition.PublicKey);
        if (key.Length == 0) return 0;
        byte[] hash = SHA1.HashData(key);
        ulong token = 0;
        for (int i = hash.Length - 1; i >= hash.Length - 8; i--)
            token = (token << 8) | hash[i];
        return token;
    }

    private static string? FileVersion(MetadataReader reader, AssemblyDefinition assembly)
    {
        string? version = null;
        foreach (var handle in assembly.GetCustomAttributes())
        {
            var attribute = reader.GetCustomAttribute(handle);
            EntityHandle owner = attribute.Constructor.Kind switch
            {
                HandleKind.MethodDefinition => reader.GetMethodDefinition(
                    (MethodDefinitionHandle)attribute.Constructor).GetDeclaringType(),
                HandleKind.MemberReference => reader.GetMemberReference(
                    (MemberReferenceHandle)attribute.Constructor).Parent,
                _ => default,
            };
            var (ns, name) = owner.Kind switch
            {
                HandleKind.TypeDefinition => (reader.GetTypeDefinition((TypeDefinitionHandle)owner).Namespace,
                    reader.GetTypeDefinition((TypeDefinitionHandle)owner).Name),
                HandleKind.TypeReference => (reader.GetTypeReference((TypeReferenceHandle)owner).Namespace,
                    reader.GetTypeReference((TypeReferenceHandle)owner).Name),
                _ => default,
            };
            if (!reader.StringComparer.Equals(ns, "System.Reflection")
                || !reader.StringComparer.Equals(name, "AssemblyFileVersionAttribute")) continue;
            var value = reader.GetBlobReader(attribute.Value);
            if (version is not null || value.ReadUInt16() != 1) return null;
            version = value.ReadSerializedString();
            if (value.RemainingBytes != 2 || value.ReadUInt16() != 0) return null;
        }
        return version;
    }

    private sealed class ManifestObject
    {
        internal readonly Dictionary<string, object?> Fields = new(StringComparer.Ordinal);
        internal ManifestObject? Object(string name) =>
            Fields.TryGetValue(name, out var value) ? value as ManifestObject : null;
        internal string? String(string name) =>
            Fields.TryGetValue(name, out var value) ? value as string : null;
        internal bool? Boolean(string name) =>
            Fields.TryGetValue(name, out var value) && value is bool flag ? flag : null;
    }

    // Read just the installed FrameworkVersion catalogue schema, not a new
    // JSON runtime dependency in the compiler. String arrays are other RIDs'
    // fallback lists.
    private sealed class ManifestReader(string text)
    {
        private int _at;

        internal ManifestObject Read()
        {
            var value = ReadObject(0);
            SkipSpace();
            if (_at != text.Length) throw Invalid();
            return value;
        }

        private ManifestObject ReadObject(int depth)
        {
            if (depth == 16 || !Take('{')) throw Invalid();
            var value = new ManifestObject();
            if (Take('}')) return value;
            do
            {
                string key = ReadString();
                if (!Take(':')) throw Invalid();
                object? item = Peek() switch
                {
                    '{' => ReadObject(depth + 1),
                    '[' => ReadStringArray(),
                    '"' => ReadString(),
                    't' when TakeLiteral("true") => true,
                    'f' when TakeLiteral("false") => false,
                    _ => throw Invalid(),
                };
                if (!value.Fields.TryAdd(key, item)) throw Invalid();
            } while (Take(','));
            if (!Take('}')) throw Invalid();
            return value;
        }

        private object? ReadStringArray()
        {
            if (!Take('[')) throw Invalid();
            if (Take(']')) return null;
            do { _ = ReadString(); } while (Take(','));
            if (!Take(']')) throw Invalid();
            return null;
        }

        private string ReadString()
        {
            if (!Take('"')) throw Invalid();
            var value = new StringBuilder();
            while (_at < text.Length)
            {
                char c = text[_at++];
                if (c == '"') return value.ToString();
                if (c < ' ') throw Invalid();
                if (c != '\\') { value.Append(c); continue; }
                if (_at == text.Length) throw Invalid();
                value.Append(text[_at++] switch
                {
                    '"' => '"', '\\' => '\\', '/' => '/', 'b' => '\b', 'f' => '\f',
                    'n' => '\n', 'r' => '\r', 't' => '\t', 'u' => ReadUnicode(),
                    _ => throw Invalid(),
                });
            }
            throw Invalid();
        }

        private char ReadUnicode()
        {
            if (_at + 4 > text.Length) throw Invalid();
            int value = 0;
            for (int i = 0; i < 4; i++)
            {
                char digit = text[_at++];
                value = (value << 4) + (digit switch
                {
                    >= '0' and <= '9' => digit - '0',
                    >= 'a' and <= 'f' => digit - 'a' + 10,
                    >= 'A' and <= 'F' => digit - 'A' + 10,
                    _ => throw Invalid(),
                });
            }
            return (char)value;
        }

        private void SkipSpace()
        {
            while (_at < text.Length && text[_at] is ' ' or '\t' or '\n' or '\r') _at++;
        }

        private char Peek()
        {
            SkipSpace();
            return _at < text.Length ? text[_at] : '\0';
        }

        private bool Take(char value)
        {
            if (Peek() != value) return false;
            _at++;
            return true;
        }

        private bool TakeLiteral(string value)
        {
            if (text.Length - _at < value.Length) return false;
            for (int i = 0; i < value.Length; i++)
                if (text[_at + i] != value[i]) return false;
            _at += value.Length;
            return true;
        }

        private static FormatException Invalid() => new("Not a .NET " + FrameworkVersion + " runtime catalogue");
    }
}

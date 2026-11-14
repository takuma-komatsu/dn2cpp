using System.Reflection.Metadata;

namespace Dn2Cpp;

// Assembly-scoped identity is shared by model resolution and raw reflection planning.
// The result is a metadata definition; choosing to decode or retain it belongs to the caller.
internal sealed class RawAssemblyTypeResolver<TModule> where TModule : notnull
{
    private readonly Dictionary<TModule, (string Name, MetadataReader Reader)> _modules = new();
    private readonly Dictionary<(string Assembly, string Namespace, string Name), (MetadataReader Reader, AssemblyReferenceHandle Scope)> _typeForwarders = new();
    private readonly Dictionary<string, string> _loadedAssemblyNames = new(StringComparer.OrdinalIgnoreCase);

    public RawAssemblyTypeResolver(IEnumerable<(TModule Module, string Name, MetadataReader Reader)> modules)
    {
        foreach (var (module, name, reader) in modules)
        {
            _modules.Add(module, (name, reader));
            _loadedAssemblyNames.TryAdd(name, name);
            foreach (var handle in reader.ExportedTypes)
            {
                var type = reader.GetExportedType(handle);
                if (type.Implementation.Kind != HandleKind.AssemblyReference) continue;
                _typeForwarders.TryAdd((name, reader.GetString(type.Namespace), reader.GetString(type.Name)),
                    (reader, (AssemblyReferenceHandle)type.Implementation));
            }
        }
    }

    public (TModule Module, TypeDefinitionHandle Handle)? Resolve(string assembly, string ns, string name,
        IEnumerable<(TModule Module, TypeDefinitionHandle Handle)> candidates, MetadataReader scopeReader, AssemblyReferenceHandle scope)
    {
        for (int hops = 0; hops <= _modules.Count; hops++)
        {
            if (_loadedAssemblyNames.TryGetValue(assembly, out var canonical)) assembly = canonical;
            foreach (var (module, handle) in candidates)
            {
                var info = _modules[module];
                if (info.Name.Equals(assembly, StringComparison.OrdinalIgnoreCase)
                    && info.Reader.GetTypeDefinition(handle).GetDeclaringType().IsNil) return (module, handle);
            }
            if (_typeForwarders.TryGetValue((assembly, ns, name), out var forwarded))
            {
                scopeReader = forwarded.Reader;
                scope = forwarded.Scope;
                assembly = scopeReader.GetString(scopeReader.GetAssemblyReference(scope).Name);
                continue;
            }
            // Signed facades can be omitted when their implementation is loaded.
            if (!_loadedAssemblyNames.ContainsKey(assembly)
                && IsSignedStandardFacadeScope(scopeReader, scope, assembly))
            {
                string target = IsPrivateUriFacadeType(scopeReader, scope, assembly, ns, name)
                    ? "System.Private.Uri" : "System.Private.CoreLib";
                foreach (var (module, handle) in candidates)
                {
                    var info = _modules[module];
                    if (info.Name == target && info.Reader.GetTypeDefinition(handle).GetDeclaringType().IsNil)
                        return (module, handle);
                }
            }
            return null;
        }
        return null;
    }

    private static bool IsPrivateUriFacadeType(MetadataReader reader, AssemblyReferenceHandle scope,
        string assembly, string ns, string name)
    {
        bool runtime = assembly.Equals("System.Runtime", StringComparison.OrdinalIgnoreCase);
        if (ns != "System" || (!runtime
            && !assembly.Equals("netstandard", StringComparison.OrdinalIgnoreCase)))
            return false;
        var reference = reader.GetAssemblyReference(scope);
        var token = reader.GetBlobReader(reference.PublicKeyOrToken);
        if (token.Length != 8 || token.ReadUInt64() != (runtime
            ? 0x3a0ad5117f5f3fb0UL : 0x51dd2dcdff137bccUL))
            return false;
        // These public forwarding rows share Private.Uri; nested rows resolve through their owner.
        return name is "FileStyleUriParser" or "FtpStyleUriParser" or "GenericUriParser"
            or "GenericUriParserOptions" or "GopherStyleUriParser" or "HttpStyleUriParser"
            or "LdapStyleUriParser" or "NetPipeStyleUriParser" or "NetTcpStyleUriParser"
            or "NewsStyleUriParser" or "Uri" or "UriBuilder" or "UriComponents"
            or "UriFormat" or "UriFormatException" or "UriHostNameType" or "UriKind"
            or "UriParser" or "UriPartial" || (runtime && name == "UriCreationOptions");
    }

    private static bool IsSignedStandardFacadeScope(MetadataReader reader, AssemblyReferenceHandle scope, string assembly)
    {
        if (!assembly.StartsWith("System.", StringComparison.OrdinalIgnoreCase)
            && !assembly.Equals("netstandard", StringComparison.OrdinalIgnoreCase)
            && !assembly.Equals("mscorlib", StringComparison.OrdinalIgnoreCase))
            return false;
        var reference = reader.GetAssemblyReference(scope);
        var token = reader.GetBlobReader(reference.PublicKeyOrToken);
        if (token.Length != 8)
            return false;
        return token.ReadUInt64() is 0x3a0ad5117f5f3fb0UL or 0x51dd2dcdff137bccUL
            or 0x8e79a7bed785ec7cUL or 0x89e03419565c7ab7UL;
    }

}

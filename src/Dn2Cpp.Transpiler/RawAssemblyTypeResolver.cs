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

    // A serialized assembly name has no signed TypeRef scope from which to infer a missing facade.
    public bool ForwardsTo(string assembly, string ns, string name, string definition)
    {
        for (int hops = 0; hops < _modules.Count; hops++)
        {
            if (!_loadedAssemblyNames.TryGetValue(assembly, out var canonical)
                || !_typeForwarders.TryGetValue((canonical, ns, name), out var forwarded)) return false;
            assembly = forwarded.Reader.GetString(forwarded.Reader.GetAssemblyReference(forwarded.Scope).Name);
            if (assembly.Equals(definition, StringComparison.OrdinalIgnoreCase)) return true;
        }
        return false;
    }

    public (TModule Module, TypeDefinitionHandle Type)? ResolveNamed(TModule source, string? assembly, string fullName,
        Func<string, string, IEnumerable<(TModule Module, TypeDefinitionHandle Handle)>> candidates)
    {
        if (fullName.IndexOfAny(['[', ']', '*', '&', '\\', ',']) >= 0) return null;
        string[] names = fullName.Split('+');
        int dot = names[0].LastIndexOf('.');
        string ns = dot < 0 ? "" : names[0][..dot];
        string name = names[0][(dot + 1)..];
        if (assembly is not null && assembly.Contains(','))
        {
            if (!AssemblyNameInfo.TryParse(assembly, out var requested)
                || !MatchesManifest(requested.Name, requested.Version, requested.CultureName ?? "")) return null;
            assembly = requested.Name;
        }
        if (assembly is not null) return InAssembly(assembly);
        return InAssembly(_modules[source].Name) ?? InAssembly("System.Private.CoreLib");

        (TModule Module, TypeDefinitionHandle Type)? InAssembly(string scope)
        {
            if (Resolve(scope, ns, name, candidates(ns, name), _modules[source].Reader, default, checkReferences: true) is not { } found) return null;
            var reader = _modules[found.Module].Reader;
            var type = found.Handle;
            for (int i = 1; i < names.Length; i++)
            {
                var nested = reader.GetTypeDefinition(type).GetNestedTypes();
                type = nested.FirstOrDefault(child => reader.StringComparer.Equals(reader.GetTypeDefinition(child).Name, names[i]));
                if (type.IsNil) return null;
            }
            return (found.Module, type);
        }
    }

    public (TModule Module, TypeDefinitionHandle Type)? ResolveType(TModule source, EntityHandle token,
        Func<string, string, IEnumerable<(TModule Module, TypeDefinitionHandle Handle)>> candidates, bool checkReferences)
    {
        if (token.Kind == HandleKind.TypeDefinition) return (source, (TypeDefinitionHandle)token);
        if (token.Kind != HandleKind.TypeReference) return null;
        var reader = _modules[source].Reader;
        var type = reader.GetTypeReference((TypeReferenceHandle)token);
        string ns = reader.GetString(type.Namespace), name = reader.GetString(type.Name);
        var scope = type.ResolutionScope;
        if (scope.Kind == HandleKind.TypeReference)
        {
            if (ResolveType(source, scope, candidates, checkReferences) is not { } parent) return null;
            var owner = _modules[parent.Module].Reader;
            foreach (var child in owner.GetTypeDefinition(parent.Type).GetNestedTypes())
            {
                var nested = owner.GetTypeDefinition(child);
                if (owner.StringComparer.Equals(nested.Name, name) && owner.StringComparer.Equals(nested.Namespace, ns))
                    return (parent.Module, child);
            }
            return null;
        }
        if (scope.Kind == HandleKind.AssemblyReference)
        {
            var reference = (AssemblyReferenceHandle)scope;
            return Resolve(reader.GetString(reader.GetAssemblyReference(reference).Name), ns, name,
                candidates(ns, name), reader, reference, checkReferences);
        }
        if (scope.Kind == HandleKind.ModuleDefinition || scope.IsNil)
            foreach (var (module, handle) in candidates(ns, name))
                if (_modules[module].Reader == reader && reader.GetTypeDefinition(handle).GetDeclaringType().IsNil)
                    return (module, handle);
        return null;
    }

    private bool MatchesManifest(string assembly, Version? minimum, string? culture)
    {
        MetadataReader? manifest = null;
        foreach (var (loaded, reader) in _modules.Values)
        {
            if (!loaded.Equals(assembly, StringComparison.OrdinalIgnoreCase)) continue;
            if (manifest is not null) return false;
            manifest = reader;
        }
        if (manifest is null) return false;
        var definition = manifest.GetAssemblyDefinition();
        return (minimum is null || definition.Version >= minimum)
            && (culture is null || string.Equals(culture, manifest.GetString(definition.Culture), StringComparison.OrdinalIgnoreCase));
    }

    private bool MatchesReference(string assembly, MetadataReader reader, AssemblyReferenceHandle scope)
    {
        if (scope.IsNil) return MatchesManifest(assembly, null, null);
        var reference = reader.GetAssemblyReference(scope);
        return MatchesManifest(assembly, reference.Version, reader.GetString(reference.Culture));
    }

    public (TModule Module, TypeDefinitionHandle Handle)? Resolve(string assembly, string ns, string name,
        IEnumerable<(TModule Module, TypeDefinitionHandle Handle)> candidates, MetadataReader scopeReader, AssemblyReferenceHandle scope,
        bool checkReferences = false)
    {
        for (int hops = 0; hops <= _modules.Count; hops++)
        {
            bool loaded = _loadedAssemblyNames.TryGetValue(assembly, out var canonical);
            if (loaded) assembly = canonical!;
            // A field address needs the actual target of every AssemblyRef, including exported-type forwarders.
            if (checkReferences && (loaded || hops != 0) && !MatchesReference(assembly, scopeReader, scope)) return null;
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
            if (!scope.IsNil && !loaded
                && IsSignedStandardFacadeScope(scopeReader, scope, assembly))
            {
                string target = IsPrivateUriFacadeType(scopeReader, scope, assembly, ns, name)
                    ? "System.Private.Uri" : "System.Private.CoreLib";
                if (checkReferences && !MatchesReference(target, scopeReader, scope)) return null;
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

using System.Reflection.Metadata;

namespace Dn2Cpp;

internal sealed partial class Compilation
{
    [Flags]
    private enum PreserveKind
    {
        None = 0,
        Type = 1,
        DefaultConstructor = 2,
        Fields = 4,
        Methods = 8,
        All = Type | Fields | Methods,
    }

    private sealed class PreservePolicy
    {
        public PreserveKind Kind;
        public readonly HashSet<FieldDefinitionHandle> Fields = new();
        public readonly HashSet<MethodDefinitionHandle> Methods = new();
        public readonly HashSet<PropertyDefinitionHandle> Properties = new();
        public readonly HashSet<EventDefinitionHandle> Events = new();
        public PreserveKind ConditionalKind;
        public readonly HashSet<FieldDefinitionHandle> ConditionalFields = new();
        public readonly HashSet<MethodDefinitionHandle> ConditionalMethods = new();
        public readonly HashSet<PropertyDefinitionHandle> ConditionalProperties = new();
        public readonly HashSet<EventDefinitionHandle> ConditionalEvents = new();
    }

    private readonly IReadOnlyList<string> _projectRoots;
    private readonly HashSet<string> _linkFeatures;
    private readonly Dictionary<(int Module, TypeDefinitionHandle Type), PreservePolicy> _preservePolicies = new();
    private readonly HashSet<ClassInfo> _explicitReflectionKeep = new();
    private readonly HashSet<ClassInfo> _activatedConditionalPolicies = new();
    private readonly Dictionary<(int Module, TypeDefinitionHandle Type), bool> _preserveAttributeTypes = new();
    private bool _preservationSeedingActive;

    private PreservePolicy Policy(Module module, TypeDefinitionHandle type)
    {
        var key = (module.Index, type);
        if (!_preservePolicies.TryGetValue(key, out var policy))
            _preservePolicies.Add(key, policy = new PreservePolicy());
        return policy;
    }

    private void DiscoverPreservationPolicies()
    {
        foreach (var module in Modules)
        {
            bool assemblyPreserved = HasPreserveAttribute(module,
                module.Reader.GetAssemblyDefinition().GetCustomAttributes());
            foreach (var tdh in module.Reader.TypeDefinitions)
            {
                var td = module.Reader.GetTypeDefinition(tdh);
                if (module.Reader.GetString(td.Name) == "<Module>")
                    continue;
                PreservePolicy? policy = null;
                if (assemblyPreserved || HasPreserveAttribute(module, td.GetCustomAttributes()))
                    (policy ??= Policy(module, tdh)).Kind |= PreserveKind.Type | PreserveKind.DefaultConstructor;
                foreach (var fdh in td.GetFields())
                    if (HasPreserveAttribute(module,
                        module.Reader.GetFieldDefinition(fdh).GetCustomAttributes()))
                        (policy ??= Policy(module, tdh)).Fields.Add(fdh);
                foreach (var mdh in td.GetMethods())
                    if (HasPreserveAttribute(module,
                        module.Reader.GetMethodDefinition(mdh).GetCustomAttributes()))
                        (policy ??= Policy(module, tdh)).Methods.Add(mdh);
                foreach (var ph in td.GetProperties())
                {
                    var pd = module.Reader.GetPropertyDefinition(ph);
                    if (!HasPreserveAttribute(module, pd.GetCustomAttributes()))
                        continue;
                    policy ??= Policy(module, tdh);
                    policy.Properties.Add(ph);
                    AddAccessors(policy, pd.GetAccessors());
                    AddBackingField(module, td, policy, module.Reader.GetString(pd.Name));
                }
                foreach (var eh in td.GetEvents())
                {
                    var ed = module.Reader.GetEventDefinition(eh);
                    if (!HasPreserveAttribute(module, ed.GetCustomAttributes()))
                        continue;
                    policy ??= Policy(module, tdh);
                    policy.Events.Add(eh);
                    AddAccessors(policy, ed.GetAccessors());
                    AddBackingField(module, td, policy, module.Reader.GetString(ed.Name));
                }
            }
        }
        foreach (string path in FindLinkFiles())
            ApplyLinkDocument(path, LinkXml.Parse(path));
    }

    private static void AddAccessors(PreservePolicy policy, PropertyAccessors accessors)
    {
        if (!accessors.Getter.IsNil) policy.Methods.Add(accessors.Getter);
        if (!accessors.Setter.IsNil) policy.Methods.Add(accessors.Setter);
    }

    private static void AddAccessors(PreservePolicy policy, EventAccessors accessors)
    {
        if (!accessors.Adder.IsNil) policy.Methods.Add(accessors.Adder);
        if (!accessors.Remover.IsNil) policy.Methods.Add(accessors.Remover);
    }

    private static void AddBackingField(Module module, TypeDefinition td,
        PreservePolicy policy, string name)
        => AddBackingField(module, td, policy.Fields, name);

    private static void AddBackingField(Module module, TypeDefinition td,
        HashSet<FieldDefinitionHandle> fields, string name)
    {
        string compilerName = "<" + name + ">k__BackingField";
        foreach (var fdh in td.GetFields())
        {
            string fieldName = module.Reader.GetString(module.Reader.GetFieldDefinition(fdh).Name);
            if (fieldName == name || fieldName == compilerName)
                fields.Add(fdh);
        }
    }

    private bool HasPreserveAttribute(Module module, CustomAttributeHandleCollection attributes)
    {
        foreach (var handle in attributes)
        {
            var attribute = module.Reader.GetCustomAttribute(handle);
            string? name = AttributeTypeName(module.Reader, attribute);
            if (name is null)
                continue;
            string simple = name.Substring(name.LastIndexOfAny(new[] { '.', '+' }) + 1);
            if (simple == "PreserveAttribute")
                return true;
            if (RawAttributeTypeDefinition(module, attribute) is { } type
                && IsRawPreserveAttributeType(type.Module, type.Handle)) return true;
        }
        return false;
    }

    private bool IsRawPreserveAttributeType(Module module, TypeDefinitionHandle handle)
    {
        var key = (module.Index, handle);
        if (_preserveAttributeTypes.TryGetValue(key, out bool cached)) return cached;
        var seen = new HashSet<(int, TypeDefinitionHandle)>();
        return _preserveAttributeTypes[key] = RawBaseIsPreserve(module, handle, seen);
    }

    private bool RawBaseIsPreserve(Module module, TypeDefinitionHandle handle,
        HashSet<(int, TypeDefinitionHandle)> seen)
    {
        if (!seen.Add((module.Index, handle))) return false;
        var td = module.Reader.GetTypeDefinition(handle);
        if (module.Reader.GetString(td.Name) == "PreserveAttribute") return true;
        if (td.BaseType.IsNil) return false;
        if (td.BaseType.Kind == HandleKind.TypeDefinition)
            return RawBaseIsPreserve(module, (TypeDefinitionHandle)td.BaseType, seen);
        if (td.BaseType.Kind != HandleKind.TypeReference) return false;
        string baseName = RawSignatureProvider.TypeReferenceName(module.Reader,
            (TypeReferenceHandle)td.BaseType);
        if (baseName.Substring(baseName.LastIndexOfAny(new[] { '.', '+' }) + 1) == "PreserveAttribute")
            return true;
        return RawTypeReferenceDefinition(module, (TypeReferenceHandle)td.BaseType) is { } type
            && RawBaseIsPreserve(type.Module, type.Handle, seen);
    }

    private (Module Module, TypeDefinitionHandle Handle)? RawAttributeTypeDefinition(
        Module module, CustomAttribute attribute)
    {
        EntityHandle parent = attribute.Constructor.Kind switch
        {
            HandleKind.MethodDefinition => module.Reader.GetMethodDefinition(
                (MethodDefinitionHandle)attribute.Constructor).GetDeclaringType(),
            HandleKind.MemberReference => module.Reader.GetMemberReference(
                (MemberReferenceHandle)attribute.Constructor).Parent,
            _ => default,
        };
        return parent.Kind switch
        {
            HandleKind.TypeDefinition => (module, (TypeDefinitionHandle)parent),
            HandleKind.TypeReference => RawTypeReferenceDefinition(module, (TypeReferenceHandle)parent),
            _ => null,
        };
    }

    private (Module Module, TypeDefinitionHandle Handle)? RawTypeReferenceDefinition(
        Module module, TypeReferenceHandle handle)
    {
        var tr = module.Reader.GetTypeReference(handle);
        string name = module.Reader.GetString(tr.Name);
        string ns = module.Reader.GetString(tr.Namespace);
        if (!TypeIndex().TryGetValue((ns, name), out var candidates))
            return null;
        if (tr.ResolutionScope.Kind == HandleKind.TypeReference)
        {
            if (RawTypeReferenceDefinition(module, (TypeReferenceHandle)tr.ResolutionScope)
                is not { } declaring)
                return null;
            foreach (var (candidateModule, candidate) in candidates)
                if (candidateModule == declaring.Module
                    && candidateModule.Reader.GetTypeDefinition(candidate).GetDeclaringType() == declaring.Handle)
                    return (candidateModule, candidate);
            return null;
        }

        Module? target = tr.ResolutionScope.Kind switch
        {
            HandleKind.AssemblyReference => Modules.FirstOrDefault(m => m.AssemblyName
                == module.Reader.GetString(module.Reader.GetAssemblyReference(
                    (AssemblyReferenceHandle)tr.ResolutionScope).Name)),
            HandleKind.ModuleDefinition => module,
            _ => null,
        };
        if (target is null)
            return null;
        foreach (var (candidateModule, candidate) in candidates)
            if (candidateModule == target
                && target.Reader.GetTypeDefinition(candidate).GetDeclaringType().IsNil)
                return (target, candidate);
        return null;
    }

    private IReadOnlyList<string> FindLinkFiles()
    {
        var paths = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (string rootValue in _projectRoots)
        {
            string root = Path.GetFullPath(rootValue);
            if (!Directory.Exists(root))
                throw new NotSupportedException($"--project-root {rootValue}: directory does not exist");
            foreach (string path in EnumerateLinkFiles(root))
            {
                string full = Path.GetFullPath(path);
                string relative = Path.GetRelativePath(root, full).Replace(Path.DirectorySeparatorChar, '/');
                if (!paths.TryGetValue(full, out var have)
                    || string.CompareOrdinal(relative, have) < 0)
                    paths[full] = relative;
            }
        }
        var rows = paths.Select(p => (Full: p.Key, Relative: p.Value)).ToList();
        rows.Sort((a, b) =>
        {
            int c = string.CompareOrdinal(a.Relative, b.Relative);
            return c != 0 ? c : string.CompareOrdinal(a.Full, b.Full);
        });
        var result = new List<string>(rows.Count);
        foreach (var row in rows) result.Add(row.Full);
        return result;
    }

    private static IEnumerable<string> EnumerateLinkFiles(string root)
    {
        var pending = new Stack<string>();
        pending.Push(root);
        while (pending.Count > 0)
        {
            string directory = pending.Pop();
            foreach (string path in Directory.EnumerateFiles(directory))
                if (string.Equals(Path.GetFileName(path), "link.xml", StringComparison.Ordinal))
                    yield return path;
            foreach (string child in Directory.EnumerateDirectories(directory))
            {
                string name = Path.GetFileName(child);
                if (name is "bin" or "obj" or ".godot" or ".git")
                    continue;
                pending.Push(child);
            }
        }
    }

    private void ApplyLinkDocument(string path, LinkNode root)
    {
        if (root.Name != "linker")
            throw LinkError(path, "root element must be <linker>");
        ValidateLinkSchema(root, path);
        RequireOnly(root, path, Array.Empty<string>());
        foreach (var assembly in root.Children)
        {
            if (assembly.Name != "assembly")
                throw LinkError(path, $"unknown <{assembly.Name}> under <linker>");
            RequireOnly(assembly, path, new[] { "fullname", "preserve", "ignoreIfMissing",
                "ignoreIfUnreferenced", "windowsruntime", "feature" });
            ValidateFeature(assembly, path);
            _ = BoolAttr(assembly, path, "ignoreIfMissing", false);
            _ = BoolAttr(assembly, path, "ignoreIfUnreferenced", false);
            _ = BoolAttr(assembly, path, "windowsruntime", false);
            if (!FeatureEnabled(assembly))
                continue;
            string assemblyName = Required(assembly, path, "fullname");
            int comma = assemblyName.IndexOf(',');
            string assemblyPattern = comma < 0 ? assemblyName : assemblyName.Substring(0, comma).Trim();
            var modules = Modules.Where(m => Wildcard(assemblyPattern, m.AssemblyName)).ToList();
            if (modules.Count == 0)
            {
                if (!BoolAttr(assembly, path, "ignoreIfMissing", false))
                    Warn(path, $"assembly '{assemblyName}' was not loaded");
                continue;
            }
            PreserveKind assemblyKind = ParsePreserve(assembly, path,
                assembly.Children.Count == 0 ? PreserveKind.All : PreserveKind.None,
                PreserveKind.None);
            foreach (var module in modules)
            {
                if (BoolAttr(assembly, path, "ignoreIfUnreferenced", false)
                    && !IsLinkAssemblyReferenced(module))
                    continue;
                if (assemblyKind != PreserveKind.None)
                    foreach (var tdh in module.Reader.TypeDefinitions)
                        Policy(module, tdh).Kind |= assemblyKind;
                foreach (var type in assembly.Children)
                    ApplyLinkType(path, module, type);
            }
        }
    }

    private static void ValidateLinkSchema(LinkNode root, string path)
    {
        RequireOnly(root, path, Array.Empty<string>());
        foreach (var assembly in root.Children)
        {
            if (assembly.Name != "assembly")
                throw LinkError(path, $"unknown <{assembly.Name}> under <linker>");
            RequireOnly(assembly, path, new[] { "fullname", "preserve", "ignoreIfMissing",
                "ignoreIfUnreferenced", "windowsruntime", "feature" });
            _ = Required(assembly, path, "fullname");
            _ = ParsePreserve(assembly, path, PreserveKind.None, PreserveKind.None);
            _ = BoolAttr(assembly, path, "ignoreIfMissing", false);
            _ = BoolAttr(assembly, path, "ignoreIfUnreferenced", false);
            _ = BoolAttr(assembly, path, "windowsruntime", false);
            ValidateFeature(assembly, path);
            foreach (var type in assembly.Children)
            {
                if (type.Name != "type")
                    throw LinkError(path, $"unknown <{type.Name}> under <assembly>");
                RequireOnly(type, path, new[] { "fullname", "name", "preserve", "required", "feature" });
                int typeSelectors = (type.Attr("fullname") is null ? 0 : 1)
                    + (type.Attr("name") is null ? 0 : 1);
                if (typeSelectors != 1)
                    throw LinkError(path, "<type> requires exactly one of fullname or name");
                _ = ParsePreserve(type, path, PreserveKind.Type, PreserveKind.Type);
                _ = BoolAttr(type, path, "required", true);
                ValidateFeature(type, path);
                foreach (var member in type.Children)
                {
                    if (member.Name is not ("field" or "method" or "property" or "event"))
                        throw LinkError(path, $"unknown <{member.Name}> under <type>");
                    RequireOnly(member, path,
                        new[] { "name", "fullname", "signature", "accessors", "feature" });
                    int selectors = (member.Attr("name") is null ? 0 : 1)
                        + (member.Attr("fullname") is null ? 0 : 1)
                        + (member.Attr("signature") is null ? 0 : 1);
                    if (selectors != 1)
                        throw LinkError(path,
                            $"<{member.Name}> requires exactly one of name, fullname, or signature");
                    string? accessors = member.Attr("accessors");
                    if (member.Name != "property" && accessors is not null)
                        throw LinkError(path, $"accessors is not valid on <{member.Name}>");
                    if (member.Name == "property" && accessors is not null
                        && accessors is not ("all" or "get" or "set"))
                        throw LinkError(path, $"invalid accessors='{accessors}'");
                    ValidateFeature(member, path);
                }
            }
        }
    }

    private bool IsLinkAssemblyReferenced(Module target)
    {
        if (target == AppModule)
            return true;
        foreach (var module in Modules)
            foreach (var handle in module.Reader.TypeReferences)
            {
                var tr = module.Reader.GetTypeReference(handle);
                EntityHandle scope = tr.ResolutionScope;
                while (scope.Kind == HandleKind.TypeReference)
                    scope = module.Reader.GetTypeReference((TypeReferenceHandle)scope).ResolutionScope;
                if (scope.Kind == HandleKind.AssemblyReference
                    && module.Reader.GetString(module.Reader.GetAssemblyReference(
                        (AssemblyReferenceHandle)scope).Name) == target.AssemblyName)
                    return true;
            }
        return false;
    }

    private void ApplyLinkType(string path, Module module, LinkNode type)
    {
        if (type.Name != "type")
            throw LinkError(path, $"unknown <{type.Name}> under <assembly>");
        RequireOnly(type, path, new[] { "fullname", "name", "preserve", "required", "feature" });
        ValidateFeature(type, path);
        _ = BoolAttr(type, path, "required", true);
        if (!FeatureEnabled(type))
            return;
        int typeSelectors = (type.Attr("fullname") is null ? 0 : 1) + (type.Attr("name") is null ? 0 : 1);
        if (typeSelectors != 1) throw LinkError(path, "<type> requires exactly one of fullname or name");
        string pattern = type.Attr("fullname") ?? type.Attr("name")!;
        pattern = pattern.Replace('/', '+');
        var matches = new List<TypeDefinitionHandle>();
        foreach (var tdh in module.Reader.TypeDefinitions)
        {
            string name = RawReflectionTypeName(module.Reader, tdh);
            if (Wildcard(pattern, name) || Wildcard(pattern, module.Reader.GetString(module.Reader.GetTypeDefinition(tdh).Name)))
                matches.Add(tdh);
        }
        if (matches.Count == 0)
        {
            Warn(path, $"type '{pattern}' was not found in assembly '{module.AssemblyName}'");
            return;
        }
        foreach (var tdh in matches)
        {
            var policy = Policy(module, tdh);
            bool conditional = !BoolAttr(type, path, "required", true);
            PreserveKind kind = ParsePreserve(type, path,
                type.Children.Count == 0 ? PreserveKind.All : PreserveKind.Type,
                PreserveKind.Type);
            if (conditional) policy.ConditionalKind |= kind;
            else policy.Kind |= kind;
            foreach (var member in type.Children)
                ApplyLinkMember(path, module, tdh, policy, member, conditional);
        }
    }

    private void ApplyLinkMember(string path, Module module, TypeDefinitionHandle tdh,
        PreservePolicy policy, LinkNode member, bool conditional)
    {
        if (member.Name is not ("field" or "method" or "property" or "event"))
            throw LinkError(path, $"unknown <{member.Name}> under <type>");
        RequireOnly(member, path, new[] { "name", "fullname", "signature", "accessors", "feature" });
        ValidateFeature(member, path);
        if (member.Name != "property" && member.Attr("accessors") is not null)
            throw LinkError(path, $"accessors is not valid on <{member.Name}>");
        int selectors = (member.Attr("name") is null ? 0 : 1)
            + (member.Attr("fullname") is null ? 0 : 1) + (member.Attr("signature") is null ? 0 : 1);
        if (selectors != 1)
            throw LinkError(path, $"<{member.Name}> requires exactly one of name, fullname, or signature");
        if (!FeatureEnabled(member))
            return;
        var td = module.Reader.GetTypeDefinition(tdh);
        string selector = member.Attr("name") ?? member.Attr("fullname") ?? member.Attr("signature")!;
        string simpleSelector = MemberSelectorName(selector);
        string declaringName = RawReflectionTypeName(module.Reader, tdh);
        bool bySignature = member.Attr("signature") is not null;
        var fields = conditional ? policy.ConditionalFields : policy.Fields;
        var methods = conditional ? policy.ConditionalMethods : policy.Methods;
        var properties = conditional ? policy.ConditionalProperties : policy.Properties;
        var events = conditional ? policy.ConditionalEvents : policy.Events;
        int hit = 0;
        if (member.Name == "field")
        {
            foreach (var h in td.GetFields())
            {
                string name = module.Reader.GetString(module.Reader.GetFieldDefinition(h).Name);
                if (bySignature ? FieldSignatureMatches(module, h, selector)
                    : Wildcard(simpleSelector, name) || Wildcard(selector, declaringName + "::" + name))
                { fields.Add(h); hit++; }
            }
        }
        else if (member.Name == "method")
        {
            foreach (var h in td.GetMethods())
            {
                var md = module.Reader.GetMethodDefinition(h);
                string name = module.Reader.GetString(md.Name);
                if (bySignature ? MethodSignatureMatches(module, h, selector)
                    : Wildcard(simpleSelector, name) || Wildcard(selector, declaringName + "::" + name))
                { methods.Add(h); hit++; }
            }
        }
        else if (member.Name is "property" or "event")
        {
            bool property = member.Name == "property";
            if (property)
                foreach (var h in td.GetProperties())
                {
                    var def = module.Reader.GetPropertyDefinition(h);
                    string name = module.Reader.GetString(def.Name);
                    if (bySignature ? !PropertySignatureMatches(module, tdh, h, selector)
                        : !Wildcard(simpleSelector, name) && !Wildcard(selector, declaringName + "::" + name)) continue;
                    AddSelectedAccessors(methods, def.GetAccessors(), member.Attr("accessors"), path);
                    properties.Add(h);
                    AddBackingField(module, td, fields, module.Reader.GetString(def.Name)); hit++;
                }
            else
                foreach (var h in td.GetEvents())
                {
                    var def = module.Reader.GetEventDefinition(h);
                    string name = module.Reader.GetString(def.Name);
                    if (bySignature ? !EventSignatureMatches(module, tdh, h, selector)
                        : !Wildcard(simpleSelector, name) && !Wildcard(selector, declaringName + "::" + name)) continue;
                    AddSelectedAccessors(methods, def.GetAccessors(), member.Attr("accessors"), path);
                    events.Add(h);
                    AddBackingField(module, td, fields, module.Reader.GetString(def.Name)); hit++;
                }
        }
        else
            throw LinkError(path, $"unknown <{member.Name}> under <type>");
        if (hit == 0)
            Warn(path, $"{member.Name} '{selector}' was not found on '{RawReflectionTypeName(module.Reader, tdh)}'");
    }

    private static string MemberSelectorName(string selector)
    {
        int scope = selector.LastIndexOf("::", StringComparison.Ordinal);
        if (scope >= 0)
            return selector.Substring(scope + 2);
        int space = selector.LastIndexOf(' ');
        return space < 0 ? selector : selector.Substring(space + 1);
    }

    private bool MethodSignatureMatches(Module module, MethodDefinitionHandle handle, string selector)
    {
        int paren = selector.IndexOf('(');
        int close = selector.LastIndexOf(')');
        if (paren < 0 || close < paren)
            return false;
        string head = selector.Substring(0, paren).Trim();
        int space = head.LastIndexOf(' ');
        string wantedName = space < 0 ? head : head.Substring(space + 1);
        int scope = wantedName.LastIndexOf("::", StringComparison.Ordinal);
        if (scope >= 0) wantedName = wantedName.Substring(scope + 2);
        var md = module.Reader.GetMethodDefinition(handle);
        if (module.Reader.GetString(md.Name) != wantedName)
            return false;
        MethodSignature<string> sig;
        try { sig = md.DecodeSignature(RawSignatureProvider.Instance, null); }
        catch (Exception e) when (!IsMustEscape(e)) { return false; }
        string args = selector.Substring(paren + 1, close - paren - 1).Trim();
        var wanted = SplitSignatureArguments(args);
        if (wanted.Length != sig.ParameterTypes.Length)
            return false;
        for (int i = 0; i < wanted.Length; i++)
            if (!TypeNameMatches(NamedGenericParameters(module.Reader, md.GetDeclaringType(), handle,
                sig.ParameterTypes[i]), wanted[i]))
                return false;
        if (space >= 0 && !TypeNameMatches(NamedGenericParameters(module.Reader,
            md.GetDeclaringType(), handle, sig.ReturnType), head.Substring(0, space).Trim()))
            return false;
        return true;
    }

    private static string[] SplitSignatureArguments(string value)
    {
        if (value.Length == 0) return Array.Empty<string>();
        var parts = new List<string>();
        int start = 0, angle = 0, square = 0;
        for (int i = 0; i < value.Length; i++)
        {
            if (value[i] == '<') angle++;
            else if (value[i] == '>') angle--;
            else if (value[i] == '[') square++;
            else if (value[i] == ']') square--;
            else if (value[i] == ',' && angle == 0 && square == 0)
            {
                parts.Add(value.Substring(start, i - start).Trim());
                start = i + 1;
            }
        }
        parts.Add(value.Substring(start).Trim());
        return parts.ToArray();
    }

    private bool FieldSignatureMatches(Module module, FieldDefinitionHandle handle, string selector)
    {
        var (type, name) = SignatureMember(selector);
        var fd = module.Reader.GetFieldDefinition(handle);
        if (module.Reader.GetString(fd.Name) != name) return false;
        try { return TypeNameMatches(NamedGenericParameters(module.Reader, fd.GetDeclaringType(), null,
            fd.DecodeSignature(RawSignatureProvider.Instance, null)), type); }
        catch (Exception e) when (!IsMustEscape(e)) { return false; }
    }

    private bool PropertySignatureMatches(Module module, TypeDefinitionHandle declaring,
        PropertyDefinitionHandle handle, string selector)
    {
        var (type, name) = SignatureMember(selector);
        var pd = module.Reader.GetPropertyDefinition(handle);
        if (module.Reader.GetString(pd.Name) != name) return false;
        try { return TypeNameMatches(NamedGenericParameters(module.Reader, declaring, null,
            pd.DecodeSignature(RawSignatureProvider.Instance, null).ReturnType), type); }
        catch (Exception e) when (!IsMustEscape(e)) { return false; }
    }

    private bool EventSignatureMatches(Module module, TypeDefinitionHandle declaring,
        EventDefinitionHandle handle, string selector)
    {
        var (type, name) = SignatureMember(selector);
        var ed = module.Reader.GetEventDefinition(handle);
        if (module.Reader.GetString(ed.Name) != name) return false;
        try
        {
            string eventType = ed.Type.Kind switch
            {
                HandleKind.TypeDefinition => RawSignatureProvider.TypeDefinitionName(module.Reader, (TypeDefinitionHandle)ed.Type),
                HandleKind.TypeReference => RawSignatureProvider.TypeReferenceName(module.Reader, (TypeReferenceHandle)ed.Type),
                HandleKind.TypeSpecification => module.Reader.GetTypeSpecification((TypeSpecificationHandle)ed.Type)
                    .DecodeSignature(RawSignatureProvider.Instance, null),
                _ => "?",
            };
            return TypeNameMatches(NamedGenericParameters(module.Reader, declaring, null, eventType), type);
        }
        catch (Exception e) when (!IsMustEscape(e)) { return false; }
    }

    private static (string Type, string Name) SignatureMember(string selector)
    {
        int paren = selector.IndexOf('(');
        string head = (paren < 0 ? selector : selector.Substring(0, paren)).Trim();
        int space = head.LastIndexOf(' ');
        if (space < 0) return ("", head);
        string name = head.Substring(space + 1);
        int scope = name.LastIndexOf("::", StringComparison.Ordinal);
        if (scope >= 0) name = name.Substring(scope + 2);
        return (head.Substring(0, space).Trim(), name);
    }

    private static bool TypeNameMatches(string actual, string wanted) =>
        string.Equals(actual, wanted.Replace('/', '+'), StringComparison.Ordinal);

    private static string NamedGenericParameters(MetadataReader reader,
        TypeDefinitionHandle declaring, MethodDefinitionHandle? method, string value)
    {
        if (method is { } mh)
        {
            var parameters = reader.GetMethodDefinition(mh).GetGenericParameters().ToArray();
            for (int i = parameters.Length - 1; i >= 0; i--)
                value = value.Replace("!!" + i,
                    reader.GetString(reader.GetGenericParameter(parameters[i]).Name), StringComparison.Ordinal);
        }
        var typeParameters = reader.GetTypeDefinition(declaring).GetGenericParameters().ToArray();
        for (int i = typeParameters.Length - 1; i >= 0; i--)
            value = value.Replace("!" + i,
                reader.GetString(reader.GetGenericParameter(typeParameters[i]).Name), StringComparison.Ordinal);
        return value;
    }

    private static void AddSelectedAccessors(HashSet<MethodDefinitionHandle> methods, PropertyAccessors a,
        string? value, string path)
    {
        string selected = value ?? "all";
        if (selected is not ("all" or "get" or "set")) throw LinkError(path, $"invalid accessors='{selected}'");
        if (selected is "all" or "get" && !a.Getter.IsNil) methods.Add(a.Getter);
        if (selected is "all" or "set" && !a.Setter.IsNil) methods.Add(a.Setter);
    }

    private static void AddSelectedAccessors(HashSet<MethodDefinitionHandle> methods, EventAccessors a,
        string? value, string path)
    {
        string selected = value ?? "all";
        if (selected is not ("all" or "add" or "remove")) throw LinkError(path, $"invalid accessors='{selected}'");
        if (selected is "all" or "add" && !a.Adder.IsNil) methods.Add(a.Adder);
        if (selected is "all" or "remove" && !a.Remover.IsNil) methods.Add(a.Remover);
    }

    private bool FeatureEnabled(LinkNode node) =>
        node.Attr("feature") is not { } feature || _linkFeatures.Contains(feature);

    private static void ValidateFeature(LinkNode node, string path)
    {
        if (node.Attr("feature") is { } feature && feature is not ("com" or "sre" or "remoting"))
            throw LinkError(path, $"unknown feature='{feature}'");
    }

    private static PreserveKind ParsePreserve(LinkNode node, string path, PreserveKind fallback,
        PreserveKind nothing) =>
        node.Attr("preserve") switch
        {
            null => fallback,
            "all" => PreserveKind.All,
            "fields" => PreserveKind.Type | PreserveKind.Fields,
            "methods" => PreserveKind.Type | PreserveKind.Methods,
            "nothing" => nothing,
            var value => throw LinkError(path, $"invalid preserve='{value}'"),
        };

    private static bool BoolAttr(LinkNode node, string path, string name, bool fallback) =>
        node.Attr(name) switch
        {
            null => fallback,
            "0" or "false" => false,
            "1" or "true" => true,
            var value => throw LinkError(path, $"invalid {name}='{value}'"),
        };

    private static string Required(LinkNode node, string path, string name) =>
        node.Attr(name) ?? throw LinkError(path, $"<{node.Name}> requires {name}");

    private static void RequireOnly(LinkNode node, string path, IReadOnlyList<string> allowed)
    {
        foreach (var pair in node.Attributes)
            if (!allowed.Contains(pair.Key))
                throw LinkError(path, $"unknown attribute '{pair.Key}' on <{node.Name}>");
    }

    private static bool Wildcard(string pattern, string value)
    {
        int p = 0, v = 0, star = -1, retry = -1;
        while (v < value.Length)
        {
            if (p < pattern.Length && pattern[p] == value[v]) { p++; v++; continue; }
            if (p < pattern.Length && pattern[p] == '*') { star = p++; retry = v; continue; }
            if (star >= 0) { p = star + 1; v = ++retry; continue; }
            return false;
        }
        while (p < pattern.Length && pattern[p] == '*') p++;
        return p == pattern.Length;
    }

    private static string RawReflectionTypeName(MetadataReader reader, TypeDefinitionHandle h)
    {
        var td = reader.GetTypeDefinition(h);
        string name = reader.GetString(td.Name);
        var decl = td.GetDeclaringType();
        while (!decl.IsNil)
        {
            td = reader.GetTypeDefinition(decl);
            name = reader.GetString(td.Name) + "+" + name;
            decl = td.GetDeclaringType();
        }
        string ns = reader.GetString(td.Namespace);
        return string.IsNullOrEmpty(ns) ? name : ns + "." + name;
    }

    private static NotSupportedException LinkError(string path, string message) =>
        new($"{path}: link.xml: {message}");

    private static void Warn(string path, string message) =>
        Console.Error.WriteLine($"warning: {path}: link.xml: {message}");

    private void SeedPreservedMembers()
    {
        _preservationSeedingActive = true;
        foreach (var cls in Classes.ToList())
            if (cls.MembersReady)
                ApplyPreservation(cls);
            else if (cls.ShapeReady)
                ApplyPreservationAfterShape(cls);
    }

    private void ApplyPreservation(ClassInfo cls)
    {
        // Policies key on the TypeDef handle, so an open shell (an unsubstituted
        // !!n spec from an empty-context overload decode) matches its definition's
        // policy — preserving it drags an open definition into the emit set. Each
        // closed instantiation gets the policy applied as it completes.
        if (ContainsGenericVar(cls))
            return;
        if (!_preservePolicies.TryGetValue((cls.Module.Index, cls.Handle), out var policy))
            return;
        bool conditional = _activatedConditionalPolicies.Contains(cls);
        PreserveKind kind = policy.Kind | (conditional ? policy.ConditionalKind : PreserveKind.None);
        bool delegateAll = cls.IsDelegate && kind != PreserveKind.None;
        if (kind != PreserveKind.None || policy.Fields.Count > 0 || policy.Methods.Count > 0
            || policy.Properties.Count > 0 || policy.Events.Count > 0
            || conditional && (policy.ConditionalFields.Count > 0 || policy.ConditionalMethods.Count > 0
                || policy.ConditionalProperties.Count > 0 || policy.ConditionalEvents.Count > 0))
        {
            NoteForceEmit(cls);
            _explicitReflectionKeep.Add(cls);
        }
        if ((kind & PreserveKind.Fields) != 0)
            foreach (var field in cls.Fields) PreserveField(field);
        else
            foreach (var field in cls.Fields)
                if (policy.Fields.Contains(field.Handle)
                    || conditional && policy.ConditionalFields.Contains(field.Handle)) PreserveField(field);
        bool ctorPreserved = false;
        foreach (var method in cls.Methods)
        {
            bool keep = delegateAll || (kind & PreserveKind.Methods) != 0
                || policy.Methods.Contains(method.Handle)
                || conditional && policy.ConditionalMethods.Contains(method.Handle)
                || (kind & PreserveKind.DefaultConstructor) != 0
                    && method.Name == ".ctor" && method.Signature.ParameterTypes.Length == 0;
            if (keep)
            {
                PreserveMethod(method);
                ctorPreserved |= method.Name == ".ctor" && !method.IsStatic && method.Rva != 0;
            }
        }
        // A preserved instance ctor declares "reflection constructs this" — the
        // instance then dispatches through vtable and interface slots, so the
        // class must cross the used-slot × allocated-type product. Reaching the
        // ctor alone leaves every slot of the minted instance a trap stub (on
        // wasm that trap is an unnamed call_indirect signature mismatch). The
        // constructor's caller dispatches the instance's USER-interface surface
        // next (a DI container's GetInterfaces → GetMethod → Invoke injection
        // pass), and that dispatch never records a used slot — it is a runtime
        // interface-table walk — so those impls are reached with it.
        if (ctorPreserved && !cls.IsValueType && !cls.IsAbstract && !cls.IsInterface
            && !cls.IsDelegate)
        {
            ReachAllocatedType(cls);
            ReachUserInterfaceImpls(cls);
        }
        foreach (var ph in policy.Properties)
            PreservePropertyType(cls, ph);
        foreach (var eh in policy.Events)
            PreserveEventType(cls, eh);
        if (conditional)
        {
            foreach (var ph in policy.ConditionalProperties) PreservePropertyType(cls, ph);
            foreach (var eh in policy.ConditionalEvents) PreserveEventType(cls, eh);
        }
    }

    private void PreservePropertyType(ClassInfo cls, PropertyDefinitionHandle handle)
    {
        var sig = cls.Module.Reader.GetPropertyDefinition(handle).DecodeSignature(SigProvider, cls.Context);
        NotePreservedType(sig.ReturnType);
        foreach (var type in sig.ParameterTypes) NotePreservedType(type);
    }

    private void PreserveEventType(ClassInfo cls, EventDefinitionHandle handle)
    {
        var ed = cls.Module.Reader.GetEventDefinition(handle);
        TypeDesc type = ed.Type.Kind switch
        {
            HandleKind.TypeDefinition => GetTypeDescForDefinition(cls.Module, (TypeDefinitionHandle)ed.Type),
            HandleKind.TypeReference => ResolveTypeRef(cls.Module, (TypeReferenceHandle)ed.Type) ?? TypeDesc.MakeExternal("?"),
            HandleKind.TypeSpecification => cls.Module.Reader.GetTypeSpecification((TypeSpecificationHandle)ed.Type)
                .DecodeSignature(SigProvider, cls.Context),
            _ => TypeDesc.MakeExternal("?"),
        };
        NotePreservedType(type);
    }

    private void ApplyPreservationAfterShape(ClassInfo cls)
    {
        if (!_preservationSeedingActive
            || ContainsGenericVar(cls) // open shell — see ApplyPreservation
            || !_preservePolicies.TryGetValue((cls.Module.Index, cls.Handle), out var policy))
            return;
        if (cls.MembersReady)
        {
            ApplyPreservation(cls);
            return;
        }
        bool conditional = PreservedTypeWasOtherwiseUsed(cls);
        PreserveKind kind = policy.Kind | (conditional ? policy.ConditionalKind : PreserveKind.None);
        if ((kind & (PreserveKind.Methods | PreserveKind.DefaultConstructor)) != 0
            || policy.Methods.Count > 0 || conditional && policy.ConditionalMethods.Count > 0
            || cls.IsDelegate && kind != PreserveKind.None)
            CompleteMembers(cls);
        else
            ApplyPreservation(cls);
    }

    private bool PreservedTypeWasOtherwiseUsed(ClassInfo cls) =>
        ReferencedTypes.Contains(cls) || ForceEmittedClasses.Contains(cls)
        || IsAllocated(cls) || Reachable.Any(m => m.DeclaringClass == cls)
        || _reflectionRoots.Count > 0 && ReflectionRootMatching(cls) is not null;

    private bool ActivateConditionalPreservationPolicies()
    {
        if (!_preservationSeedingActive) return false;
        bool activated = false;
        foreach (var cls in Classes.ToList())
            if (!_activatedConditionalPolicies.Contains(cls)
                && _preservePolicies.TryGetValue((cls.Module.Index, cls.Handle), out var policy)
                && (policy.ConditionalKind != PreserveKind.None
                    || policy.ConditionalFields.Count > 0 || policy.ConditionalMethods.Count > 0
                    || policy.ConditionalProperties.Count > 0 || policy.ConditionalEvents.Count > 0)
                && PreservedTypeWasOtherwiseUsed(cls))
            {
                _activatedConditionalPolicies.Add(cls);
                if (cls.ShapeReady) ApplyPreservationAfterShape(cls);
                activated = true;
            }
        return activated;
    }

    private void ApplyPreservationToInstantiatedMethod(MethodInfo method)
    {
        if (!_preservationSeedingActive
            || !_preservePolicies.TryGetValue((method.DeclaringClass.Module.Index,
                method.DeclaringClass.Handle), out var policy))
            return;
        bool conditional = _activatedConditionalPolicies.Contains(method.DeclaringClass);
        PreserveKind kind = policy.Kind | (conditional ? policy.ConditionalKind : PreserveKind.None);
        if ((kind & PreserveKind.Methods) != 0 || policy.Methods.Contains(method.Handle)
            || conditional && policy.ConditionalMethods.Contains(method.Handle))
        {
            PreserveMethod(method);
            // Same allocation rule as ApplyPreservation: a preserved instance
            // ctor of a concrete reference instantiation is a late-bound
            // construction site.
            var cls = method.DeclaringClass;
            if (method.Name == ".ctor" && !method.IsStatic && method.Rva != 0
                && !cls.IsValueType && !cls.IsAbstract && !cls.IsInterface && !cls.IsDelegate)
                ReachAllocatedType(cls);
        }
    }

    private void PreserveField(FieldInfo field)
    {
        NoteForceEmit(field.DeclaringClass);
        NotePreservedType(field.Type);
        ReachCctor(field.DeclaringClass);
    }

    private void PreserveMethod(MethodInfo method)
    {
        NoteForceEmit(method.DeclaringClass);
        var sig = method.Signature;
        NotePreservedType(sig.ReturnType);
        foreach (var type in sig.ParameterTypes) NotePreservedType(type);
        Reach(method);
    }

    private void NotePreservedType(TypeDesc type)
    {
        switch (type.Kind)
        {
            case TypeKind.Class:
                NoteForceEmit(type.Class);
                if (type.Class is not null) _explicitReflectionKeep.Add(type.Class);
                break;
            case TypeKind.SZArray:
            case TypeKind.MDArray:
            case TypeKind.ByRef:
                if (type.Element is not null) NotePreservedType(type.Element);
                break;
            case TypeKind.ExternalGeneric:
                if (type.GenericArgs is not null)
                    foreach (var arg in type.GenericArgs) NotePreservedType(arg);
                break;
        }
        if (type.Kind == TypeKind.Class && type.Class is { } cls)
            foreach (var arg in cls.Context.TypeArgs) NotePreservedType(arg);
    }
}

internal sealed class LinkNode
{
    public required string Name;
    public readonly Dictionary<string, string> Attributes = new(StringComparer.Ordinal);
    public readonly List<LinkNode> Children = new();
    public string? Attr(string name) => Attributes.TryGetValue(name, out var value) ? value : null;
}

internal sealed class RawSignatureProvider : ISignatureTypeProvider<string, object?>
{
    public static readonly RawSignatureProvider Instance = new();

    public string GetPrimitiveType(PrimitiveTypeCode typeCode) => "System." + typeCode;
    public string GetTypeFromDefinition(MetadataReader reader, TypeDefinitionHandle handle, byte rawTypeKind) =>
        TypeDefinitionName(reader, handle);
    public string GetTypeFromReference(MetadataReader reader, TypeReferenceHandle handle, byte rawTypeKind) =>
        TypeReferenceName(reader, handle);
    public string GetSZArrayType(string elementType) => elementType + "[]";
    public string GetArrayType(string elementType, ArrayShape shape) =>
        elementType + "[" + new string(',', shape.Rank - 1) + "]";
    public string GetByReferenceType(string elementType) => elementType + "&";
    public string GetPointerType(string elementType) => elementType + "*";
    public string GetFunctionPointerType(MethodSignature<string> signature) => "method*";
    public string GetGenericInstantiation(string genericType,
        System.Collections.Immutable.ImmutableArray<string> typeArguments)
    {
        string result = genericType + "<";
        for (int i = 0; i < typeArguments.Length; i++)
        {
            if (i != 0) result += ",";
            result += typeArguments[i];
        }
        return result + ">";
    }
    public string GetGenericMethodParameter(object? genericContext, int index) => "!!" + index;
    public string GetGenericTypeParameter(object? genericContext, int index) => "!" + index;
    public string GetModifiedType(string modifier, string unmodifiedType, bool isRequired) => unmodifiedType;
    public string GetPinnedType(string elementType) => elementType;
    public string GetTypeFromSpecification(MetadataReader reader, object? genericContext,
        TypeSpecificationHandle handle, byte rawTypeKind) =>
        reader.GetTypeSpecification(handle).DecodeSignature(this, genericContext);

    public static string TypeDefinitionName(MetadataReader reader, TypeDefinitionHandle handle)
    {
        var td = reader.GetTypeDefinition(handle);
        string name = reader.GetString(td.Name);
        var declaring = td.GetDeclaringType();
        while (!declaring.IsNil)
        {
            td = reader.GetTypeDefinition(declaring);
            name = reader.GetString(td.Name) + "+" + name;
            declaring = td.GetDeclaringType();
        }
        string ns = reader.GetString(td.Namespace);
        return string.IsNullOrEmpty(ns) ? name : ns + "." + name;
    }

    public static string TypeReferenceName(MetadataReader reader, TypeReferenceHandle handle)
    {
        var tr = reader.GetTypeReference(handle);
        string name = reader.GetString(tr.Name);
        if (tr.ResolutionScope.Kind == HandleKind.TypeReference)
            return TypeReferenceName(reader, (TypeReferenceHandle)tr.ResolutionScope) + "+" + name;
        string ns = reader.GetString(tr.Namespace);
        return string.IsNullOrEmpty(ns) ? name : ns + "." + name;
    }

}

internal static class LinkXml
{
    public static LinkNode Parse(string path)
    {
        using var reader = File.OpenText(path);
        var parser = new Parser(path, reader);
        return parser.Parse();
    }

    private sealed class Parser
    {
        private readonly string _path;
        private readonly TextReader _reader;
        private readonly List<int> _look = new();
        private int _offset;

        public Parser(string path, TextReader reader) { _path = path; _reader = reader; }

        public LinkNode Parse()
        {
            SkipSpace();
            if (Take("<?xml"))
            {
                if (!End && !char.IsWhiteSpace((char)Peek())) Fail("malformed XML declaration");
                Declaration();
            }
            SkipMisc();
            var root = Element();
            SkipMisc();
            if (!End) Fail("content after root element");
            return root;
        }

        private LinkNode Element()
        {
            Expect('<');
            if (Peek('!')) Fail("DTD and declarations are not supported");
            var node = new LinkNode { Name = Name() };
            while (true)
            {
                SkipSpace();
                if (Take("/>")) return node;
                if (Take(">")) break;
                string name = Name();
                SkipSpace(); Expect('='); SkipSpace();
                if (End || Peek() is not ('\'' or '"')) Fail("quoted attribute value expected");
                char quote = (char)Read();
                var valueText = new System.Text.StringBuilder();
                while (!End && Peek() != quote)
                {
                    if (Peek() == '<') Fail("'<' must be escaped in an attribute value");
                    valueText.Append((char)Read());
                }
                if (End) Fail("unterminated attribute value");
                Read();
                string value = Entities(valueText.ToString());
                if (!node.Attributes.TryAdd(name, value)) Fail($"duplicate attribute '{name}'");
            }
            while (true)
            {
                if (Take("<!--")) { SkipComment(); continue; }
                if (Take("</"))
                {
                    string close = Name(); SkipSpace(); Expect('>');
                    if (close != node.Name) Fail($"closing </{close}> does not match <{node.Name}>");
                    return node;
                }
                if (Peek('<')) { node.Children.Add(Element()); continue; }
                var content = new System.Text.StringBuilder();
                while (!End && Peek() != '<') content.Append((char)Read());
                if (!string.IsNullOrWhiteSpace(Entities(content.ToString())))
                    Fail("non-whitespace text is not allowed");
                if (End) Fail($"unterminated <{node.Name}>");
            }
        }

        private void SkipMisc()
        {
            while (true)
            {
                SkipSpace();
                if (!Take("<!--")) return;
                SkipComment();
            }
        }

        private void Declaration()
        {
            int stage = 0;
            while (true)
            {
                SkipSpace();
                if (Take("?>"))
                {
                    if (stage == 0) Fail("XML declaration requires version");
                    return;
                }
                string name = Name();
                SkipSpace(); Expect('='); SkipSpace();
                if (End || Peek() is not ('\'' or '"')) Fail("quoted XML declaration value expected");
                char quote = (char)Read();
                var text = new System.Text.StringBuilder();
                while (!End && Peek() != quote) text.Append((char)Read());
                if (End) Fail("unterminated XML declaration value");
                Read();
                string value = text.ToString();
                if (stage == 0 && name == "version" && value == "1.0")
                    stage = 1;
                else if (stage == 1 && name == "encoding" && ValidEncodingName(value))
                    stage = 2;
                else if (stage is 1 or 2 && name == "standalone" && value is "yes" or "no")
                    stage = 3;
                else
                    Fail($"invalid XML declaration attribute {name}='{value}'");
                if (!End && !char.IsWhiteSpace((char)Peek()) && !StartsWith("?>"))
                    Fail("whitespace expected in XML declaration");
            }
        }

        private static bool ValidEncodingName(string value)
        {
            if (value.Length == 0 || !char.IsLetter(value[0])) return false;
            for (int i = 1; i < value.Length; i++)
                if (!(char.IsLetterOrDigit(value[i]) || value[i] is '.' or '_' or '-'))
                    return false;
            return true;
        }

        private string Name()
        {
            if (End || !(char.IsLetter((char)Peek()) || Peek() is '_' or ':'))
                Fail("XML name expected");
            var name = new System.Text.StringBuilder();
            name.Append((char)Read());
            while (!End && (char.IsLetterOrDigit((char)Peek()) || Peek() is '_' or '-' or ':' or '.'))
                name.Append((char)Read());
            return name.ToString();
        }

        private string Entities(string value)
        {
            var result = new System.Text.StringBuilder();
            for (int i = 0; i < value.Length; i++)
            {
                if (value[i] != '&') { result.Append(value[i]); continue; }
                int semi = value.IndexOf(';', i + 1);
                if (semi < 0) Fail("unterminated entity");
                string entity = value.Substring(i + 1, semi - i - 1);
                if (entity == "amp") result.Append('&');
                else if (entity == "lt") result.Append('<');
                else if (entity == "gt") result.Append('>');
                else if (entity == "quot") result.Append('"');
                else if (entity == "apos") result.Append('\'');
                else if (entity.StartsWith("#", StringComparison.Ordinal))
                {
                    try
                    {
                        int number = entity.StartsWith("#x", StringComparison.Ordinal)
                            ? Convert.ToInt32(entity.Substring(2), 16)
                            : Convert.ToInt32(entity.Substring(1), 10);
                        if (!(number is 0x9 or 0xa or 0xd
                            || number >= 0x20 && number <= 0xd7ff
                            || number >= 0xe000 && number <= 0xfffd
                            || number >= 0x10000 && number <= 0x10ffff))
                            Fail($"numeric entity '&{entity};' is not a valid XML character");
                        result.Append(char.ConvertFromUtf32(number));
                    }
                    catch (Exception e) when (e is FormatException or OverflowException or ArgumentOutOfRangeException)
                    {
                        Fail($"invalid numeric entity '&{entity};'");
                    }
                }
                else Fail($"unknown entity '&{entity};'");
                i = semi;
            }
            return result.ToString();
        }

        private void SkipSpace() { while (!End && char.IsWhiteSpace((char)Peek())) Read(); }
        private bool Peek(char c) => !End && Peek() == c;
        private bool Take(string value)
        {
            for (int i = 0; i < value.Length; i++)
                if (Peek(i) != value[i]) return false;
            for (int i = 0; i < value.Length; i++) Read();
            return true;
        }
        private bool StartsWith(string value)
        {
            for (int i = 0; i < value.Length; i++)
                if (Peek(i) != value[i]) return false;
            return true;
        }
        private void Expect(char c) { if (!Peek(c)) Fail($"expected '{c}'"); Read(); }
        private void SkipUntil(string value)
        {
            while (!End)
            {
                if (Take(value)) return;
                Read();
            }
            Fail($"unterminated '{value}'");
        }
        private void SkipComment()
        {
            while (!End)
            {
                if (Take("-->")) return;
                if (Take("--")) Fail("'--' is not allowed inside an XML comment");
                Read();
            }
            Fail("unterminated XML comment");
        }

        private bool End => Peek() < 0;
        private int Peek(int index = 0)
        {
            while (_look.Count <= index)
            {
                int value = _reader.Read();
                _look.Add(value);
                if (value < 0) break;
            }
            return index < _look.Count ? _look[index] : -1;
        }

        private int Read()
        {
            int value = Peek();
            _look.RemoveAt(0);
            if (value >= 0) _offset++;
            return value;
        }

        private void Fail(string message) => throw new NotSupportedException($"{_path}: link.xml: {message} at offset {_offset}");
    }
}

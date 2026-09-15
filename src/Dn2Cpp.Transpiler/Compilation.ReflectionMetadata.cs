using System.Reflection.Metadata;
using SRME = System.Reflection.Metadata.Ecma335.MetadataTokens;

namespace Dn2Cpp;

internal sealed partial class Compilation
{
    private readonly Dictionary<string, bool> _reflectionMetadataFormats = new(StringComparer.Ordinal);
    private readonly Dictionary<string, string> _reflectionMetadataMatches = new(StringComparer.Ordinal);
    private readonly HashSet<string> _nativeReflectionMetadata = new(StringComparer.Ordinal);
    private readonly HashSet<string> _nativeReflectionDefinitions = new(StringComparer.Ordinal);
    private readonly Dictionary<string, List<Module>> _reflectionDefinitionOwners = new(StringComparer.Ordinal);
    private readonly HashSet<string> _emittedReflectionDefinitions = new(StringComparer.Ordinal);
    private readonly Dictionary<string, bool> _reflectionDefinitionFormats = new(StringComparer.Ordinal);
    private readonly Dictionary<(int Module, TypeDefinitionHandle Type), bool> _uncompressedMetadataTypes = new();
    private readonly Dictionary<(int Module, TypeDefinitionHandle Type), bool> _noCompressMetadataAttributeTypes = new();
    private bool _reflectionMetadataFrozen;
    private bool _reflectionDefinitionsPrepared;

    internal void FreezeReflectionMetadataSelection() => _reflectionMetadataFrozen = true;

    private void ConfigureReflectionMetadata(IReadOnlyList<string>? specifications)
    {
        foreach (string specification in specifications ?? Array.Empty<string>())
        {
            int separator = specification.LastIndexOf('=');
            if (separator <= 0 || separator + 1 == specification.Length)
                throw new NotSupportedException("--reflection-metadata expects <type>=native|packed.");
            string selector = specification.Substring(0, separator);
            string format = specification.Substring(separator + 1);
            if (format is not ("native" or "packed"))
                throw new NotSupportedException($"Unknown reflection metadata format '{format}'; use native or packed.");
            if (!_reflectionMetadataFormats.TryAdd(selector, format == "native"))
                throw new NotSupportedException($"Duplicate --reflection-metadata selector '{selector}'.");
        }
    }

    private static string MetadataDefinitionName(Module module, TypeDefinitionHandle handle)
    {
        var reader = module.Reader;
        var definition = reader.GetTypeDefinition(handle);
        string name = reader.GetString(definition.Name);
        if (!definition.GetDeclaringType().IsNil)
            return MetadataDefinitionName(module, definition.GetDeclaringType()) + "+" + name;
        string ns = reader.GetString(definition.Namespace);
        return ns.Length == 0 ? name : ns + "." + name;
    }

    // Selectors describe exact CLR shapes; they never expand a generic family.
    private static string MetadataTypeName(TypeDesc type) => type.Kind switch
    {
        TypeKind.Class => MetadataClassName(type.Class!),
        TypeKind.Template => MetadataDefinitionName(type.TemplateModule!, type.TemplateHandle),
        TypeKind.Primitive => "System." + type.Primitive,
        TypeKind.External => type.ExternalName!,
        TypeKind.SZArray => MetadataTypeName(type.Element!) + "[]",
        TypeKind.MDArray => MetadataTypeName(type.Element!) + "[" + new string(',', type.Rank - 1) + "]",
        TypeKind.Pointer => MetadataTypeName(type.Element!) + "*",
        TypeKind.ByRef => MetadataTypeName(type.Element!) + "&",
        _ => type.ToString(),
    };

    private static string MetadataClassName(ClassInfo cls)
    {
        string name = cls.Handle.IsNil ? cls.FullName : MetadataDefinitionName(cls.Module, cls.Handle);
        if (cls.Context.TypeArgs.Length > 0)
            name += "[" + string.Join(",", cls.Context.TypeArgs.Select(MetadataTypeName)) + "]";
        return name;
    }

    private string MetadataAssemblyName(TypeDesc type) => type.Kind switch
    {
        TypeKind.Class => type.Class!.Module.AssemblyName,
        TypeKind.Template => type.TemplateModule!.AssemblyName,
        TypeKind.SZArray or TypeKind.MDArray or TypeKind.Pointer or TypeKind.ByRef => MetadataAssemblyName(type.Element!),
        TypeKind.External when OpenGenericDefHandleByName(type.ExternalName!) is { } definition => definition.Module.AssemblyName,
        _ => "System.Private.CoreLib",
    };

    private string MetadataStorageIdentity(TypeDesc type)
    {
        if (OpenGenericDefBacktickNameOf(type) is { } definition)
            return "definition:" + MetadataAssemblyName(type) + "::" + definition;
        return type.Kind switch
        {
            TypeKind.Class => "class:" + type.Class!.Module.Index + ":" + type.Class.CppName,
            TypeKind.SZArray or TypeKind.MDArray => "array:" + ArrayElemMangle(type),
            _ => MetadataAssemblyName(type) + "::" + MetadataTypeName(type),
        };
    }

    private void AddReflectionDefinitionOwner(string name, Module module)
    {
        if (_reflectionDefinitionsPrepared)
            throw new InvalidOperationException("Generic definition ownership must be collected before its metadata format is prepared.");
        if (!_reflectionDefinitionOwners.TryGetValue(name, out var owners))
        {
            owners = new List<Module>();
            _reflectionDefinitionOwners[name] = owners;
        }
        if (!owners.Contains(module))
            owners.Add(module);
    }

    internal void NoteEmittedReflectionDefinition(string name, Module module)
    {
        _emittedReflectionDefinitions.Add(name);
        AddReflectionDefinitionOwner(name, module);
    }

    internal void PrepareReflectionDefinitionFormats()
    {
        _emittedReflectionDefinitions.UnionWith(TypeofOpenGenericDefs.Keys);
        foreach (string name in _emittedReflectionDefinitions.OrderBy(name => name, StringComparer.Ordinal))
        {
            bool? selected = null;
            bool attributed = false;
            var owners = _reflectionDefinitionOwners.TryGetValue(name, out var recorded)
                ? recorded : new List<Module>();
            if (owners.Count == 0 && OpenGenericDefHandleByName(name) is { } definition)
                owners.Add(definition.Module);
            if (owners.Count == 0)
                selected = ReflectionMetadataOverride(name, "System.Private.CoreLib", "definition:System.Private.CoreLib::" + name);
            foreach (var module in owners.OrderBy(module => module.Index))
            {
                if (RawMetadataDefinition(module, name) is { } handle)
                    attributed |= HasUncompressedMetadata(module, handle);
                bool? choice = ReflectionMetadataOverride(name, module.AssemblyName,
                    "definition:" + module.AssemblyName + "::" + name);
                if (choice is null)
                    continue;
                if (selected is not null && selected != choice)
                    throw new NotSupportedException($"Conflicting --reflection-metadata formats for shared generic definition '{name}'; its runtime identity is shared across assemblies.");
                selected = choice;
            }
            _reflectionDefinitionFormats[name] = !CompressMetadata
                || (selected ?? (attributed || _nativeReflectionDefinitions.Contains(name)));
        }
        _reflectionDefinitionsPrepared = true;
    }

    private void NoteStaticTypeofMetadata(MethodInfo method, List<Instruction> instructions,
        MethodBodyBlock body, BranchLiveness? liveness)
    {
        HashSet<int>? targets = null;
        for (int i = 0; i < instructions.Count; i++)
        {
            var token = instructions[i];
            if (token.OpCode != ILOpCode.Ldtoken
                || liveness is not null && (!liveness.LiveAt(token.Offset) || liveness.ElidedAt(token.Offset)))
                continue;
            var handle = SRME.EntityHandle(token.Token);
            if (handle.Kind is not (HandleKind.TypeDefinition or HandleKind.TypeReference or HandleKind.TypeSpecification))
                continue;
            var type = ResolveTypeTokenForScan(method.Module, handle, method.Context);
            if (type is null || ContainsCanonPlaceholder(type) || ContainsGenericVar(type))
                continue;
            string? openDefinition = OpenGenericDefBacktickNameOf(type);
            if (openDefinition is not null)
            {
                Module? owner = type.Class?.Module ?? type.TemplateModule
                    ?? OpenGenericDefHandleByName(openDefinition)?.Module;
                if (owner is not null)
                    AddReflectionDefinitionOwner(openDefinition, owner);
            }
            int next = i + 1;
            while (next < instructions.Count && instructions[next].OpCode == ILOpCode.Nop)
                next++;
            if (next == instructions.Count || instructions[next].OpCode != ILOpCode.Call
                || ClassifyTypeIdentityCall(method.Module, instructions[next].Token) != TypeIdentityCall.GetTypeFromHandle)
                continue;
            if (targets is null)
            {
                targets = new HashSet<int>();
                foreach (var instruction in instructions)
                {
                    if (instruction.SwitchTargets is { } branches)
                        foreach (int target in branches) targets.Add(target);
                    else if (ILDecoder.IsBranch(instruction.OpCode))
                        targets.Add((int)instruction.Operand);
                }
                foreach (var region in body.ExceptionRegions)
                {
                    targets.Add(region.HandlerOffset);
                    if (region.Kind == ExceptionRegionKind.Filter) targets.Add(region.FilterOffset);
                }
            }
            bool entered = false;
            for (int j = i + 1; j <= next; j++)
                entered |= targets.Contains(instructions[j].Offset);
            if (entered)
                continue;
            string identity = MetadataStorageIdentity(type);
            if (_reflectionMetadataFrozen && !_nativeReflectionMetadata.Contains(identity))
                throw new InvalidOperationException("A static typeof metadata choice was discovered after metadata emission began.");
            _nativeReflectionMetadata.Add(identity);
            if (openDefinition is not null)
                _nativeReflectionDefinitions.Add(openDefinition);
        }
    }

    internal bool UsesNativeReflectionMetadata(ClassInfo cls) => UsesNativeReflectionMetadata(TypeDesc.MakeClass(cls));

    internal bool UsesNativeReflectionMetadata(TypeDesc type)
    {
        if (ContainsCanonPlaceholder(type) || ContainsGenericVar(type))
            return !CompressMetadata;
        string identity = MetadataStorageIdentity(type);
        bool? selected = ReflectionMetadataOverride(MetadataTypeName(type), MetadataAssemblyName(type), identity);
        return !CompressMetadata || (selected ?? (HasUncompressedMetadata(type)
            || _nativeReflectionMetadata.Contains(identity)));
    }

    internal bool UsesNativeReflectionMetadata(string definitionName)
    {
        if (!_reflectionDefinitionFormats.TryGetValue(definitionName, out bool native))
            throw new InvalidOperationException("A generic definition's metadata format was not prepared before emission.");
        return native;
    }

    private bool HasUncompressedMetadata(TypeDesc type)
    {
        if (type.Kind == TypeKind.Class && !type.Class!.Handle.IsNil)
            return HasUncompressedMetadata(type.Class.Module, type.Class.Handle);
        if (type.Kind == TypeKind.Template)
            return HasUncompressedMetadata(type.TemplateModule!, type.TemplateHandle);
        return false;
    }

    // Storage policy reads raw definitions: resolving ClassInfo here can grow the
    // emitted program after reachability and generic planning have finished.
    private bool HasUncompressedMetadata(Module module, TypeDefinitionHandle handle)
    {
        if (_uncompressedMetadataTypes.TryGetValue((module.Index, handle), out bool cached))
            return cached;
        var path = new HashSet<(int Module, TypeDefinitionHandle Type)>();
        bool native = false;
        while (path.Add((module.Index, handle)))
        {
            if (_uncompressedMetadataTypes.TryGetValue((module.Index, handle), out native))
                break;
            var definition = module.Reader.GetTypeDefinition(handle);
            foreach (var attributeHandle in definition.GetCustomAttributes())
            {
                var attribute = module.Reader.GetCustomAttribute(attributeHandle);
                EntityHandle attributeType = attribute.Constructor.Kind switch
                {
                    HandleKind.MethodDefinition => module.Reader.GetMethodDefinition(
                        (MethodDefinitionHandle)attribute.Constructor).GetDeclaringType(),
                    HandleKind.MemberReference => module.Reader.GetMemberReference(
                        (MemberReferenceHandle)attribute.Constructor).Parent,
                    _ => default,
                };
                if (IsNoCompressMetadataAttribute(module, attributeType))
                {
                    native = true;
                    break;
                }
            }
            if (native || RawMetadataTypeDefinition(module, definition.BaseType) is not { } parent)
                break;
            module = parent.Module;
            handle = parent.Handle;
        }
        foreach (var key in path)
            _uncompressedMetadataTypes[key] = native;
        return native;
    }

    private bool IsNoCompressMetadataAttribute(Module module, EntityHandle handle)
    {
        var path = new HashSet<(int Module, TypeDefinitionHandle Type)>();
        bool matches = false;
        while (!handle.IsNil)
        {
            if (IsNoCompressMetadataAttributeName(module.Reader, handle))
            {
                matches = true;
                break;
            }
            if (RawMetadataTypeDefinition(module, handle) is not { } definition
                || !path.Add((definition.Module.Index, definition.Handle)))
                break;
            if (_noCompressMetadataAttributeTypes.TryGetValue((definition.Module.Index, definition.Handle), out matches))
                break;
            module = definition.Module;
            handle = module.Reader.GetTypeDefinition(definition.Handle).BaseType;
        }
        foreach (var key in path)
            _noCompressMetadataAttributeTypes[key] = matches;
        return matches;
    }

    private static bool IsNoCompressMetadataAttributeName(MetadataReader reader, EntityHandle handle)
    {
        if (handle.Kind == HandleKind.TypeDefinition)
        {
            var definition = reader.GetTypeDefinition((TypeDefinitionHandle)handle);
            return definition.GetDeclaringType().IsNil
                && reader.GetString(definition.Namespace) == "Dn2Cpp.Runtime"
                && reader.GetString(definition.Name) == "NoCompressMetadataAttribute";
        }
        if (handle.Kind == HandleKind.TypeReference)
        {
            var reference = reader.GetTypeReference((TypeReferenceHandle)handle);
            return reference.ResolutionScope.Kind != HandleKind.TypeReference
                && reader.GetString(reference.Namespace) == "Dn2Cpp.Runtime"
                && reader.GetString(reference.Name) == "NoCompressMetadataAttribute";
        }
        return false;
    }

    private TypeDefinitionHandle? RawMetadataDefinition(Module module, string name)
    {
        int cut = name.LastIndexOf('.');
        var key = cut < 0 ? ("", name) : (name.Substring(0, cut), name.Substring(cut + 1));
        if (TypeIndex().TryGetValue(key, out var candidates))
            foreach (var (candidateModule, candidateHandle) in candidates)
                if (candidateModule == module && MetadataDefinitionName(module, candidateHandle) == name)
                    return candidateHandle;
        return null;
    }

    private (Module Module, TypeDefinitionHandle Handle)? RawMetadataTypeDefinition(Module module, EntityHandle handle)
    {
        return RawMetadataTypeDefinition(module, handle, new HashSet<(int, int)>());
    }

    private (Module Module, TypeDefinitionHandle Handle)? RawMetadataTypeDefinition(Module module,
        EntityHandle handle, HashSet<(int, int)> seen)
    {
        if (handle.IsNil || !seen.Add((module.Index, SRME.GetToken(handle))))
            return null;
        if (handle.Kind == HandleKind.TypeDefinition)
            return (module, (TypeDefinitionHandle)handle);
        if (handle.Kind == HandleKind.TypeSpecification)
        {
            var blob = module.Reader.GetBlobReader(module.Reader.GetTypeSpecification((TypeSpecificationHandle)handle).Signature);
            var code = blob.ReadSignatureTypeCode();
            while (code is SignatureTypeCode.RequiredModifier or SignatureTypeCode.OptionalModifier)
            {
                blob.ReadTypeHandle();
                code = blob.ReadSignatureTypeCode();
            }
            if (code == SignatureTypeCode.GenericTypeInstance)
                code = blob.ReadSignatureTypeCode();
            return code == SignatureTypeCode.TypeHandle
                ? RawMetadataTypeDefinition(module, blob.ReadTypeHandle(), seen) : null;
        }
        if (handle.Kind != HandleKind.TypeReference)
            return null;
        var reference = module.Reader.GetTypeReference((TypeReferenceHandle)handle);
        var key = (module.Reader.GetString(reference.Namespace), module.Reader.GetString(reference.Name));
        if (!TypeIndex().TryGetValue(key, out var candidates))
            return null;
        if (reference.ResolutionScope.Kind == HandleKind.TypeReference)
        {
            if (RawMetadataTypeDefinition(module, reference.ResolutionScope, seen) is not { } declaring)
                return null;
            foreach (var (candidateModule, candidateHandle) in candidates)
                if (candidateModule == declaring.Module
                    && candidateModule.Reader.GetTypeDefinition(candidateHandle).GetDeclaringType() == declaring.Handle)
                    return (candidateModule, candidateHandle);
            return null;
        }
        Module? target = reference.ResolutionScope.Kind switch
        {
            HandleKind.AssemblyReference => Modules.FirstOrDefault(m => m.AssemblyName
                == module.Reader.GetString(module.Reader.GetAssemblyReference(
                    (AssemblyReferenceHandle)reference.ResolutionScope).Name)),
            HandleKind.ModuleDefinition => module,
            _ => null,
        };
        foreach (var (candidateModule, candidateHandle) in candidates)
            if (candidateModule == target
                && candidateModule.Reader.GetTypeDefinition(candidateHandle).GetDeclaringType().IsNil)
                return (candidateModule, candidateHandle);
        return null;
    }

    private bool? ReflectionMetadataOverride(string name, string assembly, string identity)
    {
        string qualified = assembly + "::" + name;
        bool? result = null;
        if (_reflectionMetadataFormats.TryGetValue(name, out bool unqualified))
        {
            if (_reflectionMetadataMatches.TryGetValue(name, out string? previous) && previous != identity)
                throw new NotSupportedException($"Ambiguous --reflection-metadata selector '{name}'; qualify it with an assembly simple name and '::'.");
            _reflectionMetadataMatches[name] = identity;
            result = unqualified;
        }
        if (_reflectionMetadataFormats.TryGetValue(qualified, out bool selected))
        {
            if (_reflectionMetadataFormats.ContainsKey(name))
                throw new NotSupportedException($"Overlapping --reflection-metadata selectors '{name}' and '{qualified}'.");
            if (_reflectionMetadataMatches.TryGetValue(qualified, out string? previous) && previous != identity)
                throw new NotSupportedException($"Ambiguous --reflection-metadata selector '{qualified}'; generic argument assembly qualifications are not supported.");
            _reflectionMetadataMatches[qualified] = identity;
            result = selected;
        }
        return result;
    }

    internal void ValidateReflectionMetadataFormats()
    {
        if (_reflectionMetadataFormats.Count == 0)
            return;
        foreach (var cls in Classes)
        {
            if (!CoreIntrinsics.RuntimeOwnsTypeInfo(cls))
                continue;
            string name = MetadataClassName(cls);
            if ((_reflectionMetadataFormats.ContainsKey(name)
                    || _reflectionMetadataFormats.ContainsKey(cls.Module.AssemblyName + "::" + name))
                && !UsesNativeReflectionMetadata(cls))
                throw new NotSupportedException($"--reflection-metadata cannot select packed metadata for runtime-owned type '{name}'; its metadata is always native.");
        }
        foreach (string selector in _reflectionMetadataFormats.Keys.OrderBy(name => name, StringComparer.Ordinal))
            if (!_reflectionMetadataMatches.ContainsKey(selector))
                throw new NotSupportedException($"--reflection-metadata selector '{selector}' did not match an emitted metadata type. This option does not preserve types or members.");
    }
}

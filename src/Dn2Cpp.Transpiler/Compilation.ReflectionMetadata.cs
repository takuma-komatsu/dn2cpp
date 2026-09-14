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
            var owners = _reflectionDefinitionOwners.TryGetValue(name, out var recorded)
                ? recorded : new List<Module>();
            if (owners.Count == 0 && OpenGenericDefHandleByName(name) is { } definition)
                owners.Add(definition.Module);
            if (owners.Count == 0)
                selected = ReflectionMetadataOverride(name, "System.Private.CoreLib", "definition:System.Private.CoreLib::" + name);
            foreach (var module in owners.OrderBy(module => module.Index))
            {
                bool? choice = ReflectionMetadataOverride(name, module.AssemblyName,
                    "definition:" + module.AssemblyName + "::" + name);
                if (choice is null)
                    continue;
                if (selected is not null && selected != choice)
                    throw new NotSupportedException($"Conflicting --reflection-metadata formats for shared generic definition '{name}'; its runtime identity is shared across assemblies.");
                selected = choice;
            }
            _reflectionDefinitionFormats[name] = !CompressMetadata || (selected ?? _nativeReflectionDefinitions.Contains(name));
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
        return UsesNativeReflectionMetadata(MetadataTypeName(type), MetadataAssemblyName(type), MetadataStorageIdentity(type));
    }

    internal bool UsesNativeReflectionMetadata(string definitionName)
    {
        if (!_reflectionDefinitionFormats.TryGetValue(definitionName, out bool native))
            throw new InvalidOperationException("A generic definition's metadata format was not prepared before emission.");
        return native;
    }

    private bool UsesNativeReflectionMetadata(string name, string assembly, string identity)
    {
        bool? selected = ReflectionMetadataOverride(name, assembly, identity);
        return !CompressMetadata || (selected ?? _nativeReflectionMetadata.Contains(identity));
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

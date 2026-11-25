using System.Reflection.Metadata;
using Mono.Cecil;
using MethodDefinition = Mono.Cecil.MethodDefinition;
using MetadataTokens = System.Reflection.Metadata.Ecma335.MetadataTokens;

namespace Dn2Cpp;

internal sealed partial class AssemblyDiet
{
    private RawAssemblyTypeResolver<DietAssembly>? _accessorTypes;
    private RawAssemblyTypeResolver<DietAssembly> AccessorTypes =>
        _accessorTypes ??= new RawAssemblyTypeResolver<DietAssembly>(_assemblies.Select(a =>
            (a, a.Assembly.Name.Name, a.PE.GetMetadataReader())));
    private readonly HashSet<MethodDefinition> _checkedUnsafeAccessors = new();

    private void ReachUnsafeAccessor(MethodDefinition accessor)
    {
        if (!accessor.IsStatic || accessor.RVA != 0 || accessor.IsPInvokeImpl || !accessor.HasCustomAttributes
            || !_byModule.TryGetValue(accessor.Module, out var source) || !_checkedUnsafeAccessors.Add(accessor)) return;
        var binding = UnsafeAccessorBinding.Bind(source, MetadataTokens.MethodDefinitionHandle((int)accessor.MetadataToken.RID),
            static module => module.PE.GetMetadataReader(),
            (module, token, checkReferences) => AccessorTypes.ResolveType(module, token, AccessorTypeCandidates, checkReferences),
            static (module, type) => (Mono.Cecil.TypeDefinition)module.Assembly.MainModule.LookupToken(MetadataTokens.GetToken(type)) switch
            {
                { IsEnum: true } => UnsafeAccessorBinding.TypeShape.Enum,
                { IsValueType: true } => UnsafeAccessorBinding.TypeShape.Value,
                _ => UnsafeAccessorBinding.TypeShape.Reference,
            },
            (assembly, name) => AccessorTypes.ResolveNamed(source, assembly, name, AccessorTypeCandidates),
            (assembly, ns, name, definition) => AccessorTypes.ForwardsTo(assembly, ns, name, definition));
        if (binding is not { } target) return;
        var member = target.Module.Assembly.MainModule.LookupToken(MetadataTokens.GetToken(target.Member));
        if (member is FieldReference field) MarkField(field);
        else MarkMethod((MethodDefinition)member);
    }

    private IEnumerable<(DietAssembly, TypeDefinitionHandle)> AccessorTypeCandidates(string ns, string name)
    {
        foreach (var module in _assemblies)
        {
            Spend();
            if (module.Assembly.MainModule.GetType(ns, name) is { } type)
                yield return (module, MetadataTokens.TypeDefinitionHandle((int)type.MetadataToken.RID));
        }
    }
}

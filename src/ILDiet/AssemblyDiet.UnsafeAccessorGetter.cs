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
    private readonly HashSet<MethodDefinition> _checkedGetterAccessors = new();

    private void ReachUnsafeAccessorGetter(MethodDefinition accessor)
    {
        if (!accessor.IsStatic || accessor.RVA != 0 || accessor.IsPInvokeImpl || !accessor.HasCustomAttributes
            || !_byModule.TryGetValue(accessor.Module, out var source) || !_checkedGetterAccessors.Add(accessor)) return;
        var binding = UnsafeAccessorGetter.Bind(source, MetadataTokens.MethodDefinitionHandle((int)accessor.MetadataToken.RID),
            static module => module.PE.GetMetadataReader(), ResolveAccessorType,
            static (module, type) => module.Assembly.MainModule.LookupToken(MetadataTokens.GetToken(type))
                is Mono.Cecil.TypeDefinition { IsValueType: false, IsEnum: false },
            (assembly, ns, name, definition) => AccessorTypes.ForwardsTo(assembly, ns, name, definition));
        if (binding is { } target)
            MarkMethod((MethodDefinition)target.Module.Assembly.MainModule.LookupToken(MetadataTokens.GetToken(target.Method)));
    }

    private (DietAssembly Module, TypeDefinitionHandle Type)? ResolveAccessorType(DietAssembly source, EntityHandle token)
    {
        if (token.Kind == HandleKind.TypeDefinition) return (source, (TypeDefinitionHandle)token);
        if (token.Kind != HandleKind.TypeReference) return null;
        var reader = source.PE.GetMetadataReader();
        var reference = reader.GetTypeReference((TypeReferenceHandle)token);
        string name = reader.GetString(reference.Name), ns = reader.GetString(reference.Namespace);
        var scope = reference.ResolutionScope;
        if (scope.Kind == HandleKind.TypeReference)
        {
            if (ResolveAccessorType(source, scope) is not { } parent) return null;
            var owner = (Mono.Cecil.TypeDefinition)parent.Module.Assembly.MainModule.LookupToken(MetadataTokens.GetToken(parent.Type));
            var nested = owner.NestedTypes.FirstOrDefault(t => t.Name == name && t.Namespace == ns);
            return nested is null ? null : (parent.Module, MetadataTokens.TypeDefinitionHandle((int)nested.MetadataToken.RID));
        }
        if (scope.Kind == HandleKind.ModuleDefinition || scope.IsNil)
            return source.Assembly.MainModule.GetType(ns, name) is { } local
                ? (source, MetadataTokens.TypeDefinitionHandle((int)local.MetadataToken.RID)) : null;
        if (scope.Kind != HandleKind.AssemblyReference) return null;
        // Cecil's missing-facade fallback can select a same-named type in an unrelated assembly.
        var assembly = (AssemblyReferenceHandle)scope;
        return AccessorTypes.Resolve(reader.GetString(reader.GetAssemblyReference(assembly).Name), ns, name,
            Candidates(), reader, assembly);

        IEnumerable<(DietAssembly, TypeDefinitionHandle)> Candidates()
        {
            foreach (var module in _assemblies)
            {
                Spend();
                if (module.Assembly.MainModule.GetType(ns, name) is { } type)
                    yield return (module, MetadataTokens.TypeDefinitionHandle((int)type.MetadataToken.RID));
            }
        }
    }
}

using Mono.Cecil;
using System.Reflection.Metadata;
using MetadataTokens = System.Reflection.Metadata.Ecma335.MetadataTokens;
using MethodDefinition = Mono.Cecil.MethodDefinition;

namespace Dn2Cpp;

internal sealed partial class AssemblyDiet
{
    private readonly HashSet<MethodDefinition> _defaultPrincipalEdges = new();

    private void ReachDefaultPrincipalTarget(MethodDefinition method)
    {
        if (method.Name != DefaultPrincipalContract.AccessorName
            || method.DeclaringType.FullName != "System.AppDomain"
            || !_byModule.TryGetValue(method.Module, out var source)
            || !DefaultPrincipalContract.IsAccessor(source.PE.GetMetadataReader(),
                MetadataTokens.MethodDefinitionHandle(checked((int)method.MetadataToken.RID)))
            || !_defaultPrincipalEdges.Add(method)) return;
        var target = _assemblies.SingleOrDefault(a => a.Assembly.Name.Name == DefaultPrincipalContract.TargetAssembly)
            ?? throw new NotSupportedException("default principal requires the System.Security.Claims implementation assembly");
        var handle = DefaultPrincipalContract.FindTarget(target.PE.GetMetadataReader());
        if (handle.IsNil)
            throw new NotSupportedException("default principal requires the exact managed GenericPrincipal.GetDefaultInstance factory");
        MarkMethod((MethodDefinition)target.Assembly.MainModule.LookupToken(MetadataTokens.GetToken(handle)));
    }
}

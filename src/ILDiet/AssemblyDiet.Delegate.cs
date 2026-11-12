using Mono.Cecil;
using MetadataTokens = System.Reflection.Metadata.Ecma335.MetadataTokens;
using MetadataReaderExtensions = System.Reflection.Metadata.PEReaderExtensions;

namespace Dn2Cpp;

internal sealed partial class AssemblyDiet
{
    private bool IsCopiedDelegateCallableBody(MethodDefinition method, string owner)
    {
        if (!DelegateCallableBodyContract.IsName(owner, method.Name) || method.DeclaringType.FullName != owner
            || method.Module.Assembly?.Name.Name != "System.Private.CoreLib"
            || method.MetadataToken.TokenType != TokenType.Method || method.MetadataToken.RID == 0
            || !_byModule.TryGetValue(method.Module, out var assembly)) return false;
        return DelegateCallableBodyContract.IsDefinition(MetadataReaderExtensions.GetMetadataReader(assembly.PE),
            MetadataTokens.MethodDefinitionHandle((int)method.MetadataToken.RID), owner, method.Name);
    }
}

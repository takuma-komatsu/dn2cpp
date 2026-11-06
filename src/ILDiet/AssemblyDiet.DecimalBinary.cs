using Mono.Cecil;
using MetadataTokens = System.Reflection.Metadata.Ecma335.MetadataTokens;
using MetadataReaderExtensions = System.Reflection.Metadata.PEReaderExtensions;

namespace Dn2Cpp;

internal sealed partial class AssemblyDiet
{
    private bool IsCopiedDecimalBinaryBody(MethodDefinition method, string owner)
    {
        int count = DecimalBinaryContract.ParameterCount(owner, method.Name);
        if (count < 0 || method.Parameters.Count != count || method.Module.Assembly?.Name.Name != "System.Private.CoreLib"
            || method.DeclaringType.FullName != owner || method.MetadataToken.TokenType != TokenType.Method
            || method.MetadataToken.RID == 0 || !_byModule.TryGetValue(method.Module, out var assembly)) return false;
        return DecimalBinaryContract.IsBodyDefinition(MetadataReaderExtensions.GetMetadataReader(assembly.PE), assembly.PE,
            MetadataTokens.MethodDefinitionHandle((int)method.MetadataToken.RID), owner, method.Name);
    }
}

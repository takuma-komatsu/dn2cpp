using Mono.Cecil;
using MetadataTokens = System.Reflection.Metadata.Ecma335.MetadataTokens;
using MetadataReaderExtensions = System.Reflection.Metadata.PEReaderExtensions;

namespace Dn2Cpp;

internal sealed partial class AssemblyDiet
{
    private bool IsCopiedPrimitiveParseBody(MethodDefinition method, string owner)
    {
        if (!PrimitiveParseContract.IsBodyCandidate(owner, method.Name, (System.Reflection.MethodAttributes)method.Attributes,
                PrimitiveParseContract.IsFormatHelper(owner, method.Name) ? method.Parameters.Count : -1,
                PrimitiveParseContract.IsCharNumber(owner, method.Name) && method.Parameters.Count != 0
                    && method.Parameters[0].ParameterType.MetadataType == MetadataType.String)
            || method.Module.Assembly?.Name.Name != "System.Private.CoreLib" || method.DeclaringType.FullName != owner
            || method.MetadataToken.TokenType != TokenType.Method || method.MetadataToken.RID == 0
            || !_byModule.TryGetValue(method.Module, out var assembly)) return false;
        return PrimitiveParseContract.IsBodyDefinition(MetadataReaderExtensions.GetMetadataReader(assembly.PE), assembly.PE,
            MetadataTokens.MethodDefinitionHandle((int)method.MetadataToken.RID), owner, method.Name);
    }
}

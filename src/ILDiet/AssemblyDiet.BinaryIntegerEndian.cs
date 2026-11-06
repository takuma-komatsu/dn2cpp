using Mono.Cecil;
using MetadataTokens = System.Reflection.Metadata.Ecma335.MetadataTokens;
using MetadataReaderExtensions = System.Reflection.Metadata.PEReaderExtensions;

namespace Dn2Cpp;

internal sealed partial class AssemblyDiet
{
    private bool IsCopiedBinaryIntegerEndianBody(MethodDefinition method, string owner)
    {
        if (owner == "System.ThrowHelper" && method.Name == "ThrowOverflowException"
            && method.Module.Assembly?.Name.Name == "System.Private.CoreLib"
            && method.MetadataToken.TokenType == TokenType.Method && method.MetadataToken.RID != 0
            && _byModule.TryGetValue(method.Module, out var helperAssembly))
            return BinaryIntegerEndianContract.IsOverflowHelperDefinition(
                MetadataReaderExtensions.GetMetadataReader(helperAssembly.PE), helperAssembly.PE,
                MetadataTokens.MethodDefinitionHandle((int)method.MetadataToken.RID));
        if (!BinaryIntegerEndianContract.IsLeafCandidate(owner, method.Name)
            || method.Module.Assembly?.Name.Name != "System.Private.CoreLib"
            || method.DeclaringType.FullName != owner || !method.DeclaringType.IsValueType
            || method.DeclaringType.IsEnum || method.DeclaringType.IsInterface || method.DeclaringType.HasGenericParameters
            || method.HasGenericParameters || method.HasThis || method.ExplicitThis
            || method.CallingConvention != MethodCallingConvention.Default
            || method.ReturnType.MetadataType != MetadataType.Boolean || method.Parameters.Count != 3
            || method.Parameters[0].ParameterType is not GenericInstanceType span
            || span.ElementType.FullName != "System.ReadOnlySpan`1" || span.GenericArguments.Count != 1
            || span.GenericArguments[0].MetadataType != MetadataType.Byte
            || method.Parameters[1].ParameterType.MetadataType != MetadataType.Boolean
            || method.Parameters[2].ParameterType is not ByReferenceType output
            || (int)output.ElementType.MetadataType != (int)BinaryIntegerEndianContract.Primitive(owner)
            || method.MetadataToken.TokenType != TokenType.Method || method.MetadataToken.RID == 0
            || !_byModule.TryGetValue(method.Module, out var assembly)) return false;
        var reader = MetadataReaderExtensions.GetMetadataReader(assembly.PE);
        return BinaryIntegerEndianContract.IsLeafDefinition(reader,
            MetadataTokens.MethodDefinitionHandle((int)method.MetadataToken.RID), owner, method.Name);
    }
}

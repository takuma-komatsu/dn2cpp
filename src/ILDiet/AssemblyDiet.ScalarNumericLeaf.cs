using Mono.Cecil;
using MetadataTokens = System.Reflection.Metadata.Ecma335.MetadataTokens;
using MetadataReaderExtensions = System.Reflection.Metadata.PEReaderExtensions;

namespace Dn2Cpp;

internal sealed partial class AssemblyDiet
{
    private bool IsCopiedScalarNumericLeaf(MethodDefinition method, string owner)
    {
        if (!ScalarNumericLeafContract.IsCandidate(owner, method.Name)
            || method.DeclaringType.FullName != owner || method.DeclaringType.DeclaringType is not null
            || !method.DeclaringType.IsValueType || method.DeclaringType.IsEnum || method.DeclaringType.IsInterface
            || method.DeclaringType.HasGenericParameters || method.HasGenericParameters
            || method.Module.Assembly?.Name.Name != "System.Private.CoreLib"
            || (int)method.Attributes != (int)ScalarNumericLeafContract.Attributes(owner, method.Name)
            || method.ImplAttributes != MethodImplAttributes.IL || !method.HasBody || method.RVA == 0
            || method.IsPInvokeImpl || method.HasPInvokeInfo || method.ExplicitThis
            || method.CallingConvention != MethodCallingConvention.Default
            || method.HasThis != ScalarNumericLeafContract.IsInstance(method.Name)
            || method.ReturnType is TypeSpecification
            || (int)method.ReturnType.MetadataType != (int)ScalarNumericLeafContract.Result(owner, method.Name)
            || method.Parameters.Count != (method.HasThis ? 0 : 1)
            || !method.HasThis && (method.Parameters[0].ParameterType is TypeSpecification
                || (int)method.Parameters[0].ParameterType.MetadataType != (int)ScalarNumericLeafContract.Parameter(owner))
            || method.MetadataToken.TokenType != TokenType.Method || method.MetadataToken.RID == 0
            || !_byModule.TryGetValue(method.Module, out var assembly)) return false;
        var reader = MetadataReaderExtensions.GetMetadataReader(assembly.PE);
        var handle = MetadataTokens.MethodDefinitionHandle((int)method.MetadataToken.RID);
        return ScalarNumericLeafContract.IsDefinition(reader, handle, owner, method.Name)
            && ScalarNumericLeafContract.IsBody(reader, assembly.PE, handle, owner, method.Name);
    }
}

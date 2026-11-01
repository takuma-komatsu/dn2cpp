using System.Reflection;
using System.Reflection.Metadata;
using SRME = System.Reflection.Metadata.Ecma335.MetadataTokens;

namespace Dn2Cpp;

internal sealed partial class Compilation
{
    private void ValidateNumberMathGuard(MethodInfo method, ClassInfo vector, MethodInfo count)
    {
        var owner = vector.Module.Reader.GetTypeDefinition(vector.Handle);
        if (!vector.Module.Reader.StringComparer.Equals(owner.Namespace, "System.Numerics")
            || !vector.Module.Reader.StringComparer.Equals(owner.Name, "Vector`1"))
            throw VectorMagnitudeGuardUnsupported();
        string next = method.Name switch
        {
            "MaxNumber" => "Max",
            "MinNumber" => "Min",
            "Max" => "GreaterThan",
            "Min" => "LessThan",
            _ => throw VectorMagnitudeGuardUnsupported(),
        };
        var il = GenericVectorGuardInstructions(method);
        int start = ValidateMagnitudeTypeTests(method, il, 1, new[] { "Single", "Double" });
        if (start + 3 >= il.Count || il[start].OpCode != ILOpCode.Ldarg_0
            || il[start + 1].OpCode != ILOpCode.Ldarg_1 || il[start + 2].OpCode != ILOpCode.Constrained
            || il[start + 3].OpCode != ILOpCode.Call) throw VectorMagnitudeGuardUnsupported();
        ValidateMagnitudeTypeVariable(method, il[start + 2].Token, 0);
        var bridge = ResolveNumberBinaryBridge(method, il[start + 3].Token, vector, next);
        var body = GenericVectorGuardInstructions(bridge);
        if (body.Count != 4 || body[0].OpCode != ILOpCode.Ldarg_0 || body[1].OpCode != ILOpCode.Ldarg_1
            || body[2].OpCode != ILOpCode.Call || body[3].OpCode != ILOpCode.Ret)
            throw VectorMagnitudeGuardUnsupported();
        var operation = ResolveNumberBinaryCall(bridge, body[2].Token, vector, next);
        // The comparison is evaluated before ConditionalSelect on the unsupported branch.
        var actualCount = next is "Max" or "Min" ? ResolveMagnitudePublicGuard(operation)
            : ResolveMagnitudeLoopGuard(operation, GenericVectorGuardInstructions(operation), 0);
        if (actualCount != count) throw VectorMagnitudeGuardUnsupported();
    }

    private static bool NumberInterfaceSignature(BlobReader raw)
    {
        if (raw.RemainingBytes != 8 || raw.ReadByte() != 0 || raw.ReadByte() != 2) return false;
        for (int i = 0; i < 3; i++)
            if (raw.ReadByte() != 0x13 || raw.ReadByte() != 0) return false;
        return true;
    }

    private MethodInfo ResolveNumberBinaryBridge(MethodInfo method, int token, ClassInfo vector, string name)
    {
        var module = method.Module;
        var reader = module.Reader;
        var handle = SRME.EntityHandle(token);
        if (handle.Kind != HandleKind.MemberReference) throw VectorMagnitudeGuardUnsupported();
        var reference = reader.GetMemberReference((MemberReferenceHandle)handle);
        var raw = reader.GetBlobReader(reference.Signature);
        if (!reader.StringComparer.Equals(reference.Name, name) || reference.Parent.Kind != HandleKind.TypeSpecification
            || !NumberInterfaceSignature(raw))
            throw VectorMagnitudeGuardUnsupported();
        var target = ResolveMemberRefMethod(module, (MemberReferenceHandle)handle, method.Context);
        var contract = target.DeclaringClass;
        if (target.Module != module || contract.Module != module || contract.Handle.IsNil) throw VectorMagnitudeGuardUnsupported();
        var contractDefinition = reader.GetTypeDefinition(contract.Handle);
        if (!contractDefinition.GetDeclaringType().IsNil || contractDefinition.GetGenericParameters().Count != 2
            || !reader.StringComparer.Equals(contractDefinition.Namespace, "System.Runtime.Intrinsics")
            || !reader.StringComparer.Equals(contractDefinition.Name, "ISimdVector`2")
            || !contract.IsInterface || contract.Context.TypeArgs is not [var self, var element]
            || self.Class != vector || element.Class != vector.Context.TypeArgs[0].Class
            || target.Name != name || !target.IsStatic || target.Context.MethodArgs.Length != 0
            || target.Signature.ReturnType.Class != vector || target.Signature.ParameterTypes is not [var left, var right]
            || left.Class != vector || right.Class != vector) throw VectorMagnitudeGuardUnsupported();
        var targetDefinition = reader.GetMethodDefinition(target.Handle);
        var targetRaw = reader.GetBlobReader(targetDefinition.Signature);
        if (targetDefinition.GetDeclaringType() != contract.Handle
            || targetDefinition.Attributes != (MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.Virtual
                | MethodAttributes.HideBySig | MethodAttributes.Abstract)
            || targetDefinition.ImplAttributes != MethodImplAttributes.IL || targetDefinition.RelativeVirtualAddress != 0
            || targetDefinition.GetGenericParameters().Count != 0 || target.PInvoke is not null || target.IsSynthetic
            || !NumberInterfaceSignature(targetRaw))
            throw VectorMagnitudeGuardUnsupported();
        MethodInfo? bridge = null;
        ValidateMagnitudeNoInitializer(vector);
        foreach (var implementationHandle in reader.GetTypeDefinition(vector.Handle).GetMethodImplementations())
        {
            var implementation = reader.GetMethodImplementation(implementationHandle);
            if (implementation.MethodDeclaration.Kind != HandleKind.MemberReference) continue;
            var declaration = reader.GetMemberReference((MemberReferenceHandle)implementation.MethodDeclaration);
            if (!reader.StringComparer.Equals(declaration.Name, name)) continue;
            if (!NumberInterfaceSignature(reader.GetBlobReader(declaration.Signature))) throw VectorMagnitudeGuardUnsupported();
            var declared = ResolveMemberRefMethod(module, (MemberReferenceHandle)implementation.MethodDeclaration, vector.Context);
            if (declared.DeclaringClass != contract || declared.Handle != target.Handle) continue;
            if (bridge is not null || implementation.MethodBody.Kind != HandleKind.MethodDefinition)
                throw VectorMagnitudeGuardUnsupported();
            var definition = reader.GetMethodDefinition((MethodDefinitionHandle)implementation.MethodBody);
            if (definition.GetDeclaringType() != vector.Handle
                || definition.Attributes != (MethodAttributes.Private | MethodAttributes.Static | MethodAttributes.HideBySig)
                || definition.ImplAttributes != MethodImplAttributes.IL || definition.GetGenericParameters().Count != 0)
                throw VectorMagnitudeGuardUnsupported();
            var signature = reader.GetBlobReader(definition.Signature);
            if (signature.RemainingBytes < 2 || signature.ReadByte() != 0 || signature.ReadByte() != 2
                || !ReadVectorValueType(module, ref signature, vector.Context, vector)
                || !ReadVectorValueType(module, ref signature, vector.Context, vector)
                || !ReadVectorValueType(module, ref signature, vector.Context, vector) || signature.RemainingBytes != 0)
                throw VectorMagnitudeGuardUnsupported();
            bridge = InstantiateMethodOnClass(vector, module, (MethodDefinitionHandle)implementation.MethodBody, Array.Empty<TypeDesc>());
            ValidateMagnitudeResolvedDefinition(bridge, module, (MethodDefinitionHandle)implementation.MethodBody);
            if (bridge is null || bridge.DeclaringClass != vector || bridge.Module != module
                || bridge.Signature.ReturnType.Class != vector || bridge.Signature.ParameterTypes is not [var first, var second]
                || first.Class != vector || second.Class != vector) throw VectorMagnitudeGuardUnsupported();
        }
        return bridge ?? throw VectorMagnitudeGuardUnsupported();
    }

    private MethodInfo ResolveNumberBinaryCall(MethodInfo bridge, int token, ClassInfo vector, string name)
    {
        var module = bridge.Module;
        var reader = module.Reader;
        var handle = SRME.EntityHandle(token);
        if (handle.Kind != HandleKind.MethodSpecification) throw VectorMagnitudeGuardUnsupported();
        var spec = reader.GetMethodSpecification((MethodSpecificationHandle)handle);
        if (spec.Method.Kind != HandleKind.MethodDefinition) throw VectorMagnitudeGuardUnsupported();
        var definition = reader.GetMethodDefinition((MethodDefinitionHandle)spec.Method);
        var owner = reader.GetTypeDefinition(definition.GetDeclaringType());
        var vectorDefinition = reader.GetTypeDefinition(vector.Handle);
        string vectorName = reader.GetString(vectorDefinition.Name);
        if (!vectorName.EndsWith("`1", StringComparison.Ordinal)
            || !reader.StringComparer.Equals(owner.Namespace, reader.GetString(vectorDefinition.Namespace))
            || !reader.StringComparer.Equals(owner.Name, vectorName[..^2]) || !owner.GetDeclaringType().IsNil
            || owner.GetGenericParameters().Count != 0 || !reader.StringComparer.Equals(definition.Name, name)
            || definition.Attributes != (MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.HideBySig)
            || definition.ImplAttributes != MethodImplAttributes.AggressiveInlining
            || definition.GetGenericParameters().Count != 1) throw VectorMagnitudeGuardUnsupported();
        foreach (var parameterHandle in definition.GetGenericParameters())
        {
            var parameter = reader.GetGenericParameter(parameterHandle);
            if (parameter.Parent != spec.Method || parameter.Index != 0 || parameter.Attributes != GenericParameterAttributes.None
                || parameter.GetConstraints().Count != 0) throw VectorMagnitudeGuardUnsupported();
        }
        var raw = reader.GetBlobReader(spec.Signature);
        if (raw.RemainingBytes != 4 || raw.ReadByte() != 0x0a || raw.ReadByte() != 1
            || raw.ReadByte() != 0x13 || raw.ReadByte() != 0) throw VectorMagnitudeGuardUnsupported();
        var context = new GenericContext(Array.Empty<TypeDesc>(), new[] { vector.Context.TypeArgs[0] });
        var signature = reader.GetBlobReader(definition.Signature);
        if (signature.RemainingBytes < 3 || signature.ReadByte() != 0x10 || signature.ReadByte() != 1 || signature.ReadByte() != 2
            || !ReadVectorValueType(module, ref signature, context, vector)
            || !ReadVectorValueType(module, ref signature, context, vector)
            || !ReadVectorValueType(module, ref signature, context, vector) || signature.RemainingBytes != 0)
            throw VectorMagnitudeGuardUnsupported();
        var result = ResolveMethodSpec(module, (MethodSpecificationHandle)handle, bridge.Context);
        ValidateMagnitudeResolvedDefinition(result, module, (MethodDefinitionHandle)spec.Method);
        if (result.Module != module || result.PInvoke is not null || result.IsSynthetic
            || result.Context.TypeArgs.Length != 0 || result.Context.MethodArgs is not [var element]
            || element.Class != vector.Context.TypeArgs[0].Class || result.Signature.ReturnType.Class != vector
            || result.Signature.ParameterTypes is not [var first, var second] || first.Class != vector || second.Class != vector)
            throw VectorMagnitudeGuardUnsupported();
        ValidateMagnitudeNoInitializer(result.DeclaringClass);
        return result;
    }

}

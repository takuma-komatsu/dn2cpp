using System.Reflection;
using System.Reflection.Metadata;
using SRME = System.Reflection.Metadata.Ecma335.MetadataTokens;

namespace Dn2Cpp;

internal sealed partial class Compilation
{
    private void ValidateNumberMathGuard(MethodInfo method, ClassInfo vector, MethodInfo count)
    {
        if (!CoreIntrinsics.IsVectorManagedValueClass(vector))
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
        var actualCount = ResolveNumberDependencyGuard(operation);
        if (actualCount != count) throw VectorMagnitudeGuardUnsupported();
    }

    private MethodInfo ResolveNumberDependencyGuard(MethodInfo method)
    {
        if (method.Name is "Max" or "Min") return ResolveMagnitudePublicGuard(method);
        if (method.Name is not ("GreaterThan" or "LessThan")) throw VectorMagnitudeGuardUnsupported();
        var il = GenericVectorGuardInstructions(method);
        return method.DeclaringClass.FullName is "System.Numerics.Vector" or "System.Runtime.Intrinsics.Vector64"
            ? ResolveMagnitudeLoopGuard(method, il, 0)
            : ResolveMagnitudeHalfGuard(method, il, 0, unary: false);
    }

    private static bool NumberInterfaceSignature(BlobReader raw)
    {
        if (raw.RemainingBytes != 8 || raw.ReadByte() != 0 || raw.ReadByte() != 2) return false;
        for (int i = 0; i < 3; i++)
            if (raw.ReadByte() != 0x13 || raw.ReadByte() != 0) return false;
        return true;
    }

    private static bool NumberBinarySignature(MethodInfo method, ClassInfo vector, bool generic)
    {
        var signature = method.Signature;
        return signature.Header.RawValue == (generic ? 0x10 : 0)
            && signature.GenericParameterCount == (generic ? 1 : 0) && signature.RequiredParameterCount == 2
            && signature.ReturnType is { Kind: TypeKind.Class, IsCanonPlaceholder: false } result && result.Class == vector
            && signature.ParameterTypes is [{ Kind: TypeKind.Class, IsCanonPlaceholder: false } left,
                { Kind: TypeKind.Class, IsCanonPlaceholder: false } right]
            && left.Class == vector && right.Class == vector;
    }

    private static TypeDefinitionHandle ReadNumberContractPrefix(Module module, ref BlobReader raw)
    {
        if (raw.RemainingBytes < 4 || raw.ReadByte() != 0x15 || raw.ReadByte() != 0x12
            || !raw.TryReadCompressedInteger(out int token) || token <= 0 || (token & 3) != 0
            || raw.RemainingBytes == 0 || raw.ReadByte() != 2) throw VectorMagnitudeGuardUnsupported();
        var handle = SRME.TypeDefinitionHandle(token >> 2);
        var owner = module.Reader.GetTypeDefinition(handle);
        if (owner.Attributes != (TypeAttributes.NotPublic | TypeAttributes.Interface | TypeAttributes.Abstract | TypeAttributes.BeforeFieldInit)
            || !owner.GetDeclaringType().IsNil || !owner.BaseType.IsNil || owner.GetGenericParameters().Count != 2
            || !module.Reader.StringComparer.Equals(owner.Namespace, "System.Runtime.Intrinsics")
            || !module.Reader.StringComparer.Equals(owner.Name, "ISimdVector`2")) throw VectorMagnitudeGuardUnsupported();
        return handle;
    }

    private static bool NumberImplementationType(Module module, EntityHandle handle, TypeDefinitionHandle contract, ClassInfo vector)
    {
        if (handle.Kind != HandleKind.TypeSpecification) return false;
        var raw = module.Reader.GetBlobReader(module.Reader.GetTypeSpecification((TypeSpecificationHandle)handle).Signature);
        // Only the exact installed closed slot has authority, not a normalized TypeSpec.
        return ReadNumberContractPrefix(module, ref raw) == contract
            && ReadFixedGenericVectorEstimateType(ref raw, vector.Handle, 0x13)
            && raw.RemainingBytes == 2 && raw.ReadByte() == 0x13 && raw.ReadByte() == 0;
    }

    private static void ValidateNumberContractParameters(Module module, TypeDefinitionHandle contract)
    {
        var reader = module.Reader;
        int index = 0;
        foreach (var parameterHandle in reader.GetTypeDefinition(contract).GetGenericParameters())
        {
            var parameter = reader.GetGenericParameter(parameterHandle);
            if (parameter.Parent != contract || parameter.Index != index || parameter.Attributes != GenericParameterAttributes.None
                || parameter.GetConstraints().Count != (index == 0 ? 1 : 0)) throw VectorMagnitudeGuardUnsupported();
            foreach (var constraintHandle in parameter.GetConstraints())
            {
                var constraint = reader.GetGenericParameterConstraint(constraintHandle);
                if (constraint.Parameter != parameterHandle || constraint.Type.Kind != HandleKind.TypeSpecification)
                    throw VectorMagnitudeGuardUnsupported();
                var raw = reader.GetBlobReader(reader.GetTypeSpecification((TypeSpecificationHandle)constraint.Type).Signature);
                if (ReadNumberContractPrefix(module, ref raw) != contract || raw.RemainingBytes != 4
                    || raw.ReadByte() != 0x13 || raw.ReadByte() != 0 || raw.ReadByte() != 0x13 || raw.ReadByte() != 1)
                    throw VectorMagnitudeGuardUnsupported();
            }
            index++;
        }
        if (index != 2) throw VectorMagnitudeGuardUnsupported();
    }

    private MethodInfo ResolveNumberBinaryBridge(MethodInfo method, int token, ClassInfo vector, string name)
    {
        var module = method.Module;
        var reader = module.Reader;
        var handle = SRME.EntityHandle(token);
        if (handle.Kind != HandleKind.MemberReference || vector.Module != module
            || !CoreIntrinsics.IsVectorManagedValueClass(vector)) throw VectorMagnitudeGuardUnsupported();
        var reference = reader.GetMemberReference((MemberReferenceHandle)handle);
        if (!reader.StringComparer.Equals(reference.Name, name) || reference.Parent.Kind != HandleKind.TypeSpecification
            || !NumberInterfaceSignature(reader.GetBlobReader(reference.Signature))) throw VectorMagnitudeGuardUnsupported();
        var parent = reader.GetBlobReader(reader.GetTypeSpecification((TypeSpecificationHandle)reference.Parent).Signature);
        var contractHandle = ReadNumberContractPrefix(module, ref parent);
        if (parent.RemainingBytes != 4 || parent.ReadByte() != 0x1e || parent.ReadByte() != 0
            || parent.ReadByte() != 0x1e || parent.ReadByte() != 1) throw VectorMagnitudeGuardUnsupported();
        ValidateNumberContractParameters(module, contractHandle);
        MethodDefinitionHandle targetHandle = default;
        foreach (var candidate in reader.GetTypeDefinition(contractHandle).GetMethods())
        {
            var definition = reader.GetMethodDefinition(candidate);
            if (!reader.StringComparer.Equals(definition.Name, name)) continue;
            if (!targetHandle.IsNil || definition.GetDeclaringType() != contractHandle
                || definition.Attributes != (MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.Virtual
                    | MethodAttributes.HideBySig | MethodAttributes.Abstract)
                || definition.ImplAttributes != MethodImplAttributes.IL || definition.RelativeVirtualAddress != 0
                || definition.GetGenericParameters().Count != 0
                || !NumberInterfaceSignature(reader.GetBlobReader(definition.Signature))) throw VectorMagnitudeGuardUnsupported();
            targetHandle = candidate;
        }
        if (targetHandle.IsNil) throw VectorMagnitudeGuardUnsupported();
        var target = ResolveMemberRefMethod(module, (MemberReferenceHandle)handle, method.Context);
        ValidateMagnitudeResolvedDefinition(target, module, targetHandle);
        var contract = target.DeclaringClass;
        if (contract.Handle != contractHandle || !contract.IsInterface || contract.IsValueType || contract.IsEnum
            || contract.GenericArity != 2 || contract.Context.TypeArgs is not [var self, var element]
            || self.Kind != TypeKind.Class || self.IsCanonPlaceholder || self.Class != vector
            || element.Kind != TypeKind.Class || element.IsCanonPlaceholder || element.Class != vector.Context.TypeArgs[0].Class
            || target.Context.MethodArgs.Length != 0 || target.Context.TypeArgs is not [var actualSelf, var actualElement]
            || actualSelf.Kind != TypeKind.Class || actualSelf.IsCanonPlaceholder || actualSelf.Class != vector
            || actualElement.Kind != TypeKind.Class || actualElement.IsCanonPlaceholder || actualElement.Class != element.Class
            || !NumberBinarySignature(target, vector, generic: false)) throw VectorMagnitudeGuardUnsupported();
        ValidateMagnitudeNoInitializer(contract);
        ValidateMagnitudeNoInitializer(vector);
        var vectorDefinition = reader.GetTypeDefinition(vector.Handle);
        int interfaces = 0;
        foreach (var interfaceHandle in vectorDefinition.GetInterfaceImplementations())
        {
            var declared = reader.GetInterfaceImplementation(interfaceHandle).Interface;
            if (declared.Kind != HandleKind.TypeSpecification) continue;
            var raw = reader.GetBlobReader(reader.GetTypeSpecification((TypeSpecificationHandle)declared).Signature);
            // Other declared interfaces are unrelated to this proof-only slot.
            if (raw.RemainingBytes < 4 || raw.ReadByte() != 0x15 || raw.ReadByte() != 0x12
                || !raw.TryReadCompressedInteger(out int owner) || owner != SRME.GetRowNumber(contractHandle) << 2) continue;
            if (!NumberImplementationType(module, declared, contractHandle, vector)) throw VectorMagnitudeGuardUnsupported();
            interfaces++;
        }
        if (interfaces != 1) throw VectorMagnitudeGuardUnsupported();
        MethodDefinitionHandle selected = default;
        foreach (var implementationHandle in vectorDefinition.GetMethodImplementations())
        {
            var implementation = reader.GetMethodImplementation(implementationHandle);
            if (implementation.MethodDeclaration.IsNil) throw VectorMagnitudeGuardUnsupported();
            if (implementation.MethodDeclaration.Kind == HandleKind.MethodDefinition)
            {
                var declaration = (MethodDefinitionHandle)implementation.MethodDeclaration;
                // An open MethodDef edge must not hide a second implementation of the closed slot.
                if (declaration == targetHandle || reader.StringComparer.Equals(reader.GetMethodDefinition(declaration).Name, name))
                    throw VectorMagnitudeGuardUnsupported();
                continue;
            }
            if (implementation.MethodDeclaration.Kind != HandleKind.MemberReference) throw VectorMagnitudeGuardUnsupported();
            var declared = reader.GetMemberReference((MemberReferenceHandle)implementation.MethodDeclaration);
            if (!reader.StringComparer.Equals(declared.Name, name)) continue;
            if (!selected.IsNil || implementation.MethodBody.Kind != HandleKind.MethodDefinition
                || !NumberInterfaceSignature(reader.GetBlobReader(declared.Signature))
                || !NumberImplementationType(module, declared.Parent, contractHandle, vector)) throw VectorMagnitudeGuardUnsupported();
            selected = (MethodDefinitionHandle)implementation.MethodBody;
        }
        if (selected.IsNil) throw VectorMagnitudeGuardUnsupported();
        var body = reader.GetMethodDefinition(selected);
        if (body.GetDeclaringType() != vector.Handle
            || body.Attributes != (MethodAttributes.Private | MethodAttributes.Static | MethodAttributes.HideBySig)
            || body.ImplAttributes != MethodImplAttributes.IL || body.RelativeVirtualAddress == 0
            || body.GetGenericParameters().Count != 0) throw VectorMagnitudeGuardUnsupported();
        var signature = reader.GetBlobReader(body.Signature);
        if (signature.RemainingBytes < 2 || signature.ReadByte() != 0 || signature.ReadByte() != 2
            || !ReadFixedGenericVectorEstimateType(ref signature, vector.Handle, 0x13)
            || !ReadFixedGenericVectorEstimateType(ref signature, vector.Handle, 0x13)
            || !ReadFixedGenericVectorEstimateType(ref signature, vector.Handle, 0x13) || signature.RemainingBytes != 0)
            throw VectorMagnitudeGuardUnsupported();
        var bridge = InstantiateMethodOnClass(vector, module, selected, Array.Empty<TypeDesc>());
        ValidateMagnitudeResolvedDefinition(bridge, module, selected);
        if (bridge.DeclaringClass != vector || bridge.Context.MethodArgs.Length != 0
            || bridge.Context.TypeArgs is not [var actual] || actual.Kind != TypeKind.Class
            || actual.IsCanonPlaceholder || actual.Class != element.Class
            || !NumberBinarySignature(bridge, vector, generic: false)) throw VectorMagnitudeGuardUnsupported();
        return bridge;
    }

    private MethodInfo ResolveNumberBinaryCall(MethodInfo bridge, int token, ClassInfo vector, string name, int variable = 0x13)
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
            || definition.RelativeVirtualAddress == 0 || definition.GetGenericParameters().Count != 1)
            throw VectorMagnitudeGuardUnsupported();
        foreach (var parameterHandle in definition.GetGenericParameters())
        {
            var parameter = reader.GetGenericParameter(parameterHandle);
            if (parameter.Parent != spec.Method || parameter.Index != 0 || parameter.Attributes != GenericParameterAttributes.None
                || parameter.GetConstraints().Count != 0) throw VectorMagnitudeGuardUnsupported();
        }
        var raw = reader.GetBlobReader(spec.Signature);
        if (raw.RemainingBytes != 4 || raw.ReadByte() != 0x0a || raw.ReadByte() != 1
            || raw.ReadByte() != variable || raw.ReadByte() != 0) throw VectorMagnitudeGuardUnsupported();
        if (!CoreIntrinsics.ReadVectorMagnitudeSignature(reader.GetBlobReader(definition.Signature), out int vectorToken)
            || vectorToken != SRME.GetRowNumber(vector.Handle) << 2)
            throw VectorMagnitudeGuardUnsupported();
        var result = ResolveMethodSpec(module, (MethodSpecificationHandle)handle, bridge.Context);
        ValidateMagnitudeResolvedDefinition(result, module, (MethodDefinitionHandle)spec.Method);
        if (result.Module != module || result.PInvoke is not null || result.IsSynthetic
            || !CoreIntrinsics.IsVectorEstimateOwner(result.DeclaringClass)
            || result.Context.TypeArgs.Length != 0 || result.Context.MethodArgs is not [var element]
            || element.Kind != TypeKind.Class || element.IsCanonPlaceholder || element.Class != vector.Context.TypeArgs[0].Class
            || !NumberBinarySignature(result, vector, generic: true))
            throw VectorMagnitudeGuardUnsupported();
        ValidateMagnitudeNoInitializer(result.DeclaringClass);
        return result;
    }

}

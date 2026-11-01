using System.Reflection.Metadata;
using SRME = System.Reflection.Metadata.Ecma335.MetadataTokens;

namespace Dn2Cpp;

internal sealed partial class Compilation
{
    private MethodInfo ResolveMagnitudeAbsGuard(MethodInfo method)
    {
        var il = GenericVectorGuardInstructions(method);
        int start = ValidateMagnitudeTypeTests(method, il, 0, new[] { "Byte", "UInt16", "UInt32", "UInt64", "UIntPtr" });
        return method.DeclaringClass.FullName is "System.Numerics.Vector" or "System.Runtime.Intrinsics.Vector64"
            ? ResolveMagnitudeLoopGuard(method, il, start)
            : ResolveMagnitudeHalfGuard(method, il, start, unary: true);
    }

    private MethodInfo ResolveMagnitudeHalfGuard(MethodInfo method, List<Instruction> il, int start, bool unary)
    {
        string lowerName = method.DeclaringClass.FullName switch
        {
            "System.Runtime.Intrinsics.Vector128" => "Vector64",
            "System.Runtime.Intrinsics.Vector256" => "Vector128",
            "System.Runtime.Intrinsics.Vector512" => "Vector256",
            _ => throw VectorMagnitudeGuardUnsupported(),
        };
        int callIndex = start + (unary ? 2 : 4);
        if (il.Count <= callIndex || il[start].OpCode != ILOpCode.Ldarg_0
            || il[start + 1].OpCode != ILOpCode.Ldfld || il[callIndex].OpCode != ILOpCode.Call
            || !unary && (il[start + 2].OpCode != ILOpCode.Ldarg_1
                || il[start + 3].OpCode != ILOpCode.Ldfld || il[start + 1].Token != il[start + 3].Token))
            throw VectorMagnitudeGuardUnsupported();
        var module = method.Module;
        var reader = module.Reader;
        var handle = SRME.EntityHandle(il[callIndex].Token);
        if (handle.Kind != HandleKind.MethodSpecification) throw VectorMagnitudeGuardUnsupported();
        var spec = reader.GetMethodSpecification((MethodSpecificationHandle)handle);
        var raw = reader.GetBlobReader(spec.Signature);
        if (spec.Method.Kind != HandleKind.MethodDefinition || raw.RemainingBytes != 4
            || raw.ReadByte() != 0x0a || raw.ReadByte() != 1 || raw.ReadByte() != 0x1e || raw.ReadByte() != 0)
            throw VectorMagnitudeGuardUnsupported();
        var definition = reader.GetMethodDefinition((MethodDefinitionHandle)spec.Method);
        var owner = reader.GetTypeDefinition(definition.GetDeclaringType());
        // Authenticate strict descent before resolution can enter an instantiated-method cache.
        if (!owner.GetDeclaringType().IsNil || owner.GetGenericParameters().Count != 0
            || !reader.StringComparer.Equals(owner.Namespace, "System.Runtime.Intrinsics")
            || !reader.StringComparer.Equals(owner.Name, lowerName)
            || !reader.StringComparer.Equals(definition.Name, unary ? "Abs" : method.Name))
            throw VectorMagnitudeGuardUnsupported();
        MethodInfo lower;
        if (unary)
        {
            var signature = definition.DecodeSignature(SigProvider, method.Context);
            if (signature.ReturnType.Class is not { } half) throw VectorMagnitudeGuardUnsupported();
            EnsureShape(half);
            if (!CoreIntrinsics.IsFixedVectorManagedValueClass(half)) throw VectorMagnitudeGuardUnsupported();
            lower = ResolveMagnitudeAbsCall(method, il[callIndex].Token, half, 0x1e);
        }
        else
        {
            lower = TryResolveVectorMagnitudeSpec(module, (MethodSpecificationHandle)handle, method.Context)
                ?? throw VectorMagnitudeGuardUnsupported();
            if (!CoreIntrinsics.IsVectorMagnitudeManagedGuard(lower)) throw VectorMagnitudeGuardUnsupported();
        }
        if (lower.Module != module || lower.Context.MethodArgs is not [var element]
            || element.Class != method.Context.MethodArgs[0].Class) throw VectorMagnitudeGuardUnsupported();
        ValidateMagnitudeNoInitializer(lower.DeclaringClass);
        ValidateMagnitudeNoInitializer(lower.Signature.ReturnType.Class!);
        ValidateFixedGenericVectorEstimateField(method, il[start + 1].Token, lower.Signature.ReturnType.Class!);
        return unary ? ResolveMagnitudeAbsGuard(lower) : ResolveVectorMagnitudeManagedGuard(lower);
    }
}

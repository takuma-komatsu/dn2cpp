using System.Reflection;
using System.Reflection.Metadata;
using SRME = System.Reflection.Metadata.Ecma335.MetadataTokens;

namespace Dn2Cpp;

internal sealed partial class Compilation
{
    internal MethodInfo ResolveGenericVectorEstimateGuard(MethodInfo method)
    {
        if (!CoreIntrinsics.IsGenericVectorEstimate(method)) throw GenericVectorGuardUnsupported();
        var body = method.Module.PE.GetMethodBody(method.Rva);
        var instructions = GenericVectorGuardInstructions(method);
        if (body.LocalSignature.IsNil || instructions.Count < 7
            || instructions[0].OpCode != ILOpCode.Ldloca_s || instructions[0].Operand != 0
            || instructions[1].OpCode != ILOpCode.Call
            || instructions[2].OpCode != ILOpCode.Ldc_i4_0
            || instructions[3].OpCode != ILOpCode.Stloc_1
            || instructions[4].OpCode != ILOpCode.Br_s)
            throw GenericVectorGuardUnsupported();
        var locals = method.Module.Reader.GetStandaloneSignature(body.LocalSignature)
            .DecodeLocalSignature(SigProvider, method.Context);
        var vector = method.Signature.ReturnType.Class!;
        if (locals.Length < 2 || locals[0].Class != vector || locals[0].Kind != TypeKind.Class
            || locals[1].Kind != TypeKind.Primitive || locals[1].Primitive != PrimitiveTypeCode.Int32)
            throw GenericVectorGuardUnsupported();
        ValidateGenericVectorSkipInit(method, instructions[1].Token, vector);
        int target = -1;
        for (int i = 5; i < instructions.Count; i++)
            if (instructions[i].Offset == instructions[4].Operand) target = i;
        if (target < 0 || target + 1 >= instructions.Count
            || instructions[target].OpCode != ILOpCode.Ldloc_1
            || instructions[target + 1].OpCode != ILOpCode.Call)
            throw GenericVectorGuardUnsupported();
        var handle = SRME.EntityHandle(instructions[target + 1].Token);
        if (handle.Kind != HandleKind.MemberReference) throw GenericVectorGuardUnsupported();
        var count = ResolveVectorManagedCountReference(method.Module, (MemberReferenceHandle)handle, method.Context);
        if (count.DeclaringClass != vector) throw GenericVectorGuardUnsupported();
        ValidateGenericVectorCountGuard(count);
        return count;
    }

    private List<Instruction> GenericVectorGuardInstructions(MethodInfo method)
    {
        if (method.Rva == 0 || method.PInvoke is not null) throw GenericVectorGuardUnsupported();
        var body = method.Module.PE.GetMethodBody(method.Rva);
        if (body.ExceptionRegions.Length != 0) throw GenericVectorGuardUnsupported();
        return ILDecoder.Decode(body.GetILBytes()!.ToImmutableArrayCompat());
    }

    private void ValidateGenericVectorSkipInit(MethodInfo estimate, int token, ClassInfo vector)
    {
        var module = estimate.Module;
        var handle = SRME.EntityHandle(token);
        if (handle.Kind != HandleKind.MethodSpecification) throw GenericVectorGuardUnsupported();
        var specification = module.Reader.GetMethodSpecification((MethodSpecificationHandle)handle);
        if (specification.Method.Kind != HandleKind.MethodDefinition) throw GenericVectorGuardUnsupported();
        var blob = module.Reader.GetBlobReader(specification.Signature);
        if (blob.RemainingBytes < 3 || blob.ReadByte() != 0x0a || blob.ReadByte() != 1
            || !ReadVectorValueType(module, ref blob, estimate.Context, vector) || blob.RemainingBytes != 0)
            throw GenericVectorGuardUnsupported();
        var definitionHandle = (MethodDefinitionHandle)specification.Method;
        var definition = module.Reader.GetMethodDefinition(definitionHandle);
        var owner = module.Reader.GetTypeDefinition(definition.GetDeclaringType());
        if (!owner.GetDeclaringType().IsNil || owner.GetGenericParameters().Count != 0
            || !module.Reader.StringComparer.Equals(owner.Namespace, "System.Runtime.CompilerServices")
            || !module.Reader.StringComparer.Equals(owner.Name, "Unsafe")
            || !module.Reader.StringComparer.Equals(definition.Name, "SkipInit")
            || definition.Attributes != (MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.HideBySig)
            || definition.ImplAttributes != MethodImplAttributes.AggressiveInlining
            || definition.GetGenericParameters().Count != 1)
            throw GenericVectorGuardUnsupported();
        var raw = module.Reader.GetBlobReader(definition.Signature);
        if (raw.RemainingBytes != 7 || raw.ReadByte() != 0x10 || raw.ReadByte() != 1
            || raw.ReadByte() != 1 || raw.ReadByte() != 1 || raw.ReadByte() != 0x10
            || raw.ReadByte() != 0x1e || raw.ReadByte() != 0)
            throw GenericVectorGuardUnsupported();
        foreach (var parameterHandle in definition.GetGenericParameters())
        {
            var parameter = module.Reader.GetGenericParameter(parameterHandle);
            if (parameter.Parent != definitionHandle || parameter.Index != 0
                || parameter.Attributes != GenericParameterAttributes.AllowByRefLike
                || parameter.GetConstraints().Count != 0)
                throw GenericVectorGuardUnsupported();
        }
        // Unsafe's exact intrinsic is a no-op; its managed fallback must remain a throw stub.
        var skip = ResolveMethodSpec(module, (MethodSpecificationHandle)handle, estimate.Context);
        var instructions = GenericVectorGuardInstructions(skip);
        if (instructions.Count != 2 || instructions[0].OpCode != ILOpCode.Newobj
            || instructions[1].OpCode != ILOpCode.Throw)
            throw GenericVectorGuardUnsupported();
        var constructor = ResolveMethodHandle(module, SRME.EntityHandle(instructions[0].Token), GenericContext.Empty);
        if (constructor is null || constructor.Module != module
            || constructor.DeclaringClass.FullName != "System.PlatformNotSupportedException"
            || constructor.Name != ".ctor" || constructor.IsStatic
            || constructor.Signature.ParameterTypes.Length != 0)
            throw GenericVectorGuardUnsupported();
    }

    private void ValidateGenericVectorCountGuard(MethodInfo count)
    {
        var instructions = GenericVectorGuardInstructions(count);
        if (instructions.Count == 0 || instructions[0].OpCode != ILOpCode.Call)
            throw GenericVectorGuardUnsupported();
        var token = SRME.EntityHandle(instructions[0].Token);
        if (token.Kind != HandleKind.MethodSpecification) throw GenericVectorGuardUnsupported();
        var guard = TryResolveVectorManagedThrowSpec(count.Module, (MethodSpecificationHandle)token, count.Context);
        if (guard is null || !CoreIntrinsics.IsVectorManagedThrowHelper(guard)
            || guard.Name != "ThrowForUnsupportedNumericsVectorBaseType"
            || guard.Context.MethodArgs[0].Class != count.DeclaringClass.Context.TypeArgs[0].Class)
            throw GenericVectorGuardUnsupported();
        var il = GenericVectorGuardInstructions(guard);
        if (il.Count != 5 || il[0].OpCode != ILOpCode.Call
            || il[1].OpCode != ILOpCode.Brtrue_s || il[1].Operand != il[4].Offset
            || il[2].OpCode != ILOpCode.Ldc_i4_s
            || il[3].OpCode != ILOpCode.Call || il[4].OpCode != ILOpCode.Ret)
            throw GenericVectorGuardUnsupported();
        ValidateGenericVectorUnsupportedGetter(guard, il[0].Token, count.DeclaringClass);
        var sinkToken = SRME.EntityHandle(il[3].Token);
        if (sinkToken.Kind != HandleKind.MethodDefinition) throw GenericVectorGuardUnsupported();
        var sink = ResolveMethodHandle(guard.Module, sinkToken, GenericContext.Empty);
        if (sink is null || sink.DeclaringClass != guard.DeclaringClass
            || sink.Name != "ThrowNotSupportedException" || sink.Context.MethodArgs.Length != 0
            || sink.Attributes != (MethodAttributes.Assembly | MethodAttributes.Static | MethodAttributes.HideBySig)
            || sink.ImplAttributes != MethodImplAttributes.IL
            || sink.Signature.ParameterTypes is not [{ Class: { } resource }]
            || resource.Module != guard.Module || resource.FullName != "System.ExceptionResource"
            || !resource.IsEnum || !sink.Signature.ReturnType.IsVoid
            || sink.Signature.Header.RawValue != 0 || sink.Signature.GenericParameterCount != 0
            || sink.Signature.RequiredParameterCount != 1)
            throw GenericVectorGuardUnsupported();
        var sinkBody = GenericVectorGuardInstructions(sink);
        if (sinkBody.Count != 4 || sinkBody[0].OpCode != ILOpCode.Ldarg_0
            || sinkBody[1].OpCode != ILOpCode.Call || sinkBody[2].OpCode != ILOpCode.Newobj
            || sinkBody[3].OpCode != ILOpCode.Throw)
            throw GenericVectorGuardUnsupported();
        var resourceGetter = ResolveMethodHandle(sink.Module, SRME.EntityHandle(sinkBody[1].Token), GenericContext.Empty);
        if (resourceGetter is null || resourceGetter.DeclaringClass != sink.DeclaringClass
            || resourceGetter.Name != "GetResourceString" || !resourceGetter.IsStatic
            || resourceGetter.Signature.Header.RawValue != 0 || resourceGetter.Signature.GenericParameterCount != 0
            || resourceGetter.Signature.ParameterTypes is not [var resourceArgument] || resourceArgument.Class != resource
            || resourceGetter.Signature.ReturnType is not { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.String })
            throw GenericVectorGuardUnsupported();
        var constructor = ResolveMethodHandle(sink.Module, SRME.EntityHandle(sinkBody[2].Token), GenericContext.Empty);
        if (constructor is null || constructor.Module != sink.Module
            || constructor.DeclaringClass.FullName != "System.NotSupportedException"
            || constructor.Name != ".ctor" || constructor.IsStatic
            || constructor.Signature.ParameterTypes is not [{ Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.String }])
            throw GenericVectorGuardUnsupported();
    }

    private void ValidateGenericVectorUnsupportedGetter(MethodInfo guard, int token, ClassInfo vector)
    {
        var module = guard.Module;
        var handle = SRME.EntityHandle(token);
        if (handle.Kind != HandleKind.MemberReference) throw GenericVectorGuardUnsupported();
        var reference = module.Reader.GetMemberReference((MemberReferenceHandle)handle);
        if (reference.Parent.Kind != HandleKind.TypeSpecification
            || !module.Reader.StringComparer.Equals(reference.Name, "get_IsSupported"))
            throw GenericVectorGuardUnsupported();
        var owner = module.Reader.GetBlobReader(module.Reader.GetTypeSpecification((TypeSpecificationHandle)reference.Parent).Signature);
        if (!ReadVectorValueType(module, ref owner, guard.Context, vector) || owner.RemainingBytes != 0)
            throw GenericVectorGuardUnsupported();
        var raw = module.Reader.GetBlobReader(reference.Signature);
        if (!GenericVectorBooleanGetterSignature(raw)) throw GenericVectorGuardUnsupported();
        var getter = ResolveMemberRefMethod(module, (MemberReferenceHandle)handle, guard.Context);
        if (getter.DeclaringClass != vector || getter.Module != module || getter.IsSynthetic
            || getter.Handle.IsNil || getter.Context.MethodArgs.Length != 0
            || getter.Attributes != (MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.HideBySig | MethodAttributes.SpecialName)
            || getter.ImplAttributes != MethodImplAttributes.AggressiveInlining
            || getter.Rva == 0 || getter.PInvoke is not null)
            throw GenericVectorGuardUnsupported();
        var definition = module.Reader.GetMethodDefinition(getter.Handle);
        if (definition.GetDeclaringType() != vector.Handle || definition.GetGenericParameters().Count != 0
            || !GenericVectorBooleanGetterSignature(module.Reader.GetBlobReader(definition.Signature)))
            throw GenericVectorGuardUnsupported();
        // The emitter lowers this exact unsupported element getter to false.
        if (!CoreIntrinsics.IsVectorManagedValueElement(vector.Context.TypeArgs[0]))
            throw GenericVectorGuardUnsupported();
    }

    private static bool GenericVectorBooleanGetterSignature(BlobReader blob) =>
        blob.RemainingBytes == 3 && blob.ReadByte() == 0 && blob.ReadByte() == 0 && blob.ReadByte() == 2;

    private static NotSupportedException GenericVectorGuardUnsupported() =>
        new("Generic vector estimate requires its original unsupported-element guard before any lane operation");
}

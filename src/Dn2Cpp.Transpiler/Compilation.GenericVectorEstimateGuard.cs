using System.Reflection;
using System.Reflection.Metadata;
using SRME = System.Reflection.Metadata.Ecma335.MetadataTokens;

namespace Dn2Cpp;

internal sealed partial class Compilation
{
    internal MethodInfo ResolveGenericVectorEstimateGuard(MethodInfo method)
    {
        if (!CoreIntrinsics.IsGenericVectorEstimate(method)) throw GenericVectorGuardUnsupported();
        if (method.DeclaringClass.FullName is not ("System.Numerics.Vector" or "System.Runtime.Intrinsics.Vector64"))
            return ResolveFixedGenericVectorEstimateGuard(method);
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
            || !ReadFixedGenericVectorEstimateType(ref blob, vector.Handle, 0x1e) || blob.RemainingBytes != 0)
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
        ValidateMagnitudeResolvedDefinition(skip, module, definitionHandle);
        if (skip.DeclaringClass.FullName != "System.Runtime.CompilerServices.Unsafe"
            || skip.DeclaringClass.GenericArity != 0 || skip.DeclaringClass.Context.TypeArgs.Length != 0
            || skip.Context.TypeArgs.Length != 0 || skip.Context.MethodArgs is not [var argument]
            || argument.Kind != TypeKind.Class || argument.IsCanonPlaceholder || argument.Class != vector
            || skip.Signature.Header.RawValue != 0x10 || skip.Signature.GenericParameterCount != 1
            || skip.Signature.RequiredParameterCount != 1
            || skip.Signature.ReturnType is not { Kind: TypeKind.Primitive, IsCanonPlaceholder: false, Primitive: PrimitiveTypeCode.Void }
            || skip.Signature.ParameterTypes is not [{ Kind: TypeKind.ByRef, IsCanonPlaceholder: false,
                Element: { Kind: TypeKind.Class, IsCanonPlaceholder: false } value }] || value.Class != vector)
            throw GenericVectorGuardUnsupported();
        ValidateMagnitudeNoInitializer(skip.DeclaringClass);
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
        var specification = count.Module.Reader.GetMethodSpecification((MethodSpecificationHandle)token);
        var raw = count.Module.Reader.GetBlobReader(specification.Signature);
        if (specification.Method.Kind != HandleKind.MethodDefinition || raw.RemainingBytes != 4
            || raw.ReadByte() != 0x0a || raw.ReadByte() != 1 || raw.ReadByte() != 0x13 || raw.ReadByte() != 0)
            throw GenericVectorGuardUnsupported();
        var guard = TryResolveVectorManagedThrowSpec(count.Module, (MethodSpecificationHandle)token, count.Context);
        if (guard is null || !CoreIntrinsics.IsVectorManagedThrowHelper(guard)
            || guard.Name != (CoreIntrinsics.IsVectorExplicitOwner(count.DeclaringClass)
                ? "ThrowForUnsupportedNumericsVectorBaseType" : "ThrowForUnsupportedIntrinsicsVector64BaseType")
            || guard.Context.MethodArgs[0].Class != count.DeclaringClass.Context.TypeArgs[0].Class)
            throw GenericVectorGuardUnsupported();
        ValidateGenericVectorThrowGuard(guard, count.DeclaringClass);
    }

    private void ValidateGenericVectorThrowGuard(MethodInfo guard, ClassInfo vector) =>
        ValidateGenericVectorThrowGuardCore(guard, vector, memory: false);

    private void ValidateGenericVectorThrowGuardCore(MethodInfo guard, ClassInfo vector, bool memory)
    {
        var il = GenericVectorGuardInstructions(guard);
        if (il.Count != 5 || il[0].OpCode != ILOpCode.Call
            || il[1].OpCode != ILOpCode.Brtrue_s || il[1].Operand != il[4].Offset
            || il[2].OpCode != ILOpCode.Ldc_i4_s
            || il[3].OpCode != ILOpCode.Call || il[4].OpCode != ILOpCode.Ret)
            throw GenericVectorGuardUnsupported();
        ValidateGenericVectorUnsupportedGetterCore(guard, il[0].Token, vector, memory);
        var sinkToken = SRME.EntityHandle(il[3].Token);
        if (sinkToken.Kind != HandleKind.MethodDefinition) throw GenericVectorGuardUnsupported();
        var sink = ResolveMethodHandle(guard.Module, sinkToken, GenericContext.Empty);
        if (sink is null || sink.DeclaringClass != guard.DeclaringClass
            || sink.Name != "ThrowNotSupportedException" || sink.Context.MethodArgs.Length != 0 || sink.Context.TypeArgs.Length != 0
            || sink.Attributes != (MethodAttributes.Assembly | MethodAttributes.Static | MethodAttributes.HideBySig)
            || sink.ImplAttributes != MethodImplAttributes.IL
            || sink.Signature.ParameterTypes is not [{ Kind: TypeKind.Class, IsCanonPlaceholder: false, Class: { } resource }]
            || resource.Module != guard.Module || resource.FullName != "System.ExceptionResource"
            || !resource.IsEnum
            || sink.Signature.ReturnType is not { Kind: TypeKind.Primitive, IsCanonPlaceholder: false, Primitive: PrimitiveTypeCode.Void }
            || sink.Signature.Header.RawValue != 0 || sink.Signature.GenericParameterCount != 0
            || sink.Signature.RequiredParameterCount != 1)
            throw GenericVectorGuardUnsupported();
        ValidateMagnitudeResolvedDefinition(sink, guard.Module, (MethodDefinitionHandle)sinkToken);
        var sinkSignature = guard.Module.Reader.GetBlobReader(guard.Module.Reader.GetMethodDefinition((MethodDefinitionHandle)sinkToken).Signature);
        if (sinkSignature.RemainingBytes < 5 || sinkSignature.ReadByte() != 0 || sinkSignature.ReadByte() != 1
            || sinkSignature.ReadByte() != 1 || !VectorAsRawClass(ref sinkSignature, resource, 0x11)
            || sinkSignature.RemainingBytes != 0) throw GenericVectorGuardUnsupported();
        if (memory) ValidateUnsupportedAlignedDependencyOwner(sink.DeclaringClass, guard.Module, "ThrowHelper");
        var sinkBody = GenericVectorGuardInstructions(sink);
        if (sinkBody.Count != 4 || sinkBody[0].OpCode != ILOpCode.Ldarg_0
            || sinkBody[1].OpCode != ILOpCode.Call || sinkBody[2].OpCode != ILOpCode.Newobj
            || sinkBody[3].OpCode != ILOpCode.Throw)
            throw GenericVectorGuardUnsupported();
        var resourceToken = SRME.EntityHandle(sinkBody[1].Token);
        var constructorToken = SRME.EntityHandle(sinkBody[2].Token);
        if (resourceToken.Kind != HandleKind.MethodDefinition || constructorToken.Kind != HandleKind.MethodDefinition)
            throw GenericVectorGuardUnsupported();
        var resourceGetter = ResolveMethodHandle(sink.Module, resourceToken, GenericContext.Empty);
        if (resourceGetter is null || resourceGetter.DeclaringClass != sink.DeclaringClass
            || resourceGetter.Name != "GetResourceString" || !resourceGetter.IsStatic
            || resourceGetter.Signature.Header.RawValue != 0 || resourceGetter.Signature.GenericParameterCount != 0
            || resourceGetter.Signature.ParameterTypes is not [{ Kind: TypeKind.Class, IsCanonPlaceholder: false } resourceArgument]
            || resourceArgument.Class != resource
            || resourceGetter.Signature.ReturnType is not { Kind: TypeKind.Primitive, IsCanonPlaceholder: false, Primitive: PrimitiveTypeCode.String })
            throw GenericVectorGuardUnsupported();
        ValidateMagnitudeResolvedDefinition(resourceGetter, sink.Module, (MethodDefinitionHandle)resourceToken);
        var resourceSignature = sink.Module.Reader.GetBlobReader(sink.Module.Reader.GetMethodDefinition((MethodDefinitionHandle)resourceToken).Signature);
        if (resourceSignature.RemainingBytes < 5 || resourceSignature.ReadByte() != 0 || resourceSignature.ReadByte() != 1
            || resourceSignature.ReadByte() != 0x0e || !VectorAsRawClass(ref resourceSignature, resource, 0x11)
            || resourceSignature.RemainingBytes != 0 || resourceGetter.Context.TypeArgs.Length != 0
            || resourceGetter.Context.MethodArgs.Length != 0 || resourceGetter.Signature.RequiredParameterCount != 1)
            throw GenericVectorGuardUnsupported();
        var constructor = ResolveMethodHandle(sink.Module, constructorToken, GenericContext.Empty);
        if (constructor is null || constructor.Module != sink.Module
            || constructor.DeclaringClass.FullName != "System.NotSupportedException"
            || constructor.Name != ".ctor" || constructor.IsStatic
            || constructor.Signature.ParameterTypes is not [{ Kind: TypeKind.Primitive, IsCanonPlaceholder: false, Primitive: PrimitiveTypeCode.String }])
            throw GenericVectorGuardUnsupported();
        ValidateMagnitudeResolvedDefinition(constructor, sink.Module, (MethodDefinitionHandle)constructorToken);
        var constructorSignature = sink.Module.Reader.GetBlobReader(sink.Module.Reader.GetMethodDefinition((MethodDefinitionHandle)constructorToken).Signature);
        if (constructorSignature.RemainingBytes != 4 || constructorSignature.ReadByte() != 0x20
            || constructorSignature.ReadByte() != 1 || constructorSignature.ReadByte() != 1 || constructorSignature.ReadByte() != 0x0e
            || constructor.Context.TypeArgs.Length != 0 || constructor.Context.MethodArgs.Length != 0
            || constructor.Signature.Header.RawValue != 0x20 || constructor.Signature.GenericParameterCount != 0
            || constructor.Signature.RequiredParameterCount != 1
            || constructor.Signature.ReturnType is not { Kind: TypeKind.Primitive, IsCanonPlaceholder: false, Primitive: PrimitiveTypeCode.Void })
            throw GenericVectorGuardUnsupported();
        if (memory) ValidateUnsupportedAlignedDependencyOwner(constructor.DeclaringClass, sink.Module, "NotSupportedException");
    }

    private void ValidateGenericVectorUnsupportedGetter(MethodInfo guard, int token, ClassInfo vector) =>
        ValidateGenericVectorUnsupportedGetterCore(guard, token, vector, memory: false);

    private MethodInfo ValidateGenericVectorUnsupportedGetterCore(MethodInfo guard, int token, ClassInfo vector, bool memory)
    {
        var module = guard.Module;
        var handle = SRME.EntityHandle(token);
        if (handle.Kind != HandleKind.MemberReference) throw GenericVectorGuardUnsupported();
        var reference = module.Reader.GetMemberReference((MemberReferenceHandle)handle);
        if (reference.Parent.Kind != HandleKind.TypeSpecification
            || !module.Reader.StringComparer.Equals(reference.Name, "get_IsSupported"))
            throw GenericVectorGuardUnsupported();
        var owner = module.Reader.GetBlobReader(module.Reader.GetTypeSpecification((TypeSpecificationHandle)reference.Parent).Signature);
        if (!ReadFixedGenericVectorEstimateType(ref owner, vector.Handle, 0x1e) || owner.RemainingBytes != 0)
            throw GenericVectorGuardUnsupported();
        var raw = module.Reader.GetBlobReader(reference.Signature);
        if (!GenericVectorBooleanGetterSignature(raw)) throw GenericVectorGuardUnsupported();
        MethodDefinitionHandle expected = default;
        foreach (var candidate in module.Reader.GetTypeDefinition(vector.Handle).GetMethods())
        {
            var definition = module.Reader.GetMethodDefinition(candidate);
            if (!module.Reader.StringComparer.Equals(definition.Name, "get_IsSupported")) continue;
            if (!expected.IsNil || definition.GetDeclaringType() != vector.Handle || definition.GetGenericParameters().Count != 0
                || definition.Attributes != (MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.HideBySig | MethodAttributes.SpecialName)
                || definition.ImplAttributes != MethodImplAttributes.AggressiveInlining || definition.RelativeVirtualAddress == 0
                || !GenericVectorBooleanGetterSignature(module.Reader.GetBlobReader(definition.Signature)))
                throw GenericVectorGuardUnsupported();
            expected = candidate;
        }
        if (expected.IsNil) throw GenericVectorGuardUnsupported();
        // Mapped Char vectors have no ordinary managed-member models. This shell
        // authenticates their real whitelist without admitting it as a callable body.
        var getter = memory && vector.IntrinsicCppName is not null
            ? UnsupportedAlignedModel(vector, expected, vector, vector.Context)
            : ResolveMemberRefMethod(module, (MemberReferenceHandle)handle, guard.Context);
        if (getter.DeclaringClass != vector || getter.Module != module || getter.IsSynthetic
            || getter.Handle.IsNil || getter.Context.MethodArgs.Length != 0
            || getter.Attributes != (MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.HideBySig | MethodAttributes.SpecialName)
            || getter.ImplAttributes != MethodImplAttributes.AggressiveInlining
            || getter.Rva == 0 || getter.PInvoke is not null)
            throw GenericVectorGuardUnsupported();
        ValidateMagnitudeResolvedDefinition(getter, module, expected);
        if (getter.Context.TypeArgs is not [var element]
            || (memory ? !CoreIntrinsics.SameUnsupportedAlignedDimElement(element, vector.Context.TypeArgs[0])
                : element.Kind != TypeKind.Class || element.IsCanonPlaceholder || element.Class != vector.Context.TypeArgs[0].Class)
            || getter.Signature.Header.RawValue != 0 || getter.Signature.GenericParameterCount != 0
            || getter.Signature.RequiredParameterCount != 0 || getter.Signature.ParameterTypes.Length != 0
            || getter.Signature.ReturnType is not { Kind: TypeKind.Primitive, IsCanonPlaceholder: false, Primitive: PrimitiveTypeCode.Boolean })
            throw GenericVectorGuardUnsupported();
        // The emitter lowers this exact unsupported element getter to false.
        if (memory ? !CoreIntrinsics.IsUnsupportedAlignedDimVector(vector)
            : !CoreIntrinsics.IsVectorManagedValueElement(vector.Context.TypeArgs[0]))
            throw GenericVectorGuardUnsupported();
        return getter;
    }

    private static bool GenericVectorBooleanGetterSignature(BlobReader blob) =>
        blob.RemainingBytes == 3 && blob.ReadByte() == 0 && blob.ReadByte() == 0 && blob.ReadByte() == 2;

    private static NotSupportedException GenericVectorGuardUnsupported() =>
        new("Generic vector estimate requires its original unsupported-element guard before any lane operation");
}

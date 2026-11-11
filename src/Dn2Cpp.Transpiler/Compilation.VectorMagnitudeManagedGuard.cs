using System.Reflection;
using System.Reflection.Metadata;
using SRME = System.Reflection.Metadata.Ecma335.MetadataTokens;

namespace Dn2Cpp;

internal sealed partial class Compilation
{
    internal MethodInfo ResolveVectorMagnitudeManagedGuard(MethodInfo method)
    {
        if (!CoreIntrinsics.IsVectorMagnitudeManagedGuard(method)) throw VectorMagnitudeGuardUnsupported();
        return ResolveMagnitudePublicGuard(method);
    }

    private MethodInfo ResolveMagnitudePublicGuard(MethodInfo method)
    {
        ValidateMagnitudeNoInitializer(method.DeclaringClass);
        // Either prologue form refuses an initializer on the guarded vector itself.
        ValidateMagnitudeNoInitializer(method.Signature.ReturnType.Class ?? throw VectorMagnitudeGuardUnsupported());
        var il = GenericVectorGuardInstructions(method);
        // Vector64 and Vector<T> fall back to the element loop, wider fixed vectors to
        // the half descent; both prologue forms branch to that same software tail.
        bool loop = method.DeclaringClass.FullName is "System.Runtime.Intrinsics.Vector64" or "System.Numerics.Vector";
        MethodInfo SoftwareTail(int start) => loop
            ? ResolveMagnitudeLoopGuard(method, il, start)
            : ResolveMagnitudeHalfGuard(method, il, start, unary: false);
        // A CoreLib build without a SIMD ISA at this width compiles the hardware branch to constant false.
        if (il.Count > 0 && il[0].OpCode == ILOpCode.Ldc_i4_0)
        {
            if (il.Count < 3 || il[1].OpCode != ILOpCode.Brfalse_s || il[1].Operand != il[2].Offset)
                throw VectorMagnitudeGuardUnsupported();
            return SoftwareTail(2);
        }
        if (il.Count < 7 || il[0].OpCode != ILOpCode.Call || il[1].OpCode != ILOpCode.Brfalse_s
            || il[1].Operand != il[6].Offset || il[2].OpCode != ILOpCode.Ldarg_0
            || il[3].OpCode != ILOpCode.Ldarg_1 || il[4].OpCode != ILOpCode.Call
            || il[5].OpCode != ILOpCode.Ret) throw VectorMagnitudeGuardUnsupported();
        ValidateMagnitudeHardwareGetter(method, il[0].Token);
        var count = SoftwareTail(6);
        var hardware = ResolveMagnitudeMathCall(method, il[4].Token, method.Name);
        if (CoreIntrinsics.IsVectorNumberName(method.Name) || method.Name is "Max" or "Min")
            ValidateNumberMathGuard(hardware, method.Signature.ReturnType.Class!, count);
        else
            ValidateMagnitudeMathGuard(hardware, method.Signature.ReturnType.Class!, count);
        return count;
    }

    private MethodInfo ResolveMagnitudeLoopGuard(MethodInfo method, List<Instruction> il, int start)
    {
        var vector = method.Signature.ReturnType.Class!;
        var body = method.Module.PE.GetMethodBody(method.Rva);
        if (body.LocalSignature.IsNil || il.Count < start + 6
            || il[start].OpCode != ILOpCode.Ldloca_s || il[start].Operand != 0
            || il[start + 1].OpCode != ILOpCode.Call || il[start + 2].OpCode != ILOpCode.Ldc_i4_0
            || il[start + 3].OpCode != ILOpCode.Stloc_1 || il[start + 4].OpCode != ILOpCode.Br_s)
            throw VectorMagnitudeGuardUnsupported();
        var locals = method.Module.Reader.GetStandaloneSignature(body.LocalSignature).DecodeLocalSignature(SigProvider, method.Context);
        if (locals.Length < 2 || locals[0].Kind != TypeKind.Class || locals[0].Class != vector
            || locals[1].Kind != TypeKind.Primitive || locals[1].Primitive != PrimitiveTypeCode.Int32)
            throw VectorMagnitudeGuardUnsupported();
        ValidateGenericVectorSkipInit(method, il[start + 1].Token, vector);
        int condition = MagnitudeInstructionIndex(il, il[start + 4].Operand);
        if (condition < start + 5 || condition + 1 >= il.Count
            || il[condition].OpCode != ILOpCode.Ldloc_1 || il[condition + 1].OpCode != ILOpCode.Call)
            throw VectorMagnitudeGuardUnsupported();
        var token = SRME.EntityHandle(il[condition + 1].Token);
        if (token.Kind != HandleKind.MemberReference) throw VectorMagnitudeGuardUnsupported();
        var count = ResolveVectorManagedCountReference(method.Module, (MemberReferenceHandle)token, method.Context);
        if (count.DeclaringClass != vector) throw VectorMagnitudeGuardUnsupported();
        ValidateGenericVectorCountGuard(count);
        var countBody = GenericVectorGuardInstructions(count);
        var guard = TryResolveVectorManagedThrowSpec(method.Module,
            (MethodSpecificationHandle)SRME.EntityHandle(countBody[0].Token), count.Context)!;
        var guardBody = GenericVectorGuardInstructions(guard);
        if (guardBody[2].Operand != 0x43) throw VectorMagnitudeGuardUnsupported();
        var getter = ResolveMemberRefMethod(method.Module,
            (MemberReferenceHandle)SRME.EntityHandle(guardBody[0].Token), guard.Context);
        ValidateVectorAsSupportedWhitelist(getter);
        ValidateMagnitudeNoInitializer(guard.DeclaringClass);
        ValidateMagnitudeNoInitializer(vector);
        return count;
    }

    private void ValidateMagnitudeHardwareGetter(MethodInfo method, int token)
    {
        var handle = SRME.EntityHandle(token);
        if (handle.Kind != HandleKind.MethodDefinition) throw VectorMagnitudeGuardUnsupported();
        var definition = method.Module.Reader.GetMethodDefinition((MethodDefinitionHandle)handle);
        if (definition.GetDeclaringType() != method.DeclaringClass.Handle
            || !method.Module.Reader.StringComparer.Equals(definition.Name, "get_IsHardwareAccelerated")
            || definition.Attributes != (MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.HideBySig | MethodAttributes.SpecialName)
            || definition.ImplAttributes != MethodImplAttributes.IL || definition.RelativeVirtualAddress == 0
            || definition.GetGenericParameters().Count != 0
            || !GenericVectorBooleanGetterSignature(method.Module.Reader.GetBlobReader(definition.Signature)))
            throw VectorMagnitudeGuardUnsupported();
        var getter = ResolveMethodHandle(method.Module, handle, GenericContext.Empty);
        if (getter is null) throw VectorMagnitudeGuardUnsupported();
        ValidateMagnitudeResolvedDefinition(getter, method.Module, (MethodDefinitionHandle)handle);
        if (getter.DeclaringClass != method.DeclaringClass || getter.Context.MethodArgs.Length != 0 || getter.Context.TypeArgs.Length != 0
            || getter.Signature.Header.RawValue != 0 || getter.Signature.GenericParameterCount != 0
            || getter.Signature.RequiredParameterCount != 0 || getter.Signature.ParameterTypes.Length != 0
            || getter.Signature.ReturnType is not { Kind: TypeKind.Primitive, IsCanonPlaceholder: false, Primitive: PrimitiveTypeCode.Boolean })
            throw VectorMagnitudeGuardUnsupported();
        var il = GenericVectorGuardInstructions(getter);
        // The exact recursive CoreLib intrinsic is a pure hardware query on either backend.
        if (il.Count != 2 || il[0].OpCode != ILOpCode.Call || il[0].Token != token || il[1].OpCode != ILOpCode.Ret)
            throw VectorMagnitudeGuardUnsupported();
    }

    private MethodInfo ResolveMagnitudeMathCall(MethodInfo caller, int token, string name)
    {
        var module = caller.Module;
        var reader = module.Reader;
        var handle = SRME.EntityHandle(token);
        if (handle.Kind != HandleKind.MethodSpecification) throw VectorMagnitudeGuardUnsupported();
        var spec = reader.GetMethodSpecification((MethodSpecificationHandle)handle);
        if (spec.Method.Kind != HandleKind.MethodDefinition) throw VectorMagnitudeGuardUnsupported();
        var definition = reader.GetMethodDefinition((MethodDefinitionHandle)spec.Method);
        var owner = reader.GetTypeDefinition(definition.GetDeclaringType());
        if (!owner.GetDeclaringType().IsNil || owner.GetGenericParameters().Count != 0
            || !reader.StringComparer.Equals(owner.Namespace, "System.Runtime.Intrinsics")
            || !reader.StringComparer.Equals(owner.Name, "VectorMath")
            || !reader.StringComparer.Equals(definition.Name, name)
            || definition.Attributes != (MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.HideBySig)
            || definition.ImplAttributes != MethodImplAttributes.AggressiveInlining
            || definition.GetGenericParameters().Count != 2) throw VectorMagnitudeGuardUnsupported();
        ValidateMagnitudeMathParameters(module, (MethodDefinitionHandle)spec.Method);
        var signature = reader.GetBlobReader(definition.Signature);
        if (signature.RemainingBytes != 9 || signature.ReadByte() != 0x10 || signature.ReadByte() != 2
            || signature.ReadByte() != 2 || signature.ReadByte() != 0x1e || signature.ReadByte() != 0
            || signature.ReadByte() != 0x1e || signature.ReadByte() != 0
            || signature.ReadByte() != 0x1e || signature.ReadByte() != 0) throw VectorMagnitudeGuardUnsupported();
        var vector = caller.Signature.ReturnType.Class!;
        var element = caller.Context.MethodArgs[^1];
        var raw = reader.GetBlobReader(spec.Signature);
        if (raw.RemainingBytes < 4 || raw.ReadByte() != 0x0a || raw.ReadByte() != 2
            || !ReadVectorValueType(module, ref raw, caller.Context, vector)
            || !ReadVectorValueElement(module, ref raw, caller.Context, element) || raw.RemainingBytes != 0)
            throw VectorMagnitudeGuardUnsupported();
        var result = ResolveMethodSpec(module, (MethodSpecificationHandle)handle, caller.Context);
        ValidateMagnitudeResolvedDefinition(result, module, (MethodDefinitionHandle)spec.Method);
        if (result.Module != module || result.PInvoke is not null || result.IsSynthetic
            || result.DeclaringClass.FullName != "System.Runtime.Intrinsics.VectorMath"
            || result.DeclaringClass.GenericArity != 0 || result.DeclaringClass.Context.TypeArgs.Length != 0
            || result.Signature.Header.RawValue != 0x10 || result.Signature.GenericParameterCount != 2
            || result.Signature.RequiredParameterCount != 2
            || result.Signature.ReturnType is not { Kind: TypeKind.Class, IsCanonPlaceholder: false } returned || returned.Class != vector
            || result.Signature.ParameterTypes is not [{ Kind: TypeKind.Class, IsCanonPlaceholder: false } left,
                { Kind: TypeKind.Class, IsCanonPlaceholder: false } right]
            || left.Class != vector || right.Class != vector || result.Context.TypeArgs.Length != 0
            || result.Context.MethodArgs is not [{ Kind: TypeKind.Class, IsCanonPlaceholder: false } actualVector,
                { Kind: TypeKind.Class, IsCanonPlaceholder: false } actualElement]
            || actualVector.Class != vector || actualElement.Class != element.Class) throw VectorMagnitudeGuardUnsupported();
        ValidateMagnitudeNoInitializer(result.DeclaringClass);
        return result;
    }

    private void ValidateMagnitudeMathGuard(MethodInfo method, ClassInfo vector, MethodInfo count)
    {
        var il = GenericVectorGuardInstructions(method);
        if (!method.Name.EndsWith("Number", StringComparison.Ordinal))
        {
            int target = ValidateMagnitudeTypeTests(method, il, 1, new[] { "Single", "Double" });
            if (target + 3 >= il.Count || il[target].OpCode != ILOpCode.Ldarg_0
                || il[target + 1].OpCode != ILOpCode.Ldarg_1 || il[target + 2].OpCode != ILOpCode.Call
                || il[target + 3].OpCode != ILOpCode.Ret) throw VectorMagnitudeGuardUnsupported();
            var number = ResolveMagnitudeMathCall(method, il[target + 2].Token, method.Name + "Number");
            ValidateMagnitudeMathGuard(number, vector, count);
            return;
        }
        int abs = ValidateMagnitudeTypeTests(method, il, 1, new[] { "Byte", "UInt16", "UInt32", "UInt64", "UIntPtr" });
        if (abs + 2 >= il.Count || il[abs].OpCode != ILOpCode.Ldarg_0
            || il[abs + 1].OpCode != ILOpCode.Constrained || il[abs + 2].OpCode != ILOpCode.Call)
            throw VectorMagnitudeGuardUnsupported();
        ValidateMagnitudeTypeVariable(method, il[abs + 1].Token, 0);
        var bridge = ResolveMagnitudeAbsBridge(method, il[abs + 2].Token, vector);
        var body = GenericVectorGuardInstructions(bridge);
        if (body.Count != 3 || body[0].OpCode != ILOpCode.Ldarg_0 || body[1].OpCode != ILOpCode.Call
            || body[2].OpCode != ILOpCode.Ret) throw VectorMagnitudeGuardUnsupported();
        var absMethod = ResolveMagnitudeAbsCall(bridge, body[1].Token, vector);
        var actualCount = ResolveMagnitudeAbsGuard(absMethod);
        if (actualCount != count) throw VectorMagnitudeGuardUnsupported();
    }

    private MethodInfo ResolveMagnitudeAbsBridge(MethodInfo method, int token, ClassInfo vector)
    {
        var module = method.Module;
        var reader = module.Reader;
        var handle = SRME.EntityHandle(token);
        if (handle.Kind != HandleKind.MemberReference) throw VectorMagnitudeGuardUnsupported();
        var reference = reader.GetMemberReference((MemberReferenceHandle)handle);
        var raw = reader.GetBlobReader(reference.Signature);
        if (!reader.StringComparer.Equals(reference.Name, "Abs") || reference.Parent.Kind != HandleKind.TypeSpecification
            || raw.RemainingBytes != 6 || raw.ReadByte() != 0 || raw.ReadByte() != 1
            || raw.ReadByte() != 0x13 || raw.ReadByte() != 0 || raw.ReadByte() != 0x13 || raw.ReadByte() != 0)
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
            || target.Name != "Abs" || !target.IsStatic || target.Context.MethodArgs.Length != 0
            || target.Signature.ReturnType.Class != vector || target.Signature.ParameterTypes is not [var input]
            || input.Class != vector) throw VectorMagnitudeGuardUnsupported();
        var targetDefinition = reader.GetMethodDefinition(target.Handle);
        var targetRaw = reader.GetBlobReader(targetDefinition.Signature);
        if (targetDefinition.GetDeclaringType() != contract.Handle
            || targetDefinition.Attributes != (MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.Virtual
                | MethodAttributes.HideBySig | MethodAttributes.Abstract)
            || targetDefinition.ImplAttributes != MethodImplAttributes.IL || targetDefinition.RelativeVirtualAddress != 0
            || targetDefinition.GetGenericParameters().Count != 0 || target.PInvoke is not null || target.IsSynthetic
            || targetRaw.RemainingBytes != 6 || targetRaw.ReadByte() != 0 || targetRaw.ReadByte() != 1
            || targetRaw.ReadByte() != 0x13 || targetRaw.ReadByte() != 0 || targetRaw.ReadByte() != 0x13 || targetRaw.ReadByte() != 0)
            throw VectorMagnitudeGuardUnsupported();
        MethodInfo? bridge = null;
        ValidateMagnitudeNoInitializer(vector);
        foreach (var implementationHandle in reader.GetTypeDefinition(vector.Handle).GetMethodImplementations())
        {
            var implementation = reader.GetMethodImplementation(implementationHandle);
            if (implementation.MethodDeclaration.Kind != HandleKind.MemberReference) continue;
            var declaration = reader.GetMemberReference((MemberReferenceHandle)implementation.MethodDeclaration);
            if (!reader.StringComparer.Equals(declaration.Name, "Abs")) continue;
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
            if (signature.RemainingBytes < 2 || signature.ReadByte() != 0 || signature.ReadByte() != 1
                || !ReadVectorValueType(module, ref signature, vector.Context, vector)
                || !ReadVectorValueType(module, ref signature, vector.Context, vector) || signature.RemainingBytes != 0)
                throw VectorMagnitudeGuardUnsupported();
            bridge = InstantiateMethodOnClass(vector, module, (MethodDefinitionHandle)implementation.MethodBody, Array.Empty<TypeDesc>());
            ValidateMagnitudeResolvedDefinition(bridge, module, (MethodDefinitionHandle)implementation.MethodBody);
            if (bridge is null || bridge.DeclaringClass != vector || bridge.Module != module
                || bridge.Signature.ReturnType.Class != vector || bridge.Signature.ParameterTypes is not [var parameter]
                || parameter.Class != vector) throw VectorMagnitudeGuardUnsupported();
        }
        return bridge ?? throw VectorMagnitudeGuardUnsupported();
    }

    private MethodInfo ResolveMagnitudeAbsCall(MethodInfo bridge, int token, ClassInfo vector, int variable = 0x13)
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
            || owner.GetGenericParameters().Count != 0 || !reader.StringComparer.Equals(definition.Name, "Abs")
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
            || raw.ReadByte() != variable || raw.ReadByte() != 0) throw VectorMagnitudeGuardUnsupported();
        var result = ResolveMethodSpec(module, (MethodSpecificationHandle)handle, bridge.Context);
        ValidateMagnitudeResolvedDefinition(result, module, (MethodDefinitionHandle)spec.Method);
        if (result.Module != module || result.PInvoke is not null || result.IsSynthetic
            || result.Context.TypeArgs.Length != 0 || result.Context.MethodArgs is not [var element]
            || element.Class != vector.Context.TypeArgs[0].Class || result.Signature.ReturnType.Class != vector
            || result.Signature.ParameterTypes is not [var input] || input.Class != vector)
            throw VectorMagnitudeGuardUnsupported();
        var signature = reader.GetBlobReader(definition.Signature);
        if (signature.RemainingBytes < 3 || signature.ReadByte() != 0x10 || signature.ReadByte() != 1 || signature.ReadByte() != 1
            || !ReadVectorValueType(module, ref signature, result.Context, vector)
            || !ReadVectorValueType(module, ref signature, result.Context, vector) || signature.RemainingBytes != 0)
            throw VectorMagnitudeGuardUnsupported();
        ValidateMagnitudeNoInitializer(result.DeclaringClass);
        return result;
    }

    private int ValidateMagnitudeTypeTests(MethodInfo method, List<Instruction> il, int variable, string[] names)
    {
        if (il.Count <= names.Length * 6) throw VectorMagnitudeGuardUnsupported();
        for (int i = 0; i < names.Length; i++)
        {
            int start = i * 6;
            if (il[start].OpCode != ILOpCode.Ldtoken || il[start + 1].OpCode != ILOpCode.Call
                || il[start + 2].OpCode != ILOpCode.Ldtoken || il[start + 3].OpCode != ILOpCode.Call
                || il[start + 4].OpCode != ILOpCode.Call
                || il[start + 5].OpCode != (i == names.Length - 1 ? ILOpCode.Brfalse_s : ILOpCode.Brtrue_s))
                throw VectorMagnitudeGuardUnsupported();
            ValidateMagnitudeTypeVariable(method, il[start].Token, variable);
            var primitive = SRME.EntityHandle(il[start + 2].Token);
            if (primitive.Kind != HandleKind.TypeDefinition) throw VectorMagnitudeGuardUnsupported();
            var definition = method.Module.Reader.GetTypeDefinition((TypeDefinitionHandle)primitive);
            if (!definition.GetDeclaringType().IsNil || definition.GetGenericParameters().Count != 0
                || !method.Module.Reader.StringComparer.Equals(definition.Namespace, "System")
                || !method.Module.Reader.StringComparer.Equals(definition.Name, names[i])) throw VectorMagnitudeGuardUnsupported();
            // The admitted managed value identity cannot equal any of these mapped primitive identities.
            if (method.Context.MethodArgs[variable].Class is not { } element
                || element.Module == method.Module && element.Handle == (TypeDefinitionHandle)primitive)
                throw VectorMagnitudeGuardUnsupported();
            ValidateVectorAsTypeOperation(method.Module, il[start + 1].Token, false);
            if (il[start + 3].Token != il[start + 1].Token) throw VectorMagnitudeGuardUnsupported();
            ValidateVectorAsTypeOperation(method.Module, il[start + 4].Token, true);
            if (i < names.Length - 1 && il[start + 5].Operand != il[names.Length * 6].Offset)
                throw VectorMagnitudeGuardUnsupported();
        }
        int target = MagnitudeInstructionIndex(il, il[names.Length * 6 - 1].Operand);
        if (target <= names.Length * 6) throw VectorMagnitudeGuardUnsupported();
        return target;
    }

    private static void ValidateMagnitudeTypeVariable(MethodInfo method, int token, int index)
    {
        var handle = SRME.EntityHandle(token);
        if (handle.Kind != HandleKind.TypeSpecification) throw VectorMagnitudeGuardUnsupported();
        var raw = method.Module.Reader.GetBlobReader(method.Module.Reader.GetTypeSpecification((TypeSpecificationHandle)handle).Signature);
        if (raw.RemainingBytes != 2 || raw.ReadByte() != 0x1e || raw.ReadByte() != index)
            throw VectorMagnitudeGuardUnsupported();
    }

    private static void ValidateMagnitudeResolvedDefinition(MethodInfo method, Module module, MethodDefinitionHandle handle)
    {
        if (handle.IsNil) throw VectorMagnitudeGuardUnsupported();
        var definition = module.Reader.GetMethodDefinition(handle);
        if (method.Module != module || method.Handle != handle || method.DeclaringClass.Module != module
            || method.DeclaringClass.Handle != definition.GetDeclaringType()
            || !module.Reader.StringComparer.Equals(definition.Name, method.Name)
            || method.Attributes != definition.Attributes || method.ImplAttributes != definition.ImplAttributes
            || method.Rva != definition.RelativeVirtualAddress || method.IsSynthetic || method.PInvoke is not null)
            throw VectorMagnitudeGuardUnsupported();
    }

    private static void ValidateMagnitudeMathParameters(Module module, MethodDefinitionHandle handle)
    {
        var reader = module.Reader;
        int index = 0;
        foreach (var parameterHandle in reader.GetMethodDefinition(handle).GetGenericParameters())
        {
            var parameter = reader.GetGenericParameter(parameterHandle);
            if (parameter.Parent != handle || parameter.Index != index
                || parameter.Attributes != (index == 0
                    ? GenericParameterAttributes.NotNullableValueTypeConstraint | GenericParameterAttributes.DefaultConstructorConstraint
                    : GenericParameterAttributes.None)
                || parameter.GetConstraints().Count != (index == 0 ? 2 : 0)) throw VectorMagnitudeGuardUnsupported();
            int constraintIndex = 0;
            foreach (var constraintHandle in parameter.GetConstraints())
            {
                var constraint = reader.GetGenericParameterConstraint(constraintHandle);
                if (constraint.Parameter != parameterHandle || constraint.Type.Kind != HandleKind.TypeSpecification)
                    throw VectorMagnitudeGuardUnsupported();
                var raw = reader.GetBlobReader(reader.GetTypeSpecification((TypeSpecificationHandle)constraint.Type).Signature);
                if (constraintIndex == 0)
                {
                    if (raw.RemainingBytes < 4 || raw.ReadByte() != 0x1f
                        || !MagnitudeNamedType(module, ref raw, "System.Runtime.InteropServices", "UnmanagedType")
                        || raw.RemainingBytes == 0 || raw.ReadByte() != 0x12
                        || !MagnitudeNamedType(module, ref raw, "System", "ValueType") || raw.RemainingBytes != 0)
                        throw VectorMagnitudeGuardUnsupported();
                }
                else if (raw.RemainingBytes < 7 || raw.ReadByte() != 0x15 || raw.ReadByte() != 0x12
                    || !MagnitudeNamedType(module, ref raw, "System.Runtime.Intrinsics", "ISimdVector`2")
                    || raw.RemainingBytes != 5 || raw.ReadByte() != 2 || raw.ReadByte() != 0x1e || raw.ReadByte() != 0
                    || raw.ReadByte() != 0x1e || raw.ReadByte() != 1) throw VectorMagnitudeGuardUnsupported();
                constraintIndex++;
            }
            index++;
        }
        if (index != 2) throw VectorMagnitudeGuardUnsupported();
    }

    private static bool MagnitudeNamedType(Module module, ref BlobReader raw, string ns, string name)
    {
        if (!raw.TryReadCompressedInteger(out int token) || token <= 0 || (token & 3) != 0) return false;
        var definition = module.Reader.GetTypeDefinition(SRME.TypeDefinitionHandle(token >> 2));
        return definition.GetDeclaringType().IsNil && module.Reader.StringComparer.Equals(definition.Namespace, ns)
            && module.Reader.StringComparer.Equals(definition.Name, name);
    }

    private static int MagnitudeInstructionIndex(List<Instruction> instructions, long offset)
    {
        for (int i = 0; i < instructions.Count; i++) if (instructions[i].Offset == offset) return i;
        return -1;
    }

    private static void ValidateMagnitudeNoInitializer(ClassInfo owner)
    {
        foreach (var handle in owner.Module.Reader.GetTypeDefinition(owner.Handle).GetMethods())
            if (owner.Module.Reader.StringComparer.Equals(owner.Module.Reader.GetMethodDefinition(handle).Name, ".cctor"))
                throw VectorMagnitudeGuardUnsupported();
    }

    private static NotSupportedException VectorMagnitudeGuardUnsupported() =>
        new("Vector magnitude requires its original managed guard on both hardware and software paths before any lane operation");
}

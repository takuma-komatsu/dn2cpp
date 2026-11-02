using System.Reflection;
using System.Reflection.Metadata;
using SRME = System.Reflection.Metadata.Ecma335.MetadataTokens;

namespace Dn2Cpp;

internal sealed partial class Compilation
{
    internal MethodInfo[] ResolveGuardedVectorAsGuards(MethodInfo method)
    {
        if (!CoreIntrinsics.IsGuardedVectorAs(method)) throw GuardedVectorAsUnsupported();
        var module = method.Module;
        var reader = module.Reader;
        var body = module.PE.GetMethodBody(method.Rva);
        var il = GenericVectorGuardInstructions(method);
        if (!body.LocalSignature.IsNil || il.Count != 5 || il[0].OpCode != ILOpCode.Call
            || il[1].OpCode != ILOpCode.Call || il[2].OpCode != ILOpCode.Ldarg_0
            || il[3].OpCode != ILOpCode.Call || il[4].OpCode != ILOpCode.Ret)
            throw GuardedVectorAsUnsupported();
        var first = VectorAsGuardSpecification(method, il[0].Token, 0);
        var second = VectorAsGuardSpecification(method, il[1].Token, 1);
        if (first.Method != second.Method) throw GuardedVectorAsUnsupported();
        ValidateVectorAsBitCast(method, il[3].Token);
        // A supported prefix guard is a pure whitelist test. The first unsupported
        // guard throws, so no later guard or representation conversion can execute.
        int selected = CoreIntrinsics.IsVectorManagedValueElement(method.Context.MethodArgs[0]) ? 0 : 1;
        var guard = TryResolveVectorManagedThrowSpec(module,
            (MethodSpecificationHandle)SRME.EntityHandle(il[selected].Token), method.Context);
        var vector = (selected == 0 ? method.Signature.ParameterTypes[0] : method.Signature.ReturnType).Class!;
        if (guard is null || !CoreIntrinsics.IsVectorManagedThrowHelper(guard)
            || guard.Name != "ThrowForUnsupportedNumericsVectorBaseType"
            || guard.Module != module || guard.Handle != first.Method
            || !SameTypeArg(guard.Context.MethodArgs[0], method.Context.MethodArgs[selected]))
            throw GuardedVectorAsUnsupported();
        ValidateGenericVectorThrowGuard(guard, vector);
        var guardIl = GenericVectorGuardInstructions(guard);
        if (guardIl[2].Operand != 0x43) throw GuardedVectorAsUnsupported();
        var getterReference = reader.GetMemberReference((MemberReferenceHandle)SRME.EntityHandle(guardIl[0].Token));
        var getterParent = reader.GetBlobReader(reader.GetTypeSpecification((TypeSpecificationHandle)getterReference.Parent).Signature);
        if (!VectorAsRawVectorArgument(ref getterParent, vector.Handle, 0) || getterParent.RemainingBytes != 0)
            throw GuardedVectorAsUnsupported();
        var getter = ResolveMemberRefMethod(module, (MemberReferenceHandle)SRME.EntityHandle(guardIl[0].Token), guard.Context);
        ValidateVectorAsSupportedWhitelist(getter);
        VectorAsNoInitializer(method.DeclaringClass);
        VectorAsNoInitializer(guard.DeclaringClass);
        VectorAsNoInitializer(vector);
        return new[] { guard };
    }

    private MethodSpecification VectorAsGuardSpecification(MethodInfo method, int token, int index)
    {
        var handle = SRME.EntityHandle(token);
        if (handle.Kind != HandleKind.MethodSpecification) throw GuardedVectorAsUnsupported();
        var reader = method.Module.Reader;
        var spec = reader.GetMethodSpecification((MethodSpecificationHandle)handle);
        var raw = reader.GetBlobReader(spec.Signature);
        if (spec.Method.Kind != HandleKind.MethodDefinition || raw.RemainingBytes != 4
            || raw.ReadByte() != 0x0a || raw.ReadByte() != 1 || raw.ReadByte() != 0x1e || raw.ReadByte() != index)
            throw GuardedVectorAsUnsupported();
        var definition = reader.GetMethodDefinition((MethodDefinitionHandle)spec.Method);
        var owner = reader.GetTypeDefinition(definition.GetDeclaringType());
        if (!owner.GetDeclaringType().IsNil || owner.GetGenericParameters().Count != 0
            || !reader.StringComparer.Equals(owner.Namespace, "System")
            || !reader.StringComparer.Equals(owner.Name, "ThrowHelper")
            || !reader.StringComparer.Equals(definition.Name, "ThrowForUnsupportedNumericsVectorBaseType"))
            throw GuardedVectorAsUnsupported();
        return spec;
    }

    private void ValidateVectorAsBitCast(MethodInfo method, int token)
    {
        var reader = method.Module.Reader;
        var handle = SRME.EntityHandle(token);
        if (handle.Kind != HandleKind.MethodSpecification) throw GuardedVectorAsUnsupported();
        var spec = reader.GetMethodSpecification((MethodSpecificationHandle)handle);
        var raw = reader.GetBlobReader(spec.Signature);
        var source = method.Signature.ParameterTypes[0].Class!;
        var destination = method.Signature.ReturnType.Class!;
        if (spec.Method.Kind != HandleKind.MethodDefinition || raw.RemainingBytes < 2
            || raw.ReadByte() != 0x0a || raw.ReadByte() != 2
            || !VectorAsRawVectorArgument(ref raw, source.Handle, 0)
            || !VectorAsRawVectorArgument(ref raw, destination.Handle, 1) || raw.RemainingBytes != 0)
            throw GuardedVectorAsUnsupported();
        var definition = reader.GetMethodDefinition((MethodDefinitionHandle)spec.Method);
        var owner = reader.GetTypeDefinition(definition.GetDeclaringType());
        if (!owner.GetDeclaringType().IsNil || owner.GetGenericParameters().Count != 0
            || !reader.StringComparer.Equals(owner.Namespace, "System.Runtime.CompilerServices")
            || !reader.StringComparer.Equals(owner.Name, "Unsafe") || !reader.StringComparer.Equals(definition.Name, "BitCast")
            || definition.Attributes != (MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.HideBySig)
            || definition.ImplAttributes != MethodImplAttributes.AggressiveInlining
            || definition.GetGenericParameters().Count != 2)
            throw GuardedVectorAsUnsupported();
        int index = 0;
        foreach (var parameterHandle in definition.GetGenericParameters())
        {
            var parameter = reader.GetGenericParameter(parameterHandle);
            if (parameter.Parent != spec.Method || parameter.Index != index++
                || parameter.Attributes != GenericParameterAttributes.AllowByRefLike || parameter.GetConstraints().Count != 0)
                throw GuardedVectorAsUnsupported();
        }
        var signature = reader.GetBlobReader(definition.Signature);
        if (signature.RemainingBytes != 7 || signature.ReadByte() != 0x10 || signature.ReadByte() != 2
            || signature.ReadByte() != 1 || signature.ReadByte() != 0x1e || signature.ReadByte() != 1
            || signature.ReadByte() != 0x1e || signature.ReadByte() != 0)
            throw GuardedVectorAsUnsupported();
    }

    private static bool VectorAsRawVectorArgument(ref BlobReader raw, TypeDefinitionHandle vector, int index) =>
        raw.RemainingBytes >= 6 && raw.ReadByte() == 0x15 && raw.ReadByte() == 0x11
        && raw.TryReadCompressedInteger(out int token) && token == SRME.GetRowNumber(vector) << 2
        && raw.RemainingBytes >= 3 && raw.ReadByte() == 1 && raw.ReadByte() == 0x1e && raw.ReadByte() == index;

    private static void VectorAsNoInitializer(ClassInfo owner)
    {
        var reader = owner.Module.Reader;
        foreach (var handle in reader.GetTypeDefinition(owner.Handle).GetMethods())
            if (reader.StringComparer.Equals(reader.GetMethodDefinition(handle).Name, ".cctor"))
                throw GuardedVectorAsUnsupported();
    }

    private void ValidateVectorAsSupportedWhitelist(MethodInfo getter) =>
        ValidateVectorAsSupportedWhitelistCore(getter, memory: false);

    private void ValidateVectorAsSupportedWhitelistCore(MethodInfo getter, bool memory)
    {
        var module = getter.Module;
        var reader = module.Reader;
        var il = GenericVectorGuardInstructions(getter);
        string[] names = CoreIntrinsics.IsVectorExplicitOwner(getter.DeclaringClass)
            ? new[] { "Byte", "Double", "Int16", "Int32", "Int64", "IntPtr", "UIntPtr", "SByte", "Single", "UInt16", "UInt32", "UInt64" }
            : (memory && CoreIntrinsics.IsUnsupportedAlignedDimVector(getter.DeclaringClass)
                || reader.StringComparer.Equals(reader.GetTypeDefinition(getter.DeclaringClass.Handle).Name, "Vector64`1")
                    && CoreIntrinsics.IsFixedVectorManagedValueClass(getter.DeclaringClass))
                ? new[] { "Byte", "Double", "Int16", "Int32", "Int64", "IntPtr", "SByte", "Single", "UInt16", "UInt32", "UInt64", "UIntPtr" }
                : throw GuardedVectorAsUnsupported();
        if (il.Count != 74 || !module.PE.GetMethodBody(getter.Rva).LocalSignature.IsNil
            || il[71].OpCode != ILOpCode.Ret || il[72].OpCode != ILOpCode.Ldc_i4_1 || il[73].OpCode != ILOpCode.Ret)
            throw GuardedVectorAsUnsupported();
        for (int index = 0; index < names.Length; index++)
        {
            int start = index * 6;
            if (il[start].OpCode != ILOpCode.Ldtoken || il[start + 1].OpCode != ILOpCode.Call
                || il[start + 2].OpCode != ILOpCode.Ldtoken || il[start + 3].OpCode != ILOpCode.Call
                || il[start + 4].OpCode != ILOpCode.Call || il[start + 1].Token != il[start + 3].Token
                || index < 11 && (il[start + 5].OpCode is not (ILOpCode.Brtrue or ILOpCode.Brtrue_s)
                    || il[start + 5].Operand != il[72].Offset))
                throw GuardedVectorAsUnsupported();
            var variable = SRME.EntityHandle(il[start].Token);
            if (variable.Kind != HandleKind.TypeSpecification) throw GuardedVectorAsUnsupported();
            var raw = reader.GetBlobReader(reader.GetTypeSpecification((TypeSpecificationHandle)variable).Signature);
            if (raw.RemainingBytes != 2 || raw.ReadByte() != 0x13 || raw.ReadByte() != 0)
                throw GuardedVectorAsUnsupported();
            var type = SRME.EntityHandle(il[start + 2].Token);
            if (type.Kind != HandleKind.TypeDefinition) throw GuardedVectorAsUnsupported();
            var definition = reader.GetTypeDefinition((TypeDefinitionHandle)type);
            if (!definition.GetDeclaringType().IsNil || definition.GetGenericParameters().Count != 0
                || !reader.StringComparer.Equals(definition.Namespace, "System")
                || !reader.StringComparer.Equals(definition.Name, names[index]))
                throw GuardedVectorAsUnsupported();
            ValidateVectorAsTypeOperationCore(module, il[start + 1].Token, false, memory);
            ValidateVectorAsTypeOperationCore(module, il[start + 4].Token, true, memory);
        }
    }

    private void ValidateVectorAsTypeOperation(Module module, int token, bool equality) =>
        ValidateVectorAsTypeOperationCore(module, token, equality, memory: false);

    private void ValidateVectorAsTypeOperationCore(Module module, int token, bool equality, bool memory)
    {
        var handle = SRME.EntityHandle(token);
        if (handle.Kind != HandleKind.MethodDefinition) throw GuardedVectorAsUnsupported();
        var method = ResolveMethodHandle(module, handle, GenericContext.Empty);
        if (method is null || method.Module != module || method.DeclaringClass.FullName != "System.Type"
            || !method.IsStatic || method.IsSynthetic || method.PInvoke is not null
            || method.Context.MethodArgs.Length != 0 || method.Context.TypeArgs.Length != 0
            || method.Signature.Header.RawValue != 0 || method.Signature.GenericParameterCount != 0
            || method.Name != (equality ? "op_Equality" : "GetTypeFromHandle"))
            throw GuardedVectorAsUnsupported();
        ValidateMagnitudeResolvedDefinition(method, module, (MethodDefinitionHandle)handle);
        if (memory) ValidateUnsupportedAlignedDependencyOwner(method.DeclaringClass, module, "Type");
        var definition = module.Reader.GetMethodDefinition(method.Handle);
        var attributes = MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.HideBySig;
        if (equality) attributes |= MethodAttributes.SpecialName;
        if (definition.Attributes != attributes || definition.ImplAttributes != MethodImplAttributes.IL
            || definition.GetDeclaringType() != method.DeclaringClass.Handle || definition.GetGenericParameters().Count != 0
            || method.Signature.RequiredParameterCount != (equality ? 2 : 1))
            throw GuardedVectorAsUnsupported();
        var raw = module.Reader.GetBlobReader(definition.Signature);
        if (raw.RemainingBytes < 3 || raw.ReadByte() != 0 || raw.ReadByte() != (equality ? 2 : 1))
            throw GuardedVectorAsUnsupported();
        if (equality)
        {
            if (raw.ReadByte() != 2 || !VectorAsRawClass(ref raw, method.DeclaringClass, 0x12)
                || !VectorAsRawClass(ref raw, method.DeclaringClass, 0x12) || raw.RemainingBytes != 0)
                throw GuardedVectorAsUnsupported();
        }
        else
        {
            if (method.Signature.ParameterTypes is not [{ Class: { } handleType }]
                || !VectorAsRawClass(ref raw, method.DeclaringClass, 0x12)
                || !VectorAsRawClass(ref raw, handleType, 0x11) || raw.RemainingBytes != 0)
                throw GuardedVectorAsUnsupported();
        }
        var signature = method.Signature;
        if (equality ? signature.ReturnType is not { Kind: TypeKind.Primitive, IsCanonPlaceholder: false, Primitive: PrimitiveTypeCode.Boolean }
                || signature.ParameterTypes is not [{ Kind: TypeKind.Class, IsCanonPlaceholder: false } left,
                    { Kind: TypeKind.Class, IsCanonPlaceholder: false } right]
                || left.Class != method.DeclaringClass || right.Class != method.DeclaringClass
            : signature.ReturnType is not { Kind: TypeKind.Class, IsCanonPlaceholder: false } returned || returned.Class != method.DeclaringClass
                || signature.ParameterTypes is not [{ Kind: TypeKind.Class, IsCanonPlaceholder: false } argument]
                || argument.Class is not { FullName: "System.RuntimeTypeHandle" } owner || owner.Module != module)
            throw GuardedVectorAsUnsupported();
    }
    private static bool VectorAsRawClass(ref BlobReader raw, ClassInfo type, int code) =>
        raw.RemainingBytes >= 2 && raw.ReadByte() == code
        && raw.TryReadCompressedInteger(out int token) && token == SRME.GetRowNumber(type.Handle) << 2;

}

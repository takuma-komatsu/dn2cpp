using System.Reflection;
using System.Reflection.Metadata;
using SRME = System.Reflection.Metadata.Ecma335.MetadataTokens;

namespace Dn2Cpp;

internal sealed partial class Compilation
{
    private readonly Dictionary<(ClassInfo Owner, MethodDefinitionHandle Definition), MethodInfo> _simdAlignmentBodies = new();

    internal static bool IsSimdAlignmentSlotCandidate(MethodInfo method) =>
        method.Name == "get_Alignment" && SimdAlignmentContractName(method.DeclaringClass);

    // This dispatch admits only numeric closures. In particular, admitting the
    // getter for char would expose the older name-based unsafe-memory lowering.
    internal MethodInfo? TryResolveSimdAlignmentCall(Module module, EntityHandle handle, GenericContext context,
        EntityHandle constrainedHandle, TypeDesc? constrained, MethodInfo target)
    {
        if (!IsSimdAlignmentSlotCandidate(target) || ContainsCanonPlaceholder(target.DeclaringClass))
            return null;
        if (constrained is not { Kind: TypeKind.Class, IsCanonPlaceholder: false, Class: { } vector }
            || vector.Context.TypeArgs is not [var element])
            throw SimdAlignmentUnsupported();
        ValidateSimdAlignmentOwner(vector, requireNumeric: false);
        ValidateSimdAlignmentSlot(target, vector);
        if (constrainedHandle.Kind != HandleKind.TypeSpecification) throw SimdAlignmentUnsupported();
        var receiver = module.Reader.GetBlobReader(module.Reader.GetTypeSpecification((TypeSpecificationHandle)constrainedHandle).Signature);
        if (!ReadVectorValueType(module, ref receiver, context, vector) || receiver.RemainingBytes != 0)
            throw SimdAlignmentUnsupported();
        if (handle.Kind == HandleKind.MemberReference)
        {
            var reference = module.Reader.GetMemberReference((MemberReferenceHandle)handle);
            if (!module.Reader.StringComparer.Equals(reference.Name, target.Name)
                || !CoreIntrinsics.IsVectorAlignmentSignature(module.Reader.GetBlobReader(reference.Signature))
                || reference.Parent.Kind != HandleKind.TypeSpecification) throw SimdAlignmentUnsupported();
            var parent = module.Reader.GetBlobReader(module.Reader.GetTypeSpecification((TypeSpecificationHandle)reference.Parent).Signature);
            if (!ReadSimdAlignmentContractPrefix(module, ref parent, target.DeclaringClass)
                || !ReadVectorValueType(module, ref parent, context, vector)
                || !ReadVectorValueElement(module, ref parent, context, element) || parent.RemainingBytes != 0)
                throw SimdAlignmentUnsupported();
        }
        else if (handle.Kind != HandleKind.MethodDefinition || module != target.Module || handle != target.Handle)
            throw SimdAlignmentUnsupported();
        // Unmapped managed-value vectors otherwise reach their private bridge
        // through ordinary interface dispatch, outside this bounded admission.
        if (!CoreIntrinsics.IsVectorNumericElement(element) || !CoreIntrinsics.IsVectorValueClass(vector))
            throw SimdAlignmentUnsupported();
        return ResolveSimdAlignmentBody(target, vector);
    }

    private static bool SimdAlignmentContractName(ClassInfo owner)
    {
        if (owner.Handle.IsNil) return false;
        var reader = owner.Module.Reader;
        var definition = reader.GetTypeDefinition(owner.Handle);
        return reader.StringComparer.Equals(definition.Namespace, "System.Runtime.Intrinsics")
            && reader.StringComparer.Equals(definition.Name, "ISimdVector`2");
    }

    private void ValidateSimdAlignmentOwner(ClassInfo vector, bool requireNumeric = true)
    {
        if ((!CoreIntrinsics.IsVectorExplicitOwner(vector) && !CoreIntrinsics.IsFixedVectorValueOwner(vector))
            || !vector.IsValueType || vector.IsEnum || vector.IsInterface || vector.IsByRefLike || vector.GenericArity != 1
            || vector.Context.TypeArgs.Length != 1 || (requireNumeric && !CoreIntrinsics.IsVectorValueClass(vector)))
            throw SimdAlignmentUnsupported();
        var reader = vector.Module.Reader;
        var definition = reader.GetTypeDefinition(vector.Handle);
        if (definition.Attributes != (TypeAttributes.Public | TypeAttributes.SequentialLayout | TypeAttributes.Sealed | TypeAttributes.BeforeFieldInit)
            || !definition.GetDeclaringType().IsNil || definition.GetGenericParameters().Count != 1)
            throw SimdAlignmentUnsupported();
        var baseType = TypeDefinitionOf(vector.Module, definition.BaseType);
        if (baseType is not { } identity || identity.Module != vector.Module)
            throw SimdAlignmentUnsupported();
        var baseDefinition = reader.GetTypeDefinition(identity.Handle);
        if (!reader.StringComparer.Equals(baseDefinition.Namespace, "System")
            || !reader.StringComparer.Equals(baseDefinition.Name, "ValueType")) throw SimdAlignmentUnsupported();
        foreach (var parameterHandle in definition.GetGenericParameters())
        {
            var parameter = reader.GetGenericParameter(parameterHandle);
            if (parameter.Parent != vector.Handle || parameter.Index != 0
                || parameter.Attributes != GenericParameterAttributes.None || parameter.GetConstraints().Count != 0)
                throw SimdAlignmentUnsupported();
        }
        foreach (var method in definition.GetMethods())
            if (reader.StringComparer.Equals(reader.GetMethodDefinition(method).Name, ".cctor")) throw SimdAlignmentUnsupported();
    }

    private void ValidateSimdAlignmentSlot(MethodInfo target, ClassInfo vector)
    {
        var contract = target.DeclaringClass;
        if (!SimdAlignmentContractName(contract) || contract.Module != vector.Module || target.Module != vector.Module
            || !contract.IsInterface || contract.IsValueType || contract.IsEnum || contract.GenericArity != 2
            || contract.Context.TypeArgs is not [{ Kind: TypeKind.Class, IsCanonPlaceholder: false, Class: { } self }, var element]
            || self != vector || !SameTypeArg(element, vector.Context.TypeArgs[0])
            || target.Handle.IsNil || target.Name != "get_Alignment" || target.IsSynthetic || target.PInvoke is not null
            || target.Context.MethodArgs.Length != 0 || target.Context.TypeArgs.Length != 2
            || !SameTypeArg(target.Context.TypeArgs[0], contract.Context.TypeArgs[0])
            || !SameTypeArg(target.Context.TypeArgs[1], element)
            || target.Attributes != (MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.Virtual
                | MethodAttributes.HideBySig | MethodAttributes.Abstract | MethodAttributes.SpecialName)
            || target.ImplAttributes != MethodImplAttributes.IL || target.Rva != 0
            || !CoreIntrinsics.IsVectorAlignmentDecodedSignature(target.Signature)) throw SimdAlignmentUnsupported();
        var reader = contract.Module.Reader;
        var owner = reader.GetTypeDefinition(contract.Handle);
        if (owner.Attributes != (TypeAttributes.NotPublic | TypeAttributes.Interface | TypeAttributes.Abstract | TypeAttributes.BeforeFieldInit)
            || !owner.GetDeclaringType().IsNil || !owner.BaseType.IsNil || owner.GetGenericParameters().Count != 2)
            throw SimdAlignmentUnsupported();
        foreach (var parameterHandle in owner.GetGenericParameters())
        {
            var parameter = reader.GetGenericParameter(parameterHandle);
            if (parameter.Parent != contract.Handle || parameter.Index is not (0 or 1)
                || parameter.Attributes != GenericParameterAttributes.None
                || parameter.GetConstraints().Count != (parameter.Index == 0 ? 1 : 0)) throw SimdAlignmentUnsupported();
            foreach (var constraintHandle in parameter.GetConstraints())
            {
                var constraint = reader.GetGenericParameterConstraint(constraintHandle);
                if (constraint.Parameter != parameterHandle || constraint.Type.Kind != HandleKind.TypeSpecification)
                    throw SimdAlignmentUnsupported();
                var raw = reader.GetBlobReader(reader.GetTypeSpecification((TypeSpecificationHandle)constraint.Type).Signature);
                if (!ReadSimdAlignmentContractPrefix(contract.Module, ref raw, contract) || raw.RemainingBytes != 4
                    || raw.ReadByte() != 0x13 || raw.ReadByte() != 0 || raw.ReadByte() != 0x13 || raw.ReadByte() != 1)
                    throw SimdAlignmentUnsupported();
            }
        }
        var definition = reader.GetMethodDefinition(target.Handle);
        if (definition.GetDeclaringType() != contract.Handle || definition.GetGenericParameters().Count != 0
            || !reader.StringComparer.Equals(definition.Name, target.Name) || definition.Attributes != target.Attributes
            || definition.ImplAttributes != target.ImplAttributes || definition.RelativeVirtualAddress != 0
            || !CoreIntrinsics.IsVectorAlignmentSignature(reader.GetBlobReader(definition.Signature)))
            throw SimdAlignmentUnsupported();
    }

    private bool ReadSimdAlignmentContractPrefix(Module module, ref BlobReader raw, ClassInfo contract)
    {
        if (raw.RemainingBytes < 4 || raw.ReadByte() != 0x15 || raw.ReadByte() != 0x12
            || !raw.TryReadCompressedInteger(out int token) || token >> 2 == 0 || (token & 3) > 1)
            return false;
        EntityHandle handle = (token & 3) == 0 ? SRME.TypeDefinitionHandle(token >> 2) : SRME.TypeReferenceHandle(token >> 2);
        var identity = TypeDefinitionOf(module, handle);
        return identity is { } type && type.Module == contract.Module && type.Handle == contract.Handle
            && raw.RemainingBytes > 0 && raw.ReadByte() == 2;
    }

    private bool ReadSimdAlignmentImplementationType(Module module, EntityHandle handle, ClassInfo contract, ClassInfo vector)
    {
        if (handle.Kind != HandleKind.TypeSpecification) return false;
        var raw = module.Reader.GetBlobReader(module.Reader.GetTypeSpecification((TypeSpecificationHandle)handle).Signature);
        return ReadSimdAlignmentContractPrefix(module, ref raw, contract)
            && ReadVectorValueInstantiation(module, ref raw, vector, 0x11) && raw.RemainingBytes == 4
            && raw.ReadByte() == 0x13 && raw.ReadByte() == 0 && raw.ReadByte() == 0x13 && raw.ReadByte() == 0;
    }

    private MethodInfo ResolveSimdAlignmentBody(MethodInfo slot, ClassInfo vector)
    {
        var module = vector.Module;
        var reader = module.Reader;
        var throwOwner = FindClassByFullName("System.ThrowHelper");
        var throwMethods = throwOwner?.MethodsNamed("ThrowAccessViolationException");
        if (throwOwner?.Module != module || throwMethods is null || throwMethods.Count != 1
            || !CoreIntrinsics.IsVectorAccessViolationHelper(throwMethods[0])) throw SimdAlignmentUnsupported();
        ValidateSimdAlignmentBody(throwMethods[0]);
        var owner = reader.GetTypeDefinition(vector.Handle);
        int interfaces = 0;
        foreach (var interfaceHandle in owner.GetInterfaceImplementations())
            if (ReadSimdAlignmentImplementationType(module, reader.GetInterfaceImplementation(interfaceHandle).Interface, slot.DeclaringClass, vector))
                interfaces++;
        if (interfaces != 1) throw SimdAlignmentUnsupported();
        MethodDefinitionHandle selected = default;
        foreach (var implementationHandle in owner.GetMethodImplementations())
        {
            var implementation = reader.GetMethodImplementation(implementationHandle);
            if (implementation.MethodDeclaration.IsNil) throw SimdAlignmentUnsupported();
            if (implementation.MethodDeclaration.Kind == HandleKind.MethodDefinition)
            {
                var declarationHandle = (MethodDefinitionHandle)implementation.MethodDeclaration;
                // The installed closed slot uses a MemberRef. A second edge to
                // its open MethodDef must not evade the uniqueness check.
                if (declarationHandle == slot.Handle
                    || reader.StringComparer.Equals(reader.GetMethodDefinition(declarationHandle).Name, "get_Alignment"))
                    throw SimdAlignmentUnsupported();
                continue;
            }
            if (implementation.MethodDeclaration.Kind != HandleKind.MemberReference) throw SimdAlignmentUnsupported();
            var declaration = reader.GetMemberReference((MemberReferenceHandle)implementation.MethodDeclaration);
            if (!reader.StringComparer.Equals(declaration.Name, "get_Alignment")) continue;
            if (!selected.IsNil || implementation.MethodBody.Kind != HandleKind.MethodDefinition
                || !CoreIntrinsics.IsVectorAlignmentSignature(reader.GetBlobReader(declaration.Signature))
                || !ReadSimdAlignmentImplementationType(module, declaration.Parent, slot.DeclaringClass, vector))
                throw SimdAlignmentUnsupported();
            selected = (MethodDefinitionHandle)implementation.MethodBody;
        }
        if (selected.IsNil) throw SimdAlignmentUnsupported();
        var definition = reader.GetMethodDefinition(selected);
        ValidateSimdAlignmentBodyDefinition(vector, selected);
        // Validate raw authority before consulting a materialized body's cache.
        var key = (vector, selected);
        if (!_simdAlignmentBodies.TryGetValue(key, out var method))
        {
            method = new MethodInfo
            {
                DeclaringClass = vector, Module = module, Handle = selected, Name = reader.GetString(definition.Name),
                Attributes = definition.Attributes, ImplAttributes = definition.ImplAttributes,
                Rva = definition.RelativeVirtualAddress, Context = vector.Context, PInvoke = ReadPInvoke(module, definition),
            };
            _simdAlignmentBodies.Add(key, method);
        }
        ValidateSimdAlignmentBodyModel(method, vector, selected);
        return method;
    }

    private void ValidateSimdAlignmentBodyModel(MethodInfo method, ClassInfo vector, MethodDefinitionHandle handle)
    {
        var definition = vector.Module.Reader.GetMethodDefinition(handle);
        if (method.DeclaringClass != vector || method.Module != vector.Module || method.Handle != handle
            || !vector.Module.Reader.StringComparer.Equals(definition.Name, method.Name)
            || method.IsSynthetic || method.PInvoke is not null || method.Attributes != definition.Attributes
            || method.ImplAttributes != definition.ImplAttributes || method.Rva != definition.RelativeVirtualAddress
            || method.Context.MethodArgs.Length != 0 || method.Context.TypeArgs is not [var element]
            || !SameTypeArg(element, vector.Context.TypeArgs[0])
            || !CoreIntrinsics.IsVectorAlignmentDecodedSignature(method.Signature)) throw SimdAlignmentUnsupported();
    }

    internal void ValidateSimdAlignmentBody(MethodInfo method)
    {
        if (method.Name == "ThrowAccessViolationException" && method.DeclaringClass.FullName == "System.ThrowHelper")
        {
            if (!CoreIntrinsics.IsVectorAccessViolationHelper(method)) throw SimdAlignmentUnsupported();
            var module = method.Module;
            var body = module.PE.GetMethodBody(method.Rva);
            var bytes = body.GetILBytes()!;
            if (!body.LocalSignature.IsNil || body.ExceptionRegions.Length != 0 || bytes.Length != 6
                || bytes[0] != 0x73 || bytes[5] != 0x7a) throw SimdAlignmentUnsupported();
            var token = SRME.EntityHandle(bytes[1] | bytes[2] << 8 | bytes[3] << 16 | bytes[4] << 24);
            if (token.Kind != HandleKind.MethodDefinition) throw SimdAlignmentUnsupported();
            var constructor = module.Reader.GetMethodDefinition((MethodDefinitionHandle)token);
            var exception = module.Reader.GetTypeDefinition(constructor.GetDeclaringType());
            var raw = module.Reader.GetBlobReader(constructor.Signature);
            if (!exception.GetDeclaringType().IsNil || exception.GetGenericParameters().Count != 0
                || !module.Reader.StringComparer.Equals(exception.Namespace, "System")
                || !module.Reader.StringComparer.Equals(exception.Name, "AccessViolationException")
                || !module.Reader.StringComparer.Equals(constructor.Name, ".ctor") || constructor.GetGenericParameters().Count != 0
                || constructor.Attributes != (MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName)
                || constructor.ImplAttributes != MethodImplAttributes.IL || constructor.RelativeVirtualAddress == 0
                || raw.RemainingBytes != 3 || raw.ReadByte() != 0x20 || raw.ReadByte() != 0 || raw.ReadByte() != 1)
                throw SimdAlignmentUnsupported();
            var resolved = ResolveMethodHandle(module, token, GenericContext.Empty);
            if (resolved is null) throw SimdAlignmentUnsupported();
            ValidateMagnitudeResolvedDefinition(resolved, module, (MethodDefinitionHandle)token);
            var owner = resolved.DeclaringClass;
            EnsureShape(owner);
            if (owner.Namespace != "System" || owner.Name != "AccessViolationException" || owner.FullName != "System.AccessViolationException"
                || owner.IsValueType || owner.IsInterface || owner.IsEnum || owner.IsByRefLike || owner.GenericArity != 0
                || owner.Context.TypeArgs.Length != 0 || owner.Context.MethodArgs.Length != 0
                || resolved.Context.TypeArgs.Length != 0 || resolved.Context.MethodArgs.Length != 0
                || resolved.Signature.Header.RawValue != 0x20 || resolved.Signature.GenericParameterCount != 0
                || resolved.Signature.RequiredParameterCount != 0 || resolved.Signature.ParameterTypes.Length != 0
                || resolved.Signature.ReturnType is not { Kind: TypeKind.Primitive, IsCanonPlaceholder: false, Primitive: PrimitiveTypeCode.Void })
                throw SimdAlignmentUnsupported();
            return;
        }
        foreach (var pair in _simdAlignmentBodies)
            if (pair.Value == method)
            {
                ValidateSimdAlignmentOwner(pair.Key.Owner);
                ValidateSimdAlignmentBodyDefinition(pair.Key.Owner, pair.Key.Definition);
                ValidateSimdAlignmentBodyModel(method, pair.Key.Owner, pair.Key.Definition);
                return;
            }
    }

    private void ValidateSimdAlignmentBodyDefinition(ClassInfo vector, MethodDefinitionHandle handle)
    {
        var module = vector.Module;
        var reader = module.Reader;
        var definition = reader.GetMethodDefinition(handle);
        if (definition.GetDeclaringType() != vector.Handle || definition.GetGenericParameters().Count != 0
            || definition.Attributes != (MethodAttributes.Private | MethodAttributes.Static | MethodAttributes.HideBySig | MethodAttributes.SpecialName)
            || definition.ImplAttributes != MethodImplAttributes.IL || definition.RelativeVirtualAddress == 0
            || !reader.GetString(definition.Name).EndsWith(".get_Alignment", StringComparison.Ordinal)
            || !CoreIntrinsics.IsVectorAlignmentSignature(reader.GetBlobReader(definition.Signature))) throw SimdAlignmentUnsupported();
        var body = module.PE.GetMethodBody(definition.RelativeVirtualAddress);
        if (!body.LocalSignature.IsNil || body.ExceptionRegions.Length != 0) throw SimdAlignmentUnsupported();
        var bytes = body.GetILBytes()!;
        if (CoreIntrinsics.IsVectorExplicitOwner(vector))
        {
            if (bytes.Length != 6 || bytes[0] != 0x28 || bytes[5] != 0x2a) throw SimdAlignmentUnsupported();
            int token = bytes[1] | bytes[2] << 8 | bytes[3] << 16 | bytes[4] << 24;
            var dependency = SRME.EntityHandle(token);
            if (dependency.Kind != HandleKind.MethodDefinition
                || !module.MethodMap.TryGetValue((MethodDefinitionHandle)dependency, out var helper)
                || !CoreIntrinsics.IsVectorAlignment(helper)) throw SimdAlignmentUnsupported();
            ValidateSimdNumericsAlignmentDependency(helper, vector);
        }
        else
        {
            int width = CoreIntrinsics.VectorValueWidth(vector);
            if (!(width == 8 ? bytes.Length == 2 && bytes[0] == 0x1e && bytes[1] == 0x2a
                : bytes.Length == 3 && bytes[0] == 0x1f && bytes[1] == width && bytes[2] == 0x2a))
                throw SimdAlignmentUnsupported();
        }
    }

    private void ValidateSimdNumericsAlignmentDependency(MethodInfo helper, ClassInfo vector)
    {
        var module = helper.Module;
        var body = module.PE.GetMethodBody(helper.Rva);
        var bytes = body.GetILBytes()!;
        if (!body.LocalSignature.IsNil || body.ExceptionRegions.Length != 0 || bytes.Length != 6
            || bytes[0] != 0x28 || bytes[5] != 0x2a) throw SimdAlignmentUnsupported();
        var handle = SRME.EntityHandle(bytes[1] | bytes[2] << 8 | bytes[3] << 16 | bytes[4] << 24);
        if (handle.Kind != HandleKind.MemberReference) throw SimdAlignmentUnsupported();
        var reference = module.Reader.GetMemberReference((MemberReferenceHandle)handle);
        if (!module.Reader.StringComparer.Equals(reference.Name, "get_Count")
            || reference.Parent.Kind != HandleKind.TypeSpecification
            || !CoreIntrinsics.IsVectorAlignmentSignature(module.Reader.GetBlobReader(reference.Signature))) throw SimdAlignmentUnsupported();
        var raw = module.Reader.GetBlobReader(module.Reader.GetTypeSpecification((TypeSpecificationHandle)reference.Parent).Signature);
        if (!ReadVectorValueInstantiation(module, ref raw, vector, 0x11) || raw.RemainingBytes != 1 || raw.ReadByte() != 5)
            throw SimdAlignmentUnsupported();
        int countTargets = 0;
        foreach (var member in module.Reader.GetTypeDefinition(vector.Handle).GetMethods())
        {
            var definition = module.Reader.GetMethodDefinition(member);
            if (!module.Reader.StringComparer.Equals(definition.Name, "get_Count")) continue;
            countTargets++;
            if (definition.GetGenericParameters().Count != 0 || definition.RelativeVirtualAddress == 0
                || definition.ImplAttributes != MethodImplAttributes.IL
                || definition.Attributes != (MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.HideBySig | MethodAttributes.SpecialName)
                || !CoreIntrinsics.IsVectorAlignmentSignature(module.Reader.GetBlobReader(definition.Signature)))
                throw SimdAlignmentUnsupported();
        }
        if (countTargets != 1) throw SimdAlignmentUnsupported();
    }

    private static NotSupportedException SimdAlignmentUnsupported() => new(
        "ISimdVector.Alignment requires its exact CoreLib slot, numeric closure and concrete managed MethodImpl");
}

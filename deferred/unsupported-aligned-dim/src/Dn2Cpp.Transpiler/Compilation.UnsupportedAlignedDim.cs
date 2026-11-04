using System.Reflection;
using System.Reflection.Metadata;
using SRME = System.Reflection.Metadata.Ecma335.MetadataTokens;

namespace Dn2Cpp;

internal sealed partial class Compilation
{
    internal sealed class UnsupportedAlignedDimProof
    {
        internal required MethodInfo Root, Alignment, Guard, Bridge, Facade, Getter;
        internal required ClassInfo Vector;
        internal required int AlignmentOffset, UnsafeOffset, AlignmentToken, UnsafeToken;
        internal required int AlignmentConstrainedToken, UnsafeConstrainedToken;
    }

    // These proof-only shells never enter the general callable-member cache.
    private readonly Dictionary<(ClassInfo Owner, MethodDefinitionHandle Definition, ClassInfo Vector), MethodInfo>
        _unsupportedAlignedDimModels = new();

    internal void ValidateUnsupportedAlignedDim(MethodInfo method)
    {
        if (CoreIntrinsics.IsUnsupportedAlignedDimCandidate(method)
            && !ContainsCanonPlaceholder(method.DeclaringClass)
            && method.DeclaringClass.Context.TypeArgs is [{ Class: { } vector }, _]
            && CoreIntrinsics.IsUnsupportedAlignedDimVector(vector))
            _ = ResolveUnsupportedAlignedDimProof(method);
    }

    internal MethodInfo? TryResolveUnsupportedAlignedDimEdge(MethodInfo caller, int offset, int token,
        EntityHandle constrainedHandle, TypeDesc? constrained, out bool terminal)
    {
        terminal = false;
        if (!CoreIntrinsics.IsUnsupportedAlignedDimCandidate(caller)
            || ContainsCanonPlaceholder(caller.DeclaringClass)
            || caller.DeclaringClass.Context.TypeArgs is not [{ Class: { } vector }, _]
            || !CoreIntrinsics.IsUnsupportedAlignedDimVector(vector)) return null;
        var proof = ResolveUnsupportedAlignedDimProof(caller);
        if (offset != proof.AlignmentOffset && offset != proof.UnsafeOffset) return null;
        terminal = offset == proof.UnsafeOffset;
        if (token != (terminal ? proof.UnsafeToken : proof.AlignmentToken)
            || SRME.GetToken(constrainedHandle) != (terminal ? proof.UnsafeConstrainedToken : proof.AlignmentConstrainedToken)
            || constrained is not { Kind: TypeKind.Class, IsCanonPlaceholder: false } self || self.Class != vector)
            throw UnsupportedAlignedDim();
        return terminal ? proof.Guard : proof.Alignment;
    }

    internal UnsupportedAlignedDimProof ResolveUnsupportedAlignedDimProof(MethodInfo method)
    {
        var contract = method.DeclaringClass;
        if (contract.Context.TypeArgs is [{ Class: { } shapeVector }, _]) EnsureShape(shapeVector);
        if (!CoreIntrinsics.IsUnsupportedAlignedDimCandidate(method)
            || contract.Context.TypeArgs is not [{ Kind: TypeKind.Class, IsCanonPlaceholder: false, Class: { } vector }, var element]
            || !CoreIntrinsics.IsUnsupportedAlignedDimVector(vector)
            || !CoreIntrinsics.SameUnsupportedAlignedDimElement(element, vector.Context.TypeArgs[0])
            || contract.Module != vector.Module || method.Module != vector.Module || method.Handle.IsNil)
            throw UnsupportedAlignedDim();
        ValidateUnsupportedAlignedCachedName(contract);
        ValidateUnsupportedAlignedCachedName(vector);
        ValidateSimdAlignmentOwner(vector, requireNumeric: false);
        ValidateUnsupportedAlignedVectorMapping(vector);
        ValidateUnsupportedAlignedContext(method, vector);
        var module = method.Module;
        var reader = module.Reader;
        bool store = method.Name == "StoreAligned";
        if (method.Name is not ("LoadAligned" or "StoreAligned")
            || UnsupportedAlignedRootDefinition(module, contract.Handle, store) != method.Handle)
            throw UnsupportedAlignedDim();
        ValidateMagnitudeResolvedDefinition(method, module, method.Handle);
        if (!UnsupportedAlignedSignature(method, vector, store, pointer: true, generic: false)) throw UnsupportedAlignedDim();
        ValidateMagnitudeNoInitializer(contract);
        ValidateMagnitudeNoInitializer(vector);
        var body = module.PE.GetMethodBody(method.Rva);
        var il = GenericVectorGuardInstructions(method);
        int last = store ? 10 : 9;
        if (!body.LocalSignature.IsNil || il.Count != last + 2
            || il[0].OpCode != (store ? ILOpCode.Ldarg_1 : ILOpCode.Ldarg_0)
            || il[1].OpCode != ILOpCode.Constrained || il[2].OpCode != ILOpCode.Call
            || il[3].OpCode != ILOpCode.Conv_u || il[4].OpCode != ILOpCode.Rem_un
            || il[5].OpCode != ILOpCode.Brfalse_s || il[5].Operand != il[7].Offset
            || il[6].OpCode != ILOpCode.Call || il[7].OpCode != ILOpCode.Ldarg_0
            || store && il[8].OpCode != ILOpCode.Ldarg_1
            || il[last - 1].OpCode != ILOpCode.Constrained || il[last].OpCode != ILOpCode.Call
            || il[last + 1].OpCode != ILOpCode.Ret) throw UnsupportedAlignedDim();
        ValidateUnsupportedAlignedConstrained(module, il[1].Token);
        ValidateUnsupportedAlignedConstrained(module, il[last - 1].Token);
        var alignmentRef = UnsupportedAlignedSlotReference(method, il[2].Token, "get_Alignment", alignment: true, store);
        var alignmentSlot = ResolveMemberRefMethod(module, alignmentRef, method.Context);
        ValidateSimdAlignmentSlot(alignmentSlot, vector);
        ValidateUnsupportedAlignedContext(alignmentSlot, vector);
        var violationToken = SRME.EntityHandle(il[6].Token);
        if (violationToken.Kind != HandleKind.MethodDefinition) throw UnsupportedAlignedDim();
        var violation = ResolveMethodHandle(module, violationToken, GenericContext.Empty);
        if (violation is null || !CoreIntrinsics.IsVectorAccessViolationHelper(violation)) throw UnsupportedAlignedDim();
        ValidateMagnitudeResolvedDefinition(violation, module, (MethodDefinitionHandle)violationToken);
        ValidateSimdAlignmentBody(violation);
        var alignment = ResolveSimdAlignmentBody(alignmentSlot, vector);
        var unsafeRef = UnsupportedAlignedSlotReference(method, il[last].Token, store ? "StoreUnsafe" : "LoadUnsafe", alignment: false, store);
        var bridge = ResolveUnsupportedAlignedBridge(method, vector, unsafeRef, store);
        var facade = ResolveUnsupportedAlignedFacade(bridge, vector, store);
        var facadeIl = GenericVectorGuardInstructions(facade);
        if (!module.PE.GetMethodBody(facade.Rva).LocalSignature.IsNil || facadeIl.Count == 0
            || facadeIl[0].OpCode != ILOpCode.Call) throw UnsupportedAlignedDim();
        var guardToken = SRME.EntityHandle(facadeIl[0].Token);
        if (guardToken.Kind != HandleKind.MethodSpecification) throw UnsupportedAlignedDim();
        var spec = reader.GetMethodSpecification((MethodSpecificationHandle)guardToken);
        if (spec.Method.Kind != HandleKind.MethodDefinition || !UnsupportedAlignedMethodArgument(reader.GetBlobReader(spec.Signature), 0x1e))
            throw UnsupportedAlignedDim();
        var guardDefinition = reader.GetMethodDefinition((MethodDefinitionHandle)spec.Method);
        string guardName = CoreIntrinsics.IsVectorExplicitOwner(vector) ? "ThrowForUnsupportedNumericsVectorBaseType"
            : "ThrowForUnsupportedIntrinsicsVector" + (CoreIntrinsics.UnsupportedAlignedDimWidth(vector) * 8) + "BaseType";
        if (!reader.StringComparer.Equals(guardDefinition.Name, guardName)) throw UnsupportedAlignedDim();
        var guard = TryResolveVectorManagedThrowSpec(module, (MethodSpecificationHandle)guardToken, facade.Context);
        if (guard is null || !CoreIntrinsics.IsVectorManagedThrowHelper(guard)
            || guard.Name != guardName || guard.Context.MethodArgs is not [var guardElement]
            || !CoreIntrinsics.SameUnsupportedAlignedDimElement(guardElement, element)) throw UnsupportedAlignedDim();
        ValidateMagnitudeResolvedDefinition(guard, module, (MethodDefinitionHandle)spec.Method);
        ValidateMagnitudeNoInitializer(guard.DeclaringClass);
        ValidateGenericVectorThrowGuardCore(guard, vector, memory: true);
        var guardIl = GenericVectorGuardInstructions(guard);
        if (!module.PE.GetMethodBody(guard.Rva).LocalSignature.IsNil || guardIl[2].Operand != 0x43)
            throw UnsupportedAlignedDim();
        var getter = ValidateGenericVectorUnsupportedGetterCore(guard, guardIl[0].Token, vector, memory: true);
        ValidateVectorAsSupportedWhitelistCore(getter, memory: true);
        ValidateUnsupportedAlignedResource(module);
        return new UnsupportedAlignedDimProof
        {
            Root = method, Vector = vector, Alignment = alignment, Guard = guard,
            Bridge = bridge, Facade = facade, Getter = getter,
            AlignmentOffset = il[2].Offset, UnsafeOffset = il[last].Offset,
            AlignmentToken = il[2].Token, UnsafeToken = il[last].Token,
            AlignmentConstrainedToken = il[1].Token, UnsafeConstrainedToken = il[last - 1].Token,
        };
    }

    private static void ValidateUnsupportedAlignedCachedName(ClassInfo owner)
    {
        // Emitted identity must not depend on whether CppName was already cached.
        string expected = string.IsNullOrEmpty(owner.Namespace) ? owner.Name : owner.Namespace + "." + owner.Name;
        if (owner.FullName != expected) throw UnsupportedAlignedDim();
    }

    private void ValidateUnsupportedAlignedDependencyOwner(ClassInfo owner, Module module, string name)
    {
        if (owner.Module != module || module.AssemblyName != "System.Private.CoreLib" || owner.Handle.IsNil)
            throw UnsupportedAlignedDim();
        var reader = module.Reader;
        var definition = reader.GetTypeDefinition(owner.Handle);
        if (!definition.GetDeclaringType().IsNil || definition.GetGenericParameters().Count != 0
            || !reader.StringComparer.Equals(definition.Namespace, "System")
            || !reader.StringComparer.Equals(definition.Name, name))
            throw UnsupportedAlignedDim();
        // These known nongeneric reference owners never acquire method arguments.
        if (owner.Namespace != "System" || owner.Name != name || owner.FullName != "System." + name
            || owner.GenericArity != 0 || owner.Context.TypeArgs.Length != 0 || owner.Context.MethodArgs.Length != 0
            || owner.IsValueType || owner.IsInterface || owner.IsEnum || owner.IsByRefLike)
            throw UnsupportedAlignedDim();
    }

    private void ValidateUnsupportedAlignedVectorMapping(ClassInfo vector)
    {
        var element = vector.Context.TypeArgs[0];
        if (CoreIntrinsics.IsUnsupportedAlignedDimChar(element))
        {
            if (!CoreIntrinsics.IsFixedVectorValueStorageClass(vector)) throw UnsupportedAlignedDim();
            return;
        }
        if (vector.IntrinsicCppName is not null || element.Class is not { } value || value.Module != vector.Module)
            throw UnsupportedAlignedDim();
        EnsureShape(value);
        var reader = value.Module.Reader;
        var definition = reader.GetTypeDefinition(value.Handle);
        string name = reader.GetString(definition.Name);
        if (value.Namespace != "System" || value.Name != name || value.FullName != "System." + name)
            throw UnsupportedAlignedDim();
    }

    private static MethodDefinitionHandle UnsupportedAlignedRootDefinition(Module module, TypeDefinitionHandle contract, bool store)
    {
        var reader = module.Reader;
        MethodDefinitionHandle selected = default;
        foreach (var candidate in reader.GetTypeDefinition(contract).GetMethods())
        {
            var definition = reader.GetMethodDefinition(candidate);
            if (!reader.StringComparer.Equals(definition.Name, store ? "StoreAligned" : "LoadAligned")) continue;
            if (!selected.IsNil || definition.GetDeclaringType() != contract || definition.GetGenericParameters().Count != 0
                || definition.Attributes != (MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.Virtual | MethodAttributes.HideBySig)
                || definition.ImplAttributes != MethodImplAttributes.IL || definition.RelativeVirtualAddress == 0
                || !ReadUnsupportedAlignedRootSignature(reader.GetBlobReader(definition.Signature), store))
                throw UnsupportedAlignedDim();
            selected = candidate;
        }
        if (selected.IsNil) throw UnsupportedAlignedDim();
        return selected;
    }

    private void ValidateUnsupportedAlignedContext(MethodInfo method, ClassInfo vector)
    {
        var contract = method.DeclaringClass;
        if (!SimdAlignmentContractName(contract) || contract.Module != vector.Module || method.Module != vector.Module
            || !contract.IsInterface || contract.IsValueType || contract.IsEnum || contract.IsByRefLike
            || contract.GenericArity != 2 || contract.Context.MethodArgs.Length != 0
            || contract.Context.TypeArgs is not [{ Kind: TypeKind.Class, IsCanonPlaceholder: false } self, var element]
            || self.Class != vector || !CoreIntrinsics.SameUnsupportedAlignedDimElement(element, vector.Context.TypeArgs[0])
            || method.Context.MethodArgs.Length != 0
            || method.Context.TypeArgs is not [{ Kind: TypeKind.Class, IsCanonPlaceholder: false } actualSelf, var actualElement]
            || actualSelf.Class != vector || !CoreIntrinsics.SameUnsupportedAlignedDimElement(actualElement, element))
            throw UnsupportedAlignedDim();
        var definition = contract.Module.Reader.GetTypeDefinition(contract.Handle);
        if (definition.Attributes != (TypeAttributes.NotPublic | TypeAttributes.Interface | TypeAttributes.Abstract | TypeAttributes.BeforeFieldInit)
            || !definition.GetDeclaringType().IsNil || !definition.BaseType.IsNil || definition.GetGenericParameters().Count != 2)
            throw UnsupportedAlignedDim();
        ValidateNumberContractParameters(contract.Module, contract.Handle);
    }

    private static bool ReadUnsupportedAlignedRootSignature(BlobReader raw, bool store) =>
        raw.RemainingBytes == (store ? 8 : 7) && raw.ReadByte() == 0 && raw.ReadByte() == (store ? 2 : 1)
        && (!store || raw.ReadByte() == 1) && raw.ReadByte() == 0x13 && raw.ReadByte() == 0
        && raw.ReadByte() == 0x0f && raw.ReadByte() == 0x13 && raw.ReadByte() == 1;

    private static bool UnsupportedAlignedSignature(MethodInfo method, ClassInfo vector, bool store, bool pointer, bool generic)
    {
        var signature = method.Signature;
        if (signature.Header.RawValue != (generic ? 0x10 : 0) || signature.GenericParameterCount != (generic ? 1 : 0)
            || signature.RequiredParameterCount != (store ? 2 : 1) || signature.ParameterTypes.Length != (store ? 2 : 1)) return false;
        var result = store ? signature.ParameterTypes[0] : signature.ReturnType;
        var address = signature.ParameterTypes[store ? 1 : 0];
        return result is { Kind: TypeKind.Class, IsCanonPlaceholder: false } && result.Class == vector
            && (!store || signature.ReturnType is { Kind: TypeKind.Primitive, IsCanonPlaceholder: false, Primitive: PrimitiveTypeCode.Void })
            && address.Kind == (pointer ? TypeKind.Pointer : TypeKind.ByRef) && !address.IsCanonPlaceholder
            && !address.IsFunctionPointer && address.Element is { } element
            && CoreIntrinsics.SameUnsupportedAlignedDimElement(element, vector.Context.TypeArgs[0]);
    }

    private static void ValidateUnsupportedAlignedConstrained(Module module, int token)
    {
        var handle = SRME.EntityHandle(token);
        if (handle.Kind != HandleKind.TypeSpecification) throw UnsupportedAlignedDim();
        var raw = module.Reader.GetBlobReader(module.Reader.GetTypeSpecification((TypeSpecificationHandle)handle).Signature);
        if (raw.RemainingBytes != 2 || raw.ReadByte() != 0x13 || raw.ReadByte() != 0) throw UnsupportedAlignedDim();
    }

    private MemberReferenceHandle UnsupportedAlignedSlotReference(MethodInfo root, int token, string name, bool alignment, bool store)
    {
        var handle = SRME.EntityHandle(token);
        var reader = root.Module.Reader;
        if (handle.Kind != HandleKind.MemberReference) throw UnsupportedAlignedDim();
        var reference = reader.GetMemberReference((MemberReferenceHandle)handle);
        if (!reader.StringComparer.Equals(reference.Name, name) || reference.Parent.Kind != HandleKind.TypeSpecification)
            throw UnsupportedAlignedDim();
        var parent = reader.GetBlobReader(reader.GetTypeSpecification((TypeSpecificationHandle)reference.Parent).Signature);
        if (!ReadSimdAlignmentContractPrefix(root.Module, ref parent, root.DeclaringClass)
            || parent.RemainingBytes != 4 || parent.ReadByte() != 0x13 || parent.ReadByte() != 0
            || parent.ReadByte() != 0x13 || parent.ReadByte() != 1
            || !(alignment ? CoreIntrinsics.IsVectorAlignmentSignature(reader.GetBlobReader(reference.Signature))
                : ReadUnsupportedAlignedSlotSignature(root.Module, reader.GetBlobReader(reference.Signature), store, offset: false)))
            throw UnsupportedAlignedDim();
        return (MemberReferenceHandle)handle;
    }

    private static bool ReadUnsupportedAlignedSlotSignature(Module module, BlobReader raw, bool store, bool offset)
    {
        if (raw.RemainingBytes < 7 || raw.ReadByte() != 0 || raw.ReadByte() != (store ? 2 : 1) + (offset ? 1 : 0)
            || store && raw.ReadByte() != 1 || raw.ReadByte() != 0x13 || raw.ReadByte() != 0
            || !store && !ReadUnsupportedAlignedInModifier(module, ref raw)
            || raw.RemainingBytes < 3 || raw.ReadByte() != 0x10 || raw.ReadByte() != 0x13 || raw.ReadByte() != 1)
            return false;
        return offset ? raw.RemainingBytes == 1 && raw.ReadByte() == 0x19 : raw.RemainingBytes == 0;
    }

    private static bool ReadUnsupportedAlignedInModifier(Module module, ref BlobReader raw)
    {
        if (raw.RemainingBytes < 2 || raw.ReadByte() != 0x1f || !raw.TryReadCompressedInteger(out int token)
            || token <= 0 || (token & 3) != 0) return false;
        var definition = module.Reader.GetTypeDefinition(SRME.TypeDefinitionHandle(token >> 2));
        return definition.GetDeclaringType().IsNil && definition.GetGenericParameters().Count == 0
            && module.Reader.StringComparer.Equals(definition.Namespace, "System.Runtime.InteropServices")
            && module.Reader.StringComparer.Equals(definition.Name, "InAttribute");
    }

    private MethodInfo ResolveUnsupportedAlignedBridge(MethodInfo root, ClassInfo vector, MemberReferenceHandle reference, bool store)
    {
        var module = root.Module;
        var reader = module.Reader;
        string name = store ? "StoreUnsafe" : "LoadUnsafe";
        MethodDefinitionHandle target = default, offsetTarget = default;
        foreach (var candidate in reader.GetTypeDefinition(root.DeclaringClass.Handle).GetMethods())
        {
            var definition = reader.GetMethodDefinition(candidate);
            if (!reader.StringComparer.Equals(definition.Name, name)) continue;
            bool offset = ReadUnsupportedAlignedSlotSignature(module, reader.GetBlobReader(definition.Signature), store, offset: true);
            if (!offset && !ReadUnsupportedAlignedSlotSignature(module, reader.GetBlobReader(definition.Signature), store, offset: false)
                || !(offset ? offsetTarget.IsNil : target.IsNil)
                || definition.GetGenericParameters().Count != 0 || definition.ImplAttributes != MethodImplAttributes.IL
                || definition.Attributes != (MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.Virtual
                    | MethodAttributes.HideBySig | (offset ? MethodAttributes.Abstract : 0))
                || (definition.RelativeVirtualAddress == 0) != offset) throw UnsupportedAlignedDim();
            if (offset) offsetTarget = candidate; else target = candidate;
        }
        if (target.IsNil || offsetTarget.IsNil) throw UnsupportedAlignedDim();
        var slot = ResolveMemberRefMethod(module, reference, root.Context);
        ValidateMagnitudeResolvedDefinition(slot, module, target);
        ValidateUnsupportedAlignedContext(slot, vector);
        if (!UnsupportedAlignedSignature(slot, vector, store, pointer: false, generic: false)) throw UnsupportedAlignedDim();
        MethodDefinitionHandle selected = default, offsetBody = default;
        foreach (var implementationHandle in reader.GetTypeDefinition(vector.Handle).GetMethodImplementations())
        {
            var implementation = reader.GetMethodImplementation(implementationHandle);
            if (implementation.MethodDeclaration.IsNil) throw UnsupportedAlignedDim();
            if (implementation.MethodDeclaration.Kind == HandleKind.MethodDefinition)
            {
                var declared = (MethodDefinitionHandle)implementation.MethodDeclaration;
                if (declared == target || declared == offsetTarget
                    || reader.StringComparer.Equals(reader.GetMethodDefinition(declared).Name, name)) throw UnsupportedAlignedDim();
                continue;
            }
            if (implementation.MethodDeclaration.Kind != HandleKind.MemberReference) throw UnsupportedAlignedDim();
            var declaration = reader.GetMemberReference((MemberReferenceHandle)implementation.MethodDeclaration);
            if (!reader.StringComparer.Equals(declaration.Name, name)) continue;
            bool offset = ReadUnsupportedAlignedSlotSignature(module, reader.GetBlobReader(declaration.Signature), store, offset: true);
            if (!offset && !ReadUnsupportedAlignedSlotSignature(module, reader.GetBlobReader(declaration.Signature), store, offset: false)
                || !(offset ? offsetBody.IsNil : selected.IsNil) || implementation.MethodBody.Kind != HandleKind.MethodDefinition
                || !ReadSimdAlignmentImplementationType(module, declaration.Parent, root.DeclaringClass, vector))
                throw UnsupportedAlignedDim();
            var body = (MethodDefinitionHandle)implementation.MethodBody;
            ValidateUnsupportedAlignedBridgeDefinition(vector, body, store, offset);
            if (offset) offsetBody = body; else selected = body;
        }
        if (selected.IsNil || offsetBody.IsNil || selected == offsetBody) throw UnsupportedAlignedDim();
        var bridge = UnsupportedAlignedModel(vector, selected, vector, vector.Context);
        if (bridge.Context.MethodArgs.Length != 0 || bridge.Context.TypeArgs is not [var element]
            || !CoreIntrinsics.SameUnsupportedAlignedDimElement(element, vector.Context.TypeArgs[0])
            || !UnsupportedAlignedSignature(bridge, vector, store, pointer: false, generic: false)) throw UnsupportedAlignedDim();
        return bridge;
    }

    private static void ValidateUnsupportedAlignedBridgeDefinition(ClassInfo vector, MethodDefinitionHandle handle, bool store, bool offset)
    {
        var reader = vector.Module.Reader;
        var definition = reader.GetMethodDefinition(handle);
        if (definition.GetDeclaringType() != vector.Handle || definition.GetGenericParameters().Count != 0
            || definition.Attributes != (MethodAttributes.Private | MethodAttributes.Static | MethodAttributes.HideBySig)
            || definition.ImplAttributes != MethodImplAttributes.IL || definition.RelativeVirtualAddress == 0
            || !reader.GetString(definition.Name).EndsWith(store ? ".StoreUnsafe" : ".LoadUnsafe", StringComparison.Ordinal)
            || !ReadUnsupportedAlignedConcreteSignature(vector, reader.GetBlobReader(definition.Signature), store, offset, generic: false))
            throw UnsupportedAlignedDim();
    }

    private static bool ReadUnsupportedAlignedConcreteSignature(ClassInfo vector, BlobReader raw, bool store, bool offset, bool generic)
    {
        if (raw.RemainingBytes < 3 || raw.ReadByte() != (generic ? 0x10 : 0) || generic && raw.ReadByte() != 1
            || raw.ReadByte() != (store ? 2 : 1) + (offset ? 1 : 0) || store && raw.ReadByte() != 1
            || !ReadFixedGenericVectorEstimateType(ref raw, vector.Handle, generic ? 0x1e : 0x13)
            || !store && !generic && !ReadUnsupportedAlignedInModifier(vector.Module, ref raw)
            || raw.RemainingBytes < 3 || raw.ReadByte() != 0x10 || raw.ReadByte() != (generic ? 0x1e : 0x13)
            || raw.ReadByte() != 0) return false;
        return offset ? raw.RemainingBytes == 1 && raw.ReadByte() == 0x19 : raw.RemainingBytes == 0;
    }

    private MethodInfo ResolveUnsupportedAlignedFacade(MethodInfo bridge, ClassInfo vector, bool store)
    {
        var module = bridge.Module;
        var reader = module.Reader;
        var il = GenericVectorGuardInstructions(bridge);
        int call = store ? 2 : 1;
        if (!module.PE.GetMethodBody(bridge.Rva).LocalSignature.IsNil || il.Count != call + 2
            || il[0].OpCode != ILOpCode.Ldarg_0 || store && il[1].OpCode != ILOpCode.Ldarg_1
            || il[call].OpCode != ILOpCode.Call || il[call + 1].OpCode != ILOpCode.Ret)
            throw UnsupportedAlignedDim();
        var handle = SRME.EntityHandle(il[call].Token);
        if (handle.Kind != HandleKind.MethodSpecification) throw UnsupportedAlignedDim();
        var spec = reader.GetMethodSpecification((MethodSpecificationHandle)handle);
        if (spec.Method.Kind != HandleKind.MethodDefinition || !UnsupportedAlignedMethodArgument(reader.GetBlobReader(spec.Signature), 0x13))
            throw UnsupportedAlignedDim();
        var definitionHandle = (MethodDefinitionHandle)spec.Method;
        var definition = reader.GetMethodDefinition(definitionHandle);
        var owner = reader.GetTypeDefinition(definition.GetDeclaringType());
        var vectorDefinition = reader.GetTypeDefinition(vector.Handle);
        string vectorName = reader.GetString(vectorDefinition.Name);
        if (!reader.StringComparer.Equals(owner.Namespace, reader.GetString(vectorDefinition.Namespace))
            || !reader.StringComparer.Equals(owner.Name, vectorName[..^2])
            || !owner.GetDeclaringType().IsNil || owner.GetGenericParameters().Count != 0
            || !reader.StringComparer.Equals(definition.Name, store ? "StoreUnsafe" : "LoadUnsafe")
            || definition.Attributes != (MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.HideBySig)
            || definition.ImplAttributes != MethodImplAttributes.AggressiveInlining || definition.RelativeVirtualAddress == 0
            || definition.GetGenericParameters().Count != 1
            || !ReadUnsupportedAlignedConcreteSignature(vector, reader.GetBlobReader(definition.Signature), store, offset: false, generic: true))
            throw UnsupportedAlignedDim();
        foreach (var parameterHandle in definition.GetGenericParameters())
        {
            var parameter = reader.GetGenericParameter(parameterHandle);
            if (parameter.Parent != definitionHandle || parameter.Index != 0 || parameter.Attributes != GenericParameterAttributes.None
                || parameter.GetConstraints().Count != 0) throw UnsupportedAlignedDim();
        }
        var facadeClass = GetClass(module, definition.GetDeclaringType());
        if (!CoreIntrinsics.IsVectorEstimateOwner(facadeClass) || facadeClass.Context.TypeArgs.Length != 0
            || facadeClass.Context.MethodArgs.Length != 0) throw UnsupportedAlignedDim();
        ValidateMagnitudeNoInitializer(facadeClass);
        var facade = UnsupportedAlignedModel(facadeClass, definitionHandle, vector,
            new GenericContext(Array.Empty<TypeDesc>(), new[] { vector.Context.TypeArgs[0] }));
        if (facade.Context.TypeArgs.Length != 0 || facade.Context.MethodArgs is not [var element]
            || !CoreIntrinsics.SameUnsupportedAlignedDimElement(element, vector.Context.TypeArgs[0])
            || !UnsupportedAlignedSignature(facade, vector, store, pointer: false, generic: true)) throw UnsupportedAlignedDim();
        return facade;
    }

    private MethodInfo UnsupportedAlignedModel(ClassInfo owner, MethodDefinitionHandle handle, ClassInfo vector, GenericContext context)
    {
        var definition = owner.Module.Reader.GetMethodDefinition(handle);
        var key = (owner, handle, vector);
        if (!_unsupportedAlignedDimModels.TryGetValue(key, out var method))
        {
            if (Phase == EmitPhase.Emission) throw new InvalidOperationException("Aligned DIM proof shell was not prepared before emission");
            method = new MethodInfo
            {
                DeclaringClass = owner, Module = owner.Module, Handle = handle,
                Name = owner.Module.Reader.GetString(definition.Name), Attributes = definition.Attributes,
                ImplAttributes = definition.ImplAttributes, Rva = definition.RelativeVirtualAddress, Context = context,
                PInvoke = ReadPInvoke(owner.Module, definition),
            };
            _unsupportedAlignedDimModels.Add(key, method);
        }
        ValidateMagnitudeResolvedDefinition(method, owner.Module, handle);
        if (method.DeclaringClass != owner) throw UnsupportedAlignedDim();
        return method;
    }

    private static bool UnsupportedAlignedMethodArgument(BlobReader raw, int variable) =>
        raw.RemainingBytes == 4 && raw.ReadByte() == 0x0a && raw.ReadByte() == 1
        && raw.ReadByte() == variable && raw.ReadByte() == 0;

    private void ValidateUnsupportedAlignedResource(Module module)
    {
        var resource = FindClassByFullName("System.ExceptionResource");
        if (resource is null || resource.Module != module || !resource.IsEnum) throw UnsupportedAlignedDim();
        int matches = 0;
        foreach (var handle in module.Reader.GetTypeDefinition(resource.Handle).GetFields())
        {
            var field = module.Reader.GetFieldDefinition(handle);
            if (!module.Reader.StringComparer.Equals(field.Name, "Arg_TypeNotSupported")) continue;
            var raw = module.Reader.GetBlobReader(field.Signature);
            if (field.Attributes != (FieldAttributes.Public | FieldAttributes.Static | FieldAttributes.Literal | FieldAttributes.HasDefault)
                || raw.RemainingBytes < 3 || raw.ReadByte() != 6 || !VectorAsRawClass(ref raw, resource, 0x11)
                || raw.RemainingBytes != 0 || field.GetDefaultValue().IsNil) throw UnsupportedAlignedDim();
            var constant = module.Reader.GetConstant(field.GetDefaultValue());
            var value = module.Reader.GetBlobReader(constant.Value);
            if (constant.Parent != handle || constant.TypeCode != ConstantTypeCode.Int32
                || value.RemainingBytes != 4 || value.ReadInt32() != 0x43) throw UnsupportedAlignedDim();
            matches++;
        }
        if (matches != 1) throw UnsupportedAlignedDim();
    }

    private bool ReadUnsupportedAlignedElement(Module module, ref BlobReader raw, GenericContext context, TypeDesc element)
    {
        if (raw.RemainingBytes == 0) return false;
        int code = raw.ReadByte();
        if (code is 0x13 or 0x1e)
        {
            if (!raw.TryReadCompressedInteger(out int index)) return false;
            var arguments = code == 0x13 ? context.TypeArgs : context.MethodArgs;
            return index >= 0 && index < arguments.Length && CoreIntrinsics.SameUnsupportedAlignedDimElement(arguments[index], element);
        }
        if (CoreIntrinsics.IsUnsupportedAlignedDimChar(element)) return code == 3;
        if (element.Kind != TypeKind.Class || element.Class is not { } value || code != 0x11
            || !raw.TryReadCompressedInteger(out int token) || token >> 2 == 0 || (token & 3) > 1) return false;
        var handle = (token & 3) == 0 ? (EntityHandle)SRME.TypeDefinitionHandle(token >> 2) : SRME.TypeReferenceHandle(token >> 2);
        return TypeDefinitionOf(module, handle) is { } identity && identity.Module == value.Module && identity.Handle == value.Handle;
    }

    internal MethodInfo? TryResolveUnsupportedAlignedDimReference(Module module, MemberReferenceHandle handle, GenericContext context)
    {
        var reader = module.Reader;
        var reference = reader.GetMemberReference(handle);
        if (!reader.StringComparer.Equals(reference.Name, "LoadAligned")
            && !reader.StringComparer.Equals(reference.Name, "StoreAligned")) return null;
        if (reference.Parent.Kind != HandleKind.TypeSpecification) return null;
        var decoded = reader.GetTypeSpecification((TypeSpecificationHandle)reference.Parent).DecodeSignature(SigProvider, context);
        var owner = decoded.Class;
        // Authenticate the original CoreLib parent before reading the mutable
        // closed owner's metadata name. Nonmatching parents keep the old path.
        if (owner is not null && TypeDefinitionOf(module, reference.Parent) is { } originalOwner
            && originalOwner.Module.AssemblyName == "System.Private.CoreLib" && !originalOwner.Handle.IsNil
            && SRME.GetRowNumber(originalOwner.Handle) <= originalOwner.Module.Reader.TypeDefinitions.Count)
        {
            var originalReader = originalOwner.Module.Reader;
            var originalDefinition = originalReader.GetTypeDefinition(originalOwner.Handle);
            if (originalReader.StringComparer.Equals(originalDefinition.Namespace, "System.Runtime.Intrinsics")
                && originalReader.StringComparer.Equals(originalDefinition.Name, "ISimdVector`2")
                && (owner.Module != originalOwner.Module || owner.Handle != originalOwner.Handle))
                throw UnsupportedAlignedDim();
        }
        if (owner is null || !SimdAlignmentContractName(owner) || ContainsCanonPlaceholder(owner)
            || owner.Context.TypeArgs is not [{ Class: { } vector }, var element]
            || !CoreIntrinsics.IsUnsupportedAlignedDimVector(vector)) return null;
        // The generic MemberRef cache omits parts of caller context. Authenticate
        // the raw parent and signature before any cached method can be returned.
        var raw = reader.GetBlobReader(reader.GetTypeSpecification((TypeSpecificationHandle)reference.Parent).Signature);
        if (decoded.Kind != TypeKind.Class || decoded.IsCanonPlaceholder
            || !ReadSimdAlignmentContractPrefix(module, ref raw, owner)
            || !ReadUnsupportedAlignedVector(module, ref raw, context, vector)
            || !ReadUnsupportedAlignedElement(module, ref raw, context, element) || raw.RemainingBytes != 0)
            throw UnsupportedAlignedDim();
        bool store = reader.StringComparer.Equals(reference.Name, "StoreAligned");
        if (!ReadUnsupportedAlignedRootSignature(reader.GetBlobReader(reference.Signature), store)) throw UnsupportedAlignedDim();
        var expected = UnsupportedAlignedRootDefinition(owner.Module, owner.Handle, store);
        ValidateUnsupportedAlignedCachedName(owner);
        ValidateUnsupportedAlignedCachedName(vector);
        EnsureCompleted(owner);
        var methods = owner.MethodsNamed(store ? "StoreAligned" : "LoadAligned");
        if (methods is null || methods.Count != 1 || methods[0].Handle != expected) throw UnsupportedAlignedDim();
        var result = methods[0];
        _ = ResolveUnsupportedAlignedDimProof(result);
        return result;
    }

    private bool ReadUnsupportedAlignedVector(Module module, ref BlobReader raw, GenericContext context, ClassInfo vector)
    {
        if (raw.RemainingBytes == 0) return false;
        var probe = raw;
        int code = probe.ReadByte();
        if (code is 0x13 or 0x1e)
        {
            if (!probe.TryReadCompressedInteger(out int index)) return false;
            var arguments = code == 0x13 ? context.TypeArgs : context.MethodArgs;
            if (index < 0 || index >= arguments.Length || arguments[index] is not { Kind: TypeKind.Class, IsCanonPlaceholder: false } value
                || value.Class != vector) return false;
            raw = probe;
            return true;
        }
        return ReadVectorValueInstantiation(module, ref raw, vector, 0x11)
            && ReadUnsupportedAlignedElement(module, ref raw, context, vector.Context.TypeArgs[0]);
    }

    private void RejectUnsupportedAlignedDimSpecification(Module module, MethodSpecificationHandle handle, GenericContext context)
    {
        var specification = module.Reader.GetMethodSpecification(handle);
        if (MethodSpecMethodName(module, specification) is not ("LoadAligned" or "StoreAligned")) return;
        if (specification.Method.Kind == HandleKind.MemberReference)
        {
            if (TryResolveUnsupportedAlignedDimReference(module, (MemberReferenceHandle)specification.Method, context) is not null)
                throw UnsupportedAlignedDim();
        }
        else if (specification.Method.Kind == HandleKind.MethodDefinition)
        {
            var definition = module.Reader.GetMethodDefinition((MethodDefinitionHandle)specification.Method);
            var owner = module.Reader.GetTypeDefinition(definition.GetDeclaringType());
            if (module.Reader.StringComparer.Equals(owner.Namespace, "System.Runtime.Intrinsics")
                && module.Reader.StringComparer.Equals(owner.Name, "ISimdVector`2")) throw UnsupportedAlignedDim();
        }
    }

    private static NotSupportedException UnsupportedAlignedDim() => new(
        "Unsupported aligned SIMD DIM requires its exact caller, alignment branch and terminal unsupported-type guard");
}

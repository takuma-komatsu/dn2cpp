using System.Reflection;
using System.Reflection.Metadata;
using SRME = System.Reflection.Metadata.Ecma335.MetadataTokens;

namespace Dn2Cpp;

internal sealed partial class Compilation
{
    internal sealed class UnsupportedAlignedEdges
    {
        internal required ClassInfo Vector;
        internal required MethodInfo Alignment, Violation, Sink;
        internal required Instruction AlignmentCall, UnsafeCall;
        internal required int ConstrainedToken, Resource;
    }

    internal UnsupportedAlignedEdges? ResolveUnsupportedAlignedEdges(MethodInfo root)
    {
        if (!CoreIntrinsics.IsUnsupportedAlignedDefault(root)) throw UnsupportedAlignedEdge();
        var vector = root.DeclaringClass.Context.TypeArgs[0].Class!;
        if (!CoreIntrinsics.IsOrderedUnsupportedAlignedVector(vector)) return null;
        ValidateSimdAlignmentOwner(vector, requireNumeric: false);
        var module = root.Module;
        var reader = module.Reader;
        var body = module.PE.GetMethodBody(root.Rva);
        var il = GenericVectorGuardInstructions(root);
        bool store = root.Name == "StoreAligned";
        int last = store ? 10 : 9;
        if (!body.LocalSignature.IsNil || il.Count != last + 2
            || il[0].OpCode != (store ? ILOpCode.Ldarg_1 : ILOpCode.Ldarg_0)
            || il[1].OpCode != ILOpCode.Constrained || il[2].OpCode != ILOpCode.Call
            || il[3].OpCode != ILOpCode.Conv_u || il[4].OpCode != ILOpCode.Rem_un
            || il[5].OpCode != ILOpCode.Brfalse_s || il[5].Operand != il[7].Offset
            || il[6].OpCode != ILOpCode.Call || il[7].OpCode != ILOpCode.Ldarg_0
            || store && il[8].OpCode != ILOpCode.Ldarg_1
            || il[last - 1].OpCode != ILOpCode.Constrained || il[last].OpCode != ILOpCode.Call
            || il[last + 1].OpCode != ILOpCode.Ret || il[1].Token != il[last - 1].Token)
            throw UnsupportedAlignedEdge();
        var self = SRME.EntityHandle(il[1].Token);
        if (self.Kind != HandleKind.TypeSpecification) throw UnsupportedAlignedEdge();
        var receiver = reader.GetBlobReader(reader.GetTypeSpecification((TypeSpecificationHandle)self).Signature);
        if (receiver.RemainingBytes != 2 || receiver.ReadByte() != 0x13 || receiver.ReadByte() != 0)
            throw UnsupportedAlignedEdge();
        var alignmentRef = UnsupportedAlignedSlotReference(root, il[2].Token, "get_Alignment", alignment: true, store);
        var slot = ResolveMemberRefMethod(module, alignmentRef, root.Context);
        ValidateSimdAlignmentSlot(slot, vector);
        var violationHandle = SRME.EntityHandle(il[6].Token);
        if (violationHandle.Kind != HandleKind.MethodDefinition) throw UnsupportedAlignedEdge();
        var violation = ResolveMethodHandle(module, violationHandle, GenericContext.Empty);
        if (violation is null || !CoreIntrinsics.IsVectorAccessViolationHelper(violation)) throw UnsupportedAlignedEdge();
        ValidateMagnitudeResolvedDefinition(violation, module, (MethodDefinitionHandle)violationHandle);
        ValidateSimdAlignmentBody(violation);

        var unsafeRef = UnsupportedAlignedSlotReference(root, il[last].Token, store ? "StoreUnsafe" : "LoadUnsafe", alignment: false, store);
        var bridge = ResolveUnsupportedAlignedBridge(root, vector, unsafeRef, store);
        var facade = ResolveUnsupportedAlignedFacade(bridge, vector, store);
        var facadeIl = GenericVectorGuardInstructions(facade);
        if (!module.PE.GetMethodBody(facade.Rva).LocalSignature.IsNil || facadeIl.Count == 0
            || facadeIl[0].OpCode != ILOpCode.Call) throw UnsupportedAlignedEdge();
        var guardHandle = SRME.EntityHandle(facadeIl[0].Token);
        if (guardHandle.Kind != HandleKind.MethodSpecification) throw UnsupportedAlignedEdge();
        var spec = reader.GetMethodSpecification((MethodSpecificationHandle)guardHandle);
        if (spec.Method.Kind != HandleKind.MethodDefinition || !UnsupportedAlignedMethodArgument(reader.GetBlobReader(spec.Signature), 0x1e))
            throw UnsupportedAlignedEdge();
        var guard = ResolveMethodSpec(module, (MethodSpecificationHandle)guardHandle, facade.Context);
        var definition = reader.GetMethodDefinition((MethodDefinitionHandle)spec.Method);
        string guardName = CoreIntrinsics.IsVectorExplicitOwner(vector) ? "ThrowForUnsupportedNumericsVectorBaseType"
            : "ThrowForUnsupportedIntrinsicsVector" + (CoreIntrinsics.FixedVectorValueOwnerWidth(vector) * 8) + "BaseType";
        if (guard.Name != guardName || guard.DeclaringClass.FullName != "System.ThrowHelper"
            || guard.DeclaringClass != FindClassByFullName("System.ThrowHelper")
            || guard.Attributes != (MethodAttributes.Assembly | MethodAttributes.Static | MethodAttributes.HideBySig)
            || guard.ImplAttributes != MethodImplAttributes.AggressiveInlining || guard.Rva == 0
            || guard.Context.TypeArgs.Length != 0 || guard.Context.MethodArgs is not [var element]
            || !CoreIntrinsics.SameUnsupportedAlignedElement(element, vector.Context.TypeArgs[0])
            || !CoreIntrinsics.IsVectorManagedThrowSignature(reader.GetBlobReader(definition.Signature)))
            throw UnsupportedAlignedEdge();
        ValidateMagnitudeResolvedDefinition(guard, module, (MethodDefinitionHandle)spec.Method);
        ValidateAlignedGenericParameter(module, (MethodDefinitionHandle)spec.Method);
        ValidateMagnitudeNoInitializer(guard.DeclaringClass);
        var sink = ValidateGenericVectorThrowGuard(guard, vector, aligned: true);
        int resource = (int)GenericVectorGuardInstructions(guard)[2].Operand;
        if (!module.PE.GetMethodBody(guard.Rva).LocalSignature.IsNil
            || ThrowHelperResources.ResourceKey(module, resource) != "Arg_TypeNotSupported")
            throw UnsupportedAlignedEdge();

        return new UnsupportedAlignedEdges
        {
            Vector = vector, Alignment = ResolveSimdAlignmentBody(slot, vector), Violation = violation,
            Sink = sink, Resource = resource, AlignmentCall = il[2], UnsafeCall = il[last], ConstrainedToken = il[1].Token,
        };
    }

    private MemberReferenceHandle UnsupportedAlignedSlotReference(MethodInfo root, int token, string name, bool alignment, bool store)
    {
        var handle = SRME.EntityHandle(token);
        var reader = root.Module.Reader;
        if (handle.Kind != HandleKind.MemberReference) throw UnsupportedAlignedEdge();
        var reference = reader.GetMemberReference((MemberReferenceHandle)handle);
        if (!reader.StringComparer.Equals(reference.Name, name) || reference.Parent.Kind != HandleKind.TypeSpecification)
            throw UnsupportedAlignedEdge();
        var parent = reader.GetBlobReader(reader.GetTypeSpecification((TypeSpecificationHandle)reference.Parent).Signature);
        if (!ReadSimdAlignmentContractPrefix(root.Module, ref parent, root.DeclaringClass)
            || parent.RemainingBytes != 4 || parent.ReadByte() != 0x13 || parent.ReadByte() != 0
            || parent.ReadByte() != 0x13 || parent.ReadByte() != 1
            || !(alignment ? CoreIntrinsics.IsVectorAlignmentSignature(reader.GetBlobReader(reference.Signature))
                : ReadUnsupportedAlignedSlotSignature(reader, reader.GetBlobReader(reference.Signature), store, offset: false)))
            throw UnsupportedAlignedEdge();
        return (MemberReferenceHandle)handle;
    }

    private static bool ReadUnsupportedAlignedSlotSignature(MetadataReader reader, BlobReader raw, bool store, bool offset)
    {
        if (raw.RemainingBytes < 7 || raw.ReadByte() != 0 || raw.ReadByte() != (store ? 2 : 1) + (offset ? 1 : 0)
            || store && raw.ReadByte() != 1 || raw.ReadByte() != 0x13 || raw.ReadByte() != 0
            || !store && !ReadUnsupportedAlignedInModifier(reader, ref raw)
            || raw.RemainingBytes < 3 || raw.ReadByte() != 0x10 || raw.ReadByte() != 0x13 || raw.ReadByte() != 1)
            return false;
        return offset ? raw.RemainingBytes == 1 && raw.ReadByte() == 0x19 : raw.RemainingBytes == 0;
    }

    private static bool ReadUnsupportedAlignedInModifier(MetadataReader reader, ref BlobReader raw)
    {
        if (raw.RemainingBytes < 2 || raw.ReadByte() != 0x1f || !raw.TryReadCompressedInteger(out int token)
            || token <= 0 || (token & 3) != 0 || (token >> 2) > reader.TypeDefinitions.Count) return false;
        var definition = reader.GetTypeDefinition(SRME.TypeDefinitionHandle(token >> 2));
        return definition.GetDeclaringType().IsNil && definition.GetGenericParameters().Count == 0
            && reader.StringComparer.Equals(definition.Namespace, "System.Runtime.InteropServices")
            && reader.StringComparer.Equals(definition.Name, "InAttribute");
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
            bool offset = ReadUnsupportedAlignedSlotSignature(reader, reader.GetBlobReader(definition.Signature), store, offset: true);
            if (!offset && !ReadUnsupportedAlignedSlotSignature(reader, reader.GetBlobReader(definition.Signature), store, offset: false)
                || !(offset ? offsetTarget.IsNil : target.IsNil)
                || definition.GetGenericParameters().Count != 0 || definition.ImplAttributes != MethodImplAttributes.IL
                || definition.Attributes != (MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.Virtual
                    | MethodAttributes.HideBySig | (offset ? MethodAttributes.Abstract : 0))
                || (definition.RelativeVirtualAddress == 0) != offset) throw UnsupportedAlignedEdge();
            if (offset) offsetTarget = candidate; else target = candidate;
        }
        if (target.IsNil || offsetTarget.IsNil) throw UnsupportedAlignedEdge();
        var slot = ResolveMemberRefMethod(module, reference, root.Context);
        ValidateMagnitudeResolvedDefinition(slot, module, target);
        if (slot.DeclaringClass != root.DeclaringClass || slot.Context.MethodArgs.Length != 0
            || slot.Context.TypeArgs is not [var self, var element] || self.Class != vector
            || !CoreIntrinsics.SameUnsupportedAlignedElement(element, vector.Context.TypeArgs[0])) throw UnsupportedAlignedEdge();
        MethodDefinitionHandle selected = default, offsetBody = default;
        foreach (var implementationHandle in reader.GetTypeDefinition(vector.Handle).GetMethodImplementations())
        {
            var implementation = reader.GetMethodImplementation(implementationHandle);
            if (implementation.MethodDeclaration.IsNil) throw UnsupportedAlignedEdge();
            if (implementation.MethodDeclaration.Kind == HandleKind.MethodDefinition)
            {
                var declared = (MethodDefinitionHandle)implementation.MethodDeclaration;
                if (declared == target || declared == offsetTarget
                    || reader.StringComparer.Equals(reader.GetMethodDefinition(declared).Name, name)) throw UnsupportedAlignedEdge();
                continue;
            }
            if (implementation.MethodDeclaration.Kind != HandleKind.MemberReference) throw UnsupportedAlignedEdge();
            var declaration = reader.GetMemberReference((MemberReferenceHandle)implementation.MethodDeclaration);
            if (!reader.StringComparer.Equals(declaration.Name, name)) continue;
            bool offset = ReadUnsupportedAlignedSlotSignature(reader, reader.GetBlobReader(declaration.Signature), store, offset: true);
            if (!offset && !ReadUnsupportedAlignedSlotSignature(reader, reader.GetBlobReader(declaration.Signature), store, offset: false)
                || !(offset ? offsetBody.IsNil : selected.IsNil) || implementation.MethodBody.Kind != HandleKind.MethodDefinition
                || !ReadSimdAlignmentImplementationType(module, declaration.Parent, root.DeclaringClass, vector))
                throw UnsupportedAlignedEdge();
            var body = (MethodDefinitionHandle)implementation.MethodBody;
            ValidateUnsupportedAlignedBridgeDefinition(vector, body, store, offset);
            if (offset) offsetBody = body; else selected = body;
        }
        if (selected.IsNil || offsetBody.IsNil || selected == offsetBody) throw UnsupportedAlignedEdge();
        return AlignedProofMethod(vector, selected, vector.Context);
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
            throw UnsupportedAlignedEdge();
    }

    private static bool ReadUnsupportedAlignedConcreteSignature(ClassInfo vector, BlobReader raw, bool store, bool offset, bool generic)
    {
        if (raw.RemainingBytes < 3 || raw.ReadByte() != (generic ? 0x10 : 0) || generic && raw.ReadByte() != 1
            || raw.ReadByte() != (store ? 2 : 1) + (offset ? 1 : 0) || store && raw.ReadByte() != 1
            || !ReadFixedGenericVectorEstimateType(ref raw, vector.Handle, generic ? 0x1e : 0x13)
            || !store && !generic && !ReadUnsupportedAlignedInModifier(vector.Module.Reader, ref raw)
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
            throw UnsupportedAlignedEdge();
        var handle = SRME.EntityHandle(il[call].Token);
        if (handle.Kind != HandleKind.MethodSpecification) throw UnsupportedAlignedEdge();
        var spec = reader.GetMethodSpecification((MethodSpecificationHandle)handle);
        if (spec.Method.Kind != HandleKind.MethodDefinition || !UnsupportedAlignedMethodArgument(reader.GetBlobReader(spec.Signature), 0x13))
            throw UnsupportedAlignedEdge();
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
            throw UnsupportedAlignedEdge();
        ValidateAlignedGenericParameter(module, definitionHandle);
        var facade = GetClass(module, definition.GetDeclaringType());
        if (!CoreIntrinsics.IsVectorEstimateOwner(facade)) throw UnsupportedAlignedEdge();
        ValidateMagnitudeNoInitializer(facade);
        return AlignedProofMethod(facade, definitionHandle,
            new GenericContext(Array.Empty<TypeDesc>(), new[] { vector.Context.TypeArgs[0] }));
    }

    private static void ValidateAlignedGenericParameter(Module module, MethodDefinitionHandle method)
    {
        var parameters = module.Reader.GetMethodDefinition(method).GetGenericParameters();
        if (parameters.Count != 1) throw UnsupportedAlignedEdge();
        foreach (var handle in parameters)
        {
            var parameter = module.Reader.GetGenericParameter(handle);
            if (parameter.Parent != method || parameter.Index != 0 || parameter.Attributes != GenericParameterAttributes.None
                || parameter.GetConstraints().Count != 0) throw UnsupportedAlignedEdge();
        }
    }

    // Proof-only methods never enter reachability or the ordinary member caches.
    private static MethodInfo AlignedProofMethod(ClassInfo owner, MethodDefinitionHandle handle, GenericContext context)
    {
        var reader = owner.Module.Reader;
        var definition = reader.GetMethodDefinition(handle);
        return new MethodInfo
        {
            DeclaringClass = owner, Module = owner.Module, Handle = handle, Name = reader.GetString(definition.Name),
            Attributes = definition.Attributes, ImplAttributes = definition.ImplAttributes,
            Rva = definition.RelativeVirtualAddress, Context = context,
        };
    }

    private static bool UnsupportedAlignedMethodArgument(BlobReader raw, int variable) =>
        raw.RemainingBytes == 4 && raw.ReadByte() == 0x0a && raw.ReadByte() == 1
        && raw.ReadByte() == variable && raw.ReadByte() == 0;

    internal static NotSupportedException UnsupportedAlignedEdge() =>
        new("Unsupported aligned SIMD defaults require their exact CoreLib alignment and unsafe calls and terminal element guard");
}

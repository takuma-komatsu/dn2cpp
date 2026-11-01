using System.Reflection.Metadata;
using SRME = System.Reflection.Metadata.Ecma335.MetadataTokens;

namespace Dn2Cpp;

internal sealed partial class Compilation
{
    internal MethodInfo? TryResolveVectorMagnitudeSpec(Module module, MethodSpecificationHandle handle, GenericContext context) =>
        ResolveVectorMagnitudeSpecification(module, handle, context, allowCanonicalForScan: false);

    private MethodInfo? TryResolveVectorMagnitudeSpecForScan(Module module, MethodSpecificationHandle handle, GenericContext context) =>
        ResolveVectorMagnitudeSpecification(module, handle, context, allowCanonicalForScan: true);

    private MethodInfo? ResolveVectorMagnitudeSpecification(Module module, MethodSpecificationHandle handle, GenericContext context,
        bool allowCanonicalForScan)
    {
        var reader = module.Reader;
        var specification = reader.GetMethodSpecification(handle);
        string? operation = MethodSpecMethodName(module, specification);
        if (operation is null || !CoreIntrinsics.IsVectorMagnitudeName(operation)) return null;
        var parent = specification.Method.Kind switch
        {
            HandleKind.MethodDefinition => reader.GetMethodDefinition((MethodDefinitionHandle)specification.Method).GetDeclaringType(),
            HandleKind.MemberReference => reader.GetMemberReference((MemberReferenceHandle)specification.Method).Parent,
            _ => default(EntityHandle),
        };
        if (parent.IsNil) return null;
        if (parent.Kind != HandleKind.TypeSpecification
            && (MethodSpecParentTypeName(module, specification) is not { } name || !CoreIntrinsics.IsVectorEstimateOwnerName(name))) return null;
        var type = ResolveTypeTokenForScan(module, parent, context);
        while (type?.Element is { } element) type = element;
        if (type?.Class is not { } owner || !CoreIntrinsics.IsVectorEstimateOwnerName(owner.FullName)) return null;
        if (parent.Kind is not (HandleKind.TypeDefinition or HandleKind.TypeReference)
            || !CoreIntrinsics.IsVectorEstimateOwner(owner))
            throw VectorMagnitudeUnsupported();

        MethodDefinitionHandle definitionHandle = default;
        if (specification.Method.Kind == HandleKind.MethodDefinition)
        {
            definitionHandle = (MethodDefinitionHandle)specification.Method;
            if (!CoreIntrinsics.IsVectorMagnitudeDefinition(owner, module, definitionHandle)) throw VectorMagnitudeUnsupported();
        }
        else
        {
            var reference = reader.GetMemberReference((MemberReferenceHandle)specification.Method);
            if (!CoreIntrinsics.ReadVectorMagnitudeSignature(reader.GetBlobReader(reference.Signature), out int token))
                throw VectorMagnitudeUnsupported();
            EntityHandle vectorHandle = (token & 3) == 0
                ? SRME.TypeDefinitionHandle(token >> 2) : SRME.TypeReferenceHandle(token >> 2);
            var identity = TypeDefinitionOf(module, vectorHandle);
            foreach (var candidate in owner.Module.Reader.GetTypeDefinition(owner.Handle).GetMethods())
            {
                if (!CoreIntrinsics.IsVectorMagnitudeDefinition(owner, owner.Module, candidate)
                    || !owner.Module.Reader.StringComparer.Equals(owner.Module.Reader.GetMethodDefinition(candidate).Name, operation)) continue;
                var definition = owner.Module.Reader.GetMethodDefinition(candidate);
                if (!CoreIntrinsics.ReadVectorMagnitudeSignature(owner.Module.Reader.GetBlobReader(definition.Signature), out int original)
                    || identity is not { } target || target.Module != owner.Module
                    || target.Handle != SRME.TypeDefinitionHandle(original >> 2)) continue;
                if (!definitionHandle.IsNil) throw VectorMagnitudeUnsupported();
                definitionHandle = candidate;
            }
            if (definitionHandle.IsNil) throw VectorMagnitudeUnsupported();
        }
        var raw = reader.GetBlobReader(specification.Signature);
        if (raw.RemainingBytes < 3 || raw.ReadByte() != 0x0a || raw.ReadByte() != 1) throw VectorMagnitudeUnsupported();
        // Check the unnormalized argument before the decoder or instantiation cache.
        var shape = raw;
        int code = shape.ReadByte();
        if (code is 0x0c or 0x0d)
        {
            if (shape.RemainingBytes != 0) throw VectorMagnitudeUnsupported();
        }
        else if (!shape.TryReadCompressedInteger(out int index) || shape.RemainingBytes != 0
            || (code == 0x11 ? index >> 2 == 0 || (index & 3) > 1
                : code == 0x13 ? index < 0 || index >= context.TypeArgs.Length
                : code == 0x1e ? index < 0 || index >= context.MethodArgs.Length : true))
            throw VectorMagnitudeUnsupported();
        var arguments = specification.DecodeSignature(SigProvider, context);
        if (arguments is not [var argument]) throw VectorMagnitudeUnsupported();
        if (argument.IsCanonPlaceholder)
        {
            if (!allowCanonicalForScan || owner.FullName != "System.Numerics.Vector"
                || argument.Kind != TypeKind.Primitive || argument.CanonAnyIndex != -1
                || !CoreIntrinsics.IsVectorNumericElement(TypeDesc.MakePrimitive(argument.Primitive))
                || !ReadVectorAsCanonicalArgument(ref raw, context, argument) || raw.RemainingBytes != 0)
                throw VectorMagnitudeUnsupported();
            // Width placeholders cannot choose between a supported scalar and an enum.
            // Existing call/address taint forces the concrete body before emission.
            return null;
        }
        if (argument.Class is { } value) EnsureShape(value);
        if (!(CoreIntrinsics.IsVectorMagnitudeElement(argument)
                || owner.FullName == "System.Numerics.Vector" && CoreIntrinsics.IsVectorManagedValueElement(argument))
            || !ReadVectorValueElement(module, ref raw, context, argument) || raw.RemainingBytes != 0)
            throw VectorMagnitudeUnsupported();
        return InstantiateMethodOnClass(owner, owner.Module, definitionHandle, new[] { argument });
    }

    private void ValidateVectorMagnitudeInstantiation(ClassInfo owner, Module module, MethodDefinitionHandle handle, TypeDesc[] arguments)
    {
        var definition = module.Reader.GetMethodDefinition(handle);
        if (!CoreIntrinsics.IsVectorMagnitudeName(module.Reader.GetString(definition.Name))
            || !CoreIntrinsics.IsVectorEstimateOwnerName(owner.FullName)) return;
        if (arguments is not [var argument] || !CoreIntrinsics.IsVectorMagnitudeDefinition(owner, module, handle))
            throw VectorMagnitudeUnsupported();
        if (argument.Class is { } value) EnsureShape(value);
        var candidate = new MethodInfo
        {
            DeclaringClass = owner, Module = module, Handle = handle, Name = module.Reader.GetString(definition.Name),
            Context = new GenericContext(owner.Context.TypeArgs, arguments), Attributes = definition.Attributes,
            ImplAttributes = definition.ImplAttributes, Rva = definition.RelativeVirtualAddress, PInvoke = ReadPInvoke(module, definition),
        };
        if (candidate.Signature.ReturnType.Class is { } vector) EnsureShape(vector);
        if (CoreIntrinsics.IsVectorMagnitudeManagedGuard(candidate)) ResolveVectorMagnitudeManagedGuard(candidate);
        else if (!CoreIntrinsics.IsVectorMagnitude(candidate)) throw VectorMagnitudeUnsupported();
    }

    internal void RejectUninstantiatedVectorMagnitudeReference(Module module, MemberReferenceHandle handle, GenericContext context)
    {
        var reference = module.Reader.GetMemberReference(handle);
        if (!CoreIntrinsics.IsVectorMagnitudeName(module.Reader.GetString(reference.Name))) return;
        if (MemberRefParentTypeName(module, handle) is { } name && CoreIntrinsics.IsVectorEstimateOwnerName(name))
            throw VectorMagnitudeUnsupported();
        if (reference.Parent.Kind != HandleKind.TypeSpecification) return;
        var type = module.Reader.GetTypeSpecification((TypeSpecificationHandle)reference.Parent).DecodeSignature(SigProvider, context);
        while (type.Element is { } element) type = element;
        if (type.Class is { } owner && CoreIntrinsics.IsVectorEstimateOwnerName(owner.FullName))
            throw VectorMagnitudeUnsupported();
    }

    private static NotSupportedException VectorMagnitudeUnsupported() =>
        new("Vector magnitude selection requires an exact floating-point generic CoreLib declaration and method specification");
}

using System.Reflection.Metadata;
using SRME = System.Reflection.Metadata.Ecma335.MetadataTokens;

namespace Dn2Cpp;

internal sealed partial class Compilation
{
    internal MethodInfo? TryResolveGenericVectorEstimateSpec(Module module, MethodSpecificationHandle handle, GenericContext context)
    {
        var reader = module.Reader;
        var specification = reader.GetMethodSpecification(handle);
        if (MethodSpecMethodName(module, specification) != "MultiplyAddEstimate") return null;
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
            || owner.FullName != "System.Numerics.Vector" || !CoreIntrinsics.IsVectorEstimateOwner(owner))
            throw GenericVectorEstimateUnsupported();

        MethodDefinitionHandle definitionHandle = default;
        if (specification.Method.Kind == HandleKind.MethodDefinition)
        {
            definitionHandle = (MethodDefinitionHandle)specification.Method;
            if (!CoreIntrinsics.IsGenericVectorEstimateDefinition(owner, module, definitionHandle)) throw GenericVectorEstimateUnsupported();
        }
        else
        {
            var reference = reader.GetMemberReference((MemberReferenceHandle)specification.Method);
            if (!CoreIntrinsics.ReadGenericVectorEstimateSignature(reader.GetBlobReader(reference.Signature), out int token))
                throw GenericVectorEstimateUnsupported();
            EntityHandle vectorHandle = (token & 3) == 0
                ? SRME.TypeDefinitionHandle(token >> 2) : SRME.TypeReferenceHandle(token >> 2);
            var identity = TypeDefinitionOf(module, vectorHandle);
            foreach (var candidate in owner.Module.Reader.GetTypeDefinition(owner.Handle).GetMethods())
            {
                if (!CoreIntrinsics.IsGenericVectorEstimateDefinition(owner, owner.Module, candidate)) continue;
                var definition = owner.Module.Reader.GetMethodDefinition(candidate);
                if (!CoreIntrinsics.ReadGenericVectorEstimateSignature(owner.Module.Reader.GetBlobReader(definition.Signature), out int original)
                    || identity is not { } target || target.Module != owner.Module
                    || target.Handle != SRME.TypeDefinitionHandle(original >> 2)) continue;
                if (!definitionHandle.IsNil) throw GenericVectorEstimateUnsupported();
                definitionHandle = candidate;
            }
            if (definitionHandle.IsNil) throw GenericVectorEstimateUnsupported();
        }
        var raw = reader.GetBlobReader(specification.Signature);
        if (raw.RemainingBytes < 3 || raw.ReadByte() != 0x0a || raw.ReadByte() != 1) throw GenericVectorEstimateUnsupported();
        // Reject wrappers and trailing bytes before decoding can normalize the argument.
        var shape = raw;
        int code = shape.ReadByte();
        if (!shape.TryReadCompressedInteger(out int argumentToken) || shape.RemainingBytes != 0
            || (code == 0x11 ? argumentToken >> 2 == 0 || (argumentToken & 3) > 1
                : code == 0x13 ? argumentToken < 0 || argumentToken >= context.TypeArgs.Length
                : code == 0x1e ? argumentToken < 0 || argumentToken >= context.MethodArgs.Length : true))
            throw GenericVectorEstimateUnsupported();
        var arguments = specification.DecodeSignature(SigProvider, context);
        if (arguments is not [var argument]) throw GenericVectorEstimateUnsupported();
        if (argument.Class is { } value) EnsureShape(value);
        if (!CoreIntrinsics.IsVectorManagedValueElement(argument)
            || !ReadVectorValueElement(module, ref raw, context, argument) || raw.RemainingBytes != 0)
            throw GenericVectorEstimateUnsupported();
        return InstantiateMethodOnClass(owner, owner.Module, definitionHandle, new[] { argument });
    }

    private void ValidateGenericVectorEstimateInstantiation(ClassInfo owner, Module module, MethodDefinitionHandle handle, TypeDesc[] arguments)
    {
        var definition = module.Reader.GetMethodDefinition(handle);
        if (!module.Reader.StringComparer.Equals(definition.Name, "MultiplyAddEstimate")
            || !CoreIntrinsics.IsVectorEstimateOwnerName(owner.FullName)) return;
        if (arguments is not [var argument] || !CoreIntrinsics.IsGenericVectorEstimateDefinition(owner, module, handle))
            throw GenericVectorEstimateUnsupported();
        if (argument.Class is { } value) EnsureShape(value);
        var candidate = new MethodInfo
        {
            DeclaringClass = owner, Module = module, Handle = handle, Name = "MultiplyAddEstimate",
            Context = new GenericContext(owner.Context.TypeArgs, arguments), Attributes = definition.Attributes,
            ImplAttributes = definition.ImplAttributes, Rva = definition.RelativeVirtualAddress, PInvoke = ReadPInvoke(module, definition),
        };
        if (candidate.Signature.ReturnType.Class is { } vector) EnsureShape(vector);
        if (!CoreIntrinsics.IsGenericVectorEstimate(candidate)) throw GenericVectorEstimateUnsupported();
        ResolveGenericVectorEstimateGuard(candidate);
    }

    private static NotSupportedException GenericVectorEstimateUnsupported() =>
        new("Vector MultiplyAddEstimate does not support method specifications without an exact unsupported managed-value generic declaration");
}

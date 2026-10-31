using System.Reflection.Metadata;

namespace Dn2Cpp;

internal sealed partial class Compilation
{
    private readonly Dictionary<(ClassInfo Owner, MethodDefinitionHandle Definition), MethodInfo> _vectorEstimates = new();

    internal void RejectVectorEstimateMethodSpecification(Module module, MethodSpecificationHandle handle, GenericContext context)
    {
        var specification = module.Reader.GetMethodSpecification(handle);
        if (MethodSpecMethodName(module, specification) != "MultiplyAddEstimate") return;
        var parent = specification.Method.Kind switch
        {
            HandleKind.MethodDefinition => module.Reader.GetMethodDefinition((MethodDefinitionHandle)specification.Method).GetDeclaringType(),
            HandleKind.MemberReference => module.Reader.GetMemberReference((MemberReferenceHandle)specification.Method).Parent,
            _ => default(EntityHandle),
        };
        if (parent.IsNil) return;
        if (parent.Kind != HandleKind.TypeSpecification
            && (MethodSpecParentTypeName(module, specification) is not { } name || !CoreIntrinsics.IsVectorEstimateOwnerName(name))) return;
        // Context and wrappers discover candidates only; no MethodSpec is admitted.
        var type = ResolveTypeTokenForScan(module, parent, context);
        while (type?.Element is { } element) type = element;
        if (type?.Class is { } owner && owner.Module.AssemblyName == "System.Private.CoreLib"
            && CoreIntrinsics.IsVectorEstimateOwnerName(owner.FullName))
            throw new NotSupportedException("Vector MultiplyAddEstimate does not support method specifications");
    }

    internal MethodInfo? TryResolveVectorEstimateReference(Module module, MemberReferenceHandle handle, GenericContext context)
    {
        var reader = module.Reader;
        var reference = reader.GetMemberReference(handle);
        if (!reader.StringComparer.Equals(reference.Name, "MultiplyAddEstimate")) return null;
        if (reference.Parent.Kind == HandleKind.TypeSpecification)
        {
            var type = reader.GetTypeSpecification((TypeSpecificationHandle)reference.Parent).DecodeSignature(SigProvider, context);
            while (type.Element is { } element) type = element;
            if (type.Class is { } candidate && CoreIntrinsics.IsVectorEstimateOwnerName(candidate.FullName))
                throw new NotSupportedException("Vector MultiplyAddEstimate requires an unmodified declaring type");
            return null;
        }
        string? name = MemberRefParentTypeName(module, handle);
        if (name is null || !CoreIntrinsics.IsVectorEstimateOwnerName(name)) return null;
        var signature = reference.DecodeMethodSignature(SigProvider, GenericContext.Empty);
        // Generic references must not fall through to the address resolver,
        // which can otherwise select a concrete overload after substitution.
        ClassInfo? owner = reference.Parent.Kind == HandleKind.TypeReference
            ? ResolveTypeRef(module, (TypeReferenceHandle)reference.Parent)?.Class
            : reference.Parent.Kind == HandleKind.TypeDefinition
                ? ResolveTypeDefMemberRefClass(module, (TypeDefinitionHandle)reference.Parent) : null;
        if (owner is null || !CoreIntrinsics.IsVectorEstimateOwner(owner)
            || !CoreIntrinsics.IsVectorEstimateRawSignature(reader.GetBlobReader(reference.Signature)))
            throw new NotSupportedException("Vector MultiplyAddEstimate requires its exact floating-point CoreLib reference");
        // Direct MemberRef decoding does not drain freshly minted vector shapes.
        if (signature.ReturnType.Class is { } vector) EnsureShape(vector);
        if (!CoreIntrinsics.IsVectorEstimateSignature(signature, owner))
            throw new NotSupportedException("Vector MultiplyAddEstimate requires its exact floating-point CoreLib reference");
        foreach (var definitionHandle in owner.Module.Reader.GetTypeDefinition(owner.Handle).GetMethods())
        {
            var definition = owner.Module.Reader.GetMethodDefinition(definitionHandle);
            if (!owner.Module.Reader.StringComparer.Equals(definition.Name, "MultiplyAddEstimate")
                || definition.GetGenericParameters().Count != 0) continue;
            var key = (owner, definitionHandle);
            if (!_vectorEstimates.TryGetValue(key, out var method))
            {
                method = new MethodInfo
                {
                    DeclaringClass = owner, Name = "MultiplyAddEstimate", Handle = definitionHandle, Module = owner.Module,
                    Context = owner.Context, Attributes = definition.Attributes, ImplAttributes = definition.ImplAttributes,
                    Rva = definition.RelativeVirtualAddress, PInvoke = ReadPInvoke(owner.Module, definition),
                };
                _vectorEstimates.Add(key, method);
            }
            if (CoreIntrinsics.IsVectorMultiplyAddEstimate(method)
                && SameTypeArg(signature.ReturnType, method.Signature.ReturnType)) return method;
        }
        throw new NotSupportedException("Vector MultiplyAddEstimate requires its exact floating-point CoreLib declaration");
    }
}

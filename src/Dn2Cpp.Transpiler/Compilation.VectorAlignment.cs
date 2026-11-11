using System.Reflection.Metadata;

namespace Dn2Cpp;

internal sealed partial class Compilation
{
    internal MethodInfo? TryResolveVectorAlignmentReference(Module module, MemberReferenceHandle handle, GenericContext context)
    {
        var reader = module.Reader;
        var reference = reader.GetMemberReference(handle);
        if (reader.StringComparer.Equals(reference.Name, "ThrowAccessViolationException")
            && reference.Parent.Kind == HandleKind.TypeSpecification)
        {
            var type = reader.GetTypeSpecification((TypeSpecificationHandle)reference.Parent).DecodeSignature(SigProvider, context);
            while (type.Element is { } element) type = element;
            if (type.Class is { } helper && (helper.FullName == "System.ThrowHelper"
                || GenericDefFullName(helper) == "System.ThrowHelper")) throw SimdAlignmentUnsupported();
        }
        if (reader.StringComparer.Equals(reference.Name, "ThrowAccessViolationException")
            && MemberRefParentTypeName(module, handle) == "System.ThrowHelper")
        {
            if (!CoreIntrinsics.IsVectorAccessViolationSignature(reader.GetBlobReader(reference.Signature))
                || reference.Parent.Kind is not (HandleKind.TypeReference or HandleKind.TypeDefinition)) throw SimdAlignmentUnsupported();
            ClassInfo? helperOwner = reference.Parent.Kind == HandleKind.TypeReference
                ? ResolveTypeRef(module, (TypeReferenceHandle)reference.Parent)?.Class
                : ResolveTypeDefMemberRefClass(module, (TypeDefinitionHandle)reference.Parent);
            if (helperOwner is not null && helperOwner.MethodsNamed("ThrowAccessViolationException") is { } helpers)
                foreach (var helper in helpers)
                    if (CoreIntrinsics.IsVectorAccessViolationHelper(helper))
                    {
                        ValidateSimdAlignmentBody(helper);
                        return helper;
                    }
            throw SimdAlignmentUnsupported();
        }
        if (!reader.StringComparer.Equals(reference.Name, "get_Alignment")) return null;
        if (reference.Parent.Kind == HandleKind.TypeSpecification)
        {
            var type = reader.GetTypeSpecification((TypeSpecificationHandle)reference.Parent).DecodeSignature(SigProvider, context);
            while (type.Element is { } element) type = element;
            if (type.Class?.FullName == "System.Numerics.Vector")
                throw new NotSupportedException("Vector.get_Alignment requires an unmodified declaring type");
            return null;
        }
        if (MemberRefParentTypeName(module, handle) != "System.Numerics.Vector") return null;
        // Validate before the general MemberRef caches: neither a warmed cache nor
        // caller-context substitution may turn another signature into this getter.
        if (!CoreIntrinsics.IsVectorAlignmentSignature(reader.GetBlobReader(reference.Signature)))
            throw new NotSupportedException("Vector.get_Alignment requires its exact CoreLib reference");
        ClassInfo? owner = reference.Parent.Kind == HandleKind.TypeReference
            ? ResolveTypeRef(module, (TypeReferenceHandle)reference.Parent)?.Class
            : reference.Parent.Kind == HandleKind.TypeDefinition
                ? ResolveTypeDefMemberRefClass(module, (TypeDefinitionHandle)reference.Parent) : null;
        if (owner is not null && CoreIntrinsics.IsVectorEstimateOwner(owner))
        {
            EnsureShape(owner);
            if (owner.MethodsNamed("get_Alignment") is { } methods)
                foreach (var method in methods)
                    if (CoreIntrinsics.IsVectorAlignment(method)) return method;
        }
        throw new NotSupportedException("Vector.get_Alignment requires its exact CoreLib declaration");
    }

    internal void RejectVectorAlignmentMethodSpecification(Module module, MethodSpecificationHandle handle, GenericContext context)
    {
        var specification = module.Reader.GetMethodSpecification(handle);
        if (MethodSpecMethodName(module, specification) is not ("get_Alignment" or "ThrowAccessViolationException")) return;
        if (specification.Method.Kind == HandleKind.MemberReference)
        {
            if (TryResolveVectorAlignmentReference(module, (MemberReferenceHandle)specification.Method, context) is not null)
                throw new NotSupportedException("Vector alignment dependencies are not generic methods");
        }
        else if (specification.Method.Kind == HandleKind.MethodDefinition)
        {
            var definition = module.Reader.GetMethodDefinition((MethodDefinitionHandle)specification.Method);
            var owner = module.Reader.GetTypeDefinition(definition.GetDeclaringType());
            if (module.Reader.StringComparer.Equals(owner.Namespace, "System.Numerics")
                && module.Reader.StringComparer.Equals(owner.Name, "Vector"))
                throw new NotSupportedException("Vector.get_Alignment is not a generic method");
            if (module.Reader.StringComparer.Equals(owner.Namespace, "System")
                && module.Reader.StringComparer.Equals(owner.Name, "ThrowHelper")) throw SimdAlignmentUnsupported();
        }
    }
}

using System.Reflection.Metadata;

namespace Dn2Cpp;

internal sealed partial class Compilation
{
    internal MethodInfo? TryResolveScalarNumericLeafReference(Module module, MemberReferenceHandle handle, GenericContext context)
    {
        var reader = module.Reader;
        var reference = reader.GetMemberReference(handle);
        string name = reader.GetString(reference.Name);
        if (!ScalarNumericLeafContract.IsName(name)) return null;
        if (reference.Parent.Kind == HandleKind.TypeSpecification)
        {
            var type = reader.GetTypeSpecification((TypeSpecificationHandle)reference.Parent).DecodeSignature(SigProvider, context);
            while (type.Element is { } element) type = element;
            string? ownerName = type.Class?.FullName;
            if (type.Kind == TypeKind.Primitive)
                ownerName = type.Primitive switch { PrimitiveTypeCode.Int32 => "System.Int32", PrimitiveTypeCode.Single => "System.Single", PrimitiveTypeCode.Double => "System.Double", _ => null };
            if (ScalarNumericLeafContract.IsCandidate(ownerName, name))
                throw new NotSupportedException("Scalar numeric leaf requires an unmodified declaring type");
            return null;
        }
        string? parent = MemberRefParentTypeName(module, handle);
        if (!ScalarNumericLeafContract.IsCandidate(parent, name)) return null;
        // Raw authority precedes every general MemberRef cache and substitution.
        if (!ScalarNumericLeafContract.IsSignature(parent!, name, reader.GetBlobReader(reference.Signature)))
            throw new NotSupportedException("Scalar numeric leaf requires its exact CoreLib reference");
        ClassInfo? owner = reference.Parent.Kind == HandleKind.TypeReference
            ? ResolveTypeRef(module, (TypeReferenceHandle)reference.Parent)?.Class
            : reference.Parent.Kind == HandleKind.TypeDefinition
                ? ResolveTypeDefMemberRefClass(module, (TypeDefinitionHandle)reference.Parent) : null;
        if (owner is not null && CoreIntrinsics.IsScalarNumericLeafOwner(owner))
        {
            EnsureShape(owner);
            if (owner.MethodsNamed(name) is { } methods)
                foreach (var method in methods)
                    if (CoreIntrinsics.IsScalarNumericLeaf(method)) return method;
        }
        throw new NotSupportedException("Scalar numeric leaf requires its exact CoreLib declaration");
    }

    internal void RejectScalarNumericLeafMethodSpecification(Module module, MethodSpecificationHandle handle, GenericContext context)
    {
        var specification = module.Reader.GetMethodSpecification(handle);
        string name = MethodSpecMethodName(module, specification);
        if (!ScalarNumericLeafContract.IsName(name)) return;
        if (specification.Method.Kind == HandleKind.MemberReference)
        {
            if (TryResolveScalarNumericLeafReference(module, (MemberReferenceHandle)specification.Method, context) is not null)
                throw new NotSupportedException("Scalar numeric leaf is not a generic method");
        }
        else if (specification.Method.Kind == HandleKind.MethodDefinition)
        {
            var definition = module.Reader.GetMethodDefinition((MethodDefinitionHandle)specification.Method);
            if (ScalarNumericLeafContract.IsCandidate(TypeFullName(module.Reader, definition.GetDeclaringType()), name))
                throw new NotSupportedException("Scalar numeric leaf is not a generic method");
        }
    }
}

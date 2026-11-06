using System.Reflection.Metadata;

namespace Dn2Cpp;

internal sealed partial class Compilation
{
    internal MethodInfo? TryResolveConcreteVectorReductionReference(Module module, MemberReferenceHandle handle, GenericContext context)
    {
        var reader = module.Reader;
        var reference = reader.GetMemberReference(handle);
        string name = reader.GetString(reference.Name);
        if (!CoreIntrinsics.IsConcreteVectorReductionName(name)) return null;
        if (reference.Parent.Kind == HandleKind.TypeSpecification)
        {
            var type = reader.GetTypeSpecification((TypeSpecificationHandle)reference.Parent).DecodeSignature(SigProvider, context);
            while (type.Element is { } element) type = element;
            if (type.Class?.FullName == "System.Runtime.Intrinsics.Vector128"
                && CoreIntrinsics.IsConcreteVectorReductionCandidate(reference.DecodeMethodSignature(SigProvider, context)))
                throw ConcreteVectorReductionUnsupported();
            return null;
        }
        if (MemberRefParentTypeName(module, handle) != "System.Runtime.Intrinsics.Vector128") return null;
        var signature = reference.DecodeMethodSignature(SigProvider, GenericContext.Empty);
        if (!CoreIntrinsics.IsConcreteVectorReductionCandidate(signature))
        {
            if (CoreIntrinsics.IsConcreteVectorReductionCandidate(reference.DecodeMethodSignature(SigProvider, context)))
                throw ConcreteVectorReductionUnsupported();
            return null;
        }
        // Raw validation precedes all general caches and never uses caller substitutions.
        if (!CoreIntrinsics.IsConcreteVectorReductionRawSignature(name, reader.GetBlobReader(reference.Signature)))
            throw ConcreteVectorReductionUnsupported();
        ClassInfo? owner = reference.Parent.Kind == HandleKind.TypeReference
            ? ResolveTypeRef(module, (TypeReferenceHandle)reference.Parent)?.Class
            : reference.Parent.Kind == HandleKind.TypeDefinition
                ? ResolveTypeDefMemberRefClass(module, (TypeDefinitionHandle)reference.Parent) : null;
        if (owner is null || !CoreIntrinsics.IsVectorEstimateOwner(owner)) throw ConcreteVectorReductionUnsupported();
        if (signature.ParameterTypes[0].Class is { } operand) EnsureShape(operand);
        if (!CoreIntrinsics.IsConcreteVectorReductionSignature(name, signature, owner.Module))
            throw ConcreteVectorReductionUnsupported();
        EnsureShape(owner);
        if (owner.MethodsNamed(name) is { } methods)
            foreach (var method in methods)
                if (CoreIntrinsics.IsConcreteVectorReduction(method)
                    && SameTypeArg(signature.ParameterTypes[0], method.Signature.ParameterTypes[0])) return method;
        throw ConcreteVectorReductionUnsupported();
    }

    internal void RejectConcreteVectorReductionMethodSpecification(Module module, MethodSpecificationHandle handle, GenericContext context)
    {
        var specification = module.Reader.GetMethodSpecification(handle);
        string? name = MethodSpecMethodName(module, specification);
        if (name is null || !CoreIntrinsics.IsConcreteVectorReductionName(name)) return;
        if (specification.Method.Kind == HandleKind.MemberReference)
        {
            if (TryResolveConcreteVectorReductionReference(module, (MemberReferenceHandle)specification.Method, context) is not null)
                throw ConcreteVectorReductionUnsupported();
        }
        else if (specification.Method.Kind == HandleKind.MethodDefinition)
        {
            var definition = module.Reader.GetMethodDefinition((MethodDefinitionHandle)specification.Method);
            var owner = module.Reader.GetTypeDefinition(definition.GetDeclaringType());
            if (module.Reader.StringComparer.Equals(owner.Namespace, "System.Runtime.Intrinsics")
                && module.Reader.StringComparer.Equals(owner.Name, "Vector128")
                && CoreIntrinsics.IsConcreteVectorReductionCandidate(definition.DecodeSignature(SigProvider, context)))
                throw ConcreteVectorReductionUnsupported();
        }
    }

    private static NotSupportedException ConcreteVectorReductionUnsupported() =>
        new("Concrete vector reduction requires an exact nongeneric CoreLib declaration and unmodified reference");
}

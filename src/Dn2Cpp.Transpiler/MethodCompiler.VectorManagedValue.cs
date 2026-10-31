using System.Reflection.Metadata;

namespace Dn2Cpp;

internal sealed partial class MethodCompiler
{
    // A width placeholder can denote both an enum and a supported integer. Their
    // Numerics vectors do not share storage or support/exception semantics.
    private void TaintCanonicalVectorOwner(ClassInfo owner)
    {
        if (CoreIntrinsics.IsVectorExplicitOwner(owner))
            TaintIfCanonical(owner, "numerics-vector-element");
    }

    private static bool IsNumericsVectorFacade(ClassInfo owner)
    {
        if (owner.Handle.IsNil || owner.Module.AssemblyName != "System.Private.CoreLib") return false;
        var reader = owner.Module.Reader;
        var definition = reader.GetTypeDefinition(owner.Handle);
        return definition.GetDeclaringType().IsNil && definition.GetGenericParameters().Count == 0
            && reader.StringComparer.Equals(definition.Namespace, "System.Numerics")
            && reader.StringComparer.Equals(definition.Name, "Vector");
    }

    private void TaintCanonicalVectorOperation(EntityHandle handle)
    {
        if (!SharedTrial) return;
        MethodSpecification? specification = null;
        if (handle.Kind == HandleKind.MethodSpecification)
        {
            specification = _reader.GetMethodSpecification((MethodSpecificationHandle)handle);
            handle = specification.Value.Method;
        }
        EntityHandle parent;
        if (handle.Kind == HandleKind.MemberReference)
            parent = _reader.GetMemberReference((MemberReferenceHandle)handle).Parent;
        else if (handle.Kind == HandleKind.MethodDefinition)
            parent = _reader.GetMethodDefinition((MethodDefinitionHandle)handle).GetDeclaringType();
        else return;
        bool contextualOwner = false;
        if (parent.Kind == HandleKind.TypeSpecification)
        {
            var raw = _reader.GetBlobReader(_reader.GetTypeSpecification((TypeSpecificationHandle)parent).Signature);
            contextualOwner = raw.RemainingBytes > 0 && raw.ReadByte() is 0x13 or 0x1e;
        }
        if (!contextualOwner && !Compilation.VectorValueNamedOwner(_module, parent, includeFacade: true)) return;
        var type = parent.Kind switch
        {
            HandleKind.TypeSpecification => _reader.GetTypeSpecification((TypeSpecificationHandle)parent)
                .DecodeSignature(_c.SigProvider, _method.Context),
            HandleKind.TypeReference => _c.ResolveTypeRef(_module, (TypeReferenceHandle)parent),
            HandleKind.TypeDefinition => _method.DeclaringClass.Module == _module && _method.DeclaringClass.Handle == parent
                ? TypeDesc.MakeClass(_method.DeclaringClass)
                : _c.GetTypeDescForDefinition(_module, (TypeDefinitionHandle)parent),
            _ => null,
        };
        if (type?.Class is not { } owner) return;
        TaintCanonicalVectorOwner(owner);
        if (specification is { } spec && IsNumericsVectorFacade(owner))
            foreach (var argument in spec.DecodeSignature(_c.SigProvider, _method.Context))
                TaintIfCanonical(argument, "numerics-vector-element");
    }
}

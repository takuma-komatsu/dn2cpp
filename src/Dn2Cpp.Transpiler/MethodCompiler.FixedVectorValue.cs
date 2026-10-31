using System.Reflection.Metadata;

namespace Dn2Cpp;

internal sealed partial class MethodCompiler
{
    private void TaintCanonicalFixedVectorValueOwner(ClassInfo owner, string name)
    {
        if (name is not ("Equals" or "GetHashCode")) return;
        if (CoreIntrinsics.IsFixedVectorValueOwner(owner))
            TaintIfCanonical(owner, "fixed-vector-value-element");
        else if (owner.Context.TypeArgs is [{ Class: { } vector }]
            && CoreIntrinsics.IsFixedVectorValueEquatableCandidate(owner, vector))
            TaintIfCanonical(vector, "fixed-vector-value-element");
    }

    private void TaintCanonicalFixedVectorValueOperation(EntityHandle handle)
    {
        if (!SharedTrial) return;
        if (handle.Kind == HandleKind.MethodSpecification)
            handle = _reader.GetMethodSpecification((MethodSpecificationHandle)handle).Method;
        EntityHandle parent;
        string name;
        if (handle.Kind == HandleKind.MemberReference)
        {
            var reference = _reader.GetMemberReference((MemberReferenceHandle)handle);
            parent = reference.Parent;
            name = _reader.GetString(reference.Name);
        }
        else if (handle.Kind == HandleKind.MethodDefinition)
        {
            var definition = _reader.GetMethodDefinition((MethodDefinitionHandle)handle);
            parent = definition.GetDeclaringType();
            name = _reader.GetString(definition.Name);
        }
        else return;
        if (name is not ("Equals" or "GetHashCode")) return;
        if (_constrained?.Class is { } constrained)
            TaintCanonicalFixedVectorValueOwner(constrained, name);
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
        if (type?.Class is { } owner) TaintCanonicalFixedVectorValueOwner(owner, name);
    }
}

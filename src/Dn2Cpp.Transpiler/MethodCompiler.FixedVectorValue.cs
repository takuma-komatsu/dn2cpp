using System.Reflection.Metadata;

namespace Dn2Cpp;

internal sealed partial class MethodCompiler
{
    private void TaintCanonicalFixedVectorValueOwner(ClassInfo owner, string name)
    {
        if (CoreIntrinsics.IsFixedVectorValueOwner(owner))
            TaintIfCanonical(owner, "fixed-vector-value-element");
        else if (name == "Equals" && owner.Context.TypeArgs is [{ Class: { } vector }]
            && CoreIntrinsics.IsFixedVectorValueEquatableCandidate(owner, vector))
            TaintIfCanonical(vector, "fixed-vector-value-element");
    }

    private static bool IsFixedVectorFacade(ClassInfo owner)
    {
        if (owner.Handle.IsNil || owner.Module.AssemblyName != "System.Private.CoreLib") return false;
        var reader = owner.Module.Reader;
        var definition = reader.GetTypeDefinition(owner.Handle);
        return definition.GetDeclaringType().IsNil && definition.GetGenericParameters().Count == 0
            && reader.StringComparer.Equals(definition.Namespace, "System.Runtime.Intrinsics")
            && (reader.StringComparer.Equals(definition.Name, "Vector64")
                || reader.StringComparer.Equals(definition.Name, "Vector128")
                || reader.StringComparer.Equals(definition.Name, "Vector256")
                || reader.StringComparer.Equals(definition.Name, "Vector512"));
    }

    private void TaintCanonicalFixedVectorValueOperation(EntityHandle handle)
    {
        if (!SharedTrial) return;
        MethodSpecification? specification = null;
        if (handle.Kind == HandleKind.MethodSpecification)
        {
            specification = _reader.GetMethodSpecification((MethodSpecificationHandle)handle);
            handle = specification.Value.Method;
        }
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
        if (_constrained?.Class is { } constrained)
            TaintCanonicalFixedVectorValueOwner(constrained, name);
        bool contextualOwner = false;
        if (parent.Kind == HandleKind.TypeSpecification)
        {
            var raw = _reader.GetBlobReader(_reader.GetTypeSpecification((TypeSpecificationHandle)parent).Signature);
            contextualOwner = raw.RemainingBytes > 0 && raw.ReadByte() is 0x13 or 0x1e;
        }
        if (!contextualOwner && !Compilation.VectorValueNamedOwner(_module, parent, includeFacade: true, includeFixed: true)
            && name != "Equals") return;
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
        TaintCanonicalFixedVectorValueOwner(owner, name);
        if (specification is { } spec && IsFixedVectorFacade(owner))
            foreach (var argument in spec.DecodeSignature(_c.SigProvider, _method.Context))
                TaintIfCanonical(argument, "fixed-vector-element");
    }
}

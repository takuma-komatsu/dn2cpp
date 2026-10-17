using Mono.Cecil;

namespace Dn2Cpp;

internal sealed partial class AssemblyDiet
{
    private readonly Dictionary<TypeReference, byte> _attributeTypeIdentities = new();
    private readonly Dictionary<GenericParameter, byte> _attributeIdentityParameters = new();
    private readonly Dictionary<ProjectionNode, byte> _attributeIdentityProjections = new();

    private void ObserveAttributeTypeIdentity(TypeReference reference, bool read)
    {
        EnsureModelReference(reference);
        BindAttributeIdentityReference(reference);
        byte state = read ? (byte)3 : (byte)1;
        _attributeTypeIdentities.TryGetValue(reference, out byte previous);
        if ((previous & state) == state) return;
        _attributeTypeIdentities[reference] = (byte)(previous | state);
        Spend();
        if (reference is GenericParameter parameter)
        {
            _attributeIdentityParameters[parameter] = (byte)(previous | state);
            if (_parameters.TryGetValue(parameter, out var node))
                foreach (var bound in node.Bound.ToArray()) ObserveAttributeTypeIdentity(bound, read);
            return;
        }
        if (reference is ProjectedTypeReference projected)
        {
            _attributeIdentityProjections[projected.Node] = (byte)(previous | state);
            foreach (var target in projected.Node.Targets.ToArray()) ObserveAttributeTypeIdentity(target, read);
            return;
        }
        // Referenced identity is conditional-policy evidence even without a
        // reflection read. Its signature closure is not an executable type use.
        _passiveAttributeMetadataDepth++;
        try { MarkSignatureType(reference); }
        finally { _passiveAttributeMetadataDepth--; }
        if (reference is OptionalModifierType or RequiredModifierType or PinnedType)
        {
            ObserveAttributeTypeIdentity(((TypeSpecification)reference).ElementType, read);
            return;
        }
        if (reference is ArrayType array)
        {
            ObserveAttributeTypeIdentity(array.ElementType, read);
            return;
        }
        if (reference is TypeSpecification && reference is not GenericInstanceType) return;
        if (Resolve(reference) is { } type && CoreIntrinsicTypeMaps.WellKnownPrimitiveCode(Name(type)) is null)
            ApplyConditionalPolicy(type);
        if (reference is GenericInstanceType generic)
        {
            // Read-promoted copied generic identities retain the existing opaque
            // argument contract without promoting the attribute row to a factory.
            bool opaque = read && Resolve(generic) is { } owner && !IsStripped(owner);
            foreach (var argument in generic.GenericArguments)
            {
                if (opaque) QueueDemand(argument, Demand.OpaqueData);
                ObserveAttributeTypeIdentity(argument, read);
            }
        }
    }

    private void ReplayAttributeIdentityBinding(GenericParameter parameter, TypeReference argument)
    {
        if (_attributeIdentityParameters.TryGetValue(parameter, out byte state))
            ObserveAttributeTypeIdentity(argument, (state & 2) != 0);
    }

    private void ReplayAttributeIdentityProjection(ProjectionNode node, TypeReference target)
    {
        if (_attributeIdentityProjections.TryGetValue(node, out byte state))
            ObserveAttributeTypeIdentity(target, (state & 2) != 0);
    }
}

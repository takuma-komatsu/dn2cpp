using Mono.Cecil;

namespace Dn2Cpp;

internal sealed partial class AssemblyDiet
{
    private readonly Dictionary<TypeReference, byte> _attributeTypeIdentities = new();
    private readonly Dictionary<GenericParameter, byte> _attributeIdentityParameters = new();
    private readonly Dictionary<ProjectionNode, byte> _attributeIdentityProjections = new();
    private readonly HashSet<string> _readAttributeContractTypes = new(StringComparer.Ordinal);

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
        if (read && IsClosedAttributeType(reference)) ApplyReadAttributeContracts(reference, null, new());
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

    private void ApplyReadAttributeContracts(TypeReference reference, GenericInstanceType? context, HashSet<TypeReference> path)
    {
        if (reference is ArrayType array)
        {
            ApplyReadAttributeContracts(array.ElementType, context, path);
            return;
        }
        // A formal's actual has already been visited where it entered this
        // hierarchy. Only the inherited declaration contributes a new mask.
        if (IsSymbolic(reference) || reference is TypeSpecification && reference is not GenericInstanceType
            || Resolve(reference) is not { } type || CloseAttributeArgument(reference, context) is not { } closed) return;
        string key = ArgumentKey(closed);
        if (_readAttributeContractTypes.Contains(key)) return;
        Spend();
        var instance = closed as GenericInstanceType;
        if (instance is not null)
        {
            for (int i = 0; i < type.GenericParameters.Count && i < instance.GenericArguments.Count; i++)
                QueueDemand(instance.GenericArguments[i], GenericAccessedMemberDemand(type.GenericParameters[i]));
        }
        // Recursive interface arguments can grow at each substitution. Expand
        // an original signature once per ancestry path, including diamond arms.
        if (!path.Add(reference)) return;
        _readAttributeContractTypes.Add(key);
        if (reference is GenericInstanceType generic)
            foreach (var argument in generic.GenericArguments) ApplyReadAttributeContracts(argument, context, path);
        if (type.BaseType is { } parent) ApplyReadAttributeContracts(parent, instance, path);
        foreach (var implementation in type.Interfaces)
            ApplyReadAttributeContracts(implementation.InterfaceType, instance, path);
        path.Remove(reference);
    }

    private TypeReference? CloseAttributeArgument(TypeReference reference, GenericInstanceType? context)
    {
        if (IsClosedAttributeType(reference)) return reference;
        if (context is null) return null;
        if (reference is GenericParameter parameter)
            return parameter.Type == GenericParameterType.Type && (uint)parameter.Position < (uint)context.GenericArguments.Count
                ? context.GenericArguments[parameter.Position] : null;
        if (reference is GenericInstanceType generic)
        {
            var closed = new GenericInstanceType(generic.ElementType);
            foreach (var argument in generic.GenericArguments)
            {
                if (CloseAttributeArgument(argument, context) is not { } actual) return null;
                closed.GenericArguments.Add(actual);
            }
            return closed;
        }
        if (reference is ArrayType array && CloseAttributeArgument(array.ElementType, context) is { } element)
        {
            if (array.IsVector) return new ArrayType(element);
            var closed = new ArrayType(element, array.Rank);
            for (int i = 0; i < array.Rank; i++) closed.Dimensions[i] = array.Dimensions[i];
            return closed;
        }
        return null;
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

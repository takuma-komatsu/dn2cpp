using Mono.Cecil;

namespace Dn2Cpp;

internal sealed partial class AssemblyDiet
{
    private bool _attributeReadUsed;
    private readonly HashSet<TypeDefinition> _attributeCarrierTypes = new();
    private readonly Dictionary<TypeDefinition, List<SelectedAttributeRow>> _attributeRowsByOwner = new();
    private readonly Dictionary<MethodDefinition, List<SelectedAttributeRow>> _attributeRowsByConstructor = new();
    private readonly Dictionary<FrameworkAttributeName, List<SelectedAttributeRow>> _attributeIdentityNameWaiters = new();
    private readonly HashSet<SelectedAttributeRow> _attributeIdentityWaitingForName = new();
    private readonly Queue<SelectedAttributeRow> _attributeIdentityPending = new();
    private readonly HashSet<SelectedAttributeRow> _attributeIdentityQueued = new();
    private readonly HashSet<TypeReference> _attributeAllocationReferences = new();
    private readonly HashSet<GenericParameter> _attributeAllocationParameters = new();
    private readonly HashSet<ProjectionNode> _attributeAllocationProjections = new();

    private static TypeDefinition? AttributeIdentityOwner(ICustomAttributeProvider provider) => provider switch
    {
        TypeDefinition type => type,
        FieldDefinition field => field.DeclaringType,
        MethodDefinition method => method.DeclaringType,
        PropertyDefinition property => property.DeclaringType,
        ParameterDefinition parameter when parameter.Method is MethodDefinition method => method.DeclaringType,
        MethodReturnType result when result.Method is MethodDefinition method => method.DeclaringType,
        _ => null,
    };

    private void IndexAttributeIdentityRow(SelectedAttributeRow row)
    {
        if (row.Provider is not (AssemblyDefinition or ModuleDefinition))
        {
            var owner = AttributeIdentityOwner(row.Provider);
            if (owner is null || PreservationReader.IsFrameworkAssemblyName(owner.Module.Assembly.Name.Name)) return;
            if (!_attributeRowsByOwner.TryGetValue(owner, out var owners))
                _attributeRowsByOwner.Add(owner, owners = new());
            owners.Add(row);
            if (ResolveMember(row.Attribute.Constructor) is MethodDefinition constructor)
            {
                if (!_attributeRowsByConstructor.TryGetValue(constructor, out var rows))
                    _attributeRowsByConstructor.Add(constructor, rows = new());
                rows.Add(row);
            }
        }
        QueueAttributeIdentity(row);
    }

    private void QueueAttributeIdentity(SelectedAttributeRow row)
    {
        if (!_attributeIdentityQueued.Add(row)) return;
        Spend();
        _attributeIdentityPending.Enqueue(row);
    }

    private void NoteAttributeRead()
    {
        if (_attributeReadUsed) return;
        _attributeReadUsed = true;
        foreach (var row in _selectedAttributeRowsInOrder)
        {
            Spend();
            QueueAttributeIdentity(row);
        }
    }

    private void NoteAttributeCarrier(TypeDefinition type)
    {
        ObserveAttributeModel(type);
        if (!_attributeCarrierTypes.Add(type)) return;
        Spend();
        if (_attributeRowsByOwner.TryGetValue(type, out var rows))
            foreach (var row in rows) QueueAttributeIdentity(row);
    }

    private void NoteAttributeBody(MethodDefinition method)
    {
        NoteAttributeCarrier(method.DeclaringType);
        if (_attributeRowsByConstructor.TryGetValue(method, out var rows))
            foreach (var row in rows) QueueAttributeIdentity(row);
    }

    private void NoteAttributeAllocation(TypeReference reference)
    {
        EnsureModelReference(reference);
        if (!_attributeAllocationReferences.Add(reference)) return;
        Spend();
        if (reference is GenericParameter parameter)
        {
            _attributeAllocationParameters.Add(parameter);
            if (_parameters.TryGetValue(parameter, out var node))
                foreach (var bound in node.Bound.ToArray()) NoteAttributeAllocation(bound);
        }
        else if (reference is ProjectedTypeReference projected)
        {
            _attributeAllocationProjections.Add(projected.Node);
            foreach (var target in projected.Node.Targets.ToArray()) NoteAttributeAllocation(target);
        }
        else if (reference is OptionalModifierType or RequiredModifierType or PinnedType)
            NoteAttributeAllocation(((TypeSpecification)reference).ElementType);
        else if (Resolve(reference) is { IsAbstract: false, IsInterface: false } type
            && (!type.IsValueType || !HasAttribute(type, "System.Runtime.CompilerServices.IsByRefLikeAttribute")))
            NoteAttributeCarrier(type);
    }

    private void AttributeIdentityNameChanged(FrameworkAttributeName name)
    {
        foreach (var pair in _attributeIdentityNameWaiters)
        {
            Spend();
            if (FrameworkAttributeNamesMayMatch(name, pair.Key))
                foreach (var row in pair.Value) QueueAttributeIdentity(row);
        }
    }

    private bool ApplyAttributeIdentities()
    {
        while (_attributeIdentityPending.Count != 0)
        {
            var row = _attributeIdentityPending.Dequeue();
            _attributeIdentityQueued.Remove(row);
            Spend();
            bool carried = row.Provider is AssemblyDefinition or ModuleDefinition;
            if (!carried)
            {
                var owner = AttributeIdentityOwner(row.Provider);
                if (owner is null || !_attributeModeledTypes.Contains(owner)) continue;
                carried = owner.Module == _assemblies[0].Assembly.MainModule || _attributeCarrierTypes.Contains(owner);
            }
            if (ResolveMember(row.Attribute.Constructor) is not MethodDefinition constructor) continue;
            bool constructorReached = _methods.Contains(constructor);
            if (!carried && !constructorReached) continue;
            if (!SelectedAttributeEligible(row.Attribute, out var type))
            {
                if (type is not null && _attributeIdentityWaitingForName.Add(row))
                {
                    var name = FrameworkAttributeNameOf(type);
                    if (!_attributeIdentityNameWaiters.TryGetValue(name, out var rows))
                        _attributeIdentityNameWaiters.Add(name, rows = new());
                    rows.Add(row);
                }
                continue;
            }
            PromoteAttributeEffects(row, type!, classIdentity: carried && !constructorReached);
            row.RetentionRequested = true;
            KeepSelectedAttributeMetadata(row.Attribute);
            KeepAttributeIdentityProvider(row);
            if (carried && !constructorReached && !row.ClassIdentityObserved)
            {
                row.ClassIdentityObserved = true;
                ObserveAttributeTypeIdentity(row.Attribute.AttributeType, read: false);
            }
            ObserveAttributeArguments(row, type!, read: _attributeReadUsed);
        }
        return _work.Count != 0;
    }
}

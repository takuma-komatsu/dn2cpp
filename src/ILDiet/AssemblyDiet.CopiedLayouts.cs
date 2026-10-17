using Mono.Cecil;

namespace Dn2Cpp;

internal sealed partial class AssemblyDiet
{
    private readonly HashSet<TypeDefinition> _copiedLayoutCandidates = new();
    private readonly HashSet<TypeDefinition> _observedCopiedLayouts = new();
    private readonly Queue<TypeDefinition> _copiedLayoutsPending = new();

    private void ObserveCopiedLayout(TypeDefinition type)
    {
        if (!UsesReachedCopyTraversal(type)) return;
        if (type.HasGenericParameters) EnsureOriginalModels();
        if (_copiedLayoutCandidates.Add(type))
        {
            Spend();
            if (type.HasGenericParameters) _modelGenericCopiedCandidates++;
        }
        if (type.HasGenericParameters && type.GenericParameters.Any(parameter =>
                !_parameters.TryGetValue(parameter, out var node) || node.Bound.Count == 0)) return;
        if (!_observedCopiedLayouts.Add(type)) return;
        Spend();
        _copiedLayoutsPending.Enqueue(type);
    }

    private void CopiedLayoutBindingChanged(GenericParameter parameter)
    {
        if (parameter.Owner is TypeDefinition owner && _copiedLayoutCandidates.Contains(owner))
            ObserveCopiedLayout(owner);
    }

    private bool ApplyCopiedLayouts()
    {
        while (_copiedLayoutsPending.Count != 0)
        {
            Spend();
            var type = _copiedLayoutsPending.Dequeue();
            // Match emitted layout closure without treating its copied metadata
            // as an executable owner. Opaque runtime layouts expose no fields.
            bool passive = _passiveAttributeTypes.Contains(type) && !_independentMetadataTypes.Contains(type)
                && !_runtimeTypes.Contains(type);
            var bindingOwner = _passiveBindingOwner;
            if (passive) { _passiveAttributeMetadataDepth++; _passiveBindingOwner = type; }
            try
            {
                MarkSignatureType(type.BaseType);
                if (CopiedRuntimeOwnedType(type)) continue;
                foreach (var implementation in type.Interfaces) MarkSignatureType(implementation.InterfaceType);
                foreach (var field in type.Fields)
                    QueueDemand(field.FieldType, passive ? Demand.PassiveMetadata : Demand.Metadata);
            }
            finally
            {
                _passiveBindingOwner = bindingOwner;
                if (passive) _passiveAttributeMetadataDepth--;
            }
            ApplyConservativeValueDispatch(type);
        }
        return _work.Count != 0;
    }
}

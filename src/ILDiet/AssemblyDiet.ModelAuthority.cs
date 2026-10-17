using Mono.Cecil;

namespace Dn2Cpp;

internal sealed partial class AssemblyDiet
{
    private readonly record struct ModelAuthorityWork(TypeReference Reference, GenericParameter? Parameter = null);
    private readonly Queue<ModelAuthorityWork> _modelAuthorityPending = new();
    private readonly HashSet<TypeReference> _promotedModelReferences = new();
    private readonly HashSet<(GenericParameter Parameter, string Argument)> _ordinaryBindingQueued = new();
    private readonly HashSet<(GenericParameter Left, GenericParameter Right)> _modelAliases = new();
    private readonly HashSet<TypeDefinition> _modelOwnerPromotions = new(), _promotedModelOwners = new();
    private readonly Dictionary<object, List<(GenericParameter Parameter, TypeReference Argument)>> _passiveBindings = new();
    private readonly HashSet<(object Owner, GenericParameter Parameter, TypeReference Argument)> _passiveBindingKeys = new();
    private object? _passiveBindingOwner;
    private bool _drainingModelAuthority;

    private void RegisterModelAliases(Mono.Collections.Generic.Collection<GenericParameter> definitions,
        Mono.Collections.Generic.Collection<GenericParameter> references)
    {
        for (int i = 0; i < definitions.Count && i < references.Count; i++)
        {
            var left = definitions[i];
            var right = references[i];
            if (left == right || !_modelAliases.Add((left, right))) continue;
            Spend();
            var a = Parameter(left);
            var b = Parameter(right);
            a.Aliases.Add(right);
            b.Aliases.Add(left);
            foreach (var argument in a.Bound.ToArray()) QueueOrdinaryBinding(right, argument);
            foreach (var argument in b.Bound.ToArray()) QueueOrdinaryBinding(left, argument);
        }
        DrainModelAuthority();
    }

    private void QueueOrdinaryBinding(GenericParameter parameter, TypeReference argument)
    {
        if (!_ordinaryBindingQueued.Add((parameter, ArgumentKey(argument)))) return;
        Spend();
        _modelAuthorityPending.Enqueue(new(argument, parameter));
    }

    private void QueueModelPromotion(TypeReference reference)
    {
        if (!_promotedModelReferences.Add(reference)) return;
        Spend();
        _modelAuthorityPending.Enqueue(new(reference));
    }

    private void PromoteModelReference(TypeReference reference)
    {
        if (reference is not (GenericParameter or TypeSpecification or ProjectedTypeReference)) return;
        EnsureModelReference(reference);
        QueueModelPromotion(reference);
        DrainModelAuthority();
    }

    private void BindAttributeIdentityReference(TypeReference reference)
    {
        // The admitted identity authorizes this reference's arguments, not every
        // formation of its generic definition or its caller's formal parameters.
        if (reference is not GenericInstanceType generic || Resolve(generic) is not { } definition) return;
        RegisterModelAliases(definition.GenericParameters, generic.ElementType.GenericParameters);
        for (int i = 0; i < definition.GenericParameters.Count && i < generic.GenericArguments.Count; i++)
            QueueOrdinaryBinding(definition.GenericParameters[i], generic.GenericArguments[i]);
        DrainModelAuthority();
    }

    private void RequestModelOwnerPromotion(TypeDefinition type)
    {
        if (!type.HasGenericParameters || !_modelOwnerPromotions.Add(type)) return;
        Spend();
        EnsureOriginalModels();
        TryPromoteModelOwner(type);
    }

    private void TryPromoteModelOwner(TypeDefinition type)
    {
        if (!_modelOwnerPromotions.Contains(type) || _promotedModelOwners.Contains(type)) return;
        if (!HasModelBindings(type, OriginalModelFor(type))) return;
        Spend();
        _promotedModelOwners.Add(type);
        foreach (var parameter in type.GenericParameters) QueueModelPromotion(parameter);
        DrainModelAuthority();
    }

    private void NotePassiveBinding(GenericParameter parameter, TypeReference argument)
    {
        // Record the owning metadata traversal before either global membership
        // test. A later ordinary owner use can authorize an already-known pair.
        if (_passiveBindingOwner is not { } owner || !_passiveBindingKeys.Add((owner, parameter, argument))) return;
        Spend();
        if (owner is TypeDefinition type && (_independentMetadataTypes.Contains(type) || _runtimeTypes.Contains(type))
            || owner is MethodDefinition method && (_independentMetadataMethods.Contains(method) || _methods.Contains(method)))
        {
            QueueOrdinaryBinding(parameter, argument);
            return;
        }
        if (!_passiveBindings.TryGetValue(owner, out var bindings))
            _passiveBindings.Add(owner, bindings = new());
        bindings.Add((parameter, argument));
    }

    private void ReplayPassiveBindings(object owner)
    {
        if (!_passiveBindings.Remove(owner, out var bindings)) return;
        foreach (var binding in bindings) QueueOrdinaryBinding(binding.Parameter, binding.Argument);
        DrainModelAuthority();
    }

    private void DrainModelAuthority()
    {
        if (_drainingModelAuthority) return;
        _drainingModelAuthority = true;
        int modelDepth = _observingOriginalModel, passiveDepth = _passiveAttributeMetadataDepth;
        _observingOriginalModel = 0;
        _passiveAttributeMetadataDepth = 0;
        try
        {
            while (_modelAuthorityPending.Count != 0)
            {
                var work = _modelAuthorityPending.Dequeue();
                Spend();
                if (work.Parameter is { } target)
                {
                    Bind(target, work.Reference, ordinary: true);
                    continue;
                }
                var reference = work.Reference;
                if (reference is GenericParameter parameter)
                {
                    foreach (var argument in Parameter(parameter).FormationBound.ToArray())
                    {
                        QueueOrdinaryBinding(parameter, argument);
                        QueueModelPromotion(argument);
                    }
                }
                else if (reference is ProjectedTypeReference projected)
                {
                    foreach (var argument in projected.Node.Targets.ToArray()) QueueModelPromotion(argument);
                }
                else if (reference is GenericInstanceType generic)
                {
                    if (Resolve(generic) is { } definition)
                    {
                        RegisterModelAliases(definition.GenericParameters, generic.ElementType.GenericParameters);
                        for (int i = 0; i < definition.GenericParameters.Count && i < generic.GenericArguments.Count; i++)
                            QueueOrdinaryBinding(definition.GenericParameters[i], generic.GenericArguments[i]);
                    }
                    foreach (var argument in generic.GenericArguments) QueueModelPromotion(argument);
                }
                else if (reference is FunctionPointerType pointer)
                {
                    QueueModelPromotion(pointer.ReturnType);
                    foreach (var parameterType in pointer.Parameters) QueueModelPromotion(parameterType.ParameterType);
                }
                else if (reference is TypeSpecification specification)
                {
                    QueueModelPromotion(specification.ElementType);
                    if (reference is IModifierType modifier) QueueModelPromotion(modifier.ModifierType);
                }
            }
        }
        finally
        {
            _observingOriginalModel = modelDepth;
            _passiveAttributeMetadataDepth = passiveDepth;
            _drainingModelAuthority = false;
        }
    }
}

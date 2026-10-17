using Mono.Cecil;

namespace Dn2Cpp;

internal sealed partial class AssemblyDiet
{
    private enum ModelActivation : byte { Dormant, Activating, Active }
    private ModelActivation _modelActivation;
    private bool _modelHasUserGenericDefinitions, _modelHasGenericPolicy;
    private long _modelGenericCopiedCandidates;

    private void FinishModelActivationInventory()
    {
        if (_modelHasUserGenericDefinitions || _modelHasGenericPolicy) EnsureOriginalModels();
    }

    private void EnsureModelReference(TypeReference reference)
    {
        if (reference is GenericParameter or GenericInstanceType or ProjectedTypeReference
            || reference.HasGenericParameters)
            EnsureOriginalModels();
    }

    private void EnsureOriginalModels()
    {
        if (_modelActivation != ModelActivation.Dormant) return;
        CheckMutable();
        CheckDormantModelConsumers();
        // Binding callbacks register replay or queue work; they never run queries.
        _modelActivation = ModelActivation.Activating;
        InitializeOriginalModels();
        ApplyOriginalModels();
        if (_modelPending.Count != 0)
            throw new InvalidOperationException("model bootstrap left pending models");
        _modelActivation = ModelActivation.Active;
    }

    private void CheckDormantModelConsumers()
    {
        if (_modelGenericCopiedCandidates != 0 || _parameters.Count != 0 || _projections.Count != 0
            || _attributeIdentityParameters.Count != 0 || _attributeIdentityProjections.Count != 0
            || _attributeAllocationParameters.Count != 0 || _attributeAllocationProjections.Count != 0
            || _originalModels.Count != 0)
            throw new InvalidOperationException("dormant model has binding consumers");
    }

    private void RejectModelActivationQuery(string query)
    {
        if (_modelActivation == ModelActivation.Activating)
            throw new InvalidOperationException("query during model bootstrap: " + query);
    }

    private void RequireCompletedModelActivation(string consumer)
    {
        RejectModelActivationQuery(consumer);
        if (_modelActivation != ModelActivation.Active)
            throw new InvalidOperationException("binding consumer while dormant: " + consumer);
    }

    private void CheckModelActivationFreeze()
    {
        // Budget exhaustion may interrupt bootstrap; the existing copy fallback wins.
        if (_fallback != "none") return;
        RejectModelActivationQuery("freeze");
        if (_modelPending.Count != 0) throw new InvalidOperationException("pending model at freeze");
        if (_modelAuthorityPending.Count != 0 || _drainingModelAuthority)
            throw new InvalidOperationException("pending binding authority at freeze");
        if (_modelActivation == ModelActivation.Dormant) CheckDormantModelConsumers();
    }
}

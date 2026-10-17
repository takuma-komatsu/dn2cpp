using Mono.Cecil;
using MetadataTokens = System.Reflection.Metadata.Ecma335.MetadataTokens;

namespace Dn2Cpp;

internal sealed partial class AssemblyDiet
{
    private bool _formationAsyncActive, _formationAsyncDraining;
    private int _formationAsyncDepth;
    private readonly HashSet<OriginalAsyncCandidate> _formationAsyncCandidates = new();
    private readonly SortedDictionary<int, OriginalAsyncCandidate> _formationAsyncPendingCandidates = new();
    private readonly HashSet<TypeDefinition> _formationAsyncCheckedTypes = new();
    private readonly HashSet<TypeDefinition> _formationAsyncTypes = new();
    private readonly SortedDictionary<long, TypeDefinition> _formationAsyncPendingTypes = new();
    private readonly HashSet<long> _formationAsyncReferences = new();
    private readonly SortedDictionary<long, RawAsyncAdoption.ContractReference> _formationAsyncPendingReferences = new();

    private static long AsyncRowOrder(int module, int token) => ((long)module << 32) | (uint)token;

    private long AsyncTypeOrder(TypeDefinition type) =>
        AsyncRowOrder(_byModule[type.Module].Index, type.MetadataToken.ToInt32());

    private void KeepFormationAsyncComponent(OriginalAsyncCandidate candidate)
    {
        ActivateAsyncFormation();
        QueueAsyncFormationComponent(candidate);
        DrainAsyncFormation();
    }

    private void ActivateAsyncFormation()
    {
        if (_formationAsyncActive) return;
        _formationAsyncActive = true;
        // Earlier passive keeps must participate even if their normal keep
        // path now returns from its membership cache.
        foreach (var type in _formationTypes.OrderBy(AsyncTypeOrder))
        {
            Spend();
            ObserveAsyncFormationType(type);
        }
    }

    private void ObserveAsyncFormationType(TypeDefinition type)
    {
        if (!_byModule.ContainsKey(type.Module)) return;
        if (!_formationAsyncActive)
        {
            if (!_formationAsyncCheckedTypes.Add(type)) return;
            Spend();
            OriginalAsyncTasks();
            // Any passive source can restore a predicate, including a real-shape
            // source whose task has no possible generic backing.
            if (!_originalAsyncByRow.ContainsKey(type)
                && !_originalAsyncBuilderWinners.ContainsKey((type.Namespace, type.Name))) return;
            ActivateAsyncFormation();
        }
        if (!_formationAsyncTypes.Add(type)) return;
        Spend();
        _formationAsyncPendingTypes.Add(AsyncTypeOrder(type), type);
    }

    private void QueueAsyncFormationComponent(OriginalAsyncCandidate candidate)
    {
        var pending = new Queue<OriginalAsyncCandidate>();
        Add(candidate);
        while (pending.Count != 0)
        {
            var current = pending.Dequeue();
            foreach (var sibling in _originalAsyncByAssembly[current.Raw.AssemblyName])
            {
                Spend();
                Add(sibling);
            }
            foreach (var role in current.Roles)
            {
                Spend();
                foreach (var owner in _originalAsyncByRow[role])
                {
                    Spend();
                    Add(owner);
                }
            }
        }

        void Add(OriginalAsyncCandidate item)
        {
            if (!_formationAsyncCandidates.Add(item)) return;
            Spend();
            _formationAsyncPendingCandidates.Add(item.Order, item);
            pending.Enqueue(item);
        }
    }

    private void DrainAsyncFormation()
    {
        if (!_formationAsyncActive || _formationAsyncDraining || _formationAsyncDepth != 0) return;
        _formationAsyncDraining = true;
        try
        {
            while (_formationAsyncPendingTypes.Count != 0 || _formationAsyncPendingCandidates.Count != 0
                || _formationAsyncPendingReferences.Count != 0)
            {
                if (_formationAsyncPendingTypes.Count != 0)
                {
                    var next = _formationAsyncPendingTypes.First();
                    _formationAsyncPendingTypes.Remove(next.Key);
                    Spend();
                    var type = next.Value;
                    if (_originalAsyncByRow.TryGetValue(type, out var owners))
                        foreach (var owner in owners)
                        {
                            Spend();
                            QueueAsyncFormationComponent(owner);
                        }
                    if (_originalAsyncBuilderWinners.TryGetValue((type.Namespace, type.Name), out var winner)
                        && winner is not null)
                        KeepFormationType(winner);
                    continue;
                }
                if (_formationAsyncPendingCandidates.Count != 0)
                {
                    var next = _formationAsyncPendingCandidates.First();
                    _formationAsyncPendingCandidates.Remove(next.Key);
                    KeepAsyncFormationPredicates(next.Value);
                    continue;
                }
                var reference = _formationAsyncPendingReferences.First();
                _formationAsyncPendingReferences.Remove(reference.Key);
                Spend();
                if (!KeepOriginalAsyncWitness(reference.Value))
                    throw new NotSupportedException("ILDiet cannot retain an original async contract reference without a metadata carrier: "
                        + reference.Value.Module.AssemblyName + " token "
                        + MetadataTokens.GetToken(reference.Value.Reference).ToString("X8"));
            }
        }
        finally
        {
            _formationAsyncDraining = false;
        }
    }

    private void KeepAsyncFormationPredicates(OriginalAsyncCandidate candidate)
    {
        Spend();
        foreach (var role in candidate.Roles)
        {
            Spend();
            KeepFormationType(role);
            if (!_originalAsyncReferences.TryGetValue(role, out var references)) continue;
            foreach (var reference in references)
            {
                Spend();
                long order = AsyncRowOrder(reference.Module.Index, MetadataTokens.GetToken(reference.Reference));
                if (!_formationAsyncReferences.Add(order)) continue;
                Spend();
                _formationAsyncPendingReferences.Add(order, reference);
            }
        }
        foreach (var method in candidate.Task.Methods)
        {
            Spend();
            if (method.Name == "GetAwaiter") KeepFormationMethod(method);
        }
        foreach (var method in candidate.Builder.Methods)
        {
            Spend();
            if (RawAsyncAdoption.BuilderMethodNames.Contains(method.Name)) KeepFormationMethod(method);
        }
    }

    private void ValidateAsyncFormationPredicates()
    {
        if (_formationAsyncPendingTypes.Count != 0 || _formationAsyncPendingCandidates.Count != 0
            || _formationAsyncPendingReferences.Count != 0 || _formationAsyncDepth != 0 || _formationAsyncDraining)
            throw new InvalidOperationException("ILDiet left original async predicates pending");
        foreach (var candidate in _formationAsyncCandidates.OrderBy(item => item.Order))
        {
            foreach (var role in candidate.Roles) ValidateKeptType(role);
            foreach (var method in candidate.Task.Methods)
                if (method.Name == "GetAwaiter") Validate(method);
            foreach (var method in candidate.Builder.Methods)
                if (RawAsyncAdoption.BuilderMethodNames.Contains(method.Name)) Validate(method);
        }

        void Validate(MethodDefinition method)
        {
            if (IsStripped(method.DeclaringType) && !KeepsMethod(method))
                throw new InvalidOperationException("ILDiet removed an original async predicate method");
        }
    }
}

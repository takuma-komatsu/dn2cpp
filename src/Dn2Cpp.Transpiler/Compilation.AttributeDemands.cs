using System.Reflection.Metadata;

namespace Dn2Cpp;

internal sealed partial class Compilation
{
    private readonly Dictionary<string, List<(Module Module, CustomAttributeHandle Handle)>> _backendAttributeDemands = new(StringComparer.Ordinal);
    private readonly HashSet<(int Module, CustomAttributeHandle Handle)> _backendAttributeRows = new();
    private readonly Queue<(Module Module, CustomAttributeHandle Handle)> _pendingBackendAttributes = new();
    private long _backendAttributeReplayCount;
    private int _backendAllocationCursor;

    private void ReachBackendAttributes(Module module, CustomAttributeHandleCollection handles)
    {
        foreach (var handle in handles)
        {
            string? name = AttributeTypeName(module.Reader, module.Reader.GetCustomAttribute(handle));
            if (name is null || IsUserTypeofNamedFrameworkType(name)
                || !_backendAttributeRows.Add((module.Index, handle)))
                continue;
            if (!_backendAttributeDemands.TryGetValue(name, out var rows))
                _backendAttributeDemands.Add(name, rows = new List<(Module, CustomAttributeHandle)>());
            rows.Add((module, handle));
        }
        ReachAttributesOf(module, handles, reach: true);
    }

    private void NoteUserTypeofFrameworkType(string name)
    {
        if (!_userTypeofNamedFrameworkTypes.Add(name))
            return;
        // A backend's instance read also constructs rows admitted by a later typeof.
        // Only the registered rows for that type need another admission attempt.
        if (_backendAttributeDemands.TryGetValue(name, out var rows))
        {
            foreach (var row in rows)
                _pendingBackendAttributes.Enqueue(row);
            _backendAttributeDemands.Remove(name);
        }
    }

    private bool ReachPendingBackendDemands()
    {
        bool pending = false;
        if (_backend is not null)
            while (_backendAllocationCursor < _allocatedTypeOrder.Count)
            {
                var allocated = _allocatedTypeOrder[_backendAllocationCursor++];
                if (_backend.AllocatedTypeRoots(this, allocated) is not { } roots)
                    continue;
                pending = true;
                foreach (var method in roots.Methods)
                {
                    Reach(method);
                    ReachRootInitializer(method);
                }
                foreach (var cls in roots.AttributeReadClasses)
                    ReachBackendAttributeRows(cls);
            }
        pending |= _pendingBackendAttributes.Count > 0;
        while (_pendingBackendAttributes.Count > 0)
        {
            var row = _pendingBackendAttributes.Dequeue();
            _backendAttributeReplayCount++;
            if (DecodeCustomAttribute(row.Module, row.Handle, reachedCtorsOnly: false) is { } attribute)
                ConstructAttributeRow(attribute);
        }
        return pending;
    }
}

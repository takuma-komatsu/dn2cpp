using System.Reflection.Metadata;

namespace Dn2Cpp;

internal sealed partial class Compilation
{
    private readonly List<ClassInfo> _allocatedTypeOrder = new();
    private readonly Dictionary<MethodInfo, int> _virtualDemandOrder = new();
    private readonly Dictionary<(int, TypeDefinitionHandle), List<ClassInfo>> _dispatchReceivers = new();
    private readonly Dictionary<(int, TypeDefinitionHandle), List<MethodInfo>> _dispatchDemands = new();
    private int _dispatchReceiverCursor;
    private int _dispatchInterfaceVersion;
    private bool _dispatchProvisional;
    private int _boxedEqualityCursor;
    private long _virtualIndexFallbacks;

    private static (int, TypeDefinitionHandle) DispatchDefinition(ClassInfo cls) =>
        (cls.Module.Index, cls.Handle);

    // Definition identity includes every closed variant of an interface. Exact,
    // variant and array-wrapper dispatch still use their existing resolution rules.
    private List<(int, TypeDefinitionHandle)> ReceiverDefinitions(ClassInfo cls, out bool complete)
    {
        var definitions = new List<(int, TypeDefinitionHandle)>();
        var seen = new HashSet<(int, TypeDefinitionHandle)>();
        for (var b = cls; b is not null; b = b.BaseClass)
            if (seen.Add(DispatchDefinition(b)))
                definitions.Add(DispatchDefinition(b));
        var closure = GetInterfaceClosure(cls);
        complete = ReferenceEquals(cls.InterfaceClosureCache, closure);
        foreach (var itf in closure.Ordered)
            if (seen.Add(DispatchDefinition(itf)))
                definitions.Add(DispatchDefinition(itf));
        return definitions;
    }

    private void IndexDispatchReceivers()
    {
        // A comparer can gain an interface after allocation; descendants inherit it.
        if (_dispatchProvisional || _dispatchInterfaceVersion != _interfaceClosureVersion)
        {
            _dispatchReceivers.Clear();
            _dispatchReceiverCursor = 0;
            _dispatchInterfaceVersion = _interfaceClosureVersion;
            _dispatchProvisional = false;
        }
        while (_dispatchReceiverCursor < _allocatedTypeOrder.Count)
        {
            var cls = _allocatedTypeOrder[_dispatchReceiverCursor++];
            var definitions = ReceiverDefinitions(cls, out bool complete);
            _dispatchProvisional |= !complete;
            foreach (var definition in definitions)
            {
                if (!_dispatchReceivers.TryGetValue(definition, out var receivers))
                    _dispatchReceivers.Add(definition, receivers = new List<ClassInfo>());
                receivers.Add(cls);
            }
        }
    }

    private List<ClassInfo> DispatchReceivers(ClassInfo owner)
    {
        IndexDispatchReceivers();
        // A pending inherited interface can add another definition when its shape
        // completes. Until then retain the original candidate set and retry indexing.
        if (_dispatchProvisional)
        {
            _virtualIndexFallbacks++;
            return _allocatedTypeOrder.ToList();
        }
        return _dispatchReceivers.TryGetValue(DispatchDefinition(owner), out var receivers)
            ? receivers.ToList() : new List<ClassInfo>();
    }

    private void RegisterVirtualDemand(MethodInfo method)
    {
        _virtualDemandOrder.Add(method, _virtualDemandOrder.Count);
        var definition = DispatchDefinition(method.DeclaringClass);
        if (!_dispatchDemands.TryGetValue(definition, out var demands))
            _dispatchDemands.Add(definition, demands = new List<MethodInfo>());
        demands.Add(method);
    }

    private List<MethodInfo> ReceiverVirtualDemands(ClassInfo cls)
    {
        var demands = new List<MethodInfo>();
        var definitions = ReceiverDefinitions(cls, out bool complete);
        if (!complete)
        {
            _virtualIndexFallbacks++;
            return _usedVirtualDecls.ToList();
        }
        foreach (var definition in definitions)
            if (_dispatchDemands.TryGetValue(definition, out var matching))
                demands.AddRange(matching);
        // Preserve the old used-slot order across different declaring definitions.
        demands.Sort((left, right) => _virtualDemandOrder[left].CompareTo(_virtualDemandOrder[right]));
        return demands;
    }
}

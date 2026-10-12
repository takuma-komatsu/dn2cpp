using System.Collections.Generic;
using System.Linq;

namespace Dn2Cpp;

internal sealed partial class Compilation
{
    internal IEnumerable<MethodInfo> GetDelegateIdentityDeclarations() =>
        _delegateIdentities.Values.Select(entry => entry.Method);

    // Keep the declaration selected by a known dispatch, rather than every
    // receiver level: an inherited slot does not require its receiver's rows.
    internal IEnumerable<MethodInfo> GetDelegateSelectedMethods()
    {
        var methods = new HashSet<MethodInfo>();
        var gvms = UsedGvms.ToDictionary(g => g.Gvm.CppName, System.StringComparer.Ordinal);
        var receivers = DelegateCloneReceiverTypes.ToList();
        if (StringInterfaces is { } strings)
            receivers.Add(strings.StringClass);
        foreach (var (method, isVirtual) in _delegateIdentities.Values)
        {
            methods.Add(method);
            if (!isVirtual)
                continue;
            var owner = method.DeclaringClass;
            foreach (var receiver in receivers)
            {
                if (receiver.IsInterface || ContainsCanonPlaceholder(receiver))
                    continue;
                MethodInfo? selected = null;
                if (gvms.TryGetValue(method.CppName, out var dispatch)
                    && dispatch.Cases.TryGetValue(receiver, out var gvm))
                    selected = gvm;
                else if (IsGvmCall(method)
                    && (owner.IsInterface ? ImplementsInterface(receiver, owner)
                        : DerivesFromOrIs(receiver, owner)))
                    selected = method;
                else if (owner.IsInterface)
                {
                    var interfaces = new List<ClassInfo>();
                    for (var level = receiver; level is not null; level = level.BaseClass)
                        foreach (var itf in level.Interfaces)
                            if (!interfaces.Contains(itf))
                                interfaces.Add(itf);
                    if (StringInterfaces is { } si && receiver == si.StringClass)
                        interfaces = si.Dispatches.Select(d => d.Itf).ToList();
                    selected = DelegateInterfaceTarget(receiver, method, interfaces);
                }
                else if (owner.FullName == "System.Object")
                    selected = ObjectVirtualMetadataTarget(receiver, method.Name) ?? method;
                else if (DerivesFromOrIs(receiver, owner)
                    && method.VtableSlot >= 0 && method.VtableSlot < receiver.Vtable.Count)
                    selected = receiver.Vtable[method.VtableSlot];
                if (selected is not null)
                    methods.Add(selected);
            }
            if (EnumInterfaces is { } enums)
            {
                MethodInfo? selected = owner.IsInterface
                    ? DelegateInterfaceTarget(enums.EnumClass, method,
                        enums.Dispatches.Select(d => d.Itf).ToList())
                    : owner.FullName == "System.Object" ? ObjectVirtualMetadataTarget(enums.EnumClass, method.Name)
                    : null;
                if (selected is not null)
                    methods.Add(selected);
            }
            foreach (var info in IntrinsicInterfaces)
                if (info.Itf == owner && info.SlotDecl == method && info.Target is { } target)
                    methods.Add(target);
            foreach (var (_, declaration, target) in RuntimeDelegateInterfaceTargets)
                if (declaration == method)
                    methods.Add(target);
            if (ArrayInterfaceMetadataTarget(method) is { } arrayTarget)
                methods.Add(arrayTarget);
        }
        if (NeedsReflectionDelegateBind)
            foreach (var dispatch in gvms.Values)
            {
                methods.Add(dispatch.Gvm);
                foreach (var selected in dispatch.Cases.Values)
                    methods.Add(selected);
            }
        return methods.OrderBy(m => m.CppName, System.StringComparer.Ordinal);
    }
}

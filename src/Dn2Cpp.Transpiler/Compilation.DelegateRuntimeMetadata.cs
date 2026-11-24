namespace Dn2Cpp;

internal sealed partial class Compilation
{
    private readonly Dictionary<MethodInfo, MethodInfo> _arrayDelegateMetadata = new();
    internal IEnumerable<KeyValuePair<MethodInfo, MethodInfo>> ArrayDelegateMetadataTargets => _arrayDelegateMetadata;

    internal MethodInfo? ArrayInterfaceMetadataTarget(MethodInfo declaration) =>
        _arrayDelegateMetadata.GetValueOrDefault(declaration);

    internal readonly List<(ClassInfo Receiver, MethodInfo Declaration, MethodInfo Target)> RuntimeDelegateInterfaceTargets = new();
    private readonly HashSet<(ClassInfo, MethodInfo)> _runtimeDelegateInterfacePairs = new();

    internal MethodInfo? NullableDelegateRuntimeOverride { get; private set; }

    internal void PrepareNullableDelegateMetadata()
    {
        if (NullableDelegateRuntimeOverride is not null || !ReadsDelegateMethod)
            return;
        foreach (var (declaration, isVirtual) in _delegateIdentities.Values)
        {
            if (!isVirtual || declaration.Name != "GetNullableUnderlyingType" || !CoreIntrinsics.IsNullableTypeQueryBase(declaration))
                continue;
            // typeof produces a runtime-owned receiver that is absent from managed allocations.
            var owner = FindClassByFullName("System.RuntimeType");
            int slot = declaration.VtableSlot;
            if (owner is not null && slot >= 0 && slot < owner.Vtable.Count
                && owner.Vtable[slot] is { } target && CoreIntrinsics.IsRuntimeNullableTypeQuery(target))
            {
                NullableDelegateRuntimeOverride = target;
                _delegateIdentityTargets.Add(target.CppName);
                NoteReferencedType(owner);
                Reach(target);
            }
            return;
        }
    }

    internal void PrepareRuntimeDelegateInterfaceMetadata()
    {
        if (!ReadsDelegateMethod)
            return;
        foreach (var receiver in Classes.ToList())
        {
            if (CoreIntrinsics.RuntimeTypeInfoSymbol(receiver) is null || receiver.IsInterface)
                continue;
            foreach (var declaration in _usedVirtualDecls.ToList())
            {
                if (!declaration.DeclaringClass.IsInterface || declaration.IsStatic
                    || declaration.Context.MethodArgs.Length != 0 || declaration.VtableSlot < 0
                    || !_runtimeDelegateInterfacePairs.Add((receiver, declaration))
                    || !ImplementsInterface(receiver, declaration.DeclaringClass))
                    continue;
                if (ResolveItfImplOrNull(receiver, declaration) is { } target)
                    RuntimeDelegateInterfaceTargets.Add((receiver, declaration, target.EnsureSignature()));
            }
        }
    }

    internal void PrepareArrayDelegateMetadata()
    {
        if (!ReadsDelegateMethod || ArrayEnumerableElementTypes.Count == 0
            || FindClassByFullName("System.SZArrayHelper") is not { } helper)
            return;
        helper.EnsureMembers();
        NoteReferencedType(helper);
        foreach (var map in ArrayEnumerableElementTypes.Values.ToList())
            foreach (var dispatch in map.Dispatches)
                foreach (var (declaration, _) in dispatch.Methods)
                {
                    if (_arrayDelegateMetadata.ContainsKey(declaration))
                        continue;
                    if (dispatch.Itf.Context.TypeArgs.Length == 0)
                    {
                        if (ArrayDispatchClass is { } array
                            && ResolveItfImplOrNull(array, declaration) is { } implementation)
                        {
                            _arrayDelegateMetadata.Add(declaration, implementation);
                            _delegateIdentityTargets.Add(implementation.CppName);
                            RuntimeDelegateInterfaceTargets.Add((array, declaration, implementation));
                        }
                        continue;
                    }
                    if (dispatch.Itf.Context.TypeArgs is not [{ } element])
                        continue;
                    foreach (var handle in helper.Module.Reader.GetTypeDefinition(helper.Handle).GetMethods())
                    {
                        var definition = helper.Module.Reader.GetMethodDefinition(handle);
                        if (helper.Module.Reader.GetString(definition.Name) != declaration.Name
                            || definition.GetGenericParameters().Count != 1)
                            continue;
                        var method = InstantiateMethodOnClass(helper, helper.Module, handle, new[] { element });
                        method.EnsureSignature();
                        if (method.Signature.ParameterTypes.Length == declaration.Signature.ParameterTypes.Length)
                        {
                            _arrayDelegateMetadata.Add(declaration, method);
                            _delegateIdentityTargets.Add(method.CppName);
                            break;
                        }
                    }
                }
    }
}

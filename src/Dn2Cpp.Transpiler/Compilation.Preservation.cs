using System.Reflection.Metadata;
using PreserveKind = Dn2Cpp.PreservationReader.PreserveKind;
using PreservePolicy = Dn2Cpp.PreservationReader.PreservePolicy;

namespace Dn2Cpp;

internal sealed partial class Compilation
{
    private readonly IReadOnlyList<string> _projectRoots;
    private readonly IReadOnlyList<string> _linkXmlFiles;
    private readonly HashSet<string> _linkFeatures;
    private Dictionary<(int Module, TypeDefinitionHandle Type), PreservePolicy> _preservePolicies = new();
    private readonly HashSet<ClassInfo> _explicitReflectionKeep = new();
    private readonly HashSet<ClassInfo> _activatedConditionalPolicies = new();
    private bool _preservationSeedingActive;
    private long _conditionalPreservePasses, _conditionalPreserveVisits;
    private long _conditionalPreserveChecks, _conditionalPreserveActivations;
    private readonly HashSet<(int Module, TypeDefinitionHandle Type)> _conditionalPreserveDefinitions = new();
    private readonly List<ClassInfo> _conditionalPreserveClasses = new();
    private readonly Dictionary<ClassInfo, int> _conditionalPreserveOrdinals = new();
    private readonly SortedSet<int> _conditionalPreservePending = new();
    private readonly HashSet<int> _conditionalPreserveDeferred = new();
    private int _conditionalPreserveClassCursor;
    private int _conditionalPreservePosition = -1;

    // A generic definition's rule applies to the instantiations the program mints, never
    // to one minted only by applying the rule (its signatures, or bodies only it admitted),
    // so preserve cannot expand a self-nesting generic. A program body that later names
    // such an instantiation, or reaches such a body, restores the rule for it.
    private readonly HashSet<MethodInfo> _preserveOnlyBodies = new();
    private readonly HashSet<ClassInfo> _preserveMintedInstances = new();
    private int _instancePreserveDepth;
    private bool _scanningBody, _scanningPreserveOnlyBody;

    private bool MintingFromPreserve => _instancePreserveDepth > 0 || _scanningPreserveOnlyBody
        || _decodingMember is { Spec: var decoding } && _preserveMintedInstances.Count > 0
            && _preserveMintedInstances.Contains(decoding);

    /// <summary>Records <paramref name="m"/> as admitted only by an instantiation's preserve
    /// rule, or restores a program edge to it; true when it must be scanned again.</summary>
    private bool NotePreserveReach(MethodInfo m)
    {
        if (MintingFromPreserve || !_scanningBody && _preserveMintedInstances.Count > 0
            && _preserveMintedInstances.Contains(m.DeclaringClass))
        {
            if (!Reachable.Contains(m))
                _preserveOnlyBodies.Add(m);
            return false;
        }
        return _preserveOnlyBodies.Count > 0 && _preserveOnlyBodies.Remove(m);
    }

    private readonly List<ClassInfo> _preserveRestored = new();

    private void NotePreserveMint(ClassInfo spec, bool minted)
    {
        if (!_preservationSeedingActive)
            return;
        if (minted)
        {
            if (MintingFromPreserve)
                _preserveMintedInstances.Add(spec);
        }
        else if (_scanningBody && !MintingFromPreserve && _preserveMintedInstances.Remove(spec))
            _preserveRestored.Add(spec);
    }

    /// <summary>Applies the rule to the instantiations program bodies named during the
    /// last scan, outside it, so no walk of the scan sees the model grow.</summary>
    private void ApplyRestoredPreservation()
    {
        for (int i = 0; i < _preserveRestored.Count; i++)
        {
            var spec = _preserveRestored[i];
            if (spec.MembersReady)
                ApplyPreservation(spec);
            else if (spec.ShapeReady)
                ApplyPreservationAfterShape(spec);
        }
        _preserveRestored.Clear();
    }

    private void DiscoverPreservationPolicies()
    {
        var modules = new List<PreservationModule>();
        foreach (var module in Modules)
            modules.Add(new PreservationModule { Index = module.Index, AssemblyName = module.AssemblyName, Reader = module.Reader });
        _preservePolicies = new PreservationReader(modules, _projectRoots, _linkXmlFiles,
            _linkFeatures.ToArray()).Read();
        foreach (var entry in _preservePolicies)
        {
            var policy = entry.Value;
            if (policy.ConditionalKind != PreserveKind.None
                || policy.ConditionalFields.Count > 0 || policy.ConditionalMethods.Count > 0
                || policy.ConditionalProperties.Count > 0 || policy.ConditionalEvents.Count > 0)
                _conditionalPreserveDefinitions.Add(entry.Key);
        }
    }

    private void SeedPreservedMembers()
    {
        _preservationSeedingActive = true;
        foreach (var cls in Classes.ToList())
            if (cls.MembersReady)
                ApplyPreservation(cls);
            else if (cls.ShapeReady)
                ApplyPreservationAfterShape(cls);
    }

    private void ApplyPreservation(ClassInfo cls)
    {
        // Policies key on the TypeDef handle, so an open shell (an unsubstituted
        // !!n spec from an empty-context overload decode) matches its definition's
        // policy — preserving it drags an open definition into the emit set. Each
        // closed instantiation gets the policy applied as it completes.
        if (ContainsGenericVar(cls))
            return;
        if (!_preservePolicies.TryGetValue((cls.Module.Index, cls.Handle), out var policy)
            || _preserveMintedInstances.Contains(cls))
            return;
        if (cls.Context.TypeArgs.Length == 0)
        {
            ApplyPolicy(cls, policy);
            return;
        }
        _instancePreserveDepth++;
        try
        {
            ApplyPolicy(cls, policy);
        }
        finally
        {
            _instancePreserveDepth--;
        }
    }

    private void ApplyPolicy(ClassInfo cls, PreservePolicy policy)
    {
        bool conditional = _activatedConditionalPolicies.Contains(cls);
        PreserveKind kind = policy.Kind | (conditional ? policy.ConditionalKind : PreserveKind.None);
        bool delegateAll = cls.IsDelegate && kind != PreserveKind.None;
        if (kind != PreserveKind.None || policy.Fields.Count > 0 || policy.Methods.Count > 0
            || policy.Properties.Count > 0 || policy.Events.Count > 0
            || conditional && (policy.ConditionalFields.Count > 0 || policy.ConditionalMethods.Count > 0
                || policy.ConditionalProperties.Count > 0 || policy.ConditionalEvents.Count > 0))
        {
            NoteForceEmit(cls);
            _explicitReflectionKeep.Add(cls);
            TrimNoteNamedClass(cls);
            NoteReflectionBoxed(cls);
        }
        if ((kind & PreserveKind.Fields) != 0)
            foreach (var field in cls.Fields) PreserveField(field);
        else
            foreach (var field in cls.Fields)
                if (policy.Fields.Contains(field.Handle)
                    || conditional && policy.ConditionalFields.Contains(field.Handle)) PreserveField(field);
        bool ctorPreserved = false;
        bool keepMethods = delegateAll || (kind & (PreserveKind.Methods | PreserveKind.DefaultConstructor)) != 0
            || policy.Methods.Count > 0 || conditional && policy.ConditionalMethods.Count > 0;
        // By index: preserving a method can instantiate methods on cls.
        for (int i = 0; keepMethods && i < cls.Methods.Count; i++)
        {
            var method = cls.Methods[i];
            if (PolicyKeeps(policy, kind, conditional, delegateAll, method))
            {
                PreserveMethod(method);
                ctorPreserved |= method.Name == ".ctor" && !method.IsStatic && method.Rva != 0;
            }
        }
        // A preserved instance ctor declares "reflection constructs this" — the
        // instance then dispatches through vtable and interface slots, so the
        // class must cross the used-slot × allocated-type product. Reaching the
        // ctor alone leaves every slot of the minted instance a trap stub (on
        // wasm that trap is an unnamed call_indirect signature mismatch).
        // Construction is allocation only: an interface body follows a used slot.
        if (ctorPreserved && !cls.IsValueType && !cls.IsAbstract && !cls.IsInterface
            && !cls.IsDelegate)
            ReachAllocatedType(cls);
        foreach (var ph in policy.Properties)
            PreservePropertyType(cls, ph);
        foreach (var eh in policy.Events)
            PreserveEventType(cls, eh);
        if (conditional)
        {
            foreach (var ph in policy.ConditionalProperties) PreservePropertyType(cls, ph);
            foreach (var eh in policy.ConditionalEvents) PreserveEventType(cls, eh);
        }
    }

    private static bool PolicyKeeps(PreservePolicy policy, PreserveKind kind, bool conditional, bool delegateAll,
        MethodInfo method) =>
        delegateAll || (kind & PreserveKind.Methods) != 0
        || policy.Methods.Contains(method.Handle)
        || conditional && policy.ConditionalMethods.Contains(method.Handle)
        || (kind & PreserveKind.DefaultConstructor) != 0
            && method.Name == ".ctor" && method.Signature.ParameterTypes.Length == 0;

    /// <summary>Whether a descriptor or <c>PreserveAttribute</c> rule on the definition of
    /// runtime-template level <paramref name="level"/> keeps <paramref name="method"/>, which
    /// the level's clones then run (<see cref="ReachAskedTemplateMembers"/>).</summary>
    private bool PreservesTemplateMember(ClassInfo level, MethodInfo method)
    {
        if (!_preservePolicies.TryGetValue((level.Module.Index, level.Handle), out var policy)
            || _preserveMintedInstances.Contains(level))
            return false;
        bool conditional = _activatedConditionalPolicies.Contains(level);
        PreserveKind kind = policy.Kind | (conditional ? policy.ConditionalKind : PreserveKind.None);
        return PolicyKeeps(policy, kind, conditional, level.IsDelegate && kind != PreserveKind.None, method);
    }

    private void PreservePropertyType(ClassInfo cls, PropertyDefinitionHandle handle)
    {
        var sig = cls.Module.Reader.GetPropertyDefinition(handle).DecodeSignature(SigProvider, cls.Context);
        NotePreservedType(sig.ReturnType);
        foreach (var type in sig.ParameterTypes) NotePreservedType(type);
    }

    private void PreserveEventType(ClassInfo cls, EventDefinitionHandle handle)
    {
        var ed = cls.Module.Reader.GetEventDefinition(handle);
        TypeDesc type = ed.Type.Kind switch
        {
            HandleKind.TypeDefinition => GetTypeDescForDefinition(cls.Module, (TypeDefinitionHandle)ed.Type),
            HandleKind.TypeReference => ResolveTypeRef(cls.Module, (TypeReferenceHandle)ed.Type) ?? TypeDesc.MakeExternal("?"),
            HandleKind.TypeSpecification => cls.Module.Reader.GetTypeSpecification((TypeSpecificationHandle)ed.Type)
                .DecodeSignature(SigProvider, cls.Context),
            _ => TypeDesc.MakeExternal("?"),
        };
        NotePreservedType(type);
    }

    private void ApplyPreservationAfterShape(ClassInfo cls)
    {
        if (!_preservationSeedingActive
            || ContainsGenericVar(cls) // open shell — see ApplyPreservation
            || !_preservePolicies.TryGetValue((cls.Module.Index, cls.Handle), out var policy)
            || _preserveMintedInstances.Contains(cls))
            return;
        if (cls.MembersReady)
        {
            ApplyPreservation(cls);
            return;
        }
        bool conditional = PreservedTypeWasOtherwiseUsed(cls);
        PreserveKind kind = policy.Kind | (conditional ? policy.ConditionalKind : PreserveKind.None);
        if ((kind & (PreserveKind.Methods | PreserveKind.DefaultConstructor)) != 0
            || policy.Methods.Count > 0 || conditional && policy.ConditionalMethods.Count > 0
            || cls.IsDelegate && kind != PreserveKind.None)
            CompleteMembers(cls);
        else
            ApplyPreservation(cls);
    }

    private bool PreservedTypeWasOtherwiseUsed(ClassInfo cls) =>
        ReferencedTypes.Contains(cls) || ForceEmittedClasses.Contains(cls)
        || IsAllocated(cls) || Reachable.Any(m => m.DeclaringClass == cls)
        || _reflectionRoots.Count > 0 && ReflectionRootMatching(cls) is not null;

    private bool ActivateConditionalPreservationPolicies()
    {
        if (!_preservationSeedingActive || _conditionalPreserveDefinitions.Count == 0)
            return false;
        _conditionalPreservePasses++;
        // Snapshot the same class boundary as a full pass. Facts for an earlier
        // class wait for the next drain; later classes still activate in this one.
        int end = Classes.Count;
        while (_conditionalPreserveClassCursor < end)
        {
            var cls = Classes[_conditionalPreserveClassCursor++];
            _conditionalPreserveVisits++;
            if (!_conditionalPreserveDefinitions.Contains((cls.Module.Index, cls.Handle)))
                continue;
            int ordinal = _conditionalPreserveClasses.Count;
            _conditionalPreserveClasses.Add(cls);
            _conditionalPreserveOrdinals.Add(cls, ordinal);
            _conditionalPreservePending.Add(ordinal);
        }
        bool activated = false;
        while (_conditionalPreservePending.Count > 0)
        {
            int ordinal = _conditionalPreservePending.Min;
            _conditionalPreservePending.Remove(ordinal);
            _conditionalPreservePosition = ordinal;
            var cls = _conditionalPreserveClasses[ordinal];
            if (!_activatedConditionalPolicies.Contains(cls) && CheckConditionalPreservationUse(cls))
            {
                _activatedConditionalPolicies.Add(cls);
                _conditionalPreserveActivations++;
                if (cls.ShapeReady) ApplyPreservationAfterShape(cls);
                activated = true;
            }
        }
        _conditionalPreservePosition = -1;
        foreach (int ordinal in _conditionalPreserveDeferred)
            _conditionalPreservePending.Add(ordinal);
        _conditionalPreserveDeferred.Clear();
        if (activated)
            ReachPreservedStrippedGvmCases();
        return activated;
    }

    private bool CheckConditionalPreservationUse(ClassInfo cls)
    {
        _conditionalPreserveChecks++;
        return PreservedTypeWasOtherwiseUsed(cls);
    }

    private void NoteConditionalPreservationUse(ClassInfo cls)
    {
        if (_conditionalPreserveOrdinals.TryGetValue(cls, out int ordinal)
            && !_activatedConditionalPolicies.Contains(cls))
        {
            if (ordinal <= _conditionalPreservePosition)
                _conditionalPreserveDeferred.Add(ordinal);
            else
                _conditionalPreservePending.Add(ordinal);
        }
    }

    private bool AddReachable(MethodInfo method)
    {
        if (!Reachable.Add(method))
            return false;
        NoteConditionalPreservationUse(method.DeclaringClass);
        return true;
    }

    private void ApplyPreservationToInstantiatedMethod(MethodInfo method)
    {
        if (!_preservationSeedingActive || MintingFromPreserve
            || !PreservesMethod(method.DeclaringClass, method.Handle))
            return;
        _instancePreserveDepth++;
        try
        {
            PreserveMethod(method);
        }
        finally
        {
            _instancePreserveDepth--;
        }
        // Same allocation rule as ApplyPreservation: a preserved instance
        // ctor of a concrete reference instantiation is a late-bound
        // construction site.
        var cls = method.DeclaringClass;
        if (method.Name == ".ctor" && !method.IsStatic && method.Rva != 0
            && !cls.IsValueType && !cls.IsAbstract && !cls.IsInterface && !cls.IsDelegate)
            ReachAllocatedType(cls);
    }

    /// <summary>Whether a descriptor or <c>PreserveAttribute</c> rule keeps method
    /// definition <paramref name="handle"/> on <paramref name="cls"/> or on each of its
    /// instantiations.</summary>
    private bool PreservesMethod(ClassInfo cls, MethodDefinitionHandle handle)
    {
        if (!_preservePolicies.TryGetValue((cls.Module.Index, cls.Handle), out var policy)
            || _preserveMintedInstances.Contains(cls))
            return false;
        bool conditional = _activatedConditionalPolicies.Contains(cls);
        PreserveKind kind = policy.Kind | (conditional ? policy.ConditionalKind : PreserveKind.None);
        return (kind & PreserveKind.Methods) != 0 || policy.Methods.Contains(handle)
            || conditional && policy.ConditionalMethods.Contains(handle);
    }

    private void PreserveField(FieldInfo field)
    {
        NoteForceEmit(field.DeclaringClass);
        NotePreservedType(field.Type);
        NoteReflectionBoxed(field.Type);
        ReachCctor(field.DeclaringClass);
    }

    private void PreserveMethod(MethodInfo method)
    {
        // A runtime-template level's bodies are its clones'; ReachAskedTemplateMembers
        // admits a preserved one only as reflection enters a clone, so one no clone can
        // run drops its row rather than failing the template.
        if (method.DeclaringClass.Context.TypeArgs.Any(ContainsCanonAny))
            return;
        NoteForceEmit(method.DeclaringClass);
        var sig = method.Signature;
        NotePreservedType(sig.ReturnType);
        foreach (var type in sig.ParameterTypes) NotePreservedType(type);
        Reach(method);
        // Reflection enters a preserved member with no IL caller to trigger its type's
        // initializer, as a run-time root does.
        ReachRootInitializer(method);
        NoteReflectionInvokeBoxes(method);
    }

    private void NotePreservedType(TypeDesc type)
    {
        switch (type.Kind)
        {
            case TypeKind.Class:
                NoteForceEmit(type.Class);
                if (type.Class is not null)
                {
                    _explicitReflectionKeep.Add(type.Class);
                    TrimNoteNamedClass(type.Class);
                }
                break;
            case TypeKind.SZArray:
            case TypeKind.MDArray:
            case TypeKind.ByRef:
                if (type.Element is not null) NotePreservedType(type.Element);
                break;
            case TypeKind.ExternalGeneric:
                if (type.GenericArgs is not null)
                    foreach (var arg in type.GenericArgs) NotePreservedType(arg);
                break;
        }
        if (type.Kind == TypeKind.Class && type.Class is { } cls)
            foreach (var arg in cls.Context.TypeArgs) NotePreservedType(arg);
    }
}

using Mono.Cecil;
using Mono.Cecil.Cil;
using CecilInstruction = Mono.Cecil.Cil.Instruction;

namespace Dn2Cpp;

internal sealed partial class AssemblyDiet
{
    private readonly HashSet<TypeDefinition> _signatureTypes = new();
    private readonly HashSet<GenericInstanceType> _signatureApplicationInstances = new();
    private int _knownGenericDepth;
    private readonly HashSet<TypeDefinition> _applicationMetadata = new();
    private readonly HashSet<MethodDefinition> _signatureMethods = new();
    private readonly HashSet<GenericParameter> _signatureParameters = new();
    private readonly HashSet<TypeDefinition> _constructionSurface = new();
    private readonly HashSet<TypeDefinition> _constructedTypes = new();

    private IEnumerable<TypeDefinition> ReflectionApplicationTypes() =>
        AllTypes(_assemblies[0].Assembly.MainModule.Types).Where(type =>
            !_suppressDefaultSeeds.Contains(type)
            && (!type.HasGenericParameters || _runtimeTypes.Contains(type)
                || (_constructsFromRuntimeType || _runsReflectedMethods) && _signatureTypes.Contains(type)));

    private bool NoteApplicationInstance(GenericInstanceType instance, bool shapeOwner = false)
    {
        if (instance.ContainsGenericParameter) return false;
        // All modules contribute type instantiation depth. Shape-only discoveries
        // join the snapshot without raising the depth seed for subsequent walks.
        if (!shapeOwner)
        {
            _knownGenericDepth = Math.Max(_knownGenericDepth, ReflectedFieldTypeDepth(instance));
            NoteCopiedShapeDepth(instance, null, null, null, null);
        }
        if (Resolve(instance) is not { } owner
            || owner.Module != _assemblies[0].Assembly.MainModule || !IsStripped(owner)) return false;
        foreach (var known in _signatureApplicationInstances)
            if (SameReflectedFieldType(known, instance)) return false;
        return _signatureApplicationInstances.Add(instance);
    }

    // Generic methods add their own instantiation level even when their body
    // does not allocate a type, matching emission's method-depth accounting.
    private void NoteGenericMethodDepth(GenericInstanceMethod method)
    {
        int depth = 0;
        foreach (var argument in method.GenericArguments)
        {
            if (argument.ContainsGenericParameter) return;
            depth = Math.Max(depth, ReflectedFieldTypeDepth(argument));
        }
        _knownGenericDepth = Math.Max(_knownGenericDepth, depth + 1);
    }

    private sealed class MethodDepthContext
    {
        internal required MethodDefinition Method;
        internal required int[] TypeArguments;
        internal required int[] MethodArguments;
        internal GenericInstanceType? DeclaringInstance;
        // Equal depth vectors can select different generic factory constructors.
        internal TypeReference[] MethodTypes = Array.Empty<TypeReference>();
        internal bool ReflectionOnly;
        internal bool ReflectionEntry;
        internal bool DispatchSlot;
        internal MethodDepthContext? Predecessor;
    }

    private readonly Dictionary<MethodDefinition, List<MethodDepthContext>> _methodDepthContexts = new();
    private readonly Queue<MethodDepthContext> _pendingDepthContexts = new();
    private readonly HashSet<MethodDefinition> _reflectionDepthMethods = new();
    private ApplicationShapeWalk? _reflectionDepthWalk;
    private List<GenericInstanceType>? _reflectionFirstInstances;
    private readonly Dictionary<TypeDefinition, List<int[]>> _reflectionFirstArguments = new();
    private readonly Dictionary<TypeDefinition, List<(GenericInstanceType? Instance, int[] Arguments)>> _reflectionDepthContexts = new();
    private readonly Dictionary<TypeDefinition, int> _reflectionMintedWalks = new();
    private bool _virtualDepthContextsChanged;
    // Partial depth summaries can lose reflected bodies. Budget exhaustion
    // aborts before rewriting, independently of emission's instantiation count.
    private readonly int _depthContextNestingLimit = EnvKnobs.PositiveInt(EnvKnobs.MaxGenericDepth, 32);
    private readonly int _depthContextCountLimit = EnvKnobs.PositiveInt(EnvKnobs.MaxInstantiations, 1_000_000);
    private int _depthContextCount;
    private MethodDepthContext? _depthSummaryCaller;

    private string DepthSummaryDriver(string subject)
    {
        var chain = new List<string>();
        var seen = new HashSet<MethodDepthContext>();
        for (var caller = _depthSummaryCaller; caller is not null && seen.Add(caller); caller = caller.Predecessor)
            chain.Add(caller.Method.DeclaringType.FullName + "." + caller.Method.Name);
        return "\n  while summarizing " + subject
            + (chain.Count == 0 ? "" : "\n  [chain: " + string.Join(" <- ", chain) + "]");
    }

    private void CheckDepthContextDepth(int depth, string subject)
    {
        if (depth > _depthContextNestingLimit)
            throw new NotSupportedException($"ILDiet depth summary needs generic nesting depth {depth}, past the {_depthContextNestingLimit}-level limit ({EnvKnobs.MaxGenericDepth})." + DepthSummaryDriver(subject));
    }

    private void CountDepthContext(string subject)
    {
        if (_depthContextCount >= _depthContextCountLimit)
            throw new NotSupportedException($"ILDiet depth summary exceeded its {_depthContextCountLimit} depth-context record budget ({EnvKnobs.MaxInstantiations}); this count is separate from emitted instantiations." + DepthSummaryDriver(subject));
        _depthContextCount++;
    }

    // Scanning a definition loses caller substitutions. Depth-only contexts
    // recover those instantiations without retaining additional IL dependencies.
    private void NoteMethodDepthContext(MethodReference reference, MethodDefinition method,
        MethodDepthContext? caller, bool dispatchSlot = false, bool allocateReceiver = false)
    {
        NoteCopiedShapeDepth(reference.DeclaringType, caller?.Method.DeclaringType, caller?.TypeArguments,
            caller?.Method, caller?.MethodArguments);
        if (reference is GenericInstanceMethod argumentMethod)
            foreach (var argument in argumentMethod.GenericArguments)
                NoteCopiedShapeDepth(argument, caller?.Method.DeclaringType, caller?.TypeArguments,
                    caller?.Method, caller?.MethodArguments);
        int[] typeArguments = reference.DeclaringType is GenericInstanceType owner
            ? owner.GenericArguments.Select(argument => CallerTypeDepth(argument, caller)).ToArray()
            : Array.Empty<int>();
        int[] methodArguments = reference is GenericInstanceMethod generic
            ? generic.GenericArguments.Select(argument => CallerTypeDepth(argument, caller)).ToArray()
            : Array.Empty<int>();
        if (typeArguments.Any(depth => depth < 0) || methodArguments.Any(depth => depth < 0)) return;
        int typeDepth = typeArguments.Length == 0 ? 0 : typeArguments.Max() + 1;
        int methodDepth = methodArguments.Length == 0 ? 0 : methodArguments.Max() + 1;
        int depth = Math.Max(typeDepth, methodDepth);
        CheckDepthContextDepth(depth, method.FullName);
        _knownGenericDepth = Math.Max(_knownGenericDepth, depth);
        TypeReference declaringType = SubstituteCallerType(reference.DeclaringType, caller);
        TypeReference[] methodTypes = reference is GenericInstanceMethod instanceMethod
            ? instanceMethod.GenericArguments.Select(argument => SubstituteCallerType(argument, caller)).ToArray()
            : Array.Empty<TypeReference>();
        var context = KeepMethodDepthContext(method, typeArguments, methodArguments,
            declaringType as GenericInstanceType, methodTypes, caller?.ReflectionOnly ?? false,
            dispatchSlot: dispatchSlot, predecessor: caller);
        if (method.IsStatic || method.IsConstructor)
            NoteTypeInitializerDepths(declaringType, method.DeclaringType, typeArguments, caller?.ReflectionOnly ?? false);
        if (allocateReceiver && method.IsConstructor && !method.IsStatic)
            NoteAllocatedTypeDepths(declaringType, method.DeclaringType, typeArguments, caller?.ReflectionOnly ?? false);
        if (context is not null && dispatchSlot) NoteVirtualMethodDepths(context);
    }

    private MethodDepthContext? KeepMethodDepthContext(MethodDefinition method,
        int[] typeArguments, int[] methodArguments, GenericInstanceType? declaringInstance = null,
        TypeReference[]? methodTypes = null, bool reflectionOnly = false, bool reflectionEntry = false,
        bool dispatchSlot = false, MethodDepthContext? predecessor = null)
    {
        if (_cutMethods.Contains(method) || (!IsStripped(method.DeclaringType) && !method.IsVirtual)
            || typeArguments.Length != method.DeclaringType.GenericParameters.Count
            || methodArguments.Length != method.GenericParameters.Count) return null;
        if (!_methodDepthContexts.TryGetValue(method, out var contexts))
            _methodDepthContexts.Add(method, contexts = new());
        foreach (var previous in contexts)
            if (previous.ReflectionOnly == reflectionOnly && previous.ReflectionEntry == reflectionEntry
                && previous.DispatchSlot == dispatchSlot
                && previous.TypeArguments.SequenceEqual(typeArguments)
                && previous.MethodArguments.SequenceEqual(methodArguments)
                && SameReflectedFieldType(previous.DeclaringInstance, declaringInstance)
                && SameMethodArgumentTypes(previous.MethodTypes, methodTypes)) return previous;
        CountDepthContext(method.FullName);
        var context = new MethodDepthContext
        {
            Method = method, TypeArguments = typeArguments, MethodArguments = methodArguments,
            DeclaringInstance = declaringInstance, MethodTypes = methodTypes ?? Array.Empty<TypeReference>(),
            ReflectionOnly = reflectionOnly, ReflectionEntry = reflectionEntry, DispatchSlot = dispatchSlot,
            Predecessor = predecessor ?? _depthSummaryCaller
        };
        contexts.Add(context);
        if (dispatchSlot) _virtualDepthContextsChanged = true;
        if (method.HasBody && IsStripped(method.DeclaringType)) _pendingDepthContexts.Enqueue(context);
        return context;
    }

    private readonly HashSet<TypeDefinition> _depthRuntimeTypes = new();
    private readonly HashSet<TypeDefinition> _ordinaryDepthRuntimeTypes = new();
    private readonly Dictionary<TypeDefinition, List<(GenericInstanceType Instance, int[] Arguments, bool ReflectionOnly)>> _runtimeTypeDepthArguments = new();
    private readonly Dictionary<TypeDefinition, List<(int[] Arguments, bool ReflectionOnly)>> _symbolicRuntimeTypeDepthArguments = new();

    private void NoteRuntimeTypeDepthContexts(GenericInstanceType instance, TypeDefinition owner,
        int[]? arguments = null, bool virtualBodiesOnly = false, bool reflectionOnly = false)
    {
        if (arguments is null && instance.ContainsGenericParameter) return;
        arguments ??= instance.GenericArguments.Select(ReflectedFieldTypeDepth).ToArray();
        // Inherited virtual bodies run with the substituted base receiver even
        // when the ordinary callvirt names Object. This adds no method roots.
        var path = new HashSet<TypeDefinition>();
        // Base contexts describe inherited bodies, not additional allocations.
        _depthRuntimeTypes.Add(owner);
        if (!reflectionOnly) _ordinaryDepthRuntimeTypes.Add(owner);
        while (IsStripped(owner) && path.Add(owner))
        {
            NoteRuntimeTypeOwnDepthContexts(instance, owner, arguments, virtualBodiesOnly, reflectionOnly);
            if (owner.BaseType is not GenericInstanceType baseType
                || SubstituteClosedApplicationType(baseType, owner, instance) is not GenericInstanceType parent
                || Resolve(parent) is not { } baseOwner) break;
            arguments = baseType.GenericArguments.Select(argument =>
                ContextTypeDepth(argument, owner, arguments, null, null)).ToArray();
            instance = parent;
            owner = baseOwner;
        }
    }

    private void NoteRuntimeTypeOwnDepthContexts(GenericInstanceType instance, TypeDefinition owner,
        int[] arguments, bool virtualBodiesOnly, bool reflectionOnly)
    {
        if (!IsStripped(owner) || arguments.Any(depth => depth < 0)) return;
        if (arguments.Length == 0 || arguments.Length != owner.GenericParameters.Count) return;
        CheckDepthContextDepth(arguments.Max() + 1, owner.FullName);
        // Symbolic caller receivers carry depth vectors without inventing closed
        // type identities for dispatch binding or signature discovery.
        if (!instance.ContainsGenericParameter)
        {
            if (!_runtimeTypeDepthArguments.TryGetValue(owner, out var contexts))
                _runtimeTypeDepthArguments.Add(owner, contexts = new());
            if (!contexts.Any(previous => previous.ReflectionOnly == reflectionOnly
                && SameReflectedFieldType(previous.Instance, instance)))
            {
                CountDepthContext(owner.FullName);
                contexts.Add((instance, arguments, reflectionOnly));
                _virtualDepthContextsChanged = true;
            }
        }
        else
        {
            if (!_symbolicRuntimeTypeDepthArguments.TryGetValue(owner, out var contexts))
                _symbolicRuntimeTypeDepthArguments.Add(owner, contexts = new());
            if (!contexts.Any(previous => previous.ReflectionOnly == reflectionOnly && previous.Arguments.SequenceEqual(arguments)))
            {
                CountDepthContext(owner.FullName);
                contexts.Add((arguments, reflectionOnly));
                _virtualDepthContextsChanged = true;
            }
        }
        // Explicit method roots supply closed receivers independently of slots.
        // Conservatively retained bodies wait for an executable dispatch edge.
        foreach (var method in owner.Methods)
            if (_depthMethods.Contains(method) && !method.HasGenericParameters
                && (!virtualBodiesOnly || method.IsVirtual || method.HasOverrides))
                KeepMethodDepthContext(method, arguments, Array.Empty<int>(), instance, reflectionOnly: reflectionOnly);
    }

    // Body retention alone does not make a method executable. An explicit root
    // can arrive after its receiver was retained for layout or dispatch metadata.
    private void NoteRootedMethodReceiverDepths(MethodDefinition method)
    {
        if (method.HasGenericParameters) return;
        if (_runtimeTypeDepthArguments.TryGetValue(method.DeclaringType, out var contexts))
            foreach (var context in contexts.ToList())
                KeepMethodDepthContext(method, context.Arguments, Array.Empty<int>(), context.Instance, reflectionOnly: context.ReflectionOnly);
        if (_symbolicRuntimeTypeDepthArguments.TryGetValue(method.DeclaringType, out var symbolic))
            foreach (var context in symbolic.ToList())
                KeepMethodDepthContext(method, context.Arguments, Array.Empty<int>(), reflectionOnly: context.ReflectionOnly);
    }

    private void NoteTypeInitializerDepths(TypeReference reference, TypeDefinition owner, int[]? arguments = null,
        bool reflectionOnly = false)
    {
        arguments ??= reference is GenericInstanceType instance
            ? instance.GenericArguments.Select(ReflectedFieldTypeDepth).ToArray() : Array.Empty<int>();
        foreach (var method in owner.Methods)
            if (method.IsConstructor && method.IsStatic)
                KeepMethodDepthContext(method, arguments, Array.Empty<int>(), reference as GenericInstanceType, reflectionOnly: reflectionOnly);
    }

    // Allocation wires these object slots independently of explicit callvirt.
    // Other retained virtual bodies wait for an executable slot/receiver join.
    private void NoteAllocatedTypeDepths(TypeReference reference, TypeDefinition owner,
        int[]? arguments = null, bool reflectionOnly = false)
    {
        if (owner.IsAbstract || owner.IsInterface) return;
        NoteTypeInitializerDepths(reference, owner, arguments, reflectionOnly);
        bool valueType = owner.IsValueType;
        var remaining = new HashSet<string>(StringComparer.Ordinal) { "ToString" };
        if (!valueType && !IsDelegate(owner))
        {
            remaining.Add("GetHashCode");
            remaining.Add("Equals");
            remaining.Add("Finalize");
        }
        var path = new HashSet<TypeDefinition>();
        bool receiver = true;
        while (IsStripped(owner) && path.Add(owner))
        {
            var instance = reference as GenericInstanceType;
            arguments ??= instance is null ? Array.Empty<int>()
                : instance.GenericArguments.Select(ReflectedFieldTypeDepth).ToArray();
            if (receiver)
            {
                if (instance is not null)
                    NoteRuntimeTypeDepthContexts(instance, owner, arguments, virtualBodiesOnly: true, reflectionOnly);
                else if (_depthRuntimeTypes.Add(owner)) _virtualDepthContextsChanged = true;
                if (!reflectionOnly && _ordinaryDepthRuntimeTypes.Add(owner)) _virtualDepthContextsChanged = true;
                receiver = false;
            }
            else if (instance is not null)
                NoteRuntimeTypeOwnDepthContexts(instance, owner, arguments, virtualBodiesOnly: true, reflectionOnly);
            if (IsAsyncStateMachine(owner))
                foreach (var method in owner.Methods)
                    if (_methods.Contains(method) && method.Parameters.Count == 0
                        && (method.Name == "MoveNext" || method.Overrides.Any(overridden =>
                            overridden.Name == "MoveNext"
                            && overridden.DeclaringType.FullName == "System.Runtime.CompilerServices.IAsyncStateMachine")))
                        KeepMethodDepthContext(method, arguments, Array.Empty<int>(), instance, reflectionOnly: reflectionOnly);
            foreach (var method in owner.Methods)
                if (!method.HasGenericParameters && ObjectDepthSlot(method) is { } slot
                    && remaining.Remove(slot))
                    KeepMethodDepthContext(method, arguments, Array.Empty<int>(), instance, reflectionOnly: reflectionOnly);
            if (owner.BaseType is null) break;
            TypeReference parent = SubstituteClosedApplicationType(owner.BaseType, owner, instance);
            arguments = owner.BaseType is GenericInstanceType generic
                ? generic.GenericArguments.Select(argument => ContextTypeDepth(argument, owner, arguments, null, null)).ToArray()
                : Array.Empty<int>();
            reference = parent;
            if (Resolve(parent) is not { } baseOwner) break;
            owner = baseOwner;
        }
    }

    private static bool IsAsyncStateMachine(TypeDefinition type) => type.Interfaces.Any(implementation =>
        implementation.InterfaceType.FullName == "System.Runtime.CompilerServices.IAsyncStateMachine");

    private string? ObjectDepthSlot(MethodDefinition method)
    {
        if (method.IsStatic || !method.IsVirtual) return null;
        foreach (var overridden in method.Overrides)
        {
            try
            {
                if (overridden.Resolve() is { } declaration && ObjectDepthSlot(declaration) is { } slot)
                    return slot;
            }
            catch (AssemblyResolutionException) { }
        }
        string name = method.Name;
        string result = method.ReturnType.FullName;
        bool shape = name switch
        {
            "ToString" => method.Parameters.Count == 0 && result == "System.String",
            "GetHashCode" => method.Parameters.Count == 0 && result == "System.Int32",
            "Equals" => method.Parameters.Count == 1 && result == "System.Boolean"
                && method.Parameters[0].ParameterType.FullName == "System.Object",
            "Finalize" => method.Parameters.Count == 0 && result == "System.Void",
            _ => false
        };
        if (!shape) return null;
        if (method.DeclaringType.FullName == "System.Object") return name;
        if (method.IsNewSlot) return null;
        for (var parent = method.DeclaringType.BaseType; parent is not null;)
        {
            var owner = Resolve(parent);
            if (owner is null) return name;
            foreach (var candidate in owner.Methods)
                if (candidate.IsVirtual && candidate.Name == name
                    && candidate.ReturnType.FullName == result
                    && candidate.Parameters.Count == method.Parameters.Count)
                    return ObjectDepthSlot(candidate);
            parent = owner.BaseType;
        }
        return name;
    }

    private void NoteVirtualMethodDepths(MethodDepthContext slot)
    {
        if (!slot.DispatchSlot) return;
        foreach (var owner in _depthRuntimeTypes.ToList())
        {
            if (!owner.HasGenericParameters)
                NoteVirtualOwnerDepths(owner, null, Array.Empty<int>(), slot,
                    reflectionReceiver: !_ordinaryDepthRuntimeTypes.Contains(owner));
            else
            {
                if (_runtimeTypeDepthArguments.TryGetValue(owner, out var contexts))
                    foreach (var context in contexts)
                        NoteVirtualOwnerDepths(owner, context.Instance, context.Arguments, slot, reflectionReceiver: context.ReflectionOnly);
                if (_symbolicRuntimeTypeDepthArguments.TryGetValue(owner, out var symbolicContexts))
                    foreach (var context in symbolicContexts)
                        NoteVirtualOwnerDepths(owner, null, context.Arguments, slot, symbolicReceiver: true,
                            reflectionReceiver: context.ReflectionOnly);
            }
        }
    }

    private void NoteVirtualOwnerDepths(TypeDefinition owner, GenericInstanceType? instance,
        int[] arguments, MethodDepthContext slot, bool symbolicReceiver = false, bool reflectionReceiver = false)
    {
        TypeReference? binding = FindDispatchBinding(instance ?? (TypeReference)owner, slot, new(),
            symbolicReceiver ? owner : null, symbolicReceiver ? arguments : null);
        if (binding is null || slot.ReflectionEntry && !AdmitReflectedDepthOwner(owner, instance, arguments)) return;
        var levels = new List<(TypeDefinition Owner, GenericInstanceType? Instance, int[] Arguments)>();
        var path = new HashSet<TypeDefinition>();
        while (path.Add(owner))
        {
            levels.Add((owner, instance, arguments));
            if (owner.BaseType is null) break;
            TypeReference parent = SubstituteClosedApplicationType(owner.BaseType, owner, instance);
            arguments = owner.BaseType is GenericInstanceType generic
                ? generic.GenericArguments.Select(argument => ContextTypeDepth(argument, owner, arguments, null, null)).ToArray()
                : Array.Empty<int>();
            instance = parent as GenericInstanceType;
            if (Resolve(parent) is not { } baseOwner) break;
            owner = baseOwner;
        }
        MethodDefinition? selected = slot.Method;
        if (slot.Method.DeclaringType.IsInterface)
            selected = InterfaceDepthImplementation(levels, slot, binding);
        if (selected is null) return;
        var implementation = ClassDepthImplementation(levels, selected);
        if (implementation is not { } body || !_methods.Contains(body.Method) || !body.Method.HasBody) return;
        if (body.Method != slot.Method)
            KeepMethodDepthContext(body.Method, body.Arguments, slot.MethodArguments, body.Instance, slot.MethodTypes,
                slot.ReflectionOnly || reflectionReceiver, slot.ReflectionEntry, predecessor: slot);
    }

    // Interface maps select a class slot at a listing level. Descendants can
    // override that slot, but an unrelated name match cannot replace the map.
    private MethodDefinition? InterfaceDepthImplementation(
        List<(TypeDefinition Owner, GenericInstanceType? Instance, int[] Arguments)> levels,
        MethodDepthContext slot, TypeReference binding)
    {
        var listing = new List<int>();
        for (int i = 0; i < levels.Count; i++)
        {
            var level = levels[i];
            foreach (var method in level.Owner.Methods)
                if (method.GenericParameters.Count == slot.MethodArguments.Length
                    && MatchesDispatchMethod(method, level.Instance, slot, binding, explicitOnly: true)) return method;
            if (ListedDepthInterface(level.Owner, level.Instance, slot.Method.DeclaringType, binding) is null) continue;
            listing.Add(i);
            if (PublicDepthImplementation(level.Owner, level.Instance, level.Owner, slot, binding) is { } local)
                return local;
        }
        for (int i = listing.Count - 1; i >= 0; i--)
        {
            int stop = i + 1 < listing.Count ? listing[i + 1] : levels.Count;
            for (int j = listing[i] + 1; j < stop; j++)
                if (PublicDepthImplementation(levels[j].Owner, levels[j].Instance,
                    levels[listing[i]].Owner, slot, binding) is { } inherited) return inherited;
        }
        return null;
    }

    private MethodDefinition? PublicDepthImplementation(TypeDefinition owner, GenericInstanceType? instance,
        TypeDefinition listing, MethodDepthContext slot, TypeReference binding)
    {
        var definitionBinding = ListedDepthInterface(listing, null, slot.Method.DeclaringType, null);
        var ownerBinding = ClassDepthBinding(listing, owner) as GenericInstanceType;
        foreach (var method in owner.Methods)
        {
            if (!method.IsPublic || !method.IsVirtual || method.IsStatic
                || method.GenericParameters.Count != slot.MethodArguments.Length
                || !MatchesDispatchMethod(method, instance, slot, binding)) continue;
            if (listing.HasGenericParameters && definitionBinding is not null
                && !SameDepthSignature(method, ownerBinding, slot.Method, definitionBinding as GenericInstanceType)) continue;
            return method;
        }
        return null;
    }

    private TypeReference? ListedDepthInterface(TypeDefinition owner, GenericInstanceType? instance,
        TypeDefinition target, TypeReference? binding)
    {
        foreach (var implementation in owner.Interfaces)
        {
            var found = ListedDepthInterfaceShape(implementation.InterfaceType,
                SubstituteClosedApplicationType(implementation.InterfaceType, owner, instance), target, binding, new());
            if (found is not null) return found;
        }
        return null;
    }

    private TypeReference? ListedDepthInterfaceShape(TypeReference definition, TypeReference actual,
        TypeDefinition target, TypeReference? binding, HashSet<TypeDefinition> path)
    {
        if (Resolve(definition) is not { } owner) return null;
        if (owner == target) return binding is null || SameDispatchType(actual, binding) ? definition : null;
        if (!path.Add(owner)) return null;
        TypeReference? found = null;
        foreach (var implementation in owner.Interfaces)
        {
            found = ListedDepthInterfaceShape(
                SubstituteClosedApplicationType(implementation.InterfaceType, owner, definition as GenericInstanceType),
                SubstituteClosedApplicationType(implementation.InterfaceType, owner, actual as GenericInstanceType),
                target, binding, path);
            if (found is not null) break;
        }
        path.Remove(owner);
        return found;
    }

    private TypeReference? ClassDepthBinding(TypeDefinition owner, TypeDefinition target)
    {
        TypeReference current = owner;
        var path = new HashSet<TypeDefinition>();
        while (path.Add(owner))
        {
            if (owner == target) return current;
            if (owner.BaseType is null) break;
            current = SubstituteClosedApplicationType(owner.BaseType, owner, current as GenericInstanceType);
            if (Resolve(current) is not { } parent) break;
            owner = parent;
        }
        return null;
    }

    private bool SameDepthSignature(MethodDefinition left, GenericInstanceType? leftInstance,
        MethodDefinition right, GenericInstanceType? rightInstance)
    {
        if (left.GenericParameters.Count != right.GenericParameters.Count
            || left.Parameters.Count != right.Parameters.Count) return false;
        for (int i = 0; i < left.Parameters.Count; i++)
            if (!SameDispatchType(SubstituteClosedApplicationType(left.Parameters[i].ParameterType,
                    left.DeclaringType, leftInstance),
                SubstituteClosedApplicationType(right.Parameters[i].ParameterType, right.DeclaringType, rightInstance))) return false;
        return SameDispatchType(SubstituteClosedApplicationType(left.ReturnType, left.DeclaringType, leftInstance),
            SubstituteClosedApplicationType(right.ReturnType, right.DeclaringType, rightInstance));
    }

    // Build only slot identities from metadata. A newslot hider and its overrides
    // stay separate even when a substituted signature also matches an older slot.
    private (MethodDefinition Method, GenericInstanceType? Instance, int[] Arguments)? ClassDepthImplementation(
        List<(TypeDefinition Owner, GenericInstanceType? Instance, int[] Arguments)> levels, MethodDefinition selected)
    {
        var slots = new List<(MethodDefinition Method, GenericInstanceType? Instance, int[] Arguments)>();
        var methodSlots = new Dictionary<MethodDefinition, int>();
        for (int i = levels.Count - 1; i >= 0; i--)
        {
            var level = levels[i];
            int inherited = slots.Count;
            foreach (var method in level.Owner.Methods)
            {
                if (!method.IsVirtual) continue;
                int found = -1;
                foreach (var overridden in method.Overrides)
                {
                    try
                    {
                        var declaration = overridden.Resolve();
                        if (declaration is not null && !declaration.DeclaringType.IsInterface
                            && methodSlots.TryGetValue(declaration, out int explicitSlot)) found = explicitSlot;
                    }
                    catch (AssemblyResolutionException) { }
                }
                if (found < 0 && !method.IsNewSlot)
                {
                    var matches = new List<int>();
                    for (int j = inherited - 1; j >= 0; j--)
                        if (slots[j].Method.Name == method.Name
                            && SameDepthSignature(method, level.Instance, slots[j].Method, slots[j].Instance)) matches.Add(j);
                    if (matches.Count > 0) found = matches[0];
                    if (matches.Count > 1)
                        foreach (int j in matches)
                            if (SameDepthSignature(method, null, slots[j].Method,
                                ClassDepthBinding(level.Owner, slots[j].Method.DeclaringType) as GenericInstanceType))
                            {
                                found = j;
                                break;
                            }
                }
                if (found < 0)
                {
                    found = slots.Count;
                    slots.Add((method, level.Instance, level.Arguments));
                }
                else slots[found] = (method, level.Instance, level.Arguments);
                methodSlots[method] = found;
            }
        }
        if (methodSlots.TryGetValue(selected, out int slot))
            return slots[slot].Method.IsAbstract ? null : slots[slot];
        foreach (var level in levels)
            if (level.Owner == selected.DeclaringType) return (selected, level.Instance, level.Arguments);
        return null;
    }

    // Bind a slot through the receiver's inheritance shape.
    // Definition guards are branch-local so independent interface contexts survive.
    private TypeReference? FindDispatchBinding(TypeReference reference, MethodDepthContext slot,
        HashSet<TypeDefinition> path, TypeDefinition? depthOwner = null, int[]? depthArguments = null)
    {
        if (Resolve(reference) is not { } owner) return null;
        if (owner == slot.Method.DeclaringType)
        {
            if (depthOwner is null && slot.DeclaringInstance is { ContainsGenericParameter: false } declared)
                return SameReflectedFieldType(reference, declared) ? reference : null;
            if (reference is GenericInstanceType generic
                && !generic.GenericArguments.Select(argument => depthOwner is null
                    ? ReflectedFieldTypeDepth(argument)
                    : ContextTypeDepth(argument, depthOwner, depthArguments, null, null))
                    .SequenceEqual(slot.TypeArguments)) return null;
            return reference;
        }
        if (!path.Add(owner)) return null;
        var instance = reference as GenericInstanceType;
        TypeReference? found = null;
        if (owner.BaseType is not null)
            found = FindDispatchBinding(SubstituteClosedApplicationType(owner.BaseType, owner, instance), slot, path,
                depthOwner, depthArguments);
        if (found is null)
            foreach (var implementation in owner.Interfaces)
            {
                found = FindDispatchBinding(SubstituteClosedApplicationType(implementation.InterfaceType, owner, instance), slot, path,
                    depthOwner, depthArguments);
                if (found is not null) break;
            }
        path.Remove(owner);
        return found;
    }

    private bool MatchesDispatchMethod(MethodDefinition method, GenericInstanceType? instance,
        MethodDepthContext slot, TypeReference binding, bool explicitOnly = false)
    {
        bool explicitSlot = false;
        foreach (var overridden in method.Overrides)
        {
            try
            {
                if (overridden.Resolve() == slot.Method
                    && SameReflectedFieldType(SubstituteClosedApplicationType(overridden.DeclaringType,
                        method.DeclaringType, instance), binding)) explicitSlot = true;
            }
            catch (AssemblyResolutionException) { }
        }
        if (explicitOnly && !explicitSlot) return false;
        if (!explicitSlot && (method.Name != slot.Method.Name
            || !slot.Method.DeclaringType.IsInterface && (!method.IsVirtual || method.IsNewSlot))) return false;
        if (method.Parameters.Count != slot.Method.Parameters.Count) return false;
        var slotInstance = binding as GenericInstanceType;
        for (int i = 0; i < method.Parameters.Count; i++)
            if (!SameDispatchType(SubstituteClosedApplicationType(method.Parameters[i].ParameterType,
                    method.DeclaringType, instance),
                SubstituteClosedApplicationType(slot.Method.Parameters[i].ParameterType,
                    slot.Method.DeclaringType, slotInstance))) return false;
        return explicitSlot || SameDispatchType(SubstituteClosedApplicationType(method.ReturnType,
                method.DeclaringType, instance),
            SubstituteClosedApplicationType(slot.Method.ReturnType, slot.Method.DeclaringType, slotInstance));
    }

    private bool SameDispatchType(TypeReference left, TypeReference right)
    {
        if (left is GenericParameter parameter)
            return right is GenericParameter other && parameter.Type == other.Type
                && parameter.Position == other.Position;
        if (left is GenericInstanceType generic)
        {
            if (right is not GenericInstanceType other || generic.GenericArguments.Count != other.GenericArguments.Count
                || !SameReflectedFieldType(generic.ElementType, other.ElementType)) return false;
            for (int i = 0; i < generic.GenericArguments.Count; i++)
                if (!SameDispatchType(generic.GenericArguments[i], other.GenericArguments[i])) return false;
            return true;
        }
        if (left is ArrayType array)
        {
            if (right is not ArrayType other || array.IsVector != other.IsVector || array.Rank != other.Rank)
                return false;
            for (int i = 0; i < array.Dimensions.Count; i++)
                if (array.Dimensions[i].LowerBound != other.Dimensions[i].LowerBound
                    || array.Dimensions[i].UpperBound != other.Dimensions[i].UpperBound) return false;
        }
        if (left is TypeSpecification specification)
        {
            if (right is not TypeSpecification other || left.MetadataType != right.MetadataType) return false;
            if (left is IModifierType modifier
                && (right is not IModifierType otherModifier
                    || !SameReflectedFieldType(modifier.ModifierType, otherModifier.ModifierType))) return false;
            return SameDispatchType(specification.ElementType, other.ElementType);
        }
        return SameReflectedFieldType(left, right);
    }

    private static int CallerTypeDepth(TypeReference? reference, MethodDepthContext? caller) =>
        ContextTypeDepth(reference, caller?.Method.DeclaringType, caller?.TypeArguments,
            caller?.Method, caller?.MethodArguments);

    private static int ContextTypeDepth(TypeReference? reference, TypeDefinition? owner,
        int[]? typeArguments, MethodDefinition? method, int[]? methodArguments)
    {
        if (reference is GenericParameter parameter)
        {
            int[]? arguments = parameter.Owner == method ? methodArguments
                : parameter.Owner == owner ? typeArguments : null;
            return arguments is not null && parameter.Position < arguments.Length
                ? arguments[parameter.Position] : -1;
        }
        if (reference is GenericInstanceType generic)
        {
            int depth = 0;
            foreach (var argument in generic.GenericArguments)
            {
                int argumentDepth = ContextTypeDepth(argument, owner, typeArguments, method, methodArguments);
                if (argumentDepth < 0) return -1;
                depth = Math.Max(depth, argumentDepth);
            }
            return depth + 1;
        }
        if (reference is ArrayType or ByReferenceType or PointerType)
        {
            int depth = ContextTypeDepth(((TypeSpecification)reference).ElementType,
                owner, typeArguments, method, methodArguments);
            return depth < 0 ? -1 : depth + 1;
        }
        if (reference is TypeSpecification specification)
            return ContextTypeDepth(specification.ElementType, owner, typeArguments, method, methodArguments);
        return 0;
    }

    private readonly Dictionary<TypeDefinition, List<int[]>> _copiedShapeDepthContexts = new();
    private readonly Queue<(TypeDefinition Owner, int[] Arguments)> _pendingCopiedShapeDepths = new();

    // Framework shape completion closes bases and interfaces before the route
    // depth freezes. Summarize those shapes without entering their method bodies.
    private void NoteCopiedShapeDepth(TypeReference? reference, TypeDefinition? owner,
        int[]? typeArguments, MethodDefinition? method, int[]? methodArguments)
    {
        if (reference is GenericInstanceType generic)
        {
            foreach (var argument in generic.GenericArguments)
                NoteCopiedShapeDepth(argument, owner, typeArguments, method, methodArguments);
            var arguments = generic.GenericArguments.Select(argument =>
                ContextTypeDepth(argument, owner, typeArguments, method, methodArguments)).ToArray();
            if (arguments.Any(depth => depth < 0) || Resolve(generic) is not { } definition
                || !_byModule.TryGetValue(definition.Module, out var assembly) || !assembly.Copy) return;
            int depth = arguments.Length == 0 ? 1 : arguments.Max() + 1;
            CheckDepthContextDepth(depth, definition.FullName);
            _knownGenericDepth = Math.Max(_knownGenericDepth, depth);
            if (!_copiedShapeDepthContexts.TryGetValue(definition, out var contexts))
                _copiedShapeDepthContexts.Add(definition, contexts = new());
            foreach (var previous in contexts)
                if (previous.SequenceEqual(arguments)) return;
            CountDepthContext(definition.FullName);
            contexts.Add(arguments);
            _pendingCopiedShapeDepths.Enqueue((definition, arguments));
        }
        else if (reference is TypeSpecification specification)
            NoteCopiedShapeDepth(specification.ElementType, owner, typeArguments, method, methodArguments);
    }

    private void NoteCopiedShapeDepths()
    {
        while (_pendingCopiedShapeDepths.Count != 0)
        {
            var context = _pendingCopiedShapeDepths.Dequeue();
            NoteCopiedShapeDepth(context.Owner.BaseType, context.Owner, context.Arguments, null, null);
            foreach (var implementation in context.Owner.Interfaces)
                NoteCopiedShapeDepth(implementation.InterfaceType, context.Owner, context.Arguments, null, null);
        }
    }

    private void NoteCallerTypeDepth(TypeReference? reference, MethodDepthContext caller)
    {
        NoteCopiedShapeDepth(reference, caller.Method.DeclaringType, caller.TypeArguments,
            caller.Method, caller.MethodArguments);
        int depth = CallerTypeDepth(reference, caller);
        CheckDepthContextDepth(depth, reference?.FullName ?? caller.Method.FullName);
        _knownGenericDepth = Math.Max(_knownGenericDepth, depth);
    }

    private void NoteClosedCallerDepths()
    {
        bool attributeTypesChanged;
        // Receivers and generic slots can arrive in either order. Settle their
        // depth-only joins before the single application-signature snapshot.
        do
        {
            _virtualDepthContextsChanged = false;
            foreach (var entry in _methodDepthContexts.ToList())
                if (entry.Key.IsVirtual)
                    foreach (var context in entry.Value.ToList())
                        if (context.DispatchSlot) NoteVirtualMethodDepths(context);
            DrainCallerDepthContexts();
            NoteCopiedShapeDepths();
            NoteReflectedDepthRoots();
            attributeTypesChanged = NoteReflectionAttributeDepthRoots();
        } while (_virtualDepthContextsChanged || _pendingDepthContexts.Count != 0 || attributeTypesChanged);
    }

    // Whole-class reflection roots have the same fixed first-walk depth and
    // minted/one-further boundary as emission. Calls inside admitted bodies still
    // consume the ordinary fatal depth budget; they do not refresh this anchor.
    private bool AdmitReflectedDepthOwner(TypeDefinition owner, GenericInstanceType? instance, int[] arguments)
    {
        if (_reflectionDepthWalk is null) return false;
        if (!_reflectionDepthContexts.TryGetValue(owner, out var contexts))
            _reflectionDepthContexts.Add(owner, contexts = new());
        foreach (var previous in contexts)
            if (previous.Arguments.SequenceEqual(arguments)
                && SameReflectedFieldType(previous.Instance, instance)) return true;
        int depth = arguments.Length == 0 ? 0 : arguments.Max() + 1;
        bool minted = instance is not null
            ? !_reflectionFirstInstances!.Any(previous => SameReflectedFieldType(previous, instance))
            : owner.HasGenericParameters && (!_reflectionFirstArguments.TryGetValue(owner, out var initial)
                || !initial.Any(previous => previous.SequenceEqual(arguments)));
        _reflectionMintedWalks.TryGetValue(owner, out int walks);
        if (PreservationReader.ReflectionRouteWalksWithinDepth(depth, _reflectionDepthWalk.Depth, minted, walks))
        {
            if (minted) _reflectionMintedWalks[owner] = walks + 1;
        }
        else if (!_reflectionDepthWalk.Further.Add(owner)) return false;
        contexts.Add((instance, arguments));
        return true;
    }

    private void NoteReflectedDepthRoots()
    {
        if (!_depthRunsReflectedMethods) return;
        if (_reflectionDepthWalk is null)
        {
            _reflectionDepthWalk = new ApplicationShapeWalk { Depth = _knownGenericDepth };
            _reflectionFirstInstances = _signatureApplicationInstances.ToList();
            foreach (var contexts in _runtimeTypeDepthArguments.Values)
                foreach (var context in contexts) _reflectionFirstInstances.Add(context.Instance);
            foreach (var entry in _symbolicRuntimeTypeDepthArguments)
                _reflectionFirstArguments.Add(entry.Key, entry.Value.Select(context => context.Arguments).ToList());
        }
        foreach (var method in _reflectionDepthMethods.ToList())
        {
            // Interface instance bodies need a receiver slot, not a class-wide Invoke root.
            if (method.HasGenericParameters
                || method.DeclaringType.IsInterface && method.IsVirtual && !method.IsStatic) continue;
            var owner = method.DeclaringType;
            if (!owner.HasGenericParameters)
                KeepMethodDepthContext(method, Array.Empty<int>(), Array.Empty<int>(),
                    reflectionOnly: true, reflectionEntry: true, dispatchSlot: method.IsVirtual);
            else
            {
                foreach (var instance in _signatureApplicationInstances.ToList())
                    if (Resolve(instance) == owner)
                        NoteReflectedDepthMethod(method, instance,
                            instance.GenericArguments.Select(ReflectedFieldTypeDepth).ToArray());
                if (_runtimeTypeDepthArguments.TryGetValue(owner, out var contexts))
                    foreach (var context in contexts.ToList())
                        NoteReflectedDepthMethod(method, context.Instance, context.Arguments);
                if (_symbolicRuntimeTypeDepthArguments.TryGetValue(owner, out var symbolic))
                    foreach (var context in symbolic.ToList())
                        NoteReflectedDepthMethod(method, null, context.Arguments);
            }
        }
    }

    private void NoteReflectedDepthMethod(MethodDefinition method, GenericInstanceType? instance, int[] arguments)
    {
        if (AdmitReflectedDepthOwner(method.DeclaringType, instance, arguments))
            KeepMethodDepthContext(method, arguments, Array.Empty<int>(), instance,
                reflectionOnly: true, reflectionEntry: true, dispatchSlot: method.IsVirtual);
    }

    private void DrainCallerDepthContexts()
    {
        try
        {
            while (_pendingDepthContexts.Count != 0)
            {
                var caller = _pendingDepthContexts.Dequeue();
                _depthSummaryCaller = caller;
                NoteCallerTypeDepth(caller.Method.ReturnType, caller);
                foreach (var parameter in caller.Method.Parameters)
                    NoteCallerTypeDepth(parameter.ParameterType, caller);
                foreach (var variable in caller.Method.Body.Variables)
                    NoteCallerTypeDepth(variable.VariableType, caller);
                var liveness = CallerBranchLiveness(caller);
                foreach (var instruction in caller.Method.Body.Instructions)
                {
                    if (liveness is not null && (!liveness.LiveAt(instruction.Offset)
                        || liveness.ElidedAt(instruction.Offset))) continue;
                    if (instruction.Operand is MethodReference target && !_registryFactories.ContainsKey(instruction))
                    {
                        MethodDefinition? method;
                        try { method = target.Resolve(); }
                        catch (AssemblyResolutionException) { continue; }
                        if (method is not null)
                        {
                            NoteMethodDepthContext(target, method, caller,
                                dispatchSlot: (instruction.OpCode.Code is Code.Callvirt or Code.Ldvirtftn)
                                    && !IsPrimitiveConstrainedCall(instruction, caller),
                                allocateReceiver: instruction.OpCode.Code == Code.Newobj);
                            if (_cutMethods.Contains(method)) continue;
                        }
                        if (instruction.OpCode.Code is Code.Call or Code.Callvirt or Code.Ldftn or Code.Ldvirtftn)
                        {
                            NoteExecutableReflectionRoute(target);
                            NoteGenericFactoryDepths(target, caller);
                        }
                    }
                    else if (instruction.Operand is TypeReference type)
                    {
                        NoteCallerTypeDepth(type, caller);
                        if (instruction.OpCode.Code == Code.Ldtoken
                            && !PreservationReader.IsFrameworkAssemblyName(caller.Method.Module.Assembly.Name.Name)
                            && Resolve(SubstituteCallerType(type, caller)) is { } named
                            && PreservationReader.IsFrameworkAssemblyName(named.Module.Assembly.Name.Name))
                            _attributeDepthFrameworkTypes.Add(named);
                        if (instruction.OpCode.Code is Code.Box or Code.Initobj)
                        {
                            var boxed = SubstituteClosedApplicationType(type, caller.Method.DeclaringType, caller.DeclaringInstance);
                            if (boxed is GenericInstanceType nullable
                                && nullable.ElementType.FullName == "System.Nullable`1") boxed = nullable.GenericArguments[0];
                            if (Resolve(boxed) is { } owner && (instruction.OpCode.Code == Code.Box || IsAsyncStateMachine(owner)))
                            {
                                int[] arguments = boxed is GenericInstanceType instance
                                    ? instance.GenericArguments.Select(argument => CallerTypeDepth(argument, caller)).ToArray()
                                    : Array.Empty<int>();
                                if (arguments.All(depth => depth >= 0)) NoteAllocatedTypeDepths(boxed, owner, arguments, caller.ReflectionOnly);
                            }
                        }
                    }
                    else if (instruction.Operand is FieldReference field)
                    {
                        NoteCallerTypeDepth(field.DeclaringType, caller);
                        NoteCallerTypeDepth(field.FieldType, caller);
                        TypeReference ownerType = SubstituteClosedApplicationType(field.DeclaringType,
                            caller.Method.DeclaringType, caller.DeclaringInstance);
                        if (Resolve(ownerType) is { } owner && field.Resolve() is { IsStatic: true })
                        {
                            int[] arguments = field.DeclaringType is GenericInstanceType instance
                                ? instance.GenericArguments.Select(argument => CallerTypeDepth(argument, caller)).ToArray()
                                : Array.Empty<int>();
                            if (arguments.All(depth => depth >= 0)) NoteTypeInitializerDepths(ownerType, owner, arguments, caller.ReflectionOnly);
                        }
                    }
                }
            }
        }
        finally
        {
            _depthSummaryCaller = null;
        }
    }

    private bool SameMethodArgumentTypes(TypeReference[] previous, TypeReference[]? current)
    {
        if (previous.Length != (current?.Length ?? 0)) return false;
        for (int i = 0; i < previous.Length; i++)
            if (!SameReflectedFieldType(previous[i], current![i])) return false;
        return true;
    }

    private static TypeReference SubstituteCallerType(TypeReference reference, MethodDepthContext? caller)
    {
        if (caller is null) return reference;
        return SubstituteClosedApplicationType(reference, caller.Method.DeclaringType, caller.DeclaringInstance,
            caller.Method, caller.MethodTypes);
    }

    // Generic Activator selects only the public parameterless constructor. Its
    // data closure does not make other constructors or getters executable.
    private void NoteGenericFactoryDepths(MethodReference reference, MethodDepthContext caller)
    {
        if (reference is not GenericInstanceMethod factory || factory.GenericArguments.Count != 1
            || factory.HasThis || factory.Parameters.Count != 0 || factory.Name != "CreateInstance"
            || factory.DeclaringType.FullName != "System.Activator") return;
        TypeReference selected = SubstituteCallerType(factory.GenericArguments[0], caller);
        if (selected.ContainsGenericParameter || Resolve(selected) is not { } owner
            || owner.IsAbstract || owner.IsInterface) return;
        foreach (var method in owner.Methods)
            if (method.IsPublic && method.IsConstructor && !method.IsStatic && method.Parameters.Count == 0)
            {
                MarkMethod(method, false);
                var constructor = new MethodReference(method.Name, method.ReturnType, selected) { HasThis = true };
                NoteMethodDepthContext(constructor, method, caller, allocateReceiver: true);
            }
    }

    // Preserved bodies can arm retention without executing. Only executable
    // descriptors supply depth roots for those retained reflection surfaces.
    private void NoteExecutableReflectionRoute(MethodReference target)
    {
        string owner = target.DeclaringType.FullName;
        if (PreservationReader.ReadsReflectedAttributes(owner, target.Name))
            _depthReadsReflectionAttributes = true;
        if (!_depthRunsReflectedMethods && (PreservationReader.RunsReflectedMethod(owner, target.Name)
            || PreservationReader.BindsReflectedMethod(owner, target.Name)))
        {
            _depthRunsReflectedMethods = true;
            foreach (var type in ReflectionApplicationTypes().ToList()) KeepReflectedMethods(type);
            foreach (var type in _typeTokenLibraryTypes.ToList()) KeepTypeTokenLibrarySurface(type);
        }
        if (!_depthConstructsFromRuntimeType && target.MetadataToken.TokenType == TokenType.MemberRef
            && PreservationReader.ConstructsFromRuntimeType(owner, target.Name))
        {
            _depthConstructsFromRuntimeType = true;
            foreach (var type in _constructedTypes.ToList())
            {
                if (type.Module == _assemblies[0].Assembly.MainModule || !type.IsAbstract)
                    KeepInstanceConstructors(type);
                if (!type.IsAbstract) NoteAllocatedTypeDepths(type, type);
                if (type.Module != _assemblies[0].Assembly.MainModule && !type.IsAbstract)
                    foreach (var method in type.Methods)
                        if (method.HasBody && !method.IsStatic && !method.IsConstructor
                            && !method.HasGenericParameters && method.Parameters.Count == 0
                            && method.ReturnType.FullName == "System.Void") MarkMethod(method);
            }
            foreach (var instance in _signatureApplicationInstances.ToList())
                if (Resolve(instance) is { } type && _constructedTypes.Contains(type))
                    NoteAllocatedTypeDepths(instance, type);
            foreach (var type in _constructionSurface.ToList())
                KeepConstructionAccessors(type);
        }
        if (!_depthReadsReflectedFields && PreservationReader.ReadsReflectedField(owner, target.Name))
        {
            _depthReadsReflectedFields = true;
            foreach (var type in _types.ToList()) KeepApplicationShapes(type);
            foreach (var instance in _signatureApplicationInstances.ToList())
                if (Resolve(instance) is { } type) KeepApplicationShapes(type, instance);
        }
        if (!_depthBindsNamedConstructors && owner == "System.Delegate" && target.Name == "CreateDelegate"
            && target.Parameters.Count >= 3 && target.Parameters[2].ParameterType.FullName == "System.String")
        {
            _depthBindsNamedConstructors = true;
            foreach (var type in _namedConstructorOwners.ToList())
                foreach (var method in type.Methods)
                    if (method.IsConstructor) MarkMethod(method);
        }
        if (!_depthInitializesArrays && owner == "System.Array" && target.Name == "Initialize"
            && target.Parameters.Count == 0)
        {
            _depthInitializesArrays = true;
            foreach (var assembly in _assemblies.Where(assembly => !assembly.Copy))
                foreach (var type in AllTypes(assembly.Assembly.MainModule.Types))
                    if (type.IsValueType)
                        foreach (var method in type.Methods)
                            if (method.IsConstructor && !method.IsStatic && method.Parameters.Count == 0) MarkMethod(method);
        }
    }

    private bool _depthReadsReflectionAttributes;
    private readonly HashSet<TypeDefinition> _attributeDepthFrameworkTypes = new();
    private readonly HashSet<Mono.Cecil.CustomAttribute> _depthAttributeRows = new();

    // Attribute rows preserve original bodies without executing them. The live
    // attribute route promotes the same user elements and named setters as emission.
    private bool NoteReflectionAttributeDepthRoots()
    {
        if (!_depthReadsReflectionAttributes) return false;
        int count = _types.Count;
        foreach (var assembly in _assemblies) NoteAttributeDepthRoots(assembly.Assembly);
        foreach (var type in _types.ToList())
        {
            if (PreservationReader.IsFrameworkAssemblyName(type.Module.Assembly.Name.Name)) continue;
            NoteAttributeDepthRoots(type);
            foreach (var field in type.Fields) NoteAttributeDepthRoots(field);
            foreach (var method in type.Methods)
            {
                NoteAttributeDepthRoots(method);
                NoteAttributeDepthRoots(method.MethodReturnType);
                foreach (var parameter in method.Parameters) NoteAttributeDepthRoots(parameter);
            }
            foreach (var property in type.Properties) NoteAttributeDepthRoots(property);
        }
        return count != _types.Count;
    }

    private void NoteAttributeDepthRoots(Mono.Cecil.ICustomAttributeProvider provider)
    {
        if (!provider.HasCustomAttributes) return;
        foreach (var attribute in provider.CustomAttributes)
        {
            var owner = Resolve(attribute.AttributeType);
            if (owner is null || PreservationReader.IsFrameworkAssemblyName(owner.Module.Assembly.Name.Name)
                    && !_attributeDepthFrameworkTypes.Contains(owner)
                || !_depthAttributeRows.Add(attribute)) continue;
            MarkMethod(attribute.Constructor);
            foreach (var argument in attribute.Properties)
            {
                TypeReference current = attribute.AttributeType;
                var path = new HashSet<TypeDefinition>();
                while (Resolve(current) is { } declaring && path.Add(declaring))
                {
                    var setter = declaring.Methods.FirstOrDefault(method =>
                        !method.IsStatic && method.Name == "set_" + argument.Name);
                    if (setter is not null)
                    {
                        var instance = current as GenericInstanceType;
                        var reference = new MethodReference(setter.Name,
                            SubstituteClosedApplicationType(setter.ReturnType, declaring, instance), current)
                        {
                            HasThis = setter.HasThis,
                            ExplicitThis = setter.ExplicitThis,
                            CallingConvention = setter.CallingConvention,
                        };
                        foreach (var parameter in setter.Parameters)
                            reference.Parameters.Add(new ParameterDefinition(
                                SubstituteClosedApplicationType(parameter.ParameterType, declaring, instance)));
                        MarkMethod(reference);
                        break;
                    }
                    if (declaring.BaseType is null) break;
                    current = SubstituteClosedApplicationType(declaring.BaseType, declaring, current as GenericInstanceType);
                }
            }
        }
    }

    // Emission closes app member signatures once over its existing classes.
    // Newly named contexts carry metadata and armed routes, not another method walk.
    private void KeepClosedApplicationSignatures()
    {
        PrepareClosedApplicationBases();
        // Shape and armed field discovery precede the fixed member snapshot.
        foreach (var instance in _signatureApplicationInstances.ToList())
            if (Resolve(instance) is { } owner) KeepApplicationShapes(owner, instance);
        foreach (var instance in _signatureApplicationInstances.ToList())
        {
            if (Resolve(instance) is not { } owner) continue;
            foreach (var method in owner.Methods)
            {
                if (!_methods.Contains(method) && !_signatureMethods.Contains(method)) continue;
                MarkSignatureType(SubstituteClosedApplicationType(method.ReturnType, owner, instance));
                foreach (var parameter in method.Parameters)
                    MarkSignatureType(SubstituteClosedApplicationType(parameter.ParameterType, owner, instance));
            }
        }
    }

    // Shape completion materializes substituted base types before emission's
    // member snapshot. Each valid base chain visits a definition only once.
    private void PrepareClosedApplicationBases()
    {
        foreach (var instance in _signatureApplicationInstances.ToList())
        {
            TypeReference current = instance;
            var seen = new HashSet<TypeDefinition>();
            while (Resolve(current) is { } owner && owner.Module == _assemblies[0].Assembly.MainModule
                && seen.Add(owner) && owner.BaseType is not null)
            {
                current = SubstituteClosedApplicationType(owner.BaseType, owner, current as GenericInstanceType);
                MarkSignatureType(current);
            }
        }
    }

    // Member queries observe declarations without running their bodies. Keep the
    // original IL until the reachability fixpoint so a later invoke can promote it.
    private void KeepApplicationMetadata(TypeDefinition type)
    {
        if (!IsStripped(type) || !_applicationMetadata.Add(type)) return;
        foreach (var method in type.Methods) KeepMethodSignature(method);
        foreach (var property in type.Properties)
        {
            _properties.Add(property);
            MarkSignatureType(property.PropertyType);
            foreach (var parameter in property.Parameters) MarkSignatureType(parameter.ParameterType);
            MarkAttributes(property);
            if (property.GetMethod is not null) KeepMethodSignature(property.GetMethod);
            if (property.SetMethod is not null) KeepMethodSignature(property.SetMethod);
            foreach (var method in property.OtherMethods) KeepMethodSignature(method);
        }
        foreach (var item in type.Events)
        {
            _events.Add(item);
            MarkSignatureType(item.EventType);
            MarkAttributes(item);
            if (item.AddMethod is not null) KeepMethodSignature(item.AddMethod);
            if (item.RemoveMethod is not null) KeepMethodSignature(item.RemoveMethod);
            if (item.InvokeMethod is not null) KeepMethodSignature(item.InvokeMethod);
            foreach (var method in item.OtherMethods) KeepMethodSignature(method);
        }
        if (_readsReflectedFields) KeepApplicationShapes(type);
    }

    private void ArmReflectedFieldReads()
    {
        if (_readsReflectedFields) return;
        _readsReflectedFields = true;
        foreach (var type in _types.ToList()) KeepApplicationShapes(type);
        foreach (var instance in _signatureApplicationInstances.ToList())
            if (Resolve(instance) is { } owner) KeepApplicationShapes(owner, instance);
    }

    // Shape discovery carries metadata context. Only armed field reads upgrade
    // the value types they box, whose dispatch must survive stubbing.
    private sealed class ApplicationShapeWalk
    {
        internal int Depth;
        internal readonly Dictionary<TypeDefinition, List<GenericInstanceType?>> Contexts = new();
        internal readonly HashSet<TypeDefinition> Further = new();
    }

    private void KeepApplicationShapes(TypeDefinition owner, GenericInstanceType? instance = null)
    {
        var walk = new ApplicationShapeWalk { Depth = _knownGenericDepth };
        KeepApplicationShapes(owner, instance, walk);
    }

    private void KeepApplicationShapes(TypeDefinition owner, GenericInstanceType? instance,
        ApplicationShapeWalk walk)
    {
        if (owner.Module != _assemblies[0].Assembly.MainModule
            || _suppressDefaultSeeds.Contains(owner)) return;
        if (!walk.Contexts.TryGetValue(owner, out var contexts))
            walk.Contexts.Add(owner, contexts = new());
        foreach (var previous in contexts)
            if (SameReflectedFieldType(previous, instance)) return;
        // Distinct closed contexts may cycle at the same depth. Match emission's
        // minted-context budget and one further walk without growing forever.
        if (ReflectedFieldTypeDepth(instance) > walk.Depth
            || contexts.Count > PreservationReader.ReflectionRouteMintedWalksPerDefinition)
            if (!walk.Further.Add(owner)) return;
        contexts.Add(instance);
        if (instance is not null)
        {
            NoteApplicationInstance(instance, shapeOwner: true);
            foreach (var argument in instance.GenericArguments) KeepApplicationShapeContexts(argument, walk);
        }
        foreach (var implementation in owner.Interfaces)
            KeepApplicationShapeContexts(SubstituteClosedApplicationType(implementation.InterfaceType, owner, instance), walk);
        foreach (var field in owner.Fields)
        {
            if ((!_readsReflectedFields && !_constructsFromRuntimeType) || field.IsLiteral) continue;
            TypeReference reference = SubstituteClosedApplicationType(field.FieldType, owner, instance);
            KeepApplicationShapeContexts(reference, walk);
            if (!_readsReflectedFields) continue;
            if (reference is GenericInstanceType nullable
                && nullable.ElementType.FullName == "System.Nullable`1")
                reference = nullable.GenericArguments[0];
            if (reference.ContainsGenericParameter
                || reference is TypeSpecification && reference is not GenericInstanceType) continue;
            if (Resolve(reference) is not { } type) continue;
            if (type.IsValueType && IsStripped(type)
                && !IsProtected(type.Module.Assembly.Name.Name)
                && !HasAttribute(type, "System.Runtime.CompilerServices.IsByRefLikeAttribute"))
            {
                MarkType(reference);
                if (_depthReadsReflectedFields) NoteAllocatedTypeDepths(reference, type);
            }
        }
        if (owner.BaseType is not null)
        {
            TypeReference parent = SubstituteClosedApplicationType(owner.BaseType, owner, instance);
            if (Resolve(parent) is { } definition)
                KeepApplicationShapes(definition, parent as GenericInstanceType, walk);
        }
    }

    // Shapes also materialize element and generic argument types. Carry their
    // owner context without promoting their runtime bodies.
    private void KeepApplicationShapeContexts(TypeReference reference, ApplicationShapeWalk walk)
    {
        if (reference.ContainsGenericParameter) return;
        if (reference is ArrayType array)
        {
            KeepApplicationShapeContexts(array.ElementType, walk);
            return;
        }
        if (reference is not GenericInstanceType generic) return;
        foreach (var argument in generic.GenericArguments)
            KeepApplicationShapeContexts(argument, walk);
        if (Resolve(generic) is { } owner)
            KeepApplicationShapes(owner, generic, walk);
    }

    private static int ReflectedFieldTypeDepth(TypeReference? reference)
    {
        if (reference is GenericInstanceType generic)
        {
            int depth = 0;
            foreach (var argument in generic.GenericArguments)
                depth = Math.Max(depth, ReflectedFieldTypeDepth(argument));
            return depth + 1;
        }
        if (reference is ArrayType or ByReferenceType or PointerType)
            return 1 + ReflectedFieldTypeDepth(((TypeSpecification)reference).ElementType);
        return 0;
    }

    private bool SameReflectedFieldType(TypeReference? left, TypeReference? right)
    {
        if (ReferenceEquals(left, right)) return true;
        if (left is null || right is null) return false;
        if (left is GenericInstanceType a)
        {
            if (right is not GenericInstanceType b || a.GenericArguments.Count != b.GenericArguments.Count
                || !SameReflectedFieldType(a.ElementType, b.ElementType)) return false;
            for (int i = 0; i < a.GenericArguments.Count; i++)
                if (!SameReflectedFieldType(a.GenericArguments[i], b.GenericArguments[i])) return false;
            return true;
        }
        if (left is ArrayType array)
        {
            if (right is not ArrayType other || array.IsVector != other.IsVector || array.Rank != other.Rank)
                return false;
            for (int i = 0; i < array.Dimensions.Count; i++)
                if (array.Dimensions[i].LowerBound != other.Dimensions[i].LowerBound
                    || array.Dimensions[i].UpperBound != other.Dimensions[i].UpperBound) return false;
            return SameReflectedFieldType(array.ElementType, other.ElementType);
        }
        if (left is TypeSpecification specification)
        {
            if (right is not TypeSpecification other || left.MetadataType != right.MetadataType) return false;
            if (left is IModifierType modifier
                && (right is not IModifierType otherModifier
                    || !SameReflectedFieldType(modifier.ModifierType, otherModifier.ModifierType))) return false;
            return SameReflectedFieldType(specification.ElementType, other.ElementType);
        }
        if (right is TypeSpecification) return false;
        var definition = Resolve(left);
        var otherDefinition = Resolve(right);
        if (definition is not null || otherDefinition is not null) return definition == otherDefinition;
        return left.FullName == right.FullName && left.Scope?.ToString() == right.Scope?.ToString();
    }

    private static TypeReference SubstituteClosedApplicationType(TypeReference reference,
        TypeDefinition owner, GenericInstanceType? instance, MethodDefinition? method = null,
        TypeReference[]? methodTypes = null)
    {
        if (instance is null && method is null) return reference;
        if (reference is GenericParameter methodParameter && methodParameter.Owner == method
            && methodTypes is not null && methodParameter.Position < methodTypes.Length)
            return methodTypes[methodParameter.Position];
        if (reference is GenericParameter parameter && parameter.Owner == owner
            && instance is not null && parameter.Position < instance.GenericArguments.Count)
            return instance.GenericArguments[parameter.Position];
        if (reference is ArrayType array && array.ContainsGenericParameter)
        {
            var substitutedArray = new ArrayType(SubstituteClosedApplicationType(array.ElementType, owner, instance, method, methodTypes), array.Rank);
            for (int i = 0; i < array.Dimensions.Count; i++)
                substitutedArray.Dimensions[i] = array.Dimensions[i];
            return substitutedArray;
        }
        if (!reference.ContainsGenericParameter) return reference;
        if (reference is ByReferenceType byReference)
            return new ByReferenceType(SubstituteClosedApplicationType(byReference.ElementType, owner, instance, method, methodTypes));
        if (reference is PointerType pointer)
            return new PointerType(SubstituteClosedApplicationType(pointer.ElementType, owner, instance, method, methodTypes));
        if (reference is OptionalModifierType optional)
            return new OptionalModifierType(SubstituteClosedApplicationType(optional.ModifierType, owner, instance, method, methodTypes),
                SubstituteClosedApplicationType(optional.ElementType, owner, instance, method, methodTypes));
        if (reference is RequiredModifierType required)
            return new RequiredModifierType(SubstituteClosedApplicationType(required.ModifierType, owner, instance, method, methodTypes),
                SubstituteClosedApplicationType(required.ElementType, owner, instance, method, methodTypes));
        if (reference is not GenericInstanceType generic)
            return reference;
        var substituted = new GenericInstanceType(generic.ElementType);
        foreach (var argument in generic.GenericArguments)
            substituted.GenericArguments.Add(SubstituteClosedApplicationType(argument, owner, instance, method, methodTypes));
        return substituted;
    }

    private void KeepMethodSignature(MethodDefinition method)
    {
        // Eager startup can execute a retained app initializer. Suppressed
        // initializers need a real root rather than a throwing metadata stub.
        if (method.IsConstructor && method.IsStatic && _suppressDefaultSeeds.Contains(method.DeclaringType)) return;
        if (!_signatureMethods.Add(method)) return;
        MarkSignatureType(method.ReturnType);
        foreach (var parameter in method.Parameters) MarkSignatureType(parameter.ParameterType);
        MarkAttributes(method, signatureOnly: true);
        MarkSecurity(method);
        MarkAttributes(method.MethodReturnType);
        MarkMarshal(method.MethodReturnType);
        foreach (var parameter in method.Parameters)
        {
            MarkAttributes(parameter);
            MarkMarshal(parameter);
        }
        foreach (var parameter in method.GenericParameters) MarkSignatureParameter(parameter);
        foreach (var overridden in method.Overrides)
        {
            MarkSignatureType(overridden.DeclaringType);
            MarkSignatureType(overridden.ReturnType);
            foreach (var parameter in overridden.Parameters) MarkSignatureType(parameter.ParameterType);
        }
    }

    // Signature dependencies need layouts and declarations, not virtual bodies,
    // generic factory data or constructor dependencies. MarkType upgrades them
    // independently if ordinary IL or an armed reflection route uses the type.
    private void MarkSignatureType(TypeReference? reference)
    {
        if (reference is null) return;
        if (reference is GenericParameter parameter)
        {
            MarkSignatureParameter(parameter);
            return;
        }
        if (reference is GenericInstanceType generic)
        {
            foreach (var argument in generic.GenericArguments) MarkSignatureType(argument);
            // Armed runtime routes need the closed argument context that the
            // definition's type parameters do not retain.
            if (NoteApplicationInstance(generic) && Resolve(generic) is { } owner)
            {
                if (_readsReflectedFields) KeepApplicationShapes(owner, generic);
                if ((_constructsFromRuntimeType || _runsReflectedMethods)
                    && !_suppressDefaultSeeds.Contains(owner)) MarkType(generic);
            }
        }
        if (reference is IModifierType modifier) MarkSignatureType(modifier.ModifierType);
        if (reference is FunctionPointerType pointer)
        {
            MarkSignatureType(pointer.ReturnType);
            foreach (var parameterType in pointer.Parameters) MarkSignatureType(parameterType.ParameterType);
            return;
        }
        if (reference is TypeSpecification specification)
        {
            MarkSignatureType(specification.ElementType);
            return;
        }
        var type = Resolve(reference);
        if (type is null || !IsStripped(type) || !_signatureTypes.Add(type)) return;
        _types.Add(type);
        MarkSignatureType(type.DeclaringType);
        MarkSignatureType(type.BaseType);
        MarkAttributes(type);
        MarkSecurity(type);
        foreach (var parameterType in type.GenericParameters) MarkSignatureParameter(parameterType);
        foreach (var implementation in type.Interfaces)
        {
            MarkSignatureType(implementation.InterfaceType);
            MarkAttributes(implementation);
        }
        foreach (var field in type.Fields)
        {
            _fields.Add(field);
            MarkSignatureType(field.FieldType);
            MarkAttributes(field);
            MarkMarshal(field);
        }
        if (type.Module == _assemblies[0].Assembly.MainModule)
        {
            KeepApplicationMetadata(type);
            // Decoding a retained member signature can materialize a closed app
            // type after the runtime routes have already walked their roots.
            if (_runsReflectedMethods && !_suppressDefaultSeeds.Contains(type))
            {
                MarkType(type);
                KeepReflectedMethods(type);
            }
            if (_constructsFromRuntimeType && !_suppressDefaultSeeds.Contains(type))
                KeepRuntimeConstructionSurface(type);
        }
        else
            foreach (var method in type.Methods)
                if (method.IsVirtual || method.HasOverrides || type.IsInterface || IsDelegate(type))
                    KeepMethodSignature(method);
    }

    private void MarkSignatureParameter(GenericParameter parameter)
    {
        if (!_signatureParameters.Add(parameter)) return;
        MarkAttributes(parameter);
        foreach (var constraint in parameter.Constraints)
        {
            MarkSignatureType(constraint.ConstraintType);
            MarkAttributes(constraint);
        }
    }

    // The constructor route opens declared data types recursively, including
    // libraries. A base contributes inherited data without rooting unused ctors.
    private void KeepRuntimeConstructionSurface(TypeDefinition type)
    {
        if (!IsStripped(type) || IsProtected(type.Module.Assembly.Name.Name)
            || !_constructedTypes.Add(type)) return;
        MarkType(type);
        bool application = type.Module == _assemblies[0].Assembly.MainModule;
        if (application || !type.IsAbstract) KeepInstanceConstructors(type, _depthConstructsFromRuntimeType);
        KeepConstructionData(type);
        if (!application && !type.IsAbstract)
            for (TypeDefinition? current = type; current is not null && IsStripped(current)
                    && !IsProtected(current.Module.Assembly.Name.Name);
                current = current.BaseType is null ? null : Resolve(current.BaseType))
                foreach (var method in current.Methods)
                    if (method.HasBody && !method.IsStatic && !method.IsConstructor
                        && !method.HasGenericParameters && method.Parameters.Count == 0
                        && method.ReturnType.FullName == "System.Void")
                        MarkMethod(method, _depthConstructsFromRuntimeType);
    }

    private void KeepConstructionData(TypeDefinition type)
    {
        if (!IsStripped(type) || IsProtected(type.Module.Assembly.Name.Name)
            || !_constructionSurface.Add(type)) return;
        MarkType(type);
        if (type.BaseType is GenericInstanceType generic)
            foreach (var argument in generic.GenericArguments) MarkConstructionType(argument);
        if (type.BaseType is not null && Resolve(type.BaseType) is { } parent)
            KeepConstructionData(parent);
        foreach (var field in type.Fields)
            if (!field.IsLiteral) MarkConstructionType(field.FieldType);
        foreach (var property in type.Properties)
        {
            MarkConstructionType(property.PropertyType);
        }
        KeepConstructionAccessors(type);
        foreach (var method in type.Methods)
            if (method.IsConstructor && !method.IsStatic)
                foreach (var parameter in method.Parameters) MarkConstructionType(parameter.ParameterType);
    }

    private void KeepConstructionAccessors(TypeDefinition type)
    {
        if (type.Module == _assemblies[0].Assembly.MainModule) return;
        foreach (var property in type.Properties)
            if (property.GetMethod is { IsStatic: false } || property.SetMethod is { IsStatic: false })
                MarkProperty(property, true, true, _depthConstructsFromRuntimeType);
    }

    private void MarkConstructionType(TypeReference? reference)
    {
        if (reference is null) return;
        MarkType(reference);
        if (reference is FunctionPointerType) return;
        if (reference is GenericInstanceType generic)
            foreach (var argument in generic.GenericArguments) MarkConstructionType(argument);
        if (reference is TypeSpecification specification)
        {
            MarkConstructionType(specification.ElementType);
            return;
        }
        if (reference is not GenericParameter && Resolve(reference) is { } type)
            KeepRuntimeConstructionSurface(type);
    }

    private bool KeepsMethod(MethodDefinition method) =>
        _methods.Contains(method) || _signatureMethods.Contains(method);

    private static void StubBody(MethodDefinition method)
    {
        method.Body = new MethodBody(method) { MaxStackSize = 1 };
        method.Body.Instructions.Add(CecilInstruction.Create(OpCodes.Ldnull));
        method.Body.Instructions.Add(CecilInstruction.Create(OpCodes.Throw));
    }
}

using Mono.Cecil;

namespace Dn2Cpp;

internal sealed partial class AssemblyDiet
{
    private readonly record struct ModelSource(TypeDefinition Owner, MethodDefinition? Member = null,
        MethodDefinition? Trigger = null);

    private sealed class OriginalModel
    {
        internal required TypeDefinition Type;
        internal bool Formed, Shape, MembersRequested, Members, PreserveSources;
        internal readonly HashSet<ModelSource> Sources = new();
    }

    private sealed class OriginalModelMethodIndex
    {
        internal readonly Dictionary<string, List<MethodDefinition>> Named = new(StringComparer.Ordinal);
        internal readonly Dictionary<string, List<MethodDefinition>> VirtualNamed = new(StringComparer.Ordinal);
        internal readonly List<MethodDefinition> Overrides = new();
    }

    private readonly Dictionary<TypeDefinition, OriginalModel> _originalModels = new();
    private readonly Dictionary<TypeDefinition, OriginalModelMethodIndex> _originalModelMethodIndexes = new();
    private readonly Queue<OriginalModel> _modelPending = new();
    private readonly HashSet<OriginalModel> _modelQueued = new();
    private readonly HashSet<(TypeReference Reference, ModelSource? Source)> _modelReferences = new();
    private readonly Dictionary<GenericParameter, HashSet<ModelSource?>> _modelParameterReferences = new();
    private readonly Dictionary<ProjectionNode, HashSet<ModelSource?>> _modelProjectionReferences = new();
    private readonly Dictionary<GenericParameter, HashSet<OriginalModel>> _modelBindingWaiters = new();
    private readonly Dictionary<ProjectionNode, HashSet<OriginalModel>> _modelProjectionWaiters = new();
    private readonly HashSet<(OriginalModel Model, MethodDefinition Method, MethodDefinition? Trigger)> _modelSignatures = new();
    private readonly HashSet<TypeDefinition> _modelTaskBackingSources = new();
    private int _observingOriginalModel;

    private OriginalModel OriginalModelFor(TypeDefinition type)
    {
        if (_originalModels.TryGetValue(type, out var model)) return model;
        Spend();
        model = new OriginalModel { Type = type };
        _originalModels.Add(type, model);
        QueueOriginalModel(model);
        return model;
    }

    private void QueueOriginalModel(OriginalModel model)
    {
        if (!_modelQueued.Add(model)) return;
        Spend();
        _modelPending.Enqueue(model);
    }

    private void InitializeOriginalModels()
    {
        foreach (var assembly in _assemblies)
            foreach (var type in AllTypes(assembly.Assembly.MainModule.Types))
            {
                Spend();
                if (!type.HasGenericParameters && (type.Name != "<Module>" || type.HasMethods || type.HasFields))
                {
                    var model = OriginalModelFor(type);
                    model.MembersRequested = true;
                }
            }
    }

    private void ObserveModelDefinition(TypeDefinition type)
    {
        if (_modelActivation == ModelActivation.Dormant) return;
        OriginalModelFor(type);
    }

    private void ObserveModelReference(TypeReference? reference, ModelSource? source = null)
    {
        if (_modelActivation == ModelActivation.Dormant) return;
        if (reference is null || !_modelReferences.Add((reference, source))) return;
        Spend();
        if (reference is GenericParameter parameter)
        {
            if (!_modelParameterReferences.TryGetValue(parameter, out var sources))
                _modelParameterReferences.Add(parameter, sources = new());
            sources.Add(source);
            if (_parameters.TryGetValue(parameter, out var node))
                foreach (var bound in node.FormationBound.ToArray()) ObserveModelReference(bound, source);
            return;
        }
        if (reference is ProjectedTypeReference projected)
        {
            if (!_modelProjectionReferences.TryGetValue(projected.Node, out var sources))
                _modelProjectionReferences.Add(projected.Node, sources = new());
            sources.Add(source);
            foreach (var target in projected.Node.Targets.ToArray()) ObserveModelReference(target, source);
            return;
        }
        if (reference is GenericInstanceType generic)
        {
            if (Resolve(generic) is { } definition)
            {
                _observingOriginalModel++;
                try
                {
                    RegisterModelAliases(definition.GenericParameters, generic.ElementType.GenericParameters);
                    BindParameters(definition.GenericParameters, generic.GenericArguments);
                    // MemberRef signatures can own separate Cecil parameter rows
                    // for the same original generic parent.
                    if (generic.ElementType != definition)
                        BindParameters(generic.ElementType.GenericParameters, generic.GenericArguments);
                }
                finally { _observingOriginalModel--; }
                AddModelSource(OriginalModelFor(definition), source);
            }
            foreach (var argument in generic.GenericArguments) ObserveModelReference(argument, source);
            return;
        }
        if (reference is FunctionPointerType pointer)
        {
            ObserveModelReference(pointer.ReturnType, source);
            foreach (var argument in pointer.Parameters) ObserveModelReference(argument.ParameterType, source);
            return;
        }
        // Custom modifiers are metadata identities, not SigProvider formations.
        if (reference is TypeSpecification specification)
        {
            ObserveModelReference(specification.ElementType, source);
            return;
        }
        if (Resolve(reference) is { } type) AddModelSource(OriginalModelFor(type), source);
    }

    private void AddModelSource(OriginalModel model, ModelSource? source)
    {
        if (!model.Type.HasGenericParameters || source is not { } evidence || !model.Sources.Add(evidence)) return;
        Spend();
        if (model.PreserveSources) KeepModelSource(evidence);
    }

    private bool HasModelBinding(TypeReference reference, OriginalModel? waiter, HashSet<TypeReference> visiting)
    {
        if (!visiting.Add(reference)) return false;
        Spend();
        try
        {
            if (reference is GenericParameter parameter)
            {
                if (waiter is not null)
                {
                    if (!_modelBindingWaiters.TryGetValue(parameter, out var waiters))
                        _modelBindingWaiters.Add(parameter, waiters = new());
                    waiters.Add(waiter);
                }
                return _parameters.TryGetValue(parameter, out var node)
                    && node.FormationBound.Any(bound => HasModelBinding(bound, waiter, visiting));
            }
            if (reference is ProjectedTypeReference projected)
            {
                if (waiter is not null)
                {
                    if (!_modelProjectionWaiters.TryGetValue(projected.Node, out var waiters))
                        _modelProjectionWaiters.Add(projected.Node, waiters = new());
                    waiters.Add(waiter);
                }
                return projected.Node.Targets.Any(target => HasModelBinding(target, waiter, visiting));
            }
            if (reference is GenericInstanceType generic)
                return generic.GenericArguments.All(argument => HasModelBinding(argument, waiter, visiting));
            if (reference is FunctionPointerType pointer)
                return HasModelBinding(pointer.ReturnType, waiter, visiting)
                    && pointer.Parameters.All(parameter => HasModelBinding(parameter.ParameterType, waiter, visiting));
            if (reference is TypeSpecification specification)
                return HasModelBinding(specification.ElementType, waiter, visiting);
            return true;
        }
        finally { visiting.Remove(reference); }
    }

    private bool HasModelBindings(TypeDefinition type, OriginalModel? waiter = null)
    {
        bool bound = true;
        foreach (var parameter in type.GenericParameters)
            bound &= HasModelBinding(parameter, waiter, new());
        return bound;
    }

    private void ModelBindingChanged(GenericParameter parameter, TypeReference argument)
    {
        if (_modelParameterReferences.TryGetValue(parameter, out var sources))
            foreach (var source in sources.ToArray()) ObserveModelReference(argument, source);
        if (_modelBindingWaiters.TryGetValue(parameter, out var waiters))
            foreach (var waiter in waiters.ToArray())
            {
                QueueOriginalModel(waiter);
                TryPromoteModelOwner(waiter.Type);
            }
    }

    private void ModelProjectionChanged(ProjectionNode projection, TypeReference target)
    {
        if (_modelProjectionReferences.TryGetValue(projection, out var sources))
            foreach (var source in sources.ToArray()) ObserveModelReference(target, source);
        if (_modelProjectionWaiters.TryGetValue(projection, out var waiters))
            foreach (var waiter in waiters.ToArray())
            {
                QueueOriginalModel(waiter);
                TryPromoteModelOwner(waiter.Type);
            }
    }

    private bool ModelMayBeNonPrimitive(TypeReference reference, OriginalModel model, HashSet<TypeReference> visiting)
    {
        if (!visiting.Add(reference)) return false;
        Spend();
        try
        {
            if (reference is GenericParameter parameter)
            {
                HasModelBinding(parameter, model, new());
                return _parameters.TryGetValue(parameter, out var node)
                    && node.FormationBound.Any(bound => ModelMayBeNonPrimitive(bound, model, visiting));
            }
            if (reference is ProjectedTypeReference projected)
            {
                HasModelBinding(projected, model, new());
                return projected.Node.Targets.Any(target => ModelMayBeNonPrimitive(target, model, visiting));
            }
            return reference is TypeSpecification || CoreIntrinsicTypeMaps.WellKnownPrimitiveCode(Name(reference)) is null;
        }
        finally { visiting.Remove(reference); }
    }

    private bool ModelHasIntrinsicShape(OriginalModel model)
    {
        var type = model.Type;
        if (!type.HasGenericParameters) return false;
        string name = type.Name;
        int tick = name.IndexOf('`');
        if (tick >= 0) name = name[..tick];
        string full = type.Namespace.Length == 0 ? name : type.Namespace + "." + name;
        if (CoreIntrinsicTypeMaps.IntrinsicGenericCppType(full, primitiveElement: false) is not null) return true;
        return CoreIntrinsicTypeMaps.IntrinsicGenericCppType(full, primitiveElement: true) is not null
            && !ModelMayBeNonPrimitive(type.GenericParameters[0], model, new());
    }

    private void ObserveImplicitTaskModel(OriginalModel model)
    {
        var type = model.Type;
        if (type.GenericParameters.Count != 1) return;
        bool backing = type.FullName is "System.Threading.Tasks.ValueTask`1" or "System.Threading.Tasks.TaskCompletionSource`1";
        // Opt-out choices can enable a shared contract declined by default.
        // The original candidate graph bounds possible backing without modeling it.
        bool custom = !backing && type.IsValueType
            && HasAttribute(type, "System.Runtime.CompilerServices.AsyncMethodBuilderAttribute")
            && OriginalAsyncTasks().ContainsKey(type);
        backing |= custom;
        if (!backing || _resolver.FindDefinition("System.Threading.Tasks.Task`1") is not { } task) return;
        if (custom) _modelTaskBackingSources.Add(type);
        var target = OriginalModelFor(task);
        _observingOriginalModel++;
        try { Bind(task.GenericParameters[0], type.GenericParameters[0]); }
        finally { _observingOriginalModel--; }
        AddModelSource(target, new(type));
    }

    private bool ApplyOriginalModels()
    {
        while (_modelPending.Count != 0)
        {
            var model = _modelPending.Dequeue();
            _modelQueued.Remove(model);
            Spend();
            var type = model.Type;
            if (!HasModelBindings(type, model)) continue;
            TryPromoteModelOwner(type);
            if (!model.Formed)
            {
                model.Formed = true;
                ObserveAttributeModel(type);
                ObserveArrayInitializerLayout(type);
                ObserveImplicitTaskModel(model);
                if (_policyTypes.TryGetValue(type, out var policy)
                    && (policy.Kind != PreservationReader.PreserveKind.None || policy.Fields.Count != 0
                        || policy.Methods.Count != 0 || policy.Properties.Count != 0 || policy.Events.Count != 0))
                    KeepOriginalModelSources(type);
            }
            if (ModelHasIntrinsicShape(model)) continue;
            if (!model.Shape)
            {
                model.Shape = true;
                ObserveModelReference(type.BaseType, new(type));
                foreach (var implementation in type.Interfaces) ObserveModelReference(implementation.InterfaceType, new(type));
            }
            if (!model.MembersRequested || model.Members) continue;
            model.Members = true;
            ObserveModelMembers(model);
        }
        return _work.Count != 0;
    }

    private void RequestModelMembers(TypeDefinition type, ModelSource source)
    {
        var model = OriginalModelFor(type);
        AddModelSource(model, source);
        if (model.MembersRequested) return;
        model.MembersRequested = true;
        QueueOriginalModel(model);
    }

    private TypeReference[] OriginalMethodSignature(MethodDefinition method)
    {
        var node = Node(method) ?? throw new InvalidOperationException("ILDiet method is outside its load set");
        if (node.Signature is not null) return node.Signature;
        Spend();
        _signatureScans++;
        var signature = new List<TypeReference> { method.ReturnType };
        foreach (var parameter in method.Parameters) signature.Add(parameter.ParameterType);
        node.Signature = signature.ToArray();
        return node.Signature;
    }

    private void ObserveModelSignature(OriginalModel model, MethodDefinition method, MethodDefinition? trigger)
    {
        if (!_modelSignatures.Add((model, method, trigger))) return;
        Spend();
        foreach (var reference in OriginalMethodSignature(method))
            ObserveModelReference(reference, new(model.Type, method, trigger));
    }

    private OriginalModelMethodIndex OriginalModelMethodsFor(TypeDefinition owner)
    {
        Spend();
        if (_originalModelMethodIndexes.TryGetValue(owner, out var known)) return known;
        Spend();
        var index = new OriginalModelMethodIndex();
        // Index original row order using only flags and names. Signature asks
        // remain with the actual MethodImpl and virtual-slot consumers.
        foreach (var method in owner.Methods)
        {
            Spend();
            if (method.HasGenericParameters) continue;
            Add(index.Named, method);
            if (!method.IsVirtual) continue;
            Add(index.VirtualNamed, method);
            if (method.IsNewSlot) continue;
            Spend();
            index.Overrides.Add(method);
        }
        _originalModelMethodIndexes.Add(owner, index);
        return index;

        void Add(Dictionary<string, List<MethodDefinition>> names, MethodDefinition method)
        {
            if (!names.TryGetValue(method.Name, out var methods))
            {
                Spend();
                names.Add(method.Name, methods = new());
            }
            Spend();
            methods.Add(method);
        }
    }

    private IReadOnlyList<MethodDefinition> OriginalModelMethodCandidates(TypeDefinition owner, string name, bool virtualOnly)
    {
        var index = OriginalModelMethodsFor(owner);
        Spend();
        var names = virtualOnly ? index.VirtualNamed : index.Named;
        return names.TryGetValue(name, out var methods) ? methods : Array.Empty<MethodDefinition>();
    }

    private void ObserveModelMemberReference(OriginalModel model, MethodReference reference, MethodDefinition body)
    {
        if (reference is MethodDefinition) return;
        ObserveModelReference(reference.DeclaringType, new(model.Type, body));
        if (Resolve(reference.DeclaringType) is not { } owner) return;
        RequestModelMembers(owner, new(model.Type, body));
        ObserveModelReference(reference.ReturnType, new(model.Type, body));
        foreach (var parameter in reference.Parameters)
            ObserveModelReference(parameter.ParameterType, new(model.Type, body));
        if (ModelHasIntrinsicShape(OriginalModelFor(owner))) return;
        foreach (var candidate in OriginalModelMethodCandidates(owner, reference.Name, virtualOnly: false))
        {
            Spend();
            ObserveModelSignature(model, candidate, body);
        }
    }

    private void ObserveModelMembers(OriginalModel model)
    {
        var type = model.Type;
        if (!type.IsEnum)
        {
            ObserveModelMethodImplBodies(model);
            foreach (var body in type.Methods)
                foreach (var declaration in body.Overrides)
                {
                    Spend();
                    if (declaration.HasGenericParameters
                        || declaration.Name == "Finalize" && body.Name == "Finalize"
                            && declaration.DeclaringType.FullName == "System.Object") continue;
                    if (Resolve(declaration.DeclaringType) is null) continue;
                    ObserveModelMemberReference(model, declaration, body);
                }
        }
        if (type.IsInterface) return;
        if (type.BaseType is { } baseReference && Resolve(baseReference) is { } parent)
            RequestModelMembers(parent, new(type));
        foreach (var method in OriginalModelMethodsFor(type).Overrides)
        {
            Spend();
            if (method.Overrides.Any(declaration => Resolve(declaration.DeclaringType) is { IsInterface: false } owner
                    && owner != type && Ancestors(type).Contains(owner))) continue;
            foreach (var ancestor in Ancestors(type).Skip(1))
            {
                if (ModelHasIntrinsicShape(OriginalModelFor(ancestor))) break;
                foreach (var candidate in OriginalModelMethodCandidates(ancestor, method.Name, virtualOnly: true))
                {
                    Spend();
                    ObserveModelSignature(model, candidate, method);
                    ObserveModelSignature(model, method, candidate);
                }
            }
        }
    }

    private void ObserveModelMethodImplBodies(OriginalModel model)
    {
        var type = model.Type;
        var reader = System.Reflection.Metadata.PEReaderExtensions.GetMetadataReader(_byModule[type.Module].PE);
        var handle = (System.Reflection.Metadata.TypeDefinitionHandle)
            System.Reflection.Metadata.Ecma335.MetadataTokens.EntityHandle(type.MetadataToken.ToInt32());
        foreach (var implementationHandle in reader.GetTypeDefinition(handle).GetMethodImplementations())
        {
            Spend();
            var implementation = reader.GetMethodImplementation(implementationHandle);
            if (implementation.MethodBody.Kind != System.Reflection.Metadata.HandleKind.MemberReference) continue;
            if (type.Module.LookupToken(System.Reflection.Metadata.Ecma335.MetadataTokens.GetToken(
                    implementation.MethodDeclaration)) is not MethodReference declaration
                || declaration.HasGenericParameters || Resolve(declaration.DeclaringType) is null) continue;
            if (type.Module.LookupToken(System.Reflection.Metadata.Ecma335.MetadataTokens.GetToken(
                    implementation.MethodBody)) is not MethodReference reference
                || ResolveMember(reference) is not MethodDefinition body) continue;
            if (declaration.Name == "Finalize" && reference.Name == "Finalize"
                && declaration.DeclaringType.FullName == "System.Object") continue;
            ObserveModelMemberReference(model, reference, body);
            ObserveModelMemberReference(model, declaration, body);
        }
    }

    private void KeepOriginalModelSources(TypeDefinition type)
    {
        if (!type.HasGenericParameters || !_originalModels.TryGetValue(type, out var model) || model.PreserveSources) return;
        model.PreserveSources = true;
        Spend();
        foreach (var source in model.Sources.ToArray()) KeepModelSource(source);
    }

    private void KeepModelSource(ModelSource source)
    {
        KeepFormationType(source.Owner);
        if (_modelTaskBackingSources.Contains(source.Owner)) KeepFormationTaskContract(source.Owner);
        if (source.Member is not null) KeepFormationMethod(source.Member);
        if (source.Trigger is not null) KeepFormationMethod(source.Trigger);
        KeepOriginalModelSources(source.Owner);
    }
}

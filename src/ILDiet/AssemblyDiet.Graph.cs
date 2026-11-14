using Mono.Cecil;
using Mono.Cecil.Cil;
using MetadataTokens = System.Reflection.Metadata.Ecma335.MetadataTokens;

namespace Dn2Cpp;

internal sealed partial class AssemblyDiet
{
    [Flags]
    private enum TypeState : byte { None = 0, Metadata = 1, Runtime = 2, Members = 4, Explicit = 8 }
    [Flags]
    private enum MethodState : byte { None = 0, Metadata = 1, Body = 2 }
    [Flags]
    private enum Demand : ushort { None = 0, DefaultCtor = 1, Ctors = 2, Members = 4, MethodBodies = 8, AccessorBodies = 16, OpaqueData = 32, Runtime = 64, Boxed = 128, FieldRead = 256, Metadata = 512, DefaultCtorAnyVisibility = 1024, ArrayLayout = 2048, FrameworkAttributeNamed = 4096, PreservedUse = 8192, TypeWide = 16384, PassiveMetadata = 32768 }

    private sealed class TypeNode
    {
        internal required TypeDefinition Def;
        internal TypeState State, Applied;
        internal bool UnknownMembersApplied;
    }

    private sealed class MethodNode
    {
        internal required MethodDefinition Def;
        internal MethodState State, Applied;
        internal TypeReference[]? Signature;
        internal BodyDeps? Body;
        internal byte BodyScans;
    }

    private sealed class BodyDeps
    {
        internal required (object Operand, Code Op, Mono.Cecil.Cil.Instruction Instruction)[] Operands;
        internal required TypeReference[] Locals;
        internal required TypeReference[] Catches;
        internal ReflectionSummary? Reflection;
        internal bool ReflectionPrepared;
        internal readonly Dictionary<int, Mono.Cecil.MemberReference> Tokens = new();
    }

    private sealed class ParamNode
    {
        internal required GenericParameter Parameter;
        internal Demand Demands, Applied;
        internal readonly List<TypeReference> FormationBound = new();
        internal readonly HashSet<string> FormationBoundKeys = new(StringComparer.Ordinal);
        internal readonly List<GenericParameter> Aliases = new();
        internal readonly List<TypeReference> Bound = new();
        internal readonly HashSet<string> BoundKeys = new(StringComparer.Ordinal);
        internal readonly List<ProjectionNode> Projections = new();
    }

    private readonly struct Work
    {
        internal readonly TypeNode? Type;
        internal readonly MethodNode? Method;
        internal readonly ParamNode? Parameter;
        internal readonly TypeReference? Argument;
        internal readonly Demand Demands;
        internal readonly ProjectionNode? Projection;
        internal readonly TypeReference? ProjectionSource;
        internal Work(TypeNode type) { Type = type; }
        internal Work(MethodNode method) { Method = method; }
        internal Work(ParamNode parameter) { Parameter = parameter; }
        internal Work(TypeReference argument, Demand demands) { Argument = argument; Demands = demands; }
        internal Work(ProjectionNode projection) { Projection = projection; }
        internal Work(ProjectionNode projection, TypeReference source) { Projection = projection; ProjectionSource = source; }
    }

    private sealed class DietBudgetExceeded : Exception { }
    private readonly Dictionary<ModuleDefinition, TypeNode?[]> _typeNodes = new();
    private readonly Dictionary<ModuleDefinition, MethodNode?[]> _methodNodes = new();
    private readonly Dictionary<GenericParameter, ParamNode> _parameters = new();
    private readonly Queue<Work> _work = new();
    private bool _frozen;
    private long _workCount, _workBudget;
    private long _signatureScans, _bodyScans, _summaryMethods, _summaryUnknown, _summaryBudget;
    private long _reflectionRules, _demandRules, _opaqueRules, _unknownMemberTypes, _unknownSelectionTypes;
    private bool _unknownMembers, _unknownTypes, _initializesArrays;

    private void InitializeBudget()
    {
        long rows = 0;
        foreach (var assembly in _assemblies)
        {
            var reader = System.Reflection.Metadata.PEReaderExtensions.GetMetadataReader(assembly.PE);
            rows += reader.TypeDefinitions.Count + reader.MethodDefinitions.Count + reader.FieldDefinitions.Count
                + reader.PropertyDefinitions.Count + reader.EventDefinitions.Count;
        }
        _workBudget = _request.WorkBudget ?? Math.Max(16384, rows * 1024);
    }

    private void InitializeGraph()
    {
        foreach (var assembly in _assemblies)
        {
            var types = AllTypes(assembly.Assembly.MainModule.Types).ToArray();
            int typeRows = types.Length == 0 ? 0 : (int)types.Max(t => t.MetadataToken.RID);
            int methodRows = types.SelectMany(t => t.Methods).Select(m => (int)m.MetadataToken.RID).DefaultIfEmpty().Max();
            var typeNodes = new TypeNode?[typeRows + 1];
            var methodNodes = new MethodNode?[methodRows + 1];
            foreach (var type in types)
            {
                typeNodes[type.MetadataToken.RID] = new TypeNode { Def = type };
                foreach (var method in type.Methods)
                {
                    methodNodes[method.MetadataToken.RID] = new MethodNode { Def = method };
                }
            }
            _typeNodes.Add(assembly.Assembly.MainModule, typeNodes);
            _methodNodes.Add(assembly.Assembly.MainModule, methodNodes);
        }
    }

    private void Spend()
    {
        if (_workCount >= _workBudget) throw new DietBudgetExceeded();
        _workCount++;
    }

    private void CheckMutable()
    {
        if (_frozen) throw new InvalidOperationException("ILDiet graph is frozen");
    }

    private TypeNode? Node(TypeDefinition type) => _typeNodes.TryGetValue(type.Module, out var nodes)
        && type.MetadataToken.RID < nodes.Length ? nodes[type.MetadataToken.RID] : null;
    private MethodNode? Node(MethodDefinition method) => _methodNodes.TryGetValue(method.Module, out var nodes)
        && method.MetadataToken.RID < nodes.Length ? nodes[method.MetadataToken.RID] : null;

    private void Require(TypeDefinition type, TypeState state)
    {
        CheckMutable();
        if ((state & TypeState.Runtime) != 0) NoteMediumType(type);
        if (type.HasGenericParameters) EnsureOriginalModels();
        if (_passiveAttributeMetadataDepth == 0 && state != TypeState.None
            && _independentMetadataTypes.Add(type) && _passiveAttributeTypes.Contains(type)
            && Node(type) is { Applied: var applied } && (applied & TypeState.Metadata) != 0)
            ApplyConditionalPolicy(type);
        if (_passiveAttributeMetadataDepth == 0 || (state & TypeState.Runtime) != 0) ReplayPassiveBindings(type);
        ObserveModelDefinition(type);
        ObserveAttributeModel(type);
        if (UsesReachedCopyTraversal(type)) ObserveCopiedLayout(type);
        if (UsesReachedCopyTraversal(type) && (state & TypeState.Runtime) == 0)
        {
            if ((state & TypeState.Explicit) != 0) NoteCopiedPreservationUse(type);
            return;
        }
        if (state != TypeState.None) state |= TypeState.Metadata;
        var node = Node(type);
        if (node is null || (node.State & state) == state) return;
        node.State |= state;
        _types.Add(type);
        if ((state & TypeState.Runtime) != 0 && _runtimeTypes.Add(type)) RegistrationRuntimeChanged();
        _work.Enqueue(new Work(node));
        RegistryRule(type);
    }

    private void Require(MethodDefinition method, MethodState state)
    {
        CheckMutable();
        if ((state & MethodState.Body) != 0)
        {
            NoteMediumMethod(method);
            ReachDefaultPrincipalTarget(method);
        }
        if ((state & MethodState.Body) != 0 && _cutMethods.Contains(method))
        {
            KeepCutMethod(method);
            // A normal use still carries metadata authority, but the primary
            // bounds the original body even when a call or policy reaches it.
            state = (state & ~MethodState.Body) | MethodState.Metadata;
        }
        if (UsesReachedCopyTraversal(method.DeclaringType)
            && ((state & MethodState.Body) == 0 || !TraverseCopiedBody(method))) return;
        if (state != MethodState.None && (method.HasGenericParameters || method.DeclaringType.HasGenericParameters))
            EnsureOriginalModels();
        if (_passiveAttributeMetadataDepth == 0 || (state & MethodState.Body) != 0) ReplayPassiveBindings(method);
        if (state != MethodState.None) state |= MethodState.Metadata;
        var node = Node(method);
        if (node is null || (node.State & state) == state) return;
        node.State |= state;
        _signatureMethods.Add(method);
        if ((state & MethodState.Body) != 0)
        {
            _methods.Add(method);
            NoteAttributeBody(method);
        }
        _work.Enqueue(new Work(node));
    }

    private void RequireType(TypeReference? reference, TypeState state)
    {
        int modelDepth = _observingOriginalModel, passiveDepth = _passiveAttributeMetadataDepth;
        if ((state & TypeState.Runtime) != 0) { _observingOriginalModel = 0; _passiveAttributeMetadataDepth = 0; }
        try { RequireTypeReference(reference, state); }
        finally
        {
            _observingOriginalModel = modelDepth;
            _passiveAttributeMetadataDepth = passiveDepth;
        }
    }

    private void RequireTypeReference(TypeReference? reference, TypeState state)
    {
        if (reference is null) return;
        CheckMutable();
        if ((state & TypeState.Runtime) != 0) NoteMediumType(reference);
        Spend();
        EnsureModelReference(reference);
        ObserveModelReference(reference);
        if (reference is ProjectedTypeReference projected)
        {
            Demand demand = (state & TypeState.Runtime) != 0 ? Demand.Runtime
                : _passiveAttributeMetadataDepth != 0 ? Demand.PassiveMetadata : Demand.Metadata;
            if ((state & TypeState.Members) != 0) demand |= Demand.Members;
            RequireProjection(projected.Node, demand);
            return;
        }
        if (_passiveAttributeMetadataDepth != 0) NotePassiveAttributeType(reference);
        ObserveArrayInitializerReference(reference);
        PreserveMemberArrayKinds(reference);
        if (reference is GenericParameter parameter)
        {
            MarkGenericParameter(parameter);
            if (_passiveAttributeMetadataDepth != 0) AddDemand(parameter, Demand.PassiveMetadata);
            if ((state & TypeState.Runtime) != 0) AddDemand(parameter, Demand.Runtime);
            if ((state & TypeState.Members) != 0) AddDemand(parameter, Demand.Members);
            return;
        }
        if (reference is ArrayType array) NoteArrayElement(array.ElementType);
        if (reference is GenericInstanceType generic)
        {
            if (Resolve(generic) is { } owner)
            {
                RegisterModelAliases(owner.GenericParameters, generic.ElementType.GenericParameters);
                BindParameters(owner.GenericParameters, generic.GenericArguments);
                if (!IsStripped(owner) && _passiveAttributeMetadataDepth == 0)
                    foreach (var argument in generic.GenericArguments) QueueDemand(argument, Demand.OpaqueData);
            }
            foreach (var argument in generic.GenericArguments) RequireType(argument, state & ~TypeState.Explicit);
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
            RequireType(specification.ElementType, state);
            return;
        }
        if (Resolve(reference) is { } type) Require(type, state);
    }

    private void Drain()
    {
        while (true)
        {
            while (_work.Count != 0)
            {
                var work = _work.Dequeue();
                if (work.Type is { } type) ApplyType(type);
                else if (work.Method is { } method) ApplyMethod(method);
                else if (work.Parameter is { } parameter) ApplyParameter(parameter);
                else if (work.Projection is { } projection)
                {
                    if (work.ProjectionSource is { } source) ApplyProjectionSource(projection, source);
                    else ApplyProjection(projection);
                }
                else if (work.Argument is { } argument) ApplyDemand(argument, work.Demands);
            }
            bool pending = ApplyOriginalModels();
            pending |= ApplyAttributeInventory();
            pending |= ApplyAttributeMetadataClosure();
            pending |= ApplyCopiedLayouts();
            pending |= ApplySelectedAttributeSites();
            pending |= ApplyAttributeIdentities();
            pending |= ApplyMediumTypes();
            pending |= _modelPending.Count != 0 || _attributeInventoryPending.Count != 0 || _attributeMetadataPending.Count != 0 || _copiedLayoutsPending.Count != 0
                || _attributeIdentityPending.Count != 0;
            if (!pending) break;
        }
    }

    private void ApplyType(TypeNode node)
    {
        var mediumOwner = _mediumOwner;
        if (_request.StrippingLevel == StrippingLevel.Medium) _mediumOwner = node.Def;
        bool passive = _passiveAttributeTypes.Contains(node.Def) && !_independentMetadataTypes.Contains(node.Def)
            && (node.State & TypeState.Runtime) == 0;
        var bindingOwner = _passiveBindingOwner;
        if (passive) { _passiveAttributeMetadataDepth++; _passiveBindingOwner = node.Def; }
        try { ApplyTypeState(node); }
        finally
        {
            _mediumOwner = mediumOwner;
            _passiveBindingOwner = bindingOwner;
            if (passive) _passiveAttributeMetadataDepth--;
        }
    }

    private void ApplyTypeState(TypeNode node)
    {
        var delta = node.State & ~node.Applied;
        if (delta == TypeState.None) return;
        Spend();
        node.Applied |= delta;
        var type = node.Def;
        if (UsesReachedCopyTraversal(type))
        {
            if ((delta & TypeState.Runtime) != 0)
            {
                NoteCopiedPreservationUse(type);
                ApplyCopiedRuntimeType(type);
            }
            return;
        }
        if ((delta & TypeState.Metadata) != 0)
        {
            MarkSignatureType(type.DeclaringType);
            MarkSignatureType(type.BaseType);
            MarkAttributes(type);
            MarkSecurity(type);
            foreach (var parameter in type.GenericParameters) MarkGenericParameter(parameter);
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
            if (type.Module == _assemblies[0].Assembly.MainModule) Require(type, TypeState.Members);
            else
                foreach (var method in type.Methods)
                    if (method.IsVirtual || method.HasOverrides || type.IsInterface || IsDelegate(type)) KeepMethodSignature(method);
            if (_passiveAttributeMetadataDepth == 0) ApplyConditionalPolicy(type);
            ApplyConservativeValueDispatch(type);
        }
        if ((delta & TypeState.Runtime) != 0)
        {
            if (IsDelegate(type))
            {
                _runtimeDelegates.Add(type);
                if (_delegateInvokeBoxesUsed) NoteDelegateInvokeBoxes(type);
            }
            MarkType(type.BaseType);
            if (type.IsValueType)
                foreach (var field in type.Fields)
                    if (!field.IsStatic) QueueDemand(field.FieldType, Demand.Boxed);
            foreach (var implementation in type.Interfaces) MarkType(implementation.InterfaceType);
            foreach (var method in type.Methods)
                if (method.IsVirtual || method.HasOverrides || type.IsInterface || IsDelegate(type)
                    || method.IsConstructor && method.IsStatic) MarkMethod(method);
            if (type.HasInterfaces) KeepInterfaceHierarchy(type);
            if (_conditionalOwnMembers.Contains(type)) KeepOwnMembers(type);
            if (_initializesArrays) KeepArrayInitializer(type);
        }
        if ((delta & TypeState.Members) != 0)
        {
            foreach (var method in type.Methods) KeepMethodSignature(method);
            foreach (var property in type.Properties) KeepPropertySignature(property);
            foreach (var item in type.Events) KeepEventSignature(item);
        }
        if (_unknownMembers && !node.UnknownMembersApplied)
        {
            node.UnknownMembersApplied = true;
            Spend();
            _unknownMemberTypes++;
            Require(type, TypeState.Members);
        }
    }

    private void ApplyMethod(MethodNode node)
    {
        bool medium = _request.StrippingLevel == StrippingLevel.Medium;
        var mediumOwner = _mediumOwner;
        var mediumSource = _mediumSource;
        if (medium)
        {
            _mediumOwner = node.Def.DeclaringType;
            _mediumSource = node.Def;
        }
        bool passive = _passiveAttributeMethods.Contains(node.Def) && !_independentMetadataMethods.Contains(node.Def)
            && (node.State & MethodState.Body) == 0;
        var bindingOwner = _passiveBindingOwner;
        if (passive) { _passiveAttributeMetadataDepth++; _passiveBindingOwner = node.Def; }
        try { ApplyMethodState(node); }
        catch (NotSupportedException error) when (medium)
        {
            throw new NotSupportedException(error.Message + MediumRetentionDiagnostic(node.Def.DeclaringType), error);
        }
        finally
        {
            _mediumOwner = mediumOwner;
            _mediumSource = mediumSource;
            _passiveBindingOwner = bindingOwner;
            if (passive) _passiveAttributeMetadataDepth--;
        }
    }

    private void ApplyMethodState(MethodNode node)
    {
        var delta = node.State & ~node.Applied;
        if (delta == MethodState.None) return;
        Spend();
        node.Applied |= delta;
        var method = node.Def;
        if ((delta & MethodState.Metadata) != 0)
        {
            Require(method.DeclaringType, TypeState.Metadata);
            foreach (var reference in OriginalMethodSignature(method)) MarkSignatureType(reference);
            if (UsesReachedCopyTraversal(method.DeclaringType))
            {
                foreach (var parameter in method.GenericParameters) MarkGenericParameter(parameter);
            }
            else
            {
                MarkAttributes(method, signatureOnly: true);
                MarkSecurity(method);
                MarkAttributes(method.MethodReturnType);
                MarkMarshal(method.MethodReturnType);
                foreach (var parameter in method.Parameters)
                {
                    MarkAttributes(parameter);
                    MarkMarshal(parameter);
                }
                foreach (var parameter in method.GenericParameters) MarkGenericParameter(parameter);
                foreach (var overridden in method.Overrides)
                {
                    MarkSignatureType(overridden.DeclaringType);
                    if (ResolveMember(overridden) is MethodDefinition target) KeepMethodSignature(target);
                    if (overridden is GenericInstanceMethod generic && ResolveMember(generic) is MethodDefinition definition)
                        BindParameters(definition.GenericParameters, generic.GenericArguments);
                }
                foreach (var property in method.DeclaringType.Properties)
                    if (property.GetMethod == method || property.SetMethod == method || property.OtherMethods.Contains(method))
                        KeepPropertyRow(property);
                foreach (var item in method.DeclaringType.Events)
                    if (item.AddMethod == method || item.RemoveMethod == method || item.InvokeMethod == method || item.OtherMethods.Contains(method))
                        KeepEventSignature(item);
            }
        }
        if ((delta & MethodState.Body) != 0)
        {
            Require(method.DeclaringType, TypeState.Runtime);
            ApplyExecutableAttributeDelta(method);
            if (!method.HasBody) return;
            var body = OriginalBodyDeps(method);
            PrepareExecutableBody(method, body);
            ApplyBodyDeps(method, body);
        }
    }

    private BodyDeps OriginalBodyDeps(MethodDefinition method)
    {
        var node = Node(method) ?? throw new InvalidOperationException("ILDiet method is outside its load set");
        if (node.Body is not null) return node.Body;
        if (++node.BodyScans != 1) throw new InvalidOperationException("ILDiet decoded a method body twice");
        _bodyScans++;
        node.Body = BuildBodyDeps(method);
        return node.Body;
    }

    private BodyDeps BuildBodyDeps(MethodDefinition method)
    {
        var operands = new List<(object Operand, Code Op, Mono.Cecil.Cil.Instruction Instruction)>();
        foreach (var instruction in method.Body.Instructions)
        {
            if (instruction.Operand is MethodReference or FieldReference or TypeReference or CallSite)
                operands.Add((instruction.Operand, instruction.OpCode.Code, instruction));
        }
        return new BodyDeps
        {
            Operands = operands.ToArray(),
            Locals = method.Body.Variables.Select(v => v.VariableType).ToArray(),
            Catches = method.Body.ExceptionHandlers.Where(h => h.CatchType is not null).Select(h => h.CatchType).ToArray(),
        };
    }

    private RuntimeTypeMakeGenericTypePlan? _makeGenericTypePlan;
    private bool _makeGenericTypePlanInitialized;

    private ReflectionSummarizer GetReflectionSummarizer(ModuleDefinition module)
    {
        if (!_makeGenericTypePlanInitialized)
        {
            _makeGenericTypePlanInitialized = true;
            _makeGenericTypePlan = new RuntimeTypeMakeGenericTypePlan(_assemblies.Select(a =>
                System.Reflection.Metadata.PEReaderExtensions.GetMetadataReader(a.PE)));
        }
        if (!_summarizers.TryGetValue(module, out var summarizer))
        {
            var assembly = _byModule[module];
            _summarizers.Add(module, summarizer = new ReflectionSummarizer(System.Reflection.Metadata.PEReaderExtensions.GetMetadataReader(assembly.PE), assembly.PE, _makeGenericTypePlan));
        }
        return summarizer;
    }

    private void PrepareExecutableBody(MethodDefinition method, BodyDeps result)
    {
        // Physical carrier facts can precede executable admission. A consumed
        // summary is null too, so null cannot serve as the preparation flag.
        if (result.ReflectionPrepared) return;
        result.ReflectionPrepared = true;
        bool reflection = false;
        foreach (var operand in result.Operands)
            if (operand.Operand is MethodReference target && ClassifyReflectionTarget(target) is { Kind: not ReflectionOpKind.Transfer })
                reflection = true;
        if (reflection && method.RVA != 0 && _byModule.TryGetValue(method.Module, out var assembly))
        {
            var summarizer = GetReflectionSummarizer(method.Module);
            var body = System.Reflection.Metadata.PEReaderExtensions.GetMethodBody(assembly.PE, method.RVA);
            List<Dn2Cpp.Instruction> code;
            try
            {
                code = ILDecoder.Decode(body.GetILContent());
            }
            catch (NotSupportedException error)
            {
                throw new NotSupportedException("cannot decode reflection body " + method.Module.Assembly.Name.Name
                    + "::" + method.FullName + ": " + error.Message, error);
            }
            var contextualOperands = new Dictionary<int, Mono.Cecil.MemberReference>();
            foreach (var operand in result.Operands)
                if (operand.Operand is Mono.Cecil.MemberReference reference)
                    contextualOperands[operand.Instruction.Offset] = reference;
            foreach (var instruction in code)
                if (instruction.Token != 0 && contextualOperands.TryGetValue(instruction.Offset, out var reference))
                    result.Tokens.TryAdd(instruction.Token, reference);
            result.Reflection = summarizer.Summarize(MetadataTokens.MethodDefinitionHandle((int)method.MetadataToken.RID),
                body, code);
            _summaryMethods++;
            if (result.Reflection.OverBudget) _summaryBudget++;
        }
    }

    private readonly Dictionary<TypeDefinition, bool> _namedByRefLikeTokens = new();

    private bool IsNamedByRefLikeToken(TypeReference reference)
    {
        if (reference is TypeSpecification or GenericParameter or ProjectedTypeReference
            || Resolve(reference) is not { HasGenericParameters: false } type) return false;
        if (_namedByRefLikeTokens.TryGetValue(type, out bool known)) return known;
        Spend();
        bool byRefLike = false;
        foreach (var attribute in type.CustomAttributes)
        {
            Spend();
            if (attribute.AttributeType.FullName != "System.Runtime.CompilerServices.IsByRefLikeAttribute") continue;
            byRefLike = true;
            break;
        }
        _namedByRefLikeTokens.Add(type, byRefLike);
        return byRefLike;
    }

    private void ApplyBodyDeps(MethodDefinition method, BodyDeps body)
    {
        foreach (var type in body.Locals) MarkType(type);
        foreach (var type in body.Catches) MarkType(type);
        foreach (var dep in body.Operands)
        {
            Spend();
            switch (dep.Operand)
            {
                case MethodReference target:
                    if (dep.Op == Code.Ldtoken)
                    {
                        KeepMethodToken(target);
                        break;
                    }
                    if (dep.Op == Code.Newobj) NoteAttributeAllocation(target.DeclaringType);
                    if (!_registryFactories.ContainsKey(dep.Instruction)) MarkMethod(target);
                    NoteDelegateInvokeCapability(target);
                    NoteImplicitDispatchOperation(target);
                    if (target.Name == "Initialize" && target.DeclaringType.FullName == "System.Array" && target.Parameters.Count == 0)
                        ArmArrayInitialize();
                    break;
                case FieldReference field: MarkField(field); break;
                case TypeReference type:
                    NoteMediumType(type);
                    if (dep.Op == Code.Box) QueueDemand(type, Demand.Boxed);
                    // A named ref struct token exposes metadata, not an instance's
                    // virtual bodies. Direct and constrained operands remain executable.
                    if (dep.Op == Code.Ldtoken && IsNamedByRefLikeToken(type)) NotePreservedReference(type);
                    else MarkType(type);
                    if (dep.Op == Code.Ldtoken && !PreservationReader.IsFrameworkAssemblyName(method.Module.Assembly.Name.Name))
                        QueueDemand(type, Demand.FrameworkAttributeNamed);
                    break;
                case CallSite signature:
                    MarkType(signature.ReturnType);
                    foreach (var parameter in signature.Parameters) MarkType(parameter.ParameterType);
                    break;
            }
        }
        if (body.Reflection is { } summary)
        {
            ApplySummary(method, summary);
            body.Reflection = null;
        }
    }

    private IEnumerable<KeyValuePair<string, long>> GraphCounters()
    {
        long Count(TypeState state) => _typeNodes.Values.Sum(a => (long)a.Count(n => n is not null && (n.State & state) != 0));
        yield return new("types.metadata", Count(TypeState.Metadata));
        yield return new("types.runtime", Count(TypeState.Runtime));
        yield return new("types.members", Count(TypeState.Members));
        yield return new("types.explicit", Count(TypeState.Explicit));
        yield return new("methods.metadata", _signatureMethods.Count);
        yield return new("methods.body", _methods.Count);
        yield return new("methods.evidence-body", _formationBodies.Count(method => !_methods.Contains(method)));
        yield return new("methods.stub", _plannedStubs);
        yield return new("methods.removed", _plannedRemovedMethods);
        yield return new("decode.signature", _signatureScans);
        yield return new("decode.body", _bodyScans);
        yield return new("decode.max-per-method", _bodyScans == 0 ? 0 : 1);
        yield return new("summary.methods", _summaryMethods);
        yield return new("summary.unknown", _summaryUnknown);
        yield return new("summary.budget", _summaryBudget);
        yield return new("rules.reflection", _reflectionRules);
        yield return new("rules.demand", _demandRules);
        yield return new("rules.opaque", _opaqueRules);
        yield return new("scope.unknown-members.types", _unknownMemberTypes);
        yield return new("scope.unknown-types.types", _unknownSelectionTypes);
        yield return new("work", _workCount);
        yield return new("work.budget", _workBudget);
        yield return new("fallback", _fallback == "none" ? 0 : 1);
        yield return new("fallback.assemblies", _fallback == "none" ? 0 : _assemblies.Count);
        yield return new("registry.factories.kept", _registryKept);
        yield return new("registry.factories.redirected", _registryRedirected);
        yield return new("registry.factories.unknown", _registryUnknown);
        yield return new("registry.scripts.kept", _registryScriptsKept);
        yield return new("registry.scripts.filtered", _registryScriptsFiltered);
    }
}

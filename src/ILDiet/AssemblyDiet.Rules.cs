using Mono.Cecil;

namespace Dn2Cpp;

internal sealed partial class AssemblyDiet
{
    private ParamNode Parameter(GenericParameter parameter)
    {
        EnsureOriginalModels();
        if (!_parameters.TryGetValue(parameter, out var node))
            _parameters.Add(parameter, node = new ParamNode { Parameter = parameter });
        return node;
    }

    private void BindParameters(Mono.Collections.Generic.Collection<GenericParameter> parameters,
        Mono.Collections.Generic.Collection<TypeReference> arguments)
    {
        for (int i = 0; i < parameters.Count && i < arguments.Count; i++) Bind(parameters[i], arguments[i]);
    }

    private bool RecordBinding(ParamNode node, TypeReference argument, string key, bool ordinary)
    {
        if (node.FormationBoundKeys.Add(key))
        {
            Spend();
            node.FormationBound.Add(argument);
            ModelBindingChanged(node.Parameter, argument);
            AttributeModelBindingChanged(node.Parameter);
            if (_promotedModelReferences.Contains(node.Parameter))
            {
                QueueOrdinaryBinding(node.Parameter, argument);
                QueueModelPromotion(argument);
            }
        }
        if (!ordinary || !node.BoundKeys.Add(key)) return false;
        Spend();
        node.Bound.Add(argument);
        // Ordinary authority can arrive after the same formation pair. Replay
        // installed consumers even when the complete formation union is unchanged.
        int modelDepth = _observingOriginalModel, passiveDepth = _passiveAttributeMetadataDepth;
        _observingOriginalModel = 0;
        _passiveAttributeMetadataDepth = 0;
        try
        {
            if (_attributeAllocationParameters.Contains(node.Parameter)) NoteAttributeAllocation(argument);
            ObserveArrayInitializerReference(argument);
            CopiedLayoutBindingChanged(node.Parameter);
            ReplayAttributeIdentityBinding(node.Parameter, argument);
            AttributeBindingsChanged();
            QueueDemand(argument, node.Demands);
            foreach (var projection in node.Projections.ToArray()) QueueProjectionSource(projection, argument);
            foreach (var alias in node.Aliases.ToArray()) QueueOrdinaryBinding(alias, argument);
        }
        finally
        {
            _observingOriginalModel = modelDepth;
            _passiveAttributeMetadataDepth = passiveDepth;
        }
        return true;
    }

    private void Bind(GenericParameter parameter, TypeReference argument)
    {
        bool ordinary = _observingOriginalModel == 0 && _passiveAttributeMetadataDepth == 0;
        if (!ordinary && _observingOriginalModel == 0) NotePassiveBinding(parameter, argument);
        Bind(parameter, argument, ordinary);
        DrainModelAuthority();
    }

    private void Bind(GenericParameter parameter, TypeReference argument, bool ordinary)
    {
        CheckMutable();
        var node = Parameter(parameter);
        string key = ArgumentKey(argument);
        if (!RecordBinding(node, argument, key, ordinary)) return;
        if (node.Parameter.Owner is MethodDefinition method)
            foreach (var candidate in SlotFamily(method))
                RecordBinding(Parameter(candidate.GenericParameters[parameter.Position]), argument, key, ordinary: true);
    }

    private readonly Dictionary<TypeReference, string> _argumentKeys = new();
    private readonly Dictionary<object, int> _argumentOwners = new();

    private static string KeyPart(string value) => value.Length + ":" + value;

    private string ArgumentModuleIdentity(ModuleDefinition? module)
    {
        if (module is null || !_byModule.TryGetValue(module, out var assembly))
            throw new InvalidOperationException("ILDiet argument scope is outside the original load set");
        return assembly.Index.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }

    private string ArgumentScopeKey(TypeReference reference) => reference.Scope switch
    {
        ModuleDefinition module => "m" + ArgumentModuleIdentity(module),
        AssemblyNameReference assembly => "a" + KeyPart(assembly.FullName),
        ModuleReference module => "r" + KeyPart(ArgumentModuleIdentity(reference.Module)) + KeyPart(module.Name),
        null => "z" + ArgumentModuleIdentity(reference.Module),
        _ => throw new InvalidOperationException("unsupported ILDiet argument scope: " + reference.Scope.MetadataScopeType),
    };

    private string ArgumentKey(TypeReference reference)
    {
        if (_argumentKeys.TryGetValue(reference, out var known)) return known;
        CheckMutable();
        Spend();
        string key;
        if (reference is GenericParameter parameter)
        {
            object owner = (object?)parameter.Owner ?? parameter;
            if (!_argumentOwners.TryGetValue(owner, out int identity))
            {
                Spend();
                _argumentOwners.Add(owner, identity = _argumentOwners.Count);
            }
            // Original owner identity survives inside every reference shape. Names
            // alone merge distinct method formals such as A<T> and B<T>.
            key = "p" + identity + ":" + parameter.Type + ":" + parameter.Position;
        }
        else if (reference is ProjectedTypeReference)
            key = "projection" + KeyPart(reference.Name);
        else if (reference is GenericInstanceType generic)
        {
            key = "g" + KeyPart(ArgumentKey(generic.ElementType));
            foreach (var argument in generic.GenericArguments)
            {
                Spend();
                key += KeyPart(ArgumentKey(argument));
            }
        }
        else if (reference is ArrayType array)
        {
            key = "a" + array.IsVector + ":" + array.Rank + ":" + KeyPart(ArgumentKey(array.ElementType));
            foreach (var dimension in array.Dimensions)
            {
                Spend();
                key += KeyPart(dimension.LowerBound?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "")
                    + KeyPart(dimension.UpperBound?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "");
            }
        }
        else if (reference is FunctionPointerType pointer)
        {
            key = "f" + pointer.CallingConvention + ":" + pointer.HasThis + ":" + pointer.ExplicitThis
                + KeyPart(ArgumentKey(pointer.ReturnType));
            foreach (var parameterType in pointer.Parameters)
            {
                Spend();
                key += KeyPart(ArgumentKey(parameterType.ParameterType));
            }
        }
        else if (reference is TypeSpecification specification)
        {
            key = "s" + (int)specification.MetadataType + ":" + KeyPart(ArgumentKey(specification.ElementType));
            if (reference is IModifierType modifier) key += KeyPart(ArgumentKey(modifier.ModifierType));
        }
        else
            key = "n" + KeyPart(ArgumentScopeKey(reference)) + KeyPart(reference.Namespace) + KeyPart(reference.Name)
                + (reference.DeclaringType is null ? "" : KeyPart(ArgumentKey(reference.DeclaringType)));
        _argumentKeys.Add(reference, key);
        return key;
    }

    private void AddDemand(GenericParameter parameter, Demand demands)
    {
        if (demands == Demand.None) return;
        CheckMutable();
        var node = Parameter(parameter);
        if ((node.Demands & demands) == demands) return;
        node.Demands |= demands;
        _work.Enqueue(new Work(node));
    }

    private void QueueDemand(TypeReference argument, Demand demands)
    {
        if (demands == Demand.None) return;
        if (argument is GenericParameter parameter) AddDemand(parameter, demands);
        else if (argument is ProjectedTypeReference projected) RequireProjection(projected.Node, demands);
        else _work.Enqueue(new Work(argument, demands));
    }

    private void ApplyParameter(ParamNode node)
    {
        RequireCompletedModelActivation("ApplyParameter");
        var delta = node.Demands & ~node.Applied;
        if (delta == Demand.None) return;
        Spend();
        _demandRules++;
        node.Applied |= delta;
        foreach (var argument in node.Bound) QueueDemand(argument, delta);
        // Bindings and demands meet across a cached slot family, including inherited
        // non-virtual implementations. Opaque copied signatures add no body demand.
        if (node.Parameter.Owner is MethodDefinition method
            && TracksMethodBody(method))
            foreach (var candidate in SlotFamily(method))
            {
                var target = candidate.GenericParameters[node.Parameter.Position];
                AddDemand(target, delta);
                foreach (var argument in node.Bound.ToArray()) Bind(target, argument);
            }
    }

    private readonly Dictionary<string, List<MethodDefinition>> _genericSlots = new(StringComparer.Ordinal);
    private readonly Dictionary<MethodDefinition, MethodDefinition[]> _slotFamilies = new();
    private readonly Dictionary<MethodDefinition, List<MethodDefinition>> _explicitSlotEdges = new();
    private readonly Dictionary<TypeDefinition, HashSet<TypeDefinition>> _typeRelations = new();
    private sealed class TypeDescendants
    {
        internal readonly List<TypeDefinition> Ordered = new();
        internal readonly HashSet<TypeDefinition> Members = new();
    }
    private readonly Dictionary<TypeDefinition, TypeDescendants> _typeDescendants = new();
    private TypeDefinition[]? _orderedTypes;
    private bool _slotIndexBuilt, _typeDescendantsBuilt;

    private static string SlotKey(MethodDefinition method)
    {
        string name = method.Name;
        int dot = name.LastIndexOf('.');
        if (dot >= 0) name = name[(dot + 1)..];
        return name + "`" + method.GenericParameters.Count + "/" + method.Parameters.Count;
    }

    private static bool IsSlot(MethodDefinition method) => method.IsVirtual || method.HasOverrides || method.DeclaringType.IsInterface;

    private MethodDefinition[] SlotFamily(MethodDefinition method)
    {
        if (!method.HasGenericParameters || !IsStripped(method.DeclaringType) && !IsSlot(method))
            return Array.Empty<MethodDefinition>();
        if (_slotFamilies.TryGetValue(method, out var known)) return known;
        if (!_slotIndexBuilt)
        {
            _orderedTypes = _assemblies.SelectMany(a => AllTypes(a.Assembly.MainModule.Types)
                .OrderBy(t => t.MetadataToken.ToInt32())).ToArray();
            foreach (var type in _orderedTypes)
                foreach (var candidate in type.Methods)
                    if (candidate.HasGenericParameters)
                    {
                        Spend();
                        string key = SlotKey(candidate);
                        if (!_genericSlots.TryGetValue(key, out var list)) _genericSlots.Add(key, list = new());
                        list.Add(candidate);
                        foreach (var overridden in candidate.Overrides)
                            if (ResolveMember(overridden) is MethodDefinition declaration
                                && declaration.GenericParameters.Count == candidate.GenericParameters.Count)
                            {
                                AddExplicitSlotEdge(candidate, declaration);
                                AddExplicitSlotEdge(declaration, candidate);
                            }
                    }
            _slotIndexBuilt = true;
        }
        var result = new List<MethodDefinition>();
        var seeds = new List<MethodDefinition> { method };
        var seen = new HashSet<MethodDefinition> { method };
        for (int i = 0; i < seeds.Count; i++)
            if (_explicitSlotEdges.TryGetValue(seeds[i], out var declarations))
                foreach (var declaration in declarations)
                {
                    Spend();
                    if (seen.Add(declaration)) { seeds.Add(declaration); result.Add(declaration); }
                }
        foreach (var seed in seeds)
        if (_genericSlots.TryGetValue(SlotKey(seed), out var candidates) && candidates.Any(IsSlot))
            foreach (var candidate in candidates)
            {
                Spend();
                if (candidate == method || !IsSlot(seed) && !IsSlot(candidate)) continue;
                if (RelatedTypes(candidate.DeclaringType, seed.DeclaringType)
                    || RelatedTypes(seed.DeclaringType, candidate.DeclaringType))
                {
                    if (seen.Add(candidate)) result.Add(candidate);
                    continue;
                }
                if (!(seed.IsPublic || seed.HasOverrides || seed.DeclaringType.IsInterface)
                    || !(candidate.IsPublic || candidate.HasOverrides || candidate.DeclaringType.IsInterface)) continue;
                if (ShareDescendant(seed.DeclaringType, candidate.DeclaringType)
                    && seen.Add(candidate)) result.Add(candidate);
            }
        var family = result.ToArray();
        _slotFamilies.Add(method, family);
        return family;
    }

    private void AddExplicitSlotEdge(MethodDefinition body, MethodDefinition declaration)
    {
        if (!_explicitSlotEdges.TryGetValue(body, out var edges)) _explicitSlotEdges.Add(body, edges = new());
        if (!edges.Contains(declaration)) { Spend(); edges.Add(declaration); }
    }

    private bool RelatedTypes(TypeDefinition type, TypeDefinition target) => TypeRelations(type).Contains(target);

    private HashSet<TypeDefinition> TypeRelations(TypeDefinition type)
    {
        if (!_typeRelations.TryGetValue(type, out var related))
        {
            related = new HashSet<TypeDefinition>();
            var pending = new Queue<TypeDefinition>();
            pending.Enqueue(type);
            while (pending.Count != 0)
            {
                var current = pending.Dequeue();
                if (!related.Add(current)) continue;
                Spend();
                if (current.BaseType is not null && Resolve(current.BaseType) is { } parent) pending.Enqueue(parent);
                foreach (var implementation in current.Interfaces)
                    if (Resolve(implementation.InterfaceType) is { } contract) pending.Enqueue(contract);
            }
            _typeRelations.Add(type, related);
        }
        return related;
    }

    private bool ShareDescendant(TypeDefinition first, TypeDefinition second)
    {
        if (!_typeDescendantsBuilt)
        {
            // Inverse lists follow original metadata order. Resolving ancestor
            // edges does not instantiate types or add graph demands.
            foreach (var type in _orderedTypes!)
                foreach (var ancestor in TypeRelations(type))
                {
                    Spend();
                    if (!_typeDescendants.TryGetValue(ancestor, out var descendants))
                        _typeDescendants.Add(ancestor, descendants = new TypeDescendants());
                    descendants.Ordered.Add(type);
                    descendants.Members.Add(type);
                }
            _typeDescendantsBuilt = true;
        }
        if (!_typeDescendants.TryGetValue(first, out var a)
            || !_typeDescendants.TryGetValue(second, out var b)) return false;
        var probes = a.Ordered.Count <= b.Ordered.Count ? a : b;
        var targets = probes == a ? b : a;
        foreach (var type in probes.Ordered)
        {
            Spend();
            if (targets.Members.Contains(type)) return true;
        }
        return false;
    }

    private readonly HashSet<(TypeReference Argument, Demand Demands)> _appliedDemands = new();

    private void ApplyDemand(TypeReference reference, Demand demands)
    {
        if (reference is GenericParameter parameter) { AddDemand(parameter, demands); return; }
        if (!_appliedDemands.Add((reference, demands))) return;
        Spend();
        if ((demands & Demand.TypeWide) != 0)
        {
            NoteMediumType(reference);
            demands &= ~Demand.TypeWide;
            if (demands == Demand.None) return;
        }
        if ((demands & Demand.PassiveMetadata) != 0)
        {
            _passiveAttributeMetadataDepth++;
            try { MarkSignatureType(reference); }
            finally { _passiveAttributeMetadataDepth--; }
            demands &= ~Demand.PassiveMetadata;
            if (demands == Demand.None) return;
        }
        if ((demands & Demand.PreservedUse) != 0)
        {
            NotePreservedReference(reference);
            demands &= ~Demand.PreservedUse;
            if (demands == Demand.None) return;
        }
        if ((demands & Demand.ArrayLayout) != 0)
        {
            if (Resolve(reference) is { } layout) ObserveArrayInitializerLayout(layout);
            demands &= ~Demand.ArrayLayout;
            if (demands == Demand.None) return;
        }
        if ((demands & Demand.FrameworkAttributeNamed) != 0)
        {
            NameFrameworkAttribute(reference);
            demands &= ~Demand.FrameworkAttributeNamed;
            if (demands == Demand.None) return;
        }
        if ((demands & Demand.Boxed) != 0)
        {
            KeepReflectionBox(reference);
            demands &= ~Demand.Boxed;
            if (demands == Demand.None) return;
        }
        if ((demands & Demand.Metadata) != 0) MarkSignatureType(reference);
        if ((demands & Demand.Runtime) != 0) MarkType(reference);
        if (reference is GenericInstanceType generic)
            foreach (var argument in generic.GenericArguments) QueueDemand(argument, demands);
        if (reference is TypeSpecification specification && reference is not GenericInstanceType)
        {
            QueueDemand(specification.ElementType, demands);
            MarkSignatureType(reference);
            return;
        }
        if (Resolve(reference) is not { } type) return;
        if ((demands & Demand.Members) != 0 && UsesReachedCopyTraversal(type))
            foreach (var owner in Ancestors(type))
                if (owner.HasMethods || owner.HasFields || owner.HasProperties || owner.HasEvents)
                    NoteCopiedListedOwner(owner);
        // Copied metadata stays opaque; executable demands still enter the same
        // body and runtime policy as a directly selected original member.
        if (IsStripped(type) || _traversedCopies.Contains(type.Module))
        {
            if ((demands & Demand.OpaqueData) != 0) KeepReflectionData(type);
            if ((demands & Demand.Members) != 0) KeepMemberAncestors(type);
        }
        if ((demands & (Demand.DefaultCtor | Demand.DefaultCtorAnyVisibility | Demand.Ctors)) != 0)
        {
            MarkType(type);
            foreach (var constructor in type.Methods)
                if (constructor.IsConstructor && !constructor.IsStatic)
                {
                    // In-place value initialization must distinguish a private default
                    // constructor from an implicit no-op, without retaining its body.
                    if (type.IsValueType && constructor.Parameters.Count == 0 && (demands & Demand.DefaultCtor) != 0)
                        KeepMethodSignature(constructor);
                    if ((demands & Demand.Ctors) != 0 || constructor.Parameters.Count == 0
                        && (constructor.IsPublic || (demands & Demand.DefaultCtorAnyVisibility) != 0))
                    {
                        MarkMethod(constructor);
                        NoteReflectionInvokeBoxes(constructor);
                    }
                }
        }
        if ((demands & Demand.FieldRead) != 0)
            foreach (var owner in Ancestors(type))
                foreach (var field in owner.Fields)
                {
                    MarkField(field);
                    QueueDemand(field.FieldType, Demand.Boxed);
                }
        if ((demands & Demand.MethodBodies) != 0)
        {
            MarkType(type);
            foreach (var owner in Ancestors(type))
                foreach (var method in owner.Methods)
                    if (!method.IsConstructor)
                    {
                        MarkMethod(method);
                        NoteReflectionInvokeBoxes(method);
                    }
        }
        if ((demands & Demand.AccessorBodies) != 0)
            foreach (var owner in Ancestors(type))
            {
                foreach (var property in owner.Properties) MarkProperty(property, true, true);
                foreach (var item in owner.Events) MarkEvent(item);
            }
    }

    private void KeepReflectionData(TypeDefinition type)
    {
        if (!IsStripped(type) || !_reflectionDataTypes.Add(type)) return;
        Spend();
        _opaqueRules++;
        MarkType(type);
        if (type.BaseType is not null) QueueDemand(type.BaseType, Demand.OpaqueData);
        foreach (var method in type.Methods)
            if (method.IsConstructor && !method.IsStatic)
            {
                MarkMethod(method);
                foreach (var parameter in method.Parameters) QueueDemand(parameter.ParameterType, Demand.OpaqueData);
            }
        foreach (var field in type.Fields)
            if (!field.IsStatic) QueueDemand(field.FieldType, Demand.OpaqueData);
        foreach (var property in type.Properties)
            if (property.GetMethod is { IsStatic: false } || property.SetMethod is { IsStatic: false })
            {
                MarkProperty(property, true, true);
                QueueDemand(property.PropertyType, Demand.OpaqueData);
            }
    }

    private void ArmArrayInitialize()
    {
        if (_initializesArrays) return;
        EnsureOriginalModels();
        _initializesArrays = true;
        foreach (var assembly in _assemblies)
            foreach (var type in AllTypes(assembly.Assembly.MainModule.Types))
            {
                Spend();
                if (!type.HasGenericParameters || _runtimeTypes.Contains(type) || _arrayInitializerLayouts.Contains(type))
                    KeepArrayInitializer(type);
            }
    }

    private bool ArrayInitializerEligible(TypeDefinition type) => type.IsValueType && !type.IsEnum
        && !PreservationReader.IsFrameworkAssemblyName(type.Module.Assembly.Name.Name)
        && !CopiedIntrinsicType(type);

    private void KeepArrayInitializer(TypeDefinition type)
    {
        if (!ArrayInitializerEligible(type)) return;
        foreach (var method in type.Methods)
            if (method.IsConstructor && !method.IsStatic && method.Parameters.Count == 0)
            {
                KeepOriginalModelSources(type);
                RequestModelOwnerPromotion(type);
                MarkMethod(method);
            }
    }

    private IEnumerable<TypeDefinition> Ancestors(TypeDefinition type)
    {
        var seen = new HashSet<TypeDefinition>();
        for (var current = type; current is not null && seen.Add(current);
            current = current.BaseType is null ? null : Resolve(current.BaseType)) yield return current;
    }

    private void KeepMemberAncestors(TypeDefinition type)
    {
        foreach (var owner in Ancestors(type)) Require(owner, TypeState.Members);
    }

    private void UnknownMembers()
    {
        if (_unknownMembers) return;
        _unknownMembers = true;
        foreach (var assembly in _assemblies)
            foreach (var type in AllTypes(assembly.Assembly.MainModule.Types))
                if (Node(type) is { State: not TypeState.None } node)
                {
                    Spend();
                    if (!node.UnknownMembersApplied)
                    {
                        node.UnknownMembersApplied = true;
                        _unknownMemberTypes++;
                        Require(type, TypeState.Members);
                    }
                }
    }

    private void UnknownTypes()
    {
        if (_unknownTypes) return;
        _unknownTypes = true;
        foreach (var assembly in _assemblies)
            if (!assembly.Copy)
                foreach (var type in AllTypes(assembly.Assembly.MainModule.Types).OrderBy(t => t.MetadataToken.ToInt32()))
                {
                    Spend();
                    _unknownSelectionTypes++;
                    Require(type, TypeState.Members);
                }
    }
}

internal sealed partial class AssemblyDiet
{
    private readonly Dictionary<int, SummaryValue> _summaryValues = new();

    private sealed class SummaryValue
    {
        internal readonly List<TypeReference> Types = new();
        internal readonly List<TypeReference[][]> TypeArrays = new();
        internal readonly List<TypeReference> SymbolicGenericArrays = new();
        internal readonly List<IMemberDefinition> Members = new();
        internal readonly List<TypeReference> AttributeTypes = new();
        internal readonly List<ParameterDefinition> AttributeParameters = new();
        internal readonly List<ModuleDefinition> Assemblies = new();
        internal readonly List<TypeReference> SymbolicAssemblyOwners = new();
        internal bool ParameterTypesResolved;
        internal readonly List<string> Strings = new();
        internal readonly List<long> Integers = new();
        internal bool Unknown, HasObjectOwners, HasOpenTypeSignatures, HasOpenMethodSignatures;
        internal bool HasNullValues, HasNonArrayArgumentValues;
        internal bool HasUnknownOwner => Unknown || HasObjectOwners;
        internal void Add(SummaryValue other)
        {
            Types.AddRange(other.Types); TypeArrays.AddRange(other.TypeArrays);
            SymbolicGenericArrays.AddRange(other.SymbolicGenericArrays); Members.AddRange(other.Members);
            AttributeTypes.AddRange(other.AttributeTypes);
            AttributeParameters.AddRange(other.AttributeParameters);
            Assemblies.AddRange(other.Assemblies); SymbolicAssemblyOwners.AddRange(other.SymbolicAssemblyOwners);
            ParameterTypesResolved |= other.ParameterTypesResolved; Strings.AddRange(other.Strings); Integers.AddRange(other.Integers);
            Unknown |= other.Unknown;
            HasObjectOwners |= other.HasObjectOwners;
            HasOpenTypeSignatures |= other.HasOpenTypeSignatures;
            HasOpenMethodSignatures |= other.HasOpenMethodSignatures;
            HasNullValues |= other.HasNullValues;
            HasNonArrayArgumentValues |= other.HasNonArrayArgumentValues;
        }
    }

    private static RValue SiteOperand(RSite site, sbyte slot) => slot switch
    {
        ReflectionApi.Receiver => site.Receiver,
        ReflectionApi.MethodTypeArgument => site.TypeArgument,
        >= 0 when slot < site.Operands.Length => site.Operands[slot],
        _ => RValue.None,
    };

    private IMetadataTokenProvider? SummaryToken(MethodDefinition caller, int token)
    {
        // TypeSpec tokens containing !n/!!n need the body's declaring context.
        // Cecil's module-level lookup has no owner for those parameter symbols.
        if (Node(caller)?.Body is { } body && body.Tokens.TryGetValue(token, out var contextual)) return contextual;
        return caller.Module.LookupToken(token);
    }

    private SummaryValue SummaryValueOf(MethodDefinition caller, ReflectionSummary summary, RValue value)
    {
        var result = new SummaryValue { Unknown = value.Unknown >= 0 };
        var seen = new HashSet<int>();
        foreach (int id in value.Alternatives ?? Array.Empty<int>()) result.Add(SummaryTerm(caller, summary, id, seen));
        return result;
    }

    private SummaryValue SummaryTerm(MethodDefinition caller, ReflectionSummary summary, int id, HashSet<int> seen)
    {
        var result = new SummaryValue();
        if (id < 0 || id >= summary.Terms.Length || !seen.Add(id)) return result;
        if (_summaryValues.TryGetValue(id, out var cached)) return cached;
        Spend();
        var term = summary.Terms[id];
        SummaryValue Child(int slot) => slot < term.Children.Length
            ? SummaryValueOf(caller, summary, summary.ValueOf(term.Children[slot])) : new SummaryValue();
        switch (term.Kind)
        {
            case TermKind.Set:
                foreach (int child in term.Children) result.Add(SummaryTerm(caller, summary, child, seen));
                result.Unknown |= term.Number >= 0;
                break;
            case TermKind.TypeToken:
            case TermKind.ExactObject:
            case TermKind.Closure:
            {
                if (SummaryToken(caller, term.Token) is TypeReference reference)
                {
                    var type = term.Number == ReflectionSummary.InexactBox ? BoxedRuntimeType(reference) : reference;
                    result.Types.Add(type);
                    result.HasObjectOwners = term.Kind is TermKind.ExactObject or TermKind.Closure;
                    if (term.Number == ReflectionSummary.InexactBox && !ExactSummaryRuntimeType(type)) result.Unknown = true;
                }
                else result.Unknown = true;
                break;
            }
            case TermKind.MethodSpecArg:
            {
                if (SummaryToken(caller, term.Token) is GenericInstanceMethod generic
                    && term.Number >= 0 && term.Number < generic.GenericArguments.Count)
                {
                    var argument = generic.GenericArguments[(int)term.Number];
                    result.Types.Add(argument);
                }
                else result.Unknown = true;
                break;
            }
            case TermKind.MemberToken:
            {
                if (SummaryToken(caller, term.Token) is Mono.Cecil.MemberReference member && ResolveMember(member) is { } definition)
                {
                    result.Members.Add(definition);
                    result.HasOpenMethodSignatures = member is not GenericInstanceMethod
                        && definition is MethodDefinition { HasGenericParameters: true };
                }
                else result.Unknown = true;
                break;
            }
            case TermKind.Str: if (term.Text is not null) result.Strings.Add(term.Text); break;
            case TermKind.Int: result.Integers.Add(term.Number); break;
            case TermKind.TypeName:
            {
                if (term.Text is { } text)
                {
                    if (ReflectionTypeName.Parse(text) is { } parsed)
                    {
                        var assemblies = Child(0);
                        ResolveTypeName(parsed, result, assemblies);
                        result.Unknown |= TypeNameIsUncertain(parsed, assemblies, caller.Module,
                            term.Children.Length > 0 && term.Children[0] >= 0, (term.Number & 1) != 0);
                    }
                }
                else result.Unknown = true;
                break;
            }
            case TermKind.Assembly:
                if (term.Text is { } assemblyName)
                {
                    string simple = assemblyName.Split(',')[0].Trim();
                    foreach (var assembly in _assemblies)
                        if (string.Equals(assembly.Assembly.Name.Name, simple, StringComparison.OrdinalIgnoreCase))
                            result.Assemblies.Add(assembly.Assembly.MainModule);
                }
                else if (term.Children.Length > 0)
                {
                    var owner = SelectedAttributeOwners(Child(0));
                    foreach (var type in owner.Types)
                        if (IsSymbolic(type)) result.SymbolicAssemblyOwners.Add(type);
                        else if (Resolve(type) is { } definition) result.Assemblies.Add(definition.Module);
                    result.Unknown = owner.Unknown || owner.Types.Count == 0;
                }
                else result.Assemblies.Add(term.Number == 2 ? _assemblies[0].Assembly.MainModule : caller.Module);
                break;
            case TermKind.Lookup:
            {
                var owners = Child(ReflectionSummary.LookupOwner);
                var names = Child(ReflectionSummary.LookupName);
                var flags = Child(ReflectionSummary.LookupFlags);
                var types = Child(ReflectionSummary.LookupTypes);
                var arity = Child(ReflectionSummary.LookupArity);
                SelectMembers(result, owners, names, (ReflectionOp)term.Number, flags, types, arity,
                    term.Children.Length > ReflectionSummary.LookupName && term.Children[ReflectionSummary.LookupName] >= 0);
                break;
            }
            case TermKind.Enumeration:
            {
                var owners = Child(0);
                var op = (ReflectionOp)term.Number;
                if (op is ReflectionOp.GetCustomAttributes or ReflectionOp.GetCustomAttributesData)
                    result.Unknown = owners.HasUnknownOwner;
                else if (op == ReflectionOp.AssemblyGetTypes)
                {
                    foreach (var module in owners.Assemblies)
                        foreach (var type in AllTypes(module.Types)) result.Types.Add(type);
                    foreach (var owner in owners.SymbolicAssemblyOwners)
                        result.Types.Add(Project(owner, ProjectionKind.AssemblyTypes));
                    result.Unknown = owners.HasUnknownOwner || owners.Assemblies.Count == 0 && owners.SymbolicAssemblyOwners.Count == 0;
                }
                else if (op == ReflectionOp.GetGenericArguments)
                {
                    foreach (var reference in owners.Types)
                        if (reference is ProjectedTypeReference { Node.Kind: ProjectionKind.GenericMethodClosure } closure)
                        {
                            foreach (var vector in closure.Node.GenericVectors)
                            {
                                result.TypeArrays.Add(vector);
                                foreach (var position in vector) result.Types.AddRange(position);
                            }
                        }
                        else if (IsSymbolic(reference))
                        {
                            result.SymbolicGenericArrays.Add(reference);
                            result.Types.Add(Project(reference, DetermineGenericArgProjection(reference)));
                        }
                        else
                        {
                            var arguments = GenericArguments(reference);
                            result.Types.AddRange(arguments);
                            result.TypeArrays.Add(arguments.Select(a => new[] { a }).ToArray());
                        }
                    foreach (var member in owners.Members)
                        if (member is MethodDefinition method)
                        {
                            result.Types.AddRange(method.GenericParameters);
                            result.TypeArrays.Add(method.GenericParameters.Select(p => new TypeReference[] { p }).ToArray());
                        }
                    result.Unknown = owners.HasUnknownOwner;
                }
                else if (op == ReflectionOp.GetInterfaces)
                {
                    foreach (var reference in owners.Types)
                        if (IsSymbolic(reference))
                            result.Types.Add(Project(reference, ProjectionKind.Interfaces));
                        else if (Resolve(reference) is { } type)
                        {
                            foreach (var contract in InterfaceClosure(type))
                            {
                                Spend();
                                result.Types.Add(contract);
                            }
                        }
                    result.Unknown = owners.HasUnknownOwner;
                }
                else if (op == ReflectionOp.GetNestedTypes)
                {
                    foreach (var reference in owners.Types)
                        if (IsSymbolic(reference)) result.Types.Add(Project(reference, ProjectionKind.NestedTypes));
                        else if (Resolve(reference) is { } type) result.Types.AddRange(type.NestedTypes);
                    result.Unknown = owners.HasUnknownOwner;
                }
                else if (op == ReflectionOp.GetParameters)
                {
                    result.ParameterTypesResolved = true;
                    result.HasOpenTypeSignatures = owners.HasOpenTypeSignatures;
                    result.HasOpenMethodSignatures = owners.HasOpenMethodSignatures;
                    foreach (var reference in owners.Types)
                        if (!_selectingAttributeOwners && IsSymbolic(reference)) result.Types.Add(Project(reference, ProjectionKind.ParameterTypes));
                    foreach (var member in owners.Members)
                        if (member is MethodDefinition method)
                            foreach (var parameter in method.Parameters)
                                if (_selectingAttributeOwners) result.AttributeParameters.Add(parameter);
                                else result.Types.Add(parameter.ParameterType);
                    result.Unknown = owners.HasUnknownOwner;
                }
                else SelectMembers(result, owners, Child(2), op, Child(1), new SummaryValue(), new SummaryValue(),
                    term.Children.Length > 2 && term.Children[2] >= 0, flagsAffectUncertainty: false);
                break;
            }
            case TermKind.TypeOfMember:
            case TermKind.Parameter:
            {
                var owners = Child(0);
                bool parameterType = term.Kind == TermKind.TypeOfMember && (ReflectionOp)term.Number == ReflectionOp.ParameterType;
                result.ParameterTypesResolved = term.Kind == TermKind.Parameter || parameterType;
                if (_selectingAttributeOwners && term.Kind == TermKind.Parameter)
                {
                    foreach (var member in owners.Members)
                        if (member is MethodDefinition method && term.Number >= 0 && term.Number < method.Parameters.Count)
                            result.AttributeParameters.Add(method.Parameters[(int)term.Number]);
                    result.Unknown = owners.Unknown;
                    break;
                }
                if (_selectingAttributeOwners && parameterType)
                    foreach (var parameter in owners.AttributeParameters)
                        AddSummaryType(result, parameter.ParameterType, owners);
                foreach (var member in owners.Members)
                    switch (member)
                    {
                        case FieldDefinition field: AddSummaryType(result, field.FieldType, owners); break;
                        case PropertyDefinition property: AddSummaryType(result, property.PropertyType, owners); break;
                        case MethodDefinition method:
                            if (term.Kind == TermKind.Parameter)
                            {
                                if (term.Number >= 0 && term.Number < method.Parameters.Count)
                                    AddSummaryType(result, method.Parameters[(int)term.Number].ParameterType, owners);
                            }
                            else AddSummaryType(result, method.ReturnType, owners);
                            break;
                    }
                foreach (var owner in owners.Types)
                    if (parameterType && owners.ParameterTypesResolved) AddSummaryType(result, owner, owners);
                    else if (IsSymbolic(owner))
                    {
                        var projection = term.Kind == TermKind.Parameter ? ProjectionKind.ParameterTypes
                            : (ReflectionOp)term.Number switch
                            {
                                ReflectionOp.FieldType => ProjectionKind.FieldTypes,
                                ReflectionOp.PropertyType => ProjectionKind.PropertyTypes,
                                ReflectionOp.ParameterType => ProjectionKind.ParameterTypes,
                                _ => ProjectionKind.ReturnTypes,
                            };
                        AddSummaryType(result, Project(owner, projection, term.Kind == TermKind.Parameter ? (int)term.Number : -1), owners);
                    }
                    else AddSummaryType(result, owner, owners);
                result.Unknown |= owners.Unknown;
                break;
            }
            case TermKind.BaseOf:
            {
                var owners = Child(0);
                result.Unknown = owners.Unknown;
                foreach (var reference in owners.Types)
                {
                    var parent = IsSymbolic(reference) ? Project(reference, ProjectionKind.Base) : BaseReference(reference);
                    if (parent is null) continue;
                    result.Types.Add(parent);
                }
                break;
            }
            case TermKind.GenericArgOf:
            {
                var owners = Child(0);
                foreach (var reference in owners.Types)
                    if (reference is ProjectedTypeReference { Node.Kind: ProjectionKind.GenericMethodClosure } closure)
                    {
                        foreach (var vector in closure.Node.GenericVectors)
                            if (term.Number >= 0 && term.Number < vector.Length) result.Types.AddRange(vector[(int)term.Number]);
                            else if (term.Number < 0)
                                foreach (var position in vector) result.Types.AddRange(position);
                    }
                    else if (IsSymbolic(reference)) result.Types.Add(Project(reference, DetermineGenericArgProjection(reference), (int)term.Number));
                    else
                    {
                        var arguments = GenericArguments(reference);
                        if (term.Number >= 0 && term.Number < arguments.Length) result.Types.Add(arguments[(int)term.Number]);
                        else if (term.Number < 0) result.Types.AddRange(arguments);
                    }
                foreach (var member in owners.Members)
                    if (member is MethodDefinition method)
                    {
                        if (term.Number >= 0 && term.Number < method.GenericParameters.Count)
                            result.Types.Add(method.GenericParameters[(int)term.Number]);
                        else if (term.Number < 0) result.Types.AddRange(method.GenericParameters);
                    }
                result.Unknown = owners.Unknown;
                break;
            }
            case TermKind.GenericDefOf:
            {
                var owners = Child(0);
                result.Unknown = owners.Unknown;
                foreach (var reference in owners.Types)
                    result.Types.Add(IsSymbolic(reference) ? Project(reference, ProjectionKind.GenericDefinition)
                        : reference is GenericInstanceType instance ? instance.ElementType : reference);
                break;
            }
            case TermKind.ArrayOf:
            {
                var owners = Child(0);
                result.Unknown = owners.Unknown || term.Number == 0 && owners.Types.Count != 0;
                foreach (var reference in owners.Types)
                    result.Types.Add(term.Number == ReflectionTypeName.SZArray ? new ArrayType(reference)
                        : new ArrayType(reference, Math.Max(1, (int)term.Number)));
                break;
            }
            case TermKind.ElementOf:
            {
                var owners = Child(0);
                result.Unknown = owners.Unknown;
                foreach (var reference in owners.Types)
                    if (IsSymbolic(reference)) result.Types.Add(Project(reference, ProjectionKind.Element));
                    else if (reference is ArrayType or ByReferenceType or PointerType)
                        result.Types.Add(((TypeSpecification)reference).ElementType);
                break;
            }
            case TermKind.MadeGeneric:
            {
                var definitions = Child(0);
                var arguments = Child(1);
                foreach (var reference in definitions.Types)
                {
                    if (IsSymbolic(reference))
                    {
                        if (HasSummaryArguments(arguments))
                            result.Types.Add(CloseProjectedGeneric(reference, arguments, caller, id));
                        continue;
                    }
                    if (Resolve(reference) is not { HasGenericParameters: true } definition) continue;
                    BindSummaryArguments(definition.GenericParameters, arguments);
                    bool constructed = false;
                    foreach (var alternative in arguments.TypeArrays)
                    {
                        if (alternative.Length != definition.GenericParameters.Count
                            || alternative.Any(position => position.Length == 0)) continue;
                        // Keep exact tuples when every position is a singleton. Other
                        // finite unions flow through the definition's parameter demands.
                        if (alternative.All(position => position.Length == 1))
                        {
                            var instance = new GenericInstanceType(reference);
                            foreach (var position in alternative) instance.GenericArguments.Add(position[0]);
                            result.Types.Add(instance);
                        }
                        else result.Types.Add(reference);
                        constructed = true;
                    }
                    if (!constructed && arguments.SymbolicGenericArrays.Count != 0)
                        result.Types.Add(reference);
                }
                result.Unknown = definitions.HasUnknownOwner || arguments.HasUnknownOwner;
                break;
            }
            case TermKind.MadeGenericMethod:
            {
                var methods = Child(0);
                result.HasOpenTypeSignatures = methods.HasOpenTypeSignatures;
                var arguments = Child(1);
                foreach (var owner in methods.Types)
                    if (IsSymbolic(owner) && HasSummaryArguments(arguments))
                        result.Types.Add(CloseProjectedGeneric(owner, arguments, caller, id, true));
                foreach (var member in methods.Members)
                    // Attribute reads can retain the original provider without binding unknown arguments.
                    if (member is MethodDefinition { HasGenericParameters: true } method
                        && (BindSummaryArguments(method.GenericParameters, arguments)
                            || _selectingAttributeOwners && methods.HasOpenTypeSignatures))
                        result.Members.Add(method);
                result.Unknown = methods.HasUnknownOwner || arguments.HasUnknownOwner;
                break;
            }
            case TermKind.Accessor:
            {
                var owners = Child(0);
                result.HasOpenTypeSignatures = owners.HasOpenTypeSignatures;
                result.HasOpenMethodSignatures = owners.HasOpenMethodSignatures;
                result.Unknown = owners.Unknown;
                foreach (var owner in owners.Types)
                    if (IsSymbolic(owner)) result.Types.Add(Project(owner, ProjectionKind.Methods));
                foreach (var member in owners.Members)
                    switch (member)
                    {
                        case PropertyDefinition property:
                            if (term.Number is (long)ReflectionAccessor.Getter or (long)ReflectionAccessor.All && property.GetMethod is not null)
                                result.Members.Add(property.GetMethod);
                            if (term.Number is (long)ReflectionAccessor.Setter or (long)ReflectionAccessor.All && property.SetMethod is not null)
                                result.Members.Add(property.SetMethod);
                            break;
                        case EventDefinition item:
                            if (term.Number is (long)ReflectionAccessor.Adder or (long)ReflectionAccessor.All && item.AddMethod is not null)
                                result.Members.Add(item.AddMethod);
                            if (term.Number is (long)ReflectionAccessor.Remover or (long)ReflectionAccessor.All && item.RemoveMethod is not null)
                                result.Members.Add(item.RemoveMethod);
                            if (term.Number is (long)ReflectionAccessor.Raiser or (long)ReflectionAccessor.All && item.InvokeMethod is not null)
                                result.Members.Add(item.InvokeMethod);
                            break;
                    }
                break;
            }
            case TermKind.Element:
            {
                var source = Child(0);
                if (term.Children.Length > 0 && term.Children[0] >= 0
                    && summary.Terms[term.Children[0]].Kind == TermKind.TypeArray
                    && term.Number >= 0)
                {
                    var array = summary.Terms[term.Children[0]];
                    if (term.Number < array.Children.Length)
                        result.Add(SummaryValueOf(caller, summary, summary.ValueOf(array.Children[(int)term.Number])));
                }
                else result.Add(source);
                break;
            }
            case TermKind.TypeArray:
            {
                var positions = new TypeReference[term.Children.Length][];
                for (int i = 0; i < term.Children.Length; i++)
                {
                    var position = SummaryValueOf(caller, summary, summary.ValueOf(term.Children[i]));
                    positions[i] = position.Types.ToArray();
                    result.Types.AddRange(position.Types);
                    result.Unknown |= position.Unknown || position.Types.Count == 0;
                }
                if (term.Number == term.Children.Length) result.TypeArrays.Add(positions);
                else result.Unknown = true;
                break;
            }
            case TermKind.Delegate: result.Add(Child(0)); break;
            case TermKind.Null:
                result.HasNullValues = true;
                break;
            default: result.Unknown = true; break;
        }
        CarrySelectedAttributeTypeOwners(term, result);
        result.HasNonArrayArgumentValues |= result.HasObjectOwners || result.Members.Count != 0
            || result.Assemblies.Count != 0 || result.Strings.Count != 0 || result.Integers.Count != 0
            || result.SymbolicAssemblyOwners.Count != 0
            || result.Types.Count != 0 && result.TypeArrays.Count == 0 && result.SymbolicGenericArrays.Count == 0;
        _summaryValues[id] = result;
        return result;
    }

    private void ResolveTypeName(string text, SummaryValue result, SummaryValue assemblies)
    {
        var parsed = ReflectionTypeName.Parse(text);
        if (parsed is null) return;
        ResolveTypeName(parsed, result, assemblies);
    }

    private void ResolveTypeName(ReflectionTypeName parsed, SummaryValue result, SummaryValue assemblies)
    {
        // Resolve each original argument position once. Ambiguous names bind their
        // finite alternatives without constructing a Cartesian family of contexts.
        var arguments = new TypeReference[parsed.GenericArguments.Length][];
        for (int i = 0; i < arguments.Length; i++)
        {
            Spend();
            var candidates = new SummaryValue();
            ResolveTypeName(parsed.GenericArguments[i], candidates, new SummaryValue());
            arguments[i] = candidates.Types.ToArray();
            result.Unknown |= candidates.Unknown || arguments[i].Length == 0;
        }
        foreach (var module in TypeNameScope(parsed, assemblies))
            foreach (var type in AllTypes(module.Types))
            {
                Spend();
                if (!string.Equals(Name(type), parsed.DefinitionName, StringComparison.OrdinalIgnoreCase)) continue;
                TypeReference reference = type;
                if (arguments.Length != 0 && arguments.Length == type.GenericParameters.Count)
                {
                    BindTypeArray(type.GenericParameters, arguments);
                    if (arguments.All(position => position.Length == 1))
                    {
                        var instance = new GenericInstanceType(type);
                        foreach (var position in arguments) instance.GenericArguments.Add(position[0]);
                        reference = instance;
                    }
                }
                foreach (int modifier in parsed.Modifiers)
                {
                    Spend();
                    reference = modifier switch
                    {
                        ReflectionTypeName.SZArray => new ArrayType(reference),
                        ReflectionTypeName.ByRef => new ByReferenceType(reference),
                        ReflectionTypeName.Pointer => new PointerType(reference),
                        _ => new ArrayType(reference, modifier),
                    };
                }
                result.Types.Add(reference);
            }
        result.Unknown |= assemblies.Unknown;
    }

    private ModuleDefinition[] TypeNameScope(ReflectionTypeName parsed, SummaryValue assemblies)
    {
        var scope = new List<ModuleDefinition>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var pending = new Queue<string>();
        if (assemblies.Assemblies.Count != 0)
            foreach (var module in assemblies.Assemblies) pending.Enqueue(module.Assembly.Name.Name);
        else if (parsed.AssemblySimpleName is { } simple) pending.Enqueue(simple);
        else foreach (var assembly in _assemblies) pending.Enqueue(assembly.Assembly.Name.Name);
        while (pending.Count != 0)
        {
            string name = pending.Dequeue();
            if (!seen.Add(name)) continue;
            Spend();
            bool loaded = false;
            foreach (var assembly in _assemblies)
            {
                Spend();
                if (!string.Equals(name, assembly.Assembly.Name.Name, StringComparison.OrdinalIgnoreCase)) continue;
                loaded = true;
                var module = assembly.Assembly.MainModule;
                scope.Add(module);
                foreach (var exported in module.ExportedTypes)
                {
                    Spend();
                    if (exported.DeclaringType is not null) continue;
                    string top = exported.Namespace.Length == 0 ? exported.Name : exported.Namespace + "." + exported.Name;
                    if (string.Equals(top, parsed.TopLevelName, StringComparison.OrdinalIgnoreCase)
                        && exported.Scope is AssemblyNameReference destination) pending.Enqueue(destination.Name);
                }
            }
            // Match the admission resolver's omitted framework facade convention.
            if (!loaded && name is "mscorlib" or "netstandard" or "System.Runtime") pending.Enqueue("System.Private.CoreLib");
        }
        return scope.ToArray();
    }

    private static bool CompleteNames(SummaryValue value) => !value.Unknown && value.Strings.Count != 0
        && value.Types.Count == 0 && value.Members.Count == 0 && value.Assemblies.Count == 0 && value.Integers.Count == 0
        && value.TypeArrays.Count == 0 && value.SymbolicGenericArrays.Count == 0 && value.SymbolicAssemblyOwners.Count == 0;

    private void SelectMembers(SummaryValue result, SummaryValue owners, SummaryValue names, ReflectionOp op,
        SummaryValue flags, SummaryValue parameterTypes, SummaryValue arity, bool hasName, bool flagsAffectUncertainty = true)
    {
        owners = SelectedAttributeOwners(owners);
        result.HasOpenTypeSignatures = owners.HasOpenTypeSignatures;
        result.HasOpenMethodSignatures = owners.HasOpenMethodSignatures;
        result.Unknown = owners.HasUnknownOwner || hasName && !CompleteNames(names) || flagsAffectUncertainty && flags.Unknown;
        if (op == ReflectionOp.InvokeMember) flags = InvokeMemberFlags(flags);
        else if (op is ReflectionOp.RuntimeTypeDefaultStructCtor or ReflectionOp.RuntimeTypeCreateInstanceOfT or ReflectionOp.RuntimeTypeDefaultCtor or ReflectionOp.ActivatorCreate or ReflectionOp.ActivatorCreateGeneric
            or ReflectionOp.ActivatorCreateNamed or ReflectionOp.AssemblyCreateInstance) flags = ConstructionFlags(flags);
        bool constructByFlags = op == ReflectionOp.InvokeMember && MayCreateInstance(flags);
        foreach (var reference in owners.Types)
        {
            if (IsSymbolic(reference))
            {
                if (op is ReflectionOp.GetConstructor or ReflectionOp.GetConstructors)
                {
                    result.Types.Add(Project(reference, ProjectionKind.Constructors));
                    continue;
                }
                if (op is ReflectionOp.GetMember or ReflectionOp.GetMembers or ReflectionOp.GetDefaultMembers)
                {
                    result.Types.Add(Project(reference, ProjectionKind.NestedTypes));
                    if (MaySelectConstructor(names)) result.Types.Add(Project(reference, ProjectionKind.Constructors));
                }
                result.Types.Add(op is ReflectionOp.GetInterface or ReflectionOp.GetInterfaces
                    ? Project(reference, ProjectionKind.Interfaces)
                    : op is ReflectionOp.GetNestedType or ReflectionOp.GetNestedTypes
                        ? Project(reference, ProjectionKind.NestedTypes)
                        : op is ReflectionOp.GetMethod or ReflectionOp.GetMethods or ReflectionOp.CreateDelegateNamed
                            ? Project(reference, ProjectionKind.Methods) : reference);
                continue;
            }
            if (Resolve(reference) is not { } type) continue;
            if (reference is not GenericInstanceType && type.HasGenericParameters)
                result.HasOpenTypeSignatures = true;
            var selectedNames = names;
            if (op == ReflectionOp.GetDefaultMembers)
            {
                string? defaultName = DefaultMemberName(type);
                if (defaultName is null) continue;
                selectedNames = new SummaryValue();
                selectedNames.Strings.Add(defaultName);
            }
            if (op == ReflectionOp.CreateDelegateNamed)
            {
                // Named binding searches methods of every visibility across the
                // base chain, including constructors that ordinary lookup excludes.
                foreach (var owner in Ancestors(type))
                    foreach (var method in owner.Methods)
                    {
                        Spend();
                        if ((selectedNames.Unknown || selectedNames.Strings.Count == 0
                            || selectedNames.Strings.Any(name => string.Equals(name, method.Name, StringComparison.OrdinalIgnoreCase)))
                            && MatchesNamedBindingStorage(method.IsStatic, flags)) AddSelectedSummaryMethod(result, method);
                    }
                continue;
            }
            if (op == ReflectionOp.TypeInitializer)
            {
                foreach (var initializer in type.Methods)
                {
                    Spend();
                    if (initializer.IsConstructor && initializer.IsStatic) result.Members.Add(initializer);
                }
                continue;
            }
            foreach (var owner in Ancestors(type))
            {
                if (owner != type && !flags.Unknown && flags.Integers.Count == 1
                    && (flags.Integers[0] & ReflectionApis.DeclaredOnly) != 0) break;
                bool NameMatches(string name) => selectedNames.Strings.Count == 0 || selectedNames.Unknown
                    || selectedNames.Strings.Any(n => string.Equals(n, name, StringComparison.OrdinalIgnoreCase)
                        || n.EndsWith('*') && name.StartsWith(n[..^1], StringComparison.OrdinalIgnoreCase));
                bool methods = op is ReflectionOp.GetMethod or ReflectionOp.GetMethods or ReflectionOp.GetMember
                    or ReflectionOp.GetMembers or ReflectionOp.GetDefaultMembers or ReflectionOp.InvokeMember;
                bool constructors = op is ReflectionOp.GetConstructor or ReflectionOp.GetConstructors
                    or ReflectionOp.RuntimeTypeDefaultStructCtor or ReflectionOp.RuntimeTypeCreateInstanceOfT or ReflectionOp.RuntimeTypeDefaultCtor or ReflectionOp.ActivatorCreate or ReflectionOp.ActivatorCreateGeneric or ReflectionOp.ActivatorCreateNamed
                    or ReflectionOp.AssemblyCreateInstance or ReflectionOp.InvokeMember
                    or ReflectionOp.GetMember or ReflectionOp.GetMembers or ReflectionOp.GetDefaultMembers;
                foreach (var method in owner.Methods)
                {
                    if (!(method.IsConstructor ? constructors && owner == type
                        && (!method.IsStatic || SelectsStaticConstructors(op, flags)) : methods)) continue;
                    if (!(method.IsConstructor && !method.IsStatic && constructByFlags) && !NameMatches(method.Name)) continue;
                    if (op == ReflectionOp.ActivatorCreateGeneric && (!method.IsPublic || method.Parameters.Count != 0)) continue;
                    if (op is ReflectionOp.RuntimeTypeDefaultStructCtor or ReflectionOp.RuntimeTypeDefaultCtor or ReflectionOp.RuntimeTypeCreateInstanceOfT
                        && method.Parameters.Count != 0) continue;
                    if (!arity.Unknown && arity.Integers.Count == 1 && method.GenericParameters.Count != arity.Integers[0]) continue;
                    if (parameterTypes.TypeArrays.Count > 0 && !parameterTypes.Unknown
                        && parameterTypes.SymbolicGenericArrays.Count == 0
                        && !parameterTypes.HasObjectOwners && !parameterTypes.HasNonArrayArgumentValues
                        && !(parameterTypes.HasNullValues && method.Parameters.Count == 0)
                        && !parameterTypes.TypeArrays.Any(a => a.Length == method.Parameters.Count)) continue;
                    if (!MatchesFlags(method.IsPublic, method.IsStatic, flags)) continue;
                    AddSelectedSummaryMethod(result, method);
                }
                if (op is ReflectionOp.GetProperty or ReflectionOp.GetProperties or ReflectionOp.GetMember or ReflectionOp.GetMembers or ReflectionOp.GetDefaultMembers or ReflectionOp.InvokeMember)
                    foreach (var property in owner.Properties)
                        if (NameMatches(property.Name)) result.Members.Add(property);
                if (op is ReflectionOp.GetField or ReflectionOp.GetFields or ReflectionOp.GetMember or ReflectionOp.GetMembers
                    or ReflectionOp.GetDefaultMembers or ReflectionOp.InvokeMember)
                    foreach (var field in owner.Fields)
                        if (NameMatches(field.Name) && MatchesFlags(field.IsPublic, field.IsStatic, flags)) result.Members.Add(field);
                if (op is ReflectionOp.GetEvent or ReflectionOp.GetEvents or ReflectionOp.GetMember or ReflectionOp.GetMembers
                    or ReflectionOp.GetDefaultMembers or ReflectionOp.InvokeMember)
                    foreach (var item in owner.Events)
                        if (NameMatches(item.Name)) result.Members.Add(item);
                if (owner == type && (op is ReflectionOp.GetNestedType or ReflectionOp.GetNestedTypes
                    or ReflectionOp.GetMember or ReflectionOp.GetMembers or ReflectionOp.GetDefaultMembers))
                    foreach (var nested in owner.NestedTypes)
                    {
                        Spend();
                        if ((NameMatches(nested.Name) || selectedNames.Strings.Any(n => n == Name(nested)))
                            && MatchesNestedFlags(nested.IsNestedPublic, flags)) result.Types.Add(nested);
                    }
                if (op == ReflectionOp.GetInterface && owner == type)
                    foreach (var contract in InterfaceClosure(type))
                    {
                        Spend();
                        result.Unknown |= InterfaceNameIsUncertain(contract, selectedNames, flags);
                        if (NameMatches(contract.Name) || NameMatches(contract.FullName)
                            || NameMatches(Name(contract.GetElementType()))) result.Types.Add(contract);
                    }
            }
        }
    }

    private void BindTypeArray(Mono.Collections.Generic.Collection<GenericParameter> parameters,
        TypeReference[][] positions)
    {
        for (int i = 0; i < parameters.Count && i < positions.Length; i++)
            foreach (var argument in positions[i]) Bind(parameters[i], argument);
    }

    private static bool MatchesFlags(bool isPublic, bool isStatic, SummaryValue flags)
    {
        if (flags.Unknown || flags.Integers.Count != 1) return true;
        long value = flags.Integers[0];
        return (value & (isPublic ? ReflectionApis.Public : ReflectionApis.NonPublic)) != 0
            && (value & (isStatic ? ReflectionApis.Static : ReflectionApis.Instance)) != 0;
    }

    private static bool HasSummaryArguments(SummaryValue arguments) =>
        arguments.SymbolicGenericArrays.Count != 0
        || arguments.TypeArrays.Any(vector => vector.All(position => position.Length != 0));

    private bool BindSummaryArguments(Mono.Collections.Generic.Collection<GenericParameter> parameters, SummaryValue arguments)
    {
        bool bound = false;
        foreach (var alternative in arguments.TypeArrays)
            if (alternative.Length == parameters.Count && alternative.All(position => position.Length != 0))
            {
                BindTypeArray(parameters, alternative);
                bound = true;
            }
        foreach (var source in arguments.SymbolicGenericArrays)
        {
            BindGenericVector(parameters, source);
            bound = true;
        }
        return bound;
    }

    private void ApplyInstantiation(MethodDefinition caller, RSite site, SummaryValue target, SummaryValue arguments)
    {
        // Site identities are negative; summary term identities are nonnegative.
        bool isMethod = site.Api.Op == ReflectionOp.MakeGenericMethod;
        foreach (var reference in target.Types)
            if (IsSymbolic(reference))
            {
                if (HasSummaryArguments(arguments))
                    QueueDemand(CloseProjectedGeneric(reference, arguments, caller, ~site.Offset, isMethod),
                        isMethod ? Demand.Metadata : Demand.Metadata | Demand.ArrayLayout | Demand.PreservedUse);
            }
            else if (!isMethod && Resolve(reference) is { } definition)
            {
                BindSummaryArguments(definition.GenericParameters, arguments);
                ObserveArrayInitializerLayout(definition);
                NotePreservedReference(definition);
            }
        if (isMethod)
            foreach (var member in target.Members)
                if (member is MethodDefinition method) BindSummaryArguments(method.GenericParameters, arguments);
    }

    private void ApplySummary(MethodDefinition caller, ReflectionSummary summary)
    {
        _summaryValues.Clear();
        foreach (var site in summary.Sites)
        {
            Spend();
            var api = site.Api;
            if (!api.Recognized) { _summaryUnknown++; continue; }
            _reflectionRules++;
            if (ReflectionRoutes.ReadsReflectedAttributes(api.Kind)) RegisterSelectedAttributeSite(caller, summary, site);
            var target = SummaryValueOf(caller, summary, SiteOperand(site, api.Target));
            // Formation contributes bindings even when its metadata result is discarded.
            if (api.Kind == ReflectionOpKind.Instantiate)
                ApplyInstantiation(caller, site, target, SummaryValueOf(caller, summary, SiteOperand(site, api.Types)));
            bool executes = api.Kind is ReflectionOpKind.Execute or ReflectionOpKind.Bind or ReflectionOpKind.Construct;
            if (executes && api.Op == ReflectionOp.GetUninitializedObject)
                foreach (var allocated in target.Types) NoteAttributeAllocation(allocated);
            if (api.Kind == ReflectionOpKind.Bind)
            {
                var delegates = SummaryValueOf(caller, summary, SiteOperand(site, api.DelegateType));
                foreach (var delegateType in delegates.Types)
                    if (IsSymbolic(delegateType) || Resolve(delegateType) is { IsAbstract: false } definition && IsDelegate(definition))
                        QueueDemand(delegateType, Demand.Runtime);
            }
            if (api.Op is ReflectionOp.TypeGetType or ReflectionOp.AssemblyGetType
                or ReflectionOp.ActivatorCreateNamed or ReflectionOp.AssemblyCreateInstance)
            {
                var names = SummaryValueOf(caller, summary, SiteOperand(site, api.Name));
                var named = new SummaryValue { Unknown = names.Unknown };
                foreach (string name in names.Strings) ResolveTypeName(name, named, target);
                target = named;
                foreach (var reference in named.Types) NotePreservedReference(reference);
                if (names.Unknown || names.Strings.Count == 0) UnknownTypes();
            }
            if (api.Op == ReflectionOp.AssemblyGetTypes)
            {
                foreach (var module in target.Assemblies)
                    foreach (var type in AllTypes(module.Types)) Require(type, TypeState.Metadata);
                foreach (var owner in target.SymbolicAssemblyOwners)
                    QueueDemand(Project(owner, ProjectionKind.AssemblyTypes), Demand.Metadata);
                if (target.Unknown || target.Assemblies.Count == 0 && target.SymbolicAssemblyOwners.Count == 0) UnknownTypes();
                continue;
            }
            if (api.Kind is ReflectionOpKind.Lookup or ReflectionOpKind.Enumerate
                || api.Op is ReflectionOp.CreateDelegateNamed or ReflectionOp.InvokeMember
                || api.Kind == ReflectionOpKind.Construct || api.Op == ReflectionOp.RuntimeTypeDefaultStructCtor)
            {
                var names = api.Kind == ReflectionOpKind.Construct ? new SummaryValue()
                    : SummaryValueOf(caller, summary, SiteOperand(site, api.Name));
                var selected = new SummaryValue();
                var flags = SummaryValueOf(caller, summary, SiteOperand(site, api.Flags));
                if (api.Flags == ReflectionApi.Absent)
                {
                    flags.Integers.Add(api.DefaultFlags);
                    if (api.Op == ReflectionOp.RuntimeTypeDefaultCtor)
                    {
                        var publicOnly = SummaryValueOf(caller, summary, SiteOperand(site, 0));
                        if (publicOnly.Unknown || publicOnly.Integers.Count == 0 || publicOnly.Integers.Any(value => value == 0))
                            flags.Integers.Add(api.DefaultFlags | ReflectionApis.NonPublic);
                    }
                    if (api.NonPublic >= 0)
                    {
                        var nonPublic = SummaryValueOf(caller, summary, SiteOperand(site, api.NonPublic));
                        if (nonPublic.Unknown || nonPublic.Integers.Any(value => value != 0))
                            flags.Integers.Add(api.DefaultFlags | ReflectionApis.NonPublic);
                    }
                }
                var types = SummaryValueOf(caller, summary, SiteOperand(site, api.Types));
                var arity = SummaryValueOf(caller, summary, SiteOperand(site, api.Arity));
                SelectMembers(selected, target, names, api.Op, flags, types, arity,
                    api.Kind != ReflectionOpKind.Construct && api.Name != ReflectionApi.Absent);
                foreach (var reference in target.Types)
                {
                    if (IsSymbolic(reference) || api.Op == ReflectionOp.RuntimeTypeDefaultStructCtor)
                    {
                        var owner = api.Op == ReflectionOp.CreateDelegateNamed && MayNameConstructor(names)
                            ? Project(reference, ProjectionKind.Ancestors) : reference;
                        QueueDemand(owner, executes ? DemandFor(api, flags, names)
                            : ObservesStaticConstructor(api.Op, flags, names) ? Demand.Runtime : Demand.Members);
                    }
                    else if (Resolve(reference) is { } type)
                    {
                        Require(type, executes && api.Kind == ReflectionOpKind.Construct ? TypeState.Runtime : TypeState.Metadata);
                        if (!executes && (names.Unknown || names.Strings.Count == 0)) KeepMemberAncestors(type);
                    }
                }
                foreach (var member in selected.Members) ApplyReflectedMember(member, executes, api);
                if (api.Op == ReflectionOp.CtorInvoke)
                    foreach (var member in target.Members)
                        if (member is MethodDefinition { IsConstructor: true }) ApplyReflectedMember(member, true, api);
                foreach (var type in selected.Types) MarkSignatureType(type);
                if (target.Unknown || target.Types.Count == 0 && target.Members.Count == 0)
                {
                    _summaryUnknown++;
                    if (!executes && api.Kind is ReflectionOpKind.Lookup or ReflectionOpKind.Enumerate) UnknownMembers();
                }
                continue;
            }
            foreach (var reference in target.Types)
            {
                if (IsSymbolic(reference)) QueueDemand(reference, executes ? DemandFor(api) : Demand.Metadata);
                else if (executes && api.Op is ReflectionOp.GetUninitializedObject or ReflectionOp.RunClassConstructor) MarkType(reference);
                else MarkSignatureType(reference);
            }
            foreach (var member in target.Members) ApplyReflectedMember(member, executes, api);
            if (target.Unknown)
            {
                _summaryUnknown++;
                if (!executes && api.Kind is ReflectionOpKind.Lookup or ReflectionOpKind.Enumerate) UnknownMembers();
            }
        }
    }

    private string? DefaultMemberName(TypeDefinition type)
    {
        foreach (var owner in Ancestors(type))
            foreach (var attribute in owner.CustomAttributes)
            {
                Spend();
                if (attribute.AttributeType.FullName != "System.Reflection.DefaultMemberAttribute") continue;
                try
                {
                    if (attribute.ConstructorArguments.Count > 0 && attribute.ConstructorArguments[0].Value is string name)
                        return name;
                }
                catch (AssemblyResolutionException)
                {
                    // An unreadable name admits every kind conservatively.
                    return "*";
                }
            }
        return null;
    }

    private static bool MatchesNestedFlags(bool isPublic, SummaryValue flags) => flags.Unknown || flags.Integers.Count != 1
        || (flags.Integers[0] & (isPublic ? ReflectionApis.Public : ReflectionApis.NonPublic)) != 0;

    private static SummaryValue ConstructionFlags(SummaryValue flags)
    {
        var normalized = new SummaryValue { Unknown = flags.Unknown };
        foreach (long original in flags.Integers)
            normalized.Integers.Add(((original & 0xFF) == 0 ? original | ReflectionApis.PublicInstance : original) | ReflectionApis.Instance);
        return normalized;
    }

    private static bool SelectsStaticConstructors(ReflectionOp op, SummaryValue flags) => op switch
    {
        ReflectionOp.GetConstructor or ReflectionOp.GetConstructors => flags.Unknown
            || flags.Integers.Count == 0 || flags.Integers.Any(value => value == ReflectionApis.TypeInitializerLookup),
        ReflectionOp.GetMember or ReflectionOp.GetMembers or ReflectionOp.GetDefaultMembers or ReflectionOp.InvokeMember => true,
        _ => false,
    };

    private static bool ObservesStaticConstructor(ReflectionOp op, SummaryValue flags, SummaryValue names)
    {
        if (op == ReflectionOp.TypeInitializer) return true;
        if (!SelectsStaticConstructors(op, flags)) return false;
        if (!flags.Unknown && flags.Integers.Count > 0
            && !flags.Integers.Any(value => (value & ReflectionApis.TypeInitializerLookup) == ReflectionApis.TypeInitializerLookup)) return false;
        if (op is ReflectionOp.GetConstructor or ReflectionOp.GetConstructors or ReflectionOp.GetMembers or ReflectionOp.GetDefaultMembers)
            return true;
        return names.Unknown || names.Strings.Count == 0 || names.Strings.Any(name =>
            string.Equals(name, ".cctor", StringComparison.OrdinalIgnoreCase)
                || name.EndsWith('*') && ".cctor".StartsWith(name[..^1], StringComparison.OrdinalIgnoreCase));
    }

    private static bool MatchesNamedBindingStorage(bool isStatic, SummaryValue flags) => flags.Unknown
        || flags.Integers.Count == 0 || flags.Integers.Any(value => ((value & ReflectionApis.Static) != 0) == isStatic);

    private static bool MayNameConstructor(SummaryValue names) => names.Unknown || names.Strings.Count == 0
        || names.Strings.Any(name => string.Equals(name, ".ctor", StringComparison.OrdinalIgnoreCase)
            || string.Equals(name, ".cctor", StringComparison.OrdinalIgnoreCase));

    private static bool MaySelectConstructor(SummaryValue names) => MayNameConstructor(names)
        || names.Strings.Any(name => name.EndsWith('*') && (".ctor".StartsWith(name[..^1], StringComparison.OrdinalIgnoreCase)
            || ".cctor".StartsWith(name[..^1], StringComparison.OrdinalIgnoreCase)));

    private static bool MayNameInstanceConstructor(SummaryValue? names) => names is null || names.Unknown
        || names.Strings.Count == 0 || names.Strings.Any(name => string.Equals(name, ".ctor", StringComparison.OrdinalIgnoreCase));

    private static SummaryValue InvokeMemberFlags(SummaryValue flags)
    {
        var normalized = new SummaryValue { Unknown = flags.Unknown };
        foreach (long original in flags.Integers)
        {
            long value = original;
            if ((value & (ReflectionApis.Public | ReflectionApis.NonPublic)) == 0) value |= ReflectionApis.Public;
            if ((value & (ReflectionApis.Instance | ReflectionApis.Static)) == 0) value |= ReflectionApis.Instance | ReflectionApis.Static;
            normalized.Integers.Add(value);
        }
        return normalized;
    }

    private static bool MayCreateInstance(SummaryValue flags) => flags.Unknown || flags.Integers.Count == 0
        || flags.Integers.Any(value => (value & 0x200) != 0);

    private static Demand DemandFor(ReflectionApi api, SummaryValue? flags = null, SummaryValue? names = null)
    {
        if (api.Op == ReflectionOp.CreateDelegateNamed)
            return Demand.MethodBodies | (MayNameInstanceConstructor(names) ? Demand.Ctors : Demand.None);
        if (api.Op == ReflectionOp.InvokeMember)
        {
            // Admission's named selection includes every member kind; construction
            // additionally ignores the name when CreateInstance can be present.
            var result = Demand.MethodBodies | Demand.AccessorBodies | Demand.FieldRead;
            if (flags is null || MayCreateInstance(flags) || names is null || names.Unknown || names.Strings.Count == 0
                || names.Strings.Any(name => string.Equals(name, ".ctor", StringComparison.OrdinalIgnoreCase) || name.EndsWith('*')
                    && ".ctor".StartsWith(name[..^1], StringComparison.OrdinalIgnoreCase))) result |= Demand.Ctors;
            return result;
        }
        if (api.Op == ReflectionOp.RuntimeTypeDefaultCtor)
            return flags is null || flags.Unknown || flags.Integers.Count == 0
                || flags.Integers.Any(value => (value & ReflectionApis.NonPublic) != 0)
                ? Demand.DefaultCtorAnyVisibility : Demand.DefaultCtor;
        return api.Op is ReflectionOp.ActivatorCreateGeneric or ReflectionOp.RuntimeTypeCreateInstanceOfT
            or ReflectionOp.RuntimeTypeDefaultStructCtor ? Demand.DefaultCtor
            : api.Op == ReflectionOp.FieldGet ? Demand.FieldRead
            : api.Op == ReflectionOp.FieldSet ? Demand.Runtime
            : api.Kind == ReflectionOpKind.Construct || api.Op == ReflectionOp.CtorInvoke ? Demand.Ctors
            : api.Op is ReflectionOp.PropertyGet or ReflectionOp.PropertySet or ReflectionOp.EventAdd or ReflectionOp.EventRemove
                ? Demand.AccessorBodies : Demand.MethodBodies;
    }

    private void ApplyReflectedMember(IMemberDefinition member, bool executes, ReflectionApi api)
    {
        if (api.Kind is ReflectionOpKind.Lookup or ReflectionOpKind.Enumerate
            && member is not TypeDefinition && member.DeclaringType is { } owner)
            NoteCopiedListedOwner(owner);
        switch (member)
        {
            case MethodDefinition method:
                if (executes || method.IsConstructor && method.IsStatic)
                {
                    // An explicitly observed initializer needs its original body:
                    // metadata-only initializer rows cannot be represented by stubs.
                    MarkMethod(method);
                    if (executes) NoteReflectionInvokeBoxes(method);
                }
                else KeepMethodSignature(method);
                break;
            case FieldDefinition field:
                if (executes)
                {
                    MarkField(field);
                    if (api.Op != ReflectionOp.FieldSet) QueueDemand(field.FieldType, Demand.Boxed);
                }
                else { Require(field.DeclaringType, TypeState.Metadata); _fields.Add(field); MarkSignatureType(field.FieldType); }
                break;
            case PropertyDefinition property:
                if (executes) MarkProperty(property, true, true);
                else KeepPropertySignature(property);
                break;
            case EventDefinition item:
                if (executes) MarkEvent(item); else KeepEventSignature(item);
                break;
            case TypeDefinition type: Require(type, TypeState.Metadata); break;
        }
    }
}

internal sealed partial class AssemblyDiet
{
    private void KeepReflectionBox(TypeReference reference)
    {
        if (reference is GenericParameter parameter) { AddDemand(parameter, Demand.Boxed); return; }
        if (reference is ByReferenceType byReference) { QueueDemand(byReference.ElementType, Demand.Boxed); return; }
        if (reference is GenericInstanceType nullable && nullable.ElementType.FullName == "System.Nullable`1"
            && nullable.GenericArguments.Count == 1)
        {
            QueueDemand(nullable.GenericArguments[0], Demand.Boxed);
            return;
        }
        if (reference is PointerType)
        {
            foreach (var assembly in _assemblies)
                if (assembly.Assembly.MainModule.GetType("System.Reflection.Pointer") is { } pointer) MarkType(pointer);
            return;
        }
        if (Resolve(reference) is { IsValueType: true } type
            && !HasAttribute(type, "System.Runtime.CompilerServices.IsByRefLikeAttribute"))
        {
            NoteAttributeAllocation(reference);
            MarkType(reference);
        }
    }

    private void NoteReflectionInvokeBoxes(MethodDefinition method)
    {
        QueueDemand(method.ReturnType, Demand.Boxed);
        foreach (var parameter in method.Parameters)
            if (parameter.ParameterType is ByReferenceType) QueueDemand(parameter.ParameterType, Demand.Boxed);
    }

    private bool _delegateInvokeBoxesUsed;
    private readonly List<TypeDefinition> _runtimeDelegates = new();
    private readonly HashSet<TypeDefinition> _delegateInvokeBoxes = new();

    private void NoteDelegateInvokeCapability(MethodReference target)
    {
        if (_delegateInvokeBoxesUsed || ClassifyReflectionTarget(target) is not { } api) return;
        if (api.Op != ReflectionOp.DynamicInvoke && !ReflectionRoutes.RunsReflectedMethod(api.Op)
            && !ReflectionRoutes.BindsReflectedMethod(api.Op)) return;
        CheckMutable();
        Spend();
        _delegateInvokeBoxesUsed = true;
        // Invocation capabilities consume every allocated delegate's Invoke signature,
        // including when the reflected receiver is unknown.
        foreach (var type in _runtimeDelegates) NoteDelegateInvokeBoxes(type);
    }

    private void NoteDelegateInvokeBoxes(TypeDefinition type)
    {
        CheckMutable();
        if (!_delegateInvokeBoxes.Add(type)) return;
        Spend();
        foreach (var method in type.Methods)
        {
            Spend();
            if (method.Name == "Invoke") NoteReflectionInvokeBoxes(method);
        }
    }

    private readonly List<TypeReference> _arrayElements = new();
    private readonly HashSet<string> _arrayElementKeys = new(StringComparer.Ordinal);
    private bool _nonGenericArraySearch, _arrayFactoryUsed, _runtimeHandleBoxUsed;

    private bool NeedsMetadataValueDispatch => _runtimeHandleBoxUsed || _nonGenericArraySearch && _arrayFactoryUsed;

    private void NoteArrayElement(TypeReference element)
    {
        if (!_arrayElementKeys.Add(ArgumentKey(element))) return;
        _arrayElements.Add(element);
        if (_nonGenericArraySearch) QueueDemand(element, Demand.Boxed);
    }

    private void ApplyConservativeValueDispatch(TypeDefinition type)
    {
        if (UsesReachedCopyTraversal(type) && CopiedRuntimeOwnedType(type)) return;
        // Runtime array factories and handle boxes can select emitted layouts that
        // have no executable type token. Only boxable value payloads participate.
        if (NeedsMetadataValueDispatch && type.IsValueType
            && !HasAttribute(type, "System.Runtime.CompilerServices.IsByRefLikeAttribute"))
            QueueDemand(type, Demand.Boxed);
    }

    private void NoteImplicitDispatchOperation(MethodReference target)
    {
        bool before = NeedsMetadataValueDispatch;
        bool search = false;
        if (target.DeclaringType.FullName == "System.Array")
        {
            if (target is not GenericInstanceMethod && target.Name is "IndexOf" or "LastIndexOf"
                && !_nonGenericArraySearch)
            {
                _nonGenericArraySearch = true;
                search = true;
            }
            if (target.Name is "CreateInstance" or "CreateInstanceFromArrayType") _arrayFactoryUsed = true;
        }
        if (target.DeclaringType.FullName == "System.Runtime.CompilerServices.RuntimeHelpers"
            && target.Name == "Box" && target.Parameters.Count == 2) _runtimeHandleBoxUsed = true;
        if (search)
            foreach (var element in _arrayElements) QueueDemand(element, Demand.Boxed);
        if (!before && NeedsMetadataValueDispatch)
        {
            foreach (var type in _observedCopiedLayouts) ApplyConservativeValueDispatch(type);
            foreach (var assembly in _assemblies)
                foreach (var type in AllTypes(assembly.Assembly.MainModule.Types))
                {
                    Spend();
                    if (Node(type) is { State: not TypeState.None }) ApplyConservativeValueDispatch(type);
                }
        }
    }
}

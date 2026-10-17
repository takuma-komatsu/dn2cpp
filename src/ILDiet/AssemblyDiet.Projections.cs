using Mono.Cecil;

namespace Dn2Cpp;

internal sealed partial class AssemblyDiet
{
    private enum ProjectionKind : byte
    {
        Base, Ancestors, GenericArgument, MethodGenericArgument, Methods, Constructors, Element, GenericDefinition, GenericClosure, GenericMethodClosure, Interfaces, NestedTypes, AssemblyTypes,
        FieldTypes, PropertyTypes, ReturnTypes, ParameterTypes, BoxedRuntimeType,
    }

    // Projection targets are original reference blobs or formal parameters. Subscribing
    // to binding edges avoids constructing an unbounded family of closed contexts.
    private sealed class ProjectionNode
    {
        internal required ProjectedTypeReference Reference;
        internal required ProjectionKind Kind;
        internal required int Index;
        internal required int Arity;
        internal Demand Demands, Applied;
        internal readonly HashSet<TypeReference> Sources = new();
        internal readonly HashSet<TypeReference> TargetSet = new();
        internal readonly List<TypeReference> Targets = new();
        internal readonly List<ProjectionNode> Dependents = new();
        internal readonly List<TypeReference[][]> GenericVectors = new();
    }

    private sealed class ProjectedTypeReference : TypeReference
    {
        internal ProjectionNode Node = null!;
        internal ProjectedTypeReference(TypeReference source, int index)
            : base("", "<reflection-projection-" + index + ">", source.Module, source.Scope) { }
    }

    private readonly Dictionary<(TypeReference Source, ProjectionKind Kind, int Index, int Arity), ProjectionNode> _projections = new();
    private readonly Dictionary<TypeDefinition, TypeReference[]> _interfaceClosures = new();

    private static bool IsSymbolic(TypeReference reference) => reference is GenericParameter or ProjectedTypeReference;

    private static ProjectionKind DetermineGenericArgProjection(TypeReference source) =>
        source is ProjectedTypeReference { Node.Kind: ProjectionKind.Methods }
            ? ProjectionKind.MethodGenericArgument : ProjectionKind.GenericArgument;

    private TypeReference Project(TypeReference source, ProjectionKind kind, int index = -1, int arity = -1)
    {
        EnsureOriginalModels();
        var key = (source, kind, index, arity);
        if (_projections.TryGetValue(key, out var known)) return known.Reference;
        CheckMutable();
        Spend();
        var reference = new ProjectedTypeReference(source, _projections.Count);
        var node = new ProjectionNode { Reference = reference, Kind = kind, Index = index, Arity = arity };
        reference.Node = node;
        _projections.Add(key, node);
        QueueProjectionSource(node, source);
        return reference;
    }

    private void RequireProjection(ProjectionNode node, Demand demands)
    {
        CheckMutable();
        // A selected constructor remains metadata until its reflection value is invoked or bound.
        if (node.Kind == ProjectionKind.Constructors && (demands & Demand.MethodBodies) != 0)
            demands = (demands & ~Demand.MethodBodies) | Demand.Ctors;
        if ((node.Demands & demands) == demands) return;
        node.Demands |= demands;
        _work.Enqueue(new Work(node));
    }

    private void QueueProjectionSource(ProjectionNode node, TypeReference source)
    {
        CheckMutable();
        if (!node.Sources.Add(source)) return;
        Spend();
        _work.Enqueue(new Work(node, source));
    }

    private void ApplyProjection(ProjectionNode node)
    {
        RequireCompletedModelActivation("ApplyProjection");
        var delta = node.Demands & ~node.Applied;
        if (delta == Demand.None) return;
        Spend();
        _demandRules++;
        node.Applied |= delta;
        foreach (var target in node.Targets) QueueDemand(target, delta);
    }

    private void ApplyProjectionSource(ProjectionNode node, TypeReference source)
    {
        RequireCompletedModelActivation("ApplyProjectionSource");
        if (source is GenericParameter parameter)
        {
            var parent = Parameter(parameter);
            if (!parent.Projections.Contains(node)) { Spend(); parent.Projections.Add(node); }
            foreach (var bound in parent.Bound) QueueProjectionSource(node, bound);
            return;
        }
        if (source is ProjectedTypeReference projected)
        {
            var parent = projected.Node;
            if (!parent.Dependents.Contains(node)) { Spend(); parent.Dependents.Add(node); }
            foreach (var target in parent.Targets) QueueProjectionSource(node, target);
            return;
        }
        switch (node.Kind)
        {
            case ProjectionKind.BoxedRuntimeType:
                ProjectionTarget(node, BoxedUnderlyingType(source));
                break;
            case ProjectionKind.Ancestors:
            {
                // Bind the original constructed reference before emitting definitions.
                MarkSignatureType(source);
                if (Resolve(source) is { } owner)
                    foreach (var ancestor in Ancestors(owner))
                    {
                        Spend();
                        ProjectionTarget(node, ancestor);
                    }
                break;
            }
            case ProjectionKind.Base:
            {
                if (BaseReference(source) is { } parent) ProjectionTarget(node, parent);
                break;
            }
            case ProjectionKind.GenericArgument:
            {
                var arguments = GenericArguments(source);
                if (node.Arity >= 0 && arguments.Length != node.Arity) break;
                if (node.Index >= 0)
                {
                    if (node.Index < arguments.Length) ProjectionTarget(node, arguments[node.Index]);
                }
                else foreach (var argument in arguments) ProjectionTarget(node, argument);
                break;
            }
            case ProjectionKind.Methods:
            case ProjectionKind.Constructors:
                ProjectionTarget(node, source);
                break;
            case ProjectionKind.MethodGenericArgument:
            {
                if (Resolve(source) is not { } definition) break;
                foreach (var owner in Ancestors(definition))
                    foreach (var method in owner.Methods)
                    {
                        Spend();
                        if (node.Arity >= 0 && node.Arity != method.GenericParameters.Count) continue;
                        if (node.Index >= 0)
                        {
                            if (node.Index < method.GenericParameters.Count) ProjectionTarget(node, method.GenericParameters[node.Index]);
                        }
                        else foreach (var formal in method.GenericParameters) ProjectionTarget(node, formal);
                    }
                break;
            }
            case ProjectionKind.Element:
                if (source is ArrayType or ByReferenceType or PointerType)
                    ProjectionTarget(node, ((TypeSpecification)source).ElementType);
                break;
            case ProjectionKind.GenericDefinition:
                if (source is GenericInstanceType generic) ProjectionTarget(node, generic.ElementType);
                else if (Resolve(source) is { HasGenericParameters: true }) ProjectionTarget(node, source);
                break;
            case ProjectionKind.GenericClosure:
            {
                if (Resolve(source) is not { HasGenericParameters: true } definition) break;
                foreach (var arguments in node.GenericVectors)
                    if (arguments.Length == definition.GenericParameters.Count)
                    {
                        BindTypeArray(definition.GenericParameters, arguments);
                        ProjectionTarget(node, definition);
                    }
                break;
            }
            case ProjectionKind.GenericMethodClosure:
            {
                if (Resolve(source) is not { } definition) break;
                foreach (var owner in Ancestors(definition))
                    foreach (var method in owner.Methods)
                    {
                        Spend();
                        if (!method.HasGenericParameters) continue;
                        foreach (var arguments in node.GenericVectors)
                            if (arguments.Length == method.GenericParameters.Count)
                            {
                                BindTypeArray(method.GenericParameters, arguments);
                                ProjectionTarget(node, definition);
                            }
                    }
                break;
            }
            case ProjectionKind.NestedTypes:
            {
                if (Resolve(source) is { } owner)
                    foreach (var nested in owner.NestedTypes) ProjectionTarget(node, nested);
                break;
            }
            case ProjectionKind.AssemblyTypes:
            {
                if (Resolve(source) is { } owner)
                    foreach (var assemblyType in AllTypes(owner.Module.Types)) ProjectionTarget(node, assemblyType);
                break;
            }
            case ProjectionKind.Interfaces:
            {
                if (Resolve(source) is { } owner)
                    foreach (var contract in InterfaceClosure(owner)) ProjectionTarget(node, contract);
                break;
            }
            default:
                if (Resolve(source) is not { } type) break;
                foreach (var owner in Ancestors(type))
                {
                    Spend();
                    if (node.Kind == ProjectionKind.FieldTypes)
                        foreach (var field in owner.Fields)
                        {
                            Spend();
                            ProjectionTarget(node, field.FieldType);
                        }
                    else if (node.Kind == ProjectionKind.PropertyTypes)
                        foreach (var property in owner.Properties)
                        {
                            Spend();
                            ProjectionTarget(node, property.PropertyType);
                        }
                    else
                        foreach (var method in owner.Methods)
                        {
                            Spend();
                            if (node.Kind == ProjectionKind.ReturnTypes) ProjectionTarget(node, method.ReturnType);
                            else if (node.Index >= 0)
                            {
                                if (node.Index < method.Parameters.Count) ProjectionTarget(node, method.Parameters[node.Index].ParameterType);
                            }
                            else foreach (var signatureParameter in method.Parameters)
                            {
                                Spend();
                                ProjectionTarget(node, signatureParameter.ParameterType);
                            }
                        }
                }
                break;
        }
    }

    private void ProjectionTarget(ProjectionNode node, TypeReference target)
    {
        if (!node.TargetSet.Add(target)) return;
        Spend();
        node.Targets.Add(target);
        ModelProjectionChanged(node, target);
        if (_promotedModelReferences.Contains(node.Reference)) PromoteModelReference(target);
        if (_attributeAllocationProjections.Contains(node)) NoteAttributeAllocation(target);
        ReplayAttributeIdentityProjection(node, target);
        AttributeBindingsChanged();
        QueueDemand(target, node.Demands);
        foreach (var child in node.Dependents) QueueProjectionSource(child, target);
    }

    private TypeReference BoxedRuntimeType(TypeReference reference) => IsSymbolic(reference)
        ? Project(reference, ProjectionKind.BoxedRuntimeType) : BoxedUnderlyingType(reference);

    private static TypeReference BoxedUnderlyingType(TypeReference reference) =>
        reference is GenericInstanceType nullable && nullable.ElementType.FullName == "System.Nullable`1"
            && nullable.GenericArguments.Count == 1 ? nullable.GenericArguments[0] : reference;

    private TypeReference? BaseReference(TypeReference reference) => reference is ArrayType
        ? _resolver.FindDefinition("System.Array")
        : reference is ByReferenceType or PointerType ? null : Resolve(reference)?.BaseType;

    private TypeReference[] GenericArguments(TypeReference reference)
    {
        if (reference is GenericInstanceType generic) return generic.GenericArguments.ToArray();
        if (reference is TypeSpecification) return Array.Empty<TypeReference>();
        return Resolve(reference)?.GenericParameters.Cast<TypeReference>().ToArray() ?? Array.Empty<TypeReference>();
    }

    private void BindGenericVector(Mono.Collections.Generic.Collection<GenericParameter> parameters, TypeReference source)
    {
        for (int i = 0; i < parameters.Count; i++)
            Bind(parameters[i], Project(source, DetermineGenericArgProjection(source), i, parameters.Count));
    }

    private readonly Dictionary<(TypeReference Source, MethodDefinition Method, int Term, bool IsMethod), TypeReference> _genericClosures = new();
    private int[]? _genericArities;

    private TypeReference CloseProjectedGeneric(TypeReference source, SummaryValue arguments, MethodDefinition method, int term, bool isMethod = false)
    {
        var key = (source, method, term, isMethod);
        if (_genericClosures.TryGetValue(key, out var known)) return known;
        var reference = (ProjectedTypeReference)Project(source, isMethod ? ProjectionKind.GenericMethodClosure : ProjectionKind.GenericClosure, _genericClosures.Count);
        _genericClosures.Add(key, reference);
        foreach (var vector in arguments.TypeArrays)
            if (vector.All(position => position.Length != 0)) reference.Node.GenericVectors.Add(vector);
        if (arguments.SymbolicGenericArrays.Count != 0)
        {
            if (_genericArities is null)
            {
                var arities = new List<int>();
                foreach (var assembly in _assemblies)
                    foreach (var type in AllTypes(assembly.Assembly.MainModule.Types))
                    {
                        Spend();
                        int arity = type.GenericParameters.Count;
                        if (arity > 0 && !arities.Contains(arity)) arities.Add(arity);
                        foreach (var candidate in type.Methods)
                        {
                            Spend();
                            arity = candidate.GenericParameters.Count;
                            if (arity > 0 && !arities.Contains(arity)) arities.Add(arity);
                        }
                    }
                arities.Sort();
                _genericArities = arities.ToArray();
            }
            // The finite metadata arities fix every vector before any target binds.
            foreach (var owner in arguments.SymbolicGenericArrays)
                foreach (int arity in _genericArities)
                {
                    var vector = new TypeReference[arity][];
                    for (int i = 0; i < arity; i++)
                        vector[i] = new[] { Project(owner, DetermineGenericArgProjection(owner), i, arity) };
                    reference.Node.GenericVectors.Add(vector);
                }
        }
        return reference;
    }

    private TypeReference[] InterfaceClosure(TypeDefinition type)
    {
        if (_interfaceClosures.TryGetValue(type, out var known)) return known;
        var result = new List<TypeReference>();
        var seen = new HashSet<TypeDefinition>();
        var pending = new Queue<TypeDefinition>();
        pending.Enqueue(type);
        while (pending.Count != 0)
        {
            var owner = pending.Dequeue();
            if (!seen.Add(owner)) continue;
            Spend();
            if (owner.BaseType is not null)
            {
                MarkSignatureType(owner.BaseType);
                if (Resolve(owner.BaseType) is { } parent) pending.Enqueue(parent);
            }
            foreach (var implementation in owner.Interfaces)
            {
                Spend();
                var reference = implementation.InterfaceType;
                result.Add(reference);
                // Original bindings carry generic arguments along inherited edges.
                MarkSignatureType(reference);
                if (Resolve(reference) is { } contract) pending.Enqueue(contract);
            }
        }
        var closure = result.ToArray();
        _interfaceClosures.Add(type, closure);
        return closure;
    }
}

using Mono.Cecil;

namespace Dn2Cpp;

internal sealed partial class AssemblyDiet
{
    private readonly HashSet<TypeReference> _mediumReferences = new();
    private readonly HashSet<TypeDefinition> _mediumTypes = new();
    private readonly Queue<TypeDefinition> _mediumPending = new();
    private readonly Dictionary<TypeDefinition, (TypeDefinition? Parent, MethodReference? Source)> _mediumOrigins = new();
    private TypeDefinition? _mediumOwner;
    private MethodReference? _mediumSource;

    private void NoteMediumType(TypeReference? reference)
    {
        if (_request.StrippingLevel != StrippingLevel.Medium || reference is null
            || !_mediumReferences.Add(reference)) return;
        CheckMutable();
        Spend();
        if (reference is GenericParameter or ProjectedTypeReference)
        {
            QueueDemand(reference, Demand.TypeWide);
            return;
        }
        // Metadata resolution may discover other definitions; only this explicit
        // static edge carries type-wide authority into their generic bindings.
        RequireType(reference, TypeState.Metadata);
        if (reference is GenericInstanceType generic)
            foreach (var argument in generic.GenericArguments) NoteMediumType(argument);
        if (reference is FunctionPointerType pointer)
        {
            NoteMediumType(pointer.ReturnType);
            foreach (var parameter in pointer.Parameters) NoteMediumType(parameter.ParameterType);
            return;
        }
        if (reference is TypeSpecification specification)
        {
            NoteMediumType(specification.ElementType);
            return;
        }
        if (Resolve(reference) is { } type && _mediumTypes.Add(type))
        {
            var parent = _mediumOwner;
            if (parent is null && _mediumSource is not null && Resolve(_mediumSource.DeclaringType) is { } owner
                && owner != type && _mediumOrigins.ContainsKey(owner)) parent = owner;
            _mediumOrigins.Add(type, (parent == type ? null : parent, _mediumSource));
            _mediumPending.Enqueue(type);
        }
    }

    private void NoteMediumMethod(MethodReference method)
    {
        if (_request.StrippingLevel != StrippingLevel.Medium) return;
        var previous = _mediumSource;
        _mediumSource ??= method;
        try
        {
            NoteMediumType(method.DeclaringType);
            NoteMediumType(method.ReturnType);
            foreach (var parameter in method.Parameters) NoteMediumType(parameter.ParameterType);
            if (method is GenericInstanceMethod generic)
                foreach (var argument in generic.GenericArguments) NoteMediumType(argument);
        }
        finally { _mediumSource = previous; }
    }

    private bool ApplyMediumTypes()
    {
        if (_mediumPending.Count == 0) return false;
        while (_mediumPending.Count != 0)
        {
            var type = _mediumPending.Dequeue();
            var previousOwner = _mediumOwner;
            var previousSource = _mediumSource;
            _mediumOwner = type;
            _mediumSource = null;
            try { ApplyMediumType(type); }
            catch (NotSupportedException error)
            {
                throw new NotSupportedException(error.Message + MediumRetentionDiagnostic(type), error);
            }
            finally
            {
                _mediumOwner = previousOwner;
                _mediumSource = previousSource;
            }
        }
        return true;
    }

    private void ApplyMediumType(TypeDefinition type)
    {
        Spend();
        Require(type, TypeState.Metadata | TypeState.Members);
        NoteMediumType(type.BaseType);
        NoteMediumAttributes(type);
        foreach (var parameter in type.GenericParameters) NoteMediumParameter(parameter);
        foreach (var implementation in type.Interfaces)
        {
            NoteMediumType(implementation.InterfaceType);
            NoteMediumAttributes(implementation);
        }
        foreach (var field in type.Fields)
        {
            _fields.Add(field);
            NoteMediumType(field.FieldType);
            NoteMediumAttributes(field);
            MarkMarshal(field);
        }
        foreach (var property in type.Properties)
        {
            KeepPropertySignature(property);
            NoteMediumType(property.PropertyType);
            NoteMediumAttributes(property);
            foreach (var parameter in property.Parameters)
            {
                NoteMediumType(parameter.ParameterType);
                NoteMediumAttributes(parameter);
            }
        }
        foreach (var item in type.Events)
        {
            KeepEventSignature(item);
            NoteMediumType(item.EventType);
            NoteMediumAttributes(item);
        }
        foreach (var method in type.Methods)
        {
            NoteMediumMethod(method);
            NoteMediumAttributes(method);
            NoteMediumAttributes(method.MethodReturnType);
            foreach (var parameter in method.Parameters) NoteMediumAttributes(parameter);
            foreach (var parameter in method.GenericParameters) NoteMediumParameter(parameter);
            // The existing intrinsic/cut routes still own bodies that are
            // supplied by the runtime instead of the managed implementation.
            MarkMethod(method);
        }
    }

    private string MediumRetentionDiagnostic(TypeDefinition type)
    {
        var chain = new List<string>();
        var seen = new HashSet<TypeDefinition>();
        for (TypeDefinition? current = type; current is not null && seen.Add(current);)
        {
            chain.Add(current.Module.Assembly.Name.Name + "::" + Name(current));
            if (chain.Count == 32)
            {
                chain.Add("further ancestors omitted");
                break;
            }
            if (!_mediumOrigins.TryGetValue(current, out var origin)) break;
            if (origin.Parent is null && origin.Source is not null)
                chain.Add(origin.Source.FullName);
            current = origin.Parent;
        }
        return " [Medium type retention: " + string.Join(" <- ", chain) + "]";
    }

    private void NoteMediumParameter(GenericParameter parameter)
    {
        NoteMediumAttributes(parameter);
        foreach (var constraint in parameter.Constraints)
        {
            NoteMediumType(constraint.ConstraintType);
            NoteMediumAttributes(constraint);
        }
    }

    private void NoteMediumAttributes(ICustomAttributeProvider provider)
    {
        if (!provider.HasCustomAttributes) return;
        foreach (var attribute in provider.CustomAttributes)
        {
            NoteMediumType(attribute.AttributeType);
            try
            {
                foreach (var argument in attribute.ConstructorArguments) NoteMediumAttributeArgument(argument);
                foreach (var argument in attribute.Fields) NoteMediumAttributeArgument(argument.Argument);
                foreach (var argument in attribute.Properties) NoteMediumAttributeArgument(argument.Argument);
            }
            catch (AssemblyResolutionException)
            {
                // Attribute blobs from assemblies outside the load set stay opaque.
            }
        }
    }

    private void NoteMediumAttributeArgument(CustomAttributeArgument argument)
    {
        NoteMediumType(argument.Type);
        if (argument.Value is TypeReference type) NoteMediumType(type);
        else if (argument.Value is CustomAttributeArgument nested) NoteMediumAttributeArgument(nested);
        else if (argument.Value is CustomAttributeArgument[] arguments)
            foreach (var value in arguments) NoteMediumAttributeArgument(value);
    }
}

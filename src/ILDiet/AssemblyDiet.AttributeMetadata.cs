using Mono.Cecil;

namespace Dn2Cpp;

internal sealed partial class AssemblyDiet
{
    private int _passiveAttributeMetadataDepth;
    private readonly HashSet<TypeDefinition> _passiveAttributeTypes = new();
    private readonly HashSet<TypeReference> _passiveAttributeReferences = new();
    private readonly HashSet<MethodDefinition> _passiveAttributeMethods = new();
    private readonly HashSet<TypeDefinition> _independentMetadataTypes = new();
    private readonly HashSet<MethodDefinition> _independentMetadataMethods = new();
    private readonly Queue<SelectedAttributeRow> _attributeMetadataPending = new();

    private void NotePassiveAttributeType(TypeReference? reference)
    {
        if (reference is null || !_passiveAttributeReferences.Add(reference)) return;
        Spend();
        if (reference is GenericParameter or ProjectedTypeReference) return;
        if (reference is GenericInstanceType generic)
            foreach (var argument in generic.GenericArguments) NotePassiveAttributeType(argument);
        if (reference is IModifierType modifier) NotePassiveAttributeType(modifier.ModifierType);
        if (reference is FunctionPointerType pointer)
        {
            NotePassiveAttributeType(pointer.ReturnType);
            foreach (var parameter in pointer.Parameters) NotePassiveAttributeType(parameter.ParameterType);
            return;
        }
        if (reference is TypeSpecification specification)
        {
            NotePassiveAttributeType(specification.ElementType);
            return;
        }
        if (Resolve(reference) is { } type) _passiveAttributeTypes.Add(type);
    }

    private void KeepPassiveAttributeProvider(ICustomAttributeProvider provider)
    {
        foreach (var attribute in provider.CustomAttributes)
        {
            Spend();
            var row = AttributeRowFor(provider, attribute);
            if (!row.RetentionRequested)
            {
                row.RetentionRequested = true;
                _attributeMetadataPending.Enqueue(row);
            }
            if (!row.ConstructRequested && IsBackendAttributeProvider(provider))
            {
                row.ConstructRequested = true;
                _attributeMetadataPending.Enqueue(row);
            }
        }
    }

    private bool ApplyAttributeMetadataClosure()
    {
        while (_attributeMetadataPending.Count != 0)
        {
            Spend();
            var row = _attributeMetadataPending.Dequeue();
            KeepSelectedAttributeMetadata(row.Attribute);
            ConstructSelectedAttribute(row);
            QueueAttributeIdentity(row);
        }
        return _work.Count != 0;
    }

    private bool IsBackendAttributeProvider(ICustomAttributeProvider provider) =>
        provider is TypeDefinition type && _backendAttributeOwners.Contains(type)
        || provider is MethodDefinition method && _backendAttributeOwners.Contains(method.DeclaringType);

    private FieldDefinition? AttributeNamedField(TypeDefinition type, string name)
    {
        foreach (var owner in Ancestors(type))
            if (owner.Fields.FirstOrDefault(field => !field.IsStatic && field.Name == name) is { } field)
                return field;
        return null;
    }

    private PropertyDefinition? AttributeNamedProperty(TypeDefinition type, string name)
    {
        foreach (var owner in Ancestors(type))
            if (owner.Properties.FirstOrDefault(property => property.Name == name
                && property.SetMethod is { IsStatic: false }) is { } property)
                return property;
        return null;
    }

    private void KeepAttributeNamedMetadata(CustomAttribute attribute)
    {
        if (Resolve(attribute.AttributeType) is not { } type) return;
        foreach (var argument in attribute.Fields)
            if (AttributeNamedField(type, argument.Name) is { } field)
            {
                MarkSignatureType(field.DeclaringType);
                MarkSignatureType(field.FieldType);
                KeepPassiveAttributeProvider(field);
            }
        foreach (var argument in attribute.Properties)
            if (AttributeNamedProperty(type, argument.Name) is { } property)
            {
                MarkSignatureType(property.DeclaringType);
                // The original property and accessor identities survive; no body
                // or attribute query is implied by a named-argument metadata row.
                KeepPropertySignature(property);
            }
    }

    private bool ValidateAttributeMetadataRow(CustomAttribute attribute, ICustomAttributeProvider provider,
        MethodDefinition constructor)
    {
        if (!_selectedAttributeRows.TryGetValue(attribute, out var row) || row.Provider != provider || !row.RetentionRequested) return false;
        if (!row.MetadataKept)
            throw new InvalidOperationException("ILDiet plan did not complete attribute metadata closure");
        ValidateAttributeIdentityProvider(row);
        if (row.ConstructRequested && !row.Constructed && SelectedAttributeEligible(attribute, out _))
            throw new InvalidOperationException("ILDiet plan did not construct an admitted attribute row");
        if (!constructor.IsConstructor || constructor.IsStatic
            || IsStripped(constructor.DeclaringType) && (row.Constructed ? !_methods.Contains(constructor) && !KeepsCutMethod(constructor) : !KeepsMethod(constructor)))
            throw new InvalidOperationException("ILDiet plan removed a metadata attribute constructor");
        ValidateKeptType(constructor.DeclaringType);
        ValidateKeptType(attribute.AttributeType);
        ValidateKeptType(constructor.ReturnType);
        foreach (var parameter in constructor.Parameters) ValidateKeptType(parameter.ParameterType);
        try
        {
            if (Resolve(attribute.AttributeType) is { } type)
            {
                foreach (var argument in attribute.Fields)
                    if (AttributeNamedField(type, argument.Name) is { } field)
                    {
                        ValidateKeptType(field.DeclaringType);
                        ValidateKeptType(field.FieldType);
                    }
                foreach (var argument in attribute.Properties)
                    if (AttributeNamedProperty(type, argument.Name) is { } property)
                    {
                        ValidateKeptType(property.DeclaringType);
                        ValidateKeptType(property.PropertyType);
                        if (IsStripped(property.DeclaringType) && (!_properties.Contains(property)
                            || property.SetMethod is not { } setter
                            || (row.Constructed ? !_methods.Contains(setter) && !KeepsCutMethod(setter) : !KeepsMethod(setter))))
                            throw new InvalidOperationException("ILDiet plan removed a metadata attribute named property");
                    }
            }
            foreach (var argument in EffectiveAttributeArguments(attribute)) ValidateAttributeMetadataArgument(argument);
            foreach (var argument in attribute.Fields) ValidateAttributeMetadataArgument(argument.Argument);
            foreach (var argument in attribute.Properties) ValidateAttributeMetadataArgument(argument.Argument);
        }
        catch (AssemblyResolutionException)
        {
            // External attribute values retain the same opaque metadata closure.
        }
        return true;
    }

    private void ValidateAttributeMetadataArgument(CustomAttributeArgument argument)
    {
        ValidateKeptType(argument.Type);
        if (argument.Value is TypeReference reference) ValidateKeptType(reference);
        else if (argument.Value is CustomAttributeArgument nested) ValidateAttributeMetadataArgument(nested);
        else if (argument.Value is CustomAttributeArgument[] values)
            foreach (var value in values) ValidateAttributeMetadataArgument(value);
    }
}

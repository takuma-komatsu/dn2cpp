using Mono.Cecil;

namespace Dn2Cpp;

internal sealed partial class AssemblyDiet
{
    private readonly HashSet<TypeReference> _formationReferences = new();
    private readonly HashSet<TypeDefinition> _formationTypes = new();
    private readonly HashSet<MethodDefinition> _formationMethods = new();
    private readonly HashSet<CustomAttribute> _formationAttributes = new();
    private readonly HashSet<GenericParameter> _formationParameters = new();
    private readonly HashSet<TypeDefinition> _formationTaskContracts = new();

    private void KeepFormationTaskContract(TypeDefinition type)
    {
        if (!_formationTaskContracts.Add(type)) return;
        Spend();
        if (!OriginalAsyncTasks().TryGetValue(type, out var candidate)) return;
        KeepFormationAsyncComponent(candidate);
    }

    private void KeepFormationReference(TypeReference? reference)
    {
        if (reference is null || !_formationReferences.Add(reference)) return;
        Spend();
        PreserveMemberArrayKinds(reference);
        if (reference is GenericParameter parameter)
        {
            KeepFormationParameter(parameter);
            return;
        }
        if (reference is GenericInstanceType generic)
            foreach (var argument in generic.GenericArguments) KeepFormationReference(argument);
        if (reference is IModifierType modifier) KeepFormationReference(modifier.ModifierType);
        if (reference is FunctionPointerType pointer)
        {
            KeepFormationReference(pointer.ReturnType);
            foreach (var argument in pointer.Parameters) KeepFormationReference(argument.ParameterType);
            return;
        }
        if (reference is TypeSpecification specification)
        {
            KeepFormationReference(specification.ElementType);
            return;
        }
        if (Resolve(reference) is { } type) KeepFormationType(type);
    }

    private void KeepFormationType(TypeDefinition type)
    {
        _formationAsyncDepth++;
        try
        {
            KeepFormationTypeCore(type);
        }
        finally
        {
            _formationAsyncDepth--;
        }
        DrainAsyncFormation();
    }

    private void KeepFormationTypeCore(TypeDefinition type)
    {
        ObserveAsyncFormationType(type);
        if (!IsStripped(type) || !_formationTypes.Add(type)) return;
        Spend();
        // This is original-row survival, not a request to decode the primary's
        // layout or members. Normal use can independently upgrade the graph node.
        _types.Add(type);
        KeepFormationReference(type.DeclaringType);
        KeepFormationReference(type.BaseType);
        foreach (var implementation in type.Interfaces)
        {
            KeepFormationReference(implementation.InterfaceType);
            KeepFormationAttributes(implementation);
        }
        foreach (var parameter in type.GenericParameters) KeepFormationParameter(parameter);
        foreach (var field in type.Fields)
        {
            _fields.Add(field);
            KeepFormationReference(field.FieldType);
            KeepFormationAttributes(field);
            KeepFormationMarshal(field);
        }
        KeepFormationAttributes(type);
        KeepFormationSecurity(type);
    }

    private void KeepFormationMethod(MethodDefinition method)
    {
        _formationAsyncDepth++;
        try
        {
            KeepFormationMethodCore(method);
        }
        finally
        {
            _formationAsyncDepth--;
        }
        DrainAsyncFormation();
    }

    private void KeepFormationMethodCore(MethodDefinition method)
    {
        ObserveAsyncFormationType(method.DeclaringType);
        if (!IsStripped(method.DeclaringType) || !_formationMethods.Add(method)) return;
        Spend();
        KeepFormationType(method.DeclaringType);
        _signatureMethods.Add(method);
        foreach (var reference in OriginalMethodSignature(method)) KeepFormationReference(reference);
        foreach (var parameter in method.GenericParameters) KeepFormationParameter(parameter);
        KeepFormationAttributes(method);
        KeepFormationAttributes(method.MethodReturnType);
        KeepFormationMarshal(method.MethodReturnType);
        KeepFormationSecurity(method);
        foreach (var parameter in method.Parameters)
        {
            KeepFormationAttributes(parameter);
            KeepFormationMarshal(parameter);
        }
        foreach (var declaration in method.Overrides)
        {
            KeepFormationReference(declaration.DeclaringType);
            KeepFormationReference(declaration.ReturnType);
            foreach (var parameter in declaration.Parameters) KeepFormationReference(parameter.ParameterType);
            if (ResolveMember(declaration) is MethodDefinition target) KeepFormationMethod(target);
        }
    }

    private void KeepFormationParameter(GenericParameter parameter)
    {
        if (!_formationParameters.Add(parameter)) return;
        Spend();
        KeepFormationAttributes(parameter);
        foreach (var constraint in parameter.Constraints)
        {
            KeepFormationReference(constraint.ConstraintType);
            KeepFormationAttributes(constraint);
        }
    }

    private void KeepFormationMarshal(IMarshalInfoProvider provider)
    {
        if (provider.HasMarshalInfo && provider.MarshalInfo is CustomMarshalInfo custom)
            KeepFormationReference(custom.ManagedType);
    }

    private void KeepFormationSecurity(ISecurityDeclarationProvider provider)
    {
        foreach (var declaration in provider.SecurityDeclarations)
            foreach (var attribute in declaration.SecurityAttributes)
            {
                KeepFormationReference(attribute.AttributeType);
                foreach (var field in attribute.Fields) KeepFormationArgument(field.Argument);
                foreach (var property in attribute.Properties) KeepFormationArgument(property.Argument);
            }
    }

    private void KeepFormationArgument(CustomAttributeArgument argument)
    {
        KeepFormationReference(argument.Type);
        if (argument.Value is TypeReference type) KeepFormationReference(type);
        else if (argument.Value is CustomAttributeArgument nested) KeepFormationArgument(nested);
        else if (argument.Value is CustomAttributeArgument[] array)
            foreach (var item in array) KeepFormationArgument(item);
    }

    private void KeepFormationAttributes(ICustomAttributeProvider provider)
    {
        foreach (var attribute in provider.CustomAttributes)
        {
            if (!_formationAttributes.Add(attribute)) continue;
            Spend();
            if (ResolveMember(attribute.Constructor) is MethodDefinition constructor) KeepFormationMethod(constructor);
            KeepFormationReference(attribute.AttributeType);
            KeepFormationReference(attribute.Constructor.ReturnType);
            foreach (var parameter in attribute.Constructor.Parameters) KeepFormationReference(parameter.ParameterType);
            try
            {
                foreach (var argument in EffectiveAttributeArguments(attribute)) KeepFormationArgument(argument);
                foreach (var argument in attribute.Fields) KeepFormationArgument(argument.Argument);
                foreach (var argument in attribute.Properties) KeepFormationArgument(argument.Argument);
                if (Resolve(attribute.AttributeType) is not { } type) continue;
                foreach (var argument in attribute.Fields)
                    if (AttributeNamedField(type, argument.Name) is { } field)
                        KeepFormationType(field.DeclaringType);
                foreach (var argument in attribute.Properties)
                    if (AttributeNamedProperty(type, argument.Name) is { } property)
                    {
                        KeepFormationType(property.DeclaringType);
                        _properties.Add(property);
                        _rowOnlyProperties.Add(property);
                        KeepFormationReference(property.PropertyType);
                        foreach (var parameter in property.Parameters)
                        {
                            KeepFormationReference(parameter.ParameterType);
                            KeepFormationAttributes(parameter);
                            KeepFormationMarshal(parameter);
                        }
                        KeepFormationAttributes(property);
                        if (property.GetMethod is not null) KeepFormationMethod(property.GetMethod);
                        if (property.SetMethod is not null) KeepFormationMethod(property.SetMethod);
                        foreach (var accessor in property.OtherMethods) KeepFormationMethod(accessor);
                    }
            }
            catch (AssemblyResolutionException)
            {
                // Unresolved external rows keep their original opaque blob.
            }
        }
    }

    private bool ValidateFormationAttribute(CustomAttribute attribute, MethodDefinition constructor)
    {
        if (!_formationAttributes.Contains(attribute)) return false;
        if (IsStripped(constructor.DeclaringType) && !KeepsMethod(constructor))
            throw new InvalidOperationException("ILDiet removed an original formation attribute constructor");
        ValidateKeptType(attribute.AttributeType);
        ValidateKeptType(constructor.DeclaringType);
        foreach (var argument in EffectiveAttributeArguments(attribute)) ValidateAttributeMetadataArgument(argument);
        foreach (var argument in attribute.Fields) ValidateAttributeMetadataArgument(argument.Argument);
        foreach (var argument in attribute.Properties) ValidateAttributeMetadataArgument(argument.Argument);
        return true;
    }
}

using Mono.Cecil;
using MetadataTokens = System.Reflection.Metadata.Ecma335.MetadataTokens;

namespace Dn2Cpp;

internal sealed partial class AssemblyDiet
{
    private readonly HashSet<MethodDefinition> _attributeInitializerProviders = new();
    private readonly HashSet<TypeDefinition> _attributeModelCandidates = new();
    private readonly HashSet<TypeDefinition> _attributeModeledTypes = new();
    private readonly Queue<TypeDefinition> _attributeInventoryPending = new();

    private SelectedAttributeRow AttributeRowFor(ICustomAttributeProvider provider, CustomAttribute attribute)
    {
        if (_selectedAttributeRows.TryGetValue(attribute, out var row)) return row;
        Spend();
        row = new SelectedAttributeRow { Attribute = attribute, Provider = provider };
        _selectedAttributeRows.Add(attribute, row);
        _selectedAttributeRowsInOrder.Add(row);
        IsRegistrationAttribute(attribute, provider);
        IndexAttributeIdentityRow(row);
        return row;
    }

    private void DiscoverAttributeProvider(ICustomAttributeProvider provider)
    {
        Spend();
        if (!provider.HasCustomAttributes) return;
        foreach (var attribute in provider.CustomAttributes) AttributeRowFor(provider, attribute);
    }

    private void InitializeAttributeInventory()
    {
        foreach (var assembly in _assemblies)
        {
            DiscoverAttributeProvider(assembly.Assembly);
            DiscoverAttributeProvider(assembly.Assembly.MainModule);
            if (PreservationReader.IsFrameworkAssemblyName(assembly.Assembly.Name.Name)) continue;
            foreach (var type in AllTypes(assembly.Assembly.MainModule.Types))
            {
                Spend();
                if (type.HasGenericParameters) _modelHasUserGenericDefinitions = true;
                // Primary Pass1 models every non-generic user definition, even
                // when no retained body or signature names it.
                if (!type.HasGenericParameters && (type.Name != "<Module>" || type.HasMethods || type.HasFields))
                    ObserveAttributeModel(type);
            }
        }
    }

    private void ObserveAttributeModel(TypeDefinition type)
    {
        if (PreservationReader.IsFrameworkAssemblyName(type.Module.Assembly.Name.Name)) return;
        if (type.HasGenericParameters) EnsureOriginalModels();
        if (_attributeModelCandidates.Add(type)) Spend();
        if (!HasModelBindings(type)) return;
        if (!_attributeModeledTypes.Add(type)) return;
        Spend();
        _attributeInventoryPending.Enqueue(type);
        if (_attributeRowsByOwner.TryGetValue(type, out var rows))
            foreach (var row in rows) QueueAttributeIdentity(row);
    }

    private void AttributeModelBindingChanged(GenericParameter parameter)
    {
        if (parameter.Owner is TypeDefinition type && _attributeModelCandidates.Contains(type))
            ObserveAttributeModel(type);
    }

    private bool ApplyAttributeInventory()
    {
        while (_attributeInventoryPending.Count != 0)
        {
            Spend();
            var type = _attributeInventoryPending.Dequeue();
            DiscoverAttributeProvider(type);
            foreach (var field in type.Fields) DiscoverAttributeProvider(field);
            var reader = System.Reflection.Metadata.PEReaderExtensions.GetMetadataReader(_byModule[type.Module].PE);
            foreach (var method in type.Methods)
            {
                DiscoverAttributeProvider(method);
                // Parameter tables identify attributed rows without decoding
                // unrelated method signatures or visiting any method body.
                var definition = reader.GetMethodDefinition(MetadataTokens.MethodDefinitionHandle((int)method.MetadataToken.RID));
                foreach (var handle in definition.GetParameters())
                {
                    Spend();
                    var parameter = reader.GetParameter(handle);
                    if (parameter.GetCustomAttributes().Count == 0) continue;
                    if (parameter.SequenceNumber == 0) DiscoverAttributeProvider(method.MethodReturnType);
                    else if (parameter.SequenceNumber <= method.Parameters.Count)
                        DiscoverAttributeProvider(method.Parameters[parameter.SequenceNumber - 1]);
                }
            }
            foreach (var property in type.Properties) DiscoverAttributeProvider(property);
        }
        return _work.Count != 0;
    }

    private void KeepAttributeProviderMethod(MethodDefinition method)
    {
        MarkSignatureType(method.DeclaringType);
        // An admitted row on an otherwise-unused initializer needs its original
        // MethodDef, not an executable initializer. Ordinary cctor metadata still
        // follows the body-or-remove rule.
        if (method.IsConstructor && method.IsStatic)
        {
            if (_attributeInitializerProviders.Add(method)) Spend();
            _passiveAttributeMethods.Add(method);
            Require(method, MethodState.Metadata);
        }
        else KeepMethodSignature(method);
    }

    private void ValidateAttributeIdentityProvider(SelectedAttributeRow row)
    {
        if (!row.ProviderKept || AttributeIdentityOwner(row.Provider) is not { } owner || !IsStripped(owner)) return;
        ValidateKeptType(owner);
        MethodDefinition? method = row.Provider switch
        {
            MethodDefinition value => value,
            ParameterDefinition parameter when parameter.Method is MethodDefinition value => value,
            MethodReturnType result when result.Method is MethodDefinition value => value,
            _ => null,
        };
        if (method is not null && !KeepsMethod(method)
            || row.Provider is PropertyDefinition property && !_properties.Contains(property))
            throw new InvalidOperationException("ILDiet plan removed an admitted attribute provider");
    }

    private void KeepAttributeIdentityProvider(SelectedAttributeRow row)
    {
        if (row.ProviderKept) return;
        row.ProviderKept = true;
        if (AttributeIdentityOwner(row.Provider) is { } formationOwner) KeepOriginalModelSources(formationOwner);
        var bindingOwner = _passiveBindingOwner;
        _passiveBindingOwner = row.Provider;
        _passiveAttributeMetadataDepth++;
        try
        {
            // The published primary must see the row that supplied conditional
            // identity evidence; retaining its target body alone is insufficient.
            if (row.Provider is MethodDefinition providerMethod) KeepAttributeProviderMethod(providerMethod);
            else if (row.Provider is PropertyDefinition property)
            {
                MarkSignatureType(property.DeclaringType);
                KeepPropertyRow(property, allowAccessorless: true);
                if (property.GetMethod is not null) KeepMethodSignature(property.GetMethod);
                if (property.SetMethod is not null) KeepMethodSignature(property.SetMethod);
                foreach (var accessor in property.OtherMethods) KeepMethodSignature(accessor);
            }
            else if (row.Provider is IMemberDefinition member) KeepAttributeProvider(member);
            else if (row.Provider is ParameterDefinition parameter && parameter.Method is MethodDefinition method)
            {
                KeepAttributeProviderMethod(method);
            }
            else if (row.Provider is MethodReturnType result && result.Method is MethodDefinition owner)
            {
                KeepAttributeProviderMethod(owner);
            }
        }
        finally
        {
            _passiveBindingOwner = bindingOwner;
            _passiveAttributeMetadataDepth--;
        }
    }
}

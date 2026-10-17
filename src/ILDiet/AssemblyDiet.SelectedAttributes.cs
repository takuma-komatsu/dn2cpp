using Mono.Cecil;

namespace Dn2Cpp;

internal sealed partial class AssemblyDiet
{
    private sealed record SelectedAttributeSite(MethodDefinition Caller, ReflectionSummary Summary, RSite Site);

    private sealed class SelectedAttributeRow
    {
        internal required CustomAttribute Attribute;
        internal required ICustomAttributeProvider Provider;
        internal bool Observed, RetentionRequested, MetadataKept, MetadataObserved, ArgumentsObserved, ConstructRequested, Constructed;
        internal bool ProviderKept, ClassIdentityObserved, ArgumentIdentitiesObserved;
        internal bool ModelClassIdentityPromoted, ModelArgumentIdentitiesPromoted;
    }

    private readonly List<SelectedAttributeSite> _selectedAttributeSites = new();
    private readonly HashSet<(MethodDefinition Caller, int Offset)> _selectedAttributeSiteKeys = new();
    private readonly Dictionary<CustomAttribute, SelectedAttributeRow> _selectedAttributeRows = new();
    private readonly List<SelectedAttributeRow> _selectedAttributeRowsInOrder = new();
    private bool _selectedAttributeSitesDirty, _selectingAttributeOwners;

    private void RegisterSelectedAttributeSite(MethodDefinition caller, ReflectionSummary summary, RSite site)
    {
        CheckMutable();
        NoteAttributeRead();
        if (!_selectedAttributeSiteKeys.Add((caller, site.Offset))) return;
        Spend();
        _selectedAttributeSites.Add(new SelectedAttributeSite(caller, summary, site));
        _selectedAttributeSitesDirty = true;
    }

    private void AttributeBindingsChanged()
    {
        CheckMutable();
        if (_selectedAttributeSites.Count != 0) _selectedAttributeSitesDirty = true;
    }

    private IEnumerable<TypeReference> SelectedAttributeSources(TypeReference source)
    {
        var pending = new Queue<TypeReference>();
        var seen = new HashSet<TypeReference>();
        pending.Enqueue(source);
        while (pending.Count != 0)
        {
            var current = pending.Dequeue();
            if (!seen.Add(current)) continue;
            Spend();
            RejectModelActivationQuery("SelectedAttributeSources");
            EnsureModelReference(current);
            if (current is GenericParameter parameter)
            {
                if (_parameters.TryGetValue(parameter, out var node))
                    foreach (var bound in node.Bound) pending.Enqueue(bound);
            }
            else if (current is ProjectedTypeReference projected)
            {
                foreach (var target in projected.Node.Targets) pending.Enqueue(target);
            }
            else if (current is OptionalModifierType or RequiredModifierType or PinnedType)
                pending.Enqueue(((TypeSpecification)current).ElementType);
            else yield return current;
        }
    }

    private SummaryValue SelectedAttributeOwners(SummaryValue source)
    {
        if (!_selectingAttributeOwners) return source;
        var result = new SummaryValue();
        result.Add(source);
        result.Types.Clear();
        foreach (var type in source.AttributeTypes)
            foreach (var bound in SelectedAttributeSources(type)) result.Types.Add(bound);
        return result;
    }

    private void CarrySelectedAttributeTypeOwners(RTerm term, SummaryValue result)
    {
        if (!_selectingAttributeOwners) return;
        // Runtime object types are data, not reflected attribute providers. Unions
        // keep the known Type-valued alternatives independently of unknown objects.
        if (term.Kind is TermKind.TypeToken or TermKind.MethodSpecArg or TermKind.TypeName
            or TermKind.MadeGeneric or TermKind.ArrayOf or TermKind.ElementOf or TermKind.GenericDefOf
            or TermKind.GenericArgOf or TermKind.BaseOf or TermKind.TypeOfMember
            or TermKind.Lookup or TermKind.Enumeration or TermKind.TypeArray)
            result.AttributeTypes.AddRange(result.Types);
    }

    private bool ApplySelectedAttributeSites()
    {
        RejectModelActivationQuery("ApplySelectedAttributeSites");
        CheckMutable();
        if (!_selectedAttributeSitesDirty) return false;
        _selectedAttributeSitesDirty = false;
        _selectingAttributeOwners = true;
        try
        {
            foreach (var read in _selectedAttributeSites.ToArray())
            {
                Spend();
                _summaryValues.Clear();
                var api = read.Site.Api;
                var owners = SummaryValueOf(read.Caller, read.Summary, SiteOperand(read.Site, api.Target));
                var filters = SummaryValueOf(read.Caller, read.Summary, SiteOperand(read.Site, api.AttributeType));
                var classes = new HashSet<TypeDefinition>();
                bool open = api.AttributeType == ReflectionApi.Absent || filters.HasUnknownOwner;
                foreach (var reference in filters.AttributeTypes)
                    foreach (var source in SelectedAttributeSources(reference))
                    {
                        if (source is TypeSpecification && source is not GenericInstanceType
                            || Resolve(source) is not { IsInterface: false } filter) open = true;
                        else classes.Add(filter);
                    }
                var inheritValue = SummaryValueOf(read.Caller, read.Summary, SiteOperand(read.Site, api.Inherit));
                bool inherit = api.Inherit >= 0
                    ? inheritValue.Unknown || inheritValue.Integers.Any(value => value != 0) : api.DefaultInherit;
                bool construct = api.Kind == ReflectionOpKind.AttributeInstances;
                foreach (var reference in owners.AttributeTypes)
                    foreach (var source in SelectedAttributeSources(reference))
                    {
                        if (source is TypeSpecification && source is not GenericInstanceType
                            || Resolve(source) is not { } type) continue;
                        foreach (var level in Ancestors(type))
                        {
                            SelectAttributeProvider(level, classes, open, construct);
                            if (!inherit) break;
                        }
                    }
                foreach (var member in owners.Members)
                    if (member is ICustomAttributeProvider provider)
                        SelectAttributeProvider(provider, classes, open, construct);
                foreach (var parameter in owners.AttributeParameters)
                    SelectAttributeProvider(parameter, classes, open, construct);
                foreach (var module in owners.Assemblies)
                    SelectAttributeProvider(module.Assembly, classes, open, construct);
                foreach (var reference in owners.SymbolicAssemblyOwners)
                    foreach (var source in SelectedAttributeSources(reference))
                        if (Resolve(source) is { } owner)
                            SelectAttributeProvider(owner.Module.Assembly, classes, open, construct);
            }
        }
        finally
        {
            _selectingAttributeOwners = false;
            _summaryValues.Clear();
        }
        return _work.Count != 0 || _selectedAttributeSitesDirty;
    }

    private void SelectAttributeProvider(ICustomAttributeProvider provider, HashSet<TypeDefinition> filters,
        bool open, bool construct)
    {
        CheckMutable();
        if (!provider.HasCustomAttributes) return;
        foreach (var attribute in provider.CustomAttributes)
        {
            Spend();
            var row = AttributeRowFor(provider, attribute);
            row.RetentionRequested = true;
            KeepSelectedAttributeMetadata(attribute);
            row.Observed = true;
            if (construct && (open || Resolve(attribute.AttributeType) is { } type
                && Ancestors(type).Any(filters.Contains))) row.ConstructRequested = true;
            ObserveSelectedAttribute(row);
            ConstructSelectedAttribute(row);
        }
        // Register the rows before retaining their provider: metadata retention
        // must not turn a selected data row into an executable attribute factory.
        if (provider is IMemberDefinition owner) KeepAttributeProvider(owner);
        else if (provider is ParameterDefinition parameter && parameter.Method is MethodDefinition method)
            KeepMethodSignature(method);
    }

    private bool UsesSelectedAttributeRetention(CustomAttribute attribute, ICustomAttributeProvider provider)
    {
        if (!_selectedAttributeRows.TryGetValue(attribute, out var row) || !row.RetentionRequested) return false;
        if (IsBackendAttributeProvider(provider))
        {
            row.ConstructRequested = true;
            ConstructSelectedAttribute(row);
            return true;
        }
        if (!row.Observed) return true;
        TypeDefinition? owner = provider switch
        {
            TypeDefinition type => type,
            IMemberDefinition member => member.DeclaringType,
            ParameterDefinition parameter when parameter.Method is MethodDefinition method => method.DeclaringType,
            _ => null,
        };
        // Independently executable owners retain their ordinary attribute roots,
        // including backend roots; selection alone supplies only provider metadata.
        return owner is null || Node(owner) is not { State: var state } || (state & TypeState.Runtime) == 0;
    }

    private void KeepSelectedAttributeMetadata(CustomAttribute attribute)
    {
        if (_selectedAttributeRows.TryGetValue(attribute, out var row))
        {
            if (row.MetadataKept) return;
            row.MetadataKept = true;
        }
        var bindingOwner = _passiveBindingOwner;
        _passiveBindingOwner = attribute;
        _passiveAttributeMetadataDepth++;
        try
        {
            if (ResolveMember(attribute.Constructor) is MethodDefinition constructor) KeepMethodSignature(constructor);
            MarkSignatureType(attribute.AttributeType);
            MarkSignatureType(attribute.Constructor.ReturnType);
            foreach (var parameter in attribute.Constructor.Parameters) MarkSignatureType(parameter.ParameterType);
            foreach (var argument in EffectiveAttributeArguments(attribute)) MarkAttributeMetadataArgument(argument);
            foreach (var argument in attribute.Fields) MarkAttributeMetadataArgument(argument.Argument);
            foreach (var argument in attribute.Properties) MarkAttributeMetadataArgument(argument.Argument);
            KeepAttributeNamedMetadata(attribute);
        }
        catch (AssemblyResolutionException)
        {
            // An unresolved external row retains its original opaque blob.
        }
        finally
        {
            _passiveBindingOwner = bindingOwner;
            _passiveAttributeMetadataDepth--;
        }
    }

    private void ReplaySelectedAttributeRows()
    {
        CheckMutable();
        foreach (var row in _selectedAttributeRowsInOrder.ToArray())
        {
            Spend();
            ObserveSelectedAttribute(row);
            ConstructSelectedAttribute(row);
        }
    }

    private bool SelectedAttributeEligible(CustomAttribute attribute, out TypeDefinition? type)
    {
        type = Resolve(attribute.AttributeType);
        return type is not null && (!PreservationReader.IsFrameworkAssemblyName(type.Module.Assembly.Name.Name)
            || IsFrameworkAttributeNamed(type));
    }

    private void ObserveSelectedAttribute(SelectedAttributeRow row)
    {
        if (!row.Observed || row.MetadataObserved || !SelectedAttributeEligible(row.Attribute, out var type)) return;
        row.MetadataObserved = true;
        // A data row names identities without executing its factory. An explicit
        // conditional preservation rule can observe those references independently.
        NotePreservedReference(row.Attribute.AttributeType);
        ObserveAttributeArguments(row, type!);
    }

    private void ObserveAttributeArguments(SelectedAttributeRow row, TypeDefinition type, bool read = true)
    {
        if (read ? row.ArgumentsObserved : row.ArgumentIdentitiesObserved) return;
        row.ArgumentIdentitiesObserved = true;
        if (read) row.ArgumentsObserved = true;
        VisitAttributeArguments(row, type, read, promote: false);
    }

    private void PromoteAttributeEffects(SelectedAttributeRow row, TypeDefinition type, bool classIdentity)
    {
        if (AttributeIdentityOwner(row.Provider) is not { HasGenericParameters: true }) return;
        if (classIdentity && !row.ModelClassIdentityPromoted)
        {
            row.ModelClassIdentityPromoted = true;
            PromoteModelReference(row.Attribute.AttributeType);
        }
        if (row.ModelArgumentIdentitiesPromoted) return;
        row.ModelArgumentIdentitiesPromoted = true;
        VisitAttributeArguments(row, type, read: false, promote: true);
    }

    private void VisitAttributeArguments(SelectedAttributeRow row, TypeDefinition type, bool read, bool promote)
    {
        try
        {
            var parameters = row.Attribute.Constructor.Parameters;
            for (int i = 0; i < row.Attribute.ConstructorArguments.Count; i++)
                ObserveSelectedAttributeArgument(EffectiveAttributeArgument(row.Attribute, row.Attribute.ConstructorArguments[i]),
                    i < parameters.Count && RawTypeName(parameters[i].ParameterType) == "System.Object", read, promote);
            foreach (var argument in row.Attribute.Fields)
                ObserveSelectedAttributeArgument(argument.Argument, SelectedAttributeArgumentBoxed(type!, argument.Name, false), read, promote);
            foreach (var argument in row.Attribute.Properties)
                ObserveSelectedAttributeArgument(argument.Argument, SelectedAttributeArgumentBoxed(type!, argument.Name, true), read, promote);
        }
        catch (AssemblyResolutionException)
        {
            // Unresolved values contribute no identity beyond their metadata closure.
        }
    }

    private bool SelectedAttributeArgumentBoxed(TypeDefinition type, string name, bool property)
    {
        foreach (var owner in Ancestors(type))
            if (property)
            {
                if (owner.Methods.FirstOrDefault(method => !method.IsStatic && method.Name == "set_" + name) is { } setter)
                    return setter.Parameters.Count > 0 && RawTypeName(setter.Parameters[0].ParameterType) == "System.Object";
            }
            else if (owner.Fields.FirstOrDefault(field => !field.IsStatic && field.Name == name) is { } field)
                return RawTypeName(field.FieldType) == "System.Object";
        return false;
    }

    private void ObserveSelectedAttributeArgument(CustomAttributeArgument argument, bool boxed, bool read, bool promote)
    {
        Spend();
        if (argument.Type is ArrayType array)
        {
            if (!promote) NoteArrayElement(array.ElementType);
            if (Resolve(array.ElementType) is { IsEnum: true }) AttributeEffectIdentity(array.ElementType, read, promote);
            if (argument.Value is CustomAttributeArgument[] items)
                foreach (var item in items)
                    ObserveSelectedAttributeArgument(item, RawTypeName(array.ElementType) == "System.Object", read, promote);
            return;
        }
        if (argument.Value is TypeReference reference) AttributeEffectIdentity(reference, read, promote);
        else if (argument.Value is CustomAttributeArgument nested) ObserveSelectedAttributeArgument(nested, boxed, read, promote);
        else if (boxed && argument.Value is not null && Resolve(argument.Type) is { IsEnum: true })
            AttributeEffectIdentity(argument.Type, read, promote);
    }

    private void AttributeEffectIdentity(TypeReference reference, bool read, bool promote)
    {
        if (promote) PromoteModelReference(reference);
        else ObserveAttributeTypeIdentity(reference, read);
    }

    private void ConstructSelectedAttribute(SelectedAttributeRow row)
    {
        if (!row.ConstructRequested || row.Constructed) return;
        var attribute = row.Attribute;
        if (!SelectedAttributeEligible(attribute, out var type)) return;
        row.Constructed = true;
        int modelDepth = _observingOriginalModel, passiveDepth = _passiveAttributeMetadataDepth;
        _observingOriginalModel = 0;
        _passiveAttributeMetadataDepth = 0;
        try { ApplyConstructedAttribute(attribute, type!); }
        finally
        {
            _observingOriginalModel = modelDepth;
            _passiveAttributeMetadataDepth = passiveDepth;
        }
    }

    private void ApplyConstructedAttribute(CustomAttribute attribute, TypeDefinition type)
    {
        ReplayPassiveBindings(attribute);
        MarkMethod(attribute.Constructor);
        MarkType(attribute.AttributeType);
        if (ResolveMember(attribute.Constructor) is MethodDefinition constructor) NoteReflectionInvokeBoxes(constructor);
        try
        {
            foreach (var argument in EffectiveAttributeArguments(attribute)) MarkArgument(argument);
            foreach (var argument in attribute.Fields)
            {
                MarkArgument(argument.Argument);
                foreach (var owner in Ancestors(type!))
                    if (owner.Fields.FirstOrDefault(field => !field.IsStatic && field.Name == argument.Name) is { } field)
                    {
                        MarkField(field);
                        break;
                    }
            }
            foreach (var argument in attribute.Properties)
            {
                MarkArgument(argument.Argument);
                foreach (var owner in Ancestors(type!))
                    if (owner.Methods.FirstOrDefault(method => !method.IsStatic && method.Name == "set_" + argument.Name) is { } setter)
                    {
                        MarkMethod(setter);
                        NoteReflectionInvokeBoxes(setter);
                        break;
                    }
            }
        }
        catch (AssemblyResolutionException)
        {
            // A constructor can remain callable while an external argument is opaque.
        }
    }

    private void KeepAttributeProvider(IMemberDefinition? owner)
    {
        if (owner is null) return;
        if (owner is TypeDefinition type)
        {
            Require(type, TypeState.Metadata);
            return;
        }
        Require(owner.DeclaringType, TypeState.Metadata);
        switch (owner)
        {
            case MethodDefinition method:
                KeepMethodSignature(method);
                break;
            case PropertyDefinition property:
                KeepPropertySignature(property);
                break;
            case EventDefinition item:
                KeepEventSignature(item);
                break;
        }
    }
}

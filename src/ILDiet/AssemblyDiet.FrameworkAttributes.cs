using Mono.Cecil;

namespace Dn2Cpp;

internal sealed partial class AssemblyDiet
{
    private sealed record DeferredFrameworkAttribute(CustomAttribute Attribute, ICustomAttributeProvider Provider);

    private readonly record struct FrameworkAttributeName(string Value, bool IsGeneric);

    private readonly HashSet<FrameworkAttributeName> _namedFrameworkAttributeNames = new();
    private readonly List<FrameworkAttributeName> _frameworkAttributeNamesInOrder = new();
    private readonly HashSet<TypeDefinition> _activeFrameworkAttributeTypes = new();
    private readonly HashSet<TypeDefinition> _replayedFrameworkAttributeOwners = new();
    private readonly HashSet<CustomAttribute> _deferredFrameworkAttributeRows = new();
    private readonly Dictionary<TypeDefinition, List<DeferredFrameworkAttribute>> _deferredFrameworkAttributes = new();

    private bool DeferFrameworkAttribute(CustomAttribute attribute, ICustomAttributeProvider provider)
    {
        if (Resolve(attribute.AttributeType) is not { } type || IsStripped(type)
            || !PreservationReader.IsFrameworkAssemblyName(type.Module.Assembly.Name.Name)
            || IsFrameworkAttributeNamed(type)) return false;
        if (!_deferredFrameworkAttributeRows.Add(attribute)) return true;
        Spend();
        if (!_deferredFrameworkAttributes.TryGetValue(type, out var rows))
            _deferredFrameworkAttributes.Add(type, rows = new());
        rows.Add(new DeferredFrameworkAttribute(attribute, provider));
        // Retained rows need Type-valued argument closure. Metadata alone does
        // not execute the copied attribute implementation.
        if (ResolveMember(attribute.Constructor) is MethodDefinition constructor) KeepMethodSignature(constructor);
        MarkSignatureType(attribute.AttributeType);
        MarkSignatureType(attribute.Constructor.ReturnType);
        foreach (var parameter in attribute.Constructor.Parameters) MarkSignatureType(parameter.ParameterType);
        try
        {
            foreach (var argument in EffectiveAttributeArguments(attribute)) MarkAttributeMetadataArgument(argument);
            foreach (var argument in attribute.Fields) MarkAttributeMetadataArgument(argument.Argument);
            foreach (var argument in attribute.Properties) MarkAttributeMetadataArgument(argument.Argument);
        }
        catch (AssemblyResolutionException)
        {
            // Unresolved external values keep the original opaque attribute blob.
        }
        return true;
    }

    private void MarkAttributeMetadataArgument(CustomAttributeArgument argument)
    {
        Spend();
        MarkSignatureType(argument.Type);
        if (argument.Value is TypeReference type) MarkSignatureType(type);
        else if (argument.Value is CustomAttributeArgument nested) MarkAttributeMetadataArgument(nested);
        else if (argument.Value is CustomAttributeArgument[] values)
            foreach (var value in values) MarkAttributeMetadataArgument(value);
    }

    private void NameFrameworkAttribute(TypeReference reference)
    {
        // typeof(T[]) names the array, not T. A constructed class names its original
        // definition; generic parameter demands are replayed over original bindings.
        if (reference is TypeSpecification && reference is not GenericInstanceType) return;
        if (Resolve(reference) is not { } type
            || !PreservationReader.IsFrameworkAssemblyName(type.Module.Assembly.Name.Name)) return;
        var name = FrameworkAttributeNameOf(type);
        if (!_namedFrameworkAttributeNames.Add(name)) return;
        _frameworkAttributeNamesInOrder.Add(name);
        Spend();
        // Primary eligibility compares assembly-unqualified model names. Rows retain
        // their original constructor scopes while names can activate homonymous owners.
        foreach (var pair in _deferredFrameworkAttributes.OrderBy(p => _byModule[p.Key.Module].Index)
                     .ThenBy(p => p.Key.MetadataToken.ToInt32()).ToArray())
        {
            Spend();
            if (!FrameworkAttributeNamesMayMatch(name, FrameworkAttributeNameOf(pair.Key))
                || !_replayedFrameworkAttributeOwners.Add(pair.Key)) continue;
            _activeFrameworkAttributeTypes.Add(pair.Key);
            foreach (var row in pair.Value)
            {
                Spend();
                bool signatureOnly = row.Provider is MethodDefinition method && !_methods.Contains(method);
                MarkAttribute(row.Attribute, row.Provider, signatureOnly);
            }
        }
        ReplaySelectedAttributeRows();
        AttributeIdentityNameChanged(name);
    }

    private bool IsFrameworkAttributeNamed(TypeDefinition type)
    {
        if (_activeFrameworkAttributeTypes.Contains(type)) return true;
        var name = FrameworkAttributeNameOf(type);
        foreach (var named in _frameworkAttributeNamesInOrder)
        {
            Spend();
            if (!FrameworkAttributeNamesMayMatch(named, name)) continue;
            _activeFrameworkAttributeTypes.Add(type);
            return true;
        }
        return false;
    }

    private static FrameworkAttributeName FrameworkAttributeNameOf(TypeDefinition type) =>
        new(type.HasGenericParameters ? GenericNameStem(type)
            : type.Namespace.Length == 0 ? type.Name : type.Namespace + "." + type.Name,
            type.HasGenericParameters);

    private static bool FrameworkAttributeNamesMayMatch(FrameworkAttributeName first, FrameworkAttributeName second)
    {
        if (!first.IsGeneric && !second.IsGeneric)
            return string.Equals(first.Value, second.Value, StringComparison.Ordinal);
        if (first.IsGeneric && second.IsGeneric
            && string.Equals(first.Value, second.Value, StringComparison.Ordinal)) return true;
        // A closed class name appends argument mangles to its declaring-name stem.
        // Overlapping prefix families avoid constructing closed caller contexts.
        return first.IsGeneric && second.Value.StartsWith(first.Value + "_", StringComparison.Ordinal)
            || second.IsGeneric && first.Value.StartsWith(second.Value + "_", StringComparison.Ordinal);
    }
}

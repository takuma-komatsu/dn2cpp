using Mono.Cecil;

namespace Dn2Cpp;

internal sealed partial class AssemblyDiet
{
    private readonly Dictionary<TypeReference, bool> _symbolicSummaryTypes = new();
    private readonly Dictionary<TypeReference, bool> _exactSummaryRuntimeTypes = new();

    private void AddSummaryType(SummaryValue result, TypeReference reference, SummaryValue origins)
    {
        CheckMutable();
        Spend();
        result.Types.Add(reference);
        if (origins.HasOpenTypeSignatures || origins.HasOpenMethodSignatures)
            result.Unknown |= HasSymbolicSummaryType(reference);
    }

    private static void AddSelectedSummaryMethod(SummaryValue result, MethodDefinition method)
    {
        result.Members.Add(method);
        result.HasOpenMethodSignatures |= method.HasGenericParameters;
    }

    private bool HasSymbolicSummaryType(TypeReference reference)
    {
        if (_symbolicSummaryTypes.TryGetValue(reference, out bool known)) return known;
        CheckMutable();
        Spend();
        bool symbolic;
        if (IsSymbolic(reference)) symbolic = true;
        else if (reference is GenericInstanceType generic)
            symbolic = generic.GenericArguments.Any(HasSymbolicSummaryChild)
                || generic.DeclaringType is { } owner && HasSymbolicSummaryChild(owner);
        else if (reference is FunctionPointerType pointer)
            symbolic = HasSymbolicSummaryChild(pointer.ReturnType)
                || pointer.Parameters.Any(p => HasSymbolicSummaryChild(p.ParameterType));
        else if (reference is TypeSpecification specification)
            symbolic = HasSymbolicSummaryChild(specification.ElementType)
                || reference is IModifierType modifier && HasSymbolicSummaryChild(modifier.ModifierType);
        else symbolic = reference.DeclaringType is { } declaring && HasSymbolicSummaryChild(declaring);
        // An open nominal definition is a metadata identity, not a variable-bearing
        // signature merely because its declaration has formal parameters.
        _symbolicSummaryTypes.Add(reference, symbolic);
        return symbolic;
    }

    private bool HasSymbolicSummaryChild(TypeReference reference)
    {
        Spend();
        return HasSymbolicSummaryType(reference);
    }

    private bool ExactSummaryRuntimeType(TypeReference reference)
    {
        if (_exactSummaryRuntimeTypes.TryGetValue(reference, out bool known)) return known;
        CheckMutable();
        Spend();
        bool exact;
        if (reference is GenericParameter { HasNotNullableValueTypeConstraint: true }) exact = true;
        else if (IsSymbolic(reference)) exact = false;
        else if (reference is ArrayType array) exact = ExactSummaryRuntimeType(array.ElementType);
        else if (reference is TypeSpecification && reference is not GenericInstanceType) exact = false;
        else if (reference.MetadataType == MetadataType.Object) exact = false;
        else if (reference.IsPrimitive || reference.MetadataType is MetadataType.String or MetadataType.Void or MetadataType.TypedByReference)
            exact = true;
        else exact = Resolve(reference) is { } definition && (definition.IsValueType || definition.IsSealed);
        _exactSummaryRuntimeTypes.Add(reference, exact);
        return exact;
    }
}

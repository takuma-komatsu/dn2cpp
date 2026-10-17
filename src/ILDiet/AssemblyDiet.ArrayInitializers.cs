using Mono.Cecil;

namespace Dn2Cpp;

internal sealed partial class AssemblyDiet
{
    private readonly HashSet<TypeReference> _arrayInitializerReferences = new();
    private readonly HashSet<TypeDefinition> _arrayInitializerLayouts = new();

    private void ObserveArrayInitializerReference(TypeReference reference)
    {
        CheckMutable();
        if (!_arrayInitializerReferences.Add(reference)) return;
        Spend();
        if (reference is FunctionPointerType pointer)
        {
            ObserveArrayInitializerReference(pointer.ReturnType);
            foreach (var parameter in pointer.Parameters) ObserveArrayInitializerReference(parameter.ParameterType);
            return;
        }
        if (reference is GenericInstanceType generic)
        {
            if (Resolve(generic) is { } owner) ObserveArrayInitializerLayout(owner);
            foreach (var argument in generic.GenericArguments) ObserveArrayInitializerReference(argument);
        }
        if (reference is TypeSpecification specification) ObserveArrayInitializerReference(specification.ElementType);
        if (reference.DeclaringType is { } declaring) ObserveArrayInitializerReference(declaring);
    }

    private void ObserveArrayInitializerLayout(TypeDefinition type)
    {
        CheckMutable();
        if (!ArrayInitializerEligible(type) || !_arrayInitializerLayouts.Add(type)) return;
        Spend();
        // A modeled value layout can gain its constructor during emission even
        // when only a signature or a discarded formation named that layout.
        if (_initializesArrays) KeepArrayInitializer(type);
    }
}

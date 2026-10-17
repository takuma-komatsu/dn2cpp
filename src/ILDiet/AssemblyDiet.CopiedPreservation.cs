using Mono.Cecil;

namespace Dn2Cpp;

internal sealed partial class AssemblyDiet
{
    private readonly HashSet<TypeDefinition> _copiedPreservationUses = new();

    private void NoteCopiedPreservationUse(TypeDefinition type)
    {
        CheckMutable();
        if (!UsesReachedCopyTraversal(type) || !_policyTypes.ContainsKey(type)
            || !_copiedPreservationUses.Add(type)) return;
        Spend();
        ApplyConditionalPolicy(type);
    }

    private void NotePreservedReference(TypeReference reference)
    {
        MarkSignatureType(reference);
        while (reference is OptionalModifierType or RequiredModifierType or PinnedType)
            reference = ((TypeSpecification)reference).ElementType;
        if (reference is GenericParameter or ProjectedTypeReference)
        {
            QueueDemand(reference, Demand.PreservedUse);
            return;
        }
        if (reference is GenericInstanceType generic)
        {
            if (Resolve(generic) is { } owner) NoteCopiedPreservationUse(owner);
            foreach (var argument in generic.GenericArguments) NotePreservedReference(argument);
        }
        else if (reference is ArrayType or ByReferenceType)
            NotePreservedReference(((TypeSpecification)reference).ElementType);
        else if (reference is not TypeSpecification && Resolve(reference) is { } type
            && CoreIntrinsicTypeMaps.WellKnownPrimitiveCode(Name(type)) is null)
        {
            NoteCopiedPreservationUse(type);
            foreach (var parameter in type.GenericParameters) QueueDemand(parameter, Demand.PreservedUse);
        }
    }

    private void MarkPreservedInitializer(TypeDefinition type)
    {
        if (UsesReachedCopyTraversal(type) && CopiedRuntimeOwnedType(type)) return;
        foreach (var method in type.Methods)
            if (method.IsConstructor && method.IsStatic) MarkMethod(method);
    }

    private void NoteCopiedListedOwner(TypeDefinition type)
    {
        // Primary emits a selected member's declaring table even when no body is
        // invoked. Runtime-owned tables have no managed declaring-class emission.
        if (UsesReachedCopyTraversal(type) && !CopiedRuntimeOwnedType(type)) NoteCopiedPreservationUse(type);
    }

    private void MarkPreservedMethod(MethodDefinition method)
    {
        MarkMethod(method);
        NoteReflectionInvokeBoxes(method);
        NotePreservedReference(method.ReturnType);
        foreach (var parameter in method.Parameters) NotePreservedReference(parameter.ParameterType);
        var owner = method.DeclaringType;
        if (!owner.IsBeforeFieldInit && (method.IsStatic || method.IsConstructor || owner.IsValueType))
            MarkPreservedInitializer(owner);
    }

    private void MarkPreservedField(FieldDefinition field)
    {
        MarkField(field);
        QueueDemand(field.FieldType, Demand.Boxed);
        NotePreservedReference(field.FieldType);
        MarkPreservedInitializer(field.DeclaringType);
    }
}

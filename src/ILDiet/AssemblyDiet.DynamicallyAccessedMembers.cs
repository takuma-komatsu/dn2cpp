using Mono.Cecil;
using AccessedMembers = System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes;
using MetadataTokens = System.Reflection.Metadata.Ecma335.MetadataTokens;

namespace Dn2Cpp;

internal sealed partial class AssemblyDiet
{
    private Demand AccessedMemberDemand(MethodReference reference, int parameter)
    {
        if (ResolveMember(reference) is not MethodDefinition method || !_byModule.ContainsKey(method.Module))
            return Demand.None;
        var members = GetReflectionSummarizer(method.Module).GetDynamicallyAccessedMemberTypes(
            MetadataTokens.MethodDefinitionHandle((int)method.MetadataToken.RID), parameter);
        return (Demand)((ulong)(uint)members << AccessedMembersShift);
    }

    private void MarkAttributeAccessedMembers(CustomAttributeArgument argument, Demand demand)
    {
        if (demand == Demand.None || argument.Value is not TypeReference type || !IsClosedAttributeType(type)) return;
        QueueDemand(type, demand);
    }

    private bool IsClosedAttributeType(TypeReference type) => type switch
    {
        GenericParameter => false,
        GenericInstanceType generic => generic.GenericArguments.All(IsClosedAttributeType),
        ArrayType array => IsClosedAttributeType(array.ElementType),
        TypeSpecification => false,
        _ => Resolve(type) is { HasGenericParameters: false },
    };

    private Demand AttributePropertyDemand(PropertyDefinition property, MethodDefinition setter)
    {
        if (property.PropertyType.FullName != "System.Type" || !_byModule.ContainsKey(property.Module))
            return Demand.None;
        var reader = GetReflectionSummarizer(property.Module).Reader;
        var handle = MetadataTokens.PropertyDefinitionHandle((int)property.MetadataToken.RID);
        var members = ReflectionCoreContracts.ReadAccessedMembers(reader, reader.GetPropertyDefinition(handle).GetCustomAttributes());
        return (Demand)((ulong)(uint)members << AccessedMembersShift) | AccessedMemberDemand(setter, 0);
    }

    private static Demand UnappliedDemands(Demand demands, Demand applied)
    {
        var delta = demands & ~applied;
        // Composite annotations need their earlier bits when a stronger demand
        // is replayed to an existing binding.
        if ((delta & Demand.AccessedMembers) != 0) delta |= demands & Demand.AccessedMembers;
        return delta;
    }

    private readonly HashSet<(TypeDefinition Owner, AccessedMembers Members, bool Inherited)> _accessedMemberSelections = new();

    private void MarkDynamicallyAccessedMembers(TypeReference reference, Demand demand)
    {
        var type = reference switch
        {
            ArrayType => _resolver.FindDefinition("System.Array"),
            TypeSpecification and not GenericInstanceType => null,
            _ => Resolve(reference),
        };
        if (type is null) return;
        var members = (AccessedMembers)((ulong)demand >> AccessedMembersShift);
        // Default construction can box a value type and reach its initializer
        // even when the type declares no constructor.
        if (type.IsValueType && (members & AccessedMembers.PublicParameterlessConstructor) != 0)
            MarkType(reference);
        else
            MarkSignatureType(reference);
        if ((members & AccessedMembers.Interfaces) != 0)
            foreach (var contract in InterfaceClosure(type))
            {
                if (Resolve(contract) is { } definition) NoteCopiedListedOwner(definition);
                if (members == AccessedMembers.All) QueueDemand(contract, Demand.AccessedMembers);
            }
        foreach (var owner in Ancestors(type))
        {
            bool inherited = owner != type || reference is ArrayType;
            if (!_accessedMemberSelections.Add((owner, members, inherited))) continue;
            Spend();
            MarkDeclaredAccessedMembers(owner, members, inherited);
        }
    }

    private static LookupVisibility MemberVisibility(bool isPublic, bool isPrivate) =>
        isPublic ? LookupVisibility.Public : isPrivate ? LookupVisibility.Private : LookupVisibility.NonPublic;

    private static LookupVisibility AccessorVisibility(MethodDefinition? first, MethodDefinition? second) =>
        MemberVisibility(first?.IsPublic == true || second?.IsPublic == true,
            first?.IsPrivate != false && second?.IsPrivate != false);

    private void MarkDeclaredAccessedMembers(TypeDefinition owner, AccessedMembers members, bool inherited)
    {
        bool Matches(LookupMemberKind kind, LookupVisibility visibility, bool parameterless = false) =>
            ReflectionLookupRules.MatchesAccessedMember(members, kind, visibility, inherited, parameterless);

        if ((members & (AccessedMembers.AllConstructors | AccessedMembers.AllMethods)) != 0)
            foreach (var method in owner.Methods)
            {
                Spend();
                if (!Matches(method.IsConstructor ? LookupMemberKind.Constructor : LookupMemberKind.Method,
                    MemberVisibility(method.IsPublic, method.IsPrivate), !method.HasParameters)) continue;
                NoteCopiedListedOwner(owner);
                MarkMethod(method);
                NoteReflectionInvokeBoxes(method);
            }
        if ((members & AccessedMembers.AllFields) != 0)
            foreach (var field in owner.Fields)
            {
                Spend();
                if (!Matches(LookupMemberKind.Field, MemberVisibility(field.IsPublic, field.IsPrivate))) continue;
                NoteCopiedListedOwner(owner);
                Require(owner, TypeState.Metadata);
                _fields.Add(field);
                MarkSignatureType(field.FieldType);
                if (field.IsLiteral) continue;
                QueueDemand(field.FieldType, Demand.Boxed);
                if (field.IsStatic) MarkPreservedInitializer(owner);
            }
        if ((members & AccessedMembers.AllProperties) != 0)
            foreach (var property in owner.Properties)
            {
                Spend();
                if (!Matches(LookupMemberKind.Property, AccessorVisibility(property.GetMethod, property.SetMethod))) continue;
                NoteCopiedListedOwner(owner);
                MarkProperty(property, true, true);
                foreach (var method in property.OtherMethods) MarkReflectedAccessor(method);
            }
        if ((members & AccessedMembers.AllEvents) != 0)
            foreach (var item in owner.Events)
            {
                Spend();
                if (!Matches(LookupMemberKind.Event, AccessorVisibility(item.AddMethod, item.RemoveMethod))) continue;
                NoteCopiedListedOwner(owner);
                MarkEvent(item);
            }
        if ((members & AccessedMembers.AllNestedTypes) != 0)
            foreach (var nested in owner.NestedTypes)
            {
                Spend();
                if (!Matches(LookupMemberKind.NestedType, MemberVisibility(nested.IsNestedPublic, nested.IsNestedPrivate))) continue;
                NoteCopiedListedOwner(nested);
                QueueDemand(nested, Demand.AccessedMembers);
            }
    }

    private readonly Dictionary<GenericParameter, Demand> _genericAccessedMemberDemands = new();

    private Demand GenericAccessedMemberDemand(GenericParameter parameter)
    {
        if (_genericAccessedMemberDemands.TryGetValue(parameter, out var demand))
            return demand;
        if (parameter.Module is not { } module || !_byModule.ContainsKey(module) || parameter.MetadataToken.RID == 0)
            return Demand.None;
        var reader = GetReflectionSummarizer(module).Reader;
        var handle = MetadataTokens.GenericParameterHandle((int)parameter.MetadataToken.RID);
        var members = ReflectionCoreContracts.ReadAccessedMembers(reader, reader.GetGenericParameter(handle).GetCustomAttributes());
        return _genericAccessedMemberDemands[parameter] = (Demand)((ulong)(uint)members << AccessedMembersShift);
    }
}

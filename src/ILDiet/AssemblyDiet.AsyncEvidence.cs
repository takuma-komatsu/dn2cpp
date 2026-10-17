using Mono.Cecil;
using MetadataTokens = System.Reflection.Metadata.Ecma335.MetadataTokens;

namespace Dn2Cpp;

internal sealed partial class AssemblyDiet
{
    private readonly HashSet<MethodDefinition> _formationBodies = new();
    private readonly Dictionary<Mono.Cecil.Cil.Instruction, int> _formationEvidenceInstructions = new();
    private readonly Dictionary<(int Module, int Token), bool> _formationWitnessResults = new();

    private void KeepCutMethod(MethodDefinition method)
    {
        CheckMutable();
        if (!IsStripped(method.DeclaringType)) return;
        if (method.HasBody) KeepFormationBody(method, OriginalBodyDeps(method));
        else KeepFormationMethod(method);
    }

    private bool KeepsCutMethod(MethodDefinition method) => _cutMethods.Contains(method)
        && (!IsStripped(method.DeclaringType) || KeepsMethod(method)
            && (!method.HasBody || _formationBodies.Contains(method)));

    private bool KeepOriginalAsyncWitness(RawAsyncAdoption.ContractReference witness)
    {
        var assembly = _assemblies[witness.Module.Index];
        if (assembly.Copy) return true;
        int token = MetadataTokens.GetToken(witness.Reference);
        var key = (witness.Module.Index, token);
        if (_formationWitnessResults.TryGetValue(key, out bool retained)) return retained;
        retained = KeepOriginalAsyncWitnessCarrier(witness, token);
        _formationWitnessResults.Add(key, retained);
        return retained;
    }

    private bool KeepOriginalAsyncWitnessCarrier(RawAsyncAdoption.ContractReference witness, int token)
    {
        var assembly = _assemblies[witness.Module.Index];
        var module = assembly.Assembly.MainModule;
        var reader = witness.Module.Reader;
        var bodyTokens = new List<int> { token };
        int specifications = System.Reflection.Metadata.Ecma335.MetadataReaderExtensions.GetTableRowCount(reader,
            System.Reflection.Metadata.Ecma335.TableIndex.MethodSpec);
        for (int row = 1; row <= specifications; row++)
        {
            Spend();
            var handle = MetadataTokens.MethodSpecificationHandle(row);
            if (reader.GetMethodSpecification(handle).Method == witness.Reference)
                bodyTokens.Add(MetadataTokens.GetToken(handle));
        }
        foreach (var handle in reader.CustomAttributes)
        {
            Spend();
            var attribute = reader.GetCustomAttribute(handle);
            if (MetadataTokens.GetToken(attribute.Constructor) != token) continue;
            var provider = FormationAttributeCarrier(assembly, reader, attribute.Parent);
            if (provider is ICustomAttributeProvider attributes && KeepFormationProvider(attributes))
            {
                KeepFormationAttributes(attributes);
                return true;
            }
        }
        foreach (var type in AllTypes(module.Types))
            foreach (var method in type.Methods)
            {
                Spend();
                if (!method.Overrides.Any(reference => EvidenceToken(reference, token))) continue;
                KeepFormationMethod(method);
                return true;
            }
        foreach (var handle in reader.MethodDefinitions)
        {
            Spend();
            var definition = reader.GetMethodDefinition(handle);
            if (definition.RelativeVirtualAddress == 0
                || (definition.ImplAttributes & System.Reflection.MethodImplAttributes.CodeTypeMask)
                    != System.Reflection.MethodImplAttributes.IL) continue;
            System.Collections.Immutable.ImmutableArray<byte> bytes;
            try
            {
                bytes = System.Reflection.Metadata.PEReaderExtensions.GetMethodBody(assembly.PE,
                    definition.RelativeVirtualAddress).GetILContent();
            }
            catch (BadImageFormatException)
            {
                continue;
            }
            bool possible = false;
            foreach (int bodyToken in bodyTokens)
                for (int i = 0; i + 3 < bytes.Length; i++)
                    if (bytes[i] == (byte)bodyToken && bytes[i + 1] == (byte)(bodyToken >> 8)
                        && bytes[i + 2] == (byte)(bodyToken >> 16) && bytes[i + 3] == (byte)(bodyToken >> 24))
                    {
                        possible = true;
                        break;
                    }
            if (!possible) continue;
            var method = (MethodDefinition)module.LookupToken(MetadataTokens.GetToken(handle));
            var facts = OriginalBodyDeps(method);
            var matches = facts.Operands.Where(operand => operand.Operand is MemberReference reference
                && EvidenceToken(reference, token) && !_registryFactories.ContainsKey(operand.Instruction)).ToArray();
            if (matches.Length == 0) continue;
            foreach (var match in matches) _formationEvidenceInstructions.TryAdd(match.Instruction, token);
            KeepFormationBody(method, facts);
            return true;
        }
        return false;
    }

    private object? FormationAttributeCarrier(DietAssembly assembly, System.Reflection.Metadata.MetadataReader reader,
        System.Reflection.Metadata.EntityHandle parent)
    {
        if (parent.Kind == System.Reflection.Metadata.HandleKind.AssemblyDefinition) return assembly.Assembly;
        if (parent.Kind == System.Reflection.Metadata.HandleKind.ModuleDefinition) return assembly.Assembly.MainModule;
        if (parent.Kind == System.Reflection.Metadata.HandleKind.GenericParameterConstraint)
            parent = reader.GetGenericParameterConstraint(
                (System.Reflection.Metadata.GenericParameterConstraintHandle)parent).Parameter;
        if (parent.Kind == System.Reflection.Metadata.HandleKind.GenericParameter)
            parent = reader.GetGenericParameter((System.Reflection.Metadata.GenericParameterHandle)parent).Parent;
        if (parent.Kind == System.Reflection.Metadata.HandleKind.Parameter)
            foreach (var handle in reader.MethodDefinitions)
            {
                Spend();
                if (!reader.GetMethodDefinition(handle).GetParameters().Any(parameter => parameter == parent)) continue;
                parent = handle;
                break;
            }
        if (parent.Kind == System.Reflection.Metadata.HandleKind.InterfaceImplementation)
            foreach (var handle in reader.TypeDefinitions)
            {
                Spend();
                if (!reader.GetTypeDefinition(handle).GetInterfaceImplementations().Any(item => item == parent)) continue;
                parent = handle;
                break;
            }
        return parent.Kind is System.Reflection.Metadata.HandleKind.TypeDefinition
            or System.Reflection.Metadata.HandleKind.MethodDefinition or System.Reflection.Metadata.HandleKind.FieldDefinition
            or System.Reflection.Metadata.HandleKind.PropertyDefinition or System.Reflection.Metadata.HandleKind.EventDefinition
            ? assembly.Assembly.MainModule.LookupToken(MetadataTokens.GetToken(parent)) : null;
    }

    private static bool EvidenceToken(MemberReference reference, int token) =>
        reference.MetadataToken.ToInt32() == token
        || reference is GenericInstanceMethod generic && generic.ElementMethod.MetadataToken.ToInt32() == token;

    private bool KeepFormationProvider(ICustomAttributeProvider provider)
    {
        switch (provider)
        {
            case AssemblyDefinition:
            case ModuleDefinition:
                return true;
            case TypeDefinition type:
                KeepFormationType(type);
                return true;
            case MethodDefinition method:
                if (method.IsConstructor && method.IsStatic) _attributeInitializerProviders.Add(method);
                KeepFormationMethod(method);
                return true;
            case FieldDefinition field:
                KeepFormationType(field.DeclaringType);
                return true;
            case PropertyDefinition property:
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
                if (property.GetMethod is not null) KeepFormationMethod(property.GetMethod);
                if (property.SetMethod is not null) KeepFormationMethod(property.SetMethod);
                foreach (var method in property.OtherMethods) KeepFormationMethod(method);
                return true;
            case EventDefinition item:
                KeepFormationType(item.DeclaringType);
                _events.Add(item);
                KeepFormationReference(item.EventType);
                if (item.AddMethod is not null) KeepFormationMethod(item.AddMethod);
                if (item.RemoveMethod is not null) KeepFormationMethod(item.RemoveMethod);
                if (item.InvokeMethod is not null) KeepFormationMethod(item.InvokeMethod);
                foreach (var method in item.OtherMethods) KeepFormationMethod(method);
                return true;
            case ParameterDefinition parameter when parameter.Method is MethodDefinition method:
                KeepFormationMethod(method);
                return true;
            case GenericParameter parameter when parameter.Owner is TypeDefinition type:
                KeepFormationType(type);
                return true;
            case GenericParameter parameter when parameter.Owner is MethodDefinition method:
                KeepFormationMethod(method);
                return true;
        }
        return false;
    }

    private void KeepFormationBody(MethodDefinition method, BodyDeps facts)
    {
        if (!_formationBodies.Add(method)) return;
        Spend();
        KeepFormationMethod(method);
        foreach (var reference in facts.Locals) KeepFormationReference(reference);
        foreach (var reference in facts.Catches) KeepFormationReference(reference);
        foreach (var operand in facts.Operands)
        {
            Spend();
            switch (operand.Operand)
            {
                case MethodReference reference:
                    PreserveMemberArrayKinds(reference);
                    KeepFormationReference(reference.DeclaringType);
                    KeepFormationReference(reference.ReturnType);
                    foreach (var parameter in reference.Parameters) KeepFormationReference(parameter.ParameterType);
                    if (reference is GenericInstanceMethod generic)
                        foreach (var argument in generic.GenericArguments) KeepFormationReference(argument);
                    if (ResolveMember(reference) is MethodDefinition target)
                    {
                        if (IsStripped(target.DeclaringType) && target.IsConstructor && target.IsStatic && target.HasBody)
                            KeepFormationBody(target, OriginalBodyDeps(target));
                        else KeepFormationMethod(target);
                    }
                    break;
                case FieldReference reference:
                    PreserveMemberArrayKinds(reference);
                    KeepFormationReference(reference.DeclaringType);
                    KeepFormationReference(reference.FieldType);
                    if (ResolveMember(reference) is FieldDefinition field) KeepFormationType(field.DeclaringType);
                    break;
                case TypeReference reference:
                    KeepFormationReference(reference);
                    break;
                case CallSite site:
                    KeepFormationReference(site.ReturnType);
                    foreach (var parameter in site.Parameters) KeepFormationReference(parameter.ParameterType);
                    break;
            }
        }
    }

    private void ValidateFormationBodies(DietPlan plan)
    {
        ValidateAsyncFormationPredicates();
        foreach (var method in _cutMethods)
            if (!KeepsCutMethod(method) || _methods.Contains(method)
                || Node(method) is { } node && ((node.State & MethodState.Body) != 0
                    || node.Body is { ReflectionPrepared: true }))
                throw new InvalidOperationException("ILDiet cut target lost physical retention or gained executable body authority");
        foreach (var method in _formationBodies)
        {
            if (!IsStripped(method.DeclaringType)) continue;
            if (!_types.Contains(method.DeclaringType) || !KeepsMethod(method)
                || Node(method)?.Body is not { } facts)
                throw new InvalidOperationException("ILDiet removed an original physical evidence body");
            foreach (var reference in facts.Locals) ValidateKeptType(reference);
            foreach (var reference in facts.Catches) ValidateKeptType(reference);
            foreach (var operand in facts.Operands)
                switch (operand.Operand)
                {
                    case MethodReference reference:
                        ValidateKeptType(reference.DeclaringType);
                        ValidateKeptType(reference.ReturnType);
                        foreach (var parameter in reference.Parameters) ValidateKeptType(parameter.ParameterType);
                        if (reference is GenericInstanceMethod generic)
                            foreach (var argument in generic.GenericArguments) ValidateKeptType(argument);
                        if (ResolveMember(reference) is MethodDefinition target
                            && IsStripped(target.DeclaringType) && !KeepsMethod(target))
                            throw new InvalidOperationException("ILDiet removed a physical evidence method token");
                        break;
                    case FieldReference reference:
                        ValidateKeptType(reference.DeclaringType);
                        ValidateKeptType(reference.FieldType);
                        break;
                    case TypeReference reference:
                        ValidateKeptType(reference);
                        break;
                    case CallSite site:
                        ValidateKeptType(site.ReturnType);
                        foreach (var parameter in site.Parameters) ValidateKeptType(parameter.ParameterType);
                        break;
                }
        }
        foreach (var pair in _formationEvidenceInstructions)
            if (pair.Key.Operand is not MemberReference reference || !EvidenceToken(reference, pair.Value)
                || plan.Redirects.Any(redirect => redirect.Instruction == pair.Key))
                throw new InvalidOperationException("ILDiet rewrote an original async decline witness");
    }
}

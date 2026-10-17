using Mono.Cecil;
using Mono.Cecil.Cil;
using CecilInstruction = Mono.Cecil.Cil.Instruction;

namespace Dn2Cpp;

internal sealed partial class AssemblyDiet
{
    private readonly HashSet<TypeDefinition> _conditionalOwnMembers = new();
    private readonly HashSet<TypeDefinition> _backendAttributeOwners = new();
    private readonly HashSet<TypeDefinition> _suppressDefaultSeeds = new();
    private readonly List<CustomAttribute> _registrationAttributes = new();
    private readonly HashSet<CustomAttribute> _registrationAttributeSeen = new();
    private readonly Dictionary<CecilInstruction, RegistryFactory> _registryFactories = new();
    private bool _constructorRegistriesRewritten;
    private readonly List<(string Scope, string Reason)> _registryDiagnostics = new();
    private long _registryKept, _registryRedirected, _registryUnknown, _registryScriptsKept, _registryScriptsFiltered;
    private readonly List<RegistryDescription> _registryDescriptions = new();
    private readonly Dictionary<MethodDefinition, string> _factoryKeepReasons = new();

    private sealed class RegistryDescription
    {
        internal required MethodDefinition Registry;
        internal readonly List<(MethodReference Reference, RegistryFactory? Factory)> Entries = new();
    }

    private sealed class RegistryFactory
    {
        internal required MethodDefinition Registry;
        internal required MethodDefinition Method;
        internal required TypeDefinition ConstructedType;
    }

    private TypeDefinition FindPolicyType(string assembly, string name)
    {
        return _assemblies.Where(a => a.Assembly.Name.Name == assembly)
            .SelectMany(a => AllTypes(a.Assembly.MainModule.Types))
            .FirstOrDefault(t => Name(t) == name)
            ?? throw new NotSupportedException("backend policy type was not found: " + assembly + ":" + name);
    }

    private void ConfigureBackendPolicy()
    {
        foreach (var rule in _request.ConditionalMembers)
        {
            var assembly = _assemblies.FirstOrDefault(a => a.Assembly.Name.Name == rule.Assembly)
                ?? throw new NotSupportedException("conditional-member assembly was not found: " + rule.Assembly);
            var bases = _assemblies.SelectMany(a => AllTypes(a.Assembly.MainModule.Types))
                .Where(t => Name(t) == rule.BaseType).ToList();
            if (bases.Count != 1)
                throw new NotSupportedException("conditional-member base must identify one loaded type: " + rule.BaseType);
            if (assembly.Copy) continue;
            foreach (var type in AllTypes(assembly.Assembly.MainModule.Types))
            {
                var seen = new HashSet<TypeDefinition>();
                for (var parent = type.BaseType; parent is not null;)
                {
                    var definition = Resolve(parent);
                    if (definition == bases[0])
                    {
                        _conditionalOwnMembers.Add(type);
                        _suppressDefaultSeeds.Add(type);
                        break;
                    }
                    if (definition is null || !seen.Add(definition)) break;
                    parent = definition.BaseType;
                }
            }
        }
        // Backend script roots read type and declared-method attributes along
        // their user base chain before managed reflection runs. Record that
        // contract independently of when Runtime propagates through the graph.
        foreach (var root in _request.TypeRoots)
        {
            var type = FindPolicyType(root.Assembly, root.Type);
            if (!_conditionalOwnMembers.Contains(type)) continue;
            var seen = new HashSet<TypeDefinition>();
            for (var current = type; current is not null && seen.Add(current);
                 current = current.BaseType is null ? null : Resolve(current.BaseType))
                if (_conditionalOwnMembers.Contains(current)) _backendAttributeOwners.Add(current);
        }
        foreach (var rule in _request.SuppressedSeedTypes)
        {
            var type = FindPolicyType(rule.Assembly, rule.Type);
            if (IsStripped(type)) _suppressDefaultSeeds.Add(type);
        }
        foreach (var rule in _request.ConstructorRegistries)
        {
            var type = FindPolicyType(rule.Assembly, rule.Type);
            var methods = type.Methods.Where(m => m.Name == rule.Method).ToList();
            if (methods.Count == 0)
                throw new NotSupportedException("constructor registry was not found: " + rule.Type + "::" + rule.Method);
            if (!IsStripped(type)) continue;
            foreach (var method in methods)
            {
                if (!method.HasBody || _registryDescriptions.Any(d => d.Registry == method)) continue;
                var description = new RegistryDescription { Registry = method };
                _registryDescriptions.Add(description);
                foreach (var instruction in method.Body.Instructions)
                {
                    if (instruction.OpCode.Code is not (Code.Ldftn or Code.Ldvirtftn)
                        || instruction.Operand is not MethodReference reference) continue;
                    var factory = instruction.OpCode.Code == Code.Ldftn ? RecognizeFactory(method, reference) : null;
                    description.Entries.Add((reference, factory));
                    if (factory is null) _registryUnknown++;
                    else _registryFactories.TryAdd(instruction, factory);
                }
            }
        }
    }

    private void KeepOwnMembers(TypeDefinition type)
    {
        foreach (var method in type.Methods) MarkMethod(method);
        foreach (var property in type.Properties) MarkProperty(property, true, true);
        foreach (var item in type.Events) MarkEvent(item);
    }

    private RegistryFactory? RecognizeFactory(MethodDefinition registry, MethodReference reference)
    {
        var method = ResolveMember(reference) as MethodDefinition;
        if (method is null || !method.HasBody || method.HasGenericParameters
            || method.Module != registry.Module || method.Body.ExceptionHandlers.Count != 0)
            return null;
        // Only a straight constructor adapter is safe to redirect. Debug builds
        // may spill the result and branch to their single return instruction.
        var instructions = method.Body.Instructions.Where(i => i.OpCode.Code != Code.Nop).ToList();
        int index = 0;
        while (index < instructions.Count && IsArgumentLoad(instructions[index]))
        {
            if (ArgumentIndex(instructions[index], method) != index) return null;
            index++;
        }
        if (index >= instructions.Count || instructions[index].OpCode.Code != Code.Newobj
            || instructions[index].Operand is not MethodReference constructor)
            return null;
        var constructed = Resolve(constructor.DeclaringType);
        if (constructed is null || constructed.Module != registry.Module || !IsStripped(constructed)
            || constructor.HasGenericParameters || constructor.DeclaringType is GenericInstanceType
            || constructor.Parameters.Count != index || method.Parameters.Count != index
            || !constructor.Parameters.Zip(method.Parameters).All(p =>
                SameType(p.First.ParameterType, p.Second.ParameterType))
            || !ReturnsCompatibleType(constructed, method.ReturnType))
            return null;
        index++;
        if (index < instructions.Count && IsLocalStore(instructions[index]))
        {
            var store = instructions[index++];
            if (index < instructions.Count && instructions[index].OpCode.Code is Code.Br or Code.Br_S)
            {
                if (instructions[index].Operand is not CecilInstruction target) return null;
                while (target.OpCode.Code == Code.Nop && target.Next is not null) target = target.Next;
                if (index + 1 >= instructions.Count || instructions[index + 1] != target) return null;
                index++;
            }
            if (index >= instructions.Count || !SameLocal(store, instructions[index])) return null;
            index++;
        }
        if (index + 1 != instructions.Count || instructions[index].OpCode.Code != Code.Ret)
            return null;
        return new RegistryFactory { Registry = registry, Method = method, ConstructedType = constructed };
    }

    private static bool IsArgumentLoad(CecilInstruction instruction) => instruction.OpCode.Code
        is Code.Ldarg or Code.Ldarg_S or Code.Ldarg_0 or Code.Ldarg_1 or Code.Ldarg_2 or Code.Ldarg_3;

    private static int ArgumentIndex(CecilInstruction instruction, MethodDefinition method)
    {
        int slot = instruction.OpCode.Code switch
        {
            Code.Ldarg_0 => 0,
            Code.Ldarg_1 => 1,
            Code.Ldarg_2 => 2,
            Code.Ldarg_3 => 3,
            Code.Ldarg or Code.Ldarg_S when instruction.Operand is ParameterDefinition parameter
                => parameter.Index + (method.HasThis ? 1 : 0),
            _ => -1,
        };
        return slot - (method.HasThis ? 1 : 0);
    }

    private static bool IsLocalStore(CecilInstruction instruction) => instruction.OpCode.Code
        is Code.Stloc or Code.Stloc_S or Code.Stloc_0 or Code.Stloc_1 or Code.Stloc_2 or Code.Stloc_3;

    private static bool SameLocal(CecilInstruction store, CecilInstruction load)
    {
        static int Slot(CecilInstruction instruction) => instruction.OpCode.Code switch
        {
            Code.Stloc_0 or Code.Ldloc_0 => 0,
            Code.Stloc_1 or Code.Ldloc_1 => 1,
            Code.Stloc_2 or Code.Ldloc_2 => 2,
            Code.Stloc_3 or Code.Ldloc_3 => 3,
            Code.Stloc or Code.Stloc_S or Code.Ldloc or Code.Ldloc_S
                when instruction.Operand is VariableDefinition variable => variable.Index,
            _ => -1,
        };
        return load.OpCode.Code is Code.Ldloc or Code.Ldloc_S or Code.Ldloc_0 or Code.Ldloc_1 or Code.Ldloc_2 or Code.Ldloc_3
            && Slot(load) >= 0 && Slot(store) == Slot(load);
    }

    private bool ReturnsCompatibleType(TypeDefinition constructed, TypeReference returnType)
    {
        var seen = new HashSet<TypeDefinition>();
        for (var current = constructed; current is not null && seen.Add(current);
             current = current.BaseType is null ? null : Resolve(current.BaseType))
            if (SameType(current, returnType)) return true;
        return false;
    }

    private bool SameType(TypeReference left, TypeReference right)
    {
        if (left.FullName != right.FullName) return false;
        if (left is FunctionPointerType or IModifierType || right is FunctionPointerType or IModifierType)
            return false;
        if (left is TypeSpecification || right is TypeSpecification)
        {
            if (left is not TypeSpecification first || right is not TypeSpecification second
                || first.GetType() != second.GetType() || !SameType(first.ElementType, second.ElementType))
                return false;
            if (first is GenericInstanceType firstGeneric && second is GenericInstanceType secondGeneric)
                return firstGeneric.GenericArguments.Count == secondGeneric.GenericArguments.Count
                    && firstGeneric.GenericArguments.Zip(secondGeneric.GenericArguments).All(p => SameType(p.First, p.Second));
            return true;
        }
        if (left is GenericParameter || right is GenericParameter) return left == right;
        var leftType = Resolve(left);
        var rightType = Resolve(right);
        if (leftType is not null || rightType is not null) return leftType == rightType;
        static string Scope(TypeReference type) => type.Scope switch
        {
            AssemblyNameReference assembly => assembly.FullName,
            ModuleDefinition module => module.Assembly.Name.FullName,
            _ => "",
        };
        string scope = Scope(left);
        return scope.Length != 0 && scope == Scope(right);
    }

    private bool CompatibleFactories(RegistryFactory first, RegistryFactory second)
    {
        var left = first.Method;
        var right = second.Method;
        // Instance delegates retain the original receiver object. Requiring its
        // declaring type also preserves the receiver cast after ldftn replacement.
        return left.IsStatic == right.IsStatic && left.DeclaringType == right.DeclaringType
            && SameType(left.ReturnType, right.ReturnType)
            && left.CallingConvention == right.CallingConvention
            && left.Parameters.Count == right.Parameters.Count
            && left.Parameters.Zip(right.Parameters).All(p => SameType(p.First.ParameterType, p.Second.ParameterType));
    }

    private RegistryFactory? FindAncestorFactory(RegistryFactory factory, IReadOnlyList<RegistryFactory> factories)
    {
        var seen = new HashSet<TypeDefinition>();
        for (var parent = factory.ConstructedType.BaseType; parent is not null;)
        {
            var type = Resolve(parent);
            if (type is null || !seen.Add(type)) return null;
            if (_runtimeTypes.Contains(type) || Node(type) is { } node && (node.State & TypeState.Explicit) != 0)
            {
                var candidate = factories.FirstOrDefault(f => f.ConstructedType == type
                    && f.Registry == factory.Registry && CompatibleFactories(factory, f));
                if (candidate is not null) return candidate;
            }
            parent = type.BaseType;
        }
        return null;
    }

    private bool RuntimeOrExplicit(TypeDefinition type) => _runtimeTypes.Contains(type)
        || Node(type) is { } node && (node.State & TypeState.Explicit) != 0;

    private bool KeepsRegistryFactory(MethodDefinition method) => _methods.Contains(method) || KeepsCutMethod(method);

    private void RegistryRule(TypeDefinition type)
    {
        if (!RuntimeOrExplicit(type)) return;
        foreach (var factory in _registryFactories.Values)
            if (factory.ConstructedType == type && _methods.Contains(factory.Registry))
            {
                _factoryKeepReasons.TryAdd(factory.Method, _runtimeTypes.Contains(type) ? "runtime type" : "explicit type root");
                MarkMethod(factory.Method);
            }
    }

    private void CloseRegistries()
    {
        while (true)
        {
            var factories = _registryFactories.Values.Where(f => _methods.Contains(f.Registry)).ToList();
            foreach (var factory in factories)
            {
                Spend();
                if (KeepsCutMethod(factory.Method))
                {
                    _factoryKeepReasons[factory.Method] = "cut target";
                    continue;
                }
                if (_methods.Contains(factory.Method)) _factoryKeepReasons.TryAdd(factory.Method, "direct body dependency");
                if (RuntimeOrExplicit(factory.ConstructedType))
                {
                    _factoryKeepReasons.TryAdd(factory.Method, _runtimeTypes.Contains(factory.ConstructedType)
                        ? "runtime type" : "explicit type root");
                    MarkMethod(factory.Method);
                }
                else if (FindAncestorFactory(factory, factories) is null)
                {
                    _factoryKeepReasons.TryAdd(factory.Method, "no compatible retained ancestor");
                    MarkMethod(factory.Method);
                }
            }
            if (_work.Count == 0) break;
            Drain();
        }
    }

    private void PlanRegistryDiagnostics()
    {
        foreach (var description in _registryDescriptions)
        {
            var registry = description.Registry;
            var reasons = new List<string>();
            var examples = new List<string>();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            int kept = 0;
            if (_methods.Contains(registry))
                foreach (var entry in description.Entries)
                {
                    if (entry.Factory is { } factory && !KeepsRegistryFactory(factory.Method)) continue;
                    string identity = entry.Reference.FullName;
                    if (!seen.Add(identity)) continue;
                    kept++;
                    string reason = entry.Factory is null ? "unrecognized factory shape"
                        : _factoryKeepReasons.TryGetValue(entry.Factory.Method, out var value) ? value : "direct body dependency";
                    if (!reasons.Contains(reason)) reasons.Add(reason);
                    if (examples.Count < 3) examples.Add(entry.Reference.Name);
                }
            if (reasons.Count == 0) reasons.Add(!_methods.Contains(registry) ? "registry body removed"
                : description.Entries.Count == 0 ? "no factory entries" : "compatible entries redirected");
            reasons.Sort(StringComparer.Ordinal);
            string reasonText = string.Join(", ", reasons);
            if (examples.Count != 0) reasonText += "; examples " + string.Join(", ", examples);
            _registryDiagnostics.Add((registry.Module.Assembly.Name.Name + ":" + Name(registry.DeclaringType) + "::" + registry.Name,
                "keeps " + kept + " original factories (" + reasonText + ")"));
        }
    }

    private bool IsRegistrationAttribute(CustomAttribute attribute, ICustomAttributeProvider provider)
    {
        if (provider is not AssemblyDefinition assembly
            || !_byModule.TryGetValue(assembly.MainModule, out var owner) || owner.Copy)
            return false;
        var type = Resolve(attribute.AttributeType);
        if (type is null || !_request.RegistrationAttributes.Any(rule => rule.Assembly == type.Module.Assembly.Name.Name
                && rule.Type == Name(type)))
            return false;
        if (_registrationAttributeSeen.Add(attribute)) _registrationAttributes.Add(attribute);
        return true;
    }

    private static bool IsTypeArray(CustomAttributeArgument argument) => argument.Type is ArrayType array
        && array.ElementType.FullName == "System.Type" && argument.Value is CustomAttributeArgument[];

    private CustomAttributeArgument EffectiveAttributeArgument(CustomAttribute attribute, CustomAttributeArgument argument)
    {
        if (!_registrationAttributeSeen.Contains(attribute) || !IsTypeArray(argument)) return argument;
        var values = (CustomAttributeArgument[])argument.Value;
        var kept = values.Where(value => value.Value is not TypeReference reference
            || Resolve(reference) is not { } type || !IsStripped(type) || _runtimeTypes.Contains(type)).ToArray();
        return kept.Length == values.Length ? argument : new CustomAttributeArgument(argument.Type, kept);
    }

    private IEnumerable<CustomAttributeArgument> EffectiveAttributeArguments(CustomAttribute attribute)
    {
        foreach (var argument in attribute.ConstructorArguments)
            yield return EffectiveAttributeArgument(attribute, argument);
    }

    private void RegistrationRuntimeChanged()
    {
        // Runtime use can admit a previously filtered entry. Replaying the row
        // never exposes an original entry before that independent use exists.
        foreach (var attribute in _registrationAttributes)
        {
            if (!_selectedAttributeRows.TryGetValue(attribute, out var row)) continue;
            Spend();
            row.MetadataKept = row.ArgumentsObserved = row.ArgumentIdentitiesObserved = false;
            row.Constructed = false;
            if (row.RetentionRequested) _attributeMetadataPending.Enqueue(row);
            QueueAttributeIdentity(row);
        }
    }
}

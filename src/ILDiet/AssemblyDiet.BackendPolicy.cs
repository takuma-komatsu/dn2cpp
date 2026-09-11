using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Dn2Cpp;

internal sealed partial class AssemblyDiet
{
    private readonly HashSet<TypeDefinition> _conditionalOwnMembers = new();
    private readonly HashSet<TypeDefinition> _suppressDefaultSeeds = new();
    private readonly HashSet<CustomAttribute> _registrationAttributes = new();
    private readonly Dictionary<Instruction, RegistryFactory> _registryFactories = new();
    private bool _constructorRegistriesRewritten;

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
            if (assembly.Copy) continue;
            var bases = _assemblies.SelectMany(a => AllTypes(a.Assembly.MainModule.Types))
                .Where(t => Name(t) == rule.BaseType).ToList();
            if (bases.Count != 1)
                throw new NotSupportedException("conditional-member base must identify one loaded type: " + rule.BaseType);
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
        foreach (var rule in _request.SuppressedSeedTypes)
        {
            var type = FindPolicyType(rule.Assembly, rule.Type);
            if (IsStripped(type)) _suppressDefaultSeeds.Add(type);
        }
        foreach (var rule in _request.ConstructorRegistries)
        {
            var type = FindPolicyType(rule.Assembly, rule.Type);
            if (!IsStripped(type)) continue;
            var methods = type.Methods.Where(m => m.Name == rule.Method).ToList();
            if (methods.Count == 0)
                throw new NotSupportedException("constructor registry was not found: " + rule.Type + "::" + rule.Method);
            foreach (var method in methods)
            {
                if (!method.HasBody) continue;
                int recognized = 0, unknown = 0;
                foreach (var instruction in method.Body.Instructions)
                {
                    if (instruction.OpCode.Code != Code.Ldftn || instruction.Operand is not MethodReference reference)
                        continue;
                    var factory = RecognizeFactory(method, reference);
                    if (factory is null) unknown++;
                    else
                    {
                        _registryFactories.TryAdd(instruction, factory);
                        recognized++;
                    }
                }
                if (unknown != 0 || recognized == 0)
                    Console.WriteLine("ILDiet: constructor registry " + rule.Type + "::" + rule.Method
                        + " retains unrecognized factory shapes");
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
        MethodDefinition? method;
        try { method = reference.Resolve(); }
        catch (AssemblyResolutionException) { return null; }
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
                if (instructions[index].Operand is not Instruction target) return null;
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

    private static bool IsArgumentLoad(Instruction instruction) => instruction.OpCode.Code
        is Code.Ldarg or Code.Ldarg_S or Code.Ldarg_0 or Code.Ldarg_1 or Code.Ldarg_2 or Code.Ldarg_3;

    private static int ArgumentIndex(Instruction instruction, MethodDefinition method)
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

    private static bool IsLocalStore(Instruction instruction) => instruction.OpCode.Code
        is Code.Stloc or Code.Stloc_S or Code.Stloc_0 or Code.Stloc_1 or Code.Stloc_2 or Code.Stloc_3;

    private static bool SameLocal(Instruction store, Instruction load)
    {
        static int Slot(Instruction instruction) => instruction.OpCode.Code switch
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
            if (_types.Contains(type))
            {
                var candidate = factories.FirstOrDefault(f => f.ConstructedType == type
                    && f.Registry == factory.Registry && CompatibleFactories(factory, f));
                if (candidate is not null) return candidate;
            }
            parent = type.BaseType;
        }
        return null;
    }

    private void CompleteBackendPolicy()
    {
        // A retained factory can make another wrapper live through constructor IL.
        // Reach a fixed point before choosing replacements or filtering attributes.
        while (true)
        {
            var factories = _registryFactories.Values.Where(f => _methods.Contains(f.Registry)).ToList();
            foreach (var factory in factories)
            {
                if (_types.Contains(factory.ConstructedType) || FindAncestorFactory(factory, factories) is null)
                    MarkMethod(factory.Method);
            }
            if (_pending.Count == 0) break;
            while (_pending.Count != 0) Scan(_pending.Dequeue());
        }
        var active = _registryFactories.Values.Where(f => _methods.Contains(f.Registry)).ToList();
        foreach (var pair in _registryFactories)
        {
            var factory = pair.Value;
            if (!_methods.Contains(factory.Registry) || _types.Contains(factory.ConstructedType)) continue;
            var ancestor = FindAncestorFactory(factory, active)
                ?? throw new InvalidOperationException("constructor registry has no retained compatible ancestor factory");
            pair.Key.Operand = ancestor.Method;
            _constructorRegistriesRewritten = true;
        }
        foreach (var attribute in _registrationAttributes)
            for (int i = 0; i < attribute.ConstructorArguments.Count; i++)
            {
                var argument = attribute.ConstructorArguments[i];
                if (!IsTypeArray(argument)) continue;
                var values = (CustomAttributeArgument[])argument.Value;
                var kept = values.Where(value => value.Value is not TypeReference reference
                    || Resolve(reference) is not { } type || !IsStripped(type) || _types.Contains(type)).ToArray();
                attribute.ConstructorArguments[i] = new CustomAttributeArgument(argument.Type, kept);
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
        _registrationAttributes.Add(attribute);
        return true;
    }

    private static bool IsTypeArray(CustomAttributeArgument argument) => argument.Type is ArrayType array
        && array.ElementType.FullName == "System.Type" && argument.Value is CustomAttributeArgument[];
}

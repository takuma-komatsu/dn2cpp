using Mono.Cecil;
using Mono.Cecil.Cil;

internal static class KnownConstantFixture
{
    public static bool Run(string[] args)
    {
        if (args.Length != 3 || !new[] { "--known-constant", "--known-constant-oracle",
            "--known-constant-reflection", "--known-constant-reflection-oracle",
            "--known-constant-caller", "--known-constant-caller-oracle",
            "--known-constant-generic-reflection", "--known-constant-generic-reflection-oracle",
            "--known-constant-generic-struct", "--known-constant-generic-struct-oracle" }.Contains(args[0])) return false;
        using var core = AssemblyDefinition.ReadAssembly(args[2]);
        if (core.Name.Name != "System.Private.CoreLib") throw new InvalidOperationException("Expected CoreLib");
        var owner = core.MainModule.GetType("System.Runtime.CompilerServices.RuntimeHelpers");
        var definitions = owner.Methods.Where(m => m.Name == "IsKnownConstant").ToArray();
        if (definitions.Length != 4) throw new InvalidOperationException("Known constant overload inventory changed");
        foreach (var method in definitions)
        {
            if (method.Attributes != (MethodAttributes.Assembly | MethodAttributes.Static | MethodAttributes.HideBySig)
                || method.ImplAttributes != MethodImplAttributes.IL || method.HasThis || method.ExplicitThis
                || method.ReturnType.MetadataType != MetadataType.Boolean || method.Parameters.Count != 1
                || !method.HasBody || method.Body.HasVariables || method.Body.HasExceptionHandlers
                || method.Body.Instructions.Count != 2 || method.Body.Instructions[0].OpCode != OpCodes.Ldc_I4_0
                || method.Body.Instructions[1].OpCode != OpCodes.Ret
                || method.Body.Instructions.Any(i => i.Operand is not null))
                throw new InvalidOperationException("Known constant declaration/token-free IL changed");
            if (method.HasGenericParameters)
            {
                var parameter = method.GenericParameters.Single();
                if (method.CallingConvention != MethodCallingConvention.Generic
                    || method.Parameters[0].ParameterType != parameter
                    || parameter.Attributes != (GenericParameterAttributes.NotNullableValueTypeConstraint | GenericParameterAttributes.DefaultConstructorConstraint)
                    || parameter.Constraints.Count != 1
                    || parameter.Constraints[0].ConstraintType.FullName != "System.ValueType"
                    || parameter.Constraints[0].ConstraintType.Scope != core.MainModule)
                    throw new InvalidOperationException("Known constant struct declaration changed");
            }
            else if (method.CallingConvention != MethodCallingConvention.Default
                || method.Parameters[0].ParameterType.FullName is not ("System.Type" or "System.String" or "System.Char"))
                throw new InvalidOperationException("Known constant scalar declaration changed");
        }
        string path = Path.GetFullPath(args[1]);
        using var app = AssemblyDefinition.ReadAssembly(path, new ReaderParameters { InMemory = true });
        var module = app.MainModule;
        var program = module.GetType("KnownConstantSubset.Program");
        MethodDefinition Method(string name) => program.Methods.Single(m => m.Name == name);
        bool oracle = args[0].EndsWith("-oracle", StringComparison.Ordinal);
        bool reflection = args[0].Contains("-reflection", StringComparison.Ordinal);
        bool genericReflection = args[0].Contains("-generic-reflection", StringComparison.Ordinal);
        bool genericStruct = args[0].Contains("-generic-struct", StringComparison.Ordinal);
        bool caller = args[0].Contains("-caller", StringComparison.Ordinal);
        var map = new Dictionary<string, MethodDefinition>
        {
            ["HintType"] = definitions.Single(m => !m.HasGenericParameters && m.Parameters[0].ParameterType.FullName == "System.Type"),
            ["HintString"] = definitions.Single(m => !m.HasGenericParameters && m.Parameters[0].ParameterType.MetadataType == MetadataType.String),
            ["HintChar"] = definitions.Single(m => !m.HasGenericParameters && m.Parameters[0].ParameterType.MetadataType == MetadataType.Char),
            ["HintGeneric"] = definitions.Single(m => m.HasGenericParameters),
        };
        foreach (var pair in map)
        {
            var copy = Method(pair.Key);
            copy.Body = new MethodBody(copy);
            foreach (var instruction in pair.Value.Body.Instructions)
                copy.Body.Instructions.Add(Instruction.Create(instruction.OpCode));
        }
        if (oracle)
        {
            foreach (var instruction in Method("ReflectionGenericMethod").Body.Instructions)
            {
                if (instruction.OpCode == OpCodes.Ldtoken && instruction.Operand is TypeReference type
                    && type.FullName == owner.FullName) instruction.Operand = program;
                if (instruction.OpCode == OpCodes.Ldstr && (string)instruction.Operand == "IsKnownConstant")
                    instruction.Operand = "HintGeneric";
            }
            // Reflection oracle also uses only owned copied bodies, not private runtime invocation.
            foreach (string suffix in new[] { "Type", "String", "Char" })
            {
                var method = Method("Reflection" + suffix);
                method.Body = new MethodBody(method);
                var il = method.Body.GetILProcessor();
                il.Emit(OpCodes.Ldarg_0); il.Emit(OpCodes.Call, Method("Hint" + suffix)); il.Emit(OpCodes.Ret);
            }
        }
        else if (!caller)
        {
            int direct = 0, address = 0, generic = 0;
            foreach (var method in program.Methods.Where(m => m.HasBody))
            foreach (var instruction in method.Body.Instructions)
            {
                if (instruction.Operand is not MethodReference reference || reference.DeclaringType.FullName != program.FullName
                    || !map.TryGetValue(reference.Name, out var definition)) continue;
                MethodReference target = module.ImportReference(definition);
                if (reference is GenericInstanceMethod instance)
                {
                    var closed = new GenericInstanceMethod(target);
                    foreach (var argument in instance.GenericArguments) closed.GenericArguments.Add(argument);
                    target = closed; generic++;
                }
                instruction.Operand = target;
                if (instruction.OpCode == OpCodes.Ldftn) address++; else direct++;
            }
            if (direct == 0 || address != 9 || generic == 0) throw new InvalidOperationException($"Missing hint sites: {direct}/{address}/{generic}");
            Console.WriteLine($"known-constant-sites direct={direct} address={address} generic={generic}; native-only");
        }
        if ((caller || (!reflection && !genericReflection && !genericStruct)) && !oracle)
        {
            var span = core.MainModule.GetType("System.SpanHelpers");
            var definition = span.Methods.Single(m => m.Name == "IndexOfAnyValueType" && m.GenericParameters.Count == 2 && m.Parameters.Count == 4);
            int hints = definition.Body.Instructions.Count(i => i.Operand is GenericInstanceMethod m
                && m.Name == "IsKnownConstant" && m.DeclaringType.FullName == owner.FullName);
            if (hints != 2) throw new InvalidOperationException("Actual two-candidate hint caller changed");
            foreach (string name in new[] { "Caller", "CallerExcept" })
            {
                var negator = span.NestedTypes.Single(t => t.Name == (name == "Caller" ? "DontNegate`1" : "Negate`1"));
                var closedNegator = new GenericInstanceType(module.ImportReference(negator));
                closedNegator.GenericArguments.Add(module.TypeSystem.Int16);
                var target = new GenericInstanceMethod(module.ImportReference(definition));
                target.GenericArguments.Add(module.TypeSystem.Int16); target.GenericArguments.Add(closedNegator);
                var method = Method(name); method.Body = new MethodBody(method);
                var il = method.Body.GetILProcessor();
                il.Emit(OpCodes.Ldarg_0); il.Emit(OpCodes.Ldarg_1); il.Emit(OpCodes.Ldarg_2); il.Emit(OpCodes.Ldarg_3);
                il.Emit(OpCodes.Call, target); il.Emit(OpCodes.Ret);
            }
            Console.WriteLine("known-constant-actual-caller=SpanHelpers.IndexOfAnyValueType<TValue,TNegator>; original-hint-sites=2; native-only");
        }
        module.EntryPoint = Method(caller ? "RunCallerArtifact" : genericStruct ? "RunGenericStructArtifact" : genericReflection ? "RunGenericReflectionArtifact" : reflection ? "RunReflectionArtifact" : "RunArtifact");
        app.Write(path, new WriterParameters { Timestamp = 0, DeterministicMvid = true });
        Console.WriteLine("known-constant-fixture=" + (caller ? "caller" : genericStruct ? "generic-struct" : genericReflection ? "generic-reflection" : reflection ? "reflection-only" : "all")
            + (oracle ? "; token-free-owned-IL/public-CLR-oracle" : "; exact-private-references-native-only"));
        return true;
    }
}

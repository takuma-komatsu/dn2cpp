using Mono.Cecil;
using Mono.Cecil.Cil;

internal static class ArraySortFactoryFixture
{
    public static bool Run(string[] args)
    {
        if (args.Length != 4 || args[0] != "--array-sort-factory-fixture") return false;
        string output = Path.GetFullPath(args[3]);
        if (File.Exists(output)) throw new InvalidOperationException("fixture output must be new");
        using var core = AssemblyDefinition.ReadAssembly(args[2]);
        if (core.Name.Name != "System.Private.CoreLib") throw new InvalidOperationException("Expected original CoreLib");
        using var app = AssemblyDefinition.ReadAssembly(args[1], new ReaderParameters { InMemory = true });
        var module = app.MainModule;
        var program = module.GetType("ArraySortFactoryWitness.Program");
        MethodDefinition Method(string name) => program.Methods.Single(method => method.Name == name);
        TypeReference AppType(string name) => module.GetType("ArraySortFactoryWitness." + name);
        TypeDefinition CoreType(string name) => core.MainModule.GetType(name);
        var runtimeType = CoreType("System.RuntimeType");
        var wrappers = CoreType("System.RuntimeTypeHandle").Methods.Where(method => method.Name == "CreateInstanceForAnotherGenericParameter"
            && method.Parameters.Count is 2 or 3).OrderBy(method => method.Parameters.Count).ToArray();
        if (wrappers.Length != 2 || wrappers.Any(method => method.Attributes != (MethodAttributes)0x0093
            || method.ImplAttributes != MethodImplAttributes.IL || !method.HasBody || method.HasThis
            || method.HasGenericParameters || method.ReturnType.MetadataType != MetadataType.Object
            || method.Parameters.Any(parameter => parameter.ParameterType.Resolve() != runtimeType)))
            throw new InvalidOperationException("Generic activation wrapper contract changed");
        foreach (int arity in new[] { 1, 2 })
        {
            var owner = CoreType("System.Collections.Generic.ArraySortHelper`" + arity);
            var factory = owner.Methods.Single(method => method.Name == "CreateArraySortHelper");
            if (factory.Attributes != (MethodAttributes)0x0091 || factory.ImplAttributes != MethodImplAttributes.IL
                || factory.HasThis || factory.Parameters.Count != 0 || !factory.HasBody
                || factory.Body.Instructions.Count(instruction => instruction.Operand is MethodReference target
                    && target.Name == "CreateInstanceForAnotherGenericParameter"
                    && target.DeclaringType.FullName == "System.RuntimeTypeHandle"
                    && target.Parameters.Count == arity + 1) != 1)
                throw new InvalidOperationException("Real ArraySortHelper factory contract changed");
        }

        GenericInstanceType Closed(string definition, params TypeReference[] arguments)
        {
            var result = new GenericInstanceType(module.ImportReference(CoreType(definition)));
            foreach (var argument in arguments) result.GenericArguments.Add(argument);
            return result;
        }

        MethodReference OnClosed(TypeDefinition owner, MethodDefinition definition, TypeReference[] arguments)
        {
            var result = module.ImportReference(definition);
            result.DeclaringType = Closed(owner.FullName, arguments);
            return result;
        }

        void Factory(string name, bool cache, bool address, params TypeReference[] arguments)
        {
            var owner = CoreType("System.Collections.Generic.ArraySortHelper`" + arguments.Length);
            var target = OnClosed(owner, owner.Methods.Single(method => method.Name == (cache ? "get_Default" : "CreateArraySortHelper")), arguments);
            var method = Method(name);
            method.Body = new MethodBody(method);
            var il = method.Body.GetILProcessor();
            if (address)
            {
                il.Emit(OpCodes.Ldftn, target);
                il.Emit(OpCodes.Calli, new CallSite(Closed("System.Collections.Generic.IArraySortHelper`" + arguments.Length, arguments)));
            }
            else il.Emit(OpCodes.Call, target);
            il.Emit(OpCodes.Ret);
        }

        void Sort(string name, bool comparer, params TypeReference[] arguments)
        {
            var method = Method(name);
            method.Body = new MethodBody(method);
            var il = method.Body.GetILProcessor();
            var contract = CoreType("System.Collections.Generic.IArraySortHelper`" + arguments.Length);
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Castclass, Closed(contract.FullName, arguments));
            var span = CoreType("System.Span`1");
            var constructor = span.Methods.Single(candidate => candidate.IsConstructor && !candidate.IsStatic
                && candidate.Parameters.Count == 1 && candidate.Parameters[0].ParameterType is ArrayType array
                && array.IsVector && array.ElementType is GenericParameter { Position: 0 });
            for (int index = 0; index < arguments.Length; index++)
            {
                il.Emit(OpCodes.Ldarg, method.Parameters[index + 1]);
                il.Emit(OpCodes.Newobj, OnClosed(span, constructor, new[] { arguments[index] }));
            }
            if (comparer) il.Emit(OpCodes.Ldarg, method.Parameters[arguments.Length + 1]);
            else il.Emit(OpCodes.Ldnull);
            il.Emit(OpCodes.Callvirt, OnClosed(contract, contract.Methods.Single(candidate => candidate.Name == "Sort"), arguments));
            il.Emit(OpCodes.Ret);
        }

        var integer = module.TypeSystem.Int32;
        var value = AppType("RankedValue");
        var reference = AppType("RankedReference");
        var derived = AppType("DerivedRank");
        var opaque = AppType("Opaque");
        Factory("FactoryInt", false, false, integer);
        Factory("FactoryValue", false, false, value);
        Factory("FactoryReference", false, false, reference);
        Factory("FactoryContravariant", false, false, derived);
        Factory("FactoryOpaque", false, false, opaque);
        Factory("FactoryPair", false, false, value, opaque);
        Factory("FactoryOpaquePair", false, false, opaque, integer);
        Factory("FactoryAddressInt", false, true, integer);
        Factory("CachedInt", true, false, integer);
        Sort("SortInt", false, integer);
        Sort("SortValue", false, value);
        Sort("SortReference", false, reference);
        Sort("SortContravariant", false, derived);
        Sort("SortOpaque", true, opaque);
        Sort("SortPair", false, value, opaque);
        Sort("SortOpaquePair", true, opaque, integer);
        foreach (string name in new[] { "RuntimeOneDirect", "RuntimeOneCalli", "RuntimeTwoDirect", "RuntimeTwoCalli" })
        {
            var method = Method(name);
            method.Body = new MethodBody(method);
            var il = method.Body.GetILProcessor();
            var target = module.ImportReference(wrappers[method.Parameters.Count - 2]);
            foreach (var parameter in method.Parameters)
            {
                il.Emit(OpCodes.Ldarg, parameter);
                il.Emit(OpCodes.Castclass, module.ImportReference(runtimeType));
            }
            if (name.EndsWith("Calli", StringComparison.Ordinal))
            {
                il.Emit(OpCodes.Ldftn, target);
                var signature = new CallSite(module.TypeSystem.Object);
                foreach (var parameter in target.Parameters) signature.Parameters.Add(new ParameterDefinition(parameter.ParameterType));
                il.Emit(OpCodes.Calli, signature);
            }
            else il.Emit(OpCodes.Call, target);
            il.Emit(OpCodes.Ret);
        }
        // The generated native input has no DynamicMethod oracle; its body calls the
        // same untouched private CoreLib methods the CLR oracle calls.
        foreach (string name in new[] { "HelperTypeOracle", "FactoryOracle", "SortOracle", "SortPairOracle" })
            program.Methods.Remove(Method(name));
        if (program.Methods.Where(method => method.HasBody).SelectMany(method => method.Body.Instructions)
            .Any(instruction => instruction.Operand is MethodReference target && target.DeclaringType.FullName == program.FullName
                && target.Name.EndsWith("Oracle", StringComparison.Ordinal)))
            throw new InvalidOperationException("Native fixture still references an oracle body");
        app.Write(output, new WriterParameters { Timestamp = 0, DeterministicMvid = true });
        Console.WriteLine("array-sort-factory-fixture:real-factories=7;factory-address=1;default-cache=1;sort-dispatches=7;runtime-wrapper-direct-calli=4;native-only");
        return true;
    }
}

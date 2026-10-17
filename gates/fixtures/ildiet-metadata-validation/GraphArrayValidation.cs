using Mono.Cecil;
using Mono.Cecil.Cil;

static partial class GraphArrayValidation
{
    internal static MethodDefinition[] Create(AssemblyDefinition library, AssemblyDefinition core)
    {
        var module = library.MainModule;
        TypeReference Core(string name) => module.ImportReference(core.MainModule.GetType("System." + name));
        var objectType = Core("Object");
        var voidType = Core("Void");
        var intType = Core("Int32");
        var stringType = Core("String");
        var systemType = Core("Type");
        var typeArray = new ArrayType(systemType);
        var objectArray = new ArrayType(objectType);
        var typeDefinition = core.MainModule.GetType("System.Type");
        var fromHandle = module.ImportReference(typeDefinition.Methods.Single(m => m.Name == "GetTypeFromHandle"));
        var lookup = module.ImportReference(typeDefinition.Methods.Single(m => m.Name == "GetMethod" && m.Parameters.Count == 2
            && m.Parameters[0].ParameterType.FullName == "System.String" && m.Parameters[1].ParameterType.FullName == "System.Type[]"));
        var namedLookup = module.ImportReference(typeDefinition.Methods.Single(m => m.Name == "GetMethod" && m.Parameters.Count == 1
            && m.Parameters[0].ParameterType.FullName == "System.String"));
        var getMethods = module.ImportReference(typeDefinition.Methods.Single(m => m.Name == "GetMethods" && m.Parameters.Count == 0));
        var invoke = module.ImportReference(core.MainModule.GetType("System.Reflection.MethodBase").Methods.Single(m => m.Name == "Invoke"
            && m.Parameters.Count == 2));
        var makeType = module.ImportReference(typeDefinition.Methods.Single(m => m.Name == "MakeGenericType"));
        var makeMethod = module.ImportReference(core.MainModule.GetType("System.Reflection.MethodInfo").Methods.Single(m => m.Name == "MakeGenericMethod"));
        var typeArguments = module.ImportReference(typeDefinition.Methods.Single(m => m.Name == "GetGenericArguments" && m.IsPublic && !m.IsStatic && m.Parameters.Count == 0));
        var activate = module.ImportReference(core.MainModule.GetType("System.Activator").Methods.Single(m => m.Name == "CreateInstance"
            && m.HasGenericParameters && m.Parameters.Count == 0));
        var objectCtor = module.ImportReference(core.MainModule.GetType("System.Object").Methods.Single(m => m.IsConstructor && !m.IsStatic && m.Parameters.Count == 0));
        TypeDefinition Type(string name)
        {
            var type = new TypeDefinition("Graph", name, TypeAttributes.Public | TypeAttributes.Class, objectType);
            module.Types.Add(type);
            return type;
        }
        MethodDefinition Method(TypeDefinition owner, string name, bool constructor = false)
        {
            var attributes = MethodAttributes.Public | (constructor
                ? MethodAttributes.SpecialName | MethodAttributes.RTSpecialName : MethodAttributes.Static);
            var method = new MethodDefinition(name, attributes, voidType);
            owner.Methods.Add(method);
            method.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
            return method;
        }
        void TypeValue(MethodDefinition method, TypeReference type)
        {
            method.Body.Instructions.Add(Instruction.Create(OpCodes.Ldtoken, type));
            method.Body.Instructions.Add(Instruction.Create(OpCodes.Call, fromHandle));
        }
        void Array(MethodDefinition method, TypeReference[] types, VariableDefinition destination, bool objects)
        {
            var il = method.Body.Instructions;
            il.Add(Instruction.Create(OpCodes.Ldc_I4, types.Length));
            il.Add(Instruction.Create(OpCodes.Newarr, objects ? objectType : systemType));
            for (int i = 0; i < types.Length; i++)
            {
                il.Add(Instruction.Create(OpCodes.Dup));
                il.Add(Instruction.Create(OpCodes.Ldc_I4, i));
                if (!objects) TypeValue(method, types[i]);
                else if (types[i].FullName == "System.Int32")
                {
                    il.Add(Instruction.Create(OpCodes.Ldc_I4, 42));
                    il.Add(Instruction.Create(OpCodes.Box, intType));
                }
                else il.Add(Instruction.Create(OpCodes.Ldstr, "argument"));
                il.Add(Instruction.Create(OpCodes.Stelem_Ref));
            }
            il.Add(Instruction.Create(OpCodes.Stloc, destination));
        }
        MethodDefinition Choices(TypeDefinition driver, string name, TypeReference[][] alternatives, bool objects)
        {
            var method = Method(driver, name);
            method.Parameters.Add(new ParameterDefinition(intType));
            method.Body.Instructions.Clear();
            method.Body.InitLocals = true;
            var types = new VariableDefinition(typeArray);
            method.Body.Variables.Add(types);
            VariableDefinition? arguments = objects ? new VariableDefinition(objectArray) : null;
            if (arguments is not null) method.Body.Variables.Add(arguments);
            var join = Instruction.Create(OpCodes.Nop);
            for (int choice = 0; choice < alternatives.Length; choice++)
            {
                var next = Instruction.Create(OpCodes.Nop);
                if (choice + 1 < alternatives.Length)
                {
                    method.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
                    method.Body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4, choice));
                    method.Body.Instructions.Add(Instruction.Create(OpCodes.Bne_Un, next));
                }
                Array(method, alternatives[choice], types, false);
                if (arguments is not null) Array(method, alternatives[choice], arguments, true);
                method.Body.Instructions.Add(Instruction.Create(OpCodes.Br, join));
                if (choice + 1 < alternatives.Length) method.Body.Instructions.Add(next);
            }
            method.Body.Instructions.Add(join);
            return method;
        }
        void Invoke(MethodDefinition method, bool arguments)
        {
            var il = method.Body.Instructions;
            il.Add(Instruction.Create(OpCodes.Ldnull));
            il.Add(arguments ? Instruction.Create(OpCodes.Ldloc, method.Body.Variables[1]) : Instruction.Create(OpCodes.Ldnull));
            il.Add(Instruction.Create(OpCodes.Callvirt, invoke));
            il.Add(Instruction.Create(OpCodes.Pop));
            il.Add(Instruction.Create(OpCodes.Ret));
        }
        var driver = Type("ArrayChoiceDriver");
        var target = Type("ArrayChoiceTarget");
        foreach (var parameters in new[] { new[] { intType }, new[] { stringType, stringType }, new[] { intType, intType }, new[] { stringType, intType } })
        {
            var method = Method(target, "Pick");
            foreach (var parameter in parameters) method.Parameters.Add(new ParameterDefinition(parameter));
        }
        var unused = Type("ArrayUnselectedDependency");
        var control = Method(target, "UnselectedControl");
        control.Body.Instructions.Insert(0, Instruction.Create(OpCodes.Call, Method(unused, "Touch")));
        var arities = Choices(driver, "LookupDifferentArities", new[] { new[] { intType }, new[] { stringType, stringType } }, true);
        var repeated = Choices(driver, "LookupRepeatedArguments", new[] { new[] { intType, intType }, new[] { stringType, intType } }, true);
        foreach (var method in new[] { arities, repeated })
        {
            TypeValue(method, target);
            method.Body.Instructions.Add(Instruction.Create(OpCodes.Ldstr, "Pick"));
            method.Body.Instructions.Add(Instruction.Create(OpCodes.Ldloc_0));
            method.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, lookup));
            Invoke(method, true);
        }
        // One array element itself carries alternatives, independently of array identity.
        var elements = Method(driver, "LookupElementAlternatives");
        elements.Parameters.Add(new ParameterDefinition(intType));
        elements.Body.Instructions.Clear();
        elements.Body.InitLocals = true;
        var elementType = new VariableDefinition(systemType);
        var elementObject = new VariableDefinition(objectType);
        elements.Body.Variables.Add(elementType);
        elements.Body.Variables.Add(elementObject);
        var stringBranch = Instruction.Create(OpCodes.Nop);
        var elementJoin = Instruction.Create(OpCodes.Nop);
        var elementIl = elements.Body.Instructions;
        elementIl.Add(Instruction.Create(OpCodes.Ldarg_0));
        elementIl.Add(Instruction.Create(OpCodes.Brtrue, stringBranch));
        TypeValue(elements, intType); elementIl.Add(Instruction.Create(OpCodes.Stloc_0));
        elementIl.Add(Instruction.Create(OpCodes.Ldc_I4, 42)); elementIl.Add(Instruction.Create(OpCodes.Box, intType));
        elementIl.Add(Instruction.Create(OpCodes.Stloc_1)); elementIl.Add(Instruction.Create(OpCodes.Br, elementJoin));
        elementIl.Add(stringBranch); TypeValue(elements, stringType); elementIl.Add(Instruction.Create(OpCodes.Stloc_0));
        elementIl.Add(Instruction.Create(OpCodes.Ldstr, "argument")); elementIl.Add(Instruction.Create(OpCodes.Stloc_1));
        elementIl.Add(elementJoin);
        TypeValue(elements, target); elementIl.Add(Instruction.Create(OpCodes.Ldstr, "Pick"));
        elementIl.Add(Instruction.Create(OpCodes.Ldc_I4_2)); elementIl.Add(Instruction.Create(OpCodes.Newarr, systemType));
        elementIl.Add(Instruction.Create(OpCodes.Dup)); elementIl.Add(Instruction.Create(OpCodes.Ldc_I4_0));
        elementIl.Add(Instruction.Create(OpCodes.Ldloc_0)); elementIl.Add(Instruction.Create(OpCodes.Stelem_Ref));
        elementIl.Add(Instruction.Create(OpCodes.Dup)); elementIl.Add(Instruction.Create(OpCodes.Ldc_I4_1));
        TypeValue(elements, intType); elementIl.Add(Instruction.Create(OpCodes.Stelem_Ref));
        elementIl.Add(Instruction.Create(OpCodes.Callvirt, lookup)); elementIl.Add(Instruction.Create(OpCodes.Ldnull));
        elementIl.Add(Instruction.Create(OpCodes.Ldc_I4_2)); elementIl.Add(Instruction.Create(OpCodes.Newarr, objectType));
        elementIl.Add(Instruction.Create(OpCodes.Dup)); elementIl.Add(Instruction.Create(OpCodes.Ldc_I4_0));
        elementIl.Add(Instruction.Create(OpCodes.Ldloc_1)); elementIl.Add(Instruction.Create(OpCodes.Stelem_Ref));
        elementIl.Add(Instruction.Create(OpCodes.Dup)); elementIl.Add(Instruction.Create(OpCodes.Ldc_I4_1));
        elementIl.Add(Instruction.Create(OpCodes.Ldc_I4, 42)); elementIl.Add(Instruction.Create(OpCodes.Box, intType));
        elementIl.Add(Instruction.Create(OpCodes.Stelem_Ref)); elementIl.Add(Instruction.Create(OpCodes.Callvirt, invoke));
        elementIl.Add(Instruction.Create(OpCodes.Pop)); elementIl.Add(Instruction.Create(OpCodes.Ret));

        MethodDefinition GenericChoices(string prefix, bool genericMethod, string? argumentSource = null)
        {
            TypeDefinition Payload(string suffix)
            {
                var type = Type(prefix + suffix);
                var ctor = Method(type, ".ctor", true);
                ctor.Body.Instructions.Insert(0, Instruction.Create(OpCodes.Ldarg_0));
                ctor.Body.Instructions.Insert(1, Instruction.Create(OpCodes.Call, objectCtor));
                Method(type, "SecondMember");
                return type;
            }
            var firstA = Payload("FirstA"); var secondA = Payload("SecondA");
            var firstB = Payload("FirstB"); var secondB = Payload("SecondB");
            var both = Payload("Repeated");
            var owner = Type(prefix + (genericMethod ? "MethodOwner" : "Pair`2"));
            var observe = Method(owner, "Observe");
            var first = new GenericParameter("TFirst", genericMethod ? (IGenericParameterProvider)observe : owner);
            var second = new GenericParameter("TSecond", genericMethod ? (IGenericParameterProvider)observe : owner);
            if (genericMethod) { observe.GenericParameters.Add(first); observe.GenericParameters.Add(second); }
            else { owner.GenericParameters.Add(first); owner.GenericParameters.Add(second); }
            observe.Body.Instructions.Clear();
            var create = new GenericInstanceMethod(activate); create.GenericArguments.Add(first);
            observe.Body.Instructions.Add(Instruction.Create(OpCodes.Call, create));
            observe.Body.Instructions.Add(Instruction.Create(OpCodes.Pop));
            TypeValue(observe, second);
            observe.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, getMethods));
            observe.Body.Instructions.Add(Instruction.Create(OpCodes.Pop));
            observe.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
            var choose = Choices(driver, prefix + "TupleAlternatives", new[]
                { new TypeReference[] { firstA, secondA }, new TypeReference[] { firstB, secondB }, new TypeReference[] { both, both } }, false);
            if (argumentSource == "type")
            {
                var source = Type(prefix + "ArgumentSource`2");
                source.GenericParameters.Add(new GenericParameter("TFirst", source));
                source.GenericParameters.Add(new GenericParameter("TSecond", source));
                TypeValue(choose, source);
                choose.Body.Instructions.Add(Instruction.Create(OpCodes.Ldloc_0));
                choose.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, makeType));
                choose.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, typeArguments));
                choose.Body.Instructions.Add(Instruction.Create(OpCodes.Stloc_0));
            }
            TypeValue(choose, owner);
            if (genericMethod)
            {
                choose.Body.Instructions.Add(Instruction.Create(OpCodes.Ldstr, "Observe"));
                choose.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, namedLookup));
                choose.Body.Instructions.Add(Instruction.Create(OpCodes.Ldloc_0));
                choose.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, makeMethod));
            }
            else
            {
                choose.Body.Instructions.Add(Instruction.Create(OpCodes.Ldloc_0));
                choose.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, makeType));
                choose.Body.Instructions.Add(Instruction.Create(OpCodes.Ldstr, "Observe"));
                choose.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, namedLookup));
            }
            Invoke(choose, false);
            return choose;
        }
        return new[] { arities, repeated, elements, GenericChoices("ArrayType", false), GenericChoices("ArrayMethod", true),
            GenericChoices("TypeArgumentsToType", false, "type"), GenericChoices("TypeArgumentsToMethod", true, "type") };
    }

    internal static void Check(ModuleDefinition module, string section = "all")
    {
        var prefixes = new[] { "ArrayType", "ArrayMethod", "TypeArgumentsToType", "TypeArgumentsToMethod" };
        Require(section == "all" || prefixes.Contains(section), "unknown Type[] vector section: " + section);
        if (section == "all")
        {
            var target = module.GetType("Graph.ArrayChoiceTarget");
            foreach (string parameters in new[] { "System.Int32", "System.String,System.String", "System.Int32,System.Int32", "System.String,System.Int32" })
            {
                var method = target?.Methods.SingleOrDefault(m => m.Name == "Pick"
                    && string.Join(",", m.Parameters.Select(p => p.ParameterType.FullName)) == parameters);
                Require(method is not null && !Stub(method), "Type[] alternatives lost selected overload body: " + parameters);
            }
            Require(target is not null && !target.Methods.Any(m => m.Name == "UnselectedControl")
                && module.GetType("Graph.ArrayUnselectedDependency") is null, "Type[] alternatives opened an unselected-name body");
        }
        foreach (string prefix in prefixes.Where(p => section == "all" || section == p))
        {
            foreach (string suffix in new[] { "FirstA", "FirstB", "SecondA", "SecondB", "Repeated" })
            {
                var type = module.GetType("Graph." + prefix + suffix);
                var ctor = type?.Methods.SingleOrDefault(m => m.Name == ".ctor");
                bool constructorDemand = suffix.StartsWith("First", StringComparison.Ordinal) || suffix == "Repeated";
                Require(ctor is { HasBody: true } && ctor.Body.Instructions.Count != 0 && Stub(ctor) != constructorDemand, "generic tuple bound constructor demand at wrong position: " + prefix + suffix);
                var member = type!.Methods.SingleOrDefault(m => m.Name == "SecondMember");
                Require(constructorDemand && suffix != "Repeated" ? member is null : member is not null && Stub(member),
                    "generic tuple bound member demand at wrong position: " + prefix + suffix);
            }
        }
        if (section != "all") { Console.WriteLine("graph-type-array-vector=" + section); return; }
        Console.WriteLine("graph-type-arrays=alternative-arities,repeated-types,element-alternatives,generic-type-tuples,generic-method-tuples,positional-demands,generic-argument-vectors");
    }

    static bool Stub(MethodDefinition method) => method.HasBody && method.Body.Instructions.Select(i => i.OpCode.Code)
        .SequenceEqual(new[] { Code.Ldc_I4, Code.Pop, Code.Ldnull, Code.Throw });

    static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}

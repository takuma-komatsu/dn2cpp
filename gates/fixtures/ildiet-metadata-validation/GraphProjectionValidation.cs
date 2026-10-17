using Mono.Cecil;
using Mono.Cecil.Cil;

static partial class GraphProjectionValidation
{
    internal static MethodReference[] Create(AssemblyDefinition app, AssemblyDefinition library, AssemblyDefinition core)
    {
        var module = library.MainModule;
        TypeReference Core(string name) => module.ImportReference(core.MainModule.GetType("System." + name));
        var objectType = Core("Object"); var voidType = Core("Void"); var stringType = Core("String");
        MethodReference Api(string owner, string name, params string[] parameters) => module.ImportReference(core.MainModule.GetType("System." + owner).Methods.Single(m => m.Name == name && m.IsPublic
            && m.Parameters.Select(p => p.ParameterType.FullName).SequenceEqual(parameters)));
        var fromHandle = Api("Type", "GetTypeFromHandle", "System.RuntimeTypeHandle");
        var activate = Api("Activator", "CreateInstance", "System.Type");
        var objectCtor = module.ImportReference(core.MainModule.GetType("System.Object").Methods.Single(m => m.IsConstructor && !m.IsStatic && m.Parameters.Count == 0));
        var toString = Api("Object", "ToString");
        TypeDefinition Type(string name, TypeReference? parent = null, TypeAttributes attributes = TypeAttributes.Public | TypeAttributes.Class)
        {
            var type = new TypeDefinition("Graph", name, attributes, parent ?? objectType);
            module.Types.Add(type);
            return type;
        }
        MethodDefinition Method(TypeDefinition owner, string name, TypeReference? result = null,
            MethodAttributes attributes = MethodAttributes.Public | MethodAttributes.Static)
        {
            var method = new MethodDefinition(name, attributes, result ?? voidType);
            owner.Methods.Add(method); method.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
            return method;
        }
        MethodDefinition Constructor(TypeDefinition owner, bool leaf = false)
        {
            var method = Method(owner, ".ctor", attributes: MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName);
            method.Body.Instructions.Insert(0, Instruction.Create(OpCodes.Ldarg_0));
            method.Body.Instructions.Insert(1, Instruction.Create(OpCodes.Call, objectCtor));
            if (leaf)
            {
                var dependency = Method(owner, "ConstructorLeaf", attributes: MethodAttributes.Private | MethodAttributes.Static);
                method.Body.Instructions.Insert(2, Instruction.Create(OpCodes.Call, dependency));
            }
            return method;
        }
        void TypeValue(MethodDefinition method, TypeReference type)
        {
            method.Body.Instructions.Add(Instruction.Create(OpCodes.Ldtoken, type));
            method.Body.Instructions.Add(Instruction.Create(OpCodes.Call, fromHandle));
        }
        void Construct(MethodDefinition method)
        {
            method.Body.Instructions.Add(Instruction.Create(OpCodes.Call, activate));
            method.Body.Instructions.Add(Instruction.Create(OpCodes.Pop));
            method.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
        }
        void Call(MethodDefinition caller, MethodReference target)
        {
            caller.Body.Instructions.Add(Instruction.Create(OpCodes.Call, target));
        }
        GenericInstanceMethod Bound(MethodDefinition method, TypeReference argument)
        {
            var bound = new GenericInstanceMethod(method); bound.GenericArguments.Add(argument); return bound;
        }
        var driver = Type("ProjectionDriver");
        var calls = new List<MethodReference>();
        calls.AddRange(GraphLookupValidation.Create(module, core.MainModule));
        calls.AddRange(GraphDelegateBindingValidation.Create(module, core.MainModule));
        calls.AddRange(GraphPropertyValidation.Create(module, core.MainModule));
        foreach (string selection in new[] { "Field", "Getter", "Method", "Accessor" })
        {
            var payload = Type("PlainOwner" + selection + "Payload", Core("ValueType"), TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.SequentialLayout);
            var text = Method(payload, "ToString", stringType, MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.HideBySig);
            text.Body.Instructions.Insert(0, Instruction.Create(OpCodes.Ldstr, selection));
            var owner = Type("Plain" + selection + "Owner");
            var field = new FieldDefinition(selection == "Field" ? "Value" : "Backing", FieldAttributes.Static | (selection == "Field" ? FieldAttributes.Public : FieldAttributes.Private), payload);
            owner.Fields.Add(field);
            if (selection != "Field")
            {
                var body = Method(owner, selection is "Getter" or "Accessor" ? "get_Value" : "Value", payload,
                    MethodAttributes.Public | MethodAttributes.Static | (selection is "Getter" or "Accessor" ? MethodAttributes.SpecialName : 0));
                body.Body.Instructions.Insert(0, Instruction.Create(OpCodes.Ldsfld, field));
                if (selection is "Getter" or "Accessor") owner.Properties.Add(new PropertyDefinition("Value", PropertyAttributes.None, payload) { GetMethod = body });
            }
            var read = Method(driver, "Plain" + selection);
            var parameter = new GenericParameter("T", read); read.GenericParameters.Add(parameter);
            read.Body.Instructions.Clear(); TypeValue(read, parameter);
            read.Body.Instructions.Add(Instruction.Create(OpCodes.Ldstr, "Value"));
            read.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, Api("Type", selection == "Field" ? "GetField" : selection is "Getter" or "Accessor" ? "GetProperty" : "GetMethod", "System.String")));
            if (selection == "Accessor") read.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, Api("Reflection.PropertyInfo", "GetGetMethod")));
            read.Body.Instructions.Add(Instruction.Create(OpCodes.Ldnull));
            if (selection is "Method" or "Accessor") read.Body.Instructions.Add(Instruction.Create(OpCodes.Ldnull));
            read.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, selection == "Field"
                ? Api("Reflection.FieldInfo", "GetValue", "System.Object") : selection == "Getter"
                ? Api("Reflection.PropertyInfo", "GetValue", "System.Object") : Api("Reflection.MethodBase", "Invoke", "System.Object", "System.Object[]")));
            read.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, toString));
            read.Body.Instructions.Add(Instruction.Create(OpCodes.Pop)); read.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
            calls.Add(Bound(read, owner));
        }

        var helpers = new List<(MethodDefinition Method, TypeReference First, TypeReference Late)>();
        foreach (string projection in new[] { "Base", "Argument", "Element" })
        {
            var first = Type("Projected" + projection + "First"); Constructor(first, true);
            var late = Type("Projected" + projection + "Late"); Constructor(late, true);
            TypeReference firstBinding = first, lateBinding = late;
            if (projection == "Base")
            {
                firstBinding = Type("ProjectionDerivedFirst", first);
                lateBinding = Type("ProjectionDerivedLate", late);
            }
            else if (projection == "Argument")
            {
                var source = Type("ProjectionSource`2");
                source.GenericParameters.Add(new GenericParameter("TFirst", source)); source.GenericParameters.Add(new GenericParameter("TIgnored", source));
                var firstIgnored = Type("ProjectionIgnoredFirst"); var lateIgnored = Type("ProjectionIgnoredLate");
                var ignoredDependency = Type("ProjectionIgnoredBodyDependency"); var ignoredLeaf = Method(ignoredDependency, "Touch");
                foreach (var ignored in new[] { firstIgnored, lateIgnored }) Constructor(ignored).Body.Instructions.Insert(2, Instruction.Create(OpCodes.Call, ignoredLeaf));
                var firstSource = new GenericInstanceType(source); firstSource.GenericArguments.Add(first); firstSource.GenericArguments.Add(firstIgnored);
                var lateSource = new GenericInstanceType(source); lateSource.GenericArguments.Add(late); lateSource.GenericArguments.Add(lateIgnored);
                firstBinding = firstSource; lateBinding = lateSource;
            }
            else { firstBinding = new ArrayType(first); lateBinding = new ArrayType(late, 2); }
            var helper = Method(driver, "Project" + projection);
            var parameter = new GenericParameter("T", helper); helper.GenericParameters.Add(parameter);
            helper.Body.Instructions.Clear(); TypeValue(helper, parameter);
            helper.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, Api("Type", projection == "Base" ? "get_BaseType" : projection == "Argument" ? "GetGenericArguments" : "GetElementType")));
            if (projection == "Argument")
            {
                helper.Body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4_0)); helper.Body.Instructions.Add(Instruction.Create(OpCodes.Ldelem_Ref));
            }
            Construct(helper);
            helpers.Add((helper, firstBinding, lateBinding));
        }
        var definitionTarget = Type("ProjectedDefinitionTarget`1");
        var definitionParameter = new GenericParameter("T", definitionTarget); definitionTarget.GenericParameters.Add(definitionParameter);
        var factory = Method(definitionTarget, "Create", objectType);
        var genericActivator = module.ImportReference(core.MainModule.GetType("System.Activator").Methods.Single(m => m.Name == "CreateInstance"
            && m.IsPublic && m.IsStatic && m.GenericParameters.Count == 1 && m.Parameters.Count == 0));
        var createValue = new GenericInstanceMethod(genericActivator); createValue.GenericArguments.Add(definitionParameter);
        factory.Body.Instructions.Clear(); factory.Body.Instructions.Add(Instruction.Create(OpCodes.Call, createValue));
        factory.Body.Instructions.Add(Instruction.Create(OpCodes.Box, definitionParameter)); factory.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
        var definitionFirst = Type("ProjectedDefinitionFirst"); Constructor(definitionFirst, true);
        var definitionLate = Type("ProjectedDefinitionLate"); Constructor(definitionLate, true);
        var seedFirst = new GenericInstanceType(definitionTarget); seedFirst.GenericArguments.Add(Type("ProjectedDefinitionSeedFirst"));
        var seedLate = new GenericInstanceType(definitionTarget); seedLate.GenericArguments.Add(Type("ProjectedDefinitionSeedLate"));
        var definitionHelper = Method(driver, "ProjectDefinition");
        var definitionOwner = new GenericParameter("TOwner", definitionHelper); definitionHelper.GenericParameters.Add(definitionOwner);
        var definitionArgument = new GenericParameter("TArgument", definitionHelper); definitionHelper.GenericParameters.Add(definitionArgument);
        definitionHelper.Body.Instructions.Clear(); TypeValue(definitionHelper, definitionOwner);
        definitionHelper.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, Api("Type", "GetGenericTypeDefinition")));
        definitionHelper.Body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4_1)); definitionHelper.Body.Instructions.Add(Instruction.Create(OpCodes.Newarr, Core("Type")));
        definitionHelper.Body.Instructions.Add(Instruction.Create(OpCodes.Dup)); definitionHelper.Body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4_0));
        TypeValue(definitionHelper, definitionArgument); definitionHelper.Body.Instructions.Add(Instruction.Create(OpCodes.Stelem_Ref));
        definitionHelper.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, Api("Type", "MakeGenericType", "System.Type[]")));
        definitionHelper.Body.Instructions.Add(Instruction.Create(OpCodes.Ldstr, "Create")); definitionHelper.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, Api("Type", "GetMethod", "System.String")));
        definitionHelper.Body.Instructions.Add(Instruction.Create(OpCodes.Ldnull)); definitionHelper.Body.Instructions.Add(Instruction.Create(OpCodes.Ldnull));
        definitionHelper.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, Api("Reflection.MethodBase", "Invoke", "System.Object", "System.Object[]")));
        definitionHelper.Body.Instructions.Add(Instruction.Create(OpCodes.Pop)); definitionHelper.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
        GenericInstanceMethod BindDefinition(TypeReference owner, TypeReference argument)
        {
            var bound = new GenericInstanceMethod(definitionHelper); bound.GenericArguments.Add(owner); bound.GenericArguments.Add(argument); return bound;
        }
        var methodFirst = Type("ProjectedMethodFirst"); Constructor(methodFirst, true);
        var methodLate = Type("ProjectedMethodLate"); Constructor(methodLate, true);
        TypeDefinition GenericFactory(string name)
        {
            var owner = Type(name);
            var method = Method(owner, "Create", objectType);
            var parameter = new GenericParameter("T", method); method.GenericParameters.Add(parameter);
            var create = new GenericInstanceMethod(genericActivator); create.GenericArguments.Add(parameter);
            method.Body.Instructions.Clear(); method.Body.Instructions.Add(Instruction.Create(OpCodes.Call, create));
            method.Body.Instructions.Add(Instruction.Create(OpCodes.Box, parameter)); method.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
            return owner;
        }
        var methodOwnerFirst = GenericFactory("ProjectedMethodOwnerFirst");
        var methodOwnerLate = GenericFactory("ProjectedMethodOwnerLate");
        var methodHelper = Method(driver, "ProjectGenericMethod");
        var methodOwnerParameter = new GenericParameter("TOwner", methodHelper); methodHelper.GenericParameters.Add(methodOwnerParameter);
        var methodArgumentParameter = new GenericParameter("TArgument", methodHelper); methodHelper.GenericParameters.Add(methodArgumentParameter);
        methodHelper.Body.Instructions.Clear(); TypeValue(methodHelper, methodOwnerParameter);
        methodHelper.Body.Instructions.Add(Instruction.Create(OpCodes.Ldstr, "Create")); methodHelper.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, Api("Type", "GetMethod", "System.String")));
        methodHelper.Body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4_1)); methodHelper.Body.Instructions.Add(Instruction.Create(OpCodes.Newarr, Core("Type")));
        methodHelper.Body.Instructions.Add(Instruction.Create(OpCodes.Dup)); methodHelper.Body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4_0));
        TypeValue(methodHelper, methodArgumentParameter); methodHelper.Body.Instructions.Add(Instruction.Create(OpCodes.Stelem_Ref));
        methodHelper.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, Api("Reflection.MethodInfo", "MakeGenericMethod", "System.Type[]")));
        methodHelper.Body.Instructions.Add(Instruction.Create(OpCodes.Ldnull)); methodHelper.Body.Instructions.Add(Instruction.Create(OpCodes.Ldnull));
        methodHelper.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, Api("Reflection.MethodBase", "Invoke", "System.Object", "System.Object[]")));
        methodHelper.Body.Instructions.Add(Instruction.Create(OpCodes.Pop)); methodHelper.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
        GenericInstanceMethod BindMethod(TypeReference owner, TypeReference argument)
        {
            var bound = new GenericInstanceMethod(methodHelper); bound.GenericArguments.Add(owner); bound.GenericArguments.Add(argument); return bound;
        }
        var parameterFirst = Type("ProjectedParameterFirst"); Constructor(parameterFirst, true);
        var parameterLate = Type("ProjectedParameterLate"); Constructor(parameterLate, true);
        TypeDefinition ParameterOwner(string name, TypeReference parameter)
        {
            var owner = Type(name); Method(owner, "Accept").Parameters.Add(new ParameterDefinition(parameter)); return owner;
        }
        var parameterOwnerFirst = ParameterOwner("ProjectedParameterOwnerFirst", parameterFirst);
        var parameterOwnerLate = ParameterOwner("ProjectedParameterOwnerLate", parameterLate);
        var parameterHelper = Method(driver, "ProjectParameter");
        var parameterOwner = new GenericParameter("T", parameterHelper); parameterHelper.GenericParameters.Add(parameterOwner);
        parameterHelper.Body.Instructions.Clear(); TypeValue(parameterHelper, parameterOwner);
        parameterHelper.Body.Instructions.Add(Instruction.Create(OpCodes.Ldstr, "Accept")); parameterHelper.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, Api("Type", "GetMethod", "System.String")));
        parameterHelper.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, Api("Reflection.MethodBase", "GetParameters")));
        parameterHelper.Body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4_0)); parameterHelper.Body.Instructions.Add(Instruction.Create(OpCodes.Ldelem_Ref));
        parameterHelper.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, Api("Reflection.ParameterInfo", "get_ParameterType"))); Construct(parameterHelper);
        var own = app.MainModule;
        var assemblyAnchor = new TypeDefinition("Graph", "AssemblyProjectionAnchor", TypeAttributes.Public | TypeAttributes.Class, own.ImportReference(objectType)); own.Types.Add(assemblyAnchor);
        var assemblyPayload = new TypeDefinition("Graph", "AssemblyProjectionPayload", TypeAttributes.Public | TypeAttributes.Class, own.ImportReference(objectType)); own.Types.Add(assemblyPayload);
        var assemblyCtor = new MethodDefinition(".ctor", MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName, own.ImportReference(voidType)); assemblyPayload.Methods.Add(assemblyCtor);
        var assemblyLeaf = new MethodDefinition("ConstructorLeaf", MethodAttributes.Private | MethodAttributes.Static, own.ImportReference(voidType)); assemblyPayload.Methods.Add(assemblyLeaf); assemblyLeaf.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
        assemblyCtor.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0)); assemblyCtor.Body.Instructions.Add(Instruction.Create(OpCodes.Call, own.ImportReference(objectCtor)));
        assemblyCtor.Body.Instructions.Add(Instruction.Create(OpCodes.Call, assemblyLeaf)); assemblyCtor.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
        var assemblyFactory = new TypeDefinition("Graph", "AssemblyProjectionFactory", TypeAttributes.Public | TypeAttributes.Class, own.ImportReference(objectType)); own.Types.Add(assemblyFactory);
        var assemblyCreate = new MethodDefinition("ConstructAssemblyProjectionPayload", MethodAttributes.Public | MethodAttributes.Static, own.ImportReference(objectType)); assemblyFactory.Methods.Add(assemblyCreate);
        assemblyCreate.Body.Instructions.Add(Instruction.Create(OpCodes.Newobj, assemblyCtor)); assemblyCreate.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
        var assemblyHelper = Method(driver, "ProjectAssembly"); assemblyHelper.Parameters.Add(new ParameterDefinition(Core("Int32")));
        var assemblyParameter = new GenericParameter("T", assemblyHelper); assemblyHelper.GenericParameters.Add(assemblyParameter);
        assemblyHelper.Body.Instructions.Clear(); TypeValue(assemblyHelper, assemblyParameter);
        assemblyHelper.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, Api("Type", "get_Assembly")));
        assemblyHelper.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, Api("Reflection.Assembly", "GetTypes")));
        assemblyHelper.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0)); assemblyHelper.Body.Instructions.Add(Instruction.Create(OpCodes.Ldelem_Ref));
        assemblyHelper.Body.Instructions.Add(Instruction.Create(OpCodes.Ldstr, "ConstructAssemblyProjectionPayload")); assemblyHelper.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, Api("Type", "GetMethod", "System.String")));
        assemblyHelper.Body.Instructions.Add(Instruction.Create(OpCodes.Ldnull)); assemblyHelper.Body.Instructions.Add(Instruction.Create(OpCodes.Ldnull));
        assemblyHelper.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, Api("Reflection.MethodBase", "Invoke", "System.Object", "System.Object[]")));
        assemblyHelper.Body.Instructions.Add(Instruction.Create(OpCodes.Pop)); assemblyHelper.Body.Instructions.Add(Instruction.Create(OpCodes.Ret)); calls.Add(Bound(assemblyHelper, module.ImportReference(assemblyAnchor)));
        TypeDefinition NestedOwner(string suffix)
        {
            var payload = Type("NestedProjectionPayload" + suffix); var ctor = Constructor(payload, true);
            var owner = Type("NestedProjectionOwner" + suffix);
            var nested = new TypeDefinition("", "Nested", TypeAttributes.NestedPublic | TypeAttributes.Class, objectType); owner.NestedTypes.Add(nested);
            var create = Method(nested, "ConstructNestedProjectionPayload", objectType);
            create.Body.Instructions.Insert(0, Instruction.Create(OpCodes.Newobj, ctor));
            return owner;
        }
        var nestedFirst = NestedOwner("First"); var nestedLate = NestedOwner("Late");
        var nestedHelper = Method(driver, "ProjectNested"); var nestedParameter = new GenericParameter("T", nestedHelper); nestedHelper.GenericParameters.Add(nestedParameter);
        nestedHelper.Body.Instructions.Clear(); TypeValue(nestedHelper, nestedParameter);
        nestedHelper.Body.Instructions.Add(Instruction.Create(OpCodes.Ldstr, "Nested")); nestedHelper.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, Api("Type", "GetNestedType", "System.String")));
        nestedHelper.Body.Instructions.Add(Instruction.Create(OpCodes.Ldstr, "ConstructNestedProjectionPayload")); nestedHelper.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, Api("Type", "GetMethod", "System.String")));
        nestedHelper.Body.Instructions.Add(Instruction.Create(OpCodes.Ldnull)); nestedHelper.Body.Instructions.Add(Instruction.Create(OpCodes.Ldnull));
        nestedHelper.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, Api("Reflection.MethodBase", "Invoke", "System.Object", "System.Object[]")));
        nestedHelper.Body.Instructions.Add(Instruction.Create(OpCodes.Pop)); nestedHelper.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
        var keyBox = Type("ArgumentKeyBox`1"); keyBox.GenericParameters.Add(new GenericParameter("T", keyBox));
        (MethodReference First, MethodReference Late) ArgumentKeyFamily(string suffix, bool array)
        {
            var firstPayload = Type("ArgumentKey" + suffix + "First"); Constructor(firstPayload, true);
            var latePayload = Type("ArgumentKey" + suffix + "Late"); Constructor(latePayload, true);
            var observe = Method(driver, "ObserveArgumentKey" + suffix);
            var ownerParameter = new GenericParameter("TOwner", observe); observe.GenericParameters.Add(ownerParameter);
            observe.Body.Instructions.Clear(); TypeValue(observe, ownerParameter);
            if (array) observe.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, Api("Type", "GetElementType")));
            observe.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, Api("Type", "GetGenericArguments")));
            observe.Body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4_0)); observe.Body.Instructions.Add(Instruction.Create(OpCodes.Ldelem_Ref)); Construct(observe);
            MethodDefinition Forward(string name)
            {
                var forward = Method(driver, name);
                var parameter = new GenericParameter("T", forward); forward.GenericParameters.Add(parameter);
                var box = new GenericInstanceType(keyBox); box.GenericArguments.Add(parameter);
                forward.Body.Instructions.Clear(); Call(forward, Bound(observe, array ? new ArrayType(box) : box)); forward.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
                return forward;
            }
            return (Bound(Forward("ForwardArgumentKey" + suffix + "A"), firstPayload), Bound(Forward("ForwardArgumentKey" + suffix + "B"), latePayload));
        }
        var keyDirect = ArgumentKeyFamily("Direct", false);
        var keyArray = ArgumentKeyFamily("Array", true);
        var invokeMember = Api("Type", "InvokeMember", "System.String", "System.Reflection.BindingFlags", "System.Reflection.Binder", "System.Object", "System.Object[]");
        void InvokeNamed(MethodDefinition method, string name)
        {
            method.Body.Instructions.Add(Instruction.Create(OpCodes.Ldstr, name));
        }
        void InvokeArguments(MethodDefinition method, bool print)
        {
            method.Body.Instructions.Add(Instruction.Create(OpCodes.Ldnull)); method.Body.Instructions.Add(Instruction.Create(OpCodes.Ldnull));
            method.Body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4_0)); method.Body.Instructions.Add(Instruction.Create(OpCodes.Newarr, objectType));
            method.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, invokeMember));
            if (print) method.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, toString));
            method.Body.Instructions.Add(Instruction.Create(OpCodes.Pop)); method.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
        }
        foreach (var (suffix, flags) in new[] { ("Explicit", 532), ("Defaults", 512) })
        {
            var payload = Type("InvokeConcrete" + suffix); Constructor(payload, true);
            var run = Method(driver, "InvokeConcrete" + suffix); run.Body.Instructions.Clear(); TypeValue(run, payload);
            InvokeNamed(run, "ignored-nonempty-name"); run.Body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4, flags)); InvokeArguments(run, false); calls.Add(run);
        }
        TypeDefinition InvokeFieldOwner(string suffix, bool constructor)
        {
            var payload = Type("Invoke" + suffix + "FieldPayload", Core("ValueType"), TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.SequentialLayout);
            var text = Method(payload, "ToString", stringType, MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.HideBySig);
            text.Body.Instructions.Insert(0, Instruction.Create(OpCodes.Ldstr, suffix));
            var owner = Type("Invoke" + suffix + "Owner"); owner.Fields.Add(new FieldDefinition("Value", FieldAttributes.Public | FieldAttributes.Static, payload));
            if (constructor) Constructor(owner, true);
            return owner;
        }
        var invokeFieldOwner = InvokeFieldOwner("Known", false);
        var invokeCtorOwner = Type("InvokeSymbolicConstructor"); Constructor(invokeCtorOwner, true);
        foreach (var (suffix, owner, flags) in new[] { ("Field", invokeFieldOwner, 1048), ("Constructor", invokeCtorOwner, 512) })
        {
            var run = Method(driver, "InvokeSymbolic" + suffix);
            var parameter = new GenericParameter("T", run); run.GenericParameters.Add(parameter);
            run.Body.Instructions.Clear(); TypeValue(run, parameter); InvokeNamed(run, "Value");
            run.Body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4, flags)); InvokeArguments(run, suffix == "Field"); calls.Add(Bound(run, owner));
        }
        var invokeAlternativeOwner = InvokeFieldOwner("Alternative", true);
        var invokeAlternative = Method(driver, "InvokeSymbolicAlternatives"); invokeAlternative.Parameters.Add(new ParameterDefinition(Core("Int32")));
        var alternativeParameter = new GenericParameter("T", invokeAlternative); invokeAlternative.GenericParameters.Add(alternativeParameter);
        invokeAlternative.Body.Instructions.Clear(); invokeAlternative.Body.InitLocals = true;
        invokeAlternative.Body.Variables.Add(new VariableDefinition(Core("Reflection.BindingFlags")));
        var fieldBranch = Instruction.Create(OpCodes.Nop); var flagsJoin = Instruction.Create(OpCodes.Nop);
        invokeAlternative.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0)); invokeAlternative.Body.Instructions.Add(Instruction.Create(OpCodes.Brtrue, fieldBranch));
        invokeAlternative.Body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4, 512)); invokeAlternative.Body.Instructions.Add(Instruction.Create(OpCodes.Stloc_0));
        invokeAlternative.Body.Instructions.Add(Instruction.Create(OpCodes.Br, flagsJoin)); invokeAlternative.Body.Instructions.Add(fieldBranch);
        invokeAlternative.Body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4, 1048)); invokeAlternative.Body.Instructions.Add(Instruction.Create(OpCodes.Stloc_0)); invokeAlternative.Body.Instructions.Add(flagsJoin);
        TypeValue(invokeAlternative, alternativeParameter); InvokeNamed(invokeAlternative, "Value"); invokeAlternative.Body.Instructions.Add(Instruction.Create(OpCodes.Ldloc_0));
        InvokeArguments(invokeAlternative, true); calls.Add(Bound(invokeAlternative, invokeAlternativeOwner));
        foreach (string suffix in new[] { "Known", "Symbolic", "Constructors", "Member" })
        {
            bool symbolic = suffix == "Symbolic";
            var payload = Type("Initializer" + suffix + "Payload");
            var leaf = Method(payload, "InitializerLeaf", attributes: MethodAttributes.Private | MethodAttributes.Static);
            var initializer = Method(payload, ".cctor", attributes: MethodAttributes.Private | MethodAttributes.Static | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName);
            initializer.Body.Instructions.Insert(0, Instruction.Create(OpCodes.Call, leaf));
            var holder = Type("Initializer" + suffix + "Holder");
            holder.Fields.Add(new FieldDefinition("Value", FieldAttributes.Public | FieldAttributes.Static, payload));
            var read = Method(driver, "ReadInitializer" + suffix); read.Body.Instructions.Clear();
            TypeReference owner = holder;
            if (symbolic)
            {
                var parameter = new GenericParameter("T", read); read.GenericParameters.Add(parameter); owner = parameter;
            }
            // The initializer owner enters only through the field signature.
            TypeValue(read, owner); read.Body.Instructions.Add(Instruction.Create(OpCodes.Ldstr, "Value"));
            read.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, Api("Type", "GetField", "System.String")));
            read.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, Api("Reflection.FieldInfo", "get_FieldType")));
            if (suffix is "Constructors" or "Member")
            {
                if (suffix == "Member") read.Body.Instructions.Add(Instruction.Create(OpCodes.Ldstr, ".cctor"));
                read.Body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4, 40));
                read.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, suffix == "Member"
                    ? Api("Type", "GetMember", "System.String", "System.Reflection.BindingFlags")
                    : Api("Type", "GetConstructors", "System.Reflection.BindingFlags")));
                read.Body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4_0)); read.Body.Instructions.Add(Instruction.Create(OpCodes.Ldelem_Ref));
                read.Body.Instructions.Add(Instruction.Create(OpCodes.Castclass, Core("Reflection.MethodBase")));
            }
            else read.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, Api("Type", "get_TypeInitializer")));
            read.Body.Instructions.Add(Instruction.Create(OpCodes.Ldnull)); read.Body.Instructions.Add(Instruction.Create(OpCodes.Ldnull));
            read.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, Api("Reflection.MethodBase", "Invoke", "System.Object", "System.Object[]")));
            read.Body.Instructions.Add(Instruction.Create(OpCodes.Pop)); read.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
            calls.Add(symbolic ? Bound(read, holder) : read);
        }
        foreach (bool symbolic in new[] { false, true })
        {
            string suffix = symbolic ? "Symbolic" : "Direct";
            var payload = Type("Nullable" + suffix + "Payload`1", Core("ValueType"), TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.SequentialLayout);
            payload.GenericParameters.Add(new GenericParameter("T", payload));
            var leaf = Method(payload, "DescribeLeaf", attributes: MethodAttributes.Private | MethodAttributes.Static);
            var describe = Method(payload, "Describe", stringType, MethodAttributes.Public | MethodAttributes.HideBySig);
            describe.Body.Instructions.Clear(); describe.Body.Instructions.Add(Instruction.Create(OpCodes.Call, leaf));
            describe.Body.Instructions.Add(Instruction.Create(OpCodes.Ldstr, suffix)); describe.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
            if (!symbolic)
            {
                var dependency = Type("NullableDirectUnselectedDependency"); var touch = Method(dependency, "Touch");
                var unselected = Method(payload, "Unselected", attributes: MethodAttributes.Public | MethodAttributes.HideBySig);
                unselected.Body.Instructions.Insert(0, Instruction.Create(OpCodes.Call, touch));
            }
            var closed = new GenericInstanceType(payload); closed.GenericArguments.Add(Core("Int32"));
            var run = Method(driver, "BoxNullable" + suffix); run.Body.Instructions.Clear(); run.Body.InitLocals = true;
            TypeReference value = closed;
            if (symbolic)
            {
                var parameter = new GenericParameter("T", run) { Attributes = GenericParameterAttributes.DefaultConstructorConstraint | GenericParameterAttributes.NotNullableValueTypeConstraint };
                parameter.Constraints.Add(new GenericParameterConstraint(Core("ValueType"))); run.GenericParameters.Add(parameter); value = parameter;
            }
            var nullable = new GenericInstanceType(Core("Nullable`1")); nullable.GenericArguments.Add(value);
            var nullableCtor = new MethodReference(".ctor", voidType, nullable) { HasThis = true };
            nullableCtor.Parameters.Add(new ParameterDefinition(nullable.ElementType.GenericParameters[0]));
            run.Body.Variables.Add(new VariableDefinition(value)); run.Body.Variables.Add(new VariableDefinition(objectType));
            run.Body.Instructions.Add(Instruction.Create(OpCodes.Ldloca, run.Body.Variables[0])); run.Body.Instructions.Add(Instruction.Create(OpCodes.Initobj, value));
            run.Body.Instructions.Add(Instruction.Create(OpCodes.Ldloc_0)); run.Body.Instructions.Add(Instruction.Create(OpCodes.Newobj, nullableCtor));
            run.Body.Instructions.Add(Instruction.Create(OpCodes.Box, nullable)); run.Body.Instructions.Add(Instruction.Create(OpCodes.Dup)); run.Body.Instructions.Add(Instruction.Create(OpCodes.Stloc_1));
            run.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, Api("Object", "GetType")));
            run.Body.Instructions.Add(Instruction.Create(OpCodes.Ldstr, "Describe")); run.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, Api("Type", "GetMethod", "System.String")));
            run.Body.Instructions.Add(Instruction.Create(OpCodes.Ldloc_1)); run.Body.Instructions.Add(Instruction.Create(OpCodes.Ldnull));
            run.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, Api("Reflection.MethodBase", "Invoke", "System.Object", "System.Object[]")));
            run.Body.Instructions.Add(Instruction.Create(OpCodes.Pop)); run.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
            calls.Add(symbolic ? Bound(run, closed) : run);
        }
        foreach (bool symbolic in new[] { false, true })
            foreach (bool named in new[] { false, true })
            {
                string suffix = (symbolic ? "Symbolic" : "Known") + (named ? "Named" : "Enumerated");
                var owner = Type("MemberNested" + suffix);
                var nested = new TypeDefinition("", "Inner", TypeAttributes.NestedPublic | TypeAttributes.Class, objectType); owner.NestedTypes.Add(nested);
                Constructor(nested, true);
                var run = Method(driver, "ReadMemberNested" + suffix); run.Body.Instructions.Clear();
                TypeReference target = owner;
                if (symbolic)
                {
                    var parameter = new GenericParameter("T", run); run.GenericParameters.Add(parameter); target = parameter;
                }
                TypeValue(run, target);
                if (named) run.Body.Instructions.Add(Instruction.Create(OpCodes.Ldstr, "Inner"));
                else run.Body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4, 18));
                run.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, named ? Api("Type", "GetMember", "System.String") : Api("Type", "GetMembers", "System.Reflection.BindingFlags")));
                run.Body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4_0)); run.Body.Instructions.Add(Instruction.Create(OpCodes.Ldelem_Ref));
                run.Body.Instructions.Add(Instruction.Create(OpCodes.Castclass, Core("Type"))); Construct(run);
                calls.Add(symbolic ? Bound(run, owner) : run);
            }
        foreach (var (suffix, flags) in new[] { ("Default", 0), ("Public", 16) })
        {
            var payload = Type("ActivatorFlags" + suffix); Constructor(payload, true);
            var run = Method(driver, "ActivateFlags" + suffix); run.Body.Instructions.Clear(); TypeValue(run, payload);
            run.Body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4, flags)); run.Body.Instructions.Add(Instruction.Create(OpCodes.Ldnull));
            run.Body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4_0)); run.Body.Instructions.Add(Instruction.Create(OpCodes.Newarr, objectType));
            run.Body.Instructions.Add(Instruction.Create(OpCodes.Ldnull));
            run.Body.Instructions.Add(Instruction.Create(OpCodes.Call, Api("Activator", "CreateInstance", "System.Type", "System.Reflection.BindingFlags", "System.Reflection.Binder", "System.Object[]", "System.Globalization.CultureInfo")));
            run.Body.Instructions.Add(Instruction.Create(OpCodes.Pop)); run.Body.Instructions.Add(Instruction.Create(OpCodes.Ret)); calls.Add(run);
        }
        void DefaultMember(TypeDefinition owner, string name)
        {
            var attribute = new CustomAttribute(Api("Reflection.DefaultMemberAttribute", ".ctor", "System.String"));
            attribute.ConstructorArguments.Add(new CustomAttributeArgument(stringType, name)); owner.CustomAttributes.Add(attribute);
        }
        MethodDefinition DefaultSelection(string suffix, TypeDefinition owner, string cast)
        {
            var run = Method(driver, "ReadDefault" + suffix); run.Body.Instructions.Clear(); TypeValue(run, owner);
            run.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, Api("Type", "GetDefaultMembers")));
            run.Body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4_0)); run.Body.Instructions.Add(Instruction.Create(OpCodes.Ldelem_Ref));
            run.Body.Instructions.Add(Instruction.Create(OpCodes.Castclass, Core(cast))); return run;
        }
        var defaultFieldPayload = Type("DefaultMemberFieldPayload", Core("ValueType"), TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.SequentialLayout);
        var defaultFieldLeaf = Method(defaultFieldPayload, "BoxLeaf", attributes: MethodAttributes.Private | MethodAttributes.Static);
        var defaultFieldText = Method(defaultFieldPayload, "ToString", stringType, MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.HideBySig);
        defaultFieldText.Body.Instructions.Insert(0, Instruction.Create(OpCodes.Call, defaultFieldLeaf)); defaultFieldText.Body.Instructions.Insert(1, Instruction.Create(OpCodes.Ldstr, "default-field"));
        var defaultFieldOwner = Type("DefaultMemberFieldOwner"); DefaultMember(defaultFieldOwner, "Value");
        defaultFieldOwner.Fields.Add(new FieldDefinition("Value", FieldAttributes.Public | FieldAttributes.Static, defaultFieldPayload));
        var defaultFieldRun = DefaultSelection("Field", defaultFieldOwner, "Reflection.FieldInfo");
        defaultFieldRun.Body.Instructions.Add(Instruction.Create(OpCodes.Ldnull)); defaultFieldRun.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, Api("Reflection.FieldInfo", "GetValue", "System.Object")));
        defaultFieldRun.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, toString)); defaultFieldRun.Body.Instructions.Add(Instruction.Create(OpCodes.Pop)); defaultFieldRun.Body.Instructions.Add(Instruction.Create(OpCodes.Ret)); calls.Add(defaultFieldRun);
        var defaultEventOwner = Type("DefaultMemberEventOwner"); DefaultMember(defaultEventOwner, "Tick");
        var defaultEvent = new EventDefinition("Tick", EventAttributes.None, Core("Action")); defaultEventOwner.Events.Add(defaultEvent);
        foreach (bool add in new[] { true, false })
        {
            var accessor = Method(defaultEventOwner, add ? "add_Tick" : "remove_Tick", attributes: MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.SpecialName | MethodAttributes.HideBySig);
            accessor.Parameters.Add(new ParameterDefinition(Core("Action")));
            var leaf = Method(defaultEventOwner, add ? "AddLeaf" : "RemoveLeaf", attributes: MethodAttributes.Private | MethodAttributes.Static);
            accessor.Body.Instructions.Insert(0, Instruction.Create(OpCodes.Call, leaf));
            if (add) defaultEvent.AddMethod = accessor; else defaultEvent.RemoveMethod = accessor;
        }
        var defaultEventRun = DefaultSelection("Event", defaultEventOwner, "Reflection.EventInfo");
        defaultEventRun.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, Api("Reflection.EventInfo", "GetAddMethod")));
        defaultEventRun.Body.Instructions.Add(Instruction.Create(OpCodes.Ldnull));
        defaultEventRun.Body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4_1)); defaultEventRun.Body.Instructions.Add(Instruction.Create(OpCodes.Newarr, objectType));
        defaultEventRun.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, Api("Reflection.MethodBase", "Invoke", "System.Object", "System.Object[]")));
        defaultEventRun.Body.Instructions.Add(Instruction.Create(OpCodes.Pop)); defaultEventRun.Body.Instructions.Add(Instruction.Create(OpCodes.Ret)); calls.Add(defaultEventRun);
        var defaultNestedOwner = Type("DefaultMemberNestedOwner"); DefaultMember(defaultNestedOwner, "Inner");
        var defaultNested = new TypeDefinition("", "Inner", TypeAttributes.NestedPublic | TypeAttributes.Class, objectType); defaultNestedOwner.NestedTypes.Add(defaultNested); Constructor(defaultNested, true);
        var defaultNestedRun = DefaultSelection("Nested", defaultNestedOwner, "Type"); Construct(defaultNestedRun); calls.Add(defaultNestedRun);
        var closureOwner = Type("NamedClosureOwner");
        var closure = new TypeDefinition("", "<>c__DisplayClass0_0", TypeAttributes.NestedPrivate | TypeAttributes.Class | TypeAttributes.Sealed, objectType); closureOwner.NestedTypes.Add(closure);
        closure.CustomAttributes.Add(new CustomAttribute(Api("Runtime.CompilerServices.CompilerGeneratedAttribute", ".ctor")));
        var closureCtor = Constructor(closure);
        var closureLeaf = Method(closure, "HiddenLeaf", attributes: MethodAttributes.Private | MethodAttributes.Static);
        var hidden = Method(closure, "Hidden", attributes: MethodAttributes.Private | MethodAttributes.HideBySig); hidden.Body.Instructions.Insert(0, Instruction.Create(OpCodes.Call, closureLeaf));
        var closureRun = Method(closureOwner, "Bind"); closureRun.Body.Instructions.Clear(); TypeValue(closureRun, Core("Action"));
        closureRun.Body.Instructions.Add(Instruction.Create(OpCodes.Newobj, closureCtor)); closureRun.Body.Instructions.Add(Instruction.Create(OpCodes.Ldstr, "Hidden"));
        closureRun.Body.Instructions.Add(Instruction.Create(OpCodes.Call, Api("Delegate", "CreateDelegate", "System.Type", "System.Object", "System.String")));
        closureRun.Body.Instructions.Add(Instruction.Create(OpCodes.Pop)); closureRun.Body.Instructions.Add(Instruction.Create(OpCodes.Ret)); calls.Add(closureRun);
        var actionInt = new GenericInstanceType(Core("Action`1")); actionInt.GenericArguments.Add(Core("Int32"));
        void NamedConstructorBinding(MethodDefinition method, bool isStatic)
        {
            method.Body.Instructions.Add(Instruction.Create(OpCodes.Ldstr, isStatic ? ".cctor" : ".ctor"));
            method.Body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4_0)); method.Body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4_1));
            method.Body.Instructions.Add(Instruction.Create(OpCodes.Call, Api("Delegate", "CreateDelegate", "System.Type", isStatic ? "System.Type" : "System.Object", "System.String", "System.Boolean", "System.Boolean")));
            method.Body.Instructions.Add(Instruction.Create(OpCodes.Pop)); method.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
        }
        void SelectedConstructor(TypeDefinition owner, bool valueType)
        {
            var leaf = Method(owner, "SelectedConstructorLeaf", attributes: MethodAttributes.Private | MethodAttributes.Static);
            var ctor = Method(owner, ".ctor", attributes: MethodAttributes.Private | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName);
            ctor.Parameters.Add(new ParameterDefinition(Core("Int32"))); ctor.Body.Instructions.Clear();
            if (!valueType)
            {
                ctor.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0)); ctor.Body.Instructions.Add(Instruction.Create(OpCodes.Call, objectCtor));
            }
            ctor.Body.Instructions.Add(Instruction.Create(OpCodes.Call, leaf)); ctor.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
        }
        var namedOwn = Type("NamedConstructorOwn"); var namedOwnDefault = Constructor(namedOwn); SelectedConstructor(namedOwn, false);
        var namedControl = Type("NamedConstructorUnselectedDependency");
        Method(namedOwn, "Unselected").Body.Instructions.Insert(0, Instruction.Create(OpCodes.Call, Method(namedControl, "Touch")));
        var namedOwnRun = Method(driver, "BindOwnConstructor"); namedOwnRun.Body.Instructions.Clear(); TypeValue(namedOwnRun, actionInt);
        namedOwnRun.Body.Instructions.Add(Instruction.Create(OpCodes.Newobj, namedOwnDefault)); NamedConstructorBinding(namedOwnRun, false); calls.Add(namedOwnRun);
        var namedBase = Type("NamedConstructorBase"); var namedBaseDefault = Constructor(namedBase);
        namedBaseDefault.Attributes = MethodAttributes.Family | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName;
        SelectedConstructor(namedBase, false);
        var namedDerived = Type("NamedConstructorDerived", namedBase); var namedDerivedDefault = Constructor(namedDerived);
        namedDerivedDefault.Body.Instructions[1].Operand = namedBaseDefault;
        var namedBaseRun = Method(driver, "BindBaseConstructor"); namedBaseRun.Body.Instructions.Clear(); TypeValue(namedBaseRun, actionInt);
        namedBaseRun.Body.Instructions.Add(Instruction.Create(OpCodes.Newobj, namedDerivedDefault)); NamedConstructorBinding(namedBaseRun, false); calls.Add(namedBaseRun);
        TypeDefinition NamedInitializerHolder(string suffix)
        {
            var payload = Type("NamedInitializer" + suffix + "Payload");
            var leaf = Method(payload, "SelectedInitializerLeaf", attributes: MethodAttributes.Private | MethodAttributes.Static);
            Method(payload, ".cctor", attributes: MethodAttributes.Private | MethodAttributes.Static | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName)
                .Body.Instructions.Insert(0, Instruction.Create(OpCodes.Call, leaf));
            var holder = Type("NamedInitializer" + suffix + "Holder"); holder.Fields.Add(new FieldDefinition("Value", FieldAttributes.Public | FieldAttributes.Static, payload)); return holder;
        }
        void InitializerBinding(MethodDefinition method, TypeReference holder)
        {
            TypeValue(method, Core("Action")); TypeValue(method, holder); method.Body.Instructions.Add(Instruction.Create(OpCodes.Ldstr, "Value"));
            method.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, Api("Type", "GetField", "System.String")));
            method.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, Api("Reflection.FieldInfo", "get_FieldType"))); NamedConstructorBinding(method, true);
        }
        var namedInitializerRun = Method(driver, "BindNamedInitializer"); namedInitializerRun.Body.Instructions.Clear();
        InitializerBinding(namedInitializerRun, NamedInitializerHolder("Known")); calls.Add(namedInitializerRun);
        var namedSymbolicFirst = Type("NamedConstructorSymbolicFirst", Core("ValueType"), TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.SequentialLayout);
        var namedSymbolicLate = Type("NamedConstructorSymbolicLate", Core("ValueType"), TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.SequentialLayout);
        foreach (var payload in new[] { namedSymbolicFirst, namedSymbolicLate }) SelectedConstructor(payload, true);
        var namedSymbolicRun = Method(driver, "BindSymbolicConstructor"); namedSymbolicRun.Body.Instructions.Clear(); namedSymbolicRun.Body.InitLocals = true;
        var namedSymbolicParameter = new GenericParameter("T", namedSymbolicRun) { Attributes = GenericParameterAttributes.DefaultConstructorConstraint | GenericParameterAttributes.NotNullableValueTypeConstraint };
        namedSymbolicParameter.Constraints.Add(new GenericParameterConstraint(Core("ValueType"))); namedSymbolicRun.GenericParameters.Add(namedSymbolicParameter);
        namedSymbolicRun.Body.Variables.Add(new VariableDefinition(namedSymbolicParameter)); TypeValue(namedSymbolicRun, actionInt);
        namedSymbolicRun.Body.Instructions.Add(Instruction.Create(OpCodes.Ldloca, namedSymbolicRun.Body.Variables[0])); namedSymbolicRun.Body.Instructions.Add(Instruction.Create(OpCodes.Initobj, namedSymbolicParameter));
        namedSymbolicRun.Body.Instructions.Add(Instruction.Create(OpCodes.Ldloc_0)); namedSymbolicRun.Body.Instructions.Add(Instruction.Create(OpCodes.Box, namedSymbolicParameter)); NamedConstructorBinding(namedSymbolicRun, false);
        var namedInitializerFirst = NamedInitializerHolder("First"); var namedInitializerLate = NamedInitializerHolder("Late");
        var namedInitializerHelper = Method(driver, "BindSymbolicInitializer"); namedInitializerHelper.Body.Instructions.Clear();
        var namedHolderParameter = new GenericParameter("THolder", namedInitializerHelper); namedInitializerHelper.GenericParameters.Add(namedHolderParameter); InitializerBinding(namedInitializerHelper, namedHolderParameter);
        void InvokeTypeName(MethodDefinition method, string typeName, string name)
        {
            var destination = method.Module;
            method.Body.Instructions.Add(Instruction.Create(OpCodes.Ldstr, typeName)); method.Body.Instructions.Add(Instruction.Create(OpCodes.Call, destination.ImportReference(Api("Type", "GetType", "System.String"))));
            method.Body.Instructions.Add(Instruction.Create(OpCodes.Ldstr, name)); method.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, destination.ImportReference(Api("Type", "GetMethod", "System.String"))));
            method.Body.Instructions.Add(Instruction.Create(OpCodes.Ldnull)); method.Body.Instructions.Add(Instruction.Create(OpCodes.Ldnull));
            method.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, destination.ImportReference(Api("Reflection.MethodBase", "Invoke", "System.Object", "System.Object[]"))));
            method.Body.Instructions.Add(Instruction.Create(OpCodes.Pop)); method.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
        }
        void Forward(TypeDefinition target)
        {
            var scope = own.AssemblyReferences.FirstOrDefault(a => a.Name == library.Name.Name);
            if (scope is null) { scope = new AssemblyNameReference(library.Name.Name, library.Name.Version); own.AssemblyReferences.Add(scope); }
            own.ExportedTypes.Add(new ExportedType(target.Namespace, target.Name, own, scope) { Attributes = TypeAttributes.Public, IsForwarder = true });
        }
        var forwarded = Type("ForwardedNamePayload"); var forwardedLeaf = Method(forwarded, "RunLeaf", attributes: MethodAttributes.Private | MethodAttributes.Static);
        Method(forwarded, "Run").Body.Instructions.Insert(0, Instruction.Create(OpCodes.Call, forwardedLeaf)); Forward(forwarded);
        var forwardedRun = Method(driver, "InvokeForwardedName"); forwardedRun.Body.Instructions.Clear();
        InvokeTypeName(forwardedRun, "Graph.ForwardedNamePayload, GraphApp", "Run"); calls.Add(forwardedRun);
        var forwardedArgument = Type("ForwardedNameArgument"); Constructor(forwardedArgument, true); Forward(forwardedArgument);
        var forwardedFactory = Type("ForwardedNameFactory`1");
        var forwardedParameter = new GenericParameter("T", forwardedFactory) { Attributes = GenericParameterAttributes.DefaultConstructorConstraint }; forwardedFactory.GenericParameters.Add(forwardedParameter);
        var forwardedCreate = Method(forwardedFactory, "Create", objectType); forwardedCreate.Body.Instructions.Clear();
        var forwardedActivation = new GenericInstanceMethod(genericActivator); forwardedActivation.GenericArguments.Add(forwardedParameter);
        forwardedCreate.Body.Instructions.Add(Instruction.Create(OpCodes.Call, forwardedActivation)); forwardedCreate.Body.Instructions.Add(Instruction.Create(OpCodes.Box, forwardedParameter)); forwardedCreate.Body.Instructions.Add(Instruction.Create(OpCodes.Ret)); Forward(forwardedFactory);
        var forwardedGenericRun = Method(driver, "InvokeForwardedGenericName"); forwardedGenericRun.Body.Instructions.Clear();
        InvokeTypeName(forwardedGenericRun, "Graph.ForwardedNameFactory`1[[Graph.ForwardedNameArgument, GraphApp]], GraphApp", "Create"); calls.Add(forwardedGenericRun);
        var caseDependency = Type("TypeNameUnselectedDependency"); var caseTouch = Method(caseDependency, "Touch");
        foreach (bool upper in new[] { true, false })
        {
            string selectedName = upper ? "CaseUpper" : "caselower", ignoredName = upper ? "caseupper" : "CaseLower";
            foreach (string name in new[] { ignoredName, selectedName })
            {
                var payload = Type(name); var leaf = Method(payload, "RunLeaf", attributes: MethodAttributes.Private | MethodAttributes.Static);
                Method(payload, "Run").Body.Instructions.Insert(0, Instruction.Create(OpCodes.Call, leaf));
                Method(payload, "Unselected").Body.Instructions.Insert(0, Instruction.Create(OpCodes.Call, caseTouch));
            }
            var run = Method(driver, upper ? "InvokeUpperCaseName" : "InvokeLowerCaseName"); run.Body.Instructions.Clear();
            InvokeTypeName(run, "Graph." + selectedName + ", GraphLib", "Run"); calls.Add(run);
        }
        var duplicateLibPayload = Type("DuplicateNamePayload"); Constructor(duplicateLibPayload, true);
        TypeDefinition ApplicationType(string name)
        {
            var type = new TypeDefinition("Graph", name, TypeAttributes.Public | TypeAttributes.Class, own.ImportReference(objectType)); own.Types.Add(type); return type;
        }
        var duplicateAppPayload = ApplicationType("DuplicateNamePayload");
        var duplicateAppLeaf = new MethodDefinition("ConstructorLeaf", MethodAttributes.Private | MethodAttributes.Static, own.ImportReference(voidType)); duplicateAppPayload.Methods.Add(duplicateAppLeaf); duplicateAppLeaf.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
        var duplicateAppCtor = new MethodDefinition(".ctor", MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName, own.ImportReference(voidType)); duplicateAppPayload.Methods.Add(duplicateAppCtor);
        duplicateAppCtor.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0)); duplicateAppCtor.Body.Instructions.Add(Instruction.Create(OpCodes.Call, own.ImportReference(objectCtor)));
        duplicateAppCtor.Body.Instructions.Add(Instruction.Create(OpCodes.Call, duplicateAppLeaf)); duplicateAppCtor.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
        var duplicateFactory = ApplicationType("DuplicateNameFactory`1");
        var duplicateParameter = new GenericParameter("T", duplicateFactory) { Attributes = GenericParameterAttributes.DefaultConstructorConstraint }; duplicateFactory.GenericParameters.Add(duplicateParameter);
        var duplicateCreate = new MethodDefinition("Create", MethodAttributes.Public | MethodAttributes.Static, own.ImportReference(objectType)); duplicateFactory.Methods.Add(duplicateCreate);
        var duplicateActivation = new GenericInstanceMethod(own.ImportReference(genericActivator)); duplicateActivation.GenericArguments.Add(duplicateParameter);
        duplicateCreate.Body.Instructions.Add(Instruction.Create(OpCodes.Call, duplicateActivation)); duplicateCreate.Body.Instructions.Add(Instruction.Create(OpCodes.Box, duplicateParameter)); duplicateCreate.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
        var duplicateDriver = ApplicationType("DuplicateNameDriver"); var duplicateRun = new MethodDefinition("Run", MethodAttributes.Public | MethodAttributes.Static, own.ImportReference(voidType)); duplicateDriver.Methods.Add(duplicateRun);
        duplicateRun.Body.Instructions.Add(Instruction.Create(OpCodes.Ldtoken, duplicateFactory)); duplicateRun.Body.Instructions.Add(Instruction.Create(OpCodes.Call, own.ImportReference(fromHandle))); duplicateRun.Body.Instructions.Add(Instruction.Create(OpCodes.Pop));
        InvokeTypeName(duplicateRun, "Graph.DuplicateNameFactory`1[Graph.DuplicateNamePayload]", "Create"); calls.Add(duplicateRun);
        var attributeCtor = module.ImportReference(core.MainModule.GetType("System.Attribute").Methods.Single(m => m.IsConstructor && !m.IsStatic && m.Parameters.Count == 0));
        var parameterSource = Method(driver, "AttributeParameterSource"); parameterSource.Parameters.Add(new ParameterDefinition(Core("Int32")));
        foreach (bool derived in new[] { false, true })
        {
            string prefix = derived ? "UnknownParameterAttribute" : "UnknownMemberAttribute";
            var markerBase = Type(prefix + (derived ? "BaseMarker" : "Marker"), Core("Attribute"));
            var setterDependency = Type(prefix + "SetterDependency"); var setterTouch = Method(setterDependency, "Touch");
            var getter = Method(markerBase, "get_Label", stringType, MethodAttributes.Public | MethodAttributes.SpecialName);
            getter.Body.Instructions.Insert(0, Instruction.Create(OpCodes.Ldstr, "label"));
            var setter = Method(markerBase, "set_Label", attributes: MethodAttributes.Public | MethodAttributes.SpecialName);
            setter.Parameters.Add(new ParameterDefinition(stringType)); setter.Body.Instructions.Insert(0, Instruction.Create(OpCodes.Call, setterTouch));
            markerBase.Properties.Add(new PropertyDefinition("Label", PropertyAttributes.None, stringType) { GetMethod = getter, SetMethod = setter });
            markerBase.Fields.Add(new FieldDefinition("TargetType", FieldAttributes.Public, Core("Type")));
            var marker = markerBase; MethodReference parentCtor = attributeCtor;
            if (derived)
            {
                var baseCtor = Method(markerBase, ".ctor", attributes: MethodAttributes.Family | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName);
                baseCtor.Body.Instructions.Insert(0, Instruction.Create(OpCodes.Ldarg_0)); baseCtor.Body.Instructions.Insert(1, Instruction.Create(OpCodes.Call, attributeCtor)); parentCtor = baseCtor;
                marker = Type(prefix + "DerivedMarker", markerBase);
            }
            var ctorDependency = Type(prefix + "ConstructorDependency"); var ctorTouch = Method(ctorDependency, "Touch");
            var ctor = Method(marker, ".ctor", attributes: MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName);
            ctor.Parameters.Add(new ParameterDefinition(Core("Int32"))); ctor.Body.Instructions.Clear();
            ctor.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0)); ctor.Body.Instructions.Add(Instruction.Create(OpCodes.Call, parentCtor));
            ctor.Body.Instructions.Add(Instruction.Create(OpCodes.Call, ctorTouch)); ctor.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
            var argumentPayload = Type(prefix + "ArgumentPayload");
            var argumentDependency = Type(prefix + "ArgumentBodyDependency");
            Method(argumentPayload, "Unselected").Body.Instructions.Insert(0, Instruction.Create(OpCodes.Call, Method(argumentDependency, "Touch")));
            var host = Type(prefix + "Host"); var touch = Method(host, "Touch");
            var annotated = Method(host, "Annotated");
            var annotatedDependency = Type(prefix + "AnnotatedBodyDependency");
            annotated.Body.Instructions.Insert(0, Instruction.Create(OpCodes.Call, Method(annotatedDependency, "Touch")));
            var attribute = new CustomAttribute(ctor); attribute.ConstructorArguments.Add(new CustomAttributeArgument(Core("Int32"), 1));
            attribute.Properties.Add(new CustomAttributeNamedArgument("Label", new CustomAttributeArgument(stringType, "selected")));
            attribute.Fields.Add(new CustomAttributeNamedArgument("TargetType", new CustomAttributeArgument(Core("Type"), argumentPayload)));
            if (derived)
            {
                var parameter = new ParameterDefinition(Core("Int32")); annotated.Parameters.Add(parameter); parameter.CustomAttributes.Add(attribute);
            }
            else annotated.CustomAttributes.Add(attribute);
            var read = Method(driver, "Read" + prefix); read.Body.Instructions.Clear();
            read.Parameters.Add(new ParameterDefinition(Core(derived ? "Reflection.ParameterInfo" : "Reflection.MemberInfo")));
            read.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0)); TypeValue(read, markerBase); read.Body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4_0));
            read.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, Api(derived ? "Reflection.ParameterInfo" : "Reflection.MemberInfo", "GetCustomAttributes", "System.Type", "System.Boolean")));
            read.Body.Instructions.Add(Instruction.Create(OpCodes.Pop)); read.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
            var run = Method(driver, "Run" + prefix); run.Body.Instructions.Clear(); Call(run, touch); TypeValue(run, driver);
            if (derived)
            {
                run.Body.Instructions.Add(Instruction.Create(OpCodes.Ldstr, "AttributeParameterSource")); run.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, Api("Type", "GetMethod", "System.String")));
                run.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, Api("Reflection.MethodBase", "GetParameters")));
                run.Body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4_0)); run.Body.Instructions.Add(Instruction.Create(OpCodes.Ldelem_Ref));
            }
            Call(run, read); run.Body.Instructions.Add(Instruction.Create(OpCodes.Ret)); calls.Add(run);
        }
        var preserveAttribute = Type("PreserveAttribute", Core("Attribute"));
        var preserveCtor = Method(preserveAttribute, ".ctor", attributes: MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName);
        preserveCtor.Body.Instructions.Insert(0, Instruction.Create(OpCodes.Ldarg_0)); preserveCtor.Body.Instructions.Insert(1, Instruction.Create(OpCodes.Call, attributeCtor));
        var boxingPayloads = new List<TypeDefinition>();
        foreach (string selection in new[] { "PreserveField", "PreserveReturn", "PreserveRef", "NamedReturn", "MethodRef" })
        {
            string prefix = "Boxing" + selection;
            bool byReference = selection is "PreserveRef" or "MethodRef";
            var payload = Type(prefix + "Payload", Core("ValueType"), TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.SequentialLayout);
            boxingPayloads.Add(payload);
            var dependency = Type(prefix + "BodyDependency"); var dependencyTouch = Method(dependency, "Touch");
            var leaf = Method(payload, "ToStringLeaf", attributes: MethodAttributes.Private | MethodAttributes.Static);
            var text = Method(payload, "ToString", stringType, MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.HideBySig);
            text.Body.Instructions.Clear(); text.Body.Instructions.Add(Instruction.Create(OpCodes.Call, dependencyTouch)); text.Body.Instructions.Add(Instruction.Create(OpCodes.Call, leaf));
            text.Body.Instructions.Add(Instruction.Create(OpCodes.Ldstr, selection)); text.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
            var unselectedDependency = Type(prefix + "UnselectedDependency");
            Method(payload, "Unselected").Body.Instructions.Insert(0, Instruction.Create(OpCodes.Call, Method(unselectedDependency, "Touch")));
            var owner = Type(prefix + "Owner"); FieldDefinition? value = null;
            if (!byReference)
            {
                value = new FieldDefinition("Value", FieldAttributes.Public | FieldAttributes.Static, payload); owner.Fields.Add(value);
            }
            if (selection == "PreserveField")
            {
                value!.CustomAttributes.Add(new CustomAttribute(preserveCtor)); continue;
            }
            var read = Method(owner, "Read", byReference ? voidType : payload);
            if (byReference) read.Parameters.Add(new ParameterDefinition(new ByReferenceType(payload)));
            else read.Body.Instructions.Insert(0, Instruction.Create(OpCodes.Ldsfld, value!));
            if (selection.StartsWith("Preserve", StringComparison.Ordinal))
            {
                read.CustomAttributes.Add(new CustomAttribute(preserveCtor)); continue;
            }
            var delegateType = Type(prefix + "Delegate", Core("MulticastDelegate"), TypeAttributes.Public | TypeAttributes.Class | TypeAttributes.Sealed);
            var delegateCtor = new MethodDefinition(".ctor", MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName, voidType)
                { ImplAttributes = MethodImplAttributes.Runtime | MethodImplAttributes.Managed };
            delegateCtor.Parameters.Add(new ParameterDefinition(objectType)); delegateCtor.Parameters.Add(new ParameterDefinition(Core("IntPtr"))); delegateType.Methods.Add(delegateCtor);
            var invoke = new MethodDefinition("Invoke", MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Virtual | MethodAttributes.NewSlot, byReference ? voidType : payload)
                { ImplAttributes = MethodImplAttributes.Runtime | MethodImplAttributes.Managed };
            if (byReference) invoke.Parameters.Add(new ParameterDefinition(new ByReferenceType(payload))); delegateType.Methods.Add(invoke);
            var run = Method(driver, "Bind" + prefix); run.Body.Instructions.Clear();
            if (selection == "NamedReturn")
            {
                TypeValue(run, delegateType); TypeValue(run, owner); run.Body.Instructions.Add(Instruction.Create(OpCodes.Ldstr, "Read"));
                run.Body.Instructions.Add(Instruction.Create(OpCodes.Call, Api("Delegate", "CreateDelegate", "System.Type", "System.Type", "System.String")));
            }
            else
            {
                TypeValue(run, owner); run.Body.Instructions.Add(Instruction.Create(OpCodes.Ldstr, "Read"));
                run.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, Api("Type", "GetMethod", "System.String"))); TypeValue(run, delegateType);
                run.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, Api("Reflection.MethodInfo", "CreateDelegate", "System.Type")));
            }
            run.Body.Instructions.Add(Instruction.Create(OpCodes.Pop)); run.Body.Instructions.Add(Instruction.Create(OpCodes.Ret)); calls.Add(run);
        }
        foreach (var payload in boxingPayloads)
            foreach (var method in module.GetTypes().SelectMany(t => t.Methods).Where(m => m.HasBody))
            {
                Require(!method.Body.Variables.Any(v => v.VariableType.GetElementType().FullName == payload.FullName), "boxing fixture has an independent value local");
                foreach (var instruction in method.Body.Instructions)
                    Require(!(instruction.Operand is TypeReference type && type.GetElementType().FullName == payload.FullName)
                        && !(instruction.OpCode == OpCodes.Newobj && instruction.Operand is MethodReference ctor && ctor.DeclaringType.GetElementType().FullName == payload.FullName),
                        "boxing fixture has an independent executable value-type root");
            }
        var constructorSelections = new List<(MethodDefinition Helper, TypeDefinition First, MethodDefinition? FirstAllocation, TypeDefinition Late, MethodDefinition? LateAllocation)>();
        foreach (string selection in new[] { "Single", "Plural", "Member", "Members", "Default", "Query", "DirectInvoke" })
        {
            (TypeDefinition Payload, MethodDefinition? Allocation) SelectionPayload(string binding)
            {
                string name = "ConstructorSelection" + selection + binding;
                var payload = Type(name); MethodDefinition? allocation = null;
                if (selection != "Query")
                {
                    allocation = Constructor(payload);
                    if (selection == "Default")
                    {
                        allocation.Attributes = MethodAttributes.Private | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName;
                        var factory = Method(payload, "Allocate", payload); factory.Body.Instructions.Insert(0, Instruction.Create(OpCodes.Newobj, allocation)); allocation = factory;
                        DefaultMember(payload, ".ctor");
                    }
                }
                var dependency = Type(name + "BodyDependency"); var touch = Method(dependency, "Touch");
                var leaf = Method(payload, "IntConstructorLeaf", attributes: MethodAttributes.Private | MethodAttributes.Static);
                var visibility = selection is "Plural" or "Member" or "Members" ? MethodAttributes.Private : MethodAttributes.Public;
                var ctor = Method(payload, ".ctor", attributes: visibility | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName);
                ctor.Parameters.Add(new ParameterDefinition(Core("Int32"))); ctor.Body.Instructions.Clear();
                ctor.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0)); ctor.Body.Instructions.Add(Instruction.Create(OpCodes.Call, objectCtor));
                ctor.Body.Instructions.Add(Instruction.Create(OpCodes.Call, touch));
                ctor.Body.Instructions.Add(Instruction.Create(OpCodes.Call, leaf)); ctor.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
                return (payload, allocation);
            }
            var first = SelectionPayload("First"); var late = SelectionPayload("Late");
            var helper = Method(driver, "SelectConstructor" + selection); helper.Body.Instructions.Clear();
            var parameter = new GenericParameter("T", helper); helper.GenericParameters.Add(parameter);
            if (selection != "Query") helper.Parameters.Add(new ParameterDefinition(parameter));
            void SelectedConstructorInfo()
            {
                TypeValue(helper, parameter);
                if (selection is "Single" or "Query" or "DirectInvoke")
                {
                    helper.Body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4_1)); helper.Body.Instructions.Add(Instruction.Create(OpCodes.Newarr, Core("Type")));
                    helper.Body.Instructions.Add(Instruction.Create(OpCodes.Dup)); helper.Body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4_0)); TypeValue(helper, Core("Int32"));
                    helper.Body.Instructions.Add(Instruction.Create(OpCodes.Stelem_Ref)); helper.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, Api("Type", "GetConstructor", "System.Type[]")));
                }
                else
                {
                    if (selection == "Member") helper.Body.Instructions.Add(Instruction.Create(OpCodes.Ldstr, ".ctor"));
                    if (selection != "Default") helper.Body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4, selection == "Members" ? 38 : 36));
                    helper.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, selection == "Default" ? Api("Type", "GetDefaultMembers")
                        : selection == "Member" ? Api("Type", "GetMember", "System.String", "System.Reflection.BindingFlags")
                        : Api("Type", selection == "Members" ? "GetMembers" : "GetConstructors", "System.Reflection.BindingFlags")));
                    helper.Body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4_0)); helper.Body.Instructions.Add(Instruction.Create(OpCodes.Ldelem_Ref));
                    helper.Body.Instructions.Add(Instruction.Create(OpCodes.Castclass, Core("Reflection.MethodBase")));
                }
            }
            foreach (bool flags in selection == "DirectInvoke" ? new[] { false, true } : new[] { selection == "Members" })
            {
                SelectedConstructorInfo();
                if (selection == "Query") { helper.Body.Instructions.Add(Instruction.Create(OpCodes.Pop)); continue; }
                if (selection != "DirectInvoke")
                {
                    helper.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0)); helper.Body.Instructions.Add(Instruction.Create(OpCodes.Box, parameter));
                }
                if (flags)
                {
                    helper.Body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4_0)); helper.Body.Instructions.Add(Instruction.Create(OpCodes.Ldnull));
                }
                helper.Body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4_1)); helper.Body.Instructions.Add(Instruction.Create(OpCodes.Newarr, objectType));
                helper.Body.Instructions.Add(Instruction.Create(OpCodes.Dup)); helper.Body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4_0)); helper.Body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4, 17));
                helper.Body.Instructions.Add(Instruction.Create(OpCodes.Box, Core("Int32"))); helper.Body.Instructions.Add(Instruction.Create(OpCodes.Stelem_Ref));
                if (flags) helper.Body.Instructions.Add(Instruction.Create(OpCodes.Ldnull));
                helper.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, selection == "DirectInvoke"
                    ? flags ? Api("Reflection.ConstructorInfo", "Invoke", "System.Reflection.BindingFlags", "System.Reflection.Binder", "System.Object[]", "System.Globalization.CultureInfo") : Api("Reflection.ConstructorInfo", "Invoke", "System.Object[]")
                    : flags ? Api("Reflection.MethodBase", "Invoke", "System.Object", "System.Reflection.BindingFlags", "System.Reflection.Binder", "System.Object[]", "System.Globalization.CultureInfo") : Api("Reflection.MethodBase", "Invoke", "System.Object", "System.Object[]")));
                helper.Body.Instructions.Add(Instruction.Create(OpCodes.Pop));
            }
            helper.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
            constructorSelections.Add((helper, first.Payload, first.Allocation, late.Payload, late.Allocation));
        }
        // FIFO scans Start's projected helpers before Schedule's second-level binder.
        var start = Method(driver, "Start"); start.Body.Instructions.Clear();
        var schedule = Method(driver, "Schedule"); schedule.Body.Instructions.Clear();
        var bindLate = Method(driver, "BindLate"); bindLate.Body.Instructions.Clear();
        foreach (var helper in helpers)
        {
            Call(start, Bound(helper.Method, helper.First));
            Call(bindLate, Bound(helper.Method, helper.Late));
        }
        Call(start, BindDefinition(seedFirst, definitionFirst));
        Call(bindLate, BindDefinition(seedLate, definitionLate));
        Call(start, BindMethod(methodOwnerFirst, methodFirst));
        Call(bindLate, BindMethod(methodOwnerLate, methodLate));
        Call(start, Bound(parameterHelper, parameterOwnerFirst));
        Call(bindLate, Bound(parameterHelper, parameterOwnerLate));
        Call(start, Bound(nestedHelper, nestedFirst));
        Call(bindLate, Bound(nestedHelper, nestedLate));
        Call(start, keyDirect.First); Call(bindLate, keyDirect.Late);
        Call(start, keyArray.First); Call(bindLate, keyArray.Late);
        Call(start, Bound(namedSymbolicRun, namedSymbolicFirst)); Call(bindLate, Bound(namedSymbolicRun, namedSymbolicLate));
        Call(start, Bound(namedInitializerHelper, namedInitializerFirst)); Call(bindLate, Bound(namedInitializerHelper, namedInitializerLate));
        foreach (var selection in constructorSelections)
        {
            if (selection.FirstAllocation is { } first) start.Body.Instructions.Add(Instruction.Create(first.IsConstructor ? OpCodes.Newobj : OpCodes.Call, first));
            Call(start, Bound(selection.Helper, selection.First));
            if (selection.LateAllocation is { } late) bindLate.Body.Instructions.Add(Instruction.Create(late.IsConstructor ? OpCodes.Newobj : OpCodes.Call, late));
            Call(bindLate, Bound(selection.Helper, selection.Late));
        }
        start.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
        Call(schedule, bindLate); schedule.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
        bindLate.Body.Instructions.Add(Instruction.Create(OpCodes.Ret)); calls.Add(start); calls.Add(schedule);

        var directPayload = Type("InheritedInterfacePayload"); Constructor(directPayload, true);
        var directInterface = Type("IInheritedConstruction", attributes: TypeAttributes.Public | TypeAttributes.Interface | TypeAttributes.Abstract); directInterface.BaseType = null;
        var directSlot = new MethodDefinition("Make", MethodAttributes.Public | MethodAttributes.Abstract | MethodAttributes.Virtual | MethodAttributes.NewSlot, directPayload);
        directInterface.Methods.Add(directSlot);
        var directBase = Type("InheritedConstructionBase"); directBase.Interfaces.Add(new InterfaceImplementation(directInterface));
        var directBody = Method(directBase, "Make", directPayload, MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.Final | MethodAttributes.NewSlot);
        directBody.Body.Instructions.Insert(0, Instruction.Create(OpCodes.Ldnull));
        var directDerived = Type("InheritedConstructionDerived", directBase);
        var directRun = Method(driver, "InheritedEnumeration"); directRun.Body.Instructions.Clear(); TypeValue(directRun, directDerived);
        directRun.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, Api("Type", "GetInterfaces")));
        directRun.Body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4_0)); directRun.Body.Instructions.Add(Instruction.Create(OpCodes.Ldelem_Ref));
        directRun.Body.Instructions.Add(Instruction.Create(OpCodes.Ldstr, "Make")); directRun.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, Api("Type", "GetMethod", "System.String")));
        directRun.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, Api("Reflection.MethodInfo", "get_ReturnType"))); Construct(directRun); calls.Add(directRun);

        var genericPayload = Type("InheritedGenericInterfacePayload"); Constructor(genericPayload, true);
        var root = Type("IInheritedRoot`1", attributes: TypeAttributes.Public | TypeAttributes.Interface | TypeAttributes.Abstract); root.BaseType = null;
        var rootParameter = new GenericParameter("T", root); root.GenericParameters.Add(rootParameter);
        root.Methods.Add(new MethodDefinition("Make", MethodAttributes.Public | MethodAttributes.Abstract | MethodAttributes.Virtual | MethodAttributes.NewSlot, rootParameter));
        var child = Type("IInheritedChild`1", attributes: TypeAttributes.Public | TypeAttributes.Interface | TypeAttributes.Abstract); child.BaseType = null;
        var childParameter = new GenericParameter("T", child); child.GenericParameters.Add(childParameter);
        var inheritedRoot = new GenericInstanceType(root); inheritedRoot.GenericArguments.Add(childParameter); child.Interfaces.Add(new InterfaceImplementation(inheritedRoot));
        var genericBase = Type("InheritedGenericBase`1"); var baseParameter = new GenericParameter("T", genericBase); genericBase.GenericParameters.Add(baseParameter);
        var inheritedChild = new GenericInstanceType(child); inheritedChild.GenericArguments.Add(baseParameter); genericBase.Interfaces.Add(new InterfaceImplementation(inheritedChild));
        var backing = new FieldDefinition("Backing", FieldAttributes.Private | FieldAttributes.Static, baseParameter); genericBase.Fields.Add(backing);
        var genericBody = Method(genericBase, "Make", baseParameter, MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.Final | MethodAttributes.NewSlot);
        genericBody.Body.Instructions.Insert(0, Instruction.Create(OpCodes.Ldsfld, backing));
        var boundBase = new GenericInstanceType(genericBase); boundBase.GenericArguments.Add(genericPayload);
        var genericDerived = Type("InheritedGenericDerived", boundBase);
        var genericRun = Method(driver, "InheritedNamedGeneric"); genericRun.Body.Instructions.Clear(); TypeValue(genericRun, genericDerived);
        genericRun.Body.Instructions.Add(Instruction.Create(OpCodes.Ldstr, "IInheritedRoot`1")); genericRun.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, Api("Type", "GetInterface", "System.String")));
        genericRun.Body.Instructions.Add(Instruction.Create(OpCodes.Ldstr, "Make")); genericRun.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, Api("Type", "GetMethod", "System.String")));
        genericRun.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, Api("Reflection.MethodInfo", "get_ReturnType"))); Construct(genericRun); calls.Add(genericRun);
        return calls.ToArray();
    }

    internal static void Check(ModuleDefinition module, string section = "all", ModuleDefinition? app = null)
    {
        if (GraphLookupValidation.Sections.Contains(section))
        {
            GraphLookupValidation.Check(module, section);
            Console.WriteLine("graph-projection=" + section + ":checked");
            return;
        }
        if (GraphDelegateBindingValidation.Sections.Contains(section))
        {
            GraphDelegateBindingValidation.Check(module, section);
            Console.WriteLine("graph-projection=" + section + ":checked");
            return;
        }
        if (GraphPropertyValidation.Sections.Contains(section))
        {
            GraphPropertyValidation.Check(module, section);
            Console.WriteLine("graph-projection=" + section + ":checked");
            return;
        }
        var sections = new[] { "plain-field", "plain-getter", "plain-method", "plain-accessor", "base", "argument", "element", "generic-definition", "generic-method", "parameter", "assembly", "nested", "interfaces", "argument-key", "argument-key-array", "invoke-member-concrete", "invoke-member-symbolic", "invoke-member-flags", "type-initializer", "type-initializer-symbolic", "nullable-inexact", "nullable-inexact-symbolic", "member-nested", "member-nested-symbolic", "activator-flags", "default-field", "default-event", "default-nested", "initializer-constructors", "initializer-member", "closure-named", "named-constructor-own", "named-constructor-base", "named-initializer", "named-constructor-symbolic", "named-initializer-symbolic", "type-name-forwarder", "type-name-forwarder-generic", "type-name-case-upper", "type-name-case-lower", "type-name-argument-union", "symbolic-constructor", "symbolic-constructors", "symbolic-constructor-member", "symbolic-constructor-members", "symbolic-constructor-default", "symbolic-constructor-query", "symbolic-constructor-direct-invoke", "attribute-filter-member", "attribute-filter-derived-parameter", "preserve-box-field", "preserve-box-return", "preserve-box-ref", "bind-box-named-return", "bind-box-method-ref" };
        Require(section == "all" || sections.Contains(section), "unknown projection test section: " + section);
        foreach (string selected in sections.Where(s => section == "all" || section == s))
        {
            if (selected.StartsWith("plain-", StringComparison.Ordinal))
            {
                string suffix = selected == "plain-field" ? "Field" : selected == "plain-getter" ? "Getter" : selected == "plain-accessor" ? "Accessor" : "Method";
                Body(module, "PlainOwner" + suffix + "Payload", "ToString");
            }
            else if (selected == "generic-definition")
            {
                foreach (string binding in new[] { "First", "Late" })
                    foreach (string name in new[] { ".ctor", "ConstructorLeaf" }) Body(module, "ProjectedDefinition" + binding, name);
                Body(module, "ProjectedDefinitionTarget`1", "Create");
            }
            else if (selected is "generic-method" or "parameter")
            {
                string prefix = selected == "generic-method" ? "ProjectedMethod" : "ProjectedParameter";
                foreach (string binding in new[] { "First", "Late" })
                    foreach (string name in new[] { ".ctor", "ConstructorLeaf" }) Body(module, prefix + binding, name);
                if (selected == "generic-method")
                    foreach (string binding in new[] { "First", "Late" }) Body(module, "ProjectedMethodOwner" + binding, "Create");
            }
            else if (selected is "argument-key" or "argument-key-array")
            {
                string suffix = selected == "argument-key" ? "Direct" : "Array";
                foreach (string binding in new[] { "First", "Late" })
                    foreach (string name in new[] { ".ctor", "ConstructorLeaf" }) Body(module, "ArgumentKey" + suffix + binding, name);
            }
            else if (selected == "invoke-member-concrete")
            {
                foreach (string suffix in new[] { "Explicit", "Defaults" })
                    foreach (string name in new[] { ".ctor", "ConstructorLeaf" }) Body(module, "InvokeConcrete" + suffix, name);
            }
            else if (selected == "invoke-member-symbolic")
            {
                Body(module, "InvokeKnownFieldPayload", "ToString");
                foreach (string name in new[] { ".ctor", "ConstructorLeaf" }) Body(module, "InvokeSymbolicConstructor", name);
            }
            else if (selected == "invoke-member-flags")
            {
                Body(module, "InvokeAlternativeFieldPayload", "ToString");
                foreach (string name in new[] { ".ctor", "ConstructorLeaf" }) Body(module, "InvokeAlternativeOwner", name);
            }
            else if (selected is "type-initializer" or "type-initializer-symbolic" or "initializer-constructors" or "initializer-member")
            {
                string suffix = selected == "type-initializer" ? "Known" : selected == "type-initializer-symbolic" ? "Symbolic" : selected == "initializer-constructors" ? "Constructors" : "Member";
                foreach (string name in new[] { ".cctor", "InitializerLeaf" }) Body(module, "Initializer" + suffix + "Payload", name);
            }
            else if (selected is "nullable-inexact" or "nullable-inexact-symbolic")
            {
                string suffix = selected == "nullable-inexact" ? "Direct" : "Symbolic";
                foreach (string name in new[] { "Describe", "DescribeLeaf" }) Body(module, "Nullable" + suffix + "Payload`1", name);
                Require(module.GetType("Graph.NullableDirectUnselectedDependency") is null,
                    "known nullable lookup retained an unselected method body dependency");
            }
            else if (selected is "member-nested" or "member-nested-symbolic")
            {
                string prefix = selected == "member-nested" ? "Known" : "Symbolic";
                foreach (string suffix in new[] { "Named", "Enumerated" })
                    foreach (string name in new[] { ".ctor", "ConstructorLeaf" }) Body(module, "MemberNested" + prefix + suffix + "/Inner", name);
            }
            else if (selected == "activator-flags")
            {
                foreach (string suffix in new[] { "Default", "Public" })
                    foreach (string name in new[] { ".ctor", "ConstructorLeaf" }) Body(module, "ActivatorFlags" + suffix, name);
            }
            else if (selected == "default-field")
            {
                foreach (string name in new[] { "ToString", "BoxLeaf" }) Body(module, "DefaultMemberFieldPayload", name);
            }
            else if (selected == "default-event")
            {
                var item = module.GetType("Graph.DefaultMemberEventOwner")?.Events.SingleOrDefault(e => e.Name == "Tick");
                Require(item is { AddMethod: not null, RemoveMethod: not null }, "default event selection lost event/accessor metadata");
                foreach (string name in new[] { "add_Tick", "AddLeaf" }) Body(module, "DefaultMemberEventOwner", name);
            }
            else if (selected == "default-nested")
            {
                foreach (string name in new[] { ".ctor", "ConstructorLeaf" }) Body(module, "DefaultMemberNestedOwner/Inner", name);
            }
            else if (selected == "closure-named")
            {
                foreach (string name in new[] { "Hidden", "HiddenLeaf" }) Body(module, "NamedClosureOwner/<>c__DisplayClass0_0", name);
            }
            else if (selected is "named-constructor-own" or "named-constructor-base" or "named-constructor-symbolic")
            {
                string[] owners = selected == "named-constructor-symbolic" ? new[] { "NamedConstructorSymbolicFirst", "NamedConstructorSymbolicLate" }
                    : new[] { selected == "named-constructor-own" ? "NamedConstructorOwn" : "NamedConstructorBase" };
                foreach (string owner in owners)
                {
                    Body(module, owner, ".ctor", 1); Body(module, owner, "SelectedConstructorLeaf");
                }
                Require(module.GetType("Graph.NamedConstructorUnselectedDependency") is null, "named constructor binding opened an unrelated ordinary method");
            }
            else if (selected is "named-initializer" or "named-initializer-symbolic")
            {
                foreach (string suffix in selected == "named-initializer" ? new[] { "Known" } : new[] { "First", "Late" })
                    foreach (string name in new[] { ".cctor", "SelectedInitializerLeaf" }) Body(module, "NamedInitializer" + suffix + "Payload", name);
            }
            else if (selected is "type-name-forwarder" or "type-name-forwarder-generic")
            {
                Require(app is not null, "forwarder check requires the application module");
                string[] names = selected == "type-name-forwarder" ? new[] { "ForwardedNamePayload" } : new[] { "ForwardedNameFactory`1", "ForwardedNameArgument" };
                foreach (string name in names)
                    Require(app!.ExportedTypes.Any(e => e.FullName == "Graph." + name && e.IsForwarder && e.Scope.Name == "GraphLib"),
                        "fixture lost its original type forwarder: " + name);
                if (selected == "type-name-forwarder")
                    foreach (string name in new[] { "Run", "RunLeaf" }) Body(module, "ForwardedNamePayload", name);
                else
                {
                    Body(module, "ForwardedNameFactory`1", "Create");
                    foreach (string name in new[] { ".ctor", "ConstructorLeaf" }) Body(module, "ForwardedNameArgument", name);
                }
            }
            else if (selected is "type-name-case-upper" or "type-name-case-lower")
            {
                string owner = selected == "type-name-case-upper" ? "CaseUpper" : "caselower";
                foreach (string name in new[] { "Run", "RunLeaf" }) Body(module, owner, name);
                Require(module.GetType("Graph.TypeNameUnselectedDependency") is null, "type-name selection opened an unrelated method body");
            }
            else if (selected == "type-name-argument-union")
            {
                Require(app is not null, "caller-scoped argument check requires the application module");
                Body(app!, "DuplicateNameFactory`1", "Create");
                foreach (string name in new[] { ".ctor", "ConstructorLeaf" }) Body(app!, "DuplicateNamePayload", name);
            }
            else if (selected.StartsWith("symbolic-constructor", StringComparison.Ordinal))
            {
                string selection = selected switch
                {
                    "symbolic-constructor" => "Single", "symbolic-constructors" => "Plural",
                    "symbolic-constructor-member" => "Member", "symbolic-constructor-members" => "Members",
                    "symbolic-constructor-default" => "Default", "symbolic-constructor-query" => "Query", _ => "DirectInvoke",
                };
                foreach (string binding in new[] { "First", "Late" })
                {
                    string owner = "ConstructorSelection" + selection + binding;
                    if (selection == "Query")
                    {
                        var ctor = module.GetType("Graph." + owner)?.Methods.SingleOrDefault(m => m.IsConstructor && m.Parameters.Count == 1);
                        Require(ctor is { HasBody: true } && ctor.Body.Instructions.Select(i => i.OpCode.Code)
                            .SequenceEqual(new[] { Code.Ldc_I4, Code.Pop, Code.Ldnull, Code.Throw }), "query-only symbolic constructor did not retain its metadata stub: " + binding);
                        Require(module.GetType("Graph." + owner + "BodyDependency") is null, "query-only symbolic constructor opened its body dependency: " + binding);
                    }
                    else
                    {
                        Body(module, owner, ".ctor", 1); Body(module, owner, "IntConstructorLeaf"); Body(module, owner + "BodyDependency", "Touch");
                    }
                }
            }
            else if (selected is "attribute-filter-member" or "attribute-filter-derived-parameter")
            {
                bool derived = selected == "attribute-filter-derived-parameter";
                string prefix = derived ? "UnknownParameterAttribute" : "UnknownMemberAttribute";
                Body(module, prefix + "Host", "Touch");
                Unexecuted(module, prefix + (derived ? "DerivedMarker" : "Marker"), ".ctor");
                Unexecuted(module, prefix + (derived ? "BaseMarker" : "Marker"), "set_Label");
                foreach (string dependency in new[] { "ConstructorDependency", "SetterDependency" })
                    Require(module.GetType("Graph." + prefix + dependency) is null, "unknown attribute owner opened an unrelated body: " + dependency);
                Require(module.GetType("Graph." + prefix + "ArgumentPayload") is not null, "attribute materialization lost its Type-valued named argument");
                Unexecuted(module, prefix + "Host", "Annotated"); Unexecuted(module, prefix + "ArgumentPayload", "Unselected");
                foreach (string dependency in new[] { "AnnotatedBodyDependency", "ArgumentBodyDependency" })
                    Require(module.GetType("Graph." + prefix + dependency) is null, "attribute filter opened an unrelated method body: " + dependency);
            }
            else if (selected.StartsWith("preserve-box-", StringComparison.Ordinal) || selected.StartsWith("bind-box-", StringComparison.Ordinal))
            {
                string selection = selected switch
                {
                    "preserve-box-field" => "PreserveField", "preserve-box-return" => "PreserveReturn", "preserve-box-ref" => "PreserveRef",
                    "bind-box-named-return" => "NamedReturn", _ => "MethodRef",
                };
                string prefix = "Boxing" + selection;
                if (selection == "PreserveField")
                    Require(module.GetType("Graph." + prefix + "Owner")?.Fields.Any(f => f.Name == "Value") == true, "explicitly preserved field was lost");
                else Body(module, prefix + "Owner", "Read");
                if (selection is "NamedReturn" or "MethodRef")
                    Require(module.GetType("Graph." + prefix + "Delegate") is { HasGenericParameters: false } type && type.BaseType.FullName == "System.MulticastDelegate",
                        "boxing fixture lost its nongeneric delegate");
                Body(module, prefix + "Payload", "ToString"); Body(module, prefix + "Payload", "ToStringLeaf"); Body(module, prefix + "BodyDependency", "Touch");
                Unexecuted(module, prefix + "Payload", "Unselected");
                Require(module.GetType("Graph." + prefix + "UnselectedDependency") is null, "boxing closure opened an unrelated ordinary body");
            }
            else if (selected == "assembly")
            {
                Require(app is not null, "assembly projection check requires the application module");
                foreach (string name in new[] { ".ctor", "ConstructorLeaf" }) Body(app!, "AssemblyProjectionPayload", name);
                Body(app!, "AssemblyProjectionFactory", "ConstructAssemblyProjectionPayload");
            }
            else if (selected == "nested")
            {
                foreach (string binding in new[] { "First", "Late" })
                {
                    foreach (string name in new[] { ".ctor", "ConstructorLeaf" }) Body(module, "NestedProjectionPayload" + binding, name);
                    Body(module, "NestedProjectionOwner" + binding + "/Nested", "ConstructNestedProjectionPayload");
                }
            }
            else if (selected == "interfaces")
            {
                foreach (string owner in new[] { "InheritedInterfacePayload", "InheritedGenericInterfacePayload" })
                    foreach (string name in new[] { ".ctor", "ConstructorLeaf" }) Body(module, owner, name);
            }
            else
            {
                string suffix = selected == "base" ? "Base" : selected == "argument" ? "Argument" : "Element";
                foreach (string binding in new[] { "First", "Late" })
                    foreach (string name in new[] { ".ctor", "ConstructorLeaf" }) Body(module, "Projected" + suffix + binding, name);
                if (selected == "argument") Require(module.GetType("Graph.ProjectionIgnoredBodyDependency") is null,
                    "generic argument projection constructed an unselected position");
            }
            Console.WriteLine("graph-projection=" + selected + (selected.StartsWith("symbolic-constructor", StringComparison.Ordinal) || selected is "base" or "argument" or "element" or "generic-definition" or "generic-method" or "parameter" or "nested" or "argument-key" or "argument-key-array" or "named-constructor-symbolic" or "named-initializer-symbolic" ? ":first-and-late-bindings" : ":checked"));
        }
    }

    static void Body(ModuleDefinition module, string owner, string name, int? parameters = null)
    {
        var method = module.GetType("Graph." + owner)?.Methods.SingleOrDefault(m => m.Name == name && (parameters is null || m.Parameters.Count == parameters));
        Require(method is { HasBody: true } && method.Body.Instructions.Count != 0 && !method.Body.Instructions.Select(i => i.OpCode.Code)
            .SequenceEqual(new[] { Code.Ldc_I4, Code.Pop, Code.Ldnull, Code.Throw }), "projection target body was lost: " + owner + "::" + name
                + (parameters is null ? "" : "/parameters=" + parameters.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)));
    }

    static void Unexecuted(ModuleDefinition module, string owner, string name)
    {
        var method = module.GetType("Graph." + owner)?.Methods.SingleOrDefault(m => m.Name == name);
        Require(method is null || method.HasBody && method.Body.Instructions.Select(i => i.OpCode.Code)
            .SequenceEqual(new[] { Code.Ldc_I4, Code.Pop, Code.Ldnull, Code.Throw }), "an unselected method retained its original body: " + owner + "::" + name);
    }

    static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}

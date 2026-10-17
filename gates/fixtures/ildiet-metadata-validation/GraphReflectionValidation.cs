using Mono.Cecil;
using Mono.Cecil.Cil;

static class GraphReflectionValidation
{
    internal static MethodReference[] Create(AssemblyDefinition app, AssemblyDefinition library, AssemblyDefinition core)
    {
        var module = library.MainModule;
        TypeReference Core(string name) => module.ImportReference(core.MainModule.GetType("System." + name));
        var objectType = Core("Object"); var voidType = Core("Void"); var stringType = Core("String");
        var runtimeType = core.MainModule.GetType("System.Type");
        var fromHandle = module.ImportReference(runtimeType.Methods.Single(m => m.Name == "GetTypeFromHandle" && m.IsPublic && m.IsStatic && m.Parameters.Count == 1 && m.Parameters[0].ParameterType.FullName == "System.RuntimeTypeHandle"));
        var getField = module.ImportReference(runtimeType.Methods.Single(m => m.Name == "GetField" && m.IsPublic && !m.IsStatic && m.Parameters.Count == 1 && m.Parameters[0].ParameterType.FullName == "System.String"));
        var fieldValue = module.ImportReference(core.MainModule.GetType("System.Reflection.FieldInfo").Methods.Single(m => m.Name == "GetValue" && m.IsPublic && !m.IsStatic && m.Parameters.Count == 1 && m.Parameters[0].ParameterType.FullName == "System.Object"));
        var objectCtor = module.ImportReference(core.MainModule.GetType("System.Object").Methods.Single(m => m.IsConstructor && !m.IsStatic && m.Parameters.Count == 0));
        var toString = module.ImportReference(core.MainModule.GetType("System.Object").Methods.Single(m => m.Name == "ToString" && m.IsPublic && !m.IsStatic && m.Parameters.Count == 0));
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
            owner.Methods.Add(method);
            method.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
            return method;
        }
        MethodDefinition Constructor(TypeDefinition owner)
        {
            var ctor = Method(owner, ".ctor", attributes: MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName);
            ctor.Body.Instructions.Insert(0, Instruction.Create(OpCodes.Ldarg_0));
            ctor.Body.Instructions.Insert(1, Instruction.Create(OpCodes.Call, objectCtor));
            return ctor;
        }
        void TypeValue(MethodDefinition method, TypeReference type)
        {
            method.Body.Instructions.Add(Instruction.Create(OpCodes.Ldtoken, type));
            method.Body.Instructions.Add(Instruction.Create(OpCodes.Call, fromHandle));
        }
        var calls = new List<MethodReference>();
        // No IL token references these types: their assembly/type strings are the only roots.
        foreach (var assembly in new[] { app, library })
        {
            var own = assembly.MainModule;
            string suffix = assembly == app ? "App" : "Lib";
            var driver = new TypeDefinition("Graph", "NamedConstruction" + suffix, TypeAttributes.Public | TypeAttributes.Class, own.ImportReference(objectType));
            own.Types.Add(driver);
            var run = new MethodDefinition("Run", MethodAttributes.Public | MethodAttributes.Static, own.ImportReference(voidType));
            driver.Methods.Add(run);
            foreach (string route in new[] { "Activator", "Assembly" })
            {
                var target = new TypeDefinition("Graph", "Named" + route + suffix, TypeAttributes.Public | TypeAttributes.Class, own.ImportReference(objectType));
                own.Types.Add(target);
                var leaf = new MethodDefinition("ConstructorLeaf", MethodAttributes.Private | MethodAttributes.Static, own.ImportReference(voidType));
                target.Methods.Add(leaf); leaf.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
                var ctor = new MethodDefinition(".ctor", MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName, own.ImportReference(voidType));
                target.Methods.Add(ctor);
                ctor.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
                ctor.Body.Instructions.Add(Instruction.Create(OpCodes.Call, own.ImportReference(objectCtor)));
                ctor.Body.Instructions.Add(Instruction.Create(OpCodes.Call, leaf));
                ctor.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
                if (route == "Activator")
                {
                    run.Body.Instructions.Add(Instruction.Create(OpCodes.Ldstr, assembly.Name.Name));
                    run.Body.Instructions.Add(Instruction.Create(OpCodes.Ldstr, target.FullName));
                    run.Body.Instructions.Add(Instruction.Create(OpCodes.Call, own.ImportReference(core.MainModule.GetType("System.Activator").Methods.Single(m => m.Name == "CreateInstance"
                        && m.Parameters.Count == 2 && m.Parameters.All(p => p.ParameterType.FullName == "System.String")))));
                }
                else
                {
                    run.Body.Instructions.Add(Instruction.Create(OpCodes.Call, own.ImportReference(core.MainModule.GetType("System.Reflection.Assembly").Methods.Single(m => m.Name == "GetExecutingAssembly" && m.IsPublic && m.IsStatic && m.Parameters.Count == 0))));
                    run.Body.Instructions.Add(Instruction.Create(OpCodes.Ldstr, target.FullName));
                    run.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, own.ImportReference(core.MainModule.GetType("System.Reflection.Assembly").Methods.Single(m => m.Name == "CreateInstance" && m.IsPublic && !m.IsStatic
                        && m.Parameters.Count == 1 && m.Parameters[0].ParameterType.FullName == "System.String"))));
                }
                run.Body.Instructions.Add(Instruction.Create(OpCodes.Pop));
            }
            run.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
            calls.Add(run);
        }

        var fieldDriver = Type("ReflectedDefaultFields");
        TypeDefinition Struct(string name)
        {
            var type = Type(name, Core("ValueType"), TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.SequentialLayout | TypeAttributes.BeforeFieldInit);
            var method = Method(type, "ToString", stringType, MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.HideBySig);
            method.Body.Instructions.Insert(0, Instruction.Create(OpCodes.Ldstr, name));
            return type;
        }
        MethodDefinition ReadField(string name, TypeReference holder, MethodReference? ctor)
        {
            var method = Method(fieldDriver, name);
            method.Body.Instructions.Clear();
            TypeValue(method, holder);
            method.Body.Instructions.Add(Instruction.Create(OpCodes.Ldstr, "Value"));
            method.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, getField));
            method.Body.Instructions.Add(ctor is null ? Instruction.Create(OpCodes.Ldnull) : Instruction.Create(OpCodes.Newobj, ctor));
            method.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, fieldValue));
            method.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, toString));
            method.Body.Instructions.Add(Instruction.Create(OpCodes.Pop));
            method.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
            return method;
        }
        var direct = Struct("DefaultFieldPayload");
        var directHolder = Type("DefaultFieldHolder");
        directHolder.Fields.Add(new FieldDefinition("Value", FieldAttributes.Public, direct));
        calls.Add(ReadField("Direct", directHolder, Constructor(directHolder)));
        var inner = Struct("NestedInnerPayload");
        var equalityDependency = Type("NestedEqualityDependency");
        var equalityTouch = Method(equalityDependency, "Touch");
        var equalityLeaf = Method(inner, "EqualityLeaf", attributes: MethodAttributes.Private | MethodAttributes.Static);
        equalityLeaf.Body.Instructions.Insert(0, Instruction.Create(OpCodes.Call, equalityTouch));
        var equals = Method(inner, "Equals", Core("Boolean"), MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.HideBySig);
        equals.Parameters.Add(new ParameterDefinition(objectType));
        equals.Body.Instructions.Insert(0, Instruction.Create(OpCodes.Call, equalityLeaf));
        equals.Body.Instructions.Insert(1, Instruction.Create(OpCodes.Ldc_I4_1));
        var unusedNested = Type("NestedUnselectedDependency");
        var unselectedNested = Method(inner, "Unselected");
        unselectedNested.Body.Instructions.Insert(0, Instruction.Create(OpCodes.Call, Method(unusedNested, "Touch")));
        var outer = Struct("NestedOuterPayload");
        outer.Fields.Add(new FieldDefinition("Inner", FieldAttributes.Public, inner));
        var outerHolder = Type("NestedOuterHolder");
        outerHolder.Fields.Add(new FieldDefinition("Value", FieldAttributes.Public, outer));
        calls.Add(ReadField("Nested", outerHolder, Constructor(outerHolder)));
        var nullable = Struct("NullableFieldPayload");
        var nullableType = new GenericInstanceType(Core("Nullable`1")); nullableType.GenericArguments.Add(nullable);
        var nullableHolder = Type("NullableFieldHolder");
        nullableHolder.Fields.Add(new FieldDefinition("Value", FieldAttributes.Public, nullableType));
        calls.Add(ReadField("Nullable", nullableHolder, Constructor(nullableHolder)));
        var byref = Struct("ByRefFieldPayload");
        var byrefHolder = Type("ByRefFieldHolder", Core("ValueType"), TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.SequentialLayout | TypeAttributes.BeforeFieldInit);
        byrefHolder.CustomAttributes.Add(new CustomAttribute(module.ImportReference(core.MainModule.GetType("System.Runtime.CompilerServices.IsByRefLikeAttribute")
            .Methods.Single(m => m.IsConstructor && !m.IsStatic && m.Parameters.Count == 0))));
        byrefHolder.Fields.Add(new FieldDefinition("Value", FieldAttributes.Public, new ByReferenceType(byref)));
        calls.Add(ReadField("ByRefLikeMetadata", byrefHolder, null));
        var genericPayload = Struct("GenericFieldPayload");
        var genericHolder = Type("GenericFieldHolder`1");
        var fieldParameter = new GenericParameter("T", genericHolder); genericHolder.GenericParameters.Add(fieldParameter);
        genericHolder.Fields.Add(new FieldDefinition("Value", FieldAttributes.Public, fieldParameter));
        Constructor(genericHolder);
        var genericRead = Method(fieldDriver, "Generic");
        var methodParameter = new GenericParameter("T", genericRead); genericRead.GenericParameters.Add(methodParameter);
        var closedHolder = new GenericInstanceType(genericHolder); closedHolder.GenericArguments.Add(methodParameter);
        var closedCtor = new MethodReference(".ctor", voidType, closedHolder) { HasThis = true };
        genericRead.Body.Instructions.Clear();
        TypeValue(genericRead, closedHolder);
        genericRead.Body.Instructions.Add(Instruction.Create(OpCodes.Ldstr, "Value"));
        genericRead.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, getField));
        genericRead.Body.Instructions.Add(Instruction.Create(OpCodes.Newobj, closedCtor));
        genericRead.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, fieldValue));
        genericRead.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, toString));
        genericRead.Body.Instructions.Add(Instruction.Create(OpCodes.Pop)); genericRead.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
        var boundRead = new GenericInstanceMethod(genericRead); boundRead.GenericArguments.Add(genericPayload); calls.Add(boundRead);

        var getMember = module.ImportReference(runtimeType.Methods.Single(m => m.Name == "GetMember" && m.IsPublic && !m.IsStatic && m.Parameters.Count == 1 && m.Parameters[0].ParameterType.FullName == "System.String"));
        var getMembers = module.ImportReference(runtimeType.Methods.Single(m => m.Name == "GetMembers" && m.IsPublic && !m.IsStatic && m.Parameters.Count == 1 && m.Parameters[0].ParameterType.FullName == "System.Reflection.BindingFlags"));
        var ctorInvoke = module.ImportReference(core.MainModule.GetType("System.Reflection.ConstructorInfo").Methods.Single(m => m.Name == "Invoke" && m.IsPublic && !m.IsStatic && m.Parameters.Count == 1 && m.Parameters[0].ParameterType.FullName == "System.Object[]"));
        var ctorDriver = Type("GeneralMemberConstruction");
        var controlDependency = Type("GeneralMemberUnselectedDependency");
        var controlLeaf = Method(controlDependency, "Touch");
        foreach (string selection in new[] { "Named", "Enumerated" })
        {
            var target = Type("GeneralMember" + selection);
            var ctor = Constructor(target);
            var leaf = Method(target, "ConstructorLeaf", attributes: MethodAttributes.Private | MethodAttributes.Static);
            ctor.Body.Instructions.Insert(2, Instruction.Create(OpCodes.Call, leaf));
            var unselected = Method(target, "Unselected"); unselected.Body.Instructions.Insert(0, Instruction.Create(OpCodes.Call, controlLeaf));
            var run = Method(ctorDriver, selection); run.Body.Instructions.Clear();
            TypeValue(run, target);
            run.Body.Instructions.Add(selection == "Named" ? Instruction.Create(OpCodes.Ldstr, ".ctor") : Instruction.Create(OpCodes.Ldc_I4, 22));
            run.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, selection == "Named" ? getMember : getMembers));
            run.Body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4_0)); run.Body.Instructions.Add(Instruction.Create(OpCodes.Ldelem_Ref));
            run.Body.Instructions.Add(Instruction.Create(OpCodes.Castclass, Core("Reflection.ConstructorInfo")));
            run.Body.Instructions.Add(Instruction.Create(OpCodes.Ldnull)); run.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, ctorInvoke));
            run.Body.Instructions.Add(Instruction.Create(OpCodes.Pop)); run.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
            calls.Add(run);
        }

        var factory = Type("IDifferentlyNamedFactory", attributes: TypeAttributes.Public | TypeAttributes.Interface | TypeAttributes.Abstract); factory.BaseType = null;
        var declaration = new MethodDefinition("Create", MethodAttributes.Public | MethodAttributes.Abstract | MethodAttributes.Virtual | MethodAttributes.NewSlot, objectType);
        factory.Methods.Add(declaration);
        var declaredParameter = new GenericParameter("T", declaration) { Attributes = GenericParameterAttributes.DefaultConstructorConstraint };
        declaration.GenericParameters.Add(declaredParameter);
        var implementation = Type("DifferentlyNamedFactory"); implementation.Interfaces.Add(new InterfaceImplementation(factory));
        var implementationCtor = Constructor(implementation);
        var build = Method(implementation, "Build", objectType, MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.Final | MethodAttributes.NewSlot);
        var buildParameter = new GenericParameter("T", build) { Attributes = GenericParameterAttributes.DefaultConstructorConstraint }; build.GenericParameters.Add(buildParameter);
        build.Overrides.Add(declaration);
        var activate = module.ImportReference(core.MainModule.GetType("System.Activator").Methods.Single(m => m.Name == "CreateInstance" && m.HasGenericParameters && m.Parameters.Count == 0));
        var create = new GenericInstanceMethod(activate); create.GenericArguments.Add(buildParameter);
        build.Body.Instructions.Clear(); build.Body.Instructions.Add(Instruction.Create(OpCodes.Call, create));
        build.Body.Instructions.Add(Instruction.Create(OpCodes.Box, buildParameter)); build.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
        var payload = Type("MethodImplConstructorPayload"); var payloadCtor = Constructor(payload);
        var payloadLeaf = Method(payload, "ConstructorLeaf", attributes: MethodAttributes.Private | MethodAttributes.Static);
        payloadCtor.Body.Instructions.Insert(2, Instruction.Create(OpCodes.Call, payloadLeaf));
        var dispatch = Method(fieldDriver, "DifferentNameDispatch"); dispatch.Body.Instructions.Clear();
        dispatch.Body.Instructions.Add(Instruction.Create(OpCodes.Newobj, implementationCtor));
        var call = new GenericInstanceMethod(declaration); call.GenericArguments.Add(payload);
        dispatch.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, call));
        dispatch.Body.Instructions.Add(Instruction.Create(OpCodes.Pop)); dispatch.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
        calls.Add(dispatch);
        return calls.ToArray();
    }

    internal static void Check(ModuleDefinition app, ModuleDefinition library)
    {
        foreach (var module in new[] { app, library })
        {
            string suffix = module == app ? "App" : "Lib";
            foreach (string route in new[] { "Activator", "Assembly" })
                foreach (string method in new[] { ".ctor", "ConstructorLeaf" })
                    Body(module, "Named" + route + suffix, method);
        }
        foreach (string owner in new[] { "DefaultFieldPayload", "NestedOuterPayload", "NestedInnerPayload", "NullableFieldPayload", "ByRefFieldPayload", "GenericFieldPayload" }) Body(library, owner, "ToString");
        Body(library, "NestedInnerPayload", "Equals");
        Body(library, "NestedInnerPayload", "EqualityLeaf");
        Body(library, "NestedEqualityDependency", "Touch");
        Require(library.GetType("Graph.NestedUnselectedDependency") is null,
            "nested boxed value-field dispatch opened an unrelated method body");
        foreach (string owner in new[] { "GeneralMemberNamed", "GeneralMemberEnumerated", "MethodImplConstructorPayload" })
            foreach (string method in new[] { ".ctor", "ConstructorLeaf" }) Body(library, owner, method);
        Body(library, "DifferentlyNamedFactory", "Build");
        Require(library.GetType("Graph.GeneralMemberUnselectedDependency") is null, "general member lookup opened an unselected-name body");
        var holder = library.GetType("Graph.ByRefFieldHolder");
        Require(holder is not null && holder.CustomAttributes.Any(a => a.AttributeType.FullName == "System.Runtime.CompilerServices.IsByRefLikeAttribute")
            && holder.Fields.Single().FieldType is ByReferenceType, "by-ref field fixture lost its valid byref-like holder shape");
        Console.WriteLine("graph-reflection=activator-named-app,activator-named-library,assembly-named-app,assembly-named-library,default-field-box,nested-field-box,nullable-field-box,byref-field-box,generic-field-box,getmember-ctor,getmembers-ctor,differently-named-methodimpl");
    }

    static void Body(ModuleDefinition module, string owner, string name)
    {
        var method = module.GetType("Graph." + owner)?.Methods.SingleOrDefault(m => m.Name == name);
        Require(method is { HasBody: true } && !method.Body.Instructions.Select(i => i.OpCode.Code)
            .SequenceEqual(new[] { Code.Ldc_I4, Code.Pop, Code.Ldnull, Code.Throw }), "reflection target body was lost: " + owner + "::" + name);
    }

    static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}

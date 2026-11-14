using System.Collections;
using Mono.Cecil;
using Mono.Cecil.Cil;
using BindingFlags = System.Reflection.BindingFlags;

static partial class StrippingLevelValidation
{
    static readonly string[] Targets =
    {
        "BaseTarget", "SignatureTarget", "FieldTarget", "PropertyTarget", "EventTarget",
        "ArgumentTarget", "BodyTarget", "AttributeTarget", "AttributeArgumentTarget", "ConstraintTarget", "MethodArgumentTarget",
        "CallerArgumentTarget", "CalleeArgumentTarget", "OpenBodyTarget", "PreserveTarget",
    };
    static readonly string[] TraceOwners =
    {
        "AllocationTrigger", "VirtualTrigger", "GenericVirtualTrigger", "EqualityTrigger", "HashTrigger", "StaticReadTrigger",
    };

    public static bool Run(string[] args)
    {
        if (args.Length == 4 && args[0] == "--create-stripping-intercept-fixture")
        {
            CreateRetainedIntercept(args[1], args[2], args[3]);
            Console.WriteLine("stripping-fixture=intercept:" + args[3]);
            return true;
        }
        if ((args.Length == 2 || args.Length == 3 && args[2] == "retained-equality") && args[0] == "--create-stripping-fixture")
        {
            Create(args[1], args.Length == 3);
            Console.WriteLine("stripping-fixture=static-closure");
            return true;
        }
        if (args.Length == 3 && args[0] == "--check-stripping-fixture")
        {
            Check(args[1], args[2].StartsWith("medium", StringComparison.Ordinal), args[2] != "medium-direct");
            Console.WriteLine("stripping-ildiet=" + args[2]);
            return true;
        }
        if (args.Length == 4 && args[0] == "--check-stripping-core")
        {
            CheckCore(args[1], args[2], args[3].StartsWith("medium", StringComparison.Ordinal), args[3] != "medium-direct");
            Console.WriteLine("stripping-core=" + args[3]);
            return true;
        }
        if (args.Length == 3 && args[0] == "--create-stripping-forwarder-fixture")
        {
            CreateForwardedAttribute(args[1], args[2]);
            Console.WriteLine("stripping-fixture=forwarded-attribute");
            return true;
        }
        if (args.Length == 3 && args[0] == "--create-stripping-buffer-move-shape-fixture")
        {
            CreateBufferMoveShape(args[1], args[2]);
            Console.WriteLine("stripping-fixture=buffer-move-shape");
            return true;
        }
        if (args.Length == 3 && args[0] == "--create-stripping-unavailable-imports-fixture")
        {
            CreateUnavailableImports(args[1], args[2]);
            Console.WriteLine("stripping-fixture=unavailable-imports");
            return true;
        }
        return false;
    }

    static void Create(string directory, bool retainedEquality)
    {
        Directory.CreateDirectory(directory);
        using var core = AssemblyDefinition.CreateAssembly(new AssemblyNameDefinition("System.Private.CoreLib", new Version(10, 0)),
            "System.Private.CoreLib", ModuleKind.Dll);
        using var library = AssemblyDefinition.CreateAssembly(new AssemblyNameDefinition("MediumLib", new Version(1, 0)),
            "MediumLib", ModuleKind.Dll);
        using var app = AssemblyDefinition.CreateAssembly(new AssemblyNameDefinition("MediumApp", new Version(1, 0)),
            "MediumApp", ModuleKind.Console);
        var cm = core.MainModule;
        var lm = library.MainModule;
        var am = app.MainModule;
        foreach (var module in new[] { lm, am })
        {
            var coreReference = (AssemblyNameReference)module.TypeSystem.CoreLibrary;
            coreReference.Name = core.Name.Name;
            coreReference.Version = core.Name.Version;
            coreReference.PublicKeyToken = Array.Empty<byte>();
        }
        TypeDefinition CoreType(string name, TypeReference? parent = null)
        {
            var type = new TypeDefinition("System", name, TypeAttributes.Public | TypeAttributes.Class, parent);
            cm.Types.Add(type);
            return type;
        }
        var obj = CoreType("Object");
        var value = CoreType("ValueType", obj);
        CoreType("Void", value);
        var voidType = lm.TypeSystem.Void;
        CoreType("Int32", value);
        CoreType("Boolean", value);
        var objectEquals = Method(obj, "Equals", lm.TypeSystem.Boolean);
        objectEquals.Attributes = MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.NewSlot;
        objectEquals.Parameters.Add(new ParameterDefinition(obj));
        objectEquals.Body.Instructions.Insert(0, Instruction.Create(OpCodes.Ldc_I4_0));
        var staticEquals = Method(obj, "Equals", lm.TypeSystem.Boolean);
        staticEquals.Attributes = MethodAttributes.Public | MethodAttributes.Static;
        staticEquals.Parameters.Add(new ParameterDefinition(obj));
        staticEquals.Parameters.Add(new ParameterDefinition(obj));
        staticEquals.Body.Instructions.Insert(0, Instruction.Create(OpCodes.Ldc_I4_0));
        MethodDefinition? arrayIndexOf = null;
        if (retainedEquality)
        {
            var arrayType = CoreType("Array", obj);
            arrayIndexOf = Method(arrayType, "IndexOf", lm.TypeSystem.Int32);
            arrayIndexOf.Attributes = MethodAttributes.Public | MethodAttributes.Static;
            arrayIndexOf.Parameters.Add(new ParameterDefinition(arrayType));
            arrayIndexOf.Parameters.Add(new ParameterDefinition(obj));
            arrayIndexOf.Body.Instructions.Insert(0, Instruction.Create(OpCodes.Ldc_I4_M1));
        }
        var attr = CoreType("Attribute", obj);
        var refLikeAttribute = new TypeDefinition("System.Runtime.CompilerServices", "IsByRefLikeAttribute",
            TypeAttributes.Public | TypeAttributes.Class, attr);
        cm.Types.Add(refLikeAttribute);
        var refLikeCtor = Method(refLikeAttribute, ".ctor", voidType, constructor: true);
        var typeType = CoreType("Type", obj);
        var coreWitness = CoreType("MediumWitness", obj);
        var coreMarker = CoreType("MediumMarkerAttribute", attr);
        var coreMarkerCtor = Method(coreMarker, ".ctor", voidType, constructor: true);
        var markerFlag = new FieldDefinition("Flag", FieldAttributes.Private | FieldAttributes.Static, lm.TypeSystem.Int32);
        coreMarker.Fields.Add(markerFlag);
        var markerInitializer = Method(coreMarker, ".cctor", voidType);
        markerInitializer.Body.Instructions.Insert(0, Instruction.Create(OpCodes.Ldc_I4_1));
        markerInitializer.Body.Instructions.Insert(1, Instruction.Create(OpCodes.Stsfld, markerFlag));
        coreWitness.CustomAttributes.Add(new CustomAttribute(coreMarkerCtor));
        var frameworkMethod = Method(coreWitness, "FrameworkPrivate", voidType);
        frameworkMethod.CustomAttributes.Add(new CustomAttribute(coreMarkerCtor));
        Method(coreWitness, ".ctor", voidType, constructor: true);
        coreWitness.Fields.Add(new FieldDefinition("FrameworkField", FieldAttributes.Private, lm.TypeSystem.Int32));
        var frameworkGet = Method(coreWitness, "get_FrameworkValue", lm.TypeSystem.Int32);
        frameworkGet.Attributes |= MethodAttributes.SpecialName;
        frameworkGet.Body.Instructions.Insert(0, Instruction.Create(OpCodes.Ldc_I4_0));
        var frameworkSet = Method(coreWitness, "set_FrameworkValue", voidType);
        frameworkSet.Attributes |= MethodAttributes.SpecialName;
        frameworkSet.Parameters.Add(new ParameterDefinition(lm.TypeSystem.Int32));
        coreWitness.Properties.Add(new PropertyDefinition("FrameworkValue", PropertyAttributes.None, lm.TypeSystem.Int32)
            { GetMethod = frameworkGet, SetMethod = frameworkSet });
        var frameworkAdd = Method(coreWitness, "add_FrameworkChanged", voidType);
        frameworkAdd.Attributes |= MethodAttributes.SpecialName;
        frameworkAdd.Parameters.Add(new ParameterDefinition(obj));
        var frameworkRemove = Method(coreWitness, "remove_FrameworkChanged", voidType);
        frameworkRemove.Attributes |= MethodAttributes.SpecialName;
        frameworkRemove.Parameters.Add(new ParameterDefinition(obj));
        coreWitness.Events.Add(new EventDefinition("FrameworkChanged", EventAttributes.None, obj)
            { AddMethod = frameworkAdd, RemoveMethod = frameworkRemove });
        TypeDefinition Type(string name, TypeReference? parent = null)
        {
            var type = new TypeDefinition("MediumFixture", name, TypeAttributes.Public | TypeAttributes.Class,
                parent ?? lm.ImportReference(obj));
            lm.Types.Add(type);
            return type;
        }
        var targets = Targets.ToDictionary(name => name, name => Type(name));
        var preserved = Type("PreservedOnly");
        preserved.Fields.Add(new FieldDefinition("Selected", FieldAttributes.Private, targets["PreserveTarget"]));
        Method(preserved, ".ctor", voidType, constructor: true);
        Method(preserved, "Marker", voidType);
        var refLike = Type("RefLikeTarget", lm.ImportReference(value));
        refLike.CustomAttributes.Add(new CustomAttribute(lm.ImportReference(refLikeCtor)));
        Method(refLike, "Marker", voidType);
        var outer = Type("Outer");
        Method(outer, "MustStayAbsent", voidType);
        var inner = new TypeDefinition("", "Inner", TypeAttributes.NestedPublic | TypeAttributes.Class, lm.ImportReference(obj));
        outer.NestedTypes.Add(inner);
        Method(inner, "Marker", voidType);
        foreach (var target in targets.Values)
        {
            Method(target, "Marker", lm.ImportReference(voidType));
            Method(target, ".ctor", lm.ImportReference(voidType), constructor: true);
        }
        var open = Type("Open`1");
        open.GenericParameters.Add(new GenericParameter("T", open));
        var openMethod = Method(open, "Uncalled", voidType);
        openMethod.Body.Instructions.Insert(0, Instruction.Create(OpCodes.Call,
            targets["OpenBodyTarget"].Methods.Single(m => m.Name == "Marker")));
        var initializationState = Type("InitializationState");
        var initialized = new FieldDefinition("Value", FieldAttributes.Public | FieldAttributes.Static, lm.TypeSystem.Int32);
        initializationState.Fields.Add(initialized);
        var deferredBox = Type("DeferredBox", lm.ImportReference(value));
        deferredBox.Attributes = TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.SequentialLayout;
        var deferredOwner = retainedEquality ? Type("DormantDeferredTarget") : deferredBox;
        var deferredField = new FieldDefinition("Flag", FieldAttributes.Public | FieldAttributes.Static, lm.TypeSystem.Int32);
        deferredOwner.Fields.Add(deferredField);
        var deferredInitializer = Method(deferredOwner, ".cctor", voidType);
        deferredInitializer.Body.Instructions.Insert(0, Instruction.Create(OpCodes.Ldc_I4_1));
        deferredInitializer.Body.Instructions.Insert(1, Instruction.Create(OpCodes.Stsfld, initialized));
        var deferredEquals = Method(deferredBox, "Equals", lm.TypeSystem.Boolean);
        deferredEquals.Attributes = MethodAttributes.Public | MethodAttributes.Virtual;
        deferredEquals.Parameters.Add(new ParameterDefinition(lm.TypeSystem.Object));
        deferredEquals.Body.Instructions.Insert(0, Instruction.Create(OpCodes.Ldsfld, deferredField));
        deferredEquals.Body.Instructions.Insert(1, Instruction.Create(OpCodes.Pop));
        deferredEquals.Body.Instructions.Insert(2, Instruction.Create(OpCodes.Ldc_I4_0));
        foreach (string target in new[] { "SignatureTarget", "BodyTarget" })
        {
            var initializer = Method(targets[target], ".cctor", lm.ImportReference(voidType));
            initializer.Body.Instructions.Insert(0, Instruction.Create(OpCodes.Ldc_I4_1));
            initializer.Body.Instructions.Insert(1, Instruction.Create(OpCodes.Stsfld, initialized));
        }
        var bodyMarker = targets["BodyTarget"].Methods.Single(m => m.Name == "Marker");
        bodyMarker.Body.Instructions.Insert(0, Instruction.Create(OpCodes.Ldsfld, initialized));
        bodyMarker.Body.Instructions.Insert(1, Instruction.Create(OpCodes.Pop));
        var contract = Type("IContract");
        contract.Attributes = TypeAttributes.Public | TypeAttributes.Interface | TypeAttributes.Abstract;
        contract.BaseType = null;
        var slot = new MethodDefinition("Slot", MethodAttributes.Public | MethodAttributes.Virtual
            | MethodAttributes.NewSlot | MethodAttributes.Abstract, lm.ImportReference(voidType));
        contract.Methods.Add(slot);
        var used = Type("Used", targets["BaseTarget"]);
        used.Interfaces.Add(new InterfaceImplementation(contract));
        var impl = Method(used, "Slot", lm.ImportReference(voidType));
        impl.Attributes = MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.NewSlot | MethodAttributes.Final;
        Method(used, ".ctor", lm.ImportReference(voidType), constructor: true);
        used.Fields.Add(new FieldDefinition("PrivateField", FieldAttributes.Private, targets["FieldTarget"]));
        used.Fields.Add(new FieldDefinition("Framework", FieldAttributes.Private, lm.ImportReference(coreWitness)));
        used.Fields.Add(new FieldDefinition("DormantBox", FieldAttributes.Private, deferredBox));
        var get = Method(used, "get_Value", targets["PropertyTarget"]);
        get.Attributes |= MethodAttributes.SpecialName;
        get.Body.Instructions.Insert(0, Instruction.Create(OpCodes.Ldnull));
        var set = Method(used, "set_Value", lm.ImportReference(voidType));
        set.Attributes |= MethodAttributes.SpecialName;
        set.Parameters.Add(new ParameterDefinition(targets["PropertyTarget"]));
        used.Properties.Add(new PropertyDefinition("Value", PropertyAttributes.None, targets["PropertyTarget"])
            { GetMethod = get, SetMethod = set });
        var add = Method(used, "add_Changed", lm.ImportReference(voidType));
        add.Attributes |= MethodAttributes.SpecialName;
        add.Parameters.Add(new ParameterDefinition(targets["EventTarget"]));
        var remove = Method(used, "remove_Changed", lm.ImportReference(voidType));
        remove.Attributes |= MethodAttributes.SpecialName;
        remove.Parameters.Add(new ParameterDefinition(targets["EventTarget"]));
        used.Events.Add(new EventDefinition("Changed", EventAttributes.None, targets["EventTarget"])
            { AddMethod = add, RemoveMethod = remove });
        var box = Type("Box`1");
        var parameter = new GenericParameter("T", box);
        box.GenericParameters.Add(parameter);
        box.Fields.Add(new FieldDefinition("Value", FieldAttributes.Private, parameter));
        Method(box, ".ctor", lm.ImportReference(voidType), constructor: true);
        Method(box, "Marker", lm.ImportReference(voidType));
        var closed = new GenericInstanceType(box);
        closed.GenericArguments.Add(targets["ArgumentTarget"]);
        var signature = Method(used, "Signature", lm.ImportReference(voidType));
        signature.Parameters.Add(new ParameterDefinition(targets["SignatureTarget"]));
        signature.Parameters.Add(new ParameterDefinition(closed));
        var follow = Method(used, "Follow", lm.ImportReference(voidType));
        follow.Body.Instructions.Insert(0, Instruction.Create(OpCodes.Call, targets["BodyTarget"].Methods.Single(m => m.Name == "Marker")));
        if (retainedEquality)
        {
            follow.Body.Instructions.Insert(0, Instruction.Create(OpCodes.Ldnull));
            follow.Body.Instructions.Insert(1, Instruction.Create(OpCodes.Ldnull));
            follow.Body.Instructions.Insert(2, Instruction.Create(OpCodes.Call, lm.ImportReference(staticEquals)));
            follow.Body.Instructions.Insert(3, Instruction.Create(OpCodes.Pop));
            follow.Body.Instructions.Insert(4, Instruction.Create(OpCodes.Ldnull));
            follow.Body.Instructions.Insert(5, Instruction.Create(OpCodes.Ldnull));
            follow.Body.Instructions.Insert(6, Instruction.Create(OpCodes.Call, lm.ImportReference(arrayIndexOf!)));
            follow.Body.Instructions.Insert(7, Instruction.Create(OpCodes.Pop));
        }
        var generic = Method(used, "Generic", lm.ImportReference(voidType));
        var constrained = new GenericParameter("T", generic);
        constrained.Constraints.Add(new GenericParameterConstraint(targets["ConstraintTarget"]));
        generic.GenericParameters.Add(constrained);
        var factory = Type("Factory`1");
        factory.GenericParameters.Add(new GenericParameter("T", factory));
        var build = Method(factory, "Build", lm.ImportReference(voidType));
        var methodArgument = new GenericParameter("U", build);
        build.GenericParameters.Add(methodArgument);
        var resultBox = new GenericInstanceType(box);
        resultBox.GenericArguments.Add(methodArgument);
        build.ReturnType = resultBox;
        build.Body.Instructions.Insert(0, Instruction.Create(OpCodes.Ldnull));
        var holder = Type("Holder`1");
        holder.GenericParameters.Add(new GenericParameter("T", holder));
        Method(holder, ".ctor", voidType, constructor: true);
        var choose = Method(holder, "Choose", voidType);
        var chosenParameter = new GenericParameter("U", choose);
        choose.GenericParameters.Add(chosenParameter);
        var chosenBox = new GenericInstanceType(box);
        chosenBox.GenericArguments.Add(chosenParameter);
        choose.ReturnType = chosenBox;
        choose.Body.Instructions.Insert(0, Instruction.Create(OpCodes.Ldnull));
        var caller = Method(used, "Caller", voidType);
        var callerParameter = new GenericParameter("V", caller);
        caller.GenericParameters.Add(callerParameter);
        var holderInstance = new GenericInstanceType(holder);
        holderInstance.GenericArguments.Add(callerParameter);
        var chooseReference = new MethodReference("Choose", voidType, holderInstance) { HasThis = false };
        var chooseParameter = new GenericParameter("U", chooseReference);
        chooseReference.GenericParameters.Add(chooseParameter);
        var chooseBox = new GenericInstanceType(box);
        chooseBox.GenericArguments.Add(chooseParameter);
        chooseReference.ReturnType = chooseBox;
        var chooseInstance = new GenericInstanceMethod(chooseReference);
        chooseInstance.GenericArguments.Add(targets["CalleeArgumentTarget"]);
        caller.Body.Instructions.Insert(0, Instruction.Create(OpCodes.Call, chooseInstance));
        caller.Body.Instructions.Insert(1, Instruction.Create(OpCodes.Pop));
        var attribute = Type("MarkerAttribute", lm.ImportReference(attr));
        attribute.Fields.Add(new FieldDefinition("Target", FieldAttributes.Private, targets["AttributeTarget"]));
        var attributeCtor = Method(attribute, ".ctor", lm.ImportReference(voidType), constructor: true);
        attributeCtor.Parameters.Add(new ParameterDefinition(lm.ImportReference(typeType)));
        var annotation = new CustomAttribute(attributeCtor);
        annotation.ConstructorArguments.Add(new CustomAttributeArgument(lm.ImportReference(typeType), targets["AttributeArgumentTarget"]));
        used.CustomAttributes.Add(annotation);
        var unused = Type("Unreferenced");
        Method(unused, "MustStayAbsent", lm.ImportReference(voidType));
        var metadata = Type("MetadataOnly");
        Method(metadata, "MustStayAbsent", lm.ImportReference(voidType));
        var program = new TypeDefinition("MediumFixture", "Program", TypeAttributes.Public | TypeAttributes.Class,
            am.ImportReference(obj));
        am.Types.Add(program);
        var main = Method(program, "Main", am.ImportReference(voidType));
        main.ReturnType = am.TypeSystem.Int32;
        main.Attributes = MethodAttributes.Public | MethodAttributes.Static;
        main.Body.Instructions.Insert(0, Instruction.Create(OpCodes.Ldtoken, am.ImportReference(used)));
        main.Body.Instructions.Insert(1, Instruction.Create(OpCodes.Pop));
        var factoryInstance = new GenericInstanceType(factory);
        factoryInstance.GenericArguments.Add(targets["BaseTarget"]);
        var buildReference = am.ImportReference(build);
        buildReference.DeclaringType = am.ImportReference(factoryInstance);
        var buildInstance = new GenericInstanceMethod(buildReference);
        buildInstance.GenericArguments.Add(am.ImportReference(targets["MethodArgumentTarget"]));
        main.Body.Instructions.Insert(2, Instruction.Create(OpCodes.Call, buildInstance));
        main.Body.Instructions.Insert(3, Instruction.Create(OpCodes.Pop));
        var callerInstance = new GenericInstanceMethod(am.ImportReference(caller));
        callerInstance.GenericArguments.Add(am.ImportReference(targets["CallerArgumentTarget"]));
        main.Body.Instructions.Insert(4, Instruction.Create(OpCodes.Call, callerInstance));
        main.Body.Instructions.Insert(5, Instruction.Create(OpCodes.Ldtoken, am.ImportReference(refLike)));
        main.Body.Instructions.Insert(6, Instruction.Create(OpCodes.Pop));
        main.Body.Instructions.Insert(7, Instruction.Create(OpCodes.Ldtoken, am.ImportReference(inner)));
        main.Body.Instructions.Insert(8, Instruction.Create(OpCodes.Pop));
        main.Body.Instructions.Insert(9, Instruction.Create(OpCodes.Ldtoken, am.ImportReference(open)));
        main.Body.Instructions.Insert(10, Instruction.Create(OpCodes.Pop));
        main.Body.Instructions.Insert(11, Instruction.Create(OpCodes.Ldsfld, am.ImportReference(initialized)));
        if (retainedEquality)
        {
            main.Body.InitLocals = true;
            main.Body.Variables.Add(new VariableDefinition(am.ImportReference(deferredBox)));
            main.Body.Instructions.Insert(0, Instruction.Create(OpCodes.Ldloc_0));
            main.Body.Instructions.Insert(1, Instruction.Create(OpCodes.Box, am.ImportReference(deferredBox)));
            main.Body.Instructions.Insert(2, Instruction.Create(OpCodes.Pop));
        }
        else
        {
            main.Body.Instructions.Insert(0, Instruction.Create(OpCodes.Ldnull));
            main.Body.Instructions.Insert(1, Instruction.Create(OpCodes.Ldnull));
            main.Body.Instructions.Insert(2, Instruction.Create(OpCodes.Call, am.ImportReference(staticEquals)));
            main.Body.Instructions.Insert(3, Instruction.Create(OpCodes.Pop));
            AddInitializerTrace(cm, lm, am, obj, value, used, main);
        }
        am.EntryPoint = main;
        // Module initializers fill the startup pass; main runs the entry type's guard.
        Method(lm.Types.Single(t => t.Name == "<Module>"), ".cctor", voidType);
        Method(am.Types.Single(t => t.Name == "<Module>"), ".cctor", am.ImportReference(voidType));
        Method(program, ".cctor", am.ImportReference(voidType));
        var writer = new WriterParameters { Timestamp = 0, DeterministicMvid = true };
        core.Write(Path.Combine(directory, "System.Private.CoreLib.dll"), writer);
        library.Write(Path.Combine(directory, "MediumLib.dll"), writer);
        app.Write(Path.Combine(directory, "MediumApp.dll"), writer);
        File.WriteAllText(Path.Combine(directory, "link.xml"),
            "<linker><assembly fullname=\"MediumLib\"><type fullname=\"MediumFixture.PreservedOnly\" preserve=\"nothing\">"
            + "<field name=\"Selected\" /></type></assembly></linker>\n");
    }

    static MethodDefinition Method(TypeDefinition type, string name, TypeReference result, bool constructor = false)
    {
        var flags = MethodAttributes.Private | MethodAttributes.HideBySig;
        if (!constructor) flags |= MethodAttributes.Static;
        if (constructor || name == ".cctor") flags |= MethodAttributes.SpecialName | MethodAttributes.RTSpecialName;
        var method = new MethodDefinition(name, flags, result);
        type.Methods.Add(method);
        method.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
        return method;
    }

    // One precise-init type per trigger path: allocation, virtual and generic virtual
    // dispatch to an override that reads a static, a boxed struct's Equals and
    // GetHashCode, and a static read. Executed code enters them only through
    // TraceEntry's boxed Equals, an edge reachability adds after its drain; Main's
    // tokens make Medium retention reach every trigger body, override and initializer
    // first. Used's signature also retains each trigger owner, which keeps its uncalled
    // Dormant method. Every path prints the initializer count before and after its
    // trigger, the initializer prints its own line, and a dispatched path prints the
    // override's result.
    static void AddInitializerTrace(ModuleDefinition cm, ModuleDefinition lm, ModuleDefinition am,
        TypeDefinition obj, TypeDefinition value, TypeDefinition used, MethodDefinition main)
    {
        var voidType = lm.TypeSystem.Void;
        var int32 = lm.TypeSystem.Int32;
        var text = new TypeDefinition("System", "String", TypeAttributes.Public | TypeAttributes.Sealed, obj);
        cm.Types.Add(text);
        var console = new TypeDefinition("System", "Console",
            TypeAttributes.Public | TypeAttributes.Abstract | TypeAttributes.Sealed, obj);
        cm.Types.Add(console);
        MethodDefinition Output(string name, TypeReference parameter)
        {
            var method = Method(console, name, voidType);
            method.Attributes = MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.HideBySig;
            method.Parameters.Add(new ParameterDefinition(parameter));
            return method;
        }
        var write = Output("Write", text);
        var writeLine = Output("WriteLine", text);
        var writeNumber = Output("WriteLine", int32);
        var objectHash = Method(obj, "GetHashCode", int32);
        objectHash.Attributes = MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.NewSlot;
        objectHash.Body.Instructions.Insert(0, Instruction.Create(OpCodes.Ldc_I4_0));
        var objectEquals = obj.Methods.Single(m => m.Name == "Equals" && !m.IsStatic);

        TypeDefinition Type(string name, TypeReference parent)
        {
            var type = new TypeDefinition("MediumFixture", name, TypeAttributes.Public | TypeAttributes.Class, parent);
            lm.Types.Add(type);
            return type;
        }
        ILProcessor Body(MethodDefinition method)
        {
            method.Body.Instructions.Clear();
            return method.Body.GetILProcessor();
        }
        MethodDefinition Instance(TypeDefinition owner, string name, TypeReference result, bool newSlot)
        {
            var method = Method(owner, name, result);
            method.Attributes = MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.HideBySig
                | (newSlot ? MethodAttributes.NewSlot : 0);
            return method;
        }
        var counter = Type("InitializerTrace", lm.ImportReference(obj));
        var count = new FieldDefinition("Count", FieldAttributes.Public | FieldAttributes.Static, int32);
        counter.Fields.Add(count);
        // A type without beforefieldinit whose initializer prints its path, counts
        // itself and stores the new count in Value.
        (TypeDefinition Owner, FieldDefinition Value) Trigger(string name, string path, bool valueType)
        {
            var owner = Type(name, lm.ImportReference(valueType ? value : obj));
            if (valueType) owner.Attributes |= TypeAttributes.Sealed | TypeAttributes.SequentialLayout;
            var field = new FieldDefinition("Value", FieldAttributes.Public | FieldAttributes.Static, int32);
            owner.Fields.Add(field);
            var il = Body(Method(owner, ".cctor", voidType));
            il.Emit(OpCodes.Ldstr, path + " initializer");
            il.Emit(OpCodes.Call, lm.ImportReference(writeLine));
            il.Emit(OpCodes.Ldsfld, count);
            il.Emit(OpCodes.Ldc_I4_1);
            il.Emit(OpCodes.Add);
            il.Emit(OpCodes.Dup);
            il.Emit(OpCodes.Stsfld, count);
            il.Emit(OpCodes.Stsfld, field);
            il.Emit(OpCodes.Ret);
            Method(owner, "Dormant", int32).Body.Instructions.Insert(0, Instruction.Create(OpCodes.Ldsfld, field));
            return (owner, field);
        }
        // A base slot returning zero and an override returning the trigger's Value.
        (MethodDefinition Slot, MethodDefinition Constructor) Dispatch(string name, FieldDefinition read, bool generic)
        {
            var reader = Type(name + "Reader", lm.ImportReference(obj));
            var derived = Type(name + "Override", reader);
            var constructor = Method(derived, ".ctor", voidType, constructor: true);
            var slot = Instance(reader, "Read", int32, newSlot: true);
            slot.Body.Instructions.Insert(0, Instruction.Create(OpCodes.Ldc_I4_0));
            var target = Instance(derived, "Read", int32, newSlot: false);
            if (generic)
            {
                slot.GenericParameters.Add(new GenericParameter("T", slot));
                target.GenericParameters.Add(new GenericParameter("T", target));
            }
            target.Body.Instructions.Insert(0, Instruction.Create(OpCodes.Ldsfld, read));
            return (slot, constructor);
        }

        var allocation = Trigger("AllocationTrigger", "allocation", valueType: false);
        var allocate = Method(allocation.Owner, ".ctor", voidType, constructor: true);
        var virtualRead = Trigger("VirtualTrigger", "virtual", valueType: false);
        var virtualDispatch = Dispatch("Virtual", virtualRead.Value, generic: false);
        var genericRead = Trigger("GenericVirtualTrigger", "generic-virtual", valueType: false);
        var genericDispatch = Dispatch("GenericVirtual", genericRead.Value, generic: true);
        var equality = Trigger("EqualityTrigger", "boxed-equals", valueType: true);
        var equals = Instance(equality.Owner, "Equals", lm.TypeSystem.Boolean, newSlot: false);
        equals.Parameters.Add(new ParameterDefinition(lm.ImportReference(obj)));
        equals.Body.Instructions.Insert(0, Instruction.Create(OpCodes.Ldc_I4_1));
        var hash = Trigger("HashTrigger", "boxed-hash", valueType: true);
        Instance(hash.Owner, "GetHashCode", int32, newSlot: false)
            .Body.Instructions.Insert(0, Instruction.Create(OpCodes.Ldsfld, hash.Value));
        var staticRead = Trigger("StaticReadTrigger", "static-read", valueType: false);
        var retains = Method(used, "Retains", voidType);
        foreach (var owner in new[] { allocation.Owner, virtualRead.Owner, genericRead.Owner, equality.Owner,
                     hash.Owner, staticRead.Owner })
            retains.Parameters.Add(new ParameterDefinition(owner));

        var traceEntry = Type("TraceEntry", lm.ImportReference(value));
        traceEntry.Attributes |= TypeAttributes.Sealed | TypeAttributes.SequentialLayout;
        var steps = Instance(traceEntry, "Equals", lm.TypeSystem.Boolean, newSlot: false);
        steps.Parameters.Add(new ParameterDefinition(lm.ImportReference(obj)));
        steps.Body.InitLocals = true;
        var equalityLocal = new VariableDefinition(equality.Owner);
        var hashLocal = new VariableDefinition(hash.Owner);
        var result = new VariableDefinition(int32);
        foreach (var local in new[] { equalityLocal, hashLocal, result })
            steps.Body.Variables.Add(local);
        var il = Body(steps);
        void Emit(OpCode opcode, object? operand = null)
        {
            il.Append(operand switch
            {
                null => Instruction.Create(opcode),
                string literal => Instruction.Create(opcode, literal),
                MethodReference method => Instruction.Create(opcode, lm.ImportReference(method)),
                FieldReference field => Instruction.Create(opcode, lm.ImportReference(field)),
                TypeReference type => Instruction.Create(opcode, lm.ImportReference(type)),
                VariableDefinition local => Instruction.Create(opcode, local),
                _ => throw new ArgumentException("unsupported operand", nameof(operand)),
            });
        }
        void Number(string label, Action load)
        {
            Emit(OpCodes.Ldstr, label);
            Emit(OpCodes.Call, write);
            load();
            Emit(OpCodes.Call, writeNumber);
        }
        void Observe(string path, Action trigger, bool printsValue)
        {
            Number(path + " before=", () => Emit(OpCodes.Ldsfld, count));
            trigger();
            if (printsValue) Emit(OpCodes.Stloc, result);
            else Emit(OpCodes.Pop);
            Number(path + " after=", () => Emit(OpCodes.Ldsfld, count));
            if (printsValue) Number(path + " value=", () => Emit(OpCodes.Ldloc, result));
        }
        Observe("allocation", () => Emit(OpCodes.Newobj, allocate), printsValue: false);
        Observe("virtual", () =>
        {
            Emit(OpCodes.Newobj, virtualDispatch.Constructor);
            Emit(OpCodes.Callvirt, virtualDispatch.Slot);
        }, printsValue: true);
        Observe("generic-virtual", () =>
        {
            Emit(OpCodes.Newobj, genericDispatch.Constructor);
            var instance = new GenericInstanceMethod(genericDispatch.Slot);
            instance.GenericArguments.Add(int32);
            Emit(OpCodes.Callvirt, instance);
        }, printsValue: true);
        Observe("boxed-equals", () =>
        {
            Emit(OpCodes.Ldloc, equalityLocal);
            Emit(OpCodes.Box, equality.Owner);
            Emit(OpCodes.Ldnull);
            Emit(OpCodes.Callvirt, objectEquals);
        }, printsValue: true);
        Observe("boxed-hash", () =>
        {
            Emit(OpCodes.Ldloc, hashLocal);
            Emit(OpCodes.Box, hash.Owner);
            Emit(OpCodes.Callvirt, objectHash);
        }, printsValue: true);
        Observe("static-read", () => Emit(OpCodes.Ldsfld, staticRead.Value), printsValue: true);
        Emit(OpCodes.Ldc_I4_1);
        Emit(OpCodes.Ret);

        main.Body.InitLocals = true;
        var entryLocal = new VariableDefinition(am.ImportReference(traceEntry));
        main.Body.Variables.Add(entryLocal);
        var exit = main.Body.Instructions.Last(i => i.OpCode == OpCodes.Ldsfld);
        var entry = main.Body.GetILProcessor();
        foreach (var type in new[] { allocation.Owner, virtualRead.Owner, virtualDispatch.Slot.DeclaringType,
                     virtualDispatch.Constructor.DeclaringType, genericRead.Owner, genericDispatch.Slot.DeclaringType,
                     genericDispatch.Constructor.DeclaringType, equality.Owner, hash.Owner, staticRead.Owner })
        {
            entry.InsertBefore(exit, Instruction.Create(OpCodes.Ldtoken, am.ImportReference(type)));
            entry.InsertBefore(exit, Instruction.Create(OpCodes.Pop));
        }
        entry.InsertBefore(exit, Instruction.Create(OpCodes.Ldloc, entryLocal));
        entry.InsertBefore(exit, Instruction.Create(OpCodes.Box, am.ImportReference(traceEntry)));
        entry.InsertBefore(exit, Instruction.Create(OpCodes.Ldnull));
        entry.InsertBefore(exit, Instruction.Create(OpCodes.Callvirt, am.ImportReference(objectEquals)));
        entry.InsertBefore(exit, Instruction.Create(OpCodes.Pop));
    }

    static void CreateForwardedAttribute(string original, string directory)
    {
        Directory.CreateDirectory(directory);
        var resolver = new DefaultAssemblyResolver();
        resolver.AddSearchDirectory(original);
        var read = new ReaderParameters { InMemory = true, AssemblyResolver = resolver };
        using var core = AssemblyDefinition.ReadAssembly(Path.Combine(original, "System.Private.CoreLib.dll"), read);
        using var library = AssemblyDefinition.ReadAssembly(Path.Combine(original, "MediumLib.dll"), read);
        using var facade = AssemblyDefinition.CreateAssembly(new AssemblyNameDefinition("MediumFacade", new Version(1, 0)),
            "MediumFacade", ModuleKind.Dll);
        var cm = core.MainModule;
        var lm = library.MainModule;
        var enumBase = new TypeDefinition("System", "Enum", TypeAttributes.Public, cm.GetType("System.ValueType"));
        cm.Types.Add(enumBase);
        cm.Types.Add(new TypeDefinition("System", "Int64", TypeAttributes.Public, cm.GetType("System.ValueType")));
        var flags = new TypeDefinition("MediumFixture", "WideFlags", TypeAttributes.Public | TypeAttributes.Sealed,
            lm.ImportReference(enumBase));
        flags.Fields.Add(new FieldDefinition("value__", FieldAttributes.Public | FieldAttributes.SpecialName
            | FieldAttributes.RTSpecialName, lm.TypeSystem.Int64));
        flags.Fields.Add(new FieldDefinition("Large", FieldAttributes.Public | FieldAttributes.Static | FieldAttributes.Literal,
            flags) { Constant = 0x100000002L });
        lm.Types.Add(flags);
        var target = new TypeDefinition("MediumFixture", "ForwardedArgumentTarget", TypeAttributes.Public,
            lm.ImportReference(cm.GetType("System.Object")));
        lm.Types.Add(target);
        Method(target, "Marker", lm.TypeSystem.Void);
        var attribute = new TypeDefinition("MediumFixture", "ForwardedAttribute", TypeAttributes.Public,
            lm.ImportReference(cm.GetType("System.Attribute")));
        lm.Types.Add(attribute);
        var constructor = Method(attribute, ".ctor", lm.TypeSystem.Void, constructor: true);
        attribute.Fields.Add(new FieldDefinition("Flags", FieldAttributes.Public, flags));
        attribute.Fields.Add(new FieldDefinition("Target", FieldAttributes.Public, lm.ImportReference(cm.GetType("System.Type"))));
        attribute.Fields.Add(new FieldDefinition("Sentinel", FieldAttributes.Public, lm.TypeSystem.Int32));
        var destination = new AssemblyNameReference("MediumLib", new Version(1, 0));
        facade.MainModule.AssemblyReferences.Add(destination);
        facade.MainModule.ExportedTypes.Add(new ExportedType(flags.Namespace, flags.Name, facade.MainModule, destination)
            { Attributes = TypeAttributes.Public, IsForwarder = true });
        lm.AssemblyReferences.Add(new AssemblyNameReference("MediumFacade", new Version(1, 0)));
        using var bytes = new MemoryStream();
        using (var writer = new BinaryWriter(bytes, System.Text.Encoding.UTF8, leaveOpen: true))
        {
            void Text(string text)
            {
                byte[] encoded = System.Text.Encoding.UTF8.GetBytes(text);
                Require(encoded.Length < 128, "fixture serialized string requires a multibyte length");
                writer.Write((byte)encoded.Length);
                writer.Write(encoded);
            }
            writer.Write((ushort)1);
            writer.Write((ushort)3);
            writer.Write((byte)0x53); writer.Write((byte)0x55);
            Text("MediumFixture.WideFlags, MediumFacade"); Text("Flags"); writer.Write(0x100000002L);
            writer.Write((byte)0x53); writer.Write((byte)0x50);
            Text("Target"); Text("MediumFixture.ForwardedArgumentTarget, MediumLib");
            writer.Write((byte)0x53); writer.Write((byte)0x08);
            Text("Sentinel"); writer.Write(73);
        }
        lm.GetType("MediumFixture.Used").Methods.Single(m => m.Name == "Follow")
            .CustomAttributes.Add(new CustomAttribute(constructor, bytes.ToArray()));
        var output = new WriterParameters { Timestamp = 0, DeterministicMvid = true };
        core.Write(Path.Combine(directory, "System.Private.CoreLib.dll"), output);
        library.Write(Path.Combine(directory, "MediumLib.dll"), output);
        facade.Write(Path.Combine(directory, "MediumFacade.dll"), output);
        File.Copy(Path.Combine(original, "MediumApp.dll"), Path.Combine(directory, "MediumApp.dll"), overwrite: true);
        File.Copy(Path.Combine(original, "link.xml"), Path.Combine(directory, "link.xml"), overwrite: true);
    }

    static void Check(string directory, bool medium, bool metadataRoot)
    {
        using var library = AssemblyDefinition.ReadAssembly(Path.Combine(directory, "MediumLib.dll"));
        var mod = library.MainModule;
        bool Body(string type, string method) => mod.GetType("MediumFixture." + type)?.Methods
            .Any(m => m.Name == method && m.HasBody && m.Body.Instructions.Last().OpCode.Code == Code.Ret) == true;
        Require(Body("Used", ".ctor") == medium, "wrong private constructor retention");
        Require(Body("PreservedOnly", ".ctor") == medium && Body("PreservedOnly", "Marker") == medium,
            "field-only preservation did not activate type-wide policy");
        Require(Body("RefLikeTarget", "Marker") == medium, "byref-like type token did not retain its methods");
        Require(Body("Outer/Inner", "Marker") == medium, "nested type body retention changed");
        Require(!Body("Outer", "MustStayAbsent"), "nested type widened its enclosing owner's bodies");
        Require(Body("Open`1", "Uncalled") == medium, "open generic definition lost its original body");
        foreach (string name in new[] { "get_Value", "set_Value", "add_Changed", "remove_Changed", "Follow", "Signature", "Generic" })
            Require(Body("Used", name) == medium, "wrong selected body: " + name);
        Require(Body("Used", "Retains") == medium, "initializer trace owners lost their signature retention");
        foreach (string owner in TraceOwners)
            Require(Body(owner, "Dormant") == medium, "wrong type-wide retention of an initializer trace owner: " + owner);
        if (medium)
        {
            foreach (string target in Targets) Require(Body(target, "Marker"), "lost type closure: " + target);
            Require(Body("Box`1", ".ctor") && Body("Box`1", "Marker"), "lost generic definition members");
            Require(Body("SignatureTarget", ".cctor"), "retained initializer body was stubbed");
        }
        Require(mod.GetType("MediumFixture.Unreferenced") is null, "unreferenced type retained");
        Require((mod.GetType("MediumFixture.MetadataOnly") is not null) == metadataRoot && !Body("MetadataOnly", "MustStayAbsent"),
            "metadata-only root became executable");
    }

    static void CheckCore(string assemblyPath, string directory, bool medium, bool metadataRoot)
    {
        var assembly = System.Reflection.Assembly.LoadFrom(Path.GetFullPath(assemblyPath));
        var optionsType = assembly.GetType("Dn2Cpp.TranspileOptions", throwOnError: true)!;
        object options = Activator.CreateInstance(optionsType)!;
        optionsType.GetProperty("Input")!.SetValue(options, Path.Combine(directory, "MediumApp.dll"));
        optionsType.GetProperty("UseILDiet")!.SetValue(options, false);
        optionsType.GetProperty("SharedGenerics")!.SetValue(options, false);
        optionsType.GetProperty("TrimReflection")!.SetValue(options, true);
        string descriptor = Path.Combine(directory, "link.xml");
        if (!File.Exists(descriptor)) descriptor = Path.Combine(directory, "preservation.xml");
        optionsType.GetProperty("LinkXmlFiles")!.SetValue(options, new[] { descriptor });
        if (metadataRoot) optionsType.GetProperty("ReflectionRoots")!.SetValue(options, new[] { "MediumFixture.MetadataOnly" });
        var level = assembly.GetType("Dn2Cpp.StrippingLevel", throwOnError: true)!;
        optionsType.GetProperty("StrippingLevel")!.SetValue(options, Enum.Parse(level, medium ? "Medium" : "High"));
        var compilationType = assembly.GetType("Dn2Cpp.Compilation", throwOnError: true)!;
        string[] paths = { Path.Combine(directory, "MediumApp.dll"), Path.Combine(directory, "MediumLib.dll"),
            Path.Combine(directory, "System.Private.CoreLib.dll") };
        object compilation;
        try { compilation = compilationType.GetMethod("Create", BindingFlags.Public | BindingFlags.Static)!.Invoke(null, new[] { paths, options })!; }
        catch (System.Reflection.TargetInvocationException e) when (e.InnerException is not null) { throw e.InnerException; }
        object Read(object value, string name) => value.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(value)
            ?? value.GetType().GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(value)!;
        var reachable = ((IEnumerable)Read(compilation, "Reachable")).Cast<object>().ToArray();
        string Owner(object method) => (string)Read(Read(method, "DeclaringClass"), "FullName");
        bool Body(string type, string method) => reachable.Any(m => Owner(m) == type && (string)Read(m, "Name") == method);
        Require(Body("MediumFixture.Used", ".ctor") == medium, "core lost private constructor policy");
        Require(Body("MediumFixture.PreservedOnly", ".ctor") == medium && Body("MediumFixture.PreservedOnly", "Marker") == medium,
            "core field-only preservation did not activate type-wide policy");
        Require(Body("MediumFixture.RefLikeTarget", "Marker") == medium, "core lost byref-like type token");
        Require(Body("Inner", "Marker") == medium, "core lost nested type token");
        Require(!Body("MediumFixture.Outer", "MustStayAbsent"), "core widened enclosing owner's bodies");
        foreach (string name in new[] { "get_Value", "set_Value", "add_Changed", "remove_Changed", "Follow", "Signature" })
            Require(Body("MediumFixture.Used", name) == medium, "core body policy mismatch: " + name);
        Require(!reachable.Any(m => Owner(m) is "MediumFixture.Unreferenced" or "MediumFixture.MetadataOnly"),
            "core metadata discovery kept unrelated bodies");
        bool retainedEquality = reachable.Any(m => Owner(m) == "MediumFixture.DormantDeferredTarget");
        if (!retainedEquality)
            foreach (string owner in TraceOwners)
                Require(Body("MediumFixture." + owner, "Dormant") == medium,
                    "core type-wide retention mismatch for an initializer trace owner: " + owner);
        if (!medium) return;
        foreach (string target in Targets) Require(Body("MediumFixture." + target, "Marker"), "core lost closure: " + target);
        Require(Body("System.MediumWitness", "FrameworkPrivate"), "core excluded a framework type");
        Require(reachable.Any(m => Owner(m).StartsWith("MediumFixture.Box_", StringComparison.Ordinal)
            && (string)Read(m, "Name") == ".ctor"), "core lost known closed generic constructor");
        Require(reachable.Any(m => Owner(m).StartsWith("MediumFixture.Box_", StringComparison.Ordinal)
            && Owner(m).Contains("MethodArgumentTarget", StringComparison.Ordinal) && (string)Read(m, "Name") == ".ctor"),
            "core lost combined type and method generic return signature");
        Require(reachable.Any(m => Owner(m).StartsWith("MediumFixture.Holder_", StringComparison.Ordinal)
            && Owner(m).Contains("CallerArgumentTarget", StringComparison.Ordinal) && (string)Read(m, "Name") == ".ctor"),
            "callee declaring type lost the caller's generic context");
        Require(!reachable.Any(m => Owner(m).StartsWith("MediumFixture.Holder_", StringComparison.Ordinal)
            && Owner(m).Contains("CalleeArgumentTarget", StringComparison.Ordinal)),
            "callee method arguments replaced the declaring type's caller context");
        Require(!reachable.Any(m => Owner(m).StartsWith("MediumFixture.Open_", StringComparison.Ordinal)),
            "an open generic body invented an executable specialization");
        string deferredTarget = retainedEquality ? "DormantDeferredTarget" : "DeferredBox";
        foreach (string target in new[] { "SignatureTarget", "BodyTarget", deferredTarget })
            Require(Body("MediumFixture." + target, ".cctor"), "retention lost a static initializer: " + target);
    }

    // A private Buffer reference move whose count is int32 rather than nuint. The
    // collector-aware move lowering accepts only the CoreLib byte-count contract.
    static void CreateBufferMoveShape(string original, string directory)
    {
        CreateRetainedIntercept(original, directory, "retained");
        var resolver = new DefaultAssemblyResolver();
        resolver.AddSearchDirectory(directory);
        var read = new ReaderParameters { InMemory = true, AssemblyResolver = resolver };
        using var core = AssemblyDefinition.ReadAssembly(Path.Combine(directory, "System.Private.CoreLib.dll"), read);
        using var app = AssemblyDefinition.ReadAssembly(Path.Combine(directory, "MediumApp.dll"), read);
        var cm = core.MainModule;
        var am = app.MainModule;
        var move = new MethodDefinition("BulkMoveWithWriteBarrierInternal", MethodAttributes.Public | MethodAttributes.Static,
            cm.TypeSystem.Void) { ImplAttributes = MethodImplAttributes.InternalCall };
        move.Parameters.Add(new ParameterDefinition(new ByReferenceType(cm.TypeSystem.Byte)));
        move.Parameters.Add(new ParameterDefinition(new ByReferenceType(cm.TypeSystem.Byte)));
        move.Parameters.Add(new ParameterDefinition(cm.TypeSystem.Int32));
        cm.GetType("System.Buffer").Methods.Add(move);
        var main = am.EntryPoint;
        // Keep the culture pinning and type-touch prefix; replace the final `ldc.i4.0; ret`.
        main.Body.Instructions.RemoveAt(main.Body.Instructions.Count - 1);
        main.Body.Instructions.RemoveAt(main.Body.Instructions.Count - 1);
        var bytes = new VariableDefinition(new ArrayType(am.TypeSystem.Byte));
        main.Body.Variables.Add(bytes);
        var entry = main.Body.GetILProcessor();
        entry.Emit(OpCodes.Ldc_I4, 16);
        entry.Emit(OpCodes.Newarr, am.TypeSystem.Byte);
        entry.Emit(OpCodes.Stloc, bytes);
        foreach (int offset in new[] { 8, 0 })
        {
            entry.Emit(OpCodes.Ldloc, bytes);
            entry.Emit(OpCodes.Ldc_I4, offset);
            entry.Emit(OpCodes.Ldelema, am.TypeSystem.Byte);
        }
        entry.Emit(OpCodes.Ldc_I4_4);
        entry.Emit(OpCodes.Call, am.ImportReference(move));
        entry.Emit(OpCodes.Ldc_I4_0);
        entry.Emit(OpCodes.Ret);
        var writer = new WriterParameters { Timestamp = 0, DeterministicMvid = true };
        core.Write(Path.Combine(directory, "System.Private.CoreLib.dll"), writer);
        app.Write(Path.Combine(directory, "MediumApp.dll"), writer);
    }

    // CoreLib owners of an ICU PAL import and a CoreCLR QCall import, each reached by a
    // direct call and an address-taken call. The entry only touches the owners, so the
    // imports are retained by Medium and never called.
    static void CreateUnavailableImports(string original, string directory)
    {
        Directory.CreateDirectory(directory);
        var resolver = new DefaultAssemblyResolver();
        resolver.AddSearchDirectory(original);
        var read = new ReaderParameters { InMemory = true, AssemblyResolver = resolver };
        using var core = AssemblyDefinition.ReadAssembly(Path.Combine(original, "System.Private.CoreLib.dll"), read);
        using var app = AssemblyDefinition.ReadAssembly(Path.Combine(original, "MediumApp.dll"), read);
        var cm = core.MainModule;
        var am = app.MainModule;
        var obj = cm.GetType("System.Object");
        var value = cm.GetType("System.ValueType");
        cm.Types.Add(new TypeDefinition("System", "String", TypeAttributes.Public | TypeAttributes.Sealed, obj));
        cm.Types.Add(new TypeDefinition("System", "UInt16", TypeAttributes.Public | TypeAttributes.Sealed, value));
        var exception = new TypeDefinition("System", "Exception", TypeAttributes.Public, obj);
        cm.Types.Add(exception);
        cm.Types.Add(new TypeDefinition("System", "PlatformNotSupportedException", TypeAttributes.Public, exception));
        var interop = new TypeDefinition("", "Interop", TypeAttributes.Public | TypeAttributes.Abstract | TypeAttributes.Sealed, obj);
        cm.Types.Add(interop);
        var pointer = new PointerType(am.TypeSystem.UInt16);
        var main = am.EntryPoint;
        main.ReturnType = am.TypeSystem.Int32;
        main.Body = new MethodBody(main);
        var entry = main.Body.GetILProcessor();
        foreach (var (owner, library, prefix, name) in new[]
            { ("Globalization", "libSystem.Globalization.Native", "GlobalizationNative_", "GetCalendars"), ("Kernel32", "QCall", "", "CloseHandle") })
        {
            var type = new TypeDefinition("", owner, TypeAttributes.NestedPublic | TypeAttributes.Abstract | TypeAttributes.Sealed, obj);
            interop.NestedTypes.Add(type);
            var native = new ModuleReference(library);
            cm.ModuleReferences.Add(native);
            var import = new MethodDefinition(name, MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.PInvokeImpl,
                am.TypeSystem.Int32)
            {
                ImplAttributes = MethodImplAttributes.PreserveSig,
                PInvokeInfo = new PInvokeInfo(PInvokeAttributes.CallConvCdecl | PInvokeAttributes.CharSetUnicode, prefix + name, native),
            };
            import.Parameters.Add(new ParameterDefinition(pointer));
            type.Methods.Add(import);
            foreach (bool address in new[] { false, true })
            {
                var wrapper = Method(type, address ? "AddressImport" : "CallImport", am.TypeSystem.Int32);
                wrapper.Attributes = MethodAttributes.Public | MethodAttributes.Static;
                wrapper.Body.Instructions.Clear();
                var il = wrapper.Body.GetILProcessor();
                il.Emit(OpCodes.Ldc_I4_0);
                il.Emit(OpCodes.Conv_U);
                if (address)
                {
                    il.Emit(OpCodes.Ldftn, import);
                    var site = new CallSite(am.TypeSystem.Int32);
                    site.Parameters.Add(new ParameterDefinition(pointer));
                    il.Emit(OpCodes.Calli, site);
                }
                else il.Emit(OpCodes.Call, import);
                il.Emit(OpCodes.Ret);
            }
            var touch = Method(type, "Touch", am.TypeSystem.Void);
            touch.Attributes = MethodAttributes.Public | MethodAttributes.Static;
            entry.Emit(OpCodes.Call, am.ImportReference(touch));
        }
        entry.Emit(OpCodes.Ldc_I4_0);
        entry.Emit(OpCodes.Ret);
        var writer = new WriterParameters { Timestamp = 0, DeterministicMvid = true };
        core.Write(Path.Combine(directory, "System.Private.CoreLib.dll"), writer);
        app.Write(Path.Combine(directory, "MediumApp.dll"), writer);
        File.Copy(Path.Combine(original, "MediumLib.dll"), Path.Combine(directory, "MediumLib.dll"), true);
    }

    static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}

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

    public static bool Run(string[] args)
    {
        if (args.Length == 4 && args[0] == "--create-stripping-gc-generations-fixture")
        {
            CreateGcGenerations(args[1], args[2], args[3]);
            Console.WriteLine("stripping-fixture=gc-generations:" + args[3]);
            return true;
        }
        if (args.Length == 1 && args[0] == "--check-gc-memory-clr")
        {
            CheckGcMemoryClr();
            return true;
        }
        if (args.Length == 4 && args[0] == "--create-stripping-gc-memory-fixture")
        {
            CreateGcMemory(args[1], args[2], args[3]);
            Console.WriteLine("stripping-fixture=gc-memory:" + args[3]);
            return true;
        }
        if (args.Length == 1 && args[0] == "--check-buffer-moves-clr")
        {
            CheckBufferMovesClr();
            return true;
        }
        if (args.Length == 4 && args[0] == "--create-stripping-buffer-moves-fixture")
        {
            CreateBufferMoves(args[1], args[2], args[3]);
            Console.WriteLine("stripping-fixture=buffer-moves:" + args[3]);
            return true;
        }
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
        if (args.Length == 4 && args[0] == "--create-stripping-globalization-fixture")
        {
            CreateUnavailablePal(args[1], args[2], args[3], kernel32: false);
            Console.WriteLine("stripping-fixture=globalization:" + args[3]);
            return true;
        }
        if (args.Length == 4 && args[0] == "--create-stripping-kernel32-fixture")
        {
            CreateUnavailablePal(args[1], args[2], args[3], kernel32: true);
            Console.WriteLine("stripping-fixture=kernel32:" + args[3]);
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
        }
        am.EntryPoint = main;
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
        string deferredTarget = reachable.Any(m => Owner(m) == "MediumFixture.DormantDeferredTarget")
            ? "DormantDeferredTarget" : "DeferredBox";
        foreach (string target in new[] { "SignatureTarget", "BodyTarget", deferredTarget })
        {
            var initializer = reachable.Single(m => Owner(m) == "MediumFixture." + target && (string)Read(m, "Name") == ".cctor");
            Require((bool)compilationType.GetMethod("IsBindingOnlyInitializer", BindingFlags.NonPublic | BindingFlags.Instance)!
                .Invoke(compilation, new[] { initializer })!, "retention eagerly schedules a static initializer: " + target);
        }
    }

    static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}

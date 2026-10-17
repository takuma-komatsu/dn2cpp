using Mono.Cecil;
using Mono.Cecil.Cil;
using System.Xml.Linq;

static partial class GraphArrayValidation
{
    private static readonly string[] EagerModes =
    {
        "eager-attribute-stripped", "eager-attribute-copied", "eager-attribute-chain",
        "eager-attribute-no-ctor", "eager-attribute-template", "eager-attribute-field", "eager-attribute-signature",
        "eager-array-direct", "eager-array-nested", "eager-array-chain", "eager-attribute-framework", "eager-array-fanout",
        "eager-array-unarmed", "eager-array-template", "eager-array-field", "eager-array-signature",
        "eager-array-methodimpl", "eager-array-methodimpl-absent", "eager-array-methodimpl-candidate", "eager-array-methodimpl-different-name",
        "eager-array-vtable", "eager-array-vtable-newslot", "eager-array-vtable-different-name",
        "eager-preserve-direct", "eager-preserve-chain", "eager-preserve-template",
        "eager-array-intrinsic-shell", "eager-array-vector-shape",
        "eager-array-async-adopted", "eager-array-async-declined",
        "eager-array-async-shared-builder",
        "eager-array-async-option-sensitive",
        "eager-array-async-activation",
    };

    private static bool EagerModelMode(string mode) => mode.StartsWith("eager-", StringComparison.Ordinal);
    private static bool EagerAttribute(string mode) => mode.StartsWith("eager-attribute-", StringComparison.Ordinal);
    private static bool EagerArray(string mode) => mode.StartsWith("eager-array-", StringComparison.Ordinal);
    private static bool EagerPreserve(string mode) => mode.StartsWith("eager-preserve-", StringComparison.Ordinal);
    private static bool EagerAsync(string mode) => mode.StartsWith("eager-array-async-", StringComparison.Ordinal);
    private static bool EagerActivation(string mode) => mode == "eager-array-async-activation";
    private static bool EagerOptionSensitive(string mode) => mode == "eager-array-async-option-sensitive";
    private static bool EagerSharedBuilder(string mode) => mode == "eager-array-async-shared-builder" || EagerOptionSensitive(mode);
    private static bool EagerPolicyMode(string mode) => EagerPreserve(mode);
    private static bool EagerConditionalActive(string mode) => EagerAttribute(mode) && EagerActive(mode)
        || mode == "eager-array-async-adopted" || EagerSharedBuilder(mode);
    private static bool EagerActive(string mode) => mode is "eager-attribute-stripped" or "eager-attribute-copied" or "eager-attribute-chain"
        or "eager-array-direct" or "eager-array-nested" or "eager-array-chain" or "eager-attribute-framework" or "eager-array-fanout"
        or "eager-array-methodimpl" or "eager-array-methodimpl-candidate" or "eager-array-vtable"
        or "eager-preserve-direct" or "eager-preserve-chain" or "eager-array-vector-shape"
        or "eager-array-async-adopted" or "eager-array-async-declined" or "eager-array-async-shared-builder"
        or "eager-array-async-option-sensitive" or "eager-array-async-activation";

    private static void CreateEagerModel(string directory, string corelib, string mode)
    {
        Require(EagerModes.Contains(mode), "unknown eager model mode");
        Directory.CreateDirectory(directory);
        if (EagerAsync(mode)) CreateEagerCore(directory, corelib);
        using var core = AssemblyDefinition.ReadAssembly(EagerAsync(mode) ? Path.Combine(directory, "System.Private.CoreLib.dll") : corelib);
        using var app = AssemblyDefinition.CreateAssembly(new AssemblyNameDefinition("GraphApp", new Version(1, 0)), "GraphApp", ModuleKind.Console);
        using var lib = AssemblyDefinition.CreateAssembly(new AssemblyNameDefinition("GraphLib", new Version(1, 0)), "GraphLib", ModuleKind.Dll);
        using var shim = AssemblyDefinition.CreateAssembly(new AssemblyNameDefinition("DnZlib", new Version(1, 0)), "DnZlib", ModuleKind.Dll);
        using var facade = AssemblyDefinition.CreateAssembly(new AssemblyNameDefinition("System.ArrayFacade", new Version(1, 0)), "System.ArrayFacade", ModuleKind.Dll);
        using var leaf = AssemblyDefinition.CreateAssembly(new AssemblyNameDefinition("System.ArrayLeaf", new Version(1, 0)), "System.ArrayLeaf", ModuleKind.Dll);
        using var second = EagerSharedBuilder(mode)
            ? AssemblyDefinition.CreateAssembly(new AssemblyNameDefinition("GraphTaskB", new Version(1, 0)), "GraphTaskB", ModuleKind.Dll) : null;
        TypeReference Core(ModuleDefinition m, string name) => m.ImportReference(core.MainModule.GetType("System." + name));
        MethodReference Api(ModuleDefinition m, string owner, string name, params string[] parameters) => m.ImportReference(
            core.MainModule.GetType("System." + owner).Methods.Single(x => x.Name == name && !x.HasGenericParameters
                && x.Parameters.Select(p => p.ParameterType.FullName).SequenceEqual(parameters)));
        TypeDefinition Type(ModuleDefinition m, string name, bool generic = false, bool value = false)
        {
            var t = new TypeDefinition("Graph", name, TypeAttributes.Public | (value
                ? TypeAttributes.Sealed | TypeAttributes.SequentialLayout | TypeAttributes.BeforeFieldInit : TypeAttributes.Class),
                Core(m, value ? "ValueType" : "Object"));
            m.Types.Add(t);
            if (generic) t.GenericParameters.Add(new GenericParameter("T", t));
            return t;
        }
        MethodDefinition Method(TypeDefinition t, string name, bool instance = false)
        {
            var m = new MethodDefinition(name, MethodAttributes.Public | MethodAttributes.HideBySig
                | (instance ? 0 : MethodAttributes.Static), Core(t.Module, "Void"));
            if (name is ".ctor" or ".cctor") m.Attributes |= MethodAttributes.SpecialName | MethodAttributes.RTSpecialName;
            t.Methods.Add(m);
            return m;
        }
        void Dependency(MethodDefinition m, string name)
        {
            var dep = Method(Type(lib.MainModule, name), "Touch");
            dep.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
            m.Body.Instructions.Add(Instruction.Create(OpCodes.Call, m.Module.ImportReference(dep)));
        }
        void Base(MethodDefinition m, string name)
        {
            m.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
            m.Body.Instructions.Add(Instruction.Create(OpCodes.Call, Api(m.Module, name, ".ctor")));
        }
        GenericInstanceType Closed(ModuleDefinition m, TypeReference t, TypeReference argument)
        {
            var result = new GenericInstanceType(m.ImportReference(t));
            result.GenericArguments.Add(argument);
            return result;
        }
        void DefaultConstructor(TypeDefinition t)
        {
            var ctor = Method(t, ".ctor", true);
            ctor.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
            ctor.Body.Instructions.Add(Instruction.Create(OpCodes.Call,
                new MethodReference(".ctor", Core(t.Module, "Void"), t.BaseType) { HasThis = true }));
            ctor.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
        }
        bool copiedProvider = mode is "eager-attribute-copied" or "eager-attribute-framework";
        var attrModule = copiedProvider ? shim.MainModule : lib.MainModule;
        var attr = Type(attrModule, "EagerDataAttribute");
        attr.BaseType = Core(attrModule, "Attribute");
        var attrCtor = Method(attr, ".ctor", true);
        attrCtor.Parameters.Add(new ParameterDefinition(Core(attrModule, "Type")));
        Base(attrCtor, "Attribute");
        attrCtor.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
        var attrUnused = Method(attr, "Unused");
        attrUnused.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
        void Attribute(TypeDefinition owner, string target)
        {
            using var stream = new MemoryStream();
            using (var writer = new BinaryWriter(stream, System.Text.Encoding.UTF8, leaveOpen: true))
            {
                writer.Write((ushort)1);
                var bytes = System.Text.Encoding.UTF8.GetBytes("Graph." + target + ", GraphLib");
                Require(bytes.Length < 128, "eager attribute SerString grew");
                writer.Write((byte)bytes.Length);
                writer.Write(bytes);
                writer.Write((ushort)0);
            }
            owner.CustomAttributes.Add(new CustomAttribute(owner.Module.ImportReference(attrCtor), stream.ToArray()));
        }
        foreach (string name in new[] { "EagerConditional", "EagerLazyConditional" })
        {
            var activate = Method(Type(lib.MainModule, name), "Activate");
            Dependency(activate, name + "Dependency");
            activate.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
        }
        TypeDefinition Cell(string name, string dependency, bool lazy = false)
        {
            var t = Type(lib.MainModule, name + "`1", generic: true, value: true);
            t.Fields.Add(new FieldDefinition("Value", FieldAttributes.Public, t.GenericParameters[0]));
            var ctor = Method(t, ".ctor", true);
            Dependency(ctor, dependency);
            ctor.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
            if (lazy) Attribute(t, "EagerLazyConditional");
            return t;
        }
        var cell = Cell("EagerCell", "EagerArrayDependency");
        var lazyField = Cell("EagerLazyFieldCell", "EagerLazyFieldDependency", lazy: true);
        var lazyMethod = Cell("EagerLazyMethodCell", "EagerLazyMethodDependency", lazy: true);
        var provider = Type(attrModule, "EagerProvider`1", generic: true);
        DefaultConstructor(provider);
        Attribute(provider, "EagerConditional");
        var policyOwner = Type(lib.MainModule, "EagerPolicy`1", generic: true);
        DefaultConstructor(policyOwner);
        var policyMethod = Method(policyOwner, "Activate");
        Dependency(policyMethod, "EagerPolicyDependency");
        policyMethod.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
        var marker = Type(lib.MainModule, "EagerMarker`1", generic: true);
        marker.Attributes = TypeAttributes.Public | TypeAttributes.Interface | TypeAttributes.Abstract;
        marker.BaseType = null;
        var wrap = Type(lib.MainModule, "EagerWrap`1", generic: true);
        var anchorModule = mode == "eager-attribute-framework" ? facade.MainModule : lib.MainModule;
        var anchor = Type(anchorModule, "EagerAnchor");
        var anchorCtor = Method(anchor, ".ctor", true);
        Base(anchorCtor, "Object");
        if (mode != "eager-attribute-framework") Dependency(anchorCtor, "EagerAnchorCtorDependency");
        anchorCtor.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
        var cctor = Method(anchor, ".cctor");
        if (mode != "eager-attribute-framework") Dependency(cctor, "EagerAnchorCctorDependency");
        cctor.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
        if (mode != "eager-attribute-framework")
            anchor.Fields.Add(new FieldDefinition("LazyField", FieldAttributes.Public,
                Closed(anchorModule, lazyField, Core(anchorModule, "Int32"))));
        var ordinary = Method(anchor, "Ordinary");
        if (mode != "eager-attribute-framework")
        {
            ordinary.Parameters.Add(new ParameterDefinition(Closed(anchorModule, lazyMethod, Core(anchorModule, "Int32"))));
            Dependency(ordinary, "EagerAnchorOrdinaryDependency");
        }
        ordinary.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
        TypeReference formation = Closed(anchorModule, EagerAttribute(mode) ? provider : EagerPreserve(mode) ? policyOwner : cell, Core(anchorModule, "Int32"));
        if (mode.EndsWith("-field", StringComparison.Ordinal))
            anchor.Fields.Add(new FieldDefinition("OnlyFormation", FieldAttributes.Public, formation));
        else if (mode.EndsWith("-signature", StringComparison.Ordinal))
        {
            var unused = Method(anchor, "OnlyFormation");
            unused.Parameters.Add(new ParameterDefinition(formation));
            unused.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
        }
        else if (mode.Contains("-methodimpl", StringComparison.Ordinal))
        {
            anchor.Attributes |= TypeAttributes.Abstract;
            var contract = Type(lib.MainModule, "EagerContract");
            contract.Attributes = TypeAttributes.Public | TypeAttributes.Interface | TypeAttributes.Abstract;
            contract.BaseType = null;
            bool candidate = mode is "eager-array-methodimpl-candidate" or "eager-array-methodimpl-different-name";
            if (candidate)
            {
                var sibling = Method(contract, mode.EndsWith("-different-name", StringComparison.Ordinal) ? "Other" : "Resolve", true);
                sibling.Attributes |= MethodAttributes.Abstract | MethodAttributes.Virtual | MethodAttributes.NewSlot;
                sibling.Parameters.Add(new ParameterDefinition(formation));
                sibling.Parameters.Add(new ParameterDefinition(Core(lib.MainModule, "Int32")));
            }
            var declaration = Method(contract, "Resolve", true);
            declaration.Attributes |= MethodAttributes.Abstract | MethodAttributes.Virtual | MethodAttributes.NewSlot;
            declaration.Parameters.Add(new ParameterDefinition(candidate ? Core(lib.MainModule, "Int32") : formation));
            anchor.Interfaces.Add(new InterfaceImplementation(contract));
            var body = Method(anchor, "Implement", true);
            body.Attributes |= MethodAttributes.Virtual | MethodAttributes.NewSlot | MethodAttributes.Final;
            body.Parameters.Add(new ParameterDefinition(candidate ? Core(lib.MainModule, "Int32") : formation));
            body.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
            if (mode != "eager-array-methodimpl-absent")
            {
                var reference = new MethodReference("Resolve", Core(lib.MainModule, "Void"), contract) { HasThis = true };
                reference.Parameters.Add(new ParameterDefinition(candidate ? Core(lib.MainModule, "Int32") : formation));
                body.Overrides.Add(reference);
            }
        }
        else if (mode.Contains("-vtable", StringComparison.Ordinal))
        {
            var parent = Type(lib.MainModule, "EagerVirtualBase`1", generic: true);
            DefaultConstructor(parent);
            var slot = Method(parent, "Resolve", true);
            slot.Attributes |= MethodAttributes.Virtual | MethodAttributes.NewSlot;
            slot.Parameters.Add(new ParameterDefinition(Closed(lib.MainModule, cell, parent.GenericParameters[0])));
            slot.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
            anchor.BaseType = Closed(anchorModule, parent, Core(anchorModule, "Int32"));
            var body = Method(anchor, mode.EndsWith("-different-name", StringComparison.Ordinal) ? "Other" : "Resolve", true);
            body.Attributes |= MethodAttributes.Virtual;
            if (mode.EndsWith("-newslot", StringComparison.Ordinal)) body.Attributes |= MethodAttributes.NewSlot;
            body.Parameters.Add(new ParameterDefinition(formation));
            body.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
        }
        else if (EagerActivation(mode))
        {
            var backing = Type(lib.MainModule, "Task`1", generic: true);
            backing.Namespace = "System.Threading.Tasks";
            Attribute(backing, "EagerConditional");
            var task = Type(lib.MainModule, "EagerTask", value: true);
            task.Interfaces.Add(new InterfaceImplementation(Closed(lib.MainModule, marker,
                Closed(lib.MainModule, cell, Core(lib.MainModule, "Int32")))));
            var builder = Type(shim.MainModule, "EagerBuilder", value: true);
            foreach (string name in new[] { "Create", "Start", "SetStateMachine", "SetResult", "SetException", "get_Task" })
                Method(builder, name).Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
            var attribute = new CustomAttribute(Api(lib.MainModule, "Runtime.CompilerServices.AsyncMethodBuilderAttribute", ".ctor", "System.Type"));
            attribute.ConstructorArguments.Add(new CustomAttributeArgument(Core(lib.MainModule, "Type"), lib.MainModule.ImportReference(builder)));
            task.CustomAttributes.Add(attribute);
            Method(task, "OutsideContract", true).Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
            anchor.Interfaces.Add(new InterfaceImplementation(Closed(anchorModule, marker, anchorModule.ImportReference(task))));
            var dead = Method(Type(app.MainModule, "EagerDeadCross"), "Decline");
            dead.Attributes = MethodAttributes.Private | MethodAttributes.Static | MethodAttributes.HideBySig;
            dead.Body.InitLocals = true;
            var local = new VariableDefinition(app.MainModule.ImportReference(task));
            dead.Body.Variables.Add(local);
            dead.Body.Instructions.Add(Instruction.Create(OpCodes.Ldloca, local));
            dead.Body.Instructions.Add(Instruction.Create(OpCodes.Call,
                new MethodReference("OutsideContract", Core(app.MainModule, "Void"), local.VariableType) { HasThis = true }));
            dead.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
        }
        else if (EagerAsync(mode))
        {
            var backing = Type(lib.MainModule, "Task`1", generic: true);
            backing.Namespace = "System.Threading.Tasks";
            Attribute(backing, "EagerConditional");
            var task = Type(lib.MainModule, "EagerTask`1", generic: true, value: true);
            task.Interfaces.Add(new InterfaceImplementation(Closed(lib.MainModule, marker,
                Closed(lib.MainModule, cell, task.GenericParameters[0]))));
            var builder = Type(lib.MainModule, "EagerBuilder`1", generic: true, value: true);
            foreach (string name in new[] { "Create", "Start", "SetStateMachine", "SetResult", "SetException", "get_Task" })
            {
                var method = Method(builder, name);
                method.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
            }
            var attribute = new CustomAttribute(Api(lib.MainModule, "Runtime.CompilerServices.AsyncMethodBuilderAttribute", ".ctor", "System.Type"));
            attribute.ConstructorArguments.Add(new CustomAttributeArgument(Core(lib.MainModule, "Type"), builder));
            task.CustomAttributes.Add(attribute);
            var outside = Method(task, "OutsideContract", true);
            outside.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
            anchor.Interfaces.Add(new InterfaceImplementation(Closed(anchorModule, marker,
                Closed(anchorModule, task, Core(anchorModule, "Int32")))));
            var declinedTask = task;
            if (second is not null)
            {
                var otherModule = second.MainModule;
                var otherCell = Type(otherModule, "EagerBCell`1", generic: true, value: true);
                otherCell.Fields.Add(new FieldDefinition("Value", FieldAttributes.Public, otherCell.GenericParameters[0]));
                var otherCtor = Method(otherCell, ".ctor", true);
                Dependency(otherCtor, "EagerBArrayDependency");
                otherCtor.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
                declinedTask = Type(otherModule, "EagerDeclinedTask`1", generic: true, value: true);
                declinedTask.Interfaces.Add(new InterfaceImplementation(Closed(otherModule, marker,
                    Closed(otherModule, otherCell, declinedTask.GenericParameters[0]))));
                var sharedAttribute = new CustomAttribute(Api(otherModule, "Runtime.CompilerServices.AsyncMethodBuilderAttribute", ".ctor", "System.Type"));
                sharedAttribute.ConstructorArguments.Add(new CustomAttributeArgument(Core(otherModule, "Type"), otherModule.ImportReference(builder)));
                declinedTask.CustomAttributes.Add(sharedAttribute);
                Method(declinedTask, "OutsideContract", true).Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
                anchor.Interfaces.Add(new InterfaceImplementation(Closed(anchorModule, marker,
                    Closed(anchorModule, declinedTask, Core(anchorModule, "Int64")))));
            }
            if (EagerOptionSensitive(mode))
            {
                var getTask = builder.Methods.Single(m => m.Name == "get_Task");
                getTask.Attributes &= ~MethodAttributes.Static;
                getTask.HasThis = true;
                getTask.ReturnType = Closed(lib.MainModule, declinedTask, builder.GenericParameters[0]);
                getTask.Body.Instructions.Clear();
                getTask.Body.InitLocals = true;
                var result = new VariableDefinition(getTask.ReturnType);
                getTask.Body.Variables.Add(result);
                getTask.Body.Instructions.Add(Instruction.Create(OpCodes.Ldloca, result));
                getTask.Body.Instructions.Add(Instruction.Create(OpCodes.Initobj, getTask.ReturnType));
                getTask.Body.Instructions.Add(Instruction.Create(OpCodes.Ldloc, result));
                getTask.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
            }
            if (mode == "eager-array-async-declined" || second is not null)
            {
                var dead = Method(Type(app.MainModule, "EagerDeadCross"), "Decline");
                dead.Attributes = MethodAttributes.Private | MethodAttributes.Static | MethodAttributes.HideBySig;
                dead.Body.InitLocals = true;
                var local = new VariableDefinition(Closed(app.MainModule, EagerOptionSensitive(mode) ? builder : declinedTask,
                    Core(app.MainModule, second is null ? "Int32" : "Int64")));
                dead.Body.Variables.Add(local);
                dead.Body.Instructions.Add(Instruction.Create(OpCodes.Ldloca, local));
                if (EagerOptionSensitive(mode))
                {
                    var called = new MethodReference("get_Task",
                        Closed(app.MainModule, declinedTask, builder.GenericParameters[0]), local.VariableType) { HasThis = true };
                    dead.Body.Instructions.Add(Instruction.Create(OpCodes.Call, called));
                    dead.Body.Instructions.Add(Instruction.Create(OpCodes.Pop));
                }
                else
                    dead.Body.Instructions.Add(Instruction.Create(OpCodes.Call,
                        new MethodReference("OutsideContract", Core(app.MainModule, "Void"), local.VariableType) { HasThis = true }));
                dead.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
            }
        }
        else if (mode is "eager-array-intrinsic-shell" or "eager-array-vector-shape")
        {
            var vector = Type(lib.MainModule, "Vector`1", generic: true, value: true);
            vector.Namespace = "System.Numerics";
            vector.Interfaces.Add(new InterfaceImplementation(Closed(lib.MainModule, marker,
                Closed(lib.MainModule, cell, vector.GenericParameters[0]))));
            var scalar = Type(lib.MainModule, "EagerScalar", value: true);
            scalar.Fields.Add(new FieldDefinition("Value", FieldAttributes.Public, Core(lib.MainModule, "Int32")));
            anchor.Interfaces.Add(new InterfaceImplementation(Closed(anchorModule, marker,
                Closed(anchorModule, vector, mode == "eager-array-intrinsic-shell" ? Core(anchorModule, "Int32") : scalar))));
        }
        else if (!mode.EndsWith("-template", StringComparison.Ordinal))
        {
            if (mode.EndsWith("-chain", StringComparison.Ordinal))
            {
                var mid = Type(lib.MainModule, "EagerMid`1", generic: true);
                if (EagerArray(mode))
                    mid.Interfaces.Add(new InterfaceImplementation(Closed(lib.MainModule, marker,
                        Closed(lib.MainModule, cell, mid.GenericParameters[0]))));
                else
                    mid.BaseType = Closed(lib.MainModule, EagerAttribute(mode) ? provider : policyOwner, mid.GenericParameters[0]);
                DefaultConstructor(mid);
                anchor.BaseType = Closed(anchorModule, mid, Core(anchorModule, "Int32"));
            }
            else if (EagerAttribute(mode) || EagerPreserve(mode)) anchor.BaseType = formation;
            else
            {
                if (mode == "eager-array-nested") formation = Closed(anchorModule, wrap, formation);
                anchor.Interfaces.Add(new InterfaceImplementation(Closed(anchorModule, marker, formation)));
                if (mode == "eager-array-fanout")
                {
                    var other = Type(lib.MainModule, "EagerSecondAnchor");
                    other.Interfaces.Add(new InterfaceImplementation(Closed(lib.MainModule, marker,
                        Closed(lib.MainModule, cell, Core(lib.MainModule, "Int64")))));
                }
            }
        }
        anchorCtor.Body.Instructions[1].Operand = new MethodReference(".ctor", Core(anchorModule, "Void"), anchor.BaseType) { HasThis = true };
        var entry = Type(app.MainModule, "EagerEntry");
        var carry = Method(entry, "Carry");
        if (mode != "eager-attribute-no-ctor")
        {
            carry.Body.Instructions.Add(Instruction.Create(OpCodes.Ldnull));
            carry.Body.Instructions.Add(Instruction.Create(OpCodes.Newobj, app.MainModule.ImportReference(attrCtor)));
            carry.Body.Instructions.Add(Instruction.Create(OpCodes.Pop));
        }
        carry.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
        var arm = Method(entry, "Arm");
        if (EagerArray(mode) && mode != "eager-array-unarmed")
        {
            arm.Body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4_1));
            arm.Body.Instructions.Add(Instruction.Create(OpCodes.Newarr, Core(app.MainModule, "Int32")));
            arm.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, Api(app.MainModule, "Array", "Initialize")));
        }
        arm.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
        var main = Method(entry, "Main");
        app.EntryPoint = main;
        foreach (string setter in new[] { "set_CurrentCulture", "set_CurrentUICulture" })
        {
            main.Body.Instructions.Add(Instruction.Create(OpCodes.Call, Api(app.MainModule, "Globalization.CultureInfo", "get_InvariantCulture")));
            main.Body.Instructions.Add(Instruction.Create(OpCodes.Call, Api(app.MainModule, "Globalization.CultureInfo", setter, "System.Globalization.CultureInfo")));
        }
        main.Body.Instructions.Add(Instruction.Create(OpCodes.Call, carry));
        main.Body.Instructions.Add(Instruction.Create(OpCodes.Call, arm));
        main.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
        var rules = new XElement("assembly", new XAttribute("fullname", "GraphLib"), new XAttribute("preserve", "nothing"));
        foreach (string name in new[] { "Graph.EagerConditional", "Graph.EagerLazyConditional" })
            rules.Add(new XElement("type", new XAttribute("fullname", name), new XAttribute("preserve", "nothing"),
                new XAttribute("required", "0"), new XElement("method", new XAttribute("name", "Activate"))));
        if (EagerPolicyMode(mode))
            rules.Add(new XElement("type", new XAttribute("fullname", "Graph.EagerPolicy`1"), new XAttribute("preserve", "nothing"),
                new XElement("method", new XAttribute("name", "Activate"))));
        File.WriteAllText(Path.Combine(directory, "link.xml"), new XElement("linker", rules) + "\n");
        foreach (var assembly in new[] { app, lib, shim, facade, leaf })
            assembly.Write(Path.Combine(directory, assembly.Name.Name + ".dll"), new WriterParameters { Timestamp = 0, DeterministicMvid = true });
        second?.Write(Path.Combine(directory, "GraphTaskB.dll"), new WriterParameters { Timestamp = 0, DeterministicMvid = true });
    }

    private static void CheckEagerModel(string directory, string mode, string state)
    {
        Require(EagerModes.Contains(mode) && (state is "original" or "published" or "observed-before"), "unknown eager fixture state");
        if (EagerAsync(mode)) CheckEagerCore(directory);
        using var app = AssemblyDefinition.ReadAssembly(Path.Combine(directory, "GraphApp.dll"));
        using var lib = AssemblyDefinition.ReadAssembly(Path.Combine(directory, "GraphLib.dll"));
        using var shim = AssemblyDefinition.ReadAssembly(Path.Combine(directory, "DnZlib.dll"));
        using var facade = AssemblyDefinition.ReadAssembly(Path.Combine(directory, "System.ArrayFacade.dll"));
        using var second = EagerSharedBuilder(mode) ? AssemblyDefinition.ReadAssembly(Path.Combine(directory, "GraphTaskB.dll")) : null;
        var assemblies = second is null ? new[] { app, lib, shim, facade } : new[] { app, lib, shim, facade, second };
        bool original = state == "original", observed = state == "observed-before";
        bool Body(MethodDefinition? m) => m is { HasBody: true } && !Stub(m);
        bool BodyAt(string type, string method) => Body(lib.MainModule.GetType("Graph." + type)?.Methods.SingleOrDefault(m => m.Name == method));
        bool NamesBacking(TypeReference? type) => type is not null && (type.FullName == "System.Threading.Tasks.Task`1"
            || type is GenericInstanceType generic && (NamesBacking(generic.ElementType) || generic.GenericArguments.Any(NamesBacking))
            || type is TypeSpecification specification && NamesBacking(specification.ElementType));
        bool OperandNamesBacking(object? operand) => operand switch
        {
            TypeReference type => NamesBacking(type),
            MethodReference method => NamesBacking(method.DeclaringType) || NamesBacking(method.ReturnType)
                || method.Parameters.Any(p => NamesBacking(p.ParameterType)),
            FieldReference field => NamesBacking(field.DeclaringType) || NamesBacking(field.FieldType),
            _ => false,
        };
        bool conditional = BodyAt("EagerConditional", "Activate"), array = BodyAt("EagerCell`1", ".ctor"), policy = BodyAt("EagerPolicy`1", "Activate");
        bool arrayB = Body(second?.MainModule.GetType("Graph.EagerBCell`1")?.Methods.SingleOrDefault(m => m.Name == ".ctor"));
        Require(arrayB == BodyAt("EagerBArrayDependency", "Touch"), "declined task constructor and dependency disagree");
        foreach (var item in new[] { ("EagerConditionalDependency", conditional), ("EagerArrayDependency", array), ("EagerPolicyDependency", policy) })
            Require(BodyAt(item.Item1, "Touch") == item.Item2, "eager body and unique dependency disagree: " + item.Item1);
        var negativeNames = new[] { "EagerLazyFieldDependency", "EagerLazyMethodDependency", "EagerLazyConditionalDependency",
            "EagerAnchorCtorDependency", "EagerAnchorCctorDependency", "EagerAnchorOrdinaryDependency" };
        var negatives = negativeNames.ToDictionary(name => name, name => BodyAt(name, "Touch"));
        if (!original && !observed)
        {
            Require(conditional == EagerConditionalActive(mode), "eager conditional body admission changed");
            if (mode != "eager-array-async-adopted" && !EagerSharedBuilder(mode))
                Require(array == (EagerArray(mode) && EagerActive(mode)), "eager array constructor admission changed");
            if (EagerOptionSensitive(mode))
                Require(array, "option-sensitive task real-shape constructor disappeared");
            Require(arrayB == EagerSharedBuilder(mode), "shared-builder declined task real shape disappeared");
            // An unconditional generic rule can retain its definition before any specialization exists.
            Require(!EagerPolicyMode(mode) || policy, "eager generic preservation lost its selected definition body");
            Require(negatives.Values.All(value => !value), "passive formation anchor opened a lazy signature or body");
        }
        var anchorModule = mode == "eager-attribute-framework" ? facade.MainModule : lib.MainModule;
        var anchor = anchorModule.GetType("Graph.EagerAnchor");
        var provider = (mode is "eager-attribute-copied" or "eager-attribute-framework" ? shim.MainModule : lib.MainModule).GetType("Graph.EagerProvider`1");
        if (EagerAsync(mode)) provider = lib.MainModule.GetType("System.Threading.Tasks.Task`1");
        var row = provider?.CustomAttributes.SingleOrDefault(a => a.AttributeType.FullName == "Graph.EagerDataAttribute");
        bool attributeEvidence = row is not null;
        if (row is not null)
            Require(row.ConstructorArguments.Count == 1 && row.ConstructorArguments[0].Value is TypeReference t
                && t.FullName == "Graph.EagerConditional" && t.Scope.Name == "GraphLib" && !row.HasFields && !row.HasProperties,
                "eager provider attribute argument changed");
        if (original || !observed && EagerActive(mode) && !EagerActivation(mode))
        {
            Require(anchor is not null && !anchor.HasGenericParameters, "eager original nongeneric anchor disappeared");
            if (EagerAttribute(mode) || mode == "eager-array-async-adopted" || EagerSharedBuilder(mode))
                Require(attributeEvidence, "eager original generic provider row disappeared");
            if (mode.Contains("-methodimpl", StringComparison.Ordinal))
                Require(anchor!.Methods.SingleOrDefault(m => m.Name == "Implement")?.Overrides.Count
                    == (mode == "eager-array-methodimpl-absent" ? 0 : 1), "eager causal MethodImpl changed");
            else if (mode.Contains("-vtable", StringComparison.Ordinal))
                Require(anchor!.Methods.SingleOrDefault(m => m.Name == (mode.EndsWith("-different-name", StringComparison.Ordinal) ? "Other" : "Resolve"))
                    is { IsVirtual: true } slot && slot.IsNewSlot == mode.EndsWith("-newslot", StringComparison.Ordinal),
                    "eager causal override signature disappeared");
            else if (!mode.EndsWith("-field", StringComparison.Ordinal) && !mode.EndsWith("-signature", StringComparison.Ordinal)
                && !mode.EndsWith("-template", StringComparison.Ordinal))
                Require(anchor!.BaseType is GenericInstanceType || anchor.Interfaces.Any(i => i.InterfaceType is GenericInstanceType), "eager relation TypeSpec disappeared");
        }
        if (!original && !observed && anchorModule == lib.MainModule && anchor is not null)
            foreach (var m in anchor.Methods)
                Require(!Body(m), "passive formation anchor acquired an executable member: " + m.Name);
        if (original)
        {
            Require(app.EntryPoint.Name == "Main" && app.EntryPoint.DeclaringType.FullName == "Graph.EagerEntry", "eager entry changed");
            Require(app.EntryPoint.Body.Instructions.Take(4).Select(i => (i.Operand as MethodReference)?.Name).SequenceEqual(
                new[] { "get_InvariantCulture", "set_CurrentCulture", "get_InvariantCulture", "set_CurrentUICulture" }), "eager cultures are not pinned");
            Require(app.MainModule.GetTypes().All(t => !t.HasCustomAttributes && t.Fields.Count == 0), "eager app gained metadata formation roots");
            foreach (var m in app.MainModule.GetTypes().SelectMany(t => t.Methods))
            {
                Require(m.Parameters.Count == 0 && m.ReturnType.FullName == "System.Void", "eager app gained a witness signature");
                if (m.DeclaringType.FullName == "Graph.EagerDeadCross")
                {
                    if (EagerActivation(mode))
                    {
                        Require(ActivationCarrier(m), "original arity-zero decline MemberRef changed");
                        continue;
                    }
                    if (EagerOptionSensitive(mode))
                    {
                        Require(OptionCarrier(m), "original option-sensitive builder MemberRef changed");
                        continue;
                    }
                    Require((mode == "eager-array-async-declined" || EagerSharedBuilder(mode)) && m.Name == "Decline" && m.IsPrivate && m.IsStatic
                        && m.Body.InitLocals && m.Body.Variables.Single().VariableType.FullName
                            == (EagerSharedBuilder(mode) ? "Graph.EagerDeclinedTask`1<System.Int64>" : "Graph.EagerTask`1<System.Int32>")
                        && m.Body.Instructions.Select(i => i.OpCode.Code).SequenceEqual(new[] { Code.Ldloca, Code.Call, Code.Ret })
                        && m.Body.Instructions[1].Operand is MethodReference outside && outside.Name == "OutsideContract"
                        && outside.DeclaringType.Scope.Name == (EagerSharedBuilder(mode) ? "GraphTaskB" : "GraphLib"), "original async decline MemberRef changed");
                    continue;
                }
                foreach (var instruction in m.Body.Instructions)
                {
                    Require(instruction.OpCode != OpCodes.Ldtoken && instruction.OpCode != OpCodes.Initobj && instruction.OpCode != OpCodes.Box,
                        "eager app gained a witness type token or construction");
                    if (instruction.Operand is MethodReference called)
                        Require(called.DeclaringType.FullName is "Graph.EagerEntry" or "Graph.EagerDataAttribute"
                            or "System.Array" or "System.Globalization.CultureInfo", "eager app directly called a witness");
                    if (instruction.Operand is TypeReference used)
                        Require(used.FullName == "System.Int32", "eager app directly formed a witness type");
                }
            }
            if (mode != "eager-attribute-framework")
                Require(anchor!.Fields.Single(f => f.Name == "LazyField").FieldType.FullName == "Graph.EagerLazyFieldCell`1<System.Int32>"
                    && anchor.Methods.Single(m => m.Name == "Ordinary") is { IsVirtual: false, HasOverrides: false } lazy
                    && lazy.Parameters.Single().ParameterType.FullName == "Graph.EagerLazyMethodCell`1<System.Int32>",
                    "eager retained-anchor lazy controls changed");
            if (mode is "eager-attribute-copied" or "eager-attribute-framework" || EagerActivation(mode))
                Require(shim.MainModule.AssemblyReferences.All(r => r.Name == "System.Private.CoreLib"), "copied eager provider gained a stripped reference");
            if (mode == "eager-attribute-framework")
                Require(facade.MainModule.AssemblyReferences.All(r => r.Name is "System.Private.CoreLib" or "DnZlib"),
                    "framework formation anchor gained a stripped reference");
            if (EagerAsync(mode))
                foreach (var assembly in assemblies)
                {
                    Require(assembly.MainModule.GetTypeReferences().All(t => !NamesBacking(t)),
                        "implicit async backing acquired a direct TypeRef");
                    foreach (var type in assembly.MainModule.GetTypes())
                    {
                        Require(!NamesBacking(type.BaseType) && type.Interfaces.All(i => !NamesBacking(i.InterfaceType))
                            && type.Fields.All(f => !NamesBacking(f.FieldType)), "implicit async backing acquired a shape root");
                        foreach (var method in type.Methods)
                            Require(!NamesBacking(method.ReturnType) && method.Parameters.All(p => !NamesBacking(p.ParameterType))
                                && method.Overrides.All(o => !OperandNamesBacking(o))
                                && (!method.HasBody || method.Body.Variables.All(v => !NamesBacking(v.VariableType))
                                    && method.Body.Instructions.All(i => !OperandNamesBacking(i.Operand))),
                                "implicit async backing acquired a signature or IL root");
                    }
                }
        }
        var asyncBuilder = EagerActivation(mode) ? shim.MainModule.GetType("Graph.EagerBuilder") : lib.MainModule.GetType("Graph.EagerBuilder`1");
        string[] builderMethods = asyncBuilder?.Methods.Select(m => m.Name).OrderBy(n => n, StringComparer.Ordinal).ToArray() ?? Array.Empty<string>();
        if ((original && EagerAsync(mode)) || (!observed && (mode == "eager-array-async-adopted" || EagerSharedBuilder(mode) || EagerActivation(mode))))
            Require(builderMethods.SequenceEqual(new[] { "Create", "SetException", "SetResult", "SetStateMachine", "Start", "get_Task" }),
                "eligible original async builder contract rows disappeared");
        var asyncContracts = assemblies.SelectMany(a => a.MainModule.Types)
            .Where(t => t.Name is "EagerTask`1" or "EagerDeclinedTask`1" || EagerActivation(mode) && t.Name == "EagerTask")
            .ToDictionary(t => t.Module.Assembly.Name.Name + ":" + t.FullName, t =>
            {
                var contract = t.CustomAttributes.SingleOrDefault(a => a.AttributeType.FullName == "System.Runtime.CompilerServices.AsyncMethodBuilderAttribute");
                var builderType = contract?.ConstructorArguments.Single().Value as TypeReference;
                return builderType is null ? null : builderType.FullName + "@" + builderType.Scope.Name;
            });
        if (EagerSharedBuilder(mode) && !observed)
            Require(asyncContracts.Count == 2 && asyncContracts.Values.All(v => v == "Graph.EagerBuilder`1@GraphLib"),
                "shared-builder task eligibility attributes changed");
        if (EagerActivation(mode) && !observed)
            Require(asyncContracts.Count == 1 && asyncContracts.Values.Single() == "Graph.EagerBuilder@DnZlib"
                && lib.MainModule.GetType("Graph.EagerTask") is { HasGenericParameters: false, IsValueType: true } task
                && task.Interfaces.Count == 1
                && task.Interfaces[0].InterfaceType.FullName == "Graph.EagerMarker`1<Graph.EagerCell`1<System.Int32>>"
                && asyncBuilder is { HasGenericParameters: false, IsValueType: true },
                "arity-zero copied-builder eligibility changed");
        var carrier = app.MainModule.GetType("Graph.EagerDeadCross")?.Methods.SingleOrDefault(m => m.Name == "Decline");
        if (EagerActivation(mode) && !observed)
            Require(ActivationCarrier(carrier), "arity-zero original decline carrier disappeared");
        else if (EagerOptionSensitive(mode) && !observed)
            Require(OptionCarrier(carrier), "option-sensitive original decline carrier disappeared");
        else if (EagerSharedBuilder(mode) && !observed)
            Require(Body(carrier) && carrier!.IsPrivate && carrier.IsStatic && carrier.Body.InitLocals
                && carrier.Body.Variables.Single().VariableType.FullName == "Graph.EagerDeclinedTask`1<System.Int64>"
                && carrier.Body.Instructions.Select(i => i.OpCode.Code).SequenceEqual(new[] { Code.Ldloca, Code.Call, Code.Ret })
                && carrier.Body.Instructions[1].Operand is MethodReference witness && witness.Name == "OutsideContract"
                && witness.DeclaringType.Scope.Name == "GraphTaskB", "shared-builder original decline carrier disappeared");
        if (EagerOptionSensitive(mode) && !observed)
        {
            var getTask = asyncBuilder!.Methods.Single(m => m.Name == "get_Task");
            Require(getTask.HasThis && getTask.Parameters.Count == 0 && !getTask.HasGenericParameters
                && getTask.ReturnType is GenericInstanceType returned
                && returned.ElementType.FullName == "Graph.EagerDeclinedTask`1"
                && returned.ElementType.Scope.Name == "GraphTaskB"
                && returned.GenericArguments.Single() is GenericParameter { Position: 0, Type: GenericParameterType.Type },
                "option-sensitive builder return contract changed");
        }
        bool OptionCarrier(MethodDefinition? method) => Body(method) && method!.Name == "Decline"
            && method.IsPrivate && method.IsStatic && method.Body.InitLocals
            && method.Body.Variables.Single().VariableType.FullName == "Graph.EagerBuilder`1<System.Int64>"
            && method.Body.Instructions.Select(i => i.OpCode.Code).SequenceEqual(new[] { Code.Ldloca, Code.Call, Code.Pop, Code.Ret })
            && method.Body.Instructions[1].Operand is MethodReference witness && witness.Name == "get_Task"
            && witness.HasThis && witness.Parameters.Count == 0 && !witness.HasGenericParameters
            && witness.DeclaringType.FullName == "Graph.EagerBuilder`1<System.Int64>"
            && witness.DeclaringType.Scope.Name == "GraphLib"
            && witness.ReturnType is GenericInstanceType returned
            && returned.ElementType.FullName == "Graph.EagerDeclinedTask`1"
            && returned.ElementType.Scope.Name == "GraphTaskB"
            && returned.GenericArguments.Single() is GenericParameter { Position: 0, Type: GenericParameterType.Type };
        bool ActivationCarrier(MethodDefinition? method) => Body(method) && method!.Name == "Decline"
            && method.IsPrivate && method.IsStatic && method.Body.InitLocals
            && method.Body.Variables.Single().VariableType.FullName == "Graph.EagerTask"
            && method.Body.Instructions.Select(i => i.OpCode.Code).SequenceEqual(new[] { Code.Ldloca, Code.Call, Code.Ret })
            && method.Body.Instructions[1].Operand is MethodReference witness && witness.Name == "OutsideContract"
            && witness.HasThis && witness.Parameters.Count == 0 && !witness.HasGenericParameters
            && witness.DeclaringType.FullName == "Graph.EagerTask" && witness.DeclaringType.Scope.Name == "GraphLib"
            && witness.ReturnType.FullName == "System.Void";
        string? carrierIl = null;
        if (carrier is { HasBody: true } && carrier.RVA != 0)
        {
            using var bytes = File.OpenRead(Path.Combine(directory, "GraphApp.dll"));
            using var pe = new System.Reflection.PortableExecutable.PEReader(bytes);
            var body = pe.GetSectionData(carrier.RVA).GetContent().ToArray();
            int header = (body[0] & 3) == 2 ? 1 : (body[1] >> 4) * 4;
            int size = header == 1 ? body[0] >> 2 : BitConverter.ToInt32(body, 4);
            carrierIl = Convert.ToHexString(body.AsSpan(header, size));
        }
        if (EagerOptionSensitive(mode))
            Console.WriteLine("graph-eager-option-order=" + System.Text.Json.JsonSerializer.Serialize(
                assemblies.SelectMany(a => a.MainModule.Types
                    .Where(t => t.Name is "EagerTask`1" or "EagerBuilder`1" or "EagerDeclinedTask`1")
                    .Select(t => new
                    {
                        module = a.Name.Name, type = t.FullName,
                        methods = t.Methods.Where(m => t.Name == "EagerBuilder`1" || m.Name == "GetAwaiter")
                            .Select(m => m.FullName).ToArray(),
                    })).ToArray()));
        Console.WriteLine("graph-eager-observed=" + System.Text.Json.JsonSerializer.Serialize(new
        {
            conditional, array, arrayB, policy, negatives, anchor = anchor is not null, attributeEvidence,
            builderMethods,
            asyncContracts,
            declineCarrierIl = carrierIl,
            declineCarrier = carrier is not { HasBody: true } ? null : new
            {
                signature = carrier.FullName, attributes = (int)carrier.Attributes,
                implAttributes = (int)carrier.ImplAttributes, initLocals = carrier.Body.InitLocals,
                maxStack = carrier.Body.MaxStackSize,
                locals = carrier.Body.Variables.Select(v => v.VariableType.FullName + "@" + v.VariableType.Scope.Name).ToArray(),
                instructions = carrier.Body.Instructions.Select(i => new
                {
                    opcode = i.OpCode.Code.ToString(),
                    operand = i.Operand is MethodReference method ? method.FullName + "@" + method.DeclaringType.Scope.Name
                        : i.Operand is VariableDefinition local ? local.Index + ":" + local.VariableType.FullName
                        : i.Operand?.ToString(),
                }).ToArray(),
                handlers = carrier.Body.ExceptionHandlers.Count,
            },
            anchorBase = anchor?.BaseType?.FullName,
            anchorInterfaces = anchor?.Interfaces.Select(i => i.InterfaceType.FullName).ToArray(),
            methodImpls = anchor?.Methods.SelectMany(m => m.Overrides.Select(o => m.FullName + " -> " + o.FullName)).ToArray(),
            formations = assemblies.Where(a => a != app).SelectMany(a => a.MainModule.Types)
                .Where(t => t.Name is "EagerAnchor" or "EagerSecondAnchor" or "EagerMid`1" or "EagerContract"
                    or "EagerVirtualBase`1" or "EagerProvider`1" or "EagerPolicy`1" or "Vector`1" or "EagerTask`1" or "Task`1" or "EagerDeclinedTask`1"
                    || EagerActivation(mode) && t.Name == "EagerTask")
                .ToDictionary(t => t.Module.Assembly.Name.Name + ":" + t.FullName, t => new
                {
                    baseType = t.BaseType?.FullName,
                    interfaces = t.Interfaces.Select(i => i.InterfaceType.FullName).ToArray(),
                    methods = t.Methods.Where(m => m.Name is "Resolve" or "Implement" or "Other").Select(m => new
                    {
                        signature = m.FullName, isVirtual = m.IsVirtual, newSlot = m.IsNewSlot,
                        overrides = m.Overrides.Select(o => o.FullName).ToArray(),
                    }).ToArray(),
                }),
        }));
        Console.WriteLine("graph-copied-array=" + state + ":" + mode + ":original-formation-evidence,lazy-signatures,body-dependencies,passive-anchor");
    }
}

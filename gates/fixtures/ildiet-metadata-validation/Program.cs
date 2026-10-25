using System.Globalization;
using Mono.Cecil;
using Mono.Cecil.Cil;

CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
if (CutRequestCapture.Run(args)) return;
if (ProtocolMutation.Run(args)) return;
if (GodotValidation.Run(args)) return;
if (GraphValidation.Run(args)) return;
if (PropertySignatureValidation.Run(args)) return;
if (args.Length == 3 && args[0] == "--create-slot-storage-fixture")
{
    CreateArrayNarrowingFixture(args[1], args[2], scalarDefaults: true, slotStorage: true);
    return;
}
if (args.Length == 3 && args[0] == "--create-captured-scalar-defaults-fixture")
{
    CreateArrayNarrowingFixture(args[1], args[2], scalarDefaults: true);
    return;
}
if (args.Length == 3 && args[0] == "--create-array-narrowing-fixture")
{
    CreateArrayNarrowingFixture(args[1], args[2]);
    return;
}
if (args.Length == 3 && args[0] == "--create-local-defaults-fixture")
{
    CreateLocalDefaultsFixture(args[1], args[2]);
    return;
}
if (args.Length == 2 && args[0] == "--check-script-discovery")
{
    ScriptDiscoveryValidation.Run(args[1]);
    return;
}
if (args.Length == 3 && args[0] == "--create-initializer-fixture")
{
    CreateInitializerFixture(args[1], args[2] == "late");
    Console.WriteLine("fixture-created=suppressed-initializer");
    return;
}
if (args.Length == 4 && args[0] == "--check-initializer-fixture")
{
    using var before = AssemblyDefinition.ReadAssembly(args[1]);
    using var after = AssemblyDefinition.ReadAssembly(args[2]);
    bool rooted = args[3] != "signature";
    foreach (string name in new[] { "SuppressedCell", "OrdinaryCell", "GenericCell`1" })
    {
        var originalType = before.MainModule.GetType("InitializerFixture." + name);
        var type = after.MainModule.GetType("InitializerFixture." + name);
        Require(type is not null, "signature-only initializer owner was removed: " + name);
        var originalInitializer = originalType.Methods.Single(m => m.Name == ".cctor");
        var rewrittenInitializer = type!.Methods.SingleOrDefault(m => m.Name == ".cctor");
        if (name == "SuppressedCell" && !rooted)
            Require(rewrittenInitializer is null, "suppressed metadata initializer became executable");
        else
            Require(rewrittenInitializer is not null && InitializerBody(originalInitializer) == InitializerBody(rewrittenInitializer),
                "rooted initializer body changed: " + name);
    }
    Require((after.MainModule.GetType("InitializerFixture.SuppressedDependency") is not null) == rooted,
        "suppressed initializer dependencies did not follow its real root");
    Require(after.MainModule.GetType("InitializerFixture.BodyOnlyDependency") is null,
        "metadata signature opened an uncalled body dependency");
    var dead = after.MainModule.GetType("InitializerFixture.Owner").Methods.Single(m => m.Name == "Dead");
    Require(dead.ReturnType.FullName == "InitializerFixture.SuppressedCell"
        && dead.Body.Instructions.Select(i => i.OpCode.Code).SequenceEqual(new[] { Code.Ldc_I4, Code.Pop, Code.Ldnull, Code.Throw }),
        "ordinary metadata method did not retain its signature and stub");
    Console.WriteLine("initializer-metadata=" + args[3]);
    return;
}
if (args.Length == 2 && args[0] == "--create-backend-policy-fixture")
{
    CreateBackendPolicyFixture(args[1]);
    return;
}
if (args.Length == 3 && args[0] == "--check-backend-policy-fixture")
{
    CheckBackendPolicyFixture(args[1], args[2]);
    return;
}
if (args.Length == 2 && args[0] == "--create-dead")
{
    using var fixture = AssemblyDefinition.CreateAssembly(new AssemblyNameDefinition("ILDietDeadReference", new Version(1, 0)),
        "ILDietDeadReference", ModuleKind.Dll);
    var type = new TypeDefinition("ILDietFixture", "Unused", TypeAttributes.Public | TypeAttributes.Class,
        fixture.MainModule.TypeSystem.Object);
    fixture.MainModule.Types.Add(type);
    var method = new MethodDefinition("Uncalled", MethodAttributes.Public | MethodAttributes.Static,
        fixture.MainModule.TypeSystem.Int32);
    type.Methods.Add(method);
    method.Body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4, 73));
    method.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
    fixture.MainModule.Resources.Add(new EmbeddedResource("unused-resource.bin", ManifestResourceAttributes.Public,
        new byte[] { 0, 1, 127, 128, 255 }));
    fixture.Write(args[1], new WriterParameters { Timestamp = 0, DeterministicMvid = true });
    Console.WriteLine("fixture-created=ILDietDeadReference");
    return;
}
if (args.Length == 2 && args[0] == "--create-resolver-fixtures")
{
    Directory.CreateDirectory(args[1]);
    CreateResolverSink(Path.Combine(args[1], "System.ILDietFixtureSink.dll"));
    CreateResolverLibrary(Path.Combine(args[1], "ResolverFirst.dll"), "ResolverFirst", true);
    CreateResolverLibrary(Path.Combine(args[1], "ResolverSecond.dll"), "ResolverSecond", false);
    CreateResolverApp(Path.Combine(args[1], "ResolverApp.dll"));
    Console.WriteLine("fixture-created=resolver-order");
    return;
}
if (args.Length == 3 && args[0] == "--check-resolver")
{
    string selected = args[2];
    string other = selected == "ResolverFirst" ? "ResolverSecond" : "ResolverFirst";
    using var selectedAssembly = AssemblyDefinition.ReadAssembly(Path.Combine(args[1], selected + ".dll"));
    using var otherAssembly = AssemblyDefinition.ReadAssembly(Path.Combine(args[1], other + ".dll"));
    using var firstAssembly = AssemblyDefinition.ReadAssembly(Path.Combine(args[1], "ResolverFirst.dll"));
    using var resolverApp = AssemblyDefinition.ReadAssembly(Path.Combine(args[1], "ResolverApp.dll"));
    using var sinkAssembly = AssemblyDefinition.ReadAssembly(Path.Combine(args[1], "System.ILDietFixtureSink.dll"));
    Require(!sinkAssembly.MainModule.AssemblyReferences.Any(r => r.Name is "ResolverFirst" or "ResolverSecond" or "ResolverApp"),
        "the copied opaque sink references a stripped assembly");
    var selectedCollision = selectedAssembly.MainModule.GetType("System.ILDietFixture.Collision");
    Require(ResolverBody(selectedCollision?.Methods.SingleOrDefault(m => m.Name == "Selected")),
        "first collision definition was not selected: " + selected);
    foreach (string name in new[] { "GenericSelected", "EmptySelected" })
        Require(ResolverBody(selectedCollision?.Methods.SingleOrDefault(m => m.Name == name)), "generic resolver call body was lost: " + name);
    Require(otherAssembly.MainModule.GetType("System.ILDietFixture.Collision")?.Methods.Any(m => m.Name == "Selected") != true,
        "later collision definition was selected: " + other);
    Require(ResolverBody(selectedAssembly.MainModule.GetType("System.ILDietFixture.LateBound")?.Methods.SingleOrDefault(m =>
            m.IsConstructor && m.Parameters.Count == 1)),
        "generic argument constructor was removed: " + selected);
    var methodBound = selectedAssembly.MainModule.GetType("System.ILDietFixture.MethodBound");
    Require(ResolverBody(methodBound?.Methods.SingleOrDefault(m => m.IsConstructor && m.IsPrivate && m.Parameters.Count == 1)),
        "generic method argument constructor was removed: " + selected);
    Require(ResolverBody(methodBound?.Methods.SingleOrDefault(m => m.Name == "ConstructorLeaf")),
        "generic argument constructor body was not scanned: " + selected);
    foreach (var (typeName, propertyName) in new[]
        {
            ("LateBound", "Value"), ("LateBound", "Child"), ("MethodBound", "Value"),
            ("LateBoundBase", "InheritedValue"), ("FieldPayload", "Value"),
            ("PropertyPayload", "Value"), ("ConstructorPayload", "Value"),
        })
    {
        var dataType = selectedAssembly.MainModule.GetType("System.ILDietFixture." + typeName);
        var property = dataType?.Properties.SingleOrDefault(p => p.Name == propertyName);
        Require(property?.GetMethod is not null && property.SetMethod is { IsPrivate: true },
            "generic data property accessor was removed: " + typeName + "." + propertyName);
        Require(ResolverBody(property!.GetMethod) && ResolverBody(property.SetMethod),
            "generic data property accessor body was lost: " + typeName + "." + propertyName);
        Require(ResolverBody(dataType?.Methods.SingleOrDefault(m => m.Name == propertyName + "SetterLeaf")),
            "generic data setter body was not scanned: " + typeName + "." + propertyName);
    }
    var emptyBound = selectedAssembly.MainModule.GetType("System.ILDietFixture.EmptyBound");
    Require(emptyBound is not null && !emptyBound.Methods.Any(m => m.IsConstructor || m.Name == "EmptyConstructorLeaf"),
        "an empty stripped generic body kept its argument's constructor body");
    Require(selectedAssembly.MainModule.GetType("System.ILDietFixture.Collision")?.Properties.Count == 0,
        "unselected ordinary property survived");
    var owner = firstAssembly.MainModule.GetType("System.ILDietFixture.Owner");
    Require(ResolverBody(owner?.NestedTypes.SingleOrDefault(t => t.Name == "Inner")?.Methods.SingleOrDefault(m => m.Name == "NestedSelected")),
        "nested definition was not resolved");
    var main = resolverApp.EntryPoint;
    Require(main is not null && main.Body.Variables.Any(v => v.VariableType.FullName == "System.ILDietFixture.Missing"),
        "unresolved type metadata changed");
    Console.WriteLine("resolver-valid=" + selected);
    Console.WriteLine("resolver-data-demand=copied-generic-field,copied-generic-method-use,constructor-bodies,accessor-bodies,empty-stripped-generic");
    return;
}
if (args.Length < 2) throw new ArgumentException("expected original.dll rewritten.dll [empty|resources|signed]");
using var resolver = new DefaultAssemblyResolver();
resolver.AddSearchDirectory(Path.GetDirectoryName(Path.GetFullPath(args[1]))!);
using var original = AssemblyDefinition.ReadAssembly(args[0]);
using var rewritten = AssemblyDefinition.ReadAssembly(args[1], new ReaderParameters { AssemblyResolver = resolver });
Require(original.Name.FullName == rewritten.Name.FullName, "assembly identity changed");
Require(original.Name.PublicKey.SequenceEqual(rewritten.Name.PublicKey), "public key changed");
Require((rewritten.MainModule.Attributes & ModuleAttributes.StrongNameSigned) == 0, "rewritten DLL retains a stale strong-name signature");
Require(!File.Exists(Path.ChangeExtension(args[1], ".pdb")), "rewritten DLL has a stale PDB");
var beforeResources = original.MainModule.Resources.OfType<EmbeddedResource>().ToDictionary(r => r.Name);
var afterResources = rewritten.MainModule.Resources.OfType<EmbeddedResource>().ToDictionary(r => r.Name);
Require(beforeResources.Count == afterResources.Count, "resource count changed");
foreach (var (name, resource) in beforeResources)
    Require(afterResources.TryGetValue(name, out var result) && resource.GetResourceData().SequenceEqual(result.GetResourceData()),
        "resource bytes changed: " + name);
var types = rewritten.MainModule.GetTypes().ToHashSet();
var methods = types.SelectMany(t => t.Methods).ToHashSet();
var fields = types.SelectMany(t => t.Fields).ToHashSet();
var seenTypes = new HashSet<TypeReference>();
foreach (var type in types)
{
    Type(type.BaseType);
    Type(type.DeclaringType);
    Attributes(type);
    foreach (var implementation in type.Interfaces) { Type(implementation.InterfaceType); Attributes(implementation); }
    foreach (var parameter in type.GenericParameters) Parameter(parameter);
    foreach (var field in type.Fields) { Type(field.FieldType); Attributes(field); Marshal(field); }
    foreach (var property in type.Properties)
    {
        Type(property.PropertyType);
        Attributes(property);
        if (property.GetMethod is not null) Require(methods.Contains(property.GetMethod), "dangling getter");
        if (property.SetMethod is not null) Require(methods.Contains(property.SetMethod), "dangling setter");
    }
    foreach (var @event in type.Events)
    {
        Type(@event.EventType);
        Attributes(@event);
        foreach (var accessor in new[] { @event.AddMethod, @event.RemoveMethod, @event.InvokeMethod })
            if (accessor is not null) Require(methods.Contains(accessor), "dangling event accessor");
    }
    foreach (var method in type.Methods)
    {
        Type(method.ReturnType);
        Attributes(method);
        Attributes(method.MethodReturnType);
        Marshal(method.MethodReturnType);
        foreach (var parameter in method.Parameters) { Type(parameter.ParameterType); Attributes(parameter); Marshal(parameter); }
        foreach (var parameter in method.GenericParameters) Parameter(parameter);
        foreach (var target in method.Overrides) Method(target);
        if (!method.HasBody) continue;
        foreach (var variable in method.Body.Variables) Type(variable.VariableType);
        foreach (var handler in method.Body.ExceptionHandlers) Type(handler.CatchType);
        foreach (var instruction in method.Body.Instructions)
            switch (instruction.Operand)
            {
                case TypeReference reference: Type(reference); break;
                case MethodReference reference: Method(reference); break;
                case FieldReference reference:
                    Type(reference.DeclaringType); Type(reference.FieldType);
                    if (reference is FieldDefinition definition && definition.Module == rewritten.MainModule)
                        Require(fields.Contains(definition), "dangling field token");
                    break;
            }
    }
}
Attributes(rewritten);
Attributes(rewritten.MainModule);
foreach (string check in args.Skip(2))
    switch (check)
    {
        case "empty": Require(types.All(t => t.Name == "<Module>") && methods.Count == 0, "dead reference was not reduced to an empty module"); break;
        case "resources": Require(beforeResources.Count != 0, "resource check had no witness"); break;
        case "signed": Require(original.Name.HasPublicKey && (original.MainModule.Attributes & ModuleAttributes.StrongNameSigned) != 0, "signing check had no signed witness"); break;
        default: throw new ArgumentException("unknown check: " + check);
    }
Console.WriteLine("metadata-valid=" + rewritten.Name.Name);

void Type(TypeReference? reference)
{
    if (reference is null || !seenTypes.Add(reference)) return;
    if (reference is TypeDefinition definition && definition.Module == rewritten.MainModule)
        Require(types.Contains(definition), "dangling type token");
    if (reference is GenericInstanceType generic)
        foreach (var argument in generic.GenericArguments) Type(argument);
    if (reference is TypeSpecification specification) Type(specification.ElementType);
    if (reference is IModifierType modifier) Type(modifier.ModifierType);
}

void Method(MethodReference reference)
{
    Type(reference.DeclaringType);
    Type(reference.ReturnType);
    foreach (var parameter in reference.Parameters) Type(parameter.ParameterType);
    if (reference is MethodDefinition definition && definition.Module == rewritten.MainModule)
        Require(methods.Contains(definition), "dangling method token");
    if (reference is GenericInstanceMethod generic)
        foreach (var argument in generic.GenericArguments) Type(argument);
}

void Parameter(GenericParameter parameter)
{
    Attributes(parameter);
    foreach (var constraint in parameter.Constraints) { Type(constraint.ConstraintType); Attributes(constraint); }
}

void Marshal(IMarshalInfoProvider provider)
{
    if (provider.HasMarshalInfo && provider.MarshalInfo is CustomMarshalInfo custom) Type(custom.ManagedType);
}

void Attributes(ICustomAttributeProvider provider)
{
    if (!provider.HasCustomAttributes) return;
    foreach (var attribute in provider.CustomAttributes) Method(attribute.Constructor);
}

static void Require(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}

static bool ResolverBody(MethodDefinition? method) => method is { HasBody: true } && method.Body.Instructions.Count != 0
    && !method.Body.Instructions.Select(i => i.OpCode.Code).SequenceEqual(new[] { Code.Ldc_I4, Code.Pop, Code.Ldnull, Code.Throw });

static void CreateResolverSink(string path)
{
    using var sink = AssemblyDefinition.CreateAssembly(new AssemblyNameDefinition("System.ILDietFixtureSink", new Version(1, 0)),
        "System.ILDietFixtureSink", ModuleKind.Dll);
    var module = sink.MainModule;
    var type = new TypeDefinition("System.ILDietFixture", "OpaqueList`1", TypeAttributes.Public | TypeAttributes.Class, module.TypeSystem.Object);
    type.GenericParameters.Add(new GenericParameter("T", type)); module.Types.Add(type);
    var ctor = new MethodDefinition(".ctor", MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName, module.TypeSystem.Void);
    type.Methods.Add(ctor); ctor.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
    ctor.Body.Instructions.Add(Instruction.Create(OpCodes.Call, new MethodReference(".ctor", module.TypeSystem.Void, module.TypeSystem.Object) { HasThis = true }));
    ctor.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
    sink.Write(path, new WriterParameters { Timestamp = 0, DeterministicMvid = true });
}

static void CreateResolverLibrary(string path, string assemblyName, bool nested)
{
    using var fixture = AssemblyDefinition.CreateAssembly(new AssemblyNameDefinition(assemblyName, new Version(1, 0)),
        assemblyName, ModuleKind.Dll);
    var collision = new TypeDefinition("System.ILDietFixture", "Collision",
        TypeAttributes.Public | TypeAttributes.Class, fixture.MainModule.TypeSystem.Object);
    fixture.MainModule.Types.Add(collision);
    AddVoidMethod(collision, "Selected");
    AddDataProperty(collision, "UnusedProperty", fixture.MainModule.TypeSystem.Int32);
    var baseData = AddDataType(fixture.MainModule, "LateBoundBase");
    AddDataProperty(baseData, "InheritedValue", fixture.MainModule.TypeSystem.Int32);
    var lateBound = new TypeDefinition("System.ILDietFixture", "LateBound",
        TypeAttributes.Public | TypeAttributes.Class, baseData);
    fixture.MainModule.Types.Add(lateBound);
    AddDataProperty(lateBound, "Value", fixture.MainModule.TypeSystem.Int32);
    var fieldPayload = AddDataType(fixture.MainModule, "FieldPayload");
    AddDataProperty(fieldPayload, "Value", fixture.MainModule.TypeSystem.Int32);
    lateBound.Fields.Add(new FieldDefinition("Payload", FieldAttributes.Public, fieldPayload));
    var propertyPayload = AddDataType(fixture.MainModule, "PropertyPayload");
    AddDataProperty(propertyPayload, "Value", fixture.MainModule.TypeSystem.Int32);
    AddDataProperty(lateBound, "Child", propertyPayload, stored: false);
    var sink = new AssemblyNameReference("System.ILDietFixtureSink", new Version(1, 0)); fixture.MainModule.AssemblyReferences.Add(sink);
    var opaque = new TypeReference("System.ILDietFixture", "OpaqueList`1", fixture.MainModule, sink);
    opaque.GenericParameters.Add(new GenericParameter("T", opaque));
    var generic = new GenericInstanceType(opaque);
    generic.GenericArguments.Add(lateBound);
    var constructor = new MethodDefinition(".ctor",
        MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName,
        fixture.MainModule.TypeSystem.Void) { HasThis = true };
    constructor.Parameters.Add(new ParameterDefinition(generic));
    constructor.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
    lateBound.Methods.Add(constructor);
    collision.Fields.Add(new FieldDefinition("Items", FieldAttributes.Public, generic));
    var methodBound = new TypeDefinition("System.ILDietFixture", "MethodBound",
        TypeAttributes.Public | TypeAttributes.Class, fixture.MainModule.TypeSystem.Object);
    fixture.MainModule.Types.Add(methodBound);
    AddDataProperty(methodBound, "Value", fixture.MainModule.TypeSystem.Int32);
    var constructorPayload = AddDataType(fixture.MainModule, "ConstructorPayload");
    AddDataProperty(constructorPayload, "Value", fixture.MainModule.TypeSystem.Int32);
    var leaf = AddVoidMethod(methodBound, "ConstructorLeaf");
    leaf.Attributes = MethodAttributes.Private | MethodAttributes.Static;
    var methodConstructor = new MethodDefinition(".ctor",
        MethodAttributes.Private | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName,
        fixture.MainModule.TypeSystem.Void) { HasThis = true };
    methodConstructor.Parameters.Add(new ParameterDefinition(constructorPayload));
    methodConstructor.Body.Instructions.Add(Instruction.Create(OpCodes.Call, leaf));
    methodConstructor.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
    methodBound.Methods.Add(methodConstructor);
    var genericMethod = AddVoidMethod(collision, "GenericSelected");
    var genericParameter = new GenericParameter("T", genericMethod); genericMethod.GenericParameters.Add(genericParameter);
    var opaqueUse = new GenericInstanceType(opaque); opaqueUse.GenericArguments.Add(genericParameter);
    genericMethod.Body.Instructions.Clear();
    genericMethod.Body.Instructions.Add(Instruction.Create(OpCodes.Newobj, new MethodReference(".ctor", fixture.MainModule.TypeSystem.Void, opaqueUse) { HasThis = true }));
    genericMethod.Body.Instructions.Add(Instruction.Create(OpCodes.Pop)); genericMethod.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
    var emptyMethod = AddVoidMethod(collision, "EmptySelected"); emptyMethod.GenericParameters.Add(new GenericParameter("T", emptyMethod));
    var emptyBound = AddDataType(fixture.MainModule, "EmptyBound");
    var emptyLeaf = AddVoidMethod(emptyBound, "EmptyConstructorLeaf"); emptyLeaf.Attributes = MethodAttributes.Private | MethodAttributes.Static;
    var emptyCtor = new MethodDefinition(".ctor", MethodAttributes.Private | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName, fixture.MainModule.TypeSystem.Void);
    emptyCtor.Parameters.Add(new ParameterDefinition(fixture.MainModule.TypeSystem.Int32)); emptyBound.Methods.Add(emptyCtor);
    emptyCtor.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
    emptyCtor.Body.Instructions.Add(Instruction.Create(OpCodes.Call, new MethodReference(".ctor", fixture.MainModule.TypeSystem.Void, fixture.MainModule.TypeSystem.Object) { HasThis = true }));
    emptyCtor.Body.Instructions.Add(Instruction.Create(OpCodes.Call, emptyLeaf)); emptyCtor.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
    if (nested)
    {
        var owner = new TypeDefinition("System.ILDietFixture", "Owner",
            TypeAttributes.Public | TypeAttributes.Class, fixture.MainModule.TypeSystem.Object);
        var inner = new TypeDefinition("", "Inner", TypeAttributes.NestedPublic | TypeAttributes.Class,
            fixture.MainModule.TypeSystem.Object);
        owner.NestedTypes.Add(inner);
        fixture.MainModule.Types.Add(owner);
        AddVoidMethod(inner, "NestedSelected");
    }
    fixture.Write(path, new WriterParameters { Timestamp = 0, DeterministicMvid = true });
}

static void CreateResolverApp(string path)
{
    using var fixture = AssemblyDefinition.CreateAssembly(new AssemblyNameDefinition("ResolverApp", new Version(1, 0)),
        "ResolverApp", ModuleKind.Console);
    var program = new TypeDefinition("ILDietFixture", "ResolverProgram",
        TypeAttributes.Public | TypeAttributes.Class, fixture.MainModule.TypeSystem.Object);
    fixture.MainModule.Types.Add(program);
    var main = AddVoidMethod(program, "Main");
    main.Attributes |= MethodAttributes.Static;
    fixture.EntryPoint = main;

    var facade = new AssemblyNameReference("System.ILDietMissingFacade", new Version(1, 0));
    fixture.MainModule.AssemblyReferences.Add(facade);
    var collision = new TypeReference("System.ILDietFixture", "Collision", fixture.MainModule, facade);
    var owner = new TypeReference("System.ILDietFixture", "Owner", fixture.MainModule, facade);
    var inner = new TypeReference("", "Inner", fixture.MainModule, facade) { DeclaringType = owner };
    var missing = new TypeReference("System.ILDietFixture", "Missing", fixture.MainModule, facade);
    main.Body.Variables.Add(new VariableDefinition(missing));
    main.Body.Instructions.Add(Instruction.Create(OpCodes.Call,
        new MethodReference("Selected", fixture.MainModule.TypeSystem.Void, collision) { HasThis = false }));
    main.Body.Instructions.Add(Instruction.Create(OpCodes.Call,
        new MethodReference("NestedSelected", fixture.MainModule.TypeSystem.Void, inner) { HasThis = false }));
    var genericMethod = new MethodReference("GenericSelected", fixture.MainModule.TypeSystem.Void, collision)
        { HasThis = false };
    genericMethod.GenericParameters.Add(new GenericParameter("T", genericMethod));
    var genericCall = new GenericInstanceMethod(genericMethod);
    genericCall.GenericArguments.Add(new TypeReference("System.ILDietFixture", "MethodBound", fixture.MainModule, facade));
    main.Body.Instructions.Add(Instruction.Create(OpCodes.Call, genericCall));
    var emptyMethod = new MethodReference("EmptySelected", fixture.MainModule.TypeSystem.Void, collision) { HasThis = false };
    emptyMethod.GenericParameters.Add(new GenericParameter("T", emptyMethod));
    var emptyCall = new GenericInstanceMethod(emptyMethod);
    emptyCall.GenericArguments.Add(new TypeReference("System.ILDietFixture", "EmptyBound", fixture.MainModule, facade));
    main.Body.Instructions.Add(Instruction.Create(OpCodes.Call, emptyCall));
    main.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
    fixture.Write(path, new WriterParameters { Timestamp = 0, DeterministicMvid = true });
}

static MethodDefinition AddVoidMethod(TypeDefinition type, string name)
{
    var method = new MethodDefinition(name, MethodAttributes.Public | MethodAttributes.Static,
        type.Module.TypeSystem.Void);
    type.Methods.Add(method);
    method.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
    return method;
}

static TypeDefinition AddDataType(ModuleDefinition module, string name)
{
    var type = new TypeDefinition("System.ILDietFixture", name,
        TypeAttributes.Public | TypeAttributes.Class, module.TypeSystem.Object);
    module.Types.Add(type);
    return type;
}

static void AddDataProperty(TypeDefinition type, string name, TypeReference valueType, bool stored = true)
{
    var field = new FieldDefinition("_" + name, FieldAttributes.Private, valueType);
    if (stored) type.Fields.Add(field);
    var getter = new MethodDefinition("get_" + name,
        MethodAttributes.Public | MethodAttributes.SpecialName, valueType) { HasThis = true };
    if (stored)
    {
        getter.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
        getter.Body.Instructions.Add(Instruction.Create(OpCodes.Ldfld, field));
    }
    else getter.Body.Instructions.Add(Instruction.Create(OpCodes.Ldnull));
    getter.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
    var setter = new MethodDefinition("set_" + name,
        MethodAttributes.Private | MethodAttributes.SpecialName, type.Module.TypeSystem.Void) { HasThis = true };
    setter.Parameters.Add(new ParameterDefinition(valueType));
    if (stored)
    {
        setter.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
        setter.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_1));
        setter.Body.Instructions.Add(Instruction.Create(OpCodes.Stfld, field));
    }
    var leaf = AddVoidMethod(type, name + "SetterLeaf");
    leaf.Attributes = MethodAttributes.Private | MethodAttributes.Static;
    setter.Body.Instructions.Add(Instruction.Create(OpCodes.Call, leaf));
    setter.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
    type.Methods.Add(getter);
    type.Methods.Add(setter);
    type.Properties.Add(new PropertyDefinition(name, PropertyAttributes.None, valueType)
        { GetMethod = getter, SetMethod = setter });
}

static string InitializerBody(MethodDefinition method) => string.Join("|", method.Body.Instructions.Select(i =>
    i.OpCode.Code + ":" + (i.Operand is MemberReference member ? member.FullName : i.Operand?.ToString())));

static void CreateInitializerFixture(string path, bool late)
{
    Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
    using var fixture = AssemblyDefinition.CreateAssembly(new AssemblyNameDefinition("InitializerFixture", new Version(1, 0)),
        "InitializerFixture", ModuleKind.Console);
    var module = fixture.MainModule;
    var owner = new TypeDefinition("InitializerFixture", "Owner", TypeAttributes.NotPublic | TypeAttributes.Class,
        module.ImportReference(typeof(object)));
    module.Types.Add(owner);
    var suppressed = AddCell("SuppressedCell", "SuppressedDependency", false);
    AddCell("OrdinaryCell", "OrdinaryDependency", false);
    var generic = AddCell("GenericCell`1", "GenericDependency", true);
    var argument = new GenericInstanceType(generic);
    argument.GenericArguments.Add(module.TypeSystem.Int32);
    var dead = new MethodDefinition("Dead", MethodAttributes.Private | MethodAttributes.Static, suppressed);
    owner.Methods.Add(dead);
    var bodyOnly = new TypeDefinition("InitializerFixture", "BodyOnlyDependency", TypeAttributes.NotPublic | TypeAttributes.Class,
        module.ImportReference(typeof(object)));
    module.Types.Add(bodyOnly);
    var unused = new MethodDefinition("Uncalled", MethodAttributes.Public | MethodAttributes.Static, module.TypeSystem.Void);
    bodyOnly.Methods.Add(unused);
    unused.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
    dead.Body.Instructions.Add(Instruction.Create(OpCodes.Call, unused));
    dead.Body.Instructions.Add(Instruction.Create(OpCodes.Ldnull));
    dead.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
    var genericSignature = new MethodDefinition("GenericSignature", MethodAttributes.Private | MethodAttributes.Static, argument);
    owner.Methods.Add(genericSignature);
    genericSignature.Body.Instructions.Add(Instruction.Create(OpCodes.Ldnull));
    genericSignature.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
    var touch = new MethodDefinition("Touch", MethodAttributes.Private | MethodAttributes.Static, module.TypeSystem.Int32);
    owner.Methods.Add(touch);
    touch.Body.Instructions.Add(Instruction.Create(OpCodes.Ldsfld, suppressed.Fields.Single()));
    touch.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
    var main = new MethodDefinition("Main", MethodAttributes.Public | MethodAttributes.Static, module.TypeSystem.Void);
    owner.Methods.Add(main);
    module.EntryPoint = main;
    var il = main.Body.GetILProcessor();
    var invariant = module.ImportReference(typeof(CultureInfo).GetProperty(nameof(CultureInfo.InvariantCulture))!.GetMethod!);
    il.Emit(OpCodes.Call, invariant);
    il.Emit(OpCodes.Call, module.ImportReference(typeof(CultureInfo).GetProperty(nameof(CultureInfo.CurrentCulture))!.SetMethod!));
    il.Emit(OpCodes.Call, invariant);
    il.Emit(OpCodes.Call, module.ImportReference(typeof(CultureInfo).GetProperty(nameof(CultureInfo.CurrentUICulture))!.SetMethod!));
    if (late) { il.Emit(OpCodes.Call, touch); il.Emit(OpCodes.Pop); }
    il.Emit(OpCodes.Ldstr, "initializer-startup-end");
    il.Emit(OpCodes.Call, module.ImportReference(typeof(Console).GetMethod(nameof(Console.WriteLine), new[] { typeof(string) })!));
    il.Emit(OpCodes.Ret);
    fixture.Write(path, new WriterParameters { Timestamp = 0, DeterministicMvid = true });
    File.Copy(Path.ChangeExtension(System.Reflection.Assembly.GetExecutingAssembly().Location, ".runtimeconfig.json"),
        Path.ChangeExtension(path, ".runtimeconfig.json"), true);
    File.WriteAllText(Path.ChangeExtension(path, ".deps.json"),
        "{\"runtimeTarget\":{\"name\":\".NETCoreApp,Version=v10.0\"},\"targets\":{\".NETCoreApp,Version=v10.0\":{}},\"libraries\":{}}");

    TypeDefinition AddCell(string name, string dependencyName, bool genericType)
    {
        var cell = new TypeDefinition("InitializerFixture", name, TypeAttributes.NotPublic | TypeAttributes.Class,
            module.ImportReference(typeof(object)));
        module.Types.Add(cell);
        if (genericType) cell.GenericParameters.Add(new GenericParameter("T", cell));
        var value = new FieldDefinition("Value", FieldAttributes.Public | FieldAttributes.Static, module.TypeSystem.Int32);
        cell.Fields.Add(value);
        var dependency = new TypeDefinition("InitializerFixture", dependencyName, TypeAttributes.NotPublic | TypeAttributes.Class,
            module.ImportReference(typeof(object)));
        module.Types.Add(dependency);
        var read = new MethodDefinition("Read", MethodAttributes.Public | MethodAttributes.Static, module.TypeSystem.Int32);
        dependency.Methods.Add(read);
        read.Body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4, 7));
        read.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
        var initializer = new MethodDefinition(".cctor", MethodAttributes.Private | MethodAttributes.Static
            | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName, module.TypeSystem.Void);
        cell.Methods.Add(initializer);
        initializer.Body.Instructions.Add(Instruction.Create(OpCodes.Call, read));
        initializer.Body.Instructions.Add(Instruction.Create(OpCodes.Stsfld, value));
        initializer.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
        return cell;
    }
}

static void CheckBackendPolicyFixture(string originalDirectory, string outputDirectory)
{
    using var beforeApp = AssemblyDefinition.ReadAssembly(Path.Combine(originalDirectory, "PolicyApp.dll"));
    using var app = AssemblyDefinition.ReadAssembly(Path.Combine(outputDirectory, "PolicyApp.dll"));
    using var beforeLibrary = AssemblyDefinition.ReadAssembly(Path.Combine(originalDirectory, "PolicyLibrary.dll"));
    using var library = AssemblyDefinition.ReadAssembly(Path.Combine(outputDirectory, "PolicyLibrary.dll"));
    var unused = app.MainModule.GetType("PolicyFixture.SignatureScript");
    Require(unused is not null && unused.Methods.All(m => m.Name != ".cctor"), "signature-only registration opened an initializer");
    Require(unused!.Methods.Single(m => m.Name == "Callback").Body.Instructions.Select(i => i.OpCode.Code)
        .SequenceEqual(new[] { Code.Ldc_I4, Code.Pop, Code.Ldnull, Code.Throw }), "signature-only callback became executable");
    Require(app.MainModule.GetType("PolicyFixture.BodyOnlyDependency") is null, "unused signature opened its body dependency");
    foreach (string name in new[] { "ExplicitScript", "CodeScript", "ScalarScript" })
    {
        var original = beforeApp.MainModule.GetType("PolicyFixture." + name);
        var retained = app.MainModule.GetType("PolicyFixture." + name);
        Require(retained is not null, "runtime policy type was removed: " + name);
        foreach (var method in original.Methods)
            Require(retained!.Methods.Any(m => m.FullName == method.FullName && InitializerBody(m) == InitializerBody(method)),
                "runtime policy body changed: " + method.FullName);
    }
    foreach (string name in new[] { "SignatureParent", "SignatureWrapper" })
    {
        var retained = library.MainModule.GetType("Policy." + name);
        Require(retained is not null, "wrapper signature was removed: " + name);
        Require(retained!.Methods.All(m => m.Name != ".ctor" && m.Name != "CallbackLeaf"), "signature-only wrapper body was rooted: " + name);
        Require(retained.Methods.Single(m => m.Name == "Callback").Body.Instructions.Select(i => i.OpCode.Code)
            .SequenceEqual(new[] { Code.Ldc_I4, Code.Pop, Code.Ldnull, Code.Throw }), "signature-only wrapper virtual body became executable: " + name);
    }
    var originalRegistry = beforeLibrary.MainModule.GetType("Policy.Registry");
    var registry = library.MainModule.GetType("Policy.Registry");
    foreach (string name in new[] { "Root", "Live", "AllocatedWrapper", "FallbackWrapper" })
    {
        var factory = originalRegistry.Methods.Single(m => m.Name == "Factory" + name);
        Require(registry.Methods.Any(m => m.FullName == factory.FullName && InitializerBody(m) == InitializerBody(factory)),
            "runtime or fallback factory changed: " + name);
    }
    Require(registry.Methods.All(m => m.Name != "FactorySignatureParent" && m.Name != "FactorySignatureWrapper"),
        "unused wrapper factory survived");
    var originalKeys = originalRegistry.Methods.Single(m => m.Name == ".cctor").Body.Instructions
        .Where(i => i.OpCode.Code == Code.Ldstr).Select(i => (string)i.Operand);
    var instructions = registry.Methods.Single(m => m.Name == ".cctor").Body.Instructions;
    Require(originalKeys.SequenceEqual(instructions.Where(i => i.OpCode.Code == Code.Ldstr).Select(i => (string)i.Operand)),
        "constructor registry keys changed");
    for (int i = 0; i + 1 < instructions.Count; i++)
        if (instructions[i].Operand is string key && key is "SignatureParent" or "SignatureWrapper")
            Require(instructions[i + 1].Operand is MethodReference target && target.Name == "FactoryLive",
                "signature-only ancestor intercepted a factory redirect: " + key);
    var array = app.CustomAttributes.Single(a => a.ConstructorArguments.Single().Value is CustomAttributeArgument[]);
    var registered = ((CustomAttributeArgument[])array.ConstructorArguments.Single().Value)
        .Select(a => ((TypeReference)a.Value).FullName);
    Require(registered.SequenceEqual(new[] { "PolicyFixture.ExplicitScript", "PolicyFixture.CodeScript", "PolicyFixture.ScalarScript" }),
        "registration did not distinguish metadata and runtime roots");
    var scalar = app.CustomAttributes.Single(a => a.ConstructorArguments.Single().Value is TypeReference);
    Require(((TypeReference)scalar.ConstructorArguments.Single().Value).FullName == "PolicyFixture.ScalarScript",
        "ordinary scalar attribute root changed");
    Console.WriteLine("backend-policy-metadata=signature-only,runtime-roots,ancestor-factory,allocation,fallback-factory,registration-array,scalar-root,body-dependencies");
}

static void CreateBackendPolicyFixture(string directory)
{
    Directory.CreateDirectory(directory);
    using var library = AssemblyDefinition.CreateAssembly(new AssemblyNameDefinition("PolicyLibrary", new Version(1, 0)),
        "PolicyLibrary", ModuleKind.Dll);
    var module = library.MainModule;
    TypeDefinition Wrapper(string name, TypeReference parent)
    {
        var type = new TypeDefinition("Policy", name, TypeAttributes.Public | TypeAttributes.Class, parent);
        module.Types.Add(type);
        AddPolicyMethod(type, ".ctor", module.TypeSystem.Void,
            MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName);
        var callback = AddPolicyMethod(type, "Callback", module.TypeSystem.Void,
            MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.NewSlot);
        var leaf = AddPolicyMethod(type, "CallbackLeaf", module.TypeSystem.Void, MethodAttributes.Private | MethodAttributes.Static);
        callback.Body.Instructions.Insert(0, Instruction.Create(OpCodes.Call, leaf));
        return type;
    }
    var root = Wrapper("Root", module.TypeSystem.Object);
    var live = Wrapper("Live", root);
    var parent = Wrapper("SignatureParent", live);
    var unused = Wrapper("SignatureWrapper", parent);
    var allocated = Wrapper("AllocatedWrapper", live);
    var fallback = Wrapper("FallbackWrapper", module.TypeSystem.Object);
    var registry = new TypeDefinition("Policy", "Registry", TypeAttributes.Public | TypeAttributes.Class, module.TypeSystem.Object);
    module.Types.Add(registry);
    var register = AddPolicyMethod(registry, "Register", module.TypeSystem.Void, MethodAttributes.Public | MethodAttributes.Static);
    register.Parameters.Add(new ParameterDefinition(module.TypeSystem.String));
    register.Parameters.Add(new ParameterDefinition(module.TypeSystem.IntPtr));
    var cctor = AddPolicyMethod(registry, ".cctor", module.TypeSystem.Void,
        MethodAttributes.Private | MethodAttributes.Static | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName);
    cctor.Body.Instructions.Clear();
    foreach (var type in new[] { root, live, parent, unused, allocated, fallback })
    {
        var factory = AddPolicyMethod(registry, "Factory" + type.Name, module.TypeSystem.Object, MethodAttributes.Private | MethodAttributes.Static);
        factory.Body.Instructions.Clear();
        factory.Body.Instructions.Add(Instruction.Create(OpCodes.Newobj, type.Methods.Single(m => m.Name == ".ctor")));
        factory.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
        cctor.Body.Instructions.Add(Instruction.Create(OpCodes.Ldstr, type.Name));
        cctor.Body.Instructions.Add(Instruction.Create(OpCodes.Ldftn, factory));
        cctor.Body.Instructions.Add(Instruction.Create(OpCodes.Call, register));
    }
    cctor.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
    var attribute = new TypeDefinition("Policy", "RegistrationsAttribute", TypeAttributes.Public | TypeAttributes.Class,
        module.ImportReference(typeof(Attribute)));
    module.Types.Add(attribute);
    var arrayCtor = AddPolicyMethod(attribute, ".ctor", module.TypeSystem.Void,
        MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName);
    arrayCtor.Parameters.Add(new ParameterDefinition(new ArrayType(module.ImportReference(typeof(Type)))));
    var scalarCtor = AddPolicyMethod(attribute, ".ctor", module.TypeSystem.Void,
        MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName);
    scalarCtor.Parameters.Add(new ParameterDefinition(module.ImportReference(typeof(Type))));
    library.Write(Path.Combine(directory, "PolicyLibrary.dll"), new WriterParameters { Timestamp = 0, DeterministicMvid = true });
    using var app = AssemblyDefinition.CreateAssembly(new AssemblyNameDefinition("PolicyApp", new Version(1, 0)),
        "PolicyApp", ModuleKind.Console);
    var appModule = app.MainModule;
    TypeDefinition Script(string name)
    {
        var type = new TypeDefinition("PolicyFixture", name, TypeAttributes.Public | TypeAttributes.Class, appModule.ImportReference(live));
        appModule.Types.Add(type);
        AddPolicyMethod(type, ".ctor", appModule.TypeSystem.Void,
            MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName);
        AddPolicyMethod(type, ".cctor", appModule.TypeSystem.Void,
            MethodAttributes.Private | MethodAttributes.Static | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName);
        var callback = AddPolicyMethod(type, "Callback", appModule.TypeSystem.Void, MethodAttributes.Public);
        var leaf = AddPolicyMethod(type, "CallbackLeaf", appModule.TypeSystem.Void, MethodAttributes.Private | MethodAttributes.Static);
        callback.Body.Instructions.Insert(0, Instruction.Create(OpCodes.Call, leaf));
        return type;
    }
    var signature = Script("SignatureScript");
    var explicitScript = Script("ExplicitScript");
    var codeScript = Script("CodeScript");
    var scalarScript = Script("ScalarScript");
    var program = new TypeDefinition("PolicyFixture", "Program", TypeAttributes.Public | TypeAttributes.Class, appModule.TypeSystem.Object);
    appModule.Types.Add(program);
    var main = AddPolicyMethod(program, "Main", appModule.TypeSystem.Void, MethodAttributes.Public | MethodAttributes.Static);
    appModule.EntryPoint = main;
    foreach (var type in new[] { allocated, codeScript })
    {
        main.Body.Instructions.Insert(main.Body.Instructions.Count - 1,
            Instruction.Create(OpCodes.Newobj, appModule.ImportReference(type.Methods.Single(m => m.Name == ".ctor"))));
        main.Body.Instructions.Insert(main.Body.Instructions.Count - 1, Instruction.Create(OpCodes.Pop));
    }
    var dead = AddPolicyMethod(program, "Uncalled", appModule.TypeSystem.Void, MethodAttributes.Public | MethodAttributes.Static);
    dead.Parameters.Add(new ParameterDefinition(appModule.ImportReference(unused)));
    dead.Parameters.Add(new ParameterDefinition(signature));
    var dependency = new TypeDefinition("PolicyFixture", "BodyOnlyDependency", TypeAttributes.NotPublic | TypeAttributes.Class, appModule.TypeSystem.Object);
    appModule.Types.Add(dependency);
    var leafDependency = AddPolicyMethod(dependency, "Read", appModule.TypeSystem.Void, MethodAttributes.Public | MethodAttributes.Static);
    dead.Body.Instructions.Insert(0, Instruction.Create(OpCodes.Call, leafDependency));
    var systemType = appModule.ImportReference(typeof(Type));
    var arrayAttribute = new CustomAttribute(appModule.ImportReference(arrayCtor));
    arrayAttribute.ConstructorArguments.Add(new CustomAttributeArgument(new ArrayType(systemType),
        new[] { signature, explicitScript, codeScript, scalarScript }.Select(t => new CustomAttributeArgument(systemType, t)).ToArray()));
    app.CustomAttributes.Add(arrayAttribute);
    var scalarAttribute = new CustomAttribute(appModule.ImportReference(scalarCtor));
    scalarAttribute.ConstructorArguments.Add(new CustomAttributeArgument(systemType, scalarScript));
    app.CustomAttributes.Add(scalarAttribute);
    app.Write(Path.Combine(directory, "PolicyApp.dll"), new WriterParameters { Timestamp = 0, DeterministicMvid = true });
    Console.WriteLine("fixture-created=backend-policy");
}

static MethodDefinition AddPolicyMethod(TypeDefinition type, string name, TypeReference result, MethodAttributes attributes)
{
    var method = new MethodDefinition(name, attributes, result);
    type.Methods.Add(method);
    method.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
    return method;
}

static void CreateLocalDefaultsFixture(string input, string output)
{
    using var fixture = AssemblyDefinition.ReadAssembly(input);
    var module = fixture.MainModule;
    var originalMain = module.EntryPoint;
    var owner = originalMain.DeclaringType;
    var writeLine = module.ImportReference(typeof(Console).GetMethod(nameof(Console.WriteLine), new[] { typeof(string) })!);
    var getType = module.ImportReference(typeof(Type).GetMethod(nameof(System.Type.GetTypeFromHandle))!);
    var getAssembly = module.ImportReference(typeof(Type).GetProperty(nameof(System.Type.Assembly))!.GetMethod!);
    var namedType = module.ImportReference(typeof(System.Reflection.Assembly).GetMethod(nameof(System.Reflection.Assembly.GetType),
        new[] { typeof(string), typeof(bool), typeof(bool) })!);
    var create = module.ImportReference(typeof(Activator).GetMethod(nameof(Activator.CreateInstance), new[] { typeof(Type) })!);
    var createArgs = module.ImportReference(typeof(Activator).GetMethod(nameof(Activator.CreateInstance),
        new[] { typeof(Type), typeof(object[]) })!);
    var caseTarget = Target("InitializedCaseTarget", false);
    var argsTarget = Target("InitializedArgumentsTarget", true);

    var caseLocal = AddMethod("InitializedCaseLocal");
    caseLocal.Body.InitLocals = true;
    caseLocal.Body.Variables.Add(new VariableDefinition(module.TypeSystem.Boolean));
    EmitNamedConstruction(caseLocal, caseTarget);

    var joined = AddMethod("JoinedArgumentsLocal");
    joined.Parameters.Add(new ParameterDefinition(module.TypeSystem.Boolean));
    joined.Body.InitLocals = true;
    joined.Body.Variables.Add(new VariableDefinition(new ArrayType(module.TypeSystem.Object)));
    var il = joined.Body.GetILProcessor();
    var lookup = Instruction.Create(OpCodes.Ldtoken, argsTarget);
    il.Emit(OpCodes.Ldarg_0);
    il.Emit(OpCodes.Brfalse, lookup);
    il.Emit(OpCodes.Ldc_I4_1);
    il.Emit(OpCodes.Newarr, module.TypeSystem.Object);
    il.Emit(OpCodes.Dup);
    il.Emit(OpCodes.Ldc_I4_0);
    il.Emit(OpCodes.Ldstr, "one");
    il.Emit(OpCodes.Stelem_Ref);
    il.Emit(OpCodes.Stloc_0);
    il.Append(lookup);
    il.Emit(OpCodes.Call, getType);
    il.Emit(OpCodes.Ldloc_0);
    il.Emit(OpCodes.Call, createArgs);
    il.Emit(OpCodes.Pop);
    il.Emit(OpCodes.Ret);

    var uninitialized = AddMethod("UninitializedCaseLocal");
    uninitialized.Parameters.Add(new ParameterDefinition(module.TypeSystem.Boolean));
    uninitialized.Body.InitLocals = false;
    uninitialized.Body.Variables.Add(new VariableDefinition(module.TypeSystem.Boolean));
    var end = Instruction.Create(OpCodes.Ret);
    il = uninitialized.Body.GetILProcessor();
    il.Emit(OpCodes.Ldarg_0);
    il.Emit(OpCodes.Brfalse, end);
    EmitNamedConstruction(uninitialized, caseTarget);
    il.Append(end);

    var main = AddMethod("RunWithLocalDefaults");
    module.EntryPoint = main;
    il = main.Body.GetILProcessor();
    il.Emit(OpCodes.Call, originalMain);
    il.Emit(OpCodes.Ldstr, "DN2CPP_BEFORE_LOCAL_DEFAULTS");
    il.Emit(OpCodes.Call, module.ImportReference(typeof(Environment).GetMethod(nameof(Environment.GetEnvironmentVariable),
        new[] { typeof(string) })!));
    il.Emit(OpCodes.Ldstr, "1");
    il.Emit(OpCodes.Call, module.ImportReference(typeof(string).GetMethod("op_Equality", new[] { typeof(string), typeof(string) })!));
    var done = Instruction.Create(OpCodes.Ret);
    il.Emit(OpCodes.Brtrue, done);
    Print(il, "== initialized local operands ==");
    il.Emit(OpCodes.Call, caseLocal);
    il.Emit(OpCodes.Ldc_I4_0);
    il.Emit(OpCodes.Call, joined);
    il.Emit(OpCodes.Ldc_I4_1);
    il.Emit(OpCodes.Call, joined);
    il.Emit(OpCodes.Ldc_I4_0);
    il.Emit(OpCodes.Call, uninitialized);
    Print(il, "uninitialized-local=skipped");
    Print(il, "initialized local operands end");
    il.Append(done);

    Require(caseLocal.Body.InitLocals && caseLocal.Body.Variables.Single().VariableType.MetadataType == MetadataType.Boolean
        && caseLocal.Body.Instructions.All(i => i.OpCode.Code != Code.Stloc_0), "Boolean local fixture lost its untouched default");
    Require(joined.Body.InitLocals && joined.Body.Variables.Single().VariableType is ArrayType
        && joined.Body.Instructions.Count(i => i.OpCode.Code == Code.Stloc_0) == 1,
        "array local fixture lost its null/allocated join");
    Require(!uninitialized.Body.InitLocals, "non-initialized local fixture became initialized");
    fixture.Write(output, new WriterParameters { Timestamp = 0, DeterministicMvid = true });
    Console.WriteLine("local-defaults-fixture=initialized-bool,joined-null-array,uninitialized-bool");

    MethodDefinition AddMethod(string name)
    {
        var method = new MethodDefinition(name, MethodAttributes.Private | MethodAttributes.Static, module.TypeSystem.Void);
        owner.Methods.Add(method);
        return method;
    }

    void EmitNamedConstruction(MethodDefinition method, TypeDefinition target)
    {
        var body = method.Body.GetILProcessor();
        body.Emit(OpCodes.Ldtoken, target);
        body.Emit(OpCodes.Call, getType);
        body.Emit(OpCodes.Callvirt, getAssembly);
        body.Emit(OpCodes.Ldstr, target.FullName);
        body.Emit(OpCodes.Ldc_I4_0);
        body.Emit(OpCodes.Ldloc_0);
        body.Emit(OpCodes.Callvirt, namedType);
        body.Emit(OpCodes.Call, create);
        body.Emit(OpCodes.Pop);
        body.Emit(OpCodes.Ret);
    }

    void Print(ILProcessor body, string text)
    {
        body.Emit(OpCodes.Ldstr, text);
        body.Emit(OpCodes.Call, writeLine);
    }

    TypeDefinition Target(string name, bool withArgument)
    {
        var type = new TypeDefinition("ILDietConstructorOnly", name,
            TypeAttributes.NotPublic | TypeAttributes.Sealed | TypeAttributes.Class, module.TypeSystem.Object);
        module.Types.Add(type);
        Constructor(0, "SelectedDefault", withArgument ? "args-local=default" : "case-local=default");
        if (withArgument)
            Constructor(1, "SelectedArgument", "args-local=one");
        Constructor(2, "UnselectedPair", "unselected-local-constructor");
        return type;

        void Constructor(int arity, string markerName, string text)
        {
            var marker = new MethodDefinition(markerName, MethodAttributes.Private | MethodAttributes.Static, module.TypeSystem.Void);
            type.Methods.Add(marker);
            Print(marker.Body.GetILProcessor(), text);
            marker.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
            var constructor = new MethodDefinition(".ctor", (arity == 2 ? MethodAttributes.Private : MethodAttributes.Public)
                | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName, module.TypeSystem.Void);
            for (int i = 0; i < arity; i++)
                constructor.Parameters.Add(new ParameterDefinition(module.TypeSystem.String));
            type.Methods.Add(constructor);
            var body = constructor.Body.GetILProcessor();
            body.Emit(OpCodes.Ldarg_0);
            body.Emit(OpCodes.Call, module.ImportReference(typeof(object).GetConstructor(System.Type.EmptyTypes)!));
            body.Emit(OpCodes.Call, marker);
            body.Emit(OpCodes.Ret);
        }
    }
}

static void CreateArrayNarrowingFixture(string input, string output, bool scalarDefaults = false, bool slotStorage = false)
{
    using var fixture = AssemblyDefinition.ReadAssembly(input);
    var module = fixture.MainModule;
    var originalMain = module.EntryPoint;
    var owner = originalMain.DeclaringType;
    var getType = module.ImportReference(typeof(Type).GetMethod(nameof(System.Type.GetTypeFromHandle))!);
    var lookup = module.ImportReference(typeof(Type).GetMethod(nameof(System.Type.GetMethod),
        new[] { typeof(string), typeof(int), typeof(Type[]) })!);
    var invoke = module.ImportReference(typeof(System.Reflection.MethodBase).GetMethod(nameof(System.Reflection.MethodBase.Invoke),
        new[] { typeof(object), typeof(object[]) })!);
    var write = module.ImportReference(typeof(Console).GetMethod(nameof(Console.WriteLine), new[] { typeof(object) })!);
    var closure = new TypeDefinition("", "<>c__DisplayClassArrayNarrowing",
        TypeAttributes.NestedPrivate | TypeAttributes.Sealed | TypeAttributes.Class, module.TypeSystem.Object);
    owner.NestedTypes.Add(closure);
    closure.CustomAttributes.Add(new CustomAttribute(module.ImportReference(
        typeof(System.Runtime.CompilerServices.CompilerGeneratedAttribute).GetConstructor(System.Type.EmptyTypes)!)));
    var closureCtor = new MethodDefinition(".ctor", MethodAttributes.Public | MethodAttributes.SpecialName
        | MethodAttributes.RTSpecialName, module.TypeSystem.Void);
    closure.Methods.Add(closureCtor);
    var ctorIL = closureCtor.Body.GetILProcessor();
    ctorIL.Emit(OpCodes.Ldarg_0);
    ctorIL.Emit(OpCodes.Call, module.ImportReference(typeof(object).GetConstructor(System.Type.EmptyTypes)!));
    ctorIL.Emit(OpCodes.Ret);
    var arrayField = new FieldDefinition("Array", FieldAttributes.Public, new ArrayType(module.TypeSystem.Byte));
    var scalarField = new FieldDefinition("Scalar", FieldAttributes.Public, module.TypeSystem.Byte);
    closure.Fields.Add(arrayField);
    closure.Fields.Add(scalarField);
    var cases = new List<MethodDefinition>();
    ArrayCase("ByteWrap", module.TypeSystem.Byte, 256, OpCodes.Stelem_I1, OpCodes.Ldelem_U1, 0);
    ArrayCase("WordWrap", module.TypeSystem.UInt16, 65536, OpCodes.Stelem_I2, OpCodes.Ldelem_U2, 0);
    ArrayCase("ByteSigned", module.TypeSystem.Byte, 255, OpCodes.Stelem_I1, OpCodes.Ldelem_I1, 1);
    ArrayCase("ByteUnsigned", module.TypeSystem.SByte, -1, OpCodes.Stelem_I1, OpCodes.Ldelem_U1, -255);
    ArrayCase("WordSigned", module.TypeSystem.UInt16, 65535, OpCodes.Stelem_I2, OpCodes.Ldelem_I2, 1);
    ArrayCase("WordUnsigned", module.TypeSystem.Int16, -1, OpCodes.Stelem_I2, OpCodes.Ldelem_U2, -65535);
    ArrayCase("GenericByte", module.TypeSystem.Byte, 256, OpCodes.Stelem_Any, OpCodes.Ldelem_Any, 0);
    ArrayCase("ByteSequence", module.TypeSystem.Byte, -1, OpCodes.Stelem_I1, OpCodes.Nop, -255);
    ArrayCase("ByteCopy", module.TypeSystem.Byte, -1, OpCodes.Stelem_I1, OpCodes.Nop, -255, copy: true);
    CapturedCase("CapturedArray", array: true);
    CapturedCase("CapturedScalar", array: false);
    var main = AddMethod(owner, "RunWithArrayNarrowing", module.TypeSystem.Void);
    module.EntryPoint = main;
    var il = main.Body.GetILProcessor();
    il.Emit(OpCodes.Call, originalMain);
    il.Emit(OpCodes.Ldstr, "DN2CPP_BEFORE_ARRAY_NARROWING");
    il.Emit(OpCodes.Call, module.ImportReference(typeof(Environment).GetMethod(nameof(Environment.GetEnvironmentVariable),
        new[] { typeof(string) })!));
    il.Emit(OpCodes.Ldstr, "1");
    il.Emit(OpCodes.Call, module.ImportReference(typeof(string).GetMethod("op_Equality", new[] { typeof(string), typeof(string) })!));
    var done = Instruction.Create(OpCodes.Ret);
    il.Emit(OpCodes.Brtrue, done);
    Print(il, "== integer storage operands ==");
    foreach (var method in cases)
    {
        var start = Instruction.Create(OpCodes.Call, method);
        var handler = Instruction.Create(OpCodes.Pop);
        var end = Instruction.Create(OpCodes.Nop);
        il.Append(start);
        il.Emit(OpCodes.Leave, end);
        il.Append(handler);
        Print(il, "integer-storage-error=" + method.Name);
        il.Emit(OpCodes.Leave, end);
        il.Append(end);
        main.Body.ExceptionHandlers.Add(new ExceptionHandler(ExceptionHandlerType.Catch)
        {
            CatchType = module.ImportReference(typeof(Exception)), TryStart = start,
            TryEnd = handler, HandlerStart = handler, HandlerEnd = end,
        });
    }
    Print(il, "integer storage operands end");
    il.Append(done);
    if (scalarDefaults)
        AddScalarDefaults();
    if (slotStorage)
        AddSlotStorage();
    fixture.Write(output, new WriterParameters { Timestamp = 0, DeterministicMvid = true });
    Console.WriteLine(slotStorage ? "slot-storage-fixture=locals,arguments,instance-arguments,wrap,signed,in-range" : scalarDefaults ? "captured-scalar-defaults-fixture=byte,int,array-reference,object-reference,generic-reference"
        : "array-narrowing-fixture=wrap,sign-extension,zero-extension,generic,sequence,copy,captured-array,captured-scalar");

    void AddSlotStorage()
    {
        var previousMain = module.EntryPoint;
        var slotMain = AddMethod(owner, "RunWithSlotStorage", module.TypeSystem.Void);
        module.EntryPoint = slotMain;
        var body = slotMain.Body.GetILProcessor();
        body.Emit(OpCodes.Call, previousMain);
        var end = Instruction.Create(OpCodes.Ret);
        foreach (string flag in new[] { "DN2CPP_BEFORE_SLOT_STORAGE", "DN2CPP_BEFORE_ARRAY_NARROWING", "DN2CPP_BEFORE_CAPTURED_SCALAR_DEFAULTS" })
        {
            body.Emit(OpCodes.Ldstr, flag);
            body.Emit(OpCodes.Call, module.ImportReference(typeof(Environment).GetMethod(nameof(Environment.GetEnvironmentVariable),
                new[] { typeof(string) })!));
            body.Emit(OpCodes.Ldstr, "1");
            body.Emit(OpCodes.Call, module.ImportReference(typeof(string).GetMethod("op_Equality", new[] { typeof(string), typeof(string) })!));
            body.Emit(OpCodes.Brtrue, end);
        }
        Print(body, "== local and argument integer storage ==");
        foreach (var type in new[] { module.TypeSystem.Byte, module.TypeSystem.SByte, module.TypeSystem.UInt16,
                module.TypeSystem.Int16, module.TypeSystem.Char })
        {
            bool signed = type == module.TypeSystem.SByte || type == module.TypeSystem.Int16;
            int width = type == module.TypeSystem.Byte || type == module.TypeSystem.SByte ? 256 : 65536;
            foreach (string storage in new[] { "Local", "Argument", "InstanceArgument" })
            {
                SlotCase(storage + type.Name + "Wrap", type, storage, width, 0);
                SlotCase(storage + type.Name + "Reload", type, storage, signed ? width - 1 : -1, signed ? -1 : width - 1);
                SlotCase(storage + type.Name + "InRange", type, storage, 7, 7);
            }
        }
        Print(body, "local and argument integer storage end");
        body.Append(end);

        void SlotCase(string name, TypeReference type, string storage, int stored, int expected)
        {
            bool instance = storage == "InstanceArgument";
            var method = AddMethod(instance ? closure : owner, "Narrowing" + name, module.TypeSystem.Void);
            if (instance)
            {
                method.Attributes = MethodAttributes.Public;
                method.HasThis = true;
            }
            var code = method.Body.GetILProcessor();
            if (storage == "Local")
            {
                var slot = new VariableDefinition(type);
                method.Body.Variables.Add(slot);
                code.Emit(OpCodes.Ldc_I4, stored);
                if (expected == 0) code.Emit(OpCodes.Stloc_0);
                else code.Emit(expected == 7 ? OpCodes.Stloc : OpCodes.Stloc_S, slot);
                code.Emit(OpCodes.Ldloc_0);
            }
            else
            {
                var slot = new ParameterDefinition(type);
                method.Parameters.Add(slot);
                code.Emit(OpCodes.Ldc_I4, stored);
                code.Emit(expected == 7 ? OpCodes.Starg : OpCodes.Starg_S, slot);
                code.Emit(instance ? OpCodes.Ldarg_1 : OpCodes.Ldarg_0);
            }
            code.Emit(OpCodes.Dup);
            Print(code, "slot-value=" + name);
            code.Emit(OpCodes.Call, module.ImportReference(typeof(Console).GetMethod(nameof(Console.WriteLine), new[] { typeof(int) })!));
            code.Emit(OpCodes.Ldc_I4, expected);
            code.Emit(OpCodes.Sub);
            Select(method, name);
            var start = Instruction.Create(OpCodes.Nop);
            var handler = Instruction.Create(OpCodes.Pop);
            var next = Instruction.Create(OpCodes.Nop);
            body.Append(start);
            if (instance) body.Emit(OpCodes.Newobj, closureCtor);
            if (storage != "Local") body.Emit(OpCodes.Ldc_I4_0);
            body.Emit(OpCodes.Call, method);
            body.Emit(OpCodes.Leave, next);
            body.Append(handler);
            Print(body, "slot-storage-error=" + name);
            body.Emit(OpCodes.Leave, next);
            body.Append(next);
            slotMain.Body.ExceptionHandlers.Add(new ExceptionHandler(ExceptionHandlerType.Catch)
            {
                CatchType = module.ImportReference(typeof(Exception)), TryStart = start,
                TryEnd = handler, HandlerStart = handler, HandlerEnd = next,
            });
        }
    }

    void AddScalarDefaults()
    {
        var oldMain = module.EntryPoint;
        var defaultsMain = AddMethod(owner, "RunWithCapturedScalarDefaults", module.TypeSystem.Void);
        module.EntryPoint = defaultsMain;
        var defaults = defaultsMain.Body.GetILProcessor();
        defaults.Emit(OpCodes.Call, oldMain);
        defaults.Emit(OpCodes.Ldstr, "DN2CPP_BEFORE_CAPTURED_SCALAR_DEFAULTS");
        defaults.Emit(OpCodes.Call, module.ImportReference(typeof(Environment).GetMethod(nameof(Environment.GetEnvironmentVariable),
            new[] { typeof(string) })!));
        defaults.Emit(OpCodes.Ldstr, "1");
        defaults.Emit(OpCodes.Call, module.ImportReference(typeof(string).GetMethod("op_Equality", new[] { typeof(string), typeof(string) })!));
        var endDefaults = Instruction.Create(OpCodes.Ret);
        defaults.Emit(OpCodes.Brtrue, endDefaults);
        defaults.Emit(OpCodes.Ldstr, "DN2CPP_BEFORE_ARRAY_NARROWING");
        defaults.Emit(OpCodes.Call, module.ImportReference(typeof(Environment).GetMethod(nameof(Environment.GetEnvironmentVariable),
            new[] { typeof(string) })!));
        defaults.Emit(OpCodes.Ldstr, "1");
        defaults.Emit(OpCodes.Call, module.ImportReference(typeof(string).GetMethod("op_Equality", new[] { typeof(string), typeof(string) })!));
        defaults.Emit(OpCodes.Brtrue, endDefaults);
        Print(defaults, "== captured scalar defaults ==");
        var methods = new List<MethodDefinition>();
        Integral("Byte", module.TypeSystem.Byte);
        Integral("Int", module.TypeSystem.Int32);
        Arguments();
        Receiver("Object", generic: false);
        Receiver("Generic", generic: true);
        foreach (var method in methods)
        {
            var start = Instruction.Create(OpCodes.Call, method);
            var handler = Instruction.Create(OpCodes.Pop);
            var end = Instruction.Create(OpCodes.Nop);
            defaults.Append(start);
            defaults.Emit(OpCodes.Leave, end);
            defaults.Append(handler);
            Print(defaults, "scalar-default-error=" + method.Name);
            defaults.Emit(OpCodes.Leave, end);
            defaults.Append(end);
            defaultsMain.Body.ExceptionHandlers.Add(new ExceptionHandler(ExceptionHandlerType.Catch)
            {
                CatchType = module.ImportReference(typeof(Exception)), TryStart = start,
                TryEnd = handler, HandlerStart = handler, HandlerEnd = end,
            });
        }
        Print(defaults, "captured scalar defaults end");
        defaults.Append(endDefaults);

        void Integral(string name, TypeReference type)
        {
            var field = new FieldDefinition("Default" + name, FieldAttributes.Public, type);
            closure.Fields.Add(field);
            var method = AddMethod(owner, "ScalarDefault" + name, module.TypeSystem.Void);
            methods.Add(method);
            method.Body.InitLocals = true;
            method.Body.Variables.Add(new VariableDefinition(closure));
            var body = method.Body.GetILProcessor();
            body.Emit(OpCodes.Newobj, closureCtor);
            body.Emit(OpCodes.Stloc_0);
            body.Emit(OpCodes.Ldloc_0);
            body.Emit(OpCodes.Ldfld, field);
            Select(method, "Default" + name, finish: false);
            body.Emit(OpCodes.Ldloc_0);
            body.Emit(OpCodes.Ldc_I4_1);
            body.Emit(OpCodes.Stfld, field);
            body.Emit(OpCodes.Ret);
        }

        void Arguments()
        {
            var captureOwner = new TypeDefinition("ILDietConstructorOnly", "ScalarArgumentsCaptureOwner",
                TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.Class, module.TypeSystem.Object);
            module.Types.Add(captureOwner);
            var argumentClosure = new TypeDefinition("", "<>c__DisplayClassArguments",
                TypeAttributes.NestedPublic | TypeAttributes.Sealed | TypeAttributes.Class, module.TypeSystem.Object);
            captureOwner.NestedTypes.Add(argumentClosure);
            argumentClosure.CustomAttributes.Add(new CustomAttribute(module.ImportReference(
                typeof(System.Runtime.CompilerServices.CompilerGeneratedAttribute).GetConstructor(System.Type.EmptyTypes)!)));
            var argumentCtor = new MethodDefinition(".ctor", MethodAttributes.Public | MethodAttributes.SpecialName
                | MethodAttributes.RTSpecialName, module.TypeSystem.Void);
            argumentClosure.Methods.Add(argumentCtor);
            var argumentCtorIL = argumentCtor.Body.GetILProcessor();
            argumentCtorIL.Emit(OpCodes.Ldarg_0);
            argumentCtorIL.Emit(OpCodes.Call, module.ImportReference(typeof(object).GetConstructor(System.Type.EmptyTypes)!));
            argumentCtorIL.Emit(OpCodes.Ret);
            var field = new FieldDefinition("DefaultArguments", FieldAttributes.Public, new ArrayType(module.TypeSystem.Object));
            argumentClosure.Fields.Add(field);
            var writeArguments = AddMethod(captureOwner, "WriteArguments", module.TypeSystem.Void);
            writeArguments.Attributes = MethodAttributes.Public | MethodAttributes.Static;
            writeArguments.Parameters.Add(new ParameterDefinition(argumentClosure));
            var writeArgumentsIL = writeArguments.Body.GetILProcessor();
            writeArgumentsIL.Emit(OpCodes.Ldarg_0);
            writeArgumentsIL.Emit(OpCodes.Ldc_I4_1);
            writeArgumentsIL.Emit(OpCodes.Newarr, module.TypeSystem.Object);
            writeArgumentsIL.Emit(OpCodes.Stfld, field);
            writeArgumentsIL.Emit(OpCodes.Ret);
            var target = new TypeDefinition("ILDietConstructorOnly", "ScalarDefaultArgumentsTarget",
                TypeAttributes.NotPublic | TypeAttributes.Sealed | TypeAttributes.Class, module.TypeSystem.Object);
            module.Types.Add(target);
            var objectCtor = module.ImportReference(typeof(object).GetConstructor(System.Type.EmptyTypes)!);
            for (int count = 0; count < 3; count++)
            {
                var constructor = new MethodDefinition(".ctor", (count == 2 ? MethodAttributes.Private : MethodAttributes.Public)
                    | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName, module.TypeSystem.Void);
                target.Methods.Add(constructor);
                for (int i = 0; i < count; i++)
                    constructor.Parameters.Add(new ParameterDefinition(module.TypeSystem.Object));
                var witness = AddMethod(target, count == 0 ? "SelectedDefault" : count == 1 ? "SelectedArgument" : "UnselectedPair",
                    module.TypeSystem.Void);
                var witnessIL = witness.Body.GetILProcessor();
                Print(witnessIL, "scalar-arguments=" + count);
                witnessIL.Emit(OpCodes.Ret);
                var ctor = constructor.Body.GetILProcessor();
                ctor.Emit(OpCodes.Ldarg_0);
                ctor.Emit(OpCodes.Call, objectCtor);
                ctor.Emit(OpCodes.Call, witness);
                ctor.Emit(OpCodes.Ret);
            }
            var method = AddMethod(owner, "ScalarDefaultArguments", module.TypeSystem.Void);
            methods.Add(method);
            method.Body.InitLocals = true;
            method.Body.Variables.Add(new VariableDefinition(argumentClosure));
            var body = method.Body.GetILProcessor();
            body.Emit(OpCodes.Newobj, argumentCtor);
            body.Emit(OpCodes.Stloc_0);
            Construct();
            body.Emit(OpCodes.Ldloc_0);
            body.Emit(OpCodes.Call, writeArguments);
            Construct();
            body.Emit(OpCodes.Ret);

            void Construct()
            {
                body.Emit(OpCodes.Ldtoken, target);
                body.Emit(OpCodes.Call, getType);
                body.Emit(OpCodes.Ldloc_0);
                body.Emit(OpCodes.Ldfld, field);
                body.Emit(OpCodes.Call, module.ImportReference(typeof(Activator).GetMethod(nameof(Activator.CreateInstance),
                    new[] { typeof(Type), typeof(object[]) })!));
                body.Emit(OpCodes.Pop);
            }
        }

        void Receiver(string name, bool generic)
        {
            var receiverClosure = new TypeDefinition("", "<>c__DisplayClassScalar" + name + (generic ? "`1" : ""),
                TypeAttributes.NestedPrivate | TypeAttributes.Sealed | TypeAttributes.Class, module.TypeSystem.Object);
            owner.NestedTypes.Add(receiverClosure);
            receiverClosure.CustomAttributes.Add(new CustomAttribute(module.ImportReference(
                typeof(System.Runtime.CompilerServices.CompilerGeneratedAttribute).GetConstructor(System.Type.EmptyTypes)!)));
            if (generic) receiverClosure.GenericParameters.Add(new GenericParameter("T", receiverClosure));
            var receiverField = new FieldDefinition("Receiver", FieldAttributes.Public,
                generic ? receiverClosure.GenericParameters[0] : module.TypeSystem.Object);
            receiverClosure.Fields.Add(receiverField);
            var ctor = new MethodDefinition(".ctor", MethodAttributes.Public | MethodAttributes.SpecialName
                | MethodAttributes.RTSpecialName, module.TypeSystem.Void);
            receiverClosure.Methods.Add(ctor);
            var ctorBody = ctor.Body.GetILProcessor();
            ctorBody.Emit(OpCodes.Ldarg_0);
            var objectCtor = module.ImportReference(typeof(object).GetConstructor(System.Type.EmptyTypes)!);
            ctorBody.Emit(OpCodes.Call, objectCtor);
            ctorBody.Emit(OpCodes.Ret);
            TypeReference instantiated = receiverClosure;
            MethodReference useCtor = ctor;
            FieldReference useField = receiverField;
            if (generic)
            {
                var closed = new GenericInstanceType(receiverClosure);
                closed.GenericArguments.Add(module.TypeSystem.Object);
                instantiated = closed;
                useCtor = new MethodReference(".ctor", module.TypeSystem.Void, closed) { HasThis = true };
                useField = new FieldReference(receiverField.Name, receiverField.FieldType, closed);
            }
            var method = AddMethod(owner, "ScalarDefault" + name + "Receiver", module.TypeSystem.Void);
            methods.Add(method);
            method.Body.InitLocals = true;
            method.Body.Variables.Add(new VariableDefinition(instantiated));
            var body = method.Body.GetILProcessor();
            body.Emit(OpCodes.Newobj, useCtor);
            body.Emit(OpCodes.Stloc_0);
            body.Emit(OpCodes.Ldc_I4_0);
            Select(method, "Default" + name + "Receiver", finish: false, receiver: useField);
            body.Emit(OpCodes.Ldloc_0);
            body.Emit(OpCodes.Newobj, objectCtor);
            body.Emit(OpCodes.Stfld, useField);
            body.Emit(OpCodes.Ret);
        }
    }

    void ArrayCase(string name, TypeReference element, int stored, OpCode store, OpCode load, int adjustment, bool copy = false)
    {
        var method = AddMethod(owner, "Narrowing" + name, module.TypeSystem.Void);
        cases.Add(method);
        var body = method.Body.GetILProcessor();
        body.Emit(OpCodes.Ldc_I4_1);
        body.Emit(OpCodes.Newarr, element);
        body.Emit(OpCodes.Dup);
        body.Emit(OpCodes.Ldc_I4_0);
        body.Emit(OpCodes.Ldc_I4, stored);
        if (store == OpCodes.Stelem_Any) body.Emit(store, element); else body.Emit(store);
        if (load == OpCodes.Nop)
        {
            if (copy) body.Emit(OpCodes.Call, Sequence("ToArray", element));
            body.Emit(OpCodes.Call, Sequence("First", element));
        }
        else
        {
            body.Emit(OpCodes.Ldc_I4_0);
            if (load == OpCodes.Ldelem_Any) body.Emit(load, element); else body.Emit(load);
        }
        if (adjustment != 0)
        {
            body.Emit(OpCodes.Ldc_I4, adjustment);
            body.Emit(OpCodes.Add);
        }
        Select(method, name);
    }

    void CapturedCase(string name, bool array)
    {
        var method = AddMethod(owner, "Narrowing" + name, module.TypeSystem.Void);
        cases.Add(method);
        method.Body.InitLocals = true;
        method.Body.Variables.Add(new VariableDefinition(closure));
        var body = method.Body.GetILProcessor();
        body.Emit(OpCodes.Newobj, closureCtor);
        body.Emit(OpCodes.Stloc_0);
        if (array)
        {
            body.Emit(OpCodes.Ldloc_0);
            body.Emit(OpCodes.Ldc_I4_1);
            body.Emit(OpCodes.Newarr, module.TypeSystem.Byte);
            body.Emit(OpCodes.Stfld, arrayField);
            body.Emit(OpCodes.Ldloc_0);
            body.Emit(OpCodes.Ldfld, arrayField);
            body.Emit(OpCodes.Ldc_I4_0);
            body.Emit(OpCodes.Ldc_I4, 256);
            body.Emit(OpCodes.Stelem_I1);
            body.Emit(OpCodes.Ldloc_0);
            body.Emit(OpCodes.Ldfld, arrayField);
            body.Emit(OpCodes.Ldc_I4_0);
            body.Emit(OpCodes.Ldelem_U1);
        }
        else
        {
            body.Emit(OpCodes.Ldloc_0);
            body.Emit(OpCodes.Ldc_I4, 256);
            body.Emit(OpCodes.Stfld, scalarField);
            body.Emit(OpCodes.Ldloc_0);
            body.Emit(OpCodes.Ldfld, scalarField);
        }
        Select(method, name);
    }

    void Select(MethodDefinition method, string name, bool finish = true, FieldReference? receiver = null)
    {
        var target = new TypeDefinition("ILDietConstructorOnly", "Narrowing" + name + "Target",
            TypeAttributes.NotPublic | TypeAttributes.Sealed | TypeAttributes.Class, module.TypeSystem.Object);
        module.Types.Add(target);
        var selected = AddMethod(target, "SelectedBody", module.TypeSystem.String);
        selected.Body.Instructions.Add(Instruction.Create(OpCodes.Ldstr, "integer-storage=" + name));
        selected.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
        var pick = AddMethod(target, "Pick", module.TypeSystem.String);
        pick.Attributes = MethodAttributes.Public | MethodAttributes.Static;
        pick.Body.Instructions.Add(Instruction.Create(OpCodes.Call, selected));
        pick.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
        var unselected = AddMethod(target, "UnselectedBody", module.TypeSystem.String);
        unselected.Body.Instructions.Add(Instruction.Create(OpCodes.Ldstr, "unselected-integer-storage"));
        unselected.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
        var other = AddMethod(target, "Pick", module.TypeSystem.String);
        other.Attributes = MethodAttributes.Public | MethodAttributes.Static;
        other.GenericParameters.Add(new GenericParameter("T", other));
        other.Body.Instructions.Add(Instruction.Create(OpCodes.Call, unselected));
        other.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
        method.Body.InitLocals = true;
        var count = new VariableDefinition(module.TypeSystem.Int32);
        method.Body.Variables.Add(count);
        var body = method.Body.GetILProcessor();
        body.Emit(OpCodes.Stloc, count);
        body.Emit(OpCodes.Ldtoken, target);
        body.Emit(OpCodes.Call, getType);
        body.Emit(OpCodes.Ldstr, "Pick");
        body.Emit(OpCodes.Ldloc, count);
        body.Emit(OpCodes.Ldc_I4_0);
        body.Emit(OpCodes.Newarr, module.ImportReference(typeof(Type)));
        body.Emit(OpCodes.Callvirt, lookup);
        if (receiver is null)
            body.Emit(OpCodes.Ldnull);
        else
        {
            body.Emit(OpCodes.Ldloc_0);
            body.Emit(OpCodes.Ldfld, receiver);
        }
        body.Emit(OpCodes.Ldnull);
        body.Emit(OpCodes.Callvirt, invoke);
        body.Emit(OpCodes.Call, write);
        if (finish) body.Emit(OpCodes.Ret);
    }

    MethodReference Sequence(string name, TypeReference element)
    {
        var reference = module.ImportReference(typeof(Enumerable).GetMethods().Single(m => m.Name == name
            && m.GetParameters().Length == 1 && m.GetGenericArguments().Length == 1));
        var method = new GenericInstanceMethod(reference);
        method.GenericArguments.Add(element);
        return method;
    }

    MethodDefinition AddMethod(TypeDefinition declaring, string name, TypeReference returns)
    {
        var method = new MethodDefinition(name, MethodAttributes.Private | MethodAttributes.Static, returns);
        declaring.Methods.Add(method);
        return method;
    }

    void Print(ILProcessor body, string text)
    {
        body.Emit(OpCodes.Ldstr, text);
        body.Emit(OpCodes.Call, write);
    }
}

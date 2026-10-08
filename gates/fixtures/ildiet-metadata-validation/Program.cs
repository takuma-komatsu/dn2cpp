using System.Globalization;
using Mono.Cecil;
using Mono.Cecil.Cil;

CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
if (GodotValidation.Run(args)) return;
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
        && dead.Body.Instructions.Select(i => i.OpCode.Code).SequenceEqual(new[] { Code.Ldnull, Code.Throw }),
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
    Require(selectedAssembly.MainModule.GetType("System.ILDietFixture.Collision")?.Methods.Any(m => m.Name == "Selected") == true,
        "first collision definition was not selected: " + selected);
    Require(otherAssembly.MainModule.GetType("System.ILDietFixture.Collision")?.Methods.Any(m => m.Name == "Selected") != true,
        "later collision definition was selected: " + other);
    Require(selectedAssembly.MainModule.GetType("System.ILDietFixture.LateBound")?.Methods.Any(m =>
            m.IsConstructor && m.Parameters.Count == 1) == true,
        "generic argument constructor was removed: " + selected);
    var methodBound = selectedAssembly.MainModule.GetType("System.ILDietFixture.MethodBound");
    Require(methodBound?.Methods.Any(m => m.IsConstructor && m.IsPrivate && m.Parameters.Count == 1) == true,
        "generic method argument constructor was removed: " + selected);
    Require(methodBound?.Methods.Any(m => m.Name == "ConstructorLeaf") == true,
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
        Require(dataType?.Methods.Any(m => m.Name == propertyName + "SetterLeaf") == true,
            "generic data setter body was not scanned: " + typeName + "." + propertyName);
    }
    Require(selectedAssembly.MainModule.GetType("System.ILDietFixture.Collision")?.Properties.Count == 0,
        "unselected ordinary property survived");
    var owner = firstAssembly.MainModule.GetType("System.ILDietFixture.Owner");
    Require(owner?.NestedTypes.SingleOrDefault(t => t.Name == "Inner")?.Methods.Any(m => m.Name == "NestedSelected") == true,
        "nested definition was not resolved");
    var main = resolverApp.EntryPoint;
    Require(main is not null && main.Body.Variables.Any(v => v.VariableType.FullName == "System.ILDietFixture.Missing"),
        "unresolved type metadata changed");
    Console.WriteLine("resolver-valid=" + selected);
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
    var generic = new GenericInstanceType(new TypeReference("System.Collections.Generic", "List`1",
        fixture.MainModule, fixture.MainModule.TypeSystem.CoreLibrary));
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
    genericMethod.GenericParameters.Add(new GenericParameter("T", genericMethod));
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
        .SequenceEqual(new[] { Code.Ldnull, Code.Throw }), "signature-only callback became executable");
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
            .SequenceEqual(new[] { Code.Ldnull, Code.Throw }), "signature-only wrapper virtual body became executable: " + name);
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

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

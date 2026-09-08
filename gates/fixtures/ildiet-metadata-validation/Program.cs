using System.Globalization;
using Mono.Cecil;
using Mono.Cecil.Cil;

CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
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

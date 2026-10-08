using System.Globalization;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
if (args.Length == 3 && args[0] == "--check-construction-only")
{
    var predicates = System.Reflection.Assembly.LoadFrom(Path.GetFullPath(args[2]))
        .GetType("Dn2Cpp.PreservationReader", true)!;
    var checks = new[] { "RunsReflectedMethod", "BindsReflectedMethod", "ReadsReflectedField" }
        .Select(name => predicates.GetMethod(name,
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!).ToArray();
    using var input = File.OpenRead(args[1]);
    using var image = new PEReader(input);
    var metadata = image.GetMetadataReader();
    foreach (var handle in metadata.MemberReferences)
    {
        var member = metadata.GetMemberReference(handle);
        if (member.Parent.Kind != HandleKind.TypeReference) continue;
        var parent = metadata.GetTypeReference((TypeReferenceHandle)member.Parent);
        string ns = metadata.GetString(parent.Namespace), type = metadata.GetString(parent.Name);
        string full = ns.Length == 0 ? type : ns + "." + type;
        string name = metadata.GetString(member.Name);
        foreach (var check in checks)
            if ((bool)check.Invoke(null, new object[] { full, name })!)
                throw new InvalidOperationException("constructor fixture arms another reflection route: " + full + "::" + name);
    }
    Console.WriteLine("constructor-only descriptors=clean");
    return;
}
if (args.Length > 2 && args[0] == "--root-types")
{
    var policy = new Dn2Cpp.ILDietRootPolicy();
    policy.BaseTypes.Add(args[1]);
    policy.PreservePublicAppTypes = false;
    foreach (var root in policy.Resolve(args.Skip(2).ToArray()))
        Console.WriteLine(root.Assembly + ":" + root.Type);
    return;
}
if (args.Length == 4 && args[0] == "--lookup-event")
{
    var assembly = System.Reflection.Assembly.LoadFile(Path.GetFullPath(args[1]));
    var selected = assembly.GetType(args[2], true)!.GetEvent(args[3])!;
    Console.WriteLine("event-add=" + selected.GetAddMethod()!.Name);
    Console.WriteLine("event-remove=" + selected.GetRemoveMethod()!.Name);
    Console.WriteLine("event-raise=" + (selected.GetRaiseMethod() is null));
    Console.WriteLine("event-others=" + selected.GetOtherMethods(true).Length);
    return;
}
if (args.Length == 2 && args[0] == "--state-machine-bodies")
{
    var assembly = System.Reflection.Assembly.LoadFile(Path.GetFullPath(args[1]));
    var owner = assembly.GetType("ILDietLookupOnly.StateMachineMethods", true)!;
    foreach (string name in new[] { "UncalledAsync", "UncalledIterator", "UncalledAsyncIterator", "CalledAsync", "CalledIterator", "LateRegistration", "CalledRegistration" })
    {
        var method = owner.GetMethod(name)!;
        var attribute = method.GetCustomAttributes(typeof(System.Runtime.CompilerServices.StateMachineAttribute), false)
            .Cast<System.Runtime.CompilerServices.StateMachineAttribute>().Single();
        var moveNext = attribute.StateMachineType.GetMethod("MoveNext",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic)!;
        Console.WriteLine("state-machine " + name + "/method-stub=" + IsStub(method)
            + "/move-next-stub=" + IsStub(moveNext));
    }
    return;
}
if (args.Length != 1)
    throw new ArgumentException("expected one managed DLL path");
using var stream = File.OpenRead(args[0]);
using var pe = new PEReader(stream);
var reader = pe.GetMetadataReader();
var lines = new List<string>();
foreach (var handle in reader.TypeDefinitions)
{
    var type = reader.GetTypeDefinition(handle);
    string name = FullName(handle);
    lines.Add("type " + name);
    foreach (var field in type.GetFields())
        lines.Add("field " + name + "::" + reader.GetString(reader.GetFieldDefinition(field).Name));
    foreach (var method in type.GetMethods())
        lines.Add("method " + name + "::" + reader.GetString(reader.GetMethodDefinition(method).Name));
    foreach (var handleEvent in type.GetEvents())
    {
        var item = reader.GetEventDefinition(handleEvent);
        var accessors = item.GetAccessors();
        int count = (accessors.Adder.IsNil ? 0 : 1) + (accessors.Remover.IsNil ? 0 : 1)
            + (accessors.Raiser.IsNil ? 0 : 1) + accessors.Others.Length;
        lines.Add("event " + name + "::" + reader.GetString(item.Name) + "/accessors=" + count);
    }
    foreach (var handleProperty in type.GetProperties())
    {
        var property = reader.GetPropertyDefinition(handleProperty);
        var accessors = property.GetAccessors();
        int count = (accessors.Getter.IsNil ? 0 : 1) + (accessors.Setter.IsNil ? 0 : 1) + accessors.Others.Length;
        lines.Add("property " + name + "::" + reader.GetString(property.Name) + "/accessors=" + count);
    }
}
lines.Sort(StringComparer.Ordinal);
foreach (string line in lines)
    Console.WriteLine(line);

string FullName(TypeDefinitionHandle handle)
{
    var type = reader.GetTypeDefinition(handle);
    string name = reader.GetString(type.Name);
    if (!type.GetDeclaringType().IsNil)
        return FullName(type.GetDeclaringType()) + "+" + name;
    string ns = reader.GetString(type.Namespace);
    return ns.Length == 0 ? name : ns + "." + name;
}

bool IsStub(System.Reflection.MethodInfo method) =>
    method.GetMethodBody()!.GetILAsByteArray()!.SequenceEqual(new byte[] { 0x14, 0x7a });

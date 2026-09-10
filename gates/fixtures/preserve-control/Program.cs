using System.Globalization;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
if (args.Length > 2 && args[0] == "--root-types")
{
    var policy = new Dn2Cpp.ILDietRootPolicy();
    policy.BaseTypes.Add(args[1]);
    policy.PreservePublicAppTypes = false;
    foreach (var root in policy.Resolve(args.Skip(2).ToArray()))
        Console.WriteLine(root.Assembly + ":" + root.Type);
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

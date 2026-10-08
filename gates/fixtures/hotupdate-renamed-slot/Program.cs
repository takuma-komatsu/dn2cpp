using System.Globalization;
using Mono.Cecil;

CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;

if (args.Length != 1 && (args.Length != 2 || args[1] is not ("--object" or "--finalize")))
    throw new ArgumentException("expected HotUpdateBase.dll [--object|--finalize]");

string path = Path.GetFullPath(args[0]);
using var assembly = AssemblyDefinition.ReadAssembly(path, new ReaderParameters { InMemory = true });
var owner = assembly.MainModule.GetType("HotUpdateBase.RenamedMeasure")
    ?? throw new InvalidOperationException("missing renamed interface fixture");
var stub = owner.Methods.Single(m => m.Name == "HotUpdateBase.IRenamedMeasure.Measure");
var body = owner.Methods.Single(m => m.Name == "Weigh");
// MethodImpl, rather than its qualifier, selects the interface body. Rewriting
// an already rewritten assembly finds the declaration on that body.
if (stub.Overrides.Count != 0)
{
    body.Overrides.Add(stub.Overrides[0]);
    stub.Overrides.Clear();
}

if (args.Length == 2)
{
    string declarationName = args[1] == "--object" ? "ToString" : "Finalize";
    string bodyName = args[1] == "--object" ? "Show" : "Release";
    var renamed = owner.Methods.Single(m => m.Name == declarationName || m.Name == bodyName);
    renamed.Name = bodyName;
    renamed.Overrides.Clear();
    renamed.Overrides.Add(new MethodReference(declarationName, renamed.ReturnType,
        assembly.MainModule.TypeSystem.Object) { HasThis = true });
}

string temporary = path + ".renamed-slot.tmp";
try
{
    assembly.Write(temporary, new WriterParameters { Timestamp = 0, DeterministicMvid = true });
    File.Move(temporary, path, overwrite: true);
}
finally
{
    if (File.Exists(temporary))
        File.Delete(temporary);
}

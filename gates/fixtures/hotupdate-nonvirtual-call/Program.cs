using System.Globalization;
using Mono.Cecil;
using Mono.Cecil.Cil;

CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;

if (args.Length != 1)
    throw new ArgumentException("expected GvmCallPatch.dll");

// Every callvirt in HotGvmCall.NonVirtual becomes call, so each method names its
// import non-virtually. A second run over the rewritten assembly finds none.
string path = Path.GetFullPath(args[0]);
using var assembly = AssemblyDefinition.ReadAssembly(path, new ReaderParameters { InMemory = true });
var owner = assembly.MainModule.GetType("HotGvmCall.NonVirtual")
    ?? throw new InvalidOperationException("missing IL fixture owner");
foreach (var method in owner.Methods)
{
    if (!method.HasBody)
        continue;
    foreach (var insn in method.Body.Instructions)
    {
        if (insn.OpCode == OpCodes.Callvirt)
            insn.OpCode = OpCodes.Call;
    }
}

string temporary = path + ".nonvirtual-call.tmp";
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

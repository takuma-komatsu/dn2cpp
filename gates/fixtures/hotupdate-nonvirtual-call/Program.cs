using System.Globalization;
using Mono.Cecil;
using Mono.Cecil.Cil;

CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;

if (args.Length != 1 && (args.Length != 2 || args[1] != "--type-predicate-nops"))
    throw new ArgumentException("expected GvmCallPatch.dll or HotUpdateBase.dll --type-predicate-nops");

// Every callvirt in HotGvmCall.NonVirtual becomes call, so each method names its
// import non-virtually. A second run over the rewritten assembly finds none.
string path = Path.GetFullPath(args[0]);
using var assembly = AssemblyDefinition.ReadAssembly(path, new ReaderParameters { InMemory = true });
if (args.Length == 1)
{
    var owner = assembly.MainModule.GetType("HotGvmCall.NonVirtual")
        ?? throw new InvalidOperationException("missing IL fixture owner");
    foreach (var method in owner.Methods)
    {
        if (!method.HasBody)
            continue;
        foreach (var insn in method.Body.Instructions)
            if (insn.OpCode == OpCodes.Callvirt)
                insn.OpCode = OpCodes.Call;
    }
}
else
{
    var owner = assembly.MainModule.GetType("HotUpdateBase.Holder`1")
        ?? throw new InvalidOperationException("missing type predicate fixture owner");
    foreach (string name in new[] { "NopBeforeValue", "NopAfterPrimitive", "NopBeforeEnum", "NopAfterByRefLike", "NopBeforeClass" })
    {
        var method = owner.Methods.Single(m => m.Name == name);
        bool before = name.StartsWith("NopBefore", StringComparison.Ordinal);
        var target = method.Body.Instructions.Single(i => before ? i.OpCode == OpCodes.Ldtoken
            : i.OpCode == OpCodes.Call && i.Operand is MethodReference m && m.Name == "GetTypeFromHandle");
        if (target.Next.OpCode != OpCodes.Nop)
            method.Body.GetILProcessor().InsertAfter(target, Instruction.Create(OpCodes.Nop));
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

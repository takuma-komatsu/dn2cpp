using Mono.Cecil;
using Mono.Cecil.Cil;

internal static class GcFinalizerWorkerFixture
{
    public static bool Run(string[] args)
    {
        if (args.Length != 3 || args[0] != "--gc-finalizer-worker") return false;
        using var core = AssemblyDefinition.ReadAssembly(args[2]);
        var worker = core.MainModule.GetType("System.GC").Methods.Single(m => m.Name == "RunFinalizers");
        if (core.Name.Name != "System.Private.CoreLib" || !worker.IsPrivate || !worker.IsStatic
            || worker.HasGenericParameters || worker.Parameters.Count != 0
            || worker.ReturnType.MetadataType != MetadataType.UInt32 || !worker.HasBody
            || !worker.Body.Instructions.Any(i => i.Operand is MethodReference m
                && m.DeclaringType.FullName == "System.Threading.Thread" && m.Name == "ResetFinalizerThread"))
            throw new InvalidOperationException("The CoreLib-owned worker contract changed");
        string path = Path.GetFullPath(args[1]);
        using var app = AssemblyDefinition.ReadAssembly(path, new ReaderParameters { InMemory = true });
        var module = app.MainModule;
        var target = module.ImportReference(worker);
        IEnumerable<TypeDefinition> Types(TypeDefinition type) => new[] { type }.Concat(type.NestedTypes.SelectMany(Types));
        int rewritten = 0;
        foreach (var type in module.Types.SelectMany(Types))
        foreach (var method in type.Methods.Where(m => m.HasBody))
        foreach (var instruction in method.Body.Instructions)
        {
            if (instruction.Operand is not MethodReference m || m.Name != "WorkerEntry"
                || m.DeclaringType.FullName != "GcFinalizerWorkerSubset.Program") continue;
            if (instruction.OpCode.Code is not (Code.Call or Code.Ldftn))
                throw new InvalidOperationException("Unexpected worker fixture site");
            instruction.Operand = target;
            rewritten++;
        }
        if (rewritten != 3) throw new InvalidOperationException("Expected direct/delegate/pointer worker sites");
        app.Write(path, new WriterParameters { Timestamp = 0, DeterministicMvid = true });
        Console.WriteLine("gc-worker-source=CoreLib,private,static,nongeneric,uint32,zero,reset-thread");
        Console.WriteLine("gc-worker-sites=3");
        return true;
    }
}

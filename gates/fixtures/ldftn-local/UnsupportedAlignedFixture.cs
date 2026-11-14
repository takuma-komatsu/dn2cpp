using Mono.Cecil;
using Mono.Cecil.Cil;

internal static class UnsupportedAlignedFixture
{
    public static bool Run(string[] args)
    {
        if (args.Length != 4 || args[0] != "--unsupported-aligned") return false;
        if (File.Exists(args[2])) throw new InvalidOperationException("fixture output must be new");
        using var core = AssemblyDefinition.ReadAssembly(args[3]);
        using var app = AssemblyDefinition.ReadAssembly(args[1]);
        var module = app.MainModule;
        var sample = module.GetType("VectorProbe.UnsupportedAlignedBoundary`2")
            ?? throw new InvalidOperationException("missing Unsupported aligned fixture");
        var definition = core.MainModule.GetType("System.Runtime.Intrinsics.ISimdVector`2");
        var declaration = module.ImportReference(definition);
        var contract = new GenericInstanceType(declaration);
        contract.GenericArguments.Add(sample.GenericParameters[0]);
        contract.GenericArguments.Add(sample.GenericParameters[1]);
        var names = new[] { "LoadAligned", "StoreAligned" };
        int calls = 0, addresses = 0;
        foreach (var method in sample.Methods.Where(m => m.HasBody))
            foreach (var instruction in method.Body.Instructions)
            {
                if (instruction.Operand is not MethodReference target || target.DeclaringType.GetElementType().FullName != sample.FullName
                    || !names.Contains(target.Name)) continue;
                var actual = definition.Methods.Single(m => m.Name == target.Name);
                var replacement = module.ImportReference(actual);
                replacement.DeclaringType = contract;
                instruction.Operand = replacement;
                if (instruction.OpCode == OpCodes.Call) calls++;
                else if (instruction.OpCode == OpCodes.Ldftn) addresses++;
                else throw new InvalidOperationException("unexpected Unsupported aligned operand");
            }
        if (calls != 2 || addresses != 2) throw new InvalidOperationException("missing Unsupported aligned call/address coverage");
        app.Write(args[2], new WriterParameters { Timestamp = 0, DeterministicMvid = true });
        Console.WriteLine("unsupported aligned defaults: 2 calls, 2 addresses");
        return true;
    }
}

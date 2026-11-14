using Mono.Cecil;
using Mono.Cecil.Cil;

// Retargets DateTimeOps' DTFI day-name call to a MemberRef whose return type
// differs from CoreLib's declaration; the transpiler must refuse it.
internal static class AbbreviatedDayFixture
{
    public static bool Run(string[] args)
    {
        if (args.Length != 4 || args[0] != "--abbreviated-day-negative" || args[3] != "return")
            return false;
        using var core = AssemblyDefinition.ReadAssembly(args[2]);
        var day = core.MainModule.GetType("System.DayOfWeek");
        string path = Path.GetFullPath(args[1]);
        using var app = AssemblyDefinition.ReadAssembly(path, new ReaderParameters { InMemory = true });
        var module = app.MainModule;
        var call = module.GetType("DateTimeFormatInfoSubset.Program").Methods.Single(m => m.Name == "DayDirect")
            .Body.Instructions.Single(i => i.OpCode == OpCodes.Callvirt);
        var target = (MethodReference)call.Operand;
        var bad = new MethodReference(target.Name, module.TypeSystem.Object, target.DeclaringType) { HasThis = true };
        bad.Parameters.Add(new ParameterDefinition(module.ImportReference(day)));
        call.Operand = bad;
        app.Write(path, new WriterParameters { Timestamp = 0, DeterministicMvid = true });
        return true;
    }
}

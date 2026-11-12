using Mono.Cecil;
using Mono.Cecil.Cil;

internal static class DateTimeTCharFixture
{
    public static bool Run(string[] args)
    {
        if (args.Length != 3 || args[0] != "--datetime-tchar-negative")
            return false;
        using var core = AssemblyDefinition.ReadAssembly(args[2]);
        string path = Path.GetFullPath(args[1]);
        using var app = AssemblyDefinition.ReadAssembly(path, new ReaderParameters { InMemory = true });
        var module = app.MainModule;
        var call = module.GetType("DateTimeFormatInfoSubset.Program").Methods.Single(method => method.Name == "DateSpan")
            .Body.Instructions.Single(instruction => instruction.OpCode == OpCodes.Call);
        var original = (GenericInstanceMethod)call.Operand;
        var bad = new MethodReference(original.Name, module.TypeSystem.Void, original.DeclaringType) { HasThis = true };
        bad.GenericParameters.Add(new GenericParameter("TChar", bad));
        var returnType = new GenericInstanceType(module.ImportReference(core.MainModule.GetType("System.Span`1")));
        returnType.GenericArguments.Add(bad.GenericParameters[0]);
        bad.ReturnType = returnType;
        var specification = new GenericInstanceMethod(bad);
        specification.GenericArguments.Add(original.GenericArguments[0]);
        call.Operand = specification;
        app.Write(path, new WriterParameters { Timestamp = 0, DeterministicMvid = true });
        Console.WriteLine("DateSeparatorTChar return-owner=System.Span`1");
        return true;
    }
}

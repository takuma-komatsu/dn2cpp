using Mono.Cecil;

// Rewrites DecimalOps' Divide calls into a Decimal.DecDivMod1E9 MemberRef whose
// return type differs from CoreLib's declaration; the transpiler must refuse it.
internal static class DecimalDivModFixture
{
    public static bool Run(string[] args)
    {
        if (args.Length != 4 || args[0] != "--decimal-divmod" || args[3] != "return") return false;
        using var core = AssemblyDefinition.ReadAssembly(args[2]);
        var target = core.MainModule.Types.Single(t => t.FullName == "System.Decimal").Methods.Single(m => m.Name == "DecDivMod1E9");
        using var app = AssemblyDefinition.ReadAssembly(args[1], new ReaderParameters { InMemory = true });
        var imported = app.MainModule.ImportReference(target);
        imported.ReturnType = app.MainModule.TypeSystem.Int32;
        int rewrites = 0;
        foreach (var type in app.MainModule.Types)
        foreach (var method in type.Methods.Where(m => m.HasBody))
        foreach (var instruction in method.Body.Instructions)
            if (instruction.Operand is MethodReference { Name: "Divide" } reference
                && reference.DeclaringType.FullName == "Dn2Cpp.DecimalDivModSubset")
            {
                instruction.Operand = imported;
                rewrites++;
            }
        if (rewrites == 0) throw new InvalidOperationException("no Divide reference to rewrite");
        app.Write(args[1]);
        return true;
    }
}

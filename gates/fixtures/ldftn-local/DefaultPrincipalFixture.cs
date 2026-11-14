using System.Globalization;
using Mono.Cecil;
using Mono.Cecil.Cil;

// Points the probe's Direct stub at CoreLib's compiler-generated default-principal
// accessor. The probe references neither GenericPrincipal nor the Claims assembly.
internal static class DefaultPrincipalFixture
{
    public static bool Run(string[] args)
    {
        if (args.Length != 3 || args[0] != "--default-principal") return false;
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        using var core = AssemblyDefinition.ReadAssembly(args[2]);
        var target = core.MainModule.GetType("System.AppDomain").Methods
            .Single(m => m.Name == "<GetThreadPrincipal>g__GetDefaultPrincipal|107_0");
        string path = Path.GetFullPath(args[1]);
        using var app = AssemblyDefinition.ReadAssembly(path, new ReaderParameters { InMemory = true });
        var module = app.MainModule;
        var program = module.GetType("DefaultPrincipalSubset.Program");
        var direct = program.Methods.Single(m => m.Name == "Direct");
        direct.Body = new MethodBody(direct);
        var il = direct.Body.GetILProcessor();
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Call, module.ImportReference(target));
        il.Emit(OpCodes.Ret);
        module.EntryPoint = program.Methods.Single(m => m.Name == "RunArtifactChecks");
        app.Write(path, new WriterParameters { Timestamp = 0, DeterministicMvid = true });
        return true;
    }
}

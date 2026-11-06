using System.Globalization;
using Mono.Cecil;

internal static class DefaultPrincipalPublicSmokeFixture
{
    public static bool Run(string[] args)
    {
        if (args.Length != 3 || args[0] != "--default-principal-public-smoke") return false;
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        // Public managed sample only; never select a private accessor native fixture.
        string path = Path.GetFullPath(args[1]);
        using var app = AssemblyDefinition.ReadAssembly(path, new ReaderParameters { InMemory = true });
        var run = app.MainModule.GetType("DefaultPrincipalPublicSubset.Program").Methods.Single(m => m.Name == "Run" && m.IsStatic && m.Parameters.Count == 0 && m.ReturnType.MetadataType == MetadataType.Void);
        app.MainModule.EntryPoint = run;
        app.Write(path, new WriterParameters { Timestamp = 0, DeterministicMvid = true });
        return true;
    }
}

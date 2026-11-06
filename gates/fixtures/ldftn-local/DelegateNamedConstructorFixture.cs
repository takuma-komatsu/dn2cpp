using Mono.Cecil;
using Mono.Cecil.Cil;

internal static class DelegateNamedConstructorFixture
{
    public static bool Run(string[] args)
    {
        if (args.Length != 3 || args[0] != "--delegate-named-constructors") return false;
        using var core = AssemblyDefinition.ReadAssembly(args[2]);
        if (core.Name.Name != "System.Private.CoreLib") throw new InvalidOperationException("Expected input CoreLib");
        string path = Path.GetFullPath(args[1]);
        using var app = AssemblyDefinition.ReadAssembly(path, new ReaderParameters { InMemory = true });
        var module = app.MainModule;
        var replacements = new Dictionary<string, MethodReference>();
        foreach (string ownerName in new[] { "Delegate", "MulticastDelegate" })
        foreach (string parameter in new[] { "Object", "Type" })
        {
            var owner = core.MainModule.GetType("System." + ownerName);
            var ctor = owner.Methods.Single(m => m.Name == ".ctor" && m.Parameters.Count == 2
                && m.Parameters[0].ParameterType.FullName == "System." + parameter
                && m.Parameters[1].ParameterType.MetadataType == MetadataType.String);
            if (!ctor.IsFamily || ctor.IsStatic || !ctor.HasThis || ctor.HasGenericParameters
                || ctor.ReturnType.MetadataType != MetadataType.Void || !ctor.HasBody)
                throw new InvalidOperationException("Named constructor contract changed");
            replacements.Add("Stub" + (ownerName == "Delegate" ? "Delegate" : "Multicast") + parameter,
                module.ImportReference(ctor));
        }
        var program = module.GetType("MulticastNamedConstructorSubset.Program");
        foreach (var pair in replacements)
        {
            var stub = program.Methods.Single(m => m.Name == pair.Key);
            stub.Body = new MethodBody(stub);
            var il = stub.Body.GetILProcessor();
            il.Append(il.Create(OpCodes.Ldarg_0));
            il.Append(il.Create(OpCodes.Ldarg_1));
            il.Append(il.Create(OpCodes.Ldarg_2));
            il.Append(il.Create(OpCodes.Call, pair.Value));
            il.Append(il.Create(OpCodes.Ret));
        }
        module.EntryPoint = program.Methods.Single(m => m.Name == "RunArtifactChecks");
        app.Write(path, new WriterParameters { Timestamp = 0, DeterministicMvid = true });
        Console.WriteLine("named-ctor-sites=4-direct,isolated-entrypoint");
        return true;
    }
}

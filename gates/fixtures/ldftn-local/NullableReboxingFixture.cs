using Mono.Cecil;
using Mono.Cecil.Cil;

internal static class NullableReboxingFixture
{
    public static bool Run(string[] args)
    {
        if (args.Length != 3 || args[0] != "--nullable-reboxing") return false;
        using var core = AssemblyDefinition.ReadAssembly(args[2]);
        if (core.Name.Name != "System.Private.CoreLib") throw new InvalidOperationException("Expected input CoreLib");
        var owner = core.MainModule.GetType("System.RuntimeMethodHandle");
        string path = Path.GetFullPath(args[1]);
        using var app = AssemblyDefinition.ReadAssembly(path, new ReaderParameters { InMemory = true });
        var module = app.MainModule;
        var program = module.GetType("NullableReboxingSubset.Program");
        foreach (string direction in new[] { "From", "To" })
        {
            bool to = direction == "To";
            var method = owner.Methods.Single(m => m.Name == "Rebox" + direction + "Nullable");
            if (!method.IsAssembly || !method.IsStatic || method.HasThis || method.HasGenericParameters
                || !method.HasBody || method.IsInternalCall || method.IsPInvokeImpl
                || method.ReturnType.FullName != "System.Object" || method.Parameters.Count != (to ? 2 : 1)
                || method.Parameters[0].ParameterType.FullName != "System.Object"
                || to && method.Parameters[1].ParameterType.FullName != "System.RuntimeType")
                throw new InvalidOperationException("Nullable reboxing declaration changed: " + direction);
            foreach (string route in new[] { "Direct", "Pointer" })
            {
                var stub = program.Methods.Single(m => m.Name == direction + route);
                stub.Body = new MethodBody(stub);
                var il = stub.Body.GetILProcessor();
                var target = module.ImportReference(method);
                il.Emit(OpCodes.Ldarg_0);
                if (to)
                {
                    il.Emit(OpCodes.Ldarg_1);
                    il.Emit(OpCodes.Castclass, target.Parameters[1].ParameterType);
                }
                if (route == "Pointer")
                {
                    il.Emit(OpCodes.Ldftn, target);
                    var site = new CallSite(target.ReturnType);
                    foreach (var parameter in target.Parameters)
                        site.Parameters.Add(new ParameterDefinition(parameter.ParameterType));
                    il.Emit(OpCodes.Calli, site);
                }
                else il.Emit(OpCodes.Call, target);
                il.Emit(OpCodes.Ret);
            }
        }
        module.EntryPoint = program.Methods.Single(m => m.Name == "RunArtifactChecks");
        app.Write(path, new WriterParameters { Timestamp = 0, DeterministicMvid = true });
        Console.WriteLine("nullable-rebox-sites=2-direct,2-address,exact-CoreLib-signatures");
        return true;
    }
}

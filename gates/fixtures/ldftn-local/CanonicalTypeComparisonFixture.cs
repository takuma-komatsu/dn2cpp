using Mono.Cecil;
using Mono.Cecil.Cil;

internal static class CanonicalTypeComparisonFixture
{
    public static bool Run(string[] args)
    {
        if (args.Length != 3 || args[0] != "--canonical-type-comparison") return false;
        using var core = AssemblyDefinition.ReadAssembly(args[2]);
        if (core.Name.Name != "System.Private.CoreLib") throw new InvalidOperationException("Expected input CoreLib");
        var owner = core.MainModule.GetType("System.RuntimeTypeHandle");
        var method = owner.Methods.Single(m => m.Name == "CompareCanonicalHandles");
        if (!method.IsAssembly || !method.IsStatic || method.HasThis || method.ExplicitThis || method.HasGenericParameters
            || method.CallingConvention != MethodCallingConvention.Default || method.HasBody
            || !method.IsIL || !method.IsManaged || !method.IsInternalCall || method.IsPInvokeImpl
            || method.ReturnType.FullName != "System.Boolean" || method.Parameters.Count != 2
            || method.Parameters.Any(p => p.ParameterType.FullName != "System.RuntimeType"
                || p.ParameterType.Resolve().Module != core.MainModule))
            throw new InvalidOperationException("Canonical type comparison declaration changed");
        string path = Path.GetFullPath(args[1]);
        using var app = AssemblyDefinition.ReadAssembly(path, new ReaderParameters { InMemory = true });
        var module = app.MainModule;
        var program = module.GetType("CanonicalTypeComparisonSubset.Program");
        var target = module.ImportReference(method);
        foreach (string route in new[] { "Direct", "Pointer" })
        {
            var stub = program.Methods.Single(m => m.Name == "Compare" + route);
            stub.Body = new MethodBody(stub);
            var il = stub.Body.GetILProcessor();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Castclass, target.Parameters[0].ParameterType);
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Castclass, target.Parameters[1].ParameterType);
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
        module.EntryPoint = program.Methods.Single(m => m.Name == "RunArtifactChecks");
        app.Write(path, new WriterParameters { Timestamp = 0, DeterministicMvid = true });
        Console.WriteLine("canonical-type-sites=direct,address,exact-CoreLib-signature");
        return true;
    }
}

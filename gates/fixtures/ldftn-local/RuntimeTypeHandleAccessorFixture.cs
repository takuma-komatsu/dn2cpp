using Mono.Cecil;
using Mono.Cecil.Cil;

internal static class RuntimeTypeHandleAccessorFixture
{
    public static bool Run(string[] args)
    {
        if (args.Length != 3 || args[0] != "--runtime-type-handle-accessor") return false;
        using var core = AssemblyDefinition.ReadAssembly(args[2]);
        if (core.Name.Name != "System.Private.CoreLib") throw new InvalidOperationException("Expected input CoreLib");
        var owner = core.MainModule.GetType("System.RuntimeTypeHandle");
        string path = Path.GetFullPath(args[1]);
        using var app = AssemblyDefinition.ReadAssembly(path, new ReaderParameters { InMemory = true });
        var module = app.MainModule;
        var program = module.GetType("RuntimeTypeHandleAccessorSubset.Program");
        foreach (string kind in new[] { "Checked", "Unchecked" })
        {
            string name = kind == "Checked" ? "GetRuntimeTypeChecked" : "GetRuntimeType";
            var method = owner.Methods.Single(m => m.Name == name && !m.IsStatic && m.Parameters.Count == 0);
            if (!method.IsAssembly || method.IsStatic || !method.HasThis || method.ExplicitThis || method.HasGenericParameters
                || method.CallingConvention != MethodCallingConvention.Default || !method.HasBody
                || !method.IsIL || !method.IsManaged || method.IsInternalCall || method.IsPInvokeImpl
                || method.ReturnType.FullName != "System.RuntimeType" || method.ReturnType.Resolve().Module != core.MainModule)
                throw new InvalidOperationException("Runtime type handle accessor declaration changed: " + name);
            foreach (string route in new[] { "Direct", "Pointer" })
            {
                var stub = program.Methods.Single(m => m.Name == kind + route);
                stub.Body = new MethodBody(stub);
                var il = stub.Body.GetILProcessor();
                var target = module.ImportReference(method);
                il.Emit(OpCodes.Ldarga_S, stub.Parameters[0]);
                if (route == "Pointer")
                {
                    il.Emit(OpCodes.Ldftn, target);
                    var site = new CallSite(target.ReturnType);
                    site.Parameters.Add(new ParameterDefinition(new ByReferenceType(target.DeclaringType)));
                    il.Emit(OpCodes.Calli, site);
                }
                else il.Emit(OpCodes.Call, target);
                il.Emit(OpCodes.Ret);
            }
        }
        module.EntryPoint = program.Methods.Single(m => m.Name == "RunArtifactChecks");
        app.Write(path, new WriterParameters { Timestamp = 0, DeterministicMvid = true });
        Console.WriteLine("handle-accessor-sites=2-direct,2-address,exact-CoreLib-signatures");
        return true;
    }
}

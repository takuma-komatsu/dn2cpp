using Mono.Cecil;
using Mono.Cecil.Cil;

internal static class ConstraintCheckFixture
{
    public static bool Run(string[] args)
    {
        if (args.Length != 3 || args[0] is not ("--constraint-check" or "--constraint-check-reflection")) return false;
        using var core = AssemblyDefinition.ReadAssembly(args[2]);
        if (core.Name.Name != "System.Private.CoreLib") throw new InvalidOperationException("Expected input CoreLib");
        var owner = core.MainModule.GetType("System.RuntimeTypeHandle");
        var method = owner.Methods.Single(m => m.Name == "SatisfiesConstraints" && m.ReturnType.FullName == "System.Boolean");
        if (!method.IsAssembly || !method.IsStatic || method.HasThis || method.ExplicitThis || method.HasGenericParameters
            || method.CallingConvention != MethodCallingConvention.Default || !method.HasBody
            || !method.IsIL || !method.IsManaged || method.IsInternalCall || method.IsPInvokeImpl
            || method.Parameters.Count != 4)
            throw new InvalidOperationException("Managed constraint-check declaration changed");
        for (int i = 0; i < method.Parameters.Count; i++)
            if (method.Parameters[i].ParameterType.FullName != (i == 2 ? "System.Reflection.RuntimeMethodInfo" : "System.RuntimeType")
                || method.Parameters[i].ParameterType.Resolve().Module != core.MainModule)
                throw new InvalidOperationException("Managed constraint-check parameter changed");
        string path = Path.GetFullPath(args[1]);
        using var app = AssemblyDefinition.ReadAssembly(path, new ReaderParameters { InMemory = true });
        var module = app.MainModule;
        var program = module.GetType("ConstraintCheckSubset.Program");
        bool reflectionOnly = args[0] == "--constraint-check-reflection";
        if (!reflectionOnly)
        {
            var target = module.ImportReference(method);
            foreach (string route in new[] { "Direct", "Pointer" })
            {
                var stub = program.Methods.Single(m => m.Name == "Check" + route);
                stub.Body = new MethodBody(stub);
                var il = stub.Body.GetILProcessor();
                for (int i = 0; i < target.Parameters.Count; i++)
                {
                    il.Emit(OpCodes.Ldarg, stub.Parameters[i]);
                    il.Emit(OpCodes.Castclass, target.Parameters[i].ParameterType);
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
        module.EntryPoint = program.Methods.Single(m => m.Name == (reflectionOnly ? "RunReflectionChecks" : "RunArtifactChecks"));
        app.Write(path, new WriterParameters { Timestamp = 0, DeterministicMvid = true });
        Console.WriteLine("constraint-check-sites=" + (reflectionOnly ? "reflection-only" : "direct,address,reflection") + ",exact-managed-CoreLib-signature,native-only");
        return true;
    }
}

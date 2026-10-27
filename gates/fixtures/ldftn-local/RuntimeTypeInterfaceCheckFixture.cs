using Mono.Cecil;
using Mono.Cecil.Cil;

internal static class RuntimeTypeInterfaceCheckFixture
{
    public static bool Run(string[] args)
    {
        if (args.Length != 3 || args[0] is not ("--runtime-type-interface-check" or "--runtime-type-interface-check-reflection")) return false;
        using var core = AssemblyDefinition.ReadAssembly(args[2]);
        if (core.Name.Name != "System.Private.CoreLib") throw new InvalidOperationException("Expected input CoreLib");
        var owner = core.MainModule.GetType("System.RuntimeTypeHandle");
        var method = owner.Methods.Single(m => m.Name == "VerifyInterfaceIsImplemented" && !m.IsStatic);
        if (!method.IsAssembly || !method.HasThis || method.ExplicitThis || method.HasGenericParameters || method.IsVirtual
            || method.CallingConvention != MethodCallingConvention.Default || !method.HasBody
            || !method.IsIL || !method.IsManaged || method.IsInternalCall || method.IsPInvokeImpl
            || method.ReturnType.MetadataType != MetadataType.Void || method.Parameters.Count != 1
            || method.Parameters[0].ParameterType.Resolve() != owner)
            throw new InvalidOperationException("Runtime type interface check declaration changed");
        string path = Path.GetFullPath(args[1]);
        using var app = AssemblyDefinition.ReadAssembly(path, new ReaderParameters { InMemory = true });
        var module = app.MainModule;
        var program = module.GetType("RuntimeTypeInterfaceCheckSubset.Program");
        bool reflectionOnly = args[0] == "--runtime-type-interface-check-reflection";
        if (!reflectionOnly)
        {
            foreach (string route in new[] { "Direct", "Pointer" })
            {
                var stub = program.Methods.Single(m => m.Name == route);
                stub.Body = new MethodBody(stub);
                var il = stub.Body.GetILProcessor();
                var target = module.ImportReference(method);
                il.Emit(OpCodes.Ldarga_S, stub.Parameters[0]);
                il.Emit(OpCodes.Ldarg_1);
                if (route == "Pointer")
                {
                    il.Emit(OpCodes.Ldftn, target);
                    var site = new CallSite(target.ReturnType);
                    site.Parameters.Add(new ParameterDefinition(new ByReferenceType(target.DeclaringType)));
                    site.Parameters.Add(new ParameterDefinition(target.Parameters[0].ParameterType));
                    il.Emit(OpCodes.Calli, site);
                }
                else il.Emit(OpCodes.Call, target);
                il.Emit(OpCodes.Ret);
            }
        }
        module.EntryPoint = program.Methods.Single(m => m.Name == (reflectionOnly ? "RunReflectionChecks" : "RunArtifactChecks"));
        app.Write(path, new WriterParameters { Timestamp = 0, DeterministicMvid = true });
        Console.WriteLine("interface-check-sites=" + (reflectionOnly ? "reflection-only" : "direct,address,reflection") + ",exact-managed-CoreLib-signature,native-only");
        return true;
    }
}

using Mono.Cecil;
using Mono.Cecil.Cil;

internal static class RuntimeTypeCacheHandleFixture
{
    public static bool Run(string[] args)
    {
        if (args.Length != 3 || args[0] is not ("--runtime-type-cache-handle" or "--runtime-type-cache-handle-reflection")) return false;
        using var core = AssemblyDefinition.ReadAssembly(args[2]);
        if (core.Name.Name != "System.Private.CoreLib") throw new InvalidOperationException("Expected input CoreLib");
        var owner = core.MainModule.GetType("System.RuntimeTypeHandle");
        string path = Path.GetFullPath(args[1]);
        using var app = AssemblyDefinition.ReadAssembly(path, new ReaderParameters { InMemory = true });
        var module = app.MainModule;
        var program = module.GetType("RuntimeTypeCacheHandleSubset.Program");
        bool reflectionOnly = args[0] == "--runtime-type-cache-handle-reflection";
        foreach (string member in new[] { "Get", "Free" })
        {
            var method = owner.Methods.Single(m => m.Name == member + "GCHandle" && !m.IsStatic);
            string parameter = member == "Get" ? "System.Runtime.InteropServices.GCHandleType" : "System.IntPtr";
            if (!method.IsAssembly || !method.HasThis || method.ExplicitThis || method.HasGenericParameters
                || method.CallingConvention != MethodCallingConvention.Default || !method.HasBody
                || !method.IsIL || !method.IsManaged || method.IsInternalCall || method.IsPInvokeImpl
                || method.ReturnType.MetadataType != MetadataType.IntPtr || method.Parameters.Count != 1
                || method.Parameters[0].ParameterType.FullName != parameter
                || method.Parameters[0].ParameterType.Resolve().Module != core.MainModule)
                throw new InvalidOperationException("Runtime type cache handle declaration changed: " + member);
            if (reflectionOnly) continue;
            foreach (string route in new[] { "Direct", "Pointer" })
            {
                var stub = program.Methods.Single(m => m.Name == member + route);
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
        Console.WriteLine("type-cache-handle-sites=" + (reflectionOnly ? "reflection-only" : "direct,address,reflection") + ",exact-managed-CoreLib-signatures,native-only");
        return true;
    }
}

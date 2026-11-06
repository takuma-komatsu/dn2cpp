using Mono.Cecil;
using Mono.Cecil.Cil;

internal static class MethodGenericCountFixture
{
    public static bool Run(string[] args)
    {
        if (args.Length != 3 || args[0] is not ("--method-generic-count" or "--method-generic-count-reflection")) return false;
        using var core = AssemblyDefinition.ReadAssembly(args[2]);
        if (core.Name.Name != "System.Private.CoreLib") throw new InvalidOperationException("Expected input CoreLib");
        var method = core.MainModule.GetType("System.Reflection.MethodInfo").Methods.Single(m => m.Name == "get_GenericParameterCount");
        if (!method.IsAssembly || !method.IsVirtual || method.IsFinal || !method.IsNewSlot || !method.IsCheckAccessOnOverride
            || !method.IsSpecialName || method.IsStatic || !method.HasThis || method.ExplicitThis || method.HasGenericParameters
            || method.CallingConvention != MethodCallingConvention.Default || !method.HasBody
            || !method.IsIL || !method.IsManaged || method.IsInternalCall || method.IsPInvokeImpl
            || method.ReturnType.MetadataType != MetadataType.Int32 || method.Parameters.Count != 0)
            throw new InvalidOperationException("MethodInfo generic count declaration changed");
        string path = Path.GetFullPath(args[1]);
        using var app = AssemblyDefinition.ReadAssembly(path, new ReaderParameters { InMemory = true });
        var module = app.MainModule;
        var program = module.GetType("MethodGenericCountSubset.Program");
        bool reflectionOnly = args[0] == "--method-generic-count-reflection";
        if (!reflectionOnly)
        {
            var target = module.ImportReference(method);
            foreach (string route in new[] { "Direct", "Virtual", "Pointer", "VirtualPointer" })
            {
                var stub = program.Methods.Single(m => m.Name == route);
                stub.Body = new MethodBody(stub);
                var il = stub.Body.GetILProcessor();
                il.Emit(OpCodes.Ldarg_0);
                if (route is "Pointer" or "VirtualPointer")
                {
                    if (route == "VirtualPointer") il.Emit(OpCodes.Ldarg_0);
                    il.Emit(route == "Pointer" ? OpCodes.Ldftn : OpCodes.Ldvirtftn, target);
                    // The native callable ABI takes the MethodInfo receiver explicitly.
                    var site = new CallSite(target.ReturnType);
                    site.Parameters.Add(new ParameterDefinition(target.DeclaringType));
                    il.Emit(OpCodes.Calli, site);
                }
                else il.Emit(route == "Direct" ? OpCodes.Call : OpCodes.Callvirt, target);
                il.Emit(OpCodes.Ret);
            }
        }
        module.EntryPoint = program.Methods.Single(m => m.Name == (reflectionOnly ? "RunReflectionChecks" : "RunArtifactChecks"));
        app.Write(path, new WriterParameters { Timestamp = 0, DeterministicMvid = true });
        Console.WriteLine("method-generic-count-sites=" + (reflectionOnly ? "reflection-only" : "call,callvirt,ldftn,ldvirtftn,reflection")
            + ",exact-CoreLib-signature,native-only");
        return true;
    }
}

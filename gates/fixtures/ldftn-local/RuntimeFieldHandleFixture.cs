using Mono.Cecil;
using Mono.Cecil.Cil;

// Rewrites the native-only GetAttributesDirect stub into a direct call of the
// CoreCLR FieldDesc InternalCall RuntimeFieldHandle.GetAttributes.
internal static class RuntimeFieldHandleFixture
{
    public static bool Run(string[] args)
    {
        if (args.Length != 3 || args[0] != "--runtime-field-handle") return false;
        using var core = AssemblyDefinition.ReadAssembly(args[2]);
        if (core.Name.Name != "System.Private.CoreLib") throw new InvalidOperationException("Expected input CoreLib");
        var method = core.MainModule.GetType("System.RuntimeFieldHandle").Methods.Single(m => m.Name == "GetAttributes" && !m.HasBody);
        if (!method.IsStatic || !method.IsInternalCall || method.Parameters.Count != 1
            || method.Parameters[0].ParameterType.FullName != "System.RuntimeFieldHandleInternal")
            throw new InvalidOperationException("CoreCLR FieldDesc declaration changed: GetAttributes");
        string path = Path.GetFullPath(args[1]);
        using var app = AssemblyDefinition.ReadAssembly(path, new ReaderParameters { InMemory = true });
        var module = app.MainModule;
        var stub = module.GetType("RuntimeFieldHandleSubset.Program").Methods.Single(m => m.Name == "GetAttributesDirect");
        stub.Body = new MethodBody(stub) { InitLocals = true };
        var il = stub.Body.GetILProcessor();
        var target = module.ImportReference(method);
        var local = new VariableDefinition(target.Parameters[0].ParameterType);
        stub.Body.Variables.Add(local);
        il.Emit(OpCodes.Ldloca, local);
        il.Emit(OpCodes.Initobj, local.VariableType);
        il.Emit(OpCodes.Ldloc, local);
        il.Emit(OpCodes.Call, target);
        il.Emit(OpCodes.Pop);
        il.Emit(OpCodes.Ret);
        app.Write(path, new WriterParameters { Timestamp = 0, DeterministicMvid = true });
        Console.WriteLine("field-handle-sites=GetAttributes-direct");
        return true;
    }
}

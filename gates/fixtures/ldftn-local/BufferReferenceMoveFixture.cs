using Mono.Cecil;
using Mono.Cecil.Cil;

internal static class BufferReferenceMoveFixture
{
    public static bool Run(string[] args)
    {
        if (args.Length != 3 || args[0] is not ("--buffer-reference-moves"
            or "--buffer-reference-moves-negative-internal" or "--buffer-reference-moves-negative-batch"))
            return false;
        using var core = AssemblyDefinition.ReadAssembly(args[2]);
        string path = Path.GetFullPath(args[1]);
        using var app = AssemblyDefinition.ReadAssembly(path, new ReaderParameters { InMemory = true });
        var module = app.MainModule;
        var owner = module.GetType("BufferReferenceMoveSubset.Program");
        foreach (string suffix in new[] { "Internal", "Batch" })
        {
            var target = core.MainModule.GetType("System.Buffer").Methods.Single(method =>
                method.Name == "BulkMoveWithWriteBarrier" + suffix && method.IsStatic
                && !method.HasGenericParameters && method.ReturnType.FullName == "System.Void"
                && method.Parameters.Select(parameter => parameter.ParameterType.FullName)
                    .SequenceEqual(new[] { "System.Byte&", "System.Byte&", "System.UIntPtr" }));
            var stub = owner.Methods.Single(method => method.Name == "Move" + suffix);
            stub.Body = new MethodBody(stub);
            var il = stub.Body.GetILProcessor();
            foreach (var parameter in stub.Parameters) il.Emit(OpCodes.Ldarg, parameter);
            il.Emit(OpCodes.Call, module.ImportReference(target));
            il.Emit(OpCodes.Ret);
            Console.WriteLine("buffer-move-fixture:" + suffix + ":ref-byte,ref-byte,nuint");
        }
        if (args[0] != "--buffer-reference-moves")
        {
            string suffix = args[0].EndsWith("-batch", StringComparison.Ordinal) ? "Batch" : "Internal";
            var malformed = new MethodReference("BulkMoveWithWriteBarrier" + suffix, module.TypeSystem.Void,
                module.ImportReference(core.MainModule.GetType("System.Buffer")));
            malformed.Parameters.Add(new ParameterDefinition(new ByReferenceType(module.TypeSystem.Byte)));
            malformed.Parameters.Add(new ParameterDefinition(new ByReferenceType(module.TypeSystem.Byte)));
            malformed.Parameters.Add(new ParameterDefinition(module.TypeSystem.Int32));
            var entry = module.EntryPoint;
            entry.Body = new MethodBody(entry) { InitLocals = true };
            var bytes = new VariableDefinition(new ArrayType(module.TypeSystem.Byte));
            entry.Body.Variables.Add(bytes);
            var il = entry.Body.GetILProcessor();
            il.Emit(OpCodes.Ldc_I4, 16);
            il.Emit(OpCodes.Newarr, module.TypeSystem.Byte);
            il.Emit(OpCodes.Stloc, bytes);
            foreach (int offset in new[] { 8, 0 })
            {
                il.Emit(OpCodes.Ldloc, bytes);
                il.Emit(OpCodes.Ldc_I4, offset);
                il.Emit(OpCodes.Ldelema, module.TypeSystem.Byte);
            }
            il.Emit(OpCodes.Ldc_I4_4);
            il.Emit(OpCodes.Call, malformed);
            il.Emit(OpCodes.Ret);
            Console.WriteLine("buffer-move-negative:" + suffix + ":int32-count");
        }
        app.Write(path, new WriterParameters { Timestamp = 0, DeterministicMvid = true });
        return true;
    }
}

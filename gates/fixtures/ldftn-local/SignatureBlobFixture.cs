using Mono.Cecil;
using Mono.Cecil.Cil;

internal static class SignatureBlobFixture
{
    public static bool Run(string[] args)
    {
        bool verify = args.Length == 2 && args[0] == "--signature-blob-verify";
        if (!verify && (args.Length != 3 || args[0] != "--signature-blob")) return false;
        using var core = AssemblyDefinition.ReadAssembly(args[verify ? 1 : 2]);
        if (core.Name.Name != "System.Private.CoreLib") throw new InvalidOperationException("Expected input CoreLib");
        var owner = core.MainModule.GetType("System.Signature");
        var shapes = new (string Name, string Label, int Count)[]
        {
            ("GetParameterOffsetInternal", "Parameter", 3),
            ("GetTypeParameterOffsetInternal", "TypeParameter", 4),
            ("GetCallingConventionFromFunctionPointerAtOffsetInternal", "CallingConvention", 3),
        };
        foreach (var shape in shapes)
        {
            var method = owner.Methods.Single(m => m.Name == shape.Name);
            if (!method.IsPrivate || !method.IsStatic || method.HasThis || method.ExplicitThis || method.HasGenericParameters
                || method.CallingConvention != MethodCallingConvention.Default || method.HasBody
                || !method.IsIL || !method.IsManaged || !method.IsInternalCall || method.IsPInvokeImpl
                || method.ReturnType.FullName != "System.Int32" || method.Parameters.Count != shape.Count
                || method.Parameters[0].ParameterType.FullName != "System.Void*"
                || method.Parameters.Skip(1).Any(p => p.ParameterType.FullName != "System.Int32"))
                throw new InvalidOperationException("Signature parser declaration changed: " + shape.Name);
        }
        foreach (string name in new[] { "GetTypeParameterOffset", "GetCallingConventionFromFunctionPointerAtOffset" })
        {
            var method = owner.Methods.Single(m => m.Name == name);
            var code = method.Body.Instructions;
            // The CLR oracle may use an uninitialized receiver only if no fields/native calls precede the negative return.
            if (method.IsStatic || !method.IsAssembly || !method.IsIL || !method.IsManaged || method.IsInternalCall
                || code[0].OpCode != OpCodes.Ldarg_1 || code[1].OpCode != OpCodes.Ldc_I4_0
                || code[2].OpCode != OpCodes.Bge_S || code[2].Operand != code[5]
                || code[3].OpCode != (name == "GetTypeParameterOffset" ? OpCodes.Ldarg_1 : OpCodes.Ldc_I4_0)
                || code[4].OpCode != OpCodes.Ret)
                throw new InvalidOperationException("Signature negative-offset fast path changed: " + name);
        }
        if (verify)
        {
            Console.WriteLine("signature-blob-oracle=verified-negative-offset-fast-paths");
            return true;
        }
        string path = Path.GetFullPath(args[1]);
        using var app = AssemblyDefinition.ReadAssembly(path, new ReaderParameters { InMemory = true });
        var module = app.MainModule;
        var program = module.GetType("SignatureBlobSubset.Program");
        foreach (var shape in shapes)
        {
            var target = module.ImportReference(owner.Methods.Single(m => m.Name == shape.Name));
            foreach (string route in new[] { "Direct", "Pointer" })
            {
                var stub = program.Methods.Single(m => m.Name == shape.Label + route);
                stub.Body = new MethodBody(stub);
                var il = stub.Body.GetILProcessor();
                il.Emit(OpCodes.Ldc_I4_0);
                il.Emit(OpCodes.Conv_U);
                for (int i = 1; i < shape.Count; i++) il.Emit(OpCodes.Ldc_I4_0);
                if (route == "Pointer")
                {
                    il.Emit(OpCodes.Ldftn, target);
                    var site = new CallSite(target.ReturnType);
                    foreach (var parameter in target.Parameters)
                        site.Parameters.Add(new ParameterDefinition(parameter.ParameterType));
                    il.Emit(OpCodes.Calli, site);
                }
                else il.Emit(OpCodes.Call, target);
                il.Emit(OpCodes.Pop);
                il.Emit(OpCodes.Ret);
            }
        }
        module.EntryPoint = program.Methods.Single(m => m.Name == "RunArtifactChecks");
        app.Write(path, new WriterParameters { Timestamp = 0, DeterministicMvid = true });
        Console.WriteLine("signature-blob-sites=3-direct,3-address,exact-CoreLib-signatures");
        return true;
    }
}

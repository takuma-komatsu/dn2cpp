using Mono.Cecil;
using Mono.Cecil.Cil;

internal static class RuntimeTypeHandleCopyFixture
{
    public static bool Run(string[] args)
    {
        if (args.Length == 3 && args[0] == "--runtime-type-handle-copy-measure")
        {
            using var measureCore = AssemblyDefinition.ReadAssembly(args[2]);
            if (measureCore.Name.Name != "System.Private.CoreLib") throw new InvalidOperationException("Expected input CoreLib");
            var measureOwner = measureCore.MainModule.GetType("System.RuntimeTypeHandle");
            var probe = new MethodDefinition("CopyMeasureProbe", MethodAttributes.Assembly | MethodAttributes.Static,
                measureCore.MainModule.TypeSystem.Void);
            probe.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
            measureOwner.Methods.Add(probe);
            foreach (var method in measureOwner.Methods.Where(m => m.Name == "CopyRuntimeTypeHandles"))
                method.Body.Instructions.Insert(0, Instruction.Create(OpCodes.Call, probe));
            // The diagnostic-only artifact contains IL, not CoreLib's ReadyToRun image.
            measureCore.MainModule.Attributes |= ModuleAttributes.ILOnly;
            measureCore.Write(args[1], new WriterParameters { Timestamp = 0, DeterministicMvid = true });
            Console.WriteLine("handle-copy-measure=compile-only-gap");
            return true;
        }
        if (args.Length != 3 || args[0] != "--runtime-type-handle-copy") return false;
        using var core = AssemblyDefinition.ReadAssembly(args[2]);
        if (core.Name.Name != "System.Private.CoreLib") throw new InvalidOperationException("Expected input CoreLib");
        var owner = core.MainModule.GetType("System.RuntimeTypeHandle");
        string path = Path.GetFullPath(args[1]);
        using var app = AssemblyDefinition.ReadAssembly(path, new ReaderParameters { InMemory = true });
        var module = app.MainModule;
        var program = module.GetType("RuntimeTypeHandleCopySubset.Program");
        foreach (string kind in new[] { "Handles", "Types" })
        {
            bool handles = kind == "Handles";
            string input = handles ? "System.RuntimeTypeHandle[]" : "System.Type[]";
            var method = owner.Methods.Single(m => m.Name == "CopyRuntimeTypeHandles"
                && m.Parameters.Count == 2 && m.Parameters[0].ParameterType.FullName == input);
            if (!method.IsAssembly || !method.IsStatic || method.HasThis || method.ExplicitThis || method.HasGenericParameters
                || method.CallingConvention != MethodCallingConvention.Default || !method.HasBody
                || !method.IsIL || !method.IsManaged || method.IsInternalCall || method.IsPInvokeImpl
                || method.ReturnType.FullName != (handles ? "System.ReadOnlySpan`1<System.IntPtr>" : "System.IntPtr[]")
                || method.Parameters[1].ParameterType.FullName != (handles ? "System.Span`1<System.IntPtr>" : "System.Int32&")
                || method.Body.Instructions.Any(i => i.Operand is FieldReference)
                || !method.Body.Instructions.Any(i => i.Operand is MethodReference target
                    && target.DeclaringType.FullName == "System.RuntimeTypeHandle" && target.Name == "get_Value"))
                throw new InvalidOperationException("Runtime type handle copy declaration changed: " + kind);
            foreach (string route in new[] { "Direct", "Pointer" })
            {
                var stub = program.Methods.Single(m => m.Name == kind + route);
                stub.Body = new MethodBody(stub);
                var il = stub.Body.GetILProcessor();
                var target = module.ImportReference(method);
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldarg_1);
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
        Console.WriteLine("handle-copy-sites=2-direct,2-address,exact-CoreLib-signatures");
        return true;
    }
}

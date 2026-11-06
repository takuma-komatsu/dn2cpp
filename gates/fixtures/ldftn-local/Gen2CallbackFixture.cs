using System.Globalization;
using Mono.Cecil;
using Mono.Cecil.Cil;

internal static class Gen2CallbackFixture
{
    public static bool Run(string[] args)
    {
        if (args.Length != 3 || !new[]
            { "--gen2-callback", "--gen2-callback-direct", "--gen2-callback-delegate", "--gen2-callback-reflection", "--gen2-callback-noalloc", "--gen2-callback-pointer-noalloc", "--gen2-callback-newobj" }.Contains(args[0]))
            return false;
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        using var core = AssemblyDefinition.ReadAssembly(args[2]);
        var targetType = core.MainModule.GetType("System.Gen2GcCallback");
        if (targetType is null || !targetType.IsSealed || targetType.IsValueType || targetType.IsNested
            || targetType.HasGenericParameters || targetType.BaseType.FullName != "System.Runtime.ConstrainedExecution.CriticalFinalizerObject")
            throw new InvalidOperationException("Gen2GcCallback declaration changed");
        var ctor0 = targetType.Methods.Single(m => m.IsConstructor && !m.IsStatic && m.Parameters.Count == 1);
        var ctor1 = targetType.Methods.Single(m => m.IsConstructor && !m.IsStatic && m.Parameters.Count == 2);
        var finalizer = targetType.Methods.Single(m => m.Name == "Finalize");
        if ((int)ctor0.Attributes != 0x1881 || (int)ctor1.Attributes != 0x1881 || (int)finalizer.Attributes != 0xc4
            || new[] { ctor0, ctor1, finalizer }.Any(m => m.ImplAttributes != 0 || !m.HasBody || m.HasGenericParameters)
            || ctor0.Parameters[0].ParameterType.FullName != "System.Func`1<System.Boolean>"
            || ctor1.Parameters[0].ParameterType.FullName != "System.Func`2<System.Object,System.Boolean>"
            || ctor1.Parameters[1].ParameterType.FullName != "System.Object"
            || finalizer.Parameters.Count != 0 || finalizer.ReturnType.FullName != "System.Void")
            throw new InvalidOperationException("Gen2GcCallback callable declarations changed");
        string path = Path.GetFullPath(args[1]);
        using var app = AssemblyDefinition.ReadAssembly(path, new ReaderParameters { InMemory = true });
        var module = app.MainModule;
        var program = module.GetType("Gen2CallbackSubset.Program");
        MethodDefinition Method(string name) => program.Methods.Single(m => m.Name == name);
        var nativeType = module.ImportReference(targetType);
        ILProcessor Body(string name)
        {
            var method = Method(name);
            method.Body = new MethodBody(method);
            return method.Body.GetILProcessor();
        }
        var getType = module.ImportReference(core.MainModule.GetType("System.Type").Methods.Single(m => m.Name == "GetTypeFromHandle"));
        var typeIl = Body("TargetType");
        typeIl.Emit(OpCodes.Ldtoken, nativeType);
        typeIl.Emit(OpCodes.Call, getType);
        typeIl.Emit(OpCodes.Ret);
        foreach (var row in new[]
        {
            (Suffix: "0", Definition: ctor0, Delegate: "Callback0", Metadata: "Constructor0"),
            (Suffix: "1", Definition: ctor1, Delegate: "Callback1", Metadata: "Constructor1"),
            (Suffix: "Final", Definition: finalizer, Delegate: "Finish", Metadata: "FinalizerMethod")
        })
        {
            var target = module.ImportReference(row.Definition);
            foreach (string prefix in new[] { "Direct", "Pointer" })
            {
                var il = Body(prefix + row.Suffix);
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Castclass, nativeType);
                for (int i = 0; i < target.Parameters.Count; i++)
                    il.Emit(OpCodes.Ldarg, Method(prefix + row.Suffix).Parameters[i + 1]);
                if (prefix == "Pointer")
                {
                    il.Emit(OpCodes.Ldftn, target);
                    var site = new CallSite(module.TypeSystem.Void);
                    site.Parameters.Add(new ParameterDefinition(nativeType));
                    foreach (var parameter in target.Parameters)
                        site.Parameters.Add(new ParameterDefinition(parameter.ParameterType));
                    il.Emit(OpCodes.Calli, site);
                }
                else il.Emit(OpCodes.Call, target);
                il.Emit(OpCodes.Ret);
            }
            var delegateType = program.NestedTypes.Single(t => t.Name == row.Delegate);
            var addressIl = Body("Address" + row.Suffix);
            addressIl.Emit(OpCodes.Ldarg_0);
            addressIl.Emit(OpCodes.Castclass, nativeType);
            addressIl.Emit(OpCodes.Ldftn, target);
            addressIl.Emit(OpCodes.Newobj, delegateType.Methods.Single(m => m.IsConstructor));
            addressIl.Emit(OpCodes.Ret);

        }
        foreach (int argc in new[] { 1, 2 })
        {
            var register = targetType.Methods.Single(m => m.Name == "Register" && m.Parameters.Count == argc);
            if ((int)register.Attributes != 0x96 || register.ImplAttributes != 0)
                throw new InvalidOperationException("Gen2GcCallback Register declaration changed");
            string name = argc == 1 ? "Register0" : "Register1";
            var il = Body(name);
            foreach (var parameter in Method(name).Parameters) il.Emit(OpCodes.Ldarg, parameter);
            il.Emit(OpCodes.Call, module.ImportReference(register));
            il.Emit(OpCodes.Ret);
        }
        var negative = Body("StaticNewobj");
        negative.Emit(OpCodes.Ldnull);
        negative.Emit(OpCodes.Newobj, module.ImportReference(ctor0));
        negative.Emit(OpCodes.Pop);
        negative.Emit(OpCodes.Ret);
        if (args[0] is "--gen2-callback-noalloc" or "--gen2-callback-pointer-noalloc")
        {
            foreach (string name in args[0] == "--gen2-callback-noalloc"
                ? new[] { "Direct0", "Direct1", "DirectFinal" }
                : new[] { "Pointer0", "Pointer1", "PointerFinal" })
                RuntimeTypeDefaultCtorFixture.MarkNoAlloc(module, Method(name));
        }
        module.EntryPoint = Method(args[0] switch
        {
            "--gen2-callback-direct" or "--gen2-callback-noalloc" or "--gen2-callback-pointer-noalloc" => "RunDirectChecks",
            "--gen2-callback-delegate" => "RunDelegateChecks",
            "--gen2-callback-reflection" => "RunReflectionChecks",
            "--gen2-callback-newobj" => "RunNewobjNegative",
            _ => "RunArtifactChecks"
        });
        app.Write(path, new WriterParameters { Timestamp = 0, DeterministicMvid = true });
        return true;
    }
}

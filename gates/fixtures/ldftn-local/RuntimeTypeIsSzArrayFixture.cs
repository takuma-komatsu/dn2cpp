using Mono.Cecil;
using Mono.Cecil.Cil;

internal static class RuntimeTypeIsSzArrayFixture
{
    public static bool Run(string[] args)
    {
        if (args.Length != 3 || args[0] != "--runtime-type-szarray") return false;
        using var core = AssemblyDefinition.ReadAssembly(args[2]);
        if (core.Name.Name != "System.Private.CoreLib") throw new InvalidOperationException("Expected input CoreLib");
        var runtimeType = core.MainModule.GetType("System.RuntimeType");
        var helper = core.MainModule.GetType("System.RuntimeTypeHandle").Methods.Single(m => m.Name == "IsSZArray");
        var getter = runtimeType.Methods.Single(m => m.Name == "get_IsSZArray");
        if (helper.Attributes != (MethodAttributes.Assembly | MethodAttributes.Static | MethodAttributes.HideBySig)
            || helper.HasThis || helper.ExplicitThis || helper.HasGenericParameters
            || helper.CallingConvention != MethodCallingConvention.Default || !helper.HasBody
            || !helper.IsIL || !helper.IsManaged || helper.IsInternalCall || helper.IsPInvokeImpl
            || helper.ReturnType.MetadataType != MetadataType.Boolean || helper.Parameters.Count != 1
            || helper.Parameters[0].ParameterType.FullName != "System.RuntimeType"
            || helper.Parameters[0].ParameterType.Resolve() != runtimeType
            || !getter.HasBody || !getter.HasThis || getter.Parameters.Count != 0
            || getter.ReturnType.MetadataType != MetadataType.Boolean
            || !getter.Body.Instructions.Any(i => i.OpCode == OpCodes.Call && i.Operand is MethodReference m && m.Resolve() == helper))
            throw new InvalidOperationException("Runtime type SZ-array declarations changed");
        string path = Path.GetFullPath(args[1]);
        using var app = AssemblyDefinition.ReadAssembly(path, new ReaderParameters { InMemory = true });
        var module = app.MainModule;
        var program = module.GetType("RuntimeTypeIsSzArraySubset.Program");
        foreach (string route in new[] { "Direct", "Pointer", "Getter", "Address", "Generic" })
        {
            var stub = program.Methods.Single(m => m.Name == route);
            if (route == "Generic")
            {
                var call = stub.Body.Instructions.Single(i => i.OpCode == OpCodes.Callvirt
                    && i.Operand is MethodReference m && m.Name == "get_IsSZArray");
                call.OpCode = OpCodes.Call;
                call.Operand = module.ImportReference(helper);
                continue;
            }
            stub.Body = new MethodBody(stub);
            var il = stub.Body.GetILProcessor();
            var target = module.ImportReference(route == "Getter" ? getter : helper);
            if (route == "Address")
            {
                var constructor = new MethodReference(".ctor", module.TypeSystem.Void, stub.ReturnType) { HasThis = true };
                constructor.Parameters.Add(new ParameterDefinition(module.TypeSystem.Object));
                constructor.Parameters.Add(new ParameterDefinition(module.TypeSystem.IntPtr));
                il.Emit(OpCodes.Ldnull);
                il.Emit(OpCodes.Ldftn, target);
                il.Emit(OpCodes.Newobj, constructor);
            }
            else
            {
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Castclass, module.ImportReference(runtimeType));
                if (route == "Pointer")
                {
                    il.Emit(OpCodes.Ldftn, target);
                    var site = new CallSite(target.ReturnType);
                    site.Parameters.Add(new ParameterDefinition(module.ImportReference(runtimeType)));
                    il.Emit(OpCodes.Calli, site);
                }
                else il.Emit(OpCodes.Call, target);
            }
            il.Emit(OpCodes.Ret);
        }
        module.EntryPoint = program.Methods.Single(m => m.Name == "RunArtifact");
        app.Write(path, new WriterParameters { Timestamp = 0, DeterministicMvid = true });
        Console.WriteLine("runtime-type-szarray-sites=direct,pointer,getter,address,reflection,generic,exact-CoreLib-signature,native-only");
        return true;
    }
}

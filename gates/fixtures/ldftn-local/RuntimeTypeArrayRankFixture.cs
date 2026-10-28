using System.Globalization;
using Mono.Cecil;
using Mono.Cecil.Cil;

internal static class RuntimeTypeArrayRankFixture
{
    public static bool Run(string[] args)
    {
        if (args.Length != 3 || args[0] is not ("--runtime-type-array-rank" or "--runtime-type-array-rank-reflection"
            or "--runtime-type-array-rank-public" or "--runtime-type-array-rank-oracle" or "--runtime-type-array-rank-oracle-reflection")) return false;
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        using var core = AssemblyDefinition.ReadAssembly(args[2]);
        if (core.Name.Name != "System.Private.CoreLib") throw new InvalidOperationException("Expected CoreLib");
        var runtimeType = core.MainModule.GetType("System.RuntimeType");
        var targetDefinition = runtimeType.Methods.Single(m => m.Name == "GetArrayRank");
        if (!runtimeType.IsSealed
            || targetDefinition.Attributes != (MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.HideBySig)
            || !targetDefinition.HasThis || targetDefinition.ExplicitThis || targetDefinition.HasGenericParameters
            || targetDefinition.CallingConvention != MethodCallingConvention.Default || targetDefinition.Parameters.Count != 0
            || targetDefinition.ReturnType.MetadataType != MetadataType.Int32 || !targetDefinition.HasBody
            || targetDefinition.RVA == 0 || targetDefinition.IsPInvokeImpl || targetDefinition.ImplAttributes != MethodImplAttributes.IL
            || !targetDefinition.Body.Instructions.Any(i => i.OpCode == OpCodes.Callvirt
                && i.Operand is MethodReference m && m.DeclaringType.FullName == "System.Type" && m.Name == "IsArrayImpl")
            || !targetDefinition.Body.Instructions.Any(i => i.OpCode == OpCodes.Call
                && i.Operand is MethodReference m && m.DeclaringType.FullName == "System.RuntimeTypeHandle" && m.Name == "GetArrayRank"))
            throw new InvalidOperationException("RuntimeType.GetArrayRank declaration changed");
        string path = Path.GetFullPath(args[1]);
        using var app = AssemblyDefinition.ReadAssembly(path, new ReaderParameters { InMemory = true });
        var module = app.MainModule;
        var checks = module.GetType("RuntimeTypeArrayRankSubset.ArrayRankChecks");
        bool publicOnly = args[0] == "--runtime-type-array-rank-public";
        bool reflectionOnly = args[0].EndsWith("-reflection", StringComparison.Ordinal);
        bool oracle = args[0].Contains("-oracle", StringComparison.Ordinal);
        string[] routes = publicOnly || oracle ? Array.Empty<string>() : reflectionOnly
            ? new[] { "ReflectionLeaf" }
            : new[] { "Direct", "Pointer", "Virtual", "VirtualPointer", "Constrained", "Address", "VirtualAddress", "FlowAddress", "ReflectionLeaf" };
        var target = module.ImportReference(targetDefinition);
        if (target.MetadataToken.TokenType != TokenType.MemberRef)
            throw new InvalidOperationException("Expected imported override MemberRef");
        foreach (string route in routes)
        {
            var stub = checks.Methods.Single(m => m.Name == route);
            stub.Body = new MethodBody(stub);
            var il = stub.Body.GetILProcessor();
            if (route == "Constrained")
            {
                stub.Body.InitLocals = true;
                var receiver = new VariableDefinition(module.ImportReference(runtimeType));
                stub.Body.Variables.Add(receiver);
                LoadReceiver(il, module, runtimeType);
                il.Emit(OpCodes.Stloc, receiver);
                il.Emit(OpCodes.Ldloca, receiver);
                il.Emit(OpCodes.Constrained, module.ImportReference(runtimeType));
                il.Emit(OpCodes.Callvirt, target);
            }
            else if (route == "FlowAddress")
            {
                stub.Body.InitLocals = true;
                var receiver = new VariableDefinition(module.ImportReference(runtimeType));
                var pointer = new VariableDefinition(module.TypeSystem.IntPtr);
                stub.Body.Variables.Add(receiver);
                stub.Body.Variables.Add(pointer);
                var direct = Instruction.Create(OpCodes.Ldftn, target);
                var bind = Instruction.Create(OpCodes.Ldloc, receiver);
                LoadReceiver(il, module, runtimeType);
                il.Emit(OpCodes.Stloc, receiver);
                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Brfalse, direct);
                il.Emit(OpCodes.Ldloc, receiver);
                il.Emit(OpCodes.Ldvirtftn, target);
                il.Emit(OpCodes.Stloc, pointer);
                il.Emit(OpCodes.Br, bind);
                il.Append(direct);
                il.Emit(OpCodes.Stloc, pointer);
                il.Append(bind);
                il.Emit(OpCodes.Ldloc, pointer);
                EmitDelegate(il, module, stub.ReturnType);
            }
            else
            {
                LoadReceiver(il, module, runtimeType);
                if (route is "Pointer" or "VirtualPointer")
                {
                    if (route == "VirtualPointer") il.Emit(OpCodes.Dup);
                    il.Emit(route == "VirtualPointer" ? OpCodes.Ldvirtftn : OpCodes.Ldftn, target);
                    var site = new CallSite(module.TypeSystem.Int32);
                    site.Parameters.Add(new ParameterDefinition(module.ImportReference(runtimeType)));
                    il.Emit(OpCodes.Calli, site);
                }
                else if (route is "Address" or "VirtualAddress")
                {
                    if (route == "VirtualAddress") il.Emit(OpCodes.Dup);
                    il.Emit(route == "VirtualAddress" ? OpCodes.Ldvirtftn : OpCodes.Ldftn, target);
                    EmitDelegate(il, module, stub.ReturnType);
                }
                else il.Emit(route == "Virtual" ? OpCodes.Callvirt : OpCodes.Call, target);
            }
            il.Emit(OpCodes.Ret);
        }
        module.EntryPoint = checks.Methods.Single(m => m.Name == (publicOnly ? "Run" : reflectionOnly ? "RunReflectionArtifact" : "RunArtifact"));
        app.Write(path, new WriterParameters { Timestamp = 0, DeterministicMvid = true });
        Console.WriteLine("runtime-type-array-rank-sites=" + (publicOnly ? "public-only" : reflectionOnly ? "reflection-only-local-wrapper"
            : "direct,calli,callvirt,constrained,ldftn,ldvirtftn,flowing,reflection-wrapper")
            + ",exact-CoreLib-override,MemberRef," + (publicOnly || oracle ? "public-CLR-oracle" : "native-only"));
        return true;
    }

    private static void LoadReceiver(ILProcessor il, ModuleDefinition module, TypeDefinition runtimeType)
    {
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Castclass, module.ImportReference(runtimeType));
    }

    private static void EmitDelegate(ILProcessor il, ModuleDefinition module, TypeReference type)
    {
        var constructor = new MethodReference(".ctor", module.TypeSystem.Void, type) { HasThis = true };
        constructor.Parameters.Add(new ParameterDefinition(module.TypeSystem.Object));
        constructor.Parameters.Add(new ParameterDefinition(module.TypeSystem.IntPtr));
        il.Emit(OpCodes.Newobj, constructor);
    }
}

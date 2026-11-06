using Mono.Cecil;
using Mono.Cecil.Cil;

internal static class MemberNotFoundFactoryFixture
{
    public static bool Run(string[] args)
    {
        if (args.Length != 3 || args[0] is not ("--member-not-found-factory" or "--member-not-found-factory-reflection")) return false;
        using var core = AssemblyDefinition.ReadAssembly(args[2]);
        if (core.Name.Name != "System.Private.CoreLib") throw new InvalidOperationException("Expected input CoreLib");
        var method = core.MainModule.GetType("System.Type").Methods.Single(m =>
            m.Name == "CreateGetMemberWithSameMetadataDefinitionAsNotFoundException");
        bool CoreType(TypeReference type, string fullName) => type.FullName == fullName && type.Resolve().Module == core.MainModule;
        if (!method.IsFamilyAndAssembly || !method.IsStatic || method.HasThis || method.ExplicitThis || method.HasGenericParameters
            || method.IsVirtual || method.IsAbstract || method.CallingConvention != MethodCallingConvention.Default
            || !method.HasBody || !method.IsIL || !method.IsManaged || method.IsInternalCall || method.IsPInvokeImpl
            || !CoreType(method.ReturnType, "System.ArgumentException") || method.Parameters.Count != 1
            || !CoreType(method.Parameters[0].ParameterType, "System.Reflection.MemberInfo")
            || method.Parameters[0].IsOptional || method.Parameters[0].HasConstant)
            throw new InvalidOperationException("Member-not-found factory declaration changed");
        var body = method.Body.Instructions;
        bool Call(int index, OpCode opcode, string owner, string name, bool instance, string result, params string[] parameters)
        {
            if (body[index].OpCode != opcode || body[index].Operand is not MethodReference target
                || !CoreType(target.DeclaringType, owner) || target.Name != name || target.HasThis != instance
                || target.ReturnType.FullName != result || target.Parameters.Count != parameters.Length)
                return false;
            for (int i = 0; i < parameters.Length; i++)
                if (target.Parameters[i].ParameterType.FullName != parameters[i]) return false;
            return true;
        }
        if (body.Count != 7 || method.Body.HasExceptionHandlers
            || !Call(0, OpCodes.Call, "System.SR", "get_Arg_MemberInfoNotFound", false, "System.String")
            || body[1].OpCode != OpCodes.Ldarg_0
            || !Call(2, OpCodes.Callvirt, "System.Reflection.MemberInfo", "get_Name", true, "System.String")
            || !Call(3, OpCodes.Call, "System.SR", "Format", false, "System.String", "System.String", "System.Object")
            || body[4].OpCode != OpCodes.Ldstr || body[4].Operand is not string parameterName || parameterName != "member"
            || !Call(5, OpCodes.Newobj, "System.ArgumentException", ".ctor", true, "System.Void", "System.String", "System.String")
            || body[6].OpCode != OpCodes.Ret)
            throw new InvalidOperationException("Member-not-found factory body changed");
        string path = Path.GetFullPath(args[1]);
        using var app = AssemblyDefinition.ReadAssembly(path, new ReaderParameters { InMemory = true });
        var module = app.MainModule;
        var program = module.GetType("MemberNotFoundFactorySubset.Program");
        bool reflectionOnly = args[0] == "--member-not-found-factory-reflection";
        if (!reflectionOnly)
        {
            var target = module.ImportReference(method);
            foreach (string route in new[] { "Direct", "Pointer" })
            {
                var stub = program.Methods.Single(m => m.Name == route);
                stub.Body = new MethodBody(stub);
                var il = stub.Body.GetILProcessor();
                il.Emit(OpCodes.Ldarg_0);
                if (route == "Pointer")
                {
                    il.Emit(OpCodes.Ldftn, target);
                    var site = new CallSite(target.ReturnType);
                    site.Parameters.Add(new ParameterDefinition(target.Parameters[0].ParameterType));
                    il.Emit(OpCodes.Calli, site);
                }
                else il.Emit(OpCodes.Call, target);
                il.Emit(OpCodes.Ret);
            }
        }
        module.EntryPoint = program.Methods.Single(m => m.Name == (reflectionOnly ? "RunReflectionChecks" : "RunArtifactChecks"));
        app.Write(path, new WriterParameters { Timestamp = 0, DeterministicMvid = true });
        Console.WriteLine("member-not-found-factory-sites=" + (reflectionOnly ? "reflection-only" : "direct,address,reflection")
            + ",exact-CoreLib-signature,native-only");
        return true;
    }
}

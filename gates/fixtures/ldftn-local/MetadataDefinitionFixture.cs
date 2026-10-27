using Mono.Cecil;
using Mono.Cecil.Cil;

internal static class MetadataDefinitionFixture
{
    public static bool Run(string[] args)
    {
        if (args.Length != 3 || args[0] != "--metadata-definition-routes") return false;
        using var core = AssemblyDefinition.ReadAssembly(args[2]);
        if (core.Name.Name != "System.Private.CoreLib") throw new InvalidOperationException("Expected input CoreLib");
        var method = core.MainModule.GetType("System.Reflection.MemberInfo").Methods.Single(m =>
            m.Name == "HasSameMetadataDefinitionAs");
        bool CoreType(TypeReference type, string fullName) => type.FullName == fullName && type.Resolve().Module == core.MainModule;
        if (!method.IsPublic || method.IsStatic || !method.HasThis || method.ExplicitThis || method.HasGenericParameters
            || !method.IsVirtual || method.IsFinal || method.IsAbstract || method.CallingConvention != MethodCallingConvention.Default
            || !method.HasBody || !method.IsIL || !method.IsManaged || method.IsInternalCall || method.IsPInvokeImpl
            || !CoreType(method.ReturnType, "System.Boolean") || method.Parameters.Count != 1
            || !CoreType(method.Parameters[0].ParameterType, "System.Reflection.MemberInfo")
            || method.Parameters[0].IsOptional || method.Parameters[0].HasConstant)
            throw new InvalidOperationException("Member metadata-definition declaration changed");
        var body = method.Body.Instructions;
        if (body.Count != 2 || method.Body.HasExceptionHandlers || body[0].OpCode != OpCodes.Call
            || body[0].Operand is not MethodReference factory || !CoreType(factory.DeclaringType, "System.NotImplemented")
            || factory.Name != "get_ByDesign" || factory.HasThis || factory.HasGenericParameters
            || factory.Parameters.Count != 0 || !CoreType(factory.ReturnType, "System.Exception")
            || body[1].OpCode != OpCodes.Throw)
            throw new InvalidOperationException("Member metadata-definition base body changed");
        string path = Path.GetFullPath(args[1]);
        using var app = AssemblyDefinition.ReadAssembly(path, new ReaderParameters { InMemory = true });
        var module = app.MainModule;
        var program = module.GetType("MetadataDefinitionRoutes.Program")
            ?? throw new InvalidOperationException("Missing member metadata-definition routes");
        var direct = program.Methods.Single(m => m.Name == "Direct");
        var address = program.Methods.Single(m => m.Name == "Address");
        if (!direct.IsPublic || !direct.IsStatic || direct.Parameters.Count != 2
            || direct.Parameters.Any(p => p.ParameterType.FullName != "System.Reflection.MemberInfo")
            || direct.ReturnType.FullName != "System.Boolean"
            || !address.IsPublic || !address.IsStatic || address.Parameters.Count != 1
            || address.Parameters[0].ParameterType.FullName != "System.Reflection.MemberInfo"
            || address.ReturnType.FullName != "System.Func`2<System.Reflection.MemberInfo,System.Boolean>")
            throw new InvalidOperationException("Member metadata-definition route signatures changed");
        var delegateConstructor = address.Body.Instructions.Single(i => i.OpCode == OpCodes.Newobj
            && i.Operand is MethodReference ctor && ctor.Name == ".ctor"
            && ctor.DeclaringType.FullName == address.ReturnType.FullName).Operand as MethodReference;
        if (delegateConstructor is null || !delegateConstructor.HasThis || delegateConstructor.Parameters.Count != 2
            || delegateConstructor.Parameters[0].ParameterType.FullName != "System.Object"
            || delegateConstructor.Parameters[1].ParameterType.FullName != "System.IntPtr")
            throw new InvalidOperationException("Member metadata-definition delegate constructor changed");
        var target = module.ImportReference(method);
        direct.Body = new MethodBody(direct);
        var directIl = direct.Body.GetILProcessor();
        directIl.Emit(OpCodes.Ldarg_0);
        directIl.Emit(OpCodes.Ldarg_1);
        directIl.Emit(OpCodes.Call, target);
        directIl.Emit(OpCodes.Ret);
        address.Body = new MethodBody(address);
        var addressIl = address.Body.GetILProcessor();
        addressIl.Emit(OpCodes.Ldarg_0);
        addressIl.Emit(OpCodes.Ldftn, target);
        addressIl.Emit(OpCodes.Newobj, delegateConstructor);
        addressIl.Emit(OpCodes.Ret);
        module.EntryPoint = program.Methods.Single(m => m.Name == "RunArtifactChecks");
        app.Write(path, new WriterParameters { Timestamp = 0, DeterministicMvid = true });
        Console.WriteLine("metadata-definition-sites=direct,bound-instance-address,exact-CoreLib-public-signature,managed-base-throw");
        return true;
    }
}

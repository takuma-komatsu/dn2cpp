using Mono.Cecil;
using Mono.Cecil.Cil;

internal static class GcCoreLibInputFixture
{
    public static bool Run(string[] args)
    {
        if (args.Length != 3 || args[0] is not ("--gc-missing-corelib-collect" or "--gc-missing-corelib-memory"))
            return false;
        bool collect = args[0] == "--gc-missing-corelib-collect";
        using var core = AssemblyDefinition.ReadAssembly(args[2]);
        var definition = core.MainModule.GetType("System.GC").Methods.Single(method =>
            method.IsPublic && method.IsStatic && !method.HasGenericParameters
            && (collect
                ? method.Name == "Collect" && method.ReturnType.MetadataType == MetadataType.Void
                    && method.Parameters.Count == 0
                : method.Name == "GetTotalMemory" && method.ReturnType.MetadataType == MetadataType.Int64
                    && method.Parameters.Count == 1
                    && method.Parameters[0].ParameterType.MetadataType == MetadataType.Boolean));
        string path = Path.GetFullPath(args[1]);
        using var app = AssemblyDefinition.ReadAssembly(path, new ReaderParameters { InMemory = true });
        var module = app.MainModule;
        var entry = module.EntryPoint;
        var owner = entry.DeclaringType;
        // Unrelated generic signatures must not fail before the missing GC input.
        foreach (var type in module.Types.ToArray())
            if (type.Name != "<Module>" && type != owner)
                module.Types.Remove(type);
        foreach (var method in owner.Methods.ToArray())
            if (method != entry)
                owner.Methods.Remove(method);
        owner.NestedTypes.Clear();
        owner.Fields.Clear();
        owner.Properties.Clear();
        owner.Events.Clear();
        owner.CustomAttributes.Clear();
        entry.CustomAttributes.Clear();
        app.CustomAttributes.Clear();
        module.CustomAttributes.Clear();
        entry.Parameters.Clear();
        entry.Body = new MethodBody(entry);
        var il = entry.Body.GetILProcessor();
        if (!collect)
            il.Emit(OpCodes.Ldc_I4_1);
        il.Emit(OpCodes.Call, module.ImportReference(definition));
        if (!collect)
            il.Emit(OpCodes.Pop);
        il.Emit(OpCodes.Ret);
        app.Write(path, new WriterParameters { Timestamp = 0, DeterministicMvid = true });

        using var check = AssemblyDefinition.ReadAssembly(path);
        var body = check.EntryPoint.Body;
        var calls = body.Instructions.Where(item => item.Operand is MethodReference).ToArray();
        if (check.MainModule.Types.Count != 2 || check.EntryPoint.DeclaringType.Methods.Count != 1
            || body.Variables.Count != 0 || body.Instructions.Count != (collect ? 2 : 4)
            || calls.Length != 1 || calls[0].OpCode != OpCodes.Call
            || calls[0].Operand is not MethodReference target
            || target.MetadataToken.TokenType != TokenType.MemberRef
            || target.DeclaringType.Scope.Name != "System.Private.CoreLib"
            || target.FullName != definition.FullName || target.HasThis || target.HasGenericParameters
            || target.CallingConvention != MethodCallingConvention.Default)
            throw new InvalidOperationException("GC missing-CoreLib fixture lost its isolated public MemberRef");
        Console.WriteLine("gc-missing-corelib-fixture:" + (collect ? "collect:" : "memory:") + definition.FullName);
        return true;
    }
}

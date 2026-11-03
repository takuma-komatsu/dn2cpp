using Mono.Cecil;
using Mono.Cecil.Cil;

internal static class EnumStaticFormatFixture
{
    public static bool Run(string[] args)
    {
        if (args.Length != 3 || args[0] is not ("--enum-static-format" or "--enum-static-format-public")) return false;
        using var core = AssemblyDefinition.ReadAssembly(args[2]);
        if (core.Name.Name != "System.Private.CoreLib") throw new InvalidOperationException("Expected CoreLib");
        var owner = core.MainModule.GetType("System.Enum");
        MethodDefinition Helper(int count)
        {
            var method = owner.Methods.Single(m => m.Name == "ToString" && m.IsStatic && m.Parameters.Count == count);
            if (method.Attributes != (MethodAttributes.Private | MethodAttributes.Static | MethodAttributes.HideBySig)
                || method.ImplAttributes != MethodImplAttributes.NoInlining || !method.HasBody || method.HasThis || method.ExplicitThis
                || method.CallingConvention != MethodCallingConvention.Generic || method.GenericParameters.Count != 2
                || method.ReturnType.MetadataType != MetadataType.String
                || method.Parameters[0].ParameterType is not TypeDefinition { FullName: "System.RuntimeType" } runtimeType
                || runtimeType.Module != core.MainModule || count == 3 && method.Parameters[1].ParameterType.MetadataType != MetadataType.Char
                || method.Parameters[^1].ParameterType is not ByReferenceType { ElementType.MetadataType: MetadataType.Byte })
                throw new InvalidOperationException("Static Enum formatter contract changed");
            return method;
        }
        var plain = Helper(2); var formatted = Helper(3);
        string path = Path.GetFullPath(args[1]);
        using var app = AssemblyDefinition.ReadAssembly(path, new ReaderParameters { InMemory = true });
        var module = app.MainModule;
        var program = module.GetType("EnumStaticFormatSubset.Program");
        if (args[0] == "--enum-static-format")
        {
            foreach (var (name, definition, pointer) in new[] {
                ("Direct", plain, false), ("Format", formatted, false),
                ("Pointer", plain, true), ("FormatPointer", formatted, true) })
            {
                var method = program.Methods.Single(m => m.Name == name);
                var target = new GenericInstanceMethod(module.ImportReference(definition));
                target.GenericArguments.Add(module.TypeSystem.Byte); target.GenericArguments.Add(module.TypeSystem.Byte);
                if (method.Parameters.Count != target.Parameters.Count) throw new InvalidOperationException("Static fixture arity changed");
                method.Body = new MethodBody(method);
                var il = method.Body.GetILProcessor();
                il.Emit(OpCodes.Ldarg_0); il.Emit(OpCodes.Castclass, target.Parameters[0].ParameterType);
                foreach (var parameter in method.Parameters.Skip(1)) il.Emit(OpCodes.Ldarg, parameter);
                if (pointer)
                {
                    il.Emit(OpCodes.Ldftn, target);
                    var call = new CallSite(target.ReturnType);
                    foreach (var parameter in target.Parameters) call.Parameters.Add(new ParameterDefinition(parameter.ParameterType));
                    il.Emit(OpCodes.Calli, call);
                }
                else il.Emit(OpCodes.Call, target);
                il.Emit(OpCodes.Ret);
            }
        }
        module.EntryPoint = program.Methods.Single(m => m.Name == "RunProbe");
        app.Write(path, new WriterParameters { Timestamp = 0, DeterministicMvid = true });
        Console.WriteLine("enum-static-fixture=" + args[0] + ":two-real-generic-helpers,private-calls-native-only");
        return true;
    }
}

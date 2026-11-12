using System.Globalization;
using Mono.Cecil;
using Mono.Cecil.Cil;

// Points the gen2-callback stubs at CoreLib's private Gen2GcCallback. The
// rewritten assemblies are native-only and never run on the CLR.
internal static class Gen2CallbackFixture
{
    public static bool Run(string[] args)
    {
        if (args.Length != 3 || !new[] { "--gen2-callback-base", "--gen2-callback-patch", "--gen2-callback-minimal", "--gen2-callback-newobj", "--gen2-callback-memberref" }.Contains(args[0]))
            return false;
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        using var core = AssemblyDefinition.ReadAssembly(args[2]);
        var targetType = core.MainModule.GetType("System.Gen2GcCallback")
            ?? throw new InvalidOperationException("System.Gen2GcCallback is missing");
        var ctor0 = targetType.Methods.Single(m => m.IsConstructor && !m.IsStatic && m.Parameters.Count == 1);
        string path = Path.GetFullPath(args[1]);
        using var app = AssemblyDefinition.ReadAssembly(path, new ReaderParameters { InMemory = true });
        var module = app.MainModule;
        var constructor = module.ImportReference(ctor0);
        if (args[0] == "--gen2-callback-patch")
        {
            var create = Body(module.GetType("Gen2CallbackPatch.Program"), "Create0");
            create.Emit(OpCodes.Ldnull);
            create.Emit(OpCodes.Newobj, constructor);
            create.Emit(OpCodes.Pop);
            create.Emit(OpCodes.Ret);
        }
        else
        {
            var program = module.GetType("Gen2CallbackSubset.Program");
            var typeIl = Body(program, "TargetType");
            typeIl.Emit(OpCodes.Ldtoken, module.ImportReference(targetType));
            typeIl.Emit(OpCodes.Call, module.ImportReference(core.MainModule.GetType("System.Type").Methods.Single(m => m.Name == "GetTypeFromHandle")));
            typeIl.Emit(OpCodes.Ret);
            string entry = "RunReflectionChecks";
            if (args[0] == "--gen2-callback-minimal")
            {
                // Signature modifiers enter the reference-only TI pass after
                // emitted layouts are fixed; a typeof token would add a shell.
                var anchor = program.Methods.Single(m => m.Name == "MinimalMetadataAnchor");
                anchor.Parameters[0].ParameterType = new OptionalModifierType(
                    module.ImportReference(targetType), module.TypeSystem.Int32);
                entry = "RunMinimalChecks";
            }
            else if (args[0] == "--gen2-callback-newobj")
            {
                var newobj = Body(program, "StaticNewobj");
                newobj.Emit(OpCodes.Ldnull);
                newobj.Emit(OpCodes.Newobj, constructor);
                newobj.Emit(OpCodes.Pop);
                newobj.Emit(OpCodes.Ret);
                entry = "RunNewobjNegative";
            }
            else if (args[0] == "--gen2-callback-memberref")
            {
                // The constructor reference declares an Object return instead of void.
                var malformed = new MethodReference(constructor.Name, module.TypeSystem.Object, constructor.DeclaringType)
                {
                    HasThis = constructor.HasThis,
                    ExplicitThis = constructor.ExplicitThis,
                    CallingConvention = constructor.CallingConvention
                };
                foreach (var parameter in constructor.Parameters)
                    malformed.Parameters.Add(new ParameterDefinition(parameter.ParameterType));
                var address = Body(program, "MalformedAddress");
                address.Emit(OpCodes.Ldftn, malformed);
                address.Emit(OpCodes.Pop);
                address.Emit(OpCodes.Ret);
                entry = "RunMemberRefNegative";
            }
            module.EntryPoint = args[0] == "--gen2-callback-base"
                ? module.GetType("Gen2CallbackInterp.Host").Methods.Single(m => m.Name == "Main")
                : program.Methods.Single(m => m.Name == entry);
        }
        app.Write(path, new WriterParameters { Timestamp = 0, DeterministicMvid = true });
        Console.WriteLine("gen2-fixture:" + args[0]["--gen2-callback-".Length..]);
        return true;
    }

    private static ILProcessor Body(TypeDefinition type, string name)
    {
        var method = type.Methods.Single(m => m.Name == name);
        method.Body = new MethodBody(method);
        return method.Body.GetILProcessor();
    }
}

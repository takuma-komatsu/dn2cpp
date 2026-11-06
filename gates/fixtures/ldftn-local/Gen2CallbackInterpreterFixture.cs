using System.Globalization;
using Mono.Cecil;
using Mono.Cecil.Cil;

internal static class Gen2CallbackInterpreterFixture
{
    public static bool Run(string[] args)
    {
        if (args.Length != 3 || !new[] { "--gen2-interp-base", "--gen2-interp-patch", "--gen2-interp-sealed" }.Contains(args[0]))
            return false;
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        if (args[0] == "--gen2-interp-base")
            Gen2CallbackFixture.Run(new[] { "--gen2-callback", args[1], args[2] });
        using var core = AssemblyDefinition.ReadAssembly(args[2]);
        var type = core.MainModule.GetType("System.Gen2GcCallback");
        if (!type.IsSealed || type.IsValueType || type.HasGenericParameters
            || type.BaseType.FullName != "System.Runtime.ConstrainedExecution.CriticalFinalizerObject")
            throw new InvalidOperationException("Gen2 callback type changed");
        var ctors = new[] { 1, 2 }.Select(count => type.Methods.Single(m => m.IsConstructor && !m.IsStatic && m.Parameters.Count == count)).ToArray();
        if (ctors.Any(m => (int)m.Attributes != 0x1881 || m.ImplAttributes != 0 || !m.HasBody))
            throw new InvalidOperationException("Gen2 callback constructors changed");
        string path = Path.GetFullPath(args[1]);
        using var app = AssemblyDefinition.ReadAssembly(path, new ReaderParameters { InMemory = true });
        var module = app.MainModule;
        if (args[0] == "--gen2-interp-base")
        {
            // Hot-update bases export all user methods, including otherwise dead
            // negative-fixture bodies. This host does not test static newobj.
            var fixtureProgram = module.GetType("Gen2CallbackSubset.Program");
            foreach (string name in new[] { "StaticNewobj", "RunNewobjNegative" })
                fixtureProgram.Methods.Remove(fixtureProgram.Methods.Single(m => m.Name == name));
            module.EntryPoint = module.GetType("Gen2CallbackInterp.Host").Methods.Single(m => m.Name == "Main");
        }
        else if (args[0] == "--gen2-interp-patch")
        {
            var program = module.GetType("Gen2CallbackPatch.Program");
            for (int index = 0; index < ctors.Length; index++)
            {
                var method = program.Methods.SingleOrDefault(m => m.Name == "Create" + index);
                if (method is null && index == 1) continue;
                if (method is null) throw new InvalidOperationException("constructor probe missing");
                method.Body = new MethodBody(method);
                var il = method.Body.GetILProcessor();
                for (int argument = 0; argument <= index; argument++) il.Emit(OpCodes.Ldnull);
                il.Emit(OpCodes.Newobj, module.ImportReference(ctors[index]));
                il.Emit(OpCodes.Pop);
                il.Emit(OpCodes.Ret);
            }
        }
        else
        {
            var derived = module.GetType("Gen2CallbackSealedPatch.Derived");
            derived.BaseType = module.ImportReference(type);
            var constructor = derived.Methods.Single(m => m.IsConstructor);
            constructor.Body = new MethodBody(constructor);
            var il = constructor.Body.GetILProcessor();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldnull);
            il.Emit(OpCodes.Call, module.ImportReference(ctors[0]));
            il.Emit(OpCodes.Ret);
        }
        app.Write(path, new WriterParameters { Timestamp = 0, DeterministicMvid = true });
        return true;
    }
}

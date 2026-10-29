using System.Globalization;
using Mono.Cecil;
using Mono.Cecil.Cil;

internal static class AppContextExitFixture
{
    public static bool Run(string[] args)
    {
        bool negative = args.Length == 4 && args[0] == "--appcontext-exit-negative";
        if (!negative && (args.Length != 3 || !new[]
            { "--appcontext-exit", "--appcontext-exit-reflection", "--appcontext-exit-noalloc", "--appcontext-exit-oracle" }.Contains(args[0])))
            return false;
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        using var core = AssemblyDefinition.ReadAssembly(args[2]);
        var definition = core.MainModule.GetType("System.AppContext").Methods.Single(m => m.Name == "OnProcessExit");
        if ((int)definition.Attributes != 0x93 || (int)definition.ImplAttributes != 0 || !definition.HasBody
            || definition.HasGenericParameters || definition.Parameters.Count != 0 || definition.ReturnType.MetadataType != MetadataType.Void)
            throw new InvalidOperationException("AppContext.OnProcessExit declaration changed");
        string path = Path.GetFullPath(args[1]);
        using var app = AssemblyDefinition.ReadAssembly(path, new ReaderParameters { InMemory = true });
        var module = app.MainModule;
        var program = module.GetType("AppContextExitSubset.Program");
        MethodDefinition Method(string name) => program.Methods.Single(m => m.Name == name);
        if (args[0] == "--appcontext-exit-oracle")
        {
            var entry = Method("RunArtifactChecks");
            entry.Body = new MethodBody(entry);
            var il = entry.Body.GetILProcessor();
            il.Emit(OpCodes.Call, Method("Run"));
            il.Emit(OpCodes.Ret);
            module.EntryPoint = entry;
        }
        else
        {
            var target = module.ImportReference(definition);
            foreach (string name in new[] { "Direct", "Pointer" })
            {
                var stub = Method(name);
                stub.Body = new MethodBody(stub);
                var il = stub.Body.GetILProcessor();
                if (name == "Pointer")
                {
                    il.Emit(OpCodes.Ldftn, target);
                    il.Emit(OpCodes.Calli, new CallSite(module.TypeSystem.Void));
                }
                else il.Emit(OpCodes.Call, target);
                il.Emit(OpCodes.Ret);
            }
            var address = Method("Address");
            var actionCtor = module.ImportReference(core.MainModule.GetType("System.Action").Methods.Single(m => m.IsConstructor));
            address.Body = new MethodBody(address);
            var ai = address.Body.GetILProcessor();
            ai.Emit(OpCodes.Ldnull);
            ai.Emit(OpCodes.Ldftn, target);
            ai.Emit(OpCodes.Newobj, module.ImportReference(actionCtor));
            ai.Emit(OpCodes.Ret);
            if (negative)
            {
                string mode = args[3];
                bool pointer = mode.StartsWith("address-", StringComparison.Ordinal);
                if (pointer) mode = mode.Substring(8);
                var malformed = new MethodReference(target.Name, module.TypeSystem.Void, target.DeclaringType) { HasThis = false };
                switch (mode)
                {
                    case "return": malformed.ReturnType = module.TypeSystem.Boolean; break;
                    case "modifier": malformed.ReturnType = new OptionalModifierType(module.TypeSystem.Object, module.TypeSystem.Void); break;
                    case "argument": malformed.Parameters.Add(new ParameterDefinition(module.TypeSystem.Int32)); break;
                    case "instance": malformed.HasThis = true; break;
                    case "generic": malformed.GenericParameters.Add(new GenericParameter("T", malformed)); break;
                    default: throw new InvalidOperationException("unknown negative " + mode);
                }
                Method(pointer ? "Address" : "Direct").Body.Instructions.Single(i => i.OpCode == (pointer ? OpCodes.Ldftn : OpCodes.Call)).Operand = malformed;
            }
            if (args[0] == "--appcontext-exit-noalloc")
                RuntimeTypeDefaultCtorFixture.MarkNoAlloc(module, Method("Direct"));
            module.EntryPoint = Method(args[0] == "--appcontext-exit-reflection" ? "RunReflectionChecks" : "RunArtifactChecks");
        }
        app.Write(path, new WriterParameters { Timestamp = 0, DeterministicMvid = true });
        return true;
    }
}

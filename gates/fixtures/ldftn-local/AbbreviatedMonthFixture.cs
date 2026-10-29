using System.Globalization;
using Mono.Cecil;
using Mono.Cecil.Cil;

internal static class AbbreviatedMonthFixture
{
    public static bool Run(string[] args)
    {
        const string targetName = "GetAbbreviatedMonthName";
        bool negative = args.Length == 4 && args[0] == "--abbreviated-month-negative";
        if (!negative && (args.Length != 3 || !new[] { "--abbreviated-month", "--abbreviated-month-direct", "--abbreviated-month-reflection", "--abbreviated-month-noalloc", "--abbreviated-month-oracle", "--abbreviated-month-direct-oracle", "--abbreviated-month-reflection-oracle" }.Contains(args[0])))
            return false;
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        using var core = AssemblyDefinition.ReadAssembly(args[2]);
        var definition = core.MainModule.GetType("System.Globalization.DateTimeFormatInfo").Methods.Single(m => m.Name == targetName);
        if ((int)definition.Attributes != 0x86 || definition.ImplAttributes != 0 || !definition.HasBody
            || definition.HasGenericParameters || definition.Parameters.Count != 1 || definition.ReturnType.MetadataType != MetadataType.String
            || definition.Parameters[0].ParameterType.MetadataType != MetadataType.Int32)
            throw new InvalidOperationException("abbreviated month declaration changed");
        string path = Path.GetFullPath(args[1]);
        using var app = AssemblyDefinition.ReadAssembly(path, new ReaderParameters { InMemory = true });
        var module = app.MainModule;
        var program = module.GetType("AbbreviatedMonthSubset.Program");
        MethodDefinition Method(string name) => program.Methods.Single(m => m.Name == name);
        ILProcessor Body(string name)
        {
            var method = Method(name);
            method.Body = new MethodBody(method);
            return method.Body.GetILProcessor();
        }
        if (!args[0].EndsWith("-oracle", StringComparison.Ordinal))
        {
            var target = module.ImportReference(definition);
            foreach (string name in new[] { "Direct", "Pointer" })
            {
                var il = Body(name);
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldarg_1);
                if (name == "Pointer")
                {
                    il.Emit(OpCodes.Ldftn, target);
                    var site = new CallSite(module.TypeSystem.String);
                    site.Parameters.Add(new ParameterDefinition(target.DeclaringType));
                    site.Parameters.Add(new ParameterDefinition(module.TypeSystem.Int32));
                    il.Emit(OpCodes.Calli, site);
                }
                else il.Emit(OpCodes.Call, target);
                il.Emit(OpCodes.Ret);
            }
            var delegateCtor = (MethodReference)Method("Address").Body.Instructions.Single(i => i.OpCode == OpCodes.Newobj).Operand;
            var ai = Body("Address");
            ai.Emit(OpCodes.Ldarg_0);
            ai.Emit(OpCodes.Ldftn, target);
            ai.Emit(OpCodes.Newobj, delegateCtor);
            ai.Emit(OpCodes.Ret);
            if (args[0] == "--abbreviated-month-noalloc")
                RuntimeTypeDefaultCtorFixture.MarkNoAlloc(module, Method("Direct"));
            if (negative)
            {
                bool pointer = args[3].StartsWith("address-", StringComparison.Ordinal);
                string mode = pointer ? args[3].Substring(8) : args[3];
                var bad = new MethodReference(target.Name, target.ReturnType, target.DeclaringType) { HasThis = true };
                bad.Parameters.Add(new ParameterDefinition(module.TypeSystem.Int32));
                switch (mode)
                {
                    case "return": bad.ReturnType = module.TypeSystem.Object; break;
                    case "argument": bad.Parameters[0].ParameterType = module.TypeSystem.Int64; break;
                    case "enum": bad.Parameters[0].ParameterType = module.ImportReference(core.MainModule.GetType("System.DayOfWeek")); break;
                    case "byref": bad.Parameters[0].ParameterType = new ByReferenceType(bad.Parameters[0].ParameterType); break;
                    case "modifier": bad.Parameters[0].ParameterType = new OptionalModifierType(module.TypeSystem.Object, bad.Parameters[0].ParameterType); break;
                    case "static": bad.HasThis = false; break;
                    default: throw new InvalidOperationException("unknown abbreviated month negative");
                }
                Method(pointer ? "Pointer" : "Direct").Body.Instructions.Single(i => i.OpCode == (pointer ? OpCodes.Ldftn : OpCodes.Call)).Operand = bad;
            }
        }
        module.EntryPoint = Method(args[0] switch
        {
            "--abbreviated-month-reflection" or "--abbreviated-month-reflection-oracle" => "RunReflectionChecks",
            "--abbreviated-month-direct" or "--abbreviated-month-direct-oracle" or "--abbreviated-month-noalloc" => "RunDirectChecks",
            _ => "Run"
        });
        app.Write(path, new WriterParameters { Timestamp = 0, DeterministicMvid = true });
        return true;
    }
}

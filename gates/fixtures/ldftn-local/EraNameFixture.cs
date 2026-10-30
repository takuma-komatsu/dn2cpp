using System.Globalization;
using Mono.Cecil;
using Mono.Cecil.Cil;

internal static class EraNameFixture
{
    public static bool Run(string[] args)
    {
        if (args.Length == 0) return false;
        bool abbreviated = args[0].StartsWith("--abbreviated-era-name", StringComparison.Ordinal);
        string flag = abbreviated ? "--era-name" + args[0].Substring("--abbreviated-era-name".Length) : args[0];
        bool negative = args.Length == 4 && flag == "--era-name-negative";
        if (!negative && (args.Length != 3 || !new[] { "--era-name", "--era-name-direct", "--era-name-reflection", "--era-name-minimal", "--era-name-minimal-oracle", "--era-name-noalloc", "--era-name-oracle", "--era-name-direct-oracle", "--era-name-reflection-oracle" }.Contains(flag)))
            return false;
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        using var core = AssemblyDefinition.ReadAssembly(args[2]);
        string path = Path.GetFullPath(args[1]);
        using var app = AssemblyDefinition.ReadAssembly(path, new ReaderParameters { InMemory = true });
        var module = app.MainModule;
        var program = module.GetType("EraNameSubset.Program");
        MethodDefinition Method(string name) => program.Methods.Single(m => m.Name == name);
        ILProcessor Body(string name)
        {
            var method = Method(name);
            method.Body = new MethodBody(method);
            return method.Body.GetILProcessor();
        }
        if (!flag.EndsWith("-oracle", StringComparison.Ordinal))
        {
            foreach (bool shortName in new[] { false, true })
            {
                string targetName = shortName ? "GetAbbreviatedEraName" : "GetEraName";
                string suffix = shortName ? "Abbreviated" : "";
                var definition = core.MainModule.GetType("System.Globalization.DateTimeFormatInfo").Methods.Single(m => m.Name == targetName);
                if (definition.MetadataToken.ToInt32() != (shortName ? 0x060033f9 : 0x060033f7) || (int)definition.Attributes != 0x86 || definition.ImplAttributes != 0 || !definition.HasBody
                    || definition.HasGenericParameters || definition.Parameters.Count != 1 || definition.ReturnType.MetadataType != MetadataType.String
                    || definition.Parameters[0].ParameterType.MetadataType != MetadataType.Int32)
                    throw new InvalidOperationException("era name declaration changed");
                var target = module.ImportReference(definition);
                foreach (string name in shortName ? new[] { "Direct", "Virtual", "Pointer" } : new[] { "Direct", "Pointer" })
                {
                    var il = Body(name + suffix);
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
                    else il.Emit(name == "Virtual" ? OpCodes.Callvirt : OpCodes.Call, target);
                    il.Emit(OpCodes.Ret);
                }
                var delegateCtor = (MethodReference)Method("Address" + suffix).Body.Instructions.Single(i => i.OpCode == OpCodes.Newobj).Operand;
                var ai = Body("Address" + suffix);
                ai.Emit(OpCodes.Ldarg_0);
                ai.Emit(OpCodes.Ldftn, target);
                ai.Emit(OpCodes.Newobj, delegateCtor);
                ai.Emit(OpCodes.Ret);
                if (flag == "--era-name-noalloc" && shortName == abbreviated)
                    RuntimeTypeDefaultCtorFixture.MarkNoAlloc(module, Method("Direct" + suffix));
                if (negative && shortName == abbreviated)
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
                        case "generic": bad.GenericParameters.Add(new GenericParameter("T", bad)); break;
                        case "arity-zero": bad.Parameters.Clear(); break;
                        case "arity-two": bad.Parameters.Add(new ParameterDefinition(module.TypeSystem.Int32)); break;
                        case "static": bad.HasThis = false; break;
                        default: throw new InvalidOperationException("unknown era name negative");
                    }
                    Method((pointer ? "Pointer" : "Direct") + suffix).Body.Instructions.Single(i => i.OpCode == (pointer ? OpCodes.Ldftn : OpCodes.Call)).Operand = bad;
                }
            }
        }
        string entry = flag switch
        {
            "--era-name-minimal" or "--era-name-minimal-oracle" => "RunMinimal",
            "--era-name-reflection" or "--era-name-reflection-oracle" => "RunReflectionChecks",
            "--era-name-direct" or "--era-name-direct-oracle" or "--era-name-noalloc" => "RunDirectChecks",
            _ => "Run"
        };
        if (abbreviated) entry = entry == "Run" ? "RunAbbreviated" : entry.Replace("Run", "RunAbbreviated", StringComparison.Ordinal);
        module.EntryPoint = Method(entry);
        app.Write(path, new WriterParameters { Timestamp = 0, DeterministicMvid = true });
        return true;
    }
}

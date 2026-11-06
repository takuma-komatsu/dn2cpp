using System.Globalization;
using Mono.Cecil;
using Mono.Cecil.Cil;

internal static class ForceTwoDigitYearsFixture
{
    public static bool Run(string[] args)
    {
        bool negative = args.Length == 4 && args[0] == "--force-two-digit-years-negative";
        if (!negative && (args.Length != 3 || !new[] { "--force-two-digit-years", "--force-two-digit-years-direct",
            "--force-two-digit-years-reflection", "--force-two-digit-years-minimal", "--force-two-digit-years-noalloc", "--force-two-digit-years-oracle" }.Contains(args[0])))
            return false;
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        using var core = AssemblyDefinition.ReadAssembly(args[2]);
        var definition = core.MainModule.GetType("System.Globalization.DateTimeFormatInfo").Methods.Single(m => m.Name == "get_HasForceTwoDigitYears");
        if ((int)definition.Attributes != 0x883 || definition.ImplAttributes != 0 || definition.RVA == 0
            || definition.HasGenericParameters || definition.Parameters.Count != 0
            || definition.ReturnType.MetadataType != MetadataType.Boolean)
            throw new InvalidOperationException("force-two-digit years declaration changed");
        string path = Path.GetFullPath(args[1]);
        using var app = AssemblyDefinition.ReadAssembly(path, new ReaderParameters { InMemory = true });
        var module = app.MainModule;
        var program = module.GetType("ForceTwoDigitYearsSubset.Program");
        MethodDefinition Method(string name) => program.Methods.Single(m => m.Name == name);
        ILProcessor Body(string name)
        {
            var method = Method(name);
            method.Body = new MethodBody(method);
            return method.Body.GetILProcessor();
        }
        if (args[0] != "--force-two-digit-years-oracle")
        {
            var target = module.ImportReference(definition);
            foreach (string name in new[] { "Direct", "Pointer" })
            {
                var il = Body(name);
                il.Emit(OpCodes.Ldarg_0);
                if (name == "Pointer")
                {
                    il.Emit(OpCodes.Ldftn, target);
                    var site = new CallSite(module.TypeSystem.Boolean);
                    site.Parameters.Add(new ParameterDefinition(target.DeclaringType));
                    il.Emit(OpCodes.Calli, site);
                }
                else il.Emit(OpCodes.Call, target);
                il.Emit(OpCodes.Ret);
            }
            var closed = program.NestedTypes.Single(t => t.Name == "ClosedQuery");
            var ai = Body("Address");
            ai.Emit(OpCodes.Ldarg_0);
            ai.Emit(OpCodes.Ldftn, target);
            ai.Emit(OpCodes.Newobj, closed.Methods.Single(m => m.IsConstructor));
            ai.Emit(OpCodes.Ret);
            // Only the adapter leaf is annotated; caller/reflection boxing remains outside NoAlloc.
            RuntimeTypeDefaultCtorFixture.MarkNoAlloc(module, Method("Direct"));
            if (negative)
            {
                bool pointer = args[3].StartsWith("address-", StringComparison.Ordinal);
                string mode = pointer ? args[3].Substring(8) : args[3];
                var bad = new MethodReference(target.Name, target.ReturnType, target.DeclaringType) { HasThis = true };
                switch (mode)
                {
                    case "int": bad.ReturnType = module.TypeSystem.Int32; break;
                    case "enum": bad.ReturnType = module.ImportReference(core.MainModule.GetType("System.Globalization.MonthNameStyles")); break;
                    case "named-bool": bad.ReturnType = new TypeReference("System", "Boolean", module, target.DeclaringType.Scope, true); break;
                    case "byref": bad.ReturnType = new ByReferenceType(module.TypeSystem.Boolean); break;
                    case "modifier": bad.ReturnType = new OptionalModifierType(module.TypeSystem.Object, module.TypeSystem.Boolean); break;
                    case "arity": bad.Parameters.Add(new ParameterDefinition(module.TypeSystem.Int32)); break;
                    case "static": bad.HasThis = false; break;
                    case "generic": bad.GenericParameters.Add(new GenericParameter("T", bad)); break;
                    default: throw new InvalidOperationException("unknown force-two-digit years negative");
                }
                Method(pointer ? "Pointer" : "Direct").Body.Instructions.Single(i => i.OpCode == (pointer ? OpCodes.Ldftn : OpCodes.Call)).Operand = bad;
            }
        }
        module.EntryPoint = Method(args[0] switch
        {
            "--force-two-digit-years-oracle" => "RunPublicOracle",
            "--force-two-digit-years-reflection" => "RunReflectionChecks",
            "--force-two-digit-years-minimal" => "RunMinimal",
            "--force-two-digit-years-direct" or "--force-two-digit-years-noalloc" => "RunDirectChecks",
            _ => "Run"
        });
        app.Write(path, new WriterParameters { Timestamp = 0, DeterministicMvid = true });
        return true;
    }
}

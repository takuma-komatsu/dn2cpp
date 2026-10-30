using System.Globalization;
using Mono.Cecil;
using Mono.Cecil.Cil;

internal static class DtfIParserFlagsFixture
{
    public static bool Run(string[] args)
    {
        bool negative = args.Length == 4 && args[0] == "--dtfi-parser-flags-negative";
        if (!negative && (args.Length != 3 || !new[] { "--dtfi-parser-flags", "--dtfi-parser-flags-reflection", "--dtfi-parser-flags-minimal", "--dtfi-parser-flags-oracle" }.Contains(args[0]))) return false;
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        using var core = AssemblyDefinition.ReadAssembly(args[2]);
        var owner = core.MainModule.GetType("System.Globalization.DateTimeFormatInfo");
        string path = Path.GetFullPath(args[1]);
        using var app = AssemblyDefinition.ReadAssembly(path, new ReaderParameters { InMemory = true });
        var module = app.MainModule;
        var program = module.GetType("DtfIParserFlagsSubset.Program");
        MethodDefinition Method(string name) => program.Methods.Single(method => method.Name == name);
        ILProcessor Body(string name)
        {
            var method = Method(name);method.Body = new MethodBody(method);return method.Body.GetILProcessor();
        }
        int mutations = 0;
        if (args[0] != "--dtfi-parser-flags-oracle")
        foreach (var row in new[] {
            (Suffix: "Month", Name: "get_HasSpacesInMonthNames", Token: 0x0600342c),
            (Suffix: "Day", Name: "get_HasSpacesInDayNames", Token: 0x0600342d),
            (Suffix: "Adjustment", Name: "get_HasYearMonthAdjustment", Token: 0x06003455) })
        {
            var definition = owner.Methods.Single(method => method.Name == row.Name);
            if (definition.MetadataToken.ToInt32() != row.Token || (int)definition.Attributes != 0x883
                || definition.ImplAttributes != 0 || definition.RVA == 0 || definition.HasGenericParameters
                || definition.Parameters.Count != 0 || definition.ReturnType.MetadataType != MetadataType.Boolean)
                throw new InvalidOperationException("installed parser flag declaration changed");
            var target = module.ImportReference(definition);
            foreach (string route in new[] { "Direct", "Virtual", "Pointer" })
            {
                var il = Body(route + row.Suffix);il.Emit(OpCodes.Ldarg_0);
                if (route == "Pointer")
                {
                    il.Emit(OpCodes.Ldftn, target);
                    var site = new CallSite(module.TypeSystem.Boolean);site.Parameters.Add(new ParameterDefinition(target.DeclaringType));
                    il.Emit(OpCodes.Calli, site);
                }
                else il.Emit(route == "Virtual" ? OpCodes.Callvirt : OpCodes.Call, target);
                il.Emit(OpCodes.Ret);
            }
            var address = Body("Address" + row.Suffix);address.Emit(OpCodes.Ldarg_0);address.Emit(OpCodes.Ldftn, target);
            address.Emit(OpCodes.Newobj, program.NestedTypes.Single(type => type.Name == "ClosedQuery").Methods.Single(method => method.IsConstructor));address.Emit(OpCodes.Ret);
            Method("Producer" + row.Suffix).Body.Instructions.Single(instruction => instruction.OpCode == OpCodes.Call && instruction.Operand is MethodReference method && method.Name == "Direct" + row.Suffix).Operand = target;
            RuntimeTypeDefaultCtorFixture.MarkNoAlloc(module, Method("Direct" + row.Suffix));
            if (negative)
            {
                string mode = args[3];bool pointer = mode.StartsWith("address-", StringComparison.Ordinal);
                if (pointer) mode = mode.Substring(8);
                string prefix = row.Suffix.ToLowerInvariant() + "-";
                if (!mode.StartsWith(prefix, StringComparison.Ordinal)) continue;
                mode = mode.Substring(prefix.Length);
                mutations++;
                var bad = new MethodReference(target.Name, target.ReturnType, target.DeclaringType) { HasThis = true };
                switch (mode)
                {
                    case "int": bad.ReturnType = module.TypeSystem.Int32;break;
                    case "named-bool": bad.ReturnType = new TypeReference("System", "Boolean", module, target.DeclaringType.Scope, true);break;
                    case "enum": bad.ReturnType = module.ImportReference(core.MainModule.GetType("System.Globalization.MonthNameStyles"));break;
                    case "byref": bad.ReturnType = new ByReferenceType(module.TypeSystem.Boolean);break;
                    case "modifier": bad.ReturnType = new OptionalModifierType(module.TypeSystem.Object, module.TypeSystem.Boolean);break;
                    case "arity": bad.Parameters.Add(new ParameterDefinition(module.TypeSystem.Int32));break;
                    case "static": bad.HasThis = false;break;
                    case "generic": bad.GenericParameters.Add(new GenericParameter("T", bad));break;
                    default: throw new InvalidOperationException("unknown parser flag negative");
                }
                Method((pointer ? "Pointer" : "Direct") + row.Suffix).Body.Instructions.Single(instruction => instruction.OpCode == (pointer ? OpCodes.Ldftn : OpCodes.Call)).Operand = bad;
            }
        }
        if (negative && mutations != 1) throw new InvalidOperationException("negative must alter exactly one actual target");
        module.EntryPoint = Method(args[0] switch { "--dtfi-parser-flags-oracle" => "RunPublicOracle", "--dtfi-parser-flags-reflection" => "RunReflectionChecks", "--dtfi-parser-flags-minimal" => "RunMinimal", _ => "Run" });
        app.Write(path, new WriterParameters { Timestamp = 0, DeterministicMvid = true });
        return true;
    }
}

using System.Globalization;
using Mono.Cecil;
using Mono.Cecil.Cil;

internal static class YearMonthAdjustmentFixture
{
    public static bool Run(string[] args)
    {
        bool negative = args.Length == 4 && args[0] == "--year-month-adjustment-negative";
        if (!negative && (args.Length != 3 || !new[] { "--year-month-adjustment", "--year-month-adjustment-reflection", "--year-month-adjustment-minimal" }.Contains(args[0]))) return false;
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        using var core = AssemblyDefinition.ReadAssembly(args[2]);
        var owner = core.MainModule.GetType("System.Globalization.DateTimeFormatInfo");
        var definition = owner.Methods.Single(method => method.Name == "YearMonthAdjustment");
        var hasDefinition = owner.Methods.Single(method => method.Name == "get_HasYearMonthAdjustment");
        if ((int)definition.Attributes != 0x83 || definition.ImplAttributes != 0 || definition.RVA == 0 || definition.HasGenericParameters
            || definition.ReturnType.MetadataType != MetadataType.Boolean || definition.Parameters.Count != 3
            || definition.Parameters[0].ParameterType is not ByReferenceType { ElementType.MetadataType: MetadataType.Int32 }
            || definition.Parameters[1].ParameterType is not ByReferenceType { ElementType.MetadataType: MetadataType.Int32 }
            || definition.Parameters[2].ParameterType.MetadataType != MetadataType.Boolean
            || (int)hasDefinition.Attributes != 0x883 || hasDefinition.ImplAttributes != 0 || hasDefinition.RVA == 0
            || hasDefinition.HasGenericParameters || hasDefinition.Parameters.Count != 0 || hasDefinition.ReturnType.MetadataType != MetadataType.Boolean)
            throw new InvalidOperationException("year/month adjustment declaration changed");
        string path = Path.GetFullPath(args[1]);
        using var app = AssemblyDefinition.ReadAssembly(path, new ReaderParameters { InMemory = true });
        var module = app.MainModule;
        var program = module.GetType("YearMonthAdjustmentSubset.Program");
        MethodDefinition Method(string name) => program.Methods.Single(method => method.Name == name);
        ILProcessor Body(string name)
        {
            var method = Method(name);
            method.Body = new MethodBody(method);
            return method.Body.GetILProcessor();
        }
        var target = module.ImportReference(definition);
        var hasTarget = module.ImportReference(hasDefinition);
        foreach (string name in new[] { "Direct", "Virtual", "Pointer", "HasDirect", "HasPointer" })
        {
            bool has = name.StartsWith("Has", StringComparison.Ordinal);
            var called = has ? hasTarget : target;
            var il = Body(name);
            il.Emit(OpCodes.Ldarg_0);
            if (!has) { il.Emit(OpCodes.Ldarg_1); il.Emit(OpCodes.Ldarg_2); il.Emit(OpCodes.Ldarg_3); }
            if (name.EndsWith("Pointer", StringComparison.Ordinal))
            {
                il.Emit(OpCodes.Ldftn, called);
                var site = new CallSite(module.TypeSystem.Boolean);
                site.Parameters.Add(new ParameterDefinition(called.DeclaringType));
                foreach (var parameter in called.Parameters) site.Parameters.Add(new ParameterDefinition(parameter.ParameterType));
                il.Emit(OpCodes.Calli, site);
            }
            else il.Emit(name == "Virtual" ? OpCodes.Callvirt : OpCodes.Call, called);
            il.Emit(OpCodes.Ret);
        }
        var closed = program.NestedTypes.Single(type => type.Name == "ClosedQuery");
        var address = Body("Address");
        address.Emit(OpCodes.Ldarg_0);
        address.Emit(OpCodes.Ldftn, target);
        address.Emit(OpCodes.Newobj, closed.Methods.Single(method => method.IsConstructor));
        address.Emit(OpCodes.Ret);
        var producers = Body("ProducerRoute");
        foreach (string name in new[] { "Receiver", "YearArgument", "MonthArgument", "ParsedArgument" }) producers.Emit(OpCodes.Call, Method(name));
        producers.Emit(OpCodes.Call, target);
        producers.Emit(OpCodes.Ret);
        RuntimeTypeDefaultCtorFixture.MarkNoAlloc(module, Method("Direct"));
        if (negative)
        {
            bool pointer = args[3].StartsWith("address-", StringComparison.Ordinal);
            string mode = pointer ? args[3].Substring(8) : args[3];
            bool has = mode.StartsWith("has-", StringComparison.Ordinal);
            if (has)
            {
                mode = mode.Substring(4);
                if (mode is not ("return" or "static")) throw new InvalidOperationException("unknown HasYearMonthAdjustment negative");
            }
            var selected = has ? hasTarget : target;
            var bad = new MethodReference(selected.Name, selected.ReturnType, selected.DeclaringType) { HasThis = true };
            foreach (var parameter in selected.Parameters) bad.Parameters.Add(new ParameterDefinition(parameter.ParameterType));
            switch (mode)
            {
                case "return": bad.ReturnType = module.TypeSystem.Int32; break;
                case "year-value": bad.Parameters[0].ParameterType = module.TypeSystem.Int32; break;
                case "month-value": bad.Parameters[1].ParameterType = module.TypeSystem.Int32; break;
                case "year-element": bad.Parameters[0].ParameterType = new ByReferenceType(module.TypeSystem.Int64); break;
                case "bool-int": bad.Parameters[2].ParameterType = module.TypeSystem.Int32; break;
                case "modifier": bad.Parameters[0].ParameterType = new OptionalModifierType(module.TypeSystem.Object, bad.Parameters[0].ParameterType); break;
                case "arity": bad.Parameters.RemoveAt(2); break;
                case "static": bad.HasThis = false; break;
                case "generic": bad.GenericParameters.Add(new GenericParameter("T", bad)); break;
                default: throw new InvalidOperationException("unknown year/month adjustment negative");
            }
            string route = has ? (pointer ? "HasPointer" : "HasDirect") : (pointer ? "Pointer" : "Direct");
            Method(route).Body.Instructions.Single(instruction => instruction.OpCode == (pointer ? OpCodes.Ldftn : OpCodes.Call)).Operand = bad;
        }
        module.EntryPoint = Method(args[0] switch
        {
            "--year-month-adjustment-reflection" => "RunReflectionChecks",
            "--year-month-adjustment-minimal" => "RunMinimal",
            _ => "Run"
        });
        app.Write(path, new WriterParameters { Timestamp = 0, DeterministicMvid = true });
        return true;
    }
}

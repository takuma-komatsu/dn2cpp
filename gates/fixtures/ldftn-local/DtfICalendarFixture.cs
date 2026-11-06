using System.Globalization;
using Mono.Cecil;
using Mono.Cecil.Cil;

internal static class DtfICalendarFixture
{
    public static bool Run(string[] args)
    {
        bool negative = args.Length == 4 && args[0] == "--dtfi-calendar-negative";
        if (!negative && (args.Length != 3 || !new[] { "--dtfi-calendar", "--dtfi-calendar-direct",
            "--dtfi-calendar-reflection", "--dtfi-calendar-noalloc", "--dtfi-calendar-oracle" }.Contains(args[0])))
            return false;
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        using var core = AssemblyDefinition.ReadAssembly(args[2]);
        var definition = core.MainModule.GetType("System.Globalization.DateTimeFormatInfo").Methods.Single(m => m.Name == "get_Calendar");
        if ((int)definition.Attributes != 0x886 || definition.ImplAttributes != 0 || definition.RVA == 0
            || definition.HasGenericParameters || definition.Parameters.Count != 0
            || definition.ReturnType.FullName != "System.Globalization.Calendar" || definition.ReturnType.IsValueType)
            throw new InvalidOperationException("DTFI Calendar declaration changed");
        string path = Path.GetFullPath(args[1]);
        using var app = AssemblyDefinition.ReadAssembly(path, new ReaderParameters { InMemory = true });
        var module = app.MainModule;
        var program = module.GetType("DtfICalendarSubset.Program");
        MethodDefinition Method(string name) => program.Methods.Single(m => m.Name == name);
        ILProcessor Body(string name)
        {
            var method = Method(name);
            method.Body = new MethodBody(method);
            return method.Body.GetILProcessor();
        }
        if (args[0] != "--dtfi-calendar-oracle")
        {
            var target = module.ImportReference(definition);
            foreach (string name in new[] { "Direct", "Virtual", "Pointer" })
            {
                var il = Body(name);
                il.Emit(OpCodes.Ldarg_0);
                if (name == "Pointer")
                {
                    il.Emit(OpCodes.Ldftn, target);
                    var site = new CallSite(target.ReturnType);
                    site.Parameters.Add(new ParameterDefinition(target.DeclaringType));
                    il.Emit(OpCodes.Calli, site);
                }
                else il.Emit(name == "Virtual" ? OpCodes.Callvirt : OpCodes.Call, target);
                il.Emit(OpCodes.Ret);
            }
            var action = new GenericInstanceType(module.ImportReference(core.MainModule.GetType("System.Func`1")));
            action.GenericArguments.Add(target.ReturnType);
            var ctor = new MethodReference(".ctor", module.TypeSystem.Void, action) { HasThis = true };
            ctor.Parameters.Add(new ParameterDefinition(module.TypeSystem.Object));
            ctor.Parameters.Add(new ParameterDefinition(module.TypeSystem.IntPtr));
            var ai = Body("Address");
            ai.Emit(OpCodes.Ldarg_0);
            ai.Emit(OpCodes.Ldftn, target);
            ai.Emit(OpCodes.Newobj, ctor);
            ai.Emit(OpCodes.Ret);
            if (args[0] == "--dtfi-calendar-noalloc")
                RuntimeTypeDefaultCtorFixture.MarkNoAlloc(module, Method("Direct"));
            if (negative)
            {
                bool pointer = args[3].StartsWith("address-", StringComparison.Ordinal);
                string mode = pointer ? args[3].Substring(8) : args[3];
                var bad = new MethodReference(target.Name, target.ReturnType, target.DeclaringType) { HasThis = true };
                switch (mode)
                {
                    case "return": bad.ReturnType = module.TypeSystem.Object; break;
                    case "byref": bad.ReturnType = new ByReferenceType(target.ReturnType); break;
                    case "modifier": bad.ReturnType = new OptionalModifierType(module.TypeSystem.Object, target.ReturnType); break;
                    case "argument": bad.Parameters.Add(new ParameterDefinition(module.TypeSystem.Int32)); break;
                    case "static": bad.HasThis = false; break;
                    case "generic": bad.GenericParameters.Add(new GenericParameter("T", bad)); break;
                    default: throw new InvalidOperationException("unknown Calendar negative");
                }
                Method(pointer ? "Pointer" : "Direct").Body.Instructions.Single(i => i.OpCode == (pointer ? OpCodes.Ldftn : OpCodes.Call)).Operand = bad;
            }
        }
        module.EntryPoint = Method(args[0] switch
        {
            "--dtfi-calendar-oracle" => "RunPublicOracle",
            "--dtfi-calendar-reflection" => "RunReflectionChecks",
            "--dtfi-calendar-direct" or "--dtfi-calendar-noalloc" => "RunDirectChecks",
            _ => "Run"
        });
        app.Write(path, new WriterParameters { Timestamp = 0, DeterministicMvid = true });
        return true;
    }
}

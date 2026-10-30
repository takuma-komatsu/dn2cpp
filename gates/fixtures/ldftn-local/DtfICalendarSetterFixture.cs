using System.Globalization;
using Mono.Cecil;
using Mono.Cecil.Cil;

internal static class DtfICalendarSetterFixture
{
    public static bool Run(string[] args)
    {
        bool negative = args.Length == 4 && args[0] == "--dtfi-calendar-setter-negative";
        if (!negative && (args.Length != 3 || !new[] { "--dtfi-calendar-setter", "--dtfi-calendar-setter-direct",
            "--dtfi-calendar-setter-reflection", "--dtfi-calendar-setter-minimal", "--dtfi-calendar-setter-noalloc",
            "--dtfi-calendar-setter-oracle" }.Contains(args[0])))
            return false;
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        using var core = AssemblyDefinition.ReadAssembly(args[2]);
        var definition = core.MainModule.GetType("System.Globalization.DateTimeFormatInfo").Methods.Single(m => m.Name == "set_Calendar");
        if ((int)definition.Attributes != 0x886 || definition.ImplAttributes != 0 || definition.RVA == 0
            || definition.HasGenericParameters || definition.Parameters.Count != 1
            || definition.ReturnType.MetadataType != MetadataType.Void
            || definition.Parameters[0].ParameterType.FullName != "System.Globalization.Calendar"
            || definition.Parameters[0].ParameterType.IsValueType)
            throw new InvalidOperationException("DTFI Calendar setter declaration changed");
        string path = Path.GetFullPath(args[1]);
        using var app = AssemblyDefinition.ReadAssembly(path, new ReaderParameters { InMemory = true });
        var module = app.MainModule;
        var program = module.GetType("DtfICalendarSetterSubset.Program");
        MethodDefinition Method(string name) => program.Methods.Single(m => m.Name == name);
        ILProcessor Body(string name)
        {
            var method = Method(name);
            method.Body = new MethodBody(method);
            return method.Body.GetILProcessor();
        }
        if (args[0] != "--dtfi-calendar-setter-oracle")
        {
            var target = module.ImportReference(definition);
            foreach (string name in new[] { "Direct", "Virtual", "Pointer" })
            {
                var il = Body(name);
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldarg_1);
                if (name == "Pointer")
                {
                    il.Emit(OpCodes.Ldftn, target);
                    var site = new CallSite(target.ReturnType);
                    site.Parameters.Add(new ParameterDefinition(target.DeclaringType));
                    site.Parameters.Add(new ParameterDefinition(target.Parameters[0].ParameterType));
                    il.Emit(OpCodes.Calli, site);
                }
                else il.Emit(name == "Virtual" ? OpCodes.Callvirt : OpCodes.Call, target);
                il.Emit(OpCodes.Ret);
            }
            var action = new GenericInstanceType(module.ImportReference(core.MainModule.GetType("System.Action`1")));
            action.GenericArguments.Add(target.Parameters[0].ParameterType);
            var ctor = new MethodReference(".ctor", module.TypeSystem.Void, action) { HasThis = true };
            ctor.Parameters.Add(new ParameterDefinition(module.TypeSystem.Object));
            ctor.Parameters.Add(new ParameterDefinition(module.TypeSystem.IntPtr));
            var ai = Body("Address");
            ai.Emit(OpCodes.Ldarg_0);
            ai.Emit(OpCodes.Ldftn, target);
            ai.Emit(OpCodes.Newobj, ctor);
            ai.Emit(OpCodes.Ret);
            var pi = Body("ProducerRoute");
            pi.Emit(OpCodes.Call, Method("Receiver"));
            pi.Emit(OpCodes.Ldarg_0);
            pi.Emit(OpCodes.Call, Method("Value"));
            pi.Emit(OpCodes.Call, target);
            pi.Emit(OpCodes.Ret);
            RuntimeTypeDefaultCtorFixture.MarkNoAlloc(module, Method("Direct"));
            if (negative)
            {
                bool pointer = args[3].StartsWith("address-", StringComparison.Ordinal);
                string mode = pointer ? args[3].Substring(8) : args[3];
                var bad = new MethodReference(target.Name, target.ReturnType, target.DeclaringType) { HasThis = true };
                bad.Parameters.Add(new ParameterDefinition(target.Parameters[0].ParameterType));
                switch (mode)
                {
                    case "return": bad.ReturnType = module.TypeSystem.Object; break;
                    case "argument": bad.Parameters[0].ParameterType = module.TypeSystem.Object; break;
                    case "byref": bad.Parameters[0].ParameterType = new ByReferenceType(target.Parameters[0].ParameterType); break;
                    case "modifier": bad.Parameters[0].ParameterType = new OptionalModifierType(module.TypeSystem.Object, target.Parameters[0].ParameterType); break;
                    case "arity": bad.Parameters.Clear(); break;
                    case "static": bad.HasThis = false; break;
                    case "generic": bad.GenericParameters.Add(new GenericParameter("T", bad)); break;
                    default: throw new InvalidOperationException("unknown Calendar setter negative");
                }
                Method(pointer ? "Pointer" : "Direct").Body.Instructions.Single(i => i.OpCode == (pointer ? OpCodes.Ldftn : OpCodes.Call)).Operand = bad;
            }
        }
        module.EntryPoint = Method(args[0] switch
        {
            "--dtfi-calendar-setter-oracle" => "RunPublicOracle",
            "--dtfi-calendar-setter-reflection" => "RunReflectionChecks",
            "--dtfi-calendar-setter-minimal" => "RunMinimal",
            "--dtfi-calendar-setter-direct" or "--dtfi-calendar-setter-noalloc" => "RunDirectChecks",
            _ => "Run"
        });
        app.Write(path, new WriterParameters { Timestamp = 0, DeterministicMvid = true });
        return true;
    }
}

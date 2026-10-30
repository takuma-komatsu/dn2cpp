using System.Globalization;
using Mono.Cecil;
using Mono.Cecil.Cil;

internal static class AllDateTimePatternsFixture
{
    public static bool Run(string[] args)
    {
        bool negative = args.Length == 4 && args[0] == "--all-datetime-patterns-negative";
        if (!negative && (args.Length != 3 || !new[] { "--all-datetime-patterns", "--all-datetime-patterns-direct",
            "--all-datetime-patterns-reflection", "--all-datetime-patterns-minimal", "--all-datetime-patterns-noalloc",
            "--all-datetime-patterns-oracle", "--all-datetime-patterns-zeroarg" }.Contains(args[0])))
            return false;
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        using var core = AssemblyDefinition.ReadAssembly(args[2]);
        var definition = core.MainModule.GetType("System.Globalization.DateTimeFormatInfo").Methods.Single(m => m.Name == "GetAllDateTimePatterns" && m.Parameters.Count == 1);
        if ((int)definition.Attributes != 0x86 || definition.ImplAttributes != 0 || definition.RVA == 0
            || definition.HasGenericParameters || definition.Parameters.Count != 1
            || definition.ReturnType is not ArrayType { IsVector: true, ElementType.MetadataType: MetadataType.String }
            || definition.Parameters[0].ParameterType.MetadataType != MetadataType.Char)
            throw new InvalidOperationException("DTFI patterns declaration changed");
        string path = Path.GetFullPath(args[1]);
        using var app = AssemblyDefinition.ReadAssembly(path, new ReaderParameters { InMemory = true });
        var module = app.MainModule;
        var program = module.GetType("AllDateTimePatternsSubset.Program");
        MethodDefinition Method(string name) => program.Methods.Single(m => m.Name == name);
        ILProcessor Body(string name)
        {
            var method = Method(name);
            method.Body = new MethodBody(method);
            return method.Body.GetILProcessor();
        }
        if (args[0] != "--all-datetime-patterns-oracle")
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
            var action = new GenericInstanceType(module.ImportReference(core.MainModule.GetType("System.Func`2")));
            action.GenericArguments.Add(target.Parameters[0].ParameterType);
            action.GenericArguments.Add(target.ReturnType);
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
            pi.Emit(OpCodes.Call, Method("Format"));
            pi.Emit(OpCodes.Call, target);
            pi.Emit(OpCodes.Ret);
            if (args[0] == "--all-datetime-patterns-noalloc")
                RuntimeTypeDefaultCtorFixture.MarkNoAlloc(module, Method("Direct"));
            if (args[0] == "--all-datetime-patterns-zeroarg")
            {
                var sibling = module.ImportReference(core.MainModule.GetType("System.Globalization.DateTimeFormatInfo").Methods.Single(m => m.Name == target.Name && m.Parameters.Count == 0));
                var zi = Body("RunMinimal");
                var invariant = module.ImportReference(core.MainModule.GetType("System.Globalization.DateTimeFormatInfo").Methods.Single(m => m.Name == "get_InvariantInfo"));
                zi.Emit(OpCodes.Call, invariant);
                zi.Emit(OpCodes.Call, sibling);
                zi.Emit(OpCodes.Pop);
                zi.Emit(OpCodes.Ret);
            }
            if (negative)
            {
                bool pointer = args[3].StartsWith("address-", StringComparison.Ordinal);
                string mode = pointer ? args[3].Substring(8) : args[3];
                var bad = new MethodReference(target.Name, target.ReturnType, target.DeclaringType) { HasThis = true };
                bad.Parameters.Add(new ParameterDefinition(target.Parameters[0].ParameterType));
                switch (mode)
                {
                    case "return": bad.ReturnType = module.TypeSystem.Object; break;
                    case "argument": bad.Parameters[0].ParameterType = module.TypeSystem.Int32; break;
                    case "named-char": bad.Parameters[0].ParameterType = new TypeReference("System", "Char", module, target.DeclaringType.Scope, true); break;
                    case "named-string-element": bad.ReturnType = new ArrayType(new TypeReference("System", "String", module, target.DeclaringType.Scope, false)); break;
                    case "array-rank": bad.ReturnType = new ArrayType(module.TypeSystem.String, 2); break;
                    case "return-element": bad.ReturnType = new ArrayType(module.TypeSystem.Object); break;
                    case "byref": bad.Parameters[0].ParameterType = new ByReferenceType(target.Parameters[0].ParameterType); break;
                    case "modifier": bad.Parameters[0].ParameterType = new OptionalModifierType(module.TypeSystem.Object, target.Parameters[0].ParameterType); break;
                    case "arity": bad.Parameters.Add(new ParameterDefinition(module.TypeSystem.Char)); break;
                    case "static": bad.HasThis = false; break;
                    case "generic": bad.GenericParameters.Add(new GenericParameter("T", bad)); break;
                    default: throw new InvalidOperationException("unknown patterns negative");
                }
                Method(pointer ? "Pointer" : "Direct").Body.Instructions.Single(i => i.OpCode == (pointer ? OpCodes.Ldftn : OpCodes.Call)).Operand = bad;
            }
        }
        module.EntryPoint = Method(args[0] switch
        {
            "--all-datetime-patterns-oracle" => "RunPublicOracle",
            "--all-datetime-patterns-reflection" => "RunReflectionChecks",
            "--all-datetime-patterns-minimal" or "--all-datetime-patterns-zeroarg" => "RunMinimal",
            "--all-datetime-patterns-direct" => "RunDirectChecks",
            "--all-datetime-patterns-noalloc" => "RunNoAllocChecks",
            _ => "Run"
        });
        app.Write(path, new WriterParameters { Timestamp = 0, DeterministicMvid = true });
        return true;
    }
}

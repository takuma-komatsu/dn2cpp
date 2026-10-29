using System.Globalization;
using Mono.Cecil;
using Mono.Cecil.Cil;

internal static class InternalMonthFixture
{
    public static bool Run(string[] args)
    {
        bool negative = args.Length == 4 && args[0] == "--internal-month-negative";
        if (!negative && (args.Length != 3 || !new[] { "--internal-month", "--internal-month-direct",
            "--internal-month-reflection", "--internal-month-minimal", "--internal-month-noalloc", "--internal-month-oracle" }.Contains(args[0])))
            return false;
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        using var core = AssemblyDefinition.ReadAssembly(args[2]);
        var definition = core.MainModule.GetType("System.Globalization.DateTimeFormatInfo").Methods.Single(m => m.Name == "InternalGetMonthName");
        var style = core.MainModule.GetType("System.Globalization.MonthNameStyles");
        if ((int)definition.Attributes != 0x83 || definition.ImplAttributes != 0 || definition.RVA == 0
            || definition.HasGenericParameters || definition.Parameters.Count != 3
            || definition.ReturnType.MetadataType != MetadataType.String
            || definition.Parameters[0].ParameterType.MetadataType != MetadataType.Int32
            || definition.Parameters[1].ParameterType.FullName != style.FullName || !style.IsEnum
            || definition.Parameters[2].ParameterType.MetadataType != MetadataType.Boolean)
            throw new InvalidOperationException("internal month declaration changed");
        string path = Path.GetFullPath(args[1]);
        using var app = AssemblyDefinition.ReadAssembly(path, new ReaderParameters { InMemory = true });
        var module = app.MainModule;
        var program = module.GetType("InternalMonthSubset.Program");
        MethodDefinition Method(string name) => program.Methods.Single(m => m.Name == name);
        ILProcessor Body(string name)
        {
            var method = Method(name);
            method.Body = new MethodBody(method);
            return method.Body.GetILProcessor();
        }
        if (args[0] != "--internal-month-oracle")
        {
            var actualStyle = module.ImportReference(style);
            TypeReference Map(TypeReference type) => type.FullName == "InternalMonthSubset.Style" ? actualStyle : type;
            IEnumerable<TypeDefinition> Types(TypeDefinition type)
            {
                yield return type;
                foreach (var nested in type.NestedTypes)
                    foreach (var child in Types(nested)) yield return child;
            }
            // The placeholder enum is never a native target parameter: remap both delegate ABIs and boxes.
            foreach (var type in module.Types.SelectMany(Types))
            {
                foreach (var method in type.Methods)
                {
                    method.ReturnType = Map(method.ReturnType);
                    foreach (var parameter in method.Parameters) parameter.ParameterType = Map(parameter.ParameterType);
                    if (!method.HasBody) continue;
                    foreach (var variable in method.Body.Variables) variable.VariableType = Map(variable.VariableType);
                    foreach (var instruction in method.Body.Instructions)
                    {
                        if (instruction.Operand is TypeReference operand) instruction.Operand = Map(operand);
                        if (instruction.Operand is MethodReference called)
                        {
                            called.ReturnType = Map(called.ReturnType);
                            foreach (var parameter in called.Parameters) parameter.ParameterType = Map(parameter.ParameterType);
                        }
                    }
                }
            }
            var target = module.ImportReference(definition);
            foreach (string name in new[] { "Direct", "Pointer" })
            {
                var il = Body(name);
                il.Emit(OpCodes.Ldarg_0); il.Emit(OpCodes.Ldarg_1); il.Emit(OpCodes.Ldarg_2); il.Emit(OpCodes.Ldarg_3);
                if (name == "Pointer")
                {
                    il.Emit(OpCodes.Ldftn, target);
                    var site = new CallSite(module.TypeSystem.String);
                    site.Parameters.Add(new ParameterDefinition(target.DeclaringType));
                    foreach (var parameter in target.Parameters) site.Parameters.Add(new ParameterDefinition(parameter.ParameterType));
                    il.Emit(OpCodes.Calli, site);
                }
                else il.Emit(OpCodes.Call, target);
                il.Emit(OpCodes.Ret);
            }
            var closed = program.NestedTypes.Single(t => t.Name == "ClosedQuery");
            var invoke = closed.Methods.Single(m => m.Name == "Invoke");
            if (invoke.Parameters[1].ParameterType.FullName != style.FullName
                || Method("Direct").Parameters[2].ParameterType.FullName != style.FullName)
                throw new InvalidOperationException("native enum signature remap failed");
            var ctor = closed.Methods.Single(m => m.IsConstructor);
            var ai = Body("Address");
            ai.Emit(OpCodes.Ldarg_0); ai.Emit(OpCodes.Ldftn, target); ai.Emit(OpCodes.Newobj, ctor); ai.Emit(OpCodes.Ret);
            if (args[0] == "--internal-month-noalloc")
                RuntimeTypeDefaultCtorFixture.MarkNoAlloc(module, Method("Direct"));
            if (negative)
            {
                bool pointer = args[3].StartsWith("address-", StringComparison.Ordinal);
                string mode = pointer ? args[3].Substring(8) : args[3];
                var bad = new MethodReference(target.Name, target.ReturnType, target.DeclaringType) { HasThis = true };
                foreach (var parameter in target.Parameters) bad.Parameters.Add(new ParameterDefinition(parameter.ParameterType));
                switch (mode)
                {
                    case "return": bad.ReturnType = module.TypeSystem.Object; break;
                    case "style-int": bad.Parameters[1].ParameterType = module.TypeSystem.Int32; break;
                    case "style-enum": bad.Parameters[1].ParameterType = module.ImportReference(core.MainModule.GetType("System.DayOfWeek")); break;
                    case "bool-int": bad.Parameters[2].ParameterType = module.TypeSystem.Int32; break;
                    case "byref": bad.Parameters[1].ParameterType = new ByReferenceType(actualStyle); break;
                    case "modifier": bad.Parameters[1].ParameterType = new OptionalModifierType(module.TypeSystem.Object, actualStyle); break;
                    case "arity": bad.Parameters.RemoveAt(2); break;
                    case "arity-four": bad.Parameters.Add(new ParameterDefinition(module.TypeSystem.Int32)); break;
                    case "static": bad.HasThis = false; break;
                    case "generic": bad.GenericParameters.Add(new GenericParameter("T", bad)); break;
                    default: throw new InvalidOperationException("unknown internal month negative");
                }
                Method(pointer ? "Pointer" : "Direct").Body.Instructions.Single(i => i.OpCode == (pointer ? OpCodes.Ldftn : OpCodes.Call)).Operand = bad;
            }
        }
        module.EntryPoint = Method(args[0] switch
        {
            "--internal-month-oracle" => "RunPublicOracle",
            "--internal-month-reflection" => "RunReflectionChecks",
            "--internal-month-minimal" => "RunMinimal",
            "--internal-month-direct" or "--internal-month-noalloc" => "RunDirectChecks",
            _ => "Run"
        });
        app.Write(path, new WriterParameters { Timestamp = 0, DeterministicMvid = true });
        return true;
    }
}

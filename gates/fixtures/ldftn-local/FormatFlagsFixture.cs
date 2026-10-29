using System.Globalization;
using Mono.Cecil;
using Mono.Cecil.Cil;

internal static class FormatFlagsFixture
{
    public static bool Run(string[] args)
    {
        bool negative = args.Length == 4 && args[0] == "--format-flags-negative";
        if (!negative && (args.Length != 3 || !new[] { "--format-flags", "--format-flags-direct",
            "--format-flags-reflection", "--format-flags-minimal", "--format-flags-noalloc", "--format-flags-oracle" }.Contains(args[0])))
            return false;
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        using var core = AssemblyDefinition.ReadAssembly(args[2]);
        var definition = core.MainModule.GetType("System.Globalization.DateTimeFormatInfo").Methods.Single(m => m.Name == "get_FormatFlags");
        var flags = core.MainModule.GetType("System.Globalization.DateTimeFormatFlags");
        if ((int)definition.Attributes != 0x883 || definition.ImplAttributes != 0 || definition.RVA == 0
            || definition.HasGenericParameters || definition.Parameters.Count != 0
            || definition.ReturnType.FullName != flags.FullName || !flags.IsEnum
            || flags.Fields.Single(f => f.Name == "value__").FieldType.MetadataType != MetadataType.Int32)
            throw new InvalidOperationException("format flags declaration changed");
        string path = Path.GetFullPath(args[1]);
        using var app = AssemblyDefinition.ReadAssembly(path, new ReaderParameters { InMemory = true });
        var module = app.MainModule;
        var program = module.GetType("FormatFlagsSubset.Program");
        MethodDefinition Method(string name) => program.Methods.Single(m => m.Name == name);
        ILProcessor Body(string name)
        {
            var method = Method(name);
            method.Body = new MethodBody(method);
            return method.Body.GetILProcessor();
        }
        if (args[0] != "--format-flags-oracle")
        {
            var actualFlags = module.ImportReference(flags);
            TypeReference Map(TypeReference type) => type.FullName == "FormatFlagsSubset.Flag" ? actualFlags : type;
            IEnumerable<TypeDefinition> Types(TypeDefinition type)
            {
                yield return type;
                foreach (var nested in type.NestedTypes)
                    foreach (var child in Types(nested)) yield return child;
            }
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
                il.Emit(OpCodes.Ldarg_0);
                if (name == "Pointer")
                {
                    il.Emit(OpCodes.Ldftn, target);
                    var site = new CallSite(actualFlags);
                    site.Parameters.Add(new ParameterDefinition(target.DeclaringType));
                    il.Emit(OpCodes.Calli, site);
                }
                else il.Emit(OpCodes.Call, target);
                il.Emit(OpCodes.Ret);
            }
            var closed = program.NestedTypes.Single(t => t.Name == "ClosedQuery");
            if (closed.Methods.Single(m => m.Name == "Invoke").ReturnType.FullName != flags.FullName
                || Method("Direct").ReturnType.FullName != flags.FullName)
                throw new InvalidOperationException("native enum return remap failed");
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
                    case "class": bad.ReturnType = new TypeReference(flags.Namespace, flags.Name, module, actualFlags.Scope, false); break;
                    case "byref": bad.ReturnType = new ByReferenceType(actualFlags); break;
                    case "modifier": bad.ReturnType = new OptionalModifierType(module.TypeSystem.Object, actualFlags); break;
                    case "arity": bad.Parameters.Add(new ParameterDefinition(module.TypeSystem.Int32)); break;
                    case "static": bad.HasThis = false; break;
                    case "generic": bad.GenericParameters.Add(new GenericParameter("T", bad)); break;
                    default: throw new InvalidOperationException("unknown format flags negative");
                }
                Method(pointer ? "Pointer" : "Direct").Body.Instructions.Single(i => i.OpCode == (pointer ? OpCodes.Ldftn : OpCodes.Call)).Operand = bad;
            }
        }
        module.EntryPoint = Method(args[0] switch
        {
            "--format-flags-oracle" => "RunPublicOracle",
            "--format-flags-reflection" => "RunReflectionChecks",
            "--format-flags-minimal" => "RunMinimal",
            "--format-flags-direct" or "--format-flags-noalloc" => "RunDirectChecks",
            _ => "Run"
        });
        app.Write(path, new WriterParameters { Timestamp = 0, DeterministicMvid = true });
        return true;
    }
}

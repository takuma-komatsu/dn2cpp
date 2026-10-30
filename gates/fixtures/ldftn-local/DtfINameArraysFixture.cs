using System.Globalization;
using Mono.Cecil;
using Mono.Cecil.Cil;

internal static class DtfINameArraysFixture
{
    public static bool Run(string[] args)
    {
        bool negative = args.Length == 4 && args[0] == "--dtfi-name-arrays-negative";
        if (!negative && (args.Length != 3 || !new[] { "--dtfi-name-arrays", "--dtfi-name-arrays-reflection", "--dtfi-name-arrays-minimal", "--dtfi-name-arrays-oracle", "--dtfi-name-arrays-noalloc-genitive", "--dtfi-name-arrays-noalloc-leap", "--dtfi-name-arrays-setter-month", "--dtfi-name-arrays-setter-genitive" }.Contains(args[0]))) return false;
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        using var core = AssemblyDefinition.ReadAssembly(args[2]);
        var owner = core.MainModule.GetType("System.Globalization.DateTimeFormatInfo");
        string path = Path.GetFullPath(args[1]);
        using var app = AssemblyDefinition.ReadAssembly(path, new ReaderParameters { InMemory = true });
        var module = app.MainModule;
        var program = module.GetType("DtfINameArraysSubset.Program");
        MethodDefinition Method(string name) => program.Methods.Single(method => method.Name == name);
        ILProcessor Body(string name)
        {
            var method = Method(name);
            method.Body = new MethodBody(method);
            return method.Body.GetILProcessor();
        }
        int mutations = 0;
        if (args[0] != "--dtfi-name-arrays-oracle")
        {
            foreach (var row in new[] {
                (Suffix: "Genitive", Name: "InternalGetGenitiveMonthNames", Token: 0x0600342f, Boolean: true),
                (Suffix: "Leap", Name: "InternalGetLeapYearMonthNames", Token: 0x06003430, Boolean: false) })
            {
                var definition = owner.Methods.Single(method => method.Name == row.Name);
                if (definition.MetadataToken.ToInt32() != row.Token || (int)definition.Attributes != 0x83
                    || definition.ImplAttributes != 0 || definition.RVA == 0 || definition.HasGenericParameters
                    || definition.Parameters.Count != (row.Boolean ? 1 : 0)
                    || definition.ReturnType is not ArrayType { IsVector: true, ElementType.MetadataType: MetadataType.String }
                    || (row.Boolean && definition.Parameters[0].ParameterType.MetadataType != MetadataType.Boolean))
                    throw new InvalidOperationException("installed month-array declaration changed");
                var target = module.ImportReference(definition);
                foreach (string route in new[] { "Direct", "Virtual", "Pointer" })
                {
                    var il = Body(route + row.Suffix);
                    il.Emit(OpCodes.Ldarg_0);
                    if (row.Boolean) il.Emit(OpCodes.Ldarg_1);
                    if (route == "Pointer")
                    {
                        il.Emit(OpCodes.Ldftn, target);
                        var site = new CallSite(target.ReturnType);
                        site.Parameters.Add(new ParameterDefinition(target.DeclaringType));
                        if (row.Boolean) site.Parameters.Add(new ParameterDefinition(module.TypeSystem.Boolean));
                        il.Emit(OpCodes.Calli, site);
                    }
                    else il.Emit(route == "Virtual" ? OpCodes.Callvirt : OpCodes.Call, target);
                    il.Emit(OpCodes.Ret);
                }
                var address = Body("Address" + row.Suffix);
                address.Emit(OpCodes.Ldarg_0);
                address.Emit(OpCodes.Ldftn, target);
                address.Emit(OpCodes.Newobj, program.NestedTypes.Single(type => type.Name == "Closed" + row.Suffix).Methods.Single(method => method.IsConstructor));
                address.Emit(OpCodes.Ret);
                if (row.Boolean) Method("Producer").Body.Instructions.Single(instruction => instruction.OpCode == OpCodes.Call && instruction.Operand is MethodReference method && method.Name == "DirectGenitive").Operand = target;
                if (args[0] == "--dtfi-name-arrays-noalloc-" + row.Suffix.ToLowerInvariant()) RuntimeTypeDefaultCtorFixture.MarkNoAlloc(module, Method("Direct" + row.Suffix));
                if (negative)
                {
                    string mode = args[3];
                    bool pointer = mode.StartsWith("address-", StringComparison.Ordinal);
                    if (pointer) mode = mode.Substring(8);
                    string prefix = row.Suffix.ToLowerInvariant() + "-";
                    if (!mode.StartsWith(prefix, StringComparison.Ordinal)) continue;
                    mode = mode.Substring(prefix.Length);
                    mutations++;
                    var bad = new MethodReference(target.Name, target.ReturnType, target.DeclaringType) { HasThis = true };
                    foreach (var parameter in target.Parameters) bad.Parameters.Add(new ParameterDefinition(parameter.ParameterType));
                    switch (mode)
                    {
                        case "return": bad.ReturnType = new ArrayType(module.TypeSystem.Object); break;
                        case "named-string": bad.ReturnType = new ArrayType(new TypeReference("System", "String", module, target.DeclaringType.Scope)); break;
                        case "byref": bad.ReturnType = new ByReferenceType(target.ReturnType); break;
                        case "modifier": bad.ReturnType = new OptionalModifierType(module.TypeSystem.Object, target.ReturnType); break;
                        case "bool-int": if (!row.Boolean) throw new InvalidOperationException("bool-int requires genitive"); bad.Parameters[0].ParameterType = module.TypeSystem.Int32; break;
                        case "arity": bad.Parameters.Add(new ParameterDefinition(module.TypeSystem.Int32)); break;
                        case "static": bad.HasThis = false; break;
                        case "generic": bad.GenericParameters.Add(new GenericParameter("T", bad)); break;
                        default: throw new InvalidOperationException("unknown month-array negative");
                    }
                    Method((pointer ? "Pointer" : "Direct") + row.Suffix).Body.Instructions.Single(instruction => instruction.OpCode == (pointer ? OpCodes.Ldftn : OpCodes.Call)).Operand = bad;
                }
            }
            var scalar = module.ImportReference(owner.Methods.Single(method => method.Name == "InternalGetMonthName"));
            var scalarIl = Body("InternalMonth");
            for (int index = 0; index < 4; index++) scalarIl.Emit(OpCodes.Ldarg, Method("InternalMonth").Parameters[index]);
            scalarIl.Emit(OpCodes.Call, scalar);
            scalarIl.Emit(OpCodes.Ret);
        }
        if (negative && mutations != 1) throw new InvalidOperationException("negative must alter exactly one actual target");
        module.EntryPoint = Method(args[0] switch {
            "--dtfi-name-arrays-oracle" => "RunPublicOracle",
            "--dtfi-name-arrays-reflection" => "RunReflectionChecks",
            "--dtfi-name-arrays-minimal" => "RunMinimal",
            "--dtfi-name-arrays-setter-month" => "RunSetterMonth",
            "--dtfi-name-arrays-setter-genitive" => "RunSetterGenitive",
            _ => "Run" });
        app.Write(path, new WriterParameters { Timestamp = 0, DeterministicMvid = true });
        return true;
    }
}

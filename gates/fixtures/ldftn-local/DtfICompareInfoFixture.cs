using System.Globalization;
using Mono.Cecil;
using Mono.Cecil.Cil;

internal static class DtfICompareInfoFixture
{
    public static bool Run(string[] args)
    {
        bool negative = args.Length == 4 && args[0] == "--dtfi-compare-info-negative";
        if (!negative && (args.Length != 3 || !new[] { "--dtfi-compare-info", "--dtfi-compare-info-reflection", "--dtfi-compare-info-minimal", "--dtfi-compare-info-minimal-culture", "--dtfi-compare-info-oracle", "--dtfi-compare-info-noalloc-dtfi", "--dtfi-compare-info-noalloc-culture", "--dtfi-compare-info-subclass", "--dtfi-compare-info-subclass-virtual", "--dtfi-compare-info-subclass-address", "--dtfi-compare-info-subclass-reflection", "--dtfi-compare-info-subclass-base", "--dtfi-compare-info-subclass-loaded", "--dtfi-compare-info-subclass-lookup-only" }.Contains(args[0]))) return false;
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        using var core = AssemblyDefinition.ReadAssembly(args[2]);
        string path = Path.GetFullPath(args[1]);
        using var app = AssemblyDefinition.ReadAssembly(path, new ReaderParameters { InMemory = true });
        var module = app.MainModule;
        var program = module.GetType("DtfICompareInfoSubset.Program");
        MethodDefinition Method(string name) => program.Methods.Single(method => method.Name == name);
        ILProcessor Body(string name)
        {
            var method = Method(name);
            method.Body = new MethodBody(method);
            return method.Body.GetILProcessor();
        }
        int mutations = 0;
        if (args[0] != "--dtfi-compare-info-oracle")
        {
            foreach (var row in new[] {
                (Suffix: "DtfI", Type: "DateTimeFormatInfo", Token: 0x06003450, Attributes: 0x883),
                (Suffix: "Culture", Type: "CultureInfo", Token: 0x060033a9, Attributes: 0x9c6) })
            {
                var definition = core.MainModule.GetType("System.Globalization." + row.Type).Methods.Single(method => method.Name == "get_CompareInfo");
                if (definition.MetadataToken.ToInt32() != row.Token || (int)definition.Attributes != row.Attributes
                    || definition.ImplAttributes != 0 || definition.RVA == 0 || definition.HasGenericParameters || definition.Parameters.Count != 0
                    || definition.ReturnType.FullName != "System.Globalization.CompareInfo" || definition.ReturnType.IsValueType)
                    throw new InvalidOperationException("installed CompareInfo getter changed");
                var target = module.ImportReference(definition);
                foreach (string route in new[] { "Direct", "Virtual", "Pointer" })
                {
                    var il = Body(route + row.Suffix);
                    il.Emit(OpCodes.Ldarg_0);
                    if (route == "Pointer")
                    {
                        il.Emit(OpCodes.Ldftn, target);
                        var site = new CallSite(target.ReturnType);
                        site.Parameters.Add(new ParameterDefinition(target.DeclaringType));
                        il.Emit(OpCodes.Calli, site);
                    }
                    else il.Emit(route == "Virtual" ? OpCodes.Callvirt : OpCodes.Call, target);
                    il.Emit(OpCodes.Ret);
                }
                var address = Body("Address" + row.Suffix);
                address.Emit(OpCodes.Ldarg_0);
                address.Emit(OpCodes.Ldftn, target);
                address.Emit(OpCodes.Newobj, program.NestedTypes.Single(type => type.Name == "ClosedQuery").Methods.Single(method => method.IsConstructor));
                address.Emit(OpCodes.Ret);
                if (row.Suffix == "Culture")
                {
                    var virtualAddress = Body("VirtualAddressCulture");
                    virtualAddress.Emit(OpCodes.Ldarg_0);
                    virtualAddress.Emit(OpCodes.Dup);
                    virtualAddress.Emit(OpCodes.Ldvirtftn, target);
                    virtualAddress.Emit(OpCodes.Newobj, program.NestedTypes.Single(type => type.Name == "ClosedQuery").Methods.Single(method => method.IsConstructor));
                    virtualAddress.Emit(OpCodes.Ret);
                }
                Method("Producer" + row.Suffix).Body.Instructions.Single(instruction => instruction.OpCode == OpCodes.Call && instruction.Operand is MethodReference method && method.Name == "Direct" + row.Suffix).Operand = target;
                if (args[0] == "--dtfi-compare-info-noalloc-" + row.Suffix.ToLowerInvariant()) RuntimeTypeDefaultCtorFixture.MarkNoAlloc(module, Method("Direct" + row.Suffix));
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
                    switch (mode)
                    {
                        case "return": bad.ReturnType = module.TypeSystem.Object; break;
                        case "primitive": bad.ReturnType = module.TypeSystem.String; break;
                        case "byref": bad.ReturnType = new ByReferenceType(target.ReturnType); break;
                        case "modifier": bad.ReturnType = new OptionalModifierType(module.TypeSystem.Object, target.ReturnType); break;
                        case "static": bad.HasThis = false; break;
                        case "generic": bad.GenericParameters.Add(new GenericParameter("T", bad)); break;
                        case "arity": bad.Parameters.Add(new ParameterDefinition(module.TypeSystem.Int32)); break;
                        default: throw new InvalidOperationException("unknown CompareInfo negative");
                    }
                    Method((pointer ? "Pointer" : "Direct") + row.Suffix).Body.Instructions.Single(instruction => instruction.OpCode == (pointer ? OpCodes.Ldftn : OpCodes.Call)).Operand = bad;
                }
            }
            // Genuine managed subclass allocation, with no unsupported base constructor call.
            // Its override touches only a static counter and never depends on skipped instance initialization.
            var subclass = program.NestedTypes.Single(type => type.Name == "OverrideCulture");
            var make = Body("MakeOverride");
            make.Emit(OpCodes.Ldtoken, subclass);
            make.Emit(OpCodes.Call, module.ImportReference(typeof(Type).GetMethod(nameof(Type.GetTypeFromHandle), new[] { typeof(RuntimeTypeHandle) })!));
            make.Emit(OpCodes.Call, module.ImportReference(typeof(System.Runtime.CompilerServices.RuntimeHelpers).GetMethod(nameof(System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject), new[] { typeof(Type) })!));
            make.Emit(OpCodes.Castclass, subclass);
            make.Emit(OpCodes.Ret);
        }
        if (negative && mutations != 1) throw new InvalidOperationException("negative must alter exactly one target");
        module.EntryPoint = Method(args[0] switch {
            "--dtfi-compare-info-oracle" => "RunPublicOracle",
            "--dtfi-compare-info-reflection" => "RunReflectionChecks",
            "--dtfi-compare-info-minimal" => "RunMinimalDtfI",
            "--dtfi-compare-info-minimal-culture" => "RunMinimalCulture",
            "--dtfi-compare-info-subclass" => "RunOverridePolicy",
            "--dtfi-compare-info-subclass-virtual" => "RunSubclassVirtual",
            "--dtfi-compare-info-subclass-address" => "RunSubclassAddress",
            "--dtfi-compare-info-subclass-reflection" => "RunSubclassReflection",
            "--dtfi-compare-info-subclass-base" => "RunSubclassBase",
            "--dtfi-compare-info-subclass-loaded" => "RunSubclassLoaded",
            "--dtfi-compare-info-subclass-lookup-only" => "RunSubclassLookupOnly",
            _ => "Run" });
        app.Write(path, new WriterParameters { Timestamp = 0, DeterministicMvid = true });
        return true;
    }
}

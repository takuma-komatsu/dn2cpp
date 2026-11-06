using Mono.Cecil;
using Mono.Cecil.Cil;

internal static class InterpolationHelpersFixture
{
    public static bool Run(string[] args)
    {
        if (args.Length != 4 || args[0] != "--interpolation-helpers") return false;
        using var core = AssemblyDefinition.ReadAssembly(args[2]);
        using var app = AssemblyDefinition.ReadAssembly(args[1], new ReaderParameters { InMemory = true });
        var module = app.MainModule;
        var program = module.GetType("InterpolationHelpers.Program");
        var owner = core.MainModule.GetType("System.Runtime.CompilerServices.DefaultInterpolatedStringHandler");
        foreach (var row in new[] { (Name: "HasCustomFormatter", Suffix: "", Arity: 1, Delegate: "Query"), (Name: "GetDefaultLength", Suffix: "Length", Arity: 2, Delegate: "Length") })
        {
            var definition = owner.Methods.Single(m => m.Name == row.Name);
            if ((int)definition.Attributes != 0x93 || (int)definition.ImplAttributes != 0x100 || !definition.HasBody || definition.HasGenericParameters)
                throw new InvalidOperationException("installed helper shape changed");
            var target = module.ImportReference(definition);
            if (args[3].StartsWith("negative-", StringComparison.Ordinal))
            {
                var parts = args[3].Split('-');
                if ((parts[2] == "has") == (row.Name == "HasCustomFormatter"))
                {
                    var bad = new MethodReference(target.Name, target.ReturnType, target.DeclaringType);
                    foreach (var parameter in target.Parameters) bad.Parameters.Add(new ParameterDefinition(parameter.ParameterType));
                    switch (parts[3])
                    {
                        case "spec":
                            var spec = new GenericInstanceMethod(target); spec.GenericArguments.Add(module.TypeSystem.Int32); bad = spec; break;
                        case "instance": bad.HasThis = true; break;
                        case "generic": bad.GenericParameters.Add(new GenericParameter("T", bad)); break;
                        case "return": bad.ReturnType = module.TypeSystem.UInt32; break;
                        case "modifier": bad.ReturnType = new OptionalModifierType(module.TypeSystem.Object, bad.ReturnType); break;
                        case "byref": bad.Parameters[0].ParameterType = new ByReferenceType(bad.Parameters[0].ParameterType); break;
                        case "parammodifier": bad.Parameters[0].ParameterType = new RequiredModifierType(module.TypeSystem.Object, bad.Parameters[0].ParameterType); break;
                        case "arity": bad.Parameters.Add(new ParameterDefinition(module.TypeSystem.Int32)); break;
                        case "varargs": bad.CallingConvention = MethodCallingConvention.VarArg; break;
                        case "wrongtype": bad.Parameters[0].ParameterType = module.TypeSystem.Object; break;
                        default: throw new InvalidOperationException("negative shape");
                    }
                    target = bad;
                }
            }
            var direct = program.Methods.Single(m => m.Name == "Direct" + row.Suffix);
            direct.Body = new MethodBody(direct);
            var il = direct.Body.GetILProcessor();
            il.Emit(OpCodes.Ldarg_0);
            if (row.Arity == 2) il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Call, target); il.Emit(OpCodes.Ret);
            var address = program.Methods.Single(m => m.Name == "Address" + row.Suffix);
            address.Body = new MethodBody(address); il = address.Body.GetILProcessor();
            il.Emit(OpCodes.Ldnull); il.Emit(OpCodes.Ldftn, target);
            il.Emit(OpCodes.Newobj, program.NestedTypes.Single(t => t.Name == row.Delegate).Methods.Single(m => m.IsConstructor)); il.Emit(OpCodes.Ret);
        }
        module.EntryPoint = program.Methods.Single(m => m.Name == (args[3].StartsWith("negative-", StringComparison.Ordinal) ? (args[3].StartsWith("negative-address-", StringComparison.Ordinal) ? "RunAddress" : "Main") : args[3] switch { "literal" => "RunLiteral", "text" => "RunText", "invoke" => "RunInvoke", "retained" => "RunRetained", "public" => "RunPublic", "all" => "RunAll", "derived" => "RunDerived", "direct" => "Main", "address" => "RunAddress", "reflection" => "RunReflection", _ => throw new InvalidOperationException("route") }));
        app.Write(args[1], new WriterParameters { Timestamp = 0, DeterministicMvid = true });
        return true;
    }
}

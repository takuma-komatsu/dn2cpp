using System.Globalization;
using Mono.Cecil;
using Mono.Cecil.Cil;

internal static class ResourceFormatterFixture
{
    public static bool Run(string[] args)
    {
        if (args.Length != 4 || args[0] != "--resource-formatter") return false;
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        using var core = AssemblyDefinition.ReadAssembly(args[2]);
        using var app = AssemblyDefinition.ReadAssembly(args[1], new ReaderParameters { InMemory = true });
        var module = app.MainModule;
        var program = module.GetType("ResourceFormatter.Program");
        var owner = core.MainModule.GetType("System.Resources.ResourceReader");
        foreach (string suffix in new[] { "Create", "Decode" })
        {
            var definition = owner.Methods.Single(m => m.Name == (suffix == "Create" ? "CreateBinaryFormatter" : "Deserialize"));
            if (core.Name.Name != "System.Private.CoreLib" || (int)definition.Attributes != 0x91
                || definition.ImplAttributes != 0 || definition.RVA != 0 || definition.HasGenericParameters)
                throw new InvalidOperationException("installed resource formatter declaration changed");
            MethodReference target = module.ImportReference(definition);
            bool negative = args[3].StartsWith("negative-", StringComparison.Ordinal);
            if (negative && suffix == "Decode")
            {
                var bad = new MethodReference(target.Name, target.ReturnType, target.DeclaringType);
                foreach (var p in target.Parameters) bad.Parameters.Add(new ParameterDefinition(p.ParameterType));
                switch (args[3].Split('-')[2])
                {
                    case "spec": var spec = new GenericInstanceMethod(target); spec.GenericArguments.Add(module.TypeSystem.Int32); bad = spec; break;
                    case "instance": bad.HasThis = true; break;
                    case "generic": bad.GenericParameters.Add(new GenericParameter("T", bad)); break;
                    case "return": bad.ReturnType = module.TypeSystem.String; break;
                    case "modifier": bad.ReturnType = new OptionalModifierType(module.TypeSystem.Object, bad.ReturnType); break;
                    case "byref": bad.Parameters[0].ParameterType = new ByReferenceType(bad.Parameters[0].ParameterType); break;
                    case "streambyref": bad.Parameters[1].ParameterType = new ByReferenceType(bad.Parameters[1].ParameterType); break;
                    case "parammodifier": bad.Parameters[1].ParameterType = new RequiredModifierType(module.TypeSystem.Object, bad.Parameters[1].ParameterType); break;
                    case "arity": bad.Parameters.Add(new ParameterDefinition(module.TypeSystem.Object)); break;
                    case "varargs": bad.CallingConvention = MethodCallingConvention.VarArg; break;
                    case "wrongtype": bad.Parameters[1].ParameterType = module.TypeSystem.Object; break;
                    default: throw new InvalidOperationException("negative shape");
                }
                target = bad;
            }
            foreach (string prefix in new[] { "Direct", "Pointer", "Address" })
            {
                var method = program.Methods.Single(m => m.Name == prefix + suffix);
                method.Body = new MethodBody(method);
                var il = method.Body.GetILProcessor();
                if (prefix == "Address")
                {
                    il.Emit(OpCodes.Ldnull); il.Emit(OpCodes.Ldftn, target);
                    il.Emit(OpCodes.Newobj, program.NestedTypes.Single(t => t.Name == suffix).Methods.Single(m => m.IsConstructor));
                }
                else
                {
                    foreach (var parameter in method.Parameters) il.Emit(OpCodes.Ldarg, parameter);
                    if (prefix == "Pointer")
                    {
                        il.Emit(OpCodes.Ldftn, target);
                        var site = new CallSite(target.ReturnType);
                        foreach (var parameter in target.Parameters) site.Parameters.Add(new ParameterDefinition(parameter.ParameterType));
                        il.Emit(OpCodes.Calli, site);
                    }
                    else il.Emit(OpCodes.Call, target);
                }
                il.Emit(OpCodes.Ret);
            }
        }
        module.EntryPoint = program.Methods.Single(m => m.Name == ((args[3].StartsWith("negative-", StringComparison.Ordinal) ? (args[3].StartsWith("negative-address-", StringComparison.Ordinal) ? "address" : "direct") : args[3]) switch
        { "direct" => "Main", "pointer" => "RunPointer", "address" => "RunAddress", "reflection" => "RunReflection", "all" => "RunAll", _ => throw new InvalidOperationException("route") }));
        app.Write(args[1], new WriterParameters { Timestamp = 0, DeterministicMvid = true });
        return true;
    }
}

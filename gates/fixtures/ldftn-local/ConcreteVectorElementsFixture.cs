using System.Globalization;
using Mono.Cecil;
using Mono.Cecil.Cil;

internal static class ConcreteVectorElementsFixture
{
    public static bool Run(string[] args)
    {
        if (args.Length != 4 || args[0] != "--concrete-vector-elements-fixture") return false;
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        // Mode is get2/with2/get3/with3/get4/with4:direct/address/calli:mutation.
        string[] mode = args[3] == "valid" ? new[] { "get2", "direct", "valid" } : args[3].Split(':');
        if (mode.Length != 3 || mode[0] is not ("get2" or "with2" or "get3" or "with3" or "get4" or "with4")
            || mode[1] is not ("direct" or "address" or "calli"))
            throw new ArgumentException("mode must be get2/with2/get3/with3/get4/with4:direct/address/calli:mutation");
        string output = Path.GetFullPath(args[1]);
        if (File.Exists(output)) throw new InvalidOperationException("fixture output must be new");
        using var core = AssemblyDefinition.ReadAssembly(args[2]);
        using var assembly = AssemblyDefinition.CreateAssembly(new AssemblyNameDefinition("ConcreteVectorElements", new Version(1, 0)), "ConcreteVectorElements", ModuleKind.Console);
        var module = assembly.MainModule;
        bool with = mode[0].StartsWith("with", StringComparison.Ordinal);
        string width = mode[0][^1].ToString();
        string name = with ? "WithElement" : "GetElement";
        var definition = core.MainModule.GetType("System.Numerics.Vector").Methods.Single(m => m.Name == name
            && !m.HasGenericParameters && m.Parameters[0].ParameterType.FullName == "System.Numerics.Vector" + width);
        var target = module.ImportReference(definition);
        var vector = module.ImportReference(definition.Parameters[0].ParameterType);
        var host = new TypeDefinition("Fixture", "Program", TypeAttributes.Public | TypeAttributes.Abstract | TypeAttributes.Sealed, module.TypeSystem.Object);
        module.Types.Add(host);
        var entry = new MethodDefinition("Main", MethodAttributes.Public | MethodAttributes.Static, module.TypeSystem.Void);
        host.Methods.Add(entry); assembly.EntryPoint = entry;
        var site = new CallSite(module.ImportReference(definition.ReturnType));
        foreach (var parameter in definition.Parameters) site.Parameters.Add(new ParameterDefinition(module.ImportReference(parameter.ParameterType)));
        switch (mode[2])
        {
            case "valid": break;
            case "instance": target.HasThis = true; break;
            case "vararg": target.CallingConvention = MethodCallingConvention.VarArg; break;
            case "generic":
                target.GenericParameters.Add(new GenericParameter("T", target));
                target.CallingConvention = MethodCallingConvention.Generic;
                break;
            case "byref-source": target.Parameters[0].ParameterType = new ByReferenceType(target.Parameters[0].ParameterType); break;
            case "byref-result": target.ReturnType = new ByReferenceType(target.ReturnType); break;
            case "wrong-source": target.Parameters[0].ParameterType = module.ImportReference(core.MainModule.GetType("System.Numerics.Quaternion")); break;
            case "wrong-result": target.ReturnType = with ? module.ImportReference(core.MainModule.GetType("System.Numerics.Vector" + (width == "2" ? "3" : "2"))) : module.TypeSystem.Double; break;
            case "wrong-index": target.Parameters[1].ParameterType = module.TypeSystem.UInt32; break;
            case "wrong-value":
                if (!with) throw new ArgumentException("wrong-value requires WithElement");
                target.Parameters[2].ParameterType = module.TypeSystem.Double;
                break;
            case "extra": target.Parameters.Add(new ParameterDefinition(module.TypeSystem.Int32)); break;
            case "modopt-source": target.Parameters[0].ParameterType = new OptionalModifierType(module.TypeSystem.Object, target.Parameters[0].ParameterType); break;
            case "modopt-result": target.ReturnType = new OptionalModifierType(module.TypeSystem.Object, target.ReturnType); break;
            case "modreq-source": target.Parameters[0].ParameterType = new RequiredModifierType(module.TypeSystem.Object, target.Parameters[0].ParameterType); break;
            case "modopt-owner": target.DeclaringType = new OptionalModifierType(module.TypeSystem.Object, target.DeclaringType); break;
            case "byref-owner": target.DeclaringType = new ByReferenceType(target.DeclaringType); break;
            case "generic-owner":
                var owner = new GenericInstanceType(target.DeclaringType);
                owner.GenericArguments.Add(module.TypeSystem.Single);
                target.DeclaringType = owner;
                break;
            case "foreign-owner":
                var foreign = new AssemblyNameReference("Foreign.Vector.Owner", new Version(1, 0));
                module.AssemblyReferences.Add(foreign);
                target.DeclaringType = new TypeReference("System.Numerics", "Vector", module, foreign);
                break;
            default: throw new ArgumentException("unknown mutation: " + mode[2]);
        }
        var il = entry.Body.GetILProcessor();
        if (mode[1] != "address")
        {
            entry.Body.InitLocals = true;
            var local = new VariableDefinition(vector);
            entry.Body.Variables.Add(local);
            il.Emit(OpCodes.Ldloca, local); il.Emit(OpCodes.Initobj, vector);
            if (target.HasThis && mode[1] == "direct") il.Emit(OpCodes.Ldnull);
            il.Emit(OpCodes.Ldloc, local);
            il.Emit(OpCodes.Ldc_I4_0);
            if (with) il.Emit(OpCodes.Ldc_R4, -0.0f);
            if (mode[2] == "extra" && mode[1] == "direct") il.Emit(OpCodes.Ldc_I4_0);
        }
        if (mode[1] == "direct") il.Emit(OpCodes.Call, target);
        else
        {
            il.Emit(OpCodes.Ldftn, target);
            if (mode[1] == "calli") il.Emit(OpCodes.Calli, site);
        }
        il.Emit(OpCodes.Pop); il.Emit(OpCodes.Ret);
        assembly.Write(output, new WriterParameters { Timestamp = 0, DeterministicMvid = true });
        File.WriteAllText(Path.ChangeExtension(output, ".runtimeconfig.json"), "{\"runtimeOptions\":{\"tfm\":\"net10.0\",\"framework\":{\"name\":\"Microsoft.NETCore.App\",\"version\":\"10.0.0\"}}}\n");
        Console.WriteLine("concrete-vector-elements-fixture:" + args[3]);
        return true;
    }
}

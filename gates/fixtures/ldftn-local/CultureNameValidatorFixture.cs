using Mono.Cecil;
using Mono.Cecil.Cil;

internal static class CultureNameValidatorFixture
{
    internal static int Rewrite(AssemblyDefinition core, ModuleDefinition module, TypeDefinition program, string? negative)
    {
        var definition = core.MainModule.GetType("System.Globalization.CultureInfo").Methods.Single(method =>
            method.Name == "VerifyCultureName" && method.Parameters.Count == 2
                && method.Parameters[0].ParameterType.MetadataType == MetadataType.String);
        if (!DateTimeFixtureDeclaration.IsCoreLibMethod(core, definition, "System.Globalization.CultureInfo", "VerifyCultureName", false, false)
            || (int)definition.Attributes != 0x93 || definition.ImplAttributes != 0 || !definition.HasBody || definition.RVA == 0
            || definition.HasGenericParameters || definition.ReturnType.MetadataType != MetadataType.Boolean
            || definition.Parameters[1].ParameterType.MetadataType != MetadataType.Boolean
            || definition.Body.HasExceptionHandlers)
            throw new InvalidOperationException("Culture name validator declaration changed");
        int mutations = 0;
        foreach (string route in new[] { "Direct", "Pointer" })
        {
            MethodReference target = module.ImportReference(definition);
            bool address = negative?.StartsWith("address-", StringComparison.Ordinal) == true;
            string? mode = address ? negative!.Substring(8) : negative;
            if (mode?.StartsWith("validator-", StringComparison.Ordinal) == true && route == (address ? "Pointer" : "Direct"))
            {
                var bad = new MethodReference(target.Name, target.ReturnType, target.DeclaringType);
                foreach (var parameter in target.Parameters) bad.Parameters.Add(new ParameterDefinition(parameter.ParameterType));
                switch (mode.Substring(10))
                {
                    case "return": bad.ReturnType = module.TypeSystem.Object; break;
                    case "byref": bad.Parameters[0].ParameterType = new ByReferenceType(module.TypeSystem.String); break;
                    default: throw new InvalidOperationException("Unknown culture validator negative");
                }
                target = bad; mutations++;
            }
            var stub = program.Methods.Single(method => method.Name == route + "ValidateCultureName");
            stub.Body = new MethodBody(stub);
            var il = stub.Body.GetILProcessor();
            il.Emit(OpCodes.Ldarg_0); il.Emit(OpCodes.Ldarg_1);
            if (route == "Pointer")
            {
                il.Emit(OpCodes.Ldftn, target);
                var site = new CallSite(module.TypeSystem.Boolean);
                site.Parameters.Add(new ParameterDefinition(module.TypeSystem.String));
                site.Parameters.Add(new ParameterDefinition(module.TypeSystem.Boolean));
                il.Emit(OpCodes.Calli, site);
            }
            else il.Emit(OpCodes.Call, target);
            il.Emit(OpCodes.Ret);
        }
        Console.WriteLine("culture-name-validator=genuine-CoreLib-native-only; direct,calli,reflection");
        return mutations;
    }
}

using System.Globalization;
using Mono.Cecil;
using Mono.Cecil.Cil;

internal static class DefaultPrincipalFixture
{
    public static bool Run(string[] args)
    {
        bool negative = args.Length == 4 && args[0] == "--default-principal-negative";
        if (!negative && (args.Length != 3 || !new[] { "--default-principal", "--default-principal-reflection", "--default-principal-policy", "--default-principal-noalloc" }.Contains(args[0]))) return false;
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        using var core = AssemblyDefinition.ReadAssembly(args[2]);
        var owner = core.MainModule.GetType("System.AppDomain");
        var target = owner.Methods.Single(m => m.Name == "<GetThreadPrincipal>g__GetDefaultPrincipal|107_0");
        const string identity = "System.Security.Principal.GenericPrincipal, System.Security.Claims";
        bool TargetType(Mono.Cecil.ICustomAttributeProvider provider) => provider.CustomAttributes.Count(a => a.AttributeType.FullName == "System.Runtime.CompilerServices.UnsafeAccessorTypeAttribute"
            && a.ConstructorArguments.Count == 1 && Equals(a.ConstructorArguments[0].Value, identity)) == 1;
        var accessor = target.CustomAttributes.Single(a => a.AttributeType.FullName == "System.Runtime.CompilerServices.UnsafeAccessorAttribute");
        if ((int)target.Attributes != 0x93 || (int)target.ImplAttributes != 0 || target.RVA != 0 || target.HasGenericParameters
            || target.Parameters.Count != 1 || target.Parameters[0].ParameterType.MetadataType != MetadataType.Object || target.ReturnType.MetadataType != MetadataType.Object
            || Convert.ToInt32(accessor.ConstructorArguments.Single().Value) != 2
            || accessor.Properties.Single(p => p.Name == "Name").Argument.Value as string != "GetDefaultInstance"
            || !TargetType(target.MethodReturnType) || !TargetType(target.Parameters[0])
            || !target.CustomAttributes.Any(a => a.AttributeType.FullName == "System.Runtime.CompilerServices.CompilerGeneratedAttribute"))
            throw new InvalidOperationException("exact installed default-principal accessor changed");
        string path = Path.GetFullPath(args[1]);
        using var app = AssemblyDefinition.ReadAssembly(path, new ReaderParameters { InMemory = true });
        var module = app.MainModule;
        if (module.AssemblyReferences.Any(a => a.Name == "System.Security.Claims"))
            throw new InvalidOperationException("implicit dependency fixture must not reference Claims");
        var program = module.GetType("DefaultPrincipalSubset.Program");
        MethodDefinition Method(string name) => program.Methods.Single(m => m.Name == name);
        var imported = module.ImportReference(target);
        foreach (string name in new[] { "Direct", "Pointer" })
        {
            var method = Method(name); method.Body = new MethodBody(method); var il = method.Body.GetILProcessor();
            il.Emit(OpCodes.Ldarg_0);
            if (name == "Pointer")
            {
                il.Emit(OpCodes.Ldftn, imported);
                var site = new CallSite(module.TypeSystem.Object); site.Parameters.Add(new ParameterDefinition(module.TypeSystem.Object));
                il.Emit(OpCodes.Calli, site);
            }
            else il.Emit(OpCodes.Call, imported);
            il.Emit(OpCodes.Ret);
        }
        var address = Method("Address");
        var oldCtor = (MethodReference)address.Body.Instructions.Single(i => i.OpCode == OpCodes.Newobj).Operand;
        address.Body = new MethodBody(address); var ai = address.Body.GetILProcessor();
        ai.Emit(OpCodes.Ldnull); ai.Emit(OpCodes.Ldftn, imported); ai.Emit(OpCodes.Newobj, oldCtor); ai.Emit(OpCodes.Ret);
        var getter = Method("GetPrincipal"); getter.Body = new MethodBody(getter); var gi = getter.Body.GetILProcessor();
        gi.Emit(OpCodes.Ldarg_0); gi.Emit(OpCodes.Callvirt, module.ImportReference(owner.Methods.Single(m => m.Name == "GetThreadPrincipal"))); gi.Emit(OpCodes.Ret);
        if (negative)
        {
            string mode = args[3]; bool addressMode = mode.StartsWith("address-", StringComparison.Ordinal);
            if (addressMode) mode = mode.Substring(8);
            var malformed = new MethodReference(imported.Name, imported.ReturnType, imported.DeclaringType) { HasThis = false };
            malformed.Parameters.Add(new ParameterDefinition(module.TypeSystem.Object));
            switch (mode)
            {
                case "return": malformed.ReturnType = module.TypeSystem.String; break;
                case "parameter": malformed.Parameters[0].ParameterType = module.TypeSystem.String; break;
                case "byref": malformed.Parameters[0].ParameterType = new ByReferenceType(module.TypeSystem.Object); break;
                case "modifier": malformed.ReturnType = new OptionalModifierType(module.TypeSystem.Object, module.TypeSystem.Object); break;
                case "instance": malformed.HasThis = true; break;
                case "generic": malformed.GenericParameters.Add(new GenericParameter("T", malformed)); break;
                default: throw new ArgumentException("unknown malformed accessor mode");
            }
            Method(addressMode ? "Address" : "Direct").Body.Instructions.Single(i => i.OpCode == (addressMode ? OpCodes.Ldftn : OpCodes.Call)).Operand = malformed;
        }
        if (args[0] == "--default-principal-noalloc") RuntimeTypeDefaultCtorFixture.MarkNoAlloc(module, Method("Direct"));
        module.EntryPoint = Method(args[0] switch { "--default-principal-reflection" => "RunReflectionChecks", "--default-principal-policy" => "RunPolicyChecks", _ => "RunArtifactChecks" });
        app.Write(path, new WriterParameters { Timestamp = 0, DeterministicMvid = true });
        Console.WriteLine("default-principal-fixture:actual-CoreLib-accessor;Claims-reference-absent");
        return true;
    }
}

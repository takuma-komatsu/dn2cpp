using System.Globalization;
using Mono.Cecil;
using Mono.Cecil.Cil;

internal static class DateOnlyDeconstructFixture
{
    public static bool Run(string[] args)
    {
        bool negative = args.Length == 4 && args[0] == "--dateonly-deconstruct-negative";
        if (!negative && (args.Length != 3 || args[0] is not ("--dateonly-deconstruct" or "--dateonly-deconstruct-reflection" or "--dateonly-deconstruct-oracle" or "--dateonly-deconstruct-reflection-oracle"))) return false;
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        using var core = AssemblyDefinition.ReadAssembly(args[2]);
        var owner = core.MainModule.GetType("System.DateOnly");
        var definition = owner.Methods.Single(method => method.Name == "Deconstruct");
        if ((int)definition.Attributes != 0x86 || definition.ImplAttributes != 0 || definition.RVA == 0
            || definition.HasGenericParameters || definition.ReturnType.MetadataType != MetadataType.Void
            || definition.Parameters.Count != 3 || definition.Parameters.Any(parameter => parameter.ParameterType is not ByReferenceType { ElementType.MetadataType: MetadataType.Int32 }))
            throw new InvalidOperationException("DateOnly.Deconstruct installed declaration changed");
        string path = Path.GetFullPath(args[1]);
        using var app = AssemblyDefinition.ReadAssembly(path, new ReaderParameters { InMemory = true });
        var module = app.MainModule;
        var program = module.GetType("DateOnlyDeconstructSubset.Program");
        MethodDefinition Method(string name) => program.Methods.Single(method => method.Name == name);
        if (args[0] != "--dateonly-deconstruct-oracle" && args[0] != "--dateonly-deconstruct-reflection-oracle")
        {
            var target = module.ImportReference(definition);
            foreach (string name in new[] { "Direct", "Pointer" })
            {
                var method = Method(name);
                method.Body = new MethodBody(method);
                var il = method.Body.GetILProcessor();
                il.Emit(OpCodes.Ldarg_0); il.Emit(OpCodes.Ldarg_1); il.Emit(OpCodes.Ldarg_2); il.Emit(OpCodes.Ldarg_3);
                if (name == "Pointer")
                {
                    il.Emit(OpCodes.Ldftn, target);
                    var site = new CallSite(module.TypeSystem.Void);
                    site.Parameters.Add(new ParameterDefinition(new ByReferenceType(target.DeclaringType)));
                    foreach (var parameter in target.Parameters) site.Parameters.Add(new ParameterDefinition(parameter.ParameterType));
                    il.Emit(OpCodes.Calli, site);
                }
                else il.Emit(OpCodes.Call, target);
                il.Emit(OpCodes.Ret);
            }
            var address = Method("Address");
            address.Body = new MethodBody(address);
            var ai = address.Body.GetILProcessor();
            ai.Emit(OpCodes.Ldarg_0);
            ai.Emit(OpCodes.Box, target.DeclaringType);
            ai.Emit(OpCodes.Ldftn, target);
            ai.Emit(OpCodes.Newobj, program.NestedTypes.Single(type => type.Name == "Closed").Methods.Single(method => method.IsConstructor));
            ai.Emit(OpCodes.Ret);
            if (negative)
            {
                bool pointer = args[3].StartsWith("address-", StringComparison.Ordinal);
                string mode = pointer ? args[3].Substring(8) : args[3];
                var bad = new MethodReference(target.Name, target.ReturnType, target.DeclaringType) { HasThis = true };
                foreach (var parameter in target.Parameters) bad.Parameters.Add(new ParameterDefinition(parameter.ParameterType));
                switch (mode)
                {
                    case "return": bad.ReturnType = module.TypeSystem.Int32; break;
                    case "value": bad.Parameters[0].ParameterType = module.TypeSystem.Int32; break;
                    case "element": bad.Parameters[1].ParameterType = new ByReferenceType(module.TypeSystem.Int64); break;
                    case "modifier": bad.Parameters[2].ParameterType = new OptionalModifierType(module.TypeSystem.Object, bad.Parameters[2].ParameterType); break;
                    case "static": bad.HasThis = false; break;
                    case "generic": bad.GenericParameters.Add(new GenericParameter("T", bad)); break;
                    case "arity": bad.Parameters.RemoveAt(2); break;
                    default: throw new InvalidOperationException("unknown Deconstruct negative");
                }
                Method(pointer ? "Pointer" : "Direct").Body.Instructions.Single(instruction => instruction.OpCode == (pointer ? OpCodes.Ldftn : OpCodes.Call)).Operand = bad;
            }
        }
        module.EntryPoint = Method(args[0] is "--dateonly-deconstruct-reflection" or "--dateonly-deconstruct-reflection-oracle" ? "RunReflectionChecks" : "Run");
        app.Write(path, new WriterParameters { Timestamp = 0, DeterministicMvid = true });
        return true;
    }
}

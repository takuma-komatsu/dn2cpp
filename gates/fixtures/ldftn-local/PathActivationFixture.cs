using System.Globalization;
using Mono.Cecil;
using Mono.Cecil.Cil;

internal static class PathActivationFixture
{
    public static bool Run(string[] args)
    {
        bool negative = args.Length == 4 && args[0] == "--path-activation-negative";
        if (!negative && (args.Length != 3 || !new[]
            { "--path-activation", "--path-activation-reflection", "--path-activation-noalloc", "--path-activation-oracle", "--path-activation-smoke-oracle" }.Contains(args[0])))
            return false;
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        using var core = AssemblyDefinition.ReadAssembly(args[2]);
        var definition = core.MainModule.GetType("System.Activator").Methods.Single(m => m.Name == "CreateInstanceFrom" && m.IsStatic && m.Parameters.Count == 2 && m.Parameters[0].ParameterType.MetadataType == MetadataType.String && m.Parameters[1].ParameterType.MetadataType == MetadataType.String);
        if ((int)definition.Attributes != 0x96 || (int)definition.ImplAttributes != 0 || !definition.HasBody
            || definition.HasGenericParameters || definition.Parameters.Count != 2 || definition.ReturnType.FullName != "System.Runtime.Remoting.ObjectHandle"
            || definition.Parameters[0].ParameterType.MetadataType != MetadataType.String
            || definition.Parameters[1].ParameterType.MetadataType != MetadataType.String)
            throw new InvalidOperationException("Activator.CreateInstanceFrom declaration changed");
        string path = Path.GetFullPath(args[1]);
        using var app = AssemblyDefinition.ReadAssembly(path, new ReaderParameters { InMemory = true });
        var module = app.MainModule;
        var program = module.GetType("PathActivationSubset.Program");
        MethodDefinition Method(string name) => program.Methods.Single(m => m.Name == name);
        if (args[0] is "--path-activation-oracle" or "--path-activation-smoke-oracle")
        {
            var entry = Method("RunArtifactChecks");
            entry.Body = new MethodBody(entry);
            var il = entry.Body.GetILProcessor();
            il.Emit(OpCodes.Call, Method(args[0] == "--path-activation-smoke-oracle" ? "Run" : "RunOracleCases"));
            il.Emit(OpCodes.Ret);
            module.EntryPoint = entry;
        }
        else
        {
            var target = module.ImportReference(definition);
            foreach (string name in new[] { "Direct", "Pointer" })
            {
                var stub = Method(name);
                stub.Body = new MethodBody(stub);
                var il = stub.Body.GetILProcessor();
                foreach (var parameter in stub.Parameters) il.Emit(OpCodes.Ldarg, parameter);
                if (name == "Pointer")
                {
                    il.Emit(OpCodes.Ldftn, target);
                    var site = new CallSite(target.ReturnType);
                    foreach (var parameter in target.Parameters) site.Parameters.Add(new ParameterDefinition(parameter.ParameterType));
                    il.Emit(OpCodes.Calli, site);
                }
                else il.Emit(OpCodes.Call, target);
                il.Emit(OpCodes.Ret);
            }
            var address = Method("Address");
            var query = program.NestedTypes.Single(t => t.Name == "Create");
            var actionCtor = query.Methods.Single(m => m.IsConstructor);
            address.Body = new MethodBody(address);
            var ai = address.Body.GetILProcessor();
            ai.Emit(OpCodes.Ldnull);
            ai.Emit(OpCodes.Ldftn, target);
            ai.Emit(OpCodes.Newobj, module.ImportReference(actionCtor));
            ai.Emit(OpCodes.Ret);
            if (negative)
            {
                string mode = args[3];
                bool pointer = mode.StartsWith("address-", StringComparison.Ordinal);
                if (pointer) mode = mode.Substring(8);
                var malformed = new MethodReference(target.Name, target.ReturnType, target.DeclaringType) { HasThis = false };
                foreach (var parameter in target.Parameters) malformed.Parameters.Add(new ParameterDefinition(parameter.ParameterType));
                switch (mode)
                {
                    case "return": malformed.ReturnType = module.TypeSystem.Boolean; break;
                    case "modifier": malformed.ReturnType = new OptionalModifierType(module.TypeSystem.Object, target.ReturnType); break;
                    case "argument": malformed.Parameters[0].ParameterType = module.TypeSystem.Object; break;
                    case "array": malformed.Parameters[1].ParameterType = new ArrayType(module.TypeSystem.String); break;
                    case "byref": malformed.Parameters[1].ParameterType = new ByReferenceType(target.Parameters[1].ParameterType); break;
                    case "instance": malformed.HasThis = true; break;
                    case "generic": malformed.GenericParameters.Add(new GenericParameter("T", malformed)); break;
                    default: throw new InvalidOperationException("unknown negative " + mode);
                }
                Method(pointer ? "Address" : "Direct").Body.Instructions.Single(i => i.OpCode == (pointer ? OpCodes.Ldftn : OpCodes.Call)).Operand = malformed;
            }
            if (args[0] == "--path-activation-noalloc")
                RuntimeTypeDefaultCtorFixture.MarkNoAlloc(module, Method("Direct"));
            module.EntryPoint = Method(args[0] switch
            {
                "--path-activation-reflection" => "RunReflectionChecks",
                _ => "RunArtifactChecks"
            });
        }
        app.Write(path, new WriterParameters { Timestamp = 0, DeterministicMvid = true });
        return true;
    }
}

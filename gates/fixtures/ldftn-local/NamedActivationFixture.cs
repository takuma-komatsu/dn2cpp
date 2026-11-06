using System.Globalization;
using Mono.Cecil;
using Mono.Cecil.Cil;

internal static class NamedActivationFixture
{
    public static bool Run(string[] args)
    {
        if (args.Length == 2 && args[0] == "--named-activation-planner-metadata")
        {
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
            using var image = AssemblyDefinition.ReadAssembly(args[1]);
            var planner = image.MainModule.GetType("NamedActivationSubset.PlannerOnly");
            if (planner == null || !RuntimeTypeDefaultCtorFixture.HasExecutableBody(
                planner.Methods.SingleOrDefault(m => m.IsConstructor && !m.IsStatic && m.Parameters.Count == 0)))
                throw new InvalidOperationException("literal named planner lost executable default constructor");
            // ILDiet conservatively keeps constructor CIL; native planning separately
            // verifies that only the parameterless body is emitted.
            Console.WriteLine("named-activation-planner-metadata:default-retained");
            return true;
        }

        bool negative = args.Length == 4 && args[0] == "--named-activation-negative";
        if (!negative && (args.Length != 3 || !new[]
            { "--named-activation", "--named-activation-reflection", "--named-activation-noalloc", "--named-activation-oracle", "--named-activation-smoke-oracle", "--named-activation-planner" }.Contains(args[0])))
            return false;
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        using var core = AssemblyDefinition.ReadAssembly(args[2]);
        var definition = core.MainModule.GetType("System.Activator").Methods.Single(m => m.Name == "CreateInstance" && m.IsStatic && m.Parameters.Count == 2 && m.Parameters[0].ParameterType.MetadataType == MetadataType.String && m.Parameters[1].ParameterType.MetadataType == MetadataType.String);
        if ((int)definition.Attributes != 0x8096 || (int)definition.ImplAttributes != 0 || !definition.HasBody
            || definition.HasGenericParameters || definition.Parameters.Count != 2 || definition.ReturnType.FullName != "System.Runtime.Remoting.ObjectHandle"
            || definition.Parameters[0].ParameterType.MetadataType != MetadataType.String
            || definition.Parameters[1].ParameterType.MetadataType != MetadataType.String)
            throw new InvalidOperationException("Activator.CreateInstance named declaration changed");
        string path = Path.GetFullPath(args[1]);
        using var app = AssemblyDefinition.ReadAssembly(path, new ReaderParameters { InMemory = true });
        var module = app.MainModule;
        var program = module.GetType("NamedActivationSubset.Program");
        MethodDefinition Method(string name) => program.Methods.Single(m => m.Name == name);
        if (args[0] is "--named-activation-oracle" or "--named-activation-smoke-oracle")
        {
            var entry = Method("RunArtifactChecks");
            entry.Body = new MethodBody(entry);
            var il = entry.Body.GetILProcessor();
            il.Emit(OpCodes.Call, Method(args[0] == "--named-activation-smoke-oracle" ? "Run" : "RunOracleCases"));
            il.Emit(OpCodes.Ret);
            module.EntryPoint = entry;
        }
        else
        {
            var target = module.ImportReference(definition);
            var domain = core.MainModule.GetType("System.AppDomain");
            var domainCreate = domain.Methods.Single(m => m.Name == "CreateInstance" && m.IsPublic && !m.IsStatic
                && m.Parameters.Count == 2 && m.Parameters.All(p => p.ParameterType.MetadataType == MetadataType.String));
            var domainStub = Method("DomainNull");
            domainStub.Body = new MethodBody(domainStub);
            var di = domainStub.Body.GetILProcessor();
            di.Emit(OpCodes.Ldtoken, module.ImportReference(domain));
            di.Emit(OpCodes.Call, module.ImportReference(core.MainModule.GetType("System.Type").Methods.Single(m => m.Name == "GetTypeFromHandle")));
            di.Emit(OpCodes.Call, module.ImportReference(core.MainModule.GetType("System.Runtime.CompilerServices.RuntimeHelpers").Methods.Single(m => m.Name == "GetUninitializedObject")));
            di.Emit(OpCodes.Castclass, module.ImportReference(domain));
            di.Emit(OpCodes.Ldnull);
            di.Emit(OpCodes.Ldstr, "ignored");
            di.Emit(OpCodes.Callvirt, module.ImportReference(domainCreate));
            di.Emit(OpCodes.Pop);
            di.Emit(OpCodes.Ret);

            foreach (string name in new[] { "Direct", "Pointer" })
            {
                var stub = Method(name);
                stub.Body = new MethodBody(stub);
                var il = stub.Body.GetILProcessor();
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldarg_1);
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
            var invoke = Method("DelegateRoute").Body.Instructions.Select(i => i.Operand).OfType<MethodReference>().Single(m => m.Name == "Invoke");
            var actionCtor = new MethodReference(".ctor", module.TypeSystem.Void, invoke.DeclaringType) { HasThis = true };
            actionCtor.Parameters.Add(new ParameterDefinition(module.TypeSystem.Object));
            actionCtor.Parameters.Add(new ParameterDefinition(module.TypeSystem.IntPtr));
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
                    case "args-element": malformed.Parameters[1].ParameterType = new ArrayType(module.TypeSystem.String); break;
                    case "args-byref": malformed.Parameters[1].ParameterType = new ByReferenceType(target.Parameters[1].ParameterType); break;
                    case "instance": malformed.HasThis = true; break;
                    case "generic": malformed.GenericParameters.Add(new GenericParameter("T", malformed)); break;
                    default: throw new InvalidOperationException("unknown negative " + mode);
                }
                Method(pointer ? "Address" : "Direct").Body.Instructions.Single(i => i.OpCode == (pointer ? OpCodes.Ldftn : OpCodes.Call)).Operand = malformed;
            }
            if (args[0] == "--named-activation-noalloc")
                RuntimeTypeDefaultCtorFixture.MarkNoAlloc(module, Method("Direct"));
            module.EntryPoint = Method(args[0] switch
            {
                "--named-activation-reflection" => "RunReflectionChecks",
                "--named-activation-planner" => "RunPlannerChecks",
                _ => "RunArtifactChecks"
            });
        }
        app.Write(path, new WriterParameters { Timestamp = 0, DeterministicMvid = true });
        return true;
    }
}

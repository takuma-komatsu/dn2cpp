using System.Globalization;
using Mono.Cecil;
using Mono.Cecil.Cil;

internal static class AssemblyExecutionFixture
{
    public static bool Run(string[] args)
    {
        bool negative = args.Length == 4 && args[0] == "--assembly-execution-negative";
        if (!negative && (args.Length != 3 || !new[]
            { "--assembly-execution", "--assembly-execution-reflection", "--assembly-execution-noalloc", "--assembly-execution-oracle" }.Contains(args[0])))
            return false;
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        using var core = AssemblyDefinition.ReadAssembly(args[2]);
        var definition = core.MainModule.GetType("System.AppDomain").Methods.Single(m => m.Name == "ExecuteAssembly" && m.IsStatic && m.Parameters.Count == 2);
        if ((int)definition.Attributes != 0x91 || (int)definition.ImplAttributes != 0 || !definition.HasBody
            || definition.HasGenericParameters || definition.Parameters.Count != 2 || definition.ReturnType.MetadataType != MetadataType.Int32
            || definition.Parameters[0].ParameterType.FullName != "System.Reflection.Assembly"
            || definition.Parameters[1].ParameterType.FullName != "System.String[]")
            throw new InvalidOperationException("AppDomain.ExecuteAssembly declaration changed");
        string path = Path.GetFullPath(args[1]);
        using var app = AssemblyDefinition.ReadAssembly(path, new ReaderParameters { InMemory = true });
        var module = app.MainModule;
        var program = module.GetType("AssemblyExecutionSubset.Program");
        MethodDefinition Method(string name) => program.Methods.Single(m => m.Name == name);
        if (args[0] == "--assembly-execution-oracle")
        {
            var entry = Method("RunArtifactChecks");
            entry.Body = new MethodBody(entry);
            var il = entry.Body.GetILProcessor();
            il.Emit(OpCodes.Call, Method("Run"));
            il.Emit(OpCodes.Ret);
            module.EntryPoint = entry;
        }
        else
        {
            var target = module.ImportReference(definition);
            var producer = Method("ProducerRoute");
            producer.Body = new MethodBody(producer);
            var pi = producer.Body.GetILProcessor();
            var normal = Instruction.Create(OpCodes.Call, Method("Arguments"));
            var invokeTarget = Instruction.Create(OpCodes.Call, target);
            pi.Emit(OpCodes.Ldarg_0);
            pi.Emit(OpCodes.Call, Method("AssemblyArgument"));
            pi.Emit(OpCodes.Ldarg_1);
            pi.Emit(OpCodes.Ldarg_2);
            pi.Emit(OpCodes.Brfalse, normal);
            pi.Emit(OpCodes.Call, Method("ThrowingArguments"));
            pi.Emit(OpCodes.Br, invokeTarget);
            pi.Append(normal);
            pi.Append(invokeTarget);
            pi.Emit(OpCodes.Ret);


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
                    var site = new CallSite(module.TypeSystem.Int32);
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
                var malformed = new MethodReference(target.Name, module.TypeSystem.Int32, target.DeclaringType) { HasThis = false };
                foreach (var parameter in target.Parameters) malformed.Parameters.Add(new ParameterDefinition(parameter.ParameterType));
                switch (mode)
                {
                    case "return": malformed.ReturnType = module.TypeSystem.Boolean; break;
                    case "modifier": malformed.ReturnType = new OptionalModifierType(module.TypeSystem.Object, module.TypeSystem.Int32); break;
                    case "argument": malformed.Parameters[0].ParameterType = module.TypeSystem.Object; break;
                    case "args-element": malformed.Parameters[1].ParameterType = new ArrayType(module.TypeSystem.Object); break;
                    case "args-byref": malformed.Parameters[1].ParameterType = new ByReferenceType(target.Parameters[1].ParameterType); break;
                    case "instance": malformed.HasThis = true; break;
                    case "generic": malformed.GenericParameters.Add(new GenericParameter("T", malformed)); break;
                    default: throw new InvalidOperationException("unknown negative " + mode);
                }
                Method(pointer ? "Address" : "Direct").Body.Instructions.Single(i => i.OpCode == (pointer ? OpCodes.Ldftn : OpCodes.Call)).Operand = malformed;
            }
            if (args[0] == "--assembly-execution-noalloc")
                RuntimeTypeDefaultCtorFixture.MarkNoAlloc(module, Method("Direct"));
            module.EntryPoint = Method(args[0] == "--assembly-execution-reflection" ? "RunReflectionChecks" : "RunArtifactChecks");
        }
        app.Write(path, new WriterParameters { Timestamp = 0, DeterministicMvid = true });
        return true;
    }
}

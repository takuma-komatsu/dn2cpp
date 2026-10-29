using System.Globalization;
using Mono.Cecil;
using Mono.Cecil.Cil;

internal static class RuntimeUtf8NameFixture
{
    public static bool Run(string[] args)
    {
        bool negative = args.Length == 4 && args[0] == "--runtime-utf8-name-negative";
        if (!negative && (args.Length != 3 || !new[]
            { "--runtime-utf8-name", "--runtime-utf8-name-direct", "--runtime-utf8-name-reflection", "--runtime-utf8-name-noalloc", "--runtime-utf8-name-oracle" }.Contains(args[0])))
            return false;
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        using var core = AssemblyDefinition.ReadAssembly(args[2]);
        var definition = core.MainModule.GetType("System.RuntimeTypeHandle").Methods.Single(m => m.Name == "GetUtf8Name");
        var valueDef = core.MainModule.GetType("System.MdUtf8String");
        if ((int)definition.Attributes != 0x93 || (int)definition.ImplAttributes != 0 || !definition.HasBody
            || definition.HasGenericParameters || definition.Parameters.Count != 1
            || definition.Parameters[0].ParameterType.FullName != "System.RuntimeType"
            || definition.ReturnType.Resolve() != valueDef || !valueDef.IsValueType)
            throw new InvalidOperationException("GetUtf8Name declaration changed");
        string path = Path.GetFullPath(args[1]);
        using var app = AssemblyDefinition.ReadAssembly(path, new ReaderParameters { InMemory = true });
        var module = app.MainModule;
        var program = module.GetType("RuntimeUtf8NameSubset.Program");
        MethodDefinition Method(string name) => program.Methods.Single(m => m.Name == name);
        if (args[0] == "--runtime-utf8-name-oracle")
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
            var value = module.ImportReference(valueDef);
            var runtimeType = module.ImportReference(definition.Parameters[0].ParameterType);
            var query = program.NestedTypes.Single(t => t.Name == "Query");
            var invoke = query.Methods.Single(m => m.Name == "Invoke");
            invoke.ReturnType = value;
            invoke.Parameters[0].ParameterType = runtimeType;
            var begin = query.Methods.SingleOrDefault(m => m.Name == "BeginInvoke");
            if (begin != null) begin.Parameters[0].ParameterType = runtimeType;
            var end = query.Methods.SingleOrDefault(m => m.Name == "EndInvoke");
            if (end != null) end.ReturnType = value;
            var address = Method("Address");
            address.Body = new MethodBody(address);
            var ai = address.Body.GetILProcessor();
            ai.Emit(OpCodes.Ldnull);
            ai.Emit(OpCodes.Ldftn, target);
            ai.Emit(OpCodes.Newobj, query.Methods.Single(m => m.IsConstructor));
            ai.Emit(OpCodes.Ret);
            foreach (string name in new[] { "Direct", "Pointer", "DelegateRoute" })
            {
                var stub = Method(name);
                stub.Body = new MethodBody(stub);
                var il = stub.Body.GetILProcessor();
                if (name == "DelegateRoute") il.Emit(OpCodes.Call, address);
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Castclass, runtimeType);
                if (name == "Pointer")
                {
                    il.Emit(OpCodes.Ldftn, target);
                    var site = new CallSite(value);
                    site.Parameters.Add(new ParameterDefinition(runtimeType));
                    il.Emit(OpCodes.Calli, site);
                }
                else if (name == "DelegateRoute") il.Emit(OpCodes.Callvirt, invoke);
                else il.Emit(OpCodes.Call, target);
                il.Emit(OpCodes.Pop);
                il.Emit(OpCodes.Ret);
            }
            if (negative)
            {
                string mode = args[3];
                bool pointer = mode.StartsWith("address-", StringComparison.Ordinal);
                if (pointer) mode = mode.Substring(8);
                var malformed = new MethodReference(target.Name, value, target.DeclaringType) { HasThis = false };
                malformed.Parameters.Add(new ParameterDefinition(runtimeType));
                switch (mode)
                {
                    case "return": malformed.ReturnType = module.TypeSystem.Object; break;
                    case "class-return": malformed.ReturnType = new TypeReference(value.Namespace, value.Name, module, value.Scope, false); break;
                    case "byref": malformed.ReturnType = new ByReferenceType(value); break;
                    case "modifier": malformed.ReturnType = new OptionalModifierType(module.TypeSystem.Object, value); break;
                    case "argument": malformed.Parameters[0].ParameterType = module.TypeSystem.Object; break;
                    case "argument-byref": malformed.Parameters[0].ParameterType = new ByReferenceType(runtimeType); break;
                    case "instance": malformed.HasThis = true; break;
                    case "generic": malformed.GenericParameters.Add(new GenericParameter("T", malformed)); break;
                    default: throw new InvalidOperationException("unknown negative " + mode);
                }
                Method(pointer ? "Address" : "Direct").Body.Instructions.Single(i => i.OpCode == (pointer ? OpCodes.Ldftn : OpCodes.Call)).Operand = malformed;
            }
            if (args[0] == "--runtime-utf8-name-noalloc")
                RuntimeTypeDefaultCtorFixture.MarkNoAlloc(module, Method("Direct"));
            module.EntryPoint = Method(args[0] switch
            {
                "--runtime-utf8-name-reflection" => "RunReflectionChecks",
                "--runtime-utf8-name-direct" => "RunDirectChecks",
                _ => "RunArtifactChecks"
            });
        }
        app.Write(path, new WriterParameters { Timestamp = 0, DeterministicMvid = true });
        return true;
    }
}

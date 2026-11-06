using System.Globalization;
using Mono.Cecil;
using Mono.Cecil.Cil;

internal static class FunctionPointerArgumentsFixture
{
    public static bool Run(string[] args)
    {
        bool negative = args.Length == 4 && args[0] == "--fnptr-arguments-negative";
        if (!negative && (args.Length != 3 || !new[]
        {
            "--fnptr-arguments", "--fnptr-arguments-reflection", "--fnptr-arguments-noalloc", "--fnptr-arguments-oracle", "--fnptr-arguments-raw-import", "--fnptr-arguments-raw-import-address"
        }
        .Contains(args[0])))
            return false;
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        using var core = AssemblyDefinition.ReadAssembly(args[2]);
        var owner = core.MainModule.GetType("System.RuntimeTypeHandle");
        var targetDef = owner.Methods.Single(m => m.Name == "GetArgumentTypesFromFunctionPointer" && m.IsStatic && m.Parameters.Count == 1);
        if ((int)targetDef.Attributes != 0x93 || (int)targetDef.ImplAttributes != 0 || !targetDef.HasBody || targetDef.RVA == 0 || targetDef.HasGenericParameters || targetDef.ReturnType.FullName != "System.Type[]" || targetDef.Parameters[0].ParameterType.FullName != "System.RuntimeType")
            throw new InvalidOperationException("GetArgumentTypesFromFunctionPointer metadata changed");
        string path = Path.GetFullPath(args[1]);
        using var app = AssemblyDefinition.ReadAssembly(path, new ReaderParameters
        {
            InMemory = true
        });
        var mod = app.MainModule;
        var program = mod.GetType("FunctionPointerArgumentsSubset.Program");
        MethodDefinition Method(string name) => program.Methods.Single(m => m.Name == name);
        if (args[0].StartsWith("--fnptr-arguments-raw-import", StringComparison.Ordinal))
        {
            var raw = owner.Methods.Single(m => m.Name == "GetArgumentTypesFromFunctionPointer" && m.IsPInvokeImpl);
            if ((int)raw.Attributes != 0x2091 || (int)raw.ImplAttributes != 0x80 || raw.HasBody || raw.Parameters.Count != 2
                || raw.ReturnType.MetadataType != MetadataType.Void
                || raw.Parameters[0].ParameterType.FullName != "System.Runtime.CompilerServices.QCallTypeHandle"
                || raw.Parameters[1].ParameterType.FullName != "System.Runtime.CompilerServices.ObjectHandleOnStack"
                || raw.PInvokeInfo is not { Module.Name: "QCall", EntryPoint: "RuntimeTypeHandle_GetArgumentTypesFromFunctionPointer" })
                throw new InvalidOperationException("raw function-pointer arguments import changed");
            var import = mod.ImportReference(raw);
            var entry = new MethodDefinition("RawArgumentsCompileOnly", MethodAttributes.Private | MethodAttributes.Static, mod.TypeSystem.Void);
            program.Methods.Add(entry);
            entry.Body.InitLocals = true;
            var il = entry.Body.GetILProcessor();
            var locals = import.Parameters.Select(p => new VariableDefinition(p.ParameterType)).ToArray();
            foreach (var local in locals)
            {
                entry.Body.Variables.Add(local);
                il.Emit(OpCodes.Ldloca, local);
                il.Emit(OpCodes.Initobj, local.VariableType);
            }
            foreach (var local in locals) il.Emit(OpCodes.Ldloc, local);
            if (args[0].EndsWith("-address", StringComparison.Ordinal))
            {
                il.Emit(OpCodes.Ldftn, import);
                var site = new CallSite(mod.TypeSystem.Void);
                foreach (var parameter in import.Parameters) site.Parameters.Add(new ParameterDefinition(parameter.ParameterType));
                il.Emit(OpCodes.Calli, site);
            }
            else il.Emit(OpCodes.Call, import);
            il.Emit(OpCodes.Ret);
            mod.EntryPoint = entry;
            app.Write(path, new WriterParameters { Timestamp = 0, DeterministicMvid = true });
            return true;
        }

        if (args[0] == "--fnptr-arguments-oracle")
        {
            var entry = Method("RunArtifactChecks");
            entry.Body = new MethodBody(entry);
            var il = entry.Body.GetILProcessor();
            il.Emit(OpCodes.Call, Method("Run"));
            il.Emit(OpCodes.Ret);
            mod.EntryPoint = entry;
        }
        else
        {
            var target = mod.ImportReference(targetDef);
            var rt = mod.ImportReference(targetDef.Parameters[0].ParameterType);
            var del = program.NestedTypes.Single(t => t.Name == "Query");
            var invoke = del.Methods.Single(m => m.Name == "Invoke");
            invoke.Parameters[0].ParameterType = rt;
            var begin = del.Methods.SingleOrDefault(m => m.Name == "BeginInvoke");
            if (begin != null)
                begin.Parameters[0].ParameterType = rt;
            var address = Method("Address");
            address.Body = new MethodBody(address);
            var ai = address.Body.GetILProcessor();
            ai.Emit(OpCodes.Ldnull);
            ai.Emit(OpCodes.Ldftn, target);
            ai.Emit(OpCodes.Newobj, del.Methods.Single(m => m.IsConstructor));
            ai.Emit(OpCodes.Ret);
            foreach (string name in new[]
            {
                "Direct", "Pointer", "DelegateRoute"
            }
            )
            {
                var stub = Method(name);
                stub.Body = new MethodBody(stub);
                var il = stub.Body.GetILProcessor();
                if (name == "DelegateRoute")
                    il.Emit(OpCodes.Call, address);
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Castclass, rt);
                if (name == "Pointer")
                {
                    il.Emit(OpCodes.Ldftn, target);
                    var site = new CallSite(target.ReturnType);
                    site.Parameters.Add(new ParameterDefinition(rt));
                    il.Emit(OpCodes.Calli, site);
                }
                else
                    il.Emit(name == "DelegateRoute"?OpCodes.Callvirt:OpCodes.Call, name == "DelegateRoute"?invoke:target);
                il.Emit(OpCodes.Ret);
            }
            if (negative)
            {
                string mode = args[3];
                bool addressMode = mode.StartsWith("address-", StringComparison.Ordinal);
                if (addressMode)
                    mode = mode.Substring(8);
                var malformed = new MethodReference(target.Name, target.ReturnType, target.DeclaringType)
                {
                    HasThis = false
                };
                malformed.Parameters.Add(new ParameterDefinition(rt));
                switch (mode)
                {
                    case "return":
                        malformed.ReturnType = mod.TypeSystem.Int32;
                        break;
                    case "return-byref":
                        malformed.ReturnType = new ByReferenceType(target.ReturnType);
                        break;
                    case "return-modifier":
                        malformed.ReturnType = new OptionalModifierType(mod.TypeSystem.Object, target.ReturnType);
                        break;
                    case "argument":
                        malformed.Parameters[0].ParameterType = mod.TypeSystem.Object;
                        break;
                    case "argument-byref":
                        malformed.Parameters[0].ParameterType = new ByReferenceType(rt);
                        break;
                    case "argument-modifier":
                        malformed.Parameters[0].ParameterType = new OptionalModifierType(mod.TypeSystem.Object, rt);
                        break;
                    case "instance":
                        malformed.HasThis = true;
                        break;
                    case "generic":
                        malformed.GenericParameters.Add(new GenericParameter("T", malformed));
                        break;
                    default:
                        throw new InvalidOperationException("unknown negative " + mode);
                }
                Method(addressMode ? "Address" : "Direct").Body.Instructions.Single(i => i.OpCode == (addressMode ? OpCodes.Ldftn : OpCodes.Call)).Operand = malformed;
            }
            if (args[0] == "--fnptr-arguments-noalloc")
                RuntimeTypeDefaultCtorFixture.MarkNoAlloc(mod, Method("Direct"));
            mod.EntryPoint = Method(args[0] == "--fnptr-arguments-reflection" ? "RunReflectionChecks" : "RunArtifactChecks");
        }
        app.Write(path, new WriterParameters
        {
            Timestamp = 0, DeterministicMvid = true
        });
        return true;
    }
}

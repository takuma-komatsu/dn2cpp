using System.Globalization;
using Mono.Cecil;
using Mono.Cecil.Cil;

internal static class RuntimeTypeHandleNameFixture
{
    public static bool Run(string[] args)
    {
        if (args.Length is not (3 or 4) || !args[0].StartsWith("--runtime-type-handle-name", StringComparison.Ordinal)) return false;
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture; CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        using var core = AssemblyDefinition.ReadAssembly(args[2]);
        var owner = core.MainModule.GetType("System.RuntimeTypeHandle");
        var definition = owner.Methods.Single(m => m.Name == "ConstructName" && !m.IsStatic);
        var flags = definition.Parameters.Single().ParameterType.Resolve();
        if (core.Name.Name != "System.Private.CoreLib" || definition.Attributes != (MethodAttributes.Assembly | MethodAttributes.HideBySig)
            || definition.ImplAttributes != MethodImplAttributes.IL || !definition.HasBody || !definition.HasThis || definition.ExplicitThis || definition.HasGenericParameters
            || definition.CallingConvention != MethodCallingConvention.Default || definition.ReturnType.MetadataType != MetadataType.String
            || flags.Module != core.MainModule || flags.FullName != "System.TypeNameFormatFlags" || !flags.IsEnum
            || flags.Fields.Single(f => f.Name == "value__").FieldType.MetadataType != MetadataType.Int32)
            throw new InvalidOperationException("ConstructName managed signature changed");
        using var app = AssemblyDefinition.ReadAssembly(args[1], new ReaderParameters { InMemory = true });
        var module = app.MainModule; var checks = module.GetType("RuntimeTypeHandleNameSubset.NameChecks");
        if (args[0] is "--runtime-type-handle-name-raw-import" or "--runtime-type-handle-name-raw-import-address")
        {
            var raw = owner.Methods.Single(m => m.Name == "ConstructName" && m.IsPInvokeImpl);
            if (raw.Attributes != (MethodAttributes.Private | MethodAttributes.Static | MethodAttributes.HideBySig | MethodAttributes.PInvokeImpl)
                || raw.ImplAttributes != MethodImplAttributes.PreserveSig || raw.HasBody || raw.RVA != 0 || raw.HasThis || raw.ExplicitThis
                || raw.HasGenericParameters || raw.CallingConvention != MethodCallingConvention.Default
                || raw.ReturnType.MetadataType != MetadataType.Void || raw.Parameters.Count != 3
                || raw.Parameters[0].ParameterType.FullName != "System.Runtime.CompilerServices.QCallTypeHandle"
                || raw.Parameters[1].ParameterType.Resolve() != flags
                || raw.Parameters[2].ParameterType.FullName != "System.Runtime.CompilerServices.StringHandleOnStack"
                || raw.Parameters.Any(p => p.ParameterType.Resolve().Module != core.MainModule || !p.ParameterType.Resolve().IsValueType)
                || raw.PInvokeInfo is not { Module.Name: "QCall", EntryPoint: "RuntimeTypeHandle_ConstructName" })
                throw new InvalidOperationException("ConstructName raw import metadata changed");
            var import = module.ImportReference(raw);
            var entry = new MethodDefinition("RawNameImportCompileOnly", MethodAttributes.Private | MethodAttributes.Static, module.TypeSystem.Void);
            checks.Methods.Add(entry); entry.Body.InitLocals = true;
            var handle = new VariableDefinition(import.Parameters[0].ParameterType);
            var result = new VariableDefinition(import.Parameters[2].ParameterType);
            entry.Body.Variables.Add(handle); entry.Body.Variables.Add(result);
            var il = entry.Body.GetILProcessor();
            il.Emit(OpCodes.Ldloca, handle); il.Emit(OpCodes.Initobj, handle.VariableType);
            il.Emit(OpCodes.Ldloca, result); il.Emit(OpCodes.Initobj, result.VariableType);
            il.Emit(OpCodes.Ldloc, handle); il.Emit(OpCodes.Ldc_I4_0); il.Emit(OpCodes.Ldloc, result);
            if (args[0].EndsWith("-address", StringComparison.Ordinal))
            {
                il.Emit(OpCodes.Ldftn, import); var site = new CallSite(module.TypeSystem.Void);
                foreach (var parameter in import.Parameters) site.Parameters.Add(new ParameterDefinition(parameter.ParameterType));
                il.Emit(OpCodes.Calli, site);
            }
            else il.Emit(OpCodes.Call, import);
            il.Emit(OpCodes.Ret); module.EntryPoint = entry;
            app.Write(args[1], new WriterParameters { Timestamp = 0, DeterministicMvid = true });
            Console.WriteLine("type-name-raw-import: actual QCall MemberRef; compile only, DO NOT EXECUTE");
            return true;
        }
        bool oracle = args[0] == "--runtime-type-handle-name-oracle";
        bool negative = args[0] == "--runtime-type-handle-name-negative";
        MethodReference target = module.ImportReference(definition);
        if (negative)
        {
            target = new MethodReference(target.Name, target.ReturnType, target.DeclaringType) { HasThis = true };
            target.Parameters.Add(new ParameterDefinition(module.ImportReference(flags)));
            switch (args[3].Replace("address-", "", StringComparison.Ordinal))
            {
                case "wrong-enum": target.Parameters[0].ParameterType = module.ImportReference(typeof(System.Reflection.BindingFlags)); break;
                case "primitive": target.Parameters[0].ParameterType = module.TypeSystem.Int32; break;
                case "named-primitive": target.Parameters[0].ParameterType = new TypeReference("System", "Int32", module, module.ImportReference(flags).Scope, true); break;
                case "modifier": target.Parameters[0].ParameterType = new OptionalModifierType(module.ImportReference(typeof(System.Runtime.CompilerServices.IsConst)), target.Parameters[0].ParameterType); break;
                case "byref": target.Parameters[0].ParameterType = new ByReferenceType(target.Parameters[0].ParameterType); break;
                case "static": target.HasThis = false; break;
                case "generic": target.GenericParameters.Add(new GenericParameter("T",target)); break;
                default: throw new InvalidOperationException("Unknown negative");
            }
        }
        if (!oracle)
        {
            // Delegate binding compares the true enum identity, not an Int32 approximation.
            var bound = checks.NestedTypes.Single(t => t.Name == "Bound");
            foreach (var member in bound.Methods.Where(m => m.Name is "Invoke" or "BeginInvoke")) member.Parameters[0].ParameterType = module.ImportReference(flags);
            foreach (string route in new[] { "Direct", "Pointer", "Address" })
            {
                var method = checks.Methods.Single(m => m.Name == route); method.Body = new MethodBody(method); var il = method.Body.GetILProcessor();
                il.Emit(OpCodes.Ldarg_0);
                if (route == "Address")
                {
                    il.Emit(OpCodes.Box, module.ImportReference(owner)); il.Emit(OpCodes.Ldftn,target);
                    var ctor = new MethodReference(".ctor",module.TypeSystem.Void,method.ReturnType) { HasThis=true };
                    ctor.Parameters.Add(new ParameterDefinition(module.TypeSystem.Object)); ctor.Parameters.Add(new ParameterDefinition(module.TypeSystem.IntPtr)); il.Emit(OpCodes.Newobj,ctor);
                }
                else
                {
                    il.Emit(OpCodes.Ldarg_1);
                    if (route == "Pointer")
                    {
                        il.Emit(OpCodes.Ldftn,target); var site = new CallSite(target.ReturnType);
                        site.Parameters.Add(new ParameterDefinition(new ByReferenceType(target.DeclaringType))); site.Parameters.Add(new ParameterDefinition(target.Parameters[0].ParameterType)); il.Emit(OpCodes.Calli,site);
                    }
                    else il.Emit(OpCodes.Call,target);
                }
                il.Emit(OpCodes.Ret);
            }
        }
        if (args[0] == "--runtime-type-handle-name-noalloc") RuntimeTypeDefaultCtorFixture.MarkNoAlloc(module,checks.Methods.Single(m=>m.Name=="Direct"));
        if (negative)
        {
            var entry = new MethodDefinition("MalformedName",MethodAttributes.Private|MethodAttributes.Static,module.TypeSystem.Void); checks.Methods.Add(entry); entry.Body.InitLocals=true;
            var local = new VariableDefinition(module.ImportReference(owner)); entry.Body.Variables.Add(local); var il=entry.Body.GetILProcessor();
            il.Emit(OpCodes.Ldloca,local); il.Emit(OpCodes.Ldc_I4_0); il.Emit(OpCodes.Call,checks.Methods.Single(m=>m.Name==(args[3].StartsWith("address-",StringComparison.Ordinal)?"Pointer":"Direct"))); il.Emit(OpCodes.Pop); il.Emit(OpCodes.Ret); module.EntryPoint=entry;
        }
        else module.EntryPoint=checks.Methods.Single(m=>m.Name==(oracle?"Run":args[0].EndsWith("-reflection",StringComparison.Ordinal)?"RunReflectionArtifact":"RunArtifact"));
        app.Write(args[1],new WriterParameters{Timestamp=0,DeterministicMvid=true});
        Console.WriteLine("type-name: "+args[0]+"; private CLR is never executed"); return true;
    }
}

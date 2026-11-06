using System.Globalization;
using Mono.Cecil;
using Mono.Cecil.Cil;

internal static class PrimitiveBigMulFixture
{
    public static bool Run(string[] args)
    {
        if (args.Length != 5 || args[0] != "--primitive-bigmul-fixture") return false;
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        using var app = AssemblyDefinition.ReadAssembly(args[1], new ReaderParameters { InMemory = true });
        using var core = AssemblyDefinition.ReadAssembly(args[2]);
        var module = app.MainModule;
        var section = module.GetType("Dn2Cpp.PrimitiveBigMulSubset");
        string mode = args[4];
        if (mode == "address")
        {
            var target = section.Methods.Single(m => m.Name == "RunAddress");
            if (target.Body.Instructions.Count(i => i.OpCode == OpCodes.Ldftn && i.Operand is MethodReference m && m.Name == "BigMul") != 6
                || target.Body.Instructions.Count(i => i.OpCode == OpCodes.Calli) != 6
                || target.Body.Instructions.Any(i => i.OpCode == OpCodes.Call && i.Operand is MethodReference m && m.Name == "BigMul"))
                throw new InvalidOperationException("address fixture must contain only six ldftn/calli BigMul routes");
            // Keep the driver's invariant-culture prelude when selecting an address-only root.
            var call = module.EntryPoint.Body.Instructions.Single(i => i.OpCode == OpCodes.Call
                && i.Operand is MethodReference method && method.DeclaringType.FullName == section.FullName);
            call.Operand = target;
        }
        else if (mode == "reflection")
            module.EntryPoint = section.Methods.Single(m => m.Name == "RunReflection");
        else if (mode == "retained")
        {
            var entry = module.EntryPoint;
            entry.Body = new MethodBody(entry);
            var il = entry.Body.GetILProcessor();
            var invariant = module.ImportReference(typeof(CultureInfo).GetProperty(nameof(CultureInfo.InvariantCulture))!.GetMethod!);
            il.Emit(OpCodes.Call, invariant);
            il.Emit(OpCodes.Call, module.ImportReference(typeof(CultureInfo).GetProperty(nameof(CultureInfo.CurrentCulture))!.SetMethod!));
            il.Emit(OpCodes.Call, invariant);
            il.Emit(OpCodes.Call, module.ImportReference(typeof(CultureInfo).GetProperty(nameof(CultureInfo.CurrentUICulture))!.SetMethod!));
            foreach (string name in new[] { "System.IntPtr", "System.UIntPtr" })
            {
                il.Emit(OpCodes.Ldtoken, module.ImportReference(core.MainModule.GetType(name)));
                il.Emit(OpCodes.Pop);
            }
            il.Emit(OpCodes.Ret);
        }
        else
        {
            bool address = mode.StartsWith("address-", StringComparison.Ordinal);
            string mutation = mode[(mode.IndexOf('-') + 1)..];
            var owner = core.MainModule.GetType("System.UIntPtr");
            var target = module.ImportReference(owner.Methods.Single(m => m.Name == "BigMul"));
            switch (mutation)
            {
                case "signed": target.Parameters[1].ParameterType = module.TypeSystem.IntPtr; break;
                case "return": target.ReturnType = module.TypeSystem.IntPtr; break;
                case "out": target.Parameters[2].ParameterType = new ByReferenceType(module.TypeSystem.IntPtr); break;
                case "value": target.Parameters[2].ParameterType = module.TypeSystem.UIntPtr; break;
                case "instance": target.HasThis = true; break;
                case "vararg": target.CallingConvention = MethodCallingConvention.VarArg; break;
                case "modifier": target.ReturnType = new OptionalModifierType(module.TypeSystem.Object, target.ReturnType); break;
                default: throw new InvalidOperationException("unknown primitive BigMul mutation");
            }
            var entry = module.EntryPoint; entry.Body = new MethodBody(entry) { InitLocals = true };
            var local = new VariableDefinition(module.TypeSystem.UIntPtr); entry.Body.Variables.Add(local);
            var il = entry.Body.GetILProcessor();
            if (address) { il.Emit(OpCodes.Ldftn, target); il.Emit(OpCodes.Pop); }
            else
            {
                if (target.HasThis) il.Emit(OpCodes.Ldloca, local);
                il.Emit(OpCodes.Ldc_I4_1); il.Emit(OpCodes.Conv_U); il.Emit(OpCodes.Ldc_I4_2); il.Emit(OpCodes.Conv_U);
                il.Emit(mutation == "value" ? OpCodes.Ldloc : OpCodes.Ldloca, local);
                il.Emit(OpCodes.Call, target); il.Emit(OpCodes.Pop);
            }
            il.Emit(OpCodes.Ret);
        }
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(args[3]))!);
        app.Write(args[3], new WriterParameters { Timestamp = 0, DeterministicMvid = true });
        if (mode == "retained")
        {
            // Isolate type retention from unrelated native-integer sibling bodies.
            // The uncut probe remains a separate witness of the current boundary.
            var cuts = new List<string>();
            foreach (string name in new[] { "System.IntPtr", "System.UIntPtr" })
                foreach (var method in core.MainModule.GetType(name).Methods)
                    if (method.Name != "BigMul" && !method.HasGenericParameters && !cuts.Contains(name + "::" + method.Name))
                        cuts.Add(name + "::" + method.Name);
            File.WriteAllText(args[3] + ".cuts.json", System.Text.Json.JsonSerializer.Serialize(cuts));
        }
        Console.WriteLine("primitive-bigmul-fixture:" + mode);
        return true;
    }
}

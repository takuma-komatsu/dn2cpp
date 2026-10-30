using System.Globalization;
using Mono.Cecil;
using Mono.Cecil.Cil;

internal static class Int128CheckedFixture
{
    public static bool Run(string[] args)
    {
        if (args.Length != 4 || args[0] != "--int128-checked-fixture") return false;
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        string path = Path.GetFullPath(args[1]); string mode = args[3];
        using var app = AssemblyDefinition.ReadAssembly(path, new ReaderParameters { InMemory = true });
        using var core = AssemblyDefinition.ReadAssembly(args[2]);
        var module = app.MainModule; var program = module.GetType("Int128CheckedSubset.Program");
        var pointer = program.Methods.Single(m => m.Name == "Pointer");
        var factory = pointer.Body.Instructions.Single(i => i.OpCode == OpCodes.Ldftn).Operand as GenericInstanceMethod
            ?? throw new InvalidOperationException("pointer target must be a MethodSpec");
        var indirect = (CallSite)pointer.Body.Instructions.Single(i => i.OpCode == OpCodes.Calli).Operand;
        if (factory.Name != "CreateChecked" || factory.DeclaringType.FullName != "System.Int128"
            || factory.GenericArguments.Count != 1 || factory.GenericArguments[0] is not GenericParameter { Position: 0, Type: GenericParameterType.Method }
            || indirect.HasThis || indirect.Parameters.Count != 1 || indirect.ReturnType.FullName != "System.Int128"
            || indirect.Parameters[0].ParameterType is not GenericParameter { Position: 0, Type: GenericParameterType.Method })
            throw new InvalidOperationException("wrong checked pointer ABI");
        if (mode is "all" or "isolated" or "discard")
        {
            if (mode == "isolated")
            {
                var pick = program.NestedTypes.SelectMany(t => t.Methods).Concat(program.Methods)
                    .Single(m => m.Name.Contains("g__Pick", StringComparison.Ordinal));
                if (pick.Body.Instructions.Any(i => i.Operand is MethodReference m && m.Name == "Route")
                    || !pick.Body.Instructions.Any(i => i.Operand is MethodReference m && m.Name == "Selected"))
                    throw new InvalidOperationException("isolated fixture retains alternate route selector");
            }
            if (mode == "discard")
            {
                var entry = module.GetType("Entry").Methods.Single(m => m.Name == "Main");
                if (entry.Body.Instructions.Count(i => i.Operand is GenericInstanceMethod m && m.Name == "CreateChecked") != 1
                    || entry.Body.Instructions.Any(i => i.Operand is MethodReference m && m.DeclaringType.FullName == program.FullName))
                    throw new InvalidOperationException("discard fixture must not use arithmetic or other routes");
            }
            Console.WriteLine("int128-checked-IL:" + mode + ":actual-ldftn-calli");
            return true;
        }
        if (mode == "native-getters")
        {
            var entry = module.GetType("Entry").Methods.Single(m => m.Name == "Main");
            entry.Body = new MethodBody(entry);
            var il = entry.Body.GetILProcessor();
            var write = module.ImportReference(typeof(Console).GetMethod(nameof(Console.WriteLine), new[] { typeof(long) })!);
            var native = core.MainModule.GetType("System.IntPtr");
            foreach (string name in new[] { "get_MinValue", "get_MaxValue" })
            {
                il.Emit(OpCodes.Call, module.ImportReference(native.Methods.Single(m => m.Name == name)));
                il.Emit(OpCodes.Conv_I8); il.Emit(OpCodes.Call, write);
            }
            il.Emit(OpCodes.Ret);
        }
        else if (mode == "retained")
        {
            var entry = module.GetType("Entry").Methods.Single(m => m.Name == "Main");
            var itf = core.MainModule.GetType("System.Numerics.IBinaryInteger`1");
            var method = itf.Methods.Single(m => m.Name == "LeadingZeroCount");
            var owner = new GenericInstanceType(module.ImportReference(itf));
            owner.GenericArguments.Add(module.ImportReference(core.MainModule.GetType("System.Int128")));
            var reference = module.ImportReference(method);
            reference.DeclaringType = owner;
            entry.Body = new MethodBody(entry) { InitLocals = true };
            var value = new VariableDefinition(module.ImportReference(core.MainModule.GetType("System.Int128"))); entry.Body.Variables.Add(value);
            var il = entry.Body.GetILProcessor(); il.Emit(OpCodes.Ldloc_0); il.Emit(OpCodes.Call, reference); il.Emit(OpCodes.Pop); il.Emit(OpCodes.Ret);
            Console.WriteLine("int128-checked-retained:actual-IBinaryInteger-Int128-LeadingZeroCount");
        }
        else
        {
            bool address = mode.StartsWith("address-", StringComparison.Ordinal);
            string mutation = mode[(mode.IndexOf('-') + 1)..];
            var target = core.MainModule.GetType("System.Int128").Methods.Single(m => m.Name == "CreateChecked");
            var reference = module.ImportReference(target);
            switch (mutation)
            {
                case "return": reference.ReturnType = module.ImportReference(core.MainModule.GetType("System.UInt128")); break;
                case "byref": reference.Parameters[0].ParameterType = new ByReferenceType(reference.Parameters[0].ParameterType); break;
                case "modifier": reference.ReturnType = new OptionalModifierType(module.ImportReference(typeof(object)), reference.ReturnType); break;
                case "instance": reference.HasThis = true; break;
                case "mvar": var wrong = new GenericParameter("Wrong", reference); reference.GenericParameters.Add(wrong); reference.Parameters[0].ParameterType = wrong; break;
                default: throw new InvalidOperationException("unknown malformed mode");
            }
            var spec = new GenericInstanceMethod(reference); spec.GenericArguments.Add(module.TypeSystem.Int64);
            var entry = module.GetType("Entry").Methods.Single(m => m.Name == "Main"); entry.Body = new MethodBody(entry);
            var il = entry.Body.GetILProcessor();
            if (!address) il.Emit(OpCodes.Ldc_I8, -7L);
            il.Emit(address ? OpCodes.Ldftn : OpCodes.Call, spec); il.Emit(OpCodes.Pop); il.Emit(OpCodes.Ret);
        }
        app.Write(path, new WriterParameters { Timestamp = 0, DeterministicMvid = true });
        return true;
    }
}

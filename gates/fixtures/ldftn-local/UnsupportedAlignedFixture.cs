using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;

internal static class UnsupportedAlignedFixture
{
    public static bool Run(string[] args)
    {
        if (args is ["--unsupported-aligned-profile", var targetCoreLib])
        {
            using var target = AssemblyDefinition.ReadAssembly(targetCoreLib);
            foreach (var type in new[] { typeof(System.Runtime.Intrinsics.Vector64<int>), typeof(System.Runtime.Intrinsics.Vector128<int>),
                typeof(System.Runtime.Intrinsics.Vector256<int>), typeof(System.Runtime.Intrinsics.Vector512<int>) })
            {
                var getter = type.GetMethods(System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)
                    .Single(m => m.Name.EndsWith(".get_Alignment", StringComparison.Ordinal));
                var owner = target.MainModule.GetType(type.GetGenericTypeDefinition().FullName!);
                var targetGetter = owner.Methods.Single(m => m.Overrides.Any(slot => slot.Name == "get_Alignment"
                    && slot.DeclaringType.GetElementType().FullName == "System.Runtime.Intrinsics.ISimdVector`2"));
                targetGetter.Body.SimplifyMacros();
                var instructions = targetGetter.Body.Instructions;
                if (instructions.Count != 2 || instructions[0].OpCode != OpCodes.Ldc_I4 || instructions[1].OpCode != OpCodes.Ret)
                    throw new InvalidOperationException("expected a constant alignment in " + targetGetter.FullName);
                Console.WriteLine(type.Name.Split('`')[0] + " " + getter.Invoke(null, null) + " " + instructions[0].Operand);
            }
            return true;
        }
        if (args.Length is not (4 or 5) || args[0] != "--unsupported-aligned") return false;
        bool orderedOnly = args.Length == 5;
        if (orderedOnly && args[4] != "ordered-only") throw new ArgumentException("unknown entry point");
        if (File.Exists(args[2])) throw new InvalidOperationException("fixture output must be new");
        using var core = AssemblyDefinition.ReadAssembly(args[3]);
        using var app = AssemblyDefinition.ReadAssembly(args[1]);
        var module = app.MainModule;
        var sample = module.GetType("VectorProbe.UnsupportedAlignedBoundary`2")
            ?? throw new InvalidOperationException("missing Unsupported aligned fixture");
        var definition = core.MainModule.GetType("System.Runtime.Intrinsics.ISimdVector`2");
        var declaration = module.ImportReference(definition);
        var contract = new GenericInstanceType(declaration);
        contract.GenericArguments.Add(sample.GenericParameters[0]);
        contract.GenericArguments.Add(sample.GenericParameters[1]);
        // The CLR checks the self constraint when it loads this open generic body.
        sample.GenericParameters[0].Constraints.Add(new GenericParameterConstraint(contract));
        var names = new[] { "LoadAligned", "StoreAligned" };
        int calls = 0, addresses = 0;
        foreach (var method in sample.Methods.Where(m => m.HasBody))
            foreach (var instruction in method.Body.Instructions)
            {
                if (instruction.Operand is not MethodReference target || target.DeclaringType.GetElementType().FullName != sample.FullName
                    || !names.Contains(target.Name)) continue;
                var actual = definition.Methods.Single(m => m.Name == target.Name);
                var replacement = module.ImportReference(actual);
                replacement.DeclaringType = contract;
                instruction.Operand = replacement;
                if (instruction.OpCode == OpCodes.Call) calls++;
                else if (instruction.OpCode == OpCodes.Ldftn) addresses++;
                else throw new InvalidOperationException("unexpected Unsupported aligned operand");
            }
        if (calls != 4 || addresses != 6) throw new InvalidOperationException("missing Unsupported aligned call/address coverage");
        // The CLR executes these same internal DIM calls as the native probe.
        var attributeType = new TypeDefinition("System.Runtime.CompilerServices", "IgnoresAccessChecksToAttribute",
            TypeAttributes.Sealed, module.ImportReference(typeof(Attribute)));
        module.Types.Add(attributeType);
        var ctor = new MethodDefinition(".ctor", MethodAttributes.Public | MethodAttributes.SpecialName
            | MethodAttributes.RTSpecialName | MethodAttributes.HideBySig, module.TypeSystem.Void);
        attributeType.Methods.Add(ctor);
        ctor.Parameters.Add(new ParameterDefinition(module.TypeSystem.String));
        var il = ctor.Body.GetILProcessor();
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Call, module.ImportReference(core.MainModule.GetType("System.Attribute").Methods.Single(m => m.IsConstructor)));
        il.Emit(OpCodes.Ret);
        var grant = new CustomAttribute(ctor);
        grant.ConstructorArguments.Add(new CustomAttributeArgument(module.TypeSystem.String, "System.Private.CoreLib"));
        app.CustomAttributes.Add(grant);
        if (orderedOnly)
            app.EntryPoint = module.GetType("VectorProbe.UnsupportedAlignedBoundary").Methods.Single(method => method.Name == "RunOrdered");
        app.Write(args[2], new WriterParameters { Timestamp = 0, DeterministicMvid = true });
        Console.WriteLine($"unsupported aligned defaults: {calls} calls, {addresses} addresses");
        return true;
    }
}

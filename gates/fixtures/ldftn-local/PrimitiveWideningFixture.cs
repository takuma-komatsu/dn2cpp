using Mono.Cecil;
using Mono.Cecil.Cil;

internal static class PrimitiveWideningFixture
{
    public static bool Run(string[] args)
    {
        if (args.Length != 3 || args[0] is not ("--primitive-widening" or "--primitive-widening-reflection"
            or "--primitive-widening-oracle" or "--primitive-widening-reflection-oracle")) return false;
        using var core = AssemblyDefinition.ReadAssembly(args[2]);
        if (core.Name.Name != "System.Private.CoreLib") throw new InvalidOperationException("Expected input CoreLib");
        var owner = core.MainModule.GetType("System.Runtime.CompilerServices.RuntimeHelpers");
        var elementType = core.MainModule.GetType("System.Reflection.CorElementType");
        var method = owner.Methods.Single(m => m.Name == "CanPrimitiveWiden");
        if (method.Attributes != (MethodAttributes.Assembly | MethodAttributes.Static | MethodAttributes.HideBySig)
            || method.ImplAttributes != MethodImplAttributes.AggressiveInlining || !method.HasBody || method.HasThis || method.ExplicitThis
            || method.HasGenericParameters || method.CallingConvention != MethodCallingConvention.Default
            || method.ReturnType.MetadataType != MetadataType.Boolean || method.Parameters.Count != 2
            || method.Parameters.Any(p => p.ParameterType != elementType) || !elementType.IsEnum
            || elementType.Fields.Single(f => f.Name == "value__").FieldType.MetadataType != MetadataType.Byte)
            throw new InvalidOperationException("Primitive widening declaration changed");
        Code[] codes = { Code.Ldtoken, Code.Call, Code.Stloc_0, Code.Ldarg_0, Code.Ldloca_S,
            Code.Call, Code.Blt_S, Code.Ldarg_0, Code.Ldarg_1, Code.Ceq, Code.Ret,
            Code.Ldloca_S, Code.Ldarg_0, Code.Call, Code.Ldind_I2, Code.Ldc_I4_1,
            Code.Ldarg_1, Code.Ldc_I4_S, Code.And, Code.Shl, Code.And, Code.Ldc_I4_0, Code.Cgt_Un, Code.Ret };
        var source = method.Body.Instructions;
        if (source.Count != codes.Length || source.Where((instruction, i) => instruction.OpCode.Code != codes[i]).Any()
            || method.Body.Variables.Count != 1 || method.Body.HasExceptionHandlers
            || (sbyte)source[17].Operand != 31 || source[6].Operand != source[11]
            || source[4].Operand != method.Body.Variables[0] || source[11].Operand != method.Body.Variables[0]
            || method.Body.Variables[0].VariableType.FullName != "System.ReadOnlySpan`1<System.Int16>")
            throw new InvalidOperationException("Primitive widening IL changed");
        if (source[0].Operand is not FieldDefinition table || table.RVA == 0 || table.InitialValue.Length != 28
            || table.FieldType.Resolve() is not { ClassSize: 28, PackingSize: 2 }
            || source[1].Operand is not GenericInstanceMethod create || create.Name != "CreateSpan"
            || create.GenericArguments.Count != 1 || create.GenericArguments[0].MetadataType != MetadataType.Int16
            || create.ElementMethod.Resolve() is not { IsPublic: true, IsStatic: true, Name: "CreateSpan" } createDefinition
            || createDefinition.DeclaringType != owner || createDefinition.GenericParameters.Count != 1
            || createDefinition.Parameters.Count != 1 || createDefinition.Parameters[0].ParameterType.FullName != "System.RuntimeFieldHandle"
            || source[5].Operand is not MethodReference length || !PublicSpanMember(length, "get_Length", 0)
            || source[13].Operand is not MethodReference item || !PublicSpanMember(item, "get_Item", 1))
            throw new InvalidOperationException("Primitive widening public span/RVA dependencies changed");
        string path = Path.GetFullPath(args[1]);
        using var app = AssemblyDefinition.ReadAssembly(path, new ReaderParameters { InMemory = true });
        var module = app.MainModule;
        var program = module.GetType("PrimitiveWideningSubset.Program");
        bool oracle = args[0].EndsWith("-oracle", StringComparison.Ordinal);
        bool reflectionOnly = args[0].Contains("-reflection", StringComparison.Ordinal);
        if (!reflectionOnly) NarrowEnumArgumentFixture.Apply(module);
        var copy = program.Methods.Single(m => m.Name == "CopiedPredicate");
        if (oracle)
        {
            var dataType = new TypeDefinition("", "PrimitiveWideningTable451",
                TypeAttributes.NestedPrivate | TypeAttributes.Sealed | TypeAttributes.ExplicitLayout,
                module.ImportReference(typeof(ValueType))) { ClassSize = 28, PackingSize = 2 };
            program.NestedTypes.Add(dataType);
            var data = new FieldDefinition("PrimitiveWideningData451",
                FieldAttributes.Private | FieldAttributes.Static | FieldAttributes.InitOnly | FieldAttributes.HasFieldRVA,
                dataType) { InitialValue = table.InitialValue.ToArray() };
            program.Fields.Add(data);
            copy.Body = new MethodBody(copy) { InitLocals = method.Body.InitLocals, MaxStackSize = method.Body.MaxStackSize };
            foreach (var variable in method.Body.Variables)
                copy.Body.Variables.Add(new VariableDefinition(module.ImportReference(variable.VariableType)));
            var instructions = source.ToDictionary(instruction => instruction, _ => Instruction.Create(OpCodes.Nop));
            foreach (var instruction in source)
            {
                var clone = instructions[instruction];
                clone.OpCode = instruction.OpCode;
                clone.Operand = instruction.Operand switch
                {
                    null => null,
                    sbyte value => value,
                    Instruction branch => instructions[branch],
                    VariableDefinition variable => copy.Body.Variables[variable.Index],
                    FieldDefinition field when field == table => data,
                    MethodReference reference => module.ImportReference(reference),
                    _ => throw new InvalidOperationException("Unreviewed primitive widening copy operand"),
                };
                copy.Body.Instructions.Add(clone);
            }
        }
        var target = oracle ? copy : module.ImportReference(method);
        foreach (string route in reflectionOnly ? new[] { "Reflection" }
            : new[] { "Direct", "Pointer", "DirectStack", "PointerStack", "Reflection" })
        {
            if (route == "Reflection" && !oracle) continue;
            var stub = program.Methods.Single(m => m.Name == route);
            stub.Body = new MethodBody(stub);
            var il = stub.Body.GetILProcessor();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldarg_1);
            if (route is "Pointer" or "PointerStack")
            {
                il.Emit(OpCodes.Ldftn, target);
                var site = new CallSite(target.ReturnType);
                foreach (var parameter in target.Parameters)
                    site.Parameters.Add(new ParameterDefinition(parameter.ParameterType));
                il.Emit(OpCodes.Calli, site);
            }
            else il.Emit(OpCodes.Call, target);
            il.Emit(OpCodes.Ret);
        }
        module.EntryPoint = program.Methods.Single(m => m.Name == (reflectionOnly ? "RunReflectionArtifact" : "RunArtifact"));
        app.Write(path, new WriterParameters { Timestamp = 0, DeterministicMvid = true });
        Console.WriteLine("primitive-widening-sites=" + (reflectionOnly ? "reflection-only" : "direct,calli,stack,reflection")
            + (oracle ? ",local-RVA-and-public-span-copy,CLR-oracle" : ",exact-CoreLib-signature,native-only"));
        return true;
    }

    private static bool PublicSpanMember(MethodReference method, string name, int arity) =>
        method.Name == name && method.HasThis && !method.ExplicitThis
        && method.CallingConvention == MethodCallingConvention.Default && !method.HasGenericParameters && method.Parameters.Count == arity
        && method.DeclaringType.FullName == "System.ReadOnlySpan`1<System.Int16>"
        && (arity == 0 ? method.ReturnType.MetadataType == MetadataType.Int32
            : method.Parameters[0].ParameterType.MetadataType == MetadataType.Int32
                && method.ReturnType is RequiredModifierType { ModifierType.FullName: "System.Runtime.InteropServices.InAttribute",
                    ElementType: ByReferenceType { ElementType: GenericParameter { Position: 0, Type: GenericParameterType.Type } } })
        && method.Resolve() is { IsPublic: true, IsStatic: false, DeclaringType.FullName: "System.ReadOnlySpan`1" };
}

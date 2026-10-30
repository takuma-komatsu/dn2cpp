using Mono.Cecil;
using Mono.Cecil.Cil;

internal static class VectorExplicitFixture
{
    public static bool Run(string[] args)
    {
        if ((args.Length != 3 && args.Length != 4) || args[0] != "--vector-explicit") return false;
        using var core = AssemblyDefinition.ReadAssembly(args[2]);
        using var app = AssemblyDefinition.ReadAssembly(args[1], new ReaderParameters { InMemory = true });
        var module = app.MainModule;
        var vector = core.MainModule.GetType("System.Numerics.Vector`1");
        var span = core.MainModule.GetType("System.Span`1");
        var helper = module.GetType("VectorProbe.ExplicitReinterprets");
        MethodReference Operator(TypeReference source, TypeReference destination)
        {
            var definition = vector.Methods.Single(m => m.Name == "op_Explicit"
                && ((GenericInstanceType)m.ReturnType).GenericArguments[0].FullName == destination.FullName);
            var reference = module.ImportReference(definition);
            var owner = new GenericInstanceType(module.ImportReference(vector)); owner.GenericArguments.Add(source);
            reference.DeclaringType = owner;
            return reference;
        }
        int identity = 0, address = 0, fills = 0, explicitCalls = 0;
        foreach (var method in helper.Methods.Where(m => m.HasBody))
        foreach (var instruction in method.Body.Instructions)
        {
            if (instruction.Operand is not MethodReference target) continue;
            if (target.Name == "op_Explicit") explicitCalls++;
            if (target.DeclaringType.FullName != helper.FullName) continue;
            if (target.Name == "Identity")
            {
                var source = ((GenericInstanceMethod)target).GenericArguments[0];
                instruction.Operand = Operator(source, source); identity++;
            }
            else if (instruction.OpCode == OpCodes.Ldftn && target.Name == "Address")
            {
                instruction.Operand = Operator(module.TypeSystem.UInt32, module.TypeSystem.Byte); address++;
            }
            else if (instruction.OpCode == OpCodes.Ldftn && target.Name.StartsWith("Fill", StringComparison.Ordinal))
            {
                var spanType = (GenericInstanceType)((ByReferenceType)target.Parameters[0].ParameterType).ElementType;
                var reference = module.ImportReference(span.Methods.Single(m => m.Name == "Fill" && m.Parameters.Count == 1));
                var owner = new GenericInstanceType(module.ImportReference(span)); owner.GenericArguments.Add(spanType.GenericArguments[0]);
                reference.DeclaringType = owner;
                instruction.Operand = reference; fills++;
            }
        }
        if (identity != 12 || address != 1 || fills != 13 || explicitCalls != 136)
            throw new Exception($"vector fixture unexpected routes {identity}/{address}/{fills}/{explicitCalls}");
        if (args.Length == 4 && args[3] == "operators-only")
        {
            var run = helper.Methods.Single(m => m.Name == "Run");
            var fillCall = run.Body.Instructions.Single(i => i.Operand is MethodReference m && m.Name == "RetainedFills");
            fillCall.OpCode = OpCodes.Nop; fillCall.Operand = null;
            Console.WriteLine("vector explicit operator-only fixture; retained Fill validated separately");
        }
        else if (args.Length == 4)
        {
            string mode = args[3];
            bool addressMode = mode.StartsWith("address-", StringComparison.Ordinal);
            if (addressMode) mode = mode[8..];
            var instruction = addressMode
                ? helper.Methods.SelectMany(m => m.HasBody ? m.Body.Instructions.AsEnumerable() : Enumerable.Empty<Instruction>()).Single(i => i.OpCode == OpCodes.Ldftn && i.Operand is MethodReference m && m.Name == "op_Explicit")
                : helper.Methods.Single(m => m.Name == "Source0").Body.Instructions.First(i => i.Operand is MethodReference m && m.Name == "op_Explicit");
            var old = (MethodReference)instruction.Operand;
            var replacement = new MethodReference(old.Name, old.ReturnType, old.DeclaringType) { HasThis = old.HasThis, CallingConvention = old.CallingConvention };
            foreach (var parameter in old.Parameters) replacement.Parameters.Add(new ParameterDefinition(parameter.ParameterType));
            var vectorDefinition = module.ImportReference(vector);
            GenericInstanceType VectorOf(TypeReference element)
            {
                var result = new GenericInstanceType(vectorDefinition); result.GenericArguments.Add(element); return result;
            }
            TypeReference? unsupported = mode switch
            {
                "bool" => module.TypeSystem.Boolean, "char" => module.TypeSystem.Char,
                "decimal" => module.ImportReference(core.MainModule.GetType("System.Decimal")),
                "custom" => helper.NestedTypes.Single(t => t.Name == "Three"),
                "open" => vectorDefinition.GenericParameters[0],
                _ => null
            };
            if (mode == "enum")
            {
                var enumeration = new TypeDefinition("VectorProbe", "UnsupportedElement", TypeAttributes.Public | TypeAttributes.Sealed,
                    module.ImportReference(core.MainModule.GetType("System.Enum")));
                enumeration.Fields.Add(new FieldDefinition("value__", FieldAttributes.Public | FieldAttributes.SpecialName | FieldAttributes.RTSpecialName, module.TypeSystem.Int32));
                module.Types.Add(enumeration); unsupported = enumeration;
            }
            if (unsupported is not null) replacement.DeclaringType = VectorOf(unsupported);
            else switch (mode)
            {
                case "instance": replacement.HasThis = true; break;
                case "vararg": replacement.CallingConvention = MethodCallingConvention.VarArg; break;
                case "generic": replacement.GenericParameters.Add(new GenericParameter("T", replacement)); break;
                case "byref-input": replacement.Parameters[0].ParameterType = new ByReferenceType(replacement.Parameters[0].ParameterType); break;
                case "byref-result": replacement.ReturnType = new ByReferenceType(replacement.ReturnType); break;
                case "extra": replacement.Parameters.Add(new ParameterDefinition(module.TypeSystem.Byte)); break;
                case "modopt": replacement.Parameters[0].ParameterType = new OptionalModifierType(module.TypeSystem.Object, replacement.Parameters[0].ParameterType); break;
                case "wrong-source": replacement.Parameters[0].ParameterType = VectorOf(module.TypeSystem.SByte); break;
                case "wrong-result": replacement.ReturnType = VectorOf(module.TypeSystem.Boolean); break;
                case "fixed-owner":
                    var fixedOwner = new GenericInstanceType(module.ImportReference(core.MainModule.GetType("System.Runtime.Intrinsics.Vector128`1")));
                    fixedOwner.GenericArguments.Add(module.TypeSystem.Byte); replacement.DeclaringType = fixedOwner; break;
                default: throw new ArgumentException("unknown vector negative " + mode);
            }
            instruction.Operand = replacement;
            var run = helper.Methods.Single(m => m.Name == "Run");
            var fillCall = run.Body.Instructions.Single(i => i.Operand is MethodReference m && m.Name == "RetainedFills");
            fillCall.OpCode = OpCodes.Nop; fillCall.Operand = null;
            Console.WriteLine("vector explicit negative " + args[3]);
        }
        app.Write(args[1]);
        Console.WriteLine($"vector explicit fixture identity={identity} address={address} retained-fill={fills} direct={explicitCalls}");
        return true;
    }
}

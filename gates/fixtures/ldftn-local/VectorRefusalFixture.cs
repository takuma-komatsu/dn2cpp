using System.Globalization;
using Mono.Cecil;
using Mono.Cecil.Cil;

// Malformed references must refuse instead of binding a nearby lowering. The
// alignment-valid control admits numeric default bodies for emission checks.
internal static class VectorRefusalFixture
{
    public static bool Run(string[] args)
    {
        if (args.Length != 4 || args[0] != "--vector-refusal") return false;
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        string output = Path.GetFullPath(args[1]);
        if (File.Exists(output)) throw new InvalidOperationException("fixture output must be new");
        using var core = AssemblyDefinition.ReadAssembly(args[2]);
        using var app = AssemblyDefinition.CreateAssembly(new AssemblyNameDefinition("VectorRefusal", new Version(1, 0)), "VectorRefusal", ModuleKind.Console);
        var module = app.MainModule;
        var host = new TypeDefinition("Fixture", "Program", TypeAttributes.Public | TypeAttributes.Abstract | TypeAttributes.Sealed, module.TypeSystem.Object);
        module.Types.Add(host);
        var main = new MethodDefinition("Main", MethodAttributes.Public | MethodAttributes.Static, module.TypeSystem.Void);
        host.Methods.Add(main); app.EntryPoint = main;
        main.Body.InitLocals = true;
        var il = main.Body.GetILProcessor();

        TypeDefinition Type(string name) => core.MainModule.GetType(name) ?? throw new InvalidOperationException("missing CoreLib type " + name);
        GenericInstanceType Closed(string name, TypeReference element)
        {
            var type = new GenericInstanceType(module.ImportReference(Type(name))); type.GenericArguments.Add(element);
            return type;
        }
        MethodReference Import(string owner, Func<MethodDefinition, bool> match) => module.ImportReference(Type(owner).Methods.Single(match));
        bool Lanes(TypeReference vector, MetadataType element) =>
            vector is GenericInstanceType closed && closed.GenericArguments[0].MetadataType == element;
        TypeReference Modified(TypeReference type) => new OptionalModifierType(module.TypeSystem.Object, type);
        GenericInstanceMethod Specialize(MethodReference target, TypeReference argument)
        {
            var specification = new GenericInstanceMethod(target); specification.GenericArguments.Add(argument);
            return specification;
        }
        // Operands are zeroed locals; an instance receiver is passed by address.
        void Call(MethodReference target, params TypeReference[] operands)
        {
            for (int index = 0; index < operands.Length; index++)
            {
                var local = new VariableDefinition(operands[index]); main.Body.Variables.Add(local);
                il.Emit(index == 0 && target.HasThis ? OpCodes.Ldloca : OpCodes.Ldloc, local);
            }
            il.Emit(OpCodes.Call, target);
            if (target.ReturnType.MetadataType != MetadataType.Void) il.Emit(OpCodes.Pop);
        }

        var single = module.TypeSystem.Single;
        var single128 = Closed("System.Runtime.Intrinsics.Vector128`1", single);
        switch (args[3])
        {
            case "explicit":
            {
                var target = Import("System.Numerics.Vector`1", m => m.Name == "op_Explicit" && Lanes(m.ReturnType, MetadataType.Byte));
                var source = Closed("System.Numerics.Vector`1", module.TypeSystem.UInt32);
                target.DeclaringType = source;
                target.Parameters[0].ParameterType = Modified(target.Parameters[0].ParameterType);
                Call(target, source);
                break;
            }
            case "block":
            {
                var target = new MethodReference("Create", Closed("System.Runtime.Intrinsics.Vector512`1", module.TypeSystem.Byte),
                    module.ImportReference(Type("System.Runtime.Intrinsics.Vector512")));
                target.GenericParameters.Add(new GenericParameter("T", target));
                var source = Closed("System.Runtime.Intrinsics.Vector128`1", module.TypeSystem.Int32);
                target.Parameters.Add(new ParameterDefinition(source));
                Call(Specialize(target, module.TypeSystem.Byte), source);
                break;
            }
            case "value":
            {
                var target = Import("System.Numerics.Vector`1", m => m.Name == "Equals" && m.Parameters.Count == 1
                    && m.Parameters[0].ParameterType.MetadataType != MetadataType.Object);
                var owner = Closed("System.Numerics.Vector`1", module.TypeSystem.Double);
                target.DeclaringType = owner;
                target.Parameters[0].ParameterType = Modified(target.Parameters[0].ParameterType);
                Call(target, owner, owner);
                break;
            }
            case "unsupported":
            {
                var target = Import("System.ThrowHelper", m => m.Name == "ThrowForUnsupportedNumericsVectorBaseType");
                target.DeclaringType = Modified(target.DeclaringType);
                Call(Specialize(target, module.ImportReference(Type("System.Decimal"))));
                break;
            }
            case "element":
            {
                var target = Import("System.Numerics.Vector", m => m.Name == "GetElement" && !m.HasGenericParameters
                    && m.Parameters[0].ParameterType.FullName == "System.Numerics.Vector2");
                var vector = target.Parameters[0].ParameterType;
                target.Parameters[0].ParameterType = Modified(vector);
                Call(target, vector, module.TypeSystem.Int32);
                break;
            }
            case "estimate":
            case "log":
            {
                string name = args[3] == "log" ? "Log" : "MultiplyAddEstimate";
                var target = Import("System.Runtime.Intrinsics.Vector128", m => m.Name == name && !m.HasGenericParameters
                    && Lanes(m.ReturnType, MetadataType.Single));
                target.Parameters[0].ParameterType = Modified(target.Parameters[0].ParameterType);
                Call(target, target.Parameters.Select(_ => (TypeReference)single128).ToArray());
                break;
            }
            case "lerp":
            {
                var target = Import("System.Runtime.Intrinsics.Vector128", m => m.Name == "Lerp" && !m.HasGenericParameters
                    && Lanes(m.ReturnType, MetadataType.Single));
                Call(Specialize(target, single), single128, single128, single128);
                break;
            }
            case "magnitude":
            {
                // The unmodified call resolves first, so the refusal cannot hide behind a cached resolution.
                foreach (bool modified in new[] { false, true })
                {
                    var target = Import("System.Runtime.Intrinsics.Vector128", m => m.Name == "MaxMagnitude" && m.HasGenericParameters);
                    if (modified) target.Parameters[0].ParameterType = Modified(target.Parameters[0].ParameterType);
                    Call(Specialize(target, single), single128, single128);
                }
                break;
            }
            case "alignment":
            {
                var element = module.TypeSystem.Char;
                var declaration = module.ImportReference(Type("System.Runtime.Intrinsics.ISimdVector`2"));
                var contract = new GenericInstanceType(declaration);
                contract.GenericArguments.Add(Closed("System.Runtime.Intrinsics.Vector128`1", element)); contract.GenericArguments.Add(element);
                var target = new MethodReference("LoadAligned", declaration.GenericParameters[0], contract);
                target.Parameters.Add(new ParameterDefinition(new PointerType(declaration.GenericParameters[1])));
                Call(target, new PointerType(element));
                break;
            }
            case "alignment-valid":
            {
                // Admission/emission only: the zero pointers are not executable
                // runtime probes. Each default body must retain its guard.
                var unsupportedEnum = new TypeDefinition("Fixture", "UnsupportedVectorEnum",
                    TypeAttributes.Public | TypeAttributes.Sealed, module.ImportReference(Type("System.Enum")));
                unsupportedEnum.Fields.Add(new FieldDefinition("value__",
                    FieldAttributes.Public | FieldAttributes.SpecialName | FieldAttributes.RTSpecialName, module.TypeSystem.Int32));
                module.Types.Add(unsupportedEnum);
                foreach (string owner in new[] {
                    "System.Runtime.Intrinsics.Vector64`1", "System.Runtime.Intrinsics.Vector128`1",
                    "System.Runtime.Intrinsics.Vector256`1", "System.Runtime.Intrinsics.Vector512`1",
                    "System.Numerics.Vector`1" })
                {
                    var element = module.TypeSystem.Int32;
                    var declaration = module.ImportReference(Type("System.Runtime.Intrinsics.ISimdVector`2"));
                    var contract = new GenericInstanceType(declaration);
                    contract.GenericArguments.Add(Closed(owner, element));
                    contract.GenericArguments.Add(element);
                    var target = new MethodReference("LoadAligned", declaration.GenericParameters[0], contract);
                    target.Parameters.Add(new ParameterDefinition(new PointerType(declaration.GenericParameters[1])));
                    Call(target, new PointerType(element));
                    // The two-argument contract's support getter must dispatch on
                    // its concrete vector, independently of the native backend.
                    foreach (var lane in new TypeReference[] { module.TypeSystem.Int32, module.TypeSystem.Char, unsupportedEnum })
                    {
                        var vector = Closed(owner, lane);
                        var supportContract = new GenericInstanceType(declaration);
                        supportContract.GenericArguments.Add(vector);
                        supportContract.GenericArguments.Add(lane);
                        var getter = Import("System.Runtime.Intrinsics.ISimdVector`2", m => m.Name == "get_IsSupported");
                        getter.DeclaringType = supportContract;
                        string family = owner.Substring(owner.LastIndexOf('.') + 1).Replace("`1", "");
                        var helper = new MethodDefinition("ContractSupport_" + family + "_" + lane.Name,
                            MethodAttributes.Private | MethodAttributes.Static, module.TypeSystem.Boolean);
                        host.Methods.Add(helper);
                        var supportIl = helper.Body.GetILProcessor();
                        supportIl.Emit(OpCodes.Constrained, vector);
                        supportIl.Emit(OpCodes.Call, getter);
                        supportIl.Emit(OpCodes.Ret);
                        Call(helper);
                    }
                }
                break;
            }
            case "alignment-helper":
            {
                var target = new MethodReference("ThrowAccessViolationException", module.TypeSystem.Void,
                    module.ImportReference(Type("System.ThrowHelper"))) { CallingConvention = MethodCallingConvention.VarArg };
                Call(target);
                break;
            }
            case "as":
            {
                var target = Import("System.Numerics.Vector", m => m.Name == "As" && m.GenericParameters.Count == 2);
                target.Parameters[0].ParameterType = Modified(target.Parameters[0].ParameterType);
                var specification = new GenericInstanceMethod(target);
                specification.GenericArguments.Add(module.TypeSystem.Int32);
                specification.GenericArguments.Add(single);
                Call(specification, Closed("System.Numerics.Vector`1", module.TypeSystem.Int32));
                break;
            }
            case "reduction":
            {
                var target = Import("System.Runtime.Intrinsics.Vector128", m => m.Name == "All" && !m.HasGenericParameters
                    && m.Parameters[0].ParameterType.FullName == "System.Numerics.Vector2");
                var vector = target.Parameters[0].ParameterType;
                target.Parameters[0].ParameterType = Modified(vector);
                Call(target, vector, single);
                break;
            }
            case "numerics-alignment":
            {
                var target = Import("System.Numerics.Vector", m => m.Name == "get_Alignment");
                target.CallingConvention = MethodCallingConvention.VarArg;
                Call(target);
                break;
            }
            default: throw new ArgumentException("unknown vector refusal mode " + args[3]);
        }
        il.Emit(OpCodes.Ret);
        app.Write(output, new WriterParameters { Timestamp = 0, DeterministicMvid = true });
        Console.WriteLine("vector-refusal:" + args[3]);
        return true;
    }
}

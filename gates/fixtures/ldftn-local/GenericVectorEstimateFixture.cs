using System.Globalization;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using Mono.Cecil;
using Mono.Cecil.Cil;
using AssemblyDefinition = Mono.Cecil.AssemblyDefinition;
using MethodDefinition = Mono.Cecil.MethodDefinition;
using TypeDefinition = Mono.Cecil.TypeDefinition;
using FieldDefinition = Mono.Cecil.FieldDefinition;
using MethodReference = Mono.Cecil.MethodReference;
using TypeReference = Mono.Cecil.TypeReference;
using GenericParameter = Mono.Cecil.GenericParameter;
using ModuleDefinition = Mono.Cecil.ModuleDefinition;

internal static class GenericVectorEstimateFixture
{
    internal static readonly string[] Elements = { "decimal", "enum", "struct", "refs", "int128", "uint128" };
    internal static readonly string[] Mutations = { "instance", "vararg", "arity", "nongeneric", "concrete", "typevar", "methodvar-one", "extra", "missing", "byref-result", "modopt-0", "modreq-1", "pointer-2", "width", "fixed", "modopt-owner", "byref-owner", "generic-owner", "foreign-owner", "zero", "two", "trailing-spec", "trailing-reference", "reference", "primitive", "double", "byreflike", "nullable", "byref-argument", "pointer-argument", "modopt-argument", "bare", "bare-concrete" };

    public static bool Run(string[] args)
    {
        if (args.Length == 1 && args[0] == "--fixed-generic-vector-estimate-oracle")
        {
            FixedOracle();
            return true;
        }
        if (args.Length == 4 && args[0] == "--fixed-generic-vector-estimate-fixture")
        {
            if (args[3] != "all-runtime") throw new ArgumentException("expected all-runtime");
            Build(args[1], args[2], new[] { 64, 128, 256, 512 }.SelectMany(width => Elements.SelectMany(element =>
                new[] { "direct", "address", "calli", "bridge" }.Select(route => (element, route, "valid-runtime-" + width)))));
            Console.WriteLine("fixed-generic-vector-estimate-fixture:all-runtime");
            return true;
        }
        if (args.Length == 1 && args[0] == "--generic-vector-estimate-oracle")
        {
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
            Oracle<decimal>(); Oracle<OracleEnum>(); Oracle<OraclePlain>(); Oracle<OracleReferences>(); Oracle<Int128>(); Oracle<UInt128>();
            return true;
        }
        if (args.Length != 4 || args[0] != "--generic-vector-estimate-fixture") return false;
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        if (args[3] == "all-runtime")
        {
            Build(args[1], args[2], Elements.SelectMany(element => new[] { "direct", "address", "calli" }.Select(route => (element, route, "valid-runtime"))));
            Console.WriteLine("generic-vector-estimate-fixture:all-runtime");
            return true;
        }
        var mode = args[3].Split(':');
        if (mode.Length != 3 || !Elements.Contains(mode[0]) || mode[1] is not ("direct" or "address" or "calli"))
            throw new ArgumentException("expected element:direct/address/calli:mutation");
        Build(args[1], args[2], new[] { (mode[0], mode[1], mode[2]) });
        Console.WriteLine("generic-vector-estimate-fixture:" + args[3]);
        return true;
    }

    internal static void Build(string output, string corePath, IEnumerable<(string Element, string Route, string Mutation)> cases, int width = 0)
    {
        output = Path.GetFullPath(output);
        if (File.Exists(output)) throw new InvalidOperationException("fixture output must be new");
        using var core = AssemblyDefinition.ReadAssembly(corePath);
        using var assembly = AssemblyDefinition.CreateAssembly(new AssemblyNameDefinition("GenericVectorEstimate", new Version(1, 0)), "GenericVectorEstimate", ModuleKind.Console);
        var module = assembly.MainModule;
        var host = new TypeDefinition("Fixture", "Program", TypeAttributes.Public | TypeAttributes.Abstract | TypeAttributes.Sealed, module.TypeSystem.Object);
        module.Types.Add(host);
        var plain = new TypeDefinition("Fixture", "Plain", TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.SequentialLayout, module.ImportReference(core.MainModule.GetType("System.ValueType")));
        plain.Fields.Add(new FieldDefinition("Value", FieldAttributes.Public, module.TypeSystem.Int32)); module.Types.Add(plain);
        var refs = new TypeDefinition("Fixture", "References", TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.SequentialLayout, plain.BaseType);
        refs.Fields.Add(new FieldDefinition("Value", FieldAttributes.Public, module.TypeSystem.Object)); module.Types.Add(refs);
        var enumeration = new TypeDefinition("Fixture", "Enumeration", TypeAttributes.Public | TypeAttributes.Sealed, module.ImportReference(core.MainModule.GetType("System.Enum")));
        enumeration.Fields.Add(new FieldDefinition("value__", FieldAttributes.Public | FieldAttributes.SpecialName | FieldAttributes.RTSpecialName, module.TypeSystem.Int32)); module.Types.Add(enumeration);
        TypeReference Element(string name) => name switch
        {
            "enum" => enumeration, "struct" => plain, "refs" => refs,
            "decimal" => module.ImportReference(core.MainModule.GetType("System.Decimal")),
            "int128" => module.ImportReference(core.MainModule.GetType("System.Int128")),
            "uint128" => module.ImportReference(core.MainModule.GetType("System.UInt128")),
            _ => throw new ArgumentException(name),
        };
        var byrefLike = new TypeDefinition("Fixture", "ByRefLike", TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.SequentialLayout, plain.BaseType);
        byrefLike.Fields.Add(new FieldDefinition("Value", FieldAttributes.Public, module.TypeSystem.Int32));
        byrefLike.CustomAttributes.Add(new Mono.Cecil.CustomAttribute(module.ImportReference(core.MainModule.GetType("System.Runtime.CompilerServices.IsByRefLikeAttribute").Methods.Single(m => m.IsConstructor && m.Parameters.Count == 0))));
        module.Types.Add(byrefLike);
        TypeReference Vector(TypeReference element, int selectedWidth, bool wide = false)
        {
            var vector = new GenericInstanceType(module.ImportReference(core.MainModule.GetType(wide ? (selectedWidth == 128 ? "System.Runtime.Intrinsics.Vector64`1" : "System.Runtime.Intrinsics.Vector128`1") : selectedWidth == 0 ? "System.Numerics.Vector`1" : "System.Runtime.Intrinsics.Vector" + selectedWidth + "`1")));
            vector.GenericArguments.Add(element); return vector;
        }
        var entry = new MethodDefinition("Main", MethodAttributes.Public | MethodAttributes.Static, module.TypeSystem.Void);
        host.Methods.Add(entry); assembly.EntryPoint = entry;
        var entryIl = entry.Body.GetILProcessor();
        var invariant = module.ImportReference(typeof(CultureInfo).GetProperty(nameof(CultureInfo.InvariantCulture))!.GetMethod!);
        foreach (var property in new[] { nameof(CultureInfo.CurrentCulture), nameof(CultureInfo.CurrentUICulture) })
        {
            entryIl.Emit(OpCodes.Call, invariant);
            entryIl.Emit(OpCodes.Call, module.ImportReference(typeof(CultureInfo).GetProperty(property)!.SetMethod!));
        }
        int serial = 0;
        var patches = new List<(string Caller, bool Specification)>();
        foreach (var item in cases)
        {
            int selectedWidth = item.Mutation.StartsWith("valid-runtime-", StringComparison.Ordinal) ? int.Parse(item.Mutation.Substring(14), CultureInfo.InvariantCulture) : width;
            var definition = core.MainModule.GetType(selectedWidth == 0 ? "System.Numerics.Vector" : "System.Runtime.Intrinsics.Vector" + selectedWidth).Methods.Single(m => m.Name == "MultiplyAddEstimate" && m.HasGenericParameters);
            var element = Element(item.Element);
            var caller = new MethodDefinition("Case" + serial++ + "_" + item.Element + "_" + item.Route + "_" + item.Mutation.Replace('-', '_'), MethodAttributes.Public | MethodAttributes.Static, module.TypeSystem.Void);
            host.Methods.Add(caller);
            var target = module.ImportReference(definition);
            var vector = Vector(element, selectedWidth);
            var methodVariable = new GenericParameter("TCaller", caller);
            caller.GenericParameters.Add(methodVariable);
            var typeCarrier = new TypeReference("Fixture", "Carrier`1", module, module);
            var typeVariable = new GenericParameter("TType", typeCarrier); typeCarrier.GenericParameters.Add(typeVariable);
            TypeReference argument = element;
            string mutation = item.Mutation.StartsWith("valid-runtime-", StringComparison.Ordinal) ? "valid-runtime" : item.Mutation;
            switch (mutation)
            {
                case "valid": case "valid-runtime": break;
                case "valid-mvar": argument = methodVariable; break;
                case "valid-var": argument = typeVariable; break;
                case "instance": target.HasThis = true; break;
                case "vararg": target.CallingConvention = MethodCallingConvention.VarArg; break;
                case "arity": target.GenericParameters.Add(new GenericParameter("U", target)); break;
                case "nongeneric": target.GenericParameters.Clear(); target.CallingConvention = MethodCallingConvention.Default; break;
                case "concrete": case "bare-concrete":
                    target.ReturnType = vector; foreach (var p in target.Parameters) p.ParameterType = vector; break;
                case "typevar":
                    target.ReturnType = Vector(typeVariable, selectedWidth); foreach (var p in target.Parameters) p.ParameterType = target.ReturnType; break;
                case "methodvar-one":
                    var carrier = new MethodReference("Carrier", module.TypeSystem.Void, target.DeclaringType);
                    carrier.GenericParameters.Add(new GenericParameter("T", carrier));
                    var wrong = new GenericParameter("U", carrier); carrier.GenericParameters.Add(wrong);
                    target.ReturnType = Vector(wrong, selectedWidth); foreach (var p in target.Parameters) p.ParameterType = target.ReturnType; break;
                case "trailing-reference": target.Parameters.Add(new ParameterDefinition(module.TypeSystem.Double)); break;
                case "extra": target.Parameters.Add(new ParameterDefinition(module.TypeSystem.Int32)); break;
                case "missing": target.Parameters.RemoveAt(2); break;
                case "byref-result": target.ReturnType = new ByReferenceType(target.ReturnType); break;
                case "modopt-0": target.Parameters[0].ParameterType = new OptionalModifierType(module.TypeSystem.Object, target.Parameters[0].ParameterType); break;
                case "modreq-1": target.Parameters[1].ParameterType = new RequiredModifierType(module.TypeSystem.Object, target.Parameters[1].ParameterType); break;
                case "pointer-2": target.Parameters[2].ParameterType = new PointerType(target.Parameters[2].ParameterType); break;
                case "fixed": target = module.ImportReference(core.MainModule.GetType("System.Runtime.Intrinsics.Vector128").Methods.Single(m => m.Name == "MultiplyAddEstimate" && m.HasGenericParameters)); break;
                case "width": target.ReturnType = Vector(target.GenericParameters[0], selectedWidth, true); break;
                case "modopt-owner": target.DeclaringType = new OptionalModifierType(module.TypeSystem.Object, target.DeclaringType); break;
                case "byref-owner": target.DeclaringType = new ByReferenceType(target.DeclaringType); break;
                case "generic-owner":
                    var genericOwner = new GenericInstanceType(target.DeclaringType); genericOwner.GenericArguments.Add(element); target.DeclaringType = genericOwner; break;
                case "foreign-owner":
                    var foreign = new AssemblyNameReference("Foreign.Vector.Owner", new Version(1, 0)); module.AssemblyReferences.Add(foreign);
                    target.DeclaringType = new TypeReference("System.Numerics", "Vector", module, foreign); break;
                case "reference": argument = module.TypeSystem.Object; break;
                case "primitive": argument = module.TypeSystem.Single; break;
                case "double": argument = module.TypeSystem.Double; break;
                case "byreflike": argument = byrefLike; break;
                case "nullable":
                    var nullable = new GenericInstanceType(module.ImportReference(core.MainModule.GetType("System.Nullable`1"))); nullable.GenericArguments.Add(element); argument = nullable; break;
                case "byref-argument": argument = new ByReferenceType(element); break;
                case "pointer-argument": argument = new PointerType(element); break;
                case "modopt-argument": argument = new OptionalModifierType(module.TypeSystem.Object, element); break;
                case "zero": case "two": case "trailing-spec": case "bare": break;
                default: throw new ArgumentException("unknown generic estimate mutation: " + mutation);
            }
            MethodReference call = target;
            if (mutation is not ("bare" or "bare-concrete"))
            {
                var specification = new GenericInstanceMethod(target);
                if (mutation != "zero") specification.GenericArguments.Add(argument);
                if (mutation == "two") specification.GenericArguments.Add(element);
                if (mutation == "trailing-spec") specification.GenericArguments.Add(module.TypeSystem.Double);
                call = specification;
            }
            if (mutation == "trailing-spec") patches.Add((caller.Name, true));
            if (mutation == "trailing-reference") patches.Add((caller.Name, false));
            var il = caller.Body.GetILProcessor();
            if (mutation == "valid-runtime")
            {
                if (item.Route == "bridge")
                {
                    var bridgeDefinition = core.MainModule.GetType("System.Runtime.Intrinsics.Vector" + selectedWidth + "`1").Methods.Single(m => m.Name.EndsWith(".MultiplyAddEstimate", StringComparison.Ordinal));
                    var imported = module.ImportReference(bridgeDefinition);
                    var bridge = new MethodReference(imported.Name, imported.ReturnType, vector) { HasThis = false };
                    foreach (var parameter in imported.Parameters) bridge.Parameters.Add(new ParameterDefinition(parameter.ParameterType));
                    call = bridge;
                }
                EmitRuntime(module, host, caller, call, vector, item.Route, serial);
                var closedRuntime = new GenericInstanceMethod(caller); closedRuntime.GenericArguments.Add(element);
                entry.Body.GetILProcessor().Emit(OpCodes.Call, closedRuntime);
                continue;
            }
            if (item.Route != "address")
            {
                caller.Body.InitLocals = true;
                var local = new VariableDefinition(vector); caller.Body.Variables.Add(local);
                il.Emit(OpCodes.Ldloca, local); il.Emit(OpCodes.Initobj, vector);
                if (target.HasThis && item.Route == "direct") il.Emit(OpCodes.Ldnull);
                for (int i = 0; i < (mutation == "missing" && item.Route == "direct" ? 2 : 3); i++) il.Emit(OpCodes.Ldloc, local);
                if (mutation == "extra" && item.Route == "direct") il.Emit(OpCodes.Ldc_I4_0);
            }
            if (item.Route == "direct") il.Emit(OpCodes.Call, call);
            else
            {
                il.Emit(OpCodes.Ldftn, call);
                if (item.Route == "calli")
                {
                    var site = new CallSite(vector); for (int i = 0; i < 3; i++) site.Parameters.Add(new ParameterDefinition(vector));
                    il.Emit(OpCodes.Calli, site);
                }
            }
            il.Emit(OpCodes.Pop); il.Emit(OpCodes.Ret);
            var closedCaller = new GenericInstanceMethod(caller); closedCaller.GenericArguments.Add(element);
            entry.Body.GetILProcessor().Emit(OpCodes.Call, closedCaller);
        }
        entry.Body.GetILProcessor().Emit(OpCodes.Ret);
        assembly.Write(output, new WriterParameters { Timestamp = 0, DeterministicMvid = true });
        if (patches.Count != 0)
        {
            byte[] bytes = File.ReadAllBytes(output);
            using var pe = new PEReader(new MemoryStream(bytes, writable: false));
            var reader = pe.GetMetadataReader();
            using var serialized = AssemblyDefinition.ReadAssembly(output);
            var changed = new HashSet<int>();
            foreach (var patch in patches)
            {
                var serializedCaller = serialized.MainModule.GetType("Fixture.Program").Methods.Single(m => m.Name == patch.Caller);
                var serializedTarget = serializedCaller.Body.Instructions.Select(i => i.Operand).OfType<MethodReference>().Single(m => m.Name == "MultiplyAddEstimate");
                if (!patch.Specification) serializedTarget = ((GenericInstanceMethod)serializedTarget).ElementMethod;
                var token = MetadataTokens.EntityHandle(serializedTarget.MetadataToken.ToInt32());
                var signature = patch.Specification
                    ? reader.GetMethodSpecification((MethodSpecificationHandle)token).Signature
                    : reader.GetMemberReference((MemberReferenceHandle)token).Signature;
                int offset = pe.PEHeaders.MetadataStartOffset + reader.GetHeapMetadataOffset(HeapIndex.Blob) + MetadataTokens.GetHeapOffset(signature);
                int prefix = bytes[offset] < 0x80 ? 1 : bytes[offset] < 0xc0 ? 2 : 4;
                int countOffset = offset + prefix + (patch.Specification ? 1 : 2);
                if (!changed.Add(countOffset)) continue;
                if (bytes[countOffset] != (patch.Specification ? 2 : 4)) throw new InvalidOperationException("unexpected raw count");
                bytes[countOffset] = (byte)(patch.Specification ? 1 : 3);
            }
            File.WriteAllBytes(output, bytes);
        }
        File.WriteAllText(Path.ChangeExtension(output, ".runtimeconfig.json"), "{\"runtimeOptions\":{\"tfm\":\"net10.0\",\"framework\":{\"name\":\"Microsoft.NETCore.App\",\"version\":\"10.0.0\"}}}\n");
    }
    private static void EmitRuntime(ModuleDefinition module, TypeDefinition host, MethodDefinition caller,
        MethodReference target, TypeReference vector, string route, int serial)
    {
        var counter = new FieldDefinition("Operands" + serial, FieldAttributes.Private | FieldAttributes.Static, module.TypeSystem.Int32);
        host.Fields.Add(counter);
        var factories = new List<MethodDefinition>();
        for (int i = 1; i <= 3; i++)
        {
            var factory = new MethodDefinition("Operand" + serial + "_" + i, MethodAttributes.Private | MethodAttributes.Static, vector);
            host.Methods.Add(factory); factories.Add(factory);
            var local = new VariableDefinition(vector); factory.Body.Variables.Add(local); factory.Body.InitLocals = true;
            var body = factory.Body.GetILProcessor();
            body.Emit(OpCodes.Ldsfld, counter); body.Emit(OpCodes.Ldc_I4, 10); body.Emit(OpCodes.Mul);
            body.Emit(OpCodes.Ldc_I4, i); body.Emit(OpCodes.Add); body.Emit(OpCodes.Stsfld, counter);
            body.Emit(OpCodes.Ldloca, local); body.Emit(OpCodes.Initobj, vector); body.Emit(OpCodes.Ldloc, local); body.Emit(OpCodes.Ret);
        }
        var writeString = module.ImportReference(typeof(Console).GetMethod(nameof(Console.WriteLine), new[] { typeof(string) })!);
        var writeInt = module.ImportReference(typeof(Console).GetMethod(nameof(Console.WriteLine), new[] { typeof(int) })!);
        var getType = module.ImportReference(typeof(object).GetMethod(nameof(object.GetType))!);
        var getName = module.ImportReference(typeof(Type).GetProperty(nameof(Type.FullName))!.GetMethod!);
        var getMessage = module.ImportReference(typeof(Exception).GetProperty(nameof(Exception.Message))!.GetMethod!);
        var exception = new VariableDefinition(module.ImportReference(typeof(Exception))); caller.Body.Variables.Add(exception);
        var pointer = new VariableDefinition(module.TypeSystem.IntPtr); caller.Body.Variables.Add(pointer);
        caller.Body.InitLocals = true;
        var il = caller.Body.GetILProcessor();
        il.Emit(OpCodes.Ldc_I4_0); il.Emit(OpCodes.Stsfld, counter);
        var start = Instruction.Create(OpCodes.Nop); il.Append(start);
        if (route == "address") { il.Emit(OpCodes.Ldftn, target); il.Emit(OpCodes.Stloc, pointer); }
        foreach (var factory in factories) il.Emit(OpCodes.Call, factory);
        if (route is "direct" or "bridge") il.Emit(OpCodes.Call, target);
        else
        {
            var site = new CallSite(vector); for (int i = 0; i < 3; i++) site.Parameters.Add(new ParameterDefinition(vector));
            if (route == "address") il.Emit(OpCodes.Ldloc, pointer);
            else il.Emit(OpCodes.Ldftn, target);
            il.Emit(OpCodes.Calli, site);
        }
        il.Emit(OpCodes.Pop); il.Emit(OpCodes.Ldstr, "unexpected-return"); il.Emit(OpCodes.Call, writeString);
        var done = Instruction.Create(OpCodes.Ret); il.Emit(OpCodes.Leave, done);
        var handler = Instruction.Create(OpCodes.Stloc, exception); il.Append(handler);
        il.Emit(OpCodes.Ldsfld, counter); il.Emit(OpCodes.Call, writeInt);
        il.Emit(OpCodes.Ldloc, exception); il.Emit(OpCodes.Callvirt, getType); il.Emit(OpCodes.Callvirt, getName); il.Emit(OpCodes.Call, writeString);
        il.Emit(OpCodes.Ldloc, exception); il.Emit(OpCodes.Callvirt, getMessage); il.Emit(OpCodes.Call, writeString);
        il.Emit(OpCodes.Leave, done); il.Append(done);
        caller.Body.ExceptionHandlers.Add(new ExceptionHandler(ExceptionHandlerType.Catch)
        {
            CatchType = module.ImportReference(typeof(Exception)), TryStart = start, TryEnd = handler,
            HandlerStart = handler, HandlerEnd = done,
        });
    }

    private enum OracleEnum { None }
    private struct OraclePlain { public int Value { get; set; } }
    private struct OracleReferences { public object? Value { get; set; } }
    private delegate System.Numerics.Vector<T> Estimate<T>(System.Numerics.Vector<T> left, System.Numerics.Vector<T> right, System.Numerics.Vector<T> addend) where T : struct;
    private static int _oracleOperands;
    private static System.Numerics.Vector<T> Operand<T>(int index) where T : struct
    {
        _oracleOperands = _oracleOperands * 10 + index;
        return default;
    }
    private static void Oracle<T>() where T : struct
    {
        const System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic;
        var definition = typeof(System.Numerics.Vector).GetMethods(flags).Single(m => m.Name == "MultiplyAddEstimate" && m.IsGenericMethodDefinition);
        var target = definition.MakeGenericMethod(typeof(T));
        var bridge = typeof(System.Numerics.Vector<T>).GetMethods(flags).Single(m => m.Name.EndsWith(".MultiplyAddEstimate", StringComparison.Ordinal));
        var estimate = target.CreateDelegate<Estimate<T>>();
        var bridgeCall = bridge.CreateDelegate<Estimate<T>>();
        for (int route = 0; route < 3; route++)
        {
            _oracleOperands = 0;
            try
            {
                if (route == 0) target.Invoke(null, new object[] { Operand<T>(1), Operand<T>(2), Operand<T>(3) });
                else if (route == 1) estimate(Operand<T>(1), Operand<T>(2), Operand<T>(3));
                else bridgeCall(Operand<T>(1), Operand<T>(2), Operand<T>(3));
                throw new InvalidOperationException("unexpected generic vector estimate return");
            }
            catch (Exception error)
            {
                var actual = error is System.Reflection.TargetInvocationException { InnerException: { } inner } ? inner : error;
                if (actual.GetType() != typeof(NotSupportedException) || _oracleOperands != 123) throw;
                Console.WriteLine(_oracleOperands);
                Console.WriteLine(actual.GetType().FullName);
                Console.WriteLine(actual.Message);
            }
        }
    }

    private static TVector FixedOperand<TVector>(int index) where TVector : struct
    {
        _oracleOperands = _oracleOperands * 10 + index;
        return default;
    }

    private static void FixedOracle()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        const System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public;
        var core = typeof(object).Assembly;
        foreach (int width in new[] { 64, 128, 256, 512 })
        foreach (var element in new[] { typeof(decimal), typeof(OracleEnum), typeof(OraclePlain), typeof(OracleReferences), typeof(Int128), typeof(UInt128) })
        {
            var facade = core.GetType("System.Runtime.Intrinsics.Vector" + width, true)!;
            var vector = core.GetType("System.Runtime.Intrinsics.Vector" + width + "`1", true)!.MakeGenericType(element);
            var target = facade.GetMethods(flags).Single(m => m.Name == "MultiplyAddEstimate" && m.IsGenericMethodDefinition).MakeGenericMethod(element);
            var bridge = vector.GetMethods(flags).Single(m => m.Name.EndsWith(".MultiplyAddEstimate", StringComparison.Ordinal));
            foreach (string route in new[] { "direct", "address", "calli", "bridge" })
            {
                var actualTarget = route == "bridge" ? bridge : target;
                var dynamic = new System.Reflection.Emit.DynamicMethod("FixedEstimate_" + route, typeof(void), Type.EmptyTypes, typeof(GenericVectorEstimateFixture).Module, true);
                var il = dynamic.GetILGenerator();
                System.Reflection.Emit.LocalBuilder? address = null;
                if (route == "address")
                {
                    address = il.DeclareLocal(typeof(IntPtr));
                    il.Emit(System.Reflection.Emit.OpCodes.Ldftn, actualTarget);
                    il.Emit(System.Reflection.Emit.OpCodes.Stloc, address);
                }
                var operand = typeof(GenericVectorEstimateFixture).GetMethod(nameof(FixedOperand), flags)!.MakeGenericMethod(vector);
                for (int i = 1; i <= 3; i++)
                {
                    il.Emit(System.Reflection.Emit.OpCodes.Ldc_I4, i);
                    il.Emit(System.Reflection.Emit.OpCodes.Call, operand);
                }
                if (route is "direct" or "bridge") il.Emit(System.Reflection.Emit.OpCodes.Call, actualTarget);
                else
                {
                    if (route == "address") il.Emit(System.Reflection.Emit.OpCodes.Ldloc, address!);
                    else il.Emit(System.Reflection.Emit.OpCodes.Ldftn, actualTarget);
                    il.EmitCalli(System.Reflection.Emit.OpCodes.Calli, System.Reflection.CallingConventions.Standard,
                        vector, new[] { vector, vector, vector }, null);
                }
                il.Emit(System.Reflection.Emit.OpCodes.Pop); il.Emit(System.Reflection.Emit.OpCodes.Ret);
                var call = (Action)dynamic.CreateDelegate(typeof(Action));
                _oracleOperands = 0;
                try { call(); throw new InvalidOperationException("unexpected fixed estimate return"); }
                catch (NotSupportedException error)
                {
                    if (_oracleOperands != 123) throw new InvalidOperationException("fixed estimate operand order");
                    Console.WriteLine(_oracleOperands); Console.WriteLine(error.GetType().FullName); Console.WriteLine(error.Message);
                }
            }
        }
    }

}

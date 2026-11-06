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

internal static class VectorAsFixture
{
    internal static readonly string[] Elements = { "decimal", "enum", "struct", "refs", "int128", "uint128" };
    internal static readonly string[] NumericElements = { "byte", "sbyte", "short", "ushort", "int", "uint", "long", "ulong", "nint", "nuint", "float", "double" };
    internal static readonly string[] Mutations = { "instance", "vararg", "arity", "nongeneric", "concrete", "typevar", "swapped", "extra", "missing", "byref-result", "modopt-parameter", "modreq-result", "pointer-parameter", "fixed", "modopt-owner", "byref-owner", "generic-owner", "foreign-owner", "zero", "one", "three", "trailing-spec", "trailing-reference", "reference-source", "reference-destination", "byreflike-source", "nullable-source", "byref-argument", "pointer-argument", "modopt-argument", "bare", "bare-concrete" };

    public static bool Run(string[] args)
    {
        if (args.Length != 4 || args[0] != "--vector-as-fixture") return false;
        if (args[3] == "canonical-runtime")
        {
            BuildCanonical(args[1], args[2]);
        }
        else if (args[3] == "numeric-runtime")
        {
            BuildNumeric(args[1], args[2]);
        }
        else if (args[3] is "all-runtime" or "wide-runtime")
        {
            var pairs = new List<(string Source, string Destination)>();
            foreach (string element in Elements)
            {
                foreach (string numeric in args[3] == "wide-runtime" ? NumericElements : new[] { "int" })
                {
                    pairs.Add((element, numeric)); pairs.Add((numeric, element));
                }
                foreach (string other in args[3] == "wide-runtime" ? Elements : new[] { element }) pairs.Add((element, other));
            }
            if (args[3] == "all-runtime") { pairs.Add(("decimal", "refs")); pairs.Add(("refs", "decimal")); }
            var cases = pairs.SelectMany(pair => new[] { "direct", "address", "calli" }
                .Select(route => (pair.Source, pair.Destination, route, "valid-runtime"))).ToList();
            foreach (string context in new[] { "mvar", "var" })
            foreach (var pair in new[] { ("decimal", "int"), ("int", "decimal"), ("decimal", "refs") })
            foreach (string route in new[] { "direct", "address", "calli" })
                cases.Add((pair.Item1, pair.Item2, route, "valid-runtime-" + context));
            foreach (string route in new[] { "direct", "address", "calli" })
            {
                cases.Add(("decimal", "int", route, "runtime-throw"));
                cases.Add(("decimal", "decimal", route, "runtime-and"));
                cases.Add(("decimal", "decimal", route, "runtime-and-left-throw"));
                cases.Add(("decimal", "decimal", route, "runtime-and-right-throw"));
            }
            Build(args[1], args[2], cases);
        }
        else
        {
            var mode = args[3].Split(':');
            if (mode.Length != 4) throw new ArgumentException("expected source:destination:direct/address/calli:mutation");
            Build(args[1], args[2], new[] { (mode[0], mode[1], mode[2], mode[3]) });
        }
        Console.WriteLine("vector-as-fixture:" + args[3]);
        return true;
    }

    internal static void Build(string output, string corePath, IEnumerable<(string Source, string Destination, string Route, string Mutation)> cases, string? forwardingFamily = null)
    {
        output = Path.GetFullPath(output);
        if (File.Exists(output)) throw new InvalidOperationException("fixture output must be new");
        using var core = AssemblyDefinition.ReadAssembly(corePath);
        using var assembly = AssemblyDefinition.CreateAssembly(new AssemblyNameDefinition("VectorAs", new Version(1, 0)), "VectorAs", ModuleKind.Console);
        var module = assembly.MainModule;
        var host = new TypeDefinition("Fixture", "Program", TypeAttributes.Public | TypeAttributes.Abstract | TypeAttributes.Sealed, module.TypeSystem.Object);
        module.Types.Add(host);
        var plain = new TypeDefinition("Fixture", "Plain", TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.SequentialLayout, module.ImportReference(core.MainModule.GetType("System.ValueType")));
        plain.Fields.Add(new FieldDefinition("Value", FieldAttributes.Public, module.TypeSystem.Int32)); module.Types.Add(plain);
        var refs = new TypeDefinition("Fixture", "References", TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.SequentialLayout, plain.BaseType);
        refs.Fields.Add(new FieldDefinition("Value", FieldAttributes.Public, module.TypeSystem.Object)); module.Types.Add(refs);
        var enumeration = new TypeDefinition("Fixture", "Enumeration", TypeAttributes.Public | TypeAttributes.Sealed, module.ImportReference(core.MainModule.GetType("System.Enum")));
        enumeration.Fields.Add(new FieldDefinition("value__", FieldAttributes.Public | FieldAttributes.SpecialName | FieldAttributes.RTSpecialName, module.TypeSystem.Int32)); module.Types.Add(enumeration);
        var byrefLike = new TypeDefinition("Fixture", "ByRefLike", TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.SequentialLayout, plain.BaseType);
        byrefLike.Fields.Add(new FieldDefinition("Value", FieldAttributes.Public, module.TypeSystem.Int32));
        byrefLike.CustomAttributes.Add(new Mono.Cecil.CustomAttribute(module.ImportReference(core.MainModule.GetType("System.Runtime.CompilerServices.IsByRefLikeAttribute").Methods.Single(m => m.IsConstructor && m.Parameters.Count == 0))));
        module.Types.Add(byrefLike);
        TypeReference Element(string name) => name switch
        {
            "enum" => enumeration, "struct" => plain, "refs" => refs,
            "decimal" => module.ImportReference(core.MainModule.GetType("System.Decimal")),
            "int128" => module.ImportReference(core.MainModule.GetType("System.Int128")),
            "uint128" => module.ImportReference(core.MainModule.GetType("System.UInt128")),
            "byte" => module.TypeSystem.Byte, "sbyte" => module.TypeSystem.SByte,
            "short" => module.TypeSystem.Int16, "ushort" => module.TypeSystem.UInt16,
            "int" => module.TypeSystem.Int32, "uint" => module.TypeSystem.UInt32,
            "long" => module.TypeSystem.Int64, "ulong" => module.TypeSystem.UInt64,
            "nint" => module.TypeSystem.IntPtr, "nuint" => module.TypeSystem.UIntPtr,
            "float" => module.TypeSystem.Single, "double" => module.TypeSystem.Double,
            _ => throw new ArgumentException(name),
        };
        TypeReference Vector(TypeReference element, bool fixedWidth = false)
        {
            var vector = new GenericInstanceType(module.ImportReference(core.MainModule.GetType(fixedWidth ? "System.Runtime.Intrinsics.Vector128`1" : "System.Numerics.Vector`1")));
            vector.GenericArguments.Add(element); return vector;
        }
        var definition = core.MainModule.GetType("System.Numerics.Vector").Methods.Single(m => m.Name == "As" && m.GenericParameters.Count == 2);
        var entry = new MethodDefinition("Main", MethodAttributes.Public | MethodAttributes.Static, module.TypeSystem.Void);
        host.Methods.Add(entry); assembly.EntryPoint = entry;
        var entryIl = entry.Body.GetILProcessor();
        var invariant = module.ImportReference(typeof(CultureInfo).GetProperty(nameof(CultureInfo.InvariantCulture))!.GetMethod!);
        foreach (string property in new[] { nameof(CultureInfo.CurrentCulture), nameof(CultureInfo.CurrentUICulture) })
        {
            entryIl.Emit(OpCodes.Call, invariant);
            entryIl.Emit(OpCodes.Call, module.ImportReference(typeof(CultureInfo).GetProperty(property)!.SetMethod!));
        }
        int serial = 0;
        var patches = new List<(string Caller, bool Specification)>();
        foreach (var item in cases)
        {
            if (item.Route is not ("direct" or "address" or "calli")) throw new ArgumentException(item.Route);
            var source = Element(item.Source); var destination = Element(item.Destination);
            var sourceVector = Vector(source); var resultVector = Vector(destination);
            var caller = new MethodDefinition("Case" + serial++ + "_" + item.Source + "_" + item.Destination + "_" + item.Route + "_" + item.Mutation.Replace('-', '_'), MethodAttributes.Public | MethodAttributes.Static, module.TypeSystem.Void);
            bool runtimeVar = item.Mutation == "valid-runtime-var";
            var callerHost = host;
            if (runtimeVar)
            {
                callerHost = new TypeDefinition("Fixture", "Carrier" + serial + "`2", TypeAttributes.Public | TypeAttributes.Abstract | TypeAttributes.Sealed, module.TypeSystem.Object);
                callerHost.GenericParameters.Add(new GenericParameter("TFrom", callerHost)); callerHost.GenericParameters.Add(new GenericParameter("TTo", callerHost));
                module.Types.Add(callerHost);
            }
            callerHost.Methods.Add(caller);
            var methodFrom = new GenericParameter("TFrom", caller); var methodTo = new GenericParameter("TTo", caller);
            if (!runtimeVar) { caller.GenericParameters.Add(methodFrom); caller.GenericParameters.Add(methodTo); }
            var carrier = new TypeReference("Fixture", "Carrier`2", module, module);
            var typeFrom = new GenericParameter("TFrom", carrier); var typeTo = new GenericParameter("TTo", carrier);
            carrier.GenericParameters.Add(typeFrom); carrier.GenericParameters.Add(typeTo);
            var target = module.ImportReference(definition);
            TypeReference fromArgument = source, toArgument = destination;
            switch (item.Mutation)
            {
                case "valid": case "valid-runtime": case "runtime-throw": case "runtime-and": case "runtime-and-left-throw": case "runtime-and-right-throw": break;
                case "valid-mvar": case "valid-runtime-mvar": fromArgument = methodFrom; toArgument = methodTo; break;
                case "valid-runtime-var": fromArgument = callerHost.GenericParameters[0]; toArgument = callerHost.GenericParameters[1]; break;
                case "valid-var": fromArgument = typeFrom; toArgument = typeTo; break;
                case "instance": target.HasThis = true; break;
                case "vararg": target.CallingConvention = MethodCallingConvention.VarArg; break;
                case "arity": target.GenericParameters.Add(new GenericParameter("U", target)); break;
                case "nongeneric": target.GenericParameters.Clear(); target.CallingConvention = MethodCallingConvention.Default; break;
                case "concrete": case "bare-concrete": target.ReturnType = resultVector; target.Parameters[0].ParameterType = sourceVector; break;
                case "typevar": target.ReturnType = Vector(typeTo); target.Parameters[0].ParameterType = Vector(typeFrom); break;
                case "swapped": target.ReturnType = Vector(target.GenericParameters[0]); target.Parameters[0].ParameterType = Vector(target.GenericParameters[1]); break;
                case "extra": case "trailing-reference": target.Parameters.Add(new ParameterDefinition(module.TypeSystem.Int32)); break;
                case "missing": target.Parameters.Clear(); break;
                case "byref-result": target.ReturnType = new ByReferenceType(target.ReturnType); break;
                case "modopt-parameter": target.Parameters[0].ParameterType = new OptionalModifierType(module.TypeSystem.Object, target.Parameters[0].ParameterType); break;
                case "modreq-result": target.ReturnType = new RequiredModifierType(module.TypeSystem.Object, target.ReturnType); break;
                case "pointer-parameter": target.Parameters[0].ParameterType = new PointerType(target.Parameters[0].ParameterType); break;
                case "fixed": target.ReturnType = Vector(target.GenericParameters[1], true); target.Parameters[0].ParameterType = Vector(target.GenericParameters[0], true); break;
                case "modopt-owner": target.DeclaringType = new OptionalModifierType(module.TypeSystem.Object, target.DeclaringType); break;
                case "byref-owner": target.DeclaringType = new ByReferenceType(target.DeclaringType); break;
                case "generic-owner": var owner = new GenericInstanceType(target.DeclaringType); owner.GenericArguments.Add(source); target.DeclaringType = owner; break;
                case "foreign-owner": var foreign = new AssemblyNameReference("Foreign.Vector.Owner", new Version(1, 0)); module.AssemblyReferences.Add(foreign); target.DeclaringType = new TypeReference("System.Numerics", "Vector", module, foreign); break;
                case "reference-source": fromArgument = module.TypeSystem.Object; break;
                case "reference-destination": toArgument = module.TypeSystem.Object; break;
                case "byreflike-source": fromArgument = byrefLike; break;
                case "nullable-source": var nullable = new GenericInstanceType(module.ImportReference(core.MainModule.GetType("System.Nullable`1"))); nullable.GenericArguments.Add(source); fromArgument = nullable; break;
                case "byref-argument": fromArgument = new ByReferenceType(source); break;
                case "pointer-argument": fromArgument = new PointerType(source); break;
                case "modopt-argument": fromArgument = new OptionalModifierType(module.TypeSystem.Object, source); break;
                case "zero": case "one": case "three": case "trailing-spec": case "bare": break;
                default: throw new ArgumentException(item.Mutation);
            }
            MethodReference call = target;
            if (item.Mutation is not ("bare" or "bare-concrete"))
            {
                var specification = new GenericInstanceMethod(target);
                if (item.Mutation != "zero") specification.GenericArguments.Add(fromArgument);
                if (item.Mutation is not ("zero" or "one")) specification.GenericArguments.Add(toArgument);
                if (item.Mutation is "three" or "trailing-spec") specification.GenericArguments.Add(module.TypeSystem.Double);
                call = specification;
            }
            if (forwardingFamily is not null)
                call = ForwardingTarget(module, core, forwardingFamily, fromArgument, destination);
            if (item.Mutation == "trailing-spec") patches.Add((caller.Name, true));
            if (item.Mutation == "trailing-reference") patches.Add((caller.Name, false));
            if (item.Mutation.StartsWith("valid-runtime", StringComparison.Ordinal) || item.Mutation.StartsWith("runtime-", StringComparison.Ordinal))
            {
                bool and = item.Mutation.StartsWith("runtime-and", StringComparison.Ordinal);
                if (and)
                {
                    var imported = module.ImportReference(core.MainModule.GetType("System.Numerics.Vector`1").Methods.Single(m => m.Name == "op_BitwiseAnd"));
                    call = new MethodReference(imported.Name, imported.ReturnType, sourceVector) { HasThis = false };
                    foreach (var parameter in imported.Parameters) call.Parameters.Add(new ParameterDefinition(parameter.ParameterType));
                }
                bool variable = item.Mutation is "valid-runtime-mvar" or "valid-runtime-var";
                EmitRuntime(module, callerHost, caller, call, variable ? Vector(fromArgument) : sourceVector,
                    variable && forwardingFamily is null ? Vector(toArgument) : resultVector, item.Route, item.Mutation, serial, and,
                    inlineOperand: forwardingFamily is not null && runtimeVar);
            }
            else
            {
                var il = caller.Body.GetILProcessor(); caller.Body.InitLocals = true;
                var local = new VariableDefinition(sourceVector); caller.Body.Variables.Add(local);
                if (item.Route != "address")
                {
                    il.Emit(OpCodes.Ldloca, local); il.Emit(OpCodes.Initobj, sourceVector);
                    if (target.HasThis && item.Route == "direct") il.Emit(OpCodes.Ldnull);
                    if (item.Mutation != "missing" || item.Route != "direct") il.Emit(OpCodes.Ldloc, local);
                    if (item.Mutation == "extra" && item.Route == "direct") il.Emit(OpCodes.Ldc_I4_0);
                }
                if (item.Route == "direct") il.Emit(OpCodes.Call, call);
                else
                {
                    il.Emit(OpCodes.Ldftn, call);
                    if (item.Route == "calli") { var site = new CallSite(resultVector); site.Parameters.Add(new ParameterDefinition(sourceVector)); il.Emit(OpCodes.Calli, site); }
                }
                il.Emit(OpCodes.Pop); il.Emit(OpCodes.Ret);
            }
            if (runtimeVar)
            {
                var closedOwner = new GenericInstanceType(callerHost); closedOwner.GenericArguments.Add(source); closedOwner.GenericArguments.Add(destination);
                entryIl.Emit(OpCodes.Call, new MethodReference(caller.Name, module.TypeSystem.Void, closedOwner) { HasThis = false });
            }
            else
            {
                var closed = new GenericInstanceMethod(caller); closed.GenericArguments.Add(source); closed.GenericArguments.Add(destination);
                entryIl.Emit(OpCodes.Call, closed);
            }
        }
        entryIl.Emit(OpCodes.Ret);
        assembly.Write(output, new WriterParameters { Timestamp = 0, DeterministicMvid = true });
        if (patches.Count != 0)
        {
            byte[] bytes = File.ReadAllBytes(output);
            using var pe = new PEReader(new MemoryStream(bytes, writable: false)); var reader = pe.GetMetadataReader();
            using var serialized = AssemblyDefinition.ReadAssembly(output); var changed = new HashSet<int>();
            foreach (var patch in patches)
            {
                var caller = serialized.MainModule.GetType("Fixture.Program").Methods.Single(m => m.Name == patch.Caller);
                var target = caller.Body.Instructions.Select(i => i.Operand).OfType<MethodReference>().Single(m => m.Name == "As");
                if (!patch.Specification) target = ((GenericInstanceMethod)target).ElementMethod;
                var token = MetadataTokens.EntityHandle(target.MetadataToken.ToInt32());
                var signature = patch.Specification ? reader.GetMethodSpecification((MethodSpecificationHandle)token).Signature : reader.GetMemberReference((MemberReferenceHandle)token).Signature;
                int offset = pe.PEHeaders.MetadataStartOffset + reader.GetHeapMetadataOffset(HeapIndex.Blob) + MetadataTokens.GetHeapOffset(signature);
                int prefix = bytes[offset] < 0x80 ? 1 : bytes[offset] < 0xc0 ? 2 : 4;
                int count = offset + prefix + (patch.Specification ? 1 : 2);
                if (!changed.Add(count)) continue;
                if (bytes[count] != (patch.Specification ? 3 : 2)) throw new InvalidOperationException("unexpected raw count");
                bytes[count] = (byte)(patch.Specification ? 2 : 1);
            }
            File.WriteAllBytes(output, bytes);
        }
        File.WriteAllText(Path.ChangeExtension(output, ".runtimeconfig.json"), "{\"runtimeOptions\":{\"tfm\":\"net10.0\",\"framework\":{\"name\":\"Microsoft.NETCore.App\",\"version\":\"10.0.0\"}}}\n");
    }

    internal static void BuildCanonical(string output, string corePath, string? forwardingFamily = null)
    {
        output = Path.GetFullPath(output);
        if (File.Exists(output)) throw new InvalidOperationException("fixture output must be new");
        using var core = AssemblyDefinition.ReadAssembly(corePath);
        using var assembly = AssemblyDefinition.CreateAssembly(new AssemblyNameDefinition("VectorAsCanonical", new Version(1, 0)), "VectorAsCanonical", ModuleKind.Console);
        var module = assembly.MainModule;
        var host = new TypeDefinition("Fixture", "Program", TypeAttributes.Public | TypeAttributes.Abstract | TypeAttributes.Sealed, module.TypeSystem.Object); module.Types.Add(host);
        var carrier = new TypeDefinition("Fixture", "Carrier`1", TypeAttributes.Public | TypeAttributes.Abstract | TypeAttributes.Sealed, module.TypeSystem.Object); module.Types.Add(carrier);
        carrier.GenericParameters.Add(new GenericParameter("T", carrier));
        var enumeration = new TypeDefinition("Fixture", "Enumeration", TypeAttributes.Public | TypeAttributes.Sealed, module.ImportReference(core.MainModule.GetType("System.Enum")));
        enumeration.Fields.Add(new FieldDefinition("value__", FieldAttributes.Public | FieldAttributes.SpecialName | FieldAttributes.RTSpecialName, module.TypeSystem.Int32)); module.Types.Add(enumeration);
        TypeReference Vector(TypeReference element)
        {
            var vector = new GenericInstanceType(module.ImportReference(core.MainModule.GetType("System.Numerics.Vector`1"))); vector.GenericArguments.Add(element); return vector;
        }
        var integerVector = Vector(module.TypeSystem.Int32); var enumVector = Vector(enumeration);
        var counter = new FieldDefinition("Operands", FieldAttributes.Private | FieldAttributes.Static, module.TypeSystem.Int32); host.Fields.Add(counter);
        var count = module.ImportReference(typeof(System.Numerics.Vector<int>).GetProperty("Count")!.GetMethod!);
        var item = module.ImportReference(typeof(System.Numerics.Vector<int>).GetProperty("Item")!.GetMethod!);
        var constructor = module.ImportReference(typeof(System.Numerics.Vector<int>).GetConstructor(new[] { typeof(int[]) })!);
        var writeString = module.ImportReference(typeof(Console).GetMethod(nameof(Console.WriteLine), new[] { typeof(string) })!);
        var writeInt = module.ImportReference(typeof(Console).GetMethod(nameof(Console.WriteLine), new[] { typeof(int) })!);
        var errorConstructor = module.ImportReference(typeof(InvalidOperationException).GetConstructor(new[] { typeof(string) })!);
        void Increment(ILProcessor il) { il.Emit(OpCodes.Ldsfld, counter); il.Emit(OpCodes.Ldc_I4_1); il.Emit(OpCodes.Add); il.Emit(OpCodes.Stsfld, counter); }
        void Pattern(ILProcessor il, VariableDefinition index)
        {
            il.Emit(OpCodes.Ldc_I4, unchecked((int)0x81234567)); il.Emit(OpCodes.Ldloc, index); il.Emit(OpCodes.Ldc_I4, 0x010203); il.Emit(OpCodes.Mul); il.Emit(OpCodes.Xor);
        }
        var integerFactory = new MethodDefinition("IntegerOperand", MethodAttributes.Private | MethodAttributes.Static, integerVector); host.Methods.Add(integerFactory); integerFactory.Body.InitLocals = true;
        var bytes = new VariableDefinition(new ArrayType(module.TypeSystem.Int32)); var position = new VariableDefinition(module.TypeSystem.Int32);
        integerFactory.Body.Variables.Add(bytes); integerFactory.Body.Variables.Add(position);
        var f = integerFactory.Body.GetILProcessor(); Increment(f); f.Emit(OpCodes.Call, count); f.Emit(OpCodes.Newarr, module.TypeSystem.Int32); f.Emit(OpCodes.Stloc, bytes);
        f.Emit(OpCodes.Ldc_I4_0); f.Emit(OpCodes.Stloc, position);
        var fill = Instruction.Create(OpCodes.Ldloc, bytes); var condition = Instruction.Create(OpCodes.Ldloc, position);
        f.Emit(OpCodes.Br, condition); f.Append(fill); f.Emit(OpCodes.Ldloc, position); Pattern(f, position); f.Emit(OpCodes.Stelem_I4);
        f.Emit(OpCodes.Ldloc, position); f.Emit(OpCodes.Ldc_I4_1); f.Emit(OpCodes.Add); f.Emit(OpCodes.Stloc, position);
        f.Append(condition); f.Emit(OpCodes.Call, count); f.Emit(OpCodes.Blt, fill);
        f.Emit(OpCodes.Ldloc, bytes); f.Emit(OpCodes.Newobj, constructor); f.Emit(OpCodes.Ret);
        var enumFactory = new MethodDefinition("EnumOperand", MethodAttributes.Private | MethodAttributes.Static, enumVector); host.Methods.Add(enumFactory); enumFactory.Body.InitLocals = true;
        var empty = new VariableDefinition(enumVector); enumFactory.Body.Variables.Add(empty); var ef = enumFactory.Body.GetILProcessor(); Increment(ef);
        ef.Emit(OpCodes.Ldloca, empty); ef.Emit(OpCodes.Initobj, enumVector); ef.Emit(OpCodes.Ldloc, empty); ef.Emit(OpCodes.Ret);
        var asDefinition = module.ImportReference(core.MainModule.GetType("System.Numerics.Vector").Methods.Single(m => m.Name == "As" && m.GenericParameters.Count == 2));
        var main = new MethodDefinition("Main", MethodAttributes.Public | MethodAttributes.Static, module.TypeSystem.Void); host.Methods.Add(main); assembly.EntryPoint = main;
        var entry = main.Body.GetILProcessor();
        foreach (string property in new[] { nameof(CultureInfo.CurrentCulture), nameof(CultureInfo.CurrentUICulture) })
        {
            entry.Emit(OpCodes.Call, module.ImportReference(typeof(CultureInfo).GetProperty(nameof(CultureInfo.InvariantCulture))!.GetMethod!));
            entry.Emit(OpCodes.Call, module.ImportReference(typeof(CultureInfo).GetProperty(property)!.SetMethod!));
        }
        int serial = 0;
        foreach (bool typeContext in new[] { false, true })
        foreach (bool fromGeneric in new[] { true, false })
        foreach (bool indirect in new[] { false, true })
        {
            if (forwardingFamily is not null && !fromGeneric) continue;
            var owner = typeContext ? carrier : host;
            string name = (fromGeneric ? "ToInt" : "FromInt") + (indirect ? "AdapterCalli" : "Direct");
            var adapter = new MethodDefinition(name, MethodAttributes.Public | MethodAttributes.Static, module.TypeSystem.Void); owner.Methods.Add(adapter);
            GenericParameter variable;
            if (typeContext) variable = carrier.GenericParameters[0];
            else { variable = new GenericParameter("T", adapter); adapter.GenericParameters.Add(variable); }
            var source = fromGeneric ? Vector(variable) : integerVector; var result = fromGeneric ? integerVector : Vector(variable);
            adapter.ReturnType = result; adapter.Parameters.Add(new ParameterDefinition(source));
            var target = new GenericInstanceMethod(asDefinition);
            target.GenericArguments.Add(fromGeneric ? variable : module.TypeSystem.Int32); target.GenericArguments.Add(fromGeneric ? module.TypeSystem.Int32 : variable);
            MethodReference invocationTarget = forwardingFamily is null ? target
                : ForwardingTarget(module, core, forwardingFamily, variable, module.TypeSystem.Int32);
            var body = adapter.Body.GetILProcessor(); body.Emit(OpCodes.Ldarg_0); body.Emit(OpCodes.Call, invocationTarget); body.Emit(OpCodes.Ret);
            foreach (bool unsupported in new[] { false, true })
            {
                TypeReference element = unsupported ? enumeration : module.TypeSystem.Int32;
                MethodReference invocation;
                if (typeContext)
                {
                    var closedOwner = new GenericInstanceType(carrier); closedOwner.GenericArguments.Add(element);
                    invocation = new MethodReference(adapter.Name, adapter.ReturnType, closedOwner) { HasThis = false };
                    invocation.Parameters.Add(new ParameterDefinition(adapter.Parameters[0].ParameterType));
                }
                else { var closed = new GenericInstanceMethod(adapter); closed.GenericArguments.Add(element); invocation = closed; }
                string label = "Case" + serial + ":" + (typeContext ? "var" : "mvar") + ":" + name + ":" + (unsupported ? "enum" : "int");
                var check = new MethodDefinition("Case" + serial++, MethodAttributes.Private | MethodAttributes.Static, module.TypeSystem.Void); host.Methods.Add(check); check.Body.InitLocals = true;
                var actual = new VariableDefinition(integerVector); var index = new VariableDefinition(module.TypeSystem.Int32); var error = new VariableDefinition(module.ImportReference(typeof(Exception)));
                check.Body.Variables.Add(actual); check.Body.Variables.Add(index); check.Body.Variables.Add(error);
                var il = check.Body.GetILProcessor(); il.Emit(OpCodes.Ldstr, label); il.Emit(OpCodes.Call, writeString); il.Emit(OpCodes.Ldc_I4_0); il.Emit(OpCodes.Stsfld, counter);
                var start = Instruction.Create(OpCodes.Nop); var done = Instruction.Create(OpCodes.Ret); il.Append(start);
                il.Emit(OpCodes.Call, unsupported && fromGeneric ? enumFactory : integerFactory);
                if (indirect)
                {
                    il.Emit(OpCodes.Ldftn, invocation);
                    var site = new CallSite(fromGeneric ? integerVector : Vector(element));
                    site.Parameters.Add(new ParameterDefinition(fromGeneric ? Vector(element) : integerVector));
                    il.Emit(OpCodes.Calli, site);
                }
                else il.Emit(OpCodes.Call, invocation);
                if (unsupported)
                {
                    il.Emit(OpCodes.Pop); il.Emit(OpCodes.Ldstr, "unsupported adapter returned"); il.Emit(OpCodes.Newobj, errorConstructor); il.Emit(OpCodes.Throw);
                }
                else
                {
                    il.Emit(OpCodes.Stloc, actual); il.Emit(OpCodes.Ldc_I4_0); il.Emit(OpCodes.Stloc, index);
                    var compare = Instruction.Create(OpCodes.Ldloca, actual); var test = Instruction.Create(OpCodes.Ldloc, index);
                    var fail = Instruction.Create(OpCodes.Ldstr, "numeric identity changed bytes or operand count");
                    il.Emit(OpCodes.Ldsfld, counter); il.Emit(OpCodes.Ldc_I4_1); il.Emit(OpCodes.Bne_Un, fail);
                    il.Emit(OpCodes.Br, test); il.Append(compare); il.Emit(OpCodes.Ldloc, index); il.Emit(OpCodes.Call, item); Pattern(il, index); il.Emit(OpCodes.Bne_Un, fail);
                    il.Emit(OpCodes.Ldloc, index); il.Emit(OpCodes.Ldc_I4_1); il.Emit(OpCodes.Add); il.Emit(OpCodes.Stloc, index);
                    il.Append(test); il.Emit(OpCodes.Call, count); il.Emit(OpCodes.Blt, compare);
                    il.Emit(OpCodes.Ldsfld, counter); il.Emit(OpCodes.Call, writeInt); il.Emit(OpCodes.Ldstr, "numeric"); il.Emit(OpCodes.Call, writeString); il.Emit(OpCodes.Ldstr, "pass"); il.Emit(OpCodes.Call, writeString); il.Emit(OpCodes.Leave, done);
                    il.Append(fail); il.Emit(OpCodes.Newobj, errorConstructor); il.Emit(OpCodes.Throw);
                }
                var handler = Instruction.Create(OpCodes.Stloc, error); il.Append(handler);
                il.Emit(OpCodes.Ldsfld, counter); il.Emit(OpCodes.Call, writeInt);
                il.Emit(OpCodes.Ldloc, error); il.Emit(OpCodes.Callvirt, module.ImportReference(typeof(object).GetMethod(nameof(object.GetType))!));
                il.Emit(OpCodes.Callvirt, module.ImportReference(typeof(Type).GetProperty(nameof(Type.FullName))!.GetMethod!)); il.Emit(OpCodes.Call, writeString);
                il.Emit(OpCodes.Ldloc, error); il.Emit(OpCodes.Callvirt, module.ImportReference(typeof(Exception).GetProperty(nameof(Exception.Message))!.GetMethod!)); il.Emit(OpCodes.Call, writeString);
                il.Emit(OpCodes.Leave, done); il.Append(done);
                check.Body.ExceptionHandlers.Add(new ExceptionHandler(ExceptionHandlerType.Catch) { CatchType = module.ImportReference(typeof(Exception)), TryStart = start, TryEnd = handler, HandlerStart = handler, HandlerEnd = done });
                entry.Emit(OpCodes.Call, check);
            }
        }
        entry.Emit(OpCodes.Ret);
        assembly.Write(output, new WriterParameters { Timestamp = 0, DeterministicMvid = true });
        File.WriteAllText(Path.ChangeExtension(output, ".runtimeconfig.json"), "{\"runtimeOptions\":{\"tfm\":\"net10.0\",\"framework\":{\"name\":\"Microsoft.NETCore.App\",\"version\":\"10.0.0\"}}}\n");
    }

    internal static void BuildNumeric(string output, string corePath, string? forwardingFamily = null)
    {
        output = Path.GetFullPath(output);
        if (File.Exists(output)) throw new InvalidOperationException("fixture output must be new");
        using var core = AssemblyDefinition.ReadAssembly(corePath);
        using var assembly = AssemblyDefinition.CreateAssembly(new AssemblyNameDefinition("VectorAsNumeric", new Version(1, 0)), "VectorAsNumeric", ModuleKind.Console);
        var module = assembly.MainModule;
        var host = new TypeDefinition("Fixture", "Program", TypeAttributes.Public | TypeAttributes.Abstract | TypeAttributes.Sealed, module.TypeSystem.Object); module.Types.Add(host);
        var counter = new FieldDefinition("Operands", FieldAttributes.Private | FieldAttributes.Static, module.TypeSystem.Int32); host.Fields.Add(counter);
        var byteVector = module.ImportReference(typeof(System.Numerics.Vector<byte>));
        var count = module.ImportReference(typeof(System.Numerics.Vector<byte>).GetProperty("Count")!.GetMethod!);
        var constructor = module.ImportReference(typeof(System.Numerics.Vector<byte>).GetConstructor(new[] { typeof(byte[]) })!);
        var item = module.ImportReference(typeof(System.Numerics.Vector<byte>).GetProperty("Item")!.GetMethod!);
        var write = module.ImportReference(typeof(Console).GetMethod(nameof(Console.WriteLine), new[] { typeof(string) })!);
        var errorConstructor = module.ImportReference(typeof(InvalidOperationException).GetConstructor(new[] { typeof(string) })!);
        var factory = new MethodDefinition("Operand", MethodAttributes.Private | MethodAttributes.Static, byteVector);
        factory.Parameters.Add(new ParameterDefinition(module.TypeSystem.Int32)); host.Methods.Add(factory); factory.Body.InitLocals = true;
        var bytes = new VariableDefinition(new ArrayType(module.TypeSystem.Byte)); var pattern = new VariableDefinition(new ArrayType(module.TypeSystem.Byte)); var index = new VariableDefinition(module.TypeSystem.Int32);
        factory.Body.Variables.Add(bytes); factory.Body.Variables.Add(pattern); factory.Body.Variables.Add(index);
        var f = factory.Body.GetILProcessor();
        f.Emit(OpCodes.Ldsfld, counter); f.Emit(OpCodes.Ldc_I4_1); f.Emit(OpCodes.Add); f.Emit(OpCodes.Stsfld, counter);
        f.Emit(OpCodes.Call, count); f.Emit(OpCodes.Newarr, module.TypeSystem.Byte); f.Emit(OpCodes.Stloc, bytes);
        f.Emit(OpCodes.Ldc_I4, 16); f.Emit(OpCodes.Newarr, module.TypeSystem.Byte); f.Emit(OpCodes.Stloc, pattern);
        var second = Instruction.Create(OpCodes.Nop); var fill = Instruction.Create(OpCodes.Ldc_I4_0);
        f.Emit(OpCodes.Ldarg_0); f.Emit(OpCodes.Brtrue, second);
        byte[][] patterns = { new byte[] { 0, 0, 0, 128, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 128 }, new byte[] { 1, 0, 192, 127, 255, 255, 255, 255, 1, 0, 0, 0, 0, 0, 248, 127 } };
        for (int phase = 0; phase < 2; phase++)
        {
            if (phase == 1) f.Append(second);
            for (int i = 0; i < 16; i++) { f.Emit(OpCodes.Ldloc, pattern); f.Emit(OpCodes.Ldc_I4, i); f.Emit(OpCodes.Ldc_I4, (int)patterns[phase][i]); f.Emit(OpCodes.Stelem_I1); }
            f.Emit(OpCodes.Br, fill);
        }
        f.Append(fill); f.Emit(OpCodes.Stloc, index);
        var loop = Instruction.Create(OpCodes.Ldloc, bytes); var test = Instruction.Create(OpCodes.Ldloc, index);
        f.Emit(OpCodes.Br, test); f.Append(loop); f.Emit(OpCodes.Ldloc, index); f.Emit(OpCodes.Ldloc, pattern); f.Emit(OpCodes.Ldloc, index); f.Emit(OpCodes.Ldc_I4, 15); f.Emit(OpCodes.And); f.Emit(OpCodes.Ldelem_U1); f.Emit(OpCodes.Stelem_I1);
        f.Emit(OpCodes.Ldloc, index); f.Emit(OpCodes.Ldc_I4_1); f.Emit(OpCodes.Add); f.Emit(OpCodes.Stloc, index);
        f.Append(test); f.Emit(OpCodes.Call, count); f.Emit(OpCodes.Blt, loop);
        f.Emit(OpCodes.Ldloc, bytes); f.Emit(OpCodes.Newobj, constructor); f.Emit(OpCodes.Ret);
        var definition = module.ImportReference(core.MainModule.GetType("System.Numerics.Vector").Methods.Single(m => m.Name == "As" && m.GenericParameters.Count == 2));
        TypeReference[] numeric = { module.TypeSystem.Byte, module.TypeSystem.SByte, module.TypeSystem.Int16, module.TypeSystem.UInt16, module.TypeSystem.Int32, module.TypeSystem.UInt32, module.TypeSystem.Int64, module.TypeSystem.UInt64, module.TypeSystem.IntPtr, module.TypeSystem.UIntPtr, module.TypeSystem.Single, module.TypeSystem.Double };
        var main = new MethodDefinition("Main", MethodAttributes.Public | MethodAttributes.Static, module.TypeSystem.Void); host.Methods.Add(main); assembly.EntryPoint = main;
        var entry = main.Body.GetILProcessor();
        foreach (string property in new[] { nameof(CultureInfo.CurrentCulture), nameof(CultureInfo.CurrentUICulture) })
        {
            entry.Emit(OpCodes.Call, module.ImportReference(typeof(CultureInfo).GetProperty(nameof(CultureInfo.InvariantCulture))!.GetMethod!));
            entry.Emit(OpCodes.Call, module.ImportReference(typeof(CultureInfo).GetProperty(property)!.SetMethod!));
        }
        for (int n = 0; n < numeric.Length; n++)
        {
            var check = new MethodDefinition("Numeric" + n, MethodAttributes.Private | MethodAttributes.Static, module.TypeSystem.Void); host.Methods.Add(check); check.Body.InitLocals = true;
            var original = new VariableDefinition(byteVector); var restored = new VariableDefinition(byteVector); var position = new VariableDefinition(module.TypeSystem.Int32);
            check.Body.Variables.Add(original); check.Body.Variables.Add(restored); check.Body.Variables.Add(position);
            var outbound = new GenericInstanceMethod(definition); outbound.GenericArguments.Add(module.TypeSystem.Byte); outbound.GenericArguments.Add(numeric[n]);
            var inbound = new GenericInstanceMethod(definition); inbound.GenericArguments.Add(numeric[n]); inbound.GenericArguments.Add(module.TypeSystem.Byte);
            MethodReference outboundTarget = forwardingFamily is null ? outbound
                : ForwardingTarget(module, core, forwardingFamily, module.TypeSystem.Byte, numeric[n]);
            MethodReference inboundTarget = forwardingFamily is null ? inbound
                : ForwardingTarget(module, core, forwardingFamily, numeric[n], module.TypeSystem.Byte);
            var il = check.Body.GetILProcessor(); var fail = Instruction.Create(OpCodes.Ldstr, "numeric reinterpretation changed bytes or evaluation count");
            il.Emit(OpCodes.Ldc_I4_0); il.Emit(OpCodes.Stsfld, counter);
            for (int phase = 0; phase < 2; phase++)
            {
                il.Emit(OpCodes.Ldc_I4, phase); il.Emit(OpCodes.Call, factory); il.Emit(OpCodes.Dup); il.Emit(OpCodes.Stloc, original);
                il.Emit(OpCodes.Call, outboundTarget); il.Emit(OpCodes.Call, inboundTarget); il.Emit(OpCodes.Stloc, restored);
                il.Emit(OpCodes.Ldsfld, counter); il.Emit(OpCodes.Ldc_I4, phase + 1); il.Emit(OpCodes.Bne_Un, fail);
                il.Emit(OpCodes.Ldc_I4_0); il.Emit(OpCodes.Stloc, position);
                var body = Instruction.Create(OpCodes.Ldloca, original); var condition = Instruction.Create(OpCodes.Ldloc, position);
                il.Emit(OpCodes.Br, condition); il.Append(body); il.Emit(OpCodes.Ldloc, position); il.Emit(OpCodes.Call, item);
                il.Emit(OpCodes.Ldloca, restored); il.Emit(OpCodes.Ldloc, position); il.Emit(OpCodes.Call, item); il.Emit(OpCodes.Bne_Un, fail);
                il.Emit(OpCodes.Ldloc, position); il.Emit(OpCodes.Ldc_I4_1); il.Emit(OpCodes.Add); il.Emit(OpCodes.Stloc, position);
                il.Append(condition); il.Emit(OpCodes.Call, count); il.Emit(OpCodes.Blt, body);
            }
            il.Emit(OpCodes.Ldstr, "numeric:" + NumericElements[n] + ":pass"); il.Emit(OpCodes.Call, write); il.Emit(OpCodes.Ret);
            il.Append(fail); il.Emit(OpCodes.Newobj, errorConstructor); il.Emit(OpCodes.Throw);
            entry.Emit(OpCodes.Call, check);
        }
        entry.Emit(OpCodes.Ret);
        assembly.Write(output, new WriterParameters { Timestamp = 0, DeterministicMvid = true });
        File.WriteAllText(Path.ChangeExtension(output, ".runtimeconfig.json"), "{\"runtimeOptions\":{\"tfm\":\"net10.0\",\"framework\":{\"name\":\"Microsoft.NETCore.App\",\"version\":\"10.0.0\"}}}\n");
    }

    private static MethodReference ForwardingTarget(ModuleDefinition module, AssemblyDefinition core,
        string family, TypeReference source, TypeReference destination)
    {
        if (family == "explicit")
        {
            var vector = core.MainModule.GetType("System.Numerics.Vector`1");
            var definition = vector.Methods.Single(m => m.Name == "op_Explicit"
                && ((GenericInstanceType)m.ReturnType).GenericArguments[0].FullName == destination.FullName);
            var imported = module.ImportReference(definition);
            var owner = new GenericInstanceType(module.ImportReference(vector)); owner.GenericArguments.Add(source);
            var result = new MethodReference(imported.Name, imported.ReturnType, owner) { HasThis = false };
            foreach (var parameter in imported.Parameters) result.Parameters.Add(new ParameterDefinition(parameter.ParameterType));
            return result;
        }
        if (family != "alias") throw new ArgumentException(family);
        string suffix = destination.MetadataType switch
        {
            MetadataType.IntPtr => "NInt", MetadataType.UIntPtr => "NUInt", _ => destination.Name
        };
        var alias = core.MainModule.GetType("System.Numerics.Vector").Methods.Single(m => m.Name == "AsVector" + suffix
            && m.GenericParameters.Count == 1 && m.Parameters.Count == 1);
        var call = new GenericInstanceMethod(module.ImportReference(alias)); call.GenericArguments.Add(source);
        return call;
    }

    private static void EmitRuntime(ModuleDefinition module, TypeDefinition host, MethodDefinition caller, MethodReference target,
        TypeReference source, TypeReference result, string route, string mutation, int serial, bool and, bool inlineOperand = false)
    {
        var counterDefinition = new FieldDefinition("Operands" + serial, FieldAttributes.Private | FieldAttributes.Static, module.TypeSystem.Int32); host.Fields.Add(counterDefinition);
        FieldReference counter = counterDefinition;
        if (mutation == "valid-runtime-var")
        {
            var owner = new GenericInstanceType(host); foreach (var argument in host.GenericParameters) owner.GenericArguments.Add(argument);
            counter = new FieldReference(counterDefinition.Name, module.TypeSystem.Int32, owner);
        }
        var factories = new List<MethodReference>();
        for (int index = 1; !inlineOperand && index <= (and ? 2 : 1); index++)
        {
            var factory = new MethodDefinition("Operand" + serial + "_" + index, MethodAttributes.Private | MethodAttributes.Static, source); host.Methods.Add(factory);
            if (mutation == "valid-runtime-mvar")
            {
                factory.GenericParameters.Add(new GenericParameter("TFrom", factory)); factory.GenericParameters.Add(new GenericParameter("TTo", factory));
                var invocation = new GenericInstanceMethod(factory); foreach (var argument in caller.GenericParameters) invocation.GenericArguments.Add(argument); factories.Add(invocation);
            }
            else if (mutation == "valid-runtime-var")
            {
                var owner = new GenericInstanceType(host); foreach (var argument in host.GenericParameters) owner.GenericArguments.Add(argument);
                factories.Add(new MethodReference(factory.Name, source, owner) { HasThis = false });
            }
            else factories.Add(factory);
            var local = new VariableDefinition(source); factory.Body.Variables.Add(local); factory.Body.InitLocals = true;
            var body = factory.Body.GetILProcessor();
            body.Emit(OpCodes.Ldsfld, counter); body.Emit(OpCodes.Ldc_I4, 10); body.Emit(OpCodes.Mul); body.Emit(OpCodes.Ldc_I4, index); body.Emit(OpCodes.Add); body.Emit(OpCodes.Stsfld, counter);
            bool fail = mutation == "runtime-throw" || mutation == "runtime-and-left-throw" && index == 1 || mutation == "runtime-and-right-throw" && index == 2;
            if (fail)
            {
                body.Emit(OpCodes.Ldstr, and ? index == 1 ? "left" : "right" : "operand");
                body.Emit(OpCodes.Newobj, module.ImportReference(typeof(InvalidOperationException).GetConstructor(new[] { typeof(string) })!)); body.Emit(OpCodes.Throw);
            }
            else { body.Emit(OpCodes.Ldloca, local); body.Emit(OpCodes.Initobj, source); body.Emit(OpCodes.Ldloc, local); body.Emit(OpCodes.Ret); }
        }
        var writeString = module.ImportReference(typeof(Console).GetMethod(nameof(Console.WriteLine), new[] { typeof(string) })!);
        var writeInt = module.ImportReference(typeof(Console).GetMethod(nameof(Console.WriteLine), new[] { typeof(int) })!);
        var error = new VariableDefinition(module.ImportReference(typeof(Exception))); caller.Body.Variables.Add(error);
        var pointer = new VariableDefinition(module.TypeSystem.IntPtr); caller.Body.Variables.Add(pointer); caller.Body.InitLocals = true;
        var il = caller.Body.GetILProcessor();
        il.Emit(OpCodes.Ldstr, caller.Name); il.Emit(OpCodes.Call, writeString);
        il.Emit(OpCodes.Ldc_I4_0); il.Emit(OpCodes.Stsfld, counter);
        var start = Instruction.Create(OpCodes.Nop); il.Append(start);
        if (route == "address") { il.Emit(OpCodes.Ldftn, target); il.Emit(OpCodes.Stloc, pointer); }
        if (inlineOperand)
        {
            // Keep the VAR forwarding test independent of a generic producer's return ABI.
            var operand = new VariableDefinition(source); caller.Body.Variables.Add(operand);
            il.Emit(OpCodes.Ldsfld, counter); il.Emit(OpCodes.Ldc_I4, 10); il.Emit(OpCodes.Mul);
            il.Emit(OpCodes.Ldc_I4_1); il.Emit(OpCodes.Add); il.Emit(OpCodes.Stsfld, counter);
            il.Emit(OpCodes.Ldloca, operand); il.Emit(OpCodes.Initobj, source); il.Emit(OpCodes.Ldloc, operand);
        }
        else foreach (var factory in factories) il.Emit(OpCodes.Call, factory);
        if (route == "direct") il.Emit(OpCodes.Call, target);
        else
        {
            var site = new CallSite(result);
            for (int index = 0; index < (and ? 2 : 1); index++) site.Parameters.Add(new ParameterDefinition(source));
            if (route == "address") il.Emit(OpCodes.Ldloc, pointer); else il.Emit(OpCodes.Ldftn, target);
            il.Emit(OpCodes.Calli, site);
        }
        il.Emit(OpCodes.Pop); il.Emit(OpCodes.Ldstr, "unexpected-return"); il.Emit(OpCodes.Call, writeString);
        var done = Instruction.Create(OpCodes.Ret); il.Emit(OpCodes.Leave, done);
        var handler = Instruction.Create(OpCodes.Stloc, error); il.Append(handler);
        il.Emit(OpCodes.Ldsfld, counter); il.Emit(OpCodes.Call, writeInt);
        il.Emit(OpCodes.Ldloc, error); il.Emit(OpCodes.Callvirt, module.ImportReference(typeof(object).GetMethod(nameof(object.GetType))!));
        il.Emit(OpCodes.Callvirt, module.ImportReference(typeof(Type).GetProperty(nameof(Type.FullName))!.GetMethod!)); il.Emit(OpCodes.Call, writeString);
        il.Emit(OpCodes.Ldloc, error); il.Emit(OpCodes.Callvirt, module.ImportReference(typeof(Exception).GetProperty(nameof(Exception.Message))!.GetMethod!)); il.Emit(OpCodes.Call, writeString);
        il.Emit(OpCodes.Leave, done); il.Append(done);
        caller.Body.ExceptionHandlers.Add(new ExceptionHandler(ExceptionHandlerType.Catch) { CatchType = module.ImportReference(typeof(Exception)), TryStart = start, TryEnd = handler, HandlerStart = handler, HandlerEnd = done });
    }
}

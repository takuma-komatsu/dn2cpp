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

internal static class VectorMagnitudeFixture
{
    internal static readonly string[] NumberOperations = { "MaxNumber", "MinNumber" };
    internal static readonly string[] Operations = { "MaxMagnitude", "MaxMagnitudeNumber", "MinMagnitude", "MinMagnitudeNumber" };
    internal static readonly string[] Elements = { "float", "double", "int", "uint", "long", "ulong", "byte", "sbyte", "short", "ushort", "nint", "nuint", "bool", "char", "object", "decimal", "enum", "struct", "refs", "int128", "uint128" };
    internal static readonly string[] Mutations = { "instance", "vararg", "arity", "nongeneric", "concrete", "typevar", "methodvar-one", "extra", "missing", "byref-result", "modopt-0", "modreq-1", "pointer-1", "width", "modopt-owner", "byref-owner", "generic-owner", "foreign-owner", "zero", "two", "trailing-spec", "trailing-reference", "reference", "primitive", "boolean", "byreflike", "nullable", "byref-argument", "pointer-argument", "modopt-argument", "bare", "bare-concrete" };
    public static bool Run(string[] args)
    {
        if (args.Length != 4 || args[0] is not ("--vector-magnitude-fixture" or "--vector-number-fixture")) return false;
        bool number = args[0] == "--vector-number-fixture";
        string prefix = number ? "vector-number" : "vector-magnitude";
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        var mode = args[3].Split(':');
        if (mode.Length != 5 || !(number ? NumberOperations : Operations).Contains(mode[0]) || !Elements.Contains(mode[2]) || mode[3] is not ("direct" or "address" or "calli"))
            throw new ArgumentException("expected operation:width:element:direct/address/calli:mutation");
        int width = int.Parse(mode[1], CultureInfo.InvariantCulture);
        if (width is not (0 or 64 or 128 or 256 or 512)) throw new ArgumentException("invalid width");
        Build(args[1], args[2], new[] { (mode[2], mode[3], mode[4]) }, width, mode[0]);
        using var stream = File.OpenRead(args[1]);
        using var pe = new PEReader(stream);
        var reader = pe.GetMetadataReader();
        for (int row = 1; row <= reader.GetTableRowCount(TableIndex.MethodSpec); row++)
        {
            var h = MetadataTokens.MethodSpecificationHandle(row);
            var spec = reader.GetMethodSpecification(h);
            if (spec.Method.Kind == HandleKind.MemberReference && reader.GetString(reader.GetMemberReference((MemberReferenceHandle)spec.Method).Name) == mode[0])
                Console.WriteLine(prefix + "-methodspec:" + MetadataTokens.GetToken(h).ToString("X8") + ":raw=" + Convert.ToHexString(reader.GetBlobBytes(spec.Signature)));
        }
        Console.WriteLine(prefix + "-fixture:" + args[3]);
        return true;
    }
    internal static void Build(string output, string corePath, IEnumerable<(string Element, string Route, string Mutation)> cases, int width = 0, string operation = "MaxMagnitude")
    {
        output = Path.GetFullPath(output);
        if (File.Exists(output)) throw new InvalidOperationException("fixture output must be new");
        using var core = AssemblyDefinition.ReadAssembly(corePath);
        using var assembly = AssemblyDefinition.CreateAssembly(new AssemblyNameDefinition("VectorMagnitude", new Version(1, 0)), "VectorMagnitude", ModuleKind.Console);
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
            "float" => module.TypeSystem.Single, "double" => module.TypeSystem.Double, "int" => module.TypeSystem.Int32,
            "uint" => module.TypeSystem.UInt32, "long" => module.TypeSystem.Int64, "ulong" => module.TypeSystem.UInt64,
            "byte" => module.TypeSystem.Byte, "sbyte" => module.TypeSystem.SByte, "short" => module.TypeSystem.Int16, "ushort" => module.TypeSystem.UInt16,
            "nint" => module.TypeSystem.IntPtr, "nuint" => module.TypeSystem.UIntPtr, "bool" => module.TypeSystem.Boolean, "char" => module.TypeSystem.Char,
            "object" => module.TypeSystem.Object, "enum" => enumeration, "struct" => plain, "refs" => refs,
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
            var definition = core.MainModule.GetType(selectedWidth == 0 ? "System.Numerics.Vector" : "System.Runtime.Intrinsics.Vector" + selectedWidth).Methods.Single(m => m.Name == operation && m.HasGenericParameters);
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
                case "missing": target.Parameters.RemoveAt(1); break;
                case "byref-result": target.ReturnType = new ByReferenceType(target.ReturnType); break;
                case "modopt-0": target.Parameters[0].ParameterType = new OptionalModifierType(module.TypeSystem.Object, target.Parameters[0].ParameterType); break;
                case "modreq-1": target.Parameters[1].ParameterType = new RequiredModifierType(module.TypeSystem.Object, target.Parameters[1].ParameterType); break;
                case "pointer-1": target.Parameters[1].ParameterType = new PointerType(target.Parameters[1].ParameterType); break;
                
                case "width": target.ReturnType = Vector(target.GenericParameters[0], selectedWidth, true); break;
                case "modopt-owner": target.DeclaringType = new OptionalModifierType(module.TypeSystem.Object, target.DeclaringType); break;
                case "byref-owner": target.DeclaringType = new ByReferenceType(target.DeclaringType); break;
                case "generic-owner":
                    var genericOwner = new GenericInstanceType(target.DeclaringType); genericOwner.GenericArguments.Add(element); target.DeclaringType = genericOwner; break;
                case "foreign-owner":
                    var foreign = new AssemblyNameReference("Foreign.Vector.Owner", new Version(1, 0)); module.AssemblyReferences.Add(foreign);
                    target.DeclaringType = new TypeReference("System.Numerics", "Vector", module, foreign); break;
                case "reference": argument = module.TypeSystem.Object; break;
                case "primitive": argument = module.TypeSystem.Int32; break;
                case "boolean": argument = module.TypeSystem.Boolean; break;
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
            if (item.Route != "address")
            {
                caller.Body.InitLocals = true;
                var local = new VariableDefinition(vector); caller.Body.Variables.Add(local);
                il.Emit(OpCodes.Ldloca, local); il.Emit(OpCodes.Initobj, vector);
                if (target.HasThis && item.Route == "direct") il.Emit(OpCodes.Ldnull);
                for (int i = 0; i < (mutation == "missing" && item.Route == "direct" ? 1 : 2); i++) il.Emit(OpCodes.Ldloc, local);
                if (mutation == "extra" && item.Route == "direct") il.Emit(OpCodes.Ldc_I4_0);
            }
            if (item.Route == "direct") il.Emit(OpCodes.Call, call);
            else
            {
                il.Emit(OpCodes.Ldftn, call);
                if (item.Route == "calli")
                {
                    var site = new CallSite(vector); for (int i = 0; i < 2; i++) site.Parameters.Add(new ParameterDefinition(vector));
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
                var serializedTarget = serializedCaller.Body.Instructions.Select(i => i.Operand).OfType<MethodReference>().Single(m => m.Name == operation);
                if (!patch.Specification) serializedTarget = ((GenericInstanceMethod)serializedTarget).ElementMethod;
                var token = MetadataTokens.EntityHandle(serializedTarget.MetadataToken.ToInt32());
                var signature = patch.Specification
                    ? reader.GetMethodSpecification((MethodSpecificationHandle)token).Signature
                    : reader.GetMemberReference((MemberReferenceHandle)token).Signature;
                int offset = pe.PEHeaders.MetadataStartOffset + reader.GetHeapMetadataOffset(HeapIndex.Blob) + MetadataTokens.GetHeapOffset(signature);
                int prefix = bytes[offset] < 0x80 ? 1 : bytes[offset] < 0xc0 ? 2 : 4;
                int countOffset = offset + prefix + (patch.Specification ? 1 : 2);
                if (!changed.Add(countOffset)) continue;
                if (bytes[countOffset] != (patch.Specification ? 2 : 3)) throw new InvalidOperationException("unexpected raw count");
                bytes[countOffset] = (byte)(patch.Specification ? 1 : 2);
            }
            File.WriteAllBytes(output, bytes);
        }
        File.WriteAllText(Path.ChangeExtension(output, ".runtimeconfig.json"), "{\"runtimeOptions\":{\"tfm\":\"net10.0\",\"framework\":{\"name\":\"Microsoft.NETCore.App\",\"version\":\"10.0.0\"}}}\n");
    }
}

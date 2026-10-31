using System.Globalization;
using Mono.Cecil;
using Mono.Cecil.Cil;

internal static class VectorEstimateFixture
{
    public static bool Run(string[] args)
    {
        if (args.Length != 4 || args[0] != "--vector-estimate-fixture") return false;
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        // Shape: 64f/64d/128f/128d/256f/256d/512f/512d/nf/nd:direct/address/calli:mutation.
        string[] mode = args[3] == "valid" ? new[] { "128f", "direct", "valid" } : args[3].Split(':');
        if (mode.Length != 3 || mode[0] is not ("64f" or "64d" or "128f" or "128d" or "256f" or "256d" or "512f" or "512d" or "nf" or "nd")
            || mode[1] is not ("direct" or "address" or "calli")) throw new ArgumentException("invalid estimate fixture mode");
        string output = Path.GetFullPath(args[1]);
        if (File.Exists(output)) throw new InvalidOperationException("fixture output must be new");
        using var core = AssemblyDefinition.ReadAssembly(args[2]);
        using var assembly = AssemblyDefinition.CreateAssembly(new AssemblyNameDefinition("VectorEstimate", new Version(1, 0)), "VectorEstimate", ModuleKind.Console);
        var module = assembly.MainModule;
        bool single = mode[0][^1] == 'f';
        string width = mode[0][..^1];
        string ownerName = width == "n" ? "System.Numerics.Vector" : "System.Runtime.Intrinsics.Vector" + width;
        var definition = core.MainModule.GetType(ownerName).Methods.Single(m => m.Name == "MultiplyAddEstimate"
            && !m.HasGenericParameters && ((GenericInstanceType)m.ReturnType).GenericArguments[0].MetadataType == (single ? MetadataType.Single : MetadataType.Double));
        var target = module.ImportReference(definition);
        var vector = module.ImportReference(definition.ReturnType);
        TypeReference AlterVector(string shape, TypeReference scalar)
        {
            string fullName = shape == "n" ? "System.Numerics.Vector`1" : "System.Runtime.Intrinsics.Vector" + shape + "`1";
            var changed = new GenericInstanceType(module.ImportReference(core.MainModule.GetType(fullName)));
            changed.GenericArguments.Add(scalar); return changed;
        }
        GenericParameter UnboundParameter(bool method)
        {
            IGenericParameterProvider carrier = method
                ? new MethodReference("GenericCarrier", module.TypeSystem.Void, target.DeclaringType)
                : new TypeReference("Fixture", "GenericCarrier`1", module, module);
            var parameter = new GenericParameter("T", carrier); carrier.GenericParameters.Add(parameter);
            return parameter;
        }
        var host = new TypeDefinition("Fixture", "Program", TypeAttributes.Public | TypeAttributes.Abstract | TypeAttributes.Sealed, module.TypeSystem.Object);
        module.Types.Add(host);
        var entry = new MethodDefinition("Main", MethodAttributes.Public | MethodAttributes.Static, module.TypeSystem.Void);
        host.Methods.Add(entry); assembly.EntryPoint = entry;
        var site = new CallSite(module.ImportReference(definition.ReturnType));
        foreach (var parameter in definition.Parameters) site.Parameters.Add(new ParameterDefinition(module.ImportReference(parameter.ParameterType)));
        string mutation = mode[2];
        int position = -2;
        if (mutation.EndsWith("-result", StringComparison.Ordinal)) { position = -1; mutation = mutation[..^7]; }
        else if (mutation.Length >= 2 && mutation[^2] == '-' && mutation[^1] is >= '0' and <= '2')
        { position = mutation[^1] - '0'; mutation = mutation[..^2]; }
        if (position != -2)
        {
            TypeReference original = position == -1 ? target.ReturnType : target.Parameters[position].ParameterType;
            TypeReference changed = mutation switch
            {
                "byref" => new ByReferenceType(original), "pointer" => new PointerType(original), "pinned" => new PinnedType(original),
                "modopt" => new OptionalModifierType(module.TypeSystem.Object, original),
                "modreq" => new RequiredModifierType(module.TypeSystem.Object, original),
                "width" => AlterVector(width == "128" ? "256" : "128", single ? module.TypeSystem.Single : module.TypeSystem.Double),
                "element" => AlterVector(width, single ? module.TypeSystem.Double : module.TypeSystem.Single),
                "integer" => AlterVector(width, module.TypeSystem.Int32),
                "methodvar" => AlterVector(width, UnboundParameter(true)),
                "typevar" => AlterVector(width, UnboundParameter(false)),
                _ => throw new ArgumentException("unknown operand mutation: " + mutation),
            };
            if (position == -1) target.ReturnType = changed; else target.Parameters[position].ParameterType = changed;
        }
        else switch (mutation)
        {
            case "valid": break;
            case "substitute":
            case "generic-substitute":
            case "valid-caller":
                if (mutation == "generic-substitute")
                {
                    target.GenericParameters.Add(new GenericParameter("T", target));
                    target.CallingConvention = MethodCallingConvention.Generic;
                }
                var helper = new MethodDefinition("GenericCaller", MethodAttributes.Public | MethodAttributes.Static, module.TypeSystem.Void);
                host.Methods.Add(helper);
                var argument = new GenericParameter("T", helper); helper.GenericParameters.Add(argument);
                var substituted = AlterVector(width, argument);
                if (mutation != "valid-caller")
                {
                    target.ReturnType = substituted;
                    foreach (var parameter in target.Parameters) parameter.ParameterType = substituted;
                    site.ReturnType = substituted;
                    foreach (var parameter in site.Parameters) parameter.ParameterType = substituted;
                }
                var closedHelper = new GenericInstanceMethod(helper); closedHelper.GenericArguments.Add(single ? module.TypeSystem.Single : module.TypeSystem.Double);
                var mainIl = entry.Body.GetILProcessor(); mainIl.Emit(OpCodes.Call, closedHelper); mainIl.Emit(OpCodes.Ret);
                entry = helper;
                if (mutation != "valid-caller") vector = substituted;
                break;
            case "spec-zero": target = new GenericInstanceMethod(target); break;
            case "spec-one":
                var concreteSpecification = new GenericInstanceMethod(target);
                concreteSpecification.GenericArguments.Add(module.TypeSystem.Single); target = concreteSpecification;
                break;
            case "spec-internal":
                var internalDefinition = core.MainModule.GetType(ownerName).Methods.Single(m => m.Name == "MultiplyAddEstimate" && m.HasGenericParameters);
                var internalSpecification = new GenericInstanceMethod(module.ImportReference(internalDefinition));
                internalSpecification.GenericArguments.Add(module.TypeSystem.Single); target = internalSpecification;
                break;
            case "instance": target.HasThis = true; break;
            case "vararg": target.CallingConvention = MethodCallingConvention.VarArg; break;
            case "generic":
                target.GenericParameters.Add(new GenericParameter("T", target)); target.CallingConvention = MethodCallingConvention.Generic; break;
            case "extra": target.Parameters.Add(new ParameterDefinition(module.TypeSystem.Int32)); break;
            case "missing": target.Parameters.RemoveAt(2); break;
            case "modopt-owner": target.DeclaringType = new OptionalModifierType(module.TypeSystem.Object, target.DeclaringType); break;
            case "modreq-owner": target.DeclaringType = new RequiredModifierType(module.TypeSystem.Object, target.DeclaringType); break;
            case "byref-owner": target.DeclaringType = new ByReferenceType(target.DeclaringType); break;
            case "pointer-owner": target.DeclaringType = new PointerType(target.DeclaringType); break;
            case "generic-owner":
                var owner = new GenericInstanceType(target.DeclaringType); owner.GenericArguments.Add(module.TypeSystem.Single); target.DeclaringType = owner; break;
            case "foreign-owner":
                var foreign = new AssemblyNameReference("Foreign.Vector.Owner", new Version(1, 0)); module.AssemblyReferences.Add(foreign);
                target.DeclaringType = new TypeReference(target.DeclaringType.Namespace, target.DeclaringType.Name, module, foreign); break;
            case "foreign-vector":
                var foreignVector = new AssemblyNameReference("Foreign.Vector.Value", new Version(1, 0)); module.AssemblyReferences.Add(foreignVector);
                var originalVector = (GenericInstanceType)vector;
                var replacement = new GenericInstanceType(new TypeReference(originalVector.Namespace, originalVector.ElementType.Name, module, foreignVector, true));
                replacement.GenericArguments.Add(single ? module.TypeSystem.Single : module.TypeSystem.Double);
                target.ReturnType = replacement;
                foreach (var parameter in target.Parameters) parameter.ParameterType = replacement;
                break;
            default: throw new ArgumentException("unknown mutation: " + mutation);
        }
        var il = entry.Body.GetILProcessor();
        if (mode[1] != "address")
        {
            entry.Body.InitLocals = true;
            var local = new VariableDefinition(vector); entry.Body.Variables.Add(local);
            il.Emit(OpCodes.Ldloca, local); il.Emit(OpCodes.Initobj, vector);
            if (target.HasThis && mode[1] == "direct") il.Emit(OpCodes.Ldnull);
            for (int i = 0; i < (mode[1] == "direct" && mode[2] == "missing" ? 2 : 3); i++) il.Emit(OpCodes.Ldloc, local);
            if (mode[2] == "extra" && mode[1] == "direct") il.Emit(OpCodes.Ldc_I4_0);
        }
        if (mode[1] == "direct") il.Emit(OpCodes.Call, target);
        else { il.Emit(OpCodes.Ldftn, target); if (mode[1] == "calli") il.Emit(OpCodes.Calli, site); }
        il.Emit(OpCodes.Pop); il.Emit(OpCodes.Ret);
        assembly.Write(output, new WriterParameters { Timestamp = 0, DeterministicMvid = true });
        File.WriteAllText(Path.ChangeExtension(output, ".runtimeconfig.json"), "{\"runtimeOptions\":{\"tfm\":\"net10.0\",\"framework\":{\"name\":\"Microsoft.NETCore.App\",\"version\":\"10.0.0\"}}}\n");
        if (mutation is "spec-zero" or "spec-one" or "spec-internal")
        {
            using var image = File.OpenRead(output);
            using var pe = new System.Reflection.PortableExecutable.PEReader(image);
            var metadata = System.Reflection.Metadata.PEReaderExtensions.GetMetadataReader(pe);
            int rows = System.Reflection.Metadata.Ecma335.MetadataReaderExtensions.GetTableRowCount(metadata, System.Reflection.Metadata.Ecma335.TableIndex.MethodSpec);
            if (rows != 1) throw new InvalidOperationException("expected one serialized MethodSpec");
            var handle = System.Reflection.Metadata.Ecma335.MetadataTokens.MethodSpecificationHandle(1);
            var specification = metadata.GetMethodSpecification(handle);
            string raw = Convert.ToHexString(metadata.GetBlobBytes(specification.Signature));
            string expected = mutation == "spec-zero" ? "0A00" : "0A010C";
            if (raw != expected || specification.Method.Kind != System.Reflection.Metadata.HandleKind.MemberReference)
                throw new InvalidOperationException("unexpected serialized MethodSpec shape: " + raw);
            Console.WriteLine("vector-estimate-methodspec:" + System.Reflection.Metadata.Ecma335.MetadataTokens.GetToken(handle).ToString("X8")
                + ":target=" + System.Reflection.Metadata.Ecma335.MetadataTokens.GetToken(specification.Method).ToString("X8") + ":raw=" + raw);
        }
        Console.WriteLine("vector-estimate-fixture:" + args[3]);
        return true;
    }
}

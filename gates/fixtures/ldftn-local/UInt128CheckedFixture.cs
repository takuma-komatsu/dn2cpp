using System.Reflection.Metadata;
using GenericParameter = Mono.Cecil.GenericParameter;
using AssemblyDefinition = Mono.Cecil.AssemblyDefinition;
using TypeReference = Mono.Cecil.TypeReference;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Globalization;
using Mono.Cecil;
using Mono.Cecil.Cil;

internal static class UInt128CheckedFixture
{
    public static bool Run(string[] args)
    {
        if (args.Length != 4 || args[0] != "--uint128-checked-fixture") return false;
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        string path = Path.GetFullPath(args[1]); string mode = args[3];
        using var app = AssemblyDefinition.ReadAssembly(path, new ReaderParameters { InMemory = true });
        using var core = AssemblyDefinition.ReadAssembly(args[2]);
        var module = app.MainModule; var program = module.GetType("UInt128CheckedSubset.Program");
        var pointer = program.Methods.Single(m => m.Name == "Pointer");
        var factory = pointer.Body.Instructions.Single(i => i.OpCode == OpCodes.Ldftn).Operand as GenericInstanceMethod
            ?? throw new InvalidOperationException("pointer target must be a MethodSpec");
        var indirect = (CallSite)pointer.Body.Instructions.Single(i => i.OpCode == OpCodes.Calli).Operand;
        if (factory.Name != "CreateChecked" || factory.DeclaringType.FullName != "System.UInt128"
            || factory.GenericArguments.Count != 1 || factory.GenericArguments[0] is not GenericParameter { Position: 0, Type: GenericParameterType.Method }
            || indirect.HasThis || indirect.Parameters.Count != 1 || indirect.ReturnType.FullName != "System.UInt128"
            || indirect.Parameters[0].ParameterType is not GenericParameter { Position: 0, Type: GenericParameterType.Method })
            throw new InvalidOperationException("wrong checked pointer ABI");
        if (mode is "all" or "isolated" or "discard")
        {
            if (mode == "isolated")
            {
                var pick = program.NestedTypes.SelectMany(t => t.Methods).Concat(program.Methods)
                    .Single(m => m.Name.Contains("g__Pick", StringComparison.Ordinal));
                if (pick.Body.Instructions.Any(i => i.Operand is MethodReference m && m.Name == "UInt128Route")
                    || !pick.Body.Instructions.Any(i => i.Operand is MethodReference m && m.Name == "UInt128Selected"))
                    throw new InvalidOperationException("isolated fixture retains alternate route selector");
            }
            if (mode == "discard")
            {
                var entry = module.GetType("Entry").Methods.Single(m => m.Name == "Main");
                if (entry.Body.Instructions.Count(i => i.Operand is GenericInstanceMethod m && m.Name == "CreateChecked") != 1
                    || entry.Body.Instructions.Any(i => i.Operand is MethodReference m && m.DeclaringType.FullName == program.FullName))
                    throw new InvalidOperationException("discard fixture must not use arithmetic or other routes");
            }
            Console.WriteLine("uint128-checked-IL:" + mode + ":actual-ldftn-calli");
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
            owner.GenericArguments.Add(module.ImportReference(core.MainModule.GetType("System.UInt128")));
            var reference = module.ImportReference(method);
            reference.DeclaringType = owner;
            entry.Body = new MethodBody(entry) { InitLocals = true };
            var value = new VariableDefinition(module.ImportReference(core.MainModule.GetType("System.UInt128"))); entry.Body.Variables.Add(value);
            var il = entry.Body.GetILProcessor(); il.Emit(OpCodes.Ldloc_0); il.Emit(OpCodes.Call, reference); il.Emit(OpCodes.Pop); il.Emit(OpCodes.Ret);
            Console.WriteLine("uint128-checked-retained:actual-IBinaryInteger-UInt128-LeadingZeroCount");
        }
        else if (mode.StartsWith("owner-", StringComparison.Ordinal) || mode.StartsWith("prefix-", StringComparison.Ordinal))
        {
            var method = program.Methods.Single(m => m.Name == "ViaConstraint");
            var call = method.Body.Instructions.Single(i => i.OpCode == OpCodes.Call);
            var spec = (GenericInstanceMethod)call.Operand;
            string mutation = mode[(mode.IndexOf('-') + 1)..];
            TypeReference Modify(TypeReference argument) => mutation switch
            {
                "count" or "trailing" or "modopt" => new OptionalModifierType(module.TypeSystem.Object, argument),
                "modreq" => new RequiredModifierType(module.TypeSystem.Object, argument),
                "pinned" => new PinnedType(argument),
                _ => throw new InvalidOperationException("unknown owner mutation")
            };
            if (mode.StartsWith("owner-", StringComparison.Ordinal))
            {
                var owner = (GenericInstanceType)spec.ElementMethod.DeclaringType;
                if (mode == "owner-extra") owner.GenericArguments.Add(owner.GenericArguments[0]);
                else if (mode == "owner-zero") owner.GenericArguments.Clear();
                else owner.GenericArguments[0] = Modify(owner.GenericArguments[0]);
                if (mode is "owner-extra" or "owner-zero")
                {
                    var valueType = module.ImportReference(core.MainModule.GetType("System.UInt128"));
                    var identity = new GenericInstanceMethod(method);
                    identity.GenericArguments.Add(valueType);
                    identity.GenericArguments.Add(valueType);
                    // Root only identity, through existing public entry points and same-type private access.
                    foreach (var root in program.Methods.Where(m => m.Name is "RunSelected" or "__GateEntry"))
                    {
                        root.Body = new MethodBody(root) { InitLocals = true };
                        root.Body.Variables.Add(new VariableDefinition(valueType));
                        var il = root.Body.GetILProcessor();
                        il.Emit(OpCodes.Ldloc_0);
                        il.Emit(OpCodes.Call, identity);
                        il.Emit(OpCodes.Pop);
                        il.Emit(OpCodes.Ret);
                    }
                    Console.WriteLine("uint128-checked-IL:" + mode + ":identity-only-owner-arity");
                }
            }
            else
            {
                var prefix = method.Body.Instructions.Single(i => i.OpCode == OpCodes.Constrained);
                prefix.Operand = Modify((TypeReference)prefix.Operand);
            }
        }
        else
        {
            bool address = mode.StartsWith("address-", StringComparison.Ordinal);
            string mutation = mode[(mode.IndexOf('-') + 1)..];
            var target = core.MainModule.GetType("System.UInt128").Methods.Single(m => m.Name == "CreateChecked");
            var reference = module.ImportReference(target);
            switch (mutation)
            {
                case "nested-return-modopt": case "nested-return-modreq": case "nested-return-pinned": break;
                case "arg-count": case "arg-trailing": case "arg-modopt": case "arg-modreq": case "arg-pinned": break;
                case "return": reference.ReturnType = module.ImportReference(core.MainModule.GetType("System.Int128")); break;
                case "byref": reference.Parameters[0].ParameterType = new ByReferenceType(reference.Parameters[0].ParameterType); break;
                case "ref-trailing": case "modifier": reference.ReturnType = new OptionalModifierType(module.ImportReference(typeof(object)), reference.ReturnType); break;
                case "instance": reference.HasThis = true; break;
                case "mvar": var wrong = new GenericParameter("Wrong", reference); reference.GenericParameters.Add(wrong); reference.Parameters[0].ParameterType = wrong; break;
                default: throw new InvalidOperationException("unknown malformed mode");
            }
            var spec = new GenericInstanceMethod(reference);
            TypeReference argument = module.TypeSystem.Int64;
            if (mutation is "arg-modopt" or "arg-count" or "arg-trailing") argument = new OptionalModifierType(module.TypeSystem.Object, argument);
            if (mutation == "arg-modreq") argument = new RequiredModifierType(module.TypeSystem.Object, argument);
            if (mutation == "arg-pinned") argument = new PinnedType(argument);
            spec.GenericArguments.Add(argument);
            var entry = module.GetType("Entry").Methods.Single(m => m.Name == "Main"); entry.Body = new MethodBody(entry);
            var il = entry.Body.GetILProcessor();
            if (!address) il.Emit(OpCodes.Ldc_I8, -7L);
            il.Emit(address ? OpCodes.Ldftn : OpCodes.Call, spec); il.Emit(OpCodes.Pop); il.Emit(OpCodes.Ret);
        }
        if (mode.Contains("nested-return-", StringComparison.Ordinal))
        {
            TypeReference value = module.ImportReference(core.MainModule.GetType("System.UInt128"));
            TypeReference modified = mode.EndsWith("modopt", StringComparison.Ordinal)
                ? new OptionalModifierType(module.TypeSystem.Object, value)
                : mode.EndsWith("modreq", StringComparison.Ordinal)
                    ? new RequiredModifierType(module.TypeSystem.Object, value) : new PinnedType(value);
            // An unreachable token operand forces a real TypeSpec row without running malformed IL.
            var entry = module.GetType("Entry").Methods.Single(m => m.Name == "Main");
            entry.Body.GetILProcessor().Emit(OpCodes.Ldtoken, modified);
            entry.Body.GetILProcessor().Emit(OpCodes.Pop);
            entry.Body.GetILProcessor().Emit(OpCodes.Ret);
        }
        app.Write(path, new WriterParameters { Timestamp = 0, DeterministicMvid = true });
        if (mode is "owner-extra" or "owner-zero")
        {
            using var stream = File.OpenRead(path);
            using var pe = new PEReader(stream);
            var reader = pe.GetMetadataReader();
            int expected = mode == "owner-extra" ? 2 : 0;
            int matches = 0;
            for (int row = 1; row <= reader.GetTableRowCount(TableIndex.TypeSpec); row++)
            {
                var signature = reader.GetTypeSpecification(MetadataTokens.TypeSpecificationHandle(row)).Signature;
                var blob = reader.GetBlobReader(signature);
                if (blob.RemainingBytes < 4 || blob.ReadByte() != 21 || blob.ReadByte() != 18) continue;
                int coded = blob.ReadCompressedInteger();
                if ((coded & 3) != 1 || (coded >> 2) == 0) continue;
                var declaring = reader.GetTypeReference(MetadataTokens.TypeReferenceHandle(coded >> 2));
                if (reader.GetString(declaring.Namespace) != "System.Numerics" || reader.GetString(declaring.Name) != "INumberBase`1") continue;
                int count = blob.ReadCompressedInteger();
                if (count != expected) continue;
                for (int i = 0; i < count; i++)
                    if (blob.RemainingBytes < 2 || blob.ReadByte() != 30 || blob.ReadCompressedInteger() != 0)
                        throw new InvalidOperationException("owner arity fixture must encode complete MVAR0 arguments");
                if (blob.RemainingBytes != 0) throw new InvalidOperationException("owner arity fixture has trailing bytes");
                Console.WriteLine("uint128-checked-IL:" + mode + ":INumberBase-count=" + count
                    + ":blob=" + Convert.ToHexString(reader.GetBlobBytes(signature)));
                matches++;
            }
            if (matches != 1) throw new InvalidOperationException("owner arity fixture must retain exactly one malformed GENERICINST owner");
        }
        if (mode.EndsWith("-ref-trailing", StringComparison.Ordinal))
        {
            byte[] bytes = File.ReadAllBytes(path);
            using var pe = new PEReader(new MemoryStream(bytes));
            var reader = pe.GetMetadataReader();
            bool patched = false;
            foreach (var handle in reader.MemberReferences)
            {
                var member = reader.GetMemberReference(handle);
                if (reader.GetString(member.Name) != "CreateChecked" || member.Parent.Kind != HandleKind.TypeReference) continue;
                var declaring = reader.GetTypeReference((TypeReferenceHandle)member.Parent);
                if (reader.GetString(declaring.Namespace) != "System" || reader.GetString(declaring.Name) != "UInt128") continue;
                byte[] blob = reader.GetBlobBytes(member.Signature);
                if (blob.Length < 9 || blob[0] != 16 || blob[3] != 32) continue;
                if (blob[4] >= 128 || blob[^2] != 30 || blob[^1] != 0)
                    throw new InvalidOperationException("reference trailing placeholder must end with MVAR0");
                int offset = pe.PEHeaders.MetadataStartOffset + reader.GetHeapMetadataOffset(HeapIndex.Blob) + MetadataTokens.GetHeapOffset(member.Signature);
                offset += bytes[offset] < 0x80 ? 1 : bytes[offset] < 0xc0 ? 2 : 4;
                Array.Copy(blob, 5, bytes, offset + 3, blob.Length - 5);
                patched = true;
            }
            if (!patched) throw new InvalidOperationException("missing trailing MemberRef mutation target");
            File.WriteAllBytes(path, bytes);
        }
        if (mode is "owner-count" or "owner-trailing" or "prefix-trailing")
        {
            byte[] bytes = File.ReadAllBytes(path);
            using var pe = new PEReader(new MemoryStream(bytes));
            var reader = pe.GetMetadataReader();
            bool patched = false;
            for (int row = 1; row <= reader.GetTableRowCount(TableIndex.TypeSpec); row++)
            {
                var signature = reader.GetTypeSpecification(MetadataTokens.TypeSpecificationHandle(row)).Signature;
                byte[] blob = reader.GetBlobBytes(signature);
                bool owner = mode.StartsWith("owner-", StringComparison.Ordinal);
                int index;
                if (owner)
                {
                    if (blob.Length < 8 || blob[0] != 21 || blob[1] != 18 || blob[2] >= 128 || blob[3] != 1 || blob[4] != 32) continue;
                    index = mode == "owner-count" ? 3 : 4;
                }
                else
                {
                    if (blob.Length < 4 || blob[0] != 32 || blob[^2] != 30 || blob[^1] != 0) continue;
                    index = 0;
                }
                int offset = pe.PEHeaders.MetadataStartOffset + reader.GetHeapMetadataOffset(HeapIndex.Blob) + MetadataTokens.GetHeapOffset(signature);
                offset += bytes[offset] < 0x80 ? 1 : bytes[offset] < 0xc0 ? 2 : 4;
                if (mode == "owner-count") bytes[offset + index] = 2;
                else
                {
                    if (blob[index + 1] >= 128 || blob[^2] != 30 || blob[^1] != 0)
                        throw new InvalidOperationException("trailing placeholder must preserve MVAR0 authority");
                    bytes[offset + index] = 30;
                    bytes[offset + index + 1] = 0;
                }
                patched = true;
            }
            if (!patched) throw new InvalidOperationException("missing owner/prefix raw mutation target");
            File.WriteAllBytes(path, bytes);
        }
        if (mode.Contains("nested-return-", StringComparison.Ordinal))
        {
            byte[] bytes = File.ReadAllBytes(path);
            using var pe = new PEReader(new MemoryStream(bytes));
            var reader = pe.GetMetadataReader();
            int tag = mode.EndsWith("modopt", StringComparison.Ordinal) ? 32 : mode.EndsWith("modreq", StringComparison.Ordinal) ? 31 : 69;
            var nested = Enumerable.Range(1, reader.GetTableRowCount(TableIndex.TypeSpec))
                .Select(MetadataTokens.TypeSpecificationHandle)
                .Single(h => reader.GetBlobBytes(reader.GetTypeSpecification(h).Signature)[0] == tag);
            int token = (MetadataTokens.GetRowNumber(nested) << 2) | 2;
            bool patched = false;
            foreach (var handle in reader.MemberReferences)
            {
                var member = reader.GetMemberReference(handle);
                if (reader.GetString(member.Name) != "CreateChecked" || member.Parent.Kind != HandleKind.TypeReference) continue;
                var declaring = reader.GetTypeReference((TypeReferenceHandle)member.Parent);
                if (reader.GetString(declaring.Namespace) != "System" || reader.GetString(declaring.Name) != "UInt128") continue;
                byte[] blob = reader.GetBlobBytes(member.Signature);
                if (blob.Length < 7 || blob[0] != 16 || blob[3] != 17) continue;
                if (blob[4] >= 128 || token >= 128) throw new InvalidOperationException("nested return token needs wider placeholder");
                int offset = pe.PEHeaders.MetadataStartOffset + reader.GetHeapMetadataOffset(HeapIndex.Blob) + MetadataTokens.GetHeapOffset(member.Signature);
                offset += bytes[offset] < 0x80 ? 1 : bytes[offset] < 0xc0 ? 2 : 4;
                bytes[offset + 4] = (byte)token;
                patched = true;
            }
            if (!patched) throw new InvalidOperationException("missing nested return MemberRef");
            File.WriteAllBytes(path, bytes);
        }
        if (mode.EndsWith("-arg-count", StringComparison.Ordinal) || mode.EndsWith("-arg-trailing", StringComparison.Ordinal))
        {
            byte[] bytes = File.ReadAllBytes(path);
            using var pe = new PEReader(new MemoryStream(bytes));
            var reader = pe.GetMetadataReader();
            bool patched = false;
            for (int row = 1; row <= reader.GetTableRowCount(TableIndex.MethodSpec); row++)
            {
                var signature = reader.GetMethodSpecification(MetadataTokens.MethodSpecificationHandle(row)).Signature;
                byte[] blob = reader.GetBlobBytes(signature);
                if (blob.Length < 5 || blob[0] != 10 || blob[1] != 1 || blob[2] != 32) continue;
                int offset = pe.PEHeaders.MetadataStartOffset + reader.GetHeapMetadataOffset(HeapIndex.Blob) + MetadataTokens.GetHeapOffset(signature);
                offset += bytes[offset] < 0x80 ? 1 : bytes[offset] < 0xc0 ? 2 : 4;
                if (mode.EndsWith("-arg-count", StringComparison.Ordinal)) bytes[offset + 1] = 2;
                else bytes[offset + 2] = 10; // Int64 followed by unconsumed bytes.
                patched = true;
            }
            if (!patched) throw new InvalidOperationException("missing raw MethodSpec mutation target");
            File.WriteAllBytes(path, bytes);
        }
        return true;
    }
}

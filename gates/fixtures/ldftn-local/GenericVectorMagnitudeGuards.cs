using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using Mono.Cecil.Cil;

internal static class GenericVectorMagnitudeGuards
{
    internal static void Check(object compilation, object core, string corePath, string operation,
        object[] controls, Action<bool, string> check)
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        object Get(object value, string name) => value.GetType().GetField(name, flags) is { } field ? field.GetValue(value)! : value.GetType().GetProperty(name, flags)!.GetValue(value)!;
        void Set(object value, string name, object replacement) => value.GetType().GetField(name, flags)!.SetValue(value, replacement);
        var guard = compilation.GetType().GetMethod("ResolveVectorMagnitudeManagedGuard", flags)!;
        var resolve = compilation.GetType().GetMethod("TryResolveVectorMagnitudeSpec", flags)!;
        var originalPe = (PEReader)Get(core, "PE");
        var reader = (MetadataReader)Get(core, "Reader");
        var byReader = (System.Collections.IDictionary)Get(compilation, "_byReader");
        var originalImage = File.ReadAllBytes(corePath);
        using var cecil = Mono.Cecil.AssemblyDefinition.ReadAssembly(corePath);
        int BodyOffset(int rva)
        {
            var section = originalPe.PEHeaders.SectionHeaders.Single(s => rva >= s.VirtualAddress && rva < s.VirtualAddress + s.SizeOfRawData);
            int offset = section.PointerToRawData + rva - section.VirtualAddress;
            return offset + ((originalImage[offset] & 3) == 2 ? 1 : (originalImage[offset + 1] >> 4) * 4);
        }
        foreach (var method in controls)
        {
            var vector = Get(Get(Get(method, "Signature"), "ReturnType"), "Class");
            var ownerContext = Get(vector, "Context");
            var vectorDefinition = reader.GetTypeDefinition((TypeDefinitionHandle)Get(vector, "Handle"));
            var bridge = vectorDefinition.GetMethods().Single(h => reader.GetString(reader.GetMethodDefinition(h).Name).EndsWith("." + operation, StringComparison.Ordinal));
            var bridgeIl = originalPe.GetMethodBody(reader.GetMethodDefinition(bridge).RelativeVirtualAddress).GetILBytes()!;
            check(bridgeIl.Length == 8 && bridgeIl[0] == 2 && bridgeIl[1] == 3 && bridgeIl[2] == 0x28 && bridgeIl[7] == 0x2a, "magnitude-bridge-real-il");
            var bridgeSpec = (MethodSpecificationHandle)MetadataTokens.EntityHandle(BitConverter.ToInt32(bridgeIl, 3));
            var specification = reader.GetMethodSpecification(bridgeSpec);
            check(specification.Method == (EntityHandle)(MethodDefinitionHandle)Get(method, "Handle") && Convert.ToHexString(reader.GetBlobBytes(specification.Signature)) == "0A011300", "magnitude-bridge-exact-var");
            object Resolve() => resolve.Invoke(compilation, new[] { core, (object)bridgeSpec, ownerContext })!;
            check(ReferenceEquals(Resolve(), method), "magnitude-bridge-target");
            var count = guard.Invoke(compilation, new[] { method })!;
            check(ReferenceEquals(Get(count, "DeclaringClass"), vector), "magnitude-count-exact-vector");
            var countHandle = (MethodDefinitionHandle)Get(count, "Handle");
            var countDef = reader.GetMethodDefinition(countHandle);
            var countIl = originalPe.GetMethodBody(countDef.RelativeVirtualAddress).GetILBytes()!;
            check(countIl[0] == 0x28 && countIl[4] == 0x2b, "magnitude-count-first-guard");
            var helperSpec = reader.GetMethodSpecification((MethodSpecificationHandle)MetadataTokens.EntityHandle(BitConverter.ToInt32(countIl, 1)));
            var helperDef = reader.GetMethodDefinition((MethodDefinitionHandle)helperSpec.Method);
            check(reader.GetString(helperDef.Name) == "ThrowForUnsupportedNumericsVectorBaseType", "magnitude-count-real-helper");
            var helperIl = originalPe.GetMethodBody(helperDef.RelativeVirtualAddress).GetILBytes()!;
            var mutations = new List<(int Offset, byte[] Value, string Name)>();
            void Byte(int offset, byte value, string name)
            {
                check(originalImage[offset] != value, "mutation-changes-" + name);
                mutations.Add((offset, new[] { value }, name));
            }
            void ChangeInstruction(Mono.Cecil.MethodDefinition definition, Instruction instruction, string label)
            {
                int offset = BodyOffset(definition.RVA) + instruction.Offset;
                if (instruction.OpCode == OpCodes.Call) Byte(offset, 0x6f, label + "-callvirt");
                else if (instruction.OpCode == OpCodes.Brtrue_S) Byte(offset, 0x2c, label + "-branch-inverted");
                else if (instruction.OpCode == OpCodes.Brfalse_S) Byte(offset, 0x2d, label + "-branch-inverted");
                else if (instruction.OpCode == OpCodes.Br_S) Byte(offset, 0x2c, label + "-entry-conditional");
            }
            var hardware = cecil.MainModule.GetType("System.Numerics.Vector").Methods.Single(m => m.Name == "get_IsHardwareAccelerated");
            ChangeInstruction(hardware, hardware.Body.Instructions[0], "magnitude-hardware-query");
            Byte(BodyOffset(hardware.RVA) + hardware.Body.Instructions[^1].Offset, 0, "magnitude-hardware-query-return");
            var absBridge = cecil.MainModule.GetType("System.Numerics.Vector`1").Methods.Single(m => m.Name.EndsWith(".Abs", StringComparison.Ordinal));
            check(absBridge.Body.Instructions.Count == 3, "magnitude-abs-bridge-shape");
            ChangeInstruction(absBridge, absBridge.Body.Instructions[1], "magnitude-abs-bridge");
            Byte(BodyOffset(absBridge.RVA), 3, "magnitude-abs-bridge-operand");
            var declaration = (Mono.Cecil.MethodDefinition)cecil.MainModule.LookupToken(MetadataTokens.GetToken((MethodDefinitionHandle)Get(method, "Handle")));
            foreach (var instruction in declaration.Body.Instructions)
            {
                if (instruction.Operand is Mono.Cecil.MethodReference call && (call.Name == "get_IsHardwareAccelerated" || call.Name == "get_Count" || call.DeclaringType.Name == "VectorMath" || call.Name == "SkipInit"))
                    ChangeInstruction(declaration, instruction, "magnitude-public-" + instruction.Offset);
            }
            foreach (var instruction in declaration.Body.Instructions.Where(i => i.OpCode.FlowControl == FlowControl.Cond_Branch || i.OpCode == OpCodes.Br_S).Take(2))
                ChangeInstruction(declaration, instruction, "magnitude-public-branch-" + instruction.Offset);
            var math = cecil.MainModule.GetType("System.Runtime.Intrinsics.VectorMath");
            var numberName = operation.EndsWith("Number", StringComparison.Ordinal) ? operation : operation + "Number";
            void TypeTests(Mono.Cecil.MethodDefinition definition, int groups)
            {
                foreach (var instruction in definition.Body.Instructions.Take(groups * 6))
                {
                    ChangeInstruction(definition, instruction, "magnitude-types-" + definition.Name + "-" + instruction.Offset);
                    if (instruction.OpCode == OpCodes.Ldtoken)
                    {
                        int offset = BodyOffset(definition.RVA) + instruction.Offset + 1;
                        Byte(offset, (byte)(originalImage[offset] ^ 1), "magnitude-type-token-" + definition.Name + "-" + instruction.Offset);
                    }
                }
            }
            foreach (var definition in math.Methods.Where(m => m.Name == operation || m.Name == numberName))
            {
                int groups = definition.Name.EndsWith("Number", StringComparison.Ordinal) ? 5 : 2;
                TypeTests(definition, groups);
                var selected = (Instruction)definition.Body.Instructions[groups * 6 - 1].Operand;
                for (var instruction = selected; instruction is not null; instruction = instruction.Next)
                {
                    ChangeInstruction(definition, instruction, "magnitude-math-" + definition.Name + "-" + instruction.Offset);
                    if (instruction.OpCode == OpCodes.Constrained)
                    {
                        int offset = BodyOffset(definition.RVA) + instruction.Offset + 2;
                        Byte(offset, (byte)(originalImage[offset] ^ 1), "magnitude-constrained-target");
                    }
                    if (instruction.Operand is Mono.Cecil.MethodReference call && (call.Name == "Abs" || call.Name == numberName)) break;
                }
            }
            var abs = cecil.MainModule.GetType("System.Numerics.Vector").Methods.Single(m => m.Name == "Abs" && m.HasGenericParameters);
            TypeTests(abs, 5);
            var absStart = (Instruction)abs.Body.Instructions[29].Operand;
            for (var instruction = absStart; instruction is not null; instruction = instruction.Next)
            {
                ChangeInstruction(abs, instruction, "magnitude-abs-" + instruction.Offset);
                if (instruction.OpCode == OpCodes.Br_S)
                {
                    var condition = ((Instruction)instruction.Operand).Next;
                    ChangeInstruction(abs, condition, "magnitude-abs-count");
                    break;
                }
            }
            foreach (var owner in new[] { declaration.DeclaringType, math, absBridge.DeclaringType })
            {
                var unrelated = owner.Methods.First(m => m.Name != operation && m.Name != numberName && m.Name != "Abs" && !m.Name.EndsWith(".Abs", StringComparison.Ordinal));
                var cctorName = reader.MethodDefinitions.Select(reader.GetMethodDefinition).First(m => reader.GetString(m.Name) == ".cctor").Name;
                int nameSize = reader.GetHeapSize(HeapIndex.String) > ushort.MaxValue ? 4 : 2;
                int row = originalPe.PEHeaders.MetadataStartOffset + reader.GetTableMetadataOffset(TableIndex.MethodDef)
                    + ((int)unrelated.MetadataToken.RID - 1) * reader.GetTableRowSize(TableIndex.MethodDef) + 8;
                mutations.Add((row, BitConverter.GetBytes(MetadataTokens.GetHeapOffset(cctorName)).Take(nameSize).ToArray(), "magnitude-owner-cctor-" + owner.FullName));
            }
            var mathCall = declaration.Body.Instructions.Select(i => i.Operand).OfType<Mono.Cecil.GenericInstanceMethod>().Single(m => m.DeclaringType.Name == "VectorMath");
            var mathSpec = reader.GetMethodSpecification((MethodSpecificationHandle)MetadataTokens.EntityHandle(mathCall.MetadataToken.ToInt32()));
            int blobOffset = originalPe.PEHeaders.MetadataStartOffset + reader.GetHeapMetadataOffset(HeapIndex.Blob) + MetadataTokens.GetHeapOffset(mathSpec.Signature);
            blobOffset += originalImage[blobOffset] < 0x80 ? 1 : originalImage[blobOffset] < 0xc0 ? 2 : 4;
            int blobLength = reader.GetBlobBytes(mathSpec.Signature).Length;
            Byte(blobOffset + blobLength - 1, 1, "magnitude-hardware-wrong-element-variable");
            Byte(BodyOffset(countDef.RelativeVirtualAddress), 0x6f, "magnitude-count-callvirt");
            Byte(BodyOffset(countDef.RelativeVirtualAddress) + 4, 0x0a, "magnitude-count-wrong-target-kind");
            Byte(BodyOffset(helperDef.RelativeVirtualAddress) + 5, 0x2c, "magnitude-helper-inverted-condition");
            Byte(BodyOffset(helperDef.RelativeVirtualAddress) + helperIl.Length - 1, 0, "magnitude-helper-missing-ret");
            check(helperIl[7] == 0x1f && helperIl[8] == 0x43, "magnitude-original-resource");
            Byte(BodyOffset(helperDef.RelativeVirtualAddress) + 8, 0x42, "magnitude-helper-resource");
            var supported = absBridge.DeclaringType.Methods.Single(m => m.Name == "get_IsSupported");
            var whitelistToken = supported.Body.Instructions.First(i => i.OpCode == OpCodes.Ldtoken);
            int whitelistOffset = BodyOffset(supported.RVA) + whitelistToken.Offset + 1;
            Byte(whitelistOffset, (byte)(originalImage[whitelistOffset] ^ 1), "magnitude-unsupported-whitelist-type");
            foreach (var definition in math.Methods.Where(m => m.Name == operation || m.Name == numberName))
            {
                var handle = (MethodDefinitionHandle)MetadataTokens.EntityHandle(definition.MetadataToken.ToInt32());
                foreach (var gpHandle in reader.GetMethodDefinition(handle).GetGenericParameters())
                {
                    int gpOffset = originalPe.PEHeaders.MetadataStartOffset + reader.GetTableMetadataOffset(TableIndex.GenericParam)
                        + (MetadataTokens.GetRowNumber(gpHandle) - 1) * reader.GetTableRowSize(TableIndex.GenericParam) + 2;
                    Byte(gpOffset, (byte)(originalImage[gpOffset] ^ 1), "magnitude-math-generic-variance-" + definition.Name + "-" + MetadataTokens.GetRowNumber(gpHandle));
                }
            }
            var interfaceAbs = cecil.MainModule.GetType("System.Runtime.Intrinsics.ISimdVector`2").Methods.Single(m => m.Name == "Abs");
            int interfaceFlags = originalPe.PEHeaders.MetadataStartOffset + reader.GetTableMetadataOffset(TableIndex.MethodDef)
                + ((int)interfaceAbs.MetadataToken.RID - 1) * reader.GetTableRowSize(TableIndex.MethodDef) + 6;
            Byte(interfaceFlags, (byte)(originalImage[interfaceFlags] ^ 0x10), "magnitude-interface-abs-static");

            var mathResolver = compilation.GetType().GetMethod("ResolveMagnitudeMathCall", flags)!;
            var absBridgeResolver = compilation.GetType().GetMethod("ResolveMagnitudeAbsBridge", flags)!;
            var absResolver = compilation.GetType().GetMethod("ResolveMagnitudeAbsCall", flags)!;
            var mathMethod = mathResolver.Invoke(compilation, new[] { method, (object)mathCall.MetadataToken.ToInt32(), operation })!;
            object numberMethod = mathMethod;
            if (operation != numberName)
            {
                var plainDefinition = math.Methods.Single(m => m.Name == operation);
                var numberCall = plainDefinition.Body.Instructions.Select(i => i.Operand).OfType<Mono.Cecil.GenericInstanceMethod>().Single(m => m.Name == numberName);
                numberMethod = mathResolver.Invoke(compilation, new[] { mathMethod, (object)numberCall.MetadataToken.ToInt32(), numberName })!;
            }
            var numberDefinition = math.Methods.Single(m => m.Name == numberName);
            var absReference = numberDefinition.Body.Instructions.Select(i => i.Operand).OfType<Mono.Cecil.MethodReference>().First(m => m.Name == "Abs");
            var resolvedBridge = absBridgeResolver.Invoke(compilation, new[] { numberMethod, (object)absReference.MetadataToken.ToInt32(), vector })!;
            var aliases = ((System.Collections.IEnumerable)Get(vector, "Methods")).Cast<object>()
                .Where(candidate => (string)Get(candidate, "Name") == (string)Get(resolvedBridge, "Name")).ToArray();
            Console.WriteLine("magnitude-bridge-model-aliases:total=" + aliases.Length + ":unused=" + aliases.Count(candidate => !ReferenceEquals(candidate, resolvedBridge)));
            var publicAbsCall = absBridge.Body.Instructions.Select(i => i.Operand).OfType<Mono.Cecil.GenericInstanceMethod>().Single();
            var resolvedAbs = absResolver.Invoke(compilation, new[] { resolvedBridge, (object)publicAbsCall.MetadataToken.ToInt32(), vector })!;
            var dependencies = new[] { mathMethod, numberMethod, resolvedBridge, resolvedAbs }.Distinct().ToArray();
            check(dependencies.Length == (operation == numberName ? 3 : 4), "magnitude-exact-resolved-proof-dependencies");
            void PoisonDependency(object value, string field, object replacement, string label)
            {
                var saved = Get(value, field); Set(value, field, replacement);
                try
                {
                    bool refused = false;
                    try { guard.Invoke(compilation, new[] { method }); }
                    catch (TargetInvocationException error) when (error.InnerException is NotSupportedException) { refused = true; }
                    check(refused, "magnitude-cache-" + label + "-" + field);
                    refused = false;
                    try { Resolve(); }
                    catch (TargetInvocationException error) when (error.InnerException is NotSupportedException) { refused = true; }
                    check(refused, "magnitude-cache-resolver-" + label + "-" + field);
                }
                finally { Set(value, field, saved); }
                check(ReferenceEquals(Resolve(), method), "magnitude-cache-restored-" + label + "-" + field);
            }
            foreach (var dependency in dependencies)
            {
                string label = (string)Get(dependency, "Name");
                PoisonDependency(dependency, "Name", "InvalidGuardDependency", label);
                PoisonDependency(dependency, "Rva", 1, label);
                PoisonDependency(dependency, "Handle", default(MethodDefinitionHandle), label);
                PoisonDependency(dependency, "IsSynthetic", true, label);
                PoisonDependency(dependency, "Attributes", MethodAttributes.Private, label);
                PoisonDependency(dependency, "ImplAttributes", MethodImplAttributes.InternalCall, label);
                PoisonDependency(Get(dependency, "DeclaringClass"), "Handle", default(TypeDefinitionHandle), label);
            }
            foreach (var mutation in mutations)
            {
                var changed = (byte[])originalImage.Clone(); mutation.Value.CopyTo(changed, mutation.Offset);
                using var modified = new PEReader(new MemoryStream(changed, writable: false));
                var changedReader = modified.GetMetadataReader(); byReader.Add(changedReader, core);
                Set(core, "PE", modified); Set(core, "Reader", changedReader);
                try
                {
                    bool refused = false;
                    try { guard.Invoke(compilation, new[] { method }); }
                    catch (TargetInvocationException error) when (error.InnerException is NotSupportedException) { refused = true; }
                    check(refused, mutation.Name + ":guard");
                    refused = false;
                    try { Resolve(); }
                    catch (TargetInvocationException error) when (error.InnerException is NotSupportedException) { refused = true; }
                    check(refused, mutation.Name + ":precache");
                }
                finally { Set(core, "PE", originalPe); Set(core, "Reader", reader); byReader.Remove(changedReader); }
                check(ReferenceEquals(Resolve(), method), mutation.Name + ":restored");
            }
        }
    }
}

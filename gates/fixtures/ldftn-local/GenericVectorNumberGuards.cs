using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using Mono.Cecil.Cil;

internal static class GenericVectorNumberGuards
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
            check(bridgeIl.Length == 8 && bridgeIl[0] == 2 && bridgeIl[1] == 3 && bridgeIl[2] == 0x28 && bridgeIl[7] == 0x2a, "number-bridge-real-il");
            var bridgeSpec = (MethodSpecificationHandle)MetadataTokens.EntityHandle(BitConverter.ToInt32(bridgeIl, 3));
            var specification = reader.GetMethodSpecification(bridgeSpec);
            check(specification.Method == (EntityHandle)(MethodDefinitionHandle)Get(method, "Handle") && Convert.ToHexString(reader.GetBlobBytes(specification.Signature)) == "0A011300", "number-bridge-exact-var");
            object Resolve() => resolve.Invoke(compilation, new[] { core, (object)bridgeSpec, ownerContext })!;
            check(ReferenceEquals(Resolve(), method), "number-bridge-target");
            var count = guard.Invoke(compilation, new[] { method })!;
            check(ReferenceEquals(Get(count, "DeclaringClass"), vector), "number-count-exact-vector");
            var countHandle = (MethodDefinitionHandle)Get(count, "Handle");
            var countDef = reader.GetMethodDefinition(countHandle);
            var countIl = originalPe.GetMethodBody(countDef.RelativeVirtualAddress).GetILBytes()!;
            check(countIl[0] == 0x28 && countIl[4] == 0x2b, "number-count-first-guard");
            var helperSpec = reader.GetMethodSpecification((MethodSpecificationHandle)MetadataTokens.EntityHandle(BitConverter.ToInt32(countIl, 1)));
            var helperDef = reader.GetMethodDefinition((MethodDefinitionHandle)helperSpec.Method);
            check(reader.GetString(helperDef.Name) == "ThrowForUnsupportedNumericsVectorBaseType", "number-count-real-helper");
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
            ChangeInstruction(hardware, hardware.Body.Instructions[0], "number-hardware-query");
            Byte(BodyOffset(hardware.RVA) + hardware.Body.Instructions[^1].Offset, 0, "number-hardware-query-return");
            string selectedName = operation == "MaxNumber" ? "Max" : "Min";
            string comparisonName = operation == "MaxNumber" ? "GreaterThan" : "LessThan";
            var publicVector = cecil.MainModule.GetType("System.Numerics.Vector");
            var closedVector = cecil.MainModule.GetType("System.Numerics.Vector`1");
            var math = cecil.MainModule.GetType("System.Runtime.Intrinsics.VectorMath");
            var contract = cecil.MainModule.GetType("System.Runtime.Intrinsics.ISimdVector`2");
            var declaration = (Mono.Cecil.MethodDefinition)cecil.MainModule.LookupToken(MetadataTokens.GetToken((MethodDefinitionHandle)Get(method, "Handle")));
            var selectedPublic = publicVector.Methods.Single(m => m.Name == selectedName && m.HasGenericParameters);
            var comparison = publicVector.Methods.Single(m => m.Name == comparisonName && m.HasGenericParameters);
            var proofNames = new[] { operation, selectedName, comparisonName };
            var bridges = proofNames.Select(name => closedVector.Methods.Single(m => m.Name.EndsWith("." + name, StringComparison.Ordinal))).ToArray();
            foreach (var body in new[] { declaration, selectedPublic, comparison })
            {
                foreach (var instruction in body.Body.Instructions)
                {
                    if (instruction.Operand is Mono.Cecil.MethodReference call && (call.Name == "get_IsHardwareAccelerated" || call.Name == "get_Count" || call.DeclaringType.Name == "VectorMath" || call.Name == "SkipInit"))
                        ChangeInstruction(body, instruction, "number-public-" + body.Name + "-" + instruction.Offset);
                }
                foreach (var instruction in body.Body.Instructions.Where(i => i.OpCode.FlowControl == FlowControl.Cond_Branch || i.OpCode == OpCodes.Br_S).Take(body == comparison ? 1 : 2))
                    ChangeInstruction(body, instruction, "number-public-branch-" + body.Name + "-" + instruction.Offset);
            }
            foreach (var body in bridges.Skip(1))
            {
                check(body.Body.Instructions.Count == 4, "number-binary-bridge-shape");
                Byte(BodyOffset(body.RVA), 3, "number-bridge-left-operand-" + body.Name);
                Byte(BodyOffset(body.RVA) + body.Body.Instructions[1].Offset, 2, "number-bridge-right-operand-" + body.Name);
                ChangeInstruction(body, body.Body.Instructions[2], "number-bridge-" + body.Name);
            }
            foreach (var definition in math.Methods.Where(m => m.Name == operation || m.Name == selectedName))
            {
                foreach (var instruction in definition.Body.Instructions.Take(12))
                {
                    ChangeInstruction(definition, instruction, "number-type-test-" + definition.Name + "-" + instruction.Offset);
                    if (instruction.OpCode == OpCodes.Ldtoken)
                    {
                        int offset = BodyOffset(definition.RVA) + instruction.Offset + 1;
                        Byte(offset, (byte)(originalImage[offset] ^ 1), "number-type-token-" + definition.Name + "-" + instruction.Offset);
                    }
                }
                var selected = (Instruction)definition.Body.Instructions[11].Operand;
                for (var instruction = selected; instruction is not null; instruction = instruction.Next)
                {
                    ChangeInstruction(definition, instruction, "number-math-" + definition.Name + "-" + instruction.Offset);
                    if (instruction.OpCode == OpCodes.Constrained)
                    {
                        int offset = BodyOffset(definition.RVA) + instruction.Offset + 2;
                        Byte(offset, (byte)(originalImage[offset] ^ 1), "number-constrained-target-" + definition.Name);
                    }
                    if (instruction.Operand is Mono.Cecil.MethodReference call)
                    {
                        string expected = definition.Name == operation ? selectedName : comparisonName;
                        check(call.Name == expected, "number-exact-selected-call-" + definition.Name);
                        var wrongName = expected == "Max" ? "Min" : expected == "Min" ? "Max" : expected == "GreaterThan" ? "LessThan" : "GreaterThan";
                        var wrong = math.Methods.SelectMany(m => m.Body.Instructions).Select(i => i.Operand)
                            .OfType<Mono.Cecil.MethodReference>().First(m => m.Name == wrongName && m.MetadataToken.TokenType == Mono.Cecil.TokenType.MemberRef);
                        mutations.Add((BodyOffset(definition.RVA) + instruction.Offset + 1, BitConverter.GetBytes(wrong.MetadataToken.ToInt32()), "number-wrong-comparison-or-selection-" + definition.Name));
                        break;
                    }
                }
            }
            foreach (var owner in new[] { publicVector, math, closedVector })
            {
                var unrelated = owner.Methods.First(m => !proofNames.Contains(m.Name) && !proofNames.Any(n => m.Name.EndsWith("." + n, StringComparison.Ordinal)));
                var cctorName = reader.MethodDefinitions.Select(reader.GetMethodDefinition).First(m => reader.GetString(m.Name) == ".cctor").Name;
                int nameSize = reader.GetHeapSize(HeapIndex.String) > ushort.MaxValue ? 4 : 2;
                int row = originalPe.PEHeaders.MetadataStartOffset + reader.GetTableMetadataOffset(TableIndex.MethodDef)
                    + ((int)unrelated.MetadataToken.RID - 1) * reader.GetTableRowSize(TableIndex.MethodDef) + 8;
                mutations.Add((row, BitConverter.GetBytes(MetadataTokens.GetHeapOffset(cctorName)).Take(nameSize).ToArray(), "number-owner-cctor-" + owner.FullName));
            }
            var mathCall = declaration.Body.Instructions.Select(i => i.Operand).OfType<Mono.Cecil.GenericInstanceMethod>().Single(m => m.DeclaringType.Name == "VectorMath");
            var mathSpec = reader.GetMethodSpecification((MethodSpecificationHandle)MetadataTokens.EntityHandle(mathCall.MetadataToken.ToInt32()));
            int blobOffset = originalPe.PEHeaders.MetadataStartOffset + reader.GetHeapMetadataOffset(HeapIndex.Blob) + MetadataTokens.GetHeapOffset(mathSpec.Signature);
            blobOffset += originalImage[blobOffset] < 0x80 ? 1 : originalImage[blobOffset] < 0xc0 ? 2 : 4;
            int blobLength = reader.GetBlobBytes(mathSpec.Signature).Length;
            Byte(blobOffset + blobLength - 1, 1, "number-hardware-wrong-element-variable");
            Byte(BodyOffset(countDef.RelativeVirtualAddress), 0x6f, "number-count-callvirt");
            Byte(BodyOffset(countDef.RelativeVirtualAddress) + 4, 0x0a, "number-count-wrong-target-kind");
            Byte(BodyOffset(helperDef.RelativeVirtualAddress) + 5, 0x2c, "number-helper-inverted-condition");
            Byte(BodyOffset(helperDef.RelativeVirtualAddress) + helperIl.Length - 1, 0, "number-helper-missing-ret");
            check(helperIl[7] == 0x1f && helperIl[8] == 0x43, "number-original-resource");
            Byte(BodyOffset(helperDef.RelativeVirtualAddress) + 8, 0x42, "number-helper-resource");
            var supported = closedVector.Methods.Single(m => m.Name == "get_IsSupported");
            var whitelistToken = supported.Body.Instructions.First(i => i.OpCode == OpCodes.Ldtoken);
            int whitelistOffset = BodyOffset(supported.RVA) + whitelistToken.Offset + 1;
            Byte(whitelistOffset, (byte)(originalImage[whitelistOffset] ^ 1), "number-unsupported-whitelist-type");
            foreach (var definition in math.Methods.Where(m => m.Name == operation || m.Name == selectedName))
            {
                var handle = (MethodDefinitionHandle)MetadataTokens.EntityHandle(definition.MetadataToken.ToInt32());
                foreach (var gpHandle in reader.GetMethodDefinition(handle).GetGenericParameters())
                {
                    int gpOffset = originalPe.PEHeaders.MetadataStartOffset + reader.GetTableMetadataOffset(TableIndex.GenericParam)
                        + (MetadataTokens.GetRowNumber(gpHandle) - 1) * reader.GetTableRowSize(TableIndex.GenericParam) + 2;
                    Byte(gpOffset, (byte)(originalImage[gpOffset] ^ 1), "number-math-generic-variance-" + definition.Name + "-" + MetadataTokens.GetRowNumber(gpHandle));
                }
            }
            foreach (var name in new[] { selectedName, comparisonName })
            {
                var member = contract.Methods.Single(m => m.Name == name);
                int interfaceFlags = originalPe.PEHeaders.MetadataStartOffset + reader.GetTableMetadataOffset(TableIndex.MethodDef)
                    + ((int)member.MetadataToken.RID - 1) * reader.GetTableRowSize(TableIndex.MethodDef) + 6;
                Byte(interfaceFlags, (byte)(originalImage[interfaceFlags] ^ 0x10), "number-interface-static-" + name);
                var implementation = vectorDefinition.GetMethodImplementations().Select(reader.GetMethodImplementation)
                    .Single(i => i.MethodBody == MetadataTokens.EntityHandle(bridges.Single(m => m.Name.EndsWith("." + name, StringComparison.Ordinal)).MetadataToken.ToInt32()));
                var declarationHandle = (MemberReferenceHandle)implementation.MethodDeclaration;
                var reference = reader.GetMemberReference(declarationHandle);
                int offset = originalPe.PEHeaders.MetadataStartOffset + reader.GetHeapMetadataOffset(HeapIndex.Blob) + MetadataTokens.GetHeapOffset(reference.Signature);
                offset += originalImage[offset] < 0x80 ? 1 : originalImage[offset] < 0xc0 ? 2 : 4;
                Byte(offset, (byte)(originalImage[offset] | 0x20), "number-methodimpl-instance-signature-" + name);
                int typeIndexSize = reader.GetTableRowCount(TableIndex.TypeDef) < 65536 ? 2 : 4;
                int methodIndexSize = Math.Max(reader.GetTableRowCount(TableIndex.MethodDef), reader.GetTableRowCount(TableIndex.MemberRef)) < 32768 ? 2 : 4;
                var implementationHandle = vectorDefinition.GetMethodImplementations().Single(h => reader.GetMethodImplementation(h).MethodBody == implementation.MethodBody);
                int implementationOffset = originalPe.PEHeaders.MetadataStartOffset + reader.GetTableMetadataOffset(TableIndex.MethodImpl)
                    + (MetadataTokens.GetRowNumber(implementationHandle) - 1) * reader.GetTableRowSize(TableIndex.MethodImpl) + typeIndexSize;
                var wrongBridge = bridges.Single(m => m.Name.EndsWith("." + (name == selectedName ? comparisonName : selectedName), StringComparison.Ordinal));
                mutations.Add((implementationOffset, BitConverter.GetBytes((int)wrongBridge.MetadataToken.RID << 1).Take(methodIndexSize).ToArray(), "number-methodimpl-wrong-body-" + name));
                var otherImplementation = vectorDefinition.GetMethodImplementations().First(h => h != implementationHandle);
                int otherOffset = originalPe.PEHeaders.MetadataStartOffset + reader.GetTableMetadataOffset(TableIndex.MethodImpl)
                    + (MetadataTokens.GetRowNumber(otherImplementation) - 1) * reader.GetTableRowSize(TableIndex.MethodImpl) + typeIndexSize;
                mutations.Add((otherOffset, originalImage.AsSpan(implementationOffset, 2 * methodIndexSize).ToArray(), "number-methodimpl-duplicate-" + name));
            }
            var mathResolver = compilation.GetType().GetMethod("ResolveMagnitudeMathCall", flags)!;
            var bridgeResolver = compilation.GetType().GetMethod("ResolveNumberBinaryBridge", flags)!;
            var callResolver = compilation.GetType().GetMethod("ResolveNumberBinaryCall", flags)!;
            var numberMath = mathResolver.Invoke(compilation, new[] { method, (object)mathCall.MetadataToken.ToInt32(), operation })!;
            var numberDefinition = math.Methods.Single(m => m.Name == operation);
            var selectionReference = numberDefinition.Body.Instructions.Select(i => i.Operand).OfType<Mono.Cecil.MethodReference>().First(m => m.Name == selectedName);
            var selectionBridge = bridgeResolver.Invoke(compilation, new[] { numberMath, (object)selectionReference.MetadataToken.ToInt32(), vector, selectedName })!;
            var selectionBridgeBody = bridges.Single(m => m.Name.EndsWith("." + selectedName, StringComparison.Ordinal));
            var selectionSpec = selectionBridgeBody.Body.Instructions.Select(i => i.Operand).OfType<Mono.Cecil.GenericInstanceMethod>().Single();
            var selection = callResolver.Invoke(compilation, new[] { selectionBridge, (object)selectionSpec.MetadataToken.ToInt32(), vector, selectedName })!;
            var selectionMathSpec = selectedPublic.Body.Instructions.Select(i => i.Operand).OfType<Mono.Cecil.GenericInstanceMethod>().Single(m => m.DeclaringType.Name == "VectorMath");
            var selectionMath = mathResolver.Invoke(compilation, new[] { selection, (object)selectionMathSpec.MetadataToken.ToInt32(), selectedName })!;
            var selectionDefinition = math.Methods.Single(m => m.Name == selectedName);
            var comparisonReference = selectionDefinition.Body.Instructions.Select(i => i.Operand).OfType<Mono.Cecil.MethodReference>().First(m => m.Name == comparisonName);
            var comparisonBridge = bridgeResolver.Invoke(compilation, new[] { selectionMath, (object)comparisonReference.MetadataToken.ToInt32(), vector, comparisonName })!;
            var comparisonBridgeBody = bridges.Single(m => m.Name.EndsWith("." + comparisonName, StringComparison.Ordinal));
            var comparisonSpec = comparisonBridgeBody.Body.Instructions.Select(i => i.Operand).OfType<Mono.Cecil.GenericInstanceMethod>().Single();
            var resolvedComparison = callResolver.Invoke(compilation, new[] { comparisonBridge, (object)comparisonSpec.MetadataToken.ToInt32(), vector, comparisonName })!;
            var dependencies = new[] { numberMath, selectionBridge, selection, selectionMath, comparisonBridge, resolvedComparison };
            check(dependencies.Distinct().Count() == 6, "number-exact-resolved-proof-dependencies");
            void PoisonDependency(object value, string field, object replacement, string label)
            {
                var saved = Get(value, field); Set(value, field, replacement);
                try
                {
                    bool refused = false;
                    try { guard.Invoke(compilation, new[] { method }); }
                    catch (TargetInvocationException error) when (error.InnerException is NotSupportedException) { refused = true; }
                    check(refused, "number-cache-" + label + "-" + field);
                    refused = false;
                    try { Resolve(); }
                    catch (TargetInvocationException error) when (error.InnerException is NotSupportedException) { refused = true; }
                    check(refused, "number-cache-resolver-" + label + "-" + field);
                }
                finally { Set(value, field, saved); }
                check(ReferenceEquals(Resolve(), method), "number-cache-restored-" + label + "-" + field);
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

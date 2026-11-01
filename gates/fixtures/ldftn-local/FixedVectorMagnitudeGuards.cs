using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using Mono.Cecil.Cil;

internal static class FixedVectorMagnitudeGuards
{
    internal static void Check(object compilation, object core, string corePath, string operation,
        int width, object[] controls, Action<bool, string> check)
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
        int BlobOffset(BlobHandle handle)
        {
            int offset = originalPe.PEHeaders.MetadataStartOffset + reader.GetHeapMetadataOffset(HeapIndex.Blob) + MetadataTokens.GetHeapOffset(handle);
            return offset + (originalImage[offset] < 0x80 ? 1 : originalImage[offset] < 0xc0 ? 2 : 4);
        }
        foreach (var method in controls)
        {
            var vector = Get(Get(Get(method, "Signature"), "ReturnType"), "Class");
            var context = Get(vector, "Context");
            var vectorDefinition = reader.GetTypeDefinition((TypeDefinitionHandle)Get(vector, "Handle"));
            var bridge = vectorDefinition.GetMethods().Single(h => reader.GetString(reader.GetMethodDefinition(h).Name).EndsWith("." + operation, StringComparison.Ordinal));
            var bridgeIl = originalPe.GetMethodBody(reader.GetMethodDefinition(bridge).RelativeVirtualAddress).GetILBytes()!;
            check(bridgeIl.Length == 8 && bridgeIl[0] == 2 && bridgeIl[1] == 3 && bridgeIl[2] == 0x28 && bridgeIl[7] == 0x2a, "fixed-magnitude-real-bridge");
            var bridgeSpec = (MethodSpecificationHandle)MetadataTokens.EntityHandle(BitConverter.ToInt32(bridgeIl, 3));
            var specification = reader.GetMethodSpecification(bridgeSpec);
            check(specification.Method == (EntityHandle)(MethodDefinitionHandle)Get(method, "Handle") && Convert.ToHexString(reader.GetBlobBytes(specification.Signature)) == "0A011300", "fixed-magnitude-bridge-var");
            object Resolve() => resolve.Invoke(compilation, new[] { core, (object)bridgeSpec, context })!;
            check(ReferenceEquals(Resolve(), method), "fixed-magnitude-bridge-target");
            var count = guard.Invoke(compilation, new[] { method })!;
            var leaf = Get(count, "DeclaringClass");
            var leafDefinition = reader.GetTypeDefinition((TypeDefinitionHandle)Get(leaf, "Handle"));
            check(reader.GetString(leafDefinition.Name) == "Vector64`1", "fixed-magnitude-actual-leaf-width");
            check(ReferenceEquals(((Array)Get(Get(leaf, "Context"), "TypeArgs")).GetValue(0), ((Array)Get(context, "TypeArgs")).GetValue(0)), "fixed-magnitude-leaf-element");
            check(width == 64 ? ReferenceEquals(leaf, vector) : !ReferenceEquals(leaf, vector), "fixed-magnitude-not-outer-count");
            var countDef = reader.GetMethodDefinition((MethodDefinitionHandle)Get(count, "Handle"));
            var countIl = originalPe.GetMethodBody(countDef.RelativeVirtualAddress).GetILBytes()!;
            var helperSpec = reader.GetMethodSpecification((MethodSpecificationHandle)MetadataTokens.EntityHandle(BitConverter.ToInt32(countIl, 1)));
            var helperDef = reader.GetMethodDefinition((MethodDefinitionHandle)helperSpec.Method);
            check(reader.GetString(helperDef.Name) == "ThrowForUnsupportedIntrinsicsVector64BaseType", "fixed-magnitude-real-leaf-helper");
            var mutations = new List<(int Offset, byte[] Value, string Name)>();
            void Byte(int offset, byte value, string name)
            {
                check(originalImage[offset] != value, "fixed-mutation-changes-" + name);
                mutations.Add((offset, new[] { value }, name));
            }
            void Token(Mono.Cecil.MethodDefinition definition, Instruction instruction, int replacement, string name)
            {
                mutations.Add((BodyOffset(definition.RVA) + instruction.Offset + instruction.OpCode.Size, BitConverter.GetBytes(replacement), name));
            }
            void InstructionMutation(Mono.Cecil.MethodDefinition definition, Instruction instruction, string name)
            {
                int offset = BodyOffset(definition.RVA) + instruction.Offset;
                if (instruction.OpCode == OpCodes.Call) Byte(offset, 0x6f, name + "-callvirt");
                else if (instruction.OpCode == OpCodes.Brfalse_S) Byte(offset, 0x2d, name + "-condition");
                else if (instruction.OpCode == OpCodes.Brtrue_S) Byte(offset, 0x2c, name + "-condition");
                else if (instruction.OpCode == OpCodes.Br_S) Byte(offset, 0x2c, name + "-condition");
            }
            for (int current = width; current >= 64; current /= 2)
            {
                var owner = cecil.MainModule.GetType("System.Runtime.Intrinsics.Vector" + current);
                var declaration = owner.Methods.Single(m => m.Name == operation && m.HasGenericParameters);
                var il = declaration.Body.Instructions;
                if (current == 64)
                {
                    check(il[0].OpCode == OpCodes.Ldc_I4_0 && il[1].OpCode == OpCodes.Brfalse_S, "fixed-magnitude-64-constant-false");
                    Byte(BodyOffset(declaration.RVA), 0x17, "fixed-magnitude-64-true");
                    InstructionMutation(declaration, il[1], "fixed-magnitude-64-entry");
                    Byte(BodyOffset(declaration.RVA) + il[1].Offset + 1, 1, "fixed-magnitude-64-redirect");
                    foreach (var instruction in il.Where(i => i.Operand is Mono.Cecil.MethodReference call && call.Name is "SkipInit" or "get_Count"))
                        InstructionMutation(declaration, instruction, "fixed-magnitude-64-loop-" + instruction.Offset);
                }
                else
                {
                    var getter = owner.Methods.Single(m => m.Name == "get_IsHardwareAccelerated");
                    InstructionMutation(getter, getter.Body.Instructions[0], "fixed-magnitude-hardware-" + current);
                    Byte(BodyOffset(getter.RVA) + getter.Body.Instructions[^1].Offset, 0, "fixed-magnitude-hardware-ret-" + current);
                    InstructionMutation(declaration, il[0], "fixed-magnitude-query-" + current);
                    InstructionMutation(declaration, il[1], "fixed-magnitude-branch-" + current);
                    var start = (Instruction)il[1].Operand;
                    check(start.OpCode == OpCodes.Ldarg_0 && start.Next!.OpCode == OpCodes.Ldfld, "fixed-magnitude-lower-prefix");
                    var lowerField = start.Next!;
                    var upperField = il.First(i => i.Operand is Mono.Cecil.FieldReference f && f.Name == "_upper");
                    foreach (var instruction in new[] { lowerField, lowerField.Next!.Next! })
                        Token(declaration, instruction, ((Mono.Cecil.FieldReference)upperField.Operand).MetadataToken.ToInt32(), "fixed-magnitude-upper-field-" + current + "-" + instruction.Offset);
                    Byte(BodyOffset(declaration.RVA) + start.Offset, 3, "fixed-magnitude-left-operand-" + current);
                    var lowerCall = lowerField.Next!.Next!.Next!;
                    check(lowerCall.OpCode == OpCodes.Call, "fixed-magnitude-binary-descent-call");
                    InstructionMutation(declaration, lowerCall, "fixed-magnitude-lower-call-" + current);
                    var spec = reader.GetMethodSpecification((MethodSpecificationHandle)MetadataTokens.EntityHandle(((Mono.Cecil.MethodReference)lowerCall.Operand).MetadataToken.ToInt32()));
                    Byte(BlobOffset(spec.Signature) + 2, 0x13, "fixed-magnitude-lower-var-" + current);
                    var lowerHandle = (MethodSpecificationHandle)MetadataTokens.EntityHandle(((Mono.Cecil.MethodReference)lowerCall.Operand).MetadataToken.ToInt32());
                    int methodColumn = reader.GetTableRowSize(TableIndex.MethodSpec) - (reader.GetHeapSize(HeapIndex.Blob) > ushort.MaxValue ? 4 : 2);
                    int specRow = originalPe.PEHeaders.MetadataStartOffset + reader.GetTableMetadataOffset(TableIndex.MethodSpec)
                        + (MetadataTokens.GetRowNumber(lowerHandle) - 1) * reader.GetTableRowSize(TableIndex.MethodSpec);
                    mutations.Add((specRow, BitConverter.GetBytes((int)declaration.MetadataToken.RID << 1).Take(methodColumn).ToArray(), "fixed-magnitude-nondecreasing-width-" + current));

                    var wrong = owner.Methods.Single(m => m.Name == (operation.StartsWith("Max", StringComparison.Ordinal) ? "MinMagnitude" : "MaxMagnitude") && m.HasGenericParameters);
                    var wrongLowerCall = wrong.Body.Instructions.First(i => i.Operand is Mono.Cecil.MethodReference m && m.Name == wrong.Name && m.DeclaringType.FullName == "System.Runtime.Intrinsics.Vector" + (current / 2));
                    Token(declaration, lowerCall, ((Mono.Cecil.MethodReference)wrongLowerCall.Operand).MetadataToken.ToInt32(), "fixed-magnitude-wrong-lower-operation-" + current);
                    var field = reader.GetMemberReference((MemberReferenceHandle)MetadataTokens.EntityHandle(((Mono.Cecil.FieldReference)lowerField.Operand).MetadataToken.ToInt32()));
                    var raw = reader.GetBlobBytes(field.Signature);
                    Byte(BlobOffset(field.Signature) + raw.Length - 1, 1, "fixed-magnitude-field-element-" + current);
                    var parent = reader.GetTypeSpecification((TypeSpecificationHandle)field.Parent);
                    var parentRaw = reader.GetBlobBytes(parent.Signature);
                    Byte(BlobOffset(parent.Signature) + parentRaw.Length - 2, 0x13, "fixed-magnitude-field-parent-var-" + current);
                }
                if (width > 64)
                {
                    var abs = owner.Methods.Single(m => m.Name == "Abs" && m.HasGenericParameters);
                    foreach (var instruction in abs.Body.Instructions.Take(30))
                        InstructionMutation(abs, instruction, "fixed-magnitude-abs-tests-" + current + "-" + instruction.Offset);
                    var start = (Instruction)abs.Body.Instructions[29].Operand;
                    if (current == 64)
                    {
                        foreach (var instruction in abs.Body.Instructions.Where(i => i.Operand is Mono.Cecil.MethodReference call && call.Name is "SkipInit" or "get_Count"))
                            InstructionMutation(abs, instruction, "fixed-magnitude-abs-leaf-" + instruction.Offset);
                    }
                    else
                    {
                        check(start.OpCode == OpCodes.Ldarg_0 && start.Next!.OpCode == OpCodes.Ldfld && start.Next.Next!.OpCode == OpCodes.Call, "fixed-magnitude-unary-descent");
                        var upper = abs.Body.Instructions.First(i => i.Operand is Mono.Cecil.FieldReference f && f.Name == "_upper");
                        Token(abs, start.Next!, ((Mono.Cecil.FieldReference)upper.Operand).MetadataToken.ToInt32(), "fixed-magnitude-abs-upper-" + current);
                        InstructionMutation(abs, start.Next!.Next!, "fixed-magnitude-abs-lower-" + current);
                    }
                }
            }
            var whitelist = cecil.MainModule.GetType("System.Runtime.Intrinsics.Vector64`1").Methods.Single(m => m.Name == "get_IsSupported");
            var whitelistTypes = whitelist.Body.Instructions.Where(i => i.OpCode == OpCodes.Ldtoken && i.Operand is Mono.Cecil.TypeReference t && t.FullName != "T").ToArray();
            var uintptr = whitelistTypes.Single(i => ((Mono.Cecil.TypeReference)i.Operand).FullName == "System.UIntPtr");
            var int64 = whitelistTypes.Single(i => ((Mono.Cecil.TypeReference)i.Operand).FullName == "System.Int64");
            check(uintptr.Offset > int64.Offset, "fixed-magnitude-original-whitelist-order");
            int firstTypeOffset = BodyOffset(whitelist.RVA) + int64.Offset + 1;
            int lastTypeOffset = BodyOffset(whitelist.RVA) + uintptr.Offset + 1;
            var reordered = originalImage.AsSpan(firstTypeOffset, lastTypeOffset - firstTypeOffset + 4).ToArray();
            BitConverter.GetBytes(((Mono.Cecil.TypeReference)uintptr.Operand).MetadataToken.ToInt32()).CopyTo(reordered, 0);
            BitConverter.GetBytes(((Mono.Cecil.TypeReference)int64.Operand).MetadataToken.ToInt32()).CopyTo(reordered, lastTypeOffset - firstTypeOffset);
            mutations.Add((firstTypeOffset, reordered, "fixed-magnitude-whitelist-order"));
            int whitelistOffset = BodyOffset(whitelist.RVA) + int64.Offset + 1;
            Byte(whitelistOffset, (byte)(originalImage[whitelistOffset] ^ 1), "fixed-magnitude-whitelist-token");
            Byte(BodyOffset(countDef.RelativeVirtualAddress), 0x6f, "fixed-magnitude-count-callvirt");
            var helperIl = originalPe.GetMethodBody(helperDef.RelativeVirtualAddress).GetILBytes()!;
            check(helperIl[7] == 0x1f && helperIl[8] == 0x43, "fixed-magnitude-leaf-resource");
            Byte(BodyOffset(helperDef.RelativeVirtualAddress) + 5, 0x2c, "fixed-magnitude-leaf-condition");
            Byte(BodyOffset(helperDef.RelativeVirtualAddress) + 8, 0x42, "fixed-magnitude-leaf-resource-changed");
            var resolveSpec = compilation.GetType().GetMethod("ResolveMethodSpec", flags)!;
            var dependencies = new List<object>();
            object currentMethod = method;
            for (int current = width; current > 64; current /= 2)
            {
                var declaration = (Mono.Cecil.MethodDefinition)cecil.MainModule.LookupToken(MetadataTokens.GetToken((MethodDefinitionHandle)Get(currentMethod, "Handle")));
                var lowerCall = declaration.Body.Instructions.First(i => i.Operand is Mono.Cecil.MethodReference reference && reference.Name == operation && reference.DeclaringType.FullName == "System.Runtime.Intrinsics.Vector" + (current / 2));
                var handle = (MethodSpecificationHandle)MetadataTokens.EntityHandle(((Mono.Cecil.MethodReference)lowerCall.Operand).MetadataToken.ToInt32());
                currentMethod = resolveSpec.Invoke(compilation, new[] { core, (object)handle, Get(currentMethod, "Context") })!;
                dependencies.Add(currentMethod);
            }
            if (width > 64)
            {
                var mathResolver = compilation.GetType().GetMethod("ResolveMagnitudeMathCall", flags)!;
                var bridgeResolver = compilation.GetType().GetMethod("ResolveMagnitudeAbsBridge", flags)!;
                var absResolver = compilation.GetType().GetMethod("ResolveMagnitudeAbsCall", flags)!;
                var declaration = (Mono.Cecil.MethodDefinition)cecil.MainModule.LookupToken(MetadataTokens.GetToken((MethodDefinitionHandle)Get(method, "Handle")));
                var mathCall = declaration.Body.Instructions.Select(i => i.Operand).OfType<Mono.Cecil.GenericInstanceMethod>().Single(m => m.DeclaringType.Name == "VectorMath");
                object number = mathResolver.Invoke(compilation, new[] { method, (object)mathCall.MetadataToken.ToInt32(), operation })!;
                string numberName = operation.EndsWith("Number", StringComparison.Ordinal) ? operation : operation + "Number";
                var math = cecil.MainModule.GetType("System.Runtime.Intrinsics.VectorMath");
                if (numberName != operation)
                {
                    var numberCall = math.Methods.Single(m => m.Name == operation).Body.Instructions.Select(i => i.Operand).OfType<Mono.Cecil.GenericInstanceMethod>().Single(m => m.Name == numberName);
                    number = mathResolver.Invoke(compilation, new[] { number, (object)numberCall.MetadataToken.ToInt32(), numberName })!;
                }
                var absReference = math.Methods.Single(m => m.Name == numberName).Body.Instructions.Select(i => i.Operand).OfType<Mono.Cecil.MethodReference>().First(m => m.Name == "Abs");
                var resolvedBridge = bridgeResolver.Invoke(compilation, new[] { number, (object)absReference.MetadataToken.ToInt32(), vector })!;
                var bridgeDefinition = (Mono.Cecil.MethodDefinition)cecil.MainModule.LookupToken(MetadataTokens.GetToken((MethodDefinitionHandle)Get(resolvedBridge, "Handle")));
                var publicCall = bridgeDefinition.Body.Instructions.Select(i => i.Operand).OfType<Mono.Cecil.GenericInstanceMethod>().Single();
                object currentAbs = absResolver.Invoke(compilation, new[] { resolvedBridge, (object)publicCall.MetadataToken.ToInt32(), vector, (object)0x13 })!;
                dependencies.Add(resolvedBridge);
                dependencies.Add(currentAbs);
                for (int current = width; current > 64; current /= 2)
                {
                    var absDefinition = (Mono.Cecil.MethodDefinition)cecil.MainModule.LookupToken(MetadataTokens.GetToken((MethodDefinitionHandle)Get(currentAbs, "Handle")));
                    var lowerCall = absDefinition.Body.Instructions.Select(i => i.Operand).OfType<Mono.Cecil.MethodReference>().First(m => m.Name == "Abs" && m.DeclaringType.FullName == "System.Runtime.Intrinsics.Vector" + (current / 2));
                    var handle = (MethodSpecificationHandle)MetadataTokens.EntityHandle(lowerCall.MetadataToken.ToInt32());
                    currentAbs = resolveSpec.Invoke(compilation, new[] { core, (object)handle, Get(currentAbs, "Context") })!;
                    dependencies.Add(currentAbs);
                }
            }
            foreach (var dependency in dependencies)
            {
                void Poison(string field, object replacement)
                {
                    var saved = Get(dependency, field); Set(dependency, field, replacement);
                    try
                    {
                        bool refused = false;
                        try { guard.Invoke(compilation, new[] { method }); }
                        catch (TargetInvocationException error) when (error.InnerException is NotSupportedException) { refused = true; }
                        check(refused, "fixed-magnitude-lower-cache-" + field);
                        refused = false;
                        try { Resolve(); }
                        catch (TargetInvocationException error) when (error.InnerException is NotSupportedException) { refused = true; }
                        check(refused, "fixed-magnitude-lower-cache-resolver-" + field);
                    }
                    finally { Set(dependency, field, saved); }
                    check(ReferenceEquals(Resolve(), method), "fixed-magnitude-lower-cache-restored-" + field);
                }
                Poison("Name", "InvalidMagnitudeDependency");
                Poison("Rva", 1);
                Poison("Handle", default(MethodDefinitionHandle));
                Poison("IsSynthetic", true);
                Poison("Attributes", MethodAttributes.Private);
                Poison("ImplAttributes", MethodImplAttributes.InternalCall);
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

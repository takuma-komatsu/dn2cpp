using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;

internal static class FixedGenericVectorEstimateGuards
{
    internal static void Check(object compilation, object core, string corePath,
        (EntityHandle Token, object Method)[] controls, Func<object, EntityHandle, object, object?> resolve,
        Action<bool, string> check)
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        object Get(object value, string name) => value.GetType().GetField(name, flags) is { } field ? field.GetValue(value)! : value.GetType().GetProperty(name, flags)!.GetValue(value)!;
        void Set(object value, string name, object replacement) => value.GetType().GetField(name, flags)!.SetValue(value, replacement);
        var guard = compilation.GetType().GetMethod("ResolveGenericVectorEstimateGuard", flags)!;
        var originalPe = (PEReader)Get(core, "PE");
        var reader = (MetadataReader)Get(core, "Reader");
        var byReader = (System.Collections.IDictionary)Get(compilation, "_byReader");
        check(typeof(object).Module.ModuleVersionId == reader.GetGuid(reader.GetModuleDefinition().Mvid), "fixed-oracle-corelib-identity");
        var originalImage = File.ReadAllBytes(corePath);
        int BodyOffset(int rva)
        {
            var section = originalPe.PEHeaders.SectionHeaders.Single(s => rva >= s.VirtualAddress && rva < s.VirtualAddress + s.SizeOfRawData);
            int offset = section.PointerToRawData + rva - section.VirtualAddress;
            return offset + ((originalImage[offset] & 3) == 2 ? 1 : (originalImage[offset + 1] >> 4) * 4);
        }
        int BlobOffset(BlobHandle blob)
        {
            int offset = originalPe.PEHeaders.MetadataStartOffset + reader.GetHeapMetadataOffset(HeapIndex.Blob) + MetadataTokens.GetHeapOffset(blob);
            return offset + (originalImage[offset] < 0x80 ? 1 : originalImage[offset] < 0xc0 ? 2 : 4);
        }
        int RowOffset(TableIndex table, int row) => originalPe.PEHeaders.MetadataStartOffset + reader.GetTableMetadataOffset(table) + (row - 1) * reader.GetTableRowSize(table);
        foreach (var control in controls)
        {
            var method = control.Method;
            var definitionHandle = (MethodDefinitionHandle)Get(method, "Handle");
            var definition = reader.GetMethodDefinition(definitionHandle);
            var vector = Get(Get(Get(method, "Signature"), "ReturnType"), "Class");
            var vectorHandle = (TypeDefinitionHandle)Get(vector, "Handle");
            var vectorDefinition = reader.GetTypeDefinition(vectorHandle);
            var ownerContext = Get(vector, "Context");
            var context = Get(method, "Context");
            string vectorName = reader.GetString(vectorDefinition.Name);
            int width = int.Parse(vectorName.Substring(6, vectorName.Length - 8));
            check(vectorDefinition.GetLayout().Size == width / 8 && vectorDefinition.GetLayout().PackingSize == 0
                && (vectorDefinition.Attributes & TypeAttributes.LayoutMask) == TypeAttributes.SequentialLayout, "fixed-layout");
            var bridge = vectorDefinition.GetMethods().Single(h => reader.GetString(reader.GetMethodDefinition(h).Name).EndsWith(".MultiplyAddEstimate", StringComparison.Ordinal));
            var bridgeBody = originalPe.GetMethodBody(reader.GetMethodDefinition(bridge).RelativeVirtualAddress).GetILBytes()!;
            check(bridgeBody.Length == 9 && bridgeBody[0] == 2 && bridgeBody[1] == 3 && bridgeBody[2] == 4
                && bridgeBody[3] == 0x28 && bridgeBody[8] == 0x2a, "fixed-bridge-il");
            var bridgeSpec = (MethodSpecificationHandle)MetadataTokens.EntityHandle(BitConverter.ToInt32(bridgeBody, 4));
            var specification = reader.GetMethodSpecification(bridgeSpec);
            check(specification.Method == definitionHandle && Convert.ToHexString(reader.GetBlobBytes(specification.Signature)) == "0A011300", "fixed-bridge-exact-definition-var");
            var mappings = vectorDefinition.GetMethodImplementations().Select(reader.GetMethodImplementation).Where(m => m.MethodBody == bridge).ToArray();
            check(mappings.Length == 1 && mappings[0].MethodDeclaration.Kind == HandleKind.MemberReference, "fixed-bridge-methodimpl");
            var slot = reader.GetMemberReference((MemberReferenceHandle)mappings[0].MethodDeclaration);
            check(reader.GetString(slot.Name) == "MultiplyAddEstimate" && slot.Parent.Kind == HandleKind.TypeSpecification, "fixed-bridge-slot");
            var runtimeModule = typeof(object).Module;
            var runtimeVector = runtimeModule.ResolveType(MetadataTokens.GetToken(vectorHandle));
            var runtimeSlot = runtimeModule.ResolveType(MetadataTokens.GetToken(slot.Parent), runtimeVector.GetGenericArguments(), Type.EmptyTypes);
            var slotArgs = runtimeSlot.GetGenericArguments();
            check(runtimeSlot.GetGenericTypeDefinition().FullName == "System.Runtime.Intrinsics.ISimdVector`2"
                && slotArgs.Length == 2 && slotArgs[0] == runtimeVector.MakeGenericType(runtimeVector.GetGenericArguments())
                && slotArgs[1] == runtimeVector.GetGenericArguments()[0], "fixed-bridge-closed-slot-binding");
            check(ReferenceEquals(resolve(core, bridgeSpec, ownerContext), method), "fixed-bridge-exact-cache-target");
            var count = guard.Invoke(compilation, new[] { method })!;
            check(reader.GetString(reader.GetTypeDefinition((TypeDefinitionHandle)Get(Get(count, "DeclaringClass"), "Handle")).Name) == "Vector64`1", "fixed-bottom-count-owner");
            var countDefinition = reader.GetMethodDefinition((MethodDefinitionHandle)Get(count, "Handle"));
            var countIl = originalPe.GetMethodBody(countDefinition.RelativeVirtualAddress).GetILBytes()!;
            check(countIl[0] == 0x28 && countIl[4] == 0x2b, "fixed-count-first-methodspec");
            var mutations = new List<(int Offset, byte[] Value, string Name)>();
            void Byte(int offset, byte value, string name) => mutations.Add((offset, new[] { value }, name));
            int bodyOffset = BodyOffset(definition.RelativeVirtualAddress);
            var body = originalPe.GetMethodBody(definition.RelativeVirtualAddress).GetILBytes()!;
            if (width == 64)
            {
                check(body[9] == 0x2b && body[55] == 0x28, "fixed64-entry-branch-count");
                Byte(bodyOffset + 9, 0x2c, "fixed64-conditional-entry");
                Byte(bodyOffset + 10, 0, "fixed64-fallthrough");
                Byte(bodyOffset + 55, 0x6f, "fixed64-count-callvirt");
            }
            else
            {
                check(body[0] == 2 && body[1] == 0x7b && body[6] == 3 && body[7] == 0x7b
                    && body[12] == 4 && body[13] == 0x7b && body[18] == 0x28, "fixed-half-prefix");
                Byte(bodyOffset, 3, "fixed-first-operand"); Byte(bodyOffset + 6, 2, "fixed-second-operand");
                Byte(bodyOffset + 12, 2, "fixed-third-operand"); Byte(bodyOffset + 1, 0x7c, "fixed-half-address");
                Byte(bodyOffset + 18, 0x6f, "fixed-lower-callvirt");
                var lowerSpecHandle = (MethodSpecificationHandle)MetadataTokens.EntityHandle(BitConverter.ToInt32(body, 19));
                var lowerSpec = reader.GetMethodSpecification(lowerSpecHandle);
                check(Convert.ToHexString(reader.GetBlobBytes(lowerSpec.Signature)) == "0A011E00", "fixed-lower-same-mvar");
                Byte(BlobOffset(lowerSpec.Signature) + 2, 0x13, "fixed-lower-typevar");
                Byte(BlobOffset(lowerSpec.Signature) + 3, 1, "fixed-lower-wrong-mvar");
                var lowerDefinition = reader.GetMethodDefinition((MethodDefinitionHandle)lowerSpec.Method);
                var lowerOwner = reader.GetTypeDefinition(lowerDefinition.GetDeclaringType());
                var unrelated = lowerOwner.GetMethods().First(h => h != (MethodDefinitionHandle)lowerSpec.Method);
                var cctorName = reader.MethodDefinitions.Select(reader.GetMethodDefinition).First(m => reader.GetString(m.Name) == ".cctor").Name;
                int methodNameSize = reader.GetHeapSize(HeapIndex.String) > ushort.MaxValue ? 4 : 2;
                mutations.Add((RowOffset(TableIndex.MethodDef, MetadataTokens.GetRowNumber(unrelated)) + 8,
                    BitConverter.GetBytes(MetadataTokens.GetHeapOffset(cctorName)).Take(methodNameSize).ToArray(), "fixed-lower-owner-type-initializer"));
                int specRow = RowOffset(TableIndex.MethodSpec, MetadataTokens.GetRowNumber(lowerSpecHandle));
                int codedSize = reader.GetTableRowSize(TableIndex.MethodSpec) - (reader.GetHeapSize(HeapIndex.Blob) > ushort.MaxValue ? 4 : 2);
                mutations.Add((specRow, BitConverter.GetBytes(MetadataTokens.GetRowNumber(definitionHandle) << 1).Take(codedSize).ToArray(), "fixed-self-recursion"));
                var fieldRefHandle = (MemberReferenceHandle)MetadataTokens.EntityHandle(BitConverter.ToInt32(body, 2));
                var fieldReference = reader.GetMemberReference(fieldRefHandle);
                var field = vectorDefinition.GetFields().Single(h => reader.GetString(reader.GetFieldDefinition(h).Name) == "_lower");
                var fieldDefinition = reader.GetFieldDefinition(field);
                check(fieldDefinition.Attributes == (FieldAttributes.Assembly | FieldAttributes.InitOnly), "fixed-half-instance-initonly");
                Byte(RowOffset(TableIndex.Field, MetadataTokens.GetRowNumber(field)), 0x33, "fixed-half-static");
                Byte(RowOffset(TableIndex.Field, MetadataTokens.GetRowNumber(field)), 0x21, "fixed-half-private");
                var raw = reader.GetBlobBytes(fieldDefinition.Signature);
                Byte(BlobOffset(fieldDefinition.Signature) + raw.Length - 2, 0x1e, "fixed-half-definition-mvar");
                Byte(BlobOffset(fieldDefinition.Signature) + raw.Length - 1, 1, "fixed-half-definition-wrong-var");
                int ownToken = MetadataTokens.GetRowNumber(vectorHandle) << 2;
                check(ownToken >= 0x80 && ownToken < 0x4000 && (raw[3] & 0xc0) == 0x80, "fixed-half-token-width");
                var ownEncoded = new[] { (byte)(0x80 | ownToken >> 8), (byte)ownToken };
                mutations.Add((BlobOffset(fieldDefinition.Signature) + 3, ownEncoded, "fixed-half-definition-self-width"));
                var referenceRaw = reader.GetBlobBytes(fieldReference.Signature);
                mutations.Add((BlobOffset(fieldReference.Signature) + 3, ownEncoded, "fixed-half-reference-self-width"));
                Byte(BlobOffset(fieldReference.Signature) + referenceRaw.Length - 2, 0x1e, "fixed-half-reference-mvar");
                var parentBlob = reader.GetTypeSpecification((TypeSpecificationHandle)fieldReference.Parent).Signature;
                Byte(BlobOffset(parentBlob) + reader.GetBlobBytes(parentBlob).Length - 1, 1, "fixed-half-parent-element");
                mutations.Add((BlobOffset(parentBlob) + 2, raw.Skip(3).Take(2).ToArray(), "fixed-half-wrong-parent-width"));
                var upper = vectorDefinition.GetFields().Single(h => reader.GetString(reader.GetFieldDefinition(h).Name) == "_upper");
                var upperName = reader.GetFieldDefinition(upper).Name;
                int nameSize = reader.GetHeapSize(HeapIndex.String) > ushort.MaxValue ? 4 : 2;
                mutations.Add((RowOffset(TableIndex.MemberRef, MetadataTokens.GetRowNumber(fieldRefHandle)) + reader.GetTableRowSize(TableIndex.MemberRef) - nameSize - (reader.GetHeapSize(HeapIndex.Blob) > ushort.MaxValue ? 4 : 2), BitConverter.GetBytes(MetadataTokens.GetHeapOffset(upperName)).Take(nameSize).ToArray(), "fixed-upper-field"));
            }
            var helperSpec = reader.GetMethodSpecification((MethodSpecificationHandle)MetadataTokens.EntityHandle(BitConverter.ToInt32(countIl, 1)));
            check(helperSpec.Method.Kind == HandleKind.MethodDefinition, "fixed-count-helper-definition");
            var helperDefinition = reader.GetMethodDefinition((MethodDefinitionHandle)helperSpec.Method);
            check(reader.GetString(helperDefinition.Name) == "ThrowForUnsupportedIntrinsicsVector64BaseType", "fixed-count-exact-v64-helper");
            int helperOffset = BodyOffset(helperDefinition.RelativeVirtualAddress);
            var helperIl = originalPe.GetMethodBody(helperDefinition.RelativeVirtualAddress).GetILBytes()!;
            check(helperIl[5] == 0x2d && helperIl[^1] == 0x2a, "fixed-helper-condition-ret");
            Byte(helperOffset + 5, 0x2c, "fixed-helper-inverted-condition");
            Byte(helperOffset + helperIl.Length - 1, 0, "fixed-helper-missing-return");
            int countOffset = BodyOffset(countDefinition.RelativeVirtualAddress);
            Byte(countOffset, 0x6f, "fixed-count-callvirt"); Byte(countOffset + 4, 0x0a, "fixed-count-helper-kind");
            foreach (var mutation in mutations)
            {
                var changed = (byte[])originalImage.Clone(); mutation.Value.CopyTo(changed, mutation.Offset);
                using var modified = new PEReader(new MemoryStream(changed, writable: false));
                var modifiedReader = modified.GetMetadataReader();
                byReader.Add(modifiedReader, core);
                Set(core, "PE", modified); Set(core, "Reader", modifiedReader);
                try
                {
                    bool refused = false;
                    try { guard.Invoke(compilation, new[] { method }); }
                    catch (TargetInvocationException error) when (error.InnerException is NotSupportedException) { refused = true; }
                    check(refused, mutation.Name + ":guard");
                    refused = false;
                    try { resolve(core, bridgeSpec, ownerContext); }
                    catch (TargetInvocationException error) when (error.InnerException is NotSupportedException) { refused = true; }
                    check(refused, mutation.Name + ":precache");
                }
                finally { Set(core, "PE", originalPe); Set(core, "Reader", reader); byReader.Remove(modifiedReader); }
                check(ReferenceEquals(resolve(core, bridgeSpec, ownerContext), method), mutation.Name + ":restored");
            }
        }
    }
}

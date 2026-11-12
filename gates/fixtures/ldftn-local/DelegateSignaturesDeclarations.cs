using System.Buffers.Binary;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;

internal static class DelegateSignaturesDeclarations
{
    internal readonly record struct Target(MethodDefinitionHandle Handle, string Owner, string Name);

    // Keep the oracle independent of IsName, so narrowing the production catalog
    // cannot silently remove a control from this fixture.
    internal static bool IsName(string owner, string name) =>
        owner is "System.Delegate" or "System.MulticastDelegate" && (name is ".ctor" or "Equals" or "GetHashCode"
            || owner == "System.MulticastDelegate" && name is "NewMulticastDelegate" or "CombineImpl" or "RemoveImpl"
                or "GetTarget" or "GetMethodImpl" or "GetInvocationList" or "get_HasSingleTarget" or "TryGetAt"
                or "CtorClosed" or "CtorClosedStatic" or "CtorRTClosed" or "CtorOpened" or "CtorVirtualDispatch"
                or "CtorCollectibleClosedStatic" or "CtorCollectibleOpened" or "CtorCollectibleVirtualDispatch");

    internal static Target[] Candidates(MetadataReader reader)
    {
        var result = new List<Target>();
        foreach (var handle in reader.TypeDefinitions)
        {
            var type = reader.GetTypeDefinition(handle);
            string owner = reader.GetString(type.Namespace) + "." + reader.GetString(type.Name);
            if (owner is not ("System.Delegate" or "System.MulticastDelegate")) continue;
            foreach (var methodHandle in type.GetMethods())
            {
                string name = reader.GetString(reader.GetMethodDefinition(methodHandle).Name);
                if (IsName(owner, name)) result.Add(new Target(methodHandle, owner, name));
            }
        }
        DelegateSignaturesMetadata.Require(result.Count == 25, "CoreLib delegate callable declaration catalog changed");
        return result.ToArray();
    }

    internal static void Check(string path)
    {
        byte[] original = File.ReadAllBytes(path);
        using var image = new PEReader(new MemoryStream(original));
        var reader = image.GetMetadataReader();
        var candidates = Candidates(reader);
        foreach (var item in candidates)
        {
            DelegateSignaturesMetadata.Require(DelegateSignaturesFixture.IsDefinition(reader, item.Handle, item.Owner, item.Name),
                "real CoreLib declaration was refused: " + item.Owner + "." + item.Name);
            Console.WriteLine("delegate-declaration-control:" + item.Owner + "." + item.Name + ":"
                + Convert.ToHexString(reader.GetBlobBytes(reader.GetMethodDefinition(item.Handle).Signature)));
        }
        var target = candidates.Single(item => item.Owner == "System.MulticastDelegate" && item.Name == "GetHashCode");
        var method = reader.GetMethodDefinition(target.Handle);
        var ownerHandle = method.GetDeclaringType();
        var owner = reader.GetTypeDefinition(ownerHandle);
        int methodRow = image.PEHeaders.MetadataStartOffset + reader.GetTableMetadataOffset(TableIndex.MethodDef)
            + (MetadataTokens.GetRowNumber(target.Handle) - 1) * reader.GetTableRowSize(TableIndex.MethodDef);
        int typeRow = image.PEHeaders.MetadataStartOffset + reader.GetTableMetadataOffset(TableIndex.TypeDef)
            + (MetadataTokens.GetRowNumber(ownerHandle) - 1) * reader.GetTableRowSize(TableIndex.TypeDef);
        int signature = image.PEHeaders.MetadataStartOffset + reader.GetHeapMetadataOffset(HeapIndex.Blob)
            + MetadataTokens.GetHeapOffset(method.Signature);
        DelegateSignaturesMetadata.Require(original[signature] == 3 && reader.GetBlobBytes(method.Signature).SequenceEqual(new byte[] { 0x20, 0, 8 }),
            "declaration patch requires the exact GetHashCode blob");
        Test("static", bytes => U16(bytes, methodRow + 6, (int)(method.Attributes | MethodAttributes.Static)),
            (r, m) => (m.Attributes & MethodAttributes.Static) != 0);
        Test("nonvirtual", bytes => U16(bytes, methodRow + 6, (int)(method.Attributes & ~MethodAttributes.Virtual)),
            (r, m) => (m.Attributes & MethodAttributes.Virtual) == 0);
        Test("native", bytes => U16(bytes, methodRow + 4, (int)MethodImplAttributes.Native),
            (r, m) => m.ImplAttributes == MethodImplAttributes.Native);
        Test("bodyless", bytes => BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(methodRow), 0),
            (r, m) => m.RelativeVirtualAddress == 0);
        Test("explicit-this", bytes => bytes[signature + 1] = 0x60,
            (r, m) => r.GetBlobBytes(m.Signature).SequenceEqual(new byte[] { 0x60, 0, 8 }));
        Test("wrong-return", bytes => bytes[signature + 3] = 2,
            (r, m) => r.GetBlobBytes(m.Signature).SequenceEqual(new byte[] { 0x20, 0, 2 }));
        Test("sealed-owner", bytes => BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(typeRow), (int)(owner.Attributes | TypeAttributes.Sealed)),
            (r, m) => (r.GetTypeDefinition(m.GetDeclaringType()).Attributes & TypeAttributes.Sealed) != 0);
        var objectHandle = reader.TypeDefinitions.Single(handle =>
        {
            var type = reader.GetTypeDefinition(handle);
            return reader.StringComparer.Equals(type.Namespace, "System") && reader.StringComparer.Equals(type.Name, "Object");
        });
        int stringWidth = reader.GetHeapSize(HeapIndex.String) > ushort.MaxValue ? 4 : 2;
        // TypeDefOrRef is wide if any participating table needs more than 14 bits.
        bool wideBase = new[] { TableIndex.TypeDef, TableIndex.TypeRef, TableIndex.TypeSpec }
            .Any(table => reader.GetTableRowCount(table) >= 1 << 14);
        Test("wrong-base", bytes =>
        {
            int offset = typeRow + 4 + 2 * stringWidth;
            int coded = CodedIndex.TypeDefOrRef(objectHandle);
            if (wideBase) BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(offset), coded);
            else U16(bytes, offset, coded);
        }, (r, m) => r.GetTypeDefinition(m.GetDeclaringType()).BaseType == objectHandle);
        Console.WriteLine("delegate-declaration-contract:ok");

        void Test(string label, Action<byte[]> mutate, Func<MetadataReader, MethodDefinition, bool> check)
        {
            byte[] bytes = (byte[])original.Clone();
            mutate(bytes);
            using var mutated = new PEReader(new MemoryStream(bytes));
            var changed = mutated.GetMetadataReader();
            DelegateSignaturesMetadata.Require(check(changed, changed.GetMethodDefinition(target.Handle)), "declaration mutation was lost: " + label);
            DelegateSignaturesMetadata.Require(!DelegateSignaturesFixture.IsDefinition(changed, target.Handle, target.Owner, target.Name),
                "malformed declaration was accepted: " + label);
            Console.WriteLine("delegate-declaration-refusal:" + label);
        }
    }

    private static void U16(byte[] bytes, int offset, int value) =>
        BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(offset), checked((ushort)value));
}

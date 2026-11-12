using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;

internal static class DelegateSignaturesMetadata
{
    internal static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private static string NamedOwner(MetadataReader reader, EntityHandle handle, string assembly = "System.Private.CoreLib")
    {
        if (handle.Kind == HandleKind.TypeSpecification)
        {
            var blob = reader.GetBlobReader(reader.GetTypeSpecification((TypeSpecificationHandle)handle).Signature);
            byte prefix = blob.ReadByte();
            if (prefix == 0x20)
            {
                NamedOwner(reader, blob.ReadTypeHandle());
                prefix = blob.ReadByte();
            }
            Require(prefix == 0x12, "delegate owner TypeSpec lost its class encoding");
            string owner = NamedOwner(reader, blob.ReadTypeHandle());
            Require(blob.RemainingBytes == 0, "delegate owner TypeSpec has unexpected trailing bytes");
            return owner;
        }
        if (handle.Kind == HandleKind.TypeDefinition)
        {
            var type = reader.GetTypeDefinition((TypeDefinitionHandle)handle);
            return reader.GetString(type.Namespace) + "." + reader.GetString(type.Name);
        }
        Require(handle.Kind == HandleKind.TypeReference, "delegate owner is not a named type");
        var reference = reader.GetTypeReference((TypeReferenceHandle)handle);
        Require(reference.ResolutionScope.Kind == HandleKind.AssemblyReference
            && reader.StringComparer.Equals(reader.GetAssemblyReference((AssemblyReferenceHandle)reference.ResolutionScope).Name, assembly),
            "delegate TypeRef has the wrong assembly scope");
        return reader.GetString(reference.Namespace) + "." + reader.GetString(reference.Name);
    }

    internal static MemberReferenceHandle[] Check(string path, string mode)
    {
        using var image = new PEReader(File.OpenRead(path));
        var reader = image.GetMetadataReader();
        var operands = new List<(MemberReference Reference, bool Address)>();
        var handles = new List<MemberReferenceHandle>();
        var main = reader.MethodDefinitions.Select(reader.GetMethodDefinition)
            .Single(method => reader.StringComparer.Equals(method.Name, "Main"));
        var il = image.GetMethodBody(main.RelativeVirtualAddress).GetILReader();
        while (il.RemainingBytes != 0)
        {
            byte opcode = il.ReadByte();
            bool address = opcode == 0xfe;
            if (address) Require(il.ReadByte() == 6, "unexpected two-byte IL opcode");
            if (address || opcode == 0x28)
            {
                var handle = MetadataTokens.EntityHandle(il.ReadInt32());
                Require(handle.Kind == HandleKind.MemberReference, "fixture operand lost its MemberRef");
                var reference = reader.GetMemberReference((MemberReferenceHandle)handle);
                string name = reader.GetString(reference.Name);
                string owner = NamedOwner(reader, reference.Parent,
                    mode == "fake-owner-address" && name == "GetHashCode" ? "FakeDelegateOwner" : "System.Private.CoreLib");
                if (DelegateSignaturesDeclarations.IsName(owner, name))
                {
                    operands.Add((reference, address));
                    handles.Add((MemberReferenceHandle)handle);
                }
            }
            else Require(opcode is 0x14 or 0x16 or 0x26 or 0x2a or 0xd3, "unexpected fixture IL opcode " + opcode);
        }
        Require(operands.Count == (mode == "control" ? 50 : 1), "delegate fixture operand count changed");
        if (mode == "control")
        {
            var routes = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var operand in operands)
            {
                var reference = operand.Reference;
                string owner = NamedOwner(reader, reference.Parent);
                string name = reader.GetString(reference.Name);
                Require(reference.Parent.Kind == HandleKind.TypeReference
                    && DelegateSignaturesFixture.Signature(reader, owner, name, reader.GetBlobReader(reference.Signature), false, out _),
                    "control MemberRef is not exact");
                string key = owner + "." + name + ":" + Convert.ToHexString(reader.GetBlobBytes(reference.Signature));
                routes.TryGetValue(key, out int route);
                routes[key] = route | (operand.Address ? 2 : 1);
            }
            Require(routes.Count == 25 && routes.Values.All(route => route == 3), "control lost a call or ldftn route");
            return handles.ToArray();
        }
        var only = operands.Single();
        var member = only.Reference;
        byte[] signature = reader.GetBlobBytes(member.Signature);
        Require(only.Address == mode.EndsWith("-address", StringComparison.Ordinal), "delegate fixture lost its call/ldftn route");
        string memberName = reader.GetString(member.Name);
        string memberOwner = NamedOwner(reader, member.Parent, mode == "fake-owner-address" ? "FakeDelegateOwner" : "System.Private.CoreLib");
        string expectedName = mode switch { "return-call" => "NewMulticastDelegate", "parameter-address" => "CtorOpened", "modifier-call" => "Equals", _ => "GetHashCode" };
        string expectedOwner = mode is "modifier-call" or "trailing-call" ? "System.Delegate" : "System.MulticastDelegate";
        Require(memberName == expectedName && memberOwner == expectedOwner, "delegate fixture target changed");
        var blob = reader.GetBlobReader(member.Signature);
        switch (mode)
        {
            case "return-call":
                Require(blob.ReadByte() == 0x20 && blob.ReadByte() == 2 && blob.ReadByte() == 0x12
                    && NamedOwner(reader, blob.ReadTypeHandle()) == "System.Delegate"
                    && blob.ReadBytes(blob.RemainingBytes).SequenceEqual(new byte[] { 0x1d, 0x1c, 8 }), "wrong return shape was lost");
                break;
            case "parameter-address": Exact(0x20, 3, 1, 0x1c, 0x18, 0x19); break;
            case "modifier-call":
                Require(blob.ReadByte() == 0x20 && blob.ReadByte() == 1 && blob.ReadByte() == 2 && blob.ReadByte() == 0x20
                    && NamedOwner(reader, blob.ReadTypeHandle()) == "System.Runtime.CompilerServices.IsConst"
                    && blob.ReadByte() == 0x1c && blob.RemainingBytes == 0, "parameter modopt was lost");
                break;
            case "header-address": Exact(0x60, 0, 8); break;
            case "trailing-call": Exact(0x20, 0, 8, 0); break;
            case "owner-normalized-call":
                Exact(0x20, 0, 8);
                Require(member.Parent.Kind == HandleKind.TypeReference, "normalized owner is not a named CoreLib TypeRef");
                break;
            case "fake-owner-address":
                Exact(0x20, 0, 8);
                Require(member.Parent.Kind == HandleKind.TypeReference, "fake owner lost its assembly-scoped TypeRef");
                using (var fakeImage = new PEReader(File.OpenRead(Path.Combine(Path.GetDirectoryName(path)!, "FakeDelegateOwner.dll"))))
                {
                    var fakeReader = fakeImage.GetMetadataReader();
                    var target = fakeReader.MethodDefinitions.Single();
                    Require(fakeReader.StringComparer.Equals(fakeReader.GetAssemblyDefinition().Name, "FakeDelegateOwner")
                        && fakeReader.GetBlobBytes(fakeReader.GetMethodDefinition(target).Signature).SequenceEqual(signature)
                        && !DelegateSignaturesFixture.IsDefinition(fakeReader, target, memberOwner, memberName),
                        "fake owner declaration no longer exercises the assembly boundary");
                }
                break;
            case "owner-typespec-call":
            case "owner-modifier-address":
                Exact(0x20, 0, 8);
                Require(member.Parent.Kind == HandleKind.TypeSpecification, "delegate owner lost its TypeSpec");
                var owner = reader.GetBlobReader(reader.GetTypeSpecification((TypeSpecificationHandle)member.Parent).Signature);
                if (mode == "owner-modifier-address")
                    Require(owner.ReadByte() == 0x20 && NamedOwner(reader, owner.ReadTypeHandle()) == "System.Runtime.CompilerServices.IsConst",
                        "owner modopt was lost");
                Require(owner.ReadByte() == 0x12 && NamedOwner(reader, owner.ReadTypeHandle()) == "System.MulticastDelegate"
                    && owner.RemainingBytes == 0, "owner TypeSpec is not exact");
                break;
            default: throw new ArgumentException("unknown shape " + mode);
        }
        Console.WriteLine("delegate-signature-shape:" + mode + ":" + member.Parent.Kind + ":" + Convert.ToHexString(signature));
        return handles.ToArray();
        void Exact(params byte[] expected) => Require(signature.SequenceEqual(expected), "delegate signature mutation was lost: " + mode);
    }
}

using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using Mono.Cecil;
using Mono.Cecil.Cil;
using AssemblyDefinition = Mono.Cecil.AssemblyDefinition;

internal static class MetadataShape
{
    internal static void CheckNormalizedBigMul(string mode, string manifest)
    {
        var document = System.Xml.Linq.XDocument.Load(manifest);
        string input = document.Root?.Attribute("input")?.Value
            ?? throw new InvalidOperationException("ILDiet input manifest entry is missing");
        using var image = new PEReader(File.OpenRead(input));
        var reader = image.GetMetadataReader();
        var member = reader.MemberReferences.Select(reader.GetMemberReference)
            .Single(reference => reader.StringComparer.Equals(reference.Name, "BigMul"));
        if (member.Parent.Kind != HandleKind.TypeReference)
            throw new InvalidOperationException("ILDiet did not normalize the primitive BigMul owner to TypeRef");
        var owner = reader.GetTypeReference((TypeReferenceHandle)member.Parent);
        if (!reader.StringComparer.Equals(owner.Namespace, "System") || !reader.StringComparer.Equals(owner.Name, "Int32")
            || !reader.GetBlobBytes(member.Signature).SequenceEqual(new byte[] { 0, 2, 0x0a, 8, 8 }))
            throw new InvalidOperationException("ILDiet changed the normalized BigMul declaration");
        Console.WriteLine("numeric-signature-normalized:" + mode);
    }

    internal static void PlainBigMulOwner(string path)
    {
        byte[] bytes = File.ReadAllBytes(path);
        using var image = new PEReader(new MemoryStream(bytes));
        var reader = image.GetMetadataReader();
        var member = reader.MemberReferences.Select(reader.GetMemberReference)
            .Single(reference => reader.StringComparer.Equals(reference.Name, "BigMul"));
        if (member.Parent.Kind != HandleKind.TypeSpecification)
            throw new InvalidOperationException("BigMul patch requires a TypeSpec owner");
        var signature = reader.GetTypeSpecification((TypeSpecificationHandle)member.Parent).Signature;
        var blob = reader.GetBlobBytes(signature);
        if (blob.Length >= 128 || blob[0] != 0x20 || blob[^1] != 0x08)
            throw new InvalidOperationException("unexpected BigMul modopt owner encoding");
        int offset = image.PEHeaders.MetadataStartOffset + reader.GetHeapMetadataOffset(HeapIndex.Blob)
            + MetadataTokens.GetHeapOffset(signature);
        // Keep later heap offsets stable while replacing modopt(Int32) by the
        // otherwise identical, unmodified primitive TypeSpec.
        bytes[offset] = 1;
        bytes[offset + 1] = 0x08;
        File.WriteAllBytes(path, bytes);
    }

    internal static void Check(string path, string mode)
    {
        using var image = new PEReader(File.OpenRead(path));
        var reader = image.GetMetadataReader();
        bool bigMul = mode.StartsWith("bigmul-", StringComparison.Ordinal);
        string name = bigMul ? "BigMul" : "CreateChecked";
        var references = reader.MemberReferences.Select(reader.GetMemberReference)
            .Where(reference => reader.StringComparer.Equals(reference.Name, name)).ToArray();
        if (references.Length == 0)
            throw new InvalidOperationException("fixture lost the numeric MemberRef");
        foreach (var reference in references)
        {
            byte[] signature = reader.GetBlobBytes(reference.Signature);
            Console.WriteLine("numeric-signature-member:" + reference.Parent.Kind + ":" + Convert.ToHexString(signature));
            if (reference.Parent.Kind != HandleKind.TypeSpecification) continue;
            var owner = reader.GetBlobBytes(reader.GetTypeSpecification((TypeSpecificationHandle)reference.Parent).Signature);
            Console.WriteLine("numeric-signature-owner:" + Convert.ToHexString(owner));
            if (mode.StartsWith("bigmul-int-owner-typespec-", StringComparison.Ordinal) && !owner.SequenceEqual(new byte[] { 0x08 }))
                throw new InvalidOperationException("unmodified primitive TypeSpec patch was lost");
        }
        if (mode.StartsWith("bigmul-int-owner-", StringComparison.Ordinal)
            && references.Any(reference => reference.Parent.Kind != HandleKind.TypeSpecification))
            throw new InvalidOperationException("fixture lost the BigMul owner TypeSpec");
        using var assembly = AssemblyDefinition.ReadAssembly(path);
        var instructions = assembly.MainModule.Types.SelectMany(type => type.Methods)
            .Where(method => method.HasBody).SelectMany(method => method.Body.Instructions).ToArray();
        var calls = instructions.Where(instruction => instruction.Operand is MethodReference method && method.Name == name).ToArray();
        int expectedCalls = mode == "bigmul-control" ? 12 : mode.EndsWith("-control", StringComparison.Ordinal) ? 3 : 1;
        if (calls.Length != expectedCalls)
            throw new InvalidOperationException("numeric fixture call count changed");
        if (mode.EndsWith("-address", StringComparison.Ordinal) && calls.Single().OpCode != OpCodes.Ldftn)
            throw new InvalidOperationException("numeric fixture lost ldftn");
        bool constrainedTypeSpec = false;
        foreach (var method in assembly.MainModule.Types.SelectMany(type => type.Methods).Where(method => method.HasBody))
        {
            var definition = reader.GetMethodDefinition(MetadataTokens.MethodDefinitionHandle((int)method.MetadataToken.RID));
            byte[] bytes = image.GetMethodBody(definition.RelativeVirtualAddress).GetILBytes()!;
            foreach (var instruction in method.Body.Instructions.Where(instruction => instruction.OpCode == OpCodes.Constrained))
            {
                var token = MetadataTokens.EntityHandle(BitConverter.ToInt32(bytes, instruction.Offset + instruction.OpCode.Size));
                constrainedTypeSpec |= token.Kind == HandleKind.TypeSpecification;
                Console.WriteLine("numeric-signature-constrained:" + token.Kind);
            }
        }
        if (!bigMul && mode.EndsWith("-control", StringComparison.Ordinal) && !constrainedTypeSpec)
            throw new InvalidOperationException("checked control lost constrained TypeSpec");
    }
}

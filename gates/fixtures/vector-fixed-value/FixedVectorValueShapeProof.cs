using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;

internal static class FixedVectorValueShapeProof
{
    public static void Verify(string mode, string path)
    {
        using var pe = new PEReader(File.OpenRead(path));
        var reader = pe.GetMetadataReader();
        string mutation = FixedVectorValueInputs.Mutation(mode);
        void Require(bool value, string detail)
        { if (!value) throw new InvalidOperationException($"Serialized vector fixture {mode}: {detail}"); }
        var objectIdentities = reader.TypeReferences.Where(h =>
        {
            var type = reader.GetTypeReference(h);
            return reader.StringComparer.Equals(type.Namespace, "System") && reader.StringComparer.Equals(type.Name, "Object");
        }).Select(h => Identity(reader, h)).ToArray();
        bool Modified(string shape, string qualifier, string inner) => objectIdentities.Any(id => shape == qualifier + "(" + id + "," + inner + ")");
        string? element = mutation switch
        {
            "bool" => "02", "char" => "03", "string" => "0E", "nullable" => "System.Nullable`1", "generic-struct" => "def:Guard.GenericElement`1", "open" => "!0", "decimal" => "System.Decimal", "int128" => "System.Int128",
            "uint128" => "System.UInt128", "enum" or "struct" or "byref-like" => "def:Guard.Element", _ => null
        };
        var targetTokens = new HashSet<int>();
        int targets = 0;
        foreach (var handle in reader.MemberReferences)
        {
            var member = reader.GetMemberReference(handle);
            string name = reader.GetString(member.Name);
            if (name is not ("Equals" or "GetHashCode")) continue;
            targets++; targetTokens.Add(MetadataTokens.GetToken(handle));
            bool hash = name == "GetHashCode";
            var raw = reader.GetBlobReader(member.Signature);
            int header = raw.ReadByte();
            Require(header == (mutation switch { "static" => 0, "explicit-this" => 0x60, "vararg" => 0x25, "generic" => 0x30, _ => 0x20 }), "calling convention/header");
            if ((header & 0x10) != 0) Require(raw.ReadCompressedInteger() == 1, "generic arity");
            int count = raw.ReadCompressedInteger();
            Require(count == (mutation switch { "missing" => 0, "extra" => hash ? 1 : 2, _ => hash ? 0 : 1 }), "parameter count");
            string ret = Type(reader, ref raw);
            Require(mutation == "return-modopt" ? Modified(ret, "opt", hash ? "08" : "02")
                : ret == (mutation switch { "byref-result" => "byref(" + (hash ? "08" : "02") + ")", "wrong-result" => "0A", _ => hash ? "08" : "02" }), "return shape");
            var parameters = new List<string>();
            for (int i = 0; i < count; i++) parameters.Add(Type(reader, ref raw));
            if (mutation == "trailing") Require(raw.RemainingBytes == 1 && raw.ReadByte() == 0x08, "post-parameter trailing byte");
            else Require(raw.RemainingBytes == 0, "unexpected method trailing bytes");
            if (count != 0)
            {
                string first = parameters[0];
                bool interfaceCall = mode.StartsWith("boxed-", StringComparison.Ordinal)
                    || mode.StartsWith("constrained-", StringComparison.Ordinal) && mode.Contains("equatable", StringComparison.Ordinal)
                    || mode.StartsWith("constrained-receiver-", StringComparison.Ordinal);
                bool objectCall = mode.StartsWith("object-", StringComparison.Ordinal) || mode.Contains("constrained-object", StringComparison.Ordinal) || mode == "constrained-foreign-object";
                string baseline = interfaceCall ? "!0" : objectCall ? "1C" : "";
                if (baseline == "" && !hash)
                {
                    var vectorRef = reader.TypeReferences.Single(h =>
                    {
                        var type = reader.GetTypeReference(h);
                        return reader.StringComparer.Equals(type.Namespace, "System.Runtime.Intrinsics") && reader.StringComparer.Equals(type.Name, FixedVectorValueInputs.TypeName);
                    });
                    baseline = "g(val(" + Identity(reader, vectorRef) + ");!0)";
                }
                Require(mutation switch
                {
                    "byref-input" => first == "byref(" + baseline + ")",
                    "modopt" => Modified(first, "opt", baseline),
                    "modreq" => Modified(first, "req", baseline),
                    "wrong-input" => first == "0A",
                    "wrong-closed" => first.Contains(FixedVectorValueInputs.OwnerName, StringComparison.Ordinal) && first.EndsWith(";08)", StringComparison.Ordinal),
                    "wrong-width" => first.Contains("System.Runtime.Intrinsics.Vector" + (FixedVectorValueInputs.Width == 64 ? 128 : 64) + "`1", StringComparison.Ordinal) && first.EndsWith(";0D)", StringComparison.Ordinal),
                    "wrong-family" => first.Contains("System.Numerics.Vector`1", StringComparison.Ordinal) && first.EndsWith(";0D)", StringComparison.Ordinal),
                    "mvar-context" => first == "!!0",
                    _ => hash || first == "1C" || first == "!0" || first.Contains(FixedVectorValueInputs.OwnerName, StringComparison.Ordinal) && first.EndsWith(";!0)", StringComparison.Ordinal)
                }, "parameter type/element/qualifiers");
                if (mutation == "extra") Require(parameters[^1] == "08", "extra parameter type");
            }
            string owner;
            int ownerRemainder = 0;
            if (member.Parent.Kind == HandleKind.TypeSpecification)
            {
                var blob = reader.GetBlobReader(reader.GetTypeSpecification((TypeSpecificationHandle)member.Parent).Signature);
                owner = Type(reader, ref blob); ownerRemainder = blob.RemainingBytes;
                if (mutation == "owner-trailing") Require(ownerRemainder == 1 && blob.ReadByte() == 0x08, "owner trailing byte");
                else Require(ownerRemainder == 0, "unexpected owner trailing bytes");
            }
            else owner = Identity(reader, member.Parent);
            if (mutation == "foreign-owner") Require(owner.Contains("def:" + FixedVectorValueInputs.OwnerName + "", StringComparison.Ordinal), "same name local owner identity");
            else if (mutation == "constrained-foreign-object") Require(owner == "def:System.Object", "foreign Object identity");
            else if (mutation == "constrained-foreign-equatable") Require(owner.Contains("def:System.IEquatable`1", StringComparison.Ordinal), "foreign interface identity");
            else if (mutation is "context-spec-var" or "context-spec-mvar") Require(owner == (mutation == "context-spec-var" ? "!0" : "!!0"), "contextual parent preserved");
            else Require(owner.Contains("@System.Private.CoreLib", StringComparison.Ordinal), "genuine owner source module");
            if (element is not null && !mode.StartsWith("constrained-object-", StringComparison.Ordinal)) Require(element.Length == 2 || element == "!0"
                ? owner.EndsWith(";" + element + ")", StringComparison.Ordinal)
                : owner.Contains(element, StringComparison.Ordinal), "unsupported element preserved");
            if (mutation is "enum" or "struct" or "byref-like")
            {
                var elementDefinition = reader.TypeDefinitions.Select(reader.GetTypeDefinition).Single(t => reader.StringComparer.Equals(t.Name, "Element"));
                Require(Identity(reader, elementDefinition.BaseType).Contains(mutation == "enum" ? "System.Enum@" : "System.ValueType@", StringComparison.Ordinal), "unsupported element kind");
                if (mutation == "byref-like")
                    Require(elementDefinition.GetCustomAttributes().Select(reader.GetCustomAttribute).Any(attribute =>
                        attribute.Constructor.Kind == HandleKind.MemberReference
                        && Identity(reader, reader.GetMemberReference((MemberReferenceHandle)attribute.Constructor).Parent)
                            .Contains("System.Runtime.CompilerServices.IsByRefLikeAttribute@System.Private.CoreLib", StringComparison.Ordinal)), "serialized byref-like marker");
            }
            if (mutation == "constrained-wrong-equatable") Require(owner.Contains("System.IEquatable`1", StringComparison.Ordinal) && owner.EndsWith(";08))", StringComparison.Ordinal), "wrong closed interface argument");
        }
        Require(targets > 0 && (mode == "all" || targets == 1), "exact target inventory");
        int specs = reader.GetTableRowCount(TableIndex.MethodSpec);
        Require(specs == (mutation is "methodspec" or "methodspec-zero" or "context-spec-var" or "context-spec-mvar" ? 1 : 0), "MethodSpec inventory");
        if (specs != 0)
        {
            targetTokens.Add(MetadataTokens.GetToken(MetadataTokens.MethodSpecificationHandle(1)));
            var spec = reader.GetMethodSpecification(MetadataTokens.MethodSpecificationHandle(1));
            var blob = reader.GetBlobReader(spec.Signature);
            Require(blob.ReadByte() == 0x0a && blob.ReadCompressedInteger() == (mutation == "methodspec" ? 1 : 0), "MethodSpec header/arity");
            if (mutation == "methodspec") Require(Type(reader, ref blob) == "08", "MethodSpec argument");
            Require(blob.RemainingBytes == 0 && spec.Method.Kind == HandleKind.MemberReference, "MethodSpec target/trailing bytes");
        }
        bool ExactCall(int token) => specs == 0 ? targetTokens.Contains(token)
            : token == MetadataTokens.GetToken(MetadataTokens.MethodSpecificationHandle(1));
        int constrained = 0, boxedCalls = 0;
        foreach (var handle in reader.MethodDefinitions)
        {
            var method = reader.GetMethodDefinition(handle);
            if (method.RelativeVirtualAddress == 0) continue;
            var il = pe.GetMethodBody(method.RelativeVirtualAddress).GetILBytes()!;
            foreach (int i in InstructionOffsets(il))
            {
                if (i + 10 >= il.Length || il[i] != 0xfe || il[i + 1] != 0x16 || il[i + 6] != 0x6f) continue;
                constrained++;
                var receiver = MetadataTokens.EntityHandle(BitConverter.ToInt32(il, i + 2));
                var call = MetadataTokens.EntityHandle(BitConverter.ToInt32(il, i + 7));
                Require(receiver.Kind == HandleKind.TypeSpecification && call.Kind is HandleKind.MemberReference or HandleKind.MethodSpecification, "constrained receiver/call token kinds");
                Require(ExactCall(MetadataTokens.GetToken(call)), "constrained call binds inspected reference/specification");
                var blob = reader.GetBlobReader(reader.GetTypeSpecification((TypeSpecificationHandle)receiver).Signature);
                string shape = Type(reader, ref blob);
                if (element is not null) Require(element.Length == 2 || element == "!0"
                    ? shape.EndsWith(";" + element + ")", StringComparison.Ordinal)
                    : shape.Contains(element, StringComparison.Ordinal), "constrained receiver element preserved");
                if (mutation == "constrained-receiver-modopt") Require(Modified(shape, "opt", "g(val(ref:" + FixedVectorValueInputs.OwnerName + "@System.Private.CoreLib);0D)"), "receiver modifier and exact element");
                else Require(shape.StartsWith("g(val(ref:" + FixedVectorValueInputs.OwnerName + "@System.Private.CoreLib);", StringComparison.Ordinal), "receiver genuine Vector identity");
                if (mutation == "constrained-receiver-trailing") Require(blob.RemainingBytes == 1 && blob.ReadByte() == 0x08, "receiver trailing byte");
                else Require(blob.RemainingBytes == 0, "receiver unexpected trailing bytes");
            }
            if (mode.StartsWith("boxed-", StringComparison.Ordinal))
            {
                if (!reader.GetString(method.Name).StartsWith("Probe", StringComparison.Ordinal)) continue;
                // Cecil writes our explicit ldloc operands in their long form (FE 0C + UInt16).
                bool contextualSpec = mutation is "context-spec-var" or "context-spec-mvar";
                Require(il.Length == (contextualSpec ? 26 : 20) && il[0] == 0xfe && il[1] == 0x0c && il[2] == 0 && il[3] == 0
                    && il[4] == 0x8c && il[9] == 0xfe && il[10] == 0x0c && il[11] == 0 && il[12] == 0
                    && il[13] == 0x6f && il[18] == 0x26 && (contextualSpec
                        ? il[19] == 0xd0 && il[24] == 0x26 && il[25] == 0x2a : il[19] == 0x2a), "exact boxed IL opcode sequence");
                var boxedType = MetadataTokens.EntityHandle(BitConverter.ToInt32(il, 5));
                Require(boxedType.Kind == HandleKind.TypeSpecification, "boxed receiver token");
                var receiverBlob = reader.GetBlobReader(reader.GetTypeSpecification((TypeSpecificationHandle)boxedType).Signature);
                Require(Type(reader, ref receiverBlob).StartsWith("g(val(ref:" + FixedVectorValueInputs.OwnerName + "@System.Private.CoreLib);", StringComparison.Ordinal)
                    && receiverBlob.RemainingBytes == 0, "boxed genuine vector receiver");
                Require(ExactCall(BitConverter.ToInt32(il, 14)), "boxed callvirt binds exact inspected reference/specification");
                boxedCalls++;
            }
        }
        Require(constrained == (mode == "all" ? 36 : mode.StartsWith("constrained-", StringComparison.Ordinal) ? 1 : 0), "constrained IL route inventory");
        if (mode.StartsWith("boxed-", StringComparison.Ordinal)) Require(boxedCalls == 1, "boxed ordinary route inventory");
        Console.WriteLine($"fixed-vector-value-shape-proof mode={mode} targets={targets} constrained={constrained} methodspecs={specs} verified=true");
    }

    internal static IEnumerable<int> InstructionOffsets(byte[] il)
    {
        for (int offset = 0; offset < il.Length;)
        {
            yield return offset;
            int size = il[offset] switch
            {
                0x28 or 0x6f or 0x8c or 0xd0 => 5,
                0x14 or 0x26 or 0x2a => 1,
                0xfe when offset + 1 < il.Length && il[offset + 1] is 0x0c or 0x0d => 4,
                0xfe when offset + 1 < il.Length && il[offset + 1] == 0x16 => 6,
                _ => throw new InvalidOperationException("Unexpected fixture IL opcode at " + offset)
            };
            if (offset + size > il.Length) throw new InvalidOperationException("Truncated fixture IL");
            offset += size;
        }
    }

    private static string Identity(MetadataReader reader, EntityHandle handle)
    {
        if (handle.Kind == HandleKind.TypeDefinition)
        { var type = reader.GetTypeDefinition((TypeDefinitionHandle)handle); return "def:" + reader.GetString(type.Namespace) + "." + reader.GetString(type.Name); }
        if (handle.Kind != HandleKind.TypeReference) throw new InvalidOperationException("Unexpected type identity token: " + handle.Kind);
        var reference = reader.GetTypeReference((TypeReferenceHandle)handle);
        string scope = reference.ResolutionScope.Kind == HandleKind.AssemblyReference
            ? reader.GetString(reader.GetAssemblyReference((AssemblyReferenceHandle)reference.ResolutionScope).Name) : reference.ResolutionScope.Kind.ToString();
        return "ref:" + reader.GetString(reference.Namespace) + "." + reader.GetString(reference.Name) + "@" + scope;
    }

    private static string Type(MetadataReader reader, ref BlobReader blob)
    {
        byte code = blob.ReadByte();
        if (code is 0x11 or 0x12)
        {
            int token = blob.ReadCompressedInteger();
            var handle = (token & 3) == 0 ? (EntityHandle)MetadataTokens.TypeDefinitionHandle(token >> 2) : MetadataTokens.TypeReferenceHandle(token >> 2);
            return (code == 0x11 ? "val(" : "class(") + Identity(reader, handle) + ")";
        }
        if (code is 0x1f or 0x20)
        {
            int token = blob.ReadCompressedInteger();
            var handle = (token & 3) == 0 ? (EntityHandle)MetadataTokens.TypeDefinitionHandle(token >> 2) : MetadataTokens.TypeReferenceHandle(token >> 2);
            return (code == 0x1f ? "req(" : "opt(") + Identity(reader, handle) + "," + Type(reader, ref blob) + ")";
        }
        if (code == 0x15)
        {
            string owner = Type(reader, ref blob); int count = blob.ReadCompressedInteger(); var args = new List<string>();
            for (int i = 0; i < count; i++) args.Add(Type(reader, ref blob));
            return "g(" + owner + ";" + string.Join(",", args) + ")";
        }
        if (code is 0x13 or 0x1e) return (code == 0x13 ? "!" : "!!") + blob.ReadCompressedInteger();
        if (code == 0x10) return "byref(" + Type(reader, ref blob) + ")";
        return code.ToString("X2");
    }
}

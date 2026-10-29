using System.Globalization;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;

internal static class RuntimeUtf8NameNormalization
{
    public static bool Run(string[] args)
    {
        if (args.Length != 3 || args[0] != "--runtime-utf8-name-normalization")
            return false;
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        Inspect(args[1], before: true);
        Inspect(args[2], before: false);
        Console.WriteLine("utf8-name-class-return-normalization:verified");
        return true;
    }

    private static void Inspect(string path, bool before)
    {
        using var stream = File.OpenRead(path);
        using var pe = new PEReader(stream);
        MetadataReader reader = pe.GetMetadataReader();
        int count = 0, classes = 0;
        foreach (MemberReferenceHandle handle in reader.MemberReferences)
        {
            MemberReference method = reader.GetMemberReference(handle);
            if (reader.GetString(method.Name) != "GetUtf8Name")
                continue;
            Require(method.Parent.Kind == HandleKind.TypeReference, "method owner must be a CoreLib TypeRef");
            TypeReference owner = reader.GetTypeReference((TypeReferenceHandle)method.Parent);
            Require(reader.GetString(owner.Namespace) == "System"
                && reader.GetString(owner.Name) == "RuntimeTypeHandle", "unexpected GetUtf8Name owner");
            Require(owner.ResolutionScope.Kind == HandleKind.AssemblyReference, "owner scope must be AssemblyRef");
            AssemblyReferenceHandle coreScope = (AssemblyReferenceHandle)owner.ResolutionScope;
            Require(reader.GetString(reader.GetAssemblyReference(coreScope).Name) == "System.Private.CoreLib", "non-CoreLib owner");
            BlobReader blob = reader.GetBlobReader(method.Signature);
            Require(blob.RemainingBytes >= 6 && blob.ReadByte() == 0 && blob.ReadByte() == 1,
                "expected exact static nongeneric one-parameter signature");
            byte marker = blob.ReadByte();
            Require(marker == 0x11 || marker == 0x12, "return marker is neither value nor class");
            Require(before || marker == 0x11, "post-diet return marker is not VALUETYPE");
            ReadCoreType(ref blob, reader, coreScope, "MdUtf8String");
            Require(blob.RemainingBytes >= 2 && blob.ReadByte() == 0x12, "parameter must be CLASS RuntimeType");
            ReadCoreType(ref blob, reader, coreScope, "RuntimeType");
            Require(blob.RemainingBytes == 0, "trailing signature data");
            count++;
            if (marker == 0x12) classes++;
            Console.WriteLine("utf8-name-normalization-" + (before ? "before" : "after")
                + ":" + MetadataTokens.GetToken(handle).ToString("X8", CultureInfo.InvariantCulture)
                + ":" + Convert.ToHexString(reader.GetBlobBytes(method.Signature)));
        }
        Require(count > 0, "no matching GetUtf8Name MemberRef");
        Require(!before || classes > 0, "original has no malformed CLASS return witness");
        Require(before || classes == 0, "post-diet still contains malformed CLASS return");
    }

    private static void ReadCoreType(ref BlobReader blob, MetadataReader reader,
        AssemblyReferenceHandle coreScope, string name)
    {
        Require(blob.TryReadCompressedInteger(out int coded) && coded > 0
            && (coded & 3) == 1 && (coded >> 2) > 0 && (coded >> 2) <= reader.TypeReferences.Count,
            "type token must be a valid TypeRef");
        TypeReference type = reader.GetTypeReference(MetadataTokens.TypeReferenceHandle(coded >> 2));
        Require(type.ResolutionScope == coreScope && reader.GetString(type.Namespace) == "System"
            && reader.GetString(type.Name) == name, "wrong type identity or CoreLib scope: " + name);
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException("GetUtf8Name normalization proof: " + message);
    }
}

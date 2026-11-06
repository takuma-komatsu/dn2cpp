using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography;

internal static class FixedVectorCoreMutations
{
    // Only writes a fresh private copy; source identity is checked after the write.
    public static void Generate(string mode, string source, string output)
    {
        if (mode is not ("owner-flags" or "equatable-flags" or "owner-constraint" or "equatable-constraint")) throw new ArgumentException("Unknown private CoreLib mutation: " + mode);
        if (File.Exists(output)) throw new InvalidOperationException("Refusing to replace private CoreLib: " + output);
        bool equatable = mode.StartsWith("equatable-", StringComparison.Ordinal);
        bool constraint = mode.EndsWith("-constraint", StringComparison.Ordinal);
        byte[] bytes = File.ReadAllBytes(source), originalHash = SHA256.HashData(bytes);
        using var pe = new PEReader(new MemoryStream(bytes)); var reader = pe.GetMetadataReader();
        var targets = reader.TypeDefinitions.Where(h =>
        {
            var type = reader.GetTypeDefinition(h);
            return equatable
                ? reader.StringComparer.Equals(type.Namespace, "System") && reader.StringComparer.Equals(type.Name, "IEquatable`1")
                : reader.StringComparer.Equals(type.Namespace, "System.Runtime.Intrinsics") && (!constraint || reader.StringComparer.Equals(type.Name, FixedVectorValueInputs.TypeName)) && new[] { "Vector64`1", "Vector128`1", "Vector256`1", "Vector512`1" }.Any(n => reader.StringComparer.Equals(type.Name, n));
        }).ToArray();
        if (targets.Length != (!equatable && !constraint ? 4 : 1)) throw new InvalidOperationException("Private mutation owner inventory");
        foreach (var handle in targets)
        {
            var type = reader.GetTypeDefinition(handle); var parameterHandle = type.GetGenericParameters().Single();
            var parameter = reader.GetGenericParameter(parameterHandle);
            var expected = equatable ? GenericParameterAttributes.AllowByRefLike : GenericParameterAttributes.None;
            if (parameter.Attributes != expected || parameter.GetConstraints().Count != 0 || parameter.Index != 0 || parameter.Parent != handle)
                throw new InvalidOperationException("Installed owner constraint contract changed");
            int parameterRow = MetadataTokens.GetRowNumber(parameterHandle);
            if (constraint)
            {
                int donorRow = 0, previousOwner = 0;
                for (int row = 1; row <= reader.GetTableRowCount(TableIndex.GenericParamConstraint); row++)
                {
                    int ownerRow = MetadataTokens.GetRowNumber(reader.GetGenericParameterConstraint(MetadataTokens.GenericParameterConstraintHandle(row)).Parameter);
                    if (ownerRow > parameterRow) { donorRow = row; break; }
                    previousOwner = ownerRow;
                }
                if (donorRow == 0 || previousOwner >= parameterRow) throw new InvalidOperationException("No sorted private constraint donor");
                int offset = pe.PEHeaders.MetadataStartOffset + reader.GetTableMetadataOffset(TableIndex.GenericParamConstraint)
                    + (donorRow - 1) * reader.GetTableRowSize(TableIndex.GenericParamConstraint);
                byte[] ownerBytes = reader.GetTableRowCount(TableIndex.GenericParam) < 65536
                    ? BitConverter.GetBytes((ushort)parameterRow) : BitConverter.GetBytes(parameterRow);
                ownerBytes.CopyTo(bytes, offset);
            }
            else
            {
                int offset = pe.PEHeaders.MetadataStartOffset + reader.GetTableMetadataOffset(TableIndex.GenericParam)
                    + (parameterRow - 1) * reader.GetTableRowSize(TableIndex.GenericParam) + 2;
                var flags = BitConverter.GetBytes((ushort)(expected ^ GenericParameterAttributes.ReferenceTypeConstraint));
                flags.CopyTo(bytes, offset);
            }
        }
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(output))!);
        using (var destination = new FileStream(output, FileMode.CreateNew, FileAccess.Write)) destination.Write(bytes);
        if (!SHA256.HashData(File.ReadAllBytes(source)).SequenceEqual(originalHash)) throw new InvalidOperationException("Source CoreLib changed");
        using var verification = new PEReader(File.OpenRead(output)); var changed = verification.GetMetadataReader();
        foreach (var handle in targets)
        {
            var parameter = changed.GetGenericParameter(changed.GetTypeDefinition(handle).GetGenericParameters().Single());
            if (constraint ? parameter.GetConstraints().Count != 1
                : (parameter.Attributes & GenericParameterAttributes.ReferenceTypeConstraint) == 0 || parameter.GetConstraints().Count != 0)
                throw new InvalidOperationException("Serialized private CoreLib mutation missing");
        }
        Console.WriteLine($"fixed-vector-core-mutation mode={mode} owners={targets.Length} source-unchanged=true output-sha256={Convert.ToHexString(SHA256.HashData(bytes))}");
    }
}

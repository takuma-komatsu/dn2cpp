using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

internal static class UInt128CheckedRawGuards
{
    public static bool Run(string[] args)
    {
        if (args.Length != 2 || args[0] != "--uint128-checked-raw-guards") return false;
        System.Globalization.CultureInfo.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
        System.Globalization.CultureInfo.CurrentUICulture = System.Globalization.CultureInfo.InvariantCulture;
        var type = Assembly.LoadFrom(Path.GetFullPath(args[1])).GetType("Dn2Cpp.CoreIntrinsics", true)!;
        const BindingFlags flags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
        int count = 0;
        foreach (int form in new[] { 0, 1, 2 })
        {
            bool owner = form == 1;
            var predicate = type.GetMethod(form == 2 ? "IsUInt128CheckedTypeSignature" : owner ? "IsUInt128CheckedOwnerSignature" : "IsUInt128CheckedInstantiation", flags)!;
            string prefix = form == 2 ? "" : owner ? "15120501" : "0A01";
            foreach (var vector in new (string Arg, bool Expected)[] {
                ("04",true),("05",true),("06",true),("07",true),("08",true),("09",true),("0A",true),("0B",true),
                ("03",true),("18",true),("19",true),("1105",true),("1300",true),("1E00",true),
                ("02",false),("0C",false),("0D",false),("1C",false),("1205",false),
                ("200508",false),("1F0508",false),("4508",false),("1D08",false),("1008",false),
                ("1511050108",false),("1106",false),("1100",false),("117D",false),
                ("",false),("08FF",false),("1E",false),("13200508",false) })
                Verify(prefix + vector.Arg, vector.Expected);
            if (form != 2) foreach (string hex in owner ? new[] { "1511050108", "1512050008", "151205020808", "20151512050108", "1512060108" }
                : new[] { "0A0008", "0A020808", "100108", "0A010800", "0A" }) Verify(hex, false);
            void Verify(string hex, bool expected)
            {
                var metadata = new MetadataBuilder();
                metadata.AddModule(0, metadata.GetOrAddString("Raw"), metadata.GetOrAddGuid(Guid.Empty), default, default);
                metadata.AddTypeReference(MetadataTokens.EntityHandle(0x00000001), metadata.GetOrAddString("System"), metadata.GetOrAddString("UInt128"));
                var blob = metadata.GetOrAddBlob(Convert.FromHexString(hex));
                var image = new BlobBuilder(); new MetadataRootBuilder(metadata).Serialize(image, 0, 0);
                using var provider = MetadataReaderProvider.FromMetadataImage(image.ToImmutableArray());
                bool actual = (bool)predicate.Invoke(null, new object[] { provider.GetMetadataReader(), blob })!;
                if (actual != expected) throw new InvalidOperationException($"UInt128 raw guard owner={owner} blob={hex}: {actual}");
                count++;
            }
        }
        Console.WriteLine("uint128-checked-raw-guards:" + count);
        return true;
    }
}

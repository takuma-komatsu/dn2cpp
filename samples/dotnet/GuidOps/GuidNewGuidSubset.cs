#nullable enable
using System;

// Guid.NewGuid() through the real CoreLib IL: bottoms out in the P/Invoke
// SystemNative_GetCryptographicallySecureRandomBytes (libSystem.Native),
// implemented in runtime/core/platform/posix/dn2cpp_system_native.cpp with
// real (non-deterministic) entropy. The section therefore prints only
// *derived properties* — RFC 4122 version/variant bits, uniqueness,
// parse/format round-trips — never a raw GUID, so native output and real
// `dotnet` output are identical strings even though the GUIDs differ.
namespace GuidNewGuidSubset;

static class Program
{
    internal static void __GateEntry()
    {
        Console.WriteLine("-- Guid.NewGuid properties --");
        Guid g1 = Guid.NewGuid();
        Guid g2 = Guid.NewGuid();

        byte[] bytes = g1.ToByteArray();
        Console.WriteLine(bytes.Length);                        // 16
        // RFC 4122: version nibble (high nibble of time_hi_and_version) is 4.
        // In little-endian ToByteArray layout that is the top nibble of byte 7.
        Console.WriteLine((bytes[7] >> 4));                     // 4
        // Variant: top two bits of clock_seq_hi_and_reserved (byte 8) are 10.
        Console.WriteLine((bytes[8] >> 6));                     // 2
        Console.WriteLine(g1 != g2);                            // True
        Console.WriteLine(g1 != Guid.Empty);                    // True

        Console.WriteLine("-- NewGuid round-trips --");
        Console.WriteLine(Guid.Parse(g1.ToString("D")) == g1);  // True
        Console.WriteLine(Guid.Parse(g1.ToString("N")) == g1);  // True
        Console.WriteLine(Guid.Parse(g1.ToString("B")) == g1);  // True
        Console.WriteLine(new Guid(g1.ToByteArray()) == g1);    // True
        Console.WriteLine(g1.ToString("D").Length);             // 36
        Console.WriteLine(g1.ToString("N").Length);             // 32
        Console.WriteLine(g1.CompareTo(g1));                    // 0
        Console.WriteLine(g1.Equals(g2));                       // False
    }
}

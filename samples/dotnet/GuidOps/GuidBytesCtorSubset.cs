#nullable enable
using System;

// Guid byte-level constructors and exporters through the real CoreLib IL over
// fixed data: the field ctor, byte[]/ReadOnlySpan<byte> ctors, the bigEndian
// ctor overloads, ToByteArray()/ToByteArray(bigEndian), and TryWriteBytes in
// both endiannesses. The hex dumps double as a layout regression check — any
// drift in the emitted 16-byte Guid struct shows up as a byte-order diff.
namespace GuidBytesCtorSubset;

static class Program
{
    static string Hex(byte[] b) => Convert.ToHexString(b);

    internal static void __GateEntry()
    {
        // 0f8fad5b-d9cb-469f-a165-70867728950e, little-endian mixed layout.
        byte[] le = new byte[]
        {
            0x5b, 0xad, 0x8f, 0x0f, 0xcb, 0xd9, 0x9f, 0x46,
            0xa1, 0x65, 0x70, 0x86, 0x77, 0x28, 0x95, 0x0e,
        };
        byte[] be = new byte[]
        {
            0x0f, 0x8f, 0xad, 0x5b, 0xd9, 0xcb, 0x46, 0x9f,
            0xa1, 0x65, 0x70, 0x86, 0x77, 0x28, 0x95, 0x0e,
        };

        Console.WriteLine("-- ctors --");
        Console.WriteLine(new Guid(le));
        Console.WriteLine(new Guid((ReadOnlySpan<byte>)le));
        Console.WriteLine(new Guid(le, bigEndian: false));
        Console.WriteLine(new Guid(be, bigEndian: true));
        Console.WriteLine(new Guid(0x0f8fad5b, unchecked((short)0xd9cb), 0x469f,
            0xa1, 0x65, 0x70, 0x86, 0x77, 0x28, 0x95, 0x0e));
        Console.WriteLine(new Guid(0x0f8fad5b, unchecked((short)0xd9cb), 0x469f,
            new byte[] { 0xa1, 0x65, 0x70, 0x86, 0x77, 0x28, 0x95, 0x0e }));
        Console.WriteLine(new Guid(0x0f8fad5bu, 0xd9cb, 0x469f,
            0xa1, 0x65, 0x70, 0x86, 0x77, 0x28, 0x95, 0x0e));

        Console.WriteLine("-- exporters --");
        Guid g = new Guid("0f8fad5b-d9cb-469f-a165-70867728950e");
        Console.WriteLine(Hex(g.ToByteArray()));
        Console.WriteLine(Hex(g.ToByteArray(bigEndian: false)));
        Console.WriteLine(Hex(g.ToByteArray(bigEndian: true)));

        Console.WriteLine("-- TryWriteBytes --");
        byte[] outLe = new byte[16];
        byte[] outBe = new byte[16];
        Console.WriteLine(g.TryWriteBytes(outLe));
        Console.WriteLine(Hex(outLe));
        Console.WriteLine(g.TryWriteBytes(outBe, bigEndian: true, out int written) + " " + written);
        Console.WriteLine(Hex(outBe));
        Console.WriteLine(g.TryWriteBytes(new byte[8]));                     // False

        Console.WriteLine("-- ctor failures (exception type names) --");
        try { new Guid(new byte[15]); }
        catch (Exception e) { Console.WriteLine(e.GetType().Name); }
        try { new Guid((byte[])null!); }
        catch (Exception e) { Console.WriteLine(e.GetType().Name); }
    }
}

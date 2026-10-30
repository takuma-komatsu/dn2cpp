using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;

namespace DateTimeTChar;

internal static class Program
{
    private static object? pressureRoot;
    private static int collections;
    private static ReadOnlySpan<char> CharDirect(DateTimeFormatInfo info) => info.DateSeparator.AsSpan();
    private static ReadOnlySpan<byte> ByteDirect(DateTimeFormatInfo info) => Encoding.UTF8.GetBytes(info.DateSeparator);
    private static ReadOnlySpan<char> CharPointer(DateTimeFormatInfo info) => info.DateSeparator.AsSpan();
    private static ReadOnlySpan<byte> BytePointer(DateTimeFormatInfo info) => Encoding.UTF8.GetBytes(info.DateSeparator);

    private static void Check(string name, bool condition)
    {
        Console.WriteLine("datetime-tchar-" + name + ":" + condition);
        if (!condition) throw new InvalidOperationException(name);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static string DynamicSeparator() => new string(new[]
    {
        (char)0x00c5, (char)0x6f22, (char)0xd83d, (char)0xde00,
        (char)0xd800, (char)0x0041, (char)0xdc00
    });

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static string Replacement() => new string(new[] { (char)0x03bb, (char)0x4e8c });

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ReadOnlySpan<char> CaptureChar(bool pointer)
    {
        var info = (DateTimeFormatInfo)DateTimeFormatInfo.InvariantInfo.Clone();
        info.DateSeparator = DynamicSeparator();
        var old = pointer ? CharPointer(info) : CharDirect(info);
        info.DateSeparator = Replacement();
        // Neither source String nor owner leaves this frame; only the old span does.
        return old;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ReadOnlySpan<byte> CaptureByte(bool pointer)
    {
        var info = (DateTimeFormatInfo)DateTimeFormatInfo.InvariantInfo.Clone();
        info.DateSeparator = DynamicSeparator();
        var old = pointer ? BytePointer(info) : ByteDirect(info);
        info.DateSeparator = Replacement();
        return old;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int MutateAndCollect()
    {
        var independent = (DateTimeFormatInfo)DateTimeFormatInfo.InvariantInfo.Clone();
        independent.DateSeparator = Replacement();
        for (int i = 0; i < 64; i++) pressureRoot = new byte[4096 + i];
        GC.Collect();
        collections++;
        return independent.DateSeparator.Length;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool ConsumeChar(ReadOnlySpan<char> old, int marker)
    {
        return marker == 2 && old.Length == 7
            && old[0] == (char)0x00c5 && old[1] == (char)0x6f22
            && old[2] == (char)0xd83d && old[3] == (char)0xde00
            && old[4] == (char)0xd800 && old[5] == (char)0x0041 && old[6] == (char)0xdc00;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool ConsumeByte(ReadOnlySpan<byte> old, int marker)
    {
        return marker == 2 && old.Length == 16
            && old[0] == 0xc3 && old[1] == 0x85
            && old[2] == 0xe6 && old[3] == 0xbc && old[4] == 0xa2
            && old[5] == 0xf0 && old[6] == 0x9f && old[7] == 0x98 && old[8] == 0x80
            && old[9] == 0xef && old[10] == 0xbf && old[11] == 0xbd && old[12] == 0x41
            && old[13] == 0xef && old[14] == 0xbf && old[15] == 0xbd;
    }

    private static void Run(bool pointer)
    {
        string label = pointer ? "pointer" : "direct";
        var chars = CaptureChar(pointer);
        var bytes = CaptureByte(pointer);
        int marker = MutateAndCollect();
        Check(label + "-old-local-char", ConsumeChar(chars, marker));
        Check(label + "-old-local-byte", ConsumeByte(bytes, marker));
        Check(label + "-old-nested-char", ConsumeChar(CaptureChar(pointer), MutateAndCollect()));
        Check(label + "-old-nested-byte", ConsumeByte(CaptureByte(pointer), MutateAndCollect()));

        var owner = (DateTimeFormatInfo)DateTimeFormatInfo.InvariantInfo.Clone();
        owner.DateSeparator = DynamicSeparator();
        var oldChar = pointer ? CharPointer(owner) : CharDirect(owner);
        var oldByte = pointer ? BytePointer(owner) : ByteDirect(owner);
        owner.DateSeparator = Replacement();
        var newChar = pointer ? CharPointer(owner) : CharDirect(owner);
        var newByte = pointer ? BytePointer(owner) : ByteDirect(owner);
        Check(label + "-new-char", newChar.Length == 2 && newChar[0] == (char)0x03bb && newChar[1] == (char)0x4e8c);
        Check(label + "-new-byte", newByte.Length == 5 && newByte[0] == 0xce && newByte[1] == 0xbb
            && newByte[2] == 0xe4 && newByte[3] == 0xba && newByte[4] == 0x8c);
        marker = MutateAndCollect();
        Check(label + "-replaced-old-char", ConsumeChar(oldChar, marker));
        Check(label + "-replaced-old-byte", ConsumeByte(oldByte, marker));
        owner.DateSeparator = new string(Array.Empty<char>());
        Check(label + "-empty", (pointer ? CharPointer(owner) : CharDirect(owner)).Length == 0
            && (pointer ? BytePointer(owner) : ByteDirect(owner)).Length == 0);

        bool nullChar = false, nullByte = false;
        try { _ = (pointer ? CharPointer(null!) : CharDirect(null!)).Length; }
        catch (NullReferenceException) { nullChar = true; }
        try { _ = (pointer ? BytePointer(null!) : ByteDirect(null!)).Length; }
        catch (NullReferenceException) { nullByte = true; }
        Check(label + "-null", nullChar && nullByte);
    }

    public static void Run()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        Run(false);
        Run(true);
        Check("collection-control", collections == 8 && pressureRoot is byte[]);
        Console.WriteLine("DateTime TChar span lifetime end");
    }

    private static void Main() => Run();
}

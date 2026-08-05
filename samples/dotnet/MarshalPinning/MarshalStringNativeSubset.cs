#nullable enable
using System;
using System.Runtime.InteropServices;

// Native-buffer string marshalling, both directions. Decode: Marshal.PtrToStringAnsi
// / PtrToStringUni (and the PtrToStringUTF8 sibling) over hand-written AllocHGlobal
// buffers — the UTF-8 bytes / UTF-16 code units are written one at a time with
// WriteByte/WriteInt16, then decoded back and compared to the source literal
// (non-ASCII included: accents, a bullet, CJK). Encode: StringToHGlobal{Ansi,Uni}
// and the StringToCoTaskMem trio produce caller-owned NUL-terminated native buffers
// — verified byte-by-byte / code-unit-by-code-unit with ReadByte/ReadInt16 against
// known encodings, round-tripped through PtrToString*, then freed; plus the
// ZeroFree* wipe-and-free quintet (and its IntPtr.Zero no-op). Ansi is UTF-8 on
// Unix, so the Ansi and UTF8 forms agree byte-for-byte with real .NET on macOS.
// Null strings/pointers map to IntPtr.Zero/null. No raw addresses are printed.
// CoreLib only; diffed exact vs real .NET.
namespace MarshalStringNativeSubset;

class Program
{
    private const string Mixed = "café•日本語"; // é U+00E9, • U+2022, CJK

    internal static void __GateEntry()
    {
        // UTF-8 bytes of Mixed (c a f é=C3A9 •=E280A2 日=E697A5 本=E69CAC 語=E8AA9E),
        // hand-written into a native buffer + NUL.
        byte[] utf8 =
        {
            0x63, 0x61, 0x66, 0xC3, 0xA9,
            0xE2, 0x80, 0xA2,
            0xE6, 0x97, 0xA5, 0xE6, 0x9C, 0xAC, 0xE8, 0xAA, 0x9E,
        };
        IntPtr a = Marshal.AllocHGlobal(utf8.Length + 1);
        for (int i = 0; i < utf8.Length; i++)
            Marshal.WriteByte(a, i, utf8[i]);
        Marshal.WriteByte(a, utf8.Length, 0);

        string? s1 = Marshal.PtrToStringAnsi(a);
        Console.WriteLine(s1);                         // café•日本語
        Console.WriteLine(s1 == Mixed);                // True
        Console.WriteLine(Marshal.PtrToStringAnsi(a, 5));  // café (first 5 bytes)
        Console.WriteLine(Marshal.PtrToStringUTF8(a) == Mixed); // True
        Marshal.FreeHGlobal(a);

        // UTF-16 code units of Mixed, written one WriteInt16 at a time + NUL.
        IntPtr u = Marshal.AllocHGlobal((Mixed.Length + 1) * 2);
        for (int i = 0; i < Mixed.Length; i++)
            Marshal.WriteInt16(u, i * 2, (short)Mixed[i]);
        Marshal.WriteInt16(u, Mixed.Length * 2, 0);

        string? s2 = Marshal.PtrToStringUni(u);
        Console.WriteLine(s2);                         // café•日本語
        Console.WriteLine(s2 == Mixed);                // True
        Console.WriteLine(Marshal.PtrToStringUni(u, 4));   // café (first 4 chars)
        Marshal.FreeHGlobal(u);

        // Null pointers decode to null strings (1-arg forms), matching .NET.
        Console.WriteLine(Marshal.PtrToStringAnsi(IntPtr.Zero) is null); // True
        Console.WriteLine(Marshal.PtrToStringUni(IntPtr.Zero) is null);  // True
        Console.WriteLine(Marshal.PtrToStringUTF8(IntPtr.Zero) is null); // True

        // StringToHGlobalAnsi: platform-Ansi byte values (UTF-8 on Unix: é = C3 A9,
        // 日 leads with E6; the system code page on Windows), the trailing NUL,
        // then a decode round-trip and the explicit free. The NUL is found by
        // scanning, not read at utf8.Length: the Windows code-page encoding is
        // shorter than the UTF-8 one, so a fixed index lands past the buffer's
        // own NUL and reads uninitialized heap in real .NET (non-deterministic).
        IntPtr ha = Marshal.StringToHGlobalAnsi(Mixed);
        Console.WriteLine(Marshal.ReadByte(ha, 0));    // 99  'c'
        Console.WriteLine(Marshal.ReadByte(ha, 3));    // 195 (0xC3) / Windows best-fit-off '?'
        Console.WriteLine(Marshal.ReadByte(ha, 4));    // 169 (0xA9)
        Console.WriteLine(Marshal.ReadByte(ha, 8));    // 230 (0xE6)
        int alen = 0;
        while (Marshal.ReadByte(ha, alen) != 0)
            alen++;
        Console.WriteLine(Marshal.ReadByte(ha, alen)); // 0 (NUL, at the encoding's own length)
        Console.WriteLine(Marshal.PtrToStringAnsi(ha) == Mixed); // True
        Marshal.FreeHGlobal(ha);

        // StringToHGlobalUni: known UTF-16 code units (語 U+8A9E reads back negative
        // through the signed ReadInt16), the trailing NUL, round-trip, free.
        IntPtr hu = Marshal.StringToHGlobalUni(Mixed);
        Console.WriteLine(Marshal.ReadInt16(hu, 0));      // 99    'c'
        Console.WriteLine(Marshal.ReadInt16(hu, 3 * 2));  // 233   é
        Console.WriteLine(Marshal.ReadInt16(hu, 4 * 2));  // 8226  •
        Console.WriteLine(Marshal.ReadInt16(hu, 5 * 2));  // 26085 日
        Console.WriteLine(Marshal.ReadInt16(hu, 7 * 2));  // -30050 語 (U+8A9E as short)
        Console.WriteLine(Marshal.ReadInt16(hu, Mixed.Length * 2)); // 0 (NUL)
        Console.WriteLine(Marshal.PtrToStringUni(hu) == Mixed);     // True
        Marshal.FreeHGlobal(hu);

        // The CoTaskMem trio round-trips the same way (shared process-heap allocator).
        IntPtr ca = Marshal.StringToCoTaskMemAnsi(Mixed);
        Console.WriteLine(Marshal.PtrToStringAnsi(ca) == Mixed);    // True
        Marshal.FreeCoTaskMem(ca);
        IntPtr cu = Marshal.StringToCoTaskMemUni(Mixed);
        Console.WriteLine(Marshal.PtrToStringUni(cu) == Mixed);     // True
        Marshal.FreeCoTaskMem(cu);
        IntPtr c8 = Marshal.StringToCoTaskMemUTF8(Mixed);
        Console.WriteLine(Marshal.PtrToStringUTF8(c8) == Mixed);    // True
        Marshal.FreeCoTaskMem(c8);

        // Null strings encode to IntPtr.Zero, matching .NET.
        Console.WriteLine(Marshal.StringToHGlobalAnsi(null) == IntPtr.Zero);   // True
        Console.WriteLine(Marshal.StringToHGlobalUni(null) == IntPtr.Zero);    // True
        Console.WriteLine(Marshal.StringToCoTaskMemAnsi(null) == IntPtr.Zero); // True
        Console.WriteLine(Marshal.StringToCoTaskMemUni(null) == IntPtr.Zero);  // True
        Console.WriteLine(Marshal.StringToCoTaskMemUTF8(null) == IntPtr.Zero); // True

        // ZeroFree*: wipe-and-free runs to completion (the buffer is not read after),
        // and IntPtr.Zero is a no-op.
        Marshal.ZeroFreeGlobalAllocAnsi(Marshal.StringToHGlobalAnsi("secret"));
        Marshal.ZeroFreeGlobalAllocUnicode(Marshal.StringToHGlobalUni("secret"));
        Marshal.ZeroFreeCoTaskMemAnsi(Marshal.StringToCoTaskMemAnsi("secret"));
        Marshal.ZeroFreeCoTaskMemUnicode(Marshal.StringToCoTaskMemUni("secret"));
        Marshal.ZeroFreeCoTaskMemUTF8(Marshal.StringToCoTaskMemUTF8("secret"));
        Marshal.ZeroFreeGlobalAllocAnsi(IntPtr.Zero);
        Marshal.ZeroFreeGlobalAllocUnicode(IntPtr.Zero);
        Marshal.ZeroFreeCoTaskMemAnsi(IntPtr.Zero);
        Marshal.ZeroFreeCoTaskMemUnicode(IntPtr.Zero);
        Marshal.ZeroFreeCoTaskMemUTF8(IntPtr.Zero);
        Console.WriteLine("zerofree done");
    }
}

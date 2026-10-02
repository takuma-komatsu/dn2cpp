#nullable disable
using System;
using System.Text;

// A null Encoding receiver of every intercepted GetString overload — byte[],
// (byte[], int, int), byte* and ReadOnlySpan<byte> — raises NullReferenceException
// at the call, ahead of GetString's own null-array and range checks, whichever
// encoding type the receiver is declared as.
namespace EncodingReceiverSubset;

class Program
{
    static Encoding s_encoding;
    static UTF8Encoding s_utf8;
    static ASCIIEncoding s_ascii;
    static UnicodeEncoding s_unicode;

    static void Fault(string label, Func<string> run)
    {
        string text;
        try
        {
            text = "n=" + run().Length;
        }
        catch (Exception ex)
        {
            text = ex.GetType().Name + ": " + ex.Message;
        }
        Console.WriteLine(label + " -> " + text);
    }

    public static unsafe void __GateEntry()
    {
        Console.WriteLine("== encoding receivers ==");
        byte[] bytes = { 0x41, 0x42 };
        Fault("Encoding GetString(bytes)", () => s_encoding.GetString(bytes));
        Fault("Encoding GetString(null)", () => s_encoding.GetString((byte[])null));
        Fault("Encoding GetString(bytes, 0, 2)", () => s_encoding.GetString(bytes, 0, 2));
        Fault("Encoding GetString(null, 0, 0)", () => s_encoding.GetString(null, 0, 0));
        Fault("Encoding GetString(bytes, -1, 0)", () => s_encoding.GetString(bytes, -1, 0));
        Fault("Encoding GetString(span)", () => s_encoding.GetString(new ReadOnlySpan<byte>(bytes)));
        Fault("Encoding GetString(null ptr, 0)", () => s_encoding.GetString((byte*)null, 0));
        Fault("Encoding GetString(ptr, -1)", () =>
        {
            fixed (byte* p = bytes)
                return s_encoding.GetString(p, -1);
        });
        Fault("UTF8Encoding GetString(null)", () => s_utf8.GetString((byte[])null));
        Fault("UTF8Encoding GetString(null, 0, 0)", () => s_utf8.GetString(null, 0, 0));
        Fault("UTF8Encoding GetString(span)", () => s_utf8.GetString(new ReadOnlySpan<byte>(bytes)));
        Fault("UTF8Encoding GetString(null ptr, 0)", () => s_utf8.GetString((byte*)null, 0));
        Fault("ASCIIEncoding GetString(null, 0, 0)", () => s_ascii.GetString(null, 0, 0));
        Fault("ASCIIEncoding GetString(span)", () => s_ascii.GetString(new ReadOnlySpan<byte>(bytes)));
        Fault("UnicodeEncoding GetString(null, 0, 0)", () => s_unicode.GetString(null, 0, 0));
        Fault("UnicodeEncoding GetString(span)", () => s_unicode.GetString(new ReadOnlySpan<byte>(bytes)));
        Fault("UnicodeEncoding GetString(null ptr, 0)", () => s_unicode.GetString((byte*)null, 0));
        foreach (int length in new[] { 0, 1, 2 })
        {
            Fault("base UTF8 null span " + length, () => Encoding.UTF8.GetString(new ReadOnlySpan<byte>((void*)0, length)));
            Fault("base ASCII null span " + length, () => Encoding.ASCII.GetString(new ReadOnlySpan<byte>((void*)0, length)));
            Fault("base Unicode null span " + length, () => Encoding.Unicode.GetString(new ReadOnlySpan<byte>((void*)0, length)));
            Fault("base UTF32 null span " + length, () => Encoding.UTF32.GetString(new ReadOnlySpan<byte>((void*)0, length)));
            Fault("typed UTF8 null span " + length, () => new UTF8Encoding().GetString(new ReadOnlySpan<byte>((void*)0, length)));
            Fault("typed ASCII null span " + length, () => new ASCIIEncoding().GetString(new ReadOnlySpan<byte>((void*)0, length)));
            Fault("typed Unicode null span " + length, () => new UnicodeEncoding().GetString(new ReadOnlySpan<byte>((void*)0, length)));
            Fault("typed UTF8 null pointer " + length, () => new UTF8Encoding().GetString((byte*)null, length));
            Fault("typed Unicode null pointer " + length, () => new UnicodeEncoding().GetString((byte*)null, length));
        }
        Console.WriteLine("encoding receivers end");
    }
}

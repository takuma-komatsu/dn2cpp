#nullable disable
using System;

// new string(char*, int, int), string.ToCharArray() and Exception.InnerException.
// Chars print as 4-hex-digit code units so embedded NUL, lone surrogates and
// surrogate pairs stay visible and the stdout diff stays ASCII-safe.
namespace StringCtorMiscSubset;


static class Program
{
    static void DumpUnits(string label, string s)
    {
        Console.Write(label + " len=" + s.Length + " units:");
        foreach (char c in s)
            Console.Write(" " + ((int)c).ToString("X4"));
        Console.WriteLine();
    }

    static void DumpUnits(string label, char[] a)
    {
        Console.Write(label + " len=" + a.Length + " units:");
        foreach (char c in a)
            Console.Write(" " + ((int)c).ToString("X4"));
        Console.WriteLine();
    }

    static unsafe void NewStringPointer()
    {
        Console.WriteLine("-- new string(char*, int, int) --");
        // A buffer with ASCII, a non-ASCII char (U+00E9 'é'), a surrogate pair
        // (U+1F600 = D83D DE00), an embedded NUL, and a lone surrogate (D800).
        char[] buf = { 'H', 'i', 'é', '\uD83D', '\uDE00', '\0', '\uD800', 'Z' };
        fixed (char* p = buf)
        {
            DumpUnits("full(0,8):", new string(p, 0, 8));
            DumpUnits("empty(3,0):", new string(p, 3, 0));
            DumpUnits("slice(2,3):", new string(p, 2, 3));       // é + surrogate pair start
            DumpUnits("nul-incl(4,3):", new string(p, 4, 3));    // DE00 0000 D800
            DumpUnits("tail(7,1):", new string(p, 7, 1));        // Z
        }
        // Pointer into a managed string's pinned buffer.
        string src = "Metadata";
        fixed (char* sp = src)
        {
            DumpUnits("str-slice(4,4):", new string(sp, 4, 4)); // "data"
        }
    }

    static void StringToCharArray()
    {
        Console.WriteLine("-- string.ToCharArray() --");
        DumpUnits("empty:", "".ToCharArray());
        DumpUnits("ascii:", "abc".ToCharArray());
        DumpUnits("surrogate:", "a😀b".ToCharArray()); // a + pair + b = 4 units
        char[] arr = "xyz".ToCharArray();
        Console.WriteLine("type=" + arr.GetType().Name);
        // Independent copy: mutating the array does not change the source string.
        arr[0] = 'Q';
        DumpUnits("mutated:", arr);
    }

    static void ExceptionInner()
    {
        Console.WriteLine("-- Exception.InnerException --");
        Exception noInner = new InvalidOperationException("outer-only");
        Console.WriteLine("no-inner: " + (noInner.InnerException is null ? "null" : "NOT-NULL"));

        Exception inner = new ArgumentException("the-inner-msg");
        Exception withInner = new InvalidOperationException("the-outer-msg", inner);
        Exception got = withInner.InnerException;
        Console.WriteLine("with-inner null? " + (got is null ? "yes" : "no"));
        Console.WriteLine("with-inner msg: " + got.Message);
        Console.WriteLine("with-inner type: " + got.GetType().Name);
        Console.WriteLine("same-instance: " + object.ReferenceEquals(got, inner));

        // Caught and re-inspected through a typed catch.
        try
        {
            throw new InvalidOperationException("wrap", new NotSupportedException("nested-inner"));
        }
        catch (InvalidOperationException ex)
        {
            Console.WriteLine("caught inner null? " + (ex.InnerException is null ? "yes" : "no"));
            Console.WriteLine("caught inner msg: " + ex.InnerException.Message);
        }
    }internal static void Run()
    {
        NewStringPointer();
        StringToCharArray();
        ExceptionInner();
    }
}

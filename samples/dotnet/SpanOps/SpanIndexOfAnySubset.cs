#nullable enable
using System;
using System.Buffers;
namespace SpanIndexOfAnySubset;


static class Program
{internal static void __GateEntry()
    {
        // int: IndexOfAny / LastIndexOfAny over a values span.
        int[] a = { 5, 3, 7, 2, 9, 7 };
        ReadOnlySpan<int> vals = stackalloc int[] { 2, 7 };
        Console.WriteLine(a.AsSpan().IndexOfAny(vals));      // 2
        Console.WriteLine(a.AsSpan().LastIndexOfAny(vals));  // 5
        Console.WriteLine(((ReadOnlySpan<int>)a).IndexOfAny(vals)); // 2

        // No match / empty values / empty self.
        ReadOnlySpan<int> none = stackalloc int[] { 100, 200 };
        Console.WriteLine(a.AsSpan().IndexOfAny(none));      // -1
        ReadOnlySpan<int> empty = ReadOnlySpan<int>.Empty;
        Console.WriteLine(a.AsSpan().IndexOfAny(empty));     // -1
        Console.WriteLine(ReadOnlySpan<int>.Empty.IndexOfAny(vals)); // -1

        // char: separators-set scan over a string-derived span.
        string s = "hello world";
        ReadOnlySpan<char> seps = stackalloc char[] { 'o', 'w' };
        Console.WriteLine(s.AsSpan().IndexOfAny(seps));      // 4
        Console.WriteLine(s.AsSpan().LastIndexOfAny(seps));  // 7
        ReadOnlySpan<char> miss = stackalloc char[] { 'z', 'q' };
        Console.WriteLine(s.AsSpan().IndexOfAny(miss));      // -1

        // string elements (reference, ordinal equality).
        string[] ws = { "a", "bb", "ccc", "bb" };
        ReadOnlySpan<string> wv = new[] { "bb", "zz" };
        Console.WriteLine(ws.AsSpan().IndexOfAny(wv));       // 1
        Console.WriteLine(ws.AsSpan().LastIndexOfAny(wv));   // 3

        // SearchValues<byte>: Create plus the membership-scan span forms.
        // IndexOfAny / LastIndexOfAny and the Except / ContainsAny negations all
        // route through the one runtime set.
        byte[] data = { 1, 2, 3, 4, 5, 6, 7 };
        SearchValues<byte> bsv = SearchValues.Create(new byte[] { 3, 5 });
        Console.WriteLine(((ReadOnlySpan<byte>)data).IndexOfAny(bsv));           // 2
        Console.WriteLine(((ReadOnlySpan<byte>)data).LastIndexOfAny(bsv));       // 4
        Console.WriteLine(((ReadOnlySpan<byte>)data).IndexOfAnyExcept(bsv));     // 0
        Console.WriteLine(((ReadOnlySpan<byte>)data).LastIndexOfAnyExcept(bsv)); // 6
        Console.WriteLine(((ReadOnlySpan<byte>)data).ContainsAny(bsv));          // True
        Console.WriteLine(((ReadOnlySpan<byte>)data).ContainsAnyExcept(bsv));    // True
        byte[] only35 = { 3, 5, 5, 3 };
        Console.WriteLine(((ReadOnlySpan<byte>)only35).IndexOfAnyExcept(bsv));   // -1
        Console.WriteLine(((ReadOnlySpan<byte>)only35).ContainsAnyExcept(bsv));  // False

        // SearchValues<char>: a non-ASCII value (€ = U+20AC > 255) exercises the
        // `hi` overflow set alongside the 0..255 fast table.
        SearchValues<char> csv = SearchValues.Create("ow€");
        string es = "hi €, low";
        Console.WriteLine(es.AsSpan().IndexOfAny(csv));         // 3 (the €)
        Console.WriteLine(es.AsSpan().LastIndexOfAny(csv));     // 8 (the 'w')
        Console.WriteLine(es.AsSpan().ContainsAny(csv));        // True
        Console.WriteLine(es.AsSpan().IndexOfAnyExcept(csv));   // 0 ('h')
        Console.WriteLine("no-euro".AsSpan().ContainsAny(csv)); // True ('o','w')
        Console.WriteLine("abc".AsSpan().ContainsAny(csv));     // False

        // SearchValues<string>: Ordinal and OrdinalIgnoreCase, the latter the exact
        // BMP ordinal fold, so non-ASCII candidates match case-insensitively too.
        SearchValues<string> sOrd = SearchValues.Create(new[] { "welt", "\u00E4pfel" }, StringComparison.Ordinal);
        SearchValues<string> sOic = SearchValues.Create(new[] { "welt", "\u00E4pfel" }, StringComparison.OrdinalIgnoreCase);
        Console.WriteLine("Die \u00C4PFEL hier".AsSpan().IndexOfAny(sOrd));  // -1 (case differs)
        Console.WriteLine("Die \u00C4PFEL hier".AsSpan().IndexOfAny(sOic));  // 4
        Console.WriteLine("hallo WELT".AsSpan().IndexOfAny(sOic));            // 6
        Console.WriteLine("hallo welt".AsSpan().IndexOfAny(sOrd));            // 6
        Console.WriteLine("nichts".AsSpan().IndexOfAny(sOic));                // -1

        // A NULL candidate is a catchable ArgumentNullException, not an abort:
        // a candidate list assembled from configuration is untrusted text, and
        // one null row must leave the set-building loop able to skip and carry
        // on. Type name only, since the message is localized.
        string[] rows = { "welt", null, "äpfel", null };
        int built = 0, bad = 0;
        foreach (string row in rows)
        {
            try
            {
                SearchValues<string> one = SearchValues.Create(new[] { row }, StringComparison.Ordinal);
                if ("hallo welt".AsSpan().IndexOfAny(one) >= -1) built++;
            }
            catch (ArgumentNullException)
            {
                bad++;
            }
        }
        Console.WriteLine("sv create loop: built=" + built + " bad=" + bad);
        try
        {
            SearchValues<string> bad2 = SearchValues.Create(new[] { "welt", null }, StringComparison.Ordinal);
            Console.WriteLine("unreachable " + "x".AsSpan().IndexOfAny(bad2));
        }
        catch (ArgumentNullException)
        {
            Console.WriteLine("typed catch: ArgumentNullException");
        }
        catch (Exception e)
        {
            Console.WriteLine("typed catch: fell through to " + e.GetType().Name);
        }

        // An EMPTY-string candidate: .NET accepts it and the set then matches at
        // index 0 for EVERY input, the empty span included. The empty-span row is
        // what pins the scan's inclusive start bound.
        SearchValues<string> sEmptyOnly = SearchValues.Create(new[] { "" }, StringComparison.Ordinal);
        Console.WriteLine("sv empty-only: " + "abc".AsSpan().IndexOfAny(sEmptyOnly));   // 0
        Console.WriteLine("sv empty-only []: " + "".AsSpan().IndexOfAny(sEmptyOnly));   // 0
        Console.WriteLine("sv empty-only any: " + "abc".AsSpan().ContainsAny(sEmptyOnly)); // True
        SearchValues<string> sEmptyMix = SearchValues.Create(new[] { "bc", "" }, StringComparison.Ordinal);
        Console.WriteLine("sv empty-mix: " + "abc".AsSpan().IndexOfAny(sEmptyMix));     // 0
        SearchValues<string> sEmptyOic = SearchValues.Create(new[] { "AB", "" }, StringComparison.OrdinalIgnoreCase);
        Console.WriteLine("sv empty-oic: " + "xab".AsSpan().IndexOfAny(sEmptyOic));     // 0
        Console.WriteLine("sv empty-oic []: " + "".AsSpan().IndexOfAny(sEmptyOic));     // 0
        // The control: with no empty candidate the empty span still answers -1, so
        // the inclusive bound above changed nothing else.
        Console.WriteLine("sv nonempty []: " + "".AsSpan().IndexOfAny(sOrd));           // -1
    }
}

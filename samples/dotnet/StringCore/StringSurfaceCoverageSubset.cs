using System;
using System.Globalization;
using System.Text;

internal static class StringSurfaceCoverageSubset
{
    private static void Fault<T>(string label, Func<T> action)
    {
        try { Console.WriteLine(label + "=" + action()); }
        catch (ArgumentException e) { Console.WriteLine(label + "=" + e.GetType().Name + ":" + e.ParamName); }
    }

    internal static unsafe void Run()
    {
        Console.WriteLine("== String public API coverage ==");
        const StringComparison Ord = StringComparison.Ordinal;
        const StringComparison Ignore = StringComparison.OrdinalIgnoreCase;
        string word = "AbAb";
        Console.WriteLine($"compare={Math.Sign(string.Compare("a", "b"))}:{Math.Sign(string.Compare(null, "b"))}");
        Console.WriteLine($"prefix={word.StartsWith("ab", true, CultureInfo.InvariantCulture)}:{word.EndsWith("ab", true, null)}:" +
            $"{word.StartsWith('a', Ignore)}:{word.EndsWith('B', Ignore)}");
        Console.WriteLine($"char windows={word.IndexOf('a', 1, Ignore)}:{word.IndexOf('a', 1, 1, Ignore)}:" +
            $"{word.LastIndexOf('a', Ignore)}:{word.LastIndexOf('a', 1, Ignore)}:{word.LastIndexOf('a', 2, 1, Ignore)}");
        Console.WriteLine($"string windows={word.IndexOf("ab", 1, 3, Ignore)}:{word.LastIndexOf("ab", 2, 3, Ignore)}");
        Console.WriteLine("join range=" + string.Join(':', new[] { "-", "a", "b", "-" }, 1, 2));
        string[] values = { "-", word, null };
        Console.WriteLine($"join identity={ReferenceEquals(string.Join(':', values, 1, 0), string.Empty)}:" +
            $"{ReferenceEquals(string.Join(':', values, 1, 1), word)}:{ReferenceEquals(string.Join(':', values, 2, 1), string.Empty)}");
        Console.WriteLine("type code=" + (int)word.GetTypeCode());
#pragma warning disable CS0618
        Console.WriteLine("copy=" + (string.Copy(word) == word) + ":" + ReferenceEquals(string.Copy(word), word));
        Fault("copy null", () => string.Copy(null));
#pragma warning restore CS0618
        string breakKinds = "a\r\nb\rc\nd\ve\ff\u0085g\u2028h\u2029i\u001cj";
        Console.WriteLine("line endings=" + breakKinds.ReplaceLineEndings("|"));
        Console.WriteLine("line endings default=" + (breakKinds.ReplaceLineEndings() == breakKinds.ReplaceLineEndings(Environment.NewLine)));
        Console.WriteLine("line endings identity=" + ReferenceEquals(word.ReplaceLineEndings(), word));
        Fault("line endings null", () => word.ReplaceLineEndings(null));
        Console.WriteLine("ordinal case=" + "AzÉıſ".ToUpperOrdinal() + ":" + "AZÉİΣ".ToLowerOrdinal());

        var plant = new Rune(0x1F331);
        string plants = "🌱a🌰🌱";
        Console.WriteLine($"rune edges={plants.Contains(plant)}:{plants.Contains(plant, Ord)}:" +
            $"{plants.StartsWith(plant)}:{plants.StartsWith(plant, Ignore)}:{plants.EndsWith(plant)}:{plants.EndsWith(plant, Ignore)}");
        Console.WriteLine($"rune first={plants.IndexOf(plant)}:{plants.IndexOf(plant, 1)}:{plants.IndexOf(plant, 2, 3)}:" +
            $"{plants.IndexOf(plant, Ignore)}:{plants.IndexOf(plant, 1, Ignore)}:{plants.IndexOf(plant, 1, 6, Ord)}");
        Console.WriteLine($"rune last={plants.LastIndexOf(plant)}:{plants.LastIndexOf(plant, 4)}:{plants.LastIndexOf(plant, 6, 5)}:" +
            $"{plants.LastIndexOf(plant, Ignore)}:{plants.LastIndexOf(plant, 4, Ignore)}:{plants.LastIndexOf(plant, 5, 6, Ord)}");
        Console.WriteLine($"rune edits={plants.Replace(plant, new Rune('Z'))}:{plants.Replace(new Rune('a'), plant)}:" +
            $"{plants.Trim(plant)}:{plants.TrimStart(plant)}:{plants.TrimEnd(plant)}");
        Console.WriteLine($"rune replace identity={ReferenceEquals(plants.Replace(plant, plant), plants)}:" +
            $"{ReferenceEquals(word.Replace(plant, plant), word)}");
        Console.WriteLine($"rune trim identity={ReferenceEquals("🌱".Trim(plant), string.Empty)}:" +
            $"{ReferenceEquals("🌱".TrimStart(plant), string.Empty)}:{ReferenceEquals(word.TrimEnd(plant), word)}");
        string upperDeseret = "\U00010400";
        var lowerDeseret = new Rune(0x10428);
        Console.WriteLine($"supplementary fold={upperDeseret.StartsWith(lowerDeseret, Ignore)}:{upperDeseret.EndsWith(lowerDeseret, Ignore)}:" +
            $"{upperDeseret.IndexOf(lowerDeseret, Ignore)}:{upperDeseret.LastIndexOf(lowerDeseret, Ignore)}:{upperDeseret.Contains(lowerDeseret, Ignore)}");
        Fault("rune forward bounds", () => plants.IndexOf(plant, -1, 1, (StringComparison)6));
        Fault("rune reverse bounds", () => plants.LastIndexOf(plant, 0, 2, Ord));
        Fault("rune comparison", () => plants.StartsWith(plant, (StringComparison)6));
        Fault("char reverse bounds", () => word.LastIndexOf('a', 0, 2, Ignore));
        Fault("char forward bounds", () => word.IndexOf('a', -1, 2, Ignore));

        sbyte* bytes = stackalloc sbyte[] { 0x7e, 0x61, 0, 0x62 };
        string ansi = new string(bytes, 1, 3);
        string utf8 = new string(bytes, 1, 3, Encoding.UTF8);
        Console.WriteLine($"pointer ctor={ansi.Length}:{(int)ansi[1]}:{ansi == utf8}");
        Console.WriteLine("pointer null empty=" + ReferenceEquals(new string((sbyte*)null, 0, 0), string.Empty));
        sbyte* encoded = stackalloc sbyte[] { 0, 65, -19, -96, -128 };
        Console.WriteLine($"pointer encodings={(int)new string(encoded, 0, 2, Encoding.BigEndianUnicode)[0]}:" +
            (int)new string(encoded, 2, 1, Encoding.Latin1)[0]);
        Console.WriteLine("pointer encoded empty=" + ReferenceEquals(new string(bytes, 0, 0, Encoding.UTF8), string.Empty) +
            ":" + new string(bytes, 0, 0, new EmptyInputEncoding()));
        Fault("pointer strict UTF8", () => new string(encoded, 2, 3, new UTF8Encoding(false, true)));
        Console.WriteLine("pointer default malformed=" + (OperatingSystem.IsWindows() ||
            new string(encoded, 2, 3) == "\uFFFD\uFFFD\uFFFD"));
        Fault("pointer null", () => new string((sbyte*)null, 0, 1, Encoding.UTF8));
        Fault("pointer default bounds", () => new string((sbyte*)null, -1, -1, null));
        Fault("pointer explicit bounds", () => new string((sbyte*)null, -1, -1, Encoding.UTF8));
        Console.WriteLine("String public API coverage end");
    }

    private sealed unsafe class EmptyInputEncoding : UTF8Encoding
    {
        public override int GetCharCount(byte* bytes, int count) => bytes != null && count == 0 ? 1 : 0;

        public override int GetChars(byte* bytes, int byteCount, char* chars, int charCount)
        {
            chars[0] = 'x';
            return 1;
        }
    }
}

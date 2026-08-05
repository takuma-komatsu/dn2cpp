#nullable enable
using System;
using System.Text.RegularExpressions;

// SUBJECT: RegexOptions — Multiline, Singleline, IgnorePatternWhitespace,
// RightToLeft, and Compiled. dn2cpp folds RuntimeFeature.IsDynamicCodeCompiled
// to false, so Compiled runs on the interpreter and must still match real
// .NET's compiled engine exactly.
namespace RegexOptionsSubset;

static class Program
{
    internal static void __GateEntry()
    {
        string text = "first line\nsecond line\nthird line";

        Console.WriteLine("-- Multiline --");
        Console.WriteLine(Regex.Matches(text, @"^\w+", RegexOptions.Multiline).Count);
        foreach (Match m in Regex.Matches(text, @"\w+$", RegexOptions.Multiline))
            Console.WriteLine(m.Value);

        Console.WriteLine("-- Singleline --");
        Console.WriteLine(Regex.IsMatch(text, @"first.*third"));
        Console.WriteLine(Regex.IsMatch(text, @"first.*third", RegexOptions.Singleline));
        Console.WriteLine(Regex.Match(text, @"^first.*$", RegexOptions.Singleline).Length);

        Console.WriteLine("-- IgnorePatternWhitespace --");
        Regex spaced = new Regex(@"
            \d{3}    # area
            -
            \d{4}    # number
        ", RegexOptions.IgnorePatternWhitespace);
        Console.WriteLine(spaced.Match("call 555-1234").Value);

        Console.WriteLine("-- RightToLeft --");
        Regex rtl = new Regex(@"\d+", RegexOptions.RightToLeft);
        Match r = rtl.Match("a1 b22 c333");
        Console.WriteLine(r.Value + " " + r.Index);
        Console.WriteLine(rtl.Match("a1 b22 c333", 4).Value);
        Console.WriteLine(rtl.IsMatch("no digits"));

        Console.WriteLine("-- Compiled degrades to the interpreter --");
        Regex compiled = new Regex(@"(?<w>\w+)-(\d+)", RegexOptions.Compiled);
        Match cm = compiled.Match("item-42 thing-7");
        Console.WriteLine(cm.Success + " " + cm.Value + " " + cm.Groups["w"].Value + " " + cm.Groups[2].Value);
        Console.WriteLine(compiled.Matches("item-42 thing-7").Count);
        Console.WriteLine(compiled.Replace("item-42", "${w}"));
        Console.WriteLine(Regex.IsMatch("HELLO", "hello", RegexOptions.Compiled | RegexOptions.IgnoreCase));
        Console.WriteLine(compiled.Options);

        Console.WriteLine("-- combined options --");
        Console.WriteLine(Regex.Matches(text, @"^\w+", RegexOptions.Multiline | RegexOptions.IgnoreCase).Count);
    }
}

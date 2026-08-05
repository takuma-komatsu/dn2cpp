#nullable enable
using System;
using System.Text.RegularExpressions;

// SUBJECT: Regex groups — numbered, named, unmatched, Captures of a repeated
// group, ExplicitCapture, and the group-name reflection APIs.
namespace RegexGroupsSubset;

static class Program
{
    internal static void __GateEntry()
    {
        Console.WriteLine("-- numbered groups --");
        Match m = Regex.Match("2026-07-08", @"(\d{4})-(\d{2})-(\d{2})");
        Console.WriteLine(m.Groups.Count);
        for (int i = 0; i < m.Groups.Count; i++)
            Console.WriteLine(i + ":" + m.Groups[i].Value);

        Console.WriteLine("-- named groups --");
        Match n = Regex.Match("takuma@example.com", @"(?<user>[^@]+)@(?<host>.+)");
        Console.WriteLine(n.Groups["user"].Value);
        Console.WriteLine(n.Groups["host"].Value);
        Console.WriteLine(n.Groups["user"].Name + " " + n.Groups["user"].Index);

        Console.WriteLine("-- unmatched group --");
        Match u = Regex.Match("abc", @"(a)(x)?(c)?");
        Console.WriteLine(u.Groups[1].Success + " " + u.Groups[1].Value);
        Console.WriteLine(u.Groups[2].Success + " [" + u.Groups[2].Value + "]");
        Console.WriteLine(u.Groups[3].Success + " " + u.Groups[3].Value);

        Console.WriteLine("-- captures --");
        Match c = Regex.Match("a1b2c3", @"(?:([a-z])\d)+");
        CaptureCollection caps = c.Groups[1].Captures;
        Console.WriteLine(caps.Count);
        foreach (Capture cap in caps)
            Console.WriteLine(cap.Index + ":" + cap.Value);

        Console.WriteLine("-- ExplicitCapture / name APIs --");
        Regex re = new Regex(@"(?<word>\w+)\s+(\d+)", RegexOptions.ExplicitCapture);
        Console.WriteLine(string.Join(",", re.GetGroupNames()));
        Console.WriteLine(string.Join(",", re.GetGroupNumbers()));
        Console.WriteLine(re.GroupNameFromNumber(1));
        Console.WriteLine(re.GroupNumberFromName("word"));
        Console.WriteLine(re.Match("hello 42").Groups["word"].Value);
    }
}

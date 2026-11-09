using System;
using System.Globalization;

namespace DateTimeFormatInfoSubset;

internal static class Program
{
    private static void NameArray(string label, Func<DateTimeFormatInfo, string[]> read)
    {
        var invariant = DateTimeFormatInfo.InvariantInfo;
        var clone = (DateTimeFormatInfo)invariant.Clone();
        foreach (var info in new[] { invariant, clone })
        {
            string[] first = read(info), second = read(info);
            string before = string.Join(",", first);
            first[0] = "changed";
            Console.WriteLine(label + (info == clone ? " clone " : " invariant ") + before
                + " shared=" + ReferenceEquals(first, second) + " after=" + read(info)[0]);
        }
    }

    public static void Run()
    {
        Console.WriteLine("== DateTimeFormatInfo names ==");
        NameArray("AbbreviatedMonthNames", info => info.AbbreviatedMonthNames);
        NameArray("AbbreviatedMonthGenitiveNames", info => info.AbbreviatedMonthGenitiveNames);
        Console.WriteLine("DateTimeFormatInfo names end");
    }
}

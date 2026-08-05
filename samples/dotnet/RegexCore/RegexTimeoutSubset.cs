#nullable enable
using System;
using System.Text.RegularExpressions;

// SUBJECT: match timeouts (Environment.TickCount64-based). Only the exception
// type name is printed — how long each runtime takes to trip differs.
namespace RegexTimeoutSubset;

static class Program
{
    internal static void __GateEntry()
    {
        Console.WriteLine("-- catastrophic pattern times out --");
        try
        {
            Regex slow = new Regex(@"(a+)+$", RegexOptions.None, TimeSpan.FromMilliseconds(50));
            slow.IsMatch(new string('a', 40) + "!");
            Console.WriteLine("no timeout");
        }
        catch (RegexMatchTimeoutException)
        {
            Console.WriteLine(nameof(RegexMatchTimeoutException));
        }

        Console.WriteLine("-- normal pattern under a timeout --");
        Regex ok = new Regex(@"\d+", RegexOptions.None, TimeSpan.FromSeconds(10));
        Console.WriteLine(ok.Match("abc 123").Value);
        Console.WriteLine(ok.MatchTimeout == TimeSpan.FromSeconds(10));
        Console.WriteLine(new Regex("x").MatchTimeout == Regex.InfiniteMatchTimeout);
    }
}

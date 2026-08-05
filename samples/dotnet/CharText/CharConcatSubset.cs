#nullable enable
using System;
using System.Collections.Generic;
namespace CharConcatSubset;


class Program
{
    internal static void __GateEntry()
    {
        // Roslyn lowers a char inside a string concat to String.Concat over
        // ReadOnlySpan<char> (a 1-char span over the char + string->span
        // op_Implicit), avoiding the single-char allocation. Each shape below
        // exercises that path.
        char c = 'a';
        string s = "5";

        // 2-arg: char.ToString() + string
        Console.WriteLine(c.ToString() + s);

        // 3-arg: char.ToString() + literal + string
        Console.WriteLine(c.ToString() + ":" + s);

        // two single-char spans + a string (3-arg)
        char d = 'z';
        Console.WriteLine(c.ToString() + d.ToString() + s);

        // 4-arg: char + string + char + string
        Console.WriteLine(c.ToString() + s + d.ToString() + "!");

        // string + char (char on the right)
        Console.WriteLine(s + c.ToString());

        // build a string from chars in a loop (group-key style code)
        string[] words = { "apple", "banana", "cherry" };
        List<string> tagged = new List<string>();
        foreach (string w in words)
            tagged.Add(w[0].ToString() + "=" + w);
        Console.WriteLine(string.Join(",", tagged));

        // GroupBy-style: first char as a concatenated label
        Dictionary<string, int> counts = new Dictionary<string, int>();
        foreach (string w in words)
        {
            string key = "<" + w[0].ToString() + ">";
            counts.TryGetValue(key, out int n);
            counts[key] = n + 1;
        }
        Console.WriteLine(counts["<a>"] + " " + counts["<b>"] + " " + counts["<c>"]);
    }
}

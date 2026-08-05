#nullable enable
// String and user-class keys through the shared reference-canonical Dictionary
// bodies: content-hashing on the default comparer path, a stored
// StringComparer.OrdinalIgnoreCase dispatched through the comparer alias rows,
// and a record key resolving its value-equality overrides at run time. Real
// System.Private.CoreLib (-r), run vs .NET.
using System;
using System.Collections.Generic;
namespace StringKeyDictSubset;

record RecKey(string Name, int N);

class Program
{
    internal static void __GateEntry()
    {
        var byName = new Dictionary<string, int>();
        byName.Add("alpha", 1);
        byName.Add("Beta", 2);
        byName["gamma"] = 3;
        byName["alpha"] = 11;
        Console.WriteLine("str count=" + byName.Count);
        Console.WriteLine("str [alpha]=" + byName["alpha"]);
        Console.WriteLine("str has beta=" + byName.ContainsKey("beta"));
        Console.WriteLine("str has Beta=" + byName.ContainsKey("Beta"));
        // A key built at runtime (no literal interning shortcut).
        string ga = "gam" + int.Parse("3").ToString().Substring(0, 0) + "ma";
        Console.WriteLine("str [gamma via concat]=" + byName[ga]);
        Console.WriteLine("str remove Beta=" + byName.Remove("Beta"));
        foreach (var kv in byName)
            Console.WriteLine("str " + kv.Key + "->" + kv.Value);

        var ci = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        ci.Add("Key", 1);
        ci["KEY"] = 2;
        ci["extra"] = 3;
        Console.WriteLine("ci count=" + ci.Count);
        Console.WriteLine("ci [key]=" + ci["key"]);
        Console.WriteLine("ci has kEy=" + ci.ContainsKey("kEy"));
        Console.WriteLine("ci remove KeY=" + ci.Remove("KeY"));
        Console.WriteLine("ci count after=" + ci.Count);

        var byRec = new Dictionary<RecKey, string>();
        byRec.Add(new RecKey("a", 1), "first");
        byRec.Add(new RecKey("b", 2), "second");
        // Value equality: a FRESH equal record instance hits the same entry.
        Console.WriteLine("rec [a,1]=" + byRec[new RecKey("a", 1)]);
        Console.WriteLine("rec has (b,2)=" + byRec.ContainsKey(new RecKey("b", 2)));
        Console.WriteLine("rec has (b,3)=" + byRec.ContainsKey(new RecKey("b", 3)));
        byRec[new RecKey("a", 1)] = "FIRST";
        Console.WriteLine("rec count=" + byRec.Count);
        Console.WriteLine("rec remove (a,1)=" + byRec.Remove(new RecKey("a", 1)));
        foreach (var kv in byRec)
            Console.WriteLine("rec " + kv.Key + "->" + kv.Value);
    }
}

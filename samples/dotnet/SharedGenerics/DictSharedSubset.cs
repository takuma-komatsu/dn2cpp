#nullable enable
// Dictionary<int,string> and Dictionary<Hue,object> collapse to one canonical
// group (int joins the CnInt32 width placeholder, every reference argument the
// CnRef one) and run full add/lookup/enumerate/remove against the shared bodies.
// Real System.Private.CoreLib (-r), run vs .NET; the gate additionally asserts by
// symbol grep that the two instantiations genuinely share one body set.
using System;
using System.Collections.Generic;
namespace DictSharedSubset;

enum Hue { Red = 1, Green = 2, Blue = 7 }

class Program
{
    internal static void __GateEntry()
    {
        var byInt = new Dictionary<int, string>();
        byInt.Add(10, "ten");
        byInt.Add(20, "twenty");
        byInt[30] = "thirty";
        byInt[20] = "TWENTY";
        Console.WriteLine("int count=" + byInt.Count);
        Console.WriteLine("int [20]=" + byInt[20]);
        Console.WriteLine("int has 10=" + byInt.ContainsKey(10));
        Console.WriteLine("int has 99=" + byInt.ContainsKey(99));
        Console.WriteLine("int tryget=" + (byInt.TryGetValue(30, out var t30) ? t30 : "?"));
        Console.WriteLine("int remove 10=" + byInt.Remove(10));
        Console.WriteLine("int remove 10 again=" + byInt.Remove(10));
        foreach (var kv in byInt)
            Console.WriteLine("int " + kv.Key + "->" + kv.Value);
        foreach (int k in byInt.Keys)
            Console.WriteLine("int key " + k);

        var byHue = new Dictionary<Hue, object>();
        byHue.Add(Hue.Red, "crimson");
        byHue.Add(Hue.Green, 42);
        byHue[Hue.Blue] = "azure";
        Console.WriteLine("hue count=" + byHue.Count);
        Console.WriteLine("hue [Red]=" + byHue[Hue.Red]);
        Console.WriteLine("hue [Green]=" + byHue[Hue.Green]);
        Console.WriteLine("hue has Blue=" + byHue.ContainsKey(Hue.Blue));
        Console.WriteLine("hue remove Green=" + byHue.Remove(Hue.Green));
        foreach (var kv in byHue)
            Console.WriteLine("hue " + kv.Key + "->" + kv.Value);
        foreach (object v in byHue.Values)
            Console.WriteLine("hue val " + v);

        // Identity stays per instantiation even though the bodies are shared.
        Console.WriteLine("distinct types=" + (byInt.GetType() != byHue.GetType()));
        Console.WriteLine("int self=" + (byInt.GetType() == typeof(Dictionary<int, string>)));
        Console.WriteLine("hue self=" + (byHue.GetType() == typeof(Dictionary<Hue, object>)));
    }
}

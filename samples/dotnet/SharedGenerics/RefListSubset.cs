#nullable enable
// Reference-element List<T> instantiations sharing one canonical body set, plus
// the reference semantics that must NOT be canonicalized: covariance resolves
// against the REAL interface type-infos, and ToArray keeps the exact array type
// identity. Real System.Private.CoreLib (-r), run vs .NET.
using System;
using System.Collections.Generic;
namespace RefListSubset;

class Widget
{
    public string Tag;
    public Widget(string tag) { Tag = tag; }
    public override string ToString() => "Widget(" + Tag + ")";
}

class Program
{
    internal static void __GateEntry()
    {
        var strings = new List<string> { "one", "two" };
        strings.Add("three");
        strings.Insert(1, "half");
        Console.WriteLine("ls count=" + strings.Count);
        Console.WriteLine("ls idx two=" + strings.IndexOf("two"));
        Console.WriteLine("ls has three=" + strings.Contains("three"));
        Console.WriteLine("ls remove half=" + strings.Remove("half"));
        foreach (var s in strings)
            Console.WriteLine("ls " + s);

        var objects = new List<object> { "boxed str", 5, new Widget("w0") };
        Console.WriteLine("lo count=" + objects.Count);
        foreach (var o in objects)
            Console.WriteLine("lo " + o);

        var widgets = new List<Widget>();
        widgets.Add(new Widget("a"));
        widgets.Add(new Widget("b"));
        widgets.RemoveAt(0);
        Console.WriteLine("lw count=" + widgets.Count);
        Console.WriteLine("lw [0]=" + widgets[0]);

        // Variance must use the REAL interface type-infos, never the canonical
        // alias handles.
        IEnumerable<object> co = strings;
        foreach (object o in co)
            Console.WriteLine("co " + o);
        Console.WriteLine("co is=" + (((object)strings) is IEnumerable<object>));
        Console.WriteLine("co int is=" + (((object)new List<int> { 1 }) is IEnumerable<object>));
        Console.WriteLine("co arr is=" + (((object)new[] { "x" }) is IEnumerable<object>));

        // Exact array identity out of the shared ToArray.
        string[] arr = strings.ToArray();
        Console.WriteLine("arr len=" + arr.Length);
        Console.WriteLine("arr type=" + (arr.GetType() == typeof(string[])));
        Console.WriteLine("arr not obj[]=" + (arr.GetType() != typeof(object[])));
        object[] oarr = objects.ToArray();
        Console.WriteLine("oarr type=" + (oarr.GetType() == typeof(object[])));
    }
}

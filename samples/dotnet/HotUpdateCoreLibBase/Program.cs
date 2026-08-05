using System;
using System.Collections;
using System.Collections.Generic;
using Dn2Cpp.Runtime;

namespace HotUpdateCoreLibBase;

// A hot-update base with a REAL-CoreLib closure — the only one in the suite;
// every other hot-update base is built against the intrinsic BCL and never reads
// CoreLib. A --hotupdate-base image over transpiled-CoreLib member tables emits a
// signature-only invoker thunk for EVERY interface method row it keeps, including
// rows whose signature names a struct type nothing in the image ever laid out.
// Two such shapes are staged deliberately:
//
//  - System.Enum's real bodies (CompareTo/Equals through Enum-typed receivers;
//    ToString alone routes through an intrinsic) pull in System.Enum's
//    interface set, whose ISpanFormattable.TryFormat row names Span<char> /
//    ReadOnlySpan<char> — intrinsic-represented value types with no t_ struct.
//  - A Dictionary<string, bool> used WITHOUT enumeration to KeyValuePair or
//    DictionaryEntry values keeps ICollection<KeyValuePair<string,bool>> rows
//    naming the never-instantiated KeyValuePair struct, and the non-generic
//    IDictionaryEnumerator cursor (Key/Value only) keeps a get_Entry row whose
//    DictionaryEntry return is likewise never laid out.
//
// Each such row must get a trapping invmiss_ stub rather than an invoker thunk
// naming the undeclared t_ struct, or the base fails at the C++ compile.
internal enum Phase
{
    Boot = 1,
    Ready = 2,
    Done = 4,
}

// The AOT surface the patch imports bind against: static methods over a
// CoreLib Dictionary, plus the non-generic cursor whose interface rows carry
// the trapped get_Entry. Main exercises every member the patch calls, so the
// bodies — and the IDictionaryEnumerator impls the patch's interface-typed
// callvirts dispatch onto (MoveNext/get_Key/get_Value, NOT get_Entry) — are
// reachable and emitted.
public static class Registry
{
    private static readonly Dictionary<string, bool> s_flags = new Dictionary<string, bool>();

    public static void Set(string key, bool value)
    {
        s_flags[key] = value;
    }

    public static bool Get(string key)
    {
        bool value;
        return s_flags.TryGetValue(key, out value) && value;
    }

    public static int Count()
    {
        return s_flags.Count;
    }

    // The non-generic dictionary view's cursor. Its interface,
    // System.Collections.IDictionaryEnumerator, is the carrier of the trapped
    // row: get_Entry returns DictionaryEntry by value, and this base never
    // touches a DictionaryEntry, so the struct has no emitted layout and the
    // row's invoker is the trapping stub.
    public static IDictionaryEnumerator Cursor()
    {
        return ((IDictionary)s_flags).GetEnumerator();
    }
}

internal static class Program
{
    private static void Main(string[] args)
    {
        Console.WriteLine("base: start");
        // Reach System.Enum's REAL bodies: CompareTo/Equals through Enum-typed
        // receivers are transpiled CoreLib IL (ToString alone would route
        // through an intrinsic and pull nothing in).
        Enum ready = Phase.Ready;
        Enum done = Phase.Done;
        Console.WriteLine(ready.CompareTo(done));
        Console.WriteLine(ready.Equals(ready));
        // The dictionary surface the patch drives, exercised here so every
        // member the patch imports is reachable. The cursor loop dispatches
        // MoveNext/get_Key/get_Value through IDictionaryEnumerator — but never
        // get_Entry, and never enumerates generically — so DictionaryEntry and
        // KeyValuePair<string,bool> stay layout-less.
        Registry.Set("alpha", true);
        Registry.Set("beta", false);
        Console.WriteLine(Registry.Count());
        IDictionaryEnumerator cur = Registry.Cursor();
        while (cur.MoveNext())
        {
            Console.WriteLine(cur.Key + "=" + cur.Value);
        }
        // The catch is the negative arm's assert: a patch whose import set
        // names a type this image cannot express (HotUpdateCoreLibBadPatch's
        // get_Entry callvirt) is refused by the loader with a catchable
        // NotSupportedException, printed here — never a silent bind.
        try
        {
            HotUpdate.Run(args[0]);
        }
        catch (NotSupportedException e)
        {
            Console.WriteLine("base caught: " + e.Message);
        }
        Console.WriteLine("base: done");
    }
}

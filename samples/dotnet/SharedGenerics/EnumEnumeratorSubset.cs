#nullable enable
// Enum values pulled out of shared enumerators keep their identity: the
// Dictionary/Keys/Values enumerator bodies have no instantiation-dependent site,
// so they share, and a key leaving them must still box with the REAL enum's
// type-info. Real System.Private.CoreLib (-r), run vs .NET.
using System;
using System.Collections.Generic;
namespace EnumEnumeratorSubset;

enum Note { Do = 1, Re = 2, Mi = 3 }
enum Beat { Down = 10, Up = 20 }

class Program
{
    internal static void __GateEntry()
    {
        var m = new Dictionary<Note, int> { { Note.Do, 10 }, { Note.Re, 20 }, { Note.Mi, 30 } };
        foreach (KeyValuePair<Note, int> kv in m)
            Console.WriteLine("pair " + kv.Key.ToString() + " n=" + (int)kv.Key + " v=" + kv.Value);
        foreach (var k in m.Keys)
            Console.WriteLine("key " + k + " boxed=" + ((object)k is Note));
        foreach (var v in m.Values)
            Console.WriteLine("val " + v);
        var beats = new Dictionary<Beat, string> { { Beat.Down, "d" }, { Beat.Up, "u" } };
        foreach (var kv in beats)
            Console.WriteLine($"beat {kv.Key}={kv.Value}");
        var e = m.GetEnumerator();
        Console.WriteLine("manual move=" + e.MoveNext() + " cur=" + e.Current.Key);
    }
}

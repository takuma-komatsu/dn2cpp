// positional `record` classes — `with` expressions, value equality, and the
// auto-generated ToString (`Point { X = 1, Y = 2 }`). The compiler-synthesized
// PrintMembers guards recursion with RuntimeHelpers.EnsureSufficientExecutionStack,
// which has no managed body we can transpile; it is mapped to a no-op (we rely on
// the native stack). Everything else rides existing infra: records are reference
// types, so their value-equality and ToString override transpile like any class.
// (Records as HashSet/Dictionary keys, and `obj.GetHashCode` via the Object
// virtual, need reference-type object-virtual dispatch — a separate follow-up.)
using System;

namespace RecordSubset;

record Point(int X, int Y);

record Player(string Name, int Hp)
{
    public bool Alive => Hp > 0;
}

internal static class Program
{
    internal static void __GateEntry()
    {
        var a = new Point(1, 2);
        var b = a with { Y = 9 };
        Console.WriteLine(a.X + "," + a.Y + " -> " + b.X + "," + b.Y);

        // value equality + inequality (synthesized op_Equality -> typed Equals)
        Console.WriteLine("eq=" + (a == new Point(1, 2)) + " neq=" + (a == b));
        Console.WriteLine("eqObj=" + a.Equals(new Point(1, 2)));

        // auto ToString (explicit + boxed via Console.WriteLine)
        Console.WriteLine(a.ToString());
        Console.WriteLine(a);

        var p = new Player("hero", 100);
        var hurt = p with { Hp = 0 };
        Console.WriteLine(p);
        Console.WriteLine(hurt.Alive + " " + p.Alive);
    }
}

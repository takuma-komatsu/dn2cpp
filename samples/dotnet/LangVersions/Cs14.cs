// C# 14: the `field` keyword, extension members (`extension` blocks: instance
// methods, instance properties, static members, generic receivers), null-conditional
// assignment, `nameof` of an unbound generic, modifiers on implicitly typed lambda
// parameters, partial constructors and partial events, user-defined compound
// assignment operators, and the widened implicit span conversions.
using System;
using System.Collections.Generic;
using System.Numerics;

namespace Cs14;

// C# 14 `field` keyword: a property body may name its compiler-synthesized backing
// field, so a validating setter no longer needs a hand-written field.
internal sealed class Gauge
{
    public int Level
    {
        get => field;
        set => field = value < 0 ? 0 : (value > 100 ? 100 : value);
    }

    // The getter alone may use it too: initialization still goes through the field.
    public string Label
    {
        get => field ?? "(unset)";
        set;
    }
}

// C# 14 extension members: one static class, several `extension` blocks, each
// naming its receiver once.
internal static class MyExtensions
{
    extension(int x)
    {
        public int Doubled => x * 2;

        public int Plus(int y) => x + y;

        public bool IsEven => (x % 2) == 0;
    }

    extension<T>(IEnumerable<T> src)
    {
        public bool IsEmptyExt
        {
            get
            {
                using (IEnumerator<T> e = src.GetEnumerator())
                {
                    return !e.MoveNext();
                }
            }
        }

        public int CountExt
        {
            get
            {
                int n = 0;
                foreach (T unused in src)
                {
                    n++;
                }

                return n;
            }
        }
    }

    // A receiver with no parameter name declares *static* extension members on the
    // receiver type.
    extension(int)
    {
        public static int Answer => 42;

        public static int FromBool(bool b) => b ? 1 : 0;
    }
}

// C# 14 user-defined compound assignment operator: an instance member that runs
// for `+=` and mutates in place instead of producing a new value.
internal sealed class Accumulator
{
    public int Total;

    public int Applications;

    public void operator +=(int v)
    {
        Total += v;
        Applications++;
    }

    public void operator -=(int v)
    {
        Total -= v;
        Applications++;
    }
}

// C# 14 partial constructor and partial event.
internal partial class Wired
{
    public partial Wired(int seed);

    public partial event Action<int> Pulsed;

    public int Seed;
}

internal partial class Wired
{
    private Action<int> _pulsed;

    public partial Wired(int seed)
    {
        Seed = seed;
    }

    public partial event Action<int> Pulsed
    {
        add => _pulsed += value;
        remove => _pulsed -= value;
    }

    public void Pulse() => _pulsed?.Invoke(Seed);
}

internal sealed class Target
{
    public int Prop { get; set; }

    public int Field;
}

// A delegate with a `ref` parameter, so the lambda below can leave the type off.
internal delegate void RefAction(ref int value);

internal static class Program
{
    internal static void __GateEntry()
    {
        Console.WriteLine("== C# 14.0 ==");

        FieldKeyword();
        ExtensionMembers();
        NullConditionalAssignment();
        NameofUnboundGeneric();
        LambdaParameterModifiers();
        PartialConstructorsAndEvents();
        CompoundAssignmentOperators();
        ImplicitSpanConversions();
    }

    private static void FieldKeyword()
    {
        Gauge g = new Gauge();
        Console.WriteLine("field keyword (initial): " + g.Level);

        g.Level = -20;
        Console.WriteLine("field keyword (clamped low): " + g.Level);

        g.Level = 250;
        Console.WriteLine("field keyword (clamped high): " + g.Level);

        g.Level = 55;
        Console.WriteLine("field keyword (in range): " + g.Level);

        Console.WriteLine("field keyword (getter default): " + g.Label);
        g.Label = "pressure";
        Console.WriteLine("field keyword (getter set): " + g.Label);
    }

    private static void ExtensionMembers()
    {
        Console.WriteLine("extension property: " + 21.Doubled);
        Console.WriteLine("extension method: " + 3.Plus(4));
        Console.WriteLine("extension property (bool): " + 8.IsEven + " " + 7.IsEven);

        List<int> empty = new List<int>();
        List<int> three = new List<int> { 1, 2, 3 };
        Console.WriteLine("generic extension property: " + empty.IsEmptyExt + " " + three.IsEmptyExt);
        Console.WriteLine("generic extension count: " + three.CountExt);

        Console.WriteLine("static extension property: " + int.Answer);
        Console.WriteLine("static extension method: " + int.FromBool(true) + " " + int.FromBool(false));
    }

    private static void NullConditionalAssignment()
    {
        Target present = new Target();
        present?.Prop = 3;
        present?.Field = 4;
        Console.WriteLine($"null-conditional assignment (non-null): Prop={present.Prop} Field={present.Field}");

        Target absent = null;
        absent?.Prop = SideEffect();
        absent?.Field = SideEffect();
        Console.WriteLine("null-conditional assignment (null): no throw, side effects = " + _sideEffects);

        // The compound form short-circuits as well.
        absent?.Prop += 10;
        Console.WriteLine("null-conditional compound assignment (null): no throw");
    }

    private static int _sideEffects;

    // The right-hand side must not be evaluated when the receiver is null.
    private static int SideEffect()
    {
        _sideEffects++;
        return 1;
    }

    private static void NameofUnboundGeneric()
    {
        Console.WriteLine("nameof(List<>): " + nameof(List<>));
        Console.WriteLine("nameof(Dictionary<,>): " + nameof(Dictionary<,>));
        Console.WriteLine("nameof(Gauge): " + nameof(Gauge));
    }

    private static void LambdaParameterModifiers()
    {
        // C# 14: the modifier may appear without the parameter type, because the
        // delegate type supplies it.
        RefAction bump = (ref v) => v += 5;

        int cell = 10;
        bump(ref cell);
        Console.WriteLine("lambda ref parameter (no type): " + cell);

        // out / in behave the same way.
        TryParseish parse = (s, out result) => { result = s.Length; return s.Length > 0; };
        bool ok = parse("hello", out int len);
        Console.WriteLine($"lambda out parameter (no type): ok={ok} len={len}");
    }

    private delegate bool TryParseish(string s, out int result);

    private static void PartialConstructorsAndEvents()
    {
        Wired w = new Wired(7);
        w.Pulsed += v => Console.WriteLine("partial event fired with " + v);
        w.Pulse();
        Console.WriteLine("partial constructor: Seed=" + w.Seed);
    }

    private static void CompoundAssignmentOperators()
    {
        Accumulator acc = new Accumulator();
        acc += 10;
        acc += 5;
        acc -= 3;
        Console.WriteLine($"user-defined operator +=/-=: Total={acc.Total} Applications={acc.Applications}");
    }

    private static void ImplicitSpanConversions()
    {
        // C# 14 lets the array -> ReadOnlySpan<T> conversion take part in generic
        // type inference, so T is inferred from the array element type.
        int[] ints = [1, 2, 3, 4];
        Console.WriteLine("implicit span conversion (inferred T): " + Total(ints));

        long[] longs = [10L, 20L];
        Console.WriteLine("implicit span conversion (inferred T, long): " + Total(longs));

        // ... and it applies to a Span<T> source as well.
        Span<int> span = [5, 6, 7];
        Console.WriteLine("implicit Span -> ReadOnlySpan (inferred T): " + Total(span));
    }

    private static T Total<T>(ReadOnlySpan<T> values) where T : INumber<T>
    {
        T acc = T.Zero;
        foreach (T v in values)
        {
            acc += v;
        }

        return acc;
    }
}

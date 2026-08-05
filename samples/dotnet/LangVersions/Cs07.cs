// C# 7.0 through 7.3 — the release where the language grew a value-oriented,
// by-reference half, and so the one whose IL a transpiler cannot paper over.
//
//   7.0: `out var`, tuples (positional / named / returned) and deconstruction
//        (both the tuple form and a user-defined `Deconstruct`), type patterns
//        (`is T x`, `case T x when ...`), local functions, `ref` locals and `ref`
//        returns, discards, binary literals and digit separators, `throw`
//        expressions, and an `async` method returning `ValueTask<T>`.
//   7.1: the `default` literal, inferred tuple element names.
//   7.2: `in` parameters, `readonly struct`, a hand-written `ref struct`,
//        `private protected`, the conditional `ref` expression, and numeric
//        literals with a leading underscore after the base prefix.
//   7.3: reassigning a `ref` local, the `unmanaged` / `Enum` / `Delegate` generic
//        constraints, and tuple `==` / `!=`.
using System;
using System.Threading.Tasks;

namespace Cs07;

internal static class Program
{
    internal static void __GateEntry()
    {
        Console.WriteLine("== C# 7.x ==");

        // ---------------- 7.0 ----------------
        Console.WriteLine("  -- 7.0 --");

        // `out var`.
        if (int.TryParse("42", out var parsed))
        {
            Console.WriteLine($"  out var: parsed={parsed}");
        }

        // Tuples: unnamed, named, returned from a method, deconstructed.
        (int, string) pair = (1, "one");
        (int id, string name) named = (7, "seven");
        Console.WriteLine($"  tuple: pair={pair} pair.Item2={pair.Item2} named.id={named.id} named.name={named.name}");

        var (lo, hi) = MinMax(new[] { 5, 3, 9, 1 });
        Console.WriteLine($"  tuple return + deconstruction: lo={lo} hi={hi}");

        // A user-defined Deconstruct.
        var pt = new Pt(3, 4);
        var (px, py) = pt;
        Console.WriteLine($"  Deconstruct: px={px} py={py}");

        // Type patterns: `is T x`, and `case T x when ...` in a switch statement.
        object boxed = 123;
        if (boxed is int unboxed)
        {
            Console.WriteLine($"  is-pattern: unboxed={unboxed}");
        }

        Console.WriteLine($"  switch pattern: {Describe(42)} / {Describe(7)} / {Describe("hello!!")} / {Describe("hi")}");
        Console.WriteLine($"  switch pattern: {Describe(null)} / {Describe(1.5m)}");

        // Local function (recursive, so it cannot collapse to a lambda-shaped call).
        int Fib(int n) => n < 2 ? n : Fib(n - 1) + Fib(n - 2);
        Console.WriteLine($"  local function: Fib(10)={Fib(10)}");

        // `ref` local bound to a `ref` return: writing through it mutates the array.
        int[] cells = { 1, 2, 3 };
        ref int firstCell = ref First(cells);
        firstCell = 100;
        string cellsText = string.Join(",", cells);
        Console.WriteLine($"  ref return + ref local: cells=[{cellsText}]");

        // Discards.
        Triple(out _, out int second, out _);
        Console.WriteLine($"  discard: second={second}");

        // Binary literals and digit separators.
        Console.WriteLine($"  literals: 0b1010_1010={0b1010_1010} 1_000_000={1_000_000} 0xFF_FF={0xFF_FF}");

        // `throw` expression (in the right operand of `??`).
        Console.WriteLine($"  throw expression: {SafeLen("abc")} / {SafeLen(null)}");

        // `async ValueTask<T>` — a struct-returning async method, not Task<T>.
        Console.WriteLine($"  async ValueTask: SumAsync(20,22)={SumAsync(20, 22).GetAwaiter().GetResult()}");

        // ---------------- 7.1 ----------------
        Console.WriteLine("  -- 7.1 --");

        int defInt = default;
        string defStr = default;
        Big defBig = default;                               // a struct: `default` zeroes it, it is not null
        Console.WriteLine($"  default literal: int={defInt} string-is-null={defStr is null} Big.Sum={defBig.Sum}");

        int count = 3;
        string label = "items";
        var inferred = (count, label);                      // names inferred from the variables
        Console.WriteLine($"  inferred tuple names: count={inferred.count} label={inferred.label}");

        // ---------------- 7.2 ----------------
        Console.WriteLine("  -- 7.2 --");

        var big = new Big(20, 22);
        Console.WriteLine($"  in param + readonly struct: SumIn(in big)={SumIn(in big)} big.Sum={big.Sum}");

        // A hand-written `ref struct` — stack-only, so it can never be boxed or a field
        // of a class. Deliberately not Span<T>: the point is the user-declared kind.
        var tally = new Tally();
        tally.Add(10);
        tally.Add(32);
        Console.WriteLine($"  ref struct: tally.Sum={tally.Sum}");

        // `private protected` — visible to a derived class in this assembly only.
        Console.WriteLine($"  private protected: {new Derived72().Show()}");

        // Conditional `ref` expression: `chosen` aliases b, so only b changes.
        int a = 1;
        int b = 2;
        bool takeA = false;
        ref int chosen = ref (takeA ? ref a : ref b);
        chosen = 99;
        Console.WriteLine($"  conditional ref: a={a} b={b}");

        Console.WriteLine($"  leading-underscore literals: 0x_1F={0x_1F} 0b_1111={0b_1111}");

        // ---------------- 7.3 ----------------
        Console.WriteLine("  -- 7.3 --");

        // Reassigning a `ref` local — it points at arr[0], then at arr[2].
        int[] arr = { 1, 2, 3 };
        ref int slot = ref arr[0];
        slot = 10;
        slot = ref arr[2];
        slot = 30;
        string arrText = string.Join(",", arr);
        Console.WriteLine($"  ref local reassignment: arr=[{arrText}]");

        // `where T : unmanaged` — sizes of blittable types only, so the numbers do not
        // depend on the pointer width of the host.
        Console.WriteLine($"  where T : unmanaged: sizeof(int)={SizeOfT<int>()} sizeof(Big)={SizeOfT<Big>()} sizeof(Color)={SizeOfT<Color>()}");

        // `where T : Enum`.
        Console.WriteLine($"  where T : Enum: {NameOfEnum(Color.Green)} / {NameOfEnum(Color.Blue)}");

        // `where T : Delegate`.
        Func<int, int> identity = x => x;
        Action nothing = () => { };
        Console.WriteLine($"  where T : Delegate: {DelegateTypeName(identity)} / {DelegateTypeName(nothing)}");

        // Tuple `==` / `!=` — element-wise, and it lifts through the element operators.
        var t1 = (1, "a");
        var t2 = (1, "a");
        var t3 = (2, "a");
        Console.WriteLine($"  tuple equality: t1==t2 -> {t1 == t2}, t1!=t3 -> {t1 != t3}, t1==t3 -> {t1 == t3}");
    }

    // ---- 7.0 helpers ----

    private static (int lo, int hi) MinMax(int[] values)
    {
        int lo = values[0];
        int hi = values[0];
        foreach (int v in values)
        {
            if (v < lo)
            {
                lo = v;
            }

            if (v > hi)
            {
                hi = v;
            }
        }

        return (lo, hi);
    }

    private static string Describe(object o)
    {
        switch (o)
        {
            case int i when i > 10:
                return $"big-int({i})";
            case int i:
                return $"int({i})";
            case string s when s.Length > 5:
                return $"long-string({s})";
            case string s:
                return $"string({s})";
            case null:
                return "null";
            default:
                return "other";
        }
    }

    private static ref int First(int[] values) => ref values[0];

    private static void Triple(out int a, out int b, out int c)
    {
        a = 1;
        b = 2;
        c = 3;
    }

    private static int Len(string s) => (s ?? throw new ArgumentNullException(nameof(s))).Length;

    private static string SafeLen(string s)
    {
        try
        {
            return $"len({Len(s)})";
        }
        catch (ArgumentNullException e)
        {
            return $"threw(paramName={e.ParamName})";
        }
    }

    private static async ValueTask<int> SumAsync(int a, int b)
    {
        await Task.Yield();
        return a + b;
    }

    // ---- 7.2 / 7.3 helpers ----

    private static int SumIn(in Big value) => value.A + value.B;

    private static unsafe int SizeOfT<T>() where T : unmanaged => sizeof(T);

    private static string NameOfEnum<T>(T value) where T : Enum => value.ToString();

    private static string DelegateTypeName<T>(T d) where T : Delegate => typeof(T).Name;
}

internal enum Color
{
    Red,
    Green,
    Blue,
}

internal sealed class Pt
{
    internal Pt(int x, int y)
    {
        X = x;
        Y = y;
    }

    internal int X { get; }
    internal int Y { get; }

    internal void Deconstruct(out int x, out int y)
    {
        x = X;
        y = Y;
    }
}

internal readonly struct Big
{
    internal Big(int a, int b)
    {
        A = a;
        B = b;
    }

    internal int A { get; }
    internal int B { get; }
    internal int Sum => A + B;
}

internal ref struct Tally
{
    private int _sum;

    internal void Add(int value) => _sum += value;

    internal int Sum => _sum;
}

internal class Base72
{
    private protected int Secret = 42;

    private protected int Reveal() => Secret;
}

internal sealed class Derived72 : Base72
{
    internal string Show() => $"Secret={Secret} Reveal()={Reveal()}";
}

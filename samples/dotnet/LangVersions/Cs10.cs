// C# 10.0. Asserts: `record struct` and `readonly record struct`, a parameterless
// struct constructor together with struct field initializers, a hand-written
// interpolated string handler (`[InterpolatedStringHandler]`), extended property
// patterns (`o is { A.B: 1 }`), lambda natural types (`var f = (int x) => x + 1;`)
// and explicit lambda return types (`var g = int (int x) => x;`), attributes on
// lambdas, `CallerArgumentExpression`, and const interpolated strings.
//
// This file is itself a file-scoped namespace, which is the feature — every section
// in this bucket is, so the assertion is structural rather than printed.
//
// GLOBAL USINGS are deliberately NOT covered here. A `global using` is scoped to the
// whole compilation, so exercising it in this file would silently change what every
// other section in the bucket can see, and the first section to drop a `using` line
// would then be asserting C# 10 by accident. It belongs in a bucket of its own.
//
// The interpolated string handler is the one that earns its keep: `Log($"...")` does
// not build a string at all. The compiler sees that `Log`'s parameter carries
// `[InterpolatedStringHandler]`, constructs the handler with (literalLength,
// formattedCount), and rewrites the interpolation into a sequence of AppendLiteral /
// AppendFormatted<T> calls — so the braces in the source become method calls in the
// IL, and the handler decides what the text is. Here it wraps every hole in angle
// brackets, which is visible in the output precisely because no string was formatted.
using System;
using System.Runtime.CompilerServices;
using System.Text;

namespace Cs10;

internal static class Program
{
    // Const interpolated string: every hole is itself a constant, so the whole thing
    // folds at compile time and lands in the metadata as a literal.
    private const string Who = "C# 10";
    private const string Banner = $"const-interpolated: hello from {Who}";

    internal static void __GateEntry()
    {
        Console.WriteLine("== C# 10.0 ==");

        // `record struct` — value type, value equality, synthesized ToString, `with`.
        var p1 = new Pixel(3, 4);
        var p2 = new Pixel(3, 4);
        var moved = p1 with { X = 9 };
        Console.WriteLine($"  record struct: {p1} moved={moved}");
        Console.WriteLine($"  record struct equality: p1==p2 -> {p1 == p2}, p1==moved -> {p1 == moved}");
        var (px, py) = p1;
        Console.WriteLine($"  record struct Deconstruct: px={px} py={py}");

        // `readonly record struct`.
        var badge = new Badge("ada", 1);
        var promoted = badge with { Rank = 2 };
        Console.WriteLine($"  readonly record struct: {badge} promoted={promoted} equal -> {badge == promoted}");

        // Parameterless struct constructor + struct field initializers. `new Settings()`
        // runs the constructor; `default` does not — that difference is the assertion.
        var made = new Settings();
        Settings zeroed = default;
        Console.WriteLine($"  parameterless struct ctor: new={made} default={zeroed}");

        // Interpolated string handler: the holes become AppendFormatted<T> calls.
        // Every argument here has at least one non-constant hole on purpose — an
        // interpolated string whose holes are all constants IS a constant string, and a
        // constant string does not convert to a handler.
        int x = 5;
        int y = 7;
        Log($"x={x} y={y} sum={x + y}");
        Log($"name={badge.Name} rank={badge.Rank}");

        // Extended property pattern: `A.B` instead of `A: { B: ... }`.
        var outer = new Outer { A = new Inner { B = 1 } };
        var otherOuter = new Outer { A = new Inner { B = 2 } };
        Console.WriteLine($"  extended property pattern: {outer is { A.B: 1 }} / {otherOuter is { A.B: 1 }}");
        Console.WriteLine($"  extended property pattern (nested null): {new Outer() is { A.B: 1 }}");

        // Lambda natural type: `inc` is a Func<int,int> with nothing on the left saying so.
        var inc = (int n) => n + 1;
        // Explicit lambda return type.
        var sq = int (int n) => n * n;
        // An attribute on a lambda.
        var tagged = [Marker] (int n) => n * 10;
        Console.WriteLine($"  lambda natural type: inc(41)={inc(41)} type={inc.GetType().Name}");
        Console.WriteLine($"  explicit return type: sq(9)={sq(9)}");
        Console.WriteLine($"  attributed lambda: tagged(4)={tagged(4)}");

        // CallerArgumentExpression: the compiler passes the argument's SOURCE TEXT.
        int v = 5;
        Check(v > 3);
        Check(v % 2 == 0);

        // Const interpolated string.
        Console.WriteLine($"  {Banner}");
    }

    // The parameter type carries [InterpolatedStringHandler], so `Log($"...")` never
    // formats a string: it builds a LogHandler and appends into it.
    private static void Log(LogHandler handler) => Console.WriteLine($"  handler: {handler.Text}");

    private static void Check(bool cond, [CallerArgumentExpression(nameof(cond))] string expr = null)
        => Console.WriteLine($"  CallerArgumentExpression: check({expr}) = {cond}");
}

[AttributeUsage(AttributeTargets.Method)]
internal sealed class MarkerAttribute : Attribute
{
}

[InterpolatedStringHandler]
internal ref struct LogHandler
{
    private readonly StringBuilder _sb;

    // The shape the compiler looks for: (int literalLength, int formattedCount).
    internal LogHandler(int literalLength, int formattedCount)
        => _sb = new StringBuilder(literalLength + (formattedCount * 4));

    internal void AppendLiteral(string s) => _sb.Append(s);

    internal void AppendFormatted<T>(T value) => _sb.Append('<').Append(value?.ToString()).Append('>');

    internal readonly string Text => _sb.ToString();
}

internal record struct Pixel(int X, int Y);

internal readonly record struct Badge(string Name, int Rank);

internal struct Settings
{
    internal int Retries = 3;           // struct field initializer (C# 10)
    internal string Mode;

    public Settings()                   // parameterless struct constructor (C# 10; must be public)
        => Mode = "auto";

    public override readonly string ToString() => $"Settings(Retries={Retries}, Mode={Mode})";
}

internal sealed class Inner
{
    public int B { get; init; }
}

internal sealed class Outer
{
    public Inner A { get; init; }
}

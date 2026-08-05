// C# 8.0. Asserts: switch expressions, property / tuple / positional patterns,
// `using` declarations, static local functions, default interface members,
// indices and ranges (`^1`, `a[1..3]`), `??=`, `readonly` members on a struct,
// and nullable reference types (this file — and only this file — turns the NRT
// context on, so the annotations are real rather than erased by the project's
// `Nullable=disable`; the annotations themselves are metadata, but the null
// checks the compiler leaves behind are not).
//
// The centerpiece is ASYNC STREAMS: `async IAsyncEnumerable<T>` + `await foreach`
// + `IAsyncDisposable` + `await using`, in both its statement and its declaration
// form. That is the one construct in this bucket whose lowering the transpiler has
// never been asked for anywhere else in the repository — a state machine that is
// simultaneously an iterator and an awaiter, driven through
// `IAsyncEnumerator<T>.MoveNextAsync()` returning `ValueTask<bool>`. It is driven
// synchronously from `__GateEntry` via `GetAwaiter().GetResult()`, so the printed
// order is the program's order and not the thread pool's.
//
// The same section also drives the CONFIGURED / CANCELABLE wrappers around an
// `await foreach`: `.WithCancellation(...)` and `.ConfigureAwait(false)`, alone and
// chained, over the real `ConfiguredCancelableAsyncEnumerable<T>` IL (no intrinsic
// mapping — the wrapper struct and its `[EnumeratorCancellation]`-fed token are
// transpiled from the BCL as-is). It asserts the token actually reaches the iterator
// through the wrapper (a mid-stream `CancellationTokenSource.Cancel()` surfaces as
// `OperationCanceledException`), that an early `break` runs the iterator's `finally`
// through the wrapper's `DisposeAsync`, and that a reference-type element type keeps
// its shared-generics canonicalization intact.
#nullable enable

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Cs08;

internal static class Program
{
    internal static void __GateEntry()
    {
        Console.WriteLine("== C# 8.0 ==");

        // Switch expression, with property, positional and `when` patterns inside it.
        Console.WriteLine($"  switch expression: {Shape(new Circle(0))} / {Shape(new Circle(5))}");
        Console.WriteLine($"  switch expression: {Shape(new Rect(0, 9))} / {Shape(new Rect(4, 4))} / {Shape(new Rect(2, 3))}");
        Console.WriteLine($"  switch expression: {Shape(null)} / {Shape(42)}");

        // Tuple pattern.
        Console.WriteLine($"  tuple pattern: {Rps("rock", "scissors")} / {Rps("paper", "paper")} / {Rps("rock", "paper")}");

        // Property pattern in an `is`, and a positional pattern in an `is`.
        var r = new Rect(3, 3);
        Console.WriteLine($"  property pattern: {r is { W: 3, H: 3 }} / positional pattern: {r is Rect(3, var h) && h == 3}");

        // `using` declaration — disposal at the end of the enclosing scope, not a block.
        UsingDeclaration();

        // Static local function: cannot capture, so it is a plain static method in IL.
        static int Fact(int n) => n <= 1 ? 1 : n * Fact(n - 1);
        Console.WriteLine($"  static local function: Fact(6)={Fact(6)}");

        // Default interface member: `En` inherits `Greet`, `Jp` overrides it. The call
        // goes through the interface, so the DIM slot is what dispatch has to find.
        IGreeter en = new En();
        IGreeter jp = new Jp();
        Console.WriteLine($"  default interface member: {en.Greet()} / {jp.Greet()}");

        // Indices and ranges, on an array and on a string.
        int[] a = { 10, 20, 30, 40, 50 };
        string tail = string.Join(",", a[1..3]);
        string fromEnd = string.Join(",", a[^2..]);
        Index penultimate = ^2;
        Range middle = 1..^1;
        string middleText = string.Join(",", a[middle]);
        Console.WriteLine($"  index/range: a[^1]={a[^1]} a[penultimate]={a[penultimate]} a[1..3]=[{tail}] a[^2..]=[{fromEnd}] a[1..^1]=[{middleText}]");
        Console.WriteLine($"  string range: hello-world[0..5]={"hello world"[0..5]} [^5..]={"hello world"[^5..]}");

        // `??=`.
        string? maybe = null;
        maybe ??= "assigned";
        string? already = "kept";
        already ??= "not-this";
        Console.WriteLine($"  ??=: maybe={maybe} already={already}");

        // `readonly` members on a mutable struct.
        var acc = new Acc { V = 21 };
        Console.WriteLine($"  readonly member: {acc} doubled={acc.Doubled()}");

        // Nullable reference types: the annotations, and the flow analysis behind them.
        Console.WriteLine($"  NRT: {Describe(null)} / {Describe("text")}");

        // Async streams.
        ConsumeAsync().GetAwaiter().GetResult();

        // Async streams through the configured / cancelable await-foreach wrappers.
        ConsumeConfiguredAsync().GetAwaiter().GetResult();
    }

    // ---- patterns ----

    private static string Shape(object? o) => o switch
    {
        Circle { Radius: 0 } => "degenerate-circle",                    // property pattern
        Circle c => $"circle(r={c.Radius})",
        Rect(0, var h) => $"degenerate-rect(h={h})",                    // positional pattern
        Rect { W: var w, H: var h } when w == h => $"square({w})",      // property pattern + when
        Rect rect => $"rect({rect.W}x{rect.H})",
        null => "null",
        _ => "other",
    };

    private static string Rps(string a, string b) => (a, b) switch
    {
        ("rock", "scissors") => "first",
        ("scissors", "paper") => "first",
        ("paper", "rock") => "first",
        (_, _) when a == b => "draw",
        _ => "second",
    };

    private static string Describe(string? s) => s is null ? "was-null" : $"len({s.Length})";

    private static void UsingDeclaration()
    {
        using var outer = new Res("outer");
        using var inner = new Res("inner");
        Console.WriteLine("  using declaration: body");
        // Disposed in reverse declaration order at the end of this method.
    }

    // ---- async streams ----

    private static async IAsyncEnumerable<int> SquaresAsync(int n)
    {
        for (int i = 1; i <= n; i++)
        {
            await Task.Yield();                     // a real suspension point inside an iterator
            yield return i * i;
        }
    }

    private static async Task ConsumeAsync()
    {
        // `await using` statement form, around an `await foreach`.
        await using (var scope = new AsyncRes("block"))
        {
            await foreach (int v in SquaresAsync(4))
            {
                Console.WriteLine($"  await foreach: {v}");
            }
        }

        // `await using` declaration form: disposed at the end of the method, after the body.
        await using var trailing = new AsyncRes("declaration");
        Console.WriteLine("  await using declaration: body");
    }

    // ---- configured / cancelable async streams ----

    // The token flows in through `[EnumeratorCancellation]`: whatever
    // `.WithCancellation(...)` supplies at the call site is combined into `ct` here.
    private static async IAsyncEnumerable<int> SquaresCancelableAsync(
        int n, [EnumeratorCancellation] CancellationToken ct = default)
    {
        for (int i = 1; i <= n; i++)
        {
            await Task.Yield();
            ct.ThrowIfCancellationRequested();
            yield return i * i;
        }
    }

    // A `finally` inside the iterator, so an early `break` in the consumer (which
    // disposes the enumerator mid-flight through the wrapper's DisposeAsync) is visible.
    private static async IAsyncEnumerable<int> CountedAsync(
        int n, [EnumeratorCancellation] CancellationToken ct = default)
    {
        try
        {
            for (int i = 1; i <= n; i++)
            {
                await Task.Yield();
                ct.ThrowIfCancellationRequested();
                yield return i;
            }
        }
        finally
        {
            Console.WriteLine("  configured break: iterator finally");
        }
    }

    // A reference-type element type, to pin shared-generics canonicalization.
    private static async IAsyncEnumerable<string> NamesAsync(
        int n, [EnumeratorCancellation] CancellationToken ct = default)
    {
        for (int i = 1; i <= n; i++)
        {
            await Task.Yield();
            ct.ThrowIfCancellationRequested();
            yield return $"name{i}";
        }
    }

    private static async Task ConsumeConfiguredAsync()
    {
        // `.WithCancellation(...)` alone, with a token that never trips.
        await foreach (int v in SquaresCancelableAsync(3).WithCancellation(CancellationToken.None))
        {
            Console.WriteLine($"  with-cancellation: {v}");
        }

        // `.ConfigureAwait(false)` alone.
        await foreach (int v in SquaresCancelableAsync(3).ConfigureAwait(false))
        {
            Console.WriteLine($"  configure-await: {v}");
        }

        // The chained form: `.WithCancellation(ct).ConfigureAwait(false)`.
        var live = new CancellationTokenSource();
        await foreach (int v in SquaresCancelableAsync(3).WithCancellation(live.Token).ConfigureAwait(false))
        {
            Console.WriteLine($"  chained: {v}");
        }

        // Mid-stream cancel: the token reaches the iterator through the wrapper, so
        // ThrowIfCancellationRequested surfaces as OperationCanceledException.
        var cts = new CancellationTokenSource();
        int seen = 0;
        try
        {
            await foreach (int v in SquaresCancelableAsync(10).WithCancellation(cts.Token))
            {
                Console.WriteLine($"  cancel mid-stream: {v}");
                seen++;
                if (seen == 3)
                {
                    cts.Cancel();
                }
            }
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine($"  cancel mid-stream: canceled after {seen}");
        }

        // Early `break` out of a configured await-foreach: the wrapper's DisposeAsync
        // runs the iterator's `finally`.
        await foreach (int v in CountedAsync(5).ConfigureAwait(false))
        {
            Console.WriteLine($"  configured break: {v}");
            if (v == 2)
            {
                break;
            }
        }

        // Reference-type element type through `.WithCancellation(...)`.
        await foreach (string s in NamesAsync(3).WithCancellation(CancellationToken.None))
        {
            Console.WriteLine($"  ref-type cancelable: {s}");
        }
    }
}

// ---- default interface member ----

internal interface IGreeter
{
    string Name { get; }

    string Greet() => $"hello-{Name}";              // default implementation, in the interface
}

internal sealed class En : IGreeter
{
    public string Name => "en";
}

internal sealed class Jp : IGreeter
{
    public string Name => "jp";

    public string Greet() => $"konnichiwa-{Name}";  // overrides the default
}

// ---- shapes ----

internal sealed class Circle
{
    internal Circle(int radius) => Radius = radius;

    internal int Radius { get; }
}

internal sealed class Rect
{
    internal Rect(int w, int h)
    {
        W = w;
        H = h;
    }

    internal int W { get; }
    internal int H { get; }

    internal void Deconstruct(out int w, out int h)
    {
        w = W;
        h = H;
    }
}

// ---- readonly members ----

internal struct Acc
{
    internal int V;

    internal readonly int Doubled() => V * 2;

    public override readonly string ToString() => $"Acc(V={V})";
}

// ---- disposables ----

internal sealed class Res : IDisposable
{
    private readonly string _name;

    internal Res(string name)
    {
        _name = name;
        Console.WriteLine($"  using declaration: open {_name}");
    }

    public void Dispose() => Console.WriteLine($"  using declaration: dispose {_name}");
}

internal sealed class AsyncRes : IAsyncDisposable
{
    private readonly string _name;

    internal AsyncRes(string name)
    {
        _name = name;
        Console.WriteLine($"  await using: open {_name}");
    }

    public async ValueTask DisposeAsync()
    {
        await Task.Yield();
        Console.WriteLine($"  await using: disposed {_name}");
    }
}

// C# 13: params collections (params over something other than an array),
// System.Threading.Lock, the \e escape, implicit indexer access in object
// initializers, `ref` locals and `unsafe` blocks inside iterators and async
// methods, the `allows ref struct` anti-constraint, ref structs implementing
// interfaces, partial properties, and [OverloadResolutionPriority].
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Cs13;

internal interface IShow
{
    string Show();
}

// C# 13: a ref struct may implement an interface (it still cannot be boxed, so it
// may only satisfy the constraint of a type parameter that `allows ref struct`).
internal ref struct RefBox : IShow, IDisposable
{
    public int Value;

    public RefBox(int value)
    {
        Value = value;
    }

    public string Show() => "RefBox(" + Value + ")";

    public void Dispose()
    {
        Console.WriteLine("ref struct Dispose ran");
    }
}

internal sealed class HeapBox : IShow
{
    public HeapBox(int value)
    {
        Value = value;
    }

    public int Value { get; }

    public string Show() => "HeapBox(" + Value + ")";
}

// C# 13 partial property: declaration here, implementation below.
internal partial class Registers
{
    public partial int Width { get; }

    public partial string Name { get; set; }
}

internal partial class Registers
{
    private string _name = "r0";

    public partial int Width => 64;

    public partial string Name
    {
        get => _name;
        set => _name = value.ToLowerInvariant();
    }
}

internal sealed class Board
{
    public int[] Cells { get; } = new int[4];

    public override string ToString() => string.Join(",", Cells);
}

// The two classes differ only in the attribute, so the pair of calls below shows
// the priority actually changing the winner rather than merely reporting one.
internal static class PlainOverloads
{
    public static string Pick(object o) => "object overload";

    public static string Pick(string s) => "string overload";
}

internal static class PrioritizedOverloads
{
    // `string` is the more specific overload and would normally win for a string
    // argument. Raising the priority of the `object` one flips that.
    [OverloadResolutionPriority(1)]
    public static string Pick(object o) => "object overload";

    public static string Pick(string s) => "string overload";
}

internal static class Program
{
    // C# 13 dedicated lock type: `lock (Lock)` lowers to Lock.EnterScope(), not
    // Monitor.Enter.
    private static readonly Lock TheLock = new Lock();

    internal static void __GateEntry()
    {
        Console.WriteLine("== C# 13.0 ==");

        ParamsCollections();
        LockObject();
        EscapeSequence();
        ImplicitIndexerInitializer();
        RefAndUnsafeInIterators();
        RefAndUnsafeInAsync();
        AllowsRefStruct();
        PartialProperties();
        OverloadPriority();
    }

    // C# 13: `params` over a ReadOnlySpan<T>, a List<T>, and an IEnumerable<T>.
    private static int SumSpan(params ReadOnlySpan<int> xs)
    {
        int total = 0;
        foreach (int x in xs)
        {
            total += x;
        }

        return total;
    }

    private static string JoinList(params List<string> items)
    {
        return string.Join("+", items);
    }

    private static int CountEnumerable(params IEnumerable<int> xs)
    {
        int n = 0;
        foreach (int _ in xs)
        {
            n++;
        }

        return n;
    }

    private static void ParamsCollections()
    {
        Console.WriteLine("params ReadOnlySpan<int>: " + SumSpan(1, 2, 3, 4));
        Console.WriteLine("params ReadOnlySpan<int> (none): " + SumSpan());
        Console.WriteLine("params List<string>: " + JoinList("a", "b", "c"));
        Console.WriteLine("params IEnumerable<int>: " + CountEnumerable(5, 6, 7));
    }

    private static void LockObject()
    {
        int guarded = 0;

        lock (TheLock)
        {
            guarded += 5;
        }

        using (TheLock.EnterScope())
        {
            guarded += 2;
        }

        Console.WriteLine("System.Threading.Lock: " + guarded);
    }

    private static void EscapeSequence()
    {
        // Never write the control character itself to stdout: print its code point.
        char esc = '\e';
        Console.WriteLine("\\e escape as int: " + (int)esc);

        string withEsc = "a\eb";
        int[] codes = new int[withEsc.Length];
        for (int i = 0; i < withEsc.Length; i++)
        {
            codes[i] = withEsc[i];
        }

        Console.WriteLine("\\e in a string, code points: " + string.Join(",", codes));
    }

    private static void ImplicitIndexerInitializer()
    {
        // C# 13 allows the ^ index-from-end operator inside an object initializer.
        Board board = new Board { Cells = { [0] = 1, [^2] = 5, [^1] = 9 } };
        Console.WriteLine("implicit indexer in initializer: " + board);
    }

    // C# 13: `ref` locals and `unsafe` blocks are legal in an iterator (the ref local
    // still may not live across a `yield return`). Taking `&` of a *local* remains
    // illegal here, because a local may be hoisted into the state machine; `fixed`
    // over the caller's array is the shape that C# 13 actually unlocked.
    private static IEnumerable<int> Doubled(int[] data)
    {
        for (int i = 0; i < data.Length; i++)
        {
            ref int slot = ref data[i];
            int doubled = slot * 2;
            yield return doubled;
        }

        int viaPointer = 0;
        unsafe
        {
            fixed (int* p = data)
            {
                for (int i = 0; i < data.Length; i++)
                {
                    viaPointer += p[i];
                }
            }
        }

        yield return viaPointer;
    }

    private static void RefAndUnsafeInIterators()
    {
        Console.WriteLine("ref local + unsafe in iterator: " + string.Join(",", Doubled(new[] { 1, 2, 3 })));
    }

    // C# 13: the same two relaxations inside an async method. The ref local sits
    // entirely after the await, and again the pointer comes from `fixed`, not from
    // `&` on a local.
    private static async Task<int> BumpAsync(int[] data)
    {
        await Task.Yield();

        ref int slot = ref data[0];
        slot += 100;

        int copied;
        unsafe
        {
            fixed (int* p = data)
            {
                copied = *p;
            }
        }

        return copied;
    }

    private static void RefAndUnsafeInAsync()
    {
        int[] data = new[] { 1 };
        int result = BumpAsync(data).GetAwaiter().GetResult();
        Console.WriteLine($"ref local + unsafe in async: result={result} data[0]={data[0]}");
    }

    // C# 13 anti-constraint: T may be a ref struct, so it must never be boxed.
    // The interface constraint is dispatched without boxing.
    private static string ShowAny<T>(T item) where T : IShow, allows ref struct
    {
        return item.Show();
    }

    private static void AllowsRefStruct()
    {
        using (RefBox rb = new RefBox(3))
        {
            Console.WriteLine("allows ref struct (ref struct arg): " + ShowAny(rb));
        }

        Console.WriteLine("allows ref struct (class arg): " + ShowAny(new HeapBox(4)));
    }

    private static void PartialProperties()
    {
        Registers regs = new Registers();
        regs.Name = "RAX";
        Console.WriteLine($"partial property: Width={regs.Width} Name={regs.Name}");
    }

    private static void OverloadPriority()
    {
        Console.WriteLine("no priority attribute picks: " + PlainOverloads.Pick("a string"));
        Console.WriteLine("OverloadResolutionPriority(1) picks: " + PrioritizedOverloads.Pick("a string"));
    }
}

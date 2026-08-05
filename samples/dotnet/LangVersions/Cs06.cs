// C# 6.0 — the "sugar" release: almost everything here is a compile-time rewrite
// into IL the transpiler already had to handle, so what is asserted is that the
// rewrite survives the round trip. In order: `using static`, expression-bodied
// members (method / property / indexer), auto-property initializers, getter-only
// auto-properties, string interpolation, `nameof`, the null-conditional operators
// `?.` and `?[]` (including `?.Invoke` on a delegate), index initializers, and
// exception filters (`catch ... when`).
//
// The one item that is NOT sugar is `await` inside a `catch` / `finally` block:
// the compiler cannot leave the exception on the CLR's exception stack across a
// suspension point, so it lowers the handler into a state machine that captures
// the exception, resumes on a continuation, and rethrows/completes by hand. That
// is real lowering, and it is the reason this section drags a `Task` in.
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using static System.Math;

namespace Cs06;

internal static class Program
{
    internal static void __GateEntry()
    {
        Console.WriteLine("== C# 6.0 ==");

        // `using static`: Math's members without naming the type. Only whole-valued
        // results are printed — a fractional double would drag culture into the diff.
        Console.WriteLine($"  using static: Max(3,9)={Max(3, 9)} Abs(-7)={Abs(-7)} Sqrt(144)={Sqrt(144.0)}");

        // Auto-property initializer, getter-only auto-property, expression-bodied members.
        var badge = new Badge("ada");
        Console.WriteLine($"  auto-props: Id={badge.Id} Name={badge.Name}");
        Console.WriteLine($"  expression-bodied: Display={badge.Display} Shout={badge.Shout()} this[0]={badge[0]} this[1]={badge[1]}");

        // nameof — of a type, of a member, of a local.
        string ofType = nameof(Badge);
        string ofMember = nameof(Badge.Display);
        string ofLocal = nameof(badge);
        Console.WriteLine($"  nameof: {ofType} / {ofMember} / {ofLocal}");

        // Null-conditional `?.`, chained `?.`, `?.Invoke` on a null delegate, and `?[]`.
        Badge missing = null;
        Action beep = null;
        beep?.Invoke();                                     // no-op, not a NullReferenceException
        string missingName = missing?.Name ?? "<null>";
        int missingLen = missing?.Name?.Length ?? -1;
        Console.WriteLine($"  null-conditional: missing?.Name={missingName} missing?.Name?.Length={missingLen}");

        int[] nums = { 10, 20, 30 };
        int[] noNums = null;
        int? present = nums?[1];
        int absent = noNums?[0] ?? -1;
        Console.WriteLine($"  null-conditional index: nums?[1]={present} noNums?[0]={absent}");

        // Index initializer. Read back by key — never by enumeration order, which a
        // Dictionary does not promise and this gate must not depend on.
        var ages = new Dictionary<string, int> { ["ada"] = 36, ["alan"] = 41, ["grace"] = 85 };
        Console.WriteLine($"  index initializer: ada={ages["ada"]} alan={ages["alan"]} grace={ages["grace"]} count={ages.Count}");

        // Exception filters: the first `when` is true only for one of the two inputs.
        Console.WriteLine($"  exception filter: {Filtered("boom")}");
        Console.WriteLine($"  exception filter: {Filtered("other")}");

        // `await` in a catch block and in a finally block.
        Console.WriteLine($"  await in catch/finally: {AwaitInHandlersAsync().GetAwaiter().GetResult()}");
    }

    private static string Filtered(string message)
    {
        try
        {
            throw new InvalidOperationException(message);
        }
        catch (InvalidOperationException e) when (e.Message == "boom")
        {
            return $"matched-filter({e.Message})";
        }
        catch (InvalidOperationException e)
        {
            return $"fell-through({e.Message})";
        }
    }

    private static async Task<string> AwaitInHandlersAsync()
    {
        var log = new StringBuilder();
        try
        {
            log.Append(await StepAsync("try"));
            throw new InvalidOperationException("thrown");
        }
        catch (InvalidOperationException e) when (e.Message == "thrown")
        {
            log.Append(await StepAsync("catch"));
        }
        finally
        {
            log.Append(await StepAsync("finally"));
        }

        return log.ToString();
    }

    private static async Task<string> StepAsync(string name)
    {
        await Task.Yield();
        return "[" + name + "]";
    }
}

internal sealed class Badge
{
    private readonly string[] _letters;

    internal Badge(string name)
    {
        Name = name;
        _letters = new string[name.Length];
        for (int i = 0; i < name.Length; i++)
        {
            _letters[i] = name[i].ToString();
        }
    }

    internal int Id { get; set; } = 7;                      // auto-property initializer
    internal string Name { get; }                           // getter-only auto-property
    internal string Display => $"{Name}#{Id}";              // expression-bodied property
    internal string this[int i] => _letters[i];             // expression-bodied indexer
    internal string Shout() => Name.ToUpperInvariant();     // expression-bodied method
}

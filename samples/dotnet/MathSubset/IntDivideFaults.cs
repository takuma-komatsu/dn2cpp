using System;
using System.Runtime.CompilerServices;

// The two faults an integer division can raise, at every width and through every
// mouth the transpiler lowers: IL div / div.un / rem / rem.un (this file's
// operators) and Math.DivRem's two overload shapes. Without an emitted zero test
// the answer is whatever the hardware does, and that is not uniformly a trap:
// arm64 answers 0 for `10/0` and 10 for `10%0` — a silent wrong answer on every
// Apple and Android target — while x86-64 raises SIGFPE and wasm traps. None of
// the three is catchable and none names anything.
//
// Every divisor comes through NoInlining helpers for two independent reasons:
// Roslyn refuses to compile a constant `1 / 0` at all, and the emitted guards are
// inline functions over single-assignment temps, so a divisor clang can see
// through would have its check folded away and this file would assert nothing.
// The CONSTANT-divisor answers are checked at the bottom, because that fold is
// why no transpiler-side constant-divisor analysis exists and it has to keep
// producing right answers.
//
// Floating point is the control: IEEE division by zero is ±Infinity and .NET
// agrees, so the guards must not touch double or float.
namespace IntDivideFaults;

static class Program
{
    [MethodImpl(MethodImplOptions.NoInlining)] private static int Zero() => 0;
    [MethodImpl(MethodImplOptions.NoInlining)] private static int MinusOne() => -1;
    [MethodImpl(MethodImplOptions.NoInlining)] private static long ZeroL() => 0L;
    [MethodImpl(MethodImplOptions.NoInlining)] private static long MinusOneL() => -1L;
    [MethodImpl(MethodImplOptions.NoInlining)] private static uint ZeroU() => 0u;
    [MethodImpl(MethodImplOptions.NoInlining)] private static ulong ZeroUL() => 0ul;
    [MethodImpl(MethodImplOptions.NoInlining)] private static nint ZeroN() => 0;
    [MethodImpl(MethodImplOptions.NoInlining)] private static nint MinusOneN() => -1;
    [MethodImpl(MethodImplOptions.NoInlining)] private static nuint ZeroNU() => 0;
    [MethodImpl(MethodImplOptions.NoInlining)] private static decimal ZeroM() => 0m;

    // Every probe reports the exception's TYPE NAME only — the messages are not
    // part of the contract — or its value when nothing throws. Execution
    // continuing past each line is half the assertion: a runtime that aborted at
    // one of these could not produce the expected output at all.
    private static void P(string what, Func<string> body)
    {
        try { Console.WriteLine(what + " -> " + body()); }
        catch (Exception e) { Console.WriteLine(what + " -> " + e.GetType().Name); }
    }

    internal static void __GateEntry()
    {
        // ── zero divisor, signed, every width ────────────────────────────────
        P("int / 0", () => (10 / Zero()).ToString());
        P("int % 0", () => (10 % Zero()).ToString());
        P("long / 0", () => (10L / ZeroL()).ToString());
        P("long % 0", () => (10L % ZeroL()).ToString());
        P("nint / 0", () => ((nint)10 / ZeroN()).ToString());
        P("nint % 0", () => ((nint)10 % ZeroN()).ToString());
        // A zero DIVIDEND over a zero divisor still faults — the test is on the
        // divisor, and `0/0` is DivideByZeroException in .NET, not 0.
        P("0 / 0", () => (Zero() / Zero()).ToString());

        // ── zero divisor, unsigned (IL div.un / rem.un) ──────────────────────
        P("uint / 0", () => (10u / ZeroU()).ToString());
        P("uint % 0", () => (10u % ZeroU()).ToString());
        P("ulong / 0", () => (10ul / ZeroUL()).ToString());
        P("ulong % 0", () => (10ul % ZeroUL()).ToString());
        P("nuint / 0", () => ((nuint)10 / ZeroNU()).ToString());
        P("nuint % 0", () => ((nuint)10 % ZeroNU()).ToString());

        // ── MinValue / -1: OverflowException, and for the REMAINDER too ──────
        // The remainder is the one that surprises: C says `x % -1` is 0, .NET
        // says OverflowException, and the interpreter's hand-written copy of
        // this guard answered 0 until it started sharing the emitted one.
        P("int.Min / -1", () => (int.MinValue / MinusOne()).ToString());
        P("int.Min % -1", () => (int.MinValue % MinusOne()).ToString());
        P("long.Min / -1", () => (long.MinValue / MinusOneL()).ToString());
        P("long.Min % -1", () => (long.MinValue % MinusOneL()).ToString());
        P("nint.Min / -1", () => (nint.MinValue / MinusOneN()).ToString());
        P("nint.Min % -1", () => (nint.MinValue % MinusOneN()).ToString());
        // -1 as a divisor is otherwise ordinary, and MinValue is otherwise an
        // ordinary dividend: only the PAIR overflows.
        P("int.Min / 2", () => (int.MinValue / 2).ToString());
        P("(Min+1) / -1", () => ((int.MinValue + 1) / MinusOne()).ToString());
        P("7 / -1", () => (7 / MinusOne()).ToString());
        P("7 % -1", () => (7 % MinusOne()).ToString());
        // The unsigned arm must NOT inherit the overflow test: there (T)-1 is
        // the maximum value, so a shared guard would raise for this line.
        P("0u / uint.Max", () => (0u / (uint.MaxValue - (uint)Zero())).ToString());

        // ── Math.DivRem: the same operation reached by another name ──────────
        P("DivRem(10, 0)", () => Math.DivRem(10, Zero()).ToString());
        P("DivRem(10L, 0)", () => Math.DivRem(10L, ZeroL()).ToString());
        P("DivRem(10u, 0)", () => Math.DivRem(10u, ZeroU()).ToString());
        P("DivRem(Min, -1)", () => Math.DivRem(int.MinValue, MinusOne()).ToString());
        P("DivRem out(10, 0)", () => { int r; int q = Math.DivRem(10, Zero(), out r); return q + "," + r; });
        // The non-faulting shapes still answer.
        P("DivRem(17, 5)", () => Math.DivRem(17, 5 + Zero()).ToString());
        P("DivRem out(17, 5)", () => { int r; int q = Math.DivRem(17, 5 + Zero(), out r); return q + "," + r; });

        // ── decimal: its own div/rem helpers, same exception ─────────────────
        P("decimal / 0", () => (1.0m / ZeroM()).ToString());
        P("decimal % 0", () => (1.0m % ZeroM()).ToString());

        // ── floating point is NOT guarded ────────────────────────────────────
        // Reported as PREDICATES rather than as the formatted value on purpose:
        // dn2cpp's invariant culture spells positive infinity "Infinity" where
        // real .NET 10 spells it "∞", and printing the value here would fail this
        // exact diff on a symbol table that has nothing to do with division.
        P("double / 0 isPosInf", () => double.IsPositiveInfinity(10.0 / (double)Zero()).ToString());
        P("double % 0 isNaN", () => double.IsNaN(10.0 % (double)Zero()).ToString());
        P("float / 0 isPosInf", () => float.IsPositiveInfinity(10.0f / (float)Zero()).ToString());
        P("-1.0 / 0 isNegInf", () => double.IsNegativeInfinity(-1.0 / (double)Zero()).ToString());
        P("0.0 / 0 isNaN", () => double.IsNaN(0.0 / (double)Zero()).ToString());

        // ── the catch hierarchy ──────────────────────────────────────────────
        // DivideByZeroException derives from ArithmeticException in .NET, and
        // dn2cpp's runtime handle has to chain the same way or a `catch
        // (ArithmeticException)` around a numeric loop silently stops matching.
        try { Console.WriteLine(1 / Zero()); }
        catch (ArithmeticException e) { Console.WriteLine("arith-catch: " + e.GetType().Name); }
        try { Console.WriteLine(1 / Zero()); }
        catch (DivideByZeroException) { Console.WriteLine("typed catch: DivideByZeroException"); }
        catch (Exception) { Console.WriteLine("typed catch: fell through"); }
        // A fault inside a finally-guarded region still runs the finally.
        int ran = 0;
        try
        {
            try { Console.WriteLine(1 / Zero()); }
            finally { ran++; }
        }
        catch (DivideByZeroException) { Console.WriteLine("finally ran " + ran + " time(s) before the catch"); }

        // ── the ordinary path, and the constant-divisor fold ─────────────────
        // These are what the check must not cost anything on: the divisor is a
        // compile-time constant, so clang folds both arms of the guard away.
        // They are here to prove the fold still computes the right thing —
        // including the sign of a truncating division, which C and .NET agree on.
        int n = 17 + Zero();
        Console.WriteLine("17/5=" + (n / 5) + " 17%5=" + (n % 5));
        Console.WriteLine("-17/5=" + (-n / 5) + " -17%5=" + (-n % 5));
        Console.WriteLine("17/-5=" + (n / -5) + " 17%-5=" + (n % -5));
        Console.WriteLine("shifty=" + (n / 2) + "," + (n / 4) + "," + (n % 16) + "," + (n % 8));
        long l = 1234567890123L + ZeroL();
        Console.WriteLine("longconst=" + (l / 1000) + "," + (l % 1000));
        uint u = 4000000000u + ZeroU();
        Console.WriteLine("uconst=" + (u / 7) + "," + (u % 7));
        Console.WriteLine("done");
    }
}

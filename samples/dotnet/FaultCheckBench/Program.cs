using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

// The WORST CASE for the managed null-receiver and divisor guards, so their cost can be
// decided from a number rather than from an argument. Every kernel is a tight loop over
// exactly one guarded shape with nothing else in the body — a real program dilutes the
// guard among the work it is guarding, and this deliberately does not.
//
// Every guarded value VARIES PER ITERATION, and that is the whole design: hold the
// receiver or the divisor in a local and the check becomes loop-invariant, clang hoists
// it out, and the kernel measures the absence of a worst case. So receivers come out of
// an object array indexed by the loop variable and divisors out of an int array, which
// nothing can hoist and nothing can prove non-null or non-zero.
//
// The two `*const` kernels are the control and the one thing here that is NOT a worst
// case: their divisor is a compile-time constant, so both arms of the guard fold away.
// If those rows move between the guards-on and guards-off arms, then clang is not doing
// the constant-divisor analysis and a transpiler-side one is needed.
//
// Timing is per kernel and internal (Stopwatch): process wall clock on a binary this
// small is mostly dynamic linker. Driven by gates/measure-faultchecks.sh, which builds
// this one transpile with the guards on and off; it is not a gate.
namespace FaultCheckBench;

internal interface IAdder { int Add(int x); }

internal class Adder : IAdder
{
    internal int Bias;
    public virtual int Add(int x) => x + Bias;
}

internal sealed class SealedAdder : Adder
{
    public override int Add(int x) => x + Bias + 1;
}

internal static class Program
{
    private const int N = 40_000_000;
    private const int Mask = 63;          // the receiver/divisor pool size - 1

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Adder[] Adders()
    {
        var a = new Adder[Mask + 1];
        for (int i = 0; i <= Mask; i++) a[i] = (i & 1) == 0 ? new Adder { Bias = i } : new SealedAdder { Bias = i };
        return a;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static SealedAdder[] Sealeds()
    {
        var a = new SealedAdder[Mask + 1];
        for (int i = 0; i <= Mask; i++) a[i] = new SealedAdder { Bias = i };
        return a;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static IAdder[] Ifaces()
    {
        var a = new IAdder[Mask + 1];
        for (int i = 0; i <= Mask; i++) a[i] = new Adder { Bias = i };
        return a;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static string[] Strings()
    {
        var a = new string[Mask + 1];
        for (int i = 0; i <= Mask; i++) a[i] = new string('x', (i & 7) + 1);
        return a;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int[] Divisors()
    {
        var a = new int[Mask + 1];
        for (int i = 0; i <= Mask; i++) a[i] = (i & 15) + 1;   // never zero, never -1
        return a;
    }

    private static void Time(string name, Func<int> body)
    {
        var sw = Stopwatch.StartNew();
        int checksum = body();
        sw.Stop();
        Console.WriteLine(name + " checksum=" + checksum + " ms=" + sw.ElapsedMilliseconds);
    }

    internal static void Main()
    {
        // ── one null check per iteration, each in a different mouth ───────────
        // A devirtualized callvirt (sealed override), where the receiver reaches
        // the callee body with no dereference of its own to fault on.
        Time("sealedcall", () =>
        {
            var a = Sealeds();
            int acc = 0;
            for (int i = 0; i < N; i++) acc += a[i & Mask].Add(i & 15);
            return acc;
        });
        // A real vtable dispatch: the guard sits in front of the `->type` load,
        // and the array holds both subclasses so the branch is not predictable
        // into one target.
        Time("virtcall", () =>
        {
            var a = Adders();
            int acc = 0;
            for (int i = 0; i < N; i++) acc += a[i & Mask].Add(i & 15);
            return acc;
        });
        // Interface dispatch: the guard sits in front of dn2cpp_resolve_interface.
        Time("ifacecall", () =>
        {
            var a = Ifaces();
            int acc = 0;
            for (int i = 0; i < N; i++) acc += a[i & Mask].Add(i & 15);
            return acc;
        });
        // ldfld through the FieldAccess guard.
        Time("fieldread", () =>
        {
            var a = Adders();
            int acc = 0;
            for (int i = 0; i < N; i++) acc += a[i & Mask].Bias;
            return acc;
        });
        // stfld, so the write side of the same guard is measured too.
        Time("fieldwrite", () =>
        {
            var a = Adders();
            for (int i = 0; i < N; i++) a[i & Mask].Bias = i & 15;
            return a[0].Bias;
        });
        // String.get_Length, guarded on the grounds that it is the hottest
        // dereference in the corpus — this kernel is what prices that claim.
        Time("strlen", () =>
        {
            var s = Strings();
            int acc = 0;
            for (int i = 0; i < N; i++) acc += s[i & Mask].Length;
            return acc;
        });

        // ── one divisor check per iteration ──────────────────────────────────
        // The divisor comes out of an array, so neither the zero test nor the
        // MinValue/-1 test can be folded and neither can be hoisted.
        Time("divvar", () =>
        {
            var d = Divisors();
            int acc = 0;
            for (int i = 0; i < N; i++) acc += i / d[i & Mask];
            return acc;
        });
        Time("remvar", () =>
        {
            var d = Divisors();
            int acc = 0;
            for (int i = 0; i < N; i++) acc += i % d[i & Mask];
            return acc;
        });
        // Unsigned: one test rather than two.
        Time("udivvar", () =>
        {
            var d = Divisors();
            int acc = 0;
            for (int i = 0; i < N; i++) acc += (int)((uint)i / (uint)d[i & Mask]);
            return acc;
        });
        // The CONTROL: a compile-time-constant divisor, over the same array load
        // so the rest of the loop body matches divvar. These rows must not move.
        Time("divconst", () =>
        {
            var d = Divisors();
            int acc = 0;
            for (int i = 0; i < N; i++) acc += (i + d[i & Mask]) / 7;
            return acc;
        });
        Time("remconst", () =>
        {
            var d = Divisors();
            int acc = 0;
            for (int i = 0; i < N; i++) acc += (i + d[i & Mask]) % 7;
            return acc;
        });
    }
}

using System;
using System.Threading;
using System.Runtime.CompilerServices;

namespace ThreadingPrimitives;

internal static class MonitorLockValidationSubset
{
    static object? NullObject() => null;
    static string Held(object gate) => " held=" + Monitor.IsEntered(gate);

    static void Fault(string label, Func<object> run)
    {
        try { Console.WriteLine(label + " -> " + run()); }
        catch (Exception ex) { PrintFault(label, ex); }
    }

    static void PrintFault(string label, Exception ex)
    {
        object? actual = ex is ArgumentOutOfRangeException range ? range.ActualValue : null;
        Console.WriteLine(label + " -> " + ex.GetType().Name + ": "
            + ex.Message.Replace("\r", "").Replace("\n", "|")
            + " param=" + (ex is ArgumentException argument ? argument.ParamName : null)
            + " actual=" + (actual is null ? "null" : actual.GetType().Name + ":" + actual));
    }

    internal static void __GateEntry()
    {
        MonitorOwnership();
        LockOwnership();
        IndependentOwnership();
        var sync = new SynchronizedProbe();
        Fault("synchronized owned", () => sync.Held());
        Fault("synchronized manual exit", () => sync.Exit(false));
        Fault("synchronized manual exit throws", () => sync.Exit(true));
        Console.WriteLine("synchronized released=" + Monitor.IsEntered(sync));
        Console.WriteLine("ownership fields end");
    }

    static void MonitorOwnership()
    {
        Console.WriteLine("== monitor ownership ==");
        object gate = new object();
        object other = new object();
        bool taken = false;
        Fault("TryEnter, TimeSpan", () => Monitor.TryEnter(NullObject()!, TimeSpan.Zero));
        Fault("TryEnter, TimeSpan, taken", () => { Monitor.TryEnter(NullObject()!, TimeSpan.Zero, ref taken); return taken; });
        Fault("TryEnter, timeout, taken", () => { Monitor.TryEnter(NullObject()!, 1, ref taken); return taken; });
        Fault("Wait, TimeSpan", () => Monitor.Wait(NullObject()!, TimeSpan.Zero));
        Fault("Wait, timeout, exitContext", () => Monitor.Wait(NullObject()!, 1, false));
        Fault("Wait, TimeSpan, exitContext", () => Monitor.Wait(NullObject()!, TimeSpan.Zero, false));
        Fault("IsEntered", () => Monitor.IsEntered(NullObject()!));
        Console.WriteLine("taken=" + taken + Held(gate));
        lock (gate)
        {
            Console.WriteLine("locked:" + Held(gate) + " other=" + Monitor.IsEntered(other));
            lock (gate) { }
            Console.WriteLine("after a nested exit:" + Held(gate));
            // A timed wait nothing pulses times out and still owns the monitor.
            Console.WriteLine("Wait(0): " + Monitor.Wait(gate, 0) + Held(gate));
            Console.WriteLine("Wait(TimeSpan.Zero): " + Monitor.Wait(gate, TimeSpan.Zero) + Held(gate));
            Console.WriteLine("Wait(0, false): " + Monitor.Wait(gate, 0, false) + Held(gate));
            Fault("Wait, -2", () => Monitor.Wait(gate, -2));
            Fault("Wait, TimeSpan -2ms", () => Monitor.Wait(gate, TimeSpan.FromMilliseconds(-2)));
            Fault("Wait, TimeSpan past Int32", () => Monitor.Wait(gate, TimeSpan.FromMilliseconds(int.MaxValue + 1.0)));
            Fault("Pulse, owned", () => { Monitor.Pulse(gate); return "pulsed"; });
            Fault("PulseAll, owned", () => { Monitor.PulseAll(gate); return "pulsed"; });
        }
        Console.WriteLine("released:" + Held(gate));
        // A thread that does not own the monitor can neither exit, wait on nor pulse it.
        Fault("Exit, unowned", () => { Monitor.Exit(gate); return "exited"; });
        Fault("Wait, unowned", () => Monitor.Wait(gate));
        Fault("Wait, timeout, unowned", () => Monitor.Wait(gate, 0));
        Fault("Wait, -2, unowned", () => Monitor.Wait(gate, -2));
        Fault("Pulse, unowned", () => { Monitor.Pulse(gate); return "pulsed"; });
        Fault("PulseAll, unowned", () => { Monitor.PulseAll(gate); return "pulsed"; });
        Console.WriteLine("after the refusals:" + Held(gate));
        // For int timeouts, lockTaken must be false before the other checks.
        taken = true;
        Fault("Enter, taken set", () => { Monitor.Enter(gate, ref taken); return "entered"; });
        Fault("TryEnter, taken set", () => { Monitor.TryEnter(gate, ref taken); return taken; });
        Fault("TryEnter, timeout, taken set", () => { Monitor.TryEnter(gate, 0, ref taken); return taken; });
        Fault("TryEnter, TimeSpan, taken set", () => { Monitor.TryEnter(gate, TimeSpan.Zero, ref taken); return taken; });
        Fault("Enter null, taken set", () => { Monitor.Enter(NullObject()!, ref taken); return "entered"; });
        Fault("TryEnter null, taken set", () => { Monitor.TryEnter(NullObject()!, ref taken); return taken; });
        Fault("TryEnter -2, taken set", () => { Monitor.TryEnter(gate, -2, ref taken); return taken; });
        Console.WriteLine("taken=" + taken + Held(gate));
        taken = false;
        Fault("TryEnter, -2", () => Monitor.TryEnter(gate, -2));
        Fault("TryEnter, TimeSpan -2ms", () => Monitor.TryEnter(gate, TimeSpan.FromMilliseconds(-2)));
        Fault("TryEnter, TimeSpan past Int32", () => Monitor.TryEnter(gate, TimeSpan.FromMilliseconds(int.MaxValue + 1.0)));
        Fault("TryEnter null, -2", () => Monitor.TryEnter(NullObject()!, -2));
        Fault("TryEnter null, TimeSpan -2ms", () => Monitor.TryEnter(NullObject()!, TimeSpan.FromMilliseconds(-2)));
        Fault("TryEnter -2, taken", () => { Monitor.TryEnter(gate, -2, ref taken); return taken; });
        Console.WriteLine("taken=" + taken + Held(gate));
        // -1.5 ms truncates to -1, the infinite timeout, as InfiniteTimeSpan is.
        bool got = Monitor.TryEnter(gate, TimeSpan.FromMilliseconds(-1.5));
        Console.WriteLine("TryEnter, -1.5ms: " + got + Held(gate));
        if (got)
            Monitor.Exit(gate);
        got = Monitor.TryEnter(gate, Timeout.InfiniteTimeSpan);
        Console.WriteLine("TryEnter, infinite: " + got + Held(gate));
        if (got)
            Monitor.Exit(gate);
        Console.WriteLine("monitor ownership end");
    }

    static void LockOwnership()
    {
        Console.WriteLine("== lock ownership ==");
        Lock gate = new Lock();
        Console.WriteLine("held=" + gate.IsHeldByCurrentThread);
        gate.Enter();
        gate.Enter();
        gate.Exit();
        Console.WriteLine("after a nested exit: held=" + gate.IsHeldByCurrentThread);
        gate.Exit();
        Console.WriteLine("released: held=" + gate.IsHeldByCurrentThread);
        Fault("Exit, unowned", () => { gate.Exit(); return "exited"; });
        // Disposing a scope twice releases once; a copy of a disposed scope releases again.
        Fault("scope disposed twice", () =>
        {
            Lock.Scope scope = gate.EnterScope();
            scope.Dispose();
            scope.Dispose();
            return "held=" + gate.IsHeldByCurrentThread;
        });
        Fault("copy of a disposed scope", () =>
        {
            Lock.Scope scope = gate.EnterScope();
            Lock.Scope copy = scope;
            scope.Dispose();
            copy.Dispose();
            return "disposed";
        });
        Fault("TryEnter(0)", () => { bool got = gate.TryEnter(0); if (got) gate.Exit(); return got; });
        Fault("TryEnter(TimeSpan.Zero)", () => { bool got = gate.TryEnter(TimeSpan.Zero); if (got) gate.Exit(); return got; });
        Fault("TryEnter(InfiniteTimeSpan)", () => { bool got = gate.TryEnter(Timeout.InfiniteTimeSpan); if (got) gate.Exit(); return got; });
        Fault("TryEnter(-2)", () => gate.TryEnter(-2));
        Fault("TryEnter(TimeSpan -2ms)", () => gate.TryEnter(TimeSpan.FromMilliseconds(-2)));
        Fault("TryEnter(TimeSpan past Int32)", () => gate.TryEnter(TimeSpan.FromMilliseconds(int.MaxValue + 1.0)));
        Console.WriteLine("held=" + gate.IsHeldByCurrentThread);
        Lock? missing = null;
        Fault("IsHeldByCurrentThread, null", () => missing!.IsHeldByCurrentThread);
        Fault("TryEnter(TimeSpan -2ms), null", () => missing!.TryEnter(TimeSpan.FromMilliseconds(-2)));
        Console.WriteLine("lock ownership end");
    }

    static void IndependentOwnership()
    {
        var gate = new Lock();
#pragma warning disable CS9216 // Probe both independent ownership domains on the same object.
        object monitor = gate;
#pragma warning restore CS9216
        Monitor.Enter(monitor);
        Console.WriteLine("independent lock after monitor=" + gate.IsHeldByCurrentThread);
        Fault("independent Lock.Exit", () => { gate.Exit(); return "exited"; });
        Monitor.Exit(monitor);
        gate.Enter();
        Console.WriteLine("independent monitor after lock=" + Monitor.IsEntered(monitor));
        Fault("independent Monitor.Exit", () => { Monitor.Exit(monitor); return "exited"; });
        gate.Exit();
        Lock.Scope empty = default;
        empty.Dispose();
        Console.WriteLine("default scope held=" + gate.IsHeldByCurrentThread);

        gate.Enter();
        Monitor.Enter(monitor);
        var worker = new Thread(() =>
        {
            Console.WriteLine("worker held=" + Monitor.IsEntered(monitor) + "/" + gate.IsHeldByCurrentThread);
            Fault("worker Monitor.Exit", () => { Monitor.Exit(monitor); return "exited"; });
            Fault("worker Monitor.Wait", () => Monitor.Wait(monitor, 0));
            Fault("worker Monitor.Pulse", () => { Monitor.Pulse(monitor); return "pulsed"; });
            Fault("worker Lock.Exit", () => { gate.Exit(); return "exited"; });
            Console.WriteLine("worker try=" + Monitor.TryEnter(monitor, 0) + "/" + gate.TryEnter(0));
        });
        worker.Start();
        worker.Join();
        Console.WriteLine("owner held=" + Monitor.IsEntered(monitor) + "/" + gate.IsHeldByCurrentThread);
        gate.Exit();
        Monitor.Exit(monitor);

        Exception[] saved = new Exception[2];
        try { Monitor.TryEnter(monitor, TimeSpan.MinValue); } catch (Exception ex) { saved[0] = ex; }
        try { gate.TryEnter(-2); } catch (Exception ex) { saved[1] = ex; }
        GC.Collect();
        GC.WaitForPendingFinalizers();
        for (int i = 0; i < saved.Length; i++)
            PrintFault("ownership GC " + i, saved[i]);
        Console.WriteLine("independent ownership end");
    }
    sealed class SynchronizedProbe
    {
        [MethodImpl(MethodImplOptions.Synchronized)]
        internal bool Held() => Monitor.IsEntered(this);

        [MethodImpl(MethodImplOptions.Synchronized)]
        internal bool Exit(bool fail)
        {
            Monitor.Exit(this);
            if (fail)
                throw new InvalidOperationException("user fault");
            return true;
        }
    }

}

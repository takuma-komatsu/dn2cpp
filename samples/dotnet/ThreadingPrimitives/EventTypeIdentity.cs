using System;
using System.Threading;

// Type identity across the four CLR event types the runtime serves with one Dn2CppEvent.
// Each needs its own type-info: they stand in a real inheritance relation (MRE/ARE are
// sealed siblings under EventWaitHandle, ManualResetEventSlim is not a WaitHandle at all)
// that one shared handle would flatten into wrong `is` answers. SUBJECT: a reflection and
// casting invariant, not a synchronization one — do not prune this section by theme.
namespace EventTypeIdentity;

internal static class Program
{
    private static void Probe(string label, object o)
    {
        Console.WriteLine($"{label} type={o.GetType()}");
        Console.WriteLine($"{label} is  mre={o is ManualResetEvent} are={o is AutoResetEvent} "
            + $"ewh={o is EventWaitHandle} slim={o is ManualResetEventSlim} wh={o is WaitHandle}");
        Console.WriteLine($"{label} eq  mre={o.GetType() == typeof(ManualResetEvent)} "
            + $"are={o.GetType() == typeof(AutoResetEvent)} "
            + $"ewh={o.GetType() == typeof(EventWaitHandle)} "
            + $"slim={o.GetType() == typeof(ManualResetEventSlim)}");
        Console.WriteLine($"{label} base={o.GetType().BaseType}");
    }

    internal static void __GateEntry()
    {
        Console.WriteLine("== event type identity ==");

        Probe("mre ", new ManualResetEvent(false));
        Probe("are ", new AutoResetEvent(false));
        // Both reset modes of the BASE type: one CLR type (the mode is a ctor argument,
        // not a subclass), so the reset mode cannot stand in for the type-info.
        Probe("ewhM", new EventWaitHandle(false, EventResetMode.ManualReset));
        Probe("ewhA", new EventWaitHandle(false, EventResetMode.AutoReset));
        Probe("slim", new ManualResetEventSlim(false));

        // The static relations, so a wrong BASE pointer is named rather than merely implied.
        Console.WriteLine("rel mre.base=" + typeof(ManualResetEvent).BaseType);
        Console.WriteLine("rel are.base=" + typeof(AutoResetEvent).BaseType);
        Console.WriteLine("rel ewh.base=" + typeof(EventWaitHandle).BaseType);
        Console.WriteLine("rel slim.base=" + typeof(ManualResetEventSlim).BaseType);
        Console.WriteLine("rel ewh<-mre=" + typeof(EventWaitHandle).IsAssignableFrom(typeof(ManualResetEvent)));
        Console.WriteLine("rel ewh<-are=" + typeof(EventWaitHandle).IsAssignableFrom(typeof(AutoResetEvent)));
        Console.WriteLine("rel mre<-ewh=" + typeof(ManualResetEvent).IsAssignableFrom(typeof(EventWaitHandle)));
        Console.WriteLine("rel wh<-slim=" + typeof(WaitHandle).IsAssignableFrom(typeof(ManualResetEventSlim)));

        // Signalling must still work: the type-info and the event state are one memory
        // write apart in dn2cpp_event_new.
        var m = new ManualResetEvent(false);
        Console.WriteLine("work mre initial=" + m.WaitOne(0));
        m.Set();
        Console.WriteLine("work mre after-set=" + m.WaitOne(0) + " again=" + m.WaitOne(0));
        m.Reset();
        Console.WriteLine("work mre after-reset=" + m.WaitOne(0));

        var a = new AutoResetEvent(false);
        Console.WriteLine("work are initial=" + a.WaitOne(0));
        a.Set();
        // Auto-reset: the first wait consumes the signal, the second sees it gone.
        Console.WriteLine("work are after-set=" + a.WaitOne(0) + " again=" + a.WaitOne(0));

        var e = new EventWaitHandle(false, EventResetMode.AutoReset);
        e.Set();
        Console.WriteLine("work ewh auto=" + e.WaitOne(0) + " again=" + e.WaitOne(0));
        var e2 = new EventWaitHandle(false, EventResetMode.ManualReset);
        e2.Set();
        Console.WriteLine("work ewh manual=" + e2.WaitOne(0) + " again=" + e2.WaitOne(0));

        var s = new ManualResetEventSlim(false);
        Console.WriteLine("work slim initial=" + s.IsSet);
        s.Set();
        Console.WriteLine("work slim after-set=" + s.IsSet);
    }
}

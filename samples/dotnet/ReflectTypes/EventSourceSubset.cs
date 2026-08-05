using System;
using System.Diagnostics.Tracing;

namespace EventSourceSubset
{
    // A USER EventSource provider over the opaque intrinsic EventSource base — the
    // "complete fake" (the sibling of the framework-provider fold, which lives in
    // its own POSIX gate, samples/dotnet/EventSourceProbe).
    //
    // A native build ships no EventPipe/ETW/EventListener, so a user (or Microsoft.*)
    // provider transpiles and runs as plain C# over an opaque base: it constructs
    // (its cctor runs — only FRAMEWORK providers are folded away), its [Event] methods
    // run their own body and no-op the WriteEvent, IsEnabled() is always false, and
    // the provider's own derived state is real.
    //
    // EVERY LINE THIS SECTION PRINTS MATCHES REAL .NET, which is the point: two halves of
    // EventSource look alike and are not.
    //
    //   * DELIVERY is a no-op — and that is faithful, not degraded. Every write path in
    //     .NET's own EventSource opens with `if (!IsEnabled()) return;`, and no listener
    //     can attach here, so real .NET returns from WriteEvent / Write / Write<T> having
    //     done nothing observable either. Throwing or aborting instead would be a
    //     divergence dressed as a boundary.
    //   * IDENTITY is answered for real. Name, Guid and Settings are computed by .NET
    //     from the type and the base ctor's arguments before any listener exists, and
    //     CurrentThreadActivityId is a per-thread Guid slot .NET reads back with nothing
    //     listening. Guid.Empty is a value real .NET returns for NO provider, since the
    //     guid is derived from the name (EventSource.GenerateGuidFromName).
    //
    // So the sections below are a LIVE .NET oracle in freeze-gate clothing: the bucket
    // is frozen because sections elsewhere in it diverge, and if any line here ever
    // stops matching `dotnet run` on this same project, the divergence is a regression
    // rather than a boundary. The Guid literals are the load-bearing ones — they pin
    // dn2cpp's re-implementation of GenerateGuidFromName against .NET's, and a wrong
    // SHA-1 or a wrong Guid byte order would still print a plausible-looking guid.
    //
    // What is NOT here, deliberately: EventListener. Observation is unmodeled and stays
    // a loud transpile abort (see MethodCompiler.TryEmitEventSourceIntrinsic's
    // get_IsSupported arm) — a listener that silently receives nothing is the
    // load-bearing-consumer failure of docs/ARCHITECTURE.md §4-B.

    // Named by attribute — the documented way, and what the framework's own providers do.
    [EventSource(Name = "Dn2Cpp-Gate-MyLog")]
    internal sealed class MyLog : EventSource
    {
        public static readonly MyLog Log = new MyLog();

        // The provider's OWN derived state: proof the subclass body runs over the
        // opaque base rather than being folded away like a framework provider.
        public int Ticks;

        [Event(1)]
        public void Tick(int n)
        {
            Ticks += n;            // runs
            WriteEvent(1, n);      // no-op
        }
    }

    // No attribute at all: .NET falls back to the type's SIMPLE name, so this one is
    // "PlainLog" and not "EventSourceSubset.PlainLog".
    internal sealed class PlainLog : EventSource
    {
        public static readonly PlainLog Log = new PlainLog();
    }

    // An explicit guid overrides the name-derived one; the name still falls back to the
    // type name, so this pins that the two attribute properties are read independently.
    [EventSource(Guid = "12345678-1234-1234-1234-123456789012")]
    internal sealed class GuidOnlyLog : EventSource
    {
        public static readonly GuidOnlyLog Log = new GuidOnlyLog();
    }

    // Named by the BASE CTOR instead of by attribute. This is the one shape no type-info
    // stamp can carry — the name is a per-instance value — so it is what proves the
    // runtime's per-instance record exists and that the guid follows the name that won.
    internal sealed class CtorNamedLog : EventSource
    {
        public static readonly CtorNamedLog Log = new CtorNamedLog();
        private CtorNamedLog() : base("Dn2Cpp-Gate-CtorNamed") { }
    }

    // Settings passed to the base ctor. Real .NET reports 8 here and 4 for every provider
    // above (ValidateSettings defaults the manifest format in); it reports
    // EventSourceSettings.Default (0) for none of them, which is what dn2cpp used to say.
    internal sealed class SelfDescLog : EventSource
    {
        public static readonly SelfDescLog Log = new SelfDescLog();
        private SelfDescLog() : base(EventSourceSettings.EtwSelfDescribingEventFormat) { }
    }

    internal static class Program
    {
        internal static void Run()
        {
            // The provider is a real allocation whose cctor ran — Log is a live MyLog.
            Console.WriteLine("Log is not null: " + (MyLog.Log is not null));
            Console.WriteLine("Log is EventSource: " + (MyLog.Log is EventSource));

            // IsEnabled is always false: no listener can attach in a native build — and
            // false is also what real .NET answers here, with none attached.
            // Both the parameterless and the (level, keywords) overload.
            Console.WriteLine("IsEnabled(): " + MyLog.Log.IsEnabled());
            Console.WriteLine("IsEnabled(lvl,kw): "
                + MyLog.Log.IsEnabled(EventLevel.Informational, EventKeywords.None));

            // The [Event] method runs its own body — the WriteEvent inside is the no-op,
            // but the derived field write is real, so two Ticks accumulate to 50.
            MyLog.Log.Tick(42);
            MyLog.Log.Tick(8);
            Console.WriteLine("Ticks: " + MyLog.Log.Ticks);

            // ── Identity: names ──────────────────────────────────────────────────
            Console.WriteLine("attr name: " + MyLog.Log.Name);
            Console.WriteLine("type name: " + PlainLog.Log.Name);
            Console.WriteLine("guid-only name: " + GuidOnlyLog.Log.Name);
            Console.WriteLine("ctor name: " + CtorNamedLog.Log.Name);

            // ── Identity: guids ──────────────────────────────────────────────────
            // Derived from the name by SHA-1 over a fixed namespace, uppercased and in
            // big-endian UTF-16, version nibble 5. Never Guid.Empty.
            Console.WriteLine("attr guid: " + MyLog.Log.Guid);
            Console.WriteLine("type guid: " + PlainLog.Log.Guid);
            Console.WriteLine("explicit guid: " + GuidOnlyLog.Log.Guid);
            Console.WriteLine("ctor guid: " + CtorNamedLog.Log.Guid);
            Console.WriteLine("guid is empty: " + (MyLog.Log.Guid == Guid.Empty));

            // ── Identity: settings ───────────────────────────────────────────────
            Console.WriteLine("default settings: " + (int)MyLog.Log.Settings);
            Console.WriteLine("selfdesc settings: " + (int)SelfDescLog.Log.Settings);

            // ── Identity keyed by TYPE rather than instance ──────────────────────
            // .NET's static helpers read the attribute with inherit:false and fall back
            // to Type.Name, so they never see a ctor-supplied name — including here,
            // where GetName(typeof(CtorNamedLog)) reports the TYPE name while the
            // instance reports the ctor's. Pinning that disagreement is the point:
            // it is .NET's, not dn2cpp's.
            Console.WriteLine("GetName(attr): " + EventSource.GetName(typeof(MyLog)));
            Console.WriteLine("GetName(ctor-named): " + EventSource.GetName(typeof(CtorNamedLog)));
            Console.WriteLine("GetGuid(attr): " + EventSource.GetGuid(typeof(MyLog)));
            Console.WriteLine("GetGuid(explicit): " + EventSource.GetGuid(typeof(GuidOnlyLog)));

            // ── CurrentThreadActivityId: a per-thread Guid slot, and nothing else ──
            Console.WriteLine("activity initial: " + EventSource.CurrentThreadActivityId);
            Guid want = new Guid("aabbccdd-1122-3344-5566-778899aabbcc");
            Guid prev;
            EventSource.SetCurrentThreadActivityId(want, out prev);
            Console.WriteLine("activity prev: " + prev);
            Console.WriteLine("activity set: " + EventSource.CurrentThreadActivityId);
            EventSource.SetCurrentThreadActivityId(Guid.Empty);
            Console.WriteLine("activity cleared: " + EventSource.CurrentThreadActivityId);

            // ── Writes: no-ops that RETURN, on both the non-generic and the
            // self-describing generic overload. Neither throws, in .NET or here.
            MyLog.Log.Write("gate-evt");
            MyLog.Log.Write("gate-evt-payload", new ValueTuple<int>(7));
            Console.WriteLine("writes returned");
        }
    }
}

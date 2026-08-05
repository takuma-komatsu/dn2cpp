using System;
using System.Diagnostics.Tracing;
using System.Globalization;

// THIS PROGRAM IS NOT MEANT TO TRANSPILE. It is the subject of the negative arm at the
// bottom of gates/build-and-run-reflect-types.sh, which asserts that dn2cpp REFUSES it,
// loudly, naming EventListener and a remedy — so it is never compiled to native and its
// output is never diffed against anything.
//
// The refusal is the contract. dn2cpp models EventSource's IDENTITY and its WRITE side for
// real — Name, the SHA-1 name-derived Guid, Settings, the per-thread activity id, and the
// no-op returns of WriteEvent/Write/Write<T> that real .NET with no listener attached also
// performs — and the EventSourceSubset section of this same bucket asserts that half against
// a live `dotnet run` oracle. What it does not model is OBSERVATION: nothing delivers an
// event to a listener. An answered EventSource.IsSupported / AddListener would let the
// listener below compile, link, run and receive nothing forever with no diagnostic, which is
// the load-bearing-consumer failure docs/ARCHITECTURE.md §4-B forbids.
//
// The listener has to be a real subclass rather than a bare `new EventListener()`: the
// refusal is raised inside CoreLib, so the only part of the diagnostic that names the
// program the developer has to change is the reach chain's tail — this file's own subclass
// ctor. The gate asserts that too. Why delivery stays unmodeled rather than becoming another
// intrinsic arm is measured at MethodCompiler.TryEmitEventSourceIntrinsic.
namespace EventListenerProbe
{
    [EventSource(Name = "Dn2Cpp-Gate-ProbeLog")]
    internal sealed class ProbeLog : EventSource
    {
        public static readonly ProbeLog Log = new ProbeLog();

        [Event(1)]
        public void Tick(int n) => WriteEvent(1, n);
    }

    // A listener that consumes its OWN process's events — the shape any library that
    // self-diagnoses through EventSource is built out of.
    internal sealed class ProbeListener : EventListener
    {
        public int Seen;

        protected override void OnEventSourceCreated(EventSource source)
        {
            if (source.Name == "Dn2Cpp-Gate-ProbeLog")
                EnableEvents(source, EventLevel.LogAlways, EventKeywords.All);
        }

        protected override void OnEventWritten(EventWrittenEventArgs eventData)
            => Seen++;
    }

    internal static class Program
    {
        private static int Main()
        {
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
            var listener = new ProbeListener();
            ProbeLog.Log.Tick(7);
            Console.WriteLine("seen=" + listener.Seen);
            return 0;
        }
    }
}

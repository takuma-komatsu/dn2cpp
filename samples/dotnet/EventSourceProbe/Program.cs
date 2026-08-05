using System;
using System.Net;

// The framework EventSource non-void provider fold, driven end to end.
//
// A native build has no EventPipe/ETW/EventListener, so the framework's tracing
// providers are folded to no-ops: the Log singleton is null (its .cctor is
// skipped), and a call on a provider member yields the method's default — the
// value .NET itself returns when no listener is attached. The VOID event methods
// discard their args; the BOOL/scope-id members yield false/0; and a member that
// returns a struct or a reference yields a zeroed struct / null.
//
// Dns.GetHostName's real body opens a name-resolution telemetry scope through
// NameResolutionTelemetry.Log.BeforeResolution(...), whose return is a
// NameResolutionActivity STRUCT. That is the risky shape: the fold pushes a
// zeroed struct, whose C++ layout must be emitted and whose address the caller
// takes (stloc/ldloca) for the paired AfterResolution/LogFailure — both of which
// accept the zeroed activity, exactly as with no listener attached. Driving the
// real call proves the non-void (struct) fold compiles and runs.
//
// The hostname primitive under Dns.GetHostName differs per OS and both arms are
// runtime-provided (see build-and-run-eventsource.sh): libSystem.Native's
// SystemNative_GetHostName on POSIX, ws2_32 winsock's gethostname on Windows.
// The host name is machine-specific, so only its non-emptiness is asserted,
// never its value.
internal static class Program
{
    private static void Main()
    {
        string host = Dns.GetHostName();
        Console.WriteLine("GetHostName non-empty: " + (host.Length > 0));
        Console.WriteLine("GetHostName is string: " + (host is string));
    }
}

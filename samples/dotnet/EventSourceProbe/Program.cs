using System;
using System.Diagnostics.Tracing;
using System.Globalization;
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
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;

        string host = Dns.GetHostName();
        Console.WriteLine("GetHostName non-empty: " + (host.Length > 0));
        Console.WriteLine("GetHostName is string: " + (host is string));

        var provider = new ProbeEventSource();
        Console.WriteLine("direct: " + provider.ToString());
        EventSource asBase = provider;
        Console.WriteLine("base: " + asBase.ToString());
        object asObject = provider;
        Console.WriteLine("object: " + asObject.ToString());

        // Unix IPAddress parsing maps a named IPv6 scope through
        // SystemNative_InterfaceNameToIndex. Loopback's name is stable on the
        // POSIX hosts this gate covers. Windows exercises its separate Winsock
        // PAL with a deliberately absent name, whose stable index is zero.
        bool windows = OperatingSystem.IsWindows();
        string scopeName = windows
            ? "dn2cpp-no-such-interface"
            : OperatingSystem.IsMacOS() || OperatingSystem.IsFreeBSD() ? "lo0" : "lo";
        long first = IPAddress.Parse("fe80::1%" + scopeName).ScopeId;
        long second = IPAddress.Parse("fe80::1%" + scopeName).ScopeId;
        Console.WriteLine("named IPv6 scope result valid: " + (windows ? first == 0 : first > 0));
        Console.WriteLine("named IPv6 scope stable: " + (first == second));
    }
}

[EventSource(Name = "Dn2Cpp.Probe", Guid = "11111111-2222-3333-4444-555555555555")]
internal sealed class ProbeEventSource : EventSource
{
}

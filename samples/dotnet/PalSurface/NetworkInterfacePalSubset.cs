using System;
using System.Net;

namespace NetworkInterfacePalSubset
{
    // Named IPv6 scopes reach Interop.Sys.InterfaceNameToIndex through the real
    // System.Net.Primitives body. A browser exposes no host interface namespace,
    // so the wasm PAL must return the same zero an unknown POSIX interface returns.
    internal static class Program
    {
        internal static void __GateEntry()
        {
            Console.WriteLine("-- NetworkInterfacePalSubset --");
            long scope = IPAddress.Parse("fe80::1%dn2cpp-no-such-interface").ScopeId;
            Console.WriteLine($"unknownScope={scope}");
        }
    }
}

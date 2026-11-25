using System;
using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;

namespace PInvokeFramework;

internal static class Program
{
    private static void Main()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        Console.WriteLine("core-foundation=" + (CFStringGetTypeID() != 0));

        var handleType = Type.GetType("Interop+SafeNwHandle, System.Net.Security", throwOnError: true)!;
        var constructor = handleType.GetConstructor(new[] { typeof(nint), typeof(bool) })!;
        var network = Type.GetType("Interop+NetworkFramework, System.Net.Security", throwOnError: true)!;
        var retain = network.GetMethod("Retain", BindingFlags.NonPublic | BindingFlags.Static)!;
        nint endpoint = CreateEndpoint("localhost", "443");
        if (endpoint == 0)
            throw new InvalidOperationException("Network.framework did not create an endpoint.");
        nint retained = (nint)retain.Invoke(null, new object[] { endpoint })!;
        var first = (SafeHandle)constructor.Invoke(new object[] { endpoint, true });
        var second = (SafeHandle)constructor.Invoke(new object[] { retained, true });
        first.Dispose();
        second.Dispose();
        Console.WriteLine("network-ownership=" + (retained == endpoint && first.IsInvalid && second.IsInvalid));
    }

    [DllImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
    private static extern nuint CFStringGetTypeID();

    [DllImport("/System/Library/Frameworks/Network.framework/Network", EntryPoint = "nw_endpoint_create_host")]
    private static extern nint CreateEndpoint(string hostname, string port);
}

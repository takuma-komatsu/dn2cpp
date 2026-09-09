using System;
using System.Globalization;
using System.Runtime.InteropServices;

namespace PInvokeFramework;

internal static class Program
{
    private static void Main()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        Console.WriteLine("core-foundation=" + (CFStringGetTypeID() != 0));
    }

    [DllImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
    private static extern nuint CFStringGetTypeID();
}

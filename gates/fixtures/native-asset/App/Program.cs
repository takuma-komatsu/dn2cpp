using System;
using System.Globalization;
using System.Runtime.InteropServices;

CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;

#if NATIVE_ASSET_SHARED
Console.WriteLine($"native-asset={NativeMethods.Answer()},shared-asset={NativeMethods.SharedAnswer()}");
#else
Console.WriteLine($"native-asset={NativeMethods.Answer()}");
#endif

internal static partial class NativeMethods
{
    [DllImport("dn2cpp_native_asset", EntryPoint = "dn2cpp_native_asset_answer")]
    internal static extern int Answer();

#if NATIVE_ASSET_SHARED
    [DllImport("dn2cpp_shared_asset", EntryPoint = "dn2cpp_shared_asset_answer")]
    internal static extern int SharedAnswer();
#endif
}

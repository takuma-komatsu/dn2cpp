using System;
using System.Globalization;

namespace GcNotificationSubset;

internal static class Program
{
    private static void Error(string label, Action action)
    {
        try { action(); Console.WriteLine(label + ":accepted"); }
        catch (Exception error)
        {
            Console.WriteLine(label + ":" + error.GetType().Name + ":"
                + (error is ArgumentException argument ? argument.ParamName : "") + ":" + error.Message);
        }
    }
    private static void WithInvariantCulture(Action body)
    {
        var culture = CultureInfo.CurrentCulture;
        var uiCulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
            body();
        }
        finally
        {
            CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = uiCulture;
        }
    }
    internal static void Run() => WithInvariantCulture(RunValidation);
    private static void RunValidation()
    {
        Console.WriteLine("== GC notification validation ==");
        Action<int, int> register = GC.RegisterForFullGCNotification;
        Error("fullgc-max-zero", () => register(0, 50));
        Error("fullgc-max-hundred", () => register(100, 50));
        Error("fullgc-loh-zero", () => register(50, 0));
        Error("fullgc-loh-hundred", () => register(50, 100));
        Error("fullgc-max-first", () => register(0, 0));
        Error("fullgc-approach-negative", () => GC.WaitForFullGCApproach(-2));
        Error("fullgc-complete-negative", () => GC.WaitForFullGCComplete(-2));
        Error("fullgc-approach-timespan", () => GC.WaitForFullGCApproach(TimeSpan.FromMilliseconds(-2)));
        Error("fullgc-complete-timespan", () => GC.WaitForFullGCComplete(TimeSpan.FromMilliseconds(-2)));
        Error("fullgc-approach-overflow", () => GC.WaitForFullGCApproach(TimeSpan.FromMilliseconds((long)int.MaxValue + 1)));
        Error("fullgc-complete-overflow", () => GC.WaitForFullGCComplete(TimeSpan.FromMilliseconds((long)int.MaxValue + 1)));
        Console.WriteLine("GC notification validation end");
    }
    // This capability contract is native-only. CLR can successfully register;
    // its success/events are not normalized into dn2cpp's unavailable feature.
    internal static void RunNativeUnavailable() => WithInvariantCulture(RunNativeUnavailableCore);
    private static unsafe void RunNativeUnavailableCore()
    {
        Action<int, int> register = GC.RegisterForFullGCNotification;
        delegate* managed<int, int, void> registerPointer = &GC.RegisterForFullGCNotification;
        Error("fullgc-register-direct", () => GC.RegisterForFullGCNotification(50, 50));
        Error("fullgc-register-delegate", () => register(1, 99));
        try { registerPointer(99, 1); Console.WriteLine("fullgc-register-pointer:accepted"); }
        catch (Exception error) { Console.WriteLine("fullgc-register-pointer:" + error.GetType().Name + "::" + error.Message); }
        Action cancel = GC.CancelFullGCNotification;
        Error("fullgc-cancel-direct", () => GC.CancelFullGCNotification());
        Error("fullgc-cancel-delegate", () => cancel());
        Func<int, GCNotificationStatus> approach = GC.WaitForFullGCApproach;
        Func<int, GCNotificationStatus> complete = GC.WaitForFullGCComplete;
        delegate* managed<int, GCNotificationStatus> pointer = &GC.WaitForFullGCApproach;
        Console.WriteLine("fullgc-approach-default:" + GC.WaitForFullGCApproach());
        Console.WriteLine("fullgc-complete-default:" + GC.WaitForFullGCComplete());
        Console.WriteLine("fullgc-approach-zero:" + approach(0));
        Console.WriteLine("fullgc-complete-positive:" + complete(5));
        Console.WriteLine("fullgc-approach-pointer:" + pointer(0));
        Console.WriteLine("fullgc-approach-time:" + GC.WaitForFullGCApproach(TimeSpan.Zero));
        Console.WriteLine("fullgc-complete-time:" + GC.WaitForFullGCComplete(TimeSpan.Zero));
        Console.WriteLine("fullgc-approach-fraction:" + GC.WaitForFullGCApproach(TimeSpan.FromTicks(-19999)));
        Console.WriteLine("fullgc-complete-upper:" + GC.WaitForFullGCComplete(TimeSpan.FromTicks((long)int.MaxValue * 10000 + 9999)));
        Console.WriteLine("GC notification unavailable end");
    }
}

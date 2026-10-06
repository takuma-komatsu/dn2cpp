using System;
using System.Globalization;

namespace ReflectNameBindOnly;

internal sealed class Subject
{
    private int Secret(int value) => value + 1;
    private static int Twice(int value) => value * 2;
}

internal static class Program
{
    private static void Main()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;

        Console.WriteLine("== delegate names as the only reflection entry ==");
        Subject target = new Subject();
        Type delegateType = typeof(Func<int, int>);
        var instance = (Func<int, int>)Delegate.CreateDelegate(delegateType, target, "Secret");
        var instanceCase = (Func<int, int>)Delegate.CreateDelegate(delegateType, target, "secret", true);
        var instanceSoft = (Func<int, int>)Delegate.CreateDelegate(delegateType, target, "Secret", false, false)!;
        Console.WriteLine($"instance names: {instance(41)}/{instanceCase(41)}/{instanceSoft(41)}");
        var stat = (Func<int, int>)Delegate.CreateDelegate(delegateType, typeof(Subject), "Twice");
        var statCase = (Func<int, int>)Delegate.CreateDelegate(delegateType, typeof(Subject), "twice", true);
        var statSoft = (Func<int, int>)Delegate.CreateDelegate(delegateType, typeof(Subject), "Twice", false, false)!;
        Console.WriteLine($"static names: {stat(21)}/{statCase(21)}/{statSoft(21)}");
        Console.WriteLine("delegate names only end");
    }
}

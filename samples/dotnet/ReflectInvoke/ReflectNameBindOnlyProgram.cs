using System;
using System.Globalization;
using System.Threading;

namespace ReflectNameBindOnly;

internal sealed class Subject
{
    private int Secret(int value) => value + 1;
    private static int Twice(int value) => value * 2;
}

internal unsafe delegate int FunctionProbe(delegate*<int, int> value);
internal unsafe delegate int TokenProbe(delegate*<CancellationToken, int> value);
internal unsafe class FunctionOverload<T>
{
    public static int Ref(delegate*<int, int> value) => 181;
    public static int Ref(delegate*<T, int> value) => 182;
}
internal unsafe class TokenOverload<T>
{
    public static int Ref(delegate*<CancellationToken, int> value) => 191;
    public static int Ref(delegate*<T, int> value) => 192;
}

internal static class Program
{
    private static unsafe int FunctionCall(Type owner, bool hard)
    {
        var target = (FunctionProbe)Delegate.CreateDelegate(typeof(FunctionProbe), owner, "Ref", false, hard)!;
        return target((delegate*<int, int>)(nint)256);
    }

    private static unsafe int TokenCall(Type owner, bool hard)
    {
        var target = (TokenProbe)Delegate.CreateDelegate(typeof(TokenProbe), owner, "Ref", false, hard)!;
        return target((delegate*<CancellationToken, int>)(nint)512);
    }

    private static void Main(string[] args)
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;

        if (args.Length > 0 && args[0] == "intrinsic-boundary")
        {
            Type owner = typeof(TokenOverload<>).MakeGenericType(typeof(CancellationToken));
            Console.WriteLine("== matching intrinsic overload boundary ==");
            foreach (bool hard in new bool[] { false, true })
            {
                string outcome;
                try
                {
                    outcome = Delegate.CreateDelegate(typeof(TokenProbe), owner, "Ref", false, hard) is null ? "null" : "bound";
                }
                catch (PlatformNotSupportedException)
                {
                    outcome = "unsupported";
                }
                Console.WriteLine("intrinsic coincident " + (hard ? "hard" : "soft") + " => " + outcome);
            }
            Console.WriteLine("matching intrinsic overload boundary end");
            return;
        }

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
        if (args.Length > 0 && args[0] == "before-intrinsic-overloads")
            return;
        Console.WriteLine("== intrinsic identity overload selection ==");
        Type tokenArgument = typeof(FunctionOverload<>).MakeGenericType(typeof(CancellationToken));
        Console.WriteLine($"intrinsic argument => {FunctionCall(tokenArgument, false)}/{FunctionCall(tokenArgument, true)}");
        Type tokenGuid = typeof(TokenOverload<>).MakeGenericType(typeof(Guid));
        Type tokenInt = typeof(TokenOverload<>).MakeGenericType(typeof(int));
        Console.WriteLine($"intrinsic leaf => {TokenCall(tokenGuid, false)}/{TokenCall(tokenGuid, true)}/{TokenCall(tokenInt, false)}/{TokenCall(tokenInt, true)}");
        Console.WriteLine("intrinsic identity overload selection end");
    }
}

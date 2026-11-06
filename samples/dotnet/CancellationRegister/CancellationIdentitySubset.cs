using System;
using System.Threading;

namespace CancellationRegister;

internal static class CancellationIdentitySubset
{
    private static int _arguments;

    internal static void Run()
    {
        Console.WriteLine("== Cancellation identity and delegate faults ==");
        var first = new CancellationToken(true);
        var second = new CancellationToken(true);
        Console.WriteLine("canceled identity=" + first.Equals(second)
            + ":" + (first.GetHashCode() == second.GetHashCode()));
        Console.WriteLine("default identity=" + new CancellationToken(false).Equals(default(CancellationToken)));
        using var source = new CancellationTokenSource();
        source.Cancel();
        Console.WriteLine("source identity=" + source.Token.Equals(source.Token)
            + ":" + source.Token.Equals(first));
        int callbacks = 0;
        first.Register(() => callbacks++).Dispose();
        second.Register(() => callbacks++).Dispose();
        Console.WriteLine("canceled callbacks=" + callbacks);

        Action<int> missing = null!;
        try
        {
            missing(Argument());
            Console.WriteLine("delegate null=missing fault");
        }
        catch (NullReferenceException)
        {
            Console.WriteLine("delegate null=NullReferenceException arguments=" + _arguments);
        }
        try
        {
            missing(ThrowArgument());
            Console.WriteLine("delegate argument=missing fault");
        }
        catch (InvalidOperationException)
        {
            Console.WriteLine("delegate argument=InvalidOperationException");
        }
        Console.WriteLine("Cancellation identity and delegate faults end");
    }

    private static int Argument() => ++_arguments;

    private static int ThrowArgument() => throw new InvalidOperationException();
}

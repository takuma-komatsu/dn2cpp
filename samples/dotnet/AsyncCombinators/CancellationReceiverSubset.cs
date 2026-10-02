using System;
using System.Threading;

namespace CancellationReceiverSubset;

internal static class Program
{
    private static CancellationTokenSource s_source = null!;

    private static void Fault(string label, Action action)
    {
        string result = "returned";
        try
        {
            action();
        }
        catch (NullReferenceException)
        {
            result = nameof(NullReferenceException);
        }
        Console.WriteLine(label + "=" + result);
    }

    internal static void Run()
    {
        Console.WriteLine("== cancellation source receivers ==");
        Fault("Token", () => _ = s_source.Token);
        Fault("IsCancellationRequested", () => _ = s_source.IsCancellationRequested);
        Fault("Cancel", () => s_source.Cancel());
        Fault("Cancel(false)", () => s_source.Cancel(false));
        Fault("Cancel(true)", () => s_source.Cancel(true));
        Fault("Dispose", () => s_source.Dispose());
        using var source = new CancellationTokenSource();
        CancellationToken token = source.Token;
        Console.WriteLine("live before=" + source.IsCancellationRequested + "/" + token.IsCancellationRequested);
        source.Cancel();
        Console.WriteLine("live after=" + source.IsCancellationRequested + "/" + token.IsCancellationRequested);
        using var falseSource = new CancellationTokenSource();
        CancellationToken falseToken = falseSource.Token;
        falseSource.Cancel(false);
        Console.WriteLine("live Cancel(false)=" + falseSource.IsCancellationRequested + "/" + falseToken.IsCancellationRequested);
        using var trueSource = new CancellationTokenSource();
        CancellationToken trueToken = trueSource.Token;
        trueSource.Cancel(true);
        Console.WriteLine("live Cancel(true)=" + trueSource.IsCancellationRequested + "/" + trueToken.IsCancellationRequested);
        Console.WriteLine("cancellation source receivers end");
    }
}

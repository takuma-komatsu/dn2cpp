using System;
using System.Globalization;
using System.Reflection;

namespace AppContextExitSubset;

internal static class Program
{
    private static void Direct() => throw new InvalidOperationException("fixture missing");
    private static void Pointer() => throw new InvalidOperationException("fixture missing");
    private static Action Address() => throw new InvalidOperationException("fixture missing");
    private static void DelegateRoute() => Address()();
    private static void Reflect() => Address().Method.Invoke(null, null);

    internal static void Run()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        Console.WriteLine("== AppContext public neighbor ==");
        Console.WriteLine("appcontext-public-base-directory:" + !string.IsNullOrEmpty(AppContext.BaseDirectory));
        Console.WriteLine("AppContext public neighbor end");
    }

    private static void Check(string route, Action call)
    {
        bool ok = false;
        try { call(); }
        catch (Exception error)
        {
            bool wrapped = error is TargetInvocationException;
            Exception leaf = wrapped ? error.InnerException : error;
            ok = wrapped == (route == "reflection") && leaf is PlatformNotSupportedException
                && leaf.Message == "AppContext process-exit orchestration is not supported";
        }
        Console.WriteLine("appcontext-exit-" + route + ":" + ok);
    }

    internal static void RunArtifactChecks(string[] args)
    {
        Run();
        Check("direct", Direct);
        Check("calli", Pointer);
        Check("delegate", DelegateRoute);
        Check("reflection", Reflect);
        Console.WriteLine("appcontext-exit-continued:True");
    }

    internal static void RunReflectionChecks(string[] args)
    {
        Run();
        Check("reflection", Reflect);
        Console.WriteLine("appcontext-exit-continued:True");
    }
}

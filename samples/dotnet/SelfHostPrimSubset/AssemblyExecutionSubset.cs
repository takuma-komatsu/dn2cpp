using System;
using System.Globalization;
using System.Reflection;

namespace AssemblyExecutionSubset;

internal static class Program
{
    private static int Evaluations;
    private static int Direct(Assembly assembly, string[] args) => throw new InvalidOperationException("fixture missing");
    private static int Pointer(Assembly assembly, string[] args) => throw new InvalidOperationException("fixture missing");
    private static Func<Assembly, string[], int> Address() => throw new InvalidOperationException("fixture missing");
    private static int DelegateRoute(Assembly assembly, string[] args) => Address()(assembly, args);
    private static int Reflect(Assembly assembly, string[] args) => (int)Address().Method.Invoke(null, new object[] { assembly, args });
    private static Assembly AssemblyArgument(Assembly assembly)
    {
        Evaluations = Evaluations * 10 + 1;
        return assembly;
    }

    private static string[] Arguments(bool nonnull)
    {
        Evaluations = Evaluations * 10 + 2;
        return nonnull ? new[] { "untouched" } : null;
    }

    private static string[] ThrowingArguments(bool nonnull)
    {
        Evaluations = Evaluations * 10 + 2;
        throw new FormatException("argument producer");
    }

    private static int ProducerRoute(Assembly assembly, bool nonnull, bool throwArgs)
        => throw new InvalidOperationException("fixture missing");

    private static void ProducerChecks()
    {
        foreach (Assembly assembly in new[] { null, typeof(Program).Assembly })
        {
            foreach (bool nonnull in new[] { false, true })
            {
                foreach (bool throwArgs in new[] { false, true })
                {
                    Evaluations = 0;
                    bool ok = false;
                    try { ProducerRoute(assembly, nonnull, throwArgs); }
                    catch (Exception error)
                    {
                        ok = throwArgs ? error is FormatException && error.Message == "argument producer"
                            : assembly == null ? error is NullReferenceException
                            : error is PlatformNotSupportedException
                                && error.Message == "AppDomain assembly entrypoint execution is not supported";
                    }
                    Console.WriteLine("assembly-execution-producers-" + (assembly == null ? "null" : "app")
                        + "-" + nonnull + "-" + throwArgs + ":" + (ok && Evaluations == 12));
                }
            }
        }
    }


    internal static void Run()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        Console.WriteLine("== Assembly execution public neighbor ==");
        Assembly linked = typeof(Program).Assembly;
        Console.WriteLine("assembly-execution-public-linked:" + (Assembly.Load(linked.GetName().Name) == linked));
        bool nullName = false;
        try { Assembly.Load((string)null); }
        catch (ArgumentNullException) { nullName = true; }
        Console.WriteLine("assembly-execution-public-null-name:" + nullName);
        Console.WriteLine("Assembly execution public neighbor end");
    }

    private static void Check(string route, Func<Assembly, string[], int> call)
    {
        foreach (Assembly assembly in new[] { null, typeof(Program).Assembly, typeof(object).Assembly })
        {
            for (int state = 0; state < 2; state++)
            {
                Evaluations = 0;
                bool ok = false;
                try { call(AssemblyArgument(assembly), Arguments(state != 0)); }
                catch (Exception error)
                {
                    bool wrapped = error is TargetInvocationException;
                    Exception leaf = wrapped ? error.InnerException : error;
                    ok = wrapped == (route == "reflection") && (assembly == null
                        ? leaf is NullReferenceException
                        : leaf is PlatformNotSupportedException && leaf.Message == "AppDomain assembly entrypoint execution is not supported");
                }
                Console.WriteLine("assembly-execution-" + route + "-" + (assembly == null ? "null" : assembly == typeof(Program).Assembly ? "app" : "core")
                    + "-" + state + ":" + (ok && Evaluations == 12));
            }
        }
    }

    internal static void RunArtifactChecks(string[] args)
    {
        Run();
        Check("direct", Direct);
        Check("calli", Pointer);
        Check("delegate", DelegateRoute);
        Check("reflection", Reflect);
        ProducerChecks();
    }

    internal static void RunReflectionChecks(string[] args)
    {
        Run();
        Check("reflection", Reflect);
    }
}

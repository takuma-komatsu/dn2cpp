using System;
using System.Globalization;

// Environment.FailFast halts the process IMMEDIATELY and uncatchably: it is not an
// exception, so there is no unwinding, no catch, no finally and no exit code to
// negotiate. Real .NET writes its report to stderr and aborts (SIGABRT — the shell
// sees 134 on Unix), exactly as an exception escaping Main does (UnhandledExitSubset).
//
// The whole point of the gate is that this must NOT degrade into a no-op: a program
// calling FailFast to halt on corrupted state and then merrily continuing is a
// correctness hole, not a diagnostic one. Every line below tagged BUG: is a witness —
// none of them may ever print, on either runtime.
//
// Both overloads of the family are exercised, ONE PER RUN (a process can only fail
// fast once), selected by an argument the gate passes. Everything the two runtimes
// print on stdout before the abort must match, and so must the exit status; stderr —
// where the two runtimes' report wording differs — stays off the diff.
internal static class Program
{
    static void Main(string[] args)
    {
        // Pin both cultures first: gate output must not depend on the host locale (see AGENTS.md).
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;

        Console.WriteLine("before failfast");
        Fail(args);
        Console.WriteLine("BUG: execution continued past FailFast");
    }

    // Not marked [DoesNotReturn], so the caller's flow analysis (and ours) treats the
    // line after it as live — which is exactly what makes it a witness.
    private static void Fail(string[] args)
    {
        try
        {
            if (args.Length > 0)
                Environment.FailFast("halting: " + args[0]);          // FailFast(string)
            else
                Environment.FailFast("halting on corrupted state",    // FailFast(string, Exception)
                    new InvalidOperationException("the cause"));
        }
        catch (Exception e)
        {
            Console.WriteLine("BUG: FailFast was catchable: " + e.Message);
        }
        finally
        {
            Console.WriteLine("BUG: a finally ran after FailFast");
        }
    }
}

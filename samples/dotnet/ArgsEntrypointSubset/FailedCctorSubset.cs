using System;

namespace FailedCctorSubset;

// A type whose static initializer throws. .NET never marks such a type initialized:
// it wraps the failure in a TypeInitializationException, CACHES it, and rethrows the
// cached instance on every later touch, so nobody ever reads a static the initializer
// did not get to assign. dn2cpp used to latch its per-cctor done flag on a throwing
// body, which is the opposite — the first touch threw, and every touch after it was a
// no-op that handed back whatever the half-run initializer had left. `Second` below is
// what makes that lethal rather than merely wrong: it is assigned after the throw, so
// it is null forever, and a call on it is a null dereference three frames from anything
// that mentions a static constructor. (Real instance: AngleSharp's TextEncoding..cctor
// catches Encoding.GetEncoding(1252) failing inside BaseCodePageEncoding..cctor — dn2cpp
// cuts the manifest-resource read that initializer needs — and returns a UTF-8 fallback;
// the SECOND GetEncoding then took the process down with SIGSEGV.)
internal static class Broken
{
    internal static readonly string First = "first";
    internal static readonly string Second = Boom();

    private static string Boom() => throw new InvalidOperationException("initializer failed on purpose");
}

// The catch-and-fall-back caller — the AngleSharp shape. Its own initializer survives
// Broken's failure, so it IS initialized; only Broken must stay unusable. It records
// rather than prints, because the two runtimes run it at different times (dn2cpp's
// eager startup pass vs .NET's first touch) and printing would order the output
// differently on each while saying nothing extra.
internal static class Dependent
{
    internal static readonly string Fallback = Probe();

    private static string Probe()
    {
        try
        {
            return Broken.Second;
        }
        catch (Exception ex)
        {
            return "fallback:" + Program.Describe(ex);
        }
    }
}

internal static class Program
{
    // The ONE declared divergence this section normalizes: .NET reports a failed type
    // initializer as TypeInitializationException wrapping the original; dn2cpp models no
    // TypeInitializationException and re-raises the original itself. Unwrapping compares
    // what both runtimes agree on — which failure a touch reports — and leaves everything
    // else (that it throws at all, that it throws EVERY time, that it is the same object
    // every time, that the process survives) diffed exactly against real .NET.
    internal static string Describe(Exception ex)
    {
        Exception root = Unwrap(ex);
        return root.GetType().Name + "/" + root.Message;
    }

    private static Exception Unwrap(Exception ex)
    {
        while (ex is TypeInitializationException && ex.InnerException is not null)
            ex = ex.InnerException;
        return ex;
    }

    internal static void Run()
    {
        // Touch the broken type three times. Every touch must throw — the bug was that
        // only the first did, and the ones after it returned the unassigned static.
        Exception? firstSeen = null;
        bool sameEveryTime = true;
        for (int i = 0; i < 3; i++)
        {
            try
            {
                Console.WriteLine($"failedCctor.touch{i}=returned:{Broken.Second}");
            }
            catch (Exception ex)
            {
                Exception root = Unwrap(ex);
                Console.WriteLine($"failedCctor.touch{i}=threw:{Describe(ex)}");
                if (firstSeen is null)
                    firstSeen = root;
                else if (!ReferenceEquals(firstSeen, root))
                    sameEveryTime = false;
            }
        }

        // A remembered failure, not a re-run one: .NET rethrows its cached instance, and
        // dn2cpp re-raises the recorded object. Either way the identity is stable, which
        // is the difference between "the type is permanently failed" and "the initializer
        // runs again each time" — the second would also re-run its side effects.
        Console.WriteLine($"failedCctor.sameExceptionEachTouch={sameEveryTime}");

        // The static assigned BEFORE the throw is still not readable: the type as a whole
        // is uninitialized, not "initialized up to the throw".
        try
        {
            Console.WriteLine($"failedCctor.firstField=returned:{Broken.First}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"failedCctor.firstField=threw:{Describe(ex)}");
        }

        // The catching caller survives and stays usable — a failed initializer must not
        // take down the types around it, nor the process.
        Console.WriteLine($"failedCctor.dependent={Dependent.Fallback}");
        Console.WriteLine("failedCctor.survived=True");
    }
}

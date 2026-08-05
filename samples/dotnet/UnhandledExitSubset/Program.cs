using System;
using System.Globalization;

// An exception escaping Main terminates the process ABNORMALLY in real .NET —
// report on stderr, then abort (SIGABRT, 134 on Unix), not a clean nonzero
// exit. The generated main's catch funnel must match, and stdout written before
// the throw must survive on both sides (stderr wording differs, so it is off
// the diff).
internal static class Program
{
    static void Main()
    {
        // Pin both cultures first: gate output must not depend on the host locale (see AGENTS.md).
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;

        Console.WriteLine("before throw");
        throw new InvalidOperationException("escaped Main");
    }
}

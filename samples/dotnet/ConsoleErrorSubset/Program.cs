using System;
using System.Globalization;

// Console.Error — the stderr TextWriter.
//
// System.Console is an intrinsic type, so `Console.Error` (get_Error) is lowered to the
// runtime stderr writer (a Dn2CppTextWriter*); its Write/WriteLine are routed to the
// dn2cpp_textwriter_* family, which mirror the stdout dn2cpp_console_* helpers byte-for-byte
// but write to stderr. This closes the last own-code console self-host gap — dn2cpp's own
// TranspileDriver.Run reports errors via `Console.Error.WriteLine(...)`.
//
// The *direct fluent* `Console.Error.X(...)` form (no TextWriter local) takes a
// receiver-sensitive fast path straight to the dn2cpp_textwriter_* helpers. A *stored /
// spilled* receiver (a `System.IO.TextWriter` local) instead dispatches a real
// callvirt; because `Console.Error` is now a real managed TextWriter subtype
// (Dn2Cpp.Runtime.Dn2CppConsoleWriter) its vtable routes that callvirt to the same stderr
// helpers, so both forms produce identical output. The sample exercises both, plus every
// value overload, and also writes a stdout marker so the gate can confirm the two streams
// stay separate — then exact-diffs both STDOUT and STDERR against real .NET.
public static class Program
{
    // Regression driver. Passing Console.Error as a `System.IO.TextWriter` parameter
    // gives these calls a base-typed receiver, so `w.Write/WriteLine` dispatch a real
    // callvirt on the writer object — the spilled/stored-receiver path, never the
    // fluent fast path. A method parameter can't be copy-propagated to the caller's precise
    // type, so this reproduces the bug deterministically. Before Fix B the callvirt hit the
    // header-less runtime writer and SIGSEGV'd; now Console.Error is a real managed
    // TextWriter subtype whose vtable routes the callvirt to the same stderr helpers, so the
    // output is byte-identical to the fluent form. Interpolation feeds simple non-negative
    // int + plain string locals (deterministic; no culture-dependent formatting).
    private static void WriteErrVia(System.IO.TextWriter w, int code, string label)
    {
        w.WriteLine($"E:interp line code={code} label={label}"); // callvirt WriteLine(string)
        w.Write($"E:interp write code={code}|");                 // callvirt Write(string)
        w.WriteLine();                                           // callvirt WriteLine()
    }

    public static int Main()
    {
        // Pin both cultures first: gate output must not depend on the host locale (see AGENTS.md).
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;

        // stdout: Console.WriteLine / Console.Write keep going to stdout.
        Console.WriteLine("STDOUT: via Console.WriteLine");
        Console.Write("STDOUT: ");
        Console.WriteLine("via Console.Write+WriteLine");

        // stderr: every overload reached through the modeled TextWriter.
        Console.Error.Write("E:write-no-newline|");   // Write(string), no newline
        Console.Error.WriteLine("E:writeline-string"); // WriteLine(string)
        Console.Error.WriteLine();                      // WriteLine -> bare newline
        Console.Error.WriteLine((string)null);          // null string -> just a newline
        Console.Error.Write((string)null);              // null string -> nothing
        Console.Error.WriteLine(42);                    // int
        Console.Error.WriteLine(-7);                    // negative int
        Console.Error.WriteLine(9000000000L);           // long (> int range)
        Console.Error.WriteLine(true);                  // bool -> True
        Console.Error.WriteLine(false);                 // bool -> False
        Console.Error.Write('X');                       // char (no newline)
        Console.Error.WriteLine();                      // close the char line
        Console.Error.WriteLine(3.5);                   // double
        Console.Error.WriteLine("café-é (utf-8)");      // multibyte UTF-8
        Console.Error.WriteLine((object)123);           // object -> ToString

        // Drive Console.Error through a base-typed TextWriter parameter (a real
        // callvirt on the writer object — the path that used to SIGSEGV on the header-less
        // runtime writer).
        int code = 7;
        string label = "spill";
        WriteErrVia(Console.Error, code, label);
        // The fluent interpolated form still takes the fast path (its argument pre-builds);
        // kept for coverage that interpolation + fast path agree with real .NET.
        Console.Error.WriteLine($"E:interp fluent code={code}");
        Console.WriteLine($"STDOUT: interp contrast code={code}");  // stdout contrast (own intrinsic path)

        Console.Error.WriteLine("E:last");              // final line
        return 0;
    }
}

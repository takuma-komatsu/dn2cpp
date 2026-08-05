using System;
using System.IO;
using System.Text;

namespace Dn2Cpp.Runtime;

/// <summary>The real managed object dn2cpp returns from <c>Console.Error</c>.
/// <c>System.Console</c> is an intrinsic type, so <c>get_Error</c> is lowered to a cached
/// singleton of this <see cref="TextWriter"/> subtype rather than the header-less runtime
/// writer struct: a genuine managed object with a type header and vtable, so a
/// <c>callvirt</c> dispatches correctly even when Roslyn spills the receiver into a
/// <c>System.IO.TextWriter</c> local (interpolated strings). Both routes end in the same
/// <c>dn2cpp_textwriter_*</c> stderr helpers, so the output is byte-identical.
///
/// The overrides funnel through <see cref="ConsoleRuntime"/>, whose bodies never run: the
/// transpiler maps that type to runtime intrinsics (same convention as
/// <see cref="HotUpdate"/>). Under a real .NET runtime this writer is inert.</summary>
internal sealed class Dn2CppConsoleWriter : TextWriter
{
    public override Encoding Encoding => Encoding.UTF8;

    public override void Write(char value) => ConsoleRuntime.ErrWrite(value);

    public override void Write(string? value) => ConsoleRuntime.ErrWrite(value);

    public override void WriteLine() => ConsoleRuntime.ErrWriteLine();

    public override void WriteLine(string? value) => ConsoleRuntime.ErrWriteLine(value);

    public override void WriteLine(int value) => ConsoleRuntime.ErrWriteLine(value);

    public override void WriteLine(long value) => ConsoleRuntime.ErrWriteLine(value);

    public override void WriteLine(double value) => ConsoleRuntime.ErrWriteLine(value);

    public override void WriteLine(bool value) => ConsoleRuntime.ErrWriteLine(value);
}

/// <summary>The stderr-write surface <see cref="Dn2CppConsoleWriter"/>'s overrides funnel
/// into. The transpiler intercepts every call by declaring-type name and emits the
/// matching <c>dn2cpp_textwriter_*(dn2cpp_console_error(), …)</c> helper, so the bodies
/// below never execute — they throw the same placeholder as <see cref="HotUpdate"/>.</summary>
internal static class ConsoleRuntime
{
    public static void ErrWrite(char value) =>
        throw new NotSupportedException("Dn2Cpp.Runtime.ConsoleRuntime.ErrWrite is a dn2cpp intrinsic (no managed implementation)");

    public static void ErrWrite(string? value) =>
        throw new NotSupportedException("Dn2Cpp.Runtime.ConsoleRuntime.ErrWrite is a dn2cpp intrinsic (no managed implementation)");

    public static void ErrWriteLine() =>
        throw new NotSupportedException("Dn2Cpp.Runtime.ConsoleRuntime.ErrWriteLine is a dn2cpp intrinsic (no managed implementation)");

    public static void ErrWriteLine(string? value) =>
        throw new NotSupportedException("Dn2Cpp.Runtime.ConsoleRuntime.ErrWriteLine is a dn2cpp intrinsic (no managed implementation)");

    public static void ErrWriteLine(int value) =>
        throw new NotSupportedException("Dn2Cpp.Runtime.ConsoleRuntime.ErrWriteLine is a dn2cpp intrinsic (no managed implementation)");

    public static void ErrWriteLine(long value) =>
        throw new NotSupportedException("Dn2Cpp.Runtime.ConsoleRuntime.ErrWriteLine is a dn2cpp intrinsic (no managed implementation)");

    public static void ErrWriteLine(double value) =>
        throw new NotSupportedException("Dn2Cpp.Runtime.ConsoleRuntime.ErrWriteLine is a dn2cpp intrinsic (no managed implementation)");

    public static void ErrWriteLine(bool value) =>
        throw new NotSupportedException("Dn2Cpp.Runtime.ConsoleRuntime.ErrWriteLine is a dn2cpp intrinsic (no managed implementation)");
}

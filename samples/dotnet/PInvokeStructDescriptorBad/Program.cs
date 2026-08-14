using System;
using System.Runtime.InteropServices;

namespace PInvokeStructDescriptorBad;

/// <summary>Refusal subject: a struct field carrying <c>[MarshalAs(UnmanagedType.Struct)]</c>
/// on an ENUM. <c>Struct</c> names the inline-struct form, and an enum marshals as its
/// underlying integer, so real .NET raises <c>TypeLoadException</c> when the struct crosses a
/// P/Invoke — the descriptor is refused for the field's KIND, with no width in the question.
/// <c>Compilation.MarshalDescriptorKindAllows</c> is the row that says so, and both askers
/// read it: the transpile refuses (exit 2) naming the field and the descriptor.
///
/// <para>This one IS run under real .NET, unlike the sibling refusal subjects: it is its own
/// oracle. <c>Ok</c> is the control that gives the assertion teeth — same library, same
/// missing entry point, and a verdict that separates "the marshaller refused the struct" from
/// "the marshaller accepted it and the symbol was not there".</para></summary>
public static class Program
{
    public enum Tag : int { None }

    [StructLayout(LayoutKind.Sequential)]
    public struct Inner { public int V; }

    [StructLayout(LayoutKind.Sequential)]
    public struct DescribedEnum
    {
        [MarshalAs(UnmanagedType.Struct)] public Tag T;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct Ok
    {
        [MarshalAs(UnmanagedType.Struct)] public Inner I;
    }

    // libc needs no --pinvoke-module opt-in, so the transpile reaches marshalling
    // validation instead of stopping at module admission. Neither entry point exists:
    // the transpile fails before anything links, and real .NET reports the struct
    // before it reports the missing symbol.
    [DllImport("libc")]
    private static extern int dn2cpp_never_linked_structenum(DescribedEnum s);

    [DllImport("libc")]
    private static extern int dn2cpp_never_linked_structvalue(Ok s);

    private static void Verdict(string name, Action a)
    {
        try
        {
            a();
            Console.WriteLine(name + "=NOTHROW");
        }
        catch (Exception e)
        {
            Console.WriteLine(name + "=" + e.GetType().Name);
        }
    }

    public static void Main()
    {
        Verdict("struct-on-enum", () => dn2cpp_never_linked_structenum(default));
        Verdict("struct-on-value", () => dn2cpp_never_linked_structvalue(default));
    }
}

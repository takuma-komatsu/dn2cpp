#nullable disable
using System;
using System.Runtime.InteropServices;

// A struct with a string or bool field is NOT blittable, so .NET marshals it field by
// field: strings through a NUL-terminated buffer, bools through the 4-byte Win32 BOOL,
// nested blittable structs copied as-is. A by-ref string field's write-back frees a buffer
// only when the native REPLACED our [In] one, which is GC memory. Layouts are asserted
// separately, by PInvokeMarshalLayoutSubset.
namespace PInvokeMarshalStructSubset;

internal static class Program
{
    // Default/Ansi struct CharSet, so the string field is UTF-8 on Unix.
    [StructLayout(LayoutKind.Sequential)]
    private struct Person { public int Id; public string Name; }

    // The same under CharSet.Unicode, so the string field is UTF-16.
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct WidePerson { public int Id; public string Name; }

    [StructLayout(LayoutKind.Sequential)]
    private struct Point2 { public int X; public int Y; }

    [StructLayout(LayoutKind.Sequential)]
    private struct FlagPoint { public bool On; public Point2 Pt; public int Tag; }

    [DllImport("dn2cpptest")] private static extern int dn2cpptest_person_score(Person p);
    [DllImport("dn2cpptest")] private static extern void dn2cpptest_person_bump(ref Person p);
    // The same native symbol as [In] ref: read-only, so no write-back.
    [DllImport("dn2cpptest", EntryPoint = "dn2cpptest_person_bump")]
    private static extern void dn2cpptest_person_bump_in([In] ref Person p);
    [DllImport("dn2cpptest")] private static extern void dn2cpptest_person_make(out Person p, int id);

    [DllImport("dn2cpptest")] private static extern int dn2cpptest_flagpoint_eval(FlagPoint f);
    [DllImport("dn2cpptest")] private static extern void dn2cpptest_flagpoint_flip(ref FlagPoint f);

    [DllImport("dn2cpptest", CharSet = CharSet.Unicode)]
    private static extern int dn2cpptest_wperson_score(WidePerson p);
    [DllImport("dn2cpptest", CharSet = CharSet.Unicode)]
    private static extern void dn2cpptest_wperson_bump(ref WidePerson p);

    internal static void __GateEntry()
    {
        var p = new Person { Id = 10, Name = "hello" };
        Console.WriteLine(dn2cpptest_person_score(p));          // 15

        // by-ref [In,Out]: the native replaces the name slot, leaving our GC buffer alone.
        var b = new Person { Id = 3, Name = "abc" };
        dn2cpptest_person_bump(ref b);
        Console.WriteLine($"{b.Id} {b.Name}");                  // 4 ABC

        // [In] ref: the native still mutates its copy, but nothing writes back.
        var q = new Person { Id = 3, Name = "abc" };
        dn2cpptest_person_bump_in(ref q);
        Console.WriteLine($"{q.Id} {q.Name}");                  // 3 abc

        // [Out] only skips the copy-in, so the native sees a zeroed struct.
        dn2cpptest_person_make(out Person m, 99);
        Console.WriteLine($"{m.Id} {m.Name}");                  // 99 made-by-native

        // A bool, a nested blittable struct and a scalar, by value.
        var f = new FlagPoint { On = true, Pt = new Point2 { X = 2, Y = 3 }, Tag = 7 };
        Console.WriteLine(dn2cpptest_flagpoint_eval(f));        // 1030

        var g = new FlagPoint { On = false, Pt = new Point2 { X = 4, Y = 5 }, Tag = 9 };
        dn2cpptest_flagpoint_flip(ref g);
        Console.WriteLine($"{g.On} {g.Pt.X} {g.Pt.Y} {g.Tag}"); // True 5 4 -9

        var w = new WidePerson { Id = 20, Name = "wörld" };
        Console.WriteLine(dn2cpptest_wperson_score(w));         // 25

        // UTF-16 write-back: the native upper-cases ASCII, and ö must survive intact.
        var wb = new WidePerson { Id = 1, Name = "wörld" };
        dn2cpptest_wperson_bump(ref wb);
        Console.WriteLine($"{wb.Id} {wb.Name}");                // 2 WöRLD
    }
}

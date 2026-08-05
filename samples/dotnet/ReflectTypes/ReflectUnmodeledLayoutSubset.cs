#nullable enable
// A value type the transpiler reached through a TYPE TOKEN ONLY carries no emitted field
// layout — it is an opaque shell — and the emitter pads that shell to the CLR extent the
// layout model computes, so instanceSize tells the truth. This section covers the shells
// whose extent the model CANNOT compute, because a field of theirs is typed at something
// with no modeled width (an intrinsic like GCHandle / CancellationToken / Vector128<T>,
// whose C++ representation is a hand-written runtime struct s_intrinsicStructExtents
// deliberately does not list, or an unmapped external type).
//
// Such a shell stays empty, so sizeof — and the instanceSize stamped from it — reads 1,
// which is answered rather than diagnosed: an array of them has elements that overlap.
// The number cannot carry the verdict, since `struct Blank { }` is honestly 1, so the
// emitter stamps DN2CPP_TF_LAYOUT_UNKNOWN on exactly the shells it left unpadded and every
// reader that hands the value outward as a size or an element stride throws a catchable
// PlatformNotSupportedException naming the type instead. Same shape as
// DN2CPP_TF_METADATA_STRIPPED: a wrong answer with no diagnostic is worse than a refusal.
//
// FROZEN deliberately: real .NET answers 16 and builds an array where dn2cpp refuses.
// The Marshal.SizeOf column is NOT part of that divergence — all four generic probes are
// CLOSED GENERICS, which .NET refuses outright whatever their layout, so ArgumentException
// there matches. The LAYOUT_UNKNOWN refusal is carried by the Unsafe.SizeOf and
// Array.CreateInstance columns, which reach dn2cpp_require_layout.
//
// The controls below keep the refusal honest: an empty struct still answers 1 (the flag is
// not a test on the number), a typeof-only shell whose extent the model CAN compute still
// answers that extent, and HandleHolder shows the layout and marshal models reaching one
// number where both of them have one.
using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

namespace ReflectUnmodeledLayoutSubset;

struct Blank { }

struct TwoDoubles { public double A, B; }

// The non-generic control. GCHandle's C++ representation is a POINTER, so TryFieldExtent
// models it: this shell is padded, its extent is known, and the marshalled-layout model
// carries GCHandle too — so both models answer real .NET's 8, which is the right outcome
// for a type whose representation IS its unmanaged form. That the two models ask
// independent questions is asserted where it costs no divergence, by
// ReflectMarshalVerdictSubset's "the verdict SPLIT" block: one type answering a size and
// refusing a copy.
struct HandleHolder { public GCHandle H; }

static class Program
{
    static string Try(Func<object?> f)
    {
        try { return f()?.ToString() ?? "null"; }
        catch (Exception e) { return e.GetType().Name; }
    }

    // The Arch.Core.ComponentRegistry.SizeOf(Type) shape: a size looked up for a type
    // known only at run time. No static call site names any of these instantiations.
    static string SizeOf(Type t) =>
        Try(() => typeof(Unsafe).GetMethod("SizeOf")!.MakeGenericMethod(t).Invoke(null, null));

    static void Report(string label, Type t)
    {
        Console.WriteLine($"{label}:");
        Console.WriteLine($"  Unsafe.SizeOf -> {SizeOf(t)}");
        Console.WriteLine($"  Marshal.SizeOf -> {Try(() => Marshal.SizeOf(t))}");
        Console.WriteLine($"  Array.CreateInstance -> {Try(() => Array.CreateInstance(t, 3).Length)}");
    }

    internal static void Run()
    {
        Console.WriteLine("== a shell whose extent the model cannot compute ==");
        // Each field type below has a hand-written C++ representation and no modeled
        // extent, so Nullable<T> over it has none either.
        Report("Nullable<GCHandle>", typeof(GCHandle?));
        Report("Nullable<CancellationToken>", typeof(System.Threading.CancellationToken?));
        Report("Nullable<Vector128<byte>>", typeof(Vector128<byte>?));
        // A grouped specialization: its struct name redirects to the canonical owner's,
        // so the stamp it reads is the OWNER's shell. The flag has to follow the same
        // redirection or this one keeps answering 1 while the others refuse.
        Report("ValueTuple<GCHandle,int>", typeof(ValueTuple<GCHandle, int>));
        // Non-generic, and both models size it: every column answers.
        Report("HandleHolder", typeof(HandleHolder));

        Console.WriteLine("== controls: the flag is not a test on the number ==");
        // A genuinely field-less struct really is 1 byte, and must keep answering.
        Console.WriteLine($"Blank -> {SizeOf(typeof(Blank))}");
        // A typeof-only shell whose extent the model CAN compute keeps that extent.
        Console.WriteLine($"TwoDoubles -> {SizeOf(typeof(TwoDoubles))}");
        Console.WriteLine($"int? -> {SizeOf(typeof(int?))}");
        Console.WriteLine($"long? -> {SizeOf(typeof(long?))}");

        Console.WriteLine("== the refusal is catchable, and names the type ==");
        try
        {
            Array.CreateInstance(typeof(GCHandle?), 1);
            Console.WriteLine("no throw");
        }
        catch (PlatformNotSupportedException e)
        {
            Console.WriteLine($"caught PlatformNotSupportedException, names the type: "
                + $"{e.Message.Contains("System.Nullable") || e.Message.Contains("Nullable`1")}");
        }
        catch (Exception e)
        {
            Console.WriteLine($"caught {e.GetType().Name}");
        }
    }
}

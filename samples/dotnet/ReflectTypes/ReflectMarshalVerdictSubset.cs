#nullable enable
// The DIVERGING residue of the marshalling verdict — shapes real .NET MEASURES and dn2cpp
// refuses. Its agreeing half is a LIVE diff against `dotnet run` in MarshalPinning's
// SizeOfOffsetOfSubset, which is the stronger assertion and therefore the default home; a
// row lives HERE only because a live diff cannot hold a deliberate divergence.
//
// What is left here is the COM surface, a STANDING carve-out rather than an unfinished
// corner. BStr and TBStr marshal through SysAllocString/SysFreeString — a COM allocator this
// runtime does not have and does not intend to — so a size for one would be the only number
// in the model with no second reader: nothing in the tree can produce or consume such a
// field, and docs/PINVOKE-MARSHALLING.md lists COM/BStr/SafeArray among the permanent
// carve-outs on a parameter or return for the same reason. Answering "8, and any attempt to
// marshal it fails" is worse than refusing: the number would be right, useless, and would
// read as support.
//
// An OBJECT field is the same carve-out reached from the other side, and it is the row that
// forces the point: real .NET's answer SPLITS BY HOST — Windows sizes a COM interface
// pointer at 8, POSIX refuses outright — while Marshal.SizeOf's verdict here is decided at
// TRANSPILE time and so cannot depend on the host that ran the transpiler. Refusing with the
// ArgumentException the other rows raise would additionally be a false claim about .NET on
// Windows, which is the one thing the two-bit verdict below exists to prevent.
//
// Every row throws PlatformNotSupportedException — deliberately NOT the ArgumentException
// the other refusals raise. That distinction is why the verdict is two bits rather than
// one: ArgumentException is a claim that .NET refuses too, and saying it about a type .NET
// happily measures would be a lie in the one place a caller reads for a reason. The
// PlatformNotSupportedException is dn2cpp speaking for itself, the same posture as
// DN2CPP_TF_LAYOUT_UNKNOWN and DN2CPP_TF_METADATA_STRIPPED.
//
// The controls at the tail keep this file from silently widening back out. They are the
// shapes the marshal-layout model (Compilation.MarshalLayout.cs) answers for, asserted here
// as numbers so a future tightening that starts refusing them again goes red beside the
// refusals rather than somewhere quiet.
using System;
using System.Runtime.InteropServices;

namespace ReflectMarshalVerdictSubset;

// The COM string forms: .NET measures both at 8 (a pointer), dn2cpp refuses.
struct BStrField { [MarshalAs(UnmanagedType.BStr)] public string S; }
struct TBStrField { [MarshalAs(UnmanagedType.TBStr)] public string S; }

// A field with no host-independent unmanaged form: 8 on Windows (a COM interface pointer),
// ArgumentException on POSIX.
struct WithObject { public object O; }

// Controls — every one of these is a shape the model measures, and each must keep ANSWERING.
struct OneBool { public bool B; }                                   // .NET 4 (Win32 BOOL)
struct OneChar { public char C; }                                   // .NET 1 (Ansi CharSet)
[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
struct OneWideChar { public char C; }                               // .NET 2
struct OneString { public string S; }                               // .NET 8 (a pointer)
struct OneDateTime { public DateTime D; }                           // .NET 8 (the one auto exception)
[StructLayout(LayoutKind.Sequential)] class SeqClass { public int A; public int B; }  // .NET 8
struct Blittable { public int I; public double D; }
struct NestedBlittable { public Blittable B; public long L; }
enum Small : byte { A }
struct WithEnumField { public Small E; public int I; }

static class Program
{
    private static string Verdict(Func<object?> f)
    {
        try { return f()?.ToString() ?? "null"; }
        catch (Exception e) { return e.GetType().Name; }
    }

    private static void Row(string label, Type t) =>
        Console.WriteLine($"  {label} -> {Verdict(() => Marshal.SizeOf(t))}");

    internal static void Run()
    {
        Console.WriteLine("== refused where .NET measures (declared divergence) ==");
        Row("BStrField (.NET 8)", typeof(BStrField));
        Row("TBStrField (.NET 8)", typeof(TBStrField));
        Row("WithObject (.NET 8 on Windows, refused on POSIX)", typeof(WithObject));

        Console.WriteLine("== the same verdict from the generic spelling ==");
        // The two spellings must not disagree — one answering a number while the other
        // refuses is the failure mode the DateTime note in CppEmitter.cs describes.
        Console.WriteLine($"  SizeOf<BStrField> -> {Verdict(() => Marshal.SizeOf<BStrField>())}");
        Console.WriteLine($"  SizeOf<TBStrField> -> {Verdict(() => Marshal.SizeOf<TBStrField>())}");
        Console.WriteLine($"  SizeOf<WithObject> -> {Verdict(() => Marshal.SizeOf<WithObject>())}");

        Console.WriteLine("== the refusal is catchable and names the type ==");
        try
        {
            Marshal.SizeOf(typeof(BStrField));
            Console.WriteLine("  no throw");
        }
        catch (PlatformNotSupportedException e)
        {
            Console.WriteLine($"  names the type: {e.Message.Contains("BStrField")}");
        }
        catch (Exception e)
        {
            Console.WriteLine($"  caught {e.GetType().Name}");
        }

        Console.WriteLine("== controls: the measured shapes must keep answering ==");
        Row("OneBool", typeof(OneBool));
        Row("OneChar", typeof(OneChar));
        Row("OneWideChar", typeof(OneWideChar));
        Row("OneString", typeof(OneString));
        Row("OneDateTime", typeof(OneDateTime));
        Row("SeqClass", typeof(SeqClass));
        Row("Blittable", typeof(Blittable));
        Row("NestedBlittable", typeof(NestedBlittable));
        Row("WithEnumField", typeof(WithEnumField));
        Console.WriteLine($"  SizeOf<OneBool> -> {Verdict(() => Marshal.SizeOf<OneBool>())}");
        Console.WriteLine($"  SizeOf<SeqClass> -> {Verdict(() => Marshal.SizeOf<SeqClass>())}");
        Console.WriteLine($"  SizeOf<NestedBlittable> -> {Verdict(() => Marshal.SizeOf<NestedBlittable>())}");
        Console.WriteLine($"  OffsetOf<NestedBlittable>(L) -> {Verdict(() => (long)Marshal.OffsetOf<NestedBlittable>("L"))}");
        // OffsetOf over a [StructLayout(Sequential)] REFERENCE type. Lowering it to
        // `offsetof(StorageOf(t), …)` emits C++ that does not compile — StorageOf a
        // reference type is a pointer — so the offset comes from the metadata instead,
        // base-chain fields first and no object header, giving .NET's 4.
        Console.WriteLine($"  OffsetOf<SeqClass>(B) -> {Verdict(() => (long)Marshal.OffsetOf<SeqClass>("B"))}");

        Console.WriteLine("== the verdict SPLIT: a size is not a byte image ==");
        // PtrToStructure/StructureToPtr carry their own verdict. SizeOf answers for a
        // non-blittable struct because the model knows how many bytes .NET would write; a
        // COPY still requires the representation and the marshalled form to BE the same
        // bytes, and for OneBool they are not (1 byte here, 4 unmanaged). .NET copies
        // happily, so this is a declared divergence and belongs in a freeze, not in the
        // live diff where the rest of these rows now live.
        //
        // The pairing is the assertion: the SAME type answers a size and refuses a copy.
        // If a future change routed the copy through the size verdict, the first line would
        // be unchanged and the second would start printing a boxed value — which is the
        // silent misread the split exists to prevent, made visible here.
        IntPtr blk = Marshal.AllocHGlobal(32);
        // AllocHGlobal does not zero, so the copy must read a byte this program wrote:
        // an unwritten block is a fresh zero page on one allocator and reused on the next.
        Marshal.WriteInt32(blk, 0, 1234);
        try
        {
            Console.WriteLine($"  SizeOf<OneBool> -> {Verdict(() => Marshal.SizeOf<OneBool>())}");
            Console.WriteLine($"  PtrToStructure(OneBool) -> {Verdict(() => Marshal.PtrToStructure(blk, typeof(OneBool))!)}");
            Console.WriteLine($"  PtrToStructure(Blittable) -> {Verdict(() => ((Blittable)Marshal.PtrToStructure(blk, typeof(Blittable))!).I)}");
        }
        finally
        {
            Marshal.FreeHGlobal(blk);
        }
    }
}

#nullable enable
using System;
using System.Runtime.InteropServices;

// Marshal.SizeOf / OffsetOf over blittable and non-blittable shapes: the values must
// equal real .NET's marshalled size/offset exactly, and both spellings — the generic
// SizeOf<T>/OffsetOf<T> (a compile-time constant) and the non-generic SizeOf(Type) /
// OffsetOf(Type, name) (a runtime verdict) — must answer the same number on every row
// that has both. CoreLib only; diffed exact vs real .NET.
//
// A type's MARSHALLED layout is a different question from its managed representation
// (asserted via sizeof/Unsafe.SizeOf in FlatMemEdges' SubWordStructLayoutSubset); the
// two must never be conflated. The shapes dn2cpp REFUSES where .NET answers are the
// other half of the verdict and live in ReflectTypes' ReflectMarshalVerdictSubset,
// which is a freeze gate precisely because it diverges; anything that agrees with .NET
// belongs here, where .NET itself is the oracle rather than a snapshot of dn2cpp.
namespace SizeOfOffsetOfSubset;

struct Multi { public int I; public long L; public double D; public IntPtr P; }   // 32 bytes

// Shapes whose MARSHALLED layout differs from their representation. Every expected
// number here is real .NET's, and the gate diffs against it live.
struct OneBool { public bool B; }                                    // 4 — the Win32 BOOL
struct OneChar { public char C; }                                    // 1 — the Ansi CharSet
[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
struct OneWideChar { public char C; }                                // 2
struct OneString { public string S; }                                // 8 — a pointer
struct OneDateTime { public DateTime D; }                            // 8 — the one auto exception
[StructLayout(LayoutKind.Sequential)] class SeqClass { public int A; public int B; }        // 8
[StructLayout(LayoutKind.Sequential)] class SeqDerived : SeqClass { public int K; }         // 12, K@8
struct ByteThenBool { public byte A; public bool B; }                // 8, B@4 (bool aligns 4)
[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
struct UniOuterAnsiInner { public OneChar C; public byte Pad; }      // 2 — CharSet does not nest
[StructLayout(LayoutKind.Sequential, Pack = 1)]
struct Pack1 { public byte B; public long L; }                       // 9, L@1
[StructLayout(LayoutKind.Sequential, Size = 64)] struct Sized64 { public int I; }           // 64
// Size floors the total and is NOT rounded up to the alignment, even when N is not a
// multiple of it — provided the shape is representable (N a multiple of the struct's real
// sub-word alignment; Size=6 over an int is refused at emission, see the negative arm in
// build-and-run-marshal-pinning.sh, because C++ cannot express size 6 with alignment 4
// while real .NET measures exactly that).
[StructLayout(LayoutKind.Sequential, Size = 3)] struct Sized3 { public byte B; }            // 3
struct Sized3Tail { public Sized3 F; public byte T; }            // 4, T@3 (the floor is live as a MEMBER)
[StructLayout(LayoutKind.Sequential, Size = 6)] struct Sized6Short { public short A; }      // 6
struct Sized6ShortTail { public Sized6Short F; public byte T; }  // 8, T@6 (container rounds by align 2)
struct InlineArr
{
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)] public int[] A;                    // 12 inline
    public byte Tail;                                                // 16, Tail@12
}
struct InlineStr
{
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 5)] public string S;                    // 5 bytes Ansi
    public byte Tail;                                                // 6, Tail@5
}
[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
struct InlineStrWide
{
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 5)] public string S;                    // 10 bytes Unicode
    public byte Tail;                                                // 12, Tail@10
}
struct NarrowBool { [MarshalAs(UnmanagedType.U1)] public bool B; }   // 1
struct SubWord { public byte B; public short S; public ushort U; public int I; }  // 12 bytes (B@0 S@2 U@4 I@8)

// Marshalling-verdict fixtures. Every one of these agrees with real .NET.
enum EByte : byte { A }
enum ELong : long { A }
[StructLayout(LayoutKind.Auto)] struct AutoLaidOut { public int A; public int B; }
[StructLayout(LayoutKind.Explicit)] struct Overlap { [FieldOffset(0)] public int A; [FieldOffset(8)] public int B; }
struct WithEnumField { public EByte E; public int I; }
struct NestedBlittable { public Multi M; public SubWord S; }
// A descriptor may only name the width the field already has AT EVERY POINTER WIDTH: a
// fixed width on a pointer-wide field, or SysInt on a fixed-width one, contradicts itself
// at one of the two and .NET refuses it at both. The pair that agrees is the control.
struct PtrU8 { [MarshalAs(UnmanagedType.U8)] public IntPtr F; }
struct LongSysInt { [MarshalAs(UnmanagedType.SysInt)] public long F; }
struct PtrSysInt { [MarshalAs(UnmanagedType.SysInt)] public IntPtr F; }
struct IntI4 { [MarshalAs(UnmanagedType.I4)] public int F; }
unsafe struct BarePointer { public void* F; }
unsafe struct PointerSysInt { [MarshalAs(UnmanagedType.SysInt)] public void* F; }
unsafe struct FunctionPointerSysInt { [MarshalAs(UnmanagedType.SysInt)] public delegate* unmanaged<void> F; }
unsafe struct PointerI8 { [MarshalAs(UnmanagedType.I8)] public void* F; }
// FunctionPtr is the one descriptor a pointer-shaped field takes, and only a genuine
// function pointer takes it — either calling convention; void* is refused. The bare
// field is the control, and the tail field pins the described offset, not just the size.
unsafe struct BareFnPtr { public delegate* unmanaged<void> F; }
unsafe struct FnPtrFnPtr { [MarshalAs(UnmanagedType.FunctionPtr)] public delegate* unmanaged<void> F; }
unsafe struct ManagedFnPtrFnPtr { [MarshalAs(UnmanagedType.FunctionPtr)] public delegate*<void> F; }
unsafe struct VoidPtrFnPtr { [MarshalAs(UnmanagedType.FunctionPtr)] public void* F; }
unsafe struct FnPtrTail { [MarshalAs(UnmanagedType.FunctionPtr)] public delegate* unmanaged<void> F; public byte T; }
struct Empty { }
class PlainClass { public int A; }

unsafe class Program
{
    // Type name only: the messages are .NET's, and localized.
    private static string Verdict(Func<object> f)
    {
        try { return f().ToString()!; }
        catch (Exception e) { return e.GetType().Name; }
    }

    internal static void __GateEntry()
    {
        // SizeOf<T> / SizeOf(Type) — must equal .NET's marshalled size exactly.
        Console.WriteLine(Marshal.SizeOf<Multi>());            // 32
        Console.WriteLine(Marshal.SizeOf(typeof(Multi)));      // 32
        Console.WriteLine(Marshal.SizeOf<int>());              // 4
        Console.WriteLine(Marshal.SizeOf<long>());             // 8
        Console.WriteLine(Marshal.SizeOf<double>());           // 8
        Console.WriteLine(Marshal.SizeOf<IntPtr>());           // 8

        // OffsetOf<T>(name) / OffsetOf(Type, name) — field byte offsets.
        Console.WriteLine((long)Marshal.OffsetOf<Multi>("I"));        // 0
        Console.WriteLine((long)Marshal.OffsetOf<Multi>("L"));        // 8
        Console.WriteLine((long)Marshal.OffsetOf<Multi>("D"));        // 16
        Console.WriteLine((long)Marshal.OffsetOf<Multi>("P"));        // 24
        Console.WriteLine((long)Marshal.OffsetOf(typeof(Multi), "L")); // 8
        Console.WriteLine((long)Marshal.OffsetOf(typeof(Multi), "P")); // 24

        // Sub-word fields (packed at real storage width, so the managed
        // and marshalled layouts agree). SizeOf<T> on the sub-word primitives too
        // (marshalled size == storage width).
        Console.WriteLine(Marshal.SizeOf<SubWord>());                    // 12
        Console.WriteLine(Marshal.SizeOf(typeof(SubWord)));              // 12
        Console.WriteLine(Marshal.SizeOf<byte>());                       // 1
        Console.WriteLine(Marshal.SizeOf<short>());                      // 2
        Console.WriteLine((long)Marshal.OffsetOf<SubWord>("B"));         // 0
        Console.WriteLine((long)Marshal.OffsetOf<SubWord>("S"));         // 2
        Console.WriteLine((long)Marshal.OffsetOf<SubWord>("U"));         // 4
        Console.WriteLine((long)Marshal.OffsetOf<SubWord>("I"));         // 8
        Console.WriteLine((long)Marshal.OffsetOf(typeof(SubWord), "U")); // 4

        // ---- The marshalling verdict, on the rows where dn2cpp and .NET agree ----

        // The small primitives answer from a fixed table, never from instanceSize (which
        // is the BOX payload width, 4 for all of them). bool and char are the two rows a
        // sizeof could never have produced: 4 is the Win32 BOOL, 1 the default Ansi char.
        Console.WriteLine("m byte=" + Marshal.SizeOf(typeof(byte)) + "/" + Marshal.SizeOf<byte>());
        Console.WriteLine("m sbyte=" + Marshal.SizeOf(typeof(sbyte)) + "/" + Marshal.SizeOf<sbyte>());
        Console.WriteLine("m short=" + Marshal.SizeOf(typeof(short)) + "/" + Marshal.SizeOf<short>());
        Console.WriteLine("m ushort=" + Marshal.SizeOf(typeof(ushort)) + "/" + Marshal.SizeOf<ushort>());
        Console.WriteLine("m uint=" + Marshal.SizeOf(typeof(uint)) + "/" + Marshal.SizeOf<uint>());
        Console.WriteLine("m ulong=" + Marshal.SizeOf(typeof(ulong)) + "/" + Marshal.SizeOf<ulong>());
        Console.WriteLine("m float=" + Marshal.SizeOf(typeof(float)) + "/" + Marshal.SizeOf<float>());
        Console.WriteLine("m bool=" + Marshal.SizeOf(typeof(bool)) + "/" + Marshal.SizeOf<bool>());
        Console.WriteLine("m char=" + Marshal.SizeOf(typeof(char)) + "/" + Marshal.SizeOf<char>());
        Console.WriteLine("m nint=" + Marshal.SizeOf(typeof(IntPtr)) + "/" + Marshal.SizeOf<IntPtr>());
        Console.WriteLine("m nuint=" + Marshal.SizeOf(typeof(UIntPtr)) + "/" + Marshal.SizeOf<UIntPtr>());
        Console.WriteLine("m decimal=" + Marshal.SizeOf(typeof(decimal)) + "/" + Marshal.SizeOf<decimal>());

        // Blittable value types with a sequential or explicit layout: the emitted C++
        // struct's sizeof IS the marshalled size, so the answer is the number, and the two
        // spellings must produce the same one. Overlap is 12 (an explicit layout whose
        // declared fields end at 12), Empty is 1 (a field-less struct really is 1 byte).
        Console.WriteLine("m nested=" + Marshal.SizeOf(typeof(NestedBlittable)) + "/" + Marshal.SizeOf<NestedBlittable>());
        Console.WriteLine("m overlap=" + Marshal.SizeOf(typeof(Overlap)) + "/" + Marshal.SizeOf<Overlap>());
        Console.WriteLine("m empty=" + Marshal.SizeOf(typeof(Empty)) + "/" + Marshal.SizeOf<Empty>());
        // An enum FIELD marshals at its underlying width even though the enum TYPE is
        // refused below — .NET's AutoLayout refusal is a top-level rule, not an inherited one.
        Console.WriteLine("m enumfield=" + Marshal.SizeOf(typeof(WithEnumField)) + "/" + Marshal.SizeOf<WithEnumField>());
        // TimeSpan/DateOnly/TimeOnly are intrinsic value types whose runtime representation
        // equals .NET's marshalled size; DateTime and DateTimeOffset are NOT, and are
        // refused below for the reason .NET refuses them (AutoLayout).
        Console.WriteLine("m timespan=" + Marshal.SizeOf(typeof(TimeSpan)));
        Console.WriteLine("m dateonly=" + Marshal.SizeOf(typeof(DateOnly)));
        Console.WriteLine("m timeonly=" + Marshal.SizeOf(typeof(TimeOnly)));
        Console.WriteLine("m guid=" + Marshal.SizeOf(typeof(Guid)) + "/" + Marshal.SizeOf<Guid>());

        // The refusals .NET makes, which dn2cpp makes too — same exception type, from
        // both spellings.
        Console.WriteLine("m enum-int=" + Verdict(() => Marshal.SizeOf(typeof(DayOfWeek))));
        Console.WriteLine("m enum-byte=" + Verdict(() => Marshal.SizeOf(typeof(EByte))) + "/" + Verdict(() => Marshal.SizeOf<EByte>()));
        Console.WriteLine("m enum-long=" + Verdict(() => Marshal.SizeOf(typeof(ELong))) + "/" + Verdict(() => Marshal.SizeOf<ELong>()));
        Console.WriteLine("m datetime=" + Verdict(() => Marshal.SizeOf(typeof(DateTime))) + "/" + Verdict(() => Marshal.SizeOf<DateTime>()));
        Console.WriteLine("m dto=" + Verdict(() => Marshal.SizeOf(typeof(DateTimeOffset))));
        Console.WriteLine("m auto=" + Verdict(() => Marshal.SizeOf(typeof(AutoLaidOut))) + "/" + Verdict(() => Marshal.SizeOf<AutoLaidOut>()));
        // A struct holding an OBJECT field is not among these: real .NET's answer splits by
        // host (Windows sizes a COM interface pointer, POSIX refuses), so dn2cpp declines it
        // instead — a declared divergence, in ReflectTypes' ReflectMarshalVerdictSubset.
        Console.WriteLine("m plainclass=" + Verdict(() => Marshal.SizeOf(typeof(PlainClass))) + "/" + Verdict(() => Marshal.SizeOf<PlainClass>()));
        Console.WriteLine("m string=" + Verdict(() => Marshal.SizeOf(typeof(string))));
        Console.WriteLine("m object=" + Verdict(() => Marshal.SizeOf(typeof(object))));
        Console.WriteLine("m array=" + Verdict(() => Marshal.SizeOf(typeof(int[]))));
        Console.WriteLine("m iface=" + Verdict(() => Marshal.SizeOf(typeof(IDisposable))));
        Console.WriteLine("m delegate=" + Verdict(() => Marshal.SizeOf(typeof(Action))));
        // A closed generic is refused with .NET's own separate message; Nullable<T> is one.
        Console.WriteLine("m nullable=" + Verdict(() => Marshal.SizeOf(typeof(int?))));
        Console.WriteLine("m null=" + Verdict(() => Marshal.SizeOf((Type)null!)));
        // PtrToStructure(ptr, Type) runs a verdict too, and a DIFFERENT one from SizeOf's:
        // a copy needs the representation and the marshalled form to be the same bytes,
        // which SizeOf does not, so the two part company for exactly the types the block
        // below measures.
        IntPtr blk = Marshal.AllocHGlobal(64);
        // AllocHGlobal does not zero, so the copy must read a byte this program wrote:
        // an unwritten block is a fresh zero page on one allocator and reused on the next.
        Marshal.WriteInt32(blk, 0, 1234);
        Console.WriteLine("m p2s-ok=" + Verdict(() => ((Multi)Marshal.PtrToStructure(blk, typeof(Multi))!).I));
        Console.WriteLine("m p2s-bad=" + Verdict(() => Marshal.PtrToStructure(blk, typeof(DateTime))!));
        Marshal.FreeHGlobal(blk);

        // ---- The MARSHALLED layout, live-diffed against real .NET ----
        //
        // Each row is a different reason the C++ representation is not the unmanaged form:
        // a bool is 4 unmanaged and 1 here; a char follows the declaring type's CharSet
        // (1 Ansi / 2 Unicode) against 2 here; a string is a pointer against a
        // Dn2CppString*; DateTime is the ONE auto-layout type .NET marshals as a field;
        // and a [StructLayout(Sequential)] CLASS lays out from 0 where the emitted type
        // carries an object header. Both spellings on every row that has both.
        Console.WriteLine("m onebool=" + Marshal.SizeOf(typeof(OneBool)) + "/" + Marshal.SizeOf<OneBool>());
        Console.WriteLine("m onechar=" + Marshal.SizeOf(typeof(OneChar)) + "/" + Marshal.SizeOf<OneChar>());
        Console.WriteLine("m onewidechar=" + Marshal.SizeOf(typeof(OneWideChar)) + "/" + Marshal.SizeOf<OneWideChar>());
        Console.WriteLine("m onestring=" + Marshal.SizeOf(typeof(OneString)) + "/" + Marshal.SizeOf<OneString>());
        Console.WriteLine("m onedatetime=" + Marshal.SizeOf(typeof(OneDateTime)) + "/" + Marshal.SizeOf<OneDateTime>());
        Console.WriteLine("m seqclass=" + Marshal.SizeOf(typeof(SeqClass)) + "/" + Marshal.SizeOf<SeqClass>());

        // A bool does not merely widen — it ALIGNS at 4, so the byte after it starts at 4
        // and not at 1. A model that got the width right and the alignment wrong would
        // pass a size-only assertion on the one-field struct above and fail here.
        Console.WriteLine("m boolalign=" + Marshal.SizeOf(typeof(ByteThenBool))
            + "/" + (long)Marshal.OffsetOf<ByteThenBool>("B"));
        // CharSet reaches the declaring type's own fields and stops there: the outer is
        // Unicode and the inner is not, so the inner char stays 1 byte.
        Console.WriteLine("m charsetnest=" + Marshal.SizeOf(typeof(UniOuterAnsiInner)));
        // Pack caps each field's alignment; Size floors the total; an empty type is 1.
        Console.WriteLine("m pack1=" + Marshal.SizeOf(typeof(Pack1)) + "/" + (long)Marshal.OffsetOf<Pack1>("L"));
        Console.WriteLine("m sized=" + Marshal.SizeOf(typeof(Sized64)));
        // A [MarshalAs] descriptor rewrites the extent: ByValArray inlines its elements,
        // ByValTStr inlines a character buffer at the CharSet width, and a bool width
        // override forces 1 byte.
        Console.WriteLine("m byvalarray=" + Marshal.SizeOf(typeof(InlineArr)) + "/" + (long)Marshal.OffsetOf<InlineArr>("Tail"));
        Console.WriteLine("m byvaltstr=" + Marshal.SizeOf(typeof(InlineStr)) + "/" + (long)Marshal.OffsetOf<InlineStr>("Tail"));
        Console.WriteLine("m byvaltstru=" + Marshal.SizeOf(typeof(InlineStrWide)) + "/" + (long)Marshal.OffsetOf<InlineStrWide>("Tail"));
        Console.WriteLine("m boolwidth=" + Marshal.SizeOf(typeof(NarrowBool)));
        // OffsetOf over a sequential CLASS, including one with a base: the base's fields
        // come first and there is no object header.
        Console.WriteLine("m seqclass-off=" + (long)Marshal.OffsetOf<SeqClass>("B")
            + "/" + (long)Marshal.OffsetOf(typeof(SeqClass), "B"));
        Console.WriteLine("m seqderived=" + Marshal.SizeOf(typeof(SeqDerived))
            + "/" + (long)Marshal.OffsetOf<SeqDerived>("K"));
        // Size floors, and the floor survives being a MEMBER — a byte after a
        // Size=3 byte struct sits at 3 (not at a rounded 4), and a byte after a Size=6
        // short struct sits at 6 with the container rounding to 8 by the member's own
        // 2-alignment. Both spellings, plus the representation (sizeof/Unsafe.SizeOf must
        // read the same floor here, since these shapes ARE representable), live-diffed
        // against real .NET.
        Console.WriteLine("m sized3=" + Marshal.SizeOf(typeof(Sized3)) + "/" + Marshal.SizeOf<Sized3>()
            + " u=" + System.Runtime.CompilerServices.Unsafe.SizeOf<Sized3>());
        Console.WriteLine("m sized3tail=" + Marshal.SizeOf(typeof(Sized3Tail))
            + "/" + (long)Marshal.OffsetOf<Sized3Tail>("T")
            + " u=" + System.Runtime.CompilerServices.Unsafe.SizeOf<Sized3Tail>());
        Console.WriteLine("m sized6short=" + Marshal.SizeOf(typeof(Sized6Short)) + "/" + Marshal.SizeOf<Sized6Short>()
            + " u=" + System.Runtime.CompilerServices.Unsafe.SizeOf<Sized6Short>());
        Console.WriteLine("m sized6shorttail=" + Marshal.SizeOf(typeof(Sized6ShortTail))
            + "/" + (long)Marshal.OffsetOf<Sized6ShortTail>("T")
            + " u=" + System.Runtime.CompilerServices.Unsafe.SizeOf<Sized6ShortTail>());
        // Both spellings on every row: a verdict the folded constant and the runtime stamp
        // disagree on is the failure this block exists to catch.
        Console.WriteLine("m ptr-u8=" + Verdict(() => Marshal.SizeOf(typeof(PtrU8))) + "/" + Verdict(() => Marshal.SizeOf<PtrU8>()));
        Console.WriteLine("m long-sysint=" + Verdict(() => Marshal.SizeOf(typeof(LongSysInt))) + "/" + Verdict(() => Marshal.SizeOf<LongSysInt>()));
        Console.WriteLine("m ptr-sysint=" + Marshal.SizeOf(typeof(PtrSysInt)) + "/" + Marshal.SizeOf<PtrSysInt>());
        Console.WriteLine("m int-i4=" + Marshal.SizeOf(typeof(IntI4)) + "/" + Marshal.SizeOf<IntI4>());
        // The bare pointer is the control; SysInt and I8 are invalid on void* and SysInt is
        // invalid on delegate*.
        Console.WriteLine("m pointer-bare=" + Marshal.SizeOf(typeof(BarePointer)) + "/" + Marshal.SizeOf<BarePointer>());
        Console.WriteLine("m pointer-sysint=" + Verdict(() => Marshal.SizeOf(typeof(PointerSysInt))) + "/" + Verdict(() => Marshal.SizeOf<PointerSysInt>()));
        Console.WriteLine("m fnptr-sysint=" + Verdict(() => Marshal.SizeOf(typeof(FunctionPointerSysInt))) + "/" + Verdict(() => Marshal.SizeOf<FunctionPointerSysInt>()));
        Console.WriteLine("m pointer-i8=" + Verdict(() => Marshal.SizeOf(typeof(PointerI8))) + "/" + Verdict(() => Marshal.SizeOf<PointerI8>()));
        // FunctionPtr is accepted on a function pointer alone — either calling
        // convention, described or bare, size and offset — and refused on void*, where
        // every descriptor is refused.
        Console.WriteLine("m fnptr-bare=" + Marshal.SizeOf(typeof(BareFnPtr)) + "/" + Marshal.SizeOf<BareFnPtr>());
        Console.WriteLine("m fnptr-fnptr=" + Marshal.SizeOf(typeof(FnPtrFnPtr)) + "/" + Marshal.SizeOf<FnPtrFnPtr>());
        Console.WriteLine("m fnptr-managed=" + Marshal.SizeOf(typeof(ManagedFnPtrFnPtr)) + "/" + Marshal.SizeOf<ManagedFnPtrFnPtr>());
        Console.WriteLine("m voidptr-fnptr=" + Verdict(() => Marshal.SizeOf(typeof(VoidPtrFnPtr))) + "/" + Verdict(() => Marshal.SizeOf<VoidPtrFnPtr>()));
        Console.WriteLine("m fnptr-tail=" + Marshal.SizeOf(typeof(FnPtrTail)) + "/" + (long)Marshal.OffsetOf<FnPtrTail>("T"));
        // The verdict split — SizeOf answers for a non-blittable struct while
        // PtrToStructure still refuses it — is a DECLARED divergence (.NET copies happily),
        // so it cannot live in a live diff. It is asserted in ReflectTypes'
        // ReflectMarshalVerdictSubset, which is frozen for exactly that reason.
    }
}

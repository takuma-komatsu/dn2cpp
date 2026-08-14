using System.Reflection.Metadata;
using UT = System.Runtime.InteropServices.UnmanagedType;

namespace Dn2Cpp;

/// <summary>What the marshalled-layout model can say about a type or a field.</summary>
internal enum MarshalSizeVerdict
{
    /// <summary>The model has the unmanaged extent, and it is real .NET's.</summary>
    Known,
    /// <summary>Real .NET itself refuses this shape (<c>ArgumentException</c>): an
    /// auto-layout type, a field with no unmanaged form at all, a <c>[MarshalAs]</c> whose
    /// width contradicts its field's type.</summary>
    Refused,
    /// <summary>Real .NET measures it and dn2cpp does not model it — the declared
    /// divergence, raised as a catchable <c>PlatformNotSupportedException</c> naming the
    /// shape. Never a number.</summary>
    Unknown,
}

/// <summary>An unmanaged extent: the size a value of the type occupies inside a marshalled
/// struct, and the alignment its offset is rounded up to. Both are properties of the
/// MARSHALLED form and have nothing to do with the emitted C++ struct — <c>bool</c> is
/// (4, 4) here and 1 byte there.</summary>
internal readonly record struct MarshalExtent(MarshalSizeVerdict Verdict, int Size, int Align)
{
    internal static readonly MarshalExtent Refused = new(MarshalSizeVerdict.Refused, 0, 0);
    internal static readonly MarshalExtent Unknown = new(MarshalSizeVerdict.Unknown, 0, 0);
    internal static MarshalExtent Ok(int size, int align) =>
        new(MarshalSizeVerdict.Known, size, align);
    internal bool IsKnown => Verdict == MarshalSizeVerdict.Known;
}

/// <summary>A type's whole marshalled layout: its extent plus every instance field's
/// unmanaged offset. The offsets are what <c>Marshal.OffsetOf</c> answers; they are not
/// <c>offsetof</c> of anything.</summary>
internal sealed class MarshalLayoutInfo
{
    internal required MarshalExtent Extent;
    internal required Dictionary<FieldInfo, int> Offsets;
}

/// <summary>A marshalled layout rendered as C++ text: the pair of readings the walk gave at
/// the two pointer widths, folded by <see cref="PointerWidth.Model"/>. Every number a
/// <c>static_assert</c> pins comes from here rather than from a single reading.</summary>
internal sealed class MarshalLayoutText
{
    internal required ModeledSize Size;
    internal required Dictionary<FieldInfo, ModeledSize> Offsets;
}

internal sealed partial class Compilation
{
    // ---- the MARSHALLED layout, beside the C++ one ----
    //
    // There are two layout models. The other one is the emitted C++ struct's, which
    // CppEmitter computes and which Unsafe.SizeOf, IL `sizeof`, the array stride, the box
    // payload and Unsafe.Add's stride all read — the REPRESENTATION. This file is the layout
    // real .NET's marshaller gives the same type, and the two are different numbers for
    // anything that is not blittable: `bool` is 1 byte represented and 4 unmanaged, `char`
    // is 2 and 1. Every rule below mirrors measured .NET behaviour, not a spec reading.
    //
    // THE TWO MODELS MUST NOT MERGE, and merging them fails silently: a number that
    // disagrees with what Unsafe.SizeOf and Unsafe.Add's stride actually use is worse than
    // a refusal. So nothing here is consulted by an emission path that lays out storage, and
    // nothing in CppEmitter's extent model is consulted here — this walk starts from the
    // metadata rows and .NET's own rules, which is why it can answer for shapes the C++
    // model cannot (a [StructLayout(Sequential)] CLASS, whose emitted fields sit behind the
    // Dn2CppObject header, has a marshalled layout that starts at 0).
    //
    // It is cross-checked for free: the P/Invoke marshalling struct tn_<Name> CppEmitter
    // emits for every non-blittable struct a P/Invoke passes IS this layout expressed as
    // C++, and CppEmitter stamps a static_assert per such struct comparing sizeof(tn_) and
    // offsetof(tn_, f) against the numbers computed here, so every gate that links one has
    // the C++ compiler check this arithmetic (CppEmitter.EmitPInvokeMarshalStructs).
    //
    // POINTER WIDTH IS A PARAMETER READ AT TWO FIXED VALUES, NEVER AT THE HOST'S. Callers
    // read this walk at 8 and at 4 and hand the pair to PointerWidth.Model, which writes the
    // size in sizeof(void*) — so Marshal.SizeOf is truthful on wasm32 without the transpiler
    // ever asking what the target is. Both widths are constants, which is what keeps the
    // output a pure function of the metadata; reading IntPtr.Size or sizeof here would trade
    // that for a host-dependent constant, and that is the one thing this file may never do.
    //
    // THE 64-BIT READING ALONE DECIDES THE VERDICT. Refused/Unknown is what the ptr=8 walk
    // says; the ptr=4 walk is consulted for numbers and never for a diagnosis — the same
    // contract, for the same reason, as CppEmitter.TryExplicitLayoutSize.

    // Keyed on the pointer width the reading assumed, mirroring CppEmitter._structExtents.
    private readonly Dictionary<(ClassInfo Cls, int Ptr), MarshalLayoutInfo> _marshalLayouts = new();

    /// <summary>The default structure packing when <c>[StructLayout(Pack)]</c> is
    /// unspecified. 8 at BOTH pointer widths, and inert at both: pack only CAPS an
    /// alignment, no extent in this model aligns above 8, and on wasm32
    /// <c>alignof(double) == alignof(long long) == 8</c> (measured with the pinned
    /// emsdk).</summary>
    private const int DefaultMarshalPack = 8;

    /// <summary>The marshalled layout of <paramref name="cls"/> asked as the TOP-LEVEL
    /// argument of <c>Marshal.SizeOf</c> / <c>Marshal.OffsetOf</c>.
    ///
    /// <para>One gate separates this from <see cref="MarshalMemberLayout"/>, and it is not
    /// inherited: an <b>auto-layout</b> type is refused here whatever it is. That covers
    /// every enum (AutoLayout is the metadata default for one), every plain class, and
    /// <c>DateTime</c> — real .NET refuses <c>Marshal.SizeOf(typeof(DateTime))</c> and
    /// measures a struct holding a <c>DateTime</c> field at 8.</para>
    ///
    /// <para>A closed or open GENERIC is refused before this is reached, by the callers,
    /// because .NET refuses it with a different message ("The specified Type must not be a
    /// generic type.") — a generic's FIELD is laid out normally, which is why the gate
    /// cannot live in the walk.</para></summary>
    internal MarshalLayoutInfo? TopLevelMarshalLayout(ClassInfo cls)
    {
        if (cls.IsAutoLayout)
            return null;
        var l = MarshalLayoutWalk(cls, PointerWidth.Bytes64);
        return l.Extent.Verdict == MarshalSizeVerdict.Refused ? null : l;
    }

    /// <summary>The top-level verdict, kept separate from the layout because a refusal and
    /// an unknown carry different exceptions (see <see cref="MarshalSizeVerdict"/>).</summary>
    internal MarshalExtent TopLevelMarshalExtent(ClassInfo cls) =>
        cls.IsAutoLayout ? MarshalExtent.Refused : MarshalLayoutWalk(cls, PointerWidth.Bytes64).Extent;

    /// <summary>The top-level size as C++ text, null when the verdict is not
    /// <see cref="MarshalSizeVerdict.Known"/> — or when the size is GUARDED, i.e. right only
    /// at 64 bits. Both readers of this text must agree, and the stamp already declines a
    /// guarded size, so folding one into <c>SizeOf&lt;T&gt;</c> would answer where the
    /// non-generic spelling throws. Declining it here is what makes them agree.</summary>
    internal ModeledSize? TopLevelMarshalSizeText(ClassInfo cls)
    {
        var e = TopLevelMarshalExtent(cls);
        if (!e.IsKnown)
            return null;
        return PointerWidth.Model(e.Size, NarrowTopLevel(cls)?.Extent.Size) is { Guarded: false } m ? m : null;
    }

    /// <summary>One field's top-level <c>Marshal.OffsetOf</c> as C++ text; null when the type
    /// or the field is outside the model.</summary>
    internal ModeledSize? TopLevelMarshalOffsetText(ClassInfo cls, FieldInfo f)
    {
        if (TopLevelMarshalLayout(cls) is not { Extent.IsKnown: true } l
            || !l.Offsets.TryGetValue(f, out int off))
            return null;
        return PointerWidth.Model(off, NarrowOffset(NarrowTopLevel(cls), f));
    }

    /// <summary>The member-position layout rendered as C++ text — what the emitted
    /// <c>tn_&lt;Name&gt;</c> asserts pin.</summary>
    internal MarshalLayoutText? MarshalMemberLayoutText(ClassInfo cls)
    {
        if (MarshalMemberLayout(cls, PointerWidth.Bytes64) is not { } wide)
            return null;
        var narrow = NarrowMember(cls);
        var offsets = new Dictionary<FieldInfo, ModeledSize>();
        foreach (var (f, off) in wide.Offsets)
            offsets[f] = PointerWidth.Model(off, NarrowOffset(narrow, f));
        return new MarshalLayoutText
        {
            Size = PointerWidth.Model(wide.Extent.Size, narrow?.Extent.Size),
            Offsets = offsets,
        };
    }

    private MarshalLayoutInfo? NarrowTopLevel(ClassInfo cls) =>
        cls.IsAutoLayout ? null : IfKnown(MarshalLayoutWalk(cls, PointerWidth.Bytes32));

    private MarshalLayoutInfo? NarrowMember(ClassInfo cls) =>
        MarshalMemberLayout(cls, PointerWidth.Bytes32);

    private static MarshalLayoutInfo? IfKnown(MarshalLayoutInfo l) => l.Extent.IsKnown ? l : null;

    private static int? NarrowOffset(MarshalLayoutInfo? narrow, FieldInfo f) =>
        narrow is not null && narrow.Offsets.TryGetValue(f, out int o) ? o : null;

    /// <summary>The extent <paramref name="cls"/> contributes as a FIELD of something else.
    ///
    /// <para><b><c>System.DateTime</c> is the one auto-layout type real .NET marshals as a
    /// field, and it is a hard-coded CoreCLR special case rather than a rule.</b> It
    /// measures 8 bytes aligned 8; <c>DateTimeOffset</c> is refused, and so is every user
    /// <c>[StructLayout(LayoutKind.Auto)]</c> struct down to a single-<c>long</c> one. So
    /// there is no "auto layouts are laid out in metadata order" rule to generalise to;
    /// writing one would produce numbers .NET answers an exception for. (<c>TimeSpan</c>,
    /// <c>Guid</c> and <c>decimal</c> look like counter-examples and are not: all three are
    /// Sequential in metadata.)</para></summary>
    internal MarshalExtent MarshalMemberExtent(ClassInfo cls, int ptr)
    {
        if (cls.FullName == "System.DateTime")
            return MarshalExtent.Ok(8, 8);
        // An ENUM is AutoLayout in metadata — that is why .NET refuses one as the top-level
        // argument — so this test MUST precede the auto-layout gate below, or every struct
        // with an enum field is refused where .NET measures it at the underlying width
        // (a byte-backed enum field is 1, a long-backed one 8).
        if (cls.IsEnum)
            return MarshalPrimitiveExtent(cls.EnumUnderlying, unicode: false, ptr);
        if (cls.IsAutoLayout)
            return MarshalExtent.Refused;
        return MarshalLayoutWalk(cls, ptr).Extent;
    }

    /// <summary>The member-position layout — <see cref="MarshalMemberExtent"/>'s offsets,
    /// for the callers that need to place a nested type's fields.</summary>
    internal MarshalLayoutInfo? MarshalMemberLayout(ClassInfo cls, int ptr)
    {
        if (cls.IsAutoLayout && cls.FullName != "System.DateTime")
            return null;
        return IfKnown(MarshalLayoutWalk(cls, ptr));
    }

    /// <summary>The walk itself, memoized per class. Pre-seeded with Unknown before the
    /// compute, so a malformed by-value cycle degrades to a refusal rather than recursing —
    /// the same guard shape (and the same reason) as <c>CppEmitter._structExtents</c> and
    /// <c>Compilation._marshalVerdicts</c>.</summary>
    private MarshalLayoutInfo MarshalLayoutWalk(ClassInfo cls, int ptr)
    {
        if (_marshalLayouts.TryGetValue((cls, ptr), out var cached))
            return cached;
        var seed = new MarshalLayoutInfo { Extent = MarshalExtent.Unknown, Offsets = new() };
        _marshalLayouts[(cls, ptr)] = seed;
        MarshalLayoutInfo v;
        try
        {
            v = ComputeMarshalLayout(cls, ptr);
        }
        catch (NotSupportedException e) when (!IsMustEscape(e))
        {
            // Same contract as CppEmitter.TryStructExtent's catch and the marshal-verdict
            // walk's: a shape the layout model rejects loudly on the emission path has no
            // marshalled size either, and "unknown" is the honest answer. Nothing is muted —
            // a type this walk sees runs the throwing calls itself during its own emission.
            v = seed;
        }
        _marshalLayouts[(cls, ptr)] = v;
        return v;
    }

    private MarshalLayoutInfo ComputeMarshalLayout(ClassInfo cls, int ptr)
    {
        var offsets = new Dictionary<FieldInfo, int>();
        MarshalLayoutInfo Bare(MarshalExtent e) => new() { Extent = e, Offsets = offsets };

        // An intrinsic value type has no transpiled field rows to walk — its C++ body is a
        // hand-written runtime struct — so the walk below would read it as field-less and
        // answer 1 about a layout it never saw. The ones whose unmanaged extent is known
        // answer from the table below; every other intrinsic (Vector128<T>, the
        // memory-mapping handles) refuses.
        if (cls.IntrinsicCppName is not null)
            return Bare(IntrinsicMarshalExtent(cls.FullName, ptr));
        // MembersReady-guarded for the ModelCensus reason: reading the field rows of an
        // uncompleted specialization just to answer this would defeat the deferral (and
        // throw under DN2CPP_STRICT_COMPLETION).
        if (!cls.MembersReady)
            return Bare(MarshalExtent.Unknown);
        // An enum's marshalled extent is its underlying integer's. (.NET refuses an enum as
        // the TOP-LEVEL argument — that is its AutoLayout, gated above — and marshals one as
        // a field at the underlying width.)
        if (cls.IsEnum)
            return Bare(MarshalPrimitiveExtent(cls.EnumUnderlying, unicode: false, ptr));
        // An [InlineArray(N)] struct is N copies of its single field: an InlineArray(4) of
        // int is 16, aligned 4.
        if (cls.InlineArrayLength > 0)
        {
            var one = cls.Fields.FirstOrDefault(f => !f.IsStatic && !f.IsLiteral);
            if (one is null)
                return Bare(MarshalExtent.Unknown);
            var e = MarshalFieldExtent(one, cls.LayoutCharSetUnicode, ptr);
            if (!e.IsKnown)
                return Bare(e);
            offsets[one] = 0;
            return Bare(MarshalExtent.Ok(e.Size * cls.InlineArrayLength, e.Align));
        }

        // The field list. For a REFERENCE type the base chain's fields come FIRST: a
        // [StructLayout(Sequential)] class deriving from another lays the base's int at 0
        // and its own at 4. There is no object header in this model — the header is a fact
        // about the emitted C++, not about the marshalled form.
        var fields = new List<FieldInfo>();
        if (!cls.IsValueType)
        {
            var chain = new List<ClassInfo>();
            for (var b = cls; b is not null; b = b.BaseClass)
            {
                if (b.FullName == "System.Object")
                    break;
                // A base that is auto-layout has no marshalled layout of its own, and .NET
                // does not invent one for the derived type either. Unknown rather than
                // Refused: this is a shape the model declines to place, not one .NET is
                // known to reject.
                if (b != cls && (b.IsAutoLayout || !b.MembersReady))
                    return Bare(MarshalExtent.Unknown);
                chain.Add(b);
            }
            chain.Reverse();
            foreach (var b in chain)
                fields.AddRange(b.Fields.Where(f => !f.IsStatic && !f.IsLiteral));
        }
        else
        {
            fields.AddRange(cls.Fields.Where(f => !f.IsStatic && !f.IsLiteral));
        }

        int pack = cls.LayoutPack > 0 ? cls.LayoutPack : DefaultMarshalPack;
        bool unicode = cls.LayoutCharSetUnicode;
        int cursor = 0, end = 0, maxAlign = 1;
        foreach (var f in fields)
        {
            var e = MarshalFieldExtent(f, unicode, ptr);
            if (!e.IsKnown)
                return Bare(e);
            // Pack caps each field's alignment; it can never widen one — Pack=1 puts a long
            // at offset 1, Pack=16 leaves a double at 8.
            int a = Math.Min(e.Align, pack);
            int off;
            if (cls.IsExplicitLayout)
            {
                if (f.ExplicitOffset < 0)
                    return Bare(MarshalExtent.Unknown); // no [FieldOffset] under Explicit
                off = f.ExplicitOffset;
            }
            else
            {
                off = AlignUp(cursor, a);
            }
            offsets[f] = off;
            cursor = off + e.Size;
            if (cursor > end) end = cursor;
            if (a > maxAlign) maxAlign = a;
        }

        int size = AlignUp(end, maxAlign);
        // An empty struct or class marshals as one byte.
        if (size == 0)
            size = 1;
        // An explicit [StructLayout(Size = N)] floors the size and never shrinks it: Size=64
        // over one int is 64, Size=2 over one int is 4. It is deliberately NOT rounded up to
        // the alignment — a Size=3 one-byte struct measures 3, and Size=6 over an int
        // measures 6 even though 6 is not a multiple of the alignment 4. On real .NET that
        // same 6 is what sizeof, Unsafe.SizeOf and the ARRAY STRIDE read, i.e. .NET keeps
        // size 6 with alignment 4, which no C++ type can express (sizeof is always a
        // multiple of alignof). The representation side therefore refuses those shapes
        // loudly at emission (CppEmitter.SequentialSizePadding and ExplicitLayoutExtent,
        // both on max(Size, field end)), so this floor's non-multiple arm is only ever
        // consulted for a type the transpile is about to reject; the member arithmetic
        // below stays live where the member IS representable (N a multiple of a smaller
        // alignment — after a Size=3 byte struct a byte field sits at offset 3 and the
        // container measures 4).
        if (cls.LayoutSize > size)
            size = cls.LayoutSize;
        return new MarshalLayoutInfo
        {
            Extent = MarshalExtent.Ok(size, maxAlign),
            Offsets = offsets,
        };
    }

    private static int AlignUp(int v, int a) => a <= 1 ? v : (v + a - 1) / a * a;

    /// <summary>The number to stamp into <c>Dn2CppTypeInfo::marshalSize</c>, or null when
    /// the model has no answer and the trailing 0 should stand.
    ///
    /// <para>A GENERIC is deliberately never stamped: .NET refuses one whatever its layout,
    /// with its own distinct message, and the runtime asks that question before it reads
    /// this member — a stamp there would be inert at best and, if the order ever moved, a
    /// number where .NET raises. Same for a type the runtime answers from its own fixed
    /// primitive table.</para></summary>
    internal ModeledSize? MarshalStampSize(ClassInfo cls)
    {
        if (cls.GenericArity > 0 || cls.Context.TypeArgs.Length > 0)
            return null;
        return TopLevelMarshalSizeText(cls);
    }

    /// <summary>Whether one field's unmanaged form is in the model — for the diagnostics
    /// that have to name the shape that took a type out of it. It asks the same builder the
    /// walk asks, so a field it calls modelled is one the walk will place.</summary>
    internal bool MarshalFieldIsModelled(FieldInfo f) =>
        MarshalFieldExtent(f, f.DeclaringClass.LayoutCharSetUnicode, PointerWidth.Bytes64).IsKnown;

    /// <summary>The unmanaged extents of the intrinsic value types — the ones whose C++ body
    /// is a hand-written runtime struct, so there are no field rows to walk. Every row is
    /// real .NET's <c>Marshal.SizeOf</c> answer with the alignment it lays the type out at.
    ///
    /// <para><c>DateTime</c> is here for its MEMBER position only — <see cref="MarshalMemberExtent"/>
    /// reaches it before the auto-layout gate, and <see cref="TopLevelMarshalExtent"/> never
    /// does. <c>DateTimeOffset</c> is deliberately absent: .NET refuses it in both
    /// positions.</para></summary>
    private static MarshalExtent IntrinsicMarshalExtent(string fullName, int ptr) => fullName switch
    {
        "System.DateTime" => MarshalExtent.Ok(8, 8),
        "System.TimeSpan" => MarshalExtent.Ok(8, 8),
        "System.DateOnly" => MarshalExtent.Ok(4, 4),
        "System.TimeOnly" => MarshalExtent.Ok(8, 8),
        "System.Decimal" => MarshalExtent.Ok(16, 8),
        "System.Runtime.InteropServices.GCHandle" => MarshalExtent.Ok(ptr, ptr),
        "System.Runtime.DependentHandle" => MarshalExtent.Ok(ptr, ptr),
        _ => MarshalExtent.Unknown,
    };

    /// <summary>One field's contribution: the extent it occupies and the alignment its
    /// offset takes, after its <c>[MarshalAs]</c> descriptor has had its say.</summary>
    private MarshalExtent MarshalFieldExtent(FieldInfo f, bool unicode, int ptr)
    {
        var t = f.Type;
        switch (f.MarshalAs)
        {
            // ByValArray inlines SizeConst elements: SizeConst=0 is refused outright, and an
            // omitted SizeConst marshals as ONE element.
            case UT.ByValArray:
            {
                if (f.MarshalSizeConst == 0)
                    return MarshalExtent.Refused;
                int n = f.MarshalSizeConst < 0 ? 1 : f.MarshalSizeConst;
                if (t.Element is not { } el)
                    return MarshalExtent.Unknown;
                var e = MarshalElementExtent(el, f.MarshalArraySubType, unicode, ptr);
                return e.IsKnown ? MarshalExtent.Ok(e.Size * n, e.Align) : e;
            }
            // ByValTStr inlines a SizeConst-character buffer at the DECLARING type's CharSet
            // width, aligned at the character width; SizeConst=0 is refused.
            case UT.ByValTStr:
            {
                if (!t.IsString || f.MarshalSizeConst <= 0)
                    return MarshalExtent.Refused;
                int w = unicode ? 2 : 1;
                return MarshalExtent.Ok(f.MarshalSizeConst * w, w);
            }
        }

        var natural = MarshalNaturalExtent(t, unicode, ptr);
        if (f.MarshalAs == default)
            return natural;
        return MarshalDescribedExtent(t, f.MarshalAs, natural, unicode, ptr);
    }

    /// <summary>A field's extent from its TYPE alone, with no <c>[MarshalAs]</c> in play.
    /// The CharSet argument is the DECLARING type's and does not reach into a nested struct:
    /// each type's own CharSet governs its own fields, which is why it is passed down one
    /// level and never further.</summary>
    private MarshalExtent MarshalNaturalExtent(TypeDesc t, bool unicode, int ptr)
    {
        switch (t.Kind)
        {
            case TypeKind.Primitive:
                return MarshalPrimitiveExtent(t.Primitive, unicode, ptr);
            // An unmanaged pointer and a function pointer are pointer-wide on both sides.
            case TypeKind.Pointer:
                return MarshalExtent.Ok(ptr, ptr);
            // A GC reference with no descriptor to inline it: .NET refuses a struct with a
            // bare `int[]` field, and refuses a byref one.
            case TypeKind.SZArray or TypeKind.MDArray or TypeKind.ByRef:
                return MarshalExtent.Refused;
            case TypeKind.Class when t.Class is { } fc:
                if (fc.IsInterface)
                    return MarshalExtent.Refused;
                // A delegate marshals as a native function pointer, with or without an
                // explicit [MarshalAs(FunctionPtr)].
                if (fc.IsDelegate)
                    return MarshalExtent.Ok(ptr, ptr);
                // A [StructLayout(Sequential/Explicit)] CLASS field is INLINED BY VALUE at
                // its own marshalled layout — it is not a pointer. An auto-layout class (the
                // C# default, and so every ordinary class, StringBuilder included) is refused
                // by MarshalMemberExtent's gate, which is where that falls out.
                return MarshalMemberExtent(fc, ptr);
            // A type from an assembly nothing loaded, an open generic placeholder, a
            // template: no layout to speak for.
            default:
                return MarshalExtent.Unknown;
        }
    }

    private static MarshalExtent MarshalPrimitiveExtent(PrimitiveTypeCode p, bool unicode, int ptr) => p switch
    {
        PrimitiveTypeCode.SByte or PrimitiveTypeCode.Byte => MarshalExtent.Ok(1, 1),
        PrimitiveTypeCode.Int16 or PrimitiveTypeCode.UInt16 => MarshalExtent.Ok(2, 2),
        PrimitiveTypeCode.Int32 or PrimitiveTypeCode.UInt32 or PrimitiveTypeCode.Single
            => MarshalExtent.Ok(4, 4),
        PrimitiveTypeCode.Int64 or PrimitiveTypeCode.UInt64 or PrimitiveTypeCode.Double
            => MarshalExtent.Ok(8, 8),
        PrimitiveTypeCode.IntPtr or PrimitiveTypeCode.UIntPtr => MarshalExtent.Ok(ptr, ptr),
        // A bool marshals as the 4-byte Win32 BOOL, ALIGNED 4, on every platform: a
        // { byte, bool } struct puts the bool at offset 4 and is 8 bytes. This is the row no
        // sizeof could produce; the representation is 1 byte.
        PrimitiveTypeCode.Boolean => MarshalExtent.Ok(4, 4),
        // A char follows the declaring type's CharSet: 1 byte under Ansi (and under Auto on
        // a POSIX target), 2 under Unicode. Again the opposite direction from the
        // representation, which is 2 bytes always.
        PrimitiveTypeCode.Char => unicode ? MarshalExtent.Ok(2, 2) : MarshalExtent.Ok(1, 1),
        // A string field with no descriptor marshals as a pointer to a buffer.
        PrimitiveTypeCode.String => MarshalExtent.Ok(ptr, ptr),
        // An object field marshals as a COM interface pointer on Windows (8) and is refused
        // on POSIX. This verdict is decided at TRANSPILE time and must not split by host, and
        // a runtime with no COM cannot honour the Windows answer — so the model declines it.
        PrimitiveTypeCode.Object => MarshalExtent.Unknown,
        _ => MarshalExtent.Unknown,
    };

    /// <summary>A field's extent once a <c>[MarshalAs]</c> that is neither <c>ByValArray</c>
    /// nor <c>ByValTStr</c> has been applied. The rule .NET enforces is that a descriptor may
    /// only <b>name</b> the width a field already has, except for the two types whose default
    /// width is genuinely a choice:
    /// <list type="bullet">
    /// <item><c>bool</c> — <c>Bool</c> is the 4-byte default, <c>I1</c>/<c>U1</c> force 1,
    /// and <c>I2</c>, <c>I4</c>, <c>U4</c> and the COM <c>VariantBool</c> are all
    /// REFUSED.</item>
    /// <item><c>char</c> — <c>U1</c> forces 1 and <c>U2</c> forces 2, overriding the
    /// CharSet.</item>
    /// </list>
    /// For every other PRIMITIVE — and for an enum, which marshals as its underlying one — a
    /// width matching AT BOTH POINTER WIDTHS is accepted as the no-op it is and anything else
    /// is refused (<c>[MarshalAs(I4)] int</c> is 4; <c>I2</c> or <c>I8</c> on an
    /// <c>int</c> is <c>ArgumentException</c>). A field of any other KIND takes no width
    /// descriptor at all, whatever width it names. Anything this does not model answers Unknown,
    /// never a number — the permanent COM carve-outs (<c>BStr</c>, <c>SafeArray</c>,
    /// <c>Interface</c>, <c>IDispatch</c>) and <c>LPStruct</c> land there.</summary>
    private MarshalExtent MarshalDescribedExtent(TypeDesc t, UT u, MarshalExtent natural, bool unicode, int ptr)
    {
        // void* rejects width descriptors. FunctionPtr is valid on delegate*, but both
        // decode as Pointer(void), so conservatively refuse descriptors until they differ.
        if (t.Kind == TypeKind.Pointer)
            return MarshalExtent.Refused;
        if (t.Kind == TypeKind.Primitive)
        {
            switch (t.Primitive)
            {
                case PrimitiveTypeCode.Boolean:
                    return u switch
                    {
                        UT.Bool => MarshalExtent.Ok(4, 4),
                        UT.I1 or UT.U1 => MarshalExtent.Ok(1, 1),
                        _ => MarshalExtent.Refused,
                    };
                case PrimitiveTypeCode.Char:
                    // U1 and I1 force 1, U2 and I2 force 2, overriding the declaring type's
                    // CharSet either way.
                    return u switch
                    {
                        UT.U1 or UT.I1 => MarshalExtent.Ok(1, 1),
                        UT.U2 or UT.I2 => MarshalExtent.Ok(2, 2),
                        _ => MarshalExtent.Unknown,
                    };
                case PrimitiveTypeCode.String:
                    // Every pointer-shaped string encoding is one pointer. BStr and the other
                    // COM forms are deliberately absent: they are the standing carve-out, and
                    // a size for a form nothing else in the tree marshals would be the one
                    // number in this file with no second reader.
                    return u is UT.LPStr or UT.LPWStr or UT.LPUTF8Str or UT.LPTStr
                        ? MarshalExtent.Ok(ptr, ptr)
                        : MarshalExtent.Unknown;
            }
        }
        // A width-naming descriptor on anything else: accepted when it names the width the
        // field already has, refused when it contradicts it, Unknown when it is a form this
        // model does not carry (LPStruct on a class, Interface, SafeArray, LPArray on a
        // field). Those last are refusals in .NET too, but answering "unknown" here keeps
        // this file's claims about .NET to the shapes it actually models.
        int named = NamedUnmanagedWidth(u, ptr);
        if (named < 0)
            return u switch
            {
                // `Struct` names the INLINE-STRUCT form, so it is the no-op it claims to be
                // exactly where that form is what the type marshals as — a value type or a
                // [StructLayout] class alike. An enum marshals as its underlying integer and
                // a delegate as a function pointer, so .NET refuses it on both; a shape test
                // lets an enum through, since one is a value type with a known extent.
                UT.Struct when t.Kind == TypeKind.Class =>
                    t.Class is { IsEnum: false, IsDelegate: false } ? natural : MarshalExtent.Refused,
                UT.FunctionPtr when t.Kind == TypeKind.Class && t.Class is { IsDelegate: true }
                    => MarshalExtent.Ok(ptr, ptr),
                _ => MarshalExtent.Unknown,
            };
        if (!natural.IsKnown)
            return natural;
        // A width descriptor asks about the KIND before the number: .NET takes one only on a
        // primitive field, and refuses it on a nested struct, a delegate, a sequential class,
        // GCHandle or DateTime even where it names the width that field already has. An enum
        // is the one non-primitive that takes one, at its underlying integer's width.
        if (t.Kind != TypeKind.Primitive && t.Class is not { IsEnum: true })
            return MarshalExtent.Refused;
        // The descriptor has to name the field's width AT BOTH POINTER WIDTHS, not just at
        // the one being walked. .NET refuses [MarshalAs(U8)] IntPtr and [MarshalAs(SysInt)]
        // long on x64, where the two coincide, so a per-width comparison would measure at 64
        // exactly what it refuses at 32 — and the verdict is the 64-bit walk's alone.
        int other = ptr == PointerWidth.Bytes64 ? PointerWidth.Bytes32 : PointerWidth.Bytes64;
        var alt = MarshalNaturalExtent(t, unicode, other);
        // No natural extent's VERDICT depends on the width, only its number, so a known
        // reading at one width is known at both and this arm does not fire. It stands so
        // that a future width-dependent verdict declines rather than reads alt.Size as 0.
        if (!alt.IsKnown)
            return MarshalExtent.Unknown;
        return named == natural.Size && NamedUnmanagedWidth(u, other) == alt.Size
            ? natural
            : MarshalExtent.Refused;
    }

    /// <summary>The fixed byte width an <c>UnmanagedType</c> names, or -1 when it names no
    /// single width (a pointer form, a COM form, an inline form).</summary>
    private static int NamedUnmanagedWidth(UT u, int ptr) => u switch
    {
        UT.I1 or UT.U1 => 1,
        UT.I2 or UT.U2 => 2,
        UT.I4 or UT.U4 or UT.R4 => 4,
        UT.I8 or UT.U8 or UT.R8 => 8,
        UT.SysInt or UT.SysUInt => ptr,
        _ => -1,
    };

    /// <summary>One element of a <c>[MarshalAs(ByValArray)]</c> field.
    ///
    /// <para><b><c>ArraySubType</c> is honoured for <c>bool</c> and <c>char</c> elements and
    /// IGNORED for every other element type.</b> That is .NET's behaviour, not a
    /// simplification: a <c>bool[3]</c> is 12 bytes by default and 3 with
    /// <c>ArraySubType = I1</c>, a <c>char[3]</c> is 3 (Ansi) and 6 with <c>U2</c>, but an
    /// <c>int[3]</c> is 12 whatever the subtype says. The asymmetry with the scalar rule
    /// above is real: a mismatched subtype on an array element is ignored where the same
    /// descriptor on a scalar field is an <c>ArgumentException</c>.</para></summary>
    private MarshalExtent MarshalElementExtent(TypeDesc el, UT sub, bool unicode, int ptr)
    {
        if (el.Kind == TypeKind.Primitive)
        {
            if (el.Primitive == PrimitiveTypeCode.Boolean)
                return sub is UT.I1 or UT.U1 ? MarshalExtent.Ok(1, 1) : MarshalExtent.Ok(4, 4);
            if (el.Primitive == PrimitiveTypeCode.Char)
                return sub switch
                {
                    UT.U1 or UT.I1 => MarshalExtent.Ok(1, 1),
                    UT.U2 or UT.I2 => MarshalExtent.Ok(2, 2),
                    _ => unicode ? MarshalExtent.Ok(2, 2) : MarshalExtent.Ok(1, 1),
                };
        }
        // Not a bool/char element: the subtype has no say, and the element measures exactly
        // as a field of its own type would. A REFERENCE-typed element is the one place this
        // differs from the field rule — .NET refuses a ByValArray of a sequential CLASS
        // where a bare field of that class is inlined — so it is refused here.
        if (el.Kind == TypeKind.Class && el.Class is { IsValueType: false, IsDelegate: false })
            return MarshalExtent.Refused;
        return MarshalNaturalExtent(el, unicode, ptr);
    }
}

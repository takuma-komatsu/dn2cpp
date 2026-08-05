using System.Reflection.Metadata;

namespace Dn2Cpp;

/// <summary>What real .NET's <c>Marshal.SizeOf</c> does with a type, and whether the
/// number dn2cpp would hand back IS that answer. <c>instanceSize</c> is the
/// REPRESENTATION size — a different quantity from the marshalled one for anything that
/// is not blittable — so it may only be handed out under <see cref="Exact"/>.
///
/// <para>The two refusals stay separate because they carry different exceptions, and
/// collapsing them would make one of the two a lie. <see cref="NotMarshalable"/> is what
/// .NET itself refuses, so it raises .NET's own <c>ArgumentException</c> with .NET's
/// message — oracle-exact. <see cref="SizeUnknown"/> is a type .NET ANSWERS for and dn2cpp
/// cannot: it raises the catchable <c>PlatformNotSupportedException</c> naming the type,
/// the <c>DN2CPP_TF_LAYOUT_UNKNOWN</c> / <c>DN2CPP_TF_METADATA_STRIPPED</c> posture — a
/// DECLARED divergence rather than a claim about .NET.</para></summary>
internal enum MarshalVerdict
{
    /// <summary>A blittable value type with a sequential/explicit layout: the emitted C++
    /// struct's <c>sizeof</c> is exactly .NET's marshalled size, so <c>instanceSize</c> may
    /// be handed out and the static <c>sizeof</c> lowering may stand. This is the case
    /// P/Invoke actually uses.</summary>
    Exact,
    /// <summary>Real .NET raises <c>ArgumentException</c> here — an auto-layout value type
    /// (DateTime, DateTimeOffset, every enum) or one holding a field that has no unmanaged
    /// form at all (an object/array/byref reference).</summary>
    NotMarshalable,
    /// <summary>Real .NET answers a number dn2cpp's model does not have: a field whose
    /// marshalled width differs from its representation (<c>bool</c> is 4 unmanaged and 1
    /// here, <c>char</c> is 1 unmanaged and 2 here), a <c>[MarshalAs]</c> descriptor nothing
    /// models, or a field type the model cannot see through.</summary>
    SizeUnknown,
}

internal sealed partial class Compilation
{
    // ---- the marshalling verdict ----
    //
    // This lives on Compilation rather than on CppEmitter because it has TWO askers and
    // they must agree. The emitter stamps the verdict into the type-info, which decides
    // what the RUNTIME Marshal.SizeOf(Type) answers; MethodCompiler asks the same question
    // to decide whether the generic Marshal.SizeOf<T>() may keep its static `sizeof`
    // lowering or has to route to the runtime verdict. One classifier, because two
    // spellings of one question answering 8 and 16 for the same DateTime is not a
    // divergence anything downstream can detect.

    // Memoized per-class verdicts. Pre-seeded with SizeUnknown before the compute, so a
    // malformed by-value cycle degrades to a refusal rather than recursing — the same guard
    // shape as CppEmitter._structExtents.
    private readonly Dictionary<ClassInfo, MarshalVerdict> _marshalVerdicts = new();

    /// <summary>The marshalling verdict for <paramref name="cls"/>, memoized. Value types
    /// only: a reference type is refused at run time by the absence of
    /// <c>DN2CPP_TF_VALUETYPE</c>, an enum by <c>DN2CPP_TF_ENUM</c>, a closed generic by its
    /// <c>genericArgCount</c>, and a primitive is answered from the runtime's own fixed
    /// table — none of those need a stamp, and all four are asked BEFORE the flags this
    /// feeds (see <c>dn2cpp_marshal_require_size</c>).</summary>
    internal MarshalVerdict MarshalVerdictOf(ClassInfo cls)
    {
        // AutoLayout is a TOP-LEVEL refusal, never an inherited one. .NET refuses
        // Marshal.SizeOf(typeof(DateTime)) and Marshal.SizeOf(typeof(DayOfWeek)) — both
        // AutoLayout — and measures a struct holding a DateTime field at 8 and one holding a
        // DayOfWeek field at 4. So the gate is asked here, at the top, and the field walk
        // below asks MemberMarshalVerdictOf, which does not.
        if (cls.IsAutoLayout)
            return MarshalVerdict.NotMarshalable;
        // A REFERENCE type with an explicit sequential/explicit layout is the one shape .NET
        // measures and dn2cpp cannot: the emitted layout puts the fields behind the
        // Dn2CppObject header, so instanceSize is not the unmanaged size. It is stamped
        // INEXACT (a declared divergence) rather than falling into the runtime's
        // "not a value type" arm, which would claim .NET refuses it — it does not.
        if (!cls.IsValueType && !cls.IsEnum)
            return MarshalVerdict.SizeUnknown;
        return MemberMarshalVerdictOf(cls);
    }

    /// <summary>The verdict for a type appearing as a FIELD of something else — the same walk
    /// as <see cref="MarshalVerdictOf"/> without the top-level AutoLayout and reference-type
    /// gates, because neither of those is inherited (see the note there).</summary>
    private MarshalVerdict MemberMarshalVerdictOf(ClassInfo cls)
    {
        if (_marshalVerdicts.TryGetValue(cls, out var cached))
            return cached;
        _marshalVerdicts[cls] = MarshalVerdict.SizeUnknown;
        MarshalVerdict v;
        try
        {
            v = ComputeMarshalVerdict(cls);
        }
        catch (NotSupportedException e) when (!IsMustEscape(e))
        {
            // Same contract as CppEmitter.TryStructExtent's catch: a shape the layout model
            // rejects loudly on the emission path has no marshalled size either, and
            // "unknown" is the honest answer. Nothing is muted — a type this walk sees runs
            // the throwing calls itself during its own body emission.
            v = MarshalVerdict.SizeUnknown;
        }
        _marshalVerdicts[cls] = v;
        return v;
    }

    private MarshalVerdict ComputeMarshalVerdict(ClassInfo cls)
    {
        // An AutoLayout type reaching here is a MEMBER (the top-level gate is in
        // MarshalVerdictOf). .NET marshals such a member, at a layout it chooses and dn2cpp
        // does not model — for DateTime that is an 8-byte unmanaged form whose FIELD ORDER
        // is .NET's own business, which the matching 8-byte representation does not settle —
        // so it is unknown, not refused.
        if (cls.IsAutoLayout)
            return MarshalVerdict.SizeUnknown;
        // An intrinsic value type has no transpiled field rows to walk — its C++ body is a
        // hand-written runtime struct — so the walk below would read it as field-less and
        // answer Exact about a layout it never saw. The four whose representation DOES equal
        // .NET's marshalled size answer from their own hand-written type-infos
        // (dn2cpp_system_datetime.cpp / dn2cpp_system_decimal.cpp); the emitted shell here
        // refuses.
        if (cls.IntrinsicCppName is not null)
            return MarshalVerdict.SizeUnknown;
        // MembersReady-guarded for the ModelCensus reason: reading the field rows of an
        // uncompleted specialization just to answer this would defeat the deferral (and
        // throw under DN2CPP_STRICT_COMPLETION). An OPAQUE shell is deliberately NOT
        // excluded — its field ROWS exist even though no field was emitted, the shell is
        // padded to the extent they describe, and for a blittable struct that extent IS
        // the marshalled size.
        if (!cls.MembersReady)
            return MarshalVerdict.SizeUnknown;
        bool inexact = false;
        foreach (var f in cls.Fields)
        {
            if (f.IsStatic)
                continue;
            switch (FieldMarshalVerdict(f))
            {
                case MarshalVerdict.NotMarshalable:
                    return MarshalVerdict.NotMarshalable;
                case MarshalVerdict.SizeUnknown:
                    inexact = true;
                    break;
            }
        }
        return inexact ? MarshalVerdict.SizeUnknown : MarshalVerdict.Exact;
    }

    /// <summary>The verdict a single instance field contributes to its declaring struct.
    /// <b>Exact means "this field occupies the same bytes unmanaged as it does here"</b>,
    /// which is a stronger claim than "the field can be marshalled" — a <c>bool</c> or a
    /// <c>char</c> field marshals perfectly well and still moves the struct's size, so it is
    /// SizeUnknown, not NotMarshalable.</summary>
    private MarshalVerdict FieldMarshalVerdict(FieldInfo f)
    {
        // A [MarshalAs] descriptor rewrites the field's unmanaged form wholesale
        // (ByValArray inlines N elements, ByValTStr inlines a character buffer, Bool picks a
        // width). dn2cpp models exactly one of them for LAYOUT (ByValArraySize) and none of
        // them for SIZE, so any descriptor at all takes the struct out of Exact rather than
        // being read past.
        if (f.ByValArraySize >= 0 || HasFieldMarshalDescriptor(f))
            return MarshalVerdict.SizeUnknown;
        var t = f.Type;
        switch (t.Kind)
        {
            case TypeKind.Primitive:
                return MarshalPrimitiveVerdict(t.Primitive);
            // An unmanaged pointer and a function pointer are 8 bytes on both sides.
            case TypeKind.Pointer:
                return MarshalVerdict.Exact;
            // A GC reference: an array, a byref, a managed object. No unmanaged struct form.
            case TypeKind.SZArray or TypeKind.MDArray or TypeKind.ByRef:
                return MarshalVerdict.NotMarshalable;
            case TypeKind.Class when t.Class is { } fc:
                // An enum FIELD marshals at its underlying width — .NET refuses an enum only
                // as the top-level argument (its AutoLayout), never as a member.
                if (fc.IsEnum)
                    return MarshalPrimitiveVerdict(fc.EnumUnderlying);
                if (!fc.IsValueType)
                    return MarshalVerdict.NotMarshalable;
                return MemberMarshalVerdictOf(fc);
            // A type from an assembly nothing loaded, an open generic placeholder, a
            // template: no layout to speak for.
            default:
                return MarshalVerdict.SizeUnknown;
        }
    }

    private static MarshalVerdict MarshalPrimitiveVerdict(PrimitiveTypeCode p) => p switch
    {
        PrimitiveTypeCode.SByte or PrimitiveTypeCode.Byte
            or PrimitiveTypeCode.Int16 or PrimitiveTypeCode.UInt16
            or PrimitiveTypeCode.Int32 or PrimitiveTypeCode.UInt32
            or PrimitiveTypeCode.Int64 or PrimitiveTypeCode.UInt64
            or PrimitiveTypeCode.Single or PrimitiveTypeCode.Double
            or PrimitiveTypeCode.IntPtr or PrimitiveTypeCode.UIntPtr
            => MarshalVerdict.Exact,
        // Marshalled, and at a different width than the representation: Marshal.SizeOf of a
        // one-bool struct is 4 (the Win32 BOOL default) against our 1, and of a one-char
        // struct is 1 (the default Ansi CharSet) against our 2.
        PrimitiveTypeCode.Boolean or PrimitiveTypeCode.Char => MarshalVerdict.SizeUnknown,
        // A string field marshals to a pointer, which is our width too — but WHICH pointer
        // depends on the declaring type's CharSet, and a ByValTStr descriptor inlines it
        // instead. Refuse rather than lean on an agreement that holds only for the default
        // of an axis dn2cpp does not read.
        PrimitiveTypeCode.String => MarshalVerdict.SizeUnknown,
        // object marshals as a VARIANT where it marshals at all; a struct holding an object
        // field is refused outright.
        PrimitiveTypeCode.Object => MarshalVerdict.NotMarshalable,
        _ => MarshalVerdict.SizeUnknown,
    };

    /// <summary>Whether the field's metadata row carries a marshalling descriptor
    /// (<c>FieldAttributes.HasFieldMarshal</c>, the <c>[MarshalAs]</c> blob). Read from the
    /// row rather than from a decoded value: the verdict only needs to know that one EXISTS,
    /// and decoding the blob would model an unmanaged form dn2cpp does not implement.</summary>
    private static bool HasFieldMarshalDescriptor(FieldInfo f)
    {
        try
        {
            var fd = f.DeclaringClass.Module.Reader.GetFieldDefinition(f.Handle);
            return (fd.Attributes & System.Reflection.FieldAttributes.HasFieldMarshal) != 0;
        }
        catch (Exception e) when (!IsMustEscape(e))
        {
            // A synthesized field carries no readable row; it also carries no descriptor.
            return false;
        }
    }

    /// <summary>The constant real .NET's <c>Marshal.SizeOf</c> answers for a type whose size
    /// the model knows OUTRIGHT, or null when the question has to go to the runtime verdict.
    /// The mirror of <c>marshal_known_size</c> in <c>dn2cpp_marshal.cpp</c>, and it exists
    /// for the same reason: for the small primitives a C++ <c>sizeof</c> is NOT the
    /// marshalled size. Two of the rows are not widths at all — <c>bool</c> is 4 (the Win32
    /// BOOL default) and <c>char</c> is 1 (the default Ansi CharSet) — so the static
    /// lowering could never have derived them.</summary>
    internal static int? MarshalKnownSize(TypeDesc t)
    {
        if (t.Kind != TypeKind.Primitive)
            return null;
        return t.Primitive switch
        {
            PrimitiveTypeCode.SByte or PrimitiveTypeCode.Byte => 1,
            PrimitiveTypeCode.Int16 or PrimitiveTypeCode.UInt16 => 2,
            PrimitiveTypeCode.Int32 or PrimitiveTypeCode.UInt32 or PrimitiveTypeCode.Single => 4,
            PrimitiveTypeCode.Int64 or PrimitiveTypeCode.UInt64 or PrimitiveTypeCode.Double => 8,
            PrimitiveTypeCode.IntPtr or PrimitiveTypeCode.UIntPtr => 8,
            PrimitiveTypeCode.Boolean => 4,
            PrimitiveTypeCode.Char => 1,
            PrimitiveTypeCode.Void => 1,
            // String and Object are marshalable-as-arguments but refused as a SizeOf
            // argument; leave them to the runtime verdict, which raises .NET's exception.
            _ => null,
        };
    }
}

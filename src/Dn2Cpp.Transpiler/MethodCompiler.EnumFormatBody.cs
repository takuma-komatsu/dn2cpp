using System.Reflection.Metadata;

namespace Dn2Cpp;

/// <summary>Synthesizes the bodies of System.Enum's intercepted instance methods — the four
/// ToString overloads, GetTypeCode, the value-comparison overrides Equals(object) /
/// GetHashCode() / CompareTo(object), and the private GetValue() (the boxer behind every
/// IConvertible.To* override) — whose real IL reachability replaces (see
/// CoreIntrinsics.BrEnumInstanceFormat). Each is emitted under the method's own name and
/// signature, so EVERY mouth resolves to it: a direct callvirt on a System.Enum-typed
/// receiver (devirtualized in MethodCompiler.Call.cs, since enums carry no C++ vtable), the
/// IFormattable/IConvertible/IComparable itable slot on a boxed enum
/// (CppEmitter.RenderInterfaceTable), and a boxed constrained dispatch. What the SCAN cut
/// deletes are the two cascades behind those bodies: GetEnumInfo → GetEnumValuesAndNames (an
/// InternalCall) plus the RuntimeType-cache reflection for the formatters, and an
/// InternalGetCorElementType switch into inline-only intrinsic members for the value trio.
///
/// <para>Each body lowers to a runtime helper that dispatches on the boxed enum's runtime
/// type (<c>this-&gt;type</c>) and reads the payload at the underlying's width, so ONE body
/// serves every enum at every storage width byte..ulong: ToString to
/// <c>dn2cpp_object_tostring_virtual</c> (the member-name formatter; the <em>virtual</em>
/// entry point, because the plain one folds null to string.Empty for its concat callers), the
/// format overloads to <c>dn2cpp_enum_format</c> ("G"/"F"/"D"/"X"; an empty/null format is
/// "G", the provider ignored — enum formatting is culture-independent), GetTypeCode to
/// <c>dn2cpp_type_get_type_code</c>, the value trio to <c>dn2cpp_object_gethashcode</c> /
/// <c>dn2cpp_object_equals</c> / <c>dn2cpp_enum_compareto</c>, and GetValue to
/// <c>dn2cpp_enum_get_value</c> (re-boxing the payload under the underlying primitive's
/// type-info, matching <c>box &lt;underlying&gt;</c>).</para></summary>
internal sealed partial class MethodCompiler
{
    /// <summary>Compiles the synthesized body for one intercepted System.Enum instance
    /// method. Cut by NAME (BrEnumInstanceFormat gates on ToString/GetTypeCode), lowered by
    /// SHAPE here, and TOTAL over the set: an unmodeled overload throws rather than emitting a
    /// body that names the cut IL — the reach cut has already deleted the real body, so there
    /// is nothing to fall back to.</summary>
    internal string CompileEnumInstanceFormatBody()
    {
        // a0 = this (the boxed enum, Dn2CppObject* — System.Enum is a reference type).
        const string recv = "((Dn2CppObject*)a0)";
        var ps = _method.Signature.ParameterTypes;
        var rt = _method.Signature.ReturnType;
        switch (_method.Name, ps.Length)
        {
            case ("ToString", 0):
                // ToString() — the member-name formatting (Object.ToString on a boxed enum),
                // served from the type-info tostring slot (enumtostr_*).
                Emit($"return dn2cpp_object_tostring_virtual({recv});");
                break;
            case ("ToString", 1) when ps[0] is { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.String }:
                // ToString(string format).
                EmitEnumFormatReturn(recv, "a1");
                break;
            case ("ToString", 1):
                // ToString(IFormatProvider) — [Obsolete]; the provider is ignored, so it is
                // exactly ToString().
                Emit($"return dn2cpp_object_tostring_virtual({recv});");
                break;
            case ("ToString", 2):
                // ToString(string format, IFormatProvider) — [Obsolete]; a1 is the format,
                // a2 the ignored provider (real .NET delegates to ToString(format)).
                EmitEnumFormatReturn(recv, "a1");
                break;
            case ("GetTypeCode", 0):
                // GetTypeCode() — the underlying primitive's TypeCode. dn2cpp_type_get_type_code
                // unwraps the enum to its underlying and maps the name to the code.
                Emit($"return ({CppTypes.Of(rt)})dn2cpp_type_get_type_code({recv}->type);");
                break;
            case ("GetHashCode", 0):
                // GetHashCode() — the boxed enum's value hash. dn2cpp_object_gethashcode falls to
                // its boxed-enum arm (enums wire no gethashcode type-info slot, being value types,
                // so there is no recursion), which reads the payload at the underlying's width and
                // folds a 64-bit one low^high exactly like Enum.GetHashCode.
                Emit($"return dn2cpp_object_gethashcode({recv});");
                break;
            case ("Equals", 1):
                // Equals(object) — two boxed enums are equal iff same enum type and same
                // underlying value; dn2cpp_object_equals's boxed-enum arm answers exactly that at
                // the underlying's width.
                Emit($"return ({CppTypes.Of(rt)})dn2cpp_object_equals({recv}, (Dn2CppObject*)a1);");
                break;
            case ("CompareTo", 1):
                // CompareTo(object) — order by the underlying value (signed or unsigned per the
                // underlying type), the -1/0/1 sign Enum.CompareTo returns. dn2cpp_enum_compareto
                // reads both payloads at the underlying's width and throws ArgumentException on an
                // enum-type mismatch, matching .NET.
                Emit($"return ({CppTypes.Of(rt)})dn2cpp_enum_compareto({recv}, (Dn2CppObject*)a1);");
                break;
            case ("GetValue", 0):
                // GetValue() — the private boxer every IConvertible.To* override calls
                // (Convert.ToXxx(GetValue(), …)). dn2cpp_enum_get_value re-boxes the payload
                // under the UNDERLYING primitive's type-info, not the enum's, matching .NET's
                // `e.GetValue().GetType() == underlying`.
                Emit($"return dn2cpp_enum_get_value({recv});");
                break;
            default:
                // TOTAL over the BrEnumInstanceFormat name set: the reach cut deleted the real
                // body, so an unmodeled overload must throw rather than fall through to a symbol
                // nothing defines.
                throw new NotSupportedException(
                    $"{_method.DeclaringClass.FullName}.{_method.Name}: unmodeled System.Enum "
                    + $"instance ToString/GetTypeCode/Equals/GetHashCode/CompareTo/GetValue overload "
                    + $"({ps.Length} parameter(s)). The reach cut (CoreIntrinsics.BrEnumInstanceFormat) "
                    + "deletes its real IL, so this shape must be modeled here or removed from "
                    + "IsEnumInstanceFormatMethod.");
        }
        return RenderWrapperBody("dn2cpp enum value/format shim");
    }

    /// <summary>Emits <c>return dn2cpp_enum_format(...)</c> for an enum format overload: the
    /// reflection Type* comes from the boxed enum's runtime type-info, the value is the box
    /// itself, and an empty/null format collapses to "G" (Enum.ToString(string) treats
    /// null/empty as the default format).</summary>
    private void EmitEnumFormatReturn(string recv, string fmt)
    {
        Emit($"return dn2cpp_enum_format(dn2cpp_get_type_from_handle({recv}->type), {recv}, "
            + $"({fmt} == nullptr || {fmt}->length == 0) ? dn2cpp_string_from_utf8(\"G\", 1) : {fmt});");
    }
}

#!/usr/bin/env bash
# Consolidated Convert/Parse gate: Convert.To* surface, base conversions, Base64,
# Convert from object, Convert.ChangeType (+ extended + UInt64 TypeCode), and
# primitive Parse, including UTF-8 and span float parsing across fast, slow,
# subnormal, and overflow paths. Also System.Uri basics (TryCreate/ctor + component
# properties, ASCII hosts), transpiled from the real System.Private.Uri —
# hence the extra BCL reference. Also the System.ComponentModel TypeConverter
# surface (ArrayConverter / CollectionConverter ConvertTo + the
# GetConvertToException path), transpiled from the real
# System.ComponentModel.TypeConverter — its methods open with the BCL's
# bool-returning SR.UsingResourceKeys() guard, asserting the SR intrinsic
# pushes a bool for it instead of dropping the return value.
#
# Also integer-primitive TryFormat (TryFormatSubset, folded in from its own gate,
# with the three sections it already drove from its own tail). dn2cpp's own
# Compilation.Build SRM-decodes field signatures; SignatureDecoder.CheckHeader
# reaches Byte.TryFormat, so before that slice every integer primitive's real body
# routed through System.Number.TryFormat* (Int32ToHexChars / UInt32ToDecChars→
# Math.DivRem / NumberFormatInfo / IUtfChar.CastFrom / Span.TryCopyTo→
# Buffer.BulkMove / FastAllocateString / UInt32.LeadingZeroCount) — the dominant
# remaining console-self-host cascade. MethodCompiler lowers all eight integer
# primitives' TryFormat to dn2cpp_try_format_int|uint (the same byteWidth-aware
# formatters as ToString(format)), copying into the Span<char> buffer with .NET
# semantics (fits => write + charsWritten + true; too short => buffer untouched +
# charsWritten=0 + false); Compilation.ResolveCallTarget cuts the real bodies from
# reachability. The section also covers the .NET 8+ "B"/"b" binary specifier
# (BinaryFormatSubset), the shape-identical IUtf8SpanFormattable.TryFormat(
# Span<byte>, ...) UTF-8 twin (Utf8TryFormatSubset — direct + constrained-generic
# calls; the destination span's element type is the only discriminator between the
# two interface methods) and the float overload (FloatTryFormatSubset). The retired
# project set <InvariantGlobalization>true</…>; this one cannot — its NumberStyles /
# FloatParse sections assert real de-DE and fr-FR separators, which ICU-off would
# answer invariant — so the folded section pins CurrentCulture itself, for its own
# duration, at the tail of the driver. See the comment at that pin. That per-section
# pin is not the whole story: the oracle reads CurrentCulture for every
# double this bucket PRINTS, not only for the one "N0" that section formats, so the
# driver pins invariant for the whole program and the section's scope is now a
# statement of its own requirement rather than the thing that makes the gate green.
#
# Diffed exactly vs real .NET.
# Former gates: convert, convert-base, convert-base64, convert-object,
# convert-changetype, convert-changetype-ext, convert-changetype-uint64, parse,
# try-format-subset, float-parse-format-info.
# Convert validates Base64/Hex slices, Boolean text, radix syntax, empty result
# identity, and named faults with boxed bounds and UTF-16 messages.
# Boolean span Parse/TryParse and upper/lower hex encoding cover both UTF-16 and
# UTF-8 destinations, including short buffers that must remain untouched.
# Scalar Parse/TryParse overloads cover every string/UTF-16/UTF-8 entry point,
# native integers, Half, Decimal and wide integers, invalid styles and custom symbols.
source "$(dirname "$0")/_common.sh"

gate_extra_asserts() {
    local out="$1" native before prefix line
    native=$(run_bounded "./$out/ConvertParse")
    native=$(strip_cr_win "$native")
    before=$(dotnet "$_CG_APP" before-char-faults)
    before=$(strip_cr_win "$before")
    prefix=$(awk '/^== Convert string char faults ==$/ { exit } { print }' <<< "$native")
    assert_output "$prefix" "$before"
    for line in '== Convert string char faults ==' \
        'char:0 param=value' \
        'char provider:0 param=value' \
        'char:1 type=FormatException' \
        'char:4 success' \
        'char:5 success' \
        'char after GC param=value' \
        'char object:2 success' \
        'object unit=97' \
        'object unit=55' \
        'char object:4 type=FormatException' \
        'Convert string char faults end'; do
        grep -Fxq -- "$line" <<< "$native" \
            || { echo "FAIL: ConvertParse validation witness missing: $line" >&2; exit 1; }
    done
    before=$(dotnet "$_CG_APP" before-conversion-fields)
    before=$(strip_cr_win "$before")
    prefix=$(awk '/^== Conversion validation fields ==$/ { exit } { print }' <<< "$native")
    assert_output "$prefix" "$before"
    for line in '== Conversion validation fields ==' \
        'base64 range:2:-1:-1:2 param=length' \
        'base64 chars:2:2:0:0:2147483647:0 success=0' \
        'try null string:4:0 success=False:0' \
        'try null string:4:4 type=NullReferenceException' \
        'empty base64 array=True' \
        'empty from hex=True' \
        'empty from base64 string=False' \
        'empty from base64 chars=True' \
        'white from base64 chars=False' \
        'conversion after GC:0 type=FormatException' \
        'conversion after GC:3 type=OverflowException' \
        'Conversion validation fields end'; do
        grep -Fxq -- "$line" <<< "$native" \
            || { echo "FAIL: ConvertParse validation witness missing: $line" >&2; exit 1; }
    done
    before=$(dotnet "$_CG_APP" before-span-conversions)
    before=$(strip_cr_win "$before")
    prefix=$(awk '/^== Boolean spans and hex destinations ==$/ { exit } { print }' <<< "$native")
    assert_output "$prefix" "$before"
    for line in '== Boolean spans and hex destinations ==' \
        'bool span:0 success=False:False' \
        'bool span:4 success=True:True' \
        'bool span:5 success=True:False' \
        'bool slice=True' \
        'hex chars:5:False=False:0:?????' \
        'hex chars:6:True=True:6:00abff' \
        'hex bytes:6:False=True:6:30-30-41-42-46-46' \
        'empty hex chars=True:0' \
        'empty hex bytes=True:0' \
        'Boolean spans and hex destinations end'; do
        grep -Fxq -- "$line" <<< "$native" \
            || { echo "FAIL: span conversion witness missing: $line" >&2; exit 1; }
    done
    before=$(dotnet "$_CG_APP" before-hex-decoding)
    before=$(strip_cr_win "$before")
    prefix=$(awk '/^== hex decoding overloads ==$/ { exit } { print }' <<< "$native")
    assert_output "$prefix" "$before"
    for line in 'hex array utf8:00aBff success=00-AB-FF' \
        'hex decode utf8:00aBff:2=DestinationTooSmall:4:2:00-AB' \
        'empty hex chars array=True' 'empty hex utf8 array=True' \
        'hex decoding overloads end'; do
        grep -Fxq -- "$line" <<< "$native" \
            || { echo "FAIL: hex decoding witness missing: $line" >&2; exit 1; }
    done
    before=$(dotnet "$_CG_APP" before-primitive-parses)
    before=$(strip_cr_win "$before")
    prefix=$(awk '/^== Primitive parse overloads ==$/ { exit } { print }' <<< "$native")
    assert_output "$prefix" "$before"
    for line in 'Boolean:Parse:chars=True' 'Char:Parse:string=漢' \
        'Primitive parse overloads end'; do
        grep -Fxq -- "$line" <<< "$native" \
            || { echo "FAIL: scalar parsing witness missing: $line" >&2; exit 1; }
    done
    local type
    for type in SByte Byte Int16 UInt16 Int32 UInt32 Int64 UInt64 IntPtr UIntPtr Single Double Half Decimal; do
        grep -Fxq -- "$type:TryParse:utf8/styles/provider/out=True:42" <<< "$native" \
            || { echo "FAIL: scalar UTF-8 parsing did not run: $type" >&2; exit 1; }
    done
    for type in Int128 UInt128; do
        grep -Fxq -- "$type:TryParse:utf8/styles/provider/out=True:0:42" <<< "$native" \
            || { echo "FAIL: wide UTF-8 parsing did not run: $type" >&2; exit 1; }
    done
}

corelib_diff_gate ConvertParse System.Private.Uri System.ComponentModel.TypeConverter

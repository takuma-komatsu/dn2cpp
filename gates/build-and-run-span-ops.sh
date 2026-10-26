#!/usr/bin/env bash
# Consolidated span gate: Span<T> bulk ops, scanning, sort, instance methods,
# IndexOfAny, ReadOnlySpan<byte>, and MemoryMarshal.CreateSpan. Diffed exactly vs
# real .NET.
#
# Also the MemoryMarshal-over-byte-spans section (MemoryMarshalSubset, folded in
# from its own gate). GetReference / Cast / AsBytes / Read / Write / CreateSpan /
# CreateReadOnlySpan are transpiler intrinsics (the real BCL bodies use JIT
# intrinsics we don't model: IsReferenceOrContainsReferences / Unsafe.As / nuint
# math); the transpiler builds the result span's {f__reference, f__length} struct
# directly (widths via StorageOf). TryRead / TryWrite / AsRef transpile from the
# real CoreLib IL — their bodies reduce to Unsafe.* + GetReference, all of which
# the transpiler resolves. Covers narrow/widen Cast, AsBytes, a writable
# GetReference, Read/Write + TryRead/TryWrite round-trips (including the
# too-short-buffer false paths), AsRef over Span/ReadOnlySpan, and CreateSpan over
# a single ref. Reinterpretation byte order is host-endian — the gate compares the
# transpiled binary against real .NET on the same machine, so they agree. Also the
# non-generic GetArrayDataReference(Array) overload (an intrinsic — the real body
# is RawData + MethodTable pointer math): ref byte to element 0 of byte[]/int[]/
# string[] SZ arrays, identity-checked against the generic form and read/written
# through Unsafe.*Unaligned / Unsafe.As. Rank >= 2 operands and the intra-CoreLib
# (MethodDefinition) callers of that overload are ArrayCore's
# ArrayDataRefMdSubset, not this bucket's. That section
# used to assert a hard-coded expected string; folded here it is exact-diffed
# against real .NET instead, i.e. it gained a stronger oracle than it had.
#
# CoreLib only — no extra BCL reference is needed by any section.
# Former gates: span-bulk, span-scan, span-sort, span-instance, span-indexofany,
# readonlyspan-byte, create-span, memorymarshal-subset.
# Decimal storage is observable through AsBytes and MemoryMarshal Read/Write;
# raw bytes and bit-exact round-trips include signed zero and trailing scale.
# Overlapping CopyTo/TryCopyTo cover scalar storage and GC references.
source "$(dirname "$0")/_common.sh"

DN2CPP_GATE_EXTRA_CONTEXT="${DN2CPP_GATE_EXTRA_CONTEXT:-}|decimal-layout-prefix:before-decimal-layout"
gate_extra_asserts() {
    local out="$1" output oracle before_native before_oracle prefix line
    output="$out/native.stdout"
    oracle="$out/oracle.stdout"
    gate_capture_ok "$out/assert-before_native-1" run_bounded "./$out/SpanOps" before-decimal-layout
    before_native="$out/assert-before_native-1.stdout"
    gate_capture_ok "$out/assert-before_oracle-2" run_bounded dotnet "$_CG_APP" before-decimal-layout
    before_oracle="$out/assert-before_oracle-2.stdout"

    gate_output compare "$before_native" "$before_oracle"
    prefix="$out/assert-prefix-5.stdout"
    gate_output select "$output" "$prefix" --before '== decimal raw layout ==' --policy crlf
    gate_output compare "$prefix" "$before_native"
    prefix="$out/assert-prefix-6.stdout"
    gate_output select "$oracle" "$prefix" --before '== decimal raw layout ==' --policy crlf
    gate_output compare "$prefix" "$before_oracle"
    for line in '== decimal raw layout ==' 'decimal raw bytes=80' \
        'negative-zero original bits=00000000,00000000,00000000,80030000' \
        'negative-zero raw-read bits=00000000,00000000,00000000,80030000' \
        'negative-zero write-read bits=00000000,00000000,00000000,80030000' \
        'decimal raw layout end'; do
        gate_output byte-witness "$output" "$line" --count any --policy crlf \
            || { echo "FAIL: SpanOps decimal layout witness missing: $line" >&2; exit 1; }
    done
    gate_capture_ok "$out/assert-before_native-3" run_bounded "./$out/SpanOps" before-span-overlap
    before_native="$out/assert-before_native-3.stdout"
    gate_capture_ok "$out/assert-before_oracle-4" run_bounded dotnet "$_CG_APP" before-span-overlap
    before_oracle="$out/assert-before_oracle-4.stdout"
    gate_output compare "$before_native" "$before_oracle"
    prefix="$out/assert-prefix-7.stdout"
    gate_output select "$output" "$prefix" --before '== overlapping span copies ==' --policy crlf
    gate_output compare "$prefix" "$before_native"
    for line in '== overlapping span copies ==' 'bool 0:1:5:0=True' \
        'UInt128 0:1:5:3=True' 'string 0:1:5:0=True' \
        'reference struct 1:0:5:3=True' 'int short span=False' \
        "int short copy=destination:Destination is too short. (Parameter 'destination')" \
        'overlapping span copies end'; do
        gate_output byte-witness "$output" "$line" --count any --policy crlf \
            || { echo "FAIL: overlapping span witness missing: $line" >&2; exit 1; }
    done
}

corelib_diff_gate SpanOps

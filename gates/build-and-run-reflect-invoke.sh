#!/usr/bin/env bash
# Consolidated reflection-invocation gate. Merges the former reflect dynamic-use
# subset gates into one multi-section program, transpiled once against the
# tree-shaken real CoreLib and diffed exactly against real .NET. Covers:
#   MethodInfo.Invoke (instance/static, args, return boxing, void, private),
#   delegate/interface dynamic dispatch via reflection, FieldInfo.GetValue/SetValue
#   (instance/static/value-type/unbox), and a reflection-driven serializer
#   (attribute-named members + enum names).
# Also covers reflected member-handle IDENTITY (ReflectMemberIdentitySubset):
# the runtime interns Field/Method/Property/ConstructorInfo wrappers per
# metadata row like real .NET's RuntimeType member cache, so ReferenceEquals /
# virtual Equals / GetHashCode / HashSet dedup / List<MemberInfo>.Contains
# agree across repeated Get* calls — including the Newtonsoft
# GetSerializableMembers two-enumeration Contains-selection shape whose
# fresh-handle failure silently dropped every unattributed public member from
# the serialization contract (Thrive's MembraneType boot blocker).
# Two non-reflecting sections live here because the surface they exercise is the
# same "the real body reflects, so it is lowered inline" lane: ActivatorSubset
# (Activator.CreateInstance<T> / the new() constraint idiom, including an
# intrinsic-mapped reference T whose ctor is never transpiled and an intrinsic
# value T) and EventSubset (field-like `event` += / -= / invoke, whose
# compiler-generated accessors run through Interlocked.CompareExchange, plus the
# integral Interlocked overloads).
# MemberwiseCloneSubset's section 5 is in this bucket for the CoreLib surface
# it needs, not for its theme: its SUBJECT is the runtime's instance-extent model, and
# what it asserts is that a clone of an INTRINSIC-represented reference type — a
# StringBuilder, an exception (the opaque shells, whose extent is derived from the
# ALLOCATOR's floor rather than from a stamped number), a CancellationTokenSource, a
# ThreadLocal<T>, a Type handle, a CultureInfo — has the same shallow-copy semantics
# real .NET gives. A truncated clone would still print a plausible line for most of
# them, which is why the exception rows read a field that lives PAST the header. The
# seven types dn2cpp still refuses are frozen in the reflect-types bucket
# (ReflectShallowCloneRefusalSubset); the finalizability of a clone is asserted in the
# finalizers bucket (FinalizerClonedSubset).
# GetInterfaceSubset asserts Type.GetInterface(name[, ignoreCase]) against
# real .NET: simple/namespace-qualified matching, ignoreCase folding the simple-name
# part ONLY (a wrong-cased namespace misses even under ignoreCase), closed generics
# matched by the definition's mangled simple name, AmbiguousMatchException on two
# matching rows, null on no match, ArgumentNullException on a null name. Its tail is
# a second subject: the single-attribute getters (Attribute.GetCustomAttribute, the
# CustomAttributeExtensions member and Assembly forms) throw a catchable — and
# exactly-typed — AmbiguousMatchException when a base-typed filter matches two
# attribute rows, member-level and assembly-level both.
# ReflectedTypeSubset asserts MemberInfo.ReflectedType and the
# (row, reflectedType)-keyed handle identity it forces: typeof(D).GetMethod(m) !=
# typeof(Base).GetMethod(m) for an inherited m (==, .Equals, HashSet count 2)
# while same-type queries stay ReferenceEquals-identical (the Newtonsoft Contains
# selection); plus the mint-side normalizations measured on real .NET —
# delegate.Method and GetBaseDefinition answer the DECLARING-typed instance,
# MakeGenericMethod propagates the receiver's reflected type where
# GetGenericMethodDefinition normalizes it away, a property's GetGetMethod
# inherits the property handle's reflected type, and ParameterInfo.Member is the
# very instance GetParameters was called on.
# ReflectToStringSubset asserts MethodInfo/ConstructorInfo/FieldInfo/PropertyInfo/
# ParameterInfo and CustomAttributeData signature display through typed, base, and
# object dispatch, including byref, indexer, generic-method, and attribute arguments.
# Mixed native/packed metadata preserves inherited members, closed generics,
# parameter identity, and interface receiver dispatch across cache eviction.
# Disabling compression forces native metadata even for explicit packed selectors.
# NoCompressMetadata and derived attributes select native owner/member metadata
# through class inheritance without changing containing types or interface users.
# Former gates: reflect-invoke, reflect-dispatch, reflect-field-value,
# reflect-serializer, activator-subset, event-subset.
source "$(dirname "$0")/_common.sh"

py="$(resolve_python)"
DN2CPP_GATE_EXTRA_INPUTS="${DN2CPP_GATE_EXTRA_INPUTS:-} gates/fixtures/check-reflection-layout.py gates/measure-reflection-metadata.py gates/expected/reflection-allocations.csv"
gate_extra_asserts() {
    local out="$1"
    "$py" gates/fixtures/check-reflection-layout.py "$out" "$reflection_layout_axis"
    run_bounded "$out/ReflectInvoke$EXE_EXT" > "$out/metadata-layout.stdout"
    grep -Fxq 'metadata-layout-begin' "$out/metadata-layout.stdout"
    grep -Fxq 'metadata-layout-cache-capacity=72/1296' "$out/metadata-layout.stdout"
    grep -Fxq 'metadata-layout-cache-threads=1296/1296' "$out/metadata-layout.stdout"
    grep -Fxq 'metadata-layout-interface-receivers=21000' "$out/metadata-layout.stdout"
    grep -Fxq 'metadata-layout-end' "$out/metadata-layout.stdout"
    grep -Fxq 'metadata-compression-begin' "$out/metadata-layout.stdout"
    grep -Fxq 'metadata-compression-labels=field/property/constructor/method/parameter' "$out/metadata-layout.stdout"
    grep -Fxq 'metadata-compression-inheritance=v5/Direct' "$out/metadata-layout.stdout"
    grep -Fxq 'metadata-compression-generic-value=15/Int32/True' "$out/metadata-layout.stdout"
    grep -Fxq 'metadata-compression-generic-reference=text/String/True' "$out/metadata-layout.stdout"
    grep -Fxq 'metadata-compression-plain-generic=True/True' "$out/metadata-layout.stdout"
    grep -Fxq 'metadata-compression-end' "$out/metadata-layout.stdout"

    # Enforce each operation's first and repeated allocation budget independently.
    # The capture reports time too, but timing is not a pass/fail threshold.
    DN2CPP_REFLECTION_MEASURE=1 run_bounded "$out/ReflectInvoke$EXE_EXT" > "$out/allocations.csv"
    if ! "$py" gates/measure-reflection-metadata.py --check-allocation-limits \
            gates/expected/reflection-allocations.csv "$out/allocations.csv" \
            > "$out/allocation-check.json"; then
        cat "$out/allocation-check.json" >&2
        return 1
    fi
    echo "reflection allocation budgets OK"
}

# This gate measures C++ member inference from the original assembly metadata.
# Managed preservation is covered by build-and-run-preserve-control.sh.
reflection_layout_axis=default
corelib_diff_gate ReflectInvoke --no-ildiet

reflection_layout_axis=overrides
DN2CPP_OUT_SUFFIX="${DN2CPP_OUT_SUFFIX:-}-metadata-overrides" \
    corelib_diff_gate ReflectInvoke --no-ildiet \
        --reflection-metadata 'ReflectInvoke::ReflectMetadataLayoutSubset.NativeBase=packed' \
        --reflection-metadata 'ReflectMetadataLayoutSubset.PackedBase=native' \
        --reflection-metadata 'ReflectMetadataLayoutSubset.Generic`1[System.String]=packed' \
        --reflection-metadata 'ReflectMetadataLayoutSubset.Generic`1[System.Object]=native' \
        --reflection-metadata 'ReflectMetadataCompressionSubset.Direct=packed' \
        --reflection-metadata 'ReflectMetadataCompressionSubset.Generic`1[System.String]=packed' \
        --reflection-metadata 'ReflectMetadataCompressionSubset.CliBase=native' \
        --reflection-metadata 'System.String=native'

reflection_layout_axis=uncompressed
DN2CPP_OUT_SUFFIX="${DN2CPP_OUT_SUFFIX:-}-metadata-uncompressed" \
    corelib_diff_gate ReflectInvoke --no-ildiet --no-metadata-compression \
        --reflection-metadata 'ReflectMetadataLayoutSubset.NativeBase=packed' \
        --reflection-metadata 'ReflectMetadataCompressionSubset.Direct=packed' \
        --reflection-metadata 'System.String=packed'

# A global opt-out dominates packed selectors in either argument order.
uncompressed_reverse=artifacts/reflection-metadata-uncompressed-reverse
run_bounded invoke_cli "$_CG_APP" -r "$_CG_CORELIB" --no-ildiet \
    --reflection-metadata 'ReflectMetadataLayoutSubset.NativeBase=packed' \
    --reflection-metadata 'ReflectMetadataCompressionSubset.Direct=packed' \
    --reflection-metadata 'System.String=packed' --no-metadata-compression \
    -o "$uncompressed_reverse"
"$py" gates/fixtures/check-reflection-layout.py "$uncompressed_reverse" uncompressed

invalid_out=artifacts/reflection-metadata-invalid
mkdir -p "$invalid_out"
expect_policy_rejection() {
    local name="$1" diagnostic="$2" status=0
    shift 2
    run_bounded invoke_cli "$_CG_APP" -r "$_CG_CORELIB" --no-ildiet \
        -o "$invalid_out/$name" "$@" > "$invalid_out/$name.log" 2>&1 || status=$?
    if [ "$status" -ne 2 ] || ! grep -Fq -- "$diagnostic" "$invalid_out/$name.log"; then
        cat "$invalid_out/$name.log" >&2
        echo "error: invalid reflection metadata policy $name was not rejected" >&2
        return 1
    fi
}
expect_policy_rejection malformed '--reflection-metadata expects <type>=native|packed' \
    --reflection-metadata 'ReflectMetadataLayoutSubset.NativeBase'
expect_policy_rejection conflicting 'Duplicate --reflection-metadata selector' \
    --reflection-metadata 'ReflectMetadataLayoutSubset.NativeBase=native' \
    --reflection-metadata 'ReflectMetadataLayoutSubset.NativeBase=packed'
expect_policy_rejection runtime-owned "cannot select packed metadata for runtime-owned type 'System.String'" \
    --reflection-metadata 'System.String=packed'

# Exercise representation boundaries that C# metadata cannot express, using
# the production decoder and the same CMake/Ninja path as the parity binary.
codec_out=artifacts/reflection-metadata-codec
mkdir -p "$codec_out"
cp gates/fixtures/reflection-metadata-codec.cpp "$codec_out/generated.cpp"
printf '#pragma once\n' > "$codec_out/generated.h"
compile_console "$codec_out" MetadataCodec
assert_output "$("$codec_out/MetadataCodec$EXE_EXT")" "metadata codec boundaries OK"

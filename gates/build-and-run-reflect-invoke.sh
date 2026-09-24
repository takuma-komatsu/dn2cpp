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
# ReflectDelegateIdentitySubset asserts Delegate.Method for IL-bound delegates:
# class and generic virtual overrides (new-slot hiders and covariant returns
# included), interface bindings over class, struct, explicit, default and
# generic implementations, array generic arguments, and runtime-owned declaring
# types, which may answer null but never a wrong method. Its interface section
# pins the selected method for competing plain and explicit generic bodies in
# either metadata order, and for a derived interface's override of a default
# over class, struct, inherited, typed, generic, identical-body and
# MakeGenericType receivers, and the generic virtual body a MakeGenericType
# receiver runs and reports: a derived interface's generic override and an
# inherited class generic override. Its interface generic dispatch section pins
# which body a call binds: explicit overloads through a plain and a closed generic
# interface, a plain overload beside an explicit sibling, and explicit bodies
# for an interface whose name extends the called one's or differs in arity.
# Its interface redeclaration section pins which class level supplies an
# interface body, plain and generic, for the call and Delegate.Method: a level
# listing the interface again prefers its own public method to a base's
# explicit body, a level that does not list it neither displaces the inherited
# body with a same-name method or hider nor hides a default, a subclass
# override takes the class slot the mapping chose (abstract bases included),
# and a base without the interface fills a listing level's empty slot, over
# closed generic interfaces, shared generic classes and MakeGenericType receivers.
# Its runtime-level section pins the generic virtual body a MakeGenericType
# receiver runs when one of the instantiation's own generic levels declares it,
# and the method Delegate.Method reports on that level: an override of a generic
# base's method with a base call, over a constructed and an unconstructed base,
# a two-parameter level, overrides of a non-generic base's method on the leaf and
# on a middle level (minted, or the image's own abstract type without that
# instantiation), and an interface implementation, beside a plain virtual and
# an interface method of the same instantiations and a delegate created from
# the plain virtual's reflected method row.
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
    grep -Fxq 'existing-constructor-message: Exception has been thrown by the target of an invocation.' "$out/metadata-layout.stdout"
    grep -Fxq 'existing-constructor-method-composed-flags: TargetInvocationException InvalidOperationException 80131604' "$out/metadata-layout.stdout"
    grep -Fxq 'existing-constructor-end' "$out/metadata-layout.stdout"
    grep -Fxq 'activator-cold-generic=73' "$out/metadata-layout.stdout"
    DN2CPP_BEFORE_EXISTING_CONSTRUCTOR=1 run_bounded "$out/ReflectInvoke$EXE_EXT" > "$out/before-existing-constructor.stdout"
    sed '/^existing-constructor-begin/,$d' "$out/metadata-layout.stdout" > "$out/existing-constructor-prefix.stdout"
    diff -u <(strip_cr_win_file "$out/before-existing-constructor.stdout") \
        <(strip_cr_win_file "$out/existing-constructor-prefix.stdout")
    DN2CPP_BEFORE_COLD_ACTIVATOR=1 run_bounded "$out/ReflectInvoke$EXE_EXT" > "$out/before-cold-activator.stdout"
    sed '/^activator-cold-generic=/,$d' "$out/metadata-layout.stdout" > "$out/cold-activator-prefix.stdout"
    diff -u <(strip_cr_win_file "$out/before-cold-activator.stdout") \
        <(strip_cr_win_file "$out/cold-activator-prefix.stdout")
    grep -Fxq 'delegate-method-shared=True/True' "$out/metadata-layout.stdout"
    grep -Fxq 'delegate-method-generic=Int32/String' "$out/metadata-layout.stdout"
    grep -Fxq 'delegate-method-runtime-owned=True/True' "$out/metadata-layout.stdout"
    grep -Fxq 'delegate-method-struct-interface=StructProbe/Value/31/31' "$out/metadata-layout.stdout"
    grep -Fxq 'delegate-method-explicit-interface=ExplicitProbe/True/41/41' "$out/metadata-layout.stdout"
    grep -Fxq 'delegate-method-default-interface=IDefaultProbe/Default/101' "$out/metadata-layout.stdout"
    grep -Fxq 'delegate-method-interface-generic=ImplicitGeneric/String/ExplicitGeneric/True/Int32/p5' "$out/metadata-layout.stdout"
    grep -Fxq 'delegate-method-array-generic=Int32[]/String[]' "$out/metadata-layout.stdout"
    grep -Fxq 'delegate-method-generic-hider=GvmBase/base/GvmLeaf/leaf' "$out/metadata-layout.stdout"
    grep -Fxq 'delegate-method-generic-covariant=CovariantLeaf/CovariantLeaf/CovariantLeaf' "$out/metadata-layout.stdout"
    grep -Fxq 'delegate-method-end' "$out/metadata-layout.stdout"
    grep -Fxq 'delegate-method-interface-begin' "$out/metadata-layout.stdout"
    grep -Fxq 'delegate-method-generic-explicit-order=explicit/PlainFirstGeneric/True/explicit/ExplicitFirstGeneric/True' "$out/metadata-layout.stdout"
    grep -Fxq 'delegate-method-derived-default=derived/IDerivedDefault/True' "$out/metadata-layout.stdout"
    grep -Fxq 'delegate-method-derived-generic=derived/IDerivedGenericDefault/True' "$out/metadata-layout.stdout"
    grep -Fxq 'delegate-method-derived-struct=derived/IDerivedDefault' "$out/metadata-layout.stdout"
    grep -Fxq 'delegate-method-derived-inherited=derived/IDerivedDefault' "$out/metadata-layout.stdout"
    grep -Fxq 'delegate-method-derived-typed=derived/True' "$out/metadata-layout.stdout"
    grep -Fxq 'delegate-method-derived-same=same/IDerivedSame' "$out/metadata-layout.stdout"
    grep -Fxq 'delegate-method-derived-runtime-type=derived/IRuntimeDerivedDefault/True' "$out/metadata-layout.stdout"
    grep -Fxq 'delegate-method-derived-generic-runtime-type=derived/derived/IRuntimeGenericDerivedDefault/True' "$out/metadata-layout.stdout"
    grep -Fxq 'delegate-method-inherited-generic-runtime-type=mid/mid/RuntimeGvmMid' "$out/metadata-layout.stdout"
    grep -Fxq 'delegate-method-interface-end' "$out/metadata-layout.stdout"
    grep -Fxq 'interface-gvm-dispatch-begin' "$out/metadata-layout.stdout"
    grep -Fxq 'interface-gvm-explicit-overloads=generic/integer' "$out/metadata-layout.stdout"
    grep -Fxq 'interface-gvm-explicit-overloads-generic-interface=generic/integer' "$out/metadata-layout.stdout"
    grep -Fxq 'interface-gvm-plain-and-explicit-overload=plain/int-explicit/Pick/True' "$out/metadata-layout.stdout"
    grep -Fxq 'interface-gvm-qualifier-prefix=plain/longer/Pick/True' "$out/metadata-layout.stdout"
    grep -Fxq 'interface-gvm-qualifier-arity=plain/explicit-generic/Pick/True' "$out/metadata-layout.stdout"
    grep -Fxq 'interface-gvm-dispatch-end' "$out/metadata-layout.stdout"
    grep -Fxq 'interface-redeclaration-begin' "$out/metadata-layout.stdout"
    grep -Fxq 'interface-redeclaration-plain=derived-plain/derived-plain/RedeclaredDerived/plain' "$out/metadata-layout.stdout"
    grep -Fxq 'interface-redeclaration-unlisted=base-explicit/base-explicit/RedeclaredBase/explicit' "$out/metadata-layout.stdout"
    grep -Fxq 'interface-redeclaration-hider=implicit/implicit/ImplicitRedeclared/plain' "$out/metadata-layout.stdout"
    grep -Fxq 'interface-redeclaration-abstract=abstract-leaf/abstract-leaf/AbstractLeaf/plain' "$out/metadata-layout.stdout"
    grep -Fxq 'interface-redeclaration-generic-class=shared-box-String/shared-box-String/SharedRedeclaredBox`1/plain' "$out/metadata-layout.stdout"
    grep -Fxq 'interface-redeclaration-fill=fill-source/fill-source/FillSource/plain/fill-override/fill-override/FillOverride/plain' "$out/metadata-layout.stdout"
    grep -Fxq 'interface-redeclaration-explicit-mid=explicit-mid/explicit-mid/ExplicitMidRedeclared/explicit' "$out/metadata-layout.stdout"
    grep -Fxq 'interface-redeclaration-default=default/default/IRedeclaredDefault/plain/default-mid/default-mid/DefaultRedeclared/explicit' "$out/metadata-layout.stdout"
    grep -Fxq 'interface-redeclaration-closed-generic=of-derived-plain/of-derived-plain/RedeclaredOfDerived/plain' "$out/metadata-layout.stdout"
    grep -Fxq 'interface-redeclaration-runtime-type=runtime-box/runtime-box/Tag' "$out/metadata-layout.stdout"
    grep -Fxq 'interface-redeclaration-pick-plain=pick-derived-plain/pick-derived-plain/PickDerived/plain' "$out/metadata-layout.stdout"
    grep -Fxq 'interface-redeclaration-pick-unlisted=pick-base-explicit/pick-base-explicit/PickBase/explicit' "$out/metadata-layout.stdout"
    grep -Fxq 'interface-redeclaration-pick-hider=pick-implicit/pick-implicit/PickImplicit/plain/pick-virtual/pick-virtual/PickVirtual/plain' "$out/metadata-layout.stdout"
    grep -Fxq 'interface-redeclaration-pick-override=pick-override/pick-override/PickOverride/plain' "$out/metadata-layout.stdout"
    grep -Fxq 'interface-redeclaration-pick-fill=pick-source/pick-source/PickSource/plain/pick-target-override/pick-target-override/PickTargetOverride/plain' "$out/metadata-layout.stdout"
    grep -Fxq 'interface-redeclaration-end' "$out/metadata-layout.stdout"
    grep -Fxq 'runtime-level-gvm-begin' "$out/metadata-layout.stdout"
    grep -Fxq 'runtime-level-gvm-generic-base=root:Int32/String|leaf:Int32/String+root:Int32/String|leaf:Int32/String+root:Int32/String|Tag|True|True|True' "$out/metadata-layout.stdout"
    grep -Fxq 'runtime-level-gvm-unconstructed-base=leaf:String/Int32+root:String/Int32|leaf:String/Int32+root:String/Int32|True|Int32' "$out/metadata-layout.stdout"
    grep -Fxq 'runtime-level-gvm-two-arguments=pair:Int32,String/String|pair:Int32,Boolean/String|pair:Int32,Boolean/String|True' "$out/metadata-layout.stdout"
    grep -Fxq 'runtime-level-gvm-plain-base=own:Decimal/String|own:Decimal/String|True|own:Decimal|True' "$out/metadata-layout.stdout"
    grep -Fxq 'runtime-level-method-row=True|own:Decimal|True|Who' "$out/metadata-layout.stdout"
    grep -Fxq 'runtime-level-gvm-chain=chain:String+mid:String/Int32|chain:String+mid:String/Int32|True' "$out/metadata-layout.stdout"
    grep -Fxq 'runtime-level-gvm-inherited=mid:Boolean/Int32|mid:Boolean/Int32|True|Boolean' "$out/metadata-layout.stdout"
    grep -Fxq 'runtime-level-gvm-image-level=abstract-mid:Int32/Int32|True|True|True' "$out/metadata-layout.stdout"
    grep -Fxq 'runtime-level-gvm-interface=picker:Int32/String|picker:Int32/String|True|picker:Int32|True|Name' "$out/metadata-layout.stdout"
    grep -Fxq 'runtime-level-gvm-end' "$out/metadata-layout.stdout"
    DN2CPP_BEFORE_RUNTIME_LEVEL_GVM=1 run_bounded "$out/ReflectInvoke$EXE_EXT" > "$out/before-runtime-level-gvm.stdout"
    sed '/^runtime-level-gvm-begin/,$d' "$out/metadata-layout.stdout" > "$out/runtime-level-gvm-prefix.stdout"
    diff -u <(strip_cr_win_file "$out/before-runtime-level-gvm.stdout") \
        <(strip_cr_win_file "$out/runtime-level-gvm-prefix.stdout")
    DN2CPP_BEFORE_INTERFACE_REDECLARATION=1 run_bounded "$out/ReflectInvoke$EXE_EXT" > "$out/before-interface-redeclaration.stdout"
    sed '/^interface-redeclaration-begin/,$d' "$out/metadata-layout.stdout" > "$out/interface-redeclaration-prefix.stdout"
    diff -u <(strip_cr_win_file "$out/before-interface-redeclaration.stdout") \
        <(strip_cr_win_file "$out/interface-redeclaration-prefix.stdout")
    DN2CPP_BEFORE_INTERFACE_SELECTION=1 run_bounded "$out/ReflectInvoke$EXE_EXT" > "$out/before-interface-selection.stdout"
    sed '/^delegate-method-interface-begin/,$d' "$out/metadata-layout.stdout" > "$out/interface-selection-prefix.stdout"
    diff -u <(strip_cr_win_file "$out/before-interface-selection.stdout") \
        <(strip_cr_win_file "$out/interface-selection-prefix.stdout")
    DN2CPP_BEFORE_DELEGATE_METHOD=1 run_bounded "$out/ReflectInvoke$EXE_EXT" > "$out/before-delegate-method.stdout"
    sed '/^delegate-method-begin/,$d' "$out/metadata-layout.stdout" > "$out/delegate-method-prefix.stdout"
    diff -u <(strip_cr_win_file "$out/before-delegate-method.stdout") \
        <(strip_cr_win_file "$out/delegate-method-prefix.stdout")

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
DN2CPP_STRICT_COMPLETION=1 DN2CPP_GATE_EXTRA_CONTEXT="${DN2CPP_GATE_EXTRA_CONTEXT:-}|strict-completion" \
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

#!/usr/bin/env bash
# Reflected events retain accessor handles, visibility, symbolic owners and guarded operations.
reflected_event_asserts() {
    local output="$1"
    reflection_assert_witnesses "$output" <<'WITNESSES'
== reflected events ==
event basic: BaseTick/add_BaseTick/remove_BaseTick/True/0
event shape: True/True/False/True
event context: True/True/True/True
event identity: True/True/True/True
event intern allocation: True
event public order: DerivedTick,Changed,Hidden,BaseTick
event all order: DerivedTick,Changed,Hidden,Shadow,Secret,BaseTick,ProtectedTick,Shared
event declared order: DerivedTick,Changed,Hidden,Shadow,Secret
event private: True/True/True/True
event hiding: True/True/True
event ignore case: True/1/AmbiguousMatchException
event unicode: True/1
event member: True/True/True/True/True
event mixed kind: Method,Constructor,Event,Field
event attributes: base-event/True/True
event display: System.Action`1[System.Int32] BaseTick
event invocation: 10
event invocation guards: TargetException/TargetException/ArgumentException/InvalidOperationException
event exotic: True/True/True/OtherPublic/OtherPublic,OtherPrivate
event exotic identity: True/True/True
event inherited accessors: True/OtherPublic/True
event exotic invocation: 20
event fresh arrays: True/True/True
event generic shape: True/True/True/True
event generic multicast: True
event generic accessor: True/True/True
event generic guards: InvalidOperationException/InvalidOperationException/InvalidOperationException
event bare definition: True/True
event closed operations: 34/True/True
event cross owner: True/True
event runtime owner: True/True/True/1/0
event default member: True
event null guards: ArgumentNullException/NullReferenceException/NullReferenceException
event repeated: True/True/True
event generic other: True/1/True/none
event generic other context: True/True
reflected events end
WITNESSES
}

reflected_event_prefix_asserts() {
    local out="$1" axis
    for axis in dotnet native; do
        if reflection_full_profile; then
            if [ "$axis" = dotnet ]; then
                gate_capture_ok "$out/events-before.$axis.raw" reflection_oracle "$_CG_APP" before-reflected-events
            else
                gate_capture_ok "$out/events-before.$axis.raw" run_bounded "$out/ReflectInvoke$EXE_EXT" before-reflected-events
            fi
            gate_output prefix "$out/virtual-delegate-full.$axis.raw.stdout" "$out/events-before.$axis.raw.stdout" --before '== reflected events ==' --policy crlf
        fi
        reflected_event_asserts "$out/virtual-delegate-full.$axis.raw.stdout"
    done
}

reflected_event_diff_axes() {
    local axis out corelib bcl representation
    corelib=$(locate_corelib)
    bcl=$(dirname "$corelib")
    local axes=(packed native uncompressed trimmed unshared)
    if ! reflection_full_profile; then axes=(trimmed); fi
    for axis in "${axes[@]}"; do
        out="artifacts/reflected-events-$axis"
        mkdir -p "$out"
        local flags=() build_flags=() diet_flags=(--no-ildiet)
        case "$axis" in
            packed) flags=(--reflection-metadata 'EventSubset.EventBase=packed' --reflection-metadata 'EventSubset.ExoticEventOwner=native' --reflection-metadata 'EventSubset.GenericEventBase`1=packed'); diet_flags=() ;;
            native) flags=(--reflection-metadata 'EventSubset.EventBase=native' --reflection-metadata 'EventSubset.ExoticEventOwner=packed' --reflection-metadata 'EventSubset.GenericEventBase`1=native') ;;
            uncompressed) flags=(--no-metadata-compression) ;;
            trimmed) flags=(--trim-reflection) ;;
            unshared) flags=(--no-shared-generics); build_flags=(-p:ReflectedEventsUnshared=true) ;;
        esac
        reflection_build_once samples/dotnet/ReflectInvoke/ReflectedEventsOnly.csproj "$out/app" ${build_flags[@]+"${build_flags[@]}"}
        gate_capture_ok "$out/clr.raw" reflection_oracle "$out/app/ReflectedEventsOnly.dll"

        DN2CPP_STRICT_COMPLETION=1 invoke_cli "$out/app/ReflectedEventsOnly.dll" -r "$corelib" \
            -r "$bcl/System.Linq.dll" -r "$bcl/System.Threading.Tasks.Parallel.dll" -r "$out/app/TrimReflectLib.dll" \
            --link-xml samples/dotnet/ReflectInvoke/keep-event-other-methods.xml \
            ${diet_flags[@]+"${diet_flags[@]}"} ${flags[@]+"${flags[@]}"} -o "$out" > "$out/emit.log" 2>&1
        case "$axis" in
            packed|native)
                representation=record
                [ "$axis" != native ] || representation=native
                grep -Eq "^(alignas\(2\) static const uint8_t|static const Dn2CppEventInfo) md_${representation}_eventtab_EventSubset_EventBase\[\]" "$out"/generated_m*.cpp
                grep -Eq "^(alignas\(2\) static const uint8_t|static const Dn2CppEventInfo) md_${representation}_definition_EventSubset_GenericEventBase_1_events\[\]" "$out"/generated*.cpp
                ;;
        esac
        if [ "$axis" != unshared ] && grep -Eq '^extern const Dn2CppTypeInfo ti_EventSubset_SynthEventOwner_(System_)?Guid;' "$out/generated.h"; then
            echo 'FAIL: runtime event owner was unexpectedly AOT instantiated' >&2
            return 1
        fi
        compile_console "$out" ReflectedEventsOnly
        gate_capture_ok "$out/native.raw" run_bounded "$out/ReflectedEventsOnly$EXE_EXT"

        gate_output compare "$out/native.raw.stdout" "$out/clr.raw.stdout"
        reflected_event_asserts "$out/native.raw.stdout"
        gate_capture_ok "$out/stripped.native.raw" run_bounded "$out/ReflectedEventsOnly$EXE_EXT" stripped-events

        if [ "$axis" = trimmed ]; then
            gate_output witness "$out/stripped.native.raw.stdout" 'stripped event query: PlatformNotSupportedException' --count 2 --policy crlf
            gate_output witness "$out/stripped.native.raw.stdout" 'stripped events query: PlatformNotSupportedException' --count 2 --policy crlf
        else
            gate_capture_ok "$out/stripped.clr.raw" reflection_oracle "$out/app/ReflectedEventsOnly.dll" stripped-events

            gate_output compare "$out/stripped.native.raw.stdout" "$out/stripped.clr.raw.stdout"
        fi
        echo "reflected events $axis OK"
    done
}

reflected_event_preservation() {
    local out=artifacts/reflected-event-preservation owner method
    reflection_build_once samples/dotnet/ReflectInvoke/ReflectedEventsOnly.csproj "$out/app" -p:ReflectedEventsPreservation=true
    DN2CPP_STRICT_COMPLETION=1 invoke_cli "$out/app/ReflectedEventsOnly.dll" -r "$(locate_corelib)" \
        --link-xml gates/fixtures/reflected-event-preservation.xml -o "$out" > "$out/emit.log" 2>&1
    for owner in AttributeEventOwner XmlEventOwner; do
        for method in add_Exotic remove_Exotic RaiseExotic OtherPublic OtherPrivate; do
            grep -Eq "^(inline )?void ${owner}_${method}_[^(]+\([^;]*\)[[:space:]]*$" "$out"/generated*.h "$out"/generated_b*.cpp \
                || { echo "FAIL: preserved event accessor body missing: $owner.$method" >&2; return 1; }
        done
    done
    if grep -Eq '^(inline )?void ExoticEventOwner_(add_Exotic|remove_Exotic|RaiseExotic|OtherPublic|OtherPrivate)_[^(]+\([^;]*\)[[:space:]]*$' "$out"/generated*.h "$out"/generated_b*.cpp; then
        echo 'FAIL: lookup-only metadata rooted an unpreserved event body' >&2
        return 1
    fi
    compile_console "$out" ReflectedEventsOnly
    gate_capture_ok "$out/native.raw" run_bounded "$out/ReflectedEventsOnly$EXE_EXT"
    gate_capture_ok "$out/clr.raw" reflection_oracle "$out/app/ReflectedEventsOnly.dll"
    gate_output compare "$out/native.raw.stdout" "$out/clr.raw.stdout"
    echo 'reflected event attribute/link.xml preservation OK'
}

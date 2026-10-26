#!/usr/bin/env bash
# Runtime-owned lifetime and clone slots preserve cleanup, virtual overrides and copy identity.
runtime_lifetime_asserts() {
    local output="$1" line
    reflection_assert_lines "$output" 1 \
        '== runtime lifetime interfaces ==' 'runtime lifetime interfaces end' \
        'lifetime safe group: True' 'lifetime wait override: 1/True' \
        'lifetime wait explicit: 1/False' 'lifetime wait close alias: True' \
        'lifetime wait close group: 1/1' 'lifetime direct dispose close: 0/1' \
        'lifetime interface dispose close: 0/1' 'lifetime event override: 1' \
        'lifetime event explicit: 1' 'lifetime task pending: InvalidOperationException' \
        'lifetime task inline pending: InvalidOperationException' \
        'lifetime task direct pending: InvalidOperationException' \
        'lifetime task group pending: InvalidOperationException' \
        'lifetime task group alias: True' 'lifetime task interface null: NullReferenceException' \
        'lifetime task direct null: NullReferenceException' 'lifetime task terminal: disposed' \
        'lifetime factory cold pending: InvalidOperationException' \
        'lifetime factory completed: disposed' 'lifetime factory result: disposed' \
        'lifetime factory source: disposed' 'lifetime factory generic source: disposed' \
        'lifetime factory async: disposed' 'lifetime factory cold: disposed' \
        'lifetime factory run: disposed' 'lifetime factory all: disposed'
}

runtime_lifetime_diff_routes() {
    local route out witness flags app_route clone_fixture_ready=false
    local routes=(safe wait event task factory-completed factory-result factory-source \
        factory-generic-source factory-async factory-cold factory-run factory-all trimmed unshared \
        safegroups safeinvoke safeordinary safenull safe-trimmed safe-unshared \
        clone-string clone-string-calls clone-delegate clone-group clone-ordinary clone-trimmed clone-unshared \
        clone-class-aliases clone-class-invoke clone-class-overrides clone-class-override-only \
        clone-class-reflected-generic clone-class-reflected-type clone-class-reflected-trimmed \
        clone-class-reflected-unshared clone-class-reflected-field clone-class-field-trimmed \
        clone-class-field-unshared clone-class-native clone-class-null clone-class-trimmed clone-class-unshared \
        array-clone-typed array-clone-erased array-clone-interface-group array-clone-class-group \
        array-clone-aliases array-clone-reference array-clone-null array-clone-trimmed array-clone-unshared \
        array-clone-constrained array-clone-constrained-unshared array-clone-reflected \
        array-clone-md array-clone-md-unshared)
    if ! reflection_full_profile; then
        routes=(safe wait task safegroups clone-string clone-delegate
            clone-class-override-only clone-class-reflected-field
            array-clone-constrained array-clone-reflected array-clone-md trimmed)
    fi
    if [ "$#" != 0 ]; then routes=("$@"); fi
    for route in "${routes[@]}"; do
        out="artifacts/reflect-invoke-runtime-lifetime-$route"
        mkdir -p "$out"
        flags=()
        app_route="$route"
        case "$route" in
            array-clone-constrained-unshared) flags=(--no-shared-generics); app_route=array-clone-constrained ;;
            array-clone-md-unshared) flags=(--no-shared-generics); app_route=array-clone-md ;;
            array-clone-trimmed) flags=(--trim-reflection); app_route=array-clone-all ;;
            array-clone-unshared) flags=(--no-shared-generics); app_route=array-clone-all ;;
            trimmed) flags=(--trim-reflection) ;;
            unshared) flags=(--no-shared-generics) ;;
            safe-trimmed) flags=(--trim-reflection); app_route=safeall ;;
            safe-unshared) flags=(--no-shared-generics); app_route=safeall ;;
            clone-trimmed) flags=(--trim-reflection); app_route=clone-all ;;
            clone-unshared) flags=(--no-shared-generics); app_route=clone-all ;;
            clone-class-trimmed) flags=(--trim-reflection); app_route=clone-class-all ;;
            clone-class-unshared) flags=(--no-shared-generics); app_route=clone-class-all ;;
            clone-class-reflected-trimmed) flags=(--trim-reflection); app_route=clone-class-reflected-generic ;;
            clone-class-reflected-unshared) flags=(--no-shared-generics); app_route=clone-class-reflected-type ;;
            clone-class-field-trimmed) flags=(--trim-reflection); app_route=clone-class-reflected-field ;;
            clone-class-field-unshared) flags=(--no-shared-generics); app_route=clone-class-reflected-field ;;
        esac
        reflection_build_once samples/dotnet/ReflectInvoke/RuntimeLifetimeInterfacesOnly.csproj "$out/app" -p:LifetimeInterfaceRoute="$app_route"
        case "$app_route" in
            clone-class-reflected-field|array-clone-*) ;;
            clone-class-*)
                if [ "$clone_fixture_ready" = false ]; then
                    dotnet build gates/fixtures/ldftn-local/LdftnLocalFixture.csproj -c "$CONFIG" --nologo -v q
                    clone_fixture_ready=true
                fi
                dotnet exec "gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll" \
                    "$out/app/RuntimeLifetimeInterfacesOnly.dll" --delegate-clone-slots
                if [ "$route" = clone-class-overrides ]; then
                    dotnet exec "gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll" \
                        "$out/app/RuntimeLifetimeInterfacesOnly.dll" --delegate-clone-slots
                fi
                ;;
        esac
        gate_capture_ok "$out/clr.raw" reflection_oracle "$out/app/RuntimeLifetimeInterfacesOnly.dll"

        DN2CPP_STRICT_COMPLETION=1 invoke_cli "$out/app/RuntimeLifetimeInterfacesOnly.dll" -r "$(locate_corelib)" \
            --no-ildiet ${flags[@]+"${flags[@]}"} -o "$out" > "$out/emit.log" 2>&1
        if [ "$route" = array-clone-reflected ]; then
            # SZ generic cursors share IEnumerator.MoveNext; the MD enumeration
            # entry and Current must remain unrooted in this Clone-only image.
            assert_no_word_match 'reflection-only Clone image includes unrelated array methods or MD enumeration' \
                't_ArrayCloneInterfaceSubset_Program|MDArrayEnumerable_(GetEnumerator|get_Current)_m[0-9]+' \
                "$out"/generated*.cpp "$out/generated.h" || return 1
        fi
        compile_console "$out" RuntimeLifetimeInterfacesOnly
        gate_capture_ok "$out/native.raw" run_bounded "$out/RuntimeLifetimeInterfacesOnly$EXE_EXT"

        gate_output compare "$out/native.raw.stdout" "$out/clr.raw.stdout"
        case "$route" in
            array-clone-reflected) witness='array clone reflected: True/True/3/2/8' ;;
            array-clone-md|array-clone-md-unshared) witness='array clone MD: True/True/2/1/4/7/11' ;;
            array-clone-constrained|array-clone-constrained-unshared) witness='array clone constrained: True/True/3/2/8' ;;
            array-clone-typed) witness='array clone typed: True/True/3/2/8' ;;
            array-clone-erased) witness='array clone erased: True/True/3/2/8' ;;
            array-clone-interface-group) witness='array clone interface group: True/True/3/2/8' ;;
            array-clone-class-group) witness='array clone class group: True/True/3/2/8' ;;
            array-clone-aliases) witness='array clone aliases: True/True' ;;
            array-clone-reference) witness='array clone references: True/True/True/True' ;;
            array-clone-null) witness='array clone class null: ArgumentException' ;;
            array-clone-trimmed|array-clone-unshared|array-clone-all) array_clone_asserts "$out/native.raw.stdout"; continue ;;
            safe)
                witness='lifetime isolated safe: disposed'
                assert_no_word_match 'isolated SafeWaitHandle route emitted SafeHandle metadata' \
                    'tibind_System_Runtime_InteropServices_SafeHandle' "$out/generated.h" || return 1
                ;;
            wait) witness='lifetime wait close group: 1/1' ;;
            event) witness='lifetime event group: 1' ;;
            task) witness='lifetime task group pending: InvalidOperationException' ;;
            safegroups) witness='safe groups invoked: True' ;;
            safeinvoke) witness='safe close group invoked: True' ;;
            safeordinary) witness='user groups explicit invoked: 1/1/1' ;;
            safenull) witness='safe null group: ArgumentException' ;;
            safe-trimmed|safe-unshared|safeall) safe_handle_group_asserts "$out/native.raw.stdout"; continue ;;
            clone-string) witness='string clone group: True' ;;
            clone-string-calls) witness='string clone aliases: True/True' ;;
            clone-delegate) witness='delegate clone independent: 2/1/3' ;;
            clone-group) witness='delegate clone group: True/True/5' ;;
            clone-ordinary) witness='string clone null: NullReferenceException' ;;
            clone-trimmed|clone-unshared|clone-all) clone_interface_asserts "$out/native.raw.stdout"; continue ;;
            clone-class-aliases) witness='delegate clone class targets: False/True' ;;
            clone-class-invoke) witness='delegate clone class base: True/True/13' ;;
            clone-class-overrides) witness='delegate clone explicit invoked: class clone/interface clone' ;;
            clone-class-override-only) witness='delegate clone own slot only: class clone' ;;
            clone-class-reflected-generic|clone-class-reflected-trimmed) witness='delegate clone reflected generic: True/True/True/True/7' ;;
            clone-class-reflected-type|clone-class-reflected-unshared) witness='delegate clone reflected type: True/True/True/True/9' ;;
            clone-class-reflected-field|clone-class-field-trimmed|clone-class-field-unshared) witness='delegate clone reflected field: True/True/True/True/7' ;;
            clone-class-native) witness='delegate clone native pointer: True/True/True/True/9' ;;
            clone-class-null) witness='delegate clone base null: NullReferenceException' ;;
            clone-class-trimmed|clone-class-unshared|clone-class-all) delegate_clone_class_asserts "$out/native.raw.stdout"; continue ;;
            factory-*) witness="lifetime factory ${route#factory-}: disposed"; witness="${witness/generic-source/generic source}" ;;
            *) runtime_lifetime_asserts "$out/native.raw.stdout"; continue ;;
        esac
        gate_output witness "$out/native.raw.stdout" "$witness" --count 1 --policy crlf
    done
}

delegate_clone_class_asserts() {
    local output="$1" line
    reflection_assert_lines "$output" 1 \
        '== Delegate.Clone class method groups ==' 'Delegate.Clone class method groups end' \
        'delegate clone class aliases: True/True' 'delegate clone class remove: True/True' \
        'delegate clone class targets: False/True' 'delegate clone class static: True/True/True/5' \
        'delegate clone class closed: True/True/True/13' 'delegate clone class multicast: True/True/True/12' \
        'delegate clone class lists: 2/2/True/True/True' 'delegate clone class base: True/True/13' \
        'delegate clone override aliases: True/True' 'delegate clone override remove: True' \
        'delegate clone override invoked: class clone/class clone' \
        'delegate clone explicit aliases: False/True' \
        'delegate clone explicit invoked: class clone/interface clone' \
        'delegate clone class null: NullReferenceException' 'delegate clone base null: NullReferenceException'
}

gate_delegate_clone_class_prefix_asserts() {
    local out="$1" axis
    for axis in dotnet native; do
        if reflection_full_profile; then
            if [ "$axis" = dotnet ]; then
                gate_capture_ok "$out/delegate-clone-class-prefix.$axis.raw" reflection_oracle "$_CG_APP" before-delegate-clone-class-groups
            else
                gate_capture_ok "$out/delegate-clone-class-prefix.$axis.raw" run_bounded "$out/ReflectInvoke$EXE_EXT" before-delegate-clone-class-groups
            fi
            gate_output prefix "$out/virtual-delegate-full.$axis.raw.stdout" "$out/delegate-clone-class-prefix.$axis.raw.stdout" --before '== Delegate.Clone class method groups ==' --policy crlf
        fi
        delegate_clone_class_asserts "$out/virtual-delegate-full.$axis.raw.stdout"
    done
    if reflection_full_profile; then
        gate_output compare "$out/delegate-clone-class-prefix.native.raw.stdout" "$out/delegate-clone-class-prefix.dotnet.raw.stdout"
    fi
}

gate_runtime_lifetime_prefix_asserts() {
    local out="$1" axis
    for axis in dotnet native; do
        if reflection_full_profile; then
            if [ "$axis" = dotnet ]; then
                gate_capture_ok "$out/runtime-lifetime-prefix.$axis.raw" reflection_oracle "$_CG_APP" before-runtime-lifetime-interfaces
            else
                gate_capture_ok "$out/runtime-lifetime-prefix.$axis.raw" run_bounded "$out/ReflectInvoke$EXE_EXT" before-runtime-lifetime-interfaces
            fi
            gate_output prefix "$out/virtual-delegate-full.$axis.raw.stdout" "$out/runtime-lifetime-prefix.$axis.raw.stdout" --before '== runtime lifetime interfaces ==' --policy crlf
        fi
        runtime_lifetime_asserts "$out/virtual-delegate-full.$axis.raw.stdout"
    done
    if reflection_full_profile; then
        gate_output compare "$out/runtime-lifetime-prefix.native.raw.stdout" "$out/runtime-lifetime-prefix.dotnet.raw.stdout"
    fi
}


safe_handle_group_asserts() {
    local output="$1" line
    reflection_assert_lines "$output" 1 \
        '== SafeHandle method groups ==' 'SafeHandle method groups end' \
        'safe groups aliases: True/True' 'safe groups remove: True' \
        'safe groups close aliases: True' 'safe groups distinct: False/True' \
        'safe groups invoked: True' 'safe class group invoked: True' \
        'safe base group invoked: True' 'safe close group invoked: True' \
        'file groups aliases: True/True' 'file group invoked: True' \
        'user groups aliases: True/True' 'user groups invoked: 3/1/True' \
        'user groups explicit: False/True' 'user groups explicit invoked: 1/1/1' \
        'safe null group: ArgumentException' 'safe base null group: ArgumentException' \
        'safe close null group: ArgumentException' 'safe interface null group: NullReferenceException'
}

gate_safe_handle_group_prefix_asserts() {
    local out="$1" axis
    for axis in dotnet native; do
        if reflection_full_profile; then
            if [ "$axis" = dotnet ]; then
                gate_capture_ok "$out/safehandle-prefix.$axis.raw" reflection_oracle "$_CG_APP" before-safehandle-method-groups
            else
                gate_capture_ok "$out/safehandle-prefix.$axis.raw" run_bounded "$out/ReflectInvoke$EXE_EXT" before-safehandle-method-groups
            fi
            gate_output prefix "$out/virtual-delegate-full.$axis.raw.stdout" "$out/safehandle-prefix.$axis.raw.stdout" --before '== SafeHandle method groups ==' --policy crlf
        fi
        safe_handle_group_asserts "$out/virtual-delegate-full.$axis.raw.stdout"
    done
    if reflection_full_profile; then
        gate_output compare "$out/safehandle-prefix.native.raw.stdout" "$out/safehandle-prefix.dotnet.raw.stdout"
    fi
}

clone_interface_asserts() {
    local output="$1" line
    reflection_assert_lines "$output" 1 \
        '== runtime clone interfaces ==' 'runtime clone interfaces end' \
        'string clone group: True' 'string clone calls: True/True' \
        'string clone aliases: True/True' 'delegate clone group: True/True/5' \
        'delegate clone interface: True/True/True/5' 'delegate clone remove: True' \
        'delegate clone direct: True/True/6' 'delegate clone closed: True/True/True/13' \
        'delegate clone multicast: True/True/True/12' 'delegate clone lists: 2/2/True/True/True' \
        'delegate clone independent: 2/1/3' 'ordinary clone interface: True/7' \
        'clone interface null: NullReferenceException' \
        'clone group null: NullReferenceException' 'delegate clone null: NullReferenceException' \
        'string clone null: NullReferenceException'
}

gate_clone_interface_prefix_asserts() {
    local out="$1" axis
    for axis in dotnet native; do
        if reflection_full_profile; then
            if [ "$axis" = dotnet ]; then
                gate_capture_ok "$out/clone-prefix.$axis.raw" reflection_oracle "$_CG_APP" before-runtime-clone-interfaces
            else
                gate_capture_ok "$out/clone-prefix.$axis.raw" run_bounded "$out/ReflectInvoke$EXE_EXT" before-runtime-clone-interfaces
            fi
            gate_output prefix "$out/virtual-delegate-full.$axis.raw.stdout" "$out/clone-prefix.$axis.raw.stdout" --before '== runtime clone interfaces ==' --policy crlf
        fi
        clone_interface_asserts "$out/virtual-delegate-full.$axis.raw.stdout"
    done
    if reflection_full_profile; then
        gate_output compare "$out/clone-prefix.native.raw.stdout" "$out/clone-prefix.dotnet.raw.stdout"
    fi
}

array_clone_asserts() {
    local output="$1" line
    reflection_assert_lines "$output" 1 \
        '== array Clone interface dispatch ==' 'array Clone interface dispatch end' \
        'array clone typed: True/True/3/2/8' 'array clone erased: True/True/3/2/8' \
        'array clone constrained: True/True/3/2/8' \
        'array clone reflected: True/True/3/2/8' 'array clone MD: True/True/2/1/4/7/11' \
        'array clone interface group: True/True/3/2/8' 'array clone class group: True/True/3/2/8' \
        'array clone aliases: True/True' 'array clone remove: True' \
        'array clone alias invoked: True/True/3/2/8' 'array clone references: True/True/True/True' \
        'array clone class null: ArgumentException' 'array clone interface null: NullReferenceException'
}

gate_array_clone_prefix_asserts() {
    local out="$1" axis
    for axis in dotnet native; do
        if reflection_full_profile; then
            if [ "$axis" = dotnet ]; then
                gate_capture_ok "$out/array-clone-prefix.$axis.raw" reflection_oracle "$_CG_APP" before-array-clone-interfaces
            else
                gate_capture_ok "$out/array-clone-prefix.$axis.raw" run_bounded "$out/ReflectInvoke$EXE_EXT" before-array-clone-interfaces
            fi
            gate_output prefix "$out/virtual-delegate-full.$axis.raw.stdout" "$out/array-clone-prefix.$axis.raw.stdout" --before '== array Clone interface dispatch ==' --policy crlf
        fi
        array_clone_asserts "$out/virtual-delegate-full.$axis.raw.stdout"
    done
    if reflection_full_profile; then
        gate_output compare "$out/array-clone-prefix.native.raw.stdout" "$out/array-clone-prefix.dotnet.raw.stdout"
    fi
}

if [ "${BASH_SOURCE[0]}" = "$0" ]; then
    source "$(dirname "$0")/_common.sh"
    runtime_lifetime_diff_routes "$@"
fi

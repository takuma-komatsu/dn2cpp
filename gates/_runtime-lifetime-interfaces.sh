#!/usr/bin/env bash
# Runtime-owned lifetime and clone slots preserve cleanup, virtual overrides and copy identity.
runtime_lifetime_asserts() {
    local output="$1" line
    for line in '== runtime lifetime interfaces ==' 'runtime lifetime interfaces end' \
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
        'lifetime factory run: disposed' 'lifetime factory all: disposed'; do
        test "$(grep -Fxc -- "$line" "$output")" = 1 \
            || { echo "FAIL: runtime lifetime witness missing or repeated: $output/$line" >&2; return 1; }
    done
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
        clone-class-field-unshared clone-class-native clone-class-null clone-class-trimmed clone-class-unshared)
    if [ "$#" != 0 ]; then routes=("$@"); fi
    for route in "${routes[@]}"; do
        out="artifacts/reflect-invoke-runtime-lifetime-$route"
        mkdir -p "$out"
        flags=()
        app_route="$route"
        case "$route" in
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
        dotnet build samples/dotnet/ReflectInvoke/RuntimeLifetimeInterfacesOnly.csproj \
            -c "$CONFIG" --nologo -v q -p:LifetimeInterfaceRoute="$app_route" -o "$out/app"
        case "$app_route" in
            clone-class-reflected-field) ;;
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
        run_bounded dotnet "$out/app/RuntimeLifetimeInterfacesOnly.dll" > "$out/clr.raw.stdout"
        strip_cr_win_file "$out/clr.raw.stdout" > "$out/clr.stdout"
        DN2CPP_STRICT_COMPLETION=1 invoke_cli "$out/app/RuntimeLifetimeInterfacesOnly.dll" -r "$(locate_corelib)" \
            --no-ildiet ${flags[@]+"${flags[@]}"} -o "$out" > "$out/emit.log" 2>&1
        compile_console "$out" RuntimeLifetimeInterfacesOnly
        run_bounded "$out/RuntimeLifetimeInterfacesOnly$EXE_EXT" > "$out/native.raw.stdout"
        strip_cr_win_file "$out/native.raw.stdout" > "$out/native.stdout"
        diff -u "$out/clr.stdout" "$out/native.stdout"
        case "$route" in
            safe)
                witness='lifetime isolated safe: disposed'
                if rg -qw 'tibind_System_Runtime_InteropServices_SafeHandle' "$out/generated.h"; then
                    echo 'FAIL: isolated SafeWaitHandle route emitted SafeHandle metadata' >&2
                    return 1
                fi
                ;;
            wait) witness='lifetime wait close group: 1/1' ;;
            event) witness='lifetime event group: 1' ;;
            task) witness='lifetime task group pending: InvalidOperationException' ;;
            safegroups) witness='safe groups invoked: True' ;;
            safeinvoke) witness='safe close group invoked: True' ;;
            safeordinary) witness='user groups explicit invoked: 1/1/1' ;;
            safenull) witness='safe null group: ArgumentException' ;;
            safe-trimmed|safe-unshared|safeall) safe_handle_group_asserts "$out/native.stdout"; continue ;;
            clone-string) witness='string clone group: True' ;;
            clone-string-calls) witness='string clone aliases: True/True' ;;
            clone-delegate) witness='delegate clone independent: 2/1/3' ;;
            clone-group) witness='delegate clone group: True/True/5' ;;
            clone-ordinary) witness='string clone null: NullReferenceException' ;;
            clone-trimmed|clone-unshared|clone-all) clone_interface_asserts "$out/native.stdout"; continue ;;
            clone-class-aliases) witness='delegate clone class targets: False/True' ;;
            clone-class-invoke) witness='delegate clone class base: True/True/13' ;;
            clone-class-overrides) witness='delegate clone explicit invoked: class clone/interface clone' ;;
            clone-class-override-only) witness='delegate clone own slot only: class clone' ;;
            clone-class-reflected-generic|clone-class-reflected-trimmed) witness='delegate clone reflected generic: True/True/True/True/7' ;;
            clone-class-reflected-type|clone-class-reflected-unshared) witness='delegate clone reflected type: True/True/True/True/9' ;;
            clone-class-reflected-field|clone-class-field-trimmed|clone-class-field-unshared) witness='delegate clone reflected field: True/True/True/True/7' ;;
            clone-class-native) witness='delegate clone native pointer: True/True/True/True/9' ;;
            clone-class-null) witness='delegate clone base null: NullReferenceException' ;;
            clone-class-trimmed|clone-class-unshared|clone-class-all) delegate_clone_class_asserts "$out/native.stdout"; continue ;;
            factory-*) witness="lifetime factory ${route#factory-}: disposed"; witness="${witness/generic-source/generic source}" ;;
            *) runtime_lifetime_asserts "$out/native.stdout"; continue ;;
        esac
        test "$(grep -Fxc -- "$witness" "$out/native.stdout")" = 1
    done
}

delegate_clone_class_asserts() {
    local output="$1" line
    for line in '== Delegate.Clone class method groups ==' 'Delegate.Clone class method groups end' \
        'delegate clone class aliases: True/True' 'delegate clone class remove: True/True' \
        'delegate clone class targets: False/True' 'delegate clone class static: True/True/True/5' \
        'delegate clone class closed: True/True/True/13' 'delegate clone class multicast: True/True/True/12' \
        'delegate clone class lists: 2/2/True/True/True' 'delegate clone class base: True/True/13' \
        'delegate clone override aliases: True/True' 'delegate clone override remove: True' \
        'delegate clone override invoked: class clone/class clone' \
        'delegate clone explicit aliases: False/True' \
        'delegate clone explicit invoked: class clone/interface clone' \
        'delegate clone class null: NullReferenceException' 'delegate clone base null: NullReferenceException'; do
        test "$(grep -Fxc -- "$line" "$output")" = 1 \
            || { echo "FAIL: Delegate.Clone class witness missing or repeated: $output/$line" >&2; return 1; }
    done
}

gate_delegate_clone_class_prefix_asserts() {
    local out="$1" axis
    for axis in dotnet native; do
        if [ "$axis" = dotnet ]; then
            run_bounded dotnet "$_CG_APP" before-delegate-clone-class-groups > "$out/delegate-clone-class-prefix.$axis.raw.stdout"
        else
            run_bounded "$out/ReflectInvoke$EXE_EXT" before-delegate-clone-class-groups > "$out/delegate-clone-class-prefix.$axis.raw.stdout"
        fi
        strip_cr_win_file "$out/delegate-clone-class-prefix.$axis.raw.stdout" > "$out/delegate-clone-class-prefix.$axis.stdout"
        awk '/^== Delegate.Clone class method groups ==$/ { exit } { print }' \
            "$out/virtual-delegate-full.$axis.stdout" > "$out/delegate-clone-class-old.$axis.stdout"
        diff -u "$out/delegate-clone-class-prefix.$axis.stdout" "$out/delegate-clone-class-old.$axis.stdout"
        delegate_clone_class_asserts "$out/virtual-delegate-full.$axis.stdout"
    done
    diff -u "$out/delegate-clone-class-prefix.dotnet.stdout" "$out/delegate-clone-class-prefix.native.stdout"
}

gate_runtime_lifetime_prefix_asserts() {
    local out="$1" axis
    for axis in dotnet native; do
        if [ "$axis" = dotnet ]; then
            run_bounded dotnet "$_CG_APP" before-runtime-lifetime-interfaces > "$out/runtime-lifetime-prefix.$axis.raw.stdout"
        else
            run_bounded "$out/ReflectInvoke$EXE_EXT" before-runtime-lifetime-interfaces > "$out/runtime-lifetime-prefix.$axis.raw.stdout"
        fi
        strip_cr_win_file "$out/runtime-lifetime-prefix.$axis.raw.stdout" > "$out/runtime-lifetime-prefix.$axis.stdout"
        awk '/^== runtime lifetime interfaces ==$/ { exit } { print }' \
            "$out/virtual-delegate-full.$axis.stdout" > "$out/runtime-lifetime-old.$axis.stdout"
        diff -u "$out/runtime-lifetime-prefix.$axis.stdout" "$out/runtime-lifetime-old.$axis.stdout"
        runtime_lifetime_asserts "$out/virtual-delegate-full.$axis.stdout"
    done
    diff -u "$out/runtime-lifetime-prefix.dotnet.stdout" "$out/runtime-lifetime-prefix.native.stdout"
}


safe_handle_group_asserts() {
    local output="$1" line
    for line in '== SafeHandle method groups ==' 'SafeHandle method groups end' \
        'safe groups aliases: True/True' 'safe groups remove: True' \
        'safe groups close aliases: True' 'safe groups distinct: False/True' \
        'safe groups invoked: True' 'safe class group invoked: True' \
        'safe base group invoked: True' 'safe close group invoked: True' \
        'file groups aliases: True/True' 'file group invoked: True' \
        'user groups aliases: True/True' 'user groups invoked: 3/1/True' \
        'user groups explicit: False/True' 'user groups explicit invoked: 1/1/1' \
        'safe null group: ArgumentException' 'safe base null group: ArgumentException' \
        'safe close null group: ArgumentException' 'safe interface null group: NullReferenceException'; do
        test "$(grep -Fxc -- "$line" "$output")" = 1 \
            || { echo "FAIL: SafeHandle method-group witness missing or repeated: $output/$line" >&2; return 1; }
    done
}

gate_safe_handle_group_prefix_asserts() {
    local out="$1" axis
    for axis in dotnet native; do
        if [ "$axis" = dotnet ]; then
            run_bounded dotnet "$_CG_APP" before-safehandle-method-groups > "$out/safehandle-prefix.$axis.raw.stdout"
        else
            run_bounded "$out/ReflectInvoke$EXE_EXT" before-safehandle-method-groups > "$out/safehandle-prefix.$axis.raw.stdout"
        fi
        strip_cr_win_file "$out/safehandle-prefix.$axis.raw.stdout" > "$out/safehandle-prefix.$axis.stdout"
        awk '/^== SafeHandle method groups ==$/ { exit } { print }' \
            "$out/virtual-delegate-full.$axis.stdout" > "$out/safehandle-old.$axis.stdout"
        diff -u "$out/safehandle-prefix.$axis.stdout" "$out/safehandle-old.$axis.stdout"
        safe_handle_group_asserts "$out/virtual-delegate-full.$axis.stdout"
    done
    diff -u "$out/safehandle-prefix.dotnet.stdout" "$out/safehandle-prefix.native.stdout"
}

clone_interface_asserts() {
    local output="$1" line
    for line in '== runtime clone interfaces ==' 'runtime clone interfaces end' \
        'string clone group: True' 'string clone calls: True/True' \
        'string clone aliases: True/True' 'delegate clone group: True/True/5' \
        'delegate clone interface: True/True/True/5' 'delegate clone remove: True' \
        'delegate clone direct: True/True/6' 'delegate clone closed: True/True/True/13' \
        'delegate clone multicast: True/True/True/12' 'delegate clone lists: 2/2/True/True/True' \
        'delegate clone independent: 2/1/3' 'ordinary clone interface: True/7' \
        'clone interface null: NullReferenceException' \
        'clone group null: NullReferenceException' 'delegate clone null: NullReferenceException' \
        'string clone null: NullReferenceException'; do
        test "$(grep -Fxc -- "$line" "$output")" = 1 \
            || { echo "FAIL: clone interface witness missing or repeated: $output/$line" >&2; return 1; }
    done
}

gate_clone_interface_prefix_asserts() {
    local out="$1" axis
    for axis in dotnet native; do
        if [ "$axis" = dotnet ]; then
            run_bounded dotnet "$_CG_APP" before-runtime-clone-interfaces > "$out/clone-prefix.$axis.raw.stdout"
        else
            run_bounded "$out/ReflectInvoke$EXE_EXT" before-runtime-clone-interfaces > "$out/clone-prefix.$axis.raw.stdout"
        fi
        strip_cr_win_file "$out/clone-prefix.$axis.raw.stdout" > "$out/clone-prefix.$axis.stdout"
        awk '/^== runtime clone interfaces ==$/ { exit } { print }' \
            "$out/virtual-delegate-full.$axis.stdout" > "$out/clone-old.$axis.stdout"
        diff -u "$out/clone-prefix.$axis.stdout" "$out/clone-old.$axis.stdout"
        clone_interface_asserts "$out/virtual-delegate-full.$axis.stdout"
    done
    diff -u "$out/clone-prefix.dotnet.stdout" "$out/clone-prefix.native.stdout"
}

if [ "${BASH_SOURCE[0]}" = "$0" ]; then
    source "$(dirname "$0")/_common.sh"
    runtime_lifetime_diff_routes "$@"
fi

#!/usr/bin/env bash
# Runtime-owned and intrinsic-base IDisposable slots preserve cleanup and virtual overrides.
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
    local route out witness flags
    local routes=(safe wait event task factory-completed factory-result factory-source \
        factory-generic-source factory-async factory-cold factory-run factory-all trimmed unshared)
    if [ "$#" != 0 ]; then routes=("$@"); fi
    for route in "${routes[@]}"; do
        out="artifacts/reflect-invoke-runtime-lifetime-$route"
        mkdir -p "$out"
        flags=()
        case "$route" in
            trimmed) flags=(--trim-reflection) ;;
            unshared) flags=(--no-shared-generics) ;;
        esac
        dotnet build samples/dotnet/ReflectInvoke/RuntimeLifetimeInterfacesOnly.csproj \
            -c "$CONFIG" --nologo -v q -p:LifetimeInterfaceRoute="$route" -o "$out/app"
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
            factory-*) witness="lifetime factory ${route#factory-}: disposed"; witness="${witness/generic-source/generic source}" ;;
            *) runtime_lifetime_asserts "$out/native.stdout"; continue ;;
        esac
        test "$(grep -Fxc -- "$witness" "$out/native.stdout")" = 1
    done
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

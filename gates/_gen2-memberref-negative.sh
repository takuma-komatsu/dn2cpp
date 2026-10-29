#!/usr/bin/env bash
# Compile-only malformed retained Gen2 ctor/finalizer MemberRefs.
gen2_memberref_negative_gate() {
    local sample="$1" fixture="$2" out="$3"
    shift 3
    local flags=("$@") target form route lane input
    mkdir -p "$out"
    for target in ctor0 ctor1 final; do
        local forms=(return return-modifier static)
        [ "$target" = final ] || forms+=(argument argument-byref argument-modifier)
        for form in "${forms[@]}"; do
            if [[ "${DN2CPP_GEN2_MEMBERREF_SMOKE:-0}" == 1 ]] && ! { [[ "$target" == ctor0 && "$form" == return ]] || [[ "$target" == final && "$form" == static ]]; }; then
                continue
            fi
            for route in '' address-; do
                lane="$out/$target-${route}${form}"
                mkdir -p "$lane/input"
                input="$lane/input/$(basename "$sample")"
                cp "$sample" "$input"
                dotnet "$fixture" --gen2-callback-negative "$input" "$(locate_corelib)" "$target" "$route$form" || return $?
                local rc=0
                invoke_cli "$input" -r "$(locate_corelib)" --auto-ref "${flags[@]}" -o "$lane/native" > "$lane/transpile.log" 2>&1 || rc=$?
                printf '%s\n' "$rc" > "$lane/status"
                if [ "$rc" != 2 ]; then
                    echo "FAIL: malformed Gen2 MemberRef did not reject: $target $route$form ($rc)" >&2
                    return 1
                fi
                local diagnostic='error: Gen2GcCallback requires its exact MemberRef signature'
                if [[ -z "$route" ]]; then
                    local suffix=Final member=Finalize parameters=''
                    if [[ "$target" != final ]]; then
                        suffix="${target#ctor}"; member=.ctor
                        [[ "$target" == ctor0 ]] && parameters=System.Func_Boolean || parameters=System.Func_Object_Boolean
                        [[ "$form" != argument ]] || parameters=Object
                        [[ "$form" != argument-byref ]] || parameters+='&'
                        [[ "$target" != ctor1 ]] || parameters+=', Object'
                    fi
                    diagnostic="error: Gen2CallbackSubset.Program.Direct$suffix: call to System.Gen2GcCallback::$member($parameters) has no intrinsic mapping yet [chain:"
                fi
                grep -Fq "$diagnostic" "$lane/transpile.log" || {
                    cat "$lane/transpile.log" >&2
                    return 1
                }
            done
        done
    done
}

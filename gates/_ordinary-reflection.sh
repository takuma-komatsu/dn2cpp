#!/usr/bin/env bash
# A secondary driver stays beside its themed bucket and uses the same exact
# .NET parity and cache machinery as the bucket's ordinary CoreLib drivers.
ordinary_fixture_diff_gate() {
    local fixture_bucket="$1" fixture_subject="$2" core_definition
    shift 2
    core_definition=$(declare -f _corelib_gate_core)
    _corelib_gate_core() {
        local project="$1" out="$2" bcl name
        shift 2
        _CG_OUT="$out"
        _CG_CORELIB=${_CG_CORELIB_IN:-$(locate_corelib)}
        bcl=$(dirname "$_CG_CORELIB")
        dotnet build "samples/dotnet/$fixture_bucket/$project.csproj" \
            -c "$CONFIG" --nologo -v q -o "$out/app"
        _CG_APP="$out/app/$project.dll"
        _CG_EXTRA_REFERENCE_INPUTS=()
        local refs=(-r "$_CG_CORELIB")
        while [ "$#" -gt 0 ]; do
            name="$1"; shift
            if [ "$name" = --no-ildiet ] || [ "$name" = --no-metadata-compression ]; then
                refs+=("$name")
                continue
            fi
            if [ "$name" = --reflection-metadata ]; then
                [ "$#" -gt 0 ] && [ -n "$1" ] \
                    || { echo "error: $name requires a selector" >&2; return 1; }
                refs+=("$name" "$1")
                shift
                continue
            fi
            if [ "$name" = -r ]; then
                [ "$#" -gt 0 ] && [ -f "$1" ] \
                    || { echo "error: $name requires an input file" >&2; return 1; }
                refs+=("$name" "$1")
                _CG_EXTRA_REFERENCE_INPUTS+=("$1")
                shift
                continue
            fi
            [ -f "$bcl/$name.dll" ] \
                || { echo "error: requested reference $name not found beside CoreLib" >&2; return 1; }
            refs+=(-r "$bcl/$name.dll")
        done
        local target_args=()
        if [ -n "${IOS_SIM:-}" ] || [ -n "${IOS_DEV:-}" ]; then
            target_args=(--direct-pinvoke '*')
        fi
        invoke_cli "$_CG_APP" "${refs[@]}" \
            ${target_args[@]+"${target_args[@]}"} -o "$out"
    }
    corelib_diff_gate "$fixture_subject" "$@"
    eval "$core_definition"
}

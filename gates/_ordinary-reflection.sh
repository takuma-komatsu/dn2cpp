#!/usr/bin/env bash
# A secondary driver stays beside its themed bucket and uses the same exact
# .NET parity and cache machinery as the bucket's ordinary CoreLib drivers.
ordinary_fixture_diff_gate() {
    local fixture_bucket="$1" fixture_subject="$2" fixture_app="" core_definition
    shift 2
    if [ "${1:-}" = --built-app ]; then
        fixture_app=${2:-}
        if [ ! -f "$fixture_app" ] || [ "${fixture_app##*/}" != "$fixture_subject.dll" ]; then
            echo "FAIL: expected the built $fixture_subject.dll fixture at '$fixture_app'" >&2
            return 1
        fi
        shift 2
    fi
    core_definition=$(declare -f _corelib_gate_core)
    _corelib_gate_core() {
        local project="$1" out="$2" bcl name
        shift 2
        _CG_OUT="$out"
        _CG_CORELIB=${_CG_CORELIB_IN:-$(locate_corelib)}
        bcl=$(dirname "$_CG_CORELIB")
        if [ -z "$fixture_app" ]; then
            if declare -F gate_build_fixture >/dev/null; then
                gate_build_fixture "samples/dotnet/$fixture_bucket/$project.csproj" "$out/app" || return 1
            else
                dotnet build "samples/dotnet/$fixture_bucket/$project.csproj" \
                    -c "$CONFIG" --nologo -v q -o "$out/app" || return 1
            fi
            fixture_app="$out/app/$project.dll"
        fi
        _CG_APP="$fixture_app"
        _CG_EXTRA_REFERENCE_INPUTS=()
        local refs=(-r "$_CG_CORELIB")
        while [ "$#" -gt 0 ]; do
            name="$1"; shift
            if [ "$name" = --no-ildiet ] || [ "$name" = --no-metadata-compression ] \
                    || [ "$name" = --no-shared-generics ]; then
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
            if [ "$name" = -r ] || [ "$name" = --link-xml ]; then
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

#!/usr/bin/env bash
# Reuse only managed inputs and oracle observations within this gate invocation.
mkdir -p artifacts
_reflection_inputs=$(mktemp -d artifacts/reflect-invoke-inputs.XXXXXX)
_reflection_build_keys=()
_reflection_build_dirs=()
# Bash 3.2 rejects the length of an empty array under nounset.
_reflection_build_count=0
_reflection_full_out=
_reflection_full_project=

reflection_assert_lines() {
    "$py" gates/reflection-gate-inputs.py lines "$@"
}

reflection_assert_prefixes() {
    "$py" gates/reflection-gate-inputs.py prefixes "$@"
}

reflection_assert_witnesses() {
    "$py" gates/reflection-gate-inputs.py witnesses "$1" 1
}

reflection_build_once() {
    local project="$1" destination="$2" key index directory
    shift 2
    key=$(printf '%q|' "$CONFIG" "$TFM" "$project" "$@")
    directory=
    for ((index=0; index<_reflection_build_count; index++)); do
        if [ "${_reflection_build_keys[index]}" = "$key" ]; then
            directory=${_reflection_build_dirs[index]}
            break
        fi
    done
    if [ -z "$directory" ]; then
        directory="$_reflection_inputs/build-$_reflection_build_count"
        mkdir -p "$directory" || return 1
        if ! dotnet build "$project" -c "$CONFIG" --nologo -v q \
                -p:BuildInParallel=false "$@" -o "$directory" > "$directory/build.log" 2>&1; then
            cat "$directory/build.log" >&2
            return 1
        fi
        _reflection_build_keys+=("$key")
        _reflection_build_dirs+=("$directory")
        _reflection_build_count=$((_reflection_build_count + 1))
    fi
    # Fixtures rewrite each copy, leaving the shared managed build immutable.
    mkdir -p "$destination" || return 1
    cp -R "$directory/." "$destination/" || return 1
    cat "$directory/build.log"
}

build_proj() {
    [ -n "${DN2CPP_SKIP_BUILD:-}" ] && return 0
    reflection_build_once "$1" "$(dirname "$1")/bin/$CONFIG/$TFM"
}

gate_build_fixture() {
    reflection_build_once "$@"
}

reflection_oracle() {
    local key directory status
    key=$("$py" gates/reflection-gate-inputs.py oracle-key "$@") || return 1
    directory="$_reflection_inputs/oracle-$key"
    if [ ! -f "$directory/status" ]; then
        mkdir -p "$directory" || return 1
        if run_bounded dotnet "$@" > "$directory/stdout" 2> "$directory/stderr"; then
            status=0
        else
            status=$?
        fi
        printf '%s\n' "$status" > "$directory/status" || return 1
    fi
    cat "$directory/stdout" || return 1
    cat "$directory/stderr" >&2 || return 1
    read -r status < "$directory/status" || return 1
    return "$status"
}

gate_run_oracle() {
    reflection_oracle "$@"
}

gate_capture_diff_output() {
    local out="$1" project="$2" native="$3" oracle="$4"
    _reflection_full_out=
    _reflection_full_project=
    # Extra assertions request the argument-free image, even after a selected run.
    [ "$#" -eq 4 ] || return 0
    printf '%s\n' "$native" > "$out/gate.native.stdout" || return 1
    printf '%s\n' "$oracle" > "$out/gate.dotnet.stdout" || return 1
    _reflection_full_out="$out"
    _reflection_full_project="$project"
}

reflection_native_full() {
    local out="$1" project="$2"
    if [ "$_reflection_full_out" = "$out" ] && [ "$_reflection_full_project" = "$project" ]; then
        cat "$out/gate.native.stdout"
    else
        run_bounded "$out/$project$EXE_EXT"
    fi
}

reflection_inputs_cleanup() {
    local root directory
    root=$(cd artifacts && pwd -P) || return 1
    directory=$(cd "$_reflection_inputs" && pwd -P) || return 1
    if [ "${directory%/*}" != "$root" ]; then
        echo 'FAIL: reflection inputs are outside the artifacts directory' >&2
        return 1
    fi
    case "${directory##*/}" in
        reflect-invoke-inputs.*) rm -rf -- "$directory" ;;
        *) echo 'FAIL: unexpected reflection input directory' >&2; return 1 ;;
    esac
}

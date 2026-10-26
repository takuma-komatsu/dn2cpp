#!/usr/bin/env bash
# Reuse preserves compile-time variants, rewritten copies and failing CLR exits.
set -euo pipefail
cd "$(dirname "$0")/../.."
_DN2CPP_GATE_ROOT="$PWD"
py="$1"
CONFIG=Fixture
# This SDK-free adapter fixture explicitly selects the ordinary Linux policy.
DN2CPP_OS=linux
TFM=net10.0
EXE_EXT=.exe
_gate_exit_hooks=()
eval "$(sed -n '/^gate_add_exit_hook() {/,/^}/p; /^_gate_run_exit_hooks() {/,/^}/p' gates/_common.sh)"
source gates/_reflect-invoke-inputs.sh
if [ "${2:-}" = --exit-probe ]; then
    probe_record="$3"
    printf '%s\n' "$_reflection_inputs" > "$probe_record"
    fixture_exit_witness() { echo 'other exit hook ran' > "$probe_record.hook"; }
    gate_add_exit_hook fixture_exit_witness
    test "$4" = 0
    exit 0
fi
fixture="$_reflection_inputs/fixture"
mkdir -p "$fixture"
: > "$fixture/builds"
: > "$fixture/oracles"
printf 'row\r\nrow\r\n' > "$fixture/crlf.stdout"
reflection_assert_lines "$fixture/crlf.stdout" 2 row
dotnet() {
    if [ "$1" = build ]; then
        local destination= arg project="$2"
        printf '%s\n' "$*" >> "$fixture/builds"
        shift 2
        while [ "$#" -gt 0 ]; do
            arg="$1"; shift
            if [ "$arg" = -o ]; then destination="$1"; shift; fi
        done
        printf '%s\n' "$project" > "$destination/Driver.dll"
        echo 'fixture managed build'
    else
        printf '%s\n' "$*" >> "$fixture/oracles"
        echo "fixture oracle ${DN2CPP_BEFORE_FIXTURE:-full}"
        echo 'fixture diagnostic' >&2
        [ "${2:-}" != failure ] || return 7
    fi
}
run_bounded() {
    if [ "$1" = dotnet ]; then "$@"; else echo 'fixture native fallback'; fi
}
reflection_build_once Fixture.csproj "$fixture/first" -p:Route=first > "$fixture/build.stdout"
reflection_build_once Fixture.csproj "$fixture/second" -p:Route=first > "$fixture/reused-build.stdout"
test "$(wc -l < "$fixture/builds" | tr -d ' ')" = 1
diff -u "$fixture/build.stdout" "$fixture/reused-build.stdout"
printf 'rewritten IL\n' > "$fixture/first/Driver.dll"
reflection_build_once Fixture.csproj "$fixture/third" -p:Route=first > /dev/null
grep -Fxq Fixture.csproj "$fixture/third/Driver.dll"
reflection_build_once Fixture.csproj "$fixture/variant" -p:Route=second > /dev/null
test "$(wc -l < "$fixture/builds" | tr -d ' ')" = 2
grep -Fq -- '-p:Route=second' "$fixture/builds"
cp() { return 9; }
if reflection_build_once Fixture.csproj "$fixture/rejected-copy" -p:Route=first > /dev/null; then
    echo 'FAIL: managed input copy failure was accepted' >&2
    exit 1
fi
unset -f cp
reflection_oracle "$fixture/second/Driver.dll" > "$fixture/first.stdout" 2> "$fixture/first.stderr"
reflection_oracle "$fixture/third/Driver.dll" > "$fixture/second.stdout" 2> "$fixture/second.stderr"
test "$(wc -l < "$fixture/oracles" | tr -d ' ')" = 1
diff -u "$fixture/first.stdout" "$fixture/second.stdout"
diff -u "$fixture/first.stderr" "$fixture/second.stderr"
DN2CPP_BEFORE_FIXTURE=1 reflection_oracle "$fixture/third/Driver.dll" > "$fixture/prefix.stdout" 2> /dev/null
grep -Fxq 'fixture oracle 1' "$fixture/prefix.stdout"
test "$(wc -l < "$fixture/oracles" | tr -d ' ')" = 2
for observation in first second; do
    status=0
    reflection_oracle "$fixture/third/Driver.dll" failure > "$fixture/failure.$observation.stdout" \
        2> "$fixture/failure.$observation.stderr" || status=$?
    test "$status" = 7
done
test "$(wc -l < "$fixture/oracles" | tr -d ' ')" = 3
diff -u "$fixture/failure.first.stdout" "$fixture/failure.second.stdout"
diff -u "$fixture/failure.first.stderr" "$fixture/failure.second.stderr"
cat() { return 9; }
if reflection_oracle "$fixture/third/Driver.dll" > /dev/null; then
    echo 'FAIL: oracle replay failure was accepted' >&2
    exit 1
fi
unset -f cat
printf 'native observation\r\n\n\0' > "$fixture/native.raw"
printf 'CLR observation\r\n\n\0' > "$fixture/oracle.raw"
gate_capture_diff_files "$fixture" Driver "$fixture/native.raw" "$fixture/oracle.raw"
reflection_native_full "$fixture" Driver > "$fixture/full.stdout"
cmp "$fixture/native.raw" "$fixture/full.stdout"
reflection_native_full "$fixture" Other > "$fixture/other.stdout"
grep -Fxq 'fixture native fallback' "$fixture/other.stdout"
gate_capture_diff_files "$fixture" Driver "$fixture/native.raw" "$fixture/oracle.raw" before-fixture
reflection_native_full "$fixture" Driver > "$fixture/argument-run.stdout"
grep -Fxq 'fixture native fallback' "$fixture/argument-run.stdout"
eval "$(sed -n '/^corelib_diff_gate() {/,/^}/p; /^_corelib_diff_gate() {/,/^}/p; /^gate_capture() {/,/^}/p; /^gate_output() {/,/^}/p' gates/_common.sh)"
resolve_python() { printf '%s\n' "$py"; }
_corelib_gate_out() { printf '%s\n' "$fixture/corelib"; }
_corelib_gate_core() { mkdir -p "$2"; _CG_APP="$fixture/third/Driver.dll"; _CG_CORELIB=Fixture; }
_corelib_gate_check() { return 1; }
_gate_ctx_extras() { :; }
compile_console() { :; }
_gate_run_argv() { _GATE_RUN_ARGV=(); }
_gate_scratch_cleanup() { :; }
assert_output() { test "$1" = "$2"; }
assert_exit_code() { test "$1" = "$2"; }
_gate_extra_asserts() { :; }
gate_cache_commit() { :; }
run_bounded() {
    if [ "$1" = dotnet ]; then "$@"; else echo 'fixture oracle full'; fi
}
corelib_diff_gate Driver > /dev/null
grep -Fxq 'fixture oracle full' "$fixture/corelib/gate.native.stdout"
grep -Fxq 'fixture oracle full' "$fixture/corelib/gate.dotnet.stdout"
DN2CPP_ORACLE_DOTNET_ENABLE_AVX=0 corelib_diff_gate Driver > /dev/null
test "$(wc -l < "$fixture/oracles" | tr -d ' ')" = 4
unset -f gate_run_oracle gate_capture_diff_files
corelib_diff_gate Driver > /dev/null
test "$(wc -l < "$fixture/oracles" | tr -d ' ')" = 5
echo 'reflection input reuse OK'
for exit_status in 0 1; do
    probe_record="$fixture/exit-$exit_status"
    status=0
    bash gates/fixtures/reflection-gate-inputs.sh "$py" --exit-probe "$probe_record" "$exit_status" || status=$?
    test "$status" = "$exit_status"
    read -r exited_inputs < "$probe_record"
    test ! -e "$exited_inputs"
    grep -Fxq 'other exit hook ran' "$probe_record.hook"
done
echo 'reflection input cleanup OK'

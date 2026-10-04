#!/usr/bin/env bash
# Console text and standard byte streams, formatting and path APIs, diffed vs .NET.
source "$(dirname "$0")/_common.sh"

py=""
if [ "$DN2CPP_OS" != windows ]; then
    py=$(resolve_python) || gate_skip 'Python is required for nonblocking console pipe probes'
fi

gate_extra_asserts() {
    local out="$1" before prefix native expected
    assert_exit_code "$2" 0
    assert_exit_code "$3" 0
    before=$(run_bounded dotnet "$_CG_APP" before-standard-streams)
    prefix=$(awk '/^== standard console streams ==$/ { exit } { print }' "$out/native.out")
    assert_output "$(strip_cr_win "$prefix")" "$(strip_cr_win "$before")"
    grep -Fxq 'standard console streams end' "$out/native.out" \
        || { echo 'FAIL: standard console stream section did not run' >&2; return 1; }
    printf ABCDEFGHI > "$out/stdin.bin"
    native=$(run_bounded "./$out/ConsoleIo$EXE_EXT" standard-input < "$out/stdin.bin")
    expected=$(run_bounded dotnet "$_CG_APP" standard-input < "$out/stdin.bin")
    assert_output "$(strip_cr_win "$native")" "$(strip_cr_win "$expected")"
    grep -Fxq 'standard input end' <<< "$native" \
        || { echo 'FAIL: redirected standard input section did not run' >&2; return 1; }
    if [ -n "$py" ]; then
        run_bounded "$py" gates/fixtures/console-pipes.py "./$out/ConsoleIo$EXE_EXT" "$_CG_APP"
        local handles_out="$out-handles"
        mkdir -p "$handles_out"
        cp gates/fixtures/console-handles.cpp "$handles_out/generated.cpp"
        printf '#pragma once\n' > "$handles_out/generated.h"
        compile_console "$handles_out" ConsoleHandles
        assert_output "$(run_bounded "$handles_out/ConsoleHandles$EXE_EXT")" 'standard handle lifetime end'
    fi
}

DN2CPP_GATE_EXTRA_INPUTS="gates/fixtures/console-pipes.py gates/fixtures/console-handles.cpp"
corelib_diff_split_gate ConsoleIo --auto-ref

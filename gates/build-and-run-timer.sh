#!/usr/bin/env bash
# System.Threading.Timer — a per-timer OS thread that waits dueTime, fires
# TimerCallback(state), and (for a finite period > 0) re-fires periodically. Covers the
# one-shot, periodic, idle-then-Change, TimeSpan, long ctor/Change overloads, the system
# TimeProvider's ITimer adapter. Timer
# firing is timing-based, so the program asserts only deterministic facts (a latch gates
# main until the expected fires happened; it prints the one-shot fire count, the
# threaded-through state, and "count >= N" as a bool — never the timing-dependent exact
# count), so the output is byte-identical to real .NET and diffed exact. Timer/Timeout
# live in System.Threading.dll.
# Named argument faults, boxed bounds, validation order and GC-retained fields.
source "$(dirname "$0")/_common.sh"
timer_python=$(resolve_python) || gate_skip "no working Python 3 interpreter for threading call fixtures"

call_app="gates/fixtures/threading-delay-call/bin/$CONFIG/$TFM/ThreadingDelayCall.dll"
build_gate_proj gates/fixtures/threading-delay-call/ThreadingDelayCall.csproj
DN2CPP_GATE_EXTRA_INPUTS="${DN2CPP_GATE_EXTRA_INPUTS:-} $call_app ${call_app%.dll}.runtimeconfig.json ${call_app%.dll}.deps.json gates/fixtures/threading-delay-call/patch-call.py"
DN2CPP_GATE_EXTRA_CONTEXT="${DN2CPP_GATE_EXTRA_CONTEXT:-}|threading-delay-call|cli:$(_gate_cli_hash)"
gate_extra_asserts() {
    local out="$1" native before prefix line
    native=$(run_bounded "./$out/TimerSample")
    native=$(strip_cr_win "$native")
    before=$(dotnet "$_CG_APP" before-argument-fields)
    before=$(strip_cr_win "$before")
    prefix=$(awk '/^== Timer validation fields ==$/ { exit } { print }' <<< "$native")
    assert_output "$prefix" "$before"
    for line in '== Timer validation fields ==' \
        'timer int:0:-2:-2 param=dueTime' \
        'timer long:1:4294967295:-2 param=period' \
        'timer span:1:9223372036854769999:-10000 actual=Int64:922337203685477' \
        'change int:0:-2:-2 type=NullReferenceException' \
        'timer fields GC actual=Int64:-9223372036854775808' \
        'timer racing dispose:7 success=True' \
        'timer self dispose success=True' \
        'Timer validation fields end'; do
        grep -Fxq -- "$line" <<< "$native" \
            || { echo "FAIL: TimerSample validation witness missing: $line" >&2; exit 1; }
    done
    local oracle="$out/direct-call-oracle" fixture="$out/direct-call" expected actual
    mkdir -p "$oracle"
    cp "$call_app" "$oracle/ThreadingDelayCall.dll"
    cp "${call_app%.dll}.runtimeconfig.json" "${call_app%.dll}.deps.json" "$oracle/"
    $timer_python gates/fixtures/threading-delay-call/patch-call.py "$oracle/ThreadingDelayCall.dll"
    invoke_cli "$oracle/ThreadingDelayCall.dll" -r "$_CG_CORELIB" --auto-ref -o "$fixture"
    compile_console "$fixture" ThreadingDelayCall
    expected=$(run_bounded dotnet "$oracle/ThreadingDelayCall.dll")
    actual=$(run_bounded "./$fixture/ThreadingDelayCall")
    assert_output "$(strip_cr_win "$actual")" "$(strip_cr_win "$expected")"
    for line in 'timer direct int:-2:-1 param=dueTime' \
        'timer virtual int:-2:-1 type=NullReferenceException' \
        'timer direct span:-9223372036854775808:-10000 type=NullReferenceException' \
        'cts direct int:-2 param=millisecondsDelay' \
        'cts virtual int:-2 type=NullReferenceException' \
        'parallel direct:0 param=MaxDegreeOfParallelism' \
        'parallel virtual:0 type=NullReferenceException' \
        'Threading delay call faults end'; do
        grep -Fxq -- "$line" <<< "$(strip_cr_win "$actual")" \
            || { echo "FAIL: threading direct-call witness missing: $line" >&2; exit 1; }
    done

}

corelib_diff_gate TimerSample System.Threading

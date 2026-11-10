#!/usr/bin/env bash
# Process termination and managed diagnostics on exceptions escaping Main,
# pool work, local continuations, nested drains, threads, timers, timed cancellation and async void,
# including a closed generic nested exception's CLR type display and a Message
# override that only the runtime's report calls.
# Real .NET reports to
# stderr and aborts (SIGABRT -> 134), it does not exit(1) — and corelib_diff_gate
# pins the native exit status to real .NET's. The generated main's catch funnel
# must therefore abort too, with stdout flushed first (Linux's abort() does not
# flush stdio on its own). Terminal by nature — the program dies mid-section —
# so this lives in its own tiny gate rather than as a bucket section, like
# EnvSubset (Environment.Exit) and Finalizers (finalizer abort) before it.
# A second arm reruns the binary capturing stderr and asserts the report
# carries the throw-time trace — see its comment below.
source "$(dirname "$0")/_common.sh"
DN2CPP_GATE_EXTRA_CONTEXT="${DN2CPP_GATE_EXTRA_CONTEXT:-}|trace-hook:fresh-wrapper-image|worker-modes:pool,thread,timer,cts-timer,async-void,local-continuation,nested-drain,generic-exception,message-override"
# The Windows/MSYS status-4 workaround also covers the initial Main comparison.
DN2CPP_RETRY_STATUS4=${DN2CPP_RETRY_STATUS4:-1}

gate_extra_asserts() {

# Throw-time-trace arm: the unhandled report must carry the trace captured at throw. The
# stdout+exit diff above stays pinned to real .NET; stderr can never join that
# diff (trace formatting and frame names differ), so it is asserted by hand
# here: at least one "   at " trace line, and the frame of the method the
# exception escaped from. Contains-only on purpose — frame COUNT and the
# neighboring frames are best-effort under -O2 (an inlined or unresolvable
# frame simply drops); what the feature guarantees is that the report names
# the throw path.
echo "== Throw-time-trace arm: unhandled report carries the trace captured at throw =="
# The hook runs only after this output was compiled and checked by the wrapper.
local out="$1" project=UnhandledExitSubset
gate_capture "$out/trace" run_bounded "./$out/$project$EXE_EXT"
code=$(cat "$out/trace.status")
gate_output native-abort "$out/trace" --host "$DN2CPP_OS" --witness 'before throw' \
    --diagnostic 'Unhandled exception. System.InvalidOperationException: escaped Main'
err="$out/trace.stderr"
if [ "$code" -eq 0 ]; then
    echo "FAIL: the unhandled-exit binary exited 0 — an escaped exception must abort" >&2
    exit 1
fi
# A signal before any report is a launch failure, not a missing managed trace.
if [ "$code" -ge 128 ] && [ ! -s "$err" ]; then
    echo "FAIL: the binary died of signal $((code - 128)) before producing any output — it did not run." >&2
    echo "      On macOS that is what an exec of an in-place-rewritten executable does. Check that" >&2
    echo "      compile_console still stages through stage_binary (copy beside + rename, i.e. a fresh" >&2
    echo "      inode) and not a plain cp onto $out/$project. See stage_binary in gates/_common.sh." >&2
    exit 1
fi
if ! grep -q '^   at ' "$err"; then
    echo "FAIL: unhandled-exception stderr carries no '   at ' trace line" >&2
    cat "$err" >&2
    exit 1
fi
if ! grep -q '^   at Program\.Main()' "$err"; then
    echo "FAIL: unhandled-exception stderr trace does not name the throw path (Program.Main)" >&2
    cat "$err" >&2
    exit 1
fi
echo "OK — the unhandled report names Program.Main in a real '   at ' trace"

echo "== Escaping-worker arms: managed first line and abnormal exit match real .NET =="
first_line_policy=exact
if [ "$DN2CPP_OS" = windows ]; then
    first_line_policy=crlf
fi
for mode in pool thread timer 'cts timer' 'async void' 'local continuation' 'nested drain' 'generic exception' \
        'message override'; do
    run_out="$out/worker-$mode"
    gate_capture "$run_out/native" run_bounded_retry_status4 "$out" "worker $mode native" \
        "./$out/$project$EXE_EXT" "$mode"
    gate_capture "$run_out/oracle" run_bounded_retry_status4 "$out" "worker $mode oracle" \
        dotnet "$_CG_APP" "$mode"
    native_code=$(cat "$run_out/native.status")
    expected_code=$(cat "$run_out/oracle.status")
    gate_output compare "$run_out/native.stdout" "$run_out/oracle.stdout"
    gate_assert_text "$run_out/native.stdout" "before throw
-- escaping worker: $mode --
throwing $mode"
    case "$expected_code" in
        0|1) echo "FAIL: real .NET did not terminate abnormally for $mode" >&2; exit 1 ;;
    esac
    if [ "$DN2CPP_OS" = windows ] && { [ "$native_code" -eq 4 ] || [ "$expected_code" -eq 4 ]; }; then
        echo "FAIL: worker $mode still returned status 4 after one retry" >&2
        exit 1
    fi
    # The first diagnostic is a semantic record; retain both complete reports.
    gate_output select "$run_out/native.stderr" "$run_out/native-first.stderr" --first-record --policy exact
    gate_output select "$run_out/oracle.stderr" "$run_out/oracle-first.stderr" --first-record --policy exact
    gate_output compare "$run_out/native-first.stderr" "$run_out/oracle-first.stderr" --policy "$first_line_policy"
    first="$run_out/native-first.stderr"
    if [ "$mode" = 'generic exception' ]; then
        diagnostic='Unhandled exception. Program+GenericFailure`1[System.Int32]: escaped generic exception'
    elif [ "$mode" = 'message override' ]; then
        diagnostic='Unhandled exception. Program+OverriddenMessage: overridden message'
    elif [ "$mode" = 'cts timer' ]; then
        diagnostic='Unhandled exception. System.AggregateException: One or more errors occurred. (escaped cts timer)'
    else
        diagnostic="Unhandled exception. System.InvalidOperationException: escaped $mode"
    fi
    gate_assert_text "$first" "$diagnostic"
    gate_output abort-parity "$run_out/native" "$run_out/oracle" --host "$DN2CPP_OS" \
        --witness "throwing $mode" --diagnostic "$diagnostic"
    # CTS aggregation throws in native code after the managed callback returns.
    if [ "$mode" != 'cts timer' ]; then
        grep -q '^   at ' "$run_out/native.stderr" \
            || { echo "FAIL: $mode report has no managed throw-time trace" >&2; exit 1; }
    fi
    echo "OK worker $mode: reported managed fault and aborted with $native_code"
done
}

corelib_diff_abort_gate UnhandledExitSubset 'before throw' \
    'Unhandled exception. System.InvalidOperationException: escaped Main'
unset -f gate_extra_asserts

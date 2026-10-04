#!/usr/bin/env bash
# Process termination and managed diagnostics on exceptions escaping Main,
# pool work, local continuations, nested drains, threads, timers and async void,
# including a closed generic nested exception's CLR type display.
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
DN2CPP_GATE_EXTRA_CONTEXT="${DN2CPP_GATE_EXTRA_CONTEXT:-}|worker-modes:pool,thread,timer,async-void,local-continuation,nested-drain,generic-exception"

corelib_diff_gate UnhandledExitSubset

# Throw-time-trace arm: the unhandled report must carry the trace captured at throw. The
# stdout+exit diff above stays pinned to real .NET; stderr can never join that
# diff (trace formatting and frame names differ), so it is asserted by hand
# here: at least one "   at " trace line, and the frame of the method the
# exception escaped from. Contains-only on purpose — frame COUNT and the
# neighboring frames are best-effort under -O2 (an inlined or unresolvable
# frame simply drops); what the feature guarantees is that the report names
# the throw path.
echo "== Throw-time-trace arm: unhandled report carries the trace captured at throw =="
# The OUT the wrapper transpiled into, not a re-derived one: on a non-default
# axis (-hwy/-scalar/$DN2CPP_OUT_SUFFIX) a literal path would send the
# compile_console below at a directory this run never wrote, and the trace
# asserts would then certify a stale binary.
out="$_CG_OUT"
project=UnhandledExitSubset
# Compile unconditionally. `[ -x ]` asks whether SOME binary is there, not
# whether it came from this run's transpile — a stale executable left by an
# earlier run satisfies it, and the trace asserts below then certify code that
# is no longer in the tree. This arm is deliberately outside the cache
# region; a presence shortcut here partly undoes that. compile_console is
# ccache-backed, so a genuinely unchanged build costs almost nothing.
compile_console "$out" "$project"
set +e
err=$("./$out/$project" 2>&1 >/dev/null); code=$?
set -e
if [ "$code" -eq 0 ]; then
    echo "FAIL: the unhandled-exit binary exited 0 — an escaped exception must abort" >&2
    exit 1
fi
# A signal death with NO output at all is not this feature failing — it is the
# binary never having run, and the assert below would blame the trace for a
# staging bug, which is a real failure mode: on macOS the exec of a path
# whose bytes were rewritten IN PLACE after that path had already been exec'd is
# SIGKILLed by the kernel, silently and without a crash report, so the arm saw
# 137 and an empty stderr. This arm is the one place in the suite that re-execs
# a binary it has already run, which is why it is the only gate that ever showed
# it and why the check belongs here rather than in _common.sh. Six lanes spent a
# day on it because the failure carried no attribution; it does now.
if [ "$code" -ge 128 ] && [ -z "$err" ]; then
    echo "FAIL: the binary died of signal $((code - 128)) before producing any output — it did not run." >&2
    echo "      On macOS that is what an exec of an in-place-rewritten executable does. Check that" >&2
    echo "      compile_console still stages through stage_binary (copy beside + rename, i.e. a fresh" >&2
    echo "      inode) and not a plain cp onto $out/$project. See stage_binary in gates/_common.sh." >&2
    exit 1
fi
err=$(strip_cr_win "$err")
if ! grep -q '^   at ' <<<"$err"; then
    echo "FAIL: unhandled-exception stderr carries no '   at ' trace line" >&2
    printf '%s\n' "$err" >&2
    exit 1
fi
if ! grep -q '^   at Program\.Main()' <<<"$err"; then
    echo "FAIL: unhandled-exception stderr trace does not name the throw path (Program.Main)" >&2
    printf '%s\n' "$err" >&2
    exit 1
fi
echo "OK — the unhandled report names Program.Main in a real '   at ' trace"

echo "== Escaping-worker arms: managed first line and abnormal exit match real .NET =="
for mode in pool thread timer 'async void' 'local continuation' 'nested drain' 'generic exception'; do
    set +e
    native=$(run_bounded "./$out/$project$EXE_EXT" "$mode" 2>"$out/worker.err"); native_code=$?
    expected=$(run_bounded dotnet "$_CG_APP" "$mode" 2>"$out/worker-oracle.err"); expected_code=$?
    set -e
    native=$(strip_cr_win "$native")
    expected=$(strip_cr_win "$expected")
    assert_output "$native" "$expected"
    assert_output "$native" "before throw
-- escaping worker: $mode --
throwing $mode"
    case "$expected_code" in
        0|1) echo "FAIL: real .NET did not terminate abnormally for $mode" >&2; exit 1 ;;
    esac
    assert_exit_code "$native_code" "$expected_code"
    first=$(strip_cr_win "$(head -n 1 "$out/worker.err")")
    oracle_first=$(strip_cr_win "$(head -n 1 "$out/worker-oracle.err")")
    assert_output "$first" "$oracle_first"
    if [ "$mode" = 'generic exception' ]; then
        assert_output "$first" 'Unhandled exception. Program+GenericFailure`1[System.Int32]: escaped generic exception'
    else
        assert_output "$first" "Unhandled exception. System.InvalidOperationException: escaped $mode"
    fi
    grep -q '^   at ' "$out/worker.err" \
        || { echo "FAIL: $mode report has no managed throw-time trace" >&2; exit 1; }
    echo "OK worker $mode: reported managed fault and aborted with $native_code"
done

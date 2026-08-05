#!/usr/bin/env bash
# Process termination on an exception escaping Main: real .NET reports to
# stderr and aborts (SIGABRT -> 134), it does not exit(1) — and corelib_diff_gate
# pins the native exit status to real .NET's. The generated main's catch funnel
# must therefore abort too, with stdout flushed first (Linux's abort() does not
# flush stdio on its own). Terminal by nature — the program dies mid-section —
# so this lives in its own tiny gate rather than as a bucket section, like
# EnvSubset (Environment.Exit) and Finalizers (finalizer abort) before it.
# A second arm reruns the binary capturing stderr and asserts the report
# carries the throw-time trace — see its comment below.
source "$(dirname "$0")/_common.sh"

corelib_diff_gate UnhandledExitSubset

# Throw-time-trace arm: the unhandled report must carry the trace captured at throw. The
# stdout+exit diff above stays pinned to real .NET; stderr can never join that
# diff (the two runtimes' report wording differs), so it is asserted by hand
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

#!/usr/bin/env bash
# Environment.FailFast aborts the process — it does not quietly do nothing.
#
# FailFast used to sit in CoreIntrinsics' bounded-method set, whose call-site
# neutralization drops the arguments and pushes the default result. FailFast returns
# void, so "the default result" was NOTHING: the call vanished and execution continued
# past a request to halt on corrupted state. It is an intrinsic now
# (dn2cpp_environment_failfast: report on stderr, flush stdio, abort), and this gate is
# what keeps it one.
#
# Terminal by nature — the program dies mid-run — so it lives in its own tiny gate
# rather than as a section of a multi-section bucket, like UnhandledExitSubset (an
# exception escaping Main), EnvSubset (Environment.Exit) and Finalizers (finalizer
# abort) before it. It runs TWICE, because a process can only fail fast once and BOTH
# overloads of the family must be proven to abort for real (the sibling-overload rule:
# a half-mapped family is how a silent miscompile gets in).
#
# Real .NET is the oracle for stdout AND for the exit status: FailFast terminates
# abnormally (SIGABRT; a Unix shell reports 134), it does not exit(1). stderr stays off
# the diff — both runtimes report there, but with different wording (real .NET prints
# "Process terminated." plus a managed stack trace; dn2cpp prints the same header
# without the trace it does not keep).
source "$(dirname "$0")/_common.sh"

project=FailFastSubset
out="artifacts/$(printf '%s' "$project" | tr '[:upper:]' '[:lower:]')"

echo "== 1/4 Locating the real CoreLib =="
corelib=$(locate_corelib)
echo "corlib: $corelib"

echo "== 2/4 Building app assembly =="
build_proj "samples/dotnet/$project/$project.csproj"
app="samples/dotnet/$project/bin/$CONFIG/$TFM/$project.dll"

echo "== 3/4 Transpiling app + real CoreLib (tree-shaken) =="
invoke_cli "$app" -r "$corelib" -o "$out"
if gate_cache_check "$out" "failfast|$project|$corelib" \
        "$app" "${app%.dll}.runtimeconfig.json" "${app%.dll}.deps.json"; then
    gate_cache_hit_msg
    exit 0
fi

echo "== 4/4 Compiling C++ and running both overloads (exact diff vs real .NET) =="
compile_console "$out" "$project"

# Run 1 (no argument): FailFast(string, Exception).
# Run 2 (one argument): FailFast(string).
# In both, stdout must stop dead at "before failfast" — no BUG: witness may appear —
# and the status must be real .NET's abort.
for arg in "" "corrupted-heap"; do
    label="${arg:-<none>}"
    echo "-- overload run: args=$label"
    set +e
    if [ -z "$arg" ]; then
        native=$("./$out/$project" 2>"$out/native.err"); native_code=$?
        expected=$(dotnet "$app" 2>"$out/dotnet.err"); expected_code=$?
    else
        native=$("./$out/$project" "$arg" 2>"$out/native.err"); native_code=$?
        expected=$(dotnet "$app" "$arg" 2>"$out/dotnet.err"); expected_code=$?
    fi
    set -e
    assert_output "$(strip_cr_win "$native")" "$(strip_cr_win "$expected")"
    assert_exit_code "$native_code" "$expected_code"
    # The oracle itself must be an abort, not a clean exit — otherwise the two sides
    # could agree on a status that proves nothing. On Unix an aborted process is
    # reported as 128+SIGABRT = 134; Windows FailFast reports its own fatal status.
    # Either way it is neither 0 nor the 1 a caught-and-reported exception would give.
    case "$expected_code" in
        0|1)
            echo "FAIL: real .NET's FailFast exited $expected_code — that is not an abort." >&2
            echo "      The oracle no longer proves the process died; re-audit before trusting it." >&2
            exit 1
            ;;
    esac
    # WHICH overload ran is observable ONLY on stderr, and nothing else here
    # looks: both runs print the same single stdout line and abort with the same
    # status, so the loop's whole reason for existing — the sibling-overload rule
    # — was unenforced. argv silently not reaching Main on the dn2cpp side, or the
    # sample ceasing to branch, would have re-run overload 1 twice and stayed
    # green. stderr still cannot join the diff (the two runtimes word their
    # reports differently), so each side is asserted against its own message.
    if [ -z "$arg" ]; then
        needle="halting on corrupted state"
    else
        needle="halting: $arg"
    fi
    for side in native dotnet; do
        grep -qF "$needle" "$out/$side.err" || {
            echo "FAIL: the $side run's FailFast report does not carry \"$needle\" — this run did not take the overload it was meant to" >&2
            cat "$out/$side.err" >&2
            exit 1; }
    done
    # The two-argument overload additionally has to render its Exception; that is
    # the only positive evidence distinguishing it from the one-argument one.
    if [ -z "$arg" ]; then
        grep -qF "the cause" "$out/native.err" || {
            echo "FAIL: FailFast(string, Exception) did not render its exception — the second argument was dropped" >&2
            cat "$out/native.err" >&2
            exit 1; }
    fi
    echo "   aborted with status $native_code (real .NET: $expected_code), report names \"$needle\""
done

gate_cache_commit
echo "OK — Environment.FailFast aborts the process, both overloads, exactly as real .NET does."

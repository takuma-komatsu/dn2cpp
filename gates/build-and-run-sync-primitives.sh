#!/usr/bin/env bash
# Blocking synchronization primitives on real threads — SemaphoreSlim (N consumers
# wait, producer releases N), ManualResetEventSlim (gate N workers, one Set releases all,
# IsSet), AutoResetEvent (strict ping-pong, 3 turns each), Monitor condition signaling
# (Pulse/PulseAll preserve the wait-set identity),
# CountdownEvent (N workers Signal, main Wait), and Barrier (N threads run P phases,
# with a post-phase action). MethodImplSubset.cs adds MethodImplAttribute:
# [MethodImpl(Synchronized)] instance/static/recursive/throwing bodies hammered
# by racing threads, lock(typeof(X)) identity (interned Type objects) and its
# mutual exclusion with static Synchronized methods, plus the NoInlining/
# AggressiveInlining hints. Every result is read after Join, so the output is
# deterministic and diffed exact vs real .NET. CountdownEvent/Barrier live in
# the System.Threading assembly (not CoreLib), so it is referenced alongside
# CoreLib. RwLockRecursionSubset.cs asserts ReaderWriterLockSlim's per-thread
# ownership: the LockRecursionException matrix under NoRecursion AND
# SupportsRecursion (type + message verbatim — each of these used to be a silent
# same-thread HANG), the upgradeable holder's granted read/upgrade paths, the
# per-thread Is*LockHeld queries, RecursionPolicy, and the
# SynchronizationLockException release-without-hold checks.
# Blocking timeout validation, receiver order, signal preservation and boxed fields.
# Thread construction, start and join state.
source "$(dirname "$0")/_common.sh"
sync_python=$(resolve_python) || gate_skip "no working Python 3 interpreter for blocking call fixtures"
call_app="gates/fixtures/blocking-timeout-call/bin/$CONFIG/$TFM/BlockingTimeoutCall.dll"
build_gate_proj gates/fixtures/blocking-timeout-call/BlockingTimeoutCall.csproj
DN2CPP_GATE_EXTRA_INPUTS="${DN2CPP_GATE_EXTRA_INPUTS:-} $call_app ${call_app%.dll}.runtimeconfig.json ${call_app%.dll}.deps.json gates/fixtures/blocking-timeout-call/patch-call.py"
DN2CPP_GATE_EXTRA_CONTEXT="${DN2CPP_GATE_EXTRA_CONTEXT:-}|blocking-timeout-call|cli:$(_gate_cli_hash)"
gate_extra_asserts() {
    local out="$1" native before prefix line
    native=$(run_bounded "./$out/SyncPrimitives")
    native=$(strip_cr_win "$native")
    before=$(run_bounded dotnet "$_CG_APP" before-null-receivers)
    prefix=$(awk '/^== null receivers ==$/ { exit } { print }' <<< "$native")
    assert_output "$prefix" "$(strip_cr_win "$before")"
    grep -Fxq 'null receivers end' <<< "$native" \
        || { echo "FAIL: primitive receiver section did not run" >&2; exit 1; }
    before=$(run_bounded dotnet "$_CG_APP" before-thread-lifecycle)
    before=$(strip_cr_win "$before")
    prefix=$(awk '/^== thread states ==$/ { exit } { print }' <<< "$native")
    assert_output "$prefix" "$before"
    for line in '== thread states ==' \
        'thread states end'; do
        grep -Fxq -- "$line" <<< "$native" \
            || { echo "FAIL: SyncPrimitives lifecycle witness missing: $line" >&2; exit 1; }
    done
    before=$(dotnet "$_CG_APP" before-timeout-fields)
    before=$(strip_cr_win "$before")
    prefix=$(awk '/^== wait timeouts ==$/ { exit } { print }' <<< "$native")
    assert_output "$prefix" "$before"
    for line in '== wait timeouts ==' \
        'Barrier phase=0 remaining=1' \
        'held: False False False' \
        'wait timeouts end' \
        'blocking timeout fields end'; do
        grep -Fxq -- "$line" <<< "$native" \
            || { echo "FAIL: SyncPrimitives timeout witness missing: $line" >&2; exit 1; }
    done
    local oracle="$out/direct-call-oracle" fixture="$out/direct-call" expected actual
    mkdir -p "$oracle"
    cp "$call_app" "$oracle/BlockingTimeoutCall.dll"
    cp "${call_app%.dll}.runtimeconfig.json" "${call_app%.dll}.deps.json" "$oracle/"
    $sync_python gates/fixtures/blocking-timeout-call/patch-call.py "$oracle/BlockingTimeoutCall.dll"
    invoke_cli "$oracle/BlockingTimeoutCall.dll" -r "$_CG_CORELIB" --auto-ref -o "$fixture"
    compile_console "$fixture" BlockingTimeoutCall
    expected=$(run_bounded dotnet "$oracle/BlockingTimeoutCall.dll")
    actual=$(run_bounded "./$fixture/BlockingTimeoutCall")
    actual=$(strip_cr_win "$actual")
    assert_output "$actual" "$(strip_cr_win "$expected")"
    for line in 'Sem null int bad type=NullReferenceException' \
        'Sem null span bad actual=TimeSpan:-00:00:00.0020000' \
        'Mre null int bad type=NullReferenceException' \
        'WaitOne null int bad type=ArgumentOutOfRangeException' \
        'Barrier null int bad type=NullReferenceException' \
        'Lock null int bad actual=Int32:-2' \
        'Blocking timeout call faults end'; do
        grep -Fxq -- "$line" <<< "$actual" \
            || { echo "FAIL: blocking direct-call witness missing: $line" >&2; exit 1; }
    done
}
corelib_diff_gate SyncPrimitives System.Threading

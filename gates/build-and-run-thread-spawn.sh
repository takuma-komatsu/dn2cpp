#!/usr/bin/env bash
# Real OS threads (System.Threading.Thread). Spawns N worker threads that race a
# shared counter (Interlocked) and a locked accumulator, joins them, and reads results
# only after Join — so the output is fully deterministic (N*K) and diffed exact vs real
# .NET. Also covers ParameterizedThreadStart (boxed arg), a Volatile producer->consumer
# handoff, Thread.Sleep, and IsAlive. A racy atomics/lock implementation would not
# reproduce the exact totals, so this is the genuine cross-thread correctness gate.
# A cross-thread static-constructor cycle completes: the thread whose wait would close
# the cycle proceeds on the partial state, as .NET does, instead of deadlocking.
# Public thread names retain managed state; the private CoreCLR notification
# has a frozen capability boundary through direct calls, addresses and reflection.
# The retained Lock waiter-aging query shares that native-only boundary coverage.
source "$(dirname "$0")/_common.sh"
DN2CPP_GATE_EXTRA_INPUTS="${DN2CPP_GATE_EXTRA_INPUTS:-} gates/fixtures/ldftn-local/ThreadingPrivateFixture.cs gates/fixtures/ldftn-local/Program.cs gates/fixtures/ldftn-local/LdftnLocalFixture.csproj gates/expected/threading-private-boundary.txt"
DN2CPP_GATE_EXTRA_CONTEXT="${DN2CPP_GATE_EXTRA_CONTEXT:-}|prefix:before-init-cycle,before-thread-names|threading-private:notification,waiter-clock,malformed-reference,no-ildiet|cli:$(_gate_cli_hash)"
gate_prepare_app() {
    local project="$1" out="$2"
    build_proj gates/fixtures/ldftn-local/LdftnLocalFixture.csproj
    mkdir -p "$out/app"
    cp -R "samples/dotnet/$project/bin/$CONFIG/$TFM/." "$out/app/"
    _CG_APP="$out/app/$project.dll"
    gate_capture_ok "$out/thread-name-fixture" run_bounded dotnet \
        "gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll" \
        --threading-private "$_CG_APP" "$_CG_CORELIB" valid
    gate_output witness "$out/thread-name-fixture.stdout" \
        'thread-name fixture=valid direct=System.Void System.Threading.Thread::ThreadNameChanged(System.String) address=ldftn+calli:System.Void System.Threading.Thread::ThreadNameChanged(System.String)' \
        'lock-waiter fixture=valid address=ldftn+calli:System.Boolean System.Threading.Lock::get_ShouldStopPreemptingWaiters()' \
        'lock-waiter fixture=valid address=ldftn+calli:System.Void System.Threading.Lock::ResetWaiterStartTime()' \
        'lock-waiter fixture=valid address=ldftn+calli:System.Void System.Threading.Lock::RecordWaiterStartTime()' \
        --count 1 --policy crlf
}
gate_extra_asserts() {
    local out="$1" native before prefix line
    native="$out/native.stdout"
    gate_output status "$(cat "$out/native.status")" 0 || return 1

    gate_capture_ok "$out/assert-before-1" run_bounded dotnet "$_CG_APP" before-init-cycle
    before="$out/assert-before-1.stdout"

    prefix="$out/assert-prefix-2.stdout"

    gate_output select "$native" "$prefix" --before '== init cycle ==' --policy crlf
    gate_output compare "$prefix" "$before"
    for line in '== init cycle ==' \
        'init cycle joined=True' \
        'init cycle worker reads=1/2' \
        'init cycle values=1/2' \
        'init cycle end'; do
        gate_output witness "$native" "$line" --count any --policy crlf \
            || { echo "FAIL: init cycle witness missing: $line" >&2; exit 1; }
    done
    gate_capture_ok "$out/before-thread-names" run_bounded dotnet "$_CG_APP" before-thread-names
    gate_output select "$native" "$out/thread-name-prefix.stdout" --before '== thread names ==' --policy crlf
    gate_output compare "$out/thread-name-prefix.stdout" "$out/before-thread-names.stdout"
    gate_output witness "$native" 'thread fresh name=null' 'thread worker names=worker-before/worker-after' \
        'thread cleared name=null' 'thread empty name=True' 'thread names end' --count 1 --policy crlf

    gate_capture_ok "$out/thread-name-clr" run_bounded dotnet "$_CG_APP" threading-private-boundary
    gate_output witness "$out/thread-name-clr.stdout" \
        'thread notify direct=returned|unchanged=True' 'thread notify null name=returned|unchanged=True' \
        'thread notify address=returned|unchanged=True' 'thread notify reflection=returned|unchanged=True' \
        'thread notify null direct=NullReferenceException|unchanged=True' \
        'thread notify null address=NullReferenceException|unchanged=True' \
        'thread notify name effects=2' 'thread notification boundary end' --count 1 --policy crlf
    gate_output witness "$out/thread-name-clr.stdout" \
        'lock waiter direct=False' 'lock waiter address=False' 'lock waiter reflection=False' \
        'lock waiter null direct=NullReferenceException' 'lock waiter null address=NullReferenceException' \
        'lock reset direct=False' 'lock reset address=False' 'lock record direct=False' 'lock record address=False' \
        'lock reset null=NullReferenceException' 'lock record null=NullReferenceException' \
        'lock waiter boundary end' --count 1 --policy crlf
    gate_capture_ok "$out/thread-name-native" run_bounded "./$out/ThreadSpawn$EXE_EXT" threading-private-boundary
    gate_output witness "$out/thread-name-clr.stdout" 'scheduler inline address=True/False/True/42' --count 1 --policy crlf
    gate_output compare "$out/thread-name-native.stdout" gates/expected/threading-private-boundary.txt --policy crlf

    # These inputs differ in metadata, so execution of the aggregate cannot test them.
    local mode bad member
    for mode in return parameter header lock-return; do
        bad="$out/thread-name-$mode"
        mkdir -p "$bad/app"
        cp "$_CG_APP" "$bad/app/ThreadSpawn.dll"
        gate_capture_ok "$bad/fixture" run_bounded dotnet \
            "gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll" \
            --threading-private "$bad/app/ThreadSpawn.dll" "$_CG_CORELIB" "$mode"
        gate_capture "$bad/transpile" invoke_cli "$bad/app/ThreadSpawn.dll" -r "$_CG_CORELIB" --no-ildiet -o "$bad/out"
        gate_output status "$(cat "$bad/transpile.status")" 2 || return 1
        member='Thread.ThreadNameChanged'
        [ "$mode" != lock-return ] || member='Lock.get_ShouldStopPreemptingWaiters'
        grep -Fq "$member requires its exact CoreLib" "$bad/transpile.stdout" "$bad/transpile.stderr" \
            || { echo "FAIL: invalid thread name $mode was not refused" >&2; return 1; }
    done
}
corelib_diff_gate ThreadSpawn

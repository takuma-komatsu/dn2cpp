#!/usr/bin/env bash
# Consolidated async-combinators gate: Task.WhenAll/WhenAny, WhenAll over an
# enumerable, ConfigureAwait, Task.Delay ordering, CancellationToken, a custom
# awaitable, and multiple awaiter types. Diffed exactly vs real .NET.
# WaitAsyncContinueWithSubset.cs asserts Task.WaitAsync(CancellationToken)'s race in
# both directions and Task.ContinueWith's CONDITIONAL continuations (every
# TaskContinuationOptions filter spelling against every antecedent outcome, including
# that an excluded continuation is CANCELED rather than completed).
# BlockingWaitWrapSubset.cs asserts the blocking-wait wrap contract:
# Task.Wait()/Wait(TimeSpan)/Task<T>.Result wrap a fault or cancellation in an
# AggregateException (with .NET's composed Message) while GetAwaiter().GetResult()
# re-raises unwrapped, and a canceled task carries a TaskCanceledException with
# .NET's default message.
# BlockingWaitArgsSubset.cs asserts the argument contracts of the BLOCKING waits
# (Task.WaitAny/WaitAll) and of Task.WhenAll — the entry-point validation that keeps
# the C++ runtime's remaining aborts unreachable. WaitAny's contract is deliberately
# NOT WhenAny's on either count (empty array, null-element exception type), and the
# null scan precedes both the index answer and the wait, so neither a settled nor a
# pending task ahead of a null one hides the rejection.
# SettledCombinatorsSubset.cs asserts that Task.WhenAll/WhenAny over ALREADY-SETTLED
# inputs complete before the combinator returns — every row is read without waiting,
# since a wait ahead of the read passes whether the join finished inline or was posted
# to the scheduler; the mixed rows hold the other side, that one pending input still
# leaves the join pending.
# WhenAllFaultSetSubset.cs asserts that Task.WhenAll's fault set is EVERY faulted
# input rather than the first — a nested join flattens into its own inner set and a
# cancellation alongside a fault contributes nothing — and that the three mouths that
# mint an AggregateException over a task (Task.Exception, the blocking wait, and
# Task.WaitAll) agree on that set while the awaiter raises its first element unwrapped.
# TaskDelegateContractSubset.cs asserts the delegate contract of the RESULT-returning
# Task.Run / Task.Factory.StartNew overloads (stateless and the Func<object,TResult> +
# state form) and of the cold `new Task(...)` / `new Task<T>(...)` constructors: a null
# delegate is rejected synchronously at the entry with .NET's ArgumentNullException text
# (the constructors name paramName "action" for every kind, the Func ones included), and
# a COMBINED delegate runs every handler front-to-back with the task's result taken from
# the last one — including a STRUCT result, whose transpiler-stamped boxing trampoline
# walks the invocation chain itself rather than leaning on a runtime thunk, and including
# every result kind of a ContinueWith continuation, which answers through thunks of its
# own and so needs the walk written into each one, and a Task-RETURNING delegate, where
# the last handler's task is the one Run unwraps and StartNew hands back — an earlier
# handler's async fault stays unobserved in its own task, while its synchronous throw
# stops the chain and faults the outer, one handler may return a task settled only by a
# later handler without deadlocking, and a null task unwraps into a cancellation.
# Task duration, receiver, continuation and exception-argument validation.
# Former gates: whenall, whenany, when-enumerable, configure-await, delay-order,
# cancellation, custom-awaitable, multi-awaiter.
# Task sequences and cold scheduling.
source "$(dirname "$0")/_common.sh"
task_python=$(resolve_python) || gate_skip "no working Python 3 interpreter for task call fixtures"
call_app="gates/fixtures/task-call-validation/bin/$CONFIG/$TFM/TaskCallValidation.dll"
build_gate_proj gates/fixtures/task-call-validation/TaskCallValidation.csproj
DN2CPP_GATE_EXTRA_INPUTS="${DN2CPP_GATE_EXTRA_INPUTS:-} $call_app ${call_app%.dll}.runtimeconfig.json ${call_app%.dll}.deps.json gates/fixtures/task-call-validation/patch-call.py"
DN2CPP_GATE_EXTRA_CONTEXT="${DN2CPP_GATE_EXTRA_CONTEXT:-}|task-call-validation|cli:$(_gate_cli_hash)"

gate_extra_asserts() {
    local out="$1" native before prefix line
    native=$(run_bounded "./$out/AsyncCombinators")
    native=$(strip_cr_win "$native")
    before=$(run_bounded dotnet "$_CG_APP" before-cancellation-receivers)
    before=$(strip_cr_win "$before")
    prefix=$(awk '/^== cancellation source receivers ==$/ { exit } { print }' <<< "$native")
    assert_output "$prefix" "$before"
    for line in '== cancellation source receivers ==' \
        'Token=NullReferenceException' 'IsCancellationRequested=NullReferenceException' \
        'Cancel=NullReferenceException' 'Cancel(false)=NullReferenceException' \
        'Cancel(true)=NullReferenceException' 'Dispose=NullReferenceException' \
        'live before=False/False' 'live after=True/True' \
        'live Cancel(false)=True/True' 'live Cancel(true)=True/True' \
        'cancellation source receivers end'; do
        grep -Fxq -- "$line" <<< "$native" \
            || { echo "FAIL: cancellation receiver witness missing: $line" >&2; exit 1; }
    done
    before=$(run_bounded dotnet "$_CG_APP" before-task-lifecycle)
    before=$(strip_cr_win "$before")
    prefix=$(awk '/^whenall-null-seq:/ { exit } { print }' <<< "$native")
    assert_output "$prefix" "$before"
    for line in '== sequence nulls ==' \
        'sequence nulls end' \
        '== cold task scheduler ==' \
        'scheduler: True,True,True' \
        'cold task scheduler end' \
        '== task origin ==' \
        'singleton identity/type: True,True' \
        'singleton cold body: 1' \
        'task origin end'; do
        grep -Fxq -- "$line" <<< "$native" \
            || { echo "FAIL: AsyncCombinators lifecycle witness missing: $line" >&2; exit 1; }
    done
    before=$(dotnet "$_CG_APP" before-task-validation)
    before=$(strip_cr_win "$before")
    prefix=$(awk '/^== delay arguments ==$/ { exit } { print }' <<< "$native")
    assert_output "$prefix" "$before"
    for line in '== delay arguments ==' \
        'delay arguments end' \
        '== task receivers ==' \
        'sources still pending: WaitingForActivation WaitingForActivation' \
        'task receivers end'; do
        grep -Fxq -- "$line" <<< "$native" \
            || { echo "FAIL: AsyncCombinators validation witness missing: $line" >&2; exit 1; }
    done
    local oracle="$out/direct-call-oracle" fixture="$out/direct-call" expected actual
    mkdir -p "$oracle"
    cp "$call_app" "$oracle/TaskCallValidation.dll"
    cp "${call_app%.dll}.runtimeconfig.json" "${call_app%.dll}.deps.json" "$oracle/"
    $task_python gates/fixtures/task-call-validation/patch-call.py "$oracle/TaskCallValidation.dll"
    invoke_cli "$oracle/TaskCallValidation.dll" -r "$_CG_CORELIB" --auto-ref -o "$fixture"
    compile_console "$fixture" TaskCallValidation
    expected=$(run_bounded dotnet "$oracle/TaskCallValidation.dll")
    actual=$(run_bounded "./$fixture/TaskCallValidation")
    actual=$(strip_cr_win "$actual")
    assert_output "$actual" "$(strip_cr_win "$expected")"
    for line in 'direct-get-awaiter-null|ok:constructed' \
        'direct-configure-null|ok:constructed' \
        'task direct-call validation end' \
        '== task direct scheduling ==' \
        'task direct scheduling end'; do
        grep -Fxq -- "$line" <<< "$actual" \
            || { echo "FAIL: Task direct-call witness missing: $line" >&2; exit 1; }
    done
}

corelib_diff_gate AsyncCombinators

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
# ValueTaskSourceHandoffSubset.cs asserts the IValueTaskSource consumption protocol over
# a pooled source whose producer reads its continuation slot only after reporting
# completion: an await that finds the operation complete reads it once and registers
# nothing, a suspending await registers once and is read once by its continuation, and
# AsTask over a completed operation reads it synchronously. A continuation the source
# stored before rejecting its registration reads the source once more, as .NET's
# orphaned AsTask continuation does, but settles nothing and ends no registration, so
# the task a later read produced keeps its result and the next registration's waiter
# keeps its settler.
# RegistrationRejectionSubset.cs asserts a continuation registration that throws: an
# explicit awaiter.OnCompleted or AsTask throws to its caller, while at a BCL builder's
# await suspension the throw is re-raised as an unhandled ThreadPool exception, so the
# suspended method's own catch never runs and the process aborts as real .NET's does.
# Each suspension mode ends its own run, after the whole default output.
# WhenAll preserves every enum underlying width and array identity for empty,
# settled, yielding, pending and enumerable inputs, including unsigned high bits.
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
    before=$(run_bounded dotnet "$_CG_APP" before-value-task-source-handoff)
    before=$(strip_cr_win "$before")
    prefix=$(awk '/^== value task source handoff ==$/ { exit } { print }' <<< "$native")
    assert_output "$prefix" "$before"
    for line in '== value task source handoff ==' \
        'op1 reported=True value=41' \
        'op2 producer=released joined=True value=42' \
        'op3 waited=registered value=43' \
        'op4 published=True completed=True value=44' \
        'counts: getresult=4 refused=0 registered=1 invoked=1 version=4' \
        'value task source handoff end'; do
        grep -Fxq -- "$line" <<< "$native" \
            || { echo "FAIL: ValueTask source handoff witness missing: $line" >&2; exit 1; }
    done
    before=$(run_bounded dotnet "$_CG_APP" before-value-task-source-rejection)
    before=$(strip_cr_win "$before")
    prefix=$(awk '/^== value task source rejected registration ==$/ { exit } { print }' <<< "$native")
    assert_output "$prefix" "$before"
    for line in '== value task source rejected registration ==' \
        'op5 orphan=InvalidOperationException:stale token' \
        'op6 armed=True continuation=returned joined=True value=46' \
        'op7 status=RanToCompletion result=ok:47' \
        'op8 completed=InvalidOperationException:stale token' \
        'rejection counts: getresult=4 refused=0 registered=4 version=4' \
        'value task source rejected registration end'; do
        grep -Fxq -- "$line" <<< "$native" \
            || { echo "FAIL: ValueTask rejected registration witness missing: $line" >&2; exit 1; }
    done
    before=$(run_bounded dotnet "$_CG_APP" before-registration-rejection)
    before=$(strip_cr_win "$before")
    prefix=$(awk '/^== continuation registration rejection ==$/ { exit } { print }' <<< "$native")
    assert_output "$prefix" "$before"
    for line in '== continuation registration rejection ==' \
        'OnCompleted InvalidOperationException:source rejected the continuation' \
        'UnsafeOnCompleted InvalidOperationException:source rejected the continuation' \
        'configured OnCompleted InvalidOperationException:source rejected the continuation' \
        'non-generic OnCompleted InvalidOperationException:source rejected the continuation' \
        'AsTask InvalidOperationException:source rejected the continuation' \
        'registrations=5' \
        'continuation registration rejection end'; do
        grep -Fxq -- "$line" <<< "$native" \
            || { echo "FAIL: registration rejection witness missing: $line" >&2; exit 1; }
    done
    before=$(run_bounded dotnet "$_CG_APP" before-enum-whenall)
    before=$(strip_cr_win "$before")
    prefix=$(awk '/^== enum WhenAll results ==$/ { exit } { print }' <<< "$native")
    assert_output "$prefix" "$before"
    for line in '== enum WhenAll results ==' \
        'enum byte values: 255,1,128' 'enum sbyte values: -128,127,-1' \
        'enum short values: -32768,32767,-1' 'enum ushort values: 65535,1,32768' \
        'enum int values: -2147483648,2147483647,-1' \
        'enum uint values: 4294967295,1,2147483648' \
        'enum long values: -9223372036854775808,9223372036854775807,-1' \
        'enum ulong values: 18446744073709551615,1,9223372036854775808' \
        'enum WhenAll results end'; do
        grep -Fxq -- "$line" <<< "$native" \
            || { echo "FAIL: enum WhenAll result witness missing: $line" >&2; exit 1; }
    done
    for line in byte sbyte short ushort int uint long ulong; do
        grep -Fxq -- "enum $line: empty=True/0/True settled=True/True yielded=True enumerable=True pending=False/False/True/True" <<< "$native" \
            || { echo "FAIL: enum WhenAll completion/type/value witness missing: $line" >&2; exit 1; }
    done
    local mode child child_code dotnet_child dotnet_code
    for mode in value-task-source:'source OnCompleted #6' \
        custom-awaiter:'awaiter UnsafeOnCompleted' async-void:'source OnCompleted #6'; do
        set +e
        child=$(run_bounded "./$out/AsyncCombinators" suspension-rejection "${mode%%:*}" \
            2>"$out/suspension.err"); child_code=$?
        dotnet_child=$(run_bounded dotnet "$_CG_APP" suspension-rejection "${mode%%:*}" \
            2>"$out/suspension-oracle.err"); dotnet_code=$?
        set -e
        child=$(strip_cr_win "$child")
        dotnet_child=$(strip_cr_win "$dotnet_child")
        prefix=$(awk -v h="== builder suspension rejection: ${mode%%:*} ==" \
            '$0 == h { exit } { print }' <<< "$child")
        # The whole default run, then the mode's header and its one registration: the
        # suspended method neither caught the throw nor completed.
        if [ "$child" != "$dotnet_child" ] || [ "$prefix" != "$native" ] \
                || [ "$(tail -n 2 <<< "$child")" != "== builder suspension rejection: ${mode%%:*} ==
${mode#*:}" ]; then
            echo "FAIL: suspension rejection (${mode%%:*}) output differs from real .NET" >&2
            gate_run_diag "native ${mode%%:*}" "$child_code" "$(tail -n 4 <<< "$child")" "$out/suspension.err"
            gate_run_diag "dotnet ${mode%%:*}" "$dotnet_code" "$(tail -n 4 <<< "$dotnet_child")" "$out/suspension-oracle.err"
            exit 1
        fi
        case "$dotnet_code" in
            0|1) echo "FAIL: real .NET exited $dotnet_code after a rejected suspension; it must abort" >&2
                exit 1 ;;
        esac
        assert_exit_code "$child_code" "$dotnet_code"
        grep -Fq 'Unhandled exception. System.InvalidOperationException: ' "$out/suspension-oracle.err" \
            || { echo "FAIL: real .NET did not report the rejected suspension as unhandled" >&2; exit 1; }
        grep -Fq 'dn2cpp fatal: threadpool: unhandled managed exception' "$out/suspension.err" \
            || { echo "FAIL: the rejected suspension (${mode%%:*}) did not end as an unhandled ThreadPool exception" >&2
                cat "$out/suspension.err" >&2; exit 1; }
        echo "OK suspension rejection ${mode%%:*}: aborted with $child_code after '${mode#*:}'"
    done
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

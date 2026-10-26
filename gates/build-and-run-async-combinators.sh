#!/usr/bin/env bash
# Consolidated async-combinators gate: Task.WhenAll/WhenAny, WhenAll over an
# enumerable, ConfigureAwait, synchronized Delay consumers, CancellationToken, a custom
# awaitable, and multiple awaiter types. Diffed exactly vs real .NET.
# Infinite integer/TimeSpan delays prove suspension until controlled cancellation.
# Unsynchronized virtual deadline order is frozen separately from the live oracle.
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
# Pending inputs notify joins before their completion returns, including nested
# joins and concurrent producers; ordinary await callbacks remain queued.
# Cancellation callback faults preserve LIFO order and identity, aggregate by
# default, and stop on the first exception only for Cancel(true).
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
# Task and cancellation aggregate construction leaves virtual inner Messages unread.
source "$(dirname "$0")/_common.sh"
task_python=$(resolve_python) || gate_skip "no working Python 3 interpreter for task call fixtures"
call_app="gates/fixtures/task-call-validation/bin/$CONFIG/$TFM/TaskCallValidation.dll"
build_gate_proj gates/fixtures/task-call-validation/TaskCallValidation.csproj
DN2CPP_GATE_EXTRA_INPUTS="${DN2CPP_GATE_EXTRA_INPUTS:-} $call_app ${call_app%.dll}.runtimeconfig.json ${call_app%.dll}.deps.json gates/fixtures/task-call-validation/patch-call.py"
DN2CPP_GATE_EXTRA_CONTEXT="${DN2CPP_GATE_EXTRA_CONTEXT:-}|task-call-validation|cli:$(_gate_cli_hash)"
DN2CPP_GATE_EXTRA_CONTEXT="$DN2CPP_GATE_EXTRA_CONTEXT|prefix:before-pending-task-joins"
DN2CPP_GATE_EXTRA_CONTEXT="$DN2CPP_GATE_EXTRA_CONTEXT|prefix:before-cancellation-callback-faults"
DN2CPP_GATE_EXTRA_CONTEXT="$DN2CPP_GATE_EXTRA_CONTEXT|prefix:before-lazy-aggregate-message"
DN2CPP_GATE_EXTRA_CONTEXT="$DN2CPP_GATE_EXTRA_CONTEXT|prefix:before-delay-synchronization"

# The exception text is platform-owned; its record prefix is the boundary.
async_lifecycle_prefix() {
    local actual="$1" expected="$2" prefix="$3"
    if ! gate_output select "$actual" "$prefix" --before-prefix 'whenall-null-seq:' --policy exact; then
        echo 'FAIL: task lifecycle boundary must occur exactly once' >&2
        return 1
    fi
    gate_output compare "$prefix" "$expected"
}

gate_extra_asserts() {
    local out="$1" native before prefix line
    native="$out/native.stdout"
    before="$out/before-delay-synchronization.stdout"
    gate_capture_ok "${before%.stdout}" run_bounded dotnet "$_CG_APP" before-delay-synchronization
    gate_output prefix "$native" "$before" --before '== delay synchronization ==' --policy crlf
    gate_output byte-witness "$native" '== delay synchronization ==' --policy crlf \
        || { echo 'FAIL: delay synchronization block did not run once' >&2; return 1; }
    printf '%s\n' 'delay synchronization end' > "$out/delay-end.expected"
    gate_output suffix "$native" "$out/delay-end.expected" --policy crlf \
        || { echo 'FAIL: delay synchronization block did not complete last' >&2; return 1; }
    for line in 'integer suspension: True,True|True,True' \
        'TimeSpan suspension: True,True|True,True' \
        'dependency pending: True,True|0' 'dependency order: firstsecond'; do
        gate_output byte-witness "$native" "$line" --count any --policy crlf \
            || { echo "FAIL: delay synchronization witness missing: $line" >&2; return 1; }
    done
    before="$out/before-lazy-aggregate-message.stdout"
    gate_capture_ok "${before%.stdout}" run_bounded dotnet "$_CG_APP" before-lazy-aggregate-message
    gate_output prefix "$native" "$before" --before '== lazy task and cancellation Message ==' --policy crlf
    for line in '== lazy task and cancellation Message ==' \
        'task Exception result=AggregateException reads=0/0' \
        'task Wait result=AggregateException reads=0/0' \
        'task Result result=AggregateException reads=0/0' \
        'task awaiter result=MessageFault reads=0/0' 'task awaiter direct identity=True' \
        'cancel default result=AggregateException reads=0/0' \
        'cancel false result=AggregateException reads=0/0' \
        'cancel true result=MessageFault reads=0/0' 'cancel true direct identity=True' \
        'cancel false calls=last/first/ canceled=True' \
        'cancel true calls=last/ canceled=True' \
        'linked False constructed reads=0 identity=True' \
        'linked False Message=One or more errors occurred. (One or more errors occurred. (linked:2)) reads=2' \
        'linked True constructed reads=0 identity=True' \
        'linked True Message=One or more errors occurred. (linked:2) reads=2' \
        'lazy task and cancellation Message end'; do
        gate_output byte-witness "$native" "$line" --count any --policy crlf \
            || { echo "FAIL: task/cancellation Message witness missing: $line" >&2; return 1; }
    done
    before="$out/before-cancellation-callback-faults.stdout"
    gate_capture_ok "${before%.stdout}" run_bounded dotnet "$_CG_APP" before-cancellation-callback-faults
    gate_output prefix "$native" "$before" --before '== cancellation callback faults ==' --policy crlf
    for line in '== cancellation callback faults ==' \
        'cancel default: newer;older;remaining;|AggregateException/2|identity=True|requested=True/True' \
        'cancel false: newer;older;remaining;|AggregateException/2|identity=True|requested=True/True' \
        'cancel true: newer;|InvalidOperationException|identity=True|requested=True/True' \
        'cancel state/token False: token;state;remaining;|AggregateException/2|identity=True/True/True' \
        'cancel state/token True: token;|InvalidOperationException|identity=True/False/True' \
        'cancel linked False: child-newer;child-older;parent;|AggregateException/2|nested=True|identity=True|requested=True/True' \
        'cancel linked True: child-newer;child-older;|AggregateException/2|nested=False|identity=True|requested=True/True' \
        'cancel reentrant: outer;immediate;remaining;|AggregateException/1|identity=True' \
        'cancel already requested register: InvalidOperationException|identity=True' \
        'cancel collected: newer;older;remaining;|2|collected newer,collected older' \
        'cancellation callback faults end'; do
        gate_output byte-witness "$native" "$line" --count any --policy crlf \
            || { echo "FAIL: cancellation callback fault witness missing: $line" >&2; exit 1; }
    done
    before="$out/before-pending-task-joins.stdout"
    gate_capture_ok "${before%.stdout}" run_bounded dotnet "$_CG_APP" before-pending-task-joins
    gate_output prefix "$native" "$before" --before '== pending task joins ==' --policy crlf
    for line in '== pending task joins ==' \
        'pending int: False,False,True|2|7,9' \
        'pending long: False,False,True|2|4294967296,-4294967297' \
        'pending string: False,False,True|2|left,right' \
        'pending object: False,False,True|2|11,value' \
        'pending enum: False,False,True|2|Low,High' \
        'pending struct: False,False,True|2|11/left,12/right' \
        'pending nongeneric before: False,False' \
        'pending nongeneric partial: False,True' \
        'pending nongeneric final: True,True|True,True' \
        'pending fault partial: False,True|False,True' \
        'pending fault final: True,True,False|1,pending fault' \
        'pending cancel partial: False,True|False,True' \
        'pending cancel final: True,True,False|True' \
        'pending nested: True,True,True|True' \
        'pending worker: True,True|4,5,True' \
        'pending races: 32,32,32' \
        'ordinary await inline: False' \
        'pending task joins end'; do
        gate_output byte-witness "$native" "$line" --count any --policy crlf \
            || { echo "FAIL: pending task join witness missing: $line" >&2; exit 1; }
    done
    before="$out/before-value-task-source-handoff.stdout"
    gate_capture_ok "${before%.stdout}" run_bounded dotnet "$_CG_APP" before-value-task-source-handoff
    gate_output prefix "$native" "$before" --before '== value task source handoff ==' --policy crlf
    for line in '== value task source handoff ==' \
        'op1 reported=True value=41' \
        'op2 producer=released joined=True value=42' \
        'op3 waited=registered value=43' \
        'op4 published=True completed=True value=44' \
        'counts: getresult=4 refused=0 registered=1 invoked=1 version=4' \
        'value task source handoff end'; do
        gate_output byte-witness "$native" "$line" --count any --policy crlf \
            || { echo "FAIL: ValueTask source handoff witness missing: $line" >&2; exit 1; }
    done
    before="$out/before-value-task-source-rejection.stdout"
    gate_capture_ok "${before%.stdout}" run_bounded dotnet "$_CG_APP" before-value-task-source-rejection
    gate_output prefix "$native" "$before" --before '== value task source rejected registration ==' --policy crlf
    for line in '== value task source rejected registration ==' \
        'op5 orphan=InvalidOperationException:stale token' \
        'op6 armed=True continuation=returned joined=True value=46' \
        'op7 status=RanToCompletion result=ok:47' \
        'op8 completed=InvalidOperationException:stale token' \
        'rejection counts: getresult=4 refused=0 registered=4 version=4' \
        'value task source rejected registration end'; do
        gate_output byte-witness "$native" "$line" --count any --policy crlf \
            || { echo "FAIL: ValueTask rejected registration witness missing: $line" >&2; exit 1; }
    done
    before="$out/before-registration-rejection.stdout"
    gate_capture_ok "${before%.stdout}" run_bounded dotnet "$_CG_APP" before-registration-rejection
    gate_output prefix "$native" "$before" --before '== continuation registration rejection ==' --policy crlf
    for line in '== continuation registration rejection ==' \
        'OnCompleted InvalidOperationException:source rejected the continuation' \
        'UnsafeOnCompleted InvalidOperationException:source rejected the continuation' \
        'configured OnCompleted InvalidOperationException:source rejected the continuation' \
        'non-generic OnCompleted InvalidOperationException:source rejected the continuation' \
        'AsTask InvalidOperationException:source rejected the continuation' \
        'registrations=5' \
        'continuation registration rejection end'; do
        gate_output byte-witness "$native" "$line" --count any --policy crlf \
            || { echo "FAIL: registration rejection witness missing: $line" >&2; exit 1; }
    done
    before="$out/before-enum-whenall.stdout"
    gate_capture_ok "${before%.stdout}" run_bounded dotnet "$_CG_APP" before-enum-whenall
    gate_output prefix "$native" "$before" --before '== enum WhenAll results ==' --policy crlf
    for line in '== enum WhenAll results ==' \
        'enum byte values: 255,1,128' 'enum sbyte values: -128,127,-1' \
        'enum short values: -32768,32767,-1' 'enum ushort values: 65535,1,32768' \
        'enum int values: -2147483648,2147483647,-1' \
        'enum uint values: 4294967295,1,2147483648' \
        'enum long values: -9223372036854775808,9223372036854775807,-1' \
        'enum ulong values: 18446744073709551615,1,9223372036854775808' \
        'enum WhenAll results end'; do
        gate_output byte-witness "$native" "$line" --count any --policy crlf \
            || { echo "FAIL: enum WhenAll result witness missing: $line" >&2; exit 1; }
    done
    for line in byte sbyte short ushort int uint long ulong; do
        gate_output byte-witness "$native" "enum $line: empty=True/0/True settled=True/True yielded=True enumerable=True pending=False/False/True/True" --count any --policy crlf \
            || { echo "FAIL: enum WhenAll completion/type/value witness missing: $line" >&2; exit 1; }
    done
    local mode child child_code dotnet_child dotnet_code rejection
    for mode in value-task-source:'source OnCompleted #6' \
        custom-awaiter:'awaiter UnsafeOnCompleted' async-void:'source OnCompleted #6'; do
        child="$out/suspension-${mode%%:*}.stdout"
        dotnet_child="$out/suspension-oracle-${mode%%:*}.stdout"
        gate_capture "${child%.stdout}" run_bounded "./$out/AsyncCombinators$EXE_EXT" \
            suspension-rejection "${mode%%:*}"
        gate_capture "${dotnet_child%.stdout}" run_bounded dotnet "$_CG_APP" \
            suspension-rejection "${mode%%:*}"
        child_code=$(cat "${child%.stdout}.status")
        dotnet_code=$(cat "${dotnet_child%.stdout}.status")
        case "$dotnet_code" in
            0|1) echo "FAIL: real .NET exited $dotnet_code after a rejected suspension; it must abort" >&2
                return 1 ;;
        esac
        gate_output compare "$child" "$dotnet_child"
        # The default run is unchanged; only the rejected registration follows.
        gate_output prefix "$child" "$native" \
            --before "== builder suspension rejection: ${mode%%:*} ==" --policy crlf
        printf '%s\n' "${mode#*:}" > "$out/suspension.expected"
        gate_output suffix "$child" "$out/suspension.expected" \
            --after "== builder suspension rejection: ${mode%%:*} ==" --policy crlf
        case "${mode%%:*}" in
            custom-awaiter) rejection='awaiter rejected the continuation' ;;
            *) rejection='source rejected the continuation' ;;
        esac
        gate_output abort-parity "${child%.stdout}" "${dotnet_child%.stdout}" \
            --host "$DN2CPP_OS" --witness "${mode#*:}" \
            --diagnostic "Unhandled exception. System.InvalidOperationException: $rejection" || return 1
        echo "OK suspension rejection ${mode%%:*}: aborted with $child_code after '${mode#*:}'"
    done
    before="$out/before-cancellation-receivers.stdout"
    gate_capture_ok "${before%.stdout}" run_bounded dotnet "$_CG_APP" before-cancellation-receivers
    gate_output prefix "$native" "$before" --before '== cancellation source receivers ==' --policy crlf
    for line in '== cancellation source receivers ==' \
        'Token=NullReferenceException' 'IsCancellationRequested=NullReferenceException' \
        'Cancel=NullReferenceException' 'Cancel(false)=NullReferenceException' \
        'Cancel(true)=NullReferenceException' 'Dispose=NullReferenceException' \
        'live before=False/False' 'live after=True/True' \
        'live Cancel(false)=True/True' 'live Cancel(true)=True/True' \
        'cancellation source receivers end'; do
        gate_output byte-witness "$native" "$line" --count any --policy crlf \
            || { echo "FAIL: cancellation receiver witness missing: $line" >&2; exit 1; }
    done
    before="$out/before-task-lifecycle.stdout"
    gate_capture_ok "${before%.stdout}" run_bounded dotnet "$_CG_APP" before-task-lifecycle
    async_lifecycle_prefix "$native" "$before" "$out/before-task-lifecycle.prefix"
    for line in '== sequence nulls ==' \
        'sequence nulls end' \
        '== cold task scheduler ==' \
        'scheduler: True,True,True' \
        'cold task scheduler end' \
        '== task origin ==' \
        'singleton identity/type: True,True' \
        'singleton cold body: 1' \
        'task origin end'; do
        gate_output byte-witness "$native" "$line" --count any --policy crlf \
            || { echo "FAIL: AsyncCombinators lifecycle witness missing: $line" >&2; exit 1; }
    done
    before="$out/before-task-validation.stdout"
    gate_capture_ok "${before%.stdout}" run_bounded dotnet "$_CG_APP" before-task-validation
    gate_output prefix "$native" "$before" --before '== delay arguments ==' --policy crlf
    for line in '== delay arguments ==' \
        'delay arguments end' \
        '== task receivers ==' \
        'sources still pending: WaitingForActivation WaitingForActivation' \
        'task receivers end'; do
        gate_output byte-witness "$native" "$line" --count any --policy crlf \
            || { echo "FAIL: AsyncCombinators validation witness missing: $line" >&2; exit 1; }
    done
    local oracle="$out/direct-call-oracle" fixture="$out/direct-call" expected actual
    mkdir -p "$oracle"
    cp "$call_app" "$oracle/TaskCallValidation.dll"
    cp "${call_app%.dll}.runtimeconfig.json" "${call_app%.dll}.deps.json" "$oracle/"
    $task_python gates/fixtures/task-call-validation/patch-call.py "$oracle/TaskCallValidation.dll"
    invoke_cli "$oracle/TaskCallValidation.dll" -r "$_CG_CORELIB" --auto-ref -o "$fixture"
    compile_console "$fixture" TaskCallValidation
    expected="$fixture/oracle.stdout"
    actual="$fixture/native.stdout"
    gate_capture_ok "$fixture/oracle" run_bounded dotnet "$oracle/TaskCallValidation.dll"
    gate_capture_ok "$fixture/native" run_bounded "./$fixture/TaskCallValidation$EXE_EXT"
    gate_output compare "$actual" "$expected"
    for line in 'direct-get-awaiter-null|ok:constructed' \
        'direct-configure-null|ok:constructed' \
        'task direct-call validation end' \
        '== task direct scheduling ==' \
        'task direct scheduling end'; do
        gate_output byte-witness "$actual" "$line" --count any --policy crlf \
            || { echo "FAIL: Task direct-call witness missing: $line" >&2; exit 1; }
    done
}

corelib_diff_gate AsyncCombinators

(
    unset -f gate_extra_asserts
    deadline_out="$(_corelib_gate_out AsyncCombinators)${DN2CPP_OUT_SUFFIX:-}-native-deadlines"
    _corelib_gate_out() { printf '%s\n' "$deadline_out"; }
    DN2CPP_GATE_RUN_ARGS=native-delay-deadlines \
        corelib_freeze_gate AsyncCombinators "$(dirname "$0")/expected/async-delay-deadlines.txt"
)

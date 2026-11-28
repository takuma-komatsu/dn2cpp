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
# .NET's default message. A timed blocking wait can expire on a pending promise
# or before a later delay, and succeeds if the delay is already due or falls
# within its timeout. A yielded continuation can wait for the caller's signal.
# A delay can expire while its creating thread waits for the caller to release it.
# An earlier timeout on the waiting thread still wins when both timers are overdue.
# The live oracle waits for timer callbacks before polling. The native deadline
# mode leaves timers overdue until the poll, checking its WhenAny winner and WhenAll.
# Context-free configured awaits, default-scheduler continuations and yields
# under the base SynchronizationContext can complete during a finite wait.
# Yield and pending Task await continuations post to a derived context's pump.
# Configured Task awaiters preserve options through direct use and state machines.
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
# Task sequences, cold scheduling, protected starts, and CoreLib-derived promise task initialization.
# Default scheduler inline execution distinguishes an unassigned task, a missing
# queue entry, and an owned delegate that can run exactly once.
# Directly queueing a cold task dispatches its body off the calling thread.
# A pool scheduler and a reflected constructor expose their concrete CoreLib
# types. Tasks assigned to a separately constructed pool observe that scheduler.
# A managed scheduler runs explicit factory tasks and pending continuations through
# QueueTask and receives a synchronous task after declining its inline attempt.
# Token cancellation settles an eager continuation immediately and a lazy one
# when its antecedent finishes. Both delegates stay uncalled.
# Stored Task faults remain pending until the body finishes and keep their order and identity.
# Task and cancellation aggregate construction leaves virtual inner Messages unread.
# RuntimeAsyncSubset.cs asserts runtime-async method bodies (MethodImplAttributes.Async,
# opted in per method): AsyncHelpers.Await over Task and ValueTask, UnsafeAwaitAwaiter
# over Task.Yield, a value-type instance method, a generic method, awaits inside
# try regions, and configured awaits that resume without the captured context
# during a timed wait.
source "$(dirname "$0")/_common.sh"
gate_python -c 'pass' >/dev/null || gate_skip "no working Python 3 interpreter for task call fixtures"
call_app="gates/fixtures/task-call-validation/bin/$CONFIG/$TFM/TaskCallValidation.dll"
build_gate_proj gates/fixtures/task-call-validation/TaskCallValidation.csproj
deadline_snapshot="$(dirname "$0")/expected/async-delay-deadlines.txt"
DN2CPP_GATE_EXTRA_INPUTS="${DN2CPP_GATE_EXTRA_INPUTS:-} $call_app ${call_app%.dll}.runtimeconfig.json ${call_app%.dll}.deps.json gates/fixtures/task-call-validation/patch-call.py $deadline_snapshot"
DN2CPP_GATE_EXTRA_CONTEXT="${DN2CPP_GATE_EXTRA_CONTEXT:-}|task-call-validation|cli:$(_gate_cli_hash)"
DN2CPP_GATE_EXTRA_CONTEXT="$DN2CPP_GATE_EXTRA_CONTEXT|prefix:before-pending-task-joins"
DN2CPP_GATE_EXTRA_CONTEXT="$DN2CPP_GATE_EXTRA_CONTEXT|prefix:before-cancellation-callback-faults"
DN2CPP_GATE_EXTRA_CONTEXT="$DN2CPP_GATE_EXTRA_CONTEXT|prefix:before-lazy-aggregate-message"
DN2CPP_GATE_EXTRA_CONTEXT="$DN2CPP_GATE_EXTRA_CONTEXT|prefix:before-delay-synchronization"
DN2CPP_GATE_EXTRA_CONTEXT="$DN2CPP_GATE_EXTRA_CONTEXT|prefix:before-derived-task-promises|prefix:before-timed-waitasync|prefix:before-semaphore-waiters|prefix:before-task-creation-options|prefix:before-task-cancellation-tokens|prefix:before-protected-task-start"
DN2CPP_GATE_EXTRA_CONTEXT="$DN2CPP_GATE_EXTRA_CONTEXT|prefix:before-scheduler-inline"
DN2CPP_GATE_EXTRA_CONTEXT="$DN2CPP_GATE_EXTRA_CONTEXT|prefix:before-scheduler-queue"
DN2CPP_GATE_EXTRA_CONTEXT="$DN2CPP_GATE_EXTRA_CONTEXT|prefix:before-task-add-exception"
DN2CPP_GATE_EXTRA_CONTEXT="$DN2CPP_GATE_EXTRA_CONTEXT|prefix:before-task-finish|prefix:before-scheduler-ids|prefix:before-synchronization-context|prefix:before-configured-options"
DN2CPP_GATE_EXTRA_CONTEXT="$DN2CPP_GATE_EXTRA_CONTEXT|prefix:before-runtime-async|prefix:before-pool-scheduler|prefix:before-managed-scheduler"
DN2CPP_GATE_EXTRA_CONTEXT="$DN2CPP_GATE_EXTRA_CONTEXT|shared-binary:native-delay-deadlines|shared-binary:fresh-cancellation-filters"

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
    # Only argv differs; keep the frozen run inside the aggregate's cache receipt.
    gate_capture_ok "$out/native-deadlines" run_bounded "./$out/AsyncCombinators$EXE_EXT" native-delay-deadlines
    gate_output compare "$out/native-deadlines.stdout" "$deadline_snapshot" --policy crlf
    local mode
    for mode in fresh-cancellation-filter fresh-cancellation-filter-generic; do
        gate_capture_ok "$out/$mode-oracle" run_bounded dotnet "$_CG_APP" "$mode"
        gate_capture_ok "$out/$mode-native" run_bounded "./$out/AsyncCombinators$EXE_EXT" "$mode"
        gate_output compare "$out/$mode-native.stdout" "$out/$mode-oracle.stdout" --policy crlf || return 1
        gate_output byte-witness "$out/$mode-native.stdout" 'fresh filter: True' --policy crlf || return 1
    done
    native="$out/native.stdout"
    before="$out/before-managed-scheduler.stdout"
    gate_capture_ok "${before%.stdout}" run_bounded dotnet "$_CG_APP" before-managed-scheduler || return 1
    gate_output prefix "$native" "$before" --before '== managed scheduler dispatch ==' --policy crlf || return 1
    local queued='complete=True queue=1 inline=0 calls=1 current=True'
    printf '%s\n' "StartNew(Action): $queued" "StartNew(Func): $queued result=17" \
        'continuations pending=True' \
        "ContinueWith(Action): $queued" "ContinueWith(Func): $queued result=42" \
        'RunSynchronously inline first=True: complete=True queue=1 inline=1 calls=1 current=True' \
        'continuations before antecedent: eager=Canceled lazy=WaitingForActivation' \
        'continuations after antecedent: eager=Canceled lazy=Canceled callbacks=0' > "$out/managed-scheduler-and-cancellation.expected"
    gate_output suffix "$native" "$out/managed-scheduler-and-cancellation.expected" \
        --after '== managed scheduler dispatch ==' --policy crlf || return 1
    before="$out/before-pool-scheduler.stdout"
    gate_capture_ok "${before%.stdout}" run_bounded dotnet "$_CG_APP" before-pool-scheduler
    gate_output prefix "$native" "$before" --before '== pool scheduler and constructor types ==' --policy crlf || return 1
    gate_output byte-witness "$native" \
        'System.Threading.Tasks.ThreadPoolTaskScheduler abstract=True/False corelib=True app=True' \
        'System.Reflection.RuntimeConstructorInfo abstract=True/False corelib=True app=True' \
        'declared members: queue=True constructor=True parameters=True' \
        'separate pool=True start=True factory=True continuation=True' --policy crlf || return 1
    before="$out/before-protected-task-start.stdout"
    gate_capture_ok "${before%.stdout}" run_bounded dotnet "$_CG_APP" before-protected-task-start
    gate_output prefix "$native" "$before" --before '== protected task start ==' --policy crlf
    for line in 'cold claim: True/False/WaitingToRun/0' 'promise claim: True/WaitingToRun' \
        'settled claims: False/True/False/True/False' 'inline delegate: Running/False' \
        'continuation claim: False/Running' 'filtered claims: True/True/Canceled/WaitingToRun/0' \
        'protected task start end'; do
        gate_output byte-witness "$native" "$line" --policy crlf || return 1
    done
    before="$out/before-scheduler-inline.stdout"
    gate_capture_ok "${before%.stdout}" run_bounded dotnet "$_CG_APP" before-scheduler-inline
    gate_output prefix "$native" "$before" --before '== scheduler inline ==' --policy crlf || return 1
    printf '%s\n' 'protected task start end' > "$out/protected-start-end.expected"
    gate_output suffix "$before" "$out/protected-start-end.expected" --policy crlf || return 1
    printf '%s\n' 'scheduler inline: True/False/False/True/False/RanToCompletion/1' \
        'scheduler inline ineligible: False/False|True/False/False|True/False/False' \
        'scheduler inline cancellation: True/Canceled|True/Faulted' > "$out/scheduler-inline.expected"
    before="$out/before-scheduler-queue.stdout"
    gate_capture_ok "${before%.stdout}" run_bounded dotnet "$_CG_APP" before-scheduler-queue
    gate_output suffix "$before" "$out/scheduler-inline.expected" --after '== scheduler inline ==' --policy crlf || return 1
    gate_output prefix "$native" "$before" --before 'scheduler queue: True/True/False/Running/42/1' --policy crlf || return 1
    gate_output byte-witness "$native" 'scheduler queue: True/True/False/Running/42/1' --policy crlf || return 1
    before="$out/before-task-add-exception.stdout"
    gate_capture_ok "${before%.stdout}" run_bounded dotnet "$_CG_APP" before-task-add-exception
    gate_output prefix "$native" "$before" --before 'task add pending: Created/True/False/0' --policy crlf || return 1
    for line in 'task add pending: Created/True/False/0' \
        'task add finish: Faulted/1/1/True/True/True' \
        'task add promise: WaitingForActivation/True/RanToCompletion/73/True' \
        'task add join: True/True/True/True' \
        'task add proxy: True/True'; do
        gate_output byte-witness "$native" "$line" --policy crlf || return 1
    done
    before="$out/before-task-finish.stdout"
    gate_capture_ok "${before%.stdout}" run_bounded dotnet "$_CG_APP" before-task-finish
    gate_output prefix "$native" "$before" --before '== task finish ==' --policy crlf || return 1
    printf '%s\n' 'task finish False: Created/False/False/Faulted/True/True/0/True' \
        'task finish True: Created/False/False/RanToCompletion/True/True/0/True' \
        'task finish struct: True/True/True' > "$out/task-finish.expected"
    before="$out/before-scheduler-ids.stdout"
    gate_capture_ok "${before%.stdout}" run_bounded dotnet "$_CG_APP" before-scheduler-ids
    gate_output suffix "$before" "$out/task-finish.expected" --after '== task finish ==' --policy crlf || return 1
    gate_output prefix "$native" "$before" --before '== scheduler ids ==' --policy crlf || return 1
    printf '%s\n' 'default/current/reflected: True/True/True/True' \
        'read order and independent task counter: True/True/True/True' \
        'shared first read: True/True/True' \
        'wait before delay: False' 'wait through delay: True' \
        'wait pending zero: False' 'wait pending finite: False' \
        'wait after idle: True/True' 'wait before signal: False/True' \
        'timed wait contexts: configured=True continued=True base yield=True posts=0' \
        'parked owner delay: completed=True owner released=True' \
        'overdue WhenAll poll: True' \
        'parked owner race: completed=True earlier timeout=True' > "$out/scheduler-ids-and-timed-waits.expected"
    before="$out/before-synchronization-context.stdout"
    gate_capture_ok "${before%.stdout}" run_bounded dotnet "$_CG_APP" before-synchronization-context || return 1
    gate_output select "$native" "$out/before-synchronization-context.native.stdout" \
        --before-prefix 'context yield: ' --policy crlf || return 1
    gate_output compare "$out/before-synchronization-context.native.stdout" "$before" --policy crlf || return 1
    gate_output suffix "$before" "$out/scheduler-ids-and-timed-waits.expected" --after '== scheduler ids ==' --policy crlf || return 1
    gate_output byte-witness "$native" 'context yield: True/True/True' \
        'context task: True/True/True/True' 'context post nested join: True/True' \
        'context post source joined: True' --policy crlf || return 1
    before="$out/before-configured-options.stdout"
    gate_capture_ok "${before%.stdout}" run_bounded dotnet "$_CG_APP" before-configured-options || return 1
    gate_output select "$native" "$out/before-configured-options.native.stdout" \
        --before-prefix 'configured options ready: ' --policy crlf || return 1
    gate_output compare "$out/before-configured-options.native.stdout" "$before" --policy crlf || return 1
    gate_output byte-witness "$native" 'configured options ready: True/False/False/17' \
        'configured options result: ok/ok/InvalidOperationException/TaskCanceledException' \
        'configured options state: True/True/True/True' 'configured options callbacks: True/True/True' \
        'configured options after thread exit: True/True' \
        --policy crlf || return 1
    before="$out/before-runtime-async.stdout"
    gate_capture_ok "${before%.stdout}" run_bounded dotnet "$_CG_APP" before-runtime-async
    gate_output prefix "$native" "$before" --before '== runtime async ==' --policy crlf || return 1
    for line in '== runtime async ==' 'tasks: 21/40 settled: True/2' 'struct: 5/1' \
        'try: False/45' 'runtime async end' 'runtime async configured contexts: task=True value task=True' \
        'runtime async force after thread exit: True' 'runtime async task Post: True/True' \
        'scheduler dequeue: False/True/False closed-null=False' \
        'custom Start: handoff=True canceled=True faulted=True calls=0' \
        'custom Start: rejected after cancellation=True'; do
        gate_output byte-witness "$native" "$line" --policy crlf || return 1
    done
    # The block's methods are lowered as resumable bodies, not as state machines.
    if ! grep -qE 'struct Program_AwaitTasks_m[0-9]+__AsyncFrame' "$out"/generated*.cpp; then
        echo 'FAIL: RuntimeAsyncSubset.Program.AwaitTasks is not a runtime-async body' >&2
        return 1
    fi
    before="$out/before-semaphore-waiters.stdout"
    gate_capture_ok "${before%.stdout}" run_bounded dotnet "$_CG_APP" before-semaphore-waiters
    gate_output prefix "$native" "$before" --before '== semaphore waiter helpers ==' --policy crlf
    for line in 'queue remove: True/True/False' 'queue links cleared: True/True/True/True' \
        'queue release: 0/True/0/True/True/True' 'released removal: False' \
        'queue FIFO: True/True' 'null waiter: NullReferenceException' 'timeout helper removal: True' \
        'cancel helper removal: True/True' 'remove/release linearization: True' \
        'native queue root: True' 'abandoned queue collected: True' 'semaphore waiter helpers end'; do
        gate_output byte-witness "$native" "$line" --policy crlf || return 1
    done
    before="$out/before-timed-waitasync.stdout"
    gate_capture_ok "${before%.stdout}" run_bounded dotnet "$_CG_APP" before-timed-waitasync
    gate_output prefix "$native" "$before" --before '== timed WaitAsync ==' --policy crlf
    gate_output byte-witness "$native" 'timed WaitAsync end' --policy crlf
    gate_output byte-witness "$native" 'timeout cleanup: True/True' --policy crlf
    before="$out/before-derived-task-promises.stdout"
    gate_capture_ok "${before%.stdout}" run_bounded dotnet "$_CG_APP" before-derived-task-promises
    gate_output prefix "$native" "$before" --before '== derived task promises ==' --policy crlf
    for line in '== derived task promises ==' \
        'promise pending: WaitingForActivation,False,False' \
        'promise identity: True,True,True' 'promise state: True' \
        'promise completion: True,False,True,RanToCompletion,True' \
        'promise fields: True,True' 'promise await inline: False' \
        'derived task promises end'; do
        gate_output byte-witness "$native" "$line" --policy crlf \
            || { echo "FAIL: derived task promise witness missing: $line" >&2; return 1; }
    done
    before="$out/before-delay-synchronization.stdout"
    gate_capture_ok "${before%.stdout}" run_bounded dotnet "$_CG_APP" before-delay-synchronization
    gate_output prefix "$native" "$before" --before '== delay synchronization ==' --policy crlf
    gate_output byte-witness "$native" '== delay synchronization ==' --policy crlf \
        || { echo 'FAIL: delay synchronization block did not run once' >&2; return 1; }
    before="$out/before-task-creation-options.stdout"
    gate_capture_ok "${before%.stdout}" run_bounded dotnet "$_CG_APP" before-task-creation-options
    gate_output prefix "$native" "$before" --before '== task creation options ==' --policy crlf
    for line in '== task creation options ==' 'source pending: 64' \
        'run: 8' 'run unwrap: 0' 'factory: 9' 'run nested: 8' \
        'run nested identity: True' 'run nested worker: 31' \
        'wait pending identity: True' 'wait completed identity: True' \
        'semaphore cancelable: 0' 'delay canceled: 64' \
        'delay completed then canceled: 0' 'options null: True'; do
        gate_output byte-witness "$native" "$line" --policy crlf || return 1
    done
    before="$out/before-task-cancellation-tokens.stdout"
    gate_capture_ok "${before%.stdout}" run_bounded dotnet "$_CG_APP" before-task-cancellation-tokens
    gate_output prefix "$native" "$before" --before '== task cancellation tokens ==' --policy crlf
    for line in '== task cancellation tokens ==' 'cold token: True' \
        'promise losing token: True' 'join completion token: True' \
        'join input token: True' 'unwrap inner token: True' \
        'synthetic cancellation identity: True' 'synthetic transfer identity: True' \
        'captured OCE identity: True' 'captured TCE identity: True' \
        'pre-canceled tasks: True' 'pre-canceled tokens: True' 'pre-canceled start: True'; do
        gate_output byte-witness "$native" "$line" --policy crlf || return 1
    done
    printf '%s\n' 'task cancellation tokens end' > "$out/promise-end.expected"
    gate_output suffix "$out/before-protected-task-start.stdout" "$out/promise-end.expected" --policy crlf \
        || { echo 'FAIL: task cancellation tokens block did not complete before protected starts' >&2; return 1; }
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
        # The default run up to the runtime-async block is unchanged; only the
        # rejected registration follows.
        gate_output prefix "$child" "$out/before-runtime-async.stdout" \
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
    gate_python gates/fixtures/task-call-validation/patch-call.py "$oracle/TaskCallValidation.dll"
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

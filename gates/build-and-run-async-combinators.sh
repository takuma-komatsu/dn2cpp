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
# Former gates: whenall, whenany, when-enumerable, configure-await, delay-order,
# cancellation, custom-awaitable, multi-awaiter.
source "$(dirname "$0")/_common.sh"

corelib_diff_gate AsyncCombinators

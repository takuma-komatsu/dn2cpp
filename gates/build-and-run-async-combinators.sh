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
# Former gates: whenall, whenany, when-enumerable, configure-await, delay-order,
# cancellation, custom-awaitable, multi-awaiter.
source "$(dirname "$0")/_common.sh"

corelib_diff_gate AsyncCombinators

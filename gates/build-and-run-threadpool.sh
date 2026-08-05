#!/usr/bin/env bash
# ThreadPool.QueueUserWorkItem — fire-and-forget work on the worker pool. Queues many
# WaitCallback items (1-arg null-state and 2-arg boxed-state overloads), each signaling a
# CountdownEvent; the main thread Wait()s on the countdown before reading the accumulated
# totals, so the output is fully deterministic and diffed exact vs real .NET. The boxed
# state is reachable only through the queued item and the queuing loop allocates throwaway
# garbage to provoke a GC mid-queue, so this also exercises that the runtime keeps the
# queued delegate + state GC-reachable until the worker runs them.
#
# WaitCallback and CountdownEvent live in the System.Threading assembly (not CoreLib),
# so it is referenced alongside CoreLib.
#
# The UserThreadTask section covers the other kind of pool a program can have: a
# hand-rolled one, whose worker threads are the program's own and whose queue the runtime
# cannot see (Thrive's TaskExecutor shape — cold `new Task(...)` handed to `new Thread`
# workers that run them with RunSynchronously(), submitters blocking in Wait()/.Result).
# A live user thread has to count as a principal that can still settle a task, or a
# blocked wait reads the empty in-flight count as proof of deadlock; that section, plus
# Task.WaitAll/WaitAny over the same hand-off, is diffed exact vs real .NET. Its
# unsatisfiable counterpart is the frozen ColdTaskDeadlock section of the async-core gate
# (real .NET hangs on it, so it cannot be diffed).
#
# The FfSettler section covers the two settlers that are neither a pool Task nor a live
# user thread, and so used to read as an empty principal set: a fire-and-forget pool item
# that completes a TaskCompletionSource the main thread is blocked on, and the CancelAfter
# timer thread running a registered cancel callback. Every settling item sleeps before it
# settles, so the waiter reaches the drain with the item still outstanding — that window is
# where the false deadlock verdict lived, and a fast worker would hide it. That the report
# RE-ARMS once such a settler leaves cannot be diffed here either; it is the
# afterff/aftertimer pair of the same frozen ColdTaskDeadlock section.
#
# The TimerSettler section covers the third settler of that class: a
# System.Threading.Timer callback, whose count follows the timer's ARMED state — a
# pending due time or a callback in flight, with Change and Dispose as principal
# transitions — never the timer thread's lifetime. The disarm half (an undisposed idle
# timer must NOT disarm the report) is the timeridle/timerspent/timerdisarm/ontimer
# lines of the same frozen ColdTaskDeadlock section.
#
# The same section carries two more shapes, both diffed exact vs real .NET. Its
# timers are disposed through BOTH routes — `using` / an IDisposable-typed local (the
# callvirt IDisposable::Dispose dispatch, which the intrinsic timer type-info answers
# only because the init prologue installs an IDisposable row on it) and the direct
# Dispose() call (the intrinsic call-site route) — so a regression of either is red
# here; the interface route is a runtime fatal without that row. And the last two shapes
# are a Change issued while the callback is IN FLIGHT: a one-shot re-arming itself,
# and a periodic one Changing to fire once more and stop. Both are deterministic
# (every value is printed only after the blocking wait the timer settles). Losing the
# mid-callback Change again does not misprint and does not hang either: the timer
# disarms, the settler set empties under the blocked waiter, and the run dies on the
# defeated-wait verdict ("no armed timer") — verified by reverting the fix.
source "$(dirname "$0")/_common.sh"
corelib_diff_gate ThreadPoolQueue System.Threading

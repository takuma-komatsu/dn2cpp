#!/usr/bin/env bash
# Consolidated async-core gate: async/await state machine over synchronously-
# completed awaits, Task.Yield suspension, exceptions thrown across awaits,
# ValueTask, a struct-returning async Task, task state, a suspended state machine
# rooted only by per-thread scheduler state, and a continuation handed to the
# scheduler of a thread that has already exited. One multi-section program
# transpiled once against the tree-shaken real CoreLib.
#
# Uses a frozen snapshot (gates/expected/async-core.txt) rather than a live
# `dotnet $app` diff, for three intentional divergences: the async-suspend section
# produces a faulted task that real .NET surfaces as an unhandled
# AggregateException at shutdown, whereas the transpiler's async runtime does not
# propagate it that way; the thread-await-teardown section's post-await tail
# resumes on the pool in real .NET but is never run here; and the
# cold-task-deadlock section waits on tasks no principal can ever settle, which
# real .NET answers by blocking forever (it has no deadlock detector) — there is
# no oracle to diff a hang against, so the snapshot pins dn2cpp's catchable
# report instead; and the task-scheduler-sync-context section's installed-context
# arm throws NotSupportedException because dn2cpp cannot wrap it as a scheduler,
# where real .NET creates one. Its null-context InvalidOperationException arm and
# every other section match real .NET. The snapshot is deterministic native output.
# Former gates: async, async-suspend, async-yield, async-catch, value-task,
# struct-task, task-state.
source "$(dirname "$0")/_common.sh"

corelib_freeze_gate AsyncCore "$(dirname "$0")/expected/async-core.txt"

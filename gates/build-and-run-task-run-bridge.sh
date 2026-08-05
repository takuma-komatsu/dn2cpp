#!/usr/bin/env bash
# Task.Run on the real worker pool, bridged to the cooperative async scheduler.
# `await Task.Run(...)` suspends the main thread; a worker runs the delegate, completes
# the Task, and the bridge resumes the continuation on the awaiting thread. Covers
# Func<int>/Func<string>/Action, parallel Task.Run + WhenAll, and a Task.Delay mix. All
# results are awaited so the output is deterministic and diffed exact vs real .NET.
source "$(dirname "$0")/_common.sh"
corelib_diff_gate TaskRunBridge

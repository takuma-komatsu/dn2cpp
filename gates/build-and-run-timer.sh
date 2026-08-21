#!/usr/bin/env bash
# System.Threading.Timer — a per-timer OS thread that waits dueTime, fires
# TimerCallback(state), and (for a finite period > 0) re-fires periodically. Covers the
# one-shot, periodic, idle-then-Change, TimeSpan, long ctor/Change overloads, the system
# TimeProvider's ITimer adapter. Timer
# firing is timing-based, so the program asserts only deterministic facts (a latch gates
# main until the expected fires happened; it prints the one-shot fire count, the
# threaded-through state, and "count >= N" as a bool — never the timing-dependent exact
# count), so the output is byte-identical to real .NET and diffed exact. Timer/Timeout
# live in System.Threading.dll.
source "$(dirname "$0")/_common.sh"
corelib_diff_gate TimerSample System.Threading

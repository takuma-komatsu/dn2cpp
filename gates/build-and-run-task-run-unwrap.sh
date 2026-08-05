#!/usr/bin/env bash
# Task.Run(Func<Task>) / Task.Run(Func<Task<T>>) — the async-lambda unwrap. The delegate
# returns an INNER Task; the outer Task completes with the inner's result/fault/cancellation
# (unwrap), exactly like real .NET (the outer is Task / Task<T>, never Task<Task>). A worker
# runs the delegate and drains its own cooperative scheduler until the inner task settles,
# then settles the outer; an awaiting thread sleeps via the cross-thread bridge until then.
# Sections: typed results (int/long/double/float/ref), a void Func<Task> side effect, inner
# faults re-raised through the outer await, a nested Task.Run, and the existing non-unwrap
# Task.Run(Func<int>)/Task.Run(Action) cohabiting to prove no regression. Every Task is
# awaited/joined, so the output is deterministic and diffed exact vs real .NET.
source "$(dirname "$0")/_common.sh"

corelib_diff_gate TaskRunUnwrap

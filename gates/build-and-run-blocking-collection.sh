#!/usr/bin/env bash
# BlockingCollection<T> — a producer/consumer blocking queue on real threads. Sections:
# multi-producer/multi-consumer with CompleteAdding + blocking Take draining to an
# InvalidOperationException exit (totals match); single-thread FIFO order; CompleteAdding
# then drain + Take-throws / TryTake-false on completed-empty; bounded capacity (the
# capacity-2 queue blocks a 6-item producer until the consumer drains); TryTake/TryAdd
# timeouts; and reference (string) + 64-bit (long) + double element kinds. Every result
# is read after Join, so the output is deterministic and diffed exact vs real .NET.
# BlockingCollection<T> lives in System.Collections.Concurrent (not CoreLib), so that
# assembly is referenced alongside CoreLib.
source "$(dirname "$0")/_common.sh"
corelib_diff_gate BlockingCollectionSubset System.Collections.Concurrent

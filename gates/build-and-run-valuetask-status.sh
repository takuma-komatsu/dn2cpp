#!/usr/bin/env bash
# ValueTask / ValueTask<T> status properties over the terminal states: IsCompleted,
# IsCompletedSuccessfully, IsFaulted and IsCanceled read off the vtask status word.
# IsFaulted/IsCanceled are the ones a WhenAll-style combinator inspects before
# awaiting; they were unmapped. Diffed against real .NET.
source "$(dirname "$0")/_common.sh"

corelib_diff_gate ValueTaskStatus

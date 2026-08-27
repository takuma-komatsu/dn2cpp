#!/usr/bin/env bash
# Generic Enum.Parse<T> / Enum.TryParse<T> over ReadOnlySpan<char>: the span source
# is realized as a string before the per-enum parse table, instead of being cast
# to one (which the C++ compiler rejects). Names, numeric tokens, case folding,
# [Flags] lists, a 64-bit underlying enum, misses and a sliced span, diffed
# against real .NET.
source "$(dirname "$0")/_common.sh"

corelib_diff_gate EnumParseSpan

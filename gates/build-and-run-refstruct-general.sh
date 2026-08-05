#!/usr/bin/env bash
# Consolidated low-level general-model gate: a user-defined ref struct with a C# 11
# ref field (ref store / ref return / write-through), Span<T>/ReadOnlySpan<T> via
# real CoreLib IL (indexer -> ref T, Slice, Length), stackalloc of a struct with
# element field writes, and variable-size stackalloc. Locks in the verified
# low-level surface the span/ref work builds on. Diffed exact vs real .NET.
source "$(dirname "$0")/_common.sh"

corelib_diff_gate RefStructGeneral

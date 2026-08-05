#!/usr/bin/env bash
# Consolidated list/comparer gate: List<T> (core/enumeration), List.Sort
# (default/IComparer/Comparison/string), Comparer<T>.Default and custom comparers.
# Diffed exactly vs real .NET.
# Former gates: list, list-enum, list-sort, list-sort-comparer,
# list-sort-comparison, list-sort-string, comparer-default, custom-comparer.
source "$(dirname "$0")/_common.sh"

corelib_diff_gate ListCollections

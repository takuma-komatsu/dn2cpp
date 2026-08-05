#!/usr/bin/env bash
# Consolidated string-core gate. Merges the former per-feature string instance/
# static method subset gates into one multi-section program, transpiled once
# against the tree-shaken real CoreLib and diffed exactly against real .NET.
# Covers String methods (IndexOf/Contains/Replace/Substring/Trim/case/etc.),
# comparison/StartsWith/EndsWith, padding, Split, Join, string<->span,
# string.CopyTo(Span<char>), misc string constructors (char*, char[],
# ReadOnlySpan<char>), the broad string method surface, building strings from
# chars, and String.Create.
# Also covers string.Join/Concat over a List<T> that arrives as a CALL RESULT or
# a FIELD rather than a local (JoinCallResultSubset), and the
# StringComparer.CurrentCulture -> ordinal interception
# (OrdinalCultureComparerSubset).
# This project's reference set is deliberately NARROW: android-gdext,
# emit-order-stability, ios-sim-console and wasm-console each re-transpile
# StringCore with their own hand-written copy of it, so a fourth `-r` here is a
# fourth edit there and two of those are cross-compiles. The Join-over-a-concrete-
# IEnumerable section that would have needed System.Collections lives in
# DictCollections, which already carries it.
# Former gates: string-method, string-compare, string-pad, string-split,
# string-join, string-span, string-ctor-misc, string-surface, string-from-chars,
# string-create, join-callresult-subset, ordinal-culture-comparer-subset, plus
# the String-as-interface sections (LINQ over a string / CharEnumerator /
# IComparable-family dispatch / Intern), which need System.Linq.
source "$(dirname "$0")/_common.sh"

corelib_diff_gate StringCore System.Linq

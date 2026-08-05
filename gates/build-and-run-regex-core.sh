#!/usr/bin/env bash
# Consolidated Regex gate: the real net10 System.Text.RegularExpressions.dll
# transpiled and run on both engines — the backtracking interpreter and the
# RegexOptions.NonBacktracking symbolic DFA (BDD construction, minterm
# classification, SymbolicRegexMatcher) — match basics, groups/captures,
# classes/quantifiers/anchors/backreferences/lookarounds, IgnoreCase (ASCII +
# CultureInvariant non-ASCII rows), Replace/Split, options (Multiline/
# Singleline/IgnorePatternWhitespace/RightToLeft), Escape/Unescape, timeouts,
# Count/EnumerateMatches, and a NonBacktracking section covering
# IsMatch/Match/Matches/Count, anchors, alternation, IgnoreCase, Replace/Split
# plus the construction-reject parity rows. RegexOptions.Compiled degrades to
# the interpreter (RuntimeFeature.IsDynamicCodeCompiled folds to false, the
# IL2CPP/NativeAOT posture) and its section must produce identical matches to
# real .NET's compiled engine. Also covers the remaining public API surface
# (EnumerateSplits, RegexParseException Error/Offset, CompileToAssembly
# PlatformNotSupportedException parity, Match.Result/Synchronized,
# Capture.ValueSpan, CacheSize, startat/length overloads), [GeneratedRegex]
# source-generator output transpiled from the user assembly, and
# culture-tiered IgnoreCase (RegexCaseBehavior Invariant/NonTurkish/Turkish
# selected via CultureInfo.CurrentCulture). Diffed exactly vs real .NET.
source "$(dirname "$0")/_common.sh"

net10_bcl_diff_gate RegexCore System.Text.RegularExpressions

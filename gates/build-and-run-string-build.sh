#!/usr/bin/env bash
# Consolidated string-build gate. Merges the former string formatting / building
# subset gates into one multi-section program, transpiled once against the
# tree-shaken real CoreLib and diffed exactly against real .NET. Covers
# String.Format (index/alignment/format specifiers, the params-span overloads, and
# the net8+ System.Text.CompositeFormat overload family) and StringBuilder
# (Append/AppendFormat/Insert/Remove/Replace and edit operations).
# Former gates: string-format, stringbuilder, stringbuilder-edit.
source "$(dirname "$0")/_common.sh"

corelib_diff_gate StringBuild

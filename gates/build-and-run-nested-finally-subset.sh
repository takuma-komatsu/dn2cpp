#!/usr/bin/env bash
# Nested finally pipeline: a `leave` (return/goto) that exits several nested
# try/finally regions must run every enclosing finally in turn. Driven by the
# real System.Private.CoreLib (passed with -r) -> cross-assembly resolve +
# tree-shake -> native binary -> run. The three core gates
# (sample/multiassembly/godot) remain the regression set.
source "$(dirname "$0")/_common.sh"

corelib_subset_gate NestedFinallySubset

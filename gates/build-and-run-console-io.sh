#!/usr/bin/env bash
# Consolidated console/formatting gate: Console.Write/WriteLine surface,
# Console.Write composite formatting, Path APIs, and invariant-culture number
# formatting. Diffed exactly vs real .NET. (Scratch-dir gates file/directory/env
# keep dedicated gates — they take a command-line argument.)
# Former gates: console-write, console-format, path, culture.
source "$(dirname "$0")/_common.sh"

corelib_diff_gate ConsoleIo

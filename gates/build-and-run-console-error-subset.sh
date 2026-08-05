#!/usr/bin/env bash
# Console.Error — the stderr TextWriter.
#
# System.Console is an intrinsic type, so `Console.Error` (get_Error) is lowered by the
# transpiler rather than transpiled: it answers with a cached managed
# Dn2Cpp.Runtime.Dn2CppConsoleWriter singleton, a real TextWriter subtype whose overrides
# and whose receiver-keyed fast path both route to the dn2cpp_textwriter_* family, which
# mirror the stdout dn2cpp_console_* helpers byte-for-byte but write to the writer's stream
# (stderr). This closes the last own-code console self-host gap: dn2cpp's own
# TranspileDriver.Run reports errors via `Console.Error.WriteLine(...)`.
#
# This gate runs the CoreLib-full shape, which is the only one that can host the managed
# subtype. The CoreLib-LESS fallback to the header-less runtime writer is a different
# posture and is asserted by the "CoreLib-less Console.Error" section of
# gates/build-and-run-hotupdate-subset.sh.
#
# The sample writes a stdout marker AND a battery of Console.Error overloads. This gate
# captures the program's STDOUT and STDERR *separately* and exact-diffs each against real
# .NET — so a leak between streams, a wrong newline, or a formatting drift all fail loudly.
# CoreLib only (no Linq shim).
source "$(dirname "$0")/_common.sh"

# Stream-separation guard beyond the split diff itself: the Console.Error
# battery must NOT leak onto stdout (every expected stdout line is the STDOUT
# marker; no "E:" diagnostics there). Defined here, run by the wrapper INSIDE
# the cached region, so a warm hit replays a green that includes it.
gate_extra_asserts() {
    if grep -q '^E:' "$1/native.out"; then
        echo "FAIL: stderr content leaked onto stdout" >&2
        return 1
    fi
}
corelib_diff_split_gate ConsoleErrorSubset

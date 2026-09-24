#!/usr/bin/env bash
# An ambiguous default interface slot: two sibling derived interfaces each
# override one base interface method, whether a plain method, a generic method
# or a method of a generic interface. An invoked slot throws .NET's catchable
# AmbiguousImplementationException with its message and HResult, and an unused
# one leaves the conversion intact. C# rejects the shape, so the application
# compiles against one library version and both sides run against the next: a
# version-skewed reference set.
source "$(dirname "$0")/_common.sh"

APP_DIR="samples/dotnet/AmbiguousDefault/bin/$CONFIG/$TFM"

# A skew that did not take leaves both sides calling the first version's
# bodies, and that output diffs green. The generated C++ must also carry the
# unused ambiguous slot, so conversion completed with the slot modelled.
gate_extra_asserts() {
    local out="$1" output label thrown
    output="$(strip_cr_win "$native")"
    grep -Fxq 'plain: left' <<<"$output" \
        || { echo "FAIL: the unambiguous default body did not run" >&2; exit 1; }
    for label in 'pick' 'pick-generic<int>' 'pick-generic<string>' 'take<string>'; do
        thrown="$label: System.Runtime.AmbiguousImplementationException: Could not call method "
        grep -Fq -- "$thrown" <<<"$output" \
            || { echo "FAIL: $label did not throw AmbiguousImplementationException" >&2; exit 1; }
        grep -Fxq -- "$label hresult: 0x8013106A" <<<"$output" \
            || { echo "FAIL: $label lost the AmbiguousImplementationException HResult" >&2; exit 1; }
    done
    [ "$(tail -n 1 <<<"$output")" = 'after: left' ] \
        || { echo "FAIL: the program did not continue after the caught exceptions" >&2; exit 1; }
    grep -Fq "'AmbiguousDefaultLib.IBase.Unused()' on interface 'AmbiguousDefaultLib.IBase' with type 'AmbiguousDefault.Both'" \
        "$out"/generated*.cpp \
        || { echo "FAIL: the unused ambiguous slot was not modelled" >&2; exit 1; }
}

corelib_diff_gate AmbiguousDefault -r "$APP_DIR/AmbiguousDefaultLib.dll"

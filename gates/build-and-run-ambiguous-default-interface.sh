#!/usr/bin/env bash
# An ambiguous default interface slot: two sibling derived interfaces each
# override one base interface method, whether a plain method, a generic method
# or a method of a generic interface. An invoked slot, or a constrained call on a
# struct, throws .NET's catchable AmbiguousImplementationException with its
# message and HResult, an unused one leaves the conversion intact, and a struct's
# unambiguous default runs on its box. The message names a MakeGenericType
# receiver's own instantiation, and overloads whose messages match each throw
# through a stub of their own signature. C# rejects the shape, so the
# application compiles against one library version and both sides run against
# the next: a version-skewed reference set.
source "$(dirname "$0")/_common.sh"

APP_DIR="samples/dotnet/AmbiguousDefault/bin/$CONFIG/$TFM"

# A skew that did not take leaves both sides calling the first version's
# bodies, and that output diffs green. The generated C++ must also carry the
# unused ambiguous slot, so conversion completed with the slot modelled.
gate_extra_asserts() {
    local out="$1" output label thrown find_stub
    output="$(strip_cr_win "$native")"
    grep -Fxq 'plain: left' <<<"$output" \
        || { echo "FAIL: the unambiguous default body did not run" >&2; exit 1; }
    grep -Fxq 'made plain: left' <<<"$output" \
        || { echo "FAIL: the MakeGenericType receiver's unambiguous default body did not run" >&2; exit 1; }
    grep -Fxq 'constrained plain: left' <<<"$output" \
        || { echo "FAIL: the struct's unambiguous default body did not run" >&2; exit 1; }
    for label in 'pick' 'pick-generic<int>' 'pick-generic<string>' 'take<string>' \
        'find(First.Key)' 'find(Second.Key)' 'made pick' 'made pick-generic<int>' \
        'constrained pick'; do
        thrown="$label: System.Runtime.AmbiguousImplementationException: Could not call method "
        grep -Fq -- "$thrown" <<<"$output" \
            || { echo "FAIL: $label did not throw AmbiguousImplementationException" >&2; exit 1; }
        grep -Fxq -- "$label hresult: 0x8013106A" <<<"$output" \
            || { echo "FAIL: $label lost the AmbiguousImplementationException HResult" >&2; exit 1; }
    done
    grep -Fxq 'after: left' <<<"$output" && grep -Fxq 'made after: left' <<<"$output" \
        && [ "$(tail -n 1 <<<"$output")" = 'constrained after: left' ] \
        || { echo "FAIL: the program did not continue after the caught exceptions" >&2; exit 1; }
    grep -Fq "'AmbiguousDefaultLib.IBase.Unused()' on interface 'AmbiguousDefaultLib.IBase' with type 'AmbiguousDefault.Both'" \
        "$out"/generated*.cpp \
        || { echo "FAIL: the unused ambiguous slot was not modelled" >&2; exit 1; }
    # The runtime template's slot stub and generic-virtual case name the
    # receiver; an image-built instantiation would bypass both.
    grep -Fq 'dn2cpp_throw_ambiguous_implementation_for(self, ' "$out"/generated*.cpp \
        && grep -Fq 'dn2cpp_throw_ambiguous_implementation_for(a0, ' "$out"/generated*.cpp \
        || { echo "FAIL: the MakeGenericType receiver was not minted from a runtime template" >&2; exit 1; }
    # Native code throws before a stub returns, but wasm's call_indirect traps
    # on a stub whose signature differs from the slot's.
    find_stub="^\[\[maybe_unused\]\] static int32_t slotambig_[0-9]+\(void\*, void\*\) \{ dn2cpp_throw_ambiguous_implementation\(\"Could not call method 'AmbiguousDefaultLib\.IBase\.Find\(Key\)' on interface 'AmbiguousDefaultLib\.IBase' with type 'AmbiguousDefault\.Both' "
    grep -Eq -- "$find_stub" "$out"/generated*.cpp \
        || { echo "FAIL: the int-returning Find(Key) slot has no stub of its own signature" >&2; exit 1; }
}

corelib_diff_gate AmbiguousDefault -r "$APP_DIR/AmbiguousDefaultLib.dll"

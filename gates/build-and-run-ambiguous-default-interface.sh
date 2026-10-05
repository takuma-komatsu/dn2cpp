#!/usr/bin/env bash
# An ambiguous default interface slot: two sibling derived interfaces each
# override one base interface method, whether a plain method, a generic method
# or a method of a generic interface. Calls and delegate binding throw .NET's
# catchable AmbiguousImplementationException with its message and HResult.
# Constrained static slots reject sibling overrides at the call instruction,
# including concrete IL operands and generic slots without a default body,
# while an unused slot leaves conversion intact. The message names a MakeGenericType
# receiver's own instantiation, and overloads whose messages match each throw
# through a stub of their own signature. C# rejects the shape, so the
# application compiles against one library version and both sides run against
# the next: a version-skewed reference set.
source "$(dirname "$0")/_common.sh"

DN2CPP_GATE_EXTRA_CONTEXT="${DN2CPP_GATE_EXTRA_CONTEXT:-}|ordinary-constrained-prefix:${DN2CPP_BEFORE_ORDINARY_CONSTRAINED_DEFAULT:-}|binding-prefix-argv:before-binding|generic-message-prefix-argv:before-generic-messages|static-call-prefix-argv:before-static-calls"
DN2CPP_GATE_EXTRA_INPUTS="${DN2CPP_GATE_EXTRA_INPUTS:-} gates/fixtures/static-interface-calls/Program.cs gates/fixtures/static-interface-calls/StaticInterfaceCalls.csproj"

APP_DIR="samples/dotnet/AmbiguousDefault/bin/$CONFIG/$TFM"

# A skew that did not take leaves both sides calling the first version's
# bodies, and that output diffs green. The generated C++ must also carry the
# unused ambiguous slot, so conversion completed with the slot modelled.
gate_extra_asserts() {
    local out="$1" output label thrown find_stub before prefix
    output="$(strip_cr_win "$native")"
    grep -Fxq 'plain: left' <<<"$output" \
        || { echo "FAIL: the unambiguous default body did not run" >&2; exit 1; }
    grep -Fxq 'made plain: left' <<<"$output" \
        || { echo "FAIL: the MakeGenericType receiver's unambiguous default body did not run" >&2; exit 1; }
    for label in 'pick' 'pick-generic<int>' 'pick-generic<string>' 'take<string>' \
        'find(First.Key)' 'find(Second.Key)' 'made pick' 'made pick-generic<int>' 'constrained pick'; do
        thrown="$label: System.Runtime.AmbiguousImplementationException: Could not call method "
        grep -Fq -- "$thrown" <<<"$output" \
            || { echo "FAIL: $label did not throw AmbiguousImplementationException" >&2; exit 1; }
        grep -Fxq -- "$label hresult: 0x8013106A" <<<"$output" \
            || { echo "FAIL: $label lost the AmbiguousImplementationException HResult" >&2; exit 1; }
    done
    grep -Fxq 'after: left' <<<"$output" && grep -Fxq 'made after: left' <<<"$output" \
        && grep -Fxq 'constrained after: left' <<<"$output" \
        || { echo "FAIL: the program did not continue after the caught exceptions" >&2; exit 1; }
    before=$(DN2CPP_BEFORE_ORDINARY_CONSTRAINED_DEFAULT=1 dotnet "$_CG_APP")
    before=$(strip_cr_win "$before")
    prefix=$(awk '/^== a constrained call on a struct ==$/ { exit } { print }' <<< "$output")
    assert_output "$prefix" "$before"
    grep -Fxq 'constrained plain: left' <<< "$output" \
        || { echo "FAIL: the constrained unambiguous default body did not run" >&2; exit 1; }
    grep -Fxq '== virtual delegate creation ==' <<< "$output" \
        || { echo "FAIL: the delegate binding block did not run" >&2; exit 1; }
    for label in 'unused group' 'generic<int> group' 'generic<string> group' \
        'generic receiver group' 'made unused group' 'made generic group'; do
        grep -Fxq "$label created: False" <<< "$output" \
            && grep -Fxq "$label hresult: 0x8013106A" <<< "$output" \
            || { echo "FAIL: $label did not throw before creation completed" >&2; exit 1; }
    done
    for label in 'null interface group' 'null generic interface group' 'null generic class group'; do
        grep -Fxq "$label created: False" <<< "$output" \
            && grep -Fxq "$label hresult: 0x80004003" <<< "$output" \
            || { echo "FAIL: $label did not reject its null receiver at binding" >&2; exit 1; }
    done
    grep -Fxq 'callable groups: left,left,Int32' <<< "$output" \
        || { echo "FAIL: unambiguous delegates did not remain callable" >&2; exit 1; }
    before=$(dotnet "$_CG_APP" before-binding)
    before=$(strip_cr_win "$before")
    prefix=$(awk '/^== virtual delegate creation ==$/ { exit } { print }' <<< "$output")
    assert_output "$prefix" "$before"
    grep -Fxq '== generic interface ambiguity messages ==' <<< "$output" \
        || { echo "FAIL: the generic interface message block did not run" >&2; exit 1; }
    for label in 'box ref/value' 'box value/ref' 'box pair' 'duo mixed single' \
        'duo values single' 'duo pair' 'duo triple' 'box pair group' 'duo single group'; do
        grep -Fxq "$label hresult: 0x8013106A" <<< "$output" \
            || { echo "FAIL: $label did not report its ambiguity" >&2; exit 1; }
    done
    for method in 'IBox`1[U].Select' 'IBox`1[X,Y].Pair' \
        'IDuo`2[V].Single' 'IDuo`2[X,Y].Pair' 'IDuo`2[X,Y,Z].Triple'; do
        grep -Fq "$method" <<< "$output" \
            || { echo "FAIL: $method lost the method definition's parameter names" >&2; exit 1; }
    done
    grep -Fxq 'box pair group created: False' <<< "$output" \
        && grep -Fxq 'duo single group created: False' <<< "$output" \
        || { echo "FAIL: generic interface ambiguity was delayed until delegate invocation" >&2; exit 1; }
    before=$(dotnet "$_CG_APP" before-generic-messages)
    before=$(strip_cr_win "$before")
    prefix=$(awk '/^== generic interface ambiguity messages ==$/ { exit } { print }' <<< "$output")
    assert_output "$prefix" "$before"
    grep -Fxq '== constrained static interface calls ==' <<< "$output" \
        && grep -Fxq 'static interface calls end' <<< "$output" \
        || { echo "FAIL: the constrained static call block did not complete" >&2; exit 1; }
    for label in 'static default' 'static generic' 'static abstract' 'static generic abstract' \
        'static generic ref' 'static generic receiver' 'static generic owner ref' 'static generic owner value' \
        'value default' 'value generic' \
        'value abstract' 'value generic abstract' 'concrete default' 'concrete generic' \
        'concrete abstract' 'concrete generic abstract' 'concrete value default' \
        'concrete value generic' 'concrete value abstract' 'concrete value generic abstract' \
        'static group invoked'; do
        grep -Fq "$label: System.Runtime.AmbiguousImplementationException: Could not call method " <<< "$output" \
            && grep -Fxq "$label hresult: 0x8013106A" <<< "$output" \
            && grep -Fxq "$label entries: 1" <<< "$output" \
            || { echo "FAIL: $label did not reject ambiguity at the call instruction" >&2; exit 1; }
    done
    for label in 'static default skipped' 'static generic skipped' 'static abstract skipped' \
        'value default skipped' 'value generic skipped' 'value abstract skipped' \
        'concrete default skipped' 'concrete generic skipped' 'concrete abstract skipped' \
        'concrete value default skipped' 'concrete value generic skipped' 'concrete value abstract skipped'; do
        grep -Fxq "$label returned: skipped" <<< "$output" \
            && grep -Fxq "$label entries: 1" <<< "$output" \
            || { echo "FAIL: $label rejected a call that never ran" >&2; exit 1; }
    done
    for row in 'static group created entries: 0' 'static left bodies: left,left,left,left' \
        'static specific bodies: specific,specific,specific,specific' \
        'static class bodies: class,class,class,class' 'static fallback bodies: base,base,class,class'; do
        grep -Fxq "$row" <<< "$output" \
            || { echo "FAIL: missing static call witness: $row" >&2; exit 1; }
    done
    before=$(run_bounded dotnet "$_CG_APP" before-static-calls)
    before=$(strip_cr_win "$before")
    prefix=$(awk '/^== constrained static interface calls ==$/ { exit } { print }' <<< "$output")
    assert_output "$prefix" "$before"
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

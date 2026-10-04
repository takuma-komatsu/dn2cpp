#!/usr/bin/env bash
# Real synchronization primitives — Interlocked (i4/i8/ref atomics + return-value
# contract), Volatile.Read/Write (int/long/double/bool), legacy
# Thread.VolatileRead/Write (integer/reference/float/double), Interlocked.MemoryBarrier, and
# [ThreadStatic] (main-thread behavior), and managed WaitHandle subclass key lifetime
# across collection and address reuse. Primitive observations are single-threaded;
# cancellation callback faults run on a joined worker and a controlled timer.
# Deterministic output is diffed exact vs real .NET (corelib_diff_gate).
# Also covers the two lowerings of the `lock` STATEMENT, single-threaded and so
# equally deterministic: LockSubset (`lock (object)` -> Monitor.Enter(ref taken) /
# finally Exit, plus the explicit Monitor.TryEnter forms) and LockTypeSubset (the
# .NET 9 `System.Threading.Lock` -> EnterScope() / finally Scope.Dispose(), plus
# the `using`-statement form and TryEnter/Enter/Exit). Both exercise a
# value-returning body, re-entrant nesting and a throwing body that must still
# release.
# Former gates: lock-subset, locktype-subset.
#
# MonotonicClock is here for its SURFACE, not for this gate's theme:
# Stopwatch.GetTimestamp and the user assembly's Environment.TickCount64
# MemberReference reach separate libSystem.Native clocks through their real BCL
# bodies. This is the native axis's only diff of that contract against real .NET;
# its wasm twin also proves both PAL symbols link. Do not prune it by reading the
# gate's name.
# Named argument faults, boxed bounds, validation order and GC-retained fields.
# Monitor and Lock have independent ownership, checked exits and synchronized epilogues.
# Registration object equality names its type-info without a box or typeof site.
# Lock's runtime header preserves its CLR identity through typed and shared casts.
source "$(dirname "$0")/_common.sh"
registration_app="gates/fixtures/registration-object-equality/bin/$CONFIG/$TFM/RegistrationObjectEquality.dll"
build_gate_proj gates/fixtures/registration-object-equality/RegistrationObjectEquality.csproj
DN2CPP_GATE_EXTRA_INPUTS="${DN2CPP_GATE_EXTRA_INPUTS:-} $registration_app ${registration_app%.dll}.runtimeconfig.json ${registration_app%.dll}.deps.json"
DN2CPP_GATE_EXTRA_CONTEXT="${DN2CPP_GATE_EXTRA_CONTEXT:-}|registration-object-equality|cli:$(_gate_cli_hash)"
DN2CPP_GATE_EXTRA_CONTEXT="$DN2CPP_GATE_EXTRA_CONTEXT|prefix:before-cancellation-thread-faults"
gate_extra_asserts() {
    local out="$1" native before prefix line
    [[ "$_CG_APP" == */ThreadingPrimitives.dll ]] || return 0
    native=$(run_bounded "./$out/ThreadingPrimitives")
    native=$(strip_cr_win "$native")
    before=$(run_bounded dotnet "$_CG_APP" before-cancellation-thread-faults)
    before=$(strip_cr_win "$before")
    prefix=$(awk '/^== cancellation thread faults ==$/ { exit } { print }' <<< "$native")
    assert_output "$prefix" "$before"
    for line in '== cancellation thread faults ==' \
        'cancel worker False: newer;older;remaining;|AggregateException|identity=True|requested=True' \
        'cancel worker True: newer;|InvalidOperationException|identity=True|requested=True' \
        'cancel handled timer: True|newer;older;remaining;|2|timer newer,timer older|requested=True' \
        'cancellation thread faults end'; do
        grep -Fxq -- "$line" <<< "$native" \
            || { echo "FAIL: cancellation thread fault witness missing: $line" >&2; exit 1; }
    done
    before=$(dotnet "$_CG_APP" before-argument-fields)
    before=$(strip_cr_win "$before")
    prefix=$(awk '/^== WaitHandle array fields ==$/ { exit } { print }' <<< "$native")
    assert_output "$prefix" "$before"
    for line in '== WaitHandle array fields ==' \
        'wait null param=waitHandles' \
        'wait empty param=waitHandles' \
        'wait element param=waitHandles[1]' \
        'wait fields GC type=ArgumentException' \
        'WaitHandle array fields end'; do
        grep -Fxq -- "$line" <<< "$native" \
            || { echo "FAIL: ThreadingPrimitives validation witness missing: $line" >&2; exit 1; }
    done
    before=$(dotnet "$_CG_APP" before-ownership)
    before=$(strip_cr_win "$before")
    prefix=$(awk '/^== monitor ownership ==$/ { exit } { print }' <<< "$native")
    assert_output "$prefix" "$before"
    for line in '== monitor ownership ==' \
        'monitor ownership end' \
        'lock ownership end' \
        'independent lock after monitor=False' \
        'independent monitor after lock=False' \
        'owner held=True/True' \
        'independent ownership end' \
        'synchronized released=False' \
        'ownership fields end'; do
        grep -Fxq -- "$line" <<< "$native" \
            || { echo "FAIL: ThreadingPrimitives ownership witness missing: $line" >&2; exit 1; }
    done
    before=$(dotnet "$_CG_APP" before-lock-identity)
    before=$(strip_cr_win "$before")
    prefix=$(awk '/^== Lock runtime identity ==$/ { exit } { print }' <<< "$native")
    assert_output "$prefix" "$before"
    for line in '== Lock runtime identity ==' \
        'Lock type: Lock System.Threading.Lock System.Threading.Lock' \
        'Lock reflection: True True True True System.Object' \
        'Lock typed round trip: True True InvalidCastException' \
        'Lock shared round trip: True True InvalidCastException' \
        'Lock runtime identity end'; do
        grep -Fxq -- "$line" <<< "$native" \
            || { echo "FAIL: Lock runtime identity witness missing: $line" >&2; exit 1; }
    done
    if ! awk '
            { sub(/\r$/, "") }
            $0 == "// LockTypeSubset.Program::FromObject" { subject = 1; next }
            subject && /^(inline )?Dn2CppObject\* Program_FromObject_TisCnRef_m[0-9]+\(.*\)$/ {
                body = 1; subject = 0; next
            }
            subject && /^\/\// { subject = 0 }
            body {
                if (/dn2cpp_castclass/ && /__rgctx\[/) found = 1
                if (/^}/) body = 0
            }
            END { exit !found }
        ' "$out/generated.h" "$out"/generated*.cpp; then
        echo 'FAIL: Lock round trip did not exercise a shared generic cast' >&2
        exit 1
    fi
    local fixture="$out/registration-object" expected actual
    invoke_cli "$registration_app" -r "$_CG_CORELIB" -o "$fixture"
    compile_console "$fixture" RegistrationObjectEquality
    expected=$(run_bounded dotnet "$registration_app")
    actual=$(run_bounded "./$fixture/RegistrationObjectEquality")
    actual=$(strip_cr_win "$actual")
    assert_output "$actual" "$(strip_cr_win "$expected")"
    for line in 'null: False' 'wrong: False' \
        'registered null: False' 'registered wrong: False' \
        'registration object equality end'; do
        grep -Fxq -- "$line" <<< "$actual" \
            || { echo "FAIL: registration object equality witness missing: $line" >&2; exit 1; }
    done
}

corelib_diff_gate ThreadingPrimitives
corelib_diff_gate MonotonicClock

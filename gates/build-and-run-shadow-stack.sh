#!/usr/bin/env bash
# Consolidated --shadow-stack gate. The flag ships OFF by default, so without
# this nothing in the suite exercises it — yet it changes every compiled body's
# prologue AND is the mode whose whole value is a DETERMINISTIC exception trace: a
# kind-1 (shadow-stack) trace renders emitter-baked frame-name literals, so it
# survives -O2 inlining (not one NoInlining attribute in the sample) and exists on
# WASM, where the kind-0 walk captures nothing. That determinism is what lets
# the flag-on arms freeze FULL trace texts — the kind-0 coverage
# (ReflectTypes/StackTraceThrowSubset, the flag-off freeze) can only pin
# Contains()/StartsWith() verdicts on NoInlining frames.
#
# Arms 1, 2 and 4 drive ONE program (samples/dotnet/ShadowStack), transpiled
# twice one flag apart; argv picks the run mode, so no arm adds a transpile (see
# the sample's Program.cs). Arm 3 adds the hotupdate pair — the one surface a
# console program cannot fold in, because interpreted patch frames exist only
# behind --emit-patch:
#
#   1. no flag -> the generated output must not name Dn2CppShadowFrame at all (the
#      cheap permanent stand-in for the flag-off byte-identity proved by diff -r
#      when the emit landed), and the binary's "smoke" mode — the same throw paths
#      printing only trace-INDEPENDENT lines — diffs LIVE against real .NET. The
#      full-trace sections deliberately do not run flag-off: a kind-0 trace under
#      -O2 is inlining-dependent by construction, and this gate must
#      never go red because clang changed an inlining heuristic.
#
#   2. --shadow-stack -> "full" mode, native stdout diffed EXACTLY against the
#      frozen gates/expected/shadow-stack.txt: whole trace texts line for line
#      (the no-NoInlining deep chain, the shared-generics canonical frame naming
#      its representative + " [shared generic]", `throw;` preserving / `throw ex;`
#      re-capturing, the 1024-capacity marker line). Then the SAME binary reruns
#      in "unhandled" mode: the escaping exception must abort and its stderr
#      report must carry the shadow frames down to the deepest one.
#
#   3. --shadow-stack x hotupdate -> HotUpdateBase transpiled with
#      --hotupdate-base --shadow-stack, HotUpdatePatch baked to a BPI exactly as
#      build-and-run-hotupdate-subset.sh does it, and the base's --shadow-trace
#      mode drives an AOT -> interp -> AOT -> interp chain whose deepest patch
#      method throws. Stdout is diffed EXACTLY against the frozen
#      gates/expected/shadow-stack-hotupdate.txt: ONE kind-1 trace carrying the
#      interpreted frames' loader-materialized "DeclType.Method()" names
#      interleaved between the AOT guards' transpile-baked names. The plain-base
#      inverse (kind-0 must DROP interpreter frames, never misattribute them)
#      lives in build-and-run-hotupdate-subset.sh, next to the binary it reuses.
#
#   4. WASM (em++/emcmake/node) -> the arm-2 transpile built by emcmake, run under
#      node, compared against THE SAME expected file as arm 2 — the direct claim
#      that the trace is exact on every platform (the kind-0 walk captures nothing
#      on wasm; non-null AND byte-identical content is the point). Without the
#      Emscripten toolchain this arm alone is skipped via gate_partial — a
#      whole-gate gate_skip would discard the native arms' asserted coverage —
#      and DN2CPP_REQUIRE_ALL=1 turns the missing arm into a failure, exactly
#      like a whole-gate skip. LAST on purpose: its missing-toolchain path exits
#      the script, so an arm placed after it would silently never run on a
#      machine without Emscripten.
#
# The frozen snapshot's divergences from real .NET are declared in the sample
# headers (samples/dotnet/ShadowStack/*.cs), verified against `dotnet <app> full`
# at capture time: no parameter lists, the whole live stack at throw rather than
# the throw->handler window (real .NET's trace stops at the catch; the shadow
# stack stamps down to Main), the representative-named " [shared generic]" frame,
# exact `throw;` preservation, and the capacity marker.
source "$(dirname "$0")/_common.sh"

PROJECT=ShadowStack
EXPECTED="$(dirname "$0")/expected/shadow-stack.txt"

echo "== Building the app assembly =="
build_proj "samples/dotnet/$PROJECT/$PROJECT.csproj"
APP="samples/dotnet/$PROJECT/bin/$CONFIG/$TFM/$PROJECT.dll"
CORELIB=$(locate_corelib)
echo "corelib: $CORELIB"

# ── Arm 1/4: no flag — no guard token in the output, smoke diff live vs real .NET ─
echo "== Arm 1/4: no flag — clean output + smoke-mode diff vs real .NET =="
OUT=artifacts/shadowstack-plain
invoke_cli "$APP" -r "$CORELIB" -o "$OUT"
# The transpile always runs (cheap, deterministic), so this pollution grep asserts
# on every pass, cache hit or not. Top-level generated* only: $OUT/.cmake would
# add build products (a stale PCH is even named generated.h.pch) to the sweep.
if grep -q "Dn2CppShadowFrame" "$OUT"/generated*; then
    echo "FAIL: flag-off transpile emitted Dn2CppShadowFrame — --shadow-stack leaked into the default output" >&2
    exit 1
fi
if grep -q "dn2cpp_shadow_stack_mark_enabled" "$OUT"/generated*; then
    echo "FAIL: flag-off transpile emitted dn2cpp_shadow_stack_mark_enabled — --shadow-stack leaked into the default output" >&2
    exit 1
fi
if gate_cache_check "$OUT" "shadow-stack-plain|$CORELIB" \
        "$APP" "${APP%.dll}.runtimeconfig.json" "${APP%.dll}.deps.json"; then
    gate_cache_hit_msg
else
    compile_console "$OUT" "$PROJECT"
    set +e
    native=$("./$OUT/$PROJECT" smoke); native_code=$?
    expected=$(dotnet "$APP" smoke); expected_code=$?
    set -e
    assert_output "$native" "$expected"
    assert_exit_code "$native_code" "$expected_code"
    gate_cache_commit
fi

# ── Arm 2/4: --shadow-stack — full-trace freeze + unhandled report ─────────────
echo "== Arm 2/4: --shadow-stack — full-trace freeze + unhandled report =="
OUT=artifacts/shadowstack
invoke_cli "$APP" -r "$CORELIB" --shadow-stack -o "$OUT"
# Inverse of arm 1's sweep: the flag must actually arm (also on every pass).
grep -q "Dn2CppShadowFrame" "$OUT"/generated* \
    || { echo "FAIL: --shadow-stack transpile emitted no Dn2CppShadowFrame guard" >&2; exit 1; }
grep -q "dn2cpp_shadow_stack_mark_enabled" "$OUT"/generated* \
    || { echo "FAIL: --shadow-stack transpile emitted no dn2cpp_shadow_stack_mark_enabled init call" >&2; exit 1; }
if gate_cache_check "$OUT" "shadow-stack-on|$CORELIB" \
        "$APP" "${APP%.dll}.runtimeconfig.json" "${APP%.dll}.deps.json" "$EXPECTED"; then
    gate_cache_hit_msg
else
    compile_console "$OUT" "$PROJECT"
    set +e
    native=$("./$OUT/$PROJECT" full); native_code=$?
    set -e
    assert_output "$(strip_cr_win "$native")" "$(cat "$EXPECTED")"
    assert_exit_code "$native_code" 0

    # Unhandled tail (same binary, argv-switched): an exception escaping Main
    # must abort, and the stderr report must carry the shadow frames — grep
    # shape as in build-and-run-unhandled-exit-subset.sh's kind-0 arm, but the
    # deepest frame is exact-matched: under the shadow stack the whole chain is
    # guaranteed, NoInlining or not.
    set +e
    err=$("./$OUT/$PROJECT" unhandled 2>&1 >/dev/null); code=$?
    set -e
    if [ "$code" -eq 0 ]; then
        echo "FAIL: unhandled mode exited 0 — an escaped exception must abort" >&2
        exit 1
    fi
    err=$(strip_cr_win "$err")
    if ! grep -q '^   at ' <<<"$err"; then
        echo "FAIL: unhandled-exception stderr carries no '   at ' trace line" >&2
        printf '%s\n' "$err" >&2
        exit 1
    fi
    if ! grep -q '^   at DeepChainSubset\.Program\.Depth5()$' <<<"$err"; then
        echo "FAIL: unhandled stderr does not name the deepest shadow frame (DeepChainSubset.Program.Depth5)" >&2
        printf '%s\n' "$err" >&2
        exit 1
    fi
    echo "unhandled report OK: exit $code, names DeepChainSubset.Program.Depth5"
    gate_cache_commit
fi

# ── Arm 3/4: --shadow-stack x hotupdate — interpreted frames join the trace ────
# The base transpiles with --hotupdate-base --shadow-stack and the patch
# bakes exactly as build-and-run-hotupdate-subset.sh does it; the base's
# --shadow-trace mode (see both samples' Program.cs) loads the patch — whose
# entry parks a ShadowProbe and prints nothing — then Counter.Render (AOT)
# virtual-dispatches onto the interpreted ShadowProbe.Describe override, which
# re-enters AOT through Counter.RenderVia onto ShadowNote.Describe (interp),
# which throws. The frozen snapshot pins the caught exception's WHOLE kind-1
# trace: interpreted frames named by the loader-materialized BPI names,
# SANDWICHED between AOT frames named by the transpile-baked guards — every
# name baked or materialized, none PC-resolved, so the freeze is exact on any
# platform. Console lane only: no Godot, no Emscripten, no skip path.
echo "== Arm 3/4: --shadow-stack + hotupdate — interp/AOT interleaved trace =="
build_proj samples/dotnet/HotUpdateBase/HotUpdateBase.csproj
build_proj samples/dotnet/HotUpdatePatch/HotUpdatePatch.csproj
HU_APP="samples/dotnet/HotUpdateBase/bin/$CONFIG/$TFM/HotUpdateBase.dll"
HU_PATCH="samples/dotnet/HotUpdatePatch/bin/$CONFIG/$TFM/HotUpdatePatch.dll"
HU_EXPECTED="$(dirname "$0")/expected/shadow-stack-hotupdate.txt"
OUT=artifacts/shadowstack-hotupdate
invoke_cli "$HU_APP" --hotupdate-base \
    --hotupdate-refs samples/dotnet/HotUpdatePatch/hotupdate-refs.txt \
    --shadow-stack -o "$OUT"
[ -f "$OUT/base-abi.json" ] || { echo "FAIL: base-abi.json sidecar missing" >&2; exit 1; }
# Arm 2's inverse sweeps, on this transpile surface (also on every pass): the
# flag must arm the guards AND the init call the interpreter's frame-name
# materialization keys on (dn2cpp_shadow_stack_is_enabled at patch load).
grep -q "Dn2CppShadowFrame" "$OUT"/generated* \
    || { echo "FAIL: --shadow-stack hotupdate transpile emitted no Dn2CppShadowFrame guard" >&2; exit 1; }
grep -q "dn2cpp_shadow_stack_mark_enabled" "$OUT"/generated* \
    || { echo "FAIL: --shadow-stack hotupdate transpile emitted no dn2cpp_shadow_stack_mark_enabled init call" >&2; exit 1; }
invoke_cli --emit-patch "$HU_PATCH" --base-abi "$OUT/base-abi.json" -o "$OUT"
# Key inputs beyond the $OUT transpile surface (generated* + base-abi.json,
# hashed by gate_cache_check itself): the two assemblies, the baked BPI the
# run loads, and the frozen snapshot.
if gate_cache_check "$OUT" "shadow-stack-hotupdate" \
        "$HU_APP" "${HU_APP%.dll}.runtimeconfig.json" "${HU_APP%.dll}.deps.json" \
        "$HU_PATCH" "$OUT/HotUpdatePatch.bpi" "$HU_EXPECTED"; then
    gate_cache_hit_msg
else
    compile_console "$OUT" HotUpdateBase
    set +e
    native=$("./$OUT/HotUpdateBase" --shadow-trace "$OUT/HotUpdatePatch.bpi"); native_code=$?
    set -e
    assert_output "$(strip_cr_win "$native")" "$(cat "$HU_EXPECTED")"
    assert_exit_code "$native_code" 0
    gate_cache_commit
fi

# ── Arm 4/4: WASM — the SAME frozen snapshot under node ────────────────────────
echo "== Arm 4/4: WASM — same expected file, built by emcmake, run under node =="
# --no-bundled: an -O-less link needs the -debug archives the frozen bundle lacks.
dn2cpp_emsdk_resolve --no-bundled
for tool in em++ emcmake node; do
    if ! command -v "$tool" >/dev/null 2>&1; then
        gate_partial "$tool not found — no Emscripten toolchain to build wasm with; the wasm identical-trace arm did not run"
        echo "OK (native arms asserted)"
        exit 0
    fi
done
(
    export WASM=1
    OUT=artifacts/shadowstack-wasm
    invoke_cli "$APP" -r "$CORELIB" --shadow-stack -o "$OUT"
    if gate_cache_check "$OUT" "shadow-stack-wasm|$CORELIB" \
            "$APP" "${APP%.dll}.runtimeconfig.json" "${APP%.dll}.deps.json" "$EXPECTED"; then
        gate_cache_hit_msg
        exit 0
    fi
    compile_console_wasm "$OUT" "$PROJECT"
    # Output-only diff, like wasm_corelib_diff_gate: node passes argv through to
    # the module (the launcher forwards process.argv[2..]). The exit status is
    # captured explicitly (`$(...)` inline would swallow it): the `full` arm is
    # a CAUGHT throw, so the module must exit 0 after printing the trace.
    set +e
    wasm_out=$(node "./$OUT/$PROJECT.js" full); wasm_rc=$?
    set -e
    assert_output "$wasm_out" "$(cat "$EXPECTED")"
    assert_exit_code "$wasm_rc" 0
    gate_cache_commit
)

echo "OK"

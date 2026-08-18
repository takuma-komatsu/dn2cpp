#!/usr/bin/env bash
# The REAL UniTask 2.5.11 — Cysharp's .NET build from nuget.org (package id
# `UniTask`, MIT), the netstandard/net lane usable outside Unity — transpiled from
# its OWN shipped IL and run as a native console binary, byte-diffed against the
# same program on real .NET.
#
# This is the console-lane sibling of gates/build-and-run-gdtask.sh: the same
# tier-2 claim (docs/ARCHITECTURE.md §4-B) on the other real third-party task
# library, with NO engine underneath — it runs under SKIP_GODOT=1 and on the
# hosted CI runners. ZERO CUTS, ZERO FLAGS: the sample's `await UniTask.Yield()`
# is a cross-assembly MemberRef outside the adoption contract, so the pre-scan
# (Compilation.DeclineOutOfContractAdoptions) declines UniTask's adoption on its
# own and the library's real machinery — AsyncUniTaskMethodBuilder (both
# arities), UniTaskCompletionSource, the TaskPool object pool, WhenAllPromise,
# the ThreadPool-backed YieldAwaitable — transpiles through the general pipeline.
#
# What the sample covers: Yield, CompletedTask, FromResult, Run,
# SwitchToThreadPool, Task interop both ways (AsUniTask / AsTask), WhenAll over
# UniTask<T> (tuple + array) and UniTask (void), and exception propagation
# through the library's ExceptionResultSource — all as sequential awaits, so the
# printed order is a property of the program, never of the clock. UniTask.Delay
# and the rest of the PlayerLoop surface do NOT exist in the package's .NET TFMs
# (they are Unity-PlayerLoop-scheduled), so timer waits ride
# Task.Delay().AsUniTask() — an exclusion of the package's, not a carve-out of
# ours.
#
# WHAT THIS GATE ASSERTS (the gdtask ladder, console-shaped):
#   1. The transpile SUCCEEDS with no flags, bounded, and the stderr decline
#      notice names Cysharp.Threading.Tasks.UniTask — a silently re-fired
#      adoption would fail loud at emit on Yield, so the notice check catches a
#      decline that fired for the wrong reason or stopped printing.
#   2. ZERO gaps under --measure (a gap that degrades to a runtime-throwing stub
#      is a row there but not a transpile failure, so this is the stronger claim).
#   3. The library's real machinery is IN the generated C++
#      (gates/expected/unitask-markers.txt — symbol prefixes, one per line).
#   4. The native binary's stdout and exit code exact-match real .NET running
#      the same assemblies.
#
# WHY A SEPARATE GATE: a real third-party library transpiled from its own
# shipped IL needs its own .csproj, because the acquisition path IS under test —
# a plain <PackageReference Include="UniTask" Version="2.5.11" /> restored from
# nuget.org; no DLL is vendored. WHAT is transpiled is pinned by content hash
# (gates/expected/unitask-dll.sha256, the gdtask discipline): a re-published
# 2.5.11, a poisoned cache or an accidental version bump fails there, not as a
# mystified gap count. The package's net7.0 lib is its highest TFM, so that is
# what a net10.0 app restores.
source "$(dirname "$0")/_common.sh"

project=UniTaskSample
out="artifacts/unitask"
measure="artifacts/unitask-measure"
UNITASK_SHA_EXPECTED=gates/expected/unitask-dll.sha256
MARKERS_EXPECTED=gates/expected/unitask-markers.txt

echo "== 1/7 Locating the real net10 CoreLib =="
# net10-pinned: the highest installed runtime can be an 11.0 preview whose
# CoreLib shape skews the transpile spuriously.
corelib=$(resolve_net10_corelib)
echo "corelib: $corelib"

echo "== 2/7 Building the driver against the real NuGet UniTask =="
# The restore needs nuget.org exactly once per machine; with the package neither
# cached nor reachable the prerequisite is absent, which is a SKIP, not a pass.
if [ ! -d "$HOME/.nuget/packages/unitask/2.5.11" ] \
    && ! curl -fsI --max-time 15 https://api.nuget.org/v3/index.json >/dev/null 2>&1; then
    gate_skip "UniTask 2.5.11 is not in the NuGet cache and nuget.org is unreachable"
fi
build_proj "samples/dotnet/$project/$project.csproj"
app="samples/dotnet/$project/bin/$CONFIG/$TFM/$project.dll"
unitask="samples/dotnet/$project/bin/$CONFIG/$TFM/UniTask.dll"
[ -f "$app" ]     || { echo "FAIL: not built: $app" >&2; exit 1; }
[ -f "$unitask" ] || { echo "FAIL: UniTask did not restore to $unitask" >&2; exit 1; }

# Content tripwire on the library itself: the transport is the network, WHAT the
# gate transpiles is pinned by hash.
unitask_sha="$(shasum -a 256 "$unitask" | awk '{print $1}')"
if [ "$unitask_sha" != "$(cat "$UNITASK_SHA_EXPECTED")" ]; then
    echo "FAIL: UniTask.dll is not the pinned 2.5.11 net7.0 build" >&2
    echo "      expected: $(cat "$UNITASK_SHA_EXPECTED")  ($UNITASK_SHA_EXPECTED)" >&2
    echo "      actual:   $unitask_sha" >&2
    echo "      A different UniTask means different IL — re-audit before re-freezing." >&2
    exit 1
fi
echo "UniTask.dll: $unitask_sha (pinned 2.5.11)"

echo "== 3/7 Transpiling for real (tier 2: NO flags — the adoption must auto-decline) =="
build_proj src/Dn2Cpp.Cli/Dn2Cpp.Cli.csproj
CLI_DLL="${DN2CPP_CLI_DLL:-$PWD/src/Dn2Cpp.Cli/bin/$CONFIG/$TFM/dn2cpp.dll}"
CLI_SHA="$(shasum -a 256 "$CLI_DLL" | awk '{print $1}')"
refs=(-r "$corelib" -r "$unitask")
rm -rf "$out"
# Both caps ride the ONE transpile the diff uses; a cap can only turn a run into
# an abort or back, never perturb a succeeding transpile's bytes (AGENTS.md).
# Measured 1,516 instantiations (inst 1,445 + minst 71), cap ~8x — the declined
# UniTask closure is IN this count, so a resurrected signature runaway (the
# GDTask shape) trips it at once. Measured peak heap 47 MB; the belt is ~5x.
# (export inside the subshell: bash does not reliably scope `VAR=x func`.)
rc=0
err=$( export DN2CPP_MAX_INSTANTIATIONS=12000
       invoke_cli "$app" "${refs[@]}" --auto-ref --max-heap-mb 256 \
           -o "$out" 2>&1 >/dev/null ) || rc=$?
if [ "$rc" -ne 0 ]; then
    echo "FAIL: the real UniTask no longer transpiles flag-free in 12,000 instantiations / 256 MB (exit $rc)." >&2
    echo "      An emit-time 'no intrinsic mapping' on a UniTask member means the pre-scan" >&2
    echo "      missed the out-of-contract reference and adoption re-fired." >&2
    echo "$err" >&2
    exit 1
fi
# The notice is the decline's only surface in a SUCCEEDING transpile, stderr-only
# by contract (the generated bytes, and the cache key over them, must not move).
if ! grep -q "adoption declined: Cysharp.Threading.Tasks.UniTask::" <<<"$err"; then
    echo "FAIL: the transpile succeeded but never printed the adoption-declined notice" >&2
    echo "      naming Cysharp.Threading.Tasks.UniTask. Either the decline did not fire" >&2
    echo "      (the run may be adopted and miscompiled) or the notice regressed." >&2
    printf '%s\n' "$err" >&2
    exit 1
fi
echo "OK (declined flag-free; bounded: <=12,000 instantiations, <=256 MB heap)"

# Everything past this point is a pure function of the transpile surface in $out,
# the CLI that produced it (the zero-gap claim depends on the transpiler's
# behaviour, not only its output), the corelib and the pinned inputs.
if gate_cache_check "$out" \
        "unitask|cli=$CLI_SHA|corelib=$corelib" \
        "$app" "$unitask" "$MARKERS_EXPECTED" \
        "${app%.dll}.runtimeconfig.json" "${app%.dll}.deps.json"; then
    gate_cache_hit_msg
    exit 0
fi

echo "== 4/7 ASSERT: ZERO gaps, ZERO cuts =="
rm -rf "$measure"
( export DN2CPP_MAX_INSTANTIATIONS=12000
  invoke_cli "$app" "${refs[@]}" --auto-ref --max-heap-mb 256 \
      --measure -o "$measure" >/dev/null 2>&1 ) || true
# A MISSING report is a measure run that died, not zero gaps (TranspileDriver
# writes the TSV unconditionally on a clean corpus).
[ -f "$measure/s0-gaps.tsv" ] \
    || { echo "FAIL: --measure produced no gap report ($measure/s0-gaps.tsv) — the measure run died, which is not the same as a clean corpus" >&2; exit 1; }
gaps=$(wc -l < "$measure/s0-gaps.tsv" | tr -d ' ')
if [ "$gaps" -ne 0 ]; then
    echo "FAIL: the real UniTask no longer transpiles cleanly with NO carve-outs." >&2
    echo "      $gaps gap(s):" >&2
    LC_ALL=C cut -f2,3,5 "$measure/s0-gaps.tsv" | LC_ALL=C sed 's/^/        /' >&2
    echo "      Fix the gap in the transpiler; a carve-out here is how one nobody" >&2
    echo "      rechecks gets in." >&2
    exit 1
fi
echo "0 gaps, 0 cuts"

echo "== 5/7 The library's real machinery MUST be in the tree =="
# The inverse of adoption: a silently re-fired adoption cannot get past step 3
# (Yield fails loud at emit), so what this catches is a decline that routed the
# wrong assembly or a stale output directory. Symbol prefixes, one per line.
while IFS= read -r sym; do
    [ -n "$sym" ] || continue
    if ! grep -qh "$sym" "$out/generated.h"; then
        echo "FAIL: the auto-declined transpile did not put $sym in the tree" >&2
        echo "      (see $MARKERS_EXPECTED) — UniTask's real IL is not being transpiled." >&2
        exit 1
    fi
done < "$MARKERS_EXPECTED"
echo "OK ($(grep -c . "$MARKERS_EXPECTED") machinery symbols present, no flag)"

echo "== 6/7 Compiling C++ =="
compile_console "$out" "$project"

echo "== 7/7 Running (exact diff vs real .NET) =="
set +e
expected=$(dotnet "$app");   expected_code=$?
native=$("./$out/$project"); native_code=$?
set -e
assert_output "$native" "$expected"
assert_exit_code "$native_code" "$expected_code"
gate_cache_commit
echo "OK — the real UniTask 2.5.11, transpiled from its own IL with zero flags,"
echo "     ran byte-identically to real .NET."

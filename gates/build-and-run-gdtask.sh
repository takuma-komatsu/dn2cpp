#!/usr/bin/env bash
# The REAL GDTask 3.1.0 — a third-party async library, from nuget.org, MIT —
# transpiled from its OWN IL and run in the real Godot engine.
#
# This is the tier-2 custom-async-task lane end to end (docs/ARCHITECTURE.md
# §4-B). Tier 1 ADOPTS a custom async task type into dn2cpp's intrinsic Task
# family, which is cheap but only covers what the await pattern itself requires:
# the library's own combinators (WhenAll/WhenAny), its player-loop scheduler, its
# object pool, its cancellation model are all unmapped. Tier 2 (--no-adopt-async)
# declines to adopt and transpiles the library's real machinery instead. This
# gate is the proof that tier 2 works on a library nobody wrote for dn2cpp.
#
# ZERO CUTS. The engineering cost of tier 2 on this library is now nothing at all:
# no --cut, no --no-adopt-async carve-out beyond declining the adoption itself.
#
# It used to be one. `--cut GodotTask.TaskTracker::TrackActiveTask` was needed
# because that method opens with `new StackTrace()`, whose capture is an
# InternalCall into the CLR's stack walker. Two changes removed it, and neither is
# specific to GDTask:
#   - System.Diagnostics.StackTrace / StackFrame are DEGRADED INTRINSICS —
#     real objects reporting zero frames, with a ToString that says so. A stack
#     trace is a DIAGNOSTIC, and a caller of new StackTrace() proceeds regardless
#     of what it finds, so the honest posture is to degrade, not to refuse (and
#     not to throw: TaskTracker creates one per promise, ~110 call sites). That is
#     what NativeAOT and IL2CPP ship. docs/ARCHITECTURE.md §4-B has the rule.
#   - Removing that blocker exposed the NEXT one in the same method, which had
#     been hiding behind it: `ldvirtftn` of an interface method (a delegate bound
#     to an interface method group). It is now lowered through the receiver's
#     interface table, exactly as a callvirt on an interface is — see the
#     default-interface-method gate, which covers it directly.
#
# So this gate's claim got STRICTLY STRONGER and simpler: the real GDTask 3.1.0
# transpiles with ZERO gaps and ZERO cuts. If a future regression makes a carve-out
# necessary again, step 4 goes red. (The --cut lever itself is NOT deleted — it is
# still the right tool for a genuinely untranspilable corner, and it keeps its
# synthetic regression test in gates/build-and-run-transpiler-limits.sh.)
#
# Note what never needed cutting: TaskTrackerWindow, and with it the
# Regex/Immutable/reflection subtree it would have dragged in, tree-shakes away on
# its own, because nothing calls ShowTrackerWindow.
#
# WHAT THIS GATE ASSERTS — the regression value is here, not in the run:
#   1. The transpile has ZERO gaps with NO cuts (--measure enumerates every
#      reachable gap instead of stopping at the first; a gap that degrades to a
#      runtime-throwing stub would still be a row, so "it compiled" is not the
#      same claim).
#   2. The real (non-measure) transpile SUCCEEDS with no cuts — a gap the
#      transpiler enumerates but then happily emits past would be a silent
#      miscompile, not a clean bill of health.
#   3. The native binary's output exact-matches the frozen marker set AND the
#      same project running on the REAL .NET (CoreCLR under the pinned mono
#      editor: same engine, same GodotSharp, same GDTask.dll — only the managed
#      runtime differs, so the diff isolates exactly what dn2cpp changes).
#
# WHY A SEPARATE GATE (AGENTS.md says new coverage should normally be a section
# in an existing bucket): this is a genuinely new area — a real third-party
# library transpiled from its own shipped IL — and it needs its own .csproj,
# because the acquisition path IS part of what is under test: a plain
# <PackageReference Include="GDTask" Version="3.1.0" />, restored from nuget.org.
# No DLL is vendored in this repository. Folding that into the DM lane's
# DotnetSample would mean putting GDTask on the transpile path of three unrelated
# gates.
#
# Requires the artifacts of gates/setup-godot-dotnet.sh, found at that script's
# default root (DN2CPP_GODOT_DOTNET_ROOT overrides): editor_bin + template_bin
# + GodotSharp + the local Sdk feed. gate_skip's when absent — reported as a
# SKIP, never as a pass.
# Registered in run-all-gates.sh's Godot phase (it launches the engine) in its
# own chain: its write set (this project dir, gates/out-gdtask*) is disjoint from
# every other chain's. Every engine/editor launch runs under a watchdog — a
# broken Godot run hangs rather than fails.
source "$(dirname "$0")/_common.sh"
source "$(dirname "$0")/_godot_dotnet.sh"

SAMPLE_DIR=samples/godot-dotnet/GDTaskSample
OUT=gates/out-gdtask
MEASURE=gates/out-gdtask-measure/nocut
ROOT="$GODOT_DOTNET_ROOT"
PINNED_COMMIT=a13da4feb8d8aefc283c3763d33a2f170a18d541
ABI_EXPECTED=gates/expected/godot-dotnet-abi.sha256
GDTASK_SHA_EXPECTED=gates/expected/gdtask-dll.sha256
MARKERS_EXPECTED=gates/expected/gdtask-markers.txt

if ! godot_dotnet_root_ok || [ ! -x "$GODOT_DOTNET_EDITOR" ] || [ ! -x "$GODOT_DOTNET_TEMPLATE" ] \
    || [ ! -f "$ROOT/pin.txt" ]; then
    gate_skip "godot-dotnet artifacts absent/incomplete at $ROOT — run gates/setup-godot-dotnet.sh (or set DN2CPP_GODOT_DOTNET_ROOT)"
fi

echo "== 1/7 Pin + interop-ABI tripwire =="
godot_dotnet_pin_abi_check "$PINNED_COMMIT" "$ABI_EXPECTED"

echo "== 2/7 Building the sample against the real Godot.NET.Sdk + the real GDTask =="
godot_dotnet_nuget_config "$SAMPLE_DIR"
# ExportRelease is what the transpiler consumes; Debug is what the *editor*
# loads, and the editor is the real-.NET oracle in step 7. Both are built here
# even under DN2CPP_SKIP_BUILD: the orchestrator's prebuild phase deliberately
# leaves samples/godot-dotnet/ alone (it uses -c Release, which is not one of the
# Sdk's configurations, and cannot restore without the nuget.config above).
dotnet build "$SAMPLE_DIR/GDTaskSample.csproj" -c ExportRelease --nologo -v q
dotnet build "$SAMPLE_DIR/GDTaskSample.csproj" -c Debug --nologo -v q
APP="$SAMPLE_DIR/.godot/mono/temp/bin/ExportRelease/GDTaskSample.dll"
GDTASK="$SAMPLE_DIR/.godot/mono/temp/bin/ExportRelease/GDTask.dll"   # NuGet: lib/net8.0/
[ -f "$APP" ]    || { echo "FAIL: not built: $APP" >&2; exit 1; }
[ -f "$GDTASK" ] || { echo "FAIL: GDTask did not restore to $GDTASK" >&2; exit 1; }

# Content tripwire on the library itself. The transport is the network (a
# PackageReference against nuget.org), but WHAT the gate transpiles is pinned by
# hash: a re-published 3.1.0, a poisoned package cache, or an accidental version
# bump changes the IL under the assertions below and must fail loudly here, not
# show up as a mystified gap count.
gdtask_sha="$(shasum -a 256 "$GDTASK" | awk '{print $1}')"
if [ "$gdtask_sha" != "$(cat "$GDTASK_SHA_EXPECTED")" ]; then
    echo "FAIL: GDTask.dll is not the pinned 3.1.0 build" >&2
    echo "      expected: $(cat "$GDTASK_SHA_EXPECTED")  ($GDTASK_SHA_EXPECTED)" >&2
    echo "      actual:   $gdtask_sha" >&2
    echo "      A different GDTask means different IL — re-audit before re-freezing." >&2
    exit 1
fi
echo "GDTask.dll: $gdtask_sha (pinned 3.1.0)"

echo "== 3/7 Transpiling for real (tier 2, NO cuts) =="
build_proj src/Dn2Cpp.Cli/Dn2Cpp.Cli.csproj
CLI_DLL="${DN2CPP_CLI_DLL:-$PWD/src/Dn2Cpp.Cli/bin/$CONFIG/$TFM/dn2cpp.dll}"
CLI_SHA="$(shasum -a 256 "$CLI_DLL" | awk '{print $1}')"
# Pin net10.0: the highest installed runtime can be an 11.0 preview whose CoreLib
# shape skews the transpile spuriously.
CORELIB="$(resolve_net10_corelib)"
REFS=(-r "$CORELIB" -r "$GODOT_DOTNET_GODOTSHARP" -r "$GDTASK")
rm -rf "$OUT"
# No --cut. That IS the assertion: this command line is the whole engineering cost
# of running the real GDTask through dn2cpp, and if it ever needs a carve-out again,
# this line fails first.
invoke_cli "$APP" --dotnet-module "${REFS[@]}" --auto-ref \
    --no-adopt-async GDTask -o "$OUT"

# Everything past this point — the gap measurement, the native build, the engine run
# and the oracle diff — is a pure function of the transpile surface in $OUT, the CLI
# that produced it (CLI_SHA: the zero-gap assertion depends on the transpiler's
# behaviour, not only on its output, so a CLI change must miss the cache even when
# the emitted C++ is byte-identical), the corelib, the sample and its
# GDTask/GodotSharp inputs, and the pinned engine binaries. The pin/ABI tripwire
# above stays always-on, so a drifted clone still fails on a hit.
if gate_cache_check "$OUT" \
    "gdtask|nocut|cli=$CLI_SHA|corelib=$CORELIB|pin=$(file_text "$ROOT/pin.txt")|editor=$(file_sig_deref "$GODOT_DOTNET_EDITOR")|template=$(file_sig_deref "$GODOT_DOTNET_TEMPLATE")" \
    "$APP" "$GDTASK" "$GODOT_DOTNET_GODOTSHARP" "$MARKERS_EXPECTED" "$SAMPLE_DIR"; then
    { gate_cache_hit_msg; exit 0; }
fi

echo "== 4/7 ASSERT: ZERO gaps, ZERO cuts =="
# --measure drains every reachable gap into a TSV instead of throwing at the first,
# and counts the non-fatal ones too (a method that degrades to a runtime-throwing
# stub is a row here but would not fail a normal transpile). So this is a strictly
# stronger claim than "step 3 succeeded" — which is itself the second half of the
# assertion: a gap the transpiler enumerates but then happily emits past would be a
# silent miscompile, and step 3 above would have caught it by failing.
rm -rf "$MEASURE"
invoke_cli "$APP" --dotnet-module "${REFS[@]}" --auto-ref \
    --no-adopt-async GDTask --measure -o "$MEASURE" >/dev/null 2>&1 || true
# A MISSING report is not zero gaps, it is a measure run that died — and this
# gate's headline claim rides on the count. TranspileDriver writes the TSV
# unconditionally (zero rows on a clean corpus), so absence means the harness
# itself failed: bad argv, an instantiation bound, a crash. Reading that as
# "0 gaps, 0 cuts" is a vacuous pass; assert the file exists.
[ -f "$MEASURE/s0-gaps.tsv" ] \
    || { echo "FAIL: --measure produced no gap report ($MEASURE/s0-gaps.tsv) — the measure run died, which is not the same as a clean corpus" >&2; exit 1; }
gaps=$(wc -l < "$MEASURE/s0-gaps.tsv" | tr -d ' ')
if [ "$gaps" -ne 0 ]; then
    echo "FAIL: the real GDTask no longer transpiles cleanly with NO carve-outs." >&2
    echo "      $gaps gap(s):" >&2
    LC_ALL=C cut -f2,3,5 "$MEASURE/s0-gaps.tsv" | LC_ALL=C sed 's/^/        /' >&2
    echo "      This gate's claim is that GDTask needs ZERO cuts. Fix the gap in the" >&2
    echo "      transpiler; reaching for --cut here is how a carve-out nobody rechecks" >&2
    echo "      gets in. (The lever still exists for a genuinely untranspilable corner —" >&2
    echo "      see gates/build-and-run-transpiler-limits.sh — but this is not one.)" >&2
    exit 1
fi
echo "0 gaps, 0 cuts"

echo "== 5/7 Building the mono-module shared library =="
DYLIB="$OUT/$(lib_name GDTaskSample)"
compile_dotnet_module "$OUT" "$DYLIB"

echo "== 6/7 Import + export-pack + assembling the run dir =="
RUN="$OUT/run"
rm -rf "$RUN"
mkdir -p "$RUN"
# The first import may abort in editor doc-gen teardown (Godot headless bug,
# harmless — the import data is already written), so tolerate a non-zero exit.
run_with_watchdog 300 "$GODOT_DOTNET_EDITOR" --headless \
    --path "$SAMPLE_DIR" --import >"$RUN/import.log" 2>&1 || true
# A release export template rejects every path/scene CLI override, so the only
# way it finds a project is the exported-game convention <exe_basename>.pck next
# to the executable.
PCK="$PWD/$RUN/godot.pck"
if ! godot_export_step 300 "$RUN/export.log" "$PCK" \
        "$GODOT_DOTNET_EDITOR" --headless \
        --path "$SAMPLE_DIR" --export-pack gdtask-pack "$PCK"; then
    echo "FAIL: --export-pack gdtask-pack failed (see below)" >&2
    cat "$RUN/export.log" >&2
    exit 1
fi
[ -f "$PCK" ] || { echo "FAIL: export produced no $PCK" >&2; cat "$RUN/export.log" >&2; exit 1; }
# try_load_native_aot_library opens <exe_dir>/data_<AssemblyName>_<platform>_<arch>/
# <AssemblyName>.<dylib|dll|so> (no lib prefix — the NativeAOT publish naming,
# not ours). Both halves are per-platform and neither is $DN2CPP_OS: the engine
# maps Linux to "linuxbsd" (see GODOT_DOTNET_PLATFORM in _godot_dotnet.sh).
ARCH="$(godot_dotnet_host_arch)"
cp -L "$GODOT_DOTNET_TEMPLATE" "$RUN/godot$EXE_EXT"
chmod +x "$RUN/godot$EXE_EXT"
mkdir -p "$RUN/data_GDTaskSample_${GODOT_DOTNET_PLATFORM}_$ARCH"
cp -f "$DYLIB" "$RUN/data_GDTaskSample_${GODOT_DOTNET_PLATFORM}_$ARCH/GDTaskSample.$LIB_EXT"

echo "== 7/7 Running the dn2cpp native binary, and the real-.NET oracle =="
# The probe quits the tree itself once its last marker is out, so a green run
# ends deterministically; --quit-after is only the backstop for a run whose
# managed side broke before reaching the quit, and the watchdog is the backstop
# for the backstop (a Godot assert stalls the engine rather than exiting).
rc=0
run_with_watchdog 120 "$PWD/$RUN/godot$EXE_EXT" --headless --quit-after 900 \
    >"$RUN/native.log" 2>&1 || rc=$?
if [ "$rc" -ne 0 ]; then
    echo "FAIL: the native run exited $rc (137 = watchdog kill — a hung run)" >&2
    tail -40 "$RUN/native.log" >&2
    exit 1
fi
# The oracle: the SAME project, the SAME engine, on the real CoreCLR. The mono
# editor binary loads the Debug assembly built in step 2.
run_with_watchdog 120 "$GODOT_DOTNET_EDITOR" --headless \
    --path "$SAMPLE_DIR" --quit-after 900 >"$RUN/oracle.log" 2>&1 || true

grep -E "^DN2CPP_GDTASK_" "$RUN/native.log" > "$RUN/native.markers" || true
grep -E "^DN2CPP_GDTASK_" "$RUN/oracle.log" > "$RUN/oracle.markers" || true
cat "$RUN/native.markers"

# Both sides are diffed against the SAME frozen expectation. That is stronger
# than diffing them against each other: a pairwise diff passes when both are
# wrong in the same way, and it cannot tell you which side moved.
if ! diff -u "$MARKERS_EXPECTED" "$RUN/oracle.markers"; then
    echo "FAIL: the REAL-.NET oracle no longer matches $MARKERS_EXPECTED (left: expected, right: real .NET)." >&2
    echo "      dn2cpp is not implicated — the engine, GDTask, or the sample changed." >&2
    exit 1
fi
if ! diff -u "$MARKERS_EXPECTED" "$RUN/native.markers"; then
    echo "FAIL: the dn2cpp native run differs from $MARKERS_EXPECTED (left: expected, right: native)." >&2
    echo "      Real .NET, running the same project in the same engine, matched it." >&2
    exit 1
fi
# The engine keeps running (and exits 0) through a managed-side failure, only
# logging it — so the absence of errors proves nothing on its own, and these are
# checked explicitly.
for bad in "GodotPlugins initialization failed" "Failed to load hostfxr" \
        "ERROR" "SCRIPT ERROR" "ObjectDB instances leaked"; do
    if grep -q "$bad" "$RUN/native.log"; then
        echo "FAIL: the native run's log contains \"$bad\"" >&2
        exit 1
    fi
done

gate_cache_commit
echo "OK — the real GDTask 3.1.0, transpiled from its own IL with ZERO cuts,"
echo "     ran in the real engine byte-identically to real .NET."

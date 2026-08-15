#!/usr/bin/env bash
# Godot editor-export iOS E2E gate: the *forked editor* exports a C# game for
# iOS with `dotnet/export_backend = dn2cpp`, and the simulator build of the
# exported Xcode project runs the C# node in a real iPhone simulator.
#
# The iOS sibling of gates/build-and-run-godot-editor-export.sh. One headless
# `--export-debug` drives the fork's whole iOS pipeline: ExportPlugin resolves
# the target architectures and publishes the game IL ONCE, Dn2CppExporter
# transpiles it once and cmake-builds it for each of the three RIDs (ios-arm64,
# iossimulator-arm64, iossimulator-x64) with the bundled toolchain (clang
# cross-targets iOS via -arch + sysroot, so no device is needed), the simulator
# dylibs are liposed, and everything lands in {Project}_aot.xcframework inside
# the exported Xcode project — the same artifact shape a NativeAOT publish
# produces, so nothing in the engine or the export template knows dn2cpp exists.
#
# The four claims it pins down:
#   1. The exported Xcode project is self-sufficient for the simulator: built
#      unsigned with the stock xcodebuild, installed and launched on a real
#      simulator, the C# node's Console markers appear on the launch pty.
#      (GD.Print is invisible on iOS — it reaches only the platform logger —
#      so only the plain-stdout markers are asserted here; the macOS gate
#      covers the GD.Print path.)
#   2. No .NET runtime ships: no *.dll and no hostfxr anywhere in the exported
#      project tree — the device+simulator dylibs in the xcframework are the
#      only game code.
#   3. A target set the backend cannot serve is refused, and refused before
#      the publish. arm64 is the only iOS device architecture there is, so the
#      last section smuggles x86_64 in as a custom feature (get_features()
#      folds custom_features into the set ExportPlugin derives archs from) and
#      asserts the early refusal.
#   4. The three RIDs cost ONE publish and ONE transpile. This is the
#      only gate that can see it: the artifacts of a redundant transpile are
#      byte-identical to the shared one's, so nothing downstream — not the
#      xcframework, not the simulator run — is different when it regresses.
#      Section 5's marker counts are the whole oracle.
#
# Requires the artifacts of gates/setup-godot-fork.sh plus the mono iOS
# template assembled by gates/setup-godot-fork-ios.sh (its official simulator
# slice is x86_64-only and modern simulator runtimes are arm64-only, so the
# repaired template is what makes the simulator run possible at all); prints
# SKIP and exits 0 when either is absent, or without Xcode / a simulator.
# Registered in run-all-gates.sh's SERIAL Godot phase — it launches the engine,
# and every launch runs under a watchdog because a broken Godot run hangs
# rather than fails.
source "$(dirname "$0")/_common.sh"
source "$(dirname "$0")/_godot_fork.sh"

OUT=gates/out-godot-editor-export-ios
SAMPLE=samples/godot-dotnet/EditorExportSample
PROJECT_NAME=EditorExportSample
BUNDLE_ID=org.dn2cpp.editorexportsample
IOS_TEMPLATE="$FORK_ROOT/ios_template.zip"

godot_fork_preflight
if ! command -v xcodebuild >/dev/null 2>&1; then
    gate_skip "xcodebuild not found — Xcode required for the iOS editor-export gate"
fi
if [ ! -f "$IOS_TEMPLATE" ]; then
    gate_skip "no $IOS_TEMPLATE — run gates/setup-godot-fork-ios.sh"
fi
# Snapshot once, never `simctl … | grep -q` — under pipefail that reports 141 when
# grep -q exits early, so a machine WITH simulators skips blaming Xcode. Full
# reasoning at the same probe in gates/build-and-run-ios-sim-console.sh.
_sim_devices="$(xcrun simctl list devices available 2>/dev/null || true)"
if ! grep -q iPhone <<<"$_sim_devices"; then
    gate_skip "no available iPhone simulator device"
fi
# Cheap tripwire on the template itself: a stock (unrepaired) ios.zip dropped
# at the path would fail only minutes later, inside xcodebuild. Only the archs
# after lipo's colon count — the slice's own path contains "arm64"
# (ios-arm64_x86_64-simulator), which must not satisfy the check.
_tmp="$(mktemp -d)"
# `|| true`: a template with no simulator slice at all makes grep exit 1, which
# under `set -e`/`pipefail` would abort the gate instead of reaching the skip
# below — the skip path has to survive the very condition it exists to report.
# The first match is taken off a captured string rather than with `| head -1`:
# head quits after its line and the grep behind it dies of SIGPIPE, which
# `pipefail` reports as the pipeline's status (see _common.sh's note beside
# `set -o pipefail`).
_sim_entries="$(unzip -l "$IOS_TEMPLATE" 2>/dev/null | grep -oE 'libgodot\.ios\.debug\.xcframework/ios-[a-z0-9_]*-simulator/libgodot\.a' || true)"
_sim_entry="${_sim_entries%%$'\n'*}"
if [ -n "$_sim_entry" ] && unzip -q "$IOS_TEMPLATE" "$_sim_entry" -d "$_tmp" 2>/dev/null \
    && grep -q arm64 <<<"$(lipo -info "$_tmp/$_sim_entry" 2>/dev/null | LC_ALL=C sed 's/^.*: //')"; then
    rm -rf "$_tmp"
else
    rm -rf "$_tmp"
    gate_skip "$IOS_TEMPLATE has no arm64 debug simulator slice — re-run gates/setup-godot-fork-ios.sh"
fi

echo "== 1/9 Fork pin + interop-ABI tripwire =="
godot_fork_pin_abi_check

# The template is the second engine in this gate, and the arm64 probe above does
# not see its VERSION: the preset points custom_template at it and Godot
# validates that path with FileAccess::exists alone, so a zip spliced before a
# re-pin exports, runs on the simulator and matches every expectation here while
# shipping the older engine, which a re-pin has already produced once.
# Live, before gate_cache_check, for the reason the preflight is:
# `tmpl=` in the key fingerprints the stale zip itself, so a warm key replays a
# green *because* nothing changed.
godot_fork_template_check "$IOS_TEMPLATE" "iOS export template" \
    "FORCE=1 gates/setup-godot-fork-ios.sh"

# No local transpile happens here — the forked editor drives the whole pipeline
# from the packaged toolchain — so the key is inputs-only: the self-hosted CLI,
# the packaging script, the sample project and the fork's pinned binaries (pins
# + editor/template identity in the context string; the runtime/ tree the
# packaging ships is in every key already). Xcode + simulator SDK join the
# context because they build and host the exported project. The pin/ABI
# tripwire above stays always-on, so a drifted fork still fails even on
# otherwise unchanged inputs. A hit exits long before step 8/9's sim_lock, so
# a cached pass never contends for the shared simulator.
# The shared terms (pins, editor, the load-bearing GodotTools content hash) are
# godot_fork_ctx — see gates/_godot_fork.sh for why `tools=` must be a content
# hash and nothing else in this key covers the exporter.
# OUT holds the export, never a transpile output surface, so its surface term in
# the key is the `no-generated` marker — but only once OUT EXISTS. Create it here
# rather than at the staging step below: an absent OUT is unreadable, not empty,
# and gate_cache_check answers that with a warning and no key, which
# would leave this gate uncached on every fresh clone.
mkdir -p "$OUT"
if gate_cache_check "$OUT" \
    "godot-editor-export-ios|$(godot_fork_ctx)|tmpl=$(file_sig "$IOS_TEMPLATE")|xcode=$(or_none "$(first_line "$(xcodebuild -version 2>/dev/null)")")|iossdk=$(or_none "$(xcrun --sdk iphonesimulator --show-sdk-version 2>/dev/null)")" \
    "$SELFHOST_BIN" \
    dist/package-toolchain.sh \
    "$SAMPLE" \
    "$ABI_EXPECTED"; then
    { gate_cache_hit_msg; exit 0; }
fi

echo "== 2/9 Installing the working tree's toolchain into the fork editor =="
# Packaging still runs every run — that is what keeps a dn2cpp-side regression
# visible here rather than only in the next setup-godot-fork.sh. The install is
# skipped when the content already matches, and otherwise lands as an atomic
# swap under a stage lock; see stage_editor_toolchain in gates/_common.sh.
stage_editor_toolchain "$FORK_GODOTSHARP" "$SELFHOST_BIN" "$OUT/package.log"

echo "== 3/9 Staging the sample project =="
# Copied rather than exported in place: the preset needs an absolute template
# path patched in, and the project accumulates .godot/, bin/ and obj/. The work
# dir is NOT wiped — the persistent .godot/mono/dn2cpp/ slots (one per RID) are
# what keep a re-export from recompiling the runtime three times over.
PROJ="$OUT/project"
mkdir -p "$PROJ"
cp -R "$SAMPLE/." "$PROJ/"

cat > "$PROJ/nuget.config" <<EOF
<?xml version="1.0" encoding="utf-8"?>
<!-- GENERATED by gates/build-and-run-godot-editor-export-ios.sh — do not commit.
     Points the Godot.NET.Sdk resolver + restore at the fork's local feed. The
     feed path goes through godot_fork_native_path like every other gate's, a
     no-op on the only host this one runs on — kept identical so the rule has
     one shape everywhere rather than an exception nobody remembers is one. -->
<configuration>
  <packageSources>
    <clear />
    <add key="dn2cpp-godot-fork-local" value="$(godot_fork_native_path "$FORK_ROOT/nuget")" />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
  </packageSources>
</configuration>
EOF

# The iOS preset is preset.1 (the macOS gate owns preset.0); scope the patch to
# its options so preset.0's identical empty custom_template/debug stays empty.
presets_tmp="$(mktemp)"
sed "/^\[preset\.1\.options\]/,\$ s|custom_template/debug=\"\"|custom_template/debug=\"$IOS_TEMPLATE\"|" \
    "$PROJ/export_presets.cfg" > "$presets_tmp"
mv "$presets_tmp" "$PROJ/export_presets.cfg"
grep -q "ios_template.zip" "$PROJ/export_presets.cfg" \
    || { echo "FAIL: could not patch custom_template/debug into the iOS preset" >&2; exit 1; }

echo "== 4/9 Importing the project (fork editor, headless) =="
# The first import may abort in editor doc-gen teardown (Godot headless bug,
# harmless — the import data is already written), so tolerate a non-zero exit.
run_with_watchdog 600 "$FORK_EDITOR" --headless \
    --path "$PWD/$PROJ" --import >"$OUT/import.log" 2>&1 || true

echo "== 5/9 Exporting for iOS with dotnet/export_backend = dn2cpp =="
# Generous watchdog: a cold export cmake-builds the runtime, the Boehm GC and
# the vendored zlib/brotli/highway from source THREE times (device arm64 +
# both simulator arches). export_project_only makes the .ipa path merely name
# the emitted Xcode project; no signing identity is needed.
EXPORT_DIR="$PWD/$OUT/export"
rm -rf "$EXPORT_DIR"
mkdir -p "$EXPORT_DIR"
if ! godot_export_step 3600 "$OUT/export.log" \
    "$EXPORT_DIR/$PROJECT_NAME.xcodeproj/project.pbxproj" \
    "$FORK_EDITOR" --headless \
    --path "$PWD/$PROJ" --export-debug dn2cpp-ios "$EXPORT_DIR/$PROJECT_NAME.ipa"; then
    echo "FAIL: --export-debug failed (see below)" >&2
    cat "$OUT/export.log" >&2
    exit 1
fi
# An export plugin's Error fails the export, so godot_export_step above already
# asserted the exit code. This grep pins the MESSAGE: which plugin objected.
if grep -q "ERROR: Export .NET Project" "$OUT/export.log"; then
    echo "FAIL: the C# export plugin reported an error (see below)" >&2
    cat "$OUT/export.log" >&2
    exit 1
fi
# Three RIDs (ios-arm64, iossimulator-arm64, iossimulator-x64) over ONE publish
# and ONE transpile — claim 4 in the header. The publish and the transpile
# consume the game IL, which is a function of the build config and not of the
# RID; only the native compile is per-RID. Both directions are failures: three
# transpiles is the regression this exists to catch, and one native compile
# would mean two slots silently shipped another architecture's library.
#
# The reuse line is counted too, rather than being left implied by the absent
# transpile lines. Without it a Dn2CppExporter that stopped being called for the
# second and third slots at all — and so staged nothing for them — would satisfy
# a bare "transpiled once" test on its way to a broken xcframework.
assert_marker_count() {   # assert_marker_count MARKER N
    local n
    n="$(grep -cF "$1" "$OUT/export.log" || true)"
    [ "$n" -eq "$2" ] || { echo "FAIL: marker \"$1\" appeared $n times (expected $2)" >&2
                           cat "$OUT/export.log" >&2; exit 1; }
}
# The editor's own progress line, the only marker for a publish this log carries.
assert_marker_count "Running dotnet publish" 1
assert_marker_count "dn2cpp: transpiling" 1
assert_marker_count "dn2cpp: reusing the C++ transpiled" 2
assert_marker_count "dn2cpp: compiling the drop-in library" 3
assert_marker_count "dn2cpp: staged" 3

# All three slots imported the bundle's prebuilt runtime for their own axis
# The four cmake variables an iOS slot configures are four of the key's,
# so `dist/package-toolchain.sh` has to spell them exactly as
# Dn2CppExporter.BuildDropIn does — and they live in two repositories that cannot
# share a constant. A drift is fail-safe and therefore silent: the slot builds the
# runtime and its vendored trees from source, correct and ~13 s slower, and
# nothing else in this gate can tell. The line lands in the exporter's own log,
# which tees the configure's stdout; the engine's export.log never sees it.
EXPORTER_LOG="$(first_line "$(ls -t "$PROJ"/.godot/mono/temp/bin/dn2cpp/logs/export-*.log 2>/dev/null)")"
[ -n "$EXPORTER_LOG" ] || { echo "FAIL: the exporter wrote no log under .godot/mono/temp/bin/dn2cpp/logs" >&2; exit 1; }
PB_N="$(grep -cF "using the prebuilt runtime" "$EXPORTER_LOG" || true)"
[ "$PB_N" -eq 3 ] || {
    echo "FAIL: $PB_N of the 3 iOS slots imported the bundle's prebuilt runtime ($EXPORTER_LOG)" >&2
    grep -i "prebuilt" "$EXPORTER_LOG" >&2 || echo "  (the configures said nothing about a prebuilt at all)" >&2
    exit 1; }

echo "== 6/9 Asserting the exported Xcode project's artifacts =="
XCODEPROJ="$EXPORT_DIR/$PROJECT_NAME.xcodeproj"
[ -f "$XCODEPROJ/project.pbxproj" ] \
    || { echo "FAIL: no Xcode project at $XCODEPROJ" >&2; ls "$EXPORT_DIR" >&2; exit 1; }

# The Apple-embedded exporter copies the xcframework under <app>/dylibs/ and
# converts each slice's dylib into a framework on the way (App Store rule), so
# the slice binary is either {P}.dylib or {P}.framework/{P} — find the
# xcframework wherever it landed and accept both shapes.
XCFW="$(find "$EXPORT_DIR" -type d -name "${PROJECT_NAME}_aot.xcframework" -print -quit 2>/dev/null || true)"
[ -n "$XCFW" ] \
    || { echo "FAIL: no ${PROJECT_NAME}_aot.xcframework in the exported project" >&2
         find "$EXPORT_DIR" -maxdepth 3 -print >&2; exit 1; }
echo "xcframework: $XCFW"

slice_binary() {
    if [ -f "$1/$PROJECT_NAME.dylib" ]; then
        printf '%s\n' "$1/$PROJECT_NAME.dylib"
    elif [ -f "$1/$PROJECT_NAME.framework/$PROJECT_NAME" ]; then
        printf '%s\n' "$1/$PROJECT_NAME.framework/$PROJECT_NAME"
    else
        return 1
    fi
}

DEV_SLICE="$XCFW/ios-arm64"
[ -d "$DEV_SLICE" ] \
    || { echo "FAIL: the xcframework has no ios-arm64 (device) slice" >&2; ls "$XCFW" >&2; exit 1; }
SIM_SLICE="$(first_line "$(ls -d "$XCFW"/ios-*-simulator 2>/dev/null || true)")"
[ -n "$SIM_SLICE" ] \
    || { echo "FAIL: the xcframework has no simulator slice" >&2; ls "$XCFW" >&2; exit 1; }

DEV_BIN="$(slice_binary "$DEV_SLICE")" \
    || { echo "FAIL: no game library in the device slice" >&2; ls -R "$DEV_SLICE" >&2; exit 1; }
SIM_BIN="$(slice_binary "$SIM_SLICE")" \
    || { echo "FAIL: no game library in the simulator slice" >&2; ls -R "$SIM_SLICE" >&2; exit 1; }
# grep without -q so it drains nm's output: under `set -o pipefail`, `grep -q`
# exits on the first match, nm dies of SIGPIPE, and the pipeline reports a bogus
# failure.
for bin in "$DEV_BIN" "$SIM_BIN"; do
    nm -gU "$bin" | grep "godotsharp_game_main_init" >/dev/null \
        || { echo "FAIL: $bin does not export godotsharp_game_main_init" >&2; exit 1; }
done
# The simulator library is the lipo of the two simulator publishes.
sim_archs="$(lipo -archs "$SIM_BIN")"
for a in arm64 x86_64; do
    grep -q "$a" <<<"$sim_archs" \
        || { echo "FAIL: the simulator slice lacks $a (archs: $sim_archs)" >&2; exit 1; }
done
echo "device: $DEV_BIN; simulator: $SIM_BIN ($sim_archs)"

# GDMono only falls through to try_load_native_aot_library when it finds neither
# hostfxr nor coreclr next to the game. A stray .NET runtime or assembly in the
# exported tree would silently route the game back to .NET on-device and make
# the rest of this gate prove nothing.
stray="$(find "$EXPORT_DIR" \( -iname '*.dll' -o -iname '*hostfxr*' \) 2>/dev/null || true)"
if [ -n "$stray" ]; then
    echo "FAIL: the exported project tree contains .NET runtime remnants:" >&2
    printf '%s\n' "$stray" >&2
    exit 1
fi

echo "== 7/9 Building the exported project for the iOS simulator (xcodebuild) =="
# Legacy -target/-sdk form: the exported scheme declares only the iphoneos
# platform, so scheme/destination resolution finds no simulator destination,
# while a plain -sdk iphonesimulator -arch arm64 build needs none.
# CODE_SIGNING_ALLOWED=NO — simulator builds run unsigned. The target is read
# off the project rather than assumed, with the project name as the fallback.
TARGET="$(first_line "$(xcodebuild -list -project "$XCODEPROJ" 2>/dev/null \
    | awk '/Targets:/{f=1; next} f && NF {gsub(/^[ \t]+/, ""); print}' || true)")"
[ -n "$TARGET" ] || TARGET="$PROJECT_NAME"
echo "target: $TARGET"
# Through xcodebuild_bounded (gates/_common.sh) for the SWBBuildService wedge:
# the build service stops inside its compiler-macro probe, before it reads a
# source file, so it is nothing about this project and nothing this gate can
# tell apart from a hang. Same command, same exposure as
# gates/build-and-run-godot-ios-sim.sh, where it was caught and sampled.
if ! xcodebuild_bounded "$OUT/xcodebuild.log" -project "$XCODEPROJ" -target "$TARGET" \
    -configuration Debug -sdk iphonesimulator -arch arm64 \
    CODE_SIGNING_ALLOWED=NO \
    CONFIGURATION_BUILD_DIR="$EXPORT_DIR/simbuild" build; then
    echo "FAIL: xcodebuild (simulator) failed or wedged on every attempt (tail below; full log: $OUT/xcodebuild.log)" >&2
    tail -60 "$OUT/xcodebuild.log" >&2
    exit 1
fi
APP="$EXPORT_DIR/simbuild/$TARGET.app"
if [ ! -d "$APP" ]; then
    APP="$(first_line "$(ls -d "$EXPORT_DIR/simbuild"/*.app 2>/dev/null || true)")"
fi
[ -n "$APP" ] && [ -d "$APP" ] \
    || { echo "FAIL: no app bundle under $EXPORT_DIR/simbuild" >&2; ls "$EXPORT_DIR/simbuild" >&2; exit 1; }

echo "== 8/9 Running on the simulator (simctl launch, watchdog 240s) =="
# The booted simulator is shared with godot-ios-sim, which may be running
# concurrently in another Phase 5 chain — serialize the launch window.
sim_lock
UDID=$(ensure_booted_sim)
echo "simulator: $UDID"
xcrun simctl uninstall "$UDID" "$BUNDLE_ID" >/dev/null 2>&1 || true
xcrun simctl install "$UDID" "$APP"
# The probe quits itself after one _Process frame, so launch returns; the alarm
# is the safety net for a hung engine (assert failures do not exit). Under a
# saturated machine (the parallel Phase 5 chains compiling and running engines
# alongside), SpringBoard's own launch watchdog can kill the app before the
# engine finishes starting — the launch then returns with an empty pty. That
# is a load artifact, not a product failure (the same build passes solo), so
# retry the launch a bounded number of times; each attempt rewrites SIM_LOG,
# keeping the exactly-once marker asserts below meaningful.
SIM_LOG="$OUT/sim-run.log"
for _launch_attempt in 1 2 3; do
    perl -e 'alarm 240; exec @ARGV' xcrun simctl launch --console-pty "$UDID" "$BUNDLE_ID" \
        >"$SIM_LOG" 2>&1 || true
    xcrun simctl terminate "$UDID" "$BUNDLE_ID" >/dev/null 2>&1 || true
    grep -q "DN2CPP_EXPORT_READY" "$SIM_LOG" && break
    echo "note: simulator launch attempt $_launch_attempt produced no marker — retrying"
done
sim_unlock
cat "$SIM_LOG"
# The scripted node was constructed, tied to its native object (the scene's
# baked Answer override arrived through the instance Set bridge), and driven by
# the engine's main loop until it quit itself. Only the Console.WriteLine
# markers are asserted: on iOS GD.Print reaches only the platform logger, which
# the launch pty cannot observe — DN2CPP_EXPORT_GDPRINT belongs to the macOS gate.
for marker in \
    "DN2CPP_EXPORT_READY name=Main answer=42" \
    "DN2CPP_EXPORT_PROCESS class=Node inTree=True deltaOk=True" \
    "DN2CPP_EXPORT_GC finalized=True bounded=True" \
    "DN2CPP_EXPORT_INTEROP resizeOk=True sized=True" \
    "DN2CPP_EXPORT_SIGNAL awaited=True" \
    "DN2CPP_EXPORT_CLOCK ticks=True elapsed=True" \
    "DN2CPP_EXPORT_DONE"; do
    n="$(grep -cF "$marker" "$SIM_LOG" || true)"
    [ "$n" -eq 1 ] || { echo "FAIL: marker \"$marker\" appeared $n times (expected exactly 1)" >&2; exit 1; }
done

echo "== 9/9 Refusing an architecture set the backend cannot serve =="
# arm64 is the only iOS device architecture there is, and it is all the backend
# builds. The exporter derives the publish architectures from the preset's
# *features*, and get_features() folds custom_features into that set — so a
# custom feature naming x86_64 turns the target set into {arm64, x86_64}, which
# Dn2CppExporter.Create refuses for iOS. The refusal must land before the
# publish: no transpile marker in the log.
BAD_DIR="$PWD/$OUT/export-foreign-arch"
BAD_LOG="$OUT/export-foreign-arch.log"
rm -rf "$BAD_DIR"
mkdir -p "$BAD_DIR"

presets_tmp="$(mktemp)"
sed "/^\[preset\.1\]/,\$ s|custom_features=\"\"|custom_features=\"x86_64\"|" \
    "$PROJ/export_presets.cfg" > "$presets_tmp"
mv "$presets_tmp" "$PROJ/export_presets.cfg"
grep -q "custom_features=\"x86_64\"" "$PROJ/export_presets.cfg" \
    || { echo "FAIL: could not patch custom_features into the iOS preset" >&2; exit 1; }

# A regression here does a publish and three native builds before it can go
# wrong, so the watchdog doubles as the budget for "refused early".
bad_arch_rc=0
run_with_watchdog 600 "$FORK_EDITOR" --headless \
    --path "$PWD/$PROJ" --export-debug dn2cpp-ios "$BAD_DIR/$PROJECT_NAME.ipa" \
    >"$BAD_LOG" 2>&1 || bad_arch_rc=$?

# The refusal is an AddMessage(Error), so it fails the export like any other.
[ "$bad_arch_rc" -ne 0 ] \
    || { echo "FAIL: the export was refused but exited 0" >&2; cat "$BAD_LOG" >&2; exit 1; }
grep -qE "Project export for preset .* failed\." "$BAD_LOG" \
    || { echo "FAIL: the editor did not report the refused export as failed" >&2
         cat "$BAD_LOG" >&2; exit 1; }
grep -q "ERROR: Export .NET Project" "$BAD_LOG" \
    || { echo "FAIL: the export plugin accepted an {arm64, x86_64} iOS target" >&2
         cat "$BAD_LOG" >&2; exit 1; }
grep -qF "supports only the arm64 device architecture on iOS" "$BAD_LOG" \
    || { echo "FAIL: the export failed, but not on the iOS architecture guard" >&2
         cat "$BAD_LOG" >&2; exit 1; }
if grep -qF "dn2cpp: transpiling" "$BAD_LOG"; then
    echo "FAIL: the architecture guard fired only after the publish had run" >&2
    exit 1
fi

# Tripwire: a concurrent suite re-staging the shared toolchain mid-run must
# surface as this self-explaining FAIL, not as a cryptic include error inside
# the export above.
assert_editor_toolchain_current "$FORK_GODOTSHARP"
gate_cache_commit
echo "OK"

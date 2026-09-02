#!/usr/bin/env bash
# Godot iOS simulator E2E gate: the exported Xcode project (see
# build-and-run-godot-ios-export.sh) is built for the iOS simulator with
# xcodebuild, installed on a booted simulator, and launched; the C# node's
# _Ready/_Notification markers on stdout prove the transpiled GDExtension runs
# inside the real engine on iOS. The injector scene script quits the app after
# a few frames (mobile has no --quit-after), so `simctl launch --console-pty`
# returns; a watchdog still bounds the run (a pre-fix red would otherwise hang
# — Godot asserts do not exit).
#
# The xcodebuild step uses the legacy -target/-sdk form: the exported scheme
# declares only the iphoneos platform, so scheme/destination resolution finds
# no simulator destination, while a plain -sdk iphonesimulator -arch arm64
# build needs none. CODE_SIGNING_ALLOWED=NO — simulator builds run unsigned.
#
# Skips when Xcode, the Godot iOS template, or an iPhone simulator is missing —
# and also when the installed template's simulator slice lacks arm64: the
# official 4.6.x-4.7.x templates ship it x86_64-only (godot#118161) and iOS 26+
# simulator runtimes cannot boot under Rosetta, so the E2E is only possible
# after gates/setup-ios-sim-template.sh repairs the template. Every skip is
# reported as a SKIP, never as a pass — see gate_skip in _common.sh.
source "$(dirname "$0")/_common.sh"

if ! command -v xcodebuild >/dev/null 2>&1; then
    gate_skip "xcodebuild not found — Xcode required for the iOS simulator gate"
fi
if ! command -v "${GODOT:-godot}" >/dev/null 2>&1; then
    gate_skip "godot not found (set GODOT to the engine binary)"
fi
# `|| true` on the two probes below: _common.sh has enabled `set -e`/`pipefail`
# by now, and both legitimately produce nothing (a failed version probe; a grep
# that matches no simulator slice). Each must fall through to its skip, not
# abort the gate. godot_template_version_dir (in _common.sh) keeps the ".mono"
# flavor suffix a mono editor's template directory carries.
_godot_ver="$(godot_template_version_dir || true)"
_ios_zip="$HOME/Library/Application Support/Godot/export_templates/$_godot_ver/ios.zip"
if [ ! -f "$_ios_zip" ]; then
    gate_skip "Godot iOS export template not installed for $_godot_ver — run gates/setup-ios-export.sh"
fi
# Snapshot once, never `simctl … | grep -q` — under pipefail that reports 141 when
# grep -q exits early, so a machine WITH simulators skips blaming Xcode. Full
# reasoning at the same probe in gates/build-and-run-ios-sim-console.sh.
_sim_devices="$(xcrun simctl list devices available 2>/dev/null || true)"
if ! grep -q iPhone <<<"$_sim_devices"; then
    gate_skip "no available iPhone simulator device"
fi
_tmp="$(mktemp -d)"
# First match taken off a captured string, not `| head -1`: head quits after
# its line and the grep behind it dies of SIGPIPE, which `pipefail` reports as
# the pipeline's status (see _common.sh, beside `set -o pipefail`).
_sim_entries="$(unzip -l "$_ios_zip" 2>/dev/null | grep -oE 'libgodot\.ios\.debug\.xcframework/ios-[a-z0-9_]*-simulator/libgodot\.a' || true)"
_sim_entry="${_sim_entries%%$'\n'*}"
# Only the archs after the colon count — the slice's file path itself contains
# "arm64" (ios-arm64_x86_64-simulator), which must not satisfy the check.
if [ -n "$_sim_entry" ] && unzip -q "$_ios_zip" "$_sim_entry" -d "$_tmp" 2>/dev/null \
    && grep -q arm64 <<<"$(lipo -info "$_tmp/$_sim_entry" 2>/dev/null | LC_ALL=C sed 's/^.*: //')"; then
    rm -rf "$_tmp"
else
    rm -rf "$_tmp"
    gate_skip "the installed iOS template's simulator slice has no arm64 (godot#118161) — repair it with gates/setup-ios-sim-template.sh"
fi

OUT=artifacts/godot-ios-sim
GODOT=${GODOT:-godot}
PROJECT=samples/godot/godot-project
EXPORT_DIR=artifacts/godot-ios-sim-export
XCFW="$PROJECT/bin/lib$DN2CPP_GDEXT_LIB.ios.xcframework"
BUNDLE_ID=org.dn2cpp.godotsample

echo "== 1/6 Building sample C# assembly =="
build_proj samples/godot/GodotSample/GodotSample.csproj

echo "== 2/6 Transpiling IL -> C++ (GDExtension mode, shim + tree-shaken CoreLib) =="
# The real CoreLib rides along for the sample's Task.Run pool probe, as in
# build-and-run-godot-sample.sh (the generated C++ is target-agnostic).
corelib=$(locate_corelib)
invoke_cli \
    "samples/godot/GodotSample/bin/$CONFIG/$TFM/GodotSample.dll" \
    -r "samples/godot/GodotSample/bin/$CONFIG/$TFM/GodotSharp.dll" \
    -r "$corelib" \
    -o "$OUT" --gdextension --direct-pinvoke '*'

# Cache: beyond the generated output + inputs, the simulator run also depends
# on the Xcode toolchain, the iOS SDK, and the Godot iOS export template, so
# those identities ride in the context (template keyed by stat — the zip is
# huge and versioned per Godot release). A hit exits here, well before the
# step-6 sim_lock, so cached runs never contend for the shared simulator.
if gate_cache_check "$OUT" \
        "godot-ios-sim|godot=$(or_none "$(first_line "$("$GODOT" --version 2>/dev/null)")")|xcode=$(or_none "$(first_line "$(xcodebuild -version 2>/dev/null)")")|iossdk=$(or_none "$(xcrun --sdk iphonesimulator --show-sdk-version 2>/dev/null)")|tmpl=$(file_sig "$_ios_zip")" \
        "samples/godot/GodotSample/bin/$CONFIG/$TFM/GodotSample.dll" \
        "samples/godot/GodotSample/bin/$CONFIG/$TFM/GodotSharp.dll" \
        "$PROJECT"; then
    gate_cache_hit_msg
    exit 0
fi

echo "== 3/6 Compiling the iOS GDExtension xcframework =="
mkdir -p "$PROJECT/bin"
compile_gdextension_ios "$OUT" "$XCFW"

echo "== 4/6 Exporting the iOS Xcode project =="
"$GODOT" --headless --path "$PROJECT" --import >/dev/null 2>&1 || true
rm -rf "$EXPORT_DIR"
mkdir -p "$EXPORT_DIR"
"$GODOT" --headless --path "$PROJECT" --export-debug "iOS" \
    "$PWD/$EXPORT_DIR/godotsample.ipa" >/dev/null 2>&1 || true
[ -f "$EXPORT_DIR/godotsample.xcodeproj/project.pbxproj" ] \
    || { echo "FAIL: iOS export produced no Xcode project" >&2; exit 1; }

echo "== 5/6 Building the app for the iOS simulator (xcodebuild) =="
# Through xcodebuild_bounded, because this step WEDGES rather than fails, and
# the wedge is Xcode's build service rather than anything about this project:
# it stops before reading a source file, inside the compiler-macro probe SWB
# runs while building the build description. Measured on the first runs this
# gate ever had (until then a broken simulator probe skipped it — see the
# iPhone check above); the sampled process tree, the pipe arithmetic behind it
# and the three separate causes measured so far are written out at
# xcodebuild_bounded in gates/_common.sh. Two of the three are fixed there. The
# third is a machine-wide kernel pipe-buffer budget that no setting in this
# repository can raise, so a red here may be the HOST rather than the tree —
# the failure below carries that measurement and its remedy, and it is worth
# reading before looking for a regression. The unbounded form here was a hang of
# the entire Phase-5 chain, holding the machine lock, with nothing to read
# afterwards.
if ! xcodebuild_bounded "$OUT/xcodebuild.log" \
    -project "$EXPORT_DIR/godotsample.xcodeproj" -target godotsample \
    -configuration Debug -sdk iphonesimulator -arch arm64 \
    CODE_SIGNING_ALLOWED=NO \
    CONFIGURATION_BUILD_DIR="$PWD/$EXPORT_DIR/simbuild" build; then
    echo "FAIL: xcodebuild (simulator) failed or wedged on every attempt (tail below; full log: $OUT/xcodebuild.log)" >&2
    tail -40 "$OUT/xcodebuild.log" >&2
    exit 1
fi
APP="$EXPORT_DIR/simbuild/godotsample.app"
[ -d "$APP" ] || { echo "FAIL: no app bundle at $APP" >&2; exit 1; }

echo "== 6/6 Running on the simulator (simctl launch, watchdog 180s) =="
# The booted simulator is shared with godot-editor-export-ios, which may be
# running concurrently in another Phase 5 chain — serialize the launch window.
sim_lock
UDID=$(ensure_booted_sim)
echo "simulator: $UDID"
xcrun simctl uninstall "$UDID" "$BUNDLE_ID" >/dev/null 2>&1 || true
xcrun simctl install "$UDID" "$APP"
# The injector script quits the app after ~30 frames, so launch returns; the
# alarm is the safety net for a hung engine (assert failures do not exit).
# Under a saturated machine (parallel Phase 5 chains), SpringBoard's own
# launch watchdog can kill the app before the engine finishes starting and
# the launch returns with an empty pty — a load artifact, not a product
# failure, so retry the launch a bounded number of times.
for _launch_attempt in 1 2 3; do
    SIM_OUT=$(perl -e 'alarm 180; exec @ARGV' xcrun simctl launch --console-pty "$UDID" "$BUNDLE_ID" 2>&1) || true
    xcrun simctl terminate "$UDID" "$BUNDLE_ID" >/dev/null 2>&1 || true
    grep -q "\[dn2cpp\] GDExtension initialized" <<<"$SIM_OUT" && break
    echo "note: simulator launch attempt $_launch_attempt produced no marker — retrying"
done
sim_unlock
echo "$SIM_OUT"

# On iOS the engine routes GD.Print through platform loggers that reach
# neither the launch pty nor (observably) the simulator's unified log — so the
# assertions ride on plain-stdout markers instead: the runtime's extension
# banner, and a Console-path line MyNode._Ready prints with an engine-call
# result (C# executed in-scene + a ptrcall round trip, on the simulator).
grep -q "\[dn2cpp\] GDExtension initialized" <<<"$SIM_OUT" \
    || { echo "FAIL: dn2cpp GDExtension init banner missing on iOS" >&2; exit 1; }
grep -q "MyNode._Ready stdout marker: GetClass() = MyNode" <<<"$SIM_OUT" \
    || { echo "FAIL: MyNode._Ready stdout marker missing on iOS (C# scene code did not run)" >&2; exit 1; }
gate_cache_commit
echo "OK"

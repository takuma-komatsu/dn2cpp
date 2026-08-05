#!/usr/bin/env bash
# Godot iOS export gate: C# -> IL -> C++ -> iOS GDExtension .xcframework ->
# `godot --export-debug "iOS"` -> Xcode project. This is the dn2cpp equivalent
# of Unity/IL2CPP's "build an Xcode project for iOS": the transpiled game code
# rides in the xcframework, and Godot's export (application/export_project_only
# in export_presets.cfg) emits the wrapping Xcode project without needing a
# signing identity or a provisioning profile.
#
# Asserts the durable outputs, not exporter internals: the .xcodeproj exists,
# the pbxproj references the engine + extension binaries, and the dn2cpp
# xcframework (both SDK slices) plus the project data (.pck or expanded dir)
# are present in the exported tree. Building/running the project on a
# simulator is the next gate (build-and-run-godot-ios-sim.sh).
#
# Machines without the Xcode toolchain or the Godot iOS export template skip
# (install the template with gates/setup-ios-export.sh). A skip is reported as a
# SKIP, never as a pass — see gate_skip in _common.sh.
source "$(dirname "$0")/_common.sh"

if ! command -v xcodebuild >/dev/null 2>&1; then
    gate_skip "xcodebuild not found — Xcode required for the iOS export gate"
fi
if ! command -v "${GODOT:-godot}" >/dev/null 2>&1; then
    gate_skip "godot not found (set GODOT to the engine binary)"
fi
# `|| true`: _common.sh has enabled `set -e`/`pipefail` by now, and a version
# probe that fails must fall through to the template check's skip, not abort.
# godot_template_version_dir (in _common.sh) keeps the ".mono" flavor suffix a
# mono editor's template directory carries.
_godot_ver="$(godot_template_version_dir || true)"
_ios_zip="$HOME/Library/Application Support/Godot/export_templates/$_godot_ver/ios.zip"
if [ ! -f "$_ios_zip" ]; then
    gate_skip "Godot iOS export template not installed for $_godot_ver — run gates/setup-ios-export.sh"
fi

OUT=artifacts/godot-ios
GODOT=${GODOT:-godot}
PROJECT=samples/godot/godot-project
EXPORT_DIR=artifacts/godot-ios-export
XCFW="$PROJECT/bin/libgodotsample.ios.xcframework"

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
    -o "$OUT" --gdextension

# Cache: beyond the generated output + inputs, the exported Xcode project also
# depends on the Xcode toolchain, the iOS SDK, and the Godot iOS export
# template — so those identities ride in the context. The template is keyed by
# stat (size + mtime), not content: the zip is huge and versioned per Godot
# release, so a content hash would cost more than it protects.
if gate_cache_check "$OUT" \
        "godot-ios-export|godot=$(or_none "$(first_line "$("$GODOT" --version 2>/dev/null)")")|xcode=$(or_none "$(first_line "$(xcodebuild -version 2>/dev/null)")")|iossdk=$(or_none "$(xcrun --sdk iphonesimulator --show-sdk-version 2>/dev/null)")|tmpl=$(file_sig "$_ios_zip")" \
        "samples/godot/GodotSample/bin/$CONFIG/$TFM/GodotSample.dll" \
        "samples/godot/GodotSample/bin/$CONFIG/$TFM/GodotSharp.dll" \
        "$PROJECT"; then
    gate_cache_hit_msg
    exit 0
fi

echo "== 3/6 Compiling the iOS GDExtension xcframework (device + simulator) =="
mkdir -p "$PROJECT/bin"
compile_gdextension_ios "$OUT" "$XCFW"
[ -d "$XCFW/ios-arm64" ] || { echo "FAIL: xcframework has no ios-arm64 (device) slice" >&2; exit 1; }
ls -d "$XCFW"/ios-*-simulator >/dev/null 2>&1 || { echo "FAIL: xcframework has no simulator slice" >&2; exit 1; }

echo "== 4/6 Importing Godot project =="
# Note: the first import may abort during editor teardown (Godot headless
# doc-gen bug; harmless — the import data is already written).
"$GODOT" --headless --path "$PROJECT" --import >/dev/null 2>&1 || true

echo "== 5/6 Exporting the iOS Xcode project (godot --export-debug \"iOS\") =="
rm -rf "$EXPORT_DIR"
mkdir -p "$EXPORT_DIR"
# The output basename names the generated Xcode project; with
# export_project_only no .ipa is produced.
EXPORT_OUT=$("$GODOT" --headless --path "$PROJECT" --export-debug "iOS" \
    "$PWD/$EXPORT_DIR/godotsample.ipa" 2>&1) || {
    echo "$EXPORT_OUT"
    echo "FAIL: godot --export-debug \"iOS\" failed" >&2
    exit 1
}
echo "$EXPORT_OUT" | tail -5

echo "== 6/6 Asserting the exported Xcode project =="
# Layout (Godot 4.6-4.7): the engine xcframework is renamed after the app
# (godotsample.xcframework, libgodot.a per SDK slice); the extension dylibs are
# converted to per-slice .frameworks under <app>/dylibs/; the project data is
# <app>.pck next to the .xcodeproj.
PBXPROJ="$EXPORT_DIR/godotsample.xcodeproj/project.pbxproj"
[ -f "$PBXPROJ" ] || { echo "FAIL: no Xcode project at $PBXPROJ" >&2; exit 1; }
grep -q "libgodotsample.ios.xcframework" "$PBXPROJ" \
    || { echo "FAIL: pbxproj does not reference the dn2cpp extension xcframework" >&2; exit 1; }
[ -f "$EXPORT_DIR/godotsample.xcframework/ios-arm64/libgodot.a" ] \
    || { echo "FAIL: no engine static library (godotsample.xcframework/ios-arm64/libgodot.a)" >&2; exit 1; }
# Captured and tested for emptiness, never `… | grep -q .`: grep -q matches on
# line 1 of everything, the find behind it dies of SIGPIPE and `pipefail` makes
# that the pipeline's status (see _common.sh, beside `set -o pipefail`).
# `|| true` keeps the FAIL line below reachable: with the dylibs directory
# missing entirely find exits 1, and an unguarded substitution would abort
# under `set -e` before the message that says what is wrong.
_fw="$(find "$EXPORT_DIR/godotsample/dylibs" -type d -name "libgodotsample.framework" 2>/dev/null || true)"
[ -n "$_fw" ] \
    || { echo "FAIL: the dn2cpp extension frameworks are not embedded in the exported tree" >&2; exit 1; }
[ -f "$EXPORT_DIR/godotsample.pck" ] \
    || { echo "FAIL: no project data pack (godotsample.pck)" >&2; exit 1; }
echo "Xcode project exported: $EXPORT_DIR/godotsample.xcodeproj"
gate_cache_commit
echo "OK"

#!/usr/bin/env bash
# SETUP AID (NOT a regression gate — the name is outside the `build-and-run-*.sh`
# glob, so run-all-gates.sh ignores it; the sibling of gates/setup-godot-dotnet.sh).
# Installs the Godot iOS export template the godot-ios gates need:
#
#   1. download the official export-templates .tpz for the installed Godot
#      version (~1.2 GB, cached under ~/.cache/dn2cpp-ios-export/)
#   2. extract just templates/ios.zip into the editor's template search path
#      (~/Library/Application Support/Godot/export_templates/<version>/ios.zip)
#   3. inspect the template's simulator slice: the ios-arm64_x86_64-simulator
#      libgodot.a shipped broken in 4.6.2 (metadata claims arm64 but the archive
#      is x86_64-only — godotengine/godot#118161, still present through 4.7),
#      which prevents native simulator builds on Apple-Silicon hosts (iOS 26+
#      simulator runtimes cannot boot under Rosetta). The result is reported,
#      not fatal: exporting the Xcode project and building for a device are
#      unaffected; a repaired arm64 slice can be produced with
#      gates/setup-ios-sim-template.sh.
#
# Idempotent: the download is skipped when the cached .tpz is there and NON-EMPTY,
# the install when ios.zip is; the slice inspection always runs and reports. Both
# products are staged through a .part file that is verified non-empty before the
# atomic rename, the pattern the Android sibling (gates/setup-android-export.sh)
# carries: these are ~1.2 GB over a real network, and a truncated download or a
# partial extract left where a file test reads it as complete is handed downstream
# as an installed template. The .tpz download is deliberately kept out of the gates
# themselves — machines without the template just skip the godot-ios gates green.
#
# Usage:
#   ./gates/setup-ios-export.sh [TPZ_PATH]
#
#   TPZ_PATH   an already-downloaded export-templates .tpz to install from
#              (skips the download)
set -euo pipefail

# _common.sh supplies godot_template_version_dir — the ONE parser of the
# editor's version string, shared with the gates' template probes, so the
# installer and the detectors cannot drift apart on the directory name.
# Sourcing it cd's to the repo root; restore the invocation cwd so a relative
# TPZ_PATH argument keeps resolving where the caller stood.
_invoke_pwd="$PWD"
source "$(dirname "$0")/_common.sh"
cd "$_invoke_pwd"

GODOT=${GODOT:-godot}
CACHE="${DN2CPP_IOS_EXPORT_CACHE:-$HOME/.cache/dn2cpp-ios-export}"

command -v "$GODOT" >/dev/null 2>&1 || { echo "error: godot not found on PATH" >&2; exit 1; }
command -v lipo >/dev/null 2>&1 || { echo "error: lipo not found (Xcode command-line tools required)" >&2; exit 1; }

echo "== 1/4 Resolving the Godot template version =="
# Parsed by godot_template_version_dir (gates/_common.sh — shared with the
# ios/android gates' template probes):
#   "4.6.3.stable.official.7d41c59c4"       -> dir "4.6.3.stable",       tag "4.6.3-stable" (vanilla tpz)
#   "4.7.stable.official.<sha>"             -> dir "4.7.stable",         tag "4.7-stable"   (vanilla tpz)
#   "4.7.stable.mono.custom_build.<sha>"    -> dir "4.7.stable.mono",    tag "4.7-stable"   (mono tpz)
# The template dir Godot's editor searches includes ".mono" for mono flavors,
# but the godot-builds release tag never does — the flavor is a tpz filename
# variant (Godot_v<tag>_mono_export_templates.tpz) rather than a tag suffix.
godot_template_version_dir >/dev/null \
    || { echo "error: cannot parse Godot version: $GODOT_FULL_VERSION" >&2; exit 1; }
full_version="$GODOT_FULL_VERSION"
version="$GODOT_TEMPLATE_VERSION_DIR"
tag="$GODOT_RELEASE_TAG"
flavor="$GODOT_VERSION_FLAVOR"
if [ -n "$flavor" ]; then
    tpz_name="Godot_v${tag}_mono_export_templates.tpz"
else
    tpz_name="Godot_v${tag}_export_templates.tpz"
fi
templates_dir="$HOME/Library/Application Support/Godot/export_templates/$version"
echo "godot:    $full_version"
echo "version:  $version (release tag $tag${flavor:+, mono variant})"
echo "install:  $templates_dir/ios.zip"

echo "== 2/4 Obtaining the export-templates .tpz =="
# An explicit argument must name an EXISTING tpz: treating a mistyped path as
# the download destination would silently turn a typo into a ~1.2 GB download to
# the wrong place. Only the no-argument default path may download. (The Android
# sibling states the same rule at the same step.)
if [ -n "${1:-}" ] && [ ! -f "$1" ]; then
    echo "error: tpz argument is not a file: $1" >&2
    exit 1
fi
tpz="${1:-$CACHE/$tpz_name}"
if [ -s "$tpz" ]; then
    echo "skip: using existing $tpz"
else
    url="https://github.com/godotengine/godot-builds/releases/download/$tag/$tpz_name"
    echo "downloading $url"
    mkdir -p "$(dirname "$tpz")"
    curl -fL --progress-bar -o "$tpz.part" "$url"
    # Verified BEFORE the atomic rename, for the reason step 3 states: curl -f
    # catches an HTTP error, but a zero-length 200 answered by a proxy is a
    # successful transfer, and renaming it makes every later run skip the
    # download and hand the empty file to unzip.
    [ -s "$tpz.part" ] \
        || { echo "error: downloaded $tpz_name is empty (bad mirror?)" >&2
             rm -f "$tpz.part"; exit 1; }
    mv "$tpz.part" "$tpz"
fi

echo "== 3/4 Installing templates/ios.zip =="
if [ -s "$templates_dir/ios.zip" ]; then
    echo "skip: already installed: $templates_dir/ios.zip"
else
    mkdir -p "$templates_dir"
    # A .tpz is a zip with a top-level templates/ folder. Extract to a .part
    # file and verify BEFORE the atomic rename — the same three lines the
    # Android sibling (gates/setup-android-export.sh's step 3) carries, and for
    # the same reason: the tpz is ~1.2 GB over a real network, so a truncated
    # download or a partial extract must never be left where a gate's file test
    # would read it as a complete, installed template.
    unzip -p "$tpz" templates/ios.zip > "$templates_dir/ios.zip.part"
    [ -s "$templates_dir/ios.zip.part" ] \
        || { echo "error: extracted ios.zip is empty (bad tpz?)" >&2
             rm -f "$templates_dir/ios.zip.part"; exit 1; }
    mv "$templates_dir/ios.zip.part" "$templates_dir/ios.zip"
    echo "installed: $templates_dir/ios.zip"
fi

echo "== 4/4 Inspecting the simulator slice (godot#118161 check) =="
tmp="$(mktemp -d)"
trap 'rm -rf "$tmp"' EXIT
sim_lib="libgodot.ios.debug.xcframework/ios-arm64_x86_64-simulator/libgodot.a"
unzip -q -o "$templates_dir/ios.zip" "$sim_lib" -d "$tmp"
archs="$(lipo -info "$tmp/$sim_lib" | LC_ALL=C sed 's/^.*: //')"
echo "simulator slice archs: $archs"
if grep -q arm64 <<<"$archs"; then
    echo "OK: the simulator slice contains arm64 — native simulator builds work on Apple Silicon"
else
    echo "WARNING: the simulator slice is x86_64-only (godot#118161 not fixed in this"
    echo "         template) — the simulator E2E gate skips green on Apple-Silicon"
    echo "         hosts (iOS 26+ simulators cannot boot under Rosetta) until the"
    echo "         template is repaired with gates/setup-ios-sim-template.sh."
    echo "         Device + Xcode-project export are unaffected."
fi

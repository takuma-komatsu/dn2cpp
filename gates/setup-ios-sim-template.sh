#!/usr/bin/env bash
# SETUP AID (NOT a regression gate). Repairs the installed Godot iOS export
# template's broken simulator slice: the official 4.6.x-4.7.x templates ship
# an ios-arm64_x86_64-simulator libgodot.a that is actually x86_64-only
# (godotengine/godot#118161), and iOS 26+ simulator runtimes are arm64-only
# (Rosetta simulator boots were removed) — so a simulator run of an exported
# project is impossible with the stock template on today's toolchain.
#
# The fix: build the engine's simulator library from source at the matching
# tag and rebuild the template's debug xcframework with a real arm64 slice.
#
#   1. locate a godotengine/godot checkout at the tag matching the installed
#      editor (pass its path, e.g. a `git worktree` of your clone at
#      4.7-stable — this script never touches the checkout's git state)
#   2. scons platform=ios target=template_debug arch=arm64 ios_simulator=yes
#      (skipped when the product carries a provenance stamp naming those
#      settings — its name cannot; tens of minutes cold)
#   3. re-create libgodot.ios.debug.xcframework inside the installed ios.zip:
#      the device slice is kept from the template, the simulator slice is the
#      freshly built arm64 library
#   4. re-run the arch check gates/setup-ios-export.sh performs
#
# Only the DEBUG xcframework is rebuilt: `--export-debug "iOS"` (what the
# godot-ios gates run) never touches the release one.
#
# Usage:
#   ./gates/setup-ios-sim-template.sh <godot-source-checkout>
set -euo pipefail

# _common.sh supplies godot_template_version_dir — the ONE parser of the
# editor's version string, shared with the gates' template probes and
# gates/setup-ios-export.sh. Sourcing it cd's to the repo root; restore the
# invocation cwd so the relative <godot-source-checkout> argument keeps
# resolving where the caller stood.
_invoke_pwd="$PWD"
source "$(dirname "$0")/_common.sh"
cd "$_invoke_pwd"

GODOT=${GODOT:-godot}
JOBS="$(sysctl -n hw.ncpu 2>/dev/null || nproc 2>/dev/null || echo 8)"

SRC="${1:-}"
[ -n "$SRC" ] && [ -d "$SRC" ] || { echo "usage: $0 <godot-source-checkout at the editor's tag>" >&2; exit 1; }
command -v scons >/dev/null 2>&1 || { echo "error: scons not found" >&2; exit 1; }
command -v xcodebuild >/dev/null 2>&1 || { echo "error: xcodebuild not found" >&2; exit 1; }

echo "== 1/4 Verifying versions =="
# Version parsing is godot_template_version_dir (gates/_common.sh, shared with
# gates/setup-ios-export.sh and the gates' template probes): dir keeps ".mono"
# for mono flavors, release tag never does (godot-builds tags are flavor-agnostic).
godot_template_version_dir >/dev/null \
    || { echo "error: cannot parse Godot version: $GODOT_FULL_VERSION" >&2; exit 1; }
full_version="$GODOT_FULL_VERSION"
version="$GODOT_TEMPLATE_VERSION_DIR"
tag="$GODOT_RELEASE_TAG"
ios_zip="$HOME/Library/Application Support/Godot/export_templates/$version/ios.zip"
[ -f "$ios_zip" ] || { echo "error: no installed iOS template at $ios_zip — run gates/setup-ios-export.sh first" >&2; exit 1; }
src_desc="$(git -C "$SRC" describe --tags 2>/dev/null || echo unknown)"
echo "editor:   $full_version"
echo "template: $ios_zip"
echo "source:   $SRC @ $src_desc"
if [ "$src_desc" != "$tag" ]; then
    echo "error: source checkout ($src_desc) does not match the editor tag ($tag)" >&2
    echo "       check out the tag, e.g.: git -C <clone> worktree add <dir> $tag" >&2
    exit 1
fi

echo "== 2/4 Building the simulator arm64 engine library (scons) =="
# The settings are recorded beside the product and the skip reads that stamp,
# never the file's existence: LIBSUFFIX carries no module-version string, so
# this NON-mono library and the mono one gates/setup-godot-fork-ios.sh builds
# have the SAME NAME, and a checkout that has served both hands whichever came
# last to a splice that cannot tell. The two scripts write the same stamp shape
# for that reason.
SIM_SCONS_ARGS=(platform=ios target=template_debug arch=arm64 ios_simulator=yes)
SIM_PROVENANCE="${SIM_SCONS_ARGS[*]}"
sim_lib="$SRC/bin/libgodot.ios.template_debug.arm64.simulator.a"
sim_stamp="$sim_lib.provenance"
sim_built="$(cat "$sim_stamp" 2>/dev/null || echo '<no stamp>')"
if [ -f "$sim_lib" ] && [ "$sim_built" = "$SIM_PROVENANCE" ]; then
    echo "skip: already built from these settings: $sim_lib"
else
    if [ -f "$sim_lib" ] && [ "$sim_built" != "$SIM_PROVENANCE" ]; then
        echo "-- the simulator library in the checkout was not built from these settings"
        echo "   stamped $sim_built"
        echo "   wanted  $SIM_PROVENANCE"
    fi
    # Removed first and written last: an interrupted build must not leave the
    # previous library standing behind a stamp that calls it this run's product.
    rm -f "$sim_stamp"
    (cd "$SRC" && scons "${SIM_SCONS_ARGS[@]}" "-j$JOBS")
    [ -f "$sim_lib" ] || { echo "error: scons produced no $sim_lib" >&2; exit 1; }
    printf '%s\n' "$SIM_PROVENANCE" > "$sim_stamp"
fi
lipo -info "$sim_lib"

echo "== 3/4 Rebuilding the template's debug xcframework =="
tmp="$(mktemp -d)"
trap 'rm -rf "$tmp"' EXIT
# Keep the template's own device slice; only the simulator slice is replaced.
# -create-xcframework keeps each input's file name, and the template names
# every slice's library libgodot.a — stage the built one under that name.
unzip -q "$ios_zip" "libgodot.ios.debug.xcframework/ios-arm64/libgodot.a" -d "$tmp"
mkdir -p "$tmp/sim"
cp "$sim_lib" "$tmp/sim/libgodot.a"
xcodebuild -create-xcframework \
    -library "$tmp/libgodot.ios.debug.xcframework/ios-arm64/libgodot.a" \
    -library "$tmp/sim/libgodot.a" \
    -output "$tmp/new/libgodot.ios.debug.xcframework" >/dev/null
# Swap the xcframework inside ios.zip in place (zip -d + zip -r keep every
# other template file untouched).
zip -q -d "$ios_zip" "libgodot.ios.debug.xcframework/*"
(cd "$tmp/new" && zip -q -r "$ios_zip" "libgodot.ios.debug.xcframework")

echo "== 4/4 Verifying the repaired simulator slice =="
rm -rf "$tmp/check"
sim_entries="$(unzip -l "$ios_zip" | grep -oE 'libgodot\.ios\.debug\.xcframework/ios-[a-z0-9_]*-simulator/libgodot\.a' || true)"
sim_entry="${sim_entries%%$'\n'*}"
[ -n "$sim_entry" ] || { echo "error: repacked ios.zip has no simulator slice" >&2; exit 1; }
unzip -q "$ios_zip" "$sim_entry" -d "$tmp/check"
archs="$(lipo -info "$tmp/check/$sim_entry" | LC_ALL=C sed 's/^.*: //')"
echo "simulator slice archs: $archs"
grep -q arm64 <<<"$archs" \
    || { echo "error: simulator slice still has no arm64" >&2; exit 1; }
echo "OK: the installed template now has an arm64 simulator slice"

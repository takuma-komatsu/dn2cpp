#!/usr/bin/env bash
# SETUP AID (NOT a regression gate — the name is outside the `build-and-run-*.sh`
# glob, so run-all-gates.sh ignores it; the iOS sibling of setup-godot-fork.sh).
# Assembles the mono-variant iOS export template the fork-editor iOS E2E gate
# needs at $FORK_ROOT/ios_template.zip, with a *working arm64 debug simulator
# slice*: the official mono templates ship an ios-arm64_x86_64-simulator
# libgodot.a that is actually x86_64-only (godotengine/godot#118161), and
# modern iOS simulator runtimes are arm64-only (Rosetta simulator boots were
# removed) — so a simulator run of an exported project is impossible with the
# stock template.
#
#   1. resolve the engine version from the fork editor ($FORK_EDITOR,
#      produced by gates/setup-godot-fork.sh) — must be a mono flavor
#   2. download/cache the official *mono* export-templates .tpz for that
#      version (~1.4 GB, cached under ~/.cache/dn2cpp-ios-export/) and pull
#      templates/ios.zip out of it
#   3. scons-build the missing arm64 debug simulator engine library in the
#      fork worktree (module_mono_enabled=yes; tens of minutes cold, skipped
#      when the product is there AND its provenance stamp says these settings
#      built it, unless FORCE=1)
#   4. rebuild libgodot.ios.debug.xcframework inside ios.zip — the official
#      device slice is kept, the simulator slice is the freshly built arm64
#      library — and write the result to $FORK_ROOT/ios_template.zip
#   5. verify the repaired simulator slice really contains arm64
#
# Only the DEBUG xcframework is rebuilt: `--export-debug "iOS"` (what the
# simulator E2E gate runs) never touches the release one.
#
# Idempotent: the download is skipped when the cached .tpz is there and
# NON-EMPTY, the scons build when its product carries a provenance stamp naming
# both these settings and the engine sources (step 3 — the file name cannot),
# the final zip when it carries
# the engine-provenance stamp of the sources now in the fork AND is newer than
# that library (FORCE=1 redoes the build + assembly); the verification always
# runs.
#
# The ENGINE the template was assembled from is stamped beside it
# (ios_template.zip.provenance, godot_fork_engine_provenance in
# gates/_godot_fork.sh), and that stamp is what the iOS export gate reads.
# Nothing else can: the consumer only pastes the zip into a preset's
# custom_template, which Godot validates with FileAccess::exists alone, so a
# template a patch release behind exports, runs and matches every expectation
# while shipping the wrong engine. After the 4.7.1 re-pin this zip was still 4.7
# and only `strings` on the artifact said so. The mtime test alone cannot see it:
# a re-pin does not touch the simulator library's mtime, so "$OUT -nt $sim_lib"
# stays true across it. Why a missing stamp is a refusal
# rather than a warning is written out at godot_fork_template_check.
#
# One stamp covers both halves of this zip. The device slice and the whole
# project scaffolding come from the official .tpz, which the engine hash says
# nothing about directly — but the tpz is chosen by $tag, $tag is parsed out of
# the fork editor's version, and the editor is built from these same sources, so
# a 4.7 tpz can only have been spliced by a run whose engine hash was 4.7's.
#
# Usage:
#   ./gates/setup-godot-fork-ios.sh [TPZ_PATH]
#
#   TPZ_PATH                 an already-downloaded *mono* export-templates .tpz
#                            to assemble from (skips the download)
#   DN2CPP_GODOT_FORK_ROOT   fork artifact root (default: $HOME/.cache/dn2cpp-godot-fork)
#   DN2CPP_IOS_EXPORT_CACHE  .tpz cache dir (default: $HOME/.cache/dn2cpp-ios-export)
#   FORCE=1                  rebuild the simulator slice and reassemble the zip
#                            even when their products already exist
set -euo pipefail
source "$(dirname "$0")/_godot_fork.sh"

CACHE="${DN2CPP_IOS_EXPORT_CACHE:-$HOME/.cache/dn2cpp-ios-export}"
FORCE="${FORCE:-0}"
JOBS="$(sysctl -n hw.ncpu 2>/dev/null || nproc 2>/dev/null || echo 8)"
OUT="$FORK_ROOT/ios_template.zip"

[ -x "$FORK_EDITOR" ] \
    || { echo "error: no fork editor at $FORK_EDITOR — run gates/setup-godot-fork.sh first" >&2; exit 1; }
command -v scons >/dev/null 2>&1 || { echo "error: scons not found (brew install scons)" >&2; exit 1; }
command -v xcodebuild >/dev/null 2>&1 || { echo "error: xcodebuild not found (Xcode required)" >&2; exit 1; }
command -v lipo >/dev/null 2>&1 || { echo "error: lipo not found (Xcode command-line tools required)" >&2; exit 1; }

echo "== 1/5 Resolving the engine version from the fork editor =="
# Version parsing mirrors gates/setup-ios-export.sh, but reads the *fork*
# editor rather than a PATH godot. Parse
# "<major>.<minor>[.<patch>].<status>[.mono].<build>.<sha>", e.g.
# "4.7.2.stable.mono.custom_build.ed1daf0bf" -> tag "4.7.2-stable", mono flavor.
# The godot-builds release tag never carries the flavor — the mono variant is
# a tpz filename variant (Godot_v<tag>_mono_export_templates.tpz).
full_version="$("$FORK_EDITOR" --version 2>/dev/null | tail -1)"
IFS='.' read -ra _v <<< "$full_version"
status_idx=-1
for i in "${!_v[@]}"; do
    case "${_v[$i]}" in
        stable|beta*|rc*|dev*|alpha*) status_idx=$i; break ;;
    esac
done
[ "$status_idx" -ge 2 ] \
    || { echo "error: cannot parse the fork editor version: $full_version" >&2; exit 1; }
version_num="$(IFS='.'; echo "${_v[*]:0:$status_idx}")"
status="${_v[$status_idx]}"
tag="${version_num}-${status}"
if [ "${_v[$((status_idx+1))]:-}" != "mono" ]; then
    echo "error: the fork editor is not a mono flavor ($full_version)" >&2
    echo "       the iOS E2E gate needs mono templates matching a mono editor —" >&2
    echo "       rebuild the fork cache with gates/setup-godot-fork.sh" >&2
    exit 1
fi
tpz_name="Godot_v${tag}_mono_export_templates.tpz"
echo "editor:  $full_version"
echo "version: $tag (mono variant, $tpz_name)"
echo "output:  $OUT"

# Resolve the fork worktree from the path the artifact root records, the same way
# the editor-export gates do (godot_fork_resolve, gates/_godot_fork.sh).
godot_fork_resolve || exit 1
echo "fork:    $FORK"
# The pinned base and the fingerprint of the engine sources this fork would
# compile — what the assembled template is stamped with (see header). Read from
# the checked-in pin file rather than hardcoded, so this aid and
# gates/setup-godot-fork.sh cannot name two different bases.
BASE_COMMIT="$(godot_fork_base_commit)"
ENGINE_PROVENANCE="$(godot_fork_engine_provenance)"
ENGINE_STAMP="$OUT.provenance"
echo "engine:  $ENGINE_PROVENANCE"

echo "== 2/5 Obtaining the mono export-templates .tpz =="
# The same two rules the Android/iOS installers state at this step: an explicit
# argument must name an EXISTING tpz (a typo must not become a ~1.4 GB download
# to the wrong place), and the download is verified non-empty BEFORE the atomic
# rename (curl -f catches an HTTP error, but a zero-length 200 answered by a
# proxy is a successful transfer, and renaming it makes every later run skip the
# download and hand the empty file to unzip).
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
    [ -s "$tpz.part" ] \
        || { echo "error: downloaded $tpz_name is empty (bad mirror?)" >&2
             rm -f "$tpz.part"; exit 1; }
    mv "$tpz.part" "$tpz"
fi

echo "== 3/5 Building the simulator arm64 engine library (scons) =="
# LIBSUFFIX never carries the module version string (only the editor/template
# executables get the ".mono" suffix), so the mono-enabled library keeps the
# plain name — the very name a plain build writes, and a plain one splices into
# the template as a simulator slice with no mono in it, which fails at run time
# inside the exported app rather than here. The file name cannot carry the
# answer, so the SETTINGS AND ENGINE SOURCES THAT PRODUCED IT are recorded
# beside it and the skip reads the stamp: same placement and same rule as an
# engine binary's
# .engine-hash (gates/setup-godot-fork.sh), and as the .provenance stamp
# gates/setup-ios-sim-template.sh writes for its own non-mono build of this
# same library name. FORCE=1 still rebuilds; it is no longer the only way to
# find out.
SIM_SCONS_ARGS=(platform=ios target=template_debug arch=arm64
                ios_simulator=yes module_mono_enabled=yes)
# Both terms are required. SCons arguments distinguish this mono simulator
# library from another build using the same output name; engine provenance
# makes a re-pin miss even when those arguments remain byte-for-byte identical.
SIM_PROVENANCE="$ENGINE_PROVENANCE scons=${SIM_SCONS_ARGS[*]}"
sim_lib="$FORK/bin/libgodot.ios.template_debug.arm64.simulator.a"
sim_stamp="$sim_lib.provenance"
sim_built="$(cat "$sim_stamp" 2>/dev/null || echo '<no stamp>')"
if [ -f "$sim_lib" ] && [ "$sim_built" = "$SIM_PROVENANCE" ] && [ "$FORCE" != 1 ]; then
    echo "skip: already built from these settings: $sim_lib (FORCE=1 rebuilds)"
else
    if [ -f "$sim_lib" ] && [ "$sim_built" != "$SIM_PROVENANCE" ]; then
        echo "-- the simulator library in the fork was not built from these settings"
        echo "   stamped $sim_built"
        echo "   wanted  $SIM_PROVENANCE"
    fi
    echo "-- scons: building the mono iOS simulator library in the fork (tens of minutes)"
    # Removed first and written last: an interrupted build must not leave the
    # previous library standing behind a stamp that calls it this run's product
    # (install_binary in gates/setup-godot-fork.sh, same rule).
    rm -f "$sim_stamp"
    (cd "$FORK" && scons "${SIM_SCONS_ARGS[@]}" "-j$JOBS")
    [ -f "$sim_lib" ] || { echo "error: scons produced no $sim_lib" >&2; exit 1; }
    printf '%s\n' "$SIM_PROVENANCE" > "$sim_stamp"
fi
lipo -info "$sim_lib"

echo "== 4/5 Assembling $OUT =="
tmp="$(mktemp -d)"
trap 'rm -rf "$tmp"' EXIT
out_engine="$(head -1 "$ENGINE_STAMP" 2>/dev/null || echo '<no stamp>')"
[ -n "$out_engine" ] || out_engine='<no stamp>'
if [ -f "$OUT" ] && [ "$FORCE" != 1 ] && [ "$OUT" -nt "$sim_lib" ] \
    && [ "$out_engine" = "$ENGINE_PROVENANCE" ]; then
    echo "skip: already assembled from these engine sources: $OUT (FORCE=1 reassembles)"
else
    if [ -f "$OUT" ] && [ "$out_engine" != "$ENGINE_PROVENANCE" ]; then
        echo "-- the template in the artifact root was not assembled from these engine sources"
        echo "   stamped $out_engine"
        echo "   wanted  $ENGINE_PROVENANCE"
    fi
    # Removed first and written last, so an interrupted assembly leaves an
    # UNSTAMPED template rather than the previous one under this run's stamp:
    # unstamped is refused (godot_fork_template_check), mis-stamped is used.
    # Same rule as install_binary in gates/setup-godot-fork.sh.
    rm -f "$ENGINE_STAMP"
    # A .tpz is a zip with a top-level templates/ folder; start from its
    # pristine ios.zip so every non-xcframework template file is kept. Verified
    # non-empty here rather than left to the member extraction below, whose
    # failure would name a missing xcframework path and not the empty tpz.
    unzip -p "$tpz" templates/ios.zip > "$tmp/ios_template.zip"
    [ -s "$tmp/ios_template.zip" ] \
        || { echo "error: extracted templates/ios.zip is empty (bad tpz?)" >&2; exit 1; }
    # Rebuild the debug xcframework: keep the template's own device slice,
    # replace only the simulator slice (the 3-step splice of
    # gates/setup-ios-sim-template.sh). -create-xcframework keeps each input's
    # file name, and the template names every slice's library libgodot.a —
    # stage the built one under that name.
    unzip -q "$tmp/ios_template.zip" "libgodot.ios.debug.xcframework/ios-arm64/libgodot.a" -d "$tmp"
    mkdir -p "$tmp/sim"
    cp "$sim_lib" "$tmp/sim/libgodot.a"
    xcodebuild -create-xcframework \
        -library "$tmp/libgodot.ios.debug.xcframework/ios-arm64/libgodot.a" \
        -library "$tmp/sim/libgodot.a" \
        -output "$tmp/new/libgodot.ios.debug.xcframework" >/dev/null
    # Swap the xcframework inside the zip in place (zip -d + zip -r keep every
    # other template file untouched).
    zip -q -d "$tmp/ios_template.zip" "libgodot.ios.debug.xcframework/*"
    (cd "$tmp/new" && zip -q -r "$tmp/ios_template.zip" "libgodot.ios.debug.xcframework")
    mkdir -p "$FORK_ROOT"
    mv "$tmp/ios_template.zip" "$OUT"
    printf '%s\n' "$ENGINE_PROVENANCE" > "$ENGINE_STAMP"
    echo "-- wrote $OUT"
fi

echo "== 5/5 Verifying the repaired simulator slice =="
echo "-- debug xcframework entries:"
unzip -l "$OUT" | grep -E 'libgodot\.ios\.debug\.xcframework/.*libgodot\.a' | LC_ALL=C sed 's/^/   /'
sim_entries="$(unzip -l "$OUT" | grep -oE 'libgodot\.ios\.debug\.xcframework/ios-[a-z0-9_]*-simulator/libgodot\.a' || true)"
sim_entry="${sim_entries%%$'\n'*}"
[ -n "$sim_entry" ] || { echo "error: $OUT has no debug simulator slice" >&2; exit 1; }
rm -rf "$tmp/check"
unzip -q "$OUT" "$sim_entry" -d "$tmp/check"
archs="$(lipo -info "$tmp/check/$sim_entry" | LC_ALL=C sed 's/^.*: //')"
echo "simulator slice archs: $archs"
grep -q arm64 <<<"$archs" \
    || { echo "error: the simulator slice still has no arm64" >&2; exit 1; }
echo
echo "OK: $OUT has an arm64 debug simulator slice"
echo "engine stamp: $ENGINE_STAMP ($ENGINE_PROVENANCE)"
echo "then: the iOS E2E gate can point its export preset at it"

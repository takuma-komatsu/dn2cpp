#!/usr/bin/env bash
# SETUP AID (NOT a regression gate — the name is outside the `build-and-run-*.sh`
# glob, so run-all-gates.sh ignores it; the Android sibling of
# gates/setup-ios-export.sh). Installs the two host-side prerequisites the
# Android gates cannot carry: the ONE official export template file
# (android_debug.apk) and the ONE Editor Setting the exporter refuses without.
#
# All three Android gates that touch the APK-export tail
# (build-and-run-godot-editor-export-android.sh, build-and-run-cri-android.sh,
# build-and-run-android-gdext.sh) run `--export-debug` against a preset with
# `gradle_build/use_gradle_build=false` (samples/godot-dotnet/EditorExportSample
# and samples/godot/godot-project both pin it off), so every one of them takes
# the prebuilt-template path and needs exactly android_debug.apk — never
# android_release.apk (that is `--export-release`'s file, no gate here uses it)
# or android_source.zip (that is the gradle-build path, which this repo does not
# use).
#
#   1. download the official export-templates .tpz for the installed Godot
#      version (~1 GB, cached under ~/.cache/dn2cpp-android-export/ — a fresh
#      cache directory from setup-ios-export.sh's, since the two tpz's are the
#      same download but nothing here should make an iOS setup run redundant
#      for Android or vice versa)
#   2. extract just templates/android_debug.apk into the editor's template
#      search path (godot_user_data_dir/export_templates/<version>/android_debug.apk)
#   3. point the editor's `export/android/java_sdk_path` at a working JDK
#
# Step 3 is not a convenience. `EditorExportPlatformAndroid::can_export` reads that
# editor setting and refuses the preset outright when it is empty — before any
# dn2cpp code runs, with a config error naming Editor Settings — and it does so on a
# **mono editor regardless of whether the project uses C#**, so it takes down the
# GDScript-plus-GDExtension android-gdext gate too. A `java` on PATH does not help:
# Godot never consults PATH for it. This host had exactly that shape (PATH's `java`
# WAS Android Studio's JBR, the setting was ""), and all three Android gates failed
# inside the engine.
#
# Why a setup aid writes it rather than a gate: editor_settings-<major>.<minor>.tres
# is shared with every other Godot gate on the host, and a gate that rewrites it
# mid-suite is a shared-mutable-state hazard the editor-export gate deliberately
# avoids (see its stale-build-cache section). A human-run installer, with no editor
# running to overwrite the file on exit, is the safe place.
#
# Idempotent: the download is skipped when the cached .tpz is there and NON-EMPTY,
# the install when android_debug.apk is, the setting when it already names a
# JDK that runs — an existing WORKING value is never overwritten. The .tpz download
# is deliberately kept out of the gates themselves — machines without the template
# just skip (or go partial on) the Android gates.
#
# Usage:
#   ./gates/setup-android-export.sh [TPZ_PATH]
#
#   TPZ_PATH   an already-downloaded export-templates .tpz to install from
#              (skips the download)
set -euo pipefail

# _common.sh supplies godot_template_version_dir + godot_user_data_dir — the ONE
# parser of the editor's version string and the ONE per-OS template-search-path
# resolver, shared with every gate's template probe, so this installer and the
# detectors cannot drift apart on the directory name. Sourcing it cd's to the
# repo root; restore the invocation cwd so a relative TPZ_PATH argument keeps
# resolving where the caller stood.
_invoke_pwd="$PWD"
source "$(dirname "$0")/_common.sh"
cd "$_invoke_pwd"

GODOT=${GODOT:-godot}
CACHE="${DN2CPP_ANDROID_EXPORT_CACHE:-$HOME/.cache/dn2cpp-android-export}"

command -v "$GODOT" >/dev/null 2>&1 || { echo "error: godot not found on PATH" >&2; exit 1; }
command -v unzip >/dev/null 2>&1 || { echo "error: unzip not found" >&2; exit 1; }

echo "== 1/4 Resolving the Godot template version =="
# Parsed by godot_template_version_dir (gates/_common.sh — shared with the
# ios/android gates' template probes):
#   "4.6.3.stable.official.7d41c59c4"       -> dir "4.6.3.stable",       tag "4.6.3-stable" (vanilla tpz)
#   "4.7.stable.official.<sha>"             -> dir "4.7.stable",         tag "4.7-stable"   (vanilla tpz)
#   "4.7.1.stable.mono.official.<sha>"      -> dir "4.7.1.stable.mono",  tag "4.7.1-stable" (mono tpz)
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
templates_dir="$(godot_user_data_dir)/export_templates/$version"
echo "godot:    $full_version"
echo "version:  $version (release tag $tag${flavor:+, mono variant})"
echo "install:  $templates_dir/android_debug.apk"

echo "== 2/4 Obtaining the export-templates .tpz =="
# An explicit argument must name an EXISTING tpz: treating a mistyped path as
# the download destination would silently turn a typo into a ~1 GB download to
# the wrong place. Only the no-argument default path may download.
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
    # Verified BEFORE the atomic rename, the same rule step 3 applies to the
    # extract: curl -f catches an HTTP error, but a zero-length 200 answered by
    # a proxy is a successful transfer, and renaming it makes every later run
    # skip the download and hand the empty file to unzip.
    [ -s "$tpz.part" ] \
        || { echo "error: downloaded $tpz_name is empty (bad mirror?)" >&2
             rm -f "$tpz.part"; exit 1; }
    mv "$tpz.part" "$tpz"
fi

echo "== 3/4 Installing templates/android_debug.apk =="
if [ -s "$templates_dir/android_debug.apk" ]; then
    echo "skip: already installed: $templates_dir/android_debug.apk"
else
    mkdir -p "$templates_dir"
    # A .tpz is a zip with a top-level templates/ folder. Extract to a .part
    # file and verify BEFORE the atomic rename: the tpz is ~1 GB and this
    # command is run over a real network, so a truncated download or a partial
    # extract must never be left where a gate's file test would read it as a
    # complete, installed template.
    unzip -p "$tpz" templates/android_debug.apk > "$templates_dir/android_debug.apk.part"
    [ -s "$templates_dir/android_debug.apk.part" ] \
        || { echo "error: extracted android_debug.apk is empty (bad tpz?)" >&2
             rm -f "$templates_dir/android_debug.apk.part"; exit 1; }
    mv "$templates_dir/android_debug.apk.part" "$templates_dir/android_debug.apk"
    echo "installed: $templates_dir/android_debug.apk"
fi

echo "== 4/4 Pointing export/android/java_sdk_path at a working JDK =="
# The value Godot wants is the JDK HOME (it appends bin/java itself), spelled the
# way the editor spells paths in this file: NATIVE, with forward slashes. On
# Windows an MSYS /c/... would be written verbatim into the .tres — nothing
# converts a path on its way into a FILE — and the editor would then look for
# C:/c/Users/... and go on refusing the preset, which is the MSYS dialect trap in
# its usual shape. native_path (gates/_common.sh) is the one converter.
settings_file="$(godot_editor_settings_file "$GODOT")" \
    || { echo "error: cannot derive the editor settings path from $GODOT_FULL_VERSION" >&2; exit 1; }
echo "settings: $settings_file"
if existing="$(android_editor_java_sdk "$GODOT")"; then
    echo "skip: already set to a working JDK: $existing"
else
    jdk=""
    while IFS= read -r cand; do
        if "$cand/bin/java$EXE_EXT" -version >/dev/null 2>&1; then jdk="$cand"; break; fi
    done < <(android_jdk_candidates)
    [ -n "$jdk" ] || { echo "error: no working JDK found. Install Android Studio (its bundled" >&2
                       echo "       JBR is what these candidates are), or set" >&2
                       echo "       export/android/java_sdk_path by hand in the editor." >&2
                       android_jdk_candidates | LC_ALL=C sed 's|^|       tried: |' >&2
                       exit 1; }
    jdk_native="$(native_path "$jdk")"
    # Forward slashes: the editor writes them even on Windows, and a backslash in a
    # .tres string literal is an escape.
    jdk_native="${jdk_native//\\//}"
    [ -f "$settings_file" ] || { echo "error: $settings_file does not exist — launch the editor" >&2
                                 echo "       once so it writes its settings, then re-run this." >&2; exit 1; }
    cp "$settings_file" "$settings_file.dn2cpp-bak"
    if grep -q '^export/android/java_sdk_path[[:space:]]*=' "$settings_file"; then
        tmp="$(mktemp)"
        sed "s|^export/android/java_sdk_path[[:space:]]*=.*|export/android/java_sdk_path = \"$jdk_native\"|" \
            "$settings_file" > "$tmp"
        mv "$tmp" "$settings_file"
    else
        # [resource] is the LAST section of an editor_settings .tres (the
        # sub_resources for shortcut overrides precede it), so an append lands
        # inside it — which is where a property assignment has to be.
        printf 'export/android/java_sdk_path = "%s"\n' "$jdk_native" >> "$settings_file"
    fi
    android_editor_java_sdk "$GODOT" >/dev/null \
        || { echo "error: wrote export/android/java_sdk_path = \"$jdk_native\" but the probe still" >&2
             echo "       rejects it; restoring $settings_file.dn2cpp-bak" >&2
             cp "$settings_file.dn2cpp-bak" "$settings_file"; exit 1; }
    echo "set: export/android/java_sdk_path = \"$jdk_native\" (backup: $settings_file.dn2cpp-bak)"
fi

echo
size="$(du -h "$templates_dir/android_debug.apk" 2>/dev/null | cut -f1)"
echo "OK: $templates_dir/android_debug.apk (${size:-?})"
echo "OK: export/android/java_sdk_path = \"$(android_editor_java_sdk "$GODOT")\""
echo "-- android_debug.apk is itself a zip; sanity-check its contents:"
unzip -l "$templates_dir/android_debug.apk" | tail -5
echo "then: the godot-editor-export-android / cri-android / android-gdext gates'"
echo "      APK-export sections can find the official template"

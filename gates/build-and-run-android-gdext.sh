#!/usr/bin/env bash
# Android NDK cross-build gate: transpiled C++ -> arm64-v8a Android binaries.
# Two sections: a console program (StringCore, the string/formatting core) and
# the GodotSample GDExtension .so placed at the path the .gdextension's
# android.*.arm64 entries reference. Build-only — there is no emulator/device
# in the loop, so the assertion is that each artifact is a well-formed
# ELF 64-bit aarch64 binary (the Boehm GC, posix PAL and pthreads all have
# __ANDROID__/bionic arms and compile unchanged; running them on-device is a
# follow-up once an Android environment is available).
#
# Machines without an Android NDK skip (reported as a SKIP, never as a pass —
# see gate_skip in _common.sh): point ANDROID_NDK_ROOT at an installed NDK (the
# directory containing build/cmake/android.toolchain.cmake) to activate the
# gate. ANDROID_ABI / ANDROID_PLATFORM override the arm64-v8a / android-24
# defaults.
#
# On a Windows host the CoreLib used for the transpile also needs to be a
# non-Windows one — see locate_corelib_cross_posix in _common.sh for why (the
# short version: the local Windows CoreLib's FileStream implementation
# P/Invokes kernel32.dll/ntdll.dll directly, which the NDK sysroot cannot
# link). That helper fails loudly with the fetch command if none is cached.
source "$(dirname "$0")/_common.sh"

if [ -z "${ANDROID_NDK_ROOT:-}" ]; then
    gate_skip "ANDROID_NDK_ROOT not set — no Android NDK to cross-build with"
fi
if [ ! -f "$ANDROID_NDK_ROOT/build/cmake/android.toolchain.cmake" ]; then
    gate_skip "no NDK toolchain file under ANDROID_NDK_ROOT ($ANDROID_NDK_ROOT)"
fi

export ANDROID=1

# The JDK half of the APK-export guard is android_ensure_java (gates/_common.sh),
# the same predicate the two sibling Android gates ask — NOT a local `java
# -version` test. It wants a *working* java, not merely a file on PATH: macOS
# ships a /usr/bin/java stub that `command -v` finds but that exits nonzero with
# "Unable to locate a Java Runtime" when no JDK is installed, so an existence
# test reads a JDK-less machine as JDK-equipped and the APK export tail then
# fails inside the editor instead of going gate_partial. Beyond that it also
# PROVISIONS one from Android Studio's bundled JBR when PATH has none
# (android_jdk_candidates), which the local copy this replaces did not: on a
# stock Android Studio box that copy answered "no JDK" and took gate_partial
# while build-and-run-cri-android.sh and build-and-run-godot-editor-export-android.sh
# found the JBR and ran the export — an avoidable partial, and a spurious hard
# failure under DN2CPP_REQUIRE_ALL=1 — a guard must ask the question the section
# actually needs, in both directions. The section-3 guard and the cache
# key's java= term both ask it, so a JDK appearing changes the key and forces the
# full APK path.
#
# It is NOT the whole JDK story, and the other half is not on PATH at all:
# `EditorExportPlatformAndroid::can_export` reads the editor's
# export/android/java_sdk_path and refuses the preset when it is empty. That
# refusal lands on a **mono editor even for this GDScript-plus-GDExtension
# project** — the .NET-on-Android config check is not conditioned on the project
# using C# — so this gate needs android_editor_java_sdk (gates/_common.sh) beside
# it. Provisioned by gates/setup-android-export.sh.

# assert_elf_aarch64 PATH — the artifact must be an ELF 64-bit aarch64 binary.
assert_elf_aarch64() {
    local desc; desc="$(file -b "$1")"
    printf '%s: %s\n' "$1" "$desc"
    case "$desc" in
        *ELF*64-bit*aarch64*|*ELF*64-bit*ARM\ aarch64*) ;;
        *) echo "FAIL: not an ELF 64-bit aarch64 binary: $1" >&2; return 1 ;;
    esac
}

echo "== 1/3 Console program (StringCore) cross-built for Android =="
corelib=$(locate_corelib_cross_posix)
bcl=$(dirname "$corelib")
build_proj samples/dotnet/StringCore/StringCore.csproj
OUT=artifacts/stringcore-android
refs=(-r "$corelib")
# Hard requirement, not a wish: without it this section cross-builds a smaller
# StringCore and assert_elf_aarch64 still holds — a narrowed run reported green,
# and the section-1 cache key (ndk/ndkver + the app dll) cannot tell the two
# apart, so it is then replayed forever.
[ -f "$bcl/System.Linq.dll" ] \
    || { echo "error: requested reference System.Linq not found beside the cross CoreLib: $bcl/System.Linq.dll" >&2; exit 1; }
refs+=(-r "$bcl/System.Linq.dll")
invoke_cli "samples/dotnet/StringCore/bin/$CONFIG/$TFM/StringCore.dll" "${refs[@]}" -o "$OUT"
# Cache: an unchanged transpile against an unchanged NDK means the cross-build
# would only reproduce its last green ELF. A hit skips this section only — the
# GDExtension section below keeps its own cache entry (separate OUT).
if gate_cache_check "$OUT" \
        "android-gdext-stringcore|ndk=$ANDROID_NDK_ROOT|ndkver=$(file_text "$ANDROID_NDK_ROOT/source.properties" 2)" \
        "samples/dotnet/StringCore/bin/$CONFIG/$TFM/StringCore.dll"; then
    gate_cache_hit_msg
else
    compile_console "$OUT" StringCore
    assert_elf_aarch64 "$OUT/StringCore"
    gate_cache_commit
fi

echo "== 2/3 GodotSample GDExtension .so cross-built for Android =="
build_proj samples/godot/GodotSample/GodotSample.csproj
OUT=artifacts/godot-android
PROJECT=samples/godot/godot-project
# The real CoreLib rides along for the Task.Run pool probe (async/await +
# Func<T> need it), same as the native godot-sample gate.
invoke_cli \
    "samples/godot/GodotSample/bin/$CONFIG/$TFM/GodotSample.dll" \
    -r "samples/godot/GodotSample/bin/$CONFIG/$TFM/GodotSharp.dll" \
    -r "$corelib" \
    -o "$OUT" --gdextension
# The editor + Android-template identity feeds both this section's cache key
# and the APK tail's guard below, so resolve it once, up front.
# godot_template_version_dir (in _common.sh) keeps the ".mono" flavor suffix a
# mono editor's template directory carries; `|| true` because a missing editor
# must fall through to the APK tail's gate_partial, not abort the gate here.
GODOT_BIN=${GODOT:-godot}
_godot_ver="$(godot_template_version_dir "$GODOT_BIN" || true)"
# godot_user_data_dir (in _common.sh) is the OS branch — the same directory the
# editor itself reads export_templates from.
_tpl="$(godot_user_data_dir)/export_templates/$_godot_ver/android_debug.apk"
# Cache: covers the .so cross-build AND the APK tail, so the key carries the
# editor + template identity (template keyed by stat — huge zip, versioned per
# Godot release, `none` when absent) and the .so target path itself: a cleaned
# project bin/ must miss instead of skipping into a broken APK export. A hit
# ends the gate — section 1 already ran above.
if gate_cache_check "$OUT" \
        "android-gdext|godot=$(or_none "$(first_line "$("$GODOT_BIN" --version 2>/dev/null)")")|ndk=$ANDROID_NDK_ROOT|ndkver=$(file_text "$ANDROID_NDK_ROOT/source.properties" 2)|tmpl=$(file_sig "$_tpl")|sdk=${ANDROID_HOME:-${ANDROID_SDK_ROOT:-none}}|java=$(android_ensure_java && command -v java || echo none)|editorjdk=$(android_editor_java_sdk "$GODOT_BIN" || echo none)" \
        "samples/godot/GodotSample/bin/$CONFIG/$TFM/GodotSample.dll" \
        "samples/godot/GodotSample/bin/$CONFIG/$TFM/GodotSharp.dll" \
        "$PROJECT" \
        "$PROJECT/bin/lib$DN2CPP_GDEXT_LIB.android.arm64.so"; then
    gate_cache_hit_msg
    exit 0
fi
mkdir -p "$PROJECT/bin"
compile_gdextension_android "$OUT" "$PROJECT/bin/lib$DN2CPP_GDEXT_LIB.android.arm64.so"
assert_elf_aarch64 "$PROJECT/bin/lib$DN2CPP_GDEXT_LIB.android.arm64.so"

# Optional tail — the Unity-style "produce an APK" step. Runs only when the
# whole Android export stack is present: the Godot editor, its Android export
# template, an Android SDK and a JDK (the editor also needs its
# export/android/* editor settings + debug keystore configured once).
# Everything-missing machines still pass on the cross-build asserts above.
if ! command -v "$GODOT_BIN" >/dev/null 2>&1 || [ ! -f "$_tpl" ] \
    || { [ -z "${ANDROID_HOME:-}" ] && [ -z "${ANDROID_SDK_ROOT:-}" ]; } \
    || ! android_ensure_java || ! android_editor_java_sdk "$GODOT_BIN" >/dev/null; then
    # A SECTION opt-out, not a whole-gate one: the cross-build asserts above did
    # run and did hold. gate_partial states the reduced coverage on stdout, and
    # fails under DN2CPP_REQUIRE_ALL=1.
    gate_partial "Android export stack incomplete (editor/template/SDK/JDK on PATH/the editor's export/android/java_sdk_path — run gates/setup-android-export.sh) — APK export section skipped"
    # Safe to record green here: the editor/template/SDK/JDK availability is in
    # the cache key (sdk=/java=/godot=/tmpl=/editorjdk= above), so completing
    # the export stack later changes the key and forces the full APK path.
    gate_cache_commit
    echo "OK"
    exit 0
fi

echo "== 3/3 Exporting the debug APK (godot --export-debug \"Android\") =="
EXPORT_DIR=artifacts/godot-android-export
rm -rf "$EXPORT_DIR"
mkdir -p "$EXPORT_DIR"
"$GODOT_BIN" --headless --path "$PROJECT" --import >"$EXPORT_DIR/import.log" 2>&1 || true
"$GODOT_BIN" --headless --path "$PROJECT" --export-debug "Android" \
    "$PWD/$EXPORT_DIR/godotsample.apk" >"$EXPORT_DIR/export.log" 2>&1 || true
# The log is KEPT and shown on failure. It used to go to /dev/null, and the cost
# was a whole diagnosis: when the editor refused the preset over an unset
# export/android/java_sdk_path, all this gate could say was "produced no APK" —
# the engine had named the setting, the cause and the remedy, and the gate threw
# the message away.
[ -f "$EXPORT_DIR/godotsample.apk" ] \
    || { echo "FAIL: godot --export-debug \"Android\" produced no APK (export log below)" >&2
         cat "$EXPORT_DIR/export.log" >&2; exit 1; }
# Snapshot the listing once and grep the here-string (no pipe): under
# `set -o pipefail`, `unzip -l | grep -q` reports the pipeline as failed when
# grep -q matches early and unzip dies on SIGPIPE (status 141) flushing the
# remaining entries — a false "not embedded" on an APK that does embed the .so.
# Same fix as the compression gates' nm checks.
apk_listing="$(unzip -l "$EXPORT_DIR/godotsample.apk")"
grep -q "lib/arm64-v8a/lib$DN2CPP_GDEXT_LIB.android.arm64.so" <<<"$apk_listing" \
    || { echo "FAIL: the APK does not embed the dn2cpp GDExtension .so" >&2; exit 1; }
echo "APK exported: $EXPORT_DIR/godotsample.apk"
gate_cache_commit
echo "OK"

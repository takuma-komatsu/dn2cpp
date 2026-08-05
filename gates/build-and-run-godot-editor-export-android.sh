#!/usr/bin/env bash
# Godot editor-export Android E2E gate: the *forked editor* exports a C# game
# for Android with `dotnet/export_backend = dn2cpp`, and the APK it produces
# carries the drop-in where the engine's own loader looks for it.
#
# The Android sibling of gates/build-and-run-godot-editor-export{,-ios}.sh. One
# headless `--export-debug` drives the fork's whole Android pipeline:
# ExportPlugin resolves the target architecture, one framework-dependent publish
# runs (RID android-arm64), Dn2CppExporter transpiles it and cmake-builds it
# through the NDK's own toolchain file (no device needed to build), and the
# staged lib{Assembly}.so goes into the APK's lib/arm64-v8a/ down the
# AddSharedObject path the platform exporter already takes for Android.
#
# What makes this the Android gate rather than a second iOS one: it exports
# against the **stock** export template. The engine's Android fallback —
# try_load_native_aot_library dlopening the BARE soname lib{Assembly}.so, which
# the linker resolves out of the APK's lib/<abi>/ — is upstream 4.7-stable code,
# so the official mono APK template loads the drop-in as shipped and no template
# is built here at all.
#
# The four claims it pins down:
#   1. The drop-in lands where the engine opens it: lib/arm64-v8a/lib{P}.so, an
#      ELF aarch64 shared object exporting godotsharp_game_main_init (asserted in
#      the ELF spelling — llvm-nm -D, the only table a dlopen consumer resolves
#      through).
#   2. No .NET runtime ships. GDMono reaches try_load_native_aot_library only
#      after failing to find hostfxr AND coreclr, so a stray runtime in the APK
#      would route the game back to .NET on-device and make the rest prove
#      nothing.
#   3. The pck still carries .godot/mono/publish/{arch}/. GDMono aborts before
#      the fallback when that directory is missing (gd_mono.cpp's
#      "Unable to find the .NET assemblies directory"), so the drop-in being
#      packaged as a shared object rather than a pck file does NOT get to leave
#      it empty.
#   4. A target set the backend cannot serve is refused, and refused before the
#      publish (a custom feature naming a second ABI).
#
# Build-and-package only: there is no device in the suite. What a gate can hold
# onto is the artifact, and that is what this asserts.
#
# Requires the artifacts of gates/setup-godot-fork.sh, an Android NDK, an
# Android SDK, a working JDK on PATH, the editor's own
# export/android/java_sdk_path pointing at one, and the official Godot mono
# Android export template (gates/setup-android-export.sh installs the last two);
# prints SKIP and exits 0 when any is absent. Registered in run-all-gates.sh's
# SERIAL Godot phase — it launches the engine, and every launch runs under a
# watchdog because a broken Godot run hangs rather than fails.
source "$(dirname "$0")/_common.sh"
source "$(dirname "$0")/_godot_fork.sh"

OUT=gates/out-godot-editor-export-android
SAMPLE=samples/godot-dotnet/EditorExportSample
PROJECT_NAME=EditorExportSample

godot_fork_preflight
if [ -z "${ANDROID_NDK_ROOT:-}" ] || [ ! -f "$ANDROID_NDK_ROOT/build/cmake/android.toolchain.cmake" ]; then
    gate_skip "no Android NDK under ANDROID_NDK_ROOT — the drop-in cannot be cross-compiled"
fi
# Resolved once, and through android_sdk_root (gates/_common.sh) rather than an
# env-only probe: a stock Android Studio sets neither ANDROID_HOME nor
# ANDROID_SDK_ROOT, so testing the vars directly skipped this gate on a fully
# provisioned box while the migrated cri-android sibling ran. The resolved root
# also feeds the cache key below, so cached and live see the same SDK identity
# whichever way it was found.
SDK_ROOT="$(android_sdk_root)" \
    || gate_skip "no Android SDK (ANDROID_HOME/ANDROID_SDK_ROOT unset, none at the per-OS default) — the APK cannot be packaged"
# A *working* java, not merely one on PATH (android_ensure_java, gates/_common.sh,
# also prepends Android Studio's bundled JBR when the PATH one is a non-functional
# stub — e.g. macOS's /usr/bin/java). The debug APK's signing step needs it; on
# this machine java happens to already be on PATH, which is exactly the
# "harmless until it isn't" gap cri-android's sibling check exists to close.
android_ensure_java \
    || gate_skip "no working JDK on PATH (android_ensure_java found none) — the APK cannot be signed"
# And the OTHER JDK question, which is not about PATH at all: Godot's
# `EditorExportPlatformAndroid::can_export` reads the editor's
# export/android/java_sdk_path and refuses the whole preset when it is empty. That
# refusal precedes every dn2cpp step, so without this probe the gate died inside
# the engine on a config error in the editor's locale rather than skipping with a
# remedy. Provisioned by gates/setup-android-export.sh.
EDITOR_JDK="$(android_editor_java_sdk "$FORK_EDITOR")" \
    || gate_skip "the editor's export/android/java_sdk_path names no working JDK — run gates/setup-android-export.sh"
# The stock template, resolved the way the editor resolves it: by its own
# version, under the user's export_templates directory. Not built here — that it
# is the OFFICIAL one is the point (see the header). The version directory is
# VERSION_FULL_CONFIG (4.7.stable.mono), derived by godot_template_version_dir
# (gates/_common.sh) — the ONE parser of the editor's version string, which
# keeps the ".mono" flavor on a 3-component version too (a naive first-four-
# fields cut is right only for the current 2-component pin). The build suffix
# (custom_build.<hash>) must NOT reach the path, or the fork would look for a
# template only the fork could have shipped; the parser stops at the flavor.
_ver="$(godot_template_version_dir "$FORK_EDITOR" || true)"
ANDROID_TEMPLATE="$(godot_user_data_dir)/export_templates/$_ver/android_debug.apk"
if [ ! -f "$ANDROID_TEMPLATE" ]; then
    gate_skip "no official Android export template at $ANDROID_TEMPLATE — run gates/setup-android-export.sh"
fi

echo "== 1/7 Fork pin + interop-ABI tripwire =="
# On this gate the ABI tripwire is doubly load-bearing: the template the export
# runs against is the stock one, built from exactly the pinned base.
godot_fork_pin_abi_check

# Inputs-only key, as in the macOS/iOS siblings: the forked editor drives the
# whole pipeline from the packaged toolchain, so nothing is transpiled locally.
# The NDK and the stock template join the context because they are what the
# export compiles and packages against.
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
    "godot-editor-export-android|$(godot_fork_ctx)|ndk=$ANDROID_NDK_ROOT|ndkver=$(file_text "$ANDROID_NDK_ROOT/source.properties" 2)|tmpl=$(file_sig "$ANDROID_TEMPLATE")|sdk=$SDK_ROOT|editorjdk=$EDITOR_JDK" \
    "$SELFHOST_BIN" \
    dist/package-toolchain.sh \
    "$SAMPLE" \
    "$ABI_EXPECTED"; then
    { gate_cache_hit_msg; exit 0; }
fi

echo "== 2/7 Installing the working tree's toolchain into the fork editor =="
# Packaging still runs every run — that is what keeps a dn2cpp-side regression
# visible here rather than only in the next setup-godot-fork.sh. The install is
# skipped when the content already matches, and otherwise lands as an atomic
# swap under a stage lock; see stage_editor_toolchain in gates/_common.sh.
stage_editor_toolchain "$FORK_GODOTSHARP" "$SELFHOST_BIN" "$OUT/package.log"

echo "== 3/7 Staging the sample project =="
# Copied rather than exported in place: the project accumulates .godot/, bin/ and
# obj/, and the TFM below has to move without perturbing the macOS/iOS gates that
# share this sample. The work dir is NOT wiped — the persistent
# .godot/mono/dn2cpp/build tree is what keeps a re-export from recompiling the
# whole runtime.
PROJ="$OUT/project"
mkdir -p "$PROJ"
cp -R "$SAMPLE/." "$PROJ/"

cat > "$PROJ/nuget.config" <<EOF
<?xml version="1.0" encoding="utf-8"?>
<!-- GENERATED by gates/build-and-run-godot-editor-export-android.sh — do not commit.
     Points the Godot.NET.Sdk resolver + restore at the fork's local feed. The
     feed path is native (godot_fork_native_path): MSYS converts a path it hands
     a native program as an ARGUMENT, never one written into a file, so an MSYS
     /c/... here reaches NuGet as C:\c\Users\... and restore fails on a feed
     that "does not exist". -->
<configuration>
  <packageSources>
    <clear />
    <add key="dn2cpp-godot-fork-local" value="$(godot_fork_native_path "$FORK_ROOT/nuget")" />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
  </packageSources>
</configuration>
EOF

# net9.0, and only in the copy. A template (non-gradle) Android export validates
# the C# project's TFM against exactly net9.0 — the stock template ships .jar
# dependencies built for it (platform/android/export/export_plugin.cpp,
# _validate_dotnet_tfm) — and refuses anything else before it runs a single step
# of the export. That constraint is the template's, not dn2cpp's: the transpiler
# reads the game's IL against the bundle's own pinned CoreLib and does not care
# what the game targeted. Patching the staged copy keeps the macOS and iOS gates
# on the net8.0 they assert today.
csproj_tmp="$(mktemp)"
sed 's|<TargetFramework>net8.0</TargetFramework>|<TargetFramework>net9.0</TargetFramework>|' \
    "$PROJ/$PROJECT_NAME.csproj" > "$csproj_tmp"
mv "$csproj_tmp" "$PROJ/$PROJECT_NAME.csproj"
grep -q "<TargetFramework>net9.0</TargetFramework>" "$PROJ/$PROJECT_NAME.csproj" \
    || { echo "FAIL: could not retarget the staged project at net9.0" >&2; exit 1; }

echo "== 4/7 Importing the project (fork editor, headless) =="
# The first import may abort in editor doc-gen teardown (Godot headless bug,
# harmless — the import data is already written), so tolerate a non-zero exit.
run_with_watchdog 600 "$FORK_EDITOR" --headless \
    --path "$PWD/$PROJ" --import >"$OUT/import.log" 2>&1 || true

echo "== 5/7 Exporting for Android with dotnet/export_backend = dn2cpp =="
# Generous watchdog: a cold export cmake-builds the runtime, the Boehm GC and the
# vendored zlib/brotli/highway from source, through the NDK, before the game.
EXPORT_DIR="$PWD/$OUT/export"
APK="$EXPORT_DIR/$PROJECT_NAME.apk"
rm -rf "$EXPORT_DIR"
mkdir -p "$EXPORT_DIR"
if ! godot_export_step 3600 "$OUT/export.log" "$APK" \
    "$FORK_EDITOR" --headless \
    --path "$PWD/$PROJ" --export-debug dn2cpp-android "$APK"; then
    echo "FAIL: --export-debug failed (see below)" >&2
    cat "$OUT/export.log" >&2
    exit 1
fi
# The export exit code is untrustworthy: EditorNode only fails the run when
# export_project() itself returns non-OK, and ExportPlugin._ExportBegin catches
# everything _ExportBeginImpl throws and turns it into an AddMessage(Error, …),
# which downgrades to "completed with warnings". So a broken dn2cpp export
# produces a plausible-looking APK and a zero exit code — these assertions, not
# the exit code, are what makes this gate an oracle.
if grep -q "ERROR: Export .NET Project" "$OUT/export.log"; then
    echo "FAIL: the C# export plugin reported an error (see below)" >&2
    cat "$OUT/export.log" >&2
    exit 1
fi
# One publish + transpile + build: the preset targets one ABI, so one RID.
for marker in "dn2cpp: transpiling" "dn2cpp: compiling the drop-in library" "dn2cpp: staged"; do
    n="$(grep -cF "$marker" "$OUT/export.log" || true)"
    [ "$n" -eq 1 ] || { echo "FAIL: marker \"$marker\" appeared $n times (expected 1 — one RID)" >&2
                        cat "$OUT/export.log" >&2; exit 1; }
done
# The slot imported the bundle's android prebuilt runtime. The three
# cmake variables an Android slot configures are the key's, so
# `dist/package-toolchain.sh` has to spell them — NDK, ABI and API level — exactly
# as Dn2CppExporter.BuildDropIn does, out of two repositories that cannot share a
# constant. A drift is fail-safe and therefore silent: the slot builds the runtime
# and its vendored trees from source, correct and ~17 s slower, and nothing else
# here can tell. The line lands in the exporter's own log, which tees the
# configure's stdout; the engine's export.log never sees it.
EXPORTER_LOG="$(first_line "$(ls -t "$PROJ"/.godot/mono/temp/bin/dn2cpp/logs/export-*.log 2>/dev/null)")"
[ -n "$EXPORTER_LOG" ] || { echo "FAIL: the exporter wrote no log under .godot/mono/temp/bin/dn2cpp/logs" >&2; exit 1; }
grep -qF "using the prebuilt runtime" "$EXPORTER_LOG" || {
    echo "FAIL: the Android slot did not import the bundle's prebuilt runtime ($EXPORTER_LOG)" >&2
    grep -i "prebuilt" "$EXPORTER_LOG" >&2 || echo "  (the configure said nothing about a prebuilt at all)" >&2
    exit 1; }
[ -f "$APK" ] || { echo "FAIL: the export produced no APK at $APK" >&2; ls "$EXPORT_DIR" >&2; exit 1; }

echo "== 6/7 Asserting the APK =="
# The listing is taken ONCE, to a file. Every assertion below then greps that
# file rather than a pipe: under `set -o pipefail`, a `grep -q` against a piped
# `unzip -l` exits on its first match, unzip dies of SIGPIPE, and the pipeline
# reports a failure that never happened.
LIST="$OUT/apk-contents.txt"
unzip -l "$APK" > "$LIST"

SO_ENTRY="lib/arm64-v8a/lib$PROJECT_NAME.so"
grep -qF "$SO_ENTRY" "$LIST" \
    || { echo "FAIL: the APK does not carry $SO_ENTRY (the name the engine dlopens)" >&2
         grep -i '\.so' "$LIST" >&2; exit 1; }
# The pck directory GDMono checks for BEFORE it ever reaches the fallback. The
# drop-in ships as a shared object rather than a pck file, so nothing else in the
# export guarantees this directory exists — the empty publish manifest does, and
# if that ever stops being written the game aborts on device with "Unable to find
# the .NET assemblies directory" while every other assertion here still holds.
grep -qE "assets/\.godot/mono/publish/arm64/" "$LIST" \
    || { echo "FAIL: the pck has no .godot/mono/publish/arm64/ — GDMono aborts before the fallback" >&2
         exit 1; }

WORK="$OUT/apk"
rm -rf "$WORK"; mkdir -p "$WORK"
unzip -q -o "$APK" "$SO_ENTRY" -d "$WORK"
SO="$WORK/$SO_ENTRY"
desc="$(file -b "$SO")"
printf '%s: %s\n' "$SO_ENTRY" "$desc"
case "$desc" in
    *ELF*64-bit*aarch64*|*ELF*64-bit*ARM\ aarch64*) ;;
    *) echo "FAIL: the drop-in is not an ELF 64-bit aarch64 shared object" >&2; exit 1 ;;
esac
LLVM_NM="$(android_llvm_nm)" \
    || { echo "FAIL: no llvm-nm under $ANDROID_NDK_ROOT/toolchains/llvm/prebuilt/*/bin" >&2; exit 1; }
# -D: the dynamic symbol table is the only one a dlopen consumer resolves
# through. (grep without -q so it drains llvm-nm's output — under `set -o
# pipefail`, `grep -q` exits on the first match, llvm-nm dies of SIGPIPE, and the
# pipeline reports a bogus failure.)
"$LLVM_NM" -D --defined-only "$SO" | grep "godotsharp_game_main_init" >/dev/null \
    || { echo "FAIL: the drop-in does not export godotsharp_game_main_init" >&2; exit 1; }
echo "export OK: godotsharp_game_main_init (ELF dynamic)"

# GDMono only falls through to try_load_native_aot_library when it finds neither
# hostfxr nor coreclr. A .NET runtime in the APK would silently route the exported
# game back to .NET on-device and make the rest of this gate prove nothing. (The
# engine's own libgodot_android.so and the NDK's libc++_shared.so are the
# template's, not the publish's.)
stray="$(grep -iE '\.dll|hostfxr|coreclr|libmonosgen|System\..*\.so' "$LIST" || true)"
if [ -n "$stray" ]; then
    echo "FAIL: the APK contains .NET runtime remnants:" >&2
    printf '%s\n' "$stray" >&2
    exit 1
fi
echo "no .NET runtime in the APK"

echo "== 7/7 Refusing an ABI set the backend cannot serve =="
# arm64-v8a is all the backend builds. The exporter derives the publish
# architectures from the preset's *features*, and get_features() folds
# custom_features into that set — so a custom feature naming a second one turns
# the target set into {arm64, x86_64}, which Dn2CppExporter.Create refuses. The
# refusal must land before the publish: no transpile marker in the log.
BAD_APK="$PWD/$OUT/export-foreign-arch.apk"
BAD_LOG="$OUT/export-foreign-arch.log"
rm -f "$BAD_APK"

presets_tmp="$(mktemp)"
sed "/^\[preset\.2\]/,\$ s|custom_features=\"\"|custom_features=\"x86_64\"|" \
    "$PROJ/export_presets.cfg" > "$presets_tmp"
mv "$presets_tmp" "$PROJ/export_presets.cfg"
grep -q "custom_features=\"x86_64\"" "$PROJ/export_presets.cfg" \
    || { echo "FAIL: could not patch custom_features into the Android preset" >&2; exit 1; }

# Exit code ignored on purpose, as in 5/7: an export plugin's error is a warning
# to the editor. A regression here does the publish and the native build before
# it can go wrong, so the watchdog doubles as the budget for "refused early".
run_with_watchdog 900 "$FORK_EDITOR" --headless \
    --path "$PWD/$PROJ" --export-debug dn2cpp-android "$BAD_APK" \
    >"$BAD_LOG" 2>&1 || true

grep -q "ERROR: Export .NET Project" "$BAD_LOG" \
    || { echo "FAIL: the export plugin accepted an {arm64, x86_64} Android target" >&2
         cat "$BAD_LOG" >&2; exit 1; }
grep -qF "supports only the arm64 (arm64-v8a) architecture on Android" "$BAD_LOG" \
    || { echo "FAIL: the export failed, but not on the Android architecture guard" >&2
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

#!/usr/bin/env bash
# Godot editor-export E2E gate: the *forked editor* exports a C# game with
# `dotnet/export_backend = dn2cpp` and the exported desktop game runs in the real
# engine. It runs on the host-compiled desktop targets — macOS (a .app bundle),
# Windows (a bare .exe + data dir) and Linux (a bare <name>.<arch> + data dir) —
# selecting the preset, the template artifact and the artifact layout off
# $DN2CPP_OS; the OS-specific shapes are named once, in the seam just below the
# preflight, and the body reads only those variables.
#
# This is the oracle for the whole editor-export epic. It drives the pipeline the
# way a user does — through the desktop export command — and every stage in between is the
# fork's own code: ExportPlugin reads the new export option, BuildManager
# publishes the game IL, Dn2CppExporter transpiles it with the bundled native
# dn2cpp, compiles the generated C++ against the bundled runtime, and stages a
# single drop-in library which the platform exporter drops into
# data_<project>_<platform>_<arch>/ (under Contents/Resources/ on macOS; beside
# the .exe on Windows).
#
# The claims it pins down:
#   1. The exported game runs on the selected desktop template. Windows uses the
#      matching official .NET pair; macOS/Linux use the fork artifact. GDMono
#      only reaches try_load_native_aot_library because no hostfxr and no coreclr
#      sit next to the game. On Windows the drop-in links a fixture DLL staged
#      beside the executable, proving the official template's unchanged PE loader
#      can resolve project-declared native dependencies.
#   2. The bundle is the whole toolchain. The toolchain is re-packaged from the
#      *working tree* and installed into the fork editor before the export, so a
#      dn2cpp runtime or transpiler regression fails here. Including the build
#      tools: 5/14 asserts the export configured through the bundle's OWN cmake
#      and handed it the bundle's OWN ninja. That half cannot be proved by taking
#      the host's away, the way the hermetic Web gate proves the Emscripten SDK —
#      a desktop target compiles with a host clang++/cl.exe and can never be
#      hermetic — so it is read out of the log instead, and it has to be read out
#      of the log: a regression to the host's cmake builds the same game.
#   3. Re-exporting a work dir works and is incremental. 3/14 keeps the work dir
#      deliberately — the persisting .godot/mono/dn2cpp/build tree is what stops
#      a re-export recompiling the whole runtime — and every export after the
#      first takes that path. 7/14 flips the Incremental-GC preset OFF then ON in
#      that same slot, asserting both the CMake cache and real runtime mode while
#      the runtime archive is reused. 8/14 points the cache at another source
#      tree (which is what a moved or re-pointed toolchain leaves behind) and
#      asserts the export recovers, says so, and names both trees.
#   4. On Windows, the empty debug field selects the installed official debug
#      template. A later `--export-debug` uses it and cannot silently ship the
#      release executable under a debug label.
#   5. No specially-launched shell is needed. 10/14 exports from an environment
#      holding neither a C++ compiler on PATH nor INCLUDE/LIB/LIBPATH — what
#      Explorer hands a Windows editor — and asserts the editor found MSVC
#      itself and that cmake then compiled with that cl.exe. Windows-only, and
#      declared an expected partial elsewhere: MSVC exists nowhere else.
#   6. A target the host cannot serve is refused, and refused before the
#      publish. The last four sections re-export the same project with one
#      precondition broken at a time — the target's operating system, its
#      architecture, the host C++ compiler, the bundle's POSIX framework — and
#      each asserts the MESSAGE, not merely that the export died: all four kill
#      the export, and which sentence comes out is the whole content of the
#      guard. A refusal exists to convert a failure that would otherwise land
#      minutes later, inside a compiler or a linker that names none of them,
#      into one sentence; a refusal nothing reads back is a comment.
#   7. The macOS drop-in carries the selected architecture's deployment target
#      from the export preset. Both the CMake cache and the built Mach-O are
#      asserted: one alone would allow the configure or compiler half to drift.
#
# Requires the artifacts of gates/setup-godot-fork.sh (via DN2CPP_GODOT_FORK_ROOT,
# default ~/.cache/dn2cpp-godot-fork); prints SKIP and exits 0 when absent.
# Registered in run-all-gates.sh's SERIAL Godot phase — it launches the engine,
# and every launch runs under a watchdog because a broken Godot run hangs rather
# than fails.
source "$(dirname "$0")/_common.sh"
source "$(dirname "$0")/_godot_fork.sh"

OUT=gates/out-godot-editor-export
SAMPLE=samples/godot-dotnet/EditorExportSample
WINDOWS_DEPENDENCY_FIXTURE=gates/fixtures/godot-editor-export-windows-dependency
WINDOWS_DEPENDENCY_NAME=dn2cpp_editor_export_dependency
PROJECT_NAME=EditorExportSample
ARCH="$(godot_fork_host_arch)"

godot_fork_preflight

# ── Desktop-target seam ───────────────────────────────────────────────────────
# The host-compiled desktop targets differ in five shapes, and every one is the
# engine's or the OS's, not a preference: the preset name (a Windows Desktop
# preset cannot be named "macOS"), the export target path's extension (a bundle,
# a .exe, or the arch name Linux's get_binary_extensions returns), where the
# platform exporter drops the data dir (inside the bundle vs beside the
# executable), the drop-in's file name (the loader opens <name>.dylib on macOS,
# <name>.dll on Windows, <name>.so on Linux — see try_load_native_aot_library),
# and GODOT_PLATFORM.
#
# GODOT_PLATFORM is the engine's own name for the platform, as it appears in the
# data_<project>_<platform>_<arch> directory, and is NOT $DN2CPP_OS: the engine
# maps Linux to "linuxbsd". Substituting $DN2CPP_OS agrees on the two OSes where
# the names coincide and silently names a directory nothing writes on the third —
# which in 14/14 would make a negative assertion vacuously true.
# godot_editor_export_layout ARTIFACT sets DATA_DIR/DROPIN/GAME_EXE for a given
# export target, so the primary export and the foreign-arch negative test derive
# their paths one way.
case "$DN2CPP_OS" in
    macos)
        PRESET=dn2cpp-app
        GODOT_PLATFORM=macos
        EXPORT_EXT=app
        ;;
    windows)
        PRESET=dn2cpp-app-windows
        GODOT_PLATFORM=windows
        EXPORT_EXT=exe
        ;;
    linux)
        PRESET=dn2cpp-app-linux
        GODOT_PLATFORM=linuxbsd
        EXPORT_EXT="$ARCH"
        ;;
    *)
        gate_skip "the desktop editor-export gate has no $DN2CPP_OS arm (macOS, Windows and Linux only)"
        ;;
esac
EXPORT_TARGET="$PWD/$OUT/$PROJECT_NAME.$EXPORT_EXT"
# Resolved only after the case has admitted the host: godot_fork_desktop_template
# returns 1 for an OS it has no arm for, and under `set -e` that would turn the
# designed gate_skip above into a FAIL.
DESKTOP_TEMPLATE_DEBUG=
if [ "$DN2CPP_OS" = windows ]; then
    godot_template_version_dir "$FORK_EDITOR" >/dev/null \
        || { echo "FAIL: the fork editor has no parseable template version" >&2; exit 1; }
    [ "$GODOT_VERSION_FLAVOR" = .mono ] \
        || { echo "FAIL: the fork editor is not a .NET build: $GODOT_FULL_VERSION" >&2; exit 1; }
    DESKTOP_TEMPLATE="$(godot_official_windows_template "$GODOT_TEMPLATE_VERSION_DIR")"
    DESKTOP_TEMPLATE_DEBUG="$(godot_official_windows_template "$GODOT_TEMPLATE_VERSION_DIR" debug)"
else
    DESKTOP_TEMPLATE="$(godot_fork_desktop_template "$FORK_ROOT")"
fi

# godot_editor_export_layout ARTIFACT — resolve the post-export paths for a given
# export target. macOS packs the game into the bundle (data dir under
# Contents/Resources, the launchable binary under Contents/MacOS); Windows and
# Linux lay the data dir beside the executable, which IS the launchable.
godot_editor_export_layout() {
    local artifact="$1"
    case "$DN2CPP_OS" in
        macos)
            DATA_DIR="$artifact/Contents/Resources/data_${PROJECT_NAME}_${GODOT_PLATFORM}_$ARCH"
            DROPIN="$DATA_DIR/$PROJECT_NAME.dylib"
            GAME_EXE="$artifact/Contents/MacOS/$PROJECT_NAME"
            ;;
        windows)
            DATA_DIR="$(dirname "$artifact")/data_${PROJECT_NAME}_${GODOT_PLATFORM}_$ARCH"
            DROPIN="$DATA_DIR/$PROJECT_NAME.dll"
            GAME_EXE="$artifact"
            ;;
        linux)
            DATA_DIR="$(dirname "$artifact")/data_${PROJECT_NAME}_${GODOT_PLATFORM}_$ARCH"
            DROPIN="$DATA_DIR/$PROJECT_NAME.so"
            GAME_EXE="$artifact"
            ;;
    esac
}

if [ ! -f "$DESKTOP_TEMPLATE" ]; then
    if [ "$DN2CPP_OS" = windows ]; then
        gate_skip "no official release Windows template at $DESKTOP_TEMPLATE — install the matching Godot .NET export templates"
    fi
    gate_skip "no desktop template at $DESKTOP_TEMPLATE — run gates/setup-godot-fork.sh"
fi
if [ "$DN2CPP_OS" = windows ] && [ ! -f "$DESKTOP_TEMPLATE_DEBUG" ]; then
    gate_skip "no official debug Windows template at $DESKTOP_TEMPLATE_DEBUG — install the matching Godot .NET export templates"
fi

echo "== 1/14 Fork pin + interop-ABI tripwire =="
godot_fork_pin_abi_check

# The template is the second engine in this gate. Windows asks the installed
# executables for their exact official identity; other hosts verify the fork
# artifact's provenance before the cache check. The `tmpl=` key then records the
# bytes that were actually selected.
if [ "$DN2CPP_OS" = windows ]; then
    godot_official_windows_template_check "$DESKTOP_TEMPLATE" \
        "$GODOT_TEMPLATE_VERSION_DIR" "$BASE_COMMIT" \
        "official release Windows template" || exit 1
    godot_official_windows_template_check "$DESKTOP_TEMPLATE_DEBUG" \
        "$GODOT_TEMPLATE_VERSION_DIR" "$BASE_COMMIT" \
        "official debug Windows template" || exit 1
else
    godot_fork_template_check "$DESKTOP_TEMPLATE" "desktop export template" \
        "gates/setup-godot-fork.sh"
fi
DEBUG_TEMPLATE_SIG=none
if [ "$DN2CPP_OS" = windows ]; then
    DEBUG_TEMPLATE_SIG="$(file_sig "$DESKTOP_TEMPLATE_DEBUG")"
    if cmp -s "$DESKTOP_TEMPLATE" "$DESKTOP_TEMPLATE_DEBUG"; then
        echo "FAIL: the Windows release and debug desktop templates are byte-identical" >&2
        exit 1
    fi
fi

# No local transpile happens here — the forked editor drives the whole pipeline
# from the packaged toolchain — so the key is inputs-only: the self-hosted CLI,
# the packaging script, the sample project and the fork's pinned binaries (pins
# + editor/template identity in the context string; the runtime/ tree the
# packaging ships is in every key already). The pin/ABI tripwire above stays
# always-on, so a drifted fork still fails even on otherwise unchanged inputs.
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
    "godot-editor-export|os=$DN2CPP_OS|$(godot_fork_ctx)|tmpl=$(file_sig "$DESKTOP_TEMPLATE")|tmpl-debug=$DEBUG_TEMPLATE_SIG" \
    "$SELFHOST_BIN" \
    dist/package-toolchain.sh \
    "$SAMPLE" \
    "$WINDOWS_DEPENDENCY_FIXTURE" \
    "$ABI_EXPECTED"; then
    { gate_cache_hit_msg; exit 0; }
fi

echo "== 2/14 Installing the working tree's toolchain into the fork editor =="
# Packaging still runs every run — that is what keeps a dn2cpp-side regression
# visible here rather than only in the next setup-godot-fork.sh. The install is
# skipped when the content already matches, and otherwise lands as an atomic
# swap under a stage lock; see stage_editor_toolchain in gates/_common.sh.
stage_editor_toolchain "$FORK_GODOTSHARP" "$SELFHOST_BIN" "$OUT/package.log"

echo "== 3/14 Staging the sample project =="
# Copied rather than exported in place because the project accumulates .godot/,
# bin/ and obj/. Non-Windows hosts also patch in an absolute fork-template path;
# Windows deliberately preserves empty custom-template fields. The work dir is
# NOT wiped — its persistent build tree keeps re-export from recompiling the
# whole runtime.
PROJ="$OUT/project"
mkdir -p "$PROJ"
cp -R "$SAMPLE/." "$PROJ/"

# The stock Windows template does not change the DLL search path while opening
# the drop-in from its data directory. Build a dependency and declare it through
# the public project settings; the exporter must put the dependency beside the
# executable while leaving the drop-in alone in the engine-defined data dir.
if [ "$DN2CPP_OS" = windows ]; then
    WINDOWS_DEPENDENCY_BUILD="$OUT/windows-dependency"
    mkdir -p "$WINDOWS_DEPENDENCY_BUILD"
    _cmake_step "$WINDOWS_DEPENDENCY_BUILD/configure.log" \
        "configuring the Windows drop-in dependency fixture" \
        "$CMAKE" -S "$WINDOWS_DEPENDENCY_FIXTURE" -B "$WINDOWS_DEPENDENCY_BUILD" -G Ninja \
        -DCMAKE_BUILD_TYPE=Release -DCMAKE_C_COMPILER="${CMAKE_CXX_COMPILER}" || exit 1
    _cmake_step "$WINDOWS_DEPENDENCY_BUILD/build.log" \
        "building the Windows drop-in dependency fixture" \
        "$CMAKE" --build "$WINDOWS_DEPENDENCY_BUILD" || exit 1

    mkdir -p "$PROJ/native"
    cp "$WINDOWS_DEPENDENCY_BUILD/$WINDOWS_DEPENDENCY_NAME.dll" "$PROJ/native/"
    cp "$WINDOWS_DEPENDENCY_BUILD/$WINDOWS_DEPENDENCY_NAME.lib" "$PROJ/native/"

    project_tmp="$(mktemp)"
    awk -v dependency="$WINDOWS_DEPENDENCY_NAME" '
        { print }
        $0 == "project/assembly_name=\"EditorExportSample\"" {
            print "dn2cpp/extra_transpile_args=PackedStringArray(\"--direct-pinvoke\", \"" dependency "\")"
            print "dn2cpp/extra_link_libs=PackedStringArray(\"res://native/" dependency ".lib\")"
            print "dn2cpp/extra_shared_objects=PackedStringArray(\"res://native/" dependency ".dll\")"
        }
    ' "$PROJ/project.godot" > "$project_tmp"
    mv "$project_tmp" "$PROJ/project.godot"
fi

cat > "$PROJ/nuget.config" <<EOF
<?xml version="1.0" encoding="utf-8"?>
<!-- GENERATED by gates/build-and-run-godot-editor-export.sh — do not commit.
     Points the Godot.NET.Sdk resolver + restore at the fork's local feed. The
     feed path is native (godot_fork_native_path): the restore runs under the
     editor, and NuGet on Windows cannot read an MSYS /c/... feed. -->
<configuration>
  <packageSources>
    <clear />
    <add key="dn2cpp-godot-fork-local" value="$(godot_fork_native_path "$FORK_ROOT/nuget")" />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
  </packageSources>
</configuration>
EOF

# Windows deliberately leaves both custom-template fields empty. That makes the
# exporter select the matching official .NET pair from its version-keyed user
# directory, which is the public workflow this gate exists to protect. Other
# desktop hosts still exercise the fork-built artifact through an explicit path.
DESKTOP_TEMPLATE_NATIVE="$(godot_fork_native_path "$DESKTOP_TEMPLATE")"
if [ "$DN2CPP_OS" = windows ]; then
    awk '
        /^name=/ { windows = ($0 == "name=\"dn2cpp-app-windows\"") }
        windows && $0 == "custom_template/release=\"\"" { release = 1 }
        windows && $0 == "custom_template/debug=\"\"" { debug = 1 }
        windows && $0 == "binary_format/embed_pck=false" { sidecar = 1 }
        windows && $0 == "application/modify_resources=false" { unmodified = 1 }
        END { exit release && debug && sidecar && unmodified ? 0 : 1 }
    ' "$PROJ/export_presets.cfg" \
        || { echo "FAIL: the Windows preset does not use the official template pair unchanged" >&2; exit 1; }
else
    presets_tmp="$(mktemp)"
    sed "s|custom_template/release=\"\"|custom_template/release=\"$DESKTOP_TEMPLATE_NATIVE\"|" \
        "$PROJ/export_presets.cfg" > "$presets_tmp"
    mv "$presets_tmp" "$PROJ/export_presets.cfg"
    grep -qF "$DESKTOP_TEMPLATE_NATIVE" "$PROJ/export_presets.cfg" \
        || { echo "FAIL: could not patch custom_template/release into the preset" >&2; exit 1; }
fi

# Linux presets carry a checked-in x86_64 default, but this gate exports for the
# host. Patch only that preset; the macOS and Windows negative controls below
# must retain their own architecture.
if [ "$DN2CPP_OS" = linux ]; then
    presets_tmp="$(mktemp)"
    awk -v arch="$ARCH" '
        /^name="/ { linux = ($0 == "name=\"dn2cpp-app-linux\"") }
        linux && /^binary_format\/architecture=/ {
            print "binary_format/architecture=\"" arch "\""
            next
        }
        { print }
    ' "$PROJ/export_presets.cfg" > "$presets_tmp"
    mv "$presets_tmp" "$PROJ/export_presets.cfg"
    awk -v arch="$ARCH" '
        /^name="/ { linux = ($0 == "name=\"dn2cpp-app-linux\"") }
        linux && $0 == "binary_format/architecture=\"" arch "\"" { found = 1 }
        END { exit found ? 0 : 1 }
    ' "$PROJ/export_presets.cfg" \
        || { echo "FAIL: could not set the Linux preset architecture to $ARCH" >&2; exit 1; }
fi

# The first export deliberately omits the setting: absence is the compatibility
# case for existing presets and must select the documented default (ON).
if awk -v preset="$PRESET" '
    /^name=/ { selected = ($0 == "name=\"" preset "\"") }
    selected && /^dotnet\/dn2cpp\/incremental_gc=/ { found = 1 }
    END { exit found ? 0 : 1 }
' "$PROJ/export_presets.cfg"; then
    echo "FAIL: the fixture already spells dotnet/dn2cpp/incremental_gc; the first export" >&2
    echo "      would no longer prove the default for an existing preset" >&2
    exit 1
fi

# set_incremental_gc_preset VALUE — update only the active desktop preset. The
# option is inserted next to export_backend on its first use, then rewritten in
# place so OFF -> ON exercises one persistent CMake build slot.
set_incremental_gc_preset() {
    local value="$1" tmp
    tmp="$(mktemp)"
    awk -v preset="$PRESET" -v value="$value" '
        /^name=/ { selected = ($0 == "name=\"" preset "\"") }
        selected && /^dotnet\/dn2cpp\/incremental_gc=/ {
            print "dotnet/dn2cpp/incremental_gc=" value
            written = 1
            next
        }
        { print }
        selected && /^dotnet\/export_backend=/ && !written {
            print "dotnet/dn2cpp/incremental_gc=" value
            written = 1
        }
        END { exit written ? 0 : 1 }
    ' "$PROJ/export_presets.cfg" > "$tmp" \
        || { rm -f "$tmp"; echo "FAIL: could not set incremental GC on preset $PRESET" >&2; exit 1; }
    mv "$tmp" "$PROJ/export_presets.cfg"
}

echo "== 4/14 Importing the project (fork editor, headless) =="
# The first import may abort in editor doc-gen teardown (Godot headless bug,
# harmless — the import data is already written), so tolerate a non-zero exit.
run_with_watchdog 600 "$FORK_EDITOR" --headless \
    --path "$PWD/$PROJ" --import >"$OUT/import.log" 2>&1 || true

echo "== 5/14 Exporting with dotnet/export_backend = dn2cpp =="
# Generous watchdog: a cold .godot/mono/dn2cpp/build still has the whole game to
# compile, and falls back to compiling the runtime and its vendored trees too
# whenever the bundle's prebuilt does not describe this host.
# APP is the export target — a .app bundle on macOS, a bare .exe on Windows.
# The data dir is wiped explicitly: on macOS it lives inside the bundle so
# `rm -rf "$APP"` covers it, but on Windows it sits BESIDE the .exe in the
# never-wiped work dir — a previous run's copy would let a broken staging
# stale-pass (last run's drop-in still found) or false-fail the expected native
# library set asserted below.
APP="$EXPORT_TARGET"
godot_editor_export_layout "$APP"
rm -rf "$APP" "$DATA_DIR"
if ! godot_export_step 2400 "$OUT/export.log" "$APP" \
    "$FORK_EDITOR" --headless \
    --path "$PWD/$PROJ" --export-release "$PRESET" "$APP"; then
    echo "FAIL: --export-release failed (see below)" >&2
    cat "$OUT/export.log" >&2
    exit 1
fi
# An export plugin's Error fails the export, so godot_export_step above already
# asserted the exit code. This grep pins the MESSAGE: which plugin objected, and
# in a run whose exit code the teardown race made unreadable.
if grep -q "ERROR: Export .NET Project" "$OUT/export.log"; then
    echo "FAIL: the C# export plugin reported an error (see below)" >&2
    cat "$OUT/export.log" >&2
    exit 1
fi
for marker in "dn2cpp: transpiling" "dn2cpp: compiling the drop-in library" "dn2cpp: staged"; do
    grep -qF "$marker" "$OUT/export.log" \
        || { echo "FAIL: export log lacks the dn2cpp marker: $marker" >&2; cat "$OUT/export.log" >&2; exit 1; }
done
if [ "$DN2CPP_OS" = windows ] && ! cmp -s "$APP" "$DESKTOP_TEMPLATE"; then
    echo "FAIL: the release export executable is not the installed official release template" >&2
    echo "      export:   $(file_sig "$APP")" >&2
    echo "      template: $(file_sig "$DESKTOP_TEMPLATE")" >&2
    exit 1
fi

# The bundle's prebuilt runtime was ADOPTED, not merely shipped. Its whole
# effect is on time, so a refusal is invisible: the export still succeeds, still
# produces a byte-identical drop-in, and the only trace is the configure line
# below — which lands in the exporter's own log, not in the engine's. The failure
# this catches is a key the packaging and the consumer compute differently; both
# read runtime/cmake/dn2cpp_prebuilt.cmake, so a drift means one of them stopped.
# Newest log, taken here rather than at the end: sections 10-13 export again.
EXPORTER_LOG="$(first_line "$(ls -t "$PROJ"/.godot/mono/temp/bin/dn2cpp/logs/export-*.log 2>/dev/null)")"
[ -n "$EXPORTER_LOG" ] || { echo "FAIL: the exporter wrote no log under .godot/mono/temp/bin/dn2cpp/logs" >&2; exit 1; }
grep -q "using the prebuilt runtime" "$EXPORTER_LOG" || {
    echo "FAIL: the export did not use the bundle's prebuilt runtime ($EXPORTER_LOG)" >&2
    grep -i "prebuilt" "$EXPORTER_LOG" >&2 || echo "  (the configure said nothing about a prebuilt at all)" >&2
    exit 1; }

# The desktop export compiled through the BUNDLE's own cmake and ninja. Costs
# nothing — both facts are already in the logs this export wrote — and it is the
# only place they are checked on a host-compiled target: the hermetic Web gate
# takes the host's pair off PATH, which a desktop export cannot do because it
# still needs a host clang++/cl.exe beside them. A regression to the host's cmake
# is otherwise silent here, since the host's cmake builds the same game.
godot_fork_assert_bundled_buildtools "$OUT/export.log" "$EXPORTER_LOG" \
    "$FORK_GODOTSHARP/Dn2Cpp" "$OUT" || exit 1

# assert_export_artifact_and_run LOG — the artifact assertions and the real-engine
# run, shared by the first export and by the incremental release re-export of
# 7/14. Reads
# DATA_DIR / DROPIN / GAME_EXE, which the caller has resolved through
# godot_editor_export_layout. Factored out rather than copied: the two exports
# produce the same artifact and must be held to the same claims, and a second copy
# of a 40-line marker list is what drifts.
assert_export_artifact_and_run() {
    local LOG="$1"
    local expected_gc="${2:-incremental}"
    local DROPIN_NAME expected_staged staged rc n marker once bad
    DROPIN_NAME="$(basename "$DROPIN")"
    [ -f "$DROPIN" ] || { echo "FAIL: no drop-in library at $DROPIN" >&2; ls -R "$(dirname "$DATA_DIR")" >&2; exit 1; }
    # grep without -q so it drains dump_exports's output: under `set -o pipefail`,
    # `grep -q` exits on the first match, the producer dies of SIGPIPE, and the
    # pipeline reports a bogus failure. dump_exports reads the loader-visible export
    # set per platform (nm -gU on macOS, dumpbin //EXPORTS on Windows).
    dump_exports "$DROPIN" | grep "godotsharp_game_main_init" >/dev/null \
        || { echo "FAIL: $DROPIN does not export godotsharp_game_main_init" >&2; exit 1; }

    # GDMono only falls through to try_load_native_aot_library when it finds neither
    # hostfxr nor coreclr next to the game. A stray runtime here would silently route
    # the exported game back to .NET and make the rest of this gate prove nothing.
    expected_staged="$DROPIN_NAME"
    staged="$(LC_ALL=C ls -1 "$DATA_DIR" | LC_ALL=C sort)"
    expected_staged="$(printf '%s\n' "$expected_staged" | LC_ALL=C sort)"
    if [ "$staged" != "$expected_staged" ]; then
        echo "FAIL: the project data dir does not hold the expected native libraries:" >&2
        printf 'expected:\n%s\nactual:\n' "$expected_staged" >&2
        printf '%s\n' "$staged" >&2
        exit 1
    fi
    if [ "$DN2CPP_OS" = windows ] && [ ! -f "$(dirname "$GAME_EXE")/$WINDOWS_DEPENDENCY_NAME.dll" ]; then
        echo "FAIL: $WINDOWS_DEPENDENCY_NAME.dll was not copied beside the executable" >&2
        exit 1
    fi
    rc=0
    run_with_watchdog 120 env -u DN2CPP_GC_INCREMENTAL DN2CPP_GC_STATS=1 \
        "$GAME_EXE" --headless --quit-after 900 \
        >"$LOG" 2>&1 || rc=$?
    cat "$LOG"
    if [ "$rc" -ne 0 ]; then
        echo "FAIL: the exported game exited with $rc (137 = watchdog kill — a hung run)" >&2
        exit 1
    fi
    grep -qF "[dn2cpp] GC mode: $expected_gc" "$LOG" \
        || { echo "FAIL: exported game did not report GC mode $expected_gc" >&2; exit 1; }
    # The scripted node was constructed, tied to its native object (the scene's baked
    # Answer override arrived through the instance Set bridge), and driven by the
    # engine's main loop until it quit itself.
    for marker in \
        "DN2CPP_EXPORT_READY name=Main answer=42" \
        "DN2CPP_EXPORT_GDPRINT engine logger path ok" \
        "DN2CPP_EXPORT_PROCESS class=Node inTree=True deltaOk=True" \
        "DN2CPP_EXPORT_GC finalized=True bounded=True" \
        "DN2CPP_EXPORT_INTEROP resizeOk=True sized=True" \
        "DN2CPP_EXPORT_SIGNAL awaited=True" \
        "DN2CPP_EXPORT_CLOCK ticks=True elapsed=True" \
        "DN2CPP_EXPORT_CSPRNG publicEntropy=True" \
        "DN2CPP_EXPORT_DONE"; do
        grep -qF "$marker" "$LOG" || { echo "FAIL: missing marker: $marker" >&2; exit 1; }
    done
    if [ "$DN2CPP_OS" = windows ]; then
        grep -qF "DN2CPP_EXPORT_DEPENDENCY value=True" "$LOG" \
            || { echo "FAIL: the drop-in did not call its executable-directory dependency" >&2; exit 1; }
    fi
    for once in "DN2CPP_EXPORT_READY" "DN2CPP_EXPORT_GDPRINT" "DN2CPP_EXPORT_PROCESS" "DN2CPP_EXPORT_GC" \
        "DN2CPP_EXPORT_INTEROP" "DN2CPP_EXPORT_SIGNAL" "DN2CPP_EXPORT_CLOCK" "DN2CPP_EXPORT_CSPRNG" \
        "DN2CPP_EXPORT_DONE"; do
        n="$(grep -c "$once" "$LOG" || true)"
        [ "$n" -eq 1 ] || { echo "FAIL: marker $once appeared $n times (expected exactly 1)" >&2; exit 1; }
    done
    # The mono module must not have fallen over; it only logs and keeps running, so
    # these never affect the exit code.
    for bad in \
        "GodotPlugins initialization failed" \
        "Failed to load hostfxr" \
        "ERROR" \
        "SCRIPT ERROR" \
        "ObjectDB instances leaked"; do
        if grep -q "$bad" "$LOG"; then
            echo "FAIL: log contains \"$bad\"" >&2
            exit 1
        fi
    done
}

# assert_export_succeeded LOG — the success half of 5/14, for the re-exports
# below: the export plugin reported no error and the three dn2cpp stages ran.
# Each caller runs the export through godot_export_step, which asserts the exit
# code; this is the assertion on the message.
assert_export_succeeded() {
    local log="$1" what="$2" marker
    if grep -q "ERROR: Export .NET Project" "$log"; then
        echo "FAIL: $what failed (see below)" >&2
        cat "$log" >&2
        exit 1
    fi
    for marker in "dn2cpp: transpiling" "dn2cpp: compiling the drop-in library" "dn2cpp: staged"; do
        grep -qF "$marker" "$log" \
            || { echo "FAIL: $what: log lacks the dn2cpp marker: $marker" >&2; cat "$log" >&2; exit 1; }
    done
}

# NOCXX_PATH — this shell's PATH with every directory holding a host C++ compiler
# removed and nothing else touched, so what a section built on it observes is
# attributable to the compiler alone rather than to whatever else went missing
# with it. The names stripped are the ones HostCxxCompiler probes, no more: cl
# (Windows), clang++ and g++ (everywhere else); a wider set would strip
# directories the refusal cannot be attributed to.
#
# Shared by 10/14, which needs an environment no vcvars has touched, and by 12/14,
# which needs no compiler reachable at all. One way of stripping: two would be
# two definitions of "no compiler on PATH", and the sections would drift apart
# silently, each still green.
strip_host_cxx_from_path() {
    local rest="$PATH" entry out= exe suffix drop
    while [ -n "$rest" ]; do
        entry="${rest%%:*}"
        if [ "$entry" = "$rest" ]; then rest=; else rest="${rest#*:}"; fi
        [ -n "$entry" ] || continue
        drop=0
        for exe in cl clang++ g++; do
            for suffix in "" .exe .bat .cmd; do
                if [ -f "$entry/$exe$suffix" ]; then drop=1; fi
            done
        done
        if [ "$drop" -eq 1 ]; then continue; fi
        out="${out:+$out:}$entry"
    done
    printf '%s\n' "$out"
}
NOCXX_PATH="$(strip_host_cxx_from_path)"
for exe in cl clang++ g++; do
    if ( PATH="$NOCXX_PATH"; hash -r 2>/dev/null || true; command -v "$exe" >/dev/null 2>&1 ); then
        echo "FAIL: $exe is still reachable after stripping the compiler directories from PATH," >&2
        echo "      so both sections built on this PATH would assert nothing. PATH was: $PATH" >&2
        exit 1
    fi
done

echo "== 6/14 Asserting the artifact, then running it =="
# DATA_DIR / DROPIN / GAME_EXE come from the desktop-target seam: the data dir is
# inside the .app on macOS and beside the .exe on Windows, and the drop-in is
# <name>.dylib vs <name>.dll — the exact name the engine's WINDOWS/MACOS branch
# of try_load_native_aot_library opens.
godot_editor_export_layout "$APP"
assert_export_artifact_and_run "$OUT/run.log"

# ── Incremental release re-export (7..8/14) ───────────────────────────────────────────
# Everything above ran against a work dir that had never been exported before.
# Every export a user performs after the first does not, and 3/14 deliberately
# keeps that state: the persisting .godot/mono/dn2cpp/build tree is what makes a
# re-export cheap. Neither the cheapness nor the correctness of the second export
# had an oracle, because every gate ran exactly one export per work dir.
#
# The build slot is read out of the export log rather than reconstructed here. Its
# name is Dn2CppExporter's ($"{platform}-{buildConfig}-{rid}") and a second spelling
# of it in this gate would be a second thing to keep in agreement; the log line the
# exporter already prints names it, and a work dir carrying slots from earlier runs
# cannot confuse a value that came from THIS export.
BUILD_SLOT="$(LC_ALL=C sed -n 's/^dn2cpp: compiling the drop-in library (\(.*\))\.\.\.$/\1/p' "$OUT/export.log")"
BUILD_SLOT="${BUILD_SLOT%%$'\n'*}"
[ -n "$BUILD_SLOT" ] \
    || { echo "FAIL: could not read the build slot out of $OUT/export.log" >&2; exit 1; }
BUILD_DIR="$PROJ/.godot/mono/dn2cpp/build/$BUILD_SLOT"
CMAKE_CACHE="$BUILD_DIR/CMakeCache.txt"
[ -f "$CMAKE_CACHE" ] \
    || { echo "FAIL: no CMakeCache.txt in the build slot $BUILD_DIR" >&2; ls -la "$BUILD_DIR" >&2; exit 1; }

assert_incremental_gc_cache() {
    local expected="$1" actual
    actual="$(sed -n 's/^DN2CPP_GC_INCREMENTAL_DEFAULT:BOOL=//p' "$CMAKE_CACHE")"
    if [ "$actual" != "$expected" ]; then
        echo "FAIL: persistent CMake cache has DN2CPP_GC_INCREMENTAL_DEFAULT=" \
            "${actual:-<absent>}, expected $expected" >&2
        exit 1
    fi
}
assert_incremental_gc_cache ON

# A host SDK newer than the export target otherwise becomes clang's implicit
# deployment target. That makes the drop-in unloadable on systems the preset and
# template still support, while a successful build says nothing about the drift.
# Read the selected arm's preset value back at both boundaries: CMake received it
# and the final Mach-O recorded it.
if [ "$DN2CPP_OS" = macos ]; then
    CACHED_DEPLOYMENT_TARGET="$(sed -n \
        's/^CMAKE_OSX_DEPLOYMENT_TARGET:[A-Z][A-Z]*=//p' "$CMAKE_CACHE")"
    if [ "$CACHED_DEPLOYMENT_TARGET" != "$MACOS_DESKTOP_DEPLOYMENT_TARGET" ]; then
        echo "FAIL: CMake cached macOS deployment target" \
            "'${CACHED_DEPLOYMENT_TARGET:-<absent>}', expected" \
            "$MACOS_DESKTOP_DEPLOYMENT_TARGET from the arm64 preset" >&2
        exit 1
    fi

    MACHO_DEPLOYMENT_TARGET="$(vtool -show-build "$DROPIN" 2>/dev/null \
        | awk '/^[[:space:]]*minos / { print $2 }')"
    MACHO_EXPECTED_DEPLOYMENT_TARGET="$MACOS_DESKTOP_DEPLOYMENT_TARGET"
    if [ "$MACHO_DEPLOYMENT_TARGET" != "$MACHO_EXPECTED_DEPLOYMENT_TARGET" ]; then
        echo "FAIL: $DROPIN records macOS minos" \
            "'${MACHO_DEPLOYMENT_TARGET:-<absent>}', expected" \
            "$MACHO_EXPECTED_DEPLOYMENT_TARGET" \
            "from the arm64 preset" >&2
        exit 1
    fi
fi

# The witness for "the runtime was not recompiled" is the vendored Boehm GC's
# static library: it is built from the toolchain's own sources, nothing in the game
# can reach it, and it is among the largest things a cold slot compiles. Globbed
# rather than named, because what CMake calls a static library is the platform's
# rule (dn2cpp_gc.lib vs libdn2cpp_gc.a) — the same seam as the drop-in's suffix.
gc_lib_path() {
    local libs
    libs=$(ls "$BUILD_DIR"/*dn2cpp_gc.* 2>/dev/null || true)
    if [ -n "$libs" ]; then printf '%s\n' "${libs%%$'\n'*}"; fi
}
GC_LIB="$(gc_lib_path)"
# ...and on the prebuilt path there is no such library to read, because the
# runtime was never compiled into this slot at all. That absence is the same claim
# stated more strongly, so it becomes the witness — and it has to be asserted as an
# absence rather than skipped: a from-source build that lost its GC library would
# otherwise read as "prebuilt in use" and 7/14 would assert nothing. The two arms
# are picked apart by what the exporter's configure said, not by what is on disk.
if [ -z "$GC_LIB" ]; then
    grep -q "using the prebuilt runtime" "$EXPORTER_LOG" \
        || { echo "FAIL: no dn2cpp_gc library in the build slot $BUILD_DIR, and the export did" >&2
             echo "      not import a prebuilt runtime either — the witness this section reads" >&2
             echo "      does not exist, so it would assert nothing" >&2
             ls -la "$BUILD_DIR" >&2; exit 1; }
    [ ! -d "$BUILD_DIR/CMakeFiles/dn2cpp_runtime.dir" ] \
        || { echo "FAIL: the export imported the bundle's prebuilt runtime and compiled the" >&2
             echo "      runtime into $BUILD_DIR anyway" >&2
             exit 1; }
fi

echo "== 7/14 Re-exporting both GC defaults in the persisting build tree =="
# Flip the preset OFF and back ON in the same slot. The exporter must explicitly
# configure both values: relying on the CMake default would leave OFF cached on
# the final export. The runtime archive remains reusable because this default is
# compiled only into the app-specific .NET-module translation unit.
GC_SIG_BEFORE=""
[ -n "$GC_LIB" ] && GC_SIG_BEFORE="$(file_sig "$GC_LIB")"
set_incremental_gc_preset false
GC_OFF_LOG="$OUT/export-incremental-gc-off.log"
rm -rf "$APP" "$DATA_DIR"
if ! godot_export_step 1200 "$GC_OFF_LOG" "$APP" \
    "$FORK_EDITOR" --headless \
    --path "$PWD/$PROJ" --export-release "$PRESET" "$APP"; then
    echo "FAIL: the incremental-GC-OFF re-export failed (see below)" >&2
    cat "$GC_OFF_LOG" >&2
    exit 1
fi
assert_export_succeeded "$GC_OFF_LOG" "the incremental-GC-OFF re-export"
assert_incremental_gc_cache OFF
godot_editor_export_layout "$APP"
assert_export_artifact_and_run "$OUT/run-incremental-gc-off.log" stop-the-world

set_incremental_gc_preset true
REEXPORT_LOG="$OUT/export-incremental.log"
rm -rf "$APP" "$DATA_DIR"
if ! godot_export_step 1200 "$REEXPORT_LOG" "$APP" \
    "$FORK_EDITOR" --headless \
    --path "$PWD/$PROJ" --export-release "$PRESET" "$APP"; then
    echo "FAIL: the incremental --export-release failed (see below)" >&2
    cat "$REEXPORT_LOG" >&2
    exit 1
fi
assert_export_succeeded "$REEXPORT_LOG" "the incremental re-export"
assert_incremental_gc_cache ON
if grep -qF "dn2cpp: stale build cache reset" "$REEXPORT_LOG"; then
    echo "FAIL: the re-export discarded a build cache that was still current — the" >&2
    echo "      staleness test now fires on an unchanged toolchain, so every export" >&2
    echo "      recompiles the whole runtime (see 8/14 for the comparison it makes)" >&2
    cat "$REEXPORT_LOG" >&2
    exit 1
fi
godot_editor_export_layout "$APP"
assert_export_artifact_and_run "$OUT/run-incremental.log"
GC_LIB="$(gc_lib_path)"
if [ -n "$GC_SIG_BEFORE" ]; then
    if [ "$(file_sig "$GC_LIB")" != "$GC_SIG_BEFORE" ]; then
        echo "FAIL: the re-export recompiled the runtime — $GC_LIB changed" >&2
        echo "      ($GC_SIG_BEFORE -> $(file_sig "$GC_LIB")). The persisting build tree" >&2
        echo "      exists precisely so it does not." >&2
        exit 1
    fi
elif [ -n "$GC_LIB" ] || [ -d "$BUILD_DIR/CMakeFiles/dn2cpp_runtime.dir" ]; then
    echo "FAIL: the re-export compiled the runtime into $BUILD_DIR, which the first" >&2
    echo "      export had imported from the bundle's prebuilt — the slot fell back," >&2
    echo "      so every later export pays the build the prebuilt exists to remove." >&2
    exit 1
fi

echo "== 8/14 Recovering from a build cache configured from another source tree =="
# The persisting build tree is keyed on the export TARGET (platform, config, RID) —
# not on where the runtime sources it was configured from live. So every way the
# toolchain's path can move leaves a cache that names a tree cmake will not accept:
# a re-pointed dotnet/export/dn2cpp_toolchain_path, an editor whose
# GodotSharp/Dn2Cpp landed elsewhere than last time (which has happened for real, to
# every Windows work dir at once), a project copied off another machine. cmake then
# refuses with a message about the binary directory and CMakeCache.txt that names
# neither dn2cpp nor the one directory to delete.
#
# What is broken here is the CACHE's own declaration, not the editor's settings.
# The faithful re-point would write dotnet/export/dn2cpp_toolchain_path into
# %APPDATA%/Godot/editor_settings-4.7.tres — a file SHARED with every other Godot
# gate on this host — so the reproduction that looks more realistic is the one that
# perturbs everything else. Rewriting CMAKE_HOME_DIRECTORY puts the cache in
# exactly the state a moved toolchain leaves it in, and touches one file inside
# this gate's own work dir.
STALE_HOME="$(godot_fork_native_path "$PWD/$OUT")/stale-toolchain/runtime"
grep -q "^CMAKE_HOME_DIRECTORY:INTERNAL=" "$CMAKE_CACHE" \
    || { echo "FAIL: $CMAKE_CACHE holds no CMAKE_HOME_DIRECTORY:INTERNAL= line — the" >&2
         echo "      cache key this section rewrites is gone, so it would assert nothing" >&2; exit 1; }
cache_tmp="$(mktemp)"
sed "s|^CMAKE_HOME_DIRECTORY:INTERNAL=.*|CMAKE_HOME_DIRECTORY:INTERNAL=$STALE_HOME|" \
    "$CMAKE_CACHE" > "$cache_tmp"
mv "$cache_tmp" "$CMAKE_CACHE"
grep -qF "CMAKE_HOME_DIRECTORY:INTERNAL=$STALE_HOME" "$CMAKE_CACHE" \
    || { echo "FAIL: could not point $CMAKE_CACHE at a stale source tree" >&2; exit 1; }

# The witness that the tree was really discarded is a file put there for it. The
# reset is a RecreateDirectory, so nothing in the slot survives it — whereas the
# artifacts that used to serve as witnesses are all conditional now: the runtime's
# libraries are absent whenever the bundle's prebuilt is imported, and the
# drop-in's own bytes are deterministic, so a rebuilt one compares equal.
RESET_WITNESS="$BUILD_DIR/dn2cpp-gate-reset-witness"
: > "$RESET_WITNESS"
STALE_LOG="$OUT/export-stale-cache.log"
rm -rf "$APP" "$DATA_DIR"
if ! godot_export_step 2400 "$STALE_LOG" "$APP" \
    "$FORK_EDITOR" --headless \
    --path "$PWD/$PROJ" --export-release "$PRESET" "$APP"; then
    echo "FAIL: the export over a stale build cache failed (see below)" >&2
    cat "$STALE_LOG" >&2
    exit 1
fi
assert_export_succeeded "$STALE_LOG" "the export over a stale build cache"
# The recovery must be REPORTED, and report both paths: a silent recreate is
# indistinguishable from a build directory that was never populated, and the whole
# value of the line is telling a user which two trees disagreed.
grep -qF "dn2cpp: stale build cache reset" "$STALE_LOG" \
    || { echo "FAIL: the export survived a stale build cache without reporting the reset" >&2
         cat "$STALE_LOG" >&2; exit 1; }
grep -qF "$STALE_HOME" "$STALE_LOG" \
    || { echo "FAIL: the stale-cache reset does not name the tree the cache was configured from" >&2
         cat "$STALE_LOG" >&2; exit 1; }
# The other half of the line is a path this process did NOT write — the exporter
# prints Dn2CppToolchain.RuntimeDir, which carries the platform's own separator, so
# the line is slash-folded before being matched (the same fold the exporter's own
# comparison performs) and matched case-insensitively, because the host whose
# separator differs is also the one whose file system is case-insensitive.
# (No `grep -q` at the end of a pipeline: it exits on the first match, the producer
# dies of SIGPIPE, and under `set -o pipefail` the pipeline reports a bogus failure
# — the same trap as the dump_exports call above.)
grep -F "dn2cpp: stale build cache reset" "$STALE_LOG" | tr '\\' '/' \
    | grep -i "/Dn2Cpp/runtime" >/dev/null \
    || { echo "FAIL: the stale-cache reset does not name the toolchain's own runtime dir" >&2
         cat "$STALE_LOG" >&2; exit 1; }
godot_editor_export_layout "$APP"
assert_export_artifact_and_run "$OUT/run-stale-cache.log"
if [ -e "$RESET_WITNESS" ]; then
    echo "FAIL: the reset did not actually discard the build tree — $RESET_WITNESS" >&2
    echo "      is still there, so the reported reset did not happen" >&2
    exit 1
fi

# The Windows exporter consumes separate executables for release and debug.
# Prove the debug artifact crosses that final boundary: both custom fields stay
# empty, --export-debug succeeds through normal version-keyed selection, and the
# exported executable is byte-for-byte the debug input rather than the release
# input. The preset keeps both PCK embedding
# and PE resource modification off, so this is a direct comparison rather than
# an inference from a successful debug export.
echo "== 9/14 Exporting with the Windows debug template =="
if [ "$DN2CPP_OS" != windows ]; then
    gate_expected_partial "section 9 (the distinct debug desktop template export) has no reachable state on $DN2CPP_OS: Godot's Windows .NET template install supplies separate debug and release executables only on Windows. The uncovered surface is asserted by this same gate, gates/build-and-run-godot-editor-export.sh, section 9 on Windows, which drives --export-debug and compares the exported executable directly with both template inputs."
else
    DEBUG_DIR="$PWD/$OUT/debug-template"
    DEBUG_APP="$DEBUG_DIR/$PROJECT_NAME.exe"
    DEBUG_LOG="$OUT/export-debug-template.log"
    rm -rf "$DEBUG_DIR"
    mkdir -p "$DEBUG_DIR"
    if ! godot_export_step 1200 "$DEBUG_LOG" "$DEBUG_APP" \
        "$FORK_EDITOR" --headless \
        --path "$PWD/$PROJ" --export-debug "$PRESET" "$DEBUG_APP"; then
        echo "FAIL: --export-debug failed (see below)" >&2
        cat "$DEBUG_LOG" >&2
        exit 1
    fi
    assert_export_succeeded "$DEBUG_LOG" "the debug-template export"
    godot_editor_export_layout "$DEBUG_APP"
    assert_export_artifact_and_run "$OUT/run-debug-template.log"
    if ! cmp -s "$DEBUG_APP" "$DESKTOP_TEMPLATE_DEBUG"; then
        echo "FAIL: the debug export executable is not the selected debug template" >&2
        echo "      export:   $(file_sig "$DEBUG_APP")" >&2
        echo "      template: $(file_sig "$DESKTOP_TEMPLATE_DEBUG")" >&2
        exit 1
    fi
    if cmp -s "$DEBUG_APP" "$DESKTOP_TEMPLATE"; then
        echo "FAIL: --export-debug emitted the release template executable" >&2
        echo "      shared signature: $(file_sig "$DEBUG_APP")" >&2
        exit 1
    fi
    godot_editor_export_layout "$APP"
fi

echo "== 10/14 Exporting from a shell that never ran vcvars =="
# The launch every Windows user actually performs. MSVC is on no machine's PATH
# and its INCLUDE/LIB/LIBPATH exist only inside a shell that has run vcvarsall,
# so the editor resolves them itself — vswhere, then vcvarsall for the host arch
# — and overlays the result onto its build children alone. Without that import
# this export is 12/14's refusal instead, which is why the two share one PATH.
#
# THIS shell HAS run vcvars (ensure_msvc_env, sourced from gates/_common.sh), so
# what the editor inherits is sanitized rather than inherited: the stripped PATH
# and the three MSVC variables emptied, which the import reads as unset. Emptied
# rather than `env -u`d: an empty value is what a shell that never ran vcvars
# hands a child, and covering it is the point. %ProgramFiles(x86)% is left alone
# — finding the real install is the whole subject here, and 12/14 is the section
# that takes it away.
if [ "$DN2CPP_OS" != windows ]; then
    # One line: run-all-gates.sh's summary takes the FIRST line of the reason.
    gate_expected_partial "section 10 (the vcvars-less export) has no reachable state on $DN2CPP_OS: MSVC exists only on Windows and Dn2CppExporter imports it only under OS.IsWindows, so no PATH or environment this gate could hand the editor produces the state the section drives. Structural and permanent, not an absent prerequisite — nothing installable on a POSIX host creates it, which is why this is not a gate_skip/gate_partial. The uncovered surface IS asserted for real by this same gate, gates/build-and-run-godot-editor-export.sh, section 10, run on a Windows host, where the import runs and the section asserts it end to end."
else
    # Cold, deliberately: a warm slot skips cmake's compiler detection entirely,
    # and the cache witness below would then be the PREVIOUS export's answer.
    # The runtime still comes from the bundle's prebuilt (5/14 asserts the
    # adoption), so only the game is compiled.
    rm -rf "$BUILD_DIR"
    VCVARSLESS_LOG="$OUT/export-no-vcvars.log"
    rm -rf "$APP" "$DATA_DIR"
    if ! godot_export_step 2400 "$VCVARSLESS_LOG" "$APP" \
        env TERM=dumb PATH="$NOCXX_PATH" INCLUDE= LIB= LIBPATH= \
        "$FORK_EDITOR" --headless \
        --path "$PWD/$PROJ" --export-release "$PRESET" "$APP"; then
        echo "FAIL: the export from a vcvars-less environment failed (see below)" >&2
        cat "$VCVARSLESS_LOG" >&2
        exit 1
    fi
    assert_export_succeeded "$VCVARSLESS_LOG" "the export from a vcvars-less environment"
    # Two witnesses, because a green export proves neither on its own. Without
    # the first, cl.exe reached the editor by some path the strip missed and this
    # section drove the ordinary case; without the second, the import ran but
    # cmake compiled with something else and the overlay bought nothing.
    grep -qF "dn2cpp: msvc " "$VCVARSLESS_LOG" \
        || { echo "FAIL: the export succeeded without the editor importing MSVC — cl.exe was" >&2
             echo "      reachable some other way, so this section asserted nothing" >&2
             cat "$VCVARSLESS_LOG" >&2; exit 1; }
    [ -f "$CMAKE_CACHE" ] \
        || { echo "FAIL: no CMakeCache.txt in $BUILD_DIR after the cold export" >&2; exit 1; }
    # `|| true`: an absent line is the failure this reports, not a reason to die
    # in the command substitution with nothing said.
    CACHED_CXX="$(grep -E "^CMAKE_CXX_COMPILER:[A-Z]+=" "$CMAKE_CACHE" | tr '\\' '/' || true)"
    grep -qi "/cl\.exe$" <<<"$CACHED_CXX" \
        || { echo "FAIL: cmake did not configure with the imported cl.exe: ${CACHED_CXX:-(no CMAKE_CXX_COMPILER line)}" >&2
             exit 1; }
    godot_editor_export_layout "$APP"
    assert_export_artifact_and_run "$OUT/run-no-vcvars.log"
fi

# ── Refusal oracles (11..14/14) ────────────────────────────────────────────────
# Everything below re-exports the same staged project with exactly one
# precondition broken, and every one of them must die in Dn2CppExporter.Create,
# before a single byte is published. They run in this order because the last one
# is the only one that MUTATES the preset file (a custom_features rewrite that
# every preset in the file inherits); the three added here read the presets as
# staged, so putting them ahead of it keeps them independent of it.
#
# The export target of each gets its own subdirectory. A refused export still
# runs the platform exporter's own packaging afterwards, and the good artifact
# plus its data dir sit in $OUT: sharing a directory would let one of these
# overwrite what 6/14 just asserted, or let a stale copy answer for a fresh one.
#
# godot_export_refused RC LOG WHAT — the half every refusal assert shares: the
# export failed (non-zero exit and the editor's own verdict), the C# export
# plugin is what refused it, and it refused before the publish. The caller then
# greps for the sentence identifying WHICH refusal fired, because all four of
# them look identical here.
godot_export_refused() {
    local rc="$1" log="$2" what="$3"
    [ "$rc" -ne 0 ] \
        || { echo "FAIL: the export was refused but exited 0 ($what)" >&2; cat "$log" >&2; exit 1; }
    grep -qE "Project export for preset .* failed\." "$log" \
        || { echo "FAIL: the editor did not report the refused export as failed ($what)" >&2
             cat "$log" >&2; exit 1; }
    grep -q "ERROR: Export .NET Project" "$log" \
        || { echo "FAIL: the export plugin accepted $what" >&2; cat "$log" >&2; exit 1; }
    if grep -qF "dn2cpp: transpiling" "$log"; then
        echo "FAIL: $what was refused only after the publish had run" >&2
        cat "$log" >&2
        exit 1
    fi
}

echo "== 11/14 Refusing an export whose target OS is not the host's =="
# The sharpest of the four, and the one whose absence costs the most. The backend
# compiles the game with the host's own C++ compiler, so the target OPERATING
# SYSTEM is as fixed as the architecture — but an architecture test alone lets a
# foreign host straight through whenever the names happen to agree: an x86_64
# Windows box exporting an x86_64 macOS preset matches on the arch and fails on
# nothing else, so it publishes, transpiles and compiles for minutes before dying
# inside a toolchain that names no cause.
#
# So this asserts two things, and the second is the load-bearing one: that the
# export is refused, and that it is refused BY THE OS GUARD rather than by the
# architecture guard. The two desktop hosts have opposite native arches, so the
# foreign preset mismatches on both counts — which is precisely why the message
# that comes out is the only observable evidence of which test ran first.
#
# Host-independent except for the one thing that cannot be: which preset counts
# as foreign. Each desktop host throws the other's, and both are real.
case "$DN2CPP_OS" in
    macos)   FOREIGN_PRESET=dn2cpp-app-windows; FOREIGN_TARGET_OS=Windows; FOREIGN_OS_EXT=exe ;;
    windows) FOREIGN_PRESET=dn2cpp-app;         FOREIGN_TARGET_OS=macOS;   FOREIGN_OS_EXT=app ;;
    # Linux throws the macOS preset for the Windows host's reason, and gets the
    # same mismatch on both counts: the macOS preset is arm64 and this host is not.
    linux)   FOREIGN_PRESET=dn2cpp-app;         FOREIGN_TARGET_OS=macOS;   FOREIGN_OS_EXT=app ;;
esac
# On a macOS host the foreign preset needs its own custom template before the
# guard is even observable: EditorExportPlatformWindows::export_project validates
# the custom template's PE architecture (_get_exe_arch) at its very top — BEFORE
# EditorExportPlatformPC::export_project, whose ExportNotifier is what fires the
# C# plugins' _ExportBegin — and the staging of 3/14 left the host's own macOS
# binary in that slot, so the engine refuses on "Mismatching custom export
# template executable architecture" and the OS guard under test never runs. The
# arch-wording oracle below polices ordering INSIDE the plugin (OS test before
# arch test); this engine check is a different, host-specific layer, neutralized
# by handing the preset the minimal file _get_exe_arch parses: u32 e_lfanew at
# 0x3C, "PE\0\0" where it points, u16 machine 0x8664 (x86_64). It never reads
# "MZ". A Windows host's foreign macOS preset also needs a concrete path because
# an absent installed macos.zip is rejected before the plugin. The macOS exporter
# does not inspect that path's architecture on Windows, so the already-verified
# official Windows release executable is sufficient to reach the host-OS guard.
if [ "$DN2CPP_OS" = macos ]; then
    FOREIGN_STUB="$PWD/$OUT/refuse-host-os-template.exe"
    {
        printf 'MZ'
        dd if=/dev/zero bs=1 count=58 2>/dev/null
        printf '\x40\x00\x00\x00'
        printf 'PE\x00\x00'
        printf '\x64\x86'
    } > "$FOREIGN_STUB"
    # Only the Windows preset's line is rewritten — the macOS preset must keep
    # $DESKTOP_TEMPLATE_NATIVE, because 12/14 and 14/14 re-export it afterwards.
    presets_tmp="$(mktemp)"
    awk -v stub="$FOREIGN_STUB" '
        /^name="/ { preset = $0 }
        preset == "name=\"dn2cpp-app-windows\"" && /^custom_template\/release=/ {
            print "custom_template/release=\"" stub "\""
            next
        }
        { print }
    ' "$PROJ/export_presets.cfg" > "$presets_tmp"
    mv "$presets_tmp" "$PROJ/export_presets.cfg"
    grep -qF "$FOREIGN_STUB" "$PROJ/export_presets.cfg" \
        || { echo "FAIL: could not point the foreign preset at the PE stub template" >&2; exit 1; }
    grep -qF "$DESKTOP_TEMPLATE_NATIVE" "$PROJ/export_presets.cfg" \
        || { echo "FAIL: the stub patch clobbered the host preset's template" >&2; exit 1; }
elif [ "$DN2CPP_OS" = windows ]; then
    presets_tmp="$(mktemp)"
    awk -v tmpl="$DESKTOP_TEMPLATE_NATIVE" '
        /^name="/ { preset = $0 }
        preset == "name=\"dn2cpp-app\"" && /^custom_template\/release=/ {
            print "custom_template/release=\"" tmpl "\""
            next
        }
        { print }
    ' "$PROJ/export_presets.cfg" > "$presets_tmp"
    mv "$presets_tmp" "$PROJ/export_presets.cfg"
    grep -qF "$DESKTOP_TEMPLATE_NATIVE" "$PROJ/export_presets.cfg" \
        || { echo "FAIL: could not give the foreign macOS preset a template path" >&2; exit 1; }
fi
FOREIGN_OS_DIR="$PWD/$OUT/refuse-host-os"
FOREIGN_OS_LOG="$OUT/export-host-os.log"
rm -rf "$FOREIGN_OS_DIR"
mkdir -p "$FOREIGN_OS_DIR"
foreign_os_rc=0
run_with_watchdog 600 "$FORK_EDITOR" --headless \
    --path "$PWD/$PROJ" --export-release "$FOREIGN_PRESET" \
    "$FOREIGN_OS_DIR/$PROJECT_NAME.$FOREIGN_OS_EXT" >"$FOREIGN_OS_LOG" 2>&1 || foreign_os_rc=$?

godot_export_refused "$foreign_os_rc" "$FOREIGN_OS_LOG" \
    "a $FOREIGN_TARGET_OS target on a $DN2CPP_OS host"
grep -qF "export to $FOREIGN_TARGET_OS has to run on $FOREIGN_TARGET_OS" "$FOREIGN_OS_LOG" \
    || { echo "FAIL: the export failed, but not on the host-OS guard" >&2
         cat "$FOREIGN_OS_LOG" >&2; exit 1; }
for arch_worded in "cannot cross-compile" "Cross-architecture export is not supported"; do
    if grep -qF "$arch_worded" "$FOREIGN_OS_LOG"; then
        echo "FAIL: a foreign-OS export was refused as an architecture problem (\"$arch_worded\")," >&2
        echo "      so the architecture test now runs before the OS test — a host that matches" >&2
        echo "      on the arch would be let through to the publish." >&2
        cat "$FOREIGN_OS_LOG" >&2
        exit 1
    fi
done

echo "== 12/14 Refusing a host with no C++ compiler to be found =="
# The probe is HostCxxCompiler(), and on Windows it no longer reads the PATH
# alone: the editor searches for MSVC itself (10/14), so a stripped PATH stops
# being a host without a compiler there. What has to be simulated instead is a
# machine with NO Visual Studio — %ProgramFiles(x86)% and %ProgramFiles% pointed
# at an empty directory, the only place the search looks for vswhere.exe, plus
# the emptied INCLUDE/LIB/LIBPATH of a shell that never ran vcvars. Still
# nothing uninstalled, and still one thing taken away at a time.
#
# The substitution is scoped to this export alone. Those two variables also
# steer GodotTools' MSBuild and SDK probing, and a refusal runs no publish, so
# nothing outside this section is exposed to a machine with no Program Files.
NOCXX_DIR="$PWD/$OUT/refuse-no-cxx"
NOCXX_LOG="$OUT/export-no-cxx.log"
rm -rf "$NOCXX_DIR"
mkdir -p "$NOCXX_DIR"
# The sabotage is scoped to the EDITOR PROCESS alone (env), not exported
# around run_with_watchdog: the watchdog's own machinery (ps, tr) and the
# $(basename) below live in /usr/bin, which the strip removes on every macOS
# host because it holds clang++ — a subshell-wide export ran them all crippled.
#
# TERM must be pinned, and it is load-bearing, not cosmetic: on macOS a Godot
# process that sees NO TERM decides it was launched from Finder and re-imports
# the login shell's whole environment — OS_MacOS::load_shell_environment()
# sources /etc/zprofile (path_helper included) and OVERWRITES PATH — so under a
# TERM-less launcher (launchd, cron) the editor un-strips its own PATH, finds
# /usr/bin/clang++, and this section's refusal never fires while the compile
# quietly succeeds. Any TERM value disables the import; "dumb" is the honest
# one for a headless run.
NOCXX_ENV=(TERM=dumb PATH="$NOCXX_PATH")
if [ "$DN2CPP_OS" = windows ]; then
    mkdir -p "$NOCXX_DIR/no-program-files"
    NO_VS="$(cygpath -w "$NOCXX_DIR/no-program-files")"
    NOCXX_ENV+=("ProgramFiles(x86)=$NO_VS" "ProgramFiles=$NO_VS" INCLUDE= LIB= LIBPATH=)
fi
nocxx_rc=0
run_with_watchdog 600 env "${NOCXX_ENV[@]}" "$FORK_EDITOR" --headless \
    --path "$PWD/$PROJ" --export-release "$PRESET" \
    "$NOCXX_DIR/$(basename "$EXPORT_TARGET")" >"$NOCXX_LOG" 2>&1 || nocxx_rc=$?

godot_export_refused "$nocxx_rc" "$NOCXX_LOG" "a host with no C++ compiler to be found"
grep -qF "missing tools it cannot build without" "$NOCXX_LOG" \
    || { echo "FAIL: the export failed, but not on the C++ toolchain guard" >&2
         cat "$NOCXX_LOG" >&2; exit 1; }
# clang++ is named by every host arm of the missing-tool list, so this one grep
# holds wherever the gate runs. The remedy is the half that differs, and it is
# the half that has to be right: a macOS user is told about the Command Line
# Tools, a Linux user their own package manager, a Windows user to install the
# workload — never to relaunch from a Developer Command Prompt, which 10/14 is the
# proof they do not need.
grep -qF "clang++" "$NOCXX_LOG" \
    || { echo "FAIL: the toolchain refusal does not name the compiler it wanted" >&2
         cat "$NOCXX_LOG" >&2; exit 1; }
case "$DN2CPP_OS" in
    macos)   REMEDY_NEEDLE="xcode-select --install" ;;
    windows) REMEDY_NEEDLE="Visual Studio C++ workload" ;;
    linux)   REMEDY_NEEDLE="apt install clang" ;;
esac
grep -qF "$REMEDY_NEEDLE" "$NOCXX_LOG" \
    || { echo "FAIL: the toolchain refusal carries no $DN2CPP_OS remedy (\"$REMEDY_NEEDLE\")" >&2
         cat "$NOCXX_LOG" >&2; exit 1; }
# A machine with no VS and one whose VS the search did not reach need different
# answers, and only the second is served by the old advice. So the Windows
# refusal must also name what did the searching: without it the two cases are
# one sentence, and the user whose install is simply somewhere else is told to
# install what they already have.
if [ "$DN2CPP_OS" = windows ]; then
    grep -qF "vswhere.exe" "$NOCXX_LOG" \
        || { echo "FAIL: the Windows toolchain refusal does not name vswhere.exe, so it cannot" >&2
             echo "      distinguish a machine with no Visual Studio from one whose install the" >&2
             echo "      search did not reach" >&2
             cat "$NOCXX_LOG" >&2; exit 1; }
fi

echo "== 13/14 Refusing a cross-target export against a bundle with no POSIX framework =="
# What the transpiler consumes is the CoreLib's IL, so its flavour decides which
# native libraries the emitted P/Invokes name. Off a Windows host every target
# but Windows itself has to be transpiled against the POSIX framework the bundle
# stages as ref-posix/ — without it the transpile SUCCEEDS and the failure lands
# in a linker asking for kernel32 or ole32, naming no CoreLib flavour and no
# export backend.
if [ "$DN2CPP_OS" != windows ]; then
    # Not a provisioning hole and not a skip: on a POSIX host
    # Dn2CppToolchain.NeedsCrossCoreLib is false for every target, because ref/
    # already IS the POSIX flavour — so dist/package-toolchain.sh stages no
    # ref-posix/ here and there is no state in which this refusal can fire. The
    # refusal is a property of Windows hosts, and its oracle is one too.
    # Declared rather than echoed: a bare echo reduces a section's
    # coverage in a log nobody reads, and the runner's summary is the only
    # place a hole of this size can reach anybody. Structural and permanent on
    # this host, not an absent prerequisite: Dn2CppToolchain.NeedsCrossCoreLib
    # is false for every target on a POSIX host because ref/ already IS the
    # POSIX flavour, so dist/package-toolchain.sh stages no ref-posix/ here and
    # nothing this gate could install would create a state in which the refusal
    # fires. The refusal is a property of Windows hosts and so is its oracle.
    # One line, not a wrapped paragraph: the runner's summary takes the FIRST
    # line of the reason (run-all-gates.sh's `sed … | head -1`), so a reason
    # spread over several lines is reported truncated.
    gate_expected_partial "section 13 (the POSIX-framework refusal) has no reachable state on $DN2CPP_OS: Dn2CppToolchain.NeedsCrossCoreLib is false for every target here because ref/ is already the POSIX flavour, so dist/package-toolchain.sh stages no ref-posix/ for the section to hide and the guard under test cannot fire. Permanent rather than environmental — no installation on a POSIX host creates the cross-CoreLib state, which is why this is not a gate_skip/gate_partial prerequisite. The uncovered surface IS asserted for real by this same gate, gates/build-and-run-godot-editor-export.sh, run on a Windows host, where ref-posix/ is staged and section 13 hides it and asserts the refusal end to end. Sections 1-8, 11, 12 and 14 of this run did run and hold."
else
    # The refusal needs a CROSS target, and Web is the one this repository can
    # always reach: Android would need an installed export template, iOS an Apple
    # SDK. What it does not need is a working Emscripten — Create resolves an SDK
    # before the POSIX-framework guard but never runs it, taking the bundle's
    # emsdk/ when there is one and otherwise probing emcmake/em++ through
    # OS.PathWhich. So a bundle staged with an SDK needs nothing here, and one
    # staged without is served by a directory carrying two files of those names.
    # The stubs only keep the section off the provisioning list; PathWhich on
    # Windows resolves a bare name through PATHEXT, so .bat is the spelling found.
    NOPOSIX_DIR="$PWD/$OUT/refuse-no-posix-ref"
    NOPOSIX_LOG="$OUT/export-no-posix-ref.log"
    rm -rf "$NOPOSIX_DIR"
    mkdir -p "$NOPOSIX_DIR/stub-emsdk"
    for stub in emcmake em++; do
        printf '@echo off\r\nexit /b 1\r\n' > "$NOPOSIX_DIR/stub-emsdk/$stub.bat"
    done

    # This section stops in the export plugin before a Web template is opened,
    # but Godot's preset validation still requires a path. Reuse the verified
    # official executable as an existence witness; the Windows preset itself
    # remains empty and continues to exercise normal template selection.
    presets_tmp="$(mktemp)"
    awk -v tmpl="$DESKTOP_TEMPLATE_NATIVE" '
        /^name="/ { preset = $0 }
        preset == "name=\"dn2cpp-web\"" && /^custom_template\/release=/ {
            print "custom_template/release=\"" tmpl "\""
            next
        }
        { print }
    ' "$PROJ/export_presets.cfg" > "$presets_tmp"
    mv "$presets_tmp" "$PROJ/export_presets.cfg"

    # Hidden rather than deleted, and restored through a trap: the staged
    # toolchain is the fork editor's own, shared with every other editor-export
    # gate, and a run that died between the move and the restore would leave the
    # next Web or Android export failing on this very refusal for real.
    REF_POSIX="$FORK_GODOTSHARP/Dn2Cpp/ref-posix"
    REF_POSIX_HIDDEN="$FORK_GODOTSHARP/Dn2Cpp/.ref-posix.hidden.$$"
    if [ ! -f "$REF_POSIX/System.Private.CoreLib.dll" ]; then
        echo "FAIL: the staged toolchain carries no $REF_POSIX/System.Private.CoreLib.dll," >&2
        echo "      which dist/package-toolchain.sh stages on a Windows host and now asserts" >&2
        exit 1
    fi
    restore_ref_posix() {
        if [ -d "$REF_POSIX_HIDDEN" ]; then mv "$REF_POSIX_HIDDEN" "$REF_POSIX"; fi
    }
    # A hook, not a bare `trap … EXIT`: this gate holds the machine lock for
    # its whole run (gate_machine_lock in _common.sh), and the shell's single
    # trap slot would evict that lock's release. The restore is idempotent, so
    # the hook stays registered after the explicit call below.
    gate_add_exit_hook restore_ref_posix
    mv "$REF_POSIX" "$REF_POSIX_HIDDEN"
    noposix_rc=0
    (
        export PATH="$NOPOSIX_DIR/stub-emsdk:$PATH"
        run_with_watchdog 600 "$FORK_EDITOR" --headless \
            --path "$PWD/$PROJ" --export-release dn2cpp-web "$NOPOSIX_DIR/index.html"
    ) >"$NOPOSIX_LOG" 2>&1 || noposix_rc=$?
    restore_ref_posix
    [ -f "$REF_POSIX/System.Private.CoreLib.dll" ] \
        || { echo "FAIL: could not restore $REF_POSIX" >&2; exit 1; }

    godot_export_refused "$noposix_rc" "$NOPOSIX_LOG" \
        "a cross-target export against a bundle with no POSIX framework"
    for needle in "carries no POSIX framework" "ref-posix" "dist/package-toolchain.sh"; do
        grep -qF "$needle" "$NOPOSIX_LOG" \
            || { echo "FAIL: the export failed, but not on the POSIX-framework guard (no \"$needle\")" >&2
                 cat "$NOPOSIX_LOG" >&2; exit 1; }
    done
    if grep -qF "compiles the game for the Web with Emscripten, which is not on" "$NOPOSIX_LOG"; then
        echo "FAIL: the Emscripten probe refused first, so the POSIX-framework guard never ran" >&2
        cat "$NOPOSIX_LOG" >&2
        exit 1
    fi
fi

echo "== 14/14 Refusing an architecture the host cannot compile for =="
# The backend compiles the game with the host's own compiler, so the host's own
# architecture is the only one it can produce. It used to decide that from the
# preset's *features*, while the packaging loop iterates the preset's resolved
# *architectures* — and a custom feature naming a second architecture adds one
# without tripping either check (get_features() folds custom_features into the
# set the plugin sees). The loop then stored a host-compiled library under the
# other architecture's data directory — where the machine that reaches for it is
# precisely the one that cannot load it, and the export machine never notices.
#
# The export must therefore fail, and fail before the publish: no transpile
# marker, no data directory for the architecture that was smuggled in. The two
# desktop hosts have opposite native arches, so the foreign one is whichever the
# host is not — arm64 on an x86_64 Windows box, x86_64 on an arm64 Mac.
if [ "$ARCH" = "arm64" ]; then FOREIGN_ARCH=x86_64; else FOREIGN_ARCH=arm64; fi
BAD_APP="$PWD/$OUT/$PROJECT_NAME-foreign-arch.$EXPORT_EXT"
BAD_LOG="$OUT/export-foreign-arch.log"
# The smuggled-in arch's data dir path, resolved like the good export's but for
# the foreign arch. Wiped up front for the same reason as the good export's:
# on Windows it sits beside the exe in the never-wiped work dir, and a stale
# copy would false-fail the must-not-exist assert below.
# $GODOT_PLATFORM, never $DN2CPP_OS: the two agree on macOS and Windows and part
# on Linux ("linuxbsd"), and a directory name nothing ever writes would make the
# absence asserted below vacuously true.
BAD_ARCH_DATA_DIR="$(dirname "$BAD_APP")/data_${PROJECT_NAME}_${GODOT_PLATFORM}_$FOREIGN_ARCH"
[ "$DN2CPP_OS" = macos ] && BAD_ARCH_DATA_DIR="$BAD_APP/Contents/Resources/data_${PROJECT_NAME}_${GODOT_PLATFORM}_$FOREIGN_ARCH"
rm -rf "$BAD_APP" "$BAD_ARCH_DATA_DIR"

presets_tmp="$(mktemp)"
sed "s|custom_features=\"\"|custom_features=\"$FOREIGN_ARCH\"|" \
    "$PROJ/export_presets.cfg" > "$presets_tmp"
mv "$presets_tmp" "$PROJ/export_presets.cfg"
grep -q "custom_features=\"$FOREIGN_ARCH\"" "$PROJ/export_presets.cfg" \
    || { echo "FAIL: could not patch custom_features into the preset" >&2; exit 1; }

# A regression here does the whole publish and native build before it can go
# wrong, so the watchdog doubles as the budget for "refused early".
bad_arch_rc=0
run_with_watchdog 600 "$FORK_EDITOR" --headless \
    --path "$PWD/$PROJ" --export-release "$PRESET" "$BAD_APP" >"$BAD_LOG" 2>&1 || bad_arch_rc=$?

godot_export_refused "$bad_arch_rc" "$BAD_LOG" "a $FOREIGN_ARCH target on $ARCH"
grep -qF "cannot cross-compile" "$BAD_LOG" \
    || { echo "FAIL: the export failed, but not on the architecture guard" >&2
         cat "$BAD_LOG" >&2; exit 1; }
# The smuggled-in arch's data dir must not have been packaged (its path was
# resolved, and any stale copy wiped, above the export).
if [ -e "$BAD_ARCH_DATA_DIR" ]; then
    echo "FAIL: a $FOREIGN_ARCH data dir was packaged on an $ARCH host: $BAD_ARCH_DATA_DIR" >&2
    ls -R "$BAD_ARCH_DATA_DIR" >&2
    exit 1
fi

# Tripwire: a concurrent suite re-staging the shared toolchain mid-run must
# surface as this self-explaining FAIL, not as a cryptic include error inside
# the export above.
assert_editor_toolchain_current "$FORK_GODOTSHARP"
gate_cache_commit
echo "OK"

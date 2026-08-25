#!/usr/bin/env bash
# Godot .NET-module (mono module) handshake E2E gate: a real engine binary built
# from the pinned godotengine/godot clone loads the dn2cpp-produced mono-module
# library through the module's NativeAOT fallback, initialization succeeds, and
# the engine runs a scriptless scene for a few frames and exits cleanly.
#
# Flow: build the mono-module dylib (shared pipeline with the lib gate) -> pack
# the scriptless handshake scene with the mono *editor* (--export-pack, preset
# "handshake-pack" whose custom_features carries the "dotnet" feature the
# template's GDMono::should_initialize() gates on, plus "handshake" which flips
# the feature-tagged run/main_scene override to the handshake scene) -> assemble
# a loose run dir laid out like an exported game (godot + godot.pck +
# data_DotnetSample_<platform>_<arch>/DotnetSample.<dylib|dll|so>, the exact name/layout
# try_load_native_aot_library resolves; the data dir holds ONLY our library so
# the hostfxr/coreclr probes miss and the NativeAOT branch runs) -> run headless
# with DN2CPP_DM_TRACE=1 and assert the positive stage markers (a failed .NET
# init only logs an error — the engine keeps running and exits 0, so absence of
# errors proves nothing).
#
# Requires the artifacts of gates/setup-godot-dotnet.sh, found at that script's
# default root (DN2CPP_GODOT_DOTNET_ROOT overrides; here: editor_bin +
# template_bin too); skips when absent. Registered in run-all-gates.sh's SERIAL Godot phase
# (it launches the engine); every engine/editor launch runs under a watchdog —
# a broken Godot run hangs rather than fails.
source "$(dirname "$0")/_common.sh"
source "$(dirname "$0")/_godot_dotnet.sh"

OUT=gates/out-godot-dotnet-handshake
ROOT="$GODOT_DOTNET_ROOT"
PINNED_COMMIT=ed1daf0bf001b61586d9930840f2f1394092c079
ABI_EXPECTED=gates/expected/godot-dotnet-abi.sha256

if ! godot_dotnet_root_ok || [ ! -x "$GODOT_DOTNET_EDITOR" ] || [ ! -x "$GODOT_DOTNET_TEMPLATE" ] \
    || [ ! -f "$ROOT/pin.txt" ]; then
    gate_skip "godot-dotnet artifacts absent/incomplete at $ROOT — run gates/setup-godot-dotnet.sh (or set DN2CPP_GODOT_DOTNET_ROOT)"
fi

echo "== 1/6 Pin + interop-ABI tripwire =="
godot_dotnet_pin_abi_check "$PINNED_COMMIT" "$ABI_EXPECTED"

echo "== 2/6 Building the mono-module shared library =="
godot_dotnet_transpile "$OUT"
# Everything past this point — the native link, import, export-pack, the engine
# run and its marker asserts — is a pure function of the transpile surface in
# $OUT, the gate-helper set the key hashes wholesale, the sample project,
# the app/GodotSharp
# assemblies and the pinned engine binaries (pin + editor/template identity live
# in the context string). The pin/ABI tripwire above stays always-on, so a
# drifted clone still fails on a hit. The link is deliberately below the check:
# a hit exits here and never names $DYLIB, so building it first is pure waste.
if gate_cache_check "$OUT" \
    "godot-dotnet-handshake|pin=$(file_text "$ROOT/pin.txt")|editor=$(file_sig_deref "$GODOT_DOTNET_EDITOR")|template=$(file_sig_deref "$GODOT_DOTNET_TEMPLATE")" \
    "$GODOT_DOTNET_SAMPLE_DIR/.godot/mono/temp/bin/ExportRelease/DotnetSample.dll" \
    "$GODOT_DOTNET_GODOTSHARP" \
    "$GODOT_DOTNET_SAMPLE_DIR"; then
    { gate_cache_hit_msg; exit 0; }
fi
godot_dotnet_link_lib "$OUT"
DYLIB="$OUT/$(lib_name DotnetSample)"

echo "== 3/6 Importing the sample project (mono editor, headless) =="
RUN="$OUT/run"
rm -rf "$RUN"
mkdir -p "$RUN"
# The first import may abort in editor doc-gen teardown (Godot headless bug,
# harmless — the import data is already written), so tolerate a non-zero exit.
run_with_watchdog 300 "$GODOT_DOTNET_EDITOR" --headless \
    --path "$GODOT_DOTNET_SAMPLE_DIR" --import >"$RUN/import.log" 2>&1 || true

echo "== 4/6 Exporting the handshake pack (--export-pack) =="
# Named after the run dir's engine binary: a release export template rejects
# every path/scene CLI override (--main-pack, positional scene — SCons
# disable_path_overrides defaults to on), so the only way it finds a project is
# the exported-game convention <exe_basename>.pck next to the executable, and
# the only way it picks the handshake scene is the feature-tagged
# run/main_scene override baked into the pck (see the preset + project.godot).
PCK="$PWD/$RUN/godot.pck"
if ! godot_export_step 300 "$RUN/export.log" "$PCK" \
    "$GODOT_DOTNET_EDITOR" --headless \
    --path "$GODOT_DOTNET_SAMPLE_DIR" --export-pack handshake-pack "$PCK"; then
    echo "FAIL: --export-pack handshake-pack failed (see below)" >&2
    cat "$RUN/export.log" >&2
    exit 1
fi
[ -f "$PCK" ] || { echo "FAIL: export produced no $PCK" >&2; cat "$RUN/export.log" >&2; exit 1; }

echo "== 5/6 Assembling the loose run dir =="
# try_load_native_aot_library opens <exe_dir>/data_<AssemblyName>_<platform>_<arch>/
# <AssemblyName>.<dylib|dll|so> (no lib prefix — the NativeAOT publish naming,
# not ours). Both halves are per-platform and neither is $DN2CPP_OS: the engine
# maps Linux to "linuxbsd" (see GODOT_DOTNET_PLATFORM in _godot_dotnet.sh).
ARCH="$(godot_dotnet_host_arch)"
cp -L "$GODOT_DOTNET_TEMPLATE" "$RUN/godot$EXE_EXT"
chmod +x "$RUN/godot$EXE_EXT"
mkdir -p "$RUN/data_DotnetSample_${GODOT_DOTNET_PLATFORM}_$ARCH"
cp -f "$DYLIB" "$RUN/data_DotnetSample_${GODOT_DOTNET_PLATFORM}_$ARCH/DotnetSample.$LIB_EXT"

echo "== 6/6 Running the handshake scene in the real engine =="
LOG="$RUN/handshake.log"
rc=0
run_with_watchdog 120 env DN2CPP_DM_TRACE=1 "$PWD/$RUN/godot$EXE_EXT" --headless \
    --quit-after 5 >"$LOG" 2>&1 || rc=$?
cat "$LOG"
if [ "$rc" -ne 0 ]; then
    echo "FAIL: engine exited with $rc (137 = watchdog kill — a hung run)" >&2
    exit 1
fi
# Positive markers: every init stage reached, and the engine's main loop drove
# the wrapped FrameCallback at least once.
for marker in \
    "dn2cpp-dm: interop received" \
    "dn2cpp-dm: managed callbacks written" \
    "dn2cpp-dm: init ok" \
    "dn2cpp-dm: frame"; do
    grep -q "$marker" "$LOG" \
        || { echo "FAIL: missing marker: $marker" >&2; exit 1; }
done
# Negative markers: the mono module must not have fallen over (it only logs and
# keeps running, so these never affect the exit code).
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
gate_cache_commit
echo "OK"

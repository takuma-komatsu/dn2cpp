#!/usr/bin/env bash
# Godot .NET-module (mono module) library gate: transpiles the real
# Godot.NET.Sdk sample game (samples/godot-dotnet/DotnetSample) against the
# real GodotSharp.dll with the --dotnet-module backend and links the result
# into the mono-module shared library the engine loads in place of the
# hostfxr-managed game assembly (NativeAOT drop-in style). Asserts that the
# library exports godotsharp_game_main_init and that every dn2cpp_dm_* host
# symbol resolved — no engine run here (that is the handshake gate,
# gates/build-and-run-godot-dotnet-handshake.sh).
#
# Section 3 cross-builds the SAME generated C++ for Android through the NDK.
# The drop-in is what an exported APK carries in lib/<abi>/, and the engine
# there dlopens it by bare soname — so the assertion is the ELF shape plus the
# same export surface, in the ELF spelling (no leading underscore). It is the
# axis the editor's Android export backend compiles through, and the only place
# a bionic/NDK regression in the .NET-module host would surface before an
# export. Machines without an NDK run the gate without it (gate_partial).
#
# Requires the artifacts of gates/setup-godot-dotnet.sh, found at that script's
# default root (DN2CPP_GODOT_DOTNET_ROOT overrides): the real GodotSharp API
# assembly + the local nuget feed for the Godot.NET.Sdk resolver. Skips when
# absent, so the suite stays usable without the ~40-minute engine build.
source "$(dirname "$0")/_common.sh"
source "$(dirname "$0")/_godot_dotnet.sh"

OUT=gates/out-godot-dotnet-lib

if ! godot_dotnet_root_ok; then
    gate_skip "godot-dotnet artifacts absent/incomplete at $GODOT_DOTNET_ROOT — run gates/setup-godot-dotnet.sh (or set DN2CPP_GODOT_DOTNET_ROOT)"
fi

echo "== 1/3 Building the mono-module shared library =="
godot_dotnet_transpile "$OUT"

# Keyed on the transpile surface, and checked BEFORE the native link: the link
# is the most expensive step in this gate and contributes nothing to the key, so
# a warm hit must not pay for it. The link itself lives in
# gates/_godot_dotnet.sh, whose content the key hashes with the rest of the
# gate-helper set (_gate_helpers_hash) rather than per gate. The NDK root
# sits in the context so gaining/losing the Android toolchain re-runs the gate
# rather than serving the other flavor's result — and its source.properties
# CONTENT hash beside it, so an in-place NDK upgrade under the same path misses
# too (path alone cannot see one).
ndk_fp=none
[ -n "${ANDROID_NDK_ROOT:-}" ] && [ -f "$ANDROID_NDK_ROOT/source.properties" ] \
    && ndk_fp=$(shasum -a 256 "$ANDROID_NDK_ROOT/source.properties" | awk '{print $1}')
if gate_cache_check "$OUT" "godot-dotnet-lib|ndk:${ANDROID_NDK_ROOT:-none}|ndkfp:$ndk_fp" \
        "$GODOT_DOTNET_SAMPLE_DIR/.godot/mono/temp/bin/ExportRelease/DotnetSample.dll" \
        "$GODOT_DOTNET_GODOTSHARP" \
        "$GODOT_DOTNET_SAMPLE_DIR"; then
    gate_cache_hit_msg
    exit 0
fi
godot_dotnet_link_lib "$OUT"
DYLIB="$OUT/$(lib_name DotnetSample)"

echo "== 2/3 Asserting the export surface =="
# The engine resolves godotsharp_game_main_init from the loaded library.
# (grep without -q so it drains the dump's full output — `grep -q` exits on the
# first match, the tool dies of SIGPIPE, and pipefail turns that into a bogus
# failure.)
if ! dump_exports "$DYLIB" | grep "godotsharp_game_main_init" >/dev/null; then
    echo "FAIL: $DYLIB does not export godotsharp_game_main_init" >&2
    exit 1
fi
echo "export OK: godotsharp_game_main_init"
# Link sanity: the host runtime's symbols must all have resolved statically.
if [ "$DN2CPP_OS" = windows ]; then
    # Nothing to read, and nothing to check: a PE image has no way to *record* an
    # unresolved external — link.exe fails with LNK2019 instead — so the DLL
    # existing is the assertion. The POSIX arm below is not redundant, it asks a
    # question that only has an answer there.
    echo "link OK: no undefined dn2cpp_dm_* symbols (a PE link cannot leave one)"
# Unanchored, exactly like the ELF twin in section 3, and that is the whole
# point: the host functions are declared in runtime/dotnetmodule/
# dn2cpp_dotnetmodule.h with NO `extern "C"`, so an undefined reference is
# Itanium-mangled (__Z38dn2cpp_dm_set_managed_frame_callback…) and the character
# before dn2cpp_dm_ is the length digit, never an underscore. A `_dn2cpp_dm_`
# pattern therefore could never match on Mach-O — the assert had never been able
# to fail. The two genuinely extern "C" names are DEFINED, so they never
# appear under nm -u at all.
elif nm -u "$DYLIB" | grep "dn2cpp_dm_" >&2; then
    echo "FAIL: $DYLIB has undefined dn2cpp_dm_* symbols (above)" >&2
    exit 1
else
    echo "link OK: no undefined dn2cpp_dm_* symbols"
fi

echo "== 3/3 Cross-building the drop-in for Android (NDK) =="
if [ -z "${ANDROID_NDK_ROOT:-}" ] || [ ! -f "$ANDROID_NDK_ROOT/build/cmake/android.toolchain.cmake" ]; then
    # A SECTION opt-out, not a whole-gate one: the host build above did run and
    # did hold. Point ANDROID_NDK_ROOT at an installed NDK to activate this.
    gate_partial "no Android NDK under ANDROID_NDK_ROOT — the Android cross-build section did not run"
    gate_cache_commit   # records status:partial with the reason above
    echo "OK"
    exit 0
fi
LLVM_NM="$(android_llvm_nm)" \
    || { echo "FAIL: no llvm-nm under $ANDROID_NDK_ROOT/toolchains/llvm/prebuilt/*/bin" >&2; exit 1; }

# The engine dlopens the bare soname libDotnetSample.so out of the APK's
# lib/<abi>/, so the library is named for the assembly, not for the gate.
SO="$OUT/libDotnetSample.so"
compile_dotnet_module_android "$OUT" "$SO"

desc="$(file -b "$SO")"
printf '%s: %s\n' "$SO" "$desc"
case "$desc" in
    *ELF*64-bit*aarch64*|*ELF*64-bit*ARM\ aarch64*) ;;
    *) echo "FAIL: not an ELF 64-bit aarch64 shared library: $SO" >&2; exit 1 ;;
esac

# The same two asserts as section 2, in the ELF spelling: -D reads the dynamic
# symbol table (the only one a dlopen consumer can resolve through), and ELF
# carries no leading underscore. (grep without -q so it drains llvm-nm's output —
# see the note in section 2.)
if ! "$LLVM_NM" -D --defined-only "$SO" | grep "godotsharp_game_main_init" >/dev/null; then
    echo "FAIL: $SO does not export godotsharp_game_main_init" >&2
    exit 1
fi
echo "export OK: godotsharp_game_main_init (ELF dynamic)"
if "$LLVM_NM" -D --undefined-only "$SO" | grep "dn2cpp_dm_" >&2; then
    echo "FAIL: $SO has undefined dn2cpp_dm_* symbols (above)" >&2
    exit 1
fi
echo "link OK: no undefined dn2cpp_dm_* symbols (ELF dynamic)"
gate_cache_commit
echo "OK"

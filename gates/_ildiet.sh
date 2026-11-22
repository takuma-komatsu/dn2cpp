#!/usr/bin/env bash
# Source after _common.sh. Native dn2cpp and ILDiet run without a CLR.
# Resolve only the published generation; flat paths can observe partial output.
ildiet_generation() {
    local output="$1"
    gate_python - "$output" <<'PYTHON'
from pathlib import Path
import re
import sys
import xml.etree.ElementTree as ET
output = Path(sys.argv[1])
root = ET.parse(output / "ildiet-result.xml").getroot()
generation = root.get("generation", "")
if root.tag != "ildietResult" or root.get("version") != "2" or not re.fullmatch(r"g-[0-9a-f]{16}", generation):
    raise SystemExit("invalid ILDiet generation manifest: " + str(output))
path = output / generation
if not path.is_dir():
    raise SystemExit("missing ILDiet generation: " + str(path))
sys.stdout.reconfigure(encoding="utf-8")
sys.stdout.write(path.as_posix())
PYTHON
}

# The selected BCL may come from an installed SDK or a runtime NuGet pack.
ildiet_runtime_notices() {
    local corelib="$1" directory notice depth license
    [ "$DN2CPP_OS" != windows ] || corelib="$(cygpath -u "$corelib")"
    directory="$(dirname "$corelib")"
    for depth in 0 1 2 3; do
        for notice in THIRD-PARTY-NOTICES.TXT ThirdPartyNotices.txt; do
            for license in LICENSE.txt LICENSE.TXT; do
                if [ -f "$directory/$license" ] && [ -f "$directory/$notice" ]; then
                    install -m 0644 "$directory/$license" "$2/runtime-LICENSE.TXT"
                    install -m 0644 "$directory/$notice" "$2/runtime-THIRD-PARTY-NOTICES.TXT"
                    return 0
                fi
            done
        done
        directory="$(dirname "$directory")"
    done
    echo "error: no .NET redistribution notices beside the selected CoreLib: $1" >&2
    return 1
}

ildiet_assert_native() {
    local directory="$1" notice
    [ -x "$directory/ILDiet$EXE_EXT" ] || {
        echo "error: native ILDiet is missing: $directory/ILDiet$EXE_EXT" >&2; return 1; }
    for notice in LICENSE.txt Mono.Cecil.LICENSE.txt runtime-LICENSE.TXT runtime-THIRD-PARTY-NOTICES.TXT; do
        [ -f "$directory/$notice" ] || {
            echo "error: ILDiet redistribution notice is missing: $directory/$notice" >&2; return 1; }
    done
    # A managed payload can start on the build machine while still requiring CLR.
    [ ! -e "$directory/ILDiet.dll" ] && [ ! -e "$directory/Mono.Cecil.dll" ] \
        && [ ! -e "$directory/ILDiet.runtimeconfig.json" ] || {
        echo "error: ILDiet payload still carries a managed companion: $directory" >&2; return 1; }
}

ildiet_emit_native() {
    local out="$1" corelib="$2"
    local bin="src/Dn2Cpp.Cli/bin/$CONFIG/$TFM" diet="src/ILDiet/bin/$CONFIG/$TFM"
    mkdir -p "$out"
    build_proj src/Dn2Cpp.Cli/Dn2Cpp.Cli.csproj 2>&1 | tee "$out/managed-build.log"
    # ILDiet never supplies a signing key. Deterministic MVID hashing stays live.
    dotnet exec "$bin/dn2cpp.dll" "$diet/ILDiet.dll" \
        -r "$diet/Mono.Cecil.dll" -r "$corelib" \
        --cut 'Mono.Cecil.CryptoService::GetPublicKey' \
        --cut 'Mono.Cecil.CryptoService::StrongName' \
        --auto-ref --no-ildiet -o "$out" 2>&1 | tee "$out/emit.log"
    [ -f "$out/generated.cpp" ] || { echo "error: no generated.cpp" >&2; return 1; }
}

ildiet_stage_native() (
    local destination="$1" corelib out key compiler name notice
    # The companion always executes on the build host, including cross exports.
    unset WASM IOS_DEV IOS_SIM ANDROID
    corelib="$(resolve_pinned_corelib)"
    [ "$DN2CPP_OS" != windows ] || corelib="$(cygpath -u "$corelib")"
    out="artifacts/ildiet-native-$CONFIG-$DN2CPP_OS-$(uname -m)"
    compiler="$(command -v "${CMAKE_CXX_COMPILER:-${CXX:-c++}}")"
    key=$(
        {
            printf '%s\n' "$PWD" "$CONFIG" "$TFM" "$DN2CPP_OS" "$(uname -m)" "$corelib"
            src_tree_hash || exit 1
            _gate_helpers_hash || exit 1
            _gate_dotnet_hash || exit 1
            dotnet --version || exit 1
            _gate_paths_hash "$(dirname "$corelib")" || exit 1
            _gate_transpiler_env_term || exit 1
            _gate_dotnet_env_term || exit 1
            file_sig_deref "$compiler" || exit 1
            for name in SCALAR HIGHWAY DN2CPP_NO_GC DN2CPP_GC_BACKEND DN2CPP_NO_CURL PAL_REFERENCE \
                    DN2CPP_HIGHWAY_ARCH DN2CPP_EXTRA_CMAKE_ARGS DN2CPP_EXTRA_LINK_FLAGS DN2CPP_EXTRA_LINK_LIBS \
                    DN2CPP_CMAKE_RUNTIME_EXPORT CMAKE CMAKE_CXX_COMPILER CXX CFLAGS CXXFLAGS LDFLAGS; do
                printf '%s=%s\n' "$name" "${!name:-}"
            done
        } | shasum -a 256 | awk '{print $1}'
    )
    if [ "${DN2CPP_GATE_CACHE:-1}" != 0 ] && [ -x "$out/ILDiet$EXE_EXT" ] \
            && [ "$(cat "$out/native-build.key" 2>/dev/null)" = "$key" ]; then
        echo "-- reusing native ILDiet: $out/ILDiet$EXE_EXT"
    else
        mkdir -p "$out"
        rm -f "$out/native-build.key"
        ildiet_emit_native "$out" "$corelib"
        compile_console "$out" ILDiet 2>&1 | tee "$out/native-build.log"
        dotnet exec "src/ILDiet/bin/$CONFIG/$TFM/ILDiet.dll" --help \
            > "$out/managed-help.txt" 2> "$out/managed-help.stderr.log"
        "$out/ILDiet$EXE_EXT" --help > "$out/native-help.txt" 2> "$out/native-help.stderr.log"
        diff -u "$out/managed-help.txt" "$out/native-help.txt"
        printf '%s\n' "$key" > "$out/native-build.key"
    fi
    # Every caller replaces one companion directory, never a toolchain root.
    [ "$(basename "$destination")" = ildiet ] || {
        echo "error: ILDiet staging destination must name an ildiet directory: $destination" >&2; return 1; }
    mkdir -p "$(dirname "$destination")"
    destination="$(cd "$(dirname "$destination")" && pwd -P)/ildiet"
    rm -rf "$destination"
    mkdir -p "$destination/licenses"
    install -m 0755 "$out/ILDiet$EXE_EXT" "$destination/ILDiet$EXE_EXT"
    [ ! -d "$out/.dn2cpp-native" ] || cp -R "$out/.dn2cpp-native" "$destination/"
    install -m 0644 LICENSE "$destination/LICENSE.txt"
    install -m 0644 src/ILDiet/Mono.Cecil.LICENSE.txt "$destination/"
    ildiet_runtime_notices "$corelib" "$destination"
    for notice in bdwgc/LICENSE bdwgc-upstream/LICENSE brotli/LICENSE curl/COPYING \
            highway/LICENSE mbedtls/LICENSE nghttp2/COPYING zlib/LICENSE; do
        install -m 0644 "third_party/$notice" "$destination/licenses/${notice%/*}.txt"
    done
    ildiet_assert_native "$destination"
)

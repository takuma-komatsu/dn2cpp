#!/usr/bin/env bash
# Hosted Linux prerequisites only; pre-merge.sh owns selection and the verdict.
set -euo pipefail
cd "$(dirname "$0")/.."

: "${RUNNER_TEMP:?GitHub runner temporary directory required}"
: "${GITHUB_ENV:?GitHub environment file required}"
: "${DN2CPP_PREMERGE_LOGROOT:?pre-merge evidence directory required}"
[[ "$(uname -s):$(uname -m)" == Linux:x86_64 ]] || {
    echo "error: this setup aid requires Linux x86_64" >&2
    exit 1
}
TOOLS="$RUNNER_TEMP/dn2cpp-premerge-tools-$GITHUB_RUN_ID-$GITHUB_RUN_ATTEMPT"
[[ ! -e "$TOOLS" ]] || { echo "error: refusing to reuse $TOOLS" >&2; exit 1; }
mkdir -p "$TOOLS/downloads"
df -h "$PWD" "$RUNNER_TEMP"

sudo apt-get update
sudo apt-get install -y --no-install-recommends \
    clang-19 cmake ninja-build libmpfr6 libgmp10 binutils openssl \
    curl ca-certificates git unzip xz-utils
export CC=clang-19 CXX=clang++-19
export DN2CPP_EMSDK="$TOOLS/emsdk"
export DN2CPP_GODOT_CLONE="$TOOLS/godot-source"
export DN2CPP_GODOT_DOWNLOAD_CACHE="$TOOLS/downloads"
export DN2CPP_GODOT_DOTNET_ROOT="$TOOLS/godot-dotnet"
export ANDROID_NDK_ROOT="$TOOLS/ndk/android-ndk-r26d"
export DN2CPP_DECLANG_COMPILER="$TOOLS/declang/Release-Linux-swift5.10-v1.0.0-ubuntu2204/compiler/bin/clang++"

download() {
    local url="$1" file="$2" sha="$3"
    printf '%s\t%s\t%s\n' "$url" "$sha" "$file" \
        >> "$DN2CPP_PREMERGE_LOGROOT/prerequisite-origins.tsv"
    curl --fail --location --retry 3 --output "$file" "$url"
    printf '%s  %s\n' "$sha" "$file" | sha256sum --check --strict
}

# DeNA publishes no independent checksum/signature for this release. This hash
# binds the official HTTPS release asset, not a separately signed attestation.
declang_zip="$TOOLS/downloads/Release-Linux-swift5.10-v1.0.0-ubuntu2204.zip"
download \
    https://github.com/DeNA/DeClang/releases/download/swift5.10-v1.0.0/Release-Linux-swift5.10-v1.0.0-ubuntu2204.zip \
    "$declang_zip" ecba55e723a1fcbba7a64452f6de848f84f63eeb93079e162a557490c75bf249
ndk_zip="$TOOLS/downloads/android-ndk-r26d-linux.zip"
download https://dl.google.com/android/repository/android-ndk-r26d-linux.zip \
    "$ndk_zip" eefeafe7ccf177de7cc57158da585e7af119bb7504a63604ad719e4b2a328b54
# Google's published r26d checksum is SHA1; retain that independent check too.
printf '%s  %s\n' fcdad75a765a46a9cf6560353f480db251d14765 "$ndk_zip" \
    | sha1sum --check --strict
mkdir -p "$TOOLS/declang" "$TOOLS/ndk"
unzip -q "$declang_zip" -d "$TOOLS/declang"
unzip -q "$ndk_zip" -d "$TOOLS/ndk"

bash gates/setup-emsdk.sh --out "$DN2CPP_EMSDK"

# The source-only .NET-module gate fingerprints the pinned engine's interop ABI.
godot_pin=$(sed -n 's/^PINNED_COMMIT=//p' gates/setup-godot-dotnet.sh)
[[ "$godot_pin" =~ ^[0-9a-f]{40}$ ]] || {
    echo "error: missing unambiguous Godot source pin" >&2; exit 1;
}
git init "$DN2CPP_GODOT_CLONE"
git -C "$DN2CPP_GODOT_CLONE" remote add origin https://github.com/godotengine/godot.git
git -C "$DN2CPP_GODOT_CLONE" fetch --depth 1 origin "$godot_pin"
git -C "$DN2CPP_GODOT_CLONE" checkout --detach FETCH_HEAD
[[ "$(git -C "$DN2CPP_GODOT_CLONE" rev-parse HEAD)" == "$godot_pin" ]]
bash gates/setup-godot-dotnet.sh "$DN2CPP_GODOT_DOTNET_ROOT"
"$DN2CPP_GODOT_DOTNET_ROOT/editor_bin" --headless --dump-extension-api
test -s extension_api.json

for key in CC CXX DN2CPP_EMSDK DN2CPP_GODOT_CLONE DN2CPP_GODOT_DOWNLOAD_CACHE \
    DN2CPP_GODOT_DOTNET_ROOT ANDROID_NDK_ROOT DN2CPP_DECLANG_COMPILER; do
    printf '%s=%s\n' "$key" "${!key}" >> "$GITHUB_ENV"
done

{
    dotnet --info
    "$CC" --version
    cmake --version
    ninja --version
    python3 --version
    "$DN2CPP_EMSDK/emscripten/emcc" --version
    "$DN2CPP_GODOT_DOTNET_ROOT/editor_bin" --version
    cat "$ANDROID_NDK_ROOT/source.properties"
    git -C "$DN2CPP_GODOT_CLONE" remote get-url origin
    git -C "$DN2CPP_GODOT_CLONE" rev-parse HEAD HEAD^{tree}
    dpkg-query -W clang-19 cmake ninja-build libmpfr6 libgmp10 binutils openssl
    apt-cache policy clang-19 cmake ninja-build libmpfr6 libgmp10 binutils openssl
    python3 -c 'import ctypes, ctypes.util, sys; p = ctypes.util.find_library("mpfr"); print("MPFR:", p); ctypes.CDLL(p) if p else sys.exit(1)'
} > "$DN2CPP_PREMERGE_LOGROOT/tool-versions.txt" 2>&1
mkdir -p "$TOOLS/declang-version/.DeClang"
printf '{"enable_obfuscation":0}\n' > "$TOOLS/declang-version/.DeClang/config.json"
DECLANG_HOME="$TOOLS/declang-version" "$DN2CPP_DECLANG_COMPILER" --version \
    >> "$DN2CPP_PREMERGE_LOGROOT/tool-versions.txt" 2>&1
{
    sha256sum extension_api.json "$DN2CPP_DECLANG_COMPILER" \
        "$DN2CPP_GODOT_DOTNET_ROOT/GodotSharp/Api/Release/GodotSharp.dll" \
        "$DOTNET_ROOT/dotnet" "$(command -v "$CC")" \
        "$(command -v cmake)" "$(command -v ninja)"
    find "$DOTNET_ROOT/shared/Microsoft.NETCore.App" -name System.Private.CoreLib.dll \
        -exec sha256sum {} +
    find "$TOOLS/downloads" artifacts/emsdk/dl -type f -exec sha256sum {} +
} > "$DN2CPP_PREMERGE_LOGROOT/prerequisite-files.sha256"
cp gates/expected/emsdk-pin.txt gates/expected/node-pin.txt \
    gates/setup-culture-reference.py gates/setup-godot-dotnet.sh \
    "$DN2CPP_PREMERGE_LOGROOT/"
df -h "$PWD" "$RUNNER_TEMP"

#!/usr/bin/env bash
# Hosted prerequisites only; pre-merge.sh owns gate selection and the verdict.

premerge_setup_init() {
    : "${RUNNER_TEMP:?GitHub runner temporary directory required}"
    : "${GITHUB_ENV:?GitHub environment file required}"
    : "${GITHUB_PATH:?GitHub path file required}"
    : "${GITHUB_RUN_ID:?GitHub run ID required}"
    : "${GITHUB_RUN_ATTEMPT:?GitHub run attempt required}"
    : "${DN2CPP_PREMERGE_LOGROOT:?pre-merge evidence directory required}"
    PREMERGE_HOST="$1"
    case "$PREMERGE_HOST:$(uname -s):$(uname -m)" in
        linux:Linux:x86_64|macos:Darwin:arm64|windows:MINGW*:x86_64) ;;
        *) echo "error: unsupported pre-merge host: $PREMERGE_HOST/$(uname -s)/$(uname -m)" >&2; return 1 ;;
    esac
    PREMERGE_TOOLS="$RUNNER_TEMP/dn2cpp-premerge-tools-$GITHUB_RUN_ID-$GITHUB_RUN_ATTEMPT"
    if [ "$PREMERGE_HOST" = windows ]; then
        PREMERGE_TOOLS="$(cygpath -am "$PREMERGE_TOOLS")"
    fi
    [[ ! -e "$PREMERGE_TOOLS" ]] || {
        echo "error: refusing to reuse $PREMERGE_TOOLS" >&2; return 1;
    }
    mkdir -p "$PREMERGE_TOOLS/downloads"
    : > "$DN2CPP_PREMERGE_LOGROOT/prerequisite-files.sha256"
    export DN2CPP_EMSDK="$PREMERGE_TOOLS/emsdk"
    export DN2CPP_GODOT_CLONE="$PREMERGE_TOOLS/godot-source"
    export DN2CPP_GODOT_DOWNLOAD_CACHE="$PREMERGE_TOOLS/downloads"
    export DN2CPP_GODOT_DOTNET_ROOT="$PREMERGE_TOOLS/godot-dotnet"
    export ANDROID_NDK_ROOT="$PREMERGE_TOOLS/ndk/android-ndk-r26d"
    df -h "$PWD" "$RUNNER_TEMP"
}

premerge_export() {
    local key
    for key in "$@"; do
        export "$key"
        printf '%s=%s\n' "$key" "${!key}" >> "$GITHUB_ENV"
    done
}

premerge_add_path() {
    local shell_path="$1"
    [ "$PREMERGE_HOST" != windows ] || shell_path="$(cygpath -u "$1")"
    export PATH="$shell_path:$PATH"
    printf '%s\n' "$1" >> "$GITHUB_PATH"
}

premerge_verify_archive() {
    python - "$1" "$2" "$3" <<'PY'
import hashlib, pathlib, sys
path, algorithm, expected = sys.argv[1:]
with pathlib.Path(path).open('rb') as stream:
    actual = hashlib.file_digest(stream, algorithm).hexdigest()
if actual != expected.lower():
    raise SystemExit(f'{path}: {algorithm} mismatch: expected {expected}, got {actual}')
PY
}

premerge_download() {
    local url="$1" file="$2" algorithm="$3" expected="$4"
    printf '%s\t%s:%s\t%s\n' "$url" "$algorithm" "$expected" "$file" \
        >> "$DN2CPP_PREMERGE_LOGROOT/prerequisite-origins.tsv"
    if curl --fail --location --retry 3 --output "$file" "$url"; then
        :
    else
        local status=$? fallback_url="$url?download=1"
        [[ "$url" =~ ^https://github\.com/[^/?#]+/[^/?#]+/releases/download/[^/?#]+/[^/?#]+$ ]] || return "$status"
        # A failed release redirect can be cached while the asset remains available.
        printf 'Download failed (curl %s): %s; trying %s once\n' "$status" "$url" "$fallback_url" >&2
        printf '%s\t%s:%s\t%s\n' "$fallback_url" "$algorithm" "$expected" "$file" \
            >> "$DN2CPP_PREMERGE_LOGROOT/prerequisite-origins.tsv"
        curl --fail --location --retry 0 --output "$file" "$fallback_url" || return $?
    fi
    premerge_verify_archive "$file" "$algorithm" "$expected" || return $?
    python - "$file" >> "$DN2CPP_PREMERGE_LOGROOT/prerequisite-files.sha256" <<'PY'
import hashlib, pathlib, sys
path = pathlib.Path(sys.argv[1])
with path.open('rb') as stream:
    print(f'{hashlib.file_digest(stream, "sha256").hexdigest()}  {path}')
PY
}

premerge_extract_zip() {
    printf 'Unpacking %s into %s\n' "$1" "$2"
    if [ "$PREMERGE_HOST" = macos ]; then
        # Preserve compiler symlinks and app bundle metadata with the host extractor.
        /usr/bin/ditto -x -k "$1" "$2"
    else
        unzip -q "$1" -d "$2"
    fi
}

premerge_prepare_compilers() {
    local declang_archive declang_hash declang_dir ndk_host ndk_hash suffix=
    case "$PREMERGE_HOST" in
        linux)
            declang_archive=Release-Linux-swift5.10-v1.0.0-ubuntu2204
            declang_dir="$declang_archive"
            declang_hash=ecba55e723a1fcbba7a64452f6de848f84f63eeb93079e162a557490c75bf249
            ndk_host=linux
            ndk_hash=fcdad75a765a46a9cf6560353f480db251d14765
            ;;
        macos)
            declang_archive=Release-MacArm-swift5.10-v1.0.0
            declang_dir=Release-swift5.10-v1.0.0
            declang_hash=5f4c6d2c68618d43ba120f662034ee35357dbb515d57fa4c758b0601f4d65e35
            ndk_host=darwin
            ndk_hash=cebe7af9bdadc67b8f919d549e9ab269831fd2c9
            ;;
        windows)
            declang_archive=Release-Win-swift5.10-v1.0.0
            declang_dir="$declang_archive"
            declang_hash=adfd4c602d2e41e85a092197f6112d4ddd9f05b701f1ffdebca8cca79a16e8a2
            ndk_host=windows
            ndk_hash=c7ea35ffe916082876611da1a6d5618d15430c29
            suffix=.exe
            ;;
        *) echo "error: no compiler inputs for $PREMERGE_HOST" >&2; return 1 ;;
    esac
    # DeNA provides no independent checksum; these hashes bind its HTTPS assets.
    premerge_download \
        "https://github.com/DeNA/DeClang/releases/download/swift5.10-v1.0.0/$declang_archive.zip" \
        "$PREMERGE_TOOLS/downloads/$declang_archive.zip" sha256 "$declang_hash"
    # Google's repository2-3.xml publishes these zip URLs and SHA1 checksums.
    local ndk_zip="$PREMERGE_TOOLS/downloads/android-ndk-r26d-$ndk_host.zip"
    premerge_download "https://dl.google.com/android/repository/android-ndk-r26d-$ndk_host.zip" \
        "$ndk_zip" sha1 "$ndk_hash"
    if [ "$PREMERGE_HOST" = linux ]; then
        premerge_verify_archive "$ndk_zip" sha256 eefeafe7ccf177de7cc57158da585e7af119bb7504a63604ad719e4b2a328b54
    fi
    mkdir -p "$PREMERGE_TOOLS/declang" "$PREMERGE_TOOLS/ndk"
    premerge_extract_zip "$PREMERGE_TOOLS/downloads/$declang_archive.zip" "$PREMERGE_TOOLS/declang"
    premerge_extract_zip "$ndk_zip" "$PREMERGE_TOOLS/ndk"
    rm "$PREMERGE_TOOLS/downloads/$declang_archive.zip" "$ndk_zip"
    export DN2CPP_DECLANG_COMPILER="$PREMERGE_TOOLS/declang/$declang_dir/compiler/bin/clang++$suffix"
    test -x "$DN2CPP_DECLANG_COMPILER"
    local ndk_revision='Pkg.Revision = 26.3.11579264'
    grep -Fxq -e "$ndk_revision" -e "$ndk_revision"$'\r' "$ANDROID_NDK_ROOT/source.properties"
    test -f "$ANDROID_NDK_ROOT/build/cmake/android.toolchain.cmake"
    bash gates/setup-emsdk.sh --out "$DN2CPP_EMSDK"
}

premerge_prepare_godot() {
    local pin tag asset editor_hash editor template suffix=
    pin=$(sed -n 's/^PINNED_COMMIT=//p' gates/setup-godot-dotnet.sh)
    tag=$(sed -n 's/^PINNED_TAG=//p' gates/setup-godot-dotnet.sh)
    [[ "$pin" =~ ^[0-9a-f]{40}$ && "$tag" = 4.7.2-stable ]] || {
        echo "error: update the pre-merge Godot asset hashes with the engine pin" >&2; return 1;
    }
    git init "$DN2CPP_GODOT_CLONE"
    git -C "$DN2CPP_GODOT_CLONE" remote add origin https://github.com/godotengine/godot.git
    git -C "$DN2CPP_GODOT_CLONE" fetch --depth 1 origin "$pin"
    git -C "$DN2CPP_GODOT_CLONE" checkout --detach FETCH_HEAD
    [[ "$(git -C "$DN2CPP_GODOT_CLONE" rev-parse HEAD)" = "$pin" ]]
    case "$PREMERGE_HOST" in
        linux)
            asset="Godot_v${tag}_mono_linux_x86_64"
            editor_hash=129f82db7bafd54ae14bb5bb284041c73860e8c7a009a3a026ca5e946cbff247
            editor="$PREMERGE_TOOLS/godot-editor/$asset/Godot_v${tag}_mono_linux.x86_64"
            template=linux_release.x86_64
            ;;
        macos)
            asset="Godot_v${tag}_mono_macos.universal"
            editor_hash=8af3977b60d2c59802f7c8ff1914b3ca02a5e294f7381fc1104ee777e33cbbd8
            editor="$PREMERGE_TOOLS/godot-editor/Godot_mono.app/Contents/MacOS/Godot"
            template=macos.zip
            ;;
        windows)
            asset="Godot_v${tag}_mono_win64"
            editor_hash=a2a48473a7414c5f19fab690518caebb738c09ef9601f6bd2388676a7f53b3c0
            editor="$PREMERGE_TOOLS/godot-editor/$asset/${asset}.exe"
            template=windows_release_x86_64.exe
            suffix=.exe
            ;;
        *) echo "error: no Godot inputs for $PREMERGE_HOST" >&2; return 1 ;;
    esac
    local release="https://github.com/godotengine/godot-builds/releases/download/$tag"
    premerge_download "$release/$asset.zip" "$PREMERGE_TOOLS/downloads/$asset.zip" sha256 "$editor_hash"
    premerge_download "$release/Godot_v${tag}_mono_export_templates.tpz" \
        "$PREMERGE_TOOLS/downloads/Godot_v${tag}_mono_export_templates.tpz" \
        sha256 92f8681e349ef1f90891b792da95e3b2b0bd1ed610b78018c58feb2d87e15a9d
    premerge_extract_zip "$PREMERGE_TOOLS/downloads/$asset.zip" "$PREMERGE_TOOLS/godot-editor"
    mkdir -p "$PREMERGE_TOOLS/godot-template"
    unzip -q -j "$PREMERGE_TOOLS/downloads/Godot_v${tag}_mono_export_templates.tpz" \
        "templates/$template" -d "$PREMERGE_TOOLS/godot-template"
    if [ "$PREMERGE_HOST" = macos ]; then
        premerge_extract_zip "$PREMERGE_TOOLS/godot-template/macos.zip" "$PREMERGE_TOOLS/godot-template"
        rm "$PREMERGE_TOOLS/godot-template/macos.zip"
        template=macos_template.app/Contents/MacOS/godot_macos_release.universal
    fi
    rm "$PREMERGE_TOOLS/downloads/$asset.zip" "$PREMERGE_TOOLS/downloads/Godot_v${tag}_mono_export_templates.tpz"
    export DN2CPP_GODOT_PREBUILT="$editor"
    export DN2CPP_GODOT_PREBUILT_TEMPLATE="$PREMERGE_TOOLS/godot-template/$template"
    test -x "$DN2CPP_GODOT_PREBUILT"
    test -x "$DN2CPP_GODOT_PREBUILT_TEMPLATE"
    bash gates/setup-godot-dotnet.sh "$DN2CPP_GODOT_DOTNET_ROOT"
    "$DN2CPP_GODOT_DOTNET_ROOT/editor_bin$suffix" --headless --dump-extension-api
    test -s extension_api.json
}

premerge_record_inputs() {
    local suffix=
    [ "$PREMERGE_HOST" != windows ] || suffix=.exe
    premerge_export DN2CPP_EMSDK DN2CPP_GODOT_CLONE DN2CPP_GODOT_DOWNLOAD_CACHE \
        DN2CPP_GODOT_DOTNET_ROOT ANDROID_NDK_ROOT DN2CPP_DECLANG_COMPILER
    {
        dotnet --info
        cmake --version
        ninja --version
        python --version
        openssl version
        (
            source gates/_common.sh
            dn2cpp_emsdk_resolve
            emcc --version
        )
        "$DN2CPP_GODOT_DOTNET_ROOT/editor_bin$suffix" --version
        cat "$ANDROID_NDK_ROOT/source.properties"
        git -C "$DN2CPP_GODOT_CLONE" remote get-url origin
        git -C "$DN2CPP_GODOT_CLONE" rev-parse HEAD HEAD^{tree}
    } >> "$DN2CPP_PREMERGE_LOGROOT/tool-versions.txt" 2>&1
    mkdir -p "$PREMERGE_TOOLS/declang-version/.DeClang"
    printf '{"enable_obfuscation":0}\n' > "$PREMERGE_TOOLS/declang-version/.DeClang/config.json"
    DECLANG_HOME="$PREMERGE_TOOLS/declang-version" "$DN2CPP_DECLANG_COMPILER" --version \
        >> "$DN2CPP_PREMERGE_LOGROOT/tool-versions.txt" 2>&1
    python - artifacts/emsdk/dl \
        >> "$DN2CPP_PREMERGE_LOGROOT/prerequisite-files.sha256" <<'PY'
import hashlib, os, pathlib, sys
paths = [pathlib.Path('extension_api.json'), pathlib.Path(os.environ['DN2CPP_DECLANG_COMPILER']),
         pathlib.Path(os.environ['DN2CPP_GODOT_DOTNET_ROOT']) / 'GodotSharp/Api/Release/GodotSharp.dll',
         pathlib.Path(os.environ['DOTNET_ROOT']) / ('dotnet.exe' if sys.platform == 'win32' else 'dotnet')]
paths.extend((pathlib.Path(os.environ['DOTNET_ROOT']) / 'shared/Microsoft.NETCore.App').glob('*/System.Private.CoreLib.dll'))
if os.environ.get('DN2CPP_CORELIB_CROSS'):
    paths.append(pathlib.Path(os.environ['DN2CPP_CORELIB_CROSS']))
archives = [path for directory in sys.argv[1:] for path in pathlib.Path(directory).rglob('*') if path.is_file()]
paths.extend(archives)
for path in sorted(paths):
    with path.open('rb') as stream:
        print(f'{hashlib.file_digest(stream, "sha256").hexdigest()}  {path}')
for path in archives:
    path.unlink()
PY
    cp gates/expected/emsdk-pin.txt gates/expected/node-pin.txt \
        gates/setup-culture-reference.py gates/setup-godot-dotnet.sh \
        gates/_setup-premerge.sh "gates/setup-$PREMERGE_HOST-premerge.sh" \
        "$DN2CPP_PREMERGE_LOGROOT/"
    df -h "$PWD" "$RUNNER_TEMP"
}

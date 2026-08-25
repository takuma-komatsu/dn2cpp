# Shared acquisition/build setup for the YetAnotherHttpHandler gates. Callers
# provide YAHA_CACHE so independent Phase-4 workers never write the same archive,
# extracted source, or managed output.

yaha_prepare() {
    local pin="$1" fixture="$2"
    local version commit archive_sha archive_url archive source_root native_rel native_sha

    [ -n "${YAHA_CACHE:-}" ] \
        || { echo "FAIL: yaha_prepare requires a caller-owned YAHA_CACHE" >&2; return 1; }

    version=$(pin_field "$pin" version)
    commit=$(pin_field "$pin" commit)
    archive_sha=$(pin_field "$pin" archive_sha256)
    archive_url=$(pin_field "$pin" archive_url)
    archive="$YAHA_CACHE/YetAnotherHttpHandler-$version-$commit.tar.gz"
    source_root="$YAHA_CACHE/source-$commit/YetAnotherHttpHandler-$commit"

    case "$DN2CPP_OS:$(uname -m)" in
        macos:arm64|macos:x86_64)
            native_rel="src/YetAnotherHttpHandler/Plugins/$YAHA_MODULE/runtimes/osx-universal/native/$YAHA_MODULE.bundle"
            native_sha=$(pin_field "$pin" macos_universal_sha256)
            YAHA_ORACLE_NAME="lib$YAHA_MODULE.dylib"
            ;;
        linux:x86_64)
            native_rel="src/YetAnotherHttpHandler/Plugins/$YAHA_MODULE/runtimes/linux-x64/native/lib$YAHA_MODULE.so"
            native_sha=$(pin_field "$pin" linux_x64_sha256)
            YAHA_ORACLE_NAME="lib$YAHA_MODULE.so"
            ;;
        windows:x86_64)
            native_rel="src/YetAnotherHttpHandler/Plugins/$YAHA_MODULE/runtimes/win-x64/native/$YAHA_MODULE.dll"
            native_sha=$(pin_field "$pin" windows_x64_sha256)
            YAHA_ORACLE_NAME="$YAHA_MODULE.dll"
            ;;
        *) gate_skip "YetAnotherHttpHandler $version has no pinned linkable native asset for $DN2CPP_OS/$(uname -m)" ;;
    esac

    if [ "$(file_hash "$archive")" != "$archive_sha" ] \
            && ! curl -fsI --max-time 15 "$archive_url" >/dev/null 2>&1; then
        gate_skip "YetAnotherHttpHandler $version is not cached and GitHub is unreachable"
    fi
    fetch_pinned "$archive_url" "$archive_sha" "$archive"
    if [ ! -f "$source_root/LICENSE" ]; then
        rm -rf "$YAHA_CACHE/source-$commit"
        mkdir -p "$YAHA_CACHE/source-$commit"
        tar -xzf "$archive" -C "$YAHA_CACHE/source-$commit"
    fi
    [ "$(file_hash "$source_root/LICENSE")" = "$(pin_field "$pin" license_sha256)" ] \
        || { echo "FAIL: upstream LICENSE integrity mismatch" >&2; return 1; }

    YAHA_PIPELINES="$source_root/src/YetAnotherHttpHandler.Unity/Assets/Plugins/System.IO.Pipelines.8.0.0/lib/netstandard2.0/System.IO.Pipelines.dll"
    YAHA_NATIVE="$source_root/$native_rel"
    [ "$(file_hash "$YAHA_PIPELINES")" = "$(pin_field "$pin" pipelines_sha256)" ] \
        || { echo "FAIL: upstream System.IO.Pipelines.dll integrity mismatch" >&2; return 1; }
    [ "$(file_hash "$YAHA_NATIVE")" = "$native_sha" ] \
        || { echo "FAIL: upstream native asset integrity mismatch" >&2; return 1; }
    echo "official tag $version ($commit): archive, license, managed dependency, native asset pinned"

    if [ "$DN2CPP_OS" = windows ]; then
        local pipelines_short="$YAHA_CACHE/System.IO.Pipelines.dll"
        local native_short="$YAHA_CACHE/$YAHA_ORACLE_NAME"
        cp -f "$YAHA_PIPELINES" "$pipelines_short"
        [ "$(file_hash "$pipelines_short")" = "$(pin_field "$pin" pipelines_sha256)" ] \
            || { echo "FAIL: shortened System.IO.Pipelines.dll changed bytes" >&2; return 1; }
        YAHA_PIPELINES="$pipelines_short"

        # Keep MSVC below MAX_PATH; the archive's commit-qualified path can make
        # native build tools see only the DLL basename.
        cp -f "$YAHA_NATIVE" "$native_short"
        [ "$(file_hash "$native_short")" = "$native_sha" ] \
            || { echo "FAIL: shortened Windows native asset changed bytes" >&2; return 1; }
        YAHA_NATIVE="$native_short"
    fi

    local managed_out="$YAHA_CACHE/managed-$commit"
    dotnet build "$fixture/Managed/YetAnotherHttpHandler.Managed.csproj" -c Release \
        -o "$managed_out" -p:YahaVersion="$version" \
        -p:BaseIntermediateOutputPath="$PWD/$YAHA_CACHE/managed-obj/" \
        -p:YahaSourceRoot="$PWD/$source_root/src/YetAnotherHttpHandler" \
        -p:YahaPipelinesDll="$PWD/$YAHA_PIPELINES" --nologo -v:minimal
    YAHA_MANAGED="$managed_out/Cysharp.Net.Http.YetAnotherHttpHandler.dll"
    [ -f "$YAHA_MANAGED" ] \
        || { echo "FAIL: official managed source produced no assembly" >&2; return 1; }

    YAHA_VERSION="$version"
    YAHA_COMMIT="$commit"
}

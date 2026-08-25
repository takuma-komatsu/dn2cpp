#!/usr/bin/env bash
# A NuGet runtimes/<RID>/native asset selected by the SDK is carried through
# Dn2Cpp.Build into the native link. The publish and link must use the exact
# package-cache path chosen for the current target.
source "$(dirname "$0")/_common.sh"

PROJECT=NativeAssetPublish
FIXTURE=gates/fixtures/native-asset
OUT=artifacts/native-asset-publish
FEED="$OUT/feed"
NATIVE_BUILD="$OUT/native-build"
NUGET_CACHE="$OUT/nuget-cache"

set_linux_published_soname_paths() {
    PUBLISHED_SONAME="$1/$(basename "$2")"
    PUBLISHED_SONAME_ALIAS="$PUBLISHED_SONAME.1"
}

if [ "${DN2CPP_NATIVE_ASSET_BRANCH_SELF_TEST:-}" = linux ]; then
    unset PUBLISHED_SONAME PUBLISHED_SONAME_ALIAS
    set_linux_published_soname_paths /publish/.dn2cpp-native/0123456789abcdef0123 /cache/libfixture.so
    [ "$PUBLISHED_SONAME" = /publish/.dn2cpp-native/0123456789abcdef0123/libfixture.so ]
    [ "$PUBLISHED_SONAME_ALIAS" = /publish/.dn2cpp-native/0123456789abcdef0123/libfixture.so.1 ]
    exit 0
fi

"$CMAKE" -P "$FIXTURE/UniversalSelectionProbe.cmake"

case "$DN2CPP_OS:$(uname -m)" in
    macos:arm64) RID=osx-arm64; DECOY_RID=linux-x64 ;;
    macos:x86_64) RID=osx-x64; DECOY_RID=linux-x64 ;;
    linux:aarch64|linux:arm64) RID=linux-arm64; DECOY_RID=win-x64 ;;
    linux:x86_64) RID=linux-x64; DECOY_RID=win-x64 ;;
    windows:aarch64|windows:arm64) RID=win-arm64; DECOY_RID=osx-x64 ;;
    windows:x86_64) RID=win-x64; DECOY_RID=osx-x64 ;;
    *) gate_skip "no native-asset fixture RID for $DN2CPP_OS/$(uname -m)" ;;
esac

rm -rf "$OUT"
mkdir -p "$FEED"

echo "== 1/6 Building the native package payload ($RID) =="
"$CMAKE" -S samples/native/nativeasset -B "$NATIVE_BUILD" -G Ninja \
    -DCMAKE_BUILD_TYPE=Release
"$CMAKE" --build "$NATIVE_BUILD"

case "$DN2CPP_OS" in
    macos|linux)
        NATIVE_LIB="$NATIVE_BUILD/libdn2cpp_native_asset.a"
        NATIVE_STATIC_DEP="$NATIVE_BUILD/libdn2cpp_native_dependency.a"
        ;;
    windows)
        NATIVE_LIB="$NATIVE_BUILD/dn2cpp_native_asset.lib"
        NATIVE_STATIC_DEP="$NATIVE_BUILD/dn2cpp_native_dependency.lib"
        ;;
esac
[ -f "$NATIVE_LIB" ] || { echo "FAIL: native fixture missing: $NATIVE_LIB" >&2; exit 1; }
[ -f "$NATIVE_STATIC_DEP" ] || { echo "FAIL: native fixture dependency missing: $NATIVE_STATIC_DEP" >&2; exit 1; }
DECOY_LIB="$OUT/decoy/$(basename "$NATIVE_LIB")"
mkdir -p "$(dirname "$DECOY_LIB")"
cp "$NATIVE_LIB" "$DECOY_LIB"

SHARED_LIB=
SHARED_DEP=
SONAME_ASSET=
NO_INSTALL_ID_ASSET=
SHARED_RID=
if [ "$DN2CPP_OS" = macos ]; then
    SHARED_SOURCE="$NATIVE_BUILD/libdn2cpp_shared_asset.dylib"
    SHARED_DEP_SOURCE="$NATIVE_BUILD/libdn2cpp_shared_dependency.dylib"
    SHARED_RID=osx-universal
    SHARED_LIB="$OUT/shared-package/dn2cpp_shared_asset.bundle"
    SHARED_DEP="$OUT/shared-package/dn2cpp_shared_dependency.bundle"
    NO_INSTALL_ID_ASSET="$NATIVE_BUILD/dn2cpp_no_id.bundle"
    mkdir -p "$(dirname "$SHARED_LIB")"
    cp "$SHARED_SOURCE" "$SHARED_LIB"
    cp "$SHARED_DEP_SOURCE" "$SHARED_DEP"
elif [ "$DN2CPP_OS" = linux ]; then
    SHARED_RID=$RID
    SHARED_LIB="$NATIVE_BUILD/libdn2cpp_shared_asset.so"
    SHARED_DEP="$NATIVE_BUILD/libdn2cpp_shared_dependency.so"
    SONAME_ASSET="$OUT/shared-package/libdn2cpp_soname_asset.so"
    mkdir -p "$(dirname "$SONAME_ASSET")"
    cp "$NATIVE_BUILD/libdn2cpp_soname_asset.so.1" "$SONAME_ASSET"
elif [ "$DN2CPP_OS" = windows ]; then
    SHARED_RID=$RID
    SHARED_LIB="$NATIVE_BUILD/dn2cpp_shared_asset.dll"
    SHARED_DEP="$NATIVE_BUILD/dn2cpp_shared_dependency.dll"
fi

pack_args=(
    "$FIXTURE/Package/NativeAssetPackage.csproj"
    -c "$CONFIG" -o "$FEED"
    "-p:NativeAssetRid=$RID"
    "-p:NativeAssetPath=$PWD/$NATIVE_LIB"
    "-p:NativeStaticDependencyPath=$PWD/$NATIVE_STATIC_DEP"
    "-p:NativeAssetDecoyRid=$DECOY_RID"
    "-p:NativeAssetDecoyPath=$PWD/$DECOY_LIB"
)
if [ -n "$SHARED_LIB" ]; then
    [ -f "$SHARED_LIB" ] || { echo "FAIL: shared fixture missing: $SHARED_LIB" >&2; exit 1; }
    [ -f "$SHARED_DEP" ] || { echo "FAIL: shared dependency fixture missing: $SHARED_DEP" >&2; exit 1; }
    pack_args+=(
        "-p:NativeSharedAssetRid=$SHARED_RID"
        "-p:NativeSharedAssetPath=$PWD/$SHARED_LIB"
        "-p:NativeSharedDependencyPath=$PWD/$SHARED_DEP"
    )
    [ -z "$SONAME_ASSET" ] || pack_args+=("-p:NativeSonameAssetPath=$PWD/$SONAME_ASSET")
    [ -z "$NO_INSTALL_ID_ASSET" ] || pack_args+=("-p:NativeNoInstallIdAssetPath=$PWD/$NO_INSTALL_ID_ASSET")
fi
dotnet pack "${pack_args[@]}" --nologo -v:minimal
nupkg="$FEED/Dn2Cpp.NativeAssetFixture.1.0.0.nupkg"
package_list="$OUT/package-contents.txt"
unzip -l "$nupkg" > "$package_list"
grep -q "runtimes/$DECOY_RID/native/" "$package_list" \
    || { echo "FAIL: fixture package contains no $DECOY_RID decoy" >&2; exit 1; }
if [ -n "$SHARED_LIB" ]; then
    grep -q "runtimes/$SHARED_RID/native/.*dn2cpp_shared_asset" "$package_list" \
        || { echo "FAIL: fixture package contains no $SHARED_RID shared asset" >&2; exit 1; }
    grep -q "runtimes/$SHARED_RID/native/.*dn2cpp_shared_dependency" "$package_list" \
        || { echo "FAIL: fixture package contains no $SHARED_RID shared dependency" >&2; exit 1; }
    if [ -n "$NO_INSTALL_ID_ASSET" ]; then
        grep -q "runtimes/$SHARED_RID/native/.*dn2cpp_no_id.bundle" "$package_list" \
            || { echo "FAIL: fixture package contains no MH_BUNDLE asset" >&2; exit 1; }
    fi
    if [ -n "$SONAME_ASSET" ]; then
        grep -q "runtimes/$SHARED_RID/native/.*dn2cpp_soname_asset.so" "$package_list" \
            || { echo "FAIL: fixture package contains no SONAME fixture" >&2; exit 1; }
    fi
fi

echo "== 2/6 Building the dn2cpp CLI =="
build_proj src/Dn2Cpp.Cli/Dn2Cpp.Cli.csproj
DN2CPP_TOOL="$PWD/src/Dn2Cpp.Cli/bin/$CONFIG/$TFM/dn2cpp$EXE_EXT"
[ -x "$DN2CPP_TOOL" ] || { echo "FAIL: dn2cpp tool missing: $DN2CPP_TOOL" >&2; exit 1; }
DN2CPP_CMAKE_TOOL=$CMAKE
if [ "$DN2CPP_OS" != windows ]; then
    TOOL_LINK_DIR="$OUT/tool path with spaces"
    CMAKE_LINK_DIR="$OUT/cmake path with spaces"
    ln -s "$PWD/src/Dn2Cpp.Cli/bin/$CONFIG/$TFM" "$TOOL_LINK_DIR"
    CMAKE_REAL=$(command -v "$CMAKE")
    ln -s "$(dirname "$CMAKE_REAL")" "$CMAKE_LINK_DIR"
    DN2CPP_TOOL="$PWD/$TOOL_LINK_DIR/dn2cpp$EXE_EXT"
    DN2CPP_CMAKE_TOOL="$PWD/$CMAKE_LINK_DIR/$(basename "$CMAKE_REAL")"
fi

COMPOSITE_TOOL_LOG="$OUT/composite-tool.log"
if dotnet msbuild "$FIXTURE/App/$PROJECT.csproj" -t:Dn2CppNativePublish \
        "-p:RuntimeIdentifier=$RID" \
        "-p:Dn2CppTool=dotnet tool run dn2cpp" \
        -v:minimal > "$COMPOSITE_TOOL_LOG" 2>&1; then
    echo "FAIL: Dn2CppTool accepted a shell command instead of one executable path" >&2
    exit 1
fi
grep -q "Dn2CppTool must name one executable path" "$COMPOSITE_TOOL_LOG" \
    || { echo "FAIL: composite Dn2CppTool lacked its path-only diagnostic" >&2; cat "$COMPOSITE_TOOL_LOG" >&2; exit 1; }

echo "== Rejecting unsupported cross-OS native publication =="
CAPTURE_PROBE="$FIXTURE/CaptureProbe.proj"
case "$DN2CPP_OS" in
    macos) CROSS_OS_RID=linux-x64 ;;
    linux) CROSS_OS_RID=osx-x64 ;;
    windows) CROSS_OS_RID=linux-x64 ;;
esac
CROSS_OS_CAPTURE_LOG="$OUT/cross-os-capture.log"
if dotnet msbuild "$CAPTURE_PROBE" -t:_Dn2CppCaptureNativeAssets \
        "-p:ProbeRuntimeIdentifier=$CROSS_OS_RID" \
        "-p:ProbeDll=$PWD/$OUT/probe.a" \
        "-p:Dn2CppCMakeArgs=-DCMAKE_TOOLCHAIN_FILE=$PWD/$OUT/toolchain.cmake" \
        -v:minimal > "$CROSS_OS_CAPTURE_LOG" 2>&1; then
    echo "FAIL: Dn2Cpp.Build capture accepted a cross-OS publish" >&2
    exit 1
fi
grep -q "does not support cross-OS publish" "$CROSS_OS_CAPTURE_LOG" \
    || { echo "FAIL: cross-OS capture lacked its pre-copy diagnostic" >&2; cat "$CROSS_OS_CAPTURE_LOG" >&2; exit 1; }

echo "== Rejecting non-desktop and unknown-architecture RIDs =="
for unsupported_rid in android-arm64 ios-arm64 browser-wasm linux-arm linux-musl-arm64 win-x86 win10-x64; do
    unsupported_log="$OUT/unsupported-rid-$unsupported_rid.log"
    if dotnet msbuild "$CAPTURE_PROBE" -t:_Dn2CppCaptureNativeAssets \
            "-p:ProbeRuntimeIdentifier=$unsupported_rid" \
            "-p:ProbeDll=$PWD/$OUT/probe.a" -v:minimal > "$unsupported_log" 2>&1; then
        echo "FAIL: Dn2Cpp.Build accepted unsupported RID $unsupported_rid" >&2
        exit 1
    fi
    grep -q "unsupported RuntimeIdentifier '$unsupported_rid'" "$unsupported_log" \
        || { echo "FAIL: unsupported RID lacked its diagnostic: $unsupported_rid" >&2; cat "$unsupported_log" >&2; exit 1; }
done
dotnet msbuild "$CAPTURE_PROBE" -t:_Dn2CppCaptureNativeAssets \
    -p:ProbeRuntimeIdentifier= "-p:ProbeDll=$PWD/$OUT/probe.a" -v:minimal

if [ "$DN2CPP_OS:$(uname -m)" = macos:arm64 ]; then
    echo "== Rejecting cross-architecture lookalike and mismatched overrides =="
    for bad_cmake_args in \
        "-DNOT_CMAKE_TOOLCHAIN_FILE=/tmp/not-a-toolchain" \
        "-DCMAKE_OSX_ARCHITECTURES=arm64" \
        "-DCMAKE_SYSTEM_PROCESSOR=x86_64"; do
        cross_log="$OUT/cross-arch-$(printf '%s' "$bad_cmake_args" | tr -cs 'A-Za-z0-9' '_').log"
        if dotnet msbuild "$FIXTURE/App/$PROJECT.csproj" -t:Dn2CppNativePublish \
                -p:RuntimeIdentifier=osx-x64 \
                "-p:Dn2CppCMakeArgs=$bad_cmake_args" \
                "-p:NativeAssetFeed=$PWD/$FEED" -v:minimal > "$cross_log" 2>&1; then
            echo "FAIL: cross-architecture publish accepted $bad_cmake_args" >&2
            exit 1
        fi
        grep -q "requires a matching CMAKE_OSX_ARCHITECTURES" "$cross_log" \
            || { echo "FAIL: cross-architecture rejection lacked its diagnostic" >&2; cat "$cross_log" >&2; exit 1; }
    done
fi

echo "== 3/6 Publishing through Dn2Cpp.Build =="
PINVOKE_ARGS="--auto-ref --pinvoke-module dn2cpp_native_asset"
PUBLISH_ARGS=()
if [ -n "$SHARED_LIB" ]; then
    PINVOKE_ARGS="$PINVOKE_ARGS --pinvoke-module dn2cpp_shared_asset"
    PUBLISH_ARGS+=("-p:DefineConstants=NATIVE_ASSET_SHARED")
fi
PUBLISH="$FIXTURE/App/bin/$CONFIG/$TFM/$RID/publish"
GEN="$FIXTURE/App/obj/$CONFIG/$TFM/$RID/dn2cpp"
case "$DN2CPP_OS" in
    windows) STALE_STATIC_ARCHIVE=dn2cpp_removed_native_asset.lib ;;
    *) STALE_STATIC_ARCHIVE=libdn2cpp_removed_native_asset.a ;;
esac
mkdir -p "$PUBLISH" "$GEN"
printf 'stale archive\n' > "$PUBLISH/$STALE_STATIC_ARCHIVE"
printf '%s\n' "$STALE_STATIC_ARCHIVE" > "$GEN/published-static-archives.txt"
NUGET_PACKAGES="$PWD/$NUGET_CACHE" dotnet publish \
    "$FIXTURE/App/$PROJECT.csproj" -c "$CONFIG" -r "$RID" \
    "-p:NativeAssetFeed=$PWD/$FEED" \
    "-p:Dn2CppTool=$DN2CPP_TOOL" \
    "-p:Dn2CppCMake=$DN2CPP_CMAKE_TOOL" \
    "-p:Dn2CppExtraArgs=$PINVOKE_ARGS" \
    "${PUBLISH_ARGS[@]}" \
    --nologo -v:minimal

[ ! -e "$PUBLISH/$STALE_STATIC_ARCHIVE" ] \
    || { echo "FAIL: incremental publish retained a removed static archive" >&2; exit 1; }
STATIC_ARCHIVE_MANIFEST="$GEN/published-static-archives.txt"
[ -f "$STATIC_ARCHIVE_MANIFEST" ] \
    || { echo "FAIL: static archive basename manifest was not updated" >&2; exit 1; }
STATIC_ARCHIVE_MANIFEST_NORMALIZED="$OUT/published-static-archives-normalized.txt"
LC_ALL=C sed $'1s/^\xEF\xBB\xBF//' "$STATIC_ARCHIVE_MANIFEST" \
    | LC_ALL=C sort -u > "$STATIC_ARCHIVE_MANIFEST_NORMALIZED"
[ "$(wc -l < "$STATIC_ARCHIVE_MANIFEST_NORMALIZED" | tr -d ' ')" -eq 2 ] \
    && grep -Fxq "$(basename "$NATIVE_LIB")" "$STATIC_ARCHIVE_MANIFEST_NORMALIZED" \
    && grep -Fxq "$(basename "$NATIVE_STATIC_DEP")" "$STATIC_ARCHIVE_MANIFEST_NORMALIZED" \
    || { echo "FAIL: static archive basename manifest does not match the current selected set" >&2; cat "$STATIC_ARCHIVE_MANIFEST" >&2; exit 1; }
[ -f "$GEN/native-assets.txt" ] \
    || { echo "FAIL: Dn2Cpp.Build emitted no native-assets.txt" >&2; exit 1; }
NATIVE_ASSETS_NORMALIZED="$OUT/native-assets-normalized.txt"
LC_ALL=C sed $'1s/^\xEF\xBB\xBF//' "$GEN/native-assets.txt" \
    | LC_ALL=C tr '\\' '/' > "$NATIVE_ASSETS_NORMALIZED"
grep -q "/runtimes/$RID/native/" "$NATIVE_ASSETS_NORMALIZED" \
    || { echo "FAIL: native-assets.txt did not preserve the selected $RID asset" >&2; cat "$GEN/native-assets.txt" >&2; exit 1; }
if grep -q "/runtimes/$DECOY_RID/native/" "$NATIVE_ASSETS_NORMALIZED"; then
    echo "FAIL: native-assets.txt selected the $DECOY_RID decoy for $RID" >&2
    exit 1
fi
grep -q "dn2cpp_native_asset" "$NATIVE_ASSETS_NORMALIZED" \
    || { echo "FAIL: native-assets.txt omitted the fixture library" >&2; exit 1; }
grep -q "dn2cpp_native_dependency" "$NATIVE_ASSETS_NORMALIZED" \
    || { echo "FAIL: native-assets.txt omitted the static dependency closure" >&2; exit 1; }
if [ -n "$SHARED_LIB" ]; then
    GENERATION=$(cat "$GEN/build/native-assets-generation.txt")
    case "$GENERATION" in ''|*[!0-9a-f]*) echo "FAIL: invalid native generation: $GENERATION" >&2; exit 1 ;; esac
    [ "${#GENERATION}" -eq 20 ] || { echo "FAIL: invalid native generation length" >&2; exit 1; }
    PUBLISHED_NATIVE_DIR="$PUBLISH/.dn2cpp-native/$GENERATION"
    grep -q "/runtimes/$SHARED_RID/native/.*dn2cpp_shared_asset" "$NATIVE_ASSETS_NORMALIZED" \
        || { echo "FAIL: native-assets.txt omitted the $SHARED_RID shared asset" >&2; cat "$GEN/native-assets.txt" >&2; exit 1; }
    grep -q "/runtimes/$SHARED_RID/native/.*dn2cpp_shared_dependency" "$NATIVE_ASSETS_NORMALIZED" \
        || { echo "FAIL: native-assets.txt omitted the shared dependency closure" >&2; cat "$GEN/native-assets.txt" >&2; exit 1; }
fi

echo "== 4/6 Asserting CMake resolved the P/Invoke token to the package asset =="
CMAKE_CACHE="$GEN/build/CMakeCache.txt"
[ -f "$CMAKE_CACHE" ] || { echo "FAIL: native publish produced no CMake cache" >&2; exit 1; }
EXPECTED_APP_DIR=$(native_path "$PWD/$GEN")
grep -Fq "DN2CPP_APP_DIR:PATH=$EXPECTED_APP_DIR" "$CMAKE_CACHE" \
    || { echo "FAIL: the native build did not consume the generated app directory" >&2; exit 1; }
NINJA_NORMALIZED="$OUT/build-normalized.ninja"
tr '\\' '/' < "$GEN/build/build.ninja" > "$NINJA_NORMALIZED"
grep -q "runtimes/$RID/native/.*dn2cpp_native_asset" "$NINJA_NORMALIZED" \
    || { echo "FAIL: the link graph does not name the selected package asset" >&2; exit 1; }
grep -q "runtimes/$RID/native/.*dn2cpp_native_dependency" "$NINJA_NORMALIZED" \
    || { echo "FAIL: the link graph omits the selected static dependency closure" >&2; exit 1; }
if grep -q -- " -ldn2cpp_native_asset" "$NINJA_NORMALIZED"; then
    echo "FAIL: the package asset did not replace the bare P/Invoke -l token" >&2
    exit 1
fi
if [ -e "$PUBLISH/$(basename "$NATIVE_LIB")" ] \
        || [ -e "$PUBLISH/$(basename "$NATIVE_STATIC_DEP")" ]; then
    echo "FAIL: link-only static archives escaped into the publish output" >&2
    exit 1
fi

echo "== 5/6 Asserting shared-asset relocation policy =="
if [ "$DN2CPP_OS" = windows ]; then
    PUBLISHED_SHARED="$PUBLISHED_NATIVE_DIR/$(basename "$SHARED_LIB")"
    PUBLISHED_DEP="$PUBLISHED_NATIVE_DIR/$(basename "$SHARED_DEP")"
    [ -f "$PUBLISHED_SHARED" ] || { echo "FAIL: staged Windows DLL was not published" >&2; exit 1; }
    [ -f "$PUBLISHED_DEP" ] || { echo "FAIL: staged Windows DLL dependency was not published" >&2; exit 1; }
    dumpbin //NOLOGO //IMPORTS "$(cygpath -w "$PUBLISHED_SHARED")" > "$OUT/windows-shared-imports.txt"
    grep -qi '^    dn2cpp_shared_dependency.dll$' "$OUT/windows-shared-imports.txt" \
        || { echo "FAIL: Windows fixture DLL lost its dependency closure" >&2; exit 1; }
    grep -Fq ".dn2cpp-native\\\\$GENERATION" "$GEN/build/dn2cpp_delay_hook.cpp" \
        || { echo "FAIL: Windows delay-load hook does not select the immutable generation" >&2; exit 1; }
    [ ! -e "$PUBLISH/$(basename "$SHARED_LIB")" ] \
        && [ ! -e "$PUBLISH/$(basename "$SHARED_DEP")" ] \
        || { echo "FAIL: Windows DLLs escaped the immutable generation" >&2; exit 1; }
elif [ "$DN2CPP_OS" = macos ]; then
    CACHE_SHARED=$(grep -E "/runtimes/$SHARED_RID/native/.*dn2cpp_shared_asset" "$NATIVE_ASSETS_NORMALIZED")
    CACHE_DEP=$(grep -E "/runtimes/$SHARED_RID/native/.*dn2cpp_shared_dependency" "$NATIVE_ASSETS_NORMALIZED")
    PUBLISHED_SHARED="$PUBLISHED_NATIVE_DIR/$(basename "$CACHE_SHARED")"
    PUBLISHED_DEP="$PUBLISHED_NATIVE_DIR/$(basename "$CACHE_DEP")"
    PUBLISHED_NO_ID="$PUBLISHED_NATIVE_DIR/$(basename "$NO_INSTALL_ID_ASSET")"
    [ -f "$PUBLISHED_SHARED" ] || { echo "FAIL: staged bundle was not published" >&2; exit 1; }
    [ -f "$PUBLISHED_DEP" ] || { echo "FAIL: staged bundle dependency was not published" >&2; exit 1; }
    [ -f "$PUBLISHED_NO_ID" ] || { echo "FAIL: staged MH_BUNDLE asset was not published" >&2; exit 1; }
    [ "$(otool -D "$NO_INSTALL_ID_ASSET" | wc -l | tr -d ' ')" -eq 1 ] \
        || { echo "FAIL: fixture MH_BUNDLE unexpectedly carries LC_ID_DYLIB" >&2; exit 1; }
    [ "$(otool -D "$PUBLISHED_NO_ID" | wc -l | tr -d ' ')" -eq 1 ] \
        || { echo "FAIL: staged MH_BUNDLE unexpectedly gained LC_ID_DYLIB" >&2; exit 1; }
    codesign -v "$PUBLISHED_NO_ID" \
        || { echo "FAIL: staged MH_BUNDLE was not ad-hoc signed" >&2; exit 1; }
    otool -D "$CACHE_SHARED" > "$OUT/cache-install-name.txt"
    grep -q "/non-relocatable/nuget-cache/" "$OUT/cache-install-name.txt" \
        || { echo "FAIL: fixture bundle did not carry the bad absolute install name" >&2; exit 1; }
    otool -D "$PUBLISHED_SHARED" > "$OUT/published-install-name.txt"
    grep -q "@rpath/$(basename "$PUBLISHED_SHARED")" "$OUT/published-install-name.txt" \
        || { echo "FAIL: staged bundle install name was not normalized" >&2; cat "$OUT/published-install-name.txt" >&2; exit 1; }
    codesign -v "$PUBLISHED_SHARED" \
        || { echo "FAIL: staged bundle was not ad-hoc signed after normalization" >&2; exit 1; }
    otool -L "$PUBLISH/$PROJECT" > "$OUT/executable-load-commands.txt"
    grep -q "@rpath/$(basename "$PUBLISHED_SHARED")" "$OUT/executable-load-commands.txt" \
        || { echo "FAIL: executable retained a package-cache bundle identity" >&2; cat "$OUT/executable-load-commands.txt" >&2; exit 1; }
    otool -L "$PUBLISHED_SHARED" > "$OUT/shared-load-commands.txt"
    grep -q "@rpath/$(basename "$PUBLISHED_DEP")" "$OUT/shared-load-commands.txt" \
        || { echo "FAIL: staged bundle retained its dependency's package-cache identity" >&2; cat "$OUT/shared-load-commands.txt" >&2; exit 1; }
    otool -l "$PUBLISH/$PROJECT" > "$OUT/executable-load-paths.txt"
    grep -q "path @loader_path/.dn2cpp-native/$GENERATION" "$OUT/executable-load-paths.txt" \
        || { echo "FAIL: executable has no generation-specific loader path" >&2; exit 1; }
    if grep -Fq "$GEN/build" "$OUT/executable-load-paths.txt"; then
        echo "FAIL: executable retained a native build-directory loader path" >&2
        exit 1
    fi
elif [ "$DN2CPP_OS" = linux ]; then
    command -v readelf >/dev/null || gate_skip "readelf is required for native asset metadata assertions"
    CACHE_SHARED=$(grep -E "/runtimes/$SHARED_RID/native/.*dn2cpp_shared_asset" "$NATIVE_ASSETS_NORMALIZED")
    CACHE_DEP=$(grep -E "/runtimes/$SHARED_RID/native/.*dn2cpp_shared_dependency" "$NATIVE_ASSETS_NORMALIZED")
    PUBLISHED_SHARED="$PUBLISHED_NATIVE_DIR/$(basename "$CACHE_SHARED")"
    PUBLISHED_DEP="$PUBLISHED_NATIVE_DIR/$(basename "$CACHE_DEP")"
    set_linux_published_soname_paths "$PUBLISHED_NATIVE_DIR" "$SONAME_ASSET"
    [ -f "$PUBLISHED_SHARED" ] || { echo "FAIL: staged shared object was not published" >&2; exit 1; }
    [ -f "$PUBLISHED_DEP" ] || { echo "FAIL: staged shared dependency was not published" >&2; exit 1; }
    [ -f "$PUBLISHED_SONAME" ] && [ -f "$PUBLISHED_SONAME_ALIAS" ] \
        || { echo "FAIL: SONAME mismatch did not publish its basename alias" >&2; exit 1; }
    cache_shared_dynamic=$(LC_ALL=C readelf -d "$CACHE_SHARED")
    if grep -q '(SONAME)' <<<"$cache_shared_dynamic"; then
        echo "FAIL: fixture shared object unexpectedly has a SONAME" >&2
        exit 1
    fi
    LC_ALL=C readelf -d "$PUBLISH/$PROJECT" > "$OUT/executable-dynamic.txt"
    grep -q "Shared library: \[$(basename "$PUBLISHED_SHARED")\]" "$OUT/executable-dynamic.txt" \
        || { echo "FAIL: executable DT_NEEDED is not the staged basename" >&2; cat "$OUT/executable-dynamic.txt" >&2; exit 1; }
    grep -q "(RPATH).*\\\$ORIGIN/.dn2cpp-native/$GENERATION" "$OUT/executable-dynamic.txt" \
        || { echo "FAIL: executable has no transitive generation-specific DT_RPATH" >&2; exit 1; }
    if grep -Fq "$GEN/build" "$OUT/executable-dynamic.txt"; then
        echo "FAIL: executable retained a native build-directory loader path" >&2
        exit 1
    fi
    LC_ALL=C readelf -d "$PUBLISHED_SHARED" > "$OUT/shared-dynamic.txt"
    grep -q "Shared library: \[$(basename "$PUBLISHED_DEP")\]" "$OUT/shared-dynamic.txt" \
        || { echo "FAIL: staged shared object lost its dependency closure" >&2; cat "$OUT/shared-dynamic.txt" >&2; exit 1; }
fi
if [ -n "$SHARED_LIB" ]; then
    compile_console "$GEN" "$PROJECT"
    [ -f "$GEN/.dn2cpp-native/$GENERATION/$(basename "$PUBLISHED_SHARED")" ] \
        || { echo "FAIL: compile_console did not publish the shared asset" >&2; exit 1; }
    [ -f "$GEN/.dn2cpp-native/$GENERATION/$(basename "$PUBLISHED_DEP")" ] \
        || { echo "FAIL: compile_console did not publish the dependency closure" >&2; exit 1; }

    echo "== Re-publishing package version 1.0.1 with changed shared contents =="
    CHANGED_PACKAGE_DIR="$OUT/changed-package"
    CHANGED_NATIVE_BUILD="$OUT/changed-native-build"
    mkdir -p "$CHANGED_PACKAGE_DIR"
    "$CMAKE" -S samples/native/nativeasset -B "$CHANGED_NATIVE_BUILD" -G Ninja \
        -DCMAKE_BUILD_TYPE=Release -DDN2CPP_SHARED_DEP_VALUE=13
    "$CMAKE" --build "$CHANGED_NATIVE_BUILD"
    case "$DN2CPP_OS" in
        macos)
            CHANGED_SHARED="$CHANGED_PACKAGE_DIR/libdn2cpp_shared_asset.dylib"
            CHANGED_DEP="$CHANGED_PACKAGE_DIR/dn2cpp_shared_dependency.bundle"
            cp "$CHANGED_NATIVE_BUILD/libdn2cpp_shared_asset.dylib" "$CHANGED_SHARED"
            cp "$CHANGED_NATIVE_BUILD/libdn2cpp_shared_dependency.dylib" "$CHANGED_DEP"
            ;;
        linux)
            CHANGED_SHARED="$CHANGED_PACKAGE_DIR/libdn2cpp_shared_asset.so.9"
            CHANGED_DEP="$CHANGED_PACKAGE_DIR/libdn2cpp_shared_dependency.so"
            cp "$CHANGED_NATIVE_BUILD/libdn2cpp_shared_asset.so" "$CHANGED_SHARED"
            cp "$CHANGED_NATIVE_BUILD/libdn2cpp_shared_dependency.so" "$CHANGED_DEP"
            ;;
        windows)
            CHANGED_SHARED="$CHANGED_PACKAGE_DIR/dn2cpp_shared_asset.dll"
            CHANGED_DEP="$CHANGED_PACKAGE_DIR/dn2cpp_shared_dependency.dll"
            cp "$CHANGED_NATIVE_BUILD/dn2cpp_shared_asset.dll" "$CHANGED_SHARED"
            cp "$CHANGED_NATIVE_BUILD/dn2cpp_shared_dependency.dll" "$CHANGED_DEP"
            ;;
    esac
    second_pack_args=(
        "$FIXTURE/Package/NativeAssetPackage.csproj"
        -c "$CONFIG" -o "$FEED"
        -p:FixtureVersion=1.0.1
        "-p:NativeAssetRid=$RID"
        "-p:NativeAssetPath=$PWD/$NATIVE_LIB"
        "-p:NativeStaticDependencyPath=$PWD/$NATIVE_STATIC_DEP"
        "-p:NativeAssetDecoyRid=$DECOY_RID"
        "-p:NativeAssetDecoyPath=$PWD/$DECOY_LIB"
        "-p:NativeSharedAssetRid=$SHARED_RID"
        "-p:NativeSharedAssetPath=$PWD/$CHANGED_SHARED"
        "-p:NativeSharedDependencyPath=$PWD/$CHANGED_DEP"
    )
    [ -z "$SONAME_ASSET" ] || second_pack_args+=("-p:NativeSonameAssetPath=$PWD/$SONAME_ASSET")
    [ -z "$NO_INSTALL_ID_ASSET" ] || second_pack_args+=("-p:NativeNoInstallIdAssetPath=$PWD/$NO_INSTALL_ID_ASSET")
    dotnet pack "${second_pack_args[@]}" --nologo -v:minimal

    if NUGET_PACKAGES="$PWD/$NUGET_CACHE" dotnet publish \
            "$FIXTURE/App/$PROJECT.csproj" -c "$CONFIG" -r "$RID" \
            "-p:NativeAssetFixtureVersion=1.0.1" \
            "-p:NativeAssetFeed=$PWD/$FEED" \
            "-p:Dn2CppTool=$DN2CPP_TOOL" \
            "-p:Dn2CppCMake=$DN2CPP_CMAKE_TOOL" \
            "-p:Dn2CppExtraArgs=$PINVOKE_ARGS" \
            "-p:_Dn2CppTestFailAfterNativeAsset=$(basename "$CHANGED_SHARED")" \
            "${PUBLISH_ARGS[@]}" --nologo -v:minimal > "$OUT/msbuild-failure-probe.log" 2>&1; then
        echo "FAIL: Dn2Cpp.Build failure probe unexpectedly succeeded" >&2
        exit 1
    fi
    grep -q "injected native-generation copy failure after one asset" "$OUT/msbuild-failure-probe.log" \
        || { echo "FAIL: Dn2Cpp.Build failure probe lacked its diagnostic" >&2; cat "$OUT/msbuild-failure-probe.log" >&2; exit 1; }
    FAILED_GENERATION=$(cat "$GEN/build/native-assets-generation.txt")
    MSBUILD_PARTIAL=$(find "$PUBLISH/.dn2cpp-native" -mindepth 1 -maxdepth 1 \
        -type d -name ".new-$FAILED_GENERATION-*" -print -quit)
    [ -n "$MSBUILD_PARTIAL" ] && [ ! -e "$MSBUILD_PARTIAL/.complete" ] \
        && [ "$(find "$MSBUILD_PARTIAL" -mindepth 1 -maxdepth 1 -type f | wc -l | tr -d ' ')" -eq 1 ] \
        || { echo "FAIL: Dn2Cpp.Build failure did not leave exactly one unreachable partial asset" >&2; exit 1; }
    assert_output "$("$PUBLISH/$PROJECT$EXE_EXT")" "native-asset=42,shared-asset=7"
    [ -d "$PUBLISH/.dn2cpp-native/$GENERATION" ] \
        || { echo "FAIL: failed Dn2Cpp.Build publish removed the running generation" >&2; exit 1; }

    NUGET_PACKAGES="$PWD/$NUGET_CACHE" dotnet publish \
        "$FIXTURE/App/$PROJECT.csproj" -c "$CONFIG" -r "$RID" \
        "-p:NativeAssetFixtureVersion=1.0.1" \
        "-p:NativeAssetFeed=$PWD/$FEED" \
        "-p:Dn2CppTool=$DN2CPP_TOOL" \
        "-p:Dn2CppCMake=$DN2CPP_CMAKE_TOOL" \
        "-p:Dn2CppExtraArgs=$PINVOKE_ARGS" \
        "${PUBLISH_ARGS[@]}" --nologo -v:minimal
    NEXT_GENERATION=$(cat "$GEN/build/native-assets-generation.txt")
    [ "$NEXT_GENERATION" != "$GENERATION" ] \
        || { echo "FAIL: changed native contents reused the prior generation" >&2; exit 1; }
    [ -d "$PUBLISH/.dn2cpp-native/$GENERATION" ] \
        || { echo "FAIL: Dn2Cpp.Build removed the immediately prior native generation" >&2; exit 1; }
    PUBLISHED_SHARED="$PUBLISH/.dn2cpp-native/$NEXT_GENERATION/$(basename "$CHANGED_SHARED")"
    PUBLISHED_DEP="$PUBLISH/.dn2cpp-native/$NEXT_GENERATION/$(basename "$CHANGED_DEP")"
    [ -f "$PUBLISHED_SHARED" ] && [ -f "$PUBLISHED_DEP" ] \
        || { echo "FAIL: Dn2Cpp.Build did not publish the changed package assets" >&2; exit 1; }
    [ ! -e "$MSBUILD_PARTIAL" ] && [ -f "$PUBLISH/.dn2cpp-native/$NEXT_GENERATION/.complete" ] \
        || { echo "FAIL: Dn2Cpp.Build retry reused or retained a partial directory" >&2; exit 1; }

    if DN2CPP_TEST_NATIVE_ASSET_FAIL_AFTER=1 compile_console "$GEN" "$PROJECT" \
            > "$OUT/compile-failure-probe.log" 2>&1; then
        echo "FAIL: compile_console failure probe unexpectedly succeeded" >&2
        exit 1
    fi
    grep -q "injected native-generation copy failure after 1 asset" "$OUT/compile-failure-probe.log" \
        || { echo "FAIL: compile_console failure probe lacked its diagnostic" >&2; cat "$OUT/compile-failure-probe.log" >&2; exit 1; }
    COMMON_PARTIAL=$(find "$GEN/.dn2cpp-native" -mindepth 1 -maxdepth 1 \
        -type d -name ".new-$NEXT_GENERATION.*" -print -quit)
    [ -n "$COMMON_PARTIAL" ] && [ ! -e "$COMMON_PARTIAL/.complete" ] \
        && [ "$(find "$COMMON_PARTIAL" -mindepth 1 -maxdepth 1 -type f | wc -l | tr -d ' ')" -eq 1 ] \
        || { echo "FAIL: compile_console failure did not leave exactly one unreachable partial asset" >&2; exit 1; }
    assert_output "$("$GEN/$PROJECT$EXE_EXT")" "native-asset=42,shared-asset=7"
    [ -d "$GEN/.dn2cpp-native/$GENERATION" ] \
        || { echo "FAIL: failed compile_console removed the running generation" >&2; exit 1; }

    SWITCH_OUTPUT="$OUT/concurrent-switch.out"
    SWITCH_ERRORS="$OUT/concurrent-switch.err"
    printf '%s\n' "native-asset=42,shared-asset=7" > "$SWITCH_OUTPUT"
    compile_console "$GEN" "$PROJECT" &
    switch_pid=$!
    while kill -0 "$switch_pid" 2>/dev/null; do
        if switched=$("$GEN/$PROJECT$EXE_EXT" 2>> "$SWITCH_ERRORS"); then
            printf '%s\n' "$switched" >> "$SWITCH_OUTPUT"
        else
            printf 'loader-error\n' >> "$SWITCH_OUTPUT"
        fi
    done
    wait "$switch_pid"
    printf '%s\n' "$("$GEN/$PROJECT$EXE_EXT")" >> "$SWITCH_OUTPUT"
    if grep -Ev '^(native-asset=42,shared-asset=(7|17))$' "$SWITCH_OUTPUT"; then
        echo "FAIL: concurrent launch observed a mixed generation or loader error" >&2
        cat "$SWITCH_ERRORS" >&2
        exit 1
    fi
    grep -q 'shared-asset=7' "$SWITCH_OUTPUT" \
        && grep -q 'shared-asset=17' "$SWITCH_OUTPUT" \
        || { echo "FAIL: concurrent launch probe did not observe both generations" >&2; exit 1; }
    [ ! -e "$COMMON_PARTIAL" ] && [ -f "$GEN/.dn2cpp-native/$NEXT_GENERATION/.complete" ] \
        || { echo "FAIL: compile_console retry reused or retained a partial directory" >&2; exit 1; }
    [ -d "$GEN/.dn2cpp-native/$GENERATION" ] \
        || { echo "FAIL: compile_console removed the immediately prior native generation" >&2; exit 1; }

    echo "== Publishing a third generation and pruning only generation N-2 =="
    THIRD_NATIVE_BUILD="$OUT/third-native-build"
    THIRD_PACKAGE_DIR="$OUT/third-package"
    mkdir -p "$THIRD_PACKAGE_DIR"
    "$CMAKE" -S samples/native/nativeasset -B "$THIRD_NATIVE_BUILD" -G Ninja \
        -DCMAKE_BUILD_TYPE=Release -DDN2CPP_SHARED_DEP_VALUE=23
    "$CMAKE" --build "$THIRD_NATIVE_BUILD"
    case "$DN2CPP_OS" in
        macos)
            THIRD_SHARED="$THIRD_PACKAGE_DIR/libdn2cpp_shared_asset.dylib"
            THIRD_DEP="$THIRD_PACKAGE_DIR/dn2cpp_shared_dependency.bundle"
            cp "$THIRD_NATIVE_BUILD/libdn2cpp_shared_asset.dylib" "$THIRD_SHARED"
            cp "$THIRD_NATIVE_BUILD/libdn2cpp_shared_dependency.dylib" "$THIRD_DEP"
            ;;
        linux)
            THIRD_SHARED="$THIRD_PACKAGE_DIR/libdn2cpp_shared_asset.so.9"
            THIRD_DEP="$THIRD_PACKAGE_DIR/libdn2cpp_shared_dependency.so"
            cp "$THIRD_NATIVE_BUILD/libdn2cpp_shared_asset.so" "$THIRD_SHARED"
            cp "$THIRD_NATIVE_BUILD/libdn2cpp_shared_dependency.so" "$THIRD_DEP"
            ;;
        windows)
            THIRD_SHARED="$THIRD_PACKAGE_DIR/dn2cpp_shared_asset.dll"
            THIRD_DEP="$THIRD_PACKAGE_DIR/dn2cpp_shared_dependency.dll"
            cp "$THIRD_NATIVE_BUILD/dn2cpp_shared_asset.dll" "$THIRD_SHARED"
            cp "$THIRD_NATIVE_BUILD/dn2cpp_shared_dependency.dll" "$THIRD_DEP"
            ;;
    esac
    third_pack_args=(
        "$FIXTURE/Package/NativeAssetPackage.csproj"
        -c "$CONFIG" -o "$FEED"
        -p:FixtureVersion=1.0.2
        "-p:NativeAssetRid=$RID"
        "-p:NativeAssetPath=$PWD/$NATIVE_LIB"
        "-p:NativeStaticDependencyPath=$PWD/$NATIVE_STATIC_DEP"
        "-p:NativeAssetDecoyRid=$DECOY_RID"
        "-p:NativeAssetDecoyPath=$PWD/$DECOY_LIB"
        "-p:NativeSharedAssetRid=$SHARED_RID"
        "-p:NativeSharedAssetPath=$PWD/$THIRD_SHARED"
        "-p:NativeSharedDependencyPath=$PWD/$THIRD_DEP"
    )
    [ -z "$SONAME_ASSET" ] || third_pack_args+=("-p:NativeSonameAssetPath=$PWD/$SONAME_ASSET")
    [ -z "$NO_INSTALL_ID_ASSET" ] || third_pack_args+=("-p:NativeNoInstallIdAssetPath=$PWD/$NO_INSTALL_ID_ASSET")
    dotnet pack "${third_pack_args[@]}" --nologo -v:minimal
    NUGET_PACKAGES="$PWD/$NUGET_CACHE" dotnet publish \
        "$FIXTURE/App/$PROJECT.csproj" -c "$CONFIG" -r "$RID" \
        "-p:NativeAssetFixtureVersion=1.0.2" \
        "-p:NativeAssetFeed=$PWD/$FEED" \
        "-p:Dn2CppTool=$DN2CPP_TOOL" \
        "-p:Dn2CppCMake=$DN2CPP_CMAKE_TOOL" \
        "-p:Dn2CppExtraArgs=$PINVOKE_ARGS" \
        "${PUBLISH_ARGS[@]}" --nologo -v:minimal
    THIRD_GENERATION=$(cat "$GEN/build/native-assets-generation.txt")
    [ "$THIRD_GENERATION" != "$NEXT_GENERATION" ] && [ "$THIRD_GENERATION" != "$GENERATION" ] \
        || { echo "FAIL: third native contents reused an earlier generation" >&2; exit 1; }
    [ ! -e "$PUBLISH/.dn2cpp-native/$GENERATION" ] \
        || { echo "FAIL: Dn2Cpp.Build did not prune generation N-2" >&2; exit 1; }
    [ -d "$PUBLISH/.dn2cpp-native/$NEXT_GENERATION" ] \
        && [ -d "$PUBLISH/.dn2cpp-native/$THIRD_GENERATION" ] \
        || { echo "FAIL: Dn2Cpp.Build did not retain the current and previous generations" >&2; exit 1; }
    compile_console "$GEN" "$PROJECT"
    [ ! -e "$GEN/.dn2cpp-native/$GENERATION" ] \
        || { echo "FAIL: compile_console did not prune generation N-2" >&2; exit 1; }
    [ -d "$GEN/.dn2cpp-native/$NEXT_GENERATION" ] \
        && [ -d "$GEN/.dn2cpp-native/$THIRD_GENERATION" ] \
        || { echo "FAIL: compile_console did not retain the current and previous generations" >&2; exit 1; }
    GENERATION=$THIRD_GENERATION
fi

echo "== 6/6 Removing the package cache and running the dn2cpp binary =="
rm -rf "$NUGET_CACHE"
if [ "$DN2CPP_OS" = linux ]; then
    rm -rf "$NATIVE_BUILD" "${CHANGED_NATIVE_BUILD:-$OUT/no-changed-build}" \
        "${THIRD_NATIVE_BUILD:-$OUT/no-third-build}" "$GEN/build"
fi
NATIVE_EXE="$PUBLISH/$PROJECT$EXE_EXT"
[ -f "$NATIVE_EXE" ] || { echo "FAIL: native publish produced no $NATIVE_EXE" >&2; exit 1; }
native_out=$("$NATIVE_EXE")
if [ -n "$SHARED_LIB" ]; then
    assert_output "$native_out" "native-asset=42,shared-asset=27"
    assert_output "$("$GEN/$PROJECT$EXE_EXT")" "native-asset=42,shared-asset=27"
else
    assert_output "$native_out" "native-asset=42"
fi

echo "OK: NuGet native assets linked and ran after their package cache was removed"

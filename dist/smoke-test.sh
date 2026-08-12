#!/usr/bin/env bash
# dist/smoke-test.sh — prove the packaged export toolchain is self-contained.
#
# Using ONLY the bundle's own contents — the native bin/dn2cpp, the runtime/
# source, third_party/, the ref/ framework closure and the buildtools/ cmake +
# ninja — plus the DM sample's game IL, transpile + native-build the sample into
# a Godot mono-module drop-in
# and assert it exports godotsharp_game_main_init. That is the exact artifact
# shape a forked editor's dn2cpp export backend produces, and the symbol the
# engine's try_load_native_aot_library resolves.
#
# Then again for the Web, through the Emscripten SDK the bundle carries. On Linux
# THAT SECTION IS WHAT ACCEPTS THE TARBALL: the fork's Web-export gate links the
# same toolchain, but only from a baked Web template, and none is cut for Linux.
#
# NOT a regression gate: the filename is deliberately outside the
# build-and-run-*.sh glob (like the sibling selfhost-*.sh), so run-all-gates.sh
# ignores it. Opts out via gate_skip (exit 77) when the DM cache is absent —
# never "SKIP + exit 0", which is indistinguishable from a pass.
#
# The bundle's runtime/ is configured in a single CMake pass with no
# DN2CPP_RUNTIME_EXPORT, so the transpile + build touch nothing outside the
# bundle — and that is also what makes this the end-to-end proof of the bundle's
# own prebuilt runtime: with nothing pointing at an export, the configure
# adopts prebuilt/ exactly as an export does, and the link below is the only
# thing anywhere that resolves those archives. Only building the sample's *input*
# game IL legitimately uses the DM cache's real Godot.NET.Sdk + GodotSharp — that
# IL is the exporter's input and is never shipped to players.
#
# Usage: dist/smoke-test.sh [BUNDLE_DIR]
#   BUNDLE_DIR defaults to the standard layout under artifacts/toolchain/,
#   assembled on demand (reusing a prebuilt artifacts/selfhost-fullcli/dn2cpp).
source "$(dirname "$0")/../gates/_common.sh"        # repo-root cd, bundled_cmake, lib_name, DN2CPP_OS
source "$(dirname "$0")/../gates/_godot_dotnet.sh"  # GODOT_DOTNET_*, godot_dotnet_root_ok

# 1. Opt out (exit 77) when the DM cache the sample build needs is absent.
godot_dotnet_root_ok \
    || gate_skip "DM cache absent (set DN2CPP_GODOT_DOTNET_ROOT to gates/setup-godot-dotnet.sh output)"

# 2. Locate the bundle; assemble the standard layout on demand.
BUNDLE="${1:-}"
if [ -z "$BUNDLE" ]; then
    BUNDLE="artifacts/toolchain/dn2cpp-toolchain-0.1.0-${DN2CPP_OS}-$(uname -m)"
    if [ ! -x "$BUNDLE/bin/dn2cpp" ]; then
        echo "-- assembling the bundle (dist/package-toolchain.sh --layout-only)"
        bash dist/package-toolchain.sh --layout-only >/dev/null
    fi
fi
[ -x "$BUNDLE/bin/dn2cpp" ]                    || { echo "error: no bundle at $BUNDLE (bin/dn2cpp missing)" >&2; exit 1; }
# Absolute from here: CMAKE_MAKE_PROGRAM below lands in the cache, and a
# try_compile resolves it from its own scratch directory — where the default
# repo-relative spelling names nothing.
BUNDLE="$(cd "$BUNDLE" && pwd)"
[ -f "$BUNDLE/ref/System.Private.CoreLib.dll" ] || { echo "error: bundle ref/ is missing CoreLib" >&2; exit 1; }
# The bundle ships the managed support shim and the conditionally-referenced managed
# backends beside the CLI, where the native binary's AppContext.BaseDirectory
# auto-reference finds them; the transpile below also passes the shim with -r, as the
# exporter does. Assert they are there: a bundle without the shim cannot transpile a
# program that enumerates an array — the emit needs SZArrayEnumerable<T> for the array's
# IEnumerable<T> interface map — and one missing a backend cannot transpile a game whose
# IL reaches the swapped-out surface (zlib/brotli decompression, HttpClient). In both
# cases this script would otherwise report a wiring accident as a downstream failure,
# blaming the transpile or the native build for a file the packaging never copied.
for sib in Dn2Cpp.Runtime DnZlib DnBrotli DnHttp; do
    [ -f "$BUNDLE/bin/$sib.dll" ] || {
        echo "error: bundle bin/ is missing $sib.dll" >&2; exit 1; }
done
# The prebuilt runtime degrades to a from-source build when it is absent, so its
# absence costs export time and nothing else — which is precisely why it needs an
# assertion somewhere, or a packaging that quietly stopped shipping it would never
# be noticed. Step 5 asserts it is also USED.
[ -f "$BUNDLE/prebuilt/host/dn2cpp-targets.cmake" ] || {
    echo "error: bundle carries no prebuilt/host/dn2cpp-targets.cmake (packaged with --no-prebuilt?)" >&2
    exit 1; }
# The bundle exists so a machine with no C++ build tools can export; a smoke test
# that uses the host's cmake proves the bundle only for machines that need no
# bundle. Both configures below run these two and nothing on PATH.
SMOKE_CMAKE="$(bundled_cmake "$BUNDLE")"
SMOKE_NINJA="$(bundled_ninja "$BUNDLE")"
for bt in "$SMOKE_CMAKE" "$SMOKE_NINJA"; do
    [ -x "$bt" ] || {
        echo "error: bundle carries no executable $bt (gates/setup-buildtools.sh unpacks the pinned pair" >&2
        echo "       dist/package-toolchain.sh stages)" >&2
        exit 1; }
done
# Typed :FILEPATH — an untyped -D lands in the cache as UNINITIALIZED, which the
# fork's stale-cache check reads back.
SMOKE_MAKE_ARG="-DCMAKE_MAKE_PROGRAM:FILEPATH=$(native_path "$SMOKE_NINJA")"
echo "== smoke-testing bundle: $BUNDLE =="

# 3. Build the DM sample's game IL (real Godot.NET.Sdk). Mirrors the sample-build
#    half of gates/_godot_dotnet.sh::godot_dotnet_transpile; the generated
#    nuget.config points the Sdk resolver at the DM cache's local feed (gitignored,
#    machine-specific — generated, never committed).
#
#    Through that file's own writer, never a copy of it: NuGet reads the config as
#    a native program, so the feed needs the native spelling, and the copy that
#    stood here had silently dropped the conversion — an MSYS path resolves to
#    C:\c\Users\... and surfaces as a restore error naming Godot.NET.Sdk, not a path.
echo "-- building the DM sample game assembly (real Godot.NET.Sdk)"
godot_dotnet_nuget_config "$GODOT_DOTNET_SAMPLE_DIR"
dotnet build "$GODOT_DOTNET_SAMPLE_DIR/DotnetSample.csproj" -c ExportRelease --nologo -v q
APP="$GODOT_DOTNET_SAMPLE_DIR/.godot/mono/temp/bin/ExportRelease/DotnetSample.dll"
[ -f "$APP" ] || { echo "error: sample not built: $APP" >&2; exit 1; }

# 4. Transpile with ONLY the bundle's native CLI + its ref/ closure. --auto-ref
#    resolves the game's BCL deps from the dir holding CoreLib (the bundle's ref/);
#    the editor's own GodotSharp.dll (here the DM cache's) supplies the bindings.
#    The support shim goes last, mirroring where TranspileDriver appends the copy it
#    auto-references — this is the same command line Dn2CppExporter builds. Passing
#    it explicitly is belt and braces: the auto-reference already resolves it from
#    bin/, but an explicit path survives a relocated or symlinked binary.
WORK="$(mktemp -d)"
trap 'rm -rf "$WORK"' EXIT
GEN="$WORK/gen"; mkdir -p "$GEN"
echo "-- transpiling with the bundle's native dn2cpp (--dotnet-module, --auto-ref)"
"$BUNDLE/bin/dn2cpp" "$APP" --dotnet-module \
    -r "$BUNDLE/ref/System.Private.CoreLib.dll" \
    -r "$GODOT_DOTNET_GODOTSHARP" \
    -r "$BUNDLE/bin/Dn2Cpp.Runtime.dll" \
    --auto-ref -o "$GEN"
[ -f "$GEN/generated.cpp" ] || { echo "error: transpile produced no generated.cpp" >&2; exit 1; }

# 5. Native-build against the bundle's runtime in one configure pass. No
#    DN2CPP_RUNTIME_EXPORT: runtime/CMakeLists.txt imports the bundle's own
#    prebuilt/ (or, when the key does not describe this host, compiles the core +
#    dotnetmodule host from source) alongside the app — DN2CPP_ROOT =
#    <bundle>/runtime/.. resolves both inside the bundle. The configure is teed,
#    not discarded, for the same reason it was never >/dev/null: native errors
#    have to be visible.
BUILD="$WORK/build"
echo "-- building the mono-module library from the bundle's runtime"
"$SMOKE_CMAKE" -S "$BUNDLE/runtime" -B "$BUILD" -G Ninja "$SMOKE_MAKE_ARG" \
    -DDN2CPP_DOTNET_MODULE=ON \
    -DDN2CPP_APP_DIR="$GEN" \
    -DDN2CPP_APP_NAME=DotnetSample 2>&1 | tee "$WORK/configure.log"
[ "${PIPESTATUS[0]}" -eq 0 ] || { echo "error: cmake configure failed" >&2; exit 1; }
# A bundle built on this host must adopt its own prebuilt: a key mismatch here is
# not a portability degrade, it is the packaging and the consumer disagreeing
# about one key they both compute from runtime/cmake/dn2cpp_prebuilt.cmake.
grep -q "using the prebuilt runtime" "$WORK/configure.log" || {
    echo "error: the bundle's own prebuilt runtime was refused by its own runtime/CMakeLists.txt" >&2
    grep -i "prebuilt" "$WORK/configure.log" >&2 || true
    exit 1; }
"$SMOKE_CMAKE" --build "$BUILD" || { echo "error: native build failed" >&2; exit 1; }
DYLIB="$BUILD/$(lib_name DotnetSample)"
[ -f "$DYLIB" ] || { echo "error: mono-module library not produced: $DYLIB" >&2; exit 1; }

# 6. Assert the mono-module entry point is exported. Through dump_exports, which
#    is the per-platform spelling of that one question — `nm -gU` is Mach-O only,
#    and a PE export directory is dumpbin's to read. grep without -q so it drains
#    the full output: under the sourced `set -o pipefail`, `grep -q` exits on the
#    first match, the producer dies of SIGPIPE, and the pipeline reports a bogus
#    failure.
if dump_exports "$DYLIB" | grep "godotsharp_game_main_init" >/dev/null; then
    echo "OK: bundle round-trips the DM sample — $(basename "$DYLIB") exports godotsharp_game_main_init"
else
    echo "FAIL: $DYLIB does not export godotsharp_game_main_init" >&2
    exit 1
fi

# 7. The same round trip cross-compiled to wasm by the SDK inside the bundle —
# the one axis whose toolchain is shipped rather than assumed present.
#
# One legitimate reason not to run it, and it prints that reason: a bundle
# packaged on a host with no Emscripten SDK carries none (its users do not export
# to the Web). Which of the two happened has to be readable from the output — a
# section that quietly did nothing reads exactly like one that passed, and on
# Linux that section is the whole acceptance gate.
WEB_EMCMAKE="$BUNDLE/emsdk/emscripten/emcmake"
SMOKE_NODE="$(bundled_node "$BUNDLE")"
if [ ! -x "$WEB_EMCMAKE" ]; then
    echo "SKIPPED (web axis): the bundle carries no emsdk/ — packaged on a host with none"
else
    # An error, not a skip: every emcc link runs a node and the SDK stages the
    # pinned one, so an SDK standing here without it is a broken bundle, not a
    # host without a Web toolchain.
    [ -x "$SMOKE_NODE" ] || {
        echo "error: bundle carries an emsdk/ but no executable $SMOKE_NODE (gates/setup-emsdk.sh" >&2
        echo "       unpacks the pinned node dist/package-toolchain.sh stages inside the SDK)" >&2
        exit 1; }
    echo "== web axis: cross-compiling the same sample with the bundle's own Emscripten SDK =="
    web_t0=$SECONDS
    # The CoreLib flavour the exporter picks for a cross target
    # (Dn2CppToolchain.CoreLibRefFor). A Windows bundle's ref/ names kernel32 and
    # ole32, which Emscripten's sysroot has neither of — that is what ref-posix/ is
    # staged for. On a POSIX host ref/ already is that flavour.
    WEB_CORELIB="$BUNDLE/ref/System.Private.CoreLib.dll"
    [ -f "$BUNDLE/ref-posix/System.Private.CoreLib.dll" ] \
        && WEB_CORELIB="$BUNDLE/ref-posix/System.Private.CoreLib.dll"
    # --trim-reflection because the Web export passes it: untrimmed, the PIC
    # data-relocation applier is a single function past V8's ceiling and no engine
    # can instantiate the module.
    WEBGEN="$WORK/gen-web"; mkdir -p "$WEBGEN"
    echo "-- transpiling for the Web ($(basename "$(dirname "$WEB_CORELIB")")/ CoreLib, --trim-reflection)"
    "$BUNDLE/bin/dn2cpp" "$APP" --dotnet-module \
        -r "$WEB_CORELIB" \
        -r "$GODOT_DOTNET_GODOTSHARP" \
        -r "$BUNDLE/bin/Dn2Cpp.Runtime.dll" \
        --auto-ref --trim-reflection -o "$WEBGEN"
    [ -f "$WEBGEN/generated.cpp" ] || { echo "error: the web transpile produced no generated.cpp" >&2; exit 1; }

    # The bundle's own emcmake and its own config, with every variable an activated
    # SDK exports cleared: those outrank the config file, so an inherited one would
    # steer this link to the host's SDK and cache — proving nothing about the
    # bundle, and on a machine with no SDK failing for a reason this never saw.
    # EM_NODE_JS is in that set for the reason EMSDK_PYTHON is: it outranks the
    # config's NODE_JS, so an inherited one substitutes the host's node for the
    # bundle's and the section stops being about the bundle at all.
    # EM_CONFIG (and the staged python below) go through native_path: emcc reads
    # both as a native program, which never resolves an MSYS spelling.
    WEBBUILD="$WORK/build-web"
    WEB_ENV=(-u EMSDK -u EMSDK_NODE -u EMSCRIPTEN -u EM_CACHE -u EM_FROZEN_CACHE \
        -u EM_LLVM_ROOT -u EM_BINARYEN_ROOT -u EMSDK_PYTHON -u EM_NODE_JS \
        "EM_CONFIG=$(native_path "$(cd "$BUNDLE/emsdk/emscripten" && pwd)/.emscripten")")
    # A bundle that stages its own python must be proven THROUGH it: emcc.exe picks
    # the EMSDK_PYTHON interpreter ahead of any PATH search, so leaving the variable
    # merely cleared would silently substitute the host's python for the bundle's.
    [ -x "$BUNDLE/emsdk/python/python.exe" ] \
        && WEB_ENV+=("EMSDK_PYTHON=$(native_path "$(cd "$BUNDLE/emsdk/python" && pwd)/python.exe")")
    echo "-- configuring the wasm side module through $WEB_EMCMAKE"
    env "${WEB_ENV[@]}" \
        "$WEB_EMCMAKE" "$SMOKE_CMAKE" -S "$BUNDLE/runtime" -B "$WEBBUILD" -G Ninja "$SMOKE_MAKE_ARG" \
        -DDN2CPP_DOTNET_MODULE=ON \
        -DDN2CPP_APP_DIR="$WEBGEN" \
        -DDN2CPP_APP_NAME=DotnetSample 2>&1 | tee "$WORK/configure-web.log"
    [ "${PIPESTATUS[0]}" -eq 0 ] || { echo "error: the web cmake configure failed" >&2; exit 1; }
    # Same demand as step 5, and for a sharper reason: building the runtime and its
    # vendored trees from source under Emscripten succeeds, so a web-wasm32 prebuilt
    # whose key no longer describes the bundle's own SDK costs every user of this
    # bundle a full cross build and is otherwise invisible.
    grep -q "using the prebuilt runtime" "$WORK/configure-web.log" || {
        echo "error: the bundle's web prebuilt runtime was refused by its own runtime/CMakeLists.txt" >&2
        grep -i "prebuilt" "$WORK/configure-web.log" >&2 || true
        exit 1; }
    "$SMOKE_CMAKE" --build "$WEBBUILD" || { echo "error: the web native build failed" >&2; exit 1; }
    # lib<name>.so whatever the host is: the CMake target pins PREFIX/SUFFIX on the
    # Emscripten arm precisely so a consumer has a stable name to take.
    WEBSO="$WEBBUILD/libDotnetSample.so"
    [ -f "$WEBSO" ] || { echo "error: the wasm side module was not produced: $WEBSO" >&2; exit 1; }
    # A release side module has no name section, so grepping the file for a symbol
    # would also match an import: parse the export section.
    "$SMOKE_NODE" gates/_wasm_symbols.js exports "$WEBSO" > "$WORK/web-exports.txt"
    grep -qx "func godotsharp_game_main_init" "$WORK/web-exports.txt" || {
        echo "FAIL: $WEBSO does not export godotsharp_game_main_init" >&2
        cat "$WORK/web-exports.txt" >&2
        exit 1; }
    echo "OK: web axis — $(basename "$WEBSO") exports godotsharp_game_main_init" \
         "($(wc -c < "$WEBSO" | tr -d ' ') bytes, $((SECONDS - web_t0))s)"
fi

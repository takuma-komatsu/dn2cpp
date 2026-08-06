#!/usr/bin/env bash
# dist/package-toolchain.sh — assemble the self-contained dn2cpp export toolchain
# a forked Godot editor installs into GodotSharp/Dn2Cpp/ and uses to transpile a
# published C# game into a mono-module drop-in library (godotsharp_game_main_init)
# WITHOUT a .NET runtime on the shipped game. See docs/EDITOR-EXPORT-DESIGN.md §4.
#
# The bundle carries everything the exporter's transpile + native build need:
#   bin/dn2cpp        self-hosted native CLI (gates/selfhost-emit.sh; no .NET dep)
#   bin/Dn2Cpp.Runtime.dll
#                     the managed support shim holding the types the transpiler
#                     synthesizes — notably SZArrayEnumerable<T>, which backs
#                     ((IEnumerable<T>)array).GetEnumerator(). It must sit beside
#                     the CLI: TranspileDriver auto-references it from
#                     AppContext.BaseDirectory, which a native binary answers with
#                     its own directory. Consumers may also pass it with -r, and the
#                     exporter does — an explicit reference cannot be defeated by a
#                     relocated or symlinked binary. Either way a transpile that
#                     needs the shim and cannot find it now fails outright, rather
#                     than emitting arrays with no IEnumerable<T> interface map that
#                     link, export the entry point and die in dn2cpp_resolve_interface.
#   bin/DnZlib.dll bin/DnBrotli.dll bin/DnHttp.dll
#                     the managed backends the transpiler references CONDITIONALLY,
#                     resolved from the same AppContext.BaseDirectory as the shim. A
#                     game that reaches none of them pays one extra row in the emitted
#                     assembly registry and nothing else — no body of theirs is
#                     reachable, so none is transpiled into the shipped binary. They
#                     ship unconditionally all the same: which of them a given game
#                     needs is decided by that game's IL, which the packaging cannot
#                     see, and a bundle missing one fails a transpile in a forked
#                     editor's export log rather than here.
#   runtime/          core (+intrinsics/PAL) + dotnetmodule host + CMakeLists.txt
#                     + cmake/ (dn2cpp_embed_bytes.cmake, the -P script the
#                      DN2CPP_USE_CURL arm runs to embed the CA bundle)
#                     (runtime/godot omitted — DN2CPP_GODOT stays OFF for the
#                      dotnet-module build, so the bridge is never referenced)
#   third_party/      every vendored native tree runtime/CMakeLists.txt reaches
#                     for: bdwgc brotli curl highway mbedtls zlib, the cacert CA
#                     bundle the DN2CPP_USE_CURL arm embeds, and
#                     gdextension_interface.h. Each tree whole — see the copy
#                     loop for why no packaging-time subset is available.
#   ref/              the pinned net10 shared-framework managed DLLs — the WHOLE
#                     closure, because --auto-ref resolves the BCL from the dir
#                     that holds System.Private.CoreLib.dll (Compilation.cs
#                     LoadReferenceClosure). Shipping CoreLib alone would leave
#                     System.Runtime/… unresolved for real game IL.
#   ref-posix/        the same closure in its POSIX flavour, staged on a WINDOWS
#                     host only, for the exports that are cross-compiled (Android,
#                     Web). The transpiler consumes the CoreLib's IL, so its
#                     flavour is what decides the native libraries the emitted
#                     P/Invokes name: a Windows framework names kernel32/ntdll and
#                     ole32, and neither the NDK sysroot nor Emscripten has any of
#                     them. On a POSIX host `ref/` already is that flavour and this
#                     directory is absent — which is exactly why the hole stayed
#                     invisible while the lane was macOS-only. The fork picks
#                     between the two per export target
#                     (Dn2CppToolchain.CoreLibRefFor).
#   prebuilt/<axis>/  the runtime archives + a relocated dn2cpp-targets.cmake, so
#                     an export imports the runtime instead of configuring and
#                     compiling it and its seven vendored trees. One directory per
#                     axis — the host's, plus iOS device and both simulator arches
#                     on a macOS host, plus Android and Web where the packaging
#                     host has the NDK / emsdk. Keyed on the toolchain that built
#                     it and on a stamp of the staged sources; a consumer whose
#                     key matches none builds from source.
#   emsdk/            the pinned Emscripten SDK (gates/expected/emsdk-pin.txt), so
#                     a Web export cross-compiles on a host with none installed.
#                     Its toolchain file being INSIDE the bundle is also what makes
#                     the web prebuilt portable: the key records a path under the
#                     root relative to it. Its cache is baked and FROZEN, so an
#                     export never writes into the bundle. Absent when the
#                     packaging host had none.
#   buildtools/       the pinned cmake + ninja (gates/expected/buildtools-pin.txt),
#                     so an export configures and compiles on a host carrying
#                     neither — and through a cmake whose version this repository
#                     chose, which the exporter cannot say of one on PATH. Cut to
#                     the single cmake executable, its Modules/ tree and the
#                     licence texts, and thinned to arm64 on macOS. Absent when
#                     the packaging host had none unpacked.
#   manifest.json     the fork<->dn2cpp version contract (commit, godot pin, ABI
#                     fingerprint, content hash)
#
# Usage:
#   dist/package-toolchain.sh [options]
#     --no-prebuilt       skip prebuilt/ (DN2CPP_NO_PREBUILT=1 does the same)
#     --layout-only       assemble the layout dir; skip the .tar.gz
#     --install-into DIR  after assembling, copy the layout tree into DIR
#                         (mkdir -p; e.g. <editor>/GodotSharp/Dn2Cpp)
#     --dn2cpp-bin PATH   use this prebuilt native dn2cpp instead of building it
#     --emsdk DIR         bundle this Emscripten SDK, and build the Web axis with it
#     --buildtools DIR    bundle this cmake+ninja tree (gates/setup-buildtools.sh's
#                         --out; $DN2CPP_BUILDTOOLS does the same)
#     --version V         package version string (default: 0.1.0)
#     --out DIR           layout/tarball parent (default: artifacts/toolchain)
#     -h | --help
#
# Sourcing _common.sh cd's to the repo root and provides resolve_net10_corelib +
# DN2CPP_OS. `dotnet` (SDK) is needed only to build the self-host binary and to
# resolve the framework; a prepared --dn2cpp-bin keeps the SDK the sole dep.
source "$(dirname "$0")/../gates/_common.sh"

PKG_VERSION=0.1.0
OUT_PARENT=artifacts/toolchain
LAYOUT_ONLY=0
INSTALL_INTO=
DN2CPP_BIN=
EMSDK_DIR=
BUILDTOOLS_DIR=
WITH_PREBUILT=1
[ -n "${DN2CPP_NO_PREBUILT:-}" ] && WITH_PREBUILT=0

while [ $# -gt 0 ]; do
    case "$1" in
        --no-prebuilt)  WITH_PREBUILT=0; shift ;;
        --layout-only)  LAYOUT_ONLY=1; shift ;;
        --install-into) INSTALL_INTO="$2"; shift 2 ;;
        --dn2cpp-bin)   DN2CPP_BIN="$2"; shift 2 ;;
        --emsdk)        EMSDK_DIR="$2"; shift 2 ;;
        --buildtools)   BUILDTOOLS_DIR="$2"; shift 2 ;;
        --version)      PKG_VERSION="$2"; shift 2 ;;
        --out)          OUT_PARENT="$2"; shift 2 ;;
        # The header block, ended by the first line that is not a comment. A line
        # range would truncate --help silently the next time the header grows.
        -h|--help)      awk 'NR > 1 && /^#/ { print; next } NR > 1 { exit }' "$0"; exit 0 ;;
        *) echo "error: unknown argument: $1" >&2; exit 2 ;;
    esac
done

HOST_OS="$DN2CPP_OS"
HOST_ARCH="$(uname -m)"
NAME="dn2cpp-toolchain-${PKG_VERSION}-${HOST_OS}-${HOST_ARCH}"
LAYOUT="$OUT_PARENT/$NAME"

echo "== dn2cpp toolchain bundle: $NAME =="

# 1. Native self-hosted CLI (the enabler — transpiles with no .NET present).
#
# A prebuilt one is reused only when it was built from the sources now in the tree,
# never merely because a file is sitting there. `[ -x ]` alone is how a binary from
# an older tree once got bundled into the fork's export toolchain and failed every
# gate in that chain — some on a runtime header that had since moved out of the
# runtime's own header and into the emitted one, the rest on a CLI flag that did not
# exist yet: one cause, differing only in which stage died first. Nothing upstream
# could see it, because the binary is an INPUT to the packaging — a stale one
# produces a bundle that packages and installs perfectly.
#
# `src_tree_hash` (gates/_common.sh) is the shared definition; gates/selfhost-emit.sh
# writes it beside the binary it links. mtime was the cheap alternative and is the
# wrong one — `find src -newer <binary>` re-fires on every `git checkout`, i.e. an
# unconditional full self-host rebuild for switching branches. So is the manifest's
# `dn2cpp_commit`: it names the commit the BUNDLE was cut at and cannot see an
# uncommitted edit under `src/`, which is the state this runs in most of the time.
#
# An explicit --dn2cpp-bin is honoured as given, unchecked: the caller named a
# specific file (every gate does, through stage_editor_toolchain), and second-guessing
# a named path is not this script's business. The blind-reuse branch below is
# therefore reached only by gates/setup-godot-fork.sh and by hand-run packaging —
# which is precisely where that stale binary came from. The gates cover their
# own side of that bargain: godot_fork_preflight (gates/_godot_fork.sh) runs
# this same stamp comparison against $SELFHOST_BIN before the cache check and
# FAILs on a mismatch, so the path they name here is one they have verified.
if [ -z "$DN2CPP_BIN" ]; then
    DN2CPP_BIN=artifacts/selfhost-fullcli/dn2cpp$EXE_EXT
    DN2CPP_SRC_STAMP=artifacts/selfhost-fullcli/dn2cpp.src-hash
    DN2CPP_SRC_HASH="$(src_tree_hash)"
    if [ ! -x "$DN2CPP_BIN" ] || [ ! -f "$DN2CPP_SRC_STAMP" ] \
        || [ "$(cat "$DN2CPP_SRC_STAMP")" != "$DN2CPP_SRC_HASH" ]; then
        if [ -x "$DN2CPP_BIN" ]; then
            echo "-- the native dn2cpp at $DN2CPP_BIN predates the current sources"
            echo "   stamped $(cat "$DN2CPP_SRC_STAMP" 2>/dev/null || echo '<no stamp>') != $DN2CPP_SRC_HASH"
        fi
        echo "-- building the self-hosted native dn2cpp (gates/selfhost-emit.sh)"
        # What the bundle needs is the binary step 4/5 links. Step 5/5 is a separate
        # fixpoint proof (native re-transpile == managed emit); it exits non-zero on
        # a divergence, and a divergence is an open ticket, not a reason to leave the
        # already-built binary unpackaged. The [ -x ] below is the check that counts.
        ./gates/selfhost-emit.sh \
            || echo "warning: gates/selfhost-emit.sh did not complete; packaging the binary it produced" >&2
    else
        echo "-- reusing native dn2cpp: $DN2CPP_BIN (built from src $DN2CPP_SRC_HASH)"
    fi
fi
[ -x "$DN2CPP_BIN" ] || { echo "error: native dn2cpp not executable: $DN2CPP_BIN" >&2; exit 1; }

# 1b. The managed support shim that must sit next to the CLI (see the header).
SHIM="src/Dn2Cpp.Runtime/bin/$CONFIG/$TFM/Dn2Cpp.Runtime.dll"
[ -f "$SHIM" ] || build_proj src/Dn2Cpp.Runtime/Dn2Cpp.Runtime.csproj
[ -f "$SHIM" ] || { echo "error: managed support shim not built: $SHIM" >&2; exit 1; }
echo "-- managed support shim: $SHIM"

# 1c. The conditionally-referenced managed backends, which sit beside the CLI for the
# same reason the shim does (see the header). Each lives in its own internal/ solution
# with a uniform layout — internal/<N>/src/<N>/<N>.csproj — so the paths are derived
# from one name apiece rather than written out three times.
MANAGED_BACKENDS="DnZlib DnBrotli DnHttp"
for be in $MANAGED_BACKENDS; do
    be_dll="internal/$be/src/$be/bin/$CONFIG/$TFM/$be.dll"
    [ -f "$be_dll" ] || build_proj "internal/$be/src/$be/$be.csproj"
    [ -f "$be_dll" ] || { echo "error: managed backend not built: $be_dll" >&2; exit 1; }
    echo "-- managed backend: $be_dll"
done

# 2. The pinned net10 shared-framework directory (CoreLib + its BCL siblings).
corelib="$(resolve_net10_corelib)"
fwdir="$(dirname "$corelib")"
fwver="$(basename "$fwdir")"
echo "-- net10 shared framework: $fwdir ($fwver)"

# 3. Assemble the layout. Cleared AROUND the two staged trees — emsdk/ (1.4 GB)
# and buildtools/ — each of which is re-staged only when its own stamp moves (3b,
# 3b′); everything else is rebuilt from the tree. A tree left out of this
# exclusion is re-staged every run, and a run that died mid-stage would leave a
# half tree its stamp cannot see.
if [ -d "$LAYOUT" ]; then
    find "$LAYOUT" -mindepth 1 -maxdepth 1 ! -name emsdk ! -name buildtools -exec rm -rf {} +
fi
mkdir -p "$LAYOUT/bin" "$LAYOUT/runtime" "$LAYOUT/third_party" "$LAYOUT/ref"

# The bundled CLI's name is a CONTRACT with the fork, not a convention: the
# editor resolves it as Dn2CppToolchain.Dn2CppExe (`OS.IsWindows ? "dn2cpp.exe"
# : "dn2cpp"`) and refuses the whole bundle as incomplete when that exact file
# is absent. Extension-less would be wrong twice over on Windows — the toolchain
# validation would not find it, and CreateProcess cannot launch a PE image
# without the suffix even though `test -x` under MSYS says it can.
install -m 0755 "$DN2CPP_BIN" "$LAYOUT/bin/dn2cpp$EXE_EXT"
install -m 0644 "$SHIM" "$LAYOUT/bin/Dn2Cpp.Runtime.dll"
for be in $MANAGED_BACKENDS; do
    install -m 0644 "internal/$be/src/$be/bin/$CONFIG/$TFM/$be.dll" "$LAYOUT/bin/$be.dll"
done

# Everything the CLI carries as a SIBLING has to be here, and the required set is
# derived from the CLI's own csproj rather than from the list above — the same move
# the runtime/ and third_party/ guards further down make, for the same reason. The
# hardcoded-enumeration failure has already shipped once in this file: the
# DN2CPP_USE_CURL arm landed naming third_party/{curl,mbedtls,cacert} while the copy
# loop still copied four trees, and the miss surfaced only inside a forked editor's
# export. A fourth conditionally-referenced backend added to Dn2Cpp.Cli.csproj and
# forgotten here would fail the same way — a bundle that packages, installs and
# transpiles most games perfectly, and dies on the first game whose IL reaches the
# assembly nobody copied, in a log nobody reads. Deriving means forgetting this file
# fails HERE instead.
#
# A ProjectReference is a sibling unless it is a LINK-time reference — those three are
# compiled into dn2cpp.dll's dependency set and travel as ordinary managed deps of the
# CLI, not as files the transpiler reads from AppContext.BaseDirectory. Everything else
# in that ItemGroup is a ReferenceOutputAssembly="false" payload, which is exactly the
# set that has to be in bin/. Each row's assembly name is its csproj basename.
CLI_LINK_TIME_REFS="Dn2Cpp.Transpiler Dn2Cpp.Godot Dn2Cpp.DotnetModule"
bin_missing=""
for sib in $(grep -o 'ProjectReference Include="[^"]*"' src/Dn2Cpp.Cli/Dn2Cpp.Cli.csproj \
                 | sed -e 's|"$||' -e 's|.*/||' -e 's|\.csproj$||' | sort -u); do
    case " $CLI_LINK_TIME_REFS " in *" $sib "*) continue ;; esac
    [ -f "$LAYOUT/bin/$sib.dll" ] || bin_missing="$bin_missing $sib"
done
[ -z "$bin_missing" ] || {
    echo "error: Dn2Cpp.Cli.csproj carries assemblies as siblings that the bundle drops:$bin_missing" >&2
    echo "       the transpiler resolves them from AppContext.BaseDirectory, which for the" >&2
    echo "       bundled native CLI is bin/ — add them to MANAGED_BACKENDS (or to the" >&2
    echo "       installs above) in dist/package-toolchain.sh" >&2
    exit 1; }

cp "runtime/CMakeLists.txt"  "$LAYOUT/runtime/"
cp -R "runtime/cmake"        "$LAYOUT/runtime/"
cp -R "runtime/core"         "$LAYOUT/runtime/"
cp -R "runtime/dotnetmodule" "$LAYOUT/runtime/"

# runtime/cmake/ is build machinery, not documentation: dn2cpp_embed_bytes.cmake is
# a `cmake -P` script the DN2CPP_USE_CURL arm runs at BUILD time to turn
# third_party/cacert/cacert.pem into dn2cpp_ca_bundle.cpp. Omitting it configures
# perfectly and then dies in ninja ("missing and no known rule to make it"), because
# the configure only records the custom command and never reads the script — so the
# hole is invisible to every check that stops at cmake, and visible only to a build
# with curl on — which, now that DN2CPP_USE_CURL defaults ON, is every build that does
# not deliberately opt out.
#
# The guard is the general form of that lesson: derive the required runtime subtrees
# from the CMakeLists that actually ships. It allows exactly one omission —
# runtime/godot, which the bundle drops deliberately (DN2CPP_GODOT stays OFF for the
# mono-module build, so the bridge is never referenced; see the header).
rt_missing=""
for rt in $(grep -o 'DN2CPP_ROOT}/runtime/[A-Za-z0-9_-]*' "$LAYOUT/runtime/CMakeLists.txt" \
                | sed 's|.*/runtime/||' | sort -u); do
    [ "$rt" = godot ] && continue
    [ -e "$LAYOUT/runtime/$rt" ] || rt_missing="$rt_missing $rt"
done
[ -z "$rt_missing" ] || {
    echo "error: runtime/CMakeLists.txt names runtime subtrees the bundle omits:$rt_missing" >&2
    echo "       add them to the copies above in dist/package-toolchain.sh" >&2
    exit 1; }

# Every vendored tree runtime/CMakeLists.txt reaches for under
# ${DN2CPP_ROOT}/third_party, copied WHOLE. Copying whole is a decision, not
# laziness: the two big ones — curl (6.6 MiB) and Mbed TLS (7.8 MiB), pulled in by
# the DN2CPP_USE_CURL arm — are ALREADY curated subsets, and neither leaves a
# further cut a packaging script could take.
#
#   * curl builds from lib/Makefile.inc, the EXPLICIT source list its own
#     lib/CMakeLists.txt transforms into CSOURCES. Every TLS backend is named
#     there unconditionally (vtls/openssl.c, schannel.c, wolfssl.c, gtls.c,
#     rustls.c) and the unselected ones compile to empty translation units rather
#     than being excluded — so dropping one does not shrink the build, it deletes a
#     file add_library() names. Trimming the list means editing Makefile.inc, which
#     would end the "byte-identical to the 8.21.0 release tarball" property the
#     vendor rests on (third_party/curl/DN2CPP-VENDORED.md, "Modifications: None").
#   * Mbed TLS is globbed as library/*.c, and its vendoring note states why a
#     smaller glob is not on offer: the TLS, X.509 and crypto halves are one
#     dependency chain, so this is the minimal buildable unit. The lever for
#     shrinking it is the MBEDTLS_* feature configuration, which lives inside the
#     vendored tree and applies to the repository build too.
#   * cacert is one 182 KiB .pem plus its provenance note.
#
# The bundle therefore grows by what the repository itself builds from, which is
# the property that matters: the exporter's `cmake -S <bundle>/runtime` and a
# repository build configure over identical trees, so a bundle-only build failure
# cannot be a bundle-only source difference.
BUNDLED_THIRD_PARTY="bdwgc brotli cacert curl highway mbedtls nghttp2 zlib"
for tp in $BUNDLED_THIRD_PARTY; do
    [ -d "third_party/$tp" ] || {
        echo "error: vendored tree third_party/$tp is absent" >&2; exit 1; }
    cp -R "third_party/$tp" "$LAYOUT/third_party/"
done
cp "third_party/gdextension_interface.h" "$LAYOUT/third_party/"

# A tree the bundled CMakeLists names but the bundle does not carry fails LOUDLY
# here. That exact hole has shipped once already: the DN2CPP_USE_CURL arm landed
# naming third_party/{curl,mbedtls,cacert} while this loop still copied four
# trees, and nothing on the packaging side noticed — the miss surfaces only inside
# a forked editor's export, where `add_subdirectory(third_party/curl)` points at a
# directory that is not there and CMake FATAL_ERRORs in a log nobody reads until a
# game fails to export. Derive the required set from the copy of the CMakeLists
# that actually ships, so adding a vendored library to it and forgetting the list
# above cannot get past packaging.
tp_missing=""
for tp in $(grep -o 'third_party/[A-Za-z0-9_][A-Za-z0-9_.-]*' "$LAYOUT/runtime/CMakeLists.txt" \
                | sed 's|^third_party/||' | sort -u); do
    [ -e "$LAYOUT/third_party/$tp" ] || tp_missing="$tp_missing $tp"
done
[ -z "$tp_missing" ] || {
    echo "error: runtime/CMakeLists.txt names third_party trees the bundle omits:$tp_missing" >&2
    echo "       add them to BUNDLED_THIRD_PARTY in dist/package-toolchain.sh" >&2
    exit 1; }
# The CA bundle is one file inside its tree and the directory test above cannot
# see it; DN2CPP_USE_CURL embeds it byte-for-byte into dn2cpp_ca_bundle_pem, and
# an absent .pem makes the embed step fail rather than yield an empty trust store.
[ -f "$LAYOUT/third_party/cacert/cacert.pem" ] || {
    echo "error: bundle third_party/cacert/cacert.pem is missing" >&2; exit 1; }

# The whole managed framework closure (see header): every *.dll next to CoreLib.
cp "$fwdir"/*.dll "$LAYOUT/ref/"
[ -f "$LAYOUT/ref/System.Private.CoreLib.dll" ] || {
    echo "error: framework copy is missing System.Private.CoreLib.dll" >&2; exit 1; }

# The POSIX flavour, for the cross-compiled targets (see header). Staged only
# where the host's own flavour cannot serve them; a POSIX host's `ref/` already
# is this, and copying it twice would just double the bundle.
#
# An absent linux-x64 runtime pack is a hard error rather than a skip: skipping
# produces a bundle that packages, installs and exports desktop games perfectly,
# and fails the first Android or Web export inside a linker. The resolver's own
# message says how to fetch one.
CROSS_CORELIB_REL=""
if [ "$DN2CPP_OS" = windows ]; then
    cross_corelib="$(locate_corelib_cross_posix net10)" || exit 1
    cross_fwdir="$(dirname "$cross_corelib")"
    echo "-- posix cross framework: $cross_fwdir"
    mkdir -p "$LAYOUT/ref-posix"
    cp "$cross_fwdir"/*.dll "$LAYOUT/ref-posix/"
    [ -f "$LAYOUT/ref-posix/System.Private.CoreLib.dll" ] || {
        echo "error: posix framework copy is missing System.Private.CoreLib.dll" >&2; exit 1; }
    CROSS_CORELIB_REL="ref-posix/System.Private.CoreLib.dll"
fi

# emsdk_probe_link EMSCRIPTEN_DIR WORK_DIR SHAPE — link WORK_DIR/probe.cpp through
# the STAGED SDK's own config, in one of the three shapes this SDK is asked for:
# the two runtime/CMakeLists.txt emits on the Emscripten arm (the drop-in SIDE
# module an export ships, and the console main module), plus a MAIN_MODULE, which
# is what a dynamic-linking Web template is and what resolves the `pic/` half of
# the sysroot. Which cache variants a build resolves is decided by the link
# settings, not by the sources, so one bare TU under the real flags reaches the same
# set in seconds. Log: $WORK_DIR/probe-SHAPE.log.
#
# THIS FUNCTION IS THE BAKE RECIPE, and emsdk_bake_recipe below hashes its own
# code into the staging stamp: a shape or a link flag changed here must re-stage
# and re-bake, or a tree filled by the old recipe keeps passing its stamp.
emsdk_probe_link() {
    local ems work="$2" shape="$3"
    # Absolute: the link runs from WORK_DIR, so a relative SDK path is gone.
    ems="$(cd "$1" && pwd)"
    local args=(-std=c++17 -O2 -fvisibility=hidden -fwasm-exceptions)
    local spill="-sBINARYEN_EXTRA_PASSES=--spill-pointers"
    case "$shape" in
        side) args+=(-fPIC -sSIDE_MODULE=2 "-sEXPORTED_FUNCTIONS=_main" "$spill"
                     -o probe-side.so) ;;
        # -g2 keeps the wasm name section alive into wasm-opt, which SpillPointers
        # needs to find __stack_pointer — the same pairing runtime/CMakeLists.txt
        # makes on this target and only this one.
        main) args+=(-g2 -sEXIT_RUNTIME=1 -sALLOW_MEMORY_GROWTH=1 -sSTACK_SIZE=1048576
                     "$spill" -o probe-main.js) ;;
        host) args+=(-sMAIN_MODULE=1 -sALLOW_MEMORY_GROWTH=1 -o probe-host.js) ;;
    esac
    # native_path: the config path lands in emcc's own environment, where MSYS
    # rewrites nothing. A staged python/ outranks any host search — emcc.exe
    # reads EMSDK_PYTHON ahead of the PATH.
    local penv=(EM_CONFIG="$(native_path "$ems/.emscripten")")
    [ -x "$ems/../python/python.exe" ] \
        && penv+=(EMSDK_PYTHON="$(native_path "$ems/../python/python.exe")")
    ( cd "$work" && env "${penv[@]}" "$ems/em++" "${args[@]}" probe.cpp ) \
        > "$work/probe-$shape.log" 2>&1
}

# emsdk_bake_recipe — a short hash of emsdk_probe_link's own code, comments and
# blank lines removed, for the staging stamp. Derived rather than hand-bumped
# because a recipe name is one a human has to remember, and stripped of comments
# so re-wording one does not re-copy 1.4 GB.
emsdk_bake_recipe() {
    declare -f emsdk_probe_link \
        | LC_ALL=C sed -e 's/[[:space:]]*#.*$//' -e '/^[[:space:]]*$/d' \
        | shasum -a 256 | awk '{print substr($1, 1, 12)}'
}

# bundle_trim ROOT SPEC LABEL — reduce a staged tree to the set SPEC keeps,
# reporting under LABEL. For the SDK it runs between the bake and the freeze, so
# the read-only re-link that follows is the oracle for what it removed (see
# SPEC's header for the rest of the contract).
#
# A keep entry that matches nothing is fatal, and that is the whole reason this
# is a keep list: the upstream tree is 1.4 GB of someone else's layout, and a
# rename there must fail HERE rather than ship a bundle whose SDK is quietly
# short a directory — which links until the one export that needs it.
bundle_trim() {
    local root="$1" spec="$2" label="$3" tmp keep all hits pat p pkg tag
    local node_closure=0 node_drops=() trim_py os_tag=posix
    [ "$DN2CPP_OS" = windows ] && os_tag=windows
    tmp="$(mktemp -d)"
    keep="$tmp/keep"; all="$tmp/all"
    : > "$keep"

    while IFS= read -r pat; do
        case "$pat" in
            ''|'#'*) continue ;;
        esac
        # A `windows:`/`posix:` tag scopes the line to that OS's SDK tree: the
        # other OS must not even glob it, or its zero matches would be fatal
        # there. Any other `<word>:` prefix is a typo, not a keep path.
        tag="${pat%% *}"
        if [ "${tag%:}" != "$tag" ]; then
            tag="${tag%:}"
            case "$tag" in
                windows|posix) [ "$tag" = "$os_tag" ] || continue; pat="${pat#* }" ;;
                *) echo "error: $spec tags '$pat' with '$tag:', which is not an OS (windows:/posix:)" >&2
                   exit 1 ;;
            esac
        fi
        case "$pat" in
            'node-closure') node_closure=1; continue ;;
            'node-drop '*) node_drops+=(--drop "${pat#node-drop }"); continue ;;
        esac
        # Globbed INSIDE the root, so an entry is a path in the shipped tree and
        # never one on this machine.
        ( cd "$root" && for p in $pat; do [ -e "$p" ] && printf '%s\n' "$p"; done; true ) > "$tmp/hits"
        hits="$(wc -l < "$tmp/hits" | tr -d ' ')"
        [ "$hits" -gt 0 ] || {
            echo "error: $spec keeps '$pat', which matches nothing in the staged tree" >&2
            echo "       (upstream moved it, or the entry is a typo — either way the bundle" >&2
            echo "        would ship without it and fail an export instead)" >&2
            exit 1; }
        while IFS= read -r p; do
            if [ -d "$root/$p" ] && [ ! -L "$root/$p" ]; then
                ( cd "$root" && find "$p" ! -type d )
            else
                printf '%s\n' "$p"
            fi
        done < "$tmp/hits" >> "$keep"
    done < "$spec"

    # The npm side is COMPUTED, not listed: emcc loads a few packages while
    # linking and node_modules carries emscripten's whole dev chain besides.
    # A dependency added upstream therefore ships without an edit here, and one
    # that is neither reachable nor declined fails the tool rather than this loop.
    if [ "$node_closure" -eq 1 ]; then
        trim_py="$(resolve_python)" || exit 1
        # shellcheck disable=SC2086 — resolve_python may answer `py -3`.
        $trim_py tools/emsdk_node_closure.py ${node_drops[@]+"${node_drops[@]}"} \
            "$root/emscripten" > "$tmp/pkgs.raw" || exit 1
        # A native Windows python writes CRLF; the names are compared as bytes.
        tr -d '\r' < "$tmp/pkgs.raw" > "$tmp/pkgs"
        while IFS= read -r pkg; do
            [ -d "$root/emscripten/node_modules/$pkg" ] || {
                echo "error: emscripten/package.json reaches npm package '$pkg', which node_modules" >&2
                echo "       does not carry as a directory" >&2
                exit 1; }
            ( cd "$root" && find "emscripten/node_modules/$pkg" ! -type d )
        done < "$tmp/pkgs" >> "$keep"
    fi

    LC_ALL=C sort -u "$keep" -o "$keep"
    # A licence rides with its package: kept when the directory holding it still
    # holds a kept file at any depth, dropped with the tree it documented. The
    # names are the ones upstreams actually use — musl's text is a COPYRIGHT and
    # nothing else would reach it.
    ( cd "$root" && find . ! -type d \
        \( -iname 'LICENSE*' -o -iname 'COPYING*' -o -iname 'NOTICE*' \
           -o -iname 'COPYRIGHT*' \) ) \
        | LC_ALL=C sed 's|^\./||' | LC_ALL=C sort -u > "$tmp/lic"
    awk -v licfile="$tmp/lic" '
        { d = $0; while (sub(/\/[^\/]*$/, "", d)) kept[d] = 1 }
        END {
            while ((getline l < licfile) > 0) {
                ld = l
                if (sub(/\/[^\/]*$/, "", ld) && (ld in kept)) print l
            }
        }' "$keep" > "$tmp/lic-kept"
    cat "$tmp/lic-kept" >> "$keep"
    LC_ALL=C sort -u "$keep" -o "$keep"

    ( cd "$root" && find . ! -type d ) | LC_ALL=C sed 's|^\./||' | LC_ALL=C sort -u > "$all"
    ( cd "$root" && LC_ALL=C comm -23 "$all" "$keep" | tr '\n' '\0' | xargs -0 rm -f )
    # `-exec … \;`, not `+`: with -depth a batched rmdir would still be pending
    # when the parent is tested, and every nested directory would survive.
    find "$root" -depth -type d -empty -exec rmdir {} \; 2>/dev/null || true

    # One place a reader finds what the bundle carries and under whose terms.
    mkdir -p "$root/LICENSES"
    while IFS= read -r p; do
        cp "$root/$p" "$root/LICENSES/$(printf '%s' "$p" | tr '/' '_')"
    done < "$tmp/lic-kept"
    # A licence that vanished with its package is fine; one whose package
    # survived and whose file did not is a shipping defect.
    while IFS= read -r p; do
        [ -f "$root/$p" ] || {
            echo "error: the trim dropped $p while keeping the package it documents" >&2
            exit 1; }
    done < "$tmp/lic-kept"
    echo "-- $label: trimmed to $(du -sh "$root" | awk '{print $1}') by $spec"
    rm -rf "$tmp"
}

# 3b. The Emscripten SDK a Web export cross-compiles through. Two things follow
# from it being INSIDE the bundle: a host with no SDK installed can export to the
# Web at all, and the web prebuilt below becomes portable — its key records
# CMAKE_TOOLCHAIN_FILE, which a path under the bundle root is relativized against
# (runtime/cmake/dn2cpp_prebuilt.cmake) while a host SDK's absolute path pins the
# archives to this machine.
#
# A bundle without one is normal, not broken: only a Web export needs it, and the
# fork refuses that one export naming the setting that points at an SDK. So an
# absent default SDK warns and packages on — a NAMED one that is not an SDK is a
# typo and fails.
#
# The source is never the bundle's own copy, which is why dn2cpp_emsdk_resolve
# (whose first answer is that copy) is not what finds it.
EMSDK_VERSION=""
EMSDK_RELEASE_HASH=""
EMCC_VERSION=""
emsdk_host="$(dn2cpp_host_tag)" || emsdk_host=""
emsdk_src="$EMSDK_DIR"
[ -n "$emsdk_src" ] || emsdk_src="${DN2CPP_EMSDK:-}"
emsdk_named=1
if [ -z "$emsdk_src" ]; then
    emsdk_named=0
    [ -n "$emsdk_host" ] && emsdk_src="artifacts/emsdk/$(pin_field "$EMSDK_PIN" version)-$emsdk_host"
fi
if [ -n "$emsdk_src" ] && [ ! -x "$emsdk_src/emscripten/emcc" ]; then
    [ "$emsdk_named" -eq 0 ] || {
        echo "error: not an Emscripten SDK root (no emscripten/emcc): $emsdk_src" >&2
        echo "       name the directory holding bin/ and emscripten/" >&2
        exit 1; }
    emsdk_src=""
fi

if [ -z "$emsdk_src" ]; then
    echo "warning: no Emscripten SDK to bundle (gates/setup-emsdk.sh unpacks the pinned one);" >&2
    echo "         the bundle carries none and a Web export off it needs one on the host" >&2
else
    EMSDK_VERSION="$(LC_ALL=C tr -d '" \r\n' < "$emsdk_src/emscripten/emscripten-version.txt")"
    EMSDK_VERSION="${EMSDK_VERSION%-git}"
    # Keyed on the sha256 of the archive it was unpacked from — the bytes, which
    # is what a re-copy would be reproducing — read from the stamp
    # gates/setup-emsdk.sh writes beside the tree. An SDK from anywhere else has
    # no such stamp and is re-staged every run: the alternative is a stamp
    # claiming an identity nothing recorded.
    emsdk_pin_stamp="$(file_text "$emsdk_src.pin")"
    emsdk_sha=unpinned
    case "$emsdk_pin_stamp" in "$EMSDK_VERSION "*) emsdk_sha="${emsdk_pin_stamp#* }" ;; esac
    # Terms three and four: what the staging DOES — what it keeps, and what it
    # bakes. The version and the archive's sha identify the upstream bytes and
    # cannot see a change here, so a tree staged by an older recipe would
    # otherwise keep passing its own stamp. Both are hashed rather than named, so
    # editing dist/emsdk-trim.txt or emsdk_probe_link re-stages by itself.
    # gates/_common.sh keys the gate result cache on this same stamp.
    EMSDK_TRIM_SPEC=dist/emsdk-trim.txt
    [ -f "$EMSDK_TRIM_SPEC" ] || {
        echo "error: the SDK keep list $EMSDK_TRIM_SPEC is absent" >&2; exit 1; }
    emsdk_stamp="$EMSDK_VERSION $emsdk_sha trim-v1-$(shasum -a 256 "$EMSDK_TRIM_SPEC" \
        | awk '{print substr($1, 1, 12)}') bake-$(emsdk_bake_recipe)"
    emsdk_dest="$LAYOUT/emsdk"

    if [ "$emsdk_sha" != unpinned ] \
        && [ "$(file_text "$emsdk_dest/.emsdk-stamp")" = "$emsdk_stamp" ]; then
        echo "-- emsdk: $EMSDK_VERSION already staged (stamp match); not re-copied"
    else
        echo "-- emsdk: staging $EMSDK_VERSION from $emsdk_src ($(du -sh "$emsdk_src" | awk '{print $1}'))"
        rm -rf "$LAYOUT/.emsdk.part" "$emsdk_dest"
        cp -R "$emsdk_src" "$LAYOUT/.emsdk.part"
        # The BUNDLE's config, generated rather than copied: the source tree's may
        # have been edited, and every path here is relative to $CFGDIR — the
        # config's own directory — so the bundle stays movable. Written WITHOUT
        # FROZEN_CACHE; the bake below needs the cache writable, and the freeze is
        # appended once it is full.
        cat > "$LAYOUT/.emsdk.part/emscripten/.emscripten" <<'EOF'
LLVM_ROOT     = '$CFGDIR/../bin'
BINARYEN_ROOT = '$CFGDIR/..'
CACHE         = '$CFGDIR/cache'
EOF
        # A MOVED SDK must not carry the sanity file of where it came from.
        # emcc's is `version|<absolute LLVM_ROOT>`, so the first emcc at a new
        # path finds it stale and ERASES the whole cache — including the sysroot
        # the archive ships, which is 600 MB of headers and prebuilt system
        # libraries. Absent, that same emcc writes a fresh one and keeps the
        # cache. The freeze below makes check_sanity return before either branch,
        # so this stays as the second lock on the same door.
        rm -f "$LAYOUT/.emsdk.part/emscripten/cache/sanity.txt"
        # Copied bytecode records the SOURCE tree's .py mtimes, which the copy
        # does not keep: stale from birth, so a probe would rewrite it mid-proof.
        # The bake regenerates what it needs against this tree's own mtimes.
        find "$LAYOUT/.emsdk.part" -name __pycache__ -type d -prune -exec rm -rf {} +
        # The Windows front-ends run the staged python with -E, so no environment
        # variable can stop bytecode caching; sitecustomize is the one hook -E
        # leaves open, and without it every link WRITES INTO THE SHIPPED SDK.
        [ -x "$LAYOUT/.emsdk.part/python/python.exe" ] \
            && cat > "$LAYOUT/.emsdk.part/python/Lib/sitecustomize.py" <<'EOF'
# The bundled SDK must never write to itself; emcc's `python -E` ignores
# PYTHONDONTWRITEBYTECODE, so the interpreter is told here instead.
import sys
sys.dont_write_bytecode = True
EOF
        mv "$LAYOUT/.emsdk.part" "$emsdk_dest"

        # Bake, freeze, prove. The shipped SDK must never write to its own cache:
        # an editor installs this bundle where the user cannot write, and the
        # implicit embuilder a first Web export would run dies there naming
        # nothing about the export. So each shape links once with the cache still
        # writable — which fills whatever the release archive does not carry, and
        # stays exact where a hand-listed embuilder target set would decay — and
        # then again read-only, which is the assertion. Fatal rather than a
        # warning: freezing a cache nothing filled moves the failure onto the
        # consumer's machine.
        emsdk_bake="$LAYOUT/.emsdk-bake"
        rm -rf "$emsdk_bake"
        mkdir -p "$emsdk_bake"
        cat > "$emsdk_bake/probe.cpp" <<'EOF'
#include <cstdio>
#include <stdexcept>
#include <string>
#include <vector>
int main() {
    try { std::vector<std::string> v; v.at(0); }
    catch (const std::exception& e) { std::printf("%s\n", e.what()); }
    return 0;
}
EOF
        for emsdk_shape in side main host; do
            emsdk_probe_link "$emsdk_dest/emscripten" "$emsdk_bake" "$emsdk_shape" || {
                echo "error: the staged emsdk could not link the $emsdk_shape shape; the cache cannot be" >&2
                echo "       baked and the SDK must not ship frozen (see $emsdk_bake/probe-$emsdk_shape.log)" >&2
                exit 1; }
        done
        # Trim before the freeze, never after: the keep list is what the
        # read-only re-link below is asserting, and a trim that ran after it
        # would be a change nothing proved.
        bundle_trim "$emsdk_dest" "$EMSDK_TRIM_SPEC" emsdk
        printf 'FROZEN_CACHE  = True\n' >> "$emsdk_dest/emscripten/.emscripten"

        touch "$emsdk_bake/frozen-at"
        chmod -R a-w "$emsdk_dest"
        gate_add_exit_hook "chmod -R u+w '$emsdk_dest' 2>/dev/null"
        for emsdk_shape in side main host; do
            emsdk_probe_link "$emsdk_dest/emscripten" "$emsdk_bake" "$emsdk_shape" || {
                echo "error: the frozen emsdk could not link the $emsdk_shape shape read-only — its cache" >&2
                echo "       is short a variant the Web lane resolves (see $emsdk_bake/probe-$emsdk_shape.log)" >&2
                exit 1; }
        done
        emsdk_written="$(find "$emsdk_dest" -newer "$emsdk_bake/frozen-at" -print -quit)"
        chmod -R u+w "$emsdk_dest"
        if [ -n "$emsdk_written" ]; then
            echo "error: the frozen emsdk was written to by its own link: $emsdk_written" >&2
            exit 1
        fi
        rm -rf "$emsdk_bake"
        echo "-- emsdk: cache baked and frozen ($(du -sh "$emsdk_dest/emscripten/cache" | awk '{print $1}'))"
    fi

    # Run the STAGED emcc, not the source's: it is the manifest's emcc_version and
    # the one proof that the config written above drives the SDK where it landed.
    # A failure is not fatal — the SDK is staged either way — but it is said.
    emsdk_ver_env=(EM_CONFIG="$(native_path "$(cd "$emsdk_dest/emscripten" && pwd)/.emscripten")")
    [ -x "$emsdk_dest/python/python.exe" ] \
        && emsdk_ver_env+=(EMSDK_PYTHON="$(native_path "$(cd "$emsdk_dest/python" && pwd)/python.exe")")
    EMCC_VERSION="$(env "${emsdk_ver_env[@]}" \
        "$emsdk_dest/emscripten/emcc" --version 2>/dev/null)" || EMCC_VERSION=""
    EMCC_VERSION="$(first_line "$EMCC_VERSION")"
    [ -n "$EMCC_VERSION" ] \
        || echo "warning: the staged emsdk's emcc did not answer --version (no working python 3 — EMSDK_PYTHON, bundled python/, or PATH?)" >&2

    # Provenance, beside the SDK rather than in the manifest: it describes the
    # tree it sits in, and the manifest's content hash deliberately does not cover
    # that tree (the upstream sha256 is what fixes those bytes).
    emsdk_url=""
    emsdk_archive_sha=""
    if [ "$EMSDK_VERSION" = "$(pin_field "$EMSDK_PIN" version)" ] && [ -n "$emsdk_host" ]; then
        read -r emsdk_arch_path emsdk_archive_sha <<<"$(awk -v h="$emsdk_host" \
            '$1 == "archive" && $2 == h { p = $3; s = $4 } END { print p, s }' \
            gates/expected/emsdk-pin.txt)"
        EMSDK_RELEASE_HASH="$(pin_field "$EMSDK_PIN" release_hash)"
        emsdk_url="$(pin_field "$EMSDK_PIN" base_url)/${emsdk_arch_path//%h/$EMSDK_RELEASE_HASH}"
    fi
    # A Windows SDK is two pinned archives: the portable CPython staged as
    # python/ has its own provenance row, carried only when the tree carries it.
    emsdk_python_json=""
    if [ -x "$emsdk_dest/python/python.exe" ] && [ -n "$emsdk_host" ]; then
        read -r emsdk_py_path emsdk_py_sha <<<"$(awk -v h="$emsdk_host" \
            '$1 == "python" && $2 == h { p = $3; s = $4 } END { print p, s }' \
            gates/expected/emsdk-pin.txt)"
        [ -n "$emsdk_py_path" ] && emsdk_python_json="$(printf '\n  "python_archive_url": "%s",\n  "python_archive_sha256": "%s",' \
            "$(pin_field "$EMSDK_PIN" base_url)/$emsdk_py_path" "$emsdk_py_sha")"
    fi
    cat > "$emsdk_dest/emsdk.json" <<EOF
{
  "version": "$EMSDK_VERSION",
  "release_hash": "$EMSDK_RELEASE_HASH",
  "archive_url": "$emsdk_url",
  "archive_sha256": "$emsdk_archive_sha",$emsdk_python_json
  "emcc_version": "$EMCC_VERSION"
}
EOF
    # Written LAST, so a run interrupted anywhere above leaves a tree that
    # re-stages rather than one that passes for complete.
    printf '%s\n' "$emsdk_stamp" > "$emsdk_dest/.emsdk-stamp"
    echo "-- emsdk: $emsdk_dest ($(du -sh "$emsdk_dest" | awk '{print $1}')), ${EMCC_VERSION:-emcc version unknown}"
fi

# buildtools_thin ROOT — reduce every Mach-O under ROOT to its arm64 slice.
# cmake's macOS archive is universal and dist/package-editor-macos.sh refuses a
# non-arm64 host, so the x86_64 half is code the editor can never execute — and
# it is half of what the bundle would cost. A fat file with no arm64 slice is
# the wrong archive, not something to thin around. lipo's output takes default
# permissions, so the mode is carried across by hand.
buildtools_thin() {
    local root="$1" f archs mode
    while IFS= read -r f; do
        archs="$(lipo -archs "$f" 2>/dev/null)" || continue   # not a Mach-O
        case " $archs " in
            *" arm64 "*) ;;
            *) echo "error: $f has no arm64 slice (it is '$archs')" >&2; return 1 ;;
        esac
        [ "$archs" = arm64 ] && continue
        mode="$(stat -f '%Lp' "$f")"
        lipo -thin arm64 "$f" -output "$f.arm64" || return 1
        mv "$f.arm64" "$f"
        chmod "$mode" "$f"
    done < <(find "$root" -type f)
}

# 3b′. The cmake and ninja an export configures and compiles through, so a user's
# machine needs neither — and so the version is this repository's choice rather
# than whatever is on PATH, which matters: a cmake in a forbidden band builds the
# Web drop-in as a static archive (gates/expected/buildtools-pin.txt).
#
# Staged BEFORE 3c, which builds every prebuilt axis with this cmake.
#
# A bundle without them is degraded, not broken — the packaging must work on a
# host that never ran the setup aid — so an absent default tree warns and
# packages on, while a NAMED directory that is not a buildtools layout is a typo
# and fails.
BT_CMAKE_VERSION="$(pin_field "$BUILDTOOLS_PIN" cmake_version)"
BT_NINJA_VERSION="$(pin_field "$BUILDTOOLS_PIN" ninja_version)"
bt_host="$(dn2cpp_host_tag)" || bt_host=""
bt_src="$BUILDTOOLS_DIR"
[ -n "$bt_src" ] || bt_src="${DN2CPP_BUILDTOOLS:-}"
bt_named=1
if [ -z "$bt_src" ]; then
    bt_named=0
    [ -n "$bt_host" ] && bt_src="artifacts/buildtools/$BT_CMAKE_VERSION-$BT_NINJA_VERSION-$bt_host"
fi
# The source tree already IS a buildtools/ layout, so bundled_cmake — which
# appends that component to a bundle ROOT — cannot name it.
if [ -n "$bt_src" ] && [ ! -x "$bt_src/cmake/bin/cmake$EXE_EXT" ]; then
    [ "$bt_named" -eq 0 ] || {
        echo "error: not a buildtools tree (no cmake/bin/cmake$EXE_EXT): $bt_src" >&2
        echo "       name the directory holding cmake/ and ninja/" >&2
        exit 1; }
    bt_src=""
fi

if [ -z "$bt_src" ]; then
    echo "warning: no cmake/ninja to bundle (gates/setup-buildtools.sh unpacks the pinned pair);" >&2
    echo "         the bundle carries none and an export off it needs both on the host" >&2
else
    BUILDTOOLS_TRIM_SPEC=dist/buildtools-trim.txt
    [ -f "$BUILDTOOLS_TRIM_SPEC" ] || {
        echo "error: the keep list $BUILDTOOLS_TRIM_SPEC is absent" >&2; exit 1; }
    # gates/setup-buildtools.sh's stamp, beside the tree: the two versions and the
    # sha256 of the two archives they were unpacked from — the bytes a re-copy
    # would be reproducing. A tree from anywhere else has no such stamp and is
    # re-staged every run; the alternative is a stamp claiming an identity nothing
    # recorded.
    bt_cmake_sha=unpinned
    bt_ninja_sha=unpinned
    read -r bt_p1 bt_p2 bt_p3 bt_p4 <<<"$(file_text "$bt_src.pin")"
    if [ "$bt_p1" = "$BT_CMAKE_VERSION" ] && [ "$bt_p3" = "$BT_NINJA_VERSION" ]; then
        bt_cmake_sha="$bt_p2"
        bt_ninja_sha="$bt_p4"
    fi
    # Four terms, the SDK's shape: the versions and the archive shas identify the
    # upstream bytes and cannot see a change to the keep list, so editing that
    # must re-stage by itself. No bake term — nothing here is baked.
    bt_stamp="$BT_CMAKE_VERSION $bt_cmake_sha $BT_NINJA_VERSION $bt_ninja_sha trim-v1-$(shasum -a 256 \
        "$BUILDTOOLS_TRIM_SPEC" | awk '{print substr($1, 1, 12)}')"
    bt_dest="$LAYOUT/buildtools"

    if [ "$bt_cmake_sha" != unpinned ] \
        && [ "$(file_text "$bt_dest/.buildtools-stamp")" = "$bt_stamp" ]; then
        echo "-- buildtools: cmake $BT_CMAKE_VERSION + ninja $BT_NINJA_VERSION already staged (stamp match); not re-copied"
    else
        echo "-- buildtools: staging from $bt_src ($(du -sh "$bt_src" | awk '{print $1}'))"
        rm -rf "$LAYOUT/.buildtools.part" "$bt_dest"
        cp -R "$bt_src" "$LAYOUT/.buildtools.part"
        mv "$LAYOUT/.buildtools.part" "$bt_dest"

        # Thin, then trim, then prove — in that order, because the proof is the
        # oracle for what the other two removed.
        if [ "$DN2CPP_OS" = macos ]; then buildtools_thin "$bt_dest" || exit 1; fi
        bundle_trim "$bt_dest" "$BUILDTOOLS_TRIM_SPEC" buildtools

        # Run the STAGED pair. A binary that will not exec — thinned past its
        # signature, quarantined, mis-copied — dies here rather than in a user's
        # export log, and the version is asserted against the pin so a bundle
        # cannot carry a cmake the exporter would refuse.
        bt_cmake="$(bundled_cmake "$LAYOUT")"
        bt_ninja="$(bundled_ninja "$LAYOUT")"
        bt_got="$("$bt_cmake" --version | awk 'NR == 1 { print $3 }')"
        [ "$bt_got" = "$BT_CMAKE_VERSION" ] || {
            echo "error: the staged cmake reports '$bt_got', pinned $BT_CMAKE_VERSION" >&2; exit 1; }
        bt_got="$("$bt_ninja" --version)"
        [ "$bt_got" = "$BT_NINJA_VERSION" ] || {
            echo "error: the staged ninja reports '$bt_got', pinned $BT_NINJA_VERSION" >&2; exit 1; }
        # A cmake with no module tree still prints its version and passes the
        # assert above — `Could not find CMAKE_ROOT !!!` goes to stderr and the
        # exit status is 0 — so only a structural check sees a trim that dropped
        # it. This is the file cmake itself probes to accept a CMAKE_ROOT.
        bt_modules="$(echo "$bt_dest"/cmake/share/cmake-*/Modules/CMakeSystemSpecificInformation.cmake)"
        [ -f "$bt_modules" ] || {
            echo "error: the trimmed cmake has no share/cmake-*/Modules/CMakeSystemSpecificInformation.cmake," >&2
            echo "       so it would refuse its own CMAKE_ROOT (see $BUILDTOOLS_TRIM_SPEC)" >&2; exit 1; }
        bt_thinned=false
        if [ "$DN2CPP_OS" = macos ]; then
            bt_thinned=true
            # The assertion the thinning is worth: a Mach-O the walk above never
            # reached would ship its x86_64 half and nothing would say so.
            while IFS= read -r bt_f; do
                bt_archs="$(lipo -archs "$bt_f" 2>/dev/null)" || continue
                [ "$bt_archs" = arm64 ] || {
                    echo "error: the staged $bt_f is '$bt_archs', not arm64" >&2; exit 1; }
            done < <(find "$bt_dest" -type f)
        fi

        # Provenance beside the tree, as emsdk.json is: it describes the tree it
        # sits in, and both archives' bytes are already fixed by their sha256.
        read -r bt_cmake_url bt_cmake_asha <<<"$(awk -v h="$bt_host" \
            '$1 == "archive" && $2 == "cmake" && $3 == h { u = $4; s = $5 } END { print u, s }' \
            "$BUILDTOOLS_PIN")"
        read -r bt_ninja_url bt_ninja_asha <<<"$(awk -v h="$bt_host" \
            '$1 == "archive" && $2 == "ninja" && $3 == h { u = $4; s = $5 } END { print u, s }' \
            "$BUILDTOOLS_PIN")"
        cat > "$bt_dest/buildtools.json" <<EOF
{
  "cmake_version": "$BT_CMAKE_VERSION",
  "cmake_archive_url": "$bt_cmake_url",
  "cmake_archive_sha256": "$bt_cmake_asha",
  "ninja_version": "$BT_NINJA_VERSION",
  "ninja_archive_url": "$bt_ninja_url",
  "ninja_archive_sha256": "$bt_ninja_asha",
  "thinned": $bt_thinned
}
EOF
        # ninja's release zip holds the executable and nothing else, so the only
        # copy of its licence is the vendored one (dist/licenses/README.md).
        # cmake's half is already in LICENSES/, from the keep list's own entries.
        mkdir -p "$bt_dest/LICENSES"
        install -m 0644 dist/licenses/ninja-COPYING.txt "$bt_dest/LICENSES/"
        # Written LAST, so a run interrupted anywhere above leaves a tree that
        # re-stages rather than one that passes for complete.
        printf '%s\n' "$bt_stamp" > "$bt_dest/.buildtools-stamp"
        echo "-- buildtools: $bt_dest ($(du -sh "$bt_dest" | awk '{print $1}')), cmake $BT_CMAKE_VERSION + ninja $BT_NINJA_VERSION"
    fi

    # Read-only for the duration of 3c, which is the run that would prove
    # otherwise: an editor installs this bundle where the user cannot write, so a
    # cmake writing into its own CMAKE_ROOT would fail there and nowhere here.
    # Restored and asserted below 3c.
    bt_frozen_at="$OUT_PARENT/.buildtools-frozen-at"
    : > "$bt_frozen_at"
    chmod -R a-w "$bt_dest"
    gate_add_exit_hook "chmod -R u+w '$bt_dest' 2>/dev/null"
fi

# 3c. Prebuilt runtime — the archives an export imports instead of configuring
# and compiling the runtime and its seven vendored trees. The saving is almost
# entirely the vendored curl's own configure (docs/EDITOR-EXPORT-DESIGN.md §4).
#
# ONE DIRECTORY PER AXIS. The key names the toolchain, so a cross-compiled export
# refuses the host's archives by construction. Staged: the host always, the three
# iOS slots on a macOS host with both SDKs, and Android and Web wherever the
# packaging host has their cross toolchain — a missing NDK or emsdk drops that
# axis alone and says so. The axis directory's name is documentation; the consumer
# selects by reading the keys.
#
# Built from the STAGED layout, not from the repository: what the export
# configures is `<bundle>/runtime` over `<bundle>/third_party`, so those are the
# trees whose include paths must end up in the exported file. Option set is the
# exporter's exactly (`-DDN2CPP_DOTNET_MODULE=ON` plus the target's own vars,
# defaults elsewhere) — anything else and the key below refuses its own archives.
#
# A failure on the HOST axis stages nothing and warns: an export with no prebuilt
# builds from source, so degrading costs time and nothing else.
# `dist/smoke-test.sh` is what fails when the prebuilt silently stops shipping —
# the degrade must not be able to become permanent unnoticed. A failure on a CROSS
# axis drops that axis alone.
if [ "$WITH_PREBUILT" -eq 1 ]; then
    echo "== prebuilt runtime =="
    # Pairs the archives with the sources beside them. Never re-derived at
    # consume time (hashing the vendored trees on every configure is not free);
    # what it catches is a prebuilt/ that outlived its runtime/, which is exactly
    # what --install-into leaves behind — it overwrites without deleting.
    pb_stamp="$(cd "$LAYOUT" && find runtime third_party -type f -exec shasum -a 256 {} + \
        | LC_ALL=C sort | shasum -a 256 | awk '{print $1}')"
    pb_build="$(cd "$OUT_PARENT" && pwd)/.prebuilt-build"
    layout_abs="$(cd "$LAYOUT" && pwd)"
    rm -rf "$pb_build" "$LAYOUT/prebuilt"

    # pb_cmake_for ROOT / pb_make_arg_for ROOT — the build tools a configure of
    # the bundle at ROOT runs: ITS OWN when it carries them, because the key
    # records values cmake DERIVES (compiler id and version, the OSX sysroot) and
    # two cmakes may spell one of them differently — a bundle refusing its own
    # archives. The tool itself is deliberately not in the key
    # (runtime/cmake/dn2cpp_prebuilt.cmake); one tool on both sides is the
    # invariant instead. A bundle carrying none falls back to the host's.
    pb_cmake_for() {
        local c
        c="$(bundled_cmake "$1")"
        if [ -x "$c" ]; then printf '%s\n' "$c"; else printf '%s\n' "$CMAKE"; fi
    }
    # Typed :FILEPATH, not bare: an untyped -D lands in the cache as
    # CMAKE_MAKE_PROGRAM:UNINITIALIZED, which the fork's ResetStaleBuildCache
    # reads back. Native spelling — it is written into a cache file.
    pb_make_arg_for() {
        local n
        n="$(bundled_ninja "$1")"
        [ -x "$n" ] || return 0
        printf -- '-DCMAKE_MAKE_PROGRAM:FILEPATH=%s\n' "$(native_path "$n")"
    }
    echo "-- prebuilt runtime: configured through $(pb_cmake_for "$layout_abs")"

    # pb_stage_axis NAME LAUNCHER [EXTRA_CMAKE_ARG...] — build the staged layout's
    # runtime for one axis into prebuilt/NAME/. LAUNCHER is `-` for a plain cmake,
    # else a wrapper taking cmake as its first argument (emcmake). Only the
    # CONFIGURE is wrapped, as Dn2CppExporter.BuildDropIn wraps only that one: the
    # cache it writes carries the toolchain into every later build. Non-zero,
    # having staged nothing for that axis, when its configure or its compile fails;
    # its checks on the emitted text are hard errors instead, because they mean
    # the packaging is wrong rather than the target unavailable.
    #
    # pb_imported_locations FILE — the archive paths FILE names, config suffix
    # unread. runtime/CMakeLists.txt forces a build type under MSVC, so the host
    # axis exports IMPORTED_LOCATION_RELEASE while the cross arms leave the type
    # unset and get NOCONFIG; spelling one config reads NOTHING on the other axes
    # and reports success. Both askers below go through here — an extraction and a
    # check that disagree are a check over an empty set.
    pb_imported_locations() {
        sed -n 's/.*IMPORTED_LOCATION[A-Za-z0-9_]* "\([^"]*\)".*/\1/p' "$1"
    }
    # pb_launcher_path ROOT LAUNCHER — a launcher INSIDE the bundle is written
    # `@<path under the root>` and resolved against the root it is being run for,
    # so the relocated verify runs the COPY's emcmake. Running the original's
    # would hand cmake a toolchain file outside the copy, and the key would name
    # an absolute path — proving the opposite of what the verify is for.
    pb_launcher_path() {
        case "$2" in
            @*) printf '%s\n' "$1/${2#@}" ;;
            *)  printf '%s\n' "$2" ;;
        esac
    }
    pb_stage_axis() {
        local name="$1" launcher="$2"; shift 2
        local bdir="$pb_build/$name" out="$LAYOUT/prebuilt/$name" p rel staged
        local nlib=0 nchecked=0
        # The spelling CMake WRITES into the export, which on Windows is not the
        # MSYS spelling this shell holds. Everything below acts on that file's own
        # text — a prefix strip, a rewrite, a search — so all of it takes this form
        # or each silently becomes a no-op that leaves the machine's paths in place.
        local bdir_n layout_n
        bdir_n="$(native_path "$bdir")"
        layout_n="$(native_path "$layout_abs")"
        local args=(-S "$layout_abs/runtime" -B "$bdir" -G Ninja -DDN2CPP_DOTNET_MODULE=ON
                    "-DDN2CPP_EMIT_PREBUILT_KEY=$bdir/key.txt" "$@")
        local cm mk
        cm="$(pb_cmake_for "$layout_abs")"
        mk="$(pb_make_arg_for "$layout_abs")"
        [ -z "$mk" ] || args+=("$mk")
        local run=("$cm")
        [ "$launcher" = - ] || run=("$(pb_launcher_path "$layout_abs" "$launcher")" "$cm")
        if [ -z "${DN2CPP_NO_CCACHE:-}" ] && command -v ccache >/dev/null 2>&1; then
            args+=(-DCMAKE_C_COMPILER_LAUNCHER=ccache -DCMAKE_CXX_COMPILER_LAUNCHER=ccache)
        fi
        rm -rf "$out"
        "${run[@]}" "${args[@]}" > "$OUT_PARENT/prebuilt-$name-configure.log" 2>&1 || return 1
        "$cm" --build "$bdir" > "$OUT_PARENT/prebuilt-$name-build.log" 2>&1 || return 1
        mkdir -p "$out/lib"
        # The archive set is DERIVED from the export file rather than globbed:
        # what has to be staged is exactly what the file names, and a glob would
        # ship whatever the build dir happens to hold while still missing a path
        # the file spells.
        while IFS= read -r p; do
            rel="${p#"$bdir_n"/}"
            mkdir -p "$out/lib/$(dirname "$rel")"
            cp "$p" "$out/lib/$rel" || { echo "error: staging $p failed" >&2; exit 1; }
            nlib=$((nlib + 1))
        done < <(pb_imported_locations "$bdir/dn2cpp-targets.cmake")
        # Reading no row is the extraction having stopped matching the file, never
        # an axis with nothing to ship: an empty lib/ configures perfectly and dies
        # in the consumer's linker, and the checks below all pass over it.
        [ "$nlib" -gt 0 ] || {
            echo "error: the $name prebuilt export names no archive at all" >&2
            exit 1; }
        # export() writes a BUILD-TREE export: every path in it is absolute and
        # belongs to this machine. Re-anchor both families on the shipped file's
        # own location — the archives under prebuilt/<axis>/lib/, everything else
        # (the INTERFACE include dirs) under the bundle root, two levels up now
        # that the file sits in an axis directory — so the bundle is relocatable,
        # which is the whole premise of shipping one.
        {
            echo "# GENERATED by dist/package-toolchain.sh — do not edit."
            echo '# A build-tree export() is absolute; these three lines re-anchor it.'
            echo 'get_filename_component(_dn2cpp_prebuilt_dir "${CMAKE_CURRENT_LIST_DIR}" ABSOLUTE)'
            echo 'get_filename_component(_dn2cpp_root "${_dn2cpp_prebuilt_dir}/../.." ABSOLUTE)'
            echo 'set(_dn2cpp_lib "${_dn2cpp_prebuilt_dir}/lib")'
            sed -e "s|$bdir_n|\${_dn2cpp_lib}|g" -e "s|$layout_n|\${_dn2cpp_root}|g" \
                "$bdir/dn2cpp-targets.cmake"
        } > "$out/dn2cpp-targets.cmake"
        { cat "$bdir/key.txt"; printf 'SOURCE_STAMP=%s\n' "$pb_stamp"; } > "$out/key.txt"

        # Two checks on the TEXT, because that text is all the consumer's cmake
        # gets. A path this machine still owns would resolve on this machine and
        # nowhere else — the failure a relocatable bundle exists to prevent, and
        # one no run here would otherwise see. Searched in BOTH spellings: matching
        # only the one the rewrite used proves the rewrite ran, not that it was
        # aimed at what CMake wrote.
        if grep -qF -e "$bdir" -e "$layout_abs" -e "$bdir_n" -e "$layout_n" \
                "$out/dn2cpp-targets.cmake"; then
            echo "error: the $name prebuilt export still names this machine's paths after the rewrite" >&2
            exit 1
        fi
        # CMake does not check an IMPORTED_LOCATION until the link, so an archive
        # the rewrite mis-spelled would configure cleanly and die in the linker.
        while IFS= read -r p; do
            staged="$out/lib/${p#\$\{_dn2cpp_lib\}/}"
            [ -f "$staged" ] || {
                echo "error: the $name prebuilt export names an archive the bundle does not carry: $p" >&2
                exit 1; }
            nchecked=$((nchecked + 1))
        done < <(pb_imported_locations "$out/dn2cpp-targets.cmake")
        # The rewrite preserves the rows, so a check that saw fewer than the staging
        # wrote passed over an archive rather than clearing it.
        [ "$nchecked" -eq "$nlib" ] || {
            echo "error: the $name prebuilt export check saw $nchecked of $nlib archives" >&2
            exit 1; }
        return 0
    }

    # The axis table. Every row is NAME, LAUNCHER, then the extra cmake args, held
    # newline-joined so a row may carry a PATH: the NDK and the emsdk are named by
    # the environment, and a directory with a space in it would otherwise split
    # into two flags (`pb_axis_argv` splits on newlines alone).
    #
    # Each row's args must be spelled EXACTLY as Dn2CppExporter.BuildDropIn spells
    # them, deployment target and API level included — the two repositories cannot
    # share a constant, and a drift is silent AND fail-safe (the key stops
    # matching, the export builds from source), which is why the three
    # gates/build-and-run-godot-editor-export-{ios,android,web}.sh assert the
    # adoption.
    pb_add_axis() {   # pb_add_axis NAME LAUNCHER [EXTRA_CMAKE_ARG...]
        local row="$1" a
        shift
        for a in "$@"; do row="$row"$'\n'"$a"; done
        pb_specs+=("$row")
    }
    pb_axis_argv() {  # pb_axis_argv ROW — the row's fields, into pb_argv
        local f
        pb_argv=()
        while IFS= read -r f; do pb_argv+=("$f"); done <<<"$1"
    }

    IOS_DEPLOYMENT_TARGET=16.3
    ANDROID_ABI_PB=arm64-v8a
    ANDROID_PLATFORM_PB=android-24
    pb_specs=()
    pb_add_axis host -
    if [ "$HOST_OS" = macos ]; then
        if xcrun --sdk iphoneos --show-sdk-path >/dev/null 2>&1 \
           && xcrun --sdk iphonesimulator --show-sdk-path >/dev/null 2>&1; then
            pb_ios=(-DCMAKE_SYSTEM_NAME=iOS "-DCMAKE_OSX_DEPLOYMENT_TARGET=$IOS_DEPLOYMENT_TARGET")
            pb_add_axis ios-arm64 - "${pb_ios[@]}" \
                -DCMAKE_OSX_SYSROOT=iphoneos -DCMAKE_OSX_ARCHITECTURES=arm64
            pb_add_axis iossimulator-arm64 - "${pb_ios[@]}" \
                -DCMAKE_OSX_SYSROOT=iphonesimulator -DCMAKE_OSX_ARCHITECTURES=arm64
            pb_add_axis iossimulator-x64 - "${pb_ios[@]}" \
                -DCMAKE_OSX_SYSROOT=iphonesimulator -DCMAKE_OSX_ARCHITECTURES=x86_64
        else
            echo "-- prebuilt runtime: no iOS SDK (xcrun); the three iOS axes are not staged"
        fi
    fi
    # Android. Resolved as Dn2CppExporter.ResolveAndroidNdk resolves it — the two
    # environment variables first, then the newest NDK under the SDK — and tested
    # by the toolchain file cmake is actually handed, not by the directory. An
    # editor resolving a DIFFERENT NDK is not a hazard: the NDK path is in the key,
    # so that export refuses these archives and builds from source.
    pb_ndk=""
    for pb_cand in "${ANDROID_NDK_ROOT:-}" "${ANDROID_NDK_HOME:-}" \
                   $(ls -1d "${ANDROID_HOME:-${ANDROID_SDK_ROOT:-/nonexistent}}"/ndk/* 2>/dev/null | LC_ALL=C sort -r); do
        if [ -n "$pb_cand" ] && [ -f "$pb_cand/build/cmake/android.toolchain.cmake" ]; then
            pb_ndk="$pb_cand"
            break
        fi
    done
    if [ -n "$pb_ndk" ]; then
        pb_add_axis "android-$ANDROID_ABI_PB" - \
            "-DCMAKE_TOOLCHAIN_FILE=$pb_ndk/build/cmake/android.toolchain.cmake" \
            "-DANDROID_ABI=$ANDROID_ABI_PB" "-DANDROID_PLATFORM=$ANDROID_PLATFORM_PB"
    else
        echo "-- prebuilt runtime: no Android NDK (ANDROID_NDK_ROOT/ANDROID_NDK_HOME or the SDK's ndk/); the android axis is not staged"
    fi
    # Web. Built through the SDK 3b staged, so the toolchain file the key records
    # is one inside the bundle and every consumer of these archives matches it;
    # building through the source SDK instead would key them to this machine. With
    # no bundled SDK the fallback is the order dn2cpp_emsdk_resolve serves the
    # gates, ending at whatever is on PATH. em++ is probed beside emcmake for the
    # reason Dn2CppExporter.ResolveEmscripten probes it: emcmake is a thin wrapper
    # that would hand cmake a toolchain file naming a compiler that is not there.
    if [ -d "$LAYOUT/emsdk" ]; then
        DN2CPP_EMSDK="$layout_abs/emsdk" dn2cpp_emsdk_resolve
    else
        dn2cpp_emsdk_resolve
    fi
    if command -v emcmake >/dev/null 2>&1 && command -v em++ >/dev/null 2>&1; then
        if [ -d "$LAYOUT/emsdk" ]; then
            pb_add_axis web-wasm32 "@emsdk/emscripten/emcmake"
        else
            pb_add_axis web-wasm32 "$(command -v emcmake)"
        fi
    else
        echo "-- prebuilt runtime: no Emscripten SDK (emcmake + em++ on PATH); the web axis is not staged"
    fi

    pb_staged=()
    for spec in "${pb_specs[@]}"; do
        pb_axis_argv "$spec"
        pb_name="${pb_argv[0]}"
        pb_ok=1
        pb_stage_axis "${pb_argv[@]}" || pb_ok=0
        if [ "$pb_ok" -eq 1 ]; then
            pb_staged+=("$pb_name")
        elif [ "$pb_name" = host ]; then
            echo "warning: the prebuilt runtime did not build; the bundle ships none and every" >&2
            echo "         export will configure and compile the runtime itself (see" >&2
            echo "         $OUT_PARENT/prebuilt-host-{configure,build}.log)" >&2
            rm -rf "$LAYOUT/prebuilt"
            break
        else
            echo "warning: the $pb_name prebuilt runtime did not build; that export target" >&2
            echo "         will configure and compile the runtime itself (see" >&2
            echo "         $OUT_PARENT/prebuilt-$pb_name-{configure,build}.log)" >&2
        fi
    done

    if [ "${#pb_staged[@]}" -gt 0 ]; then
        printf '%s\n' "$pb_stamp" > "$LAYOUT/runtime/dn2cpp-source-stamp.txt"
        # And one check per axis that the rewrite WORKS, from a copy at another
        # path: the key is compared against a configure's own toolchain, so this
        # proves the move, the key and the stamp together — and a host-only verify
        # would prove none of the three that are cross-compiled. Configure only —
        # the link is dist/smoke-test.sh's job, over a real transpile.
        pb_verify="$(cd "$OUT_PARENT" && pwd)/.prebuilt-verify"
        rm -rf "$pb_verify"
        mkdir -p "$pb_verify/bundle"
        cp -R "$LAYOUT/." "$pb_verify/bundle/"
        # A copy of the read-only build tools cannot be deleted afterwards — the
        # entries are unlinked through their directories. This tree is scratch;
        # the shipped one's read-only proof is the check below 3c.
        chmod -R u+w "$pb_verify/bundle"
        # The copy's own SDK, config included: EM_CONFIG still names the original
        # bundle's, and a verify reading that one leaves the copy's untested.
        pb_em_config="${EM_CONFIG:-}"
        [ -f "$pb_verify/bundle/emsdk/emscripten/.emscripten" ] \
            && export EM_CONFIG="$(native_path "$pb_verify/bundle/emsdk/emscripten/.emscripten")"
        for spec in "${pb_specs[@]}"; do
            pb_axis_argv "$spec"
            pb_name="${pb_argv[0]}"
            case " ${pb_staged[*]} " in *" $pb_name "*) ;; *) continue ;; esac
            # The COPY's cmake and ninja, for the reason pb_launcher_path already
            # resolves emcmake against the copy: running the original's would
            # leave the copy's untested and prove less than this section claims.
            pb_cm="$(pb_cmake_for "$pb_verify/bundle")"
            pb_mk="$(pb_make_arg_for "$pb_verify/bundle")"
            pb_run=("$pb_cm")
            [ "${pb_argv[1]}" = - ] \
                || pb_run=("$(pb_launcher_path "$pb_verify/bundle" "${pb_argv[1]}")" "$pb_cm")
            pb_log="$OUT_PARENT/prebuilt-$pb_name-verify.log"
            if ! "${pb_run[@]}" -S "$pb_verify/bundle/runtime" -B "$pb_verify/build-$pb_name" -G Ninja \
                    -DDN2CPP_DOTNET_MODULE=ON ${pb_mk:+"$pb_mk"} "${pb_argv[@]:2}" > "$pb_log" 2>&1 \
               || ! grep -qE "using the prebuilt runtime at .*/prebuilt/${pb_name}\$" "$pb_log"; then
                echo "error: a relocated copy of the bundle did not adopt its $pb_name prebuilt runtime" >&2
                echo "       (see $pb_log)" >&2
                exit 1
            fi
        done
        # Said, not merely done: the verify tree is deleted on the next line, so
        # this is the only record that the copy's own cmake ran it.
        echo "-- prebuilt runtime: verified through $(pb_cmake_for "$pb_verify/bundle")"
        if [ -n "$pb_em_config" ]; then export EM_CONFIG="$pb_em_config"; else unset EM_CONFIG; fi
        rm -rf "$pb_verify"
        # Both static-library suffixes: the MSVC host axis ships .lib, every other
        # axis .a, so counting one names a number that is not the count.
        echo "-- prebuilt runtime: ${pb_staged[*]}; $(find "$LAYOUT/prebuilt" -type f \( -name '*.a' -o -name '*.lib' \) | wc -l | tr -d ' ') archives, $(du -sh "$LAYOUT/prebuilt" | awk '{print $1}')"
    fi
fi

# The staged build tools survived 3c read-only. Fatal rather than a warning: a
# cmake that writes into its own CMAKE_ROOT works on every machine that packages
# the bundle and fails on every machine that installs it.
if [ -n "${bt_frozen_at:-}" ]; then
    bt_written="$(find "$bt_dest" -newer "$bt_frozen_at" -print -quit)"
    chmod -R u+w "$bt_dest"
    rm -f "$bt_frozen_at"
    [ -z "$bt_written" ] || {
        echo "error: the read-only build tools were written to while the prebuilt axes built:" >&2
        echo "       $bt_written" >&2
        exit 1; }
fi

# The SHIPPED cache carries no sanity file (3b says why). A frozen cache writes
# none, so this catches only the SDK that arrived already staged under an older,
# unfrozen recipe.
rm -f "$LAYOUT/emsdk/emscripten/cache/sanity.txt"

# 4. Manifest — the fork<->dn2cpp version contract half (design §4).
dn2cpp_commit="$(git rev-parse HEAD 2>/dev/null || echo unknown)"
godot_pin="$(sed -n 's/^PINNED_COMMIT=//p' gates/setup-godot-dotnet.sh)"
godot_pin="${godot_pin%%$'\n'*}"
abi_uc=""; abi_mc=""
if [ -f gates/expected/godot-dotnet-abi.sha256 ]; then
    abi_uc="$(awk '/unmanaged_callbacks/{print $1}' gates/expected/godot-dotnet-abi.sha256)"
    abi_mc="$(awk '/ManagedCallbacks.cs/{print $1}' gates/expected/godot-dotnet-abi.sha256)"
fi
# Deterministic content hash over every bundled file except the manifest and the
# staged SDK. emsdk/ is out because it is 1.4 GB of upstream bytes already fixed
# by their archive's sha256, and because a build writes into its cache — hashing
# it would make the hash depend on what has been compiled through it, and with it
# every consumer that compares two bundles by this number. The emsdk_* fields
# below are what identifies the SDK instead. buildtools/ is IN for the converse
# reasons — ~20 MiB, deterministic, and nothing writes into it — so moving the
# cmake/ninja pin moves this hash, which is a free cache invalidation.
content_hash="$(cd "$LAYOUT" && find . -type f ! -name manifest.json ! -path './emsdk/*' \
    -exec shasum -a 256 {} + | LC_ALL=C sort | shasum -a 256 | awk '{print $1}')"

cat > "$LAYOUT/manifest.json" <<EOF
{
  "schema": 1,
  "package_version": "$PKG_VERSION",
  "dn2cpp_commit": "$dn2cpp_commit",
  "godot_pin": "$godot_pin",
  "host_os": "$HOST_OS",
  "host_arch": "$HOST_ARCH",
  "corelib_framework": "$fwver",
  "corelib": "ref/System.Private.CoreLib.dll",
  "corelib_cross": "$CROSS_CORELIB_REL",
  "emsdk_version": "$EMSDK_VERSION",
  "emsdk_release_hash": "$EMSDK_RELEASE_HASH",
  "emcc_version": "$EMCC_VERSION",
  "abi_fingerprint": {
    "unmanaged_callbacks": "$abi_uc",
    "ManagedCallbacks.cs": "$abi_mc"
  },
  "content_hash": "$content_hash"
}
EOF

echo "-- layout assembled: $LAYOUT ($(du -sh "$LAYOUT" 2>/dev/null | awk '{print $1}'))"

# 4b. Verify the ASSEMBLED LAYOUT against the host that assembled it — the one
# check in this script that is keyed on the host OS, and the half that had none.
# `ref-posix/` is the only thing here staged conditionally, it is asserted at the
# copy and then never looked at again, and the manifest's `corelib_cross` is
# written from a variable rather than from the tree: so a layout that lost it
# afterwards, or a manifest naming a path the tree does not hold, packages,
# installs and exports desktop games perfectly and fails the first cross-target
# export inside a linker — on a machine that has no copy of this repository.
# The expectation is the complement of the host-native target, stated the way
# the fork states it (Dn2CppToolchain.NeedsCrossCoreLib): a Windows bundle
# carries the POSIX framework, and a POSIX bundle carries none, because its own
# `ref/` already is one. Both directions are checked — an unexpected ref-posix/
# on a POSIX host would mean the fork picks a framework nothing staged.
if [ "$DN2CPP_OS" = windows ]; then
    if [ ! -f "$LAYOUT/ref-posix/System.Private.CoreLib.dll" ]; then
        echo "error: the assembled layout carries no POSIX framework:" >&2
        echo "       $LAYOUT/ref-posix/System.Private.CoreLib.dll is absent" >&2
        echo "       Every cross-compiled export (Android, Web) off a Windows host references it." >&2
        exit 1
    fi
    if [ "$CROSS_CORELIB_REL" != "ref-posix/System.Private.CoreLib.dll" ] \
        || [ ! -f "$LAYOUT/$CROSS_CORELIB_REL" ]; then
        echo "error: the manifest's corelib_cross ('$CROSS_CORELIB_REL') does not resolve in the layout" >&2
        exit 1
    fi
    echo "-- layout check: ref-posix/ holds $(find "$LAYOUT/ref-posix" -name '*.dll' | wc -l | tr -d ' ') assemblies"
elif [ -e "$LAYOUT/ref-posix" ]; then
    echo "error: a $HOST_OS layout must carry no ref-posix/ — its own ref/ already is the" >&2
    echo "       POSIX flavour, and the fork resolves the cross framework by host, not by" >&2
    echo "       what happens to be on disk." >&2
    exit 1
fi

# 5. Tarball (unless --layout-only).
if [ "$LAYOUT_ONLY" -eq 0 ]; then
    tar -C "$OUT_PARENT" -czf "$OUT_PARENT/$NAME.tar.gz" "$NAME"
    echo "-- tarball: $OUT_PARENT/$NAME.tar.gz ($(du -sh "$OUT_PARENT/$NAME.tar.gz" 2>/dev/null | awk '{print $1}'))"
fi

# 5b. Size report, per axis, as a FILE beside the tarball. What the bundle costs
# moves with every dependency and every trim, so the number belongs in a generated
# artifact and nowhere else: a figure written into a doc is wrong by the time the
# reader who trusts it finds it. Rows are `name<TAB>bytes` for a consumer to read
# (the editor packaging's metadata takes its byte counts from here); the tail is
# the same rows for a human. A nested name is counted in the one it sits under.
#
# Apparent bytes, not disk usage: `du` rounds every file up to a block, and the
# question a size report is asked is what the download and the install weigh.
size_bytes() {   # size_bytes PATH — total bytes of the regular files under PATH
    [ -e "$1" ] || { printf '0\n'; return 0; }
    # A `wc -c` batch prints a "total" line per exec; every real row's second
    # field is a path, which always holds a separator here.
    find "$1" -type f -exec wc -c {} + \
        | awk '$2 ~ /\// { n += $1 } END { printf "%d\n", n }'
}
size_row() {     # size_row NAME PATH — one row, nothing at all when PATH is absent
    [ -e "$2" ] || return 0
    printf '%s\t%s\n' "$1" "$(size_bytes "$2")"
}
SIZE_REPORT="$OUT_PARENT/size-report.txt"
{
    printf '# GENERATED by dist/package-toolchain.sh — one `name<TAB>bytes` row per axis.\n'
    printf '# bundle\t%s\n' "$NAME"
    size_row bin           "$LAYOUT/bin"
    size_row ref           "$LAYOUT/ref"
    size_row ref-posix     "$LAYOUT/ref-posix"
    size_row runtime       "$LAYOUT/runtime"
    size_row third_party   "$LAYOUT/third_party"
    size_row prebuilt      "$LAYOUT/prebuilt"
    for size_axis in "$LAYOUT"/prebuilt/*/; do
        [ -d "$size_axis" ] || continue
        size_row "prebuilt/$(basename "$size_axis")" "${size_axis%/}"
    done
    size_row emsdk                        "$LAYOUT/emsdk"
    size_row emsdk/bin                    "$LAYOUT/emsdk/bin"
    size_row emsdk/lib                    "$LAYOUT/emsdk/lib"
    size_row emsdk/emscripten             "$LAYOUT/emsdk/emscripten"
    size_row emsdk/emscripten/cache       "$LAYOUT/emsdk/emscripten/cache"
    size_row emsdk/emscripten/node_modules "$LAYOUT/emsdk/emscripten/node_modules"
    size_row buildtools        "$LAYOUT/buildtools"
    size_row buildtools/cmake  "$LAYOUT/buildtools/cmake"
    size_row buildtools/ninja  "$LAYOUT/buildtools/ninja"
    size_row layout        "$LAYOUT"
    size_row tarball       "$OUT_PARENT/$NAME.tar.gz"
} > "$SIZE_REPORT"
# Read whole, then appended: awk streaming a file it is also being appended to
# would read what it just wrote.
size_summary="$(awk -F'\t' '$1 !~ /^#/ && NF == 2 { printf "#   %-31s %9.1f MiB\n", $1, $2 / 1048576 }' \
    "$SIZE_REPORT")"
printf '\n# summary\n%s\n' "$size_summary" >> "$SIZE_REPORT"
echo "-- size report: $SIZE_REPORT"
printf '%s\n' "$size_summary"

# 6. Install into an editor GodotSharp/Dn2Cpp dir (when requested).
if [ -n "$INSTALL_INTO" ]; then
    mkdir -p "$INSTALL_INTO"
    cp -R "$LAYOUT/." "$INSTALL_INTO/"
    echo "-- installed into $INSTALL_INTO"
fi

echo "OK: $NAME"

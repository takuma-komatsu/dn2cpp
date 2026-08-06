#!/usr/bin/env bash
# dist/package-editor-windows.sh — wrap the fork's already-built Windows editor
# into a distributable Godot-dn2cpp directory and a zip for a GitHub release. It
# BUILDS NOTHING of the engine: gates/setup-godot-fork.sh owns that, and this
# script refuses an editor that does not describe the fork's sources now.
#
# GodotSharp/ sits beside the editor executable because godotsharp_dirs.cpp
# resolves it next to the running image, and gates/_godot_fork.sh derives
# FORK_GODOTSHARP the same way — that is what makes the assembled directory a
# fork root's editor, so the editor-export gates run against it unmodified
# (--smoke does exactly that). Nothing in the package is a symlink; see the
# assembling step for why that is an invariant here and not a preference.
#
# Usage:
#   dist/package-editor-windows.sh --version <base>-dn2cpp.<n> [options]
#     --out DIR                  output parent (default: artifacts/release)
#     --app-name NAME            package name (default: Godot-dn2cpp)
#     --smoke | --no-smoke       run the two editor-export gates against the
#                                assembled package (default: --smoke)
#     --allow-partial-prebuilt   accept a toolchain missing cross-compile axes
#     --web-asset PATH           the RELEASE Web template the smoke exports with
#                                (default: <out>/godot-dn2cpp-<version>-web-template.zip)
#     -h | --help
source "$(dirname "$0")/../gates/_common.sh"       # repo-root cd, DN2CPP_OS, EXE_EXT, stage_editor_toolchain
source "$(dirname "$0")/../gates/_godot_fork.sh"   # FORK_ROOT/FORK_EDITOR/FORK_GODOTSHARP, SELFHOST_BIN

die() { echo "error: $*" >&2; exit 1; }

VERSION=
OUT=artifacts/release
APP_NAME=Godot-dn2cpp
SMOKE=1
ALLOW_PARTIAL_PREBUILT=0
WEB_ASSET=

while [ $# -gt 0 ]; do
    case "$1" in
        --version)                VERSION="$2"; shift 2 ;;
        --out)                    OUT="$2"; shift 2 ;;
        --app-name)               APP_NAME="$2"; shift 2 ;;
        --smoke)                  SMOKE=1; shift ;;
        --no-smoke)               SMOKE=0; shift ;;
        --allow-partial-prebuilt) ALLOW_PARTIAL_PREBUILT=1; shift ;;
        --web-asset)              WEB_ASSET="$2"; shift 2 ;;
        # The header IS the help text, printed up to the first non-comment line
        # rather than by line number, so editing it cannot truncate --help.
        -h|--help)                awk 'NR > 1 && !/^#/ { exit } NR > 1 { print }' "$0"; exit 0 ;;
        *) echo "error: unknown argument: $1" >&2; exit 2 ;;
    esac
done
[ -n "$VERSION" ] || die "--version is required (e.g. --version 4.7.1-dn2cpp.1)"

# ── 0. Prerequisites ─────────────────────────────────────────────────────────
# Every one is a hard FAIL, never gate_skip: this is a release cut asked for by
# hand, and "the box could not carry it" is the wrong reading for a missing input
# the operator can produce.
echo "== 0/12 prerequisites =="
[ "$DN2CPP_OS" = windows ] || die "this packages a Windows editor; host OS is $DN2CPP_OS"
HOST_ARCH="$(uname -m)"
[ "$HOST_ARCH" = x86_64 ] || die "the fork's editor is x86_64; host arch is $HOST_ARCH"

godot_fork_resolve || exit 1
[ -x "$FORK_EDITOR" ] || die "no fork editor at $FORK_EDITOR — run gates/setup-godot-fork.sh"
# The console wrapper is half of what a Windows editor is: without it a user has
# no way to read the editor's own stdout, which every export diagnostic goes to.
FORK_CONSOLE="${FORK_EDITOR%$EXE_EXT}.console$EXE_EXT"
[ -f "$FORK_CONSOLE" ] || die "no console wrapper beside the editor at $FORK_CONSOLE — run gates/setup-godot-fork.sh"
[ -f "$FORK_ROOT/pin.txt" ] || die "no $FORK_ROOT/pin.txt — run gates/setup-godot-fork.sh"
[ -d "$FORK_GODOTSHARP" ] || die "no GodotSharp tree at $FORK_GODOTSHARP — run gates/setup-godot-fork.sh"
compgen -G "$FORK_ROOT/nuget/Godot.NET.Sdk.*.nupkg" >/dev/null \
    || die "no Godot.NET.Sdk nupkg in $FORK_ROOT/nuget — run gates/setup-godot-fork.sh"

# Same stamp comparison as godot_fork_preflight: a missing stamp is stale
# (provenance unknown), not a pass.
[ -x "$SELFHOST_BIN" ] || die "no $SELFHOST_BIN — run gates/selfhost-emit.sh"
SELFHOST_STAMPED="$(cat "$(dirname "$SELFHOST_BIN")/dn2cpp.src-hash" 2>/dev/null || echo '<no stamp>')"
SRC_NOW="$(src_tree_hash)"
if [ "$SELFHOST_STAMPED" != "$SRC_NOW" ]; then
    echo "error: $SELFHOST_BIN predates the current sources" >&2
    echo "       stamped $SELFHOST_STAMPED != $SRC_NOW (src_tree_hash of src/ runtime/ third_party/)" >&2
    echo "       Rebuild the self-hosted CLI: gates/selfhost-emit.sh" >&2
    exit 1
fi

godot_fork_pin_abi_check   # sets FORK + BASE_COMMIT; exits on any mismatch
FORK_HEAD="$(git -C "$FORK" rev-parse HEAD)"
echo "selfhost CLI:  $SELFHOST_BIN (src $SRC_NOW)"

# ── 1. Version ───────────────────────────────────────────────────────────────
echo "== 1/12 version =="
if [[ ! "$VERSION" =~ ^([0-9]+\.[0-9]+\.[0-9]+)-dn2cpp\.([0-9]+)$ ]]; then
    die "--version must read <major>.<minor>.<patch>-dn2cpp.<n>, got: $VERSION"
fi
VERSION_BASE="${BASH_REMATCH[1]}"
# version.py is the engine's own declaration of what this editor reports; a
# release named for another base would ship an editor that contradicts its name.
FORK_VERSION="$(awk -F' *= *' '/^major/{ma=$2} /^minor/{mi=$2} /^patch/{pa=$2} \
    END{ printf "%s.%s.%s\n", ma, mi, (pa == "" ? 0 : pa) }' "$FORK/version.py")"
[ "$VERSION_BASE" = "$FORK_VERSION" ] \
    || die "--version base $VERSION_BASE != the fork's version.py ($FORK_VERSION)"
echo "release:       $VERSION (base $VERSION_BASE, fork $FORK_HEAD)"

# ── 2. The editor describes the fork's engine sources now ────────────────────
echo "== 2/12 editor currency =="
ENGINE_HASH="$(godot_fork_engine_hash)"
EDITOR_STAMPED="$(head -1 "$FORK_EDITOR.engine-hash" 2>/dev/null || true)"
[ -n "$EDITOR_STAMPED" ] || EDITOR_STAMPED='<no stamp>'
if [ "$EDITOR_STAMPED" != "$ENGINE_HASH" ]; then
    echo "error: $FORK_EDITOR was not built from the engine sources in the fork" >&2
    echo "       stamped $EDITOR_STAMPED != $ENGINE_HASH" >&2
    echo "       Rebuild it: gates/setup-godot-fork.sh" >&2
    exit 1
fi
ENGINE_PROVENANCE="$(godot_fork_engine_provenance)"
echo "editor:        $FORK_EDITOR ($ENGINE_PROVENANCE)"

# ── 3. Re-stage the export toolchain into the fork's GodotSharp ──────────────
# The shared staging helper, called exactly as the gates call it: it packages
# this working tree, takes the stage lock, and swaps atomically.
echo "== 3/12 staging the export toolchain =="
mkdir -p "$OUT"
# Absolute from here: the editor resolves a relative OUT against the project.
OUT="$(cd "$OUT" && pwd)"
stage_editor_toolchain "$FORK_GODOTSHARP" "$SELFHOST_BIN" "$OUT/package.log"

MANIFEST="$FORK_GODOTSHARP/Dn2Cpp/manifest.json"
[ -f "$MANIFEST" ] || die "no staged manifest at $MANIFEST"
PY="$(resolve_python)"
manifest_get() {   # manifest_get KEY
    # shellcheck disable=SC2086
    $PY -c 'import json,sys; print(json.load(open(sys.argv[1]))[sys.argv[2]])' "$MANIFEST" "$1"
}
TOOLCHAIN_HASH="$(manifest_get content_hash)"
DN2CPP_COMMIT="$(manifest_get dn2cpp_commit)"
CORELIB_FRAMEWORK="$(manifest_get corelib_framework)"
BUNDLE_EMCC="$(manifest_get emcc_version)"
echo "toolchain:     content_hash $TOOLCHAIN_HASH, dn2cpp $DN2CPP_COMMIT, corelib $CORELIB_FRAMEWORK"

# What the bundle weighs, from the report the packaging above wrote beside the
# layout — never a second measurement here, which would be a second answer. The
# `# bundle` row names the layout the report describes, so comparing that
# layout's manifest with the staged one proves the report is this staging's.
SIZE_REPORT=artifacts/toolchain/size-report.txt
[ -f "$SIZE_REPORT" ] \
    || die "no toolchain size report at $SIZE_REPORT — dist/package-toolchain.sh writes it beside the layout"
size_report_row() {   # size_report_row NAME — whole-file awk, no early exit
    awk -F'\t' -v k="$1" '$1 == k { v = $2 } END { print v }' "$SIZE_REPORT"
}
SIZE_BUNDLE="$(size_report_row '# bundle')"
cmp -s "artifacts/toolchain/$SIZE_BUNDLE/manifest.json" "$MANIFEST" \
    || die "$SIZE_REPORT describes '$SIZE_BUNDLE', which is not the toolchain just staged"
BUNDLE_BYTES="$(size_report_row layout)"
EMSDK_BYTES="$(size_report_row emsdk)"
BUILDTOOLS_BYTES="$(size_report_row buildtools)"
NODE_BYTES="$(size_report_row emsdk/node)"
[ -n "$BUNDLE_BYTES" ] || die "$SIZE_REPORT carries no 'layout' row — the installed size is unknown"
[ -n "$EMSDK_BYTES" ] || die "$SIZE_REPORT carries no 'emsdk' row — the bundle staged no Emscripten SDK"
# Packaging a bundle without either only warns, because a developer's machine
# already has cmake and node; a PUBLISHED editor must not send its users to a
# package manager to export.
[ -n "$BUILDTOOLS_BYTES" ] \
    || die "$SIZE_REPORT carries no 'buildtools' row — the bundle staged no cmake/ninja (gates/setup-buildtools.sh unpacks the pinned pair)"
[ -n "$NODE_BYTES" ] \
    || die "$SIZE_REPORT carries no 'emsdk/node' row — the SDK staged no node, which every emcc link runs (gates/setup-emsdk.sh unpacks the pinned one)"
echo "size:          $BUNDLE_BYTES bytes installed, of which emsdk $EMSDK_BYTES (node $NODE_BYTES), buildtools $BUILDTOOLS_BYTES"

# The staged tree's own provenance, never a second reading of the pin: what
# shipped is what that file describes.
BUILDTOOLS_JSON="$FORK_GODOTSHARP/Dn2Cpp/buildtools/buildtools.json"
[ -f "$BUILDTOOLS_JSON" ] || die "no staged buildtools provenance at $BUILDTOOLS_JSON"
buildtools_get() {   # buildtools_get KEY
    # shellcheck disable=SC2086
    $PY -c 'import json,sys; print(json.load(open(sys.argv[1]))[sys.argv[2]])' "$BUILDTOOLS_JSON" "$1"
}
BUNDLE_CMAKE="$(buildtools_get cmake_version)"
BUNDLE_NINJA="$(buildtools_get ninja_version)"
echo "buildtools:    cmake $BUNDLE_CMAKE + ninja $BUNDLE_NINJA"

EMSDK_JSON="$FORK_GODOTSHARP/Dn2Cpp/emsdk/emsdk.json"
[ -f "$EMSDK_JSON" ] || die "no staged emsdk provenance at $EMSDK_JSON"
emsdk_get() {   # emsdk_get KEY
    # shellcheck disable=SC2086
    $PY -c 'import json,sys; print(json.load(open(sys.argv[1]))[sys.argv[2]])' "$EMSDK_JSON" "$1"
}
BUNDLE_NODE="$(emsdk_get node_version)"
echo "node:          $BUNDLE_NODE, inside the SDK"

# ── 4. Export axes ───────────────────────────────────────────────────────────
# A missing axis costs the first export to it a from-source runtime build — same
# binary, more minutes — so it is a release-quality question, not a correctness
# one. `host` is the exception: without it every desktop export pays that, and
# dist/smoke-test.sh proves the host prebuilt is the one thing nothing else links.
echo "== 4/12 prebuilt axes + the Web toolchain =="
PREBUILT="$FORK_GODOTSHARP/Dn2Cpp/prebuilt"
AXES=""
for d in "$PREBUILT"/*/; do
    [ -d "$d" ] || continue
    AXES="$AXES $(basename "$d")"
done
AXES="${AXES# }"
have_axis() {
    case " $AXES " in *" $1 "*) return 0 ;; esac
    return 1
}
have_axis host || die "the staged toolchain carries no prebuilt/host — re-package (dist/package-toolchain.sh)"
MISSING=""
# The axes dist/package-toolchain.sh can stage on THIS host: it gates the three
# iOS ones on `HOST_OS = macos`, so naming them here would report a permanent
# property of Windows as a missing artifact an operator could produce.
for a in android-arm64-v8a web-wasm32; do
    if ! have_axis "$a"; then MISSING="$MISSING $a"; fi
done
MISSING="${MISSING# }"
if [ -n "$MISSING" ]; then
    if [ "$ALLOW_PARTIAL_PREBUILT" -eq 0 ]; then
        echo "error: the staged toolchain is missing prebuilt axes: $MISSING" >&2
        echo "       An export to such a target builds the runtime from source on its first" >&2
        echo "       run — the result is identical, it only costs minutes. Package the missing" >&2
        echo "       axes (an NDK / emsdk on this host lets dist/package-toolchain.sh stage" >&2
        echo "       them), or accept the degrade with --allow-partial-prebuilt." >&2
        exit 1
    fi
    echo "warning: shipping without prebuilt axes: $MISSING (--allow-partial-prebuilt)" >&2
fi
echo "prebuilt:      $AXES"

# The Web axis has a second half no axis directory can express: the emcc that
# links the drop-in must be the one that linked the template it loads into. A
# bundle carrying no SDK has no emcc to disagree, and the missing web-wasm32 axis
# above is the only refusal that case earns.
if [ -n "$BUNDLE_EMCC" ]; then
    godot_fork_web_template_emcc_assert "$BUNDLE_EMCC" "$OUT" "$VERSION"
else
    echo "warning: the staged toolchain carries no Emscripten SDK — nothing to match the Web template against" >&2
fi

# ── 5. Assemble the package ──────────────────────────────────────────────────
echo "== 5/12 assembling $APP_NAME =="
STAGE="$OUT/stage"
PKG="$STAGE/$APP_NAME"
rm -rf "$STAGE"
mkdir -p "$PKG"
cp "$FORK_EDITOR" "$PKG/$APP_NAME$EXE_EXT"
# Renaming the pair together keeps them paired: console_wrapper_windows.cpp
# derives the GUI executable from its OWN module file name by rewriting the
# `.console.exe` suffix to `.exe`, so it follows any name both halves share.
cp "$FORK_CONSOLE" "$PKG/$APP_NAME.console$EXE_EXT"
# The Agility SDK, if this engine was built against one: the D3D12 driver asks
# the loader for `.\<arch>` and then `.\`, i.e. beside the running executable.
for dll in D3D12Core.dll d3d12SDKLayers.dll; do
    if [ -f "$(dirname "$FORK_EDITOR")/$dll" ]; then
        cp "$(dirname "$FORK_EDITOR")/$dll" "$PKG/$dll"
    fi
done
# A real tree, never a link. MSYS/Git-Bash `ln -s` silently COPIES without a
# winsymlinks mode and the privilege to create one, and a copied GodotSharp is
# the quiet failure this lane has already recorded twice: the editor then loads a
# snapshot of the very tree the staging rewrites.
cp -R "$FORK_GODOTSHARP" "$PKG/GodotSharp"

# ── 6. Pre-archive checks ────────────────────────────────────────────────────
echo "== 6/12 pre-archive checks =="
for f in \
    "$APP_NAME$EXE_EXT" \
    "$APP_NAME.console$EXE_EXT" \
    "D3D12Core.dll" \
    "GodotSharp/Api/Release/GodotSharp.dll" \
    "GodotSharp/Tools/GodotTools.dll" \
    "GodotSharp/Dn2Cpp/manifest.json" \
    "GodotSharp/Dn2Cpp/bin/dn2cpp$EXE_EXT" \
    "GodotSharp/Dn2Cpp/runtime/CMakeLists.txt" \
    "GodotSharp/Dn2Cpp/ref/System.Private.CoreLib.dll" \
    "GodotSharp/Dn2Cpp/ref-posix/System.Private.CoreLib.dll" \
    "GodotSharp/Dn2Cpp/bin/Dn2Cpp.Runtime.dll" \
    "GodotSharp/Dn2Cpp/bin/DnZlib.dll" \
    "GodotSharp/Dn2Cpp/bin/DnBrotli.dll" \
    "GodotSharp/Dn2Cpp/bin/DnHttp.dll" \
    ; do
    # ref-posix/ is on this list and on no macOS one: every cross-compiled export
    # off a Windows host (Android, Web) is transpiled against that POSIX flavour,
    # and dist/package-toolchain.sh stages it for a Windows layout alone.
    [ -f "$PKG/$f" ] || die "the assembled package is missing $f"
done
compgen -G "$PKG/GodotSharp/Tools/nupkgs/Godot.NET.Sdk.*.nupkg" >/dev/null \
    || die "the assembled package carries no Godot.NET.Sdk nupkg in GodotSharp/Tools/nupkgs"
BUNDLED_HASH="$($PY -c 'import json,sys; print(json.load(open(sys.argv[1]))["content_hash"])' \
    "$PKG/GodotSharp/Dn2Cpp/manifest.json")"
[ "$BUNDLED_HASH" = "$TOOLCHAIN_HASH" ] \
    || die "the bundled toolchain manifest ($BUNDLED_HASH) is not the one staged ($TOOLCHAIN_HASH) — a concurrent re-stage?"

# Nothing may be a link: the archiver below refuses one, and a link that reached
# a user would resolve against a path only this machine has.
LINKS="$(find "$PKG" -type l)"
[ -z "$LINKS" ] || die "the assembled package holds symlinks, which no consumer can resolve:
$LINKS"

EDITOR_VERSION="$(first_line "$(run_with_watchdog 60 "$PKG/$APP_NAME$EXE_EXT" --headless --version)")"
case "$EDITOR_VERSION" in
    "$VERSION_BASE".stable.mono*) ;;
    *) die "the bundled editor reports '$EDITOR_VERSION', which is no $VERSION_BASE.stable.mono build" ;;
esac
echo "editor says:   $EDITOR_VERSION"

# ── 7. RELEASE.txt ───────────────────────────────────────────────────────────
# After the checks, because it carries what the editor reports about itself. The
# PE VERSIONINFO is deliberately left alone: the editor's .engine-hash stamp is
# taken over the bytes in the fork's bin/, and rewriting a resource here would
# make the shipped image differ from the one that stamp describes.
echo "== 7/12 RELEASE.txt =="
cat > "$PKG/RELEASE.txt" <<EOF
release=$VERSION
fork_commit=$FORK_HEAD
engine_provenance=$ENGINE_PROVENANCE
toolchain_content_hash=$TOOLCHAIN_HASH
editor_version_string=$EDITOR_VERSION
EOF

# ── 8. Smoke: the real export gates, against this package ────────────────────
if [ "$SMOKE" -eq 1 ]; then
    echo "== 8/12 smoke (editor-export gates against the package) =="
    SMOKE_ROOT="$OUT/smoke-root"
    rm -rf "$SMOKE_ROOT"
    mkdir -p "$SMOKE_ROOT"
    cp -R "$FORK_ROOT/nuget" "$SMOKE_ROOT/nuget"
    DESKTOP_TEMPLATE="$(godot_fork_desktop_template "$FORK_ROOT")"
    DESKTOP_TEMPLATE_NAME="$(basename "$DESKTOP_TEMPLATE")"
    for f in pin.txt fork_head.txt clone.txt template.txt web_emcc.txt \
             "$DESKTOP_TEMPLATE_NAME" "$DESKTOP_TEMPLATE_NAME.provenance"; do
        [ -f "$FORK_ROOT/$f" ] || die "the fork root has no $f — run gates/setup-godot-fork.sh (and -web)"
        cp "$FORK_ROOT/$f" "$SMOKE_ROOT/$f"
    done
    # The Web template must be the RELEASE one, not the fork root's: the archive
    # a user downloads and this run must agree about the engine inside it.
    [ -n "$WEB_ASSET" ] || WEB_ASSET="$OUT/godot-dn2cpp-$VERSION-web-template.zip"
    [ -f "$WEB_ASSET" ] \
        || die "no $WEB_ASSET — cut the Web template first: dist/package-web-template.sh --version $VERSION"
    cp "$WEB_ASSET" "$SMOKE_ROOT/web_template.zip"
    if [ -f "$WEB_ASSET.provenance" ]; then
        cp "$WEB_ASSET.provenance" "$SMOKE_ROOT/web_template.zip.provenance"
    else
        # The stamp says which engine BAKED the asset, so it can only be copied
        # from something that witnessed the bake. dist/package-web-template.sh's
        # metadata is such a witness, and binds its claim to the asset by hash —
        # so the hash is checked first and a mismatch is fatal. Recomputing
        # godot_fork_engine_provenance here instead would stamp the asset with
        # this worktree's engine, which is a forgery whenever the two differ,
        # and unfalsifiable exactly when it matters.
        META="$OUT/web.metadata"
        [ -f "$META" ] || die "no $WEB_ASSET.provenance and no $META — the Web asset's engine is
       unknown, and this run must not invent it. Either fetch the .provenance stamp
       published beside the release asset, or re-cut the asset here:
       dist/package-web-template.sh --version $VERSION"
        meta_get() { first_line "$(sed -n "s/^$1=//p" "$META")"; }
        META_SHA="$(meta_get asset_sha256)"
        WEB_SHA="$(shasum -a 256 "$WEB_ASSET")"
        WEB_SHA="${WEB_SHA%% *}"
        [ -n "$META_SHA" ] && [ "$META_SHA" = "$WEB_SHA" ] \
            || die "$META describes an asset with sha256 '$META_SHA', but $WEB_ASSET is $WEB_SHA —
       the metadata does not describe this file, so its engine_provenance says nothing about it"
        META_PROV="$(meta_get engine_provenance)"
        [ -n "$META_PROV" ] || die "$META carries no engine_provenance"
        printf '%s\n' "$META_PROV" > "$SMOKE_ROOT/web_template.zip.provenance"
    fi
    # editor.txt is the whole point: the gates run the package's own binary, and
    # derive GodotSharp beside it.
    printf '%s\n' "$(cd "$PKG" && pwd)/$APP_NAME$EXE_EXT" > "$SMOKE_ROOT/editor.txt"

    DN2CPP_GODOT_FORK_ROOT="$SMOKE_ROOT" DN2CPP_GATE_CACHE=0 \
        ./gates/build-and-run-godot-editor-export.sh
    DN2CPP_GODOT_FORK_ROOT="$SMOKE_ROOT" DN2CPP_GATE_CACHE=0 \
        ./gates/build-and-run-godot-editor-export-web.sh
    rm -rf "$SMOKE_ROOT"
else
    echo "== 8/12 smoke skipped (--no-smoke) =="
fi

# ── 9. Archive ───────────────────────────────────────────────────────────────
# Python's zipfile, because neither `zip` nor `7z` can be assumed on a Windows
# host, and PowerShell's Compress-Archive writes `\` as the entry separator on
# 5.1 — which every other extractor reads as one file name containing slashes.
# Fixing the walk order, the timestamps and the mode also makes the asset a pure
# function of the staged tree: one input tree, one sha256, which `ditto` on the
# macOS side does not give.
echo "== 9/12 archiving =="
ASSET="$OUT/$APP_NAME-$VERSION-windows-x86_64.zip"
rm -f "$ASSET"
# The fork commit's own date, so the entry timestamps name the engine rather than
# the hour this ran.
FORK_EPOCH="$(git -C "$FORK" show -s --format=%ct "$FORK_HEAD")"
# shellcheck disable=SC2086
$PY - "$STAGE" "$APP_NAME" "$ASSET" "$FORK_EPOCH" <<'PY'
import os, shutil, sys, time, zipfile

stage, app_name, asset, epoch = sys.argv[1], sys.argv[2], sys.argv[3], int(sys.argv[4])
date_time = time.gmtime(epoch)[:6]
if date_time[0] < 1980:
    sys.exit("error: the zip format cannot express a %d timestamp" % date_time[0])

root = os.path.join(stage, app_name)
with zipfile.ZipFile(asset, "w", zipfile.ZIP_DEFLATED, allowZip64=True, compresslevel=6) as z:
    for dirpath, dirs, files in os.walk(root):
        dirs.sort()
        files.sort()
        for name in dirs + files:
            path = os.path.join(dirpath, name)
            if os.path.islink(path):
                sys.exit("error: refusing to archive a symlink: " + path)
        for name in files:
            path = os.path.join(dirpath, name)
            arcname = os.path.relpath(path, stage).replace(os.sep, "/")
            info = zipfile.ZipInfo(arcname, date_time)
            info.compress_type = zipfile.ZIP_DEFLATED
            info.external_attr = 0o644 << 16
            with open(path, "rb") as src, z.open(info, "w") as dst:
                shutil.copyfileobj(src, dst)
PY

# ── 10. Round-trip ───────────────────────────────────────────────────────────
# The archive is the artifact users get; verifying the tree we still hold proves
# nothing about what survives the archiver. There is no executable bit to check —
# Windows takes that from the file extension, not from a mode.
echo "== 10/12 round-trip =="
RT="$(mktemp -d)"
gate_add_exit_hook "rm -rf '$RT'"
# shellcheck disable=SC2086
$PY - "$ASSET" "$RT" "$STAGE" "$APP_NAME" <<'PY'
import hashlib, os, sys, zipfile

asset, dest, stage, app_name = sys.argv[1:5]
with zipfile.ZipFile(asset) as z:
    z.extractall(dest)

def tree(root):
    out = {}
    for dirpath, dirs, files in os.walk(root):
        dirs.sort()
        files.sort()
        for name in files:
            path = os.path.join(dirpath, name)
            h = hashlib.sha256()
            with open(path, "rb") as f:
                for chunk in iter(lambda: f.read(1 << 20), b""):
                    h.update(chunk)
            out[os.path.relpath(path, root).replace(os.sep, "/")] = h.hexdigest()
    return out

staged, unpacked = tree(os.path.join(stage, app_name)), tree(os.path.join(dest, app_name))
lost = sorted(set(staged) - set(unpacked))
extra = sorted(set(unpacked) - set(staged))
differ = sorted(k for k in set(staged) & set(unpacked) if staged[k] != unpacked[k])
if lost or extra or differ:
    for label, names in (("missing", lost), ("unexpected", extra), ("differing", differ)):
        for n in names:
            print("error: %s in the unpacked archive: %s" % (label, n), file=sys.stderr)
    sys.exit(1)
print("%d files, all sha256-identical" % len(staged))
PY

RT_PKG="$RT/$APP_NAME"
[ -d "$RT_PKG" ] || die "the archive does not unpack to $APP_NAME/"
RT_VERSION="$(first_line "$(run_with_watchdog 60 "$RT_PKG/$APP_NAME$EXE_EXT" --headless --version)")"
[ "$RT_VERSION" = "$EDITOR_VERSION" ] \
    || die "the unpacked editor reports '$RT_VERSION', not '$EDITOR_VERSION'"
rt_release_get() { first_line "$(sed -n "s/^$1=//p" "$RT_PKG/RELEASE.txt")"; }
[ "$(rt_release_get release)" = "$VERSION" ] \
    || die "the unpacked RELEASE.txt names release '$(rt_release_get release)', not '$VERSION'"
[ "$(rt_release_get fork_commit)" = "$FORK_HEAD" ] \
    || die "the unpacked RELEASE.txt names fork_commit '$(rt_release_get fork_commit)', not '$FORK_HEAD'"
[ "$(rt_release_get toolchain_content_hash)" = "$TOOLCHAIN_HASH" ] \
    || die "the unpacked RELEASE.txt names toolchain_content_hash '$(rt_release_get toolchain_content_hash)', not '$TOOLCHAIN_HASH'"
rm -rf "$RT"
echo "round-trip:    verified, editor reports $RT_VERSION"

# ── 11. Checksum + metadata ──────────────────────────────────────────────────
echo "== 11/12 checksum + metadata =="
ASSET_NAME="$(basename "$ASSET")"
ASSET_SHA="$(shasum -a 256 "$ASSET")"
ASSET_SHA="${ASSET_SHA%% *}"
SUMS="$OUT/SHA256SUMS.txt"
# Idempotent: drop any previous row for this exact asset before appending.
if [ -f "$SUMS" ]; then
    grep -v "  $ASSET_NAME\$" "$SUMS" > "$SUMS.tmp" || true
    mv -f "$SUMS.tmp" "$SUMS"
fi
printf '%s  %s\n' "$ASSET_SHA" "$ASSET_NAME" >> "$SUMS"

cat > "$OUT/editor-windows.metadata" <<EOF
release_version=$VERSION
asset=$ASSET_NAME
asset_sha256=$ASSET_SHA
fork_commit=$FORK_HEAD
base_pin=$(cat "$FORK_ROOT/pin.txt")
engine_provenance=$ENGINE_PROVENANCE
editor_version_string=$EDITOR_VERSION
dn2cpp_commit=$DN2CPP_COMMIT
toolchain_content_hash=$TOOLCHAIN_HASH
corelib_framework=$CORELIB_FRAMEWORK
prebuilt_axes=$AXES
bundle_installed_bytes=$BUNDLE_BYTES
emsdk_installed_bytes=$EMSDK_BYTES
buildtools_installed_bytes=$BUILDTOOLS_BYTES
node_installed_bytes=$NODE_BYTES
cmake_version=$BUNDLE_CMAKE
ninja_version=$BUNDLE_NINJA
node_version=$BUNDLE_NODE
codesign=unsigned
EOF

echo "== 12/12 done =="
echo "OK: $ASSET (unsigned, deterministic zip, sha256 $ASSET_SHA)"

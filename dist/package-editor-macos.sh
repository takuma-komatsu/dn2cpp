#!/usr/bin/env bash
# dist/package-editor-macos.sh — wrap the fork's already-built macOS editor into a
# distributable, ad-hoc signed Godot-dn2cpp.app, a ditto archive and the
# `editor-macos` lane's metadata dist/release-github.sh cuts a GitHub release
# from. It BUILDS NOTHING of the engine: gates/setup-godot-fork.sh owns that,
# and this script refuses an editor that does not describe the fork's sources now.
#
# Contents/MacOS/GodotSharp must resolve: godotsharp_dirs.cpp looks for GodotSharp
# beside the running executable, and gates/_godot_fork.sh derives FORK_GODOTSHARP
# the same way — that is what makes the assembled .app a fork root's editor, so
# the editor-export gates run against it unmodified (--smoke does exactly that).
# It is a SYMLINK to Contents/Resources/GodotSharp, where the tree itself lives,
# because codesign treats everything under Contents/MacOS as nested CODE and
# refuses to seal a bundle holding an unsigned one — a managed .dll there aborts
# the signature with "code object is not signed at all". Under Resources the same
# tree is sealed as resources, which is what every other .NET-carrying app does
# (stock Godot ships GodotSharp there too). Signing the assemblies instead would
# mean rewriting the very IL the transpiler reads.
#
# Usage:
#   dist/package-editor-macos.sh --version <base>-dn2cpp.<X>.<Y>
#                                --dn2cpp-commit <sha> [options]
#     --dn2cpp-commit SHA        the dn2cpp commit both editors are cut from; the
#                                run refuses unless this tree IS it. macOS is the
#                                host that names it, and the value travels to the
#                                Windows host in this lane's own metadata
#     --out DIR                  output parent (default: artifacts/release)
#     --app-name NAME            bundle name (default: Godot-dn2cpp); names the
#                                .app inside, never the asset file
#     --smoke | --no-smoke       run the two editor-export gates against the
#                                assembled .app (default: --smoke)
#     --allow-partial-prebuilt   accept a toolchain missing cross-compile axes
#     -h | --help
source "$(dirname "$0")/../gates/_common.sh"       # repo-root cd, DN2CPP_OS, stage_editor_toolchain
source "$(dirname "$0")/../gates/_godot_fork.sh"   # FORK_ROOT/FORK_EDITOR/FORK_GODOTSHARP, SELFHOST_BIN

die() { echo "error: $*" >&2; exit 1; }

VERSION=
DN2CPP_PIN=
OUT=artifacts/release
APP_NAME=Godot-dn2cpp
SMOKE=1
ALLOW_PARTIAL_PREBUILT=0

while [ $# -gt 0 ]; do
    case "$1" in
        --version)                VERSION="$2"; shift 2 ;;
        --dn2cpp-commit)          DN2CPP_PIN="$2"; shift 2 ;;
        --out)                    OUT="$2"; shift 2 ;;
        --app-name)               APP_NAME="$2"; shift 2 ;;
        --smoke)                  SMOKE=1; shift ;;
        --no-smoke)               SMOKE=0; shift ;;
        --allow-partial-prebuilt) ALLOW_PARTIAL_PREBUILT=1; shift ;;
        # The header IS the help text, printed up to the first non-comment line
        # rather than by line number, so editing it cannot truncate --help.
        -h|--help)                awk 'NR > 1 && !/^#/ { exit } NR > 1 { print }' "$0"; exit 0 ;;
        *) echo "error: unknown argument: $1" >&2; exit 2 ;;
    esac
done
[ -n "$VERSION" ] || die "--version is required (e.g. --version 4.7.1-dn2cpp.3.1)"
# Required, not defaulted to HEAD: a value read off the tree could not be refused,
# and refusing is the whole of it — the Windows host is pinned to whatever this
# metadata says, months of drift later.
[ -n "$DN2CPP_PIN" ] || die "--dn2cpp-commit is required — both editors are cut from one dn2cpp commit
       This is the host that names it:
         dist/package-editor-macos.sh --version $VERSION --dn2cpp-commit \"\$(git rev-parse HEAD)\""

# ── 0. Prerequisites ─────────────────────────────────────────────────────────
# Every one is a hard FAIL, never gate_skip: this is a release cut asked for by
# hand, and "the box could not carry it" is the wrong reading for a missing input
# the operator can produce.
echo "== 0/13 prerequisites =="
[ "$DN2CPP_OS" = macos ] || die "this packages a macOS editor; host OS is $DN2CPP_OS"
HOST_ARCH="$(uname -m)"
[ "$HOST_ARCH" = arm64 ] || die "the fork's editor is arm64; host arch is $HOST_ARCH"

godot_fork_resolve || exit 1
[ -x "$FORK_EDITOR" ] || die "no fork editor at $FORK_EDITOR — run gates/setup-godot-fork.sh"
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

dn2cpp_commit_pin_resolve "$DN2CPP_PIN" || exit 1

godot_fork_pin_abi_check   # sets FORK + BASE_COMMIT; exits on any mismatch
FORK_HEAD="$(git -C "$FORK" rev-parse HEAD)"
echo "selfhost CLI:  $SELFHOST_BIN (src $SRC_NOW)"
echo "dn2cpp pin:    $DN2CPP_PIN_COMMIT (this tree is that commit, clean)"

# ── 1. Version ───────────────────────────────────────────────────────────────
echo "== 1/13 version =="
release_version_split "$VERSION" || exit 1
VERSION_BASE="$RELEASE_BASE_VER"
# version.py is the engine's own declaration of what this editor reports; a
# release named for another base would ship an editor that contradicts its name.
FORK_VERSION="$(awk -F' *= *' '/^major/{ma=$2} /^minor/{mi=$2} /^patch/{pa=$2} \
    END{ printf "%s.%s.%s\n", ma, mi, (pa == "" ? 0 : pa) }' "$FORK/version.py")"
[ "$VERSION_BASE" = "$FORK_VERSION" ] \
    || die "--version base $VERSION_BASE != the fork's version.py ($FORK_VERSION)"
echo "release:       $VERSION (base $VERSION_BASE, fork $FORK_HEAD)"

# ── 2. The editor describes the fork's engine sources now ────────────────────
echo "== 2/13 editor currency =="
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
echo "== 3/13 staging the export toolchain =="
mkdir -p "$OUT"
# Absolute from here: the editor resolves a relative OUT against the project.
OUT="$(cd "$OUT" && pwd)"
stage_editor_toolchain "$FORK_GODOTSHARP" "$SELFHOST_BIN" "$OUT/package.log" "$DN2CPP_PIN_COMMIT"

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
# The pin was verified where the bundle was built; this is the same claim read
# back off the file the lane metadata below is copied from, which is the one a
# reader of the release notes ends up trusting.
[ "$DN2CPP_COMMIT" = "$DN2CPP_PIN_COMMIT" ] \
    || die "the staged toolchain says dn2cpp_commit=$DN2CPP_COMMIT, not the pinned $DN2CPP_PIN_COMMIT"
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
echo "== 4/13 prebuilt axes + the Web toolchain =="
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
for a in ios-arm64 iossimulator-arm64 iossimulator-x64 android-arm64-v8a web-wasm32; do
    if ! have_axis "$a"; then MISSING="$MISSING $a"; fi
done
MISSING="${MISSING# }"
if [ -n "$MISSING" ]; then
    if [ "$ALLOW_PARTIAL_PREBUILT" -eq 0 ]; then
        echo "error: the staged toolchain is missing prebuilt axes: $MISSING" >&2
        echo "       An export to such a target builds the runtime from source on its first" >&2
        echo "       run — the result is identical, it only costs minutes. Package the missing" >&2
        echo "       axes (an NDK / emsdk / Xcode on this host lets dist/package-toolchain.sh" >&2
        echo "       stage them), or accept the degrade with --allow-partial-prebuilt." >&2
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

# ── 5. Assemble the .app ─────────────────────────────────────────────────────
echo "== 5/13 assembling $APP_NAME.app =="
STAGE="$OUT/stage"
APP="$STAGE/$APP_NAME.app"
rm -rf "$STAGE"
mkdir -p "$APP/Contents/MacOS"
cp -R "$FORK/misc/dist/macos_tools.app/Contents/Info.plist" \
      "$FORK/misc/dist/macos_tools.app/Contents/PkgInfo" \
      "$FORK/misc/dist/macos_tools.app/Contents/Resources" \
      "$APP/Contents/"
# The binary's name is Info.plist's CFBundleExecutable, which the fork's
# macos_tools.app spells `Godot`.
cp "$FORK_EDITOR" "$APP/Contents/MacOS/Godot"
chmod +x "$APP/Contents/MacOS/Godot"
# The tree under Resources, the name the engine and the gates look up beside the
# executable as a symlink onto it — see the header for why it cannot be the tree.
cp -R "$FORK_GODOTSHARP" "$APP/Contents/Resources/GodotSharp"
ln -s ../Resources/GodotSharp "$APP/Contents/MacOS/GodotSharp"

# ── 6. Info.plist ────────────────────────────────────────────────────────────
# CFBundleShortVersionString / CFBundleVersion stay as the engine wrote them:
# macOS constrains both to dotted digits, so `4.7.1-dn2cpp.3.1` cannot go there.
# The release identity lives in the three DN2CPP* keys instead.
echo "== 6/13 Info.plist =="
PLIST="$APP/Contents/Info.plist"
plutil -replace CFBundleIdentifier -string org.godotengine.godot.dn2cpp "$PLIST"
plutil -replace CFBundleName       -string "Godot dn2cpp"               "$PLIST"
plutil -insert  DN2CPPRelease      -string "$VERSION"                   "$PLIST"
plutil -insert  DN2CPPForkCommit   -string "$FORK_HEAD"                 "$PLIST"
plutil -insert  DN2CPPToolchainHash -string "$TOOLCHAIN_HASH"           "$PLIST"

# ── 7. Pre-signature checks ──────────────────────────────────────────────────
echo "== 7/13 pre-signature checks =="
# The contract path, asserted as a path rather than as a tree: everything below
# is spelled through it, so a broken link fails here and not in an export log.
if [ ! -L "$APP/Contents/MacOS/GodotSharp" ] || [ ! -d "$APP/Contents/MacOS/GodotSharp" ]; then
    die "Contents/MacOS/GodotSharp does not resolve — the engine and the gates both look for it there"
fi
for f in \
    "Contents/MacOS/Godot" \
    "Contents/MacOS/GodotSharp/Api/Release/GodotSharp.dll" \
    "Contents/MacOS/GodotSharp/Tools/GodotTools.dll" \
    "Contents/MacOS/GodotSharp/Dn2Cpp/manifest.json" \
    "Contents/MacOS/GodotSharp/Dn2Cpp/bin/dn2cpp" \
    "Contents/MacOS/GodotSharp/Dn2Cpp/runtime/CMakeLists.txt" \
    "Contents/MacOS/GodotSharp/Dn2Cpp/ref/System.Private.CoreLib.dll" \
    "Contents/MacOS/GodotSharp/Dn2Cpp/bin/Dn2Cpp.Runtime.dll" \
    "Contents/MacOS/GodotSharp/Dn2Cpp/bin/DnZlib.dll" \
    "Contents/MacOS/GodotSharp/Dn2Cpp/bin/DnBrotli.dll" \
    "Contents/MacOS/GodotSharp/Dn2Cpp/bin/DnHttp.dll" \
    ; do
    [ -f "$APP/$f" ] || die "the assembled bundle is missing $f"
done
compgen -G "$APP/Contents/MacOS/GodotSharp/Tools/nupkgs/Godot.NET.Sdk.*.nupkg" >/dev/null \
    || die "the assembled bundle carries no Godot.NET.Sdk nupkg in GodotSharp/Tools/nupkgs"
BUNDLED_HASH="$($PY -c 'import json,sys; print(json.load(open(sys.argv[1]))["content_hash"])' \
    "$APP/Contents/MacOS/GodotSharp/Dn2Cpp/manifest.json")"
[ "$BUNDLED_HASH" = "$TOOLCHAIN_HASH" ] \
    || die "the bundled toolchain manifest ($BUNDLED_HASH) is not the one staged ($TOOLCHAIN_HASH) — a concurrent re-stage?"

# Finder droppings break codesign --strict (an unsealed resource); they arrive
# through the source trees, so delete rather than assert. `.tools-hash` and
# `Dn2Cpp/.dn2cpp-staged-atomic` stay: the latter is what lets a --smoke run's
# stage_editor_toolchain take the content-match arm and write nothing.
find "$APP" -name '.DS_Store' -delete
find "$APP" -name '._*' -delete

EDITOR_VERSION="$(first_line "$(run_with_watchdog 60 "$APP/Contents/MacOS/Godot" --headless --version)")"
case "$EDITOR_VERSION" in
    "$VERSION_BASE".stable.mono*) ;;
    *) die "the bundled editor reports '$EDITOR_VERSION', which is no $VERSION_BASE.stable.mono build" ;;
esac
echo "editor says:   $EDITOR_VERSION"

# ── 8. Smoke: the real export gates, against this .app ───────────────────────
# Before signing, not after: stage_editor_toolchain may write into the bundle,
# and a write past codesign breaks the seal.
if [ "$SMOKE" -eq 1 ]; then
    echo "== 8/13 smoke (editor-export gates against the bundle) =="
    SMOKE_ROOT="$OUT/smoke-root"
    rm -rf "$SMOKE_ROOT"
    mkdir -p "$SMOKE_ROOT"
    cp -R "$FORK_ROOT/nuget" "$SMOKE_ROOT/nuget"
    for f in pin.txt fork_head.txt clone.txt template.txt \
             macos_template.zip macos_template.zip.provenance web_emcc.txt; do
        [ -f "$FORK_ROOT/$f" ] || die "the fork root has no $f — run gates/setup-godot-fork.sh (and -web)"
        cp "$FORK_ROOT/$f" "$SMOKE_ROOT/$f"
    done
    # The Web template must be the RELEASE one, not the fork root's: the archive
    # a user downloads and this run must agree about the engine inside it.
    WEB_ASSET="$OUT/godot-$VERSION-web-template.zip"
    for sfx in "" .provenance; do
        [ -f "$WEB_ASSET$sfx" ] \
            || die "no $WEB_ASSET$sfx — cut the Web template first: dist/package-web-template.sh --version $VERSION"
        cp "$WEB_ASSET$sfx" "$SMOKE_ROOT/web_template.zip$sfx"
    done
    # editor.txt is the whole point: the gates run the .app's own binary, and
    # derive GodotSharp beside it.
    printf '%s\n' "$(cd "$APP/Contents/MacOS" && pwd)/Godot" > "$SMOKE_ROOT/editor.txt"
    cp "$FORK_EDITOR.engine-hash" "$APP/Contents/MacOS/Godot.engine-hash"

    DN2CPP_GODOT_FORK_ROOT="$SMOKE_ROOT" DN2CPP_GATE_CACHE=0 \
        ./gates/build-and-run-godot-editor-export.sh
    DN2CPP_GODOT_FORK_ROOT="$SMOKE_ROOT" DN2CPP_GATE_CACHE=0 \
        ./gates/build-and-run-godot-editor-export-web.sh
    rm -f "$APP/Contents/MacOS/Godot.engine-hash"
    rm -rf "$SMOKE_ROOT"
else
    echo "== 8/13 smoke skipped (--no-smoke) =="
fi

# ── 9. Ad-hoc signature ──────────────────────────────────────────────────────
# xattr -cr first: com.apple.provenance rides in on every copied binary and
# codesign refuses to seal a bundle carrying it.
#
# Nested Mach-O images are signed individually and BEFORE the bundle, because
# codesign seals the outer bundle over what it finds — `--deep` signing is not
# the tool for that (it is documented as a diagnostic aid, not a packaging step)
# and is used here only to VERIFY. No hardened runtime: it would demand
# entitlements and notarization the release does not have.
echo "== 9/13 ad-hoc signing =="
xattr -cr "$APP"
FILELIST="$STAGE/.files.z"
TYPELIST="$STAGE/.types.txt"
find "$APP" -type f -print0 > "$FILELIST"
LC_ALL=C xargs -0 file -h -F '|' < "$FILELIST" > "$TYPELIST"
NESTED=0
while IFS= read -r line; do
    p="${line%%|*}"
    desc="${line#*|}"
    case "$desc" in *Mach-O*) ;; *) continue ;; esac
    if [ "$p" = "$APP/Contents/MacOS/Godot" ]; then
        continue
    fi
    codesign --force --sign - --timestamp=none "$p"
    NESTED=$((NESTED + 1))
done < "$TYPELIST"
rm -f "$FILELIST" "$TYPELIST"
codesign --force --sign - --timestamp=none "$APP"
codesign --verify --deep --strict --verbose=2 "$APP"
echo "signed:        $NESTED nested Mach-O image(s) + the bundle (ad-hoc)"

# ── 10. Archive ──────────────────────────────────────────────────────────────
# `ditto`, never `zip`: the latter drops the exec bit and the extended attributes
# the signature is sealed over, so the unpacked .app fails to launch or verify.
echo "== 10/13 archiving =="
ASSET="$OUT/Godot-$VERSION-macos-arm64.zip"
rm -f "$ASSET"
ditto -c -k --sequesterRsrc --keepParent "$APP" "$ASSET"

# ── 11. Round-trip ───────────────────────────────────────────────────────────
# The archive is the artifact users get; verifying the .app we still hold proves
# nothing about what survives ditto.
echo "== 11/13 round-trip =="
RT="$(mktemp -d)"
gate_add_exit_hook "rm -rf '$RT'"
ditto -x -k "$ASSET" "$RT"
RT_APP="$RT/$APP_NAME.app"
[ -d "$RT_APP" ] || die "the archive does not unpack to $APP_NAME.app"
codesign --verify --deep --strict --verbose=2 "$RT_APP"
[ -x "$RT_APP/Contents/MacOS/GodotSharp/Dn2Cpp/bin/dn2cpp" ] \
    || die "the unpacked bundle's Dn2Cpp/bin/dn2cpp lost its executable bit"
RT_VERSION="$(first_line "$(run_with_watchdog 60 "$RT_APP/Contents/MacOS/Godot" --headless --version)")"
[ "$RT_VERSION" = "$EDITOR_VERSION" ] \
    || die "the unpacked editor reports '$RT_VERSION', not '$EDITOR_VERSION'"
rm -rf "$RT"
echo "round-trip:    verified, editor reports $RT_VERSION"

# ── 12. Checksum + metadata ──────────────────────────────────────────────────
echo "== 12/13 checksum + metadata =="
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

# The lane name, the metadata file name and the notes' placeholder suffix are one
# thing (dist/release-github.sh's lane table); a bare `editor` would leave the
# reader of a two-editor release deciding which one this is.
cat > "$OUT/editor-macos.metadata" <<EOF
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
prebuilt_axes=${AXES[*]}
bundle_installed_bytes=$BUNDLE_BYTES
emsdk_installed_bytes=$EMSDK_BYTES
buildtools_installed_bytes=$BUILDTOOLS_BYTES
node_installed_bytes=$NODE_BYTES
cmake_version=$BUNDLE_CMAKE
ninja_version=$BUNDLE_NINJA
node_version=$BUNDLE_NODE
codesign=adhoc, no hardened runtime, not notarized
EOF

echo "== 13/13 done =="
echo "OK: $ASSET (adhoc-signed, ditto-archived, sha256 $ASSET_SHA)"

#!/usr/bin/env bash
# dist/package-macos-template.sh — stage a macOS arm64 export template as a
# release asset, thinned out of the official Godot .NET template.
#
# Without it a fresh macOS preset cannot export at all. The backend compiles the
# game for the host architecture and cannot cross-compile, so the preset must
# select ONE architecture; upstream's macos.zip carries only
# `godot_macos_{release,debug}.universal`, and the exporter picks its binary by
# the exact name `godot_macos_<cfg>.<architecture>` with no lipo fallback. So
# `universal` is refused by the backend and `arm64` finds no template binary.
#
# The official template is the right source because the fork's non-Web,
# non-editor engine delta is inert in an export template — and that is CHECKED
# here, not assumed (step 2), as is the identity of the template itself (step
# 5): a template is trusted on its filename by everything downstream.
#
# Usage:
#   dist/package-macos-template.sh --version V [options]
#     --version V   release version string, e.g. 4.7.1-dn2cpp.3.1 (required)
#     --out DIR     asset directory (default: artifacts/release)
#     --src PATH    upstream macos.zip (default: the installed <base>.stable.mono one)
#     -h | --help
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
source "$SCRIPT_DIR/../gates/_common.sh"      # set -euo pipefail, cd to repo root
source "$SCRIPT_DIR/../gates/_godot_fork.sh"

VERSION=
OUT_DIR=artifacts/release
SRC=

while [ $# -gt 0 ]; do
    case "$1" in
        --version) VERSION="$2"; shift 2 ;;
        --out)     OUT_DIR="$2"; shift 2 ;;
        --src)     SRC="$2"; shift 2 ;;
        # Line-numbered against the header above: editing it without moving this
        # truncates --help silently.
        -h|--help) sed -n '2,22p' "$0"; exit 0 ;;
        *) echo "error: unknown argument: $1" >&2; exit 2 ;;
    esac
done

die() { echo "error: $*" >&2; exit 1; }

[ -n "$VERSION" ] || die "--version is required (e.g. --version 4.7.1-dn2cpp.3.1)"
release_version_split "$VERSION" || exit 1
BASE_VER="$RELEASE_BASE_VER"
[ "$DN2CPP_OS" = macos ] || die "lipo is macOS-only; this asset is cut on a macOS host"

echo "== packaging the macOS arm64 export template: $VERSION =="

# ── 1. The fork worktree, and the base it is pinned to ────────────────────────
godot_fork_resolve || exit 1
BASE_COMMIT="$(godot_fork_base_commit)"
echo "fork:  $FORK"
echo "base:  $BASE_COMMIT"

# ── 2. The fork's non-Web/non-editor engine delta is inert on macOS ──────────
# What makes an UPSTREAM binary this fork's template. Export templates compile
# neither platform/web/ nor editor/**. Before normalizing text, reject binary
# deltas and untracked inputs in every remaining engine path: awk cannot prove
# either inert. Every remaining added or removed engine line is normalized by
# deleting its WEB_ENABLED operand; the additions must then cancel the removals
# exactly — i.e. with WEB_ENABLED undefined, these sources are upstream's. A
# text change that survives normalization means the fork now builds a different
# macOS binary and this asset would ship the wrong engine.
MACOS_ENGINE_INPUTS=(
    "${FORK_OWNED[@]}"
    ':(exclude,glob)platform/web/**'
    ':(exclude,glob)editor/**'
)
binary_delta="$(git -C "$FORK" diff --no-ext-diff --no-textconv --no-color --no-renames \
    --numstat "$BASE_COMMIT" -- "${MACOS_ENGINE_INPUTS[@]}" \
    | awk '$1 == "-" && $2 == "-" { print }')"
[ -z "$binary_delta" ] || die "the fork has tracked binary changes outside platform/web/ and editor/**,
       which this macOS export-template check cannot normalize:
$(printf '%s\n' "$binary_delta" | sed 's/^/         /')
       Bake the template from the fork instead of thinning upstream's."
untracked_engine="$(git -C "$FORK" ls-files --others --exclude-standard \
    -- "${MACOS_ENGINE_INPUTS[@]}")"
# SCons reads this ignored root configuration input before building the template.
if { [ -f "$FORK/custom.py" ] || [ -L "$FORK/custom.py" ]; } \
    && ! git -C "$FORK" ls-files --error-unmatch -- custom.py >/dev/null 2>&1; then
    untracked_engine+="${untracked_engine:+$'\n'}custom.py"
fi
[ -z "$untracked_engine" ] || die "the fork has untracked engine inputs outside platform/web/ and editor/**:
$(printf '%s\n' "$untracked_engine" | sed 's/^/         /')
       Bake the template from the fork instead of thinning upstream's."
engine_delta="$(git -C "$FORK" diff --no-ext-diff --no-textconv --no-color --no-renames \
    "$BASE_COMMIT" -- "${MACOS_ENGINE_INPUTS[@]}")"
residue="$(awk '
    /^diff / { inhunk = 0; in_win = 0; saw_win_else = 0 }
    /^@@/    { inhunk = 1; in_win = 0; saw_win_else = 0; next }
    !inhunk  { next }
    /^[+-]/ {
        sign = substr($0, 1, 1); s = substr($0, 2)
        gsub(/[ \t]/, "", s)
        if (s == "" || s ~ /^\/\//) next
        if (sign == "+" && (s ~ /^#ifdefined\(WINDOWS_ENABLED\)/ || s ~ /^#ifdefWINDOWS_ENABLED/)) {
            in_win = 1
            next
        }
        if (in_win) {
            if (s == "#else") {
                in_win = 0
                saw_win_else = 1
                next
            }
            if (s == "#endif") {
                in_win = 0
                next
            }
            next
        }
        if (saw_win_else && s == "#endif") {
            saw_win_else = 0
            next
        }
        gsub(/&&!defined\(WEB_ENABLED\)/, "", s)
        gsub(/!defined\(WEB_ENABLED\)&&/, "", s)
        gsub(/\|\|defined\(WEB_ENABLED\)/, "", s)
        gsub(/defined\(WEB_ENABLED\)\|\|/, "", s)
        n[s] += (sign == "+") ? 1 : -1
    }
    END { for (s in n) if (n[s] != 0) printf "%+d %s\n", n[s], s }
' <<<"$engine_delta" | sort)"
[ -z "$residue" ] || die "the fork's non-Web/non-editor engine sources differ from $BASE_COMMIT in a way a
       macOS export template would see, so an upstream template is no longer this fork's:
$(printf '%s\n' "$residue" | sed 's/^/         /')
       Bake the template from the fork instead of thinning upstream's."
echo "delta: inert on macOS (every non-Web/non-editor engine text change reduces to $BASE_COMMIT)"

# ── 3. The upstream template ──────────────────────────────────────────────────
# Named, never downloaded: the release host installs export templates through
# the editor like everyone else, and a missing one is a one-line remedy rather
# than a URL this script would have to keep true.
TEMPLATE_DIR="$HOME/Library/Application Support/Godot/export_templates/$BASE_VER.stable.mono"
[ -n "$SRC" ] || SRC="$TEMPLATE_DIR/macos.zip"
[ -f "$SRC" ] || die "no upstream macOS export template at
         $SRC
       Install the $BASE_VER .NET templates (Editor → Manage Export Templates),
       or point --src at a downloaded macos.zip from the $BASE_VER release."
SRC_SHA="$(shasum -a 256 "$SRC")"
SRC_SHA="${SRC_SHA%% *}"
echo "src:   $SRC ($SRC_SHA)"

# ── 4. Thin both binaries to arm64 ────────────────────────────────────────────
# In place in the unpacked tree, so everything else the bundle carries — the
# Info.plist the exporter rewrites, PkgInfo, the icon and the privacy manifest —
# ships byte-identical to upstream's.
STAGE="$(mktemp -d)"
gate_add_exit_hook "rm -rf '$STAGE'"
unzip -q "$SRC" -d "$STAGE"
APP="$STAGE/macos_template.app"
MACOS_DIR="$APP/Contents/MacOS"
[ -d "$MACOS_DIR" ] || die "$SRC does not unpack to macos_template.app/Contents/MacOS"

for cfg in release debug; do
    fat="$MACOS_DIR/godot_macos_$cfg.universal"
    [ -f "$fat" ] || die "$SRC carries no godot_macos_$cfg.universal"
    thin="$MACOS_DIR/godot_macos_$cfg.arm64"
    lipo -thin arm64 "$fat" -output "$thin" \
        || die "godot_macos_$cfg.universal holds no arm64 slice"
    chmod +x "$thin"
    rm -f "$fat"
    # lipo changes every signed page while retaining the source slice's code
    # signature. Replace that invalid seal before the template can be exported.
    codesign --force --sign - --timestamp=none "$thin" \
        || die "failed to ad-hoc sign the thinned $cfg template"
    codesign --verify --strict --verbose=2 "$thin" \
        || die "the thinned $cfg template has an invalid code signature"
done

# ── 5. The template says what it is ───────────────────────────────────────────
# `<base>.stable.mono.official.<commit>` — the only self-report a template makes,
# and it answers all three questions at once: the right Godot version, a .NET
# (mono) build rather than the plain one, and the pinned upstream commit. A
# template one patch release off, or a non-.NET one, exports and runs.
for cfg in release debug; do
    ver="$(first_line "$(run_with_watchdog 60 "$MACOS_DIR/godot_macos_$cfg.arm64" --version)")"
    build="${ver#"$BASE_VER".stable.mono.official.}"
    [ "$build" != "$ver" ] \
        || die "the $cfg template reports '$ver', which is no official $BASE_VER .NET build"
    [ "${BASE_COMMIT#"$build"}" != "$BASE_COMMIT" ] \
        || die "the $cfg template was built from $build, not the pinned base $BASE_COMMIT"
    echo "$cfg: $ver"
done

# ── 6. Archive, and check the archive rather than the tree ────────────────────
ASSET="godot-$VERSION-macos-arm64-template.zip"
mkdir -p "$OUT_DIR"
# Absolute from here: the zip is written from inside $STAGE.
OUT_DIR="$(cd "$OUT_DIR" && pwd)"
rm -f "$OUT_DIR/$ASSET"
(cd "$STAGE" && zip -qry "$OUT_DIR/$ASSET" macos_template.app)

# The archive is the artifact, and the exporter reads it by name: a leftover
# `.universal` entry would be dead weight the size of the asset, and a lost exec
# bit an exported game that cannot start.
entries="$(unzip -Z1 "$OUT_DIR/$ASSET" | grep 'Contents/MacOS/godot_' | sort)"
want="$(printf 'macos_template.app/Contents/MacOS/godot_macos_debug.arm64\nmacos_template.app/Contents/MacOS/godot_macos_release.arm64\n')"
[ "$entries" = "$want" ] || die "the archive's template binaries are not the two arm64 ones:
$entries"
RT="$(mktemp -d)"
gate_add_exit_hook "rm -rf '$RT'"
unzip -q "$OUT_DIR/$ASSET" -d "$RT"
for cfg in release debug; do
    bin="$RT/macos_template.app/Contents/MacOS/godot_macos_$cfg.arm64"
    [ -x "$bin" ] || die "the unpacked godot_macos_$cfg.arm64 lost its executable bit"
    archs="$(lipo -archs "$bin")"
    [ "$archs" = arm64 ] || die "the unpacked godot_macos_$cfg.arm64 is '$archs', not arm64"
    codesign --verify --strict --verbose=2 "$bin" \
        || die "the unpacked godot_macos_$cfg.arm64 has an invalid code signature"
    exported="$RT/exported-$cfg"
    cp "$bin" "$exported"
    codesign --verify --strict --verbose=2 "$exported" \
        || die "the renamed godot_macos_$cfg.arm64 signature is bound to its template bundle"
done
rm -rf "$RT"

# ── 7. Checksum, SHA256SUMS row, metadata ─────────────────────────────────────
ASSET_SHA="$(shasum -a 256 "$OUT_DIR/$ASSET")"
ASSET_SHA="${ASSET_SHA%% *}"

# Idempotent: this asset's previous row is dropped before the new one is
# appended, so a re-run replaces rather than accumulates. grep -v exits 1 on an
# all-filtered file, which is not an error here.
SUMS="$OUT_DIR/SHA256SUMS.txt"
if [ -f "$SUMS" ]; then
    grep -v "  $ASSET\$" "$SUMS" > "$SUMS.tmp" || true
    mv -f "$SUMS.tmp" "$SUMS"
fi
printf '%s  %s\n' "$ASSET_SHA" "$ASSET" >> "$SUMS"

# base_pin is what dist/release-github.sh cross-checks against the editor's: one
# release names one engine, and this asset's engine is the pinned base itself.
cat > "$OUT_DIR/macos.metadata" <<EOF
asset=$ASSET
asset_sha256=$ASSET_SHA
architecture=arm64
base_pin=$BASE_COMMIT
upstream_template=$SRC_SHA
release_version=$VERSION
EOF

echo "OK: $OUT_DIR/$ASSET ($ASSET_SHA)"

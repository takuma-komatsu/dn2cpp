#!/usr/bin/env bash
# dist/package-web-template.sh — stage the fork's Web export template as a
# release asset, with everything a downloader cannot check checked here.
#
# Godot validates a preset's custom_template with FileAccess::exists alone, so a
# published template is trusted on its filename: a wrong engine, a stock-vs-CRI
# mix-up and a non-dlink build all export, run, and match every expectation.
# Hence the four assertions below (provenance stamp, emcc stamp, flavor,
# dlink + wasm-EH) and the metadata file beside the asset.
#
# The default SOURCE is the fork ROOT's published web_template.zip, never the
# fork's bin/ zip: both flavors are built to that one bin/ path and
# gates/setup-godot-fork-web.sh publishes a per-flavor copy out of it, so bin/
# holds whichever flavor was baked last.
#
# Usage:
#   dist/package-web-template.sh --version V [options]
#     --version V   release version string, e.g. 4.7.1-dn2cpp.3.1 (required)
#     --out DIR     asset directory (default: artifacts/release)
#     --src PATH    template zip (default: <fork root>/web_template.zip)
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
        -h|--help) sed -n '2,21p' "$0"; exit 0 ;;
        *) echo "error: unknown argument: $1" >&2; exit 2 ;;
    esac
done
[ -n "$VERSION" ] || { echo "error: --version is required" >&2; exit 2; }
release_version_split "$VERSION" || exit 1

# 1. The fork worktree, which is what the provenance stamp is compared against.
godot_fork_resolve || exit 1
BASE_COMMIT="$(godot_fork_base_commit)"
[ -n "$SRC" ] || SRC="$FORK_ROOT/web_template.zip"
[ -f "$SRC" ] || {
    echo "error: no Web export template at $SRC" >&2
    echo "       Bake one: gates/setup-godot-fork-web.sh" >&2
    exit 1
}
echo "== packaging the Web export template: $VERSION =="
echo "fork:  $FORK"
echo "src:   $SRC"

# 2. Provenance: the zip must have been baked from the engine sources now in the
#    fork. A missing stamp is a refusal — see godot_fork_template_check.
godot_fork_template_check "$SRC" "Web export template" "gates/setup-godot-fork-web.sh"
ENGINE_PROVENANCE="$(head -1 "$SRC.provenance")"

# 3. The emcc that linked it — the only record of the EH flavour, WASM_BIGINT
#    and dylink ABI a drop-in has to match, so an unstamped asset is unusable
#    even when it works today. Either stamp spelling is accepted:
#    gates/setup-godot-fork-web.sh writes <zip>.emcc beside the zip it builds and
#    publishes it as web_emcc.txt beside the flavored copy, and a --src naming
#    the built zip must reach the flavor refusal below rather than die here.
EMCC_STAMP="$SRC.emcc"
[ -f "$EMCC_STAMP" ] || EMCC_STAMP="$(dirname "$SRC")/web_emcc.txt"
[ -f "$EMCC_STAMP" ] || {
    echo "error: no emcc stamp beside the template ($EMCC_STAMP)" >&2
    echo "       Re-publish it: gates/setup-godot-fork-web.sh" >&2
    exit 1
}
EMCC_VERSION="$(head -1 "$EMCC_STAMP")"
# The SDK release the stamp names, spelled as the toolchain bundle's manifest
# spells it (emscripten-version.txt minus its `-git` suffix) so a reader can
# compare the two files directly. Taken from the stamp and not from the pin: the
# pin says what the repo asks for, the stamp says what linked these bytes.
EMSDK_VERSION="$(awk '{ for (i = 1; i <= NF; i++) if ($i ~ /^[0-9]+\.[0-9]+\.[0-9]+(-git)?$/) v = $i }
                     END { print v }' <<<"$EMCC_VERSION")"
EMSDK_VERSION="${EMSDK_VERSION%-git}"
[ -n "$EMSDK_VERSION" ] || {
    echo "error: the emcc stamp names no SDK release: $EMCC_VERSION" >&2
    echo "       Re-publish the template: gates/setup-godot-fork-web.sh" >&2
    exit 1
}
echo "emcc:  $EMCC_VERSION"
echo "emsdk: $EMSDK_VERSION"

# 4. Flavor. The CRI variant carries a third party's SDK and must never ship as
#    the general-purpose template.
FLAVOR="$(godot_fork_web_template_flavor "$SRC")"
if [ "$FLAVOR" != stock ]; then
    echo "FAIL: $SRC is a $FLAVOR-flavor template; a release asset must be stock" >&2
    echo "      Both flavors are built to ONE zip in the fork's bin/, so that path is" >&2
    echo "      whichever was baked last — it is never a valid --src." >&2
    echo "      Either rerun gates/setup-godot-fork-web.sh without CRI=1, or point" >&2
    echo "      --src at the fork root's published web_template.zip." >&2
    exit 1
fi
echo "flavor: stock"

# 5. dlink + the wasm exception tag, on the bytes actually being shipped.
godot_fork_web_template_assert "$SRC"

# 6. Stage the asset and its provenance stamp side by side.
ASSET="godot-$VERSION-web-template.zip"
mkdir -p "$OUT_DIR"
# Absolute from here: the editor resolves a relative OUT_DIR against the project.
OUT_DIR="$(cd "$OUT_DIR" && pwd)"
cp -f "$SRC" "$OUT_DIR/$ASSET"
cp -f "$SRC.provenance" "$OUT_DIR/$ASSET.provenance"
ASSET_SHA="$(shasum -a 256 "$OUT_DIR/$ASSET")"
ASSET_SHA="${ASSET_SHA%% *}"

# 7. SHA256SUMS.txt, idempotent: this asset's previous line is dropped before the
#    new one is appended, so a re-run replaces rather than accumulates. grep -v
#    exits 1 on an all-filtered file, which is not an error here.
SUMS="$OUT_DIR/SHA256SUMS.txt"
if [ -f "$SUMS" ]; then
    grep -v "  $ASSET\$" "$SUMS" > "$SUMS.tmp" || true
    mv -f "$SUMS.tmp" "$SUMS"
fi
printf '%s  %s\n' "$ASSET_SHA" "$ASSET" >> "$SUMS"

# 8. What a consumer needs to reproduce or validate the asset without unzipping
#    it. The engine provenance is the stamp verbatim, so it compares equal to
#    what godot_fork_engine_provenance prints in the fork.
cat > "$OUT_DIR/web.metadata" <<EOF
asset=$ASSET
asset_sha256=$ASSET_SHA
flavor=stock
emcc=$EMCC_VERSION
emsdk_version=$EMSDK_VERSION
engine_provenance=$ENGINE_PROVENANCE
release_version=$VERSION
EOF

echo "OK: $OUT_DIR/$ASSET ($ASSET_SHA)"

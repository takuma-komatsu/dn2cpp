#!/usr/bin/env bash
# dist/package-web-template.sh — package the fork's Release and Debug Web
# export templates as one deterministic release asset.
#
# Usage:
#   dist/package-web-template.sh --version V [options]
#     --version V       release version string (required)
#     --out DIR         asset directory (default: artifacts/release)
#     --release-src P   Release template (default: <fork>/web_template.zip)
#     --debug-src P     Debug template (default: <fork>/web_template_debug.zip)
#     -h | --help
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
source "$SCRIPT_DIR/../gates/_common.sh"
source "$SCRIPT_DIR/../gates/_godot_fork.sh"

VERSION=
OUT_DIR=artifacts/release
RELEASE_SRC=
DEBUG_SRC=
while [ $# -gt 0 ]; do
    case "$1" in
        --version)     VERSION="$2"; shift 2 ;;
        --out)         OUT_DIR="$2"; shift 2 ;;
        --release-src) RELEASE_SRC="$2"; shift 2 ;;
        --debug-src)   DEBUG_SRC="$2"; shift 2 ;;
        -h|--help)     awk 'NR > 1 && !/^#/ { exit } NR > 1 { print }' "$0"; exit 0 ;;
        *) echo "error: unknown argument: $1" >&2; exit 2 ;;
    esac
done
[ -n "$VERSION" ] || { echo "error: --version is required" >&2; exit 2; }
release_version_split "$VERSION" || exit 1

godot_fork_resolve || exit 1
BASE_COMMIT="$(godot_fork_base_commit)"
[ -n "$RELEASE_SRC" ] || RELEASE_SRC="$FORK_ROOT/web_template.zip"
[ -n "$DEBUG_SRC" ] || DEBUG_SRC="$FORK_ROOT/web_template_debug.zip"
for src in "$RELEASE_SRC" "$DEBUG_SRC"; do
    [ -f "$src" ] || {
        echo "error: no Web export template at $src" >&2
        echo "       Bake the Release/Debug pair: gates/setup-godot-fork-web.sh" >&2
        exit 1
    }
done

stamp_value() {
    local src="$1" suffix="$2" fallback="$3" stamp
    stamp="$src.$suffix"
    [ -f "$stamp" ] || stamp="$(dirname "$src")/$fallback"
    [ -f "$stamp" ] || return 1
    head -1 "$stamp"
}

validate_template() {
    local label="$1" src="$2" emcc_fallback="$3" flavor
    godot_fork_template_check "$src" "$label Web export template" \
        "gates/setup-godot-fork-web.sh"
    flavor="$(godot_fork_web_template_flavor "$src")"
    [ "$flavor" = stock ] || {
        echo "FAIL: $src is a $flavor-flavor template; a release bundle must be stock" >&2
        exit 1
    }
    godot_fork_web_template_assert "$src"
    stamp_value "$src" emcc "$emcc_fallback" >/dev/null || {
        echo "error: no emcc stamp for the $label Web template at $src" >&2
        echo "       Re-publish the pair: gates/setup-godot-fork-web.sh" >&2
        exit 1
    }
}

echo "== packaging the Web Release/Debug template bundle: $VERSION =="
validate_template Release "$RELEASE_SRC" web_emcc.txt
validate_template Debug "$DEBUG_SRC" web_emcc_debug.txt

RELEASE_PROVENANCE="$(stamp_value "$RELEASE_SRC" provenance web_template.zip.provenance)"
DEBUG_PROVENANCE="$(stamp_value "$DEBUG_SRC" provenance web_template_debug.zip.provenance)"
[ -n "$RELEASE_PROVENANCE" ] && [ "$RELEASE_PROVENANCE" = "$DEBUG_PROVENANCE" ] || {
    echo "error: the Release and Debug Web templates disagree on engine provenance" >&2
    echo "       release: ${RELEASE_PROVENANCE:-<missing>}" >&2
    echo "       debug:   ${DEBUG_PROVENANCE:-<missing>}" >&2
    exit 1
}
RELEASE_EMCC="$(stamp_value "$RELEASE_SRC" emcc web_emcc.txt)"
DEBUG_EMCC="$(stamp_value "$DEBUG_SRC" emcc web_emcc_debug.txt)"
[ -n "$RELEASE_EMCC" ] && [ "$RELEASE_EMCC" = "$DEBUG_EMCC" ] || {
    echo "error: the Release and Debug Web templates were linked by different emcc" >&2
    echo "       release: ${RELEASE_EMCC:-<missing>}" >&2
    echo "       debug:   ${DEBUG_EMCC:-<missing>}" >&2
    exit 1
}

EMSDK_VERSION="$(awk '{ for (i = 1; i <= NF; i++) if ($i ~ /^[0-9]+\.[0-9]+\.[0-9]+(-git)?$/) v = $i } END { print v }' <<<"$RELEASE_EMCC")"
EMSDK_VERSION="${EMSDK_VERSION%-git}"
[ -n "$EMSDK_VERSION" ] || {
    echo "error: the emcc stamp names no SDK release: $RELEASE_EMCC" >&2
    exit 1
}

RELEASE_NAME=godot_web_release.zip
DEBUG_NAME=godot_web_debug.zip
RELEASE_SHA="$(shasum -a 256 "$RELEASE_SRC" | awk '{print $1}')"
DEBUG_SHA="$(shasum -a 256 "$DEBUG_SRC" | awk '{print $1}')"
[ "$RELEASE_SHA" != "$DEBUG_SHA" ] || {
    echo "error: the Release and Debug Web templates have identical bytes ($RELEASE_SHA)" >&2
    exit 1
}

mkdir -p "$OUT_DIR"
OUT_DIR="$(cd "$OUT_DIR" && pwd)"
ASSET="godot-$VERSION-web-templates.zip"
ASSET_PATH="$OUT_DIR/$ASSET"
ASSET_TMP="$(mktemp "$OUT_DIR/.$ASSET.tmp.XXXXXX")"
trap 'rm -f "$ASSET_TMP"' EXIT
PY="$(resolve_python)" || { echo "error: Python 3 is required to package Web templates" >&2; exit 1; }
# Fixed timestamp, mode and entry order make the outer archive reproducible.
# shellcheck disable=SC2086 -- resolve_python may answer `py -3`.
$PY - "$ASSET_TMP" "$RELEASE_SRC" "$RELEASE_NAME" "$DEBUG_SRC" "$DEBUG_NAME" <<'PY'
import sys
import zipfile

archive, release_path, release_name, debug_path, debug_name = sys.argv[1:]
with zipfile.ZipFile(archive, "w", compression=zipfile.ZIP_STORED) as output:
    for path, name in ((release_path, release_name), (debug_path, debug_name)):
        info = zipfile.ZipInfo(name, (1980, 1, 1, 0, 0, 0))
        info.create_system = 3
        info.external_attr = 0o100644 << 16
        with open(path, "rb") as source:
            output.writestr(info, source.read(), compress_type=zipfile.ZIP_STORED)
PY
# A failed or interrupted writer leaves only the hidden temporary file; the
# public path changes in one rename after both inner ZIPs are complete.
mv -f "$ASSET_TMP" "$ASSET_PATH"

printf '%s\n' "$RELEASE_PROVENANCE" > "$ASSET_PATH.provenance"
ASSET_SHA="$(shasum -a 256 "$ASSET_PATH" | awk '{print $1}')"
SUMS="$OUT_DIR/SHA256SUMS.txt"
if [ -f "$SUMS" ]; then
    grep -vE "  godot-$VERSION-web-template(s)?\.zip$" "$SUMS" > "$SUMS.tmp" || true
    mv -f "$SUMS.tmp" "$SUMS"
fi
printf '%s  %s\n' "$ASSET_SHA" "$ASSET" >> "$SUMS"

cat > "$OUT_DIR/web.metadata" <<EOF
asset=$ASSET
asset_sha256=$ASSET_SHA
flavor=stock
emcc=$RELEASE_EMCC
emsdk_version=$EMSDK_VERSION
engine_provenance=$RELEASE_PROVENANCE
release_template=$RELEASE_NAME
release_template_sha256=$RELEASE_SHA
debug_template=$DEBUG_NAME
debug_template_sha256=$DEBUG_SHA
release_version=$VERSION
EOF

# Retire a stale singular asset only after the complete pair is published.
rm -f "$OUT_DIR/godot-$VERSION-web-template.zip" \
      "$OUT_DIR/godot-$VERSION-web-template.zip.provenance"
echo "OK: $ASSET_PATH ($ASSET_SHA)"

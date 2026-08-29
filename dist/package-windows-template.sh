#!/usr/bin/env bash
# Package the fork-built Windows release and debug export templates together.
# The fork's loader change is engine code, so upstream templates cannot load a
# dn2cpp drop-in whose project-native dependency is staged beside it. Keeping
# both SCons configurations in one lane avoids a debug export silently running
# the release engine while preserving the release system's one-asset contract.
#
# Usage:
#   dist/package-windows-template.sh --version V [options]
#     --version V       release version (required)
#     --out DIR         asset directory (default: artifacts/release)
#     --release-src P   release executable (default: fork setup artifact)
#     --debug-src P     debug executable (default: fork setup artifact)
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
        --version) VERSION="$2"; shift 2 ;;
        --out) OUT_DIR="$2"; shift 2 ;;
        --release-src) RELEASE_SRC="$2"; shift 2 ;;
        --debug-src) DEBUG_SRC="$2"; shift 2 ;;
        -h|--help) awk 'NR > 1 && !/^#/ { exit } NR > 1 { print }' "$0"; exit 0 ;;
        *) echo "error: unknown argument: $1" >&2; exit 2 ;;
    esac
done

die() { echo "error: $*" >&2; exit 1; }

[ -n "$VERSION" ] || die "--version is required"
release_version_split "$VERSION" || exit 1
BASE_VER="$RELEASE_BASE_VER"
[ "$DN2CPP_OS" = windows ] || die "the Windows template asset is cut on a Windows host"
[ "$(godot_fork_host_arch)" = x86_64 ] || die "the Windows template asset requires an x86_64 host"

echo "== packaging the Windows x86_64 export templates: $VERSION =="
godot_fork_resolve || exit 1
BASE_COMMIT="$(godot_fork_base_commit)"
[ -n "$RELEASE_SRC" ] || RELEASE_SRC="$(godot_fork_desktop_template "$FORK_ROOT" release)"
[ -n "$DEBUG_SRC" ] || DEBUG_SRC="$(godot_fork_desktop_template "$FORK_ROOT" debug)"
[ -f "$RELEASE_SRC" ] || die "no fork-built Windows release template at $RELEASE_SRC
       Run gates/setup-godot-fork.sh first."
[ -f "$DEBUG_SRC" ] || die "no fork-built Windows debug template at $DEBUG_SRC
       Run gates/setup-godot-fork.sh first."

echo "fork:     $FORK"
echo "base:     $BASE_COMMIT"
echo "release:  $RELEASE_SRC"
echo "debug:    $DEBUG_SRC"

godot_fork_template_check "$RELEASE_SRC" "Windows release export template" "gates/setup-godot-fork.sh"
godot_fork_template_check "$DEBUG_SRC" "Windows debug export template" "gates/setup-godot-fork.sh"
ENGINE_PROVENANCE="$(head -1 "$RELEASE_SRC.provenance")"
DEBUG_PROVENANCE="$(head -1 "$DEBUG_SRC.provenance")"
[ "$DEBUG_PROVENANCE" = "$ENGINE_PROVENANCE" ] \
    || die "the release and debug templates have different engine provenance"

release_reported="$(first_line "$(run_with_watchdog 60 "$RELEASE_SRC" --version)")"
debug_reported="$(first_line "$(run_with_watchdog 60 "$DEBUG_SRC" --version)")"
case "$release_reported" in
    "$BASE_VER.stable.mono.custom_build."*) ;;
    *) die "the release template reports '$release_reported', not a fork-built $BASE_VER .NET template" ;;
esac
case "$debug_reported" in
    "$BASE_VER.stable.mono.custom_build."*) ;;
    *) die "the debug template reports '$debug_reported', not a fork-built $BASE_VER .NET template" ;;
esac

RELEASE_SHA="$(shasum -a 256 "$RELEASE_SRC")"
RELEASE_SHA="${RELEASE_SHA%% *}"
DEBUG_SHA="$(shasum -a 256 "$DEBUG_SRC")"
DEBUG_SHA="${DEBUG_SHA%% *}"
[ "$RELEASE_SHA" != "$DEBUG_SHA" ] || die "the release and debug templates are identical ($RELEASE_SHA)"
echo "release template: $release_reported ($RELEASE_SHA)"
echo "debug template:   $debug_reported ($DEBUG_SHA)"

RELEASE_NAME=godot_windows_release_x86_64.exe
DEBUG_NAME=godot_windows_debug_x86_64.exe
ASSET="godot-$VERSION-windows-x86_64-templates.zip"
LEGACY_ASSET="godot-$VERSION-windows-x86_64-template.exe"
mkdir -p "$OUT_DIR"
OUT_DIR="$(cd "$OUT_DIR" && pwd)"
ASSET_PATH="$OUT_DIR/$ASSET"
ASSET_TMP="$(mktemp "$OUT_DIR/.$ASSET.tmp.XXXXXX")"
trap 'rm -f "$ASSET_TMP"' EXIT
PY="$(resolve_python)"

# zipfile is available with Python on Git Bash; zip and 7z are not guaranteed.
# shellcheck disable=SC2086
$PY - "$ASSET_TMP" "$RELEASE_SRC" "$RELEASE_NAME" "$DEBUG_SRC" "$DEBUG_NAME" <<'PY'
import shutil
import sys
import zipfile

asset, release_src, release_name, debug_src, debug_name = sys.argv[1:]
date_time = (1980, 1, 1, 0, 0, 0)
with zipfile.ZipFile(asset, "w", zipfile.ZIP_DEFLATED, allowZip64=True, compresslevel=6) as z:
    for path, name in ((release_src, release_name), (debug_src, debug_name)):
        info = zipfile.ZipInfo(name, date_time)
        info.compress_type = zipfile.ZIP_DEFLATED
        info.external_attr = 0o644 << 16
        with open(path, "rb") as src, z.open(info, "w") as dst:
            shutil.copyfileobj(src, dst)
PY

# Verify the entries and bytes before the archive becomes publicly visible.
# shellcheck disable=SC2086
$PY - "$ASSET_TMP" "$RELEASE_NAME" "$RELEASE_SHA" "$DEBUG_NAME" "$DEBUG_SHA" <<'PY'
import hashlib
import sys
import zipfile

asset, release_name, release_sha, debug_name, debug_sha = sys.argv[1:]
expected = {release_name: release_sha, debug_name: debug_sha}
with zipfile.ZipFile(asset) as z:
    if z.namelist() != [release_name, debug_name]:
        sys.exit("error: unexpected Windows template archive entries: " + repr(z.namelist()))
    for name, wanted in expected.items():
        actual = hashlib.sha256(z.read(name)).hexdigest()
        if actual != wanted:
            sys.exit("error: %s hashes to %s, expected %s" % (name, actual, wanted))
PY

# Publish only a complete, verified archive. The temporary file lives beside
# the public path so the rename is atomic and a failed rebuild preserves it.
mv -f "$ASSET_TMP" "$ASSET_PATH"

# Retire the release-only asset only after its replacement is complete. A failed
# archive write must leave the previous package recoverable for diagnosis.
rm -f "$OUT_DIR/$LEGACY_ASSET"

ASSET_SHA="$(shasum -a 256 "$ASSET_PATH")"
ASSET_SHA="${ASSET_SHA%% *}"
SUMS="$OUT_DIR/SHA256SUMS.txt"
if [ -f "$SUMS" ]; then
    grep -v -e "  $ASSET\$" -e "  $LEGACY_ASSET\$" "$SUMS" > "$SUMS.tmp" || true
    mv -f "$SUMS.tmp" "$SUMS"
fi
printf '%s  %s\n' "$ASSET_SHA" "$ASSET" >> "$SUMS"

cat > "$OUT_DIR/windows.metadata" <<EOF
asset=$ASSET
asset_sha256=$ASSET_SHA
architecture=x86_64
base_pin=$BASE_COMMIT
engine_provenance=$ENGINE_PROVENANCE
release_template=$RELEASE_NAME
release_template_sha256=$RELEASE_SHA
release_template_version_string=$release_reported
debug_template=$DEBUG_NAME
debug_template_sha256=$DEBUG_SHA
debug_template_version_string=$debug_reported
release_version=$VERSION
EOF

echo "OK: $ASSET_PATH ($ASSET_SHA)"

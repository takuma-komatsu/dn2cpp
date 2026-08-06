#!/usr/bin/env bash
# SETUP AID (NOT a regression gate — the name is outside the `build-and-run-*.sh`
# glob, so run-all-gates.sh ignores it). Unpacks the pinned Emscripten SDK from
# the official emscripten-releases archive into a local directory.
#
# The archive is the same one `emsdk install` fetches, and it holds one top-level
# directory, which is stripped; what lands is a full SDK (bin/ = clang + wasm-ld
# + binaryen, emscripten/ = emcc and its libraries). On Windows the archive is a
# zip, and the pinned portable CPython emcc.exe needs is staged beside it as
# python/. The pinned Node.js is staged the same way as node/, from a pin of its
# own: emcc runs one on every link, and an SDK without one takes the host's.
#
# The pin file is the single source of version, release hash and sha256. The
# hash is verified before the download is renamed into the cache, so a truncated
# transfer or a mirror's error page can never be read as a complete archive on a
# later run.
#
# Idempotent: a tree already unpacked from the pinned archive is left alone;
# --force re-unpacks.
#
# Usage:
#   ./gates/setup-emsdk.sh [--out DIR] [--pin FILE] [--force]
set -euo pipefail
# Sourcing cd's to the repo root, which the default --out/--pin paths are
# relative to; file_hash is the shared sha256 helper.
source "$(dirname "$0")/_common.sh"

PIN="$EMSDK_PIN"
OUT=
FORCE=0
while [ $# -gt 0 ]; do
    case "$1" in
        --out)   OUT="${2:?--out needs a directory}"; shift 2 ;;
        --pin)   PIN="${2:?--pin needs a file}"; shift 2 ;;
        --force) FORCE=1; shift ;;
        *)       echo "usage: $0 [--out DIR] [--pin FILE] [--force]" >&2; exit 1 ;;
    esac
done
[ -f "$PIN" ] || { echo "error: pin file not found: $PIN" >&2; exit 1; }
[ -f "$NODE_PIN" ] || { echo "error: pin file not found: $NODE_PIN" >&2; exit 1; }

VERSION="$(pin_field "$PIN" version)"
RELEASE_HASH="$(pin_field "$PIN" release_hash)"
BASE_URL="$(pin_field "$PIN" base_url)"
[ -n "$VERSION" ] && [ -n "$RELEASE_HASH" ] && [ -n "$BASE_URL" ] \
    || { echo "error: $PIN lacks version / release_hash / base_url" >&2; exit 1; }

# The Windows zip is unpacked together with the pinned portable python it needs
# staged beside it — a tree without one leaves emcc.exe hunting the host's.
HOST="$(dn2cpp_host_tag)" || HOST=
case "$HOST" in
    macos-arm64|linux-x64|windows-x64) ;;
    *) echo "error: no emsdk arm for $DN2CPP_OS/$(uname -m) (this aid unpacks macos-arm64, linux-x64 and windows-x64)" >&2
       exit 1 ;;
esac

read -r ARCHIVE_PATH ARCHIVE_SHA <<<"$(awk -v h="$HOST" \
    '$1 == "archive" && $2 == h { p = $3; s = $4 } END { print p, s }' "$PIN")"
[ -n "$ARCHIVE_PATH" ] && [ -n "$ARCHIVE_SHA" ] \
    || { echo "error: $PIN has no archive row for $HOST" >&2; exit 1; }
ARCHIVE_PATH="${ARCHIVE_PATH//%h/$RELEASE_HASH}"
case "$HOST:$ARCHIVE_PATH" in
    windows-x64:*.zip) ;;
    windows-x64:*) echo "error: $HOST archive is not a .zip: $ARCHIVE_PATH" >&2; exit 1 ;;
    *:*.tar.xz) ;;
    *) echo "error: $HOST archive is not a .tar.xz: $ARCHIVE_PATH" >&2; exit 1 ;;
esac

# node has its own pin and therefore its own row grammar — full URL, plus the
# strip depth, which is read rather than assumed for the reason unzip_strip
# refuses to infer one.
NODE_VERSION="$(pin_field "$NODE_PIN" version)"
read -r NODE_URL NODE_SHA NODE_STRIP <<<"$(awk -v h="$HOST" \
    '$1 == "archive" && $2 == h { u = $3; s = $4; n = $5 } END { print u, s, n }' "$NODE_PIN")"
[ -n "$NODE_VERSION" ] && [ -n "$NODE_URL" ] && [ -n "$NODE_SHA" ] && [ -n "$NODE_STRIP" ] \
    || { echo "error: $NODE_PIN lacks version or a complete archive row for $HOST" >&2; exit 1; }
case "$HOST:$NODE_URL" in
    windows-x64:*.zip) ;;
    windows-x64:*) echo "error: $HOST node archive is not a .zip: $NODE_URL" >&2; exit 1 ;;
    *:*.tar.gz) ;;
    *) echo "error: $HOST node archive is not a .tar.gz: $NODE_URL" >&2; exit 1 ;;
esac

PYTHON_PATH= PYTHON_SHA=
if [ "$HOST" = windows-x64 ]; then
    read -r PYTHON_PATH PYTHON_SHA <<<"$(awk -v h="$HOST" \
        '$1 == "python" && $2 == h { p = $3; s = $4 } END { print p, s }' "$PIN")"
    [ -n "$PYTHON_PATH" ] && [ -n "$PYTHON_SHA" ] \
        || { echo "error: $PIN has no python row for $HOST" >&2; exit 1; }
fi

OUT="${OUT:-artifacts/emsdk/$VERSION-$HOST}"
DL="artifacts/emsdk/dl"
CACHED="$DL/$HOST-$(basename "$ARCHIVE_PATH")"
# Beside the tree, never inside it: an interrupted unpack leaves an unstamped
# directory, which is stale rather than current.
STAMP="$OUT.pin"
# The stamp names every pinned input the tree was built from — node's, and on
# Windows python's — so swapping any archive invalidates the tree. The version
# stays the first token: dist/package-toolchain.sh reads the rest as one sha term
# and carries it into the bundle stamp.
STAMP_TEXT="$VERSION $ARCHIVE_SHA node-$NODE_VERSION-$NODE_SHA"
[ "$HOST" = windows-x64 ] && STAMP_TEXT="$STAMP_TEXT $PYTHON_SHA"

echo "emsdk:   $VERSION ($RELEASE_HASH)"
echo "host:    $HOST"
echo "out:     $OUT"

if [ "$FORCE" != 1 ] && [ "$(file_text "$STAMP")" = "$STAMP_TEXT" ]; then
    echo "skip: already unpacked from the pinned archive"
    exit 0
fi

echo "== 1/3 the pinned archives =="
fetch_pinned "$BASE_URL/$ARCHIVE_PATH" "$ARCHIVE_SHA" "$CACHED"
NODE_CACHED="$DL/$HOST-$(basename "$NODE_URL")"
fetch_pinned "$NODE_URL" "$NODE_SHA" "$NODE_CACHED"
PYTHON_CACHED=
if [ "$HOST" = windows-x64 ]; then
    PYTHON_CACHED="$DL/$HOST-$(basename "$PYTHON_PATH")"
    fetch_pinned "$BASE_URL/$PYTHON_PATH" "$PYTHON_SHA" "$PYTHON_CACHED"
fi

echo "== 2/3 unpacking =="
rm -f "$STAMP"
rm -rf "$OUT.part"
mkdir -p "$OUT.part"
if [ "$HOST" = windows-x64 ]; then
    # unzip_strip drops the archive's execute bits, which only works here: MSYS
    # synthesizes executability from the PE header and the .exe extension.
    unzip_strip "$CACHED" "$OUT.part" 1
    # The python zip has no top-level directory; it lands whole under python/.
    mkdir -p "$OUT.part/python"
    unzip_strip "$PYTHON_CACHED" "$OUT.part/python" 0
else
    tar -xJf "$CACHED" -C "$OUT.part" --strip-components=1
fi
# node lands under node/, the layout emsdk_node names. No chmod on POSIX: tar
# keeps the archive's mode, and on Windows the lost bit is decoration.
mkdir -p "$OUT.part/node"
if [ "$HOST" = windows-x64 ]; then
    unzip_strip "$NODE_CACHED" "$OUT.part/node" "$NODE_STRIP"
else
    tar -xzf "$NODE_CACHED" -C "$OUT.part/node" --strip-components="$NODE_STRIP"
fi

echo "== 3/3 asserting the unpacked SDK =="
ver_file="$OUT.part/emscripten/emscripten-version.txt"
[ -f "$ver_file" ] || { echo "error: no $ver_file in the archive" >&2; exit 1; }
got_version="$(LC_ALL=C tr -d '" \r\n' < "$ver_file")"
# A release build still carries the source tree's in-development suffix
# (6.0.5-git); the pin names the release tag that hash is tagged with.
got_version="${got_version%-git}"
[ "$got_version" = "$VERSION" ] || {
    echo "error: unpacked emscripten is $got_version, pinned $VERSION" >&2
    exit 1; }
for f in bin/clang bin/wasm-ld emscripten/emcc; do
    [ -x "$OUT.part/$f" ] || { echo "error: $f missing from the unpacked SDK" >&2; exit 1; }
done
if [ "$HOST" = windows-x64 ]; then
    "$OUT.part/python/python.exe" -E -c 'print(1)' >/dev/null \
        || { echo "error: the staged python/python.exe does not run" >&2; exit 1; }
fi
node_exe="$(emsdk_node "$OUT.part")"
[ -x "$node_exe" ] || { echo "error: no executable $node_exe (wrong strip depth?)" >&2; exit 1; }
got_node="$("$node_exe" --version)"
[ "$got_node" = "v$NODE_VERSION" ] || {
    echo "error: staged node is $got_node, pinned v$NODE_VERSION" >&2; exit 1; }

# The archive carries no config, and without one emcc finds clang and wasm-opt
# only off PATH — which would mean putting a bin/ full of host-tool names
# (clang++, llvm-ar, lld) ahead of the host's own. Every path here is relative to
# $CFGDIR, the config's own directory, so the tree stays movable. FROZEN_CACHE is
# deliberately absent: a full SDK builds its sysroot libraries into CACHE on
# demand.
cat > "$OUT.part/emscripten/.emscripten" <<'EOF'
LLVM_ROOT     = '$CFGDIR/../bin'
BINARYEN_ROOT = '$CFGDIR/..'
CACHE         = '$CFGDIR/cache'
EOF
# Appended rather than written above, where nothing expands: the value is host
# shaped. Without NODE_JS emcc runs whatever node PATH offers, which is the one
# version this repository promises nobody.
printf 'NODE_JS       = %s\n' "$(emsdk_node_cfg)" >> "$OUT.part/emscripten/.emscripten"

rm -rf "$OUT"
mv "$OUT.part" "$OUT"
printf '%s\n' "$STAMP_TEXT" > "$STAMP"

echo
echo "OK: $OUT ($(du -sh "$OUT" | awk '{print $1}'), node $NODE_VERSION)"
echo "    the gates find it here on their own (dn2cpp_emsdk_resolve, gates/_common.sh);"
echo "    DN2CPP_EMSDK=<dir> points them at a different SDK"

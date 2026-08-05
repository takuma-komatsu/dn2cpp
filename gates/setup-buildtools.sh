#!/usr/bin/env bash
# SETUP AID (NOT a regression gate — the name is outside the `build-and-run-*.sh`
# glob, so run-all-gates.sh ignores it). Unpacks the pinned cmake and ninja into
# one local directory laid out as a toolchain bundle's `buildtools/`:
#
#   <out>/cmake/bin/cmake[.exe]   <out>/cmake/share/cmake-<M>.<m>/...
#   <out>/ninja/ninja[.exe]
#
# so a later lane stages the tree verbatim. Nothing is trimmed here; the trim
# needs the untrimmed tree to trim from.
#
# The pin file is the single source of version, URL, sha256 and archive root
# depth. The hash is verified before the download is renamed into the cache, so a
# truncated transfer or a mirror's error page can never be read as a complete
# archive on a later run.
#
# Idempotent: a tree already unpacked from the pinned archives is left alone;
# --force re-unpacks.
#
# Usage:
#   ./gates/setup-buildtools.sh [--out DIR] [--pin FILE] [--force]
set -euo pipefail
# Sourcing cd's to the repo root, which the default --out/--pin paths are
# relative to.
source "$(dirname "$0")/_common.sh"

PIN="$BUILDTOOLS_PIN"
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

CMAKE_VERSION="$(pin_field "$PIN" cmake_version)"
NINJA_VERSION="$(pin_field "$PIN" ninja_version)"
[ -n "$CMAKE_VERSION" ] && [ -n "$NINJA_VERSION" ] \
    || { echo "error: $PIN lacks cmake_version / ninja_version" >&2; exit 1; }

HOST="$(dn2cpp_host_tag)" || HOST=
case "$HOST" in
    macos-arm64|linux-x64|windows-x64) ;;
    *) echo "error: no buildtools arm for $DN2CPP_OS/$(uname -m) (this aid unpacks macos-arm64, linux-x64 and windows-x64)" >&2
       exit 1 ;;
esac

# pin_row TOOL — `<url> <sha256> <strip>` for this host, or nothing.
pin_row() {
    awk -v t="$1" -v h="$HOST" \
        '$1 == "archive" && $2 == t && $3 == h { u = $4; s = $5; n = $6 }
         END { print u, s, n }' "$PIN"
}
read -r CMAKE_URL CMAKE_SHA CMAKE_STRIP <<<"$(pin_row cmake)"
read -r NINJA_URL NINJA_SHA NINJA_STRIP <<<"$(pin_row ninja)"
[ -n "$CMAKE_URL" ] && [ -n "$CMAKE_SHA" ] && [ -n "$CMAKE_STRIP" ] \
    || { echo "error: $PIN has no complete cmake archive row for $HOST" >&2; exit 1; }
[ -n "$NINJA_URL" ] && [ -n "$NINJA_SHA" ] && [ -n "$NINJA_STRIP" ] \
    || { echo "error: $PIN has no complete ninja archive row for $HOST" >&2; exit 1; }

# A cmake in a forbidden band builds every SHARED library as a static archive
# under Emscripten, so the Web export produces no drop-in at all. The bands are
# the fork exporter's own (CMakeVersionsWithoutWasmSharedLibs) and are checked
# HERE because the failure would otherwise land on a user's machine, in a
# bundle whose exporter refuses the cmake shipped beside it.
while read -r first past_last; do
    [ -n "$first" ] || continue
    awk -v v="$CMAKE_VERSION" -v a="$first" -v b="$past_last" '
        function key(s,   p, i, k) { split(s, p, "."); k = 0
            for (i = 1; i <= 3; i++) k = k * 100000 + (p[i] + 0); return k }
        BEGIN { exit !(key(v) >= key(a) && key(v) < key(b)) }' \
        && { echo "error: cmake $CMAKE_VERSION is inside the forbidden band [$first, $past_last)," >&2
             echo "       where Emscripten's platform module turns a SHARED library into a static" >&2
             echo "       archive and the Web drop-in is never produced (see $PIN)" >&2
             exit 1; }
done <<<"$(awk '$1 == "forbidden_cmake" { print $2, $3 }' "$PIN")"

OUT="${OUT:-artifacts/buildtools/$CMAKE_VERSION-$NINJA_VERSION-$HOST}"
DL="artifacts/buildtools/dl"
# Beside the tree, never inside it: an interrupted unpack leaves an unstamped
# directory, which is stale rather than current.
STAMP="$OUT.pin"
# The stamp names every pinned input the tree was built from, so swapping either
# archive invalidates it.
STAMP_TEXT="$CMAKE_VERSION $CMAKE_SHA $NINJA_VERSION $NINJA_SHA"

echo "cmake:   $CMAKE_VERSION"
echo "ninja:   $NINJA_VERSION"
echo "host:    $HOST"
echo "out:     $OUT"

if [ "$FORCE" != 1 ] && [ "$(file_text "$STAMP")" = "$STAMP_TEXT" ]; then
    echo "skip: already unpacked from the pinned archives"
    exit 0
fi

echo "== 1/3 the pinned archives =="
CMAKE_CACHED="$DL/$HOST-$(basename "$CMAKE_URL")"
NINJA_CACHED="$DL/$HOST-$(basename "$NINJA_URL")"
fetch_pinned "$CMAKE_URL" "$CMAKE_SHA" "$CMAKE_CACHED"
fetch_pinned "$NINJA_URL" "$NINJA_SHA" "$NINJA_CACHED"

# unpack ARCHIVE DEST STRIP — by the archive's own extension.
unpack() {
    mkdir -p "$2"
    case "$1" in
        *.tar.gz) tar -xzf "$1" -C "$2" --strip-components="$3" ;;
        *.zip)    unzip_strip "$1" "$2" "$3" ;;
        *) echo "error: $1 is neither a .tar.gz nor a .zip" >&2; exit 1 ;;
    esac
}

echo "== 2/3 unpacking =="
rm -f "$STAMP"
rm -rf "$OUT.part"
mkdir -p "$OUT.part"
unpack "$CMAKE_CACHED" "$OUT.part/cmake" "$CMAKE_STRIP"
unpack "$NINJA_CACHED" "$OUT.part/ninja" "$NINJA_STRIP"

# The layout bundled_cmake / bundled_ninja name under a bundle's buildtools/.
CMAKE_EXE="$OUT.part/cmake/bin/cmake$EXE_EXT"
NINJA_EXE="$OUT.part/ninja/ninja$EXE_EXT"
# unzip_strip drops the archive's execute bits, and ninja ships as a zip on every
# host: without this the staged POSIX ninja cannot run at all. On Windows the bit
# is decoration — MSYS synthesizes executability from the PE header.
chmod +x "$NINJA_EXE" 2>/dev/null || true

echo "== 3/3 asserting the unpacked tools =="
[ -x "$CMAKE_EXE" ] || { echo "error: no executable $CMAKE_EXE in the archive (wrong strip depth?)" >&2; exit 1; }
[ -x "$NINJA_EXE" ] || { echo "error: $NINJA_EXE is not executable" >&2; exit 1; }
got="$("$CMAKE_EXE" --version | awk 'NR == 1 { print $3 }')"
[ "$got" = "$CMAKE_VERSION" ] || {
    echo "error: unpacked cmake is $got, pinned $CMAKE_VERSION" >&2; exit 1; }
got="$("$NINJA_EXE" --version)"
[ "$got" = "$NINJA_VERSION" ] || {
    echo "error: unpacked ninja is $got, pinned $NINJA_VERSION" >&2; exit 1; }
# The file cmake itself probes to accept a CMAKE_ROOT, so this is the structural
# check rather than a guess at which of its data directories matter.
modules="$(echo "$OUT.part"/cmake/share/cmake-*/Modules/CMakeSystemSpecificInformation.cmake)"
[ -f "$modules" ] || {
    echo "error: the staged cmake has no share/cmake-*/Modules/CMakeSystemSpecificInformation.cmake," >&2
    echo "       so it would refuse its own CMAKE_ROOT" >&2; exit 1; }

rm -rf "$OUT"
mv "$OUT.part" "$OUT"
printf '%s\n' "$STAMP_TEXT" > "$STAMP"

echo
echo "OK: $OUT ($(du -sh "$OUT" | awk '{print $1}'))"

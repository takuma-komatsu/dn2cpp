#!/usr/bin/env bash
# dist/release-run.sh — run one release per host, with the two-host handoff in one script
#
# Usage:
#   dist/release-run.sh macos --version <V> --prev-version <P> [options]
#   dist/release-run.sh windows --version <V> --prev-version <P> [options]
#     --version V           release version, and the tag name
#                           (e.g. 4.7.1-dn2cpp.3.1)
#     --prev-version P      release name this one follows
#     --repo R              GitHub repo (default: takuma-komatsu/godot-dn2cpp)
#     --out DIR             output parent (default: artifacts/release)
#     --dry-run-only        run validation to release-github --dry-run only; skip
#                           draft/handoff/publish mutations
#     -h | --help
#
# The Mac host path builds editor-macos / web / macos and creates the draft
# release. The Windows host path expects the handoff tarball, builds editor-windows,
# drops the handoff, then publishes with --uploaded-lane for the other three lanes.
#
# See docs/RELEASE.md / docs/RELEASE.ja.md for the full manual procedure and
# prerequisites. This wrapper intentionally keeps the command order and checks from
# there, and does not invent extra gates.
source "$(dirname "$0")/../gates/_common.sh"

die() { echo "error: $*" >&2; exit 1; }

require_value() {
    [ "$#" -ge 2 ] || { echo "error: $1 requires a value" >&2; exit 2; }
}

log() { printf '\n== %s ==\n' "$1"; }
run() { log "$1"; shift; "$@" || die "command failed: $*"; }

meta_get() {
    # meta_get FILE KEY — value for KEY=value, or empty when absent.
    awk -F= -v k="$2" '$1 == k { sub(/^[^=]*=/, ""); print; exit }' "$1"
}

HOST=
VERSION=
PREV_VERSION=
REPO=takuma-komatsu/godot-dn2cpp
OUT=artifacts/release
DRY_RUN_ONLY=0

if [ "$#" -lt 1 ]; then
    echo "error: required host argument: macos or windows" >&2
    awk 'NR > 1 && !/^#/ { exit } NR > 1 { print }' "$0"
    exit 2
fi

case "$1" in
    macos|windows) HOST="$1"; shift ;;
    -h|--help)
        awk 'NR > 1 && !/^#/ { exit } NR > 1 { print }' "$0"
        exit 0 ;;
    *) echo "error: first argument must be macos or windows: $1" >&2; exit 2 ;;
esac

while [ $# -gt 0 ]; do
    case "$1" in
        --version)      require_value "$@"; VERSION="$2"; shift 2 ;;
        --prev-version) require_value "$@"; PREV_VERSION="$2"; shift 2 ;;
        --repo)         require_value "$@"; REPO="$2"; shift 2 ;;
        --out)          require_value "$@"; OUT="$2"; shift 2 ;;
        --dry-run-only) DRY_RUN_ONLY=1; shift ;;
        -h|--help)
            awk 'NR > 1 && !/^#/ { exit } NR > 1 { print }' "$0"
            exit 0 ;;
        *) echo "error: unknown argument: $1" >&2; exit 2 ;;
    esac
done

[ -n "$VERSION" ] || die "--version is required (e.g. --version 4.7.1-dn2cpp.3.1)"
release_version_split "$VERSION" || exit 1
[ -n "$PREV_VERSION" ] || die "--prev-version is required (e.g. --prev-version 4.7.1-dn2cpp.3)"

log "godot-dn2cpp release run ($HOST)"
echo "-- version: $VERSION"
echo "-- prev:    $PREV_VERSION"
echo "-- repo:    $REPO"
echo "-- out:     $OUT"

case "$HOST" in
    macos)
        [ "$DN2CPP_OS" = macos ] || die "this run is for macOS host; detected OS is $DN2CPP_OS"

        run "setup buildtools" ./gates/setup-buildtools.sh
        run "setup emsdk" ./gates/setup-emsdk.sh
        run "selfhost emit" ./gates/selfhost-emit.sh
        run "setup godot fork" ./gates/setup-godot-fork.sh
        run "setup web fork" ./gates/setup-godot-fork-web.sh
        run "package web template" ./dist/package-web-template.sh --version "$VERSION" --out "$OUT"
        run "package macOS template" ./dist/package-macos-template.sh --version "$VERSION" --out "$OUT"
        run "package macOS editor" \
            ./dist/package-editor-macos.sh --version "$VERSION" --dn2cpp-commit "$(git rev-parse HEAD)" --out "$OUT"

        run "release dry-run (macOS lanes)" ./dist/release-github.sh --version "$VERSION" --prev-version "$PREV_VERSION" --repo "$REPO" --out "$OUT" --dry-run
        if [ "$DRY_RUN_ONLY" -eq 1 ]; then
            log "dry-run-only: skipping draft creation and handoff upload"
            exit 0
        fi
        log "release draft (macOS lanes)"
        run "create draft" ./dist/release-github.sh --version "$VERSION" --prev-version "$PREV_VERSION" --repo "$REPO" --out "$OUT"

        log "handoff to windows"
        run "upload handoff" ./dist/release-handoff.sh put --version "$VERSION" --repo "$REPO" --out "$OUT"
        ;;
    windows)
        [ "$DN2CPP_OS" = windows ] || die "this run is for Windows host; detected OS is $DN2CPP_OS"
        case "${CMAKE_CXX_COMPILER:-}" in
            cl|cl.exe) ;;
            *) die "export CMAKE_CXX_COMPILER=cl before running the Windows flow" ;;
        esac

        run "setup buildtools" ./gates/setup-buildtools.sh
        run "setup emsdk" ./gates/setup-emsdk.sh
        run "selfhost emit" ./gates/selfhost-emit.sh
        run "setup godot fork" ./gates/setup-godot-fork.sh

        log "pull handoff asset from release draft"
        run "receive handoff" ./dist/release-handoff.sh get --version "$VERSION" --repo "$REPO" --out "$OUT"

        dn2cpp_commit="$(meta_get "$OUT/editor-macos.metadata" dn2cpp_commit)"
        [ -n "$dn2cpp_commit" ] || die "$OUT/editor-macos.metadata has no dn2cpp_commit. Did you run the macOS flow for this version?"
        run "package Windows editor" \
            ./dist/package-editor-windows.sh --version "$VERSION" --dn2cpp-commit "$dn2cpp_commit" --out "$OUT"

        if [ "$DRY_RUN_ONLY" -eq 1 ]; then
            run "release dry-run (Windows lane)" ./dist/release-github.sh --version "$VERSION" --prev-version "$PREV_VERSION" --repo "$REPO" --out "$OUT" \
                --lane editor-windows \
                --uploaded-lane editor-macos --uploaded-lane web --uploaded-lane macos \
                --dry-run
            log "dry-run-only: skipping handoff drop and publish"
            exit 0
        fi

        log "drop handoff"
        run "remove handoff" ./dist/release-handoff.sh drop --version "$VERSION" --repo "$REPO" --out "$OUT"

        run "release dry-run (Windows lane)" ./dist/release-github.sh --version "$VERSION" --prev-version "$PREV_VERSION" --repo "$REPO" --out "$OUT" \
            --lane editor-windows \
            --uploaded-lane editor-macos --uploaded-lane web --uploaded-lane macos \
            --publish --dry-run
        log "release publish (Windows lane)"
        run "publish" ./dist/release-github.sh --version "$VERSION" --prev-version "$PREV_VERSION" --repo "$REPO" --out "$OUT" \
            --lane editor-windows \
            --uploaded-lane editor-macos --uploaded-lane web --uploaded-lane macos \
            --publish
        ;;
esac

echo
echo "OK: release flow completed for $HOST"

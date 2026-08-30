#!/usr/bin/env bash
# dist/release-handoff.sh — carry the macOS→Windows handoff on the draft release
# itself, instead of by a file transfer nobody can describe afterwards.
#
# A release spans two hosts, and the second one needs eight small things the
# first produced: the three lanes' metadata, SHA256SUMS.txt, the Web template
# bundle with its provenance stamp, and both fork-root Web emcc stamps. Both hosts
# already have gh authenticated against the repo and the draft already exists,
# so the draft is the transport that is certainly there.
#
# The tarball rides the draft as an extra asset and MUST come off before the
# release is published — `dist/release-handoff.sh drop` does that, and
# `dist/release-github.sh --publish` refuses while it is still attached.
#
# Usage:
#   dist/release-handoff.sh <put|get|drop> --version <V> [options]
#     put           macOS, right after the draft is cut: check the eight items,
#                   pack them, upload
#     get           Windows: download, verify, and place them
#     drop          Windows, before publishing: take the asset off the release
#     --version V   release version, and the tag name (e.g. 4.7.1-dn2cpp.3.1)
#     --repo R      GitHub repo (default: takuma-komatsu/godot-dn2cpp)
#     --out DIR     directory holding the assets (default: artifacts/release)
#     --dry-run     run every precondition, then print the gh commands instead
#                   of running them
#     -h | --help
#
# The fork root is $DN2CPP_GODOT_FORK_ROOT, the spelling every gate already
# uses; there is deliberately no flag for a second one.
#
# Running `put` twice is fine: --clobber replaces the asset, and the tarball
# hashing differently between runs is harmless precisely because no hash of it
# is written down anywhere — `get` reads the digest off the release at fetch
# time, over the bytes GitHub is serving.
#
# macOS tar can add AppleDouble sidecar entries (`._name`) alongside real ones;
# `get` disregards them rather than treating them as undeclared members.
#
# Sourcing _common.sh cd's to the repo root; _godot_fork.sh is side-effect-free
# and gives $FORK_ROOT.

MODE=
case "${1:-}" in
    put|get|drop) MODE="$1"; shift ;;
    -h|--help) ;;
    "") echo "error: a subcommand is required: put | get | drop" >&2; exit 2 ;;
    *) echo "error: unknown argument: $1" >&2; exit 2 ;;
esac

VERSION=
REPO=takuma-komatsu/godot-dn2cpp
OUT=artifacts/release
DRY_RUN=0

while [ $# -gt 0 ]; do
    case "$1" in
        --version) VERSION="$2"; shift 2 ;;
        --repo)    REPO="$2"; shift 2 ;;
        --out)     OUT="$2"; shift 2 ;;
        --dry-run) DRY_RUN=1; shift ;;
        # The header IS the help text, printed up to the first non-comment line
        # rather than by line number, so editing it cannot truncate --help.
        -h|--help) awk 'NR > 1 && !/^#/ { exit } NR > 1 { print }' "$0"; exit 0 ;;
        *) echo "error: unknown argument: $1" >&2; exit 2 ;;
    esac
done

[ -n "$MODE" ] || { echo "error: a subcommand is required: put | get | drop" >&2; exit 2; }

source "$(dirname "$0")/../gates/_common.sh"
source "$(dirname "$0")/../gates/_godot_fork.sh"

die() { echo "error: $*" >&2; exit 1; }

[ -n "$VERSION" ] || die "--version is required (e.g. --version 4.7.1-dn2cpp.3.1)"
release_version_split "$VERSION" || exit 1
TAG="$VERSION"
ASSET="internal-handoff-$VERSION-macos-to-windows.tgz"

WORK="$(mktemp -d)"
trap 'rm -rf "$WORK"' EXIT

# ── The handoff's member set ──────────────────────────────────────────────────
# Declared once, in two -C groups: `put` hands these names to tar and `get`
# requires the archive to hold exactly them. A second copy of the list would
# drift, and the drift is silent in the direction that matters — an archive
# member nobody checks is a file nobody places.
MEMBERS_OUT="editor-macos.metadata web.metadata macos.metadata SHA256SUMS.txt
godot-$VERSION-web-templates.zip godot-$VERSION-web-templates.zip.provenance"
MEMBERS_FORK="web_emcc.txt web_emcc_debug.txt"

# member_source NAME — the script that produces the member, for a death that
# names the remedy rather than only the symptom.
member_source() {
    case "$1" in
        editor-macos.metadata) printf 'dist/package-editor-macos.sh\n' ;;
        macos.metadata)        printf 'dist/package-macos-template.sh\n' ;;
        web.metadata|godot-*)  printf 'dist/package-web-template.sh\n' ;;
        SHA256SUMS.txt)        printf 'the packaging scripts (every lane appends its row)\n' ;;
        web_emcc*.txt)         printf 'gates/setup-godot-fork-web.sh\n' ;;
    esac
}

# meta_get FILE KEY — the value of a key=value line, empty when absent. awk reads
# the FILE (no pipeline), so its `exit` is not an early-exiting pipe consumer.
meta_get() {
    awk -F= -v k="$2" '$1 == k { sub(/^[^=]*=/, ""); print; exit }' "$1"
}

# web_bundle_assert DIR — the handoff is a trust boundary between hosts. Check
# both the metadata-only pair invariants and the local nested archive before it
# is uploaded or placed.
web_bundle_assert() {
    local dir="$1" stamps_dir="$2" meta="$1/web.metadata"
    local archive="$1/godot-$VERSION-web-templates.zip" py check
    py="$(resolve_python)" || die "Python 3 is required to inspect the Web template bundle"
    # shellcheck disable=SC2086 -- resolve_python may answer `py -3`.
    if ! check="$($py - "$archive" "$meta" \
            "$stamps_dir/web_emcc.txt" "$stamps_dir/web_emcc_debug.txt" \
            "$archive.provenance" 2>&1 <<'PY'
import hashlib
import sys
import zipfile

archive, metadata_path, release_emcc_path, debug_emcc_path, provenance_path = sys.argv[1:]
try:
    with open(metadata_path, encoding="utf-8") as source:
        metadata = dict(line.rstrip("\n").split("=", 1) for line in source if "=" in line)
    required = ("flavor", "emcc", "engine_provenance",
                "release_template", "release_template_sha256",
                "debug_template", "debug_template_sha256")
    missing = [key for key in required if not metadata.get(key)]
    if missing:
        raise ValueError("metadata carries no value for: %s" % ", ".join(missing))
    if metadata["flavor"] != "stock":
        raise ValueError("flavor is %s, expected stock" % metadata["flavor"])
    release_name, debug_name = metadata["release_template"], metadata["debug_template"]
    if release_name != "godot_web_release.zip":
        raise ValueError("release template is %s, expected godot_web_release.zip" % release_name)
    if debug_name != "godot_web_debug.zip":
        raise ValueError("debug template is %s, expected godot_web_debug.zip" % debug_name)
    release_sha = metadata["release_template_sha256"]
    debug_sha = metadata["debug_template_sha256"]
    if release_name == debug_name:
        raise ValueError("release and debug both name %s" % release_name)
    if release_sha == debug_sha:
        raise ValueError("release and debug both have sha256 %s" % release_sha)
    with open(release_emcc_path, encoding="utf-8") as source:
        release_emcc = source.readline().rstrip("\r\n")
    with open(debug_emcc_path, encoding="utf-8") as source:
        debug_emcc = source.readline().rstrip("\r\n")
    if release_emcc != metadata["emcc"] or debug_emcc != metadata["emcc"]:
        raise ValueError(
            "transported emcc stamps (%r, %r) do not both equal metadata emcc %r"
            % (release_emcc, debug_emcc, metadata["emcc"])
        )
    with open(provenance_path, encoding="utf-8") as source:
        provenance = source.readline().rstrip("\r\n")
    if provenance != metadata["engine_provenance"]:
        raise ValueError(
            "bundle provenance %r does not equal metadata engine_provenance %r"
            % (provenance, metadata["engine_provenance"])
        )
    expected = [release_name, debug_name]
    with zipfile.ZipFile(archive) as bundle:
        names = bundle.namelist()
        if len(names) != 2 or sorted(names) != sorted(expected):
            raise ValueError("entries are %r, expected exactly %r" % (names, expected))
        for name, wanted in ((release_name, release_sha), (debug_name, debug_sha)):
            digest = hashlib.sha256()
            with bundle.open(name) as entry:
                for chunk in iter(lambda: entry.read(1024 * 1024), b""):
                    digest.update(chunk)
            actual = digest.hexdigest()
            if actual != wanted:
                raise ValueError("%s hashes to %s, metadata says %s" % (name, actual, wanted))
except (OSError, KeyError, ValueError, zipfile.BadZipFile) as error:
    print("Web template bundle is invalid: %s" % error, file=sys.stderr)
    sys.exit(1)
PY
    )"; then
        die "$check"
    fi
}

# run_step CMD... — run it, or print it under --dry-run. %q so the printed line
# is the command that would run, quoting included.
run_step() {
    if [ "$DRY_RUN" -eq 1 ]; then
        printf 'DRY-RUN:'
        printf ' %q' "$@"
        printf '\n'
    else
        "$@"
    fi
}

echo "== godot-dn2cpp handoff $MODE $TAG =="
echo "-- asset: $ASSET"

# ── 1. gh, authenticated ──────────────────────────────────────────────────────
command -v gh >/dev/null 2>&1 || die "gh (GitHub CLI) is not on PATH — https://cli.github.com"
if ! gh_status="$(gh auth status 2>&1)"; then
    echo "$gh_status" >&2
    die "gh is not authenticated — run 'gh auth login'"
fi
echo "-- gh: authenticated"

# release_asset_digest ASSET — the digest GitHub serves the asset under, empty
# when the release does not carry it.
release_asset_digest() {
    local assets
    assets="$(gh release view "$TAG" --repo "$REPO" --json assets \
        --jq '.assets[] | "\(.name)\t\(.digest)"' 2>/dev/null || true)"
    awk -F'\t' -v n="$1" '$1 == n { print $2 }' <<<"$assets"
}

case "$MODE" in

put)
    # ── 2. The eight items are here ───────────────────────────────────────────
    [ -d "$OUT" ] || die "no asset directory at $OUT (--out names it)"
    for m in $MEMBERS_OUT; do
        [ -f "$OUT/$m" ] || die "$OUT/$m is missing — $(member_source "$m") produces it"
    done
    for m in $MEMBERS_FORK; do
        [ -f "$FORK_ROOT/$m" ] || die "the fork root has no $m ($FORK_ROOT/$m)
       Only $(member_source "$m") writes it, and only on this host."
    done
    echo "-- items: $(printf '%s\n' $MEMBERS_OUT | wc -l | tr -d ' ') in $OUT, $(printf '%s\n' $MEMBERS_FORK | wc -l | tr -d ' ') in $FORK_ROOT"

    # ── 3. They describe THIS version ─────────────────────────────────────────
    # A directory that was never moved aside (docs/RELEASE.md §0-D) still holds
    # the previous release's files, and every one of them would travel.
    for m in $MEMBERS_OUT; do
        case "$m" in *.metadata) ;; *) continue ;; esac
        v="$(meta_get "$OUT/$m" release_version)"
        [ "$v" = "$VERSION" ] \
            || die "$OUT/$m says release_version=$v, but --version is $VERSION"
    done
    want="$(meta_get "$OUT/web.metadata" asset_sha256)"
    got="$(shasum -a 256 "$OUT/godot-$VERSION-web-templates.zip" | awk '{print $1}')"
    [ "$want" = "$got" ] \
        || die "$OUT/web.metadata says asset_sha256=$want, but the template bundle hashes to $got"
    web_bundle_assert "$OUT" "$FORK_ROOT"
    echo "-- release_version: $VERSION (every metadata file agrees; the template bundle matches its hash)"

    # ── 4. The release exists and is still a draft ────────────────────────────
    # The handoff is a draft-only asset by construction rather than by habit:
    # this is the check that makes that true.
    is_draft="$(gh release view "$TAG" --repo "$REPO" --json isDraft --jq '.isDraft' 2>/dev/null || true)"
    [ -n "$is_draft" ] || die "$REPO has no release $TAG — cut the draft first:
         dist/release-github.sh --version $VERSION --prev-version <P>"
    [ "$is_draft" = true ] || die "release $TAG is published, and the handoff rides a DRAFT
       Publishing was the last step; there is nothing left to hand over."
    echo "-- release $TAG: exists, still a draft"

    # ── 5. The tarball ────────────────────────────────────────────────────────
    # Built in $WORK, never in $OUT: a file in $OUT is an asset no lane declares,
    # and dist/release-github.sh's row-set check dies on exactly that. Built for
    # real under --dry-run too — it is invisible in a temp dir, and it is the
    # only way a dry run proves the tar invocation.
    # COPYFILE_DISABLE keeps macOS tar from writing AppleDouble sidecars
    # (`._name`) for xattrs such as com.apple.provenance; harmless elsewhere.
    COPYFILE_DISABLE=1 tar czf "$WORK/$ASSET" \
        -C "$OUT" $MEMBERS_OUT \
        -C "$FORK_ROOT" $MEMBERS_FORK
    echo "-- packed: $(wc -c < "$WORK/$ASSET" | tr -d ' ') bytes, sha256 $(shasum -a 256 "$WORK/$ASSET" | awk '{print $1}')"

    # ── 6. Upload ─────────────────────────────────────────────────────────────
    run_step gh release upload "$TAG" --repo "$REPO" --clobber "$WORK/$ASSET"

    if [ "$DRY_RUN" -eq 1 ]; then
        echo "OK: dry run — every precondition passed, the tarball packed, nothing uploaded"
    else
        echo "OK: handoff on draft $TAG. On the Windows host:"
        echo "     dist/release-handoff.sh get --version $VERSION"
        echo "   and before publishing, take it off again:"
        echo "     dist/release-handoff.sh drop --version $VERSION"
    fi
    ;;

get)
    # ── 2. The asset, as the release holds it ─────────────────────────────────
    digest="$(release_asset_digest "$ASSET")"
    [ -n "$digest" ] || die "release $TAG carries no asset named '$ASSET'
       Run it on the macOS host first:
         dist/release-handoff.sh put --version $VERSION"
    echo "-- release $TAG: serves $ASSET as $digest"

    # ── 3. Download ───────────────────────────────────────────────────────────
    gh release download "$TAG" --repo "$REPO" --pattern "$ASSET" --dir "$WORK" --clobber

    # ── 4. The bytes are the ones the release names ───────────────────────────
    # GitHub's digest is over the bytes it is serving, which is why it is the
    # witness --uploaded-lane trusts too — a hash carried alongside would only
    # speak for whatever produced the pair.
    got="$(shasum -a 256 "$WORK/$ASSET" | awk '{print $1}')"
    [ "$digest" = "sha256:$got" ] \
        || die "release $TAG serves '$ASSET' as $digest, but the downloaded bytes hash to $got"
    echo "-- digest: matches the downloaded bytes"

    # ── 5. The archive holds exactly the declared eight ───────────────────────
    # Both directions, before anything is unpacked: an eighth member, an absolute
    # path or a `..` is refused rather than written somewhere unexpected.
    got_members="$(tar tzf "$WORK/$ASSET" | grep -Ev '(^|/)\._' | sort)"
    want_members="$(printf '%s\n%s\n' "$MEMBERS_OUT" "$MEMBERS_FORK" | tr ' ' '\n' | grep -v '^$' | sort)"
    if [ "$got_members" != "$want_members" ]; then
        extra="$(comm -23 <(printf '%s\n' "$got_members") <(printf '%s\n' "$want_members") | tr '\n' ' ')"
        absent="$(comm -13 <(printf '%s\n' "$got_members") <(printf '%s\n' "$want_members") | tr '\n' ' ')"
        die "$ASSET does not hold exactly the handoff's members:
       members not in the handoff: ${extra:-none}
       members missing:            ${absent:-none}
       Re-run put on the macOS host."
    fi
    mkdir -p "$WORK/x"
    # Extract only the verified members, not the whole archive — otherwise a
    # `._` sidecar excluded above by the member-list check would still land.
    tar xzf "$WORK/$ASSET" -C "$WORK/x" $MEMBERS_OUT $MEMBERS_FORK
    web_bundle_assert "$WORK/x" "$WORK/x"
    echo "-- archive: the declared $(printf '%s\n' "$want_members" | wc -l | tr -d ' ') members, extracted to a temp dir"

    # ── 6. Nothing here is newer than what is arriving ────────────────────────
    # A Windows packaging run has already appended a later row, and placing the
    # incoming three-row file over it reverts that silently — nothing
    # downstream reads the file again until the release is rendered from it.
    if [ -f "$OUT/SHA256SUMS.txt" ]; then
        lost="$(comm -23 <(sort "$OUT/SHA256SUMS.txt") <(sort "$WORK/x/SHA256SUMS.txt") | tr '\n' ' ')"
        [ -z "${lost// }" ] || die "$OUT/SHA256SUMS.txt carries row(s) the handoff does not: $lost
       Placing the handoff would drop them. Move the directory aside instead
       (docs/RELEASE.md §0-D) — never edit the file:
         mv $OUT $OUT-<old version>"
    fi
    for meta in "$OUT"/*.metadata; do
        [ -f "$meta" ] || continue
        v="$(meta_get "$meta" release_version)"
        [ "$v" = "$VERSION" ] || die "$meta says release_version=$v, not $VERSION
       It belongs to another release. Move the directory aside (docs/RELEASE.md §0-D):
         mv $OUT $OUT-$v"
    done

    # ── 7. Place them ─────────────────────────────────────────────────────────
    # Both Web emcc stamps go to the fork root here rather than in a follow-up
    # step; forgetting either move only surfaces later in Windows editor smoke.
    run_step mkdir -p "$OUT"
    for m in $MEMBERS_OUT; do
        run_step cp -f "$WORK/x/$m" "$OUT/$m"
    done
    for m in $MEMBERS_FORK; do
        run_step mkdir -p "$FORK_ROOT"
        run_step mv -f "$WORK/x/$m" "$FORK_ROOT/$m"
    done
    echo "-- placed: $(printf '%s\n' $MEMBERS_OUT | wc -l | tr -d ' ') in $OUT, $(printf '%s\n' $MEMBERS_FORK | wc -l | tr -d ' ') in $FORK_ROOT"

    # ── 8. The checksums, over the files that are actually here ───────────────
    # The macOS assets are not on this host and are not meant to be; their rows
    # are checked against the release's own digests when the notes are rendered.
    if [ "$DRY_RUN" -eq 1 ]; then
        echo "-- SHA256SUMS.txt: not checked (dry run placed nothing)"
    else
        sums_here="$WORK/SHA256SUMS.here"
        : > "$sums_here"
        n_here=0
        n_remote=0
        while IFS= read -r row; do
            name="${row#*  }"
            if [ -f "$OUT/$name" ]; then
                printf '%s\n' "$row" >> "$sums_here"
                n_here=$((n_here + 1))
            else
                n_remote=$((n_remote + 1))
            fi
        done < "$OUT/SHA256SUMS.txt"
        if [ "$n_here" -gt 0 ]; then
            sums_out="$(cd "$OUT" && shasum -a 256 -c "$sums_here")" \
                || die "SHA256SUMS.txt does not describe the files in $OUT:
$sums_out"
            echo "$sums_out"
        fi
        echo "-- SHA256SUMS.txt: $n_here row(s) checked against the files here, $n_remote not on this host"
    fi

    if [ "$DRY_RUN" -eq 1 ]; then
        echo "OK: dry run — the handoff verified and unpacked to a temp dir, nothing placed"
    else
        echo "OK: handoff placed. Package the Windows lane next (docs/RELEASE.md Phase C),"
        echo "   and before publishing: dist/release-handoff.sh drop --version $VERSION"
    fi
    ;;

drop)
    # ── 2. Nothing to drop is a success ───────────────────────────────────────
    # The publish flow re-enters here after a failed step, and a second run of a
    # step that already did its work is not an error.
    if [ -z "$(release_asset_digest "$ASSET")" ]; then
        echo "-- release $TAG carries no $ASSET; nothing to drop"
        exit 0
    fi

    # ── 3. Delete it ──────────────────────────────────────────────────────────
    # No draft check: an internal asset on a PUBLISHED release is the one case
    # where deleting it is most urgent, not least.
    run_step gh release delete-asset "$TAG" "$ASSET" --repo "$REPO" --yes
    if [ "$DRY_RUN" -eq 1 ]; then
        echo "OK: dry run — $ASSET is on release $TAG and would be deleted"
    else
        echo "OK: $ASSET removed from release $TAG; it is ready to publish"
    fi
    ;;
esac

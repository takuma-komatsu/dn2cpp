#!/usr/bin/env bash
# dist/release-github.sh — tag the godot-dn2cpp fork and cut the GitHub release
# from the artifacts a packaging run left in artifacts/release/.
#
# The inputs are the packaging run's own metadata files, never this script's
# opinion: one <lane>.metadata per lane carries the version, the fork commit the
# editor was built from, the base pin, the provenance strings and the hashes,
# and every one of them is checked against the tree before anything is created.
# The check that matters most is fork_commit: a tag is a name for a commit, and
# an editor built from another branch would otherwise be published under it.
#
# A release spans HOSTS — a macOS editor can only be cut on macOS, a Windows one
# only on Windows — so the lane set is an argument rather than a constant, and
# the notes template carries a block per lane. --uploaded-lane is the other half
# of that: a lane whose asset is already on the release, described by metadata
# and checked against what GitHub is actually serving.
#
# The notes are a RENDERING of that metadata, not a document to edit: re-running
# this script is the only thing that changes them, published release included.
# The prose that does not change between releases lives in docs/EDITOR-GUIDE.ja.md,
# which the notes link at this repository's HEAD sha so a link keeps saying what
# it said the day it was published.
#
# Usage:
#   dist/release-github.sh --version <V> --prev-version <P> [options]
#     --version V           release version, and the tag name (e.g. 4.7.1-dn2cpp.3.1)
#     --prev-version P      the release this one follows, named in the notes'
#                           "changes since the previous release" heading
#     --repo R              GitHub repo (default: takuma-komatsu/godot-dn2cpp)
#     --commit SHA          fork commit to tag (default: the fork worktree's HEAD)
#     --out DIR             directory holding the assets (default: artifacts/release)
#     --lane NAME           a lane whose metadata AND asset are in --out; repeatable
#     --uploaded-lane NAME  a lane whose metadata is in --out and whose asset is
#                           already on the release; repeatable
#     --publish             drop the draft flag once every asset is uploaded
#     --dry-run             run every precondition, render the notes, then print
#                           the git/gh commands instead of running them
#     -h | --help
#
# The lanes are editor-macos, editor-windows, web and macos; given none, the set
# is editor-macos, web and macos — a macOS host's whole cut.
#
# The release is a DRAFT unless --publish: assets are uploaded one at a time and
# a draft is the only state in which a half-uploaded release is not visible.
#
# Sourcing _common.sh cd's to the repo root; _godot_fork.sh resolves the fork
# worktree the same way the editor-export gates do (godot_fork_resolve).
source "$(dirname "$0")/../gates/_common.sh"
source "$(dirname "$0")/../gates/_godot_fork.sh"

# The fork's release branch. A commit reachable from nothing on the remote would
# be tagged into a release whose source nobody can fetch.
FORK_BRANCH=dn2cpp/main
# The dn2cpp branch the notes' dn2cpp_commit values must be on, for that reason.
DN2CPP_BRANCH=main

VERSION=
PREV_VERSION=
REPO=takuma-komatsu/godot-dn2cpp
COMMIT=
OUT=artifacts/release
ACTIVE_LANES=
UPLOADED_LANES=
PUBLISH=0
DRY_RUN=0

while [ $# -gt 0 ]; do
    case "$1" in
        --version)       VERSION="$2"; shift 2 ;;
        --prev-version)  PREV_VERSION="$2"; shift 2 ;;
        --repo)          REPO="$2"; shift 2 ;;
        --commit)        COMMIT="$2"; shift 2 ;;
        --out)           OUT="$2"; shift 2 ;;
        --lane)          ACTIVE_LANES="$ACTIVE_LANES $2"; shift 2 ;;
        --uploaded-lane) ACTIVE_LANES="$ACTIVE_LANES $2"
                         UPLOADED_LANES="$UPLOADED_LANES $2"; shift 2 ;;
        --publish)       PUBLISH=1; shift ;;
        --dry-run)       DRY_RUN=1; shift ;;
        # The header IS the help text, printed up to the first non-comment line
        # rather than by line number, so editing it cannot truncate --help.
        -h|--help)       awk 'NR > 1 && !/^#/ { exit } NR > 1 { print }' "$0"; exit 0 ;;
        *) echo "error: unknown argument: $1" >&2; exit 2 ;;
    esac
done

die() { echo "error: $*" >&2; exit 1; }

# ── The lane table ────────────────────────────────────────────────────────────
# One row per lane, and the only place a lane is described: its KIND (`editor`
# is the kind a tag's fork_commit is checked against), the metadata keys it must
# carry, and the `PLACEHOLDER:key` bindings its part of the notes needs. The row
# is what holds lane name = metadata file name = notes placeholder suffix
# together; adding a lane is adding a row here and a block in the template.
LANES_KNOWN="editor-macos editor-windows web macos"

# editor_row SUFFIX — both editors carry the same keys and bind the same
# placeholders; only the suffix that keeps their notes rows apart differs.
editor_row() {
    printf 'editor|%s|%s\n' \
        "release_version asset asset_sha256 fork_commit base_pin engine_provenance editor_version_string dn2cpp_commit toolchain_content_hash corelib_framework prebuilt_axes cmake_version ninja_version node_version" \
        "ASSET_EDITOR$1:asset ASSET_EDITOR$1_SHA256:asset_sha256 EDITOR_VERSION_STRING$1:editor_version_string DN2CPP_COMMIT$1:dn2cpp_commit TOOLCHAIN_CONTENT_HASH$1:toolchain_content_hash PREBUILT_AXES$1:prebuilt_axes CMAKE_VERSION$1:cmake_version NINJA_VERSION$1:ninja_version NODE_VERSION$1:node_version"
}

lane_row() {
    case "$1" in
        editor-macos)   editor_row _MACOS ;;
        editor-windows) editor_row _WINDOWS ;;
        web)   printf 'template|%s|%s\n' \
                   "release_version asset asset_sha256 engine_provenance emcc emsdk_version" \
                   "ASSET_WEB:asset ASSET_WEB_SHA256:asset_sha256 EMCC:emcc EMSDK_VERSION:emsdk_version" ;;
        macos) printf 'template|%s|%s\n' \
                   "release_version asset asset_sha256 base_pin upstream_template" \
                   "ASSET_MACOS:asset ASSET_MACOS_SHA256:asset_sha256 UPSTREAM_MACOS_SHA256:upstream_template" ;;
    esac
}

lane_kind()     { local r; r="$(lane_row "$1")"; printf '%s\n' "${r%%|*}"; }
lane_keys()     { local r; r="$(lane_row "$1")"; r="${r#*|}"; printf '%s\n' "${r%%|*}"; }
lane_bindings() { local r; r="$(lane_row "$1")"; printf '%s\n' "${r##*|}"; }
lane_meta()     { printf '%s/%s.metadata\n' "$OUT" "$1"; }

# lane_has KEY LANE — whether the lane's row demands KEY at all, i.e. whether
# asking this lane for it is a question it can answer.
lane_has() {
    case " $(lane_keys "$2") " in *" $1 "*) return 0 ;; esac
    return 1
}

lane_uploaded() {
    case " $UPLOADED_LANES " in *" $1 "*) return 0 ;; esac
    return 1
}

[ -n "$VERSION" ] || die "--version is required (e.g. --version 4.7.1-dn2cpp.3.1)"
TAG="$VERSION"
# The upstream release the fork is based on, for the notes. Derived rather than
# passed: the version IS "<godot version>-dn2cpp.<X>.<Y>", and a second
# spelling of it is the one that drifts.
release_version_split "$VERSION" || exit 1
BASE_VER="$RELEASE_BASE_VER"

# The notes name the release this one follows. Deliberately not format-checked:
# the previous release's name is a historical fact, and the ones cut before the
# version form changed carry the old form.
[ -n "$PREV_VERSION" ] || die "--prev-version is required — the notes name the release this one follows (e.g. --prev-version 4.7.1-dn2cpp.3)"
[ "$PREV_VERSION" != "$VERSION" ] || die "--prev-version is $VERSION, this release's own version — name the one it follows"

# The default is a macOS host's whole cut, which is what a lane-less invocation
# has always meant.
[ -n "${ACTIVE_LANES// }" ] || ACTIVE_LANES="editor-macos web macos"
seen=
for lane in $ACTIVE_LANES; do
    [ -n "$(lane_row "$lane")" ] || die "unknown lane '$lane' — the lanes are: $LANES_KNOWN"
    case " $seen " in *" $lane "*) die "lane '$lane' was given twice" ;; esac
    seen="$seen $lane"
done
ACTIVE_LANES="$seen"

NOTES="$OUT/RELEASE-NOTES.md"
TEMPLATE=dist/release-notes-template.md
[ -f "$TEMPLATE" ] || die "release notes template missing: $TEMPLATE"
# The guide the notes link. Renaming it turns every past release's links into
# 404s, so the path is stated once, here, and checked before anything is cut.
GUIDE=docs/EDITOR-GUIDE.ja.md
[ -f "$GUIDE" ] || die "the guide the notes link is missing: $GUIDE"

WORK="$(mktemp -d)"
trap 'rm -rf "$WORK"' EXIT

# meta_get FILE KEY — the value of a key=value line, empty when absent. awk reads
# the FILE (no pipeline), so its `exit` is not an early-exiting pipe consumer.
meta_get() {
    awk -F= -v k="$2" '$1 == k { sub(/^[^=]*=/, ""); print; exit }' "$1"
}

# meta_req FILE KEY — the same, but a missing or empty value is fatal. Every
# value here reaches the published notes, so an empty one is a lie, not a blank.
meta_req() {
    local v
    v="$(meta_get "$1" "$2")"
    [ -n "$v" ] || die "$1 carries no '$2=' value"
    printf '%s\n' "$v"
}

# lane_val LANE KEY — section 2 has already proved every key the lane's row
# demands is there and non-empty, so reads after it need no second guard.
lane_val() { meta_get "$(lane_meta "$1")" "$2"; }

# run_step CMD... — run it, or print it under --dry-run. %q so the printed line
# is the command that would run, quoting and multi-line tag message included.
run_step() {
    if [ "$DRY_RUN" -eq 1 ]; then
        printf 'DRY-RUN:'
        printf ' %q' "$@"
        printf '\n'
    else
        "$@"
    fi
}

echo "== godot-dn2cpp release $TAG =="
echo "-- lanes:$ACTIVE_LANES${UPLOADED_LANES:+ (already uploaded:$UPLOADED_LANES)}"

# ── 1. gh, authenticated ──────────────────────────────────────────────────────
command -v gh >/dev/null 2>&1 || die "gh (GitHub CLI) is not on PATH — https://cli.github.com"
if ! gh_status="$(gh auth status 2>&1)"; then
    echo "$gh_status" >&2
    die "gh is not authenticated — run 'gh auth login'"
fi
echo "-- gh: authenticated"

# ── 2. The lanes, their metadata and their assets ─────────────────────────────
[ -d "$OUT" ] || die "no asset directory at $OUT (--out names it)"
[ -f "$OUT/SHA256SUMS.txt" ] || die "$OUT/SHA256SUMS.txt is missing — the packaging run did not finish"

for lane in $ACTIVE_LANES; do
    meta="$(lane_meta "$lane")"
    [ -f "$meta" ] || die "$meta is missing — the packaging run for lane '$lane' did not finish"
    for k in $(lane_keys "$lane"); do
        meta_req "$meta" "$k" >/dev/null
    done
done

# asset_path VALUE — the file an `asset=` value names, inside $OUT. Accepts a
# bare file name or a path, since only the basename is a contract.
asset_path() {
    if [ -f "$OUT/$1" ]; then
        printf '%s\n' "$OUT/$1"
    elif [ -f "$OUT/$(basename "$1")" ]; then
        printf '%s\n' "$OUT/$(basename "$1")"
    else
        return 1
    fi
}

lane_asset() { local a; a="$(lane_val "$1" asset)"; printf '%s\n' "$(basename "$a")"; }
# lane_zip LANE — the file backing the lane's asset, empty when $OUT has none.
lane_zip() { local p; p="$(asset_path "$(lane_val "$1" asset)")" || return 0; printf '%s\n' "$p"; }

for lane in $ACTIVE_LANES; do
    zip="$(lane_zip "$lane")"
    if [ -n "$zip" ]; then
        echo "-- $lane: $(basename "$zip")"
    elif lane_uploaded "$lane"; then
        echo "-- $lane: $(lane_asset "$lane") (not here; already on the release)"
    else
        die "$(lane_meta "$lane") names asset '$(lane_asset "$lane")', which is not in $OUT
       Pass --uploaded-lane $lane if that asset is already on release $TAG."
    fi
done

# ── 3. Every lane describes THIS version, and one engine ──────────────────────
for lane in $ACTIVE_LANES; do
    v="$(lane_val "$lane" release_version)"
    [ "$v" = "$VERSION" ] \
        || die "$(lane_meta "$lane") says release_version=$v, but --version is $VERSION"
done
echo "-- release_version: $VERSION (every lane's metadata agrees)"

# lane_agree KEY REMEDY — the one value every lane carrying KEY holds, or a death
# naming the two that disagree. Empty when no active lane carries KEY at all.
lane_agree() {
    local key="$1" remedy="$2" lane ref= ref_lane= v
    for lane in $ACTIVE_LANES; do
        lane_has "$key" "$lane" || continue
        v="$(lane_val "$lane" "$key")"
        if [ -z "$ref_lane" ]; then
            ref="$v"; ref_lane="$lane"; continue
        fi
        [ "$v" = "$ref" ] || die "lanes '$ref_lane' and '$lane' disagree on $key:
       $(lane_meta "$ref_lane"): $key=$ref
       $(lane_meta "$lane"): $key=$v
       $remedy"
    done
    printf '%s\n' "$ref"
}

# One release names ONE engine tree, and the notes publish a single provenance
# line for all of it — a template baked from another tree would ship under a line
# saying it was not. base_pin is the same statement one level down: the macOS
# export template is upstream's own binary, so its pin IS its whole identity.
ENGINE_PROVENANCE="$(lane_agree engine_provenance \
    "Re-package the odd one out from the same fork tree, e.g.: dist/package-web-template.sh --version $VERSION")"
BASE_PIN="$(lane_agree base_pin \
    "Re-cut the odd one out, e.g.: dist/package-macos-template.sh --version $VERSION")"
[ -n "$ENGINE_PROVENANCE" ] || die "no active lane carries engine_provenance, which the notes publish unconditionally"
[ -n "$BASE_PIN" ] || die "no active lane carries base_pin, which the notes publish unconditionally"
echo "-- engine_provenance: $ENGINE_PROVENANCE (every lane that has one agrees)"

# The editors are prerequisites for the same .NET SDK, so two spellings of it
# would put one of them under a requirement it does not have.
CORELIB_FRAMEWORK="$(lane_agree corelib_framework \
    "Both editors are built against one SDK; re-package the stale one")"

# ── 4. The fork worktree, and the commit the editors were built from ──────────
godot_fork_resolve || die "cannot resolve the fork worktree (see above)"
[ -n "$COMMIT" ] || COMMIT="$(git -C "$FORK" rev-parse HEAD)"
COMMIT="$(git -C "$FORK" rev-parse --verify --quiet "$COMMIT^{commit}")" \
    || die "the fork at $FORK does not contain commit $COMMIT"

FIRST_EDITOR_LANE=
for lane in $ACTIVE_LANES; do
    [ "$(lane_kind "$lane")" = editor ] || continue
    [ -n "$FIRST_EDITOR_LANE" ] || FIRST_EDITOR_LANE="$lane"
    fc="$(lane_val "$lane" fork_commit)"
    fc_full="$(git -C "$FORK" rev-parse --verify --quiet "$fc^{commit}")" \
        || die "$(lane_meta "$lane")'s fork_commit ($fc) is not a commit in $FORK"
    # The check the tag exists for: the binaries were built from ONE tree, and
    # the tag names another commit only by accident — a released editor whose
    # sources are not the tagged ones, with nothing to detect it afterwards.
    [ "$fc_full" = "$COMMIT" ] || die \
        "the packaged $lane editor was built from $fc_full, but the tag would name $COMMIT
       $(lane_meta "$lane"): fork_commit=$fc
       Re-package from the commit you mean to tag, or pass --commit $fc_full."
done
[ -n "$FIRST_EDITOR_LANE" ] || die "no editor lane is active — a release with no editor names a commit nothing was built from"
echo "-- fork: $FORK @ $(git -C "$FORK" rev-parse --short "$COMMIT") (every editor lane's metadata agrees)"

# Tracked files only: the fork's bin/ and its export artifacts are untracked by
# design, and they are not what a tag describes.
dirty="$(git -C "$FORK" status --porcelain --untracked-files=no)"
[ -z "$dirty" ] || die "the fork worktree has uncommitted changes to tracked files:
$dirty"

git -C "$FORK" merge-base --is-ancestor "$BASE_PIN" "$COMMIT" \
    || die "$COMMIT does not descend from the pinned upstream base $BASE_PIN"
echo "-- base pin: $BASE_PIN (an ancestor of the tagged commit)"

# ── 5. The tag on origin names THIS commit, and the commits are pushed ────────
# A tag already on origin at the tagged commit is this script's own earlier run:
# the draft → --publish flow re-enters here, as does every retry, and so does a
# second host adding its lane. Only a tag naming a *different* commit is the
# collision worth refusing. Peel before comparing — an annotated tag's own object
# id is never its commit, so the bare ref line answers a different question than
# the one being asked.
remote_tag="$(git -C "$FORK" ls-remote --tags origin "refs/tags/$TAG" "refs/tags/$TAG^{}")"
TAG_ON_ORIGIN=0
if [ -n "$remote_tag" ]; then
    remote_tag_commit="$(printf '%s\n' "$remote_tag" \
        | awk -v p="refs/tags/$TAG^{}" -v b="refs/tags/$TAG" \
              '$2 == p { peeled = $1 } $2 == b { bare = $1 }
               END { print (peeled != "" ? peeled : bare) }')"
    [ "$remote_tag_commit" = "$COMMIT" ] || die \
        "origin already carries the tag $TAG, at $remote_tag_commit, not $COMMIT:
$remote_tag
       Pick a new version, or delete the tag deliberately."
    TAG_ON_ORIGIN=1
fi

remote_tip="$(git -C "$FORK" ls-remote origin "refs/heads/$FORK_BRANCH" | awk '{print $1}')"
[ -n "$remote_tip" ] || die "origin has no branch $FORK_BRANCH — push the fork first"
# Ancestry needs the remote tip as a local object; a fetch is the caller's, since
# fetching here would quietly change what the answer is about.
git -C "$FORK" rev-parse --verify --quiet "$remote_tip^{commit}" >/dev/null \
    || die "origin/$FORK_BRANCH is at $remote_tip, which this clone does not have — run: git -C $FORK fetch origin"
git -C "$FORK" merge-base --is-ancestor "$COMMIT" "$remote_tip" || die \
    "$COMMIT is not reachable from origin/$FORK_BRANCH ($remote_tip)
       A release names a commit nobody can fetch. Push the fork first:
         git -C $FORK push origin $FORK_BRANCH"
if [ "$TAG_ON_ORIGIN" -eq 1 ]; then
    echo "-- origin/$FORK_BRANCH: contains the tagged commit; tag $TAG is already on origin at it"
else
    echo "-- origin/$FORK_BRANCH: contains the tagged commit; no tag $TAG on origin"
fi

# The same question one repository over: the notes print each editor's
# dn2cpp_commit beside a link to the dn2cpp repository, so a sha that repository
# cannot reach is a dead reference, and the release was cut from a tree nobody
# pushed. The remote is `origin`, the repository the notes link — `archive`
# carries commits it does not, and would answer for a repository nobody can fetch.
dn2cpp_tip="$(git ls-remote origin "refs/heads/$DN2CPP_BRANCH" | awk '{print $1}')"
[ -n "$dn2cpp_tip" ] || die "the dn2cpp origin has no branch $DN2CPP_BRANCH"
# Ancestry needs the remote tip as a local object; a fetch is the caller's, since
# fetching here would quietly change what the answer is about.
git rev-parse --verify --quiet "$dn2cpp_tip^{commit}" >/dev/null \
    || die "the dn2cpp origin/$DN2CPP_BRANCH is at $dn2cpp_tip, which this clone does not have — run: git fetch origin"
for lane in $ACTIVE_LANES; do
    lane_has dn2cpp_commit "$lane" || continue
    dc="$(lane_val "$lane" dn2cpp_commit)"
    dc_full="$(git rev-parse --verify --quiet "$dc^{commit}")" \
        || die "$(lane_meta "$lane")'s dn2cpp_commit ($dc) is not a commit in this repository"
    git merge-base --is-ancestor "$dc_full" "$dn2cpp_tip" || die \
        "$dc_full is not reachable from the dn2cpp origin/$DN2CPP_BRANCH ($dn2cpp_tip)
       $(lane_meta "$lane"): dn2cpp_commit=$dc
       The notes print it beside a link to the repository. Push dn2cpp first:
         git push origin $DN2CPP_BRANCH"
done
echo "-- dn2cpp origin/$DN2CPP_BRANCH: contains every editor lane's dn2cpp_commit"

# The notes link the guide at a fixed sha — this worktree's HEAD, not the remote
# tip, so a reader clicking through gets the text the operator read before
# cutting. Both checks are read-only, so --dry-run runs them for real.
DOCS_REF="$(git rev-parse HEAD)"
git merge-base --is-ancestor "$DOCS_REF" "$dn2cpp_tip" || die \
    "HEAD ($DOCS_REF) is not reachable from the dn2cpp origin/$DN2CPP_BRANCH ($dn2cpp_tip)
       The notes link $GUIDE at it, so every reader's first click is a 404.
       Push dn2cpp first:
         git push origin $DN2CPP_BRANCH"
# Only the guide, not the whole worktree: unrelated work in progress is no
# reason to refuse a cut, but an edit to the linked file is not in that sha.
guide_dirty="$(git status --porcelain -- "$GUIDE")"
[ -z "$guide_dirty" ] || die "$GUIDE has uncommitted changes, and the notes would link $DOCS_REF, which does not have them:
$guide_dirty"
echo "-- docs ref: $(git rev-parse --short "$DOCS_REF") ($GUIDE is committed and pushed)"

# ── 6. The checksums ──────────────────────────────────────────────────────────
# The rows and the active lanes' assets are the SAME set, both ways: a row for an
# asset no lane declares is another host's cut leaking in, and a missing row is
# an asset the release's own checksum file cannot vouch for.
sums_rows="$(awk '{ print substr($0, index($0, "  ") + 2) }' "$OUT/SHA256SUMS.txt" | sort)"
lane_assets="$(for lane in $ACTIVE_LANES; do lane_asset "$lane"; done | sort)"
if [ "$sums_rows" != "$lane_assets" ]; then
    extra="$(comm -23 <(printf '%s\n' "$sums_rows") <(printf '%s\n' "$lane_assets") | tr '\n' ' ')"
    absent="$(comm -13 <(printf '%s\n' "$sums_rows") <(printf '%s\n' "$lane_assets") | tr '\n' ' ')"
    die "$OUT/SHA256SUMS.txt does not describe exactly the active lanes' assets:
       rows for no active lane: ${extra:-none}
       lanes with no row:       ${absent:-none}
       Re-run the packaging script for a missing lane, or name it with --lane / --uploaded-lane."
fi

# `shasum -c` gets the rows whose file is actually here; the rest belong to an
# --uploaded-lane, and section 7 checks those against the release's own digest.
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
echo "-- SHA256SUMS.txt: $n_here row(s) checked against the files here, $n_remote against the release's digests"

# The notes publish the metadata's hashes, so those are what has to be true —
# SHA256SUMS.txt agreeing with the files says nothing about the metadata.
for lane in $ACTIVE_LANES; do
    zip="$(lane_zip "$lane")"
    [ -n "$zip" ] || continue
    want="$(lane_val "$lane" asset_sha256)"
    got="$(shasum -a 256 "$zip" | awk '{print $1}')"
    [ "$want" = "$got" ] || die "$(lane_meta "$lane") says asset_sha256=$want, but $zip hashes to $got"
    echo "-- $lane asset_sha256 matches the file"
done

# ── 7. The release as GitHub holds it, and the lanes already on it ────────────
# Read here rather than at upload time: whether the release exists decides
# create-vs-edit, whether it is a draft decides the upload order, and an
# --uploaded-lane is checked against its assets and its notes. All of it is
# read-only, so --dry-run runs it too rather than guessing.
RELEASE_EXISTS=0
REL_IS_DRAFT=true
if gh release view "$TAG" --repo "$REPO" >/dev/null 2>&1; then
    RELEASE_EXISTS=1
    REL_IS_DRAFT="$(gh release view "$TAG" --repo "$REPO" --json isDraft --jq '.isDraft')"
fi

# --uploaded-lane subtracts exactly two things: the upload, and the demand that
# the asset sit in $OUT. It subtracts no verification.
if [ -n "${UPLOADED_LANES// }" ]; then
    [ "$RELEASE_EXISTS" -eq 1 ] || die \
        "--uploaded-lane names assets already on release $TAG, and $REPO has no release $TAG"
    rel_assets="$(gh release view "$TAG" --repo "$REPO" --json assets \
        --jq '.assets[] | "\(.name)\t\(.digest)"')"
    rel_body="$(gh release view "$TAG" --repo "$REPO" --json body --jq '.body')"
    for lane in $UPLOADED_LANES; do
        asset="$(lane_asset "$lane")"
        want="$(lane_val "$lane" asset_sha256)"

        digest="$(awk -F'\t' -v n="$asset" '$1 == n { print $2 }' <<<"$rel_assets")"
        [ -n "$digest" ] || die "release $TAG carries no asset named '$asset' (lane $lane):
$rel_assets
       Put the file in $OUT and pass --lane $lane instead, or fix the metadata."
        # GitHub's digest is over the bytes it is serving, so this says more than
        # `shasum -c` over a local copy ever could.
        [ "$digest" = "sha256:$want" ] || die \
            "release $TAG serves '$asset' as $digest, but $(lane_meta "$lane") says asset_sha256=$want"

        row="$(awk -v n="$asset" '{ h = $1; if (substr($0, index($0, "  ") + 2) == n) print h }' \
            "$OUT/SHA256SUMS.txt")"
        [ "$row" = "$want" ] || die \
            "$OUT/SHA256SUMS.txt gives '$asset' as ${row:-no row at all}, but $(lane_meta "$lane") says $want"

        # The safety net for metadata reconstructed by hand on another host. A
        # digest speaks for the bytes and for nothing else, so an emcc version, an
        # editor --version, a dn2cpp commit or a toolchain hash has only one
        # witness left: the notes GitHub is already showing. Match value by value
        # rather than line by line, so editing the template cannot turn this red.
        for binding in $(lane_bindings "$lane"); do
            key="${binding#*:}"
            v="$(lane_val "$lane" "$key")"
            if [ "$key" = asset ]; then v="$asset"; fi
            case "$rel_body" in
                *"$v"*) ;;
                *) die "the notes on release $TAG do not mention $key=$v (lane $lane)
       $(lane_meta "$lane") describes something other than what was uploaded.
       Reconstruct it from the published notes before re-rendering them." ;;
            esac
        done
        echo "-- $lane: on the release as $asset, digest and published notes agree"
    done
fi

# ── 8. Release notes ──────────────────────────────────────────────────────────
# One map, one substitution pass, and a hard failure on anything left unbound:
# an unreplaced @@KEY@@ in a published note is a claim rendered as machinery.
MAP="$WORK/notes.map"
: > "$MAP"
map_put() { printf '%s\t%s\n' "$1" "$2" >> "$MAP"; }

map_put VERSION           "$VERSION"
map_put PREV_VERSION      "$PREV_VERSION"
map_put BASE_VER          "$BASE_VER"
map_put FORK_COMMIT       "$COMMIT"
map_put BASE_PIN          "$BASE_PIN"
map_put ENGINE_PROVENANCE "$ENGINE_PROVENANCE"
map_put CORELIB_FRAMEWORK "$CORELIB_FRAMEWORK"
map_put DOCS_REF          "$DOCS_REF"

# The lane rows say which placeholder each value binds, so the notes gain a lane
# by gaining a row and a template block — never a second list here.
for lane in $ACTIVE_LANES; do
    for binding in $(lane_bindings "$lane"); do
        placeholder="${binding%%:*}"
        key="${binding#*:}"
        value="$(lane_val "$lane" "$key")"
        if [ "$key" = asset ]; then value="$(lane_asset "$lane")"; fi
        map_put "$placeholder" "$value"
    done
done

# The template's rules live here, because the template publishes verbatim and so
# can hold no note to its own editor: a fact in it is a placeholder bound above
# or it is not written, and `<!--lane:NAME-->` keeps text out of a cut without
# that lane — with a line after it, that line; alone, a block ending at
# `<!--/lane-->`. `!NAME` negates.
#
# Literal substitution, never sed's: a value is data, and a `&` or a `/` in one
# would be sed syntax. The same pass drops the template's inactive lane blocks,
# which is what keeps the unbound-@@KEY@@ death honest — a dropped lane's
# placeholders never reach the output, so nothing has to invent values for it.
awk -v lanes=" $ACTIVE_LANES " -v known=" $LANES_KNOWN " '
     function active(n,   neg) {
        neg = 0
        if (substr(n, 1, 1) == "!") { neg = 1; n = substr(n, 2) }
        if (index(known, " " n " ") == 0) {
            printf "error: %s:%d names lane \"%s\", which does not exist\n", FILENAME, FNR, n > "/dev/stderr"
            bad = 1
        }
        return (index(lanes, " " n " ") > 0) != neg
     }
     NR == FNR {
        k = $0; sub(/\t.*/, "", k)
        v = $0; sub(/^[^\t]*\t/, "", v)
        map["@@" k "@@"] = v
        next
     }
     {
        if (skip) { if ($0 == "<!--/lane-->") skip = 0; next }
        if (match($0, /^<!--lane:!?[a-z0-9-]+-->/)) {
            on = active(substr($0, 10, RLENGTH - 12))
            rest = substr($0, RLENGTH + 1)
            if (rest == "") { if (!on) skip = 1; next }
            if (!on) next
            $0 = rest
        }
        if ($0 == "<!--/lane-->") next
        line = $0; out = ""
        while (match(line, /@@[A-Z0-9_]+@@/)) {
            ph = substr(line, RSTART, RLENGTH)
            out = out substr(line, 1, RSTART - 1)
            out = out ((ph in map) ? map[ph] : ph)
            line = substr(line, RSTART + RLENGTH)
        }
        print out line
     }
     END { if (bad) exit 1 }' "$MAP" "$TEMPLATE" > "$NOTES" \
    || die "$TEMPLATE names a lane that does not exist (above). The lanes are: $LANES_KNOWN"

left="$(grep -n '@@' "$NOTES" || true)"
[ -z "$left" ] || die "$NOTES still holds unbound placeholders:
$left
       Every @@KEY@@ in $TEMPLATE needs a lane row binding it, or a lane marker
       that keeps its line out of a cut this lane is not in."

stray="$(grep -n '<!--' "$NOTES" || true)"
[ -z "$stray" ] || die "$NOTES carries an HTML comment:
$stray
       The notes publish verbatim, and the lane markers are the only comments
       the render consumes. A comment holding a nested \`-->\` ends the outer
       one early, and the remainder lands on the release page as text."
echo "-- release notes: $NOTES ($(grep -c '' "$NOTES") lines)"

# ── 9. Tag, release, assets, notes ────────────────────────────────────────────
# The tag goes FIRST and is pushed before the release is created: a failure
# anywhere below then leaves the tag in place, and a rerun is idempotent.
TAG_MSG="godot-dn2cpp $TAG

Godot $BASE_VER + the dn2cpp .NET export backend.

base pin:       $BASE_PIN
fork commit:    $COMMIT
dn2cpp commit:  $(lane_val "$FIRST_EDITOR_LANE" dn2cpp_commit)
toolchain hash: $(lane_val "$FIRST_EDITOR_LANE" toolchain_content_hash)"

existing_tag="$(git -C "$FORK" rev-parse --verify --quiet "refs/tags/$TAG^{commit}" || true)"
if [ "$TAG_ON_ORIGIN" -eq 1 ]; then
    # Section 5 proved it peels to $COMMIT, so the name is already fixed on the
    # remote. Minting a local tag object for it would push nothing and differ
    # from origin's for having a second tagger stamp.
    echo "-- tag $TAG: already on origin at the tagged commit; nothing to create or push"
else
    if [ -n "$existing_tag" ]; then
        [ "$existing_tag" = "$COMMIT" ] || die \
            "a local tag $TAG already points at $existing_tag, not $COMMIT"
        echo "-- tag $TAG already exists locally at the right commit; reusing it"
    else
        run_step git -C "$FORK" tag -a "$TAG" "$COMMIT" -m "$TAG_MSG"
    fi
    run_step git -C "$FORK" push origin "$TAG"
fi

# `gh release create` has no --clobber, so a release that is already there is
# edited instead: it is the draft this script cut, the published release being
# re-uploaded to, or the one another host cut for its own lanes.
NOTES_UPDATE=0
if [ "$RELEASE_EXISTS" -eq 1 ]; then
    echo "-- release $TAG already exists; skipping create"
    NOTES_UPDATE=1
else
    run_step gh release create "$TAG" --repo "$REPO" --target "$COMMIT" \
        --title "godot-dn2cpp $TAG" --notes-file "$NOTES" --draft
fi

# One upload per asset, never `gh release create <files...>`: gh has no resume,
# and an editor zip is hundreds of megabytes — a failed batch would restart all
# of it. --clobber makes a rerun after a failed upload replace the partial asset.
PENDING=()
for lane in $ACTIVE_LANES; do
    if lane_uploaded "$lane"; then continue; fi
    PENDING+=("$(lane_zip "$lane")")
done
ORDERED=()
if [ "${#PENDING[@]}" -gt 0 ]; then
    while IFS=$'\t' read -r _ path; do
        ORDERED+=("$path")
    done <<<"$(for p in "${PENDING[@]}"; do printf '%s\t%s\n' "$(wc -c < "$p")" "$p"; done | sort -n)"
fi

# SHA256SUMS.txt and the notes are *claims about* the other assets, so on a
# PUBLISHED release they go last: otherwise there is a window in which the
# release names an asset it does not yet carry. On a draft nothing is visible at
# all, so smallest-first wins instead — a credential or quota failure costs least.
upload_assets() {
    local a
    for a in ${ORDERED[@]+"${ORDERED[@]}"}; do
        run_step gh release upload "$TAG" --repo "$REPO" --clobber "$a"
    done
}
if [ "$REL_IS_DRAFT" = true ]; then
    run_step gh release upload "$TAG" --repo "$REPO" --clobber "$OUT/SHA256SUMS.txt"
    upload_assets
else
    upload_assets
    run_step gh release upload "$TAG" --repo "$REPO" --clobber "$OUT/SHA256SUMS.txt"
fi

if [ "$NOTES_UPDATE" -eq 1 ]; then
    run_step gh release edit "$TAG" --repo "$REPO" --notes-file "$NOTES"
fi

if [ "$PUBLISH" -eq 1 ]; then
    run_step gh release edit "$TAG" --repo "$REPO" --draft=false
fi

if [ "$DRY_RUN" -eq 1 ]; then
    echo "OK: $TAG dry run — every precondition passed, notes rendered, nothing created"
elif [ "$PUBLISH" -eq 1 ]; then
    echo "OK: $TAG published — https://github.com/$REPO/releases/tag/$TAG"
elif [ "$NOTES_UPDATE" -eq 1 ]; then
    echo "OK: $TAG updated — assets uploaded and the notes re-rendered"
else
    echo "OK: $TAG drafted — review and publish it (--publish, or the web UI)"
fi

#!/usr/bin/env bash
# The gate that drives the release scripts' REFUSALS, over synthetic inputs.
#
# A release cut is hand-run, on two hosts, a few times a year — so its checks are
# the code in this repository least likely to be executed before it is needed,
# and every one of them is an error path nobody sees until the day it matters. A
# check that has never been seen to fail is not a check. This runs each one
# against inputs built to break it, and asserts the message it produces: exit
# status alone would still pass a refusal that lost the sentence naming the
# remedy, which is the half a reader at 2am actually needs.
#
# It builds nothing, transpiles nothing and reaches no network, so it can never
# `gate_skip` and deliberately takes no result cache — the same reasoning as
# gates/build-and-run-doc-claims.sh, whose shape this copies.
#
# Four fixtures, because the subjects ask different questions:
#   * a throwaway git repository for `dn2cpp_commit_pin_resolve`, which asks
#     about the tree it is run in. Asking it about THIS tree would mean dirtying
#     the tree the rest of the suite is hashing, and would answer differently
#     depending on whether the developer had uncommitted work.
#   * a directory holding a file called `dn2cpp` and a stamp beside it, for
#     `dn2cpp_pin_bin_assert` — which compares two arguments and reads nothing
#     ambient, which is what lets a fixture supply both sides.
#   * a synthetic artifacts directory of `<lane>.metadata` + empty assets for
#     dist/release-github.sh, whose agreement checks read nothing else.
#   * a complete synthetic release with stubbed git and gh views, which reaches
#     uploaded-lane verification and the notes renderer without network access.
#
# The first `gh` stub belongs to the cross-lane fixture, not the operator's
# environment: section 1 of the release script demands an authenticated gh, and
# every arm in section 4 dies before a release is read. A stub that refuses
# everything else keeps that true — an arm that runs too far fails loudly here
# instead of reaching GitHub from a gate.
source "$(dirname "$0")/_common.sh"

FAILS=0
CHECKS=0
ok()  { CHECKS=$((CHECKS + 1)); printf '  ok    %s\n' "$1"; }
bad() { CHECKS=$((CHECKS + 1)); FAILS=$((FAILS + 1)); printf '  FAIL  %s\n' "$1" >&2; }

WORK="$(mktemp -d)"
gate_add_exit_hook "rm -rf '$WORK'"

# refused LABEL RC OUTPUT NEEDLE... — the run must have failed AND said each
# needle. Both halves matter: a refusal is a status and a sentence, and the
# sentence is the part that rots.
refused() {
    local label="$1" rc="$2" out="$3"; shift 3
    if [ "$rc" -eq 0 ]; then
        bad "$label: the run succeeded; nothing refused it"
        printf '%s\n' "$out" | sed 's/^/        | /' >&2
        return
    fi
    local needle missing=
    for needle in "$@"; do
        case "$out" in *"$needle"*) ;; *) missing="$missing
        missing: $needle" ;; esac
    done
    if [ -n "$missing" ]; then
        bad "$label: refused, but not in the words that tell the operator what to do:$missing"
        printf '%s\n' "$out" | sed 's/^/        | /' >&2
    else
        ok "$label"
    fi
}

# ── 1/5 the pin resolver's arms ──────────────────────────────────────────────
echo "== 1/5 dn2cpp_commit_pin_resolve — against a throwaway repository =="
FIX="$WORK/pinrepo"
mkdir -p "$FIX/src" "$FIX/runtime" "$FIX/third_party"
# src_tree_hash enumerates exactly these three, and a git checkout is its own
# precondition; one file apiece is all the fixture owes it.
for d in src runtime third_party; do printf 'x\n' > "$FIX/$d/a.txt"; done

# Everything ambient that can reach a throwaway repository is neutralised here,
# and it is the CLASS rather than the variable that last bit us: the hooks a
# global core.hooksPath supplies, the hooks (and more) a global init.templateDir
# would install as the repo is created, a global gitignore that could hide the
# fixture's own files from `add -A`, and identity + signing. Each of them fails
# the FIXTURE and would be read as a failure of the subject.
#
# `status.showUntrackedFiles` is deliberately NOT neutralised. The fixture must
# see what the operator's git sees — that setting is the subject of an arm below,
# and a fixture immune to it could not speak for the resolver at all.
mkdir -p "$WORK/nohooks" "$WORK/notemplate"
# An empty file, not /dev/null: git for Windows maps that to nul and refuses it.
: > "$WORK/noexcludes"
fixture_git() {
    git -C "$FIX" \
        -c core.hooksPath="$WORK/nohooks" \
        -c core.excludesFile="$WORK/noexcludes" \
        -c user.name=dn2cpp -c user.email=dn2cpp@invalid \
        -c commit.gpgsign=false "$@"
}
git init -q --template="$WORK/notemplate" "$FIX"
fixture_commit() {
    fixture_git add -A
    fixture_git commit -q -m "$1"
}
fixture_commit one
printf 'y\n' > "$FIX/src/b.txt"
fixture_commit two
FIX_HEAD="$(fixture_git rev-parse HEAD)"
FIX_PREV="$(fixture_git rev-parse HEAD~1)"

# pin_try PIN — the resolver run inside the fixture. A subshell for the cd, so
# the gate's own working directory (the repo root _common.sh moved us to) is
# what every other section still sees.
pin_try() {
    PIN_OUT="$( cd "$FIX" && dn2cpp_commit_pin_resolve "$1" 2>&1 \
                && printf 'RESOLVED=%s\n' "$DN2CPP_PIN_COMMIT" )" && PIN_RC=0 || PIN_RC=$?
}

pin_try 0123456789012345678901234567890123456789
refused "a commit the repository does not have" "$PIN_RC" "$PIN_OUT" \
    "is not a commit in this repository" "git fetch origin"

pin_try "$FIX_PREV"
refused "a real commit that is not HEAD" "$PIN_RC" "$PIN_OUT" \
    "but this tree is at $FIX_HEAD" "git checkout $FIX_PREV"

pin_try "$FIX_HEAD"
if [ "$PIN_RC" -eq 0 ] && [ "$PIN_OUT" = "RESOLVED=$FIX_HEAD" ]; then
    ok "HEAD on a clean tree resolves to the full sha"
else
    bad "HEAD on a clean tree was refused, or resolved to something else: rc=$PIN_RC out='$PIN_OUT'"
fi

printf 'edited\n' >> "$FIX/src/a.txt"
pin_try "$FIX_HEAD"
refused "an uncommitted edit to a tracked file" "$PIN_RC" "$PIN_OUT" \
    "the working tree is not $FIX_HEAD" " M src/a.txt" "Commit or stash them"
fixture_git checkout -q -- src/a.txt

printf 'stray\n' > "$FIX/src/stray.txt"
pin_try "$FIX_HEAD"
refused "an untracked file the compiler would read" "$PIN_RC" "$PIN_OUT" \
    "the working tree is not $FIX_HEAD" "?? src/stray.txt"

# The same file, in a repository configured to not mention untracked files. The
# setting is the fixture's own, not the host's: a `git status --porcelain` that
# does not say --untracked-files reads this tree as clean, and the pin would then
# vouch for a commit whose content was never built. Nothing else here would
# notice — the arm above stays green on a machine whose git is configured the
# ordinary way, which is how this reached a review in the first place.
fixture_git config status.showUntrackedFiles no
pin_try "$FIX_HEAD"
refused "an untracked file, in a repo configured to hide untracked files" \
    "$PIN_RC" "$PIN_OUT" "the working tree is not $FIX_HEAD" "?? src/stray.txt"
fixture_git config --unset status.showUntrackedFiles
rm -f "$FIX/src/stray.txt"

# ── 2/5 the pinned bundle's own CLI ──────────────────────────────────────────
# The tree being the pinned commit says nothing about a binary built elsewhere,
# and under --dn2cpp-bin the stamp beside it is the only witness. Driven against
# a fixture rather than the real artifacts/: asking about the real one needs this
# tree to be clean AND at the pin, which a suite run cannot promise — and an
# assertion that runs only when it happens to be both is the fail-open shape.
echo "== 2/5 dn2cpp_pin_bin_assert — a named binary's stamp =="
BINFIX="$WORK/bin"
mkdir -p "$BINFIX"
: > "$BINFIX/dn2cpp"
PIN_SHA=1111111111111111111111111111111111111111

bin_try() {
    BIN_OUT="$(dn2cpp_pin_bin_assert "$BINFIX/dn2cpp" "$1" "$PIN_SHA" 2>&1)" && BIN_RC=0 || BIN_RC=$?
}

printf 'abc123\n' > "$BINFIX/dn2cpp.src-hash"
bin_try abc123
if [ "$BIN_RC" -eq 0 ] && [ -z "$BIN_OUT" ]; then
    ok "a binary stamped with the sources now in the tree"
else
    bad "a matching stamp was refused: rc=$BIN_RC out='$BIN_OUT'"
fi

bin_try def456
refused "a binary stamped with other sources" "$BIN_RC" "$BIN_OUT" \
    "was not built from dn2cpp $PIN_SHA" "stamped abc123 != def456" \
    "gates/selfhost-emit.sh"

rm -f "$BINFIX/dn2cpp.src-hash"
bin_try abc123
refused "a binary with no stamp at all (provenance unknown, not a pass)" \
    "$BIN_RC" "$BIN_OUT" "stamped <no stamp> != abc123"

# The three arms above prove the helper; this proves it is still WIRED. By name
# and not by execution, because reaching that line for real needs this tree
# clean and at the pin — the condition the section header refuses to depend on.
# Deleting the call is otherwise invisible here: the arms would stay green over
# a pinned bundle that had stopped asking about its own binary.
if grep -q '^    dn2cpp_pin_bin_assert "\$DN2CPP_BIN"' dist/package-toolchain.sh; then
    ok "dist/package-toolchain.sh's pinned arm still calls it"
else
    bad "dist/package-toolchain.sh no longer calls dn2cpp_pin_bin_assert on its --dn2cpp-bin — a pinned bundle would carry an unvouched-for CLI"
fi

# ── 3/5 the editor packagers demand a pin at all ─────────────────────────────
# The arm that makes the rest reachable: without the flag there is no pin to
# verify, and the metadata falls back to whatever HEAD said. Both scripts exit
# on it before touching a fork, a host check or a file, so this costs nothing
# and runs on either host.
echo "== 3/5 the editor packagers' required --dn2cpp-commit =="
V=4.7.1-dn2cpp.3.1
for lane in macos windows; do
    out="$(bash "dist/package-editor-$lane.sh" --version "$V" 2>&1)" && rc=0 || rc=$?
    case "$lane" in
        macos)   hint='$(git rev-parse HEAD)' ;;
        windows) hint='editor-macos.metadata' ;;
    esac
    refused "dist/package-editor-$lane.sh without --dn2cpp-commit" "$rc" "$out" \
        "--dn2cpp-commit is required" "$hint"
done

# ── 4/5 dist/release-github.sh's cross-lane agreement ────────────────────────
echo "== 4/5 dist/release-github.sh — one value per release, across the lanes =="
STUB="$WORK/stub"
mkdir -p "$STUB"
cat > "$STUB/gh" <<'STUBEOF'
#!/bin/sh
if [ "$1 $2" = "auth status" ]; then echo "stub gh: authenticated"; exit 0; fi
echo "stub gh: this gate's arms must die before any release is read; got: gh $*" >&2
exit 1
STUBEOF
chmod +x "$STUB/gh"

PRISTINE="$WORK/release-pristine"
REL="$WORK/release"
mkdir -p "$PRISTINE"
FORK_SHA=aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa
BASE_SHA=bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb
DN2_SHA=cccccccccccccccccccccccccccccccccccccccc
lane_asset_name() {
    case "$1" in
        editor-macos)   printf 'Godot-%s-macos-arm64.zip\n' "$V" ;;
        editor-windows) printf 'Godot-%s-windows-x86_64.zip\n' "$V" ;;
        web)            printf 'godot-%s-web-template.zip\n' "$V" ;;
        macos)          printf 'Godot-%s-macos-template.zip\n' "$V" ;;
    esac
}
# The keys are the lane table's own demand (dist/release-github.sh, lane_row):
# section 2 requires every one of them non-empty before an agreement is asked.
for lane in editor-macos editor-windows web macos; do
    asset="$(lane_asset_name "$lane")"
    : > "$PRISTINE/$asset"
    sha="$(shasum -a 256 "$PRISTINE/$asset" | awk '{print $1}')"
    {
        printf 'release_version=%s\nasset=%s\nasset_sha256=%s\n' "$V" "$asset" "$sha"
        case "$lane" in
            editor-*)
                printf 'fork_commit=%s\nbase_pin=%s\nengine_provenance=stub engine\n' "$FORK_SHA" "$BASE_SHA"
                printf 'editor_version_string=4.7.1.stable.mono\ndn2cpp_commit=%s\n' "$DN2_SHA"
                printf 'toolchain_content_hash=stubhash\ncorelib_framework=10.0.0\n'
                printf 'prebuilt_axes=host\ncmake_version=0.0.0\nninja_version=0.0.0\nnode_version=0.0.0\n' ;;
            web)
                printf 'engine_provenance=stub engine\nemcc=stub emcc 6.0.5-git (abcdef)\nemsdk_version=6.0.5\n' ;;
            macos)
                printf 'base_pin=%s\nupstream_template=stubsha\n' "$BASE_SHA" ;;
        esac
    } > "$PRISTINE/$lane.metadata"
done
( cd "$PRISTINE" && for lane in editor-macos editor-windows web macos; do
    asset="$(lane_asset_name "$lane")"
    sha="$(shasum -a 256 "$asset")"
    printf '%s  %s\n' "${sha%% *}" "$asset"
done > SHA256SUMS.txt )

# meta_set FILE KEY VALUE — rewrite one key in place, the whole file through awk
# so no other line can be reformatted by the edit.
meta_set() {
    awk -F= -v k="$2" -v v="$3" '$1 == k { print k "=" v; next } { print }' "$1" > "$1.new"
    mv -f "$1.new" "$1"
}

# rel_try — the release script over $REL, restored from pristine first, with the
# caller's `meta_set` edits applied in between.
rel_reset() { rm -rf "$REL"; cp -R "$PRISTINE" "$REL"; }
rel_try() {
    REL_OUT="$(PATH="$STUB:$PATH" bash dist/release-github.sh --dry-run \
        --version "$V" --prev-version 4.7.1-dn2cpp.3 --out "$REL" \
        --lane editor-macos --lane editor-windows --lane web --lane macos 2>&1)" \
        && REL_RC=0 || REL_RC=$?
}

# The positive control, and the only arm that must NOT be refused here: the
# lanes agree, so the run walks past every agreement check and dies further
# down, on a fork commit no clone has. Without this arm the four below would
# all pass on a script that refused everything.
rel_reset
rel_try
if [ "$REL_RC" -ne 0 ] && [[ "$REL_OUT" != *disagree* ]] \
        && [[ "$REL_OUT" == *"-- engine_provenance:"* ]]; then
    ok "lanes that agree walk past every agreement check"
else
    bad "agreeing lanes did not get past the agreement checks: rc=$REL_RC"
    printf '%s\n' "$REL_OUT" | sed 's/^/        | /' >&2
fi

rel_reset
meta_set "$REL/editor-windows.metadata" dn2cpp_commit dddddddddddddddddddddddddddddddddddddddd
rel_try
refused "two editors, two dn2cpp commits" "$REL_RC" "$REL_OUT" \
    "disagree on dn2cpp_commit" \
    "editor-macos.metadata: dn2cpp_commit=$DN2_SHA" \
    "dist/package-editor-windows.sh --version $V --dn2cpp-commit"

rel_reset
meta_set "$REL/web.metadata" engine_provenance "another engine"
rel_try
refused "two engine trees" "$REL_RC" "$REL_OUT" \
    "disagree on engine_provenance" "dist/package-web-template.sh --version $V"

rel_reset
meta_set "$REL/macos.metadata" base_pin eeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeee
rel_try
refused "two upstream base pins" "$REL_RC" "$REL_OUT" \
    "disagree on base_pin" "dist/package-macos-template.sh --version $V"

rel_reset
meta_set "$REL/editor-windows.metadata" corelib_framework 9.9.9
rel_try
refused "two .NET SDKs" "$REL_RC" "$REL_OUT" \
    "disagree on corelib_framework" "re-package the stale one"

# ── 5/5 uploaded lanes and the simplified public notes ───────────────────────
# This fixture reaches the read-only GitHub view and renderer. git supplies the
# two repository identities the release script asks about; gh serves one draft,
# its asset digests and its body. Any command outside that read-only vocabulary
# is a gate failure, so --dry-run cannot accidentally mutate external state.
echo "== 5/5 dist/release-github.sh — uploaded lanes and public-note bindings =="
FULL_STUB="$WORK/full-stub"
FULL_FORK_ROOT="$WORK/full-fork-root"
FULL_FORK="$WORK/full-fork"
FULL_BODY="$WORK/release-body.md"
mkdir -p "$FULL_STUB" "$FULL_FORK_ROOT" "$FULL_FORK/modules/mono/mono_gd"
: > "$FULL_FORK/modules/mono/mono_gd/gd_mono.cpp"
printf '%s\n' "$FULL_FORK" > "$FULL_FORK_ROOT/clone.txt"

cat > "$FULL_STUB/git" <<'GITEOF'
#!/bin/sh
repo=dn2cpp
if [ "$1" = -C ]; then
    repo="$2"
    shift 2
fi
case "$1" in
    rev-parse)
        case "$*" in
            *refs/tags/*) exit 1 ;;
            *--short*)
                if [ "$repo" = "$TEST_FORK_DIR" ]; then
                    printf '%.7s\n' "$TEST_FORK_SHA"
                else
                    printf '%.7s\n' "$TEST_DN2_SHA"
                fi ;;
            *)
                if [ "$repo" = "$TEST_FORK_DIR" ]; then
                    printf '%s\n' "$TEST_FORK_SHA"
                else
                    printf '%s\n' "$TEST_DN2_SHA"
                fi ;;
        esac ;;
    status) exit 0 ;;
    merge-base) exit 0 ;;
    ls-remote)
        case " $* " in
            *' --tags '*) exit 0 ;;
            *' refs/heads/dn2cpp/main '*)
                printf '%s\trefs/heads/dn2cpp/main\n' "$TEST_FORK_SHA" ;;
            *' refs/heads/main '*)
                printf '%s\trefs/heads/main\n' "$TEST_DN2_SHA" ;;
            *)
                echo "stub git: unsupported ls-remote: $*" >&2
                exit 1 ;;
        esac ;;
    *)
        echo "stub git: this dry-run must not execute: git $*" >&2
        exit 1 ;;
esac
GITEOF

cat > "$FULL_STUB/gh" <<'GHEOF'
#!/bin/sh
if [ "$1 $2" = "auth status" ]; then
    echo "stub gh: authenticated"
    exit 0
fi
if [ "$1 $2" != "release view" ]; then
    echo "stub gh: this dry-run must only view the release; got: gh $*" >&2
    exit 1
fi
case " $* " in
    *' --json isDraft '*) printf 'true\n' ;;
    *' --json assets '*)
        while IFS= read -r row; do
            hash="${row%%  *}"
            name="${row#*  }"
            if [ "${TEST_BAD_DIGEST_ASSET:-}" = "$name" ]; then
                hash=ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff
            fi
            printf '%s\tsha256:%s\n' "$name" "$hash"
        done < "$TEST_RELEASE_DIR/SHA256SUMS.txt" ;;
    *' --json body '*)
        cat "$TEST_RELEASE_BODY" ;;
    *) exit 0 ;;
esac
GHEOF
chmod +x "$FULL_STUB/git" "$FULL_STUB/gh"

public_body_write() {
    {
        printf 'engine provenance: stub engine\n'
        printf 'dn2cpp commit: %s\n' "$DN2_SHA"
        printf 'Web emcc: stub emcc 6.0.5-git (abcdef)\n'
        printf 'Web Emscripten: 6.0.5\n'
        printf 'macOS upstream template sha256: stubsha\n'
    } > "$FULL_BODY"
}

public_body_drop() {
    grep -vF "$1" "$FULL_BODY" > "$FULL_BODY.new" || true
    mv -f "$FULL_BODY.new" "$FULL_BODY"
}

full_reset() {
    rel_reset
    # macos is an uploaded-only lane: its checksum row and metadata remain here,
    # but only GitHub's digest can speak for the absent asset bytes. Web stays
    # local so the same fixture also exercises shasum and lane_zip validation.
    rm -f "$REL/$(lane_asset_name macos)"
    public_body_write
    FULL_BAD_DIGEST=
}

full_try() {
    FULL_OUT="$(TEST_FORK_DIR="$FULL_FORK" TEST_FORK_SHA="$FORK_SHA" \
        TEST_DN2_SHA="$DN2_SHA" TEST_RELEASE_DIR="$REL" \
        TEST_RELEASE_BODY="$FULL_BODY" TEST_BAD_DIGEST_ASSET="$FULL_BAD_DIGEST" \
        DN2CPP_GODOT_FORK_ROOT="$FULL_FORK_ROOT" \
        DN2CPP_GODOT_FORK_CLONE="$FULL_FORK" PATH="$FULL_STUB:$PATH" \
        bash dist/release-github.sh --dry-run --version "$V" \
        --prev-version 4.7.1-dn2cpp.3 --out "$REL" \
        --uploaded-lane editor-macos --uploaded-lane editor-windows \
        --uploaded-lane web --uploaded-lane macos 2>&1)" \
        && FULL_RC=0 || FULL_RC=$?
}

# Values retained in metadata for packaging and tag integrity are intentionally
# absent from the public body. Make the host-local ones differ as well: if the
# uploaded check regresses to all required keys, this positive control fails.
full_reset
meta_set "$REL/editor-macos.metadata" editor_version_string macos-editor-version
meta_set "$REL/editor-windows.metadata" editor_version_string windows-editor-version
meta_set "$REL/editor-macos.metadata" toolchain_content_hash macos-toolchain-hash
meta_set "$REL/editor-windows.metadata" toolchain_content_hash windows-toolchain-hash
meta_set "$REL/editor-macos.metadata" prebuilt_axes macos-axes
meta_set "$REL/editor-windows.metadata" prebuilt_axes windows-axes
full_try
if [ "$FULL_RC" -eq 0 ] && [[ "$FULL_OUT" == *"every precondition passed"* ]] \
        && [[ "$FULL_OUT" == *"3 row(s) checked against the files here, 1 against the release's digests"* ]]; then
    ok "an uploaded draft regenerates notes without body witnesses for internal metadata"
else
    bad "the simplified uploaded draft did not regenerate: rc=$FULL_RC"
    printf '%s\n' "$FULL_OUT" | sed 's/^/        | /' >&2
fi

rendered="$REL/RELEASE-NOTES.md"
render_faults=
[ -f "$rendered" ] || render_faults="$render_faults missing output"
if [ -f "$rendered" ]; then
    grep -nE '@@|<!--|^## アセット$|^>' "$rendered" > "$WORK/render-faults" || true
    [ ! -s "$WORK/render-faults" ] || render_faults="$render_faults $(tr '\n' ' ' < "$WORK/render-faults")"
fi
if [ -z "$render_faults" ]; then
    ok "rendered notes contain no placeholders, lane markers, asset table or trailing blockquote"
else
    bad "rendered notes retain removed or unrendered material:$render_faults"
fi

# Every value still published has the existing body as its uploaded-lane
# witness. Web provenance is one public item backed by two scalar metadata
# values, and both must remain visible.
for spec in \
    "engine provenance:stub engine:engine_provenance=stub engine" \
    "dn2cpp commit:$DN2_SHA:dn2cpp_commit=$DN2_SHA" \
    "Web emcc:stub emcc 6.0.5-git (abcdef):emcc=stub emcc 6.0.5-git (abcdef)" \
    "macOS template:stubsha:upstream_template=stubsha"; do
    label="${spec%%:*}"
    rest="${spec#*:}"
    value="${rest%%:*}"
    needle="${rest#*:}"
    full_reset
    public_body_drop "$value"
    full_try
    refused "$label missing from an uploaded release body" "$FULL_RC" "$FULL_OUT" "$needle"
done

full_reset
meta_set "$REL/web.metadata" emsdk_version 9.9.9
full_try
refused "an uploaded Web lane whose Emscripten value differs" "$FULL_RC" "$FULL_OUT" \
    "emsdk_version=9.9.9"

full_reset
FULL_BAD_DIGEST="$(lane_asset_name macos)"
full_try
refused "an uploaded asset whose GitHub digest differs" "$FULL_RC" "$FULL_OUT" \
    "release $V serves '$(lane_asset_name macos)' as sha256:ffffffff" \
    "macos.metadata says asset_sha256="

# With the remote digest changed to the same value as metadata, the byte witness
# passes and the independent checksum-file row must be what refuses the lane.
full_reset
remote_wrong=ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff
meta_set "$REL/macos.metadata" asset_sha256 "$remote_wrong"
FULL_BAD_DIGEST="$(lane_asset_name macos)"
full_try
refused "an uploaded-only lane whose checksum row differs from metadata" \
    "$FULL_RC" "$FULL_OUT" \
    "SHA256SUMS.txt gives '$(lane_asset_name macos)' as" \
    "macos.metadata says $remote_wrong"

full_reset
web_asset="$(lane_asset_name web)"
web_sha="$(awk -v n="$web_asset" '$2 == n { print $1 }' "$REL/SHA256SUMS.txt")"
wrong_sha=eeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeee
awk -v n="$web_asset" -v h="$wrong_sha" '$2 == n { print h "  " n; next } { print }' \
    "$REL/SHA256SUMS.txt" > "$REL/SHA256SUMS.txt.new"
mv -f "$REL/SHA256SUMS.txt.new" "$REL/SHA256SUMS.txt"
full_try
refused "SHA256SUMS.txt whose hash differs from a local asset" "$FULL_RC" "$FULL_OUT" \
    "SHA256SUMS.txt does not describe the files in $REL" "FAILED"
[ -n "$web_sha" ] || bad "the Web fixture had no checksum row"

full_reset
meta_set "$REL/web.metadata" asset_sha256 "$wrong_sha"
full_try
refused "metadata whose SHA-256 differs from its asset" "$FULL_RC" "$FULL_OUT" \
    "web.metadata says asset_sha256=$wrong_sha" "hashes to"

echo
if [ "$FAILS" -ne 0 ]; then
    printf 'error: %d of %d release preconditions did not refuse what they exist to refuse.\n' \
        "$FAILS" "$CHECKS" >&2
    exit 1
fi
printf '== OK — %d release preconditions checked, each refused its own input ==\n' "$CHECKS"

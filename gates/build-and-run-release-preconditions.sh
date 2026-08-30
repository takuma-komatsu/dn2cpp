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
# Five fixtures, because the subjects ask different questions:
#   * a throwaway git repository for `dn2cpp_commit_pin_resolve`, which asks
#     about the tree it is run in. Asking it about THIS tree would mean dirtying
#     the tree the rest of the suite is hashing, and would answer differently
#     depending on whether the developer had uncommitted work.
#   * a directory holding a file called `dn2cpp` and a stamp beside it, for
#     `dn2cpp_pin_bin_assert` — which compares two arguments and reads nothing
#     ambient, which is what lets a fixture supply both sides.
#   * a synthetic Windows fork and Python writer for the template packager's
#     failure path, where the public archive must remain unchanged.
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

# ── 1/6 the pin resolver's arms ──────────────────────────────────────────────
echo "== 1/6 dn2cpp_commit_pin_resolve — against a throwaway repository =="
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

# ── 2/6 the pinned bundle's own CLI ──────────────────────────────────────────
# The tree being the pinned commit says nothing about a binary built elsewhere,
# and under --dn2cpp-bin the stamp beside it is the only witness. Driven against
# a fixture rather than the real artifacts/: asking about the real one needs this
# tree to be clean AND at the pin, which a suite run cannot promise — and an
# assertion that runs only when it happens to be both is the fail-open shape.
echo "== 2/6 dn2cpp_pin_bin_assert — a named binary's stamp =="
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

# ── 3/6 the editor packagers demand a pin at all ─────────────────────────────
# The arm that makes the rest reachable: without the flag there is no pin to
# verify, and the metadata falls back to whatever HEAD said. Both scripts exit
# on it before touching a fork, a host check or a file, so this costs nothing
# and runs on either host.
echo "== 3/6 the editor packagers' required --dn2cpp-commit =="
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

# ── 4/6 the Windows template packager's atomic publication ───────────────────
echo "== 4/6 dist/package-windows-template.sh — preserve the public ZIP on failure =="
WIN_PKG="$WORK/windows-package"
WIN_PKG_FORK="$WIN_PKG/fork"
WIN_PKG_ROOT="$WIN_PKG/root"
WIN_PKG_OUT="$WIN_PKG/out"
WIN_PKG_STUB="$WIN_PKG/stub"
WIN_PKG_PY="$(resolve_python)"
mkdir -p "$WIN_PKG_FORK/modules/mono/mono_gd" "$WIN_PKG_ROOT" \
    "$WIN_PKG_OUT" "$WIN_PKG_STUB"
: > "$WIN_PKG_FORK/modules/mono/mono_gd/gd_mono.cpp"
printf '%s\n' "$WIN_PKG_FORK" > "$WIN_PKG_ROOT/clone.txt"

cat > "$WIN_PKG_STUB/git" <<'GITEOF'
#!/bin/sh
case "$1" in
    diff|ls-files) exit 0 ;;
    *) echo "stub git: unsupported command: $*" >&2; exit 1 ;;
esac
GITEOF
cat > "$WIN_PKG_STUB/uname" <<'UNAMEEOF'
#!/bin/sh
case "$1" in
    -m) echo x86_64 ;;
    -s) echo MINGW64_NT ;;
    *) echo MINGW64_NT ;;
esac
UNAMEEOF
cat > "$WIN_PKG_STUB/chcp.com" <<'CHCPEOF'
#!/bin/sh
exit 0
CHCPEOF
cat > "$WIN_PKG_STUB/python3" <<'PYEOF'
#!/bin/sh
if [ "$1" = -c ]; then
    # shellcheck disable=SC2086 -- resolve_python may answer `py -3`.
    exec $TEST_REAL_PY "$@"
fi
if [ "$1" = - ] && [ ! -f "$TEST_PY_SEEN" ]; then
    : > "$TEST_PY_SEEN"
    printf 'incomplete archive\n' > "$2"
    exit 0
fi
# shellcheck disable=SC2086 -- resolve_python may answer `py -3`.
exec $TEST_REAL_PY "$@"
PYEOF
chmod +x "$WIN_PKG_STUB/git" "$WIN_PKG_STUB/uname" \
    "$WIN_PKG_STUB/chcp.com" "$WIN_PKG_STUB/python3"

for config in release debug; do
    template="$WIN_PKG/$config.exe"
    cat > "$template" <<'TEMPLATEEOF'
#!/bin/sh
echo 4.7.1.stable.mono.custom_build.fixture
TEMPLATEEOF
    chmod +x "$template"
done

WIN_PKG_BASE="$(head -1 gates/expected/godot-fork-pin.txt)"
WIN_PKG_ENGINE_HASH="$(printf 'base %s\n' "$WIN_PKG_BASE" | shasum -a 256 | cut -c1-16)"
WIN_PKG_PROVENANCE="engine=$WIN_PKG_ENGINE_HASH base=$WIN_PKG_BASE"
printf '%s\n' "$WIN_PKG_PROVENANCE" > "$WIN_PKG/release.exe.provenance"
printf '%s\n' "$WIN_PKG_PROVENANCE" > "$WIN_PKG/debug.exe.provenance"

WIN_PKG_ASSET="$WIN_PKG_OUT/godot-$V-windows-x86_64-templates.zip"
printf 'previous published archive\n' > "$WIN_PKG_ASSET"
cp "$WIN_PKG_ASSET" "$WIN_PKG/asset-before.zip"
WIN_PKG_OUT_TEXT="$(TEST_REAL_PY="$WIN_PKG_PY" TEST_PY_SEEN="$WIN_PKG/python-used" \
    DN2CPP_PYTHON="$WIN_PKG_STUB/python3" DN2CPP_OS=windows \
    DN2CPP_GODOT_FORK_ROOT="$WIN_PKG_ROOT" \
    DN2CPP_GODOT_FORK_CLONE="$WIN_PKG_FORK" PATH="$WIN_PKG_STUB:$PATH" \
    bash dist/package-windows-template.sh --version "$V" --out "$WIN_PKG_OUT" \
    --release-src "$WIN_PKG/release.exe" --debug-src "$WIN_PKG/debug.exe" 2>&1)" \
    && WIN_PKG_RC=0 || WIN_PKG_RC=$?
WIN_PKG_FAULT=
[ "$WIN_PKG_RC" -ne 0 ] || WIN_PKG_FAULT="$WIN_PKG_FAULT package unexpectedly succeeded;"
cmp -s "$WIN_PKG/asset-before.zip" "$WIN_PKG_ASSET" \
    || WIN_PKG_FAULT="$WIN_PKG_FAULT public archive changed;"
if compgen -G "$WIN_PKG_OUT/.$(basename "$WIN_PKG_ASSET").tmp.*" >/dev/null; then
    WIN_PKG_FAULT="$WIN_PKG_FAULT temporary archive remained;"
fi
if [ -z "$WIN_PKG_FAULT" ]; then
    ok "a failed Windows archive verification preserves the published ZIP"
else
    bad "Windows archive failure was not atomic:$WIN_PKG_FAULT rc=$WIN_PKG_RC"
    printf '%s\n' "$WIN_PKG_OUT_TEXT" | sed 's/^/        | /' >&2
fi

# ── 5/6 dist/release-github.sh's cross-lane agreement ────────────────────────
echo "== 5/6 dist/release-github.sh — one value per release, across the lanes =="
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
WIN_RELEASE_NAME=godot_windows_release_x86_64.exe
WIN_DEBUG_NAME=godot_windows_debug_x86_64.exe
WEB_RELEASE_NAME=godot_web_release.zip
WEB_DEBUG_NAME=godot_web_debug.zip
printf 'release template fixture\n' > "$WORK/$WIN_RELEASE_NAME"
printf 'debug template fixture\n' > "$WORK/$WIN_DEBUG_NAME"
printf 'web release template fixture\n' > "$WORK/$WEB_RELEASE_NAME"
printf 'web debug template fixture\n' > "$WORK/$WEB_DEBUG_NAME"
WIN_RELEASE_SHA="$(shasum -a 256 "$WORK/$WIN_RELEASE_NAME" | awk '{print $1}')"
WIN_DEBUG_SHA="$(shasum -a 256 "$WORK/$WIN_DEBUG_NAME" | awk '{print $1}')"
WEB_RELEASE_SHA="$(shasum -a 256 "$WORK/$WEB_RELEASE_NAME" | awk '{print $1}')"
WEB_DEBUG_SHA="$(shasum -a 256 "$WORK/$WEB_DEBUG_NAME" | awk '{print $1}')"
PY="$(resolve_python)"
lane_asset_name() {
    case "$1" in
        editor-macos)   printf 'Godot-%s-macos-arm64.zip\n' "$V" ;;
        editor-windows) printf 'Godot-%s-windows-x86_64.zip\n' "$V" ;;
        windows)        printf 'godot-%s-windows-x86_64-templates.zip\n' "$V" ;;
        web)            printf 'godot-%s-web-templates.zip\n' "$V" ;;
        macos)          printf 'Godot-%s-macos-template.zip\n' "$V" ;;
    esac
}
# The keys are the lane table's own demand (dist/release-github.sh, lane_row):
# section 2 requires every one of them non-empty before an agreement is asked.
for lane in editor-macos editor-windows windows web macos; do
    asset="$(lane_asset_name "$lane")"
    if [ "$lane" = windows ]; then
        # A real archive makes the positive path exercise the inner contract;
        # empty stand-ins can only test checks that run before zip inspection.
        # shellcheck disable=SC2086 -- resolve_python may answer `py -3`.
        $PY - "$PRISTINE/$asset" "$WORK/$WIN_RELEASE_NAME" "$WIN_RELEASE_NAME" \
                "$WORK/$WIN_DEBUG_NAME" "$WIN_DEBUG_NAME" <<'PY'
import sys
import zipfile

archive, release_path, release_name, debug_path, debug_name = sys.argv[1:]
with zipfile.ZipFile(archive, "w", zipfile.ZIP_DEFLATED) as z:
    z.write(release_path, release_name)
    z.write(debug_path, debug_name)
PY
    elif [ "$lane" = web ]; then
        # Web is likewise a nested pair; release validation checks its exact
        # member set and hashes, not merely the outer digest.
        # shellcheck disable=SC2086 -- resolve_python may answer `py -3`.
        $PY - "$PRISTINE/$asset" "$WORK/$WEB_RELEASE_NAME" "$WEB_RELEASE_NAME" \
                "$WORK/$WEB_DEBUG_NAME" "$WEB_DEBUG_NAME" <<'PY'
import sys
import zipfile

archive, release_path, release_name, debug_path, debug_name = sys.argv[1:]
with zipfile.ZipFile(archive, "w", zipfile.ZIP_STORED) as z:
    z.write(release_path, release_name)
    z.write(debug_path, debug_name)
PY
    else
        : > "$PRISTINE/$asset"
    fi
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
                printf 'flavor=stock\nengine_provenance=stub engine\nemcc=stub emcc 6.0.5-git (abcdef)\nemsdk_version=6.0.5\n'
                printf 'release_template=%s\nrelease_template_sha256=%s\n' "$WEB_RELEASE_NAME" "$WEB_RELEASE_SHA"
                printf 'debug_template=%s\ndebug_template_sha256=%s\n' "$WEB_DEBUG_NAME" "$WEB_DEBUG_SHA" ;;
            windows)
                printf 'architecture=x86_64\nbase_pin=%s\nengine_provenance=stub engine\n' "$BASE_SHA"
                printf 'release_template=%s\n' "$WIN_RELEASE_NAME"
                printf 'release_template_sha256=%s\n' "$WIN_RELEASE_SHA"
                printf 'release_template_version_string=4.7.1.stable.mono.custom_build.stub\n'
                printf 'debug_template=%s\n' "$WIN_DEBUG_NAME"
                printf 'debug_template_sha256=%s\n' "$WIN_DEBUG_SHA"
                printf 'debug_template_version_string=4.7.1.stable.mono.custom_build.stub\n' ;;
            macos)
                printf 'base_pin=%s\nupstream_template=stubsha\n' "$BASE_SHA" ;;
        esac
    } > "$PRISTINE/$lane.metadata"
done
( cd "$PRISTINE" && for lane in editor-macos editor-windows windows web macos; do
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

meta_drop() {
    awk -F= -v k="$2" '$1 != k' "$1" > "$1.new"
    mv -f "$1.new" "$1"
}

# rel_try — the release script over $REL, restored from pristine first, with the
# caller's `meta_set` edits applied in between.
rel_reset() { rm -rf "$REL"; cp -R "$PRISTINE" "$REL"; }
rel_try() {
    REL_OUT="$(PATH="$STUB:$PATH" bash dist/release-github.sh --dry-run \
        --version "$V" --prev-version 4.7.1-dn2cpp.3 --out "$REL" \
        --lane editor-macos --lane editor-windows --lane windows --lane web --lane macos 2>&1)" \
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

# The Windows lane is one archive and one metadata row, but the row must retain
# the identity of both files inside it. This is the durable witness that the
# archive was assembled from distinct release and debug products rather than a
# renamed single-template package.
for key in release_template release_template_sha256 release_template_version_string \
        debug_template debug_template_sha256 debug_template_version_string; do
    rel_reset
    meta_drop "$REL/windows.metadata" "$key"
    rel_try
    refused "Windows template metadata without $key" "$REL_RC" "$REL_OUT" \
        "windows.metadata carries no '$key=' value"
done

rel_reset
meta_set "$REL/windows.metadata" debug_template "$WIN_RELEASE_NAME"
rel_try
refused "Windows metadata that names one executable for both configurations" \
    "$REL_RC" "$REL_OUT" \
    "names '$WIN_RELEASE_NAME' as both the release and debug template"

rel_reset
meta_set "$REL/windows.metadata" debug_template_sha256 "$WIN_RELEASE_SHA"
rel_try
refused "Windows metadata that gives both configurations the same bytes" \
    "$REL_RC" "$REL_OUT" \
    "release and debug templates the same sha256: $WIN_RELEASE_SHA"

rel_reset
meta_set "$REL/windows.metadata" release_template unexpected-release.exe
rel_try
refused "Windows metadata with a noncanonical release entry name" \
    "$REL_RC" "$REL_OUT" "names release template 'unexpected-release.exe'" \
    "expected godot_windows_release_x86_64.exe"

rel_reset
meta_set "$REL/windows.metadata" debug_template unexpected-debug.exe
rel_try
refused "Windows metadata with a noncanonical debug entry name" \
    "$REL_RC" "$REL_OUT" "names debug template 'unexpected-debug.exe'" \
    "expected godot_windows_debug_x86_64.exe"

rel_reset
windows_asset="$(lane_asset_name windows)"
# shellcheck disable=SC2086 -- resolve_python may answer `py -3`.
$PY - "$REL/$windows_asset" "$WORK/$WIN_RELEASE_NAME" \
        "$WORK/$WIN_DEBUG_NAME" "$WIN_DEBUG_NAME" <<'PY'
import sys
import zipfile

archive, release_path, debug_path, debug_name = sys.argv[1:]
with zipfile.ZipFile(archive, "w", zipfile.ZIP_DEFLATED) as z:
    z.write(release_path, "unexpected-release.exe")
    z.write(debug_path, debug_name)
PY
rel_try
refused "a Windows archive whose entry names differ from metadata" \
    "$REL_RC" "$REL_OUT" "Windows template archive" "unexpected-release.exe" \
    "expected exactly ['$WIN_RELEASE_NAME', '$WIN_DEBUG_NAME']"

rel_reset
wrong_inner_sha=eeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeee
meta_set "$REL/windows.metadata" release_template_sha256 "$wrong_inner_sha"
rel_try
refused "a Windows archive whose entry hash differs from metadata" \
    "$REL_RC" "$REL_OUT" \
    "$WIN_RELEASE_NAME hashes to $WIN_RELEASE_SHA, metadata says $wrong_inner_sha"

rel_reset
printf 'unexpected archive entry\n' > "$WORK/unexpected.txt"
windows_asset="$(lane_asset_name windows)"
# shellcheck disable=SC2086 -- resolve_python may answer `py -3`.
$PY - "$REL/$windows_asset" "$WORK/unexpected.txt" <<'PY'
import sys
import zipfile

with zipfile.ZipFile(sys.argv[1], "a", zipfile.ZIP_DEFLATED) as z:
    z.write(sys.argv[2], "unexpected.txt")
PY
rel_try
refused "a Windows archive with an undeclared entry" "$REL_RC" "$REL_OUT" \
    "entries are" "unexpected.txt" "expected exactly"

# Web exposes the same pair contract, with stock flavor as an additional public
# invariant. Each malformed nested archive must fail before ambient git state.
for key in flavor release_template release_template_sha256 \
        debug_template debug_template_sha256; do
    rel_reset
    meta_drop "$REL/web.metadata" "$key"
    rel_try
    refused "Web template metadata without $key" "$REL_RC" "$REL_OUT" \
        "web.metadata carries no '$key=' value"
done

rel_reset
meta_set "$REL/web.metadata" flavor cri
rel_try
refused "a non-stock public Web bundle" "$REL_RC" "$REL_OUT" \
    "web.metadata says flavor=cri" "must be stock"

rel_reset
meta_set "$REL/web.metadata" debug_template "$WEB_RELEASE_NAME"
rel_try
refused "Web metadata that names one inner ZIP for both configurations" \
    "$REL_RC" "$REL_OUT" \
    "names '$WEB_RELEASE_NAME' as both the release and debug template"

rel_reset
meta_set "$REL/web.metadata" debug_template_sha256 "$WEB_RELEASE_SHA"
rel_try
refused "Web metadata that gives both configurations the same bytes" \
    "$REL_RC" "$REL_OUT" \
    "release and debug templates the same sha256: $WEB_RELEASE_SHA"

rel_reset
meta_set "$REL/web.metadata" release_template unexpected-web-release.zip
rel_try
refused "Web metadata with a noncanonical release entry name" \
    "$REL_RC" "$REL_OUT" \
    "names release template 'unexpected-web-release.zip'" \
    "expected godot_web_release.zip"

rel_reset
meta_set "$REL/web.metadata" debug_template unexpected-web-debug.zip
rel_try
refused "Web metadata with a noncanonical debug entry name" \
    "$REL_RC" "$REL_OUT" \
    "names debug template 'unexpected-web-debug.zip'" \
    "expected godot_web_debug.zip"

rel_reset
meta_set "$REL/web.metadata" release_template_sha256 "$wrong_inner_sha"
rel_try
refused "a Web archive whose entry hash differs from metadata" \
    "$REL_RC" "$REL_OUT" \
    "$WEB_RELEASE_NAME hashes to $WEB_RELEASE_SHA, metadata says $wrong_inner_sha"

rel_reset
web_asset="$(lane_asset_name web)"
# shellcheck disable=SC2086 -- resolve_python may answer `py -3`.
$PY - "$REL/$web_asset" "$WORK/unexpected.txt" <<'PY'
import sys
import zipfile

with zipfile.ZipFile(sys.argv[1], "a", zipfile.ZIP_STORED) as z:
    z.write(sys.argv[2], "unexpected.txt")
PY
rel_try
refused "a Web archive with an undeclared entry" "$REL_RC" "$REL_OUT" \
    "entries are" "unexpected.txt" "expected exactly"

rel_reset
REL_OUT="$(PATH="$STUB:$PATH" bash dist/release-github.sh --dry-run \
    --version "$V" --prev-version 4.7.1-dn2cpp.3 --out "$REL" \
    --lane editor-macos --lane editor-windows --lane web --lane macos 2>&1)" \
    && REL_RC=0 || REL_RC=$?
refused "a Windows editor without its fork-built export template" "$REL_RC" "$REL_OUT" \
    "lanes 'editor-windows' and 'windows' must be released together"

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

# ── 6/6 uploaded lanes and the simplified public notes ───────────────────────
# This fixture reaches the read-only GitHub view and renderer. git supplies the
# two repository identities the release script asks about; gh serves one draft,
# its asset digests and its body. Any command outside that read-only vocabulary
# is a gate failure, so --dry-run cannot accidentally mutate external state.
echo "== 6/6 dist/release-github.sh — uploaded lanes and public-note bindings =="
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
    # macos and windows are uploaded-only lanes: their checksum rows and
    # metadata remain here, but only GitHub's digest can speak for the absent
    # asset bytes. Web stays local so the same fixture also exercises shasum and
    # lane_zip validation; section 4 exercises the local Windows ZIP itself.
    rm -f "$REL/$(lane_asset_name macos)"
    rm -f "$REL/$(lane_asset_name windows)"
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
        --uploaded-lane windows --uploaded-lane web --uploaded-lane macos 2>&1)" \
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
        && [[ "$FULL_OUT" == *"3 row(s) checked against the files here, 2 against the release's digests"* ]]; then
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

# With no Windows ZIP on this host, metadata is the only remaining witness for
# the release/debug distinction. These checks must therefore run before and
# independently of archive inspection.
full_reset
meta_set "$REL/windows.metadata" debug_template "$WIN_RELEASE_NAME"
full_try
refused "an uploaded-only Windows lane with one name for both templates" \
    "$FULL_RC" "$FULL_OUT" "as both the release and debug template"

full_reset
meta_set "$REL/windows.metadata" debug_template_sha256 "$WIN_RELEASE_SHA"
full_try
refused "an uploaded-only Windows lane with one hash for both templates" \
    "$FULL_RC" "$FULL_OUT" "release and debug templates the same sha256"

full_reset
meta_set "$REL/windows.metadata" release_template uploaded-release.exe
full_try
refused "uploaded-only Windows metadata with a noncanonical release name" \
    "$FULL_RC" "$FULL_OUT" "names release template 'uploaded-release.exe'" \
    "expected godot_windows_release_x86_64.exe"

full_reset
meta_set "$REL/windows.metadata" debug_template uploaded-debug.exe
full_try
refused "uploaded-only Windows metadata with a noncanonical debug name" \
    "$FULL_RC" "$FULL_OUT" "names debug template 'uploaded-debug.exe'" \
    "expected godot_windows_debug_x86_64.exe"

full_reset
rm -f "$REL/$(lane_asset_name web)"
meta_set "$REL/web.metadata" debug_template "$WEB_RELEASE_NAME"
full_try
refused "an uploaded-only Web lane with one name for both templates" \
    "$FULL_RC" "$FULL_OUT" "as both the release and debug template"

full_reset
rm -f "$REL/$(lane_asset_name web)"
meta_set "$REL/web.metadata" debug_template_sha256 "$WEB_RELEASE_SHA"
full_try
refused "an uploaded-only Web lane with one hash for both templates" \
    "$FULL_RC" "$FULL_OUT" "release and debug templates the same sha256"

full_reset
rm -f "$REL/$(lane_asset_name web)"
meta_set "$REL/web.metadata" flavor cri
full_try
refused "an uploaded-only non-stock Web lane" "$FULL_RC" "$FULL_OUT" \
    "web.metadata says flavor=cri" "must be stock"

full_reset
rm -f "$REL/$(lane_asset_name web)"
meta_set "$REL/web.metadata" release_template uploaded-release.zip
full_try
refused "uploaded-only Web metadata with a noncanonical release name" \
    "$FULL_RC" "$FULL_OUT" "names release template 'uploaded-release.zip'" \
    "expected godot_web_release.zip"

full_reset
rm -f "$REL/$(lane_asset_name web)"
meta_set "$REL/web.metadata" debug_template uploaded-debug.zip
full_try
refused "uploaded-only Web metadata with a noncanonical debug name" \
    "$FULL_RC" "$FULL_OUT" "names debug template 'uploaded-debug.zip'" \
    "expected godot_web_debug.zip"

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

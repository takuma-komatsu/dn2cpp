---
name: mac-release
description: Run and verify the dn2cpp macOS-side release workflow for the Godot fork, including pinned toolchain setup, self-host rebuild, Web/macOS/editor packaging, draft release creation, and the mandatory macOS-to-Windows handoff. Use when cutting a new release, rebuilding Mac release artifacts, preparing a Windows handoff, or diagnosing a missing handoff or draft-release asset.
---

# Mac Release Workflow

Read docs/RELEASE.md Phase 0, Phase A, and Phase B before acting. Treat that
document and the repository scripts as the source of truth. This skill fixes
the ordering and stop conditions that are easy to miss.

## Invariants

- Work from the dn2cpp checkout's clean main, including no untracked files.
  Ensure the exact commit is reachable from origin/main.
- Resolve the fork through gates/_godot_fork.sh. Keep its dn2cpp/main
  worktree clean and ensure its exact commit is reachable from
  origin/dn2cpp/main.
- Set a concrete V and its immediately preceding PREV release. Use the exact
  form <godot-version>-dn2cpp.<X>.<Y>; increment Y for a dn2cpp-only change
  and increment X with Y=0 when the fork moves.
- Treat a separately completed gates/pre-merge.sh run as a prerequisite. Do
  not launch it automatically in this workflow. If the user has not confirmed
  it, stop before packaging and request the result.
- Keep the release unpublished on Mac. Mac creates a draft; Windows completes
  and publishes it.
- Upload the handoff immediately after creating the draft. Do not report the
  Mac phase complete until GitHub shows
  internal-handoff-$V-macos-to-windows.tgz on that draft.

## Phase 0: inspect and prepare

Run the command blocks in one shell, or enable errexit and pipefail in each
shell:

~~~
set -e -o pipefail
~~~

Set concrete values before running commands:

~~~
V='REPLACE_WITH_NEW_RELEASE_VERSION'
PREV='REPLACE_WITH_PREVIOUS_RELEASE_VERSION'
REPO='takuma-komatsu/godot-dn2cpp'
~~~

Replace both placeholders before running anything. Before the confirmed
pre-merge run, update dist/release-notes-template.md's hand-written change
bullets and compare URL for this release. Commit and push that change, and
commit and push any required docs/EDITOR-GUIDE.ja.md change. The merge-gate
confirmation must refer to this exact dn2cpp HEAD; --prev-version supplies the
heading version, but it does not update the bullets or compare URL.

Confirm both values from the fork's tags/releases. Never infer them from a
stale artifact directory and never use the same value for both.

Inspect both repositories:

~~~
git fetch origin
git checkout main
git status --porcelain --untracked-files=all
git merge-base --is-ancestor HEAD origin/main
git rev-parse HEAD

source gates/_godot_fork.sh
godot_fork_resolve
git -C "$FORK" fetch --tags --prune --prune-tags --force origin
git -C "$FORK" checkout dn2cpp/main
git -C "$FORK" status --porcelain --untracked-files=no
git -C "$FORK" merge-base --is-ancestor HEAD origin/dn2cpp/main
git -C "$FORK" rev-parse HEAD
echo "fork_root=$FORK_ROOT"
echo "fork=$FORK"
~~~

Require empty status output and successful ancestry checks. Push intended
commits before continuing; no release script pushes them. Run gh auth status
in the same permission context that will run the release and handoff commands.
If authentication works only in an approved/escalated shell, use that context
for all GitHub calls and for scripts that write the fork cache.

For a new version, move the entire old output directory aside. Resolve the
destination first and never overwrite an existing directory:

~~~
if [ -e "artifacts/release-$PREV" ]; then
    echo "error: artifacts/release-$PREV already exists; inspect it before moving anything" >&2
    exit 1
fi
if [ -d artifacts/release ]; then
    mv artifacts/release artifacts/release-$PREV
fi
~~~

Do not copy selected files or leave old metadata in artifacts/release.

## Phase A: build the Mac lanes

Run these commands in order, using each setup script's default output path:

~~~
./gates/setup-buildtools.sh
./gates/setup-emsdk.sh
./gates/selfhost-emit.sh
./gates/setup-godot-fork.sh 2>&1 | tee /tmp/setup-godot-fork.log
./gates/setup-godot-fork-web.sh 2>&1 | tee /tmp/setup-godot-fork-web.log

./dist/package-web-template.sh --version "$V"
./dist/package-macos-template.sh --version "$V"
./dist/package-editor-macos.sh --version "$V" --dn2cpp-commit "$(git rev-parse HEAD)" 2>&1 | tee /tmp/pkg-editor-macos.log
~~~

Do not pass --out to either setup script, use CRI=1, disable the editor
packager's default smoke, or use --allow-partial-prebuilt. The Web package
must precede the macOS template, and both must precede the editor package.
Treat the Web smoke's final HTTP-server SIGTERM as teardown only when the gate
otherwise reports success.

Expect these files under artifacts/release:

~~~
Godot-$V-macos-arm64.zip
godot-$V-web-templates.zip
godot-$V-web-templates.zip.provenance
godot-$V-macos-arm64-template.zip
editor-macos.metadata
web.metadata
macos.metadata
SHA256SUMS.txt                 # exactly 3 rows before Windows
~~~

Verify the set and hashes before touching GitHub:

~~~
test -f "artifacts/release/Godot-$V-macos-arm64.zip"
test -f "artifacts/release/godot-$V-web-templates.zip"
test -f "artifacts/release/godot-$V-web-templates.zip.provenance"
test -f "artifacts/release/godot-$V-macos-arm64-template.zip"
test -f artifacts/release/editor-macos.metadata
test -f artifacts/release/web.metadata
test -f artifacts/release/macos.metadata
test "$(wc -l < artifacts/release/SHA256SUMS.txt | tr -d ' ')" -eq 3
(cd artifacts/release && shasum -a 256 -c SHA256SUMS.txt)
~~~

Check that metadata says release_version=$V, that editor-macos.metadata names
the current git rev-parse HEAD, and that Web metadata says `flavor=stock`, names
`godot_web_release.zip` and `godot_web_debug.zip` in its `release_template` /
`debug_template` records, records each inner SHA-256, and agrees with the staged
toolchain. Inspect the outer zip's exact two-entry set and verify both recorded
hashes; the two inner hashes must differ. Never edit metadata or checksums by
hand.

## Phase A-6: rehearse and create the draft

Run the dry run and inspect the rendered notes:

~~~
./dist/release-github.sh --repo "$REPO" --version "$V" --prev-version "$PREV" --dry-run
~~~

Read artifacts/release/RELEASE-NOTES.md. Require no @@ placeholders or HTML
comments; exactly the four headings overview, changes, download, and
Provenance; the correct previous-release heading; no asset table or trailing
blockquote; and exactly two fixed guide links (the guide root and its
download-verification section). The Mac-only draft must retain the short
warning that no Windows editor is included. If this fails, rerun the packager
that produced the odd metadata without changing the committed tree. If the
committed source or template needs correction, restart Phase 0: commit and push
it, reconfirm the merge gate for the new HEAD, rerun Phase A, and then repeat
the dry run. Do not edit rendered notes to bypass a check.

Create the draft without --publish:

~~~
./dist/release-github.sh --repo "$REPO" --version "$V" --prev-version "$PREV" 2>&1 | tee /tmp/release-macos.log
~~~

Verify the release before handoff:

~~~
gh release view "$V" --repo "$REPO" --json isDraft,targetCommitish,assets --jq '{isDraft,targetCommitish,assets:[.assets[]|{name,digest}]}'
gh release view "$V" --repo "$REPO" --json body --jq .body
~~~

Require isDraft: true, the fork commit as targetCommitish, four initial assets
(three lane assets plus SHA256SUMS.txt), and a body without @@ or <!--. Confirm
the body has the same four-section structure and two guide links as the dry
run. Do not publish from Mac and do not add a Windows checksum row.

## Phase B: mandatory Windows handoff

Run this immediately after creating the draft:

~~~
./dist/release-handoff.sh put --repo "$REPO" --version "$V" 2>&1 | tee /tmp/release-handoff-put.log
~~~

Require the uploaded asset
internal-handoff-$V-macos-to-windows.tgz. Its exact seven members are the
three metadata files, the three-row SHA256SUMS.txt, the Web template bundle and
provenance, and web_emcc.txt from FORK_ROOT. Keep the tarball outside
artifacts/release; it is an internal transport asset, not a release lane.

Compare the digest printed by put with the digest GitHub serves:

~~~
LOCAL_HANDOFF_SHA="$(sed -n 's/.*sha256 \([0-9a-f]\{64\}\)$/\1/p' /tmp/release-handoff-put.log | tail -1)"
REMOTE_HANDOFF_SHA="$(gh release view "$V" --repo "$REPO" --json assets --jq '.assets[] | select(.name | startswith("internal-handoff-")) | .digest' | sed 's/^sha256://')"
test -n "$LOCAL_HANDOFF_SHA"
test "$LOCAL_HANDOFF_SHA" = "$REMOTE_HANDOFF_SHA"
~~~

Verify the asset remotely and leave the release as a draft:

~~~
gh release view "$V" --repo "$REPO" --json isDraft,assets --jq '{isDraft,assets:[.assets[]|{name,size,digest}]}'
~~~

Require a non-empty handoff entry named
internal-handoff-$V-macos-to-windows.tgz and isDraft: true. If it is missing,
rerun put and verify again; put is intentionally repeatable. Never finish the
Mac phase based only on a successful local packaging log.

Give Windows this fixed continuation:

~~~
./dist/release-handoff.sh get --repo "$REPO" --version "$V"
# Check out the dn2cpp_commit from artifacts/release/editor-macos.metadata.
# Run Windows Phase C from docs/RELEASE.md.
./dist/package-windows-template.sh --version "$V"
./dist/package-editor-windows.sh --version "$V" --dn2cpp-commit \
    "$(sed -n 's/^dn2cpp_commit=//p' artifacts/release/editor-macos.metadata)"
./dist/release-handoff.sh drop --repo "$REPO" --version "$V"
./dist/release-github.sh --repo "$REPO" --version "$V" --prev-version "$PREV" --lane windows --lane editor-windows --uploaded-lane editor-macos --uploaded-lane web --uploaded-lane macos --publish
~~~

Windows must run drop before publishing and must not run
setup-godot-fork-web.sh; Mac supplies the Web Release/Debug bundle and
web_emcc.txt.

## Stop conditions and recovery

- Stop when either repository is dirty, a commit is not pushed, the host
  commits disagree, or the merge-gate result is unconfirmed.
- Stop on an emcc, engine_provenance, base_pin, corelib_framework, or
  dn2cpp_commit mismatch. Rebuild the affected lane from the same pinned tree;
  never rewrite metadata.
- If buildtools or emsdk rows are absent from the toolchain, rerun setup with
  default output paths before rebuilding the fork and editor.
- If either Web template's metadata and bundled emcc disagree, run
  FORCE=1 ./gates/setup-godot-fork-web.sh, then repackage Web before rerunning
  the Mac editor package.
- If the handoff is absent from the draft, run release-handoff.sh put with
  --repo "$REPO"; do not publish an unverified handoff.
- If the release was published prematurely or put refuses because it is no
  longer a draft, stop and report the exact GitHub state before changing it.
- Keep GitHub calls, fork-cache writes, and setup downloads in one approved
  execution context. Never expose authentication tokens in logs.

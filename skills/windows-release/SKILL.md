---
name: windows-release
description: Run and verify the dn2cpp Windows-side Godot editor release workflow, including macOS handoff retrieval, pinned toolchain setup, self-host rebuild, Windows packaging, smoke tests, draft cleanup, four-lane publication, and post-publish checks. Use when finishing a Godot editor release on Windows, rebuilding Windows release artifacts, or diagnosing handoff, MSVC, emcc, prebuilt-axis, checksum, or publish failures.
---

# Windows Release Workflow

Read `docs/RELEASE.md` Phase 0, Phase B, Phase C, and Phase D before acting.
Treat that document and the release scripts as the source of truth. This skill
fixes the Windows continuation's ordering and stop conditions.

## Preconditions

- Use Git Bash/MSYS on Windows x86_64.
- Use the exact `V`, immediately preceding `PREV`, and `REPO` chosen by the
  Mac phase. The accepted version form is `<godot-version>-dn2cpp.<X>.<Y>`.
- Require the Mac phase's `gates/pre-merge.sh` result for the exact dn2cpp
  commit named by the handoff. Do not launch the merge gate automatically.
- Require a host Python 3, Visual Studio's C++ workload, an Android NDK, the
  pinned .NET SDK, and an authenticated `gh`.
- Set `CMAKE_CXX_COMPILER=cl` before sourcing or running repository scripts so
  `_common.sh` imports the MSVC environment. Do not rely on a Developer Prompt
  being the parent of Git Bash.
- Do not run `gates/setup-godot-fork-web.sh`: the Mac phase supplies the one
  Web template and `web_emcc.txt` for this release.
- Do not use `--allow-partial-prebuilt`, `--no-smoke`, `CRI=1`, or a custom
  `--out` for the setup scripts. Missing prerequisites are release failures.

Use one approved shell context for GitHub calls, fork-cache writes, setup
downloads, and packaging. Keep authentication tokens out of logs.

## Phase 0: import and pin the Mac handoff

Run commands with errexit and pipefail:

```bash
set -e -o pipefail
V='REPLACE_WITH_RELEASE_VERSION'
PREV='REPLACE_WITH_PREVIOUS_RELEASE_VERSION'
REPO='takuma-komatsu/godot-dn2cpp'
```

Confirm the source tree and fork are clean and pushed before changing the
checkout. The Windows packager reads the whole dn2cpp tree, including
untracked files.

```bash
git fetch origin
test -z "$(git status --porcelain --untracked-files=all)"
git merge-base --is-ancestor HEAD origin/main

source gates/_godot_fork.sh
godot_fork_resolve
git -C "$FORK" fetch --tags --prune --prune-tags --force origin
git -C "$FORK" checkout dn2cpp/main
test -z "$(git -C "$FORK" status --porcelain --untracked-files=no)"
git -C "$FORK" merge-base --is-ancestor HEAD origin/dn2cpp/main
```

Move the complete previous output directory aside before importing anything.
Resolve the destination first and never overwrite an existing directory:

```bash
if [ -e "artifacts/release-$PREV" ]; then
    echo "error: artifacts/release-$PREV already exists; inspect it first" >&2
    exit 1
fi
if [ -d artifacts/release ]; then
    mv artifacts/release "artifacts/release-$PREV"
fi
```

Get the handoff from the draft release. It verifies the GitHub-served digest
and the archive's exact seven members; never reconstruct metadata by hand.

```bash
./dist/release-handoff.sh get --repo "$REPO" --version "$V"
IS_DRAFT="$(gh release view "$V" --repo "$REPO" --json isDraft --jq .isDraft)"
test "$IS_DRAFT" = true || {
    echo "error: release $V is not a draft; do not consume a published handoff" >&2
    exit 1
}
```

Require these files under `artifacts/release`:

```text
editor-macos.metadata
web.metadata
macos.metadata
SHA256SUMS.txt                         # exactly 3 rows at this point
godot-$V-web-template.zip
godot-$V-web-template.zip.provenance
```

Require `web_emcc.txt` under `FORK_ROOT`. Read `dn2cpp_commit` from
`editor-macos.metadata`; do not substitute the current branch tip:

```bash
DN2CPP_PIN="$(sed -n 's/^dn2cpp_commit=//p' artifacts/release/editor-macos.metadata)"
test -n "$DN2CPP_PIN"
git cat-file -e "$DN2CPP_PIN^{commit}"
git merge-base --is-ancestor "$DN2CPP_PIN" origin/main
git checkout "$DN2CPP_PIN"
test -z "$(git status --porcelain --untracked-files=all)"
```

The handoff must come from a draft. If it is absent, the release is published,
or a local old row/metadata file remains, stop and fix the state rather than
editing the handoff or checksum file.

## Phase C-1: prepare the Windows toolchain

Run the setup scripts with their default output paths, then rebuild the
self-host CLI before packaging:

```bash
export CMAKE_CXX_COMPILER=cl
./gates/setup-buildtools.sh
./gates/setup-emsdk.sh
./gates/selfhost-emit.sh
./gates/setup-godot-fork.sh 2>&1 | tee /tmp/setup-godot-fork-windows.log
```

Require the fork commit, engine provenance, and base pin to agree with the Mac
metadata before packaging. The local provenance is derived from the same fork
sources used by the packager:

```bash
source gates/_godot_fork.sh
godot_fork_resolve
BASE_COMMIT="$(head -1 "$FORK_PIN_EXPECTED")"
WINDOWS_FORK_COMMIT="$(git -C "$FORK" rev-parse HEAD)"
WINDOWS_ENGINE_PROVENANCE="$(godot_fork_engine_provenance)"
MAC_FORK_COMMIT="$(sed -n 's/^fork_commit=//p' artifacts/release/editor-macos.metadata)"
MAC_ENGINE_PROVENANCE="$(sed -n 's/^engine_provenance=//p' artifacts/release/editor-macos.metadata)"
MAC_BASE_PIN="$(sed -n 's/^base_pin=//p' artifacts/release/editor-macos.metadata)"
test "$WINDOWS_FORK_COMMIT" = "$MAC_FORK_COMMIT"
test "$WINDOWS_ENGINE_PROVENANCE" = "$MAC_ENGINE_PROVENANCE"
test "$BASE_COMMIT" = "$MAC_BASE_PIN"
```

The self-host stamp must also match the current `src/`, `runtime/`, and
`third_party/` tree. Do not accept an old native binary because packaging can
otherwise continue with a stale transpiler.

## Phase C-2: confirm emcc agreement

Compare the emcc recorded in the staged Windows bundle with the Mac Web
metadata. Discover the layout from the size report instead of hard-coding its
version:

```bash
BUNDLE="$(awk -F '\t' '$1 == "# bundle" { print $2 }' artifacts/toolchain/size-report.txt)"
test -n "$BUNDLE"
python -c 'import json,sys; print(json.load(open(sys.argv[1]))["emcc_version"])' \
    "artifacts/toolchain/$BUNDLE/emsdk/emsdk.json"
sed -n 's/^emcc=//p' artifacts/release/web.metadata
```

These values must be identical. Only the Mac host can re-bake the Web
template; if they differ, stop and rerun the Mac Web setup and packaging in
order.

## Phase C-3: package and smoke-test the Windows editor

Package from the pinned commit, leaving the default smoke tests enabled:

```bash
./dist/package-editor-windows.sh \
    --version "$V" \
    --dn2cpp-commit "$DN2CPP_PIN" \
    2>&1 | tee /tmp/pkg-editor-windows.log
```

Require the log to show:

- bundled cmake and ninja, not host fallbacks;
- prebuilt `host`, `android-arm64-v8a`, and `web-wasm32` axes;
- the Web template's emcc matching `web.metadata`;
- both desktop and browser editor-export smoke gates green;
- deterministic archive round-trip with all files SHA-256 identical.

The script must produce `editor-windows.metadata`,
`Godot-$V-windows-x86_64.zip`, and a fourth row in `SHA256SUMS.txt`. Check
the metadata and checksum row; never edit either file manually.

## Phase C-4/C-5: remove transport and rehearse publication

Remove the internal handoff before any publish attempt:

```bash
./dist/release-handoff.sh drop --repo "$REPO" --version "$V"
```

Run the complete four-lane dry run. Naming only `editor-windows` would drop
the other lanes from the rendered notes and checksum claims.

```bash
./dist/release-github.sh \
    --repo "$REPO" \
    --version "$V" \
    --prev-version "$PREV" \
    --lane editor-windows \
    --uploaded-lane editor-macos \
    --uploaded-lane web \
    --uploaded-lane macos \
    --publish --dry-run
```

Require all uploaded lanes to match GitHub's served digest and the published
metadata in the draft. Read `artifacts/release/RELEASE-NOTES.md`; require the
correct previous-release heading, all four lane sections, and no `@@` or
`<!--` markers. A leftover handoff must fail this stage rather than being
silently published.

## Phase C-6/D: publish and verify

Repeat the exact dry-run lane set without `--dry-run`:

```bash
./dist/release-github.sh \
    --repo "$REPO" \
    --version "$V" \
    --prev-version "$PREV" \
    --lane editor-windows \
    --uploaded-lane editor-macos \
    --uploaded-lane web \
    --uploaded-lane macos \
    --publish 2>&1 | tee /tmp/release-windows.log
```

Verify the published release:

```bash
gh release view "$V" --repo "$REPO" --json isDraft,targetCommitish,assets \
    --jq '{isDraft,targetCommitish,assets:[.assets[]|{name,size,digest}]}'
gh release view "$V" --repo "$REPO" --json body --jq .body
```

Require `isDraft: false`, the fork commit as `targetCommitish`, exactly five
assets (two editors, two templates, and `SHA256SUMS.txt`), no internal handoff,
and no `@@` or `<!--` markers. Check every guide URL in the body at the fixed
dn2cpp commit, then download and verify the release once if the host is also
the consumer.

## Stop conditions

- Stop on any dirty tree, unpushed commit, missing handoff, non-draft mismatch,
  or disagreement in `dn2cpp_commit`, `fork_commit`, engine provenance,
  `base_pin`, `corelib_framework`, or emcc.
- Stop when MSVC, Python, NDK, fixed buildtools, Emscripten, or a required
  prebuilt axis is missing. Do not paper over it with `--allow-partial-prebuilt`.
- Never run `setup-godot-fork-web.sh` on Windows or hand-copy metadata from
  the Mac host.
- If publication fails after the package succeeds, leave the release as a
  draft and rerun the complete four-lane command; never rerun with a subset.
- Do not delete or recreate a tag or release to work around a validation error.

# RELEASE — cutting a godot-dn2cpp editor release

A Japanese translation of this file is `docs/RELEASE.ja.md`; this file is the original.

Run it top to bottom and two hosts — macOS and Windows — produce one release.

Or use the wrapper for the same flow:

```bash
cd <DEV>
./dist/release-run.sh macos   --version "$V" --prev-version "$PREV" [--repo "$REPO"] [--out "$OUT"]
./dist/release-run.sh windows --version "$V" --prev-version "$PREV" [--repo "$REPO"] [--out "$OUT"]
```

`--dry-run-only` still runs setup, build, packaging, and
`release-github --dry-run`, but makes no remote release mutation: macOS skips
draft creation and handoff upload; Windows skips handoff removal and publish.
Run the macOS form by itself to validate the macOS lanes:

```bash
./dist/release-run.sh macos --version "$V" --prev-version "$PREV" --dry-run-only
```

The Windows form requires the draft and handoff produced by a normal macOS run;
it is not the next step after the macOS dry-only form:

```bash
./dist/release-run.sh windows --version "$V" --prev-version "$PREV" --dry-run-only
```

To preserve the handoff, Windows dry-only does not pass `--publish` to
`release-github`. It therefore omits C-5's publish-only check for assets outside
the active lanes; the normal flow runs that check after dropping the handoff.

The wrapper still follows this document's order. Use it after the preconditions in
Phase 0 are met.

**This is a procedure, not a design document.** Why the pieces are shaped this
way is `docs/EDITOR-EXPORT-DESIGN.md` §11 and is not repeated here.

---

## 0. Read this checklist first

### Hosts you need

| host | lanes it bakes | required |
|---|---|---|
| macOS (Apple Silicon) | `editor-macos` / `web` / `macos` | Xcode Command Line Tools, Xcode (iOS axes), Android NDK, a real python3, `gh` (authenticated) |
| Windows (x86_64) | `editor-windows` | Visual Studio's C++ workload, Android NDK, a host Python 3, `gh` (authenticated) |

- **The order is fixed** (macOS → Windows). The reason opens §B.
- There are exactly four lane names (`editor-macos` `editor-windows` `web`
  `macos`); the lane table in `dist/release-github.sh` is their only definition.
- **Use your normal shell** — plain zsh works on macOS. Only Windows assumes
  Git Bash / MSYS. The python3 version matters on macOS only inside the Web
  export gates (`gates/build-and-run-godot-editor-export-web.sh` and friends),
  so a real python3 (Homebrew's, say) first on `PATH` keeps that check quiet.

### Decide before you start

**The version number.** No form but `<godot version>-dn2cpp.<X>.<Y>` is
accepted: `release_version_split` in `gates/_common.sh` is the one judgement,
and `dist/release-github.sh`, `dist/package-editor-macos.sh`,
`dist/package-editor-windows.sh`, `dist/package-macos-template.sh` and
`dist/package-web-template.sh` all pass through it. The editor packagers
additionally require `<godot version>` to match the fork's `version.py`.

`<X>.<Y>` is dn2cpp's own semver, bumped by what changed:

- the fork (the editor) moved → `X+1`, `Y=0`
- dn2cpp alone moved → `Y+1`

Releases published under the old `<godot version>-dn2cpp.<n>` form stay where
they are; the scripts accept the new form only.

Every command below assumes these variables, set on both hosts:

```bash
V=<godot version>-dn2cpp.<X>.<Y>      # e.g. 4.7.1-dn2cpp.3.1
PREV=<the release this one follows>   # the notes' heading; may be the old form
REPO=takuma-komatsu/godot-dn2cpp      # the --repo default; if you change it, change it on both hosts
```

The `<n>` in an `artifacts/toolchain/dn2cpp-toolchain-<n>-…` path is the
toolchain bundle's layout version, unrelated to the release version.

### How long it takes

Measured on macOS (Apple Silicon) against a **set-up, warm tree**. A cold tree
is unmeasured and is an order of magnitude off. In particular, **if the engine
C++ differs and `gates/setup-godot-fork.sh` drops into a scons build, that step
alone is 1–2 hours**.

| step | warm tree |
|---|---|
| `gates/setup-buildtools.sh` / `gates/setup-emsdk.sh` | under 1s each (both skip) |
| `gates/selfhost-emit.sh` | ~13 min (instant when the stamp matches) |
| `gates/setup-godot-fork.sh` | ~2.5 min (matching engine hash skips editor and templates; only the toolchain bundle and the managed assemblies re-run) |
| `dist/package-web-template.sh` | under 1s |
| `dist/package-macos-template.sh` | ~10s |
| `dist/package-editor-macos.sh` (smoke included) | ~6.5 min |
| `dist/smoke-test.sh` | ~1 min |
| `dist/release-github.sh --dry-run` | ~10s |

**The two hosts need not run on the same day, but neither repository's `main` /
`dn2cpp/main` may move in between** (§0-C).

---

## Phase 0: preconditions (both hosts)

### 0-A. State of both repositories

`dist/release-github.sh` refuses a release that names a commit the remote does
not have. **Pushing is a precondition of this procedure; no script does it for
you.**

```bash
# the fork (godot-dn2cpp)
git -C <FORK> fetch --tags --prune --prune-tags --force origin
git -C <FORK> checkout dn2cpp/main
git -C <FORK> status --porcelain --untracked-files=no    # → must be empty
git -C <FORK> rev-parse HEAD                             # ← note it; both hosts must match
git -C <FORK> merge-base --is-ancestor HEAD origin/dn2cpp/main && echo pushed

# dn2cpp — main on the first host; on the second, the commit the first one used (below)
git -C <DEV> fetch origin
git -C <DEV> checkout main
git -C <DEV> status --porcelain --untracked-files=all    # → must be empty
git -C <DEV> rev-parse HEAD                              # ← note it
git -C <DEV> merge-base --is-ancestor HEAD origin/main && echo pushed
```

- Uncommitted changes to the fork's *tracked* files die (untracked things like
  `bin/` do not count).
- On the dn2cpp side **untracked files count too**: the toolchain bundle is
  built from the working tree, and a stray `.cs` under `src/` is compiled into
  it. Spell `--untracked-files=all` rather than relying on the default — a
  `status.showUntrackedFiles=no` in your own git config answers "clean" for
  exactly that tree, and the packagers ask for it explicitly for the same
  reason.
- On the dn2cpp side the remote checked is **`origin`, the public repository**.
  A commit that exists only on `archive` is unreachable from what the notes
  link to, and dies.
- A tag left pointing at an older commit dies. Delete it and re-fetch:
  `git -C <FORK> tag -d $V && git -C <FORK> fetch --tags origin`

### 0-B. Are the gates green

A release runs no gates. `--smoke` in `dist/package-editor-*.sh` runs the two
editor-export gates and nothing else is verified. **Cut a release from a `main`
on which `gates/pre-merge.sh` is green.**

The release scripts' own refusals are the exception, and they are exercised by
`gates/build-and-run-release-preconditions.sh` in the ordinary suite: it drives
them over synthetic metadata and asserts the remedy text, not just the exit
status. So a green suite says the messages in the table at the bottom of this
file are the ones you will actually see.

### 0-C. Both hosts on the same commit

- **The fork commit** is enforced by `dist/release-github.sh`: both editors'
  `fork_commit` must agree with each other and with the commit the tag lands on,
  or it dies.
- **`engine_provenance`, `base_pin` and `corelib_framework`** are likewise
  forced to agree across every lane (a .NET SDK version difference surfaces as
  `corelib_framework`).
- **The dn2cpp commit is enforced too**, and by both ends. Each editor packager
  takes a required `--dn2cpp-commit` and refuses unless the tree *is* that
  commit and is clean — the bundle is built from the working tree, so a stamp
  taken off `HEAD` alone would name something that was never built.
  `dist/release-github.sh` then demands the lanes agree on it, exactly as it
  does for `engine_provenance`. So **the second host checks out the commit the
  first host used, not the tip of `main`**:

  ```bash
  git -C <DEV> checkout "$(sed -n 's/^dn2cpp_commit=//p' artifacts/release/editor-macos.metadata)"
  ```

  The handed-over metadata is that value's only source. **Following `main`
  breaks in two ways**: if `main` moved after the first host cut its lanes, the
  cut dies on the disagreement; and if the move is not pushed yet, it dies as
  not contained in `origin/main`.
- **The release notes are re-rendered in full from the `dist/release-notes-template.md`
  of whichever host ran last**, and they link `docs/EDITOR-GUIDE.ja.md` at *that*
  host's dn2cpp `HEAD`. If either differs between hosts, the later host's version
  is what readers get. This is the strongest practical reason to keep the two
  hosts on one commit.

### 0-D. Move the previous release's artifacts aside (**first move of a new version**)

`dist/release-github.sh` requires the row set of `SHA256SUMS.txt` and the asset
set of the active lanes to match **in both directions**. Each packager, however,
rewrites **only its own row**. Bake a new version on top of the previous
artifacts and you get 3 old rows plus 3 new ones — six — and it dies with
`rows for no active lane`.

```bash
mv artifacts/release artifacts/release-<old version>
```

That also clears metadata from a previous generation of the lane table (an old
`editor.metadata`, say). **Move the whole directory aside rather than picking
files out of `--out`.**

### 0-E. A release in which only dn2cpp moved

The fork may not have moved at all — a new transpiler inside the same editor.
**It is still every lane and both hosts.** Two reasons:

- The version is part of every asset name, so the previous release's files
  cannot be reused (§A-5).
- `dist/release-github.sh` matches `SHA256SUMS.txt`'s row set against the active
  lanes' asset set in both directions, and requires every lane's metadata
  `release_version` to be this release's.

What drops out by itself — the commands are the same, the work inside them is
not:

- `gates/setup-godot-fork.sh` reuses the pristine clone's prebuilt editor and
  templates on a matching engine hash, so no scons build (§A-3).
- `gates/setup-godot-fork-web.sh` is effectively a no-op (§A-4).
- Pushing the fork: it did not move, so §0-A already holds.
- Only the tag is new. It lands on the same fork commit the previous release's
  tag names, beside it; `dist/release-github.sh` refuses a tag of this name only
  when origin carries it at a *different* commit, so this passes and `--commit`
  is not needed.

What does not drop out:

| step | why |
|---|---|
| `gates/selfhost-emit.sh` re-bake | `src/` moved — that is what this release is |
| both template lanes re-packaged | for the name and for `release_version`; the contents may be byte-identical to the previous version's |
| both editor lanes re-packaged, smoke included | the transpiler is the thing that changed, so the smoke *is* the release |
| moving `artifacts/release` aside (§0-D) | as in any other release |
| editing `dist/release-notes-template.md` by hand | `--prev-version` binds the version in the heading; the bullet list and the compare URL stay hand-written, and a stale one is detected by nothing (*The release notes*) |
| the handoff (§B) | the Windows editor is a lane like any other |

Times are the ones in *How long it takes*.

The other direction: a release in which the fork moved takes `X+1` and `Y=0`
(*Decide before you start*), and none of the above drops out — the engine hash
has moved, so a scons build is on the table.

---

## Phase A: the macOS host

### A-1. Two setup scripts (**always with the default `--out`**)

```bash
cd <DEV>
./gates/setup-buildtools.sh      # first; reason below
./gates/setup-emsdk.sh
```

- **Do not pass `--out`.** The default output path is exactly what the later
  resolvers (`dn2cpp_emsdk_resolve`, and the buildtools resolution in
  `dist/package-toolchain.sh`) assemble from the pinned version and the host
  tag. Put it anywhere else and the bundle silently ships with nothing in it.
- **`setup-buildtools.sh` runs before `setup-godot-fork.sh`.** Packaging
  without cmake/ninja merely warns and succeeds, but
  `dist/package-editor-macos.sh` dies when the size report has no `buildtools`
  row. The `emsdk` and `emsdk/node` rows are required for the same reason.
- Both are idempotent, and both verify the pin's sha256 before unpacking, so a
  truncated download is never read as a complete archive.

### A-2. The self-host CLI

Whether a re-bake is needed is the source's last commit date against the
existing binary's mtime:

```bash
git log -1 --format=%ci -- src runtime third_party
ls -l artifacts/selfhost-fullcli/dn2cpp
```

If the source is newer, re-bake.

```bash
./gates/selfhost-emit.sh
cat artifacts/selfhost-fullcli/dn2cpp.src-hash
```

`dist/package-editor-macos.sh` dies unless that stamp matches the current hash
of `src/`, `runtime/` and `third_party/`.

**Do not rely on `dist/package-toolchain.sh` re-baking for you.** It does call
`gates/selfhost-emit.sh` when it detects a stale binary, but **if that call
fails it warns, continues, and packages the old binary**. Run it by hand first
and watch it succeed.

### A-3. The fork cache and the toolchain bundle

```bash
./gates/setup-godot-fork.sh 2>&1 | tee /tmp/setup-godot-fork.log
```

If the engine sources are identical to the base commit it reuses the pristine
clone's prebuilt binaries. A difference in the engine C++ drops into a scons
build — long; run it in the background.

Note the `engine hash:` from the log. **If the two hosts disagree on it, the
`engine_provenance` check kills the release** — one release, one engine tree.

That log interleaves MSBuild output in the host's locale. **Do not assume
English if you parse it mechanically.**

### A-4. Bake the Web template

```bash
./gates/setup-godot-fork-web.sh 2>&1 | tee /tmp/setup-godot-fork-web.log
```

- With an unchanged engine hash this is effectively a no-op. If the fork's
  changes are managed-side only, nothing is re-baked and **the zip's contents
  can be byte-identical to the previous version's**. Run `dist/package-web-template.sh`
  in A-5 regardless — the asset name carries the version and `web.metadata`'s
  `release_version` takes part in the all-lane agreement check, so the previous
  version's file cannot be reused.
- Do not pass `CRI=1`. The CRI build is a variant carrying a third-party SDK and
  must never become a release asset (`dist/package-web-template.sh` checks the
  flavor and fails).

### A-5. Bake the assets (**web → macos → editor, in that order**)

```bash
./dist/package-web-template.sh   --version "$V"
./dist/package-macos-template.sh --version "$V"
./dist/package-editor-macos.sh   --version "$V" --dn2cpp-commit "$(git rev-parse HEAD)" \
    2>&1 | tee /tmp/pkg-editor-macos.log
```

- **`--dn2cpp-commit` is this host naming the commit both editors are cut from.**
  It is required, and the run refuses a tree that is not it or that carries
  uncommitted work — including untracked files, which the compiler reads too.
  Windows takes the value from this lane's metadata (§C-3).

- **The order is enforced.** The smoke in `dist/package-editor-macos.sh` reads
  `<out>/godot-$V-web-template.zip` and its `.provenance` **as release
  artifacts** and exports with them; absent, it dies telling you to cut the Web
  template first. It also requires the bundled SDK's `emcc_version` to match
  `web.metadata`. On a mismatch, in order:
  `FORCE=1 ./gates/setup-godot-fork-web.sh` → `./dist/package-web-template.sh --version "$V"`.
- **Re-bake the macOS template every version too.** Its input is the **official
  upstream `macos.zip`** (the one the editor's "Manage Export Templates"
  installs), whose sha does not move, but the output zip is non-deterministic
  and gets a fresh sha each time. What blocks reuse is not the sha: it is the
  version in the asset name and the `release_version` agreement check in
  `macos.metadata`.
- `dist/package-editor-macos.sh` has `--smoke` on by default — it runs
  `gates/build-and-run-godot-editor-export.sh` and
  `gates/build-and-run-godot-editor-export-web.sh` against the assembled `.app`
  itself. Do not turn it off.
- A single missing prebuilt axis dies. The required macOS set is `host` + the 3
  iOS axes + `android-arm64-v8a` + `web-wasm32`. `--allow-partial-prebuilt`
  accepts the downgrade, but **it degrades the notes' `prebuilt_axes` line** —
  install the missing prerequisite (Xcode / NDK / emsdk) instead. A missing
  `host` axis is not rescued even by that flag.

The Web export smoke always ends with `Terminated: 15 ... http.server`. **That
is the teardown SIGTERM, not a failure.**

Output (`artifacts/release/`):

```
Godot-$V-macos-arm64.zip                + editor-macos.metadata
godot-$V-web-template.zip{,.provenance} + web.metadata
godot-$V-macos-arm64-template.zip       + macos.metadata
SHA256SUMS.txt                          ← 3 rows at this point
```

### A-6. Dry run

```bash
./dist/release-github.sh --version "$V" --prev-version "$PREV" --dry-run
```

With no `--lane` at all the default is exactly the macOS host's three lanes
(`editor-macos` `web` `macos`). It runs every precondition for real read-only,
renders the notes, prints the git / gh commands it would have run, and stops.

**The rendered notes are written for real under `--dry-run`**, so this is where
you read the body and follow both fixed links to `docs/EDITOR-GUIDE.ja.md`
(the guide itself and its download-verification section) before anyone else can.

**When two accounts of the state disagree, run this instead of arguing.** The
line that fails is the cause.

### A-7. Create the draft

```bash
./dist/release-github.sh --version "$V" --prev-version "$PREV" 2>&1 | tee /tmp/release-macos.log
```

- **Do not pass `--publish`.** Hand it to Windows as a draft — a draft is the
  only state in which a half-populated asset list is invisible to everyone else.
- **Leave `SHA256SUMS.txt` at 3 rows.** Adding the Windows asset's row ahead of
  time dies on the row-set / active-lane check (`rows for no active lane`). The
  Windows packager adds the fourth row itself.
- The tag is created and pushed here. Later runs report it as already on origin
  and leave it alone.
- **The handoff tarball goes onto this draft next** (§B). `--publish` refuses
  while it is attached, so it is dropped on the Windows host (C-4).

---

## Phase B: the handoff

The Windows host gets **six files** from `artifacts/release/` plus
**`web_emcc.txt`** from the fork root — seven items, travelling as one tarball
attached to the draft release, `internal-handoff-$V-macos-to-windows.tgz`. The
count is checked: `get` requires the archive's member list to be exactly those
seven.

| item | why |
|---|---|
| `editor-macos.metadata` `web.metadata` `macos.metadata` | `--uploaded-lane` reads metadata **from the local `<out>/<lane>.metadata`**; the full schema remains required, public provenance is checked against the draft body, and applicable integrity and cross-lane checks still run — never rewrite these by hand |
| `SHA256SUMS.txt` (3 rows) | the Windows packager appends the fourth; all four rows are checked against the final active-lane asset set |
| `godot-$V-web-template.zip` | needed in the flesh: the Windows editor's smoke exports with **the release** template |
| `godot-$V-web-template.zip.provenance` | that same smoke reads the engine provenance from it; without it the value is derived from `web.metadata`, and then the hashes must agree |
| `web_emcc.txt` from the fork root | one of the files `dist/package-editor-windows.sh`'s smoke copies into the smoke root; absent, it dies with `the fork root has no web_emcc.txt`. **Only `gates/setup-godot-fork-web.sh` writes it**, and Windows does not run that (C-1), so the macOS copy is the only source |

The **contents** of `web_emcc.txt` are an emcc version string, and they only
enter the Web export gates' cache key. The emcc agreement check (C-2 /
`godot_fork_web_template_emcc_assert`) prefers a `web.metadata` with a matching
`release_version` as its witness, so this file is in practice one that merely
has to exist.

On the producing side (macOS):

```bash
./dist/release-handoff.sh put --version "$V"
```

- **It refuses unless the release exists and is still a draft.**
- The tarball is built in a temp dir and **never in `artifacts/release/`**, where
  it would be an asset no lane declares and the row-set check would die on it.

**What you do not hand over:**

- **The macOS editor zip and the macOS template zip** (hundreds of MB between
  them). `--uploaded-lane` waives exactly two things — uploading, and the asset
  being present in `--out`. **It waives no verification**: the bytes are checked
  against the digest GitHub serves, public provenance against the draft body,
  the full metadata schema remains required, and applicable integrity and
  cross-lane checks still run.
- **This is why the host order is fixed.** Reversed, the Windows host would need
  the macOS assets themselves on disk.

Placing them (Windows side):

```bash
./dist/release-handoff.sh get --version "$V"
```

- The bytes are verified against the digest GitHub is serving before anything is
  extracted — the same witness `--uploaded-lane` trusts.
- It places `web_emcc.txt` at the fork root itself.
- **It refuses when `artifacts/release/SHA256SUMS.txt` already carries a row the
  handoff does not**: that row is a later packaging run's, and writing the
  handoff's three-row copy over it would revert it in silence.

The route's only prerequisite is a `gh` authenticated against `$REPO`, which
both hosts need anyway (§0), so nothing is carried by hand and no value is
transcribed.

If the previous release's artifacts are still in `artifacts/release/` on the
Windows side, move them aside first too (§0-D).

---

## Phase C: the Windows host

The shell is Git Bash / MSYS.

### C-0. Windows-specific prerequisites

- **Install a host Python 3.** `resolve_python` considers exactly `python3`,
  `python` and `py -3`; the portable CPython inside the bundled Emscripten SDK
  is not a candidate (it is passed to `emcc.exe` alone, as `EMSDK_PYTHON`).
  `dist/package-editor-windows.sh` uses it for everything from reading the
  manifest to creating the zip. The only way out is naming one in
  `DN2CPP_PYTHON`.
- **Export `CMAKE_CXX_COMPILER=cl`.**

  ```bash
  export CMAKE_CXX_COMPILER=cl
  ```

  `gates/_common.sh` pulls in `vcvarsall x64` only when it sees that value
  (`is_msvc_compiler` → `ensure_msvc_env`, run at source time). Without it, a
  bare Git Bash has neither `cl.exe` nor `INCLUDE` / `LIB`, and the prebuilt
  runtime's host axis fails to configure. **`dist/package-toolchain.sh` then
  warns that away, deletes `prebuilt/` entirely and exits 0.** The failure
  surfaces later, when `dist/package-editor-windows.sh` dies with
  `the staged toolchain carries no prebuilt/host`. (A Git Bash launched from a
  Developer Command Prompt has the same effect.)
- **Install the Android NDK.** Without it `dist/package-toolchain.sh` silently
  drops the android axis (one informational line) and
  `dist/package-editor-windows.sh` dies with `missing prebuilt axes`. The only
  workaround is `--allow-partial-prebuilt`, which degrades the notes'
  `prebuilt_axes`. Windows requires three axes: `host` + `android-arm64-v8a` +
  `web-wasm32` (the iOS axes cannot be baked on Windows and are not asked for).
- **The same .NET SDK version as macOS.** A different one dies on the
  `corelib_framework` agreement check.

### C-1. Setup (the same two scripts, plus self-host and fork)

Check out the dn2cpp commit the macOS side used (§0-C), not `main`. C-3 refuses
otherwise, but it refuses after `gates/setup-godot-fork.sh`, so doing it here
costs nothing and doing it later costs that build.

```bash
cd <DEV>
export CMAKE_CXX_COMPILER=cl
./gates/setup-buildtools.sh
./gates/setup-emsdk.sh
./gates/selfhost-emit.sh
./gates/setup-godot-fork.sh 2>&1 | tee /tmp/setup-godot-fork.log
```

`engine hash:` must equal the value noted on macOS. **If it differs, stop
here.** Carrying on guarantees death at the `engine_provenance` check.

Do **not** run `gates/setup-godot-fork-web.sh`. There is one Web template per
release and it is the one macOS baked.

### C-2. Confirm the emcc agreement first

```bash
python -c "import json;print(json.load(open('artifacts/toolchain/dn2cpp-toolchain-0.1.0-windows-x86_64/emsdk/emsdk.json'))['emcc_version'])"
sed -n 's/^emcc=//p' artifacts/release/web.metadata
```

**The two must match.** If they do not, C-3 dies, and Windows has no way out —
only macOS can re-bake the Web template.

### C-3. Package the Windows editor

```bash
./dist/package-editor-windows.sh --version "$V" --dn2cpp-commit \
    "$(sed -n 's/^dn2cpp_commit=//p' artifacts/release/editor-macos.metadata)" \
    2>&1 | tee /tmp/pkg-editor-windows.log
```

Check:

- `web template:  emcc matches the bundled toolchain (.../artifacts/release/web.metadata)`
- `prebuilt:` lists the three axes host / android / web
- both smoke gates green
- a final `OK: ...-windows-x86_64.zip`

```bash
grep '^fork_commit=' artifacts/release/editor-windows.metadata   # same SHA as macOS
wc -l < artifacts/release/SHA256SUMS.txt                          # → 4
```

### C-4. Drop the handoff asset

```bash
./dist/release-handoff.sh drop --version "$V"
```

Its contents are consumed by now; up to this point `get` can still be re-run.
Idempotent — it prints a line and exits 0 when the asset is not there.

### C-5. Dry run

```bash
./dist/release-github.sh --version "$V" --prev-version "$PREV" \
  --lane editor-windows \
  --uploaded-lane editor-macos --uploaded-lane web --uploaded-lane macos \
  --publish --dry-run
```

- **`--publish` here costs nothing and is what rehearses the "no asset outside
  the lanes" refusal**, so a forgotten C-4 surfaces here instead of at the real
  publish.
- `--uploaded-lane` means two things: make that lane active, and its asset is
  already on the release. **Name all four lanes.**
- Every uploaded lane must report
  `on the release as ..., digest and published notes agree`.
- This host renders the notes that get published, so read the rendered body and
  both fixed `docs/EDITOR-GUIDE.ja.md` links here — under `--dry-run` they are
  written for real.

### C-6. The real run, and publish

```bash
./dist/release-github.sh --version "$V" --prev-version "$PREV" \
  --lane editor-windows \
  --uploaded-lane editor-macos --uploaded-lane web --uploaded-lane macos \
  --publish 2>&1 | tee /tmp/release-windows.log
```

While the release is a draft, `SHA256SUMS.txt` goes up first and the assets
follow smallest-first. Appending to a published release reverses it — assets
first, `SHA256SUMS.txt` last — so no window opens in which the release names an
asset that is not there yet. The script picks either one.

---

## Phase D: after publishing

```bash
gh release view "$V" --repo "$REPO" --json isDraft,targetCommitish,assets \
  --jq '{isDraft, targetCommitish, assets:[.assets[]|{name,digest}]}'
```

- `isDraft: false`
- five assets (2 editors + 2 templates + `SHA256SUMS.txt`) — now what `--publish`
  enforced, not only what you are checking
- `targetCommitish` is the fork commit the tag landed on

```bash
gh release view "$V" --repo "$REPO" --json body --jq .body | grep -n '@@'   # → no output
gh release view "$V" --repo "$REPO" --json body --jq .body | grep -n '<!--' # → no output
```

(The script dies on both of these at render time as well, but the published body
is worth one look with your own eyes.)

```bash
gh release view "$V" --repo "$REPO" --json body --jq .body \
  | grep -o 'https://github.com/[^ )]*/docs/[^ )]*' | sort -u \
  | xargs -n1 curl -sfI -o /dev/null -w '%{http_code} %{url_effective}\n'   # → 200 each
```

This selects the notes' two commit-fixed guide links. Every check before
publication was made against the local tree — nothing so far has asked GitHub
whether it serves that path at that commit. Fragments never reach the server, so
a 200 says the page exists and nothing about where the anchor lands; the slug
rules `gates/build-and-run-doc-claims.sh` checks them against are an
approximation, so open the verification link and look.

Finally, download and unpack it once for real: verify against `SHA256SUMS.txt`,
then `xattr -dr com.apple.quarantine` on macOS, or unblock the zip on Windows.

---

## The release notes

- **The original is `dist/release-notes-template.md` (in Japanese).** Never edit
  the release page body directly; only re-running `dist/release-github.sh`
  changes it.
- **The published body has four sections only:** an overview, changes since the
  previous release, download guidance, and minimal provenance. The download
  section points readers to GitHub's Assets list, `SHA256SUMS.txt`, the guide,
  and the guide's verification section instead of repeating an asset table or
  verification commands. Everything a downloader does next — installing, host
  requirements, exporting to each platform, troubleshooting, the known limits,
  and the licence terms — is `docs/EDITOR-GUIDE.ja.md`, linked at a fixed commit
  so that a published link keeps saying what it said the day it was published.
- **"Changes since the previous release" is the only section written by hand**,
  and only its bullet list and its compare URL: the version in the heading is
  `@@PREV_VERSION@@`, bound from `--prev-version`. Other dynamic values are
  derived from the checked-out trees or from the public subset of lane metadata.
  Nothing detects a stale list: leave it untouched and the notes render happily,
  still describing the release before this one.
- Public provenance is limited to engine provenance, the dn2cpp commit, Web's
  emcc/Emscripten information, and the source SHA of the macOS template. The
  Web and macOS rows appear only when their lanes are active.
- **The guide is edited when the editor's behaviour or standing requirements
  change, not when a release is cut.** Per-release tool versions do not belong
  there. Concrete bundled-tool versions omitted from public provenance remain
  available in the lane metadata and packaged records.
- `@@DOCS_REF@@` is this repository's `HEAD` at the moment of the cut, so
  **commit and push a guide edit before cutting.** `dist/release-github.sh`
  otherwise dies — on the sha not being reachable from `origin/main`, whose
  readers' first click would 404, or on the guide having uncommitted changes,
  which that sha does not carry. Both checks are read-only and run under
  `--dry-run`.
- **GitHub renders a single newline inside a paragraph as a line break.** So the
  template is written **one paragraph per line**. Wrap it and the published body
  fills with line breaks.
- Metadata requirements and public bindings are separate contracts. Lane rows
  still require their full schema, including records not printed in the body;
  applicable artifact, hash, and cross-lane checks remain unchanged. Removing a
  value from the public notes does not remove its metadata key. The template's
  `@@KEY@@` set must be a **subset** of the smaller public binding set. One
  unbound `@@KEY@@` left over and the rendering dies.
- `<!--lane:NAME-->` at the start of a line drops that one line if body follows
  it, or everything up to `<!--/lane-->` if it stands alone, from a cut without
  that lane. `!NAME` negates. **It does not nest** — the inner `-->` closes the
  outer marker early and the remainder reaches the release page as body. An
  unknown lane name dies.
- Do not spell an Emscripten / Node.js / cmake / ninja version into the markdown
  under `dist/`, nor into `docs/EDITOR-GUIDE.ja.md`. Pins and metadata remain the
  single source. The only bundled-tool version retained in the public body is
  Web's Emscripten information, received through placeholders; cmake, ninja,
  and Node.js stay out of the body. A spelled-out version fails
  `gates/build-and-run-doc-claims.sh`.

---

## Troubleshooting

**Start with `--dry-run`.** It runs every precondition for real, read-only.
There is nothing to argue about when accounts of the state disagree.

| symptom | cause and remedy |
|---|---|
| `--version must read <major>.<minor>.<patch>-dn2cpp.<X>.<Y>, got: ...` | malformed; nothing but `<base>-dn2cpp.<X>.<Y>` is accepted |
| `--version base X != the fork's version.py (Y)` | the fork is checked out at a different base |
| `lanes 'A' and 'B' disagree on engine_provenance` | the two hosts have different engine trees; re-bake one |
| `... disagree on corelib_framework` | the two hosts have different .NET SDKs |
| `... disagree on dn2cpp_commit` | the second host packaged from another commit; re-run its editor packager with `--dn2cpp-commit` naming the first host's (§C-3) |
| `--dn2cpp-commit names X, but this tree is at Y` | check X out. On Windows, X is the value the handoff carried in `editor-macos.metadata` |
| `the working tree is not X — it carries changes of its own` | commit, stash or delete them. Untracked files count: they reach the bundle as surely as tracked ones |
| `the packaged X editor was built from A, but the tag would name B` | the fork HEAD moved since packaging; re-bake, or pass `--commit A` |
| `is not reachable from origin/...` | not pushed; push the fork or dn2cpp |
| `HEAD (...) is not reachable from the dn2cpp origin/...` | the commit the notes would link the guide at is unpushed. Distinct from the row above, which is about a lane's `dn2cpp_commit`: this one is your working `HEAD`. `git push origin main` |
| `... has uncommitted changes, and the notes would link ...` | commit and push the guide first. Only the guide is checked, so unrelated work in progress is not the cause |
| `the guide the notes link is missing: ...` | run from the dn2cpp checkout, not the fork's; or the guide was renamed without renaming `GUIDE=` in `dist/release-github.sh`, which would 404 every past release's links too |
| `SHA256SUMS.txt does not describe exactly the active lanes' assets` | `rows for no active lane` means the previous version's artifacts are still in `--out` (§0-D). Otherwise you forgot to name a lane, or added a row ahead of time |
| `the notes on release ... do not mention KEY=...` | the local metadata's public provenance disagrees with the published notes. **Do not fix it by hand** — re-copy it from the packaging host |
| `no working Python 3 interpreter found` | Windows; install a host Python 3 |
| `the staged toolchain carries no prebuilt/host` | on Windows suspect an unset `CMAKE_CXX_COMPILER=cl` first. The real cause is in `dist/package-toolchain.sh`'s log (`prebuilt-host-configure.log`) |
| `missing prebuilt axes: android-arm64-v8a ...` | no NDK / emsdk / Xcode. Installing it is the answer |
| `the Web template and the bundled toolchain were linked by different emcc` | re-bake, in order: `FORCE=1 gates/setup-godot-fork-web.sh` → `dist/package-web-template.sh --version "$V"` (macOS only) |
| `predates the current sources` | re-bake `gates/selfhost-emit.sh` |
| `the fork root has no web_emcc.txt` | Windows; `dist/release-handoff.sh get` places this file, so re-run it (§B) |
| `release ... carries assets no active lane declares` | the handoff tarball was not dropped (C-4). If the name is anything else, someone uploaded by hand or a lane was dropped from this run — the message names both remedies |
| `put` refuses: the release is not a draft | it is already published; there is nothing left to hand over |
| `get` refuses: the release carries no handoff asset | `put` was not run on macOS, or `drop` already ran |
| `get` refuses: the downloaded tarball does not match the served digest | a corrupt download; re-run it |
| `get` refuses: local `SHA256SUMS.txt` carries rows the handoff does not | a Windows packaging run already appended its fourth row. The handoff is stale — you are past it (C-3) |

**Log lines that look like failures and are not:**

- `Terminated: 15 ... http.server` at the end of the Web export smoke — the
  teardown SIGTERM.
- Non-English MSBuild output mixed into `gates/setup-godot-fork.sh`'s log — the
  host locale.

**Keep verification helpers inside `gates/`.** `gates/_common.sh` derives the
repository root from `BASH_SOURCE[1]` — its caller — and nothing validates the
result, so a script outside the repository that sources it does not die: it runs
on quietly believing some other directory is the root.

**To fix only the notes body, touching no asset:** re-run with all four lanes
declared as `--uploaded-lane`. Nothing is left to upload, and only the
`SHA256SUMS.txt` re-upload and the notes re-render happen. It requires the four
metadata files and the four-row `SHA256SUMS.txt` to be present locally.

---

## What not to do

1. **Do not bake a new version on top of the previous release's artifacts.**
   Move the whole directory aside (§0-D).
2. **Once every lane is up, do not re-run `dist/release-github.sh` on a subset
   of lanes.** A lane absent from `--lane` / `--uploaded-lane` loses its whole
   block from the notes and its row from `SHA256SUMS.txt`, while its asset stays
   on GitHub — a release with the Windows editor attached, notes that say it is
   not, and no row for it in `SHA256SUMS.txt`. **The script does not detect
   this.** Always name every lane when re-running.
3. **Do not `--publish` from macOS.** Hand it over as a draft. While the handoff
   asset is attached `--publish` refuses mechanically — but only until C-4 drops
   it.
4. **Do not let `SHA256SUMS.txt` reach 4 rows on macOS.**
5. **Do not hand-write metadata for `--uploaded-lane`.** Copy the packaging
   host's verbatim. The required key set grows across versions (`node_version`
   was added in one), so **an old metadata file cannot be edited into claiming a
   newer version**.
6. **Do not run the two setup scripts with `--out`.**
7. **Do not edit the release body in the web UI.** The next re-run erases it.
8. **Do not make `--allow-partial-prebuilt` routine.** It declares a missing
   prerequisite; it does not work around one.
9. **Do not upload anything to a release by hand.** `--publish` refuses on any
   asset no lane declares, and cannot tell yours from a leak.

---

## Signing (what to tell people downloading)

- **The macOS editor is ad-hoc signed only, and not notarized.** Downloaded
  through a browser it picks up the quarantine attribute and refuses to launch.
  The linked editor guide carries the `xattr -dr com.apple.quarantine` step.
  (Do not advise `spctl --master-disable`.)
- **The Windows editor is unsigned.** SmartScreen reports an unknown publisher.
  The only things vouching for identity are `SHA256SUMS.txt` and the
  `RELEASE.txt` inside the package.

Both are current behaviour, each with a row in `docs/STATUS.md`.

---

## Unverified (what this procedure has not confirmed)

- **Cold-tree timings.** The table above is one measured run on one host with a
  warm tree; the cold side is unmeasured.
- **Whether Windows works without `CMAKE_CXX_COMPILER=cl` in a shell that
  already ran vcvars** — in code `ensure_msvc_env` then does nothing and cmake's
  default detection takes over. It may well work, but the code does not settle
  it. Setting it is the safe choice.
- **Whether `web_emcc.txt` necessarily agrees across hosts** — the stamp is
  emcc's version string and SDKs from the same pin ought to produce the same
  one, but no check guarantees the hosts cannot differ. The emcc agreement is
  underwritten by the side that takes `web.metadata` as its witness, so the only
  exposure is the Web export gates' cache key — but this is unmeasured.

# Contributing to dn2cpp

Read `AGENTS.md` first — build, gates, module boundaries, code style — and the
*Permanent non-goals* section of `README.md`. Those boundaries are settled and
each line says why, so a change that lands inside one is declined rather than
discussed.

## What CI can and cannot say

The hosted smoke workflows (`.github/workflows/linux-smoke.yml`,
`.github/workflows/windows-smoke.yml`, `.github/workflows/macos-smoke.yml`) run
basic regression checks on every pull request and push to `main`.
`gates/ci-smoke.sh` selects the same fixed, uncached Debug allowlist on Linux,
Windows/MSVC and macOS. Windows and macOS run `array-core` and `transpiler-limits`
in separate jobs from the remaining primitive gates, using disjoint partitions
of the full allowlist.
Linux runs the allowlist in a single job. The checks cover language,
primitive values, arrays, strings, collections, generics, async, threads and GC
barriers.
It also checks OS APIs through filesystem, environment, synchronization,
memory-mapped file and native interop tests. Every selected gate must run. Gate
logs are uploaded, and the basic profile publishes timings in the job summary.

Each OS runs the wider non-Godot suite in a single job daily and through
`workflow_dispatch`. That profile retains the OSS integration gates and other
checks outside the basic allowlist. Missing optional prerequisites remain
reported as skips in its summary and logs. Scheduled and manual runs use a
separate concurrency group, so a new push cannot cancel them.

The Godot-inclusive merge gate — `./gates/pre-merge.sh` — does not run there and
cannot. Its header states each structural reason (a scons-built dn2cpp fork of
the Godot editor, Xcode plus an iOS simulator, an Android NDK);
read it there rather than trusting a summary. A human runs it on a
provisioned machine. The Emscripten SDK is the one prerequisite pre-merge
provisions itself (`gates/setup-emsdk.sh`); a host missing any other one still
runs to completion, and the surfaces it cannot cover are reported red at the
verdict. The proprietary CRI SDK is optional: when absent, CRI gates are
reported as skips and their Web templates are not prepared. With the SDK
installed, CRI gates run under the same strict checks as the rest of the suite;
failures, missing other prerequisites and incomplete coverage remain red.

So a green pull request is not permission to merge — and equally, a pull request
never goes red over a toolchain you cannot install.

Running this much locally is enough:

```bash
./gates/build-and-run-sample.sh          # console: C# → IL → C++ → native
./gates/build-and-run-multiassembly.sh   # multi-assembly (-r)
./gates/pre-merge.sh --skip-godot        # strict Debug non-Godot check
```

The merge gate runs Debug by default, enabling the shared-generics assertions
while running the suite once. Use `CONFIG=Release ./gates/pre-merge.sh` for
Release only, or `./gates/pre-merge.sh --both-configs` for Release followed by
Debug. All selections disable caching and require every selected gate to run,
except for the reported CRI skips when its SDK is absent.

For a PR that changes no Godot-specific files, a passing default Debug
`./gates/pre-merge.sh --skip-godot` is sufficient to merge. Coding agents may
run this strict non-Godot check autonomously for any change. It omits the Godot
phase and its self-host/fork/template preparation; missing prerequisites still
fail the selected gates except for CRI SDK absence in the console-wasm gate,
and caching stays disabled. Its receipt records the
non-Godot scope, excluded gate count and configurations. Combine `--skip-godot`
with `--both-configs` to check both configurations.

Godot-specific files are the Godot backends, shim and runtimes in the module
map, plus Godot-only samples, gates, helpers, packaging and documentation.
Shared runners and general verification infrastructure are not Godot-specific;
`AGENTS.md` defines the scope. A PR that changes Godot-specific files still
requires a human-run Godot-inclusive merge gate. Selecting `--skip-godot` does
not classify a PR's changed files.

**A skip is not a pass.** A gate whose optional prerequisite is absent opts out
via `gate_skip` (`gates/_common.sh`) and is counted and reported **separately**
with its reason, so the summary never claims all N passed when some never ran.
The pre-merge receipt names any CRI SDK skips per configuration. If you add a
gate, opt out that way and never with `echo SKIP; exit 0`.

## What to work on

Open work is `docs/STATUS.md`. A row that names a host prerequisite — a signing
identity, a Windows host, a device or an SDK the repository cannot carry — is one
you cannot verify where you are; open an issue before starting, so the
verification half has an owner.

## Issues

One tracker: this repository. The forked Godot editor,
[takuma-komatsu/godot-dn2cpp](https://github.com/takuma-komatsu/godot-dn2cpp),
has its issues disabled, so editor problems come here too. For an editor report,
paste the build's identity as it ships — the unpacked Windows package's
`RELEASE.txt`, or on macOS the `.app`'s `DN2CPP*` `Info.plist` keys — together
with the editor's `--version` string.

Issues and `docs/STATUS.md` are separate ledgers, and the reference between them
is one-directional. A row may cite an issue number: a GitHub number is permanent
and resolvable. Nothing cites a row — not an issue, not a commit, not code —
because the row is deleted when the ticket lands. An issue accepted as work
becomes one row and is closed with a comment saying so; the point is to keep one
ledger, not two that drift.

## Commits and style

`AGENTS.md`'s *Naming / style* and *Write like an experienced programmer* are the
contract. In practice: 50-character summary, blank line, 72-column body; small
and frequent commits; no `TODO` / `FIXME` / `HACK` markers.

Never write a `docs/STATUS.md` ticket id into a commit message, code or a
comment. Not because it is private — that file is public — but because the id
dies with its row, leaving a reference that resolves to nothing. Write the
invariant at the site it constrains instead.

Never write a bare number in a doc. Write it so
`gates/build-and-run-doc-claims.sh` can count it, or leave it out.

## License

dn2cpp is MIT (`LICENSE`). Opening a pull request is your agreement to contribute
under those terms; there is no CLA and no DCO sign-off. Vendored code under
`third_party/` and `internal/` keeps its upstream licence, and a change touching
one must satisfy that licence too.

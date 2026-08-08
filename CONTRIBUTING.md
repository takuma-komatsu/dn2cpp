# Contributing to dn2cpp

Read `AGENTS.md` first — build, gates, module boundaries, code style — and the
*Permanent non-goals* section of `README.md`. Those boundaries are settled and
each line says why, so a change that lands inside one is declined rather than
discussed.

## What CI can and cannot say

The hosted smoke workflows (`.github/workflows/linux-smoke.yml`,
`.github/workflows/windows-smoke.yml`, `.github/workflows/macos-smoke.yml`) run
on every pull request and are the only green a contributor can see. They answer
one question: does the tree still build across Linux, Windows, and macOS, and
does C# still reach a running native binary.

The merge gate — `./gates/pre-merge.sh` — does not run there and cannot. Its
header states each structural reason (a scons-built dn2cpp fork of the Godot
editor, Xcode plus an iOS simulator, an Android NDK, a proprietary CRI package);
read it there rather than trusting a summary. A maintainer runs it on a
provisioned machine.

So a green pull request is not permission to merge — and equally, a pull request
never goes red over a toolchain you cannot install.

Running this much locally is enough:

```bash
./gates/build-and-run-sample.sh          # console: C# → IL → C++ → native
./gates/build-and-run-multiassembly.sh   # multi-assembly (-r)
SKIP_GODOT=1 ./gates/run-all-gates.sh    # the non-Godot suite
```

**A skip is not a pass.** A gate whose optional prerequisite is absent opts out
via `gate_skip` (`gates/_common.sh`) and is counted and reported **separately**
with its reason, so the summary never claims all N passed when some never ran.
If you add a gate, opt out that way and never with `echo SKIP; exit 0`.

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

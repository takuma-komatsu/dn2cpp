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
Windows/MSVC and macOS. Each OS runs `array-core` and `transpiler-limits`
in separate jobs from the remaining primitive gates, using disjoint partitions
of the full allowlist. The checks cover language,
primitive values, arrays, strings, collections, generics, async, threads and GC
barriers.
It also checks OS APIs through filesystem, environment, synchronization,
memory-mapped file and native interop tests. Every selected gate must run. Gate
logs are uploaded, and the basic profile publishes timings in the job summary.

`culture-info-api` is an optional strict reference check, excluded from default
pre-merge and hosted smoke CI (including the wider scheduled/manual profile).
Its CLR culture display data depends on the loaded ICU version; dn2cpp uses
frozen native tables. Exclusion does not establish culture parity and is not a
passed or skipped execution. Other selected gates retain their existing checks.

To include CultureInfoAPI in pre-merge, provision the reference runtime and ICU,
then use `CONFIG=Debug bash gates/pre-merge.sh --skip-godot --with-culture`.
`--with-culture` runs the strict reference preflight before the selected suite.
The individual gate can also be run explicitly:

```bash
bash gates/check-culture-reference.sh && CONFIG=Debug bash gates/build-and-run-culture-info-api.sh
```

The preflight checks the selected SDK, CoreLib runtime and the ICU actually
loaded by that CLR. The expected versions live in
`tools/check-culture-reference/`. Unexpected versions, invariant globalization
and Windows NLS fail rather than silently changing the oracle. Native programs
do not load ICU. For local Linux reference checks, provide the reference ICU
libraries through the loader path and `DOTNET_ICU_VERSION_OVERRIDE`; another
installed ICU is not another native compatibility profile. The strict gate
retains its exact comparisons and intentional frozen divergences; there is no
portable tolerance mode. Updating a reference requires reviewing CLR output and
the corresponding frozen runtime profile together. Hosted workflows retain
isolated exact SDK provisioning without making this optional ICU preflight a
prerequisite for unrelated smoke gates.

Each OS runs the wider non-Godot suite in a single job daily and through
`workflow_dispatch`. That profile retains the OSS integration gates and other
checks outside the basic allowlist. Missing optional prerequisites remain
reported as skips in its summary and logs. Scheduled and manual runs use a
separate concurrency group, so a new push cannot cancel them.

The Godot-inclusive merge gate — `./gates/pre-merge.sh` — does not run in
hosted smoke CI. A human runs it on a provisioned desktop host. Xcode-dependent
gates are excluded automatically when Xcode is unavailable, allowing Windows
and Linux hosts to complete the merge check. On a host with Xcode they remain
strict. Use `--skip-xcode` to exclude them explicitly, or `--require-xcode` to
include them even when Xcode is unavailable. The receipt lists each excluded
gate, and excluded gates are not counted as passed. Pre-merge provisions the
Emscripten SDK itself (`gates/setup-emsdk.sh`); a host missing another required
prerequisite still runs to completion, and uncovered surfaces are reported
red at the verdict. The proprietary CRI SDK is optional: when absent, CRI gates are
reported as skips and their Web templates are not prepared. With the SDK
installed, CRI gates run under the same strict checks as the rest of the suite;
failures, missing other prerequisites and incomplete coverage remain red.

Hosted smoke CI alone is not permission to merge.

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
except for the reported CRI skips when its SDK is absent. Xcode gates omitted
by host selection or `--skip-xcode` are outside the selected suite. CultureInfoAPI
is likewise outside the default selection unless `--with-culture` is supplied.

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

Gate stdout/stderr capture, byte parity and explicit semantic record assertions
follow [the gate output contract](docs/GATE-OUTPUT.md). Run its lightweight
helper regressions when changing shared runners or output checks.

## What to work on

Choose open work from [GitHub Issues](https://github.com/takuma-komatsu/dn2cpp/issues?q=is%3Aissue%20is%3Aopen).
Read the permanent non-goals in `README.md` before proposing work. An issue must
state the problem, scope, acceptance criteria, verification and dependencies
well enough to continue on another machine. If verification needs a signing
identity, a Windows host, a device or an SDK the repository cannot carry, agree
on its owner before starting.

## Issues

One tracker: this repository. The forked Godot editor,
[takuma-komatsu/godot-dn2cpp](https://github.com/takuma-komatsu/godot-dn2cpp),
has its issues disabled, so editor problems come here too. For an editor report,
paste the build's identity as it ships — the unpacked Windows package's
`RELEASE.txt`, or on macOS the `.app`'s `DN2CPP*` `Info.plist` keys — together
with the editor's `--version` string.

GitHub Issues are the sole backlog; `docs/STATUS.md` only directs readers here.
Keep an accepted issue open while its implementation or required verification
is pending. A PR should link the issues it addresses and use closing keywords
only for issues it fully resolves when merged. Split remaining work into a
separate issue rather than closing it with an incomplete implementation.

Keep durable constraints in repository code or design docs as invariants. An
issue records actionable work; it does not replace the product contract or
authorize an unrelated expansion of scope.

## Commits and style

`AGENTS.md`'s *Naming / style* and *Write like an experienced programmer* are the
contract. In practice: 50-character summary, blank line, 72-column body; small
and frequent commits; no `TODO` / `FIXME` / `HACK` markers.

Do not reuse or cite retired `docs/STATUS.md` ticket ids. Link GitHub Issues
from PRs; write the invariant at the site it constrains in code and design docs.

Never write a bare number in a doc. Write it so
`gates/build-and-run-doc-claims.sh` can count it, or leave it out.

## License

dn2cpp is MIT (`LICENSE`). Opening a pull request is your agreement to contribute
under those terms; there is no CLA and no DCO sign-off. Vendored code under
`third_party/` and `internal/` keeps its upstream licence, and a change touching
one must satisfy that licence too.

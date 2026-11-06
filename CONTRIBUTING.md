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

The Linux workflow also offers the manual `premerge-debug` mode. Unlike the
wider smoke profile, it provisions the selected prerequisites and invokes the
actual strict, uncached `gates/pre-merge.sh --skip-godot` in Debug. Dispatch it
against the branch containing the workflow and supply the full commit SHA:

```bash
gh workflow run linux-smoke.yml --repo takuma-komatsu/dn2cpp \
  --ref YOUR_BRANCH -f mode=premerge-debug -f expected_head=FULL_COMMIT_SHA
```

The input must match both the workflow's dispatched commit and checkout before
provisioning or tests begin. The default manual mode remains `suite`; pull
request, push and scheduled smoke checks are unchanged. The manual pre-merge
uploads its setup evidence, exact source identity, full log root and receipt
when produced, including failure logs. Hosted time or disk exhaustion is not a
pass. Its non-Godot receipt does not replace human Godot-inclusive verification
for Godot-specific changes.

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

Normal pre-merge validation uses the default High reachability policy and always
checks basic High/Medium static closure, metadata-only retention, deferred
initializers, and callable intercepts. The representative preservation profile
uses rewritten DLLs; full mode adds the raw-input twin. Extended API retention
matrices remain opt-in: their omission is recorded separately from the basic
Medium regressions that ran. The pre-merge wrapper fixes these selections even
when the invoking shell exports a different development profile.

Normal gate profiles reuse their themed aggregate binaries for output, status,
metadata and runtime assertions. The array gate also keeps isolated finite-search
coverage. Preservation compares the default output with explicit High without
recompiling identical C++; manifest-resource limits rerun the control binary.
Reflection type checks retain the trimmed aggregate and ordinary leaves. The
HTTP gate shares a transport image across live GET, request-body/header and
trusted/untrusted TLS checks, running each mode in a separate process.
Emit-order stability compares the shared-generics aggregate across representative
combinations of worker count, strict completion, drain order and output location.
Argument and culture variants reuse their compiled images. Native-asset publication
reuses each SDK-built image for failure, atomic-switch and generation-pruning checks.

Default-culture normally checks representative numeric cultures, environment
precedence and frozen unknown-region currency. Its full profile adds the detailed
locale, fallback, identifier, Windows user-setting and libc-injection matrices.

Detailed isolation, preprocessing, backend and culture matrices are separate
development checks. Their omission does not establish equivalent coverage:
independent roots, raw/ILDiet pairs, authority rejection inventories, individual
emit-order axes and specialized HTTP packaging contracts require the relevant
full profile. Gate logs and the pre-merge receipt mark them **skipped (not
selected)**, never passed.
Pre-merge pins all of the following selectors to `0`, even if the invoking shell
requests full coverage. Full development coverage remains explicitly selectable:

```bash
DN2CPP_DATETIME_FULL=1 CONFIG=Debug bash gates/build-and-run-datetime.sh
DN2CPP_ARRAY_FULL=1 CONFIG=Debug bash gates/build-and-run-array-core.sh
DN2CPP_MANIFEST_RESOURCES_FULL=1 CONFIG=Debug bash gates/build-and-run-manifest-resources.sh
DN2CPP_PRESERVE_CONTROL_FULL=1 CONFIG=Debug bash gates/build-and-run-preserve-control.sh
DN2CPP_REFLECT_INVOKE_FULL=1 CONFIG=Debug bash gates/build-and-run-reflect-invoke.sh
DN2CPP_REFLECT_TYPES_FULL=1 CONFIG=Debug bash gates/build-and-run-reflect-types.sh
DN2CPP_TRANSPILER_LIMITS_FULL=1 CONFIG=Debug bash gates/build-and-run-transpiler-limits.sh
DN2CPP_EMIT_ORDER_FULL=1 CONFIG=Debug bash gates/build-and-run-emit-order-stability.sh
DN2CPP_MATH_FULL=1 CONFIG=Debug bash gates/build-and-run-math-subset.sh
DN2CPP_MARSHAL_FULL=1 CONFIG=Debug bash gates/build-and-run-marshal-pinning.sh
DN2CPP_HTTP_FULL=1 CONFIG=Debug bash gates/build-and-run-http-get.sh
DN2CPP_HOTUPDATE_FULL=1 CONFIG=Debug bash gates/build-and-run-hotupdate-subset.sh
DN2CPP_THREADING_FULL=1 CONFIG=Debug bash gates/build-and-run-threading-primitives.sh
DN2CPP_STRING_FULL=1 CONFIG=Debug bash gates/build-and-run-string-core.sh
DN2CPP_STRING_INTERP_FULL=1 CONFIG=Debug bash gates/build-and-run-string-interp.sh
DN2CPP_DECIMAL_FULL=1 CONFIG=Debug bash gates/build-and-run-decimal-ops.sh
DN2CPP_DICT_FULL=1 CONFIG=Debug bash gates/build-and-run-dict-collections.sh
DN2CPP_CUSTOM_ASYNC_TASK_FULL=1 CONFIG=Debug bash gates/build-and-run-custom-async-task.sh
DN2CPP_EXCEPTION_MESSAGE_FULL=1 CONFIG=Debug bash gates/build-and-run-exception-message-subset.sh
DN2CPP_MULTIASSEMBLY_FULL=1 CONFIG=Debug bash gates/build-and-run-multiassembly.sh
DN2CPP_TRIM_REFLECTION_FULL=1 CONFIG=Debug bash gates/build-and-run-trim-reflection.sh
DN2CPP_OBFUSCATION_FULL=1 CONFIG=Debug bash gates/build-and-run-obfuscation.sh
DN2CPP_PLATFORM_ISA_FULL=1 CONFIG=Debug bash gates/build-and-run-platform-isa-native.sh
DN2CPP_DEFAULT_CULTURE_FULL=1 CONFIG=Debug bash gates/build-and-run-default-culture.sh
DN2CPP_WASM_CONSOLE_FULL=1 CONFIG=Debug bash gates/build-and-run-wasm-console.sh
DN2CPP_VECTOR_FULL=1 CONFIG=Debug bash gates/build-and-run-vector-value-wasm.sh
DN2CPP_VECTOR_FULL=1 CONFIG=Debug bash gates/build-and-run-vector-probe.sh
DN2CPP_VECTOR_FULL=1 CONFIG=Debug bash gates/build-and-run-vector-probe-scalar.sh
DN2CPP_SELFHOST_PRIM_FULL=1 CONFIG=Debug bash gates/build-and-run-selfhost-prim-subset.sh
```

The hotupdate aggregate reuses its native base for register and stack patches.
The Wasm vector gate reuses its ILDiet image across numeric, fixed-width and
unsupported-value checks; the raw-input twin requires the full profile.
The Wasm console aggregate shares string, exception, weak-reference, clock and PAL
drivers while keeping finalizer, HTTP and multidimensional-array images separate.
The ISA gate shares its native X86+Arm image across architecture-selected process
runs. Independent input-root isolation remains part of their full profiles.

The vector gates retain full-driver output/status parity on Highway and scalar
backends. The selfhost primitive gate retains the source-built aggregate and
tool-process fixture. Completion witnesses ensure the aggregate drivers reach
their final sections. No production optimization or selected comparison assertion
is relaxed by choosing a representative profile.

Extended Medium API matrices remain available separately, with their original
assertions and failure status:

```bash
DN2CPP_SELFHOST_PRIM_FULL=1 DN2CPP_KNOWN_CONSTANT_LEVELS="High Medium" CONFIG=Debug bash gates/build-and-run-selfhost-prim-subset.sh
DN2CPP_PRESERVE_CONTROL_FULL=1 DN2CPP_PRESERVE_CONTROL_MEDIUM=1 CONFIG=Debug bash gates/build-and-run-preserve-control.sh
```

These optional checks do not establish full Medium completion. Medium remains
incomplete; preserve each failed run and its exact source/input identity.

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

Gate-owned Python calls use `gate_python`, including calls passed to capture
helpers. SDK-free tests and setup aids can source `gates/_python.sh` without
loading the full host setup. The shared entry point selects a working Python 3,
preserves launcher arguments and uses UTF-8 for child interpreters.
`DN2CPP_PYTHON` accepts an executable path (including spaces) or a
whitespace-separated command such as `py -3`; it is not evaluated as shell code.
Selection is reused within a gate until that override or `PATH` changes. Use
`gate_python_exec` only in child shells whose PID is used for server cleanup or
a watchdog. Tool-owned interpreters, such as Emscripten's, retain their separate
prerequisite checks.

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

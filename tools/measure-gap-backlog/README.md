# Measure the observed transpilation backlog

The process-budget runner requires Python 3 on a POSIX host (Linux or macOS).
The compiler sidecars themselves are platform-independent.

This tool runs the existing `--measure` diagnostic mode and groups its failures by
arithmetic/Vector operation hints. It does not implement missing operations or
emit C++. Normal transpilation still stops on unsupported input.

Create a JSON command file containing the **same compiler, target assembly,
references and policies** used by the failing transpilation. An explicit
`System.Private.CoreLib.dll` reference is required so the host cannot silently
select an unrecorded CoreLib. For example:

```json
["dotnet", "exec", "/path/to/dn2cpp.dll", "/path/to/app.dll",
 "-r", "/path/to/System.Private.CoreLib.dll", "--auto-ref",
 "--no-ildiet", "--stripping-level", "medium", "--jobs", "8"]
```

The runner deliberately covers console measurement. It rejects project-directory
discovery, engine/hot-update/patch modes and explicit external-output flags such
as `--ildiet-output` and `--dump-isa-surface`: their extra input/output or backend
scope is not bound by this small runner. Explicit `--link-xml` files are bound by
content. Semantic input paths and the compiler path must not use symlink aliases:
the compiler's lexical-directory resolution would not match a resolved-path
inventory. These restrictions do not change the compiler's normal CLI.

Then run from the repository root:

```bash
python3 tools/measure-gap-backlog/measure.py \
  --command-file command.json --output artifacts/gaps-before

# After a compiler change, use the same target/reference assembly bytes:
python3 tools/measure-gap-backlog/measure.py \
  --command-file command-after.json --output artifacts/gaps-after \
  --baseline artifacts/gaps-before/report.json
```

The output directory must be new. The runner replaces any `-o`/`--output` option,
adds `--measure`, records process termination and input identities, and retains
stdout/stderr even when no compiler report is produced. Budgets are configurable
with `--timeout` (seconds) and `--max-output-bytes` (aggregate output bytes).
It does not set a process file-size limit.

## Reading the result

- `report.json`: machine-readable observations, grouping, coverage and optional
  baseline delta. Each raw gap retains its phase, pass, method, C++ symbol,
  exception type and message.
- `summary.txt`: semantic-family/mechanism hints and observed counts.
- `raw/s0-gaps.tsv`: compatible legacy five-column report.
- `raw/s0-gaps-detail.tsv`: seven columns, adding the C++ symbol and pass.
- `raw/s0-measure.tsv`: versioned compiler counters and coverage status.
- `command.json`, `stdout.log`, `stderr.log`: reproducibility and failure evidence.

A completed diagnostic run can contain failures. Its zero exit status is **not**
a successful transpilation. An aborted run has unavailable (`null`) counts,
not zero gaps. No native compilation, linking, backend epilogue or execution is
validated by this measurement.

The counts are an **observed lower bound**, not the total remaining work:

- A failed scan or compilation reports its first failure. Later instructions in
  that body, and dependencies only discoverable through them, may be unvisited.
- Planning errors not repeated in emission remain marked `pass=planning`:
  finalization can discard a failed canonical trial. Separate emission and
  unreplayed-planning counters prevent those trials being mistaken for retained
  emission failures.
- `discoveryStatus=partial` records failed reachability work.
- `bodyPassStatus=truncated` records layout/specialization failures, including
  those in the planning pass. The symbol sweep is skipped when unsound.
- A method compiling successfully is not proof that every arithmetic/Vector API
  is implemented. The tool measures the selected input and retention policy,
  not the entire CoreLib API surface.
- Grouping is heuristic. One family can require several fixes; different
  families can share one fix. Width and unsupported-element hints do not prove
  that an exception guard is correct. Inspect the retained messages and symbols.

`groups` keeps operation-level detail. `backlogFamilies` combines related
operations with a diagnostic-mechanism hint, retaining the operations, concrete
symbols and target owners. For example, a static interface with no IL body is
kept separate from a missing arithmetic lowering; it is not automatically a
request to implement an extern method. `categorySummary` separates arithmetic,
Vector and other BCL observations, with separate emission and planning counters.
They describe retained emission attempts and discarded planning trials, not a
trace of executed control flow.

A `failureId` binds a concrete reported symbol, target, classification, exception
and normalized diagnostic (including a reported overload signature). It
deduplicates the same first failure reported by reachability and compilation,
ignoring retention-chain text. It is an observation identity, not proof of an
independent root cause; ambiguous diagnostics stay explicitly unclassified.

`remainingWorkCount`, `completionPercentage` and `unknownGapCount` deliberately
stay `null`. No estimate of engineer time is inferred from method counts.

## Comparing runs

The runner binds target/reference files, sibling DLL contents (for auto-reference
resolution), default-shim candidates (`DnZlib`, `DnBrotli`, `DnHttp`) and the
auto-added `Dn2Cpp.Runtime` support assembly beside the compiler, and non-output
policies by content. These candidates are bound conservatively even when a
particular run does not inject them. Runtime-support bytes are input identity,
not merely compiler implementation; Debug and Release builds can differ.
`DN2CPP_*`
(except restore configuration), `COMPlus_*`, and semantic `DOTNET_SYSTEM_*`,
`DOTNET_Enable*`, `DOTNET_Jit*` overrides are also bound. Compiler identity is recorded
with implementation DLLs and JSON configuration beneath its output directory
(including the ILDiet companion), either ILDiet native apphost candidate, and the host executable,
but implementation identity is excluded from the comparison fingerprint so
compiler changes can be measured. All recorded files are rehashed after the run
to detect mutation. Other unrecorded machine or OS differences can still affect
results; this fingerprint is not a hermetic-environment guarantee.
The same target and references must be kept fixed; rebuilding the target from a
changed compiler produces a different corpus and is not a comparable baseline.

The delta lists **newly observed**, **still observed**, and **no longer observed**
families and concrete failure identities. “No longer observed” is not “proven
fixed”: an earlier failure or a changed discovery path can hide a later one.
Coverage fields must be read beside
the delta. Incompatible inputs, policies or classifier versions are rejected as
comparisons. An aborted run is never treated as a resolution of prior gaps.

## Verification

```bash
python3 -m unittest discover -s tools/measure-gap-backlog -p 'test_*.py'
```

Managed integration tests additionally verify multiple failing bodies, normal
fail-fast behavior, report metadata and the no-native-output contract. The emitter
fixture uses controlled models and real body compilation to cover synthesized,
HTTP and Enum wrapper failures, the intrinsic trap fallback, late-discovered scan
failures and a discarded canonical planning failure. The report fixture also
checks parallel failure handling, including both must-escape exception types.
With the CLI built and `CORELIB` set:

```bash
dotnet build tools/measure-gap-backlog/fixture/GapFixture.csproj \
  --artifacts-path /absolute/new/gap-fixture
dotnet build tools/measure-gap-backlog/fixture/ReportFixture.csproj \
  --artifacts-path /absolute/new/report-fixture
dotnet build tools/measure-gap-backlog/fixture/EmitterFixture.csproj \
  --artifacts-path /absolute/new/emitter-fixture
python3 tools/measure-gap-backlog/check_cli.py \
  --cli /absolute/path/dn2cpp.dll --corelib "$CORELIB" \
  --fixture /absolute/new/gap-fixture/bin/GapFixture/debug/GapFixture.dll \
  --report-fixture /absolute/new/report-fixture/bin/ReportFixture/debug/ReportFixture.dll \
  --emitter-fixture /absolute/new/emitter-fixture/bin/EmitterFixture/debug/EmitterFixture.dll \
  --emitter-input /absolute/new/emitter-fixture/bin/EmitterInput/debug/EmitterInput.dll \
  --output /absolute/new/measure-contract
```

# Gate output contracts

## Capture before interpretation

A program's output is evidence, not a shell string. `gate_capture PREFIX
COMMAND...` stores `PREFIX.stdout`, `PREFIX.stderr` and `PREFIX.status` without
newline conversion. Failure to create or write the observation is a helper
failure and must abort the assertion path; never ignore that return status,
even in a shell region that disables `errexit`. A new capture invalidates the
previous status receipt before opening its streams. A child process failure is
recorded separately: the caller must check that status, including when stdout
already matches. Keep the raw
files until the gate's artifacts can be discarded. Never replace them with a
normalized view. `gate_capture_ok PREFIX COMMAND...` is the success-only
adapter: it captures the same evidence, requires a zero status and replays
stderr on failure.

Use `gate_output` from `gates/_common.sh` to call `gates/gate-output.py`.
The helper has no SDK or native build dependency.

```bash
gate_capture "$out/native" run_bounded "./$out/$project$EXE_EXT" || exit 1
gate_output status "$(cat "$out/native.status")" 0
gate_output compare "$out/native.stdout" "$out/expected.stdout"
gate_output byte-witness "$out/native.stdout" 'section end' --policy crlf
```

`gate_assert_text ACTUAL_FILE EXPECTED_LITERAL` checks an inline text fixture
with explicit CRLF-pair equivalence and an optional final LF. It saves the
literal beside the observation for diagnostics. This is not a parity or binary
comparison; use `gate_output compare` for those.

Shell command substitution discards trailing LF bytes and cannot carry NUL.
Do not use it to capture program output for a new assertion. Reading the decimal
status file into a shell argument is safe; reading stdout that way is not.

## Exact bytes and explicit text policies

The default `compare` policy is byte identity, including CR, NUL, encoding,
spaces, blank records and the final newline. Same-host CLR/native parity uses
this policy. Encoding, binary payload, pipe and NUL regressions must stay on
this path. Do not weaken their checks to make a text assertion easier.

A semantic text assertion may opt into `--policy crlf`. This replaces only CRLF
pairs with LF, on every host. It does not remove lone CR, trim spaces, decode
bytes, fold case, sort records or collapse empty records. Semantic operations
reject NUL rather than interpreting binary data as text. A host OS check is not
a text policy: Linux can read CRLF fixtures and Windows can read LF output.

`compare` also accepts `--terminal-newline optional` for a contract that regards
a final LF as an optional terminator. It removes at most the final LF from each
side after the selected CRLF policy. Additional trailing blank records remain
significant. The default is `--terminal-newline exact`; use it for raw parity
and any test of output termination itself.

`view INPUT OUTPUT --policy crlf` makes a separate semantic view for a remaining
text-tool consumer. It preserves terminal LF bytes and rejects overwriting the
raw input. Prefer an assertion command below instead of a view followed by an
ad hoc `strip`/`grep` pipeline.

## Record assertions

Record commands split on LF and treat the final LF as a terminator rather than
an extra empty record. Interior empty records and additional final empty records
are preserved. Text record assertions reject NUL even with `--policy exact`. Markers supplied as
arguments are UTF-8 literals and match complete records, not substrings or regexes.
CRLF equivalence still requires an explicit `--policy crlf`.

- `records ACTUAL EXPECTED` compares the complete record sequence.
- `byte-witness ACTUAL RECORD... --count COUNT` is the recommended execution-marker
  API for captured program output. It matches NUL-free complete records in an
  arbitrary byte container without decoding or deleting anything. A NUL-bearing
  record cannot match a NUL-free needle; unrelated binary records are ignored.
  Counts, whitespace and payload CR remain significant. This is not a claim that
  the whole stream is text, nor a replacement for raw parity or exit checks.
  Retain structural boundaries and existing multiplicity requirements: arbitrary
  binary data can itself contain an LF-delimited marker.
- `witness ACTUAL RECORD... --count COUNT` checks the exact multiplicity of
  every supplied record in a text-only container; the default count requires each to occur uniquely.
  `--count any` requires presence without imposing uniqueness.
  `--binary-container` remains a compatibility alias for `byte-witness` using
  the same framing and counting implementation. New program-marker assertions
  should use the named byte API instead.
- `witnesses ACTUAL EXPECTED` checks the expected witness sequence and exact
  multiplicity, allowing unrelated records between the witnesses.
- `match-record ACTUAL PATTERN --count COUNT` applies a Python byte regular
  expression to each complete NUL-free record, using full-match semantics.
  Use explicit patterns for variable diagnostics, not automatic translations of
  shell-tool regex dialects. `any` requires at least one matching record.
- `prefix ACTUAL EXPECTED` and `suffix ACTUAL EXPECTED` check ordered prefix and
  suffix record sequences. `prefix --before MARKER` checks the whole prefix
  preceding a unique marker; `suffix --after MARKER` checks the whole suffix
  following it.
- `section ACTUAL EXPECTED --start START --end END` checks the complete sequence
  strictly between unique, correctly ordered boundary records.

`select INPUT OUTPUT --before MARKER --policy exact|crlf` writes records
preceding a unique marker, excluding it; `--from MARKER` includes the marker
and everything following it; `--through MARKER` includes everything through the
marker and its original terminator. Combining `--from START --through END`
selects that inclusive range, requiring unique markers in source order.
`--exclude-start START --exclude-end END`
removes the inclusive range between unique, ordered markers and preserves both
remaining byte spans. The policy is mandatory. Selection rejects
missing or repeated markers, and an output that aliases its raw input. A
`--from` selection preserves the original bytes from the marker onward;
`--before` preserves all original bytes preceding it, including terminators.
`--before-prefix PREFIX` requires exactly one record starting with a nonempty,
NUL-free prefix and preserves all bytes before it. Use this for boundaries whose
remaining text is platform-owned, without reconstructing output through text tools.
`--first-record` copies the first raw record, including its terminator when present,
and rejects empty input. Apply semantic comparison only after this raw extraction.
The policy controls marker matching only: selection never normalizes its output,
so a later comparison applies its CRLF policy only once. Use this for a derived
snapshot, never to overwrite raw evidence. Binary records outside the marker
are retained verbatim; a selected text slice must still pass the NUL-free
precondition of a downstream semantic assertion.

`omit-prefix INPUT OUTPUT --prefix TEXT [--prefix TEXT...] --count N|all --policy exact|crlf`
omits whole records matching each explicit prefix, requiring the declared count
for every prefix before writing. `all` explicitly permits zero or more matches.
Remaining records retain their original bytes.
Use it only for a documented intentional divergence, with separate witnesses for
the omitted records.

Do not reconstruct raw output or normalized views with host text-mode tools.
A normalized view may still contain payload CR followed by a separator LF;
another text-mode read can silently consume that CR. Filter views as bytes with
`--policy exact` and compare them exactly, or filter raw spans and normalize once.
ISA probe records use a narrower schema: no payload CR or NUL is valid. Their
binary parser validates once-normalized files before contract, adjacency, plan,
or count checks; residual CR is rejected rather than normalized again.

`byte-prefix ACTUAL EXPECTED` and `byte-suffix ACTUAL EXPECTED` compare literal
byte spans, including NUL and terminators. They have no normalization policy
and are the prefix/suffix APIs for binary or encoding observations.

A witness proves execution, not parity. Keep the raw comparison and exit check
as well. A prefix assertion proves unchanged preceding behavior; it does not
prove that the appended section ran. Pair it with a witness or section check.

## Exit status and cross-target comparisons

`status ACTUAL EXPECTED` requires exact equality. Ordinary success checks use
an expected zero. A parity gate that intentionally exercises failure may compare
the native status against the CLR status instead; matching output alone never
passes it. Reserved infrastructure statuses `124`, `125`, `126`, `127`, `130`,
`137` and `143` fail either status policy, even when equal or in the same
failure class. A timeout, missing executable or terminated runner is not
evidence of matching program behavior. Intentional abort status `134` remains
available to failure-parity gates.

`abort-parity ACTUAL_PREFIX EXPECTED_PREFIX --host HOST --witness RECORD
--diagnostic RECORD` is a separate, explicit expected-abort contract over raw
`.stdout`, `.stderr` and `.status` files. It requires exact stdout parity, a
unique nonempty program witness in each stdout, and the declared first diagnostic
record in both stderr streams. Both statuses must be equal and abnormal.
This evidence-bearing contract permits Windows status `127`, which can
also represent an unhandled managed abort. Other reserved statuses still fail;
ordinary `status` checks continue to reject `127` unconditionally. Fixed program
markers and mode-specific diagnostics must be declared by the caller, never
inferred from arbitrary nonempty output.

`corelib_diff_abort_gate PROJECT WITNESS DIAGNOSTIC [CLI_ARGS...]` declares this
paired contract for a deliberately aborting native bucket. Ordinary
`corelib_diff_gate` calls remain strict, even after an aborting bucket ran in the
same shell. The policy, witness and diagnostic are separate cache-context inputs;
the sourcing gate's identity and all ordinary sample inputs remain in the key.

`native-abort PREFIX --host HOST --diagnostic RECORD` declares a native-only
abort, without claiming CLR parity. It requires either `--witness RECORD` for a
unique nonempty stdout record or `--empty-stdout` for exactly zero stdout bytes,
plus the exact declared first stderr record and an abnormal status. It uses the
same evidence and status rules as `abort-parity`, including the narrowly allowed
Windows `127` collision. Empty or missing diagnostics, success, clean failure
status `1`, and other reserved statuses fail. Keep any existing stack, shape or
message checks alongside this declaration. Neither abort command changes the
ordinary status policy or creates an implicit policy for later commands.

Wasm is a cross-target comparison: its text output may explicitly use CRLF-pair
equivalence against a host CLR. Its `status --policy success-class` requires
both processes to succeed or both to fail because abort status values differ
between runtimes. That policy is not appropriate for a gate whose contract is
successful execution: such a gate must assert zero. Neither Wasm policy changes
the default same-host contract or authorizes normalization of binary output.
For the Windows `Finalizers` abort, `wasm-abort` requires the finalizer's stdout
witness and distinct first stderr diagnostics on both sides before accepting
different abort statuses. It compares stdout with CRLF-pair equivalence and
rejects reserved statuses on other hosts. Ordinary Wasm status checks remain
strict.

## Migration boundaries and cache inputs

The common file-based APIs are the path for new checks. Legacy `assert_output`,
`strip_cr_win` and `strip_cr_win_file` string/host-based interfaces remain only
for callers not yet migrated; they are not the new semantic policy. Positional
legacy capture hooks retain their existing argument meanings.
`gate_capture_diff_files` is the file-based hook;
`gate_capture_diff_output` retains legacy string arguments. A path must never
silently be treated as output.

Migrate a caller as a unit: capture raw files and statuses, preserve its exact
parity assertion, declare semantic policies for witnesses and fixture checks,
then remove redundant string normalization. Do not globally rewrite unrelated
checks or treat an old lossy capture as raw evidence. Existing unconverted
callers retain their limitations until individually migrated.

The Python output helper, ISA record parser and policy document are gate-cache inputs. An edit to output
comparison semantics must invalidate cached gate results, even when the sample
and native binary have not changed.

## Lightweight verification and enforcement

Run the helper and lint regressions without building the transpiler:

```bash
python gates/test-gate-output.py
bash gates/test-gate-output-shell.sh python
python gates/test-lint-gate-output.py
python gates/lint-gate-output.py --base origin/main
```

The `gate-output-contract` workflow runs on Linux and real Windows, independently
of the SDK/native smoke jobs. This proves helper portability, not the full native
suite or the merge gate described in `CONTRIBUTING.md`.

The lint is an incremental ratchet over gate shell scripts. It rejects new
`strip_cr_win`/`strip_cr_win_file` call sites and recognized `tr` CR-deletion
forms and same-line `witness --binary-container` spellings relative to an explicit
review base. Use `byte-witness` for new mixed-container markers. Unchanged legacy lines are allowed;
adding another occurrence or moving one into another file is not. Local checks
include untracked gate scripts. An unavailable base fails rather than silently
skipping enforcement. CI checks the PR base or the preceding push revision.
The lint does not infer shell data flow or whether a variable denotes a parity-backed
capture. Choosing strict text witnesses versus byte execution markers remains a
reviewed contract decision; it is not enforced by a blanket grep ban.

This is deliberately not a shell parser or a universal normalization detector.
It does not ban `grep`, source inspection, tool-version parsing, or byte-exact
file comparisons. Review still checks custom normalization code and verifies
that a semantic policy is appropriate for each assertion.

An intentionally aborting CLR/native pair may declare distinct exact first stderr
records with `abort-parity --oracle-diagnostic RECORD` (default: the native
`--diagnostic`). `corelib_diff_abort_gate` accepts this optional pair immediately
after its project, witness and native diagnostic, before transpiler arguments.
Both declarations and the actual host selector are cache inputs. This is used by
Finalizers, whose native finalizer fatal message differs from the CLR unhandled
exception message; neither diagnostic is weakened to a substring.

Hot-update process observations reject reserved infrastructure statuses by default.
A fatal refusal uses the local `hotupdate_capture --expect-abort` declaration
(after its output/status/stream arguments), supplying an exact first stderr
record and a unique stdout witness or `--empty-stdout`. This delegates to the
same `native-abort` proof; Windows status 127 is not accepted by ordinary
captures or by a diagnostic alone. Raw stdout and stderr stay separate for
these declarations. The actual host selector is part of the gate cache context.


## Optional strict culture reference check

`culture-info-api` is excluded from default pre-merge and hosted smoke CI,
including scheduled/manual smoke suites. CultureInfoAPI compares host CLR/ICU
data with frozen native culture tables; an ICU data difference is not a newline
mismatch. The optional gate retains its exact comparisons and declared frozen
divergences. No shared output policy is relaxed, and no portable tolerance or
candidate baseline exporter is enabled.

On a host provisioned with the reference SDK, CLR and ICU versions, run:

```bash
bash gates/check-culture-reference.sh && CONFIG=Debug bash gates/build-and-run-culture-info-api.sh
# Or include the gate in the Debug non-Godot pre-merge selection:
CONFIG=Debug bash gates/pre-merge.sh --skip-godot --with-culture
```

`--with-culture` requires the strict reference preflight before the selected
suite runs. Default exclusion is reported as an exclusion, not a pass or a
prerequisite skip. Smoke workflows retain their exact SDK installation but do
not require the optional ICU reference preflight for unrelated gates.

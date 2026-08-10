# AGENTS.md — dn2cpp

Working rules for **anyone** contributing to dn2cpp — human or coding agent.
The repository is the single source of truth. Read this file, `README.md`
(architecture overview) and `docs/ARCHITECTURE.md` (extension points — where to
add what) before starting; open work is listed in `docs/STATUS.md`.

## What this project is

.NET IL → C++ transpiler plus a C++ runtime: an IL2CPP-equivalent for .NET. A
pure-.NET core turns any IL assembly into a native executable; a Godot layer on
top emits GDExtension native libraries.

## Module boundaries

The core knows nothing about Godot; the Godot layer plugs in through two hooks.
The contract between lanes is `IEmitBackend` + `ICallIntrinsics`
(`src/Dn2Cpp.Transpiler/`).

| Lane | C# | C++ runtime | Owns |
|------|----|-------------|------|
| **Core** | `src/Dn2Cpp.Transpiler/` (`ConsoleBackend`) | `runtime/core/` | IL features, generics, arrays, BCL-as-IL, tree-shaking, multi-assembly |
| **Godot** | `src/Dn2Cpp.Godot/` (`GodotBackend`, `GodotCallIntrinsics`) | `runtime/godot/` | ClassDB registration, engine method binds, signals, shims |
| **DotnetModule** | `src/Dn2Cpp.DotnetModule/` | `runtime/dotnetmodule/` | `--dotnet-module`: mono-module drop-in against the real GodotSharp |
| **CLI** | `src/Dn2Cpp.Cli/` (composition root) | — | arg parsing, backend selection |

## Backlog workflow

`docs/STATUS.md` is the only place open work lives, and it lists only genuinely
open tickets. When a ticket is done and merged, **delete its row**. Git history
is the authoritative record of how a change was made.

GitHub issues are the intake, not a second backlog, and the reference is
one-directional: a row may cite an issue number, which is permanent and
resolvable, while nothing cites a row — the row is deleted when the ticket
lands. An issue taken as work becomes one row, and is closed saying so.

## A ticket id lives in `docs/STATUS.md` and nowhere else

A ticket id is a temporary handle on a conversation. The moment the ticket
closes it means nothing to a reader and cannot be looked up. So do not write one
into code, comments, gate scripts, samples, other docs or commit messages. What
belongs at a call site is the invariant itself — state the constraint, not the
ticket that discovered it.

## Write like an experienced programmer

Comments and docs are compressed information. Write only: an invariant, the
reason behind a non-obvious choice, or the failure a piece of code prevents. Do
not write a restatement of the code, project history, rejected alternatives,
dated measurements, or a bare count. One or two lines is the norm; a block past
about five is almost always wrong. Coding agents are bound hardest by this —
their default is far too long.

## Build, run, and verification gate

Build the CLI with `dotnet build src/Dn2Cpp.Cli -c Release`; run it with
`dotnet run --project src/Dn2Cpp.Cli -- <assembly.dll> [-r <ref.dll>] [-o <dir>]
[--gdextension]`. The native build of the runtime plus the generated output goes
through CMake (`runtime/CMakeLists.txt`, Ninja generator) — the sole build
backend; the gate scripts are thin wrappers around it.

The regression gate is the whole `gates/build-and-run-*.sh` suite, run through
the parallel runner. **It must exit 0 before you commit:**

```bash
./gates/run-all-gates.sh                      # pre-builds once, runs all gates in parallel
SKIP_GODOT=1 ./gates/run-all-gates.sh         # skip the Godot gates (faster smoke check)
JOBS=8 ./gates/run-all-gates.sh               # override parallel job count (default: nCPU)
DN2CPP_GATE_CACHE=0 ./gates/run-all-gates.sh  # force every gate to actually run
DN2CPP_REQUIRE_ALL=1 ./gates/run-all-gates.sh # every gate must RUN — a skip is a failure
./gates/pre-merge.sh                          # THE MERGE GATE
```

**Before a merge, run `./gates/pre-merge.sh`.** It runs the Release suite then
the Debug suite, each under `DN2CPP_REQUIRE_ALL=1 DN2CPP_GATE_CACHE=0`, with
`gates/verify-culture-invariance.sh` ahead of both and, ahead of the suites, a
self-host rebuild (`gates/selfhost-emit.sh`) whenever the binary's stamp no
longer matches the source tree — the fork lane's gates require that binary, so
there is no separate manual step to run first. It re-derives its verdict from
the run's own artifacts rather than trusting an exit code.
The hosted smoke workflows (`.github/workflows/linux-smoke.yml`,
`.github/workflows/windows-smoke.yml`, `.github/workflows/macos-smoke.yml`) are
**not** that gate: the cross-toolchain gates cannot run on a hosted runner, so CI
runs the console lane and the `SKIP_GODOT=1` suite in normal mode. Passing them
is not permission to merge.

For a quick manual check, one script per lane:
`gates/build-and-run-sample.sh` (console: C# → IL → C++ → native),
`gates/build-and-run-multiassembly.sh` (multi-assembly `-r`) and
`gates/build-and-run-godot-sample.sh` (GDExtension in a real engine).

### A skip is not a pass

A few gates need something the repository cannot carry (an Android NDK, an Xcode
simulator, Emscripten). They opt out via `gate_skip` (`gates/_common.sh`), which
exits 77; the runner counts skips **separately** from greens and names each with
its reason, and `DN2CPP_REQUIRE_ALL=1` turns any skip into a failure. Never
write `echo SKIP; exit 0` — the runner fails a gate that exits 0 having printed
`SKIP`.

**A declared expected partial is a pass — and is still named.** A section barred
by a *structural, permanent* limit whose surface another gate asserts for real
calls `gate_expected_partial`, which passes in every mode. Its reason must be
written at the call site, must fit on **one line**, and must **name** the gate
that covers the surface. "Structural" means no configuration of either side can
close it — establish that first; a declaration silences a comparison, not a
cause. An undeclared `gate_partial` remains a failure under REQUIRE_ALL.

### `CONFIG=Debug` must be green too

A Debug build arms the shared-generics backstop
(`CppEmitter.AssertSharedBodySymbols`) for **every** gate, whereas Release arms
it on one. The Debug suite is therefore the only run that asserts the backstop
across the whole corpus, and a regression that lets a shared canonical body name
a grouped instantiation's symbol is red only there. Run it by hand while you are
touching shared generics, canonicalization or the emit pipeline.

### Result cache and machine lock

Gates cache their green result keyed on the transpile output plus every other
input, so a rerun with unchanged inputs reports "cached green" without compiling
(`DN2CPP_GATE_CACHE=0` disables; REQUIRE_ALL refuses cached partials). Native
compiles go through `ccache` when installed (`DN2CPP_NO_CCACHE=1` opts out).

The Godot engine and the iOS simulator are machine singletons, so the Godot
gates — run by the suite or by hand — take a machine-wide lock
(`suite_machine_lock`, `gates/_common.sh`). Membership lives in
`DN2CPP_MACHINE_LOCK_GATES`, which restates the runner's chain set; the runner
diffs the two every suite and dies naming the drift, because a gate that
silently stopped locking is the fail-open direction. `gates/verify-locks.sh` is
a manual harness for the lock machinery itself.

### Prerequisites

.NET SDK (net10.0), `clang++` (C++17), `cmake` (≥3.20) + `ninja`, Godot on PATH
(override with the `GODOT` env var), and `openssl` — the HTTPS gate mints a
throwaway CA at run time, because no TLS key may be checked in. A gitignored
`extension_api.json` at the repository root is required even for `SKIP_GODOT=1`
runs; on a fresh clone generate it with `godot --headless --dump-extension-api`.

A python too, and the Web export gates hold it to a floor rather than to
working: emcc is a launcher over python, so the fork's exporter refuses a stale
one mid-link. macOS answers `python3` with an Xcode stub that clears every
liveness test and is refused — install a real one. The floor itself is nowhere
in this repository; `godot_fork_emcc_python_check` reads it off the fork.

No hand-installed Emscripten: `gates/setup-emsdk.sh` unpacks the SDK
`gates/expected/emsdk-pin.txt` pins, and every wasm gate resolves one through
`dn2cpp_emsdk_resolve`. That SDK carries the node `gates/expected/node-pin.txt`
pins, inside itself: `emcc` runs one on every link, and the SDK's own config is
what names it.

`gates/expected/buildtools-pin.txt` pins a cmake and a ninja too, but that pin
governs only what the **toolchain bundle ships**: the repository's own gates
build through the host's pair, which is why the row above still asks for one.
Run `gates/setup-buildtools.sh` all the same, before `gates/setup-godot-fork.sh`
packages a bundle — packaging without it merely warns, and the two editor-export
gates then fail (not skip) asserting the export used the bundle's own cmake. The
node pin reaches further than that: `dn2cpp_emsdk_resolve` puts the resolved
SDK's node ahead of any host one, so the gates link through the pinned node the
bundle ships rather than through whatever the machine happens to have.

Two subsystems are vendored and on by default: the Boehm GC (`third_party/bdwgc`;
`DN2CPP_NO_GC=1` opts out) and the HTTP/HTTPS transport (`DN2CPP_USE_CURL`:
libcurl over Mbed TLS, verifying against `third_party/cacert/`;
`-DDN2CPP_USE_CURL=OFF` opts out and fails loudly at link).

## Consolidated gate structure

Most gates are **themed multi-section programs**, not one-feature scripts: one
sample project (e.g. `samples/dotnet/ReflectTypes/`) holds one `*.cs` file per
feature — each keeping its own namespace, its `Main` renamed to an internal
`__GateEntry` — driven in order by a generated `Program.cs`. The gate transpiles
that one program against the tree-shaken real CoreLib once, then asserts.
`gates/_common.sh` provides the wrappers:

- `corelib_diff_gate PROJECT [EXTRA_BCL...]` — diff native output against **real
  .NET**. The default; use it when the transpiler matches .NET.
- `corelib_freeze_gate PROJECT gates/expected/NAME.txt [EXTRA_BCL...]` — diff
  against a frozen snapshot; only when the program *intentionally* diverges.
- `corelib_subset_gate PROJECT [EXPECTED]` — the single-feature form, still used
  by a few standalone gates.

**To cover a new feature, add a section to the most relevant existing bucket** —
a new `*.cs` plus one `__GateEntry()` call in that bucket's `Program.cs` (and
re-freeze the snapshot if it is a freeze gate). Create a new bucket only for a
genuinely new area, and keep buckets deterministic.

**A new driver's first two statements pin `CultureInfo.CurrentCulture` AND
`CultureInfo.CurrentUICulture` to `CultureInfo.InvariantCulture`.** Without it,
printing a double is a host-locale read and the bucket is red on a de-DE machine
and nowhere else; the UI culture separately selects the resource set, so it
moves display names and time-zone names. `<InvariantGlobalization>` in the
csproj is **not** an alternative spelling — only real .NET reads it, so it pins
the oracle and leaves the native subject unpinned. Write the pin;
`gates/verify-culture-invariance.sh` finds buckets that lack one, but exits 77
on Windows.

Four rules bind anyone adding a section; each failure is silent, the suite stays
green.

- **Prove the section ran; a green bucket does not** — the expectation is
  derived from the same run, so a missing driver line still diffs exact. Check
  the new block appears verbatim and contiguous, and that the bucket's previous
  output is an unchanged **prefix** of the new one.
- **Anchor a reference-set grep with `-w`.** A bucket may be transpiled by other
  gates, each with its own hand-written `-r` list; unanchored, the grep names
  the wrong ones.
- **Diff the two `.csproj`s** when moving code between projects: MSBuild
  properties (`InvariantGlobalization`, `AllowUnsafeBlocks`, `Nullable`) are
  invisible from the gate script and the `.cs` alike, and none fails loudly.
- **Write the section's real subject into the gate's header, not its theme** —
  a section lands in a bucket for the CoreLib surface it needs, and the header
  is what stops a later reader pruning by theme.

## A number in a doc is a claim

`gates/build-and-run-doc-claims.sh` checks the mechanical half of what the docs
assert: a name spelled against a file that must exist, a count against the thing
counted. It builds nothing, so it can never skip and takes no result cache.

**Do not write a bare number.** Write it so that gate can count it, or do not
write it: a count nothing counts has a decay rate, and the reader who finds it
wrong is the one who trusted it. The gate cannot check a claim about behaviour;
attribute such a claim to the gate bucket **and section** that asserts it.

## Default references are FILES beside the CLI

The assemblies under `internal/` are conditional default references:
`Compilation.InjectDefaultRefs` loads each from `AppContext.BaseDirectory` when,
and only when, the BCL assembly it serves is already in the load set.

| shim | trigger assembly | what it does |
|------|------------------|--------------|
| `DnZlib` | `System.IO.Compression` | `[NativeImplementation]` adapters replacing the `CompressionNative_*` imports with transpiled C# |
| `DnBrotli` | `System.IO.Compression.Brotli` | the same for the raw `Brotli*` imports |
| `DnHttp` | `System.Net.Http` | the transport an intercepted `SocketsHttpHandler.Send`/`SendAsync` forwards to |

An explicit `-r` wins (dedupe is by assembly simple name);
`--no-default-ref <Name>` declines one — repeatable, unknown name is a hard
error, and a flag rather than an env var for the reason `--trim-reflection` is.

**Nothing references these assemblies at build time, so nothing fails when a
wiring is dropped.** The contract is that the DLLs sit as files next to
`dn2cpp.dll`, wherever `dn2cpp.dll` is:

| # | destination | wired in |
|---|-------------|----------|
| 1 | the full CLI's output dir | `src/Dn2Cpp.Cli/Dn2Cpp.Cli.csproj` (`ReferenceOutputAssembly="false"` + `OutputItemType="Content"`) |
| 2 | the console CLI's output dir — a different base directory | `src/Dn2Cpp.Cli.Console/Dn2Cpp.Cli.Console.csproj` |
| 3 | the NuGet tool payload (`tools/net10.0/any/`) | the same row as #1 in `src/Dn2Cpp.Cli/Dn2Cpp.Cli.csproj`, via `PackAsTool` |
| 4 | `bin/` of the toolchain bundle a forked editor exports with | `dist/package-toolchain.sh` |
| 5 | the self-transpiled native binary's own directory | `gates/selfhost-emit.sh` |

Only #4 fails when you forget it: it **derives** the required sibling set from
the CLI csproj and errors naming the missing assembly. `dist/smoke-test.sh` and
`dist/nuget-smoke-test.sh` each assert that the shipped artifact carries the
siblings — they are the proof, not the wiring.

- **The emitted output is a function of the LOAD SET, not of reachability.** The
  assembly registry walks every loaded module, so one extra loaded module that
  nothing calls changes the output byte for byte; injection must be symmetric
  across both sides of the self-host fixpoint.
- **A gate that declines a default reference must say so in its own text.** The
  codec shims swap one working path for another, so the diff passes either way
  and a stray `--no-default-ref` reads as a leftover flag — deleting it deletes
  the native codec's only coverage.
- **Fold `--no-default-ref` and `--keep-symbols` into the gate cache CONTEXT.**
  Two arms transpiling one project under different switches are two gates.

## The transpiler's own resource use

Monomorphization has no natural fixpoint, so instantiation is capped on nesting
depth (`DN2CPP_MAX_GENERIC_DEPTH`) and on total count
(`DN2CPP_MAX_INSTANTIATIONS`); past either the transpile fails loudly naming the
offending instantiation, the lever, and what drove it.
`gates/build-and-run-transpiler-limits.sh` asserts the bounds. A cap *may* be
environment-driven without breaking the self-host fixpoint: it can only turn a
run into an abort or back, never perturb a transpile that succeeds.

Members, method signatures and field types are decoded **on demand**, and most
are never asked for. Three rules follow, each easy to undo by accident:

- **A signature or field-type read is a decode, and a decode grows
  `Compilation.Classes`.** Never read one while walking `Classes` — snapshot the
  walk, or act after it closes — and do not write a pass that reads members it
  then filters away.
- **Guard a `SigKey` comparison with the name** (`x.Name == t.Name && x.SigKey
  == t.SigKey`); the key test alone decodes every candidate it walks past.
- **`DN2CPP_STRICT_COMPLETION=1` makes an un-asked-for member read throw**, and
  `gates/build-and-run-emit-order-stability.sh` runs the corpus under it.

Emission **streams**: each translation unit is written and released as the next
one opens. Do not reintroduce a structure that holds the whole program's text.

## The transpiler's own C# is transpiler input

dn2cpp transpiles its own source: `gates/selfhost-emit.sh` drives the full CLI
to a byte-identical fixpoint, and `gates/build-and-run-selfhost-prim-subset.sh`
is the in-suite gate. So every line of transpiler C# has a second reader, and it
is the transpiler:

- Stick to BCL surface the transpiler already supports. Shapes that make Roslyn
  reach for a newer lowering (an `[InlineArray]`-backed span, a lazy enumeration
  cascade) break the self-host; pass arguments that bind the plain overload.
- Keep everything that reaches emitted text deterministic across hosts: no
  environment-driven output — the flag-not-env rule — and no seeded hashing.
  `System.HashCode`'s seed is a `.cctor` fill and reads as 0 under dn2cpp's
  eager static init, so fold hashes with plain arithmetic instead.

## Equality and ordering over an arbitrary element type

Every site that compares two values of a type it does not know statically — the
`Dictionary`/`HashSet` key path, the array and span scans, `Array.Sort`,
`BinarySearch`, the synthesized `ValueType` field walk — asks one pair of
builders in `src/Dn2Cpp.Transpiler/MethodCompiler.Call.cs`: `CanEqualityEquals`
/ `TryEqualityEqualsLValue` for equality, `TryCompareLValue` for ordering.

**Do not write a private rule for what two values being equal means.** Sites
that grew their own covered different handfuls of element types, and every
disagreement between them was silent. The question and the answer are separate
on purpose: the builders record call edges, so a guard asks `CanEqualityEquals`
and never the builder. The remaining traps are written out at those call sites.

## An intercept has askers on both sides

Lowering a BCL method to a runtime helper takes two decisions: an **emit route**
picks the helper, and a **reachability cut** deletes the real body so its
subtree never enters the tree. The invariant is one-directional:

> **`cut ⟹ route`.** If reachability cut the edge to a body, no emitted body may
> name that body's C++ symbol.

Break it and the transpile reports success while the C++ link fails on a mangled
name with no cause attached. `CppEmitter.AssertCalledBodiesEmitted` checks it
after the emit fixpoint, failing with callee, caller and reach chain. The other
direction is mere bloat.

Reachability and emission each ask about a call from several positions, and an
intercept reached through one is invisible to the others. So **write one
descriptor row and reference it from every asker that can reach the member**
(`src/Dn2Cpp.Transpiler/CoreIntrinsics.Intercepts.cs`): the four-tuple **(pure
predicate, cut kind, emit arm, scan effect)**. Keep the predicate pure and
allocation-free — the askers sit in the innermost per-token loops. A few tests
genuinely cannot be rows; those stay at their call site with a comment naming
their counterpart. Doctrine for the intrinsic-versus-transpiled decision is in
`docs/ARCHITECTURE.md` §4-B.

## A stripped type must throw, not answer empty

`--trim-reflection` (off by default; the Godot Web export turns it on) drops the
field, method and property tables of every class the program cannot plausibly
reflect over. A stripped type and a genuinely member-less one are
indistinguishable from the tables alone, so every member-metadata entry point
calls `dn2cpp_require_metadata`, which throws a catchable
`PlatformNotSupportedException` naming the type and the remedy — the alternative
is not "no answer", it is `GetMethods()` cheerfully returning an empty array.
The analysis is unsound and cannot be made sound, which is why that throw and
the `--reflection-root` escape hatch exist. It is a CLI flag and never an env
var: it changes the C++ a successful transpile emits.

## Naming / style

- C# namespace `Dn2Cpp` (+ `Dn2Cpp.Godot`); C++ types `Dn2Cpp*`, functions
  `dn2cpp_*`. The `Godot.GD` / `Godot.Node` C# shims keep the `Godot` namespace.
- 4-space indent, file-scoped namespaces, `is null` / `is not null`, `nameof`,
  final `return` on its own line.
- BCL coverage: prefer transpiling the real BCL IL; intrinsics are reserved for
  untranspilable bodies, clear-win hot paths, and runtime-called methods —
  criteria and the sibling-overload rule in `docs/ARCHITECTURE.md` §4-B.
- Exceptions carry a contract. An unsupported IL/BCL shape throws
  `NotSupportedException`; the driver's catch is the only one, rendering it as a
  single `error:` line, exit 2. An internal invariant violation throws
  `InvalidOperationException` and crashes raw with its stack — that is a
  transpiler bug, not bad input, and a polite one-liner would bury it.
  `StrictCompletionException` / `InstantiationBoundException` are must-escape
  (`Compilation.IsMustEscape`): every broad catch filters
  `when (!Compilation.IsMustEscape(e))`.
- No `TODO`/`FIXME`/`HACK` markers — the tree is at zero and stays there. Debt
  gets a row in `docs/STATUS.md`; a constraint worth keeping is written out at
  its site as an invariant, not deferred with a marker.
- Comments are terse, in every language here. See *Write like an experienced
  programmer* above.
- Prefer small, frequent commits. Commit messages: 50-char summary, blank line,
  72-col body. Changes made by a coding agent end with a `Co-Authored-By:`
  trailer naming the agent — no email address.

## The Godot lane

**Engine-class bindings are generated, not hand-listed.** `BindingGenerator`
emits the C# shim surface (`src/GodotSharpShim/GodotShims.g.cs`) from
`extension_api.json`, and `GodotCallIntrinsics` builds the transpiler's call map
from the same JSON, so any method whose signature fits the supported marshalling
shapes already works. If a method is filtered out, widen the shape: extend
`IsSupportedArgType` / `IsSupportedRetType` / `MapTypeToGd` in
`src/Dn2Cpp.Godot/GodotApi.cs`, add a bridge variant under `runtime/godot/` only
if the generic method-bind ptrcall does not cover it, then regenerate with
`dotnet run --project src/Dn2Cpp.Cli -- --generate-bindings extension_api.json`.
Builtin value-type instance methods (Vector3, Color, …) are the exception:
curated one tuple at a time in `GodotApi.BridgedBuiltinMethods`.

**A singleton is two C# types over one engine class**, matching real GodotSharp:
a static facade under the engine name (what game code writes) and
`<Name>Instance` for the ClassDB class. C# cannot host both in one namespace, so
the split is forced — and matching it is what lets one game source build on this
lane and on `--dotnet-module`. `GodotApi.InstanceCSharpName` is the naming seam.

### `System.IO` takes OS paths, never `res://` / `user://`

dn2cpp does not translate virtual paths inside `System.IO`, and will not.
Convert first with `ProjectSettings.GlobalizePath(...)`, or take the writable
root from `OS.GetUserDataDir()`; read `res://` through the engine (`FileAccess`
/ `ResourceLoader`), which reads the PCK. The reason it is a non-goal rather
than a to-do: in an exported game `res://` lives inside the PCK archive where
`open(2)` cannot reach it, so a transparent translation would work in the editor
and break in the shipped game.

### Web export carve-outs

A C# Godot game exports to the Web through the same mono-module drop-in every
other platform uses, compiled by Emscripten into a wasm **side module** staged as
`<Assembly>.so` (no `lib` prefix — that name is the contract). Mechanism and the
engine-side changes are in `docs/EDITOR-EXPORT-DESIGN.md`. Carve-outs:

- **No threads** (`threads=no`), so `Task.Run`, `Thread` and `Timer` throw.
- **No HTTP transport** — a browser has no TCP socket layer, so
  `DN2CPP_USE_CURL` is forced off; `HttpClient` links and fails every call with
  an `HttpRequestException` naming the platform.
- **`FileStream` does not link**; the intercepted `File.*` subset works on MEMFS.
- **The GC is real** — the same vendored Boehm, single threaded — but with no
  page-protection VDB, so collection is stop-the-world and finalizers drain
  manually.
- **Publish with the host RID, never `browser-wasm`**, which would demand a
  Mono-flavoured CoreLib — i.e. change the very IL we transpile.
- **Build the project (or Play once) before the first export of a session**, or
  editor-wired C# `[Signal]` scene connections are dropped with no error — and a
  stale `.godot/exported/` scene cache keeps the drop across a later rebuild. A
  general Godot trap; remedies in `docs/EDITOR-EXPORT-DESIGN.md` §9.

Two Emscripten defaults are actively hostile. **`emcc -shared` implies
`SIDE_MODULE=1` → `LINKABLE=1`**, which disables DCE, ignores
`-sEXPORTED_FUNCTIONS` and silently turns undefined symbols into imports —
always `SIDE_MODULE=2`. And **`-fvisibility=hidden` matters more here than
natively**: wasm-ld exports every non-hidden global, and every export is a DCE
root. Related: emscripten's `dlopen` does not fail on an unresolved symbol — the
first call dies with `TypeError: resolved is not a function`, naming nothing,
which is why the drop-in's import set is asserted statically.

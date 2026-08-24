# AGENTS.md — dn2cpp

Rules for contributors and coding agents. The repository is the source of truth.
Read `README.md` for the product and setup, `docs/ARCHITECTURE.md` for extension
points, `CONTRIBUTING.md` for the contribution workflow, and `docs/STATUS.md`
for open work.

## Module boundaries

- `src/Dn2Cpp.Transpiler/` and `runtime/core/` never depend on Godot.
- Target behavior enters through `IEmitBackend` and `ICallIntrinsics`.
- Godot code belongs under `src/Dn2Cpp.Godot/`, `src/Dn2Cpp.DotnetModule/`,
  `runtime/godot/`, or `runtime/dotnetmodule/` as appropriate.
- `src/Dn2Cpp.Cli/` is the composition root. See `README.md` for the full map.

## Backlog workflow

- `docs/STATUS.md` contains open work only; delete a row when it lands.
- A ticket id lives only in `docs/STATUS.md`. Never put one in code, comments,
  gates, samples, other docs, commits, or issues; write the invariant instead.
- GitHub issues are intake, not a second backlog. A STATUS row may cite an issue,
  but nothing cites a STATUS row.

## Write like an experienced programmer

Comments and docs state an invariant, a non-obvious reason, or a prevented
failure. Omit code restatements, history, rejected alternatives, dated
measurements, and unverified counts. Keep comments short.

## Build and verification

```bash
dotnet build src/Dn2Cpp.Cli -c Release
dotnet run --project src/Dn2Cpp.Cli -- <assembly.dll> [-r <ref.dll>] [-o <dir>] [--gdextension]
./gates/run-all-gates.sh
SKIP_GODOT=1 ./gates/run-all-gates.sh
CONFIG=Debug ./gates/run-all-gates.sh
```

- Run the relevant build and gates before committing. CMake with Ninja is the
  only native build path; use the gate wrappers.
- A human must run `./gates/pre-merge.sh` before merge. Coding agents must not.
- `gate_skip` is the only prerequisite opt-out; a skip is not a pass.
  `gate_expected_partial` is only for a permanent structural limit and must name
  the gate that covers the omitted surface. See `gates/_common.sh`.
- Run Debug gates when changing shared generics, canonicalization, or emission.
- Never trust hosted smoke CI as the merge verdict; see `CONTRIBUTING.md`.

## Adding regression coverage

- Add a section to the closest themed bucket; create a bucket only for a new area.
- New drivers first pin `CurrentCulture` and `CurrentUICulture` to invariant.
- Prove the new block ran and the old output is an unchanged prefix.
- Use `corelib_diff_gate` for .NET parity and `corelib_freeze_gate` only for an
  intentional divergence. Keep buckets deterministic.
- Anchor reference-set greps with `-w`; include behavior-changing flags in the
  gate-cache context; diff both `.csproj` files when moving sample code.
- Gate headers name the tested subject, not merely the bucket theme.
- Never write a bare numeric claim in docs. Make
  `gates/build-and-run-doc-claims.sh` verify it or remove it.

## Default references are files beside the CLI

`Compilation.InjectDefaultRefs` conditionally loads these assemblies from
`AppContext.BaseDirectory`; explicit `-r` wins and `--no-default-ref` declines.

| shim | trigger assembly | role |
|------|------------------|------|
| `DnZlib` | `System.IO.Compression` | replaces native zlib imports |
| `DnBrotli` | `System.IO.Compression.Brotli` | replaces native Brotli imports |
| `DnHttp` | `System.Net.Http` | backs intercepted HTTP sends |

Every shim must be shipped beside `dn2cpp.dll` in each destination:

| # | destination | wired in |
|---|-------------|----------|
| 1 | full CLI output | `src/Dn2Cpp.Cli/Dn2Cpp.Cli.csproj` |
| 2 | console CLI output | `src/Dn2Cpp.Cli.Console/Dn2Cpp.Cli.Console.csproj` |
| 3 | NuGet tool payload | `src/Dn2Cpp.Cli/Dn2Cpp.Cli.csproj` |
| 4 | editor toolchain `bin/` | `dist/package-toolchain.sh` |
| 5 | self-host binary directory | `gates/selfhost-emit.sh` |

`dist/smoke-test.sh` and `dist/nuget-smoke-test.sh` prove shipped siblings.
The emitted output depends on the load set, so self-host injection must be
symmetric. Put `--no-default-ref` and `--keep-symbols` in gate-cache context.

## Transpiler invariants

- Members, signatures, and field types decode on demand and may grow
  `Compilation.Classes`. Snapshot walks before decoding; name-gate `SigKey`.
  `DN2CPP_STRICT_COMPLETION=1` detects accidental reads.
- Emission streams translation units. Never retain the whole program text.
- Transpiler C# is self-host input: use supported BCL/lowerings and keep emitted
  text host-deterministic. Never use seeded hashing for emitted data.
- Arbitrary-type equality and ordering use `CanEqualityEquals`,
  `TryEqualityEqualsLValue`, and `TryCompareLValue`; do not create local rules.
- Intercepts obey `cut ⟹ route`. Define descriptor rows in
  `CoreIntrinsics.Intercepts.cs` and use them from every reachability/emission
  asker. Predicates stay pure and allocation-free.
- A type stripped by `--trim-reflection` throws through
  `dn2cpp_require_metadata`; it never reports an empty member set.

## Naming / style

- C# namespace: `Dn2Cpp` or `Dn2Cpp.Godot`; C++: `Dn2Cpp*` and `dn2cpp_*`.
- Use 4 spaces, file-scoped namespaces, `is null` / `is not null`, `nameof`, and
  a final `return` on its own line.
- Prefer real BCL IL. Intrinsics are for untranspilable bodies, clear hot paths,
  or runtime-called methods; cover sibling overloads. See ARCHITECTURE §4-B.
- Unsupported input throws `NotSupportedException`; broken invariants throw
  `InvalidOperationException`. Broad catches preserve `Compilation.IsMustEscape`.
- No `TODO`, `FIXME`, or `HACK`. Put open work in `docs/STATUS.md`.
- Prefer small commits: 50-character summary, blank line, 72-column body.
  Agent commits end with `Co-Authored-By:` naming the agent, without email.

## The Godot lane

- Engine bindings are generated from `extension_api.json`. Widen supported
  shapes in `GodotApi.cs`, add a runtime bridge only for a new marshalling shape,
  then regenerate with `--generate-bindings`. Builtin value-type methods remain
  curated in `GodotApi.BridgedBuiltinMethods`.
- A singleton has a static facade and `<Name>Instance`; use
  `GodotApi.InstanceCSharpName`.

### `System.IO` takes OS paths, never `res://` / `user://`

Use `ProjectSettings.GlobalizePath`, `OS.GetUserDataDir`, or engine APIs. In an
export, `res://` may live in the PCK and is not an OS path.

### Web exports

Read `docs/EDITOR-EXPORT-DESIGN.md` and `docs/PORTING.md`. Web is threadless,
uses ephemeral MEMFS, and has no HTTP transport. Add missing `SystemNative_*`
symbols under `runtime/core/platform/wasm/` and prove them with the wasm-console
gate. Publish with the host RID, use `SIDE_MODULE=2`, and keep non-exported
symbols hidden.

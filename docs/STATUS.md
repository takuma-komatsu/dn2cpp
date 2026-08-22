# STATUS — open backlog

Open tickets only. Closed work is not tracked here — delete the row when it is
done.

## Conventions

**A ticket id is a temporary handle.** It lives in this file and nowhere else:
not in source, not in a comment, not in a gate script, not in another document,
not in a commit message. A ticket that lands has its row deleted and its id goes
with it, so nothing outside this file can be left pointing at a number that
stopped meaning anything. A constraint worth keeping past the ticket is written
out as an invariant at the site it constrains, or as a boundary in `README.md` —
never as a reference back to a row.

- **Ids are ascending integers and are never reused.** A deleted row's number
  stays spent; the next ticket takes the next integer above every number this
  file has ever issued.
- **A permanent boundary is not a ticket.** The things dn2cpp will not do — the
  AOT and platform limits it shares with the IL2CPP family — are the *Permanent
  non-goals* section of `README.md`. Read it before filing, so a settled boundary
  does not come back as a row.
- **Write the row so somebody else can act on it**: what to build, where the
  design lives, and which of its numbers must be re-measured rather than trusted.

## Open

| # | area | ticket |
|---|------|--------|
| 1 | godot-editor | **Notarize the macOS editor.** The `.app` is packaged and ad-hoc signed (`dist/package-editor-macos.sh`), so what remains is the notarization story: a Developer ID identity, `--options runtime`, and a re-audit of the fork's `misc/dist/macos/editor.entitlements` against what the export backend actually does at export time — spawn the host's `clang++` (external) and the bundle's own `cmake`, `ninja`, `node` and Emscripten `clang` (internal, hence part of the notarized payload). Audit by running a hardened build and measuring which entitlement each spawn needs, not by reading the existing list. The signing set needs no code change: the packager enumerates every Mach-O under the `.app` via `file -h -F '\|'` and signs each before sealing, so the bundled tools are already covered and only the printed count moves. One ordering trap — `lipo -thin` invalidates any signature it rewrites, which is harmless only because staging runs before signing. |
| 2 | godot-editor | **Re-pin the fork to the next upstream stable.** Move the fork's base off the pinned release and onto the next Godot stable. The procedure — rebase order, the ABI fingerprint re-freeze, and what to re-run — is `docs/EDITOR-EXPORT-DESIGN.md` §7. |
| 7 | godot-editor | **Sign the Windows editor.** `dist/package-editor-windows.sh` ships it unsigned and says so in its metadata, so SmartScreen reports an unknown publisher and the download's whole identity is `SHA256SUMS.txt`. What is needed: a code-signing certificate, `signtool` on the packaging host, and a decision about the signing set — the two editor binaries alone (`Godot-dn2cpp.exe` and its `.console.exe`), or every PE the bundle carries with them: `Dn2Cpp/bin/dn2cpp.exe`, `buildtools/` cmake, cmcldeps and ninja, and the Emscripten SDK's own executables. The decision is the same for all of them, so make it once. The macOS notarization row above is this one's counterpart, and neither answers for the other's platform. |
| 18 | godot-editor | **The Windows preflight still passes a bare `cl.exe` once the MSVC import has failed.** The fork's export backend now locates an installation through vswhere, runs `vcvarsall` itself and hands the resulting `INCLUDE`/`LIB` and a pinned `CMAKE_CXX_COMPILER` down to the build, so the ordinary machine with Visual Studio installed and no Developer Command Prompt exports fine and is no longer the case to fix. What is left is the fallback arm: `HostCxxCompiler()` answers `msvc?.ClExe ?? OS.PathWhich("cl") ?? OS.PathWhich("clang++")`, and the middle term is reached exactly when the import found nothing — a hand-assembled PATH carrying `cl.exe` with no vswhere or `vcvarsall.bat` in reach. There the preflight clears and cmake's compiler check dies instead, which is the failure the preflight exists to turn into an actionable refusal. `gates/_common.sh` spells the condition and the reason with it — *"cl.exe on PATH does not imply vcvars ran; without LIB link.exe is lost"*, testing `cl.exe` **and** `${LIB}` **and** `${INCLUDE}` — and it belongs on that arm alone, so an import that succeeded is not re-litigated. **Verifiable on a Windows host only.** |
| 19 | runtime | **A pending `IValueTaskSource`-backed `ValueTask` blocks and succeeds under `.GetAwaiter().GetResult()` instead of throwing.** Real .NET's `ManualResetValueTaskSourceCore<T>.GetResult` throws `InvalidOperationException` when read before its source has signalled; `dn2cpp_vts_task` (`runtime/core/dn2cpp_tasks.cpp`) always wraps the source in a real `Dn2CppTask` and registers completion through `OnCompleted`, so a synchronous read on the still-pending `ValueTask` just blocks in `dn2cpp_task_block` until some other thread settles it, then returns normally — a real task standing in for a promise the CLR would have refused to read early. Found incidentally while adding the pending-read sections to `gates/build-and-run-r3.sh`'s async bucket, not introduced by that work; those sections avoid the divergence by always `await`-ing rather than blocking. Needs a deliberate decision: make the bridge throw on an early read to match the CLR, or write the divergence down and keep blocking — either way, decide once rather than leaving it accidental. |
| 46 | runtime | **Two residual delegate divergences on the Task surface.** (a) A cold task built over a null delegate — `new Task((Action)null!)` and siblings, the `dn2cpp_task_cold_*` path in `runtime/core/dn2cpp_tasks.cpp` — completes silently where real .NET throws `ArgumentNullException("action")` from the constructor; the run-and-submit entries (`dn2cpp_task_run_*`) validate via `dn2cpp_task_require_delegate`, so only the cold constructors lack the check. (b) A multicast delegate with a STRUCT return passed to `Task.Run`/`StartNew` runs its last handler alone: the struct-result thunk is synthesized by the transpiler (`TaskStructResultThunk`, `src/Dn2Cpp.Transpiler/MethodCompiler.GenericIntrinsic.cs`) and does not walk the `prev` chain the way the runtime's primitive/ref result thunks now do — mirror their prev-first walk in the synthesized body. The `TaskDelegateContractSubset` section of the AsyncCombinators bucket is where both get their oracle-diffed coverage when fixed. |
| 47 | runtime | **`Dn2CppDecimal`'s field order is not `System.Decimal`'s, so a memcpy of a `decimal` puts different bytes on the wire.** .NET lays `decimal` out as `{ int _flags; uint _hi32; ulong _lo64 }` (sign and scale packed into `_flags`); `Dn2CppDecimal` (`runtime/core/dn2cpp_core.h`) is `{ uint64 lo; uint32 hi; uint8 scale; uint8 sign }`. Every arithmetic, formatting and parsing path agrees with .NET, so nothing that reads a `decimal` *as a number* can see this — it surfaces only where the raw 16 bytes are the observable, which is any serializer that treats an unmanaged value as a blob (`MemoryPackSerializer.Serialize` of a type with a `decimal` member is the case that found it, and `gates/build-and-run-memorypack.sh` says in its header why its driver serializes none). Fixing it means re-laying the struct to .NET's field order and re-auditing every reader of the four fields across the decimal intrinsics and the runtime; the sign/scale split into a packed `_flags` word is the part that is not mechanical. Re-measure nothing here — the layouts are in the two sources named above. |

## Regression gate

The full gate suite (`gates/build-and-run-*.sh`, each a themed multi-section
program) is the regression gate — run it through the parallel runner before
committing:

```bash
./gates/run-all-gates.sh               # all gates (pre-build once → parallel; Godot in chains)
SKIP_GODOT=1 ./gates/run-all-gates.sh  # skip Godot for a faster smoke check
DN2CPP_REQUIRE_ALL=1 ./gates/run-all-gates.sh   # every gate must RUN
```

**A skip is not a pass.** A gate whose optional prerequisite is absent (an
Android NDK, an Xcode simulator, Emscripten, a scons-built Godot editor) opts out
via `gate_skip` and is counted and reported **separately** — the summary says how
many gates skipped and why, and never claims "all N passed" when some of the N
never ran. `DN2CPP_REQUIRE_ALL=1` turns any skip into a failure and refuses
cached partials; that is the mode a merge runs. This is not hypothetical: a whole
lane once shipped red behind a gate that skipped on every machine lacking its
artifacts — the default — while the suite reported all-green.

A failing gate is listed in `$LOGDIR/_failures.txt` with its log, a skipped one
in `$LOGDIR/_skips.txt` with its reason. For a quick manual smoke check of the
three lanes, run the representative trio directly:

```bash
./gates/build-and-run-sample.sh          # console: C# → IL → C++ → native
./gates/build-and-run-multiassembly.sh   # multi-assembly (-r)
./gates/build-and-run-godot-sample.sh    # Godot GDExtension, real engine
```

The runner discovers gates by glob, so a new `build-and-run-*.sh` is picked up
automatically — there is no list to maintain here. `AGENTS.md` is the
authoritative contract for the suite, the skip protocol, the result caches and
the merge gate.

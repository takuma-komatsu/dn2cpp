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
| 8 | godot-editor | **Cut every editor lane from one dn2cpp commit.** The macOS and Windows editors are packaged on separate hosts at separate times, so their `dn2cpp_commit` values differ and the release notes carry a row per editor for everything below the engine. The end state is `dist/release-github.sh` demanding that every editor lane agree on `dn2cpp_commit`, the way it already demands one `engine_provenance` and one `corelib_framework` — which needs a way to pin each host's packaging run to a named dn2cpp commit first. |
| 16 | dist | **Cut and accept a Linux x64 toolchain bundle on a real machine.** The Linux half of the packaging lane rests on the pin's `linux-x64` archive row and on code paths only a macOS host has executed. Acceptance is one run and its exit code is not enough: `gates/setup-buildtools.sh` unpacks the pinned cmake and ninja first — `dist/smoke-test.sh` hard-fails on a bundle carrying none, and it drives the bundle's own cmake — and `gates/setup-emsdk.sh` unpacks the SDK together with the pinned node inside it, which the web axis links through; then `dist/package-toolchain.sh` produces the tarball, then `dist/smoke-test.sh` must be green *including* its web axis; that section is the whole acceptance gate on Linux, and it prints the reason when it declines instead of quietly passing. |
| 18 | godot-editor | **The Windows preflight still passes a bare `cl.exe` once the MSVC import has failed.** The fork's export backend now locates an installation through vswhere, runs `vcvarsall` itself and hands the resulting `INCLUDE`/`LIB` and a pinned `CMAKE_CXX_COMPILER` down to the build, so the ordinary machine with Visual Studio installed and no Developer Command Prompt exports fine and is no longer the case to fix. What is left is the fallback arm: `HostCxxCompiler()` answers `msvc?.ClExe ?? OS.PathWhich("cl") ?? OS.PathWhich("clang++")`, and the middle term is reached exactly when the import found nothing — a hand-assembled PATH carrying `cl.exe` with no vswhere or `vcvarsall.bat` in reach. There the preflight clears and cmake's compiler check dies instead, which is the failure the preflight exists to turn into an actionable refusal. `gates/_common.sh` spells the condition and the reason with it — *"cl.exe on PATH does not imply vcvars ran; without LIB link.exe is lost"*, testing `cl.exe` **and** `${LIB}` **and** `${INCLUDE}` — and it belongs on that arm alone, so an import that succeeded is not re-litigated. **Verifiable on a Windows host only.** |
| 22 | runtime | **`Task.WhenAll` propagates one fault, not the set.** Real .NET aggregates every faulted input — `WhenAll(faulted1, faulted2).Exception.InnerExceptions` holds two — while `dn2cpp_when_all_finish` (`runtime/core/dn2cpp_tasks.cpp`) copies the first faulted input's exception and stops, so the awaiter is handed one and the rest are lost. Measured on net10.0 against the native subject. The cancellation arm beside it ranks a fault anywhere over a cancellation anywhere and is correct as it stands; what has to change is the payload, from a single slot to a real aggregate. The settled-input contract is asserted next door in `gates/build-and-run-async-combinators.sh`, which is where the section goes — it pins no count today, so nothing reds until one is written. |
| 25 | transpiler | **A guarded `ModeledSize` is honoured at the asserts and ignored where it decides a layout.** `PointerWidth.Model` falls back to a bare 64-bit constant with `Guarded` set when the narrow reading is missing — the model refused the type at 32 bits and there is no second number to select between. Two sites take `.Text` regardless — the opaque value shell's `__opaque_pad` and the explicit layout's `__explicit_pad`, both of which ARE the struct's size — and the `marshalSize` stamp takes `.Int32Expr`, which drops the flag the same way; the explicit-size assert beside it then guards itself off, so a 64-bit size on wasm32 would be silent in exactly the case the fallback exists for. Reachability differs by site and has to be established per site rather than assumed: `ExplicitLayoutExtent`'s refusals are weakly monotone in the pointer width, which argues the explicit pad cannot reach it, while the marshalled walk has its own refusals and its own narrow reading to lose. The fix is not a wider guard but a decision at each site about what a size the model could not write in `sizeof(void*)` should do — a refusal is available and a silent 64-bit number is not. |

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

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
| 1 | godot-editor | **Notarize the macOS editor.** The `.app` is packaged and ad-hoc signed (`dist/package-editor-macos.sh`), so what remains is the notarization story: a Developer ID identity, `--options runtime`, and a re-audit of the fork's `misc/dist/macos/editor.entitlements` against what the export backend actually does at export time — spawn the host's `clang++` (external) and the bundle's own `cmake`, `ninja` and Emscripten `clang` (internal, hence part of the notarized payload). Audit by running a hardened build and measuring which entitlement each spawn needs, not by reading the existing list. The signing set needs no code change: the packager enumerates every Mach-O under the `.app` via `file -h -F '\|'` and signs each before sealing, so the bundled tools are already covered and only the printed count moves. One ordering trap — `lipo -thin` invalidates any signature it rewrites, which is harmless only because staging runs before signing. |
| 2 | godot-editor | **Re-pin the fork to the next upstream stable.** Move the fork's base off the pinned release and onto the next Godot stable. The procedure — rebase order, the ABI fingerprint re-freeze, and what to re-run — is `docs/EDITOR-EXPORT-DESIGN.md` §7. |
| 4 | ci | **First real run of the Linux CI workflow.** `.github/workflows/linux-smoke.yml` has never executed on a hosted runner. Its first red has exactly two possible meanings — a defect in the workflow, or a genuine Linux regression — and a human has to tell them apart; treat red as a Linux lane to close, not as CI flake. Measure one thing while you are there: whether the suite fits inside the configured timeout on a cold hosted runner, with no ccache and no pre-built CoreLib closure. |
| 5 | cli | **Optional `--target` / build manifest.** Generated C++ absorbs platform divergence through PAL `#ifdef`s, so moving output naming and build flags into the CLI is optional rather than blocking. It is, however, the only route to a truthful 32-bit `Marshal.SizeOf`: the marshalled-layout model's pointer width is a fixed 64-bit premise, stated at the top of `src/Dn2Cpp.Transpiler/Compilation.MarshalLayout.cs`, so a wasm32 build reads 8 where its ABI is 4. |
| 7 | godot-editor | **Sign the Windows editor.** `dist/package-editor-windows.sh` ships it unsigned and says so in its metadata, so SmartScreen reports an unknown publisher and the download's whole identity is `SHA256SUMS.txt`. What is needed: a code-signing certificate, `signtool` on the packaging host, and a decision about the signing set — the two editor binaries alone (`Godot-dn2cpp.exe` and its `.console.exe`), or every PE the bundle carries with them: `Dn2Cpp/bin/dn2cpp.exe`, `buildtools/` cmake, cmcldeps and ninja, and the Emscripten SDK's own executables. The decision is the same for all of them, so make it once. The macOS notarization row above is this one's counterpart, and neither answers for the other's platform. |
| 8 | godot-editor | **Cut every editor lane from one dn2cpp commit.** The macOS and Windows editors are packaged on separate hosts at separate times, so their `dn2cpp_commit` values differ and the release notes carry a row per editor for everything below the engine. The end state is `dist/release-github.sh` demanding that every editor lane agree on `dn2cpp_commit`, the way it already demands one `engine_provenance` and one `corelib_framework` — which needs a way to pin each host's packaging run to a named dn2cpp commit first. |
| 14 | runtime | **`Task.WhenAll` over already-settled inputs does not complete synchronously.** dn2cpp posts a settled task's continuation to the scheduler (`dn2cpp_task_on_completed`, `runtime/core/dn2cpp_tasks.cpp`) instead of running it inline, so `WhenAll(completed, completed).IsCompleted` reads false right after the call where real .NET reads true — measured on net10.0. `dn2cpp_task_wait_any` short-circuits on a settled input to honour the same kind of contract and is the shape a fix takes. The `BlockingWaitArgsSubset` section of `gates/build-and-run-async-combinators.sh` waits before it reads to work around this, so closing the row means deleting that workaround and letting the section assert the synchronous answer instead. |
| 16 | dist | **Cut and accept a Linux x64 toolchain bundle on a real machine.** The Linux half of the packaging lane rests on the pin's `linux-x64` archive row and on code paths only a macOS host has executed. Acceptance is one run and its exit code is not enough: `gates/setup-buildtools.sh` unpacks the pinned cmake and ninja first — `dist/smoke-test.sh` hard-fails on a bundle carrying none, and it drives the bundle's own cmake — then `dist/package-toolchain.sh` produces the tarball, then `dist/smoke-test.sh` must be green *including* its web axis; that section is the whole acceptance gate on Linux, and it prints the reason when it declines instead of quietly passing. |
| 17 | dist | **The bundled SDK ships binaries whose licence text it does not carry.** The trim in `dist/package-toolchain.sh` copies a licence file into `emsdk/LICENSES/` when the directory holding it still holds a kept file, which covers Emscripten itself and the npm closure. It covers neither the LLVM and Binaryen executables — the upstream archive ships those with no licence file anywhere — nor the libc++, libunwind and compiler-rt archives in the baked sysroot, whose `LICENSE.TXT` sits in the `emscripten/system/` source tree the trim drops. Decide per package between keeping the dropped text and vendoring one, then widen what `dist/release-notes-template.md` says: it deliberately claims only what the upstream archive carries. |
| 18 | godot-editor | **The Windows host-compiler preflight passes a machine that cannot compile.** `HostCxxCompiler()` answers non-null the moment `cl.exe` is on PATH, but MSVC's driver needs `INCLUDE` for its headers and `LIB` for its import libraries — so a box with `cl.exe` on PATH and no vcvars environment clears the preflight and dies inside cmake's compiler check instead, which is precisely the failure the preflight exists to turn into an actionable refusal. `gates/_common.sh` already spells the right condition and the reason with it — *"cl.exe on PATH does not imply vcvars ran; without LIB link.exe is lost"*, testing `command -v cl.exe` **and** `${LIB}` **and** `${INCLUDE}` — so widening the fork's probe to the same three is the whole change, the existing vcvars remedy text staying as it is. **Verifiable on a Windows host only.** |
| 19 | gates | **The plain Web gate reds on a python it should skip on.** `gates/build-and-run-godot-editor-export-web.sh` takes its interpreter from `resolve_python` (`gates/_common.sh`), which accepts any candidate whose `-c 'print(1)'` exits 0 — and Apple's `/usr/bin/python3`, an Xcode stub stuck at 3.9.6, passes. But emcc is a launcher over python, and the fork's `VerifyEmscriptenPython` refuses anything below its `RequiredPythonMajor`/`RequiredPythonMinor` floor, so on a host carrying nothing else the export refuses and the gate goes red where it should skip with a named reason: the interpreter is a host prerequisite the repository cannot carry, and the bundled SDK ships one on Windows alone. `8b836a6c` fixed the hermetic sibling and is the shape — the same two names in the same order off the gate's own PATH, the interpreter run rather than resolved, the floor parsed out of the fork rather than restated — but copying it here is the wrong move, and separating what a copy would conflate is the work. Two reasons: the hermetic gate does `env -u EMSDK_PYTHON` and cannot reach that arm at all, whereas here `dn2cpp_emsdk_resolve` sets `EMSDK_PYTHON` from the SDK's staged `python.exe` and the arm is live; and `resolve_python`'s answer is the *gate's own* interpreter, for reading wasm exports and serving over `http.server`, which is a different question from which interpreter `emcc` will start. `gates/build-and-run-cri-web.sh` is in scope for the same reason and wants the same treatment — it drives a fork-editor Web export through that same preflight and resolves python the same way. `gates/build-and-run-http-get.sh` calls `resolve_python` too, but only to run a local server; it exports nothing and stays as it is. |

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

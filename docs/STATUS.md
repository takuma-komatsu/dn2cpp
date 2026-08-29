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
| 7 | godot-editor | **Sign the Windows editor.** `dist/package-editor-windows.sh` ships it unsigned and says so in its metadata, so SmartScreen reports an unknown publisher and the download's whole identity is `SHA256SUMS.txt`. What is needed: a code-signing certificate, `signtool` on the packaging host, and a decision about the signing set — the two editor binaries alone (`Godot-dn2cpp.exe` and its `.console.exe`), or every PE the bundle carries with them: `Dn2Cpp/bin/dn2cpp.exe`, `buildtools/` cmake, cmcldeps and ninja, and the Emscripten SDK's own executables. The decision is the same for all of them, so make it once. The macOS notarization row above is this one's counterpart, and neither answers for the other's platform. |
| 18 | godot-editor | **The Windows preflight still passes a bare `cl.exe` once the MSVC import has failed.** The fork's export backend now locates an installation through vswhere, runs `vcvarsall` itself and hands the resulting `INCLUDE`/`LIB` and a pinned `CMAKE_CXX_COMPILER` down to the build, so the ordinary machine with Visual Studio installed and no Developer Command Prompt exports fine and is no longer the case to fix. What is left is the fallback arm: `HostCxxCompiler()` answers `msvc?.ClExe ?? OS.PathWhich("cl") ?? OS.PathWhich("clang++")`, and the middle term is reached exactly when the import found nothing — a hand-assembled PATH carrying `cl.exe` with no vswhere or `vcvarsall.bat` in reach. There the preflight clears and cmake's compiler check dies instead, which is the failure the preflight exists to turn into an actionable refusal. `gates/_common.sh` spells the condition and the reason with it — *"cl.exe on PATH does not imply vcvars ran; without LIB link.exe is lost"*, testing `cl.exe` **and** `${LIB}` **and** `${INCLUDE}` — and it belongs on that arm alone, so an import that succeeded is not re-litigated. **Verifiable on a Windows host only.** |
| 50 | intrinsics | **Lower the platform intrinsic families behind the ISA contract.** The contract itself is in place: the generated family table (`src/Dn2Cpp.Transpiler/CoreIntrinsics.PlatformIsa.g.cs`), the runtime's token header and CPU detection with the `DN2CPP_CPU_FEATURES` mask, and the x86 / Arm / Wasm / surface gates (`gates/build-and-run-platform-isa-surface.sh` and siblings). The scalar families have their maps (X86Base, Lzcnt, Popcnt, Bmi1, Bmi2, X86Serialize, ArmBase, Crc32), and the whole Arm surface is Lowered: X86Base, X86Serialize, ArmBase, Crc32, AdvSimd, Aes, Sha1, Sha256, Dp and Rdm with their nested types; Lzcnt, Popcnt, Bmi1 and Bmi2 are mapped but wait for the families they imply (Sse42, Avx2). What remains, in this order of pay-off: PackedSimd together with the wasm SIMD build axis (`DN2CPP_WASM_SIMD`) and the supported arm of the wasm gate; Sse through Sse42 with Pclmulqdq and Aes; Avx, Avx2, Fma and AvxVnni; the Avx512, Avx10, AvxVnniInt8/Int16 and Gfni families; Sve and Sve2 last. Promotion is never a hand edit: the generator derives Lowered from map coverage and the feature-bit implications (every public static method of the family has a helper, and every family it implies — enclosing type and .NET instruction-set parents alike — is Lowered too), the surface gate re-derives both, and the generated `Exercises.g.cs` exercises each promoted family in the probe. Host-bound follow-up: the x86 gate's supported runs (S, and P with the partial-mask pair `DN2CPP_CPU_FEATURES=-Avx` vs `DOTNET_EnableAVX=0`, witnessed on X86Base and, once its chain is Lowered, Bmi1) have not yet run on an x86-64 host, so the x86 exercise output and the `DivideByZeroException` the faulting `DivRem` cases print are unverified against real .NET until the linux-x64 or windows-x64 lane runs the gate. |

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

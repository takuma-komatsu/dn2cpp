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
| 50 | intrinsics | **Platform ISA contract: the host-bound and compiler-surface follow-ups.** Every family with feature bits is Lowered (the whole `X86.*`, `Arm.*` and `Wasm.*` surfaces; `Sve` and `Sve2` have no feature bits and answer false by design), so what remains is verification. Host-bound: a manual run of `./gates/build-and-run-platform-isa-native.sh` on a provisioned windows-x64 host with `DN2CPP_SKIP_PLATFORM_ISA_NATIVE` unset must validate S across the full supported surface, including zero-divisor versus quotient-overflow `DivRem` exceptions, full-range SSE lane-index wrapping, and opaque VCMPSS/VCMPSD upper-lane preservation, then complete O (`DN2CPP_ENABLE_AVX10V2=1` vs `DOTNET_EnableAVX10v2=1` when the compiler can build it) and P (`DN2CPP_CPU_FEATURES=-Avx` vs `DOTNET_EnableAVX=0`, with X86Base kept and Bmi1 removed). Hosted Windows smoke excludes this gate. Linux-x64 S/O/P remain unverified. No AVX-512 or AVX10 instruction body has executed on supporting hardware: `tools/platform-isa-rosetta.sh` runs the probe under Rosetta 2, which has no AVX, and is a developer procedure without the .NET oracle, not a gate. Compiler-surface: the AVX10.2 rows use current Clang intrinsic spellings; their compiler-capability guard makes the affected tokens false under GCC, MSVC and older Clang, so adding and verifying each compiler's spellings remains open. |
| 166 | runtime | **Culture identifiers that require ICU locale data remain unresolved.** `dn2cpp_culture_by_name` in `runtime/core/intrinsics/dn2cpp_system_globalization.cpp` normalizes ASCII subtags and shares table formatting, but has no ICU alias, alternate-sort or extension data. .NET maps `eng` to `en` and `und` to invariant; `de-DE_phoneb` reports Name `de-DE` and an alternate-sort LCID, while `de_DE` retains its name and is neutral. `en-US-u-nu-arab` uses Arabic numeric separators. Constructor Name handling for uppercase extension markers or keys remains incomplete: .NET drops the extension from `de-DE-U-NU-LATN`, while `GetCultureInfo` ASCII-lowercases its input first and retains `de-DE-u-nu-latn`. Unmodeled regions and explicit-script countries use invariant data unless a compatible fallback is supported. For `de-NO`, .NET uses Norwegian separators and `kr` with Apple ICU, but German separators and `NOK` with standard ICU; `de-FOO` uses root numeric data with Apple ICU but German data with standard ICU. `zh-Hans-HK` requires simplified-script symbols and Hong Kong currency, so removing its script and using `zh-HK` gives the wrong NaN symbol. The supported script fallback is German `Latn`; other scripts require explicit data. `CreateSpecificCulture` retries malformed names through the language prefix but does not select a specific country for a neutral culture (`de` should become `de-DE`, `zh-Hans` should become `zh-CN`). Add deterministic canonicalization, neutral-to-specific and numeric-data tables, retaining culture identity separately from its formatting source, and extend the live-diff culture-name section of `gates/build-and-run-culture-info-api.sh` and the environment cases of `gates/build-and-run-default-culture.sh`. |
| 188 | intrinsics | **Convert.ChangeType discards the caller's format provider.** The Type and TypeCode overloads in `src/Dn2Cpp.Transpiler/MethodCompiler.EmitIntrinsic.Convert.cs` pop the provider before calling the provider-free matrix in `runtime/core/intrinsics/dn2cpp_system_convert.cpp`. With a counting provider returning a NumberFormatInfo whose NegativeSign is `minus`, `Convert.ChangeType((object)(-12), typeof(string), provider)` and the `TypeCode.String` sibling produce `minus12/1` on CLR but `-12/0` natively (result/GetFormat calls). Pass the provider through consumed conversions without rooting unused GetFormat bodies; add Type/TypeCode parity cases to `samples/dotnet/ConvertParse` and `gates/build-and-run-convert-parse.sh`. |
| 189 | intrinsics | **Unsupported Convert.ChangeType conversions report the wrong exception.** The Type and TypeCode routes in `src/Dn2Cpp.Transpiler/MethodCompiler.EmitIntrinsic.Convert.cs` share `dn2cpp_change_type_to` in `runtime/core/intrinsics/dn2cpp_system_convert.cpp`, whose DateTime target arm calls `dn2cpp_throw_invalid_operation` for a numeric source. `Convert.ChangeType((object)(-12), typeof(DateTime), CultureInfo.InvariantCulture)` and the `TypeCode.DateTime` sibling throw `InvalidCastException` on CLR but `InvalidOperationException` natively. Correct the unsupported source/target exception classification and add failure witnesses to `samples/dotnet/ConvertParse` and `gates/build-and-run-convert-parse.sh`. |

## Regression gate

Merge verification follows the scoped policy in `AGENTS.md` and
`CONTRIBUTING.md`: a passing default Debug `./gates/pre-merge.sh --skip-godot`
is sufficient for PRs without Godot-specific file changes, and coding agents
may run that check autonomously. Godot-specific changes require a human-run
Godot-inclusive pre-merge check. The parallel runner provides manual smoke
checks:

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
cached partials. Pre-merge enables a narrow exception for an absent proprietary
CRI SDK: CRI gates skip and are named in the receipt, while other missing
prerequisites, incomplete coverage and CRI execution failures remain red.
CRI Web templates are prepared only when that SDK is installed. A whole
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

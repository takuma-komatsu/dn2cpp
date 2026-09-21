# Real UnrealSharp smoke project

This source project requires the pinned [UnrealSharp-dn2cpp fork](https://github.com/takuma-komatsu/UnrealSharp-dn2cpp) checkout and an existing
UE installation. It is distinct from the UE-independent bootstrap fixtures.
The managed project templates are materialized by the helper so the ordinary
dn2cpp sample prebuild does not attempt to resolve external generated bindings.

Run `gates/run-unrealsharp-engine-smoke.sh` with `UE_ROOT` and
`UNREALSHARP_PLUGIN` set. The helper creates its working project under artifacts,
builds the Editor plugin modules and CLR assemblies, and runs a bounded headless
Editor scripting test (it does not launch PIE). The harness creates a Blueprint subclass with a custom event that
calls the managed `Add` function, plus a BeginPlay override that calls its
managed parent before hiding the Actor. It saves that Blueprint and its level under
the generated project's `Content` directory. It does not Cook or claim
packaged-game validation.

For gameplay inspection, open the generated project in Editor and run PIE in
`Dn2CppSmokeMap`. BeginPlay writes `begin-play=42 tick=False` through the native
logging callback. The headless harness calls the Blueprint's `RunBlueprintAdd`
event through `ProcessEvent`, then reads `Counter` through UE reflection and
requires `blueprint-call=45 property=45`. The override must preserve the parent's
initial counter and set the Actor's hidden state. The saved level and Blueprint serve as
the shared CLR/native comparison input for subsequent cooking.

After the helper builds and saves the fixture, run
`UE_ROOT=/absolute/path/to/UnrealEngine ./gates/run-unrealsharp-pie-smoke.sh`
for actual automated CLR PIE. This uses the Editor play-session API, verifies
automatic BeginPlay in a PIE world, invokes the saved Blueprint event, and
requires PIE to finish before writing its result. It has passed with the pinned
engine and plugin; the scripting gate and PIE gate remain separate checks.

The harness also invokes the separate feature Actor through reflected UFunctions
and checks scalar, enum, bool, Unicode, names/text, vector, array/set/map, object
identity and ref/out results. The shutdown probe Actor also checks matrix-to-rotator argument passing, opaque
struct storage alignment, and small/large native struct allocation in both CLR
and native runs. The async Actor exercises Task/ValueTask,
cancellation, native worker and game-thread callbacks, managed collection and a
weak UObject reference. The lifetime Actor verifies a default component, class
and Blueprint-event interface references, loaded soft references, native single
and multicast delegates, and wrapper invalidation after repeated UE destruction
and collection. The dependency project exercises registration and UType use
across user assemblies. Soft-reference checks use already loaded objects; they
do not prove loading an unloaded cooked asset. The interface probe uses a
Blueprint event because the pinned upstream callable-only interface path does
not resolve the concrete receiver method.

`-Dn2CppSmokeResult=/absolute/path/result.txt` writes deterministic results even
when Shipping logging is disabled. The helper requires both exit status and the
result file. The harness enumerates loaded macOS images and requires CLR in the
baseline; `-Dn2CppSmokeExpectNative` instead requires that no hostfxr, CoreCLR,
hostpolicy or CLR JIT image is loaded. A native run additionally requires staged
native output and cooked assets. The module's startup log alone is not a gameplay
pass.

Run the packaged fixture with the same harness:

```sh
UNREALSHARP_APP=/absolute/path/to/Baseline.app \
UNREALSHARP_BACKEND=Dn2Cpp ./gates/run-unrealsharp-package-smoke.sh
```

Use `Clr` for the baseline. The package gate checks the signature, result and
process exit; the native lane also rejects managed runtime files and requires
reverse module shutdown order in an artifact written independently of logging.
Set `UNREALSHARP_RESULT_DIR` to retain separate evidence for each configuration
or relocated application. It verifies an existing archive and does not build it.
For signed sandboxed apps, child-written evidence goes into a unique app-container
directory and is copied back on success or failure; the gate preserves signed
entitlements. `runtime-directory.txt` records the child output location.

`gates/run-unrealsharp-package-failure-smoke.sh` clones the native app and checks
missing-library and incompatible-library startup failures. Its incompatible
library is a deliberately failing ABI fixture, not an UnrealSharp runtime; real
backend ABI validation is covered by the standalone host tests.

To cook the generated fixture after the Editor gate succeeds:

```sh
"$UE_ROOT/Engine/Binaries/Mac/UnrealEditor-Cmd" \
  "$PWD/artifacts/unrealsharp/Baseline/Baseline.uproject" \
  -run=Cook -TargetPlatform=Mac -Map=/Game/Dn2CppSmokeMap \
  -Unattended -NullRHI -NoAutoRecompile
```

Use an absolute project path: the Editor resolves relative arguments against its
binary directory. Select the packaging backend in the generated project's
`Config/DefaultUnrealSharp.ini` before cooking each archive. Editor and Cook
continue to use CLR when the native Game backend is selected.

The cooked CLR Development baseline and native Development/Shipping packages
pass the same gameplay artifact. Native relocation, signed missing/incompatible
library failures, and ILDiet-enabled/disabled artifact parity are also verified.
The packaged native runs retain sandbox entitlements and require CLR absence.
The package gates retain `-LLM` as a verified mitigation for the pinned UE's
pre-runtime tracker startup race. This preserves signed entitlements and does
not modify the engine. Ordinary launches without the flag and an upstream fix
remain separate concerns; see [the integration guide](../../../docs/UNREALSHARP.md).

The opt-in pending-shutdown gate additionally checks a worker active at module
shutdown and a real foreign-thread callback after native runtime shutdown. Its
callback positive control runs before shutdown; the later call uses the released
tracked handle and must return without a managed action or boundary error. This
proves harmless late arrival, while the standalone admission tests establish
that rejection precedes handle lookup. The runtime owner registry also rejects
released handles independently, so the engine result alone does not distinguish
those two protections.

# Experimental UnrealSharp integration

The integration keeps UnrealSharp's CLR and Hot Reload in Editor and Cook,
then compiles Game assemblies into a native library for distribution. The
experimental backend and pinned fork provide static assembly registration,
managed/native callbacks, wrapper and handle ownership, and UAT packaging.
Standalone contract probes and a real UE sample exercise these paths separately;
the validation section distinguishes their evidence.

The integration targets macOS arm64 Game Development and Shipping builds.
`UE_ROOT` identifies the installed engine.
The ABI compatibility baseline is UE `5.8.3` and upstream UnrealSharp commit
`b78e073ab4e81e6eae3c57ba1f5ecf5f29eef1f4`. The native ABI retains this upstream
identity; it is distinct from the integration commit selected by `prepare.sh`
in the [UnrealSharp-dn2cpp fork](https://github.com/takuma-komatsu/UnrealSharp-dn2cpp).
Changing the ABI baseline requires an audit and rerunning both the CLR baseline
and native game validation.

## Prepare the plugin

Build the Release CLI and clone the pinned UnrealSharp integration:

```bash
dotnet build src/Dn2Cpp.Cli -c Release
integrations/unrealsharp/prepare.sh /path/to/Game/Plugins/UnrealSharp
integrations/unrealsharp/check-abi.sh /path/to/Game/Plugins/UnrealSharp
```

The prepare script clones the fork and checks out integration commit
[`7f197e0ad75fc9bf0d5bfb206f2f4962ed346535`](https://github.com/takuma-komatsu/UnrealSharp-dn2cpp/commit/7f197e0ad75fc9bf0d5bfb206f2f4962ed346535)
in detached HEAD state. It refuses an existing destination and applies no local
patches. Plugin implementation changes belong in the fork; this repository owns
the backend, native ABI contract, packaging helper, and verification probes.
The plugin checkout stays separate from UE. Follow UnrealSharp's normal setup
to build the C++ project and generate bindings. Game IL must use Game bindings;
Editor bindings are not a substitute.

The ABI probe first requires identical fork/runtime native ABI headers, then
extracts the fork's native initialization-result declaration,
checks its layout against the managed UTF-8 buffer declaration, and verifies the
delegate callback return type and shared callback-table storage across native
libraries. It uses CMake/Ninja without linking UE. It does
not replace a CLR initialization success/failure test inside the engine.

## Backend contract

`--unrealsharp` selects `Dn2Cpp.UnrealSharp`; it is mutually exclusive with the
Godot backends. Supply each UnrealSharp load-order manifest through repeatable
`--unrealsharp-load-order <file>` arguments. Manifests use the existing
`Priority`, `Collectible`, and `LoadOrder` format. Native images have a fixed
assembly set, so collectibility does not enable unload or runtime DLL loading.

Files are ordered by descending priority and then ordinal filename. Names in
each file retain their order. Duplicate or missing assemblies, an omitted input
assembly, and omitted module-initializer assemblies are rejected. The assembly
reference closure must be supplied at transpile time. Reflection stripping and
hot-update images are rejected for this backend.

The external ABI is declared in
`runtime/unrealsharp/dn2cpp_unrealsharp_abi.h`. Initialization validates the ABI
version, host structure size, compatibility pins, and callback table sizes
before entering managed code. The host owns callback storage until shutdown.
Strings crossing this ABI are UTF-8; export return values and `result.success`
are nonzero on success, with failure details in `result.error`.

Initialization installs the runtime and callback tables and prepares records
for the closed assembly set, then returns to UE so the manager can be created.
The host registers each baked assembly in order.
Registration marks its prepared record as loading, runs its module initializer, and then
completes module startup. Tick becomes available after registration finishes.
Required initializer exceptions fail registration; they do not publish a ready
runtime. Static constructors are not run in a global startup sweep.

Registration, tick, and shutdown belong to the initializing thread. Shutdown
stops lifecycle entry and permanently closes raw managed callback admission,
drains admitted callbacks before module cleanup, quiesces runtime workers, and releases handles only after quiescence succeeds. A cleanup failure
or timeout retains handles and the loaded image. The library
is process-lifetime state: reinitialization and native Hot Reload are unsupported.

The fork's managed branch replaces dynamic loading with a static registry,
constructs UObject wrappers on an existing uninitialized receiver, and retains
reflection invokers in place of runtime-generated function pointers. The backend
conservatively retains user code; it does not use cooked assets to trim types.

The native bootstrap does not apply the CLR host's `APP_CONTEXT_BASE_DIRECTORY`
override. `AppContext.BaseDirectory` currently follows the native executable
directory, rather than UnrealSharp's managed-library directory. Use UE path
APIs such as `UnrealSharp.Engine.Paths.ProjectDir()` for project paths; game
code depending on the CLR base-directory override needs adaptation. The pinned
UnrealSharp Game sources do not themselves read that override.

## Packaging path

The fork defaults `PackagingBackend` to `Clr`. Native Game loading is selected
in the project's `Config/DefaultUnrealSharp.ini` before cooking:

```ini
[/Script/UnrealSharpCore.CSUnrealSharpSettings]
PackagingBackend=Dn2Cpp
```

Editor and Cook continue to use CLR. The fork's packaging command accepts
`-PackagingBackend=Dn2Cpp` and `-Dn2CppRoot=<dn2cpp checkout>`; `DN2CPP_ROOT`
provides the toolchain path when the editor launches packaging. The packaged
configuration must agree with the command selection. It builds Game IL
with the `DN2CPP` compilation branch and invokes
`integrations/unrealsharp/package-native.py`. Generated C++ and runtime code
build with CMake/Ninja, separately from UBT's plugin/game build. Native managed
intermediates are separate from CLR output. `StageUnrealSharp` defaults native
staging to the project's `Intermediate/UnrealSharp/NativeStage` tree, preserving
the CLR files used by Editor; `-ArchiveDirectory` selects an explicit destination.

Register the plugin automation scripts for the installed engine:

```bash
CMAKE_BUILD_PARALLEL_LEVEL=4 "$UE_ROOT/Engine/Build/BatchFiles/RunUAT.sh" \
  "-ScriptDir=$UNREALSHARP_PLUGIN/Build/Scripts" StageUnrealSharp \
  -Project=/absolute/path/to/Game.uproject -UETargetType=Game \
  -UEBuildConfig=Development -TargetPlatform=Mac -TargetArchitecture=arm64 \
  -PackagingBackend=Dn2Cpp "-Dn2CppRoot=$PWD" \
  -ArchiveDirectory=/absolute/path/to/clean/staged/project
```

The staging helper can also be invoked with already generated Game IL:

```bash
python3 integrations/unrealsharp/package-native.py \
  --dn2cpp-root "$PWD" \
  --managed /path/to/Game/managed-output/net10.0 \
  --archive /path/to/clean/archive \
  --work /path/to/native-work \
  --unrealsharp-config /path/to/project/Config/DefaultUnrealSharp.ini \
  --configuration Development
```

It rejects a CLR backend setting and DLLs or CLR runtime libraries anywhere in
the archive or enclosing application. Set the backend before cooking; changing
the source configuration does not rewrite an existing cooked configuration.
It requires a clean archive, builds the native library, sets its install name,
signs and verifies it, and stages the library with load-order manifests. It
generates a fixed plugin manifest ahead of generated bindings and user modules;
the compiler and native loader receive the same ordered manifests. When
the destination is inside a Mac application, it re-signs and verifies the outer
application after writing the staged files. It does not stage execution DLLs
or a CLR runtime. Non-system native dependencies are currently rejected rather
than copied; a library depending on additional native dylibs cannot be packaged
by this helper. `--identity` or `DN2CPP_SIGN_IDENTITY` selects the signing
identity; the default is ad-hoc signing. The helper selects the macOS SDK and
compiler through `xcrun`, honoring explicit `SDKROOT` and
`CMAKE_CXX_COMPILER` overrides. Successful staging alone does not prove
that the app is relocatable, correctly bundled, or free of CLR loading.

The package smoke preserves the application's signed entitlements. For a
sandboxed app, it places child-written results and logs in a unique directory
inside that app's container and copies the evidence back to the requested
workspace directory, including after a failed process. Standard output remains
in the workspace. A sandbox file-access denial is not bypassed by removing the
entitlement.

## Verification and current limits

```bash
./gates/build-and-run-unrealsharp-bootstrap.sh
UE_ROOT=/path/to/UnrealEngine integrations/unrealsharp/check-json.sh /path/to/Game/Plugins/UnrealSharp
CONFIG=Debug ./gates/build-and-run-unrealsharp-bootstrap.sh
./gates/build-and-run-reflect-invoke.sh
python3 integrations/unrealsharp/test-package-native.py
python3 integrations/unrealsharp/check-storage-abi.py /path/to/Game/Plugins/UnrealSharp
python3 integrations/unrealsharp/check-project-outputs.py /path/to/Game/Plugins/UnrealSharp
python3 integrations/unrealsharp/test-smoke-sandbox.py
dotnet run --project integrations/unrealsharp/tests/UnrealSharpForkProbe.csproj \
  -p:UnrealSharpDir=/absolute/path/to/Game/Plugins/UnrealSharp
```

The fork probe compiles the actual fork handle registry and unmanaged
callbacks with minimal dependencies. It checks owner closure, strong and weak
retention, concurrent release, callback admission shutdown, and multicast
`Action` dispatch. The packaging test substitutes build/signing commands while
checking staging and signing order; it does not build a game.

The bootstrap gate uses explicitly named fixtures under `samples/unrealsharp`.
It validates lifecycle ordering, native callbacks, failure paths, compatibility
rejection, load-order validation, and behavior with ILDiet enabled and disabled.
The reflection gate compares existing-receiver constructors, exception
wrapping, direct delegates, and `DoNotWrapExceptions` against .NET.

The real [EngineSmoke](../samples/unrealsharp/EngineSmoke/README.md) uses
UHT-generated bindings, a managed Actor and component, and a saved Blueprint
subclass. Its BeginPlay override calls the managed parent and sets native hidden
state; a separate Blueprint event calls managed code. The fixture also covers
representative scalars, Unicode, names/text, structs and ref/out, containers,
class/interface/loaded soft references, UE single/multicast delegates, wrapper
identity and destruction, GC, async cancellation and thread transitions, and
registration across user assemblies. Matrix/storage ABI checks exercise native
argument passing, alignment and allocation.

| Real-engine check | Required evidence |
|---|---|
| CLR Editor scripting | Gameplay fixture and saved Blueprint/map creation. |
| CLR PIE | Play-session entry, automatic managed BeginPlay, Blueprint override/call and PIE exit. |
| CLR cooked Development | Deterministic gameplay artifact from the self-contained baseline. |
| Native cooked Development and Shipping | The same gameplay results in signed sandboxed apps, with CLR absent. |
| Native shutdown | Reverse module shutdown, pending-worker completion with live handles, and harmless foreign callback arrival after shutdown. |
| Native ILDiet disabled | Gameplay, lifecycle and shutdown artifact parity with the enabled image. |
| Native Development and Shipping relocation | Gameplay and shutdown results from moved applications. |
| Signed native startup failures | Explicit diagnostics for missing-library and incompatible-ABI fixtures. |

Each runtime gate requires both process success and its deterministic artifacts.
The native package checks preserve signed sandbox entitlements. Shipping CLR
is not part of the baseline matrix. The incompatible-library package test uses
a deliberately failing ABI fixture; actual backend ABI rejection is also checked
by the standalone host gate.

The interface fixture uses Blueprint events; the pinned upstream callable-only
interface route remains unsupported. Soft-reference checks use loaded objects
and do not establish asynchronous loading of unloaded cooked assets.

The package gate scans staged files for managed execution DLLs and CLR runtime
components, and the running sample checks loaded images. Cooked package
contents require a separate `UnrealPak -List` inspection and UFS/NonUFS staging
manifest scan.

The initial scope excludes dn2cpp Editor/PIE, native Hot Reload, runtime-added
assemblies, comprehensive replication/RPC compatibility, and other platforms.
When updating the fork integration, first commit and review the plugin changes
in its repository, then update the integration revision in `prepare.sh`. Keep the
upstream ABI identity unchanged unless its compatibility contract changes.
Re-audit callback signatures and ownership,
the bootstrap signatures, load-order semantics, generated bindings, registration
roots, and package layout before changing compatibility checks.

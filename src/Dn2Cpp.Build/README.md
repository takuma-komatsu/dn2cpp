# Dn2Cpp.Build

MSBuild integration for [dn2cpp](https://www.nuget.org/packages/dn2cpp) — the
.NET IL → C++ transpiler. Add this package to a console app and
`dotnet publish` produces a **native binary** (no .NET runtime needed to run
it) next to the managed publish output.

## Setup

```bash
dotnet tool install -g dn2cpp        # the transpiler + bundled C++ runtime
dotnet add package Dn2Cpp.Build      # this hook
dotnet publish -c Release
./bin/Release/net10.0/publish/MyApp  # the native binary
```

Prerequisites: the `dn2cpp` dotnet tool, plus MSVC on Windows or `clang++`
elsewhere (C++17), `cmake` ≥ 3.20 and `ninja`.

## Properties

| Property | Default | Meaning |
|----------|---------|---------|
| `Dn2CppEnabled` | `true` | Set `false` to publish managed-only |
| `Dn2CppTool` | `dn2cpp` | Path to the dn2cpp executable; use a wrapper executable for a local tool manifest |
| `Dn2CppExtraArgs` | `--auto-ref` | Extra transpiler args (e.g. `-r` references) |
| `Dn2CppCMake` / `Dn2CppGenerator` | `cmake` / `Ninja` | Native build front end |
| `Dn2CppCMakeArgs` | (empty) | Extra cmake configure args |
| `Dn2CppNativeExecutableExtension` | `.exe` on a Windows host; empty otherwise | Override when the native target's executable suffix differs from the host |

The publish target always passes `$(MSBuildProjectDirectory)` as a
`--project-root`, so Unity-format `link.xml` files below the project directory
participate in stripping without extra configuration.

`Dn2Cpp.Build` accepts a host-default publish with no RID, plus
`linux-x64`, `linux-arm64`, `osx-x64`, `osx-arm64`, `win-x64`, and
`win-arm64` on their matching host
operating system. Use the target platform or its dedicated exporter for
cross-OS builds. A same-OS macOS
cross-architecture publish requires a non-empty `CMAKE_TOOLCHAIN_FILE`
assignment in `Dn2CppCMakeArgs`, or may instead set
`CMAKE_OSX_ARCHITECTURES` to a list containing the target architecture. A bare
`CMAKE_SYSTEM_PROCESSOR` value is not a compiler/toolchain selection and does
not enable cross-architecture publishing.

Native assets supplied by package references under `runtimes/<RID>/native` are
linked from the RID selection made by the .NET SDK. A macOS fat binary under
`runtimes/osx-universal/native` is also considered when an exact macOS asset is
absent and the package also contributes a managed reference. Native-only
packages must use an RID resolved by the .NET SDK. Shared assets and their
selected package-local dependency closure are copied into an immutable
`.dn2cpp-native/<generation>/` directory beside the executable;
macOS load identities and Linux SONAME aliases are normalized on private copies
without modifying the NuGet cache. Windows DLLs need no package-supplied import
library: dn2cpp constructs a delay-load library from the reachable P/Invoke
manifest and loads the DLL from its generation with dependency-directory search
enabled. The executable selects exactly one complete generation, so republishing
cannot expose a mixture of old and new dependencies to concurrent launches. The
immediately previous complete generation is retained until the next successful
publish, covering a process that opened the old executable just before the
switch. Dependencies absent from the SDK-selected native set must be supplied by
the operating system. Run only one publisher for a given `PublishDir`; immutable
generations protect concurrent application launches, not simultaneous publish
writers.

Console apps only; the Godot lanes (GDExtension / mono-module drop-in) have
their own export paths — see the
[repository](https://github.com/takuma-komatsu/dn2cpp).

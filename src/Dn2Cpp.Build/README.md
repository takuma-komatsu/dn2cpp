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

Prerequisites: the `dn2cpp` dotnet tool, plus `clang++` (C++17), `cmake` ≥ 3.20
and `ninja`.

## Properties

| Property | Default | Meaning |
|----------|---------|---------|
| `Dn2CppEnabled` | `true` | Set `false` to publish managed-only |
| `Dn2CppTool` | `dn2cpp` | How to invoke the tool (path or `dotnet tool run dn2cpp`) |
| `Dn2CppExtraArgs` | `--auto-ref` | Extra transpiler args (e.g. `-r` references) |
| `Dn2CppCMake` / `Dn2CppGenerator` | `cmake` / `Ninja` | Native build front end |
| `Dn2CppCMakeArgs` | (empty) | Extra cmake configure args |

Console apps only; the Godot lanes (GDExtension / mono-module drop-in) have
their own export paths — see the
[repository](https://github.com/takuma-komatsu/dn2cpp).

# dn2cpp

**.NET IL → C++ transpiler + native runtime** — a Unity-IL2CPP-equivalent for
.NET. Transpile any net10.0 assembly to C++ and build it into a self-contained
native executable (or a Godot GDExtension / mono-module drop-in) with the
bundled CMake runtime. No .NET runtime is needed on the machine that runs the
produced binary.

## Prerequisites

- .NET 10 runtime (runs the tool; also provides the BCL IL that `--auto-ref`
  transpiles)
- `clang++` (C++17), `cmake` ≥ 3.20, `ninja` (build the generated C++)

## Quick start

```bash
dotnet tool install -g dn2cpp

# 1. transpile: IL -> C++ (--auto-ref pulls the BCL from the live .NET runtime)
dn2cpp MyApp.dll -o out --auto-ref

# 2. native build: the tool carries the C++ runtime; ask it where
cmake -S "$(dn2cpp --print-runtime-dir)" -B out/build -G Ninja \
      -DDN2CPP_APP_DIR="$PWD/out" -DDN2CPP_APP_NAME=MyApp
cmake --build out/build

./out/build/MyApp
```

`MyApp.dll` is your project's build output (`dotnet build` is enough; the IL is
the input, not a published app).

## Beyond console apps

- `--dotnet-module` emits a Godot mono-module drop-in library
  (`-DDN2CPP_DOTNET_MODULE=ON` on the cmake configure).
- `--gdextension` emits a Godot GDExtension library
  (`-DDN2CPP_GDEXTENSION=ON`), always named `libdn2cpp.dylib`/`libdn2cpp.so`/
  `dn2cpp.dll` whatever the app is called; the engine-shim assembly is generated from your
  Godot build's `extension_api.json` via `dn2cpp --generate-bindings` — see the
  repository docs.

## Learn more

Architecture, supported IL surface, Godot integration, and the full docs live
in the repository: <https://github.com/takuma-komatsu/dn2cpp>

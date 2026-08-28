# Editor Export Design — a forked Godot with a dn2cpp export backend

Design for a **fork of Godot** whose C#-game export lets the user pick the
backend — the standard .NET host runtime, NativeAOT, or **dn2cpp** — and for
packaging dn2cpp into the distributable editor. The dn2cpp side consumes the
shipped mono-module drop-in lane (`--dotnet-module`). Open rows are in
`docs/STATUS.md`.

## 1. Why the existing engine load path is enough

Godot already loads a NativeAOT-exported C# game through a **drop-in native
library** path: `GDMono::initialize()` falls through to
`try_load_native_aot_library` (`modules/mono/mono_gd/gd_mono.cpp`), which opens
`{api_assemblies_dir}/{Assembly}.{dll|dylib|so}` and resolves
`godotsharp_game_main_init`. For an exported game
`api_assemblies_dir = {exe_dir}/data_{csprojname}_{platform}_{arch}`
(`modules/mono/godotsharp_dirs.cpp`).

`--dotnet-module` emits exactly that shape and adds no new engine ABI. The fork's
main work is the export side (GodotTools, C#) plus assembling the editor
distribution. Two load-path adjustments remain in engine code. Web (§10)
enables the existing path for wasm32. On Windows,
`try_load_native_aot_library` passes `OS::GDExtensionData` with
`also_set_library_path = true` for its one `open_dynamic_library` call, so a PE
drop-in resolves dependencies staged beside it in the data directory. The flag
does not change any other dynamic-library open or any non-Windows target.

### ABI no-touch list (fork invariant)

The drop-in lane's ABI is pinned by a fingerprint over three engine surfaces
(`gates/expected/godot-dotnet-abi.sha256`):

- `modules/mono/glue/runtime_interop.cpp` — the `unmanaged_callbacks[]` block
  (order and count; `NativeFuncs.Initialize` indexes into it).
- `modules/mono/glue/GodotSharp/GodotSharp/Core/Bridge/ManagedCallbacks.cs` —
  the `ManagedCallbacks` struct.
- `modules/mono/glue/GodotSharp/GodotSharp/Core/NativeInterop/NativeFuncs.cs` —
  the interop declarations. The backend rebuilds each `calli` function-pointer
  type from the managed signature, so a widened parameter or return type is a
  re-audit and not a re-freeze.

**The fork must never modify these three.** The Windows and Web load-path edits
live outside them in `modules/mono/mono_gd/gd_mono.cpp` and do not widen the
interop contract. Leaving all three surfaces byte-identical keeps the
fingerprint and keeps the fork drop-in compatible; the fork gates re-check the
same fingerprint.

## 2. Fork strategy

- **Base: `4.7.2-stable` (`ed1daf0bf0`)**, aligned with the 4.7 GDExtension lane
  and the shipped `extension_api.json`.
- **Local clone**: an independent clone with its own `.git`, defaulting to a
  sibling of the dn2cpp checkout (`DN2CPP_GODOT_FORK_CLONE` overrides), branch
  `dn2cpp/main`, `upstream` `godotengine/godot`. The pristine clone
  (`DN2CPP_GODOT_CLONE`) is separate and stays pristine for the drop-in gates.
- **Two pins, one fingerprint**: the drop-in gates pin the pristine clone, the
  fork gates pin the fork (`gates/expected/godot-fork-pin.txt`) and re-check the
  *same* `godot-dotnet-abi.sha256` against the fork tree.

## 3. Export UX in the fork

A new export-preset enum option `dotnet/export_backend`:

| Value | Behavior |
|-------|----------|
| `Host Runtime` (default) | Exactly today — csproj decides; hostfxr/coreclr load path. |
| `NativeAOT` | Inject `PublishAot=true`; the existing native-output probe in `_ExportBeginImpl` handles the result. The A/B baseline. |
| `dn2cpp` | Transpile the published IL to a drop-in library. |

Declared in `GodotTools/GodotTools/Export/ExportPlugin.cs::_GetExportOptions`.
The pipeline is `GodotTools/GodotTools/Export/Dn2CppExporter.cs`, invoked from
`_ExportBeginImpl` after `PublishProjectBlocking` produces `{Assembly}.dll`.
`NativeAOT` needs a `BuildManager.cs` change to thread extra MSBuild properties
through `PublishProjectBlocking`.

1. **Preflight**, once per export and *before* the publish: toolchain bundle
   present, a `cmake >= 3.20` and a `ninja` — each resolved from the editor
   setting naming it (`dotnet/export/dn2cpp_cmake_path`,
   `dotnet/export/dn2cpp_ninja_path`), else the bundle's own `buildtools/`, else
   PATH, which is why a bundle carrying the pair needs neither installed — and
   for host-compiled targets a host C++
   compiler, else `AddMessage(Error, …)` with an install hint and abort. The
   configure in step 3 spells no compiler, so cmake picks the platform default
   and the preflight probes for exactly that (`Dn2CppExporter.HostCxxCompiler`);
   Web and Android hand cmake their own toolchain files and need none. Platform
   gate: **macOS, Windows and Linux share one host-compiled desktop arm**
   (`universal` and cross-architecture presets refused, since the bundle carries
   only the host's compiler); iOS, Android and Web each check the single
   architecture the backend builds; anything else is a `NotSupportedException`.
   On the desktop arm the **host OS must equal the target OS** — arch equality
   alone lets an x86_64 Windows host accept an x86_64 Linux preset and die
   minutes later inside a compiler. One host predicate per target, never a
   two-valued test: "Windows, else macOS" answers macOS for a third target.

2. **Transpile** with the bundled native CLI:
   `dn2cpp {Assembly}.dll --dotnet-module -r <bundle>/<ref|ref-posix>/System.Private.CoreLib.dll -r <editor>/GodotSharp/Api/Release/GodotSharp.dll [-r <dep>.dll …] --auto-ref -o <gen>`.
   The editor's own `GodotSharp.dll` makes the bindings ABI match the editor and
   templates by construction. The game assembly is copied to a scratch dir
   first, since `--auto-ref` takes as framework directory the first passed
   assembly's directory holding a `System.Private.CoreLib.dll`. The game's
   managed dependencies follow the CoreLib `-r`; the scan collecting them tests
   each candidate for a **CLI metadata header**, not a `.dll` extension, because
   a framework-dependent Windows publish drops native runtime DLLs into the
   output and handing one to `-r` aborts the export.

   **The framework flavour is a function of the export TARGET, never the host**
   (`Dn2CppToolchain.CoreLibRefFor`). The transpiler consumes the CoreLib's
   *IL*, so the flavour decides which native libraries the emitted P/Invokes
   name: a Windows framework names kernel32/ntdll and ole32, which neither the
   NDK sysroot nor Emscripten has. **On a Windows host every target but Windows
   transpiles against `ref-posix/`** — a complement, not a list, so a platform
   added later is not forgotten into the host's framework. On a POSIX host
   `ref/` already is that flavour. `Create` refuses up front when the needed
   flavour is unstaged; otherwise the transpile succeeds against the wrong
   framework and fails as `-lkernel32`. The publish RID stays the host's.

3. **Native build**:
   `cmake -S <bundle>/runtime -B <build> -G Ninja -DDN2CPP_DOTNET_MODULE=ON -DDN2CPP_APP_DIR=<gen> -DDN2CPP_APP_NAME={Assembly}`
   (plus the prebuilt runtime import when a key matches, §4), then
   `cmake --build`. The build dir persists under `.godot/mono/dn2cpp/`. **No
   `CMAKE_BUILD_TYPE`**: `runtime/CMakeLists.txt` pins its own `-O2` per target,
   so a build type only adds `-g` or `-DNDEBUG`, and `NDEBUG` silently disables
   the runtime assertions every gate runs with. The work dir's four trees
   (`il/`, `gen/`, `build/`, `stage/`) are named per export target and each
   `RecreateDirectory` site clears only the slot it rewrites, so a tree whose
   *name* no current export computes is never collected; a recorded
   **slot-layout generation** (`layout.txt`, `Dn2CppExporter.WorkDirLayout`)
   covers that, and bumping it makes the next editor's first export drop all
   four. Export logs are bounded likewise, newest
   `Dn2CppExporter.LogGenerations` kept.

4. **Stage**: copy the produced library as `{Assembly}.{soExt}` into a fresh
   staging dir and point the existing `RecursePublishContents` /
   `AddSharedObject` at it, so it lands in
   `data_{csprojname}_{platform}_{arch}/`. Project-declared
   `dotnet/dn2cpp/extra_shared_objects` land beside it; no managed runtime may
   enter this directory because `try_load_native_aot_library` runs only after
   `load_hostfxr` and `load_coreclr` both fail. On Windows the NativeAOT load
   call adds this directory to the dependency search only while opening the
   drop-in, which is what makes those native siblings usable without copying
   them beside the executable.

**Error surfacing.** stdout/stderr tee to
`{ProjectBaseOutputPath}/dn2cpp/logs/export-<ts>.log`; the tail and log path go
through `GetExportPlatform().AddMessage(Error, …)`. The fork fails the export on
an Error-class plugin message — `ERROR: Project export for preset "X" failed.`
and a non-zero exit from a headless `--export-release`, where upstream printed
"completed with warnings" and exited 0. The E2E gates assert the exit code, the
dn2cpp progress markers and the staged artifact, and the Web gate's
transpile-failure section proves the exit code moves: an artifact alone cannot
say a transpile failed, because the exported page comes from the template.

**Platform matrix.** macOS host-arch and **Windows** are the two host-compiled
desktop targets. Windows differs only in the output name, and that is the
platform's rule: MSVC/lld write `<name>.dll` with **no `lib` prefix**, and the
`WINDOWS_ENABLED` branch of `try_load_native_aot_library` opens exactly
`<Assembly>.dll`. That branch also requests the DLL's containing directory for
PE dependency lookup; the shared `OS_Windows::open_dynamic_library` default is
unchanged. **iOS** cross-builds each RID (`ios-arm64`,
`iossimulator-arm64/-x64`) via `-DCMAKE_SYSTEM_NAME=iOS`; the existing
`_aot.xcframework` tail of `_ExportBeginImpl` lipos and embeds the results
unchanged, reading one `{Assembly}.dylib` per entry of the export's output-path
list, which here are the per-slot staging dirs. So the three RIDs share one
publish and one transpile, with only the native compile per slot. That publish
is framework-dependent (`PublishAot=false SelfContained=false` overriding
`iOS.props`) and simulator runs are unsigned. **Android** cross-builds through
the NDK's own CMake toolchain file and stages `lib{Assembly}.so`; its setup must
also fill in the editor's `export/android/java_sdk_path`, a **hard**
prerequisite and not a PATH one, since `EditorExportPlatformAndroid::can_export`
reads that Editor Setting and refuses the whole preset when empty, before any
dn2cpp step runs. **Linux** is the third host-compiled desktop arm: same
host-equals-target rule as macOS and Windows, `lib{Assembly}.so` built and
`{Assembly}.so` staged. **Web** is §10.

## 4. Packaging the dn2cpp toolchain into the editor

**Location**: `GodotSharp/Dn2Cpp/` beside the editor binary, inside the
directory every editor packaging flow already carries. Only editor-side C#
consumes it, so no `godotsharp_dirs.cpp` change:
`GodotTools/Utils/Dn2CppToolchain.cs` resolves it as the sibling of
`GodotSharpDirs.DataEditorToolsDir`. Not from `Assembly.Location`, which is
empty because the editor loads GodotTools from a stream.

**Bundle contents** (`dn2cpp-toolchain-<version>-<host_os>-<host_arch>.tar.gz`):

- `bin/dn2cpp` — the self-hosted native CLI (`gates/selfhost-emit.sh`: full
  Transpiler + GodotBackend + DotnetModuleBackend, **no .NET runtime
  dependency**). This is the enabler: the bundle transpiles without .NET.
- `bin/Dn2Cpp.Runtime.dll` — the managed shim defining the types the transpiler
  synthesizes (notably `SZArrayEnumerable<T>`). It **must sit beside the CLI**,
  since `TranspileDriver` auto-references it from `AppContext.BaseDirectory`;
  consumers may also pass it with `-r`, as `dist/smoke-test.sh` and
  `Dn2CppExporter` do, because an explicit path survives a relocated binary.
  Missing, a transpile that needs it fails with an actionable error
  (`Compilation.RequireShimType`) instead of dying at run time.
- `bin/DnZlib.dll`, `bin/DnBrotli.dll`, `bin/DnHttp.dll` — the **conditional
  default references**, beside the CLI for the same reason, injected only when
  the BCL assembly each serves is in the load set
  (`Compilation.InjectDefaultRefs`). A game exporter passes nothing:
  `GodotSharp → System.Diagnostics.StackTrace → System.Reflection.Metadata →
  System.IO.Compression` puts every Godot-lane transpile in DnZlib's trigger
  set, and an unreached shim costs one assembly-registry row. All three ship
  unconditionally, since which a game needs is decided by its IL.
  `dist/package-toolchain.sh` derives the required set from
  `src/Dn2Cpp.Cli/Dn2Cpp.Cli.csproj` rather than listing it, so a fourth shim
  forgotten here fails at packaging time, not inside an export log.
- `runtime/` — `runtime/core` (with intrinsics and PAL), `runtime/dotnetmodule`,
  `runtime/CMakeLists.txt` verbatim, and `runtime/cmake/`, the `cmake -P`
  helpers the CMakeLists runs at **build** time (today
  `runtime/cmake/dn2cpp_embed_bytes.cmake`, which embeds
  `third_party/cacert/cacert.pem` under `DN2CPP_USE_CURL`). Omitting one
  configures cleanly and fails in ninja; packaging derives the required subtrees
  from the shipped CMakeLists and treats `runtime/godot` as the single
  deliberate omission.
- `third_party/{bdwgc,zlib,brotli,highway,curl,mbedtls,cacert}` plus
  `gdextension_interface.h` — **every** vendored tree `runtime/CMakeLists.txt`
  names, whether or not the option that builds it defaults on, because the
  bundle's `runtime/` is a source tree the exporter configures and an absent
  `add_subdirectory` target is a CMake `FATAL_ERROR`. Each tree is copied
  **whole**; they are already curated subsets. Packaging derives the required
  set from the shipped `runtime/CMakeLists.txt` and fails if the copy list has
  fallen behind.
- `ref/` — the **whole pinned net10 shared-framework closure** (every `*.dll`
  beside `System.Private.CoreLib.dll`), not CoreLib alone, because `--auto-ref`
  resolves the game's BCL references from the directory holding CoreLib.
- `ref-posix/` — the same closure in the **POSIX flavour**, staged **only by a
  bundle built on a Windows host** (§3 step 2). There an absent linux-x64
  runtime pack is a **hard error** in `dist/package-toolchain.sh`, not a skip:
  skipping produces a bundle that exports desktop games perfectly and fails the
  first Android or Web export inside a linker.
- `prebuilt/<axis>/` — the runtime's static archives plus a **re-anchored**
  `dn2cpp-targets.cmake`, so an export imports the runtime instead of building
  it and the vendored trees.
- `emsdk/` — the pinned Emscripten SDK (`gates/expected/emsdk-pin.txt`), reduced
  to what `dist/emsdk-trim.txt` keeps — a KEEP list, and the only statement of
  what the bundle carries of the SDK: an upstream directory nobody here has heard
  of is dropped and, if it mattered, fails the packaging's own re-link, where a
  deny list would ship it forever and say nothing. Its
  `.emscripten` is `$CFGDIR`-relative and it carries **no `cache/sanity.txt`** —
  that file records an absolute `LLVM_ROOT`, and emcc erases the whole cache, the
  shipped sysroot included, on finding one that names another path. A Web export
  compiles through it, so a host with no SDK can make one; it is also what lets
  the web prebuilt's key name a toolchain file *inside* the bundle. Absent when
  the packaging host had no SDK, which refuses only a Web export. Its `node/`
  sits **inside** the SDK, the placement the Windows SDK's `python/` has and for
  the same reason: it is a second runtime `emcc` starts on every link, and the
  SDK's own `$CFGDIR`-relative config is what names it. Trimmed to the one
  executable plus `node/LICENSE`, whose text carries the bundled V8, ICU,
  OpenSSL and zlib terms as well.

  Re-staging is keyed on a stamp of the upstream archive's sha256, the keep list
  and the bake recipe below, so a re-package re-copies the SDK exactly when one
  of the three moved. `gates/_common.sh` keys the gate result cache on that same
  stamp: two SDKs of one version answer `emcc --version` identically, and a
  bundle re-staged from another recipe has to re-run the gates that used it
  rather than replay their green.

  Its cache is **frozen** (`FROZEN_CACHE = True`), so an export resolves every
  system library out of the bundle and writes nothing back — a bundle installed
  read-only, which is where an editor puts it, would otherwise die inside emcc's
  cache lock. Packaging links one translation unit in each shape this SDK is
  asked for — the drop-in side module, the console main module, and a
  `MAIN_MODULE`, which is what a dynamic-linking Web template is and what pulls
  the `pic/` half of the sysroot — to fill whatever the release archive does not
  carry, trims, then relinks all three with the whole SDK `chmod -R a-w`. That
  re-link is the assertion, and it is the trim's oracle too: a link wanting a
  variant nothing baked or an entry the keep list dropped fails naming the file.
  `dotnet/export/dn2cpp_emsdk_cache_path` is the way out — the fork points
  `EM_CACHE` at that directory and sets `EM_FROZEN_CACHE=0`, which outranks the
  config key. That a real export needs no way out is asserted by
  `gates/build-and-run-godot-editor-export-web-hermetic.sh`.

  Baking the ENGINE's Web template does need one, which is why it declines this
  SDK: its `-sMAIN_MODULE=1 -sMAX_WEBGL_VERSION=2` link resolves `pic/` archives
  no export asks for, so `gates/setup-godot-fork-web.sh` takes the unpacked SDK
  of the same pinned version — refusing a version split, because that template
  and this drop-in are two halves of one wasm program. The same cut runs through
  the gates (`dn2cpp_emsdk_resolve`, `gates/_common.sh`): the bundle answers only
  where the SUBJECT is the shipped shape —
  `gates/build-and-run-godot-dotnet-wasm.sh`,
  `gates/build-and-run-godot-editor-export-web.sh` and
  `gates/build-and-run-cri-web.sh`. Every other wasm build passes `--no-bundled`,
  because a dev-lane link carries no `-O` and so resolves the `-debug` archives
  no export links and the bake therefore never filled.
- `buildtools/` — the pinned **cmake and ninja**
  (`gates/expected/buildtools-pin.txt`, unpacked by
  `gates/setup-buildtools.sh`), reduced to what `dist/buildtools-trim.txt`
  keeps: the one `cmake` executable, its `Modules/` tree **whole**, and the
  licence texts. Two things follow from carrying them: a host with neither can
  export at all, and the cmake an export runs is the one this repository chose —
  a version inside a `forbidden_cmake` band builds the Web drop-in as a static
  archive, which the pin refuses and a cmake on PATH cannot be held to.
  `Modules/` is kept whole deliberately: `find_package`, `check_*_source_compiles`
  and every `try_compile` read it, and Emscripten's platform module and the NDK's
  toolchain file `include()` cmake's own `Platform/`/`Compiler/` — a list of the
  modules today's configure happens to name would be a second, drifting copy of
  somebody else's CMakeLists. The licence texts are named in the keep list
  because everything else under `doc/` is dropped, so the rule that keeps a
  licence beside its surviving package cannot reach them. On macOS every Mach-O
  is thinned to `arm64`, the archive being universal while
  `dist/package-editor-macos.sh` refuses a non-arm64 host. The staged pair is
  then RUN and version-checked, because a binary that will not exec — thinned
  past its signature, quarantined — would otherwise fail in a user's export log.
  Absent when the packaging host unpacked none, which leaves an export needing
  both on the host.

  Re-staging is keyed on a stamp of the two archives' sha256 plus the keep list,
  as the SDK's is. The tree is `chmod -R a-w` for the prebuilt stage and asserted
  untouched afterwards: cmake writes nothing into `CMAKE_ROOT`, and an editor
  installs this where the user cannot write. That it holds for a real export, and
  that an export reaches this pair rather than the host's, is
  `gates/build-and-run-godot-editor-export-web-hermetic.sh` — the same gate that
  answers for the SDK, on a PATH that now loses cmake and ninja too.
- `manifest.json` — dn2cpp commit, package version, target godot pin, ABI
  fingerprint copy, content hash, and the staged SDK's version, release hash and
  `emcc --version`; logged at export, the fork-to-dn2cpp half of the version
  contract. The content hash covers every bundled file except `emsdk/`, whose
  bytes the upstream archive's own sha256 already fixes; `buildtools/` is in it,
  being small, deterministic and written to by nothing, so moving the pin moves
  the hash.

**The prebuilt cache, one directory per axis.** `dist/package-toolchain.sh`
stages `host` always; `ios-arm64`, `iossimulator-arm64`, `iossimulator-x64` on a
macOS host with both iOS SDKs; and the Android and Web axes on a host holding
their cross toolchain (the NDK, resolved as `Dn2CppExporter.ResolveAndroidNdk`
resolves it, and the staged `emsdk/`, else an SDK on PATH). A host missing one
drops that axis alone, naming the missing tool. The Web axis builds through the
*bundled* SDK wherever there is one, which is what puts its toolchain file under
the bundle root — and the relocated verify below runs the copy's own `emcmake`,
or it would prove a path that only the packaging machine holds. CMake's `export()` writes a *build-tree* export
with absolute packaging-machine paths, so packaging re-anchors both path
families on the shipped file's own location, checks that nothing of its own
machine survived, and proves it by configuring a **relocated copy** of the
finished layout once per axis with that axis's cmake vars.

A prebuilt is adopted only when its `key.txt` describes the configure asking for
it: toolchain (compiler id and version, system, Apple/Android/Emscripten target
vars), every option reaching a runtime target, and a stamp pairing the archives
with the sources beside them (`--install-into` overwrites a bundle without
deleting what the previous one dropped, so a `prebuilt/` can outlive its
`runtime/`). Consumers read every key rather than matching on the directory
name, since a name is a second expression of the axis. The key is written
*once*, in `runtime/cmake/dn2cpp_prebuilt.cmake` — packaging asks with
`DN2CPP_EMIT_PREBUILT_KEY`, `runtime/CMakeLists.txt` compares against what it
wrote — because a second expression drifts fail-open into an archive from
another toolchain accepted and dying at link. `ANDROID_PLATFORM` and
`CMAKE_TOOLCHAIN_FILE` are in the key for the cross axes, the API level reaching
it no other way. A toolchain path *under the bundle root* is recorded relative
to it — with both sides symlink-resolved, since an export configures through the
`.app`'s `Contents/MacOS/GodotSharp` — or a bundle carrying its own cross
toolchain would refuse its archives everywhere but the packaging machine's
layout; `EMSCRIPTEN_VERSION` then names the emsdk the path no longer does.
Packaging and `Dn2CppExporter.BuildDropIn` spell
those vars in two repositories with no shared constant, and a drift is fail-safe
and therefore silent, so the three cross-target export gates each assert the
adoption. The host axis follows the same rule on macOS: packaging and the bundle
smoke configure with `MACOS_DESKTOP_DEPLOYMENT_TARGET`, while the desktop export
gate holds the selected architecture's preset value to that floor. What it buys
is mostly CMake configure time, fixed per build slot; the drop-in is
**byte-identical** either way.

**Produced by** `dist/package-toolchain.sh` (runs the selfhost build, assembles
under `artifacts/toolchain/`, tars; flags `--layout-only`, `--install-into
<editor GodotSharp dir>`); the fork's `build_assemblies.py --bundle-dn2cpp
<layout|tar>` installs it into the distribution. **Smoke-tested by**
`dist/smoke-test.sh`, which using *only* the bundle's contents transpiles and
native-builds the drop-in sample and asserts the `godotsharp_game_main_init`
export; it is not a regression gate, and it proves the bundle *builds* a
drop-in, never that the drop-in *runs*. **Override**: editor setting
`dotnet/export/dn2cpp_toolchain_path`, pointing at a live layout dir in the
dn2cpp tree — also the fast dev loop (§5).

**Host prerequisites** (documented and preflight-checked): .NET SDK — the IL
build via `dotnet publish` still runs, since dn2cpp removes the runtime from the
*game*, not the SDK from the *export machine* — and, for host-compiled targets, a
C++17 host compiler (`clang++` from the Xcode Command Line Tools on macOS, the
Visual Studio C++ workload on Windows). cmake and ninja are not on the list: a
bundle carries the pinned pair. **On Windows that workload has to be installed
and nothing more — the editor imports MSVC itself.** cmake picks `cl.exe` under
Ninja, and Visual Studio puts neither `cl.exe` on the machine's PATH nor
`INCLUDE`/`LIB`/`LIBPATH` anywhere outside a shell that has run `vcvarsall`. So
the preflight asks `vswhere` for the newest install carrying the C++ tools, runs
`vcvarsall` for the *host* architecture, and overlays those four variables onto
the configure and the compile alike — cmake bakes neither into the Ninja files,
so both halves need them. An environment that already carries them is left
untouched, which is what keeps a Developer Command Prompt launch behaving
exactly as before, and the editor's own environment is never written: MSVC must
not leak into `dotnet`, MSBuild or a game started with Play. Asserted by
`gates/build-and-run-godot-editor-export.sh` section 9/13, which exports with
every C++ compiler stripped from PATH and those three variables emptied — the
state Explorer launches an editor in — and reads back both the import and the
`cl.exe` cmake then chose. Web and Android
are exempt — each brings its own compiler — so off a bundle carrying the build
tools those two export from a plain Explorer launch, with only the .NET SDK
installed. A Web export off a POSIX host wants one more the bundle does not
carry: `emcc` is a Python program, an interpreter travels with the SDK on
Windows alone, and Apple's `/usr/bin/python3` is below the 3.10 emcc requires.
It is the last host tool a Web export needs, and nothing preflights it — node
left that list by moving into the SDK, python was never on it. What survives on
macOS is the compiler alone: an editor launched from Finder inherits a minimal
PATH, so a `clang++` outside `/usr/bin` is invisible to it.

## 5. Repo / project structure for parallel development

Layout is env-configurable; both godot clones default to siblings of the dn2cpp
checkout.

```
<workspace>/
  dn2cpp/           main checkout — gates, docs, tickets, dist/
  dn2cpp-editor/    editor lane worktree
  godot/            pristine clone — drop-in gates (DN2CPP_GODOT_CLONE),
                     kept at the pin, which is the fork's base too
  godot-dn2cpp/     fork clone — own .git, branch dn2cpp/main
                     (DN2CPP_GODOT_FORK_CLONE)
~/.cache/
  dn2cpp-godot-dotnet/  drop-in lane cache (untouched)
  dn2cpp-godot-fork/    fork artifact root: export templates, nuget feed,
                     bin-cache, pin/fork_head, and the recorded paths into
                     the fork clone (editor.txt / template.txt / clone.txt)
```

- **No submodules.** The godot tree is huge and slow to build; the gates model
  godot as an external pinned resource via env plus committed pin files.
  Submodules would couple clone lifecycles and break the pristine/fork split.
- **Bidirectional pin contract**: dn2cpp to fork via
  `gates/expected/godot-fork-pin.txt`, recording the **base** commit
  (`dn2cpp/main` must descend from it) and not the fork's moving HEAD; fork to
  dn2cpp via the bundled `manifest.json`.
- **Engine rebuilds are content-keyed.** GodotTools is a *managed* assembly the
  editor loads from `GodotSharp/Tools/` at run time, while the Windows and Web
  load-path adjustments are engine sources. `gates/setup-godot-fork.sh` hashes
  the engine-owned tree against the base commit: an identical tree reuses the
  pristine binaries, and a changed tree takes a matching cached binary or runs
  scons. The hash is stamped beside the editor and template so a pre-populated
  `bin/` cannot hide an engine edit. Copies, not symlinks: macOS resolves
  `OS::get_executable_path()` through `proc_pidpath()`, so an editor launched
  through a symlink looks for `GodotSharp/` beside the link *target*.
- **The artifact root records paths and materializes no links** — setup writes
  `editor.txt` / `template.txt` / `clone.txt` and the gates read the fork's two
  engine binaries and the `bin/GodotSharp` beside them in place
  (`gates/_godot_fork.sh` owns those names). On stock MSYS/Git-Bash `ln -s`
  **copies** unless the shell has both a winsymlinks mode and the privilege to
  link, and the resulting copy fails either loudly or, worse, quietly.
- **Developer loop** (no editor rebuild in the common case): change dn2cpp
  runtime or transpiler → `dist/package-toolchain.sh --layout-only` → re-export
  in the built fork editor with the setting pointing at the layout dir. Changing
  GodotTools C# → re-run `build_assemblies.py`. Changing engine C++ → scons.

Gates, all in the dn2cpp repo:

- `gates/setup-godot-fork.sh` — sibling of `gates/setup-godot-dotnet.sh`: verify
  fork base-ancestry and the ABI fingerprint, then reuse or rebuild each engine
  artifact from its content provenance; provide editor, glue, assemblies and
  template; install the toolchain bundle
  into `bin/GodotSharp/Dn2Cpp/`; assemble `~/.cache/dn2cpp-godot-fork`. It
  carries **one arm per host OS whose engine it can build** (macOS, Windows),
  refuses an unported host up front rather than deep into a scons run, and
  stages the desktop export template in the shape that host's exporter reads, so
  the E2E gate never depends on the user's installed templates. Host-shaped
  seams live in one file, `gates/_godot_fork.sh`.
- `gates/build-and-run-godot-editor-export.sh` — the desktop E2E gate: package
  the toolchain from the working tree, install into the fork cache, headless
  export of `samples/godot-dotnet/EditorExportSample/` with
  `dotnet/export_backend = dn2cpp`, run the exported game under a watchdog,
  assert a handshake marker and exit 0. Skips green when the fork cache is
  absent; runs in the serial Godot phase. It covers **both** host-compiled
  desktop targets, selecting off `$DN2CPP_OS` the preset, the template artifact
  and the exported layout.
- `gates/build-and-run-godot-editor-export-ios.sh`, `-android.sh`, `-web.sh` —
  the same shape per cross-compiled target, each with its own setup aid
  (`gates/setup-godot-fork-ios.sh`, `gates/setup-android-export.sh`,
  `gates/setup-godot-fork-web.sh`).
- `gates/build-and-run-godot-editor-export-web-hermetic.sh` — the Web export on a
  machine synthesized without any of what the bundle carries: no Emscripten SDK,
  no cmake, no ninja and no node on `PATH`, no `EM_*` in the environment, and the
  bundle's own copies read-only across the export. Only dotnet is left, which is
  the whole claim — everything the export links through comes out of the bundle.
  Every other Web assertion is blind to which SDK and which cmake compiled the
  artifacts, so this is the only one a bundle shipping neither, or a baked cache
  short a variant the export resolves, can fail. It is also the only oracle for
  the agreement between `dist/buildtools-trim.txt` and the fork's
  `Dn2CppToolchain.IsBuildToolsLayout`: an over-trim leaves a staged tree the
  editor refuses, and nothing that reads one tree alone can see it.

## 6. Scope and status

Delivered: the toolchain bundle and its `manifest.json` contract; the fork's
`--bundle-dn2cpp` install path, toolchain resolver and override setting; the
`dotnet/export_backend` option and `Dn2CppExporter`; the headless E2E gates; the
per-axis prebuilt runtime cache; the work-dir layout generation and log
retention; the macOS, Windows, iOS, Android and Web targets; and the
distributable build — the editor `.app` and Web-template release assets and the
release script that publishes them (§11).

Remaining work is tracked in `docs/STATUS.md`, including notarization and the
hardened-runtime entitlements audit for an editor that spawns a host `clang++`
from outside the `.app` and cmake, ninja and Emscripten's clang from inside it.

## 7. Re-pin procedure (when moving the drop-in/fork base)

1. Check out the new commit in the pristine clone and re-run
   `gates/setup-godot-dotnet.sh`. **Nothing has to be deleted by hand**: every
   step of both setup aids is keyed on the tree it built from, so a checkout is
   enough and each product rebuilds itself. Preserve that when adding a step. A
   step keyed on *its product existing* leaves the artifact at the old base
   while the pin file names the new one, and the cache then lies about which
   commit it describes — the one thing the ABI tripwire cannot catch, since it
   fingerprints sources, not binaries.

   | product | key |
   |---------|-----|
   | the clone's editor + release template (`gates/setup-godot-dotnet.sh` steps 1, 4) | the binary's OWN `--version` commit. No stamp: a build cannot forge what it stamps into itself |
   | that clone's glue and assemblies + feed (steps 2, 3) | `clone_tree_hash` (pin plus working-tree dirt), stamped in the gitignored `…/Generated/GeneratedIncludes.props.clone-hash` and `bin/GodotSharp/.clone-hash` |
   | the fork's editor + release template (`gates/setup-godot-fork.sh` steps 1, 5) | `godot_fork_engine_hash`, stamped `<binary>.engine-hash` |
   | the fork's mono glue (step 2) | the same engine hash — the glue is the editor's output |
   | the fork's assemblies + feed + toolchain (step 4) | `godot_fork_tools_hash` over the managed trees `build_assemblies.py` compiles |
   | the fork's desktop export template (step 5) | `<template>.provenance`, `godot_fork_engine_provenance` |
   | the fork's iOS simulator library (`gates/setup-godot-fork-ios.sh` step 3) | `<library>.provenance`, `godot_fork_engine_provenance` plus the complete SCons argument vector |

   The three template zips built from the fork's engine rebuild themselves too
   and need no `FORCE=1` to notice a re-pin: each carries the same
   `<template>.provenance` stamp, the bake's skip reads it, and the consuming
   gates refuse a template stamped with another engine. The iOS aid also keys
   its intermediate simulator library on that engine provenance, so it cannot
   splice a library retained from the preceding pin into the rebuilt zip. They
   are separate aids, so re-run all three after a re-pin:

   ```
   ./gates/setup-godot-fork-web.sh            # web_template.zip
   CRI=1 ./gates/setup-godot-fork-web.sh      # web_template_cri.zip
   ./gates/setup-godot-fork-ios.sh            # ios_template.zip
   ```

   A template baked before that stamp existed is **unstamped**, and unstamped is
   refused rather than warned about (reasoning at `godot_fork_template_check`).
   The same rule governs the two glue stamps and the `.clone-hash` one: the
   first run after a stamp is introduced regenerates rather than skips, whatever
   the pin did. That run costs a glue regeneration and a `build_assemblies.py`
   run — minutes, not the tens of minutes a scons step would be, since the
   binaries answer for themselves and never re-scons.

   `cri_main_libs` needs nothing (a `CRI=1` run re-stages it unconditionally),
   and `bin-cache/` cannot go stale, being keyed on the engine tree hash.
2. Diff all three fingerprinted ABI surfaces (`runtime_interop.cpp` callback
   block, `ManagedCallbacks.cs`, and `NativeFuncs.cs`) against the old pin. If
   unchanged, the fingerprint in `gates/expected/godot-dotnet-abi.sha256`
   stays; if changed, re-audit the emitted `godotsharp_game_main_init` and
   re-freeze.
3. Bump the pin constant in the gates, rebase `dn2cpp/main`, rebuild the fork
   cache, update `gates/expected/godot-fork-pin.txt`, re-run all Godot gates.

### Ordinary fork updates are not a procedure

The list above is for moving the *base*. A GodotTools edit, or a rebase onto the
same base with unchanged engine sources, needs no separate cache procedure:
`gates/pre-merge.sh` asks `godot_fork_cache_fresh` (`gates/_godot_fork.sh`)
before its suites and refreshes the desktop cache and the two Web templates when
a stamp disagrees. An engine edit also makes the iOS template stale. That repair
stays manual because it consumes Xcode and an official templates archive. On a
host with Xcode, pre-merge refuses before its suites and names
`gates/setup-godot-fork-ios.sh` rather than discovering the stale template
inside the iOS gate; on a host without Xcode nothing can be repaired, so the
suites run to completion and the iOS gates are reported red at the verdict.

That predicate is also what makes forgetting *impossible* rather than merely
unnecessary. `godot_fork_preflight` runs it inside every export gate, live and
upstream of `gate_cache_check`, so a stale cache fails there with the
disagreeing stamp named. Without it the gates read `Tools/GodotTools.dll` as
their subject *and* hash that same DLL into their key, so an uncompiled
GodotTools edit moves no key and the suite replays a warm green over the old
exporter.

## 8. Risks

- **Export-time compile latency** — a full runtime plus third_party build is
  minutes; mitigated by the per-axis prebuilt cache (§4) and the persistent
  per-project build dir.
- **.NET SDK still required** on the export machine, for the IL build.
- **Arbitrary game IL** may hit unimplemented dn2cpp surface; the backend is
  opt-in, and error surfacing is a first-class requirement.
- **macOS universal / cross-arch** presets are refused, and a macOS preset that
  selects `arm64` finds no template binary in upstream's archive — it carries
  only `godot_macos_<cfg>.universal`, and the exporter picks by exact name. The
  release therefore ships upstream's own template thinned to arm64
  (`dist/package-macos-template.sh`). Worse, `binary_format/architecture` is
  `PROPERTY_USAGE_STORAGE` on macOS alone, so it is settable only by editing
  `export_presets.cfg`. lipo of two dn2cpp builds into a universal game remains
  future work.
- **No Linux editor is packaged.** The backend serves Linux as host and as
  target, and `gates/setup-godot-fork.sh` builds a Linux fork editor, but `dist/`
  has no `package-editor-linux.sh` — a Linux user builds the fork themselves.
- **macOS hardened runtime** — the shipped `.app` is ad-hoc signed with the
  hardened runtime *off*, so a first launch needs the quarantine attribute
  cleared. Notarizing means turning it on, and that is the point at which the
  fork's `editor.entitlements` has to be re-audited against an editor that spawns
  a host `clang++` from outside the bundle and cmake, ninja and Emscripten's
  clang from inside it. The inside half is the easier one for Gatekeeper and the
  stricter one for the packager: a child of the `.app` needs no
  `disable-library-validation` for its own sake, but every Mach-O the bundle
  carries is part of the notarized payload and needs the same identity and
  `--options runtime`.

## 9. Known pitfall: an export can silently drop C# [Signal] scene connections

**The failure.** An export drops every scene connection whose signal is
declared as a C# `[Signal]`. No export error, no warning; the shipped game never
fires that callback. Connections from native engine signals (`timeout`,
`body_entered`, …) survive, which makes the loss easy to misread as a runtime
dispatch bug.

**The cause.** With `editor/export/convert_text_resources_to_binary` on (the
default), the TSCN-to-SCN conversion is not a data-level transform: the platform
exporter **instantiates every scene in the editor and re-packs it**
(`EditorExportPlatform::_export_customize` in
`editor/export/editor_export_platform.cpp`). Re-packing collects the *live*
connections, and a connection is live only if `Object::connect` succeeded at
instantiation — which for a C# `[Signal]` requires the editor's .NET side to
have the **game assembly loaded**, since the script's signal list comes from
`ScriptManagerBridge`. An editor that never loaded it cannot resolve the signal,
the connect fails, and the re-packed `.scn` ships without the connection.

**When it bites.** Nothing on the export path builds. `EditorNode::call_build()`
is reached only from `EditorRunBar` (Play), the Build button calls
`BuildManager.BuildProjectBlocking` directly, and `editor_export_platform.cpp`
has no build hook and no headless/GUI branch. So a **GUI** export bites too, the
first time a freshly opened project is exported before ever pressing Play or
Build — not just a headless/CI export on a checkout with no
`.godot/mono/temp/bin/Debug`.

**The trap that survives a rebuild.** `_export_customize()` caches its converted
scene on disk under `res://.godot/exported/<config-hash>/`, keyed on the
`.tscn`'s mtime with an MD5 of the `.tscn` + `.import` as the fallback. The key
says nothing about whether the assembly was loaded, so building and exporting
again **reuses the cached broken `.scn`** — the scene is never re-instantiated,
and the failure looks immune to an otherwise-correct rebuild.

**Remedies, in order of preference:**

1. Run `dotnet build <project>.csproj` in Debug — the editor's expected config —
   **before the first export of a session** (Play once also loads it). The
   editor then loads the assembly from `.godot/mono/temp/bin/Debug`, resolves
   the `[Signal]`s, and the connections survive conversion.
2. If a broken conversion is already cached — a rebuild and re-export changed
   nothing — delete `<project>/.godot/exported/` and export again. Touching the
   `.tscn` is not enough: the mtime miss falls through to the MD5, which still
   matches.
3. Set `editor/export/convert_text_resources_to_binary=false` in
   `project.godot`. Scenes ship as text and are never instantiate-re-packed, at
   the cost of pck size and text-parse load time. This is the only option for an
   arrangement that cannot build the assembly at all.

**Gate blind spot.** No editor-export gate catches a regression of this class:
`EditorExportSample`'s `main.tscn` has a single node and no editor-wired C#
`[Signal]` connection, and the gates run a bare `--import` / `--export-release`
with no Debug build. Closing it means adding a second scripted node declaring a
`[Signal]`, wiring it to a root handler through the editor, having the handler
flip a flag the probe prints as a marker, adding a `dotnet build -c Debug`
before the import, and asserting the marker. The sample is shared by the macOS,
iOS and Android export gates, so any such closure must be re-verified there too.

## 10. The Web lane (WebAssembly / Emscripten)

Godot 4 does not support C# on the Web at all: a mono-enabled editor refuses
every Web export outright. dn2cpp closes that hole with the same drop-in the
other platforms use.

### The load path — and why it needs no engine change

1. The game is transpiled to C++ and compiled by Emscripten into a WebAssembly
   **side module**, staged as **`<Assembly>.so`** (no `lib` prefix).
2. The C# export plugin hands it to the platform exporter with
   `AddSharedObject`. The Web exporter copies every shared object **flat beside
   `index.html`** — it takes `.get_file()`, so the
   `data_<proj>_<platform>_<arch>` sub-directory every other platform uses is
   ignored here — and lists it in the generated HTML's config under
   `gdextensionLibs`.
3. `platform/web/js/engine/config.js` turns that list into Emscripten's
   `Module.dynamicLibraries`, which the loader preloads **before `main()` runs**.
4. At startup `GDMono` finds no hostfxr and no coreclr and falls through to
   `try_load_native_aot_library()`, which compiles for the Web unchanged because
   the platform defines `UNIX_ENABLED`, asking for
   `<api_assemblies_dir>/<Assembly>.so`. `OS_Web::open_dynamic_library()`
   **strips the directory** and `dlopen`s the bare file name, already in the
   loaded-library registry.

So **the staged file name is the entire contract**, and the assemblies directory
the other platforms need never has to exist.

### What the fork must change (three engine files, none of them the ABI)

| file | change | why |
|---|---|---|
| `platform/web/detect.py` | `get_flags()` gains `"supported": ["mono"]` | Without it `modules/mono/config.py` exits outright. The C# module had never been compiled for wasm32. |
| `platform/web/export/export_plugin.cpp` | the `#ifdef MODULE_MONO_ENABLED` refusal in `has_valid_export_configuration` becomes a warning | It refused *every* Web export from a mono editor, a GDScript project included. Editor-only validation; not in the template binary. |
| `modules/mono/mono_gd/gd_mono.cpp` | the assemblies-dir existence check carves out `WEB_ENABLED` | That directory cannot exist on the Web (above), and the check runs *before* the AOT fallback, so it fails a perfectly loadable game. |

None of these touch the ABI no-touch list (§1), and the Web gate re-asserts the
fingerprints — this being the only lane that changes engine C++ makes that
assertion more important here, not less.

### The template must be baked, and three of its flags are not negotiable

Upstream ships no C#-capable Web template and cannot, so
`gates/setup-godot-fork-web.sh` bakes one from the fork:

- **`dlink_enabled=yes`** — the game is a side module; a stock Web build's
  `dlopen` is an Emscripten stub returning `NULL`. (With dlink the engine is
  itself a side module, `godot.side.wasm`, under a thin `MAIN_MODULE` runtime.)
- **`threads=no`** — the drop-in is built without `-pthread`, and Emscripten
  refuses to load a non-pthread side module into a pthread main module.
- **`disable_exceptions=no` plus `-fwasm-exceptions`** — a .NET-semantics
  runtime cannot drop C++ exceptions; that is how a managed `throw` is
  implemented. Under Emscripten dynamic linking the exception **tag** is defined
  and exported by the MAIN module and *imported* by every side module, so the
  engine must carry it or the drop-in cannot be instantiated. Godot's default is
  `-fno-exceptions`, which defines no tag.

The setup script asserts each of these on the produced zip rather than trusting
the command line, and stamps the `emcc` version beside it: a side module and the
main module must agree on the EH flavour, `WASM_BIGINT` and the dylink ABI, and
nothing else pins that. The stamp is written by the **build** and copied at
publish, never re-derived from the running shell, and is read back as a skip
condition so an unstamped or differently stamped zip is relinked.

### Carve-outs (state them; do not discover them)

- **The GC is real.** The Web build runs the same vendored Boehm GC as native,
  default-on. Conservative scanning is sound on wasm through Binaryen's
  SpillPointers link pass (`-sBINARYEN_EXTRA_PASSES=--spill-pointers`), which
  spills pointer-typed wasm locals to linear memory; static-data roots come from
  the emitter-stamped `dn2cpp_roots` section, since bdwgc's EMSCRIPTEN arm
  registers an empty static-data range. The collector is single-threaded (no
  `GC_THREADS`), incremental mode is forced off (no page-protection VDB), and
  finalizers drain manually (no finalizer thread). `DN2CPP_NO_GC=1` and
  `-DDN2CPP_USE_GC=OFF` remain the calloc escape hatch.
- **There are no threads.** `Task.Run`, `Thread` and `Timer` throw.
- **`System.Net.Http` has no transport.** A browser has no TCP socket layer, so
  `DN2CPP_USE_CURL` — on by default everywhere else — is forced off on this arm
  whatever the caller passed. `HttpClient` still links: the Emscripten fallback
  arm of `runtime/core/intrinsics/dn2cpp_http2_stream.cpp` defines the whole
  `dn2cpp_http2_call_*` surface and fails every call with a message naming the
  platform, surfacing as a catchable `HttpRequestException`, so one feature
  degrades instead of the export being unbuildable. The thread carve-out is hit
  first for an async send, so a Web build uses the synchronous `Send`.
- **The filesystem is MEMFS, so files are ephemeral.** `FileStream`,
  `SafeFileHandle` and the `File.*` surface all work, but what a page writes
  lives in that page's memory and is gone when it unloads. Persistence on the
  Web is the engine's job (`user://` through `FileAccess`), not `System.IO`'s.
- **The `libSystem.Native` surface this target defines is hand-picked**, and the
  omissions do not behave like the two above. Lowering a P/Invoke is
  target-neutral, so an omitted PAL symbol is still called; on a side module that
  is a wasm IMPORT rather than a link error, and it throws
  `TypeError: resolved is not a function` at the first call, naming a function
  index. Nothing upstream sees it — which is why the two Web export gates assert
  the drop-in's whole import closure against the exported main module and glue,
  and why `gates/build-and-run-wasm-console.sh` carries the clock section: that
  axis links an executable, where the same gap is a named wasm-ld failure.
- The publish uses the **host RID**, never `browser-wasm`: dn2cpp consumes only
  IL, and a `browser-wasm` publish would demand the `wasm-tools` workload and a
  Mono-flavoured CoreLib, changing the very IL we transpile. The RID is a
  publish-only key; every name the engine reads is built from `arch` (`wasm32`).

### The size levers this lane turns on — and the one it deliberately does not

`Dn2CppExporter.BuildDropIn` adds exactly two flags for Web:
`--trim-reflection` and `--trim-godot-classes`. Both target
`__wasm_apply_data_relocs`, the single function wasm-ld emits with one store per
pointer in static data, against V8's compiled-in 7,654,321-byte per-function
ceiling. That is not an optimization: over the ceiling the module does not
instantiate at all.

The manifest-resource trim (`--no-manifest-resources <Assembly>` /
`--manifest-resource-root <name>`) is **not** among them, for three measured
reasons. It pulls on the wrong budget: a resource blob is emitted
relocation-free as `static const uint8_t[]` and only the table's rows carry
pointers, so what it buys is download size. This lane carries no resource bytes
to shed: blobs are emitted only when an emitted body lowers a manifest read
(`Compilation.ManifestResourcesUsed`) and the `--dotnet-module` closure over the
real GodotSharp lowers none, asserted by
`gates/build-and-run-godot-dotnet-wasm.sh`. And the safety half is clean, since
dn2cpp intercepts `System.SR` and folds BCL fault text in at transpile time
rather than reading CoreLib's `Strings.resources`.

A game that *does* read manifest resources opts in with no code change: the
exporter appends the `dotnet/dn2cpp/extra_transpile_args` project setting
verbatim, so the drop and its `--manifest-resource-root` keep-list live in
`project.godot`, versioned with the game. The keep-set itself stays a **global
bit**, so a single read anywhere carries every loaded module's blobs; a static
per-assembly narrowing was measured and rejected, since no site presents a
constant receiver (`IEvalStack.Push` spills every value to a temporary, so the
analysis returns "all").

### Upgrading a project that exported for Web before the GC change

The exporter no longer forces `-DDN2CPP_USE_GC=OFF` for Web; Web takes the CMake
default (`ON`) like every other platform. But the exporter reuses a **persistent
CMake build directory**, and CMake never resets a cached variable to its
default: a project that exported for Web before this change carries
`DN2CPP_USE_GC:BOOL=OFF` in that directory's `CMakeCache.txt` forever and keeps
building the calloc drop-in with no error. The one-time remedy is deleting
`.godot/mono/dn2cpp/build/`. A fresh export needs nothing.

The same shape applies to `DN2CPP_USE_CURL` on **every** platform: it defaulted
`OFF` until the HTTPS work landed, so an older build directory keeps building a
curl-less runtime unless the configure passes the option explicitly. Same
remedy, but this one is loud — an export whose game touches `HttpClient` fails
at link naming `dn2cpp_http2_call_open` rather than shipping.

## 11. Distributing the fork

This section is why the pieces are shaped as they are; the order to run them in,
host by host, is `docs/RELEASE.md` (`docs/RELEASE.ja.md` translates it) and is
not repeated here. What a *downloader* does with the result is
`docs/EDITOR-GUIDE.ja.md`, which the release notes link at a fixed commit rather
than restating each time.

A release carries one asset per row below, each with its own packaging script and
a `*.metadata` file beside it:

| asset | script |
|-------|--------|
| the macOS editor `.app`, zipped | `dist/package-editor-macos.sh` |
| the Windows editor (x86_64), zipped | `dist/package-editor-windows.sh` |
| the Web export template, zipped | `dist/package-web-template.sh` |
| the macOS arm64 export template, zipped | `dist/package-macos-template.sh` |

`dist/release-github.sh` turns a finished asset directory into an annotated tag
and a draft GitHub release, rendering `dist/release-notes-template.md` from that
metadata. Everything it publishes is checked against the tree first — the fork
commit the editor was built from, its ancestry on the remote branch, the same
ancestry for the `dn2cpp_commit` the notes link a repository beside, the hashes,
and that the assets name one engine and one dn2cpp. That last one is a demand on
the packaging hosts rather than a discovery: each names the commit it is cutting
from (`--dn2cpp-commit`) and refuses a tree that is not it.

An editor can only be packaged on the platform it runs on, so the lane set is an
argument (`--lane`) rather than a constant: a host cuts the lanes it can bake and
declares the rest with `--uploaded-lane`, re-entering the same script against the
same tag. **Declaring a lane uploaded subtracts the upload and the demand that
its asset sit in the asset directory — it subtracts no verification.** The bytes
are checked against the digest GitHub is serving them under, and the lane's
metadata value by value against the notes already published: once the file is on
another machine those two are the only witnesses left for a hash, a `--version`
string or a toolchain content hash.

- **The macOS export template is upstream's binary, not a fork build.** Export
  templates do not compile `editor/`, and with `WEB_ENABLED` undefined the
  remaining fork engine sources reduce to the pinned base, so a macOS template
  compiled from either is the same program — and the packaging script *checks*
  that reduction, plus the template's own
  `<base>.stable.mono.official.<commit>` self-report, rather than resting on it.
  The gates keep baking their own, because a fork-baked template is what
  `godot_fork_template_check`'s provenance stamp can speak for.
- **The GodotSharp payload lives in `Contents/Resources/GodotSharp`, and
  `Contents/MacOS/GodotSharp` is a relative symlink to it.** `codesign` treats
  everything under `Contents/MacOS/` as nested code and refuses to seal a bundle
  holding a managed `.dll` there; signing the assemblies individually would
  append bytes to them, and `--deep` signing is deprecated. The symlink leaves
  both existing resolvers — `godotsharp_dirs.cpp`'s `exe_dir/GodotSharp` and the
  gates' `FORK_GODOTSHARP` — reading the payload where they already look, which
  is what lets `--smoke` drive the editor-export gates against the assembled
  `.app` unmodified.
- **Signing is ad-hoc, no hardened runtime, no notarization**: nested Mach-O
  images first, the bundle last; `--deep` is for verification only. Strip
  extended attributes (`com.apple.provenance`) and `.DS_Store` / `._*` residue
  first, or the seal fails on them.
- **The smoke runs before signing**, because staging the toolchain writes inside
  the bundle and would break a seal already applied.
- **Archive with `ditto -c -k --sequesterRsrc --keepParent`.** `zip` drops
  permissions, extended attributes and symlinks, and the signature then fails to
  verify after extraction — so the run unpacks its own zip and re-verifies.
- **The Windows package puts `GodotSharp/` beside the `.exe` as a real tree, and
  holds no link at all.** Nothing here forces the macOS indirection, and MSYS
  `ln -s` silently *copies* without a `winsymlinks` mode and the privilege to
  create one — a copy is a snapshot of the tree the staging step rewrites.
- **The Windows archiver is Python's `zipfile`.** Neither `zip` nor `7z` can be
  assumed on a Windows host, and PowerShell 5.1's `Compress-Archive` writes `\`
  as the entry separator, which other extractors read as one file name. Sorting
  the walk and fixing the timestamps also makes the asset a pure function of the
  staged tree — one input tree, one sha256, which `ditto` does not give.
- **The Windows editor is unsigned**, so SmartScreen reports an unknown
  publisher. `SHA256SUMS.txt` and the package's own `RELEASE.txt` are the
  identity it ships with instead.
- **`RELEASE.txt` is where the `.app` has its `DN2CPP*` `Info.plist` keys.** The
  PE VERSIONINFO stays as the linker wrote it: the editor's `.engine-hash` stamp
  is taken over the bytes in the fork's `bin/`, and rewriting a resource would
  make the shipped image differ from the one that stamp describes.
- **`.console.exe` is renamed together with the `.exe`, never apart.**
  `console_wrapper_windows.cpp` finds the GUI half by rewriting that suffix on
  its *own* module file name, so the pair follows any name both halves share.
- **The Windows round-trip compares the sha256 of every file in the unpacked
  archive against the staged tree.** It has no counterpart to the macOS run's
  "did `+x` survive" check: Windows takes executability from the extension.
- **A Windows layout carries `ref-posix/`, and the package checks for it.** Every
  cross-compiled export from a Windows host — Android, Web — is transpiled
  against the POSIX flavour of the CoreLib closure, which no macOS layout needs.
- **The engine fingerprint must not depend on the host that measured it.** One
  release names one engine tree, so the untracked leg of
  `godot_fork_engine_hash` hashes file by file: fed through `xargs`, an empty
  list still runs `shasum` on GNU and not on BSD, and one tree would then be able
  to produce two provenance strings.
- **Ship the Web template from the fork's artifact root, never its `bin/`**,
  where the stock and CRI flavours share one file name and the wrong one goes out
  silently. `godot_fork_web_template_flavor` (`gates/_godot_fork.sh`) probes the
  archive for which it is, and `godot_fork_web_template_assert` checks the
  non-negotiable flags.
- **Cut the Web template before either editor.** The template is the wasm main
  module and the drop-in the editor's bundled SDK compiles is a side module of
  it, so `dist/package-editor-macos.sh` refuses a bundle whose `emcc_version` is
  not the one the template is stamped with — re-bake with
  `FORCE=1 gates/setup-godot-fork-web.sh` and re-cut when they disagree.
- **Web and Windows need fork-built templates.** Web enables the mono module and
  dynamic loader for wasm32. Windows passes the data-directory search request
  at the NativeAOT load call. Other desktop and mobile exports can use stock
  Godot .NET templates of the same version, though the setup cache still stamps
  every selected template against the fork's engine provenance.
- **A release is identified by the tag, the fork commit, the engine provenance
  and the dn2cpp commit — not by the toolchain's `content_hash`**, which moves
  between runs because the prebuilt rebuild is not bit-reproducible.

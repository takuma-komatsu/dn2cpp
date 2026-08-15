using Dn2Cpp;
using Dn2Cpp.DotnetModule;
using Dn2Cpp.Godot;

bool generateBindings = false;
bool checkWasmImports = false;
string checkWasmSide = "";
string checkWasmMain = "";
string? checkWasmGlue = null;
bool printRuntimeDir = false;
string extensionApiPath = "";
string input = "";
string outDir = ".";
bool gdExtension = false;
bool dotnetModule = false;
bool measure = false;
bool verbose = false;
bool autoRef = false;
bool sharedGenerics = true;
bool hotupdateBase = false;
bool emitPatch = false;
string baseAbiPath = "";
uint patchVersion = 0;
bool patchRegCode = true;
List<string>? hotupdateRefs = null;
var references = new List<string>();
var noDefaultRefs = new List<string>();
var noAdoptAsync = new List<string>();
var cutMethods = new List<string>();
bool trimReflection = false;
var reflectionRoots = new List<string>();
var noManifestResources = new List<string>();
var manifestResourceRoots = new List<string>();
var pinvokeModules = new List<string>();
bool trimGodotClasses = false;
var godotClassRoots = new List<string>();
bool shadowStack = false;
string godotApiPath = "";

for (int i = 0; i < args.Length; i++)
{
    if (args[i] == "--generate-bindings" && i + 1 < args.Length)
    {
        generateBindings = true;
        extensionApiPath = args[++i];
    }
    else if (args[i] == "--check-wasm-imports")
    {
        // Wasm side-module import-closure check (WasmSymbols): report every SIDE
        // import neither MAIN nor its JS glue can satisfy, before emscripten's
        // lazy stub turns it into an unnamed TypeError at first call. Exit 0 =
        // closed; 3 = unsatisfied symbols found (one per line on stdout, in
        // import-section order); 2 = unreadable input or a glue lacking
        // wasmImports (an `error:` line on stderr).
        checkWasmImports = true;
        if (i + 2 >= args.Length)
        {
            Console.Error.WriteLine("error: --check-wasm-imports expects <side.wasm> <main.wasm> [<main.js>]");
            return 2;
        }
        checkWasmSide = args[++i];
        checkWasmMain = args[++i];
        if (i + 1 < args.Length)
            checkWasmGlue = args[++i];
    }
    else if (args[i] == "-o" && i + 1 < args.Length)
    {
        outDir = args[++i];
    }
    else if (args[i] == "-r" && i + 1 < args.Length)
    {
        references.Add(args[++i]);
    }
    else if (args[i] == "--no-default-ref" && i + 1 < args.Length)
    {
        // Suppress one conditional default reference — a shim assembly shipped beside
        // this CLI that would otherwise be injected because the BCL assembly it serves
        // is in the load set (Compilation.InjectDefaultRefs). Repeatable, one shim per
        // occurrence; there is deliberately no blanket form, so a shim added to the
        // table later injects and forces a decision instead of being switched off by a
        // flag somebody wrote years earlier. A FLAG, never an environment variable: it
        // changes the emitted C++ (self-host fixpoint discipline). Unknown names are
        // rejected in TranspileDriver.Run, the point both CLIs share.
        noDefaultRefs.Add(args[++i]);
    }
    else if (args[i] == "--gdextension")
    {
        gdExtension = true;
    }
    else if (args[i] == "--dotnet-module")
    {
        dotnetModule = true;
    }
    else if (args[i] == "--godot-api" && i + 1 < args.Length)
    {
        // The extension_api.json driving the GDExtension call map and class
        // tables. A FLAG because it changes the C++ a successful transpile
        // emits (the flag-not-env doctrine); DN2CPP_GODOT_API remains the
        // documented fallback for callers that cannot pass a flag, and the
        // default stays extension_api.json in the working directory
        // (resolution order in GodotBackend).
        godotApiPath = args[++i];
    }
    else if (args[i] == "--measure")
    {
        // Self-hosting feasibility harness: transpile the input and report every
        // reachable unsupported-IL / unmapped-intrinsic gap (instead of stopping at
        // the first), rather than emitting C++. See gates/selfhost-measure.sh.
        measure = true;
    }
    else if (args[i] == "--verbose")
    {
        // Expand the end-of-run reports that print a count by default into their
        // per-item detail. A flag rather than an env var for consistency; it
        // changes no emitted byte.
        verbose = true;
    }
    else if (args[i] == "--hotupdate-base")
    {
        // Hot-update base build: emit the ABI-contract hash constant into the
        // base binary and write the base-abi.json sidecar (docs/BPI-FORMAT.md).
        hotupdateBase = true;
    }
    else if (args[i] == "--emit-patch" && i + 1 < args.Length)
    {
        // Hot-update converter mode: bake the given patch assembly into a
        // Baked Patch Image instead of transpiling to C++ (docs/BPI-FORMAT.md).
        emitPatch = true;
        input = args[++i];
    }
    else if (args[i] == "--base-abi" && i + 1 < args.Length)
    {
        // The base build's ABI sidecar the converter stamps into the BPI.
        baseAbiPath = args[++i];
    }
    else if (args[i] == "--patch-version" && i + 1 < args.Length)
    {
        // Deployment ordering key stamped into the BPI header; a directory
        // load applies BPIs in ascending patchVersion order (0 = unversioned).
        // Parsed via int (a version number is small and non-negative) so the CLI
        // stays inside the self-host transpilable subset, and via TryParse (the
        // --max-heap-mb form) so a malformed value is a one-line error rather
        // than a raw FormatException.
        if (int.TryParse(args[++i], out int pv) && pv >= 0)
            patchVersion = (uint)pv;
        else
        {
            Console.Error.WriteLine($"error: --patch-version expects a non-negative integer, got '{args[i]}'");
            return 1;
        }
    }
    else if (args[i] == "--patch-regcode")
    {
        // Bake the BPI in the register code format (Header.flags bit0): same
        // abstract simulation as the stack encoding, the eval temp at depth d
        // becomes register argCount+localCount+d (docs/BPI-FORMAT.md "Register
        // code format"). This is the default; the flag makes it explicit.
        patchRegCode = true;
    }
    else if (args[i] == "--patch-stackcode")
    {
        // Bake the BPI in the v1 stack-machine code format (Header.flags
        // bit0 clear) instead of the default register encoding.
        patchRegCode = false;
    }
    else if (args[i] == "--hotupdate-refs" && i + 1 < args.Length)
    {
        // Hot-update base build: a text file of extra patch-bindable roots
        // (one per line, '#' comments allowed) to force-emit even when the
        // base program never uses them: closed generic instantiations
        // ("OpenDef[arg,...]" — HybridCLR's AOTGenericReferences), generic
        // method instantiations ("DeclType::Method[arg,...]"), and plain
        // methods ("DeclType::Method", e.g. an engine-shim method only patch
        // code calls). docs/BPI-FORMAT.md.
        // ReadAllText + Split (not ReadAllLines): the intercepted ReadAllText is
        // in the self-host transpilable subset, ReadAllLines' StreamReader path is
        // not. The Where below drops empty lines (incl. the trailing newline).
        hotupdateRefs = File.ReadAllText(args[++i]).Split('\n')
            .Select(l => l.Trim())
            .Where(l => l.Length > 0 && !l.StartsWith('#'))
            .ToList();
    }
    else if (args[i] == "--print-runtime-dir")
    {
        // Print where the C++ runtime sources live and exit. An installed tool
        // finds them beside the CLI (the package lays runtime/ and third_party/
        // out as siblings there); a repo-checkout or relocated CLI finds the
        // checkout's runtime/ by walking up from the executable directory.
        printRuntimeDir = true;
    }
    else if (args[i] == "--auto-ref")
    {
        // Eagerly load the transitive shared-framework reference closure before Build()
        // (from the passed CoreLib's directory) so external generic instantiations
        // resolve, instead of requiring every transitive dependency via explicit -r.
        // Opt-in while drift vs. the explicit-ref output is being measured.
        autoRef = true;
    }
    else if (args[i] == "--no-adopt-async" && i + 1 < args.Length)
    {
        // Opt OUT of custom-async-task adoption for one assembly (simple name,
        // repeatable): its [AsyncMethodBuilder] task types are NOT mapped onto the
        // intrinsic Task family — the library's real IL transpiles through the
        // general pipeline instead, making its whole API surface work, not just the
        // await plumbing. Adoption stays the default because `-r SomeLib.dll` must
        // never explode on task types the program does not touch. A name matching no
        // loaded module is a hard error: a typo falling back to adoption fails later
        // with the confusing "X::Member has no intrinsic mapping".
        noAdoptAsync.Add(args[++i]);
    }
    else if (args[i] == "--cut" && i + 1 < args.Length)
    {
        // Cut one method by name ("DeclType::Method", repeatable): its body is
        // never transpiled — the subtree only it reaches falls out of the tree —
        // and every call site is neutralized to the default result. The same
        // semantics as the backend bounded sets (docs/ARCHITECTURE.md §2), as a
        // per-run CLI lever: the carve-out a library-specific cut needs (e.g.
        // GDTask's TaskTracker -> StackTrace subtree) without baking library
        // names into dn2cpp. A spec resolving to no loaded method is a hard
        // error — a typo silently becoming a no-op cut is a footgun.
        cutMethods.Add(args[++i]);
    }
    else if (args[i] == "--trim-reflection")
    {
        // Strip the reflection member metadata (field / method / property tables, and
        // everything only they name: accessor thunks, invoker thunks, parameter and
        // generic-argument pools, attribute factories) of every class the program cannot
        // plausibly reflect over — everything outside the app module that is neither named
        // by a type token, nor a delegate, nor on a kept class's base chain, nor a
        // --reflection-root. Reading a stripped type's members at run time throws a
        // catchable PlatformNotSupportedException naming the type and this flag; it never
        // answers an empty member list. Constructors are NOT stripped (Type.GetConstructor
        // over a run-time-chosen type is a live cross-assembly path), and neither is any
        // of the rest of a type: name, base, interfaces, layout, vtable, Type object, enum
        // members and the ToString/Equals/GetHashCode/Finalize slots all stay.
        //
        // Off by default, and a FLAG rather than an environment variable: it changes the
        // C++ a successful transpile emits, and the self-host fixpoint requires that no
        // environment variable can do that.
        trimReflection = true;
    }
    else if (args[i] == "--reflection-root" && i + 1 < args.Length)
    {
        // Keep one type's reflection metadata under --trim-reflection (repeatable). Named by
        // its full name ("MyGame.Save.Record"), or — for a generic — by its arity-stripped
        // definition name ("System.Collections.Generic.List"), which covers every
        // instantiation at once. A root matching no loaded type is a hard error: a typo
        // silently becoming a no-op root would surface as a PlatformNotSupportedException
        // in a shipped game, where the diagnostic reaches nobody.
        reflectionRoots.Add(args[++i]);
    }
    else if (args[i] == "--no-manifest-resources" && i + 1 < args.Length)
    {
        // Drop one assembly's embedded manifest resources from the image (repeatable,
        // named by assembly simple name). The dropped assembly's registry row keeps a
        // "resources dropped" bit, so a run-time query that misses throws a catchable
        // PlatformNotSupportedException instead of the null that means "no such
        // resource" in real .NET. A name matching no loaded assembly is a hard error:
        // a typo silently becoming a no-op drop would keep shipping the very bytes
        // this flag sheds. A FLAG, never an environment variable: it changes the C++
        // a successful transpile emits (self-host fixpoint discipline).
        noManifestResources.Add(args[++i]);
    }
    else if (args[i] == "--manifest-resource-root" && i + 1 < args.Length)
    {
        // Keep one resource under --no-manifest-resources (repeatable), named by its
        // exact manifest name ("System.Private.CoreLib.Strings.resources") — the
        // escape hatch for the one resource a program really reads out of an assembly
        // whose other blobs it wants shed. A root matching no embedded resource of any
        // dropped assembly is a hard error, for the --reflection-root reason.
        manifestResourceRoots.Add(args[++i]);
    }
    else if (args[i] == "--pinvoke-module" && i + 1 < args.Length)
    {
        // Lower one named [DllImport] module's P/Invokes to direct native calls from
        // any loaded assembly (repeatable) — the opt-in for an external binding
        // library pulled in with -r, whose imports otherwise stay on the
        // intrinsic/throw boundary. The name is matched by ordinal equality against
        // the exact [DllImport] module string (no normalization), and the module
        // reaches the pinvoke-libs.txt link manifest like an app-module import.
        // A FLAG, not an env var: it changes the C++ a successful transpile emits.
        pinvokeModules.Add(args[++i]);
    }
    else if (args[i] == "--trim-godot-classes")
    {
        // Allowlist-trim the GodotSharp engine-wrapper surface (--dotnet-module lane
        // only). Godot.Constructors..cctor registers one "native class name ->
        // allocate managed wrapper" lambda per engine class, and those ldftn edges
        // root every engine wrapper the game never touches. Under this flag a lambda
        // is reached only when the program NAMES its wrapper class outside GodotSharp
        // itself (new X(), typeof/cast/is, a generic argument, a signature, a base
        // chain, or a --godot-class-root); every other lambda is redirected to the
        // nearest released ancestor's, so the registry keeps every key and an
        // unreleased class's engine object is wrapped as that ancestor. Correct for
        // every type test the program can express (testable types are named types);
        // the observable residue is GetType().Name-style string reflection over
        // never-named wrappers.
        //
        // Off by default, and a FLAG rather than an environment variable: it changes
        // the C++ a successful transpile emits.
        trimGodotClasses = true;
    }
    else if (args[i] == "--godot-class-root" && i + 1 < args.Length)
    {
        // Keep one engine wrapper's registry lambda under --trim-godot-classes
        // (repeatable), named by its GodotSharp full name ("Godot.Timer") — the
        // escape hatch for a class only ever named from data (scenes/GDScript).
        // A root matching no loaded engine wrapper is a hard error: a typo becoming
        // a no-op root would surface as an ancestor-typed wrapper in a shipped game.
        godotClassRoots.Add(args[++i]);
    }
    else if (args[i] == "--max-heap-mb" && i + 1 < args.Length)
    {
        // A ceiling on the transpiler's own managed heap: past it the transpile
        // fails with an actionable error naming the phase, instead of taking the
        // machine into swap. Off unless asked for; DN2CPP_MAX_HEAP_MB is the same
        // lever for callers that cannot pass a flag (this wins over it).
        if (int.TryParse(args[++i], out int maxHeapMb) && maxHeapMb > 0)
            MemoryGuard.LimitBytes = (long)maxHeapMb * 1024 * 1024;
        else
        {
            Console.Error.WriteLine($"error: --max-heap-mb expects a positive integer, got '{args[i]}'");
            return 1;
        }
    }
    else if (args[i] == "--shared-generics")
    {
        // Canonical shared generics: group generic instantiations whose C++
        // layout coincides (enum / sub-8-byte integer args -> width-preserving
        // placeholders, reference args -> the single CnRef placeholder) under
        // a canonical owner instantiation, emit the owner's struct layout and
        // method bodies once per group, and bind each grouped instantiation's
        // calls/vtables to them. Bodies that would bake the instantiation's
        // identity read it from an rgctx table at run time, or fall back to
        // per-instantiation compilation. This is the default; the flag is
        // accepted as a no-op for compatibility with earlier opt-in usage.
        sharedGenerics = true;
    }
    else if (args[i] == "--no-shared-generics")
    {
        // Escape hatch: compile every generic instantiation monomorphically
        // (the pre-sharing behavior, byte-identical output).
        sharedGenerics = false;
    }
    else if (args[i] == "--shadow-stack")
    {
        // Opt-in shadow stack: plant a Dn2CppShadowFrame RAII guard in every
        // compiled body's prologue, buying exact throw traces on every platform —
        // emitter-stamped frame names that survive -O2 inlining and exist on WASM,
        // where the PAL backtrace reports nothing — for a per-call push/pop cost.
        // Off by default, and a FLAG rather than an environment variable: it changes
        // the C++ a successful transpile emits.
        shadowStack = true;
    }
    else if (args[i].StartsWith('-'))
    {
        Console.Error.WriteLine($"error: unrecognized option '{args[i]}'");
        return 1;
    }
    else if (string.IsNullOrEmpty(input))
    {
        input = args[i];
    }
    else
    {
        Console.Error.WriteLine($"error: unexpected argument '{args[i]}' (input already set to '{input}')");
        return 1;
    }
}

if (printRuntimeDir)
{
    // Walk up from the executable directory to the nearest runtime/CMakeLists.txt.
    // Installed tool: found immediately (tools/net10.0/any/runtime). Repo checkout
    // (bin/<cfg>/net10.0) or the self-hosted binary under artifacts/: found at the
    // repository root. The result is the -S argument for the native cmake build.
    string? probe = AppContext.BaseDirectory;
    while (!string.IsNullOrEmpty(probe))
    {
        string runtimeDir = Path.Combine(probe, "runtime");
        if (File.Exists(Path.Combine(runtimeDir, "CMakeLists.txt")))
        {
            Console.WriteLine(Path.GetFullPath(runtimeDir));
            return 0;
        }
        // GetDirectoryName alone walks up: the first step merely drops
        // BaseDirectory's trailing separator (re-probing the same level once,
        // harmlessly), each later step ascends, and the root yields null.
        // Deliberately no TrimEnd — it is outside the self-host subset.
        probe = Path.GetDirectoryName(probe);
    }
    Console.Error.WriteLine($"error: no runtime/CMakeLists.txt found at or above {AppContext.BaseDirectory}");
    return 1;
}

if (checkWasmImports)
{
    try
    {
        return WasmSymbols.PrintUnsatisfied(checkWasmSide, checkWasmMain, checkWasmGlue) == 0 ? 0 : 3;
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"error: {ex.Message}");
        return 2;
    }
}

// --generate-bindings already takes the API dump as its own argument, so a
// --godot-api riding along has nothing to name — and this block returns before
// the transpile-path checks below, so without this guard the flag would be
// silently ignored (the said-out-loud policy of the checks below).
if (generateBindings && !string.IsNullOrEmpty(godotApiPath))
{
    Console.Error.WriteLine("error: --godot-api is meaningless with --generate-bindings, which takes the extension_api.json as its own argument");
    return 1;
}
if (generateBindings)
{
    try
    {
        // Optional positional arg overrides the shims output path. The default is
        // the canonical shared-shim location (src/GodotSharpShim); both godot
        // samples consume it via a GodotSharp.csproj ProjectReference. (Writing into
        // the GodotSample dir would let the sample's own .cs glob compile it and
        // collide with the referenced GodotSharp types.)
        string shimsPath = string.IsNullOrEmpty(input) ? "src/GodotSharpShim/GodotShims.g.cs" : input;
        Console.WriteLine($"[dn2cpp] Generating engine shims from {extensionApiPath}...");
        BindingGenerator.Run(extensionApiPath, shimsPath);
        Console.WriteLine($"[dn2cpp] Done! Generated:\n  - {shimsPath}");
        return 0;
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"error during generation: {ex}");
        return 3;
    }
}

if (string.IsNullOrEmpty(input))
{
    Console.Error.WriteLine("Usage: dn2cpp <assembly.dll> [-o <output-dir>] [-r <ref.dll>] [--no-default-ref <DnZlib|DnBrotli|DnHttp>] [--pinvoke-module <name>] [--auto-ref] [--no-shared-generics] [--shadow-stack] [--trim-reflection] [--reflection-root <Type.Full.Name>] [--no-manifest-resources <Assembly>] [--manifest-resource-root <manifest.name>] [--trim-godot-classes] [--godot-class-root <Godot.Full.Name>] [--max-heap-mb <n>] [--verbose] [--gdextension [--godot-api <extension_api.json>]] [--dotnet-module] [--hotupdate-base] [--emit-patch <patch.dll> --base-abi <base-abi.json> [--patch-version <n>] [--patch-stackcode]] [--generate-bindings <extension_api.json>] [--check-wasm-imports <side.wasm> <main.wasm> [<main.js>]] [--print-runtime-dir]");
    return 1;
}

// One backend per transpile: --gdextension and --dotnet-module name different
// lanes (shim-based GDExtension vs. real-GodotSharp mono-module drop-in), so
// asking for both is a mistake said out loud rather than silently resolved.
if (gdExtension && dotnetModule)
{
    Console.Error.WriteLine("error: --gdextension and --dotnet-module are mutually exclusive (one backend per transpile)");
    return 1;
}
// --godot-api names the dump only the GDExtension backend reads — on any other
// backend it would be silently ignored, so asking is a mistake said out loud.
if (!string.IsNullOrEmpty(godotApiPath) && !gdExtension)
{
    Console.Error.WriteLine("error: --godot-api requires --gdextension (it locates the extension_api.json driving the GDExtension call map)");
    return 1;
}
// --trim-godot-classes is a statement about the real GodotSharp's constructor
// registry, which only the --dotnet-module lane transpiles — elsewhere there is
// nothing to trim, so asking is a mistake said out loud.
if (trimGodotClasses && !dotnetModule)
{
    Console.Error.WriteLine("error: --trim-godot-classes requires --dotnet-module (it trims the real GodotSharp engine-wrapper registry)");
    return 1;
}
// A hot-update base build must keep every wrapper patch code could bind against
// — the same whole-surface contract that disables --trim-reflection there.
// Warn + disable rather than hard error, mirroring that flag's handling.
if (trimGodotClasses && hotupdateBase)
{
    Console.Error.WriteLine(
        "[dn2cpp] --trim-godot-classes is ignored under --hotupdate-base: a patch "
        + "may bind against any engine wrapper, so the base image keeps them all.");
    trimGodotClasses = false;
}

if (emitPatch)
{
    return PatchConverter.Run(input, references, baseAbiPath, outDir, patchVersion, patchRegCode);
}

// Method-body chunk size for the split C++ output (~1MB default). DN2CPP_SPLIT_BYTES
// overrides it — a gate forces small chunks to exercise multi-TU builds; 0 disables
// splitting (single generated.cpp). Read here in the full CLI only; the self-hosted
// console CLI keeps the default and adds no environment dependency.
int splitBytes = 1 << 20;
if (EnvKnobs.Int(EnvKnobs.SplitBytes) is { } splitOverride)
    splitBytes = splitOverride;

// Pure .NET console output by default; the Godot backend layers GDExtension
// registration tables on top, and the .NET-module backend emits the engine mono
// module's godotsharp_game_main_init export. The pipeline itself lives in the
// shared backend-agnostic driver.
IEmitBackend backend = dotnetModule ? new DotnetModuleBackend(trimGodotClasses, godotClassRoots)
    : gdExtension ? new GodotBackend(string.IsNullOrEmpty(godotApiPath) ? null : godotApiPath)
    : new ConsoleBackend();
return TranspileDriver.Run(new TranspileOptions
{
    Input = input,
    References = references,
    NoDefaultRefs = noDefaultRefs,
    OutDir = outDir,
    Measure = measure,
    Verbose = verbose,
    Backend = backend,
    SplitBytes = splitBytes,
    AutoRef = autoRef,
    SharedGenerics = sharedGenerics,
    HotupdateBase = hotupdateBase,
    HotupdateRefs = hotupdateRefs,
    NoAdoptAsync = noAdoptAsync,
    CutMethods = cutMethods,
    TrimReflection = trimReflection,
    ReflectionRoots = reflectionRoots,
    NoManifestResources = noManifestResources,
    ManifestResourceRoots = manifestResourceRoots,
    PInvokeModules = pinvokeModules,
    ShadowStack = shadowStack,
});

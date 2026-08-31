using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;

namespace Dn2Cpp;

/// <summary>The shared console-transpile driver: builds a <see cref="Compilation"/>
/// from the input assembly + reference assemblies and emits C++ through the given
/// backend (console or GDExtension), or — in <c>--measure</c> mode — collects and
/// reports every reachable transpilation gap. A Godot-free console CLI and the full
/// CLI share this one implementation.</summary>
public static class TranspileDriver
{
    /// <summary>Console entry: always runs the pure-.NET <see cref="ConsoleBackend"/>
    /// (no Godot dependency). The Godot-free console CLI calls this, and cannot set
    /// the internal <see cref="TranspileOptions.Backend"/> member — so this path
    /// always lands on <see cref="Run"/>'s null-backend console default.</summary>
    public static int RunConsole(TranspileOptions options)
        => Run(options);

    /// <summary>The <c>--dump-isa-surface</c> report: one <c>family</c> line per
    /// platform-ISA facade stamped on a loaded TypeDef
    /// (<c>family\t&lt;contract name&gt;\t&lt;arch&gt;\t&lt;token&gt;\t&lt;lowered&gt;</c>) and one
    /// <c>method</c> line per public static instruction other than <c>get_IsSupported</c>
    /// (<c>method\t&lt;contract name&gt;\t&lt;method&gt;\t&lt;helper name&gt;</c>), preceded by a
    /// <c>corelib\t&lt;path&gt;</c> line. Lines are ordinal-sorted so two runs over one
    /// CoreLib diff clean. A method whose parameter shape the helper-name contract cannot
    /// spell is reported, not skipped: its fourth column is <c>!</c> followed by the
    /// reason, so the report stays total over the surface and the gap is visible.</summary>
    private static void WriteIsaSurface(Compilation compilation, IReadOnlyList<string> loadSet, string path)
    {
        var lines = new List<string>();
        foreach (var module in compilation.Modules)
        {
            var reader = module.Reader;
            foreach (var tdh in reader.TypeDefinitions)
            {
                if (!module.ClassMap.TryGetValue(tdh, out var cls) || cls.PlatformIsa is not { } family)
                    continue;
                string contract = CoreIntrinsics.IsaContractName(family);
                lines.Add(string.Join('\t', new[]
                {
                    "family", contract, family.Arch.ToString(), family.Token,
                    family.Lowered ? "true" : "false",
                }));
                foreach (var mh in reader.GetTypeDefinition(tdh).GetMethods())
                {
                    var md = reader.GetMethodDefinition(mh);
                    string name = reader.GetString(md.Name);
                    if (name is ".ctor" or ".cctor" or "get_IsSupported")
                        continue;
                    if ((md.Attributes & MethodAttributes.Static) == 0
                        || (md.Attributes & MethodAttributes.MemberAccessMask) != MethodAttributes.Public)
                        continue;
                    var sig = md.DecodeSignature(compilation.SigProvider, GenericContext.Empty);
                    if (sig.GenericParameterCount != 0)
                        throw new InvalidOperationException(
                            $"{contract}.{name}: a platform-ISA instruction is never generic");
                    string helper;
                    try
                    {
                        helper = CoreIntrinsics.IsaHelperName(family, name, sig);
                    }
                    catch (NotSupportedException e)
                    {
                        helper = "!" + e.Message;
                    }
                    lines.Add(string.Join('\t', new[] { "method", contract, name, helper }));
                }
            }
        }
        lines.Sort(StringComparer.Ordinal);
        string corelib = loadSet.FirstOrDefault(p =>
            Path.GetFileName(p).Equals("System.Private.CoreLib.dll", StringComparison.OrdinalIgnoreCase)) ?? "";
        lines.Insert(0, "corelib\t" + corelib);
        File.WriteAllText(path, string.Concat(lines.Select(l => l + "\n")));
    }

    /// <summary>Backend-agnostic core: the full CLI sets
    /// <see cref="TranspileOptions.Backend"/> (Godot / dotnet-module / console); the
    /// console CLI funnels through <see cref="RunConsole"/> and leaves it null, which
    /// resolves — here, nowhere else — to the pure-.NET console backend. Every other
    /// lever, with its default, is documented on <see cref="TranspileOptions"/>.</summary>
    internal static int Run(TranspileOptions options)
    {
        string input = options.Input;
        string outDir = options.OutDir;
        bool measure = options.Measure;
        if (options.MaxDegreeOfParallelism < 0)
        {
            Console.Error.WriteLine("error: MaxDegreeOfParallelism must be zero or a positive integer");
            return 1;
        }
        if (!File.Exists(input))
        {
            Console.Error.WriteLine($"error: input assembly not found: {input}");
            return 1;
        }
        // Every --no-default-ref name must be one of the shipped shims: a typo silently
        // becoming a no-op suppression injects the shim the caller meant to keep out, and
        // nothing says so until its presence has already changed the emitted C++.
        // Validated here because this is the single point both CLIs funnel through — the
        // console CLI is no InternalsVisibleTo friend and cannot ask the predicate itself.
        foreach (string name in options.NoDefaultRefs ?? Array.Empty<string>())
        {
            if (!Compilation.IsDefaultRefName(name))
            {
                Console.Error.WriteLine(
                    $"error: --no-default-ref {name}: unknown default reference "
                    + $"(known: {Compilation.DefaultRefNameList})");
                return 1;
            }
        }
        IEmitBackend backend = options.Backend ?? new ConsoleBackend();
        options = options with { Backend = backend };
        // A hot-update base build needs every method and field row: the interpreter binds a
        // patch's imports by walking the base image's registry into those tables, so
        // trimming them leaves a patch unable to bind. The two flags are mutually
        // exclusive, said out loud so a build script asking for both learns which it got.
        if (options.TrimReflection && options.HotupdateBase)
        {
            Console.Error.WriteLine(
                "[dn2cpp] --trim-reflection is ignored under --hotupdate-base: the patch "
                + "interpreter binds a patch's imports through the base image's method and "
                + "field tables.");
            options = options with { TrimReflection = false };
        }

        try
        {
            // The app assembly is module 0; reference assemblies follow.
            var paths = new List<string> { input };
            paths.AddRange(options.References);
            // Auto-reference the managed support shim that ships next to the CLI, so
            // transpiler-synthesized types (e.g. the SZArrayEnumerable<T> array wrapper) are
            // available without an explicit -r. Tree-shaking drops it when unused. A CLI
            // installed without its sibling shim lands here with nothing to add; the first
            // lowering that needs a shim type then fails the transpile with an actionable
            // error (Compilation.RequireShimType).
            // Dedupe by FILE NAME (case-insensitive): the same shim handed via -r under a
            // different path must not load twice — the emitted output is a function of the
            // load set, and a second Dn2Cpp.Runtime carries no [NativeImplementation] row
            // to make the double load fail loudly.
            string runtimeShim = Path.Combine(AppContext.BaseDirectory, "Dn2Cpp.Runtime.dll");
            if (File.Exists(runtimeShim)
                && !paths.Any(p =>
                    Path.GetFileName(p).Equals("Dn2Cpp.Runtime.dll", StringComparison.OrdinalIgnoreCase)))
                paths.Add(runtimeShim);
            // --auto-ref with no CoreLib passed: default to the live shared framework's
            // implementation CoreLib, so an installed CLI transpiles real-BCL programs
            // with no -r at all (LoadReferenceClosure then resolves the BCL closure from
            // its directory). Under the self-hosted native CLI, Assembly.Location is null
            // by design (the intrinsic's documented divergence), so the default never
            // fires there and the export toolchain stays on its explicit ref/ closure.
            if (options.AutoRef && !paths.Any(p =>
                    Path.GetFileName(p).Equals("System.Private.CoreLib.dll", StringComparison.OrdinalIgnoreCase)))
            {
                string corelib = typeof(object).Assembly.Location;
                if (!string.IsNullOrEmpty(corelib) && File.Exists(corelib))
                    paths.Add(corelib);
            }
            // Arm the conditional default references: Compilation.InjectDefaultRefs decides
            // which shims the load set calls for, and cannot decide out here because none
            // is loaded until the reference closure has run. Deliberately not merged with
            // the unconditional Dn2Cpp.Runtime injection above, which must keep firing at
            // its current position — moving it past the closure renumbers that module and,
            // through ClassInfo.CompareByOrder's Module.Index key, reorders every emission.
            options = options with { DefaultRefDir = AppContext.BaseDirectory };
            // In --measure mode, collect all reachability-phase gaps during Build() rather
            // than aborting on the first one (used by the self-hosting feasibility harness).
            Timing.Mark("setup");
            var compilation = Compilation.Create(paths, options);

            if (options.IsaSurfaceDump is { } isaDump)
            {
                // Build stopped before reachability (Compilation.StopBeforeReachability);
                // the model holds every loaded TypeDef, which is all the surface needs.
                WriteIsaSurface(compilation, paths, isaDump);
                Console.WriteLine($"dn2cpp: platform-ISA surface -> {isaDump}");
                return 0;
            }

            if (measure)
            {
                // Enumerate every reachable transpilation gap in one pass. Writes a
                // per-method TSV report plus a grouped summary; emits no C++.
                var result = new CppEmitter(compilation, backend).MeasureGaps();
                Directory.CreateDirectory(outDir);
                string reportPath = Path.Combine(outDir, "s0-gaps.tsv");
                // Byte-for-byte equal to File.WriteAllLines (each line + Environment.NewLine,
                // including a trailing newline; empty -> ""), but routed through the
                // already-intercepted File.WriteAllText so the self-host transpile of dn2cpp
                // never roots the heavy File.WriteAllLines lazy-enumeration cascade.
                File.WriteAllText(reportPath, string.Concat(result.Gaps.Select(g =>
                    // Pass an explicit string[] so this binds to string.Join(char, string[])
                    // (the array overload dn2cpp supports) rather than the modern
                    // string.Join(char, params ReadOnlySpan<string?>) overload, which Roslyn
                    // lowers loose args into via a [InlineArray]-backed span — untranspilable
                    // when dn2cpp builds itself. Output is identical.
                    string.Join('\t', new[]
                        {
                            g.Phase, g.Namespace, g.Method, g.ExceptionType,
                            g.Message.Replace('\t', ' ').Replace('\n', ' ').Replace('\r', ' '),
                        })
                    + Environment.NewLine).ToArray()));

                // Category = the gap message with its leading "<Type>.<Method>: " wrapper
                // stripped (method names carry no spaces), so variants of the same gap merge.
                static string Category(string msg)
                {
                    int c = msg.IndexOf(": ", StringComparison.Ordinal);
                    int sp = msg.IndexOf(' ');
                    return c > 0 && (sp < 0 || sp > c) ? msg[(c + 2)..] : msg;
                }

                Console.WriteLine($"dn2cpp --measure: {result.Attempted} compile-phase bodies attempted, "
                    + $"{result.Compiled} compiled OK, {result.Gaps.Count} gaps total");
                // Printed even at zero: the named count is the proof the sweep RAN — any
                // real program names symbols — because a silently skipped sweep and a
                // clean corpus would otherwise print alike.
                Console.WriteLine(result.NamedSymbols is { } namedSyms
                    ? $"  dangling-symbol sweep: {namedSyms} named symbols diffed, "
                        + $"{result.Gaps.Count(g => g.Phase == "dangling")} dangling"
                    : "  dangling-symbol sweep: skipped (a compile-spec gap truncated the body pass)");
                Console.WriteLine($"  full per-gap report -> {reportPath}");
                Console.WriteLine("  -- gaps by phase --");
                foreach (var grp in result.Gaps.GroupBy(g => g.Phase).OrderByDescending(g => g.Count()))
                    Console.WriteLine($"    {grp.Count(),5}  {grp.Key}");
                Console.WriteLine("  -- gaps by namespace --");
                foreach (var grp in result.Gaps.GroupBy(g => g.Namespace).OrderByDescending(g => g.Count()))
                    Console.WriteLine($"    {grp.Count(),5}  {grp.Key}");
                Console.WriteLine("  -- gaps by message category (top 40) --");
                foreach (var grp in result.Gaps.GroupBy(g => Category(g.Message)).OrderByDescending(g => g.Count()).Take(40))
                    Console.WriteLine($"    {grp.Count(),5}  {grp.Key}");
                WriteBoundedImportReport(compilation, outDir, options.Verbose);
                return 0;
            }

            Directory.CreateDirectory(outDir);
            // Emission streams its translation units — each chunk is written the moment it
            // is sealed — so the output directory must exist before Emit, and stale chunks
            // from a previous, larger run must be swept BEFORE the new ones land: the
            // build's `generated*.cpp` glob would otherwise pick a leftover up and fail to
            // link on duplicate definitions, and sweeping afterwards cannot help a run that
            // threw halfway. The legacy `generated_N.cpp` family is swept too, so a
            // directory written by an older dn2cpp cannot poison this build.
            RemoveStaleChunks(outDir, "generated_b", ".cpp");
            RemoveStaleChunks(outDir, "generated_m", ".cpp");
            RemoveStaleChunks(outDir, "generated_", ".cpp");
            // Optional generated TUs have fixed names, so they are swept rewrite-or-
            // remove style like the sidecars below: deleted up front and recreated
            // during emission only when this run marks their feature.
            foreach (string hotTu in new[]
                { "generated_hot.cpp", "generated_hot_fast.cpp", "generated_platform_isa.cpp" })
            {
                string hotTuPath = Path.Combine(outDir, hotTu);
                if (File.Exists(hotTuPath))
                    File.Delete(hotTuPath);
            }

            var sources = new CppEmitter(compilation, backend, options.HotupdateBase).Emit(
                (fileName, text) => File.WriteAllText(Path.Combine(outDir, fileName), text),
                options.SplitBytes);

            string outPath = Path.Combine(outDir, "generated.cpp");
            // What emission did not stream: the shared header and the primary TU. A build
            // compiles every generated*.cpp; generated.h is include-only. One at a time —
            // ToString() the builder, write it, drop it — so the two final strings never
            // coexist.
            File.WriteAllText(Path.Combine(outDir, "generated.h"), sources.Header.ToString());
            File.WriteAllText(outPath, sources.Data.ToString());

            // Hot-update base build: the ABI-contract sidecar the patch converter
            // stamps into each BPI. Rewrite-or-remove so a stale file from a prior
            // --hotupdate-base run never lingers on a flagless rebuild.
            string abiPath = Path.Combine(outDir, "base-abi.json");
            if (sources.BaseAbiJson is not null)
                File.WriteAllText(abiPath, sources.BaseAbiJson);
            else if (File.Exists(abiPath))
                File.Delete(abiPath);

            // P/Invoke link manifest: one bare library-name token per distinct
            // native library a lowered [DllImport] needs, one per line, for
            // runtime/CMakeLists.txt to feed into target_link_libraries. A bare
            // name (not a `-l...` flag) lets CMake resolve it per-compiler:
            // `-lkernel32` on a GNU-like linker, `kernel32.lib` on MSVC's
            // link.exe — a `-l` prefix would instead be forwarded verbatim on
            // every generator and break the MSVC link. Always-linked
            // libc/libSystem contribute nothing, so a libc-only program leaves
            // no manifest. Rewrite-or-remove so a stale file from a prior run
            // (e.g. a since-removed DllImport) never lingers.
            string manifestPath = Path.Combine(outDir, "pinvoke-libs.txt");
            var linkLibs = compilation.PInvokeLinkLibraries();
            if (linkLibs.Count > 0)
                // One bare token per line, each terminated by Environment.NewLine,
                // via the intercepted File.WriteAllText (see the --measure report
                // write above).
                File.WriteAllText(manifestPath, string.Concat(linkLibs.Select(l => l + Environment.NewLine).ToArray()));
            else if (File.Exists(manifestPath))
                File.Delete(manifestPath);

            // A Windows package may ship a DLL without its import .lib. Record
            // every reachable entry point so CMake can construct a delay-load
            // import library. This is separate from pinvoke-libs.txt because one
            // module token may contribute many symbols.
            string symbolManifestPath = Path.Combine(outDir, "pinvoke-symbols.txt");
            var linkSymbols = compilation.PInvokeLinkSymbols();
            if (linkSymbols.Count > 0)
                File.WriteAllText(symbolManifestPath,
                    string.Concat(linkSymbols.Select(s => s + Environment.NewLine).ToArray()));
            else if (File.Exists(symbolManifestPath))
                File.Delete(symbolManifestPath);
            Timing.Mark("write-files");
            // Second census, so the pair brackets emission: whatever grew between
            // the two was pulled in by a body's own tokens, not by discovery.
            ModelCensus.Report("emit", compilation);

            // Straight off the reachable set: walking every class's methods instead would
            // force each specialization to decode members on demand, for a headline number
            // printed after the output is already written.
            int emitted = compilation.Reachable.Count(m => m.Rva != 0);
            string asms = paths.Count == 1 ? "1 assembly" : $"{paths.Count} assemblies";
            int chunks = sources.BodyChunks + sources.MetadataChunks + sources.HotChunks
                + sources.HotFastChunks;
            string splitNote = chunks > 0 ? $" (+{chunks} split TUs, generated.h)" : " (+generated.h)";
            Console.WriteLine($"dn2cpp: {asms}, {compilation.Classes.Count} types, {emitted} reachable methods -> {outPath}{splitNote}");
            ReportBoundedImports(compilation, options.Verbose);
            if (compilation.GodotClassTrimEnabled)
            {
                var gt = compilation.GodotClassTrimStats;
                Console.WriteLine($"dn2cpp: godot-class-trim: {gt.Released} released of "
                    + $"{gt.Registered} registered engine wrappers, {gt.Redirected} lambdas redirected");
            }
            if (options.SharedGenerics)
            {
                var s = compilation.SharedStats;
                Console.WriteLine($"dn2cpp: shared-generics: {s.Instantiations} instantiations in "
                    + $"{s.Groups} groups, {s.SharedBodies}/{s.EligibleBodies} bodies shared, "
                    + $"{compilation.RgctxSlotCount} rgctx slots in {compilation.RgctxTableCount} tables, "
                    + $"{compilation.RgctxForwarderCount} forwarders");
                if (compilation.SharedTaintReasons.Count > 0)
                    Console.WriteLine("dn2cpp: shared-generics fallbacks: " + string.Join(" ",
                        compilation.SharedTaintReasons
                            .OrderByDescending(kv => kv.Value).ThenBy(kv => kv.Key, StringComparer.Ordinal)
                            .Select(kv => $"{kv.Key}={kv.Value}")));
                // Per-body fallback dump (the histogram names kinds only):
                // which canonical body carries which root/cascade taint — the
                // localization aid for chasing a fallback down to its site.
                if (EnvKnobs.BoolIsOne(EnvKnobs.SharedDump))
                    foreach (var kv in compilation.SharedTaint
                                 .OrderBy(kv => kv.Key.CppName, StringComparer.Ordinal))
                        Console.WriteLine($"dn2cpp: shared-generics taint {kv.Value} {kv.Key.CppName}");
            }
            return 0;
        }
        catch (NotSupportedException ex)
        {
            if (EnvKnobs.BoolIsOne(EnvKnobs.DebugTrace))
                Console.Error.WriteLine(ex);
            Console.Error.WriteLine($"error: {ex.Message}");
            return 2;
        }
    }

    /// <summary>The one line an ordinary transpile prints when it bounded a native import
    /// to a zero-returning default, plus the per-import detail under <c>--verbose</c>.
    /// Silent at zero, so the line's presence is itself the signal.
    ///
    /// <para>Unconditional rather than opt-in because what it reports can be a <b>silent
    /// wrong answer at run time</b>: the caller of a bounded import gets a zero it cannot
    /// distinguish from a real result, and a report you must already know to ask for would
    /// not reach that caller.</para>
    ///
    /// <para>The line states the substitution AND the verdict, because the bounded leaves
    /// no longer all do the same thing. A <c>Loud</c> row throws a catchable
    /// <c>PlatformNotSupportedException</c> naming the module — used where the caller does
    /// not check the zero, e.g. the <c>NativeLibrary.Load</c> QCalls. A <c>Silent</c> row's
    /// default is simply the truth about a transpiled binary (no managed debugger, no COM,
    /// one assembly load context). Which rows are bodyless imports at all depends on the
    /// host's CoreLib flavour; a <c>--measure</c> run's <c>s0-bounded-imports.tsv</c>
    /// sidecar carries the verdict per row in column 4.</para></summary>
    private static void ReportBoundedImports(Compilation compilation, bool verbose)
    {
        var imports = compilation.BoundedImports;
        if (imports.Count == 0)
            return;
        // Modules, not entry points: "which native module is missing" is the actionable
        // half, and it is short enough to fit on the count line.
        var modules = imports.Select(b => b.Module).Distinct(StringComparer.Ordinal)
            .OrderBy(s => s, StringComparer.Ordinal).ToList();
        int loud = CountLoud(imports);
        string plural = imports.Count == 1 ? "import" : "imports";
        Console.WriteLine($"dn2cpp: {imports.Count} native {plural} bounded "
            + $"({string.Join(", ", modules.ToArray())}) — {loud} throw "
            + $"PlatformNotSupportedException naming the module, {imports.Count - loud} answer "
            + "0/null silently and a caller cannot tell that default from a real result"
            + (verbose ? ":" : "; pass --verbose to name them"));
        if (!verbose)
            return;
        foreach (var b in imports)
            Console.WriteLine($"  - {b.Module}!{b.EntryPoint}  ({b.Method})  [{VerdictWord(b)}]");
    }

    /// <summary>How many of the recorded imports throw rather than answer. A plain loop and
    /// not <c>Count(…)</c>: the element is a value type, and a LINQ pipeline over one is the
    /// shape whose canonical-body sharing breaks the self-host C++ compile.</summary>
    private static int CountLoud(IReadOnlyList<BoundedImport> imports)
    {
        int n = 0;
        foreach (var b in imports)
        {
            if (b.Verdict == BoundedVerdict.Loud)
                n++;
        }

        return n;
    }

    /// <summary>The verdict as it appears in a report line and in the sidecar's fourth
    /// column. One vocabulary for both, so a reader who greps the TSV and a reader who reads
    /// the console see the same word.</summary>
    private static string VerdictWord(BoundedImport b) =>
        b.Verdict == BoundedVerdict.Loud ? "throws" : "silent";

    /// <summary>The <c>--measure</c> half of the same report: the count line (and
    /// <c>--verbose</c> detail) plus a full TSV sidecar beside <c>s0-gaps.tsv</c>.
    ///
    /// <para>A separate file rather than a sixth gap phase: every row of
    /// <c>s0-gaps.tsv</c> means "this could not be transpiled", and consumers assert on
    /// that (a zero-gap assert, a baseline diff). A bounded import is a fact about a
    /// <em>successful</em> transpile, so putting it there would turn those assertions red.
    /// Rewrite-or-remove, so a stale file from a prior run never lingers.</para>
    ///
    /// <para>Four tab-separated columns — module, entry point, declaring method, verdict.
    /// No message column: there is exactly one reason and it is the file's name.</para></summary>
    private static void WriteBoundedImportReport(Compilation compilation, string outDir, bool verbose)
    {
        string path = Path.Combine(outDir, "s0-bounded-imports.tsv");
        var imports = compilation.BoundedImports;
        if (imports.Count > 0)
            // The intercepted File.WriteAllText + string.Concat idiom, for the reason
            // given at the s0-gaps.tsv write above.
            File.WriteAllText(path, string.Concat(imports.Select(b =>
                string.Join('\t', new[] { b.Module, b.EntryPoint, b.Method, VerdictWord(b) })
                + Environment.NewLine).ToArray()));
        else if (File.Exists(path))
            File.Delete(path);
        int loudCount = CountLoud(imports);
        Console.WriteLine($"  -- native imports bounded: {imports.Count} "
            + $"({loudCount} throwing, {imports.Count - loudCount} silent) --");
        if (imports.Count == 0)
            return;
        Console.WriteLine($"  full per-import report -> {path}");
        if (!verbose)
            return;
        foreach (var b in imports)
            Console.WriteLine($"    {b.Module}!{b.EntryPoint}  ({b.Method})  [{VerdictWord(b)}]");
    }

    /// <summary>Deletes <c>{prefix}1{suffix}</c>, <c>{prefix}2{suffix}</c>, … until the
    /// first gap. Chunk numbering is contiguous by construction, so the first missing
    /// index means there is nothing above it either.</summary>
    private static void RemoveStaleChunks(string outDir, string prefix, string suffix)
    {
        for (int n = 1; ; n++)
        {
            string path = Path.Combine(outDir, prefix + n + suffix);
            if (!File.Exists(path))
                return;
            File.Delete(path);
        }
    }
}

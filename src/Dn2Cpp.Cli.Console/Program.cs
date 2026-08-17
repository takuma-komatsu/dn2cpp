using Dn2Cpp;

// Console-only dn2cpp CLI: the pure-.NET console transpiler with no Dn2Cpp.Godot
// dependency. This is the self-host target for console-closure measurement —
// transpiling *this* exe reaches only what a normal console transpile actually
// runs (ConsoleBackend), without the Godot lane's --generate-bindings /
// BindingGenerator.Run subtree that the full CLI statically roots but never
// executes on the console path (=, out of console-self-host scope).
//
// Supports only the console transpile arguments, including the shared
// preservation controls. There is no --gdextension / --generate-bindings here;
// the GDExtension path lives in the full `dn2cpp` CLI. The transpile pipeline
// itself is the shared backend-agnostic Dn2Cpp.TranspileDriver, so this CLI's
// console output is byte-identical to the full CLI's ConsoleBackend path.

string input = "";
string outDir = ".";
bool measure = false;
bool verbose = false;
bool autoRef = false;
bool sharedGenerics = true;
bool shadowStack = false;
var references = new List<string>();
var noDefaultRefs = new List<string>();
var projectRoots = new List<string>();
var linkFeatures = new List<string>();

for (int i = 0; i < args.Length; i++)
{
    if (args[i] == "-o" && i + 1 < args.Length)
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
        // is in the load set. Repeatable, one shim per occurrence, no blanket form (a
        // shim added later must inject and force a decision, not be silently covered by
        // an old flag). A FLAG, never an environment variable: it changes the emitted
        // C++. Unknown names are rejected in the shared TranspileDriver.
        noDefaultRefs.Add(args[++i]);
    }
    else if (args[i] == "--measure")
    {
        // self-hosting feasibility harness: transpile the input and report every
        // reachable unsupported-IL / unmapped-intrinsic gap (instead of stopping at
        // the first), rather than emitting C++. See gates/selfhost-measure-console.sh.
        measure = true;
    }
    else if (args[i] == "--verbose")
    {
        // Expand the end-of-run reports that print a count by default into their
        // per-item detail. Mirrors the full CLI so one command line works on both.
        verbose = true;
    }
    else if (args[i] == "--auto-ref")
    {
        // Eagerly load the transitive shared-framework reference closure before Build()
        // so external generic instantiations resolve without explicit -r for every
        // transitive dependency. Opt-in while drift is being measured.
        autoRef = true;
    }
    else if (args[i] == "--project-root" && i + 1 < args.Length)
    {
        projectRoots.Add(args[++i]);
    }
    else if (args[i] == "--link-feature" && i + 1 < args.Length)
    {
        string feature = args[++i];
        if (feature != "com" && feature != "sre" && feature != "remoting")
        {
            Console.Error.WriteLine($"error: --link-feature expects com, sre, or remoting, got '{feature}'");
            return 1;
        }
        linkFeatures.Add(feature);
    }
    else if (args[i] == "--no-shared-generics")
    {
        // Escape hatch: compile every generic instantiation monomorphically
        // instead of the default canonical shared-generics grouping.
        sharedGenerics = false;
    }
    else if (args[i] == "--shadow-stack")
    {
        // Opt-in shadow stack: a Dn2CppShadowFrame guard in every compiled
        // body's prologue buys exact throw traces (surviving -O2 inlining, working
        // on WASM) for a per-call cost. A FLAG, never an environment variable: it
        // changes the emitted C++ (self-host fixpoint discipline).
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

if (string.IsNullOrEmpty(input))
{
    Console.Error.WriteLine("Usage: dn2cpp-console <assembly.dll> [-o <output-dir>] [-r <ref.dll>] [--no-default-ref <DnZlib|DnBrotli|DnHttp>] [--auto-ref] [--project-root <dir>] [--link-feature <com|sre|remoting>] [--no-shared-generics] [--shadow-stack] [--measure] [--verbose]");
    return 1;
}

return TranspileDriver.RunConsole(new TranspileOptions
{
    Input = input,
    References = references,
    NoDefaultRefs = noDefaultRefs,
    OutDir = outDir,
    Measure = measure,
    Verbose = verbose,
    AutoRef = autoRef,
    SharedGenerics = sharedGenerics,
    ShadowStack = shadowStack,
    ProjectRoots = projectRoots,
    LinkFeatures = linkFeatures,
});

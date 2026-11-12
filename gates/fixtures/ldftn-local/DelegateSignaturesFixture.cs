using System.Collections;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Runtime.ExceptionServices;
using CecilAssembly = Mono.Cecil.AssemblyDefinition;

internal static partial class DelegateSignaturesFixture
{
    private const BindingFlags Members = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
    private const BindingFlags Statics = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
    private const string ExactReference = "Delegate callable member requires its exact CoreLib reference and declaration";
    private const string ExactOwner = "Delegate callable member requires an unmodified CoreLib owner";
    private const string TrailingSignature = "Original retained signature has trailing bytes.";
    private static readonly string[] Modes = { "control", "return-call", "parameter-address", "modifier-call", "header-address",
        "trailing-call", "fake-owner-address", "owner-typespec-call", "owner-modifier-address" };
    private static MethodInfo s_definition = null!;
    private static MethodInfo s_signature = null!;

    private sealed record Probe(string Mode, string Stage, string Path, MemberReferenceHandle[] References, string? Refusal);

    internal static bool Run(string[] args)
    {
        if (args.Length != 4 || args[0] != "--delegate-signatures") return false;
        string cliDirectory = Path.GetDirectoryName(Path.GetFullPath(args[1]))!;
        string core = Path.GetFullPath(args[2]);
        string root = Path.GetFullPath(args[3]);
        Directory.CreateDirectory(root);
        var transpiler = Assembly.LoadFrom(Path.Combine(cliDirectory, "Dn2Cpp.Transpiler.dll"));
        var contract = transpiler.GetType("Dn2Cpp.DelegateCallableBodyContract", throwOnError: true)!;
        s_definition = contract.GetMethod("IsDefinition", Statics)!;
        s_signature = contract.GetMethod("Signature", Statics)!;
        DelegateSignaturesDeclarations.Check(core);

        var ildiet = Assembly.LoadFrom(Path.Combine(cliDirectory, "ildiet", "ILDiet.dll"));
        var preserver = ildiet.GetType("Dn2Cpp.AssemblyDiet+ArrayKindPreserver", throwOnError: true)!;
        var preflight = preserver.GetMethod("Member", Members)!;
        var probes = new List<Probe>();
        foreach (string mode in Modes)
        {
            string input = Path.Combine(root, mode, "DelegateSignatures.dll");
            Generate(mode, input, core);
            var references = DelegateSignaturesMetadata.Check(input, mode);
            Console.WriteLine("delegate-signature-fixture:" + mode);
            string? refusal = mode == "control" ? null : mode.StartsWith("owner-", StringComparison.Ordinal) ? ExactOwner : ExactReference;
            probes.Add(new Probe(mode, "raw", input, references, refusal));

            using var image = new PEReader(File.OpenRead(input));
            using var app = CecilAssembly.ReadAssembly(input, new Mono.Cecil.ReaderParameters { InMemory = true });
            object instance = Activator.CreateInstance(preserver, Members, null,
                new object[] { image.GetMetadataReader(), app.MainModule }, null)!;
            string rewritten = Path.Combine(root, mode, "DelegateSignatures.Rewritten.dll");
            if (mode == "trailing-call")
            {
                Refuses(TrailingSignature, () => Invoke(preflight, instance,
                    app.MainModule.LookupToken(MetadataTokens.GetToken(references.Single()))));
                DelegateSignaturesMetadata.Require(!File.Exists(rewritten), "trailing signature reached the writer");
                Console.WriteLine("delegate-signature-preflight:trailing-call:refused");
                continue;
            }
            foreach (var reference in references)
                Invoke(preflight, instance, app.MainModule.LookupToken(MetadataTokens.GetToken(reference)));
            // Exercise the production preflight and the small Cecil rewrite only;
            // the existing runtime image covers ILDiet's full retention pipeline.
            app.Name.Name += ".Rewritten";
            app.Write(rewritten, new Mono.Cecil.WriterParameters { Timestamp = 0, DeterministicMvid = true });
            string shape = mode == "owner-typespec-call" ? "owner-normalized-call" : mode;
            var rewrittenReferences = DelegateSignaturesMetadata.Check(rewritten, shape);
            probes.Add(new Probe(mode, "rewritten", rewritten, rewrittenReferences,
                mode == "owner-typespec-call" ? null : refusal));
            Console.WriteLine("delegate-signature-preflight:" + mode + ":rewritten");
        }
        CheckResolver(transpiler, core, root, probes);
        Console.WriteLine("delegate-signature-boundaries:ok");
        return true;
    }

    private static void CheckResolver(Assembly transpiler, string core, string root, List<Probe> probes)
    {
        var compilationType = transpiler.GetType("Dn2Cpp.Compilation", throwOnError: true)!;
        var optionsType = transpiler.GetType("Dn2Cpp.TranspileOptions", throwOnError: true)!;
        object options = Activator.CreateInstance(optionsType)!;
        optionsType.GetProperty("Input")!.SetValue(options, probes[0].Path);
        optionsType.GetProperty("UseILDiet")!.SetValue(options, false);
        optionsType.GetProperty("SharedGenerics")!.SetValue(options, false);
        // This existing metadata-only boundary stops before reachability and
        // emission. Construct one model, not a hidden full compile per case.
        optionsType.GetProperty("IsaSurfaceDump")!.SetValue(options, "delegate-signature-metadata-only");
        string[] paths = new[] { probes[0].Path, core, Path.Combine(root, "fake-owner-address", "FakeDelegateOwner.dll") }
            .Concat(probes.Skip(1).Select(probe => probe.Path)).ToArray();
        object compilation = Invoke(compilationType.GetMethod("Create", Statics)!, null, paths, options)!;
        var modules = ((IEnumerable)Read(compilation, "Modules")).Cast<object>()
            .ToDictionary(module => Path.GetFullPath((string)Read(module, "Path")), StringComparer.Ordinal);
        DelegateSignaturesMetadata.Require(modules.Count == paths.Length
            && modules.Values.Select(module => (string)Read(module, "AssemblyName")).Distinct(StringComparer.Ordinal).Count() == modules.Count,
            "signature fixtures must have independent assembly identities");
        object context = transpiler.GetType("Dn2Cpp.GenericContext", throwOnError: true)!.GetField("Empty", Statics)!.GetValue(null)!;
        var resolve = compilationType.GetMethod("TryResolveDelegateCallableReference", Members)!;
        CheckNoBodies();
        foreach (var probe in probes)
        {
            object module = modules[probe.Path];
            foreach (var reference in probe.References)
            {
                if (probe.Refusal is not null)
                {
                    Refuses(probe.Refusal, () => Invoke(resolve, compilation, module, reference, context));
                    // Method lookup may cache a candidate before exact validation;
                    // replay must not turn a rejected raw reference into authority.
                    Refuses(probe.Refusal, () => Invoke(resolve, compilation, module, reference, context));
                }
                else
                {
                    object? method = Invoke(resolve, compilation, module, reference, context);
                    DelegateSignaturesMetadata.Require(method is not null, "real delegate reference did not resolve");
                    var reader = (MetadataReader)Read(module, "Reader");
                    var member = reader.GetMemberReference(reference);
                    var owner = reader.GetTypeReference((TypeReferenceHandle)member.Parent);
                    DelegateSignaturesMetadata.Require((string)Read(Read(method!, "Module"), "AssemblyName") == "System.Private.CoreLib"
                        && (string)Read(method!, "Name") == reader.GetString(member.Name)
                        && (string)Read(Read(method!, "DeclaringClass"), "FullName") == reader.GetString(owner.Namespace) + "." + reader.GetString(owner.Name),
                        "delegate control resolved to a different CoreLib member");
                }
            }
            Console.WriteLine("delegate-signature-resolver:" + probe.Mode + ":" + probe.Stage + ":"
                + (probe.Refusal is null ? "accepted" : "refused"));
        }
        CheckNoBodies();
        foreach (object module in modules.Values) ((PEReader)Read(module, "PE")).Dispose();
        Console.WriteLine("delegate-signature-model:one:reachable-zero:emission-none");

        void CheckNoBodies()
        {
            DelegateSignaturesMetadata.Require((bool)Read(compilation, "StopBeforeReachability")
                && (int)Read(Read(compilation, "Reachable"), "Count") == 0
                && Read(compilation, "Phase").ToString() == "Discovery"
                && !Directory.EnumerateFiles(root, "generated*.cpp", SearchOption.AllDirectories).Any(),
                "signature boundary probe unexpectedly entered reachability or emission");
        }
    }

    internal static bool IsDefinition(MetadataReader reader, MethodDefinitionHandle handle, string owner, string name) =>
        (bool)Invoke(s_definition, null, reader, handle, owner, name)!;

    internal static bool Signature(MetadataReader reader, string owner, string name, BlobReader blob,
        bool definition, out MethodAttributes attributes)
    {
        object?[] args = { reader, owner, name, blob, definition, null };
        bool result = (bool)Invoke(s_signature, null, args)!;
        attributes = (MethodAttributes)args[^1]!;
        return result;
    }

    private static object Read(object value, string name) =>
        value.GetType().GetProperty(name, Members)?.GetValue(value)
        ?? value.GetType().GetField(name, Members)!.GetValue(value)!;

    private static object? Invoke(MethodInfo method, object? target, params object?[] args)
    {
        try { return method.Invoke(target, args); }
        catch (TargetInvocationException exception) when (exception.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(exception.InnerException).Throw();
            throw;
        }
    }

    private static void Refuses(string expected, Action action)
    {
        try { action(); }
        catch (NotSupportedException exception) when (exception.Message == expected) { return; }
        throw new InvalidOperationException("signature boundary did not refuse: " + expected);
    }
}

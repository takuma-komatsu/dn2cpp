using SRM = System.Reflection.Metadata;
using SRPE = System.Reflection.PortableExecutable;
using SRME = System.Reflection.Metadata.Ecma335.MetadataTokens;
using System.Globalization;
using Mono.Cecil;
using Mono.Cecil.Cil;

internal static class ArrayNativeLengthFixture
{
    public static bool Run(string[] args)
    {
        if (RunDietProof(args)) return true;
        if (args.Length != 5 || args[0] != "--array-native-length-fixture") return false;
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        string output = Path.GetFullPath(args[3]);
        string route = args[4];
        if (route is not ("direct-calli" or "reflection-only")) throw new ArgumentException("Expected direct-calli or reflection-only");
        if (File.Exists(output)) throw new InvalidOperationException("fixture output must be new");
        using var core = AssemblyDefinition.ReadAssembly(args[2]);
        if (core.Name.Name != "System.Private.CoreLib") throw new InvalidOperationException("Expected original CoreLib PE");
        var definition = core.MainModule.GetType("System.Array").Methods.Single(method => method.Name == "get_NativeLength");
        if (definition.Attributes != (MethodAttributes)0x0883 || definition.ImplAttributes != (MethodImplAttributes)0
            || !definition.HasBody || definition.HasThis != true || definition.ExplicitThis || definition.HasGenericParameters
            || definition.CallingConvention != MethodCallingConvention.Default
            || definition.ReturnType.MetadataType != MetadataType.UIntPtr
            || definition.Parameters.Count != 0)
            throw new InvalidOperationException("Exact CoreLib leaf declaration changed");
        using var app = AssemblyDefinition.ReadAssembly(args[1], new ReaderParameters { InMemory = true });
        var module = app.MainModule;
        var program = module.GetType("ArrayNativeLengthWitness.Program");
        MethodDefinition Method(string name) => program.Methods.Single(method => method.Name == name);
        if (route == "direct-calli")
        {
            foreach (string name in new[] { "Direct", "Calli" })
            {
                var method = Method(name);
                if (method.Parameters.Count != 1 || method.Parameters[0].ParameterType.FullName != "System.Array"
                    || method.ReturnType.MetadataType != MetadataType.UIntPtr)
                    throw new InvalidOperationException("Witness call shape changed");
                method.Body = new MethodBody(method);
                var target = module.ImportReference(definition);
                var il = method.Body.GetILProcessor(); il.Emit(OpCodes.Ldarg_0);
                if (name == "Calli")
                {
                    il.Emit(OpCodes.Ldftn, target);
                    var site = new CallSite(target.ReturnType) { HasThis = false, CallingConvention = MethodCallingConvention.Default };
                    site.Parameters.Add(new ParameterDefinition(module.ImportReference(target.DeclaringType)));
                    foreach (var parameter in target.Parameters) site.Parameters.Add(new ParameterDefinition(parameter.ParameterType));
                    il.Emit(OpCodes.Calli, site);
                }
                else il.Emit(OpCodes.Call, target);
                il.Emit(OpCodes.Ret);
            }
            module.EntryPoint = Method("DirectMain");
        }
        else module.EntryPoint = Method("ReflectionOnlyMain");
        var remove = route == "direct-calli"
            ? new[] { "Main", "Oracle", "ReflectionOnlyMain", "Named", "NamedMethod" }
            : new[] { "Main", "Oracle", "DirectMain", "Direct", "Calli", "Shape", "Null" };
        foreach (string name in remove) program.Methods.Remove(Method(name));
        var instructions = program.Methods.Where(method => method.HasBody).SelectMany(method => method.Body.Instructions).ToArray();
        if (instructions.Any(instruction => instruction.Operand is MethodReference method
            && (method.DeclaringType.Namespace == "System.Reflection.Emit"
                || method.DeclaringType.FullName == program.FullName && remove.Contains(method.Name))))
            throw new InvalidOperationException("Generated fixture still references a removed oracle or route");
        bool IsLeaf(Instruction instruction) => instruction.Operand is MethodReference method
            && method.DeclaringType.FullName == definition.DeclaringType.FullName && method.Name == definition.Name;
        if (route == "direct-calli")
        {
            if (instructions.Count(instruction => IsLeaf(instruction) && instruction.OpCode == OpCodes.Call) != 1
                || instructions.Count(instruction => IsLeaf(instruction) && instruction.OpCode == OpCodes.Ldftn) != 1
                || instructions.Count(instruction => instruction.OpCode == OpCodes.Calli) != 1)
                throw new InvalidOperationException("Expected one genuine direct and one genuine address/calli site");
        }
        else if (instructions.Any(IsLeaf) || instructions.Any(instruction => instruction.OpCode == OpCodes.Calli)
            || instructions.Any(instruction => instruction.Operand is MethodReference method
                && method.DeclaringType.FullName.StartsWith("System.Runtime.InteropServices.WeakGCHandle`1", StringComparison.Ordinal)
                && (method.Name is "TryGetTarget" or "SetTarget")))
            throw new InvalidOperationException("Reflection-only fixture has a rescuing direct leaf or guarded weak consumer");
        app.Write(output, new WriterParameters { Timestamp = 0, DeterministicMvid = true });
        Console.WriteLine("array-native-length-fixture:" + route + ";exact-original-CoreLib;native-input-only");
        return true;
    }

    private const string DietLabel = "array-native-length";
    private const string DietAssemblyName = "ArrayNativeLengthWitness";
    private const string DietProgramNamespace = "ArrayNativeLengthWitness";
    private const string DietSentinelName = "UnusedDietSentinel";
    private static readonly byte[] DietOriginalBody = Convert.FromHexString("2A");
    private static readonly byte[] DietStubBody = { 0x20, 0x54, 0x44, 0x4c, 0x49, 0x26, 0x14, 0x7a };
    private static readonly byte[] DietSentinelSignature = { 0x00, 0x00, 0x01 };

    private sealed class DietProofFailure : InvalidOperationException
    {
        internal string Code { get; }
        internal DietProofFailure(string code) : base(DietLabel + " diet proof: " + code) { Code = code; }
    }

    private static void DietRequire(bool condition, string code)
    {
        if (!condition) throw new DietProofFailure(code);
    }

    private sealed class DietImage : IDisposable
    {
        private readonly FileStream stream;
        internal SRPE.PEReader Pe { get; }
        internal SRM.MetadataReader Reader { get; }
        internal DietImage(string path)
        {
            stream = File.OpenRead(path);
            try
            {
                Pe = new SRPE.PEReader(stream, SRPE.PEStreamOptions.LeaveOpen);
                Reader = SRM.PEReaderExtensions.GetMetadataReader(Pe);
            }
            catch { stream.Dispose(); throw; }
        }
        public void Dispose() { Pe.Dispose(); stream.Dispose(); }
    }

    private static string DietHash(string path)
    {
        using var stream = File.OpenRead(path);
        return Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(stream));
    }

    private static string DietAssemblyIdentity(DietImage image)
    {
        var reader = image.Reader;
        DietRequire(reader.IsAssembly, "not-assembly");
        var assembly = reader.GetAssemblyDefinition();
        string name = reader.GetString(assembly.Name);
        DietRequire(name == DietAssemblyName, "assembly-name");
        return name + "|" + assembly.Version + "|" + reader.GetString(assembly.Culture)
            + "|" + (int)assembly.Flags + "|" + (int)assembly.HashAlgorithm
            + "|" + Convert.ToHexString(reader.GetBlobBytes(assembly.PublicKey))
            + "|" + reader.GetString(reader.GetModuleDefinition().Name);
    }

    private static SRM.TypeDefinitionHandle DietFindType(DietImage image, string space, string name)
    {
        var reader = image.Reader;
        var matches = reader.TypeDefinitions.Where(handle =>
        {
            var type = reader.GetTypeDefinition(handle);
            return type.GetDeclaringType().IsNil && reader.GetString(type.Namespace) == space && reader.GetString(type.Name) == name;
        }).ToArray();
        DietRequire(matches.Length == 1, "program-owner-count");
        return matches[0];
    }

    private static SRM.TypeDefinitionHandle DietProgram(DietImage image)
    {
        var handle = DietFindType(image, DietProgramNamespace, "Program");
        var definition = image.Reader.GetTypeDefinition(handle);
        DietRequire((int)definition.Attributes == 0x00100180 && definition.GetGenericParameters().Count == 0,
            "program-owner-declaration");
        return handle;
    }

    private static SRM.MethodDefinitionHandle[] DietNamedMethods(DietImage image, SRM.TypeDefinitionHandle owner, string name) =>
        image.Reader.GetTypeDefinition(owner).GetMethods().Where(handle =>
            image.Reader.GetString(image.Reader.GetMethodDefinition(handle).Name) == name).ToArray();

    private static void DietAuthenticateSentinel(DietImage image, SRM.TypeDefinitionHandle program, SRM.MethodDefinitionHandle handle)
    {
        var reader = image.Reader; var method = reader.GetMethodDefinition(handle);
        DietRequire(method.GetDeclaringType() == program, "sentinel-owner");
        DietRequire(reader.GetString(method.Name) == DietSentinelName, "sentinel-name");
        DietRequire((int)method.Attributes == 0x0091 && (int)method.ImplAttributes == 0, "sentinel-flags");
        DietRequire(method.GetGenericParameters().Count == 0 && method.GetParameters().Count == 0
            && reader.GetBlobBytes(method.Signature).SequenceEqual(DietSentinelSignature), "sentinel-signature");
    }

    private static byte[] DietRawBody(DietImage image, SRM.MethodDefinitionHandle handle)
    {
        var method = image.Reader.GetMethodDefinition(handle);
        DietRequire(method.RelativeVirtualAddress != 0, "sentinel-bodyless");
        var body = SRM.PEReaderExtensions.GetMethodBody(image.Pe, method.RelativeVirtualAddress);
        DietRequire(body.ExceptionRegions.Length == 0 && body.LocalSignature.IsNil, "sentinel-body-shape");
        // Cecil's normalized instructions are not authority for the ILDiet marker.
        return body.GetILBytes() ?? throw new DietProofFailure("sentinel-no-il");
    }

    private static string DietEntryPoint(DietImage image, SRM.TypeDefinitionHandle program)
    {
        var header = image.Pe.PEHeaders.CorHeader;
        DietRequire(header is not null && (header.Flags & SRPE.CorFlags.NativeEntryPoint) == 0, "entrypoint-not-managed");
        int token = header!.EntryPointTokenOrRelativeVirtualAddress;
        int row = token & 0x00ffffff;
        DietRequire((token & unchecked((int)0xff000000)) == 0x06000000 && row != 0 && row <= image.Reader.MethodDefinitions.Count,
            "entrypoint-token");
        var method = image.Reader.GetMethodDefinition(SRME.MethodDefinitionHandle(row));
        DietRequire(method.GetDeclaringType() == program && ((int)method.Attributes & 0x10) != 0
            && method.GetGenericParameters().Count == 0, "entrypoint-owner");
        return image.Reader.GetString(method.Name) + "|" + (int)method.Attributes + "|" + (int)method.ImplAttributes
            + "|" + Convert.ToHexString(image.Reader.GetBlobBytes(method.Signature));
    }

    private static string DietValidate(string originalPath, string rewrittenPath)
    {
        using var original = new DietImage(originalPath);
        using var rewritten = new DietImage(rewrittenPath);
        DietRequire(DietAssemblyIdentity(original) == DietAssemblyIdentity(rewritten), "assembly-identity");
        var originalOwner = DietProgram(original); var rewrittenOwner = DietProgram(rewritten);
        DietRequire(DietEntryPoint(original, originalOwner) == DietEntryPoint(rewritten, rewrittenOwner), "entrypoint-declaration");
        var source = DietNamedMethods(original, originalOwner, DietSentinelName);
        DietRequire(source.Length == 1, "original-sentinel-count");
        DietAuthenticateSentinel(original, originalOwner, source[0]);
        DietRequire(DietRawBody(original, source[0]).SequenceEqual(DietOriginalBody), "original-sentinel-not-noop");
        DietRequire(DietHash(originalPath) != DietHash(rewrittenPath), "unchanged-image");
        var kept = DietNamedMethods(rewritten, rewrittenOwner, DietSentinelName);
        DietRequire(kept.Length <= 1, "rewritten-sentinel-count");
        if (kept.Length == 0) return "removed";
        DietAuthenticateSentinel(rewritten, rewrittenOwner, kept[0]);
        DietRequire(DietRawBody(rewritten, kept[0]).SequenceEqual(DietStubBody), "retained-sentinel-not-exact-marker");
        return "exact-ildiet-stub";
    }

    private static bool RunDietProof(string[] args)
    {
        bool proof = args.Length != 0 && args[0] == "--array-native-length-diet-proof";
        bool negatives = args.Length != 0 && args[0] == "--array-native-length-diet-proof-negatives";
        if (!proof && !negatives) return false;
        if (args.Length != (negatives ? 4 : 3)) throw new ArgumentException("Expected ORIGINAL REWRITTEN" + (negatives ? " NEW_OUT_DIR" : ""));
        System.Globalization.CultureInfo.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
        System.Globalization.CultureInfo.CurrentUICulture = System.Globalization.CultureInfo.InvariantCulture;
        string original = Path.GetFullPath(args[1]), rewritten = Path.GetFullPath(args[2]);
        string disposition = DietValidate(original, rewritten);
        if (proof)
        {
            Console.WriteLine(DietLabel + "-diet-proof:changed-image;unused-sentinel=" + disposition + ";metadata-only");
            return true;
        }
        string output = Path.GetFullPath(args[3]);
        if (File.Exists(output) || Directory.Exists(output)) throw new InvalidOperationException("negative output directory must be new");
        Directory.CreateDirectory(output);
        File.WriteAllText(Path.Combine(output, "DO_NOT_EXECUTE.txt"),
            "These assemblies are metadata-negative test data. Never execute them as CLR or native programs.\n");
        File.WriteAllText(Path.Combine(output, "inputs.json"), System.Text.Json.JsonSerializer.Serialize(new {
            original, original_sha256 = DietHash(original), rewritten, rewritten_sha256 = DietHash(rewritten),
            sentinel = disposition, scope = "sentinel-proof-only; manifest/copy/target-body checks remain separate",
            negative_assemblies_must_never_execute = true
        }, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }) + "\n");
        string results = Path.Combine(output, "results.tsv");
        File.WriteAllText(results, "case\tfile\texpected-rejection\tobserved\tsha256\n");
        int checks = 0;
        void Rejected(string label, string path, string expected, Action check)
        {
            string observed = "accepted";
            try { check(); } catch (DietProofFailure error) { observed = error.Code; }
            catch (Exception error)
            {
                File.AppendAllText(results, label + "\t" + Path.GetFileName(path) + "\t" + expected
                    + "\tunexpected:" + error.GetType().FullName + "\t" + DietHash(path) + "\n");
                throw;
            }
            File.AppendAllText(results, label + "\t" + Path.GetFileName(path) + "\t" + expected + "\t" + observed + "\t" + DietHash(path) + "\n");
            Console.WriteLine(DietLabel + "-diet-negative:" + label + ":" + observed);
            DietRequire(observed == expected, "negative-control-" + label);
            checks++;
        }
        foreach (string label in new[] { "original-live-body", "altered-marker", "unrelated-marker-live-sentinel", "wrong-owner", "wrong-name" })
        {
            string path = Path.Combine(output, label + ".metadata-negative.dll");
            DietWriteNegative(original, path, label);
            Rejected(label, path, "retained-sentinel-not-exact-marker", () => { _ = DietValidate(original, path); });
            if (label is "wrong-owner" or "wrong-name")
                Rejected(label + "-declaration", path, label == "wrong-owner" ? "sentinel-owner" : "sentinel-name",
                    () => DietCheckWrongDeclaration(path, label));
        }
        Console.WriteLine(DietLabel + "-diet-proof-negatives:files=5;checks=" + checks + ";metadata-only;never-executed");
        return true;
    }

    private static void DietWriteNegative(string originalPath, string output, string label)
    {
        if (File.Exists(output)) throw new InvalidOperationException("negative assembly must be new");
        using var app = AssemblyDefinition.ReadAssembly(originalPath, new ReaderParameters { InMemory = true });
        var module = app.MainModule;
        var program = module.GetType(DietProgramNamespace + ".Program");
        var sentinel = program.Methods.Single(method => method.Name == DietSentinelName);
        // The extra row ensures a live-body control is not rejected merely as an unchanged PE.
        var other = new MethodDefinition("DietProofUnrelatedMarker", (MethodAttributes)0x0091, module.TypeSystem.Void);
        program.Methods.Add(other);
        void Marker(MethodDefinition method, bool altered = false)
        {
            method.Body = new MethodBody(method);
            var il = method.Body.GetILProcessor();
            il.Emit(OpCodes.Ldc_I4, altered ? 0x494c4455 : 0x494c4454);
            il.Emit(OpCodes.Pop); il.Emit(OpCodes.Ldnull); il.Emit(OpCodes.Throw);
        }
        other.Body = new MethodBody(other);
        other.Body.GetILProcessor().Emit(OpCodes.Ret);
        if (label == "altered-marker") Marker(sentinel, altered: true);
        else if (label == "unrelated-marker-live-sentinel") Marker(other);
        else if (label == "wrong-owner")
        {
            var owner = new TypeDefinition(DietProgramNamespace, "DietProofWrongOwner", (TypeAttributes)0x00100180, module.TypeSystem.Object);
            module.Types.Add(owner); program.Methods.Remove(other); owner.Methods.Add(other);
            other.Name = DietSentinelName; Marker(other);
        }
        else if (label == "wrong-name") { other.Name = DietSentinelName + "Other"; Marker(other); }
        else if (label != "original-live-body") throw new InvalidOperationException("Unknown negative case");
        app.Write(output, new WriterParameters { Timestamp = 0, DeterministicMvid = true });
        // Read back the actual PE bytes before asking the rejection oracle.
        using var image = new DietImage(output);
        var expectedOwner = DietProgram(image);
        var real = DietNamedMethods(image, expectedOwner, DietSentinelName).Single();
        DietAuthenticateSentinel(image, expectedOwner, real);
        byte[] expected = label == "altered-marker" ? new byte[] { 0x20, 0x55, 0x44, 0x4c, 0x49, 0x26, 0x14, 0x7a } : DietOriginalBody;
        DietRequire(DietRawBody(image, real).SequenceEqual(expected), "negative-real-body-readback");
        if (label is "unrelated-marker-live-sentinel" or "wrong-owner" or "wrong-name")
        {
            var otherOwner = label == "wrong-owner" ? DietFindType(image, DietProgramNamespace, "DietProofWrongOwner") : expectedOwner;
            string otherName = label == "wrong-owner" ? DietSentinelName : label == "wrong-name" ? DietSentinelName + "Other" : "DietProofUnrelatedMarker";
            var otherHandle = DietNamedMethods(image, otherOwner, otherName).Single();
            DietRequire(DietRawBody(image, otherHandle).SequenceEqual(DietStubBody), "negative-other-marker-readback");
        }
    }

    private static void DietCheckWrongDeclaration(string path, string label)
    {
        using var image = new DietImage(path);
        _ = DietAssemblyIdentity(image);
        var program = DietProgram(image);
        var owner = label == "wrong-owner" ? DietFindType(image, DietProgramNamespace, "DietProofWrongOwner") : program;
        string name = label == "wrong-name" ? DietSentinelName + "Other" : DietSentinelName;
        var candidates = DietNamedMethods(image, owner, name);
        DietRequire(candidates.Length == 1, "negative-declaration-candidate-count");
        DietAuthenticateSentinel(image, program, candidates[0]);
    }
}

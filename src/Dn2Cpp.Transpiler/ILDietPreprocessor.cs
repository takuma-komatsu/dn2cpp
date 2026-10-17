using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;
using System.Text;

namespace Dn2Cpp;

internal static class ILDietPreprocessor
{
    internal static List<string> Run(IReadOnlyList<string> paths, TranspileOptions options,
        IEmitBackend backend, out string preservation, out bool cutsValidated, out ILDietResultState state)
    {
        string cppDirectory = Path.GetFullPath(options.OutDir);
        string output = Path.GetFullPath(options.ILDietOutput ?? Path.Combine(cppDirectory, "ildiet"));
        if (IsWithin(cppDirectory, output))
            throw new NotSupportedException("--ildiet-output must not contain the C++ output directory");
        Directory.CreateDirectory(cppDirectory);
        string invocation = Guid.NewGuid().ToString("N");
        string requestPath = Path.Combine(cppDirectory, ".ildiet-request-" + invocation + ".xml");
        string resultPath = Path.Combine(cppDirectory, ".ildiet-result-" + invocation + ".xml");
        if (File.Exists(requestPath) || Directory.Exists(requestPath)
            || File.Exists(resultPath) || Directory.Exists(resultPath))
            throw new NotSupportedException("ILDiet protocol filename collision; retry the transpilation");
        var text = new StringBuilder();
        text.Append("<ildiet version=\"2\" input=\"").Append(Escape(Path.GetFullPath(paths[0])))
            .Append("\" output=\"").Append(Escape(output))
            .Append("\" copyAll=\"").Append(options.HotupdateBase ? "true" : "false").Append("\">\n");
        for (int i = 1; i < paths.Count; i++)
            Element(text, "reference", "path", Path.GetFullPath(paths[i]));
        foreach (string assembly in options.ILDietSkipAssemblies)
            Element(text, "skipAssembly", "name", assembly);
        foreach (string path in options.LinkXmlFiles)
            Element(text, "linkXml", "path", Path.GetFullPath(path));
        foreach (string path in options.ProjectRoots)
            Element(text, "projectRoot", "path", Path.GetFullPath(path));
        foreach (string feature in options.LinkFeatures)
            Element(text, "feature", "name", feature);
        foreach (string type in options.ReflectionRoots ?? Array.Empty<string>())
            Element(text, "reflectionRoot", "type", type);
        foreach (string method in options.CutMethods ?? Array.Empty<string>())
            Element(text, "cut", "method", method);
        var policy = new ILDietRootPolicy();
        backend.ConfigureILDiet(policy, paths, options);
        foreach (string assembly in policy.RewriteAssemblies)
            Element(text, "rewriteAssembly", "name", assembly);
        foreach (var root in policy.TypeRoots)
            PolicyElement(text, "typeRoot", root.Assembly, root.Type);
        foreach (var root in policy.FullTypeRoots)
            PolicyElement(text, "root", root.Assembly, root.Type);
        foreach (var root in policy.MethodRoots)
            PolicyElement(text, "methodRoot", root.Assembly, root.Type, "method", root.Method);
        foreach (var members in policy.ConditionalMembers)
            text.Append("  <conditionalMembers assembly=\"").Append(Escape(members.Assembly))
                .Append("\" baseType=\"").Append(Escape(members.BaseType)).Append("\" />\n");
        foreach (var type in policy.SuppressedSeedTypes)
            PolicyElement(text, "suppressDefaultSeeds", type.Assembly, type.Type);
        foreach (var attribute in policy.RegistrationAttributes)
            PolicyElement(text, "registrationAttribute", attribute.Assembly, attribute.Type);
        foreach (var registry in policy.ConstructorRegistries)
            PolicyElement(text, "constructorRegistry", registry.Assembly, registry.Type, "method", registry.Method);
        foreach (var root in policy.Resolve(paths))
            text.Append("  <root assembly=\"").Append(Escape(root.Assembly))
                .Append("\" type=\"").Append(Escape(root.Type)).Append("\" />\n");
        text.Append("</ildiet>\n");
        bool ownsRequest = false;
        bool ownsResult = false;
        try
        {
            File.WriteAllText(requestPath, text.ToString());
            ownsRequest = true;
            string contract = ILDietProtocol.ContractDigest(LinkXml.Parse(requestPath));
            string directory = Path.Combine(AppContext.BaseDirectory, "ildiet");
            string host = Path.Combine(directory, Path.DirectorySeparatorChar == '\\' ? "ILDiet.exe" : "ILDiet");
            int exitCode;
            try
            {
                if (File.Exists(host))
                {
                    ownsResult = true;
                    exitCode = ToolProcess.Run(host, new[] { "--request", requestPath, "--result", resultPath });
                }
                else
                {
                    string dll = Path.Combine(directory, "ILDiet.dll");
                    if (!File.Exists(dll))
                        throw new NotSupportedException(
                            $"ILDiet companion not found: {dll}; restore the CLI installation or use --no-ildiet");
                    ownsResult = true;
                    exitCode = ToolProcess.Run("dotnet", new[] { "exec", dll, "--request", requestPath, "--result", resultPath });
                }
            }
            catch (InvalidOperationException ex)
            {
                throw new NotSupportedException("ILDiet could not start: " + ex.Message, ex);
            }
            if (exitCode != 0)
                throw new NotSupportedException($"ILDiet failed with exit code {exitCode}");
            if (!File.Exists(resultPath))
                throw new NotSupportedException("ILDiet succeeded without writing its result manifest");
            var result = LinkXml.Parse(resultPath);
            Check(result, "ildietResult", "version", "input", "preservation", "generation", "contract",
                "cutsValidated", "constructorRegistriesRewritten", "fallback");
            if (RequiredAttribute(result, "version") != "2")
                throw new NotSupportedException("ILDiet returned an unsupported result version");
            if (RequiredAttribute(result, "contract") != contract)
                throw new NotSupportedException("ILDiet returned a different retention contract");
            string generation = RequiredAttribute(result, "generation");
            if (!generation.StartsWith("g-", StringComparison.Ordinal) || !StableHash.IsHex(generation.Substring(2)))
                throw new NotSupportedException("ILDiet returned an invalid generation");
            string generationDirectory = Path.Combine(output, generation);
            RequireDirectory(generationDirectory);
            RequireDirectory(output);
            cutsValidated = Boolean(result, "cutsValidated");
            bool registries = Boolean(result, "constructorRegistriesRewritten");
            string fallback = RequiredAttribute(result, "fallback");
            if (fallback is not ("none" or "hot-update" or "generic-cut" or "budget"))
                throw new NotSupportedException("ILDiet returned an invalid fallback state");
            var stripped = new List<string> { RequiredAttribute(result, "input") };
            preservation = RequiredAttribute(result, "preservation");
            if (options.HotupdateBase && fallback != "hot-update"
                || !options.HotupdateBase && fallback == "hot-update"
                || fallback == "generic-cut" && cutsValidated
                || !cutsValidated && fallback is not ("hot-update" or "generic-cut"))
                throw new NotSupportedException("ILDiet returned an inconsistent fallback reason");
            var assemblies = new List<LinkNode>();
            LinkNode? counters = null;
            bool countersSeen = false;
            int phase = 0;
            foreach (var child in result.Children)
            {
                if (child.Children.Count != 0)
                    throw new NotSupportedException("ILDiet result entries cannot have children");
                switch (child.Name)
                {
                    case "reference":
                        if (phase != 0) throw new NotSupportedException("ILDiet returned unordered references");
                        Check(child, "reference", "path");
                        stripped.Add(RequiredAttribute(child, "path"));
                        break;
                    case "assembly":
                        if (phase > 1) throw new NotSupportedException("ILDiet returned unordered assemblies");
                        phase = 1;
                        Check(child, "assembly", "name", "mode", "hash", "registryRewritten");
                        assemblies.Add(child);
                        break;
                    case "counters":
                        if (countersSeen || phase > 2) throw new NotSupportedException("ILDiet returned duplicate or unordered counters");
                        phase = 2;
                        countersSeen = true;
                        counters = child;
                        if (!child.Attributes.Keys.SequenceEqual(ILDietProtocol.CounterNames))
                            throw new NotSupportedException("ILDiet returned invalid counter names or order");
                        foreach (var counter in child.Attributes)
                            if (!long.TryParse(counter.Value, System.Globalization.NumberStyles.None,
                                System.Globalization.CultureInfo.InvariantCulture, out long value) || value < 0)
                                throw new NotSupportedException("ILDiet returned an invalid counter: " + counter.Key);
                        break;
                    case "diagnostic":
                        phase = 3;
                        Check(child, "diagnostic", "kind", "scope", "reason");
                        _ = RequiredAttribute(child, "kind");
                        _ = RequiredAttribute(child, "scope");
                        _ = RequiredAttribute(child, "reason");
                        break;
                    default: throw new NotSupportedException("ILDiet returned an unknown result element: " + child.Name);
                }
            }
            if (!countersSeen || stripped.Count != paths.Count || assemblies.Count != paths.Count)
                throw new NotSupportedException("ILDiet changed the number of assemblies or omitted counters");
            RequireOutput(preservation, generationDirectory);
            if (Path.GetFileName(preservation) != "preservation.xml")
                throw new NotSupportedException("ILDiet returned an invalid preservation descriptor path");
            var modes = new List<ILDietAssemblyMode>();
            var generationHash = new StableHash();
            bool anyRegistry = false;
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < stripped.Count; i++)
            {
                RequireOutput(stripped[i], generationDirectory);
                string identity = AssemblyIdentity(paths[i]);
                if (identity != AssemblyIdentity(stripped[i]))
                    throw new NotSupportedException("ILDiet changed assembly identity or load order: " + paths[i]);
                var assembly = assemblies[i];
                string name = RequiredAttribute(assembly, "name");
                string filename = name + ".dll";
                if (name != identity.Substring(0, identity.IndexOf(',')) || !names.Add(name)
                    || Path.GetFileName(stripped[i]) != filename)
                    throw new NotSupportedException("ILDiet returned an invalid assembly name or load order");
                string mode = RequiredAttribute(assembly, "mode");
                if (mode is not ("copied" or "rewritten"))
                    throw new NotSupportedException("ILDiet returned an invalid assembly mode");
                bool rewritten = Boolean(assembly, "registryRewritten");
                if (rewritten && mode != "rewritten" || fallback != "none" && (mode != "copied" || rewritten)
                    || options.ILDietSkipAssemblies.Contains(name) && mode != "copied")
                    throw new NotSupportedException("ILDiet returned an inconsistent assembly rewrite state");
                string hash = RequiredAttribute(assembly, "hash");
                byte[] bytes = File.ReadAllBytes(stripped[i]);
                var actual = new StableHash();
                actual.AddBytes(bytes);
                if (!StableHash.IsHex(hash) || hash != actual.Hex())
                    throw new NotSupportedException("ILDiet returned an invalid assembly hash: " + name);
                if (mode == "copied" && !bytes.SequenceEqual(File.ReadAllBytes(paths[i])))
                    throw new NotSupportedException("ILDiet changed an assembly marked copied: " + name);
                generationHash.AddText(filename);
                generationHash.AddFrame(bytes);
                modes.Add(new ILDietAssemblyMode(name, mode, rewritten));
                anyRegistry |= rewritten;
            }
            generationHash.AddText("preservation.xml");
            generationHash.AddFrame(File.ReadAllBytes(preservation));
            if (generation != "g-" + generationHash.Hex() || anyRegistry != registries)
                throw new NotSupportedException("ILDiet returned an inconsistent generation or registry state");
            if (Directory.EnumerateFileSystemEntries(generationDirectory).Count() != stripped.Count + 1)
                throw new NotSupportedException("ILDiet returned an incomplete or modified generation");
            string pointer = Path.Combine(output, "ildiet-result.xml");
            if (!File.Exists(pointer) || File.ReadAllText(pointer) != File.ReadAllText(resultPath))
                throw new NotSupportedException("ILDiet result does not match its published generation pointer");
            if (counters!.Attr("fallback") != (fallback == "none" ? "0" : "1")
                || counters.Attr("fallback.assemblies") != (fallback == "none" ? "0" : paths.Count.ToString(System.Globalization.CultureInfo.InvariantCulture)))
                throw new NotSupportedException("ILDiet returned inconsistent fallback counters");
            state = new ILDietResultState(registries, modes, fallback);
            if (fallback != "none")
                Console.Error.WriteLine("[dn2cpp] ILDiet used copy-all fallback; original assembly bytes were preserved.");
            return stripped;
        }
        finally
        {
            if (ownsRequest && File.Exists(requestPath))
                File.Delete(requestPath);
            if (ownsResult && File.Exists(resultPath))
                File.Delete(resultPath);
        }
    }

    private static void PolicyElement(StringBuilder text, string element, string assembly, string type,
        string? attribute = null, string? value = null)
    {
        text.Append("  <").Append(element).Append(" assembly=\"").Append(Escape(assembly))
            .Append("\" type=\"").Append(Escape(type)).Append('"');
        if (attribute is not null)
            text.Append(' ').Append(attribute).Append("=\"").Append(Escape(value!)).Append('"');
        text.Append(" />\n");
    }

    private static string AssemblyIdentity(string path)
    {
        using var pe = new PEReader(ImmutableCollectionsMarshal.AsImmutableArray(File.ReadAllBytes(path)));
        var reader = pe.GetMetadataReader();
        var assembly = reader.GetAssemblyDefinition();
        var identity = new StringBuilder(reader.GetString(assembly.Name));
        var version = assembly.Version;
        identity.Append(',').Append(version.Major).Append('.').Append(version.Minor)
            .Append('.').Append(version.Build).Append('.').Append(version.Revision)
            .Append(',').Append(reader.GetString(assembly.Culture)).Append(',');
        foreach (byte value in reader.GetBlobBytes(assembly.PublicKey))
        {
            identity.Append("0123456789abcdef"[value >> 4]);
            identity.Append("0123456789abcdef"[value & 15]);
        }
        return identity.ToString();
    }

    private static void RequireOutput(string path, string directory)
    {
        if (Path.GetDirectoryName(Path.GetFullPath(path)) != Path.GetFullPath(directory)
            || !File.Exists(path) || new FileInfo(path).LinkTarget is not null)
            throw new NotSupportedException("ILDiet result is missing or outside its output directory: " + path);
    }

    private static void RequireDirectory(string directory)
    {
        if (!Directory.Exists(directory) || new DirectoryInfo(directory).LinkTarget is not null)
            throw new NotSupportedException("ILDiet generation is missing or a symbolic link");
    }

    private static bool Boolean(LinkNode node, string name) => RequiredAttribute(node, name) switch
    {
        "true" => true,
        "false" => false,
        _ => throw new NotSupportedException("ILDiet returned an invalid boolean: " + name),
    };

    private static void Check(LinkNode node, string name, params string[] attributes)
    {
        if (node.Name != name) throw new NotSupportedException("ILDiet returned an invalid result element");
        foreach (string attribute in node.Attributes.Keys)
            if (!attributes.Contains(attribute))
                throw new NotSupportedException("ILDiet returned an unknown result attribute: " + attribute);
    }

    private static bool IsWithin(string path, string directory)
    {
        string root = directory.TrimEnd(new[] { '/', '\\' });
        var comparison = Path.DirectorySeparatorChar == '\\'
            ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        return string.Equals(path, root, comparison)
            || path.StartsWith(root + Path.DirectorySeparatorChar, comparison);
    }

    private static string RequiredAttribute(LinkNode node, string name)
    {
        string? value = node.Attr(name);
        if (string.IsNullOrEmpty(value))
            throw new NotSupportedException("ILDiet result is missing '" + name + "'");
        return value;
    }

    private static void Element(StringBuilder text, string element, string attribute, string value)
    {
        text.Append("  <").Append(element).Append(' ').Append(attribute)
            .Append("=\"").Append(Escape(value)).Append("\" />\n");
    }

    private static string Escape(string value) => value.Replace("&", "&amp;")
        .Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;")
        .Replace("\r", "&#13;").Replace("\n", "&#10;").Replace("\t", "&#9;");
}

internal sealed record ILDietAssemblyMode(string Name, string Mode, bool RegistryRewritten);

internal sealed record ILDietResultState(bool RegistriesRewritten, IReadOnlyList<ILDietAssemblyMode> Assemblies, string Fallback);

using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;
using System.Text;

namespace Dn2Cpp;

internal static class ILDietPreprocessor
{
    internal static List<string> Run(IReadOnlyList<string> paths, TranspileOptions options,
        IEmitBackend backend, out string preservation, out bool cutsValidated)
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
        text.Append("<ildiet input=\"").Append(Escape(Path.GetFullPath(paths[0])))
            .Append("\" output=\"").Append(Escape(output))
            .Append("\" copyAll=\"").Append(options.HotupdateBase ? "true" : "false").Append("\">\n");
        for (int i = 1; i < paths.Count; i++)
            Element(text, "reference", "path", Path.GetFullPath(paths[i]));
        foreach (string path in options.LinkXmlFiles)
            Element(text, "linkXml", "path", Path.GetFullPath(path));
        foreach (string path in options.ProjectRoots)
            Element(text, "projectRoot", "path", Path.GetFullPath(path));
        foreach (string feature in options.LinkFeatures)
            Element(text, "feature", "name", feature);
        foreach (string type in options.ReflectionRoots ?? Array.Empty<string>())
            Element(text, "root", "type", type);
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
            if (result.Name != "ildietResult")
                throw new NotSupportedException("ILDiet returned an invalid result manifest");
            cutsValidated = RequiredAttribute(result, "cutsValidated") switch
            {
                "true" => true,
                "false" => false,
                _ => throw new NotSupportedException("ILDiet returned an invalid cut-validation state"),
            };
            bool registriesRewritten = RequiredAttribute(result, "constructorRegistriesRewritten") switch
            {
                "true" => true,
                "false" => false,
                _ => throw new NotSupportedException("ILDiet returned an invalid constructor-registry state"),
            };
            var stripped = new List<string> { RequiredAttribute(result, "input") };
            preservation = RequiredAttribute(result, "preservation");
            foreach (var child in result.Children)
            {
                if (child.Name != "reference")
                    throw new NotSupportedException("ILDiet returned an unknown result element: " + child.Name);
                stripped.Add(RequiredAttribute(child, "path"));
            }
            if (stripped.Count != paths.Count)
                throw new NotSupportedException("ILDiet changed the number of assemblies in the load set");
            RequireOutput(preservation, output);
            for (int i = 0; i < stripped.Count; i++)
            {
                RequireOutput(stripped[i], output);
                if (AssemblyIdentity(paths[i]) != AssemblyIdentity(stripped[i]))
                    throw new NotSupportedException("ILDiet changed assembly identity or load order: " + paths[i]);
            }
            backend.ILDietCompleted(registriesRewritten);
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
        if (!IsWithin(Path.GetFullPath(path), directory) || !File.Exists(path))
            throw new NotSupportedException("ILDiet result is missing or outside its output directory: " + path);
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

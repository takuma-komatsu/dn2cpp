using System.Text;
using Mono.Cecil;
using PreserveKind = Dn2Cpp.PreservationReader.PreserveKind;
using PreservePolicy = Dn2Cpp.PreservationReader.PreservePolicy;
using SRME = System.Reflection.Metadata.Ecma335.MetadataTokens;

namespace Dn2Cpp;

internal sealed partial class AssemblyDiet
{
    private const string OutputMarker = ".ildiet-output";
    private const string OutputOwnership = "ILDiet managed assembly output\n";

    private void WriteOutputs(string? resultPath)
    {
        string output = Path.GetFullPath(_request.Output);
        string physicalOutput = PhysicalPath(output);
        var sourceFiles = _assemblies.Select(a => PhysicalPath(a.Path)).ToHashSet(PathComparer);
        foreach (string descriptor in PreservationReader.FindLinkFiles(_request.ProjectRoots).Concat(_request.LinkXml))
            sourceFiles.Add(PhysicalPath(descriptor));
        if (_request.RequestFile is not null) sourceFiles.Add(PhysicalPath(_request.RequestFile));
        foreach (string input in sourceFiles)
            if (PathContains(physicalOutput, input))
                throw new NotSupportedException("ILDiet output would replace an input: " + input);
        if (File.Exists(output)) throw new NotSupportedException("ILDiet output is a file: " + output);
        if (Directory.Exists(output) && Directory.EnumerateFileSystemEntries(output).Any()
            && (!File.Exists(Path.Combine(output, OutputMarker))
                || File.ReadAllText(Path.Combine(output, OutputMarker)) != OutputOwnership))
            throw new NotSupportedException("ILDiet output contains files it does not own: " + output);
        if (Directory.Exists(output) && new DirectoryInfo(output).LinkTarget is not null)
            throw new NotSupportedException("ILDiet output cannot be a symbolic link: " + output);

        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var files = new List<string>();
        foreach (var assembly in _assemblies)
        {
            string name = assembly.Assembly.Name.Name + ".dll";
            if (Path.GetFileName(name) != name || name.IndexOfAny(new[] { '/', '\\', ':', '\0' }) >= 0
                || !names.Add(name))
                throw new NotSupportedException("Assemblies cannot share or escape an output filename: " + name);
            files.Add(name);
        }
        string pointer = Path.Combine(output, "ildiet-result.xml");
        string? result = resultPath is null ? null : Path.GetFullPath(resultPath);
        if (result is not null)
        {
            string physicalResult = PhysicalPath(result);
            if (PathContains(output, result) && !PathContains(physicalOutput, physicalResult))
                throw new NotSupportedException("ILDiet result escapes its output through a symbolic link: " + result);
            if (PathContains(physicalOutput, physicalResult))
            {
                string relative = Path.GetRelativePath(physicalOutput, physicalResult);
                string[] parts = relative.Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 0 || parts.Any(part => part.StartsWith(".", StringComparison.Ordinal)
                    || part.StartsWith("g-", StringComparison.OrdinalIgnoreCase))
                    || !relative.EndsWith(".xml", StringComparison.OrdinalIgnoreCase)
                    || PathComparer.Equals(relative, "preservation.xml"))
                    throw new NotSupportedException("ILDiet result would replace a reserved output: " + result);
            }
            if (sourceFiles.Contains(physicalResult) || Directory.Exists(result))
                throw new NotSupportedException("ILDiet result would replace an input or directory: " + result);
        }

        Directory.CreateDirectory(output);
        if (!File.Exists(Path.Combine(output, OutputMarker)))
            WriteText(Path.Combine(output, OutputMarker), OutputOwnership);
        string stage = Path.Combine(output, ".stage-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(stage);
        string? pointerTemporary = null, resultTemporary = null;
        try
        {
            var hashes = new List<string>();
            var generationHash = new StableHash();
            for (int i = 0; i < _assemblies.Count; i++)
            {
                var assembly = _assemblies[i];
                string path = Path.Combine(stage, files[i]);
                if (assembly.Copy) File.Copy(assembly.Path, path);
                else
                {
                    PreserveArrayKinds(assembly);
                    assembly.Assembly.MainModule.Attributes &= ~ModuleAttributes.StrongNameSigned;
                    assembly.Assembly.Write(path, new WriterParameters { Timestamp = 0, DeterministicMvid = true });
                }
                using var verified = AssemblyDefinition.ReadAssembly(path);
                if (verified.Name.FullName != assembly.Assembly.Name.FullName)
                    throw new InvalidOperationException("Writing changed assembly identity: " + assembly.Path);
                byte[] bytes = File.ReadAllBytes(path);
                var fileHash = new StableHash();
                fileHash.AddBytes(bytes);
                hashes.Add(fileHash.Hex());
                generationHash.AddText(files[i]);
                generationHash.AddFrame(bytes);
            }
            var typeNames = TypeNameSnapshot.Capture(_assemblies.Select(assembly =>
                (assembly.Assembly.Name.Name, assembly.Path)).ToArray());
            byte[] descriptor = new UTF8Encoding(false).GetBytes((_originalPreservationText
                ?? throw new InvalidOperationException("Original preservation was not captured before publication"))
                + typeNames.Comment());
            File.WriteAllBytes(Path.Combine(stage, "preservation.xml"), descriptor);
            generationHash.AddText("preservation.xml");
            generationHash.AddFrame(descriptor);
            string generation = "g-" + generationHash.Hex();
            string generationDirectory = Path.Combine(output, generation);
            FailPublication("write");
            if (Directory.Exists(generationDirectory))
            {
                if (new DirectoryInfo(generationDirectory).LinkTarget is not null
                    || Directory.EnumerateFileSystemEntries(generationDirectory).Count() != files.Count + 1)
                    throw new InvalidOperationException("ILDiet generation is not immutable: " + generation);
                foreach (string name in files.Concat(new[] { "preservation.xml" }))
                {
                    string existing = Path.Combine(generationDirectory, name);
                    if (!File.Exists(existing) || new FileInfo(existing).LinkTarget is not null
                        || !File.ReadAllBytes(existing).SequenceEqual(File.ReadAllBytes(Path.Combine(stage, name))))
                        throw new InvalidOperationException("ILDiet generation hash collision or modified output: " + generation);
                }
                CleanupDirectory(stage);
            }
            else Directory.Move(stage, generationDirectory);
            string manifest = ResultManifest(files.Select(name => Path.Combine(generationDirectory, name)).ToList(),
                Path.Combine(generationDirectory, "preservation.xml"), generation, hashes);
            pointerTemporary = pointer + "." + Guid.NewGuid().ToString("N") + ".tmp";
            WriteText(pointerTemporary, manifest);
            // Different spellings may name distinct files on a case-sensitive volume.
            if (result is not null && !StringComparer.Ordinal.Equals(PhysicalPath(result), PhysicalPath(pointer)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(result)!);
                resultTemporary = result + "." + Guid.NewGuid().ToString("N") + ".tmp";
                WriteText(resultTemporary, manifest);
            }
            FailPublication("commit");
            File.Move(pointerTemporary, pointer, overwrite: true);
            pointerTemporary = null;
            if (resultTemporary is not null)
            {
                File.Move(resultTemporary, result!, overwrite: true);
                resultTemporary = null;
            }
            CleanupOutputs(output, generation, result);
        }
        finally
        {
            CleanupDirectory(stage);
            CleanupFile(pointerTemporary);
            CleanupFile(resultTemporary);
        }
    }

    private static void FailPublication(string phase)
    {
        if (EnvKnobs.Raw(EnvKnobs.ILDietFailPoint) == phase)
            throw new InvalidOperationException("ILDiet injected publication failure: " + phase);
    }

    private static void CleanupFile(string? path)
    {
        if (path is null) return;
        try { if (File.Exists(path)) File.Delete(path); }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }

    private static void CleanupOutputs(string output, string generation, string? result)
    {
        try
        {
            foreach (string path in Directory.EnumerateFileSystemEntries(output))
            {
                string name = Path.GetFileName(path);
                if (name == generation || name == OutputMarker || name == "ildiet-result.xml"
                    || result is not null && PathComparer.Equals(PhysicalPath(path), PhysicalPath(result)))
                    continue;
                if (Directory.Exists(path) && (name.StartsWith("g-", StringComparison.Ordinal)
                    || name.StartsWith(".stage-", StringComparison.Ordinal))) CleanupDirectory(path);
                else if (File.Exists(path) && (name.EndsWith(".tmp", StringComparison.Ordinal)
                    || name.EndsWith(".dll", StringComparison.OrdinalIgnoreCase)
                    || name.EndsWith(".pdb", StringComparison.OrdinalIgnoreCase) || name == "preservation.xml"))
                    CleanupFile(path);
            }
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }

    private string EffectivePreservation()
    {
        var text = new StringBuilder("<linker>\n");
        foreach (var assembly in _assemblies)
        {
            if (assembly.PreserveAll)
            {
                text.Append("  <assembly fullname=\"").Append(Xml(assembly.Assembly.Name.Name))
                    .Append("\" preserve=\"all\" />\n");
                continue;
            }
            var types = new StringBuilder();
            foreach (var pair in _exportPolicyTypes.Where(p => p.Key.Module == assembly.Assembly.MainModule)
                         .OrderBy(p => p.Key.MetadataToken.ToInt32()))
            {
                var type = pair.Key;
                var policy = pair.Value;
                for (int branch = 0; branch < 2; branch++)
                {
                    bool conditional = branch != 0;
                    if (conditional && _fallback == "none" && !assembly.Copy && !_types.Contains(type))
                        continue;
                    var kind = conditional ? policy.ConditionalKind : policy.Kind;
                    var selectedMethods = conditional ? policy.ConditionalMethods : policy.Methods;
                    var selectedFields = conditional ? policy.ConditionalFields : policy.Fields;
                    var selectedProperties = conditional ? policy.ConditionalProperties : policy.Properties;
                    var selectedEvents = conditional ? policy.ConditionalEvents : policy.Events;
                    var members = new StringBuilder();
                    bool KeptMethod(MethodDefinition? method) => method is not null
                        && ((kind & PreserveKind.Methods) != 0
                            || ((kind & PreserveKind.DefaultConstructor) != 0 && method.IsConstructor
                                && !method.IsStatic && method.Parameters.Count == 0)
                            || selectedMethods.Contains(SRME.MethodDefinitionHandle((int)method.MetadataToken.RID)));
                    foreach (var method in type.Methods)
                        if (KeptMethod(method))
                            Member(members, "method", SignatureType(method.ReturnType) + " " + method.Name
                                + "(" + string.Join(",", method.Parameters.Select(p => SignatureType(p.ParameterType))) + ")");
                    foreach (var field in type.Fields)
                        if ((kind & PreserveKind.Fields) != 0
                            || selectedFields.Contains(SRME.FieldDefinitionHandle((int)field.MetadataToken.RID)))
                            Member(members, "field", SignatureType(field.FieldType) + " " + field.Name);
                    foreach (var property in type.Properties)
                        if (selectedProperties.Contains(SRME.PropertyDefinitionHandle((int)property.MetadataToken.RID)))
                        {
                            bool getter = KeptMethod(property.GetMethod), setter = KeptMethod(property.SetMethod);
                            string accessors;
                            if (getter && setter) accessors = "all";
                            else if (getter) accessors = "get";
                            else if (setter) accessors = "set";
                            else if (property.GetMethod is null && property.SetMethod is null) accessors = "all";
                            else if (property.GetMethod is null) accessors = "get";
                            else if (property.SetMethod is null) accessors = "set";
                            else throw new InvalidOperationException("Preserved property selects no accessor: " + property.FullName);
                            // A missing accessor selector preserves the row without
                            // inventing executable preservation for the other side.
                            Member(members, "property", SignatureType(property.PropertyType) + " " + property.Name, accessors);
                        }
                    foreach (var item in type.Events)
                        if (selectedEvents.Contains(SRME.EventDefinitionHandle((int)item.MetadataToken.RID)))
                            Member(members, "event", SignatureType(item.EventType) + " " + item.Name);
                    if (kind == PreserveKind.None && members.Length == 0) continue;
                    types.Append("    <type fullname=\"").Append(Xml(type.FullName.Replace('/', '+')))
                        .Append("\" preserve=\"nothing\"");
                    if (conditional) types.Append(" required=\"0\"");
                    types.Append(">\n").Append(members).Append("    </type>\n");
                }
            }
            if (types.Length == 0) continue;
            text.Append("  <assembly fullname=\"").Append(Xml(assembly.Assembly.Name.Name))
                .Append("\" preserve=\"nothing\">\n").Append(types).Append("  </assembly>\n");
        }
        return text.Append("</linker>\n").ToString();
    }

    private static string SignatureType(TypeReference type) => type switch
    {
        GenericParameter parameter => parameter.Name,
        GenericInstanceType generic => SignatureType(generic.ElementType) + "<"
            + string.Join(",", generic.GenericArguments.Select(SignatureType)) + ">",
        ArrayType array => SignatureType(array.ElementType) + "[" + new string(',', array.Rank - 1) + "]",
        ByReferenceType reference => SignatureType(reference.ElementType) + "&",
        PointerType pointer => SignatureType(pointer.ElementType) + "*",
        FunctionPointerType => "method*",
        OptionalModifierType modifier => SignatureType(modifier.ElementType),
        RequiredModifierType modifier => SignatureType(modifier.ElementType),
        PinnedType pinned => SignatureType(pinned.ElementType),
        SentinelType sentinel => SignatureType(sentinel.ElementType),
        _ => type.FullName.Replace('/', '+'),
    };

    private static void Member(StringBuilder text, string kind, string signature, string? accessors = null)
    {
        text.Append("      <").Append(kind).Append(" signature=\"").Append(Xml(signature)).Append('"');
        if (accessors is not null) text.Append(" accessors=\"").Append(accessors).Append('"');
        text.Append(" />\n");
    }

    private string ResultManifest(IReadOnlyList<string> assemblies, string preservation,
        string generation, IReadOnlyList<string> hashes)
    {
        string contract = _request.Contract;
        if (contract.Length == 0)
        {
            var request = new LinkNode { Name = "ildiet" };
            request.Attributes.Add("copyAll", _request.CopyAll ? "true" : "false");
            foreach (string feature in _request.Features)
            {
                var child = new LinkNode { Name = "feature" };
                child.Attributes.Add("name", feature);
                request.Children.Add(child);
            }
            foreach (string name in _request.SkipAssemblies)
            {
                var child = new LinkNode { Name = "skipAssembly" };
                child.Attributes.Add("name", name);
                request.Children.Add(child);
            }
            contract = ILDietProtocol.ContractDigest(request);
        }
        var text = new StringBuilder("<ildietResult version=\"2\" input=\"").Append(Xml(assemblies[0]))
            .Append("\" preservation=\"").Append(Xml(preservation))
            .Append("\" generation=\"").Append(generation)
            .Append("\" contract=\"").Append(contract)
            .Append("\" fallback=\"").Append(_fallback)
            .Append("\" cutsValidated=\"").Append(_cutsValidated ? "true" : "false")
            .Append("\" constructorRegistriesRewritten=\"").Append(_constructorRegistriesRewritten ? "true" : "false")
            .Append("\">\n");
        for (int i = 1; i < assemblies.Count; i++)
            text.Append("  <reference path=\"").Append(Xml(assemblies[i])).Append("\" />\n");
        for (int i = 0; i < assemblies.Count; i++)
            text.Append("  <assembly name=\"").Append(Xml(_assemblies[i].Assembly.Name.Name))
                .Append("\" mode=\"").Append(_assemblies[i].Copy ? "copied" : "rewritten")
                .Append("\" hash=\"").Append(hashes[i]).Append("\" registryRewritten=\"")
                .Append(_assemblies[i].RegistryRewritten ? "true" : "false").Append("\" />\n");
        text.Append("  <counters");
        foreach (var counter in GraphCounters())
            text.Append(' ').Append(counter.Key).Append("=\"").Append(counter.Value).Append('"');
        text.Append(" />\n");
        foreach (var diagnostic in _registryDiagnostics.OrderBy(item => item.Scope, StringComparer.Ordinal)
            .ThenBy(item => item.Reason, StringComparer.Ordinal))
            text.Append("  <diagnostic kind=\"registry\" scope=\"").Append(Xml(diagnostic.Scope))
                .Append("\" reason=\"").Append(Xml(diagnostic.Reason)).Append("\" />\n");
        return text.Append("</ildietResult>\n").ToString();
    }

    // Any host can mount a case-insensitive volume. Reject case-only overlaps
    // conservatively so replacing outputs cannot overwrite aliased inputs.
    private static StringComparer PathComparer => StringComparer.OrdinalIgnoreCase;

    private static bool PathContains(string directory, string path) => PathComparer.Equals(directory, path)
        || path.StartsWith(Path.TrimEndingDirectorySeparator(directory) + Path.DirectorySeparatorChar,
            StringComparison.OrdinalIgnoreCase);

    private static string PhysicalPath(string path)
    {
        string full = Path.GetFullPath(path);
        string current = Path.GetPathRoot(full)!;
        foreach (string part in full.Substring(current.Length).Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries))
        {
            current = Path.Combine(current, part);
            FileSystemInfo entry = Directory.Exists(current) ? new DirectoryInfo(current) : new FileInfo(current);
            if (entry.Exists && entry.ResolveLinkTarget(returnFinalTarget: true) is { } target)
                current = target.FullName;
        }
        return current;
    }

    private static string Xml(string value) => DietRequest.Escape(value)
        .Replace("\r", "&#13;").Replace("\n", "&#10;").Replace("\t", "&#9;");

    private static void WriteText(string path, string text) => File.WriteAllText(path, text, new UTF8Encoding(false));

    private static void CleanupDirectory(string path)
    {
        try { if (Directory.Exists(path)) Directory.Delete(path, recursive: true); }
        catch (IOException ex) { Console.Error.WriteLine("ILDiet: could not remove staging directory: " + ex.Message); }
        catch (UnauthorizedAccessException ex) { Console.Error.WriteLine("ILDiet: could not remove staging directory: " + ex.Message); }
    }
}

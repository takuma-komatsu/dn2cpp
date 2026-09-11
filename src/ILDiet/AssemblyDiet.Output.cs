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
        if (_request.RequestFile is not null)
            sourceFiles.Add(PhysicalPath(_request.RequestFile));
        foreach (string input in sourceFiles)
            if (PathContains(physicalOutput, input))
                throw new NotSupportedException("ILDiet output would replace an input: " + input);
        if (File.Exists(output))
            throw new NotSupportedException("ILDiet output is a file: " + output);
        if (Directory.Exists(output) && Directory.EnumerateFileSystemEntries(output).Any()
            && (!File.Exists(Path.Combine(output, OutputMarker))
                || File.ReadAllText(Path.Combine(output, OutputMarker)) != OutputOwnership))
            throw new NotSupportedException("ILDiet output contains files it does not own: " + output);
        if (Directory.Exists(output) && new DirectoryInfo(output).LinkTarget is not null)
            throw new NotSupportedException("ILDiet output cannot be a symbolic link: " + output);

        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var outputFiles = new List<string>();
        foreach (var assembly in _assemblies)
        {
            string name = assembly.Assembly.Name.Name + ".dll";
            if (Path.GetFileName(name) != name || name.IndexOfAny(new[] { '/', '\\', ':', '\0' }) >= 0
                || !names.Add(name))
                throw new NotSupportedException("Assemblies cannot share or escape an output filename: " + name);
            outputFiles.Add(Path.Combine(output, name));
        }
        string preservation = Path.Combine(output, "preservation.xml");
        string? result = resultPath is null ? null : Path.GetFullPath(resultPath);
        string? resultWithinOutput = null;
        if (result is not null)
        {
            string physicalResult = PhysicalPath(result);
            if (PathContains(output, result) && !PathContains(physicalOutput, physicalResult))
                throw new NotSupportedException("ILDiet result escapes its output through a symbolic link: " + result);
            if (PathContains(physicalOutput, physicalResult))
            {
                string prefix = Path.TrimEndingDirectorySeparator(physicalOutput) + Path.DirectorySeparatorChar;
                if (!physicalResult.StartsWith(prefix, StringComparison.Ordinal))
                    throw new NotSupportedException("ILDiet result must use its output directory's exact path casing: " + result);
                resultWithinOutput = physicalResult.Substring(prefix.Length);
            }
            if (sourceFiles.Contains(physicalResult)
                || outputFiles.Any(path => PathComparer.Equals(PhysicalPath(path), physicalResult))
                || PathComparer.Equals(PhysicalPath(preservation), physicalResult)
                || PathComparer.Equals(PhysicalPath(Path.Combine(output, OutputMarker)), physicalResult))
                throw new NotSupportedException("ILDiet result would replace an input or assembly output: " + result);
            if (Directory.Exists(result))
                throw new NotSupportedException("ILDiet result path is a directory: " + result);
        }

        // Policy tokens still describe the original metadata until Cecil writes.
        string descriptorText = EffectivePreservation();
        string resultText = ResultManifest(outputFiles, preservation);
        string parent = Path.GetDirectoryName(output)
            ?? throw new NotSupportedException("ILDiet output must have a parent directory");
        Directory.CreateDirectory(parent);
        string stage = Path.Combine(parent, ".ildiet-stage-" + Guid.NewGuid().ToString("N"));
        string backup = Path.Combine(parent, ".ildiet-backup-" + Guid.NewGuid().ToString("N"));
        string? stagedResult = null;
        bool movedOriginal = false, published = false, committed = false;
        Directory.CreateDirectory(stage);
        try
        {
            for (int i = 0; i < _assemblies.Count; i++)
            {
                var assembly = _assemblies[i];
                string path = Path.Combine(stage, Path.GetFileName(outputFiles[i]));
                if (assembly.Copy)
                    File.Copy(assembly.Path, path);
                else
                {
                    assembly.Assembly.MainModule.Attributes &= ~ModuleAttributes.StrongNameSigned;
                    assembly.Assembly.Write(path, new WriterParameters { Timestamp = 0, DeterministicMvid = true });
                }
                using var verified = AssemblyDefinition.ReadAssembly(path);
                if (verified.Name.FullName != assembly.Assembly.Name.FullName)
                    throw new InvalidOperationException("Writing changed assembly identity: " + assembly.Path);
            }
            WriteText(Path.Combine(stage, "preservation.xml"), descriptorText);
            WriteText(Path.Combine(stage, OutputMarker), OutputOwnership);
            if (result is not null)
            {
                if (resultWithinOutput is not null)
                {
                    string inside = Path.Combine(stage, resultWithinOutput);
                    Directory.CreateDirectory(Path.GetDirectoryName(inside)!);
                    WriteText(inside, resultText);
                }
                else
                {
                    string resultParent = Path.GetDirectoryName(result)!;
                    Directory.CreateDirectory(resultParent);
                    stagedResult = Path.Combine(resultParent, ".ildiet-result-" + Guid.NewGuid().ToString("N"));
                    WriteText(stagedResult, resultText);
                }
            }
            if (Directory.Exists(output))
            {
                Directory.Move(output, backup);
                movedOriginal = true;
            }
            Directory.Move(stage, output);
            published = true;
            if (stagedResult is not null)
                File.Move(stagedResult, result!, overwrite: true);
            committed = true;
        }
        catch
        {
            if (published)
                Directory.Delete(output, recursive: true);
            if (movedOriginal)
                Directory.Move(backup, output);
            throw;
        }
        finally
        {
            CleanupDirectory(stage);
            if (committed) CleanupDirectory(backup);
            if (stagedResult is not null && File.Exists(stagedResult))
            {
                try { File.Delete(stagedResult); }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
            }
        }
    }

    private string EffectivePreservation()
    {
        var text = new StringBuilder("<linker>\n");
        foreach (var assembly in _assemblies)
        {
            var types = new StringBuilder();
            foreach (var pair in _exportPolicyTypes.Where(p => p.Key.Module == assembly.Assembly.MainModule)
                         .OrderBy(p => p.Key.MetadataToken.ToInt32()))
            {
                var type = pair.Key;
                if (!_types.Contains(type) && !assembly.Copy) continue;
                var policy = pair.Value;
                for (int branch = 0; branch < 2; branch++)
                {
                    bool conditional = branch != 0;
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
                            Member(members, "property", SignatureType(property.PropertyType) + " " + property.Name,
                                getter && !setter ? "get" : setter && !getter ? "set" : "all");
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

    private string ResultManifest(IReadOnlyList<string> assemblies, string preservation)
    {
        var text = new StringBuilder("<ildietResult input=\"").Append(Xml(assemblies[0]))
            .Append("\" preservation=\"").Append(Xml(preservation))
            .Append("\" cutsValidated=\"").Append(_cutsValidated ? "true" : "false")
            .Append("\" constructorRegistriesRewritten=\"").Append(_constructorRegistriesRewritten ? "true" : "false")
            .Append("\">\n");
        for (int i = 1; i < assemblies.Count; i++)
            text.Append("  <reference path=\"").Append(Xml(assemblies[i])).Append("\" />\n");
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

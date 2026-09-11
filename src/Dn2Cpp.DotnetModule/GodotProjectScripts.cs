using System.Text;

namespace Dn2Cpp.DotnetModule;

/// <summary>Finds externally loaded scripts before ILDiet can remove their metadata.</summary>
internal static class GodotProjectScripts
{
    private sealed class Project
    {
        internal readonly string Root;
        internal readonly Dictionary<string, string> Files = new(StringComparer.Ordinal);
        internal readonly Dictionary<string, string> Uids = new(StringComparer.Ordinal);
        internal readonly HashSet<string> Scripts = new(StringComparer.Ordinal);
        internal readonly HashSet<string> Identifiers = new(StringComparer.Ordinal);
        internal readonly HashSet<string> KnownResources = new(StringComparer.Ordinal);
        internal readonly List<(string Source, string Path, string Uid)> References = new();
        internal readonly List<(string Source, string Path)> ScriptLiterals = new();
        internal readonly SortedSet<string> Failures = new(StringComparer.Ordinal);

        internal Project(string root) => Root = root;
    }

    internal static void Configure(ILDietRootPolicy policy, GodotILDietMetadata metadata,
        TranspileOptions options)
    {
        if (options.ProjectRoots.Count == 0)
        {
            policy.ExcludedAssemblies.Add("GodotSharp");
            policy.BaseTypes.Add("Godot.GodotObject");
            Console.Error.WriteLine("[ILDiet] Godot script discovery: no --project-root; keeping all user Godot types.");
            return;
        }

        var projects = new List<Project>();
        foreach (string root in options.ProjectRoots)
        {
            var project = new Project(Path.GetFullPath(root));
            projects.Add(project);
            try
            {
                Enumerate(project, project.Root, options);
                if (!project.Files.ContainsKey("res://project.godot"))
                    project.Failures.Add("project.godot is missing");
                foreach (var file in project.Files.OrderBy(pair => pair.Key, StringComparer.Ordinal))
                    Inspect(project, file.Key, file.Value);
                foreach (var reference in project.References)
                    Resolve(project, reference.Source, reference.Path, reference.Uid);
                foreach (var literal in project.ScriptLiterals)
                    ResolveScriptLiteral(project, literal.Source, literal.Path);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                project.Failures.Add("project files cannot be read: " + ex.Message);
            }
        }

        var mappedPaths = new HashSet<string>(StringComparer.Ordinal);
        foreach (var row in metadata.Types.Values)
            foreach (string path in row.ScriptPaths)
                mappedPaths.Add(path);
        foreach (var project in projects)
            foreach (string path in project.Scripts)
                if (!mappedPaths.Contains(path))
                    project.Failures.Add("script has no matching ScriptPathAttribute in the load set: " + path);

        foreach (var row in metadata.Types.Values)
        {
            if (row.Identity.Assembly == "GodotSharp"
                || (row.ScriptPaths.Count == 0 && !row.InvalidScriptPath && !row.GlobalClass))
                continue;
            bool associated = false;
            bool preserve = row.InvalidScriptPath;
            foreach (var project in projects)
            {
                bool belongs = row.ScriptPaths.Any(path => project.Files.ContainsKey(path)
                    || File.Exists(Path.Combine(project.Root, path.Substring(6))));
                associated |= belongs;
                preserve |= belongs && project.Failures.Count != 0;
                preserve |= row.ScriptPaths.Any(project.Scripts.Contains);
                if (row.GlobalClass)
                {
                    string name = row.Identity.Type;
                    int separator = Math.Max(name.LastIndexOf('.'), name.LastIndexOf('+'));
                    preserve |= project.Identifiers.Contains(name.Substring(separator + 1));
                }
            }
            // A DLL whose source tree is absent cannot be assigned to a successfully scanned project.
            if (!associated)
            {
                preserve = true;
                Console.Error.WriteLine("[ILDiet] Godot script discovery: keeping " + row.Identity.Type
                    + " (script source is absent from the supplied projects).");
            }
            if (preserve)
                policy.TypeRoots.Add(row.Identity);
        }
        foreach (var project in projects)
        {
            var compressed = project.Failures.Where(reason => reason.StartsWith("compressed binary resource ",
                StringComparison.Ordinal)).ToList();
            if (compressed.Count != 0)
                Console.Error.WriteLine("[ILDiet] Godot script discovery: keeping scripts in "
                    + project.Root + " (" + compressed.Count + " compressed binary resources; example: "
                    + compressed[0].Substring("compressed binary resource ".Length) + ").");
            int reported = 0;
            foreach (string reason in project.Failures)
            {
                if (reason.StartsWith("compressed binary resource ", StringComparison.Ordinal))
                    continue;
                if (reported++ == 8)
                {
                    Console.Error.WriteLine("[ILDiet] Godot script discovery: " + (project.Failures.Count - compressed.Count - 8)
                        + " additional resource discovery failures in " + project.Root + ".");
                    break;
                }
                Console.Error.WriteLine("[ILDiet] Godot script discovery: keeping scripts in "
                    + project.Root + " (" + reason + ").");
            }
        }
    }

    private static void Enumerate(Project project, string directory, TranspileOptions options)
    {
        if (File.Exists(Path.Combine(directory, ".gdignore")))
            return;
        foreach (string file in Directory.EnumerateFiles(directory).OrderBy(path => path, StringComparer.Ordinal))
        {
            string relative = Path.GetRelativePath(project.Root, file).Replace('\\', '/');
            project.Files.Add("res://" + relative, file);
        }
        foreach (string child in Directory.EnumerateDirectories(directory).OrderBy(path => path, StringComparer.Ordinal))
        {
            string name = Path.GetFileName(child);
            if (name.StartsWith(".", StringComparison.Ordinal) || name is "bin" or "obj"
                || SamePath(child, options.OutDir)
                || (options.ILDietOutput is not null && SamePath(child, options.ILDietOutput)))
                continue;
            if ((File.GetAttributes(child) & FileAttributes.ReparsePoint) != 0)
            {
                project.Failures.Add("symbolic-link resource directory cannot be inspected: " + child);
                continue;
            }
            Enumerate(project, child, options);
        }
    }

    private static bool SamePath(string left, string right) => string.Equals(Path.GetFullPath(left),
        Path.GetFullPath(right), Path.DirectorySeparatorChar == '\\'
            ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);

    private static void Inspect(Project project, string resource, string file)
    {
        string extension = Path.GetExtension(file).ToLowerInvariant();
        if (extension == ".uid")
        {
            string uid = File.ReadAllText(file).Trim();
            if (uid.StartsWith("uid://", StringComparison.Ordinal))
                AddUid(project, uid, resource.Substring(0, resource.Length - 4));
            return;
        }
        if (extension is ".tscn" or ".tres" or ".gd" or ".import" or ".cfg"
            || resource == "res://project.godot")
        {
            project.KnownResources.Add(resource);
            string text = File.ReadAllText(file);
            if (extension == ".gd")
                ReadGDScript(project, resource, text);
            else
                ReadText(project, resource, text, extension);
            return;
        }
        if (extension is ".cs" or ".gdextension" || IsImportedAsset(extension))
        {
            project.KnownResources.Add(resource);
            return;
        }
        using var stream = File.OpenRead(file);
        byte[] magic = new byte[4];
        int length = stream.Read(magic, 0, magic.Length);
        string header = Encoding.ASCII.GetString(magic, 0, length);
        if (header == "RSRC")
        {
            project.KnownResources.Add(resource);
            try
            {
                ReadBinary(project, resource, stream);
            }
            catch (Exception ex) when (ex is InvalidDataException or EndOfStreamException
                or DecoderFallbackException or OverflowException)
            {
                project.Failures.Add("unsupported binary resource " + resource + ": " + ex.Message);
            }
        }
        else if (header == "RSCC")
        {
            project.KnownResources.Add(resource);
            project.Failures.Add("compressed binary resource " + resource);
        }
        else if (extension is ".res" or ".scn")
            project.Failures.Add("unrecognized resource format " + resource);
    }

    private static bool IsImportedAsset(string extension) => extension is
        ".png" or ".jpg" or ".jpeg" or ".webp" or ".svg" or ".svgz" or ".bmp" or ".ico" or ".icns"
        or ".tga" or ".dds" or ".ktx" or ".hdr" or ".exr" or ".avif"
        or ".wav" or ".ogg" or ".mp3" or ".flac" or ".ogv"
        or ".ttf" or ".otf" or ".woff" or ".woff2"
        or ".glb" or ".gltf" or ".fbx" or ".blend" or ".obj" or ".dae"
        or ".csv" or ".po" or ".mo" or ".pot" or ".shader" or ".gdshader" or ".gdshaderinc";

    private static void ReadGDScript(Project project, string resource, string text)
    {
        string previous = "";
        string beforePrevious = "";
        string earlier = "";
        string receiver = "";
        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];
            if (char.IsWhiteSpace(c))
                continue;
            if (c == '#')
            {
                while (i + 1 < text.Length && text[i + 1] != '\n')
                    i++;
                continue;
            }
            string token;
            bool raw = c == 'r' && i + 1 < text.Length && text[i + 1] is '"' or '\'';
            if (raw || (c == '&' && i + 1 < text.Length && text[i + 1] is '"' or '\''))
                c = text[++i];
            if (c is '"' or '\'')
            {
                char quote = c;
                bool triple = i + 2 < text.Length && text[i + 1] == quote && text[i + 2] == quote;
                if (triple)
                    i += 2;
                var value = new StringBuilder();
                bool closed = false;
                while (++i < text.Length)
                {
                    c = text[i];
                    if (c == quote && (!triple
                        || (i + 2 < text.Length && text[i + 1] == quote && text[i + 2] == quote)))
                    {
                        if (triple)
                            i += 2;
                        closed = true;
                        break;
                    }
                    if (!raw && c == '\\' && i + 1 < text.Length)
                    {
                        c = text[++i];
                        if (c is not ('n' or 'r' or 't' or '\\' or '"' or '\''))
                            project.Failures.Add("unsupported GDScript string escape in " + resource);
                        c = c switch { 'n' => '\n', 'r' => '\r', 't' => '\t', _ => c };
                    }
                    value.Append(c);
                }
                if (!closed)
                    project.Failures.Add("unterminated GDScript string in " + resource);
                project.ScriptLiterals.Add((resource, value.ToString()));
                bool resourceCall = previous == "(" && (beforePrevious == "load"
                    ? earlier != "." || receiver == "ResourceLoader"
                    : beforePrevious == "preload" && earlier != ".");
                if (resourceCall || previous == "extends")
                {
                    string path = value.ToString();
                    if (path.StartsWith("uid://", StringComparison.Ordinal))
                        project.References.Add((resource, "", path));
                    else if (path.Length != 0)
                    {
                        // ResourceLoader resolves relative paths from the project; preload and extends use the script.
                        if (resourceCall && beforePrevious == "load" && !path.Contains("://", StringComparison.Ordinal)
                            && !Path.IsPathRooted(path))
                            path = "res://" + path;
                        project.References.Add((resource, path, ""));
                    }
                }
                token = "<string>";
            }
            else if (char.IsLetter(c) || c == '_')
            {
                int start = i;
                while (i + 1 < text.Length && (char.IsLetterOrDigit(text[i + 1]) || text[i + 1] == '_'))
                    i++;
                token = text.Substring(start, i - start + 1);
                project.Identifiers.Add(token);
            }
            else
                token = c.ToString();
            receiver = earlier;
            earlier = beforePrevious;
            beforePrevious = previous;
            previous = token;
        }
    }

    private static void ReadText(Project project, string resource, string text, string extension)
    {
        bool autoload = false;
        foreach (string line in text.Split('\n'))
        {
            string trimmed = line.TrimStart();
            if (resource == "res://project.godot" && trimmed.StartsWith("[", StringComparison.Ordinal))
                autoload = trimmed.StartsWith("[autoload]", StringComparison.Ordinal);
            var strings = new List<string>();
            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (c == '#' || c == ';')
                    break;
                if (c == '"')
                {
                    char quote = c;
                    var value = new StringBuilder();
                    while (++i < line.Length && line[i] != quote)
                    {
                        c = line[i];
                        if (c == '\\' && i + 1 < line.Length)
                        {
                            c = line[++i];
                            c = c switch { 'n' => '\n', 'r' => '\r', 't' => '\t', _ => c };
                        }
                        value.Append(c);
                    }
                    strings.Add(value.ToString());
                }
            }
            int equals = trimmed.IndexOf('=');
            bool declaration = trimmed.StartsWith("[gd_scene", StringComparison.Ordinal)
                || line.TrimStart().StartsWith("[gd_resource", StringComparison.Ordinal)
                || (extension == ".import" && equals >= 0 && trimmed.Substring(0, equals).Trim() == "uid");
            bool dependency = line.TrimStart().StartsWith("[ext_resource", StringComparison.Ordinal);
            string uid = "";
            string dependencyPath = "";
            foreach (string value in strings)
            {
                string path = autoload && value.StartsWith("*", StringComparison.Ordinal)
                    ? value.Substring(1) : value;
                if (autoload && path.Length != 0 && !path.Contains("://", StringComparison.Ordinal)
                    && !Path.IsPathRooted(path))
                    path = "res://" + path;
                if (path.StartsWith("uid://", StringComparison.Ordinal))
                    uid = path;
                else if (IsPath(path, dependency))
                {
                    if (dependency)
                        dependencyPath = path;
                    else if (extension != ".import")
                        project.References.Add((resource, path, ""));
                    else if (line.Contains("Resource(", StringComparison.Ordinal))
                        dependencyPath = path;
                }
            }
            if (uid.Length != 0 && declaration)
                AddUid(project, uid, extension == ".import"
                    ? resource.Substring(0, resource.Length - 7) : resource);
            else if (dependency || uid.Length != 0)
                project.References.Add((resource, dependencyPath, uid));
        }
    }

    private static bool IsPath(string path, bool relative) => path.StartsWith("res://", StringComparison.Ordinal)
        || (relative && (path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)
        || path.EndsWith(".gd", StringComparison.OrdinalIgnoreCase)
        || path.EndsWith(".tscn", StringComparison.OrdinalIgnoreCase)
        || path.EndsWith(".tres", StringComparison.OrdinalIgnoreCase)
        || path.EndsWith(".scn", StringComparison.OrdinalIgnoreCase)
        || path.EndsWith(".res", StringComparison.OrdinalIgnoreCase)));

    private static void AddUid(Project project, string uid, string path)
    {
        if (project.Uids.TryGetValue(uid, out string? previous) && previous != path)
            project.Failures.Add("ambiguous resource UID " + uid);
        else
            project.Uids[uid] = path;
    }

    private static void ResolveScriptLiteral(Project project, string source, string path)
    {
        if (path.StartsWith("uid://", StringComparison.Ordinal))
        {
            if (!project.Uids.TryGetValue(path, out string? resolved))
            {
                project.Failures.Add("unresolved resource UID " + path + " in " + source);
                return;
            }
            path = resolved;
        }
        if (!path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
            return;
        if (path.StartsWith("res://", StringComparison.Ordinal))
            path = path.Substring(6);
        else
        {
            if (path.Contains("://", StringComparison.Ordinal))
                return;
            string directory = Path.GetDirectoryName(project.Files[source])!;
            string? relative = LiteralResourcePath(project, directory, path);
            if (relative is not null && project.Files.ContainsKey(relative))
                project.Scripts.Add(relative);
        }
        string? resource = LiteralResourcePath(project, project.Root, path);
        if (resource is not null && project.Files.ContainsKey(resource))
            project.Scripts.Add(resource);
    }

    private static string? LiteralResourcePath(Project project, string directory, string path)
    {
        try
        {
            string absolute = Path.GetFullPath(Path.Combine(directory, path));
            return "res://" + Path.GetRelativePath(project.Root, absolute).Replace('\\', '/');
        }
        catch (ArgumentException)
        {
            return null;
        }
    }

    private static void Resolve(Project project, string source, string path, string uid)
    {
        if (uid.Length != 0 && project.Uids.TryGetValue(uid, out string? resolved))
            path = resolved;
        if (path.Length == 0)
        {
            project.Failures.Add("unresolved resource UID " + uid + " in " + source);
            return;
        }
        int subresource = path.IndexOf("::", StringComparison.Ordinal);
        if (subresource >= 0)
            path = path.Substring(0, subresource);
        if (!path.StartsWith("res://", StringComparison.Ordinal))
        {
            string directory = Path.GetDirectoryName(project.Files[source])!;
            string resolvedPath = Path.GetFullPath(Path.Combine(directory, path));
            path = "res://" + Path.GetRelativePath(project.Root, resolvedPath).Replace('\\', '/');
        }
        else
        {
            string resolvedPath = Path.GetFullPath(Path.Combine(project.Root, path.Substring(6)));
            path = "res://" + Path.GetRelativePath(project.Root, resolvedPath).Replace('\\', '/');
        }
        if (!project.Files.ContainsKey(path))
        {
            project.Failures.Add("unresolved resource " + path + " in " + source);
            return;
        }
        if (path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
            project.Scripts.Add(path);
        if (!project.KnownResources.Contains(path))
            project.Failures.Add("unknown resource " + path + " in " + source);
    }

    // ResourceLoaderBinary::open reads dependency tables before resource payloads.
    // Image resources use this same header; their pixel data need not be decoded.
    private static void ReadBinary(Project project, string resource, Stream stream)
    {
        var reader = new BinaryResourceReader(stream);
        bool bigEndian = reader.UInt32() != 0;
        reader.UInt32();
        reader.BigEndian = bigEndian;
        uint major = reader.UInt32();
        reader.UInt32();
        uint format = reader.UInt32();
        if (major != 4 || format < 3 || format > 6)
            throw new InvalidDataException("unknown engine or resource version");
        reader.String();
        reader.UInt64();
        uint flags = reader.UInt32();
        if ((flags & ~15u) != 0)
            throw new InvalidDataException("unknown resource flags");
        ulong uid = reader.UInt64();
        if ((flags & 2) != 0 && uid <= long.MaxValue)
            AddUid(project, UidText(uid), resource);
        if ((flags & 8) != 0)
            project.Identifiers.Add(reader.String());
        for (int i = 0; i < 11; i++)
            reader.UInt32();
        uint count = reader.Count();
        for (uint i = 0; i < count; i++)
            reader.String();
        count = reader.Count();
        for (uint i = 0; i < count; i++)
        {
            reader.String();
            string path = reader.String();
            uid = (flags & 2) != 0 ? reader.UInt64() : ulong.MaxValue;
            project.References.Add((resource, path, uid <= long.MaxValue ? UidText(uid) : ""));
        }
    }

    private static string UidText(ulong uid)
    {
        const string alphabet = "abcdefghijklmnopqrstuvwxy012345678";
        string digits = "";
        do
        {
            digits = alphabet[(int)(uid % 34)] + digits;
            uid /= 34;
        } while (uid != 0);
        return "uid://" + digits;
    }

    private sealed class BinaryResourceReader
    {
        private readonly Stream _stream;
        internal bool BigEndian;

        internal BinaryResourceReader(Stream stream) => _stream = stream;

        internal uint UInt32()
        {
            uint result = 0;
            for (int i = 0; i < 4; i++)
            {
                int value = _stream.ReadByte();
                if (value < 0)
                    throw new EndOfStreamException();
                result |= (uint)value << ((BigEndian ? 3 - i : i) * 8);
            }
            return result;
        }

        internal ulong UInt64()
        {
            ulong first = UInt32();
            ulong second = UInt32();
            return BigEndian ? (first << 32) | second : first | (second << 32);
        }

        internal uint Count()
        {
            uint count = UInt32();
            if (count > (_stream.Length - _stream.Position) / 4)
                throw new InvalidDataException("invalid resource table length");
            return count;
        }

        internal string String()
        {
            uint length = UInt32();
            if (length > 1024 * 1024 || length > _stream.Length - _stream.Position)
                throw new InvalidDataException("invalid resource string length");
            byte[] bytes = new byte[(int)length];
            int read = 0;
            while (read < bytes.Length)
            {
                int count = _stream.Read(bytes, read, bytes.Length - read);
                if (count == 0)
                    throw new EndOfStreamException();
                read += count;
            }
            return new UTF8Encoding(false, true).GetString(bytes).TrimEnd('\0');
        }
    }
}

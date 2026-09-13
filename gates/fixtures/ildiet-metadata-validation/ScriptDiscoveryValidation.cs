using System.Text;
using Dn2Cpp;
using Dn2Cpp.DotnetModule;

internal static class ScriptDiscoveryValidation
{
    internal static void Run(string directory)
    {
        string root = Path.GetFullPath(directory);
        Directory.CreateDirectory(root);
        string generated = Path.Combine(root, "generated-output");
        var options = new TranspileOptions { OutDir = generated, ILDietOutput = Path.Combine(root, "stripped-output") };
        options.ProjectRoots.Add(root);
        var metadata = new GodotILDietMetadata(Array.Empty<string>());
        foreach (string name in new[] { "SceneOnly", "Autoload", "AutoloadRelative", "ConstOnly", "ResourceOnly", "UidOnly", "RelativeOnly",
            "GlobalOnly", "BinaryOnly", "RootOnly", "NestedRelative", "TripleDouble", "TripleSingle", "TripleRaw", "Unused" })
        {
            string script = (name == "NestedRelative" ? "nested/" : "") + name + ".cs";
            Directory.CreateDirectory(Path.GetDirectoryName(Path.Combine(root, script))!);
            File.WriteAllText(Path.Combine(root, script), "// fixture script\n");
            var row = new GodotILDietMetadata.TypeRow
            {
                Identity = ("Scripts", name), Base = ("GodotSharp", "Godot.GodotObject"),
                GlobalClass = name == "GlobalOnly",
            };
            row.ScriptPaths.Add("res://" + script);
            metadata.Types.Add(row.Identity, row);
        }
        File.WriteAllText(Path.Combine(root, "project.godot"), "[application]\nrun/main_scene=\"res://main.tscn\"\n[autoload]\nAuto=\"*res://Autoload.cs\"\nRelativeAuto=\"*AutoloadRelative.cs\"\n");
        File.WriteAllText(Path.Combine(root, "main.tscn"), "[gd_scene format=3]\n");
        File.WriteAllText(Path.Combine(root, "other.tscn"), "[gd_scene format=3]\n[ext_resource type=\"Script\" path=\"res://SceneOnly.cs\" id=\"1\"]\n");
        File.WriteAllText(Path.Combine(root, "data.tres"), "[gd_resource format=3]\n[ext_resource type=\"Script\" path=\"res://ResourceOnly.cs\" id=\"1\"]\n");
        File.WriteAllText(Path.Combine(root, "UidOnly.cs.uid"), "uid://bfixture\n");
        File.WriteAllText(Path.Combine(root, "static.gd"), "extends Node\nconst C = preload(\"uid://bfixture\")\nconst D = preload(\"./RelativeOnly.cs\")\nconst ordinary = \"res://nonexistent-script.cs\"\nconst Path = r\"res://ConstOnly.cs\"\nconst Indirect = preload(Path)\nfunc make():\n    return GlobalOnly.new()\n# Unused.new()\n");
        File.AppendAllText(Path.Combine(root, "static.gd"), "const docs = \"\"\"preload(\"res://Unused.cs\")\"\"\"\n");
        File.AppendAllText(Path.Combine(root, "static.gd"), "const TripleDoublePath = \"\"\"res://TripleDouble.cs\"\"\"\nconst TripleSinglePath = '''res://TripleSingle.cs'''\nconst TripleRawPath = r\"\"\"res://TripleRaw.cs\"\"\"\nconst TripleScripts = [preload(TripleDoublePath), preload(TripleSinglePath), preload(TripleRawPath)]\n");
        File.AppendAllText(Path.Combine(root, "static.gd"), "const raw_example = r\"\\u0055nused.cs\"\n");
        File.WriteAllText(Path.Combine(root, "nested", "loaders.gd"), "extends Node\nfunc load_scripts():\n    var root = load(\"RootOnly.cs\")\n    var relative = preload(\"./NestedRelative.cs\")\n    var config = ConfigFile.new()\n    config.load(\"missing.cfg\")\n");
        File.AppendAllText(Path.Combine(root, "other.tscn"), "[node name=\"Picker\" type=\"FileDialog\"]\nfilters=PackedStringArray(\"*.gd\")\n");
        File.WriteAllBytes(Path.Combine(root, "image.png"), new byte[] { 137, 80, 78, 71 });
        File.WriteAllText(Path.Combine(root, "image.png.import"), "[remap]\nuid=\"uid://imagefixture\"\n[params]\nscript=Resource(\"uid://bfixture\", \"res://UidOnly.cs\")\n");
        foreach (string excluded in new[] { ".godot", "bin", "obj", "ignored", "generated-output", "stripped-output" })
        {
            string path = Path.Combine(root, excluded);
            Directory.CreateDirectory(path);
            if (excluded == "ignored") File.WriteAllText(Path.Combine(path, ".gdignore"), "");
            File.WriteAllText(Path.Combine(path, "stale.tscn"), "[ext_resource path=\"res://Unused.cs\"]\n");
            File.WriteAllBytes(Path.Combine(path, "unsupported.res"), "RSCC"u8.ToArray());
        }
        WriteBinary(Path.Combine(root, "image.resource"), "Image", null);
        WriteBinary(Path.Combine(root, "script.res"), "Resource", "res://BinaryOnly.cs");
        Check(metadata, options, "Autoload,AutoloadRelative,BinaryOnly,ConstOnly,GlobalOnly,NestedRelative,RelativeOnly,ResourceOnly,RootOnly,SceneOnly,TripleDouble,TripleRaw,TripleSingle,UidOnly", "static references");
        string unknown = Path.Combine(root, "unknown.res");
        foreach (var (content, reason) in new[] { ("RSCC"u8.ToArray(), "compressed binary resources"),
            ("unsupported"u8.ToArray(), "unrecognized resource format"), ("RSRC"u8.ToArray(), "unsupported binary resource") })
        {
            File.WriteAllBytes(unknown, content);
            Check(metadata, options, "Autoload,AutoloadRelative,BinaryOnly,ConstOnly,GlobalOnly,NestedRelative,RelativeOnly,ResourceOnly,RootOnly,SceneOnly,TripleDouble,TripleRaw,TripleSingle,UidOnly,Unused", "unknown resource fallback", true, "unknown.res", reason);
            File.Delete(unknown);
        }
        File.WriteAllText(Path.Combine(root, "unresolved.gd"), "extends Node\nconst C = preload(\"uid://missing\")\n");
        Check(metadata, options, "Autoload,AutoloadRelative,BinaryOnly,ConstOnly,GlobalOnly,NestedRelative,RelativeOnly,ResourceOnly,RootOnly,SceneOnly,TripleDouble,TripleRaw,TripleSingle,UidOnly,Unused", "unresolved UID fallback", true, "uid://missing");
        File.WriteAllText(Path.Combine(root, "unresolved.gd"), "extends Node\nconst MissingPath = \"uid://missing\"\nconst C = preload(MissingPath)\n");
        Check(metadata, options, "Autoload,AutoloadRelative,BinaryOnly,ConstOnly,GlobalOnly,NestedRelative,RelativeOnly,ResourceOnly,RootOnly,SceneOnly,TripleDouble,TripleRaw,TripleSingle,UidOnly,Unused", "indirect unresolved UID fallback", true, "uid://missing");
        File.Delete(Path.Combine(root, "unresolved.gd"));
        File.WriteAllText(Path.Combine(root, "escaped.gd"), "extends Node\nconst EscapedPath = \"res://\\u0055nused.cs\"\nconst C = preload(EscapedPath)\n");
        Check(metadata, options, "Autoload,AutoloadRelative,BinaryOnly,ConstOnly,GlobalOnly,NestedRelative,RelativeOnly,ResourceOnly,RootOnly,SceneOnly,TripleDouble,TripleRaw,TripleSingle,UidOnly,Unused", "escaped string path");
        File.Delete(Path.Combine(root, "escaped.gd"));
        var noProject = new ILDietRootPolicy();
        GodotProjectScripts.Configure(noProject, metadata, new TranspileOptions { OutDir = generated });
        Require(noProject.BaseTypes.Contains("Godot.GodotObject") && noProject.ExcludedAssemblies.Contains("GodotSharp"), "missing project did not retain user scripts");
        Console.WriteLine("godot-script-discovery=all-scenes,autoload,relative-autoload,const-path,triple-path,resource,uid,relative,root-relative,global,import-uid,ignored-strings,binary-image,binary-script,ignored-output,unknown-resource,indirect-uid,escaped-path,missing-project");
        CheckEscapes(Path.Combine(generated, "escape-project"));
    }

    private static void CheckEscapes(string root)
    {
        Directory.CreateDirectory(root);
        File.WriteAllText(Path.Combine(root, "project.godot"), "[application]\n");
        var options = new TranspileOptions { OutDir = Path.Combine(root, "output") };
        options.ProjectRoots.Add(root);
        var metadata = new GodotILDietMetadata(Array.Empty<string>());
        foreach (var (name, file) in new[] { ("Target", "Target.cs"), ("Supplementary", "😀.cs"),
            ("Replacement", "�.cs"), ("Unused", "Unused.cs") })
        {
            File.WriteAllText(Path.Combine(root, file), "// fixture script\n");
            var row = new GodotILDietMetadata.TypeRow
            {
                Identity = ("Scripts", name), Base = ("GodotSharp", "Godot.GodotObject"),
            };
            row.ScriptPaths.Add("res://" + file);
            metadata.Types.Add(row.Identity, row);
        }
        File.WriteAllText(Path.Combine(root, "Target.cs.uid"), "uid://bfixture\n");
        string source = Path.Combine(root, "escapes.gd");
        void Valid(string body, string expected, string subject)
        {
            File.WriteAllText(source, "extends Node\n" + body + "\n");
            Check(metadata, options, expected, subject);
        }
        foreach (string quote in new[] { "\"", "'", "\"\"\"", "'''" })
        {
            foreach (string escape in new[] { @"\a", @"\b", @"\f", @"\n", @"\r", @"\t", @"\v", @"\\", @"\'", "\\\"",
                @"\u0055", @"\U000055", @"\u0000", @"\uFFFF", @"\U10FFFF", @"\U01F600", @"\uD83D\uDE00" })
                Valid("const unreferenced = " + quote + escape + " harmless text" + quote, "", "unreferenced " + quote + escape);
            Valid("const script = preload(" + quote + @"res://\u0054arget.cs" + quote + ")", "Target", "Unicode path " + quote);
            Valid("const path = " + quote + @"res://\U000054arget.cs" + quote + "\nconst script = load(path)", "Target", "indirect Unicode path " + quote);
            Valid("const script = preload(" + quote + @"\u0075id://\U000062fixture" + quote + ")", "Target", "Unicode UID " + quote);
            foreach (string escape in new[] { @"\U01F600", @"\uD83D\uDE00", @"\U00D83D\U00DE00", @"\uD83D\U00DE00" })
                Valid("const script = preload(" + quote + "res://" + escape + ".cs" + quote + ")", "Supplementary", "supplementary path " + quote + escape);
            foreach (string escape in new[] { @"\u0000", @"\U000000" })
            {
                Valid("const script = preload(" + quote + "res://" + escape + ".cs" + quote + ")",
                    "Replacement", "zero escape direct path " + quote + escape);
                Valid("const path = " + quote + "res://" + escape + ".cs" + quote + "\nconst script = load(path)",
                    "Replacement", "zero escape indirect path " + quote + escape);
                Valid("const path = r" + quote + "res://" + escape + ".cs" + quote,
                    "", "raw zero escape " + quote + escape);
            }
            Valid("const script = preload(" + quote + "res://Tar\\\nget.cs" + quote + ")", "Target", "continued path " + quote);
            Valid("const script = preload(" + quote + @"res://\uD83D" + "\\\n" + @"\uDE00.cs" + quote + ")",
                "Supplementary", "continued surrogate pair " + quote);
            Valid("const unreferenced = " + quote + "line\\\r\nbreak" + quote, "", "escaped CRLF " + quote);
            Valid("const path = " + quote + "res://Tar\\\r\nget.cs" + quote + "\nconst script = load(path)",
                "", "escaped CRLF retains newline " + quote);
            Valid("const unreferenced = " + quote + "line\nbreak" + quote, "", "literal newline " + quote);
            Valid("const unreferenced = r" + quote + @"\u0055nused.cs" + quote, "", "raw Unicode " + quote);
            Valid("const unreferenced = r" + quote + @"\U110000\uD800\q" + quote, "", "raw malformed escapes " + quote);
            Valid("const unreferenced = r" + quote + "\\" + quote[0] + " Unused.new()" + quote, "", "raw escaped quote " + quote);
            Valid("const unreferenced = r" + quote + "ends with \\\\" + quote + "\nconst script = preload(\"res://Target.cs\")",
                "Target", "raw escaped backslash " + quote);
        }
        foreach (var (malformed, reason) in new[]
        {
            (@"\q", "unsupported GDScript string escape"),
            (@"\u123", "invalid GDScript Unicode escape"), (@"\U12345", "invalid GDScript Unicode escape"),
            (@"\u12x4", "invalid GDScript Unicode escape"), (@"\U0000x0", "invalid GDScript Unicode escape"),
            (@"\U110000", "invalid GDScript Unicode escape"),
            (@"\uD800", "unpaired GDScript Unicode surrogate"), (@"\uDC00", "unpaired GDScript Unicode surrogate"),
            (@"\uD800\u0041", "unpaired GDScript Unicode surrogate"), (@"\uD800\uD800", "unpaired GDScript Unicode surrogate"),
            (@"\uD800x\uDC00", "unpaired GDScript Unicode surrogate"), (@"\uD800\n\uDC00", "unpaired GDScript Unicode surrogate"),
            (@"\uD800" + "\\\r\n" + @"\uDC00", "unpaired GDScript Unicode surrogate"),
            (@"\U00DC00", "unpaired GDScript Unicode surrogate"),
        })
        {
            foreach (string quote in new[] { "\"", "'", "\"\"\"", "'''" })
            {
                File.WriteAllText(source, "const invalid = " + quote + malformed + quote + "\n");
                Check(metadata, options, "Replacement,Supplementary,Target,Unused", "invalid escape " + quote + malformed, true, "escapes.gd", reason);
            }
        }
        foreach (string incomplete in new[] { "\"unterminated", "'unterminated", "\"\"\"unterminated", "'''unterminated",
            "\"trailing\\", "\"\\u12", "\"\\U123" })
        {
            File.WriteAllText(source, "const invalid = " + incomplete);
            Check(metadata, options, "Replacement,Supplementary,Target,Unused", "incomplete string", true, "escapes.gd", "unterminated");
        }
        File.WriteAllText(source, "const invalid = preload(\"res://\\u0000Unused.cs\")\n");
        Check(metadata, options, "Replacement,Supplementary,Target,Unused", "unresolved replacement path", true, "escapes.gd", "unresolved resource res://�Unused.cs");
        File.Delete(source);
        File.AppendAllText(Path.Combine(root, "project.godot"), "[autoload]\nMissing=\"*res://missing.cs\"\n");
        Check(metadata, options, "Replacement,Supplementary,Target,Unused", "missing autoload", true, "missing.cs", "unresolved resource");
        File.WriteAllText(Path.Combine(root, "project.godot"), "[application]\n");
        Check(metadata, options, "", "fallback recovery");
        string otherRoot = Path.Combine(options.OutDir, "other-project");
        Directory.CreateDirectory(otherRoot);
        File.WriteAllText(Path.Combine(otherRoot, "project.godot"), "[application]\n");
        File.WriteAllText(Path.Combine(otherRoot, "OtherUnused.cs"), "// fixture script\n");
        var other = new GodotILDietMetadata.TypeRow
        {
            Identity = ("OtherScripts", "OtherUnused"), Base = ("GodotSharp", "Godot.GodotObject"),
        };
        other.ScriptPaths.Add("res://OtherUnused.cs");
        metadata.Types.Add(other.Identity, other);
        options.ProjectRoots.Add(otherRoot);
        File.WriteAllText(source, "const invalid = \"\\uD800\"\n");
        Check(metadata, options, "Replacement,Supplementary,Target,Unused", "fallback stays in its project", true, "escapes.gd", "unpaired");
        File.Delete(source);
        Console.WriteLine("godot-script-escapes=controls,unicode-path,unicode-uid,supplementary,surrogates,continuation,raw,invalid,unterminated,zero-replacement,missing-autoload,project-fallback,diagnostics");
    }

    private static void Check(GodotILDietMetadata metadata, TranspileOptions options, string expected, string subject,
        bool fallback = false, string? diagnosticSource = null, string? diagnosticReason = null)
    {
        var policy = new ILDietRootPolicy { PreservePublicAppTypes = false };
        using var diagnostics = new StringWriter();
        TextWriter error = Console.Error;
        try
        {
            Console.SetError(diagnostics);
            GodotProjectScripts.Configure(policy, metadata, options);
        }
        finally
        {
            Console.SetError(error);
        }
        string message = diagnostics.ToString();
        Require(fallback ? message.Contains("keeping scripts in ", StringComparison.Ordinal) : message.Length == 0,
            subject + ": unexpected diagnostics: " + message);
        if (diagnosticSource is not null)
            Require(message.Contains(diagnosticSource, StringComparison.Ordinal), subject + ": missing diagnostic source: " + message);
        if (diagnosticReason is not null)
            Require(message.Contains(diagnosticReason, StringComparison.Ordinal), subject + ": missing diagnostic reason: " + message);
        string actual = string.Join(",", policy.TypeRoots.Select(t => t.Type).Order(StringComparer.Ordinal));
        Require(actual == expected, subject + ": expected " + expected + ", got " + actual);
    }

    private static void WriteBinary(string path, string type, string? dependency)
    {
        using var stream = File.Create(path);
        using var writer = new BinaryWriter(stream, Encoding.UTF8);
        void String(string text)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(text + "\0");
            writer.Write((uint)bytes.Length);
            writer.Write(bytes);
        }
        writer.Write("RSRC"u8);
        writer.Write(0u);
        writer.Write(0u);
        writer.Write(4u);
        writer.Write(7u);
        writer.Write(6u);
        String(type);
        writer.Write(0ul);
        writer.Write(0u);
        writer.Write(ulong.MaxValue);
        for (int i = 0; i < 11; i++) writer.Write(0u);
        writer.Write(0u);
        writer.Write(dependency is null ? 0u : 1u);
        if (dependency is not null)
        {
            String("Script");
            String(dependency);
        }
        writer.Write(0u);
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}

namespace Dn2Cpp
{
    internal sealed class TranspileOptions
    {
        internal readonly List<string> ProjectRoots = new();
        internal string OutDir = ".";
        internal string? ILDietOutput;
    }
}

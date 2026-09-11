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
        foreach (var content in new[] { "RSCC"u8.ToArray(), "unsupported"u8.ToArray(), "RSRC"u8.ToArray() })
        {
            File.WriteAllBytes(unknown, content);
            Check(metadata, options, "Autoload,AutoloadRelative,BinaryOnly,ConstOnly,GlobalOnly,NestedRelative,RelativeOnly,ResourceOnly,RootOnly,SceneOnly,TripleDouble,TripleRaw,TripleSingle,UidOnly,Unused", "unknown resource fallback");
            File.Delete(unknown);
        }
        File.WriteAllText(Path.Combine(root, "unresolved.gd"), "extends Node\nconst C = preload(\"uid://missing\")\n");
        Check(metadata, options, "Autoload,AutoloadRelative,BinaryOnly,ConstOnly,GlobalOnly,NestedRelative,RelativeOnly,ResourceOnly,RootOnly,SceneOnly,TripleDouble,TripleRaw,TripleSingle,UidOnly,Unused", "unresolved UID fallback");
        File.WriteAllText(Path.Combine(root, "unresolved.gd"), "extends Node\nconst MissingPath = \"uid://missing\"\nconst C = preload(MissingPath)\n");
        Check(metadata, options, "Autoload,AutoloadRelative,BinaryOnly,ConstOnly,GlobalOnly,NestedRelative,RelativeOnly,ResourceOnly,RootOnly,SceneOnly,TripleDouble,TripleRaw,TripleSingle,UidOnly,Unused", "indirect unresolved UID fallback");
        File.Delete(Path.Combine(root, "unresolved.gd"));
        File.WriteAllText(Path.Combine(root, "escaped.gd"), "extends Node\nconst EscapedPath = \"res://\\u0055nused.cs\"\nconst C = preload(EscapedPath)\n");
        Check(metadata, options, "Autoload,AutoloadRelative,BinaryOnly,ConstOnly,GlobalOnly,NestedRelative,RelativeOnly,ResourceOnly,RootOnly,SceneOnly,TripleDouble,TripleRaw,TripleSingle,UidOnly,Unused", "unsupported string escape fallback");
        File.Delete(Path.Combine(root, "escaped.gd"));
        var noProject = new ILDietRootPolicy();
        GodotProjectScripts.Configure(noProject, metadata, new TranspileOptions { OutDir = generated });
        Require(noProject.BaseTypes.Contains("Godot.GodotObject") && noProject.ExcludedAssemblies.Contains("GodotSharp"), "missing project did not retain user scripts");
        Console.WriteLine("godot-script-discovery=all-scenes,autoload,relative-autoload,const-path,triple-path,resource,uid,relative,root-relative,global,import-uid,ignored-strings,binary-image,binary-script,ignored-output,unknown-resource,indirect-uid,escaped-path,missing-project");
    }

    private static void Check(GodotILDietMetadata metadata, TranspileOptions options, string expected, string subject)
    {
        var policy = new ILDietRootPolicy { PreservePublicAppTypes = false };
        GodotProjectScripts.Configure(policy, metadata, options);
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

using System.Globalization;
using Godot;
using GodotTools.Export;
using OS = FixtureOS;

CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
string work = args[0];
Directory.CreateDirectory(work);
File.WriteAllText(Path.Combine(work, "Game.dll"), "published input");
GodotSharpDirs.ProjectCsProjPath = Path.Combine(work, "Game.csproj");
string[] names = ["trim_reflection", "trim_godot_classes", "shared_generics", "compress_metadata"];
string[] flags = ["--trim-reflection", "--trim-godot-classes", "--no-shared-generics", "--no-metadata-compression"];
string[][] extras = [[], ["--trim-reflection", "--trim-godot-classes", "--shared-generics", "--no-ildiet"], ["--no-shared-generics"], ["--no-metadata-compression"]];
bool?[] values = [null, true, false];
int runs = 0;
foreach (string platform in OS.PlatformFeatureMap.Keys)
{
    var editor = new EditorExportPlatform(platform);
    var probe = new ExportProbe(platform, work);
    var definitions = probe._GetExportOptions(editor);
    foreach (string name in names.Append("il_prestripping"))
    {
        string key = "dotnet/dn2cpp/" + name;
        var definition = definitions.Single(item => (string)((Godot.Collections.Dictionary)item["option"])["name"] == key);
        Check((bool)definition["default_value"], platform + " default " + name);
        Check((int)((Godot.Collections.Dictionary)definition["option"])["type"] == (int)Variant.Type.Bool, "boolean option");
        foreach (int backend in new[] { 0, 1, 2 })
        {
            probe.Preset["dotnet/export_backend"] = backend;
            Check(probe._GetExportOptionVisibility(editor, key) == (backend == 2), platform + " visibility " + name);
        }
    }
    const string strippingKey = "dotnet/dn2cpp/stripping_level";
    var strippingDefinition = definitions.Single(item => (string)((Godot.Collections.Dictionary)item["option"])["name"] == strippingKey);
    var strippingOption = (Godot.Collections.Dictionary)strippingDefinition["option"];
    Check((int)strippingDefinition["default_value"] == 0, platform + " default stripping level");
    Check((int)strippingOption["type"] == (int)Variant.Type.Int, "stripping enum option");
    Check((int)strippingOption["hint"] == (int)PropertyHint.Enum && (string)strippingOption["hint_string"] == "High,Medium", "stripping choices");
    foreach (int backend in new[] { 0, 1, 2 })
    {
        probe.Preset["dotnet/export_backend"] = backend;
        Check(probe._GetExportOptionVisibility(editor, strippingKey) == (backend == 2), platform + " stripping visibility");
    }
    foreach (bool? reflection in values)
    foreach (bool? classes in values)
    foreach (bool? shared in values)
    foreach (bool? compression in values)
    foreach (bool? ildiet in values)
    foreach (string[] extra in extras)
    {
        probe.Preset.Clear();
        bool?[] selected = [reflection, classes, shared, compression, ildiet];
        string[] keys = [.. names, "il_prestripping"];
        for (int i = 0; i < keys.Length; i++)
            if (selected[i].HasValue)
                probe.Preset["dotnet/dn2cpp/" + keys[i]] = selected[i]!.Value;
        ProjectSettings.Extra = extra;
        string[] actual = probe.Generate();
        CheckStripping(actual, "high");
        var expected = new List<string>();
        if (reflection ?? true) expected.Add(flags[0]);
        if (classes ?? true) expected.Add(flags[1]);
        if (!(shared ?? true)) expected.Add(flags[2]);
        if (!(compression ?? true)) expected.Add(flags[3]);
        if (!(ildiet ?? true)) expected.Add("--no-ildiet");
        expected.AddRange(extra);
        string[] switches = actual.Where(arg => flags.Contains(arg) || arg is "--shared-generics" or "--no-ildiet").ToArray();
        Check(switches.SequenceEqual(expected), $"{platform}: expected {string.Join(' ', expected)}, got {string.Join(' ', switches)}");
        Check(actual.Contains("--dotnet-module") && actual.Contains("--auto-ref"), "base transpile arguments");
        Check(actual.Contains("--direct-pinvoke") == (platform is "web" or "ios"), "target argument path");
        Check(actual.Contains("com") == (platform == "windows"), "Windows argument path");
        runs++;
    }
    foreach (int? level in new int?[] { null, 0, 1 })
    foreach (bool? ildiet in values)
    foreach (bool duplicate in new[] { false, true })
    {
        probe.Preset.Clear();
        if (level.HasValue) probe.Preset[strippingKey] = level.Value;
        if (ildiet.HasValue) probe.Preset["dotnet/dn2cpp/il_prestripping"] = ildiet.Value;
        string expectedLevel = level == 1 ? "medium" : "high";
        ProjectSettings.Extra = duplicate ? ["--stripping-level", expectedLevel, "--ildiet-skip-assembly", "Library"] : [];
        string[] actual = probe.Generate();
        CheckStripping(actual, expectedLevel);
        Check(actual.Contains("--no-ildiet") == (ildiet == false), "stripping level independent of prestripping");
        Check(actual.Contains("Library") == duplicate, "unrelated extra arguments retained");
        runs++;
    }
    foreach (int invalid in new[] { -1, 2 })
    {
        probe.Preset.Clear();
        probe.Preset[strippingKey] = invalid;
        ProjectSettings.Extra = [];
        ExpectInvalid(() => probe.Generate(), "Stripping Level");
        runs++;
    }
    foreach (int level in new[] { 0, 1 })
    foreach (string[] extra in new[] { new[] { "--stripping-level" }, new[] { "--stripping-level", level == 0 ? "medium" : "high" }, new[] { "--stripping-level", "low" } })
    {
        probe.Preset.Clear();
        probe.Preset[strippingKey] = level;
        ProjectSettings.Extra = extra;
        ExpectInvalid(() => probe.Generate(), "extra_transpile_args");
        runs++;
    }
}
Console.WriteLine($"PASS: export option definitions, visibility and argument generation ({runs} platform/preset/extra-argument cases)");

static void CheckStripping(string[] arguments, string level)
{
    Check(arguments.Count(value => value == "--stripping-level") == 1, "one stripping-level argument");
    int index = Array.IndexOf(arguments, "--stripping-level");
    Check(index + 1 < arguments.Length && arguments[index + 1] == level, "selected stripping level reaches CLI");
}

static void ExpectInvalid(Action action, string diagnostic)
{
    try { action(); }
    catch (InvalidOperationException exception) when (exception.Message.Contains(diagnostic)) { return; }
    throw new InvalidOperationException("expected an actionable rejection: " + diagnostic);
}

static void Check(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}

sealed class ExportProbe(string platform, string work)
{
    private enum ExportBackend { HostRuntime, NativeAot, Dn2Cpp }
    public Dictionary<string, object> Preset { get; } = new();
    private Variant GetOption(string name) => new(Preset.GetValueOrDefault(name));
    private readonly string _godotPlatform = platform;
    private readonly Toolchain _toolchain = new();
    private readonly string? _declangPath = null;
    private const string ExtraTranspileArgsSetting = "dotnet/dn2cpp/extra_transpile_args";
    private string[] _arguments = [];
    private static IEnumerable<string> FindManagedDependencies(string directory, string assembly) => [];
    private static void RecreateDirectory(string path)
    {
        if (Directory.Exists(path)) Directory.Delete(path, true);
        Directory.CreateDirectory(path);
    }
    private void RunTool(string tool, List<string> arguments, string step) => _arguments = arguments.ToArray();
    public string[] Generate()
    {
        // INSERT_READ_OPTIONS
        Transpile(work, "Game", Path.Combine(work, "il"), Path.Combine(work, "generated"), ilPrestripping, optimizationOptions);
        return _arguments;
    }
    // INSERT_OPTIONS
    // INSERT_VISIBILITY
    // INSERT_TRANSPILE
}

sealed class Toolchain
{
    public string CoreLibRefFor(string platform) => "CoreLib.dll";
    public string RuntimeShim => "RuntimeShim.dll";
    public string Dn2CppExe => "dn2cpp";
}
static class Dn2CppToolchain { public const string EditorGodotSharpAssembly = "GodotSharp.dll"; }
static class GodotSharpDirs { public static string ProjectCsProjPath = ""; }
static class ProjectSettings
{
    public static string[] Extra = [];
    public static bool HasSetting(string key) => Extra.Length != 0;
    public static Variant GetSetting(string key) => new(Extra);
}
static class FixtureOS
{
    public static bool IsMacOS => true;
    public static bool IsWindows => false;
    public static Dictionary<string, string> PlatformFeatureMap = new[] { "windows", "linuxbsd", "macos", "android", "ios", "web" }.ToDictionary(p => p);
    public static class Platforms
    {
        public const string Windows = "windows", MacOS = "macos", Android = "android", iOS = "ios", Web = "web";
    }
}
namespace Godot
{
    sealed class EditorExportPlatform(string name) { public string GetOsName() => name; }
    enum PropertyHint { Enum, GlobalFile, None }
    readonly struct Variant(object? value)
    {
        public enum Type { Nil, Bool, Int, String }
        public Type VariantType => value is null ? Type.Nil : Type.Bool;
        public bool AsBool() => (bool)value!;
        public string[] AsStringArray() => (string[])value!;
        public static explicit operator int(Variant variant) => (int)variant.Value!;
        private object? Value => value;
    }
    static class GD { public static void Print(string message) { } }
    static class OS { public static string GetName() => "macos"; }
}
namespace Godot.Collections
{
    sealed class Array<T> : List<T> { }
    sealed class Dictionary : Dictionary<string, object> { }
}

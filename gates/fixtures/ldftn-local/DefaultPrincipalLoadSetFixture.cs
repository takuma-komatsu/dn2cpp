using System.Globalization;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography;
using System.Text.Json;
using Mono.Cecil;
using Mono.Cecil.Cil;

internal static class DefaultPrincipalLoadSetFixture
{
    public static bool Run(string[] args)
    {
        if (args.Length != 5 || args[0] != "--default-principal-loadset") return false;
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        string compiler = Path.GetFullPath(args[1]), input = Path.GetFullPath(args[2]), corelib = Path.GetFullPath(args[3]), output = Path.GetFullPath(args[4]);
        Directory.CreateDirectory(output);
        if (Directory.EnumerateFileSystemEntries(output).Any()) throw new InvalidOperationException("loadset output must be empty");
        using (var image = File.OpenRead(input))
        using (var pe = new PEReader(image))
        {
            var reader = pe.GetMetadataReader();
            if (reader.AssemblyReferences.Any(h => reader.GetString(reader.GetAssemblyReference(h).Name) == "System.Security.Claims"))
                throw new InvalidOperationException("key fixture has an explicit Claims reference");
        }
        var production = Assembly.LoadFrom(compiler);
        const BindingFlags instance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        const BindingFlags statics = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
        var optionType = production.GetType("Dn2Cpp.TranspileOptions", true)!;
        var loadSet = production.GetType("Dn2Cpp.AssemblyLoadSet", true)!;
        string[] Resolve(string app)
        {
            var options = Activator.CreateInstance(optionType)!;
            void Set(string name, object value)
            {
                if (optionType.GetProperty(name, instance) is { } property) property.SetValue(options, value);
                else optionType.GetField(name, instance)!.SetValue(options, value);
            }
            Set("AutoRef", true); Set("DefaultRefDir", Path.GetDirectoryName(compiler)!);
            object resolved;
            try { resolved = loadSet.GetMethod("Resolve", statics)!.Invoke(null, new object[] { new[] { app, corelib }, options })!; }
            catch (TargetInvocationException error) when (error.InnerException is not null) { throw error.InnerException; }
            return ((IEnumerable<string>)loadSet.GetField("Paths", instance)!.GetValue(resolved)!).Select(Path.GetFullPath).ToArray();
        }
        string Name(string file)
        {
            using var stream = File.OpenRead(file); using var pe = new PEReader(stream);
            var reader = pe.GetMetadataReader(); return reader.GetString(reader.GetAssemblyDefinition().Name);
        }
        void Save(string name, string app, string[] paths)
        {
            if (paths.Count(p => Name(p) == "System.Security.Claims") != 1) throw new InvalidOperationException("exactly one implicit Claims implementation required");
            File.WriteAllLines(Path.Combine(output, name + "paths.txt"), paths);
            File.WriteAllLines(Path.Combine(output, name + "refs.txt"), paths.Where(p => p != app));
            File.WriteAllText(Path.Combine(output, name + "identity.json"), JsonSerializer.Serialize(paths.Select(p => new { path = p, assembly = Name(p), sha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(p))).ToLowerInvariant() }), new JsonSerializerOptions { WriteIndented = true }));
        }
        Save("", input, Resolve(input));
        string unused = Path.Combine(output, "UnusedPrincipalProbe.dll");
        using (var core = Mono.Cecil.AssemblyDefinition.ReadAssembly(corelib))
        using (var app = Mono.Cecil.AssemblyDefinition.CreateAssembly(new AssemblyNameDefinition("UnusedPrincipalProbe", new Version(1, 0)), "UnusedPrincipalProbe", ModuleKind.Console))
        {
            var module = app.MainModule;
            var type = new Mono.Cecil.TypeDefinition("Probe", "Unused", Mono.Cecil.TypeAttributes.Public, module.ImportReference(core.MainModule.GetType("System.Object")));
            module.Types.Add(type);
            var entry = new Mono.Cecil.MethodDefinition("Main", Mono.Cecil.MethodAttributes.Public | Mono.Cecil.MethodAttributes.Static, module.TypeSystem.Void);
            type.Methods.Add(entry); entry.Body.Instructions.Add(Instruction.Create(OpCodes.Ret)); module.EntryPoint = entry;
            app.Write(unused, new WriterParameters { Timestamp = 0, DeterministicMvid = true });
        }
        Save("unused-", unused, Resolve(unused));
        File.WriteAllText(Path.Combine(output, "compiler.sha256"), Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(compiler))).ToLowerInvariant() + "\n");
        Console.WriteLine("default-principal-loadset:Claims-admitted;input-Claims-reference-absent;unused-control-created");
        return true;
    }
}

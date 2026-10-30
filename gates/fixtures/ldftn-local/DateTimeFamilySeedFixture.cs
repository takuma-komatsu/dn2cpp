using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
using Mono.Cecil;
using Mono.Cecil.Cil;

internal static class DateTimeFamilySeedFixture
{
    public static bool Run(string[] args)
    {
        if (args.Length != 3 || args[0] != "--datetime-family-seeds") return false;
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        string corePath = Path.GetFullPath(args[1]);
        string output = Path.GetFullPath(args[2]);
        if (Directory.Exists(output)) throw new InvalidOperationException("seed directory must be new");
        Directory.CreateDirectory(output);
        using var core = AssemblyDefinition.ReadAssembly(corePath);
        var owner = core.MainModule.GetType("System.DateTimeFormat")
            ?? throw new InvalidOperationException("installed DateTimeFormat missing");
        var methods = owner.Methods.OrderBy(method => method.MetadataToken.ToInt32()).ToArray();
        if (methods.Length != 35 || methods.Any(method => !method.IsStatic || method.RVA == 0)
            || methods.Count(method => method.IsConstructor) != 1)
            throw new InvalidOperationException("DateTimeFormat installed method inventory changed");
        var cctor = methods.Single(method => method.IsConstructor && method.Name == ".cctor");
        var field = owner.Fields.Single(value => value.Name == "InvariantFormatInfo" && value.IsStatic && !value.IsLiteral);
        var roots = new List<(MethodDefinition Method, string Flavor)>();
        foreach (var method in methods)
        {
            if (!method.HasGenericParameters)
            {
                roots.Add((method, "plain"));
                continue;
            }
            if (method.GenericParameters.Count != 1 || method.GenericParameters[0].Name != "TChar")
                throw new InvalidOperationException("unreviewed generic DateTimeFormat root: " + method.FullName);
            roots.Add((method, "char"));
            roots.Add((method, "byte"));
        }
        var manifest = new List<object>();
        foreach (var root in roots)
        {
            string name = root.Method.MetadataToken.ToInt32().ToString("X8", CultureInfo.InvariantCulture) + "-" + root.Flavor;
            string path = Path.Combine(output, name + ".dll");
            Write(path, new[] { root });
            manifest.Add(new
            {
                file = Path.GetFileName(path), token = root.Method.MetadataToken.ToInt32().ToString("X8", CultureInfo.InvariantCulture),
                signature = root.Method.FullName, flavor = root.Flavor,
                rootKind = root.Method == cctor ? "real-static-field-load" : "ldftn-pop",
                sha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)))
            });
        }
        Write(Path.Combine(output, "all.dll"), roots);
        File.WriteAllText(Path.Combine(output, "manifest.json"), JsonSerializer.Serialize(new
        {
            corelib = corePath, corelibSha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(corePath))),
            methodDefinitions = methods.Length, seedInputs = roots.Count, roots = manifest,
            combined = new { file = "all.dll", sha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(Path.Combine(output, "all.dll")))) },
            executionPolicy = "compile-only; never execute these private managed roots on CLR or native"
        }, new JsonSerializerOptions { WriteIndented = true }));
        Console.WriteLine("datetime-family-seeds:definitions=" + methods.Length + ":inputs=" + roots.Count);
        return true;

        void Write(string path, IEnumerable<(MethodDefinition Method, string Flavor)> selected)
        {
            using var app = AssemblyDefinition.CreateAssembly(new AssemblyNameDefinition("DateTimeFamilySeed", new Version(1, 0)),
                "DateTimeFamilySeed", ModuleKind.Console);
            var module = app.MainModule;
            var program = new TypeDefinition("DateTimeFamilySeed", "Program", TypeAttributes.Public | TypeAttributes.Abstract | TypeAttributes.Sealed,
                module.ImportReference(core.MainModule.GetType("System.Object")));
            module.Types.Add(program);
            var main = new MethodDefinition("Main", MethodAttributes.Public | MethodAttributes.Static, module.TypeSystem.Void);
            program.Methods.Add(main);
            var il = main.Body.GetILProcessor();
            foreach (var selectedRoot in selected)
            {
                if (selectedRoot.Method == cctor)
                {
                    // A normal field access reaches the actual initializer; do not take a .cctor address.
                    il.Emit(OpCodes.Ldsfld, module.ImportReference(field));
                    il.Emit(OpCodes.Pop);
                    continue;
                }
                MethodReference reference = module.ImportReference(selectedRoot.Method);
                if (selectedRoot.Method.HasGenericParameters)
                {
                    var instance = new GenericInstanceMethod(reference);
                    instance.GenericArguments.Add(selectedRoot.Flavor == "char" ? module.TypeSystem.Char : module.TypeSystem.Byte);
                    reference = instance;
                }
                il.Emit(OpCodes.Ldftn, reference);
                il.Emit(OpCodes.Pop);
            }
            il.Emit(OpCodes.Ret);
            module.EntryPoint = main;
            app.Write(path, new WriterParameters { Timestamp = 0, DeterministicMvid = true });
        }
    }
}

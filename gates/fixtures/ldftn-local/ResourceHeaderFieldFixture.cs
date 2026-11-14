using Mono.Cecil;
using Mono.Cecil.Cil;

internal static class ResourceHeaderFieldFixture
{
    public static bool Run(string[] args)
    {
        if (args.Length != 3 || args[0] != "--resource-header-fields") return false;
        string root = Path.GetFullPath(args[1]);
        foreach (string mode in new[] { "reference-type", "owner", "declaration-type", "declaration-flags", "address", "unsupported" })
        {
            string directory = Path.Combine(root, mode);
            Directory.CreateDirectory(directory);
            using var core = AssemblyDefinition.ReadAssembly(args[2]);
            var owner = core.MainModule.GetType("System.Resources.ResourceManager");
            var field = owner.Fields.Single(f => f.Name == "MagicNumber");
            if (field.Attributes != (FieldAttributes.Public | FieldAttributes.Static | FieldAttributes.InitOnly)
                || field.FieldType.MetadataType != MetadataType.Int32)
                throw new InvalidOperationException("ResourceManager header field declaration changed");
            if (mode == "declaration-type") field.FieldType = core.MainModule.TypeSystem.Int64;
            if (mode == "declaration-flags") field.IsInitOnly = false;
            if (mode.StartsWith("declaration-", StringComparison.Ordinal))
            {
                // Like DefaultPrincipalDependencyFixture, emit a metadata/IL-only
                // refusal fixture, not the input's ReadyToRun native image.
                core.MainModule.Attributes |= ModuleAttributes.ILOnly;
                core.MainModule.Attributes &= ~ModuleAttributes.StrongNameSigned;
                core.Write(Path.Combine(directory, "System.Private.CoreLib.dll"),
                    new WriterParameters { Timestamp = 0, DeterministicMvid = true });
            }
            if (mode == "unsupported") field = owner.Fields.Single(f => f.Name == "BaseNameField");

            using var app = AssemblyDefinition.CreateAssembly(new AssemblyNameDefinition("ResourceHeaderField", new Version(1, 0)),
                "ResourceHeaderField", ModuleKind.Console);
            var module = app.MainModule;
            var program = new TypeDefinition("Probe", "Program", TypeAttributes.Public, module.TypeSystem.Object);
            module.Types.Add(program);
            var main = new MethodDefinition("Main", MethodAttributes.Public | MethodAttributes.Static, module.TypeSystem.Void);
            program.Methods.Add(main);
            app.EntryPoint = main;
            FieldReference target = module.ImportReference(field);
            if (mode == "reference-type") target.FieldType = module.TypeSystem.Int64;
            if (mode == "owner")
            {
                using var other = AssemblyDefinition.CreateAssembly(new AssemblyNameDefinition("ResourceLookalike", new Version(1, 0)),
                    "ResourceLookalike", ModuleKind.Dll);
                var fake = new TypeDefinition("System.Resources", "ResourceManager", TypeAttributes.Public, other.MainModule.TypeSystem.Object);
                other.MainModule.Types.Add(fake);
                var fakeField = new FieldDefinition("MagicNumber", field.Attributes, other.MainModule.TypeSystem.Int32);
                fake.Fields.Add(fakeField);
                target = module.ImportReference(fakeField);
                other.Write(Path.Combine(directory, "ResourceLookalike.dll"));
            }
            var il = main.Body.GetILProcessor();
            il.Emit(mode == "address" ? OpCodes.Ldsflda : OpCodes.Ldsfld, target);
            il.Emit(OpCodes.Pop);
            il.Emit(OpCodes.Ret);
            app.Write(Path.Combine(directory, "ResourceHeaderField.dll"), new WriterParameters { Timestamp = 0, DeterministicMvid = true });
            Console.WriteLine("resource-header-field-fixture:" + mode);
        }
        return true;
    }
}

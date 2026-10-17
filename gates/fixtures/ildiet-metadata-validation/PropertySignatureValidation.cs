using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using Mono.Cecil;
using Mono.Cecil.Cil;
using AssemblyDefinition = Mono.Cecil.AssemblyDefinition;
using MethodDefinition = Mono.Cecil.MethodDefinition;
using ModuleDefinition = Mono.Cecil.ModuleDefinition;
using PropertyDefinition = Mono.Cecil.PropertyDefinition;
using TypeDefinition = Mono.Cecil.TypeDefinition;
using TypeReference = Mono.Cecil.TypeReference;

static class PropertySignatureValidation
{
    internal static bool Run(string[] args)
    {
        if (args.Length == 2 && args[0] == "--property-signature-count")
        {
            using var image = new PEReader(File.OpenRead(args[1]));
            var reader = image.GetMetadataReader();
            var property = reader.GetPropertyDefinition(reader.PropertyDefinitions.Single());
            var signature = reader.GetBlobReader(property.Signature);
            Require(signature.ReadSignatureHeader().Kind == SignatureKind.Property, "expected a property signature");
            Console.WriteLine("property-signature-count=" + signature.ReadCompressedInteger());
            return true;
        }
        if (args.Length == 4 && args[0] == "--create-property-signature-fixtures")
        {
            Create(args[1], args[2], args[3]);
            return true;
        }
        if (args.Length == 4 && args[0] == "--check-property-signature-fixtures")
        {
            Check(args[1], args[2], args[3]);
            return true;
        }
        return false;
    }

    private static void Create(string directory, string corelib, string mode)
    {
        Require(mode is "mismatch" or "matching" or "truncated", "unknown property signature mode");
        Directory.CreateDirectory(directory);
        using var core = AssemblyDefinition.ReadAssembly(corelib);
        using var dependency = AssemblyDefinition.CreateAssembly(new AssemblyNameDefinition("PropertyDependency", new Version(1, 0)), "PropertyDependency", ModuleKind.Dll);
        using var carrier = AssemblyDefinition.CreateAssembly(new AssemblyNameDefinition("PropertyCarrier", new Version(1, 0)), "PropertyCarrier", ModuleKind.Dll);
        using var app = AssemblyDefinition.CreateAssembly(new AssemblyNameDefinition("PropertyApp", new Version(1, 0)), "PropertyApp", ModuleKind.Console);
        TypeReference Core(ModuleDefinition module, string name) => module.ImportReference(core.MainModule.GetType("System." + name));
        TypeDefinition Owner(AssemblyDefinition assembly, string name)
        {
            var type = new TypeDefinition("Graph", name, TypeAttributes.Public | TypeAttributes.Class, Core(assembly.MainModule, "Object"));
            assembly.MainModule.Types.Add(type);
            return type;
        }
        MethodDefinition Method(TypeDefinition owner, string name, string result)
        {
            var method = new MethodDefinition(name, MethodAttributes.Public | MethodAttributes.Static, Core(owner.Module, result));
            owner.Methods.Add(method);
            return method;
        }
        var effects = Owner(dependency, "PropertyDependency");
        var touch = Method(effects, "Touch", "Void");
        var unused = Method(effects, "Unused", "Void");
        foreach (var method in new[] { touch, unused })
        {
            method.Body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4, method == touch ? 37 : 91));
            method.Body.Instructions.Add(Instruction.Create(OpCodes.Pop));
            method.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
        }
        var owner = Owner(carrier, "PropertyCarrier");
        var getter = Method(owner, "get_Value", "Boolean");
        getter.Attributes |= MethodAttributes.SpecialName | MethodAttributes.HideBySig;
        for (int i = 0; i < (mode == "truncated" ? 2 : 1); i++)
            getter.Parameters.Add(new ParameterDefinition(Core(carrier.MainModule, "Int32")));
        getter.Body.Instructions.Add(Instruction.Create(OpCodes.Call, carrier.MainModule.ImportReference(touch)));
        getter.Body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4_1));
        getter.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
        owner.Properties.Add(new PropertyDefinition("Value", PropertyAttributes.None, Core(carrier.MainModule, "Boolean")) { GetMethod = getter });
        var dormant = Method(owner, "Dormant", "Void");
        dormant.Body.Instructions.Add(Instruction.Create(OpCodes.Call, carrier.MainModule.ImportReference(unused)));
        dormant.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
        var main = Method(Owner(app, "PropertyEntry"), "Main", "Void");
        foreach (var parameter in getter.Parameters) main.Body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4_0));
        main.Body.Instructions.Add(Instruction.Create(OpCodes.Call, app.MainModule.ImportReference(getter)));
        main.Body.Instructions.Add(Instruction.Create(OpCodes.Pop));
        main.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
        app.MainModule.EntryPoint = main;
        foreach (var assembly in new[] { app, carrier, dependency })
            assembly.Write(Path.Combine(directory, assembly.Name.Name + ".dll"), new WriterParameters { Timestamp = 0, DeterministicMvid = true });
        if (mode != "matching")
        {
            string path = Path.Combine(directory, "PropertyCarrier.dll");
            byte[] bytes = File.ReadAllBytes(path);
            using var pe = new PEReader(new MemoryStream(bytes));
            var reader = pe.GetMetadataReader();
            var signature = reader.GetPropertyDefinition(reader.PropertyDefinitions.Single()).Signature;
            Require(reader.GetBlobBytes(signature).SequenceEqual(mode == "truncated" ? new byte[] { 8, 2, 2, 8, 8 } : new byte[] { 8, 1, 2, 8 }), "unexpected unpatched property signature");
            int offset = pe.PEHeaders.MetadataStartOffset + reader.GetHeapMetadataOffset(HeapIndex.Blob) + MetadataTokens.GetHeapOffset(signature);
            bytes[offset] = 3;
            bytes[offset + 2] = mode == "truncated" ? (byte)1 : (byte)0;
            File.WriteAllBytes(path, bytes);
        }
        Console.WriteLine("fixture-created=property-signature:" + mode);
    }

    private static void Check(string directory, string mode, string state)
    {
        Require(mode is "mismatch" or "matching" or "truncated", "unknown property signature mode");
        Require(state is "original" or "published" or "preserved", "unknown property signature state");
        string carrierPath = Path.Combine(directory, "PropertyCarrier.dll");
        using var pe = new PEReader(File.OpenRead(carrierPath));
        var reader = pe.GetMetadataReader();
        var row = reader.GetPropertyDefinition(reader.PropertyDefinitions.Single());
        byte[] expected = mode == "matching" ? new byte[] { 8, 1, 2, 8 } : new byte[] { 8, mode == "truncated" ? (byte)1 : (byte)0, 2 };
        Require(reader.GetBlobBytes(row.Signature).SequenceEqual(expected), "original property signature changed");
        using var carrier = AssemblyDefinition.ReadAssembly(carrierPath);
        var property = carrier.MainModule.GetType("Graph.PropertyCarrier").Properties.Single();
        Require(property.GetMethod is { } getter && getter.IsStatic && getter.Parameters.Count == (mode == "truncated" ? 2 : 1)
            && property.SetMethod is null && getter.Body.Instructions.Select(i => i.OpCode.Code).SequenceEqual(new[] { Code.Call, Code.Ldc_I4_1, Code.Ret }), "original getter shape or body changed");
        using var dependency = AssemblyDefinition.ReadAssembly(Path.Combine(directory, "PropertyDependency.dll"));
        var methods = dependency.MainModule.GetType("Graph.PropertyDependency").Methods;
        bool Body(string name, int value) => methods.SingleOrDefault(m => m.Name == name) is { HasBody: true } method
            && method.Body.Instructions.Select(i => i.OpCode.Code).SequenceEqual(new[] { Code.Ldc_I4, Code.Pop, Code.Ret })
            && (int)method.Body.Instructions[0].Operand == value;
        Require(Body("Touch", 37), "getter dependency body was lost");
        Require(Body("Unused", 91) == (state is "original" or "preserved"), "copied-to-rewritten body closure changed");
        Console.WriteLine("property-signature=" + state + ":" + mode + ":original-blob,getter,dependency-closure");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}

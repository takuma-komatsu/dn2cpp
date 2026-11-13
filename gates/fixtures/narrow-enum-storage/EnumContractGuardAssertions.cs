using Mono.Cecil;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using ModuleDefinition = Mono.Cecil.ModuleDefinition;
using GenericParameter = Mono.Cecil.GenericParameter;
using TypeReference = Mono.Cecil.TypeReference;
using BindingFlags = System.Reflection.BindingFlags;

internal static class EnumContractGuardAssertions
{
    internal static void Run(string corePath, string cliPath)
    {
        string directory = Path.GetDirectoryName(Path.GetFullPath(cliPath))!;
        var transpiler = System.Reflection.Assembly.LoadFrom(Path.Combine(directory, "Dn2Cpp.Transpiler.dll"));
        var maps = transpiler.GetType("Dn2Cpp.CoreIntrinsicTypeMaps", throwOnError: true)!;
        var declaration = maps.GetMethod("IsEnumRuntimeLeafDeclaration", BindingFlags.Public | BindingFlags.Static)!;
        var intrinsic = transpiler.GetType("Dn2Cpp.CoreIntrinsics", throwOnError: true)!;
        var signature = intrinsic.GetMethod("IsEnumRuntimeLeafSignature", BindingFlags.NonPublic | BindingFlags.Static)!;
        using var stream = File.OpenRead(corePath);
        using var pe = new PEReader(stream);
        var reader = pe.GetMetadataReader();
        int accepted = 0;
        foreach (var handle in reader.TypeDefinitions)
        {
            var type = reader.GetTypeDefinition(handle);
            if (reader.GetString(type.Namespace) != "System" || reader.GetString(type.Name) != "Enum") continue;
            foreach (var methodHandle in type.GetMethods())
            {
                var method = reader.GetMethodDefinition(methodHandle);
                string name = reader.GetString(method.Name);
                if (name is not ("GetName" or "InternalGetCorElementType" or "InternalGetUnderlyingType")) continue;
                bool isStatic = (method.Attributes & System.Reflection.MethodAttributes.Static) != 0;
                object[] arguments = { "System.Private.CoreLib", "System.Enum", name, isStatic,
                    method.GetGenericParameters().Count, (int)method.Attributes, (int)method.ImplAttributes };
                bool supported = (bool)declaration.Invoke(null, arguments)!;
                bool raw = (bool)signature.Invoke(null, new object[] { reader, name, isStatic, reader.GetBlobReader(method.Signature) })!;
                Require(supported == raw, "Enum leaf raw signature disagrees with its declaration");
                if (!supported) continue;
                accepted++;
                foreach (var mutation in new (int Index, object Value)[]
                    { (0, "Foreign.CoreLib"), (1, "Foreign.Enum"), (2, "Other"), (3, !isStatic),
                        (4, 1), (5, (int)method.Attributes ^ 1), (6, (int)method.ImplAttributes ^ 1) })
                {
                    object previous = arguments[mutation.Index];
                    arguments[mutation.Index] = mutation.Value;
                    Require(!(bool)declaration.Invoke(null, arguments)!, "Enum leaf admitted an unowned declaration");
                    arguments[mutation.Index] = previous;
                }
            }
        }
        Require(accepted == 4, "Enum runtime leaf declaration set changed");

        var diet = System.Reflection.Assembly.LoadFrom(Path.Combine(directory, "ildiet", "ILDiet.dll"));
        var copied = diet.GetType("Dn2Cpp.AssemblyDiet", throwOnError: true)!
            .GetMethod("IsCopiedEnumRuntimeLeaf", BindingFlags.NonPublic | BindingFlags.Static)!;
        using var core = ModuleDefinition.ReadModule(corePath);
        foreach (var method in core.GetType("System.Enum").Methods)
        {
            bool Matches() => (bool)copied.Invoke(null, new object[] { method, "System.Enum" })!;
            if (!Matches()) continue;
            string assembly = core.Assembly.Name.Name;
            core.Assembly.Name.Name = "Foreign.CoreLib";
            Require(!Matches(), "ILDiet accepted a foreign Enum leaf");
            core.Assembly.Name.Name = assembly;
            method.GenericParameters.Add(new GenericParameter("Unowned", method));
            Require(!Matches(), "ILDiet accepted a generic Enum leaf");
            method.GenericParameters.Clear();
            method.ExplicitThis = true;
            Require(!Matches(), "ILDiet accepted an explicit-this Enum leaf");
            method.ExplicitThis = false;
            var result = method.ReturnType;
            method.ReturnType = new ByReferenceType(result);
            Require(!Matches(), "ILDiet accepted a byref Enum leaf result");
            method.ReturnType = result;
            if (method.Parameters.Count > 0)
            {
                var parameter = method.Parameters[0].ParameterType;
                method.Parameters[0].ParameterType = new TypeReference("System", "RuntimeType", core, core);
                Require(!Matches(), "ILDiet accepted a non-definition RuntimeType lookalike");
                method.Parameters[0].ParameterType = parameter;
            }
            Require(Matches(), "ILDiet Enum leaf did not recover its exact declaration");
        }
        Console.WriteLine("enum-contract-guards=owned,non-generic,exact-signature,ildiet");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}

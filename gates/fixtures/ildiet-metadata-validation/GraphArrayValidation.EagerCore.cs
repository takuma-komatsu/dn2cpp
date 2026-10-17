using Mono.Cecil;
using Mono.Cecil.Cil;

static partial class GraphArrayValidation
{
    private static readonly string[] EagerCoreTypes =
    {
        "System.Object", "System.ValueType", "System.Void", "System.Int32", "System.Int64",
        "System.Attribute", "System.Type", "System.Array", "System.Globalization.CultureInfo",
        "System.Runtime.CompilerServices.AsyncMethodBuilderAttribute",
    };

    private static void CreateEagerCore(string directory, string source)
    {
        using var core = AssemblyDefinition.ReadAssembly(source);
        var module = core.MainModule;
        var types = EagerCoreTypes.ToDictionary(name => name, name => module.GetType(name));
        var moduleType = module.Types.Single(t => t.Name == "<Module>");
        var voidType = module.TypeSystem.Void;
        // Cecil's lazy collections and accessor links require the original attached graph.
        foreach (var type in new[] { moduleType }.Concat(types.Values))
        {
            _ = type.CustomAttributes.Count;
            _ = type.SecurityDeclarations.Count;
            _ = type.Fields.Count;
            _ = type.Methods.Count;
            _ = type.Properties.Count;
            foreach (var property in type.Properties)
            {
                _ = property.GetMethod;
                _ = property.SetMethod;
                _ = property.OtherMethods.Count;
            }
            _ = type.Events.Count;
            foreach (var item in type.Events)
            {
                _ = item.AddMethod;
                _ = item.RemoveMethod;
                _ = item.InvokeMethod;
                _ = item.OtherMethods.Count;
            }
            _ = type.Interfaces.Count;
            _ = type.GenericParameters.Count;
            _ = type.NestedTypes.Count;
            _ = type.PackingSize;
            _ = type.ClassSize;
            _ = type.BaseType;
        }
        _ = module.AssemblyReferences.Count;
        _ = module.ModuleReferences.Count;
        _ = module.ExportedTypes.Count;
        _ = module.Resources.Count;
        _ = module.CustomAttributes.Count;
        _ = core.CustomAttributes.Count;
        _ = core.SecurityDeclarations.Count;
        foreach (var type in types.Values)
        {
            type.CustomAttributes.Clear();
            type.SecurityDeclarations.Clear();
            type.Fields.Clear();
            type.Properties.Clear();
            type.Events.Clear();
            type.Methods.Clear();
            type.Interfaces.Clear();
            type.GenericParameters.Clear();
            type.NestedTypes.Clear();
            type.PackingSize = -1;
            type.ClassSize = -1;
            type.Attributes = TypeAttributes.Public | TypeAttributes.BeforeFieldInit;
            bool value = type.FullName is "System.Void" or "System.Int32" or "System.Int64";
            if (value) type.Attributes |= TypeAttributes.Sealed | TypeAttributes.SequentialLayout;
            type.BaseType = type.FullName == "System.Object" ? null : types[value ? "System.ValueType" : "System.Object"];
        }
        types["System.Runtime.CompilerServices.AsyncMethodBuilderAttribute"].BaseType = types["System.Attribute"];
        moduleType.Methods.Clear();
        moduleType.Fields.Clear();
        moduleType.CustomAttributes.Clear();
        module.AssemblyReferences.Clear();
        module.ModuleReferences.Clear();
        module.ExportedTypes.Clear();
        module.Resources.Clear();
        module.CustomAttributes.Clear();
        core.CustomAttributes.Clear();
        core.SecurityDeclarations.Clear();
        module.Types.Clear();
        module.Types.Add(moduleType);
        foreach (var type in types.Values) module.Types.Add(type);
        module.Attributes = ModuleAttributes.ILOnly;
        module.Architecture = TargetArchitecture.I386;
        module.Kind = ModuleKind.Dll;
        module.EntryPoint = null;

        MethodDefinition Method(string owner, string name, TypeReference result, bool instance, params TypeReference[] arguments)
        {
            var method = new MethodDefinition(name, MethodAttributes.Public | MethodAttributes.HideBySig
                | (instance ? 0 : MethodAttributes.Static), result);
            if (name == ".ctor") method.Attributes |= MethodAttributes.SpecialName | MethodAttributes.RTSpecialName;
            else if (name.StartsWith("get_", StringComparison.Ordinal) || name.StartsWith("set_", StringComparison.Ordinal))
                method.Attributes |= MethodAttributes.SpecialName;
            foreach (var argument in arguments) method.Parameters.Add(new ParameterDefinition(argument));
            types[owner].Methods.Add(method);
            return method;
        }
        var objectCtor = Method("System.Object", ".ctor", voidType, true);
        objectCtor.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
        var attributeCtor = Method("System.Attribute", ".ctor", voidType, true);
        attributeCtor.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
        attributeCtor.Body.Instructions.Add(Instruction.Create(OpCodes.Call, objectCtor));
        attributeCtor.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
        var builderCtor = Method("System.Runtime.CompilerServices.AsyncMethodBuilderAttribute", ".ctor",
            voidType, true, types["System.Type"]);
        builderCtor.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
        builderCtor.Body.Instructions.Add(Instruction.Create(OpCodes.Call, attributeCtor));
        builderCtor.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
        Method("System.Array", "Initialize", voidType, true).Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
        var culture = types["System.Globalization.CultureInfo"];
        var invariant = Method(culture.FullName, "get_InvariantCulture", culture, false);
        invariant.Body.Instructions.Add(Instruction.Create(OpCodes.Ldnull));
        invariant.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
        foreach (string setter in new[] { "set_CurrentCulture", "set_CurrentUICulture" })
            Method(culture.FullName, setter, voidType, false, culture).Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
        core.Write(Path.Combine(directory, "System.Private.CoreLib.dll"),
            new WriterParameters { Timestamp = 0, DeterministicMvid = true });

        using var runtime = AssemblyDefinition.CreateAssembly(new AssemblyNameDefinition("Dn2Cpp.Runtime", new Version(1, 0)),
            "Dn2Cpp.Runtime", ModuleKind.Dll);
        runtime.Write(Path.Combine(directory, "Dn2Cpp.Runtime.dll"),
            new WriterParameters { Timestamp = 0, DeterministicMvid = true });
    }

    private static void CheckEagerCore(string directory)
    {
        using var core = AssemblyDefinition.ReadAssembly(Path.Combine(directory, "System.Private.CoreLib.dll"));
        using var runtime = AssemblyDefinition.ReadAssembly(Path.Combine(directory, "Dn2Cpp.Runtime.dll"));
        Require(core.Name.Name == "System.Private.CoreLib" && core.Name.HasPublicKey
            && core.Name.PublicKeyToken.SequenceEqual(new byte[] { 0x7c, 0xec, 0x85, 0xd7, 0xbe, 0xa7, 0x79, 0x8e }),
            "isolated eager core identity changed");
        Require(core.MainModule.Types.Where(t => t.Name != "<Module>").Select(t => t.FullName)
            .SequenceEqual(EagerCoreTypes), "isolated eager core type surface changed");
        Require(core.MainModule.AssemblyReferences.Count == 0 && core.MainModule.ExportedTypes.Count == 0
            && core.MainModule.Resources.Count == 0 && !core.HasCustomAttributes && !core.MainModule.HasCustomAttributes,
            "isolated eager core gained an external dependency");
        Require(core.MainModule.Types.All(t => !t.HasGenericParameters && !t.HasInterfaces && !t.HasCustomAttributes
            && !t.HasFields && t.Methods.All(m => !m.HasGenericParameters && !m.IsVirtual && !m.HasOverrides)),
            "isolated eager core gained an unrelated formation source");
        Require(core.MainModule.Types.SelectMany(t => t.Methods).Select(m => m.FullName).OrderBy(n => n, StringComparer.Ordinal)
            .SequenceEqual(new[]
            {
                "System.Void System.Object::.ctor()",
                "System.Void System.Attribute::.ctor()",
                "System.Void System.Array::Initialize()",
                "System.Globalization.CultureInfo System.Globalization.CultureInfo::get_InvariantCulture()",
                "System.Void System.Globalization.CultureInfo::set_CurrentCulture(System.Globalization.CultureInfo)",
                "System.Void System.Globalization.CultureInfo::set_CurrentUICulture(System.Globalization.CultureInfo)",
                "System.Void System.Runtime.CompilerServices.AsyncMethodBuilderAttribute::.ctor(System.Type)",
            }.OrderBy(n => n, StringComparer.Ordinal)), "isolated eager core API surface changed");
        Require(runtime.Name.Name == "Dn2Cpp.Runtime" && runtime.MainModule.AssemblyReferences.Count == 0
            && runtime.MainModule.Types.Count == 1 && runtime.MainModule.Types[0].Name == "<Module>"
            && runtime.MainModule.Types[0].Methods.Count == 0 && runtime.MainModule.Types[0].Fields.Count == 0,
            "isolated eager runtime gained managed helpers");
        using var resolver = new EagerCoreResolver(directory);
        foreach (var assembly in resolver.Assemblies)
        {
            foreach (var reference in assembly.MainModule.AssemblyReferences) resolver.Resolve(reference);
            foreach (var type in assembly.MainModule.GetTypes())
                foreach (var method in type.Methods.Where(m => m.HasBody))
                    foreach (var instruction in method.Body.Instructions)
                    {
                        if (instruction.Operand is TypeReference target) CheckType(target);
                        else if (instruction.Operand is MethodReference called)
                        {
                            Require(called.Resolve() is not null, "isolated eager method operand is unresolved: " + called.FullName);
                            CheckType(called.DeclaringType);
                            CheckType(called.ReturnType);
                            foreach (var parameter in called.Parameters) CheckType(parameter.ParameterType);
                        }
                        else if (instruction.Operand is FieldReference field)
                            Require(field.Resolve() is not null, "isolated eager field operand is unresolved: " + field.FullName);
                    }
        }
        void CheckType(TypeReference type)
        {
            if (type is GenericParameter) return;
            if (type is GenericInstanceType generic)
                foreach (var argument in generic.GenericArguments) CheckType(argument);
            if (type is TypeSpecification specification) CheckType(specification.ElementType);
            else Require(type.Resolve() is not null, "isolated eager type operand is unresolved: " + type.FullName);
        }
    }

    private sealed class EagerCoreResolver : IAssemblyResolver
    {
        private readonly Dictionary<string, AssemblyDefinition> _assemblies = new(StringComparer.Ordinal);
        internal IEnumerable<AssemblyDefinition> Assemblies => _assemblies.Values;

        internal EagerCoreResolver(string directory)
        {
            foreach (string path in Directory.GetFiles(directory, "*.dll").OrderBy(p => p, StringComparer.Ordinal))
            {
                var assembly = AssemblyDefinition.ReadAssembly(path, new ReaderParameters { AssemblyResolver = this });
                Require(_assemblies.TryAdd(assembly.Name.Name, assembly), "duplicate isolated eager input identity");
            }
        }

        public AssemblyDefinition Resolve(AssemblyNameReference name)
        {
            Require(_assemblies.TryGetValue(name.Name, out var assembly), "isolated eager input attempted external resolution: " + name.FullName);
            return assembly!;
        }

        public AssemblyDefinition Resolve(AssemblyNameReference name, ReaderParameters parameters) => Resolve(name);

        public void Dispose()
        {
            foreach (var assembly in _assemblies.Values) assembly.Dispose();
        }
    }
}

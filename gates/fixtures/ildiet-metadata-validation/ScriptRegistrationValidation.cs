using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using Dn2Cpp.DotnetModule;
using Mono.Cecil;
using AssemblyDefinition = Mono.Cecil.AssemblyDefinition;
using TypeDefinition = Mono.Cecil.TypeDefinition;
using TypeReference = Mono.Cecil.TypeReference;

internal static class ScriptRegistrationValidation
{
    internal static void Run(string directory)
    {
        Directory.CreateDirectory(directory);
        using var engine = AssemblyDefinition.CreateAssembly(new AssemblyNameDefinition("GodotSharp", new Version(1, 0)),
            "GodotSharp", ModuleKind.Dll);
        var root = new TypeDefinition("Godot", "GodotObject", TypeAttributes.Public, engine.MainModule.TypeSystem.Object);
        engine.MainModule.Types.Add(root);
        string enginePath = Path.Combine(directory, "GodotSharp.dll");
        engine.Write(enginePath);
        foreach (string mode in new[] { "absent", "lookup", "null", "empty", "top", "nested", "full", "foreign" })
        {
            using var app = AssemblyDefinition.CreateAssembly(new AssemblyNameDefinition("EngineApp", new Version(1, 0)),
                "EngineApp", ModuleKind.Dll);
            var module = app.MainModule;
            AssemblyNameReference Scope(string name)
            {
                var scope = module.AssemblyReferences.FirstOrDefault(reference => reference.Name == name);
                if (scope is not null) return scope;
                scope = new AssemblyNameReference(name, new Version(1, 0));
                module.AssemblyReferences.Add(scope);
                return scope;
            }
            TypeDefinition Type(string name, TypeReference parent, bool path = true)
            {
                var type = new TypeDefinition("EngineFixture", name, TypeAttributes.Public, parent);
                module.Types.Add(type);
                if (path)
                {
                    var pathType = new TypeReference("Godot", "ScriptPathAttribute", module,
                        Scope("GodotSharp"));
                    var ctor = new MethodReference(".ctor", module.TypeSystem.Void, pathType) { HasThis = true };
                    ctor.Parameters.Add(new ParameterDefinition(module.TypeSystem.String));
                    var attribute = new Mono.Cecil.CustomAttribute(ctor);
                    attribute.ConstructorArguments.Add(new CustomAttributeArgument(module.TypeSystem.String, "res://" + name + ".cs"));
                    type.CustomAttributes.Add(attribute);
                }
                return type;
            }
            var top = Type("Top", module.ImportReference(root));
            var other = Type("Other", module.ImportReference(root));
            Type("NoPath", module.ImportReference(root), false);
            Type("NotGodot", module.TypeSystem.Object);
            var container = Type("Container", module.TypeSystem.Object, false);
            var nested = Type("Nested", module.ImportReference(root));
            module.Types.Remove(nested);
            nested.Namespace = "";
            nested.Attributes = TypeAttributes.NestedPublic;
            container.NestedTypes.Add(nested);
            if (mode != "absent")
            {
                var attributeType = new TypeReference("Godot", "AssemblyHasScriptsAttribute", module,
                    Scope(mode == "foreign" ? "Unrelated" : "GodotSharp"));
                var ctor = new MethodReference(".ctor", module.TypeSystem.Void, attributeType) { HasThis = true };
                var attribute = new Mono.Cecil.CustomAttribute(ctor);
                if (mode != "lookup")
                {
                    var systemType = module.ImportReference(typeof(System.Type));
                    var arrayType = new ArrayType(systemType);
                    ctor.Parameters.Add(new ParameterDefinition(arrayType));
                    TypeReference[] types = mode switch
                    {
                        "top" => new[] { top },
                        "nested" => new[] { nested },
                        "full" => new TypeReference[] { top, other, nested,
                            new TypeReference("EngineFixture", "Top", module, Scope("Shadow")) },
                        _ => Array.Empty<TypeDefinition>(),
                    };
                    attribute.ConstructorArguments.Add(new CustomAttributeArgument(arrayType,
                        mode == "null" ? null : types.Select(type => new CustomAttributeArgument(systemType, type)).ToArray()));
                }
                app.CustomAttributes.Add(attribute);
            }
            string path = Path.Combine(directory, mode + ".dll");
            app.Write(path);
            using var pe = new PEReader(File.OpenRead(path));
            var reader = pe.GetMetadataReader();
            void CheckScope(CustomAttributeHandle handle, string expected)
            {
                var constructor = reader.GetMemberReference((MemberReferenceHandle)reader.GetCustomAttribute(handle).Constructor);
                var owner = reader.GetTypeReference((TypeReferenceHandle)constructor.Parent);
                Require(owner.ResolutionScope.Kind == HandleKind.AssemblyReference,
                    mode + ": attribute owner has no assembly-reference row");
                var scope = reader.GetAssemblyReference((AssemblyReferenceHandle)owner.ResolutionScope);
                Require(reader.GetString(scope.Name) == expected, mode + ": attribute owner resolved the wrong assembly");
            }
            foreach (var handle in reader.GetAssemblyDefinition().GetCustomAttributes())
                CheckScope(handle, mode == "foreign" ? "Unrelated" : "GodotSharp");
            foreach (var handle in reader.TypeDefinitions)
                foreach (var attribute in reader.GetTypeDefinition(handle).GetCustomAttributes())
                    CheckScope(attribute, "GodotSharp");
            var registration = GodotScriptRegistration.Read(reader, new Names());
            bool Selected(string type, bool isNested = false) => registration.Includes(isNested,
                value => value == type + "@EngineApp");
            Require(registration.RequiresLookup == (mode == "lookup"), mode + ": constructor arity changed lookup semantics");
            Require(Selected("EngineFixture.Top") == (mode is "lookup" or "top" or "full"), mode + ": top-level selection changed");
            Require(Selected("EngineFixture.Other") == (mode is "lookup" or "full"), mode + ": unlisted selection changed");
            Require(Selected("EngineFixture.Container+Nested", true) == (mode is "nested" or "full"), mode + ": nested selection changed");
            Require(!registration.Includes(true, value => value == "EngineFixture.Top@OtherAssembly"),
                mode + ": serialized type assembly identity was discarded");
            Require(registration.Includes(true, value => value == "EngineFixture.Top@Shadow") == (mode == "full"),
                mode + ": explicit serialized type lost its assembly scope");
            var metadata = new GodotILDietMetadata(new[] { path, enginePath });
            string lookupRoots = string.Join(",", metadata.LookupScriptTypes().Select(row => row.Type).Order(StringComparer.Ordinal));
            Require(lookupRoots == (mode == "lookup" ? "EngineFixture.Other,EngineFixture.Top" : ""),
                mode + ": parameterless registration lost its exact runtime roots: " + lookupRoots);
        }
        Console.WriteLine("script-registration=absent,lookup-top-level,explicit-top-level,explicit-nested,null,empty,full,assembly-identity,lookup-runtime-roots");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private sealed class Names : ICustomAttributeTypeProvider<string>
    {
        public string GetPrimitiveType(PrimitiveTypeCode typeCode) => typeCode.ToString();
        public string GetTypeFromDefinition(MetadataReader reader, TypeDefinitionHandle handle, byte rawTypeKind)
        {
            var type = reader.GetTypeDefinition(handle);
            return reader.GetString(type.Namespace) + "." + reader.GetString(type.Name);
        }
        public string GetTypeFromReference(MetadataReader reader, TypeReferenceHandle handle, byte rawTypeKind)
        {
            var type = reader.GetTypeReference(handle);
            return reader.GetString(type.Namespace) + "." + reader.GetString(type.Name);
        }
        public string GetSZArrayType(string elementType) => elementType + "[]";
        public string GetSystemType() => "System.Type";
        public bool IsSystemType(string type) => type == "System.Type";
        public string GetTypeFromSerializedName(string name)
        {
            string[] parts = name.Split(',');
            return parts[0].Trim() + "@" + (parts.Length == 1 ? "EngineApp" : parts[1].Trim());
        }
        public PrimitiveTypeCode GetUnderlyingEnumType(string type) => PrimitiveTypeCode.Int32;
    }
}

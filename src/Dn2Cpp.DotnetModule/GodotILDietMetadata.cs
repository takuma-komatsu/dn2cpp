using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;
using SRME = System.Reflection.Metadata.Ecma335.MetadataTokens;

namespace Dn2Cpp.DotnetModule;

internal sealed class GodotILDietMetadata
{
    internal sealed class TypeRow
    {
        internal (string Assembly, string Type) Identity;
        internal (string Assembly, string Type) Base;
        internal (string Assembly, string Type) Declaring;
        internal readonly List<string> ScriptPaths = new();
        internal bool GlobalClass;
        internal bool InvalidScriptPath;
        internal bool CompilerGenerated;
    }

    internal readonly Dictionary<(string Assembly, string Type), TypeRow> Types = new();
    internal readonly List<string> Assemblies = new();

    internal GodotILDietMetadata(IReadOnlyList<string> paths)
    {
        foreach (string path in paths)
        {
            using var pe = new PEReader(ImmutableCollectionsMarshal.AsImmutableArray(File.ReadAllBytes(path)));
            var reader = pe.GetMetadataReader();
            string assembly = reader.GetString(reader.GetAssemblyDefinition().Name);
            Assemblies.Add(assembly);
            foreach (var handle in reader.TypeDefinitions)
            {
                var type = reader.GetTypeDefinition(handle);
                var row = new TypeRow
                {
                    Identity = Identity(reader, handle, assembly),
                    Base = Identity(reader, type.BaseType, assembly),
                    Declaring = Identity(reader, type.GetDeclaringType(), assembly),
                };
                foreach (var attributeHandle in type.GetCustomAttributes())
                {
                    var attribute = reader.GetCustomAttribute(attributeHandle);
                    EntityHandle owner = attribute.Constructor.Kind switch
                    {
                        HandleKind.MemberReference => reader.GetMemberReference(
                            (MemberReferenceHandle)attribute.Constructor).Parent,
                        HandleKind.MethodDefinition => reader.GetMethodDefinition(
                            (MethodDefinitionHandle)attribute.Constructor).GetDeclaringType(),
                        _ => default,
                    };
                    var attributeType = Identity(reader, owner, assembly);
                    if (attributeType.Type == "Godot.GlobalClassAttribute")
                        row.GlobalClass = true;
                    if (attributeType.Type == "System.Runtime.CompilerServices.CompilerGeneratedAttribute")
                        row.CompilerGenerated = true;
                    if (attributeType.Type != "Godot.ScriptPathAttribute")
                        continue;
                    try
                    {
                        var blob = reader.GetBlobReader(attribute.Value);
                        if (blob.ReadUInt16() != 1)
                            throw new BadImageFormatException("Invalid attribute prolog");
                        string? script = blob.ReadSerializedString();
                        if (script is null || !script.StartsWith("res://", StringComparison.Ordinal))
                            row.InvalidScriptPath = true;
                        else
                            row.ScriptPaths.Add(script);
                    }
                    catch (BadImageFormatException)
                    {
                        row.InvalidScriptPath = true;
                    }
                }
                Types.TryAdd(row.Identity, row);
            }
        }
    }

    internal bool IsEngineWrapper(string name)
    {
        return Types.TryGetValue(("GodotSharp", name), out var row) && IsGodotType(row);
    }

    internal IEnumerable<(string Assembly, string Type)> GeneratedScriptHelpers()
    {
        foreach (var row in Types.Values)
        {
            if (row.Identity.Assembly == "GodotSharp" || string.IsNullOrEmpty(row.Declaring.Type))
                continue;
            bool generated = row.CompilerGenerated || IsSdkHelper(row);
            bool scriptAncestor = false;
            var visited = new HashSet<(string Assembly, string Type)>();
            var declaring = row.Declaring;
            while (Types.TryGetValue(declaring, out var owner) && visited.Add(declaring))
            {
                generated |= owner.CompilerGenerated || IsSdkHelper(owner);
                scriptAncestor |= IsGodotType(owner);
                declaring = owner.Declaring;
            }
            if (generated && scriptAncestor)
                yield return row.Identity;
        }
    }

    private bool IsGodotType(TypeRow type)
    {
        var identity = type.Identity;
        var visited = new HashSet<(string Assembly, string Type)>();
        while (Types.TryGetValue(identity, out var row) && visited.Add(identity))
        {
            if (identity == ("GodotSharp", "Godot.GodotObject"))
                return true;
            identity = row.Base;
        }
        return false;
    }

    private bool IsSdkHelper(TypeRow type)
    {
        if (!Types.TryGetValue(type.Declaring, out var owner) || !IsGodotType(owner))
            return false;
        string name = type.Identity.Type.Substring(type.Identity.Type.LastIndexOf('+') + 1);
        if (name is not ("MethodName" or "PropertyName" or "SignalName"))
            return false;
        var visited = new HashSet<(string Assembly, string Type)>();
        var parent = type.Base;
        while (Types.TryGetValue(parent, out var helper) && visited.Add(parent))
        {
            if (!helper.Identity.Type.EndsWith("+" + name, StringComparison.Ordinal))
                return false;
            if (helper.Identity.Assembly == "GodotSharp"
                && Types.TryGetValue(helper.Declaring, out var engine) && IsGodotType(engine))
                return true;
            parent = helper.Base;
        }
        return false;
    }

    private static (string Assembly, string Type) Identity(
        MetadataReader reader, EntityHandle handle, string assembly)
    {
        if (handle.IsNil)
            return ("", "");
        if (handle.Kind == HandleKind.TypeDefinition)
        {
            var type = reader.GetTypeDefinition((TypeDefinitionHandle)handle);
            string name = reader.GetString(type.Name);
            var parent = type.GetDeclaringType();
            if (!parent.IsNil)
                return (assembly, Identity(reader, parent, assembly).Type + "+" + name);
            string ns = reader.GetString(type.Namespace);
            return (assembly, ns.Length == 0 ? name : ns + "." + name);
        }
        if (handle.Kind == HandleKind.TypeReference)
        {
            var type = reader.GetTypeReference((TypeReferenceHandle)handle);
            string name = reader.GetString(type.Name);
            if (type.ResolutionScope.Kind == HandleKind.TypeReference)
            {
                var parent = Identity(reader, type.ResolutionScope, assembly);
                return (parent.Assembly, parent.Type + "+" + name);
            }
            if (type.ResolutionScope.Kind == HandleKind.AssemblyReference)
                assembly = reader.GetString(reader.GetAssemblyReference(
                    (AssemblyReferenceHandle)type.ResolutionScope).Name);
            string ns = reader.GetString(type.Namespace);
            return (assembly, ns.Length == 0 ? name : ns + "." + name);
        }
        if (handle.Kind == HandleKind.TypeSpecification)
        {
            var blob = reader.GetBlobReader(reader.GetTypeSpecification((TypeSpecificationHandle)handle).Signature);
            if (blob.ReadSignatureTypeCode() != SignatureTypeCode.GenericTypeInstance)
                return ("", "");
            blob.ReadSignatureTypeCode();
            int coded = blob.ReadCompressedInteger();
            EntityHandle definition = (coded & 3) switch
            {
                0 => SRME.TypeDefinitionHandle(coded >> 2),
                1 => SRME.TypeReferenceHandle(coded >> 2),
                _ => default,
            };
            return Identity(reader, definition, assembly);
        }
        return ("", "");
    }
}

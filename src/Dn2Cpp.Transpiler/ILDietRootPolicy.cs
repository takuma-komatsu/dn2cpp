using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;
using SRME = System.Reflection.Metadata.Ecma335.MetadataTokens;

namespace Dn2Cpp;

/// <summary>Backend entry points that cannot be discovered from managed call sites.</summary>
internal sealed class ILDietRootPolicy
{
    internal readonly List<string> BaseTypes = new();
    internal bool PreservePublicAppTypes;
    internal readonly List<string> ExcludedAssemblies = new();
    internal readonly List<string> RewriteAssemblies = new();
    internal readonly List<(string Assembly, string Type)> TypeRoots = new();
    internal readonly List<(string Assembly, string Type)> FullTypeRoots = new();
    internal readonly List<(string Assembly, string Type, string Method)> MethodRoots = new();
    internal readonly List<(string Assembly, string BaseType)> ConditionalMembers = new();
    internal readonly List<(string Assembly, string Type)> SuppressedSeedTypes = new();
    internal readonly List<(string Assembly, string Type)> RegistrationAttributes = new();
    internal readonly List<(string Assembly, string Type, string Method)> ConstructorRegistries = new();

    private sealed class TypeRow
    {
        internal (string Assembly, string Name) Identity;
        internal (string Assembly, string Name) Base;
        internal bool PublicApp;
    }

    internal IEnumerable<(string Assembly, string Type)> Resolve(IReadOnlyList<string> paths)
    {
        if (BaseTypes.Count == 0 && !PreservePublicAppTypes)
            yield break;
        var rows = new List<TypeRow>();
        var types = new Dictionary<(string Assembly, string Name), TypeRow>();
        var forwarders = new Dictionary<(string Assembly, string Name), (string Assembly, string Name)>();
        for (int i = 0; i < paths.Count; i++)
        {
            using var pe = new PEReader(ImmutableCollectionsMarshal.AsImmutableArray(File.ReadAllBytes(paths[i])));
            var reader = pe.GetMetadataReader();
            string assembly = reader.GetString(reader.GetAssemblyDefinition().Name);
            foreach (var handle in reader.TypeDefinitions)
            {
                var type = reader.GetTypeDefinition(handle);
                var row = new TypeRow
                {
                    Identity = TypeIdentity(reader, handle, assembly),
                    Base = TypeIdentity(reader, type.BaseType, assembly),
                    PublicApp = i == 0 && (type.Attributes & TypeAttributes.VisibilityMask)
                        is TypeAttributes.Public or TypeAttributes.NestedPublic,
                };
                rows.Add(row);
                types.TryAdd(row.Identity, row);
            }
            foreach (var handle in reader.ExportedTypes)
            {
                var target = ExportedIdentity(reader, handle, assembly);
                forwarders.TryAdd((assembly, target.Name), target);
            }
        }
        foreach (var row in rows)
        {
            if (ExcludedAssemblies.Contains(row.Identity.Assembly))
                continue;
            bool preserve = PreservePublicAppTypes && row.PublicApp;
            var visited = new HashSet<(string Assembly, string Name)>();
            var baseType = row.Base;
            while (!preserve && baseType.Name.Length != 0 && visited.Add(baseType))
            {
                preserve = BaseTypes.Contains(baseType.Name);
                if (forwarders.TryGetValue(baseType, out var forwarded))
                    baseType = forwarded;
                else
                    baseType = types.TryGetValue(baseType, out var parent) ? parent.Base : ("", "");
            }
            if (preserve)
                yield return (row.Identity.Assembly, row.Identity.Name);
        }
    }

    private static (string Assembly, string Name) TypeIdentity(
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
                return (assembly, TypeIdentity(reader, parent, assembly).Name + "+" + name);
            string ns = reader.GetString(type.Namespace);
            return (assembly, ns.Length == 0 ? name : ns + "." + name);
        }
        if (handle.Kind == HandleKind.TypeReference)
        {
            var type = reader.GetTypeReference((TypeReferenceHandle)handle);
            string name = reader.GetString(type.Name);
            if (type.ResolutionScope.Kind == HandleKind.TypeReference)
            {
                var parent = TypeIdentity(reader, type.ResolutionScope, assembly);
                return (parent.Assembly, parent.Name + "+" + name);
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
            return TypeIdentity(reader, definition, assembly);
        }
        return ("", "");
    }

    private static (string Assembly, string Name) ExportedIdentity(
        MetadataReader reader, ExportedTypeHandle handle, string assembly)
    {
        var type = reader.GetExportedType(handle);
        string name = reader.GetString(type.Name);
        if (type.Implementation.Kind == HandleKind.ExportedType)
        {
            var parent = ExportedIdentity(reader, (ExportedTypeHandle)type.Implementation, assembly);
            return (parent.Assembly, parent.Name + "+" + name);
        }
        if (type.Implementation.Kind == HandleKind.AssemblyReference)
            assembly = reader.GetString(reader.GetAssemblyReference((AssemblyReferenceHandle)type.Implementation).Name);
        string ns = reader.GetString(type.Namespace);
        return (assembly, ns.Length == 0 ? name : ns + "." + name);
    }
}

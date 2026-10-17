using System.Collections.Immutable;
using System.Reflection.Metadata;

namespace Dn2Cpp.DotnetModule;

internal static class GodotScriptRegistration
{
    internal sealed class Selection<T>
    {
        internal bool RequiresLookup;
        internal T[] Types = Array.Empty<T>();

        internal bool Includes(bool nested, Func<T, bool> matches)
        {
            if (RequiresLookup) return !nested;
            foreach (var type in Types)
                if (matches(type)) return true;
            return false;
        }
    }

    internal static bool RequiresLookup(MetadataReader reader)
    {
        foreach (var handle in reader.GetAssemblyDefinition().GetCustomAttributes())
        {
            var attribute = reader.GetCustomAttribute(handle);
            if (IsRegistration(reader, attribute)) return ParameterCount(reader, attribute) == 0;
        }
        return false;
    }

    internal static Selection<T> Read<T>(MetadataReader reader, ICustomAttributeTypeProvider<T> provider)
    {
        foreach (var handle in reader.GetAssemblyDefinition().GetCustomAttributes())
        {
            var attribute = reader.GetCustomAttribute(handle);
            if (!IsRegistration(reader, attribute)) continue;
            var value = attribute.DecodeValue(provider);
            if (value.NamedArguments.Length != 0)
                throw new NotSupportedException("AssemblyHasScriptsAttribute has no writable named members");
            if (value.FixedArguments.Length == 0) return new Selection<T> { RequiresLookup = true };
            if (value.FixedArguments.Length != 1)
                throw new NotSupportedException("unsupported AssemblyHasScriptsAttribute constructor");
            if (value.FixedArguments[0].Value is null) return new Selection<T>();
            if (value.FixedArguments[0].Value is not ImmutableArray<CustomAttributeTypedArgument<T>> array)
                throw new NotSupportedException("AssemblyHasScriptsAttribute requires a Type array");
            var types = new T[array.Length];
            for (int i = 0; i < array.Length; i++) types[i] = (T)array[i].Value!;
            return new Selection<T> { Types = types };
        }
        return new Selection<T>();
    }

    private static int ParameterCount(MetadataReader reader, CustomAttribute attribute)
    {
        var signature = attribute.Constructor.Kind == HandleKind.MemberReference
            ? reader.GetMemberReference((MemberReferenceHandle)attribute.Constructor).Signature
            : reader.GetMethodDefinition((MethodDefinitionHandle)attribute.Constructor).Signature;
        var blob = reader.GetBlobReader(signature);
        var header = blob.ReadSignatureHeader();
        if (header.IsGeneric) blob.ReadCompressedInteger();
        return blob.ReadCompressedInteger();
    }

    private static bool IsRegistration(MetadataReader reader, CustomAttribute attribute)
    {
        EntityHandle owner = attribute.Constructor.Kind switch
        {
            HandleKind.MemberReference => reader.GetMemberReference((MemberReferenceHandle)attribute.Constructor).Parent,
            HandleKind.MethodDefinition => reader.GetMethodDefinition((MethodDefinitionHandle)attribute.Constructor).GetDeclaringType(),
            _ => default,
        };
        if (owner.Kind == HandleKind.TypeReference)
        {
            var type = reader.GetTypeReference((TypeReferenceHandle)owner);
            return reader.StringComparer.Equals(type.Namespace, "Godot")
                && reader.StringComparer.Equals(type.Name, "AssemblyHasScriptsAttribute")
                && type.ResolutionScope.Kind == HandleKind.AssemblyReference
                && reader.StringComparer.Equals(reader.GetAssemblyReference(
                    (AssemblyReferenceHandle)type.ResolutionScope).Name, "GodotSharp");
        }
        if (owner.Kind == HandleKind.TypeDefinition)
        {
            var type = reader.GetTypeDefinition((TypeDefinitionHandle)owner);
            return reader.StringComparer.Equals(type.Namespace, "Godot")
                && reader.StringComparer.Equals(type.Name, "AssemblyHasScriptsAttribute")
                && reader.StringComparer.Equals(reader.GetAssemblyDefinition().Name, "GodotSharp");
        }
        return false;
    }
}

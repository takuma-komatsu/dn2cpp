using System.Reflection;
using System.Reflection.Metadata;

namespace Dn2Cpp;

// Bind the accessor's erased object return to the getter's actual reference type.
internal static class UnsafeAccessorGetter
{
    internal static EntityHandle Receiver(BlobReader signature)
    {
        if (signature.RemainingBytes < 5 || signature.ReadByte() != 0 || signature.ReadCompressedInteger() != 1
            || signature.ReadByte() != (byte)SignatureTypeCode.Object || signature.ReadByte() != (byte)SignatureTypeKind.Class)
            return default;
        var type = signature.ReadTypeHandle();
        return signature.RemainingBytes == 0 ? type : default;
    }

    internal static (TModule Module, MethodDefinitionHandle Method)? Bind<TModule>(
        TModule source, MethodDefinitionHandle handle, Func<TModule, MetadataReader> read,
        Func<TModule, EntityHandle, (TModule Module, TypeDefinitionHandle Type)?> resolve,
        Func<TModule, TypeDefinitionHandle, bool> isReferenceDefinition,
        Func<string, string, string, string, bool> forwardsTo)
        where TModule : class
    {
        try { return FindTarget(source, handle, read, resolve, isReferenceDefinition, forwardsTo); }
        catch (BadImageFormatException) { return null; }
    }

    private static (TModule Module, MethodDefinitionHandle Method)? FindTarget<TModule>(
        TModule source, MethodDefinitionHandle handle, Func<TModule, MetadataReader> read,
        Func<TModule, EntityHandle, (TModule Module, TypeDefinitionHandle Type)?> resolve,
        Func<TModule, TypeDefinitionHandle, bool> isReferenceDefinition,
        Func<string, string, string, string, bool> forwardsTo)
        where TModule : class
    {
        var reader = read(source);
        var method = reader.GetMethodDefinition(handle);
        const MethodImplAttributes hints = MethodImplAttributes.AggressiveInlining | MethodImplAttributes.NoInlining
            | MethodImplAttributes.AggressiveOptimization | MethodImplAttributes.NoOptimization;
        if (method.GetCustomAttributes().Count == 0
            || (method.Attributes & (MethodAttributes.Static | MethodAttributes.Abstract | MethodAttributes.PinvokeImpl)) != MethodAttributes.Static
            || method.RelativeVirtualAddress != 0 || (method.ImplAttributes & ~hints) != MethodImplAttributes.IL
            || method.GetGenericParameters().Count != 0 || reader.GetTypeDefinition(method.GetDeclaringType()).GetGenericParameters().Count != 0)
            return null;
        var receiver = Receiver(reader.GetBlobReader(method.Signature));
        if (receiver.IsNil) return null;

        string? name = null, result = null;
        foreach (var row in method.GetCustomAttributes())
        {
            var attribute = reader.GetCustomAttribute(row);
            if (!IsAttribute(attribute, "UnsafeAccessorAttribute")) continue;
            var blob = reader.GetBlobReader(attribute.Value);
            if (name is not null || blob.ReadUInt16() != 1 || blob.ReadInt32() != 1) return null;
            int named = blob.ReadUInt16();
            if (named == 0)
                name = reader.GetString(method.Name);
            else if (named == 1 && blob.ReadByte() == 0x54 && blob.ReadByte() == (byte)SignatureTypeCode.String
                && blob.ReadSerializedString() == "Name")
                name = blob.ReadSerializedString();
            else return null;
            if (name is null || !name.StartsWith("get_", StringComparison.Ordinal) || blob.RemainingBytes != 0) return null;
        }
        if (name is null) return null;
        foreach (var parameter in method.GetParameters())
        {
            var p = reader.GetParameter(parameter);
            foreach (var row in p.GetCustomAttributes())
            {
                var attribute = reader.GetCustomAttribute(row);
                if (!IsAttribute(attribute, "UnsafeAccessorTypeAttribute")) continue;
                if (p.SequenceNumber != 0 || result is not null) return null;
                var blob = reader.GetBlobReader(attribute.Value);
                if (blob.ReadUInt16() != 1) return null;
                result = blob.ReadSerializedString();
                if (result is null || blob.ReadUInt16() != 0 || blob.RemainingBytes != 0) return null;
            }
        }
        if (result is null) return null;
        int comma = result.IndexOf(',');
        if (comma <= 0 || resolve(source, receiver) is not { } owner)
            return null;
        // Whitespace immediately before the assembly separator belongs to the type name.
        string resultName = result[..comma].TrimStart();
        string resultAssembly = result[(comma + 1)..].Trim();
        var targetReader = read(owner.Module);
        var type = targetReader.GetTypeDefinition(owner.Type);
        if (type.GetGenericParameters().Count != 0 || (type.Attributes & TypeAttributes.Interface) != 0
            || !isReferenceDefinition(owner.Module, owner.Type)) return null;
        MethodDefinitionHandle found = default;
        foreach (var candidate in type.GetMethods())
        {
            var getter = targetReader.GetMethodDefinition(candidate);
            if (!targetReader.StringComparer.Equals(getter.Name, name)) continue;
            var sig = targetReader.GetBlobReader(getter.Signature);
            if (sig.ReadByte() != 0x20 || sig.ReadCompressedInteger() != 0 || !MatchesResult(ref sig) || sig.RemainingBytes != 0)
                continue;
            if (!found.IsNil || getter.RelativeVirtualAddress == 0 || getter.GetGenericParameters().Count != 0
                || (getter.ImplAttributes & (MethodImplAttributes.CodeTypeMask | MethodImplAttributes.ManagedMask
                    | MethodImplAttributes.InternalCall)) != MethodImplAttributes.IL
                || (getter.Attributes & (MethodAttributes.SpecialName | MethodAttributes.Static | MethodAttributes.Virtual
                    | MethodAttributes.Abstract | MethodAttributes.PinvokeImpl)) != MethodAttributes.SpecialName)
                return null;
            found = candidate;
        }
        return found.IsNil ? null : (owner.Module, found);

        bool MatchesResult(ref BlobReader sig)
        {
            byte kind = sig.ReadByte();
            if (kind is (byte)SignatureTypeCode.String or (byte)SignatureTypeCode.Object)
            {
                string name = kind == (byte)SignatureTypeCode.String ? "String" : "Object";
                return resultName == "System." + name
                    && (resultAssembly.Equals("System.Private.CoreLib", StringComparison.OrdinalIgnoreCase)
                        || forwardsTo(resultAssembly, "System", name, "System.Private.CoreLib"));
            }
            if (kind != (byte)SignatureTypeKind.Class) return false;
            var declared = sig.ReadTypeHandle();
            if (resolve(owner.Module, declared) is not { } returned) return false;
            var rr = read(returned.Module);
            var type = rr.GetTypeDefinition(returned.Type);
            if (type.GetGenericParameters().Count != 0
                || RawSignatureProvider.TypeDefinitionName(rr, returned.Type) != resultName
                || !isReferenceDefinition(returned.Module, returned.Type)) return false;
            string definition = rr.GetString(rr.GetAssemblyDefinition().Name);
            if (resultAssembly.Equals(definition, StringComparison.OrdinalIgnoreCase)) return true;

            // The getter's own scoped TypeRef can prove the identity even with an omitted facade.
            var scope = declared;
            while (scope.Kind == HandleKind.TypeReference)
                scope = targetReader.GetTypeReference((TypeReferenceHandle)scope).ResolutionScope;
            if (scope.Kind == HandleKind.AssemblyReference && targetReader.GetString(
                targetReader.GetAssemblyReference((AssemblyReferenceHandle)scope).Name).Equals(resultAssembly, StringComparison.OrdinalIgnoreCase))
                return true;
            while (!type.GetDeclaringType().IsNil) type = rr.GetTypeDefinition(type.GetDeclaringType());
            return forwardsTo(resultAssembly, rr.GetString(type.Namespace), rr.GetString(type.Name), definition);
        }

        bool IsAttribute(CustomAttribute attribute, string expected)
        {
            var ctor = attribute.Constructor;
            StringHandle ctorName;
            BlobHandle ctorSignature;
            EntityHandle parent;
            if (ctor.Kind == HandleKind.MemberReference)
            {
                var member = reader.GetMemberReference((MemberReferenceHandle)ctor);
                (ctorName, ctorSignature, parent) = (member.Name, member.Signature, member.Parent);
            }
            else if (ctor.Kind == HandleKind.MethodDefinition)
            {
                var definition = reader.GetMethodDefinition((MethodDefinitionHandle)ctor);
                (ctorName, ctorSignature, parent) = (definition.Name, definition.Signature, definition.GetDeclaringType());
            }
            else return false;
            if (!reader.StringComparer.Equals(ctorName, ".ctor") || !IsCoreType(parent, expected)) return false;
            var sig = reader.GetBlobReader(ctorSignature);
            if (sig.ReadByte() != 0x20 || sig.ReadCompressedInteger() != 1 || sig.ReadByte() != (byte)SignatureTypeCode.Void)
                return false;
            return (expected == "UnsafeAccessorTypeAttribute"
                ? sig.ReadByte() == (byte)SignatureTypeCode.String
                : sig.ReadByte() == (byte)SignatureTypeKind.ValueType && IsCoreType(sig.ReadTypeHandle(), "UnsafeAccessorKind"))
                && sig.RemainingBytes == 0;
        }

        bool IsCoreType(EntityHandle token, string expected)
        {
            var (ns, name) = token.Kind switch
            {
                HandleKind.TypeDefinition => (reader.GetTypeDefinition((TypeDefinitionHandle)token).Namespace,
                    reader.GetTypeDefinition((TypeDefinitionHandle)token).Name),
                HandleKind.TypeReference => (reader.GetTypeReference((TypeReferenceHandle)token).Namespace,
                    reader.GetTypeReference((TypeReferenceHandle)token).Name),
                _ => default,
            };
            if (!reader.StringComparer.Equals(name, expected)
                || !reader.StringComparer.Equals(ns, "System.Runtime.CompilerServices")) return false;
            if (resolve(source, token) is not { } resolved) return false;
            var definitionReader = read(resolved.Module);
            return definitionReader.StringComparer.Equals(definitionReader.GetAssemblyDefinition().Name, "System.Private.CoreLib")
                && definitionReader.GetTypeDefinition(resolved.Type).GetDeclaringType().IsNil;
        }
    }
}

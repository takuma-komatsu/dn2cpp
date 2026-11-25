using System.Reflection;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;

namespace Dn2Cpp;

internal static class UnsafeAccessorBinding
{
    internal enum TypeShape { Reference, Value, Enum }

    internal static EntityHandle GetterReceiver(BlobReader signature)
    {
        if (signature.RemainingBytes < 5 || signature.ReadByte() != 0 || signature.ReadCompressedInteger() != 1
            || signature.ReadByte() != (byte)SignatureTypeCode.Object || signature.ReadByte() != (byte)SignatureTypeKind.Class)
            return default;
        var type = signature.ReadTypeHandle();
        return signature.RemainingBytes == 0 ? type : default;
    }

    internal static EntityHandle FieldEnum(BlobReader signature)
    {
        if (signature.RemainingBytes < 6 || signature.ReadByte() != 0 || signature.ReadCompressedInteger() != 1
            || signature.ReadByte() != (byte)SignatureTypeCode.ByReference || signature.ReadByte() != (byte)SignatureTypeKind.ValueType)
            return default;
        var type = signature.ReadTypeHandle();
        return signature.RemainingBytes == 1 && signature.ReadByte() == (byte)SignatureTypeCode.Object ? type : default;
    }

    internal static (TModule Module, EntityHandle Member)? Bind<TModule>(
        TModule source, MethodDefinitionHandle handle, Func<TModule, MetadataReader> read,
        Func<TModule, EntityHandle, bool, (TModule Module, TypeDefinitionHandle Type)?> resolve,
        Func<TModule, TypeDefinitionHandle, TypeShape> shape,
        Func<string?, string, (TModule Module, TypeDefinitionHandle Type)?> findOwner,
        Func<string, string, string, string, bool> forwardsTo)
        where TModule : class
    {
        try { return FindTarget(source, handle, read, resolve, shape, findOwner, forwardsTo); }
        catch (BadImageFormatException) { return null; }
    }

    private static (TModule Module, EntityHandle Member)? FindTarget<TModule>(
        TModule source, MethodDefinitionHandle handle, Func<TModule, MetadataReader> read,
        Func<TModule, EntityHandle, bool, (TModule Module, TypeDefinitionHandle Type)?> resolve,
        Func<TModule, TypeDefinitionHandle, TypeShape> shape,
        Func<string?, string, (TModule Module, TypeDefinitionHandle Type)?> findOwner,
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
        var receiver = GetterReceiver(reader.GetBlobReader(method.Signature));
        var fieldEnum = receiver.IsNil ? FieldEnum(reader.GetBlobReader(method.Signature)) : default;
        if (receiver.IsNil && fieldEnum.IsNil) return null;

        string? name = null, annotatedType = null;
        foreach (var row in method.GetCustomAttributes())
        {
            var attribute = reader.GetCustomAttribute(row);
            if (!IsAttribute(attribute, "UnsafeAccessorAttribute")) continue;
            var blob = reader.GetBlobReader(attribute.Value);
            if (name is not null || blob.ReadUInt16() != 1
                || blob.ReadInt32() != (int)(fieldEnum.IsNil ? UnsafeAccessorKind.Method : UnsafeAccessorKind.Field)) return null;
            int named = blob.ReadUInt16();
            if (named == 0)
                name = reader.GetString(method.Name);
            else if (named == 1 && blob.ReadByte() == 0x54 && blob.ReadByte() == (byte)SignatureTypeCode.String
                && blob.ReadSerializedString() == "Name")
                name = blob.ReadSerializedString();
            else return null;
            if (string.IsNullOrEmpty(name) || fieldEnum.IsNil && !name.StartsWith("get_", StringComparison.Ordinal)
                || blob.RemainingBytes != 0) return null;
        }
        if (name is null) return null;
        foreach (var parameter in method.GetParameters())
        {
            var p = reader.GetParameter(parameter);
            foreach (var row in p.GetCustomAttributes())
            {
                var attribute = reader.GetCustomAttribute(row);
                if (!IsAttribute(attribute, "UnsafeAccessorTypeAttribute")) continue;
                if (p.SequenceNumber != (fieldEnum.IsNil ? 0 : 1) || annotatedType is not null) return null;
                var blob = reader.GetBlobReader(attribute.Value);
                if (blob.ReadUInt16() != 1) return null;
                annotatedType = blob.ReadSerializedString();
                if (annotatedType is null || blob.ReadUInt16() != 0 || blob.RemainingBytes != 0) return null;
            }
        }
        if (annotatedType is null) return null;
        int comma = annotatedType.IndexOf(',');
        if (comma == 0 || comma < 0 && fieldEnum.IsNil) return null;
        // Whitespace immediately before the assembly separator belongs to the type name.
        string typeName = (comma < 0 ? annotatedType : annotatedType[..comma]).TrimStart();
        string assembly = comma < 0 ? "" : annotatedType[(comma + 1)..].Trim();
        var ownerDefinition = fieldEnum.IsNil ? resolve(source, receiver, false) : findOwner(comma < 0 ? null : assembly, typeName);
        if (ownerDefinition is not { } owner) return null;
        var targetReader = read(owner.Module);
        var type = targetReader.GetTypeDefinition(owner.Type);
        if (type.GetGenericParameters().Count != 0 || (type.Attributes & TypeAttributes.Interface) != 0
            || shape(owner.Module, owner.Type) != TypeShape.Reference) return null;
        if (!fieldEnum.IsNil)
        {
            if (resolve(source, fieldEnum, true) is not { } expected || shape(expected.Module, expected.Type) != TypeShape.Enum)
                return null;
            FieldDefinitionHandle field = default;
            bool sawName = false;
            foreach (var candidate in type.GetFields())
            {
                var definition = targetReader.GetFieldDefinition(candidate);
                if (!targetReader.StringComparer.Equals(definition.Name, name)) continue;
                // Ordinary generated fields in one declaring type share a C++ name if their metadata names match.
                if (sawName)
                    throw new NotSupportedException($"UnsafeAccessor field {typeName}.{name} has multiple declarations with the same name");
                sawName = true;
                var sig = targetReader.GetBlobReader(definition.Signature);
                if (sig.ReadByte() != (byte)SignatureKind.Field) continue;
                byte fieldType = sig.ReadByte();
                if (fieldType == (byte)SignatureTypeCode.RequiredModifier && IsCoreType(owner.Module, sig.ReadTypeHandle(), "IsVolatile"))
                    fieldType = sig.ReadByte();
                if (fieldType != (byte)SignatureTypeKind.ValueType
                    || resolve(owner.Module, sig.ReadTypeHandle(), true) is not { } actual
                    || actual.Module != expected.Module || actual.Type != expected.Type || sig.RemainingBytes != 0) continue;
                if ((definition.Attributes & (FieldAttributes.Static | FieldAttributes.Literal | FieldAttributes.HasFieldRVA)) != 0)
                    return null;
                field = candidate;
            }
            return field.IsNil ? null : (owner.Module, field);
        }
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
                return typeName == "System." + name
                    && (assembly.Equals("System.Private.CoreLib", StringComparison.OrdinalIgnoreCase)
                        || forwardsTo(assembly, "System", name, "System.Private.CoreLib"));
            }
            if (kind != (byte)SignatureTypeKind.Class) return false;
            var declared = sig.ReadTypeHandle();
            if (resolve(owner.Module, declared, false) is not { } returned) return false;
            var rr = read(returned.Module);
            var type = rr.GetTypeDefinition(returned.Type);
            if (type.GetGenericParameters().Count != 0
                || RawSignatureProvider.TypeDefinitionName(rr, returned.Type) != typeName
                || shape(returned.Module, returned.Type) != TypeShape.Reference) return false;
            string definition = rr.GetString(rr.GetAssemblyDefinition().Name);
            if (assembly.Equals(definition, StringComparison.OrdinalIgnoreCase)) return true;

            // The getter's own scoped TypeRef can prove the identity even with an omitted facade.
            var scope = declared;
            while (scope.Kind == HandleKind.TypeReference)
                scope = targetReader.GetTypeReference((TypeReferenceHandle)scope).ResolutionScope;
            if (scope.Kind == HandleKind.AssemblyReference && targetReader.GetString(
                targetReader.GetAssemblyReference((AssemblyReferenceHandle)scope).Name).Equals(assembly, StringComparison.OrdinalIgnoreCase))
                return true;
            while (!type.GetDeclaringType().IsNil) type = rr.GetTypeDefinition(type.GetDeclaringType());
            return forwardsTo(assembly, rr.GetString(type.Namespace), rr.GetString(type.Name), definition);
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
            if (!reader.StringComparer.Equals(ctorName, ".ctor") || !IsCoreType(source, parent, expected)) return false;
            var sig = reader.GetBlobReader(ctorSignature);
            if (sig.ReadByte() != 0x20 || sig.ReadCompressedInteger() != 1 || sig.ReadByte() != (byte)SignatureTypeCode.Void)
                return false;
            return (expected == "UnsafeAccessorTypeAttribute"
                ? sig.ReadByte() == (byte)SignatureTypeCode.String
                : sig.ReadByte() == (byte)SignatureTypeKind.ValueType && IsCoreType(source, sig.ReadTypeHandle(), "UnsafeAccessorKind"))
                && sig.RemainingBytes == 0;
        }

        bool IsCoreType(TModule module, EntityHandle token, string expected)
        {
            var reader = read(module);
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
            if (resolve(module, token, !fieldEnum.IsNil) is not { } resolved) return false;
            var definitionReader = read(resolved.Module);
            return definitionReader.StringComparer.Equals(definitionReader.GetAssemblyDefinition().Name, "System.Private.CoreLib")
                && definitionReader.GetTypeDefinition(resolved.Type).GetDeclaringType().IsNil;
        }
    }
}

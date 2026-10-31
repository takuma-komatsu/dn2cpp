using System.Reflection;
using System.Reflection.Metadata;
using SRME = System.Reflection.Metadata.Ecma335.MetadataTokens;

namespace Dn2Cpp;

internal sealed partial class Compilation
{
    private readonly Dictionary<(ClassInfo Owner, MethodDefinitionHandle Definition), MethodInfo> _vectorValueMethods = new();

    private MethodInfo VectorValueShell(ClassInfo owner, MethodDefinitionHandle handle)
    {
        if (CoreIntrinsics.IsVectorManagedValueClass(owner))
        {
            EnsureCompleted(owner);
            foreach (var declared in owner.Methods)
                if (declared.Module == owner.Module && declared.Handle == handle) return declared;
            throw VectorValueUnsupported();
        }
        var key = (owner, handle);
        if (_vectorValueMethods.TryGetValue(key, out var existing)) return existing;
        var definition = owner.Module.Reader.GetMethodDefinition(handle);
        var method = new MethodInfo
        {
            DeclaringClass = owner, Name = owner.Module.Reader.GetString(definition.Name), Handle = handle,
            Module = owner.Module, Context = owner.Context, Attributes = definition.Attributes,
            ImplAttributes = definition.ImplAttributes, Rva = definition.RelativeVirtualAddress,
            PInvoke = ReadPInvoke(owner.Module, definition),
        };
        _vectorValueMethods.Add(key, method);
        return method;
    }

    private MethodInfo FindVectorValueMethod(ClassInfo owner, string name, bool typed)
    {
        if (!CoreIntrinsics.IsVectorValueCallableClass(owner)) throw VectorValueUnsupported();
        var reader = owner.Module.Reader;
        foreach (var handle in reader.GetTypeDefinition(owner.Handle).GetMethods())
        {
            if (!reader.StringComparer.Equals(reader.GetMethodDefinition(handle).Name, name)) continue;
            var method = VectorValueShell(owner, handle);
            if (CoreIntrinsics.IsVectorValueCallableMethod(method)
                && (name != "Equals" || (method.Signature.ParameterTypes[0].Class == owner) == typed))
                return method;
        }
        throw VectorValueUnsupported();
    }

    internal MethodInfo ResolveVectorManagedCountReference(Module module, MemberReferenceHandle handle, GenericContext context)
    {
        var reader = module.Reader;
        var reference = reader.GetMemberReference(handle);
        if (!reader.StringComparer.Equals(reference.Name, "get_Count")
            || reference.Parent.Kind != HandleKind.TypeSpecification
            || ResolveTypeTokenForScan(module, reference.Parent, context) is not { Class: { } owner })
            throw VectorValueUnsupported();
        EnsureShape(owner);
        if (!CoreIntrinsics.IsVectorManagedValueClass(owner)) throw VectorValueUnsupported();
        var parent = reader.GetBlobReader(reader.GetTypeSpecification((TypeSpecificationHandle)reference.Parent).Signature);
        if (!ReadVectorValueType(module, ref parent, context, owner) || parent.RemainingBytes != 0
            || !CoreIntrinsics.IsVectorManagedCountSignature(reader.GetBlobReader(reference.Signature)))
            throw VectorValueUnsupported();
        foreach (var declaration in owner.Module.Reader.GetTypeDefinition(owner.Handle).GetMethods())
        {
            if (!owner.Module.Reader.StringComparer.Equals(owner.Module.Reader.GetMethodDefinition(declaration).Name, "get_Count")) continue;
            var method = VectorValueShell(owner, declaration);
            if (CoreIntrinsics.IsVectorManagedCount(method)) return method;
        }
        throw VectorValueUnsupported();
    }

    internal MethodInfo ResolveVectorValueReference(Module module, MemberReferenceHandle handle, GenericContext context)
    {
        var reader = module.Reader;
        var reference = reader.GetMemberReference(handle);
        string name = reader.GetString(reference.Name);
        if (reference.Parent.Kind != HandleKind.TypeSpecification
            || ResolveTypeTokenForScan(module, reference.Parent, context) is not { Class: { } owner })
            throw VectorValueUnsupported();
        EnsureShape(owner);
        if (!CoreIntrinsics.IsVectorValueCallableClass(owner)) throw VectorValueUnsupported();
        var parent = reader.GetBlobReader(reader.GetTypeSpecification((TypeSpecificationHandle)reference.Parent).Signature);
        if (!ReadVectorValueType(module, ref parent, context, owner) || parent.RemainingBytes != 0)
            throw VectorValueUnsupported();
        var referenceContext = new GenericContext(owner.Context.TypeArgs, context.MethodArgs);
        var signature = reference.DecodeMethodSignature(SigProvider, referenceContext);
        var blob = reader.GetBlobReader(reference.Signature);
        if (!CoreIntrinsics.IsVectorValueSignature(signature, name, owner)
            || !ReadVectorValueReference(module, ref blob, referenceContext, name, owner, out bool typed))
            throw VectorValueUnsupported();
        return FindVectorValueMethod(owner, name, typed);
    }

    internal void ValidateVectorValueReceiver(Module module, EntityHandle handle, GenericContext context, ClassInfo receiver)
    {
        EnsureShape(receiver);
        if (!CoreIntrinsics.IsVectorValueCallableClass(receiver) || handle.Kind != HandleKind.TypeSpecification)
            throw VectorValueUnsupported();
        var blob = module.Reader.GetBlobReader(module.Reader.GetTypeSpecification((TypeSpecificationHandle)handle).Signature);
        if (!ReadVectorValueType(module, ref blob, context, receiver) || blob.RemainingBytes != 0)
            throw VectorValueUnsupported();
    }

    internal MethodInfo ValidateVectorValueConstrained(Module module, EntityHandle handle,
        GenericContext context, ClassInfo receiver)
    {
        if (!CoreIntrinsics.IsVectorValueCallableClass(receiver)) throw VectorValueUnsupported();
        if (handle.Kind == HandleKind.MethodSpecification) throw VectorValueUnsupported();
        var reader = module.Reader;
        string name;
        EntityHandle parent;
        BlobReader blob;
        if (handle.Kind == HandleKind.MemberReference)
        {
            var reference = reader.GetMemberReference((MemberReferenceHandle)handle);
            name = reader.GetString(reference.Name);
            parent = reference.Parent;
            blob = reader.GetBlobReader(reference.Signature);
        }
        else if (handle.Kind == HandleKind.MethodDefinition)
        {
            var definition = reader.GetMethodDefinition((MethodDefinitionHandle)handle);
            name = reader.GetString(definition.Name);
            parent = definition.GetDeclaringType();
            blob = reader.GetBlobReader(definition.Signature);
        }
        else throw VectorValueUnsupported();
        if (name is not ("Equals" or "GetHashCode")) throw VectorValueUnsupported();
        var identity = TypeDefinitionOf(module, parent);
        if (identity is null || identity.Value.Module != receiver.Module) throw VectorValueUnsupported();
        if (identity.Value.Handle == receiver.Handle)
        {
            if (handle.Kind == HandleKind.MemberReference)
            {
                var referenced = ResolveVectorValueReference(module, (MemberReferenceHandle)handle, context);
                if (referenced.DeclaringClass != receiver) throw VectorValueUnsupported();
                return referenced;
            }
            var method = VectorValueShell(receiver, (MethodDefinitionHandle)handle);
            if (!CoreIntrinsics.IsVectorValueCallableMethod(method)) throw VectorValueUnsupported();
            return method;
        }
        var ownerReader = identity.Value.Module.Reader;
        var ownerDefinition = ownerReader.GetTypeDefinition(identity.Value.Handle);
        if (!ownerDefinition.GetDeclaringType().IsNil
            || !ownerReader.StringComparer.Equals(ownerDefinition.Namespace, "System"))
            throw VectorValueUnsupported();
        if (ownerReader.StringComparer.Equals(ownerDefinition.Name, "IEquatable`1"))
        {
            if (name != "Equals" || parent.Kind != HandleKind.TypeSpecification
                || ResolveTypeTokenForScan(module, parent, context) is not { Class: { } itf })
                throw VectorValueUnsupported();
            EnsureShape(itf);
            if (!CoreIntrinsics.IsVectorValueCallableEquatable(itf, receiver)) throw VectorValueUnsupported();
            var ownerBlob = reader.GetBlobReader(reader.GetTypeSpecification((TypeSpecificationHandle)parent).Signature);
            var referenceContext = new GenericContext(itf.Context.TypeArgs, context.MethodArgs);
            if (!ReadVectorValueInstantiation(module, ref ownerBlob, itf, 0x12)
                || !ReadVectorValueType(module, ref ownerBlob, context, receiver) || ownerBlob.RemainingBytes != 0
                || !ReadVectorValueReference(module, ref blob, referenceContext, name, receiver, out bool typed) || !typed)
                throw VectorValueUnsupported();
            foreach (var declaration in ownerDefinition.GetMethods())
            {
                if (!ownerReader.StringComparer.Equals(ownerReader.GetMethodDefinition(declaration).Name, "Equals")) continue;
                if (CoreIntrinsics.IsVectorValueCallableEquatableMethod(VectorValueShell(itf, declaration)))
                    return FindVectorValueMethod(receiver, "Equals", true);
            }
            throw VectorValueUnsupported();
        }
        if (parent.Kind is not (HandleKind.TypeDefinition or HandleKind.TypeReference)
            || ownerDefinition.GetGenericParameters().Count != 0
            || !(ownerReader.StringComparer.Equals(ownerDefinition.Name, "Object")
                || ownerReader.StringComparer.Equals(ownerDefinition.Name, "ValueType"))
            || !ReadVectorValueReference(module, ref blob, GenericContext.Empty, name, receiver, out bool objectTyped)
            || objectTyped)
            throw VectorValueUnsupported();
        // The target is a real Object/ValueType virtual, not just a same-name
        // interface slot or a permissively resolved overload.
        foreach (var declaration in ownerDefinition.GetMethods())
        {
            if (handle.Kind == HandleKind.MethodDefinition && declaration != (MethodDefinitionHandle)handle) continue;
            var definition = ownerReader.GetMethodDefinition(declaration);
            if (!ownerReader.StringComparer.Equals(definition.Name, name) || definition.GetGenericParameters().Count != 0
                || (definition.Attributes & (MethodAttributes.MemberAccessMask | MethodAttributes.Static | MethodAttributes.Virtual))
                    != (MethodAttributes.Public | MethodAttributes.Virtual)) continue;
            var declared = ownerReader.GetBlobReader(definition.Signature);
            if (ReadVectorValueReference(identity.Value.Module, ref declared, GenericContext.Empty, name, receiver, out bool declarationTyped)
                && !declarationTyped)
                return FindVectorValueMethod(receiver, name, false);
        }
        throw VectorValueUnsupported();
    }

    internal void RejectVectorValueMethodSpecification(Module module, MethodSpecificationHandle handle, GenericContext context)
    {
        var specification = module.Reader.GetMethodSpecification(handle);
        if (MethodSpecMethodName(module, specification) is not ("Equals" or "GetHashCode" or "get_Count")) return;
        var parent = specification.Method.Kind switch
        {
            HandleKind.MethodDefinition => module.Reader.GetMethodDefinition((MethodDefinitionHandle)specification.Method).GetDeclaringType(),
            HandleKind.MemberReference => module.Reader.GetMemberReference((MemberReferenceHandle)specification.Method).Parent,
            _ => default(EntityHandle),
        };
        if (VectorValueNamedOwner(module, parent, includeFixed: true)) throw VectorValueUnsupported();
        if (parent.Kind == HandleKind.TypeSpecification
            && ResolveTypeTokenForScan(module, parent, context) is { Kind: TypeKind.Class, Class: { } resolvedOwner }
            && CoreIntrinsics.IsVectorValueCandidate(resolvedOwner, MethodSpecMethodName(module, specification)!))
            throw VectorValueUnsupported();
        if (MethodSpecMethodName(module, specification) == "Equals"
            && parent.Kind == HandleKind.TypeSpecification
            && ResolveTypeTokenForScan(module, parent, context) is { Kind: TypeKind.Class, Class: { } itf }
            && itf.Context.TypeArgs is [{ Kind: TypeKind.Class, Class: { } vector }]
            && (CoreIntrinsics.IsVectorExplicitOwner(vector) || CoreIntrinsics.IsFixedVectorValueOwner(vector)))
        {
            EnsureShape(vector);
            EnsureShape(itf);
            if (CoreIntrinsics.IsVectorValueCallableEquatable(itf, vector)
                || CoreIntrinsics.IsVectorManagedEquatableCandidate(itf, vector)
                || CoreIntrinsics.IsFixedVectorValueEquatableCandidate(itf, vector))
                throw VectorValueUnsupported();
        }
    }

    internal static bool VectorValueNamedOwner(Module module, EntityHandle handle, bool includeFacade = false, bool includeFixed = false)
    {
        var reader = module.Reader;
        if (handle.Kind == HandleKind.TypeSpecification)
        {
            var blob = reader.GetBlobReader(reader.GetTypeSpecification((TypeSpecificationHandle)handle).Signature);
            // Candidate discovery may peel modifiers; admission never does.
            // Otherwise an invalid owner could evade the MethodSpec prohibition.
            while (blob.RemainingBytes > 0)
            {
                int code = blob.ReadByte();
                if (code is 0x1f or 0x20)
                {
                    if (!blob.TryReadCompressedInteger(out _)) return false;
                    continue;
                }
                if (code == 0x45) continue;
                if (code != 0x15 || blob.RemainingBytes == 0 || blob.ReadByte() is not (0x11 or 0x12)
                    || !blob.TryReadCompressedInteger(out int token) || token >> 2 == 0 || (token & 3) > 1)
                    return false;
                handle = (token & 3) == 0 ? SRME.TypeDefinitionHandle(token >> 2)
                    : SRME.TypeReferenceHandle(token >> 2);
                break;
            }
        }
        if (handle.Kind == HandleKind.TypeDefinition)
        {
            var definition = reader.GetTypeDefinition((TypeDefinitionHandle)handle);
            return includeFixed && reader.StringComparer.Equals(definition.Namespace, "System.Runtime.Intrinsics")
                && (reader.StringComparer.Equals(definition.Name, "Vector64`1")
                    || reader.StringComparer.Equals(definition.Name, "Vector128`1")
                    || reader.StringComparer.Equals(definition.Name, "Vector256`1")
                    || reader.StringComparer.Equals(definition.Name, "Vector512`1"))
                || reader.StringComparer.Equals(definition.Namespace, "System.Numerics")
                && (reader.StringComparer.Equals(definition.Name, "Vector`1")
                    || includeFacade && reader.StringComparer.Equals(definition.Name, "Vector"));
        }
        if (handle.Kind == HandleKind.TypeReference)
        {
            var reference = reader.GetTypeReference((TypeReferenceHandle)handle);
            return includeFixed && reader.StringComparer.Equals(reference.Namespace, "System.Runtime.Intrinsics")
                && (reader.StringComparer.Equals(reference.Name, "Vector64`1")
                    || reader.StringComparer.Equals(reference.Name, "Vector128`1")
                    || reader.StringComparer.Equals(reference.Name, "Vector256`1")
                    || reader.StringComparer.Equals(reference.Name, "Vector512`1"))
                || reader.StringComparer.Equals(reference.Namespace, "System.Numerics")
                && (reader.StringComparer.Equals(reference.Name, "Vector`1")
                    || includeFacade && reader.StringComparer.Equals(reference.Name, "Vector"));
        }
        return false;
    }

    private bool ReadVectorValueReference(Module module, ref BlobReader blob, GenericContext context,
        string name, ClassInfo receiver, out bool typed)
    {
        typed = false;
        if (!CoreIntrinsics.VectorValueHeader(ref blob, name)) return false;
        if (name == "GetHashCode") return blob.RemainingBytes == 0;
        if (blob.RemainingBytes == 1)
            return blob.ReadByte() == 0x1c;
        typed = true;
        return ReadVectorValueType(module, ref blob, context, receiver) && blob.RemainingBytes == 0;
    }

    private bool ReadVectorValueType(Module module, ref BlobReader blob, GenericContext context, ClassInfo expected)
    {
        if (blob.RemainingBytes == 0) return false;
        var probe = blob;
        int code = probe.ReadByte();
        if (code is 0x13 or 0x1e)
        {
            if (!probe.TryReadCompressedInteger(out int index)) return false;
            var args = code == 0x13 ? context.TypeArgs : context.MethodArgs;
            if (index < 0 || index >= args.Length || args[index] is not { Kind: TypeKind.Class, IsCanonPlaceholder: false, Class: { } cls }
                || cls != expected) return false;
            blob = probe;
            return true;
        }
        if (!ReadVectorValueInstantiation(module, ref blob, expected, 0x11) || blob.RemainingBytes == 0) return false;
        return ReadVectorValueElement(module, ref blob, context, expected.Context.TypeArgs[0]);
    }

    private bool ReadVectorValueElement(Module module, ref BlobReader blob, GenericContext context, TypeDesc element)
    {
        if (blob.RemainingBytes == 0) return false;
        int code = blob.ReadByte();
        if (code is 0x13 or 0x1e)
        {
            if (!blob.TryReadCompressedInteger(out int index)) return false;
            var args = code == 0x13 ? context.TypeArgs : context.MethodArgs;
            return index >= 0 && index < args.Length
                && (CoreIntrinsics.IsVectorNumericElement(args[index]) || CoreIntrinsics.IsVectorManagedValueElement(args[index]))
                && SameTypeArg(args[index], element);
        }
        if (CoreIntrinsics.IsVectorNumericElement(element)) return code == (int)element.Primitive;
        if (!CoreIntrinsics.IsVectorManagedValueElement(element) || code != 0x11
            || !blob.TryReadCompressedInteger(out int token) || token >> 2 == 0 || (token & 3) > 1)
            return false;
        EntityHandle handle = (token & 3) == 0
            ? SRME.TypeDefinitionHandle(token >> 2) : SRME.TypeReferenceHandle(token >> 2);
        var identity = TypeDefinitionOf(module, handle);
        return identity is { } target && target.Module == element.Class!.Module && target.Handle == element.Class.Handle;
    }

    private bool ReadVectorValueInstantiation(Module module, ref BlobReader blob, ClassInfo expected, int kind)
    {
        if (blob.RemainingBytes < 4 || blob.ReadByte() != 0x15 || blob.ReadByte() != kind
            || !blob.TryReadCompressedInteger(out int token) || token <= 0 || token >> 2 == 0 || (token & 3) > 1)
            return false;
        EntityHandle handle = (token & 3) == 0
            ? SRME.TypeDefinitionHandle(token >> 2) : SRME.TypeReferenceHandle(token >> 2);
        var identity = TypeDefinitionOf(module, handle);
        return identity is { } target && target.Module == expected.Module && target.Handle == expected.Handle
            && blob.RemainingBytes > 0 && blob.ReadByte() == 1;
    }

    private static NotSupportedException VectorValueUnsupported() =>
        new("Vector<T> value methods require an exact supported closed CoreLib declaration and unmodified call signature");
}

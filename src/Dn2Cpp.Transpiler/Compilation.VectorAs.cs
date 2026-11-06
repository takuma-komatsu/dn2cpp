using System.Reflection.Metadata;
using SRME = System.Reflection.Metadata.Ecma335.MetadataTokens;

namespace Dn2Cpp;

internal sealed partial class Compilation
{
    internal void RejectBareVectorAsReference(Module module, MemberReferenceHandle handle, GenericContext context)
    {
        var reference = module.Reader.GetMemberReference(handle);
        string name = module.Reader.GetString(reference.Name);
        if (name != "As" && CoreIntrinsics.VectorAliasElementCode(name) == 0) return;
        if (MemberRefParentTypeName(module, handle) == "System.Numerics.Vector")
            throw GuardedVectorAsUnsupported();
        if (reference.Parent.Kind != HandleKind.TypeSpecification) return;
        var type = ResolveTypeTokenForScan(module, reference.Parent, context);
        while (type?.Element is { } element) type = element;
        if (type?.Class?.FullName == "System.Numerics.Vector") throw GuardedVectorAsUnsupported();
    }

    internal MethodInfo? TryResolveGuardedVectorAsSpec(Module module, MethodSpecificationHandle handle, GenericContext context) =>
        ResolveVectorAsSpecification(module, handle, context, allowCanonicalForScan: false);

    private MethodInfo? TryResolveGuardedVectorAsSpecForScan(Module module, MethodSpecificationHandle handle, GenericContext context) =>
        ResolveVectorAsSpecification(module, handle, context, allowCanonicalForScan: true);

    private MethodInfo? ResolveVectorAsSpecification(Module module, MethodSpecificationHandle handle, GenericContext context,
        bool allowCanonicalForScan)
    {
        var reader = module.Reader;
        var specification = reader.GetMethodSpecification(handle);
        string name = MethodSpecMethodName(module, specification) ?? "";
        int arity = name == "As" ? 2 : CoreIntrinsics.VectorAliasElementCode(name) != 0 ? 1 : 0;
        if (arity == 0) return null;
        var parent = specification.Method.Kind switch
        {
            HandleKind.MethodDefinition => reader.GetMethodDefinition((MethodDefinitionHandle)specification.Method).GetDeclaringType(),
            HandleKind.MemberReference => reader.GetMemberReference((MemberReferenceHandle)specification.Method).Parent,
            _ => default(EntityHandle),
        };
        if (parent.IsNil) return null;
        if (parent.Kind != HandleKind.TypeSpecification && MethodSpecParentTypeName(module, specification) != "System.Numerics.Vector")
            return null;
        var type = ResolveTypeTokenForScan(module, parent, context);
        while (type?.Element is { } element) type = element;
        if (type?.Class is not { } owner || owner.FullName != "System.Numerics.Vector") return null;
        if (parent.Kind is not (HandleKind.TypeDefinition or HandleKind.TypeReference) || !CoreIntrinsics.IsVectorEstimateOwner(owner))
            throw GuardedVectorAsUnsupported();

        MethodDefinitionHandle definitionHandle = default;
        if (specification.Method.Kind == HandleKind.MethodDefinition)
        {
            definitionHandle = (MethodDefinitionHandle)specification.Method;
            if (!CoreIntrinsics.IsVectorAsFamilyDefinition(owner, module, definitionHandle)) throw GuardedVectorAsUnsupported();
        }
        else
        {
            var reference = reader.GetMemberReference((MemberReferenceHandle)specification.Method);
            if (!CoreIntrinsics.ReadVectorAsFamilySignature(reader.GetBlobReader(reference.Signature), name, out int token))
                throw GuardedVectorAsUnsupported();
            EntityHandle vectorHandle = (token & 3) == 0
                ? SRME.TypeDefinitionHandle(token >> 2) : SRME.TypeReferenceHandle(token >> 2);
            var identity = TypeDefinitionOf(module, vectorHandle);
            foreach (var candidate in owner.Module.Reader.GetTypeDefinition(owner.Handle).GetMethods())
            {
                if (!CoreIntrinsics.IsVectorAsFamilyDefinition(owner, owner.Module, candidate)
                    || !owner.Module.Reader.StringComparer.Equals(owner.Module.Reader.GetMethodDefinition(candidate).Name, name)) continue;
                var definition = owner.Module.Reader.GetMethodDefinition(candidate);
                if (!CoreIntrinsics.ReadVectorAsFamilySignature(owner.Module.Reader.GetBlobReader(definition.Signature), name, out int original)
                    || identity is not { } target || target.Module != owner.Module
                    || target.Handle != SRME.TypeDefinitionHandle(original >> 2)) continue;
                if (!definitionHandle.IsNil) throw GuardedVectorAsUnsupported();
                definitionHandle = candidate;
            }
            if (definitionHandle.IsNil) throw GuardedVectorAsUnsupported();
        }
        var raw = reader.GetBlobReader(specification.Signature);
        if (raw.RemainingBytes < 2 + arity || raw.ReadByte() != 0x0a || raw.ReadByte() != arity) throw GuardedVectorAsUnsupported();
        var shape = raw;
        for (int index = 0; index < arity; index++)
        {
            if (!ReadVectorAsArgumentShape(ref shape, context)) throw GuardedVectorAsUnsupported();
        }
        if (shape.RemainingBytes != 0) throw GuardedVectorAsUnsupported();
        var arguments = specification.DecodeSignature(SigProvider, context);
        if (arguments.Length != arity) throw GuardedVectorAsUnsupported();
        bool deferred = false;
        foreach (var argument in arguments)
        {
            if (argument.IsCanonPlaceholder)
            {
                if (!allowCanonicalForScan || argument.Kind != TypeKind.Primitive || argument.CanonAnyIndex != -1
                    || !CoreIntrinsics.IsVectorNumericElement(TypeDesc.MakePrimitive(argument.Primitive))
                    || !ReadVectorAsCanonicalArgument(ref raw, context, argument))
                    throw GuardedVectorAsUnsupported();
                deferred = true;
                continue;
            }
            if (argument.Class is { } value) EnsureShape(value);
            if (!CoreIntrinsics.IsVectorAsElement(argument)
                || !ReadVectorValueElement(module, ref raw, context, argument)) throw GuardedVectorAsUnsupported();
        }
        if (raw.RemainingBytes != 0) throw GuardedVectorAsUnsupported();
        // Canonical scans cannot choose a vector representation. The existing
        // call/address emission taint forces the concrete body before use.
        if (deferred) return null;
        if (CoreIntrinsics.IsVectorNumericElement(arguments[0])
            && (arity == 1 || CoreIntrinsics.IsVectorNumericElement(arguments[1])))
            return null;
        return InstantiateMethodOnClass(owner, owner.Module, definitionHandle, arguments.ToArray());
    }

    private static bool ReadVectorAsCanonicalArgument(ref BlobReader raw, GenericContext context, TypeDesc argument)
    {
        if (raw.RemainingBytes < 2) return false;
        int kind = raw.ReadByte();
        if (kind is not (0x13 or 0x1e) || !raw.TryReadCompressedInteger(out int index)) return false;
        var arguments = kind == 0x13 ? context.TypeArgs : context.MethodArgs;
        return index >= 0 && index < arguments.Length && SameTypeArg(arguments[index], argument);
    }

    private static bool ReadVectorAsArgumentShape(ref BlobReader blob, GenericContext context)
    {
        if (blob.RemainingBytes == 0) return false;
        int code = blob.ReadByte();
        if (code is 0x04 or 0x05 or 0x06 or 0x07 or 0x08 or 0x09 or 0x0a or 0x0b or 0x0c or 0x0d or 0x18 or 0x19)
            return true;
        if (code is not (0x11 or 0x13 or 0x1e) || !blob.TryReadCompressedInteger(out int argument)) return false;
        return code == 0x11 ? argument >> 2 != 0 && (argument & 3) <= 1
            : code == 0x13 ? argument >= 0 && argument < context.TypeArgs.Length
            : argument >= 0 && argument < context.MethodArgs.Length;
    }

    private void ValidateGuardedVectorAsInstantiation(ClassInfo owner, Module module, MethodDefinitionHandle handle, TypeDesc[] arguments)
    {
        var definition = module.Reader.GetMethodDefinition(handle);
        string name = module.Reader.GetString(definition.Name);
        int arity = name == "As" ? 2 : CoreIntrinsics.VectorAliasElementCode(name) != 0 ? 1 : 0;
        if (arity == 0 || owner.FullName != "System.Numerics.Vector") return;
        if (arguments.Length != arity || !CoreIntrinsics.IsVectorAsFamilyDefinition(owner, module, handle)) throw GuardedVectorAsUnsupported();
        foreach (var argument in arguments)
        {
            if (argument.Class is { } value) EnsureShape(value);
            if (!CoreIntrinsics.IsVectorAsElement(argument)) throw GuardedVectorAsUnsupported();
        }
        var candidate = new MethodInfo
        {
            DeclaringClass = owner, Module = module, Handle = handle, Name = name,
            Context = new GenericContext(owner.Context.TypeArgs, arguments), Attributes = definition.Attributes,
            ImplAttributes = definition.ImplAttributes, Rva = definition.RelativeVirtualAddress, PInvoke = ReadPInvoke(module, definition),
        };
        if (candidate.Signature.ReturnType.Class is { } result) EnsureShape(result);
        foreach (var parameter in candidate.Signature.ParameterTypes)
            if (parameter.Class is { } operand) EnsureShape(operand);
        if (!CoreIntrinsics.IsVectorAsFamilyCallable(candidate)) throw GuardedVectorAsUnsupported();
        if (CoreIntrinsics.IsGuardedVectorAs(candidate)) ResolveGuardedVectorAsGuards(candidate);
    }

    private static NotSupportedException GuardedVectorAsUnsupported() =>
        new("Vector reinterpretation requires an exact closed CoreLib declaration and unmodified MethodSpec");
}

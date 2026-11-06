using System.Reflection.Metadata;

namespace Dn2Cpp;

internal sealed partial class Compilation
{
    internal bool IsKnownConstantSpec(Module module, MethodSpecification specification)
    {
        var reader = module.Reader;
        bool named = specification.Method.Kind switch
        {
            HandleKind.MethodDefinition => reader.StringComparer.Equals(
                reader.GetMethodDefinition((MethodDefinitionHandle)specification.Method).Name, "IsKnownConstant"),
            HandleKind.MemberReference => reader.StringComparer.Equals(
                reader.GetMemberReference((MemberReferenceHandle)specification.Method).Name, "IsKnownConstant"),
            _ => false,
        };
        return named && MethodSpecParentTypeName(module, specification) == "System.Runtime.CompilerServices.RuntimeHelpers";
    }

    private void ValidateKnownConstantReference(Module module, MemberReference reference,
        MethodInfo target, GenericContext context)
    {
        if (!CoreIntrinsics.IsKnownConstant(target)
            || !KnownConstantTypeReference(module, reference.Parent, target.Module, "System.Runtime.CompilerServices", "RuntimeHelpers")
            || !CoreIntrinsics.IsKnownConstantSignature(module.Reader.GetBlobReader(reference.Signature),
                out byte parameter, out var type))
            throw new NotSupportedException("RuntimeHelpers.IsKnownConstant requires its exact CoreLib declaration and MemberRef signature");
        var definition = target.Module.Reader.GetMethodDefinition(target.Handle);
        if (!CoreIntrinsics.IsKnownConstantSignature(target.Module.Reader.GetBlobReader(definition.Signature),
                out byte declaredParameter, out _) || parameter != declaredParameter)
            throw new NotSupportedException("RuntimeHelpers.IsKnownConstant MemberRef must match its declaration");
        if (parameter == 0x12)
        {
            var actual = type.Kind == HandleKind.TypeReference
                ? ResolveTypeRef(module, (TypeReferenceHandle)type)
                : GetTypeDescForDefinition(module, (TypeDefinitionHandle)type);
            if (!KnownConstantTypeReference(module, type, target.Module, "System", "Type")
                || actual is not { Kind: TypeKind.Class, Class: { FullName: "System.Type" } cls }
                || cls.Module != target.Module)
                throw new NotSupportedException("RuntimeHelpers.IsKnownConstant requires the CoreLib System.Type identity");
        }
    }

    private static bool KnownConstantTypeReference(Module module, EntityHandle handle, Module core,
        string ns, string name)
    {
        var reader = module.Reader;
        if (handle.Kind == HandleKind.TypeDefinition)
        {
            var definition = reader.GetTypeDefinition((TypeDefinitionHandle)handle);
            return module == core && definition.GetDeclaringType().IsNil
                && reader.StringComparer.Equals(definition.Namespace, ns)
                && reader.StringComparer.Equals(definition.Name, name);
        }
        if (handle.Kind != HandleKind.TypeReference) return false;
        var type = reader.GetTypeReference((TypeReferenceHandle)handle);
        if (!reader.StringComparer.Equals(type.Namespace, ns) || !reader.StringComparer.Equals(type.Name, name)
            || type.ResolutionScope.Kind != HandleKind.AssemblyReference) return false;
        var reference = reader.GetAssemblyReference((AssemblyReferenceHandle)type.ResolutionScope);
        var assembly = core.Reader.GetAssemblyDefinition();
        var token = reader.GetBlobReader(reference.PublicKeyOrToken);
        return reader.StringComparer.Equals(reference.Name, "System.Private.CoreLib")
            && reference.Version == assembly.Version && reader.GetString(reference.Culture) == core.Reader.GetString(assembly.Culture)
            && (reference.Flags & System.Reflection.AssemblyFlags.PublicKey) == 0
            && token.Length == 8 && token.ReadUInt64() == 0x8e79a7bed785ec7cUL;
    }

    private void ValidateKnownConstantSpec(Module module, MethodSpecification specification,
        MethodInfo target, GenericContext context)
    {
        // MethodSpec decoding can mint a value-type shell whose kind flags are
        // unset until the pending shape tier drains.
        CompletePendingSpecializations();
        if (!CoreIntrinsics.IsKnownConstant(target)
            || target.Module.Reader.GetMethodDefinition(target.Handle).GetGenericParameters().Count != 1
            || target.Context.MethodArgs is not [var argument] || !CoreIntrinsics.IsKnownConstantArgument(argument))
            throw new NotSupportedException("RuntimeHelpers.IsKnownConstant requires its exact struct-constrained declaration and nonnullable value argument");
        if (!CoreIntrinsics.IsKnownConstantInstantiation(module.Reader, specification.Signature))
            throw new NotSupportedException("RuntimeHelpers.IsKnownConstant requires one exact unmodified MethodSpec value argument");
        if (specification.Method.Kind == HandleKind.MemberReference)
            ValidateKnownConstantReference(module,
                module.Reader.GetMemberReference((MemberReferenceHandle)specification.Method), target, context);
    }
}

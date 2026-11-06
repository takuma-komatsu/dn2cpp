using System.Reflection.Metadata;

namespace Dn2Cpp;

internal sealed partial class Compilation
{
    internal void RejectVectorLogMethodSpecification(Module module, MethodSpecificationHandle handle, GenericContext context)
    {
        var specification = module.Reader.GetMethodSpecification(handle);
        string? operation = MethodSpecMethodName(module, specification);
        if (operation is not ("Log" or "Log2")) return;
        if (MethodSpecParentTypeName(module, specification) is { } name && CoreIntrinsics.IsVectorEstimateOwnerName(name))
            throw new NotSupportedException($"Vector {operation} requires a nongeneric floating-point declaration");
        if (specification.Method.Kind == HandleKind.MemberReference)
        {
            var reference = module.Reader.GetMemberReference((MemberReferenceHandle)specification.Method);
            if (reference.Parent.Kind != HandleKind.TypeSpecification) return;
            var type = module.Reader.GetTypeSpecification((TypeSpecificationHandle)reference.Parent).DecodeSignature(SigProvider, context);
            while (type.Element is { } element) type = element;
            if (type.Class is { } owner && CoreIntrinsics.IsVectorEstimateOwnerName(owner.FullName))
                throw new NotSupportedException($"Vector {operation} requires an unmodified nongeneric declaration");
        }
    }

    private readonly Dictionary<(ClassInfo Owner, MethodDefinitionHandle Definition), MethodInfo> _vectorLogs = new();

    internal MethodInfo? TryResolveVectorLogReference(Module module, MemberReferenceHandle handle, GenericContext context)
    {
        var reader = module.Reader;
        var reference = reader.GetMemberReference(handle);
        string operation = reader.GetString(reference.Name);
        if (operation is not ("Log" or "Log2")) return null;
        if (reference.Parent.Kind == HandleKind.TypeSpecification)
        {
            var type = reader.GetTypeSpecification((TypeSpecificationHandle)reference.Parent).DecodeSignature(SigProvider, context);
            while (type.Element is { } element) type = element;
            if (type.Class is { } candidate && CoreIntrinsics.IsVectorEstimateOwnerName(candidate.FullName))
                throw new NotSupportedException($"Vector {operation} requires an unmodified declaring type");
            return null;
        }
        string? name = MemberRefParentTypeName(module, handle);
        if (name is null || !CoreIntrinsics.IsVectorEstimateOwnerName(name)) return null;
        // Validate the unmodified reference before decoding or reusing any cached declaration.
        if (!CoreIntrinsics.IsVectorLogRawSignature(reader.GetBlobReader(reference.Signature)))
            throw new NotSupportedException($"Vector {operation} requires its exact floating-point CoreLib reference");
        ClassInfo? owner = reference.Parent.Kind == HandleKind.TypeReference
            ? ResolveTypeRef(module, (TypeReferenceHandle)reference.Parent)?.Class
            : reference.Parent.Kind == HandleKind.TypeDefinition
                ? ResolveTypeDefMemberRefClass(module, (TypeDefinitionHandle)reference.Parent) : null;
        if (owner is null || !CoreIntrinsics.IsVectorEstimateOwner(owner))
            throw new NotSupportedException($"Vector {operation} requires its exact floating-point CoreLib reference");
        var signature = reference.DecodeMethodSignature(SigProvider, GenericContext.Empty);
        if (signature.ReturnType.Class is { } vector) EnsureShape(vector);
        if (!CoreIntrinsics.IsVectorLogSignature(signature, owner))
            throw new NotSupportedException($"Vector {operation} requires its exact floating-point CoreLib reference");
        foreach (var definitionHandle in owner.Module.Reader.GetTypeDefinition(owner.Handle).GetMethods())
        {
            var definition = owner.Module.Reader.GetMethodDefinition(definitionHandle);
            if (!owner.Module.Reader.StringComparer.Equals(definition.Name, operation)
                || definition.GetGenericParameters().Count != 0) continue;
            var key = (owner, definitionHandle);
            if (!_vectorLogs.TryGetValue(key, out var method))
            {
                method = new MethodInfo
                {
                    DeclaringClass = owner, Name = operation, Handle = definitionHandle, Module = owner.Module,
                    Context = owner.Context, Attributes = definition.Attributes, ImplAttributes = definition.ImplAttributes,
                    Rva = definition.RelativeVirtualAddress, PInvoke = ReadPInvoke(owner.Module, definition),
                };
                _vectorLogs.Add(key, method);
            }
            if (CoreIntrinsics.IsVectorLog(method)
                && SameTypeArg(signature.ReturnType, method.Signature.ReturnType)) return method;
        }
        throw new NotSupportedException($"Vector {operation} requires its exact floating-point CoreLib declaration");
    }
}

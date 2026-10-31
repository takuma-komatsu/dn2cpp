using System.Reflection.Metadata;

namespace Dn2Cpp;

internal sealed partial class Compilation
{
    private readonly Dictionary<(ClassInfo Owner, MethodDefinitionHandle Definition), MethodInfo> _concreteVectorElements = new();

    internal MethodInfo? TryResolveConcreteVectorElementReference(Module module, MemberReferenceHandle handle, GenericContext context)
    {
        var reader = module.Reader;
        var reference = reader.GetMemberReference(handle);
        string name = reader.GetString(reference.Name);
        if (!CoreIntrinsics.IsConcreteVectorElementName(name)) return null;
        ClassInfo? owner = null;
        if (reference.Parent.Kind == HandleKind.TypeSpecification)
        {
            var type = reader.GetTypeSpecification((TypeSpecificationHandle)reference.Parent).DecodeSignature(SigProvider, context);
            while (type.Element is { } element) type = element;
            if (type.Class?.FullName == "System.Numerics.Vector"
                && CoreIntrinsics.IsConcreteVectorElementReferenceCandidate(reference.DecodeMethodSignature(SigProvider, GenericContext.Empty)))
                throw new NotSupportedException("Concrete vector element access requires an unmodified declaring type");
            return null;
        }
        if (MemberRefParentTypeName(module, handle) != "System.Numerics.Vector") return null;
        // Only explicit concrete operands claim this family; a caller's generic
        // substitution must not turn Vector<T>'s helpers into concrete overloads.
        var signature = reference.DecodeMethodSignature(SigProvider, GenericContext.Empty);
        if (!CoreIntrinsics.IsConcreteVectorElementReferenceCandidate(signature)) return null;
        if (reference.Parent.Kind == HandleKind.TypeReference)
            owner = ResolveTypeRef(module, (TypeReferenceHandle)reference.Parent)?.Class;
        else if (reference.Parent.Kind == HandleKind.TypeDefinition)
            owner = ResolveTypeDefMemberRefClass(module, (TypeDefinitionHandle)reference.Parent);
        if (owner is null) throw new NotSupportedException("Concrete vector element access requires its exact CoreLib owner");
        return ResolveConcreteVectorElement(owner, name, signature, reader.GetBlobReader(reference.Signature));
    }

    private MethodInfo ResolveConcreteVectorElement(ClassInfo owner, string name, MethodSignature<TypeDesc> signature, BlobReader raw)
    {
        if (signature.ParameterTypes.Length != 0 && signature.ParameterTypes[0].Class is { } vector) EnsureShape(vector);
        if (!CoreIntrinsics.IsConcreteVectorElementOwner(owner)
            || !CoreIntrinsics.IsConcreteVectorElementRawSignature(name, raw)
            || !CoreIntrinsics.IsConcreteVectorElementSignature(name, signature, owner.Module))
            throw new NotSupportedException("Concrete vector element access requires its exact CoreLib reference");
        var reader = owner.Module.Reader;
        foreach (var handle in reader.GetTypeDefinition(owner.Handle).GetMethods())
        {
            var definition = reader.GetMethodDefinition(handle);
            if (!reader.StringComparer.Equals(definition.Name, name) || definition.GetGenericParameters().Count != 0) continue;
            var key = (owner, handle);
            if (!_concreteVectorElements.TryGetValue(key, out var method))
            {
                method = new MethodInfo
                {
                    DeclaringClass = owner, Name = name, Handle = handle, Module = owner.Module, Context = owner.Context,
                    Attributes = definition.Attributes, ImplAttributes = definition.ImplAttributes,
                    Rva = definition.RelativeVirtualAddress, PInvoke = ReadPInvoke(owner.Module, definition),
                };
                _concreteVectorElements.Add(key, method);
            }
            if (CoreIntrinsics.IsConcreteVectorElement(method)
                && SameTypeArg(signature.ParameterTypes[0], method.Signature.ParameterTypes[0])) return method;
        }
        throw new NotSupportedException("Concrete vector element access requires its exact CoreLib declaration");
    }
}

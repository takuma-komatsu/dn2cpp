using System.Reflection.Metadata;

namespace Dn2Cpp;

internal sealed partial class Compilation
{
    // Task<T> keeps its runtime-owned layout. Promise constructor and completion
    // shells decode no TPL fields, signatures, or unrelated member bodies.
    private void CompleteTaskPromiseMembers(ClassInfo owner)
    {
        var reader = owner.Module.Reader;
        foreach (var handle in reader.GetTypeDefinition(owner.Handle).GetMethods())
        {
            var definition = reader.GetMethodDefinition(handle);
            string name = reader.GetString(definition.Name);
            if (!CoreIntrinsics.IsTaskPromiseCandidate(name, reader.GetBlobReader(definition.Signature))) continue;
            var method = new MethodInfo
            {
                DeclaringClass = owner, Name = name, Handle = handle, Module = owner.Module,
                Context = owner.Context, Attributes = definition.Attributes,
                ImplAttributes = definition.ImplAttributes, Rva = definition.RelativeVirtualAddress,
                PInvoke = ReadPInvoke(owner.Module, definition),
            };
            owner.Methods.Add(method);
            owner.MethodByTemplate.Add(handle, method);
        }
    }

    internal MethodInfo? TryResolveTaskPromiseReference(Module module, MemberReferenceHandle handle, GenericContext context)
    {
        var reader = module.Reader;
        var reference = reader.GetMemberReference(handle);
        string name = reader.GetString(reference.Name);
        if (!CoreIntrinsics.IsTaskPromiseCandidate(name, reader.GetBlobReader(reference.Signature))) return null;
        ClassInfo? owner;
        if (reference.Parent.Kind == HandleKind.TypeSpecification)
        {
            var blob = reader.GetBlobReader(reader.GetTypeSpecification((TypeSpecificationHandle)reference.Parent).Signature);
            var type = reader.GetTypeSpecification((TypeSpecificationHandle)reference.Parent).DecodeSignature(SigProvider, context);
            owner = type.Class;
            if (owner is null || !CoreIntrinsics.IsTaskPromiseOwner(owner)) return null;
            if (blob.RemainingBytes < 2 || blob.ReadByte() != 0x15 || blob.ReadByte() != 0x12)
                throw new NotSupportedException("Task promise member requires an unmodified CoreLib owner");
        }
        else
        {
            string? full = MemberRefParentTypeName(module, handle);
            if (full is not ("System.Threading.Tasks.Task" or "System.Threading.Tasks.Task`1")) return null;
            owner = reference.Parent.Kind == HandleKind.TypeReference
                ? ResolveTypeRef(module, (TypeReferenceHandle)reference.Parent)?.Class
                : reference.Parent.Kind == HandleKind.TypeDefinition
                    ? ResolveTypeDefMemberRefClass(module, (TypeDefinitionHandle)reference.Parent) : null;
        }
        if (owner is null) throw new NotSupportedException("Task promise member requires its loaded CoreLib declaration");
        var method = ResolveMemberRefMethod(module, handle, context);
        if (!CoreIntrinsics.IsTaskPromise(method)
            || !CoreIntrinsics.IsTaskPromiseSignature(name, owner.GenericArity == 1, reader.GetBlobReader(reference.Signature))
            || !CoreIntrinsics.IsTaskPromiseDecodedSignature(method, reference.DecodeMethodSignature(SigProvider, owner.Context)))
            throw new NotSupportedException("Task promise member requires its exact CoreLib reference and declaration");
        return method;
    }
}

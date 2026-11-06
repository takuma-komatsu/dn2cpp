using System.Reflection.Metadata;

namespace Dn2Cpp;

internal sealed partial class Compilation
{
    private void ValidateUInt128CheckedSpec(Module module, MethodSpecification specification, MethodInfo method)
    {
        var owner = method.DeclaringClass;
        // Reject structurally invalid INumberBase owners before target extraction or
        // ordinary static-interface dispatch can turn them into a concrete factory.
        if (method.Name == "CreateChecked" && CoreIntrinsics.IsUInt128CheckedInterfaceDefinition(owner)
            && (owner.Module.Reader.GetTypeDefinition(owner.Handle).GetGenericParameters().Count != 1
                || owner.Context.TypeArgs.Length != 1))
            throw new NotSupportedException("INumberBase.CreateChecked requires exactly one owner type argument");
        bool interfaceForm = CoreIntrinsics.IsUInt128CheckedInterfaceOwner(owner);
        if (method.Name != "CreateChecked" || !CoreIntrinsics.IsUInt128CheckedType(owner) && !interfaceForm) return;
        var target = interfaceForm ? owner.Context.TypeArgs[0].Class! : owner;
        if (method.Context.MethodArgs is not [var source] || !CoreIntrinsics.IsUInt128CheckedSource(source, target)) return;
        var reader = module.Reader;
        if (!(interfaceForm ? CoreIntrinsics.IsUInt128CheckedInterface(method) : CoreIntrinsics.IsUInt128Checked(method))
            || !CoreIntrinsics.IsUInt128CheckedInstantiation(reader, specification.Signature))
            throw new NotSupportedException("UInt128.CreateChecked requires its exact closed integer MethodSpec signature");
        if (specification.Method.Kind != HandleKind.MemberReference) return;
        var reference = reader.GetMemberReference((MemberReferenceHandle)specification.Method);
        var signature = reference.DecodeMethodSignature(SigProvider,
            new GenericContext(owner.Context.TypeArgs, method.Context.MethodArgs));
        if (!CoreIntrinsics.IsUInt128CheckedSignature(reader.GetBlobReader(reference.Signature), interfaceForm)
            || !CoreIntrinsics.IsUInt128CheckedDecodedSignature(signature, target, source))
            throw new NotSupportedException("UInt128.CreateChecked requires its exact MethodSpec MemberRef signature");
        if (reference.Parent.Kind == HandleKind.TypeSpecification)
        {
            var parent = reader.GetTypeSpecification((TypeSpecificationHandle)reference.Parent).Signature;
            if (!(interfaceForm ? CoreIntrinsics.IsUInt128CheckedOwnerSignature(reader, parent)
                    : CoreIntrinsics.IsUInt128CheckedTypeSignature(reader, parent)))
                throw new NotSupportedException("UInt128.CreateChecked requires an unmodified exact owner TypeSpec");
        }
    }

    internal void ValidateUInt128CheckedConstrained(Module module, EntityHandle handle)
    {
        if (handle.Kind == HandleKind.TypeSpecification
            && !CoreIntrinsics.IsUInt128CheckedTypeSignature(module.Reader,
                module.Reader.GetTypeSpecification((TypeSpecificationHandle)handle).Signature))
            throw new NotSupportedException("UInt128.CreateChecked requires an unmodified constrained TypeSpec");
    }

    internal MethodInfo ResolveUInt128CheckedInterface(MethodInfo method, ClassInfo target)
    {
        if (!CoreIntrinsics.IsUInt128CheckedInterface(method)
            || method.DeclaringClass.Context.TypeArgs[0].Class != target)
            throw new NotSupportedException("UInt128.CreateChecked requires its exact constrained interface signature");
        foreach (var handle in target.Module.Reader.GetTypeDefinition(target.Handle).GetMethods())
        {
            var definition = target.Module.Reader.GetMethodDefinition(handle);
            if (!target.Module.Reader.StringComparer.Equals(definition.Name, "CreateChecked")) continue;
            var concrete = InstantiateMethodOnClass(target, target.Module, handle, method.Context.MethodArgs);
            if (CoreIntrinsics.IsUInt128Checked(concrete)) return concrete;
        }
        throw new NotSupportedException("UInt128.CreateChecked concrete integer factory is unavailable");
    }
}

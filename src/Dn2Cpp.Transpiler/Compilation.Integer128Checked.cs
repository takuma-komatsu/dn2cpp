using System.Reflection.Metadata;

namespace Dn2Cpp;

internal sealed partial class Compilation
{
    private void ValidateInteger128CheckedSpec(Module module, MethodSpecification specification, MethodInfo method)
    {
        if (method.Name != "CreateChecked") return;
        var owner = method.DeclaringClass;
        bool interfaceForm = CoreIntrinsics.IsInteger128CheckedInterfaceDefinition(owner);
        // Validate the owner before a closed-method predicate can decline it and
        // let ordinary static-interface dispatch select a different factory.
        if (interfaceForm && (owner.Module.Reader.GetTypeDefinition(owner.Handle).GetGenericParameters().Count != 1
                || owner.Context.TypeArgs.Length != 1))
            throw new NotSupportedException("INumberBase.CreateChecked requires exactly one owner type argument");
        var target = interfaceForm ? owner.Context.TypeArgs[0].Class : owner;
        if (target is null) return;
        bool signed = CoreIntrinsics.IsInt128CheckedType(target);
        if (!signed && !CoreIntrinsics.IsUInt128CheckedType(target)) return;
        if (method.Context.MethodArgs is not [var source]
            || !(signed ? CoreIntrinsics.IsInt128CheckedSource(source, target)
                : CoreIntrinsics.IsUInt128CheckedSource(source, target))) return;
        string name = signed ? "Int128" : "UInt128";
        var reader = module.Reader;
        bool exact = signed
            ? interfaceForm ? CoreIntrinsics.IsInt128CheckedInterface(method) : CoreIntrinsics.IsInt128Checked(method)
            : interfaceForm ? CoreIntrinsics.IsUInt128CheckedInterface(method) : CoreIntrinsics.IsUInt128Checked(method);
        if (!exact || !CoreIntrinsics.IsInteger128CheckedInstantiation(reader, specification.Signature))
            throw new NotSupportedException($"{name}.CreateChecked requires its exact closed integer MethodSpec signature");
        if (specification.Method.Kind != HandleKind.MemberReference) return;
        var reference = reader.GetMemberReference((MemberReferenceHandle)specification.Method);
        var signature = reference.DecodeMethodSignature(SigProvider,
            new GenericContext(owner.Context.TypeArgs, method.Context.MethodArgs));
        if (!CoreIntrinsics.IsInteger128CheckedSignature(reader, reader.GetBlobReader(reference.Signature), interfaceForm)
            || !(signed ? CoreIntrinsics.IsInt128CheckedDecodedSignature(signature, target, source)
                : CoreIntrinsics.IsUInt128CheckedDecodedSignature(signature, target, source)))
            throw new NotSupportedException($"{name}.CreateChecked requires its exact MethodSpec MemberRef signature");
        if (reference.Parent.Kind == HandleKind.TypeSpecification)
        {
            var parent = reader.GetTypeSpecification((TypeSpecificationHandle)reference.Parent).Signature;
            if (!(interfaceForm ? CoreIntrinsics.IsInteger128CheckedOwnerSignature(reader, parent)
                    : CoreIntrinsics.IsInteger128CheckedTypeSignature(reader, parent)))
                throw new NotSupportedException($"{name}.CreateChecked requires an unmodified exact owner TypeSpec");
        }
    }

    internal void ValidateInteger128CheckedConstrained(Module module, EntityHandle handle, ClassInfo target)
    {
        if (handle.Kind == HandleKind.TypeSpecification
            && !CoreIntrinsics.IsInteger128CheckedTypeSignature(module.Reader,
                module.Reader.GetTypeSpecification((TypeSpecificationHandle)handle).Signature))
            throw new NotSupportedException($"{target.Name}.CreateChecked requires an unmodified constrained TypeSpec");
    }

    internal MethodInfo ResolveInt128CheckedInterface(MethodInfo method, ClassInfo target)
    {
        if (!CoreIntrinsics.IsInt128CheckedInterface(method)
            || method.DeclaringClass.Context.TypeArgs[0].Class != target)
            throw new NotSupportedException("Int128.CreateChecked requires its exact constrained interface signature");
        foreach (var handle in target.Module.Reader.GetTypeDefinition(target.Handle).GetMethods())
        {
            var definition = target.Module.Reader.GetMethodDefinition(handle);
            if (!target.Module.Reader.StringComparer.Equals(definition.Name, "CreateChecked")) continue;
            var concrete = InstantiateMethodOnClass(target, target.Module, handle, method.Context.MethodArgs);
            if (CoreIntrinsics.IsInt128Checked(concrete)) return concrete;
        }
        throw new NotSupportedException("Int128.CreateChecked concrete integer factory is unavailable");
    }
}

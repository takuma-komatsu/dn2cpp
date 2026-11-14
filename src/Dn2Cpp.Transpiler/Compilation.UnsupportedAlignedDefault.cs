using System.Reflection.Metadata;

namespace Dn2Cpp;

internal sealed partial class Compilation
{
    internal MethodInfo? TryResolveUnsupportedAlignedReference(Module module, MemberReferenceHandle handle, GenericContext context)
    {
        var reader = module.Reader;
        var reference = reader.GetMemberReference(handle);
        string name = reader.GetString(reference.Name);
        if (!CoreIntrinsics.IsUnsupportedAlignedName(name) || reference.Parent.Kind != HandleKind.TypeSpecification)
            return null;
        var owner = ResolveTypeTokenForScan(module, reference.Parent, context)?.Class;
        if (owner is null || !CoreIntrinsics.IsUnsupportedAlignedCandidate(owner, name)) return null;
        EnsureCompleted(owner);
        var parent = reader.GetBlobReader(reader.GetTypeSpecification((TypeSpecificationHandle)reference.Parent).Signature);
        var vector = owner.Context.TypeArgs[0].Class;
        // The closed vector can still be a shape-only scan result. Admission
        // must not depend on another reachable member completing it first.
        if (vector is not null) EnsureShape(vector);
        if (vector is null || !ReadSimdAlignmentContractPrefix(module, ref parent, owner)
            || !ReadUnsupportedAlignedVector(module, ref parent, context, vector)
            || !ReadUnsupportedAlignedElement(module, ref parent, context, owner.Context.TypeArgs[1]) || parent.RemainingBytes != 0
            || !CoreIntrinsics.IsUnsupportedAlignedSignature(name, reader.GetBlobReader(reference.Signature)))
            throw UnsupportedAlignedReferenceUnsupported();
        MethodInfo? target = null;
        foreach (var method in owner.Methods)
        {
            if (method.Name != name) continue;
            if (target is not null || !CoreIntrinsics.BrUnsupportedAlignedDefault.Matches(method))
                throw UnsupportedAlignedReferenceUnsupported();
            target = method;
        }
        return target ?? throw UnsupportedAlignedReferenceUnsupported();
    }

    private bool ReadUnsupportedAlignedVector(Module module, ref BlobReader raw, GenericContext context, ClassInfo vector)
    {
        if (raw.RemainingBytes == 0) return false;
        var probe = raw;
        int code = probe.ReadByte();
        if (code is 0x13 or 0x1e)
        {
            if (!probe.TryReadCompressedInteger(out int index)) return false;
            var args = code == 0x13 ? context.TypeArgs : context.MethodArgs;
            if (index < 0 || index >= args.Length || args[index] is not { Kind: TypeKind.Class, IsCanonPlaceholder: false, Class: { } actual }
                || actual != vector) return false;
            raw = probe;
            return true;
        }
        return ReadVectorValueInstantiation(module, ref raw, vector, 0x11)
            && ReadUnsupportedAlignedElement(module, ref raw, context, vector.Context.TypeArgs[0]);
    }

    private bool ReadUnsupportedAlignedElement(Module module, ref BlobReader raw, GenericContext context, TypeDesc element)
    {
        if (!CoreIntrinsics.IsUnsupportedAlignedElement(element) || raw.RemainingBytes == 0) return false;
        var probe = raw;
        int code = probe.ReadByte();
        if (code is 0x13 or 0x1e)
        {
            if (!probe.TryReadCompressedInteger(out int index)) return false;
            var args = code == 0x13 ? context.TypeArgs : context.MethodArgs;
            if (index < 0 || index >= args.Length || !CoreIntrinsics.SameUnsupportedAlignedElement(args[index], element)) return false;
            raw = probe;
            return true;
        }
        if (element.Kind == TypeKind.Primitive)
        {
            if (code != 0x03) return false;
            raw = probe;
            return true;
        }
        return ReadVectorValueElement(module, ref raw, context, element);
    }

    private static NotSupportedException UnsupportedAlignedReferenceUnsupported() =>
        new("Unsupported aligned defaults require their exact closed CoreLib declaration and signature");
}

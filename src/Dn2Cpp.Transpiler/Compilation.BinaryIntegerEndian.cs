using System.Reflection;
using System.Reflection.Metadata;

namespace Dn2Cpp;

internal sealed partial class Compilation
{
    // Both edge production and emission select the same real MethodImpl body.
    // Other members use the ordinary static-interface resolver.
    internal MethodInfo? TryResolveBinaryIntegerEndianCall(TypeDesc? constrained, MethodInfo slot)
    {
        if (!CoreIntrinsics.IsBinaryIntegerEndianSlotCandidate(slot)
            || constrained is not { Kind: TypeKind.Primitive, IsCanonPlaceholder: false } self
            || BinaryIntegerEndianContract.Owner(self.Primitive) is not { } ownerName
            || ContainsCanonPlaceholder(slot.DeclaringClass)) return null;
        if (!CoreIntrinsics.IsBinaryIntegerEndianContractMethod(slot, out var primitive) || primitive != self.Primitive
            || slot.Rva != 0 || slot.Attributes != (MethodAttributes.Public | MethodAttributes.Static
                | MethodAttributes.Virtual | MethodAttributes.HideBySig | MethodAttributes.Abstract)
            || !CoreIntrinsics.IsBinaryIntegerEndianTrySignature(slot.Signature, primitive))
            throw BinaryIntegerEndianUnsupported();
        var module = slot.Module;
        var reader = module.Reader;
        if (!BinaryIntegerEndianContract.IsTrySignature(reader,
                reader.GetBlobReader(reader.GetMethodDefinition(slot.Handle).Signature), PrimitiveTypeCode.Void))
            throw BinaryIntegerEndianUnsupported();
        var owner = FindClassByFullName(ownerName);
        if (owner is null || owner.Module != module || !CoreIntrinsics.IsScalarNumericLeafOwner(owner))
            throw BinaryIntegerEndianUnsupported();
        MethodDefinitionHandle selected = default;
        foreach (var handle in reader.GetTypeDefinition(owner.Handle).GetMethodImplementations())
        {
            var implementation = reader.GetMethodImplementation(handle);
            if (implementation.MethodDeclaration.Kind == HandleKind.MethodDefinition
                && reader.StringComparer.Equals(reader.GetMethodDefinition((MethodDefinitionHandle)implementation.MethodDeclaration).Name, slot.Name))
                throw BinaryIntegerEndianUnsupported();
            if (implementation.MethodDeclaration.Kind != HandleKind.MemberReference) continue;
            var declaration = reader.GetMemberReference((MemberReferenceHandle)implementation.MethodDeclaration);
            if (!reader.StringComparer.Equals(declaration.Name, slot.Name)) continue;
            if (!selected.IsNil || implementation.MethodBody.Kind != HandleKind.MethodDefinition
                || !BinaryIntegerEndianContract.IsImplementationParent(reader, declaration.Parent, slot.DeclaringClass.Handle, primitive)
                || !BinaryIntegerEndianContract.IsTrySignature(reader, reader.GetBlobReader(declaration.Signature), PrimitiveTypeCode.Void))
                throw BinaryIntegerEndianUnsupported();
            selected = (MethodDefinitionHandle)implementation.MethodBody;
        }
        string name = BinaryIntegerEndianContract.LeafName(ownerName, slot.Name);
        if (selected.IsNil || !BinaryIntegerEndianContract.IsLeafDefinition(reader, selected, ownerName, name)
            || !module.MethodMap.TryGetValue(selected, out var body) || body.DeclaringClass != owner
            || !CoreIntrinsics.IsBinaryIntegerEndianBody(body)) throw BinaryIntegerEndianUnsupported();
        // Primitive owners are loaded before reachability. Emission must consume
        // the target reached earlier, never create a late model or trap wrapper.
        if (Phase == EmitPhase.Emission && !Reachable.Contains(body))
            throw new InvalidOperationException("Binary integer endian body was not reached before emission");
        return body;
    }

    private static NotSupportedException BinaryIntegerEndianUnsupported() => new(
        "Binary integer endian call requires its concrete CoreLib TryRead MethodImpl and signature");
}

using System.Reflection;
using System.Reflection.Metadata;

namespace Dn2Cpp;

internal sealed partial class Compilation
{
    internal MethodInfo? TryResolvePrimitiveParseCall(TypeDesc? constrained, MethodInfo slot)
    {
        if (!CoreIntrinsics.IsPrimitiveParseSlotCandidate(slot)
            || constrained is not { Kind: TypeKind.Primitive, IsCanonPlaceholder: false } self
            || PrimitiveParseContract.Owner(self.Primitive) is not { } ownerName
            || ContainsCanonPlaceholder(slot.DeclaringClass)) return null;
        var contract = slot.DeclaringClass;
        bool styles = GenericDefFullName(contract) == "System.Numerics.INumberBase";
        var input = CoreIntrinsics.PrimitiveParseInput(slot.Signature.ParameterTypes[0]);
        // Char has no INumberBase UTF-8 override. Its decode-first default remains real IL.
        if (input == PrimitiveParseContract.Input.Bytes && (!styles || self.Primitive == PrimitiveTypeCode.Char)) return null;
        var module = slot.Module;
        var reader = module.Reader;
        bool isTry = slot.Name == "TryParse";
        var attributes = MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.Virtual | MethodAttributes.HideBySig;
        if (input == PrimitiveParseContract.Input.Chars) attributes |= MethodAttributes.Abstract;
        if (contract.Module != module || module.AssemblyName != "System.Private.CoreLib" || contract.Handle.IsNil || slot.Handle.IsNil
            || !contract.IsInterface || contract.IsValueType || contract.GenericArity != 1
            || contract.Context.TypeArgs is not [{ Kind: TypeKind.Primitive, IsCanonPlaceholder: false } actual] || actual.Primitive != self.Primitive
            || slot.Context.TypeArgs is not [{ Kind: TypeKind.Primitive, IsCanonPlaceholder: false } methodSelf] || methodSelf.Primitive != self.Primitive
            || slot.Context.MethodArgs.Length != 0 || slot.IsSynthetic || slot.PInvoke is not null
            || slot.Attributes != attributes || slot.ImplAttributes != MethodImplAttributes.IL
            || (input == PrimitiveParseContract.Input.Chars ? slot.Rva != 0 : slot.Rva == 0)
            || !CoreIntrinsics.IsPrimitiveParseSignature(slot.Signature, isTry, self.Primitive, input, styles, true)
            || !BinaryIntegerEndianContract.IsNamedType(reader, contract.Handle, styles ? "System.Numerics" : "System",
                styles ? "INumberBase`1" : "ISpanParsable`1")) throw PrimitiveParseUnsupported();
        var definition = reader.GetMethodDefinition(slot.Handle);
        if (reader.GetTypeDefinition(contract.Handle).GetGenericParameters().Count != 1
            || definition.GetDeclaringType() != contract.Handle || !reader.StringComparer.Equals(definition.Name, slot.Name)
            || definition.GetGenericParameters().Count != 0 || definition.Attributes != slot.Attributes
            || definition.ImplAttributes != slot.ImplAttributes || definition.RelativeVirtualAddress != slot.Rva
            || !PrimitiveParseContract.TrySignature(reader, reader.GetBlobReader(definition.Signature), isTry,
                PrimitiveTypeCode.Void, styles, true, out var rawInput) || rawInput != input) throw PrimitiveParseUnsupported();
        var owner = FindClassByFullName(ownerName);
        if (owner is null || owner.Module != module || !CoreIntrinsics.IsScalarNumericLeafOwner(owner)) throw PrimitiveParseUnsupported();
        MethodDefinitionHandle selected = default;
        foreach (var handle in reader.GetTypeDefinition(owner.Handle).GetMethodImplementations())
        {
            var edge = reader.GetMethodImplementation(handle);
            if (edge.MethodDeclaration.Kind == HandleKind.MethodDefinition
                && reader.StringComparer.Equals(reader.GetMethodDefinition((MethodDefinitionHandle)edge.MethodDeclaration).Name, slot.Name))
                throw PrimitiveParseUnsupported();
            if (edge.MethodDeclaration.Kind != HandleKind.MemberReference) continue;
            var declaration = reader.GetMemberReference((MemberReferenceHandle)edge.MethodDeclaration);
            if (!reader.StringComparer.Equals(declaration.Name, slot.Name)) continue;
            int parent = PrimitiveParseContract.ImplementationParent(reader, declaration.Parent, self.Primitive, out var rawContract);
            if (parent == 0 || !PrimitiveParseContract.TrySignature(reader, reader.GetBlobReader(declaration.Signature), isTry,
                    PrimitiveTypeCode.Void, parent == 1, true, out var sibling)
                || parent == 2 && sibling != PrimitiveParseContract.Input.Chars
                || parent == 3 && sibling != PrimitiveParseContract.Input.String
                || parent == 4 && sibling != PrimitiveParseContract.Input.Bytes) throw PrimitiveParseUnsupported();
            if (rawContract != contract.Handle || sibling != input) continue;
            if (!selected.IsNil || edge.MethodBody.Kind != HandleKind.MethodDefinition) throw PrimitiveParseUnsupported();
            selected = (MethodDefinitionHandle)edge.MethodBody;
        }
        if (selected.IsNil || !module.MethodMap.TryGetValue(selected, out var body) || body.Module != module || body.Handle != selected || body.DeclaringClass != owner
            || !CoreIntrinsics.IsPrimitiveParseTarget(body, isTry, input, styles)) throw PrimitiveParseUnsupported();
        if (Phase == EmitPhase.Emission && !Reachable.Contains(body))
            throw new InvalidOperationException("Primitive span parsing target was not reached before emission");
        return body;
    }

    private static NotSupportedException PrimitiveParseUnsupported() => new(
        "Primitive span parsing requires its concrete CoreLib MethodImpl and exact overload signature");
}

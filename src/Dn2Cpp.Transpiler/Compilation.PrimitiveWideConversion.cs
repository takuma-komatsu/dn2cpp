using System.Reflection;
using System.Reflection.Metadata;
using SRME = System.Reflection.Metadata.Ecma335.MetadataTokens;

namespace Dn2Cpp;

internal sealed partial class Compilation
{
    internal MethodInfo? TryResolvePrimitiveWideConversionCall(TypeDesc? constrained, MethodInfo slot)
    {
        if (!CoreIntrinsics.IsPrimitiveWideConversionSlotFamily(slot)
            || constrained is not { Kind: TypeKind.Primitive, IsCanonPlaceholder: false } self
            || ContainsCanonPlaceholder(slot.DeclaringClass)
            || slot.DeclaringClass.Context.TypeArgs is not [{ IsCanonPlaceholder: false } source]
            || source.Primitive != self.Primitive) return null;
        var target = slot.Context.MethodArgs[0];
        var contract = slot.DeclaringClass;
        var module = slot.Module;
        var reader = module.Reader;
        if (module != contract.Module || module.AssemblyName != "System.Private.CoreLib"
            || contract.Handle.IsNil || slot.Handle.IsNil || !contract.IsInterface || contract.IsValueType
            || contract.GenericArity != 1 || slot.Context.TypeArgs is not [{ Kind: TypeKind.Primitive, IsCanonPlaceholder: false } methodSelf]
            || methodSelf.Primitive != self.Primitive || slot.IsSynthetic || slot.PInvoke is not null || slot.Rva != 0
            || slot.Attributes != (MethodAttributes.Family | MethodAttributes.Static | MethodAttributes.Virtual | MethodAttributes.HideBySig | MethodAttributes.Abstract)
            || slot.ImplAttributes != MethodImplAttributes.IL
            || !CoreIntrinsics.IsPrimitiveWideConversionSignature(slot.Signature, self.Primitive, target)
            || !BinaryIntegerEndianContract.IsNamedType(reader, contract.Handle, "System.Numerics", "INumberBase`1")
            || reader.GetTypeDefinition(contract.Handle).GetGenericParameters().Count != 1) throw PrimitiveWideConversionUnsupported();
        var definition = reader.GetMethodDefinition(slot.Handle);
        if (definition.GetDeclaringType() != contract.Handle || !reader.StringComparer.Equals(definition.Name, slot.Name)
            || definition.Attributes != slot.Attributes || definition.ImplAttributes != slot.ImplAttributes
            || definition.RelativeVirtualAddress != slot.Rva || !PrimitiveWideConversionContract.HasConstraint(reader, definition)
            || !PrimitiveWideConversionContract.IsSignature(reader.GetBlobReader(definition.Signature), PrimitiveTypeCode.Void))
            throw PrimitiveWideConversionUnsupported();
        string ownerName = PrimitiveWideConversionContract.Owner(self.Primitive)!;
        var owner = FindClassByFullName(ownerName);
        if (owner is null || owner.Module != module || !CoreIntrinsics.IsScalarNumericLeafOwner(owner)) throw PrimitiveWideConversionUnsupported();
        MethodDefinitionHandle selected = default;
        foreach (var handle in reader.GetTypeDefinition(owner.Handle).GetMethodImplementations())
        {
            var edge = reader.GetMethodImplementation(handle);
            if (edge.MethodDeclaration.Kind == HandleKind.MethodDefinition
                && reader.StringComparer.Equals(reader.GetMethodDefinition((MethodDefinitionHandle)edge.MethodDeclaration).Name, slot.Name))
                throw PrimitiveWideConversionUnsupported();
            if (edge.MethodDeclaration.Kind != HandleKind.MemberReference) continue;
            var declaration = reader.GetMemberReference((MemberReferenceHandle)edge.MethodDeclaration);
            if (!reader.StringComparer.Equals(declaration.Name, slot.Name)) continue;
            if (!selected.IsNil || edge.MethodBody.Kind != HandleKind.MethodDefinition
                || !BinaryIntegerEndianContract.IsImplementationParent(reader, declaration.Parent, contract.Handle, self.Primitive)
                || !PrimitiveWideConversionContract.IsSignature(reader.GetBlobReader(declaration.Signature), PrimitiveTypeCode.Void))
                throw PrimitiveWideConversionUnsupported();
            selected = (MethodDefinitionHandle)edge.MethodBody;
        }
        if (selected.IsNil || !PrimitiveWideConversionContract.IsBodyDefinition(reader, selected, ownerName,
                PrimitiveWideConversionContract.BodyName(ownerName)!)) throw PrimitiveWideConversionUnsupported();
        MethodInfo body;
        if (Phase == EmitPhase.Emission)
        {
            // Read the ordinary instantiation cache only; parallel emission creates no model.
            if (!_methodInstances.TryGetValue((module.Index, SRME.GetToken(selected)), out var instances)
                || !instances.TryGetValue((owner.CppName, slot.Context.MethodArgs), out body!) || !Reachable.Contains(body))
                throw new InvalidOperationException("Primitive wide conversion body was not reached before emission");
        }
        else body = InstantiateMethodOnClass(owner, module, selected, slot.Context.MethodArgs);
        if (body.Module != module || body.Handle != selected || body.DeclaringClass != owner
            || body.Context.MethodArgs is not [var actualTarget] || actualTarget.Class != target.Class
            || !CoreIntrinsics.IsPrimitiveWideConversionBody(body)) throw PrimitiveWideConversionUnsupported();
        return body;
    }

    private static NotSupportedException PrimitiveWideConversionUnsupported() => new(
        "Primitive wide conversion requires its exact CoreLib MethodImpl and closed signature");
}

using System.Reflection;
using System.Reflection.Metadata;
using SRME = System.Reflection.Metadata.Ecma335.MetadataTokens;

namespace Dn2Cpp;

internal sealed partial class Compilation
{
    private MethodInfo ResolveFixedGenericVectorEstimateGuard(MethodInfo method)
    {
        string lowerName = method.DeclaringClass.FullName switch
        {
            "System.Runtime.Intrinsics.Vector128" => "Vector64",
            "System.Runtime.Intrinsics.Vector256" => "Vector128",
            "System.Runtime.Intrinsics.Vector512" => "Vector256",
            _ => throw GenericVectorGuardUnsupported(),
        };
        var instructions = GenericVectorGuardInstructions(method);
        if (instructions.Count < 7
            || instructions[0].OpCode != ILOpCode.Ldarg_0 || instructions[1].OpCode != ILOpCode.Ldfld
            || instructions[2].OpCode != ILOpCode.Ldarg_1 || instructions[3].OpCode != ILOpCode.Ldfld
            || instructions[4].OpCode != ILOpCode.Ldarg_2 || instructions[5].OpCode != ILOpCode.Ldfld
            || instructions[1].Token != instructions[3].Token || instructions[1].Token != instructions[5].Token
            || instructions[6].OpCode != ILOpCode.Call)
            throw GenericVectorGuardUnsupported();
        var module = method.Module;
        var reader = module.Reader;
        var call = SRME.EntityHandle(instructions[6].Token);
        if (call.Kind != HandleKind.MethodSpecification) throw GenericVectorGuardUnsupported();
        var specification = reader.GetMethodSpecification((MethodSpecificationHandle)call);
        var raw = reader.GetBlobReader(specification.Signature);
        if (specification.Method.Kind != HandleKind.MethodDefinition || raw.RemainingBytes != 4
            || raw.ReadByte() != 0x0a || raw.ReadByte() != 1 || raw.ReadByte() != 0x1e || raw.ReadByte() != 0)
            throw GenericVectorGuardUnsupported();
        var definition = reader.GetMethodDefinition((MethodDefinitionHandle)specification.Method);
        var owner = reader.GetTypeDefinition(definition.GetDeclaringType());
        // Check strict descent before resolution can recursively validate a cached target.
        if (!reader.StringComparer.Equals(owner.Namespace, "System.Runtime.Intrinsics")
            || !reader.StringComparer.Equals(owner.Name, lowerName))
            throw GenericVectorGuardUnsupported();
        foreach (var handle in owner.GetMethods())
            if (reader.StringComparer.Equals(reader.GetMethodDefinition(handle).Name, ".cctor"))
                throw GenericVectorGuardUnsupported();
        var lower = TryResolveGenericVectorEstimateSpec(module, (MethodSpecificationHandle)call, method.Context);
        if (lower is null || lower.Module != module || lower.Context.MethodArgs[0].Class != method.Context.MethodArgs[0].Class)
            throw GenericVectorGuardUnsupported();
        ValidateFixedGenericVectorEstimateField(method, instructions[1].Token, lower.Signature.ReturnType.Class!);
        return ResolveGenericVectorEstimateGuard(lower);
    }

    private void ValidateFixedGenericVectorEstimateField(MethodInfo method, int token, ClassInfo half)
    {
        var module = method.Module;
        var reader = module.Reader;
        var vector = method.Signature.ReturnType.Class!;
        var handle = SRME.EntityHandle(token);
        if (handle.Kind != HandleKind.MemberReference) throw GenericVectorGuardUnsupported();
        var reference = reader.GetMemberReference((MemberReferenceHandle)handle);
        if (reference.Parent.Kind != HandleKind.TypeSpecification
            || !reader.StringComparer.Equals(reference.Name, "_lower"))
            throw GenericVectorGuardUnsupported();
        var parent = reader.GetBlobReader(reader.GetTypeSpecification((TypeSpecificationHandle)reference.Parent).Signature);
        if (!ReadFixedGenericVectorEstimateType(ref parent, vector.Handle, 0x1e) || parent.RemainingBytes != 0)
            throw GenericVectorGuardUnsupported();
        var signature = reader.GetBlobReader(reference.Signature);
        if (signature.RemainingBytes == 0 || signature.ReadByte() != 6
            || !ReadFixedGenericVectorEstimateType(ref signature, half.Handle, 0x13) || signature.RemainingBytes != 0)
            throw GenericVectorGuardUnsupported();
        var type = reader.GetTypeDefinition(vector.Handle);
        int size = method.DeclaringClass.FullName switch
        {
            "System.Runtime.Intrinsics.Vector128" => 16,
            "System.Runtime.Intrinsics.Vector256" => 32,
            "System.Runtime.Intrinsics.Vector512" => 64,
            _ => throw GenericVectorGuardUnsupported(),
        };
        var layout = type.GetLayout();
        if (vector.Module != module || half.Module != module
            || (type.Attributes & TypeAttributes.LayoutMask) != TypeAttributes.SequentialLayout
            || layout.Size != size || layout.PackingSize != 0
            || !CoreIntrinsics.IsFixedVectorManagedValueClass(vector)
            || !CoreIntrinsics.IsFixedVectorManagedValueClass(half)
            || vector.Context.TypeArgs[0].Class != half.Context.TypeArgs[0].Class)
            throw GenericVectorGuardUnsupported();
        int found = 0;
        foreach (var fieldHandle in type.GetFields())
        {
            var field = reader.GetFieldDefinition(fieldHandle);
            if (!reader.StringComparer.Equals(field.Name, "_lower")) continue;
            var fieldSignature = reader.GetBlobReader(field.Signature);
            if (field.GetDeclaringType() != vector.Handle
                || field.Attributes != (FieldAttributes.Assembly | FieldAttributes.InitOnly)
                || fieldSignature.RemainingBytes == 0 || fieldSignature.ReadByte() != 6
                || !ReadFixedGenericVectorEstimateType(ref fieldSignature, half.Handle, 0x13)
                || fieldSignature.RemainingBytes != 0)
                throw GenericVectorGuardUnsupported();
            found++;
        }
        if (found != 1) throw GenericVectorGuardUnsupported();
        // A by-value argument's exact instance field is a pure copy, not a lane read.
    }

    private static bool ReadFixedGenericVectorEstimateType(ref BlobReader blob, TypeDefinitionHandle expected, int variable)
    {
        return blob.RemainingBytes >= 6 && blob.ReadByte() == 0x15 && blob.ReadByte() == 0x11
            && blob.TryReadCompressedInteger(out int token) && token == SRME.GetRowNumber(expected) << 2
            && blob.RemainingBytes >= 3 && blob.ReadByte() == 1 && blob.ReadByte() == variable && blob.ReadByte() == 0;
    }
}

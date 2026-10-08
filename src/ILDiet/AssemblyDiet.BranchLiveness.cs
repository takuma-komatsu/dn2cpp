using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using Mono.Cecil;
using Mono.Cecil.Cil;
using TypeReference = Mono.Cecil.TypeReference;
using TypeDefinition = Mono.Cecil.TypeDefinition;
using TypeSpecification = Mono.Cecil.TypeSpecification;
using CecilInstruction = Mono.Cecil.Cil.Instruction;

namespace Dn2Cpp;

internal sealed partial class AssemblyDiet
{
    // The shared branch graph accepts only loaded exact identities. Open,
    // canonical and unresolved types keep both arms live.
    private BranchLiveness? CallerBranchLiveness(MethodDepthContext caller)
    {
        var method = caller.Method;
        bool hasFoldableBranch = method.Body.Instructions.Any(instruction =>
            instruction.Operand is MethodReference target
            && (target.DeclaringType.FullName == "System.Type"
                && target.Name is "op_Equality" or "op_Inequality"
                || CoreIntrinsics.IsConstFoldedGetterName(target.Name) || target.Name == "get_IsSupported"));
        if (!hasFoldableBranch) return null;
        using var stream = File.OpenRead(method.Module.FileName);
        using var pe = new PEReader(stream);
        var body = pe.GetMethodBody((int)method.RVA);
        var instructions = ILDecoder.Decode(body.GetILContent());
        return BranchLiveness.Compute(instructions, body,
            token => method.Module.LookupToken(token) is MethodReference target ? CallerFoldedGetter(target) : null,
            token => method.Module.LookupToken(token) is MethodReference target
                && target.DeclaringType.FullName == "System.Type"
                ? target.Name switch
                {
                    "GetTypeFromHandle" => TypeIdentityCall.GetTypeFromHandle,
                    "op_Equality" => TypeIdentityCall.OpEquality,
                    "op_Inequality" => TypeIdentityCall.OpInequality,
                    _ => TypeIdentityCall.None
                }
                : TypeIdentityCall.None,
            (left, right) =>
            {
                if (method.Module.LookupToken(left) is not TypeReference first
                    || method.Module.LookupToken(right) is not TypeReference second) return null;
                string? a = CallerTypeIdentity(SubstituteCallerType(first, caller), !first.ContainsGenericParameter);
                string? b = CallerTypeIdentity(SubstituteCallerType(second, caller), !second.ContainsGenericParameter);
                return a is not null && b is not null ? a == b : null;
            });
    }

    private static bool? CallerFoldedGetter(MethodReference target)
    {
        string type = target.DeclaringType.FullName;
        if (CoreIntrinsics.IsConstFoldedGetterName(target.Name)
            && CoreIntrinsics.ConstFoldedGetter(type, target.Name) is { } folded) return folded;
        return target.Name == "get_IsSupported" && target.DeclaringType is not TypeSpecification
            && CoreIntrinsics.PlatformIsaFamily(type.Replace('/', '+')) is { } family
            ? CoreIntrinsics.IsaGetterFold(family) : null;
    }

    private string? CallerTypeIdentity(TypeReference type, bool literalClosedGeneric)
    {
        if (type is ArrayType array)
            return array.IsVector && CallerTypeIdentity(array.ElementType, literalClosedGeneric) is { } element
                ? "a:" + element : null;
        if (type is GenericInstanceType generic)
        {
            if (!literalClosedGeneric || generic.ContainsGenericParameter
                || Resolve(generic) is not { } definition || !_byModule.ContainsKey(definition.Module)
                || generic.GenericArguments.Count != definition.GenericParameters.Count) return null;
            var arguments = new List<string>();
            foreach (var argument in generic.GenericArguments)
            {
                if (CallerTypeIdentity(argument, true) is not { } identity) return null;
                arguments.Add(identity.Length + ":" + identity);
            }
            return "g:" + CallerNominalIdentity(definition) + "<" + string.Join(",", arguments) + ">";
        }
        if (type is TypeSpecification || type.ContainsGenericParameter) return null;
        if (CallerPrimitiveIdentity(type) is { } primitive) return "p:" + primitive;
        if (Resolve(type) is not { } owner || !_byModule.ContainsKey(owner.Module)
            || owner.HasGenericParameters) return null;
        return CallerNominalIdentity(owner);
    }

    private static string CallerNominalIdentity(TypeDefinition owner)
    {
        string assembly = owner.Module.Assembly.Name.FullName;
        return "c:" + assembly.Length + ":" + assembly + ":" + owner.MetadataToken.ToInt32();
    }

    private MetadataType? CallerPrimitiveIdentity(TypeReference type)
    {
        if (ExactPrimitiveIdentity(type.MetadataType)) return type.MetadataType;
        if (type is TypeSpecification || type.ContainsGenericParameter
            || Resolve(type) is not { } owner || !_byModule.ContainsKey(owner.Module)
            || owner.Module.Assembly.Name.Name is not ("System.Private.CoreLib" or "mscorlib")) return null;
        return owner.FullName switch
        {
            "System.Boolean" => MetadataType.Boolean,
            "System.Char" => MetadataType.Char,
            "System.SByte" => MetadataType.SByte,
            "System.Byte" => MetadataType.Byte,
            "System.Int16" => MetadataType.Int16,
            "System.UInt16" => MetadataType.UInt16,
            "System.Int32" => MetadataType.Int32,
            "System.UInt32" => MetadataType.UInt32,
            "System.Int64" => MetadataType.Int64,
            "System.UInt64" => MetadataType.UInt64,
            "System.Single" => MetadataType.Single,
            "System.Double" => MetadataType.Double,
            "System.String" => MetadataType.String,
            "System.IntPtr" => MetadataType.IntPtr,
            "System.UIntPtr" => MetadataType.UIntPtr,
            "System.Object" => MetadataType.Object,
            _ => null
        };
    }

    private static bool ExactPrimitiveIdentity(MetadataType type) => type is
        MetadataType.Boolean or MetadataType.Char or MetadataType.SByte or MetadataType.Byte
        or MetadataType.Int16 or MetadataType.UInt16 or MetadataType.Int32 or MetadataType.UInt32
        or MetadataType.Int64 or MetadataType.UInt64 or MetadataType.Single or MetadataType.Double
        or MetadataType.String or MetadataType.IntPtr or MetadataType.UIntPtr or MetadataType.Object;

    private bool IsPrimitiveConstrainedCall(CecilInstruction instruction, MethodDepthContext caller)
    {
        if (instruction.OpCode.Code != Code.Callvirt) return false;
        for (var prefix = instruction.Previous; prefix is not null && prefix.OpCode.OpCodeType == OpCodeType.Prefix;
            prefix = prefix.Previous)
            if (prefix.OpCode.Code == Code.Constrained && prefix.Operand is TypeReference type)
                return CallerPrimitiveIdentity(SubstituteCallerType(type, caller)) is { } primitive
                    && primitive is not (MetadataType.Object or MetadataType.String);
        return false;
    }
}

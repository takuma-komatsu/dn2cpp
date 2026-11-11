using System.Reflection;
using System.Reflection.Metadata;

namespace Dn2Cpp;

internal static partial class CoreIntrinsics
{
    // Identity is independent of the numeric admission and Numerics-only policies.
    internal static bool IsFixedVectorValueOwner(ClassInfo owner) => FixedVectorValueOwnerWidth(owner) != 0;

    private static int FixedVectorValueOwnerWidth(ClassInfo owner)
    {
        if (owner.Handle.IsNil || owner.Module.AssemblyName != "System.Private.CoreLib") return 0;
        var reader = owner.Module.Reader;
        var definition = reader.GetTypeDefinition(owner.Handle);
        if (!definition.GetDeclaringType().IsNil || definition.GetGenericParameters().Count != 1
            || !reader.StringComparer.Equals(definition.Namespace, "System.Runtime.Intrinsics")) return 0;
        if (reader.StringComparer.Equals(definition.Name, "Vector64`1")) return 8;
        if (reader.StringComparer.Equals(definition.Name, "Vector128`1")) return 16;
        if (reader.StringComparer.Equals(definition.Name, "Vector256`1")) return 32;
        if (reader.StringComparer.Equals(definition.Name, "Vector512`1")) return 64;
        return 0;
    }

    internal static bool IsFixedVectorValueStorageClass(ClassInfo owner)
    {
        int width = FixedVectorValueOwnerWidth(owner);
        string? cpp = width switch
        {
            8 => "Dn2CppVector64", 16 => "Dn2CppVector128",
            32 => "Dn2CppVector256", 64 => "Dn2CppVector512", _ => null,
        };
        if (cpp is null || owner.IntrinsicCppName != cpp) return false;
        var reader = owner.Module.Reader;
        foreach (var handle in reader.GetTypeDefinition(owner.Handle).GetGenericParameters())
        {
            var parameter = reader.GetGenericParameter(handle);
            if (parameter.Parent != owner.Handle || parameter.Index != 0
                || parameter.Attributes != GenericParameterAttributes.None || parameter.GetConstraints().Count != 0)
                return false;
        }
        return true;
    }

    internal static int VectorValueWidth(ClassInfo owner)
    {
        if (!IsVectorValueClass(owner))
            throw new InvalidOperationException("Vector value width requires an admitted numeric owner");
        return IsVectorExplicitOwner(owner) ? 16 : FixedVectorValueOwnerWidth(owner);
    }

    internal static bool IsFixedVectorValueEquatableCandidate(ClassInfo owner, ClassInfo vector)
    {
        if (!IsFixedVectorValueOwner(vector) || owner.Module != vector.Module || owner.Handle.IsNil) return false;
        var reader = owner.Module.Reader;
        var definition = reader.GetTypeDefinition(owner.Handle);
        return definition.GetDeclaringType().IsNil && reader.StringComparer.Equals(definition.Namespace, "System")
            && reader.StringComparer.Equals(definition.Name, "IEquatable`1");
    }
}

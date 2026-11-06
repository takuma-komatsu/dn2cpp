using System.Reflection;

namespace Dn2Cpp;

internal static partial class CoreIntrinsics
{
    internal static bool IsFixedVectorManagedThrowHelper(MethodInfo method) =>
        method.Name is "ThrowForUnsupportedIntrinsicsVector64BaseType" or "ThrowForUnsupportedIntrinsicsVector128BaseType"
        && IsVectorManagedThrowDeclaration(method);

    // Unsupported value elements retain CoreLib's managed storage and guards.
    // They never acquire numeric lane support from the fixed-width identity.
    internal static bool IsFixedVectorManagedValueClass(ClassInfo owner)
    {
        if (!IsFixedVectorValueOwner(owner) || owner.IntrinsicCppName is not null
            || !owner.IsValueType || owner.IsEnum || owner.IsInterface || owner.GenericArity != 1
            || owner.Context.TypeArgs is not [var element] || !IsVectorManagedValueElement(element))
            return false;
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
}

namespace System.Numerics;

// The generic-math emitter replaces this MethodSpec before EmitManagedCall.
internal static class INumberBase_<T>
{
    static INumberBase_() =>
        GenericCctorFirstUseSubset.State.MethodSpecRoute =
            GenericCctorFirstUseSubset.Registry.IsRegistered ? "registered" : "too-early";

    internal static T CreateTruncating<U>(U value) => default!;
}

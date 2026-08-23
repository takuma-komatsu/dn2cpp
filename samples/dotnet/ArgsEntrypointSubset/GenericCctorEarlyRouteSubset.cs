namespace System.Runtime.Intrinsics;

// The emitter replaces get_IsSupported before its ordinary managed-call tail.
internal static class FirstUseRoute<T>
{
    static FirstUseRoute() =>
        GenericCctorFirstUseSubset.State.EarlyRoute =
            GenericCctorFirstUseSubset.Registry.IsRegistered ? "registered" : "too-early";

    internal static bool IsSupported => false;
}

using System;

namespace HotUpdateBase;

// The declared Type return cannot bind to this interface-returning delegate.
public delegate ICloneable CloneThunk();

internal static class TypeGetterSignatureFixture
{
    // Rooted only by the real-CoreLib base images, where ICloneable is loaded.
    public static void EmitSurface()
    {
        CloneThunk clone = CloneFixture;
        _ = clone();
    }

    private static ICloneable CloneFixture() => "clone";
}

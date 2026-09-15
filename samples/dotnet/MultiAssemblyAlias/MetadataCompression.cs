using Dn2Cpp.Runtime;

namespace MetadataCompressionCollision;

public sealed class PolicyAttribute : NoCompressMetadataAttribute { }

[Policy]
public sealed class MarkedSubject { }

[Policy]
public sealed class SharedSubject<T> { }

public static class MetadataFactory
{
    // Distinct arguments keep both assembly owners in emitted closed types.
    public static object Shared() => new SharedSubject<string>();
    public static object Marked() => new MarkedSubject();
}

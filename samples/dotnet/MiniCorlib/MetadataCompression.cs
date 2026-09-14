using System;
using Dn2Cpp.Runtime;

namespace MetadataCompressionCollision
{
    public sealed class PolicyAttribute : Attribute { }

    [Policy]
    public sealed class UnmarkedSubject { }

    public sealed class SharedSubject<T> { }
}

namespace MiniBcl
{
    public static class MetadataPolicy
    {
        public class FastAttribute : NoCompressMetadataAttribute { }
    }

    public class IntermediateFastAttribute : MetadataPolicy.FastAttribute { }

    public sealed class ExternalFastAttribute : IntermediateFastAttribute { }

    public static class MetadataBaseContainer
    {
        [ExternalFast]
        public class Base<T> { }
    }

    public class MetadataMiddle : MetadataBaseContainer.Base<int> { }

    [NoCompressMetadata]
    public sealed class MetadataUnreachable
    {
        public static int MustRemainUnreachable() => 91;
    }
}

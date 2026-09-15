extern alias metadataAlias;

using System;
using MiniBcl;

namespace MultiAssembly;

[ExternalFast]
sealed class MetadataExternalPolicy
{
    public int Reachable() => 17;
    public int MustRemainUnreachable() => 92;
}

sealed class MetadataDerived : MetadataMiddle { }

[global::MetadataCompressionCollision.Policy]
sealed class MetadataScopeUnmarked { }

[metadataAlias::MetadataCompressionCollision.Policy]
sealed class MetadataScopeMarked { }

static class MetadataCompression
{
    internal static void Run()
    {
        Console.WriteLine("metadata-policy-assembly-begin");
        object direct = new MetadataExternalPolicy();
        object derived = new MetadataDerived();
        object plain = new MetadataCompressionCollision.UnmarkedSubject();
        object marked = metadataAlias::MetadataCompressionCollision.MetadataFactory.Marked();
        object scopedPlain = new MetadataScopeUnmarked();
        object scopedMarked = new MetadataScopeMarked();
        object sharedFirst = new MetadataCompressionCollision.SharedSubject<int>();
        object sharedSecond = metadataAlias::MetadataCompressionCollision.MetadataFactory.Shared();
        Console.WriteLine("metadata-policy-direct=" + direct.GetType().Name + "/" + ((MetadataExternalPolicy)direct).Reachable());
        Console.WriteLine("metadata-policy-derived=" + derived.GetType().Name + "/" + derived.GetType().BaseType.Name);
        Console.WriteLine("metadata-policy-scopes=" + plain.GetType().Name + "/" + marked.GetType().Name);
        Console.WriteLine("metadata-policy-reference-scopes=" + scopedPlain.GetType().Name + "/" + scopedMarked.GetType().Name);
        Console.WriteLine("metadata-policy-shared=" + sharedFirst.GetType().GetGenericTypeDefinition().Name
            + "/" + sharedSecond.GetType().GetGenericTypeDefinition().Name);
        Console.WriteLine("metadata-policy-assembly-end");
    }
}

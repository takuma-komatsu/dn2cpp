using System;

namespace Q
{
    public class aLpha<T>
    {
        public class Mid<U>
        {
            public class Leaf<V> { }
        }
    }
}

namespace TypeGetTypeIgnoreCase
{
    public static class ForwardedNestedTypes
    {
        public static Type TargetLeaf => typeof(Q.aLpha<int>.Mid<string>.Leaf<long>).GetGenericTypeDefinition();
    }
}

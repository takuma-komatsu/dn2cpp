using System.Runtime.CompilerServices;

[assembly: TypeForwardedTo(typeof(TypeCaseCollision.Growth))]
[assembly: TypeForwardedTo(typeof(Q.aLpha<>))]

namespace TypeCaseCollision
{
    public sealed class gROWTH { }
}

namespace TypeGetTypeIgnoreCaseForwarder
{
    public static class SourceTypes
    {
        public static System.Type LocalCollision => typeof(TypeCaseCollision.gROWTH);
        public static System.Type LocalLeaf => typeof(Q.Alpha<int>.Mid<string>.Leaf<long>).GetGenericTypeDefinition();
    }
}

namespace Q
{
    public class Alpha<T>
    {
        public class Mid<U>
        {
            public class Leaf<V> { }
        }
    }
}

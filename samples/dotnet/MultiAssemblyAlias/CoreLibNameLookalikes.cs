#pragma warning disable CS0436 // the look-alike CoreLib names below are the point
namespace AssemblyScopeCollision
{
    public static class CoreLibNameLookalikes
    {
        public static string Read() => System.Threading.ManagedThreadId.Current + "/"
            + System.Runtime.CompilerServices.AsyncHelpers.Tag() + "/" + System.Runtime.CompilerServices.AsyncHelpers.Tag<int>()
            + "/" + System.Runtime.CompilerServices.RuntimeHelpers.IsRuntimeAsync();
    }
}

namespace System.Threading
{
    internal static class ManagedThreadId
    {
        public static int Current => 2345;
    }
}

namespace System.Runtime.CompilerServices
{
    public static class AsyncHelpers
    {
        public static string Tag() => "library-async";
        public static string Tag<T>() => "library-async-" + typeof(T).Name;
    }
}

namespace System.Runtime.CompilerServices
{
    internal static class RuntimeHelpers
    {
        public static bool IsRuntimeAsync() => true;
    }
}

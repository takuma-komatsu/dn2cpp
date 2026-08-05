using System;

namespace Dn2Cpp.Runtime
{
    // Internal copy of Dn2Cpp.Runtime.HotPathAttribute (matched by full name only).
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor, AllowMultiple = false, Inherited = false)]
    internal sealed class HotPathAttribute : Attribute
    {
        public bool SkipBoundsChecks { get; set; }
        public bool NoAlloc { get; set; }
    }
}

namespace HotPathNoAllocArrayBad
{
    using Dn2Cpp.Runtime;

    /// <summary>A [HotPath(NoAlloc = true)] method that allocates directly — a
    /// depth-1 violation: <c>new int[n]</c> lowers to a GC array allocation
    /// (<c>dn2cpp_newarr_*</c>) in the marked body itself. Transpiling this must
    /// FAIL (error: / exit 2), naming the method and the offending construct. The
    /// program is never built to native.</summary>
    internal static class Program
    {
        [HotPath(NoAlloc = true)]
        private static int[] MakeBuffer(int n)
        {
            return new int[n];
        }

        private static void Main()
        {
            Console.WriteLine(MakeBuffer(4).Length);
        }
    }
}

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

namespace HotPathNoAllocDeepBad
{
    using Dn2Cpp.Runtime;

    internal sealed class Node
    {
        public int V;
    }

    /// <summary>A [HotPath(NoAlloc = true)] method that allocates two calls deep —
    /// the marked <c>Compute</c> is itself clean, but the plain static helper it
    /// calls (<c>Build</c>) allocates a reference type (<c>new Node()</c>).
    /// Transpiling this must FAIL (error: / exit 2), and the reported call chain
    /// must name the intermediate helper <c>Build</c>. The program is never built
    /// to native.</summary>
    internal static class Program
    {
        [HotPath(NoAlloc = true)]
        private static int Compute(int n)
        {
            return Build(n).V;
        }

        private static Node Build(int n)
        {
            return new Node { V = n * 3 };
        }

        private static void Main()
        {
            Console.WriteLine(Compute(7));
        }
    }
}

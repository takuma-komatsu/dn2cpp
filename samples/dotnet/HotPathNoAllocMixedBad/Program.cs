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

namespace HotPathNoAllocMixedBad
{
    using Dn2Cpp.Runtime;

    /// <summary>Four [HotPath(NoAlloc = true)] methods, each allocating through a
    /// directly-emitted runtime helper the verifier must detect: ToString on an
    /// object receiver (dn2cpp_object_tostring — a runtime-internal dispatch that
    /// returns a fresh string), two-string concat (dn2cpp_string_concat2),
    /// Substring (dn2cpp_str_substring), and a multi-dimensional array newobj
    /// (dn2cpp_newmdarr, which the dn2cpp_newarr_ prefix does not match). None of
    /// these lower to dn2cpp_alloc/dn2cpp_newarr_, so each is a positive control
    /// for its own token family. The verifier reports all violations in one
    /// deterministic message, so a single transpile must FAIL (error: / exit 2)
    /// naming every method and its helper. The program is never built to native.</summary>
    internal static class Program
    {
        [HotPath(NoAlloc = true)]
        private static string StringifyBox(object o)
        {
            return o.ToString()!;
        }

        [HotPath(NoAlloc = true)]
        private static string JoinPair(string a, string b)
        {
            return a + b;
        }

        [HotPath(NoAlloc = true)]
        private static string ChopFirst(string s)
        {
            return s.Substring(1);
        }

        [HotPath(NoAlloc = true)]
        private static double CornerSum(int n)
        {
            var grid = new double[n, n];
            return grid[0, 0] + grid[n - 1, n - 1];
        }

        private static void Main()
        {
            Console.WriteLine(StringifyBox(42));
            Console.WriteLine(JoinPair("a", "b"));
            Console.WriteLine(ChopFirst("abc"));
            Console.WriteLine(CornerSum(3));
        }
    }
}

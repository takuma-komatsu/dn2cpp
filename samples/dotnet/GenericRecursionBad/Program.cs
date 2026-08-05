using System;
using System.Collections.Generic;

namespace GenericRecursionBad;

/// <summary>The classic AOT-hostile shape: a generic method that calls itself at a
/// strictly deeper type argument. A JIT is fine with it — the depth is bounded at
/// run time by <c>n</c>, and instantiations are created lazily as they are reached.
/// An ahead-of-time compiler cannot see <c>n</c>: it must monomorphize the call
/// graph, and this one names Descend&lt;int&gt;, Descend&lt;List&lt;int&gt;&gt;,
/// Descend&lt;List&lt;List&lt;int&gt;&gt;&gt;, … without end.
///
/// Transpiling this must therefore FAIL, loudly and quickly, on the transpiler's
/// monomorphization bound — not run the machine out of memory. That is what the
/// shared-generics gate asserts; the program is never built or run.</summary>
internal static class Program
{
    private static void Descend<T>(int n)
    {
        if (n <= 0)
            return;
        Descend<List<T>>(n - 1);
    }

    private static void Main()
    {
        Descend<int>(3);
        Console.WriteLine("unreachable for an AOT compiler");
    }
}

using System;
using System.Linq.Expressions;
using System.Reflection.Emit;

namespace DynamicCodegenSubset
{
    // The dynamic-code-generation surface (Reflection.Emit, DLR CallSite,
    // Expression.Compile) is statically cut and throws a catchable
    // PlatformNotSupportedException at run time — the NativeAOT posture.
    // Expression TREE BUILDING stays fully transpiled: libraries build trees
    // for metadata purposes without ever compiling them. This section
    // intentionally diverges from real .NET (whose JIT compiles both), hence
    // the frozen snapshot.
    internal static class Program
    {
        internal static void Run()
        {
            // Tree building + structural reads work — the surface cut stops at
            // the tree->delegate step, not at the tree. (An identity lambda:
            // richer node factories like Expression.Add run operator-resolution
            // reflection dn2cpp doesn't cover yet — a separate, recorded gap.)
            ParameterExpression p = Expression.Parameter(typeof(int), "x");
            Expression<Func<int, int>> lambda = Expression.Lambda<Func<int, int>>(p, p);
            Console.WriteLine(lambda.NodeType);           // Lambda
            Console.WriteLine(lambda.Parameters.Count);   // 1
            Console.WriteLine(lambda.Body.NodeType);      // Parameter

            // Compile() throws the precise type, with the member in the message.
            try
            {
                lambda.Compile();
                Console.WriteLine("compiled");
            }
            catch (PlatformNotSupportedException e)
            {
                Console.WriteLine(e.GetType().Name);
                Console.WriteLine(e.Message);
            }

            // ...and is catchable as the NotSupportedException base (PNSE : NSE).
            try
            {
                lambda.Compile();
                Console.WriteLine("compiled");
            }
            catch (NotSupportedException)
            {
                Console.WriteLine("caught as NotSupportedException");
            }

            // Reflection.Emit object construction throws the same way.
            try
            {
                var dm = new DynamicMethod("m", typeof(int), Type.EmptyTypes);
                Console.WriteLine(dm.Name);
            }
            catch (PlatformNotSupportedException e)
            {
                Console.WriteLine(e.GetType().Name);
            }
        }
    }
}

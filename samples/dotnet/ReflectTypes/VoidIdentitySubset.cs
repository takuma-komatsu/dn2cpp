using System;
using System.Linq.Expressions;
using System.Reflection;

namespace VoidIdentitySubset
{
    // System.Void must be ONE Type handle across its two mint routes —
    // typeof(void) (the ldtoken route) and a method signature's return type
    // (the reflection-table route, MemberTypeInfoExpr). When the routes
    // diverge, Expression.Lambda over a void-returning delegate rejects a
    // value-returning body ("Expression of type 'System.String' cannot be
    // used for return type 'System.Void'"): its validation skips the body
    // type check only when the Invoke row's return type == typeof(void), and
    // that comparison is Type identity. Every line matches real .NET.
    internal static class Program
    {
        public static string Describe(int n) => "n=" + n;

        public static void VoidWork() { }

        internal static void Run()
        {
            // The two routes must agree by identity, not just by name.
            MethodInfo voidMi = typeof(Program).GetMethod(nameof(VoidWork))!;
            Console.WriteLine(ReferenceEquals(typeof(void), voidMi.ReturnType)); // True
            Console.WriteLine(typeof(void) == voidMi.ReturnType);                // True
            Console.WriteLine(voidMi.ReturnType.Equals(typeof(void)));           // True
            Console.WriteLine(voidMi.ReturnType.FullName);                       // System.Void

            // A delegate's Invoke row is the same reflection-table route — this
            // is the handle Expression.Lambda validates against.
            MethodInfo invokeMi = typeof(Action<int, object>).GetMethod("Invoke")!;
            Console.WriteLine(invokeMi.ReturnType == typeof(void));              // True

            // The observed form: a void-returning delegate discards the body's
            // value, so real .NET accepts a value-returning body.
            ParameterExpression pn = Expression.Parameter(typeof(int), "n");
            ParameterExpression po = Expression.Parameter(typeof(object), "o");
            MethodInfo dm = typeof(Program).GetMethod(nameof(Describe))!;
            MethodCallExpression body = Expression.Call(dm, pn);
            Expression<Action<int, object>> lam =
                Expression.Lambda<Action<int, object>>(body, pn, po);
            Console.WriteLine(lam.NodeType);        // Lambda
            Console.WriteLine(lam.ReturnType.Name); // Void
            Console.WriteLine(lam.Body.Type.Name);  // String
        }
    }
}

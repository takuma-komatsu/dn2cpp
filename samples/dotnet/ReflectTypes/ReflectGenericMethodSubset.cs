#nullable enable
using System;
using System.Reflection;

namespace ReflectGenericMethodSubset
{
    // The intentionally-divergent (AOT-boundary) half of the generic-method
    // reflection dimension; the real-.NET-parity half lives in the
    // reflect-invoke diff gate. Divergences frozen here (methtab rows are
    // per-closed-instantiation — the image has no open generic method rows):
    //  - GetMethod surfaces a CLOSED instantiation, so IsGenericMethodDefinition
    //    answers false where real .NET (open definition) answers true, and
    //    GetGenericArguments reports the closed arguments (Int32, not T). The
    //    same holds for the GetGenericMethodDefinition view, whose arguments
    //    stay closed, and ContainsGenericParameters stays false on it.
    //  - MakeGenericMethod over an instantiation the transpile never reached
    //    throws the catchable PlatformNotSupportedException (real .NET JITs it).
    //  - MakeGenericMethod re-resolving the in-image instantiation returns the
    //    same row the lookup surfaced, so it compares EQUAL to the GetMethod
    //    result (real .NET: open definition != closed method).
    static class GmDiverge
    {
        public static string Pick<T>() => typeof(T).Name;
    }

    static class Program
    {
        internal static void Run()
        {
            // The one statically reached instantiation.
            Console.WriteLine($"gm-direct => {GmDiverge.Pick<int>()}");

            MethodInfo pick = typeof(GmDiverge).GetMethod("Pick")!;
            Console.WriteLine($"gm-lookup-isdef => {pick.IsGenericMethodDefinition}");
            Console.WriteLine($"gm-lookup-args => {pick.GetGenericArguments()[0].Name}");

            MethodInfo def = pick.GetGenericMethodDefinition();
            Console.WriteLine($"gm-defview-isdef => {def.IsGenericMethodDefinition}");
            Console.WriteLine($"gm-defview-args => {def.GetGenericArguments()[0].Name}");
            Console.WriteLine($"gm-defview-open => {def.ContainsGenericParameters}");

            MethodInfo remade = pick.MakeGenericMethod(typeof(int));
            Console.WriteLine($"gm-remake-eq-lookup => {remade == pick}");

            try
            {
                pick.MakeGenericMethod(typeof(double));
                Console.WriteLine("gm-unreached => no exception");
            }
            catch (PlatformNotSupportedException)
            {
                Console.WriteLine("gm-unreached => PlatformNotSupportedException");
            }
        }
    }
}

#nullable enable
using System;
using System.Reflection;

namespace ReflectGenericMethodSubset
{
    // Definition queries follow CLR. MakeGenericMethod can resolve only compiled
    // instantiations; the live parity coverage is in the reflect-invoke bucket.
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

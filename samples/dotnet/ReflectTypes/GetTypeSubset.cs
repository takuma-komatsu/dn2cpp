using System;
using System.Collections.Generic;

namespace GetTypeSubset
{
    internal class Animal
    {
    }

    internal sealed class Dog : Animal
    {
    }

    // Object.GetType returns the runtime Type from the object header, so
    // GetType.Name (the simple name) and `x.GetType == typeof(Y)` runtime type
    // checks work — even through a base reference and on boxed primitives.
    internal static class Program
    {
        private static string Describe(object o) => o.GetType().Name;

        internal static void Run()
        {
            object d = new Dog();
            Console.WriteLine(d.GetType().Name);              // Dog

            Animal a = new Dog();
            Console.WriteLine(a.GetType().Name);              // Dog (runtime, not Animal)
            Console.WriteLine(a.GetType() == typeof(Dog));    // True
            Console.WriteLine(a.GetType() == typeof(Animal)); // False

            object n = 42;
            Console.WriteLine(n.GetType().Name);              // Int32

            Console.WriteLine("hi".GetType().Name);           // String
            Console.WriteLine(Describe(new Animal()));        // Animal
            Console.WriteLine(typeof(Dog).Name);              // Dog (Type.Name)

            // Reference identity: the runtime interns one Type object per type,
            // so typeof/GetType return the *same* object every time (as in .NET) —
            // what lock(typeof(X)) and identity-keyed caches rely on.
            Console.WriteLine(ReferenceEquals(typeof(Dog), typeof(Dog)));        // True
            Console.WriteLine(ReferenceEquals(a.GetType(), d.GetType()));        // True (same runtime type)
            Console.WriteLine(ReferenceEquals(a.GetType(), typeof(Dog)));        // True
            Console.WriteLine(ReferenceEquals(typeof(Dog), typeof(Animal)));     // False
            Console.WriteLine(ReferenceEquals(typeof(List<int>), typeof(List<int>)));    // True
            Console.WriteLine(ReferenceEquals(typeof(List<int>), typeof(List<string>))); // False

            // ...including the statically-embedded companions of every type-info
            // kind: enums, arrays, and the runtime's built-in (primitive/string) tis.
            Console.WriteLine(ReferenceEquals(typeof(DayOfWeek), typeof(DayOfWeek)));    // True
            int[] arr = new int[1];
            Console.WriteLine(ReferenceEquals(arr.GetType(), typeof(int[])));            // True
            Console.WriteLine(ReferenceEquals(((object)42).GetType(), typeof(int)));     // True
            Console.WriteLine(ReferenceEquals("a".GetType(), typeof(string)));           // True
            Console.WriteLine(ReferenceEquals(typeof(List<>), typeof(List<>)));          // True (generic definition)
        }
    }
}

#nullable enable
using System;
using System.Collections.Generic;

// Guid comparison and hashing through the real CoreLib IL over fixed
// literals: operators, Equals (typed + boxed), CompareTo orderings and
// Array.Sort, raw GetHashCode output (a pure field XOR — identical across
// runtimes, so printing it exactly is safe), default(Guid) == Guid.Empty, and
// Guid as a Dictionary / HashSet key with fixed insertion order.
namespace GuidCompareSubset;

static class Program
{
    internal static void __GateEntry()
    {
        Guid a = new Guid("0f8fad5b-d9cb-469f-a165-70867728950e");
        Guid a2 = new Guid("0f8fad5b-d9cb-469f-a165-70867728950e");
        Guid b = new Guid("7c9e6679-7425-40de-944b-e07fc1f90ae7");
        Guid c = new Guid("00000000-0000-0000-0000-000000000001");

        Console.WriteLine("-- operators / Equals --");
        Console.WriteLine(a == a2);
        Console.WriteLine(a != b);
        Console.WriteLine(a.Equals(a2));
        Console.WriteLine(a.Equals(b));
        Console.WriteLine(a.Equals((object)a2));
        Console.WriteLine(a.Equals((object)"not a guid"));
        Console.WriteLine(default(Guid) == Guid.Empty);

        Console.WriteLine("-- CompareTo / Sort --");
        Console.WriteLine(Math.Sign(a.CompareTo(b)));
        Console.WriteLine(Math.Sign(b.CompareTo(a)));
        Console.WriteLine(a.CompareTo(a2));
        Console.WriteLine(Math.Sign(a.CompareTo((object)b)));
        Console.WriteLine(Math.Sign(Guid.Empty.CompareTo(c)));
        // Ordering via CompareTo over a hand-rolled insertion sort —
        // Array.Sort<T> is an intrinsic limited to int/long/double/string
        // elements (MethodCompiler.GenericIntrinsic.cs), so a struct element
        // like Guid stays outside it; the ordering semantics are what this
        // section is after.
        Guid[] arr = { b, a, Guid.Empty, c };
        for (int i = 1; i < arr.Length; i++)
        {
            Guid key = arr[i];
            int j = i - 1;
            while (j >= 0 && arr[j].CompareTo(key) > 0)
            {
                arr[j + 1] = arr[j];
                j--;
            }
            arr[j + 1] = key;
        }
        foreach (Guid g in arr)
            Console.WriteLine(g);

        Console.WriteLine("-- GetHashCode (pure field XOR, runtime-stable) --");
        Console.WriteLine(a.GetHashCode());
        Console.WriteLine(a.GetHashCode() == a2.GetHashCode());
        Console.WriteLine(Guid.Empty.GetHashCode());

        Console.WriteLine("-- collection keys --");
        var dict = new Dictionary<Guid, string> { [a] = "alpha", [b] = "beta" };
        Console.WriteLine(dict[a]);
        Console.WriteLine(dict[new Guid("7c9e6679-7425-40de-944b-e07fc1f90ae7")]);
        Console.WriteLine(dict.ContainsKey(c));
        Console.WriteLine(dict.Count);
        var set = new HashSet<Guid> { a, a2, b, Guid.Empty };
        Console.WriteLine(set.Count);
        Console.WriteLine(set.Contains(new Guid("0f8fad5b-d9cb-469f-a165-70867728950e")));
    }
}

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

// compiler-generated nested types (iterator / async state machines,
// closures) carry only their simple name (e.g. <Seq>d__0), so two same-named
// methods in different declaring types would emit colliding C++ identifiers.
// Qualifying the C++ names with the enclosing type chain disambiguates them.
namespace NsA
{
    internal static class Worker
    {
        public static IEnumerable<int> Seq()
        {
            yield return 1;
            yield return 2;
        }

        public static async Task<int> Compute()
        {
            await Task.Yield();
            return 100;
        }

        // A closure (<>c__DisplayClass) capturing a local.
        public static Func<int, int> Adder(int n) => x => x + n;
    }
}

namespace NsB
{
    internal static class Worker
    {
        public static IEnumerable<int> Seq()
        {
            yield return 10;
            yield return 20;
        }

        public static async Task<int> Compute()
        {
            await Task.Yield();
            return 200;
        }

        public static Func<int, int> Adder(int n) => x => x + n * 10;
    }
}

namespace NestedNameSubset
{
    internal static class Program
    {
        internal static void Run()
        {
            int sum = 0;
            foreach (int x in NsA.Worker.Seq())
            {
                sum += x;
            }
            foreach (int x in NsB.Worker.Seq())
            {
                sum += x;
            }
            Console.WriteLine(sum); // 1+2 + 10+20 = 33

            Console.WriteLine(NsA.Worker.Compute().Result + NsB.Worker.Compute().Result); // 300

            Console.WriteLine(NsA.Worker.Adder(5)(1) + NsB.Worker.Adder(5)(1)); // (1+5) + (1+50) = 57
        }
    }
}

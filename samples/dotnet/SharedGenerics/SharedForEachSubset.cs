// A lowered foreach inside a shared generic body — the enumeration loop of
// Task.WhenAny/WhenAll over an IEnumerable<Task<T>> and of string.Join/Concat over an
// IEnumerable<T> — resolves each interface method through the fully canonical
// interface the receivers' alias rows carry, and a class implementing two interfaces
// that collapse onto that form keeps per-instantiation bodies, so each instantiation
// enumerates the view it names.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SharedForEachSubset
{
    internal sealed class TwoViews : IEnumerable<string>, IEnumerable<object>
    {
        IEnumerator<string> IEnumerable<string>.GetEnumerator()
        {
            yield return "s1";
            yield return "s2";
        }

        IEnumerator<object> IEnumerable<object>.GetEnumerator()
        {
            yield return "o1";
        }

        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable<string>)this).GetEnumerator();
    }

    internal static class Program
    {
        private static Task<Task<T>> Any<T>(IEnumerable<Task<T>> tasks) => Task.WhenAny(tasks);

        private static Task<T[]> All<T>(IEnumerable<Task<T>> tasks) => Task.WhenAll(tasks);

        private static string Join<T>(IEnumerable<T> items) => string.Join(",", items);

        private static string Concat<T>(IEnumerable<T> items) => string.Concat(items);

        internal static void Run()
        {
            var words = new List<Task<string>> { Task.FromResult("first"), Task.FromResult("second") };
            var boxes = new List<Task<object>> { Task.FromResult<object>("boxed") };
            Console.WriteLine("shared foreach any=" + Any(words).Result.Result + "/" + Any(boxes).Result.Result);
            Console.WriteLine("shared foreach all=" + string.Join("+", All(words).Result) + "/" + All(boxes).Result[0]);
            var views = new TwoViews();
            Console.WriteLine("shared foreach views=" + Join<string>(views) + "/" + Join<object>(views) + "/"
                + Concat<string>(views) + "/" + Concat<object>(views));
        }
    }
}

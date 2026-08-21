using System;
using System.Collections.Generic;
using ZLinq;

namespace ZLinqAdvancedSubset
{
    internal sealed class Person
    {
        public string Name { get; set; }
        public int Age { get; set; }
        public string Dept { get; set; }
    }

    internal static class Program
    {
        internal static void __GateEntry()
        {
            OrderingAndReverse();
            SetOperations();
            GroupingAndJoining();
            FlattenAndChunk();
            CollectionConversions();
        }

        private static void OrderingAndReverse()
        {
            var people = new Person[]
            {
                new Person { Name = "Alice", Age = 30, Dept = "Engineering" },
                new Person { Name = "Bob", Age = 25, Dept = "Engineering" },
                new Person { Name = "Charlie", Age = 30, Dept = "Marketing" },
                new Person { Name = "Dave", Age = 20, Dept = "Marketing" }
            };

            var ordered = people.AsValueEnumerable()
                .OrderBy(p => p.Age)
                .ThenBy(p => p.Name)
                .Select(p => p.Name + ":" + p.Age)
                .ToArray();
            Console.WriteLine("[order] " + string.Join(",", ordered));

            var reversed = new int[] { 1, 2, 3, 4, 5 }.AsValueEnumerable()
                .Reverse()
                .ToArray();
            Console.WriteLine("[reverse] " + string.Join(",", reversed));
        }

        private static void SetOperations()
        {
            int[] first = new int[] { 1, 2, 2, 3, 4, 5 };
            int[] second = new int[] { 3, 4, 5, 6, 7 };

            var distinct = first.AsValueEnumerable().Distinct().ToArray();
            var union = first.AsValueEnumerable().Union(second).ToArray();
            var intersect = first.AsValueEnumerable().Intersect(second).ToArray();
            var except = first.AsValueEnumerable().Except(second).ToArray();

            Console.WriteLine("[distinct] " + string.Join(",", distinct));
            Console.WriteLine("[union] " + string.Join(",", union));
            Console.WriteLine("[intersect] " + string.Join(",", intersect));
            Console.WriteLine("[except] " + string.Join(",", except));
        }

        private static void GroupingAndJoining()
        {
            string[] words = new string[] { "ant", "bear", "ape", "cat", "bat", "bee" };
            foreach (var g in words.AsValueEnumerable().OrderBy(w => w).GroupBy(w => w[0]))
            {
                Console.WriteLine("[group] key=" + g.Key + " items=" + string.Join(",", g.AsValueEnumerable().ToArray()));
            }

            var ids = new int[] { 1, 2, 3 };
            var names = new (int Id, string Name)[]
            {
                (1, "One"),
                (2, "Two"),
                (3, "Three")
            };

            var joined = ids.AsValueEnumerable()
                .Join(names, id => id, n => n.Id, (id, n) => id + "=" + n.Name)
                .ToArray();
            Console.WriteLine("[join] " + string.Join(",", joined));

            var zipped = new int[] { 10, 20, 30 }.AsValueEnumerable()
                .Zip(new string[] { "X", "Y", "Z" }, (n, s) => n + s)
                .ToArray();
            Console.WriteLine("[zip] " + string.Join(",", zipped));
        }

        private static void FlattenAndChunk()
        {
            var nested = new int[][]
            {
                new int[] { 1, 2 },
                new int[] { 3, 4 },
                new int[] { 5 }
            };
            var flattened = nested.AsValueEnumerable()
                .SelectMany(arr => arr)
                .ToArray();
            Console.WriteLine("[select-many] " + string.Join(",", flattened));

            var items = new int[] { 1, 2, 3, 4, 5, 6, 7 };
            int chunkIdx = 0;
            foreach (var chunk in items.AsValueEnumerable().Chunk(3))
            {
                Console.WriteLine("[chunk] " + chunkIdx++ + ":" + string.Join(",", chunk));
            }
        }

        private static void CollectionConversions()
        {
            int[] src = new int[] { 1, 2, 3, 2, 1 };
            List<int> list = src.AsValueEnumerable().ToList();
            HashSet<int> set = src.AsValueEnumerable().ToHashSet();
            Dictionary<int, string> dict = set.AsValueEnumerable().ToDictionary(x => x, x => "val" + x);

            Console.WriteLine("[conv] list=" + list.Count + " set=" + set.Count + " dict=" + dict.Count + ":" + dict[2]);
        }
    }
}

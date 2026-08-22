using System;
using System.Collections.Generic;
using ZLinq;

namespace ZLinqCoreSubset
{
    internal static class Program
    {
        internal static void __GateEntry()
        {
            BasicPipeline();
            SlicingAndPredicates();
            Aggregations();
            ElementLookups();
            QuantifiersAndSpans();
        }

        private static void BasicPipeline()
        {
            int[] src = new int[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
            int sum = src.AsValueEnumerable()
                .Where(x => (x & 1) == 0)
                .Select(x => x * 10)
                .Sum();
            Console.WriteLine("[basic] evenSum=" + sum);

            var indexed = src.AsValueEnumerable()
                .Select((x, i) => i + ":" + x)
                .Take(4)
                .ToArray();
            Console.WriteLine("[indexed] " + string.Join(",", indexed));
        }

        private static void SlicingAndPredicates()
        {
            int[] src = new int[] { 1, 2, 3, 4, 5, 1, 2, 3 };
            var taken = src.AsValueEnumerable()
                .TakeWhile(x => x < 5)
                .ToArray();
            Console.WriteLine("[take-while] " + string.Join(",", taken));

            var skipped = src.AsValueEnumerable()
                .SkipWhile(x => x < 4)
                .ToArray();
            Console.WriteLine("[skip-while] " + string.Join(",", skipped));

            var sliced = src.AsValueEnumerable()
                .Skip(2)
                .Take(3)
                .ToArray();
            Console.WriteLine("[slice] " + string.Join(",", sliced));
        }

        private static void Aggregations()
        {
            int[] src = new int[] { 3, 1, 4, 1, 5, 9, 2, 6 };
            int count = src.AsValueEnumerable().Count();
            int min = src.AsValueEnumerable().Min();
            int max = src.AsValueEnumerable().Max();
            double avg = src.AsValueEnumerable().Average();
            int folded = src.AsValueEnumerable().Aggregate(100, (acc, x) => acc - x);
            Console.WriteLine("[aggr] count=" + count + " min=" + min + " max=" + max + " avg=" + avg + " fold=" + folded);
        }

        private static void ElementLookups()
        {
            int[] src = new int[] { 10, 20, 30, 40, 50 };
            int first = src.AsValueEnumerable().First();
            int firstEven = src.AsValueEnumerable().First(x => x > 25);
            int def = src.AsValueEnumerable().FirstOrDefault(x => x > 100);
            int last = src.AsValueEnumerable().Last();
            int elem2 = src.AsValueEnumerable().ElementAt(2);
            int single = new int[] { 99 }.AsValueEnumerable().Single();
            int singleDef = src.AsValueEnumerable().SingleOrDefault(x => x == 30);
            Console.WriteLine("[elem] first=" + first + " firstEven=" + firstEven + " def=" + def
                + " last=" + last + " elem2=" + elem2 + " single=" + single + " singleDef=" + singleDef);
        }

        private static void QuantifiersAndSpans()
        {
            int[] src = new int[] { 2, 4, 6, 8 };
            bool allEven = src.AsValueEnumerable().All(x => (x & 1) == 0);
            bool anyOverFive = src.AsValueEnumerable().Any(x => x > 5);
            bool hasSix = src.AsValueEnumerable().Contains(6);
            bool seqEqual = src.AsValueEnumerable().SequenceEqual(new int[] { 2, 4, 6, 8 }.AsValueEnumerable());
            Console.WriteLine("[quant] allEven=" + allEven + " any5=" + anyOverFive + " has6=" + hasSix + " eq=" + seqEqual);

            List<string> list = new List<string> { "alpha", "beta", "gamma" };
            string joined = string.Join(";", list.AsValueEnumerable().Select(s => s.ToUpperInvariant()).ToArray());
            Console.WriteLine("[list-enum] " + joined);
        }
    }
}

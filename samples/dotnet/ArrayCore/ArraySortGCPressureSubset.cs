#nullable enable
using System;
using System.Collections.Generic;

namespace ArraySortGCPressureSubset
{
    // Array.Sort / span Sort over REFERENCE elements with a managed comparer that
    // allocates: mid-merge the sort implementation may hold elements only in its
    // temporary buffer, so the comparer periodically forces a full collection and
    // an allocation storm (reusing any block freed by mistake). All keys are
    // distinct, so the sorted order is unique and diffs exact vs real .NET (whose
    // introsort is unstable only across EQUAL keys).
    internal static class Program
    {
        private sealed class Item
        {
            public int V;
            public string Tag = "";
        }

        private static object? s_occupy; // keeps the allocation storms reachable

        private sealed class Blob
        {
            public object? A;
            public object? B;
        }

        private static long Stomp(int depth)
        {
            if (depth <= 0)
                return 1;
            long a = depth;
            long b = depth * 2;
            long c = depth * 3;
            long d = depth * 4;
            return a + b + c + d + Stomp(depth - 1);
        }

        // Collect, then reuse any freed small block so a missing GC root becomes
        // observable corruption instead of a silent free.
        private static void Churn()
        {
            long scrub = Stomp(96);
            GC.Collect();
            for (int j = 0; j < 2048; j++)
            {
                var b = new Blob();
                b.A = s_occupy;
                b.B = scrub;
                s_occupy = b;
            }
        }

        private sealed class ChurnComparer : IComparer<Item>
        {
            public int Calls;

            public int Compare(Item? x, Item? y)
            {
                if (++Calls % 64 == 0)
                    Churn();
                return x!.V.CompareTo(y!.V);
            }
        }

        private static Item[] Build(int n)
        {
            var items = new Item[n];
            for (int i = 0; i < n; i++)
                items[i] = new Item { V = i, Tag = "t" + i };
            // Deterministic LCG shuffle (identical on both sides).
            uint state = 12345;
            for (int i = n - 1; i > 0; i--)
            {
                state = state * 1664525u + 1013904223u;
                int j = (int)(state % (uint)(i + 1));
                (items[i], items[j]) = (items[j], items[i]);
            }
            return items;
        }

        private static void Check(string tag, Item[] items)
        {
            int bad = 0;
            long sum = 0;
            for (int i = 0; i < items.Length; i++)
            {
                if (items[i].V != i || items[i].Tag != "t" + i)
                    bad++;
                sum += i * items[i].V;
            }
            Console.WriteLine(tag + " bad=" + bad + " sum=" + sum + " first=" + items[0].Tag
                + " last=" + items[items.Length - 1].Tag);
        }

        internal static void Run()
        {
            const int N = 257;

            // Array.Sort(T[], IComparer<T>) — the comparer-driven reference path.
            var a = Build(N);
            var cmp = new ChurnComparer();
            Array.Sort(a, cmp);
            Check("sortcmp", a);
            Console.WriteLine("sortcmp churned=" + (cmp.Calls >= N));

            // Span Sort with a Comparison<T> over the same reference elements.
            var b = Build(N);
            int calls = 0;
            b.AsSpan().Sort((x, y) =>
            {
                if (++calls % 64 == 0)
                    Churn();
                return x.V.CompareTo(y.V);
            });
            Check("spancmp", b);
        }
    }
}

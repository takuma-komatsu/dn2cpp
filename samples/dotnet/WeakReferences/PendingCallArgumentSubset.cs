using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace PendingCallArgumentSubset
{
    // The graph returned by Produce remains only on the IL evaluation stack while
    // CollectAndProbe evaluates the next argument. A conforming collector must see
    // that pending reference before Consume begins using the whole graph.
    internal static class Program
    {
        private const int EntryCount = 32;
        private static WeakReference s_pending;

        private static string Key(int index)
        {
            return "key-" + index;
        }

        private static string Value(int index)
        {
            return new string((char)('a' + index % 20), 24) + ":" + index;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static Dictionary<string, string> Produce()
        {
            var graph = new Dictionary<string, string>();
            for (int i = 0; i < EntryCount; i++)
                graph.Add(Key(i), Value(i));
            s_pending = new WeakReference(graph);
            return graph;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static bool CollectAndProbe()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            return s_pending.IsAlive;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void Consume(Dictionary<string, string> graph, bool aliveDuringCollect)
        {
            bool complete = graph.Count == EntryCount;
            int totalLength = 0;
            for (int i = 0; i < EntryCount; i++)
            {
                string expected = Value(i);
                if (!graph.TryGetValue(Key(i), out string actual) || actual != expected)
                {
                    complete = false;
                    continue;
                }
                totalLength += actual.Length;
            }
            Console.WriteLine("pending nested call: alive=" + aliveDuringCollect
                + " complete=" + complete + " length=" + totalLength);
        }

        internal static void __GateEntry()
        {
            Consume(Produce(), CollectAndProbe());
        }
    }
}

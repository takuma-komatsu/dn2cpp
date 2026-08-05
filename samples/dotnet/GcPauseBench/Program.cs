using System;

namespace GcPauseBench
{
    // A small allocation-pressure workload for the GC real-time spike. It holds a
    // large long-lived object graph (so the mark phase has real work) while
    // churning short-lived garbage and occasionally mutating the live set (so an
    // incremental/generational collector has dirty pages to re-scan). The pause
    // numbers come from the runtime's DN2CPP_GC_STATS instrumentation, not from
    // here — see gates/measure-gcpause.sh. MVP-level only (no -r CoreLib needed).
    internal sealed class Node
    {
        public int Value;
        public Node? Left;
        public Node? Right;
    }

    internal static class Program
    {
        // Build a balanced binary tree of the given depth (the retained live set).
        private static Node Build(int depth, int value)
        {
            Node n = new Node();
            n.Value = value;
            if (depth > 0)
            {
                n.Left = Build(depth - 1, value * 2);
                n.Right = Build(depth - 1, value * 2 + 1);
            }
            return n;
        }

        // Masked sum over the live tree; the mask keeps the running value positive
        // and bounded so there is no signed-overflow UB and the result is a stable
        // anti-dead-code-elimination marker.
        private static int Sum(Node n)
        {
            int s = n.Value & 0xFFFFFF;
            if (n.Left != null)
            {
                s = (s + Sum(n.Left)) & 0xFFFFFF;
            }
            if (n.Right != null)
            {
                s = (s + Sum(n.Right)) & 0xFFFFFF;
            }
            return s;
        }

        private static int Main()
        {
            // depth 22 -> ~8.4M nodes -> a few hundred MB of long-lived references
            // for the mark phase to traverse. A full stop-the-world mark over this
            // heap blows a 16 ms frame budget — which is exactly the real-time pain
            // incremental mode aims to chop into bounded increments.
            int depth = 22;
            Node root = Build(depth, 1);

            int acc = 0;
            int iterations = 1500;
            int garbagePerIter = 2048;
            Node cursor = root;

            for (int i = 0; i < iterations; i++)
            {
                // Transient reference array of fresh nodes -> immediate garbage.
                Node[] garbage = new Node[garbagePerIter];
                int j = 0;
                while (j < garbage.Length)
                {
                    Node t = new Node();
                    t.Value = i ^ j;
                    garbage[j] = t;
                    acc = (acc + t.Value) & 0xFFFFFF;
                    j = j + 1;
                }
                // Touch one slot so the array cannot be optimized away.
                acc = (acc + garbage[(i * 131) & (garbagePerIter - 1)].Value) & 0xFFFFFF;

                // Mutate the live set: splice in a fresh small subtree (abandoning
                // the old one) and walk the cursor, dirtying scattered pages.
                if (cursor.Left != null)
                {
                    cursor.Left = Build(4, i);
                    cursor = cursor.Right != null ? cursor.Right : root;
                }
                else
                {
                    cursor = root;
                }
            }

            int checksum = (Sum(root) + acc) & 0xFFFFFF;
            Console.WriteLine("GcPauseBench done");
            Console.WriteLine(checksum);
            return 0;
        }
    }
}

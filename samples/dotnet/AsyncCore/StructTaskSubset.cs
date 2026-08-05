#nullable enable
using System;
using System.Threading.Tasks;

namespace StructTaskSubset
{
    // struct Task<T> results. A value-type result does not fit the 8-byte task
    // result slot, so SetResult/FromResult heap-box the struct and the slot holds the
    // pointer; awaiting /.Result / the WhenAll<TStruct> join copy it back out by T.
    // Covers a plain struct, a struct with a reference field, a value tuple, the
    // Task.FromResult shortcut, and Task.WhenAll<TStruct> -> TStruct[].
    internal readonly struct Pt
    {
        public readonly int X;
        public readonly long Y;
        public Pt(int x, long y) { X = x; Y = y; }
    }

    internal readonly struct Lbl
    {
        public readonly int N;
        public readonly string S;
        public Lbl(int n, string s) { N = n; S = s; }
    }

    internal static class Program
    {
        private static async Task<Pt> MakePt(int x, long y) { await Task.Yield(); return new Pt(x, y); }
        private static async Task<Lbl> MakeLbl(int n, string s) { await Task.Yield(); return new Lbl(n, s); }
        private static async Task<(int, long)> Pair(int a, long b) { await Task.Yield(); return (a, b); }

        private static async Task<string> Run()
        {
            Pt p = await MakePt(7, 90);                       // suspend then resume with a struct
            Lbl l = await MakeLbl(3, "hi");                   // struct carrying a reference field
            (int, long) t = await Pair(4, 5);                // value tuple result
            Pt fr = await Task.FromResult(new Pt(1, 2));      // pre-completed struct task

            // WhenAll<TStruct> -> TStruct[] (each input's boxed struct copied into a
            // value array). Sum is order-independent.
            Pt[] all = await Task.WhenAll(MakePt(1, 10), MakePt(2, 20), MakePt(3, 30));
            long allSum = 0;
            foreach (var e in all) allSum += e.X + e.Y;      // 1+10+2+20+3+30 = 66

            return p.X + "," + p.Y + "," + l.N + l.S + "," + t.Item1 + "," + t.Item2 + ","
                 + fr.X + "," + fr.Y + "," + all.Length + "," + allSum;
        }

        internal static void __GateEntry()
        {
            Console.WriteLine(Run().Result);                 // 7,90,3hi,4,5,1,2,3,66
        }
    }
}

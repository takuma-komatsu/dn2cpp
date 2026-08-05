#nullable enable
using System;

namespace ArrayReflectionSubset
{
    // Non-generic Array reflection surface: GetValue/SetValue (with real
    // .NET's coercion rules — primitive widening, enum/Nullable exactness,
    // null -> default), Array.CreateInstance (SZ + multi-dimensional, incl.
    // an element type whose T[] is never statically instantiated), the shape
    // queries on a System.Array-typed receiver, and non-generic Reverse.
    // Every line is verified against real .NET by the live diff (exception
    // TYPES printed, not messages — trap messages are not modeled).
    enum IntE { A, B, C, D, E, F }
    enum OtherIntE { X, Y, Z }
    enum ByteE : byte { P = 1, Q = 2 }
    enum LongE : long { L1 = 1, L2 = 0x1_0000_0001 }

    struct Pt
    {
        public int X;
        public int Y;
        public Pt(int x, int y) { X = x; Y = y; }
        public override string ToString() => $"Pt({X},{Y})";
    }

    class Widget
    {
        public override string ToString() => "Widget";
    }

    static class Program
    {
        static void Try(string label, Action a)
        {
            try { a(); Console.WriteLine($"{label}: OK"); }
            catch (Exception e) { Console.WriteLine($"{label}: {e.GetType().Name}"); }
        }

        internal static void Run()
        {
            Console.WriteLine("== SetValue widening ==");
            long[] longs = new long[3];
            longs.SetValue(42, 0);
            longs.SetValue((short)-7, 1);
            longs.SetValue((byte)8, 2);
            Console.WriteLine($"long[] {longs[0]} {longs[1]} {longs[2]}");
            double[] doubles = new double[2];
            doubles.SetValue(1.5f, 0);
            doubles.SetValue(3, 1);
            Console.WriteLine($"double[] {doubles[0]} {doubles[1]}");
            float[] floats = new float[2];
            floats.SetValue(long.MaxValue, 0);
            floats.SetValue((sbyte)-3, 1);
            Console.WriteLine($"float[] {floats[0]} {floats[1]}");
            int[] ints = new int[3];
            ints.SetValue('A', 0);
            ints.SetValue((ushort)9, 1);
            ints.SetValue(IntE.F, 2);
            Console.WriteLine($"int[] {ints[0]} {ints[1]} {ints[2]}");
            char[] chars = new char[2];
            chars.SetValue((byte)66, 0);
            chars.SetValue((ushort)67, 1);
            Console.WriteLine($"char[] {chars[0]}{chars[1]}");
            long[] fromU = new long[2];
            fromU.SetValue(uint.MaxValue, 0);
            fromU.SetValue(LongE.L2, 1);
            Console.WriteLine($"u->long[] {fromU[0]} {fromU[1]}");
            bool[] bools = new bool[1];
            bools.SetValue(true, 0);
            Console.WriteLine($"bool[] {bools[0]}");
            short[] shorts = new short[2];
            shorts.SetValue((sbyte)-5, 0);
            shorts.SetValue((byte)200, 1);
            Console.WriteLine($"short[] {shorts[0]} {shorts[1]}");

            Console.WriteLine("== SetValue rejections ==");
            Try("long->int[]", () => new int[1].SetValue(42L, 0));
            Try("int->short[]", () => new short[1].SetValue(42, 0));
            Try("double->float[]", () => new float[1].SetValue(1.5, 0));
            Try("sbyte->ushort[]", () => new ushort[1].SetValue((sbyte)3, 0));
            Try("bool->int[]", () => new int[1].SetValue(true, 0));
            Try("int->uint[]", () => new uint[1].SetValue(5, 0));
            Try("uint->int[]", () => new int[1].SetValue(5u, 0));
            Try("int->ulong[]", () => new ulong[1].SetValue(5, 0));
            Try("string->int[]", () => new int[1].SetValue("x", 0));
            Try("object->string[]", () => new string[1].SetValue(new object(), 0));
            Try("int->enum[]", () => new IntE[1].SetValue(3, 0));
            Try("otherenum->enum[]", () => new IntE[1].SetValue(OtherIntE.Y, 0));
            Try("byteenum->intenum[]", () => new IntE[1].SetValue(ByteE.P, 0));
            Try("long->enum[]", () => new IntE[1].SetValue(3L, 0));
            Try("struct-mismatch", () => new Pt[1].SetValue(5, 0));

            Console.WriteLine("== null / Nullable / refs ==");
            int[] zeroed = { 7 };
            zeroed.SetValue(null, 0);
            Console.WriteLine($"null->int[] {zeroed[0]}");
            int?[] nullables = { 3 };
            nullables.SetValue(null, 0);
            Console.WriteLine($"null->int?[] {(nullables[0].HasValue ? "has" : "null")}");
            nullables.SetValue(9, 0);
            Console.WriteLine($"int->int?[] {nullables[0]}");
            int? boxedSrc = 8;
            nullables.SetValue(boxedSrc, 0);
            Console.WriteLine($"int?->int?[] {nullables[0]}");
            Try("long->int?[]", () => nullables.SetValue(3L, 0));
            string[] strs = { "a" };
            strs.SetValue(null, 0);
            Console.WriteLine($"null->string[] {(strs[0] is null ? "null" : strs[0])}");
            object[] objs = new object[1];
            objs.SetValue("s", 0);
            Console.WriteLine($"string->object[] {objs[0]}");
            Pt[] pts = new Pt[1];
            pts.SetValue(new Pt(3, 4), 0);
            Console.WriteLine($"struct[] {pts[0]}");
            IntE[] enums = new IntE[1];
            enums.SetValue(IntE.E, 0);
            Console.WriteLine($"enum[] {enums[0]}");
            ByteE[] byteEnums = new ByteE[1];
            byteEnums.SetValue(ByteE.Q, 0);
            Console.WriteLine($"byteenum[] {byteEnums[0]}");

            Console.WriteLine("== GetValue boxes ==");
            Console.WriteLine($"int[] -> {new int[] { 5 }.GetValue(0)!.GetType().Name} {new int[] { 5 }.GetValue(0)}");
            Console.WriteLine($"byte[] -> {new byte[] { 200 }.GetValue(0)!.GetType().Name} {new byte[] { 200 }.GetValue(0)}");
            Console.WriteLine($"short[] -> {new short[] { -3 }.GetValue(0)}");
            Console.WriteLine($"char[] -> {new char[] { 'Z' }.GetValue(0)}");
            Console.WriteLine($"long[] -> {new long[] { 1L << 40 }.GetValue(0)}");
            Console.WriteLine($"double[] -> {new double[] { 2.5 }.GetValue(0)}");
            Console.WriteLine($"float[] -> {new float[] { 1.25f }.GetValue(0)}");
            Console.WriteLine($"bool[] -> {new bool[] { true }.GetValue(0)}");
            Console.WriteLine($"enum[] -> {new IntE[] { IntE.C }.GetValue(0)!.GetType().Name} {new IntE[] { IntE.C }.GetValue(0)}");
            Console.WriteLine($"longenum[] -> {new LongE[] { LongE.L2 }.GetValue(0)} {(long)(LongE)new LongE[] { LongE.L2 }.GetValue(0)!}");
            Console.WriteLine($"int?[] has -> {new int?[] { 7 }.GetValue(0)?.GetType().Name} {new int?[] { 7 }.GetValue(0)}");
            Console.WriteLine($"int?[] null -> {(new int?[] { null }.GetValue(0) is null ? "null" : "obj")}");
            Console.WriteLine($"string[] -> {new string[] { "hey" }.GetValue(0)}");
            Console.WriteLine($"struct[] -> {new Pt[] { new Pt(1, 2) }.GetValue(0)}");
            Console.WriteLine($"long form -> {new int[] { 4, 9 }.GetValue(1L)}");
            var viaLong = new int[2];
            viaLong.SetValue(7, 1L);
            Console.WriteLine($"SetValue(long) -> {viaLong[1]}");
            Console.WriteLine($"indices form -> {new int[] { 1, 9 }.GetValue(new int[] { 1 })}");
            Console.WriteLine($"long-indices form -> {new int[] { 1, 9 }.GetValue(new long[] { 1 })}");

            Console.WriteLine("== bounds / rank errors ==");
            Try("GetValue OOR", () => new int[1].GetValue(5));
            Try("GetValue neg", () => new int[1].GetValue(-1));
            Try("SetValue OOR", () => new int[1].SetValue(1, 5));
            Try("GetValue big long", () => new int[1].GetValue(3000000000L));
            Try("GetValue null indices", () => new int[1].GetValue((int[])null!));
            Try("GetValue 2 indices on sz", () => new int[1].GetValue(new int[] { 0, 0 }));
            Try("sz.GetValue(int,int)", () => new int[3].GetValue(0, 0));

            Console.WriteLine("== multi-dimensional ==");
            int[,] md = new int[2, 3];
            md.SetValue(9, 1, 2);
            Console.WriteLine($"md set/get {md[1, 2]} {md.GetValue(1, 2)} {md.GetValue(new int[] { 1, 2 })}");
            Array mdArr = md;
            Console.WriteLine($"md shape rank={mdArr.Rank} len={mdArr.Length} l0={mdArr.GetLength(0)} l1={mdArr.GetLength(1)} lb={mdArr.GetLowerBound(1)} ub={mdArr.GetUpperBound(1)}");
            Try("md.SetValue linear", () => mdArr.SetValue(1, 0));
            Try("md.GetValue linear", () => mdArr.GetValue(0));
            Try("md 1 index", () => mdArr.GetValue(new int[] { 1 }));
            Try("md OOR", () => mdArr.GetValue(1, 3));
            string[,] smd = new string[2, 2];
            smd.SetValue("hi", 0, 1);
            Console.WriteLine($"string md {smd[0, 1]} {smd.GetValue(0, 1)} {(smd.GetValue(1, 1) is null ? "null" : "?")}");
            Console.WriteLine($"md type {md.GetType()}");

            Console.WriteLine("== Array.CreateInstance ==");
            Array ca = Array.CreateInstance(typeof(int), 4);
            ca.SetValue(11, 3);
            Console.WriteLine($"int,4 -> {ca.GetType()} len={ca.Length} [3]={ca.GetValue(3)}");
            Array cs = Array.CreateInstance(typeof(string), 2);
            cs.SetValue("s0", 0);
            Console.WriteLine($"string,2 -> {cs.GetType()} [0]={cs.GetValue(0)} [1]={(cs.GetValue(1) is null ? "null" : "?")}");
            Array ce = Array.CreateInstance(typeof(IntE), 2);
            Console.WriteLine($"enum,2 -> {ce.GetType()} [0]={ce.GetValue(0)}");
            Array cb = Array.CreateInstance(typeof(ByteE), 2);
            cb.SetValue(ByteE.Q, 1);
            Console.WriteLine($"byteenum,2 -> {cb.GetType()} [1]={cb.GetValue(1)}");
            Array cp = Array.CreateInstance(typeof(Pt), 2);
            cp.SetValue(new Pt(5, 6), 0);
            Console.WriteLine($"struct,2 -> {cp.GetType()} [0]={cp.GetValue(0)} [1]={cp.GetValue(1)}");
            // An element type whose T[] is never statically instantiated: the
            // runtime fabricates the array identity.
            Array cw = Array.CreateInstance(typeof(Widget), 2);
            cw.SetValue(new Widget(), 0);
            Console.WriteLine($"widget,2 -> {cw.GetType()} [0]={cw.GetValue(0)} [1]={(cw.GetValue(1) is null ? "null" : "?")}");
            Array cn = Array.CreateInstance(typeof(int?), 2);
            cn.SetValue(5, 1);
            Console.WriteLine($"int?,2 -> len={cn.Length} [0]={(cn.GetValue(0) is null ? "null" : "?")} [1]={cn.GetValue(1)}");
            Array cmd = Array.CreateInstance(typeof(string), 2, 3);
            cmd.SetValue("md", 1, 2);
            Console.WriteLine($"string,2,3 -> {cmd.GetType()} rank={cmd.Rank} [1,2]={cmd.GetValue(1, 2)}");
            Array cml = Array.CreateInstance(typeof(double), new int[] { 2, 2, 2 });
            cml.SetValue(2.5, new int[] { 1, 1, 1 });
            Console.WriteLine($"double[2,2,2] -> {cml.GetType()} rank={cml.Rank} len={cml.Length} [1,1,1]={cml.GetValue(new int[] { 1, 1, 1 })}");
            Array czb = Array.CreateInstance(typeof(int), new int[] { 3 }, new int[] { 0 });
            Console.WriteLine($"zero-bounds -> {czb.GetType()} len={czb.Length}");
            Try("CreateInstance null", () => Array.CreateInstance(null!, 1));
            Try("CreateInstance -1", () => Array.CreateInstance(typeof(int), -1));

            Console.WriteLine("== shape queries on Array-typed sz ==");
            Array szArr = new int[] { 1, 2, 3 };
            Console.WriteLine($"sz rank={szArr.Rank} len={szArr.Length} l0={szArr.GetLength(0)} lb={szArr.GetLowerBound(0)} ub={szArr.GetUpperBound(0)}");

            Console.WriteLine("== non-generic Reverse ==");
            int[] rev = { 1, 2, 3, 4 };
            Array.Reverse((Array)rev);
            Console.WriteLine($"rev int[] {string.Join(",", rev)}");
            string[] revs = { "a", "b", "c" };
            Array.Reverse((Array)revs);
            Console.WriteLine($"rev string[] {string.Join(",", revs)}");
            byte[] revb = { 1, 2, 3, 4, 5 };
            Array.Reverse((Array)revb, 1, 3);
            string joined = "";
            foreach (byte b in revb)
                joined += (joined.Length == 0 ? "" : ",") + b;
            Console.WriteLine($"rev byte[] window {joined}");

            Console.WriteLine("== CreateInstance lengths validation ==");
            // An EMPTY lengths array is ArgumentException in real .NET. dn2cpp used
            // to read lengths[0] out of bounds here — an OOB read on a
            // caller-supplied array, which is what a length that arrives from a
            // deserializer or a reflective call site looks like. The lengths are
            // built as arrays rather than written as argument lists so the empty
            // case is expressible at all (there is no `CreateInstance(t)` overload
            // to write it with). Type names only; the messages are localized.
            int[][] shapes = { new int[0], new[] { 3 }, new[] { 2, 3 } };
            foreach (int[] shape in shapes)
            {
                try
                {
                    Array made = Array.CreateInstance(typeof(int), shape);
                    Console.WriteLine($"ci rank={made.Rank} len={made.Length}");
                }
                catch (ArgumentException e)
                {
                    Console.WriteLine("ci rejected: " + e.GetType().Name);
                }
            }
        }
    }
}

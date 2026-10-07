#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

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

        sealed class CountingEquality : IEqualityComparer
        {
            public int Calls;
            public new bool Equals(object? x, object? y) => object.Equals(x, y);
            public int GetHashCode(object obj) { Calls++; return (int)obj; }
        }

        internal static void RunLowerBounds()
        {
            Console.WriteLine("== nonzero array lower bounds ==");
            Array one = Array.CreateInstance(typeof(int), new[] { 3 }, new[] { 5 });
            one.SetValue(11, 5);
            one.SetValue(22, new[] { 6 });
            one.SetValue(33, new long[] { 7 });
            Type nonSz = one.GetType();
            Console.WriteLine($"rank1 type={nonSz.Name}/{nonSz} sz={nonSz.IsSZArray} rank={one.Rank} length={one.Length} bounds={one.GetLowerBound(0)}/{one.GetUpperBound(0)} values={one.GetValue(5)}/{one.GetValue(new[] { 6 })}/{one.GetValue(new long[] { 7 })}");
            Console.WriteLine($"rank1 identity={nonSz == Array.CreateInstance(typeof(int), new[] { 0 }, new[] { 5 }).GetType()} assign={nonSz.IsAssignableFrom(typeof(int[]))}/{typeof(int[]).IsAssignableFrom(nonSz)} interfaces={one is IList}/{one is IList<int>}");
            Array zero = Array.CreateInstance(typeof(int), new[] { 2 }, new[] { 0 });
            Array from = Array.CreateInstanceFromArrayType(nonSz, new[] { 2 }, new[] { 0 });
            Array boundedFrom = Array.CreateInstanceFromArrayType(nonSz, new[] { 1 }, new[] { -3 });
            Console.WriteLine($"zero sz={zero.GetType().IsSZArray} from={from.GetType().IsSZArray}/{boundedFrom.GetLowerBound(0)}");
            Array copy = (Array)one.Clone();
            copy.SetValue(44, 5);
            Console.WriteLine($"clone type={copy.GetType() == nonSz} lower={copy.GetLowerBound(0)} distinct={one.GetValue(5)}/{copy.GetValue(5)}");
            Array.Reverse(one);
            int[] moved = new int[3];
            Array.Copy(one, moved, 3);
            Array.Copy(moved, 1, one, 6, 2);
            Array.Clear(one, 6, 1);
            Console.WriteLine($"moves={moved[0]}/{moved[1]}/{moved[2]} retained={one.GetValue(5)}/{one.GetValue(6)}/{one.GetValue(7)} bytes={Buffer.ByteLength(one)}");
            GCHandle pin = GCHandle.Alloc(one, GCHandleType.Pinned);
            Console.WriteLine($"pinned first={Marshal.ReadInt32(pin.AddrOfPinnedObject())}");
            pin.Free();
            Buffer.BlockCopy(one, 0, moved, 0, 12);
            Console.WriteLine($"blockcopy={moved[0]}/{moved[1]}/{moved[2]}");
            Array negative = Array.CreateInstance(typeof(string), new[] { 2 }, new[] { -2 });
            IList list = (IList)negative;
            list[-2] = "left";
            list[-1] = "right";
            string enumerated = "";
            foreach (object value in negative) enumerated += value + "/";
            object[] destination = new object[2];
            ((ICollection)negative).CopyTo(destination, 0);
            Console.WriteLine($"list values={list[-2]}/{list[-1]} enum={enumerated} copied={destination[0]}/{destination[1]}");
            Try("structural positive", () => ((IStructuralComparable)one).CompareTo(copy, Comparer.Default));
            Try("structural negative", () => ((IStructuralEquatable)negative).Equals(negative.Clone(), new CountingEquality()));
            Array hashArray = Array.CreateInstance(typeof(int), new[] { 10 }, new[] { 1 });
            CountingEquality equality = new CountingEquality();
            ((IStructuralEquatable)hashArray).GetHashCode(equality);
            Console.WriteLine($"structural hash calls={equality.Calls}");
            list.Clear();
            Console.WriteLine($"list cleared={list[-2] is null}/{list[-1] is null}");
            foreach (int lower in new[] { int.MinValue, -1, 7, int.MaxValue })
            {
                Array empty = Array.CreateInstance(typeof(int), new[] { 0 }, new[] { lower });
                Console.WriteLine($"empty lower={lower} upper={empty.GetUpperBound(0)} sz={empty.GetType().IsSZArray} clone={((Array)empty.Clone()).GetLowerBound(0)}");
            }
            Array extreme = Array.CreateInstance(typeof(int), new[] { 1 }, new[] { int.MaxValue });
            extreme.SetValue(71, int.MaxValue);
            Console.WriteLine($"extreme={extreme.GetUpperBound(0)}/{extreme.GetValue(int.MaxValue)}");
            Try("extreme wrong index", () => extreme.GetValue(int.MinValue));
            int[,] two = (int[,])Array.CreateInstance(typeof(int), new[] { 2, 2 }, new[] { -3, 4 });
            two[-3, 4] = 101;
            ((Array)two).SetValue(102, -2, 5);
            Console.WriteLine($"rank2={two[-3, 4]}/{two[-2, 5]}/{((Array)two).GetValue(new long[] { -2, 5 })}");
            int[,] twoCopy = (int[,])Array.CreateInstance(typeof(int), new[] { 2, 2 }, new[] { 7, -8 });
            Array.Copy(two, twoCopy, 4);
            Console.WriteLine($"rank2 copy={twoCopy[7, -8]}/{twoCopy[8, -7]}");
            Array.Clear(two, -3, 1);
            Console.WriteLine($"rank2 clear={two[-3, 4]}/{two[-2, 5]}");
            int[,,] three = (int[,,])Array.CreateInstance(typeof(int), new[] { 1, 2, 1 }, new[] { 2, -4, 6 });
            three[2, -3, 6] = 103;
            ((Array)three).SetValue(104, 2, -4, 6);
            Console.WriteLine($"rank3={three[2, -3, 6]}/{three[2, -4, 6]}");
            byte[,,,] four = (byte[,,,])Array.CreateInstance(typeof(byte), new[] { 1, 1, 1, 1 }, new[] { -1, 2, -3, 4 });
            four[-1, 2, -3, 4] = 201;
            Console.WriteLine($"rank4={four[-1, 2, -3, 4]}/{((Array)four).GetValue(new[] { -1, 2, -3, 4 })}");
            Array enumArray = Array.CreateInstance(typeof(ByteE), new[] { 1 }, new[] { -5 });
            enumArray.SetValue(ByteE.Q, -5);
            Array structArray = Array.CreateInstance(typeof(Pt), new[] { 1 }, new[] { 8 });
            structArray.SetValue(new Pt(9, 10), 8);
            Array nullableArray = Array.CreateInstance(typeof(int?), new[] { 2 }, new[] { -8 });
            nullableArray.SetValue(12, -8);
            nullableArray.SetValue(null, -7);
            Console.WriteLine($"storage={enumArray.GetValue(-5)}/{structArray.GetValue(8)}/{nullableArray.GetValue(-8)}/{nullableArray.GetValue(-7) is null}");
            Try("bounds null type", () => Array.CreateInstance(null!, new[] { -1 }, new[] { int.MaxValue }));
            Try("bounds null lengths", () => Array.CreateInstance(typeof(int), null!, new[] { 0 }));
            Try("bounds null bounds", () => Array.CreateInstance(typeof(int), new[] { 1 }, null!));
            Try("bounds rank mismatch", () => Array.CreateInstance(typeof(int), new[] { 1 }, new[] { 0, 0 }));
            Try("bounds empty rank", () => Array.CreateInstance(typeof(int), Array.Empty<int>(), Array.Empty<int>()));
            Try("bounds negative before void", () => Array.CreateInstance(typeof(void), new[] { -1 }, new[] { int.MaxValue }));
            Try("bounds void before overflow", () => Array.CreateInstance(typeof(void), new[] { 2 }, new[] { int.MaxValue }));
            try { Array.CreateInstance(typeof(int), new[] { 2 }, new[] { int.MaxValue }); }
            catch (ArgumentOutOfRangeException e) { Console.WriteLine($"bounds overflow={e.ParamName is null}/{e.Message}"); }
            Try("bounds SZ from type", () => Array.CreateInstanceFromArrayType(typeof(int[]), new[] { 1 }, new[] { 1 }));
            Console.WriteLine("nonzero array lower bounds end");
        }
        static Array SearchArray(int lower, params int[] values)
        {
            Array array = Array.CreateInstance(typeof(int), new[] { values.Length }, new[] { lower });
            for (int i = 0; i < values.Length; i++)
                array.SetValue(values[i], lower + i);
            return array;
        }

        static void SearchFault(string label, Action action)
        {
            try { action(); Console.WriteLine($"{label}=ok"); }
            catch (Exception e) { Console.WriteLine($"{label}={e.GetType().Name}/{(e as ArgumentException)?.ParamName ?? "<none>"}"); }
        }

        internal static void RunLowerBoundSearches()
        {
            Console.WriteLine("== lower-bound array sort and search ==");
            foreach (int lower in new[] { 5, -2, 0 })
            {
                for (int form = 0; form < 4; form++)
                {
                    Array array = SearchArray(lower, 7, 1, 3);
                    if (form == 0) Array.Sort(array);
                    else if (form == 1) Array.Sort(array, (IComparer?)null);
                    else if (form == 2) Array.Sort(array, lower, 3);
                    else Array.Sort(array, lower, 3, (IComparer?)null);
                    Console.WriteLine($"lower sort={lower}/{form}:{array.GetValue(lower)}/{array.GetValue(lower + 1)}/{array.GetValue(lower + 2)}");
                }
                Array sorted = SearchArray(lower, 1, 3, 7);
                for (int form = 0; form < 3; form++)
                {
                    int first = form == 0 ? Array.IndexOf(sorted, 3) : form == 1 ? Array.IndexOf(sorted, 3, lower) : Array.IndexOf(sorted, 3, lower, 3);
                    int last = form == 0 ? Array.LastIndexOf(sorted, 3) : form == 1 ? Array.LastIndexOf(sorted, 3, lower + 2) : Array.LastIndexOf(sorted, 3, lower + 2, 3);
                    int missFirst = form == 0 ? Array.IndexOf(sorted, 9) : form == 1 ? Array.IndexOf(sorted, 9, lower) : Array.IndexOf(sorted, 9, lower, 3);
                    int missLast = form == 0 ? Array.LastIndexOf(sorted, 9) : form == 1 ? Array.LastIndexOf(sorted, 9, lower + 2) : Array.LastIndexOf(sorted, 9, lower + 2, 3);
                    Console.WriteLine($"lower indices={lower}/{form}:{first}/{last}/{missFirst}/{missLast}");
                }
                for (int form = 0; form < 4; form++)
                {
                    int hit = form == 0 ? Array.BinarySearch(sorted, 3) : form == 1 ? Array.BinarySearch(sorted, 3, (IComparer?)null) : form == 2 ? Array.BinarySearch(sorted, lower, 3, 3) : Array.BinarySearch(sorted, lower, 3, 3, (IComparer?)null);
                    int inside = form == 0 ? Array.BinarySearch(sorted, 2) : form == 1 ? Array.BinarySearch(sorted, 2, (IComparer?)null) : form == 2 ? Array.BinarySearch(sorted, lower, 3, 2) : Array.BinarySearch(sorted, lower, 3, 2, (IComparer?)null);
                    int after = form == 0 ? Array.BinarySearch(sorted, 9) : form == 1 ? Array.BinarySearch(sorted, 9, (IComparer?)null) : form == 2 ? Array.BinarySearch(sorted, lower, 3, 9) : Array.BinarySearch(sorted, lower, 3, 9, (IComparer?)null);
                    Console.WriteLine($"lower binary={lower}/{form}:{hit}/{inside}/{after}");
                }
            }
            Array values = SearchArray(-2, 1, 3, 7);
            SearchFault("lower index before range", () => Array.IndexOf(values, 3, -3, -1));
            SearchFault("lower last count", () => Array.LastIndexOf(values, 3, -2, 2));
            SearchFault("lower binary before length", () => Array.BinarySearch(values, -3, -1, 3));
            SearchFault("lower binary length", () => Array.BinarySearch(values, -2, -1, 3));
            SearchFault("lower binary overrun", () => Array.BinarySearch(values, -1, 3, 3));
            SearchFault("lower sort below range", () => Array.Sort(values, -3, 1));
            SearchFault("lower sort length", () => Array.Sort(values, -2, -1));
            SearchFault("lower sort overrun", () => Array.Sort(values, -1, 3));
            Array md = Array.CreateInstance(typeof(int), new[] { 1, 1 }, new[] { -2, 3 });
            SearchFault("lower index rank first", () => Array.IndexOf(md, 3, -3, -1));
            SearchFault("lower last range first", () => Array.LastIndexOf(md, 3, -3, -1));
            SearchFault("lower binary range first", () => Array.BinarySearch(md, -3, -1, 3));
            SearchFault("lower sort rank first", () => Array.Sort(md, -3, -1));
            SearchFault("lower binary null first", () => Array.BinarySearch(null!, -3, -1, 3));
            Console.WriteLine($"lower invalid retained={values.GetValue(-2)}/{values.GetValue(-1)}/{values.GetValue(0)}");
            Array extreme = SearchArray(int.MaxValue, 9);
            Array.Sort(extreme, (IComparer?)null);
            Console.WriteLine($"lower extreme sort={extreme.GetValue(int.MaxValue)} binary={Array.BinarySearch(extreme, 9)}");
            Array empty = SearchArray(int.MinValue);
            Console.WriteLine($"lower empty min={Array.IndexOf(empty, 9)}/{Array.LastIndexOf(empty, 9)}/{Array.BinarySearch(empty, 9)}");
            Console.WriteLine("lower-bound array sort and search end");
        }
        sealed class RangeZeroComparer : IComparer
        {
            public int Compare(object? x, object? y) => 0;
        }

        static void RangeResult(string label, Func<object> action)
        {
            try { Console.WriteLine($"{label}={action()}"); }
            catch (Exception e) { Console.WriteLine($"{label}={e.GetType().Name}/{(e as ArgumentException)?.ParamName ?? "<none>"}"); }
        }

        internal static void RunWrappedRanges()
        {
            Console.WriteLine("== wrapped lower-bound array ranges ==");
            foreach (Type type in new[] { typeof(int), typeof(string), typeof(object) })
                foreach (int lower in new[] { -5, 0, 5 })
                    foreach (int count in new[] { 0, 1, 2, 3 })
                    {
                        Array array = Array.CreateInstance(type, new[] { 3 }, new[] { lower });
                        object key = type == typeof(string) ? "b" : 3;
                        for (int i = 0; i < 3; i++)
                            array.SetValue(type == typeof(string) ? new[] { "a", "b", "c" }[i] : (object)(i + 1), lower + i);
                        string label = $"wrapped {type.Name}/{lower}/{count}";
                        RangeResult(label + " sort", () => { Array.Sort(array, int.MaxValue, count); return "ok"; });
                        RangeResult(label + " sort custom", () => { Array.Sort(array, int.MaxValue, count, new RangeZeroComparer()); return "ok"; });
                        RangeResult(label + " reverse", () => { Array.Reverse(array, int.MaxValue, count); return "ok"; });
                        RangeResult(label + " binary", () => Array.BinarySearch(array, int.MaxValue, count, key));
                        RangeResult(label + " binary custom", () => Array.BinarySearch(array, int.MaxValue, count, key, new RangeZeroComparer()));
                    }
            Array values = SearchArray(-5, 1, 2, 3);
            RangeResult("wrapped Default", () => Array.BinarySearch(values, int.MaxValue, 0, 1, Comparer.Default));
            RangeResult("wrapped DefaultInvariant", () => Array.BinarySearch(values, int.MaxValue, 0, 1, Comparer.DefaultInvariant));
            RangeResult("wrapped new comparer", () => Array.BinarySearch(values, int.MaxValue, 0, 1, new Comparer(System.Globalization.CultureInfo.InvariantCulture)));
            RangeResult("wrapped null value", () => Array.BinarySearch(values, int.MaxValue, 1, null));
            RangeResult("wrapped wrong value type", () => Array.BinarySearch(values, int.MaxValue, 0, 1L));
            foreach (object value in new object[] { (byte)1, (sbyte)1, (short)1, (ushort)1, 1, 1u, 1L, 1UL, (nint)1, (nuint)1, true, 'a', 1f, 1d, IntE.B })
            {
                Array primitive = Array.CreateInstance(value.GetType(), new[] { 3 }, new[] { -5 });
                RangeResult("wrapped primitive " + value.GetType().Name, () => Array.BinarySearch(primitive, int.MaxValue, 0, value));
            }
            RangeResult("wrapped reverse primitive slice", () => { Array.Reverse(values, int.MaxValue, 2); return "ok"; });
            Array references = Array.CreateInstance(typeof(string), new[] { 3 }, new[] { -5 });
            RangeResult("wrapped reverse reference endpoints", () => { Array.Reverse(references, int.MaxValue, 2); return "ok"; });
            Array md = Array.CreateInstance(typeof(int), new[] { 1, 1 }, new[] { -5, 0 });
            RangeResult("wrapped binary rank", () => Array.BinarySearch(md, int.MaxValue, 0, 1));
            RangeResult("wrapped binary null first", () => Array.BinarySearch(null!, int.MaxValue, -1, 1));
            Console.WriteLine("wrapped lower-bound array ranges end");
        }
        internal static void RunSortAccessFaults()
        {
            Console.WriteLine("== lower-bound sort access faults ==");
            foreach (Type type in new[] { typeof(int), typeof(string), typeof(object) })
                foreach (int count in new[] { 2, 3 })
                {
                    Array array = Array.CreateInstance(type, new[] { 3 }, new[] { -10 });
                    object key = type == typeof(string) ? "b" : 1;
                    int index = int.MaxValue - 5;
                    string label = $"sort access {type.Name}/{count}";
                    RangeResult(label + " sort", () => { Array.Sort(array, index, count); return "ok"; });
                    RangeResult(label + " sort custom", () => { Array.Sort(array, index, count, new RangeZeroComparer()); return "ok"; });
                    RangeResult(label + " reverse", () => { Array.Reverse(array, index, count); return "ok"; });
                    RangeResult(label + " binary", () => Array.BinarySearch(array, index, count, key));
                    RangeResult(label + " binary custom", () => Array.BinarySearch(array, index, count, key, new RangeZeroComparer()));
                }
            foreach (Type type in new[] { typeof(int), typeof(string), typeof(object) })
                foreach (int count in new[] { 2, int.MaxValue - 4 })
                {
                    Array array = Array.CreateInstance(type, new[] { 3 }, new[] { int.MinValue });
                    string label = $"sort start {type.Name}/{count}";
                    RangeResult(label + " default", () => { Array.Sort(array, 4, count); return "ok"; });
                    RangeResult(label + " custom", () => { Array.Sort(array, 4, count, new RangeZeroComparer()); return "ok"; });
                }
            Console.WriteLine("lower-bound sort access faults end");
        }
    }
}

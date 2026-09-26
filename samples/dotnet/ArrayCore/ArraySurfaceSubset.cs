using System;
using System.Collections;
using System.Text;

namespace ArraySurfaceSubset
{
    // System.Array's non-generic surface beyond element access and Copy: the 64-bit
    // index and length overloads (each range-checks, then calls its Int32 sibling),
    // LongLength/GetLongLength, the constant ICollection/IList properties, the
    // dimension queries on statically-typed receivers, ConstrainedCopy's
    // no-conversion verdict, CreateInstanceFromArrayType's lengths forms, and
    // Initialize, which runs a struct's explicit parameterless constructor over every
    // element. Rows whose fault the C++ runtime raises print the exception type only;
    // the rest print .NET's message too.
    internal static class Program
    {
        private struct Plain
        {
            public int X;
        }

        private struct WithCtor
        {
            public int X;
            public WithCtor() { X = 7; }
        }

        private enum Small : byte { A, B }

        private static string Show(Array a)
        {
            var sb = new StringBuilder("[");
            int i = 0;
            foreach (object o in a)
                sb.Append(i++ == 0 ? "" : ",").Append(o is null ? "null" : o.ToString());
            return sb.Append(']').ToString();
        }

        private static string Describe(Array a)
        {
            var sb = new StringBuilder(a.GetType().Name).Append(" rank ").Append(a.Rank).Append(" lengths");
            for (int d = 0; d < a.Rank; d++)
                sb.Append(' ').Append(a.GetLength(d));
            return sb.ToString();
        }

        private static void E(string tag, Func<object> f)
        {
            try
            {
                Console.WriteLine(tag + ": " + f());
            }
            catch (Exception ex)
            {
                Console.WriteLine(tag + ": " + ex.GetType().Name + ": " + ex.Message);
            }
        }

        private static void T(string tag, Func<object> f)
        {
            try
            {
                Console.WriteLine(tag + ": " + f());
            }
            catch (Exception ex)
            {
                Console.WriteLine(tag + ": " + ex.GetType().Name);
            }
        }

        internal static void Run()
        {
            Console.WriteLine("== array 64-bit and constant members ==");
            int[] ia = { 1, 2, 3, 4 };
            Array arr = ia;
            int[,] md = { { 1, 2, 3 }, { 4, 5, 6 } };
            Array amd = md;
            int[,,] m3 = new int[2, 2, 2];
            int[] nulSz = null;
            int[,] nulMd = null;

            E("longlength", () => ia.LongLength);
            E("longlength-md", () => md.LongLength);
            E("longlength-array", () => arr.LongLength);
            E("getlonglength", () => md.GetLongLength(1));
            T("getlonglength-bad", () => md.GetLongLength(2));
            E("isfixedsize", () => arr.IsFixedSize);
            E("isreadonly", () => arr.IsReadOnly);
            E("issynchronized", () => arr.IsSynchronized);
            E("syncroot-self", () => ReferenceEquals(arr.SyncRoot, arr));
            E("ilist-isfixedsize", () => ((IList)ia).IsFixedSize);
            T("null-isfixedsize", () => ((Array)nulSz).IsFixedSize);
            T("null-syncroot", () => ((Array)nulSz).SyncRoot);
            T("null-longlength", () => nulSz.LongLength);

            E("getlength-sz", () => ia.GetLength(0));
            T("getlength-sz-dim1", () => ia.GetLength(1));
            T("getlowerbound-sz-dim1", () => ia.GetLowerBound(1));
            T("getupperbound-sz-neg", () => ia.GetUpperBound(-1));
            E("getlength-md", () => md.GetLength(1) + " " + md.GetLowerBound(1) + " " + md.GetUpperBound(1));
            T("getlength-md-dim2", () => md.GetLength(2));
            T("getlowerbound-md-neg", () => md.GetLowerBound(-1));
            T("getupperbound-md-dim5", () => md.GetUpperBound(5));
            T("rank-null-sz", () => nulSz.Rank);
            T("getlength-null-sz", () => nulSz.GetLength(0));
            T("getlowerbound-null-sz", () => nulSz.GetLowerBound(0));
            T("rank-null-md", () => nulMd.Rank);
            T("getlength-null-md", () => nulMd.GetLength(0));
            T("getupperbound-null-md", () => nulMd.GetUpperBound(0));

            E("getvalue-long", () => arr.GetValue(2L));
            E("getvalue-long-huge", () => arr.GetValue(1L << 32));
            T("getvalue-long-neg", () => arr.GetValue(-1L));
            E("getvalue-long2", () => amd.GetValue(1L, 2L));
            E("getvalue-long2-huge", () => amd.GetValue(0L, 1L << 33));
            E("getvalue-long3", () => { m3[1, 0, 1] = 9; return m3.GetValue(1L, 0L, 1L); });
            E("getvalue-long-array", () => amd.GetValue(new long[] { 1, 0 }));
            E("getvalue-long-array-huge", () => amd.GetValue(new long[] { 1, -(1L << 40) }));
            E("getvalue-long-array-null", () => amd.GetValue((long[])null));
            E("setvalue-long", () => { arr.SetValue(40, 3L); return ia[3]; });
            E("setvalue-long2", () => { amd.SetValue(50, 0L, 2L); return md[0, 2]; });
            E("setvalue-long3", () => { m3.SetValue(60, 0L, 1L, 1L); return m3[0, 1, 1]; });
            E("setvalue-long-array", () => { amd.SetValue(70, new long[] { 1, 1 }); return md[1, 1]; });
            E("setvalue-long-huge", () => { arr.SetValue(1, long.MaxValue); return "set"; });
            E("setvalue-long3-huge", () => { m3.SetValue(1, 0L, long.MinValue, 0L); return "set"; });

            E("copy-long", () => { var d = new int[4]; Array.Copy(ia, d, 3L); return Show(d); });
            E("copy-long5", () => { var d = new int[4]; Array.Copy(ia, 1L, d, 2L, 2L); return Show(d); });
            E("copy-long-huge", () => { var d = new int[4]; Array.Copy(ia, d, (1L << 32) + 2); return Show(d); });
            E("copy-long5-huge-index", () => { var d = new int[4]; Array.Copy(ia, 1L << 32, d, 0L, 1L); return Show(d); });
            E("copyto-long", () => { var d = new int[6]; arr.CopyTo(d, 2L); return Show(d); });
            E("copyto-long-huge", () => { var d = new int[6]; arr.CopyTo(d, 1L << 40); return Show(d); });

            E("createinstance-long-array", () => Describe(Array.CreateInstance(typeof(int), new long[] { 2, 3 })));
            E("createinstance-long-array-sz", () => Describe(Array.CreateInstance(typeof(string), new long[] { 3 })));
            E("createinstance-long-array-huge", () => Describe(Array.CreateInstance(typeof(int), new long[] { 1L << 32 })));
            E("createinstance-long-array-null", () => Describe(Array.CreateInstance(typeof(int), (long[])null)));

            T("fromarraytype-lengths", () => Describe(Array.CreateInstanceFromArrayType(typeof(int[,]), new[] { 2, 3 })));
            T("fromarraytype-lengths-sz", () => Describe(Array.CreateInstanceFromArrayType(typeof(string[]), new[] { 3 })));
            T("fromarraytype-lengths-rank", () => Describe(Array.CreateInstanceFromArrayType(typeof(int[,]), new[] { 2 })));
            T("fromarraytype-lengths-notarray", () => Describe(Array.CreateInstanceFromArrayType(typeof(int), new[] { 2 })));
            T("fromarraytype-lengths-neg", () => Describe(Array.CreateInstanceFromArrayType(typeof(int[]), new[] { -1 })));
            T("fromarraytype-lengths-null", () => Describe(Array.CreateInstanceFromArrayType(typeof(int[]), (int[])null)));
            T("fromarraytype-bounds", () => Describe(Array.CreateInstanceFromArrayType(typeof(int[,]), new[] { 2, 3 }, new[] { 0, 0 })));
            T("fromarraytype-bounds-sz", () => Describe(Array.CreateInstanceFromArrayType(typeof(long[]), new[] { 2 }, new[] { 0 })));
            T("fromarraytype-bounds-count", () => Describe(Array.CreateInstanceFromArrayType(typeof(int[,]), new[] { 2, 3 }, new[] { 0 })));
            T("fromarraytype-bounds-sz-nonzero", () => Describe(Array.CreateInstanceFromArrayType(typeof(int[]), new[] { 2 }, new[] { 1 })));
            T("fromarraytype-bounds-null", () => Describe(Array.CreateInstanceFromArrayType(typeof(int[]), new[] { 2 }, null)));
            T("fromarraytype-int-md", () => Describe(Array.CreateInstanceFromArrayType(typeof(int[,]), 2)));
            T("fromarraytype-int-neg-notarray", () => Describe(Array.CreateInstanceFromArrayType(typeof(int), -1)));

            E("constrained-same", () => { var d = new int[5]; Array.ConstrainedCopy(ia, 0, d, 1, 4); return Show(d); });
            E("constrained-covariant", () => { var d = new object[2]; Array.ConstrainedCopy(new[] { "a", "b" }, 0, d, 0, 2); return Show(d); });
            E("constrained-enum-underlying", () => { var d = new byte[2]; Array.ConstrainedCopy(new[] { Small.B, Small.A }, 0, d, 0, 2); return Show(d); });
            E("constrained-int-uint", () => { var d = new uint[2]; Array.ConstrainedCopy(new[] { -1, 2 }, 0, d, 0, 2); return Show(d); });
            E("constrained-widen", () => { var d = new long[4]; Array.ConstrainedCopy(ia, 0, d, 0, 2); return Show(d); });
            E("constrained-box", () => { var d = new object[4]; Array.ConstrainedCopy(ia, 0, d, 0, 2); return Show(d); });
            E("constrained-unbox", () => { var d = new int[1]; Array.ConstrainedCopy(new object[] { 1 }, 0, d, 0, 1); return Show(d); });
            E("constrained-downcast", () => { var d = new string[1]; Array.ConstrainedCopy(new object[] { "a" }, 0, d, 0, 1); return Show(d); });
            E("constrained-wrongtype", () => { var d = new string[1]; Array.ConstrainedCopy(ia, 0, d, 0, 1); return Show(d); });
            T("constrained-null", () => { Array.ConstrainedCopy(null, 0, ia, 0, 1); return "copied"; });
            T("constrained-range", () => { var d = new int[2]; Array.ConstrainedCopy(ia, 0, d, 0, 3); return Show(d); });
            E("constrained-dyn-same", () => { Array s = ia; Array d = new int[4]; Array.ConstrainedCopy(s, 1, d, 0, 2); return Show(d); });
            E("constrained-dyn-widen", () => { Array s = ia; Array d = new long[4]; Array.ConstrainedCopy(s, 1, d, 0, 2); return Show(d); });
            E("constrained-dyn-covariant", () => { Array s = new[] { "x" }; Array d = new object[1]; Array.ConstrainedCopy(s, 0, d, 0, 1); return Show(d); });

            int[] ints = { 5 };
            ints.Initialize();
            string[] strs = { "s" };
            strs.Initialize();
            Plain[] plain = { new Plain { X = 3 } };
            plain.Initialize();
            Console.WriteLine("initialize-noop: " + ints[0] + " " + strs[0] + " " + plain[0].X);
            WithCtor[] withCtor = new WithCtor[3];
            Console.WriteLine("initialize-before: " + withCtor[0].X + withCtor[2].X);
            withCtor[1].X = 1;
            withCtor.Initialize();
            Console.WriteLine("initialize-after: " + withCtor[0].X + withCtor[1].X + withCtor[2].X);
            WithCtor[,] withCtorMd = new WithCtor[2, 2];
            withCtorMd.Initialize();
            Console.WriteLine("initialize-md: " + withCtorMd[0, 0].X + withCtorMd[1, 1].X);
            T("initialize-null", () => { nulSz.Initialize(); return "ran"; });
            T("initialize-null-ctor", () => { WithCtor[] n = null; n.Initialize(); return "ran"; });

            // A method group names the real body a call reaches.
            Func<long, object> getValue = arr.GetValue;
            Func<Array, Array, long, int> copyGroup = (s, d, n) => { Array.Copy(s, d, n); return d.Length; };
            Action<object, long, long> setValue2 = amd.SetValue;
            setValue2(80, 1L, 0L);
            Console.WriteLine("group-getvalue-long: " + getValue(1L) + " " + md[1, 0] + " "
                + copyGroup(ia, new int[4], 2L));
        }
    }
}

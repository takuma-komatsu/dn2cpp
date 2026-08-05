#nullable disable
using System;
using Dn2Cpp.Runtime;

namespace HotPathBoundsSubset
{
    internal struct Vec3
    {
        public float X, Y, Z;

        public Vec3(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }
    }

    // [HotPath(SkipBoundsChecks = true)]: every index below is in range, so the
    // outputs stay exact-diff-identical to real .NET, where the attribute is
    // inert and every access stays checked — the knob only removes the checks.
    // The kernels fan out across every element-access emission path (the I4 /
    // Ref / ArrayN reps, the sized ldelem/stelem prim opcodes, ldelema, the
    // generic ldelem/stelem token forms, and the Span/ReadOnlySpan indexer).
    // The gate script grep-asserts that each *Unchecked body in
    // generated_hot.cpp names no dn2cpp_bounds_check / checked element wrapper,
    // while the plain-[HotPath] kernels still do.
    internal static class Program
    {
        [HotPath(SkipBoundsChecks = true)]
        private static long SumIntsUnchecked(int[] values) // ldelem.i4 (I4 rep)
        {
            long sum = 0;
            for (int i = 0; i < values.Length; i++)
                sum += values[i];
            return sum;
        }

        [HotPath(SkipBoundsChecks = true)]
        private static void ReverseIntsUnchecked(int[] values) // ldelem.i4 + stelem.i4
        {
            for (int i = 0, j = values.Length - 1; i < j; i++, j--)
            {
                int tmp = values[i];
                values[i] = values[j];
                values[j] = tmp;
            }
        }

        [HotPath(SkipBoundsChecks = true)]
        private static int MaxByRefUnchecked(int[] values) // ldelema int32 (I4 rep)
        {
            ref int best = ref values[0];
            for (int i = 1; i < values.Length; i++)
            {
                if (values[i] > best)
                    best = ref values[i];
            }
            return best;
        }

        [HotPath(SkipBoundsChecks = true)]
        private static void SwapEndsUnchecked(string[] items) // ldelem.ref + stelem.ref
        {
            string first = items[0];
            items[0] = items[items.Length - 1];
            items[items.Length - 1] = first;
        }

        [HotPath(SkipBoundsChecks = true)]
        private static void ScaleFloatsUnchecked(float[] values, float scale) // ldelem.r4 + stelem.r4 (ArrayN)
        {
            for (int i = 0; i < values.Length; i++)
                values[i] = values[i] * scale;
        }

        [HotPath(SkipBoundsChecks = true)]
        private static void FillBytesUnchecked(byte[] data, int seed) // stelem.i1 (packed sub-word)
        {
            for (int i = 0; i < data.Length; i++)
                data[i] = (byte)(seed + i * 7);
        }

        [HotPath(SkipBoundsChecks = true)]
        private static int SumBytesUnchecked(byte[] data) // ldelem.u1 (packed sub-word)
        {
            int sum = 0;
            for (int i = 0; i < data.Length; i++)
                sum += data[i];
            return sum;
        }

        [HotPath(SkipBoundsChecks = true)]
        private static long SumLongsUnchecked(long[] values) // ldelem.i8 (ArrayN)
        {
            long sum = 0;
            for (int i = 0; i < values.Length; i++)
                sum += values[i];
            return sum;
        }

        [HotPath(SkipBoundsChecks = true)]
        private static void AxpyVecsUnchecked(Vec3[] acc, Vec3[] src, float scale) // ldelema Vec3 (ArrayN)
        {
            for (int i = 0; i < acc.Length; i++)
            {
                acc[i].X += src[i].X * scale;
                acc[i].Y += src[i].Y * scale;
                acc[i].Z += src[i].Z * scale;
            }
        }

        [HotPath(SkipBoundsChecks = true)]
        private static void CopyVecsUnchecked(Vec3[] dst, Vec3[] src) // ldelem/stelem <Vec3> (ArrayN)
        {
            for (int i = 0; i < dst.Length; i++)
                dst[i] = src[i];
        }

        [HotPath(SkipBoundsChecks = true)]
        private static T LastUnchecked<T>(T[] values) // ldelem !T — I4/Ref/ArrayN arm per instantiation
        {
            return values[values.Length - 1];
        }

        [HotPath(SkipBoundsChecks = true)]
        private static void SetFirstUnchecked<T>(T[] values, T value) // stelem !T
        {
            values[0] = value;
        }

        [HotPath(SkipBoundsChecks = true)]
        private static int SumSpanUnchecked(ReadOnlySpan<int> values) // ReadOnlySpan<int>.get_Item
        {
            int sum = 0;
            for (int i = 0; i < values.Length; i++)
                sum += values[i];
            return sum;
        }

        [HotPath(SkipBoundsChecks = true)]
        private static void UpperAsciiSpanUnchecked(Span<char> chars) // Span<char>.get_Item (sub-word, read + write through the byref)
        {
            for (int i = 0; i < chars.Length; i++)
            {
                char c = chars[i];
                if (c >= 'a' && c <= 'z')
                    chars[i] = (char)(c - 32);
            }
        }

        internal static void __GateEntry()
        {
            var ints = new int[97];
            for (int i = 0; i < ints.Length; i++)
                ints[i] = i * 17 - 400;
            Console.WriteLine(SumIntsUnchecked(ints));
            ReverseIntsUnchecked(ints);
            Console.WriteLine(ints[0] + "," + ints[1] + "," + ints[96]);
            Console.WriteLine(MaxByRefUnchecked(ints));

            var names = new string[5] { "alpha", "beta", "gamma", "delta", "epsilon" };
            SwapEndsUnchecked(names);
            Console.WriteLine(string.Join("|", names));

            var floats = new float[48];
            for (int i = 0; i < floats.Length; i++)
                floats[i] = 1.5f + i * 0.75f;
            ScaleFloatsUnchecked(floats, 1.25f);
            float fsum = 0f;
            for (int i = 0; i < floats.Length; i++)
                fsum += floats[i];
            Console.WriteLine(BitConverter.SingleToInt32Bits(fsum).ToString("X8"));

            var bytes = new byte[33];
            FillBytesUnchecked(bytes, 5);
            Console.WriteLine(SumBytesUnchecked(bytes));

            var longs = new long[9];
            for (int i = 0; i < longs.Length; i++)
                longs[i] = i * 1000000007L - 3;
            Console.WriteLine(SumLongsUnchecked(longs));

            var accv = new Vec3[12];
            var srcv = new Vec3[12];
            for (int i = 0; i < accv.Length; i++)
            {
                accv[i] = new Vec3(i * 0.5f, 2f - i, i);
                srcv[i] = new Vec3(1f + i, i * 0.25f, -0.5f * i);
            }
            AxpyVecsUnchecked(accv, srcv, 0.75f);
            var dstv = new Vec3[12];
            CopyVecsUnchecked(dstv, accv);
            float vsum = 0f;
            for (int i = 0; i < dstv.Length; i++)
                vsum += dstv[i].X - dstv[i].Y + dstv[i].Z;
            Console.WriteLine(BitConverter.SingleToInt32Bits(vsum).ToString("X8"));

            SetFirstUnchecked(ints, 12345);
            Console.WriteLine(LastUnchecked(ints) + "," + ints[0]);
            SetFirstUnchecked(names, "omega");
            Console.WriteLine(LastUnchecked(names) + "," + names[0]);
            SetFirstUnchecked(dstv, new Vec3(9f, 8f, 7f));
            Vec3 lastVec = LastUnchecked(dstv);
            Console.WriteLine(BitConverter.SingleToInt32Bits(dstv[0].X + lastVec.Z).ToString("X8"));

            Console.WriteLine(SumSpanUnchecked(new ReadOnlySpan<int>(ints, 3, 40)));
            var titleChars = "hot path bounds".ToCharArray();
            UpperAsciiSpanUnchecked(new Span<char>(titleChars));
            Console.WriteLine(new string(titleChars));
        }
    }
}

#nullable disable
using System;
using Dn2Cpp.Runtime;

namespace HotPathBasicSubset
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

    // Bare [HotPath] on plain numeric kernels: results stay bit-identical to
    // real .NET (floats are printed as raw bit patterns, so the diff is
    // culture-proof and rounding-proof) — the attribute only relocates the
    // bodies into generated_hot.cpp.
    internal static class Program
    {
        [HotPath]
        private static long SumInts(int[] values)
        {
            long sum = 0;
            for (int i = 0; i < values.Length; i++)
                sum += values[i];
            return sum;
        }

        [HotPath]
        private static float DotProduct(float[] a, float[] b)
        {
            float acc = 0f;
            for (int i = 0; i < a.Length; i++)
                acc += a[i] * b[i];
            return acc;
        }

        [HotPath]
        private static void AxpyVecs(Vec3[] acc, Vec3[] src, float scale)
        {
            for (int i = 0; i < acc.Length; i++)
            {
                acc[i].X += src[i].X * scale;
                acc[i].Y += src[i].Y * scale;
                acc[i].Z += src[i].Z * scale;
            }
        }

        internal static void __GateEntry()
        {
            var ints = new int[257];
            for (int i = 0; i < ints.Length; i++)
                ints[i] = i * 31 - 1000;
            Console.WriteLine(SumInts(ints));

            var a = new float[64];
            var b = new float[64];
            for (int i = 0; i < a.Length; i++)
            {
                a[i] = 0.5f + i * 0.25f;
                b[i] = 3.0f - i * 0.125f;
            }
            Console.WriteLine(BitConverter.SingleToInt32Bits(DotProduct(a, b)).ToString("X8"));

            var accv = new Vec3[16];
            var srcv = new Vec3[16];
            for (int i = 0; i < accv.Length; i++)
            {
                accv[i] = new Vec3(i, i * 2f, i * 3f);
                srcv[i] = new Vec3(1f - i, 0.5f * i, -i);
            }
            AxpyVecs(accv, srcv, 1.5f);
            float checksum = 0f;
            for (int i = 0; i < accv.Length; i++)
                checksum += accv[i].X - accv[i].Y + accv[i].Z;
            Console.WriteLine(BitConverter.SingleToInt32Bits(checksum).ToString("X8"));
        }
    }
}

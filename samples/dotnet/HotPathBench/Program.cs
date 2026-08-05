#nullable disable
using System;
using Dn2Cpp.Runtime;

namespace HotPathBench
{
    // The A/B workload behind gates/measure-hotpath.sh: three kernels whose
    // arithmetic is fixed and whose [HotPath] marking is not. Compiled with
    // DN2CPP_HOTPATH_OFF defined the kernels are unmarked and land in an ordinary
    // body TU with checked element access, IEEE-exact arithmetic and no aliasing
    // claim; compiled without it they carry the full knob set. Everything else —
    // the IL, the runtime, the CMake configuration — is identical, so the
    // wall-clock difference between the two builds is the knobs and nothing else.
    //
    // This is a measurement aid, not a gate: it is never transpiled by
    // run-all-gates.sh and asserts nothing itself.
    internal static class Program
    {
        private const int Length = 4096;
        private const int Reps = 12000;

        // Integer kernels: their checksum is exact under every knob combination,
        // which is what lets the measure script cross-check the two builds for
        // equality before it believes either timing.
#if !DN2CPP_HOTPATH_OFF
        [HotPath(SkipBoundsChecks = true, NoAlias = true)]
#endif
        private static void Axpy(int[] dst, int[] src, int scale)
        {
            for (int i = 0; i < dst.Length; i++)
                dst[i] += src[i] * scale;
        }

#if !DN2CPP_HOTPATH_OFF
        [HotPath(SkipBoundsChecks = true, NoAlias = true)]
#endif
        private static long DotSpans(ReadOnlySpan<int> a, ReadOnlySpan<int> b)
        {
            long acc = 0;
            for (int i = 0; i < a.Length; i++)
                acc += (long)a[i] * b[i];
            return acc;
        }

        // The FastMath kernel: a reduction with a loop-carried dependence, which
        // is precisely the shape relaxed floating point is allowed to reassociate
        // and vectorize. Its result is therefore NOT bit-stable across the two
        // builds, so it is printed separately from the exact checksum.
#if !DN2CPP_HOTPATH_OFF
        [HotPath(FastMath = true, SkipBoundsChecks = true, NoAlias = true)]
#endif
        private static float Poly(float[] xs, float[] ws)
        {
            float acc = 0f;
            for (int i = 0; i < xs.Length; i++)
                acc += xs[i] * ws[i] + xs[i] * xs[i] * 0.5f;
            return acc;
        }

        private static void Main()
        {
            var dst = new int[Length];
            var src = new int[Length];
            var xs = new float[Length];
            var ws = new float[Length];
            for (int i = 0; i < Length; i++)
            {
                dst[i] = i & 1023;
                src[i] = (i * 7) & 511;
                xs[i] = 0.5f + (i & 63) * 0.125f;
                ws[i] = 1.25f - (i & 31) * 0.03125f;
            }

            long checksum = 0;
            float fsum = 0f;
            for (int r = 0; r < Reps; r++)
            {
                // Keep dst bounded: the scale alternates sign, so repeated
                // application neither overflows nor drifts to a constant.
                Axpy(dst, src, (r & 1) == 0 ? 3 : -3);
                checksum += DotSpans(new ReadOnlySpan<int>(dst), new ReadOnlySpan<int>(src));
                fsum += Poly(xs, ws);
            }

            Console.WriteLine(checksum);
            // Printed on its own line, and deliberately NOT part of the cross-
            // check: relaxed floating point may reassociate this reduction, and
            // over Reps accumulations that shows up well above the last bits. It
            // is here as an order-of-magnitude sanity signal — a kernel that
            // stopped computing would not land near the unmarked build's value at
            // all — not as an equality oracle.
            Console.WriteLine((long)fsum);
        }
    }
}

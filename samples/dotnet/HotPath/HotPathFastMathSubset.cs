#nullable disable
using System;
using Dn2Cpp.Runtime;

namespace HotPathFastMathSubset
{
    // [HotPath(FastMath = true)]: the marked kernels are routed into a second hot
    // TU (generated_hot_fast.cpp) compiled with relaxed floating-point semantics,
    // so their results are permitted to differ from IEEE-exact .NET by a few ULP
    // (reassociation, FMA contraction) — by an amount that varies with the host
    // compiler and ISA. A frozen snapshot of a marked kernel's output is therefore
    // impossible, and the section instead SELF-CHECKS in-program: each marked
    // kernel is compared against an unmarked twin of identical arithmetic and only
    // the verdict is printed. Under real .NET (the gate's oracle side) the
    // attribute is inert, both twins run the same IEEE-exact code, and the
    // difference is exactly zero — so the boolean holds on both sides and the
    // exact-diff oracle still applies.
    //
    // The unmarked twins' own values ARE printed — scaled and truncated to an
    // integer, not as a bit pattern — which keeps the section from degenerating
    // into a pair of booleans that a broken kernel could satisfy by being equally
    // wrong on both sides. The last bit of a long reduction is not pinnable even
    // with both sides IEEE-exact: the order the terms are summed in is the
    // compiler's to choose, and .NET and a C++ compiler need not agree on it.
    internal static class Program
    {
        // Relative tolerances, one per precision. The reassociation/contraction
        // error of an n-term positive-sum reduction is bounded by about n*u, with
        // u the unit roundoff (2^-24 for float, 2^-53 for double): at n = 128 that
        // is ~8e-6 relative for float and ~1.4e-14 for double. Both tolerances sit
        // more than an order of magnitude above their bound and many orders below
        // any arithmetic mistake, so the check is neither flaky nor vacuous.
        private const double FloatRelEps = 1e-4;
        private const double DoubleRelEps = 1e-9;

        [HotPath(FastMath = true)]
        private static float DotFast(float[] a, float[] b)
        {
            float acc = 0f;
            for (int i = 0; i < a.Length; i++)
                acc += a[i] * b[i];
            return acc;
        }

        // Byte-for-byte the same arithmetic, unmarked: it lands in an ordinary
        // body TU under the plain flags, so it stays IEEE-exact and diffable.
        private static float DotExact(float[] a, float[] b)
        {
            float acc = 0f;
            for (int i = 0; i < a.Length; i++)
                acc += a[i] * b[i];
            return acc;
        }

        [HotPath(FastMath = true)]
        private static double SumReciprocalsFast(double[] values)
        {
            double acc = 0.0;
            for (int i = 0; i < values.Length; i++)
                acc += 1.0 / values[i];
            return acc;
        }

        private static double SumReciprocalsExact(double[] values)
        {
            double acc = 0.0;
            for (int i = 0; i < values.Length; i++)
                acc += 1.0 / values[i];
            return acc;
        }

        private static bool CloseRel(double fast, double exact, double rel)
        {
            double diff = fast - exact;
            if (diff < 0.0)
                diff = -diff;
            double scale = exact < 0.0 ? -exact : exact;
            return diff <= rel * scale;
        }

        internal static void __GateEntry()
        {
            // Deliberately not exactly representable: values that round give
            // reassociation and contraction something to change, so the twins can
            // actually diverge on the native side.
            var fa = new float[128];
            var fb = new float[128];
            for (int i = 0; i < fa.Length; i++)
            {
                fa[i] = 0.3f + i * 0.07f;
                fb[i] = 2.1f - i * 0.011f;
            }
            float dotExact = DotExact(fa, fb);
            Console.WriteLine("fastmath float twin: " + CloseRel(DotFast(fa, fb), dotExact, FloatRelEps));
            // Scaled and truncated, like the double below and for the same reason:
            // a 128-term float reduction's last bit is not pinnable across .NET and
            // a C++ compiler even with both sides IEEE-exact, since the order the
            // sum is accumulated in is the compiler's to choose. The truncated
            // integer still puts a real value in the diff.
            Console.WriteLine((long)(dotExact * 100.0));

            var dv = new double[128];
            for (int i = 0; i < dv.Length; i++)
                dv[i] = 3.0 + i * 0.7;
            double sumExact = SumReciprocalsExact(dv);
            Console.WriteLine("fastmath double twin: "
                + CloseRel(SumReciprocalsFast(dv), sumExact, DoubleRelEps));
            // Scaled and truncated rather than printed as a bit pattern: an
            // integer that a few ULP cannot move, so the exact twin still
            // contributes a real value to the diff without pinning its last bit.
            Console.WriteLine((long)(sumExact * 1000000.0));
        }
    }
}

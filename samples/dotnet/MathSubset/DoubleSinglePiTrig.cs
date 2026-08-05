using System;

// The Double/Single-only members with managed BCL algorithms: the *Pi trig
// family proper (SinPi/CosPi/TanPi/SinCosPi — the AOCL-derived reduction over
// the fractional turn position), RootN and Hypot. The runtime ports are
// verbatim and compiled contraction-free, so they match real .NET bit-for-bit
// (verified over a ~79k-case random+edge sweep on this host) and every finite
// result is printed EXACTLY. NaN/Infinity/signed-zero results go through the
// P() anchors so the output stays independent of the formatter's
// infinity/NaN symbols and of the NaN sign bit.
namespace DoubleSinglePiTrig;

static class Program
{
    // Exact-print anchor: specials (NaN, +/-Inf, signed zero) map to fixed
    // tokens, every other value prints its full shortest-roundtrip form.
    private static void P(double v)
    {
        if (double.IsNaN(v))
        {
            Console.WriteLine("NaN");
        }
        else if (double.IsPositiveInfinity(v))
        {
            Console.WriteLine("+Inf");
        }
        else if (double.IsNegativeInfinity(v))
        {
            Console.WriteLine("-Inf");
        }
        else if (v == 0.0)
        {
            Console.WriteLine(double.IsNegative(v) ? "-0" : "+0");
        }
        else
        {
            Console.WriteLine(v);
        }
    }

    private static void P(float v)
    {
        if (float.IsNaN(v))
        {
            Console.WriteLine("NaN");
        }
        else if (float.IsPositiveInfinity(v))
        {
            Console.WriteLine("+Inf");
        }
        else if (float.IsNegativeInfinity(v))
        {
            Console.WriteLine("-Inf");
        }
        else if (v == 0.0f)
        {
            Console.WriteLine(float.IsNegative(v) ? "-0" : "+0");
        }
        else
        {
            Console.WriteLine(v);
        }
    }

    private static void PiTrigD(double x)
    {
        P(double.SinPi(x));
        P(double.CosPi(x));
        P(double.TanPi(x));
        var (sp, cp) = double.SinCosPi(x);
        P(sp);
        P(cp);
    }

    private static void PiTrigF(float x)
    {
        P(float.SinPi(x));
        P(float.CosPi(x));
        P(float.TanPi(x));
        var (sp, cp) = float.SinCosPi(x);
        P(sp);
        P(cp);
    }

    internal static void __GateEntry()
    {
        // ---- SinPi/CosPi/TanPi/SinCosPi (double) ----
        // Fractional positions across all four reduction intervals (the
        // (0.25, 0.5] and (0.5, 0.75] ones drive TanPi's isReciprocal
        // head/tail path; fractions > ~0.216 push the kernel past the 0.68
        // pi/4 transform), over even/odd integral parts and both signs.
        double[] sweep =
        {
            0.125, 0.25, 0.3, 0.375, 0.49, 0.5, 0.51, 0.625, 0.6875, 0.75,
            0.76, 0.875, 0.99, 1.125, 1.5, 1.75, 2.0625, 2.375, 3.5, 3.625,
            4.875, 100.25, 101.75, 12345.375, -0.125, -0.375, -0.5, -0.625,
            -0.875, -1.25, -2.5, -3.75, -101.9375,
        };
        foreach (double x in sweep)
        {
            PiTrigD(x);
        }

        // Integer inputs: exact signed zeros (SinPi/TanPi) and the parity
        // sign of CosPi.
        double[] ints = { 0.0, -0.0, 1.0, 2.0, 3.0, -1.0, -2.0, -3.0 };
        foreach (double x in ints)
        {
            PiTrigD(x);
        }

        // The small-argument tiers: the kernel cut at 2^-13 (SinPi/SinCosPi)
        // vs 2^-14 (CosPi/TanPi), the polynomial tier down to 2^-27, and the
        // x*Pi passthrough below it (subnormals included).
        double[] tiers =
        {
            1.220703125E-4, 1.2E-4, 6.103515625E-05, 6.0E-5, 1e-6,
            7.450580596923828E-09, 7e-9, 1e-9, 1e-300, 5e-324,
            -1.220703125E-4, -6.103515625E-05, -1e-6, -7e-9, -5e-324,
        };
        foreach (double x in tiers)
        {
            PiTrigD(x);
        }

        // The 2^52 (integral) and 2^53 (even integral) boundaries, where the
        // parity comes straight off the significand bits.
        double[] bigs =
        {
            4503599627370495.5, 4503599627370496.0, 4503599627370497.0,
            4503599627370498.0, 9007199254740991.0, 9007199254740992.0,
            9007199254740994.0, 1e300, -4503599627370495.5,
            -4503599627370497.0, -9007199254740991.0, -1e300,
        };
        foreach (double x in bigs)
        {
            PiTrigD(x);
        }
        PiTrigD(double.PositiveInfinity);
        PiTrigD(double.NegativeInfinity);
        PiTrigD(double.NaN);

        // ---- float variants: 2^-7/2^-13 tiers, 2^23/2^24 boundaries ----
        float[] fsweep =
        {
            0.125f, 0.25f, 0.3f, 0.375f, 0.49f, 0.5f, 0.51f, 0.625f,
            0.6875f, 0.75f, 0.76f, 0.875f, 0.99f, 1.125f, 1.5f, 2.375f,
            3.625f, 100.25f, 12345.375f, -0.125f, -0.5f, -0.625f, -0.875f,
            -1.25f, -2.5f, -101.9375f,
            0.0f, -0.0f, 1.0f, 2.0f, 3.0f, -1.0f, -2.0f,
            0.01f, 7.8125E-3f, 7.8E-3f, 1.22070313E-4f, 1.2E-4f, 1e-6f,
            float.Epsilon, -7.8125E-3f, -1.22070313E-4f, -float.Epsilon,
            8388607.5f, 8388608.0f, 8388609.0f, 8388610.0f, 16777215.0f,
            16777216.0f, 16777218.0f, 1e30f, -8388607.5f, -8388609.0f,
            -16777215.0f, float.PositiveInfinity, float.NegativeInfinity,
            float.NaN,
        };
        foreach (float x in fsweep)
        {
            PiTrigF(x);
        }

        // ---- RootN edge table ----
        // n == 0 answers NaN (no exception paths anywhere here); n == 2
        // normalizes -0.0 to +0.0; negative x demands odd n; the zero and
        // infinity rows pin the parity-dependent specials.
        int[] roots = { -3, -2, -1, 0, 1, 2, 3, 4, 5, 10 };
        double[] rootXs =
        {
            0.0, -0.0, 1.5, -1.5, 2.0, -2.0, 27.0, -27.0, 1e-3, 123.456,
            -123.456, 1e300, 1e-300, 5e-324, double.PositiveInfinity,
            double.NegativeInfinity, double.NaN,
        };
        foreach (double x in rootXs)
        {
            foreach (int n in roots)
            {
                P(double.RootN(x, n));
            }
        }
        float[] rootXf =
        {
            0.0f, -0.0f, 1.5f, -1.5f, 2.0f, -2.0f, 27.0f, -27.0f, 123.456f,
            -123.456f, 1e30f, float.Epsilon, float.PositiveInfinity,
            float.NegativeInfinity, float.NaN,
        };
        foreach (float x in rootXf)
        {
            foreach (int n in roots)
            {
                P(float.RootN(x, n));
            }
        }

        // ---- Hypot ----
        // Equal-exponent pairs (the expDiff == 0 extra tail accounting), the
        // 2^+600/2^-600 rescale branches with subnormal fixups, an exponent
        // gap beyond 54 (the ax + ay short-circuit), zeros, and the
        // infinity-beats-NaN rule.
        double[] hx =
        {
            3.0, 3.5, 5.0, 1.0, 1.0, 1e20, 1e300, 1e308, 1.5e308, 1e-300,
            5e-324, 5e-324, 1e-320, 4.9e-310, 0.0, -0.0, 0.0, -3.0, 3.0,
        };
        double[] hy =
        {
            4.0, 3.75, 7.0, 1.0, 1e20, 1.0, 1e299, 1e308, 1.2e308, 1e-301,
            1e-310, 5e-324, 2e-320, 3.1e-315, 5.0, -7.0, 0.0, 4.0, -4.0,
        };
        for (int i = 0; i < hx.Length; i++)
        {
            P(double.Hypot(hx[i], hy[i]));
        }
        P(double.Hypot(double.PositiveInfinity, 1.0));
        P(double.Hypot(1.0, double.NegativeInfinity));
        P(double.Hypot(double.PositiveInfinity, double.NaN));
        P(double.Hypot(double.NaN, double.NegativeInfinity));
        P(double.Hypot(double.NaN, 1.0));
        P(double.Hypot(double.NaN, double.NaN));
        P(float.Hypot(3.0f, 4.0f));
        P(float.Hypot(1e30f, 1e30f));
        P(float.Hypot(1e-40f, 1e-40f));
        P(float.Hypot(0.0f, -5.0f));
        P(float.Hypot(-0.0f, 0.0f));
        P(float.Hypot(float.PositiveInfinity, float.NaN));
        P(float.Hypot(float.NaN, 1.0f));
        P(float.Hypot(float.NaN, float.NaN));
    }
}

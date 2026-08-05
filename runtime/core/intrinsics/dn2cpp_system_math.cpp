// dn2cpp_system_math.cpp — System.Math / System.MathF intrinsics that need a
// real function body (the expression-shaped ones live in the emitter and the
// header templates).
//
// ILogB is a faithful port of the BCL's managed implementation (itself based
// on `ilogb`/`ilogbf` from amd/aocl-libm-ose, BSD 3-Clause): std::ilogb is
// unusable because its zero/NaN return values (FP_ILOGB0, FP_ILOGBNAN) are
// platform-defined, while .NET pins them to int.MinValue / int.MaxValue.
#include "dn2cpp_core.h"

#include <cmath>
#include <cstdint>
#include <cstring>
#include <limits>

#if defined(_MSC_VER) && !defined(__clang__)
#include <intrin.h> // _umul128 / _mul128 (MSVC targets are x64-only)
#endif

// The AOCL-derived kernels below (SinPi/CosPi/TanPi/SinCosPi/Hypot) are
// bit-for-bit ports of the BCL's managed implementations, which RyuJIT
// compiles without floating-point contraction: every `(a * b) + c` is two
// IEEE operations with an intermediate rounding. Clang's default
// -ffp-contract=on would fuse those chains into fma on arm64 and diverge in
// the last ulp, so contraction is pinned off for this whole file (none of
// the other helpers here have mul+add chains, so they are unaffected).
#if defined(_MSC_VER) && !defined(__clang__)
#pragma fp_contract (off)
#else
#pragma STDC FP_CONTRACT OFF
#endif

// Math.ILogB(double). Special values first (zero -> int.MinValue,
// infinity/NaN -> int.MaxValue); a subnormal counts its leading zeros below
// double.MinExponent (-1022); a normal value returns its unbiased exponent.
int32_t dn2cpp_math_ilogb(double x)
{
    uint64_t bits;
    std::memcpy(&bits, &x, sizeof(bits));
    int32_t biasedExp = static_cast<int32_t>((bits >> 52) & 0x7FF);
    if (biasedExp == 0) // zero or subnormal
    {
        uint64_t trailing = bits & 0x000FFFFFFFFFFFFFULL; // TrailingSignificand
        if (trailing == 0)
            return INT32_MIN;
        // MinExponent - (LeadingZeroCount(trailing) - BiasedExponentLength)
        return -1022 - (__builtin_clzll(trailing) - 11);
    }
    if (biasedExp == 0x7FF) // infinity or NaN
        return INT32_MAX;
    return biasedExp - 1023; // Exponent = BiasedExponent - ExponentBias
}

// MathF.ILogB(float) — the same algorithm over the single-precision layout
// (float.MinExponent -126, BiasedExponentLength 8, ExponentBias 127).
int32_t dn2cpp_math_ilogb_f(float x)
{
    uint32_t bits;
    std::memcpy(&bits, &x, sizeof(bits));
    int32_t biasedExp = static_cast<int32_t>((bits >> 23) & 0xFF);
    if (biasedExp == 0) // zero or subnormal
    {
        uint32_t trailing = bits & 0x007FFFFFu; // TrailingSignificand
        if (trailing == 0)
            return INT32_MIN;
        return -126 - (__builtin_clz(trailing) - 8);
    }
    if (biasedExp == 0xFF) // infinity or NaN
        return INT32_MAX;
    return biasedExp - 127;
}

// ---- Math.BigMul (64-bit overloads) ----
// The full 128-bit product: the high half is returned, the low half goes
// through *low. clang/gcc compute it through the native unsigned __int128;
// real MSVC (cl.exe) has no 128-bit integer type at all, but its dn2cpp
// targets are x64-only, so the <intrin.h> _umul128/_mul128 pair is always
// available (the same MSVC/clang split as dn2cpp_system_decimal.cpp's
// dec_u128).

#if !(defined(_MSC_VER) && !defined(__clang__))

// Math.BigMul(ulong a, ulong b, out ulong low): the unsigned high half.
uint64_t dn2cpp_math_bigmul_u64(uint64_t a, uint64_t b, uint64_t* low)
{
    unsigned __int128 p = (unsigned __int128)a * b;
    *low = (uint64_t)p;
    return (uint64_t)(p >> 64);
}

// Math.BigMul(long a, long b, out long low): the signed high half (the low
// half is the same bits as the unsigned product's).
int64_t dn2cpp_math_bigmul_i64(int64_t a, int64_t b, int64_t* low)
{
    __int128 p = (__int128)a * b;
    *low = (int64_t)(uint64_t)(unsigned __int128)p;
    return (int64_t)(uint64_t)((unsigned __int128)p >> 64);
}

#else

uint64_t dn2cpp_math_bigmul_u64(uint64_t a, uint64_t b, uint64_t* low)
{
    unsigned __int64 hi;
    *low = _umul128(a, b, &hi);
    return hi;
}

int64_t dn2cpp_math_bigmul_i64(int64_t a, int64_t b, int64_t* low)
{
    __int64 hi;
    *low = _mul128(a, b, &hi);
    return hi;
}

#endif

// ---- Math/MathF.Round(value, digits[, mode]) ----
// Faithful ports of the BCL's scale/round/unscale: validate digits, and only
// scale when |value| is below the limit (1e16 / 1e8f) — beyond it the value is
// already integral and passes through, which also means an invalid mode is
// only detected below the limit, exactly like the BCL. The inner rounding is
// dn2cpp_math_round_mode's semantics; the float variant runs it over the
// float overloads so MathF arithmetic never widens to double.

// Math.Round(double value, int digits, MidpointRounding mode): digits 0..15.
double dn2cpp_math_round_digits(double value, int32_t digits, int32_t mode)
{
    static const double k_pow10[16] = {
        1e0, 1e1, 1e2, 1e3, 1e4, 1e5, 1e6, 1e7, 1e8,
        1e9, 1e10, 1e11, 1e12, 1e13, 1e14, 1e15,
    };
    if (static_cast<uint32_t>(digits) > 15u)
        dn2cpp_throw_argument_out_of_range();
    if (std::fabs(value) < 1e16)
    {
        double power10 = k_pow10[digits];
        value = dn2cpp_math_round_mode(value * power10, mode) / power10;
    }
    return value;
}

// MathF.Round(float value, int digits, MidpointRounding mode): digits 0..6,
// every operation in single precision (the BCL's own algorithm is float
// throughout, so double-rounding through a wider type could diverge).
float dn2cpp_math_round_digits_f(float value, int32_t digits, int32_t mode)
{
    static const float k_pow10f[7] = { 1e0f, 1e1f, 1e2f, 1e3f, 1e4f, 1e5f, 1e6f };
    if (static_cast<uint32_t>(digits) > 6u)
        dn2cpp_throw_argument_out_of_range();
    if (std::fabs(value) < 1e8f)
    {
        float power10 = k_pow10f[digits];
        float scaled = value * power10;
        float rounded;
        switch (mode) // dn2cpp_math_round_mode over the float overloads
        {
            case 0: rounded = std::nearbyint(scaled); break; // ToEven
            case 1: rounded = std::round(scaled); break;     // AwayFromZero
            case 2: rounded = std::trunc(scaled); break;     // ToZero
            case 3: rounded = std::floor(scaled); break;     // ToNegativeInfinity
            case 4: rounded = std::ceil(scaled); break;      // ToPositiveInfinity
            default: dn2cpp_throw_argument();                // catchable, like the BCL
        }
        value = rounded / power10;
    }
    return value;
}

// ---- Double/Single.SinPi / CosPi / TanPi / SinCosPi ----
// Faithful ports of the BCL's managed implementations (themselves based on
// `sinpi`/`cospi`/`tanpi` from amd/aocl-libm-ose, BSD 3-Clause): there is no
// libm counterpart with the same last-ulp behavior. The double flavor reduces
// |x| by its integral part and dispatches the fractional position across four
// quarter-turn intervals into the shared kernels; the float flavor follows
// the same structure with single-precision reduction but computes the
// kernels in double and rounds once at the end, exactly like Single.cs.

namespace {

inline uint64_t f64_bits(double v)
{
    uint64_t b;
    std::memcpy(&b, &v, sizeof(b));
    return b;
}

inline double f64_from_bits(uint64_t b)
{
    double v;
    std::memcpy(&v, &b, sizeof(v));
    return v;
}

inline uint32_t f32_bits(float v)
{
    uint32_t b;
    std::memcpy(&b, &v, sizeof(b));
    return b;
}

inline float f32_from_bits(uint32_t b)
{
    float v;
    std::memcpy(&v, &b, sizeof(v));
    return v;
}

// The BCL's double.NaN / float.NaN constants are the NEGATIVE quiet NaN
// (0.0 / 0.0 folded by the C# compiler), and every NaN result below is an
// assignment of that constant — pinned here bit-for-bit so the ports match
// real .NET exactly (std::numeric_limits' quiet_NaN() is the positive one).
inline double dotnet_nan() { return f64_from_bits(0xFFF8000000000000ULL); }
inline float dotnet_nan_f() { return f32_from_bits(0xFFC00000u); }

constexpr double kPi = 3.141592653589793;
constexpr float kPiF = 3.14159265f;

// Double.CosForIntervalPiBy4 — minimax expansion of cos on [0, pi/4]; the
// xTail correction term is threaded verbatim (the public entry points always
// pass 0.0).
double cos_for_interval_piby4(double x, double xTail)
{
    const double C1 = +0.41666666666666665390037E-1;
    const double C2 = -0.13888888888887398280412E-2;
    const double C3 = +0.248015872987670414957399E-4;
    const double C4 = -0.275573172723441909470836E-6;
    const double C5 = +0.208761463822329611076335E-8;
    const double C6 = -0.113826398067944859590880E-10;

    double xx = x * x;

    double tmp1 = 0.5 * xx;
    double tmp2 = 1.0 - tmp1;

    double result = C6;

    result = (result * xx) + C5;
    result = (result * xx) + C4;
    result = (result * xx) + C3;
    result = (result * xx) + C2;
    result = (result * xx) + C1;

    result *= (xx * xx);
    result += 1.0 - tmp2 - tmp1 - (x * xTail);
    result += tmp2;

    return result;
}

// Double.SinForIntervalPiBy4 — minimax expansion of sin on [0, pi/4].
double sin_for_interval_piby4(double x, double xTail)
{
    const double C1 = -0.166666666666666646259241729;
    const double C2 = +0.833333333333095043065222816E-2;
    const double C3 = -0.19841269836761125688538679E-3;
    const double C4 = +0.275573161037288022676895908448E-5;
    const double C5 = -0.25051132068021699772257377197E-7;
    const double C6 = +0.159181443044859136852668200E-9;

    double xx = x * x;
    double xxx = xx * x;

    double result = C6;

    result = (result * xx) + C5;
    result = (result * xx) + C4;
    result = (result * xx) + C3;
    result = (result * xx) + C2;

    if (xTail == 0.0)
    {
        result = (xx * result) + C1;
        result = (xxx * result) + x;
    }
    else
    {
        result = x - ((xx * ((0.5 * xTail) - (xxx * result))) - xTail - (xxx * C1));
    }

    return result;
}

// Double.TanForIntervalPiBy4 — Remez [2, 3] approximation of tan on
// [0, 0.68], with the pi/4 transform for arguments beyond it and the
// head/tail-accurate reciprocal for the cotangent intervals.
double tan_for_interval_piby4(double x, double xTail, bool isReciprocal)
{
    const double PiBy4Head = 7.85398163397448278999E-01;
    const double PiBy4Tail = 3.06161699786838240164E-17;

    int transform = 0;

    if (x > +0.68)
    {
        transform = 1;
        x = (PiBy4Head - x) + (PiBy4Tail - xTail);
        xTail = 0.0;
    }
    else if (x < -0.68)
    {
        transform = -1;
        x = (PiBy4Head + x) + (PiBy4Tail + xTail);
        xTail = 0.0;
    }

    double tmp1 = (x * x) + (2.0 * x * xTail);

    double denominator = -0.232371494088563558304549252913E-3;
    denominator = +0.260656620398645407524064091208E-1 + (denominator * tmp1);
    denominator = -0.515658515729031149329237816945E+0 + (denominator * tmp1);
    denominator = +0.111713747927937668539901657944E+1 + (denominator * tmp1);

    double numerator = +0.224044448537022097264602535574E-3;
    numerator = -0.229345080057565662883358588111E-1 + (numerator * tmp1);
    numerator = +0.372379159759792203640806338901E+0 + (numerator * tmp1);

    double tmp2 = x * tmp1;
    tmp2 *= numerator / denominator;
    tmp2 += xTail;

    double result = x + tmp2;

    if (transform != 0)
    {
        if (isReciprocal)
        {
            result = (transform * (2 * result / (result - 1))) - 1.0;
        }
        else
        {
            result = transform * (1.0 - (2 * result / (1 + result)));
        }
    }
    else if (isReciprocal)
    {
        // Compute -1.0 / (x + tmp2) accurately.
        uint64_t bits = f64_bits(result);
        bits &= 0xFFFFFFFF00000000ULL;

        double z1 = f64_from_bits(bits);
        double z2 = tmp2 - (z1 - x);

        double reciprocal = -1.0 / result;

        bits = f64_bits(reciprocal);
        bits &= 0xFFFFFFFF00000000ULL;

        double reciprocalHead = f64_from_bits(bits);
        result = reciprocalHead + (reciprocal * (1.0 + (reciprocalHead * z1) + (reciprocalHead * z2)));
    }

    return result;
}

// Single kernels — the same C1..C4 double coefficients evaluated in double
// with one final rounding to float, exactly like Single.cs (the squaring
// `x * x` happens in float first, then widens, matching the C# operand types).
float cos_for_interval_piby4_f(float x)
{
    const double C1 = +0.41666666666666665390037E-1;
    const double C2 = -0.13888888888887398280412E-2;
    const double C3 = +0.248015872987670414957399E-4;
    const double C4 = -0.275573172723441909470836E-6;

    double xx = x * x;
    double result = C4;

    result = (result * xx) + C3;
    result = (result * xx) + C2;
    result = (result * xx) + C1;

    result *= xx * xx;
    result += 1.0 - (0.5 * xx);

    return (float)result;
}

float sin_for_interval_piby4_f(float x)
{
    const double C1 = -0.166666666666666646259241729;
    const double C2 = +0.833333333333095043065222816E-2;
    const double C3 = -0.19841269836761125688538679E-3;
    const double C4 = +0.275573161037288022676895908448E-5;

    double xx = x * x;
    double result = C4;

    result = (result * xx) + C3;
    result = (result * xx) + C2;
    result = (result * xx) + C1;

    result *= x * xx;
    result += x;

    return (float)result;
}

float tan_for_interval_piby4_f(float x, bool isReciprocal)
{
    double xx = x * x;

    double denominator = +0.1844239256901656082986661E-1;
    denominator = -0.51396505478854532132342E+0 + (denominator * xx);
    denominator = +0.115588821434688393452299E+1 + (denominator * xx);

    double numerator = -0.172032480471481694693109E-1;
    numerator = 0.385296071263995406715129E+0 + (numerator * xx);

    double result = x * xx;
    result *= numerator / denominator;
    result += x;

    if (isReciprocal)
    {
        result = -1.0 / result;
    }

    return (float)result;
}

} // namespace

// double.SinPi(x): sin(pi * x) computed on the fractional turn position, so
// integral x answers an exact signed zero instead of accumulating pi-multiple
// rounding error.
double dn2cpp_math_sinpi(double x)
{
    double result;

    if (std::isfinite(x))
    {
        double ax = std::fabs(x);

        if (ax < 4503599627370496.0)            // |x| < 2^52
        {
            if (ax > 0.25)
            {
                int64_t integral = (int64_t)ax;

                double fractional = ax - integral;
                double sign = ((x > 0.0) ? +1.0 : -1.0) * (((integral & 1) != 0) ? -1.0 : +1.0);

                if (fractional <= 0.25)
                {
                    result = (fractional != 0.00) ? sign * sin_for_interval_piby4(fractional * kPi, 0.0) : x * 0.0;
                }
                else if (fractional <= 0.50)
                {
                    result = (fractional != 0.50) ? sign * cos_for_interval_piby4((0.5 - fractional) * kPi, 0.0) : sign;
                }
                else if (fractional <= 0.75)
                {
                    result = sign * cos_for_interval_piby4((fractional - 0.5) * kPi, 0.0);
                }
                else
                {
                    result = sign * sin_for_interval_piby4((1.0 - fractional) * kPi, 0.0);
                }
            }
            else if (ax >= 1.220703125E-4)          // |x| >= 2^-13
            {
                result = sin_for_interval_piby4(x * kPi, 0.0);
            }
            else if (ax >= 7.450580596923828E-09)   // |x| >= 2^-27
            {
                double value = x * kPi;
                result = value - (value * value * value * (1.0 / 6.0));
            }
            else
            {
                result = x * kPi;
            }
        }
        else
        {
            // x is an integer
            result = x * 0.0;
        }
    }
    else
    {
        result = dotnet_nan();
    }

    return result;
}

// double.CosPi(x). The 2^52..2^53 integral range reads the parity straight
// off the significand's low bit (the value's units bit at that scale).
double dn2cpp_math_cospi(double x)
{
    double result;

    if (std::isfinite(x))
    {
        double ax = std::fabs(x);

        if (ax < 4503599627370496.0)            // |x| < 2^52
        {
            if (ax > 0.25)
            {
                int64_t integral = (int64_t)ax;

                double fractional = ax - integral;
                double sign = ((integral & 1) != 0) ? -1.0 : +1.0;

                if (fractional <= 0.25)
                {
                    result = (fractional != 0.00) ? sign * cos_for_interval_piby4(fractional * kPi, 0.0) : sign;
                }
                else if (fractional <= 0.50)
                {
                    result = (fractional != 0.50) ? sign * sin_for_interval_piby4((0.5 - fractional) * kPi, 0.0) : 0.0;
                }
                else if (fractional <= 0.75)
                {
                    result = -sign * sin_for_interval_piby4((fractional - 0.5) * kPi, 0.0);
                }
                else
                {
                    result = -sign * cos_for_interval_piby4((1.0 - fractional) * kPi, 0.0);
                }
            }
            else if (ax >= 6.103515625E-05)         // |x| >= 2^-14
            {
                result = cos_for_interval_piby4(x * kPi, 0.0);
            }
            else if (ax >= 7.450580596923828E-09)   // |x| >= 2^-27
            {
                double value = x * kPi;
                result = 1.0 - (value * value * 0.5);
            }
            else
            {
                result = 1.0;
            }
        }
        else if (ax < 9007199254740992.0)       // |x| < 2^53
        {
            // x is an integer
            int64_t bits = (int64_t)f64_bits(ax);
            result = ((bits & 1) != 0) ? -1.0 : +1.0;
        }
        else
        {
            // x is an even integer
            result = 1.0;
        }
    }
    else
    {
        result = dotnet_nan();
    }

    return result;
}

// double.TanPi(x): exact signed zeros/infinities at the half-turn positions.
double dn2cpp_math_tanpi(double x)
{
    double result;

    if (std::isfinite(x))
    {
        double ax = std::fabs(x);
        double sign = (x > 0.0) ? +1.0 : -1.0;

        if (ax < 4503599627370496.0)            // |x| < 2^52
        {
            if (ax > 0.25)
            {
                int64_t integral = (int64_t)ax;
                double fractional = ax - integral;

                if (fractional <= 0.25)
                {
                    result = (fractional != 0.00)
                        ? sign * tan_for_interval_piby4(fractional * kPi, 0.0, /*isReciprocal*/ false)
                        : sign * (((integral & 1) != 0) ? -0.0 : +0.0);
                }
                else if (fractional <= 0.50)
                {
                    result = (fractional != 0.50)
                        ? -sign * tan_for_interval_piby4((0.5 - fractional) * kPi, 0.0, /*isReciprocal*/ true)
                        : +sign * (((integral & 1) != 0) ? -std::numeric_limits<double>::infinity()
                                                         : +std::numeric_limits<double>::infinity());
                }
                else if (fractional <= 0.75)
                {
                    result = +sign * tan_for_interval_piby4((fractional - 0.5) * kPi, 0.0, /*isReciprocal*/ true);
                }
                else
                {
                    result = -sign * tan_for_interval_piby4((1.0 - fractional) * kPi, 0.0, /*isReciprocal*/ false);
                }
            }
            else if (ax >= 6.103515625E-05)         // |x| >= 2^-14
            {
                result = tan_for_interval_piby4(x * kPi, 0.0, /*isReciprocal*/ false);
            }
            else if (ax >= 7.450580596923828E-09)   // |x| >= 2^-27
            {
                double value = x * kPi;
                result = value + (value * value * value * (1.0 / 3.0));
            }
            else
            {
                result = x * kPi;
            }
        }
        else if (ax < 9007199254740992.0)       // |x| < 2^53
        {
            // x is an integer
            int64_t bits = (int64_t)f64_bits(ax);
            result = sign * (((bits & 1) != 0) ? -0.0 : +0.0);
        }
        else
        {
            // x is an even integer
            result = sign * 0.0;
        }
    }
    else
    {
        result = dotnet_nan();
    }

    return result;
}

// double.SinCosPi(x) — both kernels over one reduction. Note the small-
// argument thresholds follow SinPi's tiers (2^-13), not CosPi's 2^-14,
// exactly like the BCL.
void dn2cpp_math_sincospi(double x, double* sinResult, double* cosResult)
{
    double sinPi;
    double cosPi;

    if (std::isfinite(x))
    {
        double ax = std::fabs(x);

        if (ax < 4503599627370496.0)            // |x| < 2^52
        {
            if (ax > 0.25)
            {
                int64_t integral = (int64_t)ax;

                double fractional = ax - integral;
                double sign = ((integral & 1) != 0) ? -1.0 : +1.0;

                double sinSign = ((x > 0.0) ? +1.0 : -1.0) * sign;
                double cosSign = sign;

                if (fractional <= 0.25)
                {
                    if (fractional != 0.00)
                    {
                        double value = fractional * kPi;

                        sinPi = sinSign * sin_for_interval_piby4(value, 0.0);
                        cosPi = cosSign * cos_for_interval_piby4(value, 0.0);
                    }
                    else
                    {
                        sinPi = x * 0.0;
                        cosPi = cosSign;
                    }
                }
                else if (fractional <= 0.50)
                {
                    if (fractional != 0.50)
                    {
                        double value = (0.5 - fractional) * kPi;

                        sinPi = sinSign * cos_for_interval_piby4(value, 0.0);
                        cosPi = cosSign * sin_for_interval_piby4(value, 0.0);
                    }
                    else
                    {
                        sinPi = sinSign;
                        cosPi = 0.0;
                    }
                }
                else if (fractional <= 0.75)
                {
                    double value = (fractional - 0.5) * kPi;

                    sinPi = +sinSign * cos_for_interval_piby4(value, 0.0);
                    cosPi = -cosSign * sin_for_interval_piby4(value, 0.0);
                }
                else
                {
                    double value = (1.0 - fractional) * kPi;

                    sinPi = +sinSign * sin_for_interval_piby4(value, 0.0);
                    cosPi = -cosSign * cos_for_interval_piby4(value, 0.0);
                }
            }
            else if (ax >= 1.220703125E-4)          // |x| >= 2^-13
            {
                double value = x * kPi;

                sinPi = sin_for_interval_piby4(value, 0.0);
                cosPi = cos_for_interval_piby4(value, 0.0);
            }
            else if (ax >= 7.450580596923828E-09)   // |x| >= 2^-27
            {
                double value = x * kPi;
                double valueSq = value * value;

                sinPi = value - (valueSq * value * (1.0 / 6.0));
                cosPi = 1.0 - (valueSq * 0.5);
            }
            else
            {
                sinPi = x * kPi;
                cosPi = 1.0;
            }
        }
        else if (ax < 9007199254740992.0)       // |x| < 2^53
        {
            // x is an integer
            sinPi = x * 0.0;

            int64_t bits = (int64_t)f64_bits(ax);
            cosPi = ((bits & 1) != 0) ? -1.0 : +1.0;
        }
        else
        {
            // x is an even integer
            sinPi = x * 0.0;
            cosPi = 1.0;
        }
    }
    else
    {
        sinPi = dotnet_nan();
        cosPi = dotnet_nan();
    }

    *sinResult = sinPi;
    *cosResult = cosPi;
}

// float.SinPi(x) — single-precision reduction (2^23/2^24 integral
// boundaries, 2^-7/2^-13 small-argument tiers), double kernels.
float dn2cpp_math_sinpi_f(float x)
{
    float result;

    if (std::isfinite(x))
    {
        float ax = std::fabs(x);

        if (ax < 8388608.0f)                    // |x| < 2^23
        {
            if (ax > 0.25f)
            {
                int32_t integral = (int32_t)ax;

                float fractional = ax - integral;
                float sign = ((x > 0.0f) ? +1.0f : -1.0f) * (((integral & 1) != 0) ? -1.0f : +1.0f);

                if (fractional <= 0.25f)
                {
                    result = (fractional != 0.00f) ? sign * sin_for_interval_piby4_f(fractional * kPiF) : x * 0.0f;
                }
                else if (fractional <= 0.50f)
                {
                    result = (fractional != 0.50f) ? sign * cos_for_interval_piby4_f((0.5f - fractional) * kPiF) : sign;
                }
                else if (fractional <= 0.75f)
                {
                    result = sign * cos_for_interval_piby4_f((fractional - 0.5f) * kPiF);
                }
                else
                {
                    result = sign * sin_for_interval_piby4_f((1.0f - fractional) * kPiF);
                }
            }
            else if (ax >= 7.8125E-3f)          // |x| >= 2^-7
            {
                result = sin_for_interval_piby4_f(x * kPiF);
            }
            else if (ax >= 1.22070313E-4f)      // |x| >= 2^-13
            {
                float value = x * kPiF;
                result = value - (value * value * value * (1.0f / 6.0f));
            }
            else
            {
                result = x * kPiF;
            }
        }
        else
        {
            // x is an integer
            result = x * 0.0f;
        }
    }
    else
    {
        result = dotnet_nan_f();
    }

    return result;
}

// float.CosPi(x). The (0.5, 0.75] interval bound is compared as a DOUBLE
// literal in Single.cs (`fractional <= 0.75`, unlike the other 0.75f
// bounds); 0.75 is exact in both widths so the semantics coincide, but the
// quirk is transcribed for fidelity.
float dn2cpp_math_cospi_f(float x)
{
    float result;

    if (std::isfinite(x))
    {
        float ax = std::fabs(x);

        if (ax < 8388608.0f)                    // |x| < 2^23
        {
            if (ax > 0.25f)
            {
                int32_t integral = (int32_t)ax;

                float fractional = ax - integral;
                float sign = ((integral & 1) != 0) ? -1.0f : +1.0f;

                if (fractional <= 0.25f)
                {
                    result = (fractional != 0.00f) ? sign * cos_for_interval_piby4_f(fractional * kPiF) : sign;
                }
                else if (fractional <= 0.50f)
                {
                    result = (fractional != 0.50f) ? sign * sin_for_interval_piby4_f((0.5f - fractional) * kPiF) : 0.0f;
                }
                else if (fractional <= 0.75)
                {
                    result = -sign * sin_for_interval_piby4_f((fractional - 0.5f) * kPiF);
                }
                else
                {
                    result = -sign * cos_for_interval_piby4_f((1.0f - fractional) * kPiF);
                }
            }
            else if (ax >= 7.8125E-3f)          // |x| >= 2^-7
            {
                result = cos_for_interval_piby4_f(x * kPiF);
            }
            else if (ax >= 1.22070313E-4f)      // |x| >= 2^-13
            {
                float value = x * kPiF;
                result = 1.0f - (value * value * 0.5f);
            }
            else
            {
                result = 1.0f;
            }
        }
        else if (ax < 16777216.0f)              // |x| < 2^24
        {
            // x is an integer
            int32_t bits = (int32_t)f32_bits(ax);
            result = ((bits & 1) != 0) ? -1.0f : +1.0f;
        }
        else
        {
            // x is an even integer
            result = 1.0f;
        }
    }
    else
    {
        result = dotnet_nan_f();
    }

    return result;
}

// float.TanPi(x).
float dn2cpp_math_tanpi_f(float x)
{
    float result;

    if (std::isfinite(x))
    {
        float ax = std::fabs(x);
        float sign = (x > 0.0f) ? +1.0f : -1.0f;

        if (ax < 8388608.0f)                    // |x| < 2^23
        {
            if (ax > 0.25f)
            {
                int32_t integral = (int32_t)ax;
                float fractional = ax - integral;

                if (fractional <= 0.25f)
                {
                    result = (fractional != 0.00f)
                        ? sign * tan_for_interval_piby4_f(fractional * kPiF, /*isReciprocal*/ false)
                        : sign * (((integral & 1) != 0) ? -0.0f : +0.0f);
                }
                else if (fractional <= 0.50f)
                {
                    result = (fractional != 0.50f)
                        ? -sign * tan_for_interval_piby4_f((0.5f - fractional) * kPiF, /*isReciprocal*/ true)
                        : +sign * (((integral & 1) != 0) ? -std::numeric_limits<float>::infinity()
                                                         : +std::numeric_limits<float>::infinity());
                }
                else if (fractional <= 0.75f)
                {
                    result = +sign * tan_for_interval_piby4_f((fractional - 0.5f) * kPiF, /*isReciprocal*/ true);
                }
                else
                {
                    result = -sign * tan_for_interval_piby4_f((1.0f - fractional) * kPiF, /*isReciprocal*/ false);
                }
            }
            else if (ax >= 7.8125E-3f)          // |x| >= 2^-7
            {
                result = tan_for_interval_piby4_f(x * kPiF, /*isReciprocal*/ false);
            }
            else if (ax >= 1.22070313E-4f)      // |x| >= 2^-13
            {
                float value = x * kPiF;
                result = value + (value * value * value * (1.0f / 3.0f));
            }
            else
            {
                result = x * kPiF;
            }
        }
        else if (ax < 16777216.0f)              // |x| < 2^24
        {
            // x is an integer
            int32_t bits = (int32_t)f32_bits(ax);
            result = sign * (((bits & 1) != 0) ? -0.0f : +0.0f);
        }
        else
        {
            // x is an even integer
            result = sign * 0.0f;
        }
    }
    else
    {
        result = dotnet_nan_f();
    }

    return result;
}

// float.SinCosPi(x) — SinPi's float tiers, both kernels over one reduction.
void dn2cpp_math_sincospi_f(float x, float* sinResult, float* cosResult)
{
    float sinPi;
    float cosPi;

    if (std::isfinite(x))
    {
        float ax = std::fabs(x);

        if (ax < 8388608.0f)                    // |x| < 2^23
        {
            if (ax > 0.25f)
            {
                int32_t integral = (int32_t)ax;

                float fractional = ax - integral;
                float sign = ((integral & 1) != 0) ? -1.0f : +1.0f;

                float sinSign = ((x > 0.0f) ? +1.0f : -1.0f) * sign;
                float cosSign = sign;

                if (fractional <= 0.25f)
                {
                    if (fractional != 0.00f)
                    {
                        float value = fractional * kPiF;

                        sinPi = sinSign * sin_for_interval_piby4_f(value);
                        cosPi = cosSign * cos_for_interval_piby4_f(value);
                    }
                    else
                    {
                        sinPi = x * 0.0f;
                        cosPi = cosSign;
                    }
                }
                else if (fractional <= 0.50f)
                {
                    if (fractional != 0.50f)
                    {
                        float value = (0.5f - fractional) * kPiF;

                        sinPi = sinSign * cos_for_interval_piby4_f(value);
                        cosPi = cosSign * sin_for_interval_piby4_f(value);
                    }
                    else
                    {
                        sinPi = sinSign;
                        cosPi = 0.0f;
                    }
                }
                else if (fractional <= 0.75f)
                {
                    float value = (fractional - 0.5f) * kPiF;

                    sinPi = +sinSign * cos_for_interval_piby4_f(value);
                    cosPi = -cosSign * sin_for_interval_piby4_f(value);
                }
                else
                {
                    float value = (1.0f - fractional) * kPiF;

                    sinPi = +sinSign * sin_for_interval_piby4_f(value);
                    cosPi = -cosSign * cos_for_interval_piby4_f(value);
                }
            }
            else if (ax >= 7.8125E-3f)          // |x| >= 2^-7
            {
                float value = x * kPiF;

                sinPi = sin_for_interval_piby4_f(value);
                cosPi = cos_for_interval_piby4_f(value);
            }
            else if (ax >= 1.22070313E-4f)      // |x| >= 2^-13
            {
                float value = x * kPiF;
                float valueSq = value * value;

                sinPi = value - (valueSq * value * (1.0f / 6.0f));
                cosPi = 1.0f - (valueSq * 0.5f);
            }
            else
            {
                sinPi = x * kPiF;
                cosPi = 1.0f;
            }
        }
        else if (ax < 16777216.0f)              // |x| < 2^24
        {
            // x is an integer
            sinPi = x * 0.0f;

            int32_t bits = (int32_t)f32_bits(ax);
            cosPi = ((bits & 1) != 0) ? -1.0f : +1.0f;
        }
        else
        {
            // x is an even integer
            sinPi = x * 0.0f;
            cosPi = 1.0f;
        }
    }
    else
    {
        sinPi = dotnet_nan_f();
        cosPi = dotnet_nan_f();
    }

    *sinResult = sinPi;
    *cosResult = cosPi;
}

// ---- Double/Single.RootN ----
// Faithful ports of the BCL's managed RootN: n==2/3 route to sqrt/cbrt, the
// general case is pow(|x|, 1/n) with the sign copied back (no correction
// step), and every zero/infinity/NaN/parity special case is pinned. The
// float flavor's general case runs the DOUBLE pow and rounds once — that is
// Single.cs's own `(float)double.Pow(Abs(x), 1.0 / n)`, not MathF.Pow.

namespace {

// double.IsOddInteger((double)n) / IsEvenInteger — int32 -> double is exact,
// so the parity is n's own low bit.
inline bool is_odd_i32(int32_t n) { return (n & 1) != 0; }

// float.IsOddInteger((float)n) / IsEvenInteger((float)n): Single.cs converts
// n to FLOAT first, so |n| > 2^24 can lose its low bit to rounding before
// the parity test — transcribed faithfully.
inline bool is_odd_i32_as_f32(int32_t n)
{
    float f = (float)n;
    return (std::trunc(f) == f) && (std::fabs(std::fmod(f, 2.0f)) == 1.0f);
}

inline bool is_even_i32_as_f32(int32_t n)
{
    float f = (float)n;
    return (std::trunc(f) == f) && (std::fmod(std::fabs(f), 2.0f) == 0.0f);
}

// Double.RootN.PositiveN / NegativeN — one body each; they share the finite
// nonzero expression (1.0 / n is negative for NegativeN) and differ in the
// zero/negative-infinity special cases.
double rootn_positive_n(double x, int32_t n)
{
    double result;

    if (std::isfinite(x))
    {
        if (x != 0)
        {
            if ((x > 0) || is_odd_i32(n))
            {
                result = std::pow(std::fabs(x), 1.0 / n);
                result = std::copysign(result, x);
            }
            else
            {
                result = dotnet_nan();
            }
        }
        else if (!is_odd_i32(n))
        {
            result = 0.0;
        }
        else
        {
            result = std::copysign(0.0, x);
        }
    }
    else if (std::isnan(x))
    {
        result = dotnet_nan();
    }
    else if (x > 0)
    {
        result = std::numeric_limits<double>::infinity();
    }
    else
    {
        result = is_odd_i32(n) ? -std::numeric_limits<double>::infinity()
                               : dotnet_nan();
    }

    return result;
}

double rootn_negative_n(double x, int32_t n)
{
    double result;

    if (std::isfinite(x))
    {
        if (x != 0)
        {
            if ((x > 0) || is_odd_i32(n))
            {
                result = std::pow(std::fabs(x), 1.0 / n);
                result = std::copysign(result, x);
            }
            else
            {
                result = dotnet_nan();
            }
        }
        else if (!is_odd_i32(n))
        {
            result = std::numeric_limits<double>::infinity();
        }
        else
        {
            result = std::copysign(std::numeric_limits<double>::infinity(), x);
        }
    }
    else if (std::isnan(x))
    {
        result = dotnet_nan();
    }
    else if (x > 0)
    {
        result = 0.0;
    }
    else
    {
        result = is_odd_i32(n) ? -0.0 : dotnet_nan();
    }

    return result;
}

// Single.RootN's local functions: the parity tests on the finite paths go
// through the int -> float conversion (see is_odd_i32_as_f32), while the
// negative-infinity path tests int.IsOddInteger(n) directly — both quirks
// transcribed.
float rootn_positive_n_f(float x, int32_t n)
{
    float result;

    if (std::isfinite(x))
    {
        if (x != 0)
        {
            if ((x > 0) || is_odd_i32_as_f32(n))
            {
                result = (float)std::pow((double)std::fabs(x), 1.0 / n);
                result = std::copysign(result, x);
            }
            else
            {
                result = dotnet_nan_f();
            }
        }
        else if (is_even_i32_as_f32(n))
        {
            result = 0.0f;
        }
        else
        {
            result = std::copysign(0.0f, x);
        }
    }
    else if (std::isnan(x))
    {
        result = dotnet_nan_f();
    }
    else if (x > 0)
    {
        result = std::numeric_limits<float>::infinity();
    }
    else
    {
        result = is_odd_i32(n) ? -std::numeric_limits<float>::infinity()
                               : dotnet_nan_f();
    }

    return result;
}

float rootn_negative_n_f(float x, int32_t n)
{
    float result;

    if (std::isfinite(x))
    {
        if (x != 0)
        {
            if ((x > 0) || is_odd_i32_as_f32(n))
            {
                result = (float)std::pow((double)std::fabs(x), 1.0 / n);
                result = std::copysign(result, x);
            }
            else
            {
                result = dotnet_nan_f();
            }
        }
        else if (is_even_i32_as_f32(n))
        {
            result = std::numeric_limits<float>::infinity();
        }
        else
        {
            result = std::copysign(std::numeric_limits<float>::infinity(), x);
        }
    }
    else if (std::isnan(x))
    {
        result = dotnet_nan_f();
    }
    else if (x > 0)
    {
        result = 0.0f;
    }
    else
    {
        result = is_odd_i32(n) ? -0.0f : dotnet_nan_f();
    }

    return result;
}

} // namespace

// double.RootN(x, n). n == 0 answers NaN (no throw); RootN(±0.0, 2)
// normalizes to +0.0 through the `x != 0.0 ? Sqrt : 0.0` guard.
double dn2cpp_math_rootn(double x, int32_t n)
{
    double result;

    if (n > 0)
    {
        if (n == 2)
        {
            result = (x != 0.0) ? std::sqrt(x) : 0.0;
        }
        else if (n == 3)
        {
            result = std::cbrt(x);
        }
        else
        {
            result = rootn_positive_n(x, n);
        }
    }
    else if (n < 0)
    {
        result = rootn_negative_n(x, n);
    }
    else
    {
        result = dotnet_nan();
    }

    return result;
}

// float.RootN(x, n) — n == 2/3 keep single precision (sqrtf/cbrtf, like
// MathF.Sqrt/Cbrt); the general case is the double pow (see above).
float dn2cpp_math_rootn_f(float x, int32_t n)
{
    float result;

    if (n > 0)
    {
        if (n == 2)
        {
            result = (x != 0.0f) ? std::sqrt(x) : 0.0f;
        }
        else if (n == 3)
        {
            result = std::cbrt(x);
        }
        else
        {
            result = rootn_positive_n_f(x, n);
        }
    }
    else if (n < 0)
    {
        result = rootn_negative_n_f(x, n);
    }
    else
    {
        result = dotnet_nan_f();
    }

    return result;
}

// ---- Double/Single.Hypot ----
// Double.Hypot is the BCL's managed AOCL head/tail algorithm — std::hypot's
// last ulp can differ, so it is ported verbatim: the operands are rescaled by
// 2^±600 out of the overflow/underflow ranges (subnormals get the implicit-
// bit fixup), the squares are accumulated with an exact tail, and only an
// exponent gap beyond 54 short-circuits to ax + ay. The `ax == 0.0f` float-
// literal comparisons transcribe C#'s own oddity (0.0f widens to 0.0 —
// semantics identical). float.Hypot is Single.cs's simple form: the exact
// double sum of squares under one sqrt.
double dn2cpp_math_hypot(double x, double y)
{
    double result;

    if (std::isfinite(x) && std::isfinite(y))
    {
        double ax = std::fabs(x);
        double ay = std::fabs(y);

        if (ax == 0.0f)
        {
            result = ay;
        }
        else if (ay == 0.0f)
        {
            result = ax;
        }
        else
        {
            uint64_t xBits = f64_bits(ax);
            uint64_t yBits = f64_bits(ay);

            uint32_t xExp = (uint32_t)((xBits >> 52) & 0x7FF);
            uint32_t yExp = (uint32_t)((yBits >> 52) & 0x7FF);

            int32_t expDiff = (int32_t)(xExp - yExp);
            double expFix = 1.0;

            if ((expDiff <= 54) && (expDiff >= -54))
            {
                if ((xExp > 1523) || (yExp > 1523))
                {
                    // To prevent overflow, scale down by 2^+600
                    expFix = 4.149515568880993E+180;

                    xBits -= 0x2580000000000000ULL;
                    yBits -= 0x2580000000000000ULL;
                }
                else if ((xExp < 523) || (yExp < 523))
                {
                    // To prevent underflow, scale up by 2^-600
                    expFix = 2.409919865102884E-181;

                    xBits += 0x2580000000000000ULL;
                    yBits += 0x2580000000000000ULL;

                    // For subnormal values, do an additional fixing up changing the
                    // adjustment to scale up by 2^601 instead and then subtract a
                    // correction of 2^601 to account for the implicit bit.

                    if (xExp == 0) // x is subnormal
                    {
                        xBits += 0x0010000000000000ULL;

                        ax = f64_from_bits(xBits);
                        ax -= 9.232978617785736E-128;

                        xBits = f64_bits(ax);
                    }

                    if (yExp == 0) // y is subnormal
                    {
                        yBits += 0x0010000000000000ULL;

                        ay = f64_from_bits(yBits);
                        ay -= 9.232978617785736E-128;

                        yBits = f64_bits(ay);
                    }
                }

                ax = f64_from_bits(xBits);
                ay = f64_from_bits(yBits);

                if (ax < ay)
                {
                    // Sort so ax is greater than ay
                    double tmp = ax;

                    ax = ay;
                    ay = tmp;

                    uint64_t tmpBits = xBits;

                    xBits = yBits;
                    yBits = tmpBits;
                }

                // Split ax and ay into a head and tail portion

                double xHead = f64_from_bits(xBits & 0xFFFFFFFFF8000000ULL);
                double yHead = f64_from_bits(yBits & 0xFFFFFFFFF8000000ULL);

                double xTail = ax - xHead;
                double yTail = ay - yHead;

                // Compute (x * x) + (y * y) with extra precision

                double xx = ax * ax;
                double yy = ay * ay;

                double rHead = xx + yy;
                double rTail = (xx - rHead) + yy;

                rTail += (xHead * xHead) - xx;
                rTail += 2 * xHead * xTail;
                rTail += xTail * xTail;

                if (expDiff == 0)
                {
                    // Extra accounting is only needed when the exponents are equal
                    rTail += (yHead * yHead) - yy;
                    rTail += 2 * yHead * yTail;
                    rTail += yTail * yTail;
                }

                result = std::sqrt(rHead + rTail) * expFix;
            }
            else
            {
                // x or y is insignificant compared to the other
                result = ax + ay;
            }
        }
    }
    else if (std::isinf(x) || std::isinf(y))
    {
        // IEEE 754 requires +Infinity even if the other input is NaN
        result = std::numeric_limits<double>::infinity();
    }
    else
    {
        result = dotnet_nan();
    }

    return result;
}

float dn2cpp_math_hypot_f(float x, float y)
{
    float result;

    if (std::isfinite(x) && std::isfinite(y))
    {
        float ax = std::fabs(x);
        float ay = std::fabs(y);

        if (ax == 0.0f)
        {
            result = ay;
        }
        else if (ay == 0.0f)
        {
            result = ax;
        }
        else
        {
            double xx = ax;
            xx *= xx;

            double yy = ay;
            yy *= yy;

            result = (float)std::sqrt(xx + yy);
        }
    }
    else if (std::isinf(x) || std::isinf(y))
    {
        // IEEE 754 requires +Infinity even if the other input is NaN
        result = std::numeric_limits<float>::infinity();
    }
    else
    {
        result = dotnet_nan_f();
    }

    return result;
}

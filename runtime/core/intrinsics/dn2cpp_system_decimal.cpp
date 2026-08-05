// dn2cpp_system_decimal.cpp — System.Decimal intrinsics: libc-free 96-bit
// fixed-point arithmetic, parsing and formatting emitted in place of the BCL's
// Decimal IL. Value layout, rounding (half-away-from-zero to 96 bits / 28-place
// scale) and formatting match .NET exactly.
#include "dn2cpp_core.h"

#include <cstdint>
#include <cstdio>
#include <cstring>
#include <cmath>

// ===================== System.Decimal =================================
// value = (-1)^sign * mantissa * 10^-scale, mantissa a 96-bit integer.
// The mantissa fits in unsigned __int128 (top 32 bits spare); intermediate
// products/aligned sums can exceed 128 bits, so those use a small base-2^32
// bignum (`DecBig`). All operators round half-away-from-zero to fit 96 bits /
// 28-place scale, matching .NET's Decimal arithmetic.

#if !(defined(_MSC_VER) && !defined(__clang__))
typedef unsigned __int128 dec_u128;
#else
// Real MSVC (cl.exe) has no 128-bit integer type, not even as an extension
// (clang-cl defines __clang__ and keeps the native unsigned __int128 above).
// This reproduces exactly the operations this file uses on dec_u128 over a
// two-limb {hi,lo} struct. Correctness over speed — decimal arithmetic is not a
// hot path — so multiplication uses a portable widening 64x64->128 and division
// a 128-iteration binary long division.
struct dec_u128
{
    uint64_t hi, lo;

    dec_u128() : hi(0), lo(0) {}
    dec_u128(uint64_t v) : hi(0), lo(v) {} // covers every integral literal/value
                                            // this file constructs a dec_u128
                                            // from (int/uint32_t/uint64_t/
                                            // int64_t all convert to uint64_t
                                            // as a single standard conversion)
    dec_u128(uint64_t hi_, uint64_t lo_) : hi(hi_), lo(lo_) {}

    explicit operator uint64_t() const { return lo; }
    explicit operator uint32_t() const { return (uint32_t)lo; }
    explicit operator int() const { return (int)lo; }

    friend dec_u128 operator<<(dec_u128 v, int n)
    {
        if (n <= 0) return v;
        if (n >= 128) return dec_u128();
        if (n >= 64) return dec_u128(v.lo << (n - 64), 0);
        return dec_u128((v.hi << n) | (v.lo >> (64 - n)), v.lo << n);
    }
    friend dec_u128 operator>>(dec_u128 v, int n)
    {
        if (n <= 0) return v;
        if (n >= 128) return dec_u128();
        if (n >= 64) return dec_u128(0, v.hi >> (n - 64));
        return dec_u128(v.hi >> n, (v.lo >> n) | (v.hi << (64 - n)));
    }
    dec_u128& operator<<=(int n) { *this = *this << n; return *this; }
    dec_u128& operator>>=(int n) { *this = *this >> n; return *this; }

    friend dec_u128 operator|(dec_u128 a, dec_u128 b) { return dec_u128(a.hi | b.hi, a.lo | b.lo); }
    friend dec_u128 operator&(dec_u128 a, dec_u128 b) { return dec_u128(a.hi & b.hi, a.lo & b.lo); }

    friend bool operator==(dec_u128 a, dec_u128 b) { return a.hi == b.hi && a.lo == b.lo; }
    friend bool operator!=(dec_u128 a, dec_u128 b) { return !(a == b); }
    friend bool operator<(dec_u128 a, dec_u128 b) { return a.hi != b.hi ? a.hi < b.hi : a.lo < b.lo; }
    friend bool operator>(dec_u128 a, dec_u128 b) { return b < a; }
    friend bool operator<=(dec_u128 a, dec_u128 b) { return !(b < a); }
    friend bool operator>=(dec_u128 a, dec_u128 b) { return !(a < b); }

    friend dec_u128 operator+(dec_u128 a, dec_u128 b)
    {
        uint64_t lo = a.lo + b.lo;
        return dec_u128(a.hi + b.hi + (lo < a.lo ? 1 : 0), lo);
    }
    friend dec_u128 operator-(dec_u128 a, dec_u128 b)
    {
        uint64_t lo = a.lo - b.lo;
        return dec_u128(a.hi - b.hi - (a.lo < b.lo ? 1 : 0), lo);
    }
    dec_u128& operator+=(dec_u128 b) { *this = *this + b; return *this; }
    dec_u128& operator-=(dec_u128 b) { *this = *this - b; return *this; }

    friend dec_u128 operator*(dec_u128 a, dec_u128 b)
    {
        // Portable widening 64x64->128 (the standard 32-bit-limb schoolbook
        // trick) for the low halves, plus the two cross terms for the high
        // half — only the low 128 bits are kept (matching unsigned
        // __int128's mod-2^128 wraparound), which is all this file needs:
        // every product it forms here stays within 128 bits.
        uint64_t aLo = (uint32_t)a.lo, aHi = a.lo >> 32;
        uint64_t bLo = (uint32_t)b.lo, bHi = b.lo >> 32;
        uint64_t t = aLo * bLo;
        uint64_t w0 = (uint32_t)t;
        uint64_t k = t >> 32;
        t = aHi * bLo + k;
        uint64_t w1 = (uint32_t)t;
        uint64_t w2 = t >> 32;
        t = aLo * bHi + w1;
        uint64_t lo = (t << 32) | w0;
        uint64_t hi = aHi * bHi + w2 + (t >> 32) + a.lo * b.hi + a.hi * b.lo;
        return dec_u128(hi, lo);
    }
    dec_u128& operator*=(dec_u128 b) { *this = *this * b; return *this; }

    friend void dec_u128_divmod(dec_u128 num, dec_u128 den, dec_u128& q, dec_u128& r)
    {
        q = dec_u128();
        r = dec_u128();
        for (int i = 127; i >= 0; i--)
        {
            r <<= 1;
            uint64_t bit = (i >= 64) ? ((num.hi >> (i - 64)) & 1u) : ((num.lo >> i) & 1u);
            r.lo |= bit;
            if (r >= den)
            {
                r = r - den;
                q = q | (dec_u128(1ull) << i);
            }
        }
    }
    friend dec_u128 operator/(dec_u128 a, dec_u128 b) { dec_u128 q, r; dec_u128_divmod(a, b, q, r); return q; }
    friend dec_u128 operator%(dec_u128 a, dec_u128 b) { dec_u128 q, r; dec_u128_divmod(a, b, q, r); return r; }
    dec_u128& operator/=(dec_u128 b) { *this = *this / b; return *this; }
    dec_u128& operator%=(dec_u128 b) { *this = *this % b; return *this; }
};
#endif
static const dec_u128 DEC_MANT_MAX = (((dec_u128)0xFFFFFFFFu) << 64) | 0xFFFFFFFFFFFFFFFFull; // 2^96-1

static inline dec_u128 dec_mant(const Dn2CppDecimal& d) { return (((dec_u128)d.hi) << 64) | d.lo; }

// Little-endian base-2^32 magnitude, wide enough for 10^28 * 2^96 (~189 bits)
// plus a carry — 8 limbs (256 bits) is ample.
struct DecBig { uint32_t d[8]; int n; };

static inline void decbig_norm(DecBig& b) { while (b.n > 0 && b.d[b.n - 1] == 0) b.n--; }
static DecBig decbig_from_u128(dec_u128 v)
{
    DecBig b; b.n = 0;
    for (int i = 0; i < 8 && v != 0; i++) { b.d[i] = (uint32_t)v; v >>= 32; b.n = i + 1; }
    return b;
}
// Multiply in place by a small (< 2^32) value, adding `add` as the initial carry.
static void decbig_mul_add_small(DecBig& b, uint32_t mul, uint32_t add)
{
    uint64_t carry = add;
    for (int i = 0; i < b.n; i++) { uint64_t p = (uint64_t)b.d[i] * mul + carry; b.d[i] = (uint32_t)p; carry = p >> 32; }
    while (carry != 0) { b.d[b.n++] = (uint32_t)carry; carry >>= 32; }
    decbig_norm(b);
}
static void decbig_mul_pow10(DecBig& b, int e)
{
    while (e > 0) { int chunk = e > 9 ? 9 : e; static const uint32_t p10[] = {1,10,100,1000,10000,100000,1000000,10000000,100000000,1000000000}; decbig_mul_add_small(b, p10[chunk], 0); e -= chunk; }
}
// Divide in place by a small divisor; returns the remainder.
static uint32_t decbig_divmod_small(DecBig& b, uint32_t div)
{
    uint64_t rem = 0;
    for (int i = b.n - 1; i >= 0; i--) { uint64_t cur = (rem << 32) | b.d[i]; b.d[i] = (uint32_t)(cur / div); rem = cur % div; }
    decbig_norm(b);
    return (uint32_t)rem;
}
static int decbig_cmp(const DecBig& a, const DecBig& b)
{
    if (a.n != b.n) return a.n < b.n ? -1 : 1;
    for (int i = a.n - 1; i >= 0; i--) if (a.d[i] != b.d[i]) return a.d[i] < b.d[i] ? -1 : 1;
    return 0;
}
static DecBig decbig_add(const DecBig& a, const DecBig& b)
{
    DecBig r; r.n = a.n > b.n ? a.n : b.n; uint64_t carry = 0;
    for (int i = 0; i < r.n; i++) { uint64_t s = carry + (i < a.n ? a.d[i] : 0) + (i < b.n ? b.d[i] : 0); r.d[i] = (uint32_t)s; carry = s >> 32; }
    if (carry) r.d[r.n++] = (uint32_t)carry;
    decbig_norm(r);
    return r;
}
// a - b, requires a >= b.
static DecBig decbig_sub(const DecBig& a, const DecBig& b)
{
    DecBig r; r.n = a.n; int64_t borrow = 0;
    for (int i = 0; i < a.n; i++) { int64_t s = (int64_t)a.d[i] - (i < b.n ? b.d[i] : 0) - borrow; if (s < 0) { s += (int64_t)1 << 32; borrow = 1; } else borrow = 0; r.d[i] = (uint32_t)s; }
    decbig_norm(r);
    return r;
}
static DecBig decbig_mul_u128(dec_u128 a, dec_u128 b)
{
    DecBig ba = decbig_from_u128(a);
    // Schoolbook: result = sum over b's limbs of (ba * limb) << (32*i).
    DecBig r; r.n = 0; for (int i = 0; i < 8; i++) r.d[i] = 0;
    DecBig bb = decbig_from_u128(b);
    for (int i = 0; i < bb.n; i++)
    {
        uint64_t carry = 0;
        for (int j = 0; j < ba.n; j++)
        {
            uint64_t p = (uint64_t)ba.d[j] * bb.d[i] + r.d[i + j] + carry;
            r.d[i + j] = (uint32_t)p; carry = p >> 32;
        }
        int k = i + ba.n;
        while (carry) { uint64_t s = (uint64_t)r.d[k] + carry; r.d[k] = (uint32_t)s; carry = s >> 32; k++; }
        if (k > r.n) r.n = k;
    }
    decbig_norm(r);
    return r;
}
static inline bool decbig_fits96(const DecBig& b) { return b.n <= 3; }
static dec_u128 decbig_to_u128(const DecBig& b)
{
    dec_u128 v = 0;
    for (int i = b.n - 1; i >= 0; i--) v = (v << 32) | b.d[i];
    return v;
}

static inline Dn2CppDecimal dec_pack(dec_u128 mant, int sign, int scale)
{
    Dn2CppDecimal d;
    d.lo = (uint64_t)mant;
    d.hi = (uint32_t)(mant >> 64);
    d.scale = (uint8_t)scale;
    d.sign = (mant == 0) ? 0 : (uint8_t)(sign & 1); // decimal has no negative zero
    return d;
}

// Reduce a magnitude (possibly > 96 bits) to fit in 96 bits with scale <= 28,
// dropping least-significant digits and rounding the result half-away-from-zero.
static Dn2CppDecimal dec_reduce_round(DecBig s, int sign, int scale)
{
    uint32_t lastDropped = 0;
    while (!decbig_fits96(s) || scale > 28)
    {
        if (scale <= 0 && !decbig_fits96(s))
            dn2cpp_overflow();
        lastDropped = decbig_divmod_small(s, 10);
        scale--;
    }
    dec_u128 m = decbig_to_u128(s);
    if (lastDropped >= 5)
    {
        m += 1;
        if (m > DEC_MANT_MAX) { m /= 10; scale--; if (scale < 0) dn2cpp_overflow(); }
    }
    return dec_pack(m, sign, scale);
}

Dn2CppDecimal dn2cpp_decimal_from_parts(int32_t lo, int32_t mid, int32_t hi, int32_t isNeg, int32_t scale)
{
    dec_u128 m = (((dec_u128)(uint32_t)hi) << 64) | (((dec_u128)(uint32_t)mid) << 32) | (uint32_t)lo;
    return dec_pack(m, isNeg ? 1 : 0, scale & 0xFF);
}
// The four .NET GetBits words in the { lo32, mid32, hi32, flags } layout.
static inline void dn2cpp_decimal_bits(Dn2CppDecimal a, int32_t out[4])
{
    out[0] = (int32_t)(uint32_t)(a.lo & 0xFFFFFFFFu);
    out[1] = (int32_t)(uint32_t)(a.lo >> 32);
    out[2] = (int32_t)a.hi;
    out[3] = ((int32_t)a.scale << 16) | (a.sign != 0 ? (int32_t)0x80000000 : 0);
}
// `ti` is the Int32[] handle the emit arm supplies: the result is the public
// decimal.GetBits return value, so its GetType() must be the image's int[] and not
// the shared dn2cpp_array_i4_type — those agree on the NAME but not on identity, so
// without it `bits.GetType() == typeof(int[])` is False and the array carries no
// SZArray interface map. Null degrades to the shared handle.
Dn2CppArrayI4* dn2cpp_decimal_get_bits(Dn2CppDecimal a, const Dn2CppTypeInfo* ti)
{
    Dn2CppArrayI4* arr = dn2cpp_newarr_i4_t(4, ti);
    dn2cpp_decimal_bits(a, arr->data);
    return arr;
}
int32_t dn2cpp_decimal_get_bits_span(Dn2CppDecimal a, int32_t* dst, int32_t destLen)
{
    if (destLen < 4)
        dn2cpp_throw_argument(); // .NET raises ArgumentException("destination too small")
    int32_t bits[4];
    dn2cpp_decimal_bits(a, bits);
    dst[0] = bits[0]; dst[1] = bits[1]; dst[2] = bits[2]; dst[3] = bits[3];
    return 4;
}
Dn2CppDecimal dn2cpp_decimal_from_i4(int32_t v) { return dec_pack(v < 0 ? (dec_u128)(-(int64_t)v) : (dec_u128)v, v < 0 ? 1 : 0, 0); }
Dn2CppDecimal dn2cpp_decimal_from_u4(uint32_t v) { return dec_pack((dec_u128)v, 0, 0); }
// v < 0's magnitude via unsigned wraparound negation (0 - (uint64_t)v), not
// -v: negating INT64_MIN as an int64_t is signed overflow (UB); the unsigned
// subtraction is well-defined mod 2^64 and yields the exact same bit pattern
// __int128-widened negation would have, without needing a wider type.
Dn2CppDecimal dn2cpp_decimal_from_i8(int64_t v) { return dec_pack(v < 0 ? (dec_u128)(uint64_t)(0 - (uint64_t)v) : (dec_u128)v, v < 0 ? 1 : 0, 0); }
Dn2CppDecimal dn2cpp_decimal_from_u8(uint64_t v) { return dec_pack((dec_u128)v, 0, 0); }

// NaN/Infinity and out-of-range magnitudes raise a catchable
// OverflowException — the .NET contract for both the (decimal) cast and
// Convert.ToDecimal.
Dn2CppDecimal dn2cpp_decimal_from_double(double v)
{
    if (std::isnan(v) || std::isinf(v)) dn2cpp_overflow();
    int sign = std::signbit(v) ? 1 : 0;
    double mag = sign ? -v : v;
    if (mag < 1e-28) return dec_pack(0, 0, 0);
    if (mag >= 7.922816251426434e28) dn2cpp_overflow();
    // .NET converts a double to decimal using 15 significant digits.
    char buf[40];
    std::snprintf(buf, sizeof(buf), "%.15g", mag);
    Dn2CppDecimal r = dn2cpp_decimal_parse(dn2cpp_string_from_ascii(buf, (int32_t)std::strlen(buf)));
    r.sign = (dec_mant(r) == 0) ? 0 : (uint8_t)sign;
    return r;
}
Dn2CppDecimal dn2cpp_decimal_from_float(float v)
{
    if (std::isnan(v) || std::isinf(v)) dn2cpp_overflow();
    int sign = std::signbit(v) ? 1 : 0;
    double mag = sign ? -(double)v : (double)v;
    if (mag >= 7.922816251426434e28) dn2cpp_overflow();
    char buf[40];
    std::snprintf(buf, sizeof(buf), "%.7g", mag); // float carries 7 significant digits
    Dn2CppDecimal r = dn2cpp_decimal_parse(dn2cpp_string_from_ascii(buf, (int32_t)std::strlen(buf)));
    r.sign = (dec_mant(r) == 0) ? 0 : (uint8_t)sign;
    return r;
}

static Dn2CppDecimal dec_addsub(Dn2CppDecimal a, Dn2CppDecimal b, bool subtract)
{
    int bsign = subtract ? !b.sign : b.sign;
    int scale = a.scale > b.scale ? a.scale : b.scale;
    DecBig ma = decbig_from_u128(dec_mant(a)); decbig_mul_pow10(ma, scale - a.scale);
    DecBig mb = decbig_from_u128(dec_mant(b)); decbig_mul_pow10(mb, scale - b.scale);
    DecBig s; int sign;
    if ((int)a.sign == bsign) { s = decbig_add(ma, mb); sign = a.sign; }
    else
    {
        int c = decbig_cmp(ma, mb);
        if (c >= 0) { s = decbig_sub(ma, mb); sign = a.sign; }
        else { s = decbig_sub(mb, ma); sign = bsign; }
    }
    return dec_reduce_round(s, sign, scale);
}
Dn2CppDecimal dn2cpp_decimal_add(Dn2CppDecimal a, Dn2CppDecimal b) { return dec_addsub(a, b, false); }
Dn2CppDecimal dn2cpp_decimal_sub(Dn2CppDecimal a, Dn2CppDecimal b) { return dec_addsub(a, b, true); }

Dn2CppDecimal dn2cpp_decimal_mul(Dn2CppDecimal a, Dn2CppDecimal b)
{
    DecBig p = decbig_mul_u128(dec_mant(a), dec_mant(b));
    return dec_reduce_round(p, a.sign ^ b.sign, (int)a.scale + (int)b.scale);
}

// The two div-by-zero guards below raise the same catchable
// DivideByZeroException the integer div/rem lowering does, so `1.0m / 0m`
// behaves as it does in real .NET.
Dn2CppDecimal dn2cpp_decimal_div(Dn2CppDecimal a, Dn2CppDecimal b)
{
    dec_u128 den = dec_mant(b);
    if (den == 0) dn2cpp_throw_divide_by_zero();
    dec_u128 num = dec_mant(a);
    int sign = a.sign ^ b.sign;
    if (num == 0) return dec_pack(0, 0, 0);

    dec_u128 Q = num / den;
    dec_u128 r = num % den;
    int f = 0; // fractional digits produced
    while (r != 0)
    {
        if (f + (int)a.scale - (int)b.scale >= 28) break;
        if (Q > DEC_MANT_MAX / 10) break;
        r *= 10;
        Q = Q * 10 + (r / den);
        r %= den;
        f++;
    }
    if (r != 0 && 2 * r >= den) { Q += 1; } // round half-away-from-zero
    int scale = f + (int)a.scale - (int)b.scale;
    if (scale < 0)
    {
        DecBig qb = decbig_from_u128(Q); decbig_mul_pow10(qb, -scale);
        return dec_reduce_round(qb, sign, 0);
    }
    DecBig qb = decbig_from_u128(Q);
    return dec_reduce_round(qb, sign, scale);
}

Dn2CppDecimal dn2cpp_decimal_rem(Dn2CppDecimal a, Dn2CppDecimal b)
{
    dec_u128 den = dec_mant(b);
    if (den == 0) dn2cpp_throw_divide_by_zero();
    // a % b == a - truncate(a/b)*b. Compute on aligned mantissas to keep it exact.
    int scale = a.scale > b.scale ? a.scale : b.scale;
    DecBig ma = decbig_from_u128(dec_mant(a)); decbig_mul_pow10(ma, scale - a.scale);
    DecBig mb = decbig_from_u128(dec_mant(b)); decbig_mul_pow10(mb, scale - b.scale);
    // Compute ma mod mb on magnitudes; result keeps a's sign. Decimal long-division:
    // find the largest power-of-10 multiple of mb that fits within ma, subtract it
    // repeatedly, then continue until ma < mb.
    while (decbig_cmp(ma, mb) >= 0)
    {
        // shift mb left (×10) until just larger than ma, then subtract multiples.
        DecBig t = mb; int shift = 0;
        DecBig t10 = t; decbig_mul_pow10(t10, 1);
        while (decbig_cmp(t10, ma) <= 0) { t = t10; t10 = t; decbig_mul_pow10(t10, 1); shift++; }
        while (decbig_cmp(t, ma) <= 0) ma = decbig_sub(ma, t);
        (void)shift;
    }
    return dec_reduce_round(ma, a.sign, scale);
}

Dn2CppDecimal dn2cpp_decimal_neg(Dn2CppDecimal a) { a.sign = (dec_mant(a) == 0) ? 0 : !a.sign; return a; }
Dn2CppDecimal dn2cpp_decimal_abs(Dn2CppDecimal a) { a.sign = 0; return a; }

int32_t dn2cpp_decimal_is_canonical(Dn2CppDecimal a)
{
    // .NET's Decimal.IsCanonical: scale 0 is always canonical; otherwise the
    // representation is canonical iff the mantissa carries no trailing decimal
    // zero (so 0.05m is canonical while 0.50m — and 0.0m, mantissa 0 — is not).
    return (a.scale == 0 || dec_mant(a) % 10 != 0) ? 1 : 0;
}

int32_t dn2cpp_decimal_is_even_integer(Dn2CppDecimal a)
{
    // Matches the BCL body: an integer value (truncation compares equal) whose
    // truncated mantissa — exact at scale 0, so the parity is the low bit of
    // the full 96-bit integer — is even. 2.00m is even; 2.5m is neither even
    // nor odd.
    Dn2CppDecimal t = dn2cpp_decimal_truncate(a);
    return (dn2cpp_decimal_cmp(t, a) == 0 && (t.lo & 1) == 0) ? 1 : 0;
}

int32_t dn2cpp_decimal_is_odd_integer(Dn2CppDecimal a)
{
    Dn2CppDecimal t = dn2cpp_decimal_truncate(a);
    return (dn2cpp_decimal_cmp(t, a) == 0 && (t.lo & 1) != 0) ? 1 : 0;
}

Dn2CppDecimal dn2cpp_decimal_max_magnitude(Dn2CppDecimal x, Dn2CppDecimal y)
{
    // The BCL body: compare |x| vs |y|; on a magnitude tie a negative x yields
    // y (so the non-negative operand — and for equal values of equal sign, y's
    // representation — wins), else x. MaxMagnitude(1.0m, 1.00m) returns 1.0m.
    int32_t c = dn2cpp_decimal_cmp(dn2cpp_decimal_abs(x), dn2cpp_decimal_abs(y));
    if (c > 0) return x;
    if (c == 0) return x.sign ? y : x;
    return y;
}

Dn2CppDecimal dn2cpp_decimal_min_magnitude(Dn2CppDecimal x, Dn2CppDecimal y)
{
    // Mirror image: on a magnitude tie a negative x wins (the negative operand),
    // else y — MinMagnitude(1.0m, 1.00m) returns 1.00m.
    int32_t c = dn2cpp_decimal_cmp(dn2cpp_decimal_abs(x), dn2cpp_decimal_abs(y));
    if (c < 0) return x;
    if (c == 0) return x.sign ? x : y;
    return y;
}

int32_t dn2cpp_decimal_cmp(Dn2CppDecimal a, Dn2CppDecimal b)
{
    bool az = dec_mant(a) == 0, bz = dec_mant(b) == 0;
    int asg = az ? 0 : (a.sign ? -1 : 1);
    int bsg = bz ? 0 : (b.sign ? -1 : 1);
    if (asg != bsg) return asg < bsg ? -1 : 1;
    if (asg == 0) return 0; // both zero
    int scale = a.scale > b.scale ? a.scale : b.scale;
    DecBig ma = decbig_from_u128(dec_mant(a)); decbig_mul_pow10(ma, scale - a.scale);
    DecBig mb = decbig_from_u128(dec_mant(b)); decbig_mul_pow10(mb, scale - b.scale);
    int c = decbig_cmp(ma, mb);
    return asg > 0 ? c : -c; // negatives: larger magnitude is smaller
}

int32_t dn2cpp_decimal_hash(Dn2CppDecimal a)
{
    // Canonicalize so equal values hash equal: strip trailing zero digits (1.0m
    // and 1.00m reduce to the same mantissa). The mixed value need not match
    // .NET's hash — only the equal-value->equal-hash contract matters here.
    dec_u128 m = dec_mant(a);
    int scale = a.scale;
    while (scale > 0 && m != 0 && (m % 10) == 0) { m /= 10; scale--; }
    uint64_t lo = (uint64_t)m;
    uint64_t hi = (uint64_t)(m >> 64);
    int sign = (m == 0) ? 0 : a.sign; // -0 == 0
    uint64_t h = lo ^ (hi * 0x9E3779B97F4A7C15ull) ^ ((uint64_t)sign << 1);
    return (int32_t)((h ^ (h >> 32)) & 0x7fffffff);
}

double dn2cpp_decimal_to_double(Dn2CppDecimal a)
{
    double m = (double)(uint64_t)(dec_mant(a) >> 32) * 4294967296.0 + (double)(uint64_t)(dec_mant(a) & 0xFFFFFFFFull);
    // Scale by a single 10^scale divisor (built exactly — 10^k is representable in
    // double for k <= 22) rather than dividing by 10 in a loop, which would accumulate
    // rounding error (314/10/10 != 314/100, so 3.14m would round to 3.1399999999999997).
    double div = 1.0;
    for (int i = 0; i < a.scale; i++) div *= 10.0;
    m /= div;
    return a.sign ? -m : m;
}
float dn2cpp_decimal_to_float(Dn2CppDecimal a) { return (float)dn2cpp_decimal_to_double(a); }
int64_t dn2cpp_decimal_to_i8(Dn2CppDecimal a)
{
    dec_u128 m = dec_mant(a);
    for (int i = 0; i < a.scale; i++) m /= 10; // truncate toward zero
    int64_t v = (int64_t)(uint64_t)m;
    return a.sign ? -v : v;
}
uint64_t dn2cpp_decimal_to_u8(Dn2CppDecimal a)
{
    dec_u128 m = dec_mant(a);
    for (int i = 0; i < a.scale; i++) m /= 10;
    return (uint64_t)m;
}

// Drop `drop` least-significant decimal places from a, rounding per the
// MidpointRounding mode (0 = banker's/ToEven, 1 = away-from-zero, 2 = toward
// zero, 3 = toward -infinity, 4 = toward +infinity — the directed modes are
// scale-aware over the dropped digits, exactly like .NET's decimal.Round).
// Used by Round/Truncate/Floor/Ceiling.
static Dn2CppDecimal dec_drop_places(Dn2CppDecimal a, int drop, int mode)
{
    if (drop <= 0) return a;
    if (drop >= a.scale + 1 + 30) return dec_pack(0, 0, 0);
    DecBig b = decbig_from_u128(dec_mant(a));
    uint32_t lastDropped = 0; bool sticky = false;
    for (int i = 0; i < drop; i++)
    {
        if (lastDropped != 0) sticky = true;
        lastDropped = decbig_divmod_small(b, 10);
    }
    dec_u128 m = decbig_to_u128(b);
    // The mantissa is the magnitude, so "round up" grows away from zero; the
    // directed modes therefore pick it by the sign of the value.
    bool anyDropped = sticky || lastDropped != 0;
    bool roundUp;
    switch (mode)
    {
        case 0: // ToEven: exactly .5 goes to the even neighbor
            if (lastDropped > 5) roundUp = true;
            else if (lastDropped < 5) roundUp = false;
            else roundUp = sticky ? true : ((m & 1) != 0);
            break;
        case 1: roundUp = lastDropped >= 5; break;       // AwayFromZero
        case 2: roundUp = false; break;                  // ToZero: truncate
        case 3: roundUp = a.sign && anyDropped; break;   // ToNegativeInfinity
        case 4: roundUp = !a.sign && anyDropped; break;  // ToPositiveInfinity
        default: dn2cpp_throw_argument();                // catchable, like the BCL
    }
    if (roundUp) m += 1;
    return dec_pack(m, a.sign, a.scale - drop);
}

Dn2CppDecimal dn2cpp_decimal_round(Dn2CppDecimal a, int32_t digits, int32_t mode)
{
    // .NET's decimal.Round validates the mode before looking at the scale, so
    // an invalid mode throws even when no digit would be dropped.
    if ((uint32_t)mode > 4u)
        dn2cpp_throw_argument();
    if (a.scale <= digits) return a;
    return dec_drop_places(a, a.scale - digits, mode);
}
Dn2CppDecimal dn2cpp_decimal_truncate(Dn2CppDecimal a)
{
    if (a.scale == 0) return a;
    DecBig b = decbig_from_u128(dec_mant(a));
    for (int i = 0; i < a.scale; i++) decbig_divmod_small(b, 10);
    return dec_pack(decbig_to_u128(b), a.sign, 0);
}
Dn2CppDecimal dn2cpp_decimal_floor(Dn2CppDecimal a)
{
    Dn2CppDecimal t = dn2cpp_decimal_truncate(a);
    if (a.sign && dn2cpp_decimal_cmp(t, a) != 0) // negative with a fractional part -> toward -inf
        t = dn2cpp_decimal_sub(t, dn2cpp_decimal_from_i4(1));
    return t;
}
Dn2CppDecimal dn2cpp_decimal_ceiling(Dn2CppDecimal a)
{
    Dn2CppDecimal t = dn2cpp_decimal_truncate(a);
    if (!a.sign && dn2cpp_decimal_cmp(t, a) != 0) // positive with a fractional part -> toward +inf
        t = dn2cpp_decimal_add(t, dn2cpp_decimal_from_i4(1));
    return t;
}

// Decimal digits of the mantissa into `buf` (no sign), returning the length;
// always at least "0".
static int dec_digits(dec_u128 m, char* buf)
{
    char tmp[40]; int t = 0;
    if (m == 0) tmp[t++] = '0';
    while (m != 0) { tmp[t++] = (char)('0' + (int)(m % 10)); m /= 10; }
    for (int i = 0; i < t; i++) buf[i] = tmp[t - 1 - i];
    return t;
}

// Lay out a decimal's value as ASCII into `out` (sign + integer + '.' + frac),
// honoring `scale` (trailing zeros preserved). Returns the length.
static int dec_layout(const Dn2CppDecimal& a, char* out)
{
    dec_u128 m = dec_mant(a);
    char digits[40];
    int dl = dec_digits(m, digits);
    int o = 0;
    if (a.sign && m != 0) out[o++] = '-';
    int scale = a.scale;
    if (scale == 0)
    {
        for (int i = 0; i < dl; i++) out[o++] = digits[i];
        return o;
    }
    int intLen = dl - scale;
    if (intLen <= 0)
    {
        out[o++] = '0'; out[o++] = '.';
        for (int i = 0; i < -intLen; i++) out[o++] = '0';
        for (int i = 0; i < dl; i++) out[o++] = digits[i];
    }
    else
    {
        for (int i = 0; i < intLen; i++) out[o++] = digits[i];
        out[o++] = '.';
        for (int i = intLen; i < dl; i++) out[o++] = digits[i];
    }
    return o;
}

// decimal.ToString() — the provider-less overload, so the CURRENT culture, like
// every other no-provider numeric ToString (dn2cpp_nfi_or_current's contract);
// handing the ASCII layout back untouched would be wrong under, say, a de-DE
// current culture. Identical to dn2cpp_decimal_format_c(a, nullptr, nullptr),
// spelled out because that entry point is below this one.
Dn2CppString* dn2cpp_decimal_to_string(Dn2CppDecimal a)
{
    char out[80];
    int o = dec_layout(a, out);
    return dn2cpp_localize_ascii(out, o, dn2cpp_nfi_or_current(nullptr));
}

Dn2CppString* dn2cpp_decimal_format_c(Dn2CppDecimal a, Dn2CppString* fmt, const Dn2CppNumberFormatInfo* nfi)
{
    nfi = dn2cpp_nfi_or_current(nfi);
    if (fmt == nullptr || fmt->length == 0)
    {
        char out[80]; int o = dec_layout(a, out);
        return dn2cpp_localize_ascii(out, o, nfi);
    }
    // Standard specifiers route through the double number path for grouping/fixed
    // width (F/N/C/P/E and G with precision). The decimal's exact integer value is
    // preserved for default ToString; spec'd output matches .NET for the practical
    // magnitudes that fit a double's 15-17 digits.
    char letter = (char)fmt->chars[0];
    if (letter == 'G' || letter == 'g')
    {
        char out[80]; int o = dec_layout(a, out);
        return dn2cpp_localize_ascii(out, o, nfi);
    }
    return dn2cpp_format_r8_c(dn2cpp_decimal_to_double(a), fmt, nfi);
}
Dn2CppString* dn2cpp_decimal_format(Dn2CppDecimal a, Dn2CppString* fmt)
{
    return dn2cpp_decimal_format_c(a, fmt, nullptr);
}

int32_t dn2cpp_decimal_try_parse(Dn2CppString* s, Dn2CppDecimal* out)
{
    if (s == nullptr) return 0;
    int i = 0, len = s->length;
    while (i < len && (s->chars[i] == u' ' || s->chars[i] == u'\t')) i++;
    int sign = 0;
    if (i < len && (s->chars[i] == u'+' || s->chars[i] == u'-')) { sign = s->chars[i] == u'-'; i++; }
    dec_u128 m = 0; int scale = 0; bool seenDot = false, seenDigit = false;
    for (; i < len; i++)
    {
        char16_t c = s->chars[i];
        if (c == u',') continue; // tolerate group separators
        if (c == u'.') { if (seenDot) return 0; seenDot = true; continue; }
        if (c < u'0' || c > u'9') break;
        if (m > DEC_MANT_MAX / 10) return 0; // overflow
        m = m * 10 + (c - u'0');
        if (seenDot) scale++;
        seenDigit = true;
    }
    while (i < len && (s->chars[i] == u' ' || s->chars[i] == u'\t')) i++;
    if (!seenDigit || i != len || scale > 28) return 0;
    *out = dec_pack(m, sign, scale);
    return 1;
}
// A null `s` is deliberately NOT split into its own ArgumentNullException here.
// This is not the entry point decimal.Parse(string) lowers to — that is
// dn2cpp_decimal_parse_styles_*, which already raises catchably — and its three
// callers cannot pass null, so a null here means a runtime bug rather than bad
// input and must not be reported as the user's FormatException.
Dn2CppDecimal dn2cpp_decimal_parse(Dn2CppString* s)
{
    Dn2CppDecimal r;
    if (!dn2cpp_decimal_try_parse(s, &r)) dn2cpp_throw_format();
    return r;
}

// ── NumberStyles-honoring parse ──────────────────────────────────────────────
// The shared dn2cpp_parse.cpp state machine produces (digits, scale, sign);
// this converts them like .NET's Number.NumberToDecimal: the mantissa
// accumulates until the 96-bit capacity or the 28-place scale is reached, the
// first dropped digit rounds half-even (round up on >5, or on ==5 with a
// non-zero rest or an odd mantissa), an all-zero value keeps its fractional
// scale ("0.000" round-trips), and integer digits beyond capacity overflow.
// Status: 0 ok, 2 overflow.
static int32_t dec_from_number(const Dn2CppNumberParse* num, Dn2CppDecimal* out)
{
    const uint8_t* p = num->digits;
    int32_t count = num->count;
    int32_t e = num->scale;
    if (count == 0 && !num->hasNonZeroTail)
    {
        int32_t sc = -e;
        if (sc < 0) sc = 0;
        if (sc > 28) sc = 28;
        *out = dec_pack(0, num->negative, sc);
        return 0;
    }
    if (e > 29)
        return 2;
    dec_u128 m = 0;
    int32_t idx = 0;
    const dec_u128 maxDiv10 = DEC_MANT_MAX / 10;
    const uint32_t maxMod10 = (uint32_t)(uint64_t)(DEC_MANT_MAX % 10);
    while (e > 0 || (idx < count && e > -28))
    {
        uint32_t d = idx < count ? (uint32_t)(p[idx] - '0') : 0u;
        // Precise capacity check: stop only when m*10+d really exceeds 96 bits
        // (decimal.MaxValue itself must accumulate exactly).
        if (m > maxDiv10 || (m == maxDiv10 && d > maxMod10))
            break;
        m = m * 10 + (dec_u128)d;
        if (idx < count)
            idx++;
        e--;
    }
    if (idx < count || num->hasNonZeroTail)
    {
        // Digits were dropped: round on the first one, half-to-even.
        uint32_t c = idx < count ? (uint32_t)(p[idx] - '0') : 1u;
        bool restNonZero = num->hasNonZeroTail != 0;
        for (int32_t j = idx + 1; j < count && !restNonZero; j++)
            restNonZero = p[j] != '0';
        bool odd = ((uint32_t)(uint64_t)m & 1u) != 0;
        if (c > 5 || (c == 5 && (restNonZero || odd)))
        {
            m = m + 1;
            if (m > DEC_MANT_MAX)
                return 2; // round-up past 96 bits with no scale room
        }
    }
    if (e > 0)
        return 2; // integer digits beyond the 96-bit capacity
    int32_t scale = -e;
    if (scale > 28)
        return 2;
    *out = dec_pack(m, num->negative, scale);
    return 0;
}

// Status: 0 ok, 1 format, 2 overflow (argument errors throw in the validator).
static int32_t dec_styles_core(const char16_t* p, int32_t n, int32_t styles,
                               const Dn2CppNumberFormatInfo* nfi, Dn2CppDecimal* out)
{
    dn2cpp_parse_validate_fp_styles(styles);
    *out = dec_pack(0, 0, 0);
    Dn2CppNumberParse num;
    if (!dn2cpp_parse_number_styles(p, n, styles, nfi, 2, &num))
        return 1;
    if (dec_from_number(&num, out) != 0)
    {
        *out = dec_pack(0, 0, 0);
        return 2;
    }
    return 0;
}

int32_t dn2cpp_decimal_tryparse_styles_chars(const char16_t* p, int32_t n, int32_t styles,
                                             const Dn2CppNumberFormatInfo* nfi, Dn2CppDecimal* out)
{
    return dec_styles_core(p, n, styles, nfi, out) == 0 ? 1 : 0;
}

int32_t dn2cpp_decimal_tryparse_styles_str(Dn2CppString* s, int32_t styles,
                                           const Dn2CppNumberFormatInfo* nfi, Dn2CppDecimal* out)
{
    if (s == nullptr)
    {
        // Styles are validated (and may throw) before the null check, like .NET.
        dn2cpp_parse_validate_fp_styles(styles);
        *out = dec_pack(0, 0, 0);
        return 0;
    }
    return dn2cpp_decimal_tryparse_styles_chars(s->chars, s->length, styles, nfi, out);
}

Dn2CppDecimal dn2cpp_decimal_parse_styles_chars(const char16_t* p, int32_t n, int32_t styles,
                                                const Dn2CppNumberFormatInfo* nfi)
{
    Dn2CppDecimal r;
    switch (dec_styles_core(p, n, styles, nfi, &r))
    {
        case 1: dn2cpp_throw_format();
        case 2: dn2cpp_overflow();
        default: break;
    }
    return r;
}

Dn2CppDecimal dn2cpp_decimal_parse_styles_str(Dn2CppString* s, int32_t styles,
                                              const Dn2CppNumberFormatInfo* nfi)
{
    if (s == nullptr)
    {
        dn2cpp_parse_validate_fp_styles(styles);
        dn2cpp_throw_argument_null();
    }
    return dn2cpp_decimal_parse_styles_chars(s->chars, s->length, styles, nfi);
}

// ── Boxed System.Decimal type-info ───────────────────────────────────────────
// Defined here, with its slots spelled, rather than among the primitives in
// dn2cpp_typeinfo.cpp with an if-chain to reach it: a program that never mentions
// a decimal should not link this translation unit. See the same treatment of the
// date/time value types in dn2cpp_system_datetime.cpp.

// A boxed decimal ToStrings with its scale preserved ("1.50").
static Dn2CppString* dn2cpp_decimal_box_tostring(Dn2CppObject* o)
{ return dn2cpp_decimal_to_string(*dn2cpp_boxed<Dn2CppDecimal>(o)); }
// Hash and equality are scale-insensitive: 1.0m and 1.00m agree, matching
// Decimal.GetHashCode / Decimal.Equals.
static int32_t dn2cpp_decimal_box_hash(Dn2CppObject* o)
{ return dn2cpp_decimal_hash(*dn2cpp_boxed<Dn2CppDecimal>(o)); }
static int32_t dn2cpp_decimal_box_equals(Dn2CppObject* a, Dn2CppObject* b)
{
    return (b->type == &dn2cpp_decimal_type
            && dn2cpp_decimal_cmp(*dn2cpp_boxed<Dn2CppDecimal>(a),
                                  *dn2cpp_boxed<Dn2CppDecimal>(b)) == 0) ? 1 : 0;
}
// string.Format("{0:F2}", d) / $"{d:N0}" — the only spec-aware formatter that
// reads the culture's NumberFormatInfo.
static Dn2CppString* dn2cpp_decimal_box_formatspec(Dn2CppObject* o, Dn2CppString* spec,
                                                   const Dn2CppNumberFormatInfo* nfi)
{ return dn2cpp_decimal_format_c(*dn2cpp_boxed<Dn2CppDecimal>(o), spec, nfi); }

// Decimal's five public fields — real .NET's whole surface for it. (The argument
// for hand-writing an owned handle's field table is at dn2cpp_primflds_bool in
// dn2cpp_typeinfo.cpp.) They are `static readonly`, not literals — the CLR has no
// decimal constant form — so each one is a value the getter constructs.
#define DN2CPP_DECFLD(nm, lo, mid, hi, neg) \
    static Dn2CppObject* dn2cpp_ownfld_decimal_##nm(Dn2CppObject*) \
    { Dn2CppDecimal v = dn2cpp_decimal_from_parts((lo), (mid), (hi), (neg), 0); \
      return dn2cpp_box(&dn2cpp_decimal_type, &v, sizeof(v)); }
#define DN2CPP_DECFLD_ROW(nm) \
    { #nm, &dn2cpp_decimal_type, &dn2cpp_decimal_type, \
      DN2CPP_FLDA_STATIC | DN2CPP_FLDA_PUBLIC | DN2CPP_FLDA_INITONLY, \
      dn2cpp_ownfld_decimal_##nm, nullptr, nullptr, 0, 0x36, 0 }
DN2CPP_DECFLD(Zero, 0, 0, 0, 0)
DN2CPP_DECFLD(One, 1, 0, 0, 0)
DN2CPP_DECFLD(MinusOne, 1, 0, 0, 1)
DN2CPP_DECFLD(MaxValue, -1, -1, -1, 0)
DN2CPP_DECFLD(MinValue, -1, -1, -1, 1)
static const Dn2CppFieldInfo dn2cpp_ownflds_decimal[] = {
    DN2CPP_DECFLD_ROW(Zero), DN2CPP_DECFLD_ROW(One), DN2CPP_DECFLD_ROW(MinusOne),
    DN2CPP_DECFLD_ROW(MaxValue), DN2CPP_DECFLD_ROW(MinValue),
};
#undef DN2CPP_DECFLD
#undef DN2CPP_DECFLD_ROW

extern const Dn2CppType dn2cpp_decimal_type_obj;
const Dn2CppTypeInfo dn2cpp_decimal_type = dn2cpp_ti_with_formatspec(
    dn2cpp_ti_with_typeobject({ "System.Decimal", nullptr, (int32_t)sizeof(Dn2CppDecimal), nullptr, nullptr, 0, &dn2cpp_decimal_box_tostring, &dn2cpp_decimal_box_hash, &dn2cpp_decimal_box_equals, (DN2CPP_TF_VALUETYPE | DN2CPP_TF_SEALED), dn2cpp_ownflds_decimal, 5 }, &dn2cpp_decimal_type_obj),
    &dn2cpp_decimal_box_formatspec);
const Dn2CppType dn2cpp_decimal_type_obj = { { &dn2cpp_type_type }, &dn2cpp_decimal_type };

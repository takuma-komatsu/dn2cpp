// dn2cpp_parse.cpp — the NumberStyles-honoring numeric parse engine.
//
// A port of System.Number's TryParseNumber state machine over the modeled
// Dn2CppNumberFormatInfo, plus the conversions on top of it: every integer width
// (including the hex/binary specifier scanners), double/single (rebuilt to an
// invariant ASCII form and handed to strtod/strtof for the correctly rounded
// binary conversion), and the shared digit/scale/sign buffer System.Decimal's
// styles parser consumes.
//
// Semantics matching real .NET, each easy to get wrong:
//   - the parser trims only ASCII whitespace (0x20, 0x09..0x0D);
//   - group separators need a preceding digit but are otherwise free-form
//     (.NET does not validate group sizes: "1,2,3" and "123," parse);
//   - a group separator of U+00A0/U+202F also matches a plain space;
//   - AllowDecimalPoint on an integer accepts an all-zero fraction and
//     reports a non-zero fraction as *overflow*, not format;
//   - AllowHexSpecifier/AllowBinarySpecifier combine only with the whitespace
//     bits (anything else, both together, or an undefined bit is an
//     ArgumentException — thrown from TryParse as well);
//   - unsigned targets overflow on any negative parse except a bare "-0";
//   - the float path falls back to the culture's Infinity/NaN symbols with a
//     full-Unicode trim and case-insensitive compare, ignoring the styles;
//   - AllowCurrencySymbol is a VALID style, so it is accepted and an input
//     without the symbol parses as the currency-less path; consuming a symbol
//     that IS present is unmodeled and refused loudly (see the currency check
//     in dn2cpp_parse_number_styles).
#include "dn2cpp_core.h"

#include <cmath>
#include <cstdint>
#include <cstdio>
#include <cstdlib>
#include <cstring>

// NumberStyles bits.
static constexpr int32_t NS_LEADWHITE = 0x1;
static constexpr int32_t NS_TRAILWHITE = 0x2;
static constexpr int32_t NS_LEADSIGN = 0x4;
static constexpr int32_t NS_TRAILSIGN = 0x8;
static constexpr int32_t NS_PARENS = 0x10;
static constexpr int32_t NS_DECIMAL = 0x20;
static constexpr int32_t NS_THOUSANDS = 0x40;
static constexpr int32_t NS_EXPONENT = 0x80;
static constexpr int32_t NS_CURRENCY = 0x100;
static constexpr int32_t NS_HEX = 0x200;
static constexpr int32_t NS_BINARY = 0x400;

static bool ns_is_white(char16_t c) { return c == 0x20 || (c >= 0x09 && c <= 0x0D); }
static bool ns_is_digit(char16_t c) { return c >= u'0' && c <= u'9'; }

// ValidateParseStyleInteger: undefined bits, or a hex/binary specifier
// combined with anything beyond leading/trailing whitespace (or with each
// other), throw ArgumentException — from TryParse too. AllowCurrencySymbol is
// NOT an invalid style (it is part of NumberStyles.Any / .Currency, which .NET
// accepts): it passes validation here and is handled at parse time.
static void ns_validate_integer(int32_t styles)
{
    if ((styles & ~0x7FF) != 0)
        dn2cpp_throw_argument();
    if ((styles & (NS_HEX | NS_BINARY)) != 0)
    {
        if ((styles & ~(NS_LEADWHITE | NS_TRAILWHITE | NS_HEX | NS_BINARY)) != 0
            || (styles & (NS_HEX | NS_BINARY)) == (NS_HEX | NS_BINARY))
            dn2cpp_throw_argument();
    }
}

// ValidateParseStyleFloatingPoint: undefined bits or any hex/binary specifier
// throw ArgumentException. Shared with the decimal styles parser.
// AllowCurrencySymbol is a valid style here too (handled at parse time).
void dn2cpp_parse_validate_fp_styles(int32_t styles)
{
    if ((styles & ~0x7FF) != 0 || (styles & (NS_HEX | NS_BINARY)) != 0)
        dn2cpp_throw_argument();
}

// MatchChars: the separator/sign string `t` (non-empty) matched at p[i..],
// with .NET's space-replacing rule — a U+00A0/U+202F separator code unit also
// matches a plain U+0020 in the input. Returns the index just past the match,
// or -1.
static int32_t ns_match_str(const char16_t* p, int32_t n, int32_t i, Dn2CppString* t)
{
    if (t == nullptr || t->length == 0 || i + t->length > n)
        return -1;
    for (int32_t k = 0; k < t->length; k++)
    {
        char16_t want = t->chars[k], have = p[i + k];
        if (have != want && !((want == 0x00A0 || want == 0x202F) && have == 0x20))
            return -1;
    }
    return i + t->length;
}

// The TryParseNumber state machine. kind: 0 = Integer (trailing zeros are not
// recorded as significant digits), 1 = Float, 2 = Decimal. Fills `num` with
// the significant digits, the scale (digits left of the decimal point, with
// any exponent folded in), the sign, and the truncated-nonzero-tail flag.
// Returns 1 when the number grammar matched AND consumed the entire input.
int32_t dn2cpp_parse_number_styles(const char16_t* p, int32_t n, int32_t styles,
                                   const Dn2CppNumberFormatInfo* nfi, int32_t kind,
                                   Dn2CppNumberParse* num)
{
    nfi = dn2cpp_nfi_or_current(nfi);
    Dn2CppString* decSep = nfi->numberDecimal;
    Dn2CppString* grpSep = nfi->numberGroup;
    Dn2CppString* negSign = nfi->negativeSign;

    // AllowCurrencySymbol permits — does not require — the culture's currency
    // symbol, and the state machine below does not model consuming one. An input
    // that actually contains the symbol is refused with a catchable exception
    // rather than silently rejected or mis-valued; an input without it falls
    // through to the currency-less parse, which is the case that matters (the
    // setting/environment readers pass NumberStyles.Any over plain decimal text).
    if ((styles & NS_CURRENCY) != 0)
    {
        Dn2CppString* cur = nfi->currencySymbol;
        if (cur != nullptr && cur->length > 0)
            for (int32_t i = 0; i + cur->length <= n; i++)
                if (ns_match_str(p, n, i, cur) >= 0)
                    dn2cpp_throw_platform_not_supported(
                        "NumberStyles.AllowCurrencySymbol: input contains a currency symbol (not modeled)");
    }

    num->count = 0;
    num->scale = 0;
    num->negative = 0;
    num->hasNonZeroTail = 0;

    constexpr int32_t ST_SIGN = 1, ST_PARENS = 2, ST_DIGITS = 4, ST_NONZERO = 8, ST_DECIMAL = 16;
    int32_t state = 0;
    int32_t i = 0;

    // Leading whitespace / sign / '('. Whitespace is not consumed once a sign
    // was seen ("- 123" fails) — the modeled cultures all have
    // NumberNegativePattern != 2, so .NET's currency-pattern carve-out is moot.
    while (i < n)
    {
        char16_t ch = p[i];
        if (ns_is_white(ch) && (styles & NS_LEADWHITE) != 0 && (state & ST_SIGN) == 0)
        {
            i++;
            continue;
        }
        int32_t next;
        if ((styles & NS_LEADSIGN) != 0 && (state & ST_SIGN) == 0)
        {
            if (ch == u'+')
            {
                state |= ST_SIGN;
                i++;
                continue;
            }
            if ((next = ns_match_str(p, n, i, negSign)) >= 0)
            {
                state |= ST_SIGN;
                num->negative = 1;
                i = next;
                continue;
            }
        }
        if (ch == u'(' && (styles & NS_PARENS) != 0 && (state & ST_SIGN) == 0)
        {
            state |= ST_SIGN | ST_PARENS;
            num->negative = 1;
            i++;
            continue;
        }
        break;
    }

    // Digits, decimal separator, group separators. Leading zeros are skipped
    // (fractional leading zeros lower the scale); a group separator needs at
    // least one preceding digit and no decimal point yet, but placement is
    // otherwise unvalidated, matching .NET.
    int32_t digEnd = 0;
    int32_t digCount = 0;
    constexpr int32_t maxDig = DN2CPP_PARSE_MAX_DIGITS;
    while (i < n)
    {
        char16_t ch = p[i];
        if (ns_is_digit(ch))
        {
            state |= ST_DIGITS;
            if (ch != u'0' || (state & ST_NONZERO) != 0)
            {
                if (digCount < maxDig)
                {
                    num->digits[digCount] = (uint8_t)ch;
                    // Integer kind: trailing zeros are recorded but not counted
                    // as significant (the conversion pads with zeros by scale).
                    if (ch != u'0' || kind != 0)
                        digEnd = digCount + 1;
                }
                else if (ch != u'0')
                    num->hasNonZeroTail = 1;
                if ((state & ST_DECIMAL) == 0)
                    num->scale++;
                digCount++;
                state |= ST_NONZERO;
            }
            else if ((state & ST_DECIMAL) != 0)
                num->scale--;
            i++;
            continue;
        }
        int32_t next;
        if ((styles & NS_DECIMAL) != 0 && (state & ST_DECIMAL) == 0
            && (next = ns_match_str(p, n, i, decSep)) >= 0)
        {
            state |= ST_DECIMAL;
            i = next;
            continue;
        }
        if ((styles & NS_THOUSANDS) != 0 && (state & ST_DIGITS) != 0 && (state & ST_DECIMAL) == 0
            && (next = ns_match_str(p, n, i, grpSep)) >= 0)
        {
            i = next;
            continue;
        }
        break;
    }
    num->count = digEnd;
    num->digits[digEnd] = 0;

    if ((state & ST_DIGITS) != 0)
    {
        // Exponent: only when digits precede it; a dangling "1e"/"1e+" leaves
        // the 'e' unconsumed (a format error via the full-consumption check).
        if ((styles & NS_EXPONENT) != 0 && i < n && (p[i] == u'e' || p[i] == u'E'))
        {
            int32_t save = i;
            bool negExp = false;
            int32_t next;
            i++;
            if (i < n && p[i] == u'+')
                i++;
            else if ((next = ns_match_str(p, n, i, negSign)) >= 0)
            {
                i = next;
                negExp = true;
            }
            if (i < n && ns_is_digit(p[i]))
            {
                int64_t exp = 0;
                while (i < n && ns_is_digit(p[i]))
                {
                    // Saturate a huge exponent: the conversion overflows (or
                    // flushes to zero) the same way .NET's int.MaxValue cap does.
                    if (exp < 100000000)
                        exp = exp * 10 + (p[i] - u'0');
                    i++;
                }
                int64_t sc = (int64_t)num->scale + (negExp ? -exp : exp);
                num->scale = sc > 2000000000 ? 2000000000 : sc < -2000000000 ? (int32_t)-2000000000 : (int32_t)sc;
            }
            else
                i = save;
        }

        // Trailing whitespace / trailing sign / ')'.
        while (i < n)
        {
            char16_t ch = p[i];
            if (ns_is_white(ch) && (styles & NS_TRAILWHITE) != 0)
            {
                i++;
                continue;
            }
            int32_t next;
            if ((styles & NS_TRAILSIGN) != 0 && (state & ST_SIGN) == 0)
            {
                if (ch == u'+')
                {
                    state |= ST_SIGN;
                    i++;
                    continue;
                }
                if ((next = ns_match_str(p, n, i, negSign)) >= 0)
                {
                    state |= ST_SIGN;
                    num->negative = 1;
                    i = next;
                    continue;
                }
            }
            if (ch == u')' && (state & ST_PARENS) != 0)
            {
                state &= ~ST_PARENS;
                i++;
                continue;
            }
            break;
        }

        if ((state & ST_PARENS) == 0 && i == n)
        {
            if ((state & ST_NONZERO) == 0)
            {
                // All-zero value: integers/floats reset the scale, and an
                // integer with no decimal point drops the sign ("-0" == 0,
                // while "-0.00" keeps IsNegative for the unsigned overflow).
                if (kind != 2)
                    num->scale = 0;
                if (kind == 0 && (state & ST_DECIMAL) == 0)
                    num->negative = 0;
            }
            return 1;
        }
    }
    return 0;
}

// digits+scale+sign -> integer of the given width. Status: 0 ok, 2 overflow.
// A scale below the significant digit count means non-zero fractional digits
// — .NET reports that as overflow, not format.
static int32_t ns_int_from_number(const Dn2CppNumberParse* num, int32_t bitWidth, int32_t isSigned,
                                  int64_t* out)
{
    *out = 0;
    int32_t scale = num->scale;
    if (scale > 20 || scale < num->count)
        return 2;
    if (!isSigned && num->negative)
        return 2;
    uint64_t value = 0;
    for (int32_t j = 0; j < scale; j++)
    {
        uint64_t d = j < num->count ? (uint64_t)(num->digits[j] - '0') : 0;
        if (value > (UINT64_MAX - d) / 10)
            return 2;
        value = value * 10 + d;
    }
    if (isSigned)
    {
        uint64_t negMax = 1ULL << (bitWidth - 1);
        if (num->negative)
        {
            if (value > negMax)
                return 2;
            *out = (int64_t)(0 - value);
        }
        else
        {
            if (value > negMax - 1)
                return 2;
            *out = (int64_t)value;
        }
    }
    else
    {
        if (bitWidth < 64 && value > ((1ULL << bitWidth) - 1))
            return 2;
        *out = (int64_t)value;
    }
    return 0;
}

// AllowHexSpecifier / AllowBinarySpecifier scanner: optional whitespace per
// the styles, then bare digits parsed as the UNSIGNED bit pattern of the
// width and reinterpreted for signed targets ("FF" -> sbyte -1). More
// significant digits (after leading zeros) than the width holds is overflow —
// reported as soon as capacity is exceeded, before any trailing-garbage
// check, matching .NET. Status: 0 ok, 1 format, 2 overflow.
static int32_t ns_hexbin_from_chars(const char16_t* p, int32_t n, int32_t styles, int32_t bitWidth,
                                    int32_t isSigned, bool isHex, int64_t* out)
{
    *out = 0;
    int32_t i = 0, end = n;
    if ((styles & NS_LEADWHITE) != 0)
        while (i < end && ns_is_white(p[i]))
            i++;
    if ((styles & NS_TRAILWHITE) != 0)
        while (end > i && ns_is_white(p[end - 1]))
            end--;
    if (i >= end)
        return 1;
    const int32_t cap = isHex ? bitWidth / 4 : bitWidth;
    uint64_t value = 0;
    int32_t sig = 0;
    bool leading = true;
    for (; i < end; i++)
    {
        char16_t c = p[i];
        int32_t d;
        if (isHex)
        {
            if (c >= u'0' && c <= u'9') d = c - u'0';
            else if (c >= u'a' && c <= u'f') d = c - u'a' + 10;
            else if (c >= u'A' && c <= u'F') d = c - u'A' + 10;
            else
                return 1;
        }
        else
        {
            if (c != u'0' && c != u'1')
                return 1;
            d = c - u'0';
        }
        if (leading && d == 0)
            continue;
        leading = false;
        if (++sig > cap)
            return 2;
        value = isHex ? (value << 4) | (uint64_t)d : (value << 1) | (uint64_t)d;
    }
    if (isSigned)
    {
        switch (bitWidth)
        {
            case 8: *out = (int8_t)value; break;
            case 16: *out = (int16_t)value; break;
            case 32: *out = (int32_t)value; break;
            default: *out = (int64_t)value; break;
        }
    }
    else
        *out = (int64_t)value;
    return 0;
}

// The integer engine core. Status: 0 ok, 1 format, 2 overflow (argument
// errors throw inside the styles validation).
static int32_t ns_integer_core(const char16_t* p, int32_t n, int32_t styles,
                               const Dn2CppNumberFormatInfo* nfi, int32_t bitWidth,
                               int32_t isSigned, int64_t* out)
{
    ns_validate_integer(styles);
    *out = 0;
    if ((styles & NS_HEX) != 0)
        return ns_hexbin_from_chars(p, n, styles, bitWidth, isSigned, true, out);
    if ((styles & NS_BINARY) != 0)
        return ns_hexbin_from_chars(p, n, styles, bitWidth, isSigned, false, out);
    Dn2CppNumberParse num;
    if (!dn2cpp_parse_number_styles(p, n, styles, nfi, 0, &num))
        return 1;
    return ns_int_from_number(&num, bitWidth, isSigned, out);
}

int32_t dn2cpp_integer_tryparse_chars(const char16_t* p, int32_t n, int32_t styles,
                                      const Dn2CppNumberFormatInfo* nfi, int32_t bitWidth,
                                      int32_t isSigned, int64_t* out)
{
    return ns_integer_core(p, n, styles, nfi, bitWidth, isSigned, out) == 0 ? 1 : 0;
}

int32_t dn2cpp_integer_tryparse_str(Dn2CppString* s, int32_t styles,
                                    const Dn2CppNumberFormatInfo* nfi, int32_t bitWidth,
                                    int32_t isSigned, int64_t* out)
{
    if (s == nullptr)
    {
        // Styles are validated (and may throw) before the null check, like .NET.
        ns_validate_integer(styles);
        *out = 0;
        return 0;
    }
    return dn2cpp_integer_tryparse_chars(s->chars, s->length, styles, nfi, bitWidth, isSigned, out);
}

// The FormatException a Parse form raises, quoting the input the way real .NET's
// message does: the whole span the engine was handed, not the character it
// stopped at.
[[noreturn]] static void ns_throw_format_value(const char16_t* p, int32_t n)
{
    dn2cpp_throw_format_value(dn2cpp_string_from_chars(p, n));
}

int64_t dn2cpp_integer_parse_chars(const char16_t* p, int32_t n, int32_t styles,
                                   const Dn2CppNumberFormatInfo* nfi, int32_t bitWidth,
                                   int32_t isSigned)
{
    int64_t v;
    switch (ns_integer_core(p, n, styles, nfi, bitWidth, isSigned, &v))
    {
        case 1: ns_throw_format_value(p, n);
        case 2: dn2cpp_overflow();
        default: break;
    }
    return v;
}

int64_t dn2cpp_integer_parse_str(Dn2CppString* s, int32_t styles,
                                 const Dn2CppNumberFormatInfo* nfi, int32_t bitWidth,
                                 int32_t isSigned)
{
    if (s == nullptr)
    {
        ns_validate_integer(styles);
        dn2cpp_throw_argument_null();
    }
    return dn2cpp_integer_parse_chars(s->chars, s->length, styles, nfi, bitWidth, isSigned);
}

// ---- floating point ---------------------------------------------------------

// Case-insensitive (ASCII fold; the symbols are ASCII or caseless like U+221E)
// match of a char16 run against a symbol string.
static bool ns_eq_icase(const char16_t* p, int32_t n, Dn2CppString* t)
{
    if (t == nullptr || t->length != n)
        return false;
    for (int32_t k = 0; k < n; k++)
    {
        char16_t a = p[k], b = t->chars[k];
        if (a >= u'A' && a <= u'Z') a = (char16_t)(a + 32);
        if (b >= u'A' && b <= u'Z') b = (char16_t)(b + 32);
        if (a != b)
            return false;
    }
    return true;
}

// The Infinity/NaN symbol fallback .NET applies when the number grammar
// fails: a full-Unicode trim (unlike the parser's ASCII whitespace) and a
// case-insensitive compare against the culture symbols, with an optional
// leading +/negative sign — all ignoring the styles bits entirely.
static bool ns_fp_special(const char16_t* p, int32_t n, const Dn2CppNumberFormatInfo* nfi, double* out)
{
    int32_t a = 0, b = n;
    while (a < b && dn2cpp_char_is_whitespace(p[a]))
        a++;
    while (b > a && dn2cpp_char_is_whitespace(p[b - 1]))
        b--;
    const char16_t* q = p + a;
    int32_t m = b - a;
    if (ns_eq_icase(q, m, nfi->posInf)) { *out = INFINITY; return true; }
    if (ns_eq_icase(q, m, nfi->negInf)) { *out = -INFINITY; return true; }
    if (ns_eq_icase(q, m, nfi->nan)) { *out = NAN; return true; }
    if (m > 0 && q[0] == u'+')
    {
        if (ns_eq_icase(q + 1, m - 1, nfi->posInf)) { *out = INFINITY; return true; }
        if (ns_eq_icase(q + 1, m - 1, nfi->nan)) { *out = NAN; return true; }
        return false;
    }
    int32_t next = ns_match_str(q, m, 0, nfi->negativeSign);
    if (next > 0)
    {
        if (ns_eq_icase(q + next, m - next, nfi->posInf)) { *out = -INFINITY; return true; }
        if (ns_eq_icase(q + next, m - next, nfi->nan)) { *out = NAN; return true; }
    }
    return false;
}

// The float/double engine core. Status: 0 ok, 1 format. Overflow saturates to
// ±Infinity and underflow to (signed) zero, matching .NET — never an error.
// The digits are rebuilt as an invariant ASCII "0.<digits>e<scale>" string so
// strtod/strtof perform the correctly rounded binary conversion; a truncated
// non-zero tail appends a sentinel '1' to stay on the correct side of a
// halfway case, mirroring NumberBuffer.HasNonZeroTail.
static int32_t ns_fp_core(const char16_t* p, int32_t n, int32_t styles,
                          const Dn2CppNumberFormatInfo* nfi, int32_t isSingle, double* out)
{
    dn2cpp_parse_validate_fp_styles(styles);
    nfi = dn2cpp_nfi_or_current(nfi);
    *out = 0.0;
    Dn2CppNumberParse num;
    if (!dn2cpp_parse_number_styles(p, n, styles, nfi, 1, &num))
        return ns_fp_special(p, n, nfi, out) ? 0 : 1;
    char buf[DN2CPP_PARSE_MAX_DIGITS + 32];
    int32_t o = 0;
    if (num.negative)
        buf[o++] = '-';
    buf[o++] = '0';
    buf[o++] = '.';
    for (int32_t j = 0; j < num.count; j++)
        buf[o++] = (char)num.digits[j];
    if (num.hasNonZeroTail)
        buf[o++] = '1';
    if (num.count == 0 && !num.hasNonZeroTail)
        buf[o++] = '0';
    o += snprintf(buf + o, 16, "e%d", (int)num.scale);
    buf[o] = '\0';
    *out = isSingle ? (double)strtof(buf, nullptr) : strtod(buf, nullptr);
    return 0;
}

int32_t dn2cpp_fp_tryparse_chars(const char16_t* p, int32_t n, int32_t styles,
                                 const Dn2CppNumberFormatInfo* nfi, int32_t isSingle, double* out)
{
    return ns_fp_core(p, n, styles, nfi, isSingle, out) == 0 ? 1 : 0;
}

int32_t dn2cpp_fp_tryparse_str(Dn2CppString* s, int32_t styles,
                               const Dn2CppNumberFormatInfo* nfi, int32_t isSingle, double* out)
{
    if (s == nullptr)
    {
        dn2cpp_parse_validate_fp_styles(styles);
        *out = 0.0;
        return 0;
    }
    return dn2cpp_fp_tryparse_chars(s->chars, s->length, styles, nfi, isSingle, out);
}

double dn2cpp_fp_parse_chars(const char16_t* p, int32_t n, int32_t styles,
                             const Dn2CppNumberFormatInfo* nfi, int32_t isSingle)
{
    double v;
    if (ns_fp_core(p, n, styles, nfi, isSingle, &v) != 0)
        ns_throw_format_value(p, n);
    return v;
}

double dn2cpp_fp_parse_str(Dn2CppString* s, int32_t styles,
                           const Dn2CppNumberFormatInfo* nfi, int32_t isSingle)
{
    if (s == nullptr)
    {
        dn2cpp_parse_validate_fp_styles(styles);
        dn2cpp_throw_argument_null();
    }
    return dn2cpp_fp_parse_chars(s->chars, s->length, styles, nfi, isSingle);
}

// ---- UTF-8 input (IUtf8SpanParsable) -----------------------------------------
//
// The ReadOnlySpan<byte> overloads widen to UTF-16 and run the SAME engine as every
// other input form rather than carrying a second scanner over bytes. The grammar is
// not pure ASCII — the decimal point, group separator and negative sign are UTF-16
// strings off the NumberFormatInfo, and real cultures supply non-ASCII ones (U+00A0 /
// U+202F, U+2212) — so a byte-wise scanner would have to transcode them anyway, in a
// second place that can drift from the first.
//
// The decode is Encoding.UTF8.GetString's (maximal-subpart U+FFFD replacement), so
// malformed UTF-8 becomes replacement chars, which are neither digits nor any
// NumberFormatInfo symbol: the parse fails exactly as .NET does on the same bytes.
static Dn2CppString* ns_utf8_widen(const char* p, int32_t n)
{
    return dn2cpp_string_decode_utf8(p, n < 0 ? 0 : n);
}

int32_t dn2cpp_integer_tryparse_utf8(const char* p, int32_t n, int32_t styles,
                                     const Dn2CppNumberFormatInfo* nfi, int32_t bitWidth,
                                     int32_t isSigned, int64_t* out)
{
    Dn2CppString* s = ns_utf8_widen(p, n);
    return dn2cpp_integer_tryparse_chars(s->chars, s->length, styles, nfi, bitWidth, isSigned, out);
}

int64_t dn2cpp_integer_parse_utf8(const char* p, int32_t n, int32_t styles,
                                  const Dn2CppNumberFormatInfo* nfi, int32_t bitWidth,
                                  int32_t isSigned)
{
    Dn2CppString* s = ns_utf8_widen(p, n);
    return dn2cpp_integer_parse_chars(s->chars, s->length, styles, nfi, bitWidth, isSigned);
}

int32_t dn2cpp_fp_tryparse_utf8(const char* p, int32_t n, int32_t styles,
                                const Dn2CppNumberFormatInfo* nfi, int32_t isSingle, double* out)
{
    Dn2CppString* s = ns_utf8_widen(p, n);
    return dn2cpp_fp_tryparse_chars(s->chars, s->length, styles, nfi, isSingle, out);
}

double dn2cpp_fp_parse_utf8(const char* p, int32_t n, int32_t styles,
                            const Dn2CppNumberFormatInfo* nfi, int32_t isSingle)
{
    Dn2CppString* s = ns_utf8_widen(p, n);
    return dn2cpp_fp_parse_chars(s->chars, s->length, styles, nfi, isSingle);
}

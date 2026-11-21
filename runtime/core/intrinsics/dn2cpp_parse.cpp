// dn2cpp_parse.cpp — the NumberStyles-honoring numeric parse engine.
//
// A port of System.Number's TryParseNumber state machine over the modeled
// Dn2CppNumberFormatInfo, plus the conversions on top of it: every integer width
// (including the hex/binary specifier scanners), double/single (decimal text
// rebuilt to an invariant ASCII form and handed to strtod/strtof for the
// correctly rounded binary conversion; hexadecimal text rounded here), and the
// shared digit/scale/sign buffer System.Decimal's styles parser consumes.
//
// Semantics matching real .NET, each easy to get wrong:
//   - the parser trims only ASCII whitespace (0x20, 0x09..0x0D);
//   - group separators need a preceding digit but are otherwise free-form
//     (.NET does not validate group sizes: "1,2,3" and "123," parse);
//   - in UTF-16 input U+00A0, U+202F and U+0020 match each other in a
//     separator or sign, whichever side carries which; UTF-8 input matches
//     only the identical text;
//   - when both signs match, the longer one wins and a tie is positive;
//   - AllowDecimalPoint on an integer accepts an all-zero fraction and
//     reports a non-zero fraction as *overflow*, not format;
//   - on an integer AllowHexSpecifier/AllowBinarySpecifier combine only with
//     the whitespace bits (anything else, both together, or an undefined bit
//     is an ArgumentException — thrown from TryParse as well); a float takes
//     the hex specifier only with AllowExponent and within HexFloat, never the
//     binary one, and a decimal takes neither;
//   - unsigned targets overflow on any negative parse except a bare "-0";
//   - the float path falls back to the culture's Infinity/NaN symbols with a
//     full-Unicode trim and case-insensitive compare, ignoring the styles; an
//     empty symbol matches nothing;
//   - TryParse reads a null string as the empty span, so the provider is
//     resolved for it too;
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
#include <string>

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
static constexpr int32_t NS_HEXFLOAT = NS_LEADWHITE | NS_TRAILWHITE | NS_LEADSIGN | NS_DECIMAL | NS_EXPONENT | NS_HEX;

static bool ns_is_white(char16_t c) { return c == 0x20 || (c >= 0x09 && c <= 0x0D); }
static bool ns_is_digit(char16_t c) { return c >= u'0' && c <= u'9'; }

// ValidateParseStyleInteger: undefined bits, or a hex/binary specifier
// combined with anything beyond leading/trailing whitespace (or with each
// other), throw ArgumentException — from TryParse too. AllowCurrencySymbol is
// NOT an invalid style (it is part of NumberStyles.Any / .Currency, which .NET
// accepts): it passes validation here and is handled at parse time.
void dn2cpp_parse_validate_integer_styles(int32_t styles)
{
    if ((styles & ~0x7FF) != 0)
        dn2cpp_throw_argument_param(DN2CPP_SR_INVALID_NUMBER_STYLES, "style");
    if ((styles & (NS_HEX | NS_BINARY)) != 0)
    {
        if ((styles & ~(NS_LEADWHITE | NS_TRAILWHITE | NS_HEX | NS_BINARY)) != 0
            || (styles & (NS_HEX | NS_BINARY)) == (NS_HEX | NS_BINARY))
            dn2cpp_throw_argument_param(DN2CPP_SR_INVALID_HEX_BINARY_STYLE, "style");
    }
}

// ValidateParseStyleFloatingPoint: the hex specifier is legal only with
// AllowExponent and only among HexFloat's bits; the binary specifier never is.
// An undefined bit is reported first, then the binary specifier.
// AllowCurrencySymbol is a valid style here too (handled at parse time).
void dn2cpp_parse_validate_fp_styles(int32_t styles)
{
    if ((styles & (~0x7FF | NS_HEX | NS_BINARY)) == 0)
        return;
    if ((styles & ~NS_HEXFLOAT) == 0 && ((styles & NS_HEX) == 0 || (styles & NS_EXPONENT) != 0))
        return;
    dn2cpp_throw_argument_param(
        (styles & ~0x7FF) != 0 ? DN2CPP_SR_INVALID_NUMBER_STYLES
        : (styles & NS_BINARY) != 0 ? DN2CPP_SR_BINARY_STYLE_NOT_SUPPORTED
        : DN2CPP_SR_INVALID_HEX_FLOAT_STYLE,
        "style");
}

// ValidateParseStyleDecimal: undefined bits or any hex/binary specifier throw
// ArgumentException.
void dn2cpp_parse_validate_decimal_styles(int32_t styles)
{
    if ((styles & ~0x7FF) != 0)
        dn2cpp_throw_argument_param(DN2CPP_SR_INVALID_NUMBER_STYLES, "style");
    if ((styles & (NS_HEX | NS_BINARY)) != 0)
        dn2cpp_throw_argument_param(DN2CPP_SR_HEX_BINARY_STYLES_NOT_SUPPORTED, "style");
}

static char16_t ns_fold_space(char16_t c) { return c == 0x00A0 || c == 0x202F ? u' ' : c; }

// How a separator or sign string compares with the input text.
enum NsMatch
{
    NS_MATCH_SPACES, // U+00A0, U+202F and U+0020 are interchangeable on either side
    NS_MATCH_UNITS,  // unit for unit
    NS_MATCH_UTF8,   // text widened from UTF-8 input: the string as its UTF-8
                     // encoding, where an unpaired surrogate is U+FFFD
};

static bool ns_unpaired_surrogate(const Dn2CppString* t, int32_t k)
{
    char16_t c = t->chars[k];
    if (c >= 0xD800 && c <= 0xDBFF)
        return k + 1 >= t->length || t->chars[k + 1] < 0xDC00 || t->chars[k + 1] > 0xDFFF;
    if (c >= 0xDC00 && c <= 0xDFFF)
        return k == 0 || t->chars[k - 1] < 0xD800 || t->chars[k - 1] > 0xDBFF;
    return false;
}

// MatchChars: the separator/sign string `t` (non-empty) matched at p[i..].
// Returns the index just past the match, or -1.
static int32_t ns_match_str(const char16_t* p, int32_t n, int32_t i, Dn2CppString* t, NsMatch match)
{
    if (t == nullptr || t->length == 0 || i + t->length > n)
        return -1;
    for (int32_t k = 0; k < t->length; k++)
    {
        char16_t want = t->chars[k], have = p[i + k];
        if (match == NS_MATCH_UTF8 && ns_unpaired_surrogate(t, k))
            want = 0xFFFD;
        if (have != want && (match != NS_MATCH_SPACES || ns_fold_space(have) != ns_fold_space(want)))
            return -1;
    }
    return i + t->length;
}

// MatchSignChars: the index just past a sign at p[i..], or -1. A sign that is
// a prefix of the other must not shadow it, so the longer match wins and a tie
// is positive. A one-character dash-like NegativeSign also accepts U+002D.
static int32_t ns_match_sign(const char16_t* p, int32_t n, int32_t i,
                             const Dn2CppNumberFormatInfo* nfi, NsMatch match, bool* negative)
{
    Dn2CppString* neg = nfi->negativeSign;
    int32_t posEnd = ns_match_str(p, n, i, nfi->positiveSign, match);
    int32_t negEnd = ns_match_str(p, n, i, neg, match);
    if (negEnd < 0 && i < n && p[i] == u'-' && neg != nullptr && neg->length == 1)
    {
        switch (neg->chars[0])
        {
            case 0x2012: case 0x207B: case 0x208B: case 0x2212: case 0x2796: case 0xFE63: case 0xFF0D:
                negEnd = i + 1;
                break;
            default:
                break;
        }
    }
    *negative = negEnd > posEnd;
    return *negative ? negEnd : posEnd;
}

// The TryParseNumber state machine. kind: 0 = Integer (trailing zeros are not
// recorded as significant digits), 1 = Float, 2 = Decimal. Fills `num` with
// the significant digits, the scale (digits left of the decimal point, with
// any exponent folded in), the sign, and the truncated-nonzero-tail flag.
// Returns 1 when the number grammar matched AND consumed the entire input.
// `utf8` marks text widened from UTF-8 input.
int32_t dn2cpp_parse_number_styles(const char16_t* p, int32_t n, int32_t styles,
                                   const Dn2CppNumberFormatInfo* nfi, int32_t kind,
                                   int32_t utf8, Dn2CppNumberParse* num)
{
    nfi = dn2cpp_nfi_or_current(nfi);
    const NsMatch match = utf8 != 0 ? NS_MATCH_UTF8 : NS_MATCH_SPACES;
    Dn2CppString* decSep = nfi->numberDecimal;
    Dn2CppString* grpSep = nfi->numberGroup;

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
                // The symbol is matched only before and after the digits; between
                // digits the same text is the separator the digit loop consumes.
                if (ns_match_str(p, n, i, cur, match) >= 0
                    && !(i > 0 && ns_is_digit(p[i - 1])
                         && (((styles & NS_THOUSANDS) != 0 && ns_match_str(p, n, i, grpSep, match) >= 0)
                             || ((styles & NS_DECIMAL) != 0 && ns_match_str(p, n, i, decSep, match) >= 0))))
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

    // A leading sign permits following whitespace only for the spaced
    // negative-number pattern.
    while (i < n)
    {
        char16_t ch = p[i];
        if (ns_is_white(ch) && (styles & NS_LEADWHITE) != 0 && ((state & ST_SIGN) == 0 || nfi->numberNegPattern == 2))
        {
            i++;
            continue;
        }
        int32_t next;
        bool negative;
        if ((styles & NS_LEADSIGN) != 0 && (state & ST_SIGN) == 0
            && (next = ns_match_sign(p, n, i, nfi, match, &negative)) >= 0)
        {
            state |= ST_SIGN;
            num->negative = negative ? 1 : 0;
            i = next;
            continue;
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
            && (next = ns_match_str(p, n, i, decSep, match)) >= 0)
        {
            state |= ST_DECIMAL;
            i = next;
            continue;
        }
        if ((styles & NS_THOUSANDS) != 0 && (state & ST_DIGITS) != 0 && (state & ST_DECIMAL) == 0
            && (next = ns_match_str(p, n, i, grpSep, match)) >= 0)
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
            if ((next = ns_match_sign(p, n, i, nfi, match, &negExp)) >= 0)
                i = next;
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
            bool negative;
            if ((styles & NS_TRAILSIGN) != 0 && (state & ST_SIGN) == 0
                && (next = ns_match_sign(p, n, i, nfi, match, &negative)) >= 0)
            {
                state |= ST_SIGN;
                num->negative = negative ? 1 : 0;
                i = next;
                continue;
            }
            if (ch == u')' && (state & ST_PARENS) != 0)
            {
                state &= ~ST_PARENS;
                i++;
                continue;
            }
            break;
        }

        // Only an all-NUL suffix may follow the grammar's trailing whitespace.
        while (i < n && p[i] == 0)
            i++;
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
    while (end > 0 && p[end - 1] == 0)
        end--;
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
                               int32_t isSigned, bool utf8, int64_t* out)
{
    dn2cpp_parse_validate_integer_styles(styles);
    *out = 0;
    nfi = dn2cpp_nfi_or_current(nfi);
    if ((styles & NS_HEX) != 0)
        return ns_hexbin_from_chars(p, n, styles, bitWidth, isSigned, true, out);
    if ((styles & NS_BINARY) != 0)
        return ns_hexbin_from_chars(p, n, styles, bitWidth, isSigned, false, out);
    Dn2CppNumberParse num;
    if (!dn2cpp_parse_number_styles(p, n, styles, nfi, 0, utf8, &num))
        return 1;
    return ns_int_from_number(&num, bitWidth, isSigned, out);
}

int32_t dn2cpp_integer_tryparse_chars(const char16_t* p, int32_t n, int32_t styles,
                                      const Dn2CppNumberFormatInfo* nfi, int32_t bitWidth,
                                      int32_t isSigned, int64_t* out)
{
    return ns_integer_core(p, n, styles, nfi, bitWidth, isSigned, false, out) == 0 ? 1 : 0;
}

int32_t dn2cpp_integer_tryparse_str(Dn2CppString* s, int32_t styles,
                                    const Dn2CppNumberFormatInfo* nfi, int32_t bitWidth,
                                    int32_t isSigned, int64_t* out)
{
    if (s == nullptr)
        return dn2cpp_integer_tryparse_chars(u"", 0, styles, nfi, bitWidth, isSigned, out);
    return dn2cpp_integer_tryparse_chars(s->chars, s->length, styles, nfi, bitWidth, isSigned, out);
}

// The FormatException a Parse form raises, quoting the input the way real .NET's
// message does: the whole span the engine was handed, not the character it
// stopped at.
[[noreturn]] static void ns_throw_format_value(const char16_t* p, int32_t n)
{
    dn2cpp_throw_format_value(dn2cpp_string_from_chars(p, n));
}

static int64_t ns_integer_parse(const char16_t* p, int32_t n, int32_t styles,
                                const Dn2CppNumberFormatInfo* nfi, int32_t bitWidth,
                                int32_t isSigned, bool utf8)
{
    int64_t v;
    switch (ns_integer_core(p, n, styles, nfi, bitWidth, isSigned, utf8, &v))
    {
        case 1: ns_throw_format_value(p, n);
        case 2: dn2cpp_integer_overflow(bitWidth, isSigned);
        default: break;
    }
    return v;
}

int64_t dn2cpp_integer_parse_chars(const char16_t* p, int32_t n, int32_t styles,
                                   const Dn2CppNumberFormatInfo* nfi, int32_t bitWidth,
                                   int32_t isSigned)
{
    return ns_integer_parse(p, n, styles, nfi, bitWidth, isSigned, false);
}

int64_t dn2cpp_integer_parse_str(Dn2CppString* s, int32_t styles,
                                 const Dn2CppNumberFormatInfo* nfi, int32_t bitWidth,
                                 int32_t isSigned)
{
    // Unlike TryParse, Parse rejects a null string before validating the styles.
    if (s == nullptr)
        dn2cpp_throw_argument_null_param("s");
    return dn2cpp_integer_parse_chars(s->chars, s->length, styles, nfi, bitWidth, isSigned);
}

// ---- floating point ---------------------------------------------------------

// Whether `t` is an OrdinalIgnoreCase prefix of p[i..n). An empty `t` is.
static bool ns_starts_icase(const char16_t* p, int32_t n, int32_t i, Dn2CppString* t)
{
    int32_t length = t != nullptr ? t->length : 0;
    if (length > n - i)
        return false;
    for (int32_t k = 0; k < length; k++)
    {
        if (dn2cpp_ordinal_upper(p[i + k]) != dn2cpp_ordinal_upper(t->chars[k]))
            return false;
    }
    return true;
}

// TryMatchSpecialValueSymbol: `t` as an OrdinalIgnoreCase prefix of p[i..n)
// with nothing but white space behind it. An empty symbol matches nothing.
static bool ns_match_special(const char16_t* p, int32_t n, int32_t i, Dn2CppString* t)
{
    if (t == nullptr || t->length == 0 || !ns_starts_icase(p, n, i, t))
        return false;
    i += t->length;
    while (i < n && dn2cpp_char_is_whitespace(p[i]))
        i++;
    return i == n;
}

// ---- Infinity/NaN matching in the UTF-8 byte domain ----
//
// CoreLib compares the symbol's UTF-8 bytes against the input's, so a case
// variant matches only when it occupies as many bytes, and an incomplete
// sequence at the cut never does. The input has been validated.

static std::string ns_encode_utf8(Dn2CppString* t)
{
    std::string r;
    for (int32_t i = 0; t != nullptr && i < t->length; i++)
    {
        uint32_t c = t->chars[i];
        if (c >= 0xD800u && c <= 0xDBFFu && i + 1 < t->length && t->chars[i + 1] >= 0xDC00u && t->chars[i + 1] <= 0xDFFFu)
            c = 0x10000u + ((c - 0xD800u) << 10) + (t->chars[++i] - 0xDC00u);
        else if (c >= 0xD800u && c <= 0xDFFFu)
            c = 0xFFFDu;
        if (c < 0x80u)
            r += static_cast<char>(c);
        else if (c < 0x800u)
        {
            r += static_cast<char>(0xC0u | (c >> 6));
            r += static_cast<char>(0x80u | (c & 0x3Fu));
        }
        else if (c < 0x10000u)
        {
            r += static_cast<char>(0xE0u | (c >> 12));
            r += static_cast<char>(0x80u | ((c >> 6) & 0x3Fu));
            r += static_cast<char>(0x80u | (c & 0x3Fu));
        }
        else
        {
            r += static_cast<char>(0xF0u | (c >> 18));
            r += static_cast<char>(0x80u | ((c >> 12) & 0x3Fu));
            r += static_cast<char>(0x80u | ((c >> 6) & 0x3Fu));
            r += static_cast<char>(0x80u | (c & 0x3Fu));
        }
    }
    return r;
}

// One scalar of well-formed text at p[0..n): its byte width and value, or
// false when the sequence is cut off by n.
static bool ns_decode_utf8(const unsigned char* p, int32_t n, int32_t* width, uint32_t* cp)
{
    if (n <= 0)
        return false;
    const unsigned char c = p[0];
    const int32_t w = c < 0x80u ? 1 : c < 0xE0u ? 2 : c < 0xF0u ? 3 : 4;
    if (w > n)
        return false;
    uint32_t v = w == 1 ? c : w == 2 ? (c & 0x1Fu) : w == 3 ? (c & 0x0Fu) : (c & 0x07u);
    for (int32_t k = 1; k < w; k++)
        v = (v << 6) | (p[k] & 0x3Fu);
    *width = w;
    *cp = v;
    return true;
}

static uint32_t ns_fold_scalar(uint32_t c)
{
    return c < 0x10000u ? dn2cpp_ordinal_upper(static_cast<char16_t>(c)) : c;
}

// OrdinalIgnoreCase equality of the `length` bytes at a against the `length`
// bytes at b, scalar by scalar.
static bool ns_equal_icase8(const unsigned char* a, const unsigned char* b, int32_t length)
{
    int32_t ia = 0, ib = 0;
    while (ia < length && ib < length)
    {
        int32_t wa, wb;
        uint32_t ca, cb;
        if (!ns_decode_utf8(a + ia, length - ia, &wa, &ca) || !ns_decode_utf8(b + ib, length - ib, &wb, &cb)
            || ns_fold_scalar(ca) != ns_fold_scalar(cb))
            return false;
        ia += wa;
        ib += wb;
    }
    return ia == length && ib == length;
}

// A byte-domain view of the input for the special-value search. Offsets are
// byte offsets; `starts` yields the offset past a case-insensitive prefix, or -1.
struct NsUtf8Source
{
    const unsigned char* bytes;
    int32_t length;
};

struct NsSpecialUtf8
{
    const unsigned char* p;
    int32_t n;

    int32_t trim(int32_t i) const
    {
        int32_t width;
        uint32_t cp;
        while (ns_decode_utf8(p + i, n - i, &width, &cp) && cp < 0x10000u
               && dn2cpp_char_is_whitespace(static_cast<char16_t>(cp)))
            i += width;
        return i;
    }

    int32_t starts(int32_t i, Dn2CppString* t) const
    {
        const std::string sym = ns_encode_utf8(t);
        const int32_t length = static_cast<int32_t>(sym.size());
        if (length > n - i || !ns_equal_icase8(p + i, reinterpret_cast<const unsigned char*>(sym.data()), length))
            return -1;
        return i + length;
    }

    bool match(int32_t i, Dn2CppString* t) const
    {
        if (t == nullptr || t->length == 0)
            return false;
        const int32_t end = starts(i, t);
        return end >= 0 && trim(end) == n;
    }
};

struct NsSpecialUtf16
{
    const char16_t* p;
    int32_t n;

    int32_t trim(int32_t i) const
    {
        while (i < n && dn2cpp_char_is_whitespace(p[i]))
            i++;
        return i;
    }

    int32_t starts(int32_t i, Dn2CppString* t) const
    {
        return ns_starts_icase(p, n, i, t) ? i + (t != nullptr ? t->length : 0) : -1;
    }

    bool match(int32_t i, Dn2CppString* t) const { return ns_match_special(p, n, i, t); }
};

// double.NaN's own bit pattern: quiet, with the sign bit set.
static double ns_nan()
{
    const uint64_t bits = 0xFFF8000000000000ULL;
    double value;
    memcpy(&value, &bits, sizeof value);
    return value;
}

// The Infinity/NaN symbol fallback .NET applies when the number grammar
// fails, ignoring the styles bits entirely. White space is the full Unicode
// set, unlike the parser's ASCII one. A symbol stands alone or behind a sign;
// the negative sign introduces only NaN. PositiveSign is tried first and ends
// the search when it matches, which an empty one always does.
template <typename Input>
static bool ns_fp_special(const Input& in, const Dn2CppNumberFormatInfo* nfi, double* out)
{
    const int32_t a = in.trim(0);
    if (in.match(a, nfi->posInf)) { *out = INFINITY; return true; }
    if (in.match(a, nfi->negInf)) { *out = -INFINITY; return true; }
    if (in.match(a, nfi->nan)) { *out = ns_nan(); return true; }
    int32_t i = in.starts(a, nfi->positiveSign);
    if (i >= 0)
    {
        if (in.match(i, nfi->posInf)) { *out = INFINITY; return true; }
        if (in.match(i, nfi->nan)) { *out = ns_nan(); return true; }
        return false;
    }
    i = in.starts(a, nfi->negativeSign);
    if (i >= 0 && in.match(i, nfi->nan)) { *out = ns_nan(); return true; }
    return false;
}

static int32_t ns_hex_digit(char16_t c)
{
    if (c >= u'0' && c <= u'9') return c - u'0';
    if (c >= u'a' && c <= u'f') return c - u'a' + 10;
    if (c >= u'A' && c <= u'F') return c - u'A' + 10;
    return -1;
}

// TryParseHexFloatingPoint: [ws] [sign] 0x hex [. hex] p [sign] dec [ws] [NULs]
// with at least one hex digit and a mandatory exponent. The significand keeps
// 16 significant hex digits; a dropped integer digit scales the exponent and a
// dropped non-zero digit is sticky. The value is rounded to nearest-even ONCE,
// at the target width, so a float never takes a second rounding from double.
// The decimal separator matches exactly; the signs go through the sign matcher,
// the exponent's regardless of AllowLeadingSign.
static bool ns_fp_hex(const char16_t* p, int32_t n, int32_t styles,
                      const Dn2CppNumberFormatInfo* nfi, int32_t isSingle, bool utf8, double* out)
{
    const NsMatch match = utf8 ? NS_MATCH_UTF8 : NS_MATCH_SPACES;
    int32_t i = 0;
    if ((styles & NS_LEADWHITE) != 0)
        while (i < n && ns_is_white(p[i]))
            i++;
    bool negative = false;
    int32_t next;
    if ((styles & NS_LEADSIGN) != 0 && i < n && (next = ns_match_sign(p, n, i, nfi, match, &negative)) >= 0)
        i = next;
    if (i + 1 >= n || p[i] != u'0' || (p[i + 1] | 0x20) != u'x')
        return false;
    i += 2;

    uint64_t sig = 0;
    int32_t sigDigits = 0;
    int64_t droppedIntDigits = 0, fracDigits = 0;
    bool sticky = false, anyDigit = false;
    int32_t d;
    for (; i < n && (d = ns_hex_digit(p[i])) >= 0; i++)
    {
        anyDigit = true;
        if (sigDigits < 16)
        {
            if (sig != 0 || d != 0)
            {
                sig = (sig << 4) | (uint64_t)d;
                sigDigits++;
            }
        }
        else
        {
            droppedIntDigits++;
            sticky |= d != 0;
        }
    }
    if ((styles & NS_DECIMAL) != 0
        && (next = ns_match_str(p, n, i, nfi->numberDecimal, utf8 ? NS_MATCH_UTF8 : NS_MATCH_UNITS)) >= 0)
    {
        for (i = next; i < n && (d = ns_hex_digit(p[i])) >= 0; i++)
        {
            anyDigit = true;
            if (sigDigits < 16)
            {
                if (sig != 0 || d != 0)
                {
                    sig = (sig << 4) | (uint64_t)d;
                    sigDigits++;
                }
                fracDigits++;
            }
            else
                sticky |= d != 0;
        }
    }
    if (!anyDigit || i >= n || (p[i] | 0x20) != u'p')
        return false;
    i++;
    bool negExp = false;
    if (i < n && (next = ns_match_sign(p, n, i, nfi, match, &negExp)) >= 0)
        i = next;
    if (i >= n || !ns_is_digit(p[i]))
        return false;
    int64_t bexp = 0;
    for (; i < n && ns_is_digit(p[i]); i++)
    {
        const int32_t digit = p[i] - u'0';
        bexp = bexp <= (INT32_MAX - digit) / 10 ? bexp * 10 + digit : INT32_MAX;
    }
    if (negExp)
        bexp = -bexp;
    if ((styles & NS_TRAILWHITE) != 0)
        while (i < n && ns_is_white(p[i]))
            i++;
    while (i < n && p[i] == 0)
        i++;
    if (i != n)
        return false;

    const int32_t mantBits = isSingle ? 23 : 52;
    const int64_t bias = isSingle ? 127 : 1023;
    uint64_t bits = 0;
    if (sig != 0)
    {
        int32_t lz = 0;
        for (; (sig >> 63) == 0; sig <<= 1)
            lz++;
        // sig is now 1.xxx with the unit bit at bit 63.
        const int64_t binExp = bexp - 4 * fracDigits + 4 * droppedIntDigits - lz + 63;
        if (binExp > bias)
            bits = (uint64_t)(2 * bias + 1) << mantBits;
        else
        {
            int64_t biased = binExp + bias;
            int64_t shift = 63 - mantBits;
            if (biased <= 0)
            {
                shift += 1 - biased;
                biased = 0;
            }
            // Past 64 the value is below half the smallest subnormal: zero.
            if (shift <= 64)
            {
                const uint64_t kept = shift == 64 ? 0 : sig >> shift;
                const uint64_t half = 1ULL << (shift - 1);
                // A carry out of the fraction lands in the exponent field: the
                // next binade, or infinity, with a zero fraction.
                bits = ((uint64_t)biased << mantBits) + (kept & ((1ULL << mantBits) - 1));
                if ((sig & half) != 0 && ((sig & (half - 1)) != 0 || sticky || (kept & 1) != 0))
                    bits++;
            }
        }
    }
    if (isSingle)
    {
        const uint32_t single = (uint32_t)bits | (negative ? 0x80000000u : 0u);
        float f;
        memcpy(&f, &single, sizeof f);
        *out = (double)f;
    }
    else
    {
        bits |= negative ? 0x8000000000000000ULL : 0ULL;
        memcpy(out, &bits, sizeof bits);
    }
    return true;
}

// The float/double engine core. Status: 0 ok, 1 format. Overflow saturates to
// ±Infinity and underflow to (signed) zero, matching .NET — never an error.
// The digits are rebuilt as an invariant ASCII "0.<digits>e<scale>" string so
// strtod/strtof perform the correctly rounded binary conversion; a truncated
// non-zero tail appends a sentinel '1' to stay on the correct side of a
// halfway case, mirroring NumberBuffer.HasNonZeroTail. AllowHexSpecifier
// selects the hexadecimal grammar instead; either falls back to the symbols.
static int32_t ns_fp_core(const char16_t* p, int32_t n, int32_t styles,
                          const Dn2CppNumberFormatInfo* nfi, int32_t isSingle, const NsUtf8Source* source, double* out)
{
    dn2cpp_parse_validate_fp_styles(styles);
    nfi = dn2cpp_nfi_or_current(nfi);
    *out = 0.0;
    const bool utf8 = source != nullptr;
    const NsSpecialUtf8 bytes{source != nullptr ? source->bytes : nullptr, source != nullptr ? source->length : 0};
    const NsSpecialUtf16 units{p, n};
    if ((styles & NS_HEX) != 0)
    {
        if (ns_fp_hex(p, n, styles, nfi, isSingle, utf8, out))
            return 0;
        return (utf8 ? ns_fp_special(bytes, nfi, out) : ns_fp_special(units, nfi, out)) ? 0 : 1;
    }
    Dn2CppNumberParse num;
    if (!dn2cpp_parse_number_styles(p, n, styles, nfi, 1, utf8, &num))
        return (utf8 ? ns_fp_special(bytes, nfi, out) : ns_fp_special(units, nfi, out)) ? 0 : 1;
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
    return ns_fp_core(p, n, styles, nfi, isSingle, nullptr, out) == 0 ? 1 : 0;
}

int32_t dn2cpp_fp_tryparse_str(Dn2CppString* s, int32_t styles,
                               const Dn2CppNumberFormatInfo* nfi, int32_t isSingle, double* out)
{
    if (s == nullptr)
        return dn2cpp_fp_tryparse_chars(u"", 0, styles, nfi, isSingle, out);
    return dn2cpp_fp_tryparse_chars(s->chars, s->length, styles, nfi, isSingle, out);
}

static double ns_fp_parse(const char16_t* p, int32_t n, int32_t styles,
                          const Dn2CppNumberFormatInfo* nfi, int32_t isSingle, const NsUtf8Source* source)
{
    double v;
    if (ns_fp_core(p, n, styles, nfi, isSingle, source, &v) != 0)
        ns_throw_format_value(p, n);
    return v;
}

double dn2cpp_fp_parse_chars(const char16_t* p, int32_t n, int32_t styles,
                             const Dn2CppNumberFormatInfo* nfi, int32_t isSingle)
{
    return ns_fp_parse(p, n, styles, nfi, isSingle, nullptr);
}

double dn2cpp_fp_parse_str(Dn2CppString* s, int32_t styles,
                           const Dn2CppNumberFormatInfo* nfi, int32_t isSingle)
{
    // Unlike TryParse, Parse rejects a null string before validating the styles.
    if (s == nullptr)
        dn2cpp_throw_argument_null_param("s");
    return dn2cpp_fp_parse_chars(s->chars, s->length, styles, nfi, isSingle);
}

// ---- UTF-8 input (IUtf8SpanParsable) -----------------------------------------
//
// The ReadOnlySpan<byte> overloads widen to UTF-16 and run the SAME engine as every
// other input form rather than carrying a second scanner over bytes. The grammar is
// not pure ASCII — the decimal point, group separator and negative sign are UTF-16
// strings off the NumberFormatInfo, and real cultures supply non-ASCII ones (U+00A0 /
// U+202F, U+2212) — so a byte-wise scanner would have to transcode them anyway, in a
// second place that can drift from the first. The engine is told the text came from
// UTF-8: a byte comparison has no space-replacing rule.
//
// The decoder replaces malformed input, so every entry rejects it first: U+FFFD
// itself can be a legitimate NumberFormatInfo sign, separator or symbol, and an
// invalid byte matches none of them. The float entries' Infinity/NaN fallback
// then compares bytes, as CoreLib does.
static Dn2CppString* ns_utf8_widen(const char* p, int32_t n)
{
    return dn2cpp_string_decode_utf8(p, n < 0 ? 0 : n);
}

bool dn2cpp_parse_utf8_wellformed(const char* p, int32_t n)
{
    int32_t i = 0;
    while (i < n)
    {
        unsigned char c = static_cast<unsigned char>(p[i]);
        if (c < 0x80u) { ++i; continue; }
        int32_t width;
        unsigned char lo = 0x80u, hi = 0xBFu;
        if (c < 0xC2u) return false;
        if (c < 0xE0u) width = 2;
        else if (c == 0xE0u) { width = 3; lo = 0xA0u; }
        else if (c < 0xEDu) width = 3;
        else if (c == 0xEDu) { width = 3; hi = 0x9Fu; }
        else if (c < 0xF0u) width = 3;
        else if (c == 0xF0u) { width = 4; lo = 0x90u; }
        else if (c < 0xF4u) width = 4;
        else if (c == 0xF4u) { width = 4; hi = 0x8Fu; }
        else return false;
        if (n - i < width) return false;
        unsigned char second = static_cast<unsigned char>(p[i + 1]);
        if (second < lo || second > hi) return false;
        for (int32_t k = 2; k < width; ++k)
        {
            unsigned char continuation = static_cast<unsigned char>(p[i + k]);
            if (continuation < 0x80u || continuation > 0xBFu) return false;
        }
        i += width;
    }
    return true;
}

int32_t dn2cpp_integer_tryparse_utf8(const char* p, int32_t n, int32_t styles,
                                     const Dn2CppNumberFormatInfo* nfi, int32_t bitWidth,
                                     int32_t isSigned, int64_t* out)
{
    dn2cpp_parse_validate_integer_styles(styles);
    if (!dn2cpp_parse_utf8_wellformed(p, n)) { *out = 0; return 0; }
    Dn2CppString* s = ns_utf8_widen(p, n);
    return ns_integer_core(s->chars, s->length, styles, nfi, bitWidth, isSigned, true, out) == 0 ? 1 : 0;
}

int64_t dn2cpp_integer_parse_utf8(const char* p, int32_t n, int32_t styles,
                                  const Dn2CppNumberFormatInfo* nfi, int32_t bitWidth,
                                  int32_t isSigned)
{
    dn2cpp_parse_validate_integer_styles(styles);
    if (!dn2cpp_parse_utf8_wellformed(p, n))
        dn2cpp_throw_sr0(&dn2cpp_format_exception_type, DN2CPP_SR_FORMAT_INVALID_STRING);
    Dn2CppString* s = ns_utf8_widen(p, n);
    return ns_integer_parse(s->chars, s->length, styles, nfi, bitWidth, isSigned, true);
}

int32_t dn2cpp_fp_tryparse_utf8(const char* p, int32_t n, int32_t styles,
                                const Dn2CppNumberFormatInfo* nfi, int32_t isSingle, double* out)
{
    dn2cpp_parse_validate_fp_styles(styles);
    if (!dn2cpp_parse_utf8_wellformed(p, n)) { *out = 0.0; return 0; }
    Dn2CppString* s = ns_utf8_widen(p, n);
    const NsUtf8Source source{reinterpret_cast<const unsigned char*>(p), n};
    return ns_fp_core(s->chars, s->length, styles, nfi, isSingle, &source, out) == 0 ? 1 : 0;
}

double dn2cpp_fp_parse_utf8(const char* p, int32_t n, int32_t styles,
                            const Dn2CppNumberFormatInfo* nfi, int32_t isSingle)
{
    dn2cpp_parse_validate_fp_styles(styles);
    if (!dn2cpp_parse_utf8_wellformed(p, n))
        dn2cpp_throw_sr0(&dn2cpp_format_exception_type, DN2CPP_SR_FORMAT_INVALID_STRING);
    Dn2CppString* s = ns_utf8_widen(p, n);
    const NsUtf8Source source{reinterpret_cast<const unsigned char*>(p), n};
    return ns_fp_parse(s->chars, s->length, styles, nfi, isSingle, &source);
}

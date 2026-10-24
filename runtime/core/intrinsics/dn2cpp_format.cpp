// dn2cpp_format.cpp — the runtime's number→string formatting machinery and the
// C# interpolated-string handler.
//
// A cross-cutting concern rather than a single .NET namespace: the transpiler
// routes every formatted-string path through it — string interpolation
// (DefaultInterpolatedStringHandler) and the invariant + culture-aware numeric
// formatters (integer N/F/C/G exact path, float/double shortest-roundtrip / fixed
// / scientific) that ToString(format), TryFormat and interpolation share. Output
// matches real .NET.
#include "dn2cpp_core.h"

#include <charconv>
#include <cstdint>
#include <cstdio>
#include <cstring>
#include <cstdlib>
#include <cmath>
#include <string>

// Unit-local formatting primitives, used before their definitions below: `put`
// appends a string into a char16_t buffer at offset `o`; `format_int_exact`
// renders an integer at full precision for the N/F/C/G(no-precision) specs.
static void dn2cpp_put(char16_t* out, int& o, Dn2CppString* s);
static void dn2cpp_group_marks(int n, const int8_t* sizes, int count, unsigned char* sepAfter);
static Dn2CppString* dn2cpp_format_int_exact(unsigned long long mag, bool neg, char letter,
                                             int prec, const Dn2CppNumberFormatInfo* nfi);

// ---- interpolated string builder ----

// Grow the ISB buffer to hold at least `needed` more char16_t units.
static void dn2cpp_isb_ensure(Dn2CppISB* h, int32_t needed)
{
    if (h->length + needed <= h->capacity)
        return;
    int32_t newCap = h->capacity * 2;
    if (newCap < h->length + needed)
        newCap = h->length + needed;
    if (newCap < 64)
        newCap = 64;
    // char16_t scratch — no managed pointers, so allocate unscanned.
    char16_t* newBuf = static_cast<char16_t*>(
        dn2cpp_alloc_atomic(static_cast<size_t>(newCap) * sizeof(char16_t)));
    if (h->buf != nullptr && h->length > 0)
        std::memcpy(newBuf, h->buf, static_cast<size_t>(h->length) * sizeof(char16_t));
    h->buf = newBuf;
    h->capacity = newCap;
}

Dn2CppISB dn2cpp_isb_new(int32_t literalLength, int32_t formattedCount)
{
    int32_t cap = literalLength + formattedCount * 11; // heuristic from .NET
    if (cap < 64) cap = 64;
    Dn2CppISB h;
    h.buf = static_cast<char16_t*>(
        dn2cpp_alloc_atomic(static_cast<size_t>(cap) * sizeof(char16_t)));
    h.length = 0;
    h.capacity = cap;
    return h;
}

void dn2cpp_isb_append_literal(Dn2CppISB* h, Dn2CppString* s)
{
    if (s == nullptr || s->length == 0)
        return;
    dn2cpp_isb_ensure(h, s->length);
    std::memcpy(h->buf + h->length, s->chars,
        static_cast<size_t>(s->length) * sizeof(char16_t));
    h->length += s->length;
}

// Append `count` spaces (used for alignment padding).
static void dn2cpp_isb_append_spaces(Dn2CppISB* h, int32_t count)
{
    if (count <= 0)
        return;
    dn2cpp_isb_ensure(h, count);
    for (int32_t i = 0; i < count; i++)
        h->buf[h->length + i] = u' ';
    h->length += count;
}

void dn2cpp_isb_append_aligned(Dn2CppISB* h, Dn2CppString* s, int32_t alignment)
{
    int32_t len = s != nullptr ? s->length : 0;
    int32_t width = alignment < 0 ? -alignment : alignment;
    int32_t pad = width - len;
    if (pad < 0)
        pad = 0;
    if (alignment > 0)
        dn2cpp_isb_append_spaces(h, pad); // right-align
    dn2cpp_isb_append_literal(h, s);
    if (alignment < 0)
        dn2cpp_isb_append_spaces(h, pad); // left-align
}

// Parse a standard numeric format string (e.g. "X4", "N2") into its specifier
// letter and optional precision. NUL terminates the specifier; if it follows
// the letter directly, CLR uses precision zero rather than the usual default.
// Other non-digit trailing chars yield letter 0 (default formatting).
static void dn2cpp_parse_fmt(Dn2CppString* fmt, char* letter, int* prec)
{
    *letter = 0;
    *prec = -1;
    if (fmt == nullptr || fmt->length == 0 || fmt->chars[0] == u'\0')
        return;
    *letter = static_cast<char>(fmt->chars[0]);
    int p = 0;
    bool has = false;
    for (int32_t i = 1; i < fmt->length; i++)
    {
        char16_t c = fmt->chars[i];
        if (c == u'\0')
        {
            *prec = p;
            return;
        }
        if (c >= u'0' && c <= u'9')
        {
            p = p * 10 + static_cast<int>(c - u'0');
            has = true;
        }
        else
        {
            *letter = 0; // unsupported precision syntax -> default formatting
            return;
        }
    }
    if (has)
        *prec = p;
}

// Decimal with a minimum digit count (.NET "D"/"Dn"): zero-pad the magnitude to
// `prec` digits, keeping the sign outside the padding ("-007" for -7, "D3").
static Dn2CppString* dn2cpp_decimal_padded(unsigned long long mag, bool neg, int prec,
    Dn2CppString* negativeSign)
{
    char digits[24];
    int dl = std::snprintf(digits, sizeof(digits), "%llu", mag);
    int minDigits = prec < 0 ? 1 : prec;
    int zeros = dl < minDigits ? minDigits - dl : 0;
    int signLength = neg ? negativeSign->length : 0;
    int total = signLength + zeros + dl;
    char16_t* dst;
    Dn2CppString* s = dn2cpp_string_alloc(&dst, total);
    int i = 0;
    if (signLength != 0)
    {
        std::memcpy(dst, negativeSign->chars, static_cast<size_t>(signLength) * sizeof(char16_t));
        i += signLength;
    }
    for (int z = 0; z < zeros; z++)
        dst[i++] = u'0';
    for (int k = 0; k < dl; k++)
        dst[i++] = static_cast<char16_t>(static_cast<unsigned char>(digits[k]));
    return s;
}

// Hex of an integer's two's-complement bit pattern, sized to `byteWidth` (so a
// signed -1 renders "FF"/"FFFF"/"FFFFFFFF"/... by its declared type width, like
// .NET). No leading zeros beyond `prec`.
static Dn2CppString* dn2cpp_hex(uint64_t v, int32_t byteWidth, bool upper, int prec)
{
    uint64_t mask = byteWidth >= 8 ? ~0ULL : ((1ULL << (8 * byteWidth)) - 1);
    char buf[20];
    const char* spec = upper ? "%.*llX" : "%.*llx";
    int len = std::snprintf(buf, sizeof(buf), spec,
        prec <= 0 ? 1 : prec, static_cast<unsigned long long>(v & mask));
    return dn2cpp_string_from_ascii(buf, len);
}

// Binary (.NET 8+ "B"/"b") of an integer's two's-complement bit pattern, sized to
// `byteWidth` like hex — `((short)-1).ToString("B")` is sixteen 1s, not 64. Zero-pads
// to `prec` digits (minimum one digit); '0'/'1' carry no case, so "b" == "B".
static Dn2CppString* dn2cpp_binary(uint64_t v, int32_t byteWidth, int prec)
{
    uint64_t mask = byteWidth >= 8 ? ~0ULL : ((1ULL << (8 * byteWidth)) - 1);
    v &= mask;
    int digits = 1;
    for (int bit = 63; bit >= 1; bit--)
        if ((v >> bit) & 1)
        {
            digits = bit + 1;
            break;
        }
    int len = prec > digits ? prec : digits;
    char16_t* dst;
    Dn2CppString* s = dn2cpp_string_alloc(&dst, len);
    for (int i = 0; i < len; i++)
    {
        int bit = len - 1 - i;
        dst[i] = (bit < 64 && ((v >> bit) & 1)) ? u'1' : u'0';
    }
    return s;
}

// A format string is "standard" iff it starts with NUL or an ASCII letter
// optionally followed by digits, ending at the first NUL if present. Anything
// else non-empty (e.g. "000", "#,##0.00") is a custom numeric pattern.
static bool dn2cpp_is_standard_fmt(Dn2CppString* fmt)
{
    if (fmt == nullptr || fmt->length == 0)
        return true; // empty -> default standard formatting
    char16_t c0 = fmt->chars[0];
    if (c0 == u'\0')
        return true;
    bool alpha = (c0 >= u'A' && c0 <= u'Z') || (c0 >= u'a' && c0 <= u'z');
    if (!alpha)
        return false;
    for (int32_t i = 1; i < fmt->length; i++)
    {
        if (fmt->chars[i] == u'\0')
            return true;
        if (fmt->chars[i] < u'0' || fmt->chars[i] > u'9')
            return false;
    }
    return true;
}

int32_t dn2cpp_integral_format_uses_provider(Dn2CppString* fmt, int32_t negative)
{
    if (!dn2cpp_is_standard_fmt(fmt))
        return 1;
    char letter;
    int precision;
    dn2cpp_parse_fmt(fmt, &letter, &precision);
    char upper = static_cast<char>(letter & 0x5f);
    if (letter == 0 || upper == 'D' || (upper == 'G' && precision < 1))
        return negative != 0;
    return upper != 'X' && upper != 'B';
}

// A non-negative magnitude as custom numeric formats see it: the significant
// decimal digits d[0..n) of 0.d1d2...dn x 10^scale, no trailing zeros; n == 0
// is zero. Custom formats round these digits, never the binary value, so a
// decimal midpoint always rounds away from zero as in .NET.
struct Dn2CppCustomDigits
{
    char d[32];
    int n;
    int scale;
};

static void dn2cpp_custom_digits_trim(Dn2CppCustomDigits& x)
{
    while (x.n > 0 && x.d[x.n - 1] == '0') x.n--;
    if (x.n == 0) x.scale = 0;
}

static Dn2CppCustomDigits dn2cpp_custom_digits_u64(unsigned long long mag)
{
    Dn2CppCustomDigits x;
    x.n = 0;
    x.scale = 0;
    if (mag != 0)
    {
        x.n = std::snprintf(x.d, sizeof(x.d), "%llu", mag);
        x.scale = x.n;
    }
    dn2cpp_custom_digits_trim(x);
    return x;
}

// `precision` significant digits of a finite magnitude: .NET generates 15 for
// double and 7 for float before a custom format rounds them again.
static Dn2CppCustomDigits dn2cpp_custom_digits_real(double mag, int precision)
{
    Dn2CppCustomDigits x;
    x.n = 0;
    x.scale = 0;
    if (mag != 0.0)
    {
        char eb[40];
        int len = std::snprintf(eb, sizeof(eb), "%.*e", precision - 1, mag);
        int i = 0;
        for (; i < len && eb[i] != 'e'; i++)
            if (eb[i] != '.') x.d[x.n++] = eb[i];
        x.scale = std::atoi(eb + i + 1) + 1;
    }
    dn2cpp_custom_digits_trim(x);
    return x;
}

// Keep the first `pos` digits, rounding half away from zero on the digit string.
static void dn2cpp_custom_digits_round(Dn2CppCustomDigits& x, int pos)
{
    if (pos < 0)
        x.n = 0;
    else if (pos < x.n)
    {
        bool up = x.d[pos] >= '5';
        x.n = pos;
        if (up)
        {
            int i = x.n - 1;
            while (i >= 0 && x.d[i] == '9') i--;
            if (i < 0) { x.d[0] = '1'; x.n = 1; x.scale++; }
            else { x.d[i]++; x.n = i + 1; }
        }
    }
    dn2cpp_custom_digits_trim(x);
}

// Sections are selected before formatting, then selected again if rounding turns
// the value into zero. Empty sections fall back to the first section.
static int dn2cpp_custom_section(const char16_t* fmt, int length, int section)
{
    if (section == 0)
        return 0;
    for (int i = 0; i < length; i++)
    {
        char16_t c = fmt[i];
        if (c == u'\\') { i++; continue; }
        if (c == u'\'' || c == u'"')
        {
            while (++i < length && fmt[i] != c) {}
            continue;
        }
        if (c == u';' && --section == 0)
            return i + 1 < length && fmt[i + 1] != u';' ? i + 1 : 0;
    }
    return 0;
}

// Render custom numeric digits in placeholder order. This preserves literals
// between integer digits and lets the first decimal point own the separator.
static Dn2CppString* dn2cpp_format_section(const char16_t* fmt, int length, Dn2CppCustomDigits mag,
                                          bool negative, bool floating, const Dn2CppNumberFormatInfo* nfi)
{
    int section = dn2cpp_custom_section(fmt, length, mag.n == 0 ? 2 : negative ? 1 : 0);
    int digitCount, decimalPos, firstDigit, lastDigit;
    bool scientific, group;
    while (true)
    {
        digitCount = 0;
        decimalPos = -1;
        firstDigit = INT32_MAX;
        lastDigit = 0;
        scientific = false;
        group = false;
        int commaPos = -1, commaCount = 0, scaleAdjust = 0;
        for (int i = section; i < length && fmt[i] != u';'; i++)
        {
            char16_t c = fmt[i];
            if (c == u'\\') { i++; continue; }
            if (c == u'\'' || c == u'"')
            {
                while (++i < length && fmt[i] != c) {}
                continue;
            }
            if (c == u'#' || c == u'0')
            {
                if (c == u'0')
                {
                    if (firstDigit == INT32_MAX) firstDigit = digitCount;
                    lastDigit = digitCount + 1;
                }
                digitCount++;
            }
            else if (c == u'.')
            {
                if (decimalPos < 0) decimalPos = digitCount;
            }
            else if (c == u',' && digitCount > 0 && decimalPos < 0)
            {
                if (commaPos == digitCount) commaCount++;
                else
                {
                    if (commaPos >= 0) group = true;
                    commaPos = digitCount;
                    commaCount = 1;
                }
            }
            else if (c == u'%') scaleAdjust += 2;
            else if (c == u'‰') scaleAdjust += 3;
            else if (c == u'E' || c == u'e')
            {
                int j = i + 1;
                if (j < length && (fmt[j] == u'+' || fmt[j] == u'-')) j++;
                if (j < length && fmt[j] == u'0')
                {
                    while (j < length && fmt[j] == u'0') j++;
                    scientific = true;
                    i = j - 1;
                }
            }
        }
        if (decimalPos < 0) decimalPos = digitCount;
        if (commaPos == decimalPos) scaleAdjust -= 3 * commaCount;
        else if (commaPos >= 0) group = true;
        if (mag.n > 0)
        {
            mag.scale += scaleAdjust;
            dn2cpp_custom_digits_round(mag, scientific ? digitCount : mag.scale + digitCount - decimalPos);
            if (mag.n == 0)
            {
                if (!floating) negative = false;
                int zeroSection = dn2cpp_custom_section(fmt, length, 2);
                if (zeroSection != section) { section = zeroSection; continue; }
            }
        }
        else
        {
            if (!floating) negative = false;
            mag.scale = 0;
        }
        break;
    }

    firstDigit = firstDigit < decimalPos ? decimalPos - firstDigit : 0;
    lastDigit = lastDigit > decimalPos ? decimalPos - lastDigit : 0;
    int digPos = scientific ? decimalPos : (mag.scale > decimalPos ? mag.scale : decimalPos);
    int adjust = scientific ? 0 : mag.scale - decimalPos;
    int totalDigits = digPos + (adjust < 0 ? adjust : 0);
    int groupedDigits = firstDigit > totalDigits ? firstDigit : totalDigits;
    std::string sepAfter(static_cast<size_t>(groupedDigits), '\0');
    if (group && groupedDigits > 0 && nfi->numberGroup != nullptr && nfi->numberGroup->length > 0)
        dn2cpp_group_marks(groupedDigits, nfi->groupSizes, nfi->groupSizeCount,
            reinterpret_cast<unsigned char*>(&sepAfter[0]));

    std::u16string out;
    auto put = [&out](Dn2CppString* value) {
        if (value != nullptr) out.append(value->chars, static_cast<size_t>(value->length));
    };
    auto putDigit = [&](char c) {
        out.push_back(static_cast<char16_t>(c));
        int index = groupedDigits - digPos;
        if (group && digPos > 1 && index >= 0 && index < groupedDigits && sepAfter[index])
            put(nfi->numberGroup);
    };
    if (negative && section == 0 && mag.scale != 0) put(nfi->negativeSign);
    int curIndex = 0;
    bool decimalWritten = false;
    for (int i = section; i < length && fmt[i] != u';'; i++)
    {
        char16_t c = fmt[i];
        if (adjust > 0 && (c == u'#' || c == u'0' || c == u'.'))
        {
            while (adjust > 0)
            {
                putDigit(curIndex < mag.n ? mag.d[curIndex++] : '0');
                digPos--;
                adjust--;
            }
        }
        if (c == u'#' || c == u'0')
        {
            char digit = 0;
            if (adjust < 0) { adjust++; if (digPos <= firstDigit) digit = '0'; }
            else if (curIndex < mag.n) digit = mag.d[curIndex++];
            else if (digPos > lastDigit) digit = '0';
            if (digit != 0) putDigit(digit);
            digPos--;
            continue;
        }
        if (c == u'.')
        {
            if (digPos == 0 && !decimalWritten &&
                (lastDigit < 0 || (decimalPos < digitCount && curIndex < mag.n)))
            {
                put(nfi->numberDecimal);
                decimalWritten = true;
            }
            continue;
        }
        if (c == u',') continue;
        if (c == u'%') { put(nfi->percentSymbol); continue; }
        if (c == u'‰') { put(nfi->perMilleSymbol); continue; }
        if (c == u'\\') { if (i + 1 < length) out.push_back(fmt[++i]); continue; }
        if (c == u'\'' || c == u'"')
        {
            while (i + 1 < length && fmt[i + 1] != c) out.push_back(fmt[++i]);
            if (i + 1 < length) i++;
            continue;
        }
        if (c == u'E' || c == u'e')
        {
            int j = i + 1;
            bool positiveSign = false;
            if (j < length && (fmt[j] == u'+' || fmt[j] == u'-'))
            {
                positiveSign = fmt[j] == u'+';
                j++;
            }
            if (scientific && j < length && fmt[j] == u'0')
            {
                int minDigits = 0;
                while (j < length && fmt[j] == u'0') { minDigits++; j++; }
                if (minDigits > 10) minDigits = 10;
                out.push_back(c);
                int exponent = mag.n == 0 ? 0 : mag.scale - decimalPos;
                if (exponent < 0) put(nfi->negativeSign);
                else if (positiveSign) put(nfi->positiveSign);
                char eb[16];
                int el = std::snprintf(eb, sizeof(eb), "%d", exponent < 0 ? -exponent : exponent);
                for (int z = el; z < minDigits; z++) out.push_back(u'0');
                for (int z = 0; z < el; z++) out.push_back(static_cast<char16_t>(eb[z]));
                i = j - 1;
                scientific = false;
                continue;
            }
            if (!scientific)
            {
                out.push_back(c);
                if (i + 1 < length && (fmt[i + 1] == u'+' || fmt[i + 1] == u'-'))
                    out.push_back(fmt[++i]);
                while (i + 1 < length && fmt[i + 1] == u'0') out.push_back(fmt[++i]);
                continue;
            }
        }
        out.push_back(c);
    }
    if (negative && section == 0 && mag.scale == 0 && !out.empty() && nfi->negativeSign != nullptr)
        out.insert(0, nfi->negativeSign->chars, static_cast<size_t>(nfi->negativeSign->length));
    if (out.size() > INT32_MAX) dn2cpp_throw_out_of_memory();
    return dn2cpp_string_from_chars(out.data(), static_cast<int32_t>(out.size()));
}

static Dn2CppString* dn2cpp_format_custom_c(const Dn2CppCustomDigits& mag, bool negative, bool floating,
                                           Dn2CppString* fmt, const Dn2CppNumberFormatInfo* nfi)
{
    nfi = dn2cpp_nfi_or_current(nfi);
    int length = 0;
    while (length < fmt->length && fmt->chars[length] != u'\0') length++;
    return dn2cpp_format_section(fmt->chars, length, mag, negative, floating, nfi);
}

static Dn2CppString* dn2cpp_format_custom_int(bool negative, unsigned long long mag, Dn2CppString* fmt,
                                              const Dn2CppNumberFormatInfo* nfi)
{
    return dn2cpp_format_custom_c(dn2cpp_custom_digits_u64(mag), negative, false, fmt, nfi);
}

Dn2CppString* dn2cpp_format_decimal_custom_c(const char* digits, int32_t count, int32_t scale,
                                             int32_t negative, Dn2CppString* fmt,
                                             const Dn2CppNumberFormatInfo* nfi)
{
    if (dn2cpp_is_standard_fmt(fmt))
        return nullptr;
    Dn2CppCustomDigits mag;
    std::memcpy(mag.d, digits, static_cast<size_t>(count));
    mag.n = count;
    mag.scale = count - scale;
    dn2cpp_custom_digits_trim(mag);
    return dn2cpp_format_custom_c(mag, negative != 0, false, fmt, nfi);
}

static Dn2CppString* dn2cpp_format_custom_real(double v, int precision, Dn2CppString* fmt,
                                               const Dn2CppNumberFormatInfo* nfi)
{
    nfi = dn2cpp_nfi_or_current(nfi);
    if (std::isnan(v)) return nfi->nan;
    if (std::isinf(v)) return v < 0 ? nfi->negInf : nfi->posInf;
    return dn2cpp_format_custom_c(dn2cpp_custom_digits_real(std::fabs(v), precision), std::signbit(v),
                                  true, fmt, nfi);
}

// Signed integral hole (sbyte/short/int/long/nint). `byteWidth` (1/2/4/8) is the
// declared type width, used by hex to size the two's-complement pattern.
Dn2CppString* dn2cpp_format_int_c(int64_t v, int32_t byteWidth, Dn2CppString* fmt, const Dn2CppNumberFormatInfo* nfi)
{
    nfi = dn2cpp_integral_format_uses_provider(fmt, v < 0)
        ? dn2cpp_nfi_or_current(nfi) : dn2cpp_nfi_invariant();
    bool neg = v < 0;
    // Modular negation gives the right magnitude even for INT64_MIN.
    unsigned long long mag = neg
        ? (0ULL - static_cast<unsigned long long>(v))
        : static_cast<unsigned long long>(v);
    if (!dn2cpp_is_standard_fmt(fmt))
        return dn2cpp_format_custom_int(neg, mag, fmt, nfi);
    char letter;
    int prec;
    dn2cpp_parse_fmt(fmt, &letter, &prec);
    if (letter == 0 || ((letter == 'g' || letter == 'G') && prec < 1))
        return dn2cpp_decimal_padded(mag, neg, -1, nfi->negativeSign);
    switch (letter)
    {
        case 'x':
        case 'X':
            return dn2cpp_hex(static_cast<uint64_t>(v), byteWidth, letter == 'X', prec);
        case 'b':
        case 'B':
            return dn2cpp_binary(static_cast<uint64_t>(v), byteWidth, prec);
        case 'd':
        case 'D':
            return dn2cpp_decimal_padded(mag, neg, prec, nfi->negativeSign);
        case 'e':
        case 'E':
        case 'p':
        case 'P':
        case 'c':
        case 'C':
        case 'f':
        case 'F':
        case 'n':
        case 'N':
        case 'g':
        case 'G':
            // N/F/C/G(no precision) format from the exact integer (full precision
            // above 2^53); E/P/G-with-precision round to few significant digits, so
            // the double path is exact enough there.
            if (Dn2CppString* r = dn2cpp_format_int_exact(mag, neg, letter, prec, nfi))
                return r;
            return dn2cpp_format_r8_c(static_cast<double>(v), fmt, nfi);
        default:
            return dn2cpp_long_to_string(v);
    }
}

Dn2CppString* dn2cpp_format_int(int64_t v, int32_t byteWidth, Dn2CppString* fmt)
{
    return dn2cpp_format_int_c(v, byteWidth, fmt, nullptr);
}

// Unsigned integral hole (byte/ushort/uint/ulong/nuint).
Dn2CppString* dn2cpp_format_uint_c(uint64_t v, int32_t byteWidth, Dn2CppString* fmt, const Dn2CppNumberFormatInfo* nfi)
{
    nfi = dn2cpp_integral_format_uses_provider(fmt, false)
        ? dn2cpp_nfi_or_current(nfi) : dn2cpp_nfi_invariant();
    if (!dn2cpp_is_standard_fmt(fmt))
        return dn2cpp_format_custom_int(false, v, fmt, nfi);
    char letter;
    int prec;
    dn2cpp_parse_fmt(fmt, &letter, &prec);
    if (letter == 0 || ((letter == 'g' || letter == 'G') && prec < 1))
        return dn2cpp_decimal_padded(v, false, -1, nullptr);
    switch (letter)
    {
        case 'x':
        case 'X':
            return dn2cpp_hex(v, byteWidth, letter == 'X', prec);
        case 'b':
        case 'B':
            return dn2cpp_binary(v, byteWidth, prec);
        case 'd':
        case 'D':
            return dn2cpp_decimal_padded(v, false, prec, nullptr);
        case 'e':
        case 'E':
        case 'p':
        case 'P':
        case 'c':
        case 'C':
        case 'f':
        case 'F':
        case 'n':
        case 'N':
        case 'g':
        case 'G':
            if (Dn2CppString* r = dn2cpp_format_int_exact(v, false, letter, prec, nfi))
                return r;
            return dn2cpp_format_r8_c(static_cast<double>(v), fmt, nfi);
        default:
        {
            char buf[24];
            int len = std::snprintf(buf, sizeof(buf), "%llu",
                static_cast<unsigned long long>(v));
            return dn2cpp_string_from_ascii(buf, len);
        }
    }
}

Dn2CppString* dn2cpp_format_uint(uint64_t v, int32_t byteWidth, Dn2CppString* fmt)
{
    return dn2cpp_format_uint_c(v, byteWidth, fmt, nullptr);
}

// Copy an already-formatted Dn2CppString into a Span<char>'s buffer
// with .NET TryFormat semantics — fits ⇒ write code units + length, return true;
// doesn't fit ⇒ leave the buffer untouched + charsWritten=0, return false (probed:
// a too-short dest is never partially written).
static bool dn2cpp_try_emit(Dn2CppString* s, char16_t* dest, int32_t destLen, int32_t* charsWritten)
{
    int32_t len = s != nullptr ? s->length : 0;
    if (len > destLen)
    {
        if (charsWritten != nullptr)
            *charsWritten = 0;
        return false;
    }
    if (len > 0)
        std::memcpy(dest, s->chars, static_cast<size_t>(len) * sizeof(char16_t));
    if (charsWritten != nullptr)
        *charsWritten = len;
    return true;
}

bool dn2cpp_try_format_int_c(int64_t v, int32_t byteWidth, Dn2CppString* fmt,
    char16_t* dest, int32_t destLen, int32_t* charsWritten, const Dn2CppNumberFormatInfo* nfi)
{
    return dn2cpp_try_emit(dn2cpp_format_int_c(v, byteWidth, fmt, nfi), dest, destLen, charsWritten);
}

bool dn2cpp_try_format_uint_c(uint64_t v, int32_t byteWidth, Dn2CppString* fmt,
    char16_t* dest, int32_t destLen, int32_t* charsWritten, const Dn2CppNumberFormatInfo* nfi)
{
    return dn2cpp_try_emit(dn2cpp_format_uint_c(v, byteWidth, fmt, nfi), dest, destLen, charsWritten);
}

// UTF-8 twin of dn2cpp_try_emit for IUtf8SpanFormattable.TryFormat's Span<byte>
// destination: fits ⇒ write the UTF-8 encoding + byte count, return true; doesn't
// fit ⇒ leave the buffer untouched + bytesWritten=0, return false. Symbols beyond
// ASCII (culture separators such as fr-FR's U+202F group separator, the invariant
// currency sign U+00A4) encode multi-byte through the shared transcode core, never
// truncated to a single byte.
static bool dn2cpp_try_emit_utf8(Dn2CppString* s, char* dest, int32_t destLen, int32_t* bytesWritten)
{
    const char16_t* src = s != nullptr ? s->chars : nullptr;
    int32_t len = s != nullptr ? s->length : 0;
    int32_t bytes = dn2cpp_utf16_to_utf8(src, len, nullptr, 0);
    if (bytes > destLen)
    {
        if (bytesWritten != nullptr)
            *bytesWritten = 0;
        return false;
    }
    dn2cpp_utf16_to_utf8(src, len, dest, destLen);
    if (bytesWritten != nullptr)
        *bytesWritten = bytes;
    return true;
}

bool dn2cpp_try_format_int_utf8_c(int64_t v, int32_t byteWidth, Dn2CppString* fmt,
    char* dest, int32_t destLen, int32_t* bytesWritten, const Dn2CppNumberFormatInfo* nfi)
{
    return dn2cpp_try_emit_utf8(dn2cpp_format_int_c(v, byteWidth, fmt, nfi), dest, destLen, bytesWritten);
}

bool dn2cpp_try_format_uint_utf8_c(uint64_t v, int32_t byteWidth, Dn2CppString* fmt,
    char* dest, int32_t destLen, int32_t* bytesWritten, const Dn2CppNumberFormatInfo* nfi)
{
    return dn2cpp_try_emit_utf8(dn2cpp_format_uint_c(v, byteWidth, fmt, nfi), dest, destLen, bytesWritten);
}

// Enum numeric fallback. Hex zero-pads to the underlying type's width (2 hex
// digits per byte) — `((Color)2).ToString("X")` is "00000002" for an int-backed
// enum, unlike a plain int "X" which has no leading zeros. Decimal/default render
// the value by the underlying sign (an unsigned-backed enum is masked to width so
// the int32-modeled value isn't sign-extended into a negative).
Dn2CppString* dn2cpp_format_enum(int64_t v, int32_t byteWidth, int32_t isSigned, Dn2CppString* fmt)
{
    char letter;
    int prec;
    dn2cpp_parse_fmt(fmt, &letter, &prec);
    if (letter == 'x' || letter == 'X')
    {
        if (prec < 0)
            prec = byteWidth * 2; // zero-pad to the underlying width
        return dn2cpp_hex(static_cast<uint64_t>(v), byteWidth, letter == 'X', prec);
    }
    // Enums have no "B" specifier, so fall back to decimal rather than inheriting the
    // integer formatters' binary case.
    if (letter == 'b' || letter == 'B')
        fmt = nullptr;
    if (isSigned)
        return dn2cpp_format_int(v, byteWidth, fmt);
    uint64_t mask = byteWidth >= 8 ? ~0ULL : ((1ULL << (8 * byteWidth)) - 1);
    return dn2cpp_format_uint(static_cast<uint64_t>(v) & mask, byteWidth, fmt);
}

// Append a string's code units into a char16_t cursor.
static void dn2cpp_put(char16_t* out, int& o, Dn2CppString* s)
{
    if (s != nullptr)
        for (int i = 0; i < s->length; i++) out[o++] = s->chars[i];
}

// Mark the digit positions a group separator FOLLOWS, for an integer part of
// `n` digits under .NET's NumberGroupSizes rule. `sepAfter[i] = 1` means "a
// separator goes between digit i and digit i+1", indexing from the LEFT.
//
// The sizes are consumed RIGHT to LEFT — sizes[0] digits, then sizes[1], with the
// LAST element repeating for everything further left — and a size of 0 stops
// grouping rather than repeating (.NET's documented trailing zero: [3,0] renders
// 123456789012345 as 123456789012,345). An empty array groups nothing. Nothing is
// emitted left of the leading digit, so a 1-digit group never opens the number.
static void dn2cpp_group_marks(int n, const int8_t* sizes, int count, unsigned char* sepAfter)
{
    for (int i = 0; i < n; i++) sepAfter[i] = 0;
    if (sizes == nullptr || count <= 0)
        return;
    int idx = 0;
    int size = sizes[0];
    if (size < 0)
        dn2cpp_throw_argument_out_of_range_value(DN2CPP_SR_MUST_BE_NON_NEGATIVE, "", size);
    int run = 0;
    for (int i = n - 1; i >= 0; i--)
    {
        if (size <= 0)
            return;
        run++;
        if (run == size && i != 0)
        {
            sepAfter[i - 1] = 1;
            if (idx < count - 1)
                size = sizes[++idx];
            run = 0;
        }
    }
}

// The number of code units `n` grouped digits plus their separators occupy.
static int dn2cpp_group_width(int n, const unsigned char* sepAfter, Dn2CppString* grp)
{
    int sepLen = grp != nullptr ? grp->length : 0;
    int w = n;
    for (int i = 0; i < n; i++) if (sepAfter[i]) w += sepLen;
    return w;
}

// The widest integer part either renderer below can produce: %.0f of a finite
// double is at most 309 digits, and the exact-integer path at most 20.
#define DN2CPP_MAX_INT_DIGITS 320

// Render a non-negative magnitude `mag` with `prec` fraction digits, inserting
// the culture group separator at the culture's group sizes and using `dec` for
// the decimal point. Sign is added by the caller. (.NET "N"/"C"/"P" integer
// grouping.) The result is sized exactly and written straight into the string,
// so a long custom NumberGroupSeparator has no fixed budget to overrun.
static Dn2CppString* dn2cpp_localize_fixed_mag(double mag, int prec, Dn2CppString* grp,
                                               Dn2CppString* dec, const int8_t* sizes,
                                               int sizeCount, bool doGroup)
{
    char buf[DN2CPP_MAX_INT_DIGITS + 192];
    int len = std::snprintf(buf, sizeof(buf), "%.*f", prec, mag);
    if (len < 0) len = 0;
    if (len > static_cast<int>(sizeof(buf)) - 1) len = static_cast<int>(sizeof(buf)) - 1;
    int dot = len;
    for (int i = 0; i < len; i++) if (buf[i] == '.') { dot = i; break; }
    unsigned char sepAfter[DN2CPP_MAX_INT_DIGITS + 192];
    // .NET skips grouping when the rounded number has no integer digits.
    if (doGroup && !(dot == 1 && buf[0] == '0'))
        dn2cpp_group_marks(dot, sizes, sizeCount, sepAfter);
    else
        for (int i = 0; i < dot; i++) sepAfter[i] = 0;
    int total = dn2cpp_group_width(dot, sepAfter, grp);
    if (dot < len)
        total += (dec != nullptr ? dec->length : 0) + (len - dot - 1);
    char16_t* dst;
    Dn2CppString* s = dn2cpp_string_alloc(&dst, total);
    int o = 0;
    for (int i = 0; i < dot; i++)
    {
        dst[o++] = static_cast<char16_t>(buf[i]);
        if (sepAfter[i])
            dn2cpp_put(dst, o, grp);
    }
    if (dot < len)
    {
        dn2cpp_put(dst, o, dec);
        for (int i = dot + 1; i < len; i++) dst[o++] = static_cast<char16_t>(buf[i]);
    }
    return s;
}

// Like dn2cpp_localize_fixed_mag but from an *exact* non-negative integer
// magnitude (so values beyond 2^53 keep full precision, unlike the double path):
// the integer's decimal digits, grouped, then `fracDigits` trailing zeros after
// the decimal point. Backs integer N/F/C.
static Dn2CppString* dn2cpp_localize_fixed_int(unsigned long long mag, int fracDigits,
                                               Dn2CppString* grp, Dn2CppString* dec,
                                               const int8_t* sizes, int sizeCount, bool doGroup)
{
    char buf[24];
    int len = std::snprintf(buf, sizeof(buf), "%llu", mag);
    unsigned char sepAfter[24];
    if (doGroup && mag != 0)
        dn2cpp_group_marks(len, sizes, sizeCount, sepAfter);
    else
        for (int i = 0; i < len; i++) sepAfter[i] = 0;
    int total = dn2cpp_group_width(len, sepAfter, grp);
    if (fracDigits > 0)
        total += (dec != nullptr ? dec->length : 0) + fracDigits;
    char16_t* dst;
    Dn2CppString* s = dn2cpp_string_alloc(&dst, total);
    int o = 0;
    for (int i = 0; i < len; i++)
    {
        dst[o++] = static_cast<char16_t>(buf[i]);
        if (sepAfter[i])
            dn2cpp_put(dst, o, grp);
    }
    if (fracDigits > 0)
    {
        dn2cpp_put(dst, o, dec);
        for (int i = 0; i < fracDigits; i++) dst[o++] = u'0';
    }
    return s;
}

// Lay a non-negative magnitude string `num` into one of .NET's currency/percent
// pattern templates (e.g. "($n)", "n %"): `symCh` ('$' for currency, '%' for
// percent) -> `sym`; 'n' -> `num`; '-' -> `neg`; everything else (space, parens)
// is a literal. Pattern expansion allocates its exact size so a large fixed
// number or a multi-character symbol cannot overrun a scratch buffer.
static Dn2CppString* dn2cpp_lay_pattern(const char16_t* tpl, char16_t symCh, Dn2CppString* sym,
                                        Dn2CppString* num, Dn2CppString* neg)
{
    int64_t total = 0;
    for (int i = 0; tpl[i] != u'\0'; i++)
    {
        char16_t c = tpl[i];
        if (c == symCh) total += sym != nullptr ? sym->length : 0;
        else if (c == u'n') total += num != nullptr ? num->length : 0;
        else if (c == u'-') total += neg != nullptr ? neg->length : 0;
        else total++;
    }
    if (total > INT32_MAX)
        dn2cpp_throw_out_of_memory();
    char16_t* dst;
    Dn2CppString* s = dn2cpp_string_alloc(&dst, static_cast<int32_t>(total));
    int o = 0;
    for (int i = 0; tpl[i] != u'\0'; i++)
    {
        char16_t c = tpl[i];
        if (c == symCh) dn2cpp_put(dst, o, sym);
        else if (c == u'n') dn2cpp_put(dst, o, num);
        else if (c == u'-') dn2cpp_put(dst, o, neg);
        else dst[o++] = c;
    }
    return s;
}

// Normalize a printf exponential ("1.05E+03") to .NET's >=3-digit exponent
// ("1.05E+003") in-place into `out` (ASCII); returns the new length. printf emits
// a min-2-digit exponent, .NET a min-3-digit one.
static int dn2cpp_pad_exponent_buf(const char* buf, int len, char* out)
{
    int e = 0;
    while (e < len && buf[e] != 'e' && buf[e] != 'E') e++;
    if (e >= len) { std::memcpy(out, buf, len); return len; } // no exponent marker
    int sign = e + 1;             // '+'/'-' position
    int firstDigit = sign + 1;
    int digits = len - firstDigit;
    int zeros = digits < 3 ? 3 - digits : 0;
    int o = 0;
    for (int i = 0; i <= sign; i++) out[o++] = buf[i];
    for (int z = 0; z < zeros; z++) out[o++] = '0';
    for (int i = firstDigit; i < len; i++) out[o++] = buf[i];
    return o;
}

static const char16_t* const s_numberNeg[5] = { u"(n)", u"-n", u"- n", u"n-", u"n -" };
static const char16_t* const s_currencyPos[4] = { u"$n", u"n$", u"$ n", u"n $" };
static const char16_t* const s_currencyNeg[16] = {
    u"($n)", u"-$n", u"$-n", u"$n-", u"(n$)", u"-n$", u"n-$", u"n$-",
    u"-n $", u"-$ n", u"n $-", u"$ n-", u"$ -n", u"n- $", u"($ n)", u"(n $)" };
static const char16_t* const s_percentPos[4] = { u"n %", u"n%", u"%n", u"% n" };
static const char16_t* const s_percentNeg[12] = {
    u"-n %", u"-n%", u"-%n", u"%-n", u"%n-", u"n-%", u"n%-", u"-% n",
    u"n %-", u"% n-", u"% -n", u"n- %" };

static Dn2CppString* dn2cpp_format_int_exact(unsigned long long mag, bool neg, char letter,
                                             int prec, const Dn2CppNumberFormatInfo* nfi)
{
    switch (letter)
    {
        case 'f': case 'F':
        {
            Dn2CppString* num = dn2cpp_localize_fixed_int(mag, prec < 0 ? nfi->numberDigits : prec,
                nfi->numberGroup, nfi->numberDecimal, nfi->groupSizes, nfi->groupSizeCount, false);
            return neg ? dn2cpp_string_concat2(nfi->negativeSign, num) : num;
        }
        case 'n': case 'N':
        {
            Dn2CppString* num = dn2cpp_localize_fixed_int(mag, prec < 0 ? nfi->numberDigits : prec,
                nfi->numberGroup, nfi->numberDecimal, nfi->groupSizes, nfi->groupSizeCount, true);
            return neg ? dn2cpp_lay_pattern(s_numberNeg[nfi->numberNegPattern % 5],
                0, nullptr, num, nfi->negativeSign) : num;
        }
        case 'c': case 'C':
        {
            int cd = prec < 0 ? nfi->currencyDigits : prec;
            Dn2CppString* num = dn2cpp_localize_fixed_int(mag, cd,
                nfi->currencyGroup, nfi->currencyDecimal, nfi->currencyGroupSizes, nfi->currencyGroupSizeCount, true);
            const char16_t* tpl = neg ? s_currencyNeg[nfi->currencyNegPattern % 16]
                                      : s_currencyPos[nfi->currencyPosPattern % 4];
            return dn2cpp_lay_pattern(tpl, u'$', nfi->currencySymbol, num, nfi->negativeSign);
        }
        case 'g': case 'G':
            if (prec < 0) // G without precision is the exact decimal (no rounding)
            {
                Dn2CppString* num = dn2cpp_localize_fixed_int(mag, 0,
                    nfi->numberGroup, nfi->numberDecimal, nfi->groupSizes, nfi->groupSizeCount, false);
                return neg ? dn2cpp_string_concat2(nfi->negativeSign, num) : num;
            }
            return nullptr;
        default:
            return nullptr; // E/P (and G-with-precision) round — the double path suffices
    }
}

static Dn2CppString* dn2cpp_localize_decimal_fixed(const Dn2CppCustomDigits& mag, int fracDigits,
    Dn2CppString* grp, Dn2CppString* dec, const int8_t* sizes, int sizeCount, bool doGroup)
{
    int intDigits = mag.scale > 0 ? mag.scale : 1;
    unsigned char sepAfter[40];
    if (doGroup && mag.n != 0 && mag.scale > 0)
        dn2cpp_group_marks(intDigits, sizes, sizeCount, sepAfter);
    else
        for (int i = 0; i < intDigits; i++) sepAfter[i] = 0;
    int total = dn2cpp_group_width(intDigits, sepAfter, grp);
    if (fracDigits > 0)
        total += (dec != nullptr ? dec->length : 0) + fracDigits;
    char16_t* dst;
    Dn2CppString* result = dn2cpp_string_alloc(&dst, total);
    int out = 0;
    for (int i = 0; i < intDigits; i++)
    {
        dst[out++] = static_cast<char16_t>(mag.scale > 0 && i < mag.n ? mag.d[i] : '0');
        if (sepAfter[i]) dn2cpp_put(dst, out, grp);
    }
    if (fracDigits > 0)
    {
        dn2cpp_put(dst, out, dec);
        for (int i = 0; i < fracDigits; i++)
        {
            int digit = mag.scale + i;
            dst[out++] = static_cast<char16_t>(digit >= 0 && digit < mag.n ? mag.d[digit] : '0');
        }
    }
    return result;
}

Dn2CppString* dn2cpp_format_decimal_standard_c(const char* digits, int32_t count, int32_t scale,
    int32_t negative, Dn2CppString* fmt, const Dn2CppNumberFormatInfo* nfi)
{
    char letter;
    int precision;
    dn2cpp_parse_fmt(fmt, &letter, &precision);
    bool fixed = letter == 'F' || letter == 'f' || letter == 'N' || letter == 'n'
        || letter == 'C' || letter == 'c' || letter == 'P' || letter == 'p';
    bool general = (letter == 'G' || letter == 'g') && precision >= 0;
    if (!fixed && !general)
        return nullptr;
    Dn2CppCustomDigits mag;
    std::memcpy(mag.d, digits, static_cast<size_t>(count));
    mag.n = count;
    mag.scale = count - scale;
    dn2cpp_custom_digits_trim(mag);
    if (general)
    {
        if (precision == 0) precision = 29;
        dn2cpp_custom_digits_round(mag, precision);
        if (mag.n == 0)
            return dn2cpp_string_from_ascii("0", 1);
        int exponent = mag.scale - 1;
        if (exponent >= -4 && exponent < precision)
        {
            int fracDigits = mag.n > mag.scale ? mag.n - mag.scale : 0;
            Dn2CppString* result = dn2cpp_localize_decimal_fixed(mag, fracDigits,
                nfi->numberGroup, nfi->numberDecimal, nfi->groupSizes, nfi->groupSizeCount, false);
            return negative ? dn2cpp_string_concat2(nfi->negativeSign, result) : result;
        }
        char ascii[40];
        int out = 0;
        if (negative) ascii[out++] = '-';
        ascii[out++] = mag.d[0];
        if (mag.n > 1)
        {
            ascii[out++] = '.';
            for (int i = 1; i < mag.n; i++) ascii[out++] = mag.d[i];
        }
        ascii[out++] = letter == 'G' ? 'E' : 'e';
        ascii[out++] = exponent < 0 ? '-' : '+';
        int magnitude = exponent < 0 ? -exponent : exponent;
        if (magnitude < 10) ascii[out++] = '0';
        out += std::snprintf(ascii + out, sizeof(ascii) - out, "%d", magnitude);
        return dn2cpp_localize_ascii(ascii, out, nfi);
    }
    bool percent = letter == 'P' || letter == 'p';
    bool currency = letter == 'C' || letter == 'c';
    bool grouped = letter != 'F' && letter != 'f';
    if (precision < 0)
        precision = currency ? nfi->currencyDigits : percent ? nfi->percentDigits : nfi->numberDigits;
    if (percent && mag.n != 0) mag.scale += 2;
    dn2cpp_custom_digits_round(mag, mag.scale + precision);
    bool neg = negative != 0 && mag.n != 0;
    Dn2CppString* result = dn2cpp_localize_decimal_fixed(mag, precision,
        currency ? nfi->currencyGroup : percent ? nfi->percentGroup : nfi->numberGroup,
        currency ? nfi->currencyDecimal : percent ? nfi->percentDecimal : nfi->numberDecimal,
        currency ? nfi->currencyGroupSizes : percent ? nfi->percentGroupSizes : nfi->groupSizes,
        currency ? nfi->currencyGroupSizeCount : percent ? nfi->percentGroupSizeCount : nfi->groupSizeCount,
        grouped);
    if (currency)
    {
        const char16_t* pattern = neg ? s_currencyNeg[nfi->currencyNegPattern % 16]
                                      : s_currencyPos[nfi->currencyPosPattern % 4];
        return dn2cpp_lay_pattern(pattern, u'$', nfi->currencySymbol, result, nfi->negativeSign);
    }
    if (percent)
    {
        const char16_t* pattern = neg ? s_percentNeg[nfi->percentNegPattern % 12]
                                      : s_percentPos[nfi->percentPosPattern % 4];
        return dn2cpp_lay_pattern(pattern, u'%', nfi->percentSymbol, result, nfi->negativeSign);
    }
    if (letter == 'N' || letter == 'n')
        return neg ? dn2cpp_lay_pattern(s_numberNeg[nfi->numberNegPattern % 5],
            0, nullptr, result, nfi->negativeSign) : result;
    return neg ? dn2cpp_string_concat2(nfi->negativeSign, result) : result;
}

// General format ("G"/"g"). Renders the value with `prec` significant digits (or,
// when `prec < 0`, the shortest representation that round-trips), choosing fixed or
// scientific notation by .NET's rule: scientific when the decimal exponent is < -4
// or >= the precision (17 for the round-trip case). Scientific uses a >=2-digit
// exponent (unlike `:E`'s >=3) and trims trailing mantissa zeros.
// Doubles only — a float hole is widened to double upstream, so its shortest form
// is the double's, not the float's.
static Dn2CppString* dn2cpp_format_g(double v, int prec, bool upper, const Dn2CppNumberFormatInfo* nfi)
{
    if (v == 0.0)
        return std::signbit(v) ? dn2cpp_localize_ascii("-0", 2, nfi)
                               : dn2cpp_string_from_ascii("0", 1);
    bool neg = v < 0.0;
    double mag = neg ? -v : v;
    int digitCount, threshold;
    if (prec >= 1)
    {
        digitCount = prec;
        threshold = prec;
    }
    else
    {
        // Shortest significant-digit count that round-trips through the double:
        // std::to_chars(scientific, no precision) emits the shortest round-tripping
        // form in one pass, so its mantissa digit count is the answer. Only the
        // COUNT comes from to_chars; the digits are rendered by the snprintf below.
        char tb[40];
        auto res = std::to_chars(tb, tb + sizeof(tb), mag, std::chars_format::scientific);
        digitCount = 0;
        for (const char* p = tb; p < res.ptr && *p != 'e' && *p != 'E'; ++p)
            if (*p >= '0' && *p <= '9')
                digitCount++;
        if (digitCount < 1) digitCount = 1;
        if (digitCount > 17) digitCount = 17;
        threshold = 17;
    }
    // Correctly-rounded `digitCount` significant digits + the decimal exponent.
    char eb[64];
    std::snprintf(eb, sizeof(eb), "%.*e", digitCount - 1, mag);
    char digits[20];
    int nd = 0, i = 0;
    for (; eb[i] != '\0' && eb[i] != 'e' && eb[i] != 'E'; i++)
        if (eb[i] != '.')
            digits[nd++] = eb[i];
    int E = std::atoi(eb + i + 1);
    bool sci = (E < -4) || (E >= threshold);
    char out[80];
    int o = 0;
    if (neg)
        out[o++] = '-';
    if (sci)
    {
        out[o++] = digits[0];
        int last = nd;
        while (last > 1 && digits[last - 1] == '0')
            last--;
        if (last > 1)
        {
            out[o++] = '.';
            for (int k = 1; k < last; k++)
                out[o++] = digits[k];
        }
        out[o++] = upper ? 'E' : 'e';
        out[o++] = E < 0 ? '-' : '+';
        char ex[8];
        int xl = std::snprintf(ex, sizeof(ex), "%02d", E < 0 ? -E : E);
        for (int k = 0; k < xl; k++)
            out[o++] = ex[k];
    }
    else if (E >= 0)
    {
        int intDigits = E + 1;
        for (int k = 0; k < intDigits; k++)
            out[o++] = k < nd ? digits[k] : '0';
        int last = nd;
        while (last > intDigits && digits[last - 1] == '0')
            last--;
        if (last > intDigits)
        {
            out[o++] = '.';
            for (int k = intDigits; k < last; k++)
                out[o++] = digits[k];
        }
    }
    else
    {
        out[o++] = '0';
        out[o++] = '.';
        for (int k = 0; k < -E - 1; k++)
            out[o++] = '0';
        int last = nd;
        while (last > 1 && digits[last - 1] == '0')
            last--;
        for (int k = 0; k < last; k++)
            out[o++] = digits[k];
    }
    return dn2cpp_localize_ascii(out, o, nfi);
}

Dn2CppString* dn2cpp_format_r8_c(double v, Dn2CppString* fmt, const Dn2CppNumberFormatInfo* nfi)
{
    nfi = dn2cpp_nfi_or_current(nfi);
    if (!dn2cpp_is_standard_fmt(fmt))
        return dn2cpp_format_custom_real(v, 15, fmt, nfi);
    char letter;
    int prec;
    dn2cpp_parse_fmt(fmt, &letter, &prec);
    // NaN/±Infinity render as the culture symbols for every standard numeric spec.
    if (std::isnan(v)) return nfi->nan;
    if (std::isinf(v)) return v < 0 ? nfi->negInf : nfi->posInf;
    char buf[80];
    int len;
    bool neg = std::signbit(v);
    double mag = neg ? -v : v;
    switch (letter)
    {
        case 'f':
        case 'F':
        {
            Dn2CppString* num = dn2cpp_localize_fixed_mag(mag, prec < 0 ? nfi->numberDigits : prec,
                nfi->numberGroup, nfi->numberDecimal, nfi->groupSizes, nfi->groupSizeCount, false);
            return neg ? dn2cpp_string_concat2(nfi->negativeSign, num) : num;
        }
        case 'n':
        case 'N':
        {
            Dn2CppString* num = dn2cpp_localize_fixed_mag(mag, prec < 0 ? nfi->numberDigits : prec,
                nfi->numberGroup, nfi->numberDecimal, nfi->groupSizes, nfi->groupSizeCount, true);
            return neg ? dn2cpp_lay_pattern(s_numberNeg[nfi->numberNegPattern % 5],
                0, nullptr, num, nfi->negativeSign) : num;
        }
        case 'e':
        case 'E':
        {
            // Scientific notation: default 6 mantissa digits, exponent padded to 3.
            // dn2cpp_localize_ascii maps '.'->decimal and the (mantissa/exponent)
            // '-' sign(s) -> the culture negative sign.
            len = std::snprintf(buf, sizeof(buf),
                letter == 'E' ? "%.*E" : "%.*e", prec < 0 ? 6 : prec, v);
            char padded[96];
            int plen = dn2cpp_pad_exponent_buf(buf, len, padded);
            return dn2cpp_localize_ascii(padded, plen, nfi);
        }
        case 'g':
        case 'G':
            return dn2cpp_format_g(v, prec, letter == 'G', nfi);
        case 'r':
        case 'R':
            // Round-trip: the shortest round-trippable form; .NET accepts and
            // IGNORES any precision digits ("R7" == "R"). Lowercase "r" renders
            // a lowercase exponent marker, like "g".
        {
            return letter == 'r' ? dn2cpp_double_to_string_lower_c(v, nfi)
                                 : dn2cpp_double_to_string_c(v, nfi);
        }
        case 'p':
        case 'P':
        {
            // Percent: scale by 100, group with the percent separators, then lay
            // the magnitude into the culture's percent pos/neg pattern.
            Dn2CppString* num = dn2cpp_localize_fixed_mag(mag * 100.0, prec < 0 ? nfi->percentDigits : prec,
                nfi->percentGroup, nfi->percentDecimal, nfi->percentGroupSizes, nfi->percentGroupSizeCount, true);
            const char16_t* tpl = neg ? s_percentNeg[nfi->percentNegPattern % 12]
                                      : s_percentPos[nfi->percentPosPattern % 4];
            return dn2cpp_lay_pattern(tpl, u'%', nfi->percentSymbol, num, nfi->negativeSign);
        }
        case 'c':
        case 'C':
        {
            // Currency: magnitude with the currency separators + default digit
            // count, laid into the culture's currency pos/neg pattern.
            int cd = prec < 0 ? nfi->currencyDigits : prec;
            Dn2CppString* num = dn2cpp_localize_fixed_mag(mag, cd,
                nfi->currencyGroup, nfi->currencyDecimal, nfi->currencyGroupSizes, nfi->currencyGroupSizeCount, true);
            const char16_t* tpl = neg ? s_currencyNeg[nfi->currencyNegPattern % 16]
                                      : s_currencyPos[nfi->currencyPosPattern % 4];
            return dn2cpp_lay_pattern(tpl, u'$', nfi->currencySymbol, num, nfi->negativeSign);
        }
        default:
            return dn2cpp_double_to_string_c(v, nfi);
    }
}

Dn2CppString* dn2cpp_format_r8(double v, Dn2CppString* fmt)
{
    return dn2cpp_format_r8_c(v, fmt, nullptr);
}

// Formatting a float. The "G"/"R" shortest forms (no explicit precision) must
// round-trip at *float* precision (so 1f/3f is "0.33333334", not the double's digits),
// and custom formats start from 7 significant digits; every other standard spec
// rounds identically whether computed from the float or its exact double widening,
// so they delegate to the double path.
Dn2CppString* dn2cpp_format_r4_c(float v, Dn2CppString* fmt, const Dn2CppNumberFormatInfo* n)
{
    if (!dn2cpp_is_standard_fmt(fmt))
        return dn2cpp_format_custom_real(v, 7, fmt, n);
    char letter; int prec;
    dn2cpp_parse_fmt(fmt, &letter, &prec);
    // "R"/"r" accept and IGNORE a precision ("R7" == "R"), so they stay on the
    // float shortest-round-trip path regardless of digits; "G0" is also the
    // default precision. Positive G-precision rounds identically from the
    // exact double widening. Lowercase specifiers use a lowercase exponent.
    if (letter == 'R' || letter == 'r'
        || (prec <= 0 && (letter == 'G' || letter == 'g'))
        || (prec < 0 && letter == 0))
    {
        return (letter == 'r' || letter == 'g') ? dn2cpp_float_to_string_lower_c(v, n)
                                               : dn2cpp_float_to_string_c(v, n);
    }
    return dn2cpp_format_r8_c(static_cast<double>(v), fmt, n);
}

Dn2CppString* dn2cpp_format_r4(float v, Dn2CppString* fmt)
{
    return dn2cpp_format_r4_c(v, fmt, nullptr);
}

Dn2CppString* dn2cpp_format_boxed_numeric(Dn2CppObject* box, Dn2CppString* fmt,
    Dn2CppObject* provider)
{
    if (box == nullptr)
        return nullptr;
    const Dn2CppTypeInfo* type = box->type;
    const void* payload = box + 1;
    auto signedFormat = [&](int64_t value, int32_t width)
    {
        const Dn2CppNumberFormatInfo* nfi = dn2cpp_integral_format_uses_provider(fmt, value < 0)
            ? dn2cpp_format_provider_nfi(provider) : nullptr;
        return dn2cpp_format_int_c(value, width, fmt, nfi);
    };
    auto unsignedFormat = [&](uint64_t value, int32_t width)
    {
        const Dn2CppNumberFormatInfo* nfi = dn2cpp_integral_format_uses_provider(fmt, 0)
            ? dn2cpp_format_provider_nfi(provider) : nullptr;
        return dn2cpp_format_uint_c(value, width, fmt, nfi);
    };
    if (type == &dn2cpp_int32_type)
        return signedFormat(*reinterpret_cast<const int32_t*>(payload), 4);
    if (type == &dn2cpp_sbyte_type)
        return signedFormat(static_cast<int8_t>(*reinterpret_cast<const int32_t*>(payload)), 1);
    if (type == &dn2cpp_int16_type)
        return signedFormat(static_cast<int16_t>(*reinterpret_cast<const int32_t*>(payload)), 2);
    if (type == &dn2cpp_byte_type)
        return unsignedFormat(static_cast<uint8_t>(*reinterpret_cast<const int32_t*>(payload)), 1);
    if (type == &dn2cpp_uint16_type)
        return unsignedFormat(static_cast<uint16_t>(*reinterpret_cast<const int32_t*>(payload)), 2);
    if (type == &dn2cpp_uint32_type)
        return unsignedFormat(*reinterpret_cast<const uint32_t*>(payload), 4);
    if (type == &dn2cpp_int64_type)
        return signedFormat(*reinterpret_cast<const int64_t*>(payload), 8);
    if (type == &dn2cpp_uint64_type)
        return unsignedFormat(*reinterpret_cast<const uint64_t*>(payload), 8);
    if (type == &dn2cpp_intptr_type)
        return signedFormat(*reinterpret_cast<const intptr_t*>(payload), static_cast<int32_t>(sizeof(intptr_t)));
    if (type == &dn2cpp_uintptr_type)
        return unsignedFormat(*reinterpret_cast<const uintptr_t*>(payload), static_cast<int32_t>(sizeof(uintptr_t)));
    if (type == &dn2cpp_double_type)
        return dn2cpp_format_r8_c(*reinterpret_cast<const double*>(payload), fmt, dn2cpp_format_provider_nfi(provider));
    if (type == &dn2cpp_single_type)
        return dn2cpp_format_r4_c(*reinterpret_cast<const float*>(payload), fmt, dn2cpp_format_provider_nfi(provider));
    if (type == &dn2cpp_decimal_type && type->formatspec != nullptr)
        return type->formatspec(box, fmt, dn2cpp_format_provider_nfi(provider));
    return nullptr;
}

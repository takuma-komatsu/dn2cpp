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

// Unit-local formatting primitives, used before their definitions below: `put`
// appends a string into a char16_t buffer at offset `o`; `format_int_exact`
// renders an integer at full precision for the N/F/C/G(no-precision) specs.
static void dn2cpp_put(char16_t* out, int& o, Dn2CppString* s);
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
// letter and optional precision. An empty/null string or a specifier with any
// non-digit trailing char yields letter 0 (default formatting).
static void dn2cpp_parse_fmt(Dn2CppString* fmt, char* letter, int* prec)
{
    *letter = 0;
    *prec = -1;
    if (fmt == nullptr || fmt->length == 0)
        return;
    *letter = static_cast<char>(fmt->chars[0]);
    int p = 0;
    bool has = false;
    for (int32_t i = 1; i < fmt->length; i++)
    {
        char16_t c = fmt->chars[i];
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
static Dn2CppString* dn2cpp_decimal_padded(unsigned long long mag, bool neg, int prec)
{
    char digits[24];
    int dl = std::snprintf(digits, sizeof(digits), "%llu", mag);
    int minDigits = prec < 0 ? 1 : prec;
    int zeros = dl < minDigits ? minDigits - dl : 0;
    int total = (neg ? 1 : 0) + zeros + dl;
    char16_t* dst;
    Dn2CppString* s = dn2cpp_string_alloc(&dst, total);
    int i = 0;
    if (neg)
        dst[i++] = u'-';
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
        prec < 0 ? 1 : prec, static_cast<unsigned long long>(v & mask));
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

// A format string is "standard" iff it is a single ASCII letter optionally
// followed by digits (e.g. "X", "D5", "N2"). Anything else non-empty (e.g. "000",
// "#,##0.00") is a custom numeric pattern.
static bool dn2cpp_is_standard_fmt(Dn2CppString* fmt)
{
    if (fmt == nullptr || fmt->length == 0)
        return true; // empty -> default standard formatting
    char16_t c0 = fmt->chars[0];
    bool alpha = (c0 >= u'A' && c0 <= u'Z') || (c0 >= u'a' && c0 <= u'z');
    if (!alpha)
        return false;
    for (int32_t i = 1; i < fmt->length; i++)
        if (fmt->chars[i] < u'0' || fmt->chars[i] > u'9')
            return false;
    return true;
}

// Render `mag` (a non-negative magnitude) under one custom-numeric-format section
// `p[0..L)`. Handles digit placeholders `0`/`#`, `.`, `,` grouping,
// `%`(×100)/`‰`(×1000) scaling with the symbol emitted in place, `\` escapes,
// `'…'`/`"…"` quoted literals and bare literal characters emitted in position, and
// custom exponential (`E`/`e` `[+/-]` `0+`). A leading `-` is added when `emitSign`
// (a single-section negative). One integer placeholder is the common exponential
// form (`0.00E+00`); engineering grouping for >1 integer placeholders is not modeled.
static Dn2CppString* dn2cpp_format_section(const char16_t* p, int L, double mag, bool emitSign,
                                          const Dn2CppNumberFormatInfo* nfi)
{
    // ---- pass 1: analyze placeholders, scale, and any exponential spec ----
    int intPlace = 0, intZeros = 0, fracMax = 0, fracMin = 0;
    bool group = false, seenDot = false, inFrac = false;
    double scale = 1.0;
    // expForceSign / expDigits are re-parsed from the format by pass 2's exponent
    // emitter, so these copies are only pass-1 bookkeeping.
    bool expMode = false, expUpper = false;
    [[maybe_unused]] bool expForceSign = false;
    [[maybe_unused]] int expDigits = 0;
    {
        bool inq = false; char16_t q = 0;
        for (int i = 0; i < L; i++)
        {
            char16_t c = p[i];
            if (inq) { if (c == q) inq = false; continue; }
            if (c == u'\\') { i++; continue; }
            if (c == u'\'' || c == u'"') { inq = true; q = c; continue; }
            if (c == u'0' || c == u'#')
            {
                if (inFrac) { fracMax++; if (c == u'0') fracMin = fracMax; }
                else { intPlace++; if (c == u'0') intZeros++; }
            }
            else if (c == u'.') { if (!seenDot) { seenDot = true; inFrac = true; } }
            else if (c == u',') { if (!inFrac) group = true; }
            else if (c == u'%') { scale *= 100.0; }
            else if (c == u'‰') { scale *= 1000.0; }
            else if (c == u'E' || c == u'e')
            {
                int j = i + 1; bool fs = false;
                if (j < L && (p[j] == u'+' || p[j] == u'-')) { fs = (p[j] == u'+'); j++; }
                int z = 0; while (j < L && p[j] == u'0') { z++; j++; }
                if (z > 0) { expMode = true; expUpper = (c == u'E'); expForceSign = fs; expDigits = z; i = j - 1; }
            }
        }
    }
    mag *= scale;

    // ---- compute the integer/fraction digit streams ----
    char idig[64]; int iLen = 0;
    char fdig[64]; int fLen = 0;
    int expVal = 0;
    if (expMode)
    {
        // Engineering exponential: the mantissa carries `intPlace` integer digits
        // (the common intPlace==1 case is plain scientific), with the exponent
        // shifted so it is a multiple of that count. .NET: `##0.0E+0` of 1234.5 is
        // "123.5E+1" (3 integer digits), `0.0E+0` is "1.2E+3".
        int wantInt = intPlace < 1 ? 1 : intPlace;
        if (mag == 0.0)
        {
            for (int i = 0; i < wantInt; i++) idig[iLen++] = '0';
            for (int i = 0; i < fracMax; i++) fdig[fLen++] = '0';
            expVal = 0;
        }
        else
        {
            int Ep = static_cast<int>(std::floor(std::log10(mag))) - (wantInt - 1);
            char eb[80]; int len = 0, dot = 0;
            for (int guard = 0; guard < 4; guard++)
            {
                double mant = mag / std::pow(10.0, Ep);
                len = std::snprintf(eb, sizeof(eb), "%.*f", fracMax, mant);
                dot = len;
                for (int i = 0; i < len; i++) if (eb[i] == '.') { dot = i; break; }
                int idg = (dot == 1 && eb[0] == '0') ? 0 : dot;
                if (idg == wantInt) break;
                Ep += idg - wantInt; // correct a rounding carry / log10 rounding
            }
            for (int i = 0; i < dot; i++) idig[iLen++] = eb[i];
            for (int i = dot + 1; i < len; i++) fdig[fLen++] = eb[i];
            expVal = Ep;
        }
        while (fLen > fracMin && fdig[fLen - 1] == '0') fLen--; // trim '#' frac zeros
    }
    else
    {
        char buf[80];
        int len = std::snprintf(buf, sizeof(buf), "%.*f", fracMax, mag);
        int dot = len;
        for (int i = 0; i < len; i++) if (buf[i] == '.') { dot = i; break; }
        int sigInt = (dot == 1 && buf[0] == '0') ? 0 : dot; // magnitude < 1 has no int digits
        for (int i = 0; i < sigInt; i++) idig[iLen++] = buf[i];
        int fstart = (dot < len) ? dot + 1 : len;
        for (int i = fstart; i < len; i++) fdig[fLen++] = buf[i];
        while (fLen > fracMin && fdig[fLen - 1] == '0') fLen--; // trim '#' frac zeros
    }

    // Integer block: left-pad to the '0' count, then group. Emitted whole at the
    // first integer placeholder (interleaving literals between integer digits is not
    // modeled — vanishingly rare).
    char16_t intOut[80]; int io = 0;
    {
        int pad = intZeros > iLen ? intZeros - iLen : 0;
        char dg[80]; int dn = 0;
        for (int i = 0; i < pad; i++) dg[dn++] = '0';
        for (int i = 0; i < iLen; i++) dg[dn++] = idig[i];
        for (int i = 0; i < dn; i++)
        {
            intOut[io++] = (char16_t)dg[i];
            int rem = dn - 1 - i;
            if (group && rem > 0 && rem % 3 == 0) dn2cpp_put(intOut, io, nfi->numberGroup);
        }
    }

    // ---- weave the output: digits into placeholders, everything else literal ----
    char16_t out[256]; int o = 0;
    if (emitSign) dn2cpp_put(out, o, nfi->negativeSign);
    bool intDone = false, fracDone = false;
    bool inq = false; char16_t q = 0;
    for (int i = 0; i < L; i++)
    {
        char16_t c = p[i];
        if (inq) { if (c == q) inq = false; else out[o++] = c; continue; }
        if (c == u'\\') { if (i + 1 < L) out[o++] = p[++i]; continue; }
        if (c == u'\'' || c == u'"') { inq = true; q = c; continue; }
        // A digit placeholder is integer or fraction depending on whether a '.'
        // precedes it; the whole integer/fraction block lands at its first placeholder.
        if (c == u'0' || c == u'#')
        {
            bool beforeDot = true;
            for (int k = 0; k < i; k++) { char16_t d = p[k]; if (d == u'.') { beforeDot = false; break; } }
            if (beforeDot) { if (!intDone) { for (int k = 0; k < io; k++) out[o++] = intOut[k]; intDone = true; } }
            else { if (!fracDone) { for (int k = 0; k < fLen; k++) out[o++] = (char16_t)fdig[k]; fracDone = true; } }
            continue;
        }
        if (c == u'.') { if (fLen > 0) dn2cpp_put(out, o, nfi->numberDecimal); continue; }
        if (c == u',') continue;                 // grouping marker, not a literal
        if (c == u'%') { dn2cpp_put(out, o, nfi->percentSymbol); continue; }
        if (c == u'‰') { out[o++] = u'‰'; continue; }
        if (c == u'E' || c == u'e')
        {
            int j = i + 1; bool fs = false;
            if (j < L && (p[j] == u'+' || p[j] == u'-')) { fs = (p[j] == u'+'); j++; }
            int z = 0; while (j < L && p[j] == u'0') { z++; j++; }
            if (z > 0)
            {
                out[o++] = expUpper ? u'E' : u'e';
                if (expVal < 0) out[o++] = u'-';
                else if (fs) out[o++] = u'+';
                char eb[16]; int el = std::snprintf(eb, sizeof(eb), "%d", expVal < 0 ? -expVal : expVal);
                for (int z2 = el; z2 < z; z2++) out[o++] = u'0';
                for (int k = 0; k < el; k++) out[o++] = (char16_t)eb[k];
                i = j - 1;
                continue;
            }
        }
        out[o++] = c; // any other character is a literal emitted in place
    }
    char16_t* buf;
    Dn2CppString* s = dn2cpp_string_alloc(&buf, o);
    for (int i = 0; i < o; i++) buf[i] = out[i];
    return s;
}

static Dn2CppString* dn2cpp_format_custom_c(double v, Dn2CppString* fmt, const Dn2CppNumberFormatInfo* nfi)
{
    nfi = dn2cpp_nfi_or_current(nfi);
    // Split into up to 3 sections on a top-level ';' (quotes/escapes shield it):
    // positive ; negative ; zero. Fewer sections reuse section 0 (negatives then
    // carry an explicit '-').
    int secStart[3] = {0, 0, 0}, secLen[3] = {0, 0, 0}, nsec = 0;
    {
        int start = 0; bool inq = false; char16_t q = 0;
        for (int i = 0; i < fmt->length; i++)
        {
            char16_t c = fmt->chars[i];
            if (inq) { if (c == q) inq = false; continue; }
            if (c == u'\\') { i++; continue; }
            if (c == u'\'' || c == u'"') { inq = true; q = c; continue; }
            if (c == u';' && nsec < 2) { secStart[nsec] = start; secLen[nsec] = i - start; nsec++; start = i + 1; }
        }
        secStart[nsec] = start; secLen[nsec] = fmt->length - start; nsec++;
    }
    const char16_t* base = fmt->chars;
    int sel; bool emitSign; double mag;
    if (v > 0.0) { sel = 0; emitSign = false; mag = v; }
    else if (v < 0.0)
    {
        mag = -v;
        if (nsec >= 2) { sel = 1; emitSign = false; }   // section provides the negative form
        else { sel = 0; emitSign = true; }
    }
    else { mag = 0.0; emitSign = false; sel = (nsec >= 3) ? 2 : 0; }
    return dn2cpp_format_section(base + secStart[sel], secLen[sel], mag, emitSign, nfi);
}

// Signed integral hole (sbyte/short/int/long/nint). `byteWidth` (1/2/4/8) is the
// declared type width, used by hex to size the two's-complement pattern.
Dn2CppString* dn2cpp_format_int_c(int64_t v, int32_t byteWidth, Dn2CppString* fmt, const Dn2CppNumberFormatInfo* nfi)
{
    nfi = dn2cpp_nfi_or_current(nfi);
    if (!dn2cpp_is_standard_fmt(fmt))
        return dn2cpp_format_custom_c(static_cast<double>(v), fmt, nfi);
    char letter;
    int prec;
    dn2cpp_parse_fmt(fmt, &letter, &prec);
    bool neg = v < 0;
    // Modular negation gives the right magnitude even for INT64_MIN.
    unsigned long long mag = neg
        ? (0ULL - static_cast<unsigned long long>(v))
        : static_cast<unsigned long long>(v);
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
            return dn2cpp_decimal_padded(mag, neg, prec);
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
    nfi = dn2cpp_nfi_or_current(nfi);
    if (!dn2cpp_is_standard_fmt(fmt))
        return dn2cpp_format_custom_c(static_cast<double>(v), fmt, nfi);
    char letter;
    int prec;
    dn2cpp_parse_fmt(fmt, &letter, &prec);
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
            return dn2cpp_decimal_padded(v, false, prec);
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
    if (doGroup)
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
    if (doGroup)
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
// is a literal. Backs the Currency/Percent positive/negative pattern indices.
static Dn2CppString* dn2cpp_lay_pattern(const char16_t* tpl, char16_t symCh, Dn2CppString* sym,
                                        Dn2CppString* num, Dn2CppString* neg)
{
    char16_t out[256];
    int o = 0;
    for (int i = 0; tpl[i] != u'\0'; i++)
    {
        char16_t c = tpl[i];
        if (c == symCh) dn2cpp_put(out, o, sym);
        else if (c == u'n') dn2cpp_put(out, o, num);
        else if (c == u'-') dn2cpp_put(out, o, neg);
        else out[o++] = c;
    }
    char16_t* dst;
    Dn2CppString* s = dn2cpp_string_alloc(&dst, o);
    for (int i = 0; i < o; i++) dst[i] = out[i];
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
            Dn2CppString* num = dn2cpp_localize_fixed_int(mag, prec < 0 ? 2 : prec,
                nfi->numberGroup, nfi->numberDecimal, nfi->groupSizes, nfi->groupSizeCount, false);
            return neg ? dn2cpp_string_concat2(nfi->negativeSign, num) : num;
        }
        case 'n': case 'N':
        {
            Dn2CppString* num = dn2cpp_localize_fixed_int(mag, prec < 0 ? 2 : prec,
                nfi->numberGroup, nfi->numberDecimal, nfi->groupSizes, nfi->groupSizeCount, true);
            return neg ? dn2cpp_string_concat2(nfi->negativeSign, num) : num;
        }
        case 'c': case 'C':
        {
            int cd = prec < 0 ? nfi->currencyDigits : prec;
            Dn2CppString* num = dn2cpp_localize_fixed_int(mag, cd,
                nfi->currencyGroup, nfi->currencyDecimal, nfi->groupSizes, nfi->groupSizeCount, true);
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

// Lowercase-exponent twin for the shortest-round-trip renderer ("r"/"g" emit 'e'
// where "R"/"G" emit 'E'). That renderer's only alphabetic ASCII is the exponent
// marker, so a targeted 'E'->'e' rewrite is exact; the input is left untouched
// (copy-on-write) so shared culture symbols (NaN/Infinity) are never mutated —
// none of them contains an 'E', so they pass through unchanged.
static Dn2CppString* dn2cpp_lower_exp(Dn2CppString* s)
{
    if (s == nullptr)
        return s;
    for (int32_t i = 0; i < s->length; i++)
        if (s->chars[i] == u'E')
        {
            char16_t* dst;
            Dn2CppString* r = dn2cpp_string_alloc(&dst, s->length);
            for (int32_t k = 0; k < s->length; k++)
                dst[k] = s->chars[k] == u'E' ? u'e' : s->chars[k];
            return r;
        }
    return s;
}

Dn2CppString* dn2cpp_format_r8_c(double v, Dn2CppString* fmt, const Dn2CppNumberFormatInfo* nfi)
{
    nfi = dn2cpp_nfi_or_current(nfi);
    if (!dn2cpp_is_standard_fmt(fmt))
        return dn2cpp_format_custom_c(v, fmt, nfi);
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
            Dn2CppString* num = dn2cpp_localize_fixed_mag(mag, prec < 0 ? 2 : prec,
                nfi->numberGroup, nfi->numberDecimal, nfi->groupSizes, nfi->groupSizeCount, false);
            return neg ? dn2cpp_string_concat2(nfi->negativeSign, num) : num;
        }
        case 'n':
        case 'N':
        {
            Dn2CppString* num = dn2cpp_localize_fixed_mag(mag, prec < 0 ? 2 : prec,
                nfi->numberGroup, nfi->numberDecimal, nfi->groupSizes, nfi->groupSizeCount, true);
            return neg ? dn2cpp_string_concat2(nfi->negativeSign, num) : num;
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
            Dn2CppString* s = dn2cpp_double_to_string_c(v, nfi);
            return letter == 'r' ? dn2cpp_lower_exp(s) : s;
        }
        case 'p':
        case 'P':
        {
            // Percent: scale by 100, group with the percent separators, then lay
            // the magnitude into the culture's percent pos/neg pattern.
            Dn2CppString* num = dn2cpp_localize_fixed_mag(mag * 100.0, prec < 0 ? 2 : prec,
                nfi->percentGroup, nfi->percentDecimal, nfi->groupSizes, nfi->groupSizeCount, true);
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
                nfi->currencyGroup, nfi->currencyDecimal, nfi->groupSizes, nfi->groupSizeCount, true);
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
// round-trip at *float* precision (so 1f/3f is "0.33333334", not the double's digits);
// every other standard/custom spec rounds identically whether computed from the float
// or its exact double widening, so they delegate to the double path.
Dn2CppString* dn2cpp_format_r4_c(float v, Dn2CppString* fmt, const Dn2CppNumberFormatInfo* n)
{
    if (dn2cpp_is_standard_fmt(fmt))
    {
        char letter; int prec;
        dn2cpp_parse_fmt(fmt, &letter, &prec);
        // "R"/"r" accept and IGNORE a precision ("R7" == "R"), so they stay on the
        // float shortest-round-trip path regardless of digits; "G"/default only
        // when no precision is given (an explicit G-precision rounds identically
        // from the exact double widening). The lowercase specifiers render a
        // lowercase exponent marker.
        if (letter == 'R' || letter == 'r'
            || (prec < 0 && (letter == 'G' || letter == 'g' || letter == 0)))
        {
            Dn2CppString* s = dn2cpp_float_to_string_c(v, n);
            return (letter == 'r' || letter == 'g') ? dn2cpp_lower_exp(s) : s;
        }
    }
    return dn2cpp_format_r8_c(static_cast<double>(v), fmt, n);
}

Dn2CppString* dn2cpp_format_r4(float v, Dn2CppString* fmt)
{
    return dn2cpp_format_r4_c(v, fmt, nullptr);
}

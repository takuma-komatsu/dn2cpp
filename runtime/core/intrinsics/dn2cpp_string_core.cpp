// dn2cpp_string_core.cpp — System.String storage and content primitives:
// Dn2CppString allocation and construction (interned ldstr literals, the
// extern `new string(...)` ctors, string.Create), the raw UTF-8 <-> UTF-16
// transcode cores shared with the POSIX ANSI PAL seam, the ANSI code-page
// codec entry points, the span-concat lowerings Roslyn emits for
// char-in-concat, and the ordinal content equality/hash.
#include "dn2cpp_core.h"
#include "platform/dn2cpp_pal.h" // ANSI code-page codec (P/Invoke Ansi marshalling)

#include <cstdint>
#include <cstring>

Dn2CppString* dn2cpp_string_literal(const char16_t* chars, int32_t length)
{
    // Every ldstr literal is interned, matching .NET. The empty literal is also
    // emitted inline at call sites (Join/Concat lowerings), so it gets a cached
    // instance instead of a pool probe per call.
    if (length == 0)
    {
        static Dn2CppString* const empty = [] {
            auto* s = static_cast<Dn2CppString*>(dn2cpp_alloc(sizeof(Dn2CppString)));
            s->type = &dn2cpp_string_type;
            s->length = 0;
            s->chars = u"";
            return dn2cpp_string_intern(s);
        }();
        return empty;
    }
    auto* s = static_cast<Dn2CppString*>(dn2cpp_alloc(sizeof(Dn2CppString)));
    s->type = &dn2cpp_string_type;
    s->length = length;
    s->chars = chars; // literals point at .rodata; no copy
    return dn2cpp_string_intern(s);
}

// Allocates a UTF-16 string of `length` code units (NUL-terminated). The buffer
// holds no managed pointers, so it is allocated atomically (unscanned) to keep
// string data out of the GC mark phase. Atomic blocks are not zero-filled, so
// every caller must fill [0, length) itself; the under-filling callers below
// memset to match .NET's zeroed GC memory.
Dn2CppString* dn2cpp_string_alloc(char16_t** outBuf, int32_t length)
{
    char16_t* buf = static_cast<char16_t*>(
        dn2cpp_alloc_atomic((static_cast<size_t>(length) + 1) * sizeof(char16_t)));
    buf[length] = u'\0';
    auto* s = static_cast<Dn2CppString*>(dn2cpp_alloc(sizeof(Dn2CppString)));
    s->type = &dn2cpp_string_type;
    s->length = length;
    s->chars = buf;
    *outBuf = buf;
    return s;
}

// String.FastAllocateString(length): for callers that fill the buffer afterwards
// through GetRawStringData (Guid.ToString and friends), which may under-fill.
Dn2CppString* dn2cpp_string_fast_allocate(int32_t length)
{
    char16_t* buf;
    Dn2CppString* s = dn2cpp_string_alloc(&buf, length);
    memset(buf, 0, static_cast<size_t>(length) * sizeof(char16_t));
    return s;
}

// string.Create<TState>(length, state, action): hands back the buffer so the
// transpiled call can build the SpanAction's Span<char> over it. The action may
// leave slots untouched and must see '\0' there, as in .NET.
Dn2CppString* dn2cpp_string_create_buffer(int32_t length, char16_t** outBuf)
{
    if (length < 0)
        dn2cpp_throw_argument_out_of_range();
    Dn2CppString* s = dn2cpp_string_alloc(outBuf, length);
    std::memset(*outBuf, 0, static_cast<size_t>(length) * sizeof(char16_t));
    return s;
}

// Copy `length` UTF-16 code units into a fresh, independent string. Backs
// `new string(ReadOnlySpan<char>/char[])` and span<char>.ToString(); the source
// may alias another string's buffer or a mutable array, so it must be copied.
Dn2CppString* dn2cpp_string_from_chars(const char16_t* chars, int32_t length)
{
    if (length < 0)
        dn2cpp_throw_argument_out_of_range();
    char16_t* buf;
    Dn2CppString* s = dn2cpp_string_alloc(&buf, length);
    for (int32_t i = 0; i < length; i++)
        buf[i] = chars[i];
    return s;
}

// `new string(char* value)` — extern/VM-implemented in the real BCL, so there is
// no managed body to transpile. A null pointer yields Empty, not an exception.
Dn2CppString* dn2cpp_string_from_wcs(const char16_t* value)
{
    if (value == nullptr)
        return dn2cpp_string_from_chars(nullptr, 0);
    int32_t length = 0;
    while (value[length] != 0)
        length++;
    return dn2cpp_string_from_chars(value, length);
}

// `new string(sbyte* value)` — likewise extern in the real BCL. Decodes through
// the system ANSI code page (CP_ACP on Windows, UTF-8 on POSIX), NOT as
// Encoding.Default. A null pointer yields Empty, matching _from_wcs.
Dn2CppString* dn2cpp_string_from_mbs(const char* value)
{
    if (value == nullptr)
        return dn2cpp_string_from_chars(nullptr, 0);
    return dn2cpp_string_from_ansi(value, static_cast<int32_t>(std::strlen(value)));
}

// `new string(char c, int count)` — a string of `count` copies of `c`.
Dn2CppString* dn2cpp_string_repeat_char(char16_t c, int32_t count)
{
    if (count < 0)
        dn2cpp_throw_argument_out_of_range();
    char16_t* buf;
    Dn2CppString* s = dn2cpp_string_alloc(&buf, count);
    for (int32_t i = 0; i < count; i++)
        buf[i] = c;
    return s;
}

// String.ToCharArray() — a fresh packed char[]. `ti` is the precise char[]
// type-info so result.GetType() == typeof(char[]).
Dn2CppArrayN* dn2cpp_string_to_chararray(Dn2CppString* s, const Dn2CppTypeInfo* ti)
{
    if (s == nullptr)
        dn2cpp_throw_null_reference();
    Dn2CppArrayN* arr = dn2cpp_newarr_n_t(s->length, static_cast<int32_t>(sizeof(char16_t)), ti);
    std::memcpy(arr->data, s->chars, static_cast<size_t>(s->length) * sizeof(char16_t));
    return arr;
}

// Concatenate N ReadOnlySpan<char> operands into a fresh string. Backs the span
// String.Concat overloads; a null/empty operand contributes nothing.
static Dn2CppString* dn2cpp_concat_spanchars(const char16_t* const* ptrs, const int32_t* lens, int count)
{
    int32_t total = 0;
    for (int i = 0; i < count; i++)
        if (lens[i] > 0) total += lens[i];
    char16_t* buf;
    Dn2CppString* s = dn2cpp_string_alloc(&buf, total);
    int32_t pos = 0;
    for (int i = 0; i < count; i++)
        if (lens[i] > 0 && ptrs[i] != nullptr)
        {
            std::memcpy(buf + pos, ptrs[i], static_cast<size_t>(lens[i]) * sizeof(char16_t));
            pos += lens[i];
        }
    return s;
}

Dn2CppString* dn2cpp_string_concat_spanchars2(const char16_t* p0, int32_t n0, const char16_t* p1, int32_t n1)
{
    const char16_t* ptrs[2] = { p0, p1 };
    int32_t lens[2] = { n0, n1 };
    return dn2cpp_concat_spanchars(ptrs, lens, 2);
}

Dn2CppString* dn2cpp_string_concat_spanchars3(const char16_t* p0, int32_t n0, const char16_t* p1, int32_t n1, const char16_t* p2, int32_t n2)
{
    const char16_t* ptrs[3] = { p0, p1, p2 };
    int32_t lens[3] = { n0, n1, n2 };
    return dn2cpp_concat_spanchars(ptrs, lens, 3);
}

Dn2CppString* dn2cpp_string_concat_spanchars4(const char16_t* p0, int32_t n0, const char16_t* p1, int32_t n1, const char16_t* p2, int32_t n2, const char16_t* p3, int32_t n3)
{
    const char16_t* ptrs[4] = { p0, p1, p2, p3 };
    int32_t lens[4] = { n0, n1, n2, n3 };
    return dn2cpp_concat_spanchars(ptrs, lens, 4);
}

// net10 added a five-span String.Concat overload; Path.JoinInternal's
// three-part join (first + "/" + second + "/" + third) binds to it.
Dn2CppString* dn2cpp_string_concat_spanchars5(const char16_t* p0, int32_t n0, const char16_t* p1, int32_t n1, const char16_t* p2, int32_t n2, const char16_t* p3, int32_t n3, const char16_t* p4, int32_t n4)
{
    const char16_t* ptrs[5] = { p0, p1, p2, p3, p4 };
    int32_t lens[5] = { n0, n1, n2, n3, n4 };
    return dn2cpp_concat_spanchars(ptrs, lens, 5);
}

// Widens an ASCII byte run to UTF-16. Used for numeric ToString output, which
// is always ASCII (digits, sign, exponent).
Dn2CppString* dn2cpp_string_from_ascii(const char* buf, int32_t len)
{
    char16_t* dst;
    Dn2CppString* s = dn2cpp_string_alloc(&dst, len);
    for (int32_t i = 0; i < len; i++)
        dst[i] = static_cast<char16_t>(static_cast<unsigned char>(buf[i]));
    return s;
}

// Raw UTF-8 -> UTF-16 decode core. Writes the decoded code units into `out` (when
// non-null; `out` must hold at least byteLength+1 units — a UTF-8 run of N bytes
// decodes to at most N UTF-16 units) and returns the unit count (excluding any NUL).
// Pass a null `out` to query the count only. Shared by dn2cpp_string_from_utf8 and
// the POSIX ANSI PAL seam, so the Unix Ansi path is byte-identical to UTF-8.
int32_t dn2cpp_utf8_to_utf16(const char* utf8, int32_t byteLength, char16_t* out)
{
    int32_t o = 0;
    int32_t i = 0;
    while (i < byteLength)
    {
        unsigned char c = static_cast<unsigned char>(utf8[i++]);
        char32_t cp;
        int extra;
        if (c < 0x80) { cp = c; extra = 0; }
        else if ((c & 0xE0) == 0xC0) { cp = c & 0x1Fu; extra = 1; }
        else if ((c & 0xF0) == 0xE0) { cp = c & 0x0Fu; extra = 2; }
        else if ((c & 0xF8) == 0xF0) { cp = c & 0x07u; extra = 3; }
        else { cp = 0xFFFDu; extra = 0; }
        // Consume the continuation bytes; a truncated or malformed run, an
        // overlong/out-of-range value, or a surrogate code point all decode to
        // U+FFFD (matching Encoding.UTF8's replacement behavior).
        bool bad = false;
        for (int k = 0; k < extra; k++)
        {
            if (i >= byteLength ||
                (static_cast<unsigned char>(utf8[i]) & 0xC0u) != 0x80u)
            {
                bad = true;
                break;
            }
            cp = (cp << 6) | (static_cast<unsigned char>(utf8[i++]) & 0x3Fu);
        }
        if (bad || cp > 0x10FFFFu || (cp >= 0xD800u && cp <= 0xDFFFu))
            cp = 0xFFFDu;
        if (cp <= 0xFFFFu)
        {
            if (out != nullptr) out[o] = static_cast<char16_t>(cp);
            o++;
        }
        else
        {
            cp -= 0x10000u;
            if (out != nullptr)
            {
                out[o] = static_cast<char16_t>(0xD800u + (cp >> 10));
                out[o + 1] = static_cast<char16_t>(0xDC00u + (cp & 0x3FFu));
            }
            o += 2;
        }
    }
    return o;
}

Dn2CppString* dn2cpp_string_from_utf8(const char* utf8, int32_t byteLength)
{
    // A UTF-8 run of N bytes decodes to at most N UTF-16 code units.
    char16_t* buf = static_cast<char16_t*>(
        dn2cpp_alloc((static_cast<size_t>(byteLength) + 1) * sizeof(char16_t)));
    int32_t out = dn2cpp_utf8_to_utf16(utf8, byteLength, buf);
    buf[out] = u'\0';
    auto* s = static_cast<Dn2CppString*>(dn2cpp_alloc(sizeof(Dn2CppString)));
    s->type = &dn2cpp_string_type;
    s->length = out;
    s->chars = buf;
    return s;
}

// Raw UTF-16 -> UTF-8 encode core. Returns the byte count (excluding any NUL). When
// `buf` is non-null, writes up to `bufSize` bytes (no NUL terminator); pass a null
// `buf` to query the required size. Shared by dn2cpp_string_to_utf8 and the POSIX
// ANSI PAL seam.
int32_t dn2cpp_utf16_to_utf8(const char16_t* src, int32_t len, char* buf, int32_t bufSize)
{
    int32_t bytes = 0;
    for (int32_t i = 0; i < len; i++)
    {
        char32_t cp = src[i];
        if (cp >= 0xD800u && cp <= 0xDBFFu)
        {
            // High surrogate: pair it with a following low surrogate, otherwise
            // it is unpaired and encodes as U+FFFD (matching Encoding.UTF8).
            char16_t lo = i + 1 < len ? src[i + 1] : 0;
            if (lo >= 0xDC00u && lo <= 0xDFFFu)
            {
                cp = 0x10000u + ((cp - 0xD800u) << 10) + (lo - 0xDC00u);
                i++;
            }
            else
            {
                cp = 0xFFFDu;
            }
        }
        else if (cp >= 0xDC00u && cp <= 0xDFFFu)
        {
            // Lone low surrogate.
            cp = 0xFFFDu;
        }
        unsigned char enc[4];
        int n;
        if (cp < 0x80u)
        {
            enc[0] = static_cast<unsigned char>(cp);
            n = 1;
        }
        else if (cp < 0x800u)
        {
            enc[0] = static_cast<unsigned char>(0xC0u | (cp >> 6));
            enc[1] = static_cast<unsigned char>(0x80u | (cp & 0x3Fu));
            n = 2;
        }
        else if (cp < 0x10000u)
        {
            enc[0] = static_cast<unsigned char>(0xE0u | (cp >> 12));
            enc[1] = static_cast<unsigned char>(0x80u | ((cp >> 6) & 0x3Fu));
            enc[2] = static_cast<unsigned char>(0x80u | (cp & 0x3Fu));
            n = 3;
        }
        else
        {
            enc[0] = static_cast<unsigned char>(0xF0u | (cp >> 18));
            enc[1] = static_cast<unsigned char>(0x80u | ((cp >> 12) & 0x3Fu));
            enc[2] = static_cast<unsigned char>(0x80u | ((cp >> 6) & 0x3Fu));
            enc[3] = static_cast<unsigned char>(0x80u | (cp & 0x3Fu));
            n = 4;
        }
        for (int k = 0; k < n; k++)
        {
            if (buf != nullptr && bytes < bufSize)
                buf[bytes] = static_cast<char>(enc[k]);
            bytes++;
        }
    }
    return bytes;
}

int32_t dn2cpp_string_to_utf8(Dn2CppString* s, char* buf, int32_t bufSize)
{
    return dn2cpp_utf16_to_utf8(s != nullptr ? s->chars : nullptr,
                                s != nullptr ? s->length : 0, buf, bufSize);
}

// System-ANSI-code-page string codec — real .NET's default Ansi P/Invoke marshalling.
// On Windows the PAL routes to WideCharToMultiByte/MultiByteToWideChar (CP_ACP, with
// best-fit substitution — the .NET default); on POSIX the PAL delegates to the UTF-8
// cores above, so on Unix this is byte-identical to the UTF-8 path (Ansi == UTF-8 there).
int32_t dn2cpp_string_to_ansi(Dn2CppString* s, char* buf, int32_t bufSize, int32_t bestFit)
{
    return dn2cpp_pal_ansi_encode(s != nullptr ? s->chars : nullptr,
                                  s != nullptr ? s->length : 0, buf, bufSize, bestFit);
}

Dn2CppString* dn2cpp_string_from_ansi(const char* bytes, int32_t byteLength)
{
    // Query the exact decoded length, then allocate and decode once.
    int32_t n = dn2cpp_pal_ansi_decode(bytes, byteLength, nullptr, 0);
    char16_t* buf = static_cast<char16_t*>(
        dn2cpp_alloc((static_cast<size_t>(n) + 1) * sizeof(char16_t)));
    dn2cpp_pal_ansi_decode(bytes, byteLength, buf, n);
    buf[n] = u'\0';
    auto* s = static_cast<Dn2CppString*>(dn2cpp_alloc(sizeof(Dn2CppString)));
    s->type = &dn2cpp_string_type;
    s->length = n;
    s->chars = buf;
    return s;
}

int32_t dn2cpp_string_equals(Dn2CppString* a, Dn2CppString* b)
{
    if (a == b)
        return 1;
    if (a == nullptr || b == nullptr)
        return 0;
    if (a->length != b->length)
        return 0;
    return std::memcmp(a->chars, b->chars,
        static_cast<size_t>(a->length) * sizeof(char16_t)) == 0 ? 1 : 0;
}

// Deterministic FNV-1a over `length` raw UTF-16 code units. Stable within a run
// and well distributed; we don't reproduce the BCL's randomized Marvin hash. A
// ReadOnlySpan<char> hashed here matches the string carrying the same code units
// (string.GetHashCode delegates to it), which an ordinal alternate-key lookup
// — Dictionary<string,…>.GetAlternateLookup<ReadOnlySpan<char>>() — requires.
int32_t dn2cpp_chars_hashcode(const char16_t* p, int32_t length)
{
    uint32_t hash = 2166136261u;
    for (int32_t i = 0; i < length; i++)
    {
        hash = (hash ^ static_cast<uint16_t>(p[i])) * 16777619u;
    }
    return static_cast<int32_t>(hash);
}

int32_t dn2cpp_string_hashcode(Dn2CppString* s)
{
    return s == nullptr ? 0 : dn2cpp_chars_hashcode(s->chars, s->length);
}

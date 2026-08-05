// dn2cpp_system_text.cpp — System.Text intrinsics.
//
// System.Text.StringBuilder is bound to this growable UTF-16 buffer instead of
// the BCL's StringBuilder IL. Append/insert/remove/replace and the final
// ToString materialization match real .NET, capacity growth included.
#include "dn2cpp_core.h"

#include <cstdint>
#include <cstring>

// ---- System.Text.StringBuilder ----

// Each static type-info bakes its interned Type companion in (lock-free typeof/GetType).
extern const Dn2CppType dn2cpp_stringbuilder_type_obj;
const Dn2CppTypeInfo dn2cpp_stringbuilder_type =
    dn2cpp_ti_with_typeobject({ "System.Text.StringBuilder", nullptr, (int32_t)sizeof(Dn2CppStringBuilder), nullptr, nullptr, 0 }, &dn2cpp_stringbuilder_type_obj);
const Dn2CppType dn2cpp_stringbuilder_type_obj = { { &dn2cpp_type_type }, &dn2cpp_stringbuilder_type };

static void dn2cpp_sb_ensure(Dn2CppStringBuilder* sb, int32_t needed)
{
    if (sb->length + needed <= sb->capacity)
        return;
    // Grow like .NET: double the capacity, or jump straight to the needed size if
    // that is larger (probe-confirmed, e.g. new(4)+8 -> 8, new(10)+15 -> 20). No
    // floor: the ctors seed a non-zero Capacity, so this never starts from zero,
    // and a non-conformant floor would make sb.Capacity diverge from real .NET and
    // break the P/Invoke buffer-length contract.
    int32_t newCap = sb->capacity * 2;
    if (newCap < sb->length + needed)
        newCap = sb->length + needed;
    // char16_t storage — no managed pointers, so allocate unscanned.
    auto* newBuf = static_cast<char16_t*>(dn2cpp_alloc_atomic(static_cast<size_t>(newCap) * sizeof(char16_t)));
    if (sb->buf != nullptr && sb->length > 0)
        std::memcpy(newBuf, sb->buf, static_cast<size_t>(sb->length) * sizeof(char16_t));
    sb->buf = newBuf;
    sb->capacity = newCap;
}

// new StringBuilder(capacity): allocate the buffer up front so sb.Capacity
// matches .NET. A non-positive request (new()/new(0)) falls back to .NET's
// default capacity of 16.
Dn2CppStringBuilder* dn2cpp_sb_new_cap(int32_t capacity)
{
    auto* sb = static_cast<Dn2CppStringBuilder*>(dn2cpp_alloc(sizeof(Dn2CppStringBuilder)));
    sb->type = &dn2cpp_stringbuilder_type;
    int32_t cap = capacity > 0 ? capacity : 16;
    sb->buf = static_cast<char16_t*>(dn2cpp_alloc_atomic(static_cast<size_t>(cap) * sizeof(char16_t)));
    sb->length = 0;
    sb->capacity = cap;
    return sb;
}

Dn2CppStringBuilder* dn2cpp_sb_new()
{
    return dn2cpp_sb_new_cap(16);
}

Dn2CppStringBuilder* dn2cpp_sb_append_str(Dn2CppStringBuilder* sb, Dn2CppString* s)
{
    if (s != nullptr && s->length > 0)
    {
        dn2cpp_sb_ensure(sb, s->length);
        std::memcpy(sb->buf + sb->length, s->chars, static_cast<size_t>(s->length) * sizeof(char16_t));
        sb->length += s->length;
    }
    return sb;
}

// new StringBuilder(string): .NET seeds Capacity = max(16, value.Length), so
// allocate that up front (not via the growth path, which would round up by
// doubling and overshoot) and copy the content in.
Dn2CppStringBuilder* dn2cpp_sb_new_str(Dn2CppString* s)
{
    int32_t len = s != nullptr ? s->length : 0;
    return dn2cpp_sb_append_str(dn2cpp_sb_new_cap(len > 16 ? len : 16), s);
}

// new StringBuilder(string, capacity): .NET requires capacity >= value.Length;
// model the buffer as max(capacity, len) so the content always fits.
Dn2CppStringBuilder* dn2cpp_sb_new_str_cap(Dn2CppString* s, int32_t capacity)
{
    int32_t len = s != nullptr ? s->length : 0;
    return dn2cpp_sb_append_str(dn2cpp_sb_new_cap(capacity > len ? capacity : len), s);
}

Dn2CppStringBuilder* dn2cpp_sb_append_char(Dn2CppStringBuilder* sb, char16_t c)
{
    dn2cpp_sb_ensure(sb, 1);
    sb->buf[sb->length++] = c;
    return sb;
}

// Environment.NewLine: "\r\n" on Windows, "\n" elsewhere — same convention as
// the Console.WriteLine terminator (dn2cpp_write_newline in dn2cpp_runtime.cpp),
// kept as its own small helper here since StringBuilder's buffer is char16_t
// (not a std::FILE*) and a bare append_char('\n') cannot express a 2-char
// terminator.
Dn2CppStringBuilder* dn2cpp_sb_append_newline(Dn2CppStringBuilder* sb)
{
#ifdef _WIN32
    return dn2cpp_sb_append_char(dn2cpp_sb_append_char(sb, u'\r'), u'\n');
#else
    return dn2cpp_sb_append_char(sb, u'\n');
#endif
}

Dn2CppString* dn2cpp_sb_tostring(Dn2CppStringBuilder* sb)
{
    char16_t* dst;
    Dn2CppString* s = dn2cpp_string_alloc(&dst, sb->length);
    if (sb->length > 0)
        std::memcpy(dst, sb->buf, static_cast<size_t>(sb->length) * sizeof(char16_t));
    return s; // no reset: ToString may be called repeatedly
}

int32_t dn2cpp_sb_length(Dn2CppStringBuilder* sb)
{
    return sb->length;
}

int32_t dn2cpp_sb_capacity(Dn2CppStringBuilder* sb)
{
    return sb->capacity;
}

void dn2cpp_sb_copy_to(Dn2CppStringBuilder* sb, int32_t sourceIndex, Dn2CppArrayN* dest,
                       int32_t destinationIndex, int32_t count)
{
    if (dest == nullptr)
        dn2cpp_throw_argument_null();
    // Negatives are ArgumentOutOfRange; the source/destination overruns are
    // ArgumentException, matching the BCL's StringBuilder.CopyTo.
    if (sourceIndex < 0 || count < 0 || destinationIndex < 0)
        dn2cpp_throw_argument_out_of_range();
    if (sourceIndex + count > sb->length)
        dn2cpp_throw_argument(); // .NET: ArgumentException, source index + count > Length
    if (destinationIndex + count > dest->length)
        dn2cpp_throw_argument(); // .NET: ArgumentException, destination array too short
    char16_t* dd = reinterpret_cast<char16_t*>(dest->data);
    for (int32_t i = 0; i < count; i++)
        dd[destinationIndex + i] = sb->buf[sourceIndex + i];
}

Dn2CppStringBuilder* dn2cpp_sb_clear(Dn2CppStringBuilder* sb)
{
    sb->length = 0;
    return sb;
}

// Insert `value` at `index` (0..length), shifting the tail right.
Dn2CppStringBuilder* dn2cpp_sb_insert_str(Dn2CppStringBuilder* sb, int32_t index, Dn2CppString* value)
{
    if (index < 0 || index > sb->length)
        dn2cpp_throw_argument_out_of_range();
    if (value == nullptr || value->length == 0)
        return sb;
    int32_t vlen = value->length;
    dn2cpp_sb_ensure(sb, vlen);
    std::memmove(sb->buf + index + vlen, sb->buf + index,
                 static_cast<size_t>(sb->length - index) * sizeof(char16_t));
    std::memcpy(sb->buf + index, value->chars, static_cast<size_t>(vlen) * sizeof(char16_t));
    sb->length += vlen;
    return sb;
}

Dn2CppStringBuilder* dn2cpp_sb_insert_char(Dn2CppStringBuilder* sb, int32_t index, char16_t c)
{
    if (index < 0 || index > sb->length)
        dn2cpp_throw_argument_out_of_range();
    dn2cpp_sb_ensure(sb, 1);
    std::memmove(sb->buf + index + 1, sb->buf + index,
                 static_cast<size_t>(sb->length - index) * sizeof(char16_t));
    sb->buf[index] = c;
    sb->length += 1;
    return sb;
}

// Remove `count` chars at `start`, shifting the tail left.
Dn2CppStringBuilder* dn2cpp_sb_remove(Dn2CppStringBuilder* sb, int32_t start, int32_t count)
{
    if (start < 0 || count < 0 || start > sb->length - count)
        dn2cpp_throw_argument_out_of_range();
    std::memmove(sb->buf + start, sb->buf + start + count,
                 static_cast<size_t>(sb->length - start - count) * sizeof(char16_t));
    sb->length -= count;
    return sb;
}

Dn2CppStringBuilder* dn2cpp_sb_replace_char(Dn2CppStringBuilder* sb, char16_t oldc, char16_t newc)
{
    for (int32_t i = 0; i < sb->length; i++)
        if (sb->buf[i] == oldc)
            sb->buf[i] = newc;
    return sb;
}

// Replace every (non-overlapping, left-to-right) occurrence of `oldValue` with
// `newValue` whose match lies fully inside [startIndex, startIndex + count)
// (probe-confirmed: a match crossing the range end is left alone). Rebuilds into
// a fresh buffer since the two lengths can differ. null/empty oldValue throws
// like the BCL (ArgumentNull/ArgumentException, checked before the range).
Dn2CppStringBuilder* dn2cpp_sb_replace_str_range(Dn2CppStringBuilder* sb, Dn2CppString* oldValue,
                                                 Dn2CppString* newValue, int32_t startIndex, int32_t count)
{
    if (oldValue == nullptr)
        dn2cpp_throw_argument_null();
    if (oldValue->length == 0)
        dn2cpp_throw_argument();
    if (startIndex < 0 || count < 0 || startIndex > sb->length - count)
        dn2cpp_throw_argument_out_of_range();
    int32_t n = sb->length, ol = oldValue->length;
    int32_t rangeEnd = startIndex + count;
    int32_t nl = newValue == nullptr ? 0 : newValue->length;
    size_t obytes = static_cast<size_t>(ol) * sizeof(char16_t);
    int32_t occ = 0;
    for (int32_t i = startIndex; i + ol <= rangeEnd; )
    {
        if (std::memcmp(sb->buf + i, oldValue->chars, obytes) == 0)
        {
            occ++;
            i += ol;
        }
        else
        {
            i++;
        }
    }
    if (occ == 0)
        return sb;
    int32_t newLen = n + occ * (nl - ol);
    auto* dst = static_cast<char16_t*>(dn2cpp_alloc_atomic(static_cast<size_t>(newLen > 0 ? newLen : 1) * sizeof(char16_t)));
    int32_t w = 0;
    for (int32_t i = 0; i < n; )
    {
        if (i >= startIndex && i + ol <= rangeEnd && std::memcmp(sb->buf + i, oldValue->chars, obytes) == 0)
        {
            if (nl > 0)
            {
                std::memcpy(dst + w, newValue->chars, static_cast<size_t>(nl) * sizeof(char16_t));
                w += nl;
            }
            i += ol;
        }
        else
        {
            dst[w++] = sb->buf[i++];
        }
    }
    sb->buf = dst;
    sb->length = newLen;
    sb->capacity = newLen > 0 ? newLen : 1;
    return sb;
}

Dn2CppStringBuilder* dn2cpp_sb_replace_str(Dn2CppStringBuilder* sb, Dn2CppString* oldValue, Dn2CppString* newValue)
{
    return dn2cpp_sb_replace_str_range(sb, oldValue, newValue, 0, sb->length);
}

// Replace(char, char, startIndex, count): substitute within the range only.
Dn2CppStringBuilder* dn2cpp_sb_replace_char_range(Dn2CppStringBuilder* sb, char16_t oldc, char16_t newc,
                                                  int32_t startIndex, int32_t count)
{
    if (startIndex < 0 || count < 0 || startIndex > sb->length - count)
        dn2cpp_throw_argument_out_of_range();
    for (int32_t i = startIndex; i < startIndex + count; i++)
        if (sb->buf[i] == oldc)
            sb->buf[i] = newc;
    return sb;
}

// Append `n` raw UTF-16 code units. The pointer must not alias sb's own buffer
// (the growth path may reallocate it) — the self-append case goes through
// dn2cpp_sb_append_sb, which re-reads the buffer after growing.
Dn2CppStringBuilder* dn2cpp_sb_append_chars(Dn2CppStringBuilder* sb, const char16_t* chars, int32_t count)
{
    if (chars == nullptr || count <= 0)
        return sb;
    dn2cpp_sb_ensure(sb, count);
    std::memcpy(sb->buf + sb->length, chars, static_cast<size_t>(count) * sizeof(char16_t));
    sb->length += count;
    return sb;
}

// The shared (char[] value, int startIndex, int charCount) validation for the
// Append/Insert array-slice overloads, materialized as a string: a null array
// is valid only as the (null, 0, 0) no-op (ArgumentNullException otherwise,
// probe-confirmed even for (null, 1, 0)); a bad slice is ArgumentOutOfRange.
Dn2CppString* dn2cpp_sb_char_arr_str(Dn2CppArrayN* arr, int32_t startIndex, int32_t charCount)
{
    if (arr == nullptr)
    {
        if (startIndex == 0 && charCount == 0)
            return dn2cpp_string_from_chars(nullptr, 0);
        dn2cpp_throw_argument_null();
    }
    if (startIndex < 0 || charCount < 0 || startIndex > arr->length - charCount)
        dn2cpp_throw_argument_out_of_range();
    return dn2cpp_string_from_chars(reinterpret_cast<const char16_t*>(arr->data) + startIndex, charCount);
}

// Append(StringBuilder): a null value appends nothing. Self-append is legal
// (doubles the content): the growth realloc happens first, so the re-read
// source pointer is the fresh buffer holding the pre-append content.
Dn2CppStringBuilder* dn2cpp_sb_append_sb(Dn2CppStringBuilder* sb, Dn2CppStringBuilder* value)
{
    if (value == nullptr || value->length == 0)
        return sb;
    int32_t n = value->length;
    dn2cpp_sb_ensure(sb, n);
    std::memcpy(sb->buf + sb->length, value->buf, static_cast<size_t>(n) * sizeof(char16_t));
    sb->length += n;
    return sb;
}

// Append(StringBuilder, startIndex, count). .NET validates the signs before the
// null value (null is then valid only as the (null, 0, 0) no-op).
Dn2CppStringBuilder* dn2cpp_sb_append_sb_range(Dn2CppStringBuilder* sb, Dn2CppStringBuilder* value,
                                               int32_t startIndex, int32_t count)
{
    if (startIndex < 0 || count < 0)
        dn2cpp_throw_argument_out_of_range();
    if (value == nullptr)
    {
        if (startIndex == 0 && count == 0)
            return sb;
        dn2cpp_throw_argument_null();
    }
    if (count == 0)
        return sb;
    if (count > value->length - startIndex)
        dn2cpp_throw_argument_out_of_range();
    dn2cpp_sb_ensure(sb, count);
    std::memcpy(sb->buf + sb->length, value->buf + startIndex, static_cast<size_t>(count) * sizeof(char16_t));
    sb->length += count;
    return sb;
}

// Insert(index, value, count): `count` repetitions of `value` at `index`.
// count is validated before the index; a null/empty value or count 0 is a
// no-op (after both validations, probe-confirmed).
Dn2CppStringBuilder* dn2cpp_sb_insert_str_count(Dn2CppStringBuilder* sb, int32_t index,
                                                Dn2CppString* value, int32_t count)
{
    if (count < 0)
        dn2cpp_throw_argument_out_of_range();
    if (index < 0 || index > sb->length)
        dn2cpp_throw_argument_out_of_range();
    if (value == nullptr || value->length == 0 || count == 0)
        return sb;
    // Real .NET raises a CATCHABLE OutOfMemoryException here, not the
    // ArgumentOutOfRangeException of the two guards above: StringBuilder.Insert
    // computes value.Length * count and calls the OOM throw helper on overflow.
    // It is an overflow, not an allocation failure — nothing has been asked of
    // the allocator yet — which is why this one can be a managed throw while
    // dn2cpp_gc.cpp's allocation failures cannot (minting the exception object
    // needs the allocator that just refused).
    int64_t total64 = static_cast<int64_t>(value->length) * count;
    if (total64 > INT32_MAX - sb->length)
        dn2cpp_throw_out_of_memory();
    int32_t total = static_cast<int32_t>(total64);
    dn2cpp_sb_ensure(sb, total);
    std::memmove(sb->buf + index + total, sb->buf + index,
                 static_cast<size_t>(sb->length - index) * sizeof(char16_t));
    for (int32_t k = 0; k < count; k++)
        std::memcpy(sb->buf + index + k * value->length, value->chars,
                    static_cast<size_t>(value->length) * sizeof(char16_t));
    sb->length += total;
    return sb;
}

// Length setter: truncate, or grow zero-('\0')-padded. A shrink-then-regrow
// yields zeros too (probe-confirmed), which the grow-path memset provides
// since the shrink leaves the old content in place.
void dn2cpp_sb_set_length(Dn2CppStringBuilder* sb, int32_t value)
{
    if (value < 0)
        dn2cpp_throw_argument_out_of_range();
    if (value > sb->length)
    {
        dn2cpp_sb_ensure(sb, value - sb->length);
        std::memset(sb->buf + sb->length, 0,
                    static_cast<size_t>(value - sb->length) * sizeof(char16_t));
    }
    sb->length = value;
}

// The indexer: the getter throws IndexOutOfRangeException, the setter
// ArgumentOutOfRangeException (asymmetric in .NET, probe-confirmed).
char16_t dn2cpp_sb_get_char(Dn2CppStringBuilder* sb, int32_t index)
{
    if (static_cast<uint32_t>(index) >= static_cast<uint32_t>(sb->length))
        dn2cpp_throw_index_out_of_range();
    return sb->buf[index];
}

void dn2cpp_sb_set_char(Dn2CppStringBuilder* sb, int32_t index, char16_t value)
{
    if (static_cast<uint32_t>(index) >= static_cast<uint32_t>(sb->length))
        dn2cpp_throw_argument_out_of_range();
    sb->buf[index] = value;
}

// EnsureCapacity: grow-only, to EXACTLY the requested capacity (probe: 16 ->
// EnsureCapacity(17) reports 17, not a doubling), returning the new capacity.
int32_t dn2cpp_sb_ensure_capacity(Dn2CppStringBuilder* sb, int32_t capacity)
{
    if (capacity < 0)
        dn2cpp_throw_argument_out_of_range();
    if (capacity > sb->capacity)
    {
        auto* newBuf = static_cast<char16_t*>(dn2cpp_alloc_atomic(static_cast<size_t>(capacity) * sizeof(char16_t)));
        if (sb->buf != nullptr && sb->length > 0)
            std::memcpy(newBuf, sb->buf, static_cast<size_t>(sb->length) * sizeof(char16_t));
        sb->buf = newBuf;
        sb->capacity = capacity;
    }
    return sb->capacity;
}

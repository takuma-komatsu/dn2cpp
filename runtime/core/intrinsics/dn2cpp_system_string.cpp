// dn2cpp_system_string.cpp — System.String members, numeric parsing, and
// composite formatting intrinsics.
//
// The transpiler intercepts the System.String instance surface (substring, trim,
// pad, replace, split, index/contains, case, …), the invariant numeric parsers
// (int/long/double/float Parse/TryParse), and String.Format composite formatting,
// binding them to these routines instead of the BCL IL. Behavior matches real
// .NET. The lower-level string primitives (alloc, from_*, to_utf8) live in
// dn2cpp_string_core.cpp.
#include "dn2cpp_core.h"

#include <cstdint>
#include <cstring>
#include <cstdlib>
#include <cmath>
#include <mutex>
#include <string_view>
#include <unordered_map>
#include <vector>

// ============================ String members / parsing =====================
// ---- common String members ----

Dn2CppString* dn2cpp_str_substring(Dn2CppString* s, int32_t start, int32_t length)
{
    if (s == nullptr)
        dn2cpp_throw_null_reference();
    // The four refusals String.Substring(int,int) makes, each with real .NET's message
    // and paramName. The order is .NET's and it is observable: Substring(5, 0) on "abc"
    // takes the third, not the fourth.
    if (start < 0)
        dn2cpp_throw_argument_out_of_range_value(DN2CPP_SR_MUST_BE_NON_NEGATIVE, "startIndex",
            dn2cpp_format_int(start, 4, nullptr));
    if (length < 0)
        dn2cpp_throw_argument_out_of_range_value(DN2CPP_SR_MUST_BE_NON_NEGATIVE, "length",
            dn2cpp_format_int(length, 4, nullptr));
    if (start > s->length)
        dn2cpp_throw_argument_out_of_range_param(DN2CPP_SR_START_INDEX_LARGER_THAN_LENGTH,
            "startIndex");
    if (start > s->length - length)
        dn2cpp_throw_argument_out_of_range_param(DN2CPP_SR_INDEX_LENGTH, "length");
    char16_t* buf;
    Dn2CppString* r = dn2cpp_string_alloc(&buf, length);
    std::memcpy(buf, s->chars + start, static_cast<size_t>(length) * sizeof(char16_t));
    return r;
}

// Substring(startIndex): the length is `Length - startIndex`. The subtraction lives
// HERE, not at the call site, because it reads the receiver: spliced in by the emitter
// it would dereference a null string ahead of the null check — a SIGSEGV where .NET
// throws a catchable NullReferenceException. Same reasoning for the four siblings below.
Dn2CppString* dn2cpp_str_substring_to_end(Dn2CppString* s, int32_t start)
{
    if (s == nullptr)
        dn2cpp_throw_null_reference();
    return dn2cpp_str_substring(s, start, s->length - start);
}

// PadLeft/PadRight: widen to totalWidth with `pad`; widths <= length return the
// string unchanged (immutable, so reuse it). Negative width is invalid.
Dn2CppString* dn2cpp_str_pad(Dn2CppString* s, int32_t totalWidth, char16_t pad, int32_t padLeft)
{
    if (s == nullptr)
        dn2cpp_throw_null_reference();
    if (totalWidth < 0)
        dn2cpp_throw_argument_out_of_range();
    if (totalWidth <= s->length)
        return s;
    int32_t fill = totalWidth - s->length;
    char16_t* buf;
    Dn2CppString* r = dn2cpp_string_alloc(&buf, totalWidth);
    if (padLeft)
    {
        for (int32_t i = 0; i < fill; i++)
            buf[i] = pad;
        std::memcpy(buf + fill, s->chars, static_cast<size_t>(s->length) * sizeof(char16_t));
    }
    else
    {
        std::memcpy(buf, s->chars, static_cast<size_t>(s->length) * sizeof(char16_t));
        for (int32_t i = 0; i < fill; i++)
            buf[s->length + i] = pad;
    }
    return r;
}

// Remove `count` chars at `start` (the caller passes count = length-start for the
// single-argument Remove(startIndex) overload).
Dn2CppString* dn2cpp_str_remove(Dn2CppString* s, int32_t start, int32_t count)
{
    if (s == nullptr)
        dn2cpp_throw_null_reference();
    if (start < 0 || count < 0 || start > s->length - count)
        dn2cpp_throw_argument_out_of_range();
    int32_t newLen = s->length - count;
    char16_t* buf;
    Dn2CppString* r = dn2cpp_string_alloc(&buf, newLen);
    std::memcpy(buf, s->chars, static_cast<size_t>(start) * sizeof(char16_t));
    std::memcpy(buf + start, s->chars + start + count,
                static_cast<size_t>(s->length - start - count) * sizeof(char16_t));
    return r;
}

// Remove(startIndex): every char from `start` on (receiver read stays here, as above).
Dn2CppString* dn2cpp_str_remove_to_end(Dn2CppString* s, int32_t start)
{
    if (s == nullptr)
        dn2cpp_throw_null_reference();
    return dn2cpp_str_remove(s, start, s->length - start);
}

// Insert `value` at `start`.
Dn2CppString* dn2cpp_str_insert(Dn2CppString* s, int32_t start, Dn2CppString* value)
{
    if (s == nullptr)
        dn2cpp_throw_null_reference();
    if (value == nullptr)
        dn2cpp_throw_argument_null();
    if (start < 0 || start > s->length)
        dn2cpp_throw_argument_out_of_range();
    int32_t newLen = s->length + value->length;
    char16_t* buf;
    Dn2CppString* r = dn2cpp_string_alloc(&buf, newLen);
    std::memcpy(buf, s->chars, static_cast<size_t>(start) * sizeof(char16_t));
    std::memcpy(buf + start, value->chars, static_cast<size_t>(value->length) * sizeof(char16_t));
    std::memcpy(buf + start + value->length, s->chars + start,
                static_cast<size_t>(s->length - start) * sizeof(char16_t));
    return r;
}

int32_t dn2cpp_str_indexof_char(Dn2CppString* s, char16_t c, int32_t start)
{
    if (s == nullptr)
        dn2cpp_throw_null_reference();
    for (int32_t i = start; i < s->length; i++)
        if (s->chars[i] == c)
            return i;
    return -1;
}

// Two-phase ordinal substring scan over raw code units: candidate positions
// are located with char_traits<char16_t>::find (a vectorized memchr-class
// scan for the needle's first unit), each confirmed with one memcmp of the
// remainder — the same bulk-compare shape as the memcmp'd StartsWith/EndsWith,
// replacing the naive per-char O(n·m) nested loop. Returns the leftmost start
// index in [0, n - m], -1 when absent; the caller handles m == 0.
static int32_t dn2cpp_chars_indexof_ordinal(const char16_t* hay, int32_t n,
                                            const char16_t* needle, int32_t m)
{
    if (m > n)
        return -1;
    const char16_t first = needle[0];
    const char16_t* const last = hay + (n - m); // last valid start position
    const char16_t* p = hay;
    while (p <= last)
    {
        const char16_t* hit = std::char_traits<char16_t>::find(
            p, static_cast<size_t>(last - p) + 1, first);
        if (hit == nullptr)
            return -1;
        if (m == 1 || std::memcmp(hit + 1, needle + 1,
                (static_cast<size_t>(m) - 1) * sizeof(char16_t)) == 0)
            return static_cast<int32_t>(hit - hay);
        p = hit + 1;
    }
    return -1;
}

int32_t dn2cpp_str_indexof_str(Dn2CppString* s, Dn2CppString* sub)
{
    if (s == nullptr)
        dn2cpp_throw_null_reference();
    if (sub == nullptr)
        dn2cpp_throw_argument_null();
    // .NET: an empty search string is found at index 0.
    if (sub->length == 0)
        return 0;
    return dn2cpp_chars_indexof_ordinal(s->chars, s->length, sub->chars, sub->length);
}

int32_t dn2cpp_str_startswith(Dn2CppString* s, Dn2CppString* prefix)
{
    if (s == nullptr)
        dn2cpp_throw_null_reference();
    if (prefix == nullptr)
        dn2cpp_throw_argument_null();
    if (prefix->length > s->length)
        return 0;
    // Ordinal prefix test: a bulk memcmp (byte-equality == code-unit equality) in
    // place of the per-unit loop.
    return std::memcmp(s->chars, prefix->chars,
        static_cast<size_t>(prefix->length) * sizeof(char16_t)) == 0 ? 1 : 0;
}

int32_t dn2cpp_str_endswith(Dn2CppString* s, Dn2CppString* suffix)
{
    if (s == nullptr)
        dn2cpp_throw_null_reference();
    if (suffix == nullptr)
        dn2cpp_throw_argument_null();
    if (suffix->length > s->length)
        return 0;
    int32_t off = s->length - suffix->length;
    return std::memcmp(s->chars + off, suffix->chars,
        static_cast<size_t>(suffix->length) * sizeof(char16_t)) == 0 ? 1 : 0;
}

// The char / (string, StringComparison) / last-index overloads. char overloads
// compare the single edge code unit; the comparison overloads support Ordinal and
// OrdinalIgnoreCase (the exact BMP fold in dn2cpp_ordinal_casing.cpp), and the
// culture-sensitive values fold onto those two via dn2cpp_str_comparison_fold.

// THE single StringComparison map: every helper that dispatches on a
// StringComparison operand folds it through here first (this file's *_cmp
// family, dn2cpp_str_compare/_sub in dn2cpp_system_globalization.cpp, and
// the emitted GetHashCode(ReadOnlySpan<char>, StringComparison) dispatch).
// Do not re-derive the mapping at a call site.
//
// CurrentCulture (0) / CurrentCultureIgnoreCase (1) / InvariantCulture (2) /
// InvariantCultureIgnoreCase (3) map onto Ordinal (4) / OrdinalIgnoreCase (5),
// preserving the IgnoreCase bit (all four are odd exactly when their target is).
// That is the invariant-globalization posture, matching real .NET under
// InvariantGlobalization=true — which dn2cpp already bakes in by folding
// GlobalizationMode.get_Invariant to true, so the transpiled BCL span overloads
// answer 0..3 ordinally and this mapping keeps the intrinsics in agreement.
// CurrentCulture folds for the same reason: dn2cpp's current culture IS the
// invariant culture (name ""), so both families denote the same collation.
//
// INTENTIONAL DIVERGENCE from ICU-backed .NET, whose invariant collation is
// linguistic: for non-ASCII input it can disagree with Ordinal ("ä" sorts before
// "b" culturally but after "z" ordinally; "Straße" == "STRASSE" under
// InvariantCultureIgnoreCase, not under OrdinalIgnoreCase). ASCII input agrees
// except for ordering (linguistic orders "a" < "B"; ordinal compares code units).
//
// An out-of-range value ((uint)v > 5) throws a catchable ArgumentException,
// .NET's String.CheckStringComparison contract.
int32_t dn2cpp_str_comparison_fold(int32_t comparisonType)
{
    if ((uint32_t)comparisonType > 5u)
        dn2cpp_throw_argument();
    return 4 | (comparisonType & 1);
}

int32_t dn2cpp_str_startswith_char(Dn2CppString* s, char16_t c)
{
    if (s == nullptr)
        dn2cpp_throw_null_reference();
    return (s->length > 0 && s->chars[0] == c) ? 1 : 0;
}

int32_t dn2cpp_str_endswith_char(Dn2CppString* s, char16_t c)
{
    if (s == nullptr)
        dn2cpp_throw_null_reference();
    return (s->length > 0 && s->chars[s->length - 1] == c) ? 1 : 0;
}

int32_t dn2cpp_str_lastindexof_char(Dn2CppString* s, char16_t c)
{
    if (s == nullptr)
        dn2cpp_throw_null_reference();
    for (int32_t i = s->length - 1; i >= 0; i--)
        if (s->chars[i] == c)
            return i;
    return -1;
}

// Per-char worker: takes an ALREADY-FOLDED comparison (Ordinal == 4,
// OrdinalIgnoreCase == 5 — every public entry runs dn2cpp_str_comparison_fold
// first); any other value here is an internal invariant violation.
//
// Stays an abort: the unfolded value cannot come from a caller, since the fold has
// already mapped every StringComparison the public entries accept and rejected the
// rest with a managed throw. Reaching here means a new entry point was added without
// the fold — no input can cause it and none can avoid it.
static int dn2cpp_str_char_eq_cmp(char16_t a, char16_t b, int32_t comparison)
{
    if (comparison == 5) // OrdinalIgnoreCase
        return dn2cpp_ordinal_upper(a) == dn2cpp_ordinal_upper(b);
    if (comparison == 4) // Ordinal
        return a == b;
    dn2cpp_fail("internal: unfolded StringComparison reached dn2cpp_str_char_eq_cmp");
    return 0;
}

int32_t dn2cpp_str_startswith_cmp(Dn2CppString* s, Dn2CppString* prefix, int32_t comparison)
{
    comparison = dn2cpp_str_comparison_fold(comparison);
    if (s == nullptr)
        dn2cpp_throw_null_reference();
    if (prefix == nullptr)
        dn2cpp_throw_argument_null();
    if (prefix->length > s->length)
        return 0;
    for (int32_t i = 0; i < prefix->length; i++)
        if (!dn2cpp_str_char_eq_cmp(s->chars[i], prefix->chars[i], comparison))
            return 0;
    return 1;
}

int32_t dn2cpp_str_endswith_cmp(Dn2CppString* s, Dn2CppString* suffix, int32_t comparison)
{
    comparison = dn2cpp_str_comparison_fold(comparison);
    if (s == nullptr)
        dn2cpp_throw_null_reference();
    if (suffix == nullptr)
        dn2cpp_throw_argument_null();
    if (suffix->length > s->length)
        return 0;
    int32_t off = s->length - suffix->length;
    for (int32_t i = 0; i < suffix->length; i++)
        if (!dn2cpp_str_char_eq_cmp(s->chars[off + i], suffix->chars[i], comparison))
            return 0;
    return 1;
}

int32_t dn2cpp_str_contains_cmp(Dn2CppString* s, Dn2CppString* sub, int32_t comparison)
{
    comparison = dn2cpp_str_comparison_fold(comparison);
    if (s == nullptr)
        dn2cpp_throw_null_reference();
    if (sub == nullptr)
        dn2cpp_throw_argument_null();
    if (sub->length == 0)
        return 1;
    for (int32_t i = 0; i + sub->length <= s->length; i++)
    {
        int32_t j = 0;
        while (j < sub->length && dn2cpp_str_char_eq_cmp(s->chars[i + j], sub->chars[j], comparison))
            j++;
        if (j == sub->length)
            return 1;
    }
    return 0;
}

int32_t dn2cpp_str_indexof_cmp(Dn2CppString* s, Dn2CppString* sub, int32_t comparison)
{
    comparison = dn2cpp_str_comparison_fold(comparison);
    if (s == nullptr)
        dn2cpp_throw_null_reference();
    if (sub == nullptr)
        dn2cpp_throw_argument_null();
    // .NET: an empty search string is found at index 0.
    if (sub->length == 0)
        return 0;
    if (comparison == 4) // Ordinal: the two-phase bulk scan, identical results
        return dn2cpp_chars_indexof_ordinal(s->chars, s->length, sub->chars, sub->length);
    for (int32_t i = 0; i + sub->length <= s->length; i++)
    {
        int32_t j = 0;
        while (j < sub->length && dn2cpp_str_char_eq_cmp(s->chars[i + j], sub->chars[j], comparison))
            j++;
        if (j == sub->length)
            return i;
    }
    return -1;
}

int32_t dn2cpp_str_indexof_str_cmp(Dn2CppString* s, Dn2CppString* sub, int32_t startIndex, int32_t comparison)
{
    comparison = dn2cpp_str_comparison_fold(comparison);
    if (s == nullptr)
        dn2cpp_throw_null_reference();
    if (sub == nullptr)
        dn2cpp_throw_argument_null(); // catchable, like .NET's null-needle check
    if (startIndex < 0 || startIndex > s->length)
        dn2cpp_throw_argument_out_of_range(); // catchable, like .NET's range check
    // .NET: an empty search string is found at the (clamped) start index.
    if (sub->length == 0)
        return startIndex;
    if (comparison == 4) // Ordinal: the two-phase bulk scan, identical results
    {
        int32_t r = dn2cpp_chars_indexof_ordinal(s->chars + startIndex, s->length - startIndex,
                                                 sub->chars, sub->length);
        return r < 0 ? -1 : startIndex + r;
    }
    for (int32_t i = startIndex; i + sub->length <= s->length; i++)
    {
        int32_t j = 0;
        while (j < sub->length && dn2cpp_str_char_eq_cmp(s->chars[i + j], sub->chars[j], comparison))
            j++;
        if (j == sub->length)
            return i;
    }
    return -1;
}

// LastIndexOf(string, startIndex, count[, comparison]) — the .NET contract:
// the `count` character positions ending at `startIndex` form the search
// range and a match must lie ENTIRELY within it (a match may end at, but not
// after, startIndex). startIndex == Length is tolerated by adjusting both
// startIndex and a positive count down one (the historic BCL allowance); an
// empty needle reports startIndex + 1 after that adjustment. An empty source
// with startIndex -1 or 0 short-circuits before range validation, again
// matching the real CompareInfo.LastIndexOf.
int32_t dn2cpp_str_lastindexof_str_range(Dn2CppString* s, Dn2CppString* sub, int32_t startIndex,
                                         int32_t count, int32_t comparison)
{
    comparison = dn2cpp_str_comparison_fold(comparison);
    if (s == nullptr)
        dn2cpp_throw_null_reference();
    if (sub == nullptr)
        dn2cpp_throw_argument_null();
    if (s->length == 0 && (startIndex == -1 || startIndex == 0))
        return sub->length == 0 ? 0 : -1;
    if (startIndex < 0 || startIndex > s->length)
        dn2cpp_throw_argument_out_of_range();
    if (startIndex == s->length)
    {
        startIndex--;
        if (count > 0)
            count--;
    }
    if (count < 0 || startIndex - count + 1 < 0)
        dn2cpp_throw_argument_out_of_range();
    int32_t offset = startIndex - count + 1;
    // An empty pattern is "found" at the end of the searched range.
    if (sub->length == 0)
        return offset + count;
    for (int32_t i = offset + count - sub->length; i >= offset; i--)
    {
        int32_t j = 0;
        while (j < sub->length && dn2cpp_str_char_eq_cmp(s->chars[i + j], sub->chars[j], comparison))
            j++;
        if (j == sub->length)
            return i;
    }
    return -1;
}

int32_t dn2cpp_str_lastindexof_str_cmp(Dn2CppString* s, Dn2CppString* sub, int32_t startIndex, int32_t comparison)
{
    // The (value, startIndex) shape searches the startIndex + 1 positions
    // ending at startIndex, exactly .NET's LastIndexOf(value, startIndex,
    // startIndex + 1, comparison) delegation.
    return dn2cpp_str_lastindexof_str_range(s, sub, startIndex, startIndex + 1, comparison);
}

// Whole-string LastIndexOf(string[, comparison]) — an empty needle is found at
// Length (the .NET 5+ contract), otherwise the last full ordinal match.
int32_t dn2cpp_str_lastindexof_cmp(Dn2CppString* s, Dn2CppString* sub, int32_t comparison)
{
    comparison = dn2cpp_str_comparison_fold(comparison);
    if (s == nullptr)
        dn2cpp_throw_null_reference();
    if (sub == nullptr)
        dn2cpp_throw_argument_null();
    if (sub->length == 0)
        return s->length;
    for (int32_t i = s->length - sub->length; i >= 0; i--)
    {
        int32_t j = 0;
        while (j < sub->length && dn2cpp_str_char_eq_cmp(s->chars[i + j], sub->chars[j], comparison))
            j++;
        if (j == sub->length)
            return i;
    }
    return -1;
}

int32_t dn2cpp_str_lastindexof_str(Dn2CppString* s, Dn2CppString* sub)
{
    return dn2cpp_str_lastindexof_cmp(s, sub, 4);
}

// LastIndexOf(char, startIndex[, count]): backward scan of the `count`
// positions ending at `startIndex` (both inclusive). An empty source returns
// -1 before any range validation (so even startIndex -1 is tolerated there);
// a non-empty source requires startIndex < Length and 0 <= count <=
// startIndex + 1, throwing ArgumentOutOfRangeException otherwise — all
// exactly the .NET checks.
int32_t dn2cpp_str_lastindexof_char_range(Dn2CppString* s, char16_t c, int32_t start, int32_t count)
{
    if (s == nullptr)
        dn2cpp_throw_null_reference();
    if (s->length == 0)
        return -1;
    if (start < 0 || start >= s->length)
        dn2cpp_throw_argument_out_of_range();
    if (count < 0 || count > start + 1)
        dn2cpp_throw_argument_out_of_range();
    for (int32_t i = start; i > start - count; i--)
        if (s->chars[i] == c)
            return i;
    return -1;
}

// IndexOf(char, startIndex[, count]): forward scan of `count` positions from
// `start`. startIndex may equal Length (empty tail); bad start/count throws
// ArgumentOutOfRangeException like .NET.
int32_t dn2cpp_str_indexof_char_range(Dn2CppString* s, char16_t c, int32_t start, int32_t count)
{
    if (s == nullptr)
        dn2cpp_throw_null_reference();
    if (start < 0 || start > s->length)
        dn2cpp_throw_argument_out_of_range();
    if (count < 0 || count > s->length - start)
        dn2cpp_throw_argument_out_of_range();
    for (int32_t i = start; i < start + count; i++)
        if (s->chars[i] == c)
            return i;
    return -1;
}

// IndexOf(char, startIndex): the count is `Length - startIndex` — the subtraction
// reads the receiver, so it cannot happen at the call site (as above).
int32_t dn2cpp_str_indexof_char_to_end(Dn2CppString* s, char16_t c, int32_t start)
{
    if (s == nullptr)
        dn2cpp_throw_null_reference();
    return dn2cpp_str_indexof_char_range(s, c, start, s->length - start);
}

// IndexOf(char, StringComparison): Ordinal (4) / OrdinalIgnoreCase (5, exact
// BMP fold); like the other comparison helpers the culture-sensitive values
// fold onto those two (dn2cpp_str_comparison_fold).
int32_t dn2cpp_str_indexof_char_cmp(Dn2CppString* s, char16_t c, int32_t comparison)
{
    comparison = dn2cpp_str_comparison_fold(comparison);
    if (s == nullptr)
        dn2cpp_throw_null_reference();
    for (int32_t i = 0; i < s->length; i++)
        if (dn2cpp_str_char_eq_cmp(s->chars[i], c, comparison))
            return i;
    return -1;
}

// IndexOfAny / LastIndexOfAny over an explicit char set. A null set throws
// ArgumentNullException (before any range check); an empty set never matches.
// Range validation mirrors the corresponding single-char forms above.
int32_t dn2cpp_str_indexofany(Dn2CppString* s, const char16_t* set, int32_t setLen,
                              int32_t start, int32_t count)
{
    if (s == nullptr)
        dn2cpp_throw_null_reference();
    if (set == nullptr)
        dn2cpp_throw_argument_null();
    if (start < 0 || start > s->length)
        dn2cpp_throw_argument_out_of_range();
    if (count < 0 || count > s->length - start)
        dn2cpp_throw_argument_out_of_range();
    for (int32_t i = start; i < start + count; i++)
        for (int32_t k = 0; k < setLen; k++)
            if (s->chars[i] == set[k])
                return i;
    return -1;
}

int32_t dn2cpp_str_lastindexofany(Dn2CppString* s, const char16_t* set, int32_t setLen,
                                  int32_t start, int32_t count)
{
    if (s == nullptr)
        dn2cpp_throw_null_reference();
    if (set == nullptr)
        dn2cpp_throw_argument_null();
    if (s->length == 0)
        return -1;
    if (start < 0 || start >= s->length)
        dn2cpp_throw_argument_out_of_range();
    if (count < 0 || count > start + 1)
        dn2cpp_throw_argument_out_of_range();
    for (int32_t i = start; i > start - count; i--)
        for (int32_t k = 0; k < setLen; k++)
            if (s->chars[i] == set[k])
                return i;
    return -1;
}

// IndexOfAny(char[][, int startIndex]) / LastIndexOfAny(char[]): the range the bare
// overloads scan is the receiver's own, so it is computed here (as above). An EMPTY
// receiver yields last index -1 with count 0, which dn2cpp_str_lastindexofany's
// empty-source arm already answers -1 for before its range checks — so the degenerate
// pair passes through rather than being special-cased, keeping both overload families'
// validation order identical.
int32_t dn2cpp_str_indexofany_to_end(Dn2CppString* s, const char16_t* set, int32_t setLen,
                                     int32_t start)
{
    if (s == nullptr)
        dn2cpp_throw_null_reference();
    return dn2cpp_str_indexofany(s, set, setLen, start, s->length - start);
}

int32_t dn2cpp_str_lastindexofany_all(Dn2CppString* s, const char16_t* set, int32_t setLen)
{
    if (s == nullptr)
        dn2cpp_throw_null_reference();
    return dn2cpp_str_lastindexofany(s, set, setLen, s->length - 1, s->length);
}

// Invariant case mapping over the BMP (the exact per-code-unit
// dn2cpp_char_upper_invariant / dn2cpp_char_lower_invariant tables);
// supplementary-plane pairs pass through per code unit, matching .NET's
// invariant simple mapping. Same-instance fast path like .NET's
// ChangeCaseCommon: when every code unit is ASCII and already cased, the
// original string instance is returned (ReferenceEquals-visible) — a
// non-ASCII code unit bails to the allocating path even when it maps to
// itself, exactly like the real scan-then-ICU split.
Dn2CppString* dn2cpp_str_to_case(Dn2CppString* s, int32_t toUpper)
{
    if (s == nullptr)
        dn2cpp_throw_null_reference();
    auto mapc = [toUpper](char16_t c) {
        return toUpper != 0 ? dn2cpp_char_upper_invariant(c) : dn2cpp_char_lower_invariant(c);
    };
    int32_t first = 0;
    while (first < s->length && s->chars[first] < 0x80 && mapc(s->chars[first]) == s->chars[first])
        first++;
    if (first == s->length)
        return s;
    char16_t* buf;
    Dn2CppString* r = dn2cpp_string_alloc(&buf, s->length);
    if (first > 0)
        std::memcpy(buf, s->chars, static_cast<size_t>(first) * sizeof(char16_t));
    for (int32_t i = first; i < s->length; i++)
        buf[i] = mapc(s->chars[i]);
    return r;
}

Dn2CppString* dn2cpp_str_replace_char(Dn2CppString* s, char16_t oldc, char16_t newc)
{
    if (s == nullptr)
        dn2cpp_throw_null_reference();
    char16_t* buf;
    Dn2CppString* r = dn2cpp_string_alloc(&buf, s->length);
    for (int32_t i = 0; i < s->length; i++)
        buf[i] = s->chars[i] == oldc ? newc : s->chars[i];
    return r;
}

// String.Replace(string, string): ordinal, all occurrences, left-to-right,
// non-overlapping (.NET semantics: null oldValue -> ArgumentNullException,
// empty oldValue -> ArgumentException, null newValue -> treated as empty,
// no occurrence -> the original instance is returned unchanged).
Dn2CppString* dn2cpp_str_replace_str(Dn2CppString* s, Dn2CppString* oldValue, Dn2CppString* newValue)
{
    if (s == nullptr)
        dn2cpp_throw_null_reference();
    if (oldValue == nullptr)
        dn2cpp_throw_argument_null();
    if (oldValue->length == 0)
        dn2cpp_throw_argument();
    const char16_t* newChars = newValue != nullptr ? newValue->chars : nullptr;
    int32_t newLen = newValue != nullptr ? newValue->length : 0;

    // Count occurrences first so the result is allocated exactly once.
    int32_t count = 0;
    for (int32_t i = 0; i + oldValue->length <= s->length;)
    {
        if (std::memcmp(s->chars + i, oldValue->chars,
                        static_cast<size_t>(oldValue->length) * sizeof(char16_t)) == 0)
        {
            count++;
            i += oldValue->length;
        }
        else
        {
            i++;
        }
    }
    if (count == 0)
        return s;

    int64_t total64 = (int64_t)s->length + (int64_t)count * (newLen - oldValue->length);
    char16_t* buf;
    Dn2CppString* r = dn2cpp_string_alloc(&buf, (int32_t)total64);
    int32_t pos = 0;
    for (int32_t i = 0; i < s->length;)
    {
        if (i + oldValue->length <= s->length
            && std::memcmp(s->chars + i, oldValue->chars,
                           static_cast<size_t>(oldValue->length) * sizeof(char16_t)) == 0)
        {
            if (newLen > 0)
                std::memcpy(buf + pos, newChars, static_cast<size_t>(newLen) * sizeof(char16_t));
            pos += newLen;
            i += oldValue->length;
        }
        else
        {
            buf[pos++] = s->chars[i++];
        }
    }
    return r;
}

// String.Replace(string, string, StringComparison): Ordinal delegates to the ordinal
// scan above; OrdinalIgnoreCase folds each BMP code unit through dn2cpp_ordinal_upper,
// left-to-right and non-overlapping. Validation order is .NET's — the out-of-range
// comparison value (inside dn2cpp_str_comparison_fold) is checked BEFORE the
// null/empty oldValue checks. A no-match input returns the original instance.
Dn2CppString* dn2cpp_str_replace_str_cmp(Dn2CppString* s, Dn2CppString* oldValue,
                                         Dn2CppString* newValue, int32_t comparison)
{
    if (s == nullptr)
        dn2cpp_throw_null_reference();
    comparison = dn2cpp_str_comparison_fold(comparison);
    if (comparison == 4)
        return dn2cpp_str_replace_str(s, oldValue, newValue);
    if (oldValue == nullptr)
        dn2cpp_throw_argument_null();
    if (oldValue->length == 0)
        dn2cpp_throw_argument();
    const char16_t* newChars = newValue != nullptr ? newValue->chars : nullptr;
    int32_t newLen = newValue != nullptr ? newValue->length : 0;

    auto matchAt = [&](int32_t i) -> bool {
        for (int32_t j = 0; j < oldValue->length; j++)
            if (dn2cpp_ordinal_upper(s->chars[i + j]) != dn2cpp_ordinal_upper(oldValue->chars[j]))
                return false;
        return true;
    };

    // Count occurrences first so the result is allocated exactly once.
    int32_t count = 0;
    for (int32_t i = 0; i + oldValue->length <= s->length;)
    {
        if (matchAt(i))
        {
            count++;
            i += oldValue->length;
        }
        else
        {
            i++;
        }
    }
    if (count == 0)
        return s;

    int64_t total64 = (int64_t)s->length + (int64_t)count * (newLen - oldValue->length);
    char16_t* buf;
    Dn2CppString* r = dn2cpp_string_alloc(&buf, (int32_t)total64);
    int32_t pos = 0;
    for (int32_t i = 0; i < s->length;)
    {
        if (i + oldValue->length <= s->length && matchAt(i))
        {
            if (newLen > 0)
                std::memcpy(buf + pos, newChars, static_cast<size_t>(newLen) * sizeof(char16_t));
            pos += newLen;
            i += oldValue->length;
        }
        else
        {
            buf[pos++] = s->chars[i++];
        }
    }
    return r;
}

// String.Normalize/IsNormalized under the invariant-globalization model (real .NET
// with InvariantGlobalization=true): every string counts as already normalized, so
// Normalize returns the receiver and IsNormalized reports true once the form is
// validated (FormC=1 / FormD=2 / FormKC=5 / FormKD=6; anything else is a catchable
// ArgumentException). Divergence: a not-yet-composed input is returned as-is, so this
// is exact only for input already in the requested form.
Dn2CppString* dn2cpp_str_normalize(Dn2CppString* s, int32_t form)
{
    if (s == nullptr)
        dn2cpp_throw_null_reference();
    if (form != 1 && form != 2 && form != 5 && form != 6)
        dn2cpp_throw_argument();
    return s;
}

int32_t dn2cpp_str_is_normalized(Dn2CppString* s, int32_t form)
{
    if (s == nullptr)
        dn2cpp_throw_null_reference();
    if (form != 1 && form != 2 && form != 5 && form != 6)
        dn2cpp_throw_argument();
    return 1;
}

// String.ToCharArray(startIndex, length) — the substring form of the copy in
// dn2cpp_string_to_chararray, with .NET's checks (both bad start and bad
// length raise a catchable ArgumentOutOfRangeException; a zero-length slice
// at any valid position — including startIndex == Length — is an empty
// array).
Dn2CppArrayN* dn2cpp_string_to_chararray_range(Dn2CppString* s, int32_t startIndex,
                                               int32_t length, const Dn2CppTypeInfo* ti)
{
    if (s == nullptr)
        dn2cpp_throw_null_reference();
    if (length < 0)
        dn2cpp_throw_argument_out_of_range();
    if (startIndex < 0 || startIndex > s->length || startIndex > s->length - length)
        dn2cpp_throw_argument_out_of_range();
    Dn2CppArrayN* arr = dn2cpp_newarr_n_t(length, static_cast<int32_t>(sizeof(char16_t)), ti);
    if (length > 0)
        std::memcpy(arr->data, s->chars + startIndex,
                    static_cast<size_t>(length) * sizeof(char16_t));
    return arr;
}

// String.CopyTo(sourceIndex, char[], destinationIndex, count) — the legacy
// array form. .NET's validation order: a null destination is an
// ArgumentNullException first (even alongside a bad count), then the range
// checks raise a catchable ArgumentOutOfRangeException.
void dn2cpp_str_copyto_chararray(Dn2CppString* s, int32_t sourceIndex, Dn2CppArrayN* destination,
                                 int32_t destinationIndex, int32_t count)
{
    if (s == nullptr)
        dn2cpp_throw_null_reference();
    if (destination == nullptr)
        dn2cpp_throw_argument_null();
    if (count < 0 || sourceIndex < 0 || count > s->length - sourceIndex)
        dn2cpp_throw_argument_out_of_range();
    if (destinationIndex < 0 || destinationIndex > destination->length - count)
        dn2cpp_throw_argument_out_of_range();
    if (count > 0)
        std::memcpy(reinterpret_cast<char16_t*>(destination->data) + destinationIndex,
                    s->chars + sourceIndex, static_cast<size_t>(count) * sizeof(char16_t));
}

// IndexOf(string, startIndex, count[, comparison]) — forward search limited
// to the `count` positions from `startIndex`; a match must lie entirely
// within the range. Empty needle -> startIndex; null needle -> catchable
// ArgumentNullException; bad start/count -> catchable
// ArgumentOutOfRangeException (the .NET checks, in order).
int32_t dn2cpp_str_indexof_str_range(Dn2CppString* s, Dn2CppString* sub, int32_t startIndex,
                                     int32_t count, int32_t comparison)
{
    comparison = dn2cpp_str_comparison_fold(comparison);
    if (s == nullptr)
        dn2cpp_throw_null_reference();
    if (sub == nullptr)
        dn2cpp_throw_argument_null();
    if (startIndex < 0 || startIndex > s->length)
        dn2cpp_throw_argument_out_of_range();
    if (count < 0 || startIndex > s->length - count)
        dn2cpp_throw_argument_out_of_range();
    if (sub->length == 0)
        return startIndex;
    for (int32_t i = startIndex; i + sub->length <= startIndex + count; i++)
    {
        int32_t j = 0;
        while (j < sub->length && dn2cpp_str_char_eq_cmp(s->chars[i + j], sub->chars[j], comparison))
            j++;
        if (j == sub->length)
            return i;
    }
    return -1;
}

// MemoryExtensions.ToUpperInvariant/ToLowerInvariant(source, destination):
// per-code-unit BMP invariant fold; returns the source length, or -1 when
// the destination is too short (.NET's contract). .NET additionally throws
// InvalidOperationException for overlapping spans; dn2cpp does not model
// overlap detection — callers must keep the buffers disjoint.
int32_t dn2cpp_span_case_invariant(const char16_t* src, int32_t srcLen, char16_t* dst,
                                   int32_t dstLen, int32_t toUpper)
{
    if (srcLen > dstLen)
        return -1;
    for (int32_t i = 0; i < srcLen; i++)
        dst[i] = toUpper != 0 ? dn2cpp_char_upper_invariant(src[i])
                              : dn2cpp_char_lower_invariant(src[i]);
    return srcLen;
}

Dn2CppString* dn2cpp_str_trim(Dn2CppString* s)
{
    if (s == nullptr)
        dn2cpp_throw_null_reference();
    // .NET Trim() trims char.IsWhiteSpace (full Unicode: NBSP, ideographic
    // space, line/paragraph separators, ...), not just the ASCII set.
    int32_t a = 0, b = s->length;
    while (a < b && dn2cpp_char_is_whitespace(s->chars[a]))
        a++;
    while (b > a && dn2cpp_char_is_whitespace(s->chars[b - 1]))
        b--;
    // Nothing to trim: a string is immutable, so return it as-is rather than
    // allocating and copying an identical string (matches .NET's Trim()).
    if (a == 0 && b == s->length)
        return s;
    return dn2cpp_str_substring(s, a, b - a);
}

// Trim/TrimStart/TrimEnd over an explicit trim set (mode 0 = both, 1 = start
// only, 2 = end only). A null or empty set means .NET's default: trim
// char.IsWhiteSpace — Trim(null) and Trim(new char[0]) both whitespace-trim.
// Returns the ORIGINAL instance when nothing is trimmed (immutable reuse,
// ReferenceEquals-visible, matching .NET).
Dn2CppString* dn2cpp_str_trim_set(Dn2CppString* s, const char16_t* set, int32_t setLen, int32_t mode)
{
    if (s == nullptr)
        dn2cpp_throw_null_reference();
    auto inSet = [set, setLen](char16_t c) -> bool {
        if (set == nullptr || setLen <= 0)
            return dn2cpp_char_is_whitespace(c) != 0;
        for (int32_t k = 0; k < setLen; k++)
            if (set[k] == c)
                return true;
        return false;
    };
    int32_t a = 0, b = s->length;
    if (mode != 2)
        while (a < b && inSet(s->chars[a]))
            a++;
    if (mode != 1)
        while (b > a && inSet(s->chars[b - 1]))
            b--;
    if (a == 0 && b == s->length)
        return s;
    return dn2cpp_str_substring(s, a, b - a);
}

bool dn2cpp_is_ws(char16_t c)
{
    return c == u' ' || c == u'\t' || c == u'\n' || c == u'\r' || c == u'\f' || c == u'\v';
}

// TrimEntries trims char.IsWhiteSpace off both ends of a [start, end) segment,
// like Trim() itself.
static void dn2cpp_split_trim_seg(Dn2CppString* s, int32_t* start, int32_t* end)
{
    while (*start < *end && dn2cpp_char_is_whitespace(s->chars[*start]))
        (*start)++;
    while (*end > *start && dn2cpp_char_is_whitespace(s->chars[*end - 1]))
        (*end)--;
}

// The count<=1 / no-separator-found result, .NET's
// CreateSplitArrayOfThisAsSoleValue: the whole string as the single entry,
// TrimEntries applied; dropped (empty array) when RemoveEmptyEntries removes
// it. count is already validated non-negative and known nonzero here.
static Dn2CppArrayRef* dn2cpp_split_sole_value(Dn2CppString* s, bool removeEmpty, bool trim)
{
    int32_t a = 0, b = s->length;
    if (trim)
        dn2cpp_split_trim_seg(s, &a, &b);
    if (removeEmpty && a == b)
        return dn2cpp_newarr_ref(0);
    Dn2CppArrayRef* arr = dn2cpp_newarr_ref(1);
    Dn2CppString* v = (a == 0 && b == s->length) ? s : dn2cpp_str_substring(s, a, b - a);
    dn2cpp_gc_store_ref(&arr->data[0], reinterpret_cast<Dn2CppObject*>(v));
    return arr;
}

// String.Split engine behind every Split overload. Separator selection:
// string-array mode when `sepStrs` has entries (at each position the FIRST
// matching entry in array order wins, null/empty entries skipped, the scan
// resumes after the matched separator — non-overlapping); otherwise char-set
// mode over `sepChars` (any-of); otherwise (both absent/empty) whitespace mode
// (any char.IsWhiteSpace char separates). `count` caps the number of KEPT
// entries; once count-1 entries are kept the remainder of the string
// (separators included) is the last entry, except that with RemoveEmptyEntries
// the entries that would be removed right before the remainder are skipped
// first (separators consumed), matching .NET. TrimEntries trims each entry —
// including the remainder — before the RemoveEmptyEntries check. count < 0
// throws ArgumentOutOfRangeException, an options value outside [0, 3]
// ArgumentException (both catchable, validated in that order, like .NET).
Dn2CppArrayRef* dn2cpp_str_split(Dn2CppString* s, const char16_t* sepChars, int32_t nSepChars,
                                 Dn2CppArrayRef* sepStrs, int32_t count, int32_t opts)
{
    if (s == nullptr)
        dn2cpp_throw_null_reference();
    if (count < 0)
        dn2cpp_throw_argument_out_of_range(); // catchable, like .NET's count check
    if (static_cast<uint32_t>(opts) > 3u)
        dn2cpp_throw_argument(); // catchable, like .NET's CheckStringSplitOptions
    bool removeEmpty = (opts & 1) != 0;
    bool trim = (opts & 2) != 0;
    if (count == 0)
        return dn2cpp_newarr_ref(0);
    if (count == 1 || s->length == 0)
        return dn2cpp_split_sole_value(s, removeEmpty, trim);

    Dn2CppString* const* strSeps =
        sepStrs != nullptr && sepStrs->length > 0
            ? reinterpret_cast<Dn2CppString* const*>(sepStrs->data)
            : nullptr;
    int32_t nStrSeps = strSeps != nullptr ? sepStrs->length : 0;
    bool charMode = strSeps == nullptr && sepChars != nullptr && nSepChars > 0;

    // Separator scan (positions + lengths), .NET's MakeSeparatorList.
    std::vector<int32_t> sepPos, sepLen;
    if (strSeps != nullptr)
    {
        for (int32_t i = 0; i < s->length;)
        {
            int32_t matched = 0;
            for (int32_t k = 0; k < nStrSeps; k++)
            {
                Dn2CppString* sep = strSeps[k];
                if (sep == nullptr || sep->length == 0 || sep->length > s->length - i)
                    continue;
                if (std::memcmp(s->chars + i, sep->chars,
                                static_cast<size_t>(sep->length) * sizeof(char16_t)) == 0)
                {
                    sepPos.push_back(i);
                    sepLen.push_back(sep->length);
                    matched = sep->length;
                    break;
                }
            }
            i += matched != 0 ? matched : 1;
        }
    }
    else
    {
        for (int32_t i = 0; i < s->length; i++)
        {
            char16_t c = s->chars[i];
            bool isSep;
            if (charMode)
            {
                isSep = false;
                for (int32_t k = 0; k < nSepChars; k++)
                    if (sepChars[k] == c)
                    {
                        isSep = true;
                        break;
                    }
            }
            else
            {
                isSep = dn2cpp_char_is_whitespace(c) != 0;
            }
            if (isSep)
                sepPos.push_back(i);
        }
    }
    if (sepPos.empty())
        return dn2cpp_split_sole_value(s, removeEmpty, trim);

    // Two deterministic passes over the separator list: count the kept
    // entries, allocate the exact result array, fill it. Each substring is
    // stored into the GC-visible array the moment it is created — the
    // collector does not scan the malloc heap, so no fresh string may live
    // only in a std::vector.
    auto segLenAt = [&](size_t i) { return sepLen.empty() ? 1 : sepLen[i]; };
    auto emitPass = [&](Dn2CppArrayRef* out) -> int32_t {
        int32_t kept = 0, currIndex = 0;
        for (size_t i = 0; i < sepPos.size(); i++)
        {
            int32_t segStart = currIndex, segEnd = sepPos[i];
            currIndex = sepPos[i] + segLenAt(i);
            if (trim)
                dn2cpp_split_trim_seg(s, &segStart, &segEnd);
            if (!removeEmpty || segEnd > segStart)
            {
                if (out != nullptr)
                    dn2cpp_gc_store_ref(&out->data[kept], reinterpret_cast<Dn2CppObject*>(
                        dn2cpp_str_substring(s, segStart, segEnd - segStart)));
                kept++;
            }
            if (kept == count - 1)
            {
                // The remainder becomes the final entry; with
                // RemoveEmptyEntries first skip (consume the separators of)
                // the entries that would be removed anyway, so the remainder
                // starts at the first entry that survives — .NET's lookahead
                // in SplitWithPostProcessing.
                if (removeEmpty)
                {
                    while (++i < sepPos.size())
                    {
                        int32_t st = currIndex, en = sepPos[i];
                        if (trim)
                            dn2cpp_split_trim_seg(s, &st, &en);
                        if (en > st)
                            break;
                        currIndex = sepPos[i] + segLenAt(i);
                    }
                }
                break;
            }
        }
        int32_t lastStart = currIndex, lastEnd = s->length;
        if (trim)
            dn2cpp_split_trim_seg(s, &lastStart, &lastEnd);
        if (!removeEmpty || lastEnd > lastStart)
        {
            if (out != nullptr)
                dn2cpp_gc_store_ref(&out->data[kept], reinterpret_cast<Dn2CppObject*>(
                    dn2cpp_str_substring(s, lastStart, lastEnd - lastStart)));
            kept++;
        }
        return kept;
    };
    Dn2CppArrayRef* arr = dn2cpp_newarr_ref(emitPass(nullptr));
    emitPass(arr);
    return arr;
}

// Single-char separator: the common `s.Split(',')` shape.
Dn2CppArrayRef* dn2cpp_str_split_char(Dn2CppString* s, char16_t sep, int32_t count, int32_t opts)
{
    return dn2cpp_str_split(s, &sep, 1, nullptr, count, opts);
}

// Single-string separator. A null/empty separator string does NOT mean
// whitespace split (that is the null/empty separator-ARRAY contract): .NET
// treats it as string mode with no separator occurrences, so the whole string
// comes back as the sole entry. Routing it through the string-array scan with
// the one (possibly null) entry reproduces exactly that.
Dn2CppArrayRef* dn2cpp_str_split_str(Dn2CppString* s, Dn2CppString* sep, int32_t count, int32_t opts)
{
    if (s == nullptr)
        dn2cpp_throw_null_reference();
    if (count < 0)
        dn2cpp_throw_argument_out_of_range(); // catchable, like .NET's count check
    if (static_cast<uint32_t>(opts) > 3u)
        dn2cpp_throw_argument(); // catchable, like .NET's CheckStringSplitOptions
    if (count == 0)
        return dn2cpp_newarr_ref(0);
    if (sep == nullptr || sep->length == 0)
        return dn2cpp_split_sole_value(s, (opts & 1) != 0, (opts & 2) != 0);
    Dn2CppArrayRef* one = dn2cpp_newarr_ref(1);
    dn2cpp_gc_store_ref(&one->data[0], reinterpret_cast<Dn2CppObject*>(sep));
    return dn2cpp_str_split(s, nullptr, 0, one, count, opts);
}

// ---- integer parsing ----

// Parse `s` (optional surrounding whitespace, optional +/-, decimal digits) into
// an int64. Returns false on empty/malformed input.
static bool dn2cpp_try_parse_i64(Dn2CppString* s, int64_t* out)
{
    if (s == nullptr)
        return false;
    int32_t i = 0, n = s->length;
    while (i < n && dn2cpp_is_ws(s->chars[i]))
        i++;
    while (n > i && dn2cpp_is_ws(s->chars[n - 1]))
        n--;
    if (i >= n)
        return false;
    bool neg = false;
    if (s->chars[i] == u'-') { neg = true; i++; }
    else if (s->chars[i] == u'+') { i++; }
    if (i >= n)
        return false;
    int64_t v = 0;
    for (; i < n; i++)
    {
        char16_t c = s->chars[i];
        if (c < u'0' || c > u'9')
            return false;
        v = v * 10 + (c - u'0');
    }
    *out = neg ? -v : v;
    return true;
}

int32_t dn2cpp_int_tryparse(Dn2CppString* s, int32_t* out)
{
    int64_t v;
    if (!dn2cpp_try_parse_i64(s, &v) || v < -2147483648LL || v > 2147483647LL)
    {
        *out = 0; // .NET TryParse assigns the default on failure
        return 0;
    }
    *out = static_cast<int32_t>(v);
    return 1;
}

// .NET answers a malformed string and a well-formed out-of-range one with
// DIFFERENT exception types, and a caller catching one must not be handed the
// other — so the two failures are told apart here rather than through TryParse,
// which reports only that it failed.
int32_t dn2cpp_int_parse(Dn2CppString* s)
{
    int64_t wide;
    if (!dn2cpp_try_parse_i64(s, &wide))
        dn2cpp_throw_format_value(s);
    if (wide < -2147483648LL || wide > 2147483647LL)
        dn2cpp_overflow();
    return static_cast<int32_t>(wide);
}

int32_t dn2cpp_long_tryparse(Dn2CppString* s, int64_t* out)
{
    if (dn2cpp_try_parse_i64(s, out))
        return 1;
    *out = 0; // .NET TryParse assigns the default on failure
    return 0;
}

int64_t dn2cpp_long_parse(Dn2CppString* s)
{
    int64_t v;
    if (!dn2cpp_try_parse_i64(s, &v))
        dn2cpp_throw_format_value(s);
    return v;
}

// bool.TryParse: trims surrounding whitespace, then matches "True"/"False"
// ordinal-case-insensitively (Boolean.TrueLiteral/FalseLiteral). Anything else
// fails and leaves the result false, matching .NET.
int32_t dn2cpp_bool_tryparse(Dn2CppString* s, uint8_t* out)
{
    *out = 0;
    if (s == nullptr)
        return 0;
    int32_t a = 0, b = s->length;
    while (a < b && dn2cpp_is_ws(s->chars[a]))
        a++;
    while (b > a && dn2cpp_is_ws(s->chars[b - 1]))
        b--;
    int32_t n = b - a;
    auto matches = [&](const char* lit, int32_t litLen) {
        if (n != litLen)
            return false;
        for (int32_t i = 0; i < n; i++)
        {
            char16_t c = s->chars[a + i];
            if (c >= u'A' && c <= u'Z')
                c = static_cast<char16_t>(c + 32);
            char16_t lc = static_cast<char16_t>(lit[i]);
            if (c != lc)
                return false;
        }
        return true;
    };
    if (matches("true", 4)) { *out = 1; return 1; }
    if (matches("false", 5)) { *out = 0; return 1; }
    return 0;
}

// Boolean.IsTrueStringIgnoreCase / IsFalseStringIgnoreCase(ReadOnlySpan<char>):
// exact-literal ordinal-case-insensitive match (no trimming -- .NET compares
// exactly the 4/5 code units of "true"/"false").
static bool dn2cpp_chars_match_ascii_lit(const char16_t* chars, int32_t len,
                                         const char* lit, int32_t litLen)
{
    if (chars == nullptr || len != litLen)
        return false;
    for (int32_t i = 0; i < len; i++)
    {
        char16_t c = chars[i];
        if (c >= u'A' && c <= u'Z')
            c = static_cast<char16_t>(c + 32);
        if (c != static_cast<char16_t>(lit[i]))
            return false;
    }
    return true;
}

int32_t dn2cpp_bool_is_true_chars(const char16_t* chars, int32_t len)
{
    return dn2cpp_chars_match_ascii_lit(chars, len, "true", 4) ? 1 : 0;
}

int32_t dn2cpp_bool_is_false_chars(const char16_t* chars, int32_t len)
{
    return dn2cpp_chars_match_ascii_lit(chars, len, "false", 5) ? 1 : 0;
}

// ---- floating-point parsing (invariant) ----

// Parse `s` (optional surrounding whitespace, then a decimal real number) into a
// double via strtod. The C locale uses '.' as the decimal separator, matching
// InvariantCulture. Returns false on empty/malformed input or trailing garbage.
static bool dn2cpp_try_parse_r8(Dn2CppString* s, double* out)
{
    if (s == nullptr)
        return false;
    int32_t i = 0, n = s->length;
    while (i < n && dn2cpp_is_ws(s->chars[i]))
        i++;
    while (n > i && dn2cpp_is_ws(s->chars[n - 1]))
        n--;
    if (i >= n)
        return false;
    // Copy the trimmed core to a NUL-terminated ASCII buffer for strtod.
    int32_t len = n - i;
    char* buf = static_cast<char*>(dn2cpp_alloc(static_cast<size_t>(len) + 1));
    for (int32_t k = 0; k < len; k++)
    {
        char16_t c = s->chars[i + k];
        if (c > 0x7F)
            return false; // non-ASCII (e.g. real Unicode minus) — reject
        buf[k] = static_cast<char>(c);
    }
    buf[len] = '\0';
    char* end = nullptr;
    double v = std::strtod(buf, &end);
    if (end != buf + len) // no parse, or trailing characters left over
        return false;
    *out = v;
    return true;
}

// True when the code units of `t` (non-empty) appear at s[i..].
static bool dn2cpp_match_at(const char16_t* s, int n, int i, Dn2CppString* t)
{
    if (t == nullptr || t->length == 0 || i + t->length > n)
        return false;
    for (int k = 0; k < t->length; k++)
        if (s[i + k] != t->chars[k])
            return false;
    return true;
}

// Rewrite a culture-formatted numeric string to the invariant form the parsers
// accept: the culture decimal separator -> '.', the negative sign -> '-', and
// (when `stripGroup`) the group separator removed. Used by the *_parse_c /
// *_tryparse_c culture overloads.
static Dn2CppString* dn2cpp_culture_normalize(Dn2CppString* s, const Dn2CppNumberFormatInfo* nfi, bool stripGroup)
{
    nfi = dn2cpp_nfi_or_current(nfi);
    if (s == nullptr)
        return s;
    const char16_t* p = s->chars;
    int n = s->length;
    char16_t buf[256];
    int o = 0;
    for (int i = 0; i < n && o < 255;)
    {
        if (stripGroup && dn2cpp_match_at(p, n, i, nfi->numberGroup)) { i += nfi->numberGroup->length; continue; }
        if (dn2cpp_match_at(p, n, i, nfi->numberDecimal)) { buf[o++] = u'.'; i += nfi->numberDecimal->length; continue; }
        if (dn2cpp_match_at(p, n, i, nfi->negativeSign)) { buf[o++] = u'-'; i += nfi->negativeSign->length; continue; }
        buf[o++] = p[i++];
    }
    char16_t* dst;
    Dn2CppString* r = dn2cpp_string_alloc(&dst, o);
    for (int i = 0; i < o; i++) dst[i] = buf[i];
    return r;
}

double dn2cpp_double_parse_c(Dn2CppString* s, const Dn2CppNumberFormatInfo* nfi)
{
    double v;
    if (!dn2cpp_try_parse_r8(dn2cpp_culture_normalize(s, nfi, true), &v))
        dn2cpp_throw_format_value(s);
    return v;
}

float dn2cpp_float_parse_c(Dn2CppString* s, const Dn2CppNumberFormatInfo* nfi)
{
    double v;
    if (!dn2cpp_try_parse_r8(dn2cpp_culture_normalize(s, nfi, true), &v))
        dn2cpp_throw_format_value(s);
    return static_cast<float>(v);
}

int32_t dn2cpp_double_tryparse_c(Dn2CppString* s, const Dn2CppNumberFormatInfo* nfi, double* out)
{
    if (dn2cpp_try_parse_r8(dn2cpp_culture_normalize(s, nfi, true), out))
        return 1;
    *out = 0.0;
    return 0;
}

int32_t dn2cpp_float_tryparse_c(Dn2CppString* s, const Dn2CppNumberFormatInfo* nfi, float* out)
{
    double v;
    if (dn2cpp_try_parse_r8(dn2cpp_culture_normalize(s, nfi, true), &v)) { *out = static_cast<float>(v); return 1; }
    *out = 0.0f;
    return 0;
}

// Integer culture parse: NumberStyles.Integer (no thousands) — normalize the
// negative sign only (the group separator stays, so a grouped integer fails to
// parse, matching .NET's default integer style).
int32_t dn2cpp_int_parse_c(Dn2CppString* s, const Dn2CppNumberFormatInfo* nfi)
{
    return dn2cpp_int_parse(dn2cpp_culture_normalize(s, nfi, false));
}

int64_t dn2cpp_long_parse_c(Dn2CppString* s, const Dn2CppNumberFormatInfo* nfi)
{
    return dn2cpp_long_parse(dn2cpp_culture_normalize(s, nfi, false));
}

int32_t dn2cpp_int_tryparse_c(Dn2CppString* s, const Dn2CppNumberFormatInfo* nfi, int32_t* out)
{
    return dn2cpp_int_tryparse(dn2cpp_culture_normalize(s, nfi, false), out);
}

int32_t dn2cpp_long_tryparse_c(Dn2CppString* s, const Dn2CppNumberFormatInfo* nfi, int64_t* out)
{
    return dn2cpp_long_tryparse(dn2cpp_culture_normalize(s, nfi, false), out);
}

int32_t dn2cpp_double_tryparse(Dn2CppString* s, double* out)
{
    if (dn2cpp_try_parse_r8(s, out))
        return 1;
    *out = 0.0; // .NET TryParse assigns the default on failure
    return 0;
}

double dn2cpp_double_parse(Dn2CppString* s)
{
    double v;
    if (!dn2cpp_try_parse_r8(s, &v))
        dn2cpp_throw_format_value(s);
    return v;
}

int32_t dn2cpp_float_tryparse(Dn2CppString* s, float* out)
{
    double v;
    if (dn2cpp_try_parse_r8(s, &v))
    {
        *out = static_cast<float>(v);
        return 1;
    }
    *out = 0.0f; // .NET TryParse assigns the default on failure
    return 0;
}

float dn2cpp_float_parse(Dn2CppString* s)
{
    double v;
    if (!dn2cpp_try_parse_r8(s, &v))
        dn2cpp_throw_format_value(s);
    return static_cast<float>(v);
}

// ============================ String.Format =================================
// ---- string.Format composite formatting ----
// Reuses the interpolation builder (ISB) and the numeric formatters. Holes are
// `{index[,alignment][:spec]}`; `{{`/`}}` are literal braces. A hole's value is
// the boxed arg formatted by its runtime type — with an explicit `:spec` the
// boxed int32/int64/double/single route to the standard numeric formatters,
// everything else (and the no-spec case) goes through Object.ToString.

static void dn2cpp_format_append_run(Dn2CppISB* h, const char16_t* src, int32_t len)
{
    if (len <= 0)
        return;
    char16_t* buf;
    Dn2CppString* s = dn2cpp_string_alloc(&buf, len);
    std::memcpy(buf, src, static_cast<size_t>(len) * sizeof(char16_t));
    dn2cpp_isb_append_literal(h, s);
}

static Dn2CppString* dn2cpp_format_hole_value(Dn2CppObject* obj, Dn2CppString* spec,
                                             const Dn2CppNumberFormatInfo* nfi)
{
    if (spec == nullptr || spec->length == 0)
        return dn2cpp_object_tostring(obj);
    const Dn2CppTypeInfo* t = (obj != nullptr) ? obj->type : nullptr;
    const void* payload = obj + 1; // boxed value sits right after the header
    if (t == &dn2cpp_int32_type)
        return dn2cpp_format_int_c(*reinterpret_cast<const int32_t*>(payload), 4, spec, nfi);
    if (t == &dn2cpp_int64_type)
        return dn2cpp_format_int_c(*reinterpret_cast<const int64_t*>(payload), 8, spec, nfi);
    if (t == &dn2cpp_uint64_type) // unsigned hole formatting
        return dn2cpp_format_uint_c(*reinterpret_cast<const uint64_t*>(payload), 8, spec, nfi);
    if (t == &dn2cpp_intptr_type) // signed 8-byte hole formatting
        return dn2cpp_format_int_c(*reinterpret_cast<const intptr_t*>(payload), 8, spec, nfi);
    if (t == &dn2cpp_uintptr_type) // unsigned 8-byte hole formatting
        return dn2cpp_format_uint_c(*reinterpret_cast<const uintptr_t*>(payload), 8, spec, nfi);
    if (t == &dn2cpp_double_type)
        return dn2cpp_format_r8_c(*reinterpret_cast<const double*>(payload), spec, nfi);
    if (t == &dn2cpp_single_type)
        return dn2cpp_format_r4_c(*reinterpret_cast<const float*>(payload), spec, nfi);
    // Everything else spec-aware — Decimal and the date/time value types — formats
    // through the slot on its own type-info. Naming their formatters here would pin
    // those translation units into every program that interpolates anything at all.
    if (t != nullptr && t->formatspec != nullptr)
        return t->formatspec(obj, spec, nfi);
    // Other boxed types with an explicit spec fall back to default text.
    return dn2cpp_object_tostring(obj);
}

static Dn2CppString* dn2cpp_string_format_impl(Dn2CppString* fmt, Dn2CppObject** args, int32_t argc,
                                              const Dn2CppNumberFormatInfo* nfi)
{
    if (fmt == nullptr)
        dn2cpp_throw_argument_null();
    Dn2CppISB h = dn2cpp_isb_new(fmt->length, argc);
    const char16_t* p = fmt->chars;
    int32_t n = fmt->length;
    int32_t i = 0, runStart = 0;
    while (i < n)
    {
        char16_t c = p[i];
        if (c == u'{')
        {
            if (i + 1 < n && p[i + 1] == u'{') // escaped "{{" -> one '{'
            {
                dn2cpp_format_append_run(&h, p + runStart, i - runStart + 1);
                i += 2;
                runStart = i;
                continue;
            }
            dn2cpp_format_append_run(&h, p + runStart, i - runStart);
            i++; // past '{'
            int32_t index = 0;
            bool hasIndex = false;
            while (i < n && p[i] >= u'0' && p[i] <= u'9') { index = index * 10 + (p[i] - u'0'); hasIndex = true; i++; }
            if (!hasIndex)
                dn2cpp_throw_format();
            int32_t alignment = 0;
            if (i < n && p[i] == u',')
            {
                i++;
                bool neg = false;
                if (i < n && p[i] == u'-') { neg = true; i++; }
                int32_t a = 0;
                while (i < n && p[i] >= u'0' && p[i] <= u'9') { a = a * 10 + (p[i] - u'0'); i++; }
                alignment = neg ? -a : a;
            }
            Dn2CppString* spec = nullptr;
            if (i < n && p[i] == u':')
            {
                i++;
                int32_t specStart = i;
                while (i < n && p[i] != u'}') i++;
                int32_t specLen = i - specStart;
                char16_t* sbuf;
                spec = dn2cpp_string_alloc(&sbuf, specLen);
                if (specLen > 0)
                    std::memcpy(sbuf, p + specStart, static_cast<size_t>(specLen) * sizeof(char16_t));
            }
            if (i >= n || p[i] != u'}')
                dn2cpp_throw_format();
            i++; // past '}'
            runStart = i;
            if (index >= argc)
                dn2cpp_throw_format();
            dn2cpp_isb_append_aligned(&h, dn2cpp_format_hole_value(args[index], spec, nfi), alignment);
        }
        else if (c == u'}')
        {
            if (i + 1 < n && p[i + 1] == u'}') // escaped "}}" -> one '}'
            {
                dn2cpp_format_append_run(&h, p + runStart, i - runStart + 1);
                i += 2;
                runStart = i;
                continue;
            }
            dn2cpp_throw_format();
        }
        else
        {
            i++;
        }
    }
    dn2cpp_format_append_run(&h, p + runStart, n - runStart);
    return dn2cpp_isb_to_string(&h);
}

Dn2CppString* dn2cpp_string_format1(Dn2CppString* fmt, Dn2CppObject* a0)
{
    Dn2CppObject* a[1] = { a0 };
    return dn2cpp_string_format_impl(fmt, a, 1, nullptr);
}

Dn2CppString* dn2cpp_string_format2(Dn2CppString* fmt, Dn2CppObject* a0, Dn2CppObject* a1)
{
    Dn2CppObject* a[2] = { a0, a1 };
    return dn2cpp_string_format_impl(fmt, a, 2, nullptr);
}

Dn2CppString* dn2cpp_string_format3(Dn2CppString* fmt, Dn2CppObject* a0, Dn2CppObject* a1, Dn2CppObject* a2)
{
    Dn2CppObject* a[3] = { a0, a1, a2 };
    return dn2cpp_string_format_impl(fmt, a, 3, nullptr);
}

Dn2CppString* dn2cpp_string_format_arr(Dn2CppString* fmt, Dn2CppArrayRef* args)
{
    if (args == nullptr)
        dn2cpp_throw_argument_null();
    return dn2cpp_string_format_impl(fmt, args->data, args->length, nullptr);
}

Dn2CppString* dn2cpp_string_format1_c(const Dn2CppNumberFormatInfo* n, Dn2CppString* fmt, Dn2CppObject* a0)
{
    Dn2CppObject* a[1] = { a0 };
    return dn2cpp_string_format_impl(fmt, a, 1, n);
}

Dn2CppString* dn2cpp_string_format2_c(const Dn2CppNumberFormatInfo* n, Dn2CppString* fmt, Dn2CppObject* a0, Dn2CppObject* a1)
{
    Dn2CppObject* a[2] = { a0, a1 };
    return dn2cpp_string_format_impl(fmt, a, 2, n);
}

Dn2CppString* dn2cpp_string_format3_c(const Dn2CppNumberFormatInfo* n, Dn2CppString* fmt, Dn2CppObject* a0, Dn2CppObject* a1, Dn2CppObject* a2)
{
    Dn2CppObject* a[3] = { a0, a1, a2 };
    return dn2cpp_string_format_impl(fmt, a, 3, n);
}

Dn2CppString* dn2cpp_string_format_arr_c(const Dn2CppNumberFormatInfo* n, Dn2CppString* fmt, Dn2CppArrayRef* args)
{
    if (args == nullptr)
        dn2cpp_throw_argument_null();
    return dn2cpp_string_format_impl(fmt, args->data, args->length, n);
}

// The params ReadOnlySpan<object> overloads: the transpiler passes the span's
// data pointer + length directly (no array header), same formatting core.
Dn2CppString* dn2cpp_string_format_spanobjs(Dn2CppString* fmt, Dn2CppObject* const* args, int32_t n)
{
    return dn2cpp_string_format_impl(fmt, const_cast<Dn2CppObject**>(args), n < 0 ? 0 : n, nullptr);
}

Dn2CppString* dn2cpp_string_format_spanobjs_c(const Dn2CppNumberFormatInfo* nfi, Dn2CppString* fmt,
                                              Dn2CppObject* const* args, int32_t n)
{
    return dn2cpp_string_format_impl(fmt, const_cast<Dn2CppObject**>(args), n < 0 ? 0 : n, nfi);
}

// Concat over the first `n` elements of a ref array (a List<T>'s backing
// array, whose allocated length is the capacity ≥ Count, so iterate `n` = Count).
Dn2CppString* dn2cpp_string_concat_objects_n(Dn2CppArrayRef* objs, int32_t n)
{
    if (objs == nullptr || n < 0)
        n = 0;
    auto** parts = static_cast<Dn2CppString**>(dn2cpp_alloc(sizeof(Dn2CppString*) * (n > 0 ? n : 1)));
    for (int32_t i = 0; i < n; i++)
        dn2cpp_gc_store_ref(&parts[i], dn2cpp_object_tostring(objs->data[i]));
    return dn2cpp_string_concat_n(parts, n);
}

Dn2CppString* dn2cpp_string_concat_objects(Dn2CppArrayRef* objs)
{
    return dn2cpp_string_concat_objects_n(objs, objs != nullptr ? objs->length : 0);
}

// string.Join: interleave the element strings with `sep` (no trailing
// separator), then concat. `elems` holds the per-element strings.
static Dn2CppString* dn2cpp_join_strings(Dn2CppString* sep, Dn2CppString** elems, int32_t n)
{
    if (n <= 0)
        return dn2cpp_string_from_utf8("", 0);
    int32_t pc = 2 * n - 1;
    auto** parts = static_cast<Dn2CppString**>(dn2cpp_alloc(sizeof(Dn2CppString*) * pc));
    for (int32_t i = 0; i < n; i++)
    {
        dn2cpp_gc_store_ref(&parts[2 * i], elems[i]);
        if (i < n - 1)
            dn2cpp_gc_store_ref(&parts[2 * i + 1], sep);
    }
    return dn2cpp_string_concat_n(parts, pc);
}

// Join over the first `n` elements. The non-`_n` forms join the whole
// array; the `_n` forms join a List<T>'s live prefix (`n` = Count) since its
// backing array's allocated length is the capacity (≥ Count).
Dn2CppString* dn2cpp_string_join_i4_n(Dn2CppString* sep, Dn2CppArrayI4* a, int32_t n)
{
    if (a == nullptr || n < 0) n = 0;
    auto** e = static_cast<Dn2CppString**>(dn2cpp_alloc(sizeof(Dn2CppString*) * (n > 0 ? n : 1)));
    for (int32_t i = 0; i < n; i++)
        dn2cpp_gc_store_ref(&e[i], dn2cpp_int_to_string(a->data[i]));
    return dn2cpp_join_strings(sep, e, n);
}

Dn2CppString* dn2cpp_string_join_i4(Dn2CppString* sep, Dn2CppArrayI4* a)
{
    return dn2cpp_string_join_i4_n(sep, a, a->length);
}

Dn2CppString* dn2cpp_string_join_i8_n(Dn2CppString* sep, Dn2CppArrayN* a, int32_t n)
{
    if (a == nullptr || n < 0) n = 0;
    auto* d = reinterpret_cast<int64_t*>(a->data);
    auto** e = static_cast<Dn2CppString**>(dn2cpp_alloc(sizeof(Dn2CppString*) * (n > 0 ? n : 1)));
    for (int32_t i = 0; i < n; i++)
        dn2cpp_gc_store_ref(&e[i], dn2cpp_long_to_string(d[i]));
    return dn2cpp_join_strings(sep, e, n);
}

Dn2CppString* dn2cpp_string_join_i8(Dn2CppString* sep, Dn2CppArrayN* a)
{
    return dn2cpp_string_join_i8_n(sep, a, a->length);
}

// Unsigned 32/64-bit element joins — the signed join_i4/join_i8 formatters
// would print uint.MaxValue as -1 (elements must format unsigned).
Dn2CppString* dn2cpp_string_join_u4_n(Dn2CppString* sep, Dn2CppArrayI4* a, int32_t n)
{
    if (a == nullptr || n < 0) n = 0;
    auto** e = static_cast<Dn2CppString**>(dn2cpp_alloc(sizeof(Dn2CppString*) * (n > 0 ? n : 1)));
    for (int32_t i = 0; i < n; i++)
        dn2cpp_gc_store_ref(&e[i], dn2cpp_format_uint(static_cast<uint32_t>(a->data[i]), 4, nullptr));
    return dn2cpp_join_strings(sep, e, n);
}

Dn2CppString* dn2cpp_string_join_u4(Dn2CppString* sep, Dn2CppArrayI4* a)
{
    return dn2cpp_string_join_u4_n(sep, a, a->length);
}

Dn2CppString* dn2cpp_string_join_u8_n(Dn2CppString* sep, Dn2CppArrayN* a, int32_t n)
{
    if (a == nullptr || n < 0) n = 0;
    auto* d = reinterpret_cast<uint64_t*>(a->data);
    auto** e = static_cast<Dn2CppString**>(dn2cpp_alloc(sizeof(Dn2CppString*) * (n > 0 ? n : 1)));
    for (int32_t i = 0; i < n; i++)
        dn2cpp_gc_store_ref(&e[i], dn2cpp_format_uint(d[i], 8, nullptr));
    return dn2cpp_join_strings(sep, e, n);
}

Dn2CppString* dn2cpp_string_join_u8(Dn2CppString* sep, Dn2CppArrayN* a)
{
    return dn2cpp_string_join_u8_n(sep, a, a->length);
}

Dn2CppString* dn2cpp_string_join_r8_n(Dn2CppString* sep, Dn2CppArrayN* a, int32_t n)
{
    if (a == nullptr || n < 0) n = 0;
    auto* d = reinterpret_cast<double*>(a->data);
    auto** e = static_cast<Dn2CppString**>(dn2cpp_alloc(sizeof(Dn2CppString*) * (n > 0 ? n : 1)));
    for (int32_t i = 0; i < n; i++)
        dn2cpp_gc_store_ref(&e[i], dn2cpp_double_to_string(d[i]));
    return dn2cpp_join_strings(sep, e, n);
}

Dn2CppString* dn2cpp_string_join_r8(Dn2CppString* sep, Dn2CppArrayN* a)
{
    return dn2cpp_string_join_r8_n(sep, a, a->length);
}

Dn2CppString* dn2cpp_string_join_ch_n(Dn2CppString* sep, Dn2CppArrayN* a, int32_t n)
{
    if (a == nullptr || n < 0) n = 0;
    auto* d = reinterpret_cast<char16_t*>(a->data);
    // Empty separator (string.Concat over chars) is a straight code-unit copy.
    if (sep == nullptr || sep->length == 0)
        return dn2cpp_string_from_chars(d, n);
    auto** e = static_cast<Dn2CppString**>(dn2cpp_alloc(sizeof(Dn2CppString*) * (n > 0 ? n : 1)));
    for (int32_t i = 0; i < n; i++)
        dn2cpp_gc_store_ref(&e[i], dn2cpp_char_to_string(d[i]));
    return dn2cpp_join_strings(sep, e, n);
}

Dn2CppString* dn2cpp_string_join_ch(Dn2CppString* sep, Dn2CppArrayN* a)
{
    return dn2cpp_string_join_ch_n(sep, a, a->length);
}

Dn2CppString* dn2cpp_string_join_ref_n(Dn2CppString* sep, Dn2CppArrayRef* a, int32_t n)
{
    if (a == nullptr || n < 0) n = 0;
    auto** e = static_cast<Dn2CppString**>(dn2cpp_alloc(sizeof(Dn2CppString*) * (n > 0 ? n : 1)));
    for (int32_t i = 0; i < n; i++)
        dn2cpp_gc_store_ref(&e[i], dn2cpp_object_tostring(a->data[i]));
    return dn2cpp_join_strings(sep, e, n);
}

Dn2CppString* dn2cpp_string_join_ref(Dn2CppString* sep, Dn2CppArrayRef* a)
{
    return dn2cpp_string_join_ref_n(sep, a, a->length);
}

// Join/Concat over a span's data pointer + length — the params
// ReadOnlySpan<object|string> overloads (a string element is an object whose
// ToString is itself, so one helper serves both element types). Null elements
// contribute nothing, matching .NET.
Dn2CppString* dn2cpp_string_join_objs(Dn2CppString* sep, Dn2CppObject* const* d, int32_t n)
{
    if (d == nullptr || n < 0) n = 0;
    auto** e = static_cast<Dn2CppString**>(dn2cpp_alloc(sizeof(Dn2CppString*) * (n > 0 ? n : 1)));
    for (int32_t i = 0; i < n; i++)
        dn2cpp_gc_store_ref(&e[i], dn2cpp_object_tostring(d[i]));
    return dn2cpp_join_strings(sep, e, n);
}

Dn2CppString* dn2cpp_string_concat_objs(Dn2CppObject* const* d, int32_t n)
{
    if (d == nullptr || n < 0) n = 0;
    auto** e = static_cast<Dn2CppString**>(dn2cpp_alloc(sizeof(Dn2CppString*) * (n > 0 ? n : 1)));
    for (int32_t i = 0; i < n; i++)
        dn2cpp_gc_store_ref(&e[i], dn2cpp_object_tostring(d[i]));
    return dn2cpp_string_concat_n(e, n);
}

// String.Join(separator, string[], startIndex, count) — the 4-arg slice form:
// a null array is a catchable ArgumentNullException, a negative start/count
// or a slice past the end a catchable ArgumentOutOfRangeException (the .NET
// checks); null elements and a null separator contribute nothing.
Dn2CppString* dn2cpp_string_join_ref_range(Dn2CppString* sep, Dn2CppArrayRef* a,
                                           int32_t startIndex, int32_t count)
{
    if (a == nullptr)
        dn2cpp_throw_argument_null();
    if (startIndex < 0 || count < 0 || startIndex > a->length - count)
        dn2cpp_throw_argument_out_of_range();
    auto** e = static_cast<Dn2CppString**>(dn2cpp_alloc(sizeof(Dn2CppString*) * (count > 0 ? count : 1)));
    for (int32_t i = 0; i < count; i++)
        dn2cpp_gc_store_ref(&e[i], dn2cpp_object_tostring(a->data[startIndex + i]));
    return dn2cpp_join_strings(sep, e, count);
}

// RuntimeHelpers.GetHashCode: the runtime identity hash. The real BCL body uses
// object-header internals (sizeof/Unsafe) we don't model, so this stands in as
// an intrinsic. Stable for a given object within a run (pointer-derived).
int32_t dn2cpp_object_hashcode(Dn2CppObject* obj)
{
    if (obj == nullptr)
        return 0;
    // Widen to 64 bits before the fold: a 32-bit `uintptr_t >> 32` is
    // undefined (wasm32), and the widened fold is bit-identical on 64-bit.
    uint64_t p = static_cast<uint64_t>(reinterpret_cast<uintptr_t>(obj));
    return static_cast<int32_t>((p >> 4) ^ (p >> 32));
}

// Typed array allocation: set the array header to a precise per-element
// type-info (ti_arr_<T>) so arr.GetType() reports the exact array type — backing
// Type.GetElementType()/GetArrayRank() and precise array covariance. The untyped
// dn2cpp_newarr_* below default to the shared dn2cpp_array_{ref,i4}_type handles
// (object[]-ish), used for runtime-internal arrays (reflection's object[], Enum.Get*,
// String.Split, …) where a per-element type-info isn't emitted. A null `ti` falls back
// to the shared default, so a caller can pass it unconditionally.
Dn2CppArrayI4* dn2cpp_newarr_i4_t(int32_t length, const Dn2CppTypeInfo* ti)
{
    if (length < 0)
        dn2cpp_overflow();
    size_t size = sizeof(Dn2CppArray) + static_cast<size_t>(length) * sizeof(int32_t);
    if (size < sizeof(Dn2CppArrayI4))
        size = sizeof(Dn2CppArrayI4);
    // int32 storage holds no managed pointers, so allocate it unscanned. The
    // atomic allocator does not clear, but `new int[]` is zero-initialized in
    // .NET, so zero the block before stamping the header (same end state the
    // scanned, auto-cleared dn2cpp_alloc produced).
    auto* arr = static_cast<Dn2CppArrayI4*>(dn2cpp_alloc_atomic(size));
    std::memset(arr, 0, size);
    arr->type = ti != nullptr ? ti : &dn2cpp_array_i4_type;
    arr->length = length;
    return arr;
}

Dn2CppArrayRef* dn2cpp_newarr_ref_t(int32_t length, const Dn2CppTypeInfo* ti)
{
    if (length < 0)
        dn2cpp_overflow();
    size_t size = sizeof(Dn2CppArray) + static_cast<size_t>(length) * sizeof(Dn2CppObject*);
    if (size < sizeof(Dn2CppArrayRef))
        size = sizeof(Dn2CppArrayRef);
    auto* arr = static_cast<Dn2CppArrayRef*>(dn2cpp_alloc(size));
    arr->type = ti != nullptr ? ti : &dn2cpp_array_ref_type;
    arr->length = length;
    return arr;
}

Dn2CppArrayN* dn2cpp_newarr_n_t(int32_t length, int32_t elemSize, const Dn2CppTypeInfo* ti)
{
    if (length < 0)
        dn2cpp_overflow();
    // Header size up to the element storage (data is over-aligned to 16).
    size_t header = (sizeof(Dn2CppArray) + sizeof(int32_t) + 15u) & ~static_cast<size_t>(15u);
    size_t size = header + static_cast<size_t>(length) * elemSize;
    if (size < sizeof(Dn2CppArrayN))
        size = sizeof(Dn2CppArrayN);
    auto* arr = static_cast<Dn2CppArrayN*>(dn2cpp_alloc(size));
    // Without a precise type-info, fall back to the imprecise PACKED tag: a ref tag
    // here would lie about the layout to every runtime-ti reader.
    arr->type = ti != nullptr ? ti : &dn2cpp_array_n_type;
    arr->length = length;
    arr->elemSize = elemSize;
    return arr;
}

// As dn2cpp_newarr_n_t, but for element storage that provably holds no managed
// pointers (primitives, enums, reference-free structs): allocate it unscanned so
// the GC never walks these buffers as if they held pointers. The atomic allocator
// does not clear, but .NET arrays are zero-initialized, so zero the block before
// stamping the header (same end state the scanned, auto-cleared dn2cpp_alloc produced).
Dn2CppArrayN* dn2cpp_newarr_n_atomic_t(int32_t length, int32_t elemSize, const Dn2CppTypeInfo* ti)
{
    if (length < 0)
        dn2cpp_overflow();
    // Header size up to the element storage (data is over-aligned to 16).
    size_t header = (sizeof(Dn2CppArray) + sizeof(int32_t) + 15u) & ~static_cast<size_t>(15u);
    size_t size = header + static_cast<size_t>(length) * elemSize;
    if (size < sizeof(Dn2CppArrayN))
        size = sizeof(Dn2CppArrayN);
    auto* arr = static_cast<Dn2CppArrayN*>(dn2cpp_alloc_atomic(size));
    std::memset(arr, 0, size);
    // Same imprecise PACKED fallback as dn2cpp_newarr_n_t.
    arr->type = ti != nullptr ? ti : &dn2cpp_array_n_type;
    arr->length = length;
    arr->elemSize = elemSize;
    return arr;
}

// ---- Array.Empty<T> singletons ----
// Per-element-type cached length-0 arrays (matching .NET's EmptyArray<T>.Value:
// the same instance every call, no per-call allocation). Keyed by the precise
// ti_arr_<T> handle, whose address is unique per element type. The map's own
// storage is plain malloc memory the collector never walks, so each singleton
// additionally sits in a pinned (GC-scanned, uncollectable) cell that roots it
// for the process lifetime — the same shape as the string intern pool below.
namespace
{
struct Dn2CppEmptyArrayPool
{
    std::mutex mutex;
    std::unordered_map<const Dn2CppTypeInfo*, Dn2CppArray**> entries;
};

Dn2CppEmptyArrayPool& dn2cpp_empty_array_pool()
{
    static Dn2CppEmptyArrayPool& pool = dn2cpp_never_destroyed<Dn2CppEmptyArrayPool>();
    return pool;
}

template <typename Alloc>
Dn2CppArray* dn2cpp_array_empty_cached(const Dn2CppTypeInfo* ti, Alloc alloc)
{
    auto& pool = dn2cpp_empty_array_pool();
    std::lock_guard<std::mutex> lk(pool.mutex);
    auto it = pool.entries.find(ti);
    if (it != pool.entries.end())
        return *it->second;
    auto** cell = static_cast<Dn2CppArray**>(dn2cpp_alloc_pinned(sizeof(Dn2CppArray*)));
    dn2cpp_gc_store_ref(cell, alloc());
    pool.entries.emplace(ti, cell);
    return *cell;
}
} // namespace

Dn2CppArrayI4* dn2cpp_array_empty_i4(const Dn2CppTypeInfo* ti)
{
    return static_cast<Dn2CppArrayI4*>(
        dn2cpp_array_empty_cached(ti, [ti]() -> Dn2CppArray* { return dn2cpp_newarr_i4_t(0, ti); }));
}

Dn2CppArrayRef* dn2cpp_array_empty_ref(const Dn2CppTypeInfo* ti)
{
    return static_cast<Dn2CppArrayRef*>(
        dn2cpp_array_empty_cached(ti, [ti]() -> Dn2CppArray* { return dn2cpp_newarr_ref_t(0, ti); }));
}

Dn2CppArrayN* dn2cpp_array_empty_n(const Dn2CppTypeInfo* ti, int32_t elemSize)
{
    return static_cast<Dn2CppArrayN*>(dn2cpp_array_empty_cached(
        ti, [ti, elemSize]() -> Dn2CppArray* { return dn2cpp_newarr_n_t(0, elemSize, ti); }));
}

Dn2CppArrayN* dn2cpp_array_empty_n_atomic(const Dn2CppTypeInfo* ti, int32_t elemSize)
{
    return static_cast<Dn2CppArrayN*>(dn2cpp_array_empty_cached(
        ti, [ti, elemSize]() -> Dn2CppArray* { return dn2cpp_newarr_n_atomic_t(0, elemSize, ti); }));
}

// Array.Clone() shallow copy, per rep. The fresh array keeps the
// source's precise type-info (clone.GetType() == src.GetType()) and length; the
// element block is memcpy'd verbatim (value elements bitwise, reference element
// pointers shared — a shallow copy, matching .NET). A null receiver faults.
Dn2CppArrayI4* dn2cpp_array_clone_i4(Dn2CppArrayI4* src)
{
    if (src == nullptr)
        dn2cpp_throw_null_reference();
    Dn2CppArrayI4* dst = dn2cpp_newarr_i4_t(src->length, src->type);
    std::memcpy(dst->data, src->data, static_cast<size_t>(src->length) * sizeof(int32_t));
    return dst;
}

Dn2CppArrayRef* dn2cpp_array_clone_ref(Dn2CppArrayRef* src)
{
    if (src == nullptr)
        dn2cpp_throw_null_reference();
    Dn2CppArrayRef* dst = dn2cpp_newarr_ref_t(src->length, src->type);
    dn2cpp_gc_memmove_refs(dst->data, src->data,
        static_cast<size_t>(src->length) * sizeof(Dn2CppObject*));
    return dst;
}

Dn2CppArrayN* dn2cpp_array_clone_n(Dn2CppArrayN* src)
{
    if (src == nullptr)
        dn2cpp_throw_null_reference();
    Dn2CppArrayN* dst = dn2cpp_newarr_n_t(src->length, src->elemSize, src->type);
    dn2cpp_gc_memmove_refs(dst->data, src->data,
        static_cast<size_t>(src->length) * static_cast<size_t>(src->elemSize));
    return dst;
}

// Array.Clone() on a receiver whose STATIC C++ type degraded to Dn2CppObject*
// (e.g. the `?.` null-propagation merge in HashAlgorithm.get_Hash unifies the
// byte[] arm with the null arm as object) — discover the rep from the runtime
// type-info instead, mirroring dn2cpp_pinned_data_addr's discrimination.
//
// The non-array abort here — and the shared one in dn2cpp_array_rep_dyn below, which
// every _dyn copy/clear helper funnels through — stays an abort. IL is type-checked and
// the transpiler only routes Array member calls here, so a user's
// `((Array)(object)notAnArray).Clone()` already threw InvalidCastException at the
// castclass — the claim build-and-run-array-core.sh's NonArrayOperandSubset section
// asserts. Failing the flags test means the emitter routed a call it should not have:
// a transpiler bug with no catchable interpretation, so the "InvalidCastException" in
// the text is a flavour, not a type anything raises.
Dn2CppObject* dn2cpp_array_clone_dyn(Dn2CppObject* src)
{
    if (src == nullptr)
        dn2cpp_throw_null_reference();
    const Dn2CppTypeInfo* t = src->type;
    if (t == nullptr || (t->flags & DN2CPP_TF_ARRAY) == 0)
        dn2cpp_fail("InvalidCastException (Array.Clone receiver is not an array)");
    // arrayRank is 1 for an emitted ti_arr_<T> and 0 for the shared reference-
    // element fallback header; both mean an SZ rep. Only a rank above that is the
    // Dn2CppMDArray layout, whose data block is a separate field — the rep casts
    // below would read its header words as a length. One copy of this arm serves
    // both Clone mouths: Object.MemberwiseClone's array case and the
    // System.Array-typed Array.Clone intrinsic.
    if (t->arrayRank > 1)
    {
        auto* mdSrc = reinterpret_cast<Dn2CppMDArray*>(src);
        Dn2CppMDArray* dst = dn2cpp_newmdarr(mdSrc->type, mdSrc->rank, mdSrc->lengths, mdSrc->elemSize);
        int32_t total = 1;
        for (int32_t i = 0; i < mdSrc->rank; i++)
        {
            dst->lowerBounds[i] = mdSrc->lowerBounds[i]; // newmdarr zeroes them; a clone keeps the source's
            total *= mdSrc->lengths[i];
        }
        dn2cpp_gc_memmove_refs(dst->data, mdSrc->data,
            static_cast<size_t>(total) * static_cast<size_t>(mdSrc->elemSize));
        return dst;
    }
    const Dn2CppTypeInfo* el = t->elementType;
    bool isRef = el == nullptr ? (t == &dn2cpp_array_ref_type)
                               : (el->flags & DN2CPP_TF_VALUETYPE) == 0;
    if (isRef)
        return reinterpret_cast<Dn2CppObject*>(dn2cpp_array_clone_ref(static_cast<Dn2CppArrayRef*>(src)));
    bool isI4 = el == &dn2cpp_int32_type || el == &dn2cpp_uint32_type
        || (el != nullptr && (el->flags & DN2CPP_TF_ENUM) != 0
            && (el->enumUnderlying == &dn2cpp_int32_type
                || el->enumUnderlying == &dn2cpp_uint32_type));
    if (isI4)
        return reinterpret_cast<Dn2CppObject*>(dn2cpp_array_clone_i4(static_cast<Dn2CppArrayI4*>(src)));
    return reinterpret_cast<Dn2CppObject*>(dn2cpp_array_clone_n(static_cast<Dn2CppArrayN*>(src)));
}

// The clone_dyn / pinned_data_addr TypeInfo discrimination as an ArrRep-style
// answer: 0 = ref-element (Dn2CppArrayRef), 1 = int32-width (Dn2CppArrayI4),
// 2 = packed element-sized (Dn2CppArrayN). Shared by the copy/clear _dyn
// helpers below.
static int dn2cpp_array_rep_dyn(Dn2CppObject* o, const char* who)
{
    const Dn2CppTypeInfo* t = o->type;
    if (t == nullptr || (t->flags & DN2CPP_TF_ARRAY) == 0)
        dn2cpp_fail(who);
    const Dn2CppTypeInfo* el = t->elementType;
    bool isRef = el == nullptr ? (t == &dn2cpp_array_ref_type)
                               : (el->flags & DN2CPP_TF_VALUETYPE) == 0;
    if (isRef)
        return 0;
    bool isI4 = el == &dn2cpp_int32_type || el == &dn2cpp_uint32_type
        || (el != nullptr && (el->flags & DN2CPP_TF_ENUM) != 0
            && (el->enumUnderlying == &dn2cpp_int32_type
                || el->enumUnderlying == &dn2cpp_uint32_type));
    return isI4 ? 1 : 2;
}

// Array.Copy on operands whose STATIC C++ type degraded to a non-concrete array (a
// shared-generic T[] body), and every pair the emitter cannot prove same-element. Two
// IDENTICAL runtime identities move under the shared layout, mirroring EmitArrayCopy's
// static-rep rule; a mixed pair runs the CLR's full compatibility verdict
// (dn2cpp_array_copy_checked) rather than trusting the source's rep for both sides,
// which is a heap overrun whenever the reps disagree.
//
// A null operand is ArgumentNullException, not NullReferenceException: Array.Copy is
// STATIC, so .NET faults on the argument rather than on a receiver.
void dn2cpp_array_copy_dyn(Dn2CppObject* src, int32_t srcIdx,
                           Dn2CppObject* dst, int32_t dstIdx, int32_t len)
{
    if (src == nullptr || dst == nullptr)
        dn2cpp_throw_argument_null();
    // .NET's own order: the two nulls, the rank match, the range, then the type
    // verdict — which is why the checked delegation sits last.
    int32_t rank = dn2cpp_array_rank_of(src);
    if (rank != dn2cpp_array_rank_of(dst))
        dn2cpp_throw_rank();
    dn2cpp_array_copy_range(dn2cpp_array_total_length(src), srcIdx,
                            dn2cpp_array_total_length(dst), dstIdx, len);
    if (src->type != dst->type || src->type == nullptr)
    {
        dn2cpp_array_copy_checked(src, srcIdx, dst, dstIdx, len);
        return;
    }
    // The MD layout first, for the same header-vs-length reason as the clone
    // above. Elements are flat in the separate data block, in the row-major
    // order .NET's own MD Copy moves them in, so one byte-wise window serves
    // every element type and every pair of dimension shapes of that rank.
    if (rank > 1)
    {
        auto* ms = reinterpret_cast<Dn2CppMDArray*>(src);
        auto* md = reinterpret_cast<Dn2CppMDArray*>(dst);
        dn2cpp_gc_memmove_refs(md->data + static_cast<size_t>(dstIdx) * md->elemSize,
                               ms->data + static_cast<size_t>(srcIdx) * ms->elemSize,
                               static_cast<size_t>(len) * ms->elemSize);
        return;
    }
    switch (dn2cpp_array_rep_dyn(src, "InvalidCastException (Array.Copy source is not an SZArray)"))
    {
        case 0:
            dn2cpp_gc_memmove_refs(&static_cast<Dn2CppArrayRef*>(dst)->data[dstIdx],
                                   &static_cast<Dn2CppArrayRef*>(src)->data[srcIdx],
                                   static_cast<size_t>(len) * sizeof(Dn2CppObject*));
            return;
        case 1:
            std::memmove(&static_cast<Dn2CppArrayI4*>(dst)->data[dstIdx],
                         &static_cast<Dn2CppArrayI4*>(src)->data[srcIdx],
                         static_cast<size_t>(len) * sizeof(int32_t));
            return;
        default:
        {
            Dn2CppArrayN* cs = static_cast<Dn2CppArrayN*>(src);
            Dn2CppArrayN* cd = static_cast<Dn2CppArrayN*>(dst);
            dn2cpp_gc_memmove_refs(cd->data + static_cast<size_t>(dstIdx) * cd->elemSize,
                                   cs->data + static_cast<size_t>(srcIdx) * cs->elemSize,
                                   static_cast<size_t>(len) * cs->elemSize);
            return;
        }
    }
}

// Array.Clear's sibling of dn2cpp_array_copy_dyn: zero `len` elements from
// `idx`, rep from the runtime type-info. Static too, so a null array is an
// ArgumentNullException for the same reason.
void dn2cpp_array_clear_dyn(Dn2CppObject* arr, int32_t idx, int32_t len)
{
    if (arr == nullptr)
        dn2cpp_throw_argument_null();
    dn2cpp_array_clear_range(dn2cpp_array_total_length(arr), idx, len);
    // The MD layout first, for the same header-vs-length reason as the clone
    // above: elements are flat in the separate data block, so a (flat idx, len)
    // window zeroes byte-wise. Reached by Array.Clear over a System.Array-typed
    // MD receiver.
    if (arr->type != nullptr && arr->type->arrayRank > 1)
    {
        auto* md = reinterpret_cast<Dn2CppMDArray*>(arr);
        std::memset(md->data + static_cast<size_t>(idx) * static_cast<size_t>(md->elemSize), 0,
                    static_cast<size_t>(len) * static_cast<size_t>(md->elemSize));
        return;
    }
    switch (dn2cpp_array_rep_dyn(arr, "InvalidCastException (Array.Clear target is not an SZArray)"))
    {
        case 0:
            std::memset(&static_cast<Dn2CppArrayRef*>(arr)->data[idx], 0,
                        static_cast<size_t>(len) * sizeof(Dn2CppObject*));
            return;
        case 1:
            std::memset(&static_cast<Dn2CppArrayI4*>(arr)->data[idx], 0,
                        static_cast<size_t>(len) * sizeof(int32_t));
            return;
        default:
        {
            Dn2CppArrayN* ca = static_cast<Dn2CppArrayN*>(arr);
            std::memset(ca->data + static_cast<size_t>(idx) * ca->elemSize, 0,
                        static_cast<size_t>(len) * ca->elemSize);
            return;
        }
    }
}

// RuntimeHelpers.GetSubArray<T>(T[], Range) = array[range]. Resolve a
// Range's two Index ._value fields against the source length into (offset, length),
// matching System.Range.GetOffsetAndLength exactly: an Index ._value < 0 is a
// from-end index (its offset is length + _value + 1, since _value == ~fromEndIndex);
// otherwise it is the from-start offset. The (uint) bounds checks reject end > length
// and start > end (an out-of-range or reversed range -> ArgumentOutOfRangeException),
// then the slice length is end - start. Writes the offset through *outOffset and
// returns the slice length. The Range math is shared by all element reps below.
int32_t dn2cpp_range_offset_length(int32_t startVal, int32_t endVal, int32_t srcLen, int32_t* outOffset)
{
    int32_t start = startVal < 0 ? srcLen + startVal + 1 : startVal;
    int32_t end   = endVal   < 0 ? srcLen + endVal   + 1 : endVal;
    if (static_cast<uint32_t>(end) > static_cast<uint32_t>(srcLen)
        || static_cast<uint32_t>(start) > static_cast<uint32_t>(end))
        dn2cpp_throw_argument_out_of_range(); // catchable, matching Range.GetOffsetAndLength
    *outOffset = start;
    return end - start;
}

// GetSubArray<T> slice copy, per rep. A fresh array of the source's
// precise type-info (so sub.GetType() == src.GetType()) and the slice length, with
// the element run [offset, offset+length) memcpy'd verbatim — value elements bitwise,
// reference-element pointers shared (a shallow copy, matching .NET). The offset/length
// are precomputed (and bounds-checked) by dn2cpp_range_offset_length. A null receiver
// faults like .NET's ThrowArgumentNullException. One helper per rep, dispatched at emit
// time on the source's static C++ array type (the real body routes through Array.Create-
// InstanceFromArrayType / MemoryMarshal / Buffer.Memmove internals we don't model).
Dn2CppArrayI4* dn2cpp_array_subarray_i4(Dn2CppArrayI4* src, int32_t offset, int32_t length)
{
    if (src == nullptr)
        dn2cpp_throw_argument_null(); // catchable, matching ThrowHelper.ThrowArgumentNullException
    Dn2CppArrayI4* dst = dn2cpp_newarr_i4_t(length, src->type);
    if (length > 0)
        std::memcpy(dst->data, src->data + offset, static_cast<size_t>(length) * sizeof(int32_t));
    return dst;
}

Dn2CppArrayRef* dn2cpp_array_subarray_ref(Dn2CppArrayRef* src, int32_t offset, int32_t length)
{
    if (src == nullptr)
        dn2cpp_throw_argument_null(); // catchable, matching ThrowHelper.ThrowArgumentNullException
    Dn2CppArrayRef* dst = dn2cpp_newarr_ref_t(length, src->type);
    if (length > 0)
        dn2cpp_gc_memmove_refs(dst->data, src->data + offset,
            static_cast<size_t>(length) * sizeof(Dn2CppObject*));
    return dst;
}

Dn2CppArrayN* dn2cpp_array_subarray_n(Dn2CppArrayN* src, int32_t offset, int32_t length)
{
    if (src == nullptr)
        dn2cpp_throw_argument_null(); // catchable, matching ThrowHelper.ThrowArgumentNullException
    Dn2CppArrayN* dst = dn2cpp_newarr_n_t(length, src->elemSize, src->type);
    if (length > 0)
        dn2cpp_gc_memmove_refs(dst->data,
            src->data + static_cast<size_t>(offset) * src->elemSize,
            static_cast<size_t>(length) * static_cast<size_t>(src->elemSize));
    return dst;
}

Dn2CppArrayI4* dn2cpp_newarr_i4(int32_t length)
{
    return dn2cpp_newarr_i4_t(length, &dn2cpp_array_i4_type);
}

Dn2CppArrayRef* dn2cpp_newarr_ref(int32_t length)
{
    return dn2cpp_newarr_ref_t(length, &dn2cpp_array_ref_type);
}

Dn2CppArrayN* dn2cpp_newarr_n(int32_t length, int32_t elemSize)
{
    return dn2cpp_newarr_n_t(length, elemSize, &dn2cpp_array_n_type);
}

Dn2CppArrayRef* dn2cpp_argv_to_string_array(int argc, char** argv, const Dn2CppTypeInfo* ti)
{
    // .NET's args is argv[1..] — the program name (argv[0]) is excluded.
    int32_t n = argc > 1 ? static_cast<int32_t>(argc - 1) : 0;
    Dn2CppArrayRef* arr = dn2cpp_newarr_ref_t(n, ti);
    for (int32_t i = 0; i < n; i++)
    {
        const char* s = argv[i + 1];
        dn2cpp_gc_store_ref(&arr->data[i], reinterpret_cast<Dn2CppObject*>(
            dn2cpp_string_from_utf8(s, static_cast<int32_t>(std::strlen(s)))));
    }
    return arr;
}

// The negative-rank check stays an abort because no caller can produce one: there are
// two mouths, and both validate first — MethodCompiler.Tokens emits the rank as a
// literal from the IL's array shape, and dn2cpp_array_create_instance calls in only
// under `rank > 1` after validating the caller's lengths (which is where a bad request
// becomes the catchable ArgumentException real .NET gives). It is a backstop against a
// third mouth added without validation, and a backstop that threw could be swallowed.
Dn2CppMDArray* dn2cpp_newmdarr(const Dn2CppTypeInfo* ti, int32_t rank, const int32_t* lengths, int32_t elemSize)
{
    if (rank < 0)
        dn2cpp_fail("ArgumentOutOfRangeException (negative rank)");
    int32_t totalLength = 1;
    for (int32_t i = 0; i < rank; i++)
    {
        if (lengths[i] < 0)
            dn2cpp_overflow();
        if (lengths[i] > 0 && totalLength > 0x7fffffff / lengths[i])
            dn2cpp_overflow();
        totalLength *= lengths[i];
    }
    
    size_t lengthsSize = rank * sizeof(int32_t);
    size_t boundsSize = rank * sizeof(int32_t);
    size_t dataSize = static_cast<size_t>(totalLength) * elemSize;
    
    size_t totalAllocSize = sizeof(Dn2CppMDArray);
    size_t lengthsOffset = totalAllocSize;
    totalAllocSize += lengthsSize;
    
    size_t boundsOffset = totalAllocSize;
    totalAllocSize += boundsSize;
    
    size_t dataAlignment = (elemSize >= 8) ? 8 : (elemSize >= 4 ? 4 : 1);
    size_t dataOffset = (totalAllocSize + dataAlignment - 1) & ~(dataAlignment - 1);
    totalAllocSize = dataOffset + dataSize;
    
    auto* arr = static_cast<Dn2CppMDArray*>(dn2cpp_alloc(totalAllocSize));
    arr->type = ti;
    arr->rank = rank;
    arr->lengths = reinterpret_cast<int32_t*>(reinterpret_cast<char*>(arr) + lengthsOffset);
    arr->lowerBounds = reinterpret_cast<int32_t*>(reinterpret_cast<char*>(arr) + boundsOffset);
    arr->elemSize = elemSize;
    arr->data = reinterpret_cast<char*>(arr) + dataOffset;
    
    for (int32_t i = 0; i < rank; i++)
    {
        arr->lengths[i] = lengths[i];
        arr->lowerBounds[i] = 0;
    }
    
    return arr;
}

// Convert.ToChar(string) — exactly one UTF-16 code unit, else FormatException
// (ArgumentNull on a null string), matching .NET.
char16_t dn2cpp_convert_str_to_char(Dn2CppString* s)
{
    if (s == nullptr)
        dn2cpp_throw_argument_null();
    if (s->length != 1)
        dn2cpp_throw_sr1(&dn2cpp_format_exception_type, DN2CPP_SR_NEED_SINGLE_CHAR, nullptr);
    return s->chars[0];
}

// Convert.ToChar over an integer source — OverflowException outside the
// UTF-16 code-unit range, matching .NET.
char16_t dn2cpp_convert_i32_to_char(int32_t v)
{
    if (v < 0 || v > 0xFFFF)
        dn2cpp_overflow();
    return static_cast<char16_t>(v);
}

// ---- string.Intern / IsInterned ----
// Process-wide intern pool. Lookup is by string CONTENTS through an unscanned
// std::unordered_map whose key views the pooled entry's own UTF-16 buffer; each
// pooled Dn2CppString* additionally sits in a pinned (GC-scanned, uncollectable)
// cell so the pool roots its strings for the process lifetime, as in .NET.
// Every ldstr literal is interned at startup (dn2cpp_string_literal routes in
// here), matching .NET's literal interning.
namespace
{
struct Dn2CppStringInternCell
{
    Dn2CppString* str;
};

struct Dn2CppStringInternPool
{
    std::mutex mutex;
    std::unordered_map<std::u16string_view, Dn2CppStringInternCell*> entries;
};

Dn2CppStringInternPool& dn2cpp_string_intern_pool()
{
    static Dn2CppStringInternPool& pool = dn2cpp_never_destroyed<Dn2CppStringInternPool>();
    return pool;
}

std::u16string_view dn2cpp_string_intern_key(Dn2CppString* s)
{
    return { s->length > 0 ? s->chars : u"", static_cast<size_t>(s->length) };
}
} // namespace

Dn2CppString* dn2cpp_string_intern(Dn2CppString* s)
{
    if (s == nullptr)
        dn2cpp_throw_argument_null();
    auto& pool = dn2cpp_string_intern_pool();
    std::lock_guard<std::mutex> lk(pool.mutex);
    auto it = pool.entries.find(dn2cpp_string_intern_key(s));
    if (it != pool.entries.end())
        return it->second->str;
    auto* cell = static_cast<Dn2CppStringInternCell*>(dn2cpp_alloc_pinned(sizeof(Dn2CppStringInternCell)));
    dn2cpp_gc_store_ref(&cell->str, s);
    pool.entries.emplace(dn2cpp_string_intern_key(s), cell);
    return s;
}

Dn2CppString* dn2cpp_string_is_interned(Dn2CppString* s)
{
    if (s == nullptr)
        dn2cpp_throw_argument_null();
    auto& pool = dn2cpp_string_intern_pool();
    std::lock_guard<std::mutex> lk(pool.mutex);
    auto it = pool.entries.find(dn2cpp_string_intern_key(s));
    return it != pool.entries.end() ? it->second->str : nullptr;
}

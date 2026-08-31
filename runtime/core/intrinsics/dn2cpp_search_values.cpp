// dn2cpp_search_values.cpp — System.Buffers.SearchValues<byte|char> intrinsics.
//
// SearchValues.Create builds a membership set; MemoryExtensions.IndexOfAny(span, sv)
// scans a span for the first/last element in that set. The real BCL picks a
// SIMD/ProbabilisticMap subclass per the value distribution — all JIT intrinsics —
// so Create + IndexOfAny are intercepted and bound to these scalar set helpers
// instead, matching .NET semantics.
#include "dn2cpp_core.h"

#include <cstdint>
#include <cstring>
#include <algorithm>

// Build the set from the Create values. The struct is GC-scanned (dn2cpp_alloc) so the
// `hi` overflow array — and the set itself, via a static field — stay reachable; `hi`
// holds no managed pointers so it uses the atomic (unscanned) heap.
template <typename T>
static Dn2CppSearchValues* sv_create(const T* vals, int32_t n)
{
    Dn2CppSearchValues* sv = static_cast<Dn2CppSearchValues*>(dn2cpp_alloc(sizeof(Dn2CppSearchValues)));
    int32_t hiN = 0;
    for (int32_t i = 0; i < n; i++)
        if (static_cast<uint32_t>(vals[i]) > 255u) hiN++;
    if (hiN > 0)
    {
        dn2cpp_gc_store_ref(
            &sv->hi,
            static_cast<int32_t*>(dn2cpp_alloc_atomic(static_cast<size_t>(hiN) * sizeof(int32_t))));
        int32_t k = 0;
        for (int32_t i = 0; i < n; i++)
        {
            uint32_t v = static_cast<uint32_t>(vals[i]);
            if (v > 255u) sv->hi[k++] = static_cast<int32_t>(v);
            else sv->set8[v] = 1;
        }
        std::sort(sv->hi, sv->hi + k);
        sv->hiCount = k;
    }
    else
    {
        for (int32_t i = 0; i < n; i++)
            sv->set8[static_cast<uint8_t>(vals[i])] = 1;
    }
    return sv;
}

Dn2CppSearchValues* dn2cpp_search_values_create_u8(const uint8_t* vals, int32_t n) { return sv_create<uint8_t>(vals, n); }
Dn2CppSearchValues* dn2cpp_search_values_create_u16(const uint16_t* vals, int32_t n) { return sv_create<uint16_t>(vals, n); }

static inline bool sv_contains(const Dn2CppSearchValues* sv, uint32_t v)
{
    if (v <= 255u) return sv->set8[v] != 0;
    return sv->hiCount > 0
        && std::binary_search(sv->hi, sv->hi + sv->hiCount, static_cast<int32_t>(v));
}

// except=0 -> first index in the set; except=1 -> first index NOT in the set.
template <typename T>
static int32_t sv_index_of_any(const T* span, int32_t n, const Dn2CppSearchValues* sv, int32_t except)
{
    bool want = except == 0;
    for (int32_t i = 0; i < n; i++)
        if (sv_contains(sv, static_cast<uint32_t>(span[i])) == want) return i;
    return -1;
}

template <typename T>
static int32_t sv_last_index_of_any(const T* span, int32_t n, const Dn2CppSearchValues* sv, int32_t except)
{
    bool want = except == 0;
    for (int32_t i = n - 1; i >= 0; i--)
        if (sv_contains(sv, static_cast<uint32_t>(span[i])) == want) return i;
    return -1;
}

int32_t dn2cpp_search_values_index_of_any_u8(const uint8_t* span, int32_t n, const Dn2CppSearchValues* sv, int32_t except) { return sv_index_of_any<uint8_t>(span, n, sv, except); }
int32_t dn2cpp_search_values_index_of_any_u16(const uint16_t* span, int32_t n, const Dn2CppSearchValues* sv, int32_t except) { return sv_index_of_any<uint16_t>(span, n, sv, except); }
int32_t dn2cpp_search_values_last_index_of_any_u8(const uint8_t* span, int32_t n, const Dn2CppSearchValues* sv, int32_t except) { return sv_last_index_of_any<uint8_t>(span, n, sv, except); }
int32_t dn2cpp_search_values_last_index_of_any_u16(const uint16_t* span, int32_t n, const Dn2CppSearchValues* sv, int32_t except) { return sv_last_index_of_any<uint16_t>(span, n, sv, except); }

// SearchValues<byte|char>.Contains(value) — the single-element membership test
// (e.g. Path/ZipArchiveEntry name sanitization: sv.Contains(c) over the set of
// invalid path chars). Same set as the IndexOfAny scans above.
int32_t dn2cpp_search_values_contains_u8(uint8_t value, const Dn2CppSearchValues* sv) { return sv_contains(sv, static_cast<uint32_t>(value)) ? 1 : 0; }
int32_t dn2cpp_search_values_contains_u16(uint16_t value, const Dn2CppSearchValues* sv) { return sv_contains(sv, static_cast<uint32_t>(value)) ? 1 : 0; }

// SearchValues<string>: SearchValues.Create(ReadOnlySpan<string>, StringComparison)
// (only Ordinal / OrdinalIgnoreCase reach here — .NET rejects other comparisons)
// -> a leftmost multi-substring scan (SearchValues<string>.IndexOfAnyMultiString,
// Regex's leading-strings prefix optimization). The candidate array is GC-scanned
// so the strings stay alive through the set.
//
// A NULL candidate raises a catchable ArgumentNullException (parameter
// "values"): a candidate set assembled from configuration is untrusted like any
// other list, and one null row must not end the process.
//
// An EMPTY candidate is not a fault — .NET accepts it, and the accepted shape is
// total: an empty candidate makes IndexOfAny answer 0 for EVERY input, the empty
// span included ("".AsSpan().IndexOfAny(sv) == 0, not -1). The scan below models
// that by running its start position to n INCLUSIVE.
Dn2CppSearchValues* dn2cpp_search_values_create_str(Dn2CppString** vals, int32_t n, int32_t ignoreCase)
{
    Dn2CppSearchValues* sv = static_cast<Dn2CppSearchValues*>(dn2cpp_alloc(sizeof(Dn2CppSearchValues)));
    dn2cpp_gc_store_ref(
        &sv->strs,
        static_cast<Dn2CppString**>(dn2cpp_alloc(static_cast<size_t>(n) * sizeof(Dn2CppString*))));
    for (int32_t i = 0; i < n; i++)
    {
        if (vals[i] == nullptr)
            dn2cpp_throw_argument_null();
        dn2cpp_gc_store_ref(&sv->strs[i], vals[i]);
    }
    sv->strCount = n;
    sv->strIgnoreCase = ignoreCase;
    return sv;
}

// Leftmost multi-substring scan. The start position runs to n INCLUSIVE, which is
// not an off-by-one: at i == n every candidate of non-zero length fails the
// `v->length > n - i` guard, so the extra position is reachable only by a
// ZERO-LENGTH candidate — without it an empty candidate over an empty span would
// answer -1 where .NET answers 0. `span + n` is formed but never dereferenced
// (memcmp / the fold loop take length 0).
int32_t dn2cpp_search_values_index_of_any_str(const char16_t* span, int32_t n, const Dn2CppSearchValues* sv)
{
    for (int32_t i = 0; i <= n; i++)
    {
        for (int32_t k = 0; k < sv->strCount; k++)
        {
            const Dn2CppString* v = sv->strs[k];
            if (v->length > n - i)
                continue;
            if (sv->strIgnoreCase
                    ? dn2cpp_chars_equals_ignorecase_ordinal(span + i, v->chars, v->length) != 0
                    : std::memcmp(span + i, v->chars, static_cast<size_t>(v->length) * sizeof(char16_t)) == 0)
                return i;
        }
    }
    return -1;
}

// dn2cpp_system_array.cpp — System.Array (and Span) sort/reverse intrinsics.
//
// The transpiler intercepts Array.Sort / Array.Reverse and the Span<T> sort
// overloads, binding them to these std::sort / std::stable_sort / std::swap based
// routines with the BCL's typed comparers (i4/i8/r8/ref/string-ordinal), plus the
// non-generic Array surface (GetValue/SetValue/CreateInstance).
#include "dn2cpp_core.h"

#include <cstdint>
#include <cstring>
#include <algorithm>
#include <mutex> // fabricated array type-info interning (Array.CreateInstance)
#include <vector>

// ---- Array.Sort / Array.Reverse ----

int32_t dn2cpp_str_compare_ordinal(Dn2CppString* a, Dn2CppString* b)
{
    int32_t n = a->length < b->length ? a->length : b->length;
    for (int32_t i = 0; i < n; i++)
        if (a->chars[i] != b->chars[i])
            return static_cast<int32_t>(a->chars[i]) - static_cast<int32_t>(b->chars[i]);
    return a->length - b->length;
}

void dn2cpp_array_reverse_i4(Dn2CppArrayI4* a, int32_t start, int32_t count)
{
    for (int32_t i = start, j = start + count - 1; i < j; i++, j--)
        std::swap(a->data[i], a->data[j]);
}

void dn2cpp_array_reverse_ref(Dn2CppArrayRef* a, int32_t start, int32_t count)
{
    for (int32_t i = start, j = start + count - 1; i < j; i++, j--)
        std::swap(a->data[i], a->data[j]);
}

void dn2cpp_array_reverse_n(Dn2CppArrayN* a, int32_t start, int32_t count)
{
    int32_t es = a->elemSize;
    char* base = a->data + static_cast<size_t>(start) * static_cast<size_t>(es);
    // Reverse whole elements at a time. The typed views are sound because the data
    // is 16-byte aligned and `start * es` keeps `base` aligned to `es`.
    switch (es)
    {
        case 1: std::reverse(reinterpret_cast<uint8_t*>(base),  reinterpret_cast<uint8_t*>(base)  + count); return;
        case 2: std::reverse(reinterpret_cast<uint16_t*>(base), reinterpret_cast<uint16_t*>(base) + count); return;
        case 4: std::reverse(reinterpret_cast<uint32_t*>(base), reinterpret_cast<uint32_t*>(base) + count); return;
        case 8: std::reverse(reinterpret_cast<uint64_t*>(base), reinterpret_cast<uint64_t*>(base) + count); return;
        default: break;
    }
    char stackTmp[64];
    char* tmp = es <= static_cast<int32_t>(sizeof(stackTmp))
        ? stackTmp
        : static_cast<char*>(dn2cpp_native_alloc(static_cast<size_t>(es)));
    for (int32_t i = 0, j = count - 1; i < j; i++, j--)
    {
        char* pi = base + static_cast<size_t>(i) * static_cast<size_t>(es);
        char* pj = base + static_cast<size_t>(j) * static_cast<size_t>(es);
        std::memcpy(tmp, pi, static_cast<size_t>(es));
        std::memcpy(pi, pj, static_cast<size_t>(es));
        std::memcpy(pj, tmp, static_cast<size_t>(es));
    }
    if (tmp != stackTmp)
        dn2cpp_native_free(tmp);
}

void dn2cpp_array_sort_i4(Dn2CppArrayI4* a, int32_t start, int32_t count)
{
    std::sort(a->data + start, a->data + start + count);
}

void dn2cpp_array_sort_i8(Dn2CppArrayN* a, int32_t start, int32_t count)
{
    auto* d = reinterpret_cast<int64_t*>(a->data);
    std::sort(d + start, d + start + count);
}

// Double.CompareTo's total order: NaN below every number (including -inf) and equal to
// itself, +0.0 and -0.0 equal. A raw `<` predicate is not a strict weak ordering — NaN
// compares false both ways — so std::sort over an array holding one would be undefined
// behavior. Matches the order the transpiler inlines (MethodCompiler.TryCompareLValue).
template <typename F>
static inline bool dn2cpp_float_less(F x, F y)
{
    if (x < y)
        return true;
    if (y < x || x == y)
        return false;
    return x != x && y == y; // x is NaN and y is not: NaN sorts first
}

void dn2cpp_array_sort_r8(Dn2CppArrayN* a, int32_t start, int32_t count)
{
    auto* d = reinterpret_cast<double*>(a->data);
    std::sort(d + start, d + start + count, dn2cpp_float_less<double>);
}

void dn2cpp_array_sort_str(Dn2CppArrayRef* a, int32_t start, int32_t count)
{
    std::sort(a->data + start, a->data + start + count, [](Dn2CppObject* x, Dn2CppObject* y) {
        return dn2cpp_str_compare_ordinal(reinterpret_cast<Dn2CppString*>(x), reinterpret_cast<Dn2CppString*>(y)) < 0;
    });
}

// Comparer-driven sort: order `count` elements from `start` by a managed
// IComparer<T>.Compare, reached through `cmp(ctx, x, y)` (the transpiler emits a
// captureless thunk that does the interface dispatch; `ctx` is the comparer). A
// stable sort gives deterministic equal-key order (.NET's Sort is unspecified for
// equal keys). The caller routes a null comparer to the natural-order helpers above.
void dn2cpp_array_sort_cmp_i4(Dn2CppArrayI4* a, int32_t start, int32_t count, void* ctx, int32_t (*cmp)(void*, int32_t, int32_t))
{
    std::stable_sort(a->data + start, a->data + start + count,
        [=](int32_t x, int32_t y) { return cmp(ctx, x, y) < 0; });
}

void dn2cpp_array_sort_cmp_i8(Dn2CppArrayN* a, int32_t start, int32_t count, void* ctx, int32_t (*cmp)(void*, int64_t, int64_t))
{
    auto* d = reinterpret_cast<int64_t*>(a->data);
    std::stable_sort(d + start, d + start + count,
        [=](int64_t x, int64_t y) { return cmp(ctx, x, y) < 0; });
}

void dn2cpp_array_sort_cmp_r8(Dn2CppArrayN* a, int32_t start, int32_t count, void* ctx, int32_t (*cmp)(void*, double, double))
{
    auto* d = reinterpret_cast<double*>(a->data);
    std::stable_sort(d + start, d + start + count,
        [=](double x, double y) { return cmp(ctx, x, y) < 0; });
}

// Reference elements + managed comparer need a hand-written merge sort:
// std::stable_sort spills runs into an operator-new buffer the collector never scans,
// and the comparer is arbitrary managed code that may allocate — a collection then
// would reclaim elements whose only reference sits in the spill. Bottom-up merge
// through GC-visible scratch instead; both ping-pong buffers are scanned memory and
// the full element set is in `src` during every pass. A stable sort's result is
// unique, so the order matches std::stable_sort exactly.
static void dn2cpp_stable_sort_ref(Dn2CppObject** p, int32_t n, void* ctx,
    int32_t (*cmp)(void*, Dn2CppObject*, Dn2CppObject*))
{
    if (n < 2)
        return;
    auto** scratch = static_cast<Dn2CppObject**>(
        dn2cpp_alloc(sizeof(Dn2CppObject*) * static_cast<size_t>(n)));
    Dn2CppObject** src = p;
    Dn2CppObject** dst = scratch;
    for (int32_t width = 1; width < n; width *= 2)
    {
        for (int32_t lo = 0; lo < n; lo += 2 * width)
        {
            int32_t mid = lo + width < n ? lo + width : n;
            int32_t hi = lo + 2 * width < n ? lo + 2 * width : n;
            int32_t i = lo, j = mid, k = lo;
            while (i < mid && j < hi)
                dst[k++] = cmp(ctx, src[j], src[i]) < 0 ? src[j++] : src[i++]; // ties take the left run => stable
            while (i < mid)
                dst[k++] = src[i++];
            while (j < hi)
                dst[k++] = src[j++];
        }
        Dn2CppObject** t = src;
        src = dst;
        dst = t;
    }
    if (src != p)
        std::memcpy(p, src, sizeof(Dn2CppObject*) * static_cast<size_t>(n));
}

void dn2cpp_array_sort_cmp_ref(Dn2CppArrayRef* a, int32_t start, int32_t count, void* ctx, int32_t (*cmp)(void*, Dn2CppObject*, Dn2CppObject*))
{
    dn2cpp_stable_sort_ref(a->data + start, count, ctx, cmp);
}

// The boxed three-way an object-element sort/search compares with: an explicit
// System.Collections.IComparer.Compare dispatched through the interface the caller names, else
// (including a null comparer — that IS Comparer.Default) dn2cpp_object_compare. `cmp(a,b) < 0`
// means a sorts before b, the convention dn2cpp_array_sort_cmp_ref expects.
namespace {
struct Dn2CppBoxedOrderCtx
{
    const Dn2CppTypeInfo* icomparable_ti;
    Dn2CppObject* comparer;
    const Dn2CppTypeInfo* icomparer_ti;
    int32_t comparer_slot;
};
int32_t dn2cpp_boxed_order(void* ctxp, Dn2CppObject* a, Dn2CppObject* b)
{
    auto* c = static_cast<Dn2CppBoxedOrderCtx*>(ctxp);
    if (c->comparer != nullptr)
    {
        const void** slots = dn2cpp_resolve_interface(c->comparer->type, c->icomparer_ti);
        return (reinterpret_cast<int32_t (*)(Dn2CppObject*, Dn2CppObject*, Dn2CppObject*)>(
                    const_cast<void*>(slots[c->comparer_slot])))(c->comparer, a, b);
    }
    return dn2cpp_object_compare(a, b, c->icomparable_ti);
}
} // namespace

void dn2cpp_array_sort_object(Dn2CppObject* arr, int32_t index, int32_t length,
                              const Dn2CppTypeInfo* icomparable_ti, Dn2CppObject* comparer,
                              const Dn2CppTypeInfo* icomparer_ti, int32_t comparer_slot)
{
    if (arr == nullptr)
        dn2cpp_throw_argument_null();
    if (length <= 1)
        return;
    // Box the [index, index+length) window into a managed (GC-scanned) ref buffer, so the boxes
    // survive a collection a comparison may trigger; buf itself is stack-rooted here. Sort the
    // buffer with the proven ref primitive, then write the permutation back — dn2cpp_array_set_value
    // applies real .NET's SetValue coercion (unbox to the element's stored rep, widening/exactness).
    Dn2CppArrayRef* buf = dn2cpp_newarr_ref(length);
    for (int32_t i = 0; i < length; i++)
        buf->data[i] = dn2cpp_array_get_value(arr, static_cast<int64_t>(index + i));
    Dn2CppBoxedOrderCtx ctx{ icomparable_ti, comparer, icomparer_ti, comparer_slot };
    dn2cpp_array_sort_cmp_ref(buf, 0, length, &ctx, &dn2cpp_boxed_order);
    for (int32_t i = 0; i < length; i++)
        dn2cpp_array_set_value(arr, buf->data[i], static_cast<int64_t>(index + i));
}

// Element-sized elements (struct / long / double / short / 64-bit enum …) + a managed
// comparer. Elements move as opaque byte blocks and reach the comparer by ADDRESS — a
// struct of arbitrary width has no uniform by-value C signature — so this one routine
// serves every element type the i4/ref reps do not.
//
// Same GC constraint as dn2cpp_stable_sort_ref, and stricter: an element may *contain*
// managed references, so the scratch must be GC-visible rather than operator-new.
static void dn2cpp_stable_sort_n(char* base, int32_t n, size_t es, void* ctx,
    int32_t (*cmp)(void*, const void*, const void*))
{
    if (n < 2)
        return;
    char* scratch = static_cast<char*>(dn2cpp_alloc(es * static_cast<size_t>(n)));
    char* src = base;
    char* dst = scratch;
    for (int32_t width = 1; width < n; width *= 2)
    {
        for (int32_t lo = 0; lo < n; lo += 2 * width)
        {
            int32_t mid = lo + width < n ? lo + width : n;
            int32_t hi = lo + 2 * width < n ? lo + 2 * width : n;
            int32_t i = lo, j = mid, k = lo;
            while (i < mid && j < hi)
            {
                // Ties take the left run => stable.
                int32_t s = cmp(ctx, src + static_cast<size_t>(j) * es,
                                     src + static_cast<size_t>(i) * es) < 0 ? j++ : i++;
                std::memcpy(dst + static_cast<size_t>(k++) * es, src + static_cast<size_t>(s) * es, es);
            }
            while (i < mid)
                std::memcpy(dst + static_cast<size_t>(k++) * es, src + static_cast<size_t>(i++) * es, es);
            while (j < hi)
                std::memcpy(dst + static_cast<size_t>(k++) * es, src + static_cast<size_t>(j++) * es, es);
        }
        char* t = src;
        src = dst;
        dst = t;
    }
    if (src != base)
        std::memcpy(base, src, es * static_cast<size_t>(n));
}

void dn2cpp_array_sort_cmp_n(Dn2CppArrayN* a, int32_t start, int32_t count, void* ctx,
                             int32_t (*cmp)(void*, const void*, const void*))
{
    size_t es = static_cast<size_t>(a->elemSize);
    dn2cpp_stable_sort_n(a->data + static_cast<size_t>(start) * es, count, es, ctx, cmp);
}

void dn2cpp_span_sort_cmp_n(void* p, int32_t n, int32_t elemSize, void* ctx,
                            int32_t (*cmp)(void*, const void*, const void*))
{
    dn2cpp_stable_sort_n(static_cast<char*>(p), n, static_cast<size_t>(elemSize), ctx, cmp);
}

// The parallel key+value sort: Array.Sort<TKey,TValue>(TKey[] keys, TValue[] items[, cmp])
// and MemoryExtensions.Sort<TKey,TValue>(Span keys, Span items[, cmp]). `keys`/`items` are
// the two element buffers (an array's data, a span's f__reference) with the strides their
// reps pack at; `items` may be null (a bare key sort — .NET allows it). Keys reach the
// comparison by address, so one routine serves every key rep.
//
// Sort an index permutation by key, then permute both buffers through scratch. The index
// vector holds no managed reference, so the comparer may allocate and collect while every
// element is still in its original slot; the permute phase runs no managed code. The
// scratch is dn2cpp_alloc, not std::vector: an operator-new buffer is invisible to the
// collector, and a reference living only there — even across a memcpy — is a hole.
//
// stable_sort keeps a deterministic equal-key order (.NET's introsort promises none).
void dn2cpp_sort_pair(void* keys, int32_t keyStride, void* items, int32_t itemStride,
                      int32_t start, int32_t count, void* ctx,
                      int32_t (*cmp)(void*, const void*, const void*))
{
    if (count < 2)
        return;
    size_t ks = static_cast<size_t>(keyStride);
    size_t vs = static_cast<size_t>(itemStride);
    char* k = static_cast<char*>(keys) + static_cast<size_t>(start) * ks;
    char* v = items != nullptr
        ? static_cast<char*>(items) + static_cast<size_t>(start) * vs
        : nullptr;

    std::vector<int32_t> idx(static_cast<size_t>(count));
    for (int32_t i = 0; i < count; i++)
        idx[static_cast<size_t>(i)] = i;
    std::stable_sort(idx.begin(), idx.end(), [=](int32_t a, int32_t b) {
        return cmp(ctx, k + static_cast<size_t>(a) * ks, k + static_cast<size_t>(b) * ks) < 0;
    });

    char* kbuf = static_cast<char*>(dn2cpp_alloc(ks * static_cast<size_t>(count)));
    char* vbuf = v != nullptr
        ? static_cast<char*>(dn2cpp_alloc(vs * static_cast<size_t>(count)))
        : nullptr;
    for (int32_t i = 0; i < count; i++)
    {
        size_t src = static_cast<size_t>(idx[static_cast<size_t>(i)]);
        std::memcpy(kbuf + static_cast<size_t>(i) * ks, k + src * ks, ks);
        if (v != nullptr)
            std::memcpy(vbuf + static_cast<size_t>(i) * vs, v + src * vs, vs);
    }
    std::memcpy(k, kbuf, ks * static_cast<size_t>(count));
    if (v != nullptr)
        std::memcpy(v, vbuf, vs * static_cast<size_t>(count));
}

// span.Sort: same algorithms over a raw element pointer + length (the span's
// f__reference/f__length). Natural order, then comparer-driven (stable, deterministic
// equal-key order like the array path).
void dn2cpp_span_sort_i4(int32_t* p, int32_t n) { std::sort(p, p + n); }
void dn2cpp_span_sort_i8(int64_t* p, int32_t n) { std::sort(p, p + n); }
void dn2cpp_span_sort_r8(double* p, int32_t n) { std::sort(p, p + n, dn2cpp_float_less<double>); }

void dn2cpp_span_sort_str(Dn2CppObject** p, int32_t n)
{
    std::sort(p, p + n, [](Dn2CppObject* x, Dn2CppObject* y) {
        return dn2cpp_str_compare_ordinal(reinterpret_cast<Dn2CppString*>(x), reinterpret_cast<Dn2CppString*>(y)) < 0;
    });
}

void dn2cpp_span_sort_cmp_i4(int32_t* p, int32_t n, void* ctx, int32_t (*cmp)(void*, int32_t, int32_t))
{
    std::stable_sort(p, p + n, [=](int32_t x, int32_t y) { return cmp(ctx, x, y) < 0; });
}

void dn2cpp_span_sort_cmp_i8(int64_t* p, int32_t n, void* ctx, int32_t (*cmp)(void*, int64_t, int64_t))
{
    std::stable_sort(p, p + n, [=](int64_t x, int64_t y) { return cmp(ctx, x, y) < 0; });
}

void dn2cpp_span_sort_cmp_r8(double* p, int32_t n, void* ctx, int32_t (*cmp)(void*, double, double))
{
    std::stable_sort(p, p + n, [=](double x, double y) { return cmp(ctx, x, y) < 0; });
}

void dn2cpp_span_sort_cmp_ref(Dn2CppObject** p, int32_t n, void* ctx, int32_t (*cmp)(void*, Dn2CppObject*, Dn2CppObject*))
{
    dn2cpp_stable_sort_ref(p, n, ctx, cmp);
}

// The span key+value overload (MemoryExtensions.Sort<TKey,TValue>) is dn2cpp_sort_pair,
// shared with Array.Sort<TKey,TValue> — see above.

// ---- non-generic System.Array surface (GetValue/SetValue/CreateInstance) ----
// Element slots are read/written through the boxed-value conversion cores that live in
// the reflection unit beside the Activator argument binder (dn2cpp_array_box_element /
// dn2cpp_array_store_boxed / dn2cpp_prim_code / dn2cpp_prim_storage_width, declared in
// dn2cpp_core.h).

// A uniform runtime view over any array object: the element identity + packed
// storage, for SZ arrays of every rep (ref / packed-int32 / element-sized) and
// MD arrays (whose type-info carries arrayRank > 1). Faults loud on an object
// without array identity rather than guessing at its element type.
struct Dn2CppArrayViewRT
{
    const Dn2CppTypeInfo* elem;
    bool elemIsRef;
    int32_t rank;
    int32_t elemSize;
    char* data;
    Dn2CppMDArray* md; // non-null -> MD layout (lengths per dimension)
    int32_t length;    // total element count
};

static void dn2cpp_array_view_rt(Dn2CppObject* a, Dn2CppArrayViewRT* v)
{
    if (a == nullptr)
        dn2cpp_throw_null_reference();
    const Dn2CppTypeInfo* t = a->type;
    if (t == nullptr || (t->flags & DN2CPP_TF_ARRAY) == 0 || t->elementType == nullptr)
        dn2cpp_throw_platform_not_supported(
            "this array carries no element identity in this image (reflection over it is not supported)");
    const Dn2CppTypeInfo* el = t->elementType;
    v->elem = el;
    v->elemIsRef = (el->flags & DN2CPP_TF_VALUETYPE) == 0;
    if (t->arrayRank > 1)
    {
        auto* md = reinterpret_cast<Dn2CppMDArray*>(a);
        v->rank = md->rank;
        v->elemSize = md->elemSize;
        v->data = md->data;
        v->md = md;
        v->length = dn2cpp_md_total_length(md);
        return;
    }
    v->rank = 1;
    v->md = nullptr;
    if (v->elemIsRef)
    {
        auto* arr = reinterpret_cast<Dn2CppArrayRef*>(a);
        v->elemSize = static_cast<int32_t>(sizeof(Dn2CppObject*));
        v->data = reinterpret_cast<char*>(&arr->data[0]);
        v->length = arr->length;
        return;
    }
    bool isI4 = el == &dn2cpp_int32_type || el == &dn2cpp_uint32_type
        || ((el->flags & DN2CPP_TF_ENUM) != 0
            && (el->enumUnderlying == &dn2cpp_int32_type
                || el->enumUnderlying == &dn2cpp_uint32_type
                || el->enumUnderlying == nullptr));
    if (isI4)
    {
        auto* arr = reinterpret_cast<Dn2CppArrayI4*>(a);
        v->elemSize = 4;
        v->data = reinterpret_cast<char*>(&arr->data[0]);
        v->length = arr->length;
        return;
    }
    auto* arr = reinterpret_cast<Dn2CppArrayN*>(a);
    v->elemSize = arr->elemSize;
    v->data = &arr->data[0];
    v->length = arr->length;
}

// The flattened element slot for the (object, int/long index) forms. Real .NET:
// a multi-dimensional receiver throws ArgumentException ("Array was not a
// one-dimensional array"), an index beyond the 2 GB model ArgumentOutOfRange,
// an out-of-bounds index IndexOutOfRangeException (all probed).
static void* dn2cpp_array_slot_linear(const Dn2CppArrayViewRT* v, int64_t index)
{
    if (v->rank != 1)
        dn2cpp_throw_argument();
    if (index > INT32_MAX || index < INT32_MIN)
        dn2cpp_throw_argument_out_of_range();
    if (index < 0 || index >= v->length)
        dn2cpp_throw_index_out_of_range();
    return v->data + static_cast<size_t>(index) * static_cast<size_t>(v->elemSize);
}

// The element slot for the indices-array forms. Real .NET: null indices throw
// ArgumentNullException, a rank mismatch ArgumentException, out-of-bounds
// IndexOutOfRangeException (all probed).
static void* dn2cpp_array_slot_indices(const Dn2CppArrayViewRT* v, const int64_t* idx, int32_t n)
{
    if (n != v->rank)
        dn2cpp_throw_argument();
    int64_t flat = 0;
    for (int32_t d = 0; d < n; d++)
    {
        if (idx[d] > INT32_MAX || idx[d] < INT32_MIN)
            dn2cpp_throw_argument_out_of_range();
        int64_t len = v->md != nullptr ? v->md->lengths[d] : v->length;
        int64_t lo = v->md != nullptr ? v->md->lowerBounds[d] : 0;
        int64_t i = idx[d] - lo;
        if (i < 0 || i >= len)
            dn2cpp_throw_index_out_of_range();
        flat = flat * len + i;
    }
    return v->data + static_cast<size_t>(flat) * static_cast<size_t>(v->elemSize);
}

Dn2CppObject* dn2cpp_array_get_value(Dn2CppObject* a, int64_t index)
{
    Dn2CppArrayViewRT v;
    dn2cpp_array_view_rt(a, &v);
    return dn2cpp_array_box_element(v.elem, dn2cpp_array_slot_linear(&v, index), v.elemSize, v.elemIsRef);
}

void dn2cpp_array_set_value(Dn2CppObject* a, Dn2CppObject* value, int64_t index)
{
    Dn2CppArrayViewRT v;
    dn2cpp_array_view_rt(a, &v);
    dn2cpp_array_store_boxed(value, v.elem, dn2cpp_array_slot_linear(&v, index),
                             v.elemSize, v.elemIsRef);
}

// The indices copied out of the managed int[]/long[] argument (null ->
// ArgumentNullException; arity capped at the MD model's practical bound).
static int32_t dn2cpp_array_copy_indices(Dn2CppObject* indices, int32_t isLong, int64_t* out)
{
    if (indices == nullptr)
        dn2cpp_throw_argument_null();
    int32_t n;
    if (isLong != 0)
    {
        auto* arr = reinterpret_cast<Dn2CppArrayN*>(indices);
        n = arr->length;
        if (n > 32)
            dn2cpp_throw_argument();
        for (int32_t i = 0; i < n; i++)
            std::memcpy(&out[i], arr->data + static_cast<size_t>(i) * 8, 8);
    }
    else
    {
        auto* arr = reinterpret_cast<Dn2CppArrayI4*>(indices);
        n = arr->length;
        if (n > 32)
            dn2cpp_throw_argument();
        for (int32_t i = 0; i < n; i++)
            out[i] = arr->data[i];
    }
    return n;
}

Dn2CppObject* dn2cpp_array_get_value_indices(Dn2CppObject* a, Dn2CppObject* indices, int32_t isLong)
{
    Dn2CppArrayViewRT v;
    dn2cpp_array_view_rt(a, &v);
    int64_t idx[32];
    int32_t n = dn2cpp_array_copy_indices(indices, isLong, idx);
    return dn2cpp_array_box_element(v.elem, dn2cpp_array_slot_indices(&v, idx, n), v.elemSize, v.elemIsRef);
}

void dn2cpp_array_set_value_indices(Dn2CppObject* a, Dn2CppObject* value, Dn2CppObject* indices, int32_t isLong)
{
    Dn2CppArrayViewRT v;
    dn2cpp_array_view_rt(a, &v);
    int64_t idx[32];
    int32_t n = dn2cpp_array_copy_indices(indices, isLong, idx);
    dn2cpp_array_store_boxed(value, v.elem, dn2cpp_array_slot_indices(&v, idx, n),
                             v.elemSize, v.elemIsRef);
}

// The fixed-arity GetValue(int, int[, int]) / SetValue(object, int, int[, int])
// forms, lowered with an inline index pack.
Dn2CppObject* dn2cpp_array_get_value_fixed(Dn2CppObject* a, const int32_t* idx, int32_t n)
{
    Dn2CppArrayViewRT v;
    dn2cpp_array_view_rt(a, &v);
    int64_t idx64[3];
    for (int32_t i = 0; i < n; i++)
        idx64[i] = idx[i];
    return dn2cpp_array_box_element(v.elem, dn2cpp_array_slot_indices(&v, idx64, n), v.elemSize, v.elemIsRef);
}

void dn2cpp_array_set_value_fixed(Dn2CppObject* a, Dn2CppObject* value, const int32_t* idx, int32_t n)
{
    Dn2CppArrayViewRT v;
    dn2cpp_array_view_rt(a, &v);
    int64_t idx64[3];
    for (int32_t i = 0; i < n; i++)
        idx64[i] = idx[i];
    dn2cpp_array_store_boxed(value, v.elem, dn2cpp_array_slot_indices(&v, idx64, n),
                             v.elemSize, v.elemIsRef);
}

// Runtime-dispatched array shape queries, for receivers whose static type
// degraded to System.Array/object (the emitter keeps the fast inline forms for
// statically-typed receivers). An MD array is recognized by its type-info's
// arrayRank; a bad dimension throws IndexOutOfRangeException like real .NET.
static Dn2CppMDArray* dn2cpp_array_as_md(Dn2CppObject* a)
{
    if (a == nullptr)
        dn2cpp_throw_null_reference();
    const Dn2CppTypeInfo* t = a->type;
    if (t != nullptr && (t->flags & DN2CPP_TF_ARRAY) != 0 && t->arrayRank > 1)
        return reinterpret_cast<Dn2CppMDArray*>(a);
    return nullptr;
}

int32_t dn2cpp_array_rank_dyn(Dn2CppObject* a)
{
    Dn2CppMDArray* md = dn2cpp_array_as_md(a);
    return md != nullptr ? md->rank : 1;
}

int32_t dn2cpp_array_length_dyn(Dn2CppObject* a)
{
    Dn2CppMDArray* md = dn2cpp_array_as_md(a);
    return md != nullptr ? dn2cpp_md_total_length(md)
                         : reinterpret_cast<Dn2CppArray*>(a)->length;
}

int32_t dn2cpp_array_get_length_dyn(Dn2CppObject* a, int32_t dim)
{
    Dn2CppMDArray* md = dn2cpp_array_as_md(a);
    if (md != nullptr)
    {
        if (dim < 0 || dim >= md->rank)
            dn2cpp_throw_index_out_of_range();
        return md->lengths[dim];
    }
    if (dim != 0)
        dn2cpp_throw_index_out_of_range();
    return reinterpret_cast<Dn2CppArray*>(a)->length;
}

int32_t dn2cpp_array_get_lower_bound_dyn(Dn2CppObject* a, int32_t dim)
{
    Dn2CppMDArray* md = dn2cpp_array_as_md(a);
    if (md != nullptr)
    {
        if (dim < 0 || dim >= md->rank)
            dn2cpp_throw_index_out_of_range();
        return md->lowerBounds[dim];
    }
    if (dim != 0)
        dn2cpp_throw_index_out_of_range();
    return 0;
}

int32_t dn2cpp_array_get_upper_bound_dyn(Dn2CppObject* a, int32_t dim)
{
    return dn2cpp_array_get_lower_bound_dyn(a, dim) + dn2cpp_array_get_length_dyn(a, dim) - 1;
}

// Array.Reverse(Array[, int index, int length]) — the non-generic form, over
// the runtime element view (byte-wise element swaps; reference elements swap
// pointers). Real .NET: a multi-dimensional receiver throws Rank-flavored
// ArgumentException; bad (index, length) windows throw like the generic form.
void dn2cpp_array_reverse_dyn(Dn2CppObject* a, int32_t index, int32_t length)
{
    Dn2CppArrayViewRT v;
    dn2cpp_array_view_rt(a, &v);
    if (v.rank != 1)
        dn2cpp_throw_argument();
    if (index < 0 || length < 0)
        dn2cpp_throw_argument_out_of_range();
    if (v.length - index < length)
        dn2cpp_throw_argument();
    char tmp[64];
    int32_t w = v.elemSize;
    if (w > 64)
        dn2cpp_throw_platform_not_supported("Array.Reverse over >64-byte elements is not supported");
    char* lo = v.data + static_cast<size_t>(index) * w;
    char* hi = v.data + static_cast<size_t>(index + length - 1) * w;
    while (lo < hi)
    {
        std::memcpy(tmp, lo, w);
        std::memcpy(lo, hi, w);
        std::memcpy(hi, tmp, w);
        lo += w;
        hi -= w;
    }
}

// ---- Array.CreateInstance ----

// Fabricated array type-infos: Array.CreateInstance over an element type whose
// T[] was never statically instantiated needs a fresh array identity at run
// time (elementType + rank + the ARRAY flag) so GetValue/SetValue/GetType keep
// working over the result. Interned per (element, rank) behind a mutex and
// allocated uncollectable (a type-info lives for the process). Requests first
// resolve against the image's static registry (ti_arr_ rows at rank 1, ti_md_
// rows above it), so an in-image array type keeps its emitted identity.
struct Dn2CppDynArrayTiNode
{
    const Dn2CppTypeInfo* elem;
    int32_t rank;
    const Dn2CppTypeInfo* ti;
    Dn2CppDynArrayTiNode* next;
};

const Dn2CppTypeInfo* dn2cpp_array_ti(const Dn2CppTypeInfo* elem, int32_t rank)
{
    if (elem == nullptr || rank < 1)
        return nullptr;
    if (const Dn2CppTypeInfo* st = dn2cpp_find_array_ti_rank(elem, rank))
        return st;
    static std::mutex& mtx = dn2cpp_never_destroyed<std::mutex>();
    static Dn2CppDynArrayTiNode* head = nullptr;
    std::lock_guard<std::mutex> lk(mtx);
    for (Dn2CppDynArrayTiNode* n = head; n != nullptr; n = n->next)
        if (n->elem == elem && n->rank == rank)
            return n->ti;
    // "Elem[]" / "Elem[,]" / … — the CLR array type name.
    size_t base = std::strlen(elem->name);
    char* name = static_cast<char*>(dn2cpp_alloc_pinned(base + static_cast<size_t>(rank) + 2));
    std::memcpy(name, elem->name, base);
    name[base] = '[';
    for (int32_t i = 0; i < rank - 1; i++)
        name[base + 1 + i] = ',';
    name[base + static_cast<size_t>(rank)] = ']';
    name[base + static_cast<size_t>(rank) + 1] = '\0';
    auto* ti = static_cast<Dn2CppTypeInfo*>(dn2cpp_alloc_pinned(sizeof(Dn2CppTypeInfo)));
    *ti = Dn2CppTypeInfo{};
    ti->name = name;
    ti->flags = DN2CPP_TF_ARRAY | DN2CPP_TF_SEALED;
    ti->elementType = elem;
    ti->arrayRank = rank;
    auto* node = static_cast<Dn2CppDynArrayTiNode*>(dn2cpp_alloc_pinned(sizeof(Dn2CppDynArrayTiNode)));
    node->elem = elem;
    node->rank = rank;
    node->ti = ti;
    node->next = head;
    head = node;
    return ti;
}

// The MD-array type-info a `new T[,]` site stamps: interned like the above so
// two allocations of the same shape share one identity. Null element (a
// shared-generic body without a concrete element identity) keeps the legacy
// null header.
const Dn2CppTypeInfo* dn2cpp_mdarr_ti(const Dn2CppTypeInfo* elem, int32_t rank)
{
    if (elem == nullptr)
        return nullptr;
    return dn2cpp_array_ti(elem, rank);
}

// Array.CreateInstance(Type, lengths...): allocate under the element's storage
// rep (packed int32 / element-sized / reference), tagged with the (possibly
// fabricated) precise array identity. Matches real .NET's exceptions: null
// element type ArgumentNullException, void NotSupportedException, a negative
// length ArgumentOutOfRangeException. rank > 1 allocates the MD layout.
Dn2CppObject* dn2cpp_array_create_instance(Dn2CppType* t, const int32_t* lengths, int32_t rank)
{
    if (t == nullptr)
        dn2cpp_throw_argument_null();
    const Dn2CppTypeInfo* el = t->typeInfo;
    if (el == &dn2cpp_void_type)
        dn2cpp_throw_not_supported();
    // An EMPTY lengths array is ArgumentException in real .NET. Without this the
    // rank-1 arm below reads lengths[0] out of bounds — on a caller-supplied array,
    // whose length arrives from a deserializer or a reflective call site.
    if (rank <= 0)
        dn2cpp_throw_argument();
    for (int32_t i = 0; i < rank; i++)
        if (lengths[i] < 0)
            dn2cpp_throw_argument_out_of_range();
    bool isRef = (el->flags & DN2CPP_TF_VALUETYPE) == 0;
    bool isEnum = (el->flags & DN2CPP_TF_ENUM) != 0;
    const Dn2CppTypeInfo* eff = isEnum
        ? (el->enumUnderlying != nullptr ? el->enumUnderlying : &dn2cpp_int32_type)
        : el;
    int32_t code = isRef ? -1 : dn2cpp_prim_code(eff); // -1: not a primitive (dn2cpp_prim_code's miss value)
    int32_t elemSize;
    if (isRef)
        elemSize = static_cast<int32_t>(sizeof(Dn2CppObject*));
    else if (code >= 0)
        elemSize = dn2cpp_prim_storage_width(code);
    else if (el == &dn2cpp_intptr_type || el == &dn2cpp_uintptr_type)
        elemSize = static_cast<int32_t>(sizeof(intptr_t));
    else if (el->instanceSize > 0)
    {
        // The stride the elements are laid out at. An unmodeled-extent shell stamps 1,
        // which would build an array whose N elements overlap inside N bytes — silent
        // aliasing, so the layout bit traps here, before the allocation.
        dn2cpp_require_layout(el);
        elemSize = el->instanceSize;
    }
    else
        dn2cpp_throw_platform_not_supported(
            "Array.CreateInstance: the element type's layout is not modeled in this image");
    const Dn2CppTypeInfo* arrTi = dn2cpp_array_ti(el, rank);
    if (rank > 1)
        return reinterpret_cast<Dn2CppObject*>(dn2cpp_newmdarr(arrTi, rank, lengths, elemSize));
    int32_t len = lengths[0];
    if (isRef)
        return reinterpret_cast<Dn2CppObject*>(dn2cpp_newarr_ref_t(len, arrTi));
    bool isI4 = eff == &dn2cpp_int32_type || eff == &dn2cpp_uint32_type;
    if (!isRef && !isEnum && code < 0 && el != &dn2cpp_intptr_type && el != &dn2cpp_uintptr_type)
        // A struct element may hold references: keep the GC scanning it.
        return reinterpret_cast<Dn2CppObject*>(dn2cpp_newarr_n_t(len, elemSize, arrTi));
    if (isI4)
        return reinterpret_cast<Dn2CppObject*>(dn2cpp_newarr_i4_t(len, arrTi));
    return reinterpret_cast<Dn2CppObject*>(dn2cpp_newarr_n_atomic_t(len, elemSize, arrTi));
}

// The (Type, int[] lengths[, int[] lowerBounds]) forms. Non-zero lower bounds
// are not modeled (real .NET itself only supports them on Windows-style
// non-SZ arrays); all-zero bounds route to the plain form.
Dn2CppObject* dn2cpp_array_create_instance_lengths(Dn2CppType* t, Dn2CppArrayI4* lengths,
                                                   Dn2CppArrayI4* lowerBounds)
{
    if (lengths == nullptr)
        dn2cpp_throw_argument_null();
    if (lengths->length < 1 || lengths->length > 32)
        dn2cpp_throw_argument();
    if (lowerBounds != nullptr)
    {
        if (lowerBounds->length != lengths->length)
            dn2cpp_throw_argument();
        for (int32_t i = 0; i < lowerBounds->length; i++)
            if (lowerBounds->data[i] != 0)
                dn2cpp_throw_platform_not_supported(
                    "Array.CreateInstance with non-zero lower bounds is not supported");
    }
    return dn2cpp_array_create_instance(t, lengths->data, lengths->length);
}

// Array.CreateInstanceFromArrayType(Type arrayType, length(s)): the element
// comes off the ARRAY type's own identity (GetElementType), then the plain
// path applies. A non-array type throws ArgumentException like real .NET.
Dn2CppObject* dn2cpp_array_create_instance_from_arraytype(Dn2CppType* arrayType,
                                                          const int32_t* lengths, int32_t rank)
{
    if (arrayType == nullptr)
        dn2cpp_throw_argument_null();
    const Dn2CppTypeInfo* ti = arrayType->typeInfo;
    if ((ti->flags & DN2CPP_TF_ARRAY) == 0 || ti->elementType == nullptr)
        dn2cpp_throw_argument();
    return dn2cpp_array_create_instance(dn2cpp_get_type_from_handle(ti->elementType), lengths, rank);
}

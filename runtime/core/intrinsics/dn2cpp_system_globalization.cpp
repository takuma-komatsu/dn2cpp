// dn2cpp_system_globalization.cpp — System.Globalization intrinsics and the
// culture-aware numeric formatters they drive: the builtin culture table
// (kCultures, generated into dn2cpp_culture_table.inc; its membership rule is
// stated at the include site), the CurrentCulture / CurrentUICulture slots over
// the host default, a mutable NumberFormatInfo, and the *_c formatters that
// localize digits/sign/group/decimal symbols.
#include "dn2cpp_core.h"
#include "platform/dn2cpp_pal.h" // dn2cpp_pal_default_locale_name (CurrentCulture's default)

#include <cstdint>
#include <cstdio>
#include <cstring>
#include <cstdlib>
#include <cmath>
#include <mutex>   // std::call_once for thread-safe culture-table init

// ===== Culture / NumberFormatInfo ============================================
// A UTF-16 literal -> Dn2CppString (chars point at .rodata; header is GC-heap,
// kept alive as a static-data root). Used to build the culture singletons.
static Dn2CppString* dn2cpp_lit16(const char16_t* p)
{
    int32_t n = 0;
    while (p[n]) n++;
    return dn2cpp_string_literal(p, n);
}

static void dn2cpp_nfi_init_invariant(Dn2CppNumberFormatInfo* n)
{
    n->cultureName = dn2cpp_lit16(u"");
    n->lcid = 127; // the invariant culture's LCID (0x7F)
    n->numberDecimal = dn2cpp_lit16(u".");
    n->numberGroup = dn2cpp_lit16(u",");
    n->groupSizes[0] = 3;            // the invariant culture's NumberGroupSizes == [3]
    n->groupSizeCount = 1;
    n->negativeSign = dn2cpp_lit16(u"-");
    n->nan = dn2cpp_lit16(u"NaN");
    n->posInf = dn2cpp_lit16(u"Infinity");
    n->negInf = dn2cpp_lit16(u"-Infinity");
    n->percentSymbol = dn2cpp_lit16(u"%");
    n->percentDecimal = dn2cpp_lit16(u".");
    n->percentGroup = dn2cpp_lit16(u",");
    n->percentPosPattern = 0;
    n->percentNegPattern = 0;
    n->currencySymbol = dn2cpp_lit16(u"¤");
    n->currencyDecimal = dn2cpp_lit16(u".");
    n->currencyGroup = dn2cpp_lit16(u",");
    n->currencyDigits = 2;
    n->currencyPosPattern = 0;
    n->currencyNegPattern = 0;
}

const Dn2CppNumberFormatInfo* dn2cpp_nfi_invariant()
{
    // C++11 magic static: thread-safe once-initialization.
    static const Dn2CppNumberFormatInfo inv = [] {
        Dn2CppNumberFormatInfo n{};
        dn2cpp_nfi_init_invariant(&n);
        return n;
    }();
    return &inv;
}

// A null IFormatProvider means the CURRENT culture, not the invariant one
// (.NET's NumberFormatInfo.GetInstance(null) is CurrentInfo), in both
// directions: `d.ToString("N2", null)` formats in it and `double.Parse(s)` reads
// in it. Null arrives both as a literal argument and from the provider-less
// overloads, whose lowering is the same call with a null NFI. This is the single
// point at which the host default reaches the formatters. An explicitly
// invariant provider is never null (CultureInfo.InvariantCulture lowers to the
// dn2cpp_nfi_invariant() singleton), so it is unaffected.
const Dn2CppNumberFormatInfo* dn2cpp_nfi_or_current(const Dn2CppNumberFormatInfo* n)
{
    return n != nullptr ? n : dn2cpp_culture_current();
}

// ===== The named-culture table ==============================================
//
// WHAT EARNS A ROW. A culture is here iff it can be a HOST locale on a platform
// dn2cpp targets — i.e. iff `dn2cpp_pal_default_locale_name` can hand its name
// back — plus each such culture's neutral parent, which a bare `LANG=de` also
// produces. Deliberately NOT "every culture": the point is only that a
// developer's machine never silently gets invariant symbols under a real culture
// NAME. The membership set is frozen in
// tools/gen-culture-table/culture-candidates.txt, because deriving it afresh
// would make the table a fact about whoever last ran the generator.
//
// WHERE THE VALUES COME FROM. tools/gen-culture-table/gen-culture-table.cs asks
// real .NET for each candidate and writes dn2cpp_culture_table.inc, whose header
// names the .NET build, the ICU version, the host and the date — so a value is
// reproducible rather than indistinguishable from a typo. Regeneration is
// deliberately NOT a gate: its oracle is the ICU of whoever runs it, so a gate
// would be red for any developer whose ICU differs from the one the table was
// cut from. The one platform split, ja-JP's currency symbol, is carried in the
// generator.
//
// WHAT CANNOT HAVE A ROW. The generator refuses (and explains) a candidate whose
// NumberFormatInfo the row shape cannot represent; such a culture keeps the
// honest fallback — invariant symbols under the requested name — rather than a
// row that would put the right currency symbol on wrongly punctuated digits.
//
// Four properties of every row below are CHECKED by the generator against real
// .NET, so they are invariants of this table rather than hopes about it:
//   - the percent and currency separators equal the number separators (the
//     struct has three separate pairs, but nothing in the candidate set ever
//     made them differ, and the row carries one pair);
//   - the number, currency and percent group SIZES are equal, likewise (which
//     is what lets one modeled array serve all three);
//   - the group sizes fit the two columns below — one to two levels, each 1..9
//     save a trailing 0;
//   - PositiveInfinitySymbol is U+221E. NegativeInfinitySymbol is NOT derived —
//     it is a separate column, because a culture whose negative sign is U+2212
//     or carries an RTL mark spells it differently than "-" + U+221E.
struct Dn2CppCultureRow
{
    const char16_t* name;          // CultureInfo.Name, in .NET's canonical casing
    const char16_t* decimalSep;    // Number/Percent/CurrencyDecimalSeparator
    const char16_t* groupSep;      // Number/Percent/CurrencyGroupSeparator
    const char16_t* negativeSign;  // NegativeSign
    const char16_t* nan;           // NaNSymbol
    const char16_t* negInf;        // NegativeInfinitySymbol (PositiveInfinity is U+221E)
    const char16_t* currency;      // CurrencySymbol
    const char16_t* percent;       // PercentSymbol
    int32_t lcid;                  // CultureInfo.LCID (4096 == this culture has none)
    int8_t currencyDigits;         // CurrencyDecimalDigits
    int8_t currencyPos;            // CurrencyPositivePattern
    int8_t currencyNeg;            // CurrencyNegativePattern
    int8_t percentPos;             // PercentPositivePattern
    int8_t percentNeg;             // PercentNegativePattern
    int8_t isNeutral;              // CultureInfo.IsNeutralCulture
    // NumberGroupSizes, in two columns. `groupSize1` is -1 when the array has a
    // single element, which is NOT the same as 0: a real trailing 0 means "group
    // once, then stop" and is a shape .NET allows, so conflating the two would
    // make [3] and [3,0] the same row.
    int8_t groupSize0;
    int8_t groupSize1;
};

#include "dn2cpp_culture_table.inc"
static const size_t kCultureCount = sizeof(kCultures) / sizeof(kCultures[0]);

// ASCII case-insensitive compare of a managed culture name against a row's
// literal. .NET's own lookup is case-insensitive (`new CultureInfo("pt-br")` is
// pt-BR), and a case-sensitive table would drop it to the unknown path —
// invariant symbols under a correct-looking name. ASCII case is all a BCP-47 tag
// can contain.
static bool dn2cpp_culture_name_eq_ci(Dn2CppString* s, const char16_t* lit)
{
    if (s == nullptr) return false;
    int32_t i = 0;
    for (; lit[i] != u'\0'; i++)
    {
        if (i >= s->length) return false;
        char16_t a = s->chars[i], b = lit[i];
        if (a >= u'A' && a <= u'Z') a = static_cast<char16_t>(a - u'A' + u'a');
        if (b >= u'A' && b <= u'Z') b = static_cast<char16_t>(b - u'A' + u'a');
        if (a != b) return false;
    }
    return i == s->length;
}

static const Dn2CppCultureRow* dn2cpp_culture_find_row(Dn2CppString* name)
{
    // Linear scan: this runs on `new CultureInfo(x)` and once for the host
    // default, never inside a format. A binary search would add an ordering
    // invariant that, broken, fails by silently missing rows.
    for (size_t i = 0; i < kCultureCount; i++)
        if (dn2cpp_culture_name_eq_ci(name, kCultures[i].name))
            return &kCultures[i];
    return nullptr;
}

// A materialized culture. The table is pure ROM; the Dn2CppNumberFormatInfo a
// caller gets is built on first ask and cached, so the table itself costs no
// allocation. Nodes are GC-heap and rooted through g_cultureCache, a scanned
// static slot, so the chain and its strings stay alive. Keyed on the ROW for a
// modeled culture and on the name for an unmodeled one, which is what makes
// `new CultureInfo("pt-BR") == new CultureInfo("pt-br")` pointer-identical the
// way .NET's single cached CultureInfo instance is.
struct Dn2CppCultureCacheNode
{
    Dn2CppNumberFormatInfo nfi;
    const Dn2CppCultureRow* row; // null for an unmodeled name
    Dn2CppCultureCacheNode* next;
};
static DN2CPP_GC_STATIC_ROOT Dn2CppCultureCacheNode* g_cultureCache = nullptr;
static std::mutex& g_cultureCacheMutex = dn2cpp_never_destroyed<std::mutex>();

// Build (or find) the culture for `row`, or — when `row` is null — the
// invariant-symbol stand-in carrying `name`. Caller must NOT hold the lock.
static const Dn2CppNumberFormatInfo* dn2cpp_culture_intern(const Dn2CppCultureRow* row, Dn2CppString* name)
{
    std::lock_guard<std::mutex> lock(g_cultureCacheMutex);
    for (Dn2CppCultureCacheNode* p = g_cultureCache; p != nullptr; p = p->next)
    {
        if (row != nullptr ? p->row == row
                           : (p->row == nullptr && dn2cpp_string_equals(p->nfi.cultureName, name)))
            return &p->nfi;
    }
    // Allocate and LINK first, then fill: once linked, the node is reachable from
    // a scanned static slot rather than resting on the conservative stack scan.
    // The invariant copy keeps every field valid meanwhile.
    auto* node = static_cast<Dn2CppCultureCacheNode*>(dn2cpp_alloc(sizeof(Dn2CppCultureCacheNode)));
    node->nfi = *dn2cpp_nfi_invariant();
    node->row = row;
    dn2cpp_gc_store_ref(&node->next, g_cultureCache);
    g_cultureCache = node;
    if (row == nullptr)
    {
        // Unmodeled: invariant symbols, but the requested name is preserved so
        // callers that key behavior on the name (e.g. RegexCaseEquivalences'
        // Turkish/NonTurkish tiering) stay faithful, and the LCID says "this
        // culture has none" rather than claiming to be the invariant one.
        dn2cpp_gc_store_ref(&node->nfi.cultureName, name); // `name` is only stack-rooted
        node->nfi.lcid = 4096;
        return &node->nfi;
    }
    node->nfi.cultureName = dn2cpp_lit16(row->name);
    node->nfi.lcid = row->lcid;
    node->nfi.isNeutralCulture = row->isNeutral;
    node->nfi.numberDecimal = node->nfi.percentDecimal = node->nfi.currencyDecimal =
        dn2cpp_lit16(row->decimalSep);
    node->nfi.numberGroup = node->nfi.percentGroup = node->nfi.currencyGroup =
        dn2cpp_lit16(row->groupSep);
    node->nfi.groupSizes[0] = row->groupSize0;
    node->nfi.groupSizes[1] = row->groupSize1;
    node->nfi.groupSizeCount = row->groupSize1 < 0 ? 1 : 2;
    node->nfi.negativeSign = dn2cpp_lit16(row->negativeSign);
    node->nfi.nan = dn2cpp_lit16(row->nan);
    node->nfi.posInf = dn2cpp_lit16(u"∞");
    node->nfi.negInf = dn2cpp_lit16(row->negInf);
    node->nfi.percentSymbol = dn2cpp_lit16(row->percent);
    node->nfi.currencySymbol = dn2cpp_lit16(row->currency);
    node->nfi.currencyDigits = row->currencyDigits;
    node->nfi.currencyPosPattern = row->currencyPos;
    node->nfi.currencyNegPattern = row->currencyNeg;
    node->nfi.percentPosPattern = row->percentPos;
    node->nfi.percentNegPattern = row->percentNeg;
    return &node->nfi;
}

const Dn2CppNumberFormatInfo* dn2cpp_culture_by_name(Dn2CppString* name)
{
    if (name == nullptr || name->length == 0)
        return dn2cpp_nfi_invariant();
    return dn2cpp_culture_intern(dn2cpp_culture_find_row(name), name);
}

const Dn2CppNumberFormatInfo* dn2cpp_culture_by_lcid(int32_t lcid)
{
    if (lcid == 127)
        return dn2cpp_nfi_invariant();
    // 4096 is LOCALE_CUSTOM_UNSPECIFIED — what a culture WITHOUT an LCID
    // reports. Several rows carry it, so honouring it would hand back whichever
    // came first. Real .NET refuses it too.
    if (lcid == 4096)
        return nullptr;
    for (size_t i = 0; i < kCultureCount; i++)
        if (kCultures[i].lcid == lcid)
            return dn2cpp_culture_intern(&kCultures[i], nullptr);
    return nullptr;
}

int32_t dn2cpp_culture_lcid(const Dn2CppNumberFormatInfo* c)
{
    // Defensive, like dn2cpp_culture_name: an all-zero struct must not read as a
    // real culture, so it reports the invariant LCID, matching the "" name it
    // already reports. 0 is not a valid LCID in .NET either (GetCultureInfo(0)
    // raises), so it cannot shadow a real answer.
    return (c == nullptr || c->lcid == 0) ? 127 : c->lcid;
}

int32_t dn2cpp_culture_is_neutral(const Dn2CppNumberFormatInfo* c)
{
    return (c == nullptr || c->isNeutralCulture == 0) ? 0 : 1;
}

// CultureInfo.CurrentCulture — a single process-global slot defaulting to the
// culture the host says the user is in, as real .NET does; only an explicit
// assignment changes it. Process-wide rather than per-thread: dn2cpp does not
// flow ExecutionContext, so .NET's per-thread/async-local culture semantics are
// out of model. The static slot is a GC root, so a heap-allocated culture from
// the unknown-name path stays alive while current.
//
// The host is asked for a NAME only, and the answer runs through the same
// dn2cpp_culture_by_name as `new CultureInfo(x)`, so a modeled locale gets its
// real symbols and an unmodeled one keeps its name over invariant symbols. No
// ICU is consulted; the PAL reproduces only .NET's rule for WHICH locale the
// user is in.
//
// Per-culture DATE formatting is NOT in the model (the DateTime formatters take
// no NumberFormatInfo and are invariant unconditionally); this default does not
// change that in either direction.
//
// Resolved once, lazily rather than in dn2cpp_runtime_init: the resolution
// allocates, which must not happen before the GC is up, and a program that never
// formats anything never asks. `std::call_once` because two threads may reach
// the first read together.
static DN2CPP_GC_STATIC_ROOT const Dn2CppNumberFormatInfo* g_currentCulture = nullptr;
static DN2CPP_GC_STATIC_ROOT const Dn2CppNumberFormatInfo* g_hostCulture = nullptr;
static std::once_flag& g_hostCultureOnce = dn2cpp_never_destroyed<std::once_flag>();

static const Dn2CppNumberFormatInfo* dn2cpp_culture_host_default()
{
    // Fast path: every provider-less numeric format lands here. Once
    // g_hostCulture is non-null it is never written again, so a plain read is
    // enough and std::call_once's guard stays off the hot path.
    if (const Dn2CppNumberFormatInfo* c = g_hostCulture)
        return c;
    std::call_once(g_hostCultureOnce, [] {
        char name[128];
        int32_t n = dn2cpp_pal_default_locale_name(name, sizeof(name));
        g_hostCulture = n > 0
            ? dn2cpp_culture_by_name(dn2cpp_string_from_utf8(name, n))
            : dn2cpp_nfi_invariant();
    });
    return g_hostCulture;
}

const Dn2CppNumberFormatInfo* dn2cpp_culture_current()
{
    const Dn2CppNumberFormatInfo* c = g_currentCulture;
    return c != nullptr ? c : dn2cpp_culture_host_default();
}

void dn2cpp_culture_set_current(const Dn2CppNumberFormatInfo* c)
{
    g_currentCulture = c != nullptr ? c : dn2cpp_nfi_invariant();
}

// CultureInfo.CurrentUICulture — a SECOND slot over the same host default.
// Everything said above about CurrentCulture's slot applies here; it is a
// separate slot because .NET lets the two differ (`CurrentCulture = de-DE` with
// `CurrentUICulture = ja-JP` is legal and meaningful: one picks separators, the
// other the resource set), and because aliasing them would silently make one of
// a test's two invariant pins assert nothing.
//
// It deliberately does NOT reach the resource path. The UI culture selects a
// resource set and dn2cpp links no satellite assemblies, so
// dn2cpp_resourcemanager_get_string keeps answering from the neutral set;
// routing this slot into it would turn every `SR.Format` on a ja-JP host into a
// PlatformNotSupportedException. A stock .NET install has no localized CoreLib
// satellites either, so the neutral answer is the faithful one. What does move
// under real .NET and dn2cpp does not follow is TimeZoneInfo.StandardName /
// DisplayName (ICU's localized zone names; dn2cpp const-folds the English ones)
// — a permanent carve-out, since it needs a (cultures × UI languages) display-
// name table for a question a program can usually ask of the culture's NAME.
static DN2CPP_GC_STATIC_ROOT const Dn2CppNumberFormatInfo* g_currentUICulture = nullptr;

const Dn2CppNumberFormatInfo* dn2cpp_culture_current_ui()
{
    const Dn2CppNumberFormatInfo* c = g_currentUICulture;
    return c != nullptr ? c : dn2cpp_culture_host_default();
}

void dn2cpp_culture_set_current_ui(const Dn2CppNumberFormatInfo* c)
{
    g_currentUICulture = c != nullptr ? c : dn2cpp_nfi_invariant();
}

// CultureInfo.InstalledUICulture — the OS's own language, which no setter moves.
// On POSIX real .NET resolves it from the same environment scan the other two
// default to, so it is the host default itself rather than either slot.
const Dn2CppNumberFormatInfo* dn2cpp_culture_installed_ui()
{
    return dn2cpp_culture_host_default();
}

// CultureInfo.Name / ToString. Defensive: a zeroed mutable NFI (or any culture
// built before the name was modeled) reads as the invariant "".
Dn2CppString* dn2cpp_culture_name(const Dn2CppNumberFormatInfo* c)
{
    if (c == nullptr || c->cultureName == nullptr)
        return dn2cpp_lit16(u"");
    return c->cultureName;
}

Dn2CppString* dn2cpp_textinfo_tostring(const Dn2CppNumberFormatInfo* c)
{
    if (c == nullptr)
        dn2cpp_throw_null_reference();
    return dn2cpp_string_concat2(
        dn2cpp_string_from_utf8("TextInfo - ", 11), dn2cpp_culture_name(c));
}

// `new NumberFormatInfo()` — a mutable GC-heap copy of the invariant; the object
// initializer / property setters then customize. Tagged isNfi so an escape to
// `object` through an erased IFormatProvider slot recovers the NumberFormatInfo
// identity (see dn2cpp_nfi_wrap).
Dn2CppNumberFormatInfo* dn2cpp_nfi_new()
{
    auto* n = static_cast<Dn2CppNumberFormatInfo*>(dn2cpp_alloc(sizeof(Dn2CppNumberFormatInfo)));
    *n = *dn2cpp_nfi_invariant();
    n->isNfi = 1;
    return n;
}

// NumberFormatInfo.InvariantInfo — same field values as the invariant culture
// singleton, but a distinct instance tagged isNfi (see the header note).
const Dn2CppNumberFormatInfo* dn2cpp_nfi_invariant_info()
{
    static const Dn2CppNumberFormatInfo inv = [] {
        Dn2CppNumberFormatInfo n = *dn2cpp_nfi_invariant();
        n.isNfi = 1;
        return n;
    }();
    return &inv;
}

// ---- CultureInfo/NumberFormatInfo/TextInfo escaped into `object` ------------
// The managed types lower to the headerless `const Dn2CppNumberFormatInfo*`,
// so an escape into an `object` context allocates a real managed object around
// the pointer (Dn2CppNfiBox, dn2cpp_core.h). ToString on the CultureInfo form
// is the culture NAME ("" for invariant), matching CultureInfo.ToString.
// NumberFormatInfo keeps the default type-name display; TextInfo's slot
// preserves its culture-bearing "TextInfo - {culture}" display after wrapping.
static Dn2CppString* dn2cpp_cultureinfo_box_tostring(Dn2CppObject* o)
{
    return dn2cpp_culture_name(reinterpret_cast<Dn2CppNfiBox*>(o)->nfi);
}

static Dn2CppString* dn2cpp_textinfo_box_tostring(Dn2CppObject* o)
{
    return dn2cpp_textinfo_tostring(reinterpret_cast<Dn2CppNfiBox*>(o)->nfi);
}

const Dn2CppTypeInfo dn2cpp_cultureinfo_type =
    { "System.Globalization.CultureInfo", &dn2cpp_object_type, (int32_t)sizeof(Dn2CppNfiBox), nullptr, nullptr, 0,
      dn2cpp_cultureinfo_box_tostring };
const Dn2CppTypeInfo dn2cpp_numberformatinfo_type =
    { "System.Globalization.NumberFormatInfo", &dn2cpp_object_type, (int32_t)sizeof(Dn2CppNfiBox), nullptr, nullptr, 0 };
const Dn2CppTypeInfo dn2cpp_textinfo_type =
    { "System.Globalization.TextInfo", &dn2cpp_object_type, (int32_t)sizeof(Dn2CppNfiBox), nullptr, nullptr, 0,
      dn2cpp_textinfo_box_tostring };

// True when the header word of `o` names one of the wrapper type-infos. Safe
// on a RAW Dn2CppNumberFormatInfo* that flowed through an erased context: the
// probe reads the struct's first field (a string pointer) and compares it by
// VALUE against three static addresses — it never dereferences the misread
// header.
static const Dn2CppTypeInfo* dn2cpp_nfi_box_ti(const void* o)
{
    const Dn2CppTypeInfo* t = static_cast<const Dn2CppObject*>(o)->type;
    return (t == &dn2cpp_cultureinfo_type || t == &dn2cpp_numberformatinfo_type
            || t == &dn2cpp_textinfo_type) ? t : nullptr;
}

const Dn2CppNumberFormatInfo* dn2cpp_nfi_unwrap(Dn2CppObject* o)
{
    if (o == nullptr)
        return nullptr;
    if (dn2cpp_nfi_box_ti(o) != nullptr)
        return reinterpret_cast<Dn2CppNfiBox*>(o)->nfi;
    return reinterpret_cast<const Dn2CppNumberFormatInfo*>(o);
}

Dn2CppObject* dn2cpp_nfi_wrap(const Dn2CppNumberFormatInfo* n, int32_t kind)
{
    if (n == nullptr)
        return nullptr;
    // Already a wrapper (a value that crossed the boundary twice): pass through.
    if (dn2cpp_nfi_box_ti(n) != nullptr)
        return reinterpret_cast<Dn2CppObject*>(const_cast<Dn2CppNumberFormatInfo*>(n));
    const Dn2CppTypeInfo* ti =
        kind == DN2CPP_NFI_KIND_CULTURE ? &dn2cpp_cultureinfo_type
        : kind == DN2CPP_NFI_KIND_NFI ? &dn2cpp_numberformatinfo_type
        : kind == DN2CPP_NFI_KIND_TEXTINFO ? &dn2cpp_textinfo_type
        // PROVIDER: the static type was erased; recover the identity from the
        // instance tag (nfi_new/InvariantInfo set it, cultures leave it 0).
        : (n->isNfi != 0 ? &dn2cpp_numberformatinfo_type : &dn2cpp_cultureinfo_type);
    // Intern per (pointer, identity): two escapes of one culture must satisfy
    // ReferenceEquals, like .NET's single CultureInfo instance does. The chain
    // is GC-visible (nodes on the GC heap, head a scanned static slot), so a
    // heap culture (unknown-name cache node / nfi_new) stays alive while
    // wrapped.
    struct Dn2CppNfiBoxNode
    {
        Dn2CppNfiBox box;
        Dn2CppNfiBoxNode* next;
    };
    static DN2CPP_GC_STATIC_ROOT Dn2CppNfiBoxNode* g_nfiBoxes = nullptr;
    static std::mutex& g_nfiBoxMutex = dn2cpp_never_destroyed<std::mutex>();
    std::lock_guard<std::mutex> lock(g_nfiBoxMutex);
    for (Dn2CppNfiBoxNode* p = g_nfiBoxes; p != nullptr; p = p->next)
        if (p->box.nfi == n && p->box.type == ti)
            return &p->box;
    auto* node = static_cast<Dn2CppNfiBoxNode*>(dn2cpp_alloc(sizeof(Dn2CppNfiBoxNode)));
    node->box.type = ti;
    node->box.nfi = n;
    dn2cpp_gc_store_ref(&node->next, g_nfiBoxes);
    g_nfiBoxes = node;
    return &node->box;
}

// An NFI is a field of a cache node, so each setter dirties that node's slot.
void dn2cpp_nfi_set_number_decimal(Dn2CppNumberFormatInfo* n, Dn2CppString* v) { dn2cpp_gc_store_ref(&n->numberDecimal, v); }
void dn2cpp_nfi_set_number_group(Dn2CppNumberFormatInfo* n, Dn2CppString* v) { dn2cpp_gc_store_ref(&n->numberGroup, v); }
void dn2cpp_nfi_set_negative_sign(Dn2CppNumberFormatInfo* n, Dn2CppString* v) { dn2cpp_gc_store_ref(&n->negativeSign, v); }
void dn2cpp_nfi_set_nan(Dn2CppNumberFormatInfo* n, Dn2CppString* v) { dn2cpp_gc_store_ref(&n->nan, v); }
void dn2cpp_nfi_set_pos_inf(Dn2CppNumberFormatInfo* n, Dn2CppString* v) { dn2cpp_gc_store_ref(&n->posInf, v); }
void dn2cpp_nfi_set_neg_inf(Dn2CppNumberFormatInfo* n, Dn2CppString* v) { dn2cpp_gc_store_ref(&n->negInf, v); }
void dn2cpp_nfi_set_percent_symbol(Dn2CppNumberFormatInfo* n, Dn2CppString* v) { dn2cpp_gc_store_ref(&n->percentSymbol, v); }
void dn2cpp_nfi_set_currency_symbol(Dn2CppNumberFormatInfo* n, Dn2CppString* v) { dn2cpp_gc_store_ref(&n->currencySymbol, v); }

// NumberGroupSizes / CurrencyGroupSizes / PercentGroupSizes — the getter. A
// fresh array per call, matching real .NET's defensive copy: two reads are not
// reference-equal, and writing through the returned array does not move the
// culture's formatting. The single place where the modeled array becomes a
// managed int[], serving the public properties and System.Number's readers.
Dn2CppArrayI4* dn2cpp_nfi_group_sizes(const Dn2CppNumberFormatInfo* n, const Dn2CppTypeInfo* ti)
{
    int32_t count = (n == nullptr || n->groupSizeCount <= 0) ? 0 : n->groupSizeCount;
    if (count > DN2CPP_MAX_GROUP_SIZES) count = DN2CPP_MAX_GROUP_SIZES;
    Dn2CppArrayI4* a = dn2cpp_newarr_i4_t(count, ti);
    for (int32_t i = 0; i < count; i++)
        a->data[i] = n->groupSizes[i];
    return a;
}

// The setter — validate and trap. ONE array serves NumberGroupSizes,
// CurrencyGroupSizes and PercentGroupSizes (the table's row invariant), so
// honoring a write to one would silently move the other two. A write of the
// value already modeled is a true no-op and is accepted (YamlDotNet assigns
// `new[] { 3 }` to a clone of the invariant NFI); anything else throws a
// catchable PlatformNotSupportedException naming the property.
void dn2cpp_nfi_set_group_sizes(const Dn2CppNumberFormatInfo* n, Dn2CppArrayI4* v, const char* which)
{
    bool same = v != nullptr && n != nullptr && v->length == n->groupSizeCount;
    if (same)
        for (int32_t i = 0; i < v->length; i++)
            if (v->data[i] != n->groupSizes[i]) { same = false; break; }
    if (!same)
    {
        char msg[192];
        std::snprintf(msg, sizeof(msg),
                      "NumberFormatInfo.%s: dn2cpp models the number, currency and percent group "
                      "sizes as ONE array, so a write that changes it is unsupported", which);
        dn2cpp_throw_platform_not_supported(msg);
    }
}

// Replace the ASCII '-' sign (only ever leading) and '.' decimal point in a
// freshly-rendered numeric ASCII buffer with the culture's multi-char symbols,
// widening the rest to UTF-16. Grouping is applied separately upstream.
Dn2CppString* dn2cpp_localize_ascii(const char* buf, int len, const Dn2CppNumberFormatInfo* n)
{
    char16_t out[160];
    int o = 0;
    for (int i = 0; i < len; i++)
    {
        char c = buf[i];
        Dn2CppString* rep = (c == '-') ? n->negativeSign
                          : (c == '.') ? n->numberDecimal
                                       : nullptr;
        if (rep != nullptr)
            for (int k = 0; k < rep->length; k++) out[o++] = rep->chars[k];
        else
            out[o++] = static_cast<char16_t>(static_cast<unsigned char>(c));
    }
    char16_t* dst;
    Dn2CppString* s = dn2cpp_string_alloc(&dst, o);
    for (int i = 0; i < o; i++) dst[i] = out[i];
    return s;
}

static Dn2CppString* dn2cpp_float_format(double v, int maxSig, int sciThreshold, bool roundTripFloat,
                                         const Dn2CppNumberFormatInfo* nfi)
{
    nfi = dn2cpp_nfi_or_current(nfi);
    if (std::isnan(v)) return nfi->nan;
    if (std::isinf(v)) return v < 0 ? nfi->negInf : nfi->posInf;
    bool neg = std::signbit(v);
    double mag = neg ? -v : v;
    char out[64];
    int pos = 0;
    if (neg) out[pos++] = '-';
    if (mag == 0.0) { out[pos++] = '0'; return dn2cpp_localize_ascii(out, pos, nfi); }

    // Find the fewest significant digits whose decimal text round-trips to the same
    // value — exactly .NET's shortest-digit selection.
    char sci[40];
    int p = maxSig;
    for (int q = 1; q <= maxSig; q++)
    {
        std::snprintf(sci, sizeof(sci), "%.*e", q - 1, mag);
        double back = std::strtod(sci, nullptr);
        bool ok = roundTripFloat ? ((float)back == (float)mag) : (back == mag);
        if (ok) { p = q; break; }
    }
    std::snprintf(sci, sizeof(sci), "%.*e", p - 1, mag);

    // sci is "d[.ddd]e±XX": pull the significant digits and the exponent of the lead
    // digit, then strip any trailing zeros the rounding introduced.
    char dig[20];
    int L = 0;
    const char* s = sci;
    dig[L++] = *s++;
    if (*s == '.') { s++; while (*s != 'e' && *s != 'E') dig[L++] = *s++; }
    while (*s != 'e' && *s != 'E') s++;
    int E = std::atoi(s + 1);
    while (L > 1 && dig[L - 1] == '0') L--;

    if (E < -4 || E >= sciThreshold)
    {
        out[pos++] = dig[0];
        if (L > 1) { out[pos++] = '.'; for (int i = 1; i < L; i++) out[pos++] = dig[i]; }
        out[pos++] = 'E';
        out[pos++] = E < 0 ? '-' : '+';
        char eb[8];
        int el = std::snprintf(eb, sizeof(eb), "%d", E < 0 ? -E : E);
        if (el < 2) out[pos++] = '0';
        for (int i = 0; i < el; i++) out[pos++] = eb[i];
    }
    else if (E >= L - 1)
    {
        for (int i = 0; i < L; i++) out[pos++] = dig[i];
        for (int i = 0; i < E - (L - 1); i++) out[pos++] = '0';
    }
    else if (E >= 0)
    {
        for (int i = 0; i <= E; i++) out[pos++] = dig[i];
        out[pos++] = '.';
        for (int i = E + 1; i < L; i++) out[pos++] = dig[i];
    }
    else
    {
        out[pos++] = '0';
        out[pos++] = '.';
        for (int i = 0; i < -E - 1; i++) out[pos++] = '0';
        for (int i = 0; i < L; i++) out[pos++] = dig[i];
    }
    return dn2cpp_localize_ascii(out, pos, nfi);
}

Dn2CppString* dn2cpp_double_to_string(double v)
{
    return dn2cpp_float_format(v, 17, 17, false, nullptr);
}

Dn2CppString* dn2cpp_float_to_string(float v)
{
    return dn2cpp_float_format(static_cast<double>(v), 9, 9, true, nullptr);
}

Dn2CppString* dn2cpp_double_to_string_c(double v, const Dn2CppNumberFormatInfo* n)
{
    return dn2cpp_float_format(v, 17, 17, false, n);
}

Dn2CppString* dn2cpp_float_to_string_c(float v, const Dn2CppNumberFormatInfo* n)
{
    return dn2cpp_float_format(static_cast<double>(v), 9, 9, true, n);
}

Dn2CppString* dn2cpp_bool_to_string(int32_t v)
{
    return v != 0 ? dn2cpp_string_from_ascii("True", 4) : dn2cpp_string_from_ascii("False", 5);
}

Dn2CppString* dn2cpp_string_concat_n(Dn2CppString** parts, int n)
{
    int64_t total = 0;
    for (int i = 0; i < n; i++)
    {
        dn2cpp_validate_string_shape(parts[i]);
        total += parts[i] != nullptr ? parts[i]->length : 0;
    }

    char16_t* buf;
    Dn2CppString* s = dn2cpp_string_alloc(&buf, dn2cpp_string_checked_length(total));
    int32_t pos = 0;
    for (int i = 0; i < n; i++)
    {
        if (parts[i] == nullptr)
            continue;
        std::memcpy(buf + pos, parts[i]->chars,
            static_cast<size_t>(parts[i]->length) * sizeof(char16_t));
        pos += parts[i]->length;
    }
    return s;
}

Dn2CppString* dn2cpp_string_concat2(Dn2CppString* a, Dn2CppString* b)
{
    Dn2CppString* parts[] = { a, b };
    return dn2cpp_string_concat_n(parts, 2);
}

Dn2CppString* dn2cpp_string_concat3(Dn2CppString* a, Dn2CppString* b, Dn2CppString* c)
{
    Dn2CppString* parts[] = { a, b, c };
    return dn2cpp_string_concat_n(parts, 3);
}

Dn2CppString* dn2cpp_string_concat4(Dn2CppString* a, Dn2CppString* b, Dn2CppString* c, Dn2CppString* d)
{
    Dn2CppString* parts[] = { a, b, c, d };
    return dn2cpp_string_concat_n(parts, 4);
}

// Case-insensitive variant of dn2cpp_string_equals, used by Enum.Parse/TryParse's
// ignoreCase overload. ignoreCase == 0 is exactly dn2cpp_string_equals;
// otherwise each code unit folds with dn2cpp_ordinal_upper (the exact BMP
// OrdinalIgnoreCase fold; see dn2cpp_ordinal_casing.cpp).
int32_t dn2cpp_string_equals_ci(Dn2CppString* a, Dn2CppString* b, int32_t ignoreCase)
{
    if (!ignoreCase)
        return dn2cpp_string_equals(a, b);
    if (a == b)
        return 1;
    if (a == nullptr || b == nullptr)
        return 0;
    if (a->length != b->length)
        return 0;
    for (int32_t i = 0; i < a->length; i++)
        if (dn2cpp_ordinal_upper(a->chars[i]) != dn2cpp_ordinal_upper(b->chars[i]))
            return 0;
    return 1;
}

// Match an already-trimmed token [p, p+len) against the enum member-name table
// (case folded when ignoreCase != 0). Returns 1 + *out on a hit, else 0. Names
// only — numeric tokens are handled separately (the whole-string numeric path).
template <typename T>
static int32_t dn2cpp_enum_match_name(const char16_t* p, int32_t len,
    const T* values, Dn2CppString* const* names, int32_t count,
    int32_t ignoreCase, T* out)
{
    for (int32_t i = 0; i < count; i++)
    {
        Dn2CppString* nm = names[i];
        if (nm->length != len)
            continue;
        int match = 1;
        for (int32_t j = 0; j < len; j++)
        {
            char16_t a = p[j], b = nm->chars[j];
            if (ignoreCase) { a = dn2cpp_ordinal_upper(a); b = dn2cpp_ordinal_upper(b); }
            if (a != b) { match = 0; break; }
        }
        if (match) { *out = values[i]; return 1; }
    }
    return 0;
}

// The token's bit pattern if the magnitude fits the UNDERLYING type's range, else
// false. Not the model's range: "300" fits the int32 every sub-64-bit enum rides,
// and real .NET still raises OverflowException for `enum E : byte`. .NET's unsigned
// parsers accept a negative sign only on zero, which is why "-0" is a value and
// "-1" an overflow for every unsigned underlying.
static bool dn2cpp_enum_num_fits(uint64_t mag, bool neg, int32_t uWidth, bool uUnsigned,
    uint64_t* bits)
{
    if (uUnsigned)
    {
        if (neg && mag != 0)
            return false;
        uint64_t max = uWidth >= 8 ? ~0ULL : ((1ULL << (uWidth * 8)) - 1);
        if (mag > max)
            return false;
        *bits = mag;
        return true;
    }
    uint64_t maxPos = uWidth >= 8 ? static_cast<uint64_t>(INT64_MAX)
                                  : ((1ULL << (uWidth * 8 - 1)) - 1);
    if (mag > (neg ? maxPos + 1 : maxPos))
        return false;
    // Negate in the unsigned domain: the most negative magnitude has no positive form.
    *bits = neg ? 0ULL - mag : mag;
    return true;
}

// Parse [p, p+len) as a signed decimal: optional +/- then digits, nothing else.
// Returns 1 + *out on success, 0 otherwise — and on 0 distinguishes the two failures
// real .NET renders differently, by setting *overflow for a well-formed decimal outside
// the underlying's range (OverflowException) and leaving it clear for anything that is
// not a decimal at all (which falls through to the name path, then ArgumentException).
template <typename T>
static int32_t dn2cpp_enum_parse_num(const char16_t* p, int32_t len, int32_t uWidth,
    bool uUnsigned, T* out, bool* overflow)
{
    if (len == 0)
        return 0;
    int32_t idx = 0;
    bool neg = false;
    if (p[0] == u'+') idx = 1;
    else if (p[0] == u'-') { neg = true; idx = 1; }
    if (idx >= len)
        return 0;
    uint64_t acc = 0;
    bool huge = false; // a magnitude past uint64 is still a decimal, so still an overflow
    for (; idx < len; idx++)
    {
        char16_t c = p[idx];
        if (c < u'0' || c > u'9')
            return 0;
        uint64_t d = static_cast<uint64_t>(c - u'0');
        if (huge || acc > (~0ULL - d) / 10)
        {
            huge = true;
            continue;
        }
        acc = acc * 10 + d;
    }
    uint64_t bits = 0;
    if (huge || !dn2cpp_enum_num_fits(acc, neg, uWidth, uUnsigned, &bits))
    {
        *overflow = true;
        return 0;
    }
    *out = static_cast<T>(bits);
    return 1;
}

// Enum.Parse/TryParse worker. Matches .NET: the leading-trimmed string is
// first tried as a single signed decimal (returned even if not a defined member);
// otherwise it is a comma-separated list of member *names* (no per-token numeric),
// each trimmed and OR'd. Returns 1 + *out on success, 0 on the empty/whitespace
// string, an empty token, or an unmatched name. ignoreCase folds names with
// the ordinal BMP fold.
//
// Whitespace matches .NET, which is asymmetric about it: the leading trim
// (span.TrimStart) and each name token's .Trim() use char.IsWhiteSpace — full
// Unicode — but the numeric form's trailing run goes through the integer
// parser's NumberStyles.AllowTrailingWhite, which is ASCII only. So U+00A0
// before "5" trims away and the string parses as 5, while U+00A0 after "5" fails
// the numeric parse and falls through to the name path, failing the whole parse.
//
// Templated on the member table's width so 64-bit-underlying enums keep their
// members' full value: the int32 instantiation is what every narrower enum emits,
// the int64 one what dn2cpp_enum_parse64 serves. That model width is NOT the
// numeric token's range — see dn2cpp_enum_num_fits.
template <typename T>
static int32_t dn2cpp_enum_parse_t(Dn2CppString* s, const T* values,
    Dn2CppString* const* names, int32_t count, int32_t ignoreCase,
    int32_t uWidth, int32_t uUnsigned, T* out, int32_t* overflow)
{
    if (overflow != nullptr)
        *overflow = 0;
    if (s == nullptr)
        return 0;
    const char16_t* p = s->chars;
    int32_t lo = 0, hi = s->length;
    while (lo < hi && dn2cpp_char_is_whitespace(p[lo]))
        lo++;
    if (lo == hi)
        return 0; // empty / whitespace-only
    // Whole-string numeric form (decimal only; no per-token numeric in a list).
    // Trailing whitespace here is ASCII-only (see above); the name path below
    // re-covers the range with the full-Unicode per-token trim.
    int32_t hiNum = hi;
    while (hiNum > lo && dn2cpp_is_ws(p[hiNum - 1]))
        hiNum--;
    T nv;
    // Tracked locally, not through the caller's pointer: TryParse passes null, and the
    // fall-through decision below must not depend on whether anybody is listening.
    bool ovf = false;
    if (dn2cpp_enum_parse_num(p + lo, hiNum - lo, uWidth, uUnsigned != 0, &nv, &ovf))
    {
        *out = nv;
        return 1;
    }
    // A decimal that overflowed the underlying is final — .NET does not retry it as a
    // name, and falling through would report the overflow as an ArgumentException.
    if (ovf)
    {
        if (overflow != nullptr)
            *overflow = 1;
        return 0;
    }
    // Comma-separated member names, OR'd (applies regardless of [Flags]).
    T result = 0;
    int32_t i = lo;
    for (;;)
    {
        int32_t start = i;
        while (i < hi && p[i] != u',')
            i++;
        int32_t end = i;
        while (start < end && dn2cpp_char_is_whitespace(p[start]))
            start++;
        while (end > start && dn2cpp_char_is_whitespace(p[end - 1]))
            end--;
        T tv;
        if (end == start || !dn2cpp_enum_match_name(p + start, end - start, values, names, count, ignoreCase, &tv))
            return 0; // empty token or unmatched name
        result |= tv;
        if (i >= hi)
            break;
        i++; // skip the comma
    }
    *out = result;
    return 1;
}

int32_t dn2cpp_enum_parse(Dn2CppString* s, const int32_t* values,
    Dn2CppString* const* names, int32_t count, int32_t ignoreCase,
    int32_t uWidth, int32_t uUnsigned, int32_t* out, int32_t* overflow)
{
    return dn2cpp_enum_parse_t(s, values, names, count, ignoreCase, uWidth, uUnsigned, out, overflow);
}

int32_t dn2cpp_enum_parse64(Dn2CppString* s, const int64_t* values,
    Dn2CppString* const* names, int32_t count, int32_t ignoreCase,
    int32_t uWidth, int32_t uUnsigned, int64_t* out, int32_t* overflow)
{
    return dn2cpp_enum_parse_t(s, values, names, count, ignoreCase, uWidth, uUnsigned, out, overflow);
}

// String.Compare / CompareOrdinal. Ordinal (or OrdinalIgnoreCase) compare
// over the UTF-16 buffer, returning the .NET difference: the signed gap of the
// first differing code unit, else the length difference. null sorts first
// (null,null -> 0). The comparisonType folds through
// dn2cpp_str_comparison_fold (dn2cpp_system_string.cpp): culture-sensitive
// values compare ordinally — the invariant-globalization posture, with the
// non-ASCII/ordering divergence documented at the fold.
int32_t dn2cpp_str_compare(Dn2CppString* a, Dn2CppString* b, int32_t comparisonType)
{
    comparisonType = dn2cpp_str_comparison_fold(comparisonType);
    if (a == b)
        return 0;
    if (a == nullptr)
        return -1;
    if (b == nullptr)
        return 1;
    bool ignoreCase = comparisonType == 5;
    int32_t n = a->length < b->length ? a->length : b->length;
    for (int32_t i = 0; i < n; i++)
    {
        char16_t ca = a->chars[i], cb = b->chars[i];
        if (ignoreCase)
        {
            ca = dn2cpp_ordinal_upper(ca);
            cb = dn2cpp_ordinal_upper(cb);
        }
        if (ca != cb)
            return static_cast<int32_t>(static_cast<uint16_t>(ca)) - static_cast<int32_t>(static_cast<uint16_t>(cb));
    }
    return a->length - b->length;
}

// The (strA, indexA, strB, indexB, length, comparisonType) substring form —
// each side contributes at most `length` code units starting at its index
// (fewer when its string ends first), then compares like dn2cpp_str_compare:
// signed gap of the first differing unit, else the contributed-length gap.
int32_t dn2cpp_str_compare_sub(Dn2CppString* a, int32_t indexA, Dn2CppString* b,
                               int32_t indexB, int32_t length, int32_t comparisonType)
{
    comparisonType = dn2cpp_str_comparison_fold(comparisonType);
    if (a == nullptr)
        return b == nullptr ? 0 : -1;
    if (b == nullptr)
        return 1;
    // Range checks run only once both operands are present: .NET's null
    // short-circuit precedes its argument validation. A negative length, a
    // negative index, or an index past the end each raise a catchable
    // ArgumentOutOfRangeException; an index equal to Length, and an index whose
    // span overruns the end, are valid and clamp below.
    if (length < 0)
        dn2cpp_throw_argument_out_of_range();
    if (indexA < 0 || indexB < 0)
        dn2cpp_throw_argument_out_of_range();
    if (indexA > a->length || indexB > b->length)
        dn2cpp_throw_argument_out_of_range();
    int32_t la = a->length - indexA;
    if (la > length)
        la = length;
    int32_t lb = b->length - indexB;
    if (lb > length)
        lb = length;
    bool ignoreCase = comparisonType == 5;
    int32_t n = la < lb ? la : lb;
    for (int32_t i = 0; i < n; i++)
    {
        char16_t ca = a->chars[indexA + i], cb = b->chars[indexB + i];
        if (ignoreCase)
        {
            ca = dn2cpp_ordinal_upper(ca);
            cb = dn2cpp_ordinal_upper(cb);
        }
        if (ca != cb)
            return static_cast<int32_t>(static_cast<uint16_t>(ca)) - static_cast<int32_t>(static_cast<uint16_t>(cb));
    }
    return la - lb;
}

// OrdinalIgnoreCase equality over `length` UTF-16 code units of two raw buffers
// (the caller has already established equal lengths). Exact BMP ordinal fold
// per code unit. Backs Globalization.Ordinal.EqualsIgnoreCase(ref char,
// ref char, int), replacing its ICU cascade with the same results.
int32_t dn2cpp_chars_equals_ignorecase_ordinal(const char16_t* a, const char16_t* b, int32_t length)
{
    if (a == b)
        return 1;
    for (int32_t i = 0; i < length; i++)
        if (dn2cpp_ordinal_upper(a[i]) != dn2cpp_ordinal_upper(b[i]))
            return 0;
    return 1;
}

// OrdinalIgnoreCase three-way compare over two raw buffers — the .NET difference
// (signed gap of the first differing folded code unit, else the length
// difference). Backs Globalization.Ordinal.CompareStringIgnoreCase(ref char, int,
// ref char, int); same fold rule as dn2cpp_str_compare's case-5 path.
int32_t dn2cpp_chars_compare_ignorecase_ordinal(const char16_t* a, int32_t lengthA, const char16_t* b, int32_t lengthB)
{
    int32_t n = lengthA < lengthB ? lengthA : lengthB;
    for (int32_t i = 0; i < n; i++)
    {
        char16_t ca = dn2cpp_ordinal_upper(a[i]);
        char16_t cb = dn2cpp_ordinal_upper(b[i]);
        if (ca != cb)
            return static_cast<int32_t>(static_cast<uint16_t>(ca)) - static_cast<int32_t>(static_cast<uint16_t>(cb));
    }
    return lengthA - lengthB;
}

// Case-sensitive sibling: pure ordinal code-unit three-way compare over two
// raw buffers, same .NET-difference contract. Backs
// String.CompareOrdinal(ReadOnlySpan<char>, ReadOnlySpan<char>) —
// MemoryExtensions.CompareTo's Ordinal arm.
int32_t dn2cpp_chars_compare_ordinal(const char16_t* a, int32_t lengthA, const char16_t* b, int32_t lengthB)
{
    int32_t n = lengthA < lengthB ? lengthA : lengthB;
    for (int32_t i = 0; i < n; i++)
    {
        if (a[i] != b[i])
            return static_cast<int32_t>(static_cast<uint16_t>(a[i])) - static_cast<int32_t>(static_cast<uint16_t>(b[i]));
    }
    return lengthA - lengthB;
}

// OrdinalIgnoreCase substring search over two raw buffers (the leftmost start
// index of `value` in `source`, -1 when absent; an empty value matches at 0 —
// .NET IndexOf semantics). Same fold rule as the equals/compare helpers
// above. Backs Globalization.Ordinal.IndexOfOrdinalIgnoreCase
// (ReadOnlySpan<char>, ReadOnlySpan<char>) — Regex's OrdinalIgnoreCase prefix
// find optimization — replacing its ICU cascade with the same results.
int32_t dn2cpp_chars_indexof_ignorecase_ordinal(const char16_t* source, int32_t sourceLength,
                                              const char16_t* value, int32_t valueLength)
{
    if (valueLength == 0)
        return 0;
    for (int32_t i = 0; i + valueLength <= sourceLength; i++)
    {
        int32_t j = 0;
        while (j < valueLength
               && dn2cpp_ordinal_upper(source[i + j]) == dn2cpp_ordinal_upper(value[j]))
            j++;
        if (j == valueLength)
            return i;
    }
    return -1;
}

// FNV-1a over the ordinal-upper-folded code units — the OrdinalIgnoreCase sibling of
// dn2cpp_string_hashcode. Folding before mixing keeps it consistent with
// dn2cpp_chars_equals_ignorecase_ordinal (OIC-equal keys hash equal), which is what
// a case-insensitive hashed property lookup requires.
int32_t dn2cpp_chars_hashcode_oic(const char16_t* p, int32_t length)
{
    uint32_t hash = 2166136261u;
    for (int32_t i = 0; i < length; i++)
    {
        hash = (hash ^ static_cast<uint16_t>(dn2cpp_ordinal_upper(p[i]))) * 16777619u;
    }
    return static_cast<int32_t>(hash);
}

int32_t dn2cpp_string_hashcode_oic(Dn2CppString* s)
{
    return s == nullptr ? 0 : dn2cpp_chars_hashcode_oic(s->chars, s->length);
}

// String.get_Chars(index): the UTF-16 code unit at `index`. Out of range is
// IndexOutOfRangeException — for `s[-1]` as much as `s[s.Length]`; the string
// indexer is one of the few BCL indexers that does NOT report
// ArgumentOutOfRangeException — and catchable, so a scanner that walks off the
// end recovers instead of ending the process.
int32_t dn2cpp_string_get_char(Dn2CppString* s, int32_t index)
{
    if (s == nullptr)
        dn2cpp_throw_null_reference();
    if (index < 0 || index >= s->length)
        dn2cpp_throw_index_out_of_range();
    return static_cast<int32_t>(static_cast<uint16_t>(s->chars[index]));
}

int32_t dn2cpp_char_get_string_char(Dn2CppString* s, int32_t index)
{
    if (s == nullptr)
        dn2cpp_throw_argument_null_param("s");
    if (index < 0 || index >= s->length)
        dn2cpp_throw_argument_out_of_range();
    return static_cast<int32_t>(static_cast<uint16_t>(s->chars[index]));
}

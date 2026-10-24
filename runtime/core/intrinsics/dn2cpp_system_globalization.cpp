// dn2cpp_system_globalization.cpp — System.Globalization intrinsics and the
// culture-aware numeric formatters they drive: the builtin culture table
// (kCultures, generated into dn2cpp_culture_table.inc; its membership rule is
// stated at the include site), the CurrentCulture / CurrentUICulture slots over
// the host default, a mutable NumberFormatInfo, and the *_c formatters that
// localize digits/sign/group/decimal symbols.
#include "dn2cpp_core.h"
#include "platform/dn2cpp_pal.h"
#include "platform/dn2cpp_locale_id.h"

#include <cstdint>
#include <cstdio>
#include <cstring>
#include <cstdlib>
#include <cmath>
#include <string>
#include <vector>
#include <memory>
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
    n->cultureName = n->cultureRealName = dn2cpp_lit16(u"");
    n->lcid = 127; // the invariant culture's LCID (0x7F)
    n->missingNumberFormat = 0;
    n->numberFormat = nullptr;
    n->isReadOnly = 1;
    n->numberDecimal = dn2cpp_lit16(u".");
    n->numberGroup = dn2cpp_lit16(u",");
    n->groupSizes[0] = 3;            // the invariant culture's NumberGroupSizes == [3]
    n->groupSizeCount = n->currencyGroupSizeCount = n->percentGroupSizeCount = 1;
    n->currencyGroupSizes[0] = n->percentGroupSizes[0] = 3;
    n->numberDigits = n->percentDigits = 2;
    n->numberNegPattern = 1;
    n->positiveSign = dn2cpp_lit16(u"+");
    n->perMilleSymbol = dn2cpp_lit16(u"‰");
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

static Dn2CppNumberFormatInfo* dn2cpp_nfi_copy(const Dn2CppNumberFormatInfo* source,
    int32_t isNfi, int32_t isReadOnly)
{
    auto* result = static_cast<Dn2CppNumberFormatInfo*>(dn2cpp_alloc(sizeof(Dn2CppNumberFormatInfo)));
    *result = *source;
    result->numberFormat = nullptr;
    result->isNfi = isNfi;
    result->isReadOnly = isReadOnly;
    return result;
}

const Dn2CppNumberFormatInfo* dn2cpp_culture_number_format(const Dn2CppNumberFormatInfo* culture)
{
    if (culture == nullptr)
        dn2cpp_throw_null_reference();
    if (culture->isNfi != 0)
        return culture;
    if (culture == dn2cpp_nfi_invariant())
        return dn2cpp_nfi_invariant_info();
    static std::mutex& mutex = dn2cpp_never_destroyed<std::mutex>();
    std::lock_guard<std::mutex> lock(mutex);
    if (culture->numberFormat != nullptr)
        return culture->numberFormat;
    if (culture->missingNumberFormat != 0)
        dn2cpp_throw_null_reference();
    const Dn2CppNumberFormatInfo* number = dn2cpp_nfi_copy(culture, 1, culture->isReadOnly);
    dn2cpp_gc_store_ref(&const_cast<Dn2CppNumberFormatInfo*>(culture)->numberFormat, number);
    return number;
}

int32_t dn2cpp_nfi_is_read_only(const Dn2CppNumberFormatInfo* n)
{
    if (n == nullptr)
        dn2cpp_throw_null_reference();
    return n->isReadOnly;
}

void dn2cpp_nfi_verify_writable(const Dn2CppNumberFormatInfo* n)
{
    if (dn2cpp_nfi_is_read_only(n) != 0)
        dn2cpp_throw_invalid_operation_msg("Instance is read-only.");
}

void dn2cpp_culture_set_number_format(Dn2CppNumberFormatInfo* culture,
    const Dn2CppNumberFormatInfo* value)
{
    if (culture == nullptr)
        dn2cpp_throw_null_reference();
    if (value == nullptr)
        dn2cpp_throw_argument_null_param("value");
    dn2cpp_nfi_verify_writable(culture);
    dn2cpp_gc_store_ref(&culture->numberFormat, value);
}

const Dn2CppNumberFormatInfo* dn2cpp_nfi_read_only(const Dn2CppNumberFormatInfo* n)
{
    if (n == nullptr)
        dn2cpp_throw_argument_null_param("nfi");
    return n->isReadOnly != 0 ? n : dn2cpp_nfi_copy(n, 1, 1);
}

const Dn2CppNumberFormatInfo* dn2cpp_culture_read_only(const Dn2CppNumberFormatInfo* culture)
{
    if (culture == nullptr)
        dn2cpp_throw_argument_null_param("ci");
    if (culture->isReadOnly != 0)
        return culture;
    auto* result = dn2cpp_nfi_copy(culture, 0, 1);
    // Neutral ReadOnly preserves an already assigned mutable NFI. A lazy slot
    // stays lazy and is later initialized using the new culture's readonly flag.
    if (culture->numberFormat != nullptr)
        dn2cpp_gc_store_ref(&result->numberFormat, culture->isNeutralCulture != 0
            ? culture->numberFormat : dn2cpp_nfi_read_only(culture->numberFormat));
    return result;
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
    return dn2cpp_culture_number_format(n != nullptr ? n : dn2cpp_culture_current());
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
// The generator checks these properties against .NET:
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

#include "dn2cpp_culture_identifiers.inc"
#include "dn2cpp_culture_language_codes.inc"

struct Dn2CppCultureNumericGroups
{
    int8_t sizes[DN2CPP_MAX_GROUP_SIZES];
    int8_t count;
};

// Numeric profiles do not define culture identity. Currency symbols are stored
// separately; every numeric axis is independently representable.
struct Dn2CppCultureNumericData
{
    const char16_t* numberDecimal;
    const char16_t* numberGroup;
    const char16_t* currencyDecimal;
    const char16_t* currencyGroup;
    const char16_t* percentDecimal;
    const char16_t* percentGroup;
    const char16_t* negativeSign;
    const char16_t* positiveSign;
    const char16_t* nan;
    const char16_t* posInf;
    const char16_t* negInf;
    const char16_t* percentSymbol;
    const char16_t* perMilleSymbol;
    Dn2CppCultureNumericGroups numberGroups;
    Dn2CppCultureNumericGroups currencyGroups;
    Dn2CppCultureNumericGroups percentGroups;
    int8_t numberDigits;
    int8_t currencyDigits;
    int8_t percentDigits;
    int8_t numberNegPattern;
    int8_t currencyPosPattern;
    int8_t currencyNegPattern;
    int8_t percentPosPattern;
    int8_t percentNegPattern;
    bool emptyCurrency;
};

#if defined(_WIN32)
#include "dn2cpp_culture_numbering_windows.inc"
#else
#include "dn2cpp_culture_numbering.inc"
#endif
#include "dn2cpp_culture_fallback.inc"

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

static const char16_t* dn2cpp_culture_compatibility_name(Dn2CppString* name)
{
    if (dn2cpp_culture_name_eq_ci(name, u"zh-CHS"))
        return u"zh-Hans";
    if (dn2cpp_culture_name_eq_ci(name, u"zh-CHT"))
        return u"zh-Hant";
    return nullptr;
}

static const Dn2CppCultureRow* dn2cpp_culture_find_row(Dn2CppString* name)
{
    // Linear scan: this runs on `new CultureInfo(x)` and once for the host
    // default, never inside a format. A binary search would add an ordering
    // invariant that, broken, fails by silently missing rows.
    for (const Dn2CppCultureRow& row : kCultureIdentifierFormats)
        if (dn2cpp_culture_name_eq_ci(name, row.name))
            return &row;
    for (size_t i = 0; i < kCultureCount; i++)
        if (dn2cpp_culture_name_eq_ci(name, kCultures[i].name))
            return &kCultures[i];
    return nullptr;
}

static const Dn2CppCultureRow* dn2cpp_culture_find_metadata_row(Dn2CppString* name)
{
    // Legacy names share the modern LCID but retain their public identity
    // and country-shaped neutrality, independently of numeric resources.
    Dn2CppString compatible{};
    if (const char16_t* fallback = dn2cpp_culture_compatibility_name(name))
    {
        compatible.chars = fallback;
        compatible.length = static_cast<int32_t>(std::char_traits<char16_t>::length(fallback));
        name = &compatible;
    }
    return dn2cpp_culture_find_row(name);
}

static bool dn2cpp_culture_ascii_alnum(char16_t c)
{
    return (c >= u'a' && c <= u'z') || (c >= u'A' && c <= u'Z')
        || (c >= u'0' && c <= u'9');
}

// CultureData.AnsiToLower leaves non-ASCII units intact, even in refused names.
static Dn2CppString* dn2cpp_culture_lower_ascii(Dn2CppString* name)
{
    std::u16string lower(name->chars, static_cast<size_t>(name->length));
    for (char16_t& c : lower)
        if (c >= u'A' && c <= u'Z')
            c = static_cast<char16_t>(c - u'A' + u'a');
    return dn2cpp_string_from_chars(lower.data(), name->length);
}

static bool dn2cpp_culture_is_script(const char16_t* chars, int32_t count)
{
    if (count != 4)
        return false;
    for (int32_t i = 0; i < count; i++)
        if (!((chars[i] >= u'a' && chars[i] <= u'z') || (chars[i] >= u'A' && chars[i] <= u'Z')))
            return false;
    return true;
}

// CultureData's ICU validation runs before native canonicalization. NUL is
// accepted for compatibility, but ICU only consumes the prefix before it.
static std::u16string dn2cpp_culture_lower(std::u16string value)
{
    for (char16_t& c : value)
        if (c >= u'A' && c <= u'Z')
            c = static_cast<char16_t>(c - u'A' + u'a');
    return value;
}

static void dn2cpp_culture_recase_script(std::u16string& name)
{
    size_t start = name.find(u'-');
    if (start == std::u16string::npos)
        return;
    start++;
    size_t end = name.find(u'-', start);
    if (end == std::u16string::npos)
        end = name.size();
    if (end - start != 4)
        return;
    for (size_t i = start; i < end; i++)
        if (!((name[i] >= u'a' && name[i] <= u'z') || (name[i] >= u'A' && name[i] <= u'Z')))
            return;
    for (size_t i = start; i < end; i++)
        if (i == start && name[i] >= u'a' && name[i] <= u'z')
            name[i] = static_cast<char16_t>(name[i] - u'a' + u'A');
        else if (i > start && name[i] >= u'A' && name[i] <= u'Z')
            name[i] = static_cast<char16_t>(name[i] - u'A' + u'a');
}

static void dn2cpp_culture_sort_variants(std::u16string& name,
    Dn2CppLocaleExtensionState<char16_t>& extensions)
{
    Dn2CppLocaleVariantState parsed = dn2cpp_locale_sort_variants(name.data(), name.size());
    name.resize(parsed.length);
    extensions = dn2cpp_locale_identity_extensions(name.data(), name.size(), parsed);
    name.resize(extensions.baseEnd);
}

static std::u16string dn2cpp_culture_legacy_base(std::u16string name)
{
    for (size_t i = 0; i + 2 < name.size(); i++)
        if (name[i] == u'-' && name[i + 2] == u'-'
            && (name[i + 1] == u'u' || name[i + 1] == u'U'
                || name[i + 1] == u't' || name[i + 1] == u'T'
                || name[i + 1] == u'x' || name[i + 1] == u'X'))
        {
            name.resize(i);
            break;
        }
    return name;
}

static bool dn2cpp_culture_canonicalize_language_tag(std::u16string& name,
    Dn2CppLocaleExtensionState<char16_t>& extensions)
{
    if (name.find(u'_') != std::u16string::npos)
        return false;
    std::u16string lower = dn2cpp_culture_lower(name);
    size_t languageEnd = name.find(u'-');
    bool languageTag = languageEnd == 1;
    for (size_t i = 0; i + 2 < name.size(); i++)
        if (name[i] == u'-' && name[i + 2] == u'-')
            languageTag = true;
    // Legacy ICU aliases also canonicalize without a BCP47 extension.
    static const Dn2CppCultureNameMapping bareAliases[] = {
        { u"art-lojban", u"jbo" }, { u"zh-guoyu", u"zh" },
        { u"zh-hakka", u"hak" }, { u"zh-min-nan", u"nan" },
        { u"zh-xiang", u"hsn" },
    };
    if (!languageTag)
    {
        for (const Dn2CppCultureNameMapping& alias : bareAliases)
            if (lower == alias.name)
            {
                name = alias.target;
                return false;
            }
        return false;
    }
    size_t legacyLength = 0;
    const char16_t* target = nullptr;
    for (const Dn2CppCultureNameMapping& legacy : kCultureNumberingLegacyNames)
    {
        size_t count = std::char_traits<char16_t>::length(legacy.name);
        if (count > legacyLength && lower.compare(0, count, legacy.name) == 0
            && (lower.size() == count || lower[count] == u'-'))
        {
            legacyLength = count;
            target = legacy.target;
        }
    }
    if (target != nullptr)
    {
        std::u16string base(target);
        size_t privateUse = base.find(u"-x-");
        if (privateUse != std::u16string::npos)
        {
            base.resize(privateUse);
            // The introduced private-use keyword absorbs the entire suffix;
            // only CultureData's original u/t spelling can reappear later.
            name = base == u"und" ? u"" : base;
            extensions = {name.size(), true, false, {}, 0, false, false};
            return true;
        }
        if (base == u"und")
            base.clear();
        for (size_t i = base.find(u'-'); i != std::u16string::npos && i < base.size(); i++)
            if (base[i] >= u'a' && base[i] <= u'z')
                base[i] = static_cast<char16_t>(base[i] - u'a' + u'A');
        name.replace(0, legacyLength, base);
        dn2cpp_culture_recase_script(name);
        dn2cpp_culture_sort_variants(name, extensions);
        return true;
    }
    // BCP47's first extlang becomes ICU's language; the legacy locale path
    // keeps an identically spelled three-letter country instead.
    bool alphabetic = languageEnd >= 2 && languageEnd <= 3;
    for (size_t i = 0; alphabetic && i < languageEnd; i++)
        alphabetic = lower[i] >= u'a' && lower[i] <= u'z';
    bool undefinedExtlang = false;
    std::u16string legacyName;
    if (alphabetic && languageEnd + 4 <= name.size())
    {
        size_t end = name.find(u'-', languageEnd + 1);
        if (end == languageEnd + 4)
        {
            bool letters = true;
            for (size_t i = languageEnd + 1; i < end; i++)
                letters = letters && lower[i] >= u'a' && lower[i] <= u'z';
            if (letters)
            {
                undefinedExtlang = lower.compare(languageEnd + 1, 3, u"und") == 0;
                if (undefinedExtlang)
                    legacyName = dn2cpp_culture_legacy_base(name);
                size_t tail = end;
                for (int count = 1; count < 3 && tail < name.size(); count++)
                {
                    size_t next = name.find(u'-', tail + 1);
                    if (next == std::u16string::npos)
                        next = name.size();
                    if (next - tail != 4)
                        break;
                    bool extlang = true;
                    for (size_t i = tail + 1; i < next; i++)
                        extlang = extlang && lower[i] >= u'a' && lower[i] <= u'z';
                    if (!extlang)
                        break;
                    tail = next;
                }
                size_t next = name.find(u'-', tail + 1);
                if (next == std::u16string::npos)
                    next = name.size();
                bool extraExtlang = tail < name.size() && next - tail == 4;
                for (size_t i = tail + 1; extraExtlang && i < next; i++)
                    extraExtlang = lower[i] >= u'a' && lower[i] <= u'z';
                // A fourth extlang ends ICU's BCP47 parse before any region
                // or extension; it cannot become the promoted language's country.
                name = lower.substr(languageEnd + 1, 3)
                    + (extraExtlang ? std::u16string() : name.substr(tail));
                dn2cpp_culture_recase_script(name);
            }
        }
    }
    dn2cpp_culture_sort_variants(name, extensions);
    if (undefinedExtlang)
    {
        // An empty converted locale retries the original legacy name.
        if (name == u"und" && !extensions.keywords)
        {
            name = legacyName;
            return false;
        }
        if (name.compare(0, 4, u"und-") == 0)
            name.erase(0, 3);
    }
    return true;
}

static bool dn2cpp_culture_private_use_only(const std::u16string& name)
{
    if (name.size() < 3 || name[0] != u'x' || name[1] != u'-')
        return false;
    for (size_t start = 2; start < name.size();)
    {
        size_t end = name.find(u'-', start);
        if (end == std::u16string::npos)
            end = name.size();
        size_t count = end - start;
        if (count == 0 || count > 8)
            return false;
        for (size_t i = start; i < end; i++)
            if (!dn2cpp_culture_ascii_alnum(name[i]))
                return false;
        // Managed u/t suffixes and legacy lvariant spelling retain an identity.
        char16_t marker = name[start];
        if (marker >= u'A' && marker <= u'Z')
            marker = static_cast<char16_t>(marker - u'A' + u'a');
        if ((count == 1 && end < name.size() && (marker == u'u' || marker == u't'))
            || (count == 8 && dn2cpp_culture_lower(name.substr(start, count)) == u"lvariant"))
            return false;
        start = end + 1;
    }
    return true;
}

static Dn2CppString* dn2cpp_culture_normalize_name(Dn2CppString* name,
    bool* customUndefined = nullptr, bool* hasKeywords = nullptr)
{
    if (customUndefined != nullptr)
        *customUndefined = false;
    if (hasKeywords != nullptr)
        *hasKeywords = false;
    if (name->length == 1 || name->length > 85)
        return nullptr;
    int32_t extension = -1;
    bool underscore = false;
    int32_t length = name->length;
    for (int32_t i = 0; i < name->length; i++)
    {
        char16_t c = name->chars[i];
        if (c == u'\0')
        {
            if (length == name->length)
                length = i;
            continue;
        }
        if (dn2cpp_culture_ascii_alnum(c))
            continue;
        if (c != u'-' && c != u'_')
            return nullptr;
        if (i == 0 || i == name->length - 1
            || name->chars[i - 1] == u'-' || name->chars[i - 1] == u'_')
            return nullptr;
        if (c == u'_')
        {
            if (underscore)
                return nullptr;
            underscore = true;
        }
        else if (extension < 0 && i + 2 < name->length
            && (name->chars[i + 1] == u'u' || name->chars[i + 1] == u't')
            && name->chars[i + 2] == u'-')
        {
            if (name->chars[i + 1] == u't' || i + 5 >= name->length
                || name->chars[i + 3] != u'c' || name->chars[i + 4] != u'o'
                || name->chars[i + 5] != u'-')
                extension = i;
        }
    }
    char16_t normalized[86];
    int32_t start = 0;
    int32_t part = 0;
    bool privateUse = name->length > 1 && name->chars[1] == u'-'
        && (name->chars[0] == u'x' || name->chars[0] == u'X');
    while (start < length)
    {
        int32_t end = start;
        while (end < length && name->chars[end] != u'-' && name->chars[end] != u'_')
            end++;
        int32_t count = end - start;
        // ICU's language buffer includes its terminator.
        if (part == 0 && count >= 12)
            return nullptr;
        bool script = part == (privateUse ? 2 : 1)
            && dn2cpp_culture_is_script(name->chars + start, count);
        for (int32_t i = start; i < end; i++)
        {
            char16_t c = name->chars[i];
            bool lower = part == 0 || (privateUse && part == 1) || (script && i != start);
            if (lower && c >= u'A' && c <= u'Z')
                c = static_cast<char16_t>(c - u'A' + u'a');
            else if (!lower && c >= u'a' && c <= u'z')
                c = static_cast<char16_t>(c - u'a' + u'A');
            normalized[i] = c;
        }
        if (end < length)
            normalized[end] = name->chars[end];
        start = end + 1;
        part++;
    }
#if defined(_WIN32)
    std::u16string legacy(normalized, static_cast<size_t>(length));
#endif
    if (extension >= 0 && extension < length)
        for (int32_t i = extension; i < length; i++)
        {
            char16_t c = name->chars[i];
            normalized[i] = c >= u'A' && c <= u'Z' ? static_cast<char16_t>(c - u'A' + u'a') : c;
        }
    // ICU recognizes extension markers without regard to case, even when
    // CultureData does not retain that extension in the public name.
    for (int32_t i = 0; i + 2 < length; i++)
        if (normalized[i] == u'-'
            && (normalized[i + 1] == u'U' || normalized[i + 1] == u'T')
            && normalized[i + 2] == u'-')
        {
            for (int32_t j = i; j < length; j++)
                if (normalized[j] >= u'A' && normalized[j] <= u'Z')
                    normalized[j] = static_cast<char16_t>(normalized[j] - u'A' + u'a');
            break;
        }
    std::u16string canonical(normalized, static_cast<size_t>(length));
    std::u16string undefinedLegacy;
    if (canonical.compare(0, 4, u"und-") == 0)
        undefinedLegacy = dn2cpp_culture_legacy_base(canonical);
    Dn2CppLocaleExtensionState<char16_t> extensions{};
    bool languageTag = dn2cpp_culture_canonicalize_language_tag(canonical, extensions);
    bool privateUseOnly = languageTag && !underscore && dn2cpp_culture_private_use_only(canonical);
    if (privateUseOnly)
        canonical.clear();
    if (hasKeywords != nullptr)
        *hasKeywords = extensions.keywords;
    if (customUndefined != nullptr)
        *customUndefined = privateUseOnly || (languageTag && canonical == u"und");
    // Without keywords, undefined-language names keep their legacy base.
    // Custom identity is independent of the platform's visible und spelling.
    if (canonical == u"und" && !extensions.keywords && !undefinedLegacy.empty())
#if defined(_WIN32)
        // Windows ICU retries the entire legacy identifier when the converted
        // language tag is empty, including an unconsumed extension suffix.
        canonical = legacy;
#else
        canonical = undefinedLegacy;
#endif
    if (canonical == u"und" && languageTag)
    {
#if defined(__APPLE__)
        if (extensions.keywords && !extensions.nonPrivateKeywords)
            canonical.clear();
#else
        canonical.clear();
#endif
    }
    size_t languageEnd = canonical.find_first_of(u"-_");
    if (languageEnd == std::u16string::npos)
        languageEnd = canonical.size();
    Dn2CppString language{};
    language.chars = canonical.data();
    language.length = static_cast<int32_t>(languageEnd);
    for (const Dn2CppCultureNameMapping& alias : kCultureLanguageAliases)
        if (dn2cpp_culture_name_eq_ci(&language, alias.name))
        {
            canonical.replace(0, languageEnd, alias.target);
            break;
        }
    if (canonical == u"und" && !languageTag)
        canonical.clear();
    // An underscore introduces a sort identifier, not a country. ICU keeps
    // its spelling; the managed name limits it to the Windows sort width.
    size_t sort = canonical.find(u'_');
    if (sort != std::u16string::npos)
    {
        int32_t originalSort = 0;
        while (originalSort < length && name->chars[originalSort] != u'_')
            originalSort++;
        canonical.resize(sort + 1);
        int32_t count = length - originalSort - 1;
        if (count > 8)
            count = 8;
        canonical.append(name->chars + originalSort + 1, static_cast<size_t>(count));
    }
    else if (languageTag)
    {
        if (extensions.keywords && extension >= 0 && extension < length)
            canonical.append(name->chars + extension, static_cast<size_t>(length - extension));
        if (extensions.collation)
        {
            canonical += u'_';
            canonical.append(extensions.sort, extensions.sortLength);
        }
    }
    // .NET's Windows ICU path also validates the resolved real name through
    // NLS. Validate after aliases and extensions, never the requested spelling.
    if (!dn2cpp_pal_locale_name_supported(canonical.data(), static_cast<int32_t>(canonical.size())))
    {
        // CultureData retries only the exact legacy Chinese identifiers.
        // Validate the modern resource name without replacing public identity.
        Dn2CppString resolved{};
        resolved.chars = canonical.data();
        resolved.length = static_cast<int32_t>(canonical.size());
        const char16_t* fallback = dn2cpp_culture_compatibility_name(&resolved);
        if (fallback == nullptr || !dn2cpp_pal_locale_name_supported(fallback,
            static_cast<int32_t>(std::char_traits<char16_t>::length(fallback))))
            return nullptr;
    }
    return dn2cpp_string_from_chars(canonical.data(), static_cast<int32_t>(canonical.size()));
}

// Never remove a requested country to find numeric data for another country.
static int32_t dn2cpp_culture_region_end(Dn2CppString* name, bool privateUseSort = false)
{
    int32_t start = 0;
    int32_t part = 0;
    bool privateUse = name->length > 2 && name->chars[0] == u'x' && name->chars[1] == u'-';
    if (privateUse)
    {
        int32_t firstEnd = 2;
        while (firstEnd < name->length && name->chars[firstEnd] != u'-'
            && name->chars[firstEnd] != u'_')
            firstEnd++;
        for (int32_t i = firstEnd; i < name->length; i++)
            privateUseSort = privateUseSort || name->chars[i] == u'_';
        // Valid private-use bases without a sort carry no country; an overlong
        // first subtag instead takes ICU's legacy locale path.
        if (!privateUseSort && firstEnd - 2 <= 8)
            return 0;
    }
    while (start < name->length)
    {
        int32_t end = start;
        while (end < name->length && name->chars[end] != u'-' && name->chars[end] != u'_')
            end++;
        int32_t count = end - start;
        if (part > 0)
        {
            if (name->chars[start - 1] == u'_')
                return 0;
            if (privateUse && part == 1)
            {
                start = end + 1;
                part++;
                continue;
            }
            if (part == (privateUse ? 2 : 1)
                && dn2cpp_culture_is_script(name->chars + start, count))
            {
                start = end + 1;
                part++;
                continue;
            }
            return count == 2 || count == 3 ? end : 0;
        }
        start = end + 1;
        part++;
    }
    return 0;
}

static Dn2CppString* dn2cpp_culture_public_name(Dn2CppString* input, Dn2CppString* canonical)
{
    bool legacySort = false;
    for (int32_t i = 0; i < input->length && input->chars[i] != u'\0'; i++)
        legacySort = legacySort || input->chars[i] == u'_';
    if (!legacySort)
    {
        for (int32_t i = 0; i < canonical->length; i++)
            if (canonical->chars[i] == u'_' && dn2cpp_culture_region_end(canonical) != 0)
                return dn2cpp_string_from_chars(canonical->chars, i);
        return canonical;
    }
    for (int32_t i = 0; i < canonical->length; i++)
        if (canonical->chars[i] == u'_')
        {
            // A managed extension retained after a private-use base keeps its
            // legacy sort in Name even when that base has a region.
            if (canonical->length > 2 && canonical->chars[0] == u'x'
                && canonical->chars[1] == u'-')
                for (int32_t j = 2; j + 2 < i; j++)
                    if (canonical->chars[j] == u'-' && canonical->chars[j + 2] == u'-'
                        && (canonical->chars[j + 1] == u'u' || canonical->chars[j + 1] == u't'))
                        return canonical;
            if (dn2cpp_culture_region_end(canonical) == 0)
                return canonical;
            for (int32_t j = 0; j + 5 < input->length; j++)
                if (input->chars[j] == u'-' && input->chars[j + 1] == u'u'
                    && input->chars[j + 2] == u'-'
                    && (input->chars[j + 3] == u'c' || input->chars[j + 3] == u'C')
                    && (input->chars[j + 4] == u'o' || input->chars[j + 4] == u'O')
                    && input->chars[j + 5] == u'-'
                    && (input->chars[j + 3] != u'c' || input->chars[j + 4] != u'o'))
                {
                    int32_t end = j;
                    while (end < input->length && input->chars[end] != u'\0')
                        end++;
                    std::u16string publicName(canonical->chars, static_cast<size_t>(i));
                    publicName.append(input->chars + j, static_cast<size_t>(end - j));
                    return dn2cpp_string_from_chars(publicName.data(), static_cast<int32_t>(publicName.size()));
                }
            return dn2cpp_string_from_chars(canonical->chars, i);
        }
    int32_t canonicalExtension = -1;
    for (int32_t i = 0; i + 2 < canonical->length; i++)
        if (canonical->chars[i] == u'-'
            && (canonical->chars[i + 1] == u'u' || canonical->chars[i + 1] == u't')
            && canonical->chars[i + 2] == u'-')
        {
            canonicalExtension = i;
            break;
        }
    if (canonicalExtension < 0)
        return canonical;
    for (int32_t i = 0; i + 2 < input->length; i++)
        if (input->chars[i] == u'-'
            && (input->chars[i + 1] == u'u' || input->chars[i + 1] == u't'
                || input->chars[i + 1] == u'U' || input->chars[i + 1] == u'T')
            && input->chars[i + 2] == u'-')
        {
            if (input->chars[i + 1] == u'U' || input->chars[i + 1] == u'T')
                return dn2cpp_string_from_chars(canonical->chars, canonicalExtension);
            int32_t end = i;
            while (end < input->length && input->chars[end] != u'\0')
                end++;
            std::u16string publicName(canonical->chars, static_cast<size_t>(canonicalExtension));
            publicName.append(input->chars + i, static_cast<size_t>(end - i));
            return dn2cpp_string_from_chars(publicName.data(), static_cast<int32_t>(publicName.size()));
        }
    return canonical;
}

static bool dn2cpp_culture_is_posix(Dn2CppString* name)
{
    if (name->length < 11)
        return false;
    Dn2CppString base = *name;
    base.length = 11;
    return dn2cpp_culture_name_eq_ci(&base, u"en-US-POSIX")
        && (name->length == 11 || (name->length > 12 && name->chars[11] == u'_')
            || (name->length > 13
            && name->chars[11] == u'-' && name->chars[13] == u'-'));
}

static const Dn2CppCultureRow* dn2cpp_culture_find_format_row(Dn2CppString* name)
{
    if (const char16_t* resource = dn2cpp_culture_compatibility_name(name))
    {
        // Windows retries through the modern script. Native ICU accepts the
        // original country-shaped name, whose numeric resources inherit zh.
#if !defined(_WIN32)
        resource = u"zh";
#endif
        Dn2CppString resolved{};
        resolved.chars = resource;
        resolved.length = static_cast<int32_t>(std::char_traits<char16_t>::length(resource));
        return dn2cpp_culture_find_row(&resolved);
    }
    if (dn2cpp_culture_is_posix(name))
        return nullptr;
    Dn2CppString prefix = *name;
    int32_t regionEnd = dn2cpp_culture_region_end(name);
    for (;;)
    {
        if (const Dn2CppCultureRow* row = dn2cpp_culture_find_row(&prefix))
            return row;
        int32_t languageEnd = 0;
        while (languageEnd < prefix.length && prefix.chars[languageEnd] != u'-')
            languageEnd++;
        int32_t scriptEnd = languageEnd + 5;
        if (languageEnd == 2 && prefix.chars[0] == u'd' && prefix.chars[1] == u'e'
            && scriptEnd < prefix.length && prefix.chars[scriptEnd] == u'-')
        {
            if (std::memcmp(prefix.chars + languageEnd + 1, u"Latn", 4 * sizeof(char16_t)) == 0)
            {
                // German's default Latin script shares its regional numeric data.
                // Other scripts need their own data; country alone is insufficient.
                char16_t withoutScript[86];
                std::memcpy(withoutScript, prefix.chars, static_cast<size_t>(languageEnd) * sizeof(char16_t));
                std::memcpy(withoutScript + languageEnd, prefix.chars + scriptEnd,
                    static_cast<size_t>(prefix.length - scriptEnd) * sizeof(char16_t));
                Dn2CppString regional = prefix;
                regional.chars = withoutScript;
                regional.length -= 5;
                if (const Dn2CppCultureRow* row = dn2cpp_culture_find_row(&regional))
                    return row;
            }
        }
        int32_t end = prefix.length;
        while (end > 0 && prefix.chars[end - 1] != u'-' && prefix.chars[end - 1] != u'_')
            end--;
        if (end == 0 || end - 1 < regionEnd)
            return nullptr;
        prefix.length = end - 1;
    }
}

// A materialized culture. The table is pure ROM; the Dn2CppNumberFormatInfo a
// caller gets is built on first ask and cached, so the table itself costs no
// allocation. Nodes are GC-heap and rooted through g_cultureCache, a scanned
// static slot, so the chain and its strings stay alive. The canonical name is
// the identity; sharing a formatting row must not discard a requested variant.
struct Dn2CppCultureCacheNode
{
    Dn2CppNumberFormatInfo nfi;
    const Dn2CppCultureRow* row; // null for an unmodeled name
    Dn2CppString* identity;
    const Dn2CppCultureNumericData* numeric;
    const Dn2CppCultureFallbackEntry* fallback;
    Dn2CppCultureCacheNode* next;
};
static DN2CPP_GC_STATIC_ROOT Dn2CppCultureCacheNode* g_cultureCache = nullptr;
static std::mutex& g_cultureCacheMutex = dn2cpp_never_destroyed<std::mutex>();

static bool dn2cpp_culture_numbering_type(Dn2CppString* identity, std::u16string& value)
{
    int32_t length = 0;
    // ICU stops at NUL. An underscore selects its legacy collation path,
    // where Unicode numbering extensions are not interpreted.
    while (length < identity->length && identity->chars[length] != u'\0')
    {
        if (identity->chars[length] == u'_')
            return false;
        length++;
    }
    std::u16string locale(identity->chars, static_cast<size_t>(length));
    size_t legacyLength = 0;
    const char16_t* legacyTarget = nullptr;
    for (const Dn2CppCultureNameMapping& legacy : kCultureNumberingLegacyNames)
    {
        size_t count = std::char_traits<char16_t>::length(legacy.name);
        if (count > legacyLength && locale.compare(0, count, legacy.name) == 0
            && (locale.size() == count || locale[count] == u'-'))
        {
            legacyLength = count;
            legacyTarget = legacy.target;
        }
    }
    if (legacyTarget != nullptr)
        locale.replace(0, legacyLength, legacyTarget);
    size_t start = 0;
    bool unicodeExtension = false;
    int phase = 0; // 0: extlangs/script, 2: region, 3: variants, 4: extensions
    int extlangs = 0;
    uint64_t singletons = 0;
    std::vector<std::u16string> variants;
    size_t languageLength = 0;
    bool extensionValue = false;
    while (start < locale.size())
    {
        size_t end = locale.find(u'-', start);
        if (end == std::u16string::npos)
            end = locale.size();
        size_t count = end - start;
        if (count == 0 || count > 8)
            return false;
        bool alpha = true, digits = true;
        for (size_t i = start; i < end; i++)
        {
            alpha = alpha && locale[i] >= u'a' && locale[i] <= u'z';
            digits = digits && locale[i] >= u'0' && locale[i] <= u'9';
        }
        if (start == 0)
        {
            if (count < 2 || !alpha) return false;
            languageLength = count;
        }
        else if (count == 1)
        {
            // ICU stops before an empty or duplicate singleton extension.
            if (phase == 4 && !extensionValue) return false;
            char16_t marker = locale[start];
            int index = marker >= u'a' ? marker - u'a' + 10 : marker - u'0';
            uint64_t bit = uint64_t{1} << index;
            if ((singletons & bit) != 0) return false;
            singletons |= bit;
            if (marker == u'x')
                return false;
            if (marker == u'u')
            {
                unicodeExtension = true;
                start = end + 1;
                break;
            }
            phase = 4;
            extensionValue = false;
        }
        else if (phase == 4)
            extensionValue = true;
        else if (phase == 0 && languageLength <= 3 && count == 3 && alpha && extlangs < 3)
            extlangs++;
        else if (phase <= 1 && count == 4 && alpha)
            phase = 2;
        else if (phase <= 2 && ((count == 2 && alpha) || (count == 3 && digits)))
            phase = 3;
        else
        {
            if (!(count >= 5 || (count == 4 && locale[start] >= u'0' && locale[start] <= u'9')))
                return false;
            std::u16string variant = locale.substr(start, count);
            for (const std::u16string& prior : variants)
                if (prior == variant) return false;
            variants.push_back(variant);
            phase = 3;
        }
        start = end + 1;
    }
    if (!unicodeExtension)
        return false;
    while (start < locale.size())
    {
        size_t end = locale.find(u'-', start);
        if (end == std::u16string::npos)
            end = locale.size();
        size_t count = end - start;
        if (count == 1 || count > 8)
            break;
        if (count != 2 || locale[start + 1] < u'a' || locale[start + 1] > u'z')
        {
            start = end + 1;
            continue;
        }
        bool numbering = locale[start] == u'n' && locale[start + 1] == u'u';
        size_t valueStart = end + 1;
        size_t valueEnd = end;
        start = valueStart;
        while (start < locale.size())
        {
            end = locale.find(u'-', start);
            if (end == std::u16string::npos)
                end = locale.size();
            size_t typeLength = end - start;
            // ICU recognizes a two-character key only with an alphabetic
            // second character; other pairs remain part of the full value.
            if (typeLength < 2 || typeLength > 8
                || (typeLength == 2 && locale[start + 1] >= u'a' && locale[start + 1] <= u'z'))
                break;
            valueEnd = end;
            start = end + 1;
        }
        if (numbering)
        {
            value = valueEnd >= valueStart ? locale.substr(valueStart, valueEnd - valueStart) : u"";
            return true;
        }
    }
    return false;
}

static const Dn2CppCultureNumericData* dn2cpp_culture_numeric_data(
    const Dn2CppCultureRow* row, Dn2CppString* request)
{
    std::u16string value;
    if (!dn2cpp_culture_numbering_type(request, value))
        return nullptr;
    uint8_t family = kNumberingUnknownFamily;
    for (const Dn2CppCultureNumberingType& type : kCultureNumberingTypes)
        if (value == type.name)
        {
            family = type.family;
            break;
        }
    if (family == kNumberingUnknownFamily)
        return &kCultureNumericProfiles[kNumberingUnknownProfile];
    if (family == kNumberingAlgorithmicFamily)
        return &kCultureNumericProfiles[kNumberingAlgorithmicProfile];
    if (row != nullptr)
    {
        Dn2CppString name{};
        name.chars = row->name;
        while (row->name[name.length] != u'\0')
            name.length++;
        for (const Dn2CppCultureNumberingLocale& locale : kCultureNumberingLocales)
            if (dn2cpp_culture_name_eq_ci(&name, locale.name))
                return &kCultureNumericProfiles[kCultureNumericVectors[locale.vector][family]];
    }
    return nullptr;
}

static size_t dn2cpp_culture_fallback_host_index(Dn2CppString* name)
{
    if (name == nullptr)
        return 0;
    Dn2CppString prefix = *name;
    while (prefix.length > 0)
    {
        for (size_t i = 0; i < sizeof(kCultureFallbackHosts) / sizeof(kCultureFallbackHosts[0]); i++)
            if (dn2cpp_culture_name_eq_ci(&prefix, kCultureFallbackHosts[i]))
                return i;
        int32_t end = prefix.length;
        while (end > 0 && prefix.chars[end - 1] != u'-' && prefix.chars[end - 1] != u'_')
            end--;
        prefix.length = end > 0 ? end - 1 : 0;
    }
    return 0;
}

static size_t dn2cpp_culture_fallback_host()
{
    // This descriptor cannot call by_name or host_default: either can be the
    // caller while resolving an undefined host locale for the first time.
    static const size_t host = [] {
        char name[128];
        int32_t length = dn2cpp_pal_numeric_fallback_locale_name(name, sizeof(name));
        Dn2CppString* requested = length > 0 ? dn2cpp_string_from_utf8(name, length) : nullptr;
        if (requested != nullptr)
        {
            std::u16string raw = dn2cpp_culture_lower(std::u16string(requested->chars, requested->length));
            // A private-use ICU default does not select the alias's public
            // resource language. CultureInfo's displayed name omits that state.
            if (raw == u"i-default" || raw.compare(0, 10, u"i-default-") == 0)
                return size_t{0};
            if (raw.compare(0, 12, u"en-us-posix-") == 0)
            {
                Dn2CppLocalePosixState parsed = dn2cpp_locale_posix_parse(raw.data(), raw.size());
                Dn2CppString resource{};
                resource.chars = parsed.ordinaryResources ? u"en-US" : u"en-US-POSIX";
                resource.length = parsed.ordinaryResources ? 5 : 11;
                return dn2cpp_culture_fallback_host_index(&resource);
            }
        }
        Dn2CppString* canonical = requested != nullptr ? dn2cpp_culture_normalize_name(requested) : nullptr;
        return dn2cpp_culture_fallback_host_index(canonical);
    }();
    return host;
}

static const Dn2CppCultureFallbackEntry* dn2cpp_culture_fallback_entry(
    Dn2CppString* name, Dn2CppString* request)
{
    size_t host = 0;
    Dn2CppString language = *name;
    language.length = 0;
    while (language.length < name->length && name->chars[language.length] != u'-'
        && name->chars[language.length] != u'_')
        language.length++;
    bool root = language.length == 0 && name->length > 0;
#if defined(__APPLE__)
    root = root || dn2cpp_culture_name_eq_ci(&language, u"nv")
        || dn2cpp_culture_name_eq_ci(&language, u"jbo");
#endif
    if (dn2cpp_culture_is_posix(name))
    {
        Dn2CppString posix{};
        posix.chars = u"en-US-POSIX";
        posix.length = 11;
        host = dn2cpp_culture_fallback_host_index(&posix);
    }
    else if (dn2cpp_culture_name_eq_ci(request, u"zh-min"))
    {
        Dn2CppString english{};
        english.chars = u"en";
        english.length = 2;
        host = dn2cpp_culture_fallback_host_index(&english);
    }
    else if (!root)
    {
        if (language.length < 2)
            return nullptr;
        for (int32_t i = 0; i < language.length; i++)
            if (!((language.chars[i] >= u'a' && language.chars[i] <= u'z')
                || (language.chars[i] >= u'A' && language.chars[i] <= u'Z')))
                return nullptr;
        for (const char16_t* resource : kCultureResourceLanguages)
            if (dn2cpp_culture_name_eq_ci(&language, resource))
                return nullptr;
        host = dn2cpp_culture_fallback_host();
    }
    int32_t regionEnd = dn2cpp_culture_region_end(name);
    Dn2CppString region{};
    if (regionEnd > 0)
    {
        int32_t start = regionEnd;
        while (start > 0 && name->chars[start - 1] != u'-')
            start--;
        region.chars = name->chars + start;
        region.length = regionEnd - start;
    }
    for (size_t i = 0; i < sizeof(kCultureFallbackRegions) / sizeof(kCultureFallbackRegions[0]); i++)
    {
        if (dn2cpp_culture_name_eq_ci(&region, kCultureFallbackRegions[i]))
            return &kCultureFallbackEntries[host][i];
    }
    return &kCultureFallbackEntries[host][0];
}

static const Dn2CppCultureNumericData* dn2cpp_culture_fallback_numeric(
    const Dn2CppCultureFallbackEntry* entry, Dn2CppString* request)
{
    uint8_t family = 0;
    std::u16string value;
    if (dn2cpp_culture_numbering_type(request, value))
    {
        family = kFallbackUnknownFamily;
        for (const Dn2CppCultureNumberingType& type : kCultureFallbackTypes)
            if (value == type.name)
            {
                family = type.family;
                break;
            }
    }
    return &kCultureFallbackProfiles[kCultureFallbackVectors[entry->vector][family]];
}

static void dn2cpp_culture_apply_numeric(Dn2CppNumberFormatInfo* n,
    const Dn2CppCultureNumericData* data, const Dn2CppCultureRow* row)
{
    n->numberDecimal = dn2cpp_lit16(data->numberDecimal);
    n->numberGroup = dn2cpp_lit16(data->numberGroup);
    n->currencyDecimal = dn2cpp_lit16(data->currencyDecimal);
    n->currencyGroup = dn2cpp_lit16(data->currencyGroup);
    n->percentDecimal = dn2cpp_lit16(data->percentDecimal);
    n->percentGroup = dn2cpp_lit16(data->percentGroup);
    n->negativeSign = dn2cpp_lit16(data->negativeSign);
    n->positiveSign = dn2cpp_lit16(data->positiveSign);
    n->nan = dn2cpp_lit16(data->nan);
    n->posInf = dn2cpp_lit16(data->posInf);
    n->negInf = dn2cpp_lit16(data->negInf);
    n->percentSymbol = dn2cpp_lit16(data->percentSymbol);
    n->perMilleSymbol = dn2cpp_lit16(data->perMilleSymbol);
    std::memcpy(n->groupSizes, data->numberGroups.sizes, sizeof(n->groupSizes));
    std::memcpy(n->currencyGroupSizes, data->currencyGroups.sizes, sizeof(n->currencyGroupSizes));
    std::memcpy(n->percentGroupSizes, data->percentGroups.sizes, sizeof(n->percentGroupSizes));
    n->groupSizeCount = data->numberGroups.count;
    n->currencyGroupSizeCount = data->currencyGroups.count;
    n->percentGroupSizeCount = data->percentGroups.count;
    n->numberDigits = data->numberDigits;
    n->currencyDigits = data->currencyDigits;
    n->percentDigits = data->percentDigits;
    n->numberNegPattern = data->numberNegPattern;
    n->currencyPosPattern = data->currencyPosPattern;
    n->currencyNegPattern = data->currencyNegPattern;
    n->percentPosPattern = data->percentPosPattern;
    n->percentNegPattern = data->percentNegPattern;
    if (data->emptyCurrency)
        n->currencySymbol = dn2cpp_lit16(u"");
    else if (row != nullptr)
    {
        Dn2CppString name{};
        name.chars = row->name;
        while (row->name[name.length] != u'\0')
            name.length++;
        for (const Dn2CppCultureNumberingLocale& locale : kCultureNumberingLocales)
            if (dn2cpp_culture_name_eq_ci(&name, locale.name))
            {
                n->currencySymbol = dn2cpp_lit16(locale.currency);
                break;
            }
    }
}

static void dn2cpp_culture_apply_format(Dn2CppNumberFormatInfo* n, const Dn2CppCultureRow* row)
{
    n->numberDecimal = n->percentDecimal = n->currencyDecimal =
        dn2cpp_lit16(row->decimalSep);
    n->numberGroup = n->percentGroup = n->currencyGroup =
        dn2cpp_lit16(row->groupSep);
    n->groupSizes[0] = row->groupSize0;
    n->groupSizes[1] = row->groupSize1;
    n->groupSizeCount = n->currencyGroupSizeCount = n->percentGroupSizeCount = row->groupSize1 < 0 ? 1 : 2;
    std::memcpy(n->currencyGroupSizes, n->groupSizes, sizeof(n->groupSizes));
    std::memcpy(n->percentGroupSizes, n->groupSizes, sizeof(n->groupSizes));
    n->negativeSign = dn2cpp_lit16(row->negativeSign);
    n->nan = dn2cpp_lit16(row->nan);
    n->posInf = dn2cpp_lit16(u"∞");
    n->negInf = dn2cpp_lit16(row->negInf);
    n->percentSymbol = dn2cpp_lit16(row->percent);
    n->currencySymbol = dn2cpp_lit16(row->currency);
    n->numberDigits = n->percentDigits = 3;
    n->currencyDigits = row->currencyDigits;
    n->currencyPosPattern = row->currencyPos;
    n->currencyNegPattern = row->currencyNeg;
    n->percentPosPattern = row->percentPos;
    n->percentNegPattern = row->percentNeg;
}

// Intern identity separately from numeric data. Fallback entries also select
// localized currency symbols, so they participate in the key. Caller holds no lock.
static const Dn2CppNumberFormatInfo* dn2cpp_culture_intern(const Dn2CppCultureRow* row,
    Dn2CppString* identity, Dn2CppString* name = nullptr, int32_t lcid = -1,
    const Dn2CppCultureNumericData* numeric = nullptr,
    const Dn2CppCultureFallbackEntry* fallback = nullptr)
{
    if (identity == nullptr)
        identity = dn2cpp_lit16(row->name);
    if (name == nullptr)
        name = identity;
    std::lock_guard<std::mutex> lock(g_cultureCacheMutex);
    for (Dn2CppCultureCacheNode* p = g_cultureCache; p != nullptr; p = p->next)
    {
        if (p->row == row && p->numeric == numeric && p->fallback == fallback
            && dn2cpp_string_equals(p->identity, identity)
            && dn2cpp_string_equals(p->nfi.cultureName, name))
            return &p->nfi;
    }
    // Allocate and LINK first, then fill: once linked, the node is reachable from
    // a scanned static slot rather than resting on the conservative stack scan.
    // The invariant copy keeps every field valid meanwhile.
    auto* node = static_cast<Dn2CppCultureCacheNode*>(dn2cpp_alloc(sizeof(Dn2CppCultureCacheNode)));
    node->nfi = *dn2cpp_nfi_invariant();
    node->row = row;
    node->numeric = numeric;
    node->fallback = fallback;
    dn2cpp_gc_store_ref(&node->next, g_cultureCache);
    g_cultureCache = node;
    dn2cpp_gc_store_ref(&node->identity, identity);
    dn2cpp_gc_store_ref(&node->nfi.cultureName, name);
    Dn2CppString* realName = name;
    bool sorted = false;
    for (int32_t i = 0; i < identity->length; i++)
        if (identity->chars[i] == u'_')
        {
            sorted = true;
            if (dn2cpp_string_equals(name, identity))
                realName = identity;
            else
            {
                std::u16string resolved(name->chars, static_cast<size_t>(name->length));
                resolved.append(identity->chars + i, static_cast<size_t>(identity->length - i));
                realName = dn2cpp_string_from_chars(resolved.data(), static_cast<int32_t>(resolved.size()));
            }
            break;
        }
    dn2cpp_gc_store_ref(&node->nfi.cultureRealName, realName);
    node->nfi.lcid = lcid >= 0 ? lcid : 4096;
    node->nfi.isNeutralCulture = dn2cpp_culture_region_end(name, sorted) == 0 ? 1 : 0;
    node->nfi.missingNumberFormat = node->nfi.lcid == 4096 && name->length == 0 ? 1 : 0;
    if (row == nullptr)
    {
        if (numeric != nullptr)
        {
            dn2cpp_culture_apply_numeric(&node->nfi, numeric, row);
            if (fallback != nullptr && !numeric->emptyCurrency)
                node->nfi.currencySymbol = dn2cpp_lit16(kCultureFallbackCurrencies[fallback->currency]);
        }
        // Without a numeric profile, known resource languages outside the
        // modeled inventory retain invariant symbols and their own identity.
        return &node->nfi;
    }
    if (dn2cpp_culture_name_eq_ci(name, row->name))
    {
        if (lcid < 0)
            node->nfi.lcid = row->lcid;
        node->nfi.isNeutralCulture = row->isNeutral;
    }
    dn2cpp_culture_apply_format(&node->nfi, row);
    if (numeric != nullptr)
        dn2cpp_culture_apply_numeric(&node->nfi, numeric, row);
    return &node->nfi;
}

// CultureData keys its case-insensitive cache separately for each override
// choice. GetCultureInfo lowercases before entering the no-override cache.
struct Dn2CppCultureRequestCacheNode
{
    Dn2CppString* key;
    const Dn2CppNumberFormatInfo* nfi;
    bool useUserOverride;
    Dn2CppCultureRequestCacheNode* next;
};
static DN2CPP_GC_STATIC_ROOT Dn2CppCultureRequestCacheNode* g_cultureRequestCache = nullptr;

static const Dn2CppNumberFormatInfo* dn2cpp_culture_request_cache(Dn2CppString* key,
    bool useUserOverride, const Dn2CppNumberFormatInfo* resolved = nullptr)
{
    std::lock_guard<std::mutex> lock(g_cultureCacheMutex);
    for (Dn2CppCultureRequestCacheNode* p = g_cultureRequestCache; p != nullptr; p = p->next)
        if (p->useUserOverride == useUserOverride && dn2cpp_string_equals(p->key, key))
            return p->nfi;
    if (resolved == nullptr)
        return nullptr;
    auto* node = static_cast<Dn2CppCultureRequestCacheNode*>(dn2cpp_alloc(sizeof(Dn2CppCultureRequestCacheNode)));
    dn2cpp_gc_store_ref(&node->next, g_cultureRequestCache);
    g_cultureRequestCache = node;
    dn2cpp_gc_store_ref(&node->key, key);
    dn2cpp_gc_store_ref(&node->nfi, resolved);
    node->useUserOverride = useUserOverride;
    return resolved;
}

// CultureInfo's object cache is separate from CultureData. Lookup uses the
// requested name, while insertion uses the resolved name (including its sort).
struct Dn2CppNamedCultureCacheNode
{
    Dn2CppString* key;
    const Dn2CppNumberFormatInfo* nfi;
    Dn2CppNamedCultureCacheNode* next;
};
static DN2CPP_GC_STATIC_ROOT Dn2CppNamedCultureCacheNode* g_namedCultureCache = nullptr;

static const Dn2CppNumberFormatInfo* dn2cpp_culture_named_lookup(Dn2CppString* key)
{
    std::lock_guard<std::mutex> lock(g_cultureCacheMutex);
    for (Dn2CppNamedCultureCacheNode* p = g_namedCultureCache; p != nullptr; p = p->next)
        if (dn2cpp_string_equals(p->key, key))
            return p->nfi;
    return nullptr;
}

static const Dn2CppNumberFormatInfo* dn2cpp_culture_named_create(
    const Dn2CppNumberFormatInfo* data)
{
    auto* result = static_cast<Dn2CppNumberFormatInfo*>(dn2cpp_alloc(sizeof(Dn2CppNumberFormatInfo)));
    *result = *data;
    result->numberFormat = nullptr;
    result->isReadOnly = 1;
    Dn2CppString* key = dn2cpp_culture_lower_ascii(data->cultureRealName);
    std::lock_guard<std::mutex> lock(g_cultureCacheMutex);
    for (Dn2CppNamedCultureCacheNode* p = g_namedCultureCache; p != nullptr; p = p->next)
        if (dn2cpp_string_equals(p->key, key))
            return result;
    auto* node = static_cast<Dn2CppNamedCultureCacheNode*>(dn2cpp_alloc(sizeof(Dn2CppNamedCultureCacheNode)));
    dn2cpp_gc_store_ref(&node->next, g_namedCultureCache);
    g_namedCultureCache = node;
    dn2cpp_gc_store_ref(&node->key, key);
    dn2cpp_gc_store_ref(&node->nfi, static_cast<const Dn2CppNumberFormatInfo*>(result));
    return result;
}

static Dn2CppCultureNumericGroups dn2cpp_culture_windows_groups(const char16_t* value)
{
    Dn2CppCultureNumericGroups result{{3}, 1};
    size_t length = std::char_traits<char16_t>::length(value);
    if (length == 0)
        return result;
    if (value[0] == u'0')
    {
        result.sizes[0] = 0;
        return result;
    }
    // NLS's trailing zero repeats the last group; .NET's trailing zero stops.
    size_t count = value[length - 1] == u'0' ? length / 2 : length / 2 + 2;
    for (size_t i = 0, j = 0; i < length && j < count; i += 2, j++)
        if (value[i] < u'1' || value[i] > u'9')
            return result;
    if (count > DN2CPP_MAX_GROUP_SIZES)
        dn2cpp_throw_platform_not_supported("Windows numeric grouping exceeds the modeled group-size capacity");
    result = {};
    result.count = static_cast<int8_t>(count);
    for (size_t i = 0, j = 0; i < length && j < count; i += 2, j++)
        result.sizes[j] = static_cast<int8_t>(value[i] - u'0');
    return result;
}

static const Dn2CppNumberFormatInfo* dn2cpp_culture_user_number_overrides(
    const Dn2CppNumberFormatInfo* data, bool useUserOverride)
{
    if (!useUserOverride)
        return data;
    Dn2CppString* name = data->cultureRealName;
    std::unique_ptr<Dn2CppPalLocaleNumberOverrides> storage(new Dn2CppPalLocaleNumberOverrides);
    Dn2CppPalLocaleNumberOverrides& values = *storage;
    if (!dn2cpp_pal_locale_number_overrides(name->chars, name->length, &values))
        return data;
    // Override and no-override requests may share an interned profile. Its
    // ICU data must remain immutable for the no-override and LCID caches.
    auto* result = dn2cpp_nfi_copy(data, 0, data->isReadOnly);
    auto copyString = [](const char16_t* value) {
        return dn2cpp_string_from_chars(value, static_cast<int32_t>(std::char_traits<char16_t>::length(value)));
    };
    dn2cpp_gc_store_ref(&result->positiveSign, copyString(values.positiveSign[0] == u'\0' ? u"+" : values.positiveSign));
    dn2cpp_gc_store_ref(&result->negativeSign, copyString(values.negativeSign));
    dn2cpp_gc_store_ref(&result->numberDecimal, copyString(values.numberDecimal));
    dn2cpp_gc_store_ref(&result->numberGroup, copyString(values.numberGroup));
    dn2cpp_gc_store_ref(&result->currencySymbol, copyString(values.currencySymbol));
    dn2cpp_gc_store_ref(&result->currencyDecimal,
        copyString(values.currencyDecimal[0] == u'\0' ? values.numberDecimal : values.currencyDecimal));
    dn2cpp_gc_store_ref(&result->currencyGroup, copyString(values.currencyGroup));
    Dn2CppCultureNumericGroups numberGroups = dn2cpp_culture_windows_groups(values.numberGrouping);
    Dn2CppCultureNumericGroups currencyGroups = dn2cpp_culture_windows_groups(values.currencyGrouping);
    std::memcpy(result->groupSizes, numberGroups.sizes, sizeof(result->groupSizes));
    result->groupSizeCount = numberGroups.count;
    std::memcpy(result->currencyGroupSizes, currencyGroups.sizes, sizeof(result->currencyGroupSizes));
    result->currencyGroupSizeCount = currencyGroups.count;
    result->numberDigits = values.numberDigits;
    result->numberNegPattern = values.numberNegPattern;
    result->currencyDigits = values.currencyDigits;
    result->currencyPosPattern = values.currencyPosPattern;
    result->currencyNegPattern = values.currencyNegPattern;
    // CoreLib derives percent separators, grouping and precision from numbers;
    // percent patterns/symbols and NaN/infinity remain ICU data.
    result->percentDigits = result->numberDigits;
    dn2cpp_gc_store_ref(&result->percentDecimal, result->numberDecimal);
    dn2cpp_gc_store_ref(&result->percentGroup, result->numberGroup);
    std::memcpy(result->percentGroupSizes, result->groupSizes, sizeof(result->percentGroupSizes));
    result->percentGroupSizeCount = result->groupSizeCount;
    return result;
}

const Dn2CppNumberFormatInfo* dn2cpp_culture_by_name(Dn2CppString* name, bool cached, bool useUserOverride)
{
    if (name == nullptr)
        return dn2cpp_nfi_invariant();
    Dn2CppString* key = dn2cpp_culture_lower_ascii(name);
    if (cached)
    {
        name = key;
        useUserOverride = false;
        if (const Dn2CppNumberFormatInfo* existing = dn2cpp_culture_named_lookup(key))
            return existing;
    }
    if (name->length == 0
        || (name->length == 1 && (name->chars[0] == u'C' || name->chars[0] == u'c')))
        return cached ? dn2cpp_culture_named_create(dn2cpp_nfi_invariant()) : dn2cpp_nfi_invariant();
    if (const Dn2CppNumberFormatInfo* existing = dn2cpp_culture_request_cache(key, useUserOverride))
        return cached ? dn2cpp_culture_named_create(existing) : existing;
    bool customUndefined = false;
    bool hasKeywords = false;
    Dn2CppString* canonical = dn2cpp_culture_normalize_name(name, &customUndefined, &hasKeywords);
    if (canonical == nullptr)
        dn2cpp_throw_culture_not_found(name, 0);
    Dn2CppString request = *key;
    for (int32_t i = 0; i < request.length; i++)
        if (request.chars[i] == u'\0')
        {
            request.length = i;
            break;
        }
    // An empty custom identity is distinct from the invariant culture's LCID.
    Dn2CppString privateAlias = request;
    if (privateAlias.length > 10 && privateAlias.chars[10] == u'-')
        privateAlias.length = 10;
    if (canonical->length == 0 && !customUndefined
        && !dn2cpp_culture_name_eq_ci(&privateAlias, u"i-enochian"))
        return cached ? dn2cpp_culture_named_create(dn2cpp_nfi_invariant()) : dn2cpp_nfi_invariant();
    Dn2CppString* publicName = dn2cpp_culture_public_name(name, canonical);
    int32_t lcid = 4096;
    bool sorted = false;
    for (int32_t i = 0; i < canonical->length; i++)
        sorted = sorted || canonical->chars[i] == u'_';
    if (sorted)
    {
        for (const Dn2CppCultureSortIdentifier& sort : kCultureSortIdentifiers)
            if (dn2cpp_culture_name_eq_ci(canonical, sort.name)
                && publicName->length < canonical->length
                && canonical->chars[publicName->length] == u'_')
            {
                lcid = sort.lcid;
                break;
            }
    }
    else if (const Dn2CppCultureRow* metadata = dn2cpp_culture_find_metadata_row(publicName))
        lcid = metadata->lcid;
    const Dn2CppCultureRow* row = dn2cpp_culture_find_format_row(canonical);
    const Dn2CppCultureFallbackEntry* fallback = row == nullptr
        ? dn2cpp_culture_fallback_entry(canonical, &request) : nullptr;
    const Dn2CppCultureNumericData* numeric = fallback != nullptr
        ? dn2cpp_culture_fallback_numeric(fallback, key) : dn2cpp_culture_numeric_data(row, key);
    const Dn2CppNumberFormatInfo* result = dn2cpp_culture_intern(
        row, canonical, publicName, lcid, numeric, fallback);
    // ICU keywords distinguish the OS resource name even when the managed
    // culture's displayed identity drops them.
    result = dn2cpp_culture_user_number_overrides(result, useUserOverride && !hasKeywords);
    result = dn2cpp_culture_request_cache(key, useUserOverride, result);
    return cached ? dn2cpp_culture_named_create(result) : result;
}

const Dn2CppNumberFormatInfo* dn2cpp_culture_new_name(Dn2CppString* name, bool useUserOverride)
{
    if (name == nullptr)
        dn2cpp_throw_argument_null_param("name");
    return dn2cpp_nfi_copy(dn2cpp_culture_by_name(name, false, useUserOverride), 0, 0);
}

const Dn2CppNumberFormatInfo* dn2cpp_culture_new_lcid(int32_t lcid, bool useUserOverride)
{
    if (lcid < 0)
        dn2cpp_throw_argument_out_of_range_value(DN2CPP_SR_MUST_BE_NON_NEGATIVE, "culture", lcid);
    const Dn2CppNumberFormatInfo* data = dn2cpp_culture_by_lcid(lcid);
    if (data == nullptr)
        dn2cpp_throw_culture_not_found(nullptr, lcid);
    return dn2cpp_nfi_copy(dn2cpp_culture_user_number_overrides(data, useUserOverride), 0, 0);
}

const Dn2CppNumberFormatInfo* dn2cpp_culture_create_specific(Dn2CppString* name)
{
    if (name != nullptr && name->length > 0
        && !(name->length == 1 && (name->chars[0] == u'C' || name->chars[0] == u'c'))
        && dn2cpp_culture_request_cache(dn2cpp_culture_lower_ascii(name), true) == nullptr
        && dn2cpp_culture_normalize_name(name) == nullptr)
    {
        // Malformed country/variant input retries through its language prefix.
        for (int32_t i = 0; i < name->length; i++)
            if (name->chars[i] == u'-')
                return dn2cpp_culture_create_specific(dn2cpp_string_from_chars(name->chars, i));
    }
    const Dn2CppNumberFormatInfo* culture = dn2cpp_culture_new_name(name, true);
    if (culture->isNeutralCulture == 0)
        return culture;
    for (const Dn2CppCultureNameMapping& specific : kCultureSpecificNames)
        if (dn2cpp_culture_name_eq_ci(culture->cultureName, specific.name))
            return dn2cpp_culture_new_name(dn2cpp_lit16(specific.target), true);
    return dn2cpp_culture_new_name(dn2cpp_lit16(u""), true);
}

struct Dn2CppLcidCultureCacheNode
{
    int32_t lcid;
    const Dn2CppNumberFormatInfo* nfi;
    Dn2CppLcidCultureCacheNode* next;
};
static DN2CPP_GC_STATIC_ROOT Dn2CppLcidCultureCacheNode* g_lcidCultureCache = nullptr;

const Dn2CppNumberFormatInfo* dn2cpp_culture_by_lcid(int32_t lcid)
{
    // LOCALE_CUSTOM_UNSPECIFIED names the absence of an LCID, not a row.
    if (lcid == 4096)
        return nullptr;
    {
        std::lock_guard<std::mutex> lock(g_cultureCacheMutex);
        for (Dn2CppLcidCultureCacheNode* p = g_lcidCultureCache; p != nullptr; p = p->next)
            if (p->lcid == lcid)
                return p->nfi;
    }
    const Dn2CppNumberFormatInfo* data = lcid == 127 ? dn2cpp_nfi_invariant() : nullptr;
    for (const Dn2CppCultureSortIdentifier& sort : kCultureSortIdentifiers)
        if (sort.lcid == lcid)
            data = dn2cpp_culture_by_name(dn2cpp_lit16(sort.name), false, false);
    for (const Dn2CppCultureRow& row : kCultureIdentifierFormats)
        if (row.lcid == lcid)
            data = dn2cpp_culture_intern(&row, nullptr);
    for (size_t i = 0; i < kCultureCount; i++)
        if (kCultures[i].lcid == lcid)
            data = dn2cpp_culture_intern(&kCultures[i], nullptr);
#if !defined(_WIN32)
    // ICU's reverse LCID map retains legacy Chinese names outside Windows.
    if (lcid == 4 || lcid == 31748)
        data = dn2cpp_culture_by_name(dn2cpp_lit16(lcid == 4 ? u"zh-CHS" : u"zh-CHT"), false, false);
#endif
    if (data == nullptr)
        return nullptr;
    auto* result = static_cast<Dn2CppNumberFormatInfo*>(dn2cpp_alloc(sizeof(Dn2CppNumberFormatInfo)));
    *result = *data;
    result->numberFormat = nullptr;
    result->isReadOnly = 1;
    std::lock_guard<std::mutex> lock(g_cultureCacheMutex);
    for (Dn2CppLcidCultureCacheNode* p = g_lcidCultureCache; p != nullptr; p = p->next)
        if (p->lcid == lcid)
            return p->nfi;
    auto* node = static_cast<Dn2CppLcidCultureCacheNode*>(dn2cpp_alloc(sizeof(Dn2CppLcidCultureCacheNode)));
    dn2cpp_gc_store_ref(&node->next, g_lcidCultureCache);
    g_lcidCultureCache = node;
    dn2cpp_gc_store_ref(&node->nfi, static_cast<const Dn2CppNumberFormatInfo*>(result));
    node->lcid = lcid;
    return result;
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
// real symbols and undefined languages use frozen host fallback profiles.
// Known resource languages outside the modeled inventory retain invariant
// symbols. No ICU is consulted; the PAL answers locale names only.
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
        Dn2CppString* requested = n > 0 ? dn2cpp_string_from_utf8(name, n) : nullptr;
        Dn2CppString* canonical = requested != nullptr ? dn2cpp_culture_normalize_name(requested) : nullptr;
        g_hostCulture = canonical != nullptr && canonical->length > 0
            ? dn2cpp_culture_by_name(requested) : dn2cpp_nfi_invariant();
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
// On POSIX real .NET resolves it from the same host locale the other two
// default to, so it is the host default itself rather than either slot.
const Dn2CppNumberFormatInfo* dn2cpp_culture_installed_ui()
{
    return dn2cpp_culture_host_default();
}

// CultureInfo.Name. Defensive: a zeroed mutable NFI (or any culture
// built before the name was modeled) reads as the invariant "".
Dn2CppString* dn2cpp_culture_name(const Dn2CppNumberFormatInfo* c)
{
    if (c == nullptr || c->cultureName == nullptr)
        return dn2cpp_lit16(u"");
    return c->cultureName;
}

Dn2CppString* dn2cpp_culture_two_letter_iso_language_name(const Dn2CppNumberFormatInfo* c)
{
    if (c == nullptr)
        dn2cpp_throw_null_reference();
    auto* name = dn2cpp_culture_name(c);
    if (name->length == 0)
        return dn2cpp_culture_lcid(c) == 127 ? dn2cpp_lit16(u"iv") : name;
    // The alternate sort, absent from Name, keeps ICU from parsing a language tag.
    if (c->cultureRealName != nullptr)
        name = c->cultureRealName;
    bool sorted = false;
    for (int32_t i = 0; i < name->length; i++)
        sorted = sorted || name->chars[i] == u'_';
    // Legacy i- and private-use x- prefixes belong to the language identifier.
    bool privateUse = name->length > 2 && name->chars[0] == u'x' && name->chars[1] == u'-';
    int32_t length = privateUse || (name->length > 2 && name->chars[0] == u'i' && name->chars[1] == u'-')
        ? 2 : 0;
    while (length < name->length && name->chars[length] != u'-' && name->chars[length] != u'_')
        length++;
    // A well-formed private-use tag has no language; an overlong first subtag
    // is not a tag, so the identifier survives.
    if (privateUse && !sorted && length <= 10)
        return dn2cpp_lit16(u"");
    auto language = dn2cpp_culture_lower(std::u16string(name->chars, static_cast<size_t>(length)));
    // Standard ICU reports no language for und.
    if (language == u"und")
        return dn2cpp_lit16(u"");
    // ISO 639-2 terminology codes map to two-letter codes where available;
    // legacy two-letter spellings and languages without such a code survive.
    if (length == 3)
        for (const auto& code : kCultureIsoLanguageCodes)
            if (std::char_traits<char16_t>::compare(language.data(), code.name, 3) == 0)
                return dn2cpp_string_from_chars(code.target, 2);
    return dn2cpp_string_from_chars(language.data(), length);
}

Dn2CppString* dn2cpp_culture_tostring(const Dn2CppNumberFormatInfo* c)
{
    if (c == nullptr || c->cultureRealName == nullptr)
        return dn2cpp_culture_name(c);
    return c->cultureRealName;
}

Dn2CppString* dn2cpp_textinfo_tostring(const Dn2CppNumberFormatInfo* c)
{
    if (c == nullptr)
        dn2cpp_throw_null_reference();
    return dn2cpp_string_concat2(
        dn2cpp_string_from_utf8("TextInfo - ", 11), dn2cpp_culture_tostring(c));
}

// `new NumberFormatInfo()` — a mutable GC-heap copy of the invariant; the object
// initializer / property setters then customize. Tagged isNfi so an escape to
// `object` through an erased IFormatProvider slot recovers the NumberFormatInfo
// identity (see dn2cpp_nfi_wrap).
Dn2CppNumberFormatInfo* dn2cpp_nfi_new()
{
    return dn2cpp_nfi_copy(dn2cpp_nfi_invariant(), 1, 0);
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

Dn2CppObject* dn2cpp_nfi_clone(const Dn2CppNumberFormatInfo* n, int32_t kind)
{
    if (n == nullptr)
        dn2cpp_throw_null_reference();
    auto* clone = dn2cpp_nfi_copy(n, kind == DN2CPP_NFI_KIND_NFI ? 1 : n->isNfi, 0);
    if (kind == DN2CPP_NFI_KIND_CULTURE && n->numberFormat != nullptr)
    {
        const Dn2CppNumberFormatInfo* number = dn2cpp_nfi_copy(n->numberFormat, 1, 0);
        dn2cpp_gc_store_ref(&clone->numberFormat, number);
    }
    return dn2cpp_nfi_wrap(clone, kind);
}

// ---- CultureInfo/NumberFormatInfo/TextInfo escaped into `object` ------------
// The managed types lower to the headerless `const Dn2CppNumberFormatInfo*`,
// so an escape into an `object` context allocates a real managed object around
// the pointer (Dn2CppNfiBox, dn2cpp_core.h). ToString on the CultureInfo form
// retains the resolved name and alternate sort, matching CultureInfo.ToString.
// NumberFormatInfo keeps the default type-name display; TextInfo's slot
// preserves its culture-bearing "TextInfo - {culture}" display after wrapping.
static Dn2CppString* dn2cpp_cultureinfo_box_tostring(Dn2CppObject* o)
{
    return dn2cpp_culture_tostring(reinterpret_cast<Dn2CppNfiBox*>(o)->nfi);
}

static Dn2CppString* dn2cpp_textinfo_box_tostring(Dn2CppObject* o)
{
    return dn2cpp_textinfo_tostring(reinterpret_cast<Dn2CppNfiBox*>(o)->nfi);
}

const Dn2CppTypeInfo dn2cpp_cultureinfo_type =
    { "System.Globalization.CultureInfo", &dn2cpp_object_type, nullptr, nullptr, dn2cpp_cultureinfo_box_tostring, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, (int32_t)sizeof(Dn2CppNfiBox), 0, 0, 0, 0, 0, nullptr };
const Dn2CppTypeInfo dn2cpp_numberformatinfo_type =
    { "System.Globalization.NumberFormatInfo", &dn2cpp_object_type, nullptr, nullptr, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, (int32_t)sizeof(Dn2CppNfiBox), 0, 0, 0, 0, 0, nullptr };
const Dn2CppTypeInfo dn2cpp_textinfo_type =
    { "System.Globalization.TextInfo", &dn2cpp_object_type, nullptr, nullptr, dn2cpp_textinfo_box_tostring, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, (int32_t)sizeof(Dn2CppNfiBox), 0, 0, 0, 0, 0, nullptr };

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

// Fixed interface thunks share the image's demanded GetFormat slot. Generated
// initialization publishes it before managed code can enter those thunks.
static const Dn2CppTypeInfo* dn2cpp_format_provider_interface = nullptr;
static int32_t dn2cpp_format_provider_slot = 0;

void dn2cpp_set_format_provider_dispatch(const Dn2CppTypeInfo* interfaceType, int32_t slot)
{
    dn2cpp_format_provider_interface = interfaceType;
    dn2cpp_format_provider_slot = slot;
}

Dn2CppObject* dn2cpp_format_provider_get_format(Dn2CppObject* provider, Dn2CppType* requested,
    const Dn2CppTypeInfo* interfaceType, int32_t slot)
{
    if (provider == nullptr)
        dn2cpp_throw_null_reference();
    const Dn2CppTypeInfo* wanted = requested != nullptr ? requested->typeInfo : nullptr;
    const Dn2CppTypeInfo* held = dn2cpp_nfi_box_ti(provider);
    if (held != nullptr)
    {
        const auto* payload = reinterpret_cast<Dn2CppNfiBox*>(provider)->nfi;
        if (wanted == &dn2cpp_numberformatinfo_type
            && (held == &dn2cpp_cultureinfo_type || held == &dn2cpp_numberformatinfo_type))
            return dn2cpp_nfi_wrap(dn2cpp_culture_number_format(payload), DN2CPP_NFI_KIND_NFI);
        if (held == &dn2cpp_cultureinfo_type && wanted != nullptr && wanted->name != nullptr
            && std::strcmp(wanted->name, "System.Globalization.DateTimeFormatInfo") == 0)
        {
            auto* result = static_cast<Dn2CppObject*>(dn2cpp_alloc(
                static_cast<size_t>(wanted->instanceSize > static_cast<int32_t>(sizeof(Dn2CppObject))
                    ? wanted->instanceSize : sizeof(Dn2CppObject))));
            result->type = wanted;
            return result;
        }
        return nullptr;
    }
    if (provider->type != nullptr && provider->type->name != nullptr
        && std::strcmp(provider->type->name, "System.Globalization.DateTimeFormatInfo") == 0)
        return wanted == provider->type ? provider : nullptr;
    if (interfaceType == nullptr)
        dn2cpp_throw_invalid_operation_msg("A numeric provider has no demanded GetFormat slot");
    const void** methods = dn2cpp_resolve_interface(provider->type, interfaceType);
    auto getFormat = reinterpret_cast<Dn2CppObject* (*)(Dn2CppObject*, Dn2CppType*)>(
        const_cast<void*>(methods[slot]));
    return getFormat(provider, requested);
}

const Dn2CppNumberFormatInfo* dn2cpp_format_provider_nfi(Dn2CppObject* provider,
    const Dn2CppTypeInfo* interfaceType, int32_t slot)
{
    if (provider == nullptr)
        return nullptr;
    const Dn2CppTypeInfo* held = dn2cpp_nfi_box_ti(provider);
    if (held == &dn2cpp_cultureinfo_type || held == &dn2cpp_numberformatinfo_type)
        return reinterpret_cast<Dn2CppNfiBox*>(provider)->nfi;
    Dn2CppObject* result = dn2cpp_format_provider_get_format(provider,
        dn2cpp_get_type_from_handle(&dn2cpp_numberformatinfo_type), interfaceType, slot);
    return result != nullptr && dn2cpp_nfi_box_ti(result) == &dn2cpp_numberformatinfo_type
        ? reinterpret_cast<Dn2CppNfiBox*>(result)->nfi : dn2cpp_culture_current();
}

const Dn2CppNumberFormatInfo* dn2cpp_format_provider_nfi(Dn2CppObject* provider)
{
    return dn2cpp_format_provider_nfi(provider,
        dn2cpp_format_provider_interface, dn2cpp_format_provider_slot);
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

// String properties preserve CLR's validation order around the readonly guard.
static void dn2cpp_nfi_set_string(Dn2CppNumberFormatInfo* n, Dn2CppString** slot,
    Dn2CppString* value, bool separator, bool decimal)
{
    if (n == nullptr)
        dn2cpp_throw_null_reference();
    if (separator)
        dn2cpp_nfi_verify_writable(n);
    if (value == nullptr)
        dn2cpp_throw_argument_null_param("value");
    if (!separator)
        dn2cpp_nfi_verify_writable(n);
    if (decimal && value->length == 0)
        dn2cpp_throw_argument_param(DN2CPP_SR_EMPTY_STRING, "value");
    dn2cpp_gc_store_ref(slot, value);
}

void dn2cpp_nfi_set_number_decimal(Dn2CppNumberFormatInfo* n, Dn2CppString* v)
{
    dn2cpp_nfi_set_string(n, n != nullptr ? &n->numberDecimal : nullptr, v, true, true);
}
void dn2cpp_nfi_set_number_group(Dn2CppNumberFormatInfo* n, Dn2CppString* v)
{
    dn2cpp_nfi_set_string(n, n != nullptr ? &n->numberGroup : nullptr, v, true, false);
}
void dn2cpp_nfi_set_currency_decimal(Dn2CppNumberFormatInfo* n, Dn2CppString* v)
{
    dn2cpp_nfi_set_string(n, n != nullptr ? &n->currencyDecimal : nullptr, v, true, true);
}
void dn2cpp_nfi_set_currency_group(Dn2CppNumberFormatInfo* n, Dn2CppString* v)
{
    dn2cpp_nfi_set_string(n, n != nullptr ? &n->currencyGroup : nullptr, v, true, false);
}
void dn2cpp_nfi_set_negative_sign(Dn2CppNumberFormatInfo* n, Dn2CppString* v)
{
    dn2cpp_nfi_set_string(n, n != nullptr ? &n->negativeSign : nullptr, v, false, false);
}
void dn2cpp_nfi_set_positive_sign(Dn2CppNumberFormatInfo* n, Dn2CppString* v)
{
    dn2cpp_nfi_set_string(n, n != nullptr ? &n->positiveSign : nullptr, v, false, false);
}
void dn2cpp_nfi_set_nan(Dn2CppNumberFormatInfo* n, Dn2CppString* v)
{
    dn2cpp_nfi_set_string(n, n != nullptr ? &n->nan : nullptr, v, false, false);
}
void dn2cpp_nfi_set_pos_inf(Dn2CppNumberFormatInfo* n, Dn2CppString* v)
{
    dn2cpp_nfi_set_string(n, n != nullptr ? &n->posInf : nullptr, v, false, false);
}
void dn2cpp_nfi_set_neg_inf(Dn2CppNumberFormatInfo* n, Dn2CppString* v)
{
    dn2cpp_nfi_set_string(n, n != nullptr ? &n->negInf : nullptr, v, false, false);
}
void dn2cpp_nfi_set_percent_symbol(Dn2CppNumberFormatInfo* n, Dn2CppString* v)
{
    dn2cpp_nfi_set_string(n, n != nullptr ? &n->percentSymbol : nullptr, v, false, false);
}
void dn2cpp_nfi_set_currency_symbol(Dn2CppNumberFormatInfo* n, Dn2CppString* v)
{
    dn2cpp_nfi_set_string(n, n != nullptr ? &n->currencySymbol : nullptr, v, false, false);
}

void dn2cpp_nfi_set_digits(Dn2CppNumberFormatInfo* n, int32_t value, bool currency)
{
    if (n == nullptr)
        dn2cpp_throw_null_reference();
    if (value < 0 || value > 99)
        dn2cpp_throw_argument_out_of_range_param(DN2CPP_SR_ARGUMENT_OUT_OF_RANGE, "value");
    dn2cpp_nfi_verify_writable(n);
    if (currency)
        n->currencyDigits = value;
    // NumberDecimalDigits retains the accepted precision-sentinel contract.
}

// NumberGroupSizes / CurrencyGroupSizes / PercentGroupSizes — the getter. A
// fresh array per call, matching real .NET's defensive copy: two reads are not
// reference-equal, and writing through the returned array does not move the
// culture's formatting. The single place where the modeled array becomes a
// managed int[], serving the public properties and System.Number's readers.
Dn2CppArrayI4* dn2cpp_nfi_group_sizes(const Dn2CppNumberFormatInfo* n, const Dn2CppTypeInfo* ti, int32_t axis)
{
    const int8_t* sizes = n == nullptr ? nullptr : axis == 1 ? n->currencyGroupSizes
        : axis == 2 ? n->percentGroupSizes : n->groupSizes;
    int32_t count = n == nullptr ? 0 : axis == 1 ? n->currencyGroupSizeCount
        : axis == 2 ? n->percentGroupSizeCount : n->groupSizeCount;
    if (count > DN2CPP_MAX_GROUP_SIZES) count = DN2CPP_MAX_GROUP_SIZES;
    Dn2CppArrayI4* a = dn2cpp_newarr_i4_t(count, ti);
    for (int32_t i = 0; i < count; i++)
        a->data[i] = sizes[i];
    return a;
}

// Group-size writes retain the validate-and-trap contract. Each property is
// checked against its own numeric axis.
void dn2cpp_nfi_set_group_sizes(const Dn2CppNumberFormatInfo* n, Dn2CppArrayI4* v, const char* which)
{
    if (n == nullptr)
        dn2cpp_throw_null_reference();
    if (v == nullptr)
        dn2cpp_throw_argument_null_param("value");
    dn2cpp_nfi_verify_writable(n);
    int32_t axis = std::strcmp(which, "CurrencyGroupSizes") == 0 ? 1
        : std::strcmp(which, "PercentGroupSizes") == 0 ? 2 : 0;
    const int8_t* sizes = n == nullptr ? nullptr : axis == 1 ? n->currencyGroupSizes
        : axis == 2 ? n->percentGroupSizes : n->groupSizes;
    int32_t count = n == nullptr ? 0 : axis == 1 ? n->currencyGroupSizeCount
        : axis == 2 ? n->percentGroupSizeCount : n->groupSizeCount;
    bool same = v != nullptr && n != nullptr && v->length == count;
    if (same)
        for (int32_t i = 0; i < v->length; i++)
            if (v->data[i] != sizes[i]) { same = false; break; }
    if (!same)
    {
        char msg[192];
        std::snprintf(msg, sizeof(msg),
                      "NumberFormatInfo.%s: changing modeled group sizes is unsupported", which);
        dn2cpp_throw_platform_not_supported(msg);
    }
}

// Replace ASCII signs and the decimal point in a
// freshly-rendered numeric ASCII buffer with the culture's multi-char symbols,
// widening the rest to UTF-16. Grouping is applied separately upstream.
Dn2CppString* dn2cpp_localize_ascii(const char* buf, int len, const Dn2CppNumberFormatInfo* n)
{
    int64_t total = 0;
    for (int i = 0; i < len; i++)
    {
        char c = buf[i];
        Dn2CppString* rep = (c == '-') ? n->negativeSign
                          : (c == '+') ? n->positiveSign
                          : (c == '.') ? n->numberDecimal
                                       : nullptr;
        total += rep != nullptr ? rep->length : 1;
    }
    if (total > INT32_MAX)
        dn2cpp_throw_out_of_memory();
    char16_t* out;
    Dn2CppString* result = dn2cpp_string_alloc(&out, static_cast<int32_t>(total));
    int o = 0;
    for (int i = 0; i < len; i++)
    {
        char c = buf[i];
        Dn2CppString* rep = (c == '-') ? n->negativeSign
                          : (c == '+') ? n->positiveSign
                          : (c == '.') ? n->numberDecimal
                                       : nullptr;
        if (rep != nullptr)
            for (int k = 0; k < rep->length; k++) out[o++] = rep->chars[k];
        else
            out[o++] = static_cast<char16_t>(static_cast<unsigned char>(c));
    }
    return result;
}

static Dn2CppString* dn2cpp_float_format(double v, int maxSig, int sciThreshold, bool roundTripFloat,
                                         bool lowerExponent, const Dn2CppNumberFormatInfo* nfi)
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
        out[pos++] = lowerExponent ? 'e' : 'E';
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
    return dn2cpp_float_format(v, 17, 17, false, false, nullptr);
}

Dn2CppString* dn2cpp_float_to_string(float v)
{
    return dn2cpp_float_format(static_cast<double>(v), 9, 9, true, false, nullptr);
}

Dn2CppString* dn2cpp_double_to_string_c(double v, const Dn2CppNumberFormatInfo* n)
{
    return dn2cpp_float_format(v, 17, 17, false, false, n);
}

Dn2CppString* dn2cpp_float_to_string_c(float v, const Dn2CppNumberFormatInfo* n)
{
    return dn2cpp_float_format(static_cast<double>(v), 9, 9, true, false, n);
}

Dn2CppString* dn2cpp_double_to_string_lower_c(double v, const Dn2CppNumberFormatInfo* n)
{
    return dn2cpp_float_format(v, 17, 17, false, true, n);
}

Dn2CppString* dn2cpp_float_to_string_lower_c(float v, const Dn2CppNumberFormatInfo* n)
{
    return dn2cpp_float_format(static_cast<double>(v), 9, 9, true, true, n);
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

static int32_t dn2cpp_compare_info_length(Dn2CppString* s, int32_t index, int32_t length)
{
    if (s == nullptr)
        return length;
    int32_t room = static_cast<int32_t>(
        static_cast<uint32_t>(s->length) - static_cast<uint32_t>(index));
    return room < length ? room : length;
}

// Whether CompareInfo.Compare accepts one side's window: a null string only at offset 0
// and length 0, a string where offset and count are non-negative and end within it.
static bool dn2cpp_compare_info_window_ok(Dn2CppString* s, int32_t offset, int32_t count)
{
    if (s == nullptr)
        return offset == 0 && count == 0;
    return offset >= 0 && count >= 0
        && static_cast<int64_t>(offset) + count <= s->length;
}

// CompareInfo.Compare(string1, offset1, length1, string2, offset2, length2)'s bounds
// check, which runs before it orders a null. A rejected window names the first negative
// of length1, length2, offset1 and offset2, else string1 if its window is rejected, else
// string2.
static void dn2cpp_compare_info_check_windows(Dn2CppString* a, int32_t offset1,
    Dn2CppString* b, int32_t offset2, int32_t length)
{
    int32_t length1 = dn2cpp_compare_info_length(a, offset1, length);
    int32_t length2 = dn2cpp_compare_info_length(b, offset2, length);
    if (dn2cpp_compare_info_window_ok(a, offset1, length1)
        && dn2cpp_compare_info_window_ok(b, offset2, length2))
        return;
    if (length1 < 0)
        dn2cpp_throw_argument_out_of_range_value(DN2CPP_SR_MUST_BE_NON_NEGATIVE, "length1", length1);
    if (length2 < 0)
        dn2cpp_throw_argument_out_of_range_value(DN2CPP_SR_MUST_BE_NON_NEGATIVE, "length2", length2);
    if (offset1 < 0)
        dn2cpp_throw_argument_out_of_range_value(DN2CPP_SR_MUST_BE_NON_NEGATIVE, "offset1", offset1);
    if (offset2 < 0)
        dn2cpp_throw_argument_out_of_range_value(DN2CPP_SR_MUST_BE_NON_NEGATIVE, "offset2", offset2);
    int32_t lengthOfA = a != nullptr ? a->length : 0;
    dn2cpp_throw_argument_out_of_range_param(DN2CPP_SR_OFFSET_LENGTH,
        offset1 > lengthOfA - length1 ? "string1" : "string2");
}

// The (strA, indexA, strB, indexB, length, comparisonType) substring form —
// each side contributes at most `length` code units starting at its index
// (fewer when its string ends first), then compares like dn2cpp_str_compare:
// signed gap of the first differing unit, else the contributed-length gap.
// `viaCompareInfo` selects the validation of the culture overload, which hands
// CompareInfo.Compare each side's clamped length and so names that callee's
// length1/length2/offset1/offset2/string1/string2 instead of length/indexA/indexB.
int32_t dn2cpp_str_compare_sub(Dn2CppString* a, int32_t indexA, Dn2CppString* b,
                               int32_t indexB, int32_t length, int32_t comparisonType,
                               int32_t viaCompareInfo)
{
    comparisonType = dn2cpp_str_comparison_fold(comparisonType);
    // CompareInfo.Compare checks both windows before it orders a null; the ordinal
    // overloads order a null first and check the arguments of two present operands.
    if (viaCompareInfo != 0)
        dn2cpp_compare_info_check_windows(a, indexA, b, indexB, length);
    if (a == nullptr)
        return b == nullptr ? 0 : -1;
    if (b == nullptr)
        return 1;
    // Ordinal: a negative length, a negative index, or an index past the end each
    // raise a catchable ArgumentOutOfRangeException; an index equal to Length, and
    // an index whose span overruns the end, are valid and clamp below.
    if (viaCompareInfo == 0)
    {
        if (length < 0)
            dn2cpp_throw_argument_out_of_range_value(DN2CPP_SR_MUST_BE_NON_NEGATIVE, "length", length);
        if (indexA < 0 || indexB < 0)
            dn2cpp_throw_argument_out_of_range_param(DN2CPP_SR_INDEX_MUST_BE_LESS_OR_EQUAL,
                indexA < 0 ? "indexA" : "indexB");
        if (indexA > a->length || indexB > b->length)
            dn2cpp_throw_argument_out_of_range_param(DN2CPP_SR_INDEX_MUST_BE_LESS_OR_EQUAL,
                indexA > a->length ? "indexA" : "indexB");
    }
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

int32_t dn2cpp_str_compare_sub_options(Dn2CppString* a, int32_t indexA, Dn2CppString* b,
                                       int32_t indexB, int32_t length, int32_t options)
{
    dn2cpp_compare_info_check_windows(a, indexA, b, indexB, length);
    constexpr uint32_t validLinguistic = 0x2000003fu;
    constexpr uint32_t ordinalIgnoreCase = 0x10000000u;
    constexpr uint32_t ordinal = 0x40000000u;
    uint32_t flags = static_cast<uint32_t>(options);
    if ((flags & ~validLinguistic) != 0 && flags != ordinalIgnoreCase && flags != ordinal)
        dn2cpp_throw_argument_text(&dn2cpp_argument_exception_type,
            dn2cpp_sr_text((flags & ordinal) != 0
                ? DN2CPP_SR_COMPARE_OPTION_ORDINAL : DN2CPP_SR_INVALID_FLAG), "options");
    int32_t comparisonType = ((flags & 1u) != 0 || flags == ordinalIgnoreCase) ? 5 : 4;
    return dn2cpp_str_compare_sub(a, indexA, b, indexB, length, comparisonType, 1);
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

int32_t dn2cpp_char_get_string_char(Dn2CppString* s, int32_t index, const char* indexKey)
{
    if (s == nullptr)
        dn2cpp_throw_argument_null_param("s");
    if (index < 0 || index >= s->length)
        dn2cpp_throw_argument_out_of_range_param(indexKey, "index");
    return static_cast<int32_t>(static_cast<uint16_t>(s->chars[index]));
}

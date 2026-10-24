#pragma once

#include <cstddef>
#include <cstring>
#include <cstdint>

struct Dn2CppLocaleVariantState
{
    size_t length;
    size_t variantEnd;
    size_t baseEnd;
    size_t variantStart;
};

// Call only on the BCP47 path. ICU sorts accepted variants even when a
// duplicate or invalid tail ends parsing; a legacy locale keeps request order.
template<typename Char>
static Dn2CppLocaleVariantState dn2cpp_locale_sort_variants(Char* name, size_t length)
{
    auto lower = [](Char c) { return c >= 'A' && c <= 'Z' ? c - 'A' + 'a' : c; };
    auto endOf = [&](size_t start) {
        while (start < length && name[start] != '-') start++;
        return start;
    };
    auto alpha = [&](size_t start, size_t end) {
        for (size_t i = start; i < end; i++)
            if (lower(name[i]) < 'a' || lower(name[i]) > 'z') return false;
        return true;
    };
    auto digits = [&](size_t start, size_t end) {
        for (size_t i = start; i < end; i++)
            if (name[i] < '0' || name[i] > '9') return false;
        return true;
    };
    auto alnum = [&](size_t start, size_t end) {
        for (size_t i = start; i < end; i++)
            if (!((lower(name[i]) >= 'a' && lower(name[i]) <= 'z')
                || (name[i] >= '0' && name[i] <= '9'))) return false;
        return true;
    };
    Dn2CppLocaleVariantState state{length, 0, 0, 0};
    size_t languageEnd = endOf(0);
    if (languageEnd < 2 || languageEnd > 8 || !alpha(0, languageEnd))
        return state;
    size_t start = languageEnd + 1;
    for (int count = 0; languageEnd <= 3 && count < 3 && start < length; count++)
    {
        size_t end = endOf(start);
        if (end - start != 3 || !alpha(start, end)) break;
        start = end + 1;
    }
    if (start < length)
    {
        size_t end = endOf(start);
        if (end - start == 4 && alpha(start, end)) start = end + 1;
    }
    if (start < length)
    {
        size_t end = endOf(start);
        if ((end - start == 2 && alpha(start, end))
            || (end - start == 3 && digits(start, end))) start = end + 1;
    }
    state.baseEnd = start - 1;
    state.variantStart = start;
    size_t first = start;
    while (start < length)
    {
        size_t end = endOf(start);
        size_t count = end - start;
        if (count == 1 && alnum(start, end))
        {
            size_t value = end + 1;
            size_t valueEnd = endOf(value);
            size_t minimum = lower(name[start]) == 'x' ? 1 : 2;
            if (value >= length || valueEnd - value < minimum || valueEnd - value > 8
                || !alnum(value, valueEnd)) state.length = state.variantEnd;
            break;
        }
        if (count > 8 || !alnum(start, end)
            || !(count >= 5 || (count == 4 && name[start] >= '0' && name[start] <= '9')))
        {
            state.length = state.variantEnd;
            break;
        }
        bool duplicate = false;
        for (size_t prior = first; prior < start;)
        {
            size_t priorEnd = endOf(prior);
            bool same = priorEnd - prior == count;
            for (size_t i = 0; same && i < count; i++)
                same = lower(name[prior + i]) == lower(name[start + i]);
            duplicate = duplicate || same;
            prior = priorEnd + 1;
        }
        if (duplicate)
        {
            state.length = state.variantEnd;
            break;
        }
        state.variantEnd = end;
        start = end + 1;
    }
    if (state.variantEnd == 0)
    {
        state.length = length;
        return state;
    }
    state.baseEnd = state.variantEnd;
    for (size_t current = endOf(first) + 1; current < state.variantEnd;)
    {
        size_t end = endOf(current);
        size_t count = end - current;
        size_t insert = first;
        while (insert < current)
        {
            size_t priorEnd = endOf(insert);
            size_t priorCount = priorEnd - insert;
            size_t common = count < priorCount ? count : priorCount;
            size_t i = 0;
            while (i < common && lower(name[current + i]) == lower(name[insert + i])) i++;
            if ((i < common && lower(name[current + i]) < lower(name[insert + i]))
                || (i == common && count < priorCount)) break;
            insert = priorEnd + 1;
        }
        if (insert < current)
        {
            Char saved[8];
            for (size_t i = 0; i < count; i++) saved[i] = name[current + i];
            std::memmove(name + insert + count + 1, name + insert, (current - insert - 1) * sizeof(Char));
            for (size_t i = 0; i < count; i++) name[insert + i] = saved[i];
            name[insert + count] = '-';
        }
        current = end + 1;
    }
    return state;
}

// Private-use legacy variants share ICU's sorted, duplicate-free variant list,
// including values shorter than a BCP47 variant. A duplicate ends parsing.
template<typename Char>
static size_t dn2cpp_locale_merge_legacy_variants(Char* name, size_t length, size_t first)
{
    auto lower = [](Char c) { return c >= 'A' && c <= 'Z' ? c - 'A' + 'a' : c; };
    auto endOf = [&](size_t start) {
        while (start < length && name[start] != '-') start++;
        return start;
    };
    for (size_t current = first; current < length;)
    {
        size_t end = endOf(current);
        size_t count = end - current;
        size_t insert = first;
        bool duplicate = false;
        while (insert < current)
        {
            size_t priorEnd = endOf(insert);
            size_t priorCount = priorEnd - insert;
            size_t common = count < priorCount ? count : priorCount;
            size_t i = 0;
            while (i < common && lower(name[current + i]) == lower(name[insert + i])) i++;
            duplicate = i == common && count == priorCount;
            if (duplicate || (i < common && lower(name[current + i]) < lower(name[insert + i]))
                || (i == common && count < priorCount)) break;
            insert = priorEnd + 1;
        }
        if (duplicate)
            return current - 1;
        if (insert < current)
        {
            Char saved[8];
            for (size_t i = 0; i < count; i++) saved[i] = name[current + i];
            std::memmove(name + insert + count + 1, name + insert, (current - insert - 1) * sizeof(Char));
            for (size_t i = 0; i < count; i++) name[insert + i] = saved[i];
            name[insert + count] = '-';
        }
        current = end + 1;
    }
    return length;
}

template<typename Char>
struct Dn2CppLocaleExtensionState
{
    size_t baseEnd;
    bool keywords;
    bool collation;
    Char sort[8];
    size_t sortLength;
    bool legacyVariants;
    bool nonPrivateKeywords;
};

// ICU keywords and the original managed u/t suffix are separate: any accepted
// extension can supply a keyword, even when a later u/t extension is invalid.
template<typename Char>
static Dn2CppLocaleExtensionState<Char> dn2cpp_locale_identity_extensions(
    Char* name, size_t length, const Dn2CppLocaleVariantState& prefix,
    bool caseSensitiveLegacyVariant = false)
{
    Dn2CppLocaleExtensionState<Char> state{prefix.baseEnd != 0 ? prefix.baseEnd : length,
        false, false, {}, 0, false, false};
    if (prefix.baseEnd == 0)
        return state;
    auto lower = [](Char c) { return c >= 'A' && c <= 'Z' ? c - 'A' + 'a' : c; };
    auto alnum = [&](Char c) {
        c = static_cast<Char>(lower(c));
        return (c >= 'a' && c <= 'z') || (c >= '0' && c <= '9');
    };
    auto endOf = [&](size_t start) {
        while (start < length && name[start] != '-') start++;
        return start;
    };
    auto key = [&](size_t start, size_t end) {
        return end - start == 2 && lower(name[start + 1]) >= 'a'
            && lower(name[start + 1]) <= 'z';
    };
    uint64_t singletons = 0;
    size_t start = prefix.baseEnd + 1;
    while (start < length)
    {
        size_t end = endOf(start);
        if (end - start != 1 || !alnum(name[start]))
            break;
        Char marker = static_cast<Char>(lower(name[start]));
        unsigned bit = marker >= 'a' ? static_cast<unsigned>(marker - 'a' + 10)
            : static_cast<unsigned>(marker - '0');
        uint64_t mask = uint64_t{1} << bit;
        if ((singletons & mask) != 0)
            break;
        singletons |= mask;
        size_t first = end + 1;
        size_t value = first;
        size_t valueEnd = first;
        size_t minimum = marker == 'x' ? 1 : 2;
        while (value < length)
        {
            end = endOf(value);
            size_t count = end - value;
            if (count < minimum || count > 8)
                break;
            bool valid = true;
            for (size_t i = value; valid && i < end; i++) valid = alnum(name[i]);
            if (!valid)
                break;
            valueEnd = end;
            value = end + 1;
        }
        if (valueEnd == first)
            break;
        bool precedingKeyword = state.keywords;
        state.keywords = true;
        if (marker != 'x')
            state.nonPrivateKeywords = true;
        if (marker == 'u' && !state.collation)
        {
            for (size_t item = first; item < valueEnd;)
            {
                size_t itemEnd = endOf(item);
                if (!key(item, itemEnd))
                {
                    item = itemEnd + 1;
                    continue;
                }
                bool collation = lower(name[item]) == 'c' && lower(name[item + 1]) == 'o';
                size_t type = itemEnd + 1;
                size_t typeEnd = type;
                while (typeEnd < valueEnd)
                {
                    size_t next = endOf(typeEnd);
                    if (key(typeEnd, next)) break;
                    typeEnd = next + 1;
                }
                size_t count = typeEnd > type ? typeEnd - type - 1 : 0;
                if (collation)
                {
                    state.collation = true;
                    const char* mapped = count == 0 ? "yes" : nullptr;
                    const char* phone = "phonebk";
                    bool phonebook = count == 7;
                    for (size_t i = 0; phonebook && i < count; i++)
                        phonebook = lower(name[type + i]) == phone[i];
                    if (phonebook) mapped = "phonebook";
                    state.sortLength = mapped != nullptr ? std::strlen(mapped) : count;
                    if (state.sortLength > 8) state.sortLength = 8;
                    for (size_t i = 0; i < state.sortLength; i++)
                        state.sort[i] = mapped != nullptr ? static_cast<Char>(mapped[i])
                            : static_cast<Char>(lower(name[type + i]));
                    break;
                }
                item = typeEnd;
            }
        }
        if (marker == 'x')
        {
            for (size_t item = first; item < valueEnd;)
            {
                size_t itemEnd = endOf(item);
                const char* legacy = "lvariant";
                bool match = itemEnd - item == 8;
                for (size_t i = 0; match && i < 8; i++)
                    match = (caseSensitiveLegacyVariant ? name[item + i] : lower(name[item + i])) == legacy[i];
                if (match && itemEnd == valueEnd)
                {
                    // A valueless lvariant consumes the private-use keyword.
                    state.keywords = precedingKeyword;
                    break;
                }
                if (match && itemEnd < valueEnd)
                {
                    size_t count = valueEnd - itemEnd;
                    std::memmove(name + state.baseEnd, name + itemEnd, count * sizeof(Char));
                    for (size_t i = state.baseEnd + 1; i < state.baseEnd + count; i++)
                        if (name[i] >= 'a' && name[i] <= 'z')
                            name[i] = static_cast<Char>(name[i] - 'a' + 'A');
                    state.baseEnd = dn2cpp_locale_merge_legacy_variants(
                        name, state.baseEnd + count, prefix.variantStart);
                    state.keywords = precedingKeyword || item > first;
                    state.legacyVariants = true;
                    break;
                }
                item = itemEnd + 1;
            }
            break;
        }
        start = valueEnd + 1;
    }
    return state;
}

struct Dn2CppLocalePosixState
{
    size_t baseEnd;
    bool extension;
    bool ordinaryResources;
};

template<typename Char>
static Dn2CppLocalePosixState dn2cpp_locale_posix_parse(const Char* name, size_t length)
{
    auto lower = [](Char c) { return c >= 'A' && c <= 'Z' ? c - 'A' + 'a' : c; };
    auto alnum = [&](Char c) {
        c = static_cast<Char>(lower(c));
        return (c >= 'a' && c <= 'z') || (c >= '0' && c <= '9');
    };
    // The caller has matched en-US-POSIX. ICU sorts accepted BCP47
    // variants before parent lookup; the legacy path preserves their order.
    bool languageTag = false;
    for (size_t i = 11; i + 2 < length; i++)
        languageTag = languageTag || (name[i] == '-' && name[i + 2] == '-');
    bool additionalVariant = false;
    bool precedingVariant = false;
    Dn2CppLocalePosixState state{11, false, false};
    auto finish = [&](bool extension) {
        state.extension = extension;
        state.ordinaryResources = languageTag
            && (precedingVariant || (!additionalVariant && extension));
        return state;
    };
    size_t start = 12;
    while (start < length)
    {
        size_t end = start;
        while (end < length && name[end] != '-')
            end++;
        size_t count = end - start;
        if (count == 0 || count > 8)
            return finish(false);
        for (size_t i = start; i < end; i++)
            if (!alnum(name[i]))
                return finish(false);
        if (count == 1)
        {
            size_t value = end + 1;
            size_t valueEnd = value;
            while (valueEnd < length && name[valueEnd] != '-')
                valueEnd++;
            size_t minimum = lower(name[start]) == 'x' ? 1 : 2;
            if (value >= length || valueEnd - value < minimum || valueEnd - value > 8)
                return finish(false);
            for (size_t i = value; i < valueEnd; i++)
                if (!alnum(name[i]))
                    return finish(false);
            // ICU retains a valid extension prefix despite an invalid tail.
            return finish(true);
        }
        if (!(count >= 5 || (count == 4 && name[start] >= '0' && name[start] <= '9')))
            return finish(false);
        for (size_t prior = 6; prior < start;)
        {
            size_t priorEnd = prior;
            while (priorEnd < length && name[priorEnd] != '-')
                priorEnd++;
            bool same = priorEnd - prior == count;
            for (size_t i = 0; same && i < count; i++)
                same = lower(name[prior + i]) == lower(name[start + i]);
            if (same)
                return finish(false);
            prior = priorEnd + 1;
        }
        additionalVariant = true;
        const char* posix = "posix";
        size_t equal = 0;
        while (equal < count && equal < 5 && lower(name[start + equal]) == posix[equal])
            equal++;
        precedingVariant = precedingVariant || (equal == count && count < 5)
            || (equal < count && equal < 5 && lower(name[start + equal]) < posix[equal]);
        state.baseEnd = end;
        start = end + 1;
    }
    return finish(false);
}

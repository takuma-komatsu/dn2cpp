#pragma once

#include <cstddef>
#include <cstring>

struct Dn2CppLocaleVariantState
{
    size_t length;
    size_t variantEnd;
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
    Dn2CppLocaleVariantState state{length, 0};
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
        return Dn2CppLocaleVariantState{length, 0};
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

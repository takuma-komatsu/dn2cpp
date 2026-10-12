#pragma once

#include <cstddef>

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

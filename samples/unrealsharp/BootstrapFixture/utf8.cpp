#include <cstring>
#include <thread>

extern "C" int unrealsharp_fixture_utf8(const char* first, const char* second)
{
    if (!first || !second)
        return !first && !second ? 1 : 0;
    const char* unicode = "\xe7\x8a\xac\xc3\xa9\xf0\x9f\x98\x80";
    if (!std::strcmp(first, "alpha") && !std::strcmp(second, unicode))
        return 2;
    if (!std::strcmp(first, unicode) && !std::strcmp(second, "alpha"))
        return 3;
    return 0;
}

extern "C" int unrealsharp_fixture_delegates(int (*first)(int), int (*second)(int), int value)
{
    int result = 0;
    auto call = [&] { result = (first ? first(value) : 0) + (second ? second(value) : 0); };
    if (value == 777)
    {
        std::thread worker(call);
        worker.join();
    }
    else call();
    return result;
}

extern "C" bool unrealsharp_fixture_bool(bool value)
{
    return !value;
}

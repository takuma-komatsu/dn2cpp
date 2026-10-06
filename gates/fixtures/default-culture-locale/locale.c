#ifndef _GNU_SOURCE
#define _GNU_SOURCE
#endif

#include <dlfcn.h>
#include <locale.h>
#include <stdlib.h>
#include <string.h>

static char* original_setlocale(int category, const char* locale)
{
#if defined(__APPLE__)
    return setlocale(category, locale);
#else
    char* (*original)(int, const char*) = dlsym(RTLD_NEXT, "setlocale");
    return original(category, locale);
#endif
}

static char* fixture_setlocale(int category, const char* locale)
{
    const char* query = getenv("DN2CPP_GATE_QUERY_LOCALE");
    if (category == LC_MESSAGES && locale == NULL && query != NULL)
        return (char*)query;
    return original_setlocale(category, locale);
}

#if defined(__APPLE__)
__attribute__((used, section("__DATA,__interpose")))
static const struct
{
    const void* replacement;
    const void* original;
} locale_interpose = { (const void*)fixture_setlocale, (const void*)setlocale };
#else
char* setlocale(int category, const char* locale)
{
    return fixture_setlocale(category, locale);
}
#endif

__attribute__((constructor))
static void initialize_locale(void)
{
    if (getenv("DN2CPP_GATE_INITIALIZE_LOCALE") != NULL)
    {
        if (original_setlocale(LC_ALL, "") == NULL)
            abort();
        // The test changes the messages locale without changing libc number parsing.
        original_setlocale(LC_NUMERIC, "C");
    }
}

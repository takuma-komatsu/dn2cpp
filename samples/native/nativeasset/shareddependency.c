#if defined(_WIN32)
#define NATIVE_ASSET_EXPORT __declspec(dllexport)
#else
#define NATIVE_ASSET_EXPORT __attribute__((visibility("default")))
#endif

#ifndef DN2CPP_SHARED_DEP_VALUE
#define DN2CPP_SHARED_DEP_VALUE 3
#endif

NATIVE_ASSET_EXPORT int dn2cpp_shared_dependency_answer(void)
{
    return DN2CPP_SHARED_DEP_VALUE;
}

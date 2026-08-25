#if defined(_WIN32)
#define NATIVE_ASSET_EXPORT __declspec(dllexport)
#else
#define NATIVE_ASSET_EXPORT __attribute__((visibility("default")))
#endif

extern int dn2cpp_shared_dependency_answer(void);

NATIVE_ASSET_EXPORT int dn2cpp_shared_asset_answer(void)
{
    return dn2cpp_shared_dependency_answer() + 4;
}

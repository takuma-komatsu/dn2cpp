#if defined(_WIN32)
#define NATIVE_ASSET_EXPORT __declspec(dllexport)
#else
#define NATIVE_ASSET_EXPORT __attribute__((visibility("default")))
#endif

NATIVE_ASSET_EXPORT int dn2cpp_native_asset_answer(void)
{
    extern int dn2cpp_native_dependency_answer(void);
    return 40 + dn2cpp_native_dependency_answer();
}

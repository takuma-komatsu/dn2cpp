#include "dn2cpp_unrealsharp_abi.h"
#include <cstdio>

// A deliberately incompatible library tests the packaged UE loader's diagnostics.
// The production ABI validation is exercised separately against its real library.
extern "C" int32_t dn2cpp_unrealsharp_initialize(const Dn2CppUnrealSharpHost* host,
    Dn2CppUnrealSharpResult* result)
{
    result->success = 0;
    std::snprintf(result->error, sizeof(result->error),
        "UnrealSharp ABI mismatch: fixture library version 2, host version %u",
        host ? host->abi_version : 0);
    return 0;
}

extern "C" int32_t dn2cpp_unrealsharp_register_assembly(const char*, Dn2CppUnrealSharpResult*)
{
    return 0;
}

extern "C" int32_t dn2cpp_unrealsharp_tick(float, Dn2CppUnrealSharpResult*)
{
    return 0;
}

extern "C" int32_t dn2cpp_unrealsharp_shutdown(Dn2CppUnrealSharpResult* result)
{
    result->success = 1;
    result->error[0] = '\0';
    return 1;
}

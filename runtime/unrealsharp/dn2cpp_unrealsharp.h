#pragma once
#include "dn2cpp.h"
#include "dn2cpp_unrealsharp_abi.h"
#include <cstdio>
#include <cstring>
#include <mutex>
#include <thread>

inline int32_t dn2cpp_us_result(Dn2CppUnrealSharpResult* result, const char* error = nullptr)
{
    if (result != nullptr) {
        result->success = error == nullptr ? 1u : 0u;
        std::snprintf(result->error, sizeof(result->error), "%s", error == nullptr ? "" : error);
    }
    return error == nullptr ? 1 : 0;
}

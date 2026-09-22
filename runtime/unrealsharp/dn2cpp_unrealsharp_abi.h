#pragma once
#include <stdint.h>

#define DN2CPP_UNREALSHARP_ABI_VERSION 1u
#define DN2CPP_UNREALSHARP_REVISION "b78e073ab4e81e6eae3c57ba1f5ecf5f29eef1f4"

/* All strings are UTF-8. The host owns callback storage until shutdown. */
typedef struct Dn2CppUnrealSharpHost {
    uint32_t abi_version;
    uint32_t struct_size;
    uint32_t ue_major;
    uint32_t ue_minor;
    uint32_t ue_patch;
    uint32_t reserved;
    const char* unrealsharp_revision;
    const char* working_directory;
    void* plugin_callbacks;
    void* binds_callbacks;
    void* managed_callbacks;
    uint32_t plugin_callbacks_size;
    uint32_t managed_callbacks_size;
} Dn2CppUnrealSharpHost;

/* ABI v1 callers provide the full fixed-size result; success is 1, failure is 0. */
typedef struct Dn2CppUnrealSharpResult {
    uint32_t success;
    char error[4096];
} Dn2CppUnrealSharpResult;

/* Lifecycle exports run on the initializing thread. Keep the image loaded after shutdown. */
#ifdef __cplusplus
extern "C" {
#endif
int32_t dn2cpp_unrealsharp_initialize(const Dn2CppUnrealSharpHost*, Dn2CppUnrealSharpResult*);
int32_t dn2cpp_unrealsharp_register_assembly(const char*, Dn2CppUnrealSharpResult*);
int32_t dn2cpp_unrealsharp_tick(float, Dn2CppUnrealSharpResult*);
int32_t dn2cpp_unrealsharp_shutdown(Dn2CppUnrealSharpResult*);
#ifdef __cplusplus
}
#endif

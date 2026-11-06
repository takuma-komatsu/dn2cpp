#include "generated.h"
#include <cstdio>
#include <cstdlib>
#include <cstring>

// Test-only linker instrumentation. Roots prevent collector reclamation while
// inspected; exception allocations are allowed and never counted as Gen2 objects.
#ifdef DN2CPP_GEN2_ALLOC_COUNTERS
static DN2CPP_GC_STATIC_ROOT void* roots[32768];
static size_t sizes[32768];
static size_t allocations;
static size_t ordinaryRegistrations;
static bool active;
#if defined(__EMSCRIPTEN__) || INTPTR_MAX == INT64_MAX
extern "C" void* real_alloc(size_t) asm("__real__Z12dn2cpp_allocm");
extern "C" void* wrapped_alloc(size_t) asm("__wrap__Z12dn2cpp_allocm");
#else
extern "C" void* real_alloc(size_t) asm("__real__Z12dn2cpp_allocj");
extern "C" void* wrapped_alloc(size_t) asm("__wrap__Z12dn2cpp_allocj");
#endif
extern "C" void real_register(Dn2CppObject*) asm("__real__Z25dn2cpp_register_finalizerP12Dn2CppObject");
extern "C" void wrapped_register(Dn2CppObject*) asm("__wrap__Z25dn2cpp_register_finalizerP12Dn2CppObject");
extern "C" void* wrapped_alloc(size_t size)
{
    void* result = real_alloc(size);
    if (active)
    {
        if (allocations == 32768) std::abort();
        roots[allocations] = result;
        sizes[allocations++] = size;
    }
    return result;
}
extern "C" void wrapped_register(Dn2CppObject* obj)
{
    if (active && obj != nullptr)
    {
        if (obj->type == &ti_System_Gen2GcCallback)
        {
            // Fail before unsafe scheduling; do not let a buggy fence queue the
            // deliberately unsupported finalizer on a background thread.
            std::fputs("gen2-probe-target-registration:FAIL\n", stderr);
            std::abort();
        }
        ++ordinaryRegistrations;
    }
    real_register(obj);
}

void gen2_allocation_probe_start() { active = true; }
#else
void gen2_allocation_probe_start() { }
#endif

static void require_pnse(void (*action)(Dn2CppObject*), Dn2CppObject* receiver)
{
    bool refused = false;
    try { action(receiver); }
    catch (Dn2CppException& error)
    {
        refused = error.obj != nullptr && error.obj->type == &dn2cpp_platform_not_supported_exception_type;
        dn2cpp_exc_inflight_pop(error.obj);
    }
    if (!refused) std::abort();
}
static void clone(Dn2CppObject* receiver) { (void)dn2cpp_object_memberwise_clone(receiver); }

void gen2_allocation_probe_finish()
{
    const auto* type = &ti_System_Gen2GcCallback;
    if ((type->reflection().activation & DN2CPP_TA_ALLOCATION_UNSUPPORTED) == 0
        || (type->flags & DN2CPP_TF_SEALED) == 0
        || type->finalize != reinterpret_cast<void(*)(Dn2CppObject*)>(&DN2CPP_GEN2_FINALIZER))
        std::abort();
    // Header-only supplied storage is a native boundary test, not a legitimate
    // managed Gen2 allocation or private CLR finalizer execution.
    struct { Dn2CppObject header; uint64_t sentinel; } supplied = {{type}, 0x123456789abcdef0ULL};
#ifdef DN2CPP_GEN2_SUPPLIED_DELEGATES
    DN2CPP_GEN2_SUPPLIED_DELEGATES(&supplied.header);
#endif
    require_pnse(clone, &supplied.header);
    require_pnse(type->finalize, &supplied.header);
    if (supplied.sentinel != 0x123456789abcdef0ULL) std::abort();
    #ifdef DN2CPP_GEN2_ALLOC_COUNTERS
    size_t targetAllocations = 0;
    for (size_t i = 0; i != allocations; ++i)
    {
        if (sizes[i] < sizeof(Dn2CppObject)) continue;
        const Dn2CppTypeInfo* header = nullptr;
        std::memcpy(&header, roots[i], sizeof(header));
        if (header == type) ++targetAllocations;
    }
    if (targetAllocations != 0 || allocations == 0 || ordinaryRegistrations == 0) std::abort();
    std::puts("gen2-probe-target-allocations:0");
    std::puts("gen2-probe-target-registrations:0");
    std::puts("gen2-probe-ordinary-allocation-registration:True");
    #endif
    std::puts("gen2-probe-retained-finalizer-and-clone-refusal:True");
}

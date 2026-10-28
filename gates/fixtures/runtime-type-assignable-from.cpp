#include "generated.h"

#ifndef DN2CPP_ASSIGNABILITY_RAW_QUERY
#error "The gate must bind the exact emitted raw query"
#endif

namespace {
template<class Action>
void require_exception(Action action, const Dn2CppTypeInfo* expected, const char* message)
{
    bool matched = false;
    try { action(); }
    catch (Dn2CppException& error)
    {
        matched = error.obj != nullptr && error.obj->type == expected;
        dn2cpp_exc_inflight_pop(error.obj);
    }
    if (!matched)
        dn2cpp_fail(message);
}

template<class Query>
void require_raw_boundary(Query query, Dn2CppType* receiver, Dn2CppObject* custom)
{
    if (query(nullptr, nullptr) != 0 || query(receiver, nullptr) != 0)
        dn2cpp_fail("raw assignability must return false for a null source");
    require_exception([&] { query(receiver, custom); }, &dn2cpp_platform_not_supported_exception_type,
        "raw assignability must reject custom source before payload access");
    require_exception([&] { query(nullptr, custom); }, &dn2cpp_platform_not_supported_exception_type,
        "raw custom-source rejection precedes receiver validation");
    require_exception([&] { query(nullptr, receiver); }, &dn2cpp_null_reference_exception_type,
        "raw runtime-Type source requires a receiver");
}
}

void dn2cpp_assignability_boundary_checks()
{
    // This object has only a header, never a Dn2CppType payload. It must not
    // claim the canonical RuntimeType identity whose payload the adapter accepts.
    Dn2CppObject custom = { &dn2cpp_public_type_type };
    if (custom.type == &dn2cpp_type_type)
        dn2cpp_fail("header-only source must not claim the RuntimeType identity");
    auto* receiver = dn2cpp_get_type_from_handle(&dn2cpp_object_type);
    require_raw_boundary(dn2cpp_runtime_type_is_assignable_from, receiver, &custom);

    // Direct calls retain the emitted ABI; only argument pointer representations
    // change. No function pointer cast can hide a mismatched return or signature.
    auto raw = [](Dn2CppType* target, Dn2CppObject* source) -> int32_t {
        return DN2CPP_ASSIGNABILITY_RAW_QUERY(reinterpret_cast<t_System_Type*>(target),
            reinterpret_cast<t_System_Type*>(source));
    };
    require_raw_boundary(raw, receiver, &custom);

#ifdef DN2CPP_ASSIGNABILITY_VIRTUAL_QUERY
    auto virt = [](Dn2CppType* target, Dn2CppObject* source) -> int32_t {
        return DN2CPP_ASSIGNABILITY_VIRTUAL_QUERY(reinterpret_cast<t_System_Type*>(target),
            reinterpret_cast<t_System_Type*>(source));
    };
    require_exception([&] { virt(nullptr, &custom); }, &dn2cpp_null_reference_exception_type,
        "callvirt receiver null must precede unsupported-source validation");
    require_exception([&] { virt(receiver, &custom); }, &dn2cpp_platform_not_supported_exception_type,
        "callvirt custom source must reject before payload access");
    require_exception([&] { virt(nullptr, nullptr); }, &dn2cpp_null_reference_exception_type,
        "callvirt receiver null must precede null-source false");
    if (virt(receiver, nullptr) != 0)
        dn2cpp_fail("callvirt nonnull receiver must accept null source as false");
#endif
}

#include "dn2cpp_core.h"
#include <cstdio>

void dn2cpp_declaring_type_boundary_checks()
{
    auto rejects = [](Dn2CppType* value, const Dn2CppTypeInfo* exception) {
        bool rejected = false;
        try { (void)dn2cpp_runtime_type_get_declaring_type(value); }
        catch (Dn2CppException& error)
        {
            rejected = error.obj != nullptr && error.obj->type == exception;
            dn2cpp_exc_inflight_pop(error.obj);
        }
        if (!rejected) dn2cpp_fail("declaring-type boundary did not refuse correctly");
    };
    rejects(nullptr, &dn2cpp_null_reference_exception_type);
    // Header-only storage proves custom Type rejection precedes payload access.
    Dn2CppObject custom = { &dn2cpp_public_type_type };
    rejects(reinterpret_cast<Dn2CppType*>(&custom), &dn2cpp_platform_not_supported_exception_type);
    Dn2CppType empty = { { &dn2cpp_type_type }, nullptr };
    rejects(&empty, &dn2cpp_null_reference_exception_type);

    // No formal layout or registry entry exists behind this otherwise valid header.
    Dn2CppTypeInfo missing{};
    missing.flags = DN2CPP_TF_GENERICPARAM | DN2CPP_TF_NESTED;
    Dn2CppType type = { { &dn2cpp_type_type }, &missing };
    rejects(&type, &dn2cpp_platform_not_supported_exception_type);
    missing.flags = DN2CPP_TF_NESTED;
    rejects(&type, &dn2cpp_platform_not_supported_exception_type);
    // A cold nested table is neither needed nor sufficient for this hot classification.
    missing.flags = 0;
    if (dn2cpp_runtime_type_get_declaring_type(&type) != nullptr)
        dn2cpp_fail("top-level type must have no enclosing owner");
    missing.flags = DN2CPP_TF_ARRAY | DN2CPP_TF_NESTED;
    if (dn2cpp_runtime_type_get_declaring_type(&type) != nullptr)
        dn2cpp_fail("outer compound must precede nested classification");
    std::fputs("declaring-type-native-boundaries:passed\n", stderr);
}

#include "dn2cpp_core.h"
#include <cstdio>

void dn2cpp_fields_boundary_checks()
{
    auto rejects = [](Dn2CppType* value, int32_t* count, const Dn2CppTypeInfo* exception) {
        bool rejected = false;
        try { (void)dn2cpp_runtime_type_get_fields(value, count); }
        catch (Dn2CppException& error)
        {
            rejected = error.obj != nullptr && error.obj->type == exception;
            dn2cpp_exc_inflight_pop(error.obj);
        }
        if (!rejected) dn2cpp_fail("fields boundary did not refuse correctly");
    };
    int32_t count = 71;
    rejects(nullptr, &count, &dn2cpp_null_reference_exception_type);
    rejects(nullptr, nullptr, &dn2cpp_null_reference_exception_type);
    // Header-only storage proves refusal precedes access to an absent payload.
    Dn2CppObject custom = { &dn2cpp_public_type_type };
    rejects(reinterpret_cast<Dn2CppType*>(&custom), &count, &dn2cpp_platform_not_supported_exception_type);
    Dn2CppType empty = { { &dn2cpp_type_type }, nullptr };
    rejects(&empty, &count, &dn2cpp_null_reference_exception_type);
    Dn2CppType ordinary = { { &dn2cpp_type_type }, &dn2cpp_object_type };
    rejects(&ordinary, &count, &dn2cpp_platform_not_supported_exception_type);
    rejects(&ordinary, nullptr, &dn2cpp_platform_not_supported_exception_type);
    if (count != 71) dn2cpp_fail("fields refusal modified caller count");
    // No formal tail/constraints are accessed by TypeDesc classification.
    Dn2CppTypeInfo formal{};
    formal.flags = DN2CPP_TF_GENERICPARAM;
    Dn2CppType type = { { &dn2cpp_type_type }, &formal };
    rejects(&type, nullptr, &dn2cpp_null_reference_exception_type);
    if (dn2cpp_runtime_type_get_fields(&type, &count) != 1 || count != 0)
        dn2cpp_fail("formal fields branch did not zero count");
    std::fputs("fields-native-boundaries:passed\n", stderr);
}

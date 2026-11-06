#include "dn2cpp_core.h"
#include <cstdio>

void dn2cpp_utf8_name_boundary_checks()
{
    auto rejects = [](Dn2CppType* value, const Dn2CppTypeInfo* exception) {
        bool rejected = false;
        try { dn2cpp_runtime_type_get_utf8_name(value, "Invalid handle."); }
        catch (Dn2CppException& error)
        {
            rejected = error.obj != nullptr && error.obj->type == exception;
            dn2cpp_exc_inflight_pop(error.obj);
        }
        if (!rejected) dn2cpp_fail("UTF8-name boundary did not refuse correctly");
    };
    rejects(nullptr, &dn2cpp_null_reference_exception_type);
    // Header-only storage proves custom receivers are refused before payload access.
    Dn2CppObject custom = { &dn2cpp_public_type_type };
    rejects(reinterpret_cast<Dn2CppType*>(&custom), &dn2cpp_platform_not_supported_exception_type);
    Dn2CppType empty = { { &dn2cpp_type_type }, nullptr };
    rejects(&empty, &dn2cpp_null_reference_exception_type);
    Dn2CppType ordinary = { { &dn2cpp_type_type }, &dn2cpp_object_type };
    rejects(&ordinary, &dn2cpp_platform_not_supported_exception_type);
    // Validation needs only the hot classification, never formal constraint tails.
    Dn2CppTypeInfo formal{};
    formal.flags = DN2CPP_TF_GENERICPARAM;
    Dn2CppType type = { { &dn2cpp_type_type }, &formal };
    rejects(&type, &dn2cpp_argument_exception_type);
    std::fputs("utf8-name-native-boundaries:passed\n", stderr);
}

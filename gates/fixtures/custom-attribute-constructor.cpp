#include "dn2cpp_core.h"
#include <cstdio>

void dn2cpp_cad_constructor_boundary_checks()
{
    auto rejects = [](Dn2CppObject* value, const Dn2CppTypeInfo* exception) {
        bool rejected = false;
        try { (void)dn2cpp_attrdata_constructor_virtual(value); }
        catch (Dn2CppException& error)
        {
            rejected = error.obj != nullptr && error.obj->type == exception;
            dn2cpp_exc_inflight_pop(error.obj);
        }
        if (!rejected) dn2cpp_fail("CAD constructor boundary did not refuse correctly");
    };
    rejects(nullptr, &dn2cpp_null_reference_exception_type);
    // No row payload is present; canonical and custom refusal must only read headers.
    Dn2CppObject canonical = { &dn2cpp_customattributedata_type };
    rejects(&canonical, &dn2cpp_platform_not_supported_exception_type);
    Dn2CppObject unknown = { nullptr };
    rejects(&unknown, &dn2cpp_platform_not_supported_exception_type);
    Dn2CppTypeInfo missing{};
    missing.base = &dn2cpp_customattributedata_type;
    Dn2CppObject custom = { &missing };
    rejects(&custom, &dn2cpp_platform_not_supported_exception_type);
    std::fputs("cad-constructor-native-boundaries:passed\n", stderr);
}

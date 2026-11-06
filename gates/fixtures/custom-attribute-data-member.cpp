#include "dn2cpp_core.h"
#include <cstdio>

void dn2cpp_cad_member_boundary_checks()
{
    auto rejects = [](Dn2CppObject* value, const Dn2CppTypeInfo* exception) {
        bool rejected = false;
        try { (void)dn2cpp_custom_attribute_data_get_custom_attributes(value); }
        catch (Dn2CppException& error)
        {
            rejected = error.obj != nullptr && error.obj->type == exception;
            dn2cpp_exc_inflight_pop(error.obj);
        }
        if (!rejected) dn2cpp_fail("CAD member boundary did not refuse correctly");
    };
    rejects(nullptr, &dn2cpp_argument_null_exception_type);
    // Only a header exists: custom and sibling handles must refuse before payload access.
    Dn2CppTypeInfo custom_type{};
    Dn2CppObject custom = { &custom_type };
    rejects(&custom, &dn2cpp_platform_not_supported_exception_type);
    Dn2CppObject parameter = { &dn2cpp_parameterinfo_type };
    rejects(&parameter, &dn2cpp_platform_not_supported_exception_type);
    Dn2CppObject empty = { nullptr };
    rejects(&empty, &dn2cpp_platform_not_supported_exception_type);
    std::fputs("cad-member-native-boundaries:passed\n", stderr);
}

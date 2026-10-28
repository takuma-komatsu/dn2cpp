#include "dn2cpp_core.h"

void dn2cpp_activator_default_boundary_checks()
{
    Dn2CppObject custom = { &dn2cpp_public_type_type };
    if (custom.type == &dn2cpp_type_type)
        dn2cpp_fail("custom Type probe must have a noncanonical header");
    for (int32_t nonPublic = 0; nonPublic != 2; ++nonPublic)
    for (int32_t wrap = 0; wrap != 2; ++wrap)
    {
        bool rejected = false;
        try { dn2cpp_activator_create_instance_wrapped(reinterpret_cast<Dn2CppType*>(&custom), nonPublic, wrap); }
        catch (Dn2CppException& error)
        {
            rejected = error.obj != nullptr && error.obj->type == &dn2cpp_platform_not_supported_exception_type;
            dn2cpp_exc_inflight_pop(error.obj);
        }
        if (!rejected) dn2cpp_fail("custom Type must refuse before payload access");
    }
}

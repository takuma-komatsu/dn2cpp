#include "dn2cpp_core.h"

void dn2cpp_default_struct_boundary_checks()
{
    Dn2CppObject custom = { &dn2cpp_public_type_type };
    if (custom.type == &dn2cpp_type_type)
        dn2cpp_fail("custom Type probe must have a noncanonical header");
    bool rejected = false;
    try { dn2cpp_runtime_type_call_default_struct_constructor(nullptr, nullptr); }
    catch (Dn2CppException& error)
    {
        rejected = error.obj != nullptr && error.obj->type == &dn2cpp_null_reference_exception_type;
        dn2cpp_exc_inflight_pop(error.obj);
    }
    if (!rejected) dn2cpp_fail("null RuntimeType must throw NRE before touching data");
    rejected = false;
    try { dn2cpp_runtime_type_call_default_struct_constructor(reinterpret_cast<Dn2CppType*>(&custom), nullptr); }
    catch (Dn2CppException& error)
    {
        rejected = error.obj != nullptr && error.obj->type == &dn2cpp_platform_not_supported_exception_type;
        dn2cpp_exc_inflight_pop(error.obj);
    }
    if (!rejected) dn2cpp_fail("noncanonical header must refuse before payload or data access");
    auto refuses_layout = [](const Dn2CppTypeInfo* info) {
        Dn2CppType type = { { &dn2cpp_type_type }, info };
        uint8_t data = 0x73;
        bool refused = false;
        try { dn2cpp_runtime_type_call_default_struct_constructor(&type, &data); }
        catch (Dn2CppException& error)
        {
            refused = error.obj != nullptr && error.obj->type == &dn2cpp_platform_not_supported_exception_type;
            dn2cpp_exc_inflight_pop(error.obj);
        }
        if (!refused || data != 0x73)
            dn2cpp_fail("invalid struct layout must refuse without changing storage");
    };
    refuses_layout(&dn2cpp_void_type);
    refuses_layout(&dn2cpp_object_type);
    Dn2CppTypeInfo unknown{};
    unknown.name = "UnknownStructLayout";
    unknown.flags = DN2CPP_TF_VALUETYPE | DN2CPP_TF_LAYOUT_UNKNOWN;
    refuses_layout(&unknown);
}

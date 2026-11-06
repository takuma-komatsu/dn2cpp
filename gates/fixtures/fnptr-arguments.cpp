#include "dn2cpp_core.h"
#include <cstdio>

void dn2cpp_fnptr_arguments_boundary_checks()
{
    auto rejects = [](Dn2CppType* value, const Dn2CppTypeInfo* exception) {
        bool rejected = false;
        try { (void)dn2cpp_runtime_type_function_pointer_arguments(value, nullptr); }
        catch (Dn2CppException& error)
        {
            rejected = error.obj != nullptr && error.obj->type == exception;
            dn2cpp_exc_inflight_pop(error.obj);
        }
        if (!rejected) dn2cpp_fail("function-pointer arguments safety boundary did not refuse correctly");
    };
    rejects(nullptr, &dn2cpp_null_reference_exception_type);
    // Custom refusal precedes access to a payload absent from this header-only object.
    Dn2CppObject custom = { &dn2cpp_public_type_type };
    rejects(reinterpret_cast<Dn2CppType*>(&custom), &dn2cpp_platform_not_supported_exception_type);
    Dn2CppType empty = { { &dn2cpp_type_type }, nullptr };
    rejects(&empty, &dn2cpp_null_reference_exception_type);
    Dn2CppType ordinary = { { &dn2cpp_type_type }, &dn2cpp_object_type };
    rejects(&ordinary, &dn2cpp_invalid_operation_exception_type);
    std::fputs("fnptr-arguments-native-boundaries:passed\n", stderr);
}

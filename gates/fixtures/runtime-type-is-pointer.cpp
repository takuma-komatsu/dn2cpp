#include "dn2cpp_core.h"

// Erased array metadata is not a pointer; only its outer pointer wrapper is.
void dn2cpp_is_pointer_runtime_checks()
{
    const Dn2CppTypeInfo* arrays[] = {
        &dn2cpp_array_n_type, &dn2cpp_array_ref_type, &dn2cpp_array_i4_type
    };
    for (const auto* array : arrays)
    {
        if (dn2cpp_type_is_pointer(array))
            dn2cpp_fail("runtime-owned array is not a pointer");
        auto* type = dn2cpp_get_type_from_handle(array);
        auto* pointer = dn2cpp_type_make_compound(type, 1);
        auto* byref = dn2cpp_type_make_compound(type, 2);
        auto* pointerByref = dn2cpp_type_make_compound(pointer, 2);
        if (!dn2cpp_type_is_pointer(dn2cpp_type_require(pointer))
            || dn2cpp_type_is_pointer(dn2cpp_type_require(byref))
            || dn2cpp_type_is_pointer(dn2cpp_type_require(pointerByref)))
            dn2cpp_fail("pointer classification uses the outer signature kind");
    }
}

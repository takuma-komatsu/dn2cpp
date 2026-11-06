#include "dn2cpp_core.h"

// Erased array metadata must not inherit the primitive flag of an element.
void dn2cpp_is_primitive_runtime_checks()
{
    const Dn2CppTypeInfo* arrays[] = {
        &dn2cpp_array_n_type, &dn2cpp_array_ref_type, &dn2cpp_array_i4_type
    };
    for (const auto* array : arrays)
    {
        if (dn2cpp_type_is_primitive(array))
            dn2cpp_fail("runtime-owned array is not primitive");
        auto* type = dn2cpp_get_type_from_handle(array);
        auto* pointer = dn2cpp_type_make_compound(type, 1);
        auto* byref = dn2cpp_type_make_compound(type, 2);
        auto* pointerByref = dn2cpp_type_make_compound(pointer, 2);
        if (dn2cpp_type_is_primitive(dn2cpp_type_require(pointer))
            || dn2cpp_type_is_primitive(dn2cpp_type_require(byref))
            || dn2cpp_type_is_primitive(dn2cpp_type_require(pointerByref)))
            dn2cpp_fail("compound handles do not inherit primitive flags");
    }
}

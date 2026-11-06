#include "dn2cpp_core.h"

// These runtime-owned arrays include erased element metadata, which cannot be
// manufactured by the public CLR oracle. Only the outer signature kind counts.
void dn2cpp_is_by_ref_runtime_checks()
{
    const Dn2CppTypeInfo* arrays[] = {
        &dn2cpp_array_n_type, &dn2cpp_array_ref_type, &dn2cpp_array_i4_type
    };
    for (const auto* array : arrays)
    {
        if (dn2cpp_type_is_by_ref(array))
            dn2cpp_fail("runtime-owned array is not by-reference");
        auto* type = dn2cpp_get_type_from_handle(array);
        auto* pointer = dn2cpp_type_make_compound(type, 1);
        auto* byref = dn2cpp_type_make_compound(type, 2);
        auto* pointerByref = dn2cpp_type_make_compound(pointer, 2);
        if (dn2cpp_type_is_by_ref(dn2cpp_type_require(pointer))
            || !dn2cpp_type_is_by_ref(dn2cpp_type_require(byref))
            || !dn2cpp_type_is_by_ref(dn2cpp_type_require(pointerByref)))
            dn2cpp_fail("outer by-reference kind survives erased array metadata");
    }
}

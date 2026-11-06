#include "generated.h"
#include "cache_generic_definition_symbols.h"
#include <cstdio>

void dn2cpp_cache_generic_definition_boundary_checks()
{
    auto cache = (DN2CPP_CACHE_TYPE*)dn2cpp_alloc(sizeof(DN2CPP_CACHE_TYPE));
    cache->type = &DN2CPP_CACHE_TYPEINFO;
    auto sentinel = dn2cpp_get_type_from_handle(&dn2cpp_string_type);
    cache->f__genericTypeDefinition = (decltype(cache->f__genericTypeDefinition))sentinel;
    auto rejects = [&](Dn2CppType* owner, const Dn2CppTypeInfo* exception) {
        cache->f_m_runtimeType = (decltype(cache->f_m_runtimeType))owner;
        bool rejected = false;
        try { (void)DN2CPP_CACHE_HELPER(cache); }
        catch (Dn2CppException& error)
        {
            rejected = error.obj != nullptr && error.obj->type == exception;
            dn2cpp_exc_inflight_pop(error.obj);
        }
        if (!rejected || (Dn2CppType*)cache->f__genericTypeDefinition != sentinel)
            dn2cpp_fail("generic-definition cache boundary wrote before successful lookup");
    };
    rejects(nullptr, &dn2cpp_null_reference_exception_type);
    // Only the object header exists: rejection must precede a Type payload read.
    Dn2CppObject custom = { &dn2cpp_public_type_type };
    rejects(reinterpret_cast<Dn2CppType*>(&custom), &dn2cpp_platform_not_supported_exception_type);
    custom.type = nullptr;
    rejects(reinterpret_cast<Dn2CppType*>(&custom), &dn2cpp_platform_not_supported_exception_type);
    Dn2CppType empty = { { &dn2cpp_type_type }, nullptr };
    rejects(&empty, &dn2cpp_null_reference_exception_type);
    std::fputs("cache-generic-definition-native-boundaries:passed\n", stderr);
}

#include "dn2cpp_core.h"
#include <cstring>
#include <initializer_list>

#if DN2CPP_EXPECT_RAW_MD_GC
#ifndef DN2CPP_USE_BOEHM_GC
#error "readonly owner fixture requires the selected GC axis"
#endif
#elif defined(DN2CPP_USE_BOEHM_GC)
#error "readonly owner fixture requires the selected NoGC axis"
#endif

void dn2cpp_readonly_byref_runtime_checks()
{
    auto require = [](bool condition) {
        if (!condition)
            dn2cpp_fail("readonly byref owner invariant");
    };
    Dn2CppObject* marker = reinterpret_cast<Dn2CppObject*>(uintptr_t(1));
    auto ordinary = [&]() {
        require(dn2cpp_raw_md_owner(&marker) == nullptr);
        require(dn2cpp_byref_load_ref(&marker) == marker);
        for (bool atomic : {false, true})
        {
            void* buffer = atomic ? dn2cpp_alloc_atomic(128) : dn2cpp_alloc(128);
            std::memcpy(buffer, &marker, sizeof(marker));
            require(dn2cpp_raw_md_owner(buffer) == nullptr);
            require(dn2cpp_byref_load_ref(buffer) == marker);
        }
    };
    ordinary();

    static Dn2CppTypeInfo rawType{};
    rawType.elementType = &dn2cpp_cultureinfo_type;
    constexpr size_t bytes = sizeof(Dn2CppMDArray) + 2 * sizeof(void*);
    int64_t before = dn2cpp_gc_allocated_bytes_current_thread();
    auto* raw = static_cast<Dn2CppMDArray*>(dn2cpp_alloc_raw_md(bytes));
    require(dn2cpp_gc_allocated_bytes_current_thread() - before == bytes);
    int32_t length = 1;
    raw->type = &rawType;
    raw->rank = 1;
    raw->lengths = &length;
    raw->elemSize = sizeof(void*);
    raw->data = reinterpret_cast<char*>(raw) + sizeof(Dn2CppMDArray);
    ordinary();
    require(dn2cpp_raw_md_owner(raw->data) == raw);
    require(dn2cpp_byref_load_ref(raw->data) == nullptr);
    // An address in the allocation is insufficient: headers, an unaligned
    // pointer and allocated padding after the final cell are ordinary bit loads.
    require(dn2cpp_byref_load_ref(&raw->type) == reinterpret_cast<Dn2CppObject*>(&rawType));
    std::memcpy(raw->data + sizeof(void*), &marker, sizeof(marker));
    require(dn2cpp_byref_load_ref(raw->data + sizeof(void*)) == marker);
    Dn2CppObject* unaligned;
    std::memcpy(&unaligned, raw->data + 1, sizeof(unaligned));
    require(dn2cpp_byref_load_ref(raw->data + 1) == unaligned);
    length = 0;
    std::memcpy(raw->data, &marker, sizeof(marker));
    require(dn2cpp_byref_load_ref(raw->data) == marker);

    int32_t lower = -3;
    length = 1;
    auto* rank1 = dn2cpp_newmdarr(&rawType, 1, &length, sizeof(void*), &lower);
    const auto* culture = dn2cpp_nfi_invariant();
    std::memcpy(rank1->data, &culture, sizeof(culture));
    require(dn2cpp_raw_md_owner(rank1->data) == rank1);
    require(dn2cpp_byref_load_ref(dn2cpp_md_elem_addr(rank1, &lower))
        == dn2cpp_nfi_wrap(culture, DN2CPP_NFI_KIND_CULTURE));

    static Dn2CppTypeInfo managedType{};
    managedType.elementType = &dn2cpp_object_type;
    int32_t lengths[] = {1, 1};
    auto* managed = dn2cpp_newmdarr(&managedType, 2, lengths, sizeof(void*));
    require(dn2cpp_raw_md_owner(managed->data) == nullptr);
    std::memcpy(managed->data, &marker, sizeof(marker));
    require(dn2cpp_byref_load_ref(managed->data) == marker);
}

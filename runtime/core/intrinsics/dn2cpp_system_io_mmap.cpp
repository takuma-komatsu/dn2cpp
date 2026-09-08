#include "dn2cpp_core.h"

static void dn2cpp_mappedfile_finalize(Dn2CppObject* obj)
{
    auto* f = static_cast<Dn2CppMappedFile*>(obj);
    // Reflection can allocate the wrapper without constructing its descriptor owner.
    if (f->sync != nullptr) dn2cpp_mmap_file_dispose(f);
}

extern const Dn2CppType dn2cpp_mappedfile_type_obj;
Dn2CppTypeInfo dn2cpp_mappedfile_type = [] {
    auto ti = dn2cpp_ti_with_typeobject(
        { "System.IO.MemoryMappedFiles.MemoryMappedFile", &dn2cpp_object_type,
          (int32_t)sizeof(Dn2CppMappedFile), nullptr, nullptr, 0,
          nullptr, nullptr, nullptr, DN2CPP_TF_NO_SHALLOW_CLONE },
        &dn2cpp_mappedfile_type_obj);
    ti.assemblyName = "System.IO.MemoryMappedFiles";
    ti.finalize = &dn2cpp_mappedfile_finalize;
    return ti;
}();
const Dn2CppType dn2cpp_mappedfile_type_obj = { { &dn2cpp_type_type }, &dn2cpp_mappedfile_type };

Dn2CppMappedFile* dn2cpp_mmap_file_new()
{
    auto* f = static_cast<Dn2CppMappedFile*>(dn2cpp_alloc(sizeof(Dn2CppMappedFile)));
    f->type = &dn2cpp_mappedfile_type;
    f->fd = -1;
    auto* sync = static_cast<Dn2CppObject*>(dn2cpp_alloc(sizeof(Dn2CppObject)));
    sync->type = &dn2cpp_object_type;
    dn2cpp_gc_store_ref(&f->sync, sync);
    // Register before opening the descriptor: allocation failure cannot leak it.
    dn2cpp_register_finalizer(f);
    return f;
}

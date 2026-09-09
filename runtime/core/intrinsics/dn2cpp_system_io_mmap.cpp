#include "dn2cpp_core.h"

static void dn2cpp_mappedfile_finalize(Dn2CppObject* obj)
{
    auto* f = static_cast<Dn2CppMappedFile*>(obj);
    // Reflection can allocate the wrapper without constructing its descriptor owner.
    if (f->sync != nullptr)
    {
        // Collecting the map must not close a source handle still rooted by its caller.
        f->disposeSource = nullptr;
        dn2cpp_mmap_file_dispose(f);
    }
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

void dn2cpp_mmap_validate_create(Dn2CppObject* source, Dn2CppString* mapName,
                                int64_t capacity, int32_t access)
{
    if (source == nullptr) dn2cpp_throw_argument_null();
    if (mapName != nullptr && mapName->length == 0)
        dn2cpp_throw_of(&dn2cpp_argument_exception_type);
    if (capacity < 0 || access < 0 || access > 5)
        dn2cpp_throw_of(&dn2cpp_argument_out_of_range_exception_type);
    if (access == 2) dn2cpp_throw_of(&dn2cpp_argument_exception_type);
    if (mapName != nullptr || (access != 0 && access != 1))
        dn2cpp_throw_of(&dn2cpp_not_supported_exception_type);
}

void dn2cpp_mmap_validate_capacity(int64_t length, int64_t capacity, int32_t access)
{
    if ((capacity == 0 && length == 0) || (access == 1 && capacity > length))
        dn2cpp_throw_of(&dn2cpp_argument_exception_type);
    if (capacity < 0 || (capacity != 0 && capacity < length))
        dn2cpp_throw_of(&dn2cpp_argument_out_of_range_exception_type);
}

void dn2cpp_mmap_dispose_source(Dn2CppMappedFile* f)
{
    auto* source = f->sourceHandle;
    dn2cpp_gc_store_ref(&f->sourceHandle, static_cast<Dn2CppObject*>(nullptr));
    if (source != nullptr && f->disposeSource != nullptr) f->disposeSource(source);
}

static void dn2cpp_mappedview_finalize(Dn2CppObject* object)
{
    auto* view = static_cast<Dn2CppMappedViewObject*>(object);
    if (view->safeHandle == nullptr) dn2cpp_mmap_view_dispose(view->view);
    dn2cpp_gc_store_ref(&view->safeHandle, static_cast<Dn2CppObject*>(nullptr));
    view->view = {};
}

extern const Dn2CppType dn2cpp_mappedview_type_obj;
extern const Dn2CppType dn2cpp_unmanaged_memory_accessor_type_obj;
Dn2CppTypeInfo dn2cpp_unmanaged_memory_accessor_type = [] {
    return dn2cpp_ti_with_typeobject(
        { "System.IO.UnmanagedMemoryAccessor", &dn2cpp_object_type,
          (int32_t)sizeof(Dn2CppMappedViewObject), nullptr, nullptr, 0,
          nullptr, nullptr, nullptr, DN2CPP_TF_NO_SHALLOW_CLONE },
        &dn2cpp_unmanaged_memory_accessor_type_obj);
}();
const Dn2CppType dn2cpp_unmanaged_memory_accessor_type_obj = {
    { &dn2cpp_type_type }, &dn2cpp_unmanaged_memory_accessor_type };
Dn2CppTypeInfo dn2cpp_mappedview_type = [] {
    auto ti = dn2cpp_ti_with_typeobject(
        { "System.IO.MemoryMappedFiles.MemoryMappedViewAccessor", &dn2cpp_unmanaged_memory_accessor_type,
          (int32_t)sizeof(Dn2CppMappedViewObject), nullptr, nullptr, 0,
          nullptr, nullptr, nullptr, DN2CPP_TF_NO_SHALLOW_CLONE },
        &dn2cpp_mappedview_type_obj);
    ti.assemblyName = "System.IO.MemoryMappedFiles";
    ti.finalize = &dn2cpp_mappedview_finalize;
    return ti;
}();
const Dn2CppType dn2cpp_mappedview_type_obj = { { &dn2cpp_type_type }, &dn2cpp_mappedview_type };

Dn2CppMappedViewObject* dn2cpp_mmap_view_object_new(Dn2CppMappedView data)
{
    try
    {
        auto* view = static_cast<Dn2CppMappedViewObject*>(dn2cpp_alloc(sizeof(Dn2CppMappedViewObject)));
        view->type = &dn2cpp_mappedview_type;
        view->view = data;
        auto* sync = static_cast<Dn2CppObject*>(dn2cpp_alloc(sizeof(Dn2CppObject)));
        sync->type = &dn2cpp_object_type;
        dn2cpp_gc_store_ref(&view->sync, sync);
        dn2cpp_register_finalizer(view);
        return view;
    }
    catch (...)
    {
        dn2cpp_mmap_view_dispose(data);
        throw;
    }
}

void dn2cpp_mmap_view_object_dispose(Dn2CppMappedViewObject* view)
{
    dn2cpp_null_check(view);
    if (view->sync == nullptr) dn2cpp_throw_null_reference();
    Dn2CppMonitorGuard guard(view->sync);
    if (view->disposed) return;
    view->disposed = true;
    if (view->safeHandle != nullptr && view->disposeHandle != nullptr)
        view->disposeHandle(view->safeHandle);
    else
        dn2cpp_mmap_view_dispose(view->view);
    dn2cpp_gc_suppress_finalize(view);
}

Dn2CppMappedView dn2cpp_mmap_view_data(Dn2CppMappedViewObject* view)
{
    dn2cpp_null_check(view);
    if (view->disposed || (view->safeHandle != nullptr && view->isHandleClosed != nullptr
        && view->isHandleClosed(view->safeHandle))) dn2cpp_throw_object_disposed();
    return view->view;
}

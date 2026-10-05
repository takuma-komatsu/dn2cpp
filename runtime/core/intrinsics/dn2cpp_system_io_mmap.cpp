#include "dn2cpp_metadata_native.h"
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

DN2CPP_NATIVE_TYPE_REFLECTION(dn2cpp_mmap_reflection,
    nullptr, 0, nullptr, 0, nullptr, 0, nullptr, 0, nullptr, 0, nullptr, 0,
    nullptr, 0, "System.IO.MemoryMappedFiles");

extern const Dn2CppType dn2cpp_mappedfile_type_obj;
Dn2CppTypeInfo dn2cpp_mappedfile_type = [] {
    auto ti = dn2cpp_ti_with_typeobject(
        { "System.IO.MemoryMappedFiles.MemoryMappedFile", &dn2cpp_object_type, nullptr, nullptr, nullptr, nullptr, nullptr, 0, 0, 0, 0, 0, 0, 0, 0, (int32_t)sizeof(Dn2CppMappedFile), 0, DN2CPP_TF_NO_SHALLOW_CLONE, 0, 0, 0, nullptr },
        &dn2cpp_mappedfile_type_obj);
    ti.reflectionData = dn2cpp_mmap_reflection;
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

// Every factory's checks of its own arguments, in .NET's order, with the sentences
// System.IO.MemoryMappedFiles raises.
static void dn2cpp_mmap_validate_arguments(Dn2CppString* mapName, int64_t capacity, int32_t access)
{
    if (mapName != nullptr && mapName->length == 0)
        dn2cpp_throw_sr0(&dn2cpp_argument_exception_type, DN2CPP_SR_MMF_MAP_NAME_EMPTY);
    if (capacity < 0)
        dn2cpp_throw_argument_out_of_range_param(DN2CPP_SR_MMF_CAPACITY_NEGATIVE, "capacity");
    if (access < 0 || access > 5)
        dn2cpp_throw_argument_out_of_range_param(DN2CPP_SR_ARGUMENT_OUT_OF_RANGE, "access");
    if (access == 2)
        dn2cpp_throw_argument_param(DN2CPP_SR_MMF_WRITE_ACCESS, "access");
}

void dn2cpp_mmap_validate_create(Dn2CppObject* source, const char* sourceName,
                                Dn2CppString* mapName, int64_t capacity, int32_t access)
{
    if (source == nullptr) dn2cpp_throw_argument_null_param(sourceName);
    dn2cpp_mmap_validate_arguments(mapName, capacity, access);
#if defined(_WIN32)
    if (mapName != nullptr || (access != 0 && access != 1))
#else
    if (mapName == nullptr && access != 0 && access != 1)
#endif
        dn2cpp_throw_of(&dn2cpp_not_supported_exception_type);
}

void dn2cpp_mmap_validate_stream_prerequisites(int64_t length, int64_t capacity,
                                               int32_t inheritability)
{
    if (capacity == 0 && length == 0)
        dn2cpp_throw_sr0(&dn2cpp_argument_exception_type, DN2CPP_SR_MMF_EMPTY_FILE);
    if (inheritability < 0 || inheritability > 1)
        dn2cpp_throw_argument_out_of_range_param(DN2CPP_SR_ARGUMENT_OUT_OF_RANGE, "inheritability");
}

void dn2cpp_mmap_validate_capacity(int64_t length, int64_t capacity, int32_t access,
                                   int32_t inheritability)
{
    dn2cpp_mmap_validate_stream_prerequisites(length, capacity, inheritability);
    if (access == 1 && capacity > length)
        dn2cpp_throw_sr0(&dn2cpp_argument_exception_type, DN2CPP_SR_MMF_READ_ACCESS_WITH_LARGE_CAPACITY);
    if (capacity != 0 && capacity < length)
        dn2cpp_throw_argument_out_of_range_param(DN2CPP_SR_MMF_CAPACITY_BELOW_FILE_SIZE, "capacity");
}

void dn2cpp_mmap_validate_named_stream(Dn2CppString* mapName, int64_t length,
                                      int64_t capacity, int32_t access, int32_t inheritability)
{
#if !defined(_WIN32)
    if (mapName != nullptr && length > 0)
    {
        dn2cpp_mmap_validate_capacity(length, capacity, access, inheritability);
        dn2cpp_throw_sr0(&dn2cpp_platform_not_supported_exception_type, DN2CPP_SR_MMF_NAMED_MAPS);
    }
#else
    (void)mapName; (void)length; (void)capacity; (void)access; (void)inheritability;
#endif
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
        { "System.IO.UnmanagedMemoryAccessor", &dn2cpp_object_type, nullptr, nullptr, nullptr, nullptr, nullptr, 0, 0, 0, 0, 0, 0, 0, 0, (int32_t)sizeof(Dn2CppMappedViewObject), 0, DN2CPP_TF_NO_SHALLOW_CLONE, 0, 0, 0, nullptr },
        &dn2cpp_unmanaged_memory_accessor_type_obj);
}();
const Dn2CppType dn2cpp_unmanaged_memory_accessor_type_obj = {
    { &dn2cpp_type_type }, &dn2cpp_unmanaged_memory_accessor_type };
Dn2CppTypeInfo dn2cpp_mappedview_type = [] {
    auto ti = dn2cpp_ti_with_typeobject(
        { "System.IO.MemoryMappedFiles.MemoryMappedViewAccessor", &dn2cpp_unmanaged_memory_accessor_type, nullptr, nullptr, nullptr, nullptr, nullptr, 0, 0, 0, 0, 0, 0, 0, 0, (int32_t)sizeof(Dn2CppMappedViewObject), 0, DN2CPP_TF_NO_SHALLOW_CLONE, 0, 0, 0, nullptr },
        &dn2cpp_mappedview_type_obj);
    ti.reflectionData = dn2cpp_mmap_reflection;
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

Dn2CppMappedView dn2cpp_mmap_view_data(Dn2CppMappedViewObject* view, bool flushing)
{
    dn2cpp_null_check(view);
    if (view->disposed)
        dn2cpp_throw_object_disposed_named(flushing
            ? dn2cpp_string_literal(u"MemoryMappedViewAccessor", 24)
            : dn2cpp_string_literal(u"UnmanagedMemoryAccessor", 23),
            dn2cpp_sr_message(DN2CPP_SR_ACCESSOR_CLOSED, nullptr, 0));
    if (view->safeHandle != nullptr && view->isHandleClosed != nullptr
        && view->isHandleClosed(view->safeHandle))
        dn2cpp_throw_object_disposed_named(dn2cpp_type_fullname(view->safeHandle->type));
    return view->view;
}

// FileAccess of a view's MemoryMappedFileAccess: Write alone cannot read; ReadWrite,
// Write, CopyOnWrite and ReadWriteExecute can write.
static void dn2cpp_mmap_require_access(const Dn2CppMappedView& v, bool write)
{
    if (write ? !(v.access == 0 || v.access == 2 || v.access == 3 || v.access == 5) : v.access == 2)
        dn2cpp_throw_sr0(&dn2cpp_not_supported_exception_type,
            write ? DN2CPP_SR_NOT_SUPPORTED_WRITING : DN2CPP_SR_NOT_SUPPORTED_READING);
}

static void dn2cpp_mmap_require_position(int64_t position)
{
    if (position < 0)
        dn2cpp_throw_argument_out_of_range_bound(DN2CPP_SR_MUST_BE_NON_NEGATIVE, "position", position, 0, 8);
}

uint8_t* dn2cpp_mmap_view_at(Dn2CppMappedViewObject* view, int64_t position, int32_t size,
                             bool write, bool positionFirst)
{
    if (positionFirst) dn2cpp_mmap_require_position(position);
    Dn2CppMappedView v = dn2cpp_mmap_view_data(view);
    dn2cpp_mmap_require_access(v, write);
    dn2cpp_mmap_require_position(position);
    if (position > v.capacity - size)
    {
        if (position >= v.capacity)
            dn2cpp_throw_argument_out_of_range_param(DN2CPP_SR_POSITION_LESS_THAN_CAPACITY_REQUIRED, "position");
        dn2cpp_throw_argument_param(write ? DN2CPP_SR_NOT_ENOUGH_BYTES_TO_WRITE : DN2CPP_SR_NOT_ENOUGH_BYTES_TO_READ,
            "position");
    }
    return v.addr + position;
}

void dn2cpp_mmap_check_array_run(Dn2CppArray* array, int32_t offset, int32_t count)
{
    if (array == nullptr) dn2cpp_throw_argument_null_param("array");
    if (offset < 0)
        dn2cpp_throw_argument_out_of_range_bound(DN2CPP_SR_MUST_BE_NON_NEGATIVE, "offset", offset, 0, 4);
    if (count < 0)
        dn2cpp_throw_argument_out_of_range_bound(DN2CPP_SR_MUST_BE_NON_NEGATIVE, "count", count, 0, 4);
    if (array->length - offset < count)
        dn2cpp_throw_sr0(&dn2cpp_argument_exception_type, DN2CPP_SR_INVALID_OFF_LEN);
}

int32_t dn2cpp_mmap_read_array(Dn2CppMappedViewObject* view, int64_t position, void* dst,
                               int32_t count, int32_t elemSize)
{
    Dn2CppMappedView v = dn2cpp_mmap_view_data(view);
    dn2cpp_mmap_require_access(v, false);
    dn2cpp_mmap_require_position(position);
    if (position >= v.capacity)
        dn2cpp_throw_argument_out_of_range_param(DN2CPP_SR_POSITION_LESS_THAN_CAPACITY_REQUIRED, "position");
    return dn2cpp_mmap_read_into(v, position, dst, count, elemSize);
}

void dn2cpp_mmap_write_array(Dn2CppMappedViewObject* view, int64_t position, const void* src,
                             int32_t count, int32_t elemSize)
{
    dn2cpp_mmap_require_position(position);
    // .NET reads the capacity before it asks whether the view is open.
    if (position >= dn2cpp_null_check(view)->view.capacity)
        dn2cpp_throw_argument_out_of_range_param(DN2CPP_SR_POSITION_LESS_THAN_CAPACITY_REQUIRED, "position");
    Dn2CppMappedView v = dn2cpp_mmap_view_data(view);
    dn2cpp_mmap_require_access(v, true);
    dn2cpp_mmap_write_from(v, position, src, count, elemSize);
}

void dn2cpp_mmap_validate_path_create(Dn2CppString* path, int32_t fileMode, Dn2CppString* mapName,
                                      int64_t capacity, int32_t access)
{
    if (path == nullptr) dn2cpp_throw_argument_null_param("path");
    dn2cpp_mmap_validate_arguments(mapName, capacity, access);
    if (fileMode == 6)
        dn2cpp_throw_argument_param(DN2CPP_SR_MMF_APPEND_MODE, "mode");
    if (fileMode == 5)
        dn2cpp_throw_argument_param(DN2CPP_SR_MMF_TRUNCATE_MODE, "mode");
    // File.OpenHandle's checks: ThrowIfNullOrEmpty, the mode, then those of the
    // Path.GetFullPath it opens through, which on Windows also refuse an all-space path.
    if (path->length == 0)
        dn2cpp_throw_argument_param(DN2CPP_SR_EMPTY_STRING, "path");
    if (fileMode < 1 || fileMode > 6)
        dn2cpp_throw_argument_out_of_range_param(DN2CPP_SR_ENUM_OUT_OF_RANGE, "mode");
    dn2cpp_path_check_resolvable(path);
}

// dn2cpp_system_io_mmap.cpp — file-backed System.IO.MemoryMappedFiles subset.
//
// A bounded MemoryMappedFile mapped through POSIX mmap/munmap on the macOS/POSIX
// target. CreateFromFile opens + fstats the file; CreateViewAccessor mmaps a
// (page-aligned) range; the Read*/Write* accessors and the raw AcquirePointer scan
// operate on the mapped bytes; Flush msyncs; Dispose munmaps / closes the fd.
//
// MemoryMappedFile owns its descriptor independently of the mapped views.
// Named maps and CreateViewStream are unsupported.

#include "dn2cpp_core.h"

#include <string>     // managed path strings as NUL-terminated UTF-8 std::string
#include <cstring>    // std::memcpy
#include <cerrno>
#include <fcntl.h>    // open / O_RDONLY / O_RDWR / O_CREAT
#include <unistd.h>   // close / ftruncate / sysconf
#include <sys/mman.h> // mmap / munmap / msync / PROT_* / MAP_*
#include <sys/stat.h> // fstat

// SafeMemoryMappedViewHandle.ReleaseHandle retains the real SafeBuffer lease count.
extern "C" int32_t SystemNative_MUnmap(void* address, uint64_t length)
{
    if (length > SIZE_MAX)
    {
        errno = EOVERFLOW;
        return -1;
    }
    return ::munmap(address, static_cast<size_t>(length));
}

// A managed path string as a NUL-terminated UTF-8 std::string (file-local; mirrors
// the helper in dn2cpp_system_io.cpp).
static std::string dn2cpp_mmap_path_utf8(Dn2CppString* p)
{
    if (p == nullptr) dn2cpp_throw_argument_null_param("path");
    int32_t n = dn2cpp_string_to_utf8(p, nullptr, 0);
    std::string s(static_cast<size_t>(n), '\0');
    if (n > 0) dn2cpp_string_to_utf8(p, s.data(), n);
    return s;
}

Dn2CppMappedFile* dn2cpp_mmap_create_from_file(Dn2CppString* path, Dn2CppString* mapName,
                                              int32_t fileMode, int32_t access, int64_t capacity)
{
    dn2cpp_mmap_validate_path_create(path, fileMode, mapName, capacity, access);
    // File.OpenHandle runs before the platform's named-map refusal.
    if (access != 0 && access != 1)
        dn2cpp_throw_of(&dn2cpp_not_supported_exception_type);
    if (fileMode != 3 && fileMode != 4) // Open=3, OpenOrCreate=4
        dn2cpp_throw_of(&dn2cpp_not_supported_exception_type);

    path = dn2cpp_path_get_full_path(path);
    std::string p = dn2cpp_mmap_path_utf8(path);
    auto* f = dn2cpp_mmap_file_new();
    int oflag = (access == 1) ? O_RDONLY : O_RDWR;
    if (fileMode == 4) oflag |= O_CREAT; // OpenOrCreate
    struct stat st;
    bool existed = ::stat(p.c_str(), &st) == 0;
    int fd = ::open(p.c_str(), oflag, 0666);
    if (fd < 0)
    {
        dn2cpp_file_throw_open_failure(errno, path);
    }

    if (::fstat(fd, &st) != 0)
    {
        ::close(fd);
        dn2cpp_throw_of(&dn2cpp_io_exception_type);
    }
    // A read-only open of a directory succeeds; .NET refuses it as an access.
    if (S_ISDIR(st.st_mode))
    {
        ::close(fd);
        dn2cpp_throw_sr1(&dn2cpp_unauthorized_access_exception_type,
            DN2CPP_SR_UNAUTHORIZED_ACCESS_PATH, dn2cpp_path_get_full_path(path));
    }
    int64_t len = static_cast<int64_t>(st.st_size);
    try
    {
        if (mapName != nullptr && len == 0 && !S_ISREG(st.st_mode))
        {
            dn2cpp_mmap_validate_stream_prerequisites(len, capacity, 0);
            dn2cpp_throw_sr0(&dn2cpp_platform_not_supported_exception_type, DN2CPP_SR_MMF_NAMED_MAPS);
        }
        dn2cpp_mmap_validate_capacity(len, capacity, access, 0);
        if (mapName != nullptr)
            dn2cpp_throw_sr0(&dn2cpp_platform_not_supported_exception_type, DN2CPP_SR_MMF_NAMED_MAPS);
    }
    catch (...)
    {
        ::close(fd);
        if (!existed)
            ::unlink(p.c_str());
        throw;
    }

    // A ReadWrite map with an explicit capacity larger than the file grows the file
    // (the backing range must exist for mmap). Read access cannot grow the file.
    if (access == 0 && capacity > len)
    {
        if (::ftruncate(fd, static_cast<off_t>(capacity)) != 0)
        {
            ::close(fd);
            dn2cpp_throw_of(&dn2cpp_io_exception_type);
        }
        len = capacity;
    }

    f->fd = fd;
    f->access = access;
    f->length = len;
    return f;
}

void dn2cpp_mmap_file_dispose(Dn2CppMappedFile* f)
{
    dn2cpp_null_check(f);
    if (f->sync == nullptr) dn2cpp_throw_null_reference();
    Dn2CppMonitorGuard guard(f->sync);
    int32_t fd = f->fd;
    f->fd = -1;
    if (fd >= 0) ::close(fd);
    dn2cpp_mmap_dispose_source(f);
    dn2cpp_gc_suppress_finalize(f);
}

Dn2CppMappedFile* dn2cpp_mmap_create_from_handle(intptr_t handle, Dn2CppString* mapName, int32_t access,
                                                int64_t capacity, int32_t inheritability,
                                                int64_t validationLength, int32_t hasValidationLength)
{
    struct stat st;
    if (::fstat(static_cast<int>(handle), &st) != 0)
        dn2cpp_throw_of(&dn2cpp_io_exception_type);
    int64_t length = static_cast<int64_t>(st.st_size);
    int64_t checkedLength = hasValidationLength ? validationLength : length;
    if (mapName != nullptr && checkedLength == 0 && !S_ISREG(st.st_mode))
    {
        dn2cpp_mmap_validate_stream_prerequisites(checkedLength, capacity, inheritability);
        dn2cpp_throw_sr0(&dn2cpp_platform_not_supported_exception_type, DN2CPP_SR_MMF_NAMED_MAPS);
    }
    dn2cpp_mmap_validate_capacity(checkedLength, capacity, access, inheritability);
    if (mapName != nullptr)
        dn2cpp_throw_sr0(&dn2cpp_platform_not_supported_exception_type, DN2CPP_SR_MMF_NAMED_MAPS);
    int64_t mapLength = hasValidationLength
        ? (capacity == 0 ? validationLength : capacity)
        : (capacity > length ? capacity : length);
    auto* f = dn2cpp_mmap_file_new();
    int fd = ::dup(static_cast<int>(handle));
    if (fd < 0) dn2cpp_throw_of(&dn2cpp_io_exception_type);
    f->fd = fd;
    if (::fcntl(fd, F_SETFD, inheritability == 0 ? FD_CLOEXEC : 0) != 0
        || (capacity > checkedLength && ::ftruncate(fd, static_cast<off_t>(mapLength)) != 0))
    {
        dn2cpp_mmap_file_dispose(f);
        dn2cpp_throw_of(&dn2cpp_io_exception_type);
    }
    f->access = access;
    f->length = mapLength;
    return f;
}

Dn2CppMappedView dn2cpp_mmap_create_view(Dn2CppMappedFile* f, int64_t offset, int64_t size, int32_t access)
{
    dn2cpp_null_check(f);
    if (f->sync == nullptr) dn2cpp_throw_null_reference();
    // .NET checks the arguments before it touches the map's handle.
    if (offset < 0)
        dn2cpp_throw_argument_out_of_range_bound(DN2CPP_SR_MUST_BE_NON_NEGATIVE, "offset", offset, 0, 8);
    if (size < 0)
        dn2cpp_throw_argument_out_of_range_param(DN2CPP_SR_MMF_SIZE_NEGATIVE, "size");
    if (access < 0 || access > 5)
        dn2cpp_throw_argument_out_of_range_param(DN2CPP_SR_ARGUMENT_OUT_OF_RANGE, "access");
    if (sizeof(void*) == 4 && static_cast<uint64_t>(size) > 0xFFFFFFFFull)
        dn2cpp_throw_argument_out_of_range_param(DN2CPP_SR_MMF_SIZE_PAST_ADDRESS_SPACE, "size");
    Dn2CppMonitorGuard guard(f->sync);
    // Unix .NET checks the range against the map's capacity, which outlives Dispose,
    // before it checks the handle is still open.
    if (offset > f->length)
        dn2cpp_throw_argument_out_of_range_bound(DN2CPP_SR_MUST_BE_LESS_OR_EQUAL, "offset", offset,
            f->length, 8);
    if (size > 8192000000000LL) // MemoryMappedView.MaxProcessAddressSpace
        dn2cpp_throw_sr0(&dn2cpp_io_exception_type, DN2CPP_SR_MMF_SIZE_PAST_ADDRESS_SPACE);
    int64_t viewSize = (size == 0) ? (f->length - offset) : size; // 0 => rest of file
    // .NET refuses a view that runs past the mapping as an unauthorized access.
    if (viewSize > f->length - offset)
        dn2cpp_throw_of(&dn2cpp_unauthorized_access_exception_type);
    if (f->fd < 0) dn2cpp_throw_object_disposed();
    if (access != 0 && access != 1)
        dn2cpp_throw_of(&dn2cpp_not_supported_exception_type);
    if (f->access == 1 && access == 0)
        dn2cpp_throw_of(&dn2cpp_unauthorized_access_exception_type);

    // mmap requires a page-aligned file offset; map from the aligned offset and
    // expose the user's offset as `addr` (aligned base + the intra-page delta).
    long pageSize = ::sysconf(_SC_PAGESIZE);
    if (pageSize <= 0) pageSize = 4096;
    int64_t alignedOffset = offset - (offset % pageSize);
    int64_t delta = offset - alignedOffset;
    if (sizeof(void*) == 4 && static_cast<uint64_t>(viewSize + delta) > 0xFFFFFFFFull)
        dn2cpp_throw_argument_out_of_range_param(DN2CPP_SR_MMF_SIZE_PAST_ADDRESS_SPACE, "size");
    size_t mapLen = static_cast<size_t>(viewSize + delta);
    if (mapLen == 0) mapLen = 1; // mmap rejects a zero length

    int prot = (access == 1) ? PROT_READ : (PROT_READ | PROT_WRITE);
    void* m = ::mmap(nullptr, mapLen, prot, MAP_SHARED, f->fd, static_cast<off_t>(alignedOffset));
    if (m == MAP_FAILED)
        dn2cpp_throw_of(&dn2cpp_io_exception_type);

    Dn2CppMappedView v;
    v.mapBase = static_cast<uint8_t*>(m);
    v.mapLen = static_cast<int64_t>(mapLen);
    v.addr = v.mapBase + delta;
    v.capacity = viewSize;
    v.access = access;
    return v;
}

void dn2cpp_mmap_view_flush(Dn2CppMappedView v)
{
    if (v.mapBase != nullptr && v.mapLen > 0)
        ::msync(v.mapBase, static_cast<size_t>(v.mapLen), MS_SYNC);
}

void dn2cpp_mmap_view_dispose(Dn2CppMappedView v)
{
    if (v.mapBase != nullptr && v.mapLen > 0)
        ::munmap(v.mapBase, static_cast<size_t>(v.mapLen));
}

int32_t dn2cpp_mmap_read_into(Dn2CppMappedView v, int64_t pos, void* dst, int32_t count, int32_t elemSize)
{
    if (pos < 0)
        dn2cpp_throw_argument_out_of_range_bound(DN2CPP_SR_MUST_BE_NON_NEGATIVE, "position", pos, 0, 8);
    int64_t avail = v.capacity - pos;
    if (avail < 0) avail = 0;
    int64_t maxElems = avail / elemSize;
    int32_t n = (static_cast<int64_t>(count) <= maxElems) ? count : static_cast<int32_t>(maxElems);
    if (n > 0) std::memcpy(dst, v.addr + pos, static_cast<size_t>(n) * static_cast<size_t>(elemSize));
    return n;
}

void dn2cpp_mmap_write_from(Dn2CppMappedView v, int64_t pos, const void* src, int32_t count, int32_t elemSize)
{
    if (pos < 0)
        dn2cpp_throw_argument_out_of_range_bound(DN2CPP_SR_MUST_BE_NON_NEGATIVE, "position", pos, 0, 8);
    if ((pos + static_cast<int64_t>(count) * elemSize) > v.capacity)
    {
        if (pos >= v.capacity)
            dn2cpp_throw_argument_out_of_range_param(DN2CPP_SR_POSITION_LESS_THAN_CAPACITY_REQUIRED,
                "position");
        dn2cpp_throw_sr0(&dn2cpp_argument_exception_type, DN2CPP_SR_BUFFER_TOO_SMALL);
    }
    if (count > 0) std::memcpy(v.addr + pos, src, static_cast<size_t>(count) * static_cast<size_t>(elemSize));
}

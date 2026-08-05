// dn2cpp_system_io_mmap.cpp — file-backed System.IO.MemoryMappedFiles subset.
//
// A bounded MemoryMappedFile mapped through POSIX mmap/munmap on the macOS/POSIX
// target. CreateFromFile opens + fstats the file; CreateViewAccessor mmaps a
// (page-aligned) range; the Read*/Write* accessors and the raw AcquirePointer scan
// operate on the mapped bytes; Flush msyncs; Dispose munmaps / closes the fd.
//
// The three BCL reference types lower to the small by-value intrinsic structs in
// dn2cpp.h (a non-moving handle, like GCHandle). Named maps / cross-process /
// CreateNew / non-null mapName / CreateViewStream / the Windows path are carve-outs
// (loud NotSupportedException). Semantics probed against real .NET — see
// gates/build-and-run-mmap-file.sh.

#include "dn2cpp_core.h"

#include <string>     // managed path strings as NUL-terminated UTF-8 std::string
#include <cstring>    // std::memcpy
#include <fcntl.h>    // open / O_RDONLY / O_RDWR / O_CREAT
#include <unistd.h>   // close / ftruncate / sysconf
#include <sys/mman.h> // mmap / munmap / msync / PROT_* / MAP_*
#include <sys/stat.h> // fstat

// A managed path string as a NUL-terminated UTF-8 std::string (file-local; mirrors
// the helper in dn2cpp_system_io.cpp).
static std::string dn2cpp_mmap_path_utf8(Dn2CppString* p)
{
    if (p == nullptr) dn2cpp_throw_argument_null();
    int32_t n = dn2cpp_string_to_utf8(p, nullptr, 0);
    std::string s(static_cast<size_t>(n), '\0');
    if (n > 0) dn2cpp_string_to_utf8(p, s.data(), n);
    return s;
}

Dn2CppMappedFile dn2cpp_mmap_create_from_file(Dn2CppString* path, Dn2CppString* mapName,
                                              int32_t fileMode, int32_t access, int64_t capacity)
{
    // Carve-outs: a named map needs POSIX shm; only Read/ReadWrite access and the
    // Open/OpenOrCreate file modes are modeled here.
    if (mapName != nullptr)
        dn2cpp_throw_of(&dn2cpp_not_supported_exception_type);
    if (access != 0 && access != 1)
        dn2cpp_throw_of(&dn2cpp_not_supported_exception_type);
    if (fileMode != 3 && fileMode != 4) // Open=3, OpenOrCreate=4
        dn2cpp_throw_of(&dn2cpp_not_supported_exception_type);

    std::string p = dn2cpp_mmap_path_utf8(path);
    int oflag = (access == 1) ? O_RDONLY : O_RDWR;
    if (fileMode == 4) oflag |= O_CREAT; // OpenOrCreate
    int fd = ::open(p.c_str(), oflag, 0666);
    if (fd < 0)
        dn2cpp_throw_of(&dn2cpp_file_not_found_exception_type);

    struct stat st;
    if (::fstat(fd, &st) != 0)
    {
        ::close(fd);
        dn2cpp_throw_of(&dn2cpp_io_exception_type);
    }
    int64_t len = static_cast<int64_t>(st.st_size);

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

    Dn2CppMappedFile f;
    f.fd = fd;
    f.access = access;
    f.length = len;
    return f;
}

void dn2cpp_mmap_file_dispose(Dn2CppMappedFile f)
{
    if (f.fd >= 0) ::close(f.fd);
}

Dn2CppMappedView dn2cpp_mmap_create_view(Dn2CppMappedFile f, int64_t offset, int64_t size, int32_t access)
{
    if (access != 0 && access != 1)
        dn2cpp_throw_of(&dn2cpp_not_supported_exception_type);
    if (offset < 0 || size < 0)
        dn2cpp_throw_of(&dn2cpp_argument_out_of_range_exception_type);

    int64_t viewSize = (size == 0) ? (f.length - offset) : size; // 0 => rest of file
    if (viewSize < 0 || offset + viewSize > f.length)
        dn2cpp_throw_of(&dn2cpp_argument_out_of_range_exception_type);

    // mmap requires a page-aligned file offset; map from the aligned offset and
    // expose the user's offset as `addr` (aligned base + the intra-page delta).
    long pageSize = ::sysconf(_SC_PAGESIZE);
    if (pageSize <= 0) pageSize = 4096;
    int64_t alignedOffset = offset - (offset % pageSize);
    int64_t delta = offset - alignedOffset;
    size_t mapLen = static_cast<size_t>(viewSize + delta);
    if (mapLen == 0) mapLen = 1; // mmap rejects a zero length

    int prot = (access == 1) ? PROT_READ : (PROT_READ | PROT_WRITE);
    void* m = ::mmap(nullptr, mapLen, prot, MAP_SHARED, f.fd, static_cast<off_t>(alignedOffset));
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
    if (pos < 0) dn2cpp_throw_of(&dn2cpp_argument_out_of_range_exception_type);
    int64_t avail = v.capacity - pos;
    if (avail < 0) avail = 0;
    int64_t maxElems = avail / elemSize;
    int32_t n = (static_cast<int64_t>(count) <= maxElems) ? count : static_cast<int32_t>(maxElems);
    if (n > 0) std::memcpy(dst, v.addr + pos, static_cast<size_t>(n) * static_cast<size_t>(elemSize));
    return n;
}

void dn2cpp_mmap_write_from(Dn2CppMappedView v, int64_t pos, const void* src, int32_t count, int32_t elemSize)
{
    if (pos < 0 || (pos + static_cast<int64_t>(count) * elemSize) > v.capacity)
        dn2cpp_throw_of(&dn2cpp_argument_out_of_range_exception_type);
    if (count > 0) std::memcpy(v.addr + pos, src, static_cast<size_t>(count) * static_cast<size_t>(elemSize));
}

// dn2cpp_mmap_windows.cpp — file-backed System.IO.MemoryMappedFiles subset.
//
// Mirrors platform/posix/dn2cpp_mmap_posix.cpp's contract (see the struct comments in
// dn2cpp_core.h), except where Windows .NET itself differs from Unix .NET, and uses
// Win32 + Microsoft CRT calls instead of POSIX mmap:
// CreateFromFile opens the file via the CRT (`_open`, tracked as the same int32_t
// fd the cross-platform Dn2CppMappedFile carries) and sizes it via `_filelengthi64`/
// `_chsize_s`; CreateViewAccessor maps a (allocation-granularity-aligned) range via
// CreateFileMappingA + MapViewOfFile; Flush maps to FlushViewOfFile; Dispose maps to
// UnmapViewOfFile / `_close`. The read/write element-copy helpers touch only the
// mapped bytes (no OS call), so they are byte-identical to the POSIX file.
//
// No extra handle registry is needed despite Dn2CppMappedFile/Dn2CppMappedView
// having no spare field for the CreateFileMappingA HANDLE: per Win32 docs, once
// MapViewOfFile has succeeded the mapping-object handle may be closed immediately
// (the OS keeps the underlying section alive as long as a view of it remains
// mapped) — so it is opened and closed within dn2cpp_mmap_create_view itself, and
// UnmapViewOfFile/FlushViewOfFile only ever need the view's base address, never
// that handle. The CRT fd (and the file HANDLE obtained from it via
// `_get_osfhandle`, looked up only at mapping time) is the only handle that
// outlives a single call, and it already has a home: Dn2CppMappedFile.fd.
//
// Named maps / cross-process / CreateNew / non-null mapName / CreateViewStream
// are carve-outs (loud NotSupportedException), same as the POSIX file.
// Semantics probed against real .NET on this target — see
// gates/build-and-run-mmap-file.sh.

#include "dn2cpp_core.h"

#include <windows.h>

#include <string>   // managed path strings as NUL-terminated UTF-8 std::string
#include <cstring>  // std::memcpy
#include <cerrno>   // EACCES from _sopen_s
#include <fcntl.h>  // _O_RDONLY / _O_RDWR / _O_CREAT / _O_BINARY
#include <io.h>     // _sopen_s / _close / _chsize_s / _filelengthi64 / _get_osfhandle
#include <share.h>  // _SH_DENYNO
#include <sys/stat.h> // _S_IREAD / _S_IWRITE

// A managed path string as a NUL-terminated UTF-8 std::string (file-local; mirrors
// the helper in dn2cpp_system_io.cpp / the POSIX mmap file).
static std::string dn2cpp_mmap_path_utf8(Dn2CppString* p)
{
    if (p == nullptr) dn2cpp_throw_argument_null_param("path");
    int32_t n = dn2cpp_string_to_utf8(p, nullptr, 0);
    std::string s(static_cast<size_t>(n), '\0');
    if (n > 0) dn2cpp_string_to_utf8(p, s.data(), n);
    return s;
}

// Win32's MapViewOfFile requires the mapping offset to be a multiple of the
// system's allocation granularity (typically 64 KiB — coarser than its 4 KiB page
// size), not merely page-aligned like POSIX mmap. Queried once and cached, like
// the POSIX file caches sysconf(_SC_PAGESIZE) per call (this is cheaper still).
static int64_t dn2cpp_mmap_allocation_granularity()
{
    static const int64_t granularity = [] {
        SYSTEM_INFO si;
        ::GetSystemInfo(&si);
        return si.dwAllocationGranularity > 0
            ? static_cast<int64_t>(si.dwAllocationGranularity)
            : static_cast<int64_t>(65536);
    }();
    return granularity;
}

Dn2CppMappedFile* dn2cpp_mmap_create_from_file(Dn2CppString* path, Dn2CppString* mapName,
                                              int32_t fileMode, int32_t access, int64_t capacity)
{
    dn2cpp_mmap_validate_path_create(path, fileMode, mapName, capacity, access);
    // Named sections are outside the file-backed subset on Windows.
    if (mapName != nullptr)
        dn2cpp_throw_of(&dn2cpp_not_supported_exception_type);
    if (access != 0 && access != 1)
        dn2cpp_throw_of(&dn2cpp_not_supported_exception_type);
    if (fileMode != 3 && fileMode != 4) // Open=3, OpenOrCreate=4
        dn2cpp_throw_of(&dn2cpp_not_supported_exception_type);

    path = dn2cpp_path_get_full_path(path);
    std::string p = dn2cpp_mmap_path_utf8(path);
    auto* f = dn2cpp_mmap_file_new();
    int oflag = _O_BINARY | ((access == 1) ? _O_RDONLY : _O_RDWR);
    if (fileMode == 4) oflag |= _O_CREAT; // OpenOrCreate
    bool existed = ::_access(p.c_str(), 0) == 0;
    int fd = -1;
    errno_t opened = ::_sopen_s(&fd, p.c_str(), oflag, _SH_DENYNO, _S_IREAD | _S_IWRITE);
    if (fd < 0)
    {
        dn2cpp_file_throw_open_failure(static_cast<int>(opened), path);
    }

    int64_t len = ::_filelengthi64(fd);
    if (len < 0)
    {
        ::_close(fd);
        dn2cpp_throw_of(&dn2cpp_io_exception_type);
    }
    try
    {
        dn2cpp_mmap_validate_capacity(len, capacity, access, 0);
    }
    catch (...)
    {
        ::_close(fd);
        if (!existed)
            ::_unlink(p.c_str());
        throw;
    }

    // A ReadWrite map with an explicit capacity larger than the file grows the file
    // (the backing range must exist for the mapping). Read access cannot grow it.
    if (access == 0 && capacity > len)
    {
        if (::_chsize_s(fd, capacity) != 0)
        {
            ::_close(fd);
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
    if (fd >= 0) ::_close(fd);
    dn2cpp_mmap_dispose_source(f);
    dn2cpp_gc_suppress_finalize(f);
}

Dn2CppMappedFile* dn2cpp_mmap_create_from_handle(intptr_t handle, Dn2CppString* mapName, int32_t access,
                                                int64_t capacity, int32_t inheritability,
                                                int64_t validationLength, int32_t hasValidationLength)
{
    HANDLE source = reinterpret_cast<HANDLE>(handle);
    LARGE_INTEGER size;
    if (!::GetFileSizeEx(source, &size)) dn2cpp_throw_of(&dn2cpp_io_exception_type);
    int64_t length = size.QuadPart;
    int64_t checkedLength = hasValidationLength ? validationLength : length;
    dn2cpp_mmap_validate_capacity(checkedLength,
        capacity, access, inheritability);
    if (mapName != nullptr)
        dn2cpp_throw_of(&dn2cpp_not_supported_exception_type);
    int64_t mapLength = hasValidationLength
        ? (capacity == 0 ? validationLength : capacity)
        : (capacity > length ? capacity : length);
    auto* f = dn2cpp_mmap_file_new();
    HANDLE copy;
    if (!::DuplicateHandle(::GetCurrentProcess(), source, ::GetCurrentProcess(),
        &copy, 0, inheritability != 0, DUPLICATE_SAME_ACCESS))
        dn2cpp_throw_of(&dn2cpp_io_exception_type);
    int fd = ::_open_osfhandle(reinterpret_cast<intptr_t>(copy),
        _O_BINARY | (access == 1 ? _O_RDONLY : _O_RDWR));
    if (fd < 0)
    {
        ::CloseHandle(copy);
        dn2cpp_throw_of(&dn2cpp_io_exception_type);
    }
    f->fd = fd;
    FILE_END_OF_FILE_INFO end;
    end.EndOfFile.QuadPart = mapLength;
    // Duplicated handles share the stream's file position; resize without seeking.
    if (mapLength > length
        && !::SetFileInformationByHandle(copy, FileEndOfFileInfo, &end, sizeof(end)))
    {
        dn2cpp_mmap_file_dispose(f);
        dn2cpp_throw_of(&dn2cpp_io_exception_type);
    }
    f->access = access;
    f->length = mapLength;
    return f;
}

// A mapping the OS refused, raised as .NET's Win32Marshal raises it: Windows refuses
// a view past the section, or a write view of a read-only one, with
// ERROR_ACCESS_DENIED, which is UnauthorizedAccessException; the other errors these
// calls return are IOException.
[[noreturn]] static void dn2cpp_mmap_throw_win32(DWORD err)
{
    if (err == ERROR_ACCESS_DENIED)
        dn2cpp_throw_sr0(&dn2cpp_unauthorized_access_exception_type, DN2CPP_SR_MMF_IO_DENIED);
    dn2cpp_throw_of(&dn2cpp_io_exception_type);
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

    // Windows .NET checks no range itself: it widens the view down to the allocation
    // granularity MapViewOfFile requires and lets that call refuse a range past the
    // section. A size of 0 maps to the section's end.
    int64_t extra = offset % dn2cpp_mmap_allocation_granularity();
    int64_t alignedOffset = offset - extra;
    uint64_t mapSize = size != 0 ? static_cast<uint64_t>(size) + static_cast<uint64_t>(extra) : 0;
    if (sizeof(void*) == 4 && mapSize > 0xFFFFFFFFull)
        dn2cpp_throw_argument_out_of_range_param(DN2CPP_SR_MMF_SIZE_PAST_ADDRESS_SPACE, "size");
    MEMORYSTATUSEX memory;
    memory.dwLength = static_cast<DWORD>(sizeof(memory));
    if (::GlobalMemoryStatusEx(&memory) && mapSize >= memory.ullTotalVirtual)
        dn2cpp_throw_sr0(&dn2cpp_io_exception_type, DN2CPP_SR_MMF_NOT_ENOUGH_MEMORY);

    Dn2CppMonitorGuard guard(f->sync);
    if (f->fd < 0) dn2cpp_throw_object_disposed();
    if (access != 0 && access != 1)
        dn2cpp_throw_of(&dn2cpp_not_supported_exception_type);
    if (f->access == 1 && access == 0)
        dn2cpp_mmap_throw_win32(ERROR_ACCESS_DENIED);

    HANDLE hFile = reinterpret_cast<HANDLE>(::_get_osfhandle(f->fd));
    if (hFile == INVALID_HANDLE_VALUE)
        dn2cpp_throw_of(&dn2cpp_io_exception_type);

    DWORD protect = (access == 1) ? PAGE_READONLY : PAGE_READWRITE;
    // The section is the map's length, as .NET's section is the map's capacity (a
    // ReadWrite map grew the file to it), so MapViewOfFile refuses the same ranges.
    HANDLE hMap = ::CreateFileMappingA(hFile, nullptr, protect,
        static_cast<DWORD>(static_cast<uint64_t>(f->length) >> 32),
        static_cast<DWORD>(static_cast<uint64_t>(f->length) & 0xFFFFFFFFu), nullptr);
    if (hMap == nullptr)
        dn2cpp_mmap_throw_win32(::GetLastError());

    DWORD desiredAccess = (access == 1) ? FILE_MAP_READ : FILE_MAP_WRITE; // WRITE implies READ
    void* m = ::MapViewOfFile(hMap, desiredAccess,
        static_cast<DWORD>(static_cast<uint64_t>(alignedOffset) >> 32),
        static_cast<DWORD>(static_cast<uint64_t>(alignedOffset) & 0xFFFFFFFFu),
        static_cast<SIZE_T>(mapSize));
    DWORD mapError = ::GetLastError();
    // Safe to close the mapping-object handle now regardless of outcome: once a
    // view is mapped, Windows keeps the underlying section alive on its own until
    // the last view of it is unmapped (see the file header comment).
    ::CloseHandle(hMap);
    if (m == nullptr)
        dn2cpp_mmap_throw_win32(mapError);

    // A view of the section's rest reports what VirtualQuery finds, as .NET's does: a
    // mapped view occupies whole pages, so a short file's last page reads back as
    // zero-filled capacity, and an offset past that page leaves a negative capacity,
    // which the accessor .NET builds on the view refuses.
    int64_t capacity = size;
    int64_t mapLen = static_cast<int64_t>(mapSize);
    if (size == 0)
    {
        MEMORY_BASIC_INFORMATION mbi;
        if (::VirtualQuery(m, &mbi, sizeof(mbi)) == 0)
        {
            ::UnmapViewOfFile(m);
            dn2cpp_throw_of(&dn2cpp_io_exception_type);
        }
        mapLen = static_cast<int64_t>(mbi.RegionSize);
        capacity = mapLen - extra;
        if (capacity < 0)
        {
            ::UnmapViewOfFile(m);
            dn2cpp_throw_argument_out_of_range_bound(DN2CPP_SR_MUST_BE_NON_NEGATIVE, "capacity",
                capacity, 0, 8);
        }
    }

    Dn2CppMappedView v;
    v.mapBase = static_cast<uint8_t*>(m);
    v.mapLen = mapLen;
    v.addr = v.mapBase + extra;
    v.capacity = capacity;
    v.access = access;
    return v;
}

void dn2cpp_mmap_view_flush(Dn2CppMappedView v)
{
    if (v.mapBase != nullptr && v.mapLen > 0)
        ::FlushViewOfFile(v.mapBase, static_cast<SIZE_T>(v.mapLen));
}

void dn2cpp_mmap_view_dispose(Dn2CppMappedView v)
{
    if (v.mapBase != nullptr && v.mapLen > 0)
        ::UnmapViewOfFile(v.mapBase);
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

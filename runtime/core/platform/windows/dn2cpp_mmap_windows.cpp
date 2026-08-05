// dn2cpp_mmap_windows.cpp — file-backed System.IO.MemoryMappedFiles subset.
//
// Mirrors platform/posix/dn2cpp_mmap_posix.cpp's contract exactly (see the struct
// comments in dn2cpp.h) using Win32 + Microsoft CRT calls instead of POSIX mmap:
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
#include <fcntl.h>  // _O_RDONLY / _O_RDWR / _O_CREAT / _O_BINARY
#include <io.h>     // _sopen_s / _close / _chsize_s / _filelengthi64 / _get_osfhandle
#include <share.h>  // _SH_DENYNO
#include <sys/stat.h> // _S_IREAD / _S_IWRITE

// A managed path string as a NUL-terminated UTF-8 std::string (file-local; mirrors
// the helper in dn2cpp_system_io.cpp / the POSIX mmap file).
static std::string dn2cpp_mmap_path_utf8(Dn2CppString* p)
{
    if (p == nullptr) dn2cpp_throw_argument_null();
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

Dn2CppMappedFile dn2cpp_mmap_create_from_file(Dn2CppString* path, Dn2CppString* mapName,
                                              int32_t fileMode, int32_t access, int64_t capacity)
{
    // Carve-outs: a named map needs a real Win32 named section; only Read/ReadWrite
    // access and the Open/OpenOrCreate file modes are modeled here.
    if (mapName != nullptr)
        dn2cpp_throw_of(&dn2cpp_not_supported_exception_type);
    if (access != 0 && access != 1)
        dn2cpp_throw_of(&dn2cpp_not_supported_exception_type);
    if (fileMode != 3 && fileMode != 4) // Open=3, OpenOrCreate=4
        dn2cpp_throw_of(&dn2cpp_not_supported_exception_type);

    std::string p = dn2cpp_mmap_path_utf8(path);
    int oflag = _O_BINARY | ((access == 1) ? _O_RDONLY : _O_RDWR);
    if (fileMode == 4) oflag |= _O_CREAT; // OpenOrCreate
    int fd = -1;
    ::_sopen_s(&fd, p.c_str(), oflag, _SH_DENYNO, _S_IREAD | _S_IWRITE);
    if (fd < 0)
        dn2cpp_throw_of(&dn2cpp_file_not_found_exception_type);

    int64_t len = ::_filelengthi64(fd);
    if (len < 0)
    {
        ::_close(fd);
        dn2cpp_throw_of(&dn2cpp_io_exception_type);
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

    Dn2CppMappedFile f;
    f.fd = fd;
    f.access = access;
    f.length = len;
    return f;
}

void dn2cpp_mmap_file_dispose(Dn2CppMappedFile f)
{
    if (f.fd >= 0) ::_close(f.fd);
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

    // MapViewOfFile requires an allocation-granularity-aligned file offset; map
    // from the aligned offset and expose the user's offset as `addr` (aligned base
    // + the intra-page delta) — same shape as the POSIX file's page alignment.
    int64_t granularity = dn2cpp_mmap_allocation_granularity();
    int64_t alignedOffset = offset - (offset % granularity);
    int64_t delta = offset - alignedOffset;
    int64_t mapLen64 = viewSize + delta;
    if (mapLen64 == 0) mapLen64 = 1; // avoid the "0 means map-to-EOF" special case below
    size_t mapLen = static_cast<size_t>(mapLen64);

    HANDLE hFile = reinterpret_cast<HANDLE>(::_get_osfhandle(f.fd));
    if (hFile == INVALID_HANDLE_VALUE)
        dn2cpp_throw_of(&dn2cpp_io_exception_type);

    DWORD protect = (access == 1) ? PAGE_READONLY : PAGE_READWRITE;
    // maxSize 0,0 => the mapping object's size tracks the file's current size,
    // which offset+viewSize <= f.length above already guarantees covers this view.
    HANDLE hMap = ::CreateFileMappingA(hFile, nullptr, protect, 0, 0, nullptr);
    if (hMap == nullptr)
        dn2cpp_throw_of(&dn2cpp_io_exception_type);

    DWORD desiredAccess = (access == 1) ? FILE_MAP_READ : FILE_MAP_WRITE; // WRITE implies READ
    void* m = ::MapViewOfFile(hMap, desiredAccess,
        static_cast<DWORD>(static_cast<uint64_t>(alignedOffset) >> 32),
        static_cast<DWORD>(static_cast<uint64_t>(alignedOffset) & 0xFFFFFFFFu),
        mapLen);
    // Safe to close the mapping-object handle now regardless of outcome: once a
    // view is mapped, Windows keeps the underlying section alive on its own until
    // the last view of it is unmapped (see the file header comment).
    ::CloseHandle(hMap);
    if (m == nullptr)
        dn2cpp_throw_of(&dn2cpp_io_exception_type);

    // Real .NET's Windows MemoryMappedView.CreateView has a platform-specific twist
    // here that the POSIX path does not: when the caller asked to map "the rest of
    // the file" (size == 0), it does not report the exact file-length remainder as
    // Capacity — it re-queries the OS for the view's actual committed region via
    // VirtualQuery and reports THAT (minus the intra-page delta) instead, because a
    // mapped view always occupies whole pages, so a short file's last partial page
    // still reads back as a full page of (zero-filled) capacity. An explicit,
    // caller-specified size is trusted as-is and is not re-queried this way — this
    // matches CreateFromFile/CreateViewAccessor as verified against the real
    // win-x64 CoreLib installed on this machine (see gates/build-and-run-mmap-file.sh).
    int64_t reportedCapacity = viewSize;
    if (size == 0)
    {
        MEMORY_BASIC_INFORMATION mbi;
        if (::VirtualQuery(m, &mbi, sizeof(mbi)) != 0)
            reportedCapacity = static_cast<int64_t>(mbi.RegionSize) - delta;
    }

    Dn2CppMappedView v;
    v.mapBase = static_cast<uint8_t*>(m);
    v.mapLen = static_cast<int64_t>(mapLen);
    v.addr = v.mapBase + delta;
    v.capacity = reportedCapacity;
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

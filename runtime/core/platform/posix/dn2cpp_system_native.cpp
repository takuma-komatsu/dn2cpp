// dn2cpp_system_native.cpp — self-contained implementation of the .NET PAL
// (libSystem.Native) surface the transpiled real-BCL file-I/O code calls.
//
// The real CoreLib's System.IO stack (File/FileStream/RandomAccess/SafeFileHandle)
// bottoms out in `[DllImport("libSystem.Native")]` P/Invokes — thin POSIX wrappers
// that .NET ships as a native library next to the runtime. dn2cpp lowers those
// P/Invokes to direct native calls (Compilation.IsRuntimeProvidedPInvokeModule)
// and provides the SystemNative_* symbols here, so a transpiled binary stays
// self-contained instead of linking the dotnet install's libSystem.Native.
//
// The ABI (names, signatures, struct layouts, PAL constant values) mirrors the
// managed declarations in System.Private.CoreLib's Interop.Sys — the caller is
// the transpiled managed marshalling stub, so the managed side of the contract
// is authoritative. Values verified against net10.0 via reflection:
//   - FileStatus: sequential {i32,i32,u32,u32, 13×i64…, u32} (120 bytes padded)
//   - OpenFlags: PAL-stable bits (O_CREAT=0x20, …) converted to host O_* here
//   - Error: PAL codes 0x1XXXX (E2BIG=0x10001, …); unknown → ENONSTANDARD
//   - Passwd: sequential {ptr,ptr,u32,u32,ptr,ptr,ptr} (48 bytes on LP64)
//   - Signals: PAL-stable values (SIGKILL=9, SIGSTOP=19) converted to host here
//   - AccessMode: PAL values (F_OK=0, X_OK=1, W_OK=2, R_OK=4) equal the host's
//     on every platform built here — asserted at the site, passed through raw
// Only the members a reachable closure needs are implemented; a new gap shows
// up as a link error naming the missing SystemNative_* symbol (loud, not silent).
//
// macOS / Linux / BSD. Not part of the wasm build (no P/Invoke on that target).

#include "dn2cpp_core.h" // dn2cpp_gc_kernel_write_unsafe (the incremental-GC bounce)

#include <cerrno>
#include <csignal>
#include <cstddef>
#include <cstdint>
#include <cstdio>
#include <cstdlib>
#include <cstring>
#include <ctime>
#include <dirent.h>
#include <fcntl.h>
#include <pthread.h> // the LowLevelMonitor section (CoreLib's own mutex+condvar)
#include <pwd.h>
#include <sys/file.h>
#include <sys/stat.h>
#include <sys/time.h>
#include <sys/types.h>
#include <unistd.h>

#if defined(__APPLE__)
#include <copyfile.h>
#include <objc/message.h> // objc_msgSend — the SearchPath section's Foundation sends
#include <objc/runtime.h> // objc_getClass / sel_registerName
#include <sys/attr.h>
#include <sys/mount.h>
#include <sys/param.h>

// The autorelease-pool ABI clang emits for @autoreleasepool. Stable exported
// libobjc surface since 10.7, but not declared in the public objc/ headers, so
// declared here (see the SearchPath section for why a pool is needed at all).
extern "C" void* objc_autoreleasePoolPush(void);
extern "C" void objc_autoreleasePoolPop(void* pool);
#else
#include <sys/sendfile.h>
#include <sys/statfs.h>
#include <sys/time.h>
#endif

// ================ kernel writes into a managed buffer ======================
//
// (C++ linkage: this block sits OUTSIDE the extern "C" wrapper below.)
//
// Boehm's incremental collector keeps the heap write-protected: it recovers its
// dirty bits from a fault handler. A USER-SPACE store into a protected page
// faults, the handler unprotects, the store retries — invisible. A *kernel* store
// does not reach that handler: read(2)/pread(2) whose destination is a managed
// byte[] returns EFAULT ("Bad address") instead of filling it.
//
// The Godot lane runs incremental by DEFAULT (dn2cpp_gc_set_incremental_default),
// so every FileStream.Read in a game hits this; the console lane is
// stop-the-world. Allocating the buffer atomically does not help either — bdwgc
// skips pointer-free blocks only when a page is exactly one heap block, and on
// 16 KB-page arm64 it protects the whole heap.
//
// So land the transfer in memory the collector never touches, then copy into the
// managed buffer with an ordinary store — which faults, is handled, and retries.
// dn2cpp_gc_kernel_write_unsafe answers 0 whenever protection is impossible
// (stop-the-world, a dirty-bit strategy that needs no mprotect, or a pointer off
// the GC heap), so nothing here costs anything outside the one configuration that
// needs it.
//
// ONLY syscalls the kernel writes THROUGH are wrapped. A kernel *read* out of a
// managed buffer — write, pwrite, pwritev, copyfile, utimens — is unaffected: the
// protected pages keep PROT_READ. Anything added later that fills a caller buffer
// (a socket recv, an ioctl) belongs on this list.
namespace
{
thread_local uint8_t* g_bounce = nullptr;
thread_local size_t g_bounce_cap = 0;

// Per-thread, grow-only, never freed (process-lifetime, like the runtime's other
// pools — and raw realloc rather than a container, so no thread_local destructor
// has to survive the runtime's _Exit-without-unwinding shutdown).
uint8_t* bounce_scratch(size_t n)
{
    if (g_bounce_cap < n)
    {
        auto* p = static_cast<uint8_t*>(std::realloc(g_bounce, n));
        if (p == nullptr)
            return nullptr;
        g_bounce = p;
        g_bounce_cap = n;
    }
    return g_bounce;
}

// Run one read-like syscall filling `buffer`, bouncing when the collector may
// have protected it. `io(dst, count)` performs a SINGLE syscall (including its own
// EINTR retry) and returns its result; the scratch is sized to the whole request
// rather than chunked, because read(2) is allowed to return short and FileStream
// and Console are written for that — a second syscall to top up a fixed-size
// scratch could block where .NET returns.
template <typename Io>
ssize_t bounced_read(void* buffer, size_t count, Io io)
{
    if (count == 0 || dn2cpp_gc_kernel_write_unsafe(buffer) == 0)
        return io(buffer, count);
    uint8_t* scratch = bounce_scratch(count);
    if (scratch == nullptr)
    {
        errno = ENOMEM; // loud: better than a silent short read
        return -1;
    }
    ssize_t n = io(scratch, count);
    if (n > 0)
        std::memcpy(buffer, scratch, (size_t)n);
    return n; // errno preserved on the n < 0 path
}
} // namespace

extern "C" {

// ============================ PAL error codes ==============================
// Interop.Error (managed) values: 0x1XXXX per errno, ENONSTANDARD for an errno
// with no PAL equivalent (the managed ErrorInfo keeps the raw errno alongside,
// so StrError / round-trips still work).

#define DN2CPP_PAL_ENONSTANDARD 0x1FFFF

int32_t SystemNative_ConvertErrorPlatformToPal(int32_t platformErrno)
{
    switch (platformErrno)
    {
        case 0: return 0x0;
        case E2BIG: return 0x10001;
        case EACCES: return 0x10002;
        case EADDRINUSE: return 0x10003;
        case EADDRNOTAVAIL: return 0x10004;
        case EAFNOSUPPORT: return 0x10005;
        case EWOULDBLOCK: return 0x10006; // == EAGAIN
        case EALREADY: return 0x10007;
        case EBADF: return 0x10008;
        case EBADMSG: return 0x10009;
        case EBUSY: return 0x1000A;
        case ECANCELED: return 0x1000B;
        case ECHILD: return 0x1000C;
        case ECONNABORTED: return 0x1000D;
        case ECONNREFUSED: return 0x1000E;
        case ECONNRESET: return 0x1000F;
        case EDEADLK: return 0x10010;
        case EDESTADDRREQ: return 0x10011;
        case EDOM: return 0x10012;
        case EDQUOT: return 0x10013;
        case EEXIST: return 0x10014;
        case EFAULT: return 0x10015;
        case EFBIG: return 0x10016;
        case EHOSTUNREACH: return 0x10017;
        case EIDRM: return 0x10018;
        case EILSEQ: return 0x10019;
        case EINPROGRESS: return 0x1001A;
        case EINTR: return 0x1001B;
        case EINVAL: return 0x1001C;
        case EIO: return 0x1001D;
        case EISCONN: return 0x1001E;
        case EISDIR: return 0x1001F;
        case ELOOP: return 0x10020;
        case EMFILE: return 0x10021;
        case EMLINK: return 0x10022;
        case EMSGSIZE: return 0x10023;
        case EMULTIHOP: return 0x10024;
        case ENAMETOOLONG: return 0x10025;
        case ENETDOWN: return 0x10026;
        case ENETRESET: return 0x10027;
        case ENETUNREACH: return 0x10028;
        case ENFILE: return 0x10029;
        case ENOBUFS: return 0x1002A;
        case ENODEV: return 0x1002C;
        case ENOENT: return 0x1002D;
        case ENOEXEC: return 0x1002E;
        case ENOLCK: return 0x1002F;
        case ENOLINK: return 0x10030;
        case ENOMEM: return 0x10031;
        case ENOMSG: return 0x10032;
        case ENOPROTOOPT: return 0x10033;
        case ENOSPC: return 0x10034;
        case ENOSYS: return 0x10037;
        case ENOTCONN: return 0x10038;
        case ENOTDIR: return 0x10039;
#if ENOTEMPTY != EEXIST // identical on some SysV-descended platforms
        case ENOTEMPTY: return 0x1003A;
#endif
#ifdef ENOTRECOVERABLE
        case ENOTRECOVERABLE: return 0x1003B;
#endif
        case ENOTSOCK: return 0x1003C;
        case ENOTSUP: return 0x1003D; // == EOPNOTSUPP
        case ENOTTY: return 0x1003E;
        case ENXIO: return 0x1003F;
        case EOVERFLOW: return 0x10040;
#ifdef EOWNERDEAD
        case EOWNERDEAD: return 0x10041;
#endif
        case EPERM: return 0x10042;
        case EPIPE: return 0x10043;
        case EPROTO: return 0x10044;
        case EPROTONOSUPPORT: return 0x10045;
        case EPROTOTYPE: return 0x10046;
        case ERANGE: return 0x10047;
        case EROFS: return 0x10048;
        case ESPIPE: return 0x10049;
        case ESRCH: return 0x1004A;
        case ESTALE: return 0x1004B;
        case ETIMEDOUT: return 0x1004D;
        case ETXTBSY: return 0x1004E;
        case EXDEV: return 0x1004F;
#ifdef ESOCKTNOSUPPORT
        case ESOCKTNOSUPPORT: return 0x1005E;
#endif
        case EPFNOSUPPORT: return 0x10060;
        case ESHUTDOWN: return 0x1006C;
        case EHOSTDOWN: return 0x10070;
#ifdef ENODATA
        case ENODATA: return 0x10071;
#endif
        default: return DN2CPP_PAL_ENONSTANDARD;
    }
}

int32_t SystemNative_ConvertErrorPalToPlatform(int32_t palError)
{
    switch (palError)
    {
        case 0x0: return 0;
        case 0x10001: return E2BIG;
        case 0x10002: return EACCES;
        case 0x10003: return EADDRINUSE;
        case 0x10004: return EADDRNOTAVAIL;
        case 0x10005: return EAFNOSUPPORT;
        case 0x10006: return EWOULDBLOCK;
        case 0x10007: return EALREADY;
        case 0x10008: return EBADF;
        case 0x10009: return EBADMSG;
        case 0x1000A: return EBUSY;
        case 0x1000B: return ECANCELED;
        case 0x1000C: return ECHILD;
        case 0x1000D: return ECONNABORTED;
        case 0x1000E: return ECONNREFUSED;
        case 0x1000F: return ECONNRESET;
        case 0x10010: return EDEADLK;
        case 0x10011: return EDESTADDRREQ;
        case 0x10012: return EDOM;
        case 0x10013: return EDQUOT;
        case 0x10014: return EEXIST;
        case 0x10015: return EFAULT;
        case 0x10016: return EFBIG;
        case 0x10017: return EHOSTUNREACH;
        case 0x10018: return EIDRM;
        case 0x10019: return EILSEQ;
        case 0x1001A: return EINPROGRESS;
        case 0x1001B: return EINTR;
        case 0x1001C: return EINVAL;
        case 0x1001D: return EIO;
        case 0x1001E: return EISCONN;
        case 0x1001F: return EISDIR;
        case 0x10020: return ELOOP;
        case 0x10021: return EMFILE;
        case 0x10022: return EMLINK;
        case 0x10023: return EMSGSIZE;
        case 0x10024: return EMULTIHOP;
        case 0x10025: return ENAMETOOLONG;
        case 0x10026: return ENETDOWN;
        case 0x10027: return ENETRESET;
        case 0x10028: return ENETUNREACH;
        case 0x10029: return ENFILE;
        case 0x1002A: return ENOBUFS;
        case 0x1002C: return ENODEV;
        case 0x1002D: return ENOENT;
        case 0x1002E: return ENOEXEC;
        case 0x1002F: return ENOLCK;
        case 0x10030: return ENOLINK;
        case 0x10031: return ENOMEM;
        case 0x10032: return ENOMSG;
        case 0x10033: return ENOPROTOOPT;
        case 0x10034: return ENOSPC;
        case 0x10037: return ENOSYS;
        case 0x10038: return ENOTCONN;
        case 0x10039: return ENOTDIR;
        case 0x1003A: return ENOTEMPTY;
#ifdef ENOTRECOVERABLE
        case 0x1003B: return ENOTRECOVERABLE;
#endif
        case 0x1003C: return ENOTSOCK;
        case 0x1003D: return ENOTSUP;
        case 0x1003E: return ENOTTY;
        case 0x1003F: return ENXIO;
        case 0x10040: return EOVERFLOW;
#ifdef EOWNERDEAD
        case 0x10041: return EOWNERDEAD;
#endif
        case 0x10042: return EPERM;
        case 0x10043: return EPIPE;
        case 0x10044: return EPROTO;
        case 0x10045: return EPROTONOSUPPORT;
        case 0x10046: return EPROTOTYPE;
        case 0x10047: return ERANGE;
        case 0x10048: return EROFS;
        case 0x10049: return ESPIPE;
        case 0x1004A: return ESRCH;
        case 0x1004B: return ESTALE;
        case 0x1004D: return ETIMEDOUT;
        case 0x1004E: return ETXTBSY;
        case 0x1004F: return EXDEV;
        default: return -1; // ENONSTANDARD / unknown: no platform equivalent
    }
}

int32_t SystemNative_GetErrNo(void)
{
    return errno;
}

void SystemNative_SetErrNo(int32_t error)
{
    errno = error;
}

// GetHostName(buffer, size) → 0 and fills the buffer with the machine's host
// name, or -1 on error. A thin gethostname(2) wrapper — the syscall the real
// System.Net.Dns.GetHostName's Interop.Sys.GetHostName bottoms out in; no
// network, just the local uname/hostname the kernel already holds. The managed
// caller supplies a 256-byte buffer and null-terminates the last byte itself.
int32_t SystemNative_GetHostName(uint8_t* name, int32_t nameLength)
{
    return gethostname(reinterpret_cast<char*>(name), static_cast<size_t>(nameLength));
}

// StrErrorR(platform errno, buffer, size) → the message (buffer, or a static
// string strerror_r chose not to copy). The managed caller treats a null return
// as "buffer too small" and retries with a larger one.
const char* SystemNative_StrErrorR(int32_t platformErrno, char* buffer, int32_t bufferSize)
{
#if !defined(__ANDROID__) && (defined(__APPLE__) || !defined(__GLIBC__) || \
    ((_POSIX_C_SOURCE >= 200112L) && !defined(_GNU_SOURCE)))
    // XSI strerror_r: fills the buffer, returns 0 / an error code. Bionic
    // (Android's libc) doesn't define __GLIBC__ either, but unlike macOS/BSD
    // it ships the GNU-style signature (returns char*), so it must be excluded
    // from this branch explicitly rather than inferred from __GLIBC__.
    int r = ::strerror_r(platformErrno, buffer, (size_t)bufferSize);
    if (r == 0 || r == ERANGE)
        return buffer; // ERANGE = truncated, still NUL-terminated
#else
    // GNU strerror_r: returns the message (the buffer or a static string).
    char* r = ::strerror_r(platformErrno, buffer, (size_t)bufferSize);
    if (r != nullptr)
        return r;
#endif
    std::snprintf(buffer, (size_t)bufferSize, "Unknown error %d", platformErrno);
    return buffer;
}

// ============================ memory =======================================

void* SystemNative_Malloc(uintptr_t size)
{
    return std::malloc(size == 0 ? 1 : size); // malloc(0) may return NULL; .NET expects a pointer
}

void SystemNative_Free(void* ptr)
{
    std::free(ptr);
}

// ============================ file descriptors =============================

// PAL OpenFlags (managed Interop.Sys.OpenFlags) → host O_* bits.
intptr_t SystemNative_Open(const char* path, int32_t flags, int32_t mode)
{
    int f;
    switch (flags & 0x3)
    {
        case 0: f = O_RDONLY; break;
        case 1: f = O_WRONLY; break;
        default: f = O_RDWR; break;
    }
    if (flags & 0x10) f |= O_CLOEXEC;
    if (flags & 0x20) f |= O_CREAT;
    if (flags & 0x40) f |= O_EXCL;
    if (flags & 0x80) f |= O_TRUNC;
    if (flags & 0x100) f |= O_SYNC;
    if (flags & 0x200) f |= O_NOFOLLOW;
    int fd;
    while ((fd = ::open(path, f, (mode_t)mode)) < 0 && errno == EINTR)
    {
    }
    return fd;
}

int32_t SystemNative_Close(intptr_t fd)
{
    // No EINTR retry: POSIX leaves the fd state unspecified after EINTR and
    // retrying can close a recycled descriptor (the real PAL doesn't retry either).
    return ::close((int)fd);
}

int32_t SystemNative_Read(intptr_t fd, void* buffer, int32_t bufferSize)
{
    if (bufferSize < 0)
        bufferSize = 0;
    return (int32_t)bounced_read(buffer, (size_t)bufferSize, [fd](void* dst, size_t cnt) {
        ssize_t n;
        while ((n = ::read((int)fd, dst, cnt)) < 0 && errno == EINTR)
        {
        }
        return n;
    });
}

int32_t SystemNative_Write(intptr_t fd, const void* buffer, int32_t bufferSize)
{
    ssize_t n;
    while ((n = ::write((int)fd, buffer, (size_t)bufferSize)) < 0 && errno == EINTR)
    {
    }
    return (int32_t)n;
}

int32_t SystemNative_PRead(intptr_t fd, void* buffer, int32_t bufferSize, int64_t fileOffset)
{
    if (bufferSize < 0)
        bufferSize = 0;
    return (int32_t)bounced_read(buffer, (size_t)bufferSize, [fd, fileOffset](void* dst, size_t cnt) {
        ssize_t n;
        while ((n = ::pread((int)fd, dst, cnt, (off_t)fileOffset)) < 0 && errno == EINTR)
        {
        }
        return n;
    });
}

int32_t SystemNative_PWrite(intptr_t fd, const void* buffer, int32_t bufferSize, int64_t fileOffset)
{
    ssize_t n;
    while ((n = ::pwrite((int)fd, buffer, (size_t)bufferSize, (off_t)fileOffset)) < 0
           && errno == EINTR)
    {
    }
    return (int32_t)n;
}

// One scatter/gather segment — layout-compatible with the managed
// Interop.Sys.IOVector { Base, Count } (which itself mirrors struct iovec).
// Reached from RandomAccess.ReadScatterAtOffset / WriteGatherAtOffset on the
// file-backed async Stream path (ThreadPoolValueTaskSource.Execute). Mirrors
// dotnet/runtime pal_io.c v10.0.1's no-preadv/pwritev emulation arm (a
// per-segment pread/pwrite loop, stopping at the first short transfer) —
// portable to every POSIX host regardless of preadv availability.
struct Dn2CppIOVector
{
    uint8_t* base;
    uintptr_t count;
};

int64_t SystemNative_PReadV(intptr_t fd, void* vectors, int32_t vectorCount, int64_t fileOffset)
{
    auto* vs = static_cast<Dn2CppIOVector*>(vectors);
    int64_t total = 0;
    for (int32_t i = 0; i < vectorCount; i++)
    {
        // Per SEGMENT: each iovec base is its own managed buffer (the scatter list
        // comes from RandomAccess.ReadScatterAtOffset), so each is asked separately.
        int64_t at = fileOffset + total;
        ssize_t n = bounced_read(vs[i].base, (size_t)vs[i].count, [fd, at](void* dst, size_t cnt) {
            ssize_t r;
            while ((r = ::pread((int)fd, dst, cnt, (off_t)at)) < 0 && errno == EINTR)
            {
            }
            return r;
        });
        if (n < 0)
            return total > 0 ? total : (int64_t)n;
        total += (int64_t)n;
        if ((uintptr_t)n != vs[i].count)
            break; // short read: EOF or partial transfer — stop here
    }
    return total;
}

int64_t SystemNative_PWriteV(intptr_t fd, void* vectors, int32_t vectorCount, int64_t fileOffset)
{
    auto* vs = static_cast<Dn2CppIOVector*>(vectors);
    int64_t total = 0;
    for (int32_t i = 0; i < vectorCount; i++)
    {
        ssize_t n;
        while ((n = ::pwrite((int)fd, vs[i].base, (size_t)vs[i].count,
                             (off_t)(fileOffset + total))) < 0
               && errno == EINTR)
        {
        }
        if (n < 0)
            return total > 0 ? total : (int64_t)n;
        total += (int64_t)n;
        if ((uintptr_t)n != vs[i].count)
            break; // short write: partial transfer — stop here
    }
    return total;
}

// PAL SeekWhence values equal the host SEEK_SET/SEEK_CUR/SEEK_END (0/1/2).
int64_t SystemNative_LSeek(intptr_t fd, int64_t offset, int32_t whence)
{
    return (int64_t)::lseek((int)fd, (off_t)offset, whence);
}

int32_t SystemNative_FSync(intptr_t fd)
{
    int r;
    while ((r = ::fsync((int)fd)) < 0 && errno == EINTR)
    {
    }
    return r;
}

int32_t SystemNative_FTruncate(intptr_t fd, int64_t length)
{
    int r;
    while ((r = ::ftruncate((int)fd, (off_t)length)) < 0 && errno == EINTR)
    {
    }
    return r;
}

// PAL LockOperations values equal the host LOCK_SH/EX/NB/UN (1/2/4/8).
int32_t SystemNative_FLock(intptr_t fd, int32_t operation)
{
    int r;
    while ((r = ::flock((int)fd, operation)) < 0 && errno == EINTR)
    {
    }
    return r;
}

// Advisory readahead hint; macOS has no posix_fadvise, and the managed caller
// only debug-asserts success, so "unsupported" reports clean success like the
// real PAL. Returns the error number directly (posix_fadvise convention).
int32_t SystemNative_PosixFAdvise(intptr_t fd, int64_t offset, int64_t length, int32_t advice)
{
#if defined(__APPLE__)
    (void)fd; (void)offset; (void)length; (void)advice;
    return 0;
#else
    int r;
    while ((r = ::posix_fadvise((int)fd, (off_t)offset, (off_t)length, advice)) == EINTR)
    {
    }
    return r;
#endif
}

// Preallocation for FileStream(preallocationSize:) / FileStreamOptions.PreallocationSize.
// Best-effort: the managed caller only surfaces ENOSPC/EFBIG and ignores any other failure.
//
// **Preallocation reserves BLOCKS. It must not change the file's LENGTH.** A FileStream
// created with a 64 KB preallocation hint has Length 0 until something writes to it. An
// arm that extends the size instead makes the file read back as zeros nobody wrote, which
// every length-driven reader (a copy loop, a CopyTo, a ReadAllBytes) then sees.
//
//   - macOS: F_PREALLOCATE with F_PEOFPOSMODE allocates past the physical end of file and
//     leaves st_size alone. No ftruncate may follow it.
//   - Linux: posix_fallocate is specified to grow the file to offset+len, so it cannot
//     serve a size-preserving hint. fallocate() with FALLOC_FL_KEEP_SIZE reserves without
//     growing, and is what the real .NET PAL uses.
//   - Anywhere else: no size-preserving primitive, so do nothing and report success. A
//     hint that is not honored is a hint; a hint that corrupts the length is a bug.
int32_t SystemNative_FAllocate(intptr_t fd, int64_t offset, int64_t length)
{
#if defined(__APPLE__)
    fstore_t store = {};
    store.fst_flags = F_ALLOCATECONTIG;
    store.fst_posmode = F_PEOFPOSMODE;
    store.fst_offset = (off_t)offset;
    store.fst_length = (off_t)length;
    int r = ::fcntl((int)fd, F_PREALLOCATE, &store);
    if (r == -1)
    {
        // Contiguous space was not available: retry allowing a fragmented allocation.
        store.fst_flags = F_ALLOCATEALL;
        r = ::fcntl((int)fd, F_PREALLOCATE, &store);
    }
    return r;
#elif defined(__linux__) && defined(FALLOC_FL_KEEP_SIZE)
    int r;
    while ((r = ::fallocate((int)fd, FALLOC_FL_KEEP_SIZE, (off_t)offset, (off_t)length)) < 0
           && errno == EINTR)
    {
    }
    return r;
#else
    (void)fd;
    (void)offset;
    (void)length;
    return 0;
#endif
}

// ============================ stat =========================================

// Managed Interop.Sys.FileStatus, sequential layout (net10.0, verified by
// reflection: 120 bytes with trailing pad). Mode is the RAW st_mode — the
// managed FileTypes constants (S_IFMT/S_IFREG/…) match the POSIX octal values.
struct Dn2CppPalFileStatus
{
    int32_t Flags; // 1 = HasBirthTime
    int32_t Mode;
    uint32_t Uid;
    uint32_t Gid;
    int64_t Size;
    int64_t ATime;
    int64_t ATimeNsec;
    int64_t MTime;
    int64_t MTimeNsec;
    int64_t CTime;
    int64_t CTimeNsec;
    int64_t BirthTime;
    int64_t BirthTimeNsec;
    int64_t Dev;
    int64_t RDev;
    int64_t Ino;
    uint32_t UserFlags; // 0x8000 = hidden (macOS UF_HIDDEN)
};

static void dn2cpp_pal_fill_status(const struct stat& st, Dn2CppPalFileStatus* out)
{
    out->Mode = (int32_t)st.st_mode;
    out->Uid = st.st_uid;
    out->Gid = st.st_gid;
    out->Size = (int64_t)st.st_size;
    out->Dev = (int64_t)st.st_dev;
    out->RDev = (int64_t)st.st_rdev;
    out->Ino = (int64_t)st.st_ino;
#if defined(__APPLE__)
    out->ATime = st.st_atimespec.tv_sec;
    out->ATimeNsec = st.st_atimespec.tv_nsec;
    out->MTime = st.st_mtimespec.tv_sec;
    out->MTimeNsec = st.st_mtimespec.tv_nsec;
    out->CTime = st.st_ctimespec.tv_sec;
    out->CTimeNsec = st.st_ctimespec.tv_nsec;
    out->BirthTime = st.st_birthtimespec.tv_sec;
    out->BirthTimeNsec = st.st_birthtimespec.tv_nsec;
    out->Flags = 1; // HasBirthTime
    out->UserFlags = ((st.st_flags & UF_HIDDEN) == UF_HIDDEN) ? 0x8000u : 0u;
#else
    out->ATime = st.st_atim.tv_sec;
    out->ATimeNsec = st.st_atim.tv_nsec;
    out->MTime = st.st_mtim.tv_sec;
    out->MTimeNsec = st.st_mtim.tv_nsec;
    out->CTime = st.st_ctim.tv_sec;
    out->CTimeNsec = st.st_ctim.tv_nsec;
    out->BirthTime = 0;
    out->BirthTimeNsec = 0;
    out->Flags = 0;
    out->UserFlags = 0;
#endif
}

int32_t SystemNative_Stat(const char* path, Dn2CppPalFileStatus* output)
{
    struct stat st;
    int r;
    while ((r = ::stat(path, &st)) < 0 && errno == EINTR)
    {
    }
    if (r == 0)
        dn2cpp_pal_fill_status(st, output);
    return r;
}

int32_t SystemNative_LStat(const char* path, Dn2CppPalFileStatus* output)
{
    struct stat st;
    int r;
    while ((r = ::lstat(path, &st)) < 0 && errno == EINTR)
    {
    }
    if (r == 0)
        dn2cpp_pal_fill_status(st, output);
    return r;
}

int32_t SystemNative_FStat(intptr_t fd, Dn2CppPalFileStatus* output)
{
    struct stat st;
    int r;
    while ((r = ::fstat((int)fd, &st)) < 0 && errno == EINTR)
    {
    }
    if (r == 0)
        dn2cpp_pal_fill_status(st, output);
    return r;
}

// ============================ paths / links ================================

int32_t SystemNative_Unlink(const char* path)
{
    int r;
    while ((r = ::unlink(path)) < 0 && errno == EINTR)
    {
    }
    return r;
}

int32_t SystemNative_Rename(const char* oldPath, const char* newPath)
{
    return ::rename(oldPath, newPath);
}

int32_t SystemNative_Link(const char* source, const char* linkTarget)
{
    int r;
    while ((r = ::link(source, linkTarget)) < 0 && errno == EINTR)
    {
    }
    return r;
}

// pal_io.c SystemNative_ReadLink: readlink(2) into `buffer`, returning the byte
// count written (readlink does NOT NUL-terminate — the managed side length-slices
// it) or -1 with errno set. `buffer` is a managed byte[] the collector may have
// protected, so it goes through bounced_read like the other read-like fills.
// Reached by TimeZoneInfo's local-zone discovery (resolving the /etc/localtime
// symlink) and by System.IO symlink APIs.
int32_t SystemNative_ReadLink(const char* path, char* buffer, int32_t bufferSize)
{
    ssize_t count = bounced_read(buffer, (size_t)bufferSize,
        [path](void* dst, size_t n) -> ssize_t { return ::readlink(path, static_cast<char*>(dst), n); });
    return (int32_t)count;
}

char* SystemNative_GetCwd(char* buffer, int32_t bufferSize)
{
    // The BCL's ≤255-char fast path passes a stackalloc buffer (safe); the fallback
    // rents a pooled managed byte[], which the collector may have protected.
    if (buffer != nullptr && bufferSize > 0 && dn2cpp_gc_kernel_write_unsafe(buffer) != 0)
    {
        uint8_t* scratch = bounce_scratch((size_t)bufferSize);
        if (scratch == nullptr)
        {
            errno = ENOMEM;
            return nullptr;
        }
        char* r = ::getcwd(reinterpret_cast<char*>(scratch), (size_t)bufferSize);
        if (r == nullptr)
            return nullptr; // errno set by getcwd (ERANGE: caller grows and retries)
        std::memcpy(buffer, scratch, std::strlen(r) + 1);
        return buffer; // the CALLER's buffer, never the scratch
    }
    return ::getcwd(buffer, (size_t)bufferSize);
}

// pal_io.c SystemNative_Access: access(2), 0 if the requested access is
// permitted, -1 with errno set otherwise. The managed AccessMode
// (Interop.Access.cs — verified against the shipping 10.0.9 metadata: F_OK=0,
// X_OK=1, W_OK=2, R_OK=4) equals the POSIX values on every platform this file
// builds for, so the mode passes through raw; pal_io.c asserts that identity
// rather than translating, reproduced below. SetLastError=true on the managed
// declaration is the LibraryImport-generated managed bracket around a raw
// SetLastError=false extern (same mechanism as at SystemNative_Kill), so errno
// must simply still be live on return — access(2) leaves it so. Reach path:
// Environment.GetFolderPath / SystemDirectory — GetFolderPathCore verifies the
// provider's answer with Access(path, R_OK) unless the caller passed
// SpecialFolderOption.DoNotVerify.
int32_t SystemNative_Access(const char* path, int32_t mode)
{
    static_assert(F_OK == 0, "PAL F_OK must equal the host value");
    static_assert(X_OK == 1, "PAL X_OK must equal the host value");
    static_assert(W_OK == 2, "PAL W_OK must equal the host value");
    static_assert(R_OK == 4, "PAL R_OK must equal the host value");
    return ::access(path, mode);
}

// ============================ directories ==================================
// Function-for-function reproduction of dotnet/runtime pal_io.c / pal_time.c
// (tag v10.0.1, src/native/libs/System.Native/) — the same style as
// dn2cpp_zlib_native.cpp reproducing pal_zlib.c.

// Managed Interop.Sys.DirectoryEntry (pal_io.h `DirectoryEntry`). Verified by
// reflection against net10.0 System.Private.CoreLib: LayoutKind.Sequential,
// Marshal.SizeOf = 16 —
//   Byte*    Name       @ offset 0   (pointer into the DIR stream's dirent)
//   Int32    NameLength @ offset 8   (-1 = managed side walks to the NUL)
//   NodeType InodeType  @ offset 12  (Int32 enum)
// The managed NodeType values (DT_UNKNOWN=0, DT_FIFO=1, DT_CHR=2, DT_DIR=4,
// DT_BLK=6, DT_REG=8, DT_LNK=10, DT_SOCK=12, DT_WHT=14) equal the BSD/Linux
// dirent d_type constants, so d_type passes through unconverted (pal_io.c
// does the same direct cast).
struct Dn2CppPalDirectoryEntry
{
    const char* Name;
    int32_t NameLength;
    int32_t InodeType;
};
static_assert(sizeof(Dn2CppPalDirectoryEntry) == 16, "managed DirectoryEntry is 16 bytes");
static_assert(offsetof(Dn2CppPalDirectoryEntry, NameLength) == 8, "NameLength at 8");
static_assert(offsetof(Dn2CppPalDirectoryEntry, InodeType) == 12, "InodeType at 12");

// pal_io.c ConvertDirent: hand back a pointer into the caller's dirent (the
// managed enumerator copies the name out before the next ReadDir).
static void dn2cpp_pal_convert_dirent(const struct dirent* entry, Dn2CppPalDirectoryEntry* outputEntry)
{
    outputEntry->Name = entry->d_name;
#if defined(DT_UNKNOWN)
    outputEntry->InodeType = (int32_t)entry->d_type;
#else
    // No d_type on this platform: report unknown, managed code stats the entry.
    outputEntry->InodeType = 0; // PAL_DT_UNKNOWN
#endif
#if defined(__APPLE__) || defined(__FreeBSD__) || defined(__NetBSD__) || defined(__OpenBSD__)
    // HAVE_DIRENT_NAME_LEN platforms expose the name length directly.
    outputEntry->NameLength = (int32_t)entry->d_namlen;
#else
    outputEntry->NameLength = -1; // sentinel: managed side walks to the first NUL
#endif
}

DIR* SystemNative_OpenDir(const char* path)
{
    DIR* result;
    // EINTR isn't documented, happens in practice on macOS (pal_io.c).
    while ((result = ::opendir(path)) == nullptr && errno == EINTR)
    {
    }
    return result;
}

// 0 = entry produced, -1 = end-of-stream, positive errno = failure. The caller
// must not call readdir/closedir on the same DIR until the entry is consumed;
// concurrent readdir on *different* DIRs is assumed safe (pal_io.c contract).
int32_t SystemNative_ReadDir(DIR* dir, Dn2CppPalDirectoryEntry* outputEntry)
{
    errno = 0;
    struct dirent* entry = ::readdir(dir);
    if (entry == nullptr)
    {
        std::memset(outputEntry, 0, sizeof(*outputEntry)); // managed out param must be initialized
        return errno != 0 ? errno : -1; // kernel-set errno -> failure; else end-of-stream
    }
    dn2cpp_pal_convert_dirent(entry, outputEntry);
    return 0;
}

int32_t SystemNative_CloseDir(DIR* dir)
{
    int32_t result = ::closedir(dir);
    // EINTR isn't documented, happens in practice on macOS (pal_io.c).
    if (result < 0 && errno == EINTR)
        result = 0;
    return result;
}

int32_t SystemNative_MkDir(const char* path, int32_t mode)
{
    int32_t result;
    while ((result = ::mkdir(path, (mode_t)mode)) < 0 && errno == EINTR)
    {
    }
    return result;
}

int32_t SystemNative_RmDir(const char* path)
{
    int32_t result;
    while ((result = ::rmdir(path)) < 0 && errno == EINTR)
    {
    }
    return result;
}

int32_t SystemNative_ChMod(const char* path, int32_t mode)
{
    int32_t result;
    while ((result = ::chmod(path, (mode_t)mode)) < 0 && errno == EINTR)
    {
    }
    return result;
}

int32_t SystemNative_FChMod(intptr_t fd, int32_t mode)
{
    int32_t result;
    while ((result = ::fchmod((int)fd, (mode_t)mode)) < 0 && errno == EINTR)
    {
    }
    return result;
}

// Managed Interop.Sys.TimeSpec (pal_time.h `TimeSpec`). Verified by reflection
// against net10.0 System.Private.CoreLib: LayoutKind.Sequential, 16 bytes —
//   Int64 TvSec  @ offset 0
//   Int64 TvNsec @ offset 8
// The UTimensat/FUTimens callers pass `TimeSpec[2]` = { atime, mtime }.
struct Dn2CppPalTimeSpec
{
    int64_t tv_sec;
    int64_t tv_nsec;
};
static_assert(sizeof(Dn2CppPalTimeSpec) == 16, "managed TimeSpec is 16 bytes");
static_assert(offsetof(Dn2CppPalTimeSpec, tv_nsec) == 8, "TvNsec at 8");

// pal_time.c SystemNative_UTimensat (HAVE_UTIMENSAT arm — macOS >= 10.13 and
// Linux both have utimensat; the lutimes/utimes fallback is not needed here).
int32_t SystemNative_UTimensat(const char* path, Dn2CppPalTimeSpec* times)
{
    struct timespec updatedTimes[2];
    updatedTimes[0].tv_sec = (time_t)times[0].tv_sec;
    updatedTimes[0].tv_nsec = (long)times[0].tv_nsec;
    updatedTimes[1].tv_sec = (time_t)times[1].tv_sec;
    updatedTimes[1].tv_nsec = (long)times[1].tv_nsec;
    int32_t result;
    while ((result = ::utimensat(AT_FDCWD, path, updatedTimes, AT_SYMLINK_NOFOLLOW)) < 0
           && errno == EINTR)
    {
    }
    return result;
}

// pal_io.c SystemNative_CanGetHiddenFlag / SystemNative_LChflagsCanSetHiddenFlag:
// whether the platform models a per-file hidden flag (macOS/BSD st_flags
// UF_HIDDEN; Linux has none). FileStatus caches LChflagsCanSetHiddenFlag in
// Interop.Sys's cctor and FileSystemEntry.get_IsHidden reads CanGetHiddenFlag.
int32_t SystemNative_CanGetHiddenFlag(void)
{
#if defined(__APPLE__) && defined(UF_HIDDEN)
    return 1;
#else
    return 0;
#endif
}

int32_t SystemNative_LChflagsCanSetHiddenFlag(void)
{
#if defined(__APPLE__) && defined(UF_HIDDEN)
    return SystemNative_CanGetHiddenFlag();
#else
    return 0;
#endif
}

// pal_io.c SystemNative_LChflags / SystemNative_FChflags: the SETTERS behind the flag the
// two predicates above advertise. FileSystem.SetAttributes on Unix routes
// FileAttributes.Hidden through them (and only them — no other attribute reaches BSD
// st_flags), by path for a name and by descriptor for an open handle.
//
// Both must exist whenever the predicates advertise the capability: a claim without an
// implementation fails at C++ LINK time on an undefined BCL P/Invoke symbol, and only for
// a program that actually SETS an attribute.
//
// A platform with no chflags (Linux) reports 0 from both predicates, so the BCL never calls
// these — but they must still LINK, because the P/Invoke declaration is reached whether or
// not the branch guarding it is taken. ENOTSUP is the honest answer there.
int32_t SystemNative_LChflags(const char* path, uint32_t flags)
{
#if defined(__APPLE__) && defined(UF_HIDDEN)
    int32_t result;
    while ((result = ::lchflags(path, flags)) < 0 && errno == EINTR)
    {
    }
    return result;
#else
    (void)path;
    (void)flags;
    errno = ENOTSUP;
    return -1;
#endif
}

int32_t SystemNative_FChflags(intptr_t fd, uint32_t flags)
{
#if defined(__APPLE__) && defined(UF_HIDDEN)
    int32_t result;
    while ((result = ::fchflags((int)fd, flags)) < 0 && errno == EINTR)
    {
    }
    return result;
#else
    (void)fd;
    (void)flags;
    errno = ENOTSUP;
    return -1;
#endif
}

// pal_uid.c SystemNative_GetEUid / GetEGid / GetGroups: effective uid/gid and
// the supplementary group list — FileStatus's read-only/access checks
// (HasReadOnlyFlag -> IsMemberOfGroup) reach them on the enumeration path.
uint32_t SystemNative_GetEUid(void)
{
    return ::geteuid();
}

uint32_t SystemNative_GetEGid(void)
{
    return ::getegid();
}

int32_t SystemNative_GetGroups(int32_t ngroups, uint32_t* groups)
{
    // ngroups == 0 is the count query (groups is ignored, and may be null).
    if (ngroups > 0 && dn2cpp_gc_kernel_write_unsafe(groups) != 0)
    {
        size_t bytes = (size_t)ngroups * sizeof(gid_t);
        uint8_t* scratch = bounce_scratch(bytes);
        if (scratch == nullptr)
        {
            errno = ENOMEM;
            return -1;
        }
        int32_t n = ::getgroups(ngroups, reinterpret_cast<gid_t*>(scratch));
        if (n > 0)
            std::memcpy(groups, scratch, (size_t)n * sizeof(gid_t));
        return n;
    }
    return ::getgroups(ngroups, (gid_t*)groups);
}

// pal_time.c SystemNative_FUTimens (HAVE_FUTIMENS arm).
int32_t SystemNative_FUTimens(intptr_t fd, Dn2CppPalTimeSpec* times)
{
    struct timespec updatedTimes[2];
    updatedTimes[0].tv_sec = (time_t)times[0].tv_sec;
    updatedTimes[0].tv_nsec = (long)times[0].tv_nsec;
    updatedTimes[1].tv_sec = (time_t)times[1].tv_sec;
    updatedTimes[1].tv_nsec = (long)times[1].tv_nsec;
    int32_t result;
    while ((result = ::futimens((int)fd, updatedTimes)) < 0 && errno == EINTR)
    {
    }
    return result;
}

// ======================= Darwin setattrlist bridge =========================

// Managed Interop.libc.AttrList as the transpiler emits it: sub-int32 struct
// fields are emitted at int32 width, so the u_short bitmapCount/reserved pair
// widens to two int32s (28 bytes total, attribute groups from offset 8). The
// real Darwin `struct attrlist` packs the pair into 4 bytes — a direct libc
// bind would hand the kernel a misaligned bitmap (EINVAL), so the transpiler
// reroutes the libc [f]setattrlist imports here (Compilation.ReadPInvoke) and
// this bridge re-packs per call. FileStatus's macOS creation-time restore
// (SetCreationTimeCore) is the only CoreLib caller.
struct Dn2CppLibcAttrList
{
    int32_t bitmapCount;
    int32_t reserved;
    uint32_t commonAttr;
    uint32_t volAttr;
    uint32_t dirAttr;
    uint32_t fileAttr;
    uint32_t forkAttr;
};

extern "C" int32_t dn2cpp_setattrlist(
    const char* path, Dn2CppLibcAttrList* attrList, void* attrBuf,
    intptr_t attrBufSize, unsigned long options)
{
#if defined(__APPLE__)
    struct attrlist al;
    std::memset(&al, 0, sizeof(al));
    al.bitmapcount = (u_short)attrList->bitmapCount;
    al.reserved = (u_int16_t)attrList->reserved;
    al.commonattr = attrList->commonAttr;
    al.volattr = attrList->volAttr;
    al.dirattr = attrList->dirAttr;
    al.fileattr = attrList->fileAttr;
    al.forkattr = attrList->forkAttr;
    return ::setattrlist(path, &al, attrBuf, (size_t)attrBufSize, (unsigned int)options);
#else
    // The managed callers are Darwin-only; keep the symbol linkable elsewhere.
    (void)path; (void)attrList; (void)attrBuf; (void)attrBufSize; (void)options;
    errno = ENOTSUP;
    return -1;
#endif
}

extern "C" int32_t dn2cpp_fsetattrlist(
    int32_t fd, Dn2CppLibcAttrList* attrList, void* attrBuf,
    intptr_t attrBufSize, unsigned long options)
{
#if defined(__APPLE__)
    struct attrlist al;
    std::memset(&al, 0, sizeof(al));
    al.bitmapcount = (u_short)attrList->bitmapCount;
    al.reserved = (u_int16_t)attrList->reserved;
    al.commonattr = attrList->commonAttr;
    al.volattr = attrList->volAttr;
    al.dirattr = attrList->dirAttr;
    al.fileattr = attrList->fileAttr;
    al.forkattr = attrList->forkAttr;
    return ::fsetattrlist(fd, &al, attrBuf, (size_t)attrBufSize, (unsigned int)options);
#else
    (void)fd; (void)attrList; (void)attrBuf; (void)attrBufSize; (void)options;
    errno = ENOTSUP;
    return -1;
#endif
}

// ============================ file copy / fs type ==========================

// Copies data + metadata (mode, times) from one open fd to another, like the
// real PAL: fcopyfile on macOS; a read/write loop + fchmod/futimens elsewhere.
int32_t SystemNative_CopyFile(intptr_t sourceFd, intptr_t destinationFd, int64_t sourceLength)
{
#if defined(__APPLE__)
    (void)sourceLength;
    int r;
    while ((r = ::fcopyfile((int)sourceFd, (int)destinationFd, nullptr, COPYFILE_ALL)) < 0
           && errno == EINTR)
    {
    }
    return r;
#else
    int inFd = (int)sourceFd, outFd = (int)destinationFd;
    struct stat st;
    if (::fstat(inFd, &st) != 0)
        return -1;
    int64_t remaining = sourceLength;
    while (remaining > 0)
    {
        size_t chunk = remaining > (1 << 20) ? (size_t)(1 << 20) : (size_t)remaining;
        ssize_t sent = ::sendfile(outFd, inFd, nullptr, chunk);
        if (sent < 0)
        {
            if (errno == EINTR)
                continue;
            return -1;
        }
        if (sent == 0)
            break;
        remaining -= sent;
    }
    if (::fchmod(outFd, st.st_mode & 07777) != 0)
        return -1;
    struct timespec times[2] = { st.st_atim, st.st_mtim };
    (void)::futimens(outFd, times); // best-effort, like the PAL
    return 0;
#endif
}

// The mounted filesystem's type for SafeFileHandle.CanLockTheFile (flock is a
// no-op lie on nfs/smb/cifs, so the BCL skips it there). Linux statfs exposes
// the magic directly; macOS exposes a name, mapped to the same managed
// UnixFileSystemTypes values. 0 = unknown (the managed side then assumes
// lockable, matching the real PAL's failure path).
uint32_t SystemNative_GetFileSystemType(intptr_t fd)
{
    struct statfs fs;
    int r;
    while ((r = ::fstatfs((int)fd, &fs)) < 0 && errno == EINTR)
    {
    }
    if (r != 0)
        return 0;
#if defined(__APPLE__)
    struct NameToType
    {
        const char* name;
        uint32_t value;
    };
    static const NameToType map[] = {
        {"apfs", 0x1A},        {"hfs", 0x4244},     {"autofs", 0x187},
        {"msdos", 0x4D44},     {"devfs", 0x1373},   {"nfs", 0x6969},
        {"smbfs", 0x517B},     {"cifs", 0xFF534D42}, {"tmpfs", 0x1021994},
    };
    for (const auto& e : map)
        if (std::strcmp(fs.f_fstypename, e.name) == 0)
            return e.value;
    return 0;
#else
    return (uint32_t)fs.f_type;
#endif
}

// pal_time.c SystemNative_GetTimestamp / SystemNative_GetTimestampResolution:
// monotonic timestamp used by Stopwatch on Unix. Interop.Sys.GetTimestamp is
// called from Stopwatch.GetTimestamp; Interop.Sys.GetTimestampResolution feeds
// Stopwatch.Frequency once. Real PAL uses clock_gettime(CLOCK_MONOTONIC) and
// reports resolution as 1e9 (nanoseconds); we mirror that exactly so managed
// Stopwatch.Frequency == 1_000_000_000 across transpiled binaries.
uint64_t SystemNative_GetTimestamp(void)
{
    struct timespec ts;
    if (::clock_gettime(CLOCK_MONOTONIC, &ts) != 0)
        return 0;
    return (uint64_t)ts.tv_sec * 1000000000ULL + (uint64_t)ts.tv_nsec;
}

uint64_t SystemNative_GetTimestampResolution(void)
{
    return 1000000000ULL;
}

// pal_time.c SystemNative_GetLowResolutionTimestamp: monotonic milliseconds
// behind Environment.TickCount64 (the real PAL reads a coarse monotonic clock;
// CLOCK_MONOTONIC's precision is a superset of the contract). Regex's timeout
// bookkeeping (RegexRunner.CheckTimeout) is the reach path here.
int64_t SystemNative_GetLowResolutionTimestamp(void)
{
    struct timespec ts;
    if (::clock_gettime(CLOCK_MONOTONIC, &ts) != 0)
        return 0;
    return (int64_t)ts.tv_sec * 1000 + (int64_t)(ts.tv_nsec / 1000000);
}

// pal_random.c SystemNative_GetCryptographicallySecureRandomBytes: the entropy
// source behind Guid.NewGuid / RandomNumberGenerator on Unix. Returns 0 on
// success, -1 on failure — the managed forwarder
// (Interop.GetCryptographicallySecureRandomBytes) throws CryptographicException
// on nonzero. Real randomness is required here: unlike Interop.GetRandomBytes
// (the *non*-secure hash-seed source, deterministically filled by
// dn2cpp_fill_nonsecure_random), two NewGuid() results must differ across
// calls and across runs.
int32_t SystemNative_GetCryptographicallySecureRandomBytes(uint8_t* buffer, int32_t bufferLength)
{
    if (buffer == nullptr || bufferLength < 0)
        return -1;
    // getentropy(2) is a syscall, so it writes through the kernel and needs the
    // bounce; arc4random_buf is a user-space DRBG and does not. Guarding both is
    // free (the guard is what costs, and it answers 0 unless the collector really
    // could have protected this buffer) and it keeps the arms from diverging.
    // Guid.NewGuid passes a stack local — off the heap, so never bounced.
    // RandomNumberGenerator.Fill(byte[]) is the shape that is.
    uint8_t* dst = buffer;
    uint8_t* scratch = nullptr;
    if (bufferLength > 0 && dn2cpp_gc_kernel_write_unsafe(buffer) != 0)
    {
        scratch = bounce_scratch((size_t)bufferLength);
        if (scratch == nullptr)
            return -1;
        dst = scratch;
    }

#if defined(__APPLE__) || defined(__FreeBSD__) || defined(__OpenBSD__) || defined(__ANDROID__)
    // getentropy is bionic-only from API 28; arc4random_buf has been present
    // since API 21, well below this project's android-24 floor.
    ::arc4random_buf(dst, (size_t)bufferLength);
#else
    // getentropy caps each request at 256 bytes; loop for larger fills.
    size_t remaining = (size_t)bufferLength;
    uint8_t* cursor = dst;
    while (remaining > 0)
    {
        size_t chunk = remaining > 256 ? 256 : remaining;
        if (::getentropy(cursor, chunk) != 0)
            return -1;
        cursor += chunk;
        remaining -= chunk;
    }
#endif
    if (scratch != nullptr)
        std::memcpy(buffer, scratch, (size_t)bufferLength);
    return 0;
}

// ================= special-folder search paths (Darwin) ====================
// Function-for-function reproduction of dotnet/runtime pal_searchpath.m (tag
// v10.0.9, src/native/libs/System.Native/) — Apple-only like its source: only
// the OSX flavor of CoreLib declares the import (Interop.SearchPath.cs), so a
// Linux/BSD closure never names the symbol and this section compiles away.
//
// Managed contract (verified against the shipping 10.0.9 metadata):
// `[LibraryImport] string? SearchPath(NSSearchPathDirectory folderId)`, whose
// raw extern is `byte* SystemNative_SearchPath(int32_t)` with
// SetLastError=false — no errno convention at all. The returned pointer must
// be malloc(3)-compatible or null: Utf8StringMarshaller.ConvertToManaged
// copies the bytes into a managed string and Utf8StringMarshaller.Free →
// Marshal.FreeCoTaskMem → NativeMemory.Free then frees it, so strdup is the
// allocation the caller expects (and is what pal_searchpath.m uses). A null
// return is a soft miss, not an error: Environment.GetSpecialFolder propagates
// it and GetFolderPathCore answers string.Empty.
//
// The one caller is Environment.GetSpecialFolder's macOS arm, passing
// NSDocumentDirectory=9, NSDesktopDirectory=12, NSCachesDirectory=13,
// NSApplicationSupportDirectory=14, NSMoviesDirectory=17, NSMusicDirectory=18
// and NSPicturesDirectory=19 — always valid ids, so like the upstream .m this
// does not validate the value. SystemNative_SearchPath_TempDirectory (same
// upstream file) is deliberately absent: nothing reaches it (Path.GetTempPath
// on macOS reads TMPDIR), and this file implements only what a reachable
// closure needs.
//
// pal_searchpath.m is Objective-C:
//     [[[NSFileManager defaultManager] URLsForDirectory:(NSSearchPathDirectory)folderId
//                                            inDomains:NSUserDomainMask] lastObject]
// then strdup([[url path] UTF8String]). This file is C++, so the same sends go
// through the objc runtime's C surface — objc_msgSend cast to each selector's
// signature, the ABI clang would lower those messages to anyway. Answering
// through real Foundation rather than a $HOME-concatenation table matters: a
// sandboxed process (an App-Store game) gets container paths from these APIs,
// and a reimplementation would silently hand it the unsandboxed ones.
//
// One deliberate divergence from the .m: the sends run inside an explicit
// autorelease pool where upstream has none, because a transpiled caller's thread
// has no enclosing pool and every call would otherwise leak the autoreleased
// NSArray/NSURL/NSString. Foundation and libobjc are linked by
// runtime/CMakeLists.txt's APPLE block, and that link is load-bearing beyond
// symbol resolution: objc_getClass("NSFileManager") answers nil — a silently
// empty GetFolderPath — unless dyld actually loaded Foundation.
#if defined(__APPLE__)
const char* SystemNative_SearchPath(int32_t folderId)
{
    void* pool = objc_autoreleasePoolPush();
    char* result = nullptr;
    id fileManager = ((id (*)(Class, SEL))objc_msgSend)(
        objc_getClass("NSFileManager"), sel_registerName("defaultManager"));
    // A nil receiver answers nil at every step below (objc semantics), which
    // funnels into the null strdup guard — the .m relies on the same behavior.
    id urls = ((id (*)(id, SEL, unsigned long, unsigned long))objc_msgSend)(
        fileManager, sel_registerName("URLsForDirectory:inDomains:"),
        (unsigned long)(uint32_t)folderId, 1UL /* NSUserDomainMask */);
    id url = ((id (*)(id, SEL))objc_msgSend)(urls, sel_registerName("lastObject"));
    id path = ((id (*)(id, SEL))objc_msgSend)(url, sel_registerName("path"));
    const char* utf8 = ((const char* (*)(id, SEL))objc_msgSend)(path, sel_registerName("UTF8String"));
    if (utf8 != nullptr)
        result = ::strdup(utf8);
    objc_autoreleasePoolPop(pool);
    return result;
}
#endif

// ===================== process / user identity =============================
// Function-for-function reproduction of dotnet/runtime pal_process.c /
// pal_uid.c (tag v10.0.9, src/native/libs/System.Native/), same style as the
// directory section above.
//
// Two of these four are declared in System.Private.CoreLib (GetPid, GetPwUidR);
// the other two live in System.Diagnostics.Process.dll (GetSid, Kill), so they
// are absent from a CoreLib-only reachability closure. The authority is the
// SHIPPING net10.0 metadata (10.0.9), not dotnet/runtime's main branch — the
// Kill contract has already changed there (see the note at SystemNative_Kill).
//
// This is not the file's only pal_uid.c surface: GetEUid / GetEGid / GetGroups
// sit up in the enumeration path above, where FileStatus's access checks reach
// them. Environment.UserName crosses both — GetEUid feeds the uid that
// GetPwUidR down here looks up.

// pal_process.h: "Returns the Process ID of the current executing process. This
// call should never fail" — getpid(2) cannot. Reach path:
// Environment.ProcessId → Environment.GetProcessId → Interop.Sys.GetPid, and
// from there Environment.WorkingSet, the TPL/EventSource activity-id plumbing
// (TplEventSource.CreateGuidForTaskID) and System.Diagnostics.Process's own
// GetCurrentProcess.
int32_t SystemNative_GetPid(void)
{
    return (int32_t)::getpid();
}

// pal_process.h: "Returns the sessions ID of the specified process; if 0 is
// passed in, returns the session ID of the current process. Returns a session ID
// on success; otherwise, returns -1 and sets errno." Declared SetLastError=false,
// and its one caller (System.Diagnostics.Process's ProcessManager.OSX.cs
// CreateProcessInfo) drops a -1 without reading errno — so nothing observes the
// errno left behind, but getsid(2) sets it anyway.
int32_t SystemNative_GetSid(int32_t pid)
{
    return (int32_t)::getsid((pid_t)pid);
}

// Managed Interop.Sys.Signals (Interop.Kill.cs, mirrored by pal_process.h's
// `Signals` enum) — PAL-stable values that are NOT host signal numbers:
//   PAL_NONE = 0, PAL_SIGKILL = 9, PAL_SIGSTOP = 19
// Verified against the shipping 10.0.9 System.Diagnostics.Process metadata.
#define DN2CPP_PAL_SIGNONE 0
#define DN2CPP_PAL_SIGKILL 9
#define DN2CPP_PAL_SIGSTOP 19

// pal_process.h SystemNative_Kill: 0 on success, -1 with errno on failure.
//
// The translation below is load-bearing, and on Darwin it is the whole point:
// SIGSTOP is 17 there and 19 is SIGCONT, so passing the managed value through
// would RESUME the process the caller asked to stop — a silent wrong action, not
// an error. SIGKILL happens to be 9 on both Darwin and Linux, which is exactly
// what would make a pass-through look correct in a test.
//
// An unmodeled value is EINVAL, never a pass-through, mirroring the PAL's
// `default:` arm (an `assert_msg` that compiles away in release, then EINVAL). A
// raw host signal number arriving here means the caller is speaking the OTHER
// contract: .NET 11 retires this enum — Kill takes a real signal number and a new
// SystemNative_GetPlatformSIGSTOP supplies the host value — so forwarding it
// would misfire silently against a future CoreLib instead of failing. That
// CoreLib is recognizable: it exports SystemNative_GetPlatformSIGSTOP, so it
// would arrive here as a link error against this file, which is the loud outcome.
//
// SetLastError=true on the managed declaration is implemented in managed IL by
// the LibraryImport generator, not by the ImplMap: the wrapper brackets the call
// with Marshal.SetLastSystemError(0) / GetLastSystemError(), which bottom out in
// SystemNative_SetErrNo / SystemNative_GetErrNo above. So errno must still be
// live when the transpiled caller makes that second call — the same window real
// .NET runs in.
int32_t SystemNative_Kill(int32_t pid, int32_t signal)
{
    int hostSignal;
    switch (signal)
    {
        case DN2CPP_PAL_SIGNONE: hostSignal = 0; break;
        case DN2CPP_PAL_SIGKILL: hostSignal = SIGKILL; break;
        case DN2CPP_PAL_SIGSTOP: hostSignal = SIGSTOP; break;
        default:
            errno = EINVAL;
            return -1;
    }
    return ::kill((pid_t)pid, hostSignal);
}

// Managed Interop.Sys.Passwd (Interop.GetPwUid.cs). Verified by reflection
// against net10.0 System.Private.CoreLib: LayoutKind.Sequential, no ClassLayout
// row at all (no explicit Pack, no explicit Size), Marshal.SizeOf = 48 on LP64 —
//   byte*  Name          @ 0
//   byte*  Password      @ 8
//   uint   UserId        @ 16
//   uint   GroupId       @ 20
//   byte*  UserInfo      @ 24
//   byte*  HomeDirectory @ 32
//   byte*  Shell         @ 40
// The two uints fill one pointer-sized slot exactly, so the struct carries no
// internal padding and the layout is Pack-independent — there is structurally no
// room for the packing mistake that makes this class of bug silent. The asserts
// are written against sizeof(void*) so they hold on an ILP32 target too
// (0/4/8/12/16/20/24, 28 bytes), which is not measured, only derived.
struct Dn2CppPalPasswd
{
    const char* Name;
    const char* Password;
    uint32_t UserId;
    uint32_t GroupId;
    const char* UserInfo;
    const char* HomeDirectory;
    const char* Shell;
};
static_assert(offsetof(Dn2CppPalPasswd, Name) == 0, "Name first");
static_assert(offsetof(Dn2CppPalPasswd, Password) == sizeof(void*), "Password after Name");
static_assert(offsetof(Dn2CppPalPasswd, UserId) == 2 * sizeof(void*), "UserId after Password");
static_assert(offsetof(Dn2CppPalPasswd, GroupId) == 2 * sizeof(void*) + 4, "GroupId packs beside UserId");
static_assert(offsetof(Dn2CppPalPasswd, UserInfo) == 2 * sizeof(void*) + 8, "UserInfo after the two uints");
static_assert(offsetof(Dn2CppPalPasswd, HomeDirectory) == 3 * sizeof(void*) + 8, "HomeDirectory after UserInfo");
static_assert(offsetof(Dn2CppPalPasswd, Shell) == 4 * sizeof(void*) + 8, "Shell last");
static_assert(sizeof(Dn2CppPalPasswd) == 5 * sizeof(void*) + 8, "no tail padding");

// pal_uid.c ConvertNativePasswdToPalPasswd: hand back pointers INTO the caller's
// `buf`, which is why the buffer is a parameter at all.
static int32_t dn2cpp_pal_convert_passwd(
    int error, const struct passwd* nativePwd, const struct passwd* result, Dn2CppPalPasswd* pwd)
{
    // A positive return is an errno — a failure other than entry-not-found. The
    // managed out param must be initialized on every path (Interop.GetPwUid.cs
    // reads pwd.Name only after error == 0, but the PAL zeroes it regardless and
    // a transpiled `out` local is not zero-initialized for free).
    if (error != 0)
    {
        std::memset(pwd, 0, sizeof(*pwd));
        return error;
    }
    if (result == nullptr)
    {
        std::memset(pwd, 0, sizeof(*pwd));
        return -1; // shim convention for entry-not-found
    }
    pwd->Name = nativePwd->pw_name;
    pwd->Password = nativePwd->pw_passwd;
    pwd->UserId = (uint32_t)nativePwd->pw_uid;
    pwd->GroupId = (uint32_t)nativePwd->pw_gid;
    pwd->UserInfo = nativePwd->pw_gecos;
    pwd->HomeDirectory = nativePwd->pw_dir;
    pwd->Shell = nativePwd->pw_shell;
    return 0;
}

// pal_uid.h SystemNative_GetPwUidR: "Returns 0 for success, -1 if no entry
// found, positive error number for any other failure." That is NOT this file's
// usual errno convention: the declaration is SetLastError=false, so the managed
// side reads the RETURN value and wraps a positive one in Interop.ErrorInfo.
// ERANGE specifically means "buffer too small" and drives the growth loop in
// Interop.Sys.GetUserNameFromPasswd (256-byte stackalloc, then a pinned byte[]
// doubling until it fits) — answering anything else for a short buffer turns a
// retry into a thrown IOException.
//
// `buf` is handed to getpwuid_r UNBOUNCED, and that is a decision, not an
// oversight. Two reasons, both needed:
//   - The bounce above exists for syscalls the KERNEL writes through. getpwuid_r
//     is libc, and its fill is a user-space copy out of the name-service
//     backend's own buffer — a store that faults into Boehm's handler, is
//     unprotected, and retries. That is the designed path, not the broken one.
//   - Bouncing would be WRONG here without also rebasing every returned pointer,
//     because the Passwd fields point into whatever buffer was filled. A bounce
//     that copied scratch→buf and left pwd->Name aimed at the thread-local
//     scratch would read correctly today and corrupt on the next bounced call.
// Reach path: Environment.UserName and Environment.GetFolderPath /
// SystemDirectory (via System.IO.PersistedFiles.TryGetHomeDirectoryFromPasswd).
int32_t SystemNative_GetPwUidR(uint32_t uid, Dn2CppPalPasswd* pwd, char* buf, int32_t buflen)
{
    // The real PAL asserts pwd/buf non-null (compiled away in release) and
    // returns EINVAL for a negative length. Keep the length check verbatim and
    // make the pointer checks real: a null deref inside a shipped game's
    // username lookup is a worse trade than a divergence no correct caller can
    // reach, since the managed side always passes a live local and a live buffer.
    if (pwd == nullptr || buf == nullptr || buflen < 0)
        return EINVAL;

    struct passwd nativePwd;
    struct passwd* result = nullptr;
    int error;
    while ((error = ::getpwuid_r((uid_t)uid, &nativePwd, buf, (size_t)buflen, &result)) == EINTR)
    {
    }
    return dn2cpp_pal_convert_passwd(error, &nativePwd, result, pwd);
}

// ===================== low-level monitor ===================================
// Function-for-function reproduction of dotnet/runtime pal_threading.c (tag
// v10.0.9, src/native/libs/System.Native/), same style as the sections above.
//
// This is the primitive CoreLib's OWN LowLevelLock / LowLevelMonitor are built
// on — a mutex + condition variable pair whose lifetime the managed side
// controls explicitly. It is NOT dn2cpp's Monitor: `lock(obj)` lowers to
// dn2cpp_monitor_* and never comes here. What comes here is transpiled CoreLib
// IL that needs a lock BELOW the managed lock (the runtime's own bootstrap
// synchronization), which cannot use the managed one without recursing.
//
// Reach path: System.Threading.RegisteredWaitHandle's static s_callbackLock (a
// LowLevelLock) and PortableThreadPool.WaitThread, i.e. Process.Dispose ->
// Process.Close -> StopWatchingForExit -> RegisteredWaitHandle.Unregister.
//
// The managed declarations (Interop.LowLevelMonitor.cs, verified against the
// shipping 10.0.9 metadata) are SetLastError=false and every one returns void
// except Create, which returns the opaque nint the other five take back. So
// there is no errno contract and no failure the managed side can observe: a
// pthread_* error is fatal here rather than silently ignored, because a lock
// that quietly failed to lock is the one outcome the caller cannot detect.
//
// pal_threading.c also defines SystemNative_LowLevelMonitor_TimedWait. It is
// deliberately NOT reproduced: the shipping net10.0 CoreLib declares no P/Invoke
// for it (checked by reflection over the Interop.Sys members), so the symbol has
// no managed mouth and an implementation would be code no reachability closure
// can reach. If a future CoreLib adds one, it arrives here as a link error
// naming the symbol — which is this file's standard loud outcome for a new gap.
struct Dn2CppPalLowLevelMonitor
{
    pthread_mutex_t Mutex;
    pthread_cond_t Condition;
};

// pal_threading.c uses a CLOCK_MONOTONIC condition attribute where the platform
// supports it, so a TimedWait is immune to wall-clock jumps. With TimedWait out
// of scope (see above) nothing here reads the clock, so the attribute would be
// unobservable — the default attr is used, exactly as the real PAL does on
// Darwin, which has no pthread_condattr_setclock.
void* SystemNative_LowLevelMonitor_Create(void)
{
    auto* monitor = (Dn2CppPalLowLevelMonitor*)std::malloc(sizeof(Dn2CppPalLowLevelMonitor));
    if (monitor == nullptr)
        return nullptr; // the managed side treats null as a fatal OOM
    if (::pthread_mutex_init(&monitor->Mutex, nullptr) != 0)
    {
        std::free(monitor);
        return nullptr;
    }
    if (::pthread_cond_init(&monitor->Condition, nullptr) != 0)
    {
        ::pthread_mutex_destroy(&monitor->Mutex);
        std::free(monitor);
        return nullptr;
    }
    return monitor;
}

void SystemNative_LowLevelMonitor_Destroy(void* monitor)
{
    auto* m = (Dn2CppPalLowLevelMonitor*)monitor;
    ::pthread_cond_destroy(&m->Condition);
    ::pthread_mutex_destroy(&m->Mutex);
    std::free(m);
}

void SystemNative_LowLevelMonitor_Acquire(void* monitor)
{
    int error = ::pthread_mutex_lock(&((Dn2CppPalLowLevelMonitor*)monitor)->Mutex);
    if (error != 0)
        std::abort(); // see the header: an unlocked "lock" is undetectable upstream
}

void SystemNative_LowLevelMonitor_Release(void* monitor)
{
    int error = ::pthread_mutex_unlock(&((Dn2CppPalLowLevelMonitor*)monitor)->Mutex);
    if (error != 0)
        std::abort();
}

void SystemNative_LowLevelMonitor_Wait(void* monitor)
{
    auto* m = (Dn2CppPalLowLevelMonitor*)monitor;
    int error = ::pthread_cond_wait(&m->Condition, &m->Mutex);
    if (error != 0)
        std::abort();
}

// Signal-then-release, in that order and under the lock — the PAL's own
// ordering. Signaling first means the woken thread cannot miss the signal by
// racing the unlock, at the cost of it immediately blocking on the mutex the
// caller still holds for one more instruction.
void SystemNative_LowLevelMonitor_Signal_Release(void* monitor)
{
    auto* m = (Dn2CppPalLowLevelMonitor*)monitor;
    int error = ::pthread_cond_signal(&m->Condition);
    if (error != 0)
        std::abort();
    error = ::pthread_mutex_unlock(&m->Mutex);
    if (error != 0)
        std::abort();
}

} // extern "C"

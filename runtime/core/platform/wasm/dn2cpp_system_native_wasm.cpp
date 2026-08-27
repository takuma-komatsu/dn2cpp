// dn2cpp_system_native_wasm.cpp — the slice of the .NET PAL (libSystem.Native)
// surface that the Emscripten target needs.
//
// platform/posix/dn2cpp_system_native.cpp is not part of the wasm build: most of
// what it carries (process identity, directory enumeration, the Darwin bridges,
// the low-level monitor) has no caller here, and the parts that do are gathered
// below. The ABI is the managed one either way — Interop.Sys in
// System.Private.CoreLib — so every struct layout, PAL flag and PAL error code
// here must equal its POSIX twin exactly, and the bodies are that file's verbatim
// wherever Emscripten's musl answers the same call.
//
// The FILE-I/O closure is shipped, against MEMFS. FileStream, SafeFileHandle and
// the whole File.* surface work; the files live in the page's memory and are gone
// when it unloads. It is here because a game does not opt into it: Trace with a
// DefaultTraceListener log file goes File.AppendAllText -> SafeFileHandle -> the
// closure, and a Web export that omits it fails at the first log line.
//
// The rest is not file I/O, and each entry is here for its own reason:
//
//   * The CSPRNG has a caller that is not a P/Invoke at all and IS compiled on
//     wasm: intrinsics/dn2cpp_openssl_crypto_digest.cpp's
//     CryptoNative_GetRandomBytes forwards straight to it.
//   * The two monotonic clocks are what Stopwatch.GetTimestamp and an ordinary
//     user MemberReference to Environment.TickCount64 reach through their real
//     BCL bodies. In-CoreLib MethodDefinition calls to TickCount64 can instead
//     lower to dn2cpp_tickcount64. The engine's Time API is a separate surface.
//   * InterfaceNameToIndex is the named IPv6-scope parser's platform query. A
//     browser exposes no host network-interface namespace, so every name has the
//     POSIX API's not-found answer (zero) rather than becoming an unresolved import.
//   * SysLog and Write are DebugProvider.WriteCore's two sinks — the debugger arm
//     and the stderr arm — so any game that logs reaches both. Write takes an fd,
//     not a path: it is not part of the file-I/O closure.
//   * Malloc/Free are the C heap. Marshal.{AllocHGlobal,FreeHGlobal} and the
//     NativeMemory family are intercepted and never reach them, but the BSTR
//     allocators and every marshaller stub that calls NativeMemory as an
//     in-CoreLib MethodDefinition run the real body down to these two.
//   * The errno accessors and the two PAL/platform error converters ride in behind
//     any of them: a managed retry loop over a failing PAL call reads errno, and
//     every ErrorInfo the BCL builds converts through the tables.
//
// Only what a call site actually reaches is defined here. Stopwatch.Frequency is
// a constant on this CoreLib, so the POSIX twin's timestamp-resolution entry does
// not belong here.
//
// None of the read-like fills below bounce through scratch memory the way the
// POSIX twin's do. That bounce exists because Boehm's incremental mode mprotects
// heap pages, so a kernel store into a managed buffer returns EFAULT instead of
// trapping into the collector. There is no page-protection VDB under Emscripten:
// the collector is built GC_DISABLE_INCREMENTAL and nothing mprotects anything.
//
// It stayed invisible because of the shape of the two link models, not because
// nothing reached it. In an EXECUTABLE link wasm-ld dead-strips the unreferenced
// tail, and the wasm console gate's programs never touch a Guid or an RNG — so
// the reference is stripped before it can be missed. A SIDE MODULE has no such
// luck: an unresolved symbol there is not an error but a wasm IMPORT, and the
// import is not diagnosed at dlopen either (emscripten's loader hands back a
// valid handle and dlsym resolves) — it throws `TypeError: resolved is not a
// function` out of the JS glue at the instant the function is first CALLED,
// naming a wasm function index rather than the symbol. A real game reaches this
// on Guid.NewGuid(), RandomNumberGenerator, hash seeding, Stopwatch, or
// Environment.TickCount64.

#include <cstdint>
#include <cstdio>   // fprintf — the SysLog stand-in
#include <cstdlib>  // malloc / free
#include <cstring>  // strerror_r
#include <ctime>    // clock_gettime
#include <cerrno>   // EINTR
#include <fcntl.h>  // open, O_*, posix_fadvise
#include <sys/file.h>   // flock
#include <sys/stat.h>   // stat / fstat
#include <sys/statfs.h> // fstatfs — GetFileSystemType
#include <unistd.h> // getentropy, write

extern "C" {

// pal_networking.c SystemNative_InterfaceNameToIndex. Emscripten applications
// have no host interface namespace to query: browser networking is mediated by
// the host and does not expose interface indexes. Zero is if_nametoindex(3)'s
// documented not-found result and is the answer the managed parser already handles.
uint32_t SystemNative_InterfaceNameToIndex(char* interfaceName)
{
    if (interfaceName == nullptr)
        errno = EINVAL;
    return 0;
}

// pal_random.c SystemNative_GetCryptographicallySecureRandomBytes: the entropy
// source behind Guid.NewGuid / RandomNumberGenerator. The portable
// dn2cpp_apple_crypto_random.cpp adapter also consumes this ABI when the real
// BCL reaches RandomNumberGenerator through AppleCryptoNative_GetRandomBytes.
// Returns 0 on success, -1 on failure — the managed forwarder
// (Interop.GetCryptographicallySecureRandomBytes) throws CryptographicException
// on nonzero. The contract is the POSIX one, verbatim; only the backend differs.
//
// Real randomness is required, and Emscripten supplies it: its musl getentropy
// is backed by the host CSPRNG (crypto.getRandomValues in a browser,
// crypto.randomBytes under node), not by a seeded PRNG. Two NewGuid() results
// must differ across calls AND across runs, which a deterministic fill would
// quietly break — that is the failure the non-secure hash-seed source
// (dn2cpp_fill_nonsecure_random) is allowed and this one is not.
//
// No GC bounce buffer here, unlike the POSIX twin. That bounce exists because
// Boehm's incremental mode mprotects heap pages, so a kernel write through to
// one faults with EFAULT instead of trapping into the collector's handler. The
// Boehm GC is built on this axis, but incremental mode is not — there is no
// page-protection VDB under Emscripten, so the collector is compiled with
// GC_DISABLE_INCREMENTAL and dn2cpp_gc.cpp forces the mode off. Nothing
// mprotects anything, so there is no hazard to bounce around.
int32_t SystemNative_GetCryptographicallySecureRandomBytes(uint8_t* buffer, int32_t bufferLength)
{
    if (buffer == nullptr || bufferLength < 0)
        return -1;

    // getentropy caps each request at 256 bytes; loop for larger fills.
    size_t remaining = static_cast<size_t>(bufferLength);
    uint8_t* cursor = buffer;
    while (remaining > 0)
    {
        size_t chunk = remaining > 256 ? 256 : remaining;
        if (::getentropy(cursor, chunk) != 0)
            return -1;
        cursor += chunk;
        remaining -= chunk;
    }
    return 0;
}

// pal_time.c's monotonic clocks, the POSIX twins' bodies verbatim. Emscripten's
// musl supplies clock_gettime, and a side module resolves it from the main module
// the same way it resolves the rest of libc.
uint64_t SystemNative_GetTimestamp(void)
{
    struct timespec ts;
    if (::clock_gettime(CLOCK_MONOTONIC, &ts) != 0)
        return 0;
    return (uint64_t)ts.tv_sec * 1000000000ULL + (uint64_t)ts.tv_nsec;
}

int64_t SystemNative_GetLowResolutionTimestamp(void)
{
    struct timespec ts;
    if (::clock_gettime(CLOCK_MONOTONIC, &ts) != 0)
        return 0;
    return (int64_t)ts.tv_sec * 1000 + (int64_t)(ts.tv_nsec / 1000000);
}

// pal_io.c SystemNative_SysLog, the sink DebugProvider.WriteToDebugger falls to
// once Debugger.IsLogging const-folds false — a Trace/Debug write reaches it from
// any game that logs, and on a side module an undefined one throws at the first
// call. A browser has no syslog, so this writes stderr (the browser console);
// `message` is the format ("%s") and `arg1` its argument, as on the POSIX twin.
void SystemNative_SysLog(int32_t priority, const char* message, const char* arg1)
{
    (void)priority;
#if defined(__clang__)
#pragma clang diagnostic push
#pragma clang diagnostic ignored "-Wformat-nonliteral"
#endif
    ::fprintf(stderr, message, arg1);
#if defined(__clang__)
#pragma clang diagnostic pop
#endif
    ::fputc('\n', stderr);
}

// pal_io.c SystemNative_Write, the other half of DebugProvider.WriteCore. Takes an
// already-open fd, so it needs nothing from the file-I/O closure: under Emscripten
// 1 and 2 reach the JS console and any other fd is MEMFS. Contract is the POSIX
// twin's — bytes written, or -1 with errno set.
//
// No GC bounce buffer here, for the same reason the CSPRNG needs none: nothing
// mprotects the heap on this axis.
int32_t SystemNative_Write(intptr_t fd, const void* buffer, int32_t bufferSize)
{
    ssize_t n;
    while ((n = ::write((int)fd, buffer, (size_t)bufferSize)) < 0 && errno == EINTR)
    {
    }
    return (int32_t)n;
}

// The PAL's C-heap pair, the POSIX twins verbatim. Emscripten's dlmalloc backs it.
// This is not the GC heap: a block from here is invisible to the collector and must
// be freed through Free.
void* SystemNative_Malloc(uintptr_t size)
{
    return std::malloc(size == 0 ? 1 : size); // malloc(0) may return NULL; .NET expects a pointer
}

void SystemNative_Free(void* ptr)
{
    std::free(ptr);
}

// The errno slot, reached by every managed retry loop over a PAL call that can fail —
// DebugProvider's stderr loop is the one here. Emscripten's musl gives each of them a
// real errno; the managed side maps the value itself.
int32_t SystemNative_GetErrNo(void)
{
    return errno;
}

void SystemNative_SetErrNo(int32_t error)
{
    errno = error;
}

// ============================ PAL error codes ==============================
// Interop.Error (managed) values: 0x1XXXX per errno, ENONSTANDARD for an errno
// with no PAL equivalent. These two tables ARE the ABI, so they are the POSIX
// twin's verbatim, host guards and all — musl defines every errno they name, and
// a value that differs between the two files is a silently mis-mapped exception.

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

// StrErrorR(platform errno, buffer, size) → the message. The managed caller treats
// a null return as "buffer too small" and retries with a larger one.
//
// Unconditionally the XSI form: musl has no GNU strerror_r, so unlike the POSIX
// twin there is nothing here to select between.
const char* SystemNative_StrErrorR(int32_t platformErrno, char* buffer, int32_t bufferSize)
{
    int r = ::strerror_r(platformErrno, buffer, (size_t)bufferSize);
    if (r == 0 || r == ERANGE)
        return buffer; // ERANGE = truncated, still NUL-terminated
    std::snprintf(buffer, (size_t)bufferSize, "Unknown error %d", platformErrno);
    return buffer;
}

// ======================= file I/O against MEMFS ============================

// PAL OpenFlags (managed Interop.Sys.OpenFlags) → host O_* bits. The PAL side of
// this mapping is platform-stable and the host side is musl's; the POSIX twin's
// body verbatim.
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
    ssize_t n;
    while ((n = ::read((int)fd, buffer, (size_t)bufferSize)) < 0 && errno == EINTR)
    {
    }
    return (int32_t)n;
}

int32_t SystemNative_PRead(intptr_t fd, void* buffer, int32_t bufferSize, int64_t fileOffset)
{
    if (bufferSize < 0)
        bufferSize = 0;
    ssize_t n;
    while ((n = ::pread((int)fd, buffer, (size_t)bufferSize, (off_t)fileOffset)) < 0
           && errno == EINTR)
    {
    }
    return (int32_t)n;
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

// PAL LockOperations values equal the host LOCK_SH/EX/NB/UN (1/2/4/8). Emscripten
// answers flock(2) by succeeding: one page is one process, so no lock it could
// hand out is ever contended. The call is kept rather than stubbed here, so a
// future MEMFS that does arbitrate reports through it.
int32_t SystemNative_FLock(intptr_t fd, int32_t operation)
{
    int r;
    while ((r = ::flock((int)fd, operation)) < 0 && errno == EINTR)
    {
    }
    return r;
}

// Advisory readahead hint; the managed caller only debug-asserts success. Returns
// the error number directly (posix_fadvise convention).
int32_t SystemNative_PosixFAdvise(intptr_t fd, int64_t offset, int64_t length, int32_t advice)
{
    int r;
    while ((r = ::posix_fadvise((int)fd, (off_t)offset, (off_t)length, advice)) == EINTR)
    {
    }
    return r;
}

// Preallocation for FileStream(preallocationSize:). **It reserves BLOCKS and must
// not change the file's LENGTH** — an arm that extends the size makes the file read
// back as zeros nobody wrote, which every length-driven reader then sees.
//
// MEMFS has no size-preserving primitive: posix_fallocate is specified to grow the
// file to offset+len, and Emscripten has no fallocate(FALLOC_FL_KEEP_SIZE). So do
// nothing and report success, like the POSIX twin's fallback arm — a hint that is
// not honored is a hint; a hint that corrupts the length is a bug. The managed
// caller only surfaces ENOSPC/EFBIG and ignores any other outcome.
int32_t SystemNative_FAllocate(intptr_t fd, int64_t offset, int64_t length)
{
    (void)fd;
    (void)offset;
    (void)length;
    return 0;
}

// Managed Interop.Sys.FileStatus, sequential layout (net10.0): 120 bytes with
// trailing pad. Mode is the RAW st_mode — the managed FileTypes constants
// (S_IFMT/S_IFREG/…) match the POSIX octal values.
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
    uint32_t UserFlags; // 0x8000 = hidden; MEMFS has no such attribute
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
    out->ATime = st.st_atim.tv_sec;
    out->ATimeNsec = st.st_atim.tv_nsec;
    out->MTime = st.st_mtim.tv_sec;
    out->MTimeNsec = st.st_mtim.tv_nsec;
    out->CTime = st.st_ctim.tv_sec;
    out->CTimeNsec = st.st_ctim.tv_nsec;
    out->BirthTime = 0;
    out->BirthTimeNsec = 0;
    out->Flags = 0; // no birth time
    out->UserFlags = 0;
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

int32_t SystemNative_Unlink(const char* path)
{
    int r;
    while ((r = ::unlink(path)) < 0 && errno == EINTR)
    {
    }
    return r;
}

char* SystemNative_GetCwd(char* buffer, int32_t bufferSize)
{
    return ::getcwd(buffer, (size_t)bufferSize);
}

// The managed caller compares this against the NFS/CIFS magics to decide whether
// file locking is trustworthy. MEMFS reports 0, which is none of them — the
// ordinary local-filesystem behaviour, which is what MEMFS is.
uint32_t SystemNative_GetFileSystemType(intptr_t fd)
{
    struct statfs fs;
    int r;
    while ((r = ::fstatfs((int)fd, &fs)) < 0 && errno == EINTR)
    {
    }
    if (r != 0)
        return 0;
    return (uint32_t)fs.f_type;
}

} // extern "C"

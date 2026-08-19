// dn2cpp_system_native_wasm.cpp — the sliver of the .NET PAL (libSystem.Native)
// surface that the Emscripten target needs.
//
// The bulk of that surface (platform/posix/dn2cpp_system_native.cpp) is the
// file-I/O P/Invoke closure, and it is deliberately NOT part of the wasm build.
// Several things it also defines are not file I/O, and each is here for its own
// reason — the exclusion was written for the closure and covers none of them.
//
//   * The CSPRNG has a caller that is not a P/Invoke at all and IS compiled on
//     wasm: intrinsics/dn2cpp_openssl_crypto_digest.cpp's
//     CryptoNative_GetRandomBytes forwards straight to it.
//   * The two monotonic clocks are what Stopwatch.GetTimestamp and an ordinary
//     user MemberReference to Environment.TickCount64 reach through their real
//     BCL bodies. In-CoreLib MethodDefinition calls to TickCount64 can instead
//     lower to dn2cpp_tickcount64. The engine's Time API is a separate surface.
//   * SysLog and Write are DebugProvider.WriteCore's two sinks — the debugger arm
//     and the stderr arm — so any game that logs reaches both. Write takes an fd,
//     not a path: it is not part of the file-I/O closure.
//   * Malloc/Free are the C heap. Marshal.{AllocHGlobal,FreeHGlobal} and the
//     NativeMemory family are intercepted and never reach them, but the BSTR
//     allocators and every marshaller stub that calls NativeMemory as an
//     in-CoreLib MethodDefinition run the real body down to these two.
//   * The errno accessors ride in behind any of them: a managed retry loop over a
//     failing PAL call reads errno, and that read is a P/Invoke of its own.
//
// Only what a call site actually reaches is defined here. Stopwatch.Frequency is
// a constant on this CoreLib, so the POSIX twin's timestamp-resolution entry does
// not belong here.
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
#include <ctime>    // clock_gettime
#include <cerrno>   // EINTR
#include <unistd.h> // getentropy, write

extern "C" {

// pal_random.c SystemNative_GetCryptographicallySecureRandomBytes: the entropy
// source behind Guid.NewGuid / RandomNumberGenerator. Returns 0 on success, -1 on
// failure — the managed forwarder (Interop.GetCryptographicallySecureRandomBytes)
// throws CryptographicException on nonzero. The contract is the POSIX one,
// verbatim; only the backend differs.
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

} // extern "C"

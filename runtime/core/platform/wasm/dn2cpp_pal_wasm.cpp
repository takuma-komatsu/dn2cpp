// dn2cpp_pal_wasm.cpp — Emscripten (WASM) implementation of the Platform
// Abstraction Layer.
//
// Emscripten's musl libc provides the same POSIX entry points the posix PAL
// uses, so each function forwards directly — but the *semantics* behind them
// differ from a native host:
//
//   - File system: paths resolve in MEMFS, an in-memory FS private to the
//     wasm instance. It starts (nearly) empty, is not shared with the host,
//     and is discarded when the instance exits; persistence requires the
//     embedder to mount/preload files.
//   - Environment: a small synthetic block. A browser build sees only
//     Emscripten's defaults — treat the environment as effectively empty,
//     which is why DN2CPP_CPU_FEATURES_DEFAULT exists. The console executable
//     links dn2cpp_env_node.js as a pre-js, which under node copies the
//     process's DN2CPP_* variables into the block before main runs.
//   - Local time: implemented over the JS Date; under node this is UTC-only
//     unless TZ is set in the node process's environment.
//   - Allocator: dlmalloc; malloc_usable_size is available via <malloc.h>.

#include "platform/dn2cpp_pal.h"

#include <cstdint>    // int32_t / uint8_t
#include <cstdio>     // fwrite / fflush (console sink)
#include <cstdlib>    // getenv
#include <ctime>      // localtime_r / mktime / std::tm / std::time_t
#include <malloc.h>   // malloc_usable_size (dlmalloc)
#include <unistd.h>   // getcwd / unlink / chdir
#include <sys/stat.h> // stat / S_ISREG / S_ISDIR / mkdir

char* dn2cpp_pal_getcwd(char* buf, size_t size)
{
    return ::getcwd(buf, size);
}

int dn2cpp_pal_unlink(const char* path)
{
    return ::unlink(path);
}

int dn2cpp_pal_mkdir(const char* path)
{
    return ::mkdir(path, 0777);
}

int dn2cpp_pal_chdir(const char* path)
{
    return ::chdir(path);
}

int dn2cpp_pal_path_kind(const char* path)
{
    struct stat st;
    if (::stat(path, &st) != 0)
        return DN2CPP_PAL_PATH_MISSING;
    if (S_ISREG(st.st_mode))
        return DN2CPP_PAL_PATH_FILE;
    if (S_ISDIR(st.st_mode))
        return DN2CPP_PAL_PATH_DIR;
    return DN2CPP_PAL_PATH_OTHER;
}

const char* dn2cpp_pal_getenv(const char* name)
{
    return ::getenv(name);
}

// No host locale to report. The environment block is the synthetic one described
// at the top of this file, so an LC_ALL/LANG scan would answer from whatever the
// embedder happened to preload — i.e. it would make a browser build's default
// culture depend on a page's plumbing rather than on the user. Answering 0 keeps
// the invariant default a wasm program has always had, which is also the only
// deterministic answer available: `navigator.language` is reachable only through
// JS, and a module that read it would resolve its culture differently in node
// and in a browser tab.
int32_t dn2cpp_pal_default_locale_name(char* buf, size_t size)
{
    if (buf != nullptr && size != 0)
        buf[0] = '\0';
    return 0;
}

// The runtime's raw UTF-8 codec cores (defined in intrinsics/dn2cpp_system_reflection.cpp).
// Forward-declared here rather than pulling in the whole runtime header, exactly as the
// POSIX PAL does: Emscripten's narrow encoding is UTF-8 (musl, and the JS string bridge
// on both sides of the boundary), so the Ansi seam IS the UTF-8 seam here too and
// delegates byte-for-byte to the same cores. Keeping the two implementations textually
// parallel is the point — an Ansi round-trip that disagreed between the native and wasm
// builds would be a silent divergence in P/Invoke marshalling, not a crash.
int32_t dn2cpp_utf16_to_utf8(const char16_t* src, int32_t len, char* buf, int32_t bufSize);
int32_t dn2cpp_utf8_to_utf16(const char* utf8, int32_t byteLength, char16_t* out);

int32_t dn2cpp_pal_ansi_encode(const char16_t* src, int32_t srcLen, char* buf, int32_t bufSize, int32_t bestFit)
{
    // UTF-8 encodes every code unit, so there is no unmappable case and best-fit is moot.
    (void)bestFit;
    return dn2cpp_utf16_to_utf8(src, srcLen, buf, bufSize);
}

int32_t dn2cpp_pal_ansi_decode(const char* bytes, int32_t byteLen, char16_t* out, int32_t outCap)
{
    // The caller sizes `out` to the worst case (a UTF-8 run of N bytes -> <= N units),
    // so outCap is advisory here; the shared decoder writes exactly the returned count.
    (void)outCap;
    return dn2cpp_utf8_to_utf16(bytes, byteLen, out);
}

char16_t dn2cpp_pal_ansi_decode_char(uint8_t b)
{
    // Ansi == UTF-8 here: only 0x00-0x7F is a complete single-byte sequence; every
    // 0x80-0xFF byte is a lead/continuation byte that cannot stand alone -> U+FFFD.
    return b <= 0x7F ? static_cast<char16_t>(b) : static_cast<char16_t>(0xFFFD);
}

int dn2cpp_pal_executable_path(char* buf, size_t size)
{
    // A wasm instance is not a process image on a file system: there is no
    // /proc/self/exe and MEMFS holds no executable to point at. The embedder — a
    // browser or node — owns the only meaningful notion of "where this came from",
    // and it is a URL, not a path. Report "unavailable" and let the callers
    // degrade (AppContext.BaseDirectory -> "", Environment.ProcessPath -> null).
    (void)buf;
    (void)size;
    return -1;
}

void dn2cpp_pal_membarrier_processwide(void)
{
    // No cross-core IPI concept exists in a wasm instance: worker "threads" are
    // scheduled by the embedder, and the shared-memory model already makes a
    // seq_cst fence globally effective (wasm's atomic.fence is a single total
    // order over the shared memory). A plain full fence is the strongest — and
    // the only — barrier the target offers.
    __atomic_thread_fence(__ATOMIC_SEQ_CST);
}

void dn2cpp_pal_localtime(int64_t unixSeconds, std::tm* out)
{
    std::time_t secs = static_cast<std::time_t>(unixSeconds);
    localtime_r(&secs, out);
}

int64_t dn2cpp_pal_mktime_local(std::tm* localTm)
{
    return static_cast<int64_t>(std::mktime(localTm));
}

size_t dn2cpp_pal_malloc_usable_size(void* ptr)
{
    return malloc_usable_size(ptr);
}

int32_t dn2cpp_pal_backtrace(void** buf, int32_t max)
{
    // No stack walk exists on this target: -fwasm-exceptions exposes no
    // unwinder API to the module, and a release build carries no name section
    // anyway. Returning 0 is the declared degradation — the consumer stamps no
    // trace and Exception.StackTrace stays null on wasm.
    (void)buf;
    (void)max;
    return 0;
}

// ── Console ──────────────────────────────────────────────────────────────────
//
// Emscripten's musl stdio reaches the embedder's console (console.log under a
// browser, the process's fds under node), so this is the POSIX arm verbatim.

void dn2cpp_pal_console_write(int stream, const char* bytes, size_t byteCount)
{
    if (bytes == nullptr || byteCount == 0)
        return;
    std::fwrite(bytes, 1, byteCount, stream == DN2CPP_PAL_CONSOLE_ERR ? stderr : stdout);
}

void dn2cpp_pal_console_flush(void)
{
    std::fflush(nullptr);
}

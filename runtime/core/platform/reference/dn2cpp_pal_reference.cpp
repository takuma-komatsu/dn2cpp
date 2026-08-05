// dn2cpp_pal_reference.cpp — the reference implementation of the PAL seam.
//
// A complete implementation of platform/dn2cpp_pal.h that names no operating
// system: every entry is answered out of the C++17 standard library alone — no
// <unistd.h>, no <windows.h>, no <emscripten.h>, no syscall, no framework — so
// it builds anywhere a conforming hosted C++17 toolchain does. A porter copies
// this directory to platform/<target>/, adds the CMake arm (docs/PORTING.md
// §3.3), and replaces the bodies one at a time, starting from something that
// already compiles, links and runs.
//
// It is NOT a target and not a stand-in for a console port, and it is not "the
// POSIX PAL with the includes removed": an entry a hosted C++17 library cannot
// genuinely answer is answered LOUDLY rather than plausibly, since a plausible
// wrong answer is the failure this seam exists to avoid.
//
// ── How this target discharges each contract class ───────────────────────────
//
// The classification itself lives in the `// PAL-CONTRACT:` markers in
// platform/dn2cpp_pal.h (gates/build-and-run-doc-claims.sh diffs them against
// docs/PORTING.md §2.1). Here, only the three that are not answered plainly:
//
//   MUST, refused loudly — dn2cpp_pal_malloc_usable_size. C++17 has no
//     usable-size query and no sound guess exists: its caller (dn2cpp_gc.cpp's
//     portable aligned-realloc arm) uses the answer as a memcpy length, so a
//     fabricated 0 truncates every NativeMemory.AlignedRealloc and a fabricated
//     large value reads off the end of the block. A port that has not
//     implemented it must find out at the call, not from a corrupted heap.
//   MUST, answered weakly — dn2cpp_pal_membarrier_processwide, as a plain
//     seq_cst fence (the same weakening the wasm target documents). A correct
//     MemoryBarrier lacking only the asymmetric cross-thread strength; stated
//     because the caller has no arm for "this did nothing", so weak and absent
//     are indistinguishable from the call site.
//   MAY-DEGRADE — executable_path (-1), backtrace (0), default_locale_name (0),
//     each returning its documented sentinel.
//
// dn2cpp_pal_console_write defaults to stdio, so a program built here behaves
// like one built on the POSIX target; dn2cpp_pal_reference.h's hook replaces
// that sink.

#include "platform/dn2cpp_pal.h"
#include "platform/reference/dn2cpp_pal_reference.h"

#include <atomic>     // atomic_thread_fence (the process-wide barrier's weakening)
#include <cerrno>     // errno / EEXIST / ENOENT / ERANGE (the seam's error contract)
#include <cstdio>     // fwrite / fflush / fprintf (the default console sink)
#include <cstdlib>    // getenv / abort
#include <cstring>    // memcpy
#include <ctime>      // std::localtime / std::mktime / std::tm / std::time_t
#include <filesystem> // the file-system five, without an OS header
#include <system_error>

// The runtime's own UTF-8 codec cores, which the ANSI transforms delegate to
// exactly as the POSIX target does. Declared rather than included for the reason
// the seam exists: pulling dn2cpp_core.h in here would make the PAL depend on
// the runtime's public header, and the dependency runs the other way.
int32_t dn2cpp_utf16_to_utf8(const char16_t* src, int32_t srcLen, char* buf, int32_t bufSize);
int32_t dn2cpp_utf8_to_utf16(const char* bytes, int32_t byteLen, char16_t* out);

namespace fs = std::filesystem;

namespace
{
    // A MUST entry with no portable answer. Loud, immediate and attributable:
    // it names the function, says why there is no answer, and points at the file
    // a porter has to edit. It does NOT return — a refusal that returned would
    // be the silent wrong answer this file exists to refuse to give.
    [[noreturn]] void reference_unimplemented(const char* fn, const char* why)
    {
        std::fprintf(stderr,
            "dn2cpp: the reference PAL cannot answer %s: %s\n"
            "        This is a MUST entry of the seam (platform/dn2cpp_pal.h). A real\n"
            "        target implements it in its own platform/<target>/ copy of\n"
            "        runtime/core/platform/reference/dn2cpp_pal_reference.cpp.\n"
            "        See docs/PORTING.md section 2.1.\n",
            fn, why);
        std::fflush(nullptr);
        std::abort();
    }

    // std::error_code -> errno. The seam's error contract is errno-shaped
    // (ENOENT makes File.Delete a no-op, EEXIST makes CreateDirectory
    // idempotent), and generic_category's values ARE the <cerrno> constants on
    // every conforming implementation — which is what makes this portable rather
    // than a POSIX assumption wearing a std:: name.
    int errno_of(const std::error_code& ec)
    {
        if (!ec)
            return 0;
        std::error_condition cond = ec.default_error_condition();
        return cond.category() == std::generic_category() ? cond.value() : EIO;
    }

    Dn2CppPalReferenceConsoleSink g_sink = nullptr;
    void* g_sink_ctx = nullptr;
}

// ── File system ──────────────────────────────────────────────────────────────

char* dn2cpp_pal_getcwd(char* buf, size_t size)
{
    if (buf == nullptr || size == 0)
    {
        errno = EINVAL;
        return nullptr;
    }
    std::error_code ec;
    fs::path cwd = fs::current_path(ec);
    if (ec)
    {
        errno = errno_of(ec);
        return nullptr;
    }
    const std::string s = cwd.string();
    if (s.size() + 1 > size)
    {
        errno = ERANGE; // getcwd's documented too-small-buffer answer
        return nullptr;
    }
    std::memcpy(buf, s.c_str(), s.size() + 1);
    return buf;
}

int dn2cpp_pal_unlink(const char* path)
{
    if (path == nullptr)
    {
        errno = EINVAL;
        return -1;
    }
    // std::remove (from <cstdio>, not fs::remove) is used because it is the
    // standard's own errno-setting delete: the caller depends on ENOENT reaching
    // it unchanged so that File.Delete on a missing file is a no-op rather than
    // an exception, and fs::remove reports "did not exist" as `false` with an
    // EMPTY error_code — indistinguishable from a successful delete.
    //
    // The directory test in front of it is not redundant, and it is the kind of
    // divergence a port introduces without noticing: this seam entry is `unlink`,
    // which POSIX defines to refuse a directory, but C's `remove` is specified to
    // remove an EMPTY DIRECTORY as well. Without this guard the reference target
    // would silently delete a directory that every shipped target refuses — the
    // caller's own kind check (dn2cpp_system_io.cpp raises UnauthorizedAccess for
    // a directory) happens to cover it today, but a seam entry that relies on its
    // caller's validation is one that breaks the next caller.
    if (dn2cpp_pal_path_kind(path) == DN2CPP_PAL_PATH_DIR)
    {
        errno = EISDIR;
        return -1;
    }
    errno = 0;
    return std::remove(path) == 0 ? 0 : -1;
}

int dn2cpp_pal_mkdir(const char* path)
{
    if (path == nullptr)
    {
        errno = EINVAL;
        return -1;
    }
    std::error_code ec;
    if (fs::create_directory(fs::path(path), ec))
        return 0;
    if (ec)
    {
        errno = errno_of(ec);
        return -1;
    }
    // No error and no creation means the path was already there. POSIX mkdir
    // answers EEXIST for that, and Directory.CreateDirectory's idempotency is
    // built on exactly this value — collapsing it to a generic failure is one of
    // the three "wrong in a way that compiles" cases PORTING.md §2.1 names.
    errno = EEXIST;
    return -1;
}

int dn2cpp_pal_chdir(const char* path)
{
    if (path == nullptr)
    {
        errno = EINVAL;
        return -1;
    }
    std::error_code ec;
    fs::current_path(fs::path(path), ec);
    if (ec)
    {
        errno = errno_of(ec);
        return -1;
    }
    return 0;
}

int dn2cpp_pal_path_kind(const char* path)
{
    if (path == nullptr)
        return DN2CPP_PAL_PATH_MISSING;
    std::error_code ec;
    // fs::status, not symlink_status: the seam's contract is a single stat that
    // FOLLOWS symlinks, so a link to a directory answers DIR.
    const fs::file_status st = fs::status(fs::path(path), ec);
    if (ec || !fs::exists(st))
        return DN2CPP_PAL_PATH_MISSING;
    if (fs::is_regular_file(st))
        return DN2CPP_PAL_PATH_FILE;
    if (fs::is_directory(st))
        return DN2CPP_PAL_PATH_DIR;
    return DN2CPP_PAL_PATH_OTHER;
}

// ── Environment ──────────────────────────────────────────────────────────────

const char* dn2cpp_pal_getenv(const char* name)
{
    return name == nullptr ? nullptr : std::getenv(name);
}

// ── ANSI / system code page ──────────────────────────────────────────────────
//
// The reference target's default narrow encoding is UTF-8, matching POSIX and
// wasm: it is the only choice that needs no code-page table, and a target whose
// narrow encoding is something else is exactly the case PORTING.md's H5 warns
// about — get this wrong and nothing crashes, a P/Invoke just marshals different
// bytes. These three delegate to the runtime's codec cores, so the reference
// target's marshalling is byte-identical to the POSIX target's.

int32_t dn2cpp_pal_ansi_encode(const char16_t* src, int32_t srcLen, char* buf, int32_t bufSize, int32_t bestFit)
{
    (void)bestFit; // UTF-8 encodes every code unit; there is no unmappable case
    return dn2cpp_utf16_to_utf8(src, srcLen, buf, bufSize);
}

int32_t dn2cpp_pal_ansi_decode(const char* bytes, int32_t byteLen, char16_t* out, int32_t outCap)
{
    (void)outCap; // the caller sizes `out` to the worst case; the decoder writes the returned count
    return dn2cpp_utf8_to_utf16(bytes, byteLen, out);
}

char16_t dn2cpp_pal_ansi_decode_char(uint8_t b)
{
    // Only 0x00-0x7F is a complete single-byte UTF-8 sequence; every 0x80-0xFF
    // byte is a lead or continuation byte that cannot stand alone -> U+FFFD.
    return b <= 0x7F ? static_cast<char16_t>(b) : static_cast<char16_t>(0xFFFD);
}

// ── Locale ───────────────────────────────────────────────────────────────────

int32_t dn2cpp_pal_default_locale_name(char* buf, size_t size)
{
    // The declared degrade. A target-free implementation has no user to ask —
    // std::locale("") reads a host convention this file is defined not to know —
    // and 0 is the seam's documented "the host reports none", which the caller
    // reads as the invariant culture. That is the same answer the wasm target
    // gives unconditionally, and it is a real answer rather than a refusal: an
    // invariant default is correct behaviour, merely not localised.
    (void)buf;
    (void)size;
    return 0;
}

// ── Process ──────────────────────────────────────────────────────────────────

int dn2cpp_pal_executable_path(char* buf, size_t size)
{
    // The declared degrade: -1. C++17 offers no way to ask the loader where the
    // running image is, and argv[0] is not an acceptable substitute (a relative
    // invocation, a symlink or an exec-time rename each falsify it — the header
    // says so). Environment.ProcessPath answers null and AppContext.BaseDirectory
    // answers "", both of which the caller already handles.
    (void)buf;
    (void)size;
    return -1;
}

// ── Process-wide memory barrier ──────────────────────────────────────────────

void dn2cpp_pal_membarrier_processwide(void)
{
    // A plain sequentially-consistent fence — the same weakening the wasm target
    // documents, for the same reason: there is no portable way to make OTHER
    // threads execute a barrier, which is the asymmetric half of the contract.
    //
    // This is a correct Interlocked.MemoryBarrier and an incomplete
    // MemoryBarrierProcessWide. The distinction matters because the caller
    // (dn2cpp_system_threading.cpp) has no arm for a barrier that did nothing —
    // so a port must not "simplify" this to an empty body. An empty body is a
    // memory-model bug that no test on any machine reliably reproduces.
    std::atomic_thread_fence(std::memory_order_seq_cst);
}

// ── Time ─────────────────────────────────────────────────────────────────────

void dn2cpp_pal_localtime(int64_t unixSeconds, std::tm* out)
{
    if (out == nullptr)
        return;
    const std::time_t t = static_cast<std::time_t>(unixSeconds);
    // std::localtime is the C++17-portable spelling; localtime_r / localtime_s
    // are POSIX and CRT extensions respectively. It returns a pointer into a
    // shared static buffer, so the result is copied out immediately — a port
    // whose platform offers a reentrant variant should prefer it, and every
    // shipped target here does.
    const std::tm* tmp = std::localtime(&t);
    if (tmp == nullptr)
    {
        *out = std::tm{};
        return;
    }
    *out = *tmp;
}

int64_t dn2cpp_pal_mktime_local(std::tm* localTm)
{
    if (localTm == nullptr)
        return 0;
    return static_cast<int64_t>(std::mktime(localTm)); // normalises in place, as the seam requires
}

// ── Allocator ────────────────────────────────────────────────────────────────

size_t dn2cpp_pal_malloc_usable_size(void* ptr)
{
    (void)ptr;
    reference_unimplemented("dn2cpp_pal_malloc_usable_size",
        "C++17 has no usable-size query, and its caller uses the answer as a memcpy "
        "length, so any guess corrupts memory instead of degrading");
}

// ── Console ──────────────────────────────────────────────────────────────────

void dn2cpp_pal_console_write(int stream, const char* bytes, size_t byteCount)
{
    if (bytes == nullptr || byteCount == 0)
        return;
    if (g_sink != nullptr)
    {
        g_sink(stream, bytes, byteCount, g_sink_ctx);
        return;
    }
    std::fwrite(bytes, 1, byteCount, stream == DN2CPP_PAL_CONSOLE_ERR ? stderr : stdout);
}

void dn2cpp_pal_console_flush(void)
{
    // Only the default sink buffers. An installed sink is handed whole writes as
    // they happen and owns whatever it does with them, so there is nothing here
    // to commit on its behalf — a sink that buffers flushes in its own teardown.
    if (g_sink == nullptr)
        std::fflush(nullptr);
}

Dn2CppPalReferenceConsoleSink dn2cpp_reference_console_sink_install(
    Dn2CppPalReferenceConsoleSink sink, void* ctx)
{
    Dn2CppPalReferenceConsoleSink prev = g_sink;
    g_sink = sink;
    g_sink_ctx = ctx;
    return prev;
}

// ── Diagnostics ──────────────────────────────────────────────────────────────

int32_t dn2cpp_pal_backtrace(void** buf, int32_t max)
{
    // The declared degrade: 0. C++17 has no stack-walk API (<stacktrace> is
    // C++23), so the consumer stamps no trace and Exception.StackTrace stays
    // null — exactly as on wasm.
    (void)buf;
    (void)max;
    return 0;
}

// dn2cpp_pal_windows.cpp — Win32/CRT implementation of the Platform
// Abstraction Layer.
//
// Mirrors platform/posix/dn2cpp_pal_posix.cpp's contract exactly (see
// dn2cpp_pal.h) using native Win32 + Microsoft CRT calls: no libc `unistd.h`
// surface exists on this target. Where the CRT already offers a POSIX-shaped
// wrapper (`_getcwd`, `_unlink`, `_mkdir`, `_chdir`, `getenv`) this file uses
// it directly — same contract, different name — and only reaches for raw
// Win32 (`GetFileAttributesA`, `GetModuleFileNameA`) where no CRT equivalent
// exists or the CRT one is not precise enough. Two contract-shape gotchas
// called out where they occur: `localtime_s` takes its arguments in the
// opposite order from POSIX `localtime_r`, and `_msize` (not
// `malloc_usable_size`) is the MSVC CRT's usable-size query.
//
// dn2cpp_mmap_posix.cpp's Windows sibling lives alongside this file as
// dn2cpp_mmap_windows.cpp — not part of the zero-BCL console PAL surface
// this header describes, so it isn't covered here.

// NOMINMAX / WIN32_LEAN_AND_MEAN are project-wide compile definitions (see
// runtime/CMakeLists.txt's MSVC arm) rather than defined here, since any TU
// in this build can reach <windows.h> transitively (bdwgc's GC_WIN32_THREADS
// headers, this file, ...).
#include "platform/dn2cpp_pal.h"

#include <windows.h>

#include <cstdio>   // fwrite / fflush (console sink)
#include <cstdlib>  // getenv
#include <cstring>  // strlen / memcpy
#include <ctime>    // localtime_s / mktime / std::tm / std::time_t
#include <direct.h> // _getcwd / _mkdir / _chdir
#include <io.h>     // _unlink
#include <malloc.h> // _msize

char* dn2cpp_pal_getcwd(char* buf, size_t size)
{
    return ::_getcwd(buf, static_cast<int>(size));
}

int dn2cpp_pal_unlink(const char* path)
{
    return ::_unlink(path);
}

int dn2cpp_pal_mkdir(const char* path)
{
    return ::_mkdir(path);
}

int dn2cpp_pal_chdir(const char* path)
{
    return ::_chdir(path);
}

int dn2cpp_pal_path_kind(const char* path)
{
    // Real .NET's File.Exists/Directory.Exists only test the directory bit
    // (FileSystem.DirectoryExists / FileExists check FILE_ATTRIBUTE_DIRECTORY
    // alone), so a reparse point (symlink or junction) buckets by what it
    // points at, not by FILE_ATTRIBUTE_REPARSE_POINT: GetFileAttributesA
    // already reports the target's own directory bit for a reparse point
    // (it does not need to be separately dereferenced), matching the POSIX
    // side's stat() (which follows symlinks) exactly. Only a genuine device
    // (NUL, CON, ...) buckets as OTHER.
    DWORD attrs = ::GetFileAttributesA(path);
    if (attrs == INVALID_FILE_ATTRIBUTES)
        return DN2CPP_PAL_PATH_MISSING;
    if (attrs & FILE_ATTRIBUTE_DIRECTORY)
        return DN2CPP_PAL_PATH_DIR;
    if (attrs & FILE_ATTRIBUTE_DEVICE)
        return DN2CPP_PAL_PATH_OTHER;
    return DN2CPP_PAL_PATH_FILE;
}

const char* dn2cpp_pal_getenv(const char* name)
{
    return ::getenv(name);
}

// The user's default locale, already a BCP-47 name ("ja-JP") — no normalisation
// to do, unlike the POSIX ids. LANG / LC_ALL are deliberately NOT consulted:
// Windows has no such convention and real .NET does not read them there either,
// so honouring them would make dn2cpp answer a question the oracle it is diffed
// against cannot see. (That asymmetry is what gates/verify-culture-invariance.sh
// refuses to run on a Windows host over: the locale sweep it performs would
// compare four runs of the same locale and pass having asserted nothing.)
int32_t dn2cpp_pal_default_locale_name(char* buf, size_t size)
{
    if (buf == nullptr || size == 0)
        return 0;
    buf[0] = '\0';
    wchar_t wide[LOCALE_NAME_MAX_LENGTH] = {0};
    int n = ::GetUserDefaultLocaleName(wide, LOCALE_NAME_MAX_LENGTH);
    if (n <= 1) // 0 = failure; 1 = the terminating NUL alone (the invariant locale)
        return 0;
    int written = ::WideCharToMultiByte(CP_UTF8, 0, wide, n - 1, buf,
                                        static_cast<int>(size) - 1, nullptr, nullptr);
    if (written <= 0)
        return 0;
    buf[written] = '\0';
    return static_cast<int32_t>(written);
}

int32_t dn2cpp_pal_ansi_encode(const char16_t* src, int32_t srcLen, char* buf, int32_t bufSize, int32_t bestFit)
{
    if (src == nullptr || srcLen <= 0)
        return 0;
    // CP_ACP. dwFlags 0 enables best-fit substitution (é -> 'e'), matching real .NET's
    // default P/Invoke Ansi marshalling; WC_NO_BEST_FIT_CHARS disables it (unmappable ->
    // the default char '?'), matching Marshal.StringToHGlobalAnsi (bestFit:false).
    // lpDefaultChar / lpUsedDefaultChar NULL: an unmappable code unit falls back to the
    // code page's default char and never throws — .NET's ThrowOnUnmappableChar defaults
    // to false. wchar_t is UTF-16 on Windows, so the char16_t reinterpret is exact. A
    // null/zero `buf` returns the required byte count (size-query mode).
    DWORD flags = bestFit ? 0 : WC_NO_BEST_FIT_CHARS;
    int written = ::WideCharToMultiByte(
        CP_ACP, flags, reinterpret_cast<const wchar_t*>(src), srcLen,
        (buf != nullptr) ? buf : nullptr, (buf != nullptr) ? bufSize : 0,
        nullptr, nullptr);
    return written > 0 ? written : 0;
}

int32_t dn2cpp_pal_ansi_decode(const char* bytes, int32_t byteLen, char16_t* out, int32_t outCap)
{
    if (bytes == nullptr || byteLen <= 0)
        return 0;
    // CP_ACP decode; a null/zero `out` returns the required unit count (size-query
    // mode). Invalid byte sequences fall back to the replacement char, matching
    // real .NET's decode of an Ansi native buffer.
    int written = ::MultiByteToWideChar(
        CP_ACP, 0, bytes, byteLen,
        (out != nullptr) ? reinterpret_cast<wchar_t*>(out) : nullptr,
        (out != nullptr) ? outCap : 0);
    return written > 0 ? written : 0;
}

char16_t dn2cpp_pal_ansi_decode_char(uint8_t b)
{
    // Real .NET's Encoding.Default is UTF8Encoding on every platform (.NET Core
    // 3.0+), Windows included -- CP_ACP is not involved here. Only 0x00-0x7F is
    // a complete single-byte UTF-8 sequence; every 0x80-0xFF byte is a lead or
    // continuation byte that cannot stand alone -> U+FFFD.
    return b <= 0x7F ? static_cast<char16_t>(b) : static_cast<char16_t>(0xFFFD);
}

int dn2cpp_pal_executable_path(char* buf, size_t size)
{
    if (buf == nullptr || size == 0)
        return -1;

    // GetModuleFileNameA(NULL, ...) reports the current process's own image
    // path, already absolute and resolved (no argv[0]/relative-invocation/
    // symlink pitfalls) — the Win32 equivalent of Linux's /proc/self/exe.
    DWORD written = ::GetModuleFileNameA(nullptr, buf, static_cast<DWORD>(size));
    if (written == 0)
        return -1;
    // A truncated result (buffer too small) is not NUL-terminated on some
    // Windows versions and always signals ERROR_INSUFFICIENT_BUFFER; treat
    // both "exactly filled" and "overflowed" as failure so the caller never
    // reads a possibly-unterminated buffer.
    if (written >= static_cast<DWORD>(size))
        return -1;
    return 0;
}

void dn2cpp_pal_membarrier_processwide(void)
{
    // The OS call MemoryBarrierProcessWide binds to on real .NET: every thread of
    // this process observes a full barrier before this returns (kernel-issued IPI
    // to each core running one of our threads).
    ::FlushProcessWriteBuffers();
}

void dn2cpp_pal_localtime(int64_t unixSeconds, std::tm* out)
{
    std::time_t secs = static_cast<std::time_t>(unixSeconds);
    // Note the reversed argument order vs POSIX localtime_r(&secs, out): the
    // CRT's `_s` variant takes the destination first, the source second.
    ::localtime_s(out, &secs);
}

int64_t dn2cpp_pal_mktime_local(std::tm* localTm)
{
    return static_cast<int64_t>(std::mktime(localTm));
}

size_t dn2cpp_pal_malloc_usable_size(void* ptr)
{
    return ::_msize(ptr);
}

int32_t dn2cpp_pal_backtrace(void** buf, int32_t max)
{
    if (buf == nullptr || max <= 0)
        return 0;
    // RtlCaptureStackBackTrace is the documented user-mode capture: the kernel
    // walks its own frame data — no dbghelp, no symbols, async-safe. Skip 0
    // frames; the consumer drops the PAL's own frame with everything else it
    // cannot name. (The FramesToSkip + FramesToCapture < 64 ceiling was
    // Server 2003-era; current Windows accepts the full requested depth.)
    USHORT written = ::RtlCaptureStackBackTrace(0, static_cast<ULONG>(max), buf, nullptr);
#if defined(_M_X64) || defined(_M_ARM64)
    // Convert each return address to its function's ENTRY address, per the
    // PAL contract (dn2cpp_pal.h): the consumer exact-matches entries against
    // the generated method table, and only the platform's unwind data can say
    // which function a PC is inside. RtlLookupFunctionEntry reads the .pdata
    // unwind tables — present in every release binary on these targets,
    // because SEH/C++ unwinding itself rides them; no symbols involved, so
    // stripping changes nothing. pc-1 uniformly: every captured entry is a
    // return address (buf[0] is the return into this function), and -1 lands
    // it back inside the caller even when the call is its final instruction.
    for (USHORT i = 0; i < written; i++)
    {
        DWORD64 base = 0;
        PRUNTIME_FUNCTION fe = ::RtlLookupFunctionEntry(
            reinterpret_cast<DWORD64>(buf[i]) - 1, &base, nullptr);
#if defined(_M_X64)
        // x64 chained unwind info (UNW_FLAG_CHAININFO): the looked-up entry
        // may describe a FRAGMENT of a function; its primary RUNTIME_FUNCTION
        // — the one whose BeginAddress is the real entry the method table
        // holds — sits after the unwind codes (documented UNWIND_INFO layout:
        // byte 0 = version:3 | flags:5, byte 2 = code count, codes are
        // 2-byte slots padded to an even count). Bounded walk: a cycle here
        // would mean corrupt unwind data, and dropping the frame beats
        // spinning.
        for (int guard = 0; fe != nullptr && guard < 8; guard++)
        {
            const auto* ui = reinterpret_cast<const unsigned char*>(base + fe->UnwindData);
            if (((ui[0] >> 3) & UNW_FLAG_CHAININFO) == 0)
                break;
            fe = reinterpret_cast<PRUNTIME_FUNCTION>(const_cast<unsigned char*>(
                ui + 4 + ((ui[2] + 1u) & ~1u) * 2));
        }
#endif
        // ARM64 function fragments have no chain flag to follow; a fragment's
        // BeginAddress simply fails the exact match and the frame drops —
        // never misnames. A null entry (a true leaf, or a PC outside every
        // image) stores null and drops the same way.
        buf[i] = fe != nullptr ? reinterpret_cast<void*>(base + fe->BeginAddress) : nullptr;
    }
#else
    // No .pdata-equivalent function-bounds source on this target (x86 uses
    // FPO data no user API exposes): report nothing rather than hand the
    // consumer raw PCs it would have to guess about — the degrade the PAL
    // contract declares, same as WASM.
    written = 0;
#endif
    return static_cast<int32_t>(written);
}

// ── Console ──────────────────────────────────────────────────────────────────
//
// The CRT's stdio, exactly as the runtime called it inline before the seam
// existed. stdout/stderr are put into binary mode by dn2cpp_runtime_init, so the
// bytes handed over here reach the destination untranslated — which is what
// keeps a "\r\n" line terminator one CRLF rather than two.

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

// dn2cpp_pal_posix.cpp — POSIX implementation of the Platform Abstraction Layer.
//
// macOS / Linux / BSD. Each function forwards to the host syscall the intrinsics
// used inline before the seam was introduced, so the macOS gate suite stays
// behaviour-identical. A Windows / WASM port adds a sibling implementation file
// under platform/windows/ or platform/wasm/ without touching the intrinsics.

#include "platform/dn2cpp_pal.h"

#include <climits>    // PATH_MAX
#include <cstdint>    // uint32_t / uintptr_t
#include <cstdio>     // snprintf (default-locale name assembly); fwrite/fflush (console sink)
#include <cstdlib>    // getenv / realpath
#include <cstring>    // strlen / memcpy / strcmp / strncmp
#include <ctime>      // localtime_r / mktime / std::tm / std::time_t
#include <mutex>      // std::mutex (membarrier fallback serialization)
#include <vector>     // PATH_MAX scratch off the stack (dn2cpp_pal_executable_path)
#include <unistd.h>   // getcwd / unlink / chdir / readlink / sysconf
#include <unwind.h>   // _Unwind_Backtrace / _Unwind_GetIP / _Unwind_GetRegionStart (backtrace capture)
#include <dlfcn.h>    // dladdr — the image holding the runtime (frame-entry derivation)
#include <sys/mman.h> // mmap / mprotect (membarrier fallback)
#include <sys/stat.h> // stat / S_ISREG / S_ISDIR / mkdir

// macOS exposes malloc_size; glibc/musl/BSD expose malloc_usable_size.
#if defined(__APPLE__)
#include <malloc/malloc.h>
#include <mach-o/dyld.h>   // _NSGetExecutablePath
#include <mach-o/loader.h> // LC_FUNCTION_STARTS parse (frame-entry derivation)
#include <CoreFoundation/CoreFoundation.h> // CFLocale (default-locale fallback)
#else
#include <malloc.h>
#endif

#if defined(__linux__)
#include <sys/syscall.h> // __NR_membarrier
#endif

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

// ── Locale ───────────────────────────────────────────────────────────────────
//
// Rewrite a POSIX locale id into the name real .NET reports for it: strip the
// codeset after '.', turn '_' into '-', and carry an '@' modifier over as an
// uppercased trailing subtag ("de_AT.UTF-8@euro" -> "de-AT-EURO"). Returns 0 for
// an id that names no locale.
//
// The four shapes above plus the C/POSIX rejection are read off real .NET's own
// behaviour. dn2cpp does not resolve the name any further — an id the culture
// table does not carry keeps its name over invariant symbols, the same answer
// `new CultureInfo(thatName)` already gives.
static int32_t dn2cpp_pal_locale_from_posix_id(const char* id, char* buf, size_t size)
{
    if (id == nullptr || id[0] == '\0')
        return 0;
    // "C" / "POSIX" / "C.UTF-8" name the absence of a locale. Real .NET rejects
    // them rather than honouring them, and — measured — does NOT then fall
    // through to the next variable in the list: an LC_ALL of "C" makes a LANG of
    // "fr_FR.UTF-8" invisible. So the caller stops at the first variable that is
    // SET, and this function decides only whether that one names a locale.
    if (std::strcmp(id, "C") == 0 || std::strcmp(id, "POSIX") == 0 || std::strncmp(id, "C.", 2) == 0)
        return 0;

    size_t n = 0;
    size_t i = 0;
    for (; id[i] != '\0' && id[i] != '.' && id[i] != '@'; i++)
    {
        if (n + 1 >= size)
            return 0;
        buf[n++] = id[i] == '_' ? '-' : id[i];
    }
    // Skip the codeset, keep the modifier.
    while (id[i] != '\0' && id[i] != '@')
        i++;
    if (id[i] == '@' && id[i + 1] != '\0')
    {
        if (n + 1 >= size)
            return 0;
        buf[n++] = '-';
        for (i++; id[i] != '\0'; i++)
        {
            if (n + 1 >= size)
                return 0;
            char c = id[i];
            buf[n++] = (c >= 'a' && c <= 'z') ? static_cast<char>(c - 'a' + 'A') : c;
        }
    }
    if (n == 0)
        return 0;
    buf[n] = '\0';
    return static_cast<int32_t>(n);
}

#if defined(__APPLE__)
// The user's system-preference locale, as `<language>-<country>`. This is the
// shape real .NET's DetectDefaultAppleLocaleName builds out of NSLocale, and
// CFLocale is the C face of that same object — so the two agree without this
// file taking an Objective-C dependency. CoreFoundation is already a PUBLIC link
// of dn2cpp_runtime on Apple (runtime/CMakeLists.txt), so this costs no new one.
//
// CFLocale does NOT read LANG/LC_ALL — it answers the user's preference whatever
// the environment says. So it belongs here, as the fallback for an environment
// that names no locale, and never ahead of the environment scan:
// `LANG=de_DE.UTF-8` must move the answer.
static int32_t dn2cpp_pal_locale_from_cf(char* buf, size_t size)
{
    CFLocaleRef loc = ::CFLocaleCopyCurrent();
    if (loc == nullptr)
        return 0;
    int32_t written = 0;
    auto lang = static_cast<CFStringRef>(::CFLocaleGetValue(loc, kCFLocaleLanguageCode));
    auto country = static_cast<CFStringRef>(::CFLocaleGetValue(loc, kCFLocaleCountryCode));
    char langBuf[64] = {0};
    char countryBuf[64] = {0};
    bool haveLang = lang != nullptr
        && ::CFStringGetCString(lang, langBuf, sizeof(langBuf), kCFStringEncodingUTF8) && langBuf[0] != '\0';
    bool haveCountry = country != nullptr
        && ::CFStringGetCString(country, countryBuf, sizeof(countryBuf), kCFStringEncodingUTF8)
        && countryBuf[0] != '\0';
    if (haveLang && haveCountry)
    {
        size_t need = std::strlen(langBuf) + 1 + std::strlen(countryBuf);
        if (need + 1 <= size)
        {
            std::snprintf(buf, size, "%s-%s", langBuf, countryBuf);
            written = static_cast<int32_t>(need);
        }
    }
    else
    {
        // No country: fall back to the raw identifier, as real .NET does.
        auto ident = ::CFLocaleGetIdentifier(loc);
        char identBuf[128] = {0};
        if (ident != nullptr && ::CFStringGetCString(ident, identBuf, sizeof(identBuf), kCFStringEncodingUTF8))
            written = dn2cpp_pal_locale_from_posix_id(identBuf, buf, size);
    }
    ::CFRelease(loc);
    return written;
}
#endif

int32_t dn2cpp_pal_default_locale_name(char* buf, size_t size)
{
    if (buf == nullptr || size == 0)
        return 0;
    buf[0] = '\0';
    // ICU's uprv_getPOSIXIDForCategory order, which is what real .NET ends up
    // resolving through on POSIX: the FIRST of these that is set decides, set to
    // "C" included.
    static const char* const kVars[] = { "LC_ALL", "LC_MESSAGES", "LANG" };
    for (const char* v : kVars)
    {
        const char* id = ::getenv(v);
        if (id == nullptr || id[0] == '\0')
            continue;
        return dn2cpp_pal_locale_from_posix_id(id, buf, size);
    }
#if defined(__APPLE__)
    return dn2cpp_pal_locale_from_cf(buf, size);
#else
    // No CFLocale equivalent to ask. Real .NET reaches ICU's "en_US_POSIX"
    // default here, a locale whose formatting IS the invariant one; answering 0
    // (invariant, empty name) differs from it only in the NAME, and only on a
    // host that has declined to state a locale in the first place.
    return 0;
#endif
}

// The runtime's raw UTF-8 codec cores (defined in intrinsics/dn2cpp_system_reflection.cpp).
// Forward-declared here rather than pulling in the whole runtime header: the POSIX Ansi
// seam is exactly UTF-8, so it delegates byte-for-byte to these — keeping the Unix Ansi
// marshalling output identical to the pre-seam UTF-8 behaviour.
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
    // Ansi == UTF-8 on Unix: only 0x00-0x7F is a complete single-byte sequence; every
    // 0x80-0xFF byte is a lead/continuation byte that cannot stand alone -> U+FFFD.
    return b <= 0x7F ? static_cast<char16_t>(b) : static_cast<char16_t>(0xFFFD);
}

int dn2cpp_pal_executable_path(char* buf, size_t size)
{
    if (buf == nullptr || size == 0)
        return -1;

    // PATH_MAX-sized scratch on the heap, not the stack. PATH_MAX is 1024 on
    // macOS and 4096 on Linux, so the identical source below takes a ~2 KiB frame
    // on one host and a ~12 KiB one on the other — the per-target frame growth
    // DN2CPP_MAX_STACK_FRAME exists to catch, and the reason a stack-buffer audit
    // cannot be done by reading the source on one machine. This runs once
    // (dn2cpp_system_io.cpp caches the answer), so the allocation is free.
    std::vector<char> scratch(PATH_MAX);
#if defined(__APPLE__)
    // _NSGetExecutablePath reports the path as invoked: it may be relative and may
    // run through symlinks, so canonicalise it. realpath failing (the image was
    // unlinked) leaves the as-invoked path, which still beats nothing.
    char* raw = scratch.data();
    uint32_t rawSize = static_cast<uint32_t>(scratch.size());
    if (_NSGetExecutablePath(raw, &rawSize) != 0)
        return -1;
    std::vector<char> canonicalBuf(PATH_MAX);
    const char* path = ::realpath(raw, canonicalBuf.data()) != nullptr ? canonicalBuf.data() : raw;
#else
    // Linux / Android: the kernel already hands back an absolute, resolved path.
    char* link = scratch.data();
    ssize_t n = ::readlink("/proc/self/exe", link, scratch.size() - 1);
    if (n <= 0)
        return -1;
    link[n] = '\0';
    const char* path = link;
#endif

    size_t len = std::strlen(path);
    if (len + 1 > size)
        return -1;
    std::memcpy(buf, path, len + 1);
    return 0;
}

void dn2cpp_pal_membarrier_processwide(void)
{
#if defined(__linux__) && defined(__NR_membarrier)
    // membarrier(MEMBARRIER_CMD_GLOBAL): every thread of every process observes a
    // full barrier before the call returns — a strict superset of the process-wide
    // contract. Value 1 == MEMBARRIER_CMD_GLOBAL (né MEMBARRIER_CMD_SHARED); spelled
    // numerically so old kernel headers without <linux/membarrier.h> still compile.
    // ENOSYS (pre-4.3 kernel / seccomp) falls through to the portable fallback.
    if (::syscall(__NR_membarrier, /*MEMBARRIER_CMD_GLOBAL*/ 1, 0) == 0)
        return;
#endif
    // Portable fallback (macOS; Linux without membarrier): bounce a dedicated
    // anonymous page's protection. The mprotect downgrade to PROT_NONE must
    // invalidate the page's TLB entry on every core currently running a thread of
    // this process, and that inter-processor interrupt serializes each such core —
    // i.e. every other thread observes a full barrier, which is exactly the
    // FlushProcessWriteBuffers contract. The page is process-lifetime (never
    // unmapped); a static mutex serializes concurrent barrier requests.
    static std::mutex mtx;
    std::lock_guard<std::mutex> lk(mtx);
    static const size_t pageSize = static_cast<size_t>(::sysconf(_SC_PAGESIZE));
    static void* page = []() -> void* {
        void* p = ::mmap(nullptr, pageSize, PROT_NONE, MAP_PRIVATE | MAP_ANONYMOUS, -1, 0);
        return p == MAP_FAILED ? nullptr : p;
    }();
    if (page == nullptr)
    {
        // mmap failed (out of address space?): degrade to a plain full fence on
        // this thread rather than crash — still a correct MemoryBarrier, just not
        // the cross-thread asymmetric strength.
        __atomic_thread_fence(__ATOMIC_SEQ_CST);
        return;
    }
    ::mprotect(page, pageSize, PROT_READ | PROT_WRITE);
    *static_cast<volatile char*>(page) = 1; // make the mapping resident + dirty
    ::mprotect(page, pageSize, PROT_NONE);  // the downgrade IPIs the TLB shootdown
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
#if defined(__APPLE__)
    return malloc_size(ptr);
#else
    return malloc_usable_size(ptr);
#endif
}

namespace
{
#if defined(__APPLE__)
    // ── Frame-entry derivation, macOS ────────────────────────────────────────
    //
    // The contract wants each frame's function ENTRY address, and on macOS the
    // one source that answers exactly is LC_FUNCTION_STARTS: the linker-emitted
    // list of every function start in the image (ULEB128 deltas from __TEXT),
    // kept by `strip` — it lives beside the other LINKEDIT metadata the loader
    // needs — so it works in the default stripped gate binaries where dladdr
    // has no symbols left to answer with.
    //
    // _Unwind_GetRegionStart is NOT that source here: ld64's compact-unwind
    // emission OMITS the entry of a function whose encoding equals its
    // predecessor's, so libunwind reports the predecessor's start and a runtime
    // frame comes back named as the managed body linked just before it. On ELF
    // targets (the #else arm) there is no compact unwind and every function has
    // its own FDE, so the same call is exact.
    //
    // Built once, lazily, on the first capture (a program that never throws
    // never pays), under std::call_once. A parse failure — no LC_FUNCTION_STARTS
    // (a -no_function_starts link), an unexpected header — leaves the table
    // empty and every lookup answers null: frames DROP, they are never
    // misnamed, and the failure surfaces as an empty trace rather than a
    // plausible wrong one.
    struct Dn2CppMachOFnStarts
    {
        std::vector<uintptr_t> starts; // ascending slid entry addresses
        uintptr_t textLo = 0;          // slid [__TEXT,__text) range: only PCs in
        uintptr_t textHi = 0;          // here can belong to a listed function
    };

    const Dn2CppMachOFnStarts& dn2cpp_macho_fn_starts()
    {
        static Dn2CppMachOFnStarts table;
        static std::once_flag once;
        std::call_once(once, []
        {
            // The image that holds the runtime is the image that holds the
            // generated bodies — they are always linked together — and dladdr's
            // image-level answer (dli_fbase) survives stripping; only its
            // symbol-level answer does not.
            Dl_info di{};
            if (dladdr(reinterpret_cast<void*>(&dn2cpp_pal_backtrace), &di) == 0
                || di.dli_fbase == nullptr)
                return;
            const auto* hdr = static_cast<const mach_header_64*>(di.dli_fbase);
            if (hdr->magic != MH_MAGIC_64)
                return;
            uintptr_t textVm = 0, linkVm = 0;
            uint64_t linkFileOff = 0;
            uint64_t fsOff = 0, fsSize = 0;
            uintptr_t textSecLo = 0, textSecHi = 0;
            bool haveText = false, haveLink = false;
            const auto* p = reinterpret_cast<const uint8_t*>(hdr + 1);
            for (uint32_t i = 0; i < hdr->ncmds; i++)
            {
                const auto* lc = reinterpret_cast<const load_command*>(p);
                if (lc->cmd == LC_SEGMENT_64)
                {
                    const auto* seg = reinterpret_cast<const segment_command_64*>(p);
                    if (std::strncmp(seg->segname, "__TEXT", 16) == 0)
                    {
                        textVm = static_cast<uintptr_t>(seg->vmaddr);
                        haveText = true;
                        const auto* sec = reinterpret_cast<const section_64*>(seg + 1);
                        for (uint32_t s = 0; s < seg->nsects; s++, sec++)
                            if (std::strncmp(sec->sectname, "__text", 16) == 0)
                            {
                                textSecLo = static_cast<uintptr_t>(sec->addr);
                                textSecHi = static_cast<uintptr_t>(sec->addr + sec->size);
                            }
                    }
                    else if (std::strncmp(seg->segname, "__LINKEDIT", 16) == 0)
                    {
                        linkVm = static_cast<uintptr_t>(seg->vmaddr);
                        linkFileOff = seg->fileoff;
                        haveLink = true;
                    }
                }
                else if (lc->cmd == LC_FUNCTION_STARTS)
                {
                    const auto* led = reinterpret_cast<const linkedit_data_command*>(p);
                    fsOff = led->dataoff;
                    fsSize = led->datasize;
                }
                p += lc->cmdsize;
            }
            if (!haveText || !haveLink || fsSize == 0 || textSecHi == 0)
                return;
            uintptr_t slide = reinterpret_cast<uintptr_t>(hdr) - textVm;
            const auto* data = reinterpret_cast<const uint8_t*>(
                slide + linkVm + static_cast<uintptr_t>(fsOff - linkFileOff));
            // ULEB128 deltas, first from __TEXT's start, 0-delta terminates.
            uintptr_t addr = textVm;
            uint64_t val = 0;
            uint32_t shift = 0;
            for (uint64_t k = 0; k < fsSize; k++)
            {
                uint8_t b = data[k];
                val |= static_cast<uint64_t>(b & 0x7f) << shift;
                shift += 7;
                if ((b & 0x80) != 0)
                    continue;
                if (val == 0)
                    break;
                addr += static_cast<uintptr_t>(val);
                table.starts.push_back(addr + slide);
                val = 0;
                shift = 0;
            }
            table.textLo = textSecLo + slide;
            table.textHi = textSecHi + slide;
        });
        return table;
    }

    // Entry address of the function containing `pc`, or 0 for a PC outside
    // this image's __text (another image's frame is never a table body) or
    // when the table could not be built. The greatest-start-at-or-below rule
    // is EXACT here — unlike against the method table — because this list
    // carries every function in the section, so the next start is the upper
    // bound of the previous function.
    uintptr_t dn2cpp_macho_fn_entry(uintptr_t pc)
    {
        const Dn2CppMachOFnStarts& t = dn2cpp_macho_fn_starts();
        if (t.starts.empty() || pc < t.textLo || pc >= t.textHi)
            return 0;
        size_t lo = 0, hi = t.starts.size();
        while (lo < hi)
        {
            size_t mid = (lo + hi) / 2;
            if (t.starts[mid] <= pc)
                lo = mid + 1;
            else
                hi = mid;
        }
        return lo > 0 ? t.starts[lo - 1] : 0;
    }
#endif // __APPLE__

    struct Dn2CppBacktraceState
    {
        void** buf;
        int32_t max;
        int32_t count;
    };

    _Unwind_Reason_Code dn2cpp_backtrace_frame(struct _Unwind_Context* ctx, void* arg)
    {
        Dn2CppBacktraceState* st = static_cast<Dn2CppBacktraceState*>(arg);
        if (st->count >= st->max)
            return _URC_END_OF_STACK;
        uintptr_t ip = _Unwind_GetIP(ctx);
        if (ip == 0)
            return _URC_NO_REASON;
#if defined(__APPLE__)
        // ip is a RETURN address for every frame but the innermost; -1 lands
        // it back inside the caller even when the call is the function's final
        // instruction (dn2cpp_throw is [[noreturn]], so a trailing call is the
        // COMMON case on the frame just above it). The innermost frame is the
        // capture's own and drops regardless.
        uintptr_t entry = dn2cpp_macho_fn_entry(ip - 1);
#else
        // Exact on ELF: per-function FDEs, and the unwinder already did the
        // return-address correction when it located this frame's FDE.
        uintptr_t entry = _Unwind_GetRegionStart(ctx);
#endif
        st->buf[st->count++] = reinterpret_cast<void*>(entry);
        return _URC_NO_REASON;
    }
} // namespace

int32_t dn2cpp_pal_backtrace(void** buf, int32_t max)
{
    // The unwind tables this walks always exist here: the runtime compiles
    // with C++ exceptions on, and _Unwind_Backtrace is the same libunwind /
    // libgcc machinery a throw itself rides. Each stored entry is the frame's
    // function ENTRY address per the PAL contract (dn2cpp_pal.h), not its PC.
    if (buf == nullptr || max <= 0)
        return 0;
    Dn2CppBacktraceState st{ buf, max, 0 };
    _Unwind_Backtrace(dn2cpp_backtrace_frame, &st);
    return st.count;
}

// ── Console ──────────────────────────────────────────────────────────────────
//
// On a POSIX host the console IS stdio, so these two are the `fwrite` /
// `fflush(nullptr)` calls the runtime made inline before the seam existed —
// byte-identical, and deliberately so: the seam is for the targets that have no
// stdout, not a behaviour change for the ones that do.

void dn2cpp_pal_console_write(int stream, const char* bytes, size_t byteCount)
{
    if (bytes == nullptr || byteCount == 0)
        return;
    std::fwrite(bytes, 1, byteCount, stream == DN2CPP_PAL_CONSOLE_ERR ? stderr : stdout);
}

void dn2cpp_pal_console_flush(void)
{
    // fflush(nullptr) flushes every open stream, not only the two console ones.
    // That is what the call sites want: they run on the exit and abort paths,
    // where a half-written FileStream is lost output too.
    std::fflush(nullptr);
}

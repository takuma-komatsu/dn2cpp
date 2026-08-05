// dn2cpp_system_io.cpp — intrinsic implementations for System.IO + System.Environment.
//
// Libc-backed replacements for the real System.IO.Path / File / Directory and
// System.Environment bodies, whose real IL pulls in the
// ArrayPool / EventSource / Tracing / Sys.* P/Invoke cascade. Semantics probed
// against real .NET — see the
// build-and-run-{console-io,file-subset,directory-subset,env-subset}.sh gates.
//
// All public declarations live in the shared dn2cpp.h; the file-local static
// helpers (dn2cpp_path_last_sep / dn2cpp_path_to_utf8 / dn2cpp_file_read_all /
// dn2cpp_directory_create_one) are used only within this translation unit.

#include "dn2cpp_core.h"
#include "platform/dn2cpp_pal.h" // getcwd/unlink/mkdir/chdir/stat-kind/getenv via the PAL seam

#include <string>     // managed path strings as NUL-terminated UTF-8 std::string
#include <vector>     // Path.GetFullPath segment stack / Path.Combine buffer
#include <cstring>    // std::memcpy / std::strlen
#include <cstdio>     // File read/write (fopen/fread/fwrite/fclose)
#include <cerrno>     // errno / ENOENT / EEXIST (preserved across the PAL fs calls)
#if defined(_WIN32)
#include <windows.h>  // GetFullPathNameW/GetLongPathNameW — Windows Path.GetFullPath (drive/UNC-aware, 8.3 expansion); winreg.h — Environment's User/Machine registry read
#include <algorithm>  // std::find — scan the normalized path for an 8.3 '~' component
// No #pragma comment(lib, "advapi32.lib") here: advapi32 (RegOpenKeyExW /
// RegQueryValueExW / RegCloseKey, below) IS in CMake's default Windows link set
// on both arms (Windows-MSVC.cmake and Windows-GNU.cmake alike), so the pragma
// would be redundant under cl.exe — and inert under MinGW, which reaches this
// _WIN32 block too but implements no `#pragma comment(lib)` (GCC warns and links
// nothing). Anything genuinely outside that default set is linked where the link
// mechanism lives, runtime/CMakeLists.txt's WIN32 arm — see ws2_32 there.
#endif

// The buffer size the getcwd sites hand the PAL. Growing it changes which working
// directories dn2cpp_throw_getcwd_failure's ERANGE arm reports on. Written once
// here rather than twice as a literal so the two sites cannot drift apart.
//
// It is NOT PATH_MAX: PATH_MAX is a POSIX header constant this file must build
// without (it is compiled for Windows and wasm too), and it is per-target — 1024
// on macOS, 4096 on Linux — so keying a shared buffer on it would make the same
// source allocate differently per host for no reason the callers care about.
constexpr size_t kDn2cppMaxPathBytes = 4096;

// A failed getcwd(2), raised the way real .NET raises it.
//
// .NET routes the failure through Interop.CheckIo, which picks the exception type
// FROM ERRNO — it is not a flat IOException. A process whose own cwd has been
// rmdir'd gets ENOENT, and Path.GetFullPath("rel"), Environment.CurrentDirectory
// and Directory.GetCurrentDirectory each answer FileNotFoundException. That type
// derives from IOException here as it does in .NET, so a `catch (IOException)`
// still matches.
//
// EACCES gets UnauthorizedAccessException (Interop.CheckIo's other named arm) — a
// directory whose search permission was revoked after the process entered it.
// Everything else, ERANGE above all, keeps the plain IOException: a cwd longer
// than the buffer is not a missing file. ERANGE is a divergence in its own right
// — real .NET grows its buffer and succeeds where the fixed size below gives up —
// so the throw reports a limit rather than claiming to match .NET there.
[[noreturn]] static void dn2cpp_throw_getcwd_failure()
{
    if (errno == ENOENT)
        dn2cpp_throw_of(&dn2cpp_file_not_found_exception_type);
    if (errno == EACCES)
        dn2cpp_throw_of(&dn2cpp_unauthorized_access_exception_type);
    dn2cpp_throw_of(&dn2cpp_io_exception_type);
}

// ─── System.IO.Path (pure lexical) ──────────────────────────────────────────
// Path operations are ASCII-delimited ('/' and '.'), so the substring/scan
// helpers work directly on char16_t. Semantics probed against real .NET
// and matched exactly (see gates/build-and-run-console-io.sh).
//
// Separator policy is platform-specific, matching .NET's PathInternal: on
// Windows both '/' (AltDirectorySeparatorChar) and '\' (DirectorySeparatorChar)
// are recognized as separators when scanning, and '\' is emitted when joining
// (so Path.Combine agrees with Path.DirectorySeparatorChar and the Win32
// directory-enumeration PAL, which both use '\'); on POSIX only '/' is a
// separator, unchanged. This is the natural completion of the Windows path
// PAL that dn2cpp_path_get_full_path (GetFullPathNameW, below) already began.
#if defined(_WIN32)
static constexpr char16_t kDn2CppDirSep = u'\\';
static inline bool dn2cpp_is_dir_sep(char16_t c) { return c == u'/' || c == u'\\'; }
static inline bool dn2cpp_is_dir_sep(char c) { return c == '/' || c == '\\'; }
#else
static constexpr char16_t kDn2CppDirSep = u'/';
static inline bool dn2cpp_is_dir_sep(char16_t c) { return c == u'/'; }
static inline bool dn2cpp_is_dir_sep(char c) { return c == '/'; }
#endif

// Index of the last separator in s, or -1.
static int32_t dn2cpp_path_last_sep(const Dn2CppString* s)
{
    for (int32_t i = s->length - 1; i >= 0; i--)
        if (dn2cpp_is_dir_sep(s->chars[i])) return i;
    return -1;
}

#if defined(_WIN32)
// PathInternal.IsValidDriveChar: only an ASCII letter qualifies as a drive, so
// "1:foo" stays an ordinary relative path (as in real .NET) instead of gaining
// a two-char root.
static inline bool dn2cpp_is_drive_letter(char16_t c)
{
    return (c >= u'A' && c <= u'Z') || (c >= u'a' && c <= u'z');
}
#endif

int32_t dn2cpp_path_is_rooted(Dn2CppString* p)
{
    if (p == nullptr || p->length == 0) return 0;
    if (dn2cpp_is_dir_sep(p->chars[0])) return 1;
#if defined(_WIN32)
    // A drive qualifier ("C:") also roots a Windows path (PathInternal.IsPathRooted).
    if (p->length >= 2 && dn2cpp_is_drive_letter(p->chars[0]) && p->chars[1] == u':') return 1;
#endif
    return 0;
}

Dn2CppString* dn2cpp_path_get_filename(Dn2CppString* p)
{
    if (p == nullptr) return nullptr;
    int32_t start = dn2cpp_path_last_sep(p) + 1; // 0 when no separator
    return dn2cpp_string_from_chars(p->chars + start, p->length - start);
}

Dn2CppString* dn2cpp_path_get_directory_name(Dn2CppString* p)
{
    // Mirrors .NET GetDirectoryNameOffset: null for null/empty/root, else the
    // span up to (excluding) the last separator, trailing separators trimmed.
    if (p == nullptr || p->length == 0) return nullptr;
    int32_t rootLen = dn2cpp_is_dir_sep(p->chars[0]) ? 1 : 0;
#if defined(_WIN32)
    // A drive qualifier ("C:", "C:\") extends the root span past the simple
    // leading-separator case above (mirrors PathInternal.GetRootLength's
    // drive-rooted branch, same letter+':' check as dn2cpp_path_is_rooted):
    // "C:\" is root-only (-> null), "C:\foo"'s root is "C:\" (-> "C:\"), and
    // the drive-relative "C:foo" (no separator after the colon) roots at "C:".
    if (rootLen == 0 && p->length >= 2 && dn2cpp_is_drive_letter(p->chars[0]) && p->chars[1] == u':')
        rootLen = (p->length > 2 && dn2cpp_is_dir_sep(p->chars[2])) ? 3 : 2;
#endif
    int32_t end = p->length;
    if (end <= rootLen) return nullptr;
    while (end > rootLen && !dn2cpp_is_dir_sep(p->chars[end - 1])) end--;
    while (end > rootLen && dn2cpp_is_dir_sep(p->chars[end - 1])) end--;
#if defined(_WIN32)
    // Real .NET's GetDirectoryName runs the trimmed span through
    // PathInternal.NormalizeDirectorySeparators before returning it, which
    // substitutes the alternate separator ('/') for the primary one ('\') —
    // verified against the real-.NET oracle (Path.GetDirectoryName("/a/b/c.txt")
    // returns "\a\b", not "/a/b", in the console-io gate). POSIX has only one
    // separator, so this is a no-op there and the plain slice below is exact.
    std::vector<char16_t> buf(p->chars, p->chars + end);
    for (char16_t& c : buf)
        if (c == u'/') c = kDn2CppDirSep;
    return dn2cpp_string_from_chars(buf.data(), end);
#else
    return dn2cpp_string_from_chars(p->chars, end);
#endif
}

Dn2CppString* dn2cpp_path_get_extension(Dn2CppString* p)
{
    if (p == nullptr) return nullptr;
    for (int32_t i = p->length - 1; i >= 0; i--)
    {
        char16_t ch = p->chars[i];
        if (ch == u'.')
            // A dot that is the last char yields no extension (e.g. "a." -> "").
            return (i != p->length - 1)
                ? dn2cpp_string_from_chars(p->chars + i, p->length - i)
                : dn2cpp_string_from_chars(p->chars, 0);
        if (dn2cpp_is_dir_sep(ch)) break;
    }
    return dn2cpp_string_from_chars(p->chars, 0);
}

Dn2CppString* dn2cpp_path_get_filename_without_extension(Dn2CppString* p)
{
    if (p == nullptr) return nullptr;
    int32_t start = dn2cpp_path_last_sep(p) + 1;
    int32_t end = p->length;
    for (int32_t i = p->length - 1; i >= start; i--)
        if (p->chars[i] == u'.') { end = i; break; }
    return dn2cpp_string_from_chars(p->chars + start, end - start);
}

Dn2CppString* dn2cpp_path_combine2(Dn2CppString* a, Dn2CppString* b)
{
    if (a == nullptr || b == nullptr) dn2cpp_throw_argument_null();
    if (dn2cpp_path_is_rooted(b)) return b; // b rooted -> b wins
    if (a->length == 0) return b;
    if (b->length == 0) return a;
    bool sep = dn2cpp_is_dir_sep(a->chars[a->length - 1]);
    std::vector<char16_t> buf;
    buf.reserve(static_cast<size_t>(a->length) + 1 + b->length);
    buf.insert(buf.end(), a->chars, a->chars + a->length);
    if (!sep) buf.push_back(kDn2CppDirSep);
    buf.insert(buf.end(), b->chars, b->chars + b->length);
    return dn2cpp_string_from_chars(buf.data(), static_cast<int32_t>(buf.size()));
}

Dn2CppString* dn2cpp_path_combine3(Dn2CppString* a, Dn2CppString* b, Dn2CppString* c)
{
    return dn2cpp_path_combine2(dn2cpp_path_combine2(a, b), c);
}

Dn2CppString* dn2cpp_path_combine4(Dn2CppString* a, Dn2CppString* b, Dn2CppString* c, Dn2CppString* d)
{
    return dn2cpp_path_combine2(dn2cpp_path_combine2(dn2cpp_path_combine2(a, b), c), d);
}

#if defined(_WIN32)
// A GetFullPathNameW refusal, raised the way real .NET raises it: PathHelper hands
// the Win32 error to Win32Marshal.GetExceptionForWin32Error, so the TYPE COMES FROM
// THE ERROR CODE. Measured against this same primitive: a path past the Win32 32K
// ceiling fails ERROR_FILENAME_EXCED_RANGE and .NET answers PathTooLongException
// naming the path; every other refusal is a plain IOException. Only the type matches
// on that arm — .NET's message there is a FormatMessage render of the Win32 code and
// is host-localized, which no folded-in English SR string can reproduce.
[[noreturn]] static void dn2cpp_throw_full_path_failure(DWORD err, Dn2CppString* p)
{
    if (err == ERROR_FILENAME_EXCED_RANGE)
        dn2cpp_throw_sr1(&dn2cpp_path_too_long_exception_type, DN2CPP_SR_PATH_TOO_LONG_PATH, p);
    dn2cpp_throw_of(&dn2cpp_io_exception_type);
}

// Windows Path.GetFullPath: the POSIX '/'-only lexical form below cannot root a
// drive-letter (C:\…) / UNC (\\…) / '\'-rooted path, so it would wrongly prepend
// the cwd and produce a malformed path (which then overflows the real
// Directory/File normalization). Delegate to GetFullPathNameW — the same Win32
// primitive .NET's PathHelper uses: it roots drive/UNC/relative paths correctly,
// normalizes '/'→'\', and collapses '.'/'..'. char16_t and wchar_t are both
// 16-bit UTF-16 here, so the managed chars copy across directly.
//
// .NET's PathHelper.Normalize then runs TryExpandShortFileName when the result
// still carries an 8.3 short component (any '~'): GetFullPathNameW is purely
// lexical and leaves "TAKUMA~1.KOM" unexpanded, whereas Directory/FileInfo's
// FullName (the real BCL path, which expands via GetLongPathNameW) yields the
// long form — so a bare GetFullPathNameW would mismatch FullName. Mirror the
// BCL: if the normalized path contains '~', expand it with GetLongPathNameW,
// falling back to the lexical form when expansion fails (a non-existent path
// cannot be expanded — GetLongPathNameW needs the components on disk).
Dn2CppString* dn2cpp_path_get_full_path(Dn2CppString* p)
{
    // .NET answers these two with DIFFERENT types (measured on CoreCLR):
    // GetFullPath(null) is ArgumentNullException, GetFullPath("") is
    // ArgumentException. One combined check cannot say both.
    if (p == nullptr)
        dn2cpp_throw_argument_null();
    // .NET's PathInternal.IsEffectivelyEmpty counts an ALL-SPACE path as empty on
    // Windows and only there: measured, GetFullPath(" ") gives the same
    // ArgumentException as GetFullPath(""). Without the check the space case reaches
    // GetFullPathNameW, which refuses it, and an input fault reads as an IOException.
    int32_t nonSpace = 0;
    while (nonSpace < p->length && p->chars[nonSpace] == u' ')
        nonSpace++;
    if (nonSpace == p->length)
        dn2cpp_throw_argument();
    std::vector<wchar_t> in(static_cast<size_t>(p->length) + 1);
    for (int32_t i = 0; i < p->length; i++)
        in[static_cast<size_t>(i)] = static_cast<wchar_t>(p->chars[i]);
    in[static_cast<size_t>(p->length)] = L'\0';
    SetLastError(0);
    DWORD need = GetFullPathNameW(in.data(), 0, nullptr, nullptr);
    if (need == 0)
        dn2cpp_throw_full_path_failure(GetLastError(), p);
    // A fill that comes back wanting more room is the cwd having grown between the
    // two calls, not a refusal: success returns the length EXCLUDING the terminating
    // NUL, a too-small buffer the required size INCLUDING it. .NET's PathHelper
    // sizes and fills in one growing loop, so retry at the size it asked for.
    for (int attempt = 0; attempt < 4; attempt++)
    {
        std::vector<wchar_t> out(need);
        SetLastError(0);
        DWORD got = GetFullPathNameW(in.data(), need, out.data(), nullptr);
        if (got == 0)
            dn2cpp_throw_full_path_failure(GetLastError(), p);
        if (got >= need)
        {
            need = got;
            continue;
        }
        if (std::find(out.begin(), out.begin() + got, L'~') != out.begin() + got)
        {
            // Sized and filled with the same convention, so racy the same way and
            // retried the same way — but here exhaustion falls back to the lexical
            // form: having no long name is a legitimate answer, not a failure.
            DWORD longNeed = GetLongPathNameW(out.data(), nullptr, 0);
            for (int longAttempt = 0; longNeed != 0 && longAttempt < 4; longAttempt++)
            {
                std::vector<wchar_t> lng(longNeed);
                DWORD longGot = GetLongPathNameW(out.data(), lng.data(), longNeed);
                if (longGot == 0)
                    break;
                if (longGot < longNeed)
                    return dn2cpp_string_from_chars(
                        reinterpret_cast<const char16_t*>(lng.data()), static_cast<int32_t>(longGot));
                longNeed = longGot; // grew between the calls; retry at the size it asked for
            }
        }
        return dn2cpp_string_from_chars(
            reinterpret_cast<const char16_t*>(out.data()), static_cast<int32_t>(got));
    }
    dn2cpp_throw_full_path_failure(0u, p);
}
#else
Dn2CppString* dn2cpp_path_get_full_path(Dn2CppString* p)
{
    // .NET answers these two with DIFFERENT types (measured on CoreCLR):
    // GetFullPath(null) is ArgumentNullException, GetFullPath("") is
    // ArgumentException. One combined check cannot say both.
    if (p == nullptr)
        dn2cpp_throw_argument_null();
    if (p->length == 0)
        dn2cpp_throw_argument();
    // Work in UTF-8 ('/' and '.' are ASCII, never a UTF-8 continuation byte): the cwd
    // comes from getcwd as bytes, and GetFullPath is purely lexical (it collapses
    // '.'/'..'/'//', it does NOT resolve symlinks or require the path to exist).
    int32_t un = dn2cpp_string_to_utf8(p, nullptr, 0);
    std::string rel(static_cast<size_t>(un), '\0');
    if (un > 0) dn2cpp_string_to_utf8(p, rel.data(), un);
    std::string combined;
    if (!rel.empty() && rel[0] == '/')
    {
        combined = rel;
    }
    else
    {
        // Catchable, not an abort: this is not an input fault -- the argument
        // passed validation and is merely relative. getcwd fails when the
        // process's own working directory was deleted out from under it or does
        // not fit the buffer, an ENVIRONMENT failure no caller selects and one a
        // program can plausibly recover from by re-rooting itself. See
        // dn2cpp_throw_getcwd_failure above for the errno mapping.
        // Heap (see dn2cpp_file_read_all): 4 KiB of stack for a buffer whose
        // fill is a syscall away buys nothing, and this arm already allocates.
        std::vector<char> cwd(kDn2cppMaxPathBytes);
        if (dn2cpp_pal_getcwd(cwd.data(), cwd.size()) == nullptr)
            dn2cpp_throw_getcwd_failure();
        combined = std::string(cwd.data()) + "/" + rel;
    }
    // .NET preserves a trailing separator only when it is literally present (a
    // removed trailing '.'/'..' segment does not leave one).
    bool endsSep = combined.size() > 1 && combined.back() == '/';
    std::vector<std::string> stack;
    for (size_t i = 0; i < combined.size();)
    {
        if (combined[i] == '/') { i++; continue; }
        size_t j = i;
        while (j < combined.size() && combined[j] != '/') j++;
        std::string seg = combined.substr(i, j - i);
        if (seg == ".") { /* skip */ }
        else if (seg == "..") { if (!stack.empty()) stack.pop_back(); } // never above root
        else stack.push_back(seg);
        i = j;
    }
    std::string out = "/";
    for (size_t k = 0; k < stack.size(); k++)
    {
        if (k) out += '/';
        out += stack[k];
    }
    if (endsSep && !stack.empty()) out += '/';
    return dn2cpp_string_from_utf8(out.data(), static_cast<int32_t>(out.size()));
}
#endif // _WIN32

// ─── System.IO.File (UTF-8, no BOM on write; strip a UTF-8 BOM on read) ────────
// The real File.* bodies pull in the ArrayPool/EventSource/Tracing/Sys.* P/Invoke
// cascade; these libc-backed helpers replace that subtree. Semantics probed
// against real .NET (see gates/build-and-run-file-real.sh). Error paths throw
// the matching .NET exception types so `catch`/GetType() agree.

// A managed path string as a NUL-terminated UTF-8 std::string.
static std::string dn2cpp_path_to_utf8(Dn2CppString* p)
{
    if (p == nullptr) dn2cpp_throw_argument_null();
    int32_t n = dn2cpp_string_to_utf8(p, nullptr, 0);
    std::string s(static_cast<size_t>(n), '\0');
    if (n > 0) dn2cpp_string_to_utf8(p, s.data(), n);
    return s;
}

int32_t dn2cpp_file_exists(Dn2CppString* path)
{
    // .NET: false (never throws) for null/empty or a non-regular-file path.
    if (path == nullptr || path->length == 0) return 0;
    std::string p = dn2cpp_path_to_utf8(path);
    return (dn2cpp_pal_path_kind(p.c_str()) == DN2CPP_PAL_PATH_FILE) ? 1 : 0;
}

void dn2cpp_file_delete(Dn2CppString* path)
{
    std::string p = dn2cpp_path_to_utf8(path);
    if (dn2cpp_pal_path_kind(p.c_str()) == DN2CPP_PAL_PATH_DIR)
        dn2cpp_throw_of(&dn2cpp_unauthorized_access_exception_type); // .NET: dir -> UnauthorizedAccess
    // A missing file is a no-op in .NET; any other failure is an IOException.
    if (dn2cpp_pal_unlink(p.c_str()) != 0 && errno != ENOENT)
        dn2cpp_throw_of(&dn2cpp_io_exception_type);
}

// Slurp the whole file into `out`; throws FileNotFoundException when it can't open.
static void dn2cpp_file_read_all(const std::string& p, std::string& out)
{
    FILE* fp = std::fopen(p.c_str(), "rb");
    if (fp == nullptr) dn2cpp_throw_of(&dn2cpp_file_not_found_exception_type);
    // Heap, not stack: 8 KiB is more than a small-stack target (a console, an
    // engine worker thread) can spare for one frame, and this one nests under
    // whatever managed code called File.ReadAllText. The allocation is once per
    // call against a syscall-bound read loop, so it is not on any measurable
    // path; the ceiling that keeps it here is DN2CPP_MAX_STACK_FRAME.
    constexpr size_t kChunk = 8192;
    std::vector<char> buf(kChunk);
    size_t n;
    while ((n = std::fread(buf.data(), 1, kChunk, fp)) > 0)
        out.append(buf.data(), n);
    std::fclose(fp);
}

Dn2CppString* dn2cpp_file_read_all_text(Dn2CppString* path)
{
    std::string p = dn2cpp_path_to_utf8(path);
    std::string data;
    dn2cpp_file_read_all(p, data);
    // .NET strips a leading UTF-8 BOM (EF BB BF). UTF-16/32 BOM detection is a
    // carve-out — dn2cpp writes UTF-8 (no BOM), so reads round-trip.
    const char* s = data.data();
    int32_t len = static_cast<int32_t>(data.size());
    if (len >= 3 && static_cast<unsigned char>(s[0]) == 0xEF
        && static_cast<unsigned char>(s[1]) == 0xBB
        && static_cast<unsigned char>(s[2]) == 0xBF)
    {
        s += 3;
        len -= 3;
    }
    return dn2cpp_string_from_utf8(s, len);
}

Dn2CppArrayN* dn2cpp_file_read_all_bytes(Dn2CppString* path, const Dn2CppTypeInfo* ti)
{
    std::string p = dn2cpp_path_to_utf8(path);
    std::string data;
    dn2cpp_file_read_all(p, data);
    Dn2CppArrayN* arr = dn2cpp_newarr_n_t(static_cast<int32_t>(data.size()), 1, ti);
    if (!data.empty())
        std::memcpy(arr->data, data.data(), data.size());
    return arr;
}

void dn2cpp_file_write_all_text(Dn2CppString* path, Dn2CppString* contents)
{
    std::string p = dn2cpp_path_to_utf8(path);
    FILE* fp = std::fopen(p.c_str(), "wb");
    if (fp == nullptr) dn2cpp_throw_of(&dn2cpp_io_exception_type);
    // .NET: UTF-8, no BOM. null contents writes an empty file.
    if (contents != nullptr && contents->length > 0)
    {
        int32_t n = dn2cpp_string_to_utf8(contents, nullptr, 0);
        if (n > 0)
        {
            std::string u(static_cast<size_t>(n), '\0');
            dn2cpp_string_to_utf8(contents, u.data(), n);
            std::fwrite(u.data(), 1, static_cast<size_t>(n), fp);
        }
    }
    std::fclose(fp);
}

void dn2cpp_file_write_all_bytes(Dn2CppString* path, Dn2CppArrayN* bytes)
{
    std::string p = dn2cpp_path_to_utf8(path);
    FILE* fp = std::fopen(p.c_str(), "wb");
    if (fp == nullptr) dn2cpp_throw_of(&dn2cpp_io_exception_type);
    if (bytes != nullptr && bytes->length > 0)
        std::fwrite(bytes->data, 1, static_cast<size_t>(bytes->length), fp);
    std::fclose(fp);
}

// ─── System.Environment + System.IO.Directory ──────────────────────────────

Dn2CppString* dn2cpp_env_get_variable(Dn2CppString* name)
{
    std::string n = dn2cpp_path_to_utf8(name);
    const char* v = dn2cpp_pal_getenv(n.c_str());
    if (v == nullptr) return nullptr; // .NET: null when the variable is unset
    return dn2cpp_string_from_utf8(v, static_cast<int32_t>(std::strlen(v)));
}

Dn2CppString* dn2cpp_env_get_current_directory()
{
    std::vector<char> cwd(kDn2cppMaxPathBytes); // heap — see dn2cpp_file_read_all
    // The same getcwd failure as Path.GetFullPath's, and it must answer the same
    // way. Routing both through one helper keeps Directory.GetCurrentDirectory
    // and Path.GetFullPath("rel") -- two names for one syscall -- from reporting
    // the same failure as two different types.
    if (dn2cpp_pal_getcwd(cwd.data(), cwd.size()) == nullptr)
        dn2cpp_throw_getcwd_failure();
    return dn2cpp_string_from_utf8(cwd.data(), static_cast<int32_t>(std::strlen(cwd.data())));
}

void dn2cpp_env_set_current_directory(Dn2CppString* path)
{
    std::string p = dn2cpp_path_to_utf8(path);
    // .NET raises DirectoryNotFoundException when the target is missing; we don't
    // model that subtype, so a failed chdir surfaces as a catchable IOException.
    if (dn2cpp_pal_chdir(p.c_str()) != 0)
        dn2cpp_throw_of(&dn2cpp_io_exception_type);
}

#if defined(_WIN32)
// Environment.GetEnvironmentVariable(name, User|Machine): the Windows body reaches
// the Advapi32 registry P/Invokes through Internal.Win32.RegistryKey (a
// SafeRegistryHandle wrapper) — no intrinsic mapping. The seam is this one private
// method rather than the RegOpenKeyEx/RegQueryValueEx/RegCloseKey trio so the
// SafeHandle ref-count/finalizer machinery and the [Out]-buffer marshalling ABI
// stay out of the tree. Read the same key here, so the native binary and real .NET
// (the gate's live oracle) agree on the value. REG_SZ yields the raw string;
// REG_EXPAND_SZ is expanded via ExpandEnvironmentStringsW — the internal
// RegistryKey.GetValue's REG_EXPAND_SZ arm ends in
// Environment.ExpandEnvironmentVariables (only the DoNotExpandEnvironmentNames
// read option suppresses that, and the internal reader never passes it), and the
// common Machine/User values (Path, TEMP) are REG_EXPAND_SZ. Any other type or a
// missing key/value is null (the `as string` cast in
// GetEnvironmentVariableFromRegistry). char16_t and wchar_t are both 16-bit UTF-16.
Dn2CppString* dn2cpp_env_get_variable_from_registry(Dn2CppString* name, int32_t fromMachine)
{
    if (name == nullptr) return nullptr;
    HKEY root = fromMachine ? HKEY_LOCAL_MACHINE : HKEY_CURRENT_USER;
    const wchar_t* subkey = fromMachine
        ? L"System\\CurrentControlSet\\Control\\Session Manager\\Environment"
        : L"Environment";
    HKEY hkey;
    // OpenEnvironmentKeyIfExists: a missing key returns null, not an error.
    if (RegOpenKeyExW(root, subkey, 0, KEY_READ, &hkey) != ERROR_SUCCESS)
        return nullptr;
    std::vector<wchar_t> vn(static_cast<size_t>(name->length) + 1);
    for (int32_t i = 0; i < name->length; i++)
        vn[static_cast<size_t>(i)] = static_cast<wchar_t>(name->chars[i]);
    vn[static_cast<size_t>(name->length)] = L'\0';
    DWORD type = 0, cb = 0;
    LONG r = RegQueryValueExW(hkey, vn.data(), nullptr, &type, nullptr, &cb);
    if (r != ERROR_SUCCESS || (type != REG_SZ && type != REG_EXPAND_SZ))
    {
        RegCloseKey(hkey);
        return nullptr;
    }
    // cb is a byte count; round up to whole wchar_t and leave room for a
    // terminator RegQueryValueEx may or may not count, so the second read never
    // returns ERROR_MORE_DATA.
    std::vector<wchar_t> data(cb / sizeof(wchar_t) + 2, L'\0');
    DWORD cb2 = static_cast<DWORD>(data.size() * sizeof(wchar_t));
    r = RegQueryValueExW(hkey, vn.data(), nullptr, &type,
                         reinterpret_cast<LPBYTE>(data.data()), &cb2);
    RegCloseKey(hkey);
    if (r != ERROR_SUCCESS)
        return nullptr;
    int32_t chars = static_cast<int32_t>(cb2 / sizeof(wchar_t));
    if (chars > 0 && data[static_cast<size_t>(chars) - 1] == L'\0')
        chars--; // trim the single NUL RegistryKey.GetValue drops (blob[len-1]==0)
    if (type == REG_EXPAND_SZ)
    {
        // Registry data need not carry a terminator, and ExpandEnvironmentStringsW
        // (Kernel32) wants a proper NUL-terminated source — re-terminate at the
        // trimmed length (the resize covers the corner where the value grew to
        // exactly fill the slack between the two queries).
        data.resize(static_cast<size_t>(chars) + 1);
        data[static_cast<size_t>(chars)] = L'\0';
        // Size-query, then fill — the same two-call pattern as RegQueryValueExW
        // above, and racy the same way (the environment can change between the
        // calls), so retry at the larger size instead of trusting the probe. The
        // return counts wchars INCLUDING the terminating NUL; 0 means failure. An
        // undefined %Var% stays literal, exactly as ExpandEnvironmentVariables
        // leaves it.
        DWORD need = ExpandEnvironmentStringsW(data.data(), nullptr, 0);
        for (int attempt = 0; need != 0 && attempt < 4; attempt++)
        {
            std::vector<wchar_t> expanded(static_cast<size_t>(need), L'\0');
            DWORD got = ExpandEnvironmentStringsW(data.data(), expanded.data(), need);
            if (got == 0)
                break;
            if (got <= need)
                return dn2cpp_string_from_chars(
                    reinterpret_cast<const char16_t*>(expanded.data()),
                    static_cast<int32_t>(got - 1));
            need = got; // grew between the calls; retry at the size it asked for
        }
        // Expansion failed: fall through to the raw string — closer to .NET's
        // answer than the null a missing variable means.
    }
    return dn2cpp_string_from_chars(
        reinterpret_cast<const char16_t*>(data.data()), chars);
}
#else
// POSIX has no registry; the Unix CoreLib's GetEnvironmentVariableFromRegistry
// returns null. The intercept lowers it identically so this TU still compiles on
// POSIX without an Advapi32 dependency (the body is never reached there).
Dn2CppString* dn2cpp_env_get_variable_from_registry(Dn2CppString* /*name*/, int32_t /*fromMachine*/)
{
    return nullptr;
}
#endif

int32_t dn2cpp_directory_exists(Dn2CppString* path)
{
    // .NET: false (never throws) for null/empty or a non-directory path.
    if (path == nullptr || path->length == 0) return 0;
    std::string p = dn2cpp_path_to_utf8(path);
    return (dn2cpp_pal_path_kind(p.c_str()) == DN2CPP_PAL_PATH_DIR) ? 1 : 0;
}

// ─── The process image (Environment.ProcessPath / AppContext.BaseDirectory) ──

// Resolved once. The cache is a POD C string, never a Dn2CppString*: a managed
// pointer parked in static storage would outlive any collection that moved or
// swept it. Each property call allocates a fresh string, as the real BCL
// properties do (they cache the string, but callers compare by value).
namespace
{
struct Dn2CppExecutablePath
{
    char path[4096];
    bool ok;
};

const Dn2CppExecutablePath& dn2cpp_executable_path()
{
    static const Dn2CppExecutablePath cached = []
    {
        Dn2CppExecutablePath p{};
        p.ok = dn2cpp_pal_executable_path(p.path, sizeof(p.path)) == 0;
        return p;
    }();
    return cached;
}
} // namespace

Dn2CppString* dn2cpp_process_path()
{
    const Dn2CppExecutablePath& exe = dn2cpp_executable_path();
    if (!exe.ok) return nullptr; // .NET: Environment.ProcessPath is string?
    return dn2cpp_string_from_utf8(exe.path, static_cast<int32_t>(std::strlen(exe.path)));
}

Dn2CppString* dn2cpp_app_base_directory()
{
    const Dn2CppExecutablePath& exe = dn2cpp_executable_path();
    const char* sep = nullptr;
    if (exe.ok)
        for (std::size_t i = std::strlen(exe.path); i > 0; i--)
            if (dn2cpp_is_dir_sep(exe.path[i - 1])) { sep = exe.path + (i - 1); break; }
    // Keep the separator in the slice: real .NET's GetBaseDirectoryCore appends one
    // when Path.GetDirectoryName has stripped it, and an executable at the root
    // yields "/". Nothing to slice (no path, or no separator in it) -> "", the same
    // empty string GetBaseDirectoryCore returns when it has no location to work from.
    if (sep == nullptr) return dn2cpp_string_from_utf8("", 0);
    return dn2cpp_string_from_utf8(exe.path, static_cast<int32_t>(sep - exe.path + 1));
}

// mkdir one component, treating "already a directory" as success
// (the idempotent case). A pre-existing non-directory (a file) at the path is an
// IOException, matching real .NET on the leaf; any other mkdir failure is also an
// IOException (we don't model DirectoryNotFoundException — see the header note).
static void dn2cpp_directory_create_one(const std::string& p)
{
    if (dn2cpp_pal_mkdir(p.c_str()) == 0) return;
    if (errno == EEXIST)
    {
        if (dn2cpp_pal_path_kind(p.c_str()) == DN2CPP_PAL_PATH_DIR) return; // idempotent
        dn2cpp_throw_of(&dn2cpp_io_exception_type); // a file sits where a dir is asked for
    }
    dn2cpp_throw_of(&dn2cpp_io_exception_type);
}

void dn2cpp_directory_create(Dn2CppString* path)
{
    if (path == nullptr) dn2cpp_throw_argument_null(); // .NET: ArgumentNullException
    // .NET: an empty path is ArgumentException ("The path is empty.").
    if (path->length == 0) dn2cpp_throw_of(&dn2cpp_argument_exception_type);
    std::string p = dn2cpp_path_to_utf8(path);
    // Recursive (mkdir -p): create every missing parent, then the leaf. We walk
    // the path creating each prefix in turn so a fully-missing tree is built.
    // Trailing '/' is harmless (an empty final component is skipped).
    for (std::string::size_type i = 1; i < p.size(); ++i)
    {
        if (dn2cpp_is_dir_sep(p[i]))
            dn2cpp_directory_create_one(p.substr(0, i));
    }
    if (!dn2cpp_is_dir_sep(p.back()))
        dn2cpp_directory_create_one(p);
}

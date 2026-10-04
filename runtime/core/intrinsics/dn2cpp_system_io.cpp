// dn2cpp_system_io.cpp — intrinsic implementations for System.IO + System.Environment.
//
// Libc-backed replacements for the real System.IO.Path / File / Directory and
// System.Environment bodies, whose real IL pulls in the
// ArrayPool / EventSource / Tracing / Sys.* P/Invoke cascade. Semantics probed
// against real .NET — see the
// build-and-run-{console-io,filesystem-enum,env-subset}.sh gates.
//
// The public declarations live in dn2cpp_core.h; the static helpers are used only
// within this translation unit.

#include "dn2cpp_core.h"
#include "platform/dn2cpp_pal.h" // getcwd/unlink/chdir/stat-kind/getenv via the PAL seam

#include <string>     // UTF-16 lexical paths and UTF-8 OS inputs
#include <vector>     // Path.GetFullPath segment stack / Path.Combine buffer
#include <cstring>    // std::memcpy / std::strlen
#include <cstdio>     // File read/write (fopen/fread/fwrite/fclose)
#include <cerrno>     // errno / ENOENT / EACCES / EISDIR (preserved across the PAL fs calls)
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
    if (a == nullptr) dn2cpp_throw_argument_null_param("path1");
    if (b == nullptr) dn2cpp_throw_argument_null_param("path2");
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

// Every operand is checked, under its own name, before any is combined.
Dn2CppString* dn2cpp_path_combine3(Dn2CppString* a, Dn2CppString* b, Dn2CppString* c)
{
    if (a == nullptr) dn2cpp_throw_argument_null_param("path1");
    if (b == nullptr) dn2cpp_throw_argument_null_param("path2");
    if (c == nullptr) dn2cpp_throw_argument_null_param("path3");
    return dn2cpp_path_combine2(dn2cpp_path_combine2(a, b), c);
}

Dn2CppString* dn2cpp_path_combine4(Dn2CppString* a, Dn2CppString* b, Dn2CppString* c, Dn2CppString* d)
{
    if (a == nullptr) dn2cpp_throw_argument_null_param("path1");
    if (b == nullptr) dn2cpp_throw_argument_null_param("path2");
    if (c == nullptr) dn2cpp_throw_argument_null_param("path3");
    if (d == nullptr) dn2cpp_throw_argument_null_param("path4");
    return dn2cpp_path_combine2(dn2cpp_path_combine2(dn2cpp_path_combine2(a, b), c), d);
}

static bool dn2cpp_path_has_nul(const Dn2CppString* p)
{
    for (int32_t i = 0; i < p->length; i++)
        if (p->chars[i] == u'\0')
            return true;
    return false;
}

// Path.GetFullPath's argument checks, which every File and Directory member runs on
// the path it resolves: null (ArgumentNullException), empty (ArgumentException), then
// an embedded NUL, which an OS call would silently truncate the name at. .NET's
// PathInternal.IsEffectivelyEmpty counts an all-space path as empty on Windows and
// only there, with its own sentence: measured, GetFullPath(" ") gives the same
// ArgumentException as GetFullPath(""). Without that check the space case reaches
// GetFullPathNameW, which refuses it, and an input fault reads as an IOException.
// A member whose own ArgumentException.ThrowIfNullOrEmpty comes first refuses ""
// with that sentence before calling this.
void dn2cpp_path_check_resolvable(Dn2CppString* p)
{
    if (p == nullptr)
        dn2cpp_throw_argument_null_param("path");
#if defined(_WIN32)
    int32_t nonSpace = 0;
    while (nonSpace < p->length && p->chars[nonSpace] == u' ')
        nonSpace++;
    if (nonSpace == p->length)
        dn2cpp_throw_argument_param(DN2CPP_SR_PATH_EMPTY, "path");
#else
    if (p->length == 0)
        dn2cpp_throw_argument_param(DN2CPP_SR_EMPTY_STRING, "path");
#endif
    if (dn2cpp_path_has_nul(p))
        dn2cpp_throw_argument_param(DN2CPP_SR_NULL_CHAR_IN_PATH, "path");
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
// expanding the existing prefix when the final components do not exist.
Dn2CppString* dn2cpp_path_get_full_path(Dn2CppString* p)
{
    dn2cpp_path_check_resolvable(p);
    // Canonical extended paths already name the object and retain their code units.
    if (p->length >= 4 && p->chars[0] == u'\\'
        && (p->chars[1] == u'\\' || p->chars[1] == u'?')
        && p->chars[2] == u'?' && p->chars[3] == u'\\')
        return p;
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
            std::wstring input(out.data(), got);
            bool device = input.size() >= 4 && input[0] == L'\\' && input[1] == L'\\'
                && (input[2] == L'?' || input[2] == L'.') && input[3] == L'\\';
            bool dotDevice = device && input[2] == L'.';
            size_t prefixLength = 0;
            bool unc = !device && input.size() >= 2 && input[0] == L'\\' && input[1] == L'\\';
            if (dotDevice)
                input[2] = L'?';
            else if (unc)
            {
                input = L"\\\\?\\UNC\\" + input.substr(2);
                prefixLength = 6;
            }
            else if (!device)
            {
                input = L"\\\\?\\" + input;
                prefixLength = 4;
            }
            bool deviceUnc = input.size() >= 8
                && (input[4] == L'U' || input[4] == L'u')
                && (input[5] == L'N' || input[5] == L'n')
                && (input[6] == L'C' || input[6] == L'c') && input[7] == L'\\';
            size_t rootLength = 4;
            if (deviceUnc)
            {
                rootLength = 8;
                for (int component = 0; component < 2 && rootLength < input.size(); component++)
                {
                    while (rootLength < input.size() && input[rootLength] != L'\\')
                        rootLength++;
                    if (rootLength < input.size())
                        rootLength++;
                }
            }
            else if (input.size() >= 7 && input[5] == L':' && input[6] == L'\\')
                rootLength = 7;
            size_t prefixEnd = input.size();
            while (prefixEnd > rootLength)
            {
                std::wstring existing(input, 0, prefixEnd);
                DWORD longNeed = GetLongPathNameW(existing.c_str(), nullptr, 0);
                DWORD error = longNeed == 0 ? GetLastError() : ERROR_SUCCESS;
                for (int attempt = 0; longNeed != 0 && attempt < 4; attempt++)
                {
                    std::vector<wchar_t> expanded(longNeed);
                    DWORD length = GetLongPathNameW(existing.c_str(), expanded.data(), longNeed);
                    if (length == 0)
                    {
                        error = GetLastError();
                        break;
                    }
                    if (length >= longNeed)
                    {
                        longNeed = length;
                        continue;
                    }
                    std::wstring result(expanded.data(), length);
                    result.append(input, prefixEnd, input.size() - prefixEnd);
                    if (dotDevice)
                        result[2] = L'.';
                    if (unc)
                        result[6] = L'\\';
                    return dn2cpp_string_from_chars(
                        reinterpret_cast<const char16_t*>(result.data() + prefixLength),
                        static_cast<int32_t>(result.size() - prefixLength));
                }
                if (error != ERROR_FILE_NOT_FOUND && error != ERROR_PATH_NOT_FOUND)
                    break;
                // A missing suffix must not prevent an existing short parent from expanding.
                size_t separator = input.rfind(L'\\', prefixEnd - 1);
                if (separator == std::wstring::npos || separator <= rootLength)
                    break;
                prefixEnd = separator;
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
    dn2cpp_path_check_resolvable(p);
    // Lexical normalization preserves managed code units; only the OS cwd is decoded.
    std::u16string combined;
    if (p->chars[0] == u'/')
    {
        combined.assign(p->chars, p->length);
    }
    else
    {
        Dn2CppString* cwd = dn2cpp_env_get_current_directory();
        combined.assign(cwd->chars, cwd->length);
        combined += u'/';
        combined.append(p->chars, p->length);
    }
    // .NET preserves a trailing separator only when it is literally present (a
    // removed trailing '.'/'..' segment does not leave one).
    bool endsSep = combined.size() > 1 && combined.back() == u'/';
    std::vector<std::u16string> stack;
    for (size_t i = 0; i < combined.size();)
    {
        if (combined[i] == u'/') { i++; continue; }
        size_t j = i;
        while (j < combined.size() && combined[j] != u'/') j++;
        std::u16string seg = combined.substr(i, j - i);
        if (seg == u".") { /* skip */ }
        else if (seg == u"..") { if (!stack.empty()) stack.pop_back(); } // never above root
        else stack.push_back(seg);
        i = j;
    }
    std::u16string out = u"/";
    for (size_t k = 0; k < stack.size(); k++)
    {
        if (k) out += u'/';
        out += stack[k];
    }
    if (endsSep && !stack.empty()) out += u'/';
    return dn2cpp_string_from_chars(out.data(), dn2cpp_string_checked_length(static_cast<int64_t>(out.size())));
}
#endif // _WIN32

// ─── System.IO.File (UTF-8, no BOM on write; strip a UTF-8 BOM on read) ────────
// The real File.* bodies pull in the ArrayPool/EventSource/Tracing/Sys.* P/Invoke
// cascade; these libc-backed helpers replace that subtree. Semantics probed
// against real .NET (see gates/build-and-run-file-real.sh). Error paths throw
// the matching .NET exception types so `catch`/GetType() agree.

// A managed path string as a NUL-terminated UTF-8 std::string.
static std::string dn2cpp_path_to_utf8(Dn2CppString* p, const char* paramName = nullptr)
{
    if (p == nullptr)
    {
        if (paramName != nullptr) dn2cpp_throw_argument_null_param(paramName);
        dn2cpp_throw_argument_null();
    }
    int32_t n = dn2cpp_string_to_utf8(p, nullptr, 0);
    std::string s(static_cast<size_t>(n), '\0');
    if (n > 0) dn2cpp_string_to_utf8(p, s.data(), n);
    return s;
}

// The path of a member that opens the file: ArgumentException.ThrowIfNullOrEmpty's
// checks, then those of the Path.GetFullPath the open runs.
static std::string dn2cpp_file_open_path(Dn2CppString*& path)
{
    if (path != nullptr && path->length == 0)
        dn2cpp_throw_argument_param(DN2CPP_SR_EMPTY_STRING, "path");
    path = dn2cpp_path_get_full_path(path);
    return dn2cpp_path_to_utf8(path, "path");
}

// Exists suppresses path faults, but allocation failures still propagate.
static Dn2CppString* dn2cpp_path_try_get_full_path(Dn2CppString* path)
{
    try
    {
        return dn2cpp_path_get_full_path(path);
    }
    catch (const Dn2CppException& error)
    {
        if (!dn2cpp_typeinfo_assignable(error.obj->type, &dn2cpp_argument_exception_type)
            && !dn2cpp_typeinfo_assignable(error.obj->type, &dn2cpp_io_exception_type)
            && !dn2cpp_typeinfo_assignable(error.obj->type, &dn2cpp_unauthorized_access_exception_type))
            throw;
        dn2cpp_exc_inflight_pop(error.obj);
        return nullptr;
    }
}

static bool dn2cpp_file_parent_exists(Dn2CppString* full)
{
#if defined(_WIN32)
    int32_t separator = full->length - 1;
    while (separator >= 0 && full->chars[separator] != u'\\' && full->chars[separator] != u'/')
        separator--;
    std::vector<wchar_t> parent(static_cast<size_t>(separator + 2));
    for (int32_t i = 0; i <= separator; i++)
        parent[static_cast<size_t>(i)] = static_cast<wchar_t>(full->chars[i]);
    parent[static_cast<size_t>(separator + 1)] = L'\0';
    DWORD attributes = ::GetFileAttributesW(parent.data());
    return attributes != INVALID_FILE_ATTRIBUTES && (attributes & FILE_ATTRIBUTE_DIRECTORY) != 0;
#else
    std::string normalized = dn2cpp_path_to_utf8(full, "path");
    size_t separator = normalized.find_last_of('/');
    std::string parent = normalized.substr(0, separator + 1);
    return dn2cpp_pal_path_kind(parent.c_str()) == DN2CPP_PAL_PATH_DIR;
#endif
}

// An absent leaf differs from an absent parent; both messages name the normalized
// full path, as the BCL's file-open path does before issuing the OS call.
[[noreturn]] void dn2cpp_file_throw_open_failure(int err, Dn2CppString* path)
{
    if (err == EACCES || err == EPERM || err == EISDIR)
        dn2cpp_throw_sr1(&dn2cpp_unauthorized_access_exception_type,
            DN2CPP_SR_UNAUTHORIZED_ACCESS_PATH, dn2cpp_path_get_full_path(path));
    if (err == ENOENT || err == ENOTDIR)
    {
        Dn2CppString* full = dn2cpp_path_get_full_path(path);
        if (err == ENOENT && dn2cpp_file_parent_exists(full))
            dn2cpp_throw_sr1(&dn2cpp_file_not_found_exception_type,
                DN2CPP_SR_FILE_NOT_FOUND_PATH, full);
        dn2cpp_throw_sr1(&dn2cpp_directory_not_found_exception_type,
            DN2CPP_SR_DIRECTORY_NOT_FOUND_PATH, full);
    }
    dn2cpp_throw_of(&dn2cpp_io_exception_type);
}

int32_t dn2cpp_file_exists(Dn2CppString* path)
{
    // .NET: false (never throws) for null, empty, a path Path.GetFullPath refuses,
    // or one that is missing or a directory (on Windows, a device too).
    if (path == nullptr || path->length == 0 || dn2cpp_path_has_nul(path)) return 0;
    path = dn2cpp_path_try_get_full_path(path);
    if (path == nullptr) return 0;
    std::string p = dn2cpp_path_to_utf8(path, "path");
    int kind = dn2cpp_pal_path_kind(p.c_str());
#if defined(_WIN32)
    return kind == DN2CPP_PAL_PATH_FILE ? 1 : 0;
#else
    // Unix .NET counts anything that is not a directory: a device or a FIFO too.
    return (kind == DN2CPP_PAL_PATH_FILE || kind == DN2CPP_PAL_PATH_OTHER) ? 1 : 0;
#endif
}

void dn2cpp_file_delete(Dn2CppString* path)
{
    path = dn2cpp_path_get_full_path(path);
    std::string p = dn2cpp_path_to_utf8(path, "path");
    if (dn2cpp_pal_path_kind(p.c_str()) == DN2CPP_PAL_PATH_DIR)
        dn2cpp_throw_sr1(&dn2cpp_unauthorized_access_exception_type,
            DN2CPP_SR_UNAUTHORIZED_ACCESS_PATH, dn2cpp_path_get_full_path(path));
    if (dn2cpp_pal_unlink(p.c_str()) != 0)
    {
        int err = errno;
        if (err == ENOENT || err == ENOTDIR)
        {
            Dn2CppString* full = dn2cpp_path_get_full_path(path);
            if (err == ENOENT && dn2cpp_file_parent_exists(full)) return;
            dn2cpp_throw_sr1(&dn2cpp_directory_not_found_exception_type,
                DN2CPP_SR_DIRECTORY_NOT_FOUND_PATH, full);
        }
        if (err == EACCES || err == EPERM)
            dn2cpp_throw_sr1(&dn2cpp_unauthorized_access_exception_type,
                DN2CPP_SR_UNAUTHORIZED_ACCESS_PATH, dn2cpp_path_get_full_path(path));
        dn2cpp_throw_of(&dn2cpp_io_exception_type);
    }
}

// Slurp the whole file into `out`.
static void dn2cpp_file_read_all(Dn2CppString* path, const std::string& p, std::string& out)
{
    // A read-only open of a directory succeeds on POSIX; .NET refuses it.
    if (dn2cpp_pal_path_kind(p.c_str()) == DN2CPP_PAL_PATH_DIR)
        dn2cpp_throw_sr1(&dn2cpp_unauthorized_access_exception_type,
            DN2CPP_SR_UNAUTHORIZED_ACCESS_PATH, dn2cpp_path_get_full_path(path));
    FILE* fp = std::fopen(p.c_str(), "rb");
    if (fp == nullptr) dn2cpp_file_throw_open_failure(errno, path);
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
    std::string p = dn2cpp_file_open_path(path);
    std::string data;
    dn2cpp_file_read_all(path, p, data);
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
    std::string p = dn2cpp_file_open_path(path);
    std::string data;
    dn2cpp_file_read_all(path, p, data);
    Dn2CppArrayN* arr = dn2cpp_newarr_n_t(static_cast<int32_t>(data.size()), 1, ti);
    if (!data.empty())
        std::memcpy(arr->data, data.data(), data.size());
    return arr;
}

void dn2cpp_file_write_all_text(Dn2CppString* path, Dn2CppString* contents)
{
    std::string p = dn2cpp_file_open_path(path);
    FILE* fp = std::fopen(p.c_str(), "wb");
    if (fp == nullptr) dn2cpp_file_throw_open_failure(errno, path);
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
    // .NET checks the bytes before the path.
    if (bytes == nullptr) dn2cpp_throw_argument_null_param("bytes");
    std::string p = dn2cpp_file_open_path(path);
    FILE* fp = std::fopen(p.c_str(), "wb");
    if (fp == nullptr) dn2cpp_file_throw_open_failure(errno, path);
    if (bytes->length > 0)
        std::fwrite(bytes->data, 1, static_cast<size_t>(bytes->length), fp);
    std::fclose(fp);
}

// ─── System.Environment + System.IO.Directory ──────────────────────────────

Dn2CppString* dn2cpp_env_get_variable(Dn2CppString* name)
{
    std::string n = dn2cpp_path_to_utf8(name, "variable");
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

#if defined(_WIN32)
[[noreturn]] static void dn2cpp_throw_current_directory_failure(DWORD error, Dn2CppString* path)
{
    const Dn2CppTypeInfo* type = &dn2cpp_io_exception_type;
    const char* key = nullptr;
    if (error == ERROR_FILE_NOT_FOUND || error == ERROR_PATH_NOT_FOUND)
    {
        type = &dn2cpp_directory_not_found_exception_type;
        key = DN2CPP_SR_DIRECTORY_NOT_FOUND_PATH;
        error = ERROR_PATH_NOT_FOUND;
    }
    else if (error == ERROR_ACCESS_DENIED)
    {
        type = &dn2cpp_unauthorized_access_exception_type;
        key = DN2CPP_SR_UNAUTHORIZED_ACCESS_PATH;
    }
    else if (error == ERROR_FILENAME_EXCED_RANGE)
    {
        type = &dn2cpp_path_too_long_exception_type;
        key = DN2CPP_SR_PATH_TOO_LONG_PATH;
    }
    Dn2CppString* message;
    if (key != nullptr)
        message = dn2cpp_sr_message(key, &path, 1);
    else
    {
        wchar_t* buffer = nullptr;
        DWORD length = FormatMessageW(FORMAT_MESSAGE_ALLOCATE_BUFFER | FORMAT_MESSAGE_FROM_SYSTEM
            | FORMAT_MESSAGE_IGNORE_INSERTS | FORMAT_MESSAGE_ARGUMENT_ARRAY, nullptr, error, 0,
            reinterpret_cast<wchar_t*>(&buffer), 0, nullptr);
        if (length != 0)
        {
            while (length > 0 && buffer[length - 1] <= 32)
                length--;
            message = dn2cpp_string_from_chars(reinterpret_cast<const char16_t*>(buffer), length);
            LocalFree(buffer);
        }
        else
        {
            char unknown[40];
            int length = std::snprintf(unknown, sizeof(unknown), "Unknown error (0x%lx)",
                static_cast<unsigned long>(error));
            message = dn2cpp_string_from_utf8(unknown, length);
        }
        if (path->length != 0)
        {
            message = dn2cpp_string_concat3(message, dn2cpp_string_from_utf8(" : '", 4), path);
            message = dn2cpp_string_concat2(message, dn2cpp_string_from_utf8("'.", 2));
        }
    }
    auto* exception = reinterpret_cast<Dn2CppExceptionObject*>(dn2cpp_exception_new(type, message, nullptr));
    exception->hresult = (error & 0xffff0000u) != 0 ? static_cast<int32_t>(error)
        : static_cast<int32_t>(0x80070000u | (error & 0xffffu));
    dn2cpp_throw(exception);
}
#endif

void dn2cpp_env_set_current_directory(Dn2CppString* value)
{
    if (value != nullptr && value->length == 0)
        dn2cpp_throw_argument_param(DN2CPP_SR_EMPTY_STRING, "value");
    if (value == nullptr)
        dn2cpp_throw_argument_null_param("value");
#if defined(_WIN32)
    // Win32 supplies the error and consumes UTF-16; the exception retains the input.
    std::vector<wchar_t> path(static_cast<size_t>(value->length) + 1);
    for (int32_t i = 0; i < value->length; i++)
        path[i] = static_cast<wchar_t>(value->chars[i]);
    path[value->length] = L'\0';
    if (!SetCurrentDirectoryW(path.data()))
        dn2cpp_throw_current_directory_failure(GetLastError(), value);
#else
    // The name ends at an embedded NUL, as the string .NET marshals to chdir does.
    std::string p = dn2cpp_path_to_utf8(value, "value");
    if (dn2cpp_pal_chdir(p.c_str()) != 0)
    {
        int err = errno;
        if (err == ENOENT || err == ENOTDIR)
            dn2cpp_throw_sr1(&dn2cpp_directory_not_found_exception_type,
                DN2CPP_SR_DIRECTORY_NOT_FOUND_PATH, value);
        if (err == EACCES || err == EPERM)
            dn2cpp_throw_sr1(&dn2cpp_unauthorized_access_exception_type,
                DN2CPP_SR_UNAUTHORIZED_ACCESS_PATH, value);
        dn2cpp_throw_of(&dn2cpp_io_exception_type);
    }
#endif
}

// Directory.SetCurrentDirectory refuses "" through its own ThrowIfNullOrEmpty, so on
// Windows that sentence wins over GetFullPath's "The path is empty.".
void dn2cpp_directory_set_current_directory(Dn2CppString* path)
{
    if (path != nullptr && path->length == 0)
        dn2cpp_throw_argument_param(DN2CPP_SR_EMPTY_STRING, "path");
    dn2cpp_env_set_current_directory(dn2cpp_path_get_full_path(path));
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
    // .NET: false (never throws) for null, empty, a path Path.GetFullPath refuses,
    // or a non-directory path.
    if (path == nullptr || path->length == 0 || dn2cpp_path_has_nul(path)) return 0;
    path = dn2cpp_path_try_get_full_path(path);
    if (path == nullptr) return 0;
    std::string p = dn2cpp_path_to_utf8(path, "path");
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

int32_t dn2cpp_tool_process_run(Dn2CppString* executable, Dn2CppArrayRef* arguments)
{
    if (executable == nullptr) dn2cpp_throw_argument_null_param("executable");
    if (arguments == nullptr) dn2cpp_throw_argument_null_param("arguments");
    std::vector<std::string> values;
    values.reserve(static_cast<size_t>(arguments->length) + 1);
    values.push_back(dn2cpp_path_to_utf8(executable, "executable"));
    if (values[0].empty()) dn2cpp_throw_argument_msg("Executable must be a nonempty path.");
    for (int32_t i = 0; i < arguments->length; i++)
        values.push_back(dn2cpp_path_to_utf8(
            static_cast<Dn2CppString*>(arguments->data[i]), "argument"));
    std::vector<const char*> argv;
    argv.reserve(values.size() + 1);
    for (const auto& value : values)
    {
        if (value.find('\0') != std::string::npos)
            dn2cpp_throw_argument_msg("Tool executable and arguments cannot contain NUL.");
        argv.push_back(value.c_str());
    }
    argv.push_back(nullptr);
    dn2cpp_pal_console_flush();
    int32_t exitCode;
    int32_t error = dn2cpp_pal_run_process(argv[0], argv.data(), &exitCode);
    if (error == -1)
        dn2cpp_throw_platform_not_supported("Companion tool processes are unavailable on this target.");
    if (error != 0)
    {
        std::string message = "Could not run companion tool '" + values[0]
            + "' (native error " + std::to_string(error) + ").";
        dn2cpp_throw_invalid_operation_msg(message.c_str());
    }
    return exitCode;
}

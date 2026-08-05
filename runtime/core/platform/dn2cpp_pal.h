// dn2cpp_pal.h — Platform Abstraction Layer.
//
// A thin seam over the host syscalls otherwise scattered through the (mostly
// portable) intrinsic and runtime bodies: file-system metadata/mutation, the
// environment block, broken-down local time, the console byte sink and the
// allocator's usable-size query. The intrinsics keep their .NET-semantic logic
// and call only these; each target supplies one implementation file under
// platform/<os>/ (posix, windows, wasm, reference).
//
// Deliberately NOT abstracted: portable C facilities that already build
// everywhere — stdio *file* I/O, std::chrono, std::tm. A target without
// <cstdio> file streams needs a port of System.IO, not of this seam.
// Path handling stays lexical ('/'-based) in the intrinsics: '/' is accepted on
// every target including Windows; native '\' canonicalisation is a Windows-port
// concern, not part of this seam.
//
// ── The contract is machine-readable ─────────────────────────────────────────
//
// Every declaration below carries a `// PAL-CONTRACT:` line immediately above
// it, classifying it as one of:
//
//   MUST         — a correctness obligation. Answer truly or the program is
//                  wrong in a way that compiles, links and runs.
//   MAY-DEGRADE  — there is a documented "unavailable" answer and a caller that
//                  already handles it. The marker names the sentinel.
//
// That marker is the SOURCE of the MUST/MAY-DEGRADE split — `docs/PORTING.md`
// §2.1 restates it and `gates/build-and-run-doc-claims.sh` diffs the two, so a
// seam entry added without a marker, or a doc that has drifted from the header,
// is a red gate rather than a porter's discovery. The same gate derives the set
// of functions a new target must define and diffs it against every
// implementation under platform/*/.
//
// A porter's starting point is `runtime/core/platform/reference/`: a complete,
// target-free implementation of this header in portable C++17 that compiles,
// links and runs (the `PAL_REFERENCE=1` gate axis proves all three). Copy it,
// not this file plus a blank .cpp.

#pragma once

#include <cstddef> // size_t
#include <cstdint> // int64_t
#include <ctime>   // std::tm (portable broken-down time)

// ── File system ──────────────────────────────────────────────────────────────

// getcwd into buf (size bytes); returns buf on success, nullptr on failure.
// PAL-CONTRACT: MUST
char* dn2cpp_pal_getcwd(char* buf, size_t size);

// Delete a file; 0 on success, -1 on failure with errno set (ENOENT preserved so
// the caller can treat a missing file as a no-op).
// PAL-CONTRACT: MUST
int dn2cpp_pal_unlink(const char* path);

// Create one directory component (mode 0777 on POSIX); 0 on success, -1 on
// failure with errno set (EEXIST preserved so the caller can test idempotency).
// PAL-CONTRACT: MUST
int dn2cpp_pal_mkdir(const char* path);

// Change the process working directory; 0 on success, -1 on failure.
// PAL-CONTRACT: MUST
int dn2cpp_pal_chdir(const char* path);

// Existence + kind of a path resolved in a single stat call.
enum Dn2CppPalPathKind
{
    DN2CPP_PAL_PATH_MISSING = 0, // does not exist (or stat failed)
    DN2CPP_PAL_PATH_FILE    = 1, // regular file
    DN2CPP_PAL_PATH_DIR     = 2, // directory
    DN2CPP_PAL_PATH_OTHER   = 3, // exists but neither (symlink target, device, …)
};
// PAL-CONTRACT: MUST
int dn2cpp_pal_path_kind(const char* path);

// ── Environment ──────────────────────────────────────────────────────────────

// Value of an environment variable, or nullptr when unset. Points into the host
// environment block; copy before mutating it.
// PAL-CONTRACT: MUST
const char* dn2cpp_pal_getenv(const char* name);

// ── ANSI / system code page (P/Invoke Ansi marshalling) ──────────────────────
//
// real .NET's default Ansi marshalling is the host's default narrow encoding: the
// system ANSI code page with best-fit substitution on Windows
// (WideCharToMultiByte/MultiByteToWideChar, CP_ACP, WC_NO_BEST_FIT_CHARS NOT set),
// UTF-8 on POSIX. This seam supplies the per-OS transform so the runtime's `_ansi`
// marshalling helpers stay portable.

// UTF-16 -> narrow bytes. Returns the byte count (excluding any NUL). A null `buf`
// queries the required size; otherwise writes up to `bufSize` bytes (no NUL written).
// `bestFit` selects best-fit substitution for unmappable code units: real .NET's
// default P/Invoke Ansi marshalling enables it (é -> 'e'), but Marshal.StringToHGlobalAnsi
// / StringToCoTaskMemAnsi disable it (bestFit:false -> the default char '?'). Ignored on
// POSIX (UTF-8 encodes every code unit, so there is no best-fit fallback).
// PAL-CONTRACT: MUST
int32_t dn2cpp_pal_ansi_encode(const char16_t* src, int32_t srcLen, char* buf, int32_t bufSize, int32_t bestFit);

// narrow bytes -> UTF-16. Returns the code-unit count (excluding any NUL). A null
// `out` queries the required size; otherwise writes up to `outCap` units (no NUL).
// PAL-CONTRACT: MUST
int32_t dn2cpp_pal_ansi_decode(const char* bytes, int32_t byteLen, char16_t* out, int32_t outCap);

// Strict single-byte ANSI -> UTF-16 char decode: real .NET's scalar-char return
// marshalling (Encoding.Default.GetChars over one byte) maps an *undecodable* byte to
// the replacement char U+FFFD rather than the code page's best-fit substitution. Windows
// uses MultiByteToWideChar with MB_ERR_INVALID_CHARS (a lone DBCS lead byte fails ->
// U+FFFD); POSIX decodes one UTF-8 byte (0x00-0x7F pass through, else U+FFFD).
// PAL-CONTRACT: MUST
char16_t dn2cpp_pal_ansi_decode_char(uint8_t b);

// ── Locale (the host's default culture NAME, never its data) ─────────────────
//
// `CultureInfo.CurrentCulture`'s default: the BCP-47 name of the locale the host
// says the user is in ("ja-JP"), written NUL-terminated into buf; returns the
// length written, or 0 when the host reports none — which the caller reads as
// the invariant culture.
//
// This seam answers a NAME and nothing else. dn2cpp models no ICU: symbols,
// separators and patterns come from the runtime's own built-in culture table
// (dn2cpp_system_globalization.cpp), and a name the table does not carry keeps
// its name over invariant symbols.
//
// Each implementation reproduces the rule real .NET resolves the same question
// by, because the gate suite diffs the two:
//   POSIX  — ICU's uprv_getPOSIXIDForCategory order: LC_ALL, then LC_MESSAGES,
//            then LANG; "C"/"POSIX" is rejected rather than honoured. Apple
//            hosts fall back to CFLocale (the user's system preference) when
//            none of the three names a locale, matching what real .NET is
//            measured to do there.
//   Windows— GetUserDefaultLocaleName, which is already a BCP-47 name.
//   WASM   — none (a browser/node module has no locale block); returns 0, so a
//            wasm program keeps the invariant default it has always had.
// PAL-CONTRACT: MAY-DEGRADE returns 0 (the caller reads it as the invariant culture)
int32_t dn2cpp_pal_default_locale_name(char* buf, size_t size);

// ── Process ──────────────────────────────────────────────────────────────────

// Absolute path of the running executable, NUL-terminated into buf (size bytes);
// 0 on success, -1 when the host cannot report one (or buf is too small). Asked
// of the kernel, never derived from argv[0], which a relative invocation, a
// symlink or an exec-time rename all falsify.
// PAL-CONTRACT: MAY-DEGRADE returns -1 (Environment.ProcessPath -> null, AppContext.BaseDirectory -> "")
int dn2cpp_pal_executable_path(char* buf, size_t size);

// ── Process-wide memory barrier ──────────────────────────────────────────────

// Interlocked.MemoryBarrierProcessWide's per-OS mechanism: a barrier observed as
// a full fence by every OTHER thread of this process too (the asymmetric-fence
// pattern — hot readers drop their fences, the slow writer pays for everyone).
// Windows has a dedicated call (FlushProcessWriteBuffers); Linux the
// membarrier(2) syscall; the portable POSIX fallback bounces an anonymous
// page's protection (RW -> write -> PROT_NONE), whose downgrade IPIs a TLB
// shootdown to every core running this process, serializing each of them.
// PAL-CONTRACT: MUST
void dn2cpp_pal_membarrier_processwide(void);

// ── Time (host local zone, DST included) ─────────────────────────────────────

// Broken-down local time for a Unix-epoch second count (localtime_r equivalent).
// PAL-CONTRACT: MUST
void dn2cpp_pal_localtime(int64_t unixSeconds, std::tm* out);

// Unix-epoch seconds for a broken-down *local* time, resolving DST (mktime). The
// tm is normalised in place, as mktime does.
// PAL-CONTRACT: MUST
int64_t dn2cpp_pal_mktime_local(std::tm* localTm);

// ── Allocator ────────────────────────────────────────────────────────────────

// Usable size of a heap block (malloc_size on macOS, malloc_usable_size on
// glibc/musl/BSD).
// PAL-CONTRACT: MUST
size_t dn2cpp_pal_malloc_usable_size(void* ptr);

// ── Console (the byte sink Console.Out / Console.Error reach) ────────────────
//
// A seam rather than an assumption: a target whose only console is a debugger
// callback, an on-screen log or a host-supplied ring buffer has no `stdout` at
// all, and nothing portable lets the runtime notice. The whole
// `dn2cpp_console_*` (stdout) and `dn2cpp_textwriter_*` (stderr) family funnels
// here and nothing else in the runtime writes those two streams.
//
// Diagnostics that are not console output — the fatal-error and unhandled-
// exception reports — deliberately keep their own `std::fprintf(stderr, …)`:
// they run when the runtime may already be broken, and routing a panic through
// an installable sink is how a panic becomes silent.

enum Dn2CppPalConsoleStream
{
    DN2CPP_PAL_CONSOLE_OUT = 0, // Console.Out
    DN2CPP_PAL_CONSOLE_ERR = 1, // Console.Error
};

// Write `byteCount` UTF-8 bytes to the given console stream. Not NUL-terminated
// and not necessarily NUL-free; `byteCount` is the length. Called under the
// runtime's console lock, so an implementation needs no locking of its own and
// must not call back into the console family. A short write is not reported —
// Console.Write has no failure mode in .NET either.
// PAL-CONTRACT: MUST
void dn2cpp_pal_console_write(int stream, const char* bytes, size_t byteCount);

// Commit anything the console sink has buffered. Called on the graceful exit
// path and immediately before an abort, so output written before a crash still
// reaches the user — real .NET does not lose it. Implement as a no-op only if
// the sink is genuinely unbuffered.
// PAL-CONTRACT: MUST
void dn2cpp_pal_console_flush(void);

// ── Diagnostics ──────────────────────────────────────────────────────────────

// The current native call stack as per-frame function ENTRY addresses
// (innermost first), up to `max` entries into `buf`; returns the count written.
// Each entry is the entry address of the function containing that frame's PC,
// derived from the platform's unwind / function-bounds data (present even in
// stripped release binaries, since exception propagation rides it), or null when
// the platform cannot derive one. NOT raw return addresses: the consumer matches
// each entry against the generated method table EXACTLY, and only the platform
// walk knows which function a PC is inside — the table has starts but no sizes,
// so a nearest-row-below guess would rename adjacent runtime/intrinsic frames as
// managed methods. Over-capture (the PAL's own frames) is harmless — none is a
// table body. A target with no stack-walk mechanism returns 0 and the consumer
// degrades to "no trace": WASM is that target (-fwasm-exceptions exposes no
// unwinder to the module, and a release build carries no name section).
// PAL-CONTRACT: MAY-DEGRADE returns 0 (Exception.StackTrace stays null)
int32_t dn2cpp_pal_backtrace(void** buf, int32_t max);


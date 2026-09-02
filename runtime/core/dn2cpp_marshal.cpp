// dn2cpp_marshal.cpp — native interop of the dn2cpp runtime:
// Marshal, the System.Text.Ascii / Encoding
// transcode leaves, P/Invoke marshalling, and native-library symbol lookup.

#include "dn2cpp_core.h"
#include <algorithm>
#include <chrono>  // DateTime.Now/UtcNow wall clock
#include <ctime>   // localtime_r for DateTime.Now local components
#include <string>  // Path/File/Environment helpers
#include <vector>  // Path.GetFullPath segment stack
#ifndef _WIN32
#include <dlfcn.h> // dlsym for dn2cpp_native_library_get_symbol
#endif
#if defined(DN2CPP_USE_BOEHM_GC) && defined(__APPLE__)
#include <dlfcn.h>          // dladdr — locate the image holding the runtime
#include <mach-o/getsect.h> // getsegmentdata — its __DATA segment bounds
#include <mach-o/loader.h>  // mach_header_64
#endif
#include <cstdio>   // File read/write (fopen/fread/fwrite)
#include <cerrno>   // File.Delete errno/ENOENT
#include <cstdlib>  // Environment.GetEnvironmentVariable getenv
#include <cstring>  // Type.Assembly identity name compare (dn2cpp_assembly_equals)
#ifdef _MSC_VER
#include <malloc.h> // _aligned_malloc/_aligned_realloc/_aligned_free (no std::aligned_alloc on MSVC)
#endif
#ifdef _WIN32
// GetLastError() for dn2cpp_pinvoke_capture_last_error. NOMINMAX / WIN32_LEAN_AND_MEAN
// are project-wide compile definitions (runtime/CMakeLists.txt's MSVC arm), not
// repeated here — see platform/windows/dn2cpp_pal_windows.cpp's header comment.
#include <windows.h>
#include <io.h>     // _setmode / _fileno — put stdout/stderr in binary mode at init
#include <fcntl.h>  // _O_BINARY
#endif
#include <thread>              // real OS threads (Thread, worker pool)
#include <mutex>               // scheduler run-queue + task-completion + console locks
#include <condition_variable> // task_block sleeps until a cross-thread completion
#include <atomic>             // g_inflight_async_tasks, real Interlocked
#include <deque>             // Task.Run worker-pool work queue
#include <new>               // placement new (in-place ctor of a GC-allocated Timer)
#include <unordered_map>     // Dn2CppType intern table (one Type per type-info handle)

// Allocation-size query for NativeMemory.AlignedRealloc routes through the PAL
// (malloc_size on macOS, malloc_usable_size on glibc/musl/BSD).
#include "platform/dn2cpp_pal.h"

// Non-generic Type-based blittable struct marshalling. instanceSize is the
// value type's payload size (the C++ struct's sizeof), which equals the .NET
// marshalled size for a blittable sequential type — the same equivalence the
// generic forms rely on.
// The null checks below raise the catchable ArgumentNullException real .NET raises,
// rather than aborting; that includes StructureToPtr's null-ptr case, so its combined
// structure-or-ptr test needs no split. PtrToStructure over IntPtr.Zero is NOT a fault
// on .NET — it returns null.
// The marshalled size a type the runtime KNOWS outright answers, or -1 for "ask the
// flags". Keyed on the CLR name rather than on the type-info address on purpose: the
// same question arrives on two handles for the same type — the runtime's own
// dn2cpp_int32_type and a transpiled CoreLib's emitted ti_System_Int32 — and an
// address test would answer one of them from this table and the other from
// instanceSize.
//
// The table exists because instanceSize for the small primitives is the STACK width,
// not the storage width: Byte/SByte/Int16/UInt16/Char all stamp 4, correct for boxing
// and wrong for marshalling. Two rows are not widths at all: System.Boolean is 4 (the
// Win32 BOOL default) and System.Char is 1 (the default Ansi CharSet).
//
// The intrinsic value types are deliberately ABSENT: TimeSpan/DateOnly/TimeOnly/Decimal
// already stamp instanceSize equal to .NET's marshalled size, and DateTime /
// DateTimeOffset carry DN2CPP_TF_NOT_MARSHALABLE at their definitions instead.
//
// DO NOT unify this with dn2cpp_layout_size (dn2cpp_system_reflection.cpp): that one
// answers Unsafe.SizeOf<T>, the REPRESENTATION width (bool 1, char 2); this one answers
// Marshal.SizeOf, the UNMANAGED width (bool 4, char 1). Two questions, two tables — the
// overlap on the integers is blittability, not a shared definition. instanceSize's 4 is
// the BOX payload convention that dn2cpp_box/unbox depend on, so it is not the thing to
// change.
static int32_t marshal_known_size(const char* name)
{
    if (name == nullptr)
        return -1;
    struct Row { const char* name; int32_t size; };
    static const Row rows[] = {
        { "System.SByte", 1 }, { "System.Byte", 1 },
        { "System.Int16", 2 }, { "System.UInt16", 2 },
        { "System.Int32", 4 }, { "System.UInt32", 4 },
        { "System.Int64", 8 }, { "System.UInt64", 8 },
        { "System.Single", 4 }, { "System.Double", 8 },
        // The one pointer-width row: 4 on wasm32, and the emitter's mirror of this table
        // writes the same sizeof(void*) so both spellings of Marshal.SizeOf agree there.
        { "System.IntPtr", (int32_t)sizeof(void*) }, { "System.UIntPtr", (int32_t)sizeof(void*) },
        { "System.Boolean", 4 }, { "System.Char", 1 },
        { "System.Void", 1 },
    };
    for (const Row& r : rows)
        if (std::strcmp(name, r.name) == 0)
            return r.size;
    return -1;
}

[[noreturn]] static void marshal_refuse(const Dn2CppTypeInfo* ti)
{
    char buf[512];
    std::snprintf(buf, sizeof(buf),
        "Type '%s' cannot be marshaled as an unmanaged structure; no meaningful size or "
        "offset can be computed.",
        ti != nullptr && ti->name != nullptr ? ti->name : "<unnamed type>");
    dn2cpp_throw_argument_msg(buf);
}

// The ONE marshalling verdict every Type-based Marshal entry point goes through.
// Without it, `return instanceSize` answers for DateTime, `enum : long`, Nullable<int>,
// string and delegate types — all of which real .NET refuses — and answers 4 for byte,
// where .NET answers 1.
//
// The order of the tests is the contract, because several of them match the same type:
//   1. a name the runtime knows outright (the small primitives, whose instanceSize is
//      the stack width, plus bool/char/void);
//   2. an ENUM — .NET refuses every enum, whatever its underlying type, because an
//      enum is AutoLayout in metadata. (An enum FIELD marshals fine; that is the
//      declaring struct's question, decided in the emitter.)
//   3. a closed GENERIC — .NET refuses with its own distinct message, which is why
//      this is not folded into the refusal below;
//   4. a REFERENCE type — no DN2CPP_TF_VALUETYPE. A plain class, an interface, a
//      delegate, an array, string, object: .NET refuses all of them. The ONE exception
//      is a class carrying an explicit [StructLayout(Sequential/Explicit)], which .NET
//      MEASURES — the emitter stamps DN2CPP_TF_MARSHAL_INEXACT on exactly those, and
//      that arm is taken first, because refusing one through "cannot be marshaled"
//      would be a false statement about .NET. dn2cpp cannot answer it either (the
//      emitted layout puts the fields behind the Dn2CppObject header, so instanceSize
//      was never the unmanaged size), so it says THAT instead.
//   5. the unmodeled-extent shell (DN2CPP_TF_LAYOUT_UNKNOWN), ahead of the verdict bits
//      deliberately: no modelled extent means no marshalled size, which subsumes them.
//   6. DN2CPP_TF_NOT_MARSHALABLE, then — for a size query only — the MARSHALLED SIZE the
//      emitter computed, then DN2CPP_TF_MARSHAL_INEXACT. See the flags' doc in
//      dn2cpp_core.h.
// Only past all of them is instanceSize the marshalled size, and there it is exact: a
// blittable value type with a sequential/explicit layout lays out identically in the
// emitted C++ and unmanaged memory, which is the equivalence the whole Marshal surface
// has always rested on.
//
// `allowModelled` splits the two questions this function is asked. A size query may be
// served from the marshalled-layout model, which knows the unmanaged extent of shapes
// whose byte image dn2cpp cannot produce (a bool/char/string field, a [MarshalAs]
// descriptor, a [StructLayout(Sequential)] class). A COPY may not: PtrToStructure and
// StructureToPtr move instanceSize bytes, so "we know how big .NET makes it" is not
// "we can write it" — they still require the two layouts to coincide.
static int32_t marshal_require_size_impl(const Dn2CppTypeInfo* ti, bool allowModelled)
{
    if (ti == nullptr)
        dn2cpp_throw_argument_null();
    int32_t known = marshal_known_size(ti->name);
    if (known >= 0)
        return known;
    if ((ti->flags & DN2CPP_TF_ENUM) != 0)
        marshal_refuse(ti);
    if (ti->genericDef != nullptr || ti->genericArgCount > 0)
        dn2cpp_throw_argument_msg("The specified Type must not be a generic type.");
    if ((ti->flags & DN2CPP_TF_VALUETYPE) == 0 && (ti->flags & DN2CPP_TF_MARSHAL_INEXACT) == 0
        && ti->marshalSize <= 0)
        marshal_refuse(ti);
    dn2cpp_require_layout(ti);
    if ((ti->flags & DN2CPP_TF_NOT_MARSHALABLE) != 0)
        marshal_refuse(ti);
    // The marshalled-layout model's own answer, and the ONLY arm that may serve a
    // type whose representation is not its unmanaged form. `allowModelled` is what keeps
    // PtrToStructure and StructureToPtr out of it: they copy instanceSize bytes, so a size
    // that is right for Marshal.SizeOf and wrong for the byte image would turn a refusal
    // into a silent misread. A size query gets the number; a COPY still requires the two
    // layouts to coincide.
    if (allowModelled && ti->marshalSize > 0)
        return ti->marshalSize;
    if ((ti->flags & DN2CPP_TF_MARSHAL_INEXACT) != 0)
    {
        char buf[512];
        std::snprintf(buf, sizeof(buf),
            "'%s' cannot be %s: its marshalled layout differs from its representation "
            "(a bool field is 4 bytes unmanaged and 1 here, a char field 1 unmanaged and 2 "
            "here, and a [MarshalAs] descriptor rewrites the form entirely), so the two byte "
            "images are not the same bytes. Marshal.SizeOf and Marshal.OffsetOf DO answer for "
            "this type. Use a blittable struct (fixed-width integers, floats, pointers, enums "
            "and nested blittable structs) to copy one.",
            ti->name != nullptr ? ti->name : "<unnamed type>",
            allowModelled ? "sized" : "copied to or from unmanaged memory");
        dn2cpp_throw_platform_not_supported(buf);
    }
    return ti->instanceSize;
}

// The size query. Serves the marshalled-layout model's answer where it has one.
int32_t dn2cpp_marshal_require_size(const Dn2CppTypeInfo* ti)
{
    return marshal_require_size_impl(ti, /*allowModelled=*/true);
}

// The COPY query — PtrToStructure / StructureToPtr. Identical except that a type whose
// marshalled layout the model knows but whose REPRESENTATION differs from it is still
// refused: knowing how many bytes .NET would write is not knowing how to write them.
// Sharing one verdict with the size query would make PtrToStructure copy the wrong bytes
// for a bool-field struct and say nothing.
static int32_t dn2cpp_marshal_require_copyable(const Dn2CppTypeInfo* ti)
{
    return marshal_require_size_impl(ti, /*allowModelled=*/false);
}

int32_t dn2cpp_marshal_sizeof(const Dn2CppType* t)
{
    if (t == nullptr)
        dn2cpp_throw_argument_null();
    // The argument-null test comes first, as in .NET — a null Type handle has no layout
    // to ask about. Everything else is the shared verdict above, dn2cpp_require_layout
    // included.
    return dn2cpp_marshal_require_size(t->typeInfo);
}

Dn2CppObject* dn2cpp_marshal_ptr_to_structure(const void* ptr, const Dn2CppType* t)
{
    if (t == nullptr)
        dn2cpp_throw_argument_null();
    if (ptr == nullptr)
        return nullptr; // Marshal.PtrToStructure(IntPtr.Zero, …) returns null
    // The COPY verdict, then the REPRESENTATION size for the copy: past that verdict the
    // two layouts agree for every type that reaches here, and the box has to be the
    // representation because emitted code reads the boxed payload as one.
    dn2cpp_marshal_require_copyable(t->typeInfo);
    return dn2cpp_box(t->typeInfo, ptr, static_cast<size_t>(t->typeInfo->instanceSize));
}

void dn2cpp_marshal_structure_to_ptr(Dn2CppObject* structure, void* ptr)
{
    if (structure == nullptr || ptr == nullptr)
        dn2cpp_throw_argument_null();
    dn2cpp_marshal_require_copyable(structure->type); // as in dn2cpp_marshal_ptr_to_structure
    std::memcpy(ptr, structure + 1, static_cast<size_t>(structure->type->instanceSize));
}

// Marshal.PtrToStringUni(IntPtr): decode a caller-owned NUL-terminated UTF-16
// buffer. The buffer is NOT freed (the caller keeps ownership) — the string-return
// P/Invoke marshaller dn2cpp_pinvoke_str_from_utf16 frees, so it cannot be reused
// here. null -> null, matching .NET.
Dn2CppString* dn2cpp_marshal_ptr_to_string_utf16(const char16_t* p)
{
    if (p == nullptr)
        return nullptr;
    int32_t n = 0;
    while (p[n] != u'\0')
        n++;
    return dn2cpp_string_from_chars(p, n);
}

// Marshal.StringTo{HGlobal,CoTaskMem}Ansi / StringToCoTaskMemUTF8: encode to a
// NUL-terminated UTF-8 buffer the caller owns and frees. Allocated on the raw
// native heap (dn2cpp_native_alloc), NOT the GC heap: the handle flows into
// FreeHGlobal/FreeCoTaskMem == free(), which must never see a Boehm block.
void* dn2cpp_marshal_string_to_utf8(Dn2CppString* s)
{
    if (s == nullptr)
        return nullptr;
    int32_t n = dn2cpp_string_to_utf8(s, nullptr, 0);
    char* buf = static_cast<char*>(dn2cpp_native_alloc(static_cast<size_t>(n) + 1));
    dn2cpp_string_to_utf8(s, buf, n);
    buf[n] = '\0';
    return buf;
}

// Marshal.StringTo{HGlobal,CoTaskMem}Ansi: the system-ANSI-code-page sibling of the
// UTF-8 form above — the byte encoding is the host default narrow encoding (CP_ACP +
// best-fit on Windows, UTF-8 on Unix), everything else (native-heap ownership, NUL
// termination, null -> null) identical.
void* dn2cpp_marshal_string_to_ansi(Dn2CppString* s)
{
    if (s == nullptr)
        return nullptr;
    // Marshal.StringToHGlobalAnsi / StringToCoTaskMemAnsi use bestFit:false (an
    // unmappable code unit becomes '?', not a best-fit substitute), unlike the default
    // P/Invoke marshalling below.
    int32_t n = dn2cpp_string_to_ansi(s, nullptr, 0, /*bestFit=*/0);
    char* buf = static_cast<char*>(dn2cpp_native_alloc(static_cast<size_t>(n) + 1));
    dn2cpp_string_to_ansi(s, buf, n, /*bestFit=*/0);
    buf[n] = '\0';
    return buf;
}

// Marshal.StringTo{HGlobal,CoTaskMem}Uni: encode to a NUL-terminated UTF-16
// buffer (the internal representation — a plain copy) on the raw native heap.
void* dn2cpp_marshal_string_to_utf16(Dn2CppString* s)
{
    if (s == nullptr)
        return nullptr;
    int32_t n = s->length;
    char16_t* buf = static_cast<char16_t*>(
        dn2cpp_native_alloc((static_cast<size_t>(n) + 1) * sizeof(char16_t)));
    std::memcpy(buf, s->chars, static_cast<size_t>(n) * sizeof(char16_t));
    buf[n] = u'\0';
    return buf;
}

// Marshal.ZeroFree{GlobalAlloc,CoTaskMem}Ansi / ZeroFreeCoTaskMemUTF8: zero the
// buffer up to its NUL (the sensitive-data wipe), then free. null is a no-op.
void dn2cpp_marshal_zero_free_utf8(void* p)
{
    if (p == nullptr)
        return;
    char* c = static_cast<char*>(p);
    std::memset(c, 0, std::strlen(c));
    dn2cpp_native_free(p);
}

// Marshal.ZeroFree{GlobalAlloc,CoTaskMem}Unicode: the UTF-16 form of the wipe.
void dn2cpp_marshal_zero_free_utf16(void* p)
{
    if (p == nullptr)
        return;
    char16_t* c = static_cast<char16_t*>(p);
    size_t n = 0;
    while (c[n] != u'\0')
        n++;
    std::memset(c, 0, n * sizeof(char16_t));
    dn2cpp_native_free(p);
}

// ---- System.Text.Ascii transcode leaves -------------------
//
// Scalar replacements for Ascii.WidenAsciiToUtf16 / NarrowUtf16ToAscii: convert
// leading elements until the first non-ASCII (> 0x7F) element, returning the count
// converted. This is exactly what the BCL's scalar fallback does (the SIMD fast paths
// it also carries are dead here — Vector128.IsHardwareAccelerated folds to false).
size_t dn2cpp_ascii_widen_to_utf16(const uint8_t* src, char16_t* dst, size_t count)
{
    size_t i = 0;
    for (; i < count; i++)
    {
        uint8_t b = src[i];
        if (b > 0x7F)
            break;
        dst[i] = (char16_t)b;
    }
    return i;
}

size_t dn2cpp_ascii_narrow_to_ascii(const char16_t* src, uint8_t* dst, size_t count)
{
    size_t i = 0;
    for (; i < count; i++)
    {
        char16_t c = src[i];
        if (c > 0x7F)
            break;
        dst[i] = (uint8_t)c;
    }
    return i;
}

// System.Text.Unicode.Utf8Utility.TranscodeToUtf8 — the strict UTF-16 -> UTF-8
// transcode workhorse shared by Utf8.FromUtf16 and UTF8Encoding.GetBytes (replaces
// the SIMD/DWORD body; see the header). Strict: it never replaces — a trailing high
// surrogate is NeedMoreData(2), any other ill-formed surrogate is InvalidData(3); a
// full destination is DestinationTooSmall(1); otherwise Done(0). Writes the remaining
// input/output pointers (the consumed prefix) so the caller's replacement loop or
// encoder fallback resumes from the right spot.
int32_t dn2cpp_utf8_transcode_to_utf8(const char16_t* pIn, int32_t inLen,
                                      uint8_t* pOut, int32_t outLen,
                                      const char16_t** pInRem, uint8_t** pOutRem)
{
    int32_t si = 0, di = 0, status = 0; // 0 = Done
    while (si < inLen)
    {
        char32_t cp = (char32_t)(uint16_t)pIn[si];
        int32_t consume = 1;
        if (cp >= 0xD800u && cp <= 0xDBFFu)
        {
            // High surrogate: needs a following low surrogate.
            if (si + 1 >= inLen) { status = 2; break; }             // NeedMoreData
            char32_t lo = (char32_t)(uint16_t)pIn[si + 1];
            if (lo >= 0xDC00u && lo <= 0xDFFFu)
            {
                cp = 0x10000u + ((cp - 0xD800u) << 10) + (lo - 0xDC00u);
                consume = 2;
            }
            else { status = 3; break; }                             // InvalidData
        }
        else if (cp >= 0xDC00u && cp <= 0xDFFFu)
        {
            status = 3; break;                                      // InvalidData (lone low surrogate)
        }

        int32_t nbytes = cp < 0x80u ? 1 : (cp < 0x800u ? 2 : (cp < 0x10000u ? 3 : 4));
        if (di + nbytes > outLen) { status = 1; break; }            // DestinationTooSmall
        if (nbytes == 1)
        {
            pOut[di++] = (uint8_t)cp;
        }
        else if (nbytes == 2)
        {
            pOut[di++] = (uint8_t)(0xC0u | (cp >> 6));
            pOut[di++] = (uint8_t)(0x80u | (cp & 0x3Fu));
        }
        else if (nbytes == 3)
        {
            pOut[di++] = (uint8_t)(0xE0u | (cp >> 12));
            pOut[di++] = (uint8_t)(0x80u | ((cp >> 6) & 0x3Fu));
            pOut[di++] = (uint8_t)(0x80u | (cp & 0x3Fu));
        }
        else
        {
            pOut[di++] = (uint8_t)(0xF0u | (cp >> 18));
            pOut[di++] = (uint8_t)(0x80u | ((cp >> 12) & 0x3Fu));
            pOut[di++] = (uint8_t)(0x80u | ((cp >> 6) & 0x3Fu));
            pOut[di++] = (uint8_t)(0x80u | (cp & 0x3Fu));
        }
        si += consume;
    }
    if (pInRem != nullptr) *pInRem = pIn + si;
    if (pOutRem != nullptr) *pOutRem = pOut + di;
    return status;
}

// ---- Encoding.GetString ----------------------------------
//
// ASCIIEncoding maps each byte independently. Its default decoder replacement
// fallback turns every non-ASCII byte into '?'.
Dn2CppString* dn2cpp_string_decode_ascii(const char* bytes, int32_t count)
{
    char16_t* buf;
    Dn2CppString* s = dn2cpp_string_alloc(&buf, count < 0 ? 0 : count);
    for (int32_t i = 0; i < count; i++)
    {
        unsigned char b = static_cast<unsigned char>(bytes[i]);
        buf[i] = b <= 0x7Fu ? static_cast<char16_t>(b) : u'?';
    }
    buf[count < 0 ? 0 : count] = u'\0';
    s->length = count < 0 ? 0 : count;
    return s;
}

// .NET-exact UTF-8 decode with the Unicode "maximal subpart" replacement.
// Each maximal well-formed prefix of an ill-formed sequence yields ONE U+FFFD,
// and the byte that broke the sequence (unless it is a continuation byte, which
// is itself an error) is reconsidered as a fresh start. Verified byte-for-byte
// against Encoding.UTF8.GetString, e.g. `C0 80` -> FFFD FFFD, `ED A0 80` ->
// FFFD FFFD FFFD, `F4 90 80 80` -> 4x FFFD, `E2 41` -> FFFD 0041.
Dn2CppString* dn2cpp_string_decode_utf8(const char* bytes, int32_t count)
{
    // A UTF-8 run of N bytes decodes to at most N UTF-16 code units (the worst
    // case is N replacement chars; a 4-byte run yields 2 code units from 4 bytes).
    char16_t* buf;
    Dn2CppString* s = dn2cpp_string_alloc(&buf, count < 0 ? 0 : count);
    int32_t out = 0;
    int32_t i = 0;
    while (i < count)
    {
        unsigned char c = static_cast<unsigned char>(bytes[i]);
        // Determine the expected sequence length and the valid range of the
        // SECOND byte (the third/fourth are always 0x80..0xBF). C0/C1 and
        // F5..FF can never start a sequence.
        int32_t seqLen;
        unsigned char lo2, hi2;
        if (c < 0x80u) { buf[out++] = static_cast<char16_t>(c); i++; continue; }
        else if (c < 0xC2u) { buf[out++] = 0xFFFDu; i++; continue; } // 0x80..0xC1 (cont or overlong lead)
        else if (c < 0xE0u) { seqLen = 2; lo2 = 0x80u; hi2 = 0xBFu; }
        else if (c == 0xE0u) { seqLen = 3; lo2 = 0xA0u; hi2 = 0xBFu; }
        else if (c < 0xEDu) { seqLen = 3; lo2 = 0x80u; hi2 = 0xBFu; }
        else if (c == 0xEDu) { seqLen = 3; lo2 = 0x80u; hi2 = 0x9Fu; } // exclude surrogates
        else if (c < 0xF0u) { seqLen = 3; lo2 = 0x80u; hi2 = 0xBFu; }
        else if (c == 0xF0u) { seqLen = 4; lo2 = 0x90u; hi2 = 0xBFu; }
        else if (c < 0xF4u) { seqLen = 4; lo2 = 0x80u; hi2 = 0xBFu; }
        else if (c == 0xF4u) { seqLen = 4; lo2 = 0x80u; hi2 = 0x8Fu; }
        else { buf[out++] = 0xFFFDu; i++; continue; } // 0xF5..0xFF
        // Validate the continuation bytes; the second byte uses the type-specific
        // [lo2, hi2] range, the rest use 0x80..0xBF. The maximal subpart is every
        // byte that DID continue: on a break, only those bytes collapse to one
        // U+FFFD and the offending byte is left for the next iteration.
        char32_t cp = c & (seqLen == 2 ? 0x1Fu : seqLen == 3 ? 0x0Fu : 0x07u);
        int32_t k = 1;
        bool ok = true;
        for (; k < seqLen; k++)
        {
            if (i + k >= count) { ok = false; break; } // truncated -> consume the prefix
            unsigned char cc = static_cast<unsigned char>(bytes[i + k]);
            unsigned char lo = (k == 1) ? lo2 : 0x80u;
            unsigned char hi = (k == 1) ? hi2 : 0xBFu;
            if (cc < lo || cc > hi) { ok = false; break; }
            cp = (cp << 6) | (cc & 0x3Fu);
        }
        if (!ok)
        {
            // The maximal valid prefix (the lead + k-1 accepted continuations)
            // is one U+FFFD; advance past exactly those bytes (>=1) so the byte
            // that broke the sequence is reconsidered.
            buf[out++] = 0xFFFDu;
            i += k;
            continue;
        }
        // A full, well-formed (non-overlong, non-surrogate, in-range) sequence.
        if (cp <= 0xFFFFu)
            buf[out++] = static_cast<char16_t>(cp);
        else
        {
            cp -= 0x10000u;
            buf[out++] = static_cast<char16_t>(0xD800u + (cp >> 10));
            buf[out++] = static_cast<char16_t>(0xDC00u + (cp & 0x3FFu));
        }
        i += seqLen;
    }
    buf[out] = u'\0';
    s->length = out;
    return s;
}

// .NET-exact little-endian UTF-16 decode (UnicodeEncoding). An odd trailing
// byte, a lone high surrogate, or a lone low surrogate each decode to one
// U+FFFD (matching Encoding.Unicode.GetString's replacement fallback).
Dn2CppString* dn2cpp_string_decode_utf16le(const char* bytes, int32_t count)
{
    int32_t units = (count < 0 ? 0 : count) / 2;
    // Output is at most one code unit per UTF-16 unit, plus a trailing U+FFFD
    // when the byte count is odd.
    char16_t* buf;
    Dn2CppString* s = dn2cpp_string_alloc(&buf, units + 1);
    int32_t out = 0;
    int32_t i = 0;
    for (; i < units; i++)
    {
        char16_t u = static_cast<char16_t>(
            static_cast<unsigned char>(bytes[i * 2]) |
            (static_cast<unsigned char>(bytes[i * 2 + 1]) << 8));
        if (u >= 0xD800u && u <= 0xDBFFu)
        {
            // High surrogate: pair it with a following low surrogate, otherwise
            // it is lone and replaced with U+FFFD.
            char16_t lo = (i + 1 < units)
                ? static_cast<char16_t>(static_cast<unsigned char>(bytes[(i + 1) * 2]) |
                    (static_cast<unsigned char>(bytes[(i + 1) * 2 + 1]) << 8))
                : 0;
            if (lo >= 0xDC00u && lo <= 0xDFFFu)
            {
                buf[out++] = u;
                buf[out++] = lo;
                i++;
            }
            else
            {
                buf[out++] = 0xFFFDu;
            }
        }
        else if (u >= 0xDC00u && u <= 0xDFFFu)
        {
            // Lone low surrogate.
            buf[out++] = 0xFFFDu;
        }
        else
        {
            buf[out++] = u;
        }
    }
    if (((count < 0 ? 0 : count) & 1) != 0)
        buf[out++] = 0xFFFDu; // odd trailing byte
    buf[out] = u'\0';
    s->length = out;
    return s;
}

// .NET-exact little-endian UTF-32 decode (Encoding.UTF32). Each out-of-range or
// surrogate code point — and a trailing partial (1-3 byte) unit — decodes to one
// U+FFFD (matching Encoding.UTF32.GetString's replacement fallback). The Godot
// bridge leans on this: godot_string is UTF-32, and the GodotSharp marshalling
// layer converts it to a managed string through Encoding.UTF32.
Dn2CppString* dn2cpp_string_decode_utf32le(const char* bytes, int32_t count)
{
    int32_t units = (count < 0 ? 0 : count) / 4;
    // Output is at most two UTF-16 units per code point, plus a trailing U+FFFD
    // when the byte count is not a multiple of four.
    char16_t* buf;
    Dn2CppString* s = dn2cpp_string_alloc(&buf, units * 2 + 1);
    int32_t out = 0;
    for (int32_t i = 0; i < units; i++)
    {
        uint32_t cp = static_cast<uint32_t>(static_cast<unsigned char>(bytes[i * 4]))
            | (static_cast<uint32_t>(static_cast<unsigned char>(bytes[i * 4 + 1])) << 8)
            | (static_cast<uint32_t>(static_cast<unsigned char>(bytes[i * 4 + 2])) << 16)
            | (static_cast<uint32_t>(static_cast<unsigned char>(bytes[i * 4 + 3])) << 24);
        if (cp > 0x10FFFFu || (cp >= 0xD800u && cp <= 0xDFFFu))
        {
            buf[out++] = 0xFFFDu;
        }
        else if (cp <= 0xFFFFu)
        {
            buf[out++] = static_cast<char16_t>(cp);
        }
        else
        {
            cp -= 0x10000u;
            buf[out++] = static_cast<char16_t>(0xD800u + (cp >> 10));
            buf[out++] = static_cast<char16_t>(0xDC00u + (cp & 0x3FFu));
        }
    }
    if (((count < 0 ? 0 : count) & 3) != 0)
        buf[out++] = 0xFFFDu; // trailing partial unit
    buf[out] = u'\0';
    s->length = out;
    return s;
}

Dn2CppString* dn2cpp_encoding_decode_range(Dn2CppArrayN* bytes, int32_t index,
                                          int32_t count,
                                          Dn2CppString* (*decode)(const char*, int32_t))
{
    if (bytes == nullptr)
        dn2cpp_throw_argument_null();
    if (index < 0 || count < 0 || index > bytes->length || count > bytes->length - index)
        dn2cpp_throw_argument_out_of_range();
    // byte[] is a packed Dn2CppArrayN with elemSize 1; data is the raw bytes.
    return decode(bytes->data + index, count);
}

Dn2CppString* dn2cpp_encoding_get_string(Dn2CppObject* encoding, Dn2CppArrayN* bytes,
                                         int32_t index, int32_t count)
{
    // Dispatch on the receiver's runtime type by walking its type-info base chain
    // (the static type is the base System.Text.Encoding at the virtual call site).
    // The Encoding properties return internal sealed subclasses, so match the
    // supported public encoding type anywhere in the base chain.
    // Anything else is unsupported (no silent carve-out).
    for (const Dn2CppTypeInfo* t = (encoding != nullptr) ? encoding->type : nullptr;
         t != nullptr; t = t->base)
    {
        if (t->name == nullptr)
            continue;
        if (std::strcmp(t->name, "System.Text.ASCIIEncoding") == 0)
            return dn2cpp_encoding_decode_range(bytes, index, count, dn2cpp_string_decode_ascii);
        if (std::strcmp(t->name, "System.Text.UTF8Encoding") == 0)
            return dn2cpp_encoding_decode_range(bytes, index, count, dn2cpp_string_decode_utf8);
        if (std::strcmp(t->name, "System.Text.UnicodeEncoding") == 0)
            return dn2cpp_encoding_decode_range(bytes, index, count, dn2cpp_string_decode_utf16le);
        if (std::strcmp(t->name, "System.Text.UTF32Encoding") == 0)
            return dn2cpp_encoding_decode_range(bytes, index, count, dn2cpp_string_decode_utf32le);
    }
    dn2cpp_throw_not_supported();
}

Dn2CppString* dn2cpp_encoding_get_string_ptr(Dn2CppObject* encoding, const char* bytes,
                                             int32_t count)
{
    // Real .NET validates the pointer form unconditionally: a null byte* is
    // ArgumentNullException (even with count 0), a negative count is
    // ArgumentOutOfRangeException.
    if (bytes == nullptr)
        dn2cpp_throw_argument_null();
    if (count < 0)
        dn2cpp_throw_argument_out_of_range();
    for (const Dn2CppTypeInfo* t = (encoding != nullptr) ? encoding->type : nullptr;
         t != nullptr; t = t->base)
    {
        if (t->name == nullptr)
            continue;
        if (std::strcmp(t->name, "System.Text.ASCIIEncoding") == 0)
            return dn2cpp_string_decode_ascii(bytes, count);
        if (std::strcmp(t->name, "System.Text.UTF8Encoding") == 0)
            return dn2cpp_string_decode_utf8(bytes, count);
        if (std::strcmp(t->name, "System.Text.UnicodeEncoding") == 0)
            return dn2cpp_string_decode_utf16le(bytes, count);
        if (std::strcmp(t->name, "System.Text.UTF32Encoding") == 0)
            return dn2cpp_string_decode_utf32le(bytes, count);
    }
    dn2cpp_throw_not_supported();
}

// P/Invoke string marshalling.
char* dn2cpp_pinvoke_str_to_utf8(Dn2CppString* s)
{
    if (s == nullptr)
        return nullptr;
    int32_t n = dn2cpp_string_to_utf8(s, nullptr, 0);
    // GC-allocated (scanned) so it survives the [In] native call without an explicit
    // free; the GC reclaims it once the call's argument temporary is dead.
    char* buf = static_cast<char*>(dn2cpp_alloc(static_cast<size_t>(n) + 1));
    dn2cpp_string_to_utf8(s, buf, n);
    buf[n] = '\0';
    return buf;
}

Dn2CppString* dn2cpp_pinvoke_str_from_utf8(char* p)
{
    if (p == nullptr)
        return nullptr;
    Dn2CppString* s = dn2cpp_string_from_utf8(p, static_cast<int32_t>(std::strlen(p)));
    // Match .NET's default string-return marshaller, which frees the native buffer
    // (CoTaskMemFree == free on Unix); a well-behaved callee returns heap memory.
    std::free(p);
    return s;
}

// P/Invoke string marshalling under the default/Ansi CharSet (LPStr): the system
// ANSI code page (CP_ACP + best-fit on Windows, UTF-8 on Unix). Same GC-buffer /
// free-on-return discipline as the UTF-8 pair above; only the byte encoding differs.
char* dn2cpp_pinvoke_str_to_ansi(Dn2CppString* s)
{
    if (s == nullptr)
        return nullptr;
    // Default P/Invoke Ansi marshalling uses best-fit substitution (é -> 'e').
    int32_t n = dn2cpp_string_to_ansi(s, nullptr, 0, /*bestFit=*/1);
    // GC-allocated (scanned) so it survives the [In] native call without an explicit
    // free; the GC reclaims it once the call's argument temporary is dead.
    char* buf = static_cast<char*>(dn2cpp_alloc(static_cast<size_t>(n) + 1));
    dn2cpp_string_to_ansi(s, buf, n, /*bestFit=*/1);
    buf[n] = '\0';
    return buf;
}

Dn2CppString* dn2cpp_pinvoke_str_from_ansi(char* p)
{
    if (p == nullptr)
        return nullptr;
    Dn2CppString* s = dn2cpp_string_from_ansi(p, static_cast<int32_t>(std::strlen(p)));
    std::free(p);
    return s;
}

// P/Invoke string marshalling under CharSet.Unicode = UTF-16 / LPWStr. UTF-16 is
// Dn2CppString's internal representation, so no transcoding is needed.
char16_t* dn2cpp_pinvoke_str_to_utf16(Dn2CppString* s)
{
    if (s == nullptr)
        return nullptr;
    int32_t n = s->length;
    // GC-allocated (scanned) so it survives the [In] native call without an explicit
    // free; the GC reclaims it once the call's argument temporary is dead.
    char16_t* buf = static_cast<char16_t*>(dn2cpp_alloc((static_cast<size_t>(n) + 1) * sizeof(char16_t)));
    std::memcpy(buf, s->chars, static_cast<size_t>(n) * sizeof(char16_t));
    buf[n] = u'\0';
    return buf;
}

Dn2CppString* dn2cpp_pinvoke_str_from_utf16(char16_t* p)
{
    if (p == nullptr)
        return nullptr;
    Dn2CppString* s = dn2cpp_string_from_wcs(p);
    // Match .NET's default string-return marshaller, which frees the native buffer
    // (CoTaskMemFree == free on Unix), the same as the UTF-8 path.
    std::free(p);
    return s;
}

// P/Invoke byref/[Out] string result. Decode the post-call pointer
// into a managed string, then free it iff the native replaced our [In] buffer — so a
// GC-allocated [In] buffer (ref with the current value seeded in, never replaced) is
// never passed to free(), while a native-allocated buffer (out, or ref the native
// overwrote) is freed like .NET's CoTaskMemFree. result == inbuf means "unchanged".
Dn2CppString* dn2cpp_pinvoke_byref_str_result(void* result, void* inbuf, int32_t enc)
{
    Dn2CppString* s;
    if (result == nullptr)
        s = nullptr;
    else if (enc == DN2CPP_STRENC_UNICODE)
    {
        char16_t* p = static_cast<char16_t*>(result);
        int32_t n = 0;
        while (p[n] != u'\0')
            n++;
        s = dn2cpp_string_from_chars(p, n);
    }
    else
    {
        char* p = static_cast<char*>(result);
        int32_t len = static_cast<int32_t>(std::strlen(p));
        s = enc == DN2CPP_STRENC_UTF8
            ? dn2cpp_string_from_utf8(p, len)
            : dn2cpp_string_from_ansi(p, len);
    }
    if (result != inbuf && result != nullptr)
        std::free(result);
    return s;
}

// P/Invoke scalar char marshalling under the default/Ansi CharSet: the host default
// narrow encoding (system ANSI code page + best-fit on Windows, UTF-8 on Unix), via
// the ANSI PAL seam. _to_ansi yields the FIRST byte of the char's encoding (the native
// slot is one byte); _from_ansi decodes one native byte back. On Unix this reduces to
// the first UTF-8 byte / single-byte UTF-8 decode exactly as before (byte-identical).
uint8_t dn2cpp_pinvoke_char_to_ansi(char16_t c)
{
    char buf[8];
    // Default P/Invoke Ansi marshalling uses best-fit substitution.
    int32_t n = dn2cpp_pal_ansi_encode(&c, 1, buf, static_cast<int32_t>(sizeof(buf)), /*bestFit=*/1);
    // A best-fit / default-char fallback always yields at least one byte; a zero-length
    // result (only for an empty encoding) marshals as NUL, matching an empty native slot.
    return n > 0 ? static_cast<uint8_t>(buf[0]) : 0;
}

char16_t dn2cpp_pinvoke_char_from_ansi(uint8_t b)
{
    // Strict single-byte decode: an undecodable byte (a lone DBCS lead byte, or any
    // 0x80-0xFF under UTF-8) yields U+FFFD, matching real .NET's scalar-char return
    // marshalling (Encoding.Default.GetChars) rather than a best-fit substitution.
    return dn2cpp_pal_ansi_decode_char(b);
}

// P/Invoke char[] marshalling under the default/Ansi CharSet: the host default narrow
// encoding (system ANSI code page + best-fit on Windows, UTF-8 on Unix). The whole
// array is encoded as a NUL-terminated Ansi string (embedded NULs included, like real
// .NET — so the buffer length and the array length diverge). The Ansi codec handles
// surrogate pairs and embedded NULs by length, exactly as .NET decodes the array to a
// UTF-16 string and encodes it to the system code page.
void* dn2cpp_pinvoke_chararr_to_ansi(Dn2CppArrayN* arr, int32_t copyIn)
{
    if (arr == nullptr)
        return nullptr;
    int32_t n = arr->length;
    const char16_t* chars = reinterpret_cast<const char16_t*>(arr->data);
    Dn2CppString* s = dn2cpp_string_from_chars(chars, n);
    int32_t alen = dn2cpp_string_to_ansi(s, nullptr, 0, /*bestFit=*/1);
    // Room for the encoded content AND for an [In,Out]/[Out] native to overwrite up to
    // the array length: a char encodes to at most 4 bytes in any host code page, so
    // (n + 1) * 4 + 1 generously covers both, and we clamp up to the content's own
    // encoded length. Over-allocating is unobservable — the write-back decodes only up
    // to the NUL. GC-allocated so it survives the [In] call without an explicit free.
    size_t bufsz = (static_cast<size_t>(n) + 1) * 4 + 1;
    if (bufsz < static_cast<size_t>(alen) + 1)
        bufsz = static_cast<size_t>(alen) + 1;
    char* buf = static_cast<char*>(dn2cpp_alloc(bufsz));
    if (copyIn)
    {
        dn2cpp_string_to_ansi(s, buf, alen, /*bestFit=*/1);
        buf[alen] = '\0';
    }
    else
        buf[0] = '\0'; // [Out]-only: no input content copied in
    return buf;
}

void dn2cpp_pinvoke_chararr_from_ansi(Dn2CppArrayN* arr, void* buf)
{
    if (arr == nullptr || buf == nullptr)
        return;
    int32_t n = arr->length;
    char* p = static_cast<char*>(buf);
    Dn2CppString* s = dn2cpp_string_from_ansi(p, static_cast<int32_t>(std::strlen(p)));
    int32_t count = s->length < n ? s->length : n;
    char16_t* dst = reinterpret_cast<char16_t*>(arr->data);
    for (int32_t i = 0; i < count; i++)
        dst[i] = s->chars[i];
    // Shorter than the array: write a single NUL terminator after the content and leave
    // the remaining elements unchanged (probe-confirmed real-.NET write-back behaviour).
    if (count < n)
        dst[count] = u'\0';
}

// P/Invoke blittable-struct array marshalling. Unlike a primitive
// blittable array (int[]/double[]/...), which .NET pins and passes by pointer (two-way
// regardless of [In]/[Out]), an array of a blittable value-type struct (Point[]) is
// marshalled BY COPY with direction semantics (probe-confirmed vs real .NET): copy the
// managed buffer into a fresh native buffer in (unless the parameter is [Out]-only,
// which zeroes it), pass the buffer, and copy it back into the array only for
// [Out]/[In,Out]. The buffer is length * elemSize bytes (elemSize == sizeof(t_<Struct>),
// the packed Dn2CppArrayN stride, which matches the native C array layout). GC-allocated
// so it survives the call without an explicit free; non-null even for an empty array.
void* dn2cpp_pinvoke_blitarr_to_native(Dn2CppArrayN* arr, int32_t copyIn)
{
    if (arr == nullptr)
        return nullptr;
    size_t bytes = static_cast<size_t>(arr->length) * static_cast<size_t>(arr->elemSize);
    void* buf = dn2cpp_alloc(bytes > 0 ? bytes : 1);
    if (copyIn)
        std::memcpy(buf, arr->data, bytes);
    else
        std::memset(buf, 0, bytes);
    return buf;
}

void dn2cpp_pinvoke_blitarr_from_native(Dn2CppArrayN* arr, void* buf)
{
    if (arr == nullptr || buf == nullptr)
        return;
    size_t bytes = static_cast<size_t>(arr->length) * static_cast<size_t>(arr->elemSize);
    std::memcpy(arr->data, buf, bytes);
}

// P/Invoke [MarshalAs(ByValArray, SizeConst=N)] inline fixed-length array struct field.
// The native struct embeds the elements INLINE (<elem> f[N]); the managed struct holds a
// managed array reference. A blittable element's packed managed stride equals its native
// width, so the copy is a byte-for-byte memcpy.

// managed -> native (copy-in): copy n elements from the managed buffer into the inline
// slots. A source array shorter than n throws ArgumentException — exactly what real .NET's
// FixedArrayMarshaler does (it refuses to under-fill the inline buffer). A null source
// zeroes the whole buffer; a longer source is truncated to the first n elements.
void dn2cpp_pinvoke_byvalarr_in(void* dst, int32_t n, int32_t elemSize, const void* asrc, int32_t alen)
{
    size_t total = static_cast<size_t>(n) * static_cast<size_t>(elemSize);
    if (asrc == nullptr)
    {
        std::memset(dst, 0, total);
        return;
    }
    if (alen < n)
        dn2cpp_throw_argument();
    std::memcpy(dst, asrc, total);
}

// native -> managed (copy-back): allocate a fresh managed array of n elements (real .NET
// never reuses the input on copy-back) and memcpy the inline slots into it. The i4 rep is
// used for int/uint/4-byte-enum elements, Dn2CppArrayN otherwise.
Dn2CppArrayI4* dn2cpp_pinvoke_byvalarr_out_i4(const void* src, int32_t n,
                                              const Dn2CppTypeInfo* ti)
{
    Dn2CppArrayI4* a = dn2cpp_newarr_i4_t(n, ti);
    std::memcpy(a->data, src, static_cast<size_t>(n) * sizeof(int32_t));
    return a;
}

Dn2CppArrayN* dn2cpp_pinvoke_byvalarr_out_n(const void* src, int32_t n, int32_t elemSize,
                                            const Dn2CppTypeInfo* ti)
{
    Dn2CppArrayN* a = dn2cpp_newarr_n_t(n, elemSize, ti);
    std::memcpy(a->data, src, static_cast<size_t>(n) * static_cast<size_t>(elemSize));
    return a;
}

// P/Invoke string[] marshalling. A string[] marshals as an array of
// NUL-terminated buffer pointers — UTF-8 (default/Ansi) or UTF-16 (CharSet.Unicode) —
// one per element (a null element -> a null pointer). The pointer array is GC-allocated
// and roots the per-element buffers across the native call — but only if every fill
// store is barrier'd, an unbarriered slot being invisible to an incremental cycle. It
// is never null even for an empty array, matching real .NET. copyIn != 0 ([In]/[In,Out])
// encodes each element in; copyIn == 0 ([Out]-only) zeroes the slots (no input read).
void** dn2cpp_pinvoke_strarr_to_utf8(Dn2CppArrayRef* arr, int32_t copyIn)
{
    if (arr == nullptr)
        return nullptr;
    int32_t n = arr->length;
    void** buf = static_cast<void**>(dn2cpp_alloc(static_cast<size_t>(n > 0 ? n : 1) * sizeof(void*)));
    for (int32_t i = 0; i < n; i++)
        dn2cpp_gc_store_ref(&buf[i], copyIn
            ? static_cast<void*>(dn2cpp_pinvoke_str_to_utf8(reinterpret_cast<Dn2CppString*>(arr->data[i])))
            : nullptr);
    return buf;
}

void** dn2cpp_pinvoke_strarr_to_utf16(Dn2CppArrayRef* arr, int32_t copyIn)
{
    if (arr == nullptr)
        return nullptr;
    int32_t n = arr->length;
    void** buf = static_cast<void**>(dn2cpp_alloc(static_cast<size_t>(n > 0 ? n : 1) * sizeof(void*)));
    for (int32_t i = 0; i < n; i++)
        dn2cpp_gc_store_ref(&buf[i], copyIn
            ? static_cast<void*>(dn2cpp_pinvoke_str_to_utf16(reinterpret_cast<Dn2CppString*>(arr->data[i])))
            : nullptr);
    return buf;
}

// The default/Ansi-CharSet sibling of _to_utf8: each element is encoded to the host
// default narrow encoding (CP_ACP + best-fit on Windows, UTF-8 on Unix).
void** dn2cpp_pinvoke_strarr_to_ansi(Dn2CppArrayRef* arr, int32_t copyIn)
{
    if (arr == nullptr)
        return nullptr;
    int32_t n = arr->length;
    void** buf = static_cast<void**>(dn2cpp_alloc(static_cast<size_t>(n > 0 ? n : 1) * sizeof(void*)));
    for (int32_t i = 0; i < n; i++)
        dn2cpp_gc_store_ref(&buf[i], copyIn
            ? static_cast<void*>(dn2cpp_pinvoke_str_to_ansi(reinterpret_cast<Dn2CppString*>(arr->data[i])))
            : nullptr);
    return buf;
}

// A GC-allocated snapshot of a P/Invoke pointer-array's contents, taken just before the
// native call. The write-back marshaller compares each post-call slot against this to tell
// a native-replaced slot (a native heap pointer it must free, matching .NET's
// CoTaskMemFree) from an untouched slot (our GC-allocated [In] buffer, which must NEVER be
// free()d). Being GC memory, the snapshot also keeps every original [In] buffer reachable
// across the call. null buf -> null.
void** dn2cpp_pinvoke_ptrarr_dup(void** buf, int32_t n)
{
    if (buf == nullptr)
        return nullptr;
    void** copy = static_cast<void**>(
        dn2cpp_alloc(static_cast<size_t>(n > 0 ? n : 1) * sizeof(void*)));
    if (n > 0)
        dn2cpp_gc_memmove_refs(copy, buf, static_cast<size_t>(n) * sizeof(void*));
    return copy;
}

// [In,Out]/[Out] write-back: decode each slot's (possibly native-replaced) pointer into a
// fresh managed string written back into the array. Only a slot the native REPLACED
// (buf[i] != inbuf[i]) is freed after decoding — that is a native heap pointer, matching
// .NET's CoTaskMemFree-on-Unix ownership. A slot the native LEFT (buf[i] == inbuf[i]) still
// holds our GC-allocated [In] buffer (or both are null) and is decoded but NEVER freed, so
// the Boehm heap is never corrupted by free()ing a GC pointer. A null slot decodes to a
// null managed string. A null array is a no-op.
void dn2cpp_pinvoke_strarr_from_utf8(Dn2CppArrayRef* arr, void** buf, void** inbuf)
{
    if (arr == nullptr || buf == nullptr)
        return;
    int32_t n = arr->length;
    for (int32_t i = 0; i < n; i++)
    {
        char* p = static_cast<char*>(buf[i]);
        dn2cpp_gc_store_ref(&arr->data[i], p == nullptr
            ? static_cast<Dn2CppObject*>(nullptr)
            : reinterpret_cast<Dn2CppObject*>(
                  dn2cpp_string_from_utf8(p, static_cast<int32_t>(std::strlen(p)))));
        if (p != nullptr && (inbuf == nullptr || p != inbuf[i]))
            std::free(p);
    }
}

// The default/Ansi-CharSet sibling of _from_utf8: each slot decodes from the host
// default narrow encoding, with the identical free-only-if-native-replaced discipline.
void dn2cpp_pinvoke_strarr_from_ansi(Dn2CppArrayRef* arr, void** buf, void** inbuf)
{
    if (arr == nullptr || buf == nullptr)
        return;
    int32_t n = arr->length;
    for (int32_t i = 0; i < n; i++)
    {
        char* p = static_cast<char*>(buf[i]);
        dn2cpp_gc_store_ref(&arr->data[i], p == nullptr
            ? static_cast<Dn2CppObject*>(nullptr)
            : reinterpret_cast<Dn2CppObject*>(
                  dn2cpp_string_from_ansi(p, static_cast<int32_t>(std::strlen(p)))));
        if (p != nullptr && (inbuf == nullptr || p != inbuf[i]))
            std::free(p);
    }
}

void dn2cpp_pinvoke_strarr_from_utf16(Dn2CppArrayRef* arr, void** buf, void** inbuf)
{
    if (arr == nullptr || buf == nullptr)
        return;
    int32_t n = arr->length;
    for (int32_t i = 0; i < n; i++)
    {
        char16_t* p = static_cast<char16_t*>(buf[i]);
        if (p == nullptr)
        {
            arr->data[i] = nullptr;
            continue;
        }
        int32_t len = 0;
        while (p[len] != u'\0')
            len++;
        dn2cpp_gc_store_ref(&arr->data[i],
            reinterpret_cast<Dn2CppObject*>(dn2cpp_string_from_chars(p, len)));
        if (inbuf == nullptr || p != inbuf[i])
            std::free(p);
    }
}

// P/Invoke StringBuilder marshalling, default [In,Out]. _to_buffer
// hands the native a NUL-terminated copy of the builder's current content in a
// GC-scanned buffer sized for Capacity (so it survives the call and the native may
// write up to Capacity chars); _from_buffer reads the buffer back to its NUL and
// replaces the builder. Probe-confirmed vs real .NET: input is copied in, the
// write-back replaces the content, Capacity is unchanged, Length becomes the
// write-back length. Reuses the string codecs (the free-the-pointer _pinvoke_str_*
// wrappers are NOT used here — the buffer is GC memory, not native heap).
void* dn2cpp_pinvoke_sb_to_buffer(Dn2CppStringBuilder* sb, int32_t unicode)
{
    if (sb == nullptr)
        return nullptr;
    int32_t cap = sb->capacity;
    if (unicode)
    {
        // (cap + 1) UTF-16 code units: the content, a NUL, and room for the native
        // to fill up to `cap` units before re-terminating.
        char16_t* buf = static_cast<char16_t*>(
            dn2cpp_alloc((static_cast<size_t>(cap) + 1) * sizeof(char16_t)));
        if (sb->length > 0)
            std::memcpy(buf, sb->buf, static_cast<size_t>(sb->length) * sizeof(char16_t));
        buf[sb->length] = u'\0';
        return buf;
    }
    // Ansi = the host default narrow encoding (CP_ACP + best-fit on Windows, UTF-8 on
    // Unix). A char encodes to at most 3 bytes in any supported host code page, so
    // (cap + 1) * 3 + 1 bytes both holds the content and lets the native write up to
    // `cap` chars; clamp up to the content's own encoded length for safety.
    Dn2CppString* cur = dn2cpp_sb_tostring(sb);
    int32_t alen = dn2cpp_string_to_ansi(cur, nullptr, 0, /*bestFit=*/1);
    size_t bufsz = (static_cast<size_t>(cap) + 1) * 3 + 1;
    if (bufsz < static_cast<size_t>(alen) + 1)
        bufsz = static_cast<size_t>(alen) + 1;
    char* buf = static_cast<char*>(dn2cpp_alloc(bufsz));
    dn2cpp_string_to_ansi(cur, buf, alen, /*bestFit=*/1);
    buf[alen] = '\0';
    return buf;
}

void dn2cpp_pinvoke_sb_from_buffer(Dn2CppStringBuilder* sb, void* buf, int32_t unicode)
{
    if (sb == nullptr || buf == nullptr)
        return;
    Dn2CppString* s;
    if (unicode)
    {
        char16_t* p = static_cast<char16_t*>(buf);
        int32_t n = 0;
        while (p[n] != u'\0')
            n++;
        s = dn2cpp_string_from_chars(p, n);
    }
    else
    {
        char* p = static_cast<char*>(buf);
        s = dn2cpp_string_from_ansi(p, static_cast<int32_t>(std::strlen(p)));
    }
    // Replace the content: clear then append. The append grows only if the native
    // wrote more than Capacity chars (it should not), so Capacity stays put, exactly
    // as real .NET leaves it.
    sb->length = 0;
    dn2cpp_sb_append_str(sb, s);
}

// P/Invoke last-error. Per-thread, matching .NET's per-thread
// last-error slot. A SetLastError=true P/Invoke captures the platform error here
// right after the native call (dn2cpp_pinvoke_capture_last_error, this function's
// very first statement, so nothing it does afterward can perturb the value read).
// Only a SetLastError P/Invoke writes the slot — the transpiler does not emit a
// capture for plain P/Invokes — so Marshal.GetLastWin32Error reflects the most
// recent one.
static thread_local int32_t g_dn2cpp_last_pinvoke_error = 0;

// Real .NET's Marshal.GetLastWin32Error/GetLastPInvokeError need the Win32
// GetLastError() value on Windows (CoreLib's own P/Invoke surface — Interop.Kernel32.*
// — sets it, not errno; the CRT's errno is a distinct, mostly-unrelated slot on this
// platform), and POSIX errno everywhere else (dn2cpp's own SystemNative_* PAL shim
// surface sets errno on failure, matching Unix CoreLib).
void dn2cpp_pinvoke_capture_last_error(void)
{
#ifdef _WIN32
    g_dn2cpp_last_pinvoke_error = static_cast<int32_t>(::GetLastError());
#else
    g_dn2cpp_last_pinvoke_error = static_cast<int32_t>(errno);
#endif
}

void dn2cpp_pinvoke_set_last_error(int32_t e) { g_dn2cpp_last_pinvoke_error = e; }

int32_t dn2cpp_marshal_get_last_error(void) { return g_dn2cpp_last_pinvoke_error; }

// See the header: the real GetProcAddress/dlsym behind NativeLibrary.GetSymbol's
// QCall. Export/symbol names are always ASCII, so the UTF-8 narrowing of the
// managed (UTF-16) symbolName round-trips exactly.
void* dn2cpp_native_library_get_symbol(void* handle, Dn2CppString* symbolName)
{
    if (handle == nullptr || symbolName == nullptr)
        dn2cpp_throw_argument_null();
    int32_t n = dn2cpp_string_to_utf8(symbolName, nullptr, 0);
    std::string narrow(static_cast<size_t>(n), '\0');
    dn2cpp_string_to_utf8(symbolName, narrow.data(), n);
#ifdef _WIN32
    return reinterpret_cast<void*>(::GetProcAddress(static_cast<HMODULE>(handle), narrow.c_str()));
#else
    return ::dlsym(handle, narrow.c_str());
#endif
}

namespace
{
struct Dn2CppPInvokeResolverEntry
{
    const char* assemblyName;
    Dn2CppObject* resolver;
    Dn2CppPInvokeResolverInvoke invoke;
    Dn2CppPInvokeResolverEntry* next;
};

DN2CPP_GC_STATIC_ROOT Dn2CppPInvokeResolverEntry* g_pinvoke_resolvers = nullptr;
std::mutex g_pinvoke_resolver_mutex;

static std::string dn2cpp_native_utf8(Dn2CppString* value)
{
    if (value == nullptr)
        return {};
    int32_t n = dn2cpp_string_to_utf8(value, nullptr, 0);
    std::string result(static_cast<size_t>(n), '\0');
    if (n > 0)
        dn2cpp_string_to_utf8(value, result.data(), n);
    return result;
}

static bool dn2cpp_native_is_path(const std::string& name)
{
#ifdef _WIN32
    return name.find('/') != std::string::npos || name.find('\\') != std::string::npos
        || (name.size() >= 2 && name[1] == ':');
#else
    return name.find('/') != std::string::npos;
#endif
}

static void* dn2cpp_native_load_exact(const std::string& name, uint32_t flags = 0)
{
    if (name.empty())
        return nullptr;
#ifdef _WIN32
    int chars = ::MultiByteToWideChar(CP_UTF8, MB_ERR_INVALID_CHARS, name.c_str(),
        static_cast<int>(name.size()), nullptr, 0);
    if (chars <= 0)
        return nullptr;
    std::wstring wide(static_cast<size_t>(chars), L'\0');
    ::MultiByteToWideChar(CP_UTF8, MB_ERR_INVALID_CHARS, name.c_str(),
        static_cast<int>(name.size()), wide.data(), chars);
    return static_cast<void*>(::LoadLibraryExW(wide.c_str(), nullptr,
        static_cast<DWORD>(flags)));
#else
    (void)flags;
    return ::dlopen(name.c_str(), RTLD_LAZY | RTLD_LOCAL);
#endif
}

static void* dn2cpp_native_load_self()
{
#ifdef _WIN32
    return static_cast<void*>(::GetModuleHandleW(nullptr));
#else
    return ::dlopen(nullptr, RTLD_LAZY | RTLD_LOCAL);
#endif
}

static std::vector<std::string> dn2cpp_native_candidates(const std::string& name)
{
    if (dn2cpp_native_is_path(name))
        return { name };

    std::vector<std::string> candidates;
    candidates.push_back(name);
#ifdef _WIN32
    if (name.size() < 4 || name.substr(name.size() - 4) != ".dll")
        candidates.push_back(name + ".dll");
#elif defined(__APPLE__)
    bool dylib = name.size() >= 6 && name.substr(name.size() - 6) == ".dylib";
    if (!dylib)
    {
        candidates.push_back("lib" + name + ".dylib");
        candidates.push_back(name + ".dylib");
    }
    if (name.rfind("lib", 0) != 0)
        candidates.push_back("lib" + name);
#else
    bool so = name.find(".so") != std::string::npos;
    if (!so)
    {
        candidates.push_back("lib" + name + ".so");
        candidates.push_back(name + ".so");
    }
    if (name.rfind("lib", 0) != 0)
        candidates.push_back("lib" + name);
#endif
    return candidates;
}

static bool dn2cpp_native_is_absolute_path(const std::string& name)
{
#ifdef _WIN32
    return (!name.empty() && (name[0] == '/' || name[0] == '\\'))
        || (name.size() >= 2 && name[1] == ':');
#else
    return !name.empty() && name[0] == '/';
#endif
}

static std::string dn2cpp_native_app_base_directory()
{
    return dn2cpp_native_utf8(dn2cpp_app_base_directory());
}

static void* dn2cpp_native_load_candidates(const std::string& name,
    int32_t searchPathHasValue, int32_t searchPathValue)
{
    if (name == "__Internal")
        return dn2cpp_native_load_self();

    constexpr int32_t assemblyDirectory = 0x00000002;
    constexpr int32_t windowsSearchFlags = 0x00000100 | 0x00000200
        | 0x00000400 | 0x00000800 | 0x00001000;
    constexpr int32_t knownFlags = assemblyDirectory | windowsSearchFlags;
    if (searchPathHasValue != 0 && (searchPathValue & ~knownFlags) != 0)
        dn2cpp_throw_platform_not_supported(
            "DllImportSearchPath contains unsupported flags");

    uint32_t loaderFlags = 0;
#ifdef _WIN32
    // DllImportSearchPath intentionally assigns the Win32 LOAD_LIBRARY_SEARCH_*
    // bit values, so no lossy translation is needed here.
    loaderFlags = static_cast<uint32_t>(searchPathValue & windowsSearchFlags);
#endif
    if (dn2cpp_native_is_absolute_path(name))
        return dn2cpp_native_load_exact(name, loaderFlags);

    std::vector<std::string> candidates = dn2cpp_native_candidates(name);
    bool assemblyFirst = searchPathHasValue == 0
        || (searchPathValue & assemblyDirectory) != 0;
    bool assemblyOnly = searchPathHasValue != 0
        && searchPathValue == assemblyDirectory;
    if (assemblyFirst)
    {
        std::string base = dn2cpp_native_app_base_directory();
        if (!base.empty())
            for (const std::string& candidate : candidates)
            {
                uint32_t exactFlags = loaderFlags;
#ifdef _WIN32
                if (exactFlags == 0)
                    exactFlags = LOAD_WITH_ALTERED_SEARCH_PATH;
#endif
                if (void* handle = dn2cpp_native_load_exact(base + candidate, exactFlags);
                    handle != nullptr)
                    return handle;
            }
    }
    if (assemblyOnly)
        return nullptr;
    for (const std::string& candidate : candidates)
        if (void* handle = dn2cpp_native_load_exact(candidate, loaderFlags);
            handle != nullptr)
            return handle;
    return nullptr;
}
} // namespace

[[noreturn]] void dn2cpp_throw_entry_point_not_found(Dn2CppString* entryPoint)
{
    if (entryPoint == nullptr)
        dn2cpp_throw_argument_null();
    std::string narrow = dn2cpp_native_utf8(entryPoint);
    dn2cpp_throw_entry_point_not_found(narrow.c_str());
}

void dn2cpp_pinvoke_set_resolver(const char* assemblyName, Dn2CppObject* resolver,
    Dn2CppPInvokeResolverInvoke invoke)
{
    if (assemblyName == nullptr || resolver == nullptr || invoke == nullptr)
        dn2cpp_throw_argument_null();
    std::lock_guard<std::mutex> lock(g_pinvoke_resolver_mutex);
    for (Dn2CppPInvokeResolverEntry* p = g_pinvoke_resolvers; p != nullptr; p = p->next)
        if (std::strcmp(p->assemblyName, assemblyName) == 0)
            dn2cpp_throw_invalid_operation();
    auto* entry = static_cast<Dn2CppPInvokeResolverEntry*>(dn2cpp_alloc(
        sizeof(Dn2CppPInvokeResolverEntry)));
    entry->assemblyName = assemblyName;
    dn2cpp_gc_store_ref(&entry->resolver, resolver);
    entry->invoke = invoke;
    dn2cpp_gc_store_ref(&entry->next, g_pinvoke_resolvers);
    g_pinvoke_resolvers = entry;
}

void* dn2cpp_native_library_load_name(const char* name, int32_t throwOnError,
    int32_t searchPathHasValue, int32_t searchPathValue)
{
    void* handle = dn2cpp_native_load_candidates(
        name == nullptr ? std::string() : std::string(name),
        searchPathHasValue, searchPathValue);
    if (handle == nullptr && throwOnError != 0)
        dn2cpp_throw_dll_not_found(name == nullptr ? "" : name);
    return handle;
}

void* dn2cpp_native_library_load(Dn2CppString* path, int32_t throwOnError)
{
    if (path == nullptr)
        dn2cpp_throw_argument_null();
    std::string name = dn2cpp_native_utf8(path);
    void* handle = dn2cpp_native_load_exact(name);
    if (handle == nullptr && throwOnError != 0)
        dn2cpp_throw_dll_not_found(name.c_str());
    return handle;
}

void* dn2cpp_native_library_load_by_name(Dn2CppString* name, int32_t throwOnError,
    int32_t searchPathHasValue, int32_t searchPathValue)
{
    if (name == nullptr)
        dn2cpp_throw_argument_null();
    std::string narrow = dn2cpp_native_utf8(name);
    return dn2cpp_native_library_load_name(narrow.c_str(), throwOnError,
        searchPathHasValue, searchPathValue);
}

void dn2cpp_native_library_free(void* handle)
{
    if (handle == nullptr)
        return;
#ifdef _WIN32
    ::FreeLibrary(static_cast<HMODULE>(handle));
#else
    ::dlclose(handle);
#endif
}

void* dn2cpp_pinvoke_resolve(const char* moduleName, const char* entryPoint,
    const char* assemblyName, int32_t searchPathHasValue, int32_t searchPathValue)
{
    Dn2CppObject* resolver = nullptr;
    Dn2CppPInvokeResolverInvoke invoke = nullptr;
    {
        std::lock_guard<std::mutex> lock(g_pinvoke_resolver_mutex);
        for (Dn2CppPInvokeResolverEntry* p = g_pinvoke_resolvers; p != nullptr; p = p->next)
            if (std::strcmp(p->assemblyName, assemblyName) == 0)
            {
                resolver = p->resolver;
                invoke = p->invoke;
                break;
            }
    }
    void* handle = nullptr;
    if (resolver != nullptr)
    {
        Dn2CppString* managedName = dn2cpp_string_from_utf8(moduleName,
            static_cast<int32_t>(std::strlen(moduleName)));
        handle = reinterpret_cast<void*>(invoke(resolver, managedName, assemblyName,
            searchPathHasValue, searchPathValue));
    }
    if (handle == nullptr)
        handle = dn2cpp_native_library_load_name(moduleName, 1,
            searchPathHasValue, searchPathValue);
#ifdef _WIN32
    void* symbol = reinterpret_cast<void*>(::GetProcAddress(
        static_cast<HMODULE>(handle), entryPoint));
#else
    void* symbol = ::dlsym(handle, entryPoint);
#endif
    if (symbol == nullptr)
        dn2cpp_throw_entry_point_not_found(entryPoint);
    return symbol;
}

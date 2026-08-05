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
#include <cstdarg>  // va_list / va_start (the console scalar writers' vsnprintf)
#include <cstdio>   // File read/write (fopen/fread/fwrite); vsnprintf (console scalars)
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

// Deterministic fixed-seed fill for Interop.GetRandomBytes (the
// non-cryptographic random source). See the header for why a fixed seed keeps
// Dictionary/HashSet behaviour exact. A constant golden-ratio seed (non-zero, as
// xorshift32 requires) gives a well-mixed but fully deterministic byte stream.
void dn2cpp_fill_nonsecure_random(void* buffer, int32_t length)
{
    if (buffer == nullptr || length <= 0)
        return;
    uint8_t* p = static_cast<uint8_t*>(buffer);
    uint32_t state = 0x9E3779B9u; // fixed, non-zero seed
    for (int32_t i = 0; i < length; i++)
    {
        state ^= state << 13;
        state ^= state >> 17;
        state ^= state << 5;
        p[i] = static_cast<uint8_t>(state & 0xFFu);
    }
}

Dn2CppString* dn2cpp_int_to_string(int32_t v)
{
    char buf[16];
    int len = std::snprintf(buf, sizeof(buf), "%d", v);
    return dn2cpp_string_from_ascii(buf, len);
}

Dn2CppString* dn2cpp_long_to_string(int64_t v)
{
    char buf[24];
    int len = std::snprintf(buf, sizeof(buf), "%lld", static_cast<long long>(v));
    return dn2cpp_string_from_ascii(buf, len);
}

// ---- Math.Round with an explicit MidpointRounding mode ----
// Maps the BCL MidpointRounding enum (ToEven=0, AwayFromZero=1, ToZero=2,
// ToNegativeInfinity=3, ToPositiveInfinity=4) onto the matching std rounding.
double dn2cpp_math_round_mode(double value, int32_t mode)
{
    switch (mode)
    {
        case 0: return std::nearbyint(value); // ToEven (FE_TONEAREST: ties to even)
        case 1: return std::round(value);     // AwayFromZero (ties away from zero)
        case 2: return std::trunc(value);     // ToZero
        case 3: return std::floor(value);     // ToNegativeInfinity
        case 4: return std::ceil(value);      // ToPositiveInfinity
        default: dn2cpp_throw_argument();     // catchable, like the BCL's InvalidEnumValue
    }
}

int32_t dn2cpp_str_is_null_or_whitespace(Dn2CppString* s)
{
    if (s == nullptr || s->length == 0)
        return 1;
    // char.IsWhiteSpace semantics (full Unicode), like the real BCL body.
    for (int32_t i = 0; i < s->length; i++)
        if (!dn2cpp_char_is_whitespace(s->chars[i]))
            return 0;
    return 1;
}

// ---- Object.ToString / value concatenation ----
//
// INVARIANT: this formatter has TWO kinds of caller and they disagree
// about null, so there are two entry points and picking the wrong one is silent.
//
//  * CONCATENATION — String.Concat(object…) / String.Join, an interpolation or
//    String.Format hole, StringBuilder.Append/Insert(object), Console.Write(object),
//    Convert.ToString(object). Real .NET folds a null operand to string.Empty in
//    every one of them, so these call dn2cpp_object_tostring and get the fold.
//  * VIRTUAL DISPATCH — an explicit `x.ToString()`, which Roslyn always emits
//    against the least-overridden System.Object::ToString (that is why
//    `((Exception)null).ToString()` lands here too, at the exception arm below).
//    Real .NET throws NullReferenceException, so these call
//    dn2cpp_object_tostring_virtual, which null-checks and then shares this body.
//
// Route a dispatch site at the folding entry point and nothing fails: the
// transpile is green, the C++ compiles and links, and the program prints an
// empty string where it should have thrown — a wrong answer wearing the shape of
// a formatting result. The dispatch mouths are enumerable and few (the
// System.Object::ToString intrinsic, the boxed `constrained.` ToString arms, the
// synthesized System.Enum.ToString body, and the interpreter's tostring opcode);
// the concat mouths are open-ended, which is why the plain name serves them.
//
// Every dispatch mouth is on the virtual entry point, the interpreter's included.
// The interpreter's string-concat route reaches the folding entry point below through
// the dn2cpp_string_concat* helpers, which is the correct side.

Dn2CppString* dn2cpp_object_tostring(Dn2CppObject* obj)
{
    if (obj == nullptr)
        return dn2cpp_string_from_utf8("", 0); // null formats as empty (Concat semantics)
    const Dn2CppTypeInfo* t = obj->type;
    if (t != nullptr && t->tostring != nullptr)
        return t->tostring(obj); // overridden ToString
    // An exception reached through Object.ToString: the C# compiler emits every
    // virtual `ex.ToString()` against the least-overridden System.Object::ToString,
    // so the ("System.Exception", "ToString") intrinsic only ever sees non-virtual
    // BCL `call`s — the virtual path lands here. Same text either way (Exception's
    // "FullTypeName: Message" shape plus the throw-time trace section); without this
    // arm it would fall through to the bare-type-name default, which real .NET never
    // prints for an exception.
    if (t != nullptr && dn2cpp_type_is_exception(t))
        return dn2cpp_exception_tostring(obj);
    if (t == &dn2cpp_string_type)
        return reinterpret_cast<Dn2CppString*>(obj);
    const void* payload = obj + 1; // boxed value sits right after the header
    if (t == &dn2cpp_int32_type)
        return dn2cpp_int_to_string(*reinterpret_cast<const int32_t*>(payload));
    if (t == &dn2cpp_int64_type)
        return dn2cpp_long_to_string(*reinterpret_cast<const int64_t*>(payload));
    if (t == &dn2cpp_double_type)
        return dn2cpp_double_to_string(*reinterpret_cast<const double*>(payload));
    if (t == &dn2cpp_single_type)
        return dn2cpp_float_to_string(*reinterpret_cast<const float*>(payload));
    if (t == &dn2cpp_bool_type)
        return dn2cpp_bool_to_string(*reinterpret_cast<const int32_t*>(payload));
    if (t == &dn2cpp_char_type) // a boxed char ToStrings to that single character
    {
        char16_t* buf;
        Dn2CppString* s = dn2cpp_string_alloc(&buf, 1);
        buf[0] = static_cast<char16_t>(*reinterpret_cast<const int32_t*>(payload));
        return s;
    }
    if (t == &dn2cpp_byte_type || t == &dn2cpp_sbyte_type
        || t == &dn2cpp_int16_type || t == &dn2cpp_uint16_type) // all widen to int32
        return dn2cpp_int_to_string(*reinterpret_cast<const int32_t*>(payload));
    if (t == &dn2cpp_uint32_type) // unsigned; format via int64 so >int.MaxValue stays positive
        return dn2cpp_long_to_string(static_cast<int64_t>(*reinterpret_cast<const uint32_t*>(payload)));
    if (t == &dn2cpp_uint64_type) // unsigned; format via the uint64 formatter
        return dn2cpp_format_uint(*reinterpret_cast<const uint64_t*>(payload), 8, nullptr);
    if (t == &dn2cpp_intptr_type) // signed 8-byte; like int64
        return dn2cpp_long_to_string(*reinterpret_cast<const intptr_t*>(payload));
    if (t == &dn2cpp_uintptr_type) // unsigned 8-byte; like uint64
        return dn2cpp_format_uint(*reinterpret_cast<const uintptr_t*>(payload), 8, nullptr);
    // Decimal and the date/time value types reach their formatter through the
    // tostring slot handled above, wired where their type-infos are defined;
    // naming them here would pin their translation units into every binary.
    if (t == &dn2cpp_stringbuilder_type)
        return dn2cpp_sb_tostring(reinterpret_cast<Dn2CppStringBuilder*>(obj));
    // A boxed enum (its synthesized type-info bases on dn2cpp_enum_type) formats by its
    // member name, recovered from the per-enum (name, value) table the type-info
    // carries; an undefined value falls back to the underlying number, matching
    // Enum.ToString (flag combinations stay a carve-out). A 64-bit-underlying
    // enum's box carries an 8-byte payload (CppTypes.Of), so it reads at full
    // width — an int32 read printed the truncated member's name.
    if (t != nullptr && t->base == &dn2cpp_enum_type)
    {
        bool wide = t->enumUnderlying == &dn2cpp_int64_type || t->enumUnderlying == &dn2cpp_uint64_type;
        int64_t ev = wide ? *reinterpret_cast<const int64_t*>(payload)
                          : *reinterpret_cast<const int32_t*>(payload);
        for (int32_t i = 0; i < t->enumMemberCount; i++)
        {
            int64_t mv = t->enumMembers[i].value;
            // A 32-bit-underlying enum compares at 32-bit truncation (the table
            // may carry a uint32 member zero-extended while the int32 payload
            // read sign-extends); a 64-bit one compares at full width.
            bool hit = wide ? mv == ev : static_cast<int32_t>(mv) == static_cast<int32_t>(ev);
            if (hit)
                return dn2cpp_string_from_utf8(t->enumMembers[i].name,
                    static_cast<int32_t>(std::strlen(t->enumMembers[i].name)));
        }
        return wide ? dn2cpp_long_to_string(ev) : dn2cpp_int_to_string(static_cast<int32_t>(ev));
    }
    // A System.Type object formats as the wrapped type's name (Type.ToString),
    // so an interpolation hole holding a Type matches the static intrinsic.
    if (t == &dn2cpp_type_type)
    {
        const Dn2CppTypeInfo* w = reinterpret_cast<Dn2CppType*>(obj)->typeInfo;
        if (w != nullptr && w->name != nullptr)
            return dn2cpp_type_tostring(w);
    }
    // Default Object.ToString: GetType().ToString().
    if (t == nullptr || t->name == nullptr)
        return dn2cpp_string_from_utf8("System.Object", 13);
    return dn2cpp_type_tostring(t);
}

// The virtual-dispatch half of the invariant above: an explicit `x.ToString()`.
// Identical formatting, but a null receiver is a NullReferenceException rather
// than string.Empty — .NET dereferences the receiver before it formats anything.
Dn2CppString* dn2cpp_object_tostring_virtual(Dn2CppObject* obj)
{
    if (obj == nullptr)
        dn2cpp_throw_null_reference();
    return dn2cpp_object_tostring(obj);
}

// Object.MemberwiseClone — shallow copy: same runtime type, bit-copied payload
// (reference fields shared). "The payload" is six different questions, which is why
// this is six arms rather than one memcpy:
//
//   class / boxed value  the type-info states the extent — sizeof(struct) for an
//                        emitted class OR for a hand-written runtime struct, the
//                        unboxed payload size for a box.
//   exception            the 26 hand-written exception type-infos are shells over one
//                        prefix struct and state no extent of their own, so the extent
//                        FLOORS where the allocator's does. Deriving it from the
//                        allocator rather than re-stating it 26 times is what keeps the
//                        two from drifting: a clone smaller than its original is a
//                        truncation nothing reports.
//   bare System.Object   no fields at all: the instance IS the header, and the
//                        runtime type-info's instanceSize of 0 says so.
//   SZArray              the extent is on the OBJECT (its length), not in the
//                        type-info, so this routes to the same rep-discriminating
//                        copy Array.Clone() uses. Shallow in exactly .NET's sense:
//                        value elements bitwise, reference elements shared.
//   MD array             one allocation with lengths/lowerBounds/data appended
//                        INSIDE it. A straight memcpy of the whole block would
//                        hand back a clone whose three interior pointers still
//                        address the SOURCE — two arrays sharing one element
//                        block, which reads as a clone until somebody writes to
//                        it. Rebuilt through the allocator instead.
//   string               a fixed header over an out-of-line UTF-16 buffer, so the
//                        header copy alone would be a valid object; the buffer is
//                        copied anyway (see below).
//
// The string arm is a DELIBERATE divergence: real .NET's reflective MemberwiseClone
// over a string hands back a clone whose chars past the first are uninitialized heap,
// which is not even deterministic. Copying the buffer answers better than the oracle —
// which is also why no diff gate can assert the cloned CONTENT, only the length and
// the reference identity both runtimes agree on.
//
// Real .NET clones every intrinsic-represented reference type, so the refusal set here
// is only the types carrying DN2CPP_TF_NO_SHALLOW_CLONE — a statement about dn2cpp's
// REPRESENTATION, not about .NET.
Dn2CppObject* dn2cpp_object_memberwise_clone(Dn2CppObject* obj)
{
    if (obj == nullptr)
        dn2cpp_throw_null_reference();
    const Dn2CppTypeInfo* t = obj->type;
    if (t == &dn2cpp_string_type)
    {
        auto* s = reinterpret_cast<Dn2CppString*>(obj);
        return reinterpret_cast<Dn2CppObject*>(dn2cpp_string_from_chars(s->chars, s->length));
    }
    if (t != nullptr && (t->flags & DN2CPP_TF_ARRAY) != 0)
        // Every array layout — the SZ reps and (arrayRank > 1) the Dn2CppMDArray one
        // alike, through the one copy Array.Clone's MD mouths also use.
        return dn2cpp_array_clone_dyn(obj);
    // A bare `new object()` carries the runtime's own System.Object type-info, whose
    // instanceSize is 0 — not because the extent is unknown but because the instance IS
    // the header. Stating that here rather than stamping a size on the shared type-info:
    // instanceSize is read as a value type's UNBOXED payload size by several other
    // callers, and Object is the one type every reference type's base chain ends at.
    if (t == &dn2cpp_object_type)
    {
        auto* bare = static_cast<Dn2CppObject*>(dn2cpp_alloc(sizeof(Dn2CppObject)));
        bare->type = t;
        return bare;
    }
    // A representation whose ownership is singular refuses BEFORE the extent is read:
    // its extent is perfectly well known, and copying it anyway is exactly the wrong
    // answer the bit exists to name (see DN2CPP_TF_NO_SHALLOW_CLONE).
    if (t != nullptr && (t->flags & DN2CPP_TF_NO_SHALLOW_CLONE) != 0)
        dn2cpp_throw_platform_not_supported(
            (std::string("MemberwiseClone of '")
             + ((t->name != nullptr) ? t->name : "<unnamed type>")
             + "' is not supported: dn2cpp represents this type as a hand-written runtime "
               "struct that owns native state singularly — a native-heap allocation, an "
               "embedded mutex/condition variable, a running OS thread, or a slot in a "
               "process-wide registry — so a bitwise copy would be a second OWNER of one "
               "resource rather than the shallow copy .NET hands back. Rebuild the object "
               "through its constructor instead.").c_str());

    int32_t size = t != nullptr ? t->instanceSize : 0;
    // A hand-written exception type-info states no extent of its own because its
    // representation IS the prefix, so the extent floors exactly where the allocator's
    // does (dn2cpp_exception_new, dn2cpp_get_uninitialized_object, and the canceled-OCE
    // mint all compute the same max). Deriving the clone's extent from the ALLOCATOR's
    // rather than from instanceSize alone is what makes the 26 hand-written exception
    // shells clonable without stamping a number on each of them — and what keeps the
    // clone the same size as the object it copies, which is the only correctness
    // condition the memcpy below has.
    size_t floor = (t != nullptr && dn2cpp_type_is_exception(t)) ? sizeof(Dn2CppExceptionObject) : 0;
    if (t == nullptr || (size <= 0 && floor == 0))
    {
        // A reference type whose type-info states no extent and is not an exception:
        // an abstract shell that should never be an instance's header word, or a
        // hand-written type added without the stamp. A catchable throw naming the type
        // is right because the alternative is a WRONG answer, not a missing one —
        // falling back to sizeof(Dn2CppObject) hands back a header-only object that
        // passes every type test and has lost its state.
        //
        // Reachable only through the reflective mouth: a call site's receiver is the
        // declaring type's own `this`, and no user type derives from an
        // intrinsic-represented one.
        dn2cpp_throw_platform_not_supported(
            (std::string("MemberwiseClone of '")
             + ((t != nullptr && t->name != nullptr) ? t->name : "<unnamed type>")
             + "' is not supported: no type-info states this type's instance extent, so "
               "there is nothing to copy. Clone it through a type-specific member "
               "instead.").c_str());
    }
    size_t bytes;
    if ((t->flags & DN2CPP_TF_VALUETYPE) != 0)
    {
        bytes = sizeof(Dn2CppObject) + static_cast<size_t>(size); // boxed value: header + payload
    }
    else
    {
        bytes = static_cast<size_t>(size > 0 ? size : 0);         // class: instanceSize is sizeof(struct)
        if (bytes < floor)
            bytes = floor;
    }
    auto* clone = static_cast<Dn2CppObject*>(dn2cpp_alloc(bytes));
    std::memcpy(clone, obj, bytes);
    // .NET finalizes the CLONE as well as the original — measured on CoreCLR 10.0.9, a
    // clone of a finalizable class runs its finalizer, so a program that clones N
    // objects sees N+1 finalizations. dn2cpp registers at newobj and at the reflective
    // ctor path; a clone is a third allocation mouth for the same type and has to
    // register too, or a cloned object's `using`-less native handle is never released
    // and nothing says so. Value types never have finalizers, so this is the class arm
    // only — and the arms above (string, SZArray, MD array) allocate through helpers
    // whose types have no finalize slot.
    if ((t->flags & DN2CPP_TF_VALUETYPE) == 0 && t->finalize != nullptr)
        dn2cpp_register_finalizer(clone);
    return clone;
}

// ---- Double/Single value semantics -----------------------------------------
// Float equality and hashing are NOT `==` and are not the numeric value, and the
// difference is observable the moment a NaN is used as a key. These four are the
// one implementation of it: the boxed arms of dn2cpp_object_equals /
// _gethashcode below call them, and so does the transpiler's inline emit for a
// Dictionary<double,V>/HashSet<float> (MethodCompiler.Call.cs). Sharing them is
// what guarantees a value hashed inline and the same value hashed boxed land in
// the same bucket.

// Double.Equals / Single.Equals: NaN equals NaN (unlike ==), and +0.0 equals
// -0.0 (like ==). A Dictionary keyed on NaN must find its key back.
int32_t dn2cpp_double_equals(double a, double b)
{
    return (a == b || (a != a && b != b)) ? 1 : 0;
}

int32_t dn2cpp_single_equals(float a, float b)
{
    return (a == b || (a != a && b != b)) ? 1 : 0;
}

// Double.GetHashCode / Single.GetHashCode: the raw BITS, folded to 32, with every
// NaN and both zeros normalized to a single value — the contract "equal values
// hash equal" then holds for the two pairs `==` and Equals disagree about.
// Truncating the number instead (`(int32_t)v`) would not merely collide: for a
// NaN or an infinity the conversion is undefined behavior.
int32_t dn2cpp_double_hash(double v)
{
    int64_t bits;
    std::memcpy(&bits, &v, sizeof(bits));
    if (((bits - 1) & 0x7FFFFFFFFFFFFFFFLL) >= 0x7FF0000000000000LL) // NaN or zero
        bits &= 0x7FF0000000000000LL;
    return static_cast<int32_t>(bits) ^ static_cast<int32_t>(bits >> 32);
}

int32_t dn2cpp_single_hash(float v)
{
    int32_t bits;
    std::memcpy(&bits, &v, sizeof(bits));
    if (((bits - 1) & 0x7FFFFFFF) >= 0x7F800000) // NaN or zero
        bits &= 0x7F800000;
    return bits;
}

int32_t dn2cpp_object_gethashcode(Dn2CppObject* obj)
{
    if (obj == nullptr)
        return 0;
    const Dn2CppTypeInfo* t = obj->type;
    if (t != nullptr && t->gethashcode != nullptr)
        return t->gethashcode(obj); // overridden GetHashCode (record/class value hash)
    // A delegate hashes by its invocation chain (never wires the gethashcode
    // slot — the BCL override is not transpiled), so distinct delegate
    // instances over the same method/target agree, consistent with
    // dn2cpp_delegate_equal below.
    if (t != nullptr && (t->flags & DN2CPP_TF_DELEGATE) != 0)
        return dn2cpp_delegate_hash(obj);
    // An NFI wrapper (CultureInfo/NumberFormatInfo/TextInfo escaped to object,
    // dn2cpp_nfi_wrap) hashes by its WRAPPED pointer, so the wrapper and a raw
    // pointer that flowed through an erased context land in the same bucket —
    // the hash counterpart of the identity rule dn2cpp_object_equals applies.
    if (t == &dn2cpp_cultureinfo_type || t == &dn2cpp_numberformatinfo_type
        || t == &dn2cpp_textinfo_type)
    {
        uint64_t n = static_cast<uint64_t>(reinterpret_cast<uintptr_t>(
            reinterpret_cast<Dn2CppNfiBox*>(obj)->nfi));
        return static_cast<int32_t>((n ^ (n >> 32)) & 0x7fffffff);
    }
    // A string hashes by value (String overrides GetHashCode). Strings have no
    // t->gethashcode slot, so handle them here — the symmetric counterpart of the
    // string branch in dn2cpp_object_equals below. Without it, a virtual
    // obj.GetHashCode() on a string (e.g. OrdinalCaseSensitiveComparer.GetHashCode,
    // i.e. StringComparer.Ordinal) falls through to the identity hash, breaking the
    // "equal values -> equal hashes" contract: a HashSet<string>/Dictionary<string,…>
    // built with StringComparer.Ordinal buckets value-equal-but-distinct keys
    // separately and never de-duplicates them.
    if (t == &dn2cpp_string_type)
        return dn2cpp_string_hashcode(reinterpret_cast<Dn2CppString*>(obj));
    // System.Type hashes by its type identity (the TypeInfo), not the object
    // address: dn2cpp_get_type_from_handle hands out a fresh Dn2CppType per
    // `typeof` call, but dn2cpp_type_equals treats two of the same type as equal,
    // so the hash must agree (a record's EqualityContract is a Type).
    if (t == &dn2cpp_type_type)
    {
        // Widen to 64 bits before the fold: a 32-bit `uintptr_t >> 32` is
        // undefined (wasm32), and the widened fold is bit-identical on 64-bit.
        uint64_t ti = static_cast<uint64_t>(
            reinterpret_cast<uintptr_t>(reinterpret_cast<Dn2CppType*>(obj)->typeInfo));
        return static_cast<int32_t>((ti ^ (ti >> 32)) & 0x7fffffff);
    }
    // Decimal and the date/time value types hash through the gethashcode slot
    // handled above.
    // A boxed enum hashes exactly like Enum.GetHashCode: the underlying primitive's own
    // GetHashCode over the raw value. A 1/2/4-byte underlying rides the box widened to int32
    // (CppTypes.Of), so the int32 read IS that value's hash for byte/sbyte/short/ushort/int;
    // a char folds v|(v<<16); a 64-bit underlying reads 8 bytes and folds low^high
    // (Int64/UInt64.GetHashCode). A fixed int32 read would drop the high half of a long/ulong
    // enum, so two 64-bit enums differing only above bit 31 would collide.
    if (t->base == &dn2cpp_enum_type)
    {
        const Dn2CppTypeInfo* u = t->enumUnderlying;
        if (u == &dn2cpp_int64_type || u == &dn2cpp_uint64_type)
        {
            uint64_t bits = *reinterpret_cast<const uint64_t*>(obj + 1);
            return static_cast<int32_t>(bits) ^ static_cast<int32_t>(bits >> 32);
        }
        int32_t v = *reinterpret_cast<const int32_t*>(obj + 1);
        return u == &dn2cpp_char_type ? (v | (v << 16)) : v;
    }
    // Boxed numeric/char/bool primitives hash by value (matching Int32.GetHashCode
    // etc. — sub-word primitives box widened to int32, see dn2cpp_object_tostring),
    // keeping "equal values -> equal hashes" for a Hashtable/ArrayList keyed by
    // boxed numbers (Regex's parser capture map is a Hashtable with boxed int keys;
    // two boxes of the same value must land in the same bucket).
    if (t == &dn2cpp_char_type) // Char.GetHashCode: value | (value << 16)
    {
        int32_t v = *reinterpret_cast<const int32_t*>(obj + 1);
        return v | (v << 16);
    }
    if (t == &dn2cpp_int32_type || t == &dn2cpp_uint32_type
        || t == &dn2cpp_bool_type || t == &dn2cpp_byte_type
        || t == &dn2cpp_sbyte_type || t == &dn2cpp_int16_type
        || t == &dn2cpp_uint16_type)
        return *reinterpret_cast<const int32_t*>(obj + 1);
    if (t == &dn2cpp_int64_type || t == &dn2cpp_uint64_type) // low ^ high, like Int64.GetHashCode
    {
        uint64_t bits = *reinterpret_cast<const uint64_t*>(obj + 1);
        return static_cast<int32_t>(bits) ^ static_cast<int32_t>(bits >> 32);
    }
    if (t == &dn2cpp_single_type) // Single.GetHashCode: raw bits, NaN/±0 normalized
    {
        float v;
        std::memcpy(&v, obj + 1, sizeof(v));
        return dn2cpp_single_hash(v);
    }
    if (t == &dn2cpp_double_type) // Double.GetHashCode: bits folded, NaN/±0 normalized
    {
        double v;
        std::memcpy(&v, obj + 1, sizeof(v));
        return dn2cpp_double_hash(v);
    }
    // A boxed IntPtr/UIntPtr hashes by its pointer-width payload, so two equal
    // boxed values agree (matches the "equal values -> equal hashes" contract; not
    // the exact .NET hash number, which is not modeled). Read the payload at its
    // real width — the box holds sizeof(intptr_t) bytes, so a fixed 8-byte read
    // would run past it on a 32-bit target — then widen for the 64-bit fold.
    if (t == &dn2cpp_intptr_type || t == &dn2cpp_uintptr_type)
    {
        uint64_t bits = static_cast<uint64_t>(*reinterpret_cast<const uintptr_t*>(obj + 1));
        return static_cast<int32_t>((bits ^ (bits >> 32)) & 0x7fffffff);
    }
    // Default Object.GetHashCode: an identity hash from the object's address.
    // Fold the pointer's halves into 31 bits (matches .NET's "always non-negative,
    // stable per instance" contract closely enough for collection bucketing).
    // Widened to 64 bits first: a 32-bit `uintptr_t >> 32` is undefined (wasm32),
    // and the widened fold is bit-identical on 64-bit.
    uint64_t p = static_cast<uint64_t>(reinterpret_cast<uintptr_t>(obj));
    return static_cast<int32_t>((p ^ (p >> 32)) & 0x7fffffff);
}

int32_t dn2cpp_object_equals(Dn2CppObject* a, Dn2CppObject* b)
{
    if (a == b)
        return 1; // same reference (or both null) — always equal
    if (a == nullptr || b == nullptr)
        return 0; // exactly one null
    const Dn2CppTypeInfo* t = a->type;
    if (t != nullptr && t->equals != nullptr)
        return t->equals(a, b); // overridden Equals(object) (value equality)
    // A delegate compares by type + invocation chain (never wires the equals
    // slot — the BCL override is not transpiled), matching Delegate.Equals.
    if (t != nullptr && (t->flags & DN2CPP_TF_DELEGATE) != 0)
        return dn2cpp_delegate_equal(a, b);
    // An NFI wrapper (dn2cpp_nfi_wrap) compares by its WRAPPED pointer — the
    // culture/format-info identity. Wrapper-vs-wrapper requires the same
    // wrapper type too (a CultureInfo never equals the TextInfo sharing its
    // runtime pointer); against a non-wrapper the raw pointer is compared
    // as-is, so a raw NFI pointer that flowed through an erased context still
    // equals its wrapped form. (Reading b->type is a value compare only —
    // never dereferenced — so a punned raw operand stays safe.)
    if (t == &dn2cpp_cultureinfo_type || t == &dn2cpp_numberformatinfo_type
        || t == &dn2cpp_textinfo_type)
    {
        const void* an = reinterpret_cast<Dn2CppNfiBox*>(a)->nfi;
        const Dn2CppTypeInfo* bt = b->type;
        if (bt == &dn2cpp_cultureinfo_type || bt == &dn2cpp_numberformatinfo_type
            || bt == &dn2cpp_textinfo_type)
            return (bt == t && an == reinterpret_cast<Dn2CppNfiBox*>(b)->nfi) ? 1 : 0;
        return an == static_cast<const void*>(b) ? 1 : 0;
    }
    // A string compares by value (String overrides Equals). Strings have no t->equals
    // slot, so handle them here — otherwise object.Equals / static Object.Equals would
    // fall through to reference equality, diverging from .NET for distinct equal strings.
    if (t == &dn2cpp_string_type)
        return (b->type == &dn2cpp_string_type
                && dn2cpp_string_equals(reinterpret_cast<Dn2CppString*>(a),
                                        reinterpret_cast<Dn2CppString*>(b))) ? 1 : 0;
    // System.Type compares by type identity (consistent with the hash above and
    // dn2cpp_type_equals): two Type objects for the same type are equal.
    if (t == &dn2cpp_type_type && b->type == &dn2cpp_type_type)
        return reinterpret_cast<Dn2CppType*>(a)->typeInfo == reinterpret_cast<Dn2CppType*>(b)->typeInfo ? 1 : 0;
    // Decimal and the date/time value types compare through the equals slot
    // handled above.
    // Two boxed enums are equal iff same enum type and same underlying value, compared at the
    // underlying's width — a 64-bit enum needs the full 8 bytes (an int32 read would ignore any
    // difference above bit 31); a 1/2/4-byte one rides the box widened to int32.
    if (t->base == &dn2cpp_enum_type)
    {
        if (b->type != t)
            return 0;
        const Dn2CppTypeInfo* u = t->enumUnderlying;
        if (u == &dn2cpp_int64_type || u == &dn2cpp_uint64_type)
            return *reinterpret_cast<const uint64_t*>(a + 1) == *reinterpret_cast<const uint64_t*>(b + 1) ? 1 : 0;
        return *reinterpret_cast<const int32_t*>(a + 1) == *reinterpret_cast<const int32_t*>(b + 1) ? 1 : 0;
    }
    // Boxed numeric/char/bool primitives compare by value iff same type (a boxed
    // int never equals a boxed uint even at the same value, matching .NET) — the
    // symmetric counterpart of the boxed-primitive value hashes above.
    if (t == &dn2cpp_int32_type || t == &dn2cpp_uint32_type
        || t == &dn2cpp_bool_type || t == &dn2cpp_char_type
        || t == &dn2cpp_byte_type || t == &dn2cpp_sbyte_type
        || t == &dn2cpp_int16_type || t == &dn2cpp_uint16_type)
        return (b->type == t
                && *reinterpret_cast<const int32_t*>(a + 1) == *reinterpret_cast<const int32_t*>(b + 1)) ? 1 : 0;
    if (t == &dn2cpp_int64_type || t == &dn2cpp_uint64_type)
        return (b->type == t
                && *reinterpret_cast<const uint64_t*>(a + 1) == *reinterpret_cast<const uint64_t*>(b + 1)) ? 1 : 0;
    // Boxed float/double: value equality with .NET's Equals semantics — ±0 are
    // equal, NaN equals NaN (unlike ==).
    if (t == &dn2cpp_single_type)
    {
        if (b->type != t)
            return 0;
        float fa, fb2;
        std::memcpy(&fa, a + 1, sizeof(fa));
        std::memcpy(&fb2, b + 1, sizeof(fb2));
        return dn2cpp_single_equals(fa, fb2);
    }
    if (t == &dn2cpp_double_type)
    {
        if (b->type != t)
            return 0;
        double da, db;
        std::memcpy(&da, a + 1, sizeof(da));
        std::memcpy(&db, b + 1, sizeof(db));
        return dn2cpp_double_equals(da, db);
    }
    // Two boxed IntPtr/UIntPtr are equal iff same type and same 8-byte payload — a
    // boxed IntPtr never equals a boxed UIntPtr even at the same value.
    if (t == &dn2cpp_intptr_type || t == &dn2cpp_uintptr_type)
        return (b->type == t
                && *reinterpret_cast<const uint64_t*>(a + 1) == *reinterpret_cast<const uint64_t*>(b + 1)) ? 1 : 0;
    return 0; // default Object.Equals: reference equality (already !=, so false)
}

namespace {
// Signed/unsigned three-way over machine scalars — the sign is the operand type's.
template <typename T> inline int32_t dn2cpp_cmp3(T x, T y) { return x < y ? -1 : (x > y ? 1 : 0); }
// Floating TOTAL order, byte-for-byte the expression MethodCompiler.TryCompareLValue emits:
// a NaN sorts below every number (including -inf) and compares equal to NaN, so `<`/`>` alone
// (both false for a NaN) cannot leave a sort's result dependent on visit order.
template <typename T> inline int32_t dn2cpp_cmp3_total(T x, T y)
{
    return x < y ? -1 : (x > y ? 1 : (x == y ? 0 : (x != x ? (y != y ? 0 : -1) : 1)));
}
// Two box payloads through one of the by-value intrinsic three-ways.
template <typename T> inline int32_t dn2cpp_bx_cmp(const void* pa, const void* pb, int32_t (*cmp)(T, T))
{
    T x, y;
    std::memcpy(&x, pa, sizeof(T));
    std::memcpy(&y, pb, sizeof(T));
    return cmp(x, y);
}
} // namespace

int32_t dn2cpp_object_compare(Dn2CppObject* a, Dn2CppObject* b, const Dn2CppTypeInfo* icomparable_ti)
{
    // Comparer.Default null order: equal refs (incl. both null) are equal; null sorts first.
    if (a == b)
        return 0;
    if (a == nullptr)
        return -1;
    if (b == nullptr)
        return 1;
    const Dn2CppTypeInfo* t = a->type;
    // String — ordinal (see the header note): matches TryCompareLValue's dn2cpp_str_compare(…,4).
    // dn2cpp_str_compare returns the unclamped signed gap; take its sign.
    if (t == &dn2cpp_string_type && b->type == &dn2cpp_string_type)
        return dn2cpp_cmp3<int32_t>(dn2cpp_str_compare(reinterpret_cast<Dn2CppString*>(a),
                                                       reinterpret_cast<Dn2CppString*>(b), 4), 0);
    // Boxed enum: compare at the backing width + signedness (read from enumUnderlying, exactly as
    // dn2cpp_pinned_data_addr reads it). A 64-bit-backed enum orders on all 64 bits, matching
    // TryCompareLValue's CppTypes.Of. Same enum type required (payload read is width-exact, so a
    // cross-type read would be unsound); an enum backed by something outside the eight legal integer
    // bases (or an absent enumUnderlying) is refused rather than read at a guessed width.
    if (t->base == &dn2cpp_enum_type && b->type == t)
    {
        const void* pa = a + 1;
        const void* pb = b + 1;
        const Dn2CppTypeInfo* u = t->enumUnderlying;
        if (u == &dn2cpp_int32_type)  return dn2cpp_cmp3<int32_t>(*static_cast<const int32_t*>(pa),  *static_cast<const int32_t*>(pb));
        if (u == &dn2cpp_uint32_type) return dn2cpp_cmp3<uint32_t>(*static_cast<const uint32_t*>(pa), *static_cast<const uint32_t*>(pb));
        if (u == &dn2cpp_int64_type)  return dn2cpp_cmp3<int64_t>(*static_cast<const int64_t*>(pa),  *static_cast<const int64_t*>(pb));
        if (u == &dn2cpp_uint64_type) return dn2cpp_cmp3<uint64_t>(*static_cast<const uint64_t*>(pa), *static_cast<const uint64_t*>(pb));
        if (u == &dn2cpp_byte_type)   return dn2cpp_cmp3<uint8_t>(*static_cast<const uint8_t*>(pa),   *static_cast<const uint8_t*>(pb));
        if (u == &dn2cpp_sbyte_type)  return dn2cpp_cmp3<int8_t>(*static_cast<const int8_t*>(pa),     *static_cast<const int8_t*>(pb));
        if (u == &dn2cpp_int16_type)  return dn2cpp_cmp3<int16_t>(*static_cast<const int16_t*>(pa),   *static_cast<const int16_t*>(pb));
        if (u == &dn2cpp_uint16_type) return dn2cpp_cmp3<uint16_t>(*static_cast<const uint16_t*>(pa), *static_cast<const uint16_t*>(pb));
        dn2cpp_throw_platform_not_supported(
            (std::string("Array.Sort/BinarySearch: enum '") + (t->name ? t->name : "<unknown>")
             + "' has an unsupported underlying type").c_str());
    }
    // Boxed numeric/char/bool primitives — read each at its NATURAL width and signedness (a boxed
    // int never orders against a boxed uint: same-type required, as .NET's IComparable.CompareTo
    // throws otherwise). Natural-width reads are payload-safe regardless of box padding.
    if (b->type == t)
    {
        const void* pa = a + 1;
        const void* pb = b + 1;
        if (t == &dn2cpp_bool_type)   return dn2cpp_cmp3<uint8_t>(*static_cast<const uint8_t*>(pa),   *static_cast<const uint8_t*>(pb));
        if (t == &dn2cpp_sbyte_type)  return dn2cpp_cmp3<int8_t>(*static_cast<const int8_t*>(pa),     *static_cast<const int8_t*>(pb));
        if (t == &dn2cpp_byte_type)   return dn2cpp_cmp3<uint8_t>(*static_cast<const uint8_t*>(pa),   *static_cast<const uint8_t*>(pb));
        if (t == &dn2cpp_int16_type)  return dn2cpp_cmp3<int16_t>(*static_cast<const int16_t*>(pa),   *static_cast<const int16_t*>(pb));
        if (t == &dn2cpp_uint16_type) return dn2cpp_cmp3<uint16_t>(*static_cast<const uint16_t*>(pa), *static_cast<const uint16_t*>(pb));
        if (t == &dn2cpp_char_type)   return dn2cpp_cmp3<uint16_t>(*static_cast<const uint16_t*>(pa), *static_cast<const uint16_t*>(pb));
        if (t == &dn2cpp_int32_type)  return dn2cpp_cmp3<int32_t>(*static_cast<const int32_t*>(pa),   *static_cast<const int32_t*>(pb));
        if (t == &dn2cpp_uint32_type) return dn2cpp_cmp3<uint32_t>(*static_cast<const uint32_t*>(pa), *static_cast<const uint32_t*>(pb));
        if (t == &dn2cpp_int64_type)  return dn2cpp_cmp3<int64_t>(*static_cast<const int64_t*>(pa),   *static_cast<const int64_t*>(pb));
        if (t == &dn2cpp_uint64_type) return dn2cpp_cmp3<uint64_t>(*static_cast<const uint64_t*>(pa), *static_cast<const uint64_t*>(pb));
        if (t == &dn2cpp_intptr_type) return dn2cpp_cmp3<intptr_t>(*static_cast<const intptr_t*>(pa), *static_cast<const intptr_t*>(pb));
        if (t == &dn2cpp_uintptr_type) return dn2cpp_cmp3<uintptr_t>(*static_cast<const uintptr_t*>(pa), *static_cast<const uintptr_t*>(pb));
        if (t == &dn2cpp_single_type)
        {
            float fa, fb;
            std::memcpy(&fa, pa, sizeof(fa));
            std::memcpy(&fb, pb, sizeof(fb));
            return dn2cpp_cmp3_total<float>(fa, fb);
        }
        if (t == &dn2cpp_double_type)
        {
            double da, db;
            std::memcpy(&da, pa, sizeof(da));
            std::memcpy(&db, pb, sizeof(db));
            return dn2cpp_cmp3_total<double>(da, db);
        }
        // Decimal and the date/time value types order through the same runtime three-way
        // MethodCompiler.TryCompareLValue's intrinsic-value-type arm emits, so a value
        // ordered here and one ordered inline agree. Reads are memcpy'd: these payloads
        // are wider than a machine word and a box carries no alignment guarantee.
        if (t == &dn2cpp_decimal_type)  return dn2cpp_bx_cmp<Dn2CppDecimal>(pa, pb, &dn2cpp_decimal_cmp);
        if (t == &dn2cpp_timespan_type) return dn2cpp_bx_cmp<Dn2CppTimeSpan>(pa, pb, &dn2cpp_timespan_cmp);
        if (t == &dn2cpp_datetime_type) return dn2cpp_bx_cmp<Dn2CppDateTime>(pa, pb, &dn2cpp_datetime_cmp);
        if (t == &dn2cpp_datetimeoffset_type)
            return dn2cpp_bx_cmp<Dn2CppDateTimeOffset>(pa, pb, &dn2cpp_datetimeoffset_cmp);
        if (t == &dn2cpp_dateonly_type) return dn2cpp_bx_cmp<Dn2CppDateOnly>(pa, pb, &dn2cpp_dateonly_cmp);
        if (t == &dn2cpp_timeonly_type) return dn2cpp_bx_cmp<Dn2CppTimeOnly>(pa, pb, &dn2cpp_timeonly_cmp);
    }
    // A user reference type: dispatch the non-generic System.IComparable.CompareTo(object) — a.CompareTo(b)
    // when a implements it, else the asymmetric -(b.CompareTo(a)) System.Collections.Comparer.Compare
    // uses for a heterogeneous array. IComparable declares exactly one method, so its slot is 0 (verified
    // against the emitted interface method table). Primitives/enum/string are handled by the inline arms
    // ABOVE, and the ORDER is what keeps them there — not an absent map. A boxed string carries a real
    // IComparable row whenever dn2cpp_string_set_interfaces ran, and so does every boxed enum
    // (dn2cpp_enum_set_interfaces installs ONE map onto dn2cpp_enum_type, which this walk finds through the
    // box's base chain), so try_resolve_interface answers non-null for them. Do not hoist this probe above
    // the inline arms on the old "interfaces=nullptr" premise: it would route the common same-type enum and
    // string orderings through a dispatch whose slot is only filled when reachability crossed that impl in.
    // Two boxed enums only reach HERE when their types DIFFER (the arm above requires b->type == t), and
    // dispatching System.Enum.CompareTo(object) is exactly right there — it raises the ArgumentException
    // real .NET raises, instead of the type-not-comparable refusal below.
    if (icomparable_ti != nullptr)
    {
        if (const void** sa = dn2cpp_try_resolve_interface(t, icomparable_ti))
            return dn2cpp_cmp3<int32_t>(
                (reinterpret_cast<int32_t (*)(Dn2CppObject*, Dn2CppObject*)>(const_cast<void*>(sa[0])))(a, b), 0);
        if (const void** sb = dn2cpp_try_resolve_interface(b->type, icomparable_ti))
            return -dn2cpp_cmp3<int32_t>(
                (reinterpret_cast<int32_t (*)(Dn2CppObject*, Dn2CppObject*)>(const_cast<void*>(sb[0])))(b, a), 0);
    }
    // Neither an inlined kind nor IComparable — refuse loudly (never a silent 0). Mirrors real .NET's
    // Comparer.Default, which throws ArgumentException("At least one object must implement IComparable").
    dn2cpp_throw_platform_not_supported(
        (std::string("Array.Sort/BinarySearch: element type '") + (t->name ? t->name : "<unknown>")
         + "' is not comparable — it implements neither a lowered primitive/enum/string order nor "
           "System.IComparable. Implement IComparable, or sort/search with an IComparer.").c_str());
    return 0; // unreachable (the throw does not return)
}

// System.Enum::CompareTo(object) — the synthesized value body (CoreIntrinsics.BrEnumInstanceFormat)
// calls this. Enum.CompareTo orders by the underlying value and returns the -1/0/1 sign: a null
// target sorts first (this > null -> 1), a different enum type is an ArgumentException, and same
// type delegates to dn2cpp_object_compare's boxed-enum arm so the width+signedness ladder (byte..
// ulong) lives in exactly one place. The receiver `a` is `this`, never null for an instance call.
int32_t dn2cpp_enum_compareto(Dn2CppObject* a, Dn2CppObject* b)
{
    if (b == nullptr)
        return 1;
    if (a == nullptr || a->type != b->type)
        dn2cpp_throw_argument();
    return dn2cpp_object_compare(a, b, nullptr);
}

// ---- HashHelpers (Dictionary<K,V> bucket sizing) ----
// Ported from dotnet/runtime (HashHelpers.cs).
// Copyright (c) .NET Foundation and Contributors. Licensed under the MIT License.
// Source: https://github.com/dotnet/runtime/blob/main/src/libraries/System.Private.CoreLib/src/System/Collections/HashHelpers.cs

static bool dn2cpp_is_prime(int32_t candidate)
{
    if ((candidate & 1) == 0)
        return candidate == 2;
    int32_t limit = static_cast<int32_t>(std::sqrt(static_cast<double>(candidate)));
    for (int32_t divisor = 3; divisor <= limit; divisor += 2)
        if (candidate % divisor == 0)
            return false;
    return true;
}

// The BCL's cached prime ladder (each ~1.2x the previous); covers all but
// pathologically large dictionaries, which fall through to the prime search.
// File-scope so HashHelpers.Primes (a ReadOnlySpan<int> over this same table in
// the BCL) can be handed out as a span; byte-for-byte the real .NET Primes array.
static const int32_t dn2cpp_hashhelpers_primes_tbl[] = {
    3, 7, 11, 17, 23, 29, 37, 47, 59, 71, 89, 107, 131, 163, 197, 239, 293,
    353, 431, 521, 631, 761, 919, 1103, 1327, 1597, 1931, 2333, 2801, 3371,
    4049, 4861, 5839, 7013, 8419, 10103, 12143, 14591, 17519, 21023, 25229,
    30293, 36353, 43627, 52361, 62851, 75431, 90523, 108631, 130363, 156437,
    187751, 225307, 270371, 324449, 389357, 467237, 560689, 672827, 807403,
    968897, 1162687, 1395263, 1674319, 2009191, 2411033, 2893249, 3471899,
    4166287, 4999559, 5999471, 7199369 };

const int32_t* dn2cpp_hashhelpers_primes_data()
{
    return dn2cpp_hashhelpers_primes_tbl;
}

int32_t dn2cpp_hashhelpers_primes_count()
{
    return (int32_t)(sizeof(dn2cpp_hashhelpers_primes_tbl)
                     / sizeof(dn2cpp_hashhelpers_primes_tbl[0]));
}

int32_t dn2cpp_hashhelpers_getprime(int32_t min)
{
    for (int32_t p : dn2cpp_hashhelpers_primes_tbl)
        if (p >= min)
            return p;
    // HashPrime is 101; skip primes p where (p-1) % 101 == 0 (BCL behavior).
    for (int32_t i = (min | 1); i < 0x7fffffff; i += 2)
        if (dn2cpp_is_prime(i) && (i - 1) % 101 != 0)
            return i;
    return min;
}

int32_t dn2cpp_hashhelpers_expandprime(int32_t oldSize)
{
    const int32_t maxPrimeArrayLength = 0x7feffffd;
    int32_t newSize = 2 * oldSize;
    if (static_cast<uint32_t>(newSize) > static_cast<uint32_t>(maxPrimeArrayLength)
        && maxPrimeArrayLength > oldSize)
        return maxPrimeArrayLength;
    return dn2cpp_hashhelpers_getprime(newSize);
}

uint64_t dn2cpp_hashhelpers_getfastmodmultiplier(uint32_t divisor)
{
    return 0xffffffffffffffffULL / divisor + 1;
}

uint32_t dn2cpp_hashhelpers_fastmod(uint32_t value, uint32_t divisor, uint64_t multiplier)
{
    // FastMod from the BCL: equivalent to value % divisor for power-of-two-free
    // divisors via a 64-bit reciprocal multiply.
    uint32_t highbits = static_cast<uint32_t>(
        ((((multiplier * value) >> 32) + 1) * divisor) >> 32);
    return highbits;
}

// Console output is serialized per call by a single recursive mutex so concurrent
// Console.Write/WriteLine from multiple threads stay byte-atomic (matching .NET's
// synchronized Console.Out/Error). It is recursive because the WriteLine/typed
// helpers call the string/Write helpers, which lock again. This guarantees
// un-garbled output; it does NOT order lines across threads — deterministic gates
// must still join/barrier before printing. Uncontended (behavior-neutral) for a
// single thread.
static std::recursive_mutex& g_console_mtx = dn2cpp_never_destroyed<std::recursive_mutex>();

// The WriteLine line terminator, matching .NET's Environment.NewLine: "\r\n" on
// Windows, "\n" elsewhere. Emitted verbatim — on Windows stdout/stderr are in
// binary mode (dn2cpp_runtime_init), so these bytes reach the destination
// untranslated, and newlines embedded inside a written string stay a bare '\n'
// exactly as .NET emits them.
#ifdef _WIN32
static constexpr char kDn2cppNewline[] = "\r\n";
#else
static constexpr char kDn2cppNewline[] = "\n";
#endif

// EVERY console byte the runtime emits leaves through this one call, and that is
// the property `dn2cpp_pal_console_write` exists to have: a target whose console
// is not stdio implements the two PAL entries and nothing in this file — a core
// file — has to change. So do not reintroduce a direct `std::printf`/`fwrite` to
// stdout or stderr below; a single one of them is a line that silently vanishes
// on such a target, and it vanishes only there.
//
// The scalar helpers render into a small stack buffer before emitting. Each buffer is
// sized for the longest possible rendering (an int64 is at most 20 characters plus a
// sign).
static inline void dn2cpp_console_emit(int stream, const char* bytes, size_t n)
{
    dn2cpp_pal_console_write(stream, bytes, n);
}

static inline void dn2cpp_write_newline(int stream)
{
    dn2cpp_console_emit(stream, kDn2cppNewline, sizeof(kDn2cppNewline) - 1);
}

// snprintf into a fixed buffer, then one sink write. 32 bytes holds every
// rendering these three formats can produce ("%d" <= 11, "%lld" <= 20, the two
// bool literals <= 5), so the truncation branch snprintf documents is
// unreachable; the returned length is still clamped rather than trusted, because
// an unclamped negative or over-long return would be a buffer overrun rather
// than a wrong string.
static inline void dn2cpp_console_emit_fmt(int stream, const char* fmt, ...)
{
    char buf[32];
    va_list ap;
    va_start(ap, fmt);
    int n = std::vsnprintf(buf, sizeof(buf), fmt, ap);
    va_end(ap);
    if (n <= 0)
        return;
    size_t len = static_cast<size_t>(n) < sizeof(buf) - 1 ? static_cast<size_t>(n) : sizeof(buf) - 1;
    dn2cpp_console_emit(stream, buf, len);
}

// The UTF-8 transcode shared by the stdout and stderr string writers: identical
// bytes, identical allocation, only the destination stream differs.
static void dn2cpp_console_emit_str(int stream, Dn2CppString* s)
{
    if (s == nullptr)
        return;
    int32_t n = dn2cpp_string_to_utf8(s, nullptr, 0);
    char* buf = static_cast<char*>(dn2cpp_alloc(static_cast<size_t>(n) + 1));
    dn2cpp_string_to_utf8(s, buf, n);
    std::lock_guard<std::recursive_mutex> lk(g_console_mtx);
    dn2cpp_console_emit(stream, buf, static_cast<size_t>(n));
}

void dn2cpp_console_writeline_empty()
{
    std::lock_guard<std::recursive_mutex> lk(g_console_mtx);
    dn2cpp_write_newline(DN2CPP_PAL_CONSOLE_OUT);
}

// Console.Write family (no trailing newline); the WriteLine variants below add it
// so both share identical formatting.
void dn2cpp_console_write_str(Dn2CppString* s)
{
    dn2cpp_console_emit_str(DN2CPP_PAL_CONSOLE_OUT, s);
}

void dn2cpp_console_write_i4(int32_t v) { std::lock_guard<std::recursive_mutex> lk(g_console_mtx); dn2cpp_console_emit_fmt(DN2CPP_PAL_CONSOLE_OUT, "%d", v); }
void dn2cpp_console_write_i8(int64_t v) { std::lock_guard<std::recursive_mutex> lk(g_console_mtx); dn2cpp_console_emit_fmt(DN2CPP_PAL_CONSOLE_OUT, "%lld", static_cast<long long>(v)); }
void dn2cpp_console_write_r8(double v) { dn2cpp_console_write_str(dn2cpp_double_to_string(v)); }
void dn2cpp_console_write_r4(float v) { dn2cpp_console_write_str(dn2cpp_float_to_string(v)); }
void dn2cpp_console_write_bool(int32_t v) { std::lock_guard<std::recursive_mutex> lk(g_console_mtx); dn2cpp_console_emit_fmt(DN2CPP_PAL_CONSOLE_OUT, "%s", v != 0 ? "True" : "False"); }

void dn2cpp_console_writeline_str(Dn2CppString* s)
{
    std::lock_guard<std::recursive_mutex> lk(g_console_mtx);
    dn2cpp_console_write_str(s);
    dn2cpp_write_newline(DN2CPP_PAL_CONSOLE_OUT);
}

void dn2cpp_console_writeline_i4(int32_t v) { std::lock_guard<std::recursive_mutex> lk(g_console_mtx); dn2cpp_console_write_i4(v); dn2cpp_write_newline(DN2CPP_PAL_CONSOLE_OUT); }
void dn2cpp_console_writeline_i8(int64_t v) { std::lock_guard<std::recursive_mutex> lk(g_console_mtx); dn2cpp_console_write_i8(v); dn2cpp_write_newline(DN2CPP_PAL_CONSOLE_OUT); }
void dn2cpp_console_writeline_r8(double v) { std::lock_guard<std::recursive_mutex> lk(g_console_mtx); dn2cpp_console_write_r8(v); dn2cpp_write_newline(DN2CPP_PAL_CONSOLE_OUT); }
void dn2cpp_console_writeline_r4(float v) { std::lock_guard<std::recursive_mutex> lk(g_console_mtx); dn2cpp_console_write_r4(v); dn2cpp_write_newline(DN2CPP_PAL_CONSOLE_OUT); }
void dn2cpp_console_writeline_bool(int32_t v) { std::lock_guard<std::recursive_mutex> lk(g_console_mtx); dn2cpp_console_write_bool(v); dn2cpp_write_newline(DN2CPP_PAL_CONSOLE_OUT); }

// Console.Error / TextWriter family. A Dn2CppTextWriter is an opaque
// handle carrying the destination stream; Console.get_Error returns the singleton stderr
// writer. Each helper mirrors its dn2cpp_console_* (stdout) counterpart byte-for-byte —
// only the destination stream differs — so Console.Error.Write/WriteLine produce the same
// text as Console.Write/WriteLine, on stderr. Shares g_console_mtx so stdout and stderr
// writes from different threads do not interleave mid-line.
//
// The handle carries a PAL stream ID rather than a std::FILE*, because a FILE*
// is precisely what a target with no stdio cannot produce. It stays an opaque
// struct — the transpiler models it as a pointer and never dereferences it — so
// this is a representation change with no surface anywhere else.
struct Dn2CppTextWriter { int stream; };

Dn2CppTextWriter* dn2cpp_console_error()
{
    static Dn2CppTextWriter w{ DN2CPP_PAL_CONSOLE_ERR };
    return &w;
}

void dn2cpp_textwriter_write_str(Dn2CppTextWriter* w, Dn2CppString* s)
{
    dn2cpp_console_emit_str(w->stream, s);
}

void dn2cpp_textwriter_write_i4(Dn2CppTextWriter* w, int32_t v) { std::lock_guard<std::recursive_mutex> lk(g_console_mtx); dn2cpp_console_emit_fmt(w->stream, "%d", v); }
void dn2cpp_textwriter_write_i8(Dn2CppTextWriter* w, int64_t v) { std::lock_guard<std::recursive_mutex> lk(g_console_mtx); dn2cpp_console_emit_fmt(w->stream, "%lld", static_cast<long long>(v)); }
void dn2cpp_textwriter_write_r8(Dn2CppTextWriter* w, double v) { dn2cpp_textwriter_write_str(w, dn2cpp_double_to_string(v)); }
void dn2cpp_textwriter_write_r4(Dn2CppTextWriter* w, float v) { dn2cpp_textwriter_write_str(w, dn2cpp_float_to_string(v)); }
void dn2cpp_textwriter_write_bool(Dn2CppTextWriter* w, int32_t v) { std::lock_guard<std::recursive_mutex> lk(g_console_mtx); dn2cpp_console_emit_fmt(w->stream, "%s", v != 0 ? "True" : "False"); }

void dn2cpp_textwriter_writeline_empty(Dn2CppTextWriter* w) { std::lock_guard<std::recursive_mutex> lk(g_console_mtx); dn2cpp_write_newline(w->stream); }
void dn2cpp_textwriter_writeline_str(Dn2CppTextWriter* w, Dn2CppString* s) { std::lock_guard<std::recursive_mutex> lk(g_console_mtx); dn2cpp_textwriter_write_str(w, s); dn2cpp_write_newline(w->stream); }
void dn2cpp_textwriter_writeline_i4(Dn2CppTextWriter* w, int32_t v) { std::lock_guard<std::recursive_mutex> lk(g_console_mtx); dn2cpp_textwriter_write_i4(w, v); dn2cpp_write_newline(w->stream); }
void dn2cpp_textwriter_writeline_i8(Dn2CppTextWriter* w, int64_t v) { std::lock_guard<std::recursive_mutex> lk(g_console_mtx); dn2cpp_textwriter_write_i8(w, v); dn2cpp_write_newline(w->stream); }
void dn2cpp_textwriter_writeline_r8(Dn2CppTextWriter* w, double v) { std::lock_guard<std::recursive_mutex> lk(g_console_mtx); dn2cpp_textwriter_write_r8(w, v); dn2cpp_write_newline(w->stream); }
void dn2cpp_textwriter_writeline_r4(Dn2CppTextWriter* w, float v) { std::lock_guard<std::recursive_mutex> lk(g_console_mtx); dn2cpp_textwriter_write_r4(w, v); dn2cpp_write_newline(w->stream); }
void dn2cpp_textwriter_writeline_bool(Dn2CppTextWriter* w, int32_t v) { std::lock_guard<std::recursive_mutex> lk(g_console_mtx); dn2cpp_textwriter_write_bool(w, v); dn2cpp_write_newline(w->stream); }

// ---- char classification, full BMP ----
// Backed by the generated dn2cpp_char_unicode_category table, so each helper
// matches .NET for every BMP code point.
// (IsWhiteSpace is inline in dn2cpp_core.h — its Latin-1 arm is the hot path
// of every Trim/split scan.)

int32_t dn2cpp_char_is_control(char16_t c)
{
    // C0 controls (U+0000–U+001F) and DEL + C1 controls (U+007F–U+009F) —
    // exactly the Unicode Cc category, which has no members above U+009F.
    return (c <= 0x1F || (c >= 0x7F && c <= 0x9F)) ? 1 : 0;
}

int32_t dn2cpp_char_is_punctuation(char16_t c)
{
    // Unicode categories Pc..Po (18..24).
    int32_t cat = dn2cpp_char_unicode_category(c);
    return (cat >= 18 && cat <= 24) ? 1 : 0;
}

int32_t dn2cpp_char_is_symbol(char16_t c)
{
    // Unicode categories Sm/Sc/Sk/So (25..28).
    int32_t cat = dn2cpp_char_unicode_category(c);
    return (cat >= 25 && cat <= 28) ? 1 : 0;
}

Dn2CppString* dn2cpp_isb_to_string(Dn2CppISB* h)
{
    char16_t* dst;
    Dn2CppString* s = dn2cpp_string_alloc(&dst, h->length);
    if (h->length > 0)
        std::memcpy(dst, h->buf, static_cast<size_t>(h->length) * sizeof(char16_t));
    h->buf = nullptr;
    h->length = 0;
    h->capacity = 0;
    return s;
}

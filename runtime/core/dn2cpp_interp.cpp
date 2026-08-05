// dn2cpp_interp.cpp — hot update: the Baked Patch Image loader/binder and the
// minimal IL interpreter. The spec lives in docs/BPI-FORMAT.md — the format,
// the interpreter/converter surface (§"Conversion surface", the patch fence),
// §"Load & bind" (imports, static storage, patch-type construction and
// layout), §"N2M trampolines", §"Delegate thunks", the EH model, and the v1
// carve-outs. This file implements that spec; only implementation-local
// invariants are stated here, at their sites.
//
// Execution is zero-copy: instructions, names, and ldstr characters are read
// from the blob in place. The blob must therefore be GC-visible memory
// (dn2cpp_alloc), and every loaded image is kept on an append-only static
// list — a data-segment GC root — so blobs live for the process lifetime
// (v1: no unloading).
//
// Operand kinds are converter-decided hints baked into each instruction, so
// execution never infers types.

#include "dn2cpp_interp.h"

#include "dn2cpp_interp_regops.h"

#include "dn2cpp_core.h"

#include <cmath>
#include <cstdio>
#include <cstring>

// POSIX directory enumeration for the deployment driver
// (dn2cpp_hotupdate_load_dir). Deliberately not std::filesystem: the runtime
// uses it nowhere else, it drags -lstdc++fs on older libstdc++, and <dirent.h>
// also works under Emscripten.
#if defined(_WIN32)
// Win32 has no <dirent.h>. Reproduce just the slice of the POSIX surface
// dn2cpp_hotupdate_load_dir calls (DIR/dirent/opendir/readdir/closedir) over
// FindFirstFileA/FindNextFileA, so that loop stays platform-independent. The
// CRT's <sys/stat.h> already declares a POSIX-shaped `struct stat` + `stat()`;
// only the S_ISREG macro is missing.
#include <windows.h>
#include <sys/stat.h>
#define S_ISREG(m) (((m) & S_IFMT) == S_IFREG)

struct dirent { char d_name[MAX_PATH]; };
struct DIR { HANDLE h; WIN32_FIND_DATAA data; bool first; struct dirent ent; };

static DIR* opendir(const char* path)
{
    char pattern[MAX_PATH];
    std::snprintf(pattern, sizeof(pattern), "%s\\*", path);
    DIR* dp = new DIR();
    dp->h = FindFirstFileA(pattern, &dp->data);
    if (dp->h == INVALID_HANDLE_VALUE)
    {
        delete dp;
        return nullptr;
    }
    dp->first = true;
    return dp;
}

static struct dirent* readdir(DIR* dp)
{
    if (!dp->first && !FindNextFileA(dp->h, &dp->data))
        return nullptr;
    dp->first = false;
    std::snprintf(dp->ent.d_name, sizeof(dp->ent.d_name), "%s", dp->data.cFileName);
    return &dp->ent;
}

static void closedir(DIR* dp)
{
    FindClose(dp->h);
    delete dp;
}
#else
#include <dirent.h>
#include <sys/stat.h>
#endif

namespace
{

// How a bound method import is invoked by the `call` arm.
enum : uint32_t
{
    kShapeVoidStr = 1,   // void (*)(Dn2CppString*) — the intrinsic import table
    kShapeInvoker = 2,   // through a Dn2CppMethodInfo invoker thunk
    kShapeVoidEmpty = 3, // void (*)()
    kShapeVoidI4 = 4,    // void (*)(int32_t)
    kShapeVoidI8 = 5,    // void (*)(int64_t)
    kShapeVoidR8 = 6,    // void (*)(double)
    kShapeVoidBool = 7,  // void (*)(int32_t), printed as True/False
    kShapeRefRetObj = 8, // Dn2CppObject* (*)(Dn2CppObject*) — instance intrinsic, no
                         // args, reference (string/Type/...) return
    kShapeExceptionNew = 9, // newobj on an exception type -> dn2cpp_exception_new
    kShapeRefRetStr = 10, // Dn2CppObject* (*)(Dn2CppString*) — static intrinsic, one
                          // string arg, reference return
    kShapeDelegateInvoke = 11, // callvirt <delegate>::Invoke -> the pre-emitted M2N
                               // invoke bridge (fnPtr), which unpacks the argument
                               // slots and dispatches through the delegate's invoker
    kShapeRefRetRefN = 12, // Dn2CppObject* (*)(Dn2CppObject* const*) — static
                           // intrinsic, argCount reference args handed over as a
                           // dense array (arg 0 first), reference return
};

// What a kShapeRefRetObj row's DECLARED receiver type must be. A method import
// binds by name alone and nothing signs a `.bpi`, so a malformed image can label
// any call site with any intrinsic row — and the helpers behind this shape
// reinterpret_cast the receiver with no header test of their own. The row states
// what it needs and both dispatch loops test it before the call.
// kRecvUnspecified is the zero value on purpose: a row minted without a kind is
// refused at the call arm rather than aliasing kRecvAny.
enum : uint32_t
{
    kRecvUnspecified = 0,
    kRecvAny = 1,        // System.Object — every non-null reference is one
    kRecvException = 2,  // System.Exception, by base chain
    kRecvType = 3,       // System.Type — a Dn2CppType, exactly
    kRecvMemberInfo = 4, // System.Reflection.MemberInfo — the handle headers
                         // dn2cpp_memberinfo_name dispatches on
};

// The same axis one position over: what a row's REFERENCE PARAMETERS must be.
// The kind is per ROW and per POSITION, not per shape — the reference-argument
// shapes are not homogeneous: kShapeRefRetStr's two rows share one C++ signature
// (`Dn2CppObject* (*)(Dn2CppString*)`) yet one of them casts its operand
// straight to a Dn2CppArrayRef, so reading either under the other's layout
// follows a wrong pointer. Scalar shapes dereference nothing and need no kind.
// kArgUnspecified is the zero value on kRecvUnspecified's terms: the predicate
// defaults to false, so a row that forgets its kinds fails at its first call.
// A row with an `object`-typed parameter would need a `kArgAny`; there is
// deliberately none today, an unused permissive kind being the one a hurried row
// would reach for.
enum : uint32_t
{
    kArgUnspecified = 0,
    kArgString = 1,   // System.String — a Dn2CppString, exactly
    kArgRefArray = 2, // a reference-element SZArray (the Dn2CppArrayRef layout)
};

// Operand-kind hints stamped by the converter (docs/BPI-FORMAT.md
// "Operand-kind hints"). Signedness is not part of the hint — the opcode
// itself (.un forms, conv.u*) carries it.
enum : uint32_t
{
    kKindI32 = 0,
    kKindI64 = 1,
    kKindF32 = 2,
    kKindF64 = 3,
    kKindRef = 4,
};

// ECMA-335 primitive codes as baked into Primitive-tagged EntityRefs by the
// converter (System.Reflection.Metadata.PrimitiveTypeCode values).
enum : uint32_t
{
    kPrimVoid = 0x01,
    kPrimBoolean = 0x02,
    kPrimChar = 0x03,
    kPrimInt32 = 0x08,
    kPrimInt64 = 0x0A,
    kPrimSingle = 0x0C,
    kPrimDouble = 0x0D,
    kPrimString = 0x0E,
};

// The interpreter's per-call boxed-argument buffer (the converter rejects
// wider external calls at bake time).
enum : uint32_t
{
    kMaxImportArgs = 8,
};

// Array element storage kinds baked into newarr/ldelem/stelem by the
// converter (docs/BPI-FORMAT.md "element storage kinds"): the element width,
// the signedness of sub-int32 loads, and the array representation (i4 = the
// int32 array layout, ref = the reference array layout, everything else the
// packed element-sized layout — the same representations the AOT lane uses).
enum : uint32_t
{
    kElemI1 = 0,
    kElemU1 = 1,
    kElemI2 = 2,
    kElemU2 = 3,
    kElemI4 = 4,
    kElemI8 = 5,
    kElemR4 = 6,
    kElemR8 = 7,
    kElemRef = 8,
};

// How one value crosses the invoker-thunk/accessor-thunk boundary: scalars box
// into `boxType` on the way in and unbox by payload width on the way out (the
// emitted C++ widens every sub-int32 scalar to an int32_t payload, so the slot
// kind alone fixes the width); references pass through as the pointer itself.
enum : uint8_t
{
    kMarshalNone = 0, // void (returns only)
    kMarshalI32,      // int32_t payload (Int32/Boolean/Char)
    kMarshalI64,
    kMarshalF32,
    kMarshalF64,
    kMarshalRef,      // object reference (string / AOT reference type)
};

struct MarshalDesc
{
    uint8_t kind;                  // kMarshal*
    const Dn2CppTypeInfo* boxType; // scalar box type-info (null for ref/none)
};

// A frame slot / eval-stack slot. Which member is live is decided by the
// converter's operand-kind hints (i32 values are kept sign-extended in `i`;
// f32 values are kept widened in `f` — exact, since every float32 is
// representable as a double and narrowing back is lossless). Patch
// static-field storage is Slot-shaped too, so ldsfld/stsfld on patch fields
// are plain slot copies preserving the invariants. The public mirror type
// (dn2cpp.h) is the same union, so N2M trampoline argument arrays cross
// dn2cpp_interp_vcall without conversion.
using Slot = Dn2CppInterpSlot;

struct ImportBinding
{
    uint32_t kind;      // DN2CPP_BPI_IMPORT_* (0 = unbound)
    uint32_t callShape; // kShape* for methods
    uint32_t argCount;  // parameter count, excluding this
    uint32_t recvKind;  // kRecv* — the declared receiver type the call arm tests
                        // before a kShapeRefRetObj call; carried from the
                        // intrinsic table row, the only source of that shape
    uint32_t argKinds[kMaxImportArgs]; // kArg* per reference parameter, tested by the
                        // three reference-argument shapes' call arms; carried
                        // from the same row, and unspecified (hence refused) elsewhere
    bool isInstance;    // a this slot precedes the args
    bool isCtor;        // a newobj target (bound from the type's ctors table)
    bool isInterface;   // an interface-typed callvirt: dispatch resolves the fn
                        // from the receiver's interface map (type = the interface,
                        // vtableSlot = the interface method slot)
    int32_t vtableSlot; // >= 0: virtual — callvirt dispatches the receiver's vtable
    void* fnPtr;
    void* invoker;
    const Dn2CppTypeInfo* type; // Type import: the resolved base type-info (may
                                // be null — a declaring-type anchor an intrinsic
                                // import binds by name needs no type-info).
                                // Method import: the declaring type-info (the
                                // allocation source for a ctor).
    const Dn2CppTypeInfo* retType;      // the invoker thunk's retType argument; also
                                        // the declared RETURN type the delegate-capture
                                        // signature test reads — set by both bind
                                        // routes, from the resolved method row or from
                                        // the intrinsic row's own retTi
    const Dn2CppMethodInfo* method;     // the base-image method row a METADATA bind
                                        // resolved (null for an intrinsic-table bind,
                                        // which resolves no row). Carries the parameter
                                        // types the delegate-capture signature test
                                        // compares position by position; the MarshalDesc
                                        // args below cannot serve, a reference parameter
                                        // marshalling as a bare kMarshalRef with no
                                        // type-info of its own.
    MarshalDesc ret;                    // result marshalling (kMarshalNone = void)
    MarshalDesc args[kMaxImportArgs];   // per-parameter marshalling
    const Dn2CppFieldInfo* field;       // Field import: the bound accessor entry
    MarshalDesc fieldVal;               // Field import: value marshalling
    int8_t excMsgArg;   // exception-ctor intercept: the Message ctor-arg index or -1
    int8_t excInnerArg; // exception-ctor intercept: the innerException arg index or -1
};

} // namespace

struct Dn2CppInterpImage
{
    const uint8_t* blob;
    size_t len;
    const uint8_t* strings;
    uint32_t stringsLen;
    const Dn2CppBpiImport* imports;
    uint32_t importCount;
    const Dn2CppBpiType* types;
    uint32_t typeCount;
    const Dn2CppBpiField* fields;
    uint32_t fieldCount;
    const Dn2CppBpiMethod* methods;
    uint32_t methodCount;
    const Dn2CppBpiInsn* code;
    uint32_t insnCount;
    const Dn2CppBpiEH* eh;
    uint32_t ehCount;
    const uint32_t* localSigs;
    uint32_t localSigCount;
    const uint8_t* userStrings;
    uint32_t userStringsLen;
    const uint8_t* vtdesc; // VtableDescTable byte arena (vtable-slot overrides)
    uint32_t vtdescLen;
    const uint8_t* itfdesc; // ItfDescTable byte arena (interface-slot implementations)
    uint32_t itfdescLen;
    ImportBinding* bindings;
    Slot* statics;         // patch static-field storage (GC-scanned; one Slot per staticSlot)
    uint32_t staticCount;
    uint8_t* cctorStarted; // per patch type: kCctorUntouched / kCctorStarted (set
                           // before its .cctor first runs, latched on success) /
                           // kCctorFailed (the initializer threw; the type stays
                           // uninitialized and every touch replays cctorFailed[i])
    Dn2CppObject** cctorFailed; // per patch type: the managed exception a failed
                                // .cctor threw (null under kCctorFailed = a
                                // non-managed failure). dn2cpp_alloc'd — GC-scanned
                                // and reached through g_loadedImages — so the
                                // recorded exception outlives the throw.
    // Load-time patch-type construction: one real Dn2CppTypeInfo per TypeTable
    // record, plus the loader-computed instance layout — per FieldTable index,
    // the field's byte offset from the object start and its storage kind
    // (kMarshal*; both 0 for statics). Numeric layout lives only here, derived
    // fresh per load from the live base type-infos.
    const Dn2CppTypeInfo** patchTypes;
    uint32_t* instFieldOffsets;
    uint8_t* instFieldKinds;
    const char* const* frameNames; // per-MethodTable-index materialized "DeclType.Method()"
                                   // shadow-stack frame name; null unless the base image was
                                   // built with --shadow-stack (the image is dn2cpp_alloc'd —
                                   // zero-initialized — so flag-off loads read null for free)
    uint32_t entryMethodIdx;
    bool regCode; // Header.flags bit0: CodeSection records carry the register
                  // encoding (dn2cpp_interp_regops.h) — dispatched by
                  // interp_run_reg, verified up front by verify_reg_code
    Dn2CppInterpImage* nextLoaded;
};

namespace
{

// The append-only list of loaded images. A static pointer lives in the data
// segment, which the Boehm collector scans as a root set, so everything an
// image references (its blob, its bindings) stays alive.
DN2CPP_GC_STATIC_ROOT Dn2CppInterpImage* g_loadedImages = nullptr;

// Per-type lazy-cctor guard states (Dn2CppInterpImage::cctorStarted).
enum : uint8_t { kCctorUntouched = 0, kCctorStarted = 1, kCctorFailed = 2 };

[[noreturn]] void interp_fail(const char* msg)
{
    dn2cpp_throw(dn2cpp_exception_new(&dn2cpp_not_supported_exception_type,
        dn2cpp_string_from_utf8(msg, static_cast<int32_t>(std::strlen(msg))), nullptr));
}

// A loader-constructed patch type-info, extended with what an N2M trampoline
// needs to route a vtable-slot call back into the interpreter. The
// Dn2CppTypeInfo member comes FIRST, so the AOT side sees an ordinary type-info;
// dn2cpp_interp_vcall downcasts the receiver's type pointer back to this. The
// downcast is sound by construction (trampolines are installed only into the
// vtable copies of loader-constructed patch types) and the magic stamp turns a
// violated invariant into a diagnosable failure instead of a wild read.
enum : uint32_t
{
    kInterpTypeInfoMagic = 0x31495449u, // "ITI1"
};

struct InterpTypeInfo
{
    Dn2CppTypeInfo ti;
    const Dn2CppInterpImage* img;
    uint32_t typeIdx;
    uint32_t magic;
};

// A loader-built delegate closure — what a patch method bound into a base-image
// delegate carries as the AOT delegate's `target`. `ldftn` builds it; the
// delegate `newobj` captures the receiver into it (null for a static method) and
// installs the delegate thunk as the delegate's `method`, so an Invoke from
// either side lands on the thunk, which hands the closure back to
// dn2cpp_interp_dgcall. The Aot kind is a transient carrier between `ldftn` and
// the delegate `newobj` for a base-image method reference (its aotFn becomes the
// delegate's `method` directly). The Dn2CppObject header comes first so the AOT
// side sees an ordinary object; the magic stamp turns a violated invariant into a
// diagnosable failure. GC-scanned: `boundThis` and `img` must stay
// collector-visible.
enum : uint32_t
{
    kDgFtnMagic = 0x4E544644u, // "DFTN"
    kDgFtnPatch = 1,           // a baked patch method (img + methodIdx [+ boundThis])
    kDgFtnAot = 2,             // a base-image method (aotFn) — the natural AOT delegate
};

struct Dn2CppInterpDgFtn : Dn2CppObject
{
    uint32_t kind;
    uint32_t methodIdx;           // kDgFtnPatch: the patch MethodTable index
    const Dn2CppInterpImage* img; // kDgFtnPatch: the owning image
    void* aotFn;                  // kDgFtnAot: the base-image method pointer
    Dn2CppObject* boundThis;      // kDgFtnPatch: the captured receiver (null = static)
    // kDgFtnAot: what the bound import declared its receiver to be, carried from
    // `ldftn` to the delegate `newobj` because that is where the receiver first
    // exists. Exactly one of the two is set — an intrinsic-table row has a kRecv*
    // kind and no type-info, a metadata-bound one has the declaring type-info and
    // kRecvUnspecified — so dg_aot_target_ok asks the question the row it came
    // from can answer. Both are zero for kDgFtnPatch.
    uint32_t aotRecvKind;
    const Dn2CppTypeInfo* aotDeclType;
    // kDgFtnAot: what the bound import declared its SIGNATURE to be, carried from
    // `ldftn` for the same reason — the delegate type first exists at the
    // `newobj`. `aotMethod` is the resolved base-image method row for a metadata
    // bind and null for an intrinsic-table one, whose fixed `Dn2CppObject*
    // (*)(Dn2CppObject*)` shape needs none: aotArgCount is 0 and aotRetType is the
    // row's declared return. All three are zero for kDgFtnPatch.
    const Dn2CppMethodInfo* aotMethod;
    const Dn2CppTypeInfo* aotRetType;
    uint32_t aotArgCount;
    uint32_t magic;
};

// One decoded VtableDescTable entry run: `{ u32 vtableLen; u32 overrideCount;
// { u32 slot; u32 patchMethodIdx }[overrideCount] }` at a TypeRecord's
// vtableDescOff. Returns the bounds-checked entry pointer.
const uint32_t* vtdesc_at(const Dn2CppInterpImage* img, uint32_t off,
    uint32_t* vtableLenOut, uint32_t* overrideCountOut)
{
    if (img->vtdesc == nullptr || off % 4 != 0
        || static_cast<uint64_t>(off) + 8 > img->vtdescLen)
        interp_fail("BPI: vtable description out of bounds");
    const uint32_t* d = reinterpret_cast<const uint32_t*>(img->vtdesc + off);
    uint32_t vtableLen = d[0];
    uint32_t overrideCount = d[1];
    if (static_cast<uint64_t>(off) + 8 + static_cast<uint64_t>(overrideCount) * 8 > img->vtdescLen)
        interp_fail("BPI: vtable description out of bounds");
    *vtableLenOut = vtableLen;
    *overrideCountOut = overrideCount;
    return d + 2;
}

// StringPool entry at `off`: { u16 len; u8 utf8[len] }.
//
// `off` is untrusted image data, so the bounds arithmetic widens to uint64_t and
// the section pointer is tested (as in vtdesc_at / itfdesc_at): in uint32_t
// `off + 2 > stringsLen` wraps at 0xFFFFFFFE, and an image with no STRINGPOOL
// section has a null `strings` with stringsLen 0, which the wrapped compare also
// lets through.
const char* pool_name(const Dn2CppInterpImage* img, uint32_t off, uint32_t* lenOut)
{
    if (img->strings == nullptr || static_cast<uint64_t>(off) + 2 > img->stringsLen)
        interp_fail("BPI: string offset out of bounds");
    uint16_t n;
    std::memcpy(&n, img->strings + off, 2);
    if (static_cast<uint64_t>(off) + 2 + n > img->stringsLen)
        interp_fail("BPI: string entry out of bounds");
    *lenOut = n;
    return reinterpret_cast<const char*>(img->strings + off + 2);
}

// `literal` is not always a C++ literal: the overload resolver and the field and
// trampoline binds pass a base-image Dn2CppMethodInfo/Dn2CppFieldInfo name, and
// a type-info's name can be null. A null name matches nothing rather than
// reaching strlen.
bool name_equals(const char* name, uint32_t len, const char* literal)
{
    if (name == nullptr || literal == nullptr)
        return false;
    return std::strlen(literal) == len && std::memcmp(name, literal, len) == 0;
}

// The intrinsic import table: base-image surfaces that exist as runtime
// intrinsics rather than emitted Dn2CppTypeInfo methods (System.Console has no
// type-info to walk; System.Exception members map to the uniform exception
// object's accessors, exactly like the AOT emit intercept), keyed by declaring
// type + name + staticness + sigShape.
struct IntrinsicImport
{
    const char* type;
    const char* method;
    const char* sigShape;
    void* fnPtr;
    uint32_t callShape;
    uint32_t argCount;
    bool isInstance;
    // kRecv* — required on a kShapeRefRetObj row, left at the aggregate-init zero
    // on every other shape. The call arm refuses kRecvUnspecified, so a row that
    // forgets its kind fails at its first call rather than accepting anything.
    uint32_t recvKind;
    // kArg* per reference parameter, in parameter order — required on a
    // kShapeVoidStr / kShapeRefRetStr / kShapeRefRetRefN row. One entry per
    // position rather than one kind for the row: nothing about a shape makes its
    // parameters homogeneous, and kShapeRefRetStr's own two rows are not.
    uint32_t argKinds[kMaxImportArgs];
    // The row's declared RETURN type-info — required on a kShapeRefRetObj row (the
    // one shape `ldftn` may capture out of this table). The delegate-capture
    // signature test compares it against the delegate's Invoke return, an
    // intrinsic bind resolving no Dn2CppMethodInfo to read it off. A null answers
    // false there, on kRecvUnspecified's terms.
    const Dn2CppTypeInfo* retTi;
};

void intrinsic_console_writeline_str(Dn2CppString* s) { dn2cpp_console_writeline_str(s); }
void intrinsic_console_writeline_empty() { dn2cpp_console_writeline_empty(); }
void intrinsic_console_writeline_i4(int32_t v) { dn2cpp_console_writeline_i4(v); }
void intrinsic_console_writeline_i8(int64_t v) { dn2cpp_console_writeline_i8(v); }
void intrinsic_console_writeline_r8(double v) { dn2cpp_console_writeline_r8(v); }
void intrinsic_console_writeline_bool(int32_t v) { dn2cpp_console_writeline_bool(v); }
// The kShapeRefRetObj/kShapeRefRetStr intrinsics share one exact C++ signature
// (references in, reference out), so the call arm's function-pointer cast is
// always to the type the wrapper was defined with.
Dn2CppObject* intrinsic_exception_get_message(Dn2CppObject* e)
{
    return reinterpret_cast<Dn2CppObject*>(dn2cpp_exception_message(e));
}
// The instance wrappers below guard their own receiver even though the dispatch
// loops' intrinsic call arms test it too: `ldftn` hands a wrapper's raw fnPtr to
// the delegate `newobj` as the delegate's f_method, so a delegate Invoke reaches
// the wrapper without ever crossing a call arm. The construction-side guards
// (null-'this' plus dg_aot_target_ok) cover that route today; this layer turns a
// regression in either into a catchable NullReferenceException instead of a
// segfault at o->type. Wrappers whose runtime helper already null-checks its
// receiver (exception_get_message, memberinfo_get_name) need no guard here.
Dn2CppObject* intrinsic_object_get_type(Dn2CppObject* o)
{
    if (o == nullptr)
        dn2cpp_throw_null_reference();
    return reinterpret_cast<Dn2CppObject*>(dn2cpp_get_type_from_handle(o->type));
}
Dn2CppObject* intrinsic_object_tostring(Dn2CppObject* o)
{
    // The dispatch entry point, NOT the null-folding dn2cpp_object_tostring:
    // this is the interpreter's `x.ToString()` dispatch mouth, so a null receiver
    // must throw here as the AOT mouths do (invariant stated at the formatter's
    // definition in dn2cpp_runtime.cpp). The folding entry point serves
    // concatenation only, reached through the dn2cpp_string_concat* helpers.
    return reinterpret_cast<Dn2CppObject*>(dn2cpp_object_tostring_virtual(o));
}
Dn2CppObject* intrinsic_type_get_fullname(Dn2CppObject* t)
{
    if (t == nullptr)
        dn2cpp_throw_null_reference();
    return reinterpret_cast<Dn2CppObject*>(
        dn2cpp_type_fullname(reinterpret_cast<Dn2CppType*>(t)->typeInfo));
}
Dn2CppObject* intrinsic_type_get_name(Dn2CppObject* t)
{
    if (t == nullptr)
        dn2cpp_throw_null_reference();
    return reinterpret_cast<Dn2CppObject*>(
        dn2cpp_type_name(reinterpret_cast<Dn2CppType*>(t)->typeInfo));
}
Dn2CppObject* intrinsic_memberinfo_get_name(Dn2CppObject* m)
{
    // Type.Name resolves as MemberInfo::get_Name (where .NET declares it);
    // the runtime helper dispatches on the receiver's object header.
    return reinterpret_cast<Dn2CppObject*>(dn2cpp_memberinfo_name(m));
}
Dn2CppObject* intrinsic_type_get_type(Dn2CppString* name)
{
    return reinterpret_cast<Dn2CppObject*>(dn2cpp_type_get_by_name(name, 0));
}
// String.Concat, string-only overloads — what Roslyn lowers `s + t` and
// string-holed interpolation to. The runtime concat helpers skip null parts,
// matching the AOT call-site lowering (and .NET, which treats null as empty).
Dn2CppObject* intrinsic_string_concat2(Dn2CppObject* const* a)
{
    return reinterpret_cast<Dn2CppObject*>(dn2cpp_string_concat2(
        reinterpret_cast<Dn2CppString*>(a[0]), reinterpret_cast<Dn2CppString*>(a[1])));
}
Dn2CppObject* intrinsic_string_concat3(Dn2CppObject* const* a)
{
    return reinterpret_cast<Dn2CppObject*>(dn2cpp_string_concat3(
        reinterpret_cast<Dn2CppString*>(a[0]), reinterpret_cast<Dn2CppString*>(a[1]),
        reinterpret_cast<Dn2CppString*>(a[2])));
}
Dn2CppObject* intrinsic_string_concat4(Dn2CppObject* const* a)
{
    return reinterpret_cast<Dn2CppObject*>(dn2cpp_string_concat4(
        reinterpret_cast<Dn2CppString*>(a[0]), reinterpret_cast<Dn2CppString*>(a[1]),
        reinterpret_cast<Dn2CppString*>(a[2]), reinterpret_cast<Dn2CppString*>(a[3])));
}
// String.Concat(string[]) — the params fallback for 5+ operands. Elements go
// through the same ToString-formatting helper the AOT lowering uses (a string
// element passes through unchanged, nulls contribute nothing).
Dn2CppObject* intrinsic_string_concat_array(Dn2CppString* arr)
{
    return reinterpret_cast<Dn2CppObject*>(
        dn2cpp_string_concat_objects(reinterpret_cast<Dn2CppArrayRef*>(arr)));
}

// Delegate construction over a null 'this': message and type match real .NET,
// whose CtorClosed raises ArgumentException at CONSTRUCTION rather than a
// NullReferenceException at invoke. Construction is also the interpreter's only
// interceptable point for the kDgFtnAot route — the delegate's f_method is a raw
// AOT pointer, so a later Invoke hands the null receiver to code the dispatch
// loops never see again. Raised from inside a dispatch loop's own try, so it is
// scanned against the frame's EH records like an interpreted `throw`.
[[noreturn]] void throw_delegate_null_this()
{
    const char* msg = "Delegate to an instance method cannot have null 'this'.";
    dn2cpp_throw(dn2cpp_exception_new(&dn2cpp_argument_exception_type,
        dn2cpp_string_from_utf8(msg, static_cast<int32_t>(std::strlen(msg))), nullptr));
}

// The patch-target half of that refusal. Dn2CppBpiMethod carries no static bit
// (dn2cpp_interp.h), so instance-ness is DERIVED by arity, exactly as
// dn2cpp_interp_dgcall derives it at invoke time: an instance patch method's
// argCount includes `this`, so it is the delegate's Invoke parameter count plus
// one. The evidence is the delegate type's own method table, which
// --trim-reflection never strips (AGENTS.md).
//
// A table that cannot answer is NOT evidence of a static method: an absent or
// nameless Invoke row DEGRADES — the null rides into the interpreted body, whose
// receiver guards raise the catchable NRE — rather than refusing a legitimate
// static-method closure, whose null captured receiver is the format's own
// encoding of "static".
const Dn2CppMethodInfo* dg_invoke_row(const Dn2CppTypeInfo* dti)
{
    if (dti == nullptr || dti->methods == nullptr)
        return nullptr;
    for (int32_t i = 0; i < dti->methodCount; i++)
        if (dti->methods[i].name != nullptr
            && std::strcmp(dti->methods[i].name, "Invoke") == 0)
            return &dti->methods[i];
    return nullptr;
}

int32_t dg_invoke_param_count(const Dn2CppTypeInfo* dti)
{
    const Dn2CppMethodInfo* inv = dg_invoke_row(dti);
    return inv != nullptr ? inv->paramCount : -1;
}

void check_patch_delegate_null_this(const Dn2CppInterpImage* img, uint32_t methodIdx,
    const Dn2CppTypeInfo* dti, const Dn2CppObject* target)
{
    if (target != nullptr || img == nullptr || methodIdx >= img->methodCount)
        return;
    int32_t invokeParams = dg_invoke_param_count(dti);
    if (invokeParams < 0)
        return; // the degrade — see above
    if (img->methods[methodIdx].argCount == static_cast<uint32_t>(invokeParams) + 1u)
        throw_delegate_null_this();
}

const IntrinsicImport g_intrinsicImports[] = {
    { "System.Console", "WriteLine", "(String):Void",
      reinterpret_cast<void*>(&intrinsic_console_writeline_str), kShapeVoidStr, 1, false,
      kRecvUnspecified, { kArgString } },
    { "System.Console", "WriteLine", "():Void",
      reinterpret_cast<void*>(&intrinsic_console_writeline_empty), kShapeVoidEmpty, 0, false },
    { "System.Console", "WriteLine", "(Int32):Void",
      reinterpret_cast<void*>(&intrinsic_console_writeline_i4), kShapeVoidI4, 1, false },
    { "System.Console", "WriteLine", "(Int64):Void",
      reinterpret_cast<void*>(&intrinsic_console_writeline_i8), kShapeVoidI8, 1, false },
    { "System.Console", "WriteLine", "(Double):Void",
      reinterpret_cast<void*>(&intrinsic_console_writeline_r8), kShapeVoidR8, 1, false },
    { "System.Console", "WriteLine", "(Boolean):Void",
      reinterpret_cast<void*>(&intrinsic_console_writeline_bool), kShapeVoidBool, 1, false },
    { "System.Exception", "get_Message", "():String",
      reinterpret_cast<void*>(&intrinsic_exception_get_message), kShapeRefRetObj, 0, true,
      kRecvException, {}, &dn2cpp_string_type },
    // The object/Type reflection-lite surface behind patch-type registry
    // visibility: GetType() reads the live object header (a patch instance
    // reports its loader-constructed type-info), the Type getters unwrap the
    // handle, ToString is the runtime's uniform dispatcher (an overridden
    // ToString routes through the type-info's dedicated entry; the default
    // prints the type's full name), and Type.GetType(string) resolves against
    // the registry including its dynamic side-chain.
    { "System.Object", "GetType", "():System.Type",
      reinterpret_cast<void*>(&intrinsic_object_get_type), kShapeRefRetObj, 0, true,
      kRecvAny, {}, &dn2cpp_type_type },
    { "System.Object", "ToString", "():String",
      reinterpret_cast<void*>(&intrinsic_object_tostring), kShapeRefRetObj, 0, true,
      kRecvAny, {}, &dn2cpp_string_type },
    { "System.Type", "get_FullName", "():String",
      reinterpret_cast<void*>(&intrinsic_type_get_fullname), kShapeRefRetObj, 0, true,
      kRecvType, {}, &dn2cpp_string_type },
    { "System.Type", "get_Name", "():String",
      reinterpret_cast<void*>(&intrinsic_type_get_name), kShapeRefRetObj, 0, true,
      kRecvType, {}, &dn2cpp_string_type },
    { "System.Reflection.MemberInfo", "get_Name", "():String",
      reinterpret_cast<void*>(&intrinsic_memberinfo_get_name), kShapeRefRetObj, 0, true,
      kRecvMemberInfo, {}, &dn2cpp_string_type },
    { "System.Type", "GetType", "(String):System.Type",
      reinterpret_cast<void*>(&intrinsic_type_get_type), kShapeRefRetStr, 1, false,
      kRecvUnspecified, { kArgString } },
    // The string-building surface: the String.Concat overloads Roslyn emits
    // for string-only `+` chains and string-holed interpolation (2..4 operands
    // hit the typed overloads, 5+ the params string[] fallback). The
    // Concat(Object, ...) shapes (mixed/value operands) and
    // DefaultInterpolatedStringHandler (value-typed interpolation holes) stay
    // outside the surface — see docs/BPI-FORMAT.md §Deployment.
    { "System.String", "Concat", "(String,String):String",
      reinterpret_cast<void*>(&intrinsic_string_concat2), kShapeRefRetRefN, 2, false,
      kRecvUnspecified, { kArgString, kArgString } },
    { "System.String", "Concat", "(String,String,String):String",
      reinterpret_cast<void*>(&intrinsic_string_concat3), kShapeRefRetRefN, 3, false,
      kRecvUnspecified, { kArgString, kArgString, kArgString } },
    { "System.String", "Concat", "(String,String,String,String):String",
      reinterpret_cast<void*>(&intrinsic_string_concat4), kShapeRefRetRefN, 4, false,
      kRecvUnspecified, { kArgString, kArgString, kArgString, kArgString } },
    // The one row of the reference-argument shapes whose operand is NOT a string,
    // and the reason the kinds are per row: it shares kShapeRefRetStr's C++
    // signature with Type.GetType above and reinterpret_casts its `Dn2CppString*`
    // parameter straight to a Dn2CppArrayRef.
    { "System.String", "Concat", "(String[]):String",
      reinterpret_cast<void*>(&intrinsic_string_concat_array), kShapeRefRetStr, 1, false,
      kRecvUnspecified, { kArgRefArray } },
};

const Dn2CppTypeInfo* find_registry_type(const char* name, uint32_t len)
{
    // The shared lookup covers the static table plus the dynamic side-chain,
    // so a later patch's binds see the types earlier loads registered.
    return dn2cpp_type_registry_find(name, static_cast<int32_t>(len));
}

// Whether the type's base chain reaches System.Exception — the same predicate
// the AOT emit path uses to intercept exception-type newobj into
// dn2cpp_exception_new (registry-bound exception types chain to the shared
// runtime handle; emitted user exception types chain into it by name).
bool type_is_exception(const Dn2CppTypeInfo* ti)
{
    for (const Dn2CppTypeInfo* t = ti; t != nullptr; t = t->base)
    {
        // A null name is possible on a chain node (the bindIntrinsic walk tests
        // for it before its own strcmp) — it is simply not System.Exception.
        if (t == &dn2cpp_exception_type
            || (t->name != nullptr && std::strcmp(t->name, "System.Exception") == 0))
            return true;
    }
    return false;
}

// Whether `self` satisfies the declared receiver type of the kShapeRefRetObj row
// an import bound to. Header identity, not a name walk: &dn2cpp_type_type is the
// header every interned Dn2CppType carries, and the MemberInfo set is exactly
// the handle headers dn2cpp_memberinfo_name discriminates — Type included,
// because that helper's FALL-THROUGH is the Dn2CppType* cast, so "not one of the
// other three" would be the confusion rather than the guard. `self` is non-null
// here (the arms null-check first); a null `self->type` answers false.
bool intrinsic_receiver_ok(uint32_t recvKind, const Dn2CppObject* self)
{
    switch (recvKind)
    {
        case kRecvAny:
            return true;
        case kRecvException:
            return type_is_exception(self->type);
        case kRecvType:
            return self->type == &dn2cpp_type_type;
        case kRecvMemberInfo:
            return self->type == &dn2cpp_type_type || self->type == &dn2cpp_fieldinfo_type
                || self->type == &dn2cpp_methodinfo_type
                || self->type == &dn2cpp_constructorinfo_type
                || self->type == &dn2cpp_propertyinfo_type;
        default:
            // kRecvUnspecified, and anything a future row leaves unset.
            return false;
    }
}

// The same question for one reference ARGUMENT of a kShapeVoidStr /
// kShapeRefRetStr / kShapeRefRetRefN row. Header identity again, and
// dn2cpp_is_ref_array is the shared rule dn2cpp_pinned_data_addr picks its data
// offset with, so the guard and the layout it protects cannot drift apart.
//
// A NULL argument passes every kind: `Console.WriteLine((string)null)`,
// `String.Concat(null, s)` and `String.Concat((string[])null)` are all
// legitimate, and each helper handles null itself. The null test sits INSIDE
// each case, not ahead of the switch, so it cannot launder kArgUnspecified.
bool intrinsic_arg_ok(uint32_t argKind, const Dn2CppObject* a)
{
    switch (argKind)
    {
        case kArgString:
            return a == nullptr || a->type == &dn2cpp_string_type;
        case kArgRefArray:
            return a == nullptr || dn2cpp_is_ref_array(a->type);
        default:
            // kArgUnspecified, and anything a future row leaves unset.
            return false;
    }
}

// One constant for six call arms: three reference-argument shapes across both
// dispatch loops.
const char* const kIntrinsicArgFail
    = "interp: intrinsic call argument is not an instance of the import's declared parameter type";

// The receiver question for the three shapes the intrinsic table does NOT
// produce — kShapeInvoker, kShapeExceptionNew, kShapeDelegateInvoke. These are
// bound from the DECLARING TYPE's own metadata, so the binding already holds the
// answer (`b.type`) and no kRecv* kind is wanted. What each does with an
// unchecked receiver:
//
//   kShapeInvoker       a `callvirt` reads self->type->vtable[b.vtableSlot] at a
//                       slot index from the DECLARED type: on another type, an
//                       out-of-range read then a call through whatever it held.
//   kShapeExceptionNew  exc_seed_and_run_base_ctor writes message/inner/hresult
//                       at the uniform Dn2CppExceptionObject prefix — on a
//                       non-exception, an out-of-range WRITE.
//   kShapeDelegateInvoke the M2N bridge reads prev/method/target off the receiver
//                       as a Dn2CppDelegate and CALLS `method`.
//
// The rule is dn2cpp_typeinfo_assignable — the one rule `isinst` runs, i.e. the
// rule the C# receiver actually satisfied at bake time, already knowing about
// interfaces, variance and arrays. Asking it for the INTERFACE arm too is not
// redundant with dn2cpp_resolve_interface's own miss report: that one is
// dn2cpp_fail, an abort, where every other malformed-image refusal here is
// interp_fail's catchable NotSupportedException.
//
// A null `b.type` answers false — the fail-closed direction, for a bind mouth
// that forgot to set it.
bool receiver_is_a(const Dn2CppTypeInfo* declTi, const Dn2CppObject* self)
{
    return declTi != nullptr && self != nullptr && self->type != nullptr
        && dn2cpp_typeinfo_assignable(self->type, declTi) != 0;
}

bool import_receiver_ok(const ImportBinding& b, const Dn2CppObject* self)
{
    return receiver_is_a(b.type, self);
}

// One constant for eight arms: three shapes across both dispatch loops, plus the
// AOT-delegate capture at both `newobj` arms.
const char* const kImportRecvFail
    = "interp: call receiver is not an instance of the import's declared type";

// `ldftn` is a SECOND mouth onto every bound fnPtr: an import's function pointer
// becomes an AOT delegate's `method` and is called later with the captured
// `this`, never crossing a dispatch loop's call arm — so every receiver test
// those arms perform is re-openable here. Both `ldftn` arms go through this one
// transcription, so a field a later capture-time test reads cannot reach half
// the closures.
//
// The call-shape gate is not decoration: an AOT delegate's `method` is invoked
// through the delegate's OWN Invoke ABI, and only these two shapes hold a plain
// instance-method pointer. kShapeDelegateInvoke's fnPtr is an M2N bridge
// (Dn2CppInterpSlot (*)(Dn2CppObject*, const Dn2CppInterpSlot*, int32_t)) and
// kShapeExceptionNew's is a constructor — both pass `b.isInstance && b.fnPtr`
// and neither is callable through an Invoke thunk, which is a signature
// mismatch no receiver test would ever notice.
void dg_ftn_bind_aot(Dn2CppInterpDgFtn* c, const ImportBinding& b)
{
    if (!b.isInstance || b.fnPtr == nullptr)
        interp_fail("interp: a delegate over a base-image method needs a bound instance method (static AOT targets need an adapter)");
    if (b.callShape != kShapeInvoker && b.callShape != kShapeRefRetObj)
        interp_fail("interp: a delegate over a base-image method needs a plain instance method (this import's call shape carries no such pointer)");
    c->kind = kDgFtnAot;
    c->aotFn = b.fnPtr;
    c->aotRecvKind = b.recvKind;
    c->aotDeclType = b.type;
    // And the SIGNATURE, for the same reason and by the same route.
    c->aotMethod = b.method;
    c->aotRetType = b.retType;
    c->aotArgCount = b.argCount;
}

// The capture-time test both delegate `newobj` arms run, asked of whichever of
// the two records the binding left (see Dn2CppInterpDgFtn). Construction is the
// only interceptable point: an Invoke re-enters through the delegate's own
// thunk, which knows nothing about the import the pointer came from — the same
// reason the null-'this' refusal sits here.
bool dg_aot_target_ok(const Dn2CppInterpDgFtn* c, const Dn2CppObject* target)
{
    if (target == nullptr)
        return false; // the arms refuse null first (throw_delegate_null_this)
    if (c->aotRecvKind != kRecvUnspecified)
        return intrinsic_receiver_ok(c->aotRecvKind, target);
    return receiver_is_a(c->aotDeclType, target);
}

// The capture's other question: the rest of the call, past the receiver. An AOT
// delegate's `method` is the bound import's raw function pointer, invoked through
// the delegate's OWN Invoke C++ ABI (`dginvoke_<T>` at an AOT call site,
// `dn2cpp_dgbridge_<T>` from an interpreted one), so a `method` whose real
// signature is not that ABI is called with the wrong argument registers — and a
// reference parameter the caller never set is a pointer forged out of whatever
// was live. dg_ftn_bind_aot's call-shape gate rules out the pointers that are not
// plain instance methods; this rules out the plain instance method whose shape is
// simply a different one.
//
// The comparison material is live metadata, not the contract's `D` line, which is
// a hash input only (docs/BPI-FORMAT.md §Delegate thunks): the delegate
// type-info's own `Invoke` row and the base-image method row a metadata bind
// resolved.
//
// One position of the two signatures, `from` what the caller supplies and `to`
// what the callee declares. Identity settles the common case and both voids. A
// value type must be EXACT — its C++ representation is its own, so
// int32-for-int64 or float-for-pointer is the ABI break under test. Two reference
// types go to dn2cpp_typeinfo_assignable, the one rule `isinst` runs, because a
// method group conversion is legal under variance and every reference is one
// pointer, so variance costs the ABI nothing.
bool dg_sig_position_ok(const Dn2CppTypeInfo* from, const Dn2CppTypeInfo* to)
{
    if (from == to)
        return true;
    if (from == nullptr || to == nullptr)
        return false; // one side is void and the other is not
    if ((from->flags & DN2CPP_TF_VALUETYPE) != 0 || (to->flags & DN2CPP_TF_VALUETYPE) != 0)
        return false;
    return dn2cpp_typeinfo_assignable(from, to) != 0;
}

// The whole test, asked at both delegate `newobj` arms of a kDgFtnAot closure.
//
// An ABSENT Invoke row is a REFUSAL here, where the null-'this' test degrades on
// the same absence — the asymmetry is deliberate. That one asks "is this patch
// method static?", whose unknown answer has a legitimate reading, so refusing
// would break correct images. This asks "is this pointer callable through that
// ABI?", where the unknown answer has no legitimate reading at all: a delegate
// with no Invoke row cannot be invoked by anything, and admitting the capture
// only moves the failure to a raw indirect call. The row is there in practice
// because a delegate's method table is never stripped (AGENTS.md).
//
// The intrinsic-table route (aotMethod null) carries no row and needs none: the
// one capturable shape out of that table is kShapeRefRetObj, whose C++ signature
// is exactly `Dn2CppObject* (*)(Dn2CppObject*)` — no parameters past the
// receiver, one reference return — so aotArgCount and the row's declared retTi
// state it completely.
bool dg_aot_signature_ok(const Dn2CppInterpDgFtn* c, const Dn2CppTypeInfo* dti)
{
    const Dn2CppMethodInfo* inv = dg_invoke_row(dti);
    if (inv == nullptr)
        return false;
    if (static_cast<uint32_t>(inv->paramCount) != c->aotArgCount)
        return false;
    // Covariant return, contravariant parameters — the directions a method group
    // conversion is allowed to move in, asked position by position.
    if (!dg_sig_position_ok(c->aotRetType, inv->returnType))
        return false;
    if (c->aotMethod == nullptr)
        return true; // the intrinsic shape; argCount 0, so there are no positions left
    if (c->aotMethod->paramCount != inv->paramCount)
        return false;
    for (int32_t i = 0; i < inv->paramCount; i++)
    {
        if (inv->parameters == nullptr || c->aotMethod->parameters == nullptr)
            return false;
        if (!dg_sig_position_ok(inv->parameters[i].paramType,
                c->aotMethod->parameters[i].paramType))
            return false;
    }
    return true;
}

// One constant for the two `newobj` arms, as kImportRecvFail is for the eight.
const char* const kDgSigFail
    = "interp: the delegate target's signature is not the delegate's Invoke signature";

// The patch-side receiver test. A patch method's receiver is arg0 of its frame,
// and what follows a patch call is `ldfld`/`stfld` at a LOADER-ASSIGNED offset of
// the DECLARING type — an offset the receiver does not bound, so on a shorter
// object it is an out-of-range write, not a misread. (dn2cpp_interp_vcall and
// dn2cpp_interp_itfcall already state the receiver's type by re-resolving the
// target down its own patch chain; the non-virtual `call` arm and
// dn2cpp_interp_dgcall need this.)
//
// The rule is receiver_is_a against the declaring patch type-info the loader
// built: strictly stronger than the magic test the virtual mouths run, and it
// needs no magic of its own. A well-formed image pays one pointer compare —
// dn2cpp_typeinfo_assignable answers `st == ti` before probing its cache, and the
// receiver of a patch call is its declaring type in every shape but a base-class
// call from a derived patch type.
bool patch_receiver_ok(const Dn2CppInterpImage* img, uint32_t methodIdx,
    const Dn2CppObject* self)
{
    uint32_t t = img->methods[methodIdx].declTypeIdx;
    if (t >= img->typeCount || img->patchTypes == nullptr || img->patchTypes[t] == nullptr)
        return false;
    return receiver_is_a(img->patchTypes[t], self);
}

const char* const kPatchRecvFail
    = "interp: call receiver is not an instance of the patch method's declaring type";

// Decodes one signature EntityRef (a Primitive-tagged code or an Import-tagged
// type import) into the marshalling the invoker/accessor boundary needs.
// 0xFFFFFFFF (none) is a void return.
MarshalDesc marshal_from_ref(const Dn2CppInterpImage* img, uint32_t ref)
{
    if (ref == DN2CPP_BPI_REF_NONE)
        return { kMarshalNone, nullptr };
    switch (DN2CPP_BPI_REF_TAG(ref))
    {
        case DN2CPP_BPI_TAG_PRIM:
            switch (DN2CPP_BPI_REF_INDEX(ref))
            {
                case kPrimVoid: return { kMarshalNone, nullptr };
                case kPrimBoolean: return { kMarshalI32, &dn2cpp_bool_type };
                case kPrimChar: return { kMarshalI32, &dn2cpp_char_type };
                case kPrimInt32: return { kMarshalI32, &dn2cpp_int32_type };
                case kPrimInt64: return { kMarshalI64, &dn2cpp_int64_type };
                case kPrimSingle: return { kMarshalF32, &dn2cpp_single_type };
                case kPrimDouble: return { kMarshalF64, &dn2cpp_double_type };
                case kPrimString: return { kMarshalRef, nullptr };
                default:
                    interp_fail("BPI bind: unsupported primitive in an import signature");
            }
        case DN2CPP_BPI_TAG_IMPORT:
        {
            uint32_t idx = DN2CPP_BPI_REF_INDEX(ref);
            if (idx >= img->importCount)
                interp_fail("BPI bind: signature type import out of bounds");
            // An SZArray type import is always a reference; its type-info is
            // bound after patch-type construction (a patch-class element's
            // type-info must exist first), so the marshalling never waits on it.
            if (img->imports[idx].kind == DN2CPP_BPI_IMPORT_TYPE
                && (img->imports[idx].aux0 & 1u) != 0)
            {
                return { kMarshalRef, nullptr };
            }
            const Dn2CppTypeInfo* ti = img->bindings[idx].type;
            if (ti == nullptr)
                interp_fail("BPI bind: unresolved type import in a signature");
            if ((ti->flags & DN2CPP_TF_VALUETYPE) != 0)
                interp_fail("BPI bind: value types in import signatures need a later slice");
            return { kMarshalRef, nullptr };
        }
        default:
            interp_fail("BPI bind: unsupported signature EntityRef tag");
    }
}

// Forward declarations: the delegate bridge lookups are defined below (next to
// find_itf_trampoline) but bind_method_import resolves a delegate Invoke here.
const void* find_dg_thunk(const Dn2CppTypeInfo* dg);
const void* find_dg_invoke_bridge(const Dn2CppTypeInfo* dg);

enum OverloadStatus { kOverloadNone, kOverloadFound, kOverloadAmbiguous };

// Resolves one method import against a single reflected method table (`methods`
// / `count`), among entries matching (name — unless `matchName` is false, for
// constructors —, `paramCount`, `wantStatic`). A --hotupdate-base build stamps a
// `sigShape` on every row, so the import's `shape` string picks the exact
// overload — chiefly one instantiation out of the several a generic method emits
// under one name. Falls back to a lone unshaped candidate (a legacy row with no
// sigShape — never produced by a --hotupdate-base build, so defensive only) and
// reports ambiguity only among unshaped candidates. Returns kOverloadNone when
// the table holds no matching overload (the caller then walks the base chain or
// fails as unresolved); a shaped-but-no-match table reads as None too, so a
// requested instantiation the base never emitted is a clean unresolved failure
// rather than a silent mis-bind onto a different instantiation.
OverloadStatus resolve_overload(
    const Dn2CppMethodInfo* methods, int32_t count,
    const char* name, uint32_t nameLen, bool matchName,
    uint32_t paramCount, bool wantStatic,
    const char* shape, uint32_t shapeLen,
    const Dn2CppMethodInfo** out)
{
    const Dn2CppMethodInfo* exact = nullptr;
    const Dn2CppMethodInfo* unshaped = nullptr;
    int cand = 0, unshapedCount = 0;
    for (int32_t i = 0; i < count; i++)
    {
        const Dn2CppMethodInfo& mi = methods[i];
        if (mi.paramCount != static_cast<int32_t>(paramCount))
            continue;
        if (((mi.attrs & DN2CPP_MTHA_STATIC) != 0) != wantStatic)
            continue;
        if (matchName && !name_equals(name, nameLen, mi.name))
            continue;
        cand++;
        if (mi.sigShape == nullptr)
        {
            unshaped = &mi;
            unshapedCount++;
        }
        else if (name_equals(shape, shapeLen, mi.sigShape))
        {
            exact = &mi;
        }
    }
    if (exact != nullptr)
    {
        *out = exact;
        return kOverloadFound;
    }
    if (cand == 0)
        return kOverloadNone;
    if (unshapedCount == cand)
    {
        // Legacy (unshaped) candidates only: no sigShape to discriminate by.
        if (cand == 1)
        {
            *out = unshaped;
            return kOverloadFound;
        }
        return kOverloadAmbiguous;
    }
    // Shaped candidates existed but none equalled the import's shape: the wanted
    // overload/instantiation is not in this table.
    return kOverloadNone;
}

// The intrinsic table has TWO bind mouths — the exact-match loop below and the
// base-chain walk further down — so the row-to-binding transfer is ONE function
// both call: a field added to IntrinsicImport that a call arm tests cannot then
// reach half the bindings. (Only this row set produces the shapes those tests
// guard; every other bind route leaves both kinds unspecified, which the arms
// refuse.)
void bind_from_intrinsic_row(ImportBinding& b, const IntrinsicImport& ii)
{
    b.callShape = ii.callShape;
    b.fnPtr = ii.fnPtr;
    b.argCount = ii.argCount;
    b.recvKind = ii.recvKind;
    for (uint32_t j = 0; j < kMaxImportArgs; j++)
        b.argKinds[j] = ii.argKinds[j];
    // This route resolves no method row, so the row IS the signature statement:
    // `method` stays null and the capture test reads argCount + retType instead,
    // which is exact for the one capturable shape (Dn2CppObject*
    // (*)(Dn2CppObject*)).
    b.retType = ii.retTi;
}

void bind_method_import(const Dn2CppInterpImage* img, const Dn2CppBpiImport& imp, ImportBinding& b)
{
    uint32_t nameLen, shapeLen;
    const char* name = pool_name(img, imp.nameOff, &nameLen);
    const char* shape = pool_name(img, imp.sigShapeOff, &shapeLen);
    if (imp.declTypeImport >= img->importCount)
        interp_fail("BPI: method import has no declaring type");
    const Dn2CppBpiImport& declImp = img->imports[imp.declTypeImport];
    uint32_t declLen;
    const char* declName = pool_name(img, declImp.nameOff, &declLen);

    uint32_t paramCount = imp.aux0 & 0xFFFFu;
    bool isInstance = (imp.aux0 & 0x10000u) != 0;
    b.argCount = paramCount;
    b.isInstance = isInstance;
    b.vtableSlot = -1;
    // Only the intrinsic table below yields the guarded shapes, so every other
    // route leaves the receiver kind and the argument kinds unspecified — which
    // the call arms refuse.
    b.recvKind = kRecvUnspecified;
    for (uint32_t j = 0; j < kMaxImportArgs; j++)
        b.argKinds[j] = kArgUnspecified;

    // 1. The intrinsic import table (exact type + name + staticness + sigShape
    // match).
    for (const IntrinsicImport& ii : g_intrinsicImports)
    {
        if (ii.isInstance == isInstance
            && name_equals(declName, declLen, ii.type) && name_equals(name, nameLen, ii.method)
            && name_equals(shape, shapeLen, ii.sigShape))
        {
            bind_from_intrinsic_row(b, ii);
            return;
        }
    }

    // 2. The declaring type's emitted reflection metadata: `.ctor` against the
    // type's own ctors table, anything else against the method tables up the
    // base chain — matched by name + parameter count + staticness, then
    // disambiguated by full sigShape (resolve_overload) so same-(name, arity)
    // overloads — chiefly a generic method's several same-named instantiations —
    // bind to the exact one; a genuine tie (or no shape match) is rejected.
    const Dn2CppTypeInfo* declTi = img->bindings[imp.declTypeImport].type;
    if (declTi == nullptr)
        interp_fail("BPI bind: unresolved method import (declaring type not in the base image)");

    // An interface-typed callvirt (the declaring type is a base-image
    // interface): the concrete implementation is resolved from the receiver's
    // interface-dispatch map at run time, so the binding records the interface
    // type-info + method slot + the interface method's invoker (a --hotupdate-base
    // build emits it signature-only). Dispatch reuses the invoker arm.
    if ((declTi->flags & DN2CPP_TF_INTERFACE) != 0)
    {
        if (!isInstance)
            interp_fail("BPI bind: static interface method imports need a later slice");
        const Dn2CppMethodInfo* im = nullptr;
        if (resolve_overload(declTi->methods, declTi->methodCount, name, nameLen, /*matchName*/ true,
                paramCount, /*wantStatic*/ false, shape, shapeLen, &im) == kOverloadAmbiguous)
            interp_fail("BPI bind: ambiguous interface method import (same-arity overloads share a sigShape)");
        if (im == nullptr || im->invoker == nullptr || im->vtableSlot < 0)
            interp_fail("BPI bind: unresolved interface method import (is the base built with --hotupdate-base?)");
        if (paramCount > kMaxImportArgs)
            interp_fail("BPI bind: import call arity needs a later slice");
        if (static_cast<uint64_t>(imp.aux1) + paramCount + 1 > img->localSigCount)
            interp_fail("BPI: import signature run out of bounds");
        const uint32_t* run = img->localSigs + imp.aux1;
        b.ret = marshal_from_ref(img, run[0]);
        for (uint32_t j = 0; j < paramCount; j++)
            b.args[j] = marshal_from_ref(img, run[1 + j]);
        b.callShape = kShapeInvoker;
        b.invoker = im->invoker;
        b.retType = im->returnType;
        b.method = im;
        b.type = declTi;          // the interface type-info (resolve_interface key)
        b.vtableSlot = im->vtableSlot; // the interface method slot
        b.isInterface = true;
        return;
    }

    // A delegate Invoke (the declaring type is a base-image delegate): route
    // through the pre-emitted M2N invoke bridge keyed by the delegate type-info,
    // which unpacks the argument slots and dispatches through the delegate's own
    // emitted invoker (walking the multicast chain, landing each f_method on an
    // interpreted thunk or an AOT method). The receiver is the delegate, so the
    // dispatch reads no reflection metadata on the delegate type.
    if ((declTi->flags & DN2CPP_TF_DELEGATE) != 0 && name_equals(name, nameLen, "Invoke"))
    {
        if (!isInstance)
            interp_fail("BPI bind: a delegate Invoke import must be an instance member");
        if (paramCount > kMaxImportArgs)
            interp_fail("BPI bind: delegate Invoke arity needs a later slice");
        const void* bridge = find_dg_invoke_bridge(declTi);
        if (bridge == nullptr)
            interp_fail("BPI bind: no delegate invoke bridge (Invoke signature outside the bridged surface)");
        b.callShape = kShapeDelegateInvoke;
        b.fnPtr = const_cast<void*>(bridge);
        b.type = declTi;
        return;
    }

    bool isCtor = name_equals(name, nameLen, ".ctor");
    const Dn2CppMethodInfo* found = nullptr;
    if (isCtor)
    {
        if (!isInstance)
            interp_fail("BPI bind: a constructor import must be an instance member");
        // `new <ExceptionType>(...)`: mirror the AOT newobj interception. Every
        // exception ctor is intercepted so the object shares the uniform
        // message-carrying prefix (dn2cpp_exception_new). The Message /
        // innerException are recovered positionally for the shapes the AOT
        // ExceptionMessageArgIndex / ExceptionInnerArgIndex recognize; a
        // non-opaque, actually-reached ctor is then ALSO resolved to its real
        // body so its own field writes (a derived exception's `Code = c;`) run
        // — the newobj arms allocate via dn2cpp_exception_new, seed the prefix,
        // then invoke it. Opaque (System.Exception / AggregateException) and
        // never-reached ctors leave fnPtr null: a seed-only degrade, exactly as
        // AOT discards the opaque intrinsic ctor body.
        if (type_is_exception(declTi))
        {
            if (paramCount > kMaxImportArgs)
                interp_fail("BPI bind: import call arity needs a later slice");
            if (static_cast<uint64_t>(imp.aux1) + paramCount + 1 > img->localSigCount)
                interp_fail("BPI: import signature run out of bounds");
            const uint32_t* run = img->localSigs + imp.aux1;
            for (uint32_t j = 0; j < paramCount; j++)
                b.args[j] = marshal_from_ref(img, run[1 + j]);
            b.excMsgArg = -1;
            b.excInnerArg = -1;
            // A parameter is a plain (string) message.
            auto isStringParam = [&](uint32_t j) {
                return DN2CPP_BPI_REF_TAG(run[1 + j]) == DN2CPP_BPI_TAG_PRIM
                    && DN2CPP_BPI_REF_INDEX(run[1 + j]) == kPrimString;
            };
            // A parameter is an Exception (the innerException) — a resolved
            // type import whose base chain reaches System.Exception.
            auto isExceptionParam = [&](uint32_t j) {
                uint32_t r = run[1 + j];
                if (DN2CPP_BPI_REF_TAG(r) != DN2CPP_BPI_TAG_IMPORT
                    || DN2CPP_BPI_REF_INDEX(r) >= img->importCount)
                    return false;
                const Dn2CppTypeInfo* it = img->bindings[DN2CPP_BPI_REF_INDEX(r)].type;
                return it != nullptr && type_is_exception(it);
            };
            // Mirror Compilation.ExceptionMessageArgIndex /
            // ExceptionInnerArgIndex exactly (Compilation.cs): (string),
            // (string, Exception), (TCode, string), (TCode, string, Exception)
            // — TCode being a lead param that is neither string nor Exception
            // (e.g. ZlibException's (ZlibResult, string)).
            if (paramCount == 1 && isStringParam(0))
            {
                b.excMsgArg = 0;
            }
            else if (paramCount == 2 && isStringParam(0) && isExceptionParam(1))
            {
                b.excMsgArg = 0;
                b.excInnerArg = 1;
            }
            else if (paramCount == 2 && !isStringParam(0) && !isExceptionParam(0)
                && isStringParam(1))
            {
                b.excMsgArg = 1;
            }
            else if (paramCount == 3 && !isStringParam(0) && !isExceptionParam(0)
                && isStringParam(1) && isExceptionParam(2))
            {
                b.excMsgArg = 1;
                b.excInnerArg = 2;
            }
            // Resolve the real ctor body when the base build reached it (a
            // non-opaque derived exception's ctor with its own field writes).
            // Ambiguity is rejected identically to the normal ctor path; a
            // miss (opaque type, or a ctor the base never emitted) leaves
            // fnPtr/invoker null for the seed-only degrade.
            const Dn2CppMethodInfo* exFound = nullptr;
            if (resolve_overload(declTi->ctors, declTi->ctorCount, nullptr, 0, /*matchName*/ false,
                    paramCount, /*wantStatic*/ false, shape, shapeLen, &exFound) == kOverloadAmbiguous)
                interp_fail("BPI bind: ambiguous constructor import (same-arity overloads share a sigShape)");
            if (exFound != nullptr && exFound->fnPtr != nullptr && exFound->invoker != nullptr)
            {
                b.fnPtr = exFound->fnPtr;
                b.invoker = exFound->invoker;
            }
            b.callShape = kShapeExceptionNew;
            b.type = declTi;
            b.isCtor = true;
            return;
        }
        if ((declTi->flags & (DN2CPP_TF_VALUETYPE | DN2CPP_TF_ABSTRACT | DN2CPP_TF_INTERFACE)) != 0)
            interp_fail("BPI bind: newobj target is not a constructible reference type");
        if (resolve_overload(declTi->ctors, declTi->ctorCount, nullptr, 0, /*matchName*/ false,
                paramCount, /*wantStatic*/ false, shape, shapeLen, &found) == kOverloadAmbiguous)
            interp_fail("BPI bind: ambiguous constructor import (same-arity overloads share a sigShape)");
    }
    else
    {
        if (isInstance && (declTi->flags & DN2CPP_TF_VALUETYPE) != 0)
            interp_fail("BPI bind: value-type instance calls need a later slice");
        for (const Dn2CppTypeInfo* ti = declTi; ti != nullptr && found == nullptr; ti = ti->base)
        {
            if (resolve_overload(ti->methods, ti->methodCount, name, nameLen, /*matchName*/ true,
                    paramCount, /*wantStatic*/ !isInstance, shape, shapeLen, &found) == kOverloadAmbiguous)
                interp_fail("BPI bind: ambiguous method import (same-arity overloads share a sigShape)");
        }
        // Object-inherited intrinsics imported through a DERIVED declaring type:
        // Roslyn stamps a non-virtual `base.ToString()` against the nearest
        // override at or above the base type, which the exact-name loop above
        // misses, and a registry-bound BCL declaring type carries no emitted
        // method table for the metadata walk to hit. So mirror that walk over the
        // intrinsic table — each instance row at every base type-info's name, then
        // at System.Object, the IMPLICIT root every reference-type chain inherits
        // GetType/ToString from without materializing a node for it. Emitted
        // metadata stays first: a real body wins over the uniform dispatcher.
        if (found == nullptr && isInstance)
        {
            auto bindIntrinsic = [&](const char* typeName) {
                for (const IntrinsicImport& ii : g_intrinsicImports)
                {
                    if (ii.isInstance && std::strcmp(typeName, ii.type) == 0
                        && name_equals(name, nameLen, ii.method)
                        && name_equals(shape, shapeLen, ii.sigShape))
                    {
                        // The SECOND mouth of the intrinsic table, and the
                        // reason the transfer is a shared function: the call
                        // arms refuse an unspecified kind, so a row bound here
                        // without one would fail every legitimate call. The
                        // kinds stay the ROW's, not the derived declaring
                        // type's — a derived exception binding
                        // System.Exception's get_Message wants kRecvException,
                        // which its own base chain satisfies.
                        bind_from_intrinsic_row(b, ii);
                        return true;
                    }
                }
                return false;
            };
            for (const Dn2CppTypeInfo* ti = declTi->base; ti != nullptr; ti = ti->base)
            {
                if (ti->name != nullptr && bindIntrinsic(ti->name))
                    return;
            }
            if (bindIntrinsic("System.Object"))
                return;
        }
    }
    if (found == nullptr || found->fnPtr == nullptr || found->invoker == nullptr)
        interp_fail("BPI bind: unresolved method import");

    // The [return, params...] signature run drives argument boxing and result
    // unboxing across the invoker-thunk boundary.
    if (paramCount > kMaxImportArgs)
        interp_fail("BPI bind: import call arity needs a later slice");
    if (static_cast<uint64_t>(imp.aux1) + paramCount + 1 > img->localSigCount)
        interp_fail("BPI: import signature run out of bounds");
    const uint32_t* run = img->localSigs + imp.aux1;
    b.ret = marshal_from_ref(img, run[0]);
    for (uint32_t j = 0; j < paramCount; j++)
        b.args[j] = marshal_from_ref(img, run[1 + j]);

    b.callShape = kShapeInvoker;
    b.fnPtr = found->fnPtr;
    b.invoker = found->invoker;
    b.retType = found->returnType;
    b.method = found;
    b.type = declTi;
    b.isCtor = isCtor;
    b.vtableSlot = found->vtableSlot;
}

// Field import: resolved to the declaring type's Dn2CppFieldInfo entry (base
// chain walked; fields are unique by name). The bound accessor thunks carry
// the instance offset / static address / lazy-cctor guard, so the binding
// holds no separate storage location.
void bind_field_import(const Dn2CppInterpImage* img, const Dn2CppBpiImport& imp, ImportBinding& b)
{
    uint32_t nameLen;
    const char* name = pool_name(img, imp.nameOff, &nameLen);
    if (imp.declTypeImport >= img->importCount)
        interp_fail("BPI: field import has no declaring type");
    const Dn2CppTypeInfo* declTi = img->bindings[imp.declTypeImport].type;
    if (declTi == nullptr)
        interp_fail("BPI bind: unresolved field import (declaring type not in the base image)");
    const Dn2CppFieldInfo* found = nullptr;
    for (const Dn2CppTypeInfo* ti = declTi; ti != nullptr && found == nullptr; ti = ti->base)
    {
        for (int32_t i = 0; i < ti->fieldCount; i++)
        {
            if (name_equals(name, nameLen, ti->fields[i].name))
            {
                found = &ti->fields[i];
                break;
            }
        }
    }
    if (found == nullptr)
        interp_fail("BPI bind: unresolved field import");
    b.field = found;
    b.fieldVal = marshal_from_ref(img, imp.aux1);
    if (b.fieldVal.kind == kMarshalNone)
        interp_fail("BPI bind: field import carries no value type reference");
}

// The storage class of one patch instance field, decided at load from its
// baked type reference: fixes the byte width/alignment the loader lays the
// field out with and the Slot conversion the ldfld/stfld arms apply.
uint8_t patch_field_kind(const Dn2CppInterpImage* img, uint32_t typeRef)
{
    switch (DN2CPP_BPI_REF_TAG(typeRef))
    {
        case DN2CPP_BPI_TAG_PRIM:
            switch (DN2CPP_BPI_REF_INDEX(typeRef))
            {
                case kPrimBoolean:
                case kPrimChar:
                case kPrimInt32: return kMarshalI32;
                case kPrimInt64: return kMarshalI64;
                case kPrimSingle: return kMarshalF32;
                case kPrimDouble: return kMarshalF64;
                case kPrimString: return kMarshalRef;
                default:
                    interp_fail("BPI: unsupported patch-field primitive type");
            }
        case DN2CPP_BPI_TAG_IMPORT:
        {
            uint32_t idx = DN2CPP_BPI_REF_INDEX(typeRef);
            if (idx >= img->importCount)
                interp_fail("BPI: patch-field type import out of bounds");
            // SZArray imports bind after patch-type construction (which is
            // when this runs); the field is a reference either way.
            if (img->imports[idx].kind == DN2CPP_BPI_IMPORT_TYPE
                && (img->imports[idx].aux0 & 1u) != 0)
            {
                return kMarshalRef;
            }
            const Dn2CppTypeInfo* ti = img->bindings[idx].type;
            if (ti == nullptr)
                interp_fail("BPI bind: unresolved patch-field type import");
            if ((ti->flags & DN2CPP_TF_VALUETYPE) != 0)
                interp_fail("BPI bind: value-type patch fields need a later slice");
            return kMarshalRef;
        }
        case DN2CPP_BPI_TAG_PATCH:
            return kMarshalRef; // a patch-defined class type
        default:
            interp_fail("BPI: unsupported patch-field type reference");
    }
}

// Reads the ItfDesc header at `off` — { u32 itfCount; ItfEntry[] } — returning
// a pointer to the first ItfEntry and the entry count. Bounds-checked; each
// ItfEntry is variable-length ({ u32 itfImport; u32 fullSlotCount; u32
// implCount; { u32 slot; u32 patchMethodIdx }[implCount] }) and walked by the
// callers.
const uint8_t* itfdesc_at(const Dn2CppInterpImage* img, uint32_t off, uint32_t* itfCountOut)
{
    if (img->itfdesc == nullptr || off % 4 != 0
        || static_cast<uint64_t>(off) + 4 > img->itfdescLen)
        interp_fail("BPI: interface description out of bounds");
    uint32_t itfCount;
    std::memcpy(&itfCount, img->itfdesc + off, 4);
    *itfCountOut = itfCount;
    return img->itfdesc + off + 4;
}

// Finds the pre-emitted N2M interface trampoline for (interface, slot) in the
// registration table, or null when none is registered (a signature outside the
// bridged marshalling surface — the load fails loudly at the call site).
const void* find_itf_trampoline(const Dn2CppTypeInfo* itf, uint32_t slot)
{
    for (int32_t r = 0; r < dn2cpp_n2m_itf_trampoline_count; r++)
    {
        if (dn2cpp_n2m_itf_trampolines[r].itf == itf
            && dn2cpp_n2m_itf_trampolines[r].slot == static_cast<int32_t>(slot))
            return dn2cpp_n2m_itf_trampolines[r].fn;
    }
    return nullptr;
}

// The pre-emitted delegate thunk (the AOT-ABI f_method a patch method bound into
// this delegate uses) and the M2N invoke bridge (an interpreted Invoke's entry
// into the delegate's emitted invoker) for a delegate type, or null when the
// delegate's Invoke signature is outside the bridged marshalling surface.
const void* find_dg_thunk(const Dn2CppTypeInfo* dg)
{
    for (int32_t r = 0; r < dn2cpp_n2m_delegate_thunk_count; r++)
        if (dn2cpp_n2m_delegate_thunks[r].dg == dg)
            return dn2cpp_n2m_delegate_thunks[r].fn;
    return nullptr;
}

const void* find_dg_invoke_bridge(const Dn2CppTypeInfo* dg)
{
    for (int32_t r = 0; r < dn2cpp_dg_invoke_bridge_count; r++)
        if (dn2cpp_dg_invoke_bridges[r].dg == dg)
            return dn2cpp_dg_invoke_bridges[r].fn;
    return nullptr;
}

// Builds a patch type's extended interface-dispatch map from its ItfDesc run:
// for each implemented base-image interface, a fresh slots array seeded from the
// base type's implementation of that interface (if any) with each overridden
// slot rewritten to the matching N2M interface trampoline — so an AOT (or
// interpreted) interface call on a patch instance lands on the interpreted
// implementation. Interfaces the type inherits unchanged keep sharing the base
// entries. Sets ti->interfaces / interfaceCount.
//
// `ti` and `base` are non-null by contract and the reads below are raw; a new
// caller must establish both the way the single existing one does (`ti` is the
// fresh loader allocation, `base` defaults to &dn2cpp_object_type and an
// unresolved import is rejected earlier).
void build_patch_itf_map(const Dn2CppInterpImage* img, Dn2CppTypeInfo* ti,
    const Dn2CppTypeInfo* base, uint32_t itfDescOff)
{
    uint32_t itfCount;
    const uint8_t* p = itfdesc_at(img, itfDescOff, &itfCount);
    const uint8_t* end = img->itfdesc + img->itfdescLen;

    uint32_t maxCount = static_cast<uint32_t>(base->interfaceCount) + itfCount;
    auto* entries = static_cast<Dn2CppInterfaceEntry*>(
        dn2cpp_alloc(maxCount * sizeof(Dn2CppInterfaceEntry)));
    const Dn2CppTypeInfo** rebuilt = itfCount > 0
        ? static_cast<const Dn2CppTypeInfo**>(dn2cpp_alloc(itfCount * sizeof(const Dn2CppTypeInfo*)))
        : nullptr;
    uint32_t n = 0;
    for (uint32_t e = 0; e < itfCount; e++)
    {
        if (p + 12 > end)
            interp_fail("BPI: interface description entry out of bounds");
        uint32_t itfImport, fullSlotCount, implCount;
        std::memcpy(&itfImport, p, 4);
        std::memcpy(&fullSlotCount, p + 4, 4);
        std::memcpy(&implCount, p + 8, 4);
        p += 12;
        if (DN2CPP_BPI_REF_TAG(itfImport) != DN2CPP_BPI_TAG_IMPORT)
            interp_fail("BPI: interface description import tag");
        uint32_t ii = DN2CPP_BPI_REF_INDEX(itfImport);
        if (ii >= img->importCount)
            interp_fail("BPI: interface import out of bounds");
        const Dn2CppTypeInfo* itfTi = img->bindings[ii].type;
        if (itfTi == nullptr || (itfTi->flags & DN2CPP_TF_INTERFACE) == 0)
            interp_fail("BPI bind: interface import did not resolve to an interface type");

        auto** slots = static_cast<const void**>(
            dn2cpp_alloc_atomic(fullSlotCount * sizeof(const void*)));
        const void** baseSlots = dn2cpp_try_resolve_interface(base, itfTi);
        if (baseSlots != nullptr)
            std::memcpy(slots, baseSlots, fullSlotCount * sizeof(const void*));
        else
            std::memset(slots, 0, fullSlotCount * sizeof(const void*));

        if (p + static_cast<uint64_t>(implCount) * 8 > end)
            interp_fail("BPI: interface description slots out of bounds");
        for (uint32_t s = 0; s < implCount; s++)
        {
            uint32_t slot;
            std::memcpy(&slot, p, 4);
            p += 8; // skip the baked patchMethodIdx (resolved at dispatch)
            if (slot >= fullSlotCount)
                interp_fail("BPI: interface override slot out of bounds");
            const void* fn = find_itf_trampoline(itfTi, slot);
            if (fn == nullptr)
                interp_fail("BPI bind: no N2M interface trampoline for the implemented slot "
                            "(signature shape outside the bridged surface)");
            slots[slot] = fn;
        }
        entries[n].itf = itfTi;
        entries[n].slots = slots;
        if (rebuilt != nullptr)
            rebuilt[e] = itfTi;
        n++;
    }
    // Inherit the base's remaining interface entries (those this type does not
    // re-implement) unchanged.
    for (int32_t bi = 0; bi < base->interfaceCount; bi++)
    {
        const Dn2CppTypeInfo* bItf = base->interfaces[bi].itf;
        bool rebuiltHere = false;
        for (uint32_t e = 0; e < itfCount && !rebuiltHere; e++)
            if (rebuilt[e] == bItf)
                rebuiltHere = true;
        if (rebuiltHere)
            continue;
        entries[n++] = base->interfaces[bi];
    }
    ti->interfaces = entries;
    ti->interfaceCount = static_cast<int32_t>(n);
}

// ---- register code format: load-time verification ----

// Operand-class lookup for the register opcode table, indexed by opcode value
// (the X-macro list is value-sequential from 0; dn2cpp_interp_regops.h asserts
// it, so position == value).
constexpr uint8_t g_regOpClass[kRegOpCount] = {
#define DN2CPP_REGOP_CLASS_ENTRY(name, value, cls) kRegClass##cls,
    DN2CPP_REGOPS(DN2CPP_REGOP_CLASS_ENTRY)
#undef DN2CPP_REGOP_CLASS_ENTRY
};

// How many contiguous registers a call-family instruction consumes from its
// window base — derived from the callee's method record / import binding, the
// same metadata the dispatch arms read (docs/BPI-FORMAT.md "Load-time
// verification"). `a` is the instruction's EntityRef operand.
uint32_t reg_call_consumed(const Dn2CppInterpImage* img, uint16_t op, uint32_t a)
{
    if (DN2CPP_BPI_REF_TAG(a) == DN2CPP_BPI_TAG_PATCH)
    {
        uint32_t idx = DN2CPP_BPI_REF_INDEX(a);
        if (idx >= img->methodCount)
            interp_fail("BPI: call method index out of bounds");
        uint32_t n = img->methods[idx].argCount;
        if (op == R_NEWOBJ)
        {
            // The receiver is allocated by the arm itself; the window holds
            // only the ctor parameters.
            if (n == 0)
                interp_fail("BPI: patch newobj target has an invalid frame shape");
            return n - 1;
        }
        return n;
    }
    if (DN2CPP_BPI_REF_TAG(a) != DN2CPP_BPI_TAG_IMPORT)
        interp_fail("interp: unsupported entity-reference tag");
    uint32_t idx = DN2CPP_BPI_REF_INDEX(a);
    if (idx >= img->importCount)
        interp_fail("BPI: import index out of bounds");
    const ImportBinding& b = img->bindings[idx];
    if (op == R_NEWOBJ && b.kind == DN2CPP_BPI_IMPORT_TYPE)
        return 2; // delegate construction: [target, ftn]
    if (b.kind != DN2CPP_BPI_IMPORT_METHOD)
        interp_fail("interp: import kind mismatch");
    // A ctor window carries the parameters only (the object is allocated by
    // the arm); a call window prepends the receiver / delegate for instance
    // imports (a delegate Invoke binds as an instance member, so its
    // [dg, args...] window falls out of the same rule).
    if (op == R_NEWOBJ)
        return b.argCount;
    return b.argCount + (b.isInstance ? 1u : 0u);
}

// One linear verification scan per method of a register-format image, run at
// the end of dn2cpp_patch_load — after import binding and patch-type
// construction, so call windows can read callee argCounts. Validates opcode
// values, register operands (per operand class) against the method's register
// count, branch/leave targets, rethrow EH indices, and call windows; the
// dispatch loop then trusts every operand unchecked (the stack interpreter's
// validate-up-front philosophy). R_INVALID is rejected here too — zero-filled
// corruption fails loudly at load — and keeps a runtime trap arm as backstop.
void verify_reg_code(const Dn2CppInterpImage* img)
{
    for (uint32_t mi = 0; mi < img->methodCount; mi++)
    {
        const Dn2CppBpiMethod& m = img->methods[mi];
        uint32_t slotCount = m.argCount + m.localCount;
        if (slotCount > kRegSlotLimit || m.maxStack > kRegTempLimit)
            interp_fail("BPI: register frame exceeds the 64-slot/64-temp limits");
        uint32_t regCount = slotCount + m.maxStack;
        if (m.codeOff % sizeof(Dn2CppBpiInsn) != 0)
            interp_fail("BPI: misaligned code offset");
        uint32_t first = m.codeOff / sizeof(Dn2CppBpiInsn);
        if (first + static_cast<uint64_t>(m.codeInsnCount) > img->insnCount)
            interp_fail("BPI: method code out of bounds");
        const Dn2CppBpiInsn* code = img->code + first;
        for (uint32_t pc = 0; pc < m.codeInsnCount; pc++)
        {
            const Dn2CppBpiInsn& insn = code[pc];
            if (insn.op >= kRegOpCount || insn.op == R_EXT || insn.op == R_INVALID)
                interp_fail("BPI: register opcode outside the table");
            // Register operands per the opcode's operand class. Fields the
            // class leaves unused (0xFF by convention) are not operands and
            // are not validated.
            uint32_t r0 = insn.pfx & 0xFFu;
            uint32_t r1 = insn.pfx >> 8;
            switch (g_regOpClass[insn.op])
            {
                case kRegClassC3:
                    if ((insn.a & 0xFFu) >= regCount)
                        interp_fail("BPI: register operand out of bounds");
                    [[fallthrough]];
                case kRegClassC2:
                    if (r1 >= regCount)
                        interp_fail("BPI: register operand out of bounds");
                    [[fallthrough]];
                case kRegClassC1:
                    if (r0 >= regCount)
                        interp_fail("BPI: register operand out of bounds");
                    break;
                default: // C0: no register operands
                    break;
            }
            // The branch block (R_BR through the compare-branches) plus
            // R_LEAVE carry an absolute instruction index in `a`.
            if ((insn.op >= R_BR && insn.op <= R_BNE_UN_REF) || insn.op == R_LEAVE)
            {
                if (insn.a >= m.codeInsnCount)
                    interp_fail("BPI: branch target out of bounds");
            }
            switch (insn.op)
            {
                case R_RETHROW:
                    if (insn.a >= m.ehCount)
                        interp_fail("BPI: rethrow record index out of bounds");
                    break;
                case R_CALL:
                case R_CALLVIRT:
                case R_NEWOBJ:
                {
                    uint32_t consumed = reg_call_consumed(img, insn.op, insn.a);
                    if (static_cast<uint64_t>(r0) + consumed > regCount)
                        interp_fail("BPI: call window out of bounds");
                    break;
                }
                default:
                    break;
            }
        }
    }
}

} // namespace

Dn2CppInterpImage* dn2cpp_patch_load(const void* blobPtr, size_t len)
{
    const uint8_t* blob = static_cast<const uint8_t*>(blobPtr);
    if (blob == nullptr || (reinterpret_cast<uintptr_t>(blob) & 7) != 0)
        interp_fail("BPI: blob must be 8-byte-aligned non-null memory");
    if (len < sizeof(Dn2CppBpiHeader))
        interp_fail("BPI: blob shorter than the header");
    const Dn2CppBpiHeader* h = reinterpret_cast<const Dn2CppBpiHeader*>(blob);
    if (std::memcmp(h->magic, "DN2BPI\0", 8) != 0)
        interp_fail("BPI: bad magic");
    if (h->formatVersion != DN2CPP_BPI_VERSION)
        interp_fail("BPI: unsupported format version");
    if ((h->flags & ~DN2CPP_BPI_FLAGS_SUPPORTED) != 0)
        interp_fail("BPI: unsupported header flags (patch baked for a newer runtime)");
    if (h->baseImageAbiHash != dn2cpp_base_image_abi_hash)
        interp_fail("BPI/base ABI mismatch: rebuild the patch against this base image");
    if (h->sectionTableOff + static_cast<uint64_t>(h->sectionCount) * sizeof(Dn2CppBpiSection) > len)
        interp_fail("BPI: section table out of bounds");

    auto* img = static_cast<Dn2CppInterpImage*>(dn2cpp_alloc(sizeof(Dn2CppInterpImage)));
    img->blob = blob;
    img->len = len;
    img->entryMethodIdx = h->entryMethodIdx;
    img->regCode = (h->flags & DN2CPP_BPI_FLAG_REGCODE) != 0;

    const Dn2CppBpiSection* sections =
        reinterpret_cast<const Dn2CppBpiSection*>(blob + h->sectionTableOff);
    for (uint32_t i = 0; i < h->sectionCount; i++)
    {
        const Dn2CppBpiSection& s = sections[i];
        if (static_cast<uint64_t>(s.offset) + s.length > len)
            interp_fail("BPI: section out of bounds");
        const uint8_t* data = blob + s.offset;
        switch (s.kind)
        {
            case DN2CPP_BPI_SEC_STRINGPOOL:
                img->strings = data;
                img->stringsLen = s.length;
                break;
            case DN2CPP_BPI_SEC_IMPORTTABLE:
                img->imports = reinterpret_cast<const Dn2CppBpiImport*>(data);
                img->importCount = s.count;
                break;
            case DN2CPP_BPI_SEC_TYPETABLE:
                img->types = reinterpret_cast<const Dn2CppBpiType*>(data);
                img->typeCount = s.count;
                break;
            case DN2CPP_BPI_SEC_FIELDTABLE:
                img->fields = reinterpret_cast<const Dn2CppBpiField*>(data);
                img->fieldCount = s.count;
                break;
            case DN2CPP_BPI_SEC_METHODTABLE:
                img->methods = reinterpret_cast<const Dn2CppBpiMethod*>(data);
                img->methodCount = s.count;
                break;
            case DN2CPP_BPI_SEC_CODE:
                img->code = reinterpret_cast<const Dn2CppBpiInsn*>(data);
                img->insnCount = s.count;
                break;
            case DN2CPP_BPI_SEC_EHTABLE:
                img->eh = reinterpret_cast<const Dn2CppBpiEH*>(data);
                img->ehCount = s.count;
                break;
            case DN2CPP_BPI_SEC_LOCALSIG:
                img->localSigs = reinterpret_cast<const uint32_t*>(data);
                img->localSigCount = s.count;
                break;
            case DN2CPP_BPI_SEC_USERSTRINGS:
                img->userStrings = data;
                img->userStringsLen = s.length;
                break;
            case DN2CPP_BPI_SEC_VTABLEDESC:
                img->vtdesc = data;
                img->vtdescLen = s.length;
                break;
            case DN2CPP_BPI_SEC_ITFDESC:
                img->itfdesc = data;
                img->itfdescLen = s.length;
                break;
            default:
                // Sections this slice does not interpret (switch tables, …)
                // are validated for bounds and skipped.
                break;
        }
    }
    if (img->importCount != h->importCount || img->typeCount != h->patchTypeCount)
        interp_fail("BPI: header counts disagree with the section table");

    // One-shot bind pass — the only load-time allocation besides the image
    // itself, proportional to the import count. Types first (methods anchor to
    // them), then members.
    if (img->importCount > 0)
    {
        img->bindings = static_cast<ImportBinding*>(
            dn2cpp_alloc(img->importCount * sizeof(ImportBinding)));
        for (uint32_t i = 0; i < img->importCount; i++)
        {
            if (img->imports[i].kind != DN2CPP_BPI_IMPORT_TYPE)
                continue;
            uint32_t nameLen;
            const char* name = pool_name(img, img->imports[i].nameOff, &nameLen);
            img->bindings[i].kind = DN2CPP_BPI_IMPORT_TYPE;
            // Null is fine here: an intrinsic-table method import (e.g. on
            // System.Console) anchors by name only.
            img->bindings[i].type = find_registry_type(name, nameLen);
        }
        for (uint32_t i = 0; i < img->importCount; i++)
        {
            const Dn2CppBpiImport& imp = img->imports[i];
            switch (imp.kind)
            {
                case DN2CPP_BPI_IMPORT_TYPE:
                    break;
                case DN2CPP_BPI_IMPORT_METHOD:
                    img->bindings[i].kind = DN2CPP_BPI_IMPORT_METHOD;
                    bind_method_import(img, imp, img->bindings[i]);
                    break;
                case DN2CPP_BPI_IMPORT_FIELD:
                    img->bindings[i].kind = DN2CPP_BPI_IMPORT_FIELD;
                    bind_field_import(img, imp, img->bindings[i]);
                    break;
                default:
                    interp_fail("BPI bind: unknown import kind");
            }
        }
    }

    // Patch static state: one GC-scanned Slot per baked static field
    // (converter-assigned staticSlot indices are consecutive across the
    // image). Reference slots must be collector-visible, so the storage is
    // dn2cpp_alloc (scanned, zero-initialized) hung off the image — itself
    // chained into the static root list below, so it lives for the process
    // lifetime.
    uint32_t staticCount = 0;
    for (uint32_t i = 0; i < img->fieldCount; i++)
    {
        if (img->fields[i].staticSlot != DN2CPP_BPI_REF_NONE)
            staticCount++;
    }
    for (uint32_t i = 0; i < img->fieldCount; i++)
    {
        uint32_t slot = img->fields[i].staticSlot;
        if (slot != DN2CPP_BPI_REF_NONE && slot >= staticCount)
            interp_fail("BPI: static slot index out of bounds");
    }
    if (staticCount > 0)
        img->statics = static_cast<Slot*>(dn2cpp_alloc(staticCount * sizeof(Slot)));
    img->staticCount = staticCount;

    // Per-type lazy-cctor guard flags plus the per-type failure slots (the
    // recorded exception of a throwing initializer — pointer-holding, so
    // dn2cpp_alloc, not the atomic arm), and the has-cctor contract check: a
    // flagged type's .cctor is the first entry of its own MethodTable run.
    if (img->typeCount > 0)
    {
        img->cctorStarted = static_cast<uint8_t*>(dn2cpp_alloc_atomic(img->typeCount));
        std::memset(img->cctorStarted, 0, img->typeCount);
        img->cctorFailed = static_cast<Dn2CppObject**>(
            dn2cpp_alloc(img->typeCount * sizeof(Dn2CppObject*)));
        for (uint32_t t = 0; t < img->typeCount; t++)
        {
            if ((img->types[t].flags & DN2CPP_BPI_TF_HAS_CCTOR) == 0)
                continue;
            if (img->types[t].methodCount == 0 || img->types[t].methodStart >= img->methodCount
                || img->methods[img->types[t].methodStart].declTypeIdx != t
                || img->methods[img->types[t].methodStart].argCount != 0)
                interp_fail("BPI: has-cctor type does not lead with its parameterless initializer");
        }
    }

    // Patch-type construction (one-shot): one real Dn2CppTypeInfo per TypeTable
    // record — an InterpTypeInfo, so N2M trampolines can route back to the image.
    // Numeric layout is owned HERE, never by the converter: each instance field's
    // byte offset is assigned by appending after the LIVE base type's
    // instanceSize with natural alignment, so a base-image relayout can never go
    // stale inside a BPI (the ABI hash guards the symbolic contract; the numbers
    // are derived fresh per load). A type with a baked VtableDesc gets a copy of
    // the base vtable with each overridden slot rewritten to the matching
    // pre-emitted N2M trampoline, so an AOT callvirt on a patch instance
    // dispatches into the interpreted override; likewise an extended
    // interface-dispatch map for a type implementing a base-image interface.
    // Types without either share the base's tables as-is.
    // A FieldTable with no TypeTable is refused rather than tolerated: the
    // instance-field offset and kind tables the ldfld/stfld arms index are built
    // inside the per-type walk below, so such an image would leave those arms
    // with null tables that only their fieldCount bound protects — and that bound
    // does not test the table pointer.
    if (img->typeCount == 0 && img->fieldCount > 0)
        interp_fail("BPI: a field table with no type table");
    if (img->typeCount > 0)
    {
        img->patchTypes = static_cast<const Dn2CppTypeInfo**>(
            dn2cpp_alloc(img->typeCount * sizeof(const Dn2CppTypeInfo*)));
        if (img->fieldCount > 0)
        {
            img->instFieldOffsets = static_cast<uint32_t*>(
                dn2cpp_alloc_atomic(img->fieldCount * sizeof(uint32_t)));
            std::memset(img->instFieldOffsets, 0, img->fieldCount * sizeof(uint32_t));
            img->instFieldKinds = static_cast<uint8_t*>(dn2cpp_alloc_atomic(img->fieldCount));
            std::memset(img->instFieldKinds, 0, img->fieldCount);
        }
        for (uint32_t t = 0; t < img->typeCount; t++)
        {
            const Dn2CppBpiType& bt = img->types[t];
            const Dn2CppTypeInfo* base = &dn2cpp_object_type;
            if (bt.baseRef != DN2CPP_BPI_REF_NONE)
            {
                if (DN2CPP_BPI_REF_TAG(bt.baseRef) == DN2CPP_BPI_TAG_PATCH)
                {
                    // A patch base: the converter bakes the TypeTable in
                    // base-before-derived order, so the base type-info (and
                    // its loader-computed instanceSize the field append below
                    // builds on) already exists.
                    uint32_t bi = DN2CPP_BPI_REF_INDEX(bt.baseRef);
                    if (bi >= t || img->patchTypes[bi] == nullptr)
                        interp_fail("BPI: a patch base type must precede its derived types");
                    base = img->patchTypes[bi];
                }
                else
                {
                    if (DN2CPP_BPI_REF_TAG(bt.baseRef) != DN2CPP_BPI_TAG_IMPORT)
                        interp_fail("BPI: unsupported patch base-type reference");
                    uint32_t bi = DN2CPP_BPI_REF_INDEX(bt.baseRef);
                    if (bi >= img->importCount)
                        interp_fail("BPI: patch base-type import out of bounds");
                    base = img->bindings[bi].type;
                    if (base == nullptr)
                        interp_fail("BPI bind: unresolved patch base type (not in the base image)");
                    if ((base->flags & (DN2CPP_TF_VALUETYPE | DN2CPP_TF_INTERFACE
                            | DN2CPP_TF_SEALED | DN2CPP_TF_ARRAY | DN2CPP_TF_DELEGATE)) != 0)
                        interp_fail("BPI bind: patch base type is not an inheritable class");
                    // A base-image exception base is supported: the field append
                    // below builds on the base's Dn2CppExceptionObject prefix
                    // (message/inner live in that prefix, a patch-declared field —
                    // e.g. Code — lands after it), and a `base(message)` chains to
                    // the base exception ctor through the kShapeExceptionNew call arm.
                }
            }
            // The floor for the field append is the base's own instance size, but
            // never smaller than the layout prefix the base's object shape needs:
            // sizeof(Dn2CppObject) normally, sizeof(Dn2CppExceptionObject) when the
            // base is an exception. The intrinsic exception handles carry
            // instanceSize 0 (dn2cpp_exception_new floors them the same way), so
            // without this an exception-derived patch type would place its first
            // field on top of the message/inner prefix and corrupt both.
            size_t floorSize = type_is_exception(base)
                ? sizeof(Dn2CppExceptionObject) : sizeof(Dn2CppObject);
            uint32_t size = base->instanceSize > static_cast<int32_t>(floorSize)
                ? static_cast<uint32_t>(base->instanceSize)
                : static_cast<uint32_t>(floorSize);
            uint32_t fieldStart = bt.fieldStartCount >> 16;
            uint32_t fieldCount = bt.fieldStartCount & 0xFFFFu;
            if (static_cast<uint64_t>(fieldStart) + fieldCount > img->fieldCount)
                interp_fail("BPI: type field run out of bounds");
            for (uint32_t i = fieldStart; i < fieldStart + fieldCount; i++)
            {
                if (img->fields[i].staticSlot != DN2CPP_BPI_REF_NONE)
                    continue;
                uint8_t kind = patch_field_kind(img, img->fields[i].typeRef);
                uint32_t width = (kind == kMarshalI32 || kind == kMarshalF32) ? 4u : 8u;
                size = (size + width - 1) & ~(width - 1);
                img->instFieldOffsets[i] = size;
                img->instFieldKinds[i] = kind;
                size += width;
            }
            size = (size + 7u) & ~7u;

            uint32_t nameLen;
            const char* name = pool_name(img, bt.nameOff, &nameLen);
            char* nameCopy = static_cast<char*>(dn2cpp_alloc_atomic(nameLen + 1));
            std::memcpy(nameCopy, name, nameLen);
            nameCopy[nameLen] = '\0';
            // dn2cpp_alloc is zero-initialized: every member this slice does
            // not wire (reflection tables, generic/enum metadata, …) reads as
            // the trailing-member null convention of hand-written type-infos.
            auto* iti = static_cast<InterpTypeInfo*>(dn2cpp_alloc(sizeof(InterpTypeInfo)));
            iti->img = img;
            iti->typeIdx = t;
            iti->magic = kInterpTypeInfoMagic;
            Dn2CppTypeInfo* ti = &iti->ti;
            ti->name = nameCopy;
            ti->base = base;
            ti->instanceSize = static_cast<int32_t>(size);
            ti->vtable = base->vtable;
            ti->interfaces = base->interfaces;
            ti->interfaceCount = base->interfaceCount;
            ti->tostring = base->tostring;
            ti->gethashcode = base->gethashcode;
            ti->equals = base->equals;
            ti->finalize = base->finalize;
            // Wire the interned Type before the ti is published (loader is
            // single-threaded), so GetType()/typeof on patch types take the
            // same lock-free typeObject path as AOT types.
            ti->typeObject = dn2cpp_get_type_from_handle_slow(ti);
            if (bt.vtableDescOff != DN2CPP_BPI_REF_NONE)
            {
                // Vtable overrides: copy the base vtable at the converter-baked
                // length (= the base type's slot count, which the ABI contract's
                // V lines freeze against the live base), then rewrite each
                // overridden slot to the N2M trampoline matching (slot, the
                // override's sigShape) in the base image's registration table.
                uint32_t vtableLen, overrideCount;
                const uint32_t* entries = vtdesc_at(img, bt.vtableDescOff, &vtableLen, &overrideCount);
                if (vtableLen == 0 || overrideCount == 0 || overrideCount > vtableLen)
                    interp_fail("BPI: malformed vtable description");
                if (base->vtable == nullptr)
                    interp_fail("BPI bind: vtable overrides on a base type without a vtable");
                auto** vt = static_cast<const void**>(
                    dn2cpp_alloc_atomic(vtableLen * sizeof(const void*)));
                std::memcpy(vt, base->vtable, vtableLen * sizeof(const void*));
                for (uint32_t e = 0; e < overrideCount; e++)
                {
                    uint32_t slot = entries[e * 2];
                    uint32_t methodIdx = entries[e * 2 + 1];
                    if (slot >= vtableLen)
                        interp_fail("BPI: vtable override slot out of bounds");
                    if (methodIdx >= img->methodCount
                        || img->methods[methodIdx].declTypeIdx != t
                        || img->methods[methodIdx].vtableSlot != slot)
                        interp_fail("BPI: vtable override method mismatch");
                    uint32_t shapeLen;
                    const char* shape =
                        pool_name(img, img->methods[methodIdx].sigShapeOff, &shapeLen);
                    const void* fn = nullptr;
                    for (int32_t r = 0; r < dn2cpp_n2m_trampoline_count && fn == nullptr; r++)
                    {
                        if (dn2cpp_n2m_trampolines[r].slot == static_cast<int32_t>(slot)
                            && name_equals(shape, shapeLen, dn2cpp_n2m_trampolines[r].sigShape))
                            fn = dn2cpp_n2m_trampolines[r].fn;
                    }
                    if (fn == nullptr)
                        interp_fail("BPI bind: no N2M trampoline for the overridden vtable slot "
                                    "(signature shape outside the bridged surface)");
                    vt[slot] = fn;
                }
                ti->vtable = vt;
            }
            // Interface implementations: a type with an ItfDesc run gets an
            // extended interface-dispatch map (base entries plus its own
            // interpreted implementations, each behind an N2M interface
            // trampoline), so AOT and interpreted interface calls on a patch
            // instance land on the interpreted body; a type without one keeps
            // sharing the base's interface map.
            if (bt.itfDescOff != DN2CPP_BPI_REF_NONE)
                build_patch_itf_map(img, ti, base, bt.itfDescOff);
            img->patchTypes[t] = ti;
        }
    }

    // SZArray type imports (aux0 bit0 on a Type import; aux1 = the element type's
    // EntityRef), bound AFTER patch-type construction so a patch-class element's
    // type-info exists. A registry hit from the first bind pass is the base
    // image's precise per-element array type-info (exact Type identity with
    // AOT-allocated arrays); otherwise one is constructed here in the same shape
    // the AOT lane emits (DN2CPP_TF_ARRAY|SEALED, elementType, rank 1, base-less),
    // so the shared isinst/castclass covariance arm and the array allocators treat
    // it like an emitted handle. Constructed ones stay per-image and unregistered
    // and carry no SZArray interface-dispatch map.
    for (uint32_t i = 0; i < img->importCount; i++)
    {
        const Dn2CppBpiImport& imp = img->imports[i];
        if (imp.kind != DN2CPP_BPI_IMPORT_TYPE || (imp.aux0 & 1u) == 0)
            continue;
        if (img->bindings[i].type != nullptr)
        {
            if ((img->bindings[i].type->flags & DN2CPP_TF_ARRAY) == 0)
                interp_fail("BPI bind: an array type name resolved to a non-array type");
            continue;
        }
        const Dn2CppTypeInfo* el = nullptr;
        switch (DN2CPP_BPI_REF_TAG(imp.aux1))
        {
            case DN2CPP_BPI_TAG_PRIM:
                switch (DN2CPP_BPI_REF_INDEX(imp.aux1))
                {
                    case kPrimBoolean: el = &dn2cpp_bool_type; break;
                    case kPrimChar: el = &dn2cpp_char_type; break;
                    case kPrimInt32: el = &dn2cpp_int32_type; break;
                    case kPrimInt64: el = &dn2cpp_int64_type; break;
                    case kPrimSingle: el = &dn2cpp_single_type; break;
                    case kPrimDouble: el = &dn2cpp_double_type; break;
                    case kPrimString: el = &dn2cpp_string_type; break;
                    default:
                        interp_fail("BPI bind: unsupported array element primitive");
                }
                break;
            case DN2CPP_BPI_TAG_IMPORT:
            {
                uint32_t ei = DN2CPP_BPI_REF_INDEX(imp.aux1);
                if (ei >= img->importCount)
                    interp_fail("BPI: array element import out of bounds");
                el = img->bindings[ei].type;
                if (el == nullptr)
                    interp_fail("BPI bind: unresolved array element type (not in the base image)");
                if ((el->flags & DN2CPP_TF_VALUETYPE) != 0)
                    interp_fail("BPI bind: value-type array elements need a later slice");
                break;
            }
            case DN2CPP_BPI_TAG_PATCH:
            {
                uint32_t ei = DN2CPP_BPI_REF_INDEX(imp.aux1);
                if (ei >= img->typeCount || img->patchTypes == nullptr
                    || img->patchTypes[ei] == nullptr)
                    interp_fail("BPI: array element patch type out of bounds");
                el = img->patchTypes[ei];
                break;
            }
            default:
                interp_fail("BPI: unsupported array element reference");
        }
        uint32_t nameLen;
        const char* name = pool_name(img, imp.nameOff, &nameLen);
        char* nameCopy = static_cast<char*>(dn2cpp_alloc_atomic(nameLen + 1));
        std::memcpy(nameCopy, name, nameLen);
        nameCopy[nameLen] = '\0';
        auto* ti = static_cast<Dn2CppTypeInfo*>(dn2cpp_alloc(sizeof(Dn2CppTypeInfo)));
        ti->name = nameCopy;
        ti->flags = DN2CPP_TF_ARRAY | DN2CPP_TF_SEALED;
        ti->elementType = el;
        ti->arrayRank = 1;
        ti->typeObject = dn2cpp_get_type_from_handle_slow(ti);
        img->bindings[i].type = ti;
    }

    // A register-format image gets one linear verification scan per method
    // before anything publishes: import binding and patch-type construction
    // are done, so call windows can read callee argCounts, and a failing
    // verify leaves the registry untouched (registration is below).
    if (img->regCode)
        verify_reg_code(img);

    // Interpreted frames join the shadow stack: one "DeclType.Method()" frame
    // name per MethodTable index, materialized eagerly (the loader is
    // single-threaded, so no synthesize-then-publish race exists, and a flag-off
    // base leaves frameNames null and pays nothing per interpreted call). Name
    // parity with the AOT guard is a DECLARED divergence — a NESTED patch type
    // renders its bare simple name, not "Ns.Outer+Inner", and there is no
    // " [shared generic]" mark because patch methods are never canonical bodies.
    // The pointer array is dn2cpp_alloc (scanned: hanging off the rooted image, it
    // is what keeps the name buffers alive); the buffers themselves are atomic.
    if (dn2cpp_shadow_stack_is_enabled() && img->methodCount > 0)
    {
        const char** names = static_cast<const char**>(
            dn2cpp_alloc(img->methodCount * sizeof(const char*)));
        for (uint32_t i = 0; i < img->methodCount; i++)
        {
            const Dn2CppBpiMethod& m = img->methods[i];
            if (m.declTypeIdx >= img->typeCount)
                interp_fail("BPI: method declaring-type index out of bounds");
            const char* typeName = img->patchTypes[m.declTypeIdx]->name;
            uint32_t nameLen;
            const char* name = pool_name(img, m.nameOff, &nameLen);
            size_t typeLen = std::strlen(typeName);
            char* buf = static_cast<char*>(dn2cpp_alloc_atomic(typeLen + 1 + nameLen + 3));
            std::memcpy(buf, typeName, typeLen);
            buf[typeLen] = '.';
            std::memcpy(buf + typeLen + 1, name, nameLen);
            std::memcpy(buf + typeLen + 1 + nameLen, "()", 3);
            names[i] = buf;
        }
        img->frameNames = names;
    }

    if (img->typeCount > 0)
    {
        // Registry visibility: every constructed patch type-info becomes
        // resolvable by CLR FullName — to later loads' bind passes, to
        // Type.GetType(string), and to reflection-lite surfaces. Only name
        // lookups need this; GetType()/isinst/castclass/catch matching read
        // object headers and walk base chains. The side-chain is newest-first, so
        // re-loading a same-named type makes the newest registration visible
        // while instances of older loads keep their original type-info.
        // Registration runs after the whole construction loop, so a load that
        // fails partway publishes nothing.
        for (uint32_t t = 0; t < img->typeCount; t++)
            dn2cpp_type_registry_add(img->patchTypes[t]->name, img->patchTypes[t]);
    }

    img->nextLoaded = g_loadedImages;
    g_loadedImages = img;
    return img;
}

namespace
{

// Frame limits. Frames live on the native stack (conservatively scanned by
// the collector, so reference slots are GC-visible); the depth guard keeps
// runaway patch recursion from tearing through the native stack.
enum : int32_t
{
    kMaxStack = 64,
    kMaxFrame = 64,
    kMaxEH = 32,
    kMaxCallDepth = 1000,
};

// Boxes one eval-stack slot for the invoker/accessor-thunk boundary (the
// thunks unbox scalar payloads at obj+1); references pass through unboxed.
Dn2CppObject* marshal_slot_to_object(const MarshalDesc& d, Slot v)
{
    switch (d.kind)
    {
        case kMarshalRef:
            return static_cast<Dn2CppObject*>(v.ref);
        case kMarshalI32:
        {
            int32_t x = static_cast<int32_t>(v.i);
            return dn2cpp_box(d.boxType, &x, sizeof(x));
        }
        case kMarshalI64:
            return dn2cpp_box(d.boxType, &v.i, sizeof(int64_t));
        case kMarshalF32:
        {
            float x = static_cast<float>(v.f);
            return dn2cpp_box(d.boxType, &x, sizeof(x));
        }
        case kMarshalF64:
            return dn2cpp_box(d.boxType, &v.f, sizeof(double));
        default:
            interp_fail("interp: bad marshal kind");
    }
}

// Unboxes an invoker/accessor result back into an eval-stack slot: scalar
// payloads read at obj+1 at the BOX payload width, which is the int32-promoted
// stack model (CppTypes.Of) and not the field's storage width — a boxed byte is
// 4 bytes, matching dn2cpp_byte_type.instanceSize, even though the field it was
// read out of is one. i32 is kept sign-extended and f32 widened per the slot
// invariants; references are the pointer itself.
Slot marshal_object_to_slot(const MarshalDesc& d, Dn2CppObject* o)
{
    Slot s{};
    if (d.kind == kMarshalRef)
    {
        s.ref = o;
        return s;
    }
    if (o == nullptr)
        interp_fail("interp: a scalar import result came back unboxed");
    const char* payload = reinterpret_cast<const char*>(o) + sizeof(Dn2CppObject);
    switch (d.kind)
    {
        case kMarshalI32:
        {
            int32_t x;
            std::memcpy(&x, payload, sizeof(x));
            s.i = x;
            break;
        }
        case kMarshalI64:
            std::memcpy(&s.i, payload, sizeof(int64_t));
            break;
        case kMarshalF32:
        {
            float x;
            std::memcpy(&x, payload, sizeof(x));
            s.f = x;
            break;
        }
        case kMarshalF64:
            std::memcpy(&s.f, payload, sizeof(double));
            break;
        default:
            interp_fail("interp: bad marshal kind");
    }
    return s;
}

// Resolves an Import-tagged instruction operand to its bound import, checked
// against the expected import kind.
const ImportBinding& import_at(const Dn2CppInterpImage* img, uint32_t operand, uint32_t wantKind)
{
    if (DN2CPP_BPI_REF_TAG(operand) != DN2CPP_BPI_TAG_IMPORT)
        interp_fail("interp: unsupported entity-reference tag");
    uint32_t idx = DN2CPP_BPI_REF_INDEX(operand);
    if (idx >= img->importCount)
        interp_fail("BPI: import index out of bounds");
    const ImportBinding& b = img->bindings[idx];
    if (b.kind != wantKind)
        interp_fail("interp: import kind mismatch");
    return b;
}

// Resolves a type-operand EntityRef (isinst/castclass) to a live type-info: an
// Import-tagged reference to its bound base-image type, a PatchEntity-tagged
// reference to the loader-constructed patch type-info. Both feed the shared
// dn2cpp_isinst/dn2cpp_castclass base-chain walk — a patch type-info's base
// chain is fully wired at load, so no interpreter-specific matching exists.
const Dn2CppTypeInfo* type_ref_at(const Dn2CppInterpImage* img, uint32_t ref)
{
    switch (DN2CPP_BPI_REF_TAG(ref))
    {
        case DN2CPP_BPI_TAG_IMPORT:
        {
            uint32_t idx = DN2CPP_BPI_REF_INDEX(ref);
            if (idx >= img->importCount || img->bindings[idx].kind != DN2CPP_BPI_IMPORT_TYPE)
                interp_fail("BPI: type reference import out of bounds");
            const Dn2CppTypeInfo* ti = img->bindings[idx].type;
            if (ti == nullptr)
                interp_fail("BPI bind: unresolved type import in a type test");
            return ti;
        }
        case DN2CPP_BPI_TAG_PATCH:
        {
            uint32_t idx = DN2CPP_BPI_REF_INDEX(ref);
            if (idx >= img->typeCount || img->patchTypes[idx] == nullptr)
                interp_fail("BPI: type reference patch index out of bounds");
            return img->patchTypes[idx];
        }
        default:
            interp_fail("interp: unsupported type-reference tag");
    }
}

// The invoker-thunk ABI shared by every emitted member (docs in dn2cpp.h at
// Dn2CppMethodInfo::invoker).
using InvokerFn = Dn2CppObject* (*)(void*, Dn2CppObject*, Dn2CppObject**, const Dn2CppTypeInfo*);

// Base-ctor chaining for the exception intercept (`base(message)` from an
// interpreted patch exception ctor): the receiver is the half-built exception
// object the patch newobj already allocated, so this must NOT allocate. Seed
// the Message / innerException the decoded shape carries onto the shared
// Dn2CppExceptionObject prefix, then — if the base ctor resolved to a real,
// non-opaque body — run it so its own field writes land. Only indices >= 0 are
// seeded, so a parameterless `base()` nulls nothing.
//
// Deliberately diverges from AOT in *where* it seeds: AOT seeds at the outer
// newobj positionally, this at the base ctor call, which is closer to real .NET
// for the shapes the positional rule misses. A null b.fnPtr (an opaque base, or
// a ctor the base build never reached) is a seed-only degrade.
//
// `self` is non-null by contract: both dispatch-loop call sites raise the
// catchable NullReferenceException on a null receiver first, and a new caller
// must guard the same way before the seed writes below dereference it.
static void exc_seed_and_run_base_ctor(const ImportBinding& b, Dn2CppObject* self,
    Dn2CppObject** callArgs)
{
    auto* e = reinterpret_cast<Dn2CppExceptionObject*>(self);
    if (b.excMsgArg >= 0)
        e->message = reinterpret_cast<Dn2CppString*>(callArgs[b.excMsgArg]);
    if (b.excInnerArg >= 0)
        e->inner = callArgs[b.excInnerArg];
    // Seed the base COR_E_EXCEPTION default before the base body runs, matching the
    // AOT newobj intercept: a derived ctor's own set_HResult (above this in the
    // chain) then overwrites it with the per-type value. The general newobj that
    // allocated `self` zero-fills, so without this a patch exception relying on the
    // base default would read HResult 0. (kShapeExceptionNew allocates via
    // dn2cpp_exception_new, which already seeds, and does not reach here.)
    e->hresult = static_cast<int32_t>(0x80131500);
    if (b.fnPtr != nullptr)
        reinterpret_cast<InvokerFn>(b.invoker)(b.fnPtr, self, callArgs, nullptr);
}

enum class CmpOp
{
    Eq,
    Ne,
    Lt,
    Le,
    Gt,
    Ge,
};

template <typename T>
bool cmp_num(CmpOp op, T x, T y)
{
    switch (op)
    {
        case CmpOp::Eq: return x == y;
        case CmpOp::Ne: return x != y;
        case CmpOp::Lt: return x < y;
        case CmpOp::Le: return x <= y;
        case CmpOp::Gt: return x > y;
        case CmpOp::Ge: return x >= y;
    }
    return false;
}

// Evaluates one comparison over the hinted operand kind. For floats, `un`
// means "unordered or" (ECMA-335 III.1.5) — NaN makes the .un forms true; for
// integers it selects the unsigned interpretation; for references only
// (in)equality exists (cgt.un doubles as the "!= null" pattern).
bool interp_cmp(uint32_t kind, CmpOp op, bool un, Slot a, Slot b)
{
    switch (kind)
    {
        case kKindI32:
            return un ? cmp_num(op, static_cast<uint32_t>(a.i), static_cast<uint32_t>(b.i))
                      : cmp_num(op, static_cast<int32_t>(a.i), static_cast<int32_t>(b.i));
        case kKindI64:
            return un ? cmp_num(op, static_cast<uint64_t>(a.i), static_cast<uint64_t>(b.i))
                      : cmp_num(op, a.i, b.i);
        case kKindF32:
        case kKindF64:
        {
            double x = a.f, y = b.f;
            if (!un)
                return cmp_num(op, x, y);
            switch (op)
            {
                case CmpOp::Eq: return x == y;
                case CmpOp::Ne: return !(x == y);
                case CmpOp::Lt: return !(x >= y);
                case CmpOp::Le: return !(x > y);
                case CmpOp::Gt: return !(x <= y);
                case CmpOp::Ge: return !(x < y);
            }
            return false;
        }
        case kKindRef:
            switch (op)
            {
                case CmpOp::Eq: return a.ref == b.ref;
                case CmpOp::Ne: return a.ref != b.ref;
                case CmpOp::Gt: // cgt.un on refs: the canonical "!= null" pattern
                    if (un)
                        return a.ref != b.ref;
                    break;
                default:
                    break;
            }
            interp_fail("interp: ordered comparison on references");
        default:
            interp_fail("interp: bad comparison kind hint");
    }
}

// Evaluates one binary arithmetic instruction (add..rem.un, and/or/xor) over
// the hinted operand kind. Integer add/sub/mul use unsigned arithmetic for
// well-defined wraparound; div/rem delegate the two trapping cases to the same
// dn2cpp_div_signed / dn2cpp_rem_signed / _unsigned guards the EMITTED code
// takes (dn2cpp_core.h), so the two execution modes cannot answer a fault
// differently.
//
// SHARE those guards rather than copying their arms: a copy answers `x % -1`
// with 0, where real .NET raises OverflowException for `int.MinValue % -1`
// exactly as it does for the division.
Slot interp_binary(uint16_t op, uint32_t kind, Slot a, Slot b)
{
    Slot s;
    switch (kind)
    {
        case kKindI32:
        {
            int32_t x = static_cast<int32_t>(a.i), y = static_cast<int32_t>(b.i);
            uint32_t ux = static_cast<uint32_t>(x), uy = static_cast<uint32_t>(y);
            int32_t r;
            switch (op)
            {
                case 0x58: r = static_cast<int32_t>(ux + uy); break; // add
                case 0x59: r = static_cast<int32_t>(ux - uy); break; // sub
                case 0x5A: r = static_cast<int32_t>(ux * uy); break; // mul
                case 0x5B: r = dn2cpp_div_signed<int32_t>(x, y); break;                      // div
                case 0x5C: r = static_cast<int32_t>(dn2cpp_div_unsigned<uint32_t>(ux, uy)); break; // div.un
                case 0x5D: r = dn2cpp_rem_signed<int32_t>(x, y); break;                      // rem
                case 0x5E: r = static_cast<int32_t>(dn2cpp_rem_unsigned<uint32_t>(ux, uy)); break; // rem.un
                case 0x5F: r = x & y; break; // and
                case 0x60: r = x | y; break; // or
                case 0x61: r = x ^ y; break; // xor
                default: interp_fail("interp: bad binary opcode");
            }
            s.i = r; // sign-extended: the i32 slot invariant
            return s;
        }
        case kKindI64:
        {
            int64_t x = a.i, y = b.i;
            uint64_t ux = static_cast<uint64_t>(x), uy = static_cast<uint64_t>(y);
            switch (op)
            {
                case 0x58: s.i = static_cast<int64_t>(ux + uy); break; // add
                case 0x59: s.i = static_cast<int64_t>(ux - uy); break; // sub
                case 0x5A: s.i = static_cast<int64_t>(ux * uy); break; // mul
                case 0x5B: s.i = dn2cpp_div_signed<int64_t>(x, y); break;                      // div
                case 0x5C: s.i = static_cast<int64_t>(dn2cpp_div_unsigned<uint64_t>(ux, uy)); break; // div.un
                case 0x5D: s.i = dn2cpp_rem_signed<int64_t>(x, y); break;                      // rem
                case 0x5E: s.i = static_cast<int64_t>(dn2cpp_rem_unsigned<uint64_t>(ux, uy)); break; // rem.un
                case 0x5F: s.i = x & y; break; // and
                case 0x60: s.i = x | y; break; // or
                case 0x61: s.i = x ^ y; break; // xor
                default: interp_fail("interp: bad binary opcode");
            }
            return s;
        }
        case kKindF32:
        {
            float x = static_cast<float>(a.f), y = static_cast<float>(b.f);
            float r;
            switch (op)
            {
                case 0x58: r = x + y; break;           // add
                case 0x59: r = x - y; break;           // sub
                case 0x5A: r = x * y; break;           // mul
                case 0x5B: r = x / y; break;           // div
                case 0x5D: r = std::fmod(x, y); break; // rem
                default: interp_fail("interp: bad float binary opcode");
            }
            s.f = r;
            return s;
        }
        case kKindF64:
        {
            double x = a.f, y = b.f;
            switch (op)
            {
                case 0x58: s.f = x + y; break;             // add
                case 0x59: s.f = x - y; break;             // sub
                case 0x5A: s.f = x * y; break;             // mul
                case 0x5B: s.f = x / y; break;             // div
                case 0x5D: s.f = std::fmod(x, y); break;   // rem
                default: interp_fail("interp: bad float binary opcode");
            }
            return s;
        }
        default:
            interp_fail("interp: bad arithmetic kind hint");
    }
}

// One executing patch-method frame: the pieces the dispatch loop and the EH
// machinery share. `frame`/`stack`/`handlerExc` live on interp_call's native
// stack (conservatively scanned, so reference slots — including a caught
// exception — stay GC-visible).
struct InterpFrame
{
    const Dn2CppInterpImage* img;
    const Dn2CppBpiMethod* m;
    const Dn2CppBpiInsn* code;
    const Dn2CppBpiEH* eh;     // the method's EHTable run (null when ehCount == 0)
    Slot* frame;
    Slot* stack;
    Dn2CppObject** handlerExc; // per-EH-record caught exception (the rethrow source)
    int32_t sp;
    int32_t depth;
};

// How one interp_run invocation completed: a method return, or a finally/
// fault handler reaching its endfinally/endfault.
enum class ExecKind
{
    Return,
    EndFinally,
};

struct ExecResult
{
    ExecKind kind;
    Slot value;
};

Slot interp_call(const Dn2CppInterpImage* img, uint32_t methodIdx, const Slot* args, int32_t depth);

// Runs a patch type's static constructor lazily, exactly once — the
// interpreter-side twin of the AOT __ensure wrappers, matching
// dn2cpp_cctor_run_once (dn2cpp_gc.cpp): the started flag is set BEFORE the body
// runs, so a re-entrant access sees the partially-initialized state without
// recursing; an initializer that throws propagates raw (no
// TypeInitializationException wrapping, as the AOT guard) and leaves the type
// UNinitialized, its failure recorded in the image's per-type cctorFailed slot
// (GC-scanned, so the exception outlives the throw) and re-raised on every later
// touch rather than reading half-assigned statics. dn2cpp_rethrow preserves the
// trace captured at the original throw. Checked before the first static-field
// access on a patch type and before the first call into one from outside. No
// thread coordination, unlike the AOT guard's mutex/CV arm: the interpreter is
// single-threaded.
void interp_ensure_cctor(const Dn2CppInterpImage* img, uint32_t typeIdx, int32_t depth)
{
    if (typeIdx >= img->typeCount)
        interp_fail("BPI: cctor-guard type index out of bounds");
    if (img->cctorStarted[typeIdx] == kCctorStarted)
        return;
    if (img->cctorStarted[typeIdx] == kCctorFailed)
    {
        // Initialization already failed: re-raise the recorded exception,
        // never re-run. A non-managed failure has no managed object to replay,
        // and answering with a *different* exception would be a lie about what
        // went wrong — fail loudly instead (the same arm as the AOT guard's
        // dn2cpp_cctor_replay_failure).
        if (Dn2CppObject* exc = img->cctorFailed[typeIdx]; exc != nullptr)
            dn2cpp_rethrow(exc);
        dn2cpp_fail("static constructor failed with a non-managed exception; the type is unusable");
    }
    img->cctorStarted[typeIdx] = kCctorStarted;
    const Dn2CppBpiType& t = img->types[typeIdx];
    if ((t.flags & DN2CPP_BPI_TF_HAS_CCTOR) == 0)
        return;
    try
    {
        interp_call(img, t.methodStart, nullptr, depth + 1);
    }
    catch (const Dn2CppException& e)
    {
        img->cctorFailed[typeIdx] = e.obj;
        img->cctorStarted[typeIdx] = kCctorFailed;
        throw;
    }
    catch (...)
    {
        img->cctorFailed[typeIdx] = nullptr;
        img->cctorStarted[typeIdx] = kCctorFailed;
        throw;
    }
}

// Resolves a PatchEntity static-field operand to its storage slot, running
// the declaring type's lazy-cctor guard first (`a` = the field reference,
// `b` = the declaring TypeTable index, baked by the converter).
Slot* patch_static_addr(const Dn2CppInterpImage* img, uint32_t a, uint32_t b, int32_t depth)
{
    uint32_t idx = DN2CPP_BPI_REF_INDEX(a);
    if (idx >= img->fieldCount)
        interp_fail("BPI: patch field index out of bounds");
    uint32_t slot = img->fields[idx].staticSlot;
    if (slot == DN2CPP_BPI_REF_NONE || slot >= img->staticCount)
        interp_fail("BPI: patch field has no static slot");
    interp_ensure_cctor(img, b, depth);
    return &img->statics[slot];
}

// (receiver patch type, vtable slot) -> the overriding patch MethodTable
// index, walking the receiver's patch base chain within the image: a type
// that does not re-override the slot inherits the override its nearest patch
// ancestor declared (its shared/copied vtable already points at the same
// trampoline). Returns DN2CPP_BPI_REF_NONE when no type on the chain
// overrides the slot. Allocation-free: TypeRecords and VtableDesc runs are
// read from the blob in place.
uint32_t resolve_override_method(const Dn2CppInterpImage* img, uint32_t typeIdx, uint32_t slot)
{
    for (uint32_t cur = typeIdx;;)
    {
        if (cur >= img->typeCount)
            interp_fail("BPI: override lookup type index out of bounds");
        const Dn2CppBpiType& bt = img->types[cur];
        if (bt.vtableDescOff != DN2CPP_BPI_REF_NONE)
        {
            uint32_t vtableLen, overrideCount;
            const uint32_t* entries = vtdesc_at(img, bt.vtableDescOff, &vtableLen, &overrideCount);
            for (uint32_t e = 0; e < overrideCount; e++)
            {
                if (entries[e * 2] == slot)
                    return entries[e * 2 + 1];
            }
        }
        if (bt.baseRef == DN2CPP_BPI_REF_NONE
            || DN2CPP_BPI_REF_TAG(bt.baseRef) != DN2CPP_BPI_TAG_PATCH)
            return DN2CPP_BPI_REF_NONE;
        cur = DN2CPP_BPI_REF_INDEX(bt.baseRef);
    }
}

// (receiver patch type, interface, interface slot) -> the implementing patch
// MethodTable index, walking the receiver's patch base chain through the
// ItfDesc runs (deepest first, like resolve_override_method). Returns
// DN2CPP_BPI_REF_NONE when no type on the chain implements the slot with an
// interpreted method. Allocation-free: ItfDesc runs are read in place.
uint32_t resolve_itf_override(const Dn2CppInterpImage* img, uint32_t typeIdx,
    const Dn2CppTypeInfo* itf, uint32_t slot)
{
    for (uint32_t cur = typeIdx;;)
    {
        if (cur >= img->typeCount)
            interp_fail("BPI: interface override lookup type index out of bounds");
        const Dn2CppBpiType& bt = img->types[cur];
        if (bt.itfDescOff != DN2CPP_BPI_REF_NONE)
        {
            uint32_t itfCount;
            const uint8_t* p = itfdesc_at(img, bt.itfDescOff, &itfCount);
            const uint8_t* end = img->itfdesc + img->itfdescLen;
            for (uint32_t e = 0; e < itfCount; e++)
            {
                if (p + 12 > end)
                    interp_fail("BPI: interface description entry out of bounds");
                uint32_t itfImport, fullSlotCount, implCount;
                std::memcpy(&itfImport, p, 4);
                std::memcpy(&fullSlotCount, p + 4, 4);
                std::memcpy(&implCount, p + 8, 4);
                p += 12;
                const Dn2CppTypeInfo* et = nullptr;
                if (DN2CPP_BPI_REF_TAG(itfImport) == DN2CPP_BPI_TAG_IMPORT)
                {
                    uint32_t ii = DN2CPP_BPI_REF_INDEX(itfImport);
                    if (ii < img->importCount)
                        et = img->bindings[ii].type;
                }
                if (p + static_cast<uint64_t>(implCount) * 8 > end)
                    interp_fail("BPI: interface description slots out of bounds");
                for (uint32_t s = 0; s < implCount; s++)
                {
                    uint32_t sl, mi;
                    std::memcpy(&sl, p, 4);
                    std::memcpy(&mi, p + 4, 4);
                    p += 8;
                    if (et == itf && sl == slot)
                        return mi;
                }
            }
        }
        if (bt.baseRef == DN2CPP_BPI_REF_NONE
            || DN2CPP_BPI_REF_TAG(bt.baseRef) != DN2CPP_BPI_TAG_PATCH)
            return DN2CPP_BPI_REF_NONE;
        cur = DN2CPP_BPI_REF_INDEX(bt.baseRef);
    }
}

// Whether a thrown object matches a catch clause: resolve the clause's bound
// type import and walk the exception's type-info base chain (dn2cpp_isinst).
// catch (Exception) / catch (object) are catch-all — the same root-type rule
// the AOT catch dispatch applies, since runtime-trapped exceptions carry the
// flat runtime exception handles.
bool eh_catch_matches(const Dn2CppInterpImage* img, uint32_t catchTypeRef, Dn2CppObject* exc)
{
    if (catchTypeRef == DN2CPP_BPI_REF_NONE)
        return true;
    if (DN2CPP_BPI_REF_TAG(catchTypeRef) != DN2CPP_BPI_TAG_IMPORT)
        interp_fail("interp: unsupported catch-type reference");
    uint32_t idx = DN2CPP_BPI_REF_INDEX(catchTypeRef);
    if (idx >= img->importCount)
        interp_fail("BPI: catch type import out of bounds");
    const Dn2CppTypeInfo* ti = img->bindings[idx].type;
    if (ti == nullptr)
        interp_fail("BPI bind: unresolved catch type import");
    if (ti == &dn2cpp_exception_type || ti == &dn2cpp_object_type)
        return true;
    return dn2cpp_isinst(exc, ti) != nullptr;
}

// Runs the frame's instructions from `pc` until the method returns or — when
// entered at a finally/fault handler — the handler's endfinally. Managed
// exceptions ride the runtime's C++ exception (dn2cpp_throw / Dn2CppException)
// through interpreted and AOT frames alike; this frame's EH records are
// scanned innermost-first (metadata order) around the dispatch loop.
ExecResult interp_run(InterpFrame& f, uint32_t pc)
{
    const Dn2CppInterpImage* img = f.img;
    const Dn2CppBpiMethod& m = *f.m;
    const Dn2CppBpiInsn* code = f.code;
    Slot* frame = f.frame;
    Slot* stack = f.stack;
    int32_t& sp = f.sp;
    int32_t depth = f.depth;

    auto push = [&](Slot v) {
        if (sp == kMaxStack)
            interp_fail("interp: eval stack overflow");
        stack[sp++] = v;
    };
    auto pop = [&]() -> Slot {
        if (sp == 0)
            interp_fail("interp: eval stack underflow");
        return stack[--sp];
    };
    auto branch_target = [&](uint32_t t) -> uint32_t {
        if (t >= m.codeInsnCount)
            interp_fail("BPI: branch target out of bounds");
        return t;
    };

    // A pending leave. Its finally handlers must run OUTSIDE the dispatch
    // loop's try: an exception one of them raises has already been dispatched
    // against every record of this frame that can see the handler (by the
    // nested interp_run below), so it must unwind out of the frame directly
    // rather than re-enter this frame's catch scan against the stale leave pc.
    bool haveLeave = false;
    uint32_t leaveFrom = 0;
    uint32_t leaveTarget = 0;

    for (;;)
    {
        if (haveLeave)
        {
            haveLeave = false;
            // Run the finally handler of every region the leave exits,
            // innermost first (fault handlers run only on the exceptional
            // path). The handler executes in this frame and reports back
            // through endfinally; any other completion abandons the leave
            // (a handler-raised exception that an enclosing catch consumed
            // took control of the frame), matching .NET semantics.
            for (uint16_t i = 0; i < m.ehCount; i++)
            {
                const Dn2CppBpiEH& r = f.eh[i];
                if (r.kind != 2)
                    continue;
                if (!(r.tryStart <= leaveFrom && leaveFrom < r.tryEnd))
                    continue;
                if (r.tryStart <= leaveTarget && leaveTarget < r.tryEnd)
                    continue;
                sp = 0;
                ExecResult fin = interp_run(f, r.handlerStart);
                if (fin.kind != ExecKind::EndFinally)
                    return fin;
            }
            sp = 0;
            pc = leaveTarget;
        }
        try
        {
            while (pc < m.codeInsnCount)
            {
                const Dn2CppBpiInsn& insn = code[pc];
                uint32_t next = pc + 1;
                switch (insn.op)
                {
                    case 0x00: // nop
                        break;
                    case 0x20: // ldc.i4 (short forms normalized by the converter)
                    {
                        Slot v;
                        v.i = static_cast<int32_t>(insn.a);
                        push(v);
                        break;
                    }
                    case 0x21: // ldc.i8 (a/b = lo/hi)
                    {
                        Slot v;
                        v.i = static_cast<int64_t>(
                            static_cast<uint64_t>(insn.a) | (static_cast<uint64_t>(insn.b) << 32));
                        push(v);
                        break;
                    }
                    case 0x22: // ldc.r4 (a = float bit pattern; widened into the slot)
                    {
                        float fv;
                        uint32_t bits = insn.a;
                        std::memcpy(&fv, &bits, 4);
                        Slot v;
                        v.f = fv;
                        push(v);
                        break;
                    }
                    case 0x23: // ldc.r8 (a/b = lo/hi of the double bit pattern)
                    {
                        uint64_t bits = static_cast<uint64_t>(insn.a) | (static_cast<uint64_t>(insn.b) << 32);
                        double d;
                        std::memcpy(&d, &bits, 8);
                        Slot v;
                        v.f = d;
                        push(v);
                        break;
                    }
                    case 0xFE09: // ldarg
                        if (insn.a >= m.argCount)
                            interp_fail("BPI: argument slot out of bounds");
                        push(frame[insn.a]);
                        break;
                    case 0xFE0B: // starg
                        if (insn.a >= m.argCount)
                            interp_fail("BPI: argument slot out of bounds");
                        frame[insn.a] = pop();
                        break;
                    case 0xFE0C: // ldloc
                        if (insn.a >= m.localCount)
                            interp_fail("BPI: local slot out of bounds");
                        push(frame[m.argCount + insn.a]);
                        break;
                    case 0xFE0E: // stloc
                        if (insn.a >= m.localCount)
                            interp_fail("BPI: local slot out of bounds");
                        frame[m.argCount + insn.a] = pop();
                        break;
                    case 0x25: // dup
                    {
                        Slot v = pop();
                        push(v);
                        push(v);
                        break;
                    }
                    case 0x26: // pop
                        pop();
                        break;
                    case 0x14: // ldnull
                    {
                        Slot v{};
                        v.ref = nullptr;
                        push(v);
                        break;
                    }
                    case 0x72: // ldstr — a Dn2CppString aliasing the blob (zero-copy)
                    {
                        // Widened and pointer-tested for pool_name's reason: the
                        // header read's bound is image data, and in uint32_t
                        // `insn.a + 4` wraps at 0xFFFFFFFC. The length read below
                        // was already widened; this one was not.
                        if (img->userStrings == nullptr
                            || static_cast<uint64_t>(insn.a) + 4 > img->userStringsLen)
                            interp_fail("BPI: user string out of bounds");
                        uint32_t charLen;
                        std::memcpy(&charLen, img->userStrings + insn.a, 4);
                        if (insn.a + 4 + static_cast<uint64_t>(charLen) * 2 > img->userStringsLen)
                            interp_fail("BPI: user string out of bounds");
                        Slot v;
                        v.ref = dn2cpp_string_literal(
                            reinterpret_cast<const char16_t*>(img->userStrings + insn.a + 4),
                            static_cast<int32_t>(charLen));
                        push(v);
                        break;
                    }
                    case 0x38: // br
                        next = branch_target(insn.a);
                        break;
                    case 0x39: // brfalse (b = operand-kind hint)
                    case 0x3A: // brtrue
                    {
                        Slot v = pop();
                        bool truthy;
                        switch (insn.b)
                        {
                            case kKindI32: truthy = static_cast<int32_t>(v.i) != 0; break;
                            case kKindI64: truthy = v.i != 0; break;
                            case kKindRef: truthy = v.ref != nullptr; break;
                            default: interp_fail("interp: bad brtrue/brfalse kind hint");
                        }
                        if (truthy == (insn.op == 0x3A))
                            next = branch_target(insn.a);
                        break;
                    }
                    case 0x3B: // beq (a = target, b = operand-kind hint)
                    case 0x3C: // bge
                    case 0x3D: // bgt
                    case 0x3E: // ble
                    case 0x3F: // blt
                    case 0x40: // bne.un
                    case 0x41: // bge.un
                    case 0x42: // bgt.un
                    case 0x43: // ble.un
                    case 0x44: // blt.un
                    {
                        Slot b = pop();
                        Slot a = pop();
                        CmpOp op;
                        bool un = false;
                        switch (insn.op)
                        {
                            case 0x3B: op = CmpOp::Eq; break;
                            case 0x3C: op = CmpOp::Ge; break;
                            case 0x3D: op = CmpOp::Gt; break;
                            case 0x3E: op = CmpOp::Le; break;
                            case 0x3F: op = CmpOp::Lt; break;
                            case 0x40: op = CmpOp::Ne; un = true; break;
                            case 0x41: op = CmpOp::Ge; un = true; break;
                            case 0x42: op = CmpOp::Gt; un = true; break;
                            case 0x43: op = CmpOp::Le; un = true; break;
                            default:   op = CmpOp::Lt; un = true; break;
                        }
                        if (interp_cmp(insn.b, op, un, a, b))
                            next = branch_target(insn.a);
                        break;
                    }
                    case 0xFE01: // ceq (a = operand-kind hint; result is an i4 0/1)
                    case 0xFE02: // cgt
                    case 0xFE03: // cgt.un
                    case 0xFE04: // clt
                    case 0xFE05: // clt.un
                    {
                        Slot b = pop();
                        Slot a = pop();
                        CmpOp op;
                        bool un = false;
                        switch (insn.op)
                        {
                            case 0xFE01: op = CmpOp::Eq; break;
                            case 0xFE02: op = CmpOp::Gt; break;
                            case 0xFE03: op = CmpOp::Gt; un = true; break;
                            case 0xFE04: op = CmpOp::Lt; break;
                            default:     op = CmpOp::Lt; un = true; break;
                        }
                        Slot r;
                        r.i = interp_cmp(insn.a, op, un, a, b) ? 1 : 0;
                        push(r);
                        break;
                    }
                    case 0x58: // add    (a = operand-kind hint)
                    case 0x59: // sub
                    case 0x5A: // mul
                    case 0x5B: // div
                    case 0x5C: // div.un
                    case 0x5D: // rem
                    case 0x5E: // rem.un
                    case 0x5F: // and
                    case 0x60: // or
                    case 0x61: // xor
                    {
                        Slot b = pop();
                        Slot a = pop();
                        push(interp_binary(insn.op, insn.a, a, b));
                        break;
                    }
                    case 0x62: // shl (a = value operand-kind hint; amount masked to width)
                    case 0x63: // shr
                    case 0x64: // shr.un
                    {
                        Slot amount = pop();
                        Slot value = pop();
                        int32_t sh = static_cast<int32_t>(amount.i);
                        Slot r;
                        if (insn.a == kKindI64)
                        {
                            sh &= 63;
                            if (insn.op == 0x62)
                                r.i = static_cast<int64_t>(static_cast<uint64_t>(value.i) << sh);
                            else if (insn.op == 0x63)
                                r.i = value.i >> sh;
                            else
                                r.i = static_cast<int64_t>(static_cast<uint64_t>(value.i) >> sh);
                        }
                        else
                        {
                            sh &= 31;
                            int32_t v = static_cast<int32_t>(value.i);
                            if (insn.op == 0x62)
                                r.i = static_cast<int32_t>(static_cast<uint32_t>(v) << sh);
                            else if (insn.op == 0x63)
                                r.i = v >> sh;
                            else
                                r.i = static_cast<int32_t>(static_cast<uint32_t>(v) >> sh);
                        }
                        push(r);
                        break;
                    }
                    case 0x65: // neg (a = operand-kind hint)
                    {
                        Slot v = pop();
                        switch (insn.a)
                        {
                            case kKindI32: v.i = static_cast<int32_t>(0u - static_cast<uint32_t>(v.i)); break;
                            case kKindI64: v.i = static_cast<int64_t>(0ull - static_cast<uint64_t>(v.i)); break;
                            case kKindF32: v.f = -static_cast<float>(v.f); break;
                            case kKindF64: v.f = -v.f; break;
                            default: interp_fail("interp: bad neg kind hint");
                        }
                        push(v);
                        break;
                    }
                    case 0x66: // not (a = operand-kind hint)
                    {
                        Slot v = pop();
                        switch (insn.a)
                        {
                            case kKindI32: v.i = static_cast<int32_t>(~static_cast<uint32_t>(v.i)); break;
                            case kKindI64: v.i = ~v.i; break;
                            default: interp_fail("interp: bad not kind hint");
                        }
                        push(v);
                        break;
                    }
                    case 0x69: // conv.i4 (a = source operand-kind hint)
                    case 0x6D: // conv.u4
                    {
                        Slot v = pop();
                        int64_t raw;
                        switch (insn.a)
                        {
                            case kKindI32:
                            case kKindI64: raw = v.i; break;
                            case kKindF32:
                            case kKindF64:
                                raw = insn.op == 0x69 ? static_cast<int64_t>(static_cast<int32_t>(v.f))
                                                      : static_cast<int64_t>(static_cast<uint32_t>(v.f));
                                break;
                            default: interp_fail("interp: bad conv kind hint");
                        }
                        v.i = static_cast<int32_t>(raw); // truncate, then keep sign-extended
                        push(v);
                        break;
                    }
                    case 0x6A: // conv.i8 (sign-extends a 32-bit source)
                    {
                        Slot v = pop();
                        switch (insn.a)
                        {
                            case kKindI32:
                            case kKindI64: break; // i32 slots are already sign-extended
                            case kKindF32:
                            case kKindF64: v.i = static_cast<int64_t>(v.f); break;
                            default: interp_fail("interp: bad conv kind hint");
                        }
                        push(v);
                        break;
                    }
                    case 0x6E: // conv.u8 (zero-extends a 32-bit source)
                    {
                        Slot v = pop();
                        switch (insn.a)
                        {
                            case kKindI32: v.i = static_cast<int64_t>(static_cast<uint32_t>(v.i)); break;
                            case kKindI64: break;
                            case kKindF32:
                            case kKindF64: v.i = static_cast<int64_t>(static_cast<uint64_t>(v.f)); break;
                            default: interp_fail("interp: bad conv kind hint");
                        }
                        push(v);
                        break;
                    }
                    case 0x6B: // conv.r4 (rounds to float32, kept widened in the slot)
                    {
                        Slot v = pop();
                        switch (insn.a)
                        {
                            case kKindI32: v.f = static_cast<float>(static_cast<int32_t>(v.i)); break;
                            case kKindI64: v.f = static_cast<float>(v.i); break;
                            case kKindF32:
                            case kKindF64: v.f = static_cast<float>(v.f); break;
                            default: interp_fail("interp: bad conv kind hint");
                        }
                        push(v);
                        break;
                    }
                    case 0x6C: // conv.r8
                    {
                        Slot v = pop();
                        switch (insn.a)
                        {
                            case kKindI32: v.f = static_cast<double>(static_cast<int32_t>(v.i)); break;
                            case kKindI64: v.f = static_cast<double>(v.i); break;
                            case kKindF32:
                            case kKindF64: break; // f32 slots are already widened
                            default: interp_fail("interp: bad conv kind hint");
                        }
                        push(v);
                        break;
                    }
                    case 0xFE06: // ldftn — push a delegate-method closure for the
                                 // following delegate newobj. A PatchEntity operand
                                 // is a baked patch method (image + index; the
                                 // receiver is captured at newobj); an Import operand
                                 // is a base-image instance method (its AOT pointer
                                 // becomes the delegate's f_method directly).
                    {
                        auto* c = static_cast<Dn2CppInterpDgFtn*>(
                            dn2cpp_alloc(sizeof(Dn2CppInterpDgFtn)));
                        c->magic = kDgFtnMagic;
                        c->boundThis = nullptr;
                        if (DN2CPP_BPI_REF_TAG(insn.a) == DN2CPP_BPI_TAG_PATCH)
                        {
                            uint32_t idx = DN2CPP_BPI_REF_INDEX(insn.a);
                            if (idx >= img->methodCount)
                                interp_fail("BPI: ldftn patch method index out of bounds");
                            c->kind = kDgFtnPatch;
                            c->img = img;
                            c->methodIdx = idx;
                        }
                        else
                        {
                            // One transcription for both ldftn arms: it carries
                            // the import's declared receiver forward to the
                            // delegate newobj, where the receiver exists.
                            dg_ftn_bind_aot(c,
                                import_at(img, insn.a, DN2CPP_BPI_IMPORT_METHOD));
                        }
                        Slot v{};
                        v.ref = c;
                        push(v);
                        break;
                    }
                    case 0x28: // call — intra-patch (PatchEntity) or through a pre-bound import
                    case 0x6F: // callvirt — a pre-bound import, or a PatchEntity target
                               // when the bound patch method is virtual (an override):
                               // the receiver may be a patch-derived type, so the slot
                               // re-resolves against the receiver's live patch type. A
                               // callvirt on a NON-virtual patch method canonicalizes
                               // to call at bake time (exact dispatch either way).
                    {
                        uint32_t tag = DN2CPP_BPI_REF_TAG(insn.a);
                        if (tag == DN2CPP_BPI_TAG_PATCH)
                        {
                            uint32_t idx = DN2CPP_BPI_REF_INDEX(insn.a);
                            if (idx >= img->methodCount)
                                interp_fail("BPI: call method index out of bounds");
                            int32_t n = static_cast<int32_t>(img->methods[idx].argCount);
                            if (sp < n)
                                interp_fail("interp: eval stack underflow at call");
                            // &stack[sp - n] holds the arguments in parameter order;
                            // the callee copies them into its frame before this
                            // frame's stack is touched again. b bit1 = instance:
                            // arg0 is the receiver, null-checked here (the C#
                            // callvirt contract).
                            //
                            // Every null guard in the two dispatch loops raises
                            // the catchable NullReferenceException rather than a
                            // dn2cpp_fail abort, so an interpreted try/catch
                            // around a null dereference behaves as on real .NET.
                            // The throw lands in the dispatch loop's own catch
                            // below, which discards the half-consumed eval stack
                            // (sp = 0) before running this frame's handlers.
                            sp -= n;
                            // The instance bit OR the opcode: a callvirt has a
                            // receiver by definition, and the virtual-dispatch
                            // arm below reads self->type unconditionally, so
                            // leaving the guard to a bit the image carries makes
                            // an image that clears it a segfault rather than a
                            // throw. The converter always sets it, so this costs
                            // nothing on a well-formed image.
                            if (((insn.b & 2) || insn.op == 0x6F) && stack[sp].ref == nullptr)
                                dn2cpp_throw_null_reference();
                            uint32_t target = idx;
                            if (insn.op == 0x6F)
                            {
                                // Interpreted virtual dispatch: the receiver of a
                                // virtual patch method is a patch instance (its
                                // static type is a patch class), so its type-info
                                // is an extended interp record; the baked method's
                                // frozen slot re-resolves down the receiver's
                                // patch chain, landing on a re-override when the
                                // receiver is a derived patch type.
                                auto* self = static_cast<Dn2CppObject*>(stack[sp].ref);
                                const auto* iti = reinterpret_cast<const InterpTypeInfo*>(self->type);
                                if (self->type == nullptr || iti->magic != kInterpTypeInfoMagic
                                    || iti->img != img)
                                    interp_fail("interp: virtual patch dispatch on a non-patch receiver");
                                if (img->methods[idx].vtableSlot == 0xFFFF)
                                    interp_fail("interp: callvirt target occupies no vtable slot");
                                target = resolve_override_method(img, iti->typeIdx,
                                    img->methods[idx].vtableSlot);
                                if (target == DN2CPP_BPI_REF_NONE || target >= img->methodCount
                                    || img->methods[target].argCount != static_cast<uint32_t>(n))
                                    interp_fail("interp: vtable slot has no interpreted override");
                            }
                            // The first call into a patch type from outside it
                            // runs the callee type's static constructor first
                            // (lazy, exactly once).
                            // The receiver's TYPE, which only the virtual arm
                            // above states (magic stamp plus a re-resolve down
                            // the receiver's own chain). The body a call enters
                            // reads fields at the DECLARING type's
                            // loader-assigned offsets: off the end of a shorter
                            // object that is an out-of-range write, not a
                            // misread. Asked under the same condition as the
                            // null test, so a static call's arg0 is never
                            // mistaken for a receiver.
                            if (((insn.b & 2) || insn.op == 0x6F)
                                && !patch_receiver_ok(img, target,
                                    static_cast<Dn2CppObject*>(stack[sp].ref)))
                                interp_fail(kPatchRecvFail);
                            if (img->methods[target].declTypeIdx != m.declTypeIdx)
                                interp_ensure_cctor(img, img->methods[target].declTypeIdx, depth);
                            Slot r = interp_call(img, target, &stack[sp], depth + 1);
                            if (insn.b & 1)
                                push(r);
                            break;
                        }
                        const ImportBinding& b = import_at(img, insn.a, DN2CPP_BPI_IMPORT_METHOD);
                        if (sp < static_cast<int32_t>(b.argCount + (b.isInstance ? 1u : 0u)))
                            interp_fail("interp: eval stack underflow at call");
                        switch (b.callShape)
                        {
                            case kShapeVoidStr:
                            {
                                // The import bound by NAME and the helper behind
                                // it casts the operand raw, so test the declared
                                // parameter type before handing it over.
                                auto* a0 = static_cast<Dn2CppObject*>(pop().ref);
                                if (!intrinsic_arg_ok(b.argKinds[0], a0))
                                    interp_fail(kIntrinsicArgFail);
                                reinterpret_cast<void (*)(Dn2CppString*)>(b.fnPtr)(
                                    reinterpret_cast<Dn2CppString*>(a0));
                                break;
                            }
                            case kShapeVoidEmpty:
                                reinterpret_cast<void (*)()>(b.fnPtr)();
                                break;
                            case kShapeVoidI4:
                            case kShapeVoidBool:
                                reinterpret_cast<void (*)(int32_t)>(b.fnPtr)(
                                    static_cast<int32_t>(pop().i));
                                break;
                            case kShapeVoidI8:
                                reinterpret_cast<void (*)(int64_t)>(b.fnPtr)(pop().i);
                                break;
                            case kShapeVoidR8:
                                reinterpret_cast<void (*)(double)>(b.fnPtr)(pop().f);
                                break;
                            case kShapeRefRetObj: // instance intrinsic (e.g. Exception.get_Message)
                            {
                                auto* self = static_cast<Dn2CppObject*>(pop().ref);
                                if (self == nullptr)
                                    dn2cpp_throw_null_reference();
                                // The import bound by NAME and the helper behind
                                // it casts the receiver raw, so test the
                                // declared type before handing it over.
                                if (!intrinsic_receiver_ok(b.recvKind, self))
                                    interp_fail("interp: intrinsic call receiver is not an instance of the import's declared type");
                                Slot v{};
                                v.ref = reinterpret_cast<Dn2CppObject* (*)(Dn2CppObject*)>(b.fnPtr)(self);
                                if (insn.b & 1)
                                    push(v);
                                break;
                            }
                            case kShapeRefRetStr: // static intrinsic (e.g. Type.GetType)
                            {
                                // The shape where the kinds earn their per-row
                                // storage: this one C++ signature serves both
                                // Type.GetType(string) and
                                // String.Concat(string[]).
                                auto* a0 = static_cast<Dn2CppObject*>(pop().ref);
                                if (!intrinsic_arg_ok(b.argKinds[0], a0))
                                    interp_fail(kIntrinsicArgFail);
                                Slot v{};
                                v.ref = reinterpret_cast<Dn2CppObject* (*)(Dn2CppString*)>(b.fnPtr)(
                                    reinterpret_cast<Dn2CppString*>(a0));
                                if (insn.b & 1)
                                    push(v);
                                break;
                            }
                            case kShapeRefRetRefN: // static intrinsic (e.g. String.Concat)
                            {
                                Dn2CppObject* refArgs[kMaxImportArgs];
                                for (uint32_t j = b.argCount; j-- > 0;)
                                    refArgs[j] = static_cast<Dn2CppObject*>(pop().ref);
                                // Per position: nothing about the shape makes
                                // the parameters homogeneous.
                                for (uint32_t j = 0; j < b.argCount; j++)
                                    if (!intrinsic_arg_ok(b.argKinds[j], refArgs[j]))
                                        interp_fail(kIntrinsicArgFail);
                                Slot v{};
                                v.ref = reinterpret_cast<Dn2CppObject* (*)(Dn2CppObject* const*)>(
                                    b.fnPtr)(refArgs);
                                if (insn.b & 1)
                                    push(v);
                                break;
                            }
                            case kShapeDelegateInvoke:
                            {
                                // Invoke on a delegate: the delegate receiver sits
                                // below the invoke arguments. Hand it plus the raw
                                // argument slots to the pre-emitted M2N invoke
                                // bridge, which unpacks the slots into the delegate's
                                // Invoke C++ ABI and dispatches through its emitted
                                // invoker (the multicast chain, each f_method landing
                                // on an interpreted thunk or an AOT method).
                                int32_t n = static_cast<int32_t>(b.argCount);
                                if (sp < n + 1)
                                    interp_fail("interp: eval stack underflow at delegate invoke");
                                sp -= n;
                                const Slot* callArgs = &stack[sp];
                                auto* dg = static_cast<Dn2CppObject*>(stack[sp - 1].ref);
                                sp -= 1;
                                if (dg == nullptr)
                                    dn2cpp_throw_null_reference();
                                // The bridge reads prev/method/target off this
                                // object as a Dn2CppDelegate and CALLS `method`.
                                // The import bound by name, so test that the
                                // receiver really is the delegate type the
                                // binding names.
                                if (!import_receiver_ok(b, dg))
                                    interp_fail(kImportRecvFail);
                                using DgBridge = Dn2CppInterpSlot (*)(
                                    Dn2CppObject*, const Dn2CppInterpSlot*, int32_t);
                                Slot r = reinterpret_cast<DgBridge>(b.fnPtr)(dg, callArgs, n);
                                if (insn.b & 1)
                                    push(r);
                                break;
                            }
                            case kShapeInvoker:
                            {
                                // The generic path: scalars box into the invoker
                                // thunk's Dn2CppObject** args (it unboxes payloads at
                                // its precise C++ widths), references pass through. A
                                // callvirt on a virtual method fetches the target from
                                // the live receiver's vtable — the same slot the AOT
                                // call site would dispatch; call (and a non-virtual
                                // callvirt) uses the bound fnPtr directly. A ctor
                                // import reached by `call` is the base-ctor chain
                                // of an inheriting patch constructor — an ordinary
                                // instance call on the half-built receiver.
                                if (b.isCtor && insn.op == 0x6F)
                                    interp_fail("interp: a constructor import is not a callvirt target");
                                Dn2CppObject* callArgs[kMaxImportArgs];
                                for (uint32_t j = b.argCount; j-- > 0;)
                                    callArgs[j] = marshal_slot_to_object(b.args[j], pop());
                                Dn2CppObject* self = nullptr;
                                void* fn = b.fnPtr;
                                if (b.isInstance)
                                {
                                    self = static_cast<Dn2CppObject*>(pop().ref);
                                    if (self == nullptr)
                                        dn2cpp_throw_null_reference();
                                    // The vtable index below came from the
                                    // DECLARED type, and a receiver that is not
                                    // one carries no length for it: an
                                    // out-of-range read, then a call through
                                    // whatever it held. The non-virtual path is
                                    // milder and still wrong: the bound body
                                    // reads fields at the declared offsets.
                                    if (!import_receiver_ok(b, self))
                                        interp_fail(kImportRecvFail);
                                    if (b.isInterface)
                                    {
                                        // Interface dispatch: resolve the slot fn
                                        // from the receiver's live interface map
                                        // (an AOT implementation, or an N2M
                                        // interface trampoline into an interpreted
                                        // one). resolve_interface fails loudly if
                                        // the receiver does not implement it.
                                        if (self->type == nullptr)
                                            interp_fail("interp: interface dispatch on a typeless receiver");
                                        const void** slots =
                                            dn2cpp_resolve_interface(self->type, b.type);
                                        fn = const_cast<void*>(slots[b.vtableSlot]);
                                        if (fn == nullptr)
                                            interp_fail("interp: interface slot is empty");
                                    }
                                    else if (insn.op == 0x6F && b.vtableSlot >= 0)
                                    {
                                        if (self->type == nullptr || self->type->vtable == nullptr)
                                            interp_fail("interp: receiver type has no vtable");
                                        fn = const_cast<void*>(self->type->vtable[b.vtableSlot]);
                                        if (fn == nullptr)
                                            interp_fail("interp: receiver vtable slot is empty");
                                    }
                                }
                                Dn2CppObject* r =
                                    reinterpret_cast<InvokerFn>(b.invoker)(fn, self, callArgs, b.retType);
                                if (insn.b & 1)
                                    push(marshal_object_to_slot(b.ret, r));
                                break;
                            }
                            case kShapeExceptionNew:
                            {
                                // `base(message)` from an interpreted patch exception
                                // ctor: an ordinary instance call on the half-built
                                // receiver the patch newobj already allocated — do NOT
                                // allocate. Marshal the args (the receiver sits below
                                // them), seed the prefix, and run the resolved base
                                // ctor. See exc_seed_and_run_base_ctor.
                                if (insn.op == 0x6F)
                                    interp_fail("interp: a constructor import is not a callvirt target");
                                Dn2CppObject* callArgs[kMaxImportArgs];
                                for (uint32_t j = b.argCount; j-- > 0;)
                                    callArgs[j] = marshal_slot_to_object(b.args[j], pop());
                                auto* self = static_cast<Dn2CppObject*>(pop().ref);
                                if (self == nullptr)
                                    dn2cpp_throw_null_reference();
                                // The seed writes message/inner/hresult at the
                                // uniform exception prefix. On an object that is
                                // not one, those words are whatever the type
                                // does hold, or past its end.
                                if (!import_receiver_ok(b, self))
                                    interp_fail(kImportRecvFail);
                                exc_seed_and_run_base_ctor(b, self, callArgs);
                                break;
                            }
                            default:
                                interp_fail("interp: unbound call import");
                        }
                        break;
                    }
                    case 0x73: // newobj (a = ctor import or a patch `.ctor` method
                               // index) — allocate from the bound/constructed
                               // type-info exactly like the AOT newobj path (zeroed
                               // GC allocation, header type stamp, finalizer
                               // registration before the ctor), then run the ctor:
                               // through its invoker thunk for an import, as an
                               // interpreted frame with the fresh object in the
                               // `this` slot for a patch type.
                    {
                        if (DN2CPP_BPI_REF_TAG(insn.a) == DN2CPP_BPI_TAG_PATCH)
                        {
                            uint32_t idx = DN2CPP_BPI_REF_INDEX(insn.a);
                            if (idx >= img->methodCount)
                                interp_fail("BPI: newobj method index out of bounds");
                            const Dn2CppBpiMethod& cm = img->methods[idx];
                            if (cm.argCount == 0 || cm.argCount > static_cast<uint32_t>(kMaxFrame))
                                interp_fail("BPI: patch newobj target has an invalid frame shape");
                            uint32_t t = cm.declTypeIdx;
                            if (t >= img->typeCount || img->patchTypes[t] == nullptr)
                                interp_fail("BPI: patch newobj declaring type out of bounds");
                            int32_t nParams = static_cast<int32_t>(cm.argCount) - 1;
                            if (sp < nParams)
                                interp_fail("interp: eval stack underflow at newobj");
                            // Creating the first instance counts as a call into
                            // the type from outside: the lazy cctor runs first.
                            interp_ensure_cctor(img, t, depth);
                            const Dn2CppTypeInfo* ti = img->patchTypes[t];
                            size_t sz = ti->instanceSize > static_cast<int32_t>(sizeof(Dn2CppObject))
                                ? static_cast<size_t>(ti->instanceSize)
                                : sizeof(Dn2CppObject);
                            // External allocation hook (dn2cpp.h): an embedding
                            // layer may back the fresh instance with a host
                            // object (e.g. the Godot layer constructs the engine
                            // object a Node-derived patch instance is bound to)
                            // — before the ctor chain, exactly like the AOT
                            // `new` path. A null return declines: default GC
                            // allocation. Finalizer registration stays here.
                            Dn2CppObject* obj = dn2cpp_interp_alloc_hook != nullptr
                                ? dn2cpp_interp_alloc_hook(ti, sz)
                                : nullptr;
                            if (obj == nullptr)
                            {
                                obj = static_cast<Dn2CppObject*>(dn2cpp_alloc(sz));
                                obj->type = ti;
                            }
                            if (ti->finalize != nullptr) // inherited from the AOT base
                                dn2cpp_register_finalizer(obj);
                            Slot ctorArgs[kMaxFrame];
                            ctorArgs[0].ref = obj;
                            sp -= nParams;
                            for (int32_t j = 0; j < nParams; j++)
                                ctorArgs[1 + j] = stack[sp + j];
                            interp_call(img, idx, ctorArgs, depth + 1);
                            Slot v{};
                            v.ref = obj;
                            push(v);
                            break;
                        }
                        // A Type import operand is a delegate construction (the ctor
                        // is a base-image delegate's `.ctor(object, native int)`): pop
                        // the method closure `ldftn` built and the target below it, and
                        // build a real Dn2CppDelegate over the uniform delegate layout.
                        // A patch-method closure captures the receiver and takes the
                        // delegate's pre-emitted thunk as f_method (so Invoke lands on
                        // the interpreter); a base-image-method closure yields the
                        // natural AOT delegate (f_method = the method pointer).
                        if (DN2CPP_BPI_REF_TAG(insn.a) == DN2CPP_BPI_TAG_IMPORT)
                        {
                            uint32_t ti = DN2CPP_BPI_REF_INDEX(insn.a);
                            if (ti < img->importCount
                                && img->bindings[ti].kind == DN2CPP_BPI_IMPORT_TYPE)
                            {
                                const Dn2CppTypeInfo* dti = img->bindings[ti].type;
                                if (dti == nullptr || (dti->flags & DN2CPP_TF_DELEGATE) == 0)
                                    interp_fail("BPI bind: delegate newobj type import is not a delegate");
                                if (sp < 2)
                                    interp_fail("interp: eval stack underflow at delegate newobj");
                                Slot ftnSlot = pop();
                                Slot targetSlot = pop();
                                auto* c = static_cast<Dn2CppInterpDgFtn*>(ftnSlot.ref);
                                if (c == nullptr || c->magic != kDgFtnMagic)
                                    interp_fail("interp: delegate newobj without a method reference (ldftn)");
                                auto* dg = static_cast<Dn2CppDelegate*>(
                                    dn2cpp_alloc(sizeof(Dn2CppDelegate)));
                                dg->type = dti;
                                dg->prev = nullptr;
                                if (c->kind == kDgFtnPatch)
                                {
                                    // Refuse a null 'this' when the patch method
                                    // is an INSTANCE method, at construction as
                                    // real .NET does. Instance-ness is derived
                                    // from the delegate's Invoke arity; a table
                                    // that cannot answer degrades.
                                    check_patch_delegate_null_this(img, c->methodIdx, dti,
                                        static_cast<Dn2CppObject*>(targetSlot.ref));
                                    c->boundThis = static_cast<Dn2CppObject*>(targetSlot.ref);
                                    const void* thunk = find_dg_thunk(dti);
                                    if (thunk == nullptr)
                                        interp_fail("BPI bind: no delegate thunk (Invoke signature outside the bridged surface)");
                                    dg->target = reinterpret_cast<Dn2CppObject*>(c);
                                    dg->method = const_cast<void*>(thunk);
                                }
                                else
                                {
                                    // Unconditional, where the arm above is not:
                                    // a null captured receiver is legitimate for
                                    // a STATIC patch method, so that arm derives
                                    // instance-ness from the Invoke arity.
                                    // kDgFtnAot is always a bound INSTANCE
                                    // method (the ldftn arm rejects static AOT
                                    // targets), so a null target can mean
                                    // nothing else. Refuse at construction, like
                                    // real .NET.
                                    if (targetSlot.ref == nullptr)
                                        throw_delegate_null_this();
                                    // The capture is the only point at which the
                                    // receiver of an AOT delegate's method is
                                    // visible to the interpreter: Invoke
                                    // re-enters through the delegate's own thunk,
                                    // so a bad `this` bound here reaches the
                                    // method with nothing in between.
                                    if (!dg_aot_target_ok(c,
                                            static_cast<Dn2CppObject*>(targetSlot.ref)))
                                        interp_fail(kImportRecvFail);
                                    // And the rest of the call: the pointer is
                                    // invoked through the delegate's own Invoke
                                    // ABI, so a target whose real signature is a
                                    // different one is called with registers the
                                    // caller never set.
                                    if (!dg_aot_signature_ok(c, dti))
                                        interp_fail(kDgSigFail);
                                    dg->target = static_cast<Dn2CppObject*>(targetSlot.ref);
                                    dg->method = c->aotFn;
                                }
                                Slot v{};
                                v.ref = dg;
                                push(v);
                                break;
                            }
                        }
                        const ImportBinding& b = import_at(img, insn.a, DN2CPP_BPI_IMPORT_METHOD);
                        if (sp < static_cast<int32_t>(b.argCount))
                            interp_fail("interp: eval stack underflow at newobj");
                        if (b.callShape == kShapeExceptionNew)
                        {
                            // The exception-ctor intercept bound in place of the real
                            // ctor: marshal the args (ref marshalling is identity, so
                            // the Message / innerException the recognized shapes carry
                            // read straight off callArgs by index), allocate the
                            // runtime's uniform exception object stamped with the
                            // derived type's own type-info and seeded with those, then
                            // — if the ctor resolved to a real, non-opaque body — run
                            // it so a derived exception's own field writes land. This
                            // is exactly the AOT newobj order (seed the prefix, then
                            // DirectCall the ctor).
                            Dn2CppObject* callArgs[kMaxImportArgs];
                            for (uint32_t j = b.argCount; j-- > 0;)
                                callArgs[j] = marshal_slot_to_object(b.args[j], pop());
                            Dn2CppString* message = b.excMsgArg >= 0
                                ? reinterpret_cast<Dn2CppString*>(callArgs[b.excMsgArg]) : nullptr;
                            Dn2CppObject* inner = b.excInnerArg >= 0
                                ? callArgs[b.excInnerArg] : nullptr;
                            Dn2CppObject* obj = dn2cpp_exception_new(b.type, message, inner);
                            if (b.fnPtr != nullptr)
                                reinterpret_cast<InvokerFn>(b.invoker)(b.fnPtr, obj, callArgs, nullptr);
                            Slot v{};
                            v.ref = obj;
                            push(v);
                            break;
                        }
                        if (!b.isCtor || b.callShape != kShapeInvoker)
                            interp_fail("interp: newobj target is not a bound constructor import");
                        Dn2CppObject* callArgs[kMaxImportArgs];
                        for (uint32_t j = b.argCount; j-- > 0;)
                            callArgs[j] = marshal_slot_to_object(b.args[j], pop());
                        const Dn2CppTypeInfo* ti = b.type;
                        size_t sz = ti->instanceSize > static_cast<int32_t>(sizeof(Dn2CppObject))
                            ? static_cast<size_t>(ti->instanceSize)
                            : sizeof(Dn2CppObject);
                        auto* obj = static_cast<Dn2CppObject*>(dn2cpp_alloc(sz));
                        obj->type = ti;
                        if (ti->finalize != nullptr)
                            dn2cpp_register_finalizer(obj);
                        reinterpret_cast<InvokerFn>(b.invoker)(b.fnPtr, obj, callArgs, nullptr);
                        Slot v{};
                        v.ref = obj;
                        push(v);
                        break;
                    }
                    case 0x7B: // ldfld  (a = field import / patch field)
                    case 0x7E: // ldsfld
                    {
                        // A PatchEntity operand is a patch-declared field. A
                        // static is a plain Slot copy from the image's static
                        // storage, behind the declaring type's cctor guard; an
                        // instance field reads at its loader-assigned offset.
                        if (DN2CPP_BPI_REF_TAG(insn.a) == DN2CPP_BPI_TAG_PATCH)
                        {
                            if (insn.op == 0x7E)
                            {
                                push(*patch_static_addr(img, insn.a, insn.b, depth));
                                break;
                            }
                            uint32_t idx = DN2CPP_BPI_REF_INDEX(insn.a);
                            if (idx >= img->fieldCount)
                                interp_fail("BPI: patch field index out of bounds");
                            if (img->fields[idx].staticSlot != DN2CPP_BPI_REF_NONE)
                                interp_fail("interp: ldfld on a static patch field");
                            auto* obj = static_cast<Dn2CppObject*>(pop().ref);
                            if (obj == nullptr)
                                dn2cpp_throw_null_reference();
                            const char* p = reinterpret_cast<const char*>(obj)
                                + img->instFieldOffsets[idx];
                            Slot v{};
                            switch (img->instFieldKinds[idx])
                            {
                                case kMarshalI32:
                                {
                                    int32_t x;
                                    std::memcpy(&x, p, sizeof(x));
                                    v.i = x; // sign-extended: the i32 slot invariant
                                    break;
                                }
                                case kMarshalI64:
                                    std::memcpy(&v.i, p, sizeof(int64_t));
                                    break;
                                case kMarshalF32:
                                {
                                    float x;
                                    std::memcpy(&x, p, sizeof(x));
                                    v.f = x; // widened: the f32 slot invariant
                                    break;
                                }
                                case kMarshalF64:
                                    std::memcpy(&v.f, p, sizeof(double));
                                    break;
                                case kMarshalRef:
                                    std::memcpy(&v.ref, p, sizeof(void*));
                                    break;
                                default:
                                    interp_fail("interp: bad patch field kind");
                            }
                            push(v);
                            break;
                        }
                        const ImportBinding& b = import_at(img, insn.a, DN2CPP_BPI_IMPORT_FIELD);
                        const Dn2CppFieldInfo* fi = b.field;
                        bool isStatic = insn.op == 0x7E;
                        if (((fi->attrs & DN2CPP_FLDA_STATIC) != 0) != isStatic)
                            interp_fail("interp: field import staticness mismatch");
                        Dn2CppObject* obj = nullptr;
                        if (!isStatic)
                        {
                            obj = static_cast<Dn2CppObject*>(pop().ref);
                            if (obj == nullptr)
                                dn2cpp_throw_null_reference();
                        }
                        if (fi->getter == nullptr)
                            interp_fail("interp: field has no reflectable storage");
                        push(marshal_object_to_slot(b.fieldVal, fi->getter(obj)));
                        break;
                    }
                    case 0x7D: // stfld  (a = field import / patch field)
                    case 0x80: // stsfld
                    {
                        if (DN2CPP_BPI_REF_TAG(insn.a) == DN2CPP_BPI_TAG_PATCH)
                        {
                            if (insn.op == 0x80)
                            {
                                Slot* addr = patch_static_addr(img, insn.a, insn.b, depth);
                                *addr = pop();
                                break;
                            }
                            uint32_t idx = DN2CPP_BPI_REF_INDEX(insn.a);
                            if (idx >= img->fieldCount)
                                interp_fail("BPI: patch field index out of bounds");
                            if (img->fields[idx].staticSlot != DN2CPP_BPI_REF_NONE)
                                interp_fail("interp: stfld on a static patch field");
                            Slot v = pop();
                            auto* obj = static_cast<Dn2CppObject*>(pop().ref);
                            if (obj == nullptr)
                                dn2cpp_throw_null_reference();
                            char* p = reinterpret_cast<char*>(obj) + img->instFieldOffsets[idx];
                            switch (img->instFieldKinds[idx])
                            {
                                case kMarshalI32:
                                {
                                    int32_t x = static_cast<int32_t>(v.i);
                                    std::memcpy(p, &x, sizeof(x));
                                    break;
                                }
                                case kMarshalI64:
                                    std::memcpy(p, &v.i, sizeof(int64_t));
                                    break;
                                case kMarshalF32:
                                {
                                    float x = static_cast<float>(v.f);
                                    std::memcpy(p, &x, sizeof(x));
                                    break;
                                }
                                case kMarshalF64:
                                    std::memcpy(p, &v.f, sizeof(double));
                                    break;
                                case kMarshalRef:
                                    std::memcpy(p, &v.ref, sizeof(void*));
                                    break;
                                default:
                                    interp_fail("interp: bad patch field kind");
                            }
                            break;
                        }
                        const ImportBinding& b = import_at(img, insn.a, DN2CPP_BPI_IMPORT_FIELD);
                        const Dn2CppFieldInfo* fi = b.field;
                        bool isStatic = insn.op == 0x80;
                        if (((fi->attrs & DN2CPP_FLDA_STATIC) != 0) != isStatic)
                            interp_fail("interp: field import staticness mismatch");
                        Slot v = pop();
                        Dn2CppObject* obj = nullptr;
                        if (!isStatic)
                        {
                            obj = static_cast<Dn2CppObject*>(pop().ref);
                            if (obj == nullptr)
                                dn2cpp_throw_null_reference();
                        }
                        if (fi->setter == nullptr)
                            interp_fail("interp: field has no reflectable storage");
                        fi->setter(obj, marshal_slot_to_object(b.fieldVal, v));
                        break;
                    }
                    case 0x74: // castclass (a = type EntityRef — import or patch type)
                    case 0x75: // isinst
                    {
                        // The shared runtime type tests: both read the live
                        // object header and walk the base chain, so patch
                        // instances (whose headers carry loader-constructed
                        // type-infos with fully wired bases) and AOT instances
                        // resolve on one path. A failed castclass raises the
                        // runtime's catchable InvalidCastException.
                        const Dn2CppTypeInfo* ti = type_ref_at(img, insn.a);
                        Slot v = pop();
                        auto* obj = static_cast<Dn2CppObject*>(v.ref);
                        v.ref = insn.op == 0x75 ? dn2cpp_isinst(obj, ti)
                                                : dn2cpp_castclass(obj, ti);
                        push(v);
                        break;
                    }
                    case 0x8D: // newarr (a = SZArray type EntityRef, b = element
                               // storage kind) — allocate through the AOT array
                               // allocators (the same three layouts AOT code
                               // uses, headed by the bound array type-info), so
                               // the array passes into AOT methods unconverted
                               // and GetType()/covariance see a real array type.
                    {
                        const Dn2CppTypeInfo* ti = type_ref_at(img, insn.a);
                        if ((ti->flags & DN2CPP_TF_ARRAY) == 0)
                            interp_fail("BPI: newarr type reference is not an array type");
                        int32_t len = static_cast<int32_t>(pop().i);
                        if (len < 0)
                            dn2cpp_overflow(); // catchable, the .NET newarr semantics
                        Slot v{};
                        switch (insn.b)
                        {
                            case kElemI4:
                                v.ref = dn2cpp_newarr_i4_t(len, ti);
                                break;
                            case kElemRef:
                                v.ref = dn2cpp_newarr_ref_t(len, ti);
                                break;
                            case kElemI1:
                            case kElemU1:
                                v.ref = dn2cpp_newarr_n_t(len, 1, ti);
                                break;
                            case kElemI2:
                            case kElemU2:
                                v.ref = dn2cpp_newarr_n_t(len, 2, ti);
                                break;
                            case kElemR4:
                                v.ref = dn2cpp_newarr_n_t(len, 4, ti);
                                break;
                            case kElemI8:
                            case kElemR8:
                                v.ref = dn2cpp_newarr_n_t(len, 8, ti);
                                break;
                            default:
                                interp_fail("interp: bad newarr element kind");
                        }
                        push(v);
                        break;
                    }
                    case 0x8E: // ldlen
                    {
                        auto* arr = static_cast<Dn2CppArray*>(pop().ref);
                        if (arr == nullptr)
                            dn2cpp_throw_null_reference();
                        Slot v{};
                        v.i = arr->length;
                        push(v);
                        break;
                    }
                    case 0xA3: // ldelem (a = element storage kind; typed short
                               // forms are canonicalized at bake time)
                    case 0xA4: // stelem — a reference-element store skips the
                               // covariance check, matching the AOT stelem
                               // helpers (a documented carve-out vs .NET's
                               // ArrayTypeMismatchException)
                    {
                        Slot val{};
                        if (insn.op == 0xA4)
                            val = pop();
                        int32_t idx = static_cast<int32_t>(pop().i);
                        auto* arr = static_cast<Dn2CppArray*>(pop().ref);
                        if (arr == nullptr)
                            dn2cpp_throw_null_reference();
                        if (idx < 0 || idx >= arr->length)
                            dn2cpp_throw_index_out_of_range(); // catchable, like the AOT ThrowHelper trap
                        // Element access at the baked storage kind, converting
                        // to/from the slot invariants (i32 sign-extended, f32
                        // widened) — the same layouts the AOT lane reads and
                        // writes, so arrays flow across the boundary intact.
                        // The packed layouts double-check the element width
                        // against the live header (BPI/receiver mismatch).
                        Slot v{};
                        switch (insn.a)
                        {
                            case kElemI4:
                            {
                                auto* a4 = static_cast<Dn2CppArrayI4*>(arr);
                                if (insn.op == 0xA4)
                                    a4->data[idx] = static_cast<int32_t>(val.i);
                                else
                                {
                                    v.i = a4->data[idx];
                                    push(v);
                                }
                                break;
                            }
                            case kElemRef:
                            {
                                auto* ar = static_cast<Dn2CppArrayRef*>(arr);
                                if (insn.op == 0xA4)
                                    ar->data[idx] = static_cast<Dn2CppObject*>(val.ref);
                                else
                                {
                                    v.ref = ar->data[idx];
                                    push(v);
                                }
                                break;
                            }
                            default:
                            {
                                uint32_t w = (insn.a == kElemI1 || insn.a == kElemU1) ? 1u
                                    : (insn.a == kElemI2 || insn.a == kElemU2)        ? 2u
                                    : (insn.a == kElemR4)                             ? 4u
                                    : (insn.a == kElemI8 || insn.a == kElemR8)        ? 8u
                                                                                      : 0u;
                                if (w == 0)
                                    interp_fail("interp: bad array element kind");
                                auto* an = static_cast<Dn2CppArrayN*>(arr);
                                if (an->elemSize != static_cast<int32_t>(w))
                                    interp_fail("interp: array element width mismatch");
                                char* p = an->data + static_cast<size_t>(idx) * w;
                                if (insn.op == 0xA4)
                                {
                                    switch (insn.a)
                                    {
                                        case kElemI1:
                                        case kElemU1:
                                        {
                                            uint8_t x = static_cast<uint8_t>(val.i);
                                            std::memcpy(p, &x, 1);
                                            break;
                                        }
                                        case kElemI2:
                                        case kElemU2:
                                        {
                                            uint16_t x = static_cast<uint16_t>(val.i);
                                            std::memcpy(p, &x, 2);
                                            break;
                                        }
                                        case kElemR4:
                                        {
                                            float x = static_cast<float>(val.f);
                                            std::memcpy(p, &x, 4);
                                            break;
                                        }
                                        case kElemI8:
                                            std::memcpy(p, &val.i, 8);
                                            break;
                                        default: // kElemR8
                                            std::memcpy(p, &val.f, 8);
                                            break;
                                    }
                                }
                                else
                                {
                                    switch (insn.a)
                                    {
                                        case kElemI1:
                                        {
                                            int8_t x;
                                            std::memcpy(&x, p, 1);
                                            v.i = x; // sign-extended
                                            break;
                                        }
                                        case kElemU1:
                                        {
                                            uint8_t x;
                                            std::memcpy(&x, p, 1);
                                            v.i = x; // zero-extended
                                            break;
                                        }
                                        case kElemI2:
                                        {
                                            int16_t x;
                                            std::memcpy(&x, p, 2);
                                            v.i = x;
                                            break;
                                        }
                                        case kElemU2:
                                        {
                                            uint16_t x;
                                            std::memcpy(&x, p, 2);
                                            v.i = x;
                                            break;
                                        }
                                        case kElemR4:
                                        {
                                            float x;
                                            std::memcpy(&x, p, 4);
                                            v.f = x; // widened: the f32 slot invariant
                                            break;
                                        }
                                        case kElemI8:
                                            std::memcpy(&v.i, p, 8);
                                            break;
                                        default: // kElemR8
                                            std::memcpy(&v.f, p, 8);
                                            break;
                                    }
                                    push(v);
                                }
                                break;
                            }
                        }
                        break;
                    }
                    case 0x2A: // ret (a = 1 when returning a value)
                        if (insn.a & 1)
                            return { ExecKind::Return, pop() };
                        return { ExecKind::Return, Slot{} };
                    case 0xDD: // leave (a = target instruction index; empties the eval
                               // stack, then routes through the finallys it exits)
                        sp = 0;
                        leaveFrom = pc;
                        leaveTarget = branch_target(insn.a);
                        haveLeave = true;
                        break;
                    case 0xDC: // endfinally / endfault — hand control back to whoever
                               // ran this handler (a leave routing or the unwinder)
                        return { ExecKind::EndFinally, Slot{} };
                    case 0x7A: // throw — the runtime's own unwinding machine
                        dn2cpp_throw(static_cast<Dn2CppObject*>(pop().ref));
                    case 0xFE1A: // rethrow (a = the enclosing catch's method-relative
                                 // EH record index, baked by the converter)
                    {
                        if (insn.a >= m.ehCount)
                            interp_fail("BPI: rethrow record index out of bounds");
                        Dn2CppObject* exc = f.handlerExc[insn.a];
                        if (exc == nullptr)
                            interp_fail("interp: rethrow with no exception in flight");
                        // dn2cpp_rethrow, not dn2cpp_throw: IL rethrow preserves
                        // the original stack trace (AOT's bare C++ `throw;` gets
                        // that for free; this re-raise goes through the runtime).
                        dn2cpp_rethrow(exc);
                    }
                    default:
                        interp_fail("interp: unsupported opcode (outside the interpreted surface)");
                }
                if (haveLeave)
                    break;
                pc = next;
            }
            if (!haveLeave)
                interp_fail("BPI: method body fell off the end (missing ret)");
        }
        catch (Dn2CppException& ex)
        {
            // The two-pass EH walk fused over the innermost-first record
            // order: every finally/fault whose protected range contains the
            // faulting pc and that sits inner to the matching catch runs
            // before the catch handler gets control; with no matching catch
            // in this frame every enclosing finally/fault runs and the
            // exception continues out through the same C++ throw — an outer
            // interpreter frame or an AOT try around dn2cpp_interp_exec picks
            // it up, one unwinding machine end to end.
            Dn2CppObject* exc = ex.obj;
            int32_t match = -1;
            for (uint16_t i = 0; i < m.ehCount && match < 0; i++)
            {
                const Dn2CppBpiEH& r = f.eh[i];
                if (!(r.tryStart <= pc && pc < r.tryEnd))
                    continue;
                switch (r.kind)
                {
                    case 0: // catch
                        if (eh_catch_matches(img, r.catchTypeRef, exc))
                            match = static_cast<int32_t>(i);
                        break;
                    case 2: // finally
                    case 3: // fault
                    {
                        sp = 0;
                        ExecResult fin = interp_run(f, r.handlerStart);
                        if (fin.kind != ExecKind::EndFinally)
                            return fin;
                        break;
                    }
                    default:
                        interp_fail("interp: filter clauses are outside the interpreted surface");
                }
            }
            if (match < 0)
                throw;
            // The object now lives in this frame's conservatively scanned
            // arrays, so the in-flight root can drop; rethrow re-raises
            // through dn2cpp_throw, which pushes it again.
            dn2cpp_exc_inflight_pop(exc);
            f.handlerExc[match] = exc;
            sp = 0;
            Slot v{};
            v.ref = exc;
            stack[sp++] = v;
            pc = f.eh[match].handlerStart;
        }
    }
}

// The register-format arm generators (interp_run_reg only). They evaluate
// through the v1 helpers (interp_binary / interp_cmp) with the operand kind and
// signedness promoted from the opcode, so the div/rem guards, wraparound, f32
// widening and unordered float semantics are shared with the stack loop rather
// than re-implemented.
#define DN2CPP_REG_BIN(name, ilop, kind) \
    case name: \
        regs[r0] = interp_binary(ilop, kind, regs[r1], regs[r2]); \
        break;
#define DN2CPP_REG_CMP(name, kind, cmpop, un) \
    case name: \
        regs[r0].i = interp_cmp(kind, cmpop, un, regs[r1], regs[r2]) ? 1 : 0; \
        break;
#define DN2CPP_REG_BRCMP(name, kind, cmpop, un) \
    case name: \
        if (interp_cmp(kind, cmpop, un, regs[r0], regs[r1])) \
            next = branch_target(insn.a); \
        break;

// The twin of interp_run for images with Header.flags bit0 (docs/BPI-FORMAT.md
// "Register code format"), selected per image by interp_call. The outer
// structure mirrors interp_run; the difference is the frame model: f.frame is
// one flat register file — regs[0..argCount) = args, [..slotCount) = locals,
// regs[slotCount + d] = the eval temp at depth d — and f.stack/f.sp are unused.
// Register operands ride the record's pfx field (low byte = r0, high byte = r1;
// a C3 opcode's r2 in a's low byte). verify_reg_code has already validated every
// opcode, register operand, branch target and call window at load, so the arms
// trust the registers.
ExecResult interp_run_reg(InterpFrame& f, uint32_t pc)
{
    const Dn2CppInterpImage* img = f.img;
    const Dn2CppBpiMethod& m = *f.m;
    const Dn2CppBpiInsn* code = f.code;
    Slot* regs = f.frame;
    int32_t depth = f.depth;
    const uint32_t tempBase = m.argCount + m.localCount;

    auto branch_target = [&](uint32_t t) -> uint32_t {
        if (t >= m.codeInsnCount)
            interp_fail("BPI: branch target out of bounds");
        return t;
    };

    // A pending leave, routed outside the dispatch loop's try for the same
    // reason as in interp_run. No eval stack exists to empty: temps above the
    // target's depth are dead by construction.
    bool haveLeave = false;
    uint32_t leaveFrom = 0;
    uint32_t leaveTarget = 0;

    for (;;)
    {
        if (haveLeave)
        {
            haveLeave = false;
            for (uint16_t i = 0; i < m.ehCount; i++)
            {
                const Dn2CppBpiEH& r = f.eh[i];
                if (r.kind != 2)
                    continue;
                if (!(r.tryStart <= leaveFrom && leaveFrom < r.tryEnd))
                    continue;
                if (r.tryStart <= leaveTarget && leaveTarget < r.tryEnd)
                    continue;
                ExecResult fin = interp_run_reg(f, r.handlerStart);
                if (fin.kind != ExecKind::EndFinally)
                    return fin;
            }
            pc = leaveTarget;
        }
        try
        {
            while (pc < m.codeInsnCount)
            {
                const Dn2CppBpiInsn& insn = code[pc];
                uint32_t next = pc + 1;
                // Register operands (a register image stores them in the pfx
                // field); r2 is meaningful for C3 opcodes only.
                const uint32_t r0 = insn.pfx & 0xFFu;
                const uint32_t r1 = insn.pfx >> 8;
                const uint32_t r2 = insn.a & 0xFFu;
                switch (insn.op)
                {
                    case R_MOV: // the one data-movement op: a whole-slot copy
                        regs[r0] = regs[r1];
                        break;
                    case R_LDNULL:
                    {
                        Slot v{};
                        v.ref = nullptr;
                        regs[r0] = v;
                        break;
                    }
                    case R_LDC_I32: // sign-extended into the slot
                        regs[r0].i = static_cast<int32_t>(insn.a);
                        break;
                    case R_LDC_I64: // a/b = lo/hi
                        regs[r0].i = static_cast<int64_t>(
                            static_cast<uint64_t>(insn.a) | (static_cast<uint64_t>(insn.b) << 32));
                        break;
                    case R_LDC_R4: // a = float bit pattern; widened into the slot
                    {
                        float fv;
                        uint32_t bits = insn.a;
                        std::memcpy(&fv, &bits, 4);
                        regs[r0].f = fv;
                        break;
                    }
                    case R_LDC_R8: // a/b = lo/hi of the double bit pattern
                    {
                        uint64_t bits = static_cast<uint64_t>(insn.a) | (static_cast<uint64_t>(insn.b) << 32);
                        double d;
                        std::memcpy(&d, &bits, 8);
                        regs[r0].f = d;
                        break;
                    }
                    case R_LDSTR: // a Dn2CppString aliasing the blob (zero-copy)
                    {
                        // As the v1 arm: widened and pointer-tested, because the
                        // uint32_t form of this compare wraps at 0xFFFFFFFC.
                        if (img->userStrings == nullptr
                            || static_cast<uint64_t>(insn.a) + 4 > img->userStringsLen)
                            interp_fail("BPI: user string out of bounds");
                        uint32_t charLen;
                        std::memcpy(&charLen, img->userStrings + insn.a, 4);
                        if (insn.a + 4 + static_cast<uint64_t>(charLen) * 2 > img->userStringsLen)
                            interp_fail("BPI: user string out of bounds");
                        regs[r0].ref = dn2cpp_string_literal(
                            reinterpret_cast<const char16_t*>(img->userStrings + insn.a + 4),
                            static_cast<int32_t>(charLen));
                        break;
                    }
                    // Binary arithmetic: r0 = dst, r1/r2 = operands.
                    DN2CPP_REG_BIN(R_ADD_I32, 0x58, kKindI32)
                    DN2CPP_REG_BIN(R_ADD_I64, 0x58, kKindI64)
                    DN2CPP_REG_BIN(R_SUB_I32, 0x59, kKindI32)
                    DN2CPP_REG_BIN(R_SUB_I64, 0x59, kKindI64)
                    DN2CPP_REG_BIN(R_MUL_I32, 0x5A, kKindI32)
                    DN2CPP_REG_BIN(R_MUL_I64, 0x5A, kKindI64)
                    DN2CPP_REG_BIN(R_DIV_I32, 0x5B, kKindI32)
                    DN2CPP_REG_BIN(R_DIV_I64, 0x5B, kKindI64)
                    DN2CPP_REG_BIN(R_DIV_UN_I32, 0x5C, kKindI32)
                    DN2CPP_REG_BIN(R_DIV_UN_I64, 0x5C, kKindI64)
                    DN2CPP_REG_BIN(R_REM_I32, 0x5D, kKindI32)
                    DN2CPP_REG_BIN(R_REM_I64, 0x5D, kKindI64)
                    DN2CPP_REG_BIN(R_REM_UN_I32, 0x5E, kKindI32)
                    DN2CPP_REG_BIN(R_REM_UN_I64, 0x5E, kKindI64)
                    DN2CPP_REG_BIN(R_AND_I32, 0x5F, kKindI32)
                    DN2CPP_REG_BIN(R_AND_I64, 0x5F, kKindI64)
                    DN2CPP_REG_BIN(R_OR_I32, 0x60, kKindI32)
                    DN2CPP_REG_BIN(R_OR_I64, 0x60, kKindI64)
                    DN2CPP_REG_BIN(R_XOR_I32, 0x61, kKindI32)
                    DN2CPP_REG_BIN(R_XOR_I64, 0x61, kKindI64)
                    DN2CPP_REG_BIN(R_ADD_F32, 0x58, kKindF32)
                    DN2CPP_REG_BIN(R_ADD_F64, 0x58, kKindF64)
                    DN2CPP_REG_BIN(R_SUB_F32, 0x59, kKindF32)
                    DN2CPP_REG_BIN(R_SUB_F64, 0x59, kKindF64)
                    DN2CPP_REG_BIN(R_MUL_F32, 0x5A, kKindF32)
                    DN2CPP_REG_BIN(R_MUL_F64, 0x5A, kKindF64)
                    DN2CPP_REG_BIN(R_DIV_F32, 0x5B, kKindF32)
                    DN2CPP_REG_BIN(R_DIV_F64, 0x5B, kKindF64)
                    DN2CPP_REG_BIN(R_REM_F32, 0x5D, kKindF32)
                    DN2CPP_REG_BIN(R_REM_F64, 0x5D, kKindF64)
                    // Shifts: the amount masks to the operand width, as v1.
                    case R_SHL_I32:
                    case R_SHR_I32:
                    case R_SHR_UN_I32:
                    {
                        int32_t sh = static_cast<int32_t>(regs[r2].i) & 31;
                        int32_t v = static_cast<int32_t>(regs[r1].i);
                        if (insn.op == R_SHL_I32)
                            regs[r0].i = static_cast<int32_t>(static_cast<uint32_t>(v) << sh);
                        else if (insn.op == R_SHR_I32)
                            regs[r0].i = v >> sh;
                        else
                            regs[r0].i = static_cast<int32_t>(static_cast<uint32_t>(v) >> sh);
                        break;
                    }
                    case R_SHL_I64:
                    case R_SHR_I64:
                    case R_SHR_UN_I64:
                    {
                        int32_t sh = static_cast<int32_t>(regs[r2].i) & 63;
                        int64_t v = regs[r1].i;
                        if (insn.op == R_SHL_I64)
                            regs[r0].i = static_cast<int64_t>(static_cast<uint64_t>(v) << sh);
                        else if (insn.op == R_SHR_I64)
                            regs[r0].i = v >> sh;
                        else
                            regs[r0].i = static_cast<int64_t>(static_cast<uint64_t>(v) >> sh);
                        break;
                    }
                    // Unary: r0 = dst, r1 = src.
                    case R_NEG_I32:
                        regs[r0].i = static_cast<int32_t>(0u - static_cast<uint32_t>(regs[r1].i));
                        break;
                    case R_NEG_I64:
                        regs[r0].i = static_cast<int64_t>(0ull - static_cast<uint64_t>(regs[r1].i));
                        break;
                    case R_NEG_F32:
                        regs[r0].f = -static_cast<float>(regs[r1].f);
                        break;
                    case R_NEG_F64:
                        regs[r0].f = -regs[r1].f;
                        break;
                    case R_NOT_I32:
                        regs[r0].i = static_cast<int32_t>(~static_cast<uint32_t>(regs[r1].i));
                        break;
                    case R_NOT_I64:
                        regs[r0].i = ~regs[r1].i;
                        break;
                    // Conversions: the v1 conv arms with the source-kind hint
                    // promoted into the opcode (target-major; _FROM_F covers
                    // both float widths — the slot is a widened double either
                    // way). conv.i4/conv.u4 share one body per source, as v1.
                    case R_CONV_I4_FROM_I32:
                    case R_CONV_I4_FROM_I64:
                    case R_CONV_U4_FROM_I32:
                    case R_CONV_U4_FROM_I64:
                        regs[r0].i = static_cast<int32_t>(regs[r1].i); // truncate, keep sign-extended
                        break;
                    case R_CONV_I4_FROM_F:
                        regs[r0].i = static_cast<int32_t>(regs[r1].f);
                        break;
                    case R_CONV_U4_FROM_F:
                        regs[r0].i = static_cast<int32_t>(static_cast<uint32_t>(regs[r1].f));
                        break;
                    case R_CONV_I8_FROM_I32: // i32 slots are already sign-extended
                    case R_CONV_I8_FROM_I64:
                    case R_CONV_U8_FROM_I64:
                        regs[r0].i = regs[r1].i;
                        break;
                    case R_CONV_I8_FROM_F:
                        regs[r0].i = static_cast<int64_t>(regs[r1].f);
                        break;
                    case R_CONV_U8_FROM_I32: // zero-extends
                        regs[r0].i = static_cast<int64_t>(static_cast<uint32_t>(regs[r1].i));
                        break;
                    case R_CONV_U8_FROM_F:
                        regs[r0].i = static_cast<int64_t>(static_cast<uint64_t>(regs[r1].f));
                        break;
                    case R_CONV_R4_FROM_I32: // rounds to float32, kept widened
                        regs[r0].f = static_cast<float>(static_cast<int32_t>(regs[r1].i));
                        break;
                    case R_CONV_R4_FROM_I64:
                        regs[r0].f = static_cast<float>(regs[r1].i);
                        break;
                    case R_CONV_R4_FROM_F:
                        regs[r0].f = static_cast<float>(regs[r1].f);
                        break;
                    case R_CONV_R8_FROM_I32:
                        regs[r0].f = static_cast<double>(static_cast<int32_t>(regs[r1].i));
                        break;
                    case R_CONV_R8_FROM_I64:
                        regs[r0].f = static_cast<double>(regs[r1].i);
                        break;
                    case R_CONV_R8_FROM_F: // f32 slots are already widened
                        regs[r0].f = regs[r1].f;
                        break;
                    // Comparisons: r0 = dst (i32 0/1), r1/r2 = operands.
                    DN2CPP_REG_CMP(R_CEQ_I32, kKindI32, CmpOp::Eq, false)
                    DN2CPP_REG_CMP(R_CEQ_I64, kKindI64, CmpOp::Eq, false)
                    DN2CPP_REG_CMP(R_CEQ_F32, kKindF32, CmpOp::Eq, false)
                    DN2CPP_REG_CMP(R_CEQ_F64, kKindF64, CmpOp::Eq, false)
                    DN2CPP_REG_CMP(R_CGT_I32, kKindI32, CmpOp::Gt, false)
                    DN2CPP_REG_CMP(R_CGT_I64, kKindI64, CmpOp::Gt, false)
                    DN2CPP_REG_CMP(R_CGT_F32, kKindF32, CmpOp::Gt, false)
                    DN2CPP_REG_CMP(R_CGT_F64, kKindF64, CmpOp::Gt, false)
                    DN2CPP_REG_CMP(R_CGT_UN_I32, kKindI32, CmpOp::Gt, true)
                    DN2CPP_REG_CMP(R_CGT_UN_I64, kKindI64, CmpOp::Gt, true)
                    DN2CPP_REG_CMP(R_CGT_UN_F32, kKindF32, CmpOp::Gt, true)
                    DN2CPP_REG_CMP(R_CGT_UN_F64, kKindF64, CmpOp::Gt, true)
                    DN2CPP_REG_CMP(R_CLT_I32, kKindI32, CmpOp::Lt, false)
                    DN2CPP_REG_CMP(R_CLT_I64, kKindI64, CmpOp::Lt, false)
                    DN2CPP_REG_CMP(R_CLT_F32, kKindF32, CmpOp::Lt, false)
                    DN2CPP_REG_CMP(R_CLT_F64, kKindF64, CmpOp::Lt, false)
                    DN2CPP_REG_CMP(R_CLT_UN_I32, kKindI32, CmpOp::Lt, true)
                    DN2CPP_REG_CMP(R_CLT_UN_I64, kKindI64, CmpOp::Lt, true)
                    DN2CPP_REG_CMP(R_CLT_UN_F32, kKindF32, CmpOp::Lt, true)
                    DN2CPP_REG_CMP(R_CLT_UN_F64, kKindF64, CmpOp::Lt, true)
                    DN2CPP_REG_CMP(R_CEQ_REF, kKindRef, CmpOp::Eq, false)
                    DN2CPP_REG_CMP(R_CGT_UN_REF, kKindRef, CmpOp::Gt, true) // the "!= null" idiom
                    // Branches: a = the absolute instruction index.
                    case R_BR:
                        next = branch_target(insn.a);
                        break;
                    case R_BRTRUE_I32:
                        if (static_cast<int32_t>(regs[r0].i) != 0)
                            next = branch_target(insn.a);
                        break;
                    case R_BRTRUE_I64:
                        if (regs[r0].i != 0)
                            next = branch_target(insn.a);
                        break;
                    case R_BRTRUE_REF:
                        if (regs[r0].ref != nullptr)
                            next = branch_target(insn.a);
                        break;
                    case R_BRFALSE_I32:
                        if (static_cast<int32_t>(regs[r0].i) == 0)
                            next = branch_target(insn.a);
                        break;
                    case R_BRFALSE_I64:
                        if (regs[r0].i == 0)
                            next = branch_target(insn.a);
                        break;
                    case R_BRFALSE_REF:
                        if (regs[r0].ref == nullptr)
                            next = branch_target(insn.a);
                        break;
                    // Compare-branches: r0/r1 = operands; the _UN float forms
                    // take the branch on an unordered pair (v1 semantics via
                    // interp_cmp).
                    DN2CPP_REG_BRCMP(R_BEQ_I32, kKindI32, CmpOp::Eq, false)
                    DN2CPP_REG_BRCMP(R_BEQ_I64, kKindI64, CmpOp::Eq, false)
                    DN2CPP_REG_BRCMP(R_BEQ_F32, kKindF32, CmpOp::Eq, false)
                    DN2CPP_REG_BRCMP(R_BEQ_F64, kKindF64, CmpOp::Eq, false)
                    DN2CPP_REG_BRCMP(R_BNE_UN_I32, kKindI32, CmpOp::Ne, true)
                    DN2CPP_REG_BRCMP(R_BNE_UN_I64, kKindI64, CmpOp::Ne, true)
                    DN2CPP_REG_BRCMP(R_BNE_UN_F32, kKindF32, CmpOp::Ne, true)
                    DN2CPP_REG_BRCMP(R_BNE_UN_F64, kKindF64, CmpOp::Ne, true)
                    DN2CPP_REG_BRCMP(R_BGE_I32, kKindI32, CmpOp::Ge, false)
                    DN2CPP_REG_BRCMP(R_BGE_I64, kKindI64, CmpOp::Ge, false)
                    DN2CPP_REG_BRCMP(R_BGE_F32, kKindF32, CmpOp::Ge, false)
                    DN2CPP_REG_BRCMP(R_BGE_F64, kKindF64, CmpOp::Ge, false)
                    DN2CPP_REG_BRCMP(R_BGT_I32, kKindI32, CmpOp::Gt, false)
                    DN2CPP_REG_BRCMP(R_BGT_I64, kKindI64, CmpOp::Gt, false)
                    DN2CPP_REG_BRCMP(R_BGT_F32, kKindF32, CmpOp::Gt, false)
                    DN2CPP_REG_BRCMP(R_BGT_F64, kKindF64, CmpOp::Gt, false)
                    DN2CPP_REG_BRCMP(R_BLE_I32, kKindI32, CmpOp::Le, false)
                    DN2CPP_REG_BRCMP(R_BLE_I64, kKindI64, CmpOp::Le, false)
                    DN2CPP_REG_BRCMP(R_BLE_F32, kKindF32, CmpOp::Le, false)
                    DN2CPP_REG_BRCMP(R_BLE_F64, kKindF64, CmpOp::Le, false)
                    DN2CPP_REG_BRCMP(R_BLT_I32, kKindI32, CmpOp::Lt, false)
                    DN2CPP_REG_BRCMP(R_BLT_I64, kKindI64, CmpOp::Lt, false)
                    DN2CPP_REG_BRCMP(R_BLT_F32, kKindF32, CmpOp::Lt, false)
                    DN2CPP_REG_BRCMP(R_BLT_F64, kKindF64, CmpOp::Lt, false)
                    DN2CPP_REG_BRCMP(R_BGE_UN_I32, kKindI32, CmpOp::Ge, true)
                    DN2CPP_REG_BRCMP(R_BGE_UN_I64, kKindI64, CmpOp::Ge, true)
                    DN2CPP_REG_BRCMP(R_BGE_UN_F32, kKindF32, CmpOp::Ge, true)
                    DN2CPP_REG_BRCMP(R_BGE_UN_F64, kKindF64, CmpOp::Ge, true)
                    DN2CPP_REG_BRCMP(R_BGT_UN_I32, kKindI32, CmpOp::Gt, true)
                    DN2CPP_REG_BRCMP(R_BGT_UN_I64, kKindI64, CmpOp::Gt, true)
                    DN2CPP_REG_BRCMP(R_BGT_UN_F32, kKindF32, CmpOp::Gt, true)
                    DN2CPP_REG_BRCMP(R_BGT_UN_F64, kKindF64, CmpOp::Gt, true)
                    DN2CPP_REG_BRCMP(R_BLE_UN_I32, kKindI32, CmpOp::Le, true)
                    DN2CPP_REG_BRCMP(R_BLE_UN_I64, kKindI64, CmpOp::Le, true)
                    DN2CPP_REG_BRCMP(R_BLE_UN_F32, kKindF32, CmpOp::Le, true)
                    DN2CPP_REG_BRCMP(R_BLE_UN_F64, kKindF64, CmpOp::Le, true)
                    DN2CPP_REG_BRCMP(R_BLT_UN_I32, kKindI32, CmpOp::Lt, true)
                    DN2CPP_REG_BRCMP(R_BLT_UN_I64, kKindI64, CmpOp::Lt, true)
                    DN2CPP_REG_BRCMP(R_BLT_UN_F32, kKindF32, CmpOp::Lt, true)
                    DN2CPP_REG_BRCMP(R_BLT_UN_F64, kKindF64, CmpOp::Lt, true)
                    DN2CPP_REG_BRCMP(R_BEQ_REF, kKindRef, CmpOp::Eq, false)
                    DN2CPP_REG_BRCMP(R_BNE_UN_REF, kKindRef, CmpOp::Ne, true)
                    case R_LDFTN: // the delegate-method closure for the following
                                  // delegate newobj — the v1 ldftn arm, dst = regs[r0]
                    {
                        auto* c = static_cast<Dn2CppInterpDgFtn*>(
                            dn2cpp_alloc(sizeof(Dn2CppInterpDgFtn)));
                        c->magic = kDgFtnMagic;
                        c->boundThis = nullptr;
                        if (DN2CPP_BPI_REF_TAG(insn.a) == DN2CPP_BPI_TAG_PATCH)
                        {
                            uint32_t idx = DN2CPP_BPI_REF_INDEX(insn.a);
                            if (idx >= img->methodCount)
                                interp_fail("BPI: ldftn patch method index out of bounds");
                            c->kind = kDgFtnPatch;
                            c->img = img;
                            c->methodIdx = idx;
                        }
                        else
                        {
                            // As the v1 arm.
                            dg_ftn_bind_aot(c,
                                import_at(img, insn.a, DN2CPP_BPI_IMPORT_METHOD));
                        }
                        regs[r0].ref = c;
                        break;
                    }
                    case R_CALL:     // r0 = window base; a = method EntityRef; b bit0 =
                    case R_CALLVIRT: // hasResult (into regs[r0]), bit1 = null-check —
                                     // the v1 call/callvirt arms over a contiguous
                                     // register window instead of the eval stack
                    {
                        if (DN2CPP_BPI_REF_TAG(insn.a) == DN2CPP_BPI_TAG_PATCH)
                        {
                            uint32_t idx = DN2CPP_BPI_REF_INDEX(insn.a);
                            if (idx >= img->methodCount)
                                interp_fail("BPI: call method index out of bounds");
                            uint32_t n = img->methods[idx].argCount;
                            // &regs[r0] holds the arguments in parameter order;
                            // the callee copies them into its own frame. b bit1 =
                            // instance: window[0] is the receiver, null-checked
                            // here (the C# callvirt contract).
                            Slot* window = &regs[r0];
                            // As the v1 arm: the instance bit OR the opcode, so
                            // the virtual-dispatch read of self->type below
                            // cannot be reached with a null receiver by an image
                            // that clears the bit.
                            if (((insn.b & 2) || insn.op == R_CALLVIRT) && window[0].ref == nullptr)
                                dn2cpp_throw_null_reference();
                            uint32_t target = idx;
                            if (insn.op == R_CALLVIRT)
                            {
                                // Interpreted virtual dispatch: as v1, the baked
                                // method's frozen slot re-resolves down the
                                // receiver's patch chain.
                                auto* self = static_cast<Dn2CppObject*>(window[0].ref);
                                const auto* iti = reinterpret_cast<const InterpTypeInfo*>(self->type);
                                if (self->type == nullptr || iti->magic != kInterpTypeInfoMagic
                                    || iti->img != img)
                                    interp_fail("interp: virtual patch dispatch on a non-patch receiver");
                                if (img->methods[idx].vtableSlot == 0xFFFF)
                                    interp_fail("interp: callvirt target occupies no vtable slot");
                                target = resolve_override_method(img, iti->typeIdx,
                                    img->methods[idx].vtableSlot);
                                if (target == DN2CPP_BPI_REF_NONE || target >= img->methodCount
                                    || img->methods[target].argCount != n)
                                    interp_fail("interp: vtable slot has no interpreted override");
                            }
                            // The receiver's TYPE, which only the virtual arm
                            // above states (magic stamp plus a re-resolve down
                            // the receiver's own chain). The body a call enters
                            // reads fields at the DECLARING type's
                            // loader-assigned offsets: off the end of a shorter
                            // object that is an out-of-range write, not a
                            // misread. Asked under the same condition as the
                            // null test, so a static call's arg0 is never
                            // mistaken for a receiver.
                            if (((insn.b & 2) || insn.op == R_CALLVIRT)
                                && !patch_receiver_ok(img, target,
                                    static_cast<Dn2CppObject*>(window[0].ref)))
                                interp_fail(kPatchRecvFail);
                            if (img->methods[target].declTypeIdx != m.declTypeIdx)
                                interp_ensure_cctor(img, img->methods[target].declTypeIdx, depth);
                            Slot r = interp_call(img, target, window, depth + 1);
                            if (insn.b & 1)
                                regs[r0] = r;
                            break;
                        }
                        const ImportBinding& b = import_at(img, insn.a, DN2CPP_BPI_IMPORT_METHOD);
                        Slot* window = &regs[r0];
                        switch (b.callShape)
                        {
                            case kShapeVoidStr:
                            {
                                // As the v1 arm above.
                                auto* a0 = static_cast<Dn2CppObject*>(window[0].ref);
                                if (!intrinsic_arg_ok(b.argKinds[0], a0))
                                    interp_fail(kIntrinsicArgFail);
                                reinterpret_cast<void (*)(Dn2CppString*)>(b.fnPtr)(
                                    reinterpret_cast<Dn2CppString*>(a0));
                                break;
                            }
                            case kShapeVoidEmpty:
                                reinterpret_cast<void (*)()>(b.fnPtr)();
                                break;
                            case kShapeVoidI4:
                            case kShapeVoidBool:
                                reinterpret_cast<void (*)(int32_t)>(b.fnPtr)(
                                    static_cast<int32_t>(window[0].i));
                                break;
                            case kShapeVoidI8:
                                reinterpret_cast<void (*)(int64_t)>(b.fnPtr)(window[0].i);
                                break;
                            case kShapeVoidR8:
                                reinterpret_cast<void (*)(double)>(b.fnPtr)(window[0].f);
                                break;
                            case kShapeRefRetObj: // instance intrinsic (e.g. Exception.get_Message)
                            {
                                auto* self = static_cast<Dn2CppObject*>(window[0].ref);
                                if (self == nullptr)
                                    dn2cpp_throw_null_reference();
                                // As the v1 arm above.
                                if (!intrinsic_receiver_ok(b.recvKind, self))
                                    interp_fail("interp: intrinsic call receiver is not an instance of the import's declared type");
                                Slot v{};
                                v.ref = reinterpret_cast<Dn2CppObject* (*)(Dn2CppObject*)>(b.fnPtr)(self);
                                if (insn.b & 1)
                                    regs[r0] = v;
                                break;
                            }
                            case kShapeRefRetStr: // static intrinsic (e.g. Type.GetType)
                            {
                                // As the v1 arm above.
                                auto* a0 = static_cast<Dn2CppObject*>(window[0].ref);
                                if (!intrinsic_arg_ok(b.argKinds[0], a0))
                                    interp_fail(kIntrinsicArgFail);
                                Slot v{};
                                v.ref = reinterpret_cast<Dn2CppObject* (*)(Dn2CppString*)>(b.fnPtr)(
                                    reinterpret_cast<Dn2CppString*>(a0));
                                if (insn.b & 1)
                                    regs[r0] = v;
                                break;
                            }
                            case kShapeRefRetRefN: // static intrinsic (e.g. String.Concat)
                            {
                                Dn2CppObject* refArgs[kMaxImportArgs];
                                for (uint32_t j = 0; j < b.argCount; j++)
                                    refArgs[j] = static_cast<Dn2CppObject*>(window[j].ref);
                                // As the v1 arm above.
                                for (uint32_t j = 0; j < b.argCount; j++)
                                    if (!intrinsic_arg_ok(b.argKinds[j], refArgs[j]))
                                        interp_fail(kIntrinsicArgFail);
                                Slot v{};
                                v.ref = reinterpret_cast<Dn2CppObject* (*)(Dn2CppObject* const*)>(
                                    b.fnPtr)(refArgs);
                                if (insn.b & 1)
                                    regs[r0] = v;
                                break;
                            }
                            case kShapeDelegateInvoke:
                            {
                                // Invoke on a delegate: the window is [dg, args...];
                                // the raw argument slots feed the pre-emitted M2N
                                // invoke bridge exactly as in v1.
                                auto* dg = static_cast<Dn2CppObject*>(window[0].ref);
                                if (dg == nullptr)
                                    dn2cpp_throw_null_reference();
                                // As the v1 arm above.
                                if (!import_receiver_ok(b, dg))
                                    interp_fail(kImportRecvFail);
                                using DgBridge = Dn2CppInterpSlot (*)(
                                    Dn2CppObject*, const Dn2CppInterpSlot*, int32_t);
                                Slot r = reinterpret_cast<DgBridge>(b.fnPtr)(dg, window + 1,
                                    static_cast<int32_t>(b.argCount));
                                if (insn.b & 1)
                                    regs[r0] = r;
                                break;
                            }
                            case kShapeInvoker:
                            {
                                // The generic path — v1's invoker arm over the
                                // window: [this,] args in parameter order.
                                if (b.isCtor && insn.op == R_CALLVIRT)
                                    interp_fail("interp: a constructor import is not a callvirt target");
                                uint32_t argBase = b.isInstance ? 1u : 0u;
                                Dn2CppObject* callArgs[kMaxImportArgs];
                                for (uint32_t j = 0; j < b.argCount; j++)
                                    callArgs[j] = marshal_slot_to_object(b.args[j], window[argBase + j]);
                                Dn2CppObject* self = nullptr;
                                void* fn = b.fnPtr;
                                if (b.isInstance)
                                {
                                    self = static_cast<Dn2CppObject*>(window[0].ref);
                                    if (self == nullptr)
                                        dn2cpp_throw_null_reference();
                                    // As the v1 arm above.
                                    if (!import_receiver_ok(b, self))
                                        interp_fail(kImportRecvFail);
                                    if (b.isInterface)
                                    {
                                        if (self->type == nullptr)
                                            interp_fail("interp: interface dispatch on a typeless receiver");
                                        const void** slots =
                                            dn2cpp_resolve_interface(self->type, b.type);
                                        fn = const_cast<void*>(slots[b.vtableSlot]);
                                        if (fn == nullptr)
                                            interp_fail("interp: interface slot is empty");
                                    }
                                    else if (insn.op == R_CALLVIRT && b.vtableSlot >= 0)
                                    {
                                        if (self->type == nullptr || self->type->vtable == nullptr)
                                            interp_fail("interp: receiver type has no vtable");
                                        fn = const_cast<void*>(self->type->vtable[b.vtableSlot]);
                                        if (fn == nullptr)
                                            interp_fail("interp: receiver vtable slot is empty");
                                    }
                                }
                                Dn2CppObject* r =
                                    reinterpret_cast<InvokerFn>(b.invoker)(fn, self, callArgs, b.retType);
                                if (insn.b & 1)
                                    regs[r0] = marshal_object_to_slot(b.ret, r);
                                break;
                            }
                            case kShapeExceptionNew:
                            {
                                // `base(message)` from an interpreted patch exception
                                // ctor — the register-window twin of the stack arm:
                                // window[0] is the half-built receiver, args at
                                // window[1..]. Do NOT allocate; seed + run the base
                                // ctor. See exc_seed_and_run_base_ctor.
                                if (insn.op == R_CALLVIRT)
                                    interp_fail("interp: a constructor import is not a callvirt target");
                                Dn2CppObject* callArgs[kMaxImportArgs];
                                for (uint32_t j = 0; j < b.argCount; j++)
                                    callArgs[j] = marshal_slot_to_object(b.args[j], window[1 + j]);
                                auto* self = static_cast<Dn2CppObject*>(window[0].ref);
                                if (self == nullptr)
                                    dn2cpp_throw_null_reference();
                                // As the v1 arm above.
                                if (!import_receiver_ok(b, self))
                                    interp_fail(kImportRecvFail);
                                exc_seed_and_run_base_ctor(b, self, callArgs);
                                break;
                            }
                            default:
                                interp_fail("interp: unbound call import");
                        }
                        break;
                    }
                    case R_NEWOBJ: // r0 = window base; the constructed reference lands
                                   // in regs[r0] — the v1 newobj arm over the window
                    {
                        if (DN2CPP_BPI_REF_TAG(insn.a) == DN2CPP_BPI_TAG_PATCH)
                        {
                            uint32_t idx = DN2CPP_BPI_REF_INDEX(insn.a);
                            if (idx >= img->methodCount)
                                interp_fail("BPI: newobj method index out of bounds");
                            const Dn2CppBpiMethod& cm = img->methods[idx];
                            if (cm.argCount == 0 || cm.argCount > static_cast<uint32_t>(kMaxFrame))
                                interp_fail("BPI: patch newobj target has an invalid frame shape");
                            uint32_t t = cm.declTypeIdx;
                            if (t >= img->typeCount || img->patchTypes[t] == nullptr)
                                interp_fail("BPI: patch newobj declaring type out of bounds");
                            int32_t nParams = static_cast<int32_t>(cm.argCount) - 1;
                            interp_ensure_cctor(img, t, depth);
                            const Dn2CppTypeInfo* ti = img->patchTypes[t];
                            size_t sz = ti->instanceSize > static_cast<int32_t>(sizeof(Dn2CppObject))
                                ? static_cast<size_t>(ti->instanceSize)
                                : sizeof(Dn2CppObject);
                            // External allocation hook + finalizer registration,
                            // exactly as in the v1 arm.
                            Dn2CppObject* obj = dn2cpp_interp_alloc_hook != nullptr
                                ? dn2cpp_interp_alloc_hook(ti, sz)
                                : nullptr;
                            if (obj == nullptr)
                            {
                                obj = static_cast<Dn2CppObject*>(dn2cpp_alloc(sz));
                                obj->type = ti;
                            }
                            if (ti->finalize != nullptr) // inherited from the AOT base
                                dn2cpp_register_finalizer(obj);
                            Slot ctorArgs[kMaxFrame];
                            ctorArgs[0].ref = obj;
                            for (int32_t j = 0; j < nParams; j++)
                                ctorArgs[1 + j] = regs[r0 + j];
                            interp_call(img, idx, ctorArgs, depth + 1);
                            regs[r0].ref = obj;
                            break;
                        }
                        // A Type import operand is a delegate construction; the
                        // window is [target, ftn] (the v1 arm popped ftn, then
                        // target).
                        if (DN2CPP_BPI_REF_TAG(insn.a) == DN2CPP_BPI_TAG_IMPORT)
                        {
                            uint32_t ti = DN2CPP_BPI_REF_INDEX(insn.a);
                            if (ti < img->importCount
                                && img->bindings[ti].kind == DN2CPP_BPI_IMPORT_TYPE)
                            {
                                const Dn2CppTypeInfo* dti = img->bindings[ti].type;
                                if (dti == nullptr || (dti->flags & DN2CPP_TF_DELEGATE) == 0)
                                    interp_fail("BPI bind: delegate newobj type import is not a delegate");
                                Slot targetSlot = regs[r0];
                                Slot ftnSlot = regs[r0 + 1];
                                auto* c = static_cast<Dn2CppInterpDgFtn*>(ftnSlot.ref);
                                if (c == nullptr || c->magic != kDgFtnMagic)
                                    interp_fail("interp: delegate newobj without a method reference (ldftn)");
                                auto* dg = static_cast<Dn2CppDelegate*>(
                                    dn2cpp_alloc(sizeof(Dn2CppDelegate)));
                                dg->type = dti;
                                dg->prev = nullptr;
                                if (c->kind == kDgFtnPatch)
                                {
                                    // Refuse a null 'this' when the patch method
                                    // is an INSTANCE method, at construction as
                                    // real .NET does. Instance-ness is derived
                                    // from the delegate's Invoke arity; a table
                                    // that cannot answer degrades.
                                    check_patch_delegate_null_this(img, c->methodIdx, dti,
                                        static_cast<Dn2CppObject*>(targetSlot.ref));
                                    c->boundThis = static_cast<Dn2CppObject*>(targetSlot.ref);
                                    const void* thunk = find_dg_thunk(dti);
                                    if (thunk == nullptr)
                                        interp_fail("BPI bind: no delegate thunk (Invoke signature outside the bridged surface)");
                                    dg->target = reinterpret_cast<Dn2CppObject*>(c);
                                    dg->method = const_cast<void*>(thunk);
                                }
                                else
                                {
                                    // As the v1 arm: a kDgFtnAot target is a
                                    // bound instance method, so null is never
                                    // legitimate — refuse at construction,
                                    // like real .NET (throw_delegate_null_this).
                                    // Unconditional here, where the kDgFtnPatch
                                    // arm above must first derive instance-ness:
                                    // the ldftn arm rejects a static AOT target
                                    // outright, so there is nothing a null could
                                    // legitimately mean.
                                    if (targetSlot.ref == nullptr)
                                        throw_delegate_null_this();
                                    // As the v1 arm above.
                                    if (!dg_aot_target_ok(c,
                                            static_cast<Dn2CppObject*>(targetSlot.ref)))
                                        interp_fail(kImportRecvFail);
                                    // And the rest of the call: the pointer is
                                    // invoked through the delegate's own Invoke
                                    // ABI, so a target whose real signature is a
                                    // different one is called with registers the
                                    // caller never set.
                                    if (!dg_aot_signature_ok(c, dti))
                                        interp_fail(kDgSigFail);
                                    dg->target = static_cast<Dn2CppObject*>(targetSlot.ref);
                                    dg->method = c->aotFn;
                                }
                                regs[r0].ref = dg;
                                break;
                            }
                        }
                        const ImportBinding& b = import_at(img, insn.a, DN2CPP_BPI_IMPORT_METHOD);
                        if (b.callShape == kShapeExceptionNew)
                        {
                            // The exception-ctor intercept, as the stack arm: the
                            // window holds the ctor args in parameter order. Marshal,
                            // seed the prefix from the recognized shape's indices,
                            // allocate the uniform exception, then run the resolved
                            // real ctor body (field writes land) — seed-before-ctor,
                            // matching AOT.
                            Dn2CppObject* callArgs[kMaxImportArgs];
                            for (uint32_t j = 0; j < b.argCount; j++)
                                callArgs[j] = marshal_slot_to_object(b.args[j], regs[r0 + j]);
                            Dn2CppString* message = b.excMsgArg >= 0
                                ? reinterpret_cast<Dn2CppString*>(callArgs[b.excMsgArg]) : nullptr;
                            Dn2CppObject* inner = b.excInnerArg >= 0
                                ? callArgs[b.excInnerArg] : nullptr;
                            Dn2CppObject* obj = dn2cpp_exception_new(b.type, message, inner);
                            if (b.fnPtr != nullptr)
                                reinterpret_cast<InvokerFn>(b.invoker)(b.fnPtr, obj, callArgs, nullptr);
                            regs[r0].ref = obj;
                            break;
                        }
                        if (!b.isCtor || b.callShape != kShapeInvoker)
                            interp_fail("interp: newobj target is not a bound constructor import");
                        Dn2CppObject* callArgs[kMaxImportArgs];
                        for (uint32_t j = 0; j < b.argCount; j++)
                            callArgs[j] = marshal_slot_to_object(b.args[j], regs[r0 + j]);
                        const Dn2CppTypeInfo* ti = b.type;
                        size_t sz = ti->instanceSize > static_cast<int32_t>(sizeof(Dn2CppObject))
                            ? static_cast<size_t>(ti->instanceSize)
                            : sizeof(Dn2CppObject);
                        auto* obj = static_cast<Dn2CppObject*>(dn2cpp_alloc(sz));
                        obj->type = ti;
                        if (ti->finalize != nullptr)
                            dn2cpp_register_finalizer(obj);
                        reinterpret_cast<InvokerFn>(b.invoker)(b.fnPtr, obj, callArgs, nullptr);
                        regs[r0].ref = obj;
                        break;
                    }
                    case R_LDFLD: // r0 = dst, r1 = obj; a/b = v1's field operands
                    {
                        if (DN2CPP_BPI_REF_TAG(insn.a) == DN2CPP_BPI_TAG_PATCH)
                        {
                            uint32_t idx = DN2CPP_BPI_REF_INDEX(insn.a);
                            if (idx >= img->fieldCount)
                                interp_fail("BPI: patch field index out of bounds");
                            if (img->fields[idx].staticSlot != DN2CPP_BPI_REF_NONE)
                                interp_fail("interp: ldfld on a static patch field");
                            auto* obj = static_cast<Dn2CppObject*>(regs[r1].ref);
                            if (obj == nullptr)
                                dn2cpp_throw_null_reference();
                            const char* p = reinterpret_cast<const char*>(obj)
                                + img->instFieldOffsets[idx];
                            Slot v{};
                            switch (img->instFieldKinds[idx])
                            {
                                case kMarshalI32:
                                {
                                    int32_t x;
                                    std::memcpy(&x, p, sizeof(x));
                                    v.i = x; // sign-extended: the i32 slot invariant
                                    break;
                                }
                                case kMarshalI64:
                                    std::memcpy(&v.i, p, sizeof(int64_t));
                                    break;
                                case kMarshalF32:
                                {
                                    float x;
                                    std::memcpy(&x, p, sizeof(x));
                                    v.f = x; // widened: the f32 slot invariant
                                    break;
                                }
                                case kMarshalF64:
                                    std::memcpy(&v.f, p, sizeof(double));
                                    break;
                                case kMarshalRef:
                                    std::memcpy(&v.ref, p, sizeof(void*));
                                    break;
                                default:
                                    interp_fail("interp: bad patch field kind");
                            }
                            regs[r0] = v;
                            break;
                        }
                        const ImportBinding& b = import_at(img, insn.a, DN2CPP_BPI_IMPORT_FIELD);
                        const Dn2CppFieldInfo* fi = b.field;
                        if ((fi->attrs & DN2CPP_FLDA_STATIC) != 0)
                            interp_fail("interp: field import staticness mismatch");
                        auto* obj = static_cast<Dn2CppObject*>(regs[r1].ref);
                        if (obj == nullptr)
                            dn2cpp_throw_null_reference();
                        if (fi->getter == nullptr)
                            interp_fail("interp: field has no reflectable storage");
                        regs[r0] = marshal_object_to_slot(b.fieldVal, fi->getter(obj));
                        break;
                    }
                    case R_STFLD: // r0 = obj, r1 = value; a/b = v1's field operands
                    {
                        if (DN2CPP_BPI_REF_TAG(insn.a) == DN2CPP_BPI_TAG_PATCH)
                        {
                            uint32_t idx = DN2CPP_BPI_REF_INDEX(insn.a);
                            if (idx >= img->fieldCount)
                                interp_fail("BPI: patch field index out of bounds");
                            if (img->fields[idx].staticSlot != DN2CPP_BPI_REF_NONE)
                                interp_fail("interp: stfld on a static patch field");
                            Slot v = regs[r1];
                            auto* obj = static_cast<Dn2CppObject*>(regs[r0].ref);
                            if (obj == nullptr)
                                dn2cpp_throw_null_reference();
                            char* p = reinterpret_cast<char*>(obj) + img->instFieldOffsets[idx];
                            switch (img->instFieldKinds[idx])
                            {
                                case kMarshalI32:
                                {
                                    int32_t x = static_cast<int32_t>(v.i);
                                    std::memcpy(p, &x, sizeof(x));
                                    break;
                                }
                                case kMarshalI64:
                                    std::memcpy(p, &v.i, sizeof(int64_t));
                                    break;
                                case kMarshalF32:
                                {
                                    float x = static_cast<float>(v.f);
                                    std::memcpy(p, &x, sizeof(x));
                                    break;
                                }
                                case kMarshalF64:
                                    std::memcpy(p, &v.f, sizeof(double));
                                    break;
                                case kMarshalRef:
                                    std::memcpy(p, &v.ref, sizeof(void*));
                                    break;
                                default:
                                    interp_fail("interp: bad patch field kind");
                            }
                            break;
                        }
                        const ImportBinding& b = import_at(img, insn.a, DN2CPP_BPI_IMPORT_FIELD);
                        const Dn2CppFieldInfo* fi = b.field;
                        if ((fi->attrs & DN2CPP_FLDA_STATIC) != 0)
                            interp_fail("interp: field import staticness mismatch");
                        auto* obj = static_cast<Dn2CppObject*>(regs[r0].ref);
                        if (obj == nullptr)
                            dn2cpp_throw_null_reference();
                        if (fi->setter == nullptr)
                            interp_fail("interp: field has no reflectable storage");
                        fi->setter(obj, marshal_slot_to_object(b.fieldVal, regs[r1]));
                        break;
                    }
                    case R_LDSFLD: // r0 = dst; a/b = v1's static-field operands
                    {
                        if (DN2CPP_BPI_REF_TAG(insn.a) == DN2CPP_BPI_TAG_PATCH)
                        {
                            regs[r0] = *patch_static_addr(img, insn.a, insn.b, depth);
                            break;
                        }
                        const ImportBinding& b = import_at(img, insn.a, DN2CPP_BPI_IMPORT_FIELD);
                        const Dn2CppFieldInfo* fi = b.field;
                        if ((fi->attrs & DN2CPP_FLDA_STATIC) == 0)
                            interp_fail("interp: field import staticness mismatch");
                        if (fi->getter == nullptr)
                            interp_fail("interp: field has no reflectable storage");
                        regs[r0] = marshal_object_to_slot(b.fieldVal, fi->getter(nullptr));
                        break;
                    }
                    case R_STSFLD: // r0 = value; a/b = v1's static-field operands
                    {
                        if (DN2CPP_BPI_REF_TAG(insn.a) == DN2CPP_BPI_TAG_PATCH)
                        {
                            Slot* addr = patch_static_addr(img, insn.a, insn.b, depth);
                            *addr = regs[r0];
                            break;
                        }
                        const ImportBinding& b = import_at(img, insn.a, DN2CPP_BPI_IMPORT_FIELD);
                        const Dn2CppFieldInfo* fi = b.field;
                        if ((fi->attrs & DN2CPP_FLDA_STATIC) == 0)
                            interp_fail("interp: field import staticness mismatch");
                        if (fi->setter == nullptr)
                            interp_fail("interp: field has no reflectable storage");
                        fi->setter(nullptr, marshal_slot_to_object(b.fieldVal, regs[r0]));
                        break;
                    }
                    case R_CASTCLASS: // r0 = dst, r1 = src; a = type EntityRef
                    case R_ISINST:
                    {
                        const Dn2CppTypeInfo* ti = type_ref_at(img, insn.a);
                        auto* obj = static_cast<Dn2CppObject*>(regs[r1].ref);
                        regs[r0].ref = insn.op == R_ISINST ? dn2cpp_isinst(obj, ti)
                                                           : dn2cpp_castclass(obj, ti);
                        break;
                    }
                    case R_NEWARR: // r0 = dst, r1 = length; a = SZArray type EntityRef,
                                   // b = element storage kind — the v1 newarr arm
                    {
                        const Dn2CppTypeInfo* ti = type_ref_at(img, insn.a);
                        if ((ti->flags & DN2CPP_TF_ARRAY) == 0)
                            interp_fail("BPI: newarr type reference is not an array type");
                        int32_t len = static_cast<int32_t>(regs[r1].i);
                        if (len < 0)
                            dn2cpp_overflow(); // catchable, the .NET newarr semantics
                        Slot v{};
                        switch (insn.b)
                        {
                            case kElemI4:
                                v.ref = dn2cpp_newarr_i4_t(len, ti);
                                break;
                            case kElemRef:
                                v.ref = dn2cpp_newarr_ref_t(len, ti);
                                break;
                            case kElemI1:
                            case kElemU1:
                                v.ref = dn2cpp_newarr_n_t(len, 1, ti);
                                break;
                            case kElemI2:
                            case kElemU2:
                                v.ref = dn2cpp_newarr_n_t(len, 2, ti);
                                break;
                            case kElemR4:
                                v.ref = dn2cpp_newarr_n_t(len, 4, ti);
                                break;
                            case kElemI8:
                            case kElemR8:
                                v.ref = dn2cpp_newarr_n_t(len, 8, ti);
                                break;
                            default:
                                interp_fail("interp: bad newarr element kind");
                        }
                        regs[r0] = v;
                        break;
                    }
                    case R_LDLEN: // r0 = dst, r1 = array
                    {
                        auto* arr = static_cast<Dn2CppArray*>(regs[r1].ref);
                        if (arr == nullptr)
                            dn2cpp_throw_null_reference();
                        regs[r0].i = arr->length;
                        break;
                    }
                    case R_LDELEM: // r0 = dst, r1 = array, r2 = index; b = element kind
                    {
                        auto* arr = static_cast<Dn2CppArray*>(regs[r1].ref);
                        if (arr == nullptr)
                            dn2cpp_throw_null_reference();
                        int32_t idx = static_cast<int32_t>(regs[r2].i);
                        if (idx < 0 || idx >= arr->length)
                            dn2cpp_throw_index_out_of_range(); // catchable, like the AOT ThrowHelper trap
                        Slot v{};
                        switch (insn.b)
                        {
                            case kElemI4:
                                v.i = static_cast<Dn2CppArrayI4*>(arr)->data[idx];
                                break;
                            case kElemRef:
                                v.ref = static_cast<Dn2CppArrayRef*>(arr)->data[idx];
                                break;
                            default:
                            {
                                uint32_t w = (insn.b == kElemI1 || insn.b == kElemU1) ? 1u
                                    : (insn.b == kElemI2 || insn.b == kElemU2)        ? 2u
                                    : (insn.b == kElemR4)                             ? 4u
                                    : (insn.b == kElemI8 || insn.b == kElemR8)        ? 8u
                                                                                      : 0u;
                                if (w == 0)
                                    interp_fail("interp: bad array element kind");
                                auto* an = static_cast<Dn2CppArrayN*>(arr);
                                if (an->elemSize != static_cast<int32_t>(w))
                                    interp_fail("interp: array element width mismatch");
                                const char* p = an->data + static_cast<size_t>(idx) * w;
                                switch (insn.b)
                                {
                                    case kElemI1:
                                    {
                                        int8_t x;
                                        std::memcpy(&x, p, 1);
                                        v.i = x; // sign-extended
                                        break;
                                    }
                                    case kElemU1:
                                    {
                                        uint8_t x;
                                        std::memcpy(&x, p, 1);
                                        v.i = x; // zero-extended
                                        break;
                                    }
                                    case kElemI2:
                                    {
                                        int16_t x;
                                        std::memcpy(&x, p, 2);
                                        v.i = x;
                                        break;
                                    }
                                    case kElemU2:
                                    {
                                        uint16_t x;
                                        std::memcpy(&x, p, 2);
                                        v.i = x;
                                        break;
                                    }
                                    case kElemR4:
                                    {
                                        float x;
                                        std::memcpy(&x, p, 4);
                                        v.f = x; // widened: the f32 slot invariant
                                        break;
                                    }
                                    case kElemI8:
                                        std::memcpy(&v.i, p, 8);
                                        break;
                                    default: // kElemR8
                                        std::memcpy(&v.f, p, 8);
                                        break;
                                }
                                break;
                            }
                        }
                        regs[r0] = v;
                        break;
                    }
                    case R_STELEM: // r0 = array, r1 = index, r2 = value; b = element
                                   // kind. A reference-element store skips the
                                   // covariance check, matching the AOT stelem helpers.
                    {
                        auto* arr = static_cast<Dn2CppArray*>(regs[r0].ref);
                        if (arr == nullptr)
                            dn2cpp_throw_null_reference();
                        int32_t idx = static_cast<int32_t>(regs[r1].i);
                        if (idx < 0 || idx >= arr->length)
                            dn2cpp_throw_index_out_of_range(); // catchable, like the AOT ThrowHelper trap
                        Slot val = regs[r2];
                        switch (insn.b)
                        {
                            case kElemI4:
                                static_cast<Dn2CppArrayI4*>(arr)->data[idx] =
                                    static_cast<int32_t>(val.i);
                                break;
                            case kElemRef:
                                static_cast<Dn2CppArrayRef*>(arr)->data[idx] =
                                    static_cast<Dn2CppObject*>(val.ref);
                                break;
                            default:
                            {
                                uint32_t w = (insn.b == kElemI1 || insn.b == kElemU1) ? 1u
                                    : (insn.b == kElemI2 || insn.b == kElemU2)        ? 2u
                                    : (insn.b == kElemR4)                             ? 4u
                                    : (insn.b == kElemI8 || insn.b == kElemR8)        ? 8u
                                                                                      : 0u;
                                if (w == 0)
                                    interp_fail("interp: bad array element kind");
                                auto* an = static_cast<Dn2CppArrayN*>(arr);
                                if (an->elemSize != static_cast<int32_t>(w))
                                    interp_fail("interp: array element width mismatch");
                                char* p = an->data + static_cast<size_t>(idx) * w;
                                switch (insn.b)
                                {
                                    case kElemI1:
                                    case kElemU1:
                                    {
                                        uint8_t x = static_cast<uint8_t>(val.i);
                                        std::memcpy(p, &x, 1);
                                        break;
                                    }
                                    case kElemI2:
                                    case kElemU2:
                                    {
                                        uint16_t x = static_cast<uint16_t>(val.i);
                                        std::memcpy(p, &x, 2);
                                        break;
                                    }
                                    case kElemR4:
                                    {
                                        float x = static_cast<float>(val.f);
                                        std::memcpy(p, &x, 4);
                                        break;
                                    }
                                    case kElemI8:
                                        std::memcpy(p, &val.i, 8);
                                        break;
                                    default: // kElemR8
                                        std::memcpy(p, &val.f, 8);
                                        break;
                                }
                                break;
                            }
                        }
                        break;
                    }
                    case R_RET_VOID:
                        return { ExecKind::Return, Slot{} };
                    case R_RET: // return regs[r0]
                        return { ExecKind::Return, regs[r0] };
                    case R_LEAVE: // a = target; routes through the finallys it exits
                        leaveFrom = pc;
                        leaveTarget = branch_target(insn.a);
                        haveLeave = true;
                        break;
                    case R_ENDFINALLY: // hand control back to whoever ran this handler
                        return { ExecKind::EndFinally, Slot{} };
                    case R_THROW: // throw regs[r0] — the runtime's own unwinding machine
                        dn2cpp_throw(static_cast<Dn2CppObject*>(regs[r0].ref));
                    case R_RETHROW: // a = the enclosing catch's method-relative EH index
                    {
                        if (insn.a >= m.ehCount)
                            interp_fail("BPI: rethrow record index out of bounds");
                        Dn2CppObject* exc = f.handlerExc[insn.a];
                        if (exc == nullptr)
                            interp_fail("interp: rethrow with no exception in flight");
                        // dn2cpp_rethrow, not dn2cpp_throw: IL rethrow preserves
                        // the original stack trace (AOT's bare C++ `throw;` gets
                        // that for free; this re-raise goes through the runtime).
                        dn2cpp_rethrow(exc);
                    }
                    default:
                        // R_INVALID, R_EXT, out-of-table values: the verifier
                        // rejects them at load; this is the runtime backstop.
                        interp_fail("interp: invalid register opcode");
                }
                if (haveLeave)
                    break;
                pc = next;
            }
            if (!haveLeave)
                interp_fail("BPI: method body fell off the end (missing ret)");
        }
        catch (Dn2CppException& ex)
        {
            // The same fused two-pass EH walk as interp_run; nested handler
            // runs recurse into interp_run_reg over the shared register file.
            Dn2CppObject* exc = ex.obj;
            int32_t match = -1;
            for (uint16_t i = 0; i < m.ehCount && match < 0; i++)
            {
                const Dn2CppBpiEH& r = f.eh[i];
                if (!(r.tryStart <= pc && pc < r.tryEnd))
                    continue;
                switch (r.kind)
                {
                    case 0: // catch
                        if (eh_catch_matches(img, r.catchTypeRef, exc))
                            match = static_cast<int32_t>(i);
                        break;
                    case 2: // finally
                    case 3: // fault
                    {
                        ExecResult fin = interp_run_reg(f, r.handlerStart);
                        if (fin.kind != ExecKind::EndFinally)
                            return fin;
                        break;
                    }
                    default:
                        interp_fail("interp: filter clauses are outside the interpreted surface");
                }
            }
            if (match < 0)
                throw;
            dn2cpp_exc_inflight_pop(exc);
            f.handlerExc[match] = exc;
            // Catch entry: the exception object is the handler's depth-0 temp
            // (a catch's entry depth is 1); finally/fault entry writes nothing.
            Slot v{};
            v.ref = exc;
            regs[tempBase] = v;
            pc = f.eh[match].handlerStart;
        }
    }
}

#undef DN2CPP_REG_BIN
#undef DN2CPP_REG_CMP
#undef DN2CPP_REG_BRCMP

// The interpreter's conditional twin of Dn2CppShadowFrame (whose ctor pushes
// unconditionally). RAII for the same reason as the AOT guard: the dispatch
// loops re-raise with a bare `throw;`, so a destructor is the only construct
// guaranteed to pop on unwind.
struct InterpShadowFrame
{
    Dn2CppShadowStack* s = nullptr;
    ~InterpShadowFrame() { if (s != nullptr) s->depth--; }
};

// Executes one patch method. `args` holds argCount slots in parameter order;
// the frame (args + zero-initialized locals, per the LocalSigTable run), the
// eval stack, and the per-EH-record caught-exception slots are native-stack
// arrays, conservatively scanned by the collector. Returns the ret-popped
// slot (zero for void).
Slot interp_call(const Dn2CppInterpImage* img, uint32_t methodIdx, const Slot* args, int32_t depth)
{
    if (methodIdx >= img->methodCount)
        interp_fail("interp: method index out of range");
    if (depth > kMaxCallDepth)
        interp_fail("interp: patch call depth limit exceeded");
    const Dn2CppBpiMethod& m = img->methods[methodIdx];
    // Every dispatch-loop caller passes a stack array, but the .cctor driver
    // passes a literal nullptr on the strength of "a .cctor takes no arguments",
    // which is the image's claim and not a checked one. Check it here, once, for
    // all of them: the frame fills below copy argCount slots out of `args`.
    if (args == nullptr && m.argCount != 0)
        interp_fail("interp: a patch method with parameters called without arguments");
    // Push this interpreted frame onto the shadow stack, above the shape checks
    // and both frame formats so one guard covers everything below. The
    // frameNames predicate is mandatory, not an optimization:
    // dn2cpp_exc_stamp_trace stamps a kind-1 trace whenever shadow depth > 0, so
    // an unconditional push in a plain hotupdate build (no AOT guards planted)
    // would replace the AOT frames' kind-0 PC trace with an interp-only one.
    InterpShadowFrame __shadowFrame;
    if (img->frameNames != nullptr)
    {
        Dn2CppShadowStack* st = dn2cpp_shadow_tls;
        if (st == nullptr)
            st = dn2cpp_shadow_stack_acquire();
        if (st->depth < st->cap)
            st->frames[st->depth] = img->frameNames[methodIdx];
        st->depth++;
        __shadowFrame.s = st;
    }
    uint32_t slotCount = m.argCount + m.localCount;
    if (slotCount > static_cast<uint32_t>(kMaxFrame))
        interp_fail("interp: frame larger than 64 slots needs a later slice (arena frames)");
    if (m.maxStack > kMaxStack)
        interp_fail("interp: eval stack deeper than 64 needs a later slice (arena frames)");
    // The LocalSigTable run is the frame's authority: every arg/local slot has
    // a pre-resolved type reference.
    if (slotCount > 0
        && static_cast<uint64_t>(m.localSigOff) + slotCount > img->localSigCount)
        interp_fail("BPI: local-sig run out of bounds");
    if (m.codeOff % sizeof(Dn2CppBpiInsn) != 0)
        interp_fail("BPI: misaligned code offset");
    uint32_t first = m.codeOff / sizeof(Dn2CppBpiInsn);
    if (first + static_cast<uint64_t>(m.codeInsnCount) > img->insnCount)
        interp_fail("BPI: method code out of bounds");
    const Dn2CppBpiInsn* code = img->code + first;

    // The method's EHTable run, bounds-checked up front so the dispatch-loop
    // scans stay branch-light.
    const Dn2CppBpiEH* eh = nullptr;
    if (m.ehCount > 0)
    {
        if (m.ehCount > kMaxEH)
            interp_fail("interp: more than 32 EH records per method needs a later slice");
        if (img->eh == nullptr
            || m.ehStart + static_cast<uint64_t>(m.ehCount) > img->ehCount)
            interp_fail("BPI: EH table run out of bounds");
        eh = img->eh + m.ehStart;
        for (uint16_t i = 0; i < m.ehCount; i++)
        {
            const Dn2CppBpiEH& r = eh[i];
            if (r.kind == 1)
                interp_fail("interp: filter clauses are outside the interpreted surface");
            if (r.kind != 0 && r.kind != 2 && r.kind != 3)
                interp_fail("BPI: unknown EH clause kind");
            if (r.tryStart >= r.tryEnd || r.tryEnd > m.codeInsnCount
                || r.handlerStart >= r.handlerEnd || r.handlerEnd > m.codeInsnCount)
                interp_fail("BPI: EH clause range out of bounds");
        }
    }

    if (img->regCode)
    {
        // Register-format frame: one flat register file — args, locals, then
        // the eval-temp region (regCount = slotCount + maxStack ≤ 128, both
        // limits checked above and at load). Zero-initialized like the v1
        // frame (IL initlocals) and native-stack-resident, so reference
        // registers stay visible to the conservative collector; the frame's
        // stack/sp members are unused under this format.
        Slot regs[kMaxFrame + kMaxStack] = {};
        for (uint32_t i = 0; i < m.argCount; i++)
            regs[i] = args[i];
        Dn2CppObject* handlerExc[kMaxEH] = {};
        InterpFrame f{ img, &m, code, eh, regs, nullptr, handlerExc, 0, depth };
        ExecResult r = interp_run_reg(f, 0);
        if (r.kind != ExecKind::Return)
            interp_fail("BPI: endfinally outside a running finally handler");
        return r.value;
    }

    Slot frame[kMaxFrame] = {}; // args, then locals (zero-initialized: IL initlocals)
    for (uint32_t i = 0; i < m.argCount; i++)
        frame[i] = args[i];
    Slot stack[kMaxStack];
    Dn2CppObject* handlerExc[kMaxEH] = {};
    InterpFrame f{ img, &m, code, eh, frame, stack, handlerExc, 0, depth };
    ExecResult r = interp_run(f, 0);
    if (r.kind != ExecKind::Return)
        interp_fail("BPI: endfinally outside a running finally handler");
    return r.value;
}

} // namespace

Dn2CppInterpSlot dn2cpp_interp_vcall(int32_t slot, const Dn2CppInterpSlot* args, int32_t argCount)
{
    // Called only from the emitted N2M trampolines: the receiver reached a
    // patched vtable slot, so its type is a loader-constructed patch type-info
    // (only those ever hold trampolines) and the AOT call site has already
    // dereferenced it. The magic check turns a violated invariant into a
    // diagnosable failure.
    if (args == nullptr || argCount < 1)
        interp_fail("interp: vtable dispatch without a receiver");
    auto* self = static_cast<Dn2CppObject*>(args[0].ref);
    if (self == nullptr || self->type == nullptr)
        interp_fail("interp: vtable dispatch on a null receiver");
    const auto* iti = reinterpret_cast<const InterpTypeInfo*>(self->type);
    if (iti->magic != kInterpTypeInfoMagic)
        interp_fail("interp: vtable dispatch receiver is not an interpreter-constructed type");
    const Dn2CppInterpImage* img = iti->img;
    // The magic stamp above makes this non-null by construction; tested anyway,
    // as dn2cpp_interp_dgcall tests its own — three sibling entry points reading
    // the same field should not disagree about whether it needs a check.
    if (img == nullptr || iti->typeIdx >= img->typeCount)
        interp_fail("BPI: vtable dispatch type index out of bounds");

    // (receiver type, slot) -> the overriding patch method, walking the patch
    // base chain: a patch-derived type without its own re-override inherits
    // its ancestor's (their vtables share the same trampoline for the slot).
    uint32_t methodIdx = resolve_override_method(img, iti->typeIdx, static_cast<uint32_t>(slot));
    if (methodIdx == DN2CPP_BPI_REF_NONE || methodIdx >= img->methodCount)
        interp_fail("interp: vtable slot has no interpreted override");
    if (img->methods[methodIdx].argCount != static_cast<uint32_t>(argCount))
        interp_fail("interp: vtable dispatch argument count mismatch");

    // The trampoline's args array lives on the native stack (conservatively
    // scanned), and interp_call copies it into its own frame; the interpreted
    // override runs like any patch method — an exception it throws unwinds
    // through the trampoline and the AOT caller frame on the one machine.
    interp_ensure_cctor(img, img->methods[methodIdx].declTypeIdx, 0);
    return interp_call(img, methodIdx, args, 0);
}

Dn2CppInterpSlot dn2cpp_interp_itfcall(const Dn2CppTypeInfo* itf, int32_t slot,
    const Dn2CppInterpSlot* args, int32_t argCount)
{
    // Called only from the emitted N2M interface trampolines: the receiver
    // reached a patch type's interface-dispatch slot, so its type is a
    // loader-constructed patch type-info. (itf, slot) identifies the interface
    // method; the receiver's ItfDesc chain resolves it to the implementing patch
    // method (a derived patch type's re-implementation wins).
    if (args == nullptr || argCount < 1)
        interp_fail("interp: interface dispatch without a receiver");
    auto* self = static_cast<Dn2CppObject*>(args[0].ref);
    if (self == nullptr || self->type == nullptr)
        interp_fail("interp: interface dispatch on a null receiver");
    const auto* iti = reinterpret_cast<const InterpTypeInfo*>(self->type);
    if (iti->magic != kInterpTypeInfoMagic)
        interp_fail("interp: interface dispatch receiver is not an interpreter-constructed type");
    const Dn2CppInterpImage* img = iti->img;
    // As the vtable entry point: checked for symmetry with dgcall's own check.
    if (img == nullptr || iti->typeIdx >= img->typeCount)
        interp_fail("BPI: interface dispatch type index out of bounds");

    uint32_t methodIdx = resolve_itf_override(img, iti->typeIdx, itf, static_cast<uint32_t>(slot));
    if (methodIdx == DN2CPP_BPI_REF_NONE || methodIdx >= img->methodCount)
        interp_fail("interp: interface slot has no interpreted implementation");
    if (img->methods[methodIdx].argCount != static_cast<uint32_t>(argCount))
        interp_fail("interp: interface dispatch argument count mismatch");

    interp_ensure_cctor(img, img->methods[methodIdx].declTypeIdx, 0);
    return interp_call(img, methodIdx, args, 0);
}

Dn2CppInterpSlot dn2cpp_interp_dgcall(Dn2CppObject* closureObj,
    const Dn2CppInterpSlot* args, int32_t argCount)
{
    // Called only from the emitted delegate thunks: closureObj is the closure a
    // patch method bound into a delegate carries (image + patch method + bound
    // this), args the marshalled invoke arguments (excluding the bound this).
    // Prepend the captured receiver for an instance patch method, then run the
    // method as an ordinary interpreted frame — an exception it throws unwinds
    // through the thunk and the AOT caller frame on the one machine.
    auto* c = reinterpret_cast<Dn2CppInterpDgFtn*>(closureObj);
    if (c == nullptr || c->magic != kDgFtnMagic || c->kind != kDgFtnPatch)
        interp_fail("interp: delegate dispatch on a non-interpreter closure");
    const Dn2CppInterpImage* img = c->img;
    uint32_t methodIdx = c->methodIdx;
    if (img == nullptr || methodIdx >= img->methodCount)
        interp_fail("BPI: delegate dispatch method index out of bounds");
    if (argCount < 0 || argCount > kMaxFrame - 1)
        interp_fail("interp: delegate dispatch argument count out of range");
    uint32_t need = img->methods[methodIdx].argCount;
    Slot buf[kMaxFrame];
    if (need == static_cast<uint32_t>(argCount) + 1)
    {
        // An instance patch method: the captured receiver leads the frame, so it
        // answers the receiver question. A null one is left alone — the degrade
        // for an absent Invoke row on the delegate the capture was made against,
        // where the interpreted body's own guards raise the catchable NRE.
        if (c->boundThis != nullptr && !patch_receiver_ok(img, methodIdx, c->boundThis))
            interp_fail(kPatchRecvFail);
        buf[0].ref = c->boundThis;
        for (int32_t i = 0; i < argCount; i++)
            buf[1 + i] = args[i];
    }
    else if (need == static_cast<uint32_t>(argCount))
    {
        // A STATIC frame, derived from arity alone (the MethodTable record
        // carries no static bit). A captured receiver here is a contradiction —
        // the `newobj` recorded a `this` and this frame has no slot for one — so
        // the arity that chose this branch is not the arity the capture was made
        // against, which is what a relabelled delegate TYPE import produces. The
        // consequence is not a dropped argument: the calling thunk packed one
        // argument too many, so args[0] is whatever the caller's ABI left in the
        // register and it lands in the slot the body reads as its first
        // parameter. Refuse here rather than at whichever `ldfld`/`call` first
        // dereferences it.
        if (c->boundThis != nullptr)
            interp_fail("interp: a delegate closure with a captured receiver reached a static patch frame");
        for (int32_t i = 0; i < argCount; i++)
            buf[i] = args[i];
    }
    else
    {
        interp_fail("interp: delegate dispatch argument count mismatch");
    }
    interp_ensure_cctor(img, img->methods[methodIdx].declTypeIdx, 0);
    return interp_call(img, methodIdx, buf, 0);
}

void dn2cpp_interp_exec(const Dn2CppInterpImage* img, uint32_t methodIdx)
{
    if (img == nullptr || methodIdx >= img->methodCount)
        interp_fail("interp: method index out of range");
    if (img->methods[methodIdx].argCount != 0)
        interp_fail("interp: the entry method must be parameterless");
    // Entering from outside any patch type: the target type's static
    // constructor runs first, like any other call into the type.
    interp_ensure_cctor(img, img->methods[methodIdx].declTypeIdx, 0);
    interp_call(img, methodIdx, nullptr, 0);
}

namespace
{

// UTF-8 copy of a managed path string, NUL-terminated. Atomic GC memory: the
// copy holds no pointers and dies with the call.
char* hotupdate_path_utf8(Dn2CppString* path)
{
    if (path == nullptr)
        dn2cpp_throw_argument_null();
    int32_t utf8Len = dn2cpp_string_to_utf8(path, nullptr, 0);
    char* buf = static_cast<char*>(dn2cpp_alloc_atomic(static_cast<size_t>(utf8Len) + 1));
    if (utf8Len > 0)
        dn2cpp_string_to_utf8(path, buf, utf8Len);
    buf[utf8Len] = '\0';
    return buf;
}

// Whole-reads the file at `path` into a fresh GC buffer, or returns null with
// *err set to the failure message (the caller decides whether that fails the
// load or skips the file). GC memory: interpreted strings alias the blob, so
// it must be a collector object kept alive through the image (the loader
// chains the image into the static root list). Pointer-free (atomic): the
// blob holds no pointers, so it need not be scanned.
uint8_t* hotupdate_read_file(const char* path, size_t* outLen, const char** err)
{
    *outLen = 0;
    FILE* fp = std::fopen(path, "rb");
    if (fp == nullptr)
    {
        *err = "hotupdate: cannot open the BPI file";
        return nullptr;
    }
    std::fseek(fp, 0, SEEK_END);
    long size = std::ftell(fp);
    std::rewind(fp);
    if (size <= 0)
    {
        std::fclose(fp);
        *err = "hotupdate: empty BPI file";
        return nullptr;
    }
    // fread into a malloc'd staging buffer, then copy into the GC block — NEVER
    // fread straight into GC memory. A large fread issues read(2) directly, and
    // under the incremental collector a kernel write into a write-protected heap
    // page returns EFAULT instead of faulting into bdwgc's handler (mechanism at
    // dn2cpp_gc_kernel_write_unsafe); incremental is the default in the Godot
    // lane, which is where hot-update runs. The memcpy below is an ordinary
    // user-space store, which does fault and is handled.
    auto* staging = static_cast<uint8_t*>(std::malloc(static_cast<size_t>(size)));
    if (staging == nullptr)
    {
        std::fclose(fp);
        *err = "hotupdate: out of memory reading the BPI file";
        return nullptr;
    }
    size_t got = std::fread(staging, 1, static_cast<size_t>(size), fp);
    std::fclose(fp);
    if (got != static_cast<size_t>(size))
    {
        std::free(staging);
        *err = "hotupdate: short read on the BPI file";
        return nullptr;
    }
    auto* blob = static_cast<uint8_t*>(dn2cpp_alloc_atomic(static_cast<size_t>(size)));
    std::memcpy(blob, staging, static_cast<size_t>(size));
    std::free(staging);
    *outLen = static_cast<size_t>(size);
    return blob;
}

// Shared managed-path front end of the single-file drivers: UTF-8 conversion,
// whole read, load. IO failures are managed NotSupportedExceptions like every
// other loader failure.
Dn2CppInterpImage* hotupdate_load_path(Dn2CppString* bpiPath)
{
    char* path = hotupdate_path_utf8(bpiPath);
    size_t len = 0;
    const char* err = nullptr;
    uint8_t* blob = hotupdate_read_file(path, &len, &err);
    if (blob == nullptr)
        interp_fail(err);
    return dn2cpp_patch_load(blob, len);
}

} // namespace

void dn2cpp_hotupdate_run(Dn2CppString* bpiPath)
{
    Dn2CppInterpImage* img = hotupdate_load_path(bpiPath);
    if (img->entryMethodIdx == DN2CPP_BPI_REF_NONE)
        interp_fail("hotupdate: the BPI has no entry method");
    dn2cpp_interp_exec(img, img->entryMethodIdx);
}

void dn2cpp_hotupdate_load(Dn2CppString* bpiPath)
{
    Dn2CppInterpImage* img = hotupdate_load_path(bpiPath);
    // Unlike the run driver, the entry method is optional: a registration-only
    // BPI (types/overrides, no installer code) is a valid deployment unit.
    if (img->entryMethodIdx != DN2CPP_BPI_REF_NONE)
        dn2cpp_interp_exec(img, img->entryMethodIdx);
}

int32_t dn2cpp_hotupdate_load_dir(Dn2CppString* dirPath)
{
    char* dir = hotupdate_path_utf8(dirPath);

    // A missing or unopenable patch directory is the normal fresh-install
    // state — nothing deployed yet — not an error.
    DIR* dp = opendir(dir);
    if (dp == nullptr)
        return 0;

    // Candidate accumulator. Load-time allocation is fine (only interpreter
    // *execution* is allocation-free); the array is GC-scanned so its blob
    // pointers keep the blobs alive until they are chained into images.
    struct Candidate
    {
        uint8_t* blob;
        size_t len;
        uint32_t version;
        char* name; // filename copy: the deterministic sort tie-break
    };
    size_t count = 0;
    size_t cap = 8;
    auto* cands = static_cast<Candidate*>(dn2cpp_alloc(cap * sizeof(Candidate)));

    size_t dirLen = std::strlen(dir);
    for (struct dirent* e = readdir(dp); e != nullptr; e = readdir(dp))
    {
        // Only regular files named `*.bpi` (byte-wise, lowercase) are
        // deployment candidates; everything else in the directory is ignored.
        const char* name = e->d_name;
        size_t nameLen = std::strlen(name);
        if (nameLen < 4 || std::memcmp(name + nameLen - 4, ".bpi", 4) != 0)
            continue;
        char* full = static_cast<char*>(dn2cpp_alloc_atomic(dirLen + 1 + nameLen + 1));
        std::memcpy(full, dir, dirLen);
        full[dirLen] = '/';
        std::memcpy(full + dirLen + 1, name, nameLen + 1);
        struct stat st;
        if (stat(full, &st) != 0 || !S_ISREG(st.st_mode))
            continue;

        // Read once, then pre-validate the HEADER only. Anything failing here
        // is the routine stale-deployment state (e.g. patches baked against a
        // previous base build): skip it with a diagnostic — on stderr, since
        // stale patches must never block startup and stdout belongs to the
        // program. Failures past this header check throw hard below.
        size_t len = 0;
        const char* skip = nullptr;
        uint8_t* blob = hotupdate_read_file(full, &len, &skip);
        if (blob != nullptr)
        {
            const auto* h = reinterpret_cast<const Dn2CppBpiHeader*>(blob);
            if (len < sizeof(Dn2CppBpiHeader))
                skip = "shorter than a BPI header";
            else if (std::memcmp(h->magic, "DN2BPI\0", 8) != 0)
                skip = "bad BPI magic";
            else if (h->formatVersion != DN2CPP_BPI_VERSION)
                skip = "unsupported BPI format version";
            else if ((h->flags & ~DN2CPP_BPI_FLAGS_SUPPORTED) != 0)
                skip = "unsupported BPI header flags";
            else if (h->baseImageAbiHash != dn2cpp_base_image_abi_hash)
                skip = "stale BPI (base ABI hash mismatch)";
        }
        if (skip != nullptr)
        {
            std::fprintf(stderr, "dn2cpp hotupdate: skipping %s: %s\n", full, skip);
            continue;
        }

        if (count == cap)
        {
            cap *= 2;
            auto* grown = static_cast<Candidate*>(dn2cpp_alloc(cap * sizeof(Candidate)));
            std::memcpy(grown, cands, count * sizeof(Candidate));
            cands = grown;
        }
        char* nameCopy = static_cast<char*>(dn2cpp_alloc_atomic(nameLen + 1));
        std::memcpy(nameCopy, name, nameLen + 1);
        cands[count].blob = blob;
        cands[count].len = len;
        cands[count].version = reinterpret_cast<const Dn2CppBpiHeader*>(blob)->patchVersion;
        cands[count].name = nameCopy;
        count++;
    }
    closedir(dp);

    // Deterministic apply order — never trust readdir order: ascending
    // patchVersion, byte-wise filename comparison as the tie-break. The
    // highest version therefore registers last and wins name lookups
    // (append-only newest-wins registry).
    for (size_t i = 1; i < count; i++)
    {
        Candidate c = cands[i];
        size_t j = i;
        while (j > 0 && (cands[j - 1].version > c.version ||
                         (cands[j - 1].version == c.version &&
                          std::strcmp(cands[j - 1].name, c.name) > 0)))
        {
            cands[j] = cands[j - 1];
            j--;
        }
        cands[j] = c;
    }

    // Apply in order. Past the header pre-validation, a failure is corruption
    // or a converter bug — not staleness — so it throws like a single-file
    // load; a mid-directory throw leaves the earlier images published (loading
    // is per-BPI atomic, append-only).
    for (size_t i = 0; i < count; i++)
    {
        Dn2CppInterpImage* img = dn2cpp_patch_load(cands[i].blob, cands[i].len);
        if (img->entryMethodIdx != DN2CPP_BPI_REF_NONE)
            dn2cpp_interp_exec(img, img->entryMethodIdx);
    }
    return static_cast<int32_t>(count);
}

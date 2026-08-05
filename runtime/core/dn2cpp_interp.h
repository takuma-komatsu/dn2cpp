// dn2cpp_interp.h — hot-update runtime surface: the Baked Patch Image (BPI)
// on-disk format plus the loader / interpreter entry points.
//
// Binary layout per docs/BPI-FORMAT.md (v1). All multi-byte integers are
// little-endian; sections start on 16-byte boundaries; every cross-reference
// inside the blob is a table index or a blob-relative byte offset — never an
// absolute pointer — so a whole-read/mmap'd blob executes in place with no
// relocation and no per-instruction allocation.
//
// This header (and the dn2cpp_interp.cpp TU) is referenced only by
// hot-update-enabled programs: the runtime library is a static archive, so a
// program that never calls dn2cpp_hotupdate_run never links this object.
#pragma once

#include <cstddef>
#include <cstdint>

// ---- constants ----

// Header magic: "DN2BPI\0\0" (8 bytes including the two NULs).
#define DN2CPP_BPI_MAGIC "DN2BPI\0"
#define DN2CPP_BPI_VERSION 1u

// Header.flags bits (offset 12). bit0 = register-based bytecode.
#define DN2CPP_BPI_FLAG_REGCODE 0x1u
// Mask of flag bits this runtime understands: a BPI carrying any other bit
// was baked for a newer runtime and must be rejected (or skipped by the
// directory loader), never silently run through the stack interpreter.
#define DN2CPP_BPI_FLAGS_SUPPORTED DN2CPP_BPI_FLAG_REGCODE

// Tagged reference EntityRef (u32): bits 30-31 = tag, bits 0-29 = index (or a
// PrimitiveTypeCode for the Primitive tag). Sentinel 0xFFFFFFFF = none.
#define DN2CPP_BPI_TAG_PATCH 0u
#define DN2CPP_BPI_TAG_IMPORT 1u
#define DN2CPP_BPI_TAG_PRIM 2u
#define DN2CPP_BPI_REF_TAG(r) ((uint32_t)(r) >> 30)
#define DN2CPP_BPI_REF_INDEX(r) ((uint32_t)(r) & 0x3FFFFFFFu)
#define DN2CPP_BPI_REF_NONE 0xFFFFFFFFu

// Section kinds (SectionTable.kind).
enum Dn2CppBpiSectionKind : uint32_t
{
    DN2CPP_BPI_SEC_STRINGPOOL = 1,     // UTF-8 name arena: { u16 len; u8 utf8[len] }
    DN2CPP_BPI_SEC_IMPORTTABLE = 2,    // Dn2CppBpiImport[]
    DN2CPP_BPI_SEC_TYPETABLE = 3,      // Dn2CppBpiType[]
    DN2CPP_BPI_SEC_FIELDTABLE = 4,     // Dn2CppBpiField[]
    DN2CPP_BPI_SEC_METHODTABLE = 5,    // Dn2CppBpiMethod[]
    DN2CPP_BPI_SEC_CODE = 6,           // Dn2CppBpiInsn[]
    DN2CPP_BPI_SEC_EHTABLE = 7,        // Dn2CppBpiEH[]
    DN2CPP_BPI_SEC_VTABLEDESC = 8,     // { u32 vtableLen; u32 overrideCount;
                                       //   { u32 slot; u32 patchMethodIdx }[overrideCount] }
    DN2CPP_BPI_SEC_LOCALSIG = 9,       // flat u32 EntityRef[] (arg+local type refs)
    DN2CPP_BPI_SEC_USERSTRINGS = 10,   // UTF-16 ldstr arena: { u32 charLen; u16 utf16[charLen] }
    DN2CPP_BPI_SEC_SWITCHTABLE = 11,   // flat u32[] (instruction indices)
    DN2CPP_BPI_SEC_ITFDESC = 12,       // interface-implementation descriptions for patch types:
                                       //   { u32 itfCount;
                                       //     { u32 itfImport; u32 fullSlotCount; u32 implCount;
                                       //       { u32 slot; u32 patchMethodIdx }[implCount] }[itfCount] }
};

// ---- on-disk records (fixed layouts, v1) ----

// Header (64 bytes, fixed).
struct Dn2CppBpiHeader
{
    char magic[8];             // "DN2BPI\0\0"
    uint32_t formatVersion;    // +1 on layout-incompatible change
    uint32_t flags;            // bit0 = register-based bytecode (the register
                               // interpretation of the CodeSection records);
                               // loaders enforce DN2CPP_BPI_FLAGS_SUPPORTED
    uint64_t baseImageAbiHash; // must equal dn2cpp_base_image_abi_hash
    uint32_t sectionCount;
    uint32_t sectionTableOff;  // blob-relative
    uint32_t patchTypeCount;   // TypeTable record count (for bind allocation)
    uint32_t importCount;      // ImportTable record count (for bind allocation)
    uint32_t entryMethodIdx;   // patch entry point (0xFFFFFFFF = none)
    uint32_t patchVersion;     // deployment ordering key (0 = unversioned default)
    uint8_t reserved[16];      // zero-filled; claimed front-to-back by additive
                               // fields whose zero value means pre-field behavior
};
static_assert(sizeof(Dn2CppBpiHeader) == 64, "BPI header is 64 bytes");

// SectionTable entry (16 bytes, fixed).
struct Dn2CppBpiSection
{
    uint32_t kind;   // Dn2CppBpiSectionKind
    uint32_t offset; // blob-relative
    uint32_t length; // bytes
    uint32_t count;  // record count; 0 for byte arenas
};
static_assert(sizeof(Dn2CppBpiSection) == 16, "BPI section entry is 16 bytes");

// ImportTable record (24 bytes): an external reference bound to the base image
// at load time (-> ImportBinding).
struct Dn2CppBpiImport
{
    uint32_t kind;           // 1=Type / 2=Method / 3=Field
    uint32_t nameOff;        // StringPool (CLR FullName for types, simple name for members)
    uint32_t declTypeImport; // Import index of a member's declaring type (0xFFFFFFFF for Type)
    uint32_t sigShapeOff;    // StringPool; overload discrimination + bridge ABI shape
    uint32_t aux0;           // Method: parameter count (excluding this) in the low
                             // 16 bits, bit16 = instance (a this slot precedes the
                             // args). Else 0.
    uint32_t aux1;           // Method: LocalSigTable index of the [return, params...]
                             // EntityRef run (paramCount+1 entries; return is
                             // 0xFFFFFFFF for void). Field: the field type's
                             // EntityRef. Else 0.
};
static_assert(sizeof(Dn2CppBpiImport) == 24, "BPI import record is 24 bytes");

#define DN2CPP_BPI_IMPORT_TYPE 1u
#define DN2CPP_BPI_IMPORT_METHOD 2u
#define DN2CPP_BPI_IMPORT_FIELD 3u

// TypeTable record (32 bytes): a patch-defined type.
struct Dn2CppBpiType
{
    uint32_t nameOff;
    uint32_t baseRef;       // EntityRef (Import = AOT base / none = object; a patch
                            // base is a later slice)
    uint32_t flags;         // mirror of DN2CPP_TF_* + interp-specific bits
    uint32_t itfDescOff;    // into ItfDescTable (interface-implementation overrides for
                            // this patch type; 0xFFFFFFFF = implements no interface).
                            // (Was the always-zero instanceSize slot — the loader owns
                            // the numeric layout and computes the size at load, so the
                            // record never carried a size and this word was free.)
    uint32_t fieldStartCount; // fieldStart in the high 16 bits, fieldCount in the low 16
    uint32_t methodStart;
    uint32_t methodCount;
    uint32_t vtableDescOff; // into VtableDescTable; 0xFFFFFFFF = no overrides
};
static_assert(sizeof(Dn2CppBpiType) == 32, "BPI type record is 32 bytes");

// TypeRecord.flags interp-specific bits (above the DN2CPP_TF_* mirror range).
// has-cctor: the type declares a static constructor, baked as the FIRST entry
// of its MethodTable run (MethodTable[methodStart]); the interpreter runs it
// lazily, exactly once, through the per-type guard.
#define DN2CPP_BPI_TF_HAS_CCTOR 0x80000000u

// FieldTable record (20 bytes).
struct Dn2CppBpiField
{
    uint32_t nameOff;
    uint32_t typeRef;    // EntityRef
    uint32_t offset;     // instance: the field's ordinal among its type's instance
                         // fields (declaration order) — byte offsets are assigned
                         // by the loader against the live base layout. 0 for statics
    uint32_t flags;      // static/instance/literal...
    uint32_t staticSlot; // static-storage index for statics, 0xFFFFFFFF for instance
};
static_assert(sizeof(Dn2CppBpiField) == 20, "BPI field record is 20 bytes");

// MethodTable record (40 bytes).
struct Dn2CppBpiMethod
{
    uint32_t nameOff;
    uint32_t declTypeIdx;  // patch type index
    uint32_t sigShapeOff;  // StringPool; bridge ABI shape + overload discrimination
    uint16_t maxStack;
    uint16_t localCount;
    uint32_t argCount;     // including this
    uint32_t localSigOff;  // into LocalSigTable (start of the arg+local type-reference run)
    uint32_t codeOff;      // byte offset into CodeSection
    uint32_t codeInsnCount;
    uint32_t ehStart;      // EHTable index
    uint16_t ehCount;
    uint16_t vtableSlot;   // occupied slot; 0xFFFF if non-virtual
};
static_assert(sizeof(Dn2CppBpiMethod) == 40, "BPI method record is 40 bytes");

// One pre-resolved instruction (12 bytes, fixed). `op` is the raw ECMA-335
// opcode value (u16; two-byte opcodes encode as 0xFExx), with short forms
// normalized to their canonical long form by the converter (ldc.i4.3 ->
// ldc.i4 a=3, br.s -> br, ldarg.0 -> ldarg a=0, ...). All operands are
// resolved at conversion time — see docs/BPI-FORMAT.md for the per-opcode
// meaning of `a`/`b`.
struct Dn2CppBpiInsn
{
    uint16_t op;
    uint16_t pfx; // prefix bits (volatile/tail/unaligned); constrained type ref rides in b.
                  // Register-format images (Header.flags bit0) reuse this field for the
                  // two register operands: low byte = r0, high byte = r1, 0xFF = unused
    uint32_t a;   // primary operand
    uint32_t b;   // secondary operand
};
static_assert(sizeof(Dn2CppBpiInsn) == 12, "BPI instruction is 12 bytes");

// EHTable record (28 bytes). All ranges are instruction indices.
struct Dn2CppBpiEH
{
    uint32_t kind; // 0=catch / 1=filter / 2=finally / 3=fault
    uint32_t tryStart;
    uint32_t tryEnd;
    uint32_t handlerStart;
    uint32_t handlerEnd;
    uint32_t filterStart;   // or 0xFFFFFFFF
    uint32_t catchTypeRef;  // EntityRef or 0xFFFFFFFF
};
static_assert(sizeof(Dn2CppBpiEH) == 28, "BPI EH record is 28 bytes");

// ---- runtime API ----

// A loaded patch image: the blob plus its bind results (ImportBinding array,
// patch-type infos). Opaque to generated code; defined in dn2cpp_interp.cpp.
struct Dn2CppInterpImage;

// Validate a BPI blob (magic / formatVersion / baseImageAbiHash — a mismatch
// throws NotSupportedException), then run the one-shot bind pass. The blob is
// referenced in place (zero-copy: code, strings, metadata) and the image is
// kept on an append-only root list, so blob memory must be GC-visible — pass
// memory allocated with dn2cpp_alloc.
Dn2CppInterpImage* dn2cpp_patch_load(const void* blob, size_t len);

// Execute a patch method by MethodTable index. The public entry runs static
// parameterless methods (a patch entry point); intra-patch calls with
// scalar/string arguments and returns are handled internally by the
// interpreter (the converter enforces the same fence).
void dn2cpp_interp_exec(const Dn2CppInterpImage* img, uint32_t methodIdx);

// The base image's ABI-contract hash, emitted as a constant into generated.cpp
// by a --hotupdate-base build (see docs/BPI-FORMAT.md "baseImageAbiHash").
// Undefined in normal builds — but only referenced from this TU, which only a
// hot-update-enabled program links.
extern "C" const uint64_t dn2cpp_base_image_abi_hash;

// The N2M vtable trampoline registration table, emitted into generated.cpp by
// a --hotupdate-base build (one row per (slot, sigShape) pair over the emitted
// base-image vtables; the struct is defined in dn2cpp.h — forward-declared
// here since this header stays self-contained). The loader scans it when
// applying a patch type's vtable-slot overrides. Same linkage rule as the ABI
// hash above: undefined in normal builds, referenced only from this TU.
struct Dn2CppN2MTrampoline;
extern "C" const Dn2CppN2MTrampoline dn2cpp_n2m_trampolines[];
extern "C" const int32_t dn2cpp_n2m_trampoline_count;

// The N2M interface trampoline registration table, emitted into generated.cpp
// by a --hotupdate-base build (one row per (emitted interface, method slot)
// pair whose signature is bridgeable). Each trampoline has the interface
// method's AOT C++ ABI and forwards into dn2cpp_interp_itfcall; the loader
// installs it into a patch type's interface-dispatch slot when the patch type
// implements that interface method with an interpreted body. Same linkage rule
// as the vtable table above: undefined in normal builds, referenced only from
// this TU.
struct Dn2CppN2MItfTrampoline;
extern "C" const Dn2CppN2MItfTrampoline dn2cpp_n2m_itf_trampolines[];
extern "C" const int32_t dn2cpp_n2m_itf_trampoline_count;

// The delegate bridge registration tables, emitted into generated.cpp by a
// --hotupdate-base build (one row per bridgeable emitted delegate type, keyed
// by its type-info). dn2cpp_n2m_delegate_thunks[] holds the native f_method a
// patch method bound into a delegate uses (its body forwards into
// dn2cpp_interp_dgcall); dn2cpp_dg_invoke_bridges[] holds the reverse bridge an
// interpreted Invoke calls to invoke the delegate through its emitted invoker.
// Same linkage rule as the trampoline tables above: undefined in normal builds,
// referenced only from this TU. The struct is defined in dn2cpp.h.
struct Dn2CppN2MDelegate;
extern "C" const Dn2CppN2MDelegate dn2cpp_n2m_delegate_thunks[];
extern "C" const int32_t dn2cpp_n2m_delegate_thunk_count;
extern "C" const Dn2CppN2MDelegate dn2cpp_dg_invoke_bridges[];
extern "C" const int32_t dn2cpp_dg_invoke_bridge_count;

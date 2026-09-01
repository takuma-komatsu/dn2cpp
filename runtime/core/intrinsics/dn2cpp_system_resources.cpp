// dn2cpp_system_resources.cpp — System.Resources.ResourceManager, served at run time
// by reading the assembly's `<BaseName>.resources` blob out of .rodata.
//
// ResourceManager is an intrinsic rather than transpiled IL because its real lookup
// needs a CultureInfo.Parent chain and a satellite-assembly loader, neither of which
// exists here.
//
// This is one of TWO hand-written readers of the .resources wire format; the other is
// src/Dn2Cpp.Transpiler/ResourceStrings.cs, at transpile time over a PEReader blob.
// Neither can call the other, and they differ deliberately in strictness: a misparse
// there loses an exception message, while a misparse here would ANSWER — so this
// reader refuses an unknown RuntimeResourceSet version rather than decoding it under
// the wrong rules.
#include "dn2cpp_core.h"

#include <cstdint>
#include <cstdio>
#include <cstring>
#include <string>

// ===== The RuntimeResourceSet wire format ====================================
// A bounds-checked cursor over the resource blob. Every read tests remaining length
// and latches `bad` on overrun instead of trapping: the blob is .rodata the emitter
// wrote, so a malformed one is a transpiler bug, but reading past it would be an
// out-of-bounds read of the binary's own image and those do not fail loudly.
namespace {

struct BlobCursor
{
    const uint8_t* p;
    int32_t len;
    int32_t off;
    bool bad;

    BlobCursor(const uint8_t* data, int32_t length)
        : p(data), len(length), off(0), bad(data == nullptr && length != 0) {}

    bool Have(int32_t n) const { return !bad && n >= 0 && off <= len && (len - off) >= n; }

    uint32_t U32()
    {
        if (!Have(4)) { bad = true; return 0; }
        uint32_t v = static_cast<uint32_t>(p[off])
                   | (static_cast<uint32_t>(p[off + 1]) << 8)
                   | (static_cast<uint32_t>(p[off + 2]) << 16)
                   | (static_cast<uint32_t>(p[off + 3]) << 24);
        off += 4;
        return v;
    }

    int32_t I32() { return static_cast<int32_t>(U32()); }

    uint8_t U8()
    {
        if (!Have(1)) { bad = true; return 0; }
        return p[off++];
    }

    uint16_t U16()
    {
        if (!Have(2)) { bad = true; return 0; }
        uint16_t v = static_cast<uint16_t>(static_cast<uint16_t>(p[off])
                   | (static_cast<uint16_t>(p[off + 1]) << 8));
        off += 2;
        return v;
    }

    uint64_t U64()
    {
        uint64_t lo = U32();
        uint64_t hi = U32();
        return lo | (hi << 32);
    }

    int64_t I64() { return static_cast<int64_t>(U64()); }

    // The value blob stores Single/Double as their raw little-endian IEEE bits (what
    // BinaryWriter writes), so the read is a bit reinterpretation and not a conversion.
    float F4()
    {
        uint32_t bits = U32();
        float f;
        std::memcpy(&f, &bits, sizeof(f));
        return f;
    }

    double F8()
    {
        uint64_t bits = U64();
        double d;
        std::memcpy(&d, &bits, sizeof(d));
        return d;
    }

    // BinaryReader.Read7BitEncodedInt — little-endian 7 bits per byte, high bit the
    // continuation flag. NOT the ECMA compressed integer (a different, big-endian
    // scheme); the transpiler-side reader spells this out for the same reason.
    int32_t Packed7()
    {
        int32_t value = 0, shift = 0;
        uint8_t b;
        do
        {
            if (shift >= 35) { bad = true; return -1; }
            b = U8();
            if (bad) return -1;
            value |= static_cast<int32_t>(b & 0x7F) << shift;
            shift += 7;
        } while ((b & 0x80) != 0);
        return value;
    }

    void Seek(int32_t to)
    {
        if (to < 0 || to > len) { bad = true; return; }
        off = to;
    }
};

// System.Resources.ResourceTypeCode — the per-entry tag the value section carries.
// Everything up to TimeSpan is a primitive whose payload the wire format spells out.
// ByteArray and Stream are a length-prefixed run of bytes. Anything at or past
// StartOfUserTypes indexes the set's type table and its payload is a BinaryFormatter
// graph — permanently out of scope, and refused rather than guessed at.
enum : int32_t
{
    kRtcNull = 0,
    kRtcString = 1,
    kRtcBoolean = 2,
    kRtcChar = 3,
    kRtcByte = 4,
    kRtcSByte = 5,
    kRtcInt16 = 6,
    kRtcUInt16 = 7,
    kRtcInt32 = 8,
    kRtcUInt32 = 9,
    kRtcInt64 = 10,
    kRtcUInt64 = 11,
    kRtcSingle = 12,
    kRtcDouble = 13,
    kRtcDecimal = 14,
    kRtcDateTime = 15,
    kRtcTimeSpan = 16,
    kRtcByteArray = 0x20,
    kRtcStream = 0x21,
    kRtcStartOfUserTypes = 0x40,
};

// The .NET type name a decodable code denotes, for the two messages that have to name
// it (GetString's "not a String" and GetObject's refusal); null for a code with no
// fixed type. Kept beside the enum so a widened table cannot forget the name.
const char* RtcTypeName(int32_t code)
{
    switch (code)
    {
        case kRtcNull:      return "null";
        case kRtcString:    return "String";
        case kRtcBoolean:   return "Boolean";
        case kRtcChar:      return "Char";
        case kRtcByte:      return "Byte";
        case kRtcSByte:     return "SByte";
        case kRtcInt16:     return "Int16";
        case kRtcUInt16:    return "UInt16";
        case kRtcInt32:     return "Int32";
        case kRtcUInt32:    return "UInt32";
        case kRtcInt64:     return "Int64";
        case kRtcUInt64:    return "UInt64";
        case kRtcSingle:    return "Single";
        case kRtcDouble:    return "Double";
        case kRtcDecimal:   return "Decimal";
        case kRtcDateTime:  return "DateTime";
        case kRtcTimeSpan:  return "TimeSpan";
        case kRtcByteArray: return "Byte[]";
        case kRtcStream:    return "Stream";
        default:            return nullptr;
    }
}

// Where a parsed set's entries live. `dataSection` is the absolute offset (from the
// blob start) of the value section, `nameSection` that of the name section; the
// per-entry name positions are relative to the latter and the per-entry data offsets
// to the former, which is why both are carried rather than one.
struct ResourceSetLayout
{
    int32_t count;
    int32_t nameSection;
    int32_t dataSection;
    int32_t namePosTable;   // offset of the int32[count] name-position table
};

// Parses the header of a RuntimeResourceSet blob. Returns false when the blob is not
// one, or is a version this reader does not decode.
//
// The version test is strict: a v1 set's per-entry code indexes the type table rather
// than being a ResourceTypeCode, so decoding one under v2 rules would hand back some
// other type's bytes as a string. Every .NET SDK since Framework 2.0 emits v2.
bool ParseSetHeader(BlobCursor& c, ResourceSetLayout& out, int32_t& versionOut)
{
    if (c.U32() != 0xBEEFCACEu || c.bad)
        return false;                    // not a ResourceManager blob at all
    (void)c.I32();                       // ResourceManager header version
    int32_t headerSize = c.I32();
    if (c.bad || headerSize < 0)
        return false;
    c.Seek(c.off + headerSize);          // skip the reader/set type-name header

    versionOut = c.I32();                // RuntimeResourceSet version
    int32_t numResources = c.I32();
    int32_t numTypes = c.I32();
    if (c.bad || numResources < 0 || numTypes < 0)
        return false;
    for (int32_t i = 0; i < numTypes; i++)
    {
        int32_t n = c.Packed7();
        if (c.bad || n < 0)
            return false;
        c.Seek(c.off + n);
    }
    if (c.bad)
        return false;

    // The name-hash and name-position tables are 8-aligned relative to the blob start.
    c.Seek(c.off + ((8 - (c.off & 7)) & 7));
    c.Seek(c.off + 4 * numResources);    // name hashes (int32 each) — unused, scanned linearly
    if (c.bad)
        return false;

    out.count = numResources;
    out.namePosTable = c.off;
    c.Seek(c.off + 4 * numResources);    // the name-position table itself
    out.dataSection = c.I32();           // absolute, from blob start
    out.nameSection = c.off;             // name positions are relative to here
    return !c.bad;
}

// The entry named `key`, or -1. Names are stored UTF-16LE with a 7-bit-encoded BYTE
// length; the compare is against the managed string's own UTF-16 chars, so no encoding
// conversion happens on either side (a ResourceManager key is an ordinal match in .NET
// too — ResourceReader hashes it and compares ordinally).
int32_t FindEntryData(BlobCursor& c, const ResourceSetLayout& lay, Dn2CppString* key)
{
    int32_t keyLen = (key != nullptr) ? key->length : 0;
    for (int32_t i = 0; i < lay.count; i++)
    {
        c.Seek(lay.namePosTable + 4 * i);
        int32_t namePos = c.I32();
        if (c.bad || namePos < 0)
            return -1;
        c.Seek(lay.nameSection + namePos);
        int32_t nameBytes = c.Packed7();
        if (c.bad || nameBytes < 0 || (nameBytes & 1) != 0 || !c.Have(nameBytes))
            return -1;
        int32_t nameChars = nameBytes / 2;
        bool match = nameChars == keyLen;
        for (int32_t j = 0; match && j < nameChars; j++)
        {
            char16_t ch = static_cast<char16_t>(
                static_cast<uint16_t>(c.p[c.off + 2 * j])
                | (static_cast<uint16_t>(c.p[c.off + 2 * j + 1]) << 8));
            match = ch == key->chars[j];
        }
        c.Seek(c.off + nameBytes);
        int32_t dataOffset = c.I32();
        if (c.bad)
            return -1;
        if (match)
            return lay.dataSection + dataOffset;
    }
    return -1;
}

// The blob of `rm`'s resource SET, or a loud refusal.
//
// Ordering matters: the dropped-resources check runs on the MISS path, after the
// lookup, as Assembly.GetManifestResourceStream does it. Only a genuine miss has to
// tell "no such set" from "the set was shed", which is what that bit exists to say.
const Dn2CppManifestResource* RequireSet(Dn2CppResourceManager* rm, const char* api)
{
    std::string setName;
    {
        Dn2CppString* bn = rm->baseName;
        int32_t n = dn2cpp_string_to_utf8(bn, nullptr, 0);
        setName.assign(static_cast<size_t>(n > 0 ? n : 0), '\0');
        if (n > 0)
            dn2cpp_string_to_utf8(bn, setName.data(), n);
    }
    setName += ".resources";

    const Dn2CppManifestResource* r =
        dn2cpp_assembly_manifest_resource(rm->assemblyName, setName.c_str());
    if (r != nullptr)
        return r;

    dn2cpp_assembly_require_manifest_resources(rm->assemblyName, api);

    // A real miss raises MissingManifestResourceException — the family real .NET raises
    // and the one its documentation tells callers to catch. Only the type is a
    // compatibility claim; the message is dn2cpp's own. It must never degrade to the
    // null a missing KEY gives: those are different bugs.
    char buf[512];
    std::snprintf(buf, sizeof(buf),
        "%s: no resource set '%s' is embedded in assembly '%s'.",
        api, setName.c_str(), rm->assemblyName);
    dn2cpp_throw_missing_manifest_resource(buf);
}

// A culture NAME as UTF-8, for the comparisons and messages below.
std::string CultureNameUtf8(const Dn2CppNumberFormatInfo* culture)
{
    std::string cn;
    if (culture == nullptr)
        return cn;
    Dn2CppString* name = dn2cpp_culture_name(culture);
    if (name == nullptr)
        return cn;
    int32_t n = dn2cpp_string_to_utf8(name, nullptr, 0);
    cn.assign(static_cast<size_t>(n > 0 ? n : 0), '\0');
    if (n > 0)
        dn2cpp_string_to_utf8(name, cn.data(), n);
    return cn;
}

// Culture names are ASCII BCP-47 tags and .NET normalizes their casing, so an ordinal
// ASCII-case-folded compare is the same verdict CultureInfo.Name equality gives — and
// it does not depend on dn2cpp's own culture table having normalized the caller's
// spelling.
bool CultureNameEquals(const std::string& a, const char* b)
{
    if (b == nullptr)
        return false;
    size_t i = 0;
    for (; i < a.size() && b[i] != '\0'; i++)
    {
        char x = a[i], y = b[i];
        if (x >= 'A' && x <= 'Z') x = static_cast<char>(x - 'A' + 'a');
        if (y >= 'A' && y <= 'Z') y = static_cast<char>(y - 'A' + 'a');
        if (x != y)
            return false;
    }
    return i == a.size() && b[i] == '\0';
}

// The culture gate: does `culture` name an ask this image's own blob is the RIGHT
// answer to? Exactly two are.
//
//  * The INVARIANT ask — a null culture (the no-culture overloads) or any culture whose
//    Name is empty. With no satellite in the picture, the main assembly's set is what
//    real .NET reads for it.
//  * The assembly's DECLARED NEUTRAL culture — `[NeutralResourcesLanguage("en")]` asked
//    for "en". Real .NET's UltimateFallbackFixup rewrites that exact ask to the
//    invariant one and probes no satellite. The match must be EXACT: "en-US" against a
//    declared "en" reads the en-US satellite, and that parent walk is what dn2cpp
//    cannot reproduce.
//
// Everything else refuses, because dn2cpp links NO satellite assemblies: from inside
// the image "the fr satellite was never built" and "it exists and I cannot reach it"
// are the same state, and real .NET answers differently in each. Serving the neutral
// string for both would be a silent wrong answer half the time, so the refusal names
// the culture, the limit and the remedy.
//
// UltimateResourceFallbackLocation.Satellite inverts all of it — it declares the
// neutral resources live in a satellite, and real .NET then reads the satellite for
// EVERY ask, the culture-less one included. Under that declaration even the invariant
// arm would be wrong, so nothing here can be served and every ask refuses.
void RequireServableCulture(Dn2CppResourceManager* rm, const Dn2CppNumberFormatInfo* culture,
    const char* api)
{
    int32_t satellite = 0;
    const char* neutral =
        dn2cpp_assembly_neutral_resources_culture(rm->assemblyName, &satellite);
    std::string cn = CultureNameUtf8(culture);

    if (satellite != 0)
    {
        char buf[640];
        std::snprintf(buf, sizeof(buf),
            "%s: assembly '%s' declares [NeutralResourcesLanguage(\"%s\", "
            "UltimateResourceFallbackLocation.Satellite)], so even a culture-less read "
            "resolves to the '%s' satellite assembly rather than to the set embedded "
            "here — and AOT-compiled code links no satellite. Answering from the main "
            "assembly's own blob would be a silent wrong answer. Declare the fallback "
            "as MainAssembly, or localize through your engine's own translation system.",
            api, rm->assemblyName, neutral != nullptr ? neutral : "",
            neutral != nullptr ? neutral : "");
        dn2cpp_throw_platform_not_supported(buf);
    }

    if (cn.empty())
        return;                                   // the invariant ask
    if (CultureNameEquals(cn, neutral))
        return;                                   // the declared neutral culture

    char declared[128];
    declared[0] = '\0';
    if (neutral != nullptr)
        std::snprintf(declared, sizeof(declared),
            " Assembly '%s' declares '%s' as its neutral culture, and that exact name is"
            " served from here too.", rm->assemblyName, neutral);
    char buf[896];
    std::snprintf(buf, sizeof(buf),
        "%s: a culture-specific lookup ('%s') needs a satellite assembly, and "
        "AOT-compiled code links none — a satellite is a separate file the transpile "
        "never loaded, so 'not built' and 'unreachable' cannot be told apart here and "
        "answering the neutral string would be a silent wrong answer. Read the neutral "
        "resources (pass CultureInfo.InvariantCulture or use the no-culture overload), "
        "or localize through your engine's own translation system.%s",
        api, cn.c_str(), declared);
    dn2cpp_throw_platform_not_supported(buf);
}

} // namespace

// ===== The intrinsic surface =================================================

// The handle's public field surface, real .NET's exactly (the hand-writing argument is
// at dn2cpp_primflds_bool in dn2cpp_typeinfo.cpp). These restate the wire format's
// header constants deliberately: a reflected field row must say what .NET says, not
// what this reader happens to accept.
static Dn2CppObject* dn2cpp_ownfld_resmgr_MagicNumber(Dn2CppObject*)
{ int32_t v = (int32_t)0xBEEFCACEu; return dn2cpp_box(&dn2cpp_int32_type, &v, sizeof(v)); }
static Dn2CppObject* dn2cpp_ownfld_resmgr_HeaderVersionNumber(Dn2CppObject*)
{ int32_t v = 1; return dn2cpp_box(&dn2cpp_int32_type, &v, sizeof(v)); }
static const Dn2CppFieldInfo dn2cpp_ownflds_resmgr[] = {
    { "MagicNumber", &dn2cpp_resourcemanager_type, &dn2cpp_int32_type,
      DN2CPP_FLDA_STATIC | DN2CPP_FLDA_PUBLIC | DN2CPP_FLDA_INITONLY,
      dn2cpp_ownfld_resmgr_MagicNumber, nullptr, nullptr, 0, 0x36, 0 },
    { "HeaderVersionNumber", &dn2cpp_resourcemanager_type, &dn2cpp_int32_type,
      DN2CPP_FLDA_STATIC | DN2CPP_FLDA_PUBLIC | DN2CPP_FLDA_INITONLY,
      dn2cpp_ownfld_resmgr_HeaderVersionNumber, nullptr, nullptr, 0, 0x36, 0 },
};

extern const Dn2CppType dn2cpp_resourcemanager_type_obj;
const Dn2CppTypeInfo dn2cpp_resourcemanager_type =
    dn2cpp_ti_with_typeobject({ "System.Resources.ResourceManager", nullptr, (int32_t)sizeof(Dn2CppResourceManager), nullptr, nullptr, 0,
                                nullptr, nullptr, nullptr, 0,
                                dn2cpp_ownflds_resmgr, 2 },
                              &dn2cpp_resourcemanager_type_obj);
const Dn2CppType dn2cpp_resourcemanager_type_obj = { { &dn2cpp_type_type },
                                                     &dn2cpp_resourcemanager_type };

Dn2CppResourceManager* dn2cpp_resourcemanager_new(Dn2CppString* baseName, const char* assembly)
{
    if (baseName == nullptr)
        dn2cpp_throw_argument_null();
    auto* rm = static_cast<Dn2CppResourceManager*>(dn2cpp_alloc(sizeof(Dn2CppResourceManager)));
    rm->type = &dn2cpp_resourcemanager_type;
    dn2cpp_gc_store_ref(&rm->baseName, baseName);
    // A null assembly handle would mean "no registry row", which every lookup below
    // would then report as a missing SET — true, but naming no assembly. The empty
    // name is what dn2cpp_assembly_reg_find already treats as unknown, and it prints.
    rm->assemblyName = (assembly != nullptr) ? assembly : "";
    return rm;
}

Dn2CppResourceManager* dn2cpp_resourcemanager_new_for_type(Dn2CppType* resourceSource)
{
    if (resourceSource == nullptr)
        dn2cpp_throw_argument_null();
    return dn2cpp_resourcemanager_new(dn2cpp_type_fullname(dn2cpp_type_require(resourceSource)),
                                      dn2cpp_type_assembly_name(resourceSource));
}

Dn2CppString* dn2cpp_resourcemanager_base_name(Dn2CppResourceManager* rm)
{
    if (rm == nullptr)
        dn2cpp_throw_argument_null();
    return rm->baseName;
}

// The one lookup both public entries go through. `api` is the caller's own member name,
// threaded so every refusal names the overload that was actually called.
//
// It POSITIONS the cursor on the entry's value and hands back its type code rather than
// decoding: GetString and GetObject want different things out of the same bytes.
//
// Returns false for a missing KEY (null on both sides, as .NET); everything else throws.
static bool LocateEntry(Dn2CppResourceManager* rm, Dn2CppString* name,
    const Dn2CppNumberFormatInfo* culture, const char* api,
    BlobCursor& c, int32_t& typeCodeOut)
{
    if (rm == nullptr || name == nullptr)
        dn2cpp_throw_argument_null();
    RequireServableCulture(rm, culture, api);
    const Dn2CppManifestResource* set = RequireSet(rm, api);

    c = BlobCursor(set->data, set->length);
    ResourceSetLayout lay{};
    int32_t version = 0;
    if (!ParseSetHeader(c, lay, version))
    {
        char buf[512];
        std::snprintf(buf, sizeof(buf),
            "%s: the embedded resource '%s' in assembly '%s' is not a readable "
            "RuntimeResourceSet.", api, set->name, rm->assemblyName);
        dn2cpp_throw_not_supported_msg(buf);
    }
    if (version != 2)
    {
        // See ParseSetHeader: a v1 entry's code indexes the type table, so decoding one
        // with v2 rules would hand back some other type's bytes as a string.
        char buf[512];
        std::snprintf(buf, sizeof(buf),
            "%s: the embedded resource '%s' in assembly '%s' is a RuntimeResourceSet "
            "version %d; dn2cpp reads version 2 (what every .NET SDK since Framework 2.0 "
            "emits).", api, set->name, rm->assemblyName, version);
        dn2cpp_throw_not_supported_msg(buf);
    }

    int32_t dataAt = FindEntryData(c, lay, name);
    if (dataAt < 0)
        return false;                // no such KEY — null, exactly as .NET
    c.Seek(dataAt);
    typeCodeOut = c.Packed7();
    return !c.bad;
}

// The String payload at the cursor: a 7-bit-encoded BYTE length then UTF-8.
static Dn2CppString* ReadStringPayload(BlobCursor& c)
{
    int32_t byteLen = c.Packed7();
    if (c.bad || byteLen < 0 || !c.Have(byteLen))
        return nullptr;
    return dn2cpp_string_from_utf8(reinterpret_cast<const char*>(c.p + c.off), byteLen);
}

// Refuses a code this reader will not decode. Three different kinds of "no" reach here
// — a Stream (real .NET hands back an UnmanagedMemoryStream over the mapped image; the
// message names the route that does work), a BinaryFormatter graph at or past
// StartOfUserTypes, and any other code below it, which means a wire format newer than
// this reader — so each says its own thing rather than guessing at the payload.
[[noreturn]] static void RefuseUndecodable(int32_t code, const char* api)
{
    char buf[512];
    if (code == kRtcStream)
        std::snprintf(buf, sizeof(buf),
            "%s: resource entry is a Stream, which real .NET hands back as an "
            "UnmanagedMemoryStream over the mapped image — a body dn2cpp cannot "
            "transpile. Read the resource through "
            "Assembly.GetManifestResourceStream(<BaseName>.resources), which hands back "
            "a read-only MemoryStream over the same bytes.", api);
    else if (code >= kRtcStartOfUserTypes)
        std::snprintf(buf, sizeof(buf),
            "%s: resource entry carries ResourceTypeCode %d, i.e. a type serialized "
            "into the set with BinaryFormatter. dn2cpp transpiles no deserializer and "
            "decoding it under any other rules would hand back some other type's bytes. "
            "Store the value as one of the primitive resource types (string, the "
            "numeric types, byte[]) instead.", api, code);
    else
        std::snprintf(buf, sizeof(buf),
            "%s: resource entry carries ResourceTypeCode %d, which this reader has no "
            "decoding for. It reads every primitive code plus ByteArray; a code below "
            "StartOfUserTypes that is none of those is a wire format newer than this "
            "reader, and guessing at its payload would answer rather than fail.",
            api, code);
    dn2cpp_throw_not_supported_msg(buf);
}

// The value at the cursor, boxed, for GetObject. Every code the wire format spells out
// is decoded here; RefuseUndecodable owns the rest. `byteArrayType` is the precise
// Byte[] type-info, which only the emitter can name.
//
// EVERY sub-word primitive is boxed through an `int32_t` local — the tree's box payload
// convention: CppTypes.Of maps Boolean, Char, SByte, Byte, Int16, UInt16 and Int32 alike
// to int32_t, and dn2cpp_object_tostring reads a boxed sub-word primitive back as
// *(const int32_t*)payload. Boxing at the value's natural width instead under-allocates
// the payload and reads garbage in the high half — invisibly, since an unbox narrows
// back to the declared width and only a read through the box (ToString) exposes it.
static Dn2CppObject* ReadBoxedPayload(BlobCursor& c, int32_t code,
    const Dn2CppTypeInfo* byteArrayType, const char* api)
{
    switch (code)
    {
        case kRtcNull:
            return nullptr;                       // .NET answers null for a Null entry
        case kRtcString:
            return reinterpret_cast<Dn2CppObject*>(ReadStringPayload(c));
        case kRtcBoolean:
        {
            int32_t v = c.U8() != 0 ? 1 : 0;
            return dn2cpp_box(&dn2cpp_bool_type, &v, sizeof(v));
        }
        case kRtcByte:
        {
            int32_t v = static_cast<int32_t>(c.U8());
            return dn2cpp_box(&dn2cpp_byte_type, &v, sizeof(v));
        }
        case kRtcSByte:
        {
            int32_t v = static_cast<int8_t>(c.U8());   // sign-extend into the int32 slot
            return dn2cpp_box(&dn2cpp_sbyte_type, &v, sizeof(v));
        }
        case kRtcChar:
        {
            // ResourceWriter stores a char as a UInt16, NOT as BinaryWriter.Write(char)
            // would (which encodes it in the writer's encoding and is variable-length).
            int32_t v = static_cast<int32_t>(static_cast<char16_t>(c.U16()));
            return dn2cpp_box(&dn2cpp_char_type, &v, sizeof(v));
        }
        case kRtcInt16:
        {
            int32_t v = static_cast<int16_t>(c.U16());  // sign-extend into the int32 slot
            return dn2cpp_box(&dn2cpp_int16_type, &v, sizeof(v));
        }
        case kRtcUInt16:
        {
            int32_t v = static_cast<int32_t>(c.U16());
            return dn2cpp_box(&dn2cpp_uint16_type, &v, sizeof(v));
        }
        case kRtcInt32:
        {
            int32_t v = c.I32();
            return dn2cpp_box(&dn2cpp_int32_type, &v, sizeof(v));
        }
        case kRtcUInt32:
        {
            uint32_t v = c.U32();
            return dn2cpp_box(&dn2cpp_uint32_type, &v, sizeof(v));
        }
        case kRtcInt64:
        {
            int64_t v = c.I64();
            return dn2cpp_box(&dn2cpp_int64_type, &v, sizeof(v));
        }
        case kRtcUInt64:
        {
            uint64_t v = c.U64();
            return dn2cpp_box(&dn2cpp_uint64_type, &v, sizeof(v));
        }
        case kRtcSingle:
        {
            float v = c.F4();
            return dn2cpp_box(&dn2cpp_single_type, &v, sizeof(v));
        }
        case kRtcDouble:
        {
            double v = c.F8();
            return dn2cpp_box(&dn2cpp_double_type, &v, sizeof(v));
        }
        case kRtcDecimal:
        {
            // BinaryWriter.Write(decimal) is the four int32 of Decimal.GetBits in order:
            // lo, mid, hi, flags — flags carrying the scale in bits 16..23 and the sign
            // in bit 31.
            int32_t lo = c.I32(), mid = c.I32(), hi = c.I32();
            uint32_t flags = c.U32();
            if (c.bad)
                return nullptr;
            Dn2CppDecimal v = dn2cpp_decimal_from_parts(lo, mid, hi,
                (flags & 0x80000000u) != 0 ? 1 : 0,
                static_cast<int32_t>((flags >> 16) & 0xFF));
            return dn2cpp_box(&dn2cpp_decimal_type, &v, sizeof(v));
        }
        case kRtcDateTime:
        {
            // ResourceWriter stores DateTime.ToBinary(): top two bits the Kind, the rest
            // ticks. ToBinary serializes a LOCAL value as UTC, so reconstructing one is a
            // timezone conversion, not a relabel (DateTime.FromBinary's own asymmetry).
            int64_t raw = c.I64();
            if (c.bad)
                return nullptr;
            int64_t ticks = raw & 0x3FFFFFFFFFFFFFFFLL;
            int32_t kind = static_cast<int32_t>((static_cast<uint64_t>(raw) >> 62) & 3u);
            Dn2CppDateTime v = (kind == 2)
                ? dn2cpp_datetime_to_local(dn2cpp_datetime_from_ticks(ticks, 1))
                : dn2cpp_datetime_from_ticks(ticks, kind);
            return dn2cpp_box(&dn2cpp_datetime_type, &v, sizeof(v));
        }
        case kRtcTimeSpan:
        {
            int64_t ticks = c.I64();
            if (c.bad)
                return nullptr;
            Dn2CppTimeSpan v = dn2cpp_timespan_from_ticks(ticks);
            return dn2cpp_box(&dn2cpp_timespan_type, &v, sizeof(v));
        }
        case kRtcByteArray:
        {
            int32_t len = c.I32();
            if (c.bad || len < 0 || !c.Have(len))
                return nullptr;
            Dn2CppArrayN* arr = dn2cpp_newarr_n_atomic_t(len, 1, byteArrayType);
            if (len > 0)
                std::memcpy(arr->data, c.p + c.off, static_cast<size_t>(len));
            return reinterpret_cast<Dn2CppObject*>(arr);
        }
        default:
            RefuseUndecodable(code, api);
    }
}

Dn2CppString* dn2cpp_resourcemanager_get_string(Dn2CppResourceManager* rm, Dn2CppString* name,
    const Dn2CppNumberFormatInfo* culture)
{
    const char* api = "ResourceManager.GetString";
    BlobCursor c(nullptr, 0);
    int32_t code = 0;
    if (!LocateEntry(rm, name, culture, api, c, code))
        return nullptr;
    if (code == kRtcNull)
        return nullptr;              // a Null entry is null on both sides
    if (code == kRtcString)
        return ReadStringPayload(c);
    // .NET's GetString raises InvalidOperationException over a non-String entry and
    // tells the caller to use GetObject; matching that family is what makes the
    // documented catch clause fire. The type is named even for a code GetObject would
    // itself refuse — the caller has to change the overload either way.
    const char* tn = RtcTypeName(code);
    char buf[512];
    if (tn != nullptr)
        std::snprintf(buf, sizeof(buf),
            "%s: resource entry is of type '%s' instead of String — call GetObject "
            "instead.", api, tn);
    else
        std::snprintf(buf, sizeof(buf),
            "%s: resource entry is not a String (ResourceTypeCode %d) — call GetObject "
            "instead.", api, code);
    dn2cpp_throw_invalid_operation_msg(buf);
}

Dn2CppObject* dn2cpp_resourcemanager_get_object(Dn2CppResourceManager* rm, Dn2CppString* name,
    const Dn2CppNumberFormatInfo* culture, const Dn2CppTypeInfo* byteArrayType)
{
    const char* api = "ResourceManager.GetObject";
    BlobCursor c(nullptr, 0);
    int32_t code = 0;
    if (!LocateEntry(rm, name, culture, api, c, code))
        return nullptr;
    return ReadBoxedPayload(c, code, byteArrayType, api);
}

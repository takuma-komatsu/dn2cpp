// dn2cpp_eventsource.cpp — System.Diagnostics.Tracing.EventSource IDENTITY.
//
// EventSource is an opaque intrinsic base (CoreIntrinsics.s_intrinsicTypes): a user or
// Microsoft.* provider transpiles as ordinary C# over it, and the base's tracing
// plumbing is lowered inline by MethodCompiler.TryEmitEventSourceIntrinsic. A native
// build ships no EventPipe, no ETW and no EventListener, so *delivery* is a no-op — and
// that is faithful rather than degraded, because every write path in .NET's own
// EventSource begins with `if (!IsEnabled()) return;` and no listener can ever attach
// here.
//
// What this file serves is the part of EventSource unrelated to delivery: Name, Guid
// and Settings, which .NET computes from the type and the base ctor's arguments before
// any listener exists, and CurrentThreadActivityId, a thread-local Guid slot .NET reads
// back whether or not anything is listening. Those are answerable exactly, so they are
// answered exactly rather than with a plausible substitute (Guid.Empty is a value .NET
// never returns for a provider).
//
// THE BOUNDARY THIS FILE DOES NOT CROSS is observation. `EventListener` is unmodeled and
// stays a loud transpile abort (its real IL bottoms out in EventPipeInternal's
// InternalCalls): a listener's whole contract is to receive events, so one that silently
// receives none is the load-bearing-consumer failure docs/ARCHITECTURE.md §4-B forbids.
// Manifest generation (GenerateManifest, GetTrait, the EventCommand surface) is unmapped
// for the same reason — it is not identity, and the loud "no intrinsic mapping yet"
// abort is what surfaces it for triage.
#include "dn2cpp_core.h"

#include <atomic>
#include <cstring>
#include <mutex>

// ── SHA-1, non-secret ────────────────────────────────────────────────────────────
// .NET derives a provider's Guid from its NAME (EventSource.GenerateGuidFromName), and
// the derivation is part of the observable contract: the same provider name must produce
// the same Guid here as in real .NET, or a transpiled program and its .NET oracle
// disagree on the identity of the same provider. The algorithm is a plain SHA-1 over a
// fixed 16-byte namespace followed by the UPPERCASED name in big-endian UTF-16, with the
// RFC 4122 version nibble forced to 5 — .NET's own Sha1ForNonSecretPurposes, which is
// standard SHA-1 with no keying. Hand-written here rather than routed through the
// runtime's crypto intrinsics because those are platform-backed (CommonCrypto/OpenSSL)
// and a *build-configuration-dependent* provider Guid would be worse than none.
namespace {

struct Sha1
{
    uint32_t h[5] = { 0x67452301u, 0xEFCDAB89u, 0x98BADCFEu, 0x10325476u, 0xC3D2E1F0u };
    uint8_t block[64] = {};
    size_t blockLen = 0;
    uint64_t total = 0;

    static uint32_t Rol(uint32_t v, int n) { return (v << n) | (v >> (32 - n)); }

    void Process()
    {
        uint32_t w[80];
        for (int i = 0; i < 16; i++)
            w[i] = (uint32_t)block[i * 4] << 24 | (uint32_t)block[i * 4 + 1] << 16
                 | (uint32_t)block[i * 4 + 2] << 8 | (uint32_t)block[i * 4 + 3];
        for (int i = 16; i < 80; i++)
            w[i] = Rol(w[i - 3] ^ w[i - 8] ^ w[i - 14] ^ w[i - 16], 1);
        uint32_t a = h[0], b = h[1], c = h[2], d = h[3], e = h[4];
        for (int i = 0; i < 80; i++)
        {
            uint32_t f, k;
            if (i < 20)      { f = (b & c) | (~b & d);          k = 0x5A827999u; }
            else if (i < 40) { f = b ^ c ^ d;                   k = 0x6ED9EBA1u; }
            else if (i < 60) { f = (b & c) | (b & d) | (c & d); k = 0x8F1BBCDCu; }
            else             { f = b ^ c ^ d;                   k = 0xCA62C1D6u; }
            uint32_t t = Rol(a, 5) + f + e + k + w[i];
            e = d; d = c; c = Rol(b, 30); b = a; a = t;
        }
        h[0] += a; h[1] += b; h[2] += c; h[3] += d; h[4] += e;
    }

    // Feeds bytes through the compression function without counting them toward the
    // message length — the padding and the length field itself.
    void AppendRaw(const uint8_t* data, size_t len)
    {
        while (len > 0)
        {
            size_t take = 64 - blockLen;
            if (take > len)
                take = len;
            std::memcpy(block + blockLen, data, take);
            blockLen += take;
            data += take;
            len -= take;
            if (blockLen == 64)
            {
                Process();
                blockLen = 0;
            }
        }
    }

    void Append(const uint8_t* data, size_t len)
    {
        total += (uint64_t)len;
        AppendRaw(data, len);
    }

    // Writes the first 16 bytes of the digest (all .NET's GenerateGuidFromName keeps).
    void Finish16(uint8_t* out)
    {
        uint64_t bits = total * 8;
        uint8_t pad = 0x80;
        AppendRaw(&pad, 1);
        uint8_t zero = 0;
        while (blockLen != 56)
            AppendRaw(&zero, 1);
        uint8_t lenBe[8];
        for (int i = 0; i < 8; i++)
            lenBe[i] = (uint8_t)(bits >> (56 - i * 8));
        AppendRaw(lenBe, 8);
        for (int i = 0; i < 4; i++)
        {
            out[i * 4]     = (uint8_t)(h[i] >> 24);
            out[i * 4 + 1] = (uint8_t)(h[i] >> 16);
            out[i * 4 + 2] = (uint8_t)(h[i] >> 8);
            out[i * 4 + 3] = (uint8_t)(h[i]);
        }
    }
};

// The Guid layout a `new Guid(byte[16])` produces, written endian-correctly: .NET reads
// the first three groups as LITTLE-endian integers out of the array and copies the last
// eight bytes verbatim, so a straight memcpy of the array would be right only on a
// little-endian host. Assembling the fields is the same three lines and is right on both.
void WriteGuidFromBytes(const uint8_t* b, void* out16)
{
    uint8_t* o = static_cast<uint8_t*>(out16);
    uint32_t a = (uint32_t)b[3] << 24 | (uint32_t)b[2] << 16 | (uint32_t)b[1] << 8 | (uint32_t)b[0];
    uint16_t c = (uint16_t)((uint32_t)b[5] << 8 | (uint32_t)b[4]);
    uint16_t d = (uint16_t)((uint32_t)b[7] << 8 | (uint32_t)b[6]);
    std::memcpy(o, &a, 4);
    std::memcpy(o + 4, &c, 2);
    std::memcpy(o + 6, &d, 2);
    std::memcpy(o + 8, b + 8, 8);
}

void ZeroGuid(void* out16) { std::memset(out16, 0, 16); }

// EventSource.GenerateGuidFromName(name.ToUpperInvariant()): SHA-1 over a fixed
// namespace plus the uppercased name in big-endian UTF-16, version nibble forced to 5.
//
// INVARIANT: the uppercasing here is ASCII-only, and it is enough. An ETW provider name
// is constrained to the printable ASCII range by the platform that consumes it, both
// .NET's own providers and every one this transpiler has met are ASCII, and the two
// spellings only diverge on a name whose non-ASCII character has a different invariant
// upper case. A non-ASCII provider name would produce a Guid that differs from real
// .NET's; it would not produce a *wrong-looking* one, so it is stated here rather than
// guarded — pulling the invariant-casing tables in for a name nobody writes would put a
// Unicode table into every program that constructs a provider.
void GuidFromName(const char* name, void* out16)
{
    static const uint8_t kNamespace[16] = {
        0x48, 0x2C, 0x2D, 0xB2, 0xC3, 0x90, 0x47, 0xC8,
        0x87, 0xF8, 0x1A, 0x15, 0xBF, 0xC1, 0x30, 0xFB,
    };
    Sha1 sha;
    sha.Append(kNamespace, 16);
    // The name is UTF-8 in the type-info stamp; for the ASCII range this file's invariant
    // covers, each byte is one UTF-16 code unit. A non-ASCII byte is passed through as
    // that byte's code unit, which is what makes the divergence above bounded rather than
    // undefined.
    for (const char* p = name; *p != '\0'; ++p)
    {
        uint8_t ch = (uint8_t)*p;
        if (ch >= 'a' && ch <= 'z')
            ch = (uint8_t)(ch - 'a' + 'A');
        uint8_t be[2] = { 0, ch };
        sha.Append(be, 2);
    }
    uint8_t digest[16];
    sha.Finish16(digest);
    digest[7] = (uint8_t)((digest[7] & 0x0F) | 0x50); // RFC 4122 version 5
    WriteGuidFromBytes(digest, out16);
}

int HexVal(char c)
{
    if (c >= '0' && c <= '9') return c - '0';
    if (c >= 'a' && c <= 'f') return c - 'a' + 10;
    if (c >= 'A' && c <= 'F') return c - 'A' + 10;
    return -1;
}

// "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx" -> the 16 bytes `new Guid(string)` produces.
// The emitter is what writes this string (from [EventSource(Guid=…)]), and it writes
// only the canonical form, so a malformed one is a transpiler bug rather than input:
// answer the zero Guid rather than throwing out of a property getter.
bool ParseGuid(const char* s, void* out16)
{
    if (s == nullptr || std::strlen(s) != 36)
        return false;
    uint8_t raw[16];
    int bi = 0;
    for (int i = 0; i < 36; )
    {
        if (i == 8 || i == 13 || i == 18 || i == 23)
        {
            if (s[i] != '-')
                return false;
            i++;
            continue;
        }
        int hi = HexVal(s[i]), lo = HexVal(s[i + 1]);
        if (hi < 0 || lo < 0)
            return false;
        raw[bi++] = (uint8_t)(hi << 4 | lo);
        i += 2;
    }
    // The canonical text spells the first three groups big-endian; `new Guid(byte[])`
    // reads them little-endian, so hand WriteGuidFromBytes a byte-swapped copy of those
    // three groups and the two agree.
    uint8_t b[16] = {
        raw[3], raw[2], raw[1], raw[0], raw[5], raw[4], raw[7], raw[6],
        raw[8], raw[9], raw[10], raw[11], raw[12], raw[13], raw[14], raw[15],
    };
    WriteGuidFromBytes(b, out16);
    return true;
}

// ── EventSourceSettings ──────────────────────────────────────────────────────────
constexpr int32_t kThrowOnEventWriteErrors = 1;
constexpr int32_t kEtwManifestEventFormat = 4;
constexpr int32_t kEtwSelfDescribingEventFormat = 8;
constexpr int32_t kEventFormatMask = kEtwManifestEventFormat | kEtwSelfDescribingEventFormat;

// EventSource.ValidateSettings: exactly one event format must be selected, and none
// selected means the manifest format. This is why real .NET never reports
// EventSourceSettings.Default (0) from Settings — the value dn2cpp used to answer.
int32_t ValidateSettings(int32_t s)
{
    if ((s & kEventFormatMask) == kEventFormatMask)
        dn2cpp_throw_argument_msg("EventSourceSettings: only one event format may be selected");
    if ((s & kEventFormatMask) == 0)
        s |= kEtwManifestEventFormat;
    return s;
}

// ── Per-instance identity supplied by a base ctor ────────────────────────────────
// `base("MyProvider")` / `base(EventSourceSettings.EtwSelfDescribingEventFormat)` carry
// identity that is NOT a property of the type, so no type-info stamp can hold it. The
// records live here, keyed by the instance.
//
// INVARIANT: a record holds a STRONG reference to its EventSource, so a provider that
// supplies ctor identity is never collected. That is the deliberate trade: the table is
// only ever appended to by such a ctor, providers are singletons in every shape this
// models (the framework's own are static readonly fields, and dn2cpp folds those away
// entirely), and the alternative — keying on a raw address the collector may hand to a
// later object — would answer a *different* provider's name. A program that constructs
// ctor-named providers in a loop leaks them; one that constructs none allocates nothing
// here, which is the overwhelmingly common case since [EventSource(Name=…)] is the
// documented way to name a provider.
struct CtorIdentity
{
    Dn2CppObject* src;
    Dn2CppString* name;   // null when the ctor supplied no name
    int32_t settings;
};

std::mutex& IdentityMutex() { return dn2cpp_never_destroyed<std::mutex>(); }
// Pinned (GC-visible, never collected) storage: the `src`/`name` pointers in it must be
// scanned, and a plain new[] block is not a root on every platform.
CtorIdentity* g_identities = nullptr;   // guarded by IdentityMutex()
// Atomic only so the "no ctor-supplied identity anywhere in this program" fast path below
// can skip the lock without a data race; every mutation still happens under the mutex.
std::atomic<int32_t> g_identityCount{ 0 };
int32_t g_identityCap = 0;

const CtorIdentity* FindIdentity(Dn2CppObject* src)
{
    if (g_identityCount.load(std::memory_order_acquire) == 0)
        return nullptr;
    std::lock_guard<std::mutex> lock(IdentityMutex());
    for (int32_t i = 0; i < g_identityCount; i++)
        if (g_identities[i].src == src)
            return &g_identities[i];
    return nullptr;
}

Dn2CppString* StringOf(const char* utf8)
{
    return dn2cpp_string_from_utf8(utf8, (int32_t)std::strlen(utf8));
}

} // namespace

Dn2CppString* dn2cpp_eventsource_type_name(const Dn2CppTypeInfo* ti)
{
    if (ti == nullptr || ti->eventSourceName == nullptr)
        dn2cpp_throw_platform_not_supported(
            "System.Diagnostics.Tracing.EventSource.Name: the receiver's type carries no "
            "provider name (it is not an emitted EventSource-derived class)");
    return StringOf(ti->eventSourceName);
}

void dn2cpp_eventsource_type_guid(const Dn2CppTypeInfo* ti, void* out16)
{
    if (ti == nullptr)
    {
        ZeroGuid(out16);
        return;
    }
    if (ti->eventSourceGuid != nullptr && ParseGuid(ti->eventSourceGuid, out16))
        return;
    if (ti->eventSourceName != nullptr)
    {
        GuidFromName(ti->eventSourceName, out16);
        return;
    }
    ZeroGuid(out16);
}

Dn2CppString* dn2cpp_eventsource_name(Dn2CppObject* src)
{
    if (src == nullptr)
        dn2cpp_throw_null_reference();
    if (const CtorIdentity* rec = FindIdentity(src); rec != nullptr && rec->name != nullptr)
        return rec->name;
    return dn2cpp_eventsource_type_name(src->type);
}

void dn2cpp_eventsource_guid(Dn2CppObject* src, void* out16)
{
    if (src == nullptr)
        dn2cpp_throw_null_reference();
    // A ctor-supplied name replaces the type's, and .NET derives the guid from whichever
    // name won (EventSource(string) forwards to GenerateGuidFromName(name.ToUpper…)), so
    // the explicit [EventSource(Guid=…)] on the type does not apply to such an instance.
    if (const CtorIdentity* rec = FindIdentity(src); rec != nullptr && rec->name != nullptr)
    {
        int32_t len = rec->name->length;
        // The stamp path takes UTF-8; a managed string is UTF-16. Names are ASCII (the
        // invariant at GuidFromName), so narrow and reuse the one implementation rather
        // than writing a second UTF-16 arm that would have to be kept in step with it.
        char stack[256];
        char* buf = stack;
        // Plain C++ storage on purpose: this scratch holds no managed pointer, so it is
        // not GC memory and must not consume an uncollectable pinned block.
        if (len >= (int32_t)sizeof(stack))
            buf = new char[(size_t)len + 1];
        for (int32_t i = 0; i < len; i++)
            buf[i] = (char)(rec->name->chars[i] & 0xFF);
        buf[len] = '\0';
        GuidFromName(buf, out16);
        if (buf != stack)
            delete[] buf;
        return;
    }
    dn2cpp_eventsource_type_guid(src->type, out16);
}

int32_t dn2cpp_eventsource_settings(Dn2CppObject* src)
{
    if (src == nullptr)
        dn2cpp_throw_null_reference();
    if (const CtorIdentity* rec = FindIdentity(src); rec != nullptr)
        return rec->settings;
    // The parameterless base ctor: ValidateSettings(0).
    return kEtwManifestEventFormat;
}

void dn2cpp_eventsource_ctor(Dn2CppObject* src, Dn2CppString* name, int32_t settings)
{
    int32_t validated = ValidateSettings(settings);
    // Nothing to remember: the type-info stamp already answers the name and the settings
    // are what the parameterless ctor implies. This is the path every
    // [EventSource(Name=…)] provider takes, so such a program never touches the table.
    if (name == nullptr && validated == kEtwManifestEventFormat)
        return;
    std::lock_guard<std::mutex> lock(IdentityMutex());
    for (int32_t i = 0; i < g_identityCount; i++)
    {
        if (g_identities[i].src != src)
            continue;
        // A derived ctor chain can reach the base twice only through a bug, but if it
        // does, .NET's last write wins too.
        g_identities[i].name = name;
        g_identities[i].settings = validated;
        return;
    }
    if (g_identityCount == g_identityCap)
    {
        int32_t cap = g_identityCap == 0 ? 4 : g_identityCap * 2;
        auto* grown = static_cast<CtorIdentity*>(dn2cpp_alloc_pinned(sizeof(CtorIdentity) * (size_t)cap));
        if (g_identities != nullptr)
        {
            std::memcpy(grown, g_identities, sizeof(CtorIdentity) * (size_t)g_identityCount);
            dn2cpp_free_pinned(g_identities);
        }
        g_identities = grown;
        g_identityCap = cap;
    }
    g_identities[g_identityCount].src = src;
    g_identities[g_identityCount].name = name;
    g_identities[g_identityCount].settings = validated;
    g_identityCount++;
}

// ── CurrentThreadActivityId ──────────────────────────────────────────────────────
// A plain thread-local 16 bytes. It holds no managed reference, so unlike the
// SynchronizationContext slot next door it needs no GC-visible storage — which is the
// whole reason this surface is answerable at all: .NET's own setter, with no listener
// attached, does nothing but write this slot and hand back what it held.
static thread_local uint8_t t_activityId[16] = {};

void dn2cpp_eventsource_get_activity_id(void* out16)
{
    std::memcpy(out16, t_activityId, 16);
}

void dn2cpp_eventsource_set_activity_id(const void* in16, void* prev16)
{
    if (prev16 != nullptr)
        std::memcpy(prev16, t_activityId, 16);
    std::memcpy(t_activityId, in16, 16);
}

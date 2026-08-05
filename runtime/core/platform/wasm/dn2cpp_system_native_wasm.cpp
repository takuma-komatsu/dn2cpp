// dn2cpp_system_native_wasm.cpp — the sliver of the .NET PAL (libSystem.Native)
// surface that the Emscripten target needs.
//
// The bulk of that surface (platform/posix/dn2cpp_system_native.cpp) is the
// file-I/O P/Invoke closure, and it is deliberately NOT part of the wasm build.
// But one of its symbols has a caller that is not a P/Invoke at all and IS
// compiled on wasm: intrinsics/dn2cpp_openssl_crypto_digest.cpp's
// CryptoNative_GetRandomBytes forwards straight to
// SystemNative_GetCryptographicallySecureRandomBytes. So the symbol went missing
// on this target while the POSIX TU that defines it was excluded for a reason
// that does not cover it.
//
// It stayed invisible because of the shape of the two link models, not because
// nothing reached it. In an EXECUTABLE link wasm-ld dead-strips the unreferenced
// tail, and the wasm console gate's programs never touch a Guid or an RNG — so
// the reference is stripped before it can be missed. A SIDE MODULE has no such
// luck: an unresolved symbol there is not an error but a wasm IMPORT, and the
// import is not diagnosed at dlopen either (emscripten's loader hands back a
// valid handle and dlsym resolves) — it throws `TypeError: resolved is not a
// function` out of the JS glue at the instant the function is first CALLED,
// naming a wasm function index rather than the symbol. A real game reaches this
// on Guid.NewGuid(), RandomNumberGenerator, or hash seeding.

#include <cstdint>
#include <unistd.h> // getentropy

extern "C" {

// pal_random.c SystemNative_GetCryptographicallySecureRandomBytes: the entropy
// source behind Guid.NewGuid / RandomNumberGenerator. Returns 0 on success, -1 on
// failure — the managed forwarder (Interop.GetCryptographicallySecureRandomBytes)
// throws CryptographicException on nonzero. The contract is the POSIX one,
// verbatim; only the backend differs.
//
// Real randomness is required, and Emscripten supplies it: its musl getentropy
// is backed by the host CSPRNG (crypto.getRandomValues in a browser,
// crypto.randomBytes under node), not by a seeded PRNG. Two NewGuid() results
// must differ across calls AND across runs, which a deterministic fill would
// quietly break — that is the failure the non-secure hash-seed source
// (dn2cpp_fill_nonsecure_random) is allowed and this one is not.
//
// No GC bounce buffer here, unlike the POSIX twin. That bounce exists because
// Boehm's incremental mode mprotects heap pages, so a kernel write through to
// one faults with EFAULT instead of trapping into the collector's handler. The
// Boehm GC is built on this axis, but incremental mode is not — there is no
// page-protection VDB under Emscripten, so the collector is compiled with
// GC_DISABLE_INCREMENTAL and dn2cpp_gc.cpp forces the mode off. Nothing
// mprotects anything, so there is no hazard to bounce around.
int32_t SystemNative_GetCryptographicallySecureRandomBytes(uint8_t* buffer, int32_t bufferLength)
{
    if (buffer == nullptr || bufferLength < 0)
        return -1;

    // getentropy caps each request at 256 bytes; loop for larger fills.
    size_t remaining = static_cast<size_t>(bufferLength);
    uint8_t* cursor = buffer;
    while (remaining > 0)
    {
        size_t chunk = remaining > 256 ? 256 : remaining;
        if (::getentropy(cursor, chunk) != 0)
            return -1;
        cursor += chunk;
        remaining -= chunk;
    }
    return 0;
}

} // extern "C"

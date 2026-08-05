// dn2cpp_openssl_crypto_digest.cpp — self-contained implementation of the
// OpenSSL-flavor CryptoNative digest / HMAC / RNG / error surface the
// transpiled real-BCL hashing code calls on the Linux RID of
// System.Security.Cryptography.dll (SHA256/SHA1/MD5/SHA384/SHA512 + HMAC-* in
// every shape — one-shot HashData, instance ComputeHash / TransformBlock,
// IncrementalHash, and RandomNumberGenerator).
//
// The Linux build of System.Security.Cryptography.dll bottoms out in
// `[LibraryImport("libSystem.Security.Cryptography.Native.OpenSsl")]`
// P/Invokes (CryptoNative_*), unlike the macOS build's
// AppleCryptoNative_* (dn2cpp_apple_crypto_digest.cpp). dn2cpp lowers those to
// direct native calls (Compilation.IsRuntimeProvidedPInvokeModule) and provides
// the CryptoNative_* symbols here over the SAME portable MD5/SHA-1/SHA-2 + HMAC
// cores (dn2cpp_hash_cores.h, FIPS 180-4 / RFC 1321 / RFC 2104). The real
// OpenSSL is NOT linked — the binary stays self-contained, matching the
// design's self-containment principle and the Apple TU's posture.
//
// ABI source of truth: dotnet/runtime v10.0.x
// src/native/libs/System.Security.Cryptography.Native/pal_evp.{h,c},
// pal_hmac.{h,c}, pal_evp_mac usage, pal_err.c, pal_random.c; every signature
// below was additionally confirmed by decoding the real net10.0 Linux-RID
// System.Security.Cryptography.dll's P/Invoke metadata.
//
// Semantics — how the managed Unix hashing stack drives these (Interop.Crypto /
// LiteHash / EvpHashProvider / EvpHmacProvider):
//   * The EvpShaXxx()/EvpMd5() selectors return a stable, non-null EVP_MD*
//     handle; here a pointer to a static algorithm-id int the ctx/HMAC create
//     paths decode back. EvpMdSize(handle) reports its digest length.
//   * EvpMdCtxCreate(type) allocates+inits a context; EvpDigestUpdate absorbs.
//   * EvpDigestFinalEx finalizes but — UNLIKE Apple's DigestFinal — does NOT
//     re-init: EvpHashProvider.FinalizeHashAndReset issues a SEPARATE
//     EvpDigestReset(ctx, type) afterwards. So Final is a pure finalize and
//     Reset is the re-init (this is the one behavioral difference from the
//     Apple PAL, mandated by the managed call sequence).
//   * EvpDigestCurrent / HmacCurrent are clone-then-final (non-destructive).
//   * HmacFinal does NOT re-key; HMACCommon calls HmacReset to restore the
//     keyed (primed) state — so the context remembers its initial primed inner
//     state (innerInit) to restore on reset, without needing the key again.
//   * The *pcbDigest/*s/*mdSize out-length is written with the true digest
//     length; every success path returns 1 (0 on a guard failure, matching the
//     managed `ret == 1` / `!= 0` checks). Our cores never fail, so the error
//     surface (ErrClearError/ErrGetExceptionError/ErrErrorStringN/…) is
//     benign no-op/zero stubs that only need to link.
//
// PAL_HashAlgorithm mirror (dn2cpp_hash_cores.h): Md5=1, Sha1=2, Sha256=3,
// Sha384=4, Sha512=5.

#include "dn2cpp_hash_cores.h"

namespace
{

// Stable EVP_MD* handles: the selectors below hand the managed side a pointer to
// one of these algorithm-id ints; the ctx/HMAC create + size paths decode it.
const int32_t kAlgMd5 = PAL_Md5;
const int32_t kAlgSha1 = PAL_Sha1;
const int32_t kAlgSha256 = PAL_Sha256;
const int32_t kAlgSha384 = PAL_Sha384;
const int32_t kAlgSha512 = PAL_Sha512;

inline int32_t handle_alg(void* md)
{
    return md == nullptr ? PAL_Unknown : *static_cast<const int32_t*>(md);
}

// OpenSSL HMAC context: the shared HmacCtx core plus the initial primed inner
// state, so HmacReset can restore the keyed state without re-supplying the key
// (HMAC_Init_ex(ctx, NULL, 0, NULL) semantics the managed HMACCommon relies on).
struct OsslHmacCtx
{
    HmacCtx core;
    DigestCtx innerInit;
};

} // namespace

// ---- the CryptoNative_* PAL surface ----------------------------------------

extern "C" {

// -- EVP_MD* algorithm selectors (return a stable non-null handle) --

void* CryptoNative_EvpMd5(void)    { return const_cast<int32_t*>(&kAlgMd5); }
void* CryptoNative_EvpSha1(void)   { return const_cast<int32_t*>(&kAlgSha1); }
void* CryptoNative_EvpSha256(void) { return const_cast<int32_t*>(&kAlgSha256); }
void* CryptoNative_EvpSha384(void) { return const_cast<int32_t*>(&kAlgSha384); }
void* CryptoNative_EvpSha512(void) { return const_cast<int32_t*>(&kAlgSha512); }

// SHA-3 / SHAKE: not provided by the portable cores. The real pal_evp.c
// returns NULL from these exact selectors when the linked OpenSSL predates
// SHA-3 support, and the managed side (HashProviderDispenser.HashSupported)
// already treats a null handle as "algorithm not available on this platform" —
// so a null here is a fully in-contract answer, not an error.
void* CryptoNative_EvpSha3_256(void) { return nullptr; }
void* CryptoNative_EvpSha3_384(void) { return nullptr; }
void* CryptoNative_EvpSha3_512(void) { return nullptr; }
void* CryptoNative_EvpShake128(void) { return nullptr; }
void* CryptoNative_EvpShake256(void) { return nullptr; }

int32_t CryptoNative_EvpMdSize(void* md)
{
    return digest_size(handle_alg(md));
}

int32_t CryptoNative_GetMaxMdSize(void)
{
    return 64; // EVP_MAX_MD_SIZE — the largest digest here (SHA-512)
}

// -- EVP digest context lifecycle --

void* CryptoNative_EvpMdCtxCreate(void* type)
{
    int32_t alg = handle_alg(type);
    if (digest_size(alg) < 0)
        return nullptr;
    DigestCtx* d = static_cast<DigestCtx*>(std::malloc(sizeof(DigestCtx)));
    d->alg = alg;
    digest_init(*d);
    return d;
}

void CryptoNative_EvpMdCtxDestroy(void* ctx)
{
    std::free(ctx);
}

void* CryptoNative_EvpMdCtxCopyEx(void* ctx)
{
    if (ctx == nullptr)
        return nullptr;
    DigestCtx* d = static_cast<DigestCtx*>(std::malloc(sizeof(DigestCtx)));
    *d = *static_cast<DigestCtx*>(ctx);
    return d;
}

int32_t CryptoNative_EvpDigestReset(void* ctx, void* type)
{
    DigestCtx* d = static_cast<DigestCtx*>(ctx);
    if (d == nullptr)
        return 0;
    int32_t alg = handle_alg(type);
    if (digest_size(alg) >= 0)
        d->alg = alg; // re-key to the same algorithm the managed side passes
    digest_init(*d);
    return 1;
}

int32_t CryptoNative_EvpDigestUpdate(void* ctx, uint8_t* d, int32_t cnt)
{
    if (cnt == 0)
        return 1;
    if (ctx == nullptr || d == nullptr)
        return 0;
    digest_update(*static_cast<DigestCtx*>(ctx), d, (size_t)cnt);
    return 1;
}

// Finalize WITHOUT re-init: the managed EvpHashProvider issues a separate
// EvpDigestReset afterwards (the key behavioral contrast with the Apple PAL).
int32_t CryptoNative_EvpDigestFinalEx(void* ctx, uint8_t* md, uint32_t* s)
{
    DigestCtx* d = static_cast<DigestCtx*>(ctx);
    if (d == nullptr || md == nullptr)
        return 0;
    digest_final(*d, md);
    if (s != nullptr)
        *s = (uint32_t)digest_size(d->alg);
    return 1;
}

// Non-destructive snapshot of the running digest (clone-then-final).
int32_t CryptoNative_EvpDigestCurrent(void* ctx, uint8_t* md, uint32_t* s)
{
    DigestCtx* d = static_cast<DigestCtx*>(ctx);
    if (d == nullptr || md == nullptr)
        return 0;
    DigestCtx tmp = *d;
    digest_final(tmp, md);
    if (s != nullptr)
        *s = (uint32_t)digest_size(d->alg);
    return 1;
}

int32_t CryptoNative_EvpDigestOneShot(void* type, uint8_t* source, int32_t sourceSize,
                                      uint8_t* md, uint32_t* mdSize)
{
    if (md == nullptr || mdSize == nullptr)
        return 0;
    int32_t alg = handle_alg(type);
    if (digest_size(alg) < 0)
        return 0;
    DigestCtx d;
    d.alg = alg;
    digest_init(d);
    if (sourceSize != 0 && source != nullptr)
        digest_update(d, source, (size_t)sourceSize);
    digest_final(d, md);
    *mdSize = (uint32_t)digest_size(alg);
    return 1;
}

// -- HMAC --

void* CryptoNative_HmacCreate(uint8_t* key, int32_t keyLen, void* md)
{
    int32_t alg = handle_alg(md);
    if (digest_size(alg) < 0)
        return nullptr;
    OsslHmacCtx* h = static_cast<OsslHmacCtx*>(std::malloc(sizeof(OsslHmacCtx)));
    h->core.alg = alg;
    hmac_init(h->core, key, keyLen < 0 ? 0 : keyLen);
    h->innerInit = h->core.inner; // snapshot the primed inner state for Reset
    return h;
}

void CryptoNative_HmacDestroy(void* ctx)
{
    std::free(ctx);
}

int32_t CryptoNative_HmacReset(void* ctx)
{
    OsslHmacCtx* h = static_cast<OsslHmacCtx*>(ctx);
    if (h == nullptr)
        return 0;
    h->core.inner = h->innerInit; // restore keyed state, no re-key needed
    return 1;
}

int32_t CryptoNative_HmacUpdate(void* ctx, uint8_t* data, int32_t len)
{
    if (len == 0)
        return 1;
    if (ctx == nullptr || data == nullptr)
        return 0;
    digest_update(static_cast<OsslHmacCtx*>(ctx)->core.inner, data, (size_t)len);
    return 1;
}

int32_t CryptoNative_HmacFinal(void* ctx, uint8_t* md, int32_t* len)
{
    OsslHmacCtx* h = static_cast<OsslHmacCtx*>(ctx);
    if (h == nullptr || md == nullptr)
        return 0;
    hmac_final(h->core, md); // non-destructive; managed calls HmacReset separately
    if (len != nullptr)
        *len = digest_size(h->core.alg);
    return 1;
}

int32_t CryptoNative_HmacCurrent(void* ctx, uint8_t* md, int32_t* len)
{
    OsslHmacCtx* h = static_cast<OsslHmacCtx*>(ctx);
    if (h == nullptr || md == nullptr)
        return 0;
    hmac_final(h->core, md);
    if (len != nullptr)
        *len = digest_size(h->core.alg);
    return 1;
}

void* CryptoNative_HmacCopy(void* ctx)
{
    if (ctx == nullptr)
        return nullptr;
    OsslHmacCtx* h = static_cast<OsslHmacCtx*>(std::malloc(sizeof(OsslHmacCtx)));
    *h = *static_cast<OsslHmacCtx*>(ctx);
    return h;
}

int32_t CryptoNative_HmacOneShot(void* type, uint8_t* key, int32_t keyLen,
                                 uint8_t* source, int32_t sourceSize,
                                 uint8_t* md, int32_t* mdSize)
{
    if (md == nullptr || mdSize == nullptr)
        return 0;
    int32_t alg = handle_alg(type);
    if (digest_size(alg) < 0)
        return 0;
    HmacCtx h;
    h.alg = alg;
    hmac_init(h, key, keyLen < 0 ? 0 : keyLen);
    if (sourceSize != 0 && source != nullptr)
        digest_update(h.inner, source, (size_t)sourceSize);
    hmac_final(h, md);
    *mdSize = digest_size(alg);
    return 1;
}

// -- RNG: delegate to the same real-entropy backend as SystemNative_* --

int32_t SystemNative_GetCryptographicallySecureRandomBytes(uint8_t* buffer, int32_t bufferLength);

int32_t CryptoNative_GetRandomBytes(uint8_t* buf, int32_t num)
{
    if (num < 0)
        return 0;
    if (num == 0)
        return 1;
    if (buf == nullptr)
        return 0;
    return SystemNative_GetCryptographicallySecureRandomBytes(buf, num) == 0 ? 1 : 0;
}

// -- OpenSSL init + error surface: no-op / zero stubs --
// The portable cores never surface an OpenSSL error, so the managed error-path
// helpers are only reached (if at all) on init and must merely link and report
// "no error". EnsureOpenSslInitialized returns 0 = success (real pal_init.c
// convention); the Err* helpers report an empty/zero error.

int32_t CryptoNative_EnsureOpenSslInitialized(void)
{
    return 0; // success — nothing to initialize for the self-contained cores
}

uint64_t CryptoNative_ErrClearError(void)
{
    return 0; // no queued error
}

uint64_t CryptoNative_ErrPeekLastError(void)
{
    return 0;
}

uint64_t CryptoNative_ErrGetExceptionError(int32_t* isAllocFailure)
{
    if (isAllocFailure != nullptr)
        *isAllocFailure = 0;
    return 0;
}

void CryptoNative_ErrErrorStringN(uint64_t e, uint8_t* buf, int32_t len)
{
    (void)e;
    if (buf != nullptr && len > 0)
        buf[0] = 0; // empty NUL-terminated string
}

} // extern "C"

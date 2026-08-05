// dn2cpp_apple_crypto_digest.cpp — self-contained implementation of the
// AppleCryptoNative digest/HMAC surface the transpiled real-BCL hashing code
// calls (System.Security.Cryptography: SHA256/SHA1/MD5/SHA384/SHA512 +
// HMAC-* in every shape — one-shot HashData, instance ComputeHash /
// TransformBlock, IncrementalHash).
//
// The real System.Security.Cryptography.dll (macOS flavor) bottoms out in
// `[LibraryImport("libSystem.Security.Cryptography.Native.Apple")]`
// P/Invokes. dn2cpp lowers those to direct native calls
// (Compilation.IsRuntimeProvidedPInvokeModule) and provides the
// AppleCryptoNative_{Digest*,Hmac*} symbols here over the portable, hand-written
// MD5/SHA-1/SHA-2 + HMAC cores in dn2cpp_hash_cores.h (FIPS 180-4 / RFC 1321 /
// RFC 2104), so a transpiled binary stays self-contained — no CommonCrypto
// link, which also keeps this TU portable to non-Apple targets. The Linux RID
// of the same assembly instead calls the OpenSsl module (CryptoNative_*),
// implemented over the very same cores in dn2cpp_openssl_crypto_digest.cpp.
//
// ABI source of truth: dotnet/runtime v10.0.x
// src/native/libs/System.Security.Cryptography.Native.Apple/pal_digest.{h,c}
// and pal_hmac.{h,c}; the 16 signatures below were additionally verified by
// decoding the real net10.0 System.Security.Cryptography.dll's Interop
// declarations. Semantics preserved: success = 1; DigestFinal finalizes and
// then RE-INITIALIZES the context (managed HashProvider reuses it);
// DigestCurrent/HmacCurrent are clone-then-final (non-destructive);
// HmacFinal does NOT re-key (managed LiteHmac.Reset calls HmacInit again);
// zero-length input with a null pointer is accepted; an unknown algorithm
// yields a null handle with *pcbDigest = -1.
//
// PAL_HashAlgorithm: Unknown=0, Md5=1, Sha1=2, Sha256=3, Sha384=4, Sha512=5.

#include "dn2cpp_hash_cores.h"

// ---- the AppleCryptoNative_* PAL surface -----------------------------------

extern "C" {

void* AppleCryptoNative_DigestCreate(int32_t algorithm, int32_t* pcbDigest)
{
    if (pcbDigest == nullptr)
        return nullptr;
    *pcbDigest = digest_size(algorithm);
    if (*pcbDigest < 0)
        return nullptr; // unknown algorithm: null handle, *pcbDigest = -1
    DigestCtx* d = static_cast<DigestCtx*>(std::malloc(sizeof(DigestCtx)));
    d->alg = algorithm;
    digest_init(*d);
    return d;
}

int32_t AppleCryptoNative_DigestUpdate(void* ctx, uint8_t* pBuf, int32_t bufLen)
{
    if (bufLen == 0)
        return 1;
    if (ctx == nullptr || pBuf == nullptr)
        return -1;
    digest_update(*static_cast<DigestCtx*>(ctx), pBuf, (size_t)bufLen);
    return 1;
}

int32_t AppleCryptoNative_DigestFinal(void* ctx, uint8_t* pOutput, int32_t cbOutput)
{
    DigestCtx* d = static_cast<DigestCtx*>(ctx);
    if (d == nullptr || pOutput == nullptr || cbOutput < digest_size(d->alg))
        return -1;
    digest_final(*d, pOutput);
    digest_init(*d); // pal_digest.c re-initializes after finalizing (context reuse)
    return 1;
}

int32_t AppleCryptoNative_DigestCurrent(void* ctx, uint8_t* pOutput, int32_t cbOutput)
{
    DigestCtx* d = static_cast<DigestCtx*>(ctx);
    if (d == nullptr || pOutput == nullptr || cbOutput < digest_size(d->alg))
        return -1;
    DigestCtx tmp = *d; // clone-then-final: the live state is untouched
    digest_final(tmp, pOutput);
    return 1;
}

int32_t AppleCryptoNative_DigestReset(void* ctx)
{
    if (ctx == nullptr)
        return -1;
    digest_init(*static_cast<DigestCtx*>(ctx));
    return 1;
}

void* AppleCryptoNative_DigestClone(void* ctx)
{
    if (ctx == nullptr)
        return nullptr;
    DigestCtx* d = static_cast<DigestCtx*>(std::malloc(sizeof(DigestCtx)));
    *d = *static_cast<DigestCtx*>(ctx);
    return d;
}

void AppleCryptoNative_DigestFree(void* ctx)
{
    std::free(ctx);
}

int32_t AppleCryptoNative_DigestOneShot(int32_t algorithm, uint8_t* pBuf, int32_t cbBuf,
                                        uint8_t* pOutput, int32_t cbOutput, int32_t* pcbDigest)
{
    if (pOutput == nullptr || cbOutput <= 0 || pcbDigest == nullptr)
        return -1;
    *pcbDigest = digest_size(algorithm);
    if (*pcbDigest < 0 || cbOutput < *pcbDigest)
        return -1;
    DigestCtx d;
    d.alg = algorithm;
    digest_init(d);
    if (cbBuf != 0)
        digest_update(d, pBuf, (size_t)cbBuf);
    digest_final(d, pOutput);
    return 1;
}

void* AppleCryptoNative_HmacCreate(int32_t algorithm, int32_t* pcbDigest)
{
    if (pcbDigest == nullptr)
        return nullptr;
    *pcbDigest = digest_size(algorithm);
    if (*pcbDigest < 0)
        return nullptr;
    HmacCtx* h = static_cast<HmacCtx*>(std::malloc(sizeof(HmacCtx)));
    h->alg = algorithm;
    // Not yet keyed — the managed ctor always follows with HmacInit before use.
    hmac_init(*h, nullptr, 0);
    return h;
}

int32_t AppleCryptoNative_HmacInit(void* ctx, uint8_t* pKey, int32_t cbKey)
{
    if (ctx == nullptr || (cbKey != 0 && pKey == nullptr) || cbKey < 0)
        return 0;
    hmac_init(*static_cast<HmacCtx*>(ctx), pKey, cbKey);
    return 1;
}

int32_t AppleCryptoNative_HmacUpdate(void* ctx, uint8_t* pBuf, int32_t cbBuf)
{
    if (cbBuf == 0)
        return 1;
    if (ctx == nullptr || pBuf == nullptr)
        return 0;
    digest_update(static_cast<HmacCtx*>(ctx)->inner, pBuf, (size_t)cbBuf);
    return 1;
}

int32_t AppleCryptoNative_HmacFinal(void* ctx, uint8_t* pOutput, int32_t cbOutput)
{
    HmacCtx* h = static_cast<HmacCtx*>(ctx);
    if (h == nullptr || pOutput == nullptr || cbOutput < digest_size(h->alg))
        return 0;
    hmac_final(*h, pOutput);
    return 1; // no re-key: LiteHmac.Reset calls HmacInit again
}

int32_t AppleCryptoNative_HmacCurrent(void* ctx, uint8_t* pOutput, int32_t cbOutput)
{
    HmacCtx* h = static_cast<HmacCtx*>(ctx);
    if (h == nullptr || pOutput == nullptr || cbOutput < digest_size(h->alg))
        return 0;
    hmac_final(*h, pOutput); // already non-destructive
    return 1;
}

void* AppleCryptoNative_HmacClone(void* ctx)
{
    if (ctx == nullptr)
        return nullptr;
    HmacCtx* h = static_cast<HmacCtx*>(std::malloc(sizeof(HmacCtx)));
    *h = *static_cast<HmacCtx*>(ctx);
    return h;
}

void AppleCryptoNative_HmacFree(void* ctx)
{
    std::free(ctx);
}

int32_t AppleCryptoNative_HmacOneShot(int32_t algorithm, uint8_t* pKey, int32_t cbKey,
                                      uint8_t* pBuf, int32_t cbBuf,
                                      uint8_t* pOutput, int32_t cbOutput, int32_t* pcbDigest)
{
    if (pOutput == nullptr || cbOutput <= 0 || pcbDigest == nullptr)
        return -1;
    *pcbDigest = digest_size(algorithm);
    if (*pcbDigest < 0 || cbOutput < *pcbDigest)
        return -1;
    HmacCtx h;
    h.alg = algorithm;
    hmac_init(h, pKey, cbKey);
    if (cbBuf != 0)
        digest_update(h.inner, pBuf, (size_t)cbBuf);
    hmac_final(h, pOutput);
    return 1;
}

} // extern "C"
